using System.Collections.Immutable;
using System.Globalization;
using System.Text;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Findings;
using MigrateAuth.Cli.Pipeline;
using MigrateAuth.Cli.Reading;
using MigrateAuth.Cli.Reporting;
using MigrateAuth.Cli.Session;

using Xunit;

namespace MigrateAuth.Cli.Tests.Pipeline;

/// <summary>
/// The stage as the session runs it: what it puts on the state, how it ends, and what it refuses to
/// leave behind when it is interrupted.
/// </summary>
public sealed class AnalyzeStageTests
{
	private const string _CREDENTIAL_FILE_NAME = "Credentials.cs";
	private const string _SECOND_FILE_NAME = "Headers.cs";
	private const string _ORDINARY_FILE_NAME = "Ordinary.cs";
	private const string _UNCLAIMED_EXTENSION_FILE_NAME = "stray.unclaimed";
	private const string _PERSISTED_LANGUAGE_NAME = "CSharp";
	private const string _UNDECLARED_LANGUAGE_NAME = "NotDeclaredByAnyAnalyzer";

	private const string _ORDINARY_CONTENT = "public sealed class Ordinary\n{\n}\n";

	private static readonly string _credentialContent =
		$"public sealed class Credentials\n{{\n\tvoid Build() => {PipelineLanguageAnalyzer.TEMPLATED_CONSTRUCT}(user, \"a-literal-secret\");\n}}\n";

	private static readonly string _lowConfidenceContent =
		$"public sealed class Legacy\n{{\n\tvoid Build() => {PipelineLanguageAnalyzer.LOW_CONSTRUCT}();\n}}\n";

	private static readonly string _untemplatedContent =
		$"public sealed class Headers\n{{\n\tvoid Build() => {PipelineLanguageAnalyzer.UNTEMPLATED_CONSTRUCT}();\n}}\n";

	[Fact]
	public void AnAnalysisOverACleanTreeCompletes()
	{
		using PipelineHarness harness = PipelineHarness.Create();
		harness.WriteSourceFile(_ORDINARY_FILE_NAME, _ORDINARY_CONTENT);

		harness.RunAndCommit(CommandId.Scan);

		Assert.Equal(StageRunOutcome.Completed, harness.Run(CommandId.Analyze));
		Assert.Empty(harness.State.Findings);
	}

	/// <summary>The third part of the data invariant reaching the session state.</summary>
	[Fact]
	public void TheFindingSetReachesTheSessionState()
	{
		using PipelineHarness harness = PipelineHarness.Create();
		harness.WriteSourceFile(_CREDENTIAL_FILE_NAME, _credentialContent);
		harness.WriteSourceFile(_ORDINARY_FILE_NAME, _ORDINARY_CONTENT);

		harness.RunAndCommit(CommandId.Scan);
		harness.RunAndCommit(CommandId.Analyze);

		Finding finding = Assert.Single(harness.State.Findings);

		Assert.Equal(PipelineLanguageAnalyzer.TEMPLATED_PATTERN_CLASS, finding.PatternClass.Value);
		Assert.Equal(AssessedLanguage.CSharp, finding.Language);
		Assert.Equal(ConfidenceLevel.High, finding.Level);
		Assert.Equal(_CREDENTIAL_FILE_NAME, finding.Path.Value);
	}

	/// <summary>
	/// The invariant's phrase is "findings WITHOUT rendered recommendations", and this is the check that it
	/// holds structurally rather than by discipline: <see cref="Finding"/> exposes no member that
	/// could carry rendered text, so a recommendation cannot be persisted even by mistake. The
	/// assertion is over the type's own surface rather than over one instance's values, because a
	/// field added later would slip past a value check.
	/// </summary>
	[Fact]
	public void NoFindingCarriesARenderedRecommendation()
	{
		using PipelineHarness harness = PipelineHarness.Create();
		harness.WriteSourceFile(_CREDENTIAL_FILE_NAME, _credentialContent);

		harness.RunAndCommit(CommandId.Scan);
		harness.RunAndCommit(CommandId.Analyze);

		Assert.NotEmpty(harness.State.Findings);

		IEnumerable<string> memberNames = typeof(Finding)
			.GetProperties()
			.Select(property => property.Name);

		Assert.DoesNotContain(memberNames, name =>
			name.Contains("Recommendation", StringComparison.OrdinalIgnoreCase)
			|| name.Contains("Template", StringComparison.OrdinalIgnoreCase)
			|| name.Contains("Narrative", StringComparison.OrdinalIgnoreCase));
	}

