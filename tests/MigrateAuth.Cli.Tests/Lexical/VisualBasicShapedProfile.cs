using System.Collections.Immutable;
using System.Text.RegularExpressions;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Lexical;

namespace MigrateAuth.Cli.Tests.Lexical;

/// <summary>
/// THE PINNED REFERENCE PROFILE of the `VBNet` lexical vocabulary, built here from the profile vocabulary and not read from the shipped catalog. The shipped
/// profile is pinned to it by <see cref="VBNetLexicalProfileEqualityTests"/>: the same rules in the same order, field by field, and the same zones on the
/// real-shaped text of <see cref="VisualBasicRealShapedTextTests"/>. The `$"` rule declares the doubled interpolation opener as literal text, so
/// `$"{{x}}"` is string body.
/// </summary>
internal static class VisualBasicShapedProfile
{
	private const string _PROFILE_NAME = "VisualBasicShapedTestProfile";

	/// <summary>`Const` and field declarations with a string initializer, and a bare assignment at a statement start (the catalog).</summary>
	private const string _CONSTANT_PATTERN =
		@"(?:^|:)[ \t]*(?:(?:Public|Private|Friend|Protected|Shared|ReadOnly|Static)[ \t]+)*(?:Const|Dim)[ \t]+(?<name>[A-Za-z_]\w*)\$?(?:[ \t]+As[ \t]+String)?[ \t]*=[ \t]*(?<value>""(?:[^""\n]|"""")*"")";

	private const string _APOSTROPHE = "'";
	private const string _TYPOGRAPHIC_APOSTROPHE_LEFT = "‘";
	private const string _TYPOGRAPHIC_APOSTROPHE_RIGHT = "’";
	private const string _REM = "REM";
	private const string _DOUBLE_QUOTE = "\"";
	private const string _INTERPOLATED_OPENING = "$\"";
	private const string _TYPOGRAPHIC_LEFT = "“";
	private const string _TYPOGRAPHIC_RIGHT = "”";
	private const string _HOLE_OPENING = "{";
	private const string _HOLE_CLOSING = "}";
	private const char _BRACKET_OPENING = '[';

	internal static LexicalProfile Build()
	{
		return new LexicalProfile
		{
			ProfileName = _PROFILE_NAME,
			ConstantDeclarationExpression = new Regex(_CONSTANT_PATTERN, RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.Multiline),
			KeywordCaseSensitivity = CaseSensitivity.Insensitive,
			DelimiterCaseSensitivity = CaseSensitivity.Insensitive,
			IdentifierCaseSensitivity = IdentifierCaseSensitivityMode.Insensitive,
			LineCommentRules =
			[
				new LineCommentRule { Opening = _APOSTROPHE },
				new LineCommentRule { Opening = _TYPOGRAPHIC_APOSTROPHE_LEFT },
				new LineCommentRule { Opening = _TYPOGRAPHIC_APOSTROPHE_RIGHT },
				new LineCommentRule { Opening = _REM, RequiresWordBoundary = true, OpeningContextPredicate = RemIsNotABracketedName }
			],
			BlockCommentRules = [],
			QuoteRules = BuildQuoteRules()
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
				Opening = _DOUBLE_QUOTE,
				Closing = _DOUBLE_QUOTE,
				NewlinePolicy = NewlinePolicy.Allowed,
				DoubledClosingEscapes = true
			},
			new QuoteRule
			{
				Opening = _INTERPOLATED_OPENING,
				Closing = _DOUBLE_QUOTE,
				NewlinePolicy = NewlinePolicy.Allowed,
				DoubledClosingEscapes = true,
				InterpolationOpening = _HOLE_OPENING,
				InterpolationClosing = _HOLE_CLOSING,
				InterpolationNestingOpening = _HOLE_OPENING,
				DoubledInterpolationOpeningIsLiteral = true
			},
			new QuoteRule
			{
				Opening = _TYPOGRAPHIC_LEFT,
				Closing = _TYPOGRAPHIC_RIGHT,
				NewlinePolicy = NewlinePolicy.Allowed,
				DoubledClosingEscapes = true
			},
			new QuoteRule
			{
				Opening = _TYPOGRAPHIC_RIGHT,
				Closing = _TYPOGRAPHIC_RIGHT,
				NewlinePolicy = NewlinePolicy.Allowed,
				DoubledClosingEscapes = true
			}
		];
	}

	/// <summary>
	/// `REM` opens a comment wherever it stands as a whole word outside brackets, after `.` included (as the VB compiler reads it); it is a name only as
	/// `[Rem]`, so the one position that does not open is the one right after an opening square bracket.
	/// </summary>
	private static bool RemIsNotABracketedName(ReadOnlySpan<char> text, int delimiterPosition)
	{
		return delimiterPosition == 0 || text[delimiterPosition - 1] != _BRACKET_OPENING;
	}
}
