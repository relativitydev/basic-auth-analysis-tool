using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Lexical;
using MigrateAuth.Cli.Tests.Fixtures;

namespace MigrateAuth.Cli.Tests.Lexical;

/// <summary>
/// A TEST-LOCAL PROFILE WHOSE MULTI-CHARACTER TOKENS CONTAIN THE ARITHMETIC BRACKETS. The hole opens with
/// <c>@[</c>, closes with <c>]@</c> and counts <c>@[</c> as its nesting opening, while the arithmetic context is a
/// run of <c>[</c> closed by a run of <c>]</c>: the walk steps across each of those tokens at once, so a bracket
/// that sits inside a token reaches the arithmetic count only if the walk looks at every character of the token.
/// The deferred here-document rule is the shell-shaped one.
/// </summary>
internal static class BracketTokenProfile
{
	private const string _PROFILE_NAME = "BracketTokenTestProfile";
	private const string _DOUBLE_QUOTE = "\"";
	private const string _BACKTICK = "`";
	private const string _HOLE_OPENING = "@[";
	private const string _HOLE_CLOSING = "]@";
	private const string _ARITHMETIC_OPENING = "[[";
	private const string _ARITHMETIC_CLOSING = "]]";
	private const string _HERE_DOCUMENT_OPENING = "<<";
	private const string _CLOSING_IS_CAPTURED_AT_THE_OPENING = "";
	private const string _TERMINATOR_CAPTURE_GROUP = "terminator";
	private const string _QUOTED_TERMINATOR_CAPTURE_GROUP = "quoted";
	private const char _ESCAPE_CHARACTER = '\\';

	internal static LexicalProfile Build()
	{
		return new LexicalProfile
		{
			ProfileName = _PROFILE_NAME,
			ConstantDeclarationExpression = HereDocumentExemplarPatternExpressions.ConstantDeclaration(),
			LineCommentRules = [],
			BlockCommentRules = [],
			QuoteRules = [BuildTheQuoteRule(), BuildTheBacktickRule(), BuildTheHereDocumentRule()],
			ArithmeticContext = new ArithmeticContextRule { Opening = _ARITHMETIC_OPENING, Closing = _ARITHMETIC_CLOSING }
		};
	}

	private static QuoteRule BuildTheQuoteRule()
	{
		return new QuoteRule
		{
			Opening = _DOUBLE_QUOTE,
			Closing = _DOUBLE_QUOTE,
			NewlinePolicy = NewlinePolicy.Allowed,
			EscapeCharacter = _ESCAPE_CHARACTER,
			InterpolationOpening = _HOLE_OPENING,
			InterpolationClosing = _HOLE_CLOSING,
			InterpolationNestingOpening = _HOLE_OPENING
		};
	}

	private static QuoteRule BuildTheBacktickRule()
	{
		return new QuoteRule
		{
			Opening = _BACKTICK,
			Closing = _BACKTICK,
			NewlinePolicy = NewlinePolicy.Allowed,
			EscapeCharacter = _ESCAPE_CHARACTER
		};
	}

	private static QuoteRule BuildTheHereDocumentRule()
	{
		return new QuoteRule
		{
			Opening = _HERE_DOCUMENT_OPENING,
			Closing = _CLOSING_IS_CAPTURED_AT_THE_OPENING,
			NewlinePolicy = NewlinePolicy.Allowed,
			ClosingAnchoring = PositionalAnchoring.WholeLine,
			CapturedTerminator = new CapturedTerminator
			{
				OpeningExpression = ShellShapedProfile.PlainOpening(),
				TerminatorCaptureGroupName = _TERMINATOR_CAPTURE_GROUP,
				NonInterpolatingQuoteCaptureGroupName = _QUOTED_TERMINATOR_CAPTURE_GROUP,
				BodyStart = HereDocumentBodyStart.AfterTheOpeningLine
			}
		};
	}
}