	/// <summary>
	/// The set is ordered on the total key and numbered over the whole of it, with
	/// no gaps. The stage does not do that itself - composition does - and this is the check that
	/// the stage hands composition the whole set rather than numbering per file.
	/// </summary>
	[Fact]
	public void FindingsAcrossFilesAreNumberedOverTheWholeOrderedSet()
	{
		using PipelineHarness harness = PipelineHarness.Create();
		harness.WriteSourceFile(_SECOND_FILE_NAME, _untemplatedContent);
		harness.WriteSourceFile(_CREDENTIAL_FILE_NAME, _credentialContent);

		harness.RunAndCommit(CommandId.Scan);
		harness.RunAndCommit(CommandId.Analyze);

		ImmutableArray<Finding> findings = harness.State.Findings;

		Assert.Equal(2, findings.Length);
		Assert.Equal([1, 2], findings.Select(finding => finding.SequentialIndex));
		Assert.Equal([_CREDENTIAL_FILE_NAME, _SECOND_FILE_NAME], findings.Select(finding => finding.Path.Value));
	}

	/// <summary>
	/// An interrupted stage leaves nothing partial behind. A cancelled analysis must not
	/// half-replace the set an earlier one produced, and must not record the part it reached.
	/// </summary>
	[Fact]
	public void ACancelledAnalysisRecordsNothing()
	{
		using PipelineHarness harness = PipelineHarness.Create();
		harness.WriteSourceFile(_CREDENTIAL_FILE_NAME, _credentialContent);

		harness.RunAndCommit(CommandId.Scan);

		using CancellationTokenSource cancellation = new();

		cancellation.Cancel();

		StageRunOutcome outcome = harness.Run(CommandId.Analyze, cancellation.Token);

		Assert.Equal(StageRunOutcome.Cancelled, outcome);
		Assert.Empty(harness.State.Findings);
	}

	/// <summary>
	/// A cancelled analysis leaves the PREVIOUS run's set intact, which is the other half of the
	/// same rule - "the session stays intact, the stage is not completed, and nothing upstream is
	/// invalidated".
	/// </summary>
	[Fact]
	public void ACancelledAnalysisLeavesAnEarlierSetInPlace()
	{
		using PipelineHarness harness = PipelineHarness.Create();
		harness.WriteSourceFile(_CREDENTIAL_FILE_NAME, _credentialContent);

		harness.RunAndCommit(CommandId.Scan);
		harness.RunAndCommit(CommandId.Analyze);

		ImmutableArray<Finding> completedSet = harness.State.Findings;

		using CancellationTokenSource cancellation = new();

		cancellation.Cancel();
		harness.Run(CommandId.Analyze, cancellation.Token);

		Assert.Equal(completedSet, harness.State.Findings);
	}

	/// <summary>
	/// The terms <c>RecordScanOutcome</c> already established for the candidate set: a second run
	/// replaces rather than appends. Here it matters more than there - the numbering runs over the whole
	/// set, so a union would carry two findings numbered 001.
	/// </summary>
	[Fact]
	public void ASecondAnalysisReplacesTheFindingSet()
	{
		using PipelineHarness harness = PipelineHarness.Create();
		harness.WriteSourceFile(_CREDENTIAL_FILE_NAME, _credentialContent);

		harness.RunAndCommit(CommandId.Scan);
		harness.RunAndCommit(CommandId.Analyze);
		harness.RunAndCommit(CommandId.Analyze);

		Finding finding = Assert.Single(harness.State.Findings);

		Assert.Equal(FindingIdentifiers.Of(FindingIdentifiers.FIRST_INDEX), finding.Identifier);
	}

