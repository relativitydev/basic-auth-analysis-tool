using System.Diagnostics;
using System.Globalization;
using System.Text;

using MigrateAuth.Cli.Agent;
using MigrateAuth.Cli.Session;
using MigrateAuth.Cli.Tests.Agent;
using MigrateAuth.Cli.Tests.Reading;
using MigrateAuth.Cli.Tests.Session;

using MigrateAuth.FakeAgent;

using Xunit;

namespace MigrateAuth.Cli.Tests.AcceptanceHarness;

/// <summary>
/// THE LAUNCH ON A REAL TERMINAL, ON LINUX: the session runs inside a pseudo-terminal made by <c>script -qec</c>, the
/// stage offers the launch, the answer is yes, the fake agent starts and stays alive, a real 0x03 byte is typed, the
/// terminal driver turns it into an interrupt for the foreground group, and the stage reports the agent as interrupted
/// and returns to the prompt.
///
/// WHERE IT RUNS. On Linux, with <c>script</c> on the inherited search path. On Windows, and wherever <c>script</c> is
/// missing, the test is a declared no-op: it returns before starting anything, and the guard is itself asserted so a
/// platform that cannot run it says so. The child's search path is replaced, as for every process test, and the agent it
/// resolves is asserted to lie in this test's temporary folder.
/// </summary>
public sealed class AiAssistPtyProcessTests
{
	private const string _SCRIPT_COMMAND_NAME = "script";
	private const string _QUIET_COMMAND_OPTIONS = "-qec";
	private const string _NULL_TYPESCRIPT_PATH = "/dev/null";
	private const byte _INTERRUPT_BYTE = 0x03;
	private const int _WAIT_SECONDS = 90;
	private const int _POLL_MILLISECONDS = 50;
	private const int _HANG_LIFETIME_SECONDS = 120;
	private const string _YES = "y";
	private const char _LINE_FEED = '\n';
	private const int _PUMP_BUFFER_CHARACTERS = 1024;

	[Fact]
	public void TheGuardNamesThePlatformsThatCannotRunTheTestAndItIsADeclaredNoOpThere()
	{
		bool canRun = CanRunOnThisMachine();

		Assert.Equal(OperatingSystem.IsLinux() && FindOnInheritedPath(_SCRIPT_COMMAND_NAME) is not null, canRun);
	}

	[Fact]
	public void ARealInterruptTypedOnATerminalEndsTheLaunchedAgentAndTheStageReturnsToThePrompt()
	{
		if (CanRunOnThisMachine())
		{
			RunTheInterruptScenario();
		}
	}

	private static bool CanRunOnThisMachine()
	{
		return OperatingSystem.IsLinux() && FindOnInheritedPath(_SCRIPT_COMMAND_NAME) is not null;
	}

	private static void RunTheInterruptScenario()
	{
		using RepositoryFixture repository = RepositoryFixture.CreateEmpty();
		using TemporaryFolder destination = TemporaryFolder.Create();
		using FakeAgentFolder fakes = new();

		AiAssistProcessSupport.WriteRepository(repository);

		string agentPath = fakes.Install(
			AgentEnvironmentCatalog.Describe(AgentEnvironment.ClaudeCode).CommandName,
			new FakeAgentBehavior { Ending = FakeAgentEnding.Hang, HangLifetimeSeconds = _HANG_LIFETIME_SECONDS });

		AssertTheResolvedAgentIsInTheTestsTemporaryFolder(fakes, repository);

		using PtySession session = PtySession.Start(repository.RootPath, destination.Path, fakes.FolderPath);

		session.TypeLines(AiAssistProcessSupport.FourStagesThen(AiAssistProcessSupport.AiAssistCommand));
		session.WaitFor(AiAssistTexts.SELECTION_PROMPT);
		session.TypeLines([AiAssistProcessSupport.SelectionNumberOf(AgentEnvironment.ClaudeCode)]);
		session.WaitFor(AiAssistTexts.Confirmation(AgentEnvironment.ClaudeCode));
		session.TypeLines([_YES]);
		session.WaitForFile($"{agentPath}{FakeAgentRecord.FILE_SUFFIX}");
		session.TypeByte(_INTERRUPT_BYTE);

		string interrupted = string.Format(
			CultureInfo.InvariantCulture,
			AiAssistTexts.INTERRUPTED_BY_USER_FORMAT,
			AgentEnvironmentCatalog.Describe(AgentEnvironment.ClaudeCode).DisplayName);

		session.WaitFor(interrupted);
		session.TypeLines([AiAssistProcessSupport.ExitCommand]);

		int exitCode = session.WaitForExit();

		Assert.Equal((int)ExitCode.Success, exitCode);
		Assert.Contains(AiAssistTexts.TERMINATION_SUFFIX, session.Transcript, StringComparison.Ordinal);
		Assert.Single(AiAssistProcessSupport.LaunchFolders(destination));
		Assert.DoesNotContain(agentPath, session.Transcript, StringComparison.OrdinalIgnoreCase);
	}

