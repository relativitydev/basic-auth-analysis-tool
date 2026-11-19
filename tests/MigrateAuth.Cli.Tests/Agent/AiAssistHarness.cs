using System.Security.Cryptography;

using MigrateAuth.Cli.Agent;
using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Findings;
using MigrateAuth.Cli.Reading;
using MigrateAuth.Cli.Session;
using MigrateAuth.Cli.Tests.Session;
using MigrateAuth.Cli.Tests.Writing;
using MigrateAuth.Cli.Writing;

using MigrateAuth.FakeAgent;

using Xunit;

namespace MigrateAuth.Cli.Tests.Agent;

/// <summary>One flush of the typed-ahead input: how many lines had been read, and whether the fake agent had run.</summary>
internal sealed record FlushRecord(int ReadsBefore, bool ChildHadRun);

/// <summary>A native console that records every flush and does nothing else, as the Windows one would on a console.</summary>
internal sealed class FakeNativeConsole : INativeConsole
{
	internal List<FlushRecord> Flushes { get; } = [];

	internal Func<int> ReadsSoFar { get; set; } = () => 0;

	internal Func<bool> ChildHasRun { get; set; } = () => false;

	public bool TryGetMode(NativeConsoleStream stream, out uint mode)
	{
		mode = 0;

		return false;
	}

	public bool TrySetMode(NativeConsoleStream stream, uint mode)
	{
		return false;
	}

	public bool TryFlushInput()
	{
		Flushes.Add(new FlushRecord(ReadsSoFar(), ChildHasRun()));

		return true;
	}
}

/// <summary>
/// The lines a user would type, handed out one per read, with a record of what the console had shown at each read
/// and a hook a test can use to act at a given read (to cancel, or to remove the fake agent).
/// </summary>
internal sealed class ScriptedReader(IEnumerable<string> lines, Func<string> outputSoFar) : TextReader
{
	private readonly Queue<string> _lines = new(lines);

	/// <summary>The console output at the moment each read began.</summary>
	internal List<string> OutputBeforeEachRead { get; } = [];

	/// <summary>Called with the zero-based number of the read, before its line is returned.</summary>
	internal Action<int>? OnRead { get; set; }

	internal int ReadCount => OutputBeforeEachRead.Count;

	public override string? ReadLine()
	{
		int index = OutputBeforeEachRead.Count;

		OutputBeforeEachRead.Add(outputSoFar());
		OnRead?.Invoke(index);

		return _lines.Count > 0 ? _lines.Dequeue() : null;
	}
}

/// <summary>A console writer that tells a test when a given text has been written, so it can act at that moment.</summary>
internal sealed class WatchedWriter : StringWriter
{
	private string? _watchedText;
	private Action? _whenWritten;

	/// <summary>Runs <paramref name="action"/> once the output contains <paramref name="text"/>.</summary>
	internal void When(string text, Action action)
	{
		_watchedText = text;
		_whenWritten = action;
	}

	public override void Write(string? value)
	{
		base.Write(value);
		Check();
	}

	public override void WriteLine(string? value)
	{
		base.WriteLine(value);
		Check();
	}

	private void Check()
	{
		if (_watchedText is not null && ToString().Contains(_watchedText, StringComparison.Ordinal))
		{
			Action? action = _whenWritten;

			_watchedText = null;
			_whenWritten = null;
			action?.Invoke();
		}
	}
}

/// <summary>A launcher that answers what a test tells it to and starts nothing, for the branches no real fake can reach.</summary>
internal sealed class StubLauncher(
	AgentAvailability availability,
	AgentLaunchEligibility eligibility,
	AgentTerminationStatus status,
	AgentResolutionDescription? resolution = null) : IExternalAgentLauncher
{
	private const string _STUB_FILE_NAME = "stub-agent";

	internal int LaunchCount { get; private set; }

	public AgentAvailability CheckAvailability()
	{
		return availability;
	}

	public AgentLaunchEligibility CheckLaunchable(PromptPackFilePath writtenPack)
	{
		return eligibility;
	}

	public AgentResolutionDescription DescribeResolution()
	{
		return resolution ?? new AgentResolutionDescription(_STUB_FILE_NAME, AgentStartKind.Direct, 1, 1);
	}

	public AgentTerminationStatus Launch(PromptPackFilePath launchCopy)
	{
		LaunchCount++;

		return status;
	}
}