	/// <summary>
	/// THE TENTH SKIPPED-ITEM CATEGORY, RAISED BY THIS STAGE.
	///
	/// <c>RepositoryFileReader</c> declines to raise it in its own doc comment, because the
	/// situation belongs to the persisted candidate set rather than to the walk. A candidate deleted
	/// between <c>scan</c> and <c>analyze</c> is exactly that situation, and without this it would
	/// vanish from the assessment with nothing counting it.
	/// </summary>
	[Fact]
	public void ACandidateThatVanishedAfterTheScanIsCountedAsMissing()
	{
		using PipelineHarness harness = PipelineHarness.Create();
		harness.WriteSourceFile(_CREDENTIAL_FILE_NAME, _credentialContent);

		harness.RunAndCommit(CommandId.Scan);
		harness.DeleteSourceFile(_CREDENTIAL_FILE_NAME);
		harness.RunAndCommit(CommandId.Analyze);

		SkippedItemTally tally = harness.SkippedItems.TallyOf(SkippedItemCategory.MissingCandidate);

		Assert.Equal(1, tally.Count);
		Assert.Equal([_CREDENTIAL_FILE_NAME], tally.EnumeratedPaths.Select(path => path.Value));
		Assert.Empty(harness.State.Findings);
		Assert.Contains("no longer present: 1", harness.Transcript, StringComparison.Ordinal);
	}

	/// <summary>
	/// THE RECONCILIATION LINE. Said once, at the end of <c>analyze</c>, only because this
	/// run actually skipped something - a candidate the scan recorded and analysis could no longer
	/// open.
	/// </summary>
	[Fact]
	public void AnalysisPrintsItsOwnSkipLineWhenItSkippedSomething()
	{
		using PipelineHarness harness = PipelineHarness.Create();
		harness.WriteSourceFile(_CREDENTIAL_FILE_NAME, _credentialContent);

		harness.RunAndCommit(CommandId.Scan);
		harness.DeleteSourceFile(_CREDENTIAL_FILE_NAME);
		harness.RunAndCommit(CommandId.Analyze);

		Assert.Contains(
			"Skipped during analysis: 1 files. The report lists skipped items from both stages.",
			harness.Transcript,
			StringComparison.Ordinal);
	}

	/// <summary>The complement: an analysis that skipped nothing says nothing about skipping.</summary>
	[Fact]
	public void AnalysisSaysNothingAboutSkippingWhenItSkippedNothing()
	{
		using PipelineHarness harness = PipelineHarness.Create();
		harness.WriteSourceFile(_CREDENTIAL_FILE_NAME, _credentialContent);

		harness.RunAndCommit(CommandId.Scan);
		harness.RunAndCommit(CommandId.Analyze);

		Assert.DoesNotContain("Skipped during analysis", harness.Transcript, StringComparison.Ordinal);
	}

	/// <summary>
	/// The ledger is cleared at the start of every Analyze run, so a second run over the same
	/// missing candidate reports it once more rather than adding to an earlier run's count - without
	/// the clear, a second analysis over an unchanged tree would read 2 rather than 1.
	/// </summary>
	[Fact]
	public void ASecondAnalysisDoesNotDoubleTheSkippedCount()
	{
		using PipelineHarness harness = PipelineHarness.Create();
		harness.WriteSourceFile(_CREDENTIAL_FILE_NAME, _credentialContent);

		harness.RunAndCommit(CommandId.Scan);
		harness.DeleteSourceFile(_CREDENTIAL_FILE_NAME);
		harness.RunAndCommit(CommandId.Analyze);
		harness.RunAndCommit(CommandId.Analyze);

		SkippedItemTally tally = harness.SkippedItems.TallyOf(SkippedItemCategory.MissingCandidate);

		Assert.Equal(1, tally.Count);
		Assert.Equal(1, harness.State.AnalysisSkippedItems.Single(t => t.Category == SkippedItemCategory.MissingCandidate).Count);
	}

	/// <summary>
	/// The literal-credential signal, established from the ZONE of the credential argument. It
	/// travels the whole pipeline here rather than being asserted inside recognition, because the
	/// signal is what the fifth piece of the recommendation rides on and this test is what connects the two.
	/// </summary>
	[Fact]
	public void ALiteralCredentialInSourceSetsTheSignalOnTheFinding()
	{
		using PipelineHarness harness = PipelineHarness.Create();
		harness.WriteSourceFile(_CREDENTIAL_FILE_NAME, _credentialContent);

		harness.RunAndCommit(CommandId.Scan);
		harness.RunAndCommit(CommandId.Analyze);

		Assert.True(Assert.Single(harness.State.Findings).LiteralCredentialSignal);
	}

