using System.Collections.Immutable;
using System.Globalization;
using System.Text.Json;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Configuration;
using MigrateAuth.Cli.Emission;
using MigrateAuth.Cli.Findings;
using MigrateAuth.Cli.Reading;
using MigrateAuth.Cli.Scanning;
using MigrateAuth.Cli.Writing;

namespace MigrateAuth.Cli.Reporting;

/// <summary>
/// THE DATA ARTIFACT: THE REPORT'S SAME FIVE SECTIONS, AS STRUCTURED FIELDS, WRITTEN
/// STREAMING.
///
/// The coverage statement is a field rather than prose, so a consumer can read the catalog's
/// declared scope. The pattern class is carried as its identifier rather than its name, because a
/// consumer keys on that field and a name is prose that gets improved.
///
/// THE FINDINGS ARRAY IS OPENED AND CLOSED BY HAND, ON PURPOSE. Serializing the whole document in
/// one call would build the entire object graph - a hundred thousand finding records, each with a
/// rendered recommendation - before a single byte reached the file. Writing the array
/// element by element means one finding document exists at a time and is collectable the moment
/// the next iteration begins.
///
/// THE OTHER FOUR SECTIONS ARE SERIALIZED WHOLE, AND THAT IS NOT AN INCONSISTENCY. Every one of
/// them is bounded by the binary rather than by the repository: eight coverage rows, one inventory
/// row per distinct language, eleven skipped-item categories at a hundred paths each, one provenance
/// record. None of them grows with the size of the assessed repository, which is the property that
/// made the findings array the one that had to stream.
///
/// THE HANDLING NOTICE IS A FIELD AND NEVER A PREFIX. Both artifacts must carry it;
/// the Markdown report takes it as a blockquote first line and this one takes it as
/// <see cref="ArtifactHandlingNotice.STRUCTURED_DATA_FIELD_NAME"/>, because text prepended to the
/// bytes would make the document invalid for the consumers it exists to serve.
/// </summary>
public static class StructuredDataArtifactRenderer
{
	/// <summary>Report section 1, as its field name.</summary>
	public const string COVERAGE_FIELD_NAME = "coverage";

	/// <summary>Report section 2, as its field name.</summary>
	public const string SCAN_SUMMARY_FIELD_NAME = "scanSummary";

	/// <summary>Report section 3, as its field name.</summary>
	public const string SKIPPED_ITEMS_FIELD_NAME = "skippedItems";

	/// <summary>
	/// The truncation counter, as its field name. Carried beside <see cref="FINDINGS_FIELD_NAME"/>
	/// rather than folded into it, because it describes findings that are not in that array.
	/// </summary>
	public const string FINDINGS_CAP_FIELD_NAME = "findingsCap";

	/// <summary>Report section 4, as its field name.</summary>
	public const string FINDINGS_FIELD_NAME = "findings";

	/// <summary>Report section 5, as its field name.</summary>
	public const string PROVENANCE_FIELD_NAME = "provenance";

	/// <summary><see cref="ArtifactInventoryRowDocument.NotAssessedReason"/>'s value for a language this release ships no analyzer for.</summary>
	private const string _NOT_ASSESSED_REASON_NO_ANALYZER = "noAnalyzer";

	/// <summary><see cref="ArtifactInventoryRowDocument.NotAssessedReason"/>'s value for a language this session's configuration disabled.</summary>
	private const string _NOT_ASSESSED_REASON_DISABLED_BY_CONFIGURATION = "disabledByConfiguration";

	/// <summary>
	/// Written without indentation. The document is read by programs rather than
	/// by a person, and at the volume this renderer is built for the whitespace is not free.
	/// </summary>
	private static readonly JsonWriterOptions _writerOptions = new() { Indented = false };