	/// <summary>The agent the child will resolve, resolved the way the child resolves it, must be inside the temporary folder.</summary>
	private static void AssertTheResolvedAgentIsInTheTestsTemporaryFolder(FakeAgentFolder fakes, RepositoryFixture repository)
	{
		AgentResolution resolution = TestAgentResolver.Resolve(
			[fakes.FolderPath],
			repository.RootPath,
			AgentEnvironmentCatalog.Describe(AgentEnvironment.ClaudeCode).CommandName);

		Assert.True(resolution.WasFound);
		Assert.StartsWith(fakes.FolderPath, resolution.ExecutablePath, StringComparison.Ordinal);
	}

	private static string? FindOnInheritedPath(string commandName)
	{
		string inherited = Environment.GetEnvironmentVariable(ChildPathReplacement.PATH_VARIABLE_NAME) ?? string.Empty;

		return inherited
			.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
			.Select(entry => Path.Combine(entry, commandName))
			.FirstOrDefault(File.Exists);
	}

	/// <summary>One session inside a pseudo-terminal: what is typed, what is shown, and the waits between them.</summary>
	private sealed class PtySession : IDisposable
	{
		private readonly Process _process;
		private readonly ChildPathReplacement _pathReplacement;
		private readonly TemporaryFolder _profileFolder;
		private readonly StringBuilder _transcript = new();
		private readonly Lock _transcriptLock = new();
		private readonly List<byte> _rawOutput = [];
		private readonly List<byte> _rawError = [];
		private readonly List<int> _outputChunkSizes = [];
		private readonly Task _outputReader;
		private readonly Task _errorReader;
		private bool _outputEnded;
		private bool _errorEnded;
		private string? _outputFailure;
		private string? _errorFailure;

		private PtySession(Process process, ChildPathReplacement pathReplacement, TemporaryFolder profileFolder)
		{
			_process = process;
			_pathReplacement = pathReplacement;
			_profileFolder = profileFolder;
			_outputReader = Task.Run(() => Pump(process.StandardOutput, appendToTranscript: true));
			_errorReader = Task.Run(() => Pump(process.StandardError, appendToTranscript: false));
		}

		internal string Transcript
		{
			get
			{
				lock (_transcriptLock)
				{
					return _transcript.ToString();
				}
			}
		}

		internal static PtySession Start(string repositoryPath, string destinationPath, string agentFolderPath)
		{
			string scriptPath = FindOnInheritedPath(_SCRIPT_COMMAND_NAME)!;
			string childCommand = string.Join(
				' ',
				Quote(CliProcessHarness.HostPath),
				Quote(CliProcessHarness.EntryAssemblyPath),
				CommandLineParser.REPOSITORY_FOLDER_PATH_OPTION,
				Quote(repositoryPath),
				CommandLineParser.DOCUMENTATION_DESTINATION_FOLDER_PATH_OPTION,
				Quote(destinationPath));
			ProcessStartInfo startInfo = new()
			{
				FileName = scriptPath,
				WorkingDirectory = AppContext.BaseDirectory,
				RedirectStandardInput = true,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				UseShellExecute = false
			};

			startInfo.ArgumentList.Add(_QUIET_COMMAND_OPTIONS);
			startInfo.ArgumentList.Add(childCommand);
			startInfo.ArgumentList.Add(_NULL_TYPESCRIPT_PATH);

			TemporaryFolder profileFolder = TemporaryFolder.Create();
			ChildPathReplacement pathReplacement = ChildPathReplacement.Apply(startInfo, agentFolderPath);

			CliProcessHarness.IsolateProfile(startInfo, profileFolder.Path);

			Process process = Process.Start(startInfo) ?? throw new InvalidOperationException("The terminal wrapper did not start.");

			return new PtySession(process, pathReplacement, profileFolder);
		}

		internal void TypeLines(IEnumerable<string> lines)
		{
			foreach (string line in lines)
			{
				byte[] bytes = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false).GetBytes($"{line}{_LINE_FEED}");

				_process.StandardInput.BaseStream.Write(bytes);
				_process.StandardInput.BaseStream.Flush();
			}
		}

		internal void TypeByte(byte value)
		{
			_process.StandardInput.BaseStream.WriteByte(value);
			_process.StandardInput.BaseStream.Flush();
		}

		internal void WaitFor(string text)
		{
			Wait(() => Transcript.Contains(text, StringComparison.Ordinal), $"the terminal never showed: {text}");
		}

		internal void WaitForFile(string path)
		{
			Wait(() => File.Exists(path), $"the file never appeared: {path}");
		}