	/// <summary>
	/// The Low class, which exists here so that the Suggest stage's manual-review route has
	/// something to route. It is a finding like any other: classification assigns it a level and nothing
	/// filters it out of the analysis.
	/// </summary>
	[Fact]
	public void ALowConfidenceConstructIsStillAFinding()
	{
		using PipelineHarness harness = PipelineHarness.Create();
		harness.WriteSourceFile(_ORDINARY_FILE_NAME, _lowConfidenceContent);

		harness.RunAndCommit(CommandId.Scan);
		harness.RunAndCommit(CommandId.Analyze);

		Assert.Equal(ConfidenceLevel.Low, Assert.Single(harness.State.Findings).Level);
	}

	/// <summary>The start line agrees in number: one candidate is "1 candidate file".</summary>
	[Fact]
	public void TheStartLineAgreesInNumberForASingleCandidate()
	{
		using PipelineHarness harness = PipelineHarness.Create();
		harness.WriteSourceFile(_CREDENTIAL_FILE_NAME, _credentialContent);

		harness.RunAndCommit(CommandId.Scan);
		harness.Run(CommandId.Analyze);

		Assert.Contains("analyze: analyzing 1 candidate file.", harness.Transcript, StringComparison.Ordinal);
		Assert.DoesNotContain("1 candidate files", harness.Transcript, StringComparison.Ordinal);
	}

	/// <summary>
	/// THE EMPTY CASE at this stage: no candidates is not an error, and the stage says what it saw
	/// rather than reporting a count over nothing.
	/// </summary>
	[Fact]
	public void AnEmptyCandidateSetCompletesAndSaysSo()
	{
		using PipelineHarness harness = PipelineHarness.Create();

		harness.RunAndCommit(CommandId.Scan);

		Assert.Equal(StageRunOutcome.Completed, harness.Run(CommandId.Analyze));
		Assert.Contains(AnalyzeStage.NO_CANDIDATES_MESSAGE, harness.Transcript, StringComparison.Ordinal);
	}

	/// <summary>
	/// THE PER-FILE FINDINGS CAP, EXERCISED THROUGH THE REAL PIPELINE RATHER THAN THROUGH A SYNTHETIC FIXTURE.
	///
	/// One real file, scanned and recognized through the actual reader, scanner and recognition
	/// engine, offering more matches than the per-file cap allows. What is under test here is the
	/// WIRING - that the stage calls the capped composition, records findings already reduced by it,
	/// and leaves the truncation counter where the Report stage can read it one command later - and
	/// not the cap's own arithmetic, which <c>FindingsCapTests</c> already covers at both caps.
	/// </summary>
	[Fact]
	public void ARealFileOverThePerFileCapIsTruncatedAndTheOutcomeReachesTheHolder()
	{
		using PipelineHarness harness = PipelineHarness.Create();
		int offeredCount = FindingsCapPolicy.PROVISIONAL_PER_FILE_FINDINGS_CAP + 500;

		harness.WriteSourceFile(_ORDINARY_FILE_NAME, ManyUntemplatedConstructCalls(offeredCount));

		harness.RunAndCommit(CommandId.Scan);

		Assert.Equal(StageRunOutcome.Completed, harness.Run(CommandId.Analyze));

		Assert.Equal(FindingsCapPolicy.PROVISIONAL_PER_FILE_FINDINGS_CAP, harness.State.Findings.Length);
		Assert.True(harness.State.FindingsCapOutcome.IsTruncated);
		Assert.Equal(1, harness.State.FindingsCapOutcome.PerFileCapTrippedFileCount);
		Assert.Equal(500, harness.State.FindingsCapOutcome.PerFileOmittedCount);
		Assert.False(harness.State.FindingsCapOutcome.GlobalCapTripped);
		Assert.Contains("Findings capped:", harness.Transcript, StringComparison.Ordinal);
	}