/// <summary>
/// EVERYTHING AN AI-ASSIST STAGE TEST NEEDS: a repository folder, a documentation destination shown on the console
/// as a relative folder, a report this process wrote (with its holder) and a data artifact, a folder of fake agents
/// that is the whole search path, the scripted console, a fake native console and the findings of the shared fixture
/// on a session state. Nothing here is the machine's own search path, and every launcher is first held to the guard
/// of the test resolver, so a command found outside the test's temporary folders fails the test before it starts.
/// </summary>
internal sealed class AiAssistHarness : IDisposable
{
	internal const string DESTINATION_CONSOLE_TEXT = "./Outputs";
	internal const string DATA_ARTIFACT_FILE_NAME = "basic-auth-migration-2026-09-18.json";
	internal const string PACK_SEARCH_PATTERN = "ai-assist-*.md";
	internal const string LINE_FEED = "\n";
	internal const string CLAUDE_PACK_STEM = AgentEnvironmentCatalog.CLAUDE_CODE_PACK_STEM;
	internal const string CLAUDE_PACK_FILE_NAME = $"{AgentEnvironmentCatalog.CLAUDE_CODE_PACK_STEM}.md";
	internal const string CLAUDE_SECOND_PACK_FILE_NAME = $"{AgentEnvironmentCatalog.CLAUDE_CODE_PACK_STEM}-2.md";
	internal const string CODEX_PACK_FILE_NAME = $"{AgentEnvironmentCatalog.OPENAI_CODEX_PACK_STEM}.md";
	internal const string COPILOT_PACK_FILE_NAME = $"{AgentEnvironmentCatalog.COPILOT_CLI_PACK_STEM}.md";
	internal const char LINE_FEED_CHARACTER = '\n';
	internal const string REPOSITORY_TOOLS_FOLDER = "tools";
	internal const char ESCAPE_CHARACTER = '\u001b';
	internal const char BELL_CHARACTER = '\u0007';
	internal const byte REPORT_EDIT_MARKER = (byte)'#';
	internal const int CONFIRMATION_READ_INDEX = 1;
	internal const int SHELL_CONFIRMATION_READ_INDEX = 2;

	private const string _DATA_ARTIFACT_BODY = "{\"findings\":[]}";

	private readonly WatchedWriter _output = new();
	private readonly FakeNativeConsole _nativeConsole = new();
	private AiAssistStageRunner? _runner;
	private bool _destinationReplacedByFile;

	internal AiAssistHarness(IEnumerable<string> inputLines, Func<IncrementalHash>? hashFactory = null, bool writeReport = true)
	{
		Repository = TemporaryFolder.Create();
		DestinationFolder = TemporaryFolder.Create();
		Fakes = new FakeAgentFolder();
		Holder = new ReportWriteHolder();
		SearchPath = new TestAgentSearchPathProvider([Fakes.FolderPath]);
		Input = new ScriptedReader(inputLines, () => _output.ToString());
		Destination = DocumentationDestinationFolderPath.FromResolvedText(DestinationFolder.Path, DESTINATION_CONSOLE_TEXT);
		Writer = new AtomicArtifactWriter(RepositoryFolderPath.FromSuppliedText(Repository.Path), Destination, Holder, hashFactory);
		State = SessionState.Fresh(Catalog.Version, RepositoryPath.Label);
		State.RecordAnalysisOutcome(PromptPackFixture.Findings(), FindingsCapOutcome.Empty, SkippedItemTallies.Empty);

		_nativeConsole.ReadsSoFar = () => Input.ReadCount;

		if (writeReport)
		{
			ReportAppendFixture.WriteReport(Writer, DestinationFolder);
			File.WriteAllText(DestinationFolder.Combine(DATA_ARTIFACT_FILE_NAME), _DATA_ARTIFACT_BODY);
		}
	}

	internal TemporaryFolder Repository { get; }

	internal TemporaryFolder DestinationFolder { get; }

	internal FakeAgentFolder Fakes { get; }

	internal ReportWriteHolder Holder { get; }

	internal TestAgentSearchPathProvider SearchPath { get; set; }

	internal ScriptedReader Input { get; }

	internal DocumentationDestinationFolderPath Destination { get; }

	internal AtomicArtifactWriter Writer { get; }

	internal SessionState State { get; }

	internal FakeNativeConsole NativeConsole => _nativeConsole;

	internal WatchedWriter Output => _output;

	/// <summary>Whether both streams are a terminal.</summary>
	internal bool IsTerminal { get; set; } = true;

	/// <summary>The appender the stage is given; the real writer when none is set.</summary>
	internal IReportAppender? AppenderOverride { get; set; }

