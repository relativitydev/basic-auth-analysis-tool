using System.Collections.Immutable;
using System.Globalization;

using MigrateAuth.Cli.Agent;
using MigrateAuth.Cli.Session;

using Xunit;

namespace MigrateAuth.Cli.Tests.Documentation;

/// <summary>
/// EVERY CONSOLE STRING THE USER GUIDE QUOTES FOR <c>ai-assist</c> IS THE BINARY'S OWN CONSTANT.
///
/// The guide quotes the tool's lines in two places only: block quotes, and fenced blocks labelled <c>text</c>, inside the
/// section headed "The ai-assist command". Each quoted line is rebuilt here from the constants of <see cref="AiAssistTexts"/>
/// with the same sample values the guide shows, and the two sets must be equal: a line the guide quotes that the binary does not
/// print fails, and so does a line the guide is expected to quote and no longer does. The figures the prose gives (the longest current
/// folder of the command interpreter, the longest wrapper command line, the launch folder's name) are read from the constants too.
/// </summary>
public sealed class AiAssistGuideTextTests
{
	private const string _SECTION_HEADING = "## The ai-assist command";
	private const string _STRICTER_MODE_HEADING = "### Choosing a stricter mode before you launch";
	private const string _TEXT_FENCE_OPENING = "```text";
	private const string _FENCE_CLOSING = "```";
	private const string _QUOTE_PREFIX = "> ";
	private const string _UNVERIFIED_WORD = "unverified";
	private const string _NOTICE_FOOTNOTE_TAG_PATTERN = @"\[[CR]\d+\]";
	private const string _NOT_AVAILABLE_WORDING = "not available";
	private const string _LINE_SEPARATOR_TEXT = " ";
	private const string _CODE_SPAN_MARKER = "`";
	private const string _TABLE_ROW_PREFIX = "|";
	private const string _FAILURE_SEPARATOR = " || ";
	private const string _MARKDOWN_EXTENSION = ".md";
	private const string _HEXADECIMAL_DIGITS_TEXT = " hexadecimal digits";
	private const string _READ_AS_INTERRUPTION_TEXT = " is read as an interruption";
	private const string _RANGE_SEPARATOR = " to ";
	private const string _MINUS_TEXT = "minus ";
	private const string _EXIT_CODE_TEXT = "exit code ";
	private const string _BOLD_MARKER = "**";
	private const string _SENTENCE_END = ".";
	private const string _SAMPLE_GATING_STAGE_NAME = "analyze";
	private const string _SAMPLE_PACK_FILE_NAME = "ai-assist-claude-code.md";
	private const string _SAMPLE_OUTPUT_FOLDER = "./Outputs";
	private const string _SAMPLE_LAUNCH_FOLDER_NAME = "ai-assist-launch-0123456789abcdef0123456789abcdef";
	private const string _SAMPLE_WINDOWS_PROGRAM = "claude.exe";
	private const string _SAMPLE_WINDOWS_WRAPPER = "codex.cmd";
	private const string _SAMPLE_CLAUDE_COMMAND = "claude";
	private const int _SAMPLE_ENTRY_COUNT = 42;
	private const int _SAMPLE_AMBIGUOUS_COUNT = 5;
	private const int _SAMPLE_DEFAULTED_COUNT = 17;
	private const int _SAMPLE_CONFIRMATION_COUNT = 20;
	private const string _SAMPLE_POSITION = "4";
	private const string _SAMPLE_WRAPPER_POSITION = "7";
	private const string _SAMPLE_TOTAL_POSITIONS = "12";
	private const string _SAMPLE_SIGNAL_NUMBER = "15";
	private const string _SAMPLE_CANCELLED_WRAPPER_EXIT_CODE = "255";
	private const string _NUMBER_FORMAT = "N0";
	private const char _LINE_BREAK = '\n';
	private const char _CARRIAGE_RETURN = '\r';
	private const int _SAMPLE_COST_ENTRY_COUNT = 42;
	private const int _ENVIRONMENT_COUNT = 3;
	private const string _ALTERED_LINE = "ai-assist: interrupted. Nothing further was done, or maybe it was.";

