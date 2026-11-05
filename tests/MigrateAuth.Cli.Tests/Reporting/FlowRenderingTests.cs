using System.Collections.Immutable;
using System.Text;
using System.Text.Json;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Findings;
using MigrateAuth.Cli.Reporting;

using Xunit;

namespace MigrateAuth.Cli.Tests.Reporting;

/// <summary>
/// The report and the JSON artifact: every finding carries its
/// flow and its FULL note beside it, outside the recommendation text; the Suggest console alone
/// abbreviates the note to a key. And the report carries the three flow counts.
/// </summary>
public sealed class FlowRenderingTests
{
	private const string _SOURCE_PATH_FORMAT = "src/file-{0}.cs";
	private const string _CONFIRM_THE_FLOW = "Confirm the flow";
	private const string _FLOW_ROW_PREFIX = "- Flow: ";
	private const string _FLOW_NOTE_ROW_PREFIX = "- Flow note: ";

	private static readonly ImmutableArray<(CredentialFlow Flow, FlowDecision Decision)> _pairs =
	[
		(CredentialFlow.ClientCredentials, FlowDecision.Default),
		(CredentialFlow.ClientCredentials, FlowDecision.FixedSignal),
		(CredentialFlow.AuthorizationCode, FlowDecision.InteractiveSignal),
		(CredentialFlow.AuthorizationCode, FlowDecision.Default)
	];

	/// <summary>
	/// Every Default and FixedSignal finding carries the default note naming the flow it was not assigned,
	/// and every InteractiveSignal finding carries the "confirm the flow" note and not a default note - in
	/// the report beside the finding and in the JSON `flowNote`.
	/// </summary>
	[Fact]
	public void EveryDecisionCarriesItsFullNoteInTheReportAndInTheJsonFlowNote()
	{
		ImmutableArray<Finding> findings = [.. _pairs.Select((pair, index) => FindingWith(index + 1, pair.Flow, pair.Decision))];
		ReportContent content = ReportHarness.ContentWith(
			ReportHarness.CatalogWithCSharpOnly(),
			ReportHarness.ConfigurationWith(ConfidenceLevel.Low),
			findings);

		string[] reportLines = RenderMarkdown(content).Split('\n');
		ArtifactFindingDocument[] artifact = RenderArtifact(content);

		for (int index = 0; index < findings.Length; index++)
		{
			(CredentialFlow flow, FlowDecision decision) = _pairs[index];
			string note = FlowNotes.NoteFor(decision, flow);
			int headingIndex = Array.FindIndex(reportLines, line => line.StartsWith($"#### {findings[index].Identifier} -", StringComparison.Ordinal));

			Assert.True(headingIndex >= 0);
			Assert.Equal($"{_FLOW_ROW_PREFIX}{CredentialFlows.Name(flow)} (decision: {CredentialFlows.Name(decision)})", reportLines[headingIndex + 2]);
			Assert.Equal($"{_FLOW_NOTE_ROW_PREFIX}{note}", reportLines[headingIndex + 3]);
			Assert.Equal(note, artifact[index].FlowNote);
			Assert.Equal(CredentialFlows.Name(flow), artifact[index].Flow);
			Assert.Equal(CredentialFlows.Name(decision), artifact[index].FlowDecision);

			AssertTheNoteMatchesTheDecision(note, flow, decision);
			Assert.DoesNotContain(note, artifact[index].Recommendation!, StringComparison.Ordinal);
		}
	}

	/// <summary>The report carries the three counts, over the reported set, in the same sentence the console uses.</summary>
	[Fact]
	public void TheReportCarriesTheThreeFlowCountsOverTheReportedSet()
	{
		ImmutableArray<Finding> findings =
		[
			.. _pairs.Select((pair, index) => FindingWith(index + 1, pair.Flow, pair.Decision)),
			ReportHarness.FindingAt(_pairs.Length + 1, "src/low.cs", 1, ReportHarness.TEMPLATED_PATTERN_CLASS, ConfidenceLevel.Low)
		];
		ReportContent content = ReportHarness.ContentWith(
			ReportHarness.CatalogWithCSharpOnly(),
			ReportHarness.ConfigurationWith(ConfidenceLevel.Medium),
			findings);

		string report = RenderMarkdown(content);
		FlowCounts expected = new(ClientCredentials: 2, AuthorizationCode: 2, Defaulted: 2);

		Assert.Contains(expected.Describe(), report, StringComparison.Ordinal);
		Assert.Equal(4, expected.Total);
		Assert.Contains("Findings reported: 4 of 5 recognized", report, StringComparison.Ordinal);
	}

	/// <summary>A Low finding carries no flow and no note in the report: nothing is invented for it.</summary>
	[Fact]
	public void ALowFindingCarriesNoFlowRowInTheReport()
	{
		ReportContent content = ReportHarness.ContentWith(
			ReportHarness.CatalogWithCSharpOnly(),
			ReportHarness.ConfigurationWith(ConfidenceLevel.Low),
			[ReportHarness.FindingAt(1, "src/low.cs", 1, ReportHarness.TEMPLATED_PATTERN_CLASS, ConfidenceLevel.Low)]);

		string report = RenderMarkdown(content);

		Assert.DoesNotContain(_FLOW_ROW_PREFIX, report, StringComparison.Ordinal);
		Assert.DoesNotContain(_FLOW_NOTE_ROW_PREFIX, report, StringComparison.Ordinal);
	}

	private static void AssertTheNoteMatchesTheDecision(string note, CredentialFlow flow, FlowDecision decision)
	{
		if (decision is FlowDecision.InteractiveSignal)
		{
			Assert.Contains(_CONFIRM_THE_FLOW, note, StringComparison.Ordinal);
			Assert.NotEqual(FlowNotes.DEFAULT_CLIENT_CREDENTIALS_NOTE, note);
			Assert.NotEqual(FlowNotes.DEFAULT_AUTHORIZATION_CODE_NOTE, note);
		}
		else
		{
			string otherFlowName = flow is CredentialFlow.ClientCredentials ? "Authorization Code" : "Client Credentials";

			Assert.Contains($"use {otherFlowName} instead", note, StringComparison.OrdinalIgnoreCase);
			Assert.DoesNotContain(_CONFIRM_THE_FLOW, note, StringComparison.Ordinal);
		}
	}

	private static Finding FindingWith(int sequentialIndex, CredentialFlow flow, FlowDecision decision)
	{
		return ReportHarness.FindingAt(
			sequentialIndex,
			string.Format(System.Globalization.CultureInfo.InvariantCulture, _SOURCE_PATH_FORMAT, sequentialIndex),
			sequentialIndex,
			ReportHarness.TEMPLATED_PATTERN_CLASS,
			ConfidenceLevel.High,
			flow: flow,
			flowDecision: decision);
	}

	private static string RenderMarkdown(ReportContent content)
	{
		using StringWriter writer = new();

		MarkdownReportRenderer.Render(writer, content, new GenericRecommendationProvider());

		return writer.ToString();
	}

	private static ArtifactFindingDocument[] RenderArtifact(ReportContent content)
	{
		using MemoryStream stream = new();

		StructuredDataArtifactRenderer.Render(stream, content, new GenericRecommendationProvider());

		ArtifactDocument? document = JsonSerializer.Deserialize(
			Encoding.UTF8.GetString(stream.ToArray()),
			ReportJsonContext.Default.ArtifactDocument);

		Assert.NotNull(document);

		return document.Findings;
	}
}
