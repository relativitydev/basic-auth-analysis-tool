using System.Globalization;

using MigrateAuth.Cli.Reporting;

namespace MigrateAuth.Cli.Agent;

/// <summary>
/// THE BODY OF THE PROMPT PACK, streamed to the writer the artifact writer hands over.
///
/// The artifact writer puts the handling header first; this writes the remaining blocks in the order the
/// pack is specified: (1) the title, (2) the review label, (3) the cost notice, (4) the statement that the
/// entries are data, (5) the instructions with the report's bare name and the review and flow requests,
/// (6) the checklist, (7) the counts with the minimum confidence level, (8) the three sections. Each
/// section begins with the instruction text of every kind present in it, in a fixed order, and then lists
/// its entries in the report's order. Entries are written one at a time from the scope's lazy pass, so
/// nothing about the findings is held.
///
/// The text is identical on every platform: "\n" line endings, the invariant culture, no clock, no
/// absolute path and no configuration value. The same body is written again for the launch copy, which is
/// why that determinism is a requirement and not a nicety.
/// </summary>
public static class PromptPackWriter
{
	/// <summary>The line ending of the pack body, and of any text appended to the report, the same on every platform.</summary>
	public const string LINE_ENDING = "\n";

	/// <summary>The kinds in the order their texts open a section.</summary>
	private static readonly RecommendationKind[] _kindOrder =
	[
		RecommendationKind.ManualReview,
		RecommendationKind.Generic,
		RecommendationKind.FromTemplate
	];

	/// <summary>
	/// Writes the body after the handling header.
	/// <paramref name="reportFileName"/> is the report's bare file name, or null when this run wrote none.
	/// </summary>
	public static void WriteBody(TextWriter writer, AgentEnvironment environment, PromptPackScope scope, string? reportFileName)
	{
		ArgumentNullException.ThrowIfNull(writer);
		ArgumentNullException.ThrowIfNull(scope);

		writer.NewLine = LINE_ENDING;

		WriteOpening(writer, environment, scope);
		WriteInstructions(writer, reportFileName);
		WriteChecklist(writer);
		WriteCounts(writer, scope);

		WriteSection(writer, scope, PromptPackSection.AmbiguousCases, AiAssistTexts.PACK_SECTION_AMBIGUOUS_FORMAT);
		WriteSection(writer, scope, PromptPackSection.DefaultedFlow, AiAssistTexts.PACK_SECTION_DEFAULTED_FORMAT);
		WriteSection(writer, scope, PromptPackSection.ForConfirmation, AiAssistTexts.PACK_SECTION_CONFIRMATION_FORMAT);
	}

	private static void WriteOpening(TextWriter writer, AgentEnvironment environment, PromptPackScope scope)
	{
		writer.WriteLine(string.Format(
			CultureInfo.InvariantCulture,
			AiAssistTexts.PACK_TITLE_FORMAT,
			AgentEnvironmentCatalog.Describe(environment).DisplayName));
		writer.WriteLine();
		writer.WriteLine(AiAssistTexts.PACK_REVIEW_LABEL_EMPHASIZED);
		writer.WriteLine();
		writer.WriteLine(AiAssistTexts.PACK_COST_NOTICE_HEADING);
		writer.WriteLine();
		writer.WriteLine(AiAssistTexts.CostNotice(environment, scope.TotalCount));
		writer.WriteLine();
		writer.WriteLine(AiAssistTexts.PACK_DATA_HEADING);
		writer.WriteLine();
		writer.WriteLine(AiAssistTexts.PACK_DATA_STATEMENT);
		writer.WriteLine();
	}

	private static void WriteInstructions(TextWriter writer, string? reportFileName)
	{
		writer.WriteLine(AiAssistTexts.PACK_INSTRUCTIONS_HEADING);
		writer.WriteLine();
		writer.WriteLine(AiAssistTexts.PACK_WORKING_DIRECTORY);
		writer.WriteLine(reportFileName is null
			? AiAssistTexts.PACK_NO_REPORT
			: string.Format(CultureInfo.InvariantCulture, AiAssistTexts.PACK_REPORT_NAMED_FORMAT, reportFileName));
		writer.WriteLine(AiAssistTexts.PACK_TASK);
		writer.WriteLine(AiAssistTexts.PACK_RETURN_FORMAT);
		WriteFindingFormat(writer);
		writer.WriteLine(AiAssistTexts.PACK_READ_ONLY);
		writer.WriteLine(AiAssistTexts.PACK_NO_CREDENTIAL_VALUES);
		writer.WriteLine(AiAssistTexts.PACK_FLOW_REQUEST);
		writer.WriteLine();
	}

