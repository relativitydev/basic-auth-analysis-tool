using MigrateAuth.Cli.Agent;
using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Reporting;

using Xunit;

namespace MigrateAuth.Cli.Tests.Agent;

/// <summary>
/// THE PACK'S BLOCKS, IN THEIR ORDER. Each block is located by its fixed text and the positions must rise: a block
/// moved, dropped or doubled fails here. The instruction texts must open their section, before any entry, and the
/// entries must follow in the report's order.
/// </summary>
public sealed class PromptPackWriterTests
{
	private const char _CARRIAGE_RETURN = '\r';
	private const string _SECTION_ONE_HEADING = "## Section 1";
	private const string _SECTION_TWO_HEADING = "## Section 2";
	private const string _SECTION_THREE_HEADING = "## Section 3";
	private const string _ENTRY_PREFIX = "- ";
	private const string _FIELD_SEPARATOR = " | ";
	private const string _ENTRY_LINE_START = $"{AiAssistHarness.LINE_FEED}{_ENTRY_PREFIX}";
	private const string _ZERO_ENTRY_LINE_START = $"{AiAssistHarness.LINE_FEED}- 0";

	[Theory]
	[InlineData(AgentEnvironment.ClaudeCode)]
	[InlineData(AgentEnvironment.OpenAiCodex)]
	[InlineData(AgentEnvironment.GitHubCopilotCli)]
	public void ThePackAsksTheAgentNotToReproduceACredentialValueAndToCheckIt(AgentEnvironment environment)
	{
		string body = PromptPackFixture.Body(environment, PromptPackFixture.Scope());
		int instructions = body.IndexOf(AiAssistTexts.PACK_INSTRUCTIONS_HEADING, StringComparison.Ordinal);
		int request = body.IndexOf(AiAssistTexts.PACK_NO_CREDENTIAL_VALUES, StringComparison.Ordinal);
		int checklist = body.IndexOf(AiAssistTexts.PACK_CHECKLIST_HEADING, StringComparison.Ordinal);
		int check = body.IndexOf(AiAssistTexts.PACK_CHECKLIST_NO_CREDENTIAL_VALUES, StringComparison.Ordinal);

		Assert.True(instructions >= 0 && instructions < request && request < checklist && checklist < check);
	}

	[Theory]
	[InlineData(AgentEnvironment.ClaudeCode)]
	[InlineData(AgentEnvironment.OpenAiCodex)]
	[InlineData(AgentEnvironment.GitHubCopilotCli)]
	public void ThePackShowsTheFormOfAReturnedFindingEndingWithTheReviewLabel(AgentEnvironment environment)
	{
		string body = PromptPackFixture.Body(environment, PromptPackFixture.Scope()).Replace(_CARRIAGE_RETURN.ToString(), string.Empty, StringComparison.Ordinal);
		string example = string.Join(
			AiAssistHarness.LINE_FEED,
			AiAssistTexts.PACK_FORMAT_FENCE_OPENING,
			AiAssistTexts.PACK_FORMAT_HEADER_LINE,
			AiAssistTexts.PACK_FORMAT_REASON_LINE,
			AiAssistTexts.REVIEW_LABEL,
			AiAssistTexts.PACK_FORMAT_FENCE_CLOSING);
		int returnFormat = body.IndexOf(AiAssistTexts.PACK_RETURN_FORMAT, StringComparison.Ordinal);
		int introduction = body.IndexOf(AiAssistTexts.PACK_FORMAT_INTRODUCTION, StringComparison.Ordinal);
		int block = body.IndexOf(example, StringComparison.Ordinal);
		int readOnly = body.IndexOf(AiAssistTexts.PACK_READ_ONLY, StringComparison.Ordinal);

		Assert.True(returnFormat >= 0 && returnFormat < introduction && introduction < block && block < readOnly);
		Assert.Contains(AiAssistTexts.PACK_CHECKLIST_LABEL_IN_EVERY_FINDING, body, StringComparison.Ordinal);
	}

