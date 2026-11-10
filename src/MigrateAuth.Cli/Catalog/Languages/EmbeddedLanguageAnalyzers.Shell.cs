using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Text.RegularExpressions;

using MigrateAuth.Cli.Findings;

namespace MigrateAuth.Cli.Catalog.Languages;

/// <summary>
/// The `Shell` analyzer: the lexical profile of the POSIX family of shells (<c>bash</c>, <c>sh</c>,
/// <c>zsh</c>, <c>ksh</c>), the identification of its files and its twenty-five pattern classes. The profile reads a comment only where a word starts, the three quote forms, the
/// two here-document forms with their deferred body start, whole-line closings and captured terminators, and declares every mechanism the engine added
/// for this language: executed strings, the backslash as the code escape, the arm block of a command-substitution hole, the arithmetic context and the
/// dialects of a file.
///
/// FILES. The analyzer type is <c>file</c>-scoped, so its expressions live in <see cref="ShellPatternExpressions"/>, its detection rules in
/// <see cref="ShellRuleDeclarations"/> and its executed-string declaration in <see cref="ShellExecutedStringDeclarations"/>. The registration is the partial
/// method below.
/// </summary>
public static partial class EmbeddedLanguageAnalyzers
{
	static partial void AddShellAnalyzer(ICollection<ILanguageAnalyzer> analyzers)
	{
		analyzers.Add(new ShellAnalyzer());
	}
}

/// <summary>The identifiers of the dialects a `Shell` file is read in, declared once for the file dialects and for the flow signals that name them.</summary>
internal static class ShellDialectNames
{
	/// <summary>The default, and the dialect of a file whose first line names <c>bash</c> or <c>sh</c>: <c>sh</c> is read with the semantics of <c>bash</c> 5.2.</summary>
	internal const string BASH = "bash";

	internal const string ZSH = "zsh";

	/// <summary>The dialect of a file whose first line names <c>ksh</c>, and of a <c>.ksh</c> file whose first line names none.</summary>
	internal const string KSH93 = "ksh93";

	internal const string MKSH = "mksh";
}

