namespace MigrateAuth.Cli.Catalog.Languages;

/// <summary>
/// The lexical profile of the `Go` analyzer, written in the shipped vocabulary with no predicate, no prefix, no hole and no change to the vocabulary. Two comment rules (the line comment and the
/// block comment, which does not nest) and three quote rules:
///
/// - the interpreted string, opened and closed by a double quote, which forbids a raw line feed and escapes with a backslash;
/// - the rune literal, opened and closed by a single quote, with the same newline policy and escape, so <c>'"'</c>, <c>'\''</c>, <c>'\\'</c> and <c>'/'</c> are literals and never a string or a comment;
/// - the raw string, opened and closed by a back quote, which allows a line feed and has no escape character, so a backslash before the closing back quote is text and the literal ends there.
///
/// A literal that `Go` itself rejects (an interpreted or rune literal across a line, an escaped line feed) is recovered at the line end by the scanner, and only an unterminated block comment
/// abandons the file, from its opening. A directive comment is a line comment and is masked as one. A struct tag is a string literal and is masked as the literal it is.
///
/// The profile also declares the member declaration expression (EX-GO-102): the second dictionary of the file symbol table, one record per `func` declaration, marked when the encoded Basic helper reading of
/// GO-011 holds, which the call-site class GO-013 resolves a called name against.
/// </summary>
internal static class GoLexicalDeclarations
{
	private const string _PROFILE_NAME = "Go";

	private const string _LINE_COMMENT_OPENING = "//";
	private const string _BLOCK_COMMENT_OPENING = "/*";
	private const string _BLOCK_COMMENT_CLOSING = "*/";

	private const string _INTERPRETED_STRING_QUOTE = "\"";
	private const string _RUNE_QUOTE = "'";
	private const string _RAW_STRING_QUOTE = "`";

	private const char _ESCAPE_CHARACTER = '\\';

	internal static LexicalProfile BuildLexicalProfile()
	{
		LexicalProfile profile = new()
		{
			ProfileName = _PROFILE_NAME,
			ConstantDeclarationExpression = GoPatternExpressions.ConstantDeclaration(),
			MemberDeclarationExpression = GoPatternExpressions.MemberDeclaration(),
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
				BuildQuoteRuleWithEscape(_INTERPRETED_STRING_QUOTE),
				BuildQuoteRuleWithEscape(_RUNE_QUOTE),
				BuildRawStringRule()
			]
		};

		return profile;
	}

	/// <summary>An interpreted string or a rune literal: a raw line feed is forbidden, and a backslash escapes the next character, an escaped line feed included, which does not continue the literal.</summary>
	private static QuoteRule BuildQuoteRuleWithEscape(string quote)
	{
		return new QuoteRule
		{
			Opening = quote,
			Closing = quote,
			NewlinePolicy = NewlinePolicy.Forbidden,
			EscapeCharacter = _ESCAPE_CHARACTER
		};
	}

	/// <summary>The raw string: a line feed is allowed and there is no escape character, so a backslash before the closing back quote is text.</summary>
	private static QuoteRule BuildRawStringRule()
	{
		return new QuoteRule
		{
			Opening = _RAW_STRING_QUOTE,
			Closing = _RAW_STRING_QUOTE,
			NewlinePolicy = NewlinePolicy.Allowed
		};
	}
}