	/// <summary>
	/// Writes the whole artifact into the supplied stream. The stream is the caller's - it comes
	/// from <see cref="AtomicArtifactWriter.WriteStructuredDataArtifactStreaming"/>, already opened
	/// with the restrictive permissions both shareable artifacts carry - and this method neither
	/// opens nor closes it.
	/// </summary>
	public static void Render(
		Stream stream,
		ReportContent content,
		IGenericRecommendationProvider genericRecommendations)
	{
		ArgumentNullException.ThrowIfNull(stream);
		ArgumentNullException.ThrowIfNull(content);
		ArgumentNullException.ThrowIfNull(genericRecommendations);

		RecommendationRenderer recommendations = new(content.Catalog, genericRecommendations);

		using Utf8JsonWriter writer = new(stream, _writerOptions);

		writer.WriteStartObject();
		writer.WriteString(ArtifactHandlingNotice.STRUCTURED_DATA_FIELD_NAME, ArtifactHandlingNotice.NOTICE_TEXT);

		WriteSection(writer, COVERAGE_FIELD_NAME, BuildCoverage(content), ReportJsonContext.Default.ArtifactCoverageDocument);
		WriteSection(writer, SCAN_SUMMARY_FIELD_NAME, BuildScanSummary(content), ReportJsonContext.Default.ArtifactScanSummaryDocument);
		WriteSkippedItems(writer, content);
		WriteSection(writer, FINDINGS_CAP_FIELD_NAME, BuildFindingsCap(content), ReportJsonContext.Default.ArtifactFindingsCapDocument);
		WriteFindings(writer, content, recommendations);
		WriteSection(writer, PROVENANCE_FIELD_NAME, BuildProvenance(content), ReportJsonContext.Default.ArtifactProvenanceDocument);

		writer.WriteEndObject();
		writer.Flush();
	}

	private static void WriteSection<TSection>(
		Utf8JsonWriter writer,
		string fieldName,
		TSection section,
		System.Text.Json.Serialization.Metadata.JsonTypeInfo<TSection> typeInfo)
	{
		writer.WritePropertyName(fieldName);
		JsonSerializer.Serialize(writer, section, typeInfo);
	}

	private static void WriteSkippedItems(Utf8JsonWriter writer, ReportContent content)
	{
		writer.WritePropertyName(SKIPPED_ITEMS_FIELD_NAME);
		writer.WriteStartArray();

		foreach (SkippedItemTally tally in content.SkippedItems)
		{
			JsonSerializer.Serialize(
				writer,
				BuildSkippedItem(tally),
				ReportJsonContext.Default.ArtifactSkippedItemDocument);
		}

		writer.WriteEndArray();
	}

	/// <summary>
	/// The one array that grows with the repository, and therefore the one written element by
	/// element. The threshold decides which findings appear and never which were assigned a
	/// level; the identifiers were assigned over the full set before it, so the sequence here
	/// may show gaps.
	/// </summary>
	private static void WriteFindings(Utf8JsonWriter writer, ReportContent content, RecommendationRenderer recommendations)
	{
		writer.WritePropertyName(FINDINGS_FIELD_NAME);
		writer.WriteStartArray();

		foreach (Finding finding in ThresholdFilter.Apply(
			content.Findings,
			content.Configuration.MinimumConfidenceLevel))
		{
			JsonSerializer.Serialize(
				writer,
				BuildFinding(finding, recommendations),
				ReportJsonContext.Default.ArtifactFindingDocument);
		}

		writer.WriteEndArray();
	}

	private static ArtifactCoverageDocument BuildCoverage(ReportContent content)
	{
		ImmutableArray<LanguageCoverageRow> rows = content.CoverageRows;

		return new ArtifactCoverageDocument
		{
			Statement = CoverageStatement.NOT_A_GUARANTEE_OF_ABSENCE,
			Languages =
			[
				.. rows.Select(row => new ArtifactCoverageRowDocument
				{
					Language = row.LanguageName,
					Coverage = CoverageStatement.Describe(row),
					PatternClassesSearched = row.PatternClassesSearched,
					DeclaredUnsupportedConstructs = [.. row.DeclaredUnsupportedConstructs.Select(BuildDeclaredUnsupportedConstruct)]
				})
			]
		};
	}

