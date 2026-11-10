using System.Text.RegularExpressions;

namespace MigrateAuth.Cli.Catalog.Languages;

/// <summary>
/// Every expression the `Shell` analyzer hands over, generated at build time. This file holds the four the lexical profile and the identification
/// need (the two here-document openings, the constant declaration and the first-line rule) and the three content markers of the demotion signals; the pattern classes and the flow signals are declared in
/// the partial files that follow. Each declares its timeout by what evaluates it: the openings and the first-line rule run in the lexical pass and
/// carry <see cref="LexicalEnginePolicy.PROVISIONAL_LEXICAL_BUDGET_IN_MILLISECONDS"/>, the constant declaration runs in the recognition pass and
/// carries <see cref="RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS"/>.
///
/// THE OPENINGS are anchored at the scan position (<c>\G</c>), so a position that is not an opening costs the engine one failed anchored attempt and
/// the expression is never searched forward. A here-document word is read as the shell reads it: one or more parts, each single-quoted, double-quoted
/// (either may be empty), one backslash-escaped character, an ANSI-C or translated quote, or a bare run, and the word ends at a blank or a shell
/// metacharacter. Each part is captured on its own and the scanner joins the captures, so a word quoted in pieces is the unquoted word, quoted. A
/// backslash-newline is a line continuation that the shell removes before it reads the word: it is consumed as a joiner and never captured. No part
/// starts with a backslash and a newline, so the joiner is never ambiguous with a part, the bare run is an atomic group and no two alternatives start
/// alike, so a word that fails at its end is walked once.
/// </summary>
internal static partial class ShellPatternExpressions
{
	/// <summary>
	/// The plain opening: the two angles, not the here-string's third angle and not the tab-stripping dash, optional blanks, then the terminator word.
	/// The tab-stripping opening has its own expression because the two rules close on different lines.
	/// </summary>
	[GeneratedRegex(
		@"\G<<(?![<-])(?:[ \t]|\\\n)*(?:(?:'(?<terminator>[^']*)'(?<quoted>)|""(?<terminator>(?<dquoted>(?:[^""\\]|\\[\s\S])*))""(?<quoted>)|\\(?<terminator>[^\s])(?<quoted>)|(?>(?<terminator>[^\s|&;()<>'""\\$]+))|\$'(?<terminator>(?<ansic>(?:[^'\\]|\\[\s\S])*))'(?<quoted>)|\$""(?<terminator>(?<dquoted>(?:[^""\\]|\\[\s\S])*))""(?<quoted>)|(?<terminator>\$)(?!['""]))(?:\\\n)*)+(?![^\s|&;()<>])",
		RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture,
		matchTimeoutMilliseconds: LexicalEnginePolicy.PROVISIONAL_LEXICAL_BUDGET_IN_MILLISECONDS)]
	public static partial Regex PlainOpening();

	/// <summary>The tab-stripping opening, the two angles and a dash, with the same terminator forms and the same continuations.</summary>
	[GeneratedRegex(
		@"\G<<-(?!<)(?:[ \t]|\\\n)*(?:(?:'(?<terminator>[^']*)'(?<quoted>)|""(?<terminator>(?<dquoted>(?:[^""\\]|\\[\s\S])*))""(?<quoted>)|\\(?<terminator>[^\s])(?<quoted>)|(?>(?<terminator>[^\s|&;()<>'""\\$]+))|\$'(?<terminator>(?<ansic>(?:[^'\\]|\\[\s\S])*))'(?<quoted>)|\$""(?<terminator>(?<dquoted>(?:[^""\\]|\\[\s\S])*))""(?<quoted>)|(?<terminator>\$)(?!['""]))(?:\\\n)*)+(?![^\s|&;()<>])",
		RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture,
		matchTimeoutMilliseconds: LexicalEnginePolicy.PROVISIONAL_LEXICAL_BUDGET_IN_MILLISECONDS)]
	public static partial Regex TabStrippingOpening();

