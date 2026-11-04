using System.Collections.Immutable;
using System.Text.RegularExpressions;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Lexical;

namespace MigrateAuth.Cli.Tests.Lexical;

/// <summary>
/// A TEST-LOCAL PROFILE in the shape of a POSIX-family shell's lexical vocabulary, built here from the
/// profile vocabulary and not read from any shipped catalog. It exists so that every row of the shell
/// declaration table can be run on real-shaped text, on the scanner: word-start comments, quotes with and without escapes, the ANSI-C prefix, nested
/// substitution holes, and the two here-document forms with their deferred body start, whole-line
/// closings, empty terminators and opening refusals.
/// </summary>
internal static partial class ShellShapedProfile
{
	private const string _PROFILE_NAME = "ShellShapedTestProfile";
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
	private const string _COMMENT_PREDECESSORS = " \t\n;&|()";
	private const char _ESCAPE_CHARACTER = '\\';
	private const char _HERE_STRING_ANGLE = '<';
	private const char _LINE_FEED = '\n';
	private const char _OPENING_PARENTHESIS = '(';
	private const char _CLOSING_PARENTHESIS = ')';

	private static readonly string[] _commandPositionKeywords = ["{", "!", "then", "do", "else", "elif", "if", "while", "until", "time", "coproc"];

	/// <summary>
	/// The plain opening: <c>&lt;&lt;</c>, not the here-string's third angle and not the tab-stripping dash, optional
	/// blanks, then the terminator word as the shell reads it: one or more parts, each single-quoted, double-quoted
	/// (either may be empty), one backslash-escaped character, or a bare run, ending at a blank or a shell
	/// metacharacter. Each part is captured on its own and the scanner joins the captures, so a word quoted in
	/// pieces (<c>E"O"F</c>) is the unquoted word <c>EOF</c>, quoted. A <c>$</c> that opens an ANSI-C or translated
	/// quote is not part of a word: <c>$'...'</c> is an ANSI-C quoted part (decoded by the scanner, as <c>bash</c>,
	/// <c>zsh</c>, <c>ksh93</c> and <c>mksh</c> read it: <c>$'E\x4fF'</c> is <c>EOF</c>) and <c>$"..."</c> a translated
	/// double-quoted one. <c>dash</c> has no ANSI-C quoting and reads <c>$'EOF'</c> as <c>$EOF</c>; this profile is the
	/// bash family's, and the difference is a declared limit of the catalog. The bare run is an atomic group and no two
	/// alternatives start alike, so a word that fails at its end is walked once. A backslash-newline is a line continuation:
	/// the shell removes the pair before it reads the word, so the pair is a joiner that is consumed and not captured, before
	/// the word, between its parts and after it (<c>E\&lt;newline&gt;F</c> is the word <c>EF</c>, and the opening line is the one
	/// that ends the command after the continuation), and inside a double-quoted part, where the decoder removes it too. No
	/// part starts with a backslash and a newline, so the joiner is never ambiguous with a part and the word is still read in
	/// one pass.
	/// </summary>
	[GeneratedRegex(
		@"\G<<(?![<-])(?:[ \t]|\\\n)*(?:(?:'(?<terminator>[^']*)'(?<quoted>)|""(?<terminator>(?<dquoted>(?:[^""\\]|\\[\s\S])*))""(?<quoted>)|\\(?<terminator>[^\s])(?<quoted>)|(?>(?<terminator>[^\s|&;()<>'""\\$]+))|\$'(?<terminator>(?<ansic>(?:[^'\\]|\\[\s\S])*))'(?<quoted>)|\$""(?<terminator>(?<dquoted>(?:[^""\\]|\\[\s\S])*))""(?<quoted>)|(?<terminator>\$)(?!['""]))(?:\\\n)*)+(?![^\s|&;()<>])",
		RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture,
		matchTimeoutMilliseconds: LexicalEnginePolicy.PROVISIONAL_LEXICAL_BUDGET_IN_MILLISECONDS)]
	internal static partial Regex PlainOpening();

	/// <summary>The tab-stripping opening, <c>&lt;&lt;-</c>, with the same terminator forms and the same continuations.</summary>
	[GeneratedRegex(
		@"\G<<-(?!<)(?:[ \t]|\\\n)*(?:(?:'(?<terminator>[^']*)'(?<quoted>)|""(?<terminator>(?<dquoted>(?:[^""\\]|\\[\s\S])*))""(?<quoted>)|\\(?<terminator>[^\s])(?<quoted>)|(?>(?<terminator>[^\s|&;()<>'""\\$]+))|\$'(?<terminator>(?<ansic>(?:[^'\\]|\\[\s\S])*))'(?<quoted>)|\$""(?<terminator>(?<dquoted>(?:[^""\\]|\\[\s\S])*))""(?<quoted>)|(?<terminator>\$)(?!['""]))(?:\\\n)*)+(?![^\s|&;()<>])",
		RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture,
		matchTimeoutMilliseconds: LexicalEnginePolicy.PROVISIONAL_LEXICAL_BUDGET_IN_MILLISECONDS)]
	internal static partial Regex TabStrippingOpening();

