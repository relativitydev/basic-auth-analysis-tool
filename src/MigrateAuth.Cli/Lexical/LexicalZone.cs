namespace MigrateAuth.Cli.Lexical;

/// <summary>
/// THE FOUR LEXICAL ZONES. ONE PER POSITION, NEVER TWO AND NEVER NONE.
///
/// The backing type is a byte to keep the per-file memory small. Per file in flight
/// there are three structures - the bytes read, the decoded text at two bytes per UTF-16 code
/// unit, and this mask at one byte per code unit. A richer per-position record, or a compressed
/// one that has to be decoded to be read, would grow that.
///
/// <see cref="Delimiter"/> IS LOAD-BEARING AND IS NOT COSMETIC. A rule recognizing a credential
/// header written as a string literal necessarily anchors at the literal's own opening quote, and
/// that form is High confidence under the confidence rubric. With the whole literal marked
/// non-code that rule could never fire. The delimiter is part of the syntactic expression; only
/// what lies between the delimiters is the literal's body.
/// </summary>
public enum LexicalZone : byte
{
	/// <summary>
	/// Executable code, and the default. A position that no construct claimed is code, which is
	/// why this is the zero value: the mask is allocated zeroed and only non-code is written.
	/// </summary>
	Code = 0,

	/// <summary>
	/// A line or block comment, INCLUDING its own opening and closing markers. The
	/// <see cref="Delimiter"/> zone belongs to the delimiters of a LITERAL and to nothing else, so a
	/// comment is one zone from its opener to its end.
	/// </summary>
	Comment = 1,

	/// <summary>The body of a string literal, and never the delimiters that open or close it.</summary>
	StringBody = 2,

	/// <summary>
	/// The opening and closing tokens of a literal, its literal prefix where it carries one, and
	/// the opening and closing tokens of an interpolation hole. It COUNTS AS CODE.
	/// </summary>
	Delimiter = 3
}

/// <summary>What a zone means to a reader of the mask, and nothing beyond that.</summary>
public static class LexicalZones
{
	/// <summary>
	/// Whether a position counts as code. It answers the question
	/// the mask exists to answer and stops there: WHAT a later stage does with a
	/// <see cref="LexicalZone.Comment"/> or a <see cref="LexicalZone.StringBody"/> position -
	/// demote, never discard - is that stage's decision and is not pre-empted here.
	/// </summary>
	public static bool CountsAsCode(LexicalZone zone)
	{
		return zone is LexicalZone.Code or LexicalZone.Delimiter;
	}
}