	/// <summary>A run nowhere near either cap leaves the holder at its untripped default.</summary>
	[Fact]
	public void AnOrdinaryAnalysisLeavesTheFindingsCapOutcomeUntripped()
	{
		using PipelineHarness harness = PipelineHarness.Create();
		harness.WriteSourceFile(_CREDENTIAL_FILE_NAME, _credentialContent);

		harness.RunAndCommit(CommandId.Scan);
		harness.RunAndCommit(CommandId.Analyze);

		Assert.False(harness.State.FindingsCapOutcome.IsTruncated);
		Assert.DoesNotContain("Findings capped:", harness.Transcript, StringComparison.Ordinal);
	}

	/// <summary>One file carrying more untemplated-construct calls than the supplied count, one per line.</summary>
	private static string ManyUntemplatedConstructCalls(int count)
	{
		StringBuilder builder = new();

		builder.AppendLine("public sealed class ManyConstructs");
		builder.AppendLine("{");
		builder.AppendLine("\tvoid Build()");
		builder.AppendLine("\t{");

		for (int index = 0; index < count; index++)
		{
			builder.AppendLine($"\t\t{PipelineLanguageAnalyzer.UNTEMPLATED_CONSTRUCT}();");
		}

		builder.AppendLine("\t}");
		builder.AppendLine("}");

		return builder.ToString();
	}

	/// <summary>
	/// Analysis uses the first-line attribution Scan persisted for each candidate and
	/// attributes nothing itself. The candidate below has an extension no analyzer claims and a first
	/// line no first-line rule matches, so a fresh attribution would leave it unrecognized and skip
	/// it; the persisted attribution names the analyzed language, and the file is analyzed.
	/// </summary>
	[Fact]
	public void AnalysisAnalyzesACandidateUnderTheAttributionScanPersistedAndAttributesNothingItself()
	{
		using PipelineHarness harness = PipelineHarness.Create();
		harness.WriteSourceFile(_UNCLAIMED_EXTENSION_FILE_NAME, _credentialContent);
		RecordCandidateAttribution(harness, _UNCLAIMED_EXTENSION_FILE_NAME, _PERSISTED_LANGUAGE_NAME);

		StageRunOutcome outcome = harness.Run(CommandId.Analyze);

		Finding finding = Assert.Single(harness.State.Findings);

		Assert.Equal(StageRunOutcome.Completed, outcome);
		Assert.Equal(_UNCLAIMED_EXTENSION_FILE_NAME, finding.Path.Value);
		Assert.Equal(AssessedLanguage.CSharp, finding.Language);
	}

	/// <summary>
	/// The converse of the test above, which is what shows the persisted value is the only input: a
	/// candidate whose extension an analyzer claims, persisted under a name no analyzer declares, is
	/// not analyzed - Analysis does not fall back to attributing it from its extension.
	/// </summary>
	[Fact]
	public void AnalysisDoesNotAttributeACandidateWhoseExtensionAnAnalyzerClaimsWhenScanPersistedAnotherName()
	{
		using PipelineHarness harness = PipelineHarness.Create();
		harness.WriteSourceFile(_CREDENTIAL_FILE_NAME, _credentialContent);
		RecordCandidateAttribution(harness, _CREDENTIAL_FILE_NAME, _UNDECLARED_LANGUAGE_NAME);

		StageRunOutcome outcome = harness.Run(CommandId.Analyze);

		Assert.Equal(StageRunOutcome.Completed, outcome);
		Assert.Empty(harness.State.Findings);
	}

	/// <summary>
	/// The other end of that: the name a candidate carries after a real Scan is the one that
	/// attributed it, and it survives a save and a load unchanged.
	/// </summary>
	[Fact]
	public void ScanPersistsTheAttributionOfEachCandidateAndItSurvivesAReload()
	{
		using PipelineHarness harness = PipelineHarness.Create();
		harness.WriteSourceFile(_CREDENTIAL_FILE_NAME, _credentialContent);
		harness.WriteSourceFile(_SECOND_FILE_NAME, _untemplatedContent);

		harness.RunAndCommit(CommandId.Scan);

		SessionStateLoad reloaded = harness.ReloadState();

		Assert.Equal([_CREDENTIAL_FILE_NAME, _SECOND_FILE_NAME], harness.State.CandidateRelativePaths);
		Assert.Equal([_PERSISTED_LANGUAGE_NAME, _PERSISTED_LANGUAGE_NAME], harness.State.CandidateLanguageNames);
		Assert.True(reloaded.WasResumed);
		Assert.Equal(harness.State.CandidateRelativePaths, reloaded.State.CandidateRelativePaths);
		Assert.Equal(harness.State.CandidateLanguageNames, reloaded.State.CandidateLanguageNames);
	}

