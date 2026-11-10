using System.Globalization;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Session;

using Xunit;

namespace MigrateAuth.Cli.Tests.Session;

/// <summary>
/// THE FINDINGS SECTION OF THE STATE FILE, READ AS HOSTILE INPUT.
///
/// <c>SessionStateStore</c> names the reason the whole file is treated as untrusted: "the
/// readable-and-hostile case is what would otherwise let someone inject fabricated findings into a
/// client's report." These are the tests of it.
///
/// EVERY CASE BELOW EDITS ONE FIELD OF AN OTHERWISE VALID DOCUMENT, so that what is under test is
/// the check named in the test's own name rather than a document that would have been rejected for
/// several reasons at once.
/// </summary>
public sealed class PersistedFindingsTests
{
	private const string _REPOSITORY_LABEL_PLACEHOLDER = "{repository-label}";
	private const string _VALID_RELATIVE_PATH = "src/Credentials.cs";
	private const string _VALID_PATTERN_CLASS = "PIP-001";

	[Fact]
	public void AWellFormedFindingIsRestored()
	{
		SessionStateLoad load = LoadDocumentWithFindings(BuildFinding());

		Assert.True(load.WasResumed);

		MigrateAuth.Cli.Findings.Finding finding = Assert.Single(load.State.Findings);

		Assert.Equal(_VALID_RELATIVE_PATH, finding.Path.Value);
		Assert.Equal(_VALID_PATTERN_CLASS, finding.PatternClass.Value);
		Assert.Equal(AssessedLanguage.CSharp, finding.Language);
		Assert.Equal(ConfidenceLevel.High, finding.Level);
		Assert.Equal(DestinationCertainty.Established, finding.DestinationCertainty);
	}

	/// <summary>
	/// The identifier is DERIVED from the index rather than read, so a file cannot carry an
	/// identifier that disagrees with the number beside it - there is no field to disagree in.
	/// </summary>
	[Fact]
	public void TheIdentifierIsRenderedFromTheIndexRatherThanRead()
	{
		SessionStateLoad load = LoadDocumentWithFindings(BuildFinding());

		Assert.Equal("001", Assert.Single(load.State.Findings).Identifier);
	}

	/// <summary>
	/// The fingerprint is REBUILT from the finding's own path and pattern class, so a fingerprint
	/// inconsistent with the finding it belongs to is unrepresentable in the file.
	/// </summary>
	[Fact]
	public void TheFingerprintIsRebuiltFromTheFindingRatherThanRead()
	{
		SessionStateLoad load = LoadDocumentWithFindings(BuildFinding());

		MigrateAuth.Cli.Findings.Finding finding = Assert.Single(load.State.Findings);

		Assert.Equal(_VALID_RELATIVE_PATH, finding.Fingerprint.Path.Value);
		Assert.Equal(_VALID_PATTERN_CLASS, finding.Fingerprint.PatternClass.Value);
	}

	/// <summary>
	/// The full ordered set is numbered from one with no gaps, so the index is checked against the
	/// position. This is the structural check no per-field validation could supply: every field
	/// below is individually well formed and the SET is still not one this engine could produce.
	/// </summary>
	[Theory]
	[InlineData(2, "an index that does not start at one")]
	[InlineData(0, "an index below the first")]
	public void AFindingWhoseIndexIsNotItsPositionStartsAFreshSession(int sequentialIndex, string reason)
	{
		SessionStateLoad load = LoadDocumentWithFindings(BuildFinding(sequentialIndex: sequentialIndex));

		Assert.False(load.WasResumed);
		Assert.Equal(SessionStateRejectionReason.FindingNotWellFormed, load.RejectionReason);
		Assert.Empty(load.State.Findings);
		Assert.NotEmpty(reason);
	}

	[Fact]
	public void ADuplicatedFindingStartsAFreshSession()
	{
		SessionStateLoad load = LoadDocumentWithFindings($"{BuildFinding()}, {BuildFinding()}", findingsCount: 2);

		Assert.False(load.WasResumed);
		Assert.Equal(SessionStateRejectionReason.FindingNotWellFormed, load.RejectionReason);
	}

