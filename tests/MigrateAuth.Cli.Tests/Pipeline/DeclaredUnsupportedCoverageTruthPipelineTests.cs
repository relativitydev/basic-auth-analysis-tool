using System.Linq;
using System.Text.Json;

using MigrateAuth.Cli.Reporting;
using MigrateAuth.Cli.Writing;

using Xunit;

namespace MigrateAuth.Cli.Tests.Pipeline;

/// <summary>
/// Neither shareable artifact may say the findings caps or their truncation counter are
/// unshipped - both shipped, and both artifacts declare the outcome via
/// `FindingsCapPolicy`/`FindingComposition` and their own `findingsCap` section. This runs the
/// real four-stage pipeline over the real embedded `CSharp` analyzer - not a synthetic fixture -
/// because what is under test is the catalog's own declared-unsupported text as this build ships
/// it, the same text <see cref="CSharpRecommendationRoutePipelineTests"/> proves the templates
/// through.
/// </summary>
public sealed class DeclaredUnsupportedCoverageTruthPipelineTests
{
	private const string _CSHARP_LANGUAGE_NAME = "CSharp";
	private const string _FINDINGS_CAP_JSON_FIELD_NAME = "findingsCap";
	private const string _UNSHIPPED_CAP_CLAIM = "not yet shipped";
	private const string _UNSHIPPED_FALLBACK_CLAIM = "does not yet exist";
	private const string _AGGREGATE_ONLY_CLAIM = "aggregate counts are reported";
	private const string _PER_END_USER_GAP_CONSTRUCT = "authenticates as each individual end user";
	private const string _PER_END_USER_GAP_REASON = "authenticates as each of its end users";
	private const string _INBOUND_DIRECTION_GAP_CONSTRUCT = "checks incoming Basic Authentication from its own callers";
	private const string _LEGACY_ENCODING_GAP_CONSTRUCT = "older single-byte text encoding, such as Latin-1 or Windows-1252";
	private const string _LEGACY_ENCODING_GAP_REASON = "could not be read as text";
	private const string _OWN_ENCODING_HELPER_GAP_CONSTRUCT = "converted to Base64 by the project's own helper method";

	/// <summary>
	/// No candidate file is needed: coverage's declared-unsupported constructs come from the
	/// catalog itself (<see cref="MigrateAuth.Cli.Reporting.CoverageStatement"/>), never from a
	/// scan result, exactly as a zero-candidate report still states coverage.
	/// </summary>
	[Fact]
	public void TheMarkdownReportDoesNotClaimTheFindingsCapsAreUnshipped()
	{
		using PipelineHarness harness = PipelineHarness.CreateOverTheEmbeddedCatalog();

		harness.RunTheWholeWorkflow();

		string reportText = harness.ReadWrittenArtifact(OutputArtifactKind.MarkdownReport);

		Assert.Contains(_CSHARP_LANGUAGE_NAME, reportText, StringComparison.Ordinal);
		Assert.DoesNotContain(_UNSHIPPED_CAP_CLAIM, reportText, StringComparison.Ordinal);
		Assert.DoesNotContain(_UNSHIPPED_FALLBACK_CLAIM, reportText, StringComparison.Ordinal);
	}

	/// <summary>
	/// The JSON artifact carries the same declared-unsupported text under
	/// <c>coverage.languages[].declaredUnsupportedConstructs</c>, beside the shipped
	/// <see cref="_FINDINGS_CAP_JSON_FIELD_NAME"/> section itself - so the two must never disagree
	/// about whether the caps exist.
	/// </summary>
	[Fact]
	public void TheJsonArtifactDoesNotClaimTheFindingsCapsAreUnshipped()
	{
		using PipelineHarness harness = PipelineHarness.CreateOverTheEmbeddedCatalog();

		harness.RunTheWholeWorkflow();

		string artifactText = harness.ReadWrittenArtifact(OutputArtifactKind.StructuredDataArtifact);

		Assert.Contains(_FINDINGS_CAP_JSON_FIELD_NAME, artifactText, StringComparison.Ordinal);
		Assert.DoesNotContain(_UNSHIPPED_CAP_CLAIM, artifactText, StringComparison.Ordinal);
		Assert.DoesNotContain(_UNSHIPPED_FALLBACK_CLAIM, artifactText, StringComparison.Ordinal);

		ArtifactDocument document = JsonSerializer.Deserialize(artifactText, ReportJsonContext.Default.ArtifactDocument)!;
		ArtifactCoverageRowDocument cSharpRow = document.Coverage.Languages
			.Single(row => row.Language == _CSHARP_LANGUAGE_NAME);

		Assert.Contains(
			cSharpRow.DeclaredUnsupportedConstructs,
			entry => entry.Reason.Contains(_AGGREGATE_ONLY_CLAIM, StringComparison.Ordinal));
	}

