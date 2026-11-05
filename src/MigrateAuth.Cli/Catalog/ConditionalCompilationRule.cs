using System.Collections.Immutable;

namespace MigrateAuth.Cli.Catalog;

/// <summary>
/// OPTIONAL, on a <see cref="LexicalProfile"/> as <see cref="LexicalProfile.ConditionalCompilation"/>, and declared only by
/// a profile whose language has conditional-compilation directives that the file itself can switch on and off (the
/// `CSharp` profile). It is data and nothing more: the engine reads it to build the file's conditional-region map, and a
/// profile that does not declare it is not read at all.
///
/// The directive vocabulary of the element (the keywords <c>if</c>, <c>elif</c>, <c>else</c>, <c>endif</c>, <c>define</c>
/// and <c>undef</c>) is the engine's and is the same for every profile that declares the element; what a profile supplies
/// is the opening that starts a directive line and the build symbols the file can never know the value of.
/// </summary>
public sealed record ConditionalCompilationRule
{
	/// <summary>
	/// The marker a well-known word may end with to read as a PREFIX: every symbol that starts with the word before the
	/// marker is well known, whatever follows (<c>NET*</c> covers every symbol that starts with <c>NET</c>).
	/// </summary>
	public const char PREFIX_WILDCARD_MARKER = '*';

	/// <summary>
	/// The marker a well-known word may end with to read as a PLATFORM WORD: the word itself, or the word followed by an
	/// optional tail of digits and underscores, with or without a trailing <c>OR_GREATER</c> (<c>IOS+</c> covers
	/// <c>IOS</c>, <c>IOS15_0</c> and <c>IOS15_0_OR_GREATER</c>).
	/// </summary>
	public const char VERSION_TAIL_MARKER = '+';

	/// <summary>The text that opens a directive at the start of a line, after any blanks. It is never empty and holds no blank or line terminator.</summary>
	public required string DirectiveOpening { get; init; }

	/// <summary>
	/// The symbols the build can define outside the file, so the file alone cannot say they are false. A word written bare
	/// names exactly that symbol; a word ending with <see cref="PREFIX_WILDCARD_MARKER"/> names every symbol that starts
	/// with it; a word ending with <see cref="VERSION_TAIL_MARKER"/> names the platform word with its optional version
	/// tail. A well-known symbol reads as unknown until the file <c>#undef</c>ines it in live code.
	/// </summary>
	public required ImmutableArray<string> WellKnownSymbolWords { get; init; }
}
