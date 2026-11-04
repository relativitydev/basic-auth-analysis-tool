using System.Collections.Immutable;
using System.Text;
using System.Text.Json;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Configuration;
using MigrateAuth.Cli.Findings;
using MigrateAuth.Cli.Reading;
using MigrateAuth.Cli.Reporting;
using MigrateAuth.Cli.Scanning;
using MigrateAuth.Cli.Session;
using MigrateAuth.Cli.Tests.Session;
using MigrateAuth.Cli.Writing;

using Xunit;

namespace MigrateAuth.Cli.Tests.Reporting;

/// <summary>
/// The data artifact: the report's same five sections as structured fields, and the round trip that
/// proves the hand-written field names are the schema and not a guess.
///
/// THE ROUND TRIP IS THE LOAD-BEARING TEST HERE. The document is written field by field with a
/// <see cref="System.Text.Json.Utf8JsonWriter"/> so that the finding set never exists as one
/// object graph - which is what the writing contract requires and what makes a property name a string the writer
/// types rather than a name the serializer derives. Reading it back through the SAME context is
/// what checks those strings against a declared shape; a substring assertion would pass on a
/// document no consumer could parse.
/// </summary>
public sealed class StructuredDataArtifactTests
{
	private static readonly DateOnly _artifactDate = new(2026, 9, 18);

	/// <summary>The alpha label written out, so the assertion does not compare the constant with itself.</summary>
	private const string _EXPECTED_ALPHA_VERSION = "0.4.0-alpha";

	[Fact]
	public void TheProvenanceCarriesTheAlphaLabelledToolVersionLiteral()
	{
		ArtifactDocument document = RenderAndRead(ReportHarness.EmptyContent(
			ReportHarness.CatalogWithCSharpOnly(),
			ReportHarness.ConfigurationWith(ConfidenceLevel.Low)));

		Assert.Equal(_EXPECTED_ALPHA_VERSION, document.Provenance.ToolVersion);
	}

	[Fact]
	public void TheArtifactRoundTripsThroughTheContextThatWroteItAndCarriesTheFiveSections()
	{
		ArtifactDocument document = RenderAndRead(ReportHarness.EmptyContent(
			ReportHarness.CatalogWithCSharpOnly(),
			ReportHarness.ConfigurationWith(ConfidenceLevel.Low)));

		Assert.Equal(ArtifactHandlingNotice.NOTICE_TEXT, document.HandlingNotice);
		Assert.Equal(AssessedLanguages.SUPPORTED_LANGUAGE_COUNT, document.Coverage.Languages.Length);
		Assert.Equal(CoverageStatement.NOT_A_GUARANTEE_OF_ABSENCE, document.Coverage.Statement);
		Assert.Equal(0, document.ScanSummary.FilesEnumerated);
		Assert.Equal(SkippedItemCategories.ALL.Length, document.SkippedItems.Length);
		Assert.Empty(document.Findings);
		Assert.Equal(ToolVersion.CURRENT, document.Provenance.ToolVersion);
		Assert.Equal(LanguageCatalog.CurrentVersion.DisplayValue, document.Provenance.CatalogVersion);
	}

	/// <summary>
	/// The field names are declared as constants and asserted against the document that parsed, so
	/// that a rename on either side shows up here rather than in a consumer's parser.
	/// </summary>
	[Fact]
	public void TheFiveSectionsArePresentUnderTheirDeclaredFieldNames()
	{
		using JsonDocument parsed = JsonDocument.Parse(RenderToText(ReportHarness.EmptyContent(
			ReportHarness.CatalogWithCSharpOnly(),
			ReportHarness.ConfigurationWith(ConfidenceLevel.Low))));

		JsonElement root = parsed.RootElement;

		Assert.True(root.TryGetProperty(ArtifactHandlingNotice.STRUCTURED_DATA_FIELD_NAME, out _));
		Assert.True(root.TryGetProperty(StructuredDataArtifactRenderer.COVERAGE_FIELD_NAME, out _));
		Assert.True(root.TryGetProperty(StructuredDataArtifactRenderer.SCAN_SUMMARY_FIELD_NAME, out _));
		Assert.True(root.TryGetProperty(StructuredDataArtifactRenderer.SKIPPED_ITEMS_FIELD_NAME, out _));
		Assert.True(root.TryGetProperty(StructuredDataArtifactRenderer.FINDINGS_CAP_FIELD_NAME, out _));
		Assert.True(root.TryGetProperty(StructuredDataArtifactRenderer.FINDINGS_FIELD_NAME, out _));
		Assert.True(root.TryGetProperty(StructuredDataArtifactRenderer.PROVENANCE_FIELD_NAME, out _));
	}