	private static readonly AgentEnvironment _claudeCode = AgentEnvironment.ClaudeCode;

	/// <summary>Every line the guide quotes in the section is the text of a constant of the binary, with the sample values the guide shows.</summary>
	[Fact]
	public void EveryLineTheGuideQuotesIsAConsoleStringOfTheBinary()
	{
		ImmutableArray<string> quoted = QuotedLinesOf(AiAssistSection());
		ImmutableHashSet<string> expected = [.. ExpectedQuotedLines()];

		string[] foreign = [.. quoted.Where(line => !expected.Contains(line))];

		Assert.NotEmpty(quoted);
		Assert.True(foreign.Length == 0, $"The guide quotes text the binary does not print: {string.Join(_FAILURE_SEPARATOR, foreign)}");
	}

	/// <summary>Every line the guide is expected to quote is quoted, so removing a quotation fails here rather than going unnoticed.</summary>
	[Fact]
	public void EveryConsoleStringTheGuideIsExpectedToQuoteIsQuoted()
	{
		ImmutableHashSet<string> quoted = [.. QuotedLinesOf(AiAssistSection())];

		string[] missing = [.. ExpectedQuotedLines().Distinct().Where(line => !quoted.Contains(line))];

		Assert.True(missing.Length == 0, $"The guide no longer quotes: {string.Join(_FAILURE_SEPARATOR, missing)}");
	}

	/// <summary>The check fails over a quotation that differs from the constant by a few words, and passes the constant itself.</summary>
	[Fact]
	public void ACloseButDifferentQuotationIsReportedAsForeign()
	{
		ImmutableHashSet<string> expected = [.. ExpectedQuotedLines()];
		ImmutableArray<string> alteredQuotation = QuotedLinesOf($"{_QUOTE_PREFIX}{_ALTERED_LINE}");
		ImmutableArray<string> exactQuotation = QuotedLinesOf($"{_QUOTE_PREFIX}{AiAssistTexts.INTERRUPTED}");

		Assert.DoesNotContain(Assert.Single(alteredQuotation), expected);
		Assert.Contains(Assert.Single(exactQuotation), expected);
	}

	/// <summary>The environment table of the guide has, for each environment, its number, its names, its command, its provider and its pack file.</summary>
	[Fact]
	public void TheEnvironmentTableMatchesTheCatalog()
	{
		string section = AiAssistSection();

		Assert.Equal(_ENVIRONMENT_COUNT, AgentEnvironmentCatalog.All.Length);

		foreach (AgentEnvironmentDescriptor descriptor in AgentEnvironmentCatalog.All)
		{
			string packFile = $"{_CODE_SPAN_MARKER}{descriptor.PackFileNameStem}{_MARKDOWN_EXTENSION}{_CODE_SPAN_MARKER}";
			string? row = section.Split(_LINE_BREAK).FirstOrDefault(line => line.StartsWith(_TABLE_ROW_PREFIX, StringComparison.Ordinal) && line.Contains(packFile, StringComparison.Ordinal));

			Assert.True(row is not null, $"The guide has no table row for the pack file {packFile}.");
			Assert.StartsWith($"{_TABLE_ROW_PREFIX} {descriptor.SelectionNumber.ToString(CultureInfo.InvariantCulture)} {_TABLE_ROW_PREFIX}", row!, StringComparison.Ordinal);
			Assert.Contains($"{_CODE_SPAN_MARKER}{descriptor.CommandName}{_CODE_SPAN_MARKER}", row, StringComparison.Ordinal);
			Assert.Contains(descriptor.ProviderName, row, StringComparison.Ordinal);
			Assert.All(descriptor.AcceptedNames, name => Assert.Contains($"{_CODE_SPAN_MARKER}{name}{_CODE_SPAN_MARKER}", row, StringComparison.Ordinal));
		}
	}