	/// <summary>
	/// Already sorted ordinally by <see cref="LanguageCoverageRow.DeclaredUnsupportedConstructs"/>'s
	/// own contract, so nothing is sorted here.
	/// </summary>
	private static ArtifactDeclaredUnsupportedConstructDocument BuildDeclaredUnsupportedConstruct(
		DeclaredUnsupportedCoverage entry)
	{
		return new ArtifactDeclaredUnsupportedConstructDocument
		{
			Construct = entry.Construct,
			Reason = entry.Reason
		};
	}

	/// <summary>The truncation counter, as the structured field a consumer reads before the array.</summary>
	private static ArtifactFindingsCapDocument BuildFindingsCap(ReportContent content)
	{
		FindingsCapOutcome capOutcome = content.FindingsCapOutcome;

		return new ArtifactFindingsCapDocument
		{
			Truncated = capOutcome.IsTruncated,
			TotalRecognized = capOutcome.TotalRecognizedCount,
			Retained = capOutcome.RetainedCount,
			PerFileCapTrippedFileCount = capOutcome.PerFileCapTrippedFileCount,
			PerFileOmittedCount = capOutcome.PerFileOmittedCount,
			GlobalCapTripped = capOutcome.GlobalCapTripped,
			GlobalOmittedCount = capOutcome.GlobalOmittedCount
		};
	}

	/// <summary>
	/// Section 2's rows read the PERSISTED, scan-time enabled set - <see cref="ReportContent.ScanEnabledLanguages"/> -
	/// rather than <see cref="ReportContent.Configuration"/>'s current one, on the same terms
	/// <see cref="ReportContent.ScanEnabledLanguages"/>'s own doc comment gives.
	/// </summary>
	private static ArtifactScanSummaryDocument BuildScanSummary(ReportContent content)
	{
		ScanSummary summary = content.ScanSummary;
		IReadOnlySet<AssessedLanguage> scanEnabledLanguages = content.ScanEnabledLanguages.ToHashSet();

		return new ArtifactScanSummaryDocument
		{
			FilesEnumerated = summary.FileCount,
			LinesCounted = summary.LineCount,
			CandidateFiles = summary.CandidateFileCount,
			Languages = [.. summary.Rows.Select(row => BuildInventoryRow(row, content.Catalog, scanEnabledLanguages))],
			FirstLineFallbackFiles = summary.FirstLineFallbackCount,
			EnabledLanguagesDifferFromSession = content.ScanEnabledLanguagesDifferFromSession,
			ExcludedFolders = new ArtifactExcludedFoldersDocument
			{
				Count = summary.ExcludedFolders.OccurrenceCount,
				OmittedPathCount = summary.ExcludedFolders.OmittedPathCount,
				Paths =
				[
					.. summary.ExcludedFolders.Paths.Select(path =>
						EmittedPathText.ForDestination(path, EmissionDestination.StructuredData))
				]
			},
			LegacyEncodedFiles = new ArtifactLegacyEncodedFilesDocument
			{
				Encoding = ReportVocabulary.WINDOWS_1252_ENCODING_NAME,
				Count = summary.LegacyEncodedFiles.OccurrenceCount,
				OmittedPathCount = summary.LegacyEncodedFiles.OmittedPathCount,
				Paths =
				[
					.. summary.LegacyEncodedFiles.Paths.Select(path =>
						EmittedPathText.ForDestination(path, EmissionDestination.StructuredData))
				]
			}
		};
	}

	/// <summary>One inventory row, including <see cref="ArtifactInventoryRowDocument.NotAssessedReason"/>.</summary>
	private static ArtifactInventoryRowDocument BuildInventoryRow(
		LanguageInventoryRow row,
		LanguageCatalog catalog,
		IReadOnlySet<AssessedLanguage> scanEnabledLanguages)
	{
		LanguageAssessmentState state = LanguageAssessment.StateOf(catalog, row.LanguageName, scanEnabledLanguages);

		return new ArtifactInventoryRowDocument
		{
			Language = row.LanguageName,
			Files = row.FileCount,
			Lines = row.LineCount,
			CandidateFiles = row.CandidateFileCount,
			ShareOfFilesPercentage = row.ShareOfFilesPercentage,
			Assessed = DescribeAssessedField(state),
			NotAssessedReason = DescribeNotAssessedReason(state)
		};
	}