	/// <summary>
	/// The coverage section's array, per language, sorted ordinally by
	/// `construct` - see <see cref="ReportHarness.LOWERCASE_FIRST_CONSTRUCT"/> for why declaration
	/// order and ordinal order disagree here, which is what proves the artifact sorts rather than
	/// preserving whatever order the set happened to enumerate in.
	/// </summary>
	[Fact]
	public void TheCoverageArraySortsDeclaredUnsupportedConstructsOrdinally()
	{
		ArtifactDocument document = RenderAndRead(ReportHarness.EmptyContent(
			ReportHarness.CatalogWithDeclaredUnsupportedCoverage(),
			ReportHarness.ConfigurationWith(ConfidenceLevel.Low)));

		ArtifactCoverageRowDocument cSharpRow = document.Coverage.Languages
			.Single(row => row.Language == AssessedLanguages.CanonicalName(AssessedLanguage.CSharp));

		Assert.Equal(2, cSharpRow.DeclaredUnsupportedConstructs.Length);
		Assert.Equal(ReportHarness.UPPERCASE_FIRST_CONSTRUCT, cSharpRow.DeclaredUnsupportedConstructs[0].Construct);
		Assert.Equal(ReportHarness.LOWERCASE_FIRST_CONSTRUCT, cSharpRow.DeclaredUnsupportedConstructs[1].Construct);
		Assert.Equal(ReportHarness.DECLARED_UNSUPPORTED_REASON, cSharpRow.DeclaredUnsupportedConstructs[0].Reason);
	}

	/// <summary>An unsupported language's row carries an empty array rather than an absent field.</summary>
	[Fact]
	public void ALanguageWithNoAnalyzerCarriesAnEmptyDeclaredUnsupportedArray()
	{
		ArtifactDocument document = RenderAndRead(ReportHarness.EmptyContent(
			ReportHarness.CatalogWithDeclaredUnsupportedCoverage(),
			ReportHarness.ConfigurationWith(ConfidenceLevel.Low)));

		ArtifactCoverageRowDocument goRow = document.Coverage.Languages
			.Single(row => row.Language == AssessedLanguages.CanonicalName(AssessedLanguage.Go));

		Assert.Empty(goRow.DeclaredUnsupportedConstructs);
	}

	/// <summary>
	/// The zero case, extended to the findings cap: a run that capped nothing declares that explicitly
	/// rather than by an absent or zeroed-out-by-coincidence field.
	/// </summary>
	[Fact]
	public void AnEmptyRunDeclaresTheFindingsCapUntripped()
	{
		ArtifactDocument document = RenderAndRead(ReportHarness.EmptyContent(
			ReportHarness.CatalogWithCSharpOnly(),
			ReportHarness.ConfigurationWith(ConfidenceLevel.Low)));

		Assert.False(document.FindingsCap.Truncated);
		Assert.Equal(0, document.FindingsCap.TotalRecognized);
		Assert.Equal(0, document.FindingsCap.Retained);
		Assert.Equal(0, document.FindingsCap.PerFileCapTrippedFileCount);
		Assert.False(document.FindingsCap.GlobalCapTripped);
	}

