using MigrateAuth.Cli.Agent;
using MigrateAuth.Cli.Session;
using MigrateAuth.Cli.Tests.Agent;
using MigrateAuth.Cli.Tests.Fixtures;
using MigrateAuth.Cli.Tests.Reading;
using MigrateAuth.Cli.Tests.Session;
using MigrateAuth.Cli.Writing;

using Xunit;

namespace MigrateAuth.Cli.Tests.AcceptanceHarness;

/// <summary>
/// WHAT THE PROCESS TESTS OF THE AI-ASSIST STAGE SHARE: a repository whose three files each hold one finding, the
/// four assessment stages as entered commands, and the readers of what a finished session left in the destination.
///
/// Every run goes through <see cref="CliProcessHarness"/>, whose child search path is always replaced, so nothing here
/// can reach an agent installed on the machine. A test that wants an agent found puts a fake in a folder of its own
/// and names that folder; a test that wants none leaves the folder out and the child searches an empty one.
/// </summary>
internal static class AiAssistProcessSupport
{
	/// <summary>The artifact stem the pack files carry, used to find them in the destination.</summary>
	internal const string PACK_SEARCH_PATTERN = AiAssistHarness.PACK_SEARCH_PATTERN;

	/// <summary>An answer that is neither a number nor a name, used where a test checks that the text is never echoed.</summary>
	internal const string HOSTILE_ANSWER = "HOSTILE-ANSWER-9f3a";

	private const string _RUN_DATE_PATTERN = @"\d{4}-\d{2}-\d{2}";
	private const string _MASKED_RUN_DATE = "0000-00-00";

	/// <summary>The four assessment stages in run order, as entered at the prompt.</summary>
	internal static string[] FourStages { get; } =
	[
		CommandRegistry.Describe(CommandId.Scan).Name,
		CommandRegistry.Describe(CommandId.Analyze).Name,
		CommandRegistry.Describe(CommandId.Suggest).Name,
		CommandRegistry.Describe(CommandId.Report).Name
	];

	/// <summary>The command that opens the stage.</summary>
	internal static string AiAssistCommand { get; } = CommandRegistry.Describe(CommandId.AiAssist).Name;

	/// <summary>The command that ends the session.</summary>
	internal static string ExitCommand { get; } = CommandRegistry.Describe(CommandId.Exit).Name;

	/// <summary>Writes the three files, one finding each, through the real analyzer's own inputs.</summary>
	internal static void WriteRepository(RepositoryFixture repository)
	{
		foreach ((string relativePath, string content) in CSharpFlowFixtureRepository.AllFiles)
		{
			repository.WriteText(relativePath, content);
		}
	}

	/// <summary>The commands of a session that runs the four stages, then the given ones.</summary>
	internal static string[] FourStagesThen(params string[] followingLines)
	{
		return [.. FourStages, .. followingLines];
	}

	/// <summary>Runs one session; the fake folder, when given, is the first entry of the child's search path.</summary>
	internal static CliProcessOutcome Run(
		RepositoryFixture repository,
		TemporaryFolder destination,
		IEnumerable<string> lines,
		string? agentFolderPath = null)
	{
		return CliProcessHarness.Run(repository.RootPath, destination.Path, lines, agentFolderPath);
	}

	/// <summary>The prompt packs the destination holds, as bare names in name order.</summary>
	internal static string[] PackNames(TemporaryFolder destination)
	{
		return [.. Directory.GetFiles(destination.Path, PACK_SEARCH_PATTERN).Select(path => Path.GetFileName(path)).Order(StringComparer.Ordinal)];
	}

	/// <summary>The launch folders the destination holds.</summary>
	internal static string[] LaunchFolders(TemporaryFolder destination)
	{
		return Directory.GetDirectories(destination.Path, $"{LaunchCopyLayout.FOLDER_NAME_PREFIX}*");
	}

	/// <summary>The report's bytes.</summary>
	internal static byte[] ReportBytes(TemporaryFolder destination)
	{
		return ProducedArtifacts.BytesOfTheOnly(destination.Path, OutputArtifactKind.MarkdownReport);
	}