	[Fact]
	public void TwoFindingsNumberedInSequenceAreRestored()
	{
		SessionStateLoad load = LoadDocumentWithFindings(
			$"{BuildFinding()}, {BuildFinding(sequentialIndex: 2, relativePath: "src/Headers.cs")}",
			findingsCount: 2);

		Assert.True(load.WasResumed);
		Assert.Equal([1, 2], load.State.Findings.Select(finding => finding.SequentialIndex));
	}

	/// <summary>
	/// The containment check on the read path, applied to findings as it is to
	/// candidates. A path escaping the repository root is schema-valid and is refused by the same
	/// primitive that refuses it everywhere else.
	/// </summary>
	[Theory]
	[InlineData("../outside-the-repository.cs")]
	[InlineData("src/../../escaped.cs")]
	public void AFindingWhosePathEscapesTheRepositoryStartsAFreshSession(string relativePath)
	{
		SessionStateLoad load = LoadDocumentWithFindings(BuildFinding(relativePath: relativePath));

		Assert.False(load.WasResumed);
		Assert.Equal(SessionStateRejectionReason.FindingNotWellFormed, load.RejectionReason);
	}

	/// <summary>
	/// The pattern class goes through <c>RuleIdentifier.TryFromPatternClass</c>, whose shape no line
	/// of source code can satisfy - which is what stops this field becoming the route by which file
	/// text re-enters the product through its own state file.
	/// </summary>
	[Theory]
	[InlineData("not a pattern class")]
	[InlineData("var password = secretValue;")]
	[InlineData("PIP-1")]
	[InlineData("")]
	public void AFindingCarryingAnImpossiblePatternClassStartsAFreshSession(string patternClass)
	{
		SessionStateLoad load = LoadDocumentWithFindings(BuildFinding(patternClass: patternClass));

		Assert.False(load.WasResumed);
		Assert.Equal(SessionStateRejectionReason.FindingNotWellFormed, load.RejectionReason);
	}

	/// <summary>
	/// A language this build cannot represent is a structural error rather than a version
	/// difference. The tolerant read belongs to the CONFIGURATION file, which a
	/// client writes by hand; this file is written only by the tool.
	/// </summary>
	[Theory]
	[InlineData("Fortran")]
	[InlineData("")]
	public void AFindingCarryingALanguageThisBuildDoesNotKnowStartsAFreshSession(string language)
	{
		SessionStateLoad load = LoadDocumentWithFindings(BuildFinding(language: language));

		Assert.False(load.WasResumed);
		Assert.Equal(SessionStateRejectionReason.FindingNotWellFormed, load.RejectionReason);
	}

	[Theory]
	[InlineData("Critical")]
	[InlineData("")]
	public void AFindingCarryingALevelOutsideTheRubricStartsAFreshSession(string level)
	{
		SessionStateLoad load = LoadDocumentWithFindings(BuildFinding(confidenceLevel: level));

		Assert.False(load.WasResumed);
		Assert.Equal(SessionStateRejectionReason.FindingNotWellFormed, load.RejectionReason);
	}

	/// <summary>
	/// The destination certainty is a closed pair of declared names, so the integer forms an enum
	/// parse would have accepted are refused - a file carrying <c>"1"</c> must not read as
	/// Established.
	/// </summary>
	[Theory]
	[InlineData("1")]
	[InlineData("Established")]
	[InlineData("probably")]
	public void AFindingCarryingAnUndeclaredDestinationCertaintyStartsAFreshSession(string certainty)
	{
		SessionStateLoad load = LoadDocumentWithFindings(BuildFinding(destinationCertainty: certainty));

		Assert.False(load.WasResumed);
		Assert.Equal(SessionStateRejectionReason.FindingNotWellFormed, load.RejectionReason);
	}