	/// <summary>The figures the prose gives are the constants the binary applies.</summary>
	[Fact]
	public void TheFiguresInTheProseAreTheBinarysConstants()
	{
		string section = AiAssistSection();

		Assert.Contains(NumberText(WrapperWorkingDirectoryRule.MAXIMUM_CURRENT_DIRECTORY_LENGTH), section, StringComparison.Ordinal);
		Assert.Contains(NumberText(AgentCommandLineSafety.MAXIMUM_WRAPPER_COMMAND_LINE_LENGTH), section, StringComparison.Ordinal);
		Assert.Contains(LaunchCopyLayout.FOLDER_NAME_PREFIX, section, StringComparison.Ordinal);
		Assert.Contains($"{LaunchCopyLayout.IDENTIFIER_LENGTH.ToString(CultureInfo.InvariantCulture)}{_HEXADECIMAL_DIGITS_TEXT}", section, StringComparison.Ordinal);
		Assert.Equal(LaunchCopyLayout.FOLDER_NAME_PREFIX.Length + LaunchCopyLayout.IDENTIFIER_LENGTH, _SAMPLE_LAUNCH_FOLDER_NAME.Length);
		Assert.Contains(AgentChildEnvironment.SEARCH_PATH_VARIABLE, section, StringComparison.Ordinal);
		Assert.Contains($"{AgentExitMapping.LINUX_INTERRUPT_EXIT_CODE.ToString(CultureInfo.InvariantCulture)}{_READ_AS_INTERRUPTION_TEXT}", section, StringComparison.Ordinal);
		Assert.Contains($"{AgentExitMapping.LINUX_FIRST_SIGNAL_EXIT_CODE.ToString(CultureInfo.InvariantCulture)}{_RANGE_SEPARATOR}{AgentExitMapping.LINUX_LAST_SIGNAL_EXIT_CODE.ToString(CultureInfo.InvariantCulture)}", section, StringComparison.Ordinal);
		Assert.Contains($"{_MINUS_TEXT}{AgentExitMapping.LINUX_SIGNAL_EXIT_BASE.ToString(CultureInfo.InvariantCulture)}", section, StringComparison.Ordinal);
		Assert.Contains($"{_EXIT_CODE_TEXT}{_SAMPLE_CANCELLED_WRAPPER_EXIT_CODE}", section, StringComparison.Ordinal);
	}

	/// <summary>The guide says how to choose a stricter mode for each of the three tools, and marks what it could not confirm.</summary>
	[Fact]
	public void TheGuideSaysHowToChooseAStricterModeInEachToolAndMarksWhatIsUnverified()
	{
		string section = AiAssistSection();
		string? stricterModes = PublicDocumentChecks.SectionOf(section, _STRICTER_MODE_HEADING);

		Assert.True(stricterModes is not null, $"The section has no heading '{_STRICTER_MODE_HEADING}'.");
		Assert.All(AgentEnvironmentCatalog.All, descriptor => Assert.Contains($"{_BOLD_MARKER}{descriptor.DisplayName}{_SENTENCE_END}{_BOLD_MARKER}", stricterModes, StringComparison.Ordinal));
		Assert.Contains(_UNVERIFIED_WORD, stricterModes, StringComparison.OrdinalIgnoreCase);
	}

	/// <summary>The guide no longer says the command is not available.</summary>
	[Fact]
	public void TheGuideDoesNotSayThatAiAssistIsNotAvailable()
	{
		string guide = PublicDocuments.ReadUserGuideRow(PublicDocuments.ROW_USING_THE_TOOL);

		Assert.DoesNotContain(_NOT_AVAILABLE_WORDING, guide, StringComparison.OrdinalIgnoreCase);
	}

	/// <summary>The guide quotes the cost notice and the repository-access statement without footnote tags.</summary>
	[Fact]
	public void TheGuideQuotesNoFootnoteTagInTheNotices()
	{
		string guide = PublicDocuments.ReadUserGuideRow(PublicDocuments.ROW_USING_THE_TOOL);

		Assert.DoesNotMatch(_NOTICE_FOOTNOTE_TAG_PATTERN, guide);
	}

	private static string AiAssistSection()
	{
		string? section = PublicDocumentChecks.SectionOf(PublicDocuments.ReadUserGuideRow(PublicDocuments.ROW_USING_THE_TOOL), _SECTION_HEADING);

		Assert.True(section is not null, $"The guide has no section headed '{_SECTION_HEADING}'.");

		return section!;
	}