	/// <summary>
	/// The cap invariant, over the artifact: a truncated result declares itself truncated rather
	/// than reading as complete. Every field is asserted so that a consumer reading only this
	/// section, and never the findings array, can still tell how many were omitted and why.
	/// </summary>
	[Fact]
	public void ATruncatedRunDeclaresEveryFieldOfTheCapOutcome()
	{
		FindingsCapOutcome capOutcome = new()
		{
			TotalRecognizedCount = 3_500,
			RetainedCount = 3_000,
			PerFileCapTrippedFileCount = 2,
			PerFileOmittedCount = 400,
			GlobalCapTripped = true,
			GlobalOmittedCount = 100
		};

		ArtifactDocument document = RenderAndRead(ReportHarness.ContentWith(
			ReportHarness.CatalogWithCSharpOnly(),
			ReportHarness.ConfigurationWith(ConfidenceLevel.Low),
			[],
			capOutcome));

		Assert.True(document.FindingsCap.Truncated);
		Assert.Equal(3_500, document.FindingsCap.TotalRecognized);
		Assert.Equal(3_000, document.FindingsCap.Retained);
		Assert.Equal(2, document.FindingsCap.PerFileCapTrippedFileCount);
		Assert.Equal(400, document.FindingsCap.PerFileOmittedCount);
		Assert.True(document.FindingsCap.GlobalCapTripped);
		Assert.Equal(100, document.FindingsCap.GlobalOmittedCount);
	}

	[Fact]
	public void AFindingCarriesItsFieldsAndTheThreeExtensions()
	{
		ImmutableArray<Finding> findings =
		[
			ReportHarness.FindingAt(
				1,
				"folder/source.cs",
				42,
				ReportHarness.TEMPLATED_PATTERN_CLASS,
				ConfidenceLevel.High,
				DestinationCertainty.Established,
				literalCredentialSignal: true)
		];

		ArtifactFindingDocument finding = RenderAndRead(ReportHarness.ContentWith(
			ReportHarness.CatalogWithCSharpOnly(),
			ReportHarness.ConfigurationWith(ConfidenceLevel.Low),
			findings)).Findings.Single();

		Assert.Equal(FindingIdentifiers.Of(1), finding.Id);
		Assert.Equal(1, finding.SequentialIndex);
		Assert.Equal("folder/source.cs", finding.FilePath);
		Assert.Equal(42, finding.Line);
		Assert.Equal(1, finding.Column);
		Assert.Equal(AssessedLanguages.CanonicalName(AssessedLanguage.CSharp), finding.Language);
		Assert.Equal(ReportHarness.TEMPLATED_PATTERN_CLASS, finding.PatternClass);
		Assert.Equal(ConfidenceLevels.Name(ConfidenceLevel.High), finding.Confidence);
		Assert.Equal(ReportVocabulary.DESTINATION_CERTAINTY_ESTABLISHED_VALUE, finding.DestinationCertainty);
		Assert.True(finding.LiteralCredentialSignal);
	}

	/// <summary>
	/// The finding record type has no display-name property at all - the
	/// display name is a report and console concern, and the artifact keys on the
	/// pattern class identifier alone.
	/// </summary>
	[Fact]
	public void TheFindingRecordCarriesNoDisplayNameFieldAndKeysOnTheIdentifierAlone()
	{
		Assert.Null(typeof(ArtifactFindingDocument).GetProperty("DisplayName"));

		ImmutableArray<Finding> findings =
		[
			ReportHarness.FindingAt(1, "source.cs", 3, ReportHarness.TEMPLATED_PATTERN_CLASS, ConfidenceLevel.High)
		];

		ArtifactFindingDocument finding = RenderAndRead(ReportHarness.ContentWith(
			ReportHarness.CatalogWithCSharpOnly(),
			ReportHarness.ConfigurationWith(ConfidenceLevel.Low),
			findings)).Findings.Single();

		Assert.Equal(ReportHarness.TEMPLATED_PATTERN_CLASS, finding.PatternClass);
	}

