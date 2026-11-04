using System.Collections.Immutable;
using System.Text.Json;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Findings;
using MigrateAuth.Cli.Reporting;
using MigrateAuth.Cli.Session;
using MigrateAuth.Cli.Tests.Fixtures;
using MigrateAuth.Cli.Writing;

using Xunit;

namespace MigrateAuth.Cli.Tests.Pipeline;

/// <summary>
/// THE FLOW EXTENSIONS OF THE DATA ARTIFACT: WHAT A CONSUMER PARSING THE WRITTEN FILE
/// SEES, FROM A REAL RUN OF THE FOUR STAGES.
///
/// The artifact is parsed as raw JSON and not read back through the declared document type, because
/// the claim is about names, types and nulls on the wire - a round trip through the writer's own
/// type would pass on a document no other consumer could read the same way.
/// </summary>
public sealed class FlowArtifactContractTests
{
	private const string _INTERACTIVE_FILE = "Interactive.cs";
	private const string _FIXED_FILE = "Fixed.cs";
	private const string _NEITHER_FILE = "Neither.cs";
	private const string _LOW_FILE = "Low.cs";
	private const string _UNTEMPLATED_FILE = "Untemplated.cs";

	private const string _FINDINGS_FIELD = "findings";
	private const string _PROVENANCE_FIELD = "provenance";
	private const string _DEFAULT_FLOW_FIELD = "defaultFlow";
	private const string _DEFAULT_FLOW_ORIGIN_FIELD = "defaultFlowOrigin";
	private const string _FLOW_FIELD = "flow";
	private const string _FLOW_DECISION_FIELD = "flowDecision";
	private const string _FLOW_NOTE_FIELD = "flowNote";
	private const string _RECOMMENDATION_FIELD = "recommendation";
	private const string _CONFIDENCE_FIELD = "confidence";
	private const string _FILE_PATH_FIELD = "filePath";
	private const string _BUILT_IN_ORIGIN = "BuiltInDefault";
	private const string _PROFILE_ORIGIN = "Profile";
	private const string _CLIENT_CREDENTIALS_TEXT = "Client Credentials";
	private const int _SEEDED_FINDING_COUNT = 5;

	private static readonly string[] _sectionThreeOneFields =
		["id", "filePath", "line", "language", "patternClass", "confidence", "recommendation"];

	private static readonly string[] _flowExtensionFields = [_FLOW_FIELD, _FLOW_DECISION_FIELD, _FLOW_NOTE_FIELD];

	private static readonly string[] _allNoteTexts =
	[
		FlowNotes.DEFAULT_CLIENT_CREDENTIALS_NOTE,
		FlowNotes.DEFAULT_AUTHORIZATION_CODE_NOTE,
		FlowNotes.FIXED_SIGNAL_CLIENT_CREDENTIALS_NOTE,
		FlowNotes.INTERACTIVE_SIGNAL_AUTHORIZATION_CODE_NOTE
	];

	private static readonly string[] _decisionNames =
		[CredentialFlows.INTERACTIVE_SIGNAL_NAME, CredentialFlows.FIXED_SIGNAL_NAME, CredentialFlows.DEFAULT_DECISION_NAME];

	private static readonly ImmutableArray<FlowSignal> _bothKindsOfSignal =
		[FlowSignalFixtures.InteractiveCodeOnly, FlowSignalFixtures.FixedCodeOnly];

	/// <summary>Over every kind of finding a run can hold.</summary>
	[Fact]
	public void TheWrittenArtifactCarriesTheSevenFieldsUnchangedAndTheThreeExtensionsFlat()
	{
		using PipelineHarness harness = PipelineHarness.CreateWithFlowSignals(_bothKindsOfSignal);

		PlantEveryKindOfFinding(harness);
		harness.RunTheWholeWorkflow();

		using JsonDocument artifact = JsonDocument.Parse(harness.ReadWrittenArtifact(OutputArtifactKind.StructuredDataArtifact));
		JsonElement[] findings = [.. artifact.RootElement.GetProperty(_FINDINGS_FIELD).EnumerateArray()];

		Assert.Equal(_SEEDED_FINDING_COUNT, findings.Length);

		foreach (JsonElement finding in findings)
		{
			foreach (string field in _sectionThreeOneFields.Concat(_flowExtensionFields))
			{
				Assert.True(finding.TryGetProperty(field, out _), $"The finding lacks '{field}'.");
			}

			Assert.Equal(JsonValueKind.String, finding.GetProperty("id").ValueKind);
			Assert.Equal(JsonValueKind.String, finding.GetProperty(_FILE_PATH_FIELD).ValueKind);
			Assert.Equal(JsonValueKind.Number, finding.GetProperty("line").ValueKind);
			Assert.Equal(JsonValueKind.String, finding.GetProperty("language").ValueKind);
			Assert.Equal(JsonValueKind.String, finding.GetProperty("patternClass").ValueKind);
			Assert.Equal(JsonValueKind.String, finding.GetProperty(_CONFIDENCE_FIELD).ValueKind);
		}
	}