	[Theory]
	[InlineData(0, 1, 1)]
	[InlineData(1, 0, 1)]
	[InlineData(1, 1, 0)]
	[InlineData(-1, 1, 1)]
	public void AFindingCarryingAPositionBelowOneStartsAFreshSession(int line, int column, int occurrenceOrdinal)
	{
		SessionStateLoad load = LoadDocumentWithFindings(
			BuildFinding(line: line, column: column, occurrenceOrdinal: occurrenceOrdinal));

		Assert.False(load.WasResumed);
		Assert.Equal(SessionStateRejectionReason.FindingNotWellFormed, load.RejectionReason);
	}

	/// <summary>
	/// A member the finding schema does not declare is refused rather than ignored, exactly as it is
	/// on the document that contains it - which is what stops a crafted file smuggling a field a
	/// later build might read.
	/// </summary>
	[Fact]
	public void AFindingCarryingAnUndeclaredMemberStartsAFreshSession()
	{
		SessionStateLoad load = LoadDocumentWithFindings("""
			{
				"sequentialIndex": 1,
				"relativePath": "src/Credentials.cs",
				"line": 1,
				"column": 1,
				"language": "CSharp",
				"patternClass": "PIP-001",
				"confidenceLevel": "High",
				"destinationCertainty": "established",
				"literalCredentialSignal": false,
				"occurrenceOrdinal": 1,
				"renderedRecommendation": "text that has no business being here"
			}
			""");

		Assert.False(load.WasResumed);
		Assert.Equal(SessionStateRejectionReason.DocumentMalformed, load.RejectionReason);
	}

	/// <summary>
	/// A finding missing a field altogether is refused. Every member of the persisted shape is
	/// nullable precisely so that "absent" is a value the validator can see rather than a default
	/// that passes for a real one.
	/// </summary>
	[Fact]
	public void AFindingMissingAFieldStartsAFreshSession()
	{
		SessionStateLoad load = LoadDocumentWithFindings("""
			{
				"sequentialIndex": 1,
				"relativePath": "src/Credentials.cs",
				"line": 1,
				"column": 1,
				"language": "CSharp",
				"patternClass": "PIP-001",
				"confidenceLevel": "High",
				"destinationCertainty": "established",
				"occurrenceOrdinal": 1
			}
			""");

		Assert.False(load.WasResumed);
		Assert.Equal(SessionStateRejectionReason.FindingNotWellFormed, load.RejectionReason);
	}

	/// <summary>
	/// A finding decided by a fixed signal or an interactive one is restored with the evidence,
	/// the flow and the decision it was persisted with, and the note is not among the persisted
	/// members - it is derived at render time.
	/// </summary>
	[Theory]
	[InlineData("Fixed", "ClientCredentials", "FixedSignal")]
	[InlineData("Interactive", "AuthorizationCode", "InteractiveSignal")]
	[InlineData("None", "ClientCredentials", "Default")]
	public void AFindingIsRestoredWithItsPersistedFlowEvidenceFlowAndDecision(string evidence, string flow, string decision)
	{
		SessionStateLoad load = LoadDocumentWithFindings(BuildFinding(
			flowEvidence: Quote(evidence),
			flow: Quote(flow),
			flowDecision: Quote(decision)));

		MigrateAuth.Cli.Findings.Finding finding = Assert.Single(load.State.Findings);

		Assert.True(load.WasResumed);
		Assert.Equal(evidence, finding.FlowEvidence.ToString());
		Assert.Equal(flow, finding.Flow.ToString());
		Assert.Equal(decision, finding.FlowDecision.ToString());
	}

	/// <summary>A Low finding carries none of the three, exactly as the engine builds it.</summary>
	[Fact]
	public void ALowFindingIsRestoredWithNoFlowFields()
	{
		SessionStateLoad load = LoadDocumentWithFindings(BuildFinding(
			confidenceLevel: "Low",
			flowEvidence: "null",
			flow: "null",
			flowDecision: "null"));

		MigrateAuth.Cli.Findings.Finding finding = Assert.Single(load.State.Findings);

		Assert.True(load.WasResumed);
		Assert.Null(finding.FlowEvidence);
		Assert.Null(finding.Flow);
		Assert.Null(finding.FlowDecision);
	}