	/// <summary>
	/// The `recommendation` field: non-null for a High finding, and built from the same four
	/// labeled pieces <see cref="MarkdownReportRenderer"/> renders for the template route, joined
	/// on <see cref="RecommendationRenderer.ARTIFACT_TEXT_PARAGRAPH_SEPARATOR"/>.
	/// </summary>
	[Fact]
	public void TheRecommendationIsCarriedInlineWithItsFourTemplatePiecesAndTheOAuth2ClientBlock()
	{
		ImmutableArray<Finding> findings =
		[
			ReportHarness.FindingAt(1, "source.cs", 3, ReportHarness.TEMPLATED_PATTERN_CLASS, ConfidenceLevel.High)
		];

		ArtifactFindingDocument finding = RenderAndRead(ReportHarness.ContentWith(
			ReportHarness.CatalogWithCSharpOnly(),
			ReportHarness.ConfigurationWith(ConfidenceLevel.Low),
			findings)).Findings.Single();

		string expectedText = string.Join(
			RecommendationRenderer.ARTIFACT_TEXT_PARAGRAPH_SEPARATOR,
			$"{RecommendationRenderer.SOURCE_FORM_LABEL}: {ReportHarness.TEMPLATE_SOURCE_FORM}",
			$"{RecommendationRenderer.TARGET_FORM_LABEL}: {ReportHarness.TEMPLATE_TARGET_FORM}",
			$"{RecommendationRenderer.CREDENTIALS_LABEL}: {ReportHarness.TEMPLATE_CREDENTIAL_PROVENANCE}",
			$"{RecommendationRenderer.VERIFICATION_LABEL}: {ReportHarness.TEMPLATE_VERIFICATION_NOTE}",
			$"{RecommendationComposer.OAUTH2_CLIENT_LABEL}: {OAuth2ClientConfigurationBlocks.For(CredentialFlow.ClientCredentials)}");

		Assert.Equal(expectedText, finding.Recommendation);
		Assert.Null(finding.SecurityNote);
	}

	/// <summary>
	/// The contract for `recommendation`: null for Low, and nothing else stands in for
	/// the manual-review signal.
	/// </summary>
	[Fact]
	public void ALowFindingCarriesANullRecommendation()
	{
		ImmutableArray<Finding> findings =
		[
			ReportHarness.FindingAt(1, "source.cs", 3, ReportHarness.TEMPLATED_PATTERN_CLASS, ConfidenceLevel.Low)
		];

		ArtifactFindingDocument finding = RenderAndRead(ReportHarness.ContentWith(
			ReportHarness.CatalogWithCSharpOnly(),
			ReportHarness.ConfigurationWith(ConfidenceLevel.Low),
			findings)).Findings.Single();

		Assert.Null(finding.Recommendation);
	}

	[Fact]
	public void TheThresholdRemovesRowsFromTheArtifactWithoutRenumberingTheOnesThatStay()
	{
		ImmutableArray<Finding> findings =
		[
			ReportHarness.FindingAt(1, "low.cs", 1, ReportHarness.TEMPLATED_PATTERN_CLASS, ConfidenceLevel.Low),
			ReportHarness.FindingAt(2, "high.cs", 2, ReportHarness.TEMPLATED_PATTERN_CLASS, ConfidenceLevel.High)
		];

		ArtifactDocument document = RenderAndRead(ReportHarness.ContentWith(
			ReportHarness.CatalogWithCSharpOnly(),
			ReportHarness.ConfigurationWith(ConfidenceLevel.Medium),
			findings));

		ArtifactFindingDocument surviving = Assert.Single(document.Findings);

		Assert.Equal(FindingIdentifiers.Of(2), surviving.Id);
		Assert.Equal(2, surviving.SequentialIndex);
	}

	/// <summary>
	/// Both shareable artifacts must carry the handling notice, and the data artifact must
	/// carry it as a FIELD rather than as text prepended to the bytes - which would make the
	/// document invalid for the consumers it exists to serve.
	/// </summary>
	[Fact]
	public void TheArtifactWrittenThroughTheStreamingWriterIsValidJsonWithTheNoticeAsAField()
	{
		using TemporaryFolder repositoryFolder = TemporaryFolder.Create();
		using TemporaryFolder destinationFolder = TemporaryFolder.Create();

		AtomicArtifactWriter writer = new(
			RepositoryFolderPath.FromSuppliedText(repositoryFolder.Path),
			DocumentationDestinationFolderPath.FromSuppliedText(destinationFolder.Path));

		ReportContent content = ReportHarness.EmptyContent(
			ReportHarness.CatalogWithCSharpOnly(),
			ReportHarness.ConfigurationWith(ConfidenceLevel.Low));

		ArtifactWriteResult result = writer.WriteStructuredDataArtifactStreaming(
			_artifactDate,
			stream => StructuredDataArtifactRenderer.Render(stream, content, new UnwiredGenericRecommendationProvider()));

		string written = File.ReadAllText(destinationFolder.Combine(result.FileNameWritten));
		ArtifactDocument? document = JsonSerializer.Deserialize(written, ReportJsonContext.Default.ArtifactDocument);

		Assert.Equal("basic-auth-migration-2026-09-18.json", result.FileNameWritten);
		Assert.StartsWith("{", written, StringComparison.Ordinal);
		Assert.NotNull(document);
		Assert.Equal(ArtifactHandlingNotice.NOTICE_TEXT, document.HandlingNotice);
	}

