namespace MigrateAuth.Cli.Catalog;

/// <summary>
/// THE CODE-SPAN ELEMENT of the lexical vocabulary: a span of code that a shell opens and closes with one token and whose content is a command position of its
/// own, the first user being the backtick command substitution. At a position that the scan holds as code, in any code region it walks, an opening that no
/// odd run of the code escape precedes starts a search for the closing on the SAME physical line (the first closing token that no odd run of
/// <see cref="EscapeCharacter"/> precedes, no nesting counted, bounded by the line-length ceiling and by the per-scan search budget of the inline constructs).
/// Where one is found the content, with the characters of <see cref="EscapableCharacters"/> unescaped, is scanned as the outermost code region of a nested scan:
/// the opening and the closing are delimiters, the content takes the zones of the nested scan, and a here-document opened in the content has an EMPTY body, so
/// each pending opening is dropped at the span's end, stays code, and the lines after the span are read as code.
///
/// A span whose closing is not on the opening line, an unbalanced construct inside, a nested scan abandoned or recovered and the nesting bound all leave the span
/// as it was, an ordinary run of characters, with no anomaly recorded. A profile that declares none (every shipped profile but `Shell`) never takes any of these branches.
/// </summary>
public sealed record CodeSpanElement
{
	/// <summary>The token that opens the span (one token, no line terminator).</summary>
	public required string Opening { get; init; }

	/// <summary>The token that closes the span: the same token as <see cref="Opening"/> for a backtick.</summary>
	public required string Closing { get; init; }

	/// <summary>The one character that escapes the next one inside the span (the shell backtick's backslash).</summary>
	public required string EscapeCharacter { get; init; }

	/// <summary>The characters the escape removes in front of when the span's content is read as code: for the shell backtick, a backtick, a backslash and a dollar sign.</summary>
	public required string EscapableCharacters { get; init; }
}