	private static void RecordCandidateAttribution(PipelineHarness harness, string relativePath, string languageName)
	{
		harness.State.RecordScanOutcome(
			[relativePath],
			[languageName],
			ScanSummary.Empty,
			SkippedItemTallies.Empty,
			[]);
	}

	/// <summary>
	/// A catalog whose analyzer declares <c>ExemplarOne</c>, which is not one of the eight, so a file full of its constructs is read,
	/// counted and preselected and still produces no finding - because a finding must carry an
	/// <see cref="AssessedLanguage"/> and there is none to give it. It is declared unsupported
	/// coverage rather than a hole in the walk, so nothing is recorded as skipped.
	/// </summary>
	[Fact]
	public void AnAnalyzerWhoseLanguageIsNotOneOfTheEightProducesNoFinding()
	{
		using PipelineHarness harness = PipelineHarness.CreateOverTheExemplarFixtureCatalog();
		harness.WriteSourceFile("credentials.ex1", "var credentials = NewExemplarCredentials(user, secret);\n");

		harness.RunAndCommit(CommandId.Scan);
		harness.RunAndCommit(CommandId.Analyze);

		Assert.NotEmpty(harness.State.CandidateRelativePaths);
		Assert.Empty(harness.State.Findings);
		Assert.Equal(0, harness.SkippedItems.TallyOf(SkippedItemCategory.MissingCandidate).Count);
	}

	/// <summary>
	/// The confidence summary is said over the FULL composed set - not the one
	/// the confidence threshold admits - and it is said even when every count is zero, which is the empty
	/// case at this line.
	/// </summary>
	[Fact]
	public void AnAnalysisWithNoFindingsStillPrintsTheConfidenceTotals()
	{
		using PipelineHarness harness = PipelineHarness.Create();
		harness.WriteSourceFile(_ORDINARY_FILE_NAME, _ORDINARY_CONTENT);

		harness.RunAndCommit(CommandId.Scan);
		harness.RunAndCommit(CommandId.Analyze);

		Assert.Contains(
			string.Format(CultureInfo.InvariantCulture, AnalyzeConsoleReport.FINDINGS_BY_CONFIDENCE_MESSAGE_FORMAT, 0, 0, 0),
			harness.Transcript,
			StringComparison.Ordinal);
		Assert.DoesNotContain(ConsoleFindingsTable.ColumnHeadings[1], harness.Transcript, StringComparison.Ordinal);
	}

	/// <summary>The confidence summary, over a set that actually carries a High and a Low finding.</summary>
	[Fact]
	public void TheConfidenceTotalsCountEveryLevelInTheFullSet()
	{
		using PipelineHarness harness = PipelineHarness.Create();
		harness.WriteSourceFile(_CREDENTIAL_FILE_NAME, _credentialContent);
		harness.WriteSourceFile(_SECOND_FILE_NAME, _lowConfidenceContent);

		harness.RunAndCommit(CommandId.Scan);
		harness.RunAndCommit(CommandId.Analyze);

		Assert.Contains(
			string.Format(CultureInfo.InvariantCulture, AnalyzeConsoleReport.FINDINGS_BY_CONFIDENCE_MESSAGE_FORMAT, 1, 0, 1),
			harness.Transcript,
			StringComparison.Ordinal);
	}