	/// <summary>
	/// `category` stays the engine's own stable identifier, byte for byte against
	/// <see cref="ReportVocabulary.NameOf(SkippedItemCategory)"/> - a consumer keys on it -
	/// and the `description` field carries the client-facing text beside it rather than replacing it.
	/// </summary>
	[Fact]
	public void EverySkippedItemCarriesItsStableCategoryAlongsideAClientDescription()
	{
		ArtifactDocument document = RenderAndRead(ReportHarness.EmptyContent(
			ReportHarness.CatalogWithCSharpOnly(),
			ReportHarness.ConfigurationWith(ConfidenceLevel.Low)));

		Assert.NotEmpty(document.SkippedItems);

		foreach (ArtifactSkippedItemDocument skippedItem in document.SkippedItems)
		{
			SkippedItemCategory category = SkippedItemCategories.ALL.Single(
				candidate => ReportVocabulary.NameOf(candidate) == skippedItem.Category);

			Assert.Equal(ReportVocabulary.DescriptionOf(category), skippedItem.Description);
			Assert.NotEqual(skippedItem.Category, skippedItem.Description);
		}
	}

	/// <summary>
	/// The two provenance fields of the artifact, alongside the tool version
	/// <see cref="TheArtifactRoundTripsThroughTheContextThatWroteItAndCarriesTheFiveSections"/> already
	/// covers.
	/// </summary>
	[Fact]
	public void TheProvenanceCarriesTheRunDate()
	{
		ReportContent content = ReportHarness.EmptyContent(
			ReportHarness.CatalogWithCSharpOnly(),
			ReportHarness.ConfigurationWith(ConfidenceLevel.Low)) with
		{
			RunDate = new DateOnly(2026, 9, 23)
		};

		ArtifactDocument document = RenderAndRead(content);

		Assert.Equal("2026-09-23", document.Provenance.RunDate);
	}

	/// <summary>
	/// What the repository's own file narrowed is carried in the provenance in this product's own
	/// vocabulary, so a consumer can tell a repository that hid findings from one that had none.
	/// </summary>
	[Fact]
	public void TheProvenanceCarriesWhatTheRepositoryFileChanged()
	{
		ArtifactDocument document = RenderAndRead(ReportHarness.EmptyContent(
			ReportHarness.CatalogWithCSharpOnly(),
			ConfigurationWithRepositoryEffect(new RepositoryConfigurationEffect
			{
				LanguagesSetByRepository = [AssessedLanguage.Python],
				DisabledLanguages = [AssessedLanguage.CSharp, AssessedLanguage.Go],
				EnabledLanguages = [AssessedLanguage.Python],
				ThresholdChange = new ConfidenceLevelChange { Previous = ConfidenceLevel.Low, Current = ConfidenceLevel.High }
			})));

		ArtifactRepositoryConfigurationEffectDocument effect = document.Provenance.RepositoryConfigurationEffect;

		Assert.Equal(["Python"], effect.LanguagesSetByRepository);
		Assert.Equal(["CSharp", "Go"], effect.LanguagesDisabledByRepository);
		Assert.Equal(["Python"], effect.LanguagesEnabledByRepository);
		Assert.Equal("Low", effect.ThresholdChange!.Previous);
		Assert.Equal("High", effect.ThresholdChange.Current);
		Assert.Equal("Raised", effect.ThresholdChange.Direction);
	}