	/// <summary>A launcher that replaces the real one for every environment.</summary>
	internal IExternalAgentLauncher? LauncherOverride { get; set; }

	/// <summary>The test budget the real launchers are built with; none waits for the agent without limit.</summary>
	internal TimeSpan? TestBudget { get; set; }

	internal static LanguageCatalog Catalog => PromptPackFixture.Catalog;

	internal RepositoryFolderPath RepositoryPath => RepositoryFolderPath.FromSuppliedText(Repository.Path);

	internal string Transcript => _output.ToString();

	internal string ReportPath => DestinationFolder.Combine(PromptPackFixture.REPORT_FILE_NAME);

	internal string DataArtifactPath => DestinationFolder.Combine(DATA_ARTIFACT_FILE_NAME);

	internal AiAssistStageRunner Runner => _runner ??= BuildRunner();

	/// <summary>Runs the stage once.</summary>
	internal StageRunOutcome Run(CancellationToken cancellationToken = default)
	{
		return Runner.Run(CommandId.AiAssist, State, cancellationToken);
	}

	/// <summary>Installs a fake agent under the command name of <paramref name="environment"/> and returns its path.</summary>
	internal string InstallFake(AgentEnvironment environment, FakeAgentBehavior? behavior = null)
	{
		return Fakes.Install(AgentEnvironmentCatalog.Describe(environment).CommandName, behavior ?? new FakeAgentBehavior());
	}

	/// <summary>The path of the record file the fake of <paramref name="environment"/> writes.</summary>
	internal string RecordPathOf(AgentEnvironment environment)
	{
		string executable = Path.Combine(Fakes.FolderPath, FakeAgentFolder.ExecutableName(AgentEnvironmentCatalog.Describe(environment).CommandName));

		return $"{executable}{FakeAgentRecord.FILE_SUFFIX}";
	}

	internal bool FakeWasStarted(AgentEnvironment environment)
	{
		return File.Exists(RecordPathOf(environment));
	}

	internal FakeAgentRecord RecordOf(AgentEnvironment environment)
	{
		return FakeAgentRecord.ReadFrom(RecordPathOf(environment));
	}

	/// <summary>Whether any fake of the folder recorded a start.</summary>
	internal bool AnyFakeWasStarted()
	{
		return Directory.GetFiles(Fakes.FolderPath, $"*{FakeAgentRecord.FILE_SUFFIX}").Length > 0;
	}

	internal byte[] ReportBytes()
	{
		return File.ReadAllBytes(ReportPath);
	}

	/// <summary>Changes one byte of the report on disk, as an edit after this process wrote it, and returns the edited bytes.</summary>
	internal byte[] EditTheReportInPlace()
	{
		byte[] edited = ReportBytes();

		edited[^2] = REPORT_EDIT_MARKER;
		File.WriteAllBytes(ReportPath, edited);

		return edited;
	}

	/// <summary>The command name of the Claude Code environment.</summary>
	internal static string ClaudeCommandName => AgentEnvironmentCatalog.Describe(AgentEnvironment.ClaudeCode).CommandName;

	internal byte[] DataArtifactBytes()
	{
		return File.ReadAllBytes(DataArtifactPath);
	}

	/// <summary>The prompt packs in the destination, in name order.</summary>
	internal string[] PackFiles()
	{
		return [.. Directory.GetFiles(DestinationFolder.Path, PACK_SEARCH_PATTERN).Select(path => Path.GetFileName(path)).Order(StringComparer.Ordinal)];
	}

	/// <summary>The launch folders in the destination.</summary>
	internal string[] LaunchFolders()
	{
		return Directory.Exists(DestinationFolder.Path)
			? [.. Directory.GetDirectories(DestinationFolder.Path, $"{LaunchCopyLayout.FOLDER_NAME_PREFIX}*").Order(StringComparer.Ordinal)]
			: [];
	}

	/// <summary>Every absolute path this harness knows of, none of which may ever appear on the console.</summary>
	internal string[] KnownAbsolutePaths()
	{
		return
		[
			Repository.Path,
			DestinationFolder.Path,
			Fakes.FolderPath,
			TemporaryFolder.CanonicalRoot,
			Path.GetTempPath().TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
		];
	}