	[Fact]
	public void ALowFindingKeepsRecommendationNullWithAllThreeExtensionsNull()
	{
		using PipelineHarness harness = PipelineHarness.CreateWithFlowSignals(_bothKindsOfSignal);

		PlantEveryKindOfFinding(harness);
		harness.RunTheWholeWorkflow();

		using JsonDocument artifact = JsonDocument.Parse(harness.ReadWrittenArtifact(OutputArtifactKind.StructuredDataArtifact));
		JsonElement low = Assert.Single(FindingsAt(artifact, ConfidenceLevel.Low));

		Assert.Equal(JsonValueKind.Null, low.GetProperty(_RECOMMENDATION_FIELD).ValueKind);
		Assert.Equal(JsonValueKind.Null, low.GetProperty(_FLOW_FIELD).ValueKind);
		Assert.Equal(JsonValueKind.Null, low.GetProperty(_FLOW_DECISION_FIELD).ValueKind);
		Assert.Equal(JsonValueKind.Null, low.GetProperty(_FLOW_NOTE_FIELD).ValueKind);
	}

	[Fact]
	public void EveryHighAndMediumFindingCarriesADecisionAndTheNoteOfItsDecisionAndFlow()
	{
		using PipelineHarness harness = PipelineHarness.CreateWithFlowSignals(_bothKindsOfSignal);

		PlantEveryKindOfFinding(harness);
		harness.RunTheWholeWorkflow();

		using JsonDocument artifact = JsonDocument.Parse(harness.ReadWrittenArtifact(OutputArtifactKind.StructuredDataArtifact));
		JsonElement[] reported = [.. FindingsAt(artifact, ConfidenceLevel.High), .. FindingsAt(artifact, ConfidenceLevel.Medium)];

		Assert.NotEmpty(reported);

		foreach (JsonElement finding in reported)
		{
			string decisionName = finding.GetProperty(_FLOW_DECISION_FIELD).GetString()!;
			string flowName = finding.GetProperty(_FLOW_FIELD).GetString()!;

			Assert.Contains(decisionName, _decisionNames);
			Assert.True(CredentialFlows.TryParse(flowName, out CredentialFlow flow));
			Assert.Equal(flowName, CredentialFlows.Name(flow));
			Assert.Equal(
				FlowNotes.NoteFor(DecisionOf(decisionName), flow),
				finding.GetProperty(_FLOW_NOTE_FIELD).GetString());
		}

		Assert.Contains(reported, finding => DecisionNameOf(finding) == CredentialFlows.INTERACTIVE_SIGNAL_NAME);
		Assert.Contains(reported, finding => DecisionNameOf(finding) == CredentialFlows.FIXED_SIGNAL_NAME);
		Assert.Contains(reported, finding => DecisionNameOf(finding) == CredentialFlows.DEFAULT_DECISION_NAME);
	}

	[Fact]
	public void NoRecommendationContainsAFlowNoteText()
	{
		using PipelineHarness harness = PipelineHarness.CreateWithFlowSignals(_bothKindsOfSignal);

		PlantEveryKindOfFinding(harness);
		harness.RunTheWholeWorkflow();

		using JsonDocument artifact = JsonDocument.Parse(harness.ReadWrittenArtifact(OutputArtifactKind.StructuredDataArtifact));

		foreach (JsonElement finding in artifact.RootElement.GetProperty(_FINDINGS_FIELD).EnumerateArray())
		{
			string? recommendation = finding.GetProperty(_RECOMMENDATION_FIELD).GetString();

			foreach (string noteText in _allNoteTexts)
			{
				Assert.False(recommendation is not null && recommendation.Contains(noteText, StringComparison.Ordinal));
			}
		}
	}

	/// <summary>A profile default of Authorization Code, and a note naming Client Credentials.</summary>
	[Fact]
	public void AProfileDefaultOfAuthorizationCodeDecidesANoSignalFindingAndItsNoteNamesClientCredentials()
	{
		using PipelineHarness harness = PipelineHarness.CreateWithFlowSignals(_bothKindsOfSignal, CredentialFlow.AuthorizationCode);

		harness.WriteSourceFile(_NEITHER_FILE, SourceOf(token: null));
		harness.RunTheWholeWorkflow();

		using JsonDocument artifact = JsonDocument.Parse(harness.ReadWrittenArtifact(OutputArtifactKind.StructuredDataArtifact));
		JsonElement finding = Assert.Single(FindingsAt(artifact, ConfidenceLevel.High));

		Assert.Equal(CredentialFlows.AUTHORIZATION_CODE_NAME, finding.GetProperty(_FLOW_FIELD).GetString());
		Assert.Equal(CredentialFlows.DEFAULT_DECISION_NAME, finding.GetProperty(_FLOW_DECISION_FIELD).GetString());
		Assert.Contains(
			_CLIENT_CREDENTIALS_TEXT,
			finding.GetProperty(_FLOW_NOTE_FIELD).GetString(),
			StringComparison.Ordinal);
		Assert.Equal(
			FlowNotes.DEFAULT_AUTHORIZATION_CODE_NOTE,
			finding.GetProperty(_FLOW_NOTE_FIELD).GetString());
	}