	/// <summary>
	/// The report's bytes with the run date, which the report carries in its content, replaced by a fixed text of the
	/// same length. Two sessions run one after the other can straddle midnight and so carry different dates; every
	/// other byte is kept, and the positions are unchanged.
	/// </summary>
	internal static byte[] ReportBytesWithoutRunDate(TemporaryFolder destination)
	{
		string reportName = ProducedArtifacts.NamesOf(destination.Path, OutputArtifactKind.MarkdownReport).Single();
		string runDate = System.Text.RegularExpressions.Regex.Match(reportName, _RUN_DATE_PATTERN).Value;
		string text = System.Text.Encoding.Latin1.GetString(ReportBytes(destination));

		return System.Text.Encoding.Latin1.GetBytes(text.Replace(runDate, _MASKED_RUN_DATE, StringComparison.Ordinal));
	}

	/// <summary>The data artifact's bytes.</summary>
	internal static byte[] DataBytes(TemporaryFolder destination)
	{
		return ProducedArtifacts.BytesOfTheOnly(destination.Path, OutputArtifactKind.StructuredDataArtifact);
	}

	/// <summary>The persisted session state's bytes.</summary>
	internal static byte[] StateBytes(TemporaryFolder destination, RepositoryFixture repository)
	{
		return File.ReadAllBytes(destination.Combine(SessionStateStore.BuildStateFileName(repository.Repository)));
	}

	/// <summary>Whether the fake of the environment recorded that it was started.</summary>
	internal static bool FakeWasStarted(FakeAgentFolder fakes, AgentEnvironment environment)
	{
		string executable = Path.Combine(fakes.FolderPath, FakeAgentFolder.ExecutableName(AgentEnvironmentCatalog.Describe(environment).CommandName));

		return File.Exists($"{executable}{MigrateAuth.FakeAgent.FakeAgentRecord.FILE_SUFFIX}");
	}

	/// <summary>The line the stage prints before it looks for the environment's command.</summary>
	internal static string SearchingLine(AgentEnvironment environment)
	{
		return string.Format(
			System.Globalization.CultureInfo.InvariantCulture,
			AiAssistTexts.SEARCH_FORMAT,
			AgentEnvironmentCatalog.Describe(environment).CommandName);
	}

	/// <summary>The not-found line for the environment's command.</summary>
	internal static string NotFoundLine(AgentEnvironment environment)
	{
		return string.Format(
			System.Globalization.CultureInfo.InvariantCulture,
			AiAssistTexts.NOT_FOUND_FORMAT,
			AgentEnvironmentCatalog.Describe(environment).CommandName);
	}

	/// <summary>The line for a match found only inside the repository.</summary>
	internal static string MatchInsideRepositoryLine(AgentEnvironment environment)
	{
		return string.Format(
			System.Globalization.CultureInfo.InvariantCulture,
			AiAssistTexts.MATCH_INSIDE_REPOSITORY_FORMAT,
			AgentEnvironmentCatalog.Describe(environment).CommandName);
	}

	/// <summary>The number the selection list gives the environment, as the text a user types.</summary>
	internal static string SelectionNumberOf(AgentEnvironment environment)
	{
		return AgentEnvironmentCatalog.Describe(environment).SelectionNumber.ToString(System.Globalization.CultureInfo.InvariantCulture);
	}

	/// <summary>
	/// Asserts that none of the given absolute paths appears in what the session printed. The destination is not
	/// among the callers' arguments: a process is given it as an absolute path, and the console shows a folder as
	/// the user supplied it.
	/// </summary>
	internal static void AssertNoAbsolutePath(CliProcessOutcome outcome, params string[] absolutePaths)
	{
		foreach (string absolutePath in absolutePaths)
		{
			Assert.DoesNotContain(absolutePath, outcome.StandardOutput, StringComparison.OrdinalIgnoreCase);
		}
	}

	/// <summary>The prompt lines of the stage that appear in a transcript: the selection prompt and each environment's confirmation.</summary>
	internal static int CountSelectionPrompts(string transcript)
	{
		return CountOccurrences(transcript, AiAssistTexts.SELECTION_PROMPT);
	}

	/// <summary>How many confirmation questions the transcript holds, over the three environments.</summary>
	internal static int CountConfirmations(string transcript)
	{
		return AgentEnvironmentCatalog.All.Sum(descriptor => CountOccurrences(transcript, AiAssistTexts.Confirmation(descriptor.Environment)));
	}

	internal static int CountOccurrences(string text, string value)
	{
		int count = 0;
		int index = text.IndexOf(value, StringComparison.Ordinal);

		while (index >= 0)
		{
			count++;
			index = text.IndexOf(value, index + value.Length, StringComparison.Ordinal);
		}

		return count;
	}
}
