using System.Text.RegularExpressions;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Lexical;

namespace MigrateAuth.Cli.Tests.Lexical;

/// <summary>
/// A TEST-LOCAL PROFILE in the shape of the Go lexical vocabulary, built here from the shipped profile vocabulary and not read from any catalog.
/// It uses NO predicate and NO new vocabulary field (the profile design). It declares: the line comment <c>//</c>; the block comment <c>/* */</c>, which does
/// not nest in Go and does not nest in the vocabulary; the interpreted string <c>"</c> and the rune <c>'</c>, both with a backslash escape and
/// <see cref="NewlinePolicy.Forbidden"/> (an escaped line feed does NOT continue them in Go, so the policy is not EscapedContinues); and the raw
/// string <c>`</c>, <see cref="NewlinePolicy.Allowed"/> with NO escape character (a backslash is text, so a back quote after a backslash closes
/// the literal). Go has no literal prefixes, no interpolation and no here-document. A struct tag is a raw or an interpreted literal and needs no
/// rule of its own. A cgo preamble is a comment to the scanner, and SQL text inside a literal is not modelled.
///
/// The required <see cref="LexicalProfile.ConstantDeclarationExpression"/> is the one non-lexical member: it is a provisional stand-in that counts
/// <c>name := "value"</c>, <c>var name type = "value"</c> and the same with a raw literal, one line, anchored at the line start. The catalog's
/// real expression is not this one. It is verified on invented text only.
/// </summary>
internal static partial class GoShapedProfile
{
	private const string _PROFILE_NAME = "GoShapedTestProfile";
	private const string _LINE_COMMENT_OPENING = "//";
	private const string _BLOCK_COMMENT_OPENING = "/*";
	private const string _BLOCK_COMMENT_CLOSING = "*/";
	private const string _INTERPRETED_QUOTE = "\"";
	private const string _RUNE_QUOTE = "'";
	private const string _RAW_QUOTE = "`";
	private const char _ESCAPE_CHARACTER = '\\';

	/// <summary>Provisional constant declaration: optional <c>const</c> or <c>var</c>, the name, an optional type, then <c>:=</c> or <c>=</c> and a single-line interpreted or raw literal that ends the statement.</summary>
	[GeneratedRegex(
		@"^[ \t]*(?:(?:const|var)[ \t]+)?(?<name>[A-Za-z_][A-Za-z0-9_]*)(?:[ \t]+[A-Za-z_][A-Za-z0-9_.]*)?[ \t]*(?::=|=)[ \t]*(?:""(?<value>(?:[^""\\\n]|\\.)*)""|`(?<value>[^`\n]*)`)(?=[ \t]*(?:;|$|//|/\*))",
		RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture | RegexOptions.Multiline,
		matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	internal static partial Regex ConstantDeclaration();

	internal static LexicalProfile Build()
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
				new LineCommentRule { Opening = _LINE_COMMENT_OPENING }
			],
			BlockCommentRules =
			[
				new BlockCommentRule { Opening = _BLOCK_COMMENT_OPENING, Closing = _BLOCK_COMMENT_CLOSING }
			],
			QuoteRules =
			[
				BuildEscapedRule(_INTERPRETED_QUOTE),
				BuildEscapedRule(_RUNE_QUOTE),
				new QuoteRule
				{
					Opening = _RAW_QUOTE,
					Closing = _RAW_QUOTE,
					NewlinePolicy = NewlinePolicy.Allowed
				}
			]
		};
	}

	internal static ZoneMask Scan(string source)
	{
		return new LexicalScanner(Build()).Scan(source);
	}

	private static QuoteRule BuildEscapedRule(string quote)
	{
		return new QuoteRule
		{
			Opening = quote,
			Closing = quote,
			NewlinePolicy = NewlinePolicy.Forbidden,
			EscapeCharacter = _ESCAPE_CHARACTER
		};
	}
}
