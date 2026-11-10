using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Findings;
using MigrateAuth.Cli.Pipeline;
using MigrateAuth.Cli.Reporting;
using MigrateAuth.Cli.Session;
using MigrateAuth.Cli.Tests.Fixtures;
using MigrateAuth.Cli.Tests.Reporting;

using Xunit;
using Xunit.Abstractions;

namespace MigrateAuth.Cli.Tests.Pipeline;

/// <summary>
/// The Suggest console prints the three flow counts, a
/// legend giving each note once, and one row per reported High or Medium finding up to a named limit,
/// then a single summary line. Every count is taken over the reported set.
/// </summary>
public sealed class SuggestConsoleTests
{
	private const string _ROWS_HEADING_LINE = "Findings:";
	private const string _SOURCE_PATH_FORMAT = "src/file-{0}.cs";
	private const int _VOLUME_FINDING_COUNT = 50_000;
	private const long _VOLUME_DURATION_CEILING_IN_MILLISECONDS = 30_000;
	private const string _LEGEND_HEADING_LINE = "Flow notes:";

	private readonly ITestOutputHelper _output;

	public SuggestConsoleTests(ITestOutputHelper output)
	{
		_output = output;
	}

	/// <summary>
	/// The Suggest console legend and rows: each note text that occurs appears exactly once, in
	/// the legend, and every finding row carries a key that resolves to the constant for the row's
	/// (decision, flow).
	/// </summary>
	[Fact]
	public void EachNoteTextAppearsOnceInTheLegendAndEveryRowsKeyResolvesToItsConstant()
	{
		ImmutableArray<Finding> findings =
		[
			FindingWith(1, CredentialFlow.ClientCredentials, FlowDecision.Default),
			FindingWith(2, CredentialFlow.ClientCredentials, FlowDecision.FixedSignal),
			FindingWith(3, CredentialFlow.AuthorizationCode, FlowDecision.InteractiveSignal),
			FindingWith(4, CredentialFlow.AuthorizationCode, FlowDecision.Default),
			FindingWith(5, CredentialFlow.ClientCredentials, FlowDecision.Default)
		];

		string console = RunSuggest(findings, ConfidenceLevel.Low);

		foreach (FlowNoteLegendEntry entry in FlowNoteKeys.AllEntries)
		{
			Assert.Equal(1, CountOccurrences(console, entry.Note));
			Assert.Contains($"  {entry.Key} {entry.Note}", console, StringComparison.Ordinal);
		}

		string[] rows = RowLines(console);

		Assert.Equal(findings.Length, rows.Length);

		foreach (Finding finding in findings)
		{
			string expectedKey = FlowNoteKeys.KeyFor(finding.FlowDecision!.Value, finding.Flow!.Value);
			string note = FlowNotes.NoteFor(finding.FlowDecision!.Value, finding.Flow!.Value);
			string row = rows.Single(candidate => candidate.Contains($"  {finding.Identifier}  ", StringComparison.Ordinal));

			Assert.EndsWith(expectedKey, row, StringComparison.Ordinal);
			Assert.Equal(note, FlowNoteKeys.AllEntries.Single(entry => entry.Key == expectedKey).Note);
			Assert.Contains(CredentialFlows.Name(finding.Flow!.Value), row, StringComparison.Ordinal);
			Assert.Contains(CredentialFlows.Name(finding.FlowDecision!.Value), row, StringComparison.Ordinal);
		}
	}

	/// <summary>The legend lists only the notes that occur among the reported findings, and prints nothing else.</summary>
	[Fact]
	public void TheLegendListsOnlyTheNotesThatOccur()
	{
		string console = RunSuggest([FindingWith(1, CredentialFlow.ClientCredentials, FlowDecision.Default)], ConfidenceLevel.Low);

		Assert.Contains(_LEGEND_HEADING_LINE, console, StringComparison.Ordinal);
		Assert.Contains(FlowNotes.DEFAULT_CLIENT_CREDENTIALS_NOTE, console, StringComparison.Ordinal);
		Assert.DoesNotContain(FlowNotes.INTERACTIVE_SIGNAL_AUTHORIZATION_CODE_NOTE, console, StringComparison.Ordinal);
		Assert.DoesNotContain(FlowNotes.FIXED_SIGNAL_CLIENT_CREDENTIALS_NOTE, console, StringComparison.Ordinal);
		Assert.DoesNotContain(FlowNotes.DEFAULT_AUTHORIZATION_CODE_NOTE, console, StringComparison.Ordinal);
	}