	/// <summary>The lines of the block quotes and of the <c>text</c> fences of <paramref name="section"/>, without the quote marker and without trailing white space.</summary>
	private static ImmutableArray<string> QuotedLinesOf(string section)
	{
		ImmutableArray<string>.Builder quoted = ImmutableArray.CreateBuilder<string>();
		bool insideTextFence = false;

		foreach (string rawLine in section.Split(_LINE_BREAK))
		{
			string line = rawLine.TrimEnd(_CARRIAGE_RETURN);

			if (!insideTextFence && line.StartsWith(_TEXT_FENCE_OPENING, StringComparison.Ordinal))
			{
				insideTextFence = true;
			}
			else if (insideTextFence && line.StartsWith(_FENCE_CLOSING, StringComparison.Ordinal))
			{
				insideTextFence = false;
			}
			else if (insideTextFence && line.Trim().Length > 0)
			{
				quoted.Add(line.TrimEnd());
			}
			else if (!insideTextFence && line.StartsWith(_QUOTE_PREFIX, StringComparison.Ordinal))
			{
				quoted.Add(line[_QUOTE_PREFIX.Length..].TrimEnd());
			}
		}

		return quoted.ToImmutable();
	}

	/// <summary>Every line the guide quotes, as the binary prints it for the sample values the guide shows.</summary>
	private static ImmutableArray<string> ExpectedQuotedLines()
	{
		ImmutableArray<string> expected =
		[
			.. SelectionLines(),
			Formatted(CommandRegistry.GATING_MESSAGE_FORMAT, _SAMPLE_GATING_STAGE_NAME),
			AiAssistTexts.SELECTION_REASK,
			AiAssistTexts.NO_ENVIRONMENT_CHOSEN,
			AiAssistTexts.SELECTION_CURSOR_NOT_SUPPORTED,
			AiAssistTexts.INTERRUPTED,
			Formatted(AiAssistTexts.WRITTEN_FORMAT, _SAMPLE_PACK_FILE_NAME, _SAMPLE_OUTPUT_FOLDER),
			AiAssistTexts.PACK_DATA_STATEMENT,
			AiAssistTexts.PACK_READ_ONLY,
			AiAssistTexts.PACK_NO_CREDENTIAL_VALUES,
			AiAssistTexts.PACK_FORMAT_INTRODUCTION,
			AiAssistTexts.PACK_FORMAT_HEADER_LINE,
			AiAssistTexts.PACK_FORMAT_REASON_LINE,
			AiAssistTexts.REVIEW_LABEL,
			Formatted(AiAssistTexts.SECTION_APPENDED_FORMAT, _SAMPLE_PACK_FILE_NAME),
			AiAssistTexts.APPEND_REFUSED_REPORT_CHANGED,
			AiAssistTexts.REPORT_NOT_WRITTEN_BY_THIS_RUN,
			AiAssistTexts.REPORT_COULD_NOT_BE_RECORDED,
			Formatted(AiAssistTexts.SEARCH_FORMAT, _SAMPLE_CLAUDE_COMMAND),
			.. NoticeLines(),
			.. TerminationLines(),
			.. WithheldLaunchLines(),
			AiAssistTexts.LAUNCH_DECLINED,
			AiAssistTexts.LAUNCH_SENTENCE,
			Formatted(AiAssistTexts.LAUNCH_FOLDER_FORMAT, AiAssistTexts.LaunchFolderText(_SAMPLE_OUTPUT_FOLDER, _SAMPLE_LAUNCH_FOLDER_NAME))
		];

		return expected;
	}

	private static ImmutableArray<string> SelectionLines()
	{
		return
		[
			AiAssistTexts.SELECTION_HEADING,
			.. AgentEnvironmentCatalog.All.Select(descriptor => AiAssistTexts.SelectionOption(descriptor.Environment)),
			AiAssistTexts.SELECTION_PROMPT.TrimEnd()
		];
	}