	/// <summary>
	/// A statement-start assignment, with the optional declaration keyword and its flags; the value is a literal only: a single-quoted one, a
	/// double-quoted one holding no expansion, backtick or backslash, or a bare word holding none of them. A value that expands, an assignment that is
	/// not at statement start and one inside a here-document body are not declarations.
	/// </summary>
	[GeneratedRegex(
		@"^[ \t]*(?:(?:readonly|export|local|declare|typeset)[ \t]+(?:-[A-Za-z]+[ \t]+)*)?(?<name>[A-Za-z_][A-Za-z0-9_]*)=(?:'(?<value>[^'\n]*)'|""(?<value>[^""$`\\\n]*)""|(?<value>[^\s'""$`\\;&|<>()]+))(?=[ \t;]|$)",
		RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture | RegexOptions.Multiline,
		matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex ConstantDeclaration();

	/// <summary>
	/// The first-line detection of a file with no recognized extension: a shebang that names <c>bash</c>, <c>sh</c>, <c>zsh</c> or <c>ksh</c>, directly or
	/// through <c>env</c> with its flags. It reads what the base shebang table reads: the interpreter is the last path segment of the first word, or,
	/// where that word is <c>env</c>, the first following word that is not a flag, taken as written; a word is a run of anything that is not white
	/// space, and white space is every character the base table splits on (<c>\s</c> is the same set), so a vertical tab, a form feed and a no-break
	/// space separate words here as they do there. Case-sensitive, and it carries the lexical budget as its timeout.
	/// </summary>
	[GeneratedRegex(
		@"^#!\s*(?:[^\s/]*/)*(?:env\s+(?:-\S*\s+)*)?(?:bash|sh|zsh|ksh)(?:\s|\z)",
		RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture,
		matchTimeoutMilliseconds: LexicalEnginePolicy.PROVISIONAL_LEXICAL_BUDGET_IN_MILLISECONDS)]
	public static partial Regex ShebangFirstLine();

	/// <summary>
	/// The generated-file header, a content marker over the whole text, <c>AnyZone</c> because the header is itself a comment. Two alternatives anchored at
	/// the start of the file. The first reads a <c>#</c> comment on line 1, or on line 2 when line 1 is the interpreter line, that holds
	/// <c>auto-generated</c> (the separator a hyphen, a blank or nothing) or <c>generated by</c>, in any case. The second reads a <c>#</c> comment within the
	/// first ten lines that holds a do-not-edit phrase: <c>do not edit</c>, <c>do not modify</c>, <c>do not change</c>, <c>do not hand-edit</c>,
	/// <c>changes will be lost</c> or <c>changes will be overwritten</c>, in any case. A comment on a later line that says only <c>generated by</c> is a
	/// report script's own header and is not a marker.
	/// </summary>
	[GeneratedRegex(
		@"(?:\A(?:#![^\n]*\n)?[ \t]*#[^\n]*?(?i:auto[\- ]?generated|generated[ \t]+by)|\A(?:[^\n]*\n){0,9}[ \t]*#[^\n]*?(?i:do[ \t]+not[ \t]+(?:edit|modify|change|hand-edit)|changes[ \t]+will[ \t]+be[ \t]+(?:lost|overwritten)))",
		RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture,
		matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex GeneratedHeaderContentMarker();

	/// <summary>
	/// A script that sources a shell unit-test library: the dot command or <c>source</c> at the start of a line, then a path on that line (before any comment)
	/// that names one of the libraries. <c>CodeOnly</c>, so the same words in a comment or a string are not a test.
	/// </summary>
	[GeneratedRegex(
		@"^[ \t]*(?:\.|source)[ \t]+[^\n#]*(?:shunit2|bashunit|assert\.sh|bats-(?:assert|support)|shellspec_helper|roundup)",
		RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture | RegexOptions.Multiline,
		matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex SourcedTestLibraryContentMarker();

	/// <summary>
	/// A behaviour-driven test block: a <c>Describe</c> line with a quoted name, then, within a hundred lines and before another <c>Describe</c> line, an
	/// <c>It</c> line with a quoted name. <c>CodeOnly</c>.
	/// </summary>
	[GeneratedRegex(
		@"^[ \t]*Describe[ \t]+[\x27\x22][^\n]*\n(?:(?:(?![ \t]*Describe[ \t])[^\n]*\n)" + _BACKTRACK_POINT + @"){0,100}?[ \t]*It[ \t]+[\x27\x22]",
		RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture | RegexOptions.Multiline,
		matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex BehaviourDrivenTestContentMarker();
}