	/// <summary>The key of every (decision, flow) pair resolves, and the four keys are distinct.</summary>
	[Fact]
	public void EveryDecisionAndFlowPairHasAKeyAndTheKeysAreDistinct()
	{
		HashSet<string> keys = [];

		foreach ((FlowDecision decision, CredentialFlow flow) in new[]
		{
			(FlowDecision.Default, CredentialFlow.ClientCredentials),
			(FlowDecision.Default, CredentialFlow.AuthorizationCode),
			(FlowDecision.FixedSignal, CredentialFlow.ClientCredentials),
			(FlowDecision.InteractiveSignal, CredentialFlow.AuthorizationCode)
		})
		{
			Assert.True(keys.Add(FlowNoteKeys.KeyFor(decision, flow)));
		}

		Assert.Equal(FlowNoteKeys.AllEntries.Length, keys.Count);
	}

	/// <summary>With exactly the limit's worth of findings there are that many rows and no summary line.</summary>
	[Fact]
	public void AtTheLimitEveryFindingHasARowAndThereIsNoSummaryLine()
	{
		string console = RunSuggest(ManyDefaultClientCredentialsFindings(SuggestStage.SUGGEST_CONSOLE_FINDING_ROW_LIMIT), ConfidenceLevel.Low);

		Assert.Equal(100, SuggestStage.SUGGEST_CONSOLE_FINDING_ROW_LIMIT);
		Assert.Equal(SuggestStage.SUGGEST_CONSOLE_FINDING_ROW_LIMIT, RowLines(console).Length);
		Assert.DoesNotContain("further findings have no row", console, StringComparison.Ordinal);
	}

	/// <summary>
	/// Past the limit exactly the limit's worth of rows are printed, then one summary line giving
	/// the unrowed count per (decision, flow), in a fixed order, and pointing to the report.
	/// </summary>
	[Fact]
	public void PastTheLimitThereAreExactlyTheLimitOfRowsAndOneSummaryLineWithTheCountPerDecisionAndFlow()
	{
		List<Finding> findings = [.. ManyDefaultClientCredentialsFindings(SuggestStage.SUGGEST_CONSOLE_FINDING_ROW_LIMIT)];

		for (int index = 0; index < 30; index++)
		{
			findings.Add(FindingWith(findings.Count + 1, CredentialFlow.ClientCredentials, FlowDecision.FixedSignal));
		}

		for (int index = 0; index < 20; index++)
		{
			findings.Add(FindingWith(findings.Count + 1, CredentialFlow.AuthorizationCode, FlowDecision.InteractiveSignal));
		}

		string console = RunSuggest([.. findings], ConfidenceLevel.Low);
		string[] summaryLines = console.Split(Environment.NewLine).Where(line => line.Contains("further findings have no row", StringComparison.Ordinal)).ToArray();

		Assert.Equal(SuggestStage.SUGGEST_CONSOLE_FINDING_ROW_LIMIT, RowLines(console).Length);
		Assert.Single(summaryLines);
		Assert.Contains("50 further findings", summaryLines[0], StringComparison.Ordinal);
		Assert.Contains(
			$"20 AuthorizationCode InteractiveSignal {FlowNoteKeys.INTERACTIVE_SIGNAL_AUTHORIZATION_CODE_KEY}; 30 ClientCredentials FixedSignal {FlowNoteKeys.FIXED_SIGNAL_CLIENT_CREDENTIALS_KEY}",
			summaryLines[0],
			StringComparison.Ordinal);
		Assert.Contains("The report lists every finding", summaryLines[0], StringComparison.Ordinal);
		Assert.Contains(FlowNotes.INTERACTIVE_SIGNAL_AUTHORIZATION_CODE_NOTE, console, StringComparison.Ordinal);
	}

	/// <summary>The same state prints byte-identical output, the summary line's order included.</summary>
	[Fact]
	public void TheSameStatePrintsTheSameConsoleOutput()
	{
		List<Finding> findings = [.. ManyDefaultClientCredentialsFindings(SuggestStage.SUGGEST_CONSOLE_FINDING_ROW_LIMIT)];

		findings.Add(FindingWith(findings.Count + 1, CredentialFlow.AuthorizationCode, FlowDecision.InteractiveSignal));
		findings.Add(FindingWith(findings.Count + 1, CredentialFlow.ClientCredentials, FlowDecision.FixedSignal));

		Assert.Equal(RunSuggest([.. findings], ConfidenceLevel.Low), RunSuggest([.. findings], ConfidenceLevel.Low));
	}