/// <summary>
/// The `Shell` analyzer. File-local per the registration pattern every embedded language follows, so no engine type can name it and no special
/// case can be written against it.
/// </summary>
file sealed class ShellAnalyzer : ILanguageAnalyzer
{
	private const string _PROFILE_NAME = "Shell";
	private const string _LANGUAGE_NAME = "Shell";

	private const string _SHELL_EXTENSION = ".sh";
	private const string _BASH_EXTENSION = ".bash";
	private const string _ZSH_EXTENSION = ".zsh";
	private const string _KSH_EXTENSION = ".ksh";

	private const string _BASH_INTERPRETER = "bash";
	private const string _SH_INTERPRETER = "sh";
	private const string _ZSH_INTERPRETER = "zsh";
	private const string _KSH_INTERPRETER = "ksh";
	private const string _KSH93_INTERPRETER = "ksh93";
	private const string _MKSH_INTERPRETER = "mksh";

	private const string _LINE_COMMENT_OPENING = "#";
	private const string _SINGLE_QUOTE = "'";
	private const string _DOUBLE_QUOTE = "\"";
	private const string _ANSI_C_PREFIX = "$";
	private const string _SUBSTITUTION_OPENING = "$(";
	private const string _SUBSTITUTION_CLOSING = ")";
	private const string _NESTING_OPENING = "(";
	private const string _PLAIN_HERE_DOCUMENT_OPENING = "<<";
	private const string _TAB_STRIPPING_HERE_DOCUMENT_OPENING = "<<-";
	private const string _CLOSING_IS_CAPTURED_AT_THE_OPENING = "";
	private const string _TERMINATOR_CAPTURE_GROUP = "terminator";
	private const string _QUOTED_TERMINATOR_CAPTURE_GROUP = "quoted";
	private const string _ANSI_C_TERMINATOR_CAPTURE_GROUP = "ansic";
	private const string _DOUBLE_QUOTED_TERMINATOR_CAPTURE_GROUP = "dquoted";
	private const string _ARITHMETIC_OPENING = "((";
	private const string _ARITHMETIC_CLOSING = "))";
	private const string _OPTION_TAKING_KEYWORD = "time";
	private const string _CASE_KEYWORD = "case";
	private const string _CASE_CLOSING_KEYWORD = "esac";
	private const string _COMMAND_POSITION_PREDECESSORS = "\n;&|(";
	private const string _BACKTICK = "`";
	private const string _BACKTICK_ESCAPABLE_CHARACTERS = "`\\$";

	/// <summary>
	/// The characters after which a hash opens a comment: a blank, a line feed and the shell's word separators that stand before a word. The angle brackets are
	/// not among them: a hash directly after one is read as code in every dialect.
	/// </summary>
	private const string _COMMENT_PREDECESSORS = " \t\n;&|()";

	private const char _ESCAPE_CHARACTER = '\\';
	private const char _HERE_STRING_ANGLE = '<';
	private const char _LINE_FEED = '\n';
	private const char _OPENING_PARENTHESIS = '(';
	private const char _CLOSING_PARENTHESIS = ')';
	private const int _ESCAPED_LINE_FEED_LENGTH = 2;
	private const int _ESCAPE_PARITY_DIVISOR = 2;

	private static readonly string[] _commandPositionKeywords = ["{", "!", "then", "do", "else", "elif", "if", "while", "until", "time", "coproc"];

	public LexicalProfile LexicalProfile { get; } = BuildLexicalProfile();

	public LanguageIdentification LanguageIdentification { get; } = BuildLanguageIdentification();

	public FrozenDictionary<string, DetectionRule> DetectionRulesByPatternClass { get; } = ShellRuleDeclarations.BuildDetectionRules();

	public LanguageShapedDemotionSignals DemotionSignals { get; } = ShellDemotionDeclarations.Build();

	public CandidateSelectionAndRecommendation CandidateSelectionAndRecommendation { get; } = BuildCandidateSelectionAndRecommendation();

	public FrozenSet<SubsumptionRule> SubsumptionRelation { get; } = ShellRuleDeclarations.BuildSubsumptionRelation();

	public ConstructPrecedence? ConstructPrecedence { get; } = ShellRuleDeclarations.BuildConstructPrecedence();

	// ------------------------------------------------------------------------------------------
	// Element 1: the lexical profile
	// ------------------------------------------------------------------------------------------

	private static LexicalProfile BuildLexicalProfile()
	{
		LexicalProfile profile = new()
		{
			ProfileName = _PROFILE_NAME,
			ConstantDeclarationExpression = ShellPatternExpressions.ConstantDeclaration(),
			KeywordCaseSensitivity = CaseSensitivity.Sensitive,
			DelimiterCaseSensitivity = CaseSensitivity.Sensitive,
			IdentifierCaseSensitivity = IdentifierCaseSensitivityMode.Sensitive,
			LineCommentRules =
			[
				new LineCommentRule { Opening = _LINE_COMMENT_OPENING, OpeningContextPredicate = CommentOpeningStandsHere }
			],
			BlockCommentRules = [],
			QuoteRules = BuildQuoteRules(),
			ArithmeticContext = new ArithmeticContextRule { Opening = _ARITHMETIC_OPENING, Closing = _ARITHMETIC_CLOSING },
			CodeEscapeCharacter = _ESCAPE_CHARACTER,
			ExecutedStrings = ShellExecutedStringDeclarations.Build(),
			CodeSpans = BuildCodeSpans()
		};

		return profile;
	}

	/// <summary>
	/// The backtick command substitution, the one code span of the shell: a backtick that closes on its own line holds a command of its own, in which a
	/// here-document has an empty body in every shell. A backslash before a backtick, a backslash or a dollar sign is removed from the content.
	/// </summary>
	private static ImmutableArray<CodeSpanElement> BuildCodeSpans()
	{
		return
		[
			new CodeSpanElement
			{
				Opening = _BACKTICK,
				Closing = _BACKTICK,
				EscapeCharacter = _ESCAPE_CHARACTER.ToString(),
				EscapableCharacters = _BACKTICK_ESCAPABLE_CHARACTERS
			}
		];
	}

	/// <summary>
	/// The shell's arm block: a closing parenthesis met between <c>case</c> and <c>esac</c> with no opening to match ends an arm's pattern and not the
	/// substitution.
	/// </summary>
	private static ArmBlockRule BuildCaseArmBlock()
	{
		return new ArmBlockRule
		{
			OpeningKeyword = _CASE_KEYWORD,
			ClosingKeyword = _CASE_CLOSING_KEYWORD,
			CommandPositionPredecessors = _COMMAND_POSITION_PREDECESSORS,
			CommandPositionKeywords = [.. _commandPositionKeywords],
			OptionTakingKeywords = [_OPTION_TAKING_KEYWORD]
		};
	}

	/// <summary>
	/// Four quote rules. The single-quoted literal takes newlines, has no escape and reads the ANSI-C prefix as one that restores the backslash; the
	/// double-quoted one escapes with a backslash and has one <c>$( )</c> hole whose parentheses nest. The two here-document rules close on a captured
	/// terminator, the plain one on a whole line and the tab-stripping one on a whole line after leading tabs.
	/// </summary>
	private static ImmutableArray<QuoteRule> BuildQuoteRules()
	{
		return
		[
			new QuoteRule
			{
				Opening = _SINGLE_QUOTE,
				Closing = _SINGLE_QUOTE,
				NewlinePolicy = NewlinePolicy.Allowed,
				LiteralPrefixes =
				[
					new LiteralPrefixRule
					{
						Prefix = _ANSI_C_PREFIX,
						EscapeHandling = EscapeHandling.AsDeclaredByTheQuoteRule,
						EscapeCharacterOverride = _ESCAPE_CHARACTER
					}
				],
				OpeningContextPredicate = QuoteOpeningIsNotBackslashEscaped
			},
			new QuoteRule
			{
				Opening = _DOUBLE_QUOTE,
				Closing = _DOUBLE_QUOTE,
				NewlinePolicy = NewlinePolicy.Allowed,
				EscapeCharacter = _ESCAPE_CHARACTER,
				InterpolationOpening = _SUBSTITUTION_OPENING,
				InterpolationClosing = _SUBSTITUTION_CLOSING,
				InterpolationNestingOpening = _NESTING_OPENING,
				InterpolationArmBlock = BuildCaseArmBlock(),
				OpeningContextPredicate = QuoteOpeningIsNotBackslashEscaped
			},
			BuildHereDocumentRule(
				_TAB_STRIPPING_HERE_DOCUMENT_OPENING,
				ShellPatternExpressions.TabStrippingOpening(),
				PositionalAnchoring.WholeLineAfterLeadingTabs),
			BuildHereDocumentRule(
				_PLAIN_HERE_DOCUMENT_OPENING,
				ShellPatternExpressions.PlainOpening(),
				PositionalAnchoring.WholeLine)
		];
	}

	private static QuoteRule BuildHereDocumentRule(string opening, Regex openingExpression, PositionalAnchoring closingAnchoring)
	{
		return new QuoteRule
		{
			Opening = opening,
			Closing = _CLOSING_IS_CAPTURED_AT_THE_OPENING,
			NewlinePolicy = NewlinePolicy.Allowed,
			EscapeCharacter = _ESCAPE_CHARACTER,
			ClosingAnchoring = closingAnchoring,
			InterpolationOpening = _SUBSTITUTION_OPENING,
			InterpolationClosing = _SUBSTITUTION_CLOSING,
			InterpolationNestingOpening = _NESTING_OPENING,
			InterpolationArmBlock = BuildCaseArmBlock(),
			OpeningContextPredicate = HereDocumentOpeningIsNotTheHereStringsLastAngle,
			CapturedTerminator = new CapturedTerminator
			{
				OpeningExpression = openingExpression,
				TerminatorCaptureGroupName = _TERMINATOR_CAPTURE_GROUP,
				NonInterpolatingQuoteCaptureGroupName = _QUOTED_TERMINATOR_CAPTURE_GROUP,
				AnsiCQuotedCaptureGroupName = _ANSI_C_TERMINATOR_CAPTURE_GROUP,
				DoubleQuotedCaptureGroupName = _DOUBLE_QUOTED_TERMINATOR_CAPTURE_GROUP,
				BodyStart = HereDocumentBodyStart.AfterTheOpeningLine
			}
		};
	}

	/// <summary>
	/// True at the start of a word: at the start of the text, or after a blank, a line feed, or one of <c>; &amp; | ( )</c>; false everywhere else, so
	/// <c>${#v}</c>, <c>$#</c>, <c>a#b</c>, <c>16#ff</c>, <c>&gt;#w</c> and <c>&lt;#w</c> do not open a comment. A predecessor that a backslash escapes
	/// (<c>a\ #b</c>, <c>a\;#b</c>) is part of the word, and a backslash-line-feed is removed before the word is read, so <c>a\</c> then <c>#b</c> on the
	/// next line is one word. A hash after an opening parenthesis that is a zsh glob qualifier (<c>(#i)</c>, <c>(#qN)</c>) is not a comment.
	/// </summary>
	private static bool CommentOpeningStandsHere(ReadOnlySpan<char> text, int delimiterPosition)
	{
		int wordPosition = StepBackOverLineContinuations(text, delimiterPosition);
		bool stands = wordPosition == 0
			|| (_COMMENT_PREDECESSORS.Contains(text[wordPosition - 1]) && CountBackslashesBefore(text, wordPosition - 1) % _ESCAPE_PARITY_DIVISOR == 0);

		return stands && !IsZshGlobQualifier(text, delimiterPosition);
	}

	/// <summary>Moves the position back over every backslash-line-feed pair directly before it, which a shell deletes before reading the word.</summary>
	private static int StepBackOverLineContinuations(ReadOnlySpan<char> text, int position)
	{
		int stepped = position;

		while (stepped >= _ESCAPED_LINE_FEED_LENGTH && text[stepped - 1] == _LINE_FEED && CountBackslashesBefore(text, stepped - 1) % _ESCAPE_PARITY_DIVISOR == 1)
		{
			stepped -= _ESCAPED_LINE_FEED_LENGTH;
		}

		return stepped;
	}

	private static int CountBackslashesBefore(ReadOnlySpan<char> text, int position)
	{
		int run = 0;

		while (position - run - 1 >= 0 && text[position - run - 1] == _ESCAPE_CHARACTER)
		{
			run++;
		}

		return run;
	}

	/// <summary>
	/// True for a hash directly after an opening parenthesis that starts a zsh glob qualifier: a letter, then characters that are not blanks or
	/// parentheses, then the closing parenthesis. A comment that follows a parenthesis puts a blank or a word after the hash and does not close the
	/// parenthesis in the same word.
	/// </summary>
	private static bool IsZshGlobQualifier(ReadOnlySpan<char> text, int delimiterPosition)
	{
		bool followsAParenthesis = delimiterPosition > 0 && text[delimiterPosition - 1] == _OPENING_PARENTHESIS;
		int cursor = delimiterPosition + _LINE_COMMENT_OPENING.Length;
		bool qualifier = false;

		if (followsAParenthesis && cursor < text.Length && char.IsAsciiLetter(text[cursor]))
		{
			while (cursor < text.Length && !char.IsWhiteSpace(text[cursor]) && text[cursor] != _OPENING_PARENTHESIS && text[cursor] != _CLOSING_PARENTHESIS)
			{
				cursor++;
			}

			qualifier = cursor < text.Length && text[cursor] == _CLOSING_PARENTHESIS;
		}

		return qualifier;
	}

	/// <summary>An opening quote preceded by an odd run of backslashes is escaped text, not an opening.</summary>
	private static bool QuoteOpeningIsNotBackslashEscaped(ReadOnlySpan<char> text, int delimiterPosition)
	{
		return CountBackslashesBefore(text, delimiterPosition) % _ESCAPE_PARITY_DIVISOR == 0;
	}

	/// <summary>
	/// Refuses the opening that follows another angle bracket (the here-string's last two). An opening inside a double-parenthesis expression, on the line
	/// or on a later one, is refused by the profile's arithmetic context. An unquoted <c>let v=1&lt;&lt;3</c> is not refused: every shell that has
	/// <c>let</c> reads <c>&lt;&lt;3</c> as a here-document opening.
	/// </summary>
	private static bool HereDocumentOpeningIsNotTheHereStringsLastAngle(ReadOnlySpan<char> text, int delimiterPosition)
	{
		return delimiterPosition == 0 || text[delimiterPosition - 1] != _HERE_STRING_ANGLE;
	}

	// ------------------------------------------------------------------------------------------
	// Element 2: identification
	// ------------------------------------------------------------------------------------------

	private static LanguageIdentification BuildLanguageIdentification()
	{
		LanguageIdentification identification = new()
		{
			FileExtensions = new[] { _SHELL_EXTENSION, _BASH_EXTENSION, _ZSH_EXTENSION, _KSH_EXTENSION }.ToFrozenSet(StringComparer.OrdinalIgnoreCase),
			DeclaredLanguageNames = new[] { _LANGUAGE_NAME }.ToFrozenSet(StringComparer.Ordinal),
			FirstLineDetectionExpression = ShellPatternExpressions.ShebangFirstLine(),
			LanguageNameDerivation = static (_, _) => _LANGUAGE_NAME,
			DeclaredUnsupportedCoverage = ShellDeclaredCoverage.Build()
		};

		return identification;
	}

	// ------------------------------------------------------------------------------------------
	// Element 5: candidate selection, templates, flow signals and the dialects of a file
	// ------------------------------------------------------------------------------------------

	private static CandidateSelectionAndRecommendation BuildCandidateSelectionAndRecommendation()
	{
		CandidateSelectionAndRecommendation element = new()
		{
			PreselectionAnchors = ShellRuleDeclarations.BuildPreselectionAnchors(),
			TemplateProvider = ShellTemplateDeclarations.Provide,
			FlowSignals = ShellFlowSignalDeclarations.Build(),
			LanguageFallbackTextByFlow = ShellTemplateDeclarations.FallbackTextByFlow,
			FileDialects = BuildFileDialects(),
			WrapperTracking = ShellWrapperDeclarations.Build()
		};

		return element;
	}

	/// <summary>
	/// The dialect a file is read in, resolved once per file: from the interpreter its first line names, then from its extension, then the default. A
	/// shebang that names <c>sh</c> reads with the semantics of <c>bash</c> 5.2, one that names <c>ksh</c> is <c>ksh93</c>, and a file with no shebang
	/// that names a mapped interpreter is read as <c>bash</c> unless its extension is <c>.zsh</c> or <c>.ksh</c>.
	/// </summary>
	private static FileDialects BuildFileDialects()
	{
		return new FileDialects
		{
			DefaultDialect = ShellDialectNames.BASH,
			ShebangInterpreters = new Dictionary<string, string>(StringComparer.Ordinal)
			{
				[_BASH_INTERPRETER] = ShellDialectNames.BASH,
				[_SH_INTERPRETER] = ShellDialectNames.BASH,
				[_ZSH_INTERPRETER] = ShellDialectNames.ZSH,
				[_KSH_INTERPRETER] = ShellDialectNames.KSH93,
				[_KSH93_INTERPRETER] = ShellDialectNames.KSH93,
				[_MKSH_INTERPRETER] = ShellDialectNames.MKSH
			}.ToFrozenDictionary(StringComparer.Ordinal),
			ExtensionDialects = new Dictionary<string, string>(StringComparer.Ordinal)
			{
				[_ZSH_EXTENSION] = ShellDialectNames.ZSH,
				[_KSH_EXTENSION] = ShellDialectNames.KSH93
			}.ToFrozenDictionary(StringComparer.Ordinal)
		};
	}
}