	/// <summary>
	/// The table reaches the console at the END OF `analyze` ITSELF, before
	/// `report` ever runs.
	/// </summary>
	[Fact]
	public void TheFindingsTableIsPrintedByAnalyzeBeforeReportEverRuns()
	{
		using PipelineHarness harness = PipelineHarness.Create();
		harness.WriteSourceFile(_CREDENTIAL_FILE_NAME, _credentialContent);

		harness.RunAndCommit(CommandId.Scan);
		harness.RunAndCommit(CommandId.Analyze);

		Assert.Contains(
			ConsoleFindingsTable.BuildHeaderLine(harness.State.Findings, harness.Catalog),
			harness.Transcript,
			StringComparison.Ordinal);
		Assert.Contains(PipelineLanguageAnalyzer.TEMPLATED_PATTERN_CLASS, harness.Transcript, StringComparison.Ordinal);
	}

	/// <summary>
	/// The confidence threshold, applied to `analyze`'s own table on the same terms the report applies it.
	/// A Low finding a High-only threshold hides from the table never appears anywhere in the
	/// transcript, because it is not a literal-credential finding either.
	/// </summary>
	[Fact]
	public void AThresholdThatHidesFindingsSaysSoAndTheHiddenFindingNeverAppears()
	{
		using PipelineHarness harness = PipelineHarness.Create(ConfidenceLevel.High);
		harness.WriteSourceFile(_CREDENTIAL_FILE_NAME, _credentialContent);
		harness.WriteSourceFile(_SECOND_FILE_NAME, _lowConfidenceContent);

		harness.RunAndCommit(CommandId.Scan);
		harness.RunAndCommit(CommandId.Analyze);

		Assert.Contains(
			string.Format(
				CultureInfo.InvariantCulture,
				AnalyzeConsoleReport.THRESHOLD_HIDES_FINDINGS_MESSAGE_FORMAT,
				1,
				2,
				ConfidenceLevels.Name(ConfidenceLevel.High)),
			harness.Transcript,
			StringComparison.Ordinal);
		Assert.DoesNotContain(PipelineLanguageAnalyzer.LOW_PATTERN_CLASS, harness.Transcript, StringComparison.Ordinal);
	}

	/// <summary>The complement: a threshold that admits everything says nothing about hiding anything.</summary>
	[Fact]
	public void AThresholdThatHidesNothingSaysNothingAboutHiding()
	{
		using PipelineHarness harness = PipelineHarness.Create();
		harness.WriteSourceFile(_CREDENTIAL_FILE_NAME, _credentialContent);

		harness.RunAndCommit(CommandId.Scan);
		harness.RunAndCommit(CommandId.Analyze);

		Assert.DoesNotContain("Showing", harness.Transcript, StringComparison.Ordinal);
	}

	/// <summary>
	/// THROUGH THE REAL PIPELINE. A literal credential placed in source produces a
	/// warning line naming the finding's location and pattern class, citing the Secret Management
	/// guide - and never the placed secret itself, anywhere in the transcript.
	/// </summary>
	[Fact]
	public void ALiteralCredentialFindingProducesAWarningLineAndNeverTheSecretItself()
	{
		const string placedSecret = "a-literal-secret";

		using PipelineHarness harness = PipelineHarness.Create();
		harness.WriteSourceFile(_CREDENTIAL_FILE_NAME, _credentialContent);

		harness.RunAndCommit(CommandId.Scan);
		harness.RunAndCommit(CommandId.Analyze);

		Finding finding = Assert.Single(harness.State.Findings);

		Assert.Contains("Warning:", harness.Transcript, StringComparison.Ordinal);
		Assert.Contains(finding.Identifier, harness.Transcript, StringComparison.Ordinal);
		Assert.Contains(PipelineLanguageAnalyzer.TEMPLATED_PATTERN_CLASS, harness.Transcript, StringComparison.Ordinal);
		Assert.Contains(SecretManagementPointer.PART_B_NAME, harness.Transcript, StringComparison.Ordinal);
		Assert.DoesNotContain(placedSecret, harness.Transcript, StringComparison.Ordinal);
	}

	/// <summary>The complement: a finding with no literal credential produces no warning line at all.</summary>
	[Fact]
	public void AFindingWithNoLiteralCredentialProducesNoWarningLine()
	{
		using PipelineHarness harness = PipelineHarness.Create();
		harness.WriteSourceFile(_ORDINARY_FILE_NAME, _untemplatedContent);

		harness.RunAndCommit(CommandId.Scan);
		harness.RunAndCommit(CommandId.Analyze);

		Assert.DoesNotContain("Warning:", harness.Transcript, StringComparison.Ordinal);
	}