		internal int WaitForExit()
		{
			_process.StandardInput.Close();

			Assert.True(_process.WaitForExit(TimeSpan.FromSeconds(_WAIT_SECONDS)), "The session did not end.");
			_outputReader.Wait(TimeSpan.FromSeconds(_WAIT_SECONDS));
			_errorReader.Wait(TimeSpan.FromSeconds(_WAIT_SECONDS));

			return _process.ExitCode;
		}

		public void Dispose()
		{
			if (!_process.HasExited)
			{
				_process.Kill(entireProcessTree: true);
			}

			_process.Dispose();
			_pathReplacement.Dispose();
			_profileFolder.Dispose();
		}

		private static string Quote(string value) => $"\"{value}\"";

		private void Wait(Func<bool> condition, string failureMessage)
		{
			Stopwatch stopwatch = Stopwatch.StartNew();

			while (!condition() && stopwatch.Elapsed < TimeSpan.FromSeconds(_WAIT_SECONDS))
			{
				Thread.Sleep(_POLL_MILLISECONDS);
			}

			bool isMet = condition();
			string message = isMet
				? string.Empty
				: $"{failureMessage}{Environment.NewLine}Transcript so far:{Environment.NewLine}{Transcript}{Environment.NewLine}{Diagnostics()}";

			Assert.True(isMet, message);
		}

		/// <summary>
		/// What a failed wait, and only a failed one, adds to its message so one run settles why the text never arrived: the raw bytes of both
		/// streams escaped, the size of each chunk as it was read, whether either reader ended or failed, and whether the
		/// process is still alive. The same text is also written to a file whose path is printed.
		/// </summary>
		private string Diagnostics()
		{
			StringBuilder text = new();

			lock (_transcriptLock)
			{
				text.AppendLine($"Output bytes read: {_rawOutput.Count}; chunk sizes: {string.Join(',', _outputChunkSizes)}");
				text.AppendLine($"Output reader ended: {_outputEnded}; failure: {_outputFailure ?? "none"}");
				text.AppendLine($"Error bytes read: {_rawError.Count}; error reader ended: {_errorEnded}; failure: {_errorFailure ?? "none"}");
				text.AppendLine($"Process exited: {_process.HasExited}");
				text.AppendLine($"Raw output, escaped: {Escape(_rawOutput)}");
				text.AppendLine($"Raw error, escaped: {Escape(_rawError)}");
			}

			string diagnosticsPath = Path.Combine(Path.GetTempPath(), $"pty-diagnostics-{Guid.NewGuid():N}.txt");

			File.WriteAllText(diagnosticsPath, text.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
			text.AppendLine($"Diagnostics also written to: {diagnosticsPath}");

			return text.ToString();
		}

		private static string Escape(List<byte> bytes)
		{
			StringBuilder escaped = new();

			foreach (byte value in bytes)
			{
				if (value is >= 0x20 and < 0x7F)
				{
					escaped.Append((char)value);
				}
				else
				{
					escaped.Append(CultureInfo.InvariantCulture, $"\\x{value:X2}");
				}
			}

			return escaped.ToString();
		}

		private void Pump(StreamReader reader, bool appendToTranscript)
		{
			byte[] buffer = new byte[_PUMP_BUFFER_CHARACTERS];
			Decoder decoder = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false).GetDecoder();
			char[] characters = new char[_PUMP_BUFFER_CHARACTERS];
			string? failure = null;

			try
			{
				int read = reader.BaseStream.Read(buffer, 0, buffer.Length);

				while (read > 0)
				{
					int decoded = decoder.GetChars(buffer, 0, read, characters, 0);

					lock (_transcriptLock)
					{
						RecordChunk(appendToTranscript, buffer, read, characters, decoded);
					}

					read = reader.BaseStream.Read(buffer, 0, buffer.Length);
				}
			}
			catch (IOException exception)
			{
				failure = exception.ToString();
			}
			catch (ObjectDisposedException exception)
			{
				failure = exception.ToString();
			}

			lock (_transcriptLock)
			{
				RecordEnd(appendToTranscript, failure);
			}
		}

		private void RecordChunk(bool isOutput, byte[] buffer, int read, char[] characters, int decoded)
		{
			if (isOutput)
			{
				_rawOutput.AddRange(buffer.AsSpan(0, read).ToArray());
				_outputChunkSizes.Add(read);
				_transcript.Append(characters, 0, decoded);
			}
			else
			{
				_rawError.AddRange(buffer.AsSpan(0, read).ToArray());
			}
		}

		private void RecordEnd(bool isOutput, string? failure)
		{
			if (isOutput)
			{
				_outputEnded = true;
				_outputFailure = failure;
			}
			else
			{
				_errorEnded = true;
				_errorFailure = failure;
			}
		}
	}
}