	private static ImmutableArray<string> NoticeLines()
	{
		return
		[
			Formatted(AiAssistTexts.RESOLUTION_LINE_FORMAT, _SAMPLE_WINDOWS_PROGRAM, _SAMPLE_POSITION, _SAMPLE_TOTAL_POSITIONS, AiAssistTexts.STARTED_DIRECTLY),
			Formatted(
				AiAssistTexts.RESOLUTION_LINE_FORMAT,
				_SAMPLE_WINDOWS_WRAPPER,
				_SAMPLE_WRAPPER_POSITION,
				_SAMPLE_TOTAL_POSITIONS,
				AiAssistTexts.STARTED_THROUGH_COMMAND_INTERPRETER),
			.. AiAssistTexts.CostNoticeLines(_claudeCode, _SAMPLE_COST_ENTRY_COUNT),
			.. AiAssistTexts.RepositoryAccessLines(_claudeCode),
			.. AiAssistTexts.AgentSettingsLines(_claudeCode),
			AiAssistTexts.PERMISSIONS_OPENAI_CODEX,
			AiAssistTexts.ACCESS_OPENAI_CODEX,
			AiAssistTexts.PERMISSIONS_COPILOT_CLI,
			AiAssistTexts.ACCESS_COPILOT_CLI,
			AiAssistTexts.EntryCountLine(_SAMPLE_ENTRY_COUNT, _SAMPLE_AMBIGUOUS_COUNT, _SAMPLE_DEFAULTED_COUNT, _SAMPLE_CONFIRMATION_COUNT),
			AiAssistTexts.Confirmation(_claudeCode).TrimEnd()
		];
	}

	private static ImmutableArray<string> TerminationLines()
	{
		string name = AgentEnvironmentCatalog.Describe(_claudeCode).DisplayName;
		string command = AgentEnvironmentCatalog.Describe(_claudeCode).CommandName;
		string endedWithCode = Formatted(AiAssistTexts.ENDED_WITH_CODE_FORMAT, name, _SAMPLE_CANCELLED_WRAPPER_EXIT_CODE);
		string fallback = Formatted(AiAssistTexts.FALLBACK_SENTENCE_FORMAT, command);

		return
		[
			WithSuffix(Formatted(AiAssistTexts.ENDED_NORMALLY_FORMAT, name)),
			WithSuffix($"{endedWithCode}{_LINE_SEPARATOR_TEXT}{fallback}"),
			WithSuffix(Formatted(AiAssistTexts.ENDED_BY_SIGNAL_FORMAT, name, _SAMPLE_SIGNAL_NUMBER)),
			WithSuffix(Formatted(AiAssistTexts.INTERRUPTED_BY_USER_FORMAT, name)),
			WithSuffix(Formatted(AiAssistTexts.COULD_NOT_BE_STARTED_FORMAT, name))
		];
	}

	private static ImmutableArray<string> WithheldLaunchLines()
	{
		return
		[
			Formatted(AiAssistTexts.NOT_FOUND_FORMAT, _SAMPLE_CLAUDE_COMMAND),
			Formatted(AiAssistTexts.MATCH_INSIDE_REPOSITORY_FORMAT, _SAMPLE_CLAUDE_COMMAND),
			AiAssistTexts.NO_TERMINAL,
			Formatted(AiAssistTexts.UNSAFE_COMMAND_LINE_FORMAT, _SAMPLE_CLAUDE_COMMAND),
			AiAssistTexts.PATH_CONTAINS_CONTROL_CHARACTER,
			Formatted(AiAssistTexts.WRAPPER_NEEDS_LOCAL_REPOSITORY_FORMAT, _SAMPLE_CLAUDE_COMMAND)
		];
	}

	private static string WithSuffix(string message)
	{
		return $"{message}{_LINE_SEPARATOR_TEXT}{AiAssistTexts.TERMINATION_SUFFIX}";
	}

	private static string Formatted(string format, params object[] arguments)
	{
		return string.Format(CultureInfo.InvariantCulture, format, arguments);
	}

	private static string NumberText(int number)
	{
		return number.ToString(_NUMBER_FORMAT, CultureInfo.InvariantCulture);
	}
}