	/// <summary>The provenance states the effective default flow and its origin.</summary>
	[Fact]
	public void TheProvenanceStatesTheBuiltInDefaultFlowWithTheOriginAConsumerKeysOn()
	{
		using PipelineHarness harness = PipelineHarness.CreateWithFlowSignals(_bothKindsOfSignal);

		harness.WriteSourceFile(_NEITHER_FILE, SourceOf(token: null));
		harness.RunTheWholeWorkflow();

		using JsonDocument artifact = JsonDocument.Parse(harness.ReadWrittenArtifact(OutputArtifactKind.StructuredDataArtifact));
		JsonElement provenance = artifact.RootElement.GetProperty(_PROVENANCE_FIELD);

		Assert.Equal(CredentialFlows.CLIENT_CREDENTIALS_NAME, provenance.GetProperty(_DEFAULT_FLOW_FIELD).GetString());
		Assert.Equal(_BUILT_IN_ORIGIN, provenance.GetProperty(_DEFAULT_FLOW_ORIGIN_FIELD).GetString());
	}

	[Fact]
	public void TheProvenanceStatesAProfileDefaultFlowAsComingFromTheProfileAndNeverFromTheRepository()
	{
		using PipelineHarness harness = PipelineHarness.CreateWithFlowSignals(_bothKindsOfSignal, CredentialFlow.AuthorizationCode);

		harness.WriteSourceFile(_NEITHER_FILE, SourceOf(token: null));
		harness.RunTheWholeWorkflow();

		using JsonDocument artifact = JsonDocument.Parse(harness.ReadWrittenArtifact(OutputArtifactKind.StructuredDataArtifact));
		JsonElement provenance = artifact.RootElement.GetProperty(_PROVENANCE_FIELD);
		string markdown = harness.ReadWrittenArtifact(OutputArtifactKind.MarkdownReport);

		Assert.Equal(CredentialFlows.AUTHORIZATION_CODE_NAME, provenance.GetProperty(_DEFAULT_FLOW_FIELD).GetString());
		Assert.Equal(_PROFILE_ORIGIN, provenance.GetProperty(_DEFAULT_FLOW_ORIGIN_FIELD).GetString());
		Assert.Contains(CredentialFlows.AUTHORIZATION_CODE_NAME, markdown, StringComparison.Ordinal);
		Assert.Contains("Default flow", markdown, StringComparison.Ordinal);
	}

	private static void PlantEveryKindOfFinding(PipelineHarness harness)
	{
		harness.WriteSourceFile(_INTERACTIVE_FILE, SourceOf(FlowSignalFixtures.INTERACTIVE_TOKEN));
		harness.WriteSourceFile(_FIXED_FILE, SourceOf(FlowSignalFixtures.FIXED_TOKEN));
		harness.WriteSourceFile(_NEITHER_FILE, SourceOf(token: null));
		harness.WriteSourceFile(
			_UNTEMPLATED_FILE,
			$"class Untemplated\n{{\n\tvoid Build() => {PipelineLanguageAnalyzer.UNTEMPLATED_CONSTRUCT}();\n}}\n");
		harness.WriteSourceFile(
			_LOW_FILE,
			$"class Legacy\n{{\n\tvoid Build() => {PipelineLanguageAnalyzer.LOW_CONSTRUCT}();\n}}\n");
	}

	private static string SourceOf(string? token)
	{
		string tokenLine = token is null ? string.Empty : $"\tvoid Signal() => {token}();\n";

		return $"class Sample\n{{\n{tokenLine}\tvoid Build() => {PipelineLanguageAnalyzer.TEMPLATED_CONSTRUCT}(user, \"a-literal-secret\");\n}}\n";
	}

	private static IEnumerable<JsonElement> FindingsAt(JsonDocument artifact, ConfidenceLevel level)
	{
		return artifact.RootElement
			.GetProperty(_FINDINGS_FIELD)
			.EnumerateArray()
			.Where(finding => finding.GetProperty(_CONFIDENCE_FIELD).GetString() == ConfidenceLevels.Name(level));
	}

	private static string DecisionNameOf(JsonElement finding) =>
		finding.GetProperty(_FLOW_DECISION_FIELD).GetString()!;

	private static FlowDecision DecisionOf(string decisionName)
	{
		return Enum.GetValues<FlowDecision>().Single(decision => CredentialFlows.Name(decision) == decisionName);
	}
}