	/// <summary>The example of a returned finding, as a fenced block, with the review label as its last line.</summary>
	private static void WriteFindingFormat(TextWriter writer)
	{
		writer.WriteLine(AiAssistTexts.PACK_FORMAT_INTRODUCTION);
		writer.WriteLine();
		writer.WriteLine(AiAssistTexts.PACK_FORMAT_FENCE_OPENING);
		writer.WriteLine(AiAssistTexts.PACK_FORMAT_HEADER_LINE);
		writer.WriteLine(AiAssistTexts.PACK_FORMAT_REASON_LINE);
		writer.WriteLine(AiAssistTexts.REVIEW_LABEL);
		writer.WriteLine(AiAssistTexts.PACK_FORMAT_FENCE_CLOSING);
		writer.WriteLine();
	}

	private static void WriteChecklist(TextWriter writer)
	{
		writer.WriteLine(AiAssistTexts.PACK_CHECKLIST_HEADING);
		writer.WriteLine();
		writer.WriteLine(AiAssistTexts.PACK_CHECKLIST_REAL);
		writer.WriteLine(AiAssistTexts.PACK_CHECKLIST_FLOW);
		writer.WriteLine(AiAssistTexts.PACK_CHECKLIST_UNCOVERED);
		writer.WriteLine(AiAssistTexts.PACK_CHECKLIST_LABEL_IN_EVERY_FINDING);
		writer.WriteLine(AiAssistTexts.PACK_CHECKLIST_UNCHANGED);
		writer.WriteLine(AiAssistTexts.PACK_CHECKLIST_NO_CREDENTIAL_VALUES);
		writer.WriteLine();
	}

	private static void WriteCounts(TextWriter writer, PromptPackScope scope)
	{
		writer.WriteLine(AiAssistTexts.PACK_COUNTS_HEADING);
		writer.WriteLine();
		writer.WriteLine(string.Format(
			CultureInfo.InvariantCulture,
			AiAssistTexts.PACK_COUNTS_FORMAT,
			scope.TotalCount,
			scope.CountOf(PromptPackSection.AmbiguousCases),
			scope.CountOf(PromptPackSection.DefaultedFlow),
			scope.CountOf(PromptPackSection.ForConfirmation)));
		writer.WriteLine(string.Format(
			CultureInfo.InvariantCulture,
			AiAssistTexts.PACK_MINIMUM_LEVEL_FORMAT,
			AiAssistTexts.LevelText(scope.MinimumLevel)));
		writer.WriteLine();
	}

	private static void WriteSection(TextWriter writer, PromptPackScope scope, PromptPackSection section, string headingFormat)
	{
		writer.WriteLine(string.Format(CultureInfo.InvariantCulture, headingFormat, scope.CountOf(section)));
		writer.WriteLine();

		foreach (RecommendationKind kind in _kindOrder.Where(candidate => scope.ContainsKind(section, candidate)))
		{
			writer.WriteLine(string.Format(
				CultureInfo.InvariantCulture,
				AiAssistTexts.PACK_KIND_TEXT_FORMAT,
				AiAssistTexts.KindLabel(kind),
				PromptPackInstructionText.Describe(kind)));
			writer.WriteLine();
		}

		foreach (PromptPackFindingEntry entry in scope.EntriesOf(section))
		{
			writer.WriteLine(FormatEntry(entry));
		}

		writer.WriteLine();
	}

	private static string FormatEntry(PromptPackFindingEntry entry)
	{
		return string.Format(
			CultureInfo.InvariantCulture,
			AiAssistTexts.PACK_ENTRY_FORMAT,
			entry.Identifier,
			entry.PatternClass.Value,
			AiAssistTexts.LevelText(entry.Level),
			AiAssistTexts.FlowText(entry.Flow),
			entry.FlowWasDefaulted ? AiAssistTexts.DEFAULT_FLOW_MARK : AiAssistTexts.NO_DEFAULT_FLOW_MARK,
			AiAssistTexts.KindLabel(entry.Guidance),
			entry.Line,
			PackPathText.Render(entry.Path));
	}
}