	/// <summary>
	/// The per-end-user gap is not declared: the analyzer recognizes the interactive-user
	/// evidence that gap describes, and recommends the flow for it. The sibling gap about inbound
	/// authentication is declared, so the absence below is not a rendering loss.
	/// </summary>
	[Fact]
	public void TheMarkdownReportNoLongerListsThePerEndUserGapAndStillListsItsSiblings()
	{
		using PipelineHarness harness = PipelineHarness.CreateOverTheEmbeddedCatalog();

		harness.RunTheWholeWorkflow();

		string reportText = harness.ReadWrittenArtifact(OutputArtifactKind.MarkdownReport);

		Assert.DoesNotContain(_PER_END_USER_GAP_CONSTRUCT, reportText, StringComparison.Ordinal);
		Assert.DoesNotContain(_PER_END_USER_GAP_REASON, reportText, StringComparison.Ordinal);
		Assert.Contains(_INBOUND_DIRECTION_GAP_CONSTRUCT, reportText, StringComparison.Ordinal);
	}

	[Fact]
	public void TheJsonArtifactNoLongerListsThePerEndUserGapAndStillListsItsSiblings()
	{
		using PipelineHarness harness = PipelineHarness.CreateOverTheEmbeddedCatalog();

		harness.RunTheWholeWorkflow();

		string artifactText = harness.ReadWrittenArtifact(OutputArtifactKind.StructuredDataArtifact);
		ArtifactDocument document = JsonSerializer.Deserialize(artifactText, ReportJsonContext.Default.ArtifactDocument)!;
		ArtifactCoverageRowDocument cSharpRow = document.Coverage.Languages
			.Single(row => row.Language == _CSHARP_LANGUAGE_NAME);

		Assert.DoesNotContain(
			cSharpRow.DeclaredUnsupportedConstructs,
			entry => entry.Construct.Contains(_PER_END_USER_GAP_CONSTRUCT, StringComparison.Ordinal));
		Assert.Contains(
			cSharpRow.DeclaredUnsupportedConstructs,
			entry => entry.Construct.Contains(_INBOUND_DIRECTION_GAP_CONSTRUCT, StringComparison.Ordinal));
	}

	/// <summary>
	/// A C# file in a legacy single-byte encoding is skipped as undecodable, and the report says so in
	/// the coverage statement rather than leaving the reader to infer it from a count.
	/// </summary>
	[Fact]
	public void TheMarkdownReportDeclaresTheLegacyEncodingGapAndTheOwnEncodingHelperGap()
	{
		using PipelineHarness harness = PipelineHarness.CreateOverTheEmbeddedCatalog();

		harness.RunTheWholeWorkflow();

		string reportText = harness.ReadWrittenArtifact(OutputArtifactKind.MarkdownReport);

		Assert.Contains(_LEGACY_ENCODING_GAP_CONSTRUCT, reportText, StringComparison.Ordinal);
		Assert.Contains(_LEGACY_ENCODING_GAP_REASON, reportText, StringComparison.Ordinal);
		Assert.Contains(_OWN_ENCODING_HELPER_GAP_CONSTRUCT, reportText, StringComparison.Ordinal);
	}

	[Fact]
	public void TheJsonArtifactDeclaresTheLegacyEncodingGapAndTheOwnEncodingHelperGap()
	{
		using PipelineHarness harness = PipelineHarness.CreateOverTheEmbeddedCatalog();

		harness.RunTheWholeWorkflow();

		string artifactText = harness.ReadWrittenArtifact(OutputArtifactKind.StructuredDataArtifact);
		ArtifactDocument document = JsonSerializer.Deserialize(artifactText, ReportJsonContext.Default.ArtifactDocument)!;
		ArtifactCoverageRowDocument cSharpRow = document.Coverage.Languages
			.Single(row => row.Language == _CSHARP_LANGUAGE_NAME);

		Assert.Contains(
			cSharpRow.DeclaredUnsupportedConstructs,
			entry => entry.Construct.Contains(_LEGACY_ENCODING_GAP_CONSTRUCT, StringComparison.Ordinal)
				&& entry.Reason.Contains(_LEGACY_ENCODING_GAP_REASON, StringComparison.Ordinal));
		Assert.Contains(
			cSharpRow.DeclaredUnsupportedConstructs,
			entry => entry.Construct.Contains(_OWN_ENCODING_HELPER_GAP_CONSTRUCT, StringComparison.Ordinal));
	}
}