	/// <summary>
	/// The nullable-bool encoding of
	/// <see cref="LanguageAssessmentState"/> that <see cref="ArtifactInventoryRowDocument.Assessed"/>
	/// declares.
	/// </summary>
	private static bool? DescribeAssessedField(LanguageAssessmentState state)
	{
		bool? assessed = state switch
		{
			LanguageAssessmentState.Assessed => true,
			LanguageAssessmentState.NotAssessed => false,
			LanguageAssessmentState.DisabledByConfiguration => false,
			_ => null
		};

		return assessed;
	}

	/// <summary>The closed vocabulary <see cref="ArtifactInventoryRowDocument.NotAssessedReason"/> declares.</summary>
	private static string? DescribeNotAssessedReason(LanguageAssessmentState state)
	{
		string? reason = state switch
		{
			LanguageAssessmentState.NotAssessed => _NOT_ASSESSED_REASON_NO_ANALYZER,
			LanguageAssessmentState.DisabledByConfiguration => _NOT_ASSESSED_REASON_DISABLED_BY_CONFIGURATION,
			_ => null
		};

		return reason;
	}

	/// <summary>
	/// A tally's paths are handed to the encoder unescaped: the data artifact has its own
	/// encoder, and escaping before it would double-escape.
	/// </summary>
	private static ArtifactSkippedItemDocument BuildSkippedItem(SkippedItemTally tally)
	{
		return new ArtifactSkippedItemDocument
		{
			Category = ReportVocabulary.NameOf(tally.Category),
			Description = ReportVocabulary.DescriptionOf(tally.Category),
			Unit = ReportVocabulary.NameOf(tally.Unit),
			Count = tally.Count,
			OmittedPathCount = tally.OmittedPathCount,
			Paths =
			[
				.. tally.EnumeratedPaths.Select(path =>
					EmittedPathText.ForDestination(path, EmissionDestination.StructuredData))
			]
		};
	}

	private static ArtifactFindingDocument BuildFinding(Finding finding, RecommendationRenderer recommendations)
	{
		RenderedRecommendation rendered = recommendations.Render(finding, EmissionDestination.StructuredData);

		return new ArtifactFindingDocument
		{
			Id = finding.Identifier,
			SequentialIndex = finding.SequentialIndex,
			FilePath = EmittedPathText.ForDestination(finding.Path, EmissionDestination.StructuredData),
			Line = finding.Line,
			Column = finding.Column,
			Language = AssessedLanguages.CanonicalName(finding.Language),
			PatternClass = finding.PatternClass.Value,
			Confidence = ConfidenceLevels.Name(finding.Level),
			DestinationCertainty = ReportVocabulary.ValueOf(finding.DestinationCertainty),
			LiteralCredentialSignal = finding.LiteralCredentialSignal,
			TestCode = finding.TestCode,
			Fingerprint = finding.Fingerprint.Text,
			Recommendation = RecommendationRenderer.ComposeArtifactText(rendered),
			SecurityNote = rendered.SecurityNote.Length == 0 ? null : rendered.SecurityNote,
			Flow = finding.Flow is CredentialFlow flow ? CredentialFlows.Name(flow) : null,
			FlowDecision = finding.FlowDecision is FlowDecision decision ? CredentialFlows.Name(decision) : null,
			FlowNote = NoteOf(finding),
			LowReasons = finding.Level == ConfidenceLevel.Low ? LowReasonVocabulary.ValuesOf(finding.LowReasons) : null
		};
	}