	/// <summary>
	/// The flow fields, read as hostile input. Every case edits the flow fields of one otherwise well-formed
	/// finding: a missing member, a member on a Low finding, a name outside the closed vocabulary or in
	/// another casing, and a triple whose parts disagree (an interactive signal that decided a
	/// client-credentials flow, a fixed signal recorded as the default).
	/// </summary>
	[Theory]
	[InlineData("High", "null", "null", "null")]
	[InlineData("High", "\"None\"", "null", "\"Default\"")]
	[InlineData("High", "\"None\"", "\"ClientCredentials\"", "null")]
	[InlineData("Low", "\"None\"", "\"ClientCredentials\"", "\"Default\"")]
	[InlineData("Low", "\"None\"", "null", "null")]
	[InlineData("High", "\"Sometimes\"", "\"ClientCredentials\"", "\"Default\"")]
	[InlineData("High", "\"none\"", "\"ClientCredentials\"", "\"Default\"")]
	[InlineData("High", "\"None\"", "\"clientcredentials\"", "\"Default\"")]
	[InlineData("High", "\"None\"", "\"ClientCredentials\"", "\"default\"")]
	[InlineData("High", "\"Interactive\"", "\"ClientCredentials\"", "\"InteractiveSignal\"")]
	[InlineData("High", "\"Interactive\"", "\"AuthorizationCode\"", "\"Default\"")]
	[InlineData("High", "\"Fixed\"", "\"AuthorizationCode\"", "\"FixedSignal\"")]
	[InlineData("High", "\"Fixed\"", "\"ClientCredentials\"", "\"Default\"")]
	[InlineData("High", "\"None\"", "\"ClientCredentials\"", "\"FixedSignal\"")]
	public void AFindingWithMalformedFlowFieldsStartsAFreshSession(
		string confidenceLevel,
		string flowEvidence,
		string flow,
		string flowDecision)
	{
		SessionStateLoad load = LoadDocumentWithFindings(BuildFinding(
			confidenceLevel: confidenceLevel,
			flowEvidence: flowEvidence,
			flow: flow,
			flowDecision: flowDecision));

		Assert.False(load.WasResumed);
		Assert.Equal(SessionStateRejectionReason.FindingNotWellFormed, load.RejectionReason);
	}

	/// <summary>
	/// A finding decided by the default carries the flow the document says was the default;
	/// the two are one analysis's output and a file in which they disagree was not written by this tool.
	/// </summary>
	[Fact]
	public void ADefaultDecidedFindingUnderADifferentFlowThanTheDocumentDefaultStartsAFreshSession()
	{
		SessionStateLoad load = LoadDocumentWithFindings(BuildFinding(
			flowEvidence: Quote("None"),
			flow: Quote("AuthorizationCode"),
			flowDecision: Quote("Default")));

		Assert.False(load.WasResumed);
		Assert.Equal(SessionStateRejectionReason.DefaultFlowNotWellFormed, load.RejectionReason);
	}

	[Fact]
	public void ADefaultDecidedFindingUnderTheProfileDefaultIsRestored()
	{
		SessionStateLoad load = LoadDocumentWithFindings(
			BuildFinding(
				flowEvidence: Quote("None"),
				flow: Quote("AuthorizationCode"),
				flowDecision: Quote("Default")),
			defaultFlowJson: Quote("AuthorizationCode"),
			defaultFlowOriginJson: Quote("Profile"));

		Assert.True(load.WasResumed);
		Assert.Equal(MigrateAuth.Cli.Findings.CredentialFlow.AuthorizationCode, load.State.DefaultFlow.Flow);
		Assert.Equal(MigrateAuth.Cli.Configuration.ConfigurationSource.UserProfileFile, load.State.DefaultFlow.Origin);
	}

	private static string Quote(string value)
	{
		return $"\"{value}\"";
	}