	[Fact]
	public void TheProvenanceOfARunWithoutARepositoryChangeCarriesAnEmptyEffect()
	{
		ArtifactDocument document = RenderAndRead(ReportHarness.EmptyContent(
			ReportHarness.CatalogWithCSharpOnly(),
			ReportHarness.ConfigurationWith(ConfidenceLevel.Low)));

		Assert.Empty(document.Provenance.RepositoryConfigurationEffect.LanguagesDisabledByRepository);
		Assert.Empty(document.Provenance.RepositoryConfigurationEffect.LanguagesEnabledByRepository);
		Assert.Null(document.Provenance.RepositoryConfigurationEffect.ThresholdChange);
	}

	private static EffectiveConfiguration ConfigurationWithRepositoryEffect(RepositoryConfigurationEffect effect)
	{
		EffectiveConfiguration baseline = ReportHarness.ConfigurationWith(ConfidenceLevel.High);

		return baseline with { Provenance = baseline.Provenance with { RepositoryEffect = effect } };
	}

	/// <summary>
	/// The `assessed` field has three states. A row for a
	/// language the catalog ships an analyzer for reads `assessed: true`; a row for one it does not -
	/// `VBNet`, absent from <see cref="ReportHarness.CatalogWithCSharpOnly"/> - reads
	/// `assessed: false`; and a row for <see cref="LanguageAttribution.UNRECOGNIZED_LANGUAGE_NAME"/>,
	/// which was never searched at all, reads `assessed: null`.
	/// </summary>
	[Fact]
	public void ScanSummaryRowsCarryTheAssessedFieldFromTheCatalog()
	{
		LanguageInventoryRow assessedRow = new()
		{
			LanguageName = AssessedLanguages.CanonicalName(AssessedLanguage.CSharp),
			FileCount = 1,
			LineCount = 10,
			CandidateFileCount = 1,
			ShareOfFilesPercentage = 50.0
		};

		LanguageInventoryRow unassessedRow = new()
		{
			LanguageName = AssessedLanguages.CanonicalName(AssessedLanguage.VBNet),
			FileCount = 1,
			LineCount = 5,
			CandidateFileCount = 0,
			ShareOfFilesPercentage = 50.0
		};

		LanguageInventoryRow unrecognizedRow = new()
		{
			LanguageName = LanguageAttribution.UNRECOGNIZED_LANGUAGE_NAME,
			FileCount = 1,
			LineCount = 2,
			CandidateFileCount = 0,
			ShareOfFilesPercentage = 0.0
		};

		ReportContent content = ReportHarness.EmptyContent(
			ReportHarness.CatalogWithCSharpOnly(),
			ReportHarness.ConfigurationWith(ConfidenceLevel.Low)) with
		{
			ScanSummary = new ScanSummary
			{
				FileCount = 3,
				LineCount = 17,
				CandidateFileCount = 1,
				Rows = [assessedRow, unassessedRow, unrecognizedRow]
			}
		};

		ArtifactDocument document = RenderAndRead(content);

		ArtifactInventoryRowDocument cSharpRow = document.ScanSummary.Languages
			.Single(row => row.Language == AssessedLanguages.CanonicalName(AssessedLanguage.CSharp));
		ArtifactInventoryRowDocument vbNetRow = document.ScanSummary.Languages
			.Single(row => row.Language == AssessedLanguages.CanonicalName(AssessedLanguage.VBNet));
		ArtifactInventoryRowDocument unrecognizedArtifactRow = document.ScanSummary.Languages
			.Single(row => row.Language == LanguageAttribution.UNRECOGNIZED_LANGUAGE_NAME);

		Assert.Equal(true, cSharpRow.Assessed);
		Assert.Equal(false, vbNetRow.Assessed);
		Assert.Null(unrecognizedArtifactRow.Assessed);
	}

	private static ArtifactDocument RenderAndRead(ReportContent content)
	{
		ArtifactDocument? document = JsonSerializer.Deserialize(
			RenderToText(content),
			ReportJsonContext.Default.ArtifactDocument);

		Assert.NotNull(document);

		return document;
	}

	private static string RenderToText(ReportContent content)
	{
		using MemoryStream stream = new();

		StructuredDataArtifactRenderer.Render(stream, content, new UnwiredGenericRecommendationProvider());

		return Encoding.UTF8.GetString(stream.ToArray());
	}
}