	/// <summary>
	/// The note is derived here from the decision and the flow the finding carries, and is
	/// null where either is: a Low finding has neither, and a finding that has not been through
	/// assignment must not invent a note.
	/// </summary>
	private static string? NoteOf(Finding finding)
	{
		return finding.Flow is CredentialFlow flow && finding.FlowDecision is FlowDecision decision
			? FlowNotes.NoteFor(decision, flow)
			: null;
	}

	private static ArtifactProvenanceDocument BuildProvenance(ReportContent content)
	{
		ConfigurationProvenance provenance = content.Configuration.Provenance;

		return new ArtifactProvenanceDocument
		{
			RunDate = content.RunDate.ToString(OutputArtifactNaming.DATE_FORMAT, CultureInfo.InvariantCulture),
			ToolVersion = ToolVersion.CURRENT,
			CatalogVersion = content.Catalog.Version.DisplayValue,
			RepositoryLabel = content.RepositoryLabel,
			EnabledLanguages =
			[
				.. content.Configuration.LanguageEnablement
					.Where(enablement => enablement.IsEnabled)
					.Select(enablement => AssessedLanguages.CanonicalName(enablement.Language))
			],
			MinimumConfidenceLevel = ConfidenceLevels.Name(content.Configuration.MinimumConfidenceLevel),
			MinimumConfidenceLevelOrigin = ReportVocabulary.NameOf(provenance.MinimumConfidenceLevelOrigin),
			EnabledLanguagesOrigin = ReportVocabulary.NameOf(provenance.EnabledLanguagesOrigin),
			OutputPathOrigin = ReportVocabulary.NameOf(provenance.OutputPathOrigin),
			DefaultFlow = CredentialFlows.Name(content.Configuration.DefaultFlow),
			DefaultFlowOrigin = ReportVocabulary.DefaultFlowOriginValueOf(provenance.DefaultFlowOrigin),
			ConfigurationFiles = [.. provenance.ConfigurationFiles.Select(BuildConfigurationFile)],
			IgnoredRepositoryFields = [.. provenance.IgnoredRepositoryFields.Select(BuildIgnoredField)],
			RepositoryConfigurationEffect = BuildRepositoryEffect(provenance.RepositoryEffect, content.Findings)
		};
	}

	private static ArtifactRepositoryConfigurationEffectDocument BuildRepositoryEffect(
		RepositoryConfigurationEffect effect,
		ImmutableArray<Finding> findings)
	{
		return new ArtifactRepositoryConfigurationEffectDocument
		{
			LanguagesSetByRepository = [.. effect.LanguagesSetByRepository.Select(AssessedLanguages.CanonicalName)],
			LanguagesDisabledByRepository = [.. effect.DisabledLanguages.Select(AssessedLanguages.CanonicalName)],
			LanguagesEnabledByRepository = [.. effect.EnabledLanguages.Select(AssessedLanguages.CanonicalName)],
			ThresholdChange = effect.ThresholdChange is ConfidenceLevelChange change
				? new ArtifactThresholdChangeDocument
				{
					Previous = ConfidenceLevels.Name(change.Previous),
					Current = ConfidenceLevels.Name(change.Current),
					Direction = ReportVocabulary.DirectionOf(change),
					HiddenFindingCount = ThresholdFilter.CountHiddenByRaise(findings, change)
				}
				: null
		};
	}

	private static ArtifactConfigurationFileDocument BuildConfigurationFile(ConfigurationFileState file)
	{
		return new ArtifactConfigurationFileDocument
		{
			Location = ReportVocabulary.NameOf(file.Location),
			Condition = ReportVocabulary.NameOf(file.Condition)
		};
	}

	private static ArtifactIgnoredConfigurationFieldDocument BuildIgnoredField(IgnoredRepositoryConfigurationField field)
	{
		return new ArtifactIgnoredConfigurationFieldDocument
		{
			Field = ReportVocabulary.NameOf(field.Field),
			Reason = ReportVocabulary.NameOf(field.Reason),
			WouldHaveBeen = field.WouldHaveBeen
		};
	}
}