	/// <summary>A statement-start assignment, with the optional declaration keyword and its flags; the value is a literal only.</summary>
	[GeneratedRegex(
		@"^[ \t]*(?:(?:readonly|export|local|declare|typeset)[ \t]+(?:-[A-Za-z]+[ \t]+)*)?(?<name>[A-Za-z_][A-Za-z0-9_]*)=(?:'(?<value>[^'\n]*)'|""(?<value>[^""$`\\\n]*)""|(?<value>[^\s'""$`\\;&|<>()]+))(?=[ \t;]|$)",
		RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture | RegexOptions.Multiline,
		matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	internal static partial Regex ConstantDeclaration();

	internal static LexicalProfile Build()
	{
		return Build(executedStrings: null);
	}

	/// <summary>
	/// The same profile with its executed-string context declared (the test-local one of
	/// <see cref="ShellExecutedStringsDeclaration"/>), so a string that a shell runs as a command is read as code.
	/// </summary>
	internal static LexicalProfile BuildWithExecutedStrings()
	{
		return Build(ShellExecutedStringsDeclaration.Build());
	}

	internal static ZoneMask ScanWithExecutedStrings(string source)
	{
		return new LexicalScanner(BuildWithExecutedStrings()).Scan(source);
	}

	private static LexicalProfile Build(ExecutedStringContext? executedStrings)
	{
		return new LexicalProfile
		{
			ProfileName = _PROFILE_NAME,
			ConstantDeclarationExpression = ConstantDeclaration(),
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
			ExecutedStrings = executedStrings
		};
	}

	/// <summary>
	/// The shell's arm block: a closing parenthesis met between <c>case</c> and <c>esac</c> with no opening to
	/// match ends an arm's pattern and not the substitution.
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

	internal static ZoneMask Scan(string source)
	{
		return new LexicalScanner(Build()).Scan(source);
	}

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
				TabStrippingOpening(),
				PositionalAnchoring.WholeLineAfterLeadingTabs),
			BuildHereDocumentRule(
				_PLAIN_HERE_DOCUMENT_OPENING,
				PlainOpening(),
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
	/// True at the start of a word: at the start of the text, or after a blank, a line feed, or one of
	/// <c>; &amp; | ( )</c>; false everywhere else, so <c>${#v}</c>, <c>$#</c>, <c>a#b</c>, <c>16#ff</c>, <c>&gt;#w</c> and <c>&lt;#w</c> do not
	/// open a comment (the angle brackets are not comment predecessors in any dialect). A predecessor that a backslash escapes (<c>a\ #b</c>, <c>a\;#b</c>) is part of the word, and a
	/// backslash-line-feed is removed before the word is read, so <c>a\</c> then <c>#b</c> on the next line is one word.
	/// A hash after an opening parenthesis that is a zsh glob qualifier (<c>(#i)</c>, <c>(#qN)</c>) is not a comment.
	/// </summary>
	private static bool CommentOpeningStandsHere(ReadOnlySpan<char> text, int delimiterPosition)
	{
		int wordPosition = StepBackOverLineContinuations(text, delimiterPosition);
		bool stands = wordPosition == 0
			|| (_COMMENT_PREDECESSORS.Contains(text[wordPosition - 1]) && CountBackslashesBefore(text, wordPosition - 1) % 2 == 0);

		return stands && !IsZshGlobQualifier(text, delimiterPosition);
	}

	/// <summary>Moves the position back over every backslash-line-feed pair directly before it, which a shell deletes before reading the word.</summary>
	private static int StepBackOverLineContinuations(ReadOnlySpan<char> text, int position)
	{
		int stepped = position;

		while (stepped >= 2 && text[stepped - 1] == _LINE_FEED && CountBackslashesBefore(text, stepped - 1) % 2 == 1)
		{
			stepped -= 2;
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
	/// True for a hash directly after an opening parenthesis that starts a zsh glob qualifier: a letter, then
	/// characters that are not blanks or parentheses, then the closing parenthesis. A comment that follows a
	/// parenthesis puts a blank or a word after the hash and does not close the parenthesis in the same word.
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
		return CountBackslashesBefore(text, delimiterPosition) % 2 == 0;
	}

	/// <summary>
	/// Refuses the opening that follows another angle bracket (the here-string's last two). An opening inside a
	/// double-parenthesis expression, on the line or on a later one, is refused by the profile's arithmetic context. An
	/// unquoted <c>let v=1&lt;&lt;3</c> is not refused: every shell that has <c>let</c> reads <c>&lt;&lt;3</c> as a
	/// here-document opening.
	/// </summary>
	private static bool HereDocumentOpeningIsNotTheHereStringsLastAngle(ReadOnlySpan<char> text, int delimiterPosition)
	{
		return delimiterPosition == 0 || text[delimiterPosition - 1] != _HERE_STRING_ANGLE;
	}
}