	/// <summary>
	/// VOLUME EVIDENCE. The row-limit behaviour at fifty thousand findings,
	/// in-process: one hundred rows, one summary line, and the duration recorded in the test output.
	/// </summary>
	[Fact]
	public void FiftyThousandFindingsPrintTheLimitOfRowsAndOneSummaryLineAndTheDurationIsRecorded()
	{
		Finding[] findings = new Finding[_VOLUME_FINDING_COUNT];

		for (int index = 0; index < findings.Length; index++)
		{
			findings[index] = FindingWith(index + 1, CredentialFlow.ClientCredentials, index % 2 == 0 ? FlowDecision.Default : FlowDecision.FixedSignal);
		}

		ImmutableArray<Finding> findingSet = [.. findings];
		Stopwatch stopwatch = Stopwatch.StartNew();
		string console = RunSuggest(findingSet, ConfidenceLevel.Low);

		stopwatch.Stop();
		_output.WriteLine(string.Format(
			CultureInfo.InvariantCulture,
			"Suggest over {0:N0} findings: {1:N0} ms, {2:N0} console rows, {3:N0} console characters.",
			_VOLUME_FINDING_COUNT,
			stopwatch.ElapsedMilliseconds,
			RowLines(console).Length,
			console.Length));

		Assert.Equal(SuggestStage.SUGGEST_CONSOLE_FINDING_ROW_LIMIT, RowLines(console).Length);
		Assert.Equal(1, CountOccurrences(console, "further findings have no row"));
		Assert.Contains("49,900 further findings", console, StringComparison.Ordinal);
		Assert.True(
			stopwatch.ElapsedMilliseconds < _VOLUME_DURATION_CEILING_IN_MILLISECONDS,
			$"Suggest over {_VOLUME_FINDING_COUNT:N0} findings took {stopwatch.ElapsedMilliseconds:N0} ms.");
	}

	/// <summary>
	/// Client Credentials plus Authorization Code equals the High and Medium findings the
	/// report lists after the threshold filter, defaulted is at most that total, and the route counts are
	/// taken over the same set: a Low finding below the threshold is in no count.
	/// </summary>
	[Fact]
	public void TheCountsAreTakenOverTheReportedSetAfterTheThresholdFilter()
	{
		ImmutableArray<Finding> findings =
		[
			FindingWith(1, CredentialFlow.ClientCredentials, FlowDecision.Default, ReportHarness.TEMPLATED_PATTERN_CLASS, ConfidenceLevel.High),
			FindingWith(2, CredentialFlow.AuthorizationCode, FlowDecision.InteractiveSignal, ReportHarness.TEMPLATED_PATTERN_CLASS, ConfidenceLevel.Medium),
			FindingWith(3, CredentialFlow.ClientCredentials, FlowDecision.FixedSignal, ReportHarness.UNTEMPLATED_PATTERN_CLASS, ConfidenceLevel.High),
			ReportHarness.FindingAt(4, "src/low.cs", 4, ReportHarness.TEMPLATED_PATTERN_CLASS, ConfidenceLevel.Low)
		];

		string atLow = RunSuggest(findings, ConfidenceLevel.Low);
		string atMedium = RunSuggest(findings, ConfidenceLevel.Medium);
		string atHigh = RunSuggest(findings, ConfidenceLevel.High);

		Assert.Contains("suggest: recommendations resolved: 1 from a catalog template, 2 generic, 1 routed to manual review.", atLow, StringComparison.Ordinal);
		Assert.Contains(new FlowCounts(2, 1, 1).Describe(), atLow, StringComparison.Ordinal);

		Assert.Contains("suggest: recommendations resolved: 1 from a catalog template, 2 generic, 0 routed to manual review.", atMedium, StringComparison.Ordinal);
		Assert.Contains(new FlowCounts(2, 1, 1).Describe(), atMedium, StringComparison.Ordinal);

		Assert.Contains("suggest: recommendations resolved: 1 from a catalog template, 1 generic, 0 routed to manual review.", atHigh, StringComparison.Ordinal);
		Assert.Contains(new FlowCounts(2, 0, 1).Describe(), atHigh, StringComparison.Ordinal);

		FlowCounts reported = FlowCounts.Over(ThresholdFilter.Apply(findings, ConfidenceLevel.Medium));

		Assert.Equal(3, reported.Total);
		Assert.True(reported.Defaulted <= reported.Total);
	}