	[Fact]
	public void TheBlocksAppearInTheSpecifiedOrder()
	{
		AgentEnvironment environment = AgentEnvironment.ClaudeCode;
		string body = PromptPackFixture.Body(environment, PromptPackFixture.Scope());

		string[] markers =
		[
			string.Format(System.Globalization.CultureInfo.InvariantCulture, AiAssistTexts.PACK_TITLE_FORMAT, AgentEnvironmentCatalog.Describe(environment).DisplayName),
			$"**{AiAssistTexts.REVIEW_LABEL}**",
			AiAssistTexts.PACK_COST_NOTICE_HEADING,
			AiAssistTexts.PACK_DATA_STATEMENT,
			AiAssistTexts.PACK_INSTRUCTIONS_HEADING,
			string.Format(System.Globalization.CultureInfo.InvariantCulture, AiAssistTexts.PACK_REPORT_NAMED_FORMAT, PromptPackFixture.REPORT_FILE_NAME),
			AiAssistTexts.PACK_READ_ONLY,
			AiAssistTexts.PACK_NO_CREDENTIAL_VALUES,
			AiAssistTexts.PACK_FLOW_REQUEST,
			AiAssistTexts.PACK_CHECKLIST_HEADING,
			AiAssistTexts.PACK_COUNTS_HEADING,
			string.Format(System.Globalization.CultureInfo.InvariantCulture, AiAssistTexts.PACK_MINIMUM_LEVEL_FORMAT, AiAssistTexts.LevelText(ConfidenceLevel.Low)),
			$"{_SECTION_ONE_HEADING} - ambiguous cases (3)",
			$"{_SECTION_TWO_HEADING} - defaulted flow (1)",
			$"{_SECTION_THREE_HEADING} - for confirmation, optional and skippable (2)"
		];

		int previous = -1;

		foreach (string marker in markers)
		{
			int position = body.IndexOf(marker, StringComparison.Ordinal);

			Assert.True(position > previous, $"'{marker}' is missing or out of order.");
			previous = position;
		}
	}

	[Fact]
	public void TheHandlingHeaderComesFirstWhenTheArtifactWriterWritesTheFile()
	{
		byte[] bytes = PromptPackFixture.WritePackFile(AgentEnvironment.ClaudeCode, PromptPackFixture.Scope());
		string text = System.Text.Encoding.UTF8.GetString(bytes);

		Assert.StartsWith("> **Handling.**", text, StringComparison.Ordinal);
		Assert.True(text.IndexOf("# Prompt pack for AI-assisted review", StringComparison.Ordinal) > 0);
	}

	[Fact]
	public void TheCostNoticeInThePackIsTheOneTheConsoleShows()
	{
		PromptPackScope scope = PromptPackFixture.Scope();
		string body = PromptPackFixture.Body(AgentEnvironment.OpenAiCodex, scope);

		Assert.Contains(AiAssistTexts.CostNotice(AgentEnvironment.OpenAiCodex, scope.TotalCount), body, StringComparison.Ordinal);
	}

	[Fact]
	public void EachSectionOpensWithTheTextOfEveryKindItHoldsBeforeAnyEntry()
	{
		string body = PromptPackFixture.Body(AgentEnvironment.ClaudeCode, PromptPackFixture.Scope());
		string sectionOne = SectionText(body, _SECTION_ONE_HEADING, _SECTION_TWO_HEADING);
		string sectionTwo = SectionText(body, _SECTION_TWO_HEADING, _SECTION_THREE_HEADING);
		string sectionThree = SectionText(body, _SECTION_THREE_HEADING, null);

		int manual = sectionOne.IndexOf(KindLine(RecommendationKind.ManualReview), StringComparison.Ordinal);
		int generic = sectionOne.IndexOf(KindLine(RecommendationKind.Generic), StringComparison.Ordinal);
		int firstEntry = sectionOne.IndexOf(_ENTRY_LINE_START, StringComparison.Ordinal);

		Assert.True(manual >= 0 && generic > manual && firstEntry > generic);
		Assert.DoesNotContain(KindLine(RecommendationKind.FromTemplate), sectionOne, StringComparison.Ordinal);
		Assert.Contains(KindLine(RecommendationKind.FromTemplate), sectionTwo, StringComparison.Ordinal);
		Assert.Contains(KindLine(RecommendationKind.FromTemplate), sectionThree, StringComparison.Ordinal);
		Assert.DoesNotContain(KindLine(RecommendationKind.ManualReview), sectionTwo + sectionThree, StringComparison.Ordinal);
	}