	private static SessionStateLoad LoadDocumentWithFindings(
		string findings,
		int findingsCount = 1,
		string defaultFlowJson = SessionStateDocumentSections.DEFAULT_FLOW_JSON,
		string defaultFlowOriginJson = SessionStateDocumentSections.DEFAULT_FLOW_ORIGIN_JSON)
	{
		using TemporaryFolder destination = TemporaryFolder.Create();
		using TemporaryFolder repositoryFolder = TemporaryFolder.Create();

		RepositoryFolderPath repository = RepositoryFolderPath.FromSuppliedText(repositoryFolder.Path);
		string statePath = destination.Combine(SessionStateStore.BuildStateFileName(repository));

		File.WriteAllText(
			statePath,
			BuildDocumentText(findings, repository.Label, findingsCount, defaultFlowJson, defaultFlowOriginJson));

		return new SessionStateStore(DocumentationDestinationFolderPath.FromSuppliedText(destination.Path))
			.Load(repository, LanguageCatalogRegistry.BuildEmbeddedCatalog());
	}

	/// <summary>
	/// <c>completedStages</c> below names both <c>scan</c> and
	/// <c>analyze</c> complete, so the document must carry all five of their sections - a well-formed,
	/// empty scan summary and skipped-item tallies from each stage, a findings-cap outcome whose
	/// retained count agrees with <paramref name="findingsCount"/>, and an enabled-language set - or
	/// the read path refuses it for that reason alone, before a single one of these tests' own
	/// fields is even examined.
	/// </summary>
	private static string BuildDocumentText(
		string findings,
		string repositoryLabel,
		int findingsCount,
		string defaultFlowJson,
		string defaultFlowOriginJson)
	{
		CatalogVersion version = LanguageCatalog.CurrentVersion;

		return $$"""
			{
				"schemaVersion": {{SessionStateStore.CURRENT_SCHEMA_VERSION}},
				"catalogVersionMajor": {{version.Major}},
				"catalogVersionMinor": {{version.Minor}},
				"catalogVersionRevision": {{version.Revision}},
				"repositoryRootLabel": "{{repositoryLabel}}",
				"completedStages": ["scan", "analyze"],
				"candidateRelativePaths": [],
				"findings": [{{findings}}],
				"scanSummary": {{SessionStateDocumentSections.EMPTY_SCAN_SUMMARY_JSON}},
				"scanSkippedItems": {{SessionStateDocumentSections.EmptySkippedItemTalliesJson()}},
				"analysisSkippedItems": {{SessionStateDocumentSections.EmptySkippedItemTalliesJson()}},
				"findingsCapOutcome": {{SessionStateDocumentSections.UntrippedFindingsCapOutcomeJson(findingsCount)}},
				"scanEnabledLanguages": {{SessionStateDocumentSections.EMPTY_SCAN_ENABLED_LANGUAGES_JSON}},
				"candidateLanguages": [],
				"defaultFlow": {{defaultFlowJson}},
				"defaultFlowOrigin": {{defaultFlowOriginJson}},
				"flowEvaluationDiagnostics": {{SessionStateDocumentSections.EMPTY_FLOW_EVALUATION_DIAGNOSTICS_JSON}}
			}
			""";
	}

	private static string BuildFinding(
		int sequentialIndex = 1,
		string relativePath = _VALID_RELATIVE_PATH,
		int line = 12,
		int column = 5,
		string language = "CSharp",
		string patternClass = _VALID_PATTERN_CLASS,
		string confidenceLevel = "High",
		string destinationCertainty = SessionStateStore.DESTINATION_ESTABLISHED_NAME,
		int occurrenceOrdinal = 1,
		string flowEvidence = "\"None\"",
		string flow = "\"ClientCredentials\"",
		string flowDecision = "\"Default\"")
	{
		return string.Create(
			CultureInfo.InvariantCulture,
			$$"""
			{
				"sequentialIndex": {{sequentialIndex}},
				"relativePath": "{{relativePath}}",
				"line": {{line}},
				"column": {{column}},
				"language": "{{language}}",
				"patternClass": "{{patternClass}}",
				"confidenceLevel": "{{confidenceLevel}}",
				"destinationCertainty": "{{destinationCertainty}}",
				"literalCredentialSignal": false,
				"occurrenceOrdinal": {{occurrenceOrdinal}},
				"flowEvidence": {{flowEvidence}},
				"flow": {{flow}},
				"flowDecision": {{flowDecision}}
			}
			""");
	}
}