	/// <summary>
	/// Supporting. An in-process session over the real pipeline with one
	/// interactive-signal file and one no-signal file shows both flows and both notes in the Suggest
	/// console and in the report, and the report's three counts equal the console's.
	/// </summary>
	[Fact]
	public void AnInteractiveFileAndANoSignalFileShowBothFlowsAndBothNotesInTheConsoleAndTheReportWithEqualCounts()
	{
		using PipelineHarness harness = PipelineHarness.CreateWithFlowSignals([FlowSignalFixtures.InteractiveCodeOnly, FlowSignalFixtures.FixedCodeOnly]);

		harness.WriteSourceFile("Interactive.cs", SourceWith(FlowSignalFixtures.INTERACTIVE_TOKEN));
		harness.WriteSourceFile("Plain.cs", SourceWith(token: null));
		harness.RunAndCommit(CommandId.Scan);
		harness.RunAndCommit(CommandId.Analyze);
		harness.RunAndCommit(CommandId.Suggest);
		harness.RunAndCommit(CommandId.Report);

		string console = harness.Transcript;
		string report = harness.ReadWrittenArtifact(MigrateAuth.Cli.Writing.OutputArtifactKind.MarkdownReport);
		FlowCounts expected = new(ClientCredentials: 1, AuthorizationCode: 1, Defaulted: 1);

		Assert.Contains(expected.Describe(), console, StringComparison.Ordinal);
		Assert.Contains(expected.Describe(), report, StringComparison.Ordinal);
		Assert.Contains(CredentialFlows.CLIENT_CREDENTIALS_NAME, console, StringComparison.Ordinal);
		Assert.Contains(CredentialFlows.AUTHORIZATION_CODE_NAME, console, StringComparison.Ordinal);
		Assert.Equal(1, CountOccurrences(console, FlowNotes.DEFAULT_CLIENT_CREDENTIALS_NOTE));
		Assert.Equal(1, CountOccurrences(console, FlowNotes.INTERACTIVE_SIGNAL_AUTHORIZATION_CODE_NOTE));
		Assert.Contains($"- Flow note: {FlowNotes.DEFAULT_CLIENT_CREDENTIALS_NOTE}", report, StringComparison.Ordinal);
		Assert.Contains($"- Flow note: {FlowNotes.INTERACTIVE_SIGNAL_AUTHORIZATION_CODE_NOTE}", report, StringComparison.Ordinal);
		Assert.Contains($"- Flow: {CredentialFlows.AUTHORIZATION_CODE_NAME} (decision: {CredentialFlows.INTERACTIVE_SIGNAL_NAME})", report, StringComparison.Ordinal);
		Assert.Contains($"- Flow: {CredentialFlows.CLIENT_CREDENTIALS_NAME} (decision: {CredentialFlows.DEFAULT_DECISION_NAME})", report, StringComparison.Ordinal);
	}

	private string RunSuggest(ImmutableArray<Finding> findings, ConfidenceLevel minimumLevel)
	{
		LanguageCatalog catalog = ReportHarness.CatalogWithCSharpOnly();
		SessionState state = SessionState.Fresh(catalog.Version, ReportHarness.REPOSITORY_LABEL);

		state.RecordAnalysisOutcome(findings, FindingsCapOutcome.Empty, []);

		StringWriter output = new();
		SuggestStage stage = new(new SuggestStageContext
		{
			Catalog = catalog,
			GenericRecommendations = new GenericRecommendationProvider(),
			Configuration = ReportHarness.ConfigurationWith(minimumLevel),
			Output = output
		});

		Assert.Equal(StageRunOutcome.Completed, stage.Run(state, CancellationToken.None));

		return output.ToString();
	}

	private static ImmutableArray<Finding> ManyDefaultClientCredentialsFindings(int count)
	{
		ImmutableArray<Finding>.Builder findings = ImmutableArray.CreateBuilder<Finding>(count);

		for (int index = 1; index <= count; index++)
		{
			findings.Add(FindingWith(index, CredentialFlow.ClientCredentials, FlowDecision.Default));
		}

		return findings.MoveToImmutable();
	}

	private static Finding FindingWith(
		int sequentialIndex,
		CredentialFlow flow,
		FlowDecision decision,
		string patternClass = ReportHarness.TEMPLATED_PATTERN_CLASS,
		ConfidenceLevel level = ConfidenceLevel.High)
	{
		return ReportHarness.FindingAt(
			sequentialIndex,
			string.Format(CultureInfo.InvariantCulture, _SOURCE_PATH_FORMAT, sequentialIndex),
			sequentialIndex,
			patternClass,
			level,
			flow: flow,
			flowDecision: decision);
	}

	private static string[] RowLines(string console)
	{
		string[] lines = console.Split(Environment.NewLine);
		int headingIndex = Array.IndexOf(lines, _ROWS_HEADING_LINE);

		return headingIndex < 0
			? []
			: lines.Skip(headingIndex + 1).TakeWhile(line => line.StartsWith("  ", StringComparison.Ordinal)).ToArray();
	}

	private static int CountOccurrences(string text, string value)
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

	private static string SourceWith(string? token)
	{
		string tokenLine = token is null ? string.Empty : $"\tvoid Signal() => {token}();\n";

		return $"class Sample\n{{\n{tokenLine}\tvoid Build() => {PipelineLanguageAnalyzer.TEMPLATED_CONSTRUCT}(user, \"a-literal-secret\");\n}}\n";
	}
}