	private AiAssistStageRunner BuildRunner()
	{
		SessionConsole console = new(Input, _output);

		_nativeConsole.ChildHasRun = AnyFakeWasStarted;

		return new AiAssistStageRunner(
			new AiAssistStageContext
			{
				Console = console,
				Repository = RepositoryPath,
				Destination = Destination,
				Catalog = Catalog,
				MinimumConfidenceLevel = ConfidenceLevel.Low,
				ArtifactWriter = Writer,
				ReportHolder = Holder,
				LauncherFor = CreateLauncher,
				IsInteractiveTerminal = () => IsTerminal
			},
			AppenderOverride ?? Writer,
			_nativeConsole);
	}

	private IExternalAgentLauncher CreateLauncher(AgentEnvironment environment)
	{
		IExternalAgentLauncher launcher;

		if (LauncherOverride is not null)
		{
			launcher = LauncherOverride;
		}
		else
		{
			string commandName = AgentEnvironmentCatalog.Describe(environment).CommandName;

			TestAgentResolver.Resolve(new TestAgentSearchPathProvider(SearchPath.PeekEntries()), Repository.Path, commandName);

			launcher = AgentLauncherSwitch.Create(environment, SearchPath, RepositoryPath, TestBudget);
		}

		return launcher;
	}

	/// <summary>
	/// Replaces the destination folder, with everything in it, by a file, so that nothing can be written under it. The
	/// folder is made again when the harness is disposed.
	/// </summary>
	internal void ReplaceDestinationWithFile()
	{
		Directory.Delete(DestinationFolder.Path, recursive: true);
		File.WriteAllText(DestinationFolder.Path, "now a file");
		_destinationReplacedByFile = true;
	}

	public void Dispose()
	{
		if (_destinationReplacedByFile)
		{
			File.Delete(DestinationFolder.Path);
			Directory.CreateDirectory(DestinationFolder.Path);
		}

		Fakes.Dispose();
		Repository.Dispose();
		DestinationFolder.Dispose();
		_output.Dispose();
	}

	/// <summary>Asserts that nothing was started, nothing was launched and no launch folder was made.</summary>
	internal void AssertNothingWasLaunched()
	{
		Assert.False(AnyFakeWasStarted());
		Assert.Empty(LaunchFolders());
	}

	/// <summary>The path of the persisted session state of the repository in the destination.</summary>
	internal string StateFilePath => DestinationFolder.Combine(SessionStateStore.BuildStateFileName(RepositoryPath));

	/// <summary>Marks the four assessment stages completed and persists the state, as a session that ran them would have.</summary>
	internal void CompleteTheFourStagesAndSaveTheState()
	{
		foreach (CommandId stage in (CommandId[])[CommandId.Scan, CommandId.Analyze, CommandId.Suggest, CommandId.Report])
		{
			State.MarkCompleted(stage);
		}

		new SessionStateStore(Destination).Save(RepositoryPath, State);
	}

	/// <summary>Reads the bytes of the report, the pack and the persisted state, and the completed stages, as they are now.</summary>
	internal SessionSnapshot TakeSnapshot()
	{
		return new SessionSnapshot(
			ReportBytes(),
			File.ReadAllBytes(DestinationFolder.Combine(PackFiles().Single())),
			File.ReadAllBytes(StateFilePath),
			File.GetLastWriteTimeUtc(StateFilePath),
			[.. State.CompletedStagesInRunOrder().Select(stage => stage.Id)]);
	}

	/// <summary>Runs the scripted input through the shell, the way a session does, and returns the exit code it ends with.</summary>
	internal ExitCode RunThroughTheShell()
	{
		return new SessionShell(RepositoryPath, State, new SessionStateStore(Destination), Runner, new SessionConsole(Input, _output)).Run();
	}

	/// <summary>Asserts that the report, the pack and the persisted state are as the snapshot held them and that the exit code is success.</summary>
	internal void AssertNothingChangedSince(SessionSnapshot before, ExitCode exitCode)
	{
		SessionSnapshot after = TakeSnapshot();

		Assert.Equal(ExitCode.Success, exitCode);
		Assert.Equal(before.Report, after.Report);
		Assert.Equal(before.Pack, after.Pack);
		Assert.Equal(before.State, after.State);
		Assert.Equal(before.StateWrittenUtc, after.StateWrittenUtc);
		Assert.Equal(before.Completed, after.Completed);
		Assert.False(State.IsCompleted(CommandId.AiAssist));
	}
}

/// <summary>What a finished stage must have left as it was: the report, the pack, the persisted state and the completed stages.</summary>
internal sealed record SessionSnapshot(byte[] Report, byte[] Pack, byte[] State, DateTime StateWrittenUtc, CommandId[] Completed);