	/// <summary>
	/// The declared row cap, exercised through the real pipeline: past
	/// <see cref="ConsoleFindingsTable.MAXIMUM_CONSOLE_TABLE_ROWS"/> the table shows the first 200
	/// rows and says how many more the report carries, and the literal-credential warning list is
	/// capped at the same bound for the same reason - both are read by a person watching a console
	/// scroll, and neither is the artifact of record.
	/// </summary>
	[Fact]
	public void MoreThanTwoHundredFindingsCapTheTableAndTheWarningListAtTheSameBound()
	{
		const int offeredCount = ConsoleFindingsTable.MAXIMUM_CONSOLE_TABLE_ROWS + 50;

		using PipelineHarness harness = PipelineHarness.Create();
		harness.WriteSourceFile(_ORDINARY_FILE_NAME, ManyTemplatedConstructCallsWithLiteralCredential(offeredCount));

		harness.RunAndCommit(CommandId.Scan);

		Assert.Equal(StageRunOutcome.Completed, harness.Run(CommandId.Analyze));
		Assert.Equal(offeredCount, harness.State.Findings.Length);

		string transcript = harness.Transcript;
		string headerLine = ConsoleFindingsTable.BuildHeaderLine(
			harness.State.Findings.Take(ConsoleFindingsTable.MAXIMUM_CONSOLE_TABLE_ROWS),
			harness.Catalog);

		int headerIndex = transcript.IndexOf(headerLine, StringComparison.Ordinal);
		string tableFindingsPointerLine = string.Format(
			CultureInfo.InvariantCulture,
			AnalyzeConsoleReport.FURTHER_FINDINGS_NOT_SHOWN_MESSAGE_FORMAT,
			offeredCount - ConsoleFindingsTable.MAXIMUM_CONSOLE_TABLE_ROWS);

		Assert.True(headerIndex >= 0);

		string afterHeader = transcript[(headerIndex + headerLine.Length)..];
		int pointerIndex = afterHeader.IndexOf(tableFindingsPointerLine, StringComparison.Ordinal);

		Assert.True(pointerIndex >= 0);

		int tableRowCount = afterHeader[..pointerIndex]
			.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
			.Length;
		int warningLineCount = transcript
			.Split('\n')
			.Count(line => line.TrimEnd('\r').StartsWith("Warning:", StringComparison.Ordinal));

		Assert.Equal(ConsoleFindingsTable.MAXIMUM_CONSOLE_TABLE_ROWS, tableRowCount);
		Assert.Equal(ConsoleFindingsTable.MAXIMUM_CONSOLE_TABLE_ROWS, warningLineCount);
		int overflowCount = offeredCount - ConsoleFindingsTable.MAXIMUM_CONSOLE_TABLE_ROWS;

		Assert.Contains(
			string.Format(
				CultureInfo.InvariantCulture,
				AnalyzeConsoleReport.FURTHER_LITERAL_CREDENTIAL_WARNINGS_MESSAGE_FORMAT,
				overflowCount,
				overflowCount),
			transcript,
			StringComparison.Ordinal);
	}

	/// <summary>
	/// One file carrying more templated-construct calls than the supplied count, one per line, each
	/// with its own literal credential - so each becomes a High finding whose
	/// <see cref="Finding.LiteralCredentialSignal"/> is set, which is what the row-cap test needs to
	/// exercise the table's cap and the warning list's cap from a single fixture.
	/// </summary>
	private static string ManyTemplatedConstructCallsWithLiteralCredential(int count)
	{
		StringBuilder builder = new();

		builder.AppendLine("public sealed class ManyCredentialConstructs");
		builder.AppendLine("{");
		builder.AppendLine("\tvoid Build()");
		builder.AppendLine("\t{");

		for (int index = 0; index < count; index++)
		{
			builder.AppendLine($"\t\t{PipelineLanguageAnalyzer.TEMPLATED_CONSTRUCT}(user, \"invented-secret-{index}\");");
		}

		builder.AppendLine("\t}");
		builder.AppendLine("}");

		return builder.ToString();
	}
}