	[Fact]
	public void EntriesFollowTheReportsOrderInsideEachSection()
	{
		string body = PromptPackFixture.Body(AgentEnvironment.ClaudeCode, PromptPackFixture.Scope());

		Assert.Equal(["004", "005", "003"], EntryIdentifiers(SectionText(body, _SECTION_ONE_HEADING, _SECTION_TWO_HEADING)));
		Assert.Equal(["002"], EntryIdentifiers(SectionText(body, _SECTION_TWO_HEADING, _SECTION_THREE_HEADING)));
		Assert.Equal(["001", "006"], EntryIdentifiers(SectionText(body, _SECTION_THREE_HEADING, null)));
	}

	[Fact]
	public void AnEntryLineCarriesTheFieldsInTheSpecifiedOrder()
	{
		string body = PromptPackFixture.Body(AgentEnvironment.ClaudeCode, PromptPackFixture.Scope());

		Assert.Contains("- 003 | TST-002 | Medium | ClientCredentials | DEFAULT-FLOW | no template | line 30 | ` src/C.cs `", body, StringComparison.Ordinal);
		Assert.Contains("- 004 | TST-001 | Low | no flow | none | manual review | line 40 | ` src/D.cs `", body, StringComparison.Ordinal);
	}

	[Fact]
	public void ARunThatWroteNoReportSaysSoAndNamesNoFile()
	{
		string body = PromptPackFixture.Body(AgentEnvironment.ClaudeCode, PromptPackFixture.Scope(), reportFileName: null);

		Assert.Contains(AiAssistTexts.PACK_NO_REPORT, body, StringComparison.Ordinal);
		Assert.DoesNotContain("The assessment report is named", body, StringComparison.Ordinal);
	}

	[Fact]
	public void AMinimumLevelNarrowsTheListAndIsStated()
	{
		PromptPackScope scope = PromptPackFixture.Scope(ConfidenceLevel.High);
		string body = PromptPackFixture.Body(AgentEnvironment.ClaudeCode, scope);

		Assert.Equal(2, scope.TotalCount);
		Assert.Contains(string.Format(System.Globalization.CultureInfo.InvariantCulture, AiAssistTexts.PACK_MINIMUM_LEVEL_FORMAT, AiAssistTexts.LevelText(ConfidenceLevel.High)), body, StringComparison.Ordinal);
		Assert.Contains("Entries: 2. Section 1: 0. Section 2: 1. Section 3: 1.", body, StringComparison.Ordinal);
	}

	[Fact]
	public void AZeroEntryPackIsStillAWholePack()
	{
		PromptPackScope scope = new([], ConfidenceLevel.Low, PromptPackFixture.Catalog);
		string body = PromptPackFixture.Body(AgentEnvironment.GitHubCopilotCli, scope);

		Assert.Contains("Entries: 0. Section 1: 0. Section 2: 0. Section 3: 0.", body, StringComparison.Ordinal);
		Assert.Contains($"{_SECTION_THREE_HEADING} - for confirmation, optional and skippable (0)", body, StringComparison.Ordinal);
		Assert.DoesNotContain(_ZERO_ENTRY_LINE_START, body, StringComparison.Ordinal);
		Assert.DoesNotContain("**manual review.**", body, StringComparison.Ordinal);
	}

	[Fact]
	public void TheBodyUsesLineFeedsOnly()
	{
		string body = PromptPackFixture.Body(AgentEnvironment.ClaudeCode, PromptPackFixture.Scope());

		Assert.DoesNotContain(_CARRIAGE_RETURN, body);
	}

	private static string KindLine(RecommendationKind kind)
	{
		return $"**{AiAssistTexts.KindLabel(kind)}.** {PromptPackInstructionText.Describe(kind)}";
	}

	private static string SectionText(string body, string startHeading, string? endHeading)
	{
		int start = body.IndexOf(startHeading, StringComparison.Ordinal);
		int end = endHeading is null ? body.Length : body.IndexOf(endHeading, start, StringComparison.Ordinal);

		return body[start..end];
	}

	private static string[] EntryIdentifiers(string section)
	{
		return
		[
			.. section
				.Split(AiAssistHarness.LINE_FEED_CHARACTER)
				.Where(line => line.StartsWith(_ENTRY_PREFIX, StringComparison.Ordinal))
				.Select(line => line.Split(_FIELD_SEPARATOR)[0][_ENTRY_PREFIX.Length..])
		];
	}
}
