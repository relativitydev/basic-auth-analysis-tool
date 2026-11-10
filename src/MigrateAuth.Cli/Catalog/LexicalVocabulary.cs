using System.Collections.Immutable;
using System.Text.RegularExpressions;

namespace MigrateAuth.Cli.Catalog;

/// <summary>
/// Case sensitivity. Declared separately for keywords and for delimiters because
/// they are not the same question: a language may compare its delimiters without regard
/// to case while its keywords are compared exactly, and a single flag cannot say that.
/// </summary>
public enum CaseSensitivity
{
	Sensitive = 0,
	Insensitive = 1
}

/// <summary>
/// How the file-scoped symbol table compares identifier names: ordinally, or without
/// regard to case. Separate from <see cref="CaseSensitivity"/> because the question is a different
/// one (names in declarations and lookups, not keywords or delimiters). Only the comparer is selected;
/// any scope-prefix or brace normalisation is the catalog's and the analyzer's to decide.
/// </summary>
public enum IdentifierCaseSensitivityMode
{
	Sensitive = 0,
	Insensitive = 1
}

/// <summary>
/// Positional anchoring. An opening or a closing that only counts as one where it
/// stands at the start or at the end of a line.
/// </summary>
public enum PositionalAnchoring
{
	Anywhere = 0,
	StartOfLine = 1,
	EndOfLine = 2,

	/// <summary>
	/// Only spaces and tabs may stand between the end of the delimiter and the
	/// line feed (or the end of the file), which is how a here-string opener is accepted by the
	/// language: trailing blanks after it are legal, any other trailing text makes it an ordinary
	/// quote. Meant for an opening; a closing that must stand alone on its line is
	/// <see cref="StartOfLine"/>.
	/// </summary>
	EndOfLineAfterOptionalBlanks = 3,

	/// <summary>
	/// CLOSING ONLY. The closing starts at column zero and a line feed or the end of the file
	/// follows it immediately, with no trailing blank: a here-document's terminator stands alone on
	/// its line.
	/// </summary>
	WholeLine = 4,

	/// <summary>
	/// CLOSING ONLY. Only tab characters stand between the previous line feed (or the start of the
	/// text) and the closing, and a line feed or the end of the file follows it: the tab-stripping
	/// here-document's terminator.
	/// </summary>
	WholeLineAfterLeadingTabs = 5
}

/// <summary>
/// Where a captured-terminator literal's body starts. A here-document's body begins on the line
/// AFTER the one that holds its opening, so the rest of the opening line is code.
/// </summary>
public enum HereDocumentBodyStart
{
	/// <summary>The body starts right after the opening match. The default.</summary>
	AfterTheOpening = 0,

	/// <summary>
	/// The body starts after the line feed that ends the opening line; the rest of that line is
	/// scanned as code, and the openings met on it wait, in order, for their bodies.
	/// </summary>
	AfterTheOpeningLine = 1
}

/// <summary>
/// Newline policy, and it is three-valued on purpose. A boolean conflates
/// <see cref="Forbidden"/> with <see cref="EscapedContinues"/>; the scanner then reads a
/// continued literal as unterminated, which turns its closing quote into an opening one
/// and inverts the zone mask for the rest of the line.
/// </summary>
public enum NewlinePolicy
{
	/// <summary>A raw newline ends the literal and is an anomaly; an escape cannot continue it.</summary>
	Forbidden = 0,

	/// <summary>A raw newline is still an anomaly, but an escape before it continues the literal.</summary>
	EscapedContinues = 1,

	/// <summary>Newlines are content.</summary>
	Allowed = 2
}

/// <summary>
/// Variable-length delimiter. An opening of arbitrary length whose closing must be
/// at least as long as the opening that started it.
/// </summary>
public enum DelimiterLength
{
	Fixed = 0,
	VariableWithAtLeastEqualClosing = 1
}

/// <summary>
/// Literal prefix. What a prefix does to escape handling for the same quote pair.
/// </summary>
public enum EscapeHandling
{
	AsDeclaredByTheQuoteRule = 0,
	Suppressed = 1
}

/// <summary>
/// Delimiter context predicate: a delimiter that only counts as one depending on the
/// preceding significant token, or on its position within a word.
///
/// A dedicated delegate rather than a <see cref="Func{T1, T2, TResult}"/>, because the text
/// arrives as a span and the engine must not be forced to copy the file to ask the question.
/// </summary>
public delegate bool DelimiterContextPredicate(ReadOnlySpan<char> text, int delimiterPosition);

/// <summary>
/// Literal prefix. A modifier that changes escape behaviour for the same quote pair,
/// so that a raw form and an escaped form are one rule rather than two.
/// </summary>
public sealed record LiteralPrefixRule
{
	public required string Prefix { get; init; }

	public required EscapeHandling EscapeHandling { get; init; }

	/// <summary>An escape character that replaces the quote rule's own, or null to keep it.</summary>
	public char? EscapeCharacterOverride { get; init; }

	public CaseSensitivity Sensitivity { get; init; } = CaseSensitivity.Sensitive;
}

/// <summary>
/// Suspended sub-range. A range inside a literal where the closing delimiter does not
/// apply, which is what stops a character class from ending a regular expression literal at
/// its first slash and truncating the construct.
/// </summary>
public sealed record SuspendedSubRange
{
	public required string Opening { get; init; }

	public required string Closing { get; init; }
}

/// <summary>
/// Captured terminator. A here-document opens with a token the author chooses and
/// closes on that same token, so its closing delimiter is not a constant of the rule but
/// text captured at the opening. Whether the body interpolates is decided per occurrence,
/// by how the opening token was quoted, which is why that too is a capture group and not a
/// property of the rule.
/// </summary>
public sealed record CapturedTerminator
{
	/// <summary>
	/// The opening expression, handed over already constructed. It carries the
	/// group that captures the terminator token. It is matched within the logical line that holds the
	/// opening: a line feed after an odd run of backslashes continues the line, so an expression may consume
	/// the backslash-newline pair, without capturing it, as a joiner inside the word
	/// (<c>&lt;&lt;E\&lt;newline&gt;F</c> is the word <c>EF</c>, as the shell reads it).
	/// </summary>
	public required Regex OpeningExpression { get; init; }

	/// <summary>
	/// The group that captures the terminator word. The terminator is the CONCATENATION OF EVERY CAPTURE of
	/// this group, in order, so an expression may capture each part of a word that is quoted in pieces (a
	/// bare run, a single-quoted part, a double-quoted part, a backslash-escaped character) and the scanner
	/// reads the effective word the shell reads, with the quoting removed. An expression that captures the
	/// group once reads that one capture as the word.
	/// </summary>
	public required string TerminatorCaptureGroupName { get; init; }

	/// <summary>
	/// The group whose captures are ANSI-C quoted text (a shell's <c>$'...'</c>), or null. Each capture of
	/// <see cref="TerminatorCaptureGroupName"/> that spans exactly one capture of this group is decoded by the
	/// ANSI-C escapes (<c>\n \t \xHH \nnn \uHHHH \cX</c> and the rest of the C-style set) before the captures
	/// are joined, so <c>$'E\x4fF'</c> is the word <c>EOF</c>. The expression nests this group inside the
	/// terminator group. A profile of a shell that has no ANSI-C quoting (dash) does not declare it.
	/// </summary>
	public string? AnsiCQuotedCaptureGroupName { get; init; }

	/// <summary>
	/// The group whose captures are double-quoted text, or null. Each capture of
	/// <see cref="TerminatorCaptureGroupName"/> that spans exactly one capture of this group is unescaped by the
	/// double-quote rule before the captures are joined: a backslash before a backslash, a double quote, a
	/// dollar sign or a backtick is removed, as is a backslash with the line feed after it, any other backslash
	/// stays, so <c>"E\"F"</c> is the word <c>E"F</c>.
	/// The expression nests this group inside the terminator group.
	/// </summary>
	public string? DoubleQuotedCaptureGroupName { get; init; }

	/// <summary>
	/// The group whose participation at an occurrence says that occurrence's body does not
	/// interpolate, or null where every occurrence behaves the same way.
	/// </summary>
	public string? NonInterpolatingQuoteCaptureGroupName { get; init; }

	/// <summary>
	/// Where the body starts. <see cref="HereDocumentBodyStart.AfterTheOpeningLine"/> requires a
	/// whole-line closing anchoring, newlines <see cref="NewlinePolicy.Allowed"/> and an opening
	/// anchored <see cref="PositionalAnchoring.Anywhere"/>; the validator refuses any other
	/// combination.
	/// </summary>
	public HereDocumentBodyStart BodyStart { get; init; } = HereDocumentBodyStart.AfterTheOpening;
}

/// <summary>
/// An arm block inside a bracketed interpolation hole: a keyword pair (<c>case</c> and <c>esac</c> in a
/// shell) between which a closing token that no opening matched is an ARM TERMINATOR, not the end of the
/// hole. Without it <c>"$(case a in a) echo hit;; esac)"</c> ends the hole at the arm's <c>)</c> and reads
/// the rest of the substitution as string body.
///
/// The keywords count only where a command may begin, which is what keeps a word <c>case</c> among a
/// command's arguments from opening a block: at the start of the hole, after one of
/// <see cref="CommandPositionPredecessors"/> (blanks skipped), after one of
/// <see cref="CommandPositionKeywords"/>, directly after the closing token that ended an arm's pattern, or
/// after the empty parentheses of a function definition; not after an assignment word or a plain command
/// word. A closing keyword ends only a block opened at the same nesting depth: one inside a nested
/// substitution never ends the outer block, and a block opened inside a nested substitution stays open until its own closing keyword (the nested substitution's closing token is that block's arm terminator, and a block never closed is the unbalanced nesting below).
/// A block that never closes is an unbalanced nesting: while the per-file recovery bound lasts the hole is re-read
/// by its first closing, and once it is spent the hole is abandoned as unterminated. Legal only beside
/// <see cref="QuoteRule.InterpolationNestingOpening"/>, whose depth it is measured against.
/// </summary>
public sealed record ArmBlockRule
{
	/// <summary>The keyword that opens a block (a shell's <c>case</c>).</summary>
	public required string OpeningKeyword { get; init; }

	/// <summary>The keyword that ends the block opened at the same nesting depth (a shell's <c>esac</c>).</summary>
	public required string ClosingKeyword { get; init; }

	/// <summary>
	/// The characters after which, with blanks skipped, a keyword stands at a command position (a shell's
	/// line feed, <c>;</c>, <c>&amp;</c>, <c>|</c> and opening parenthesis: the last four also cover <c>;;</c>, <c>&amp;&amp;</c>
	/// and <c>||</c>). A closing token is not one: it is a predecessor only where it ended an arm's pattern.
	/// </summary>
	public required string CommandPositionPredecessors { get; init; }

	/// <summary>
	/// Words after which a keyword stands at a command position: the reserved words that begin a command list
	/// (a shell's <c>then</c>, <c>do</c>, <c>else</c>, <c>elif</c>, <c>if</c>, <c>while</c>, <c>until</c>, <c>time</c>,
	/// <c>coproc</c>) and the symbols <c>{</c> and <c>!</c>. A word is a run of characters that are neither blanks nor
	/// predecessors. THE CHAIN RULE: such a word licenses a keyword after it only where it stands at a command
	/// position itself, or after a chain of such words whose first does, so a word of that spelling among a command's
	/// arguments (<c>echo then case</c>) licenses nothing.
	/// </summary>
	public ImmutableArray<string> CommandPositionKeywords { get; init; } = [];

	/// <summary>
	/// The words of <see cref="CommandPositionKeywords"/> that take options (a shell's <c>time</c>): words that begin
	/// with a hyphen between such a word and the keyword leave the keyword at a command position (<c>time -p case</c>),
	/// and a plain argument there does not (<c>time x -p case</c>).
	/// </summary>
	public ImmutableArray<string> OptionTakingKeywords { get; init; } = [];
}

/// <summary>
/// The arithmetic context: a bracketed expression in which a deferred-body opening is an operator and not
/// an opening (a shell's <c>$(( 1 &lt;&lt; 3 ))</c> and <c>(( n &lt;&lt; 2 ))</c>). It is read by the scanner and
/// not by a context predicate because it spans lines, and a predicate sees one.
///
/// <see cref="Opening"/> is a run of one bracket character (two or more), and <see cref="Closing"/> a run of
/// its partner of THE SAME LENGTH, adjacent: a closing run of at least that length is what ends the expression, after
/// its first <c>Closing.Length</c> brackets. The context starts
/// where an opening run starts in code, or where the interpolation opening of a bracketed hole ends in the
/// first bracket of the run (<c>$((</c>), and only where the text closes it as arithmetic: the scanner reads
/// ahead from the end of the opening run, counting brackets of the opening kind, skipping escapes, line comments and
/// quoted runs (read by the quote rule's own escape handling, a literal prefix's included), to the first closing
/// bracket met with none open. If a whole closing run follows it there, the
/// expression ends after that run. If not, the opening run was two nested groups, as a shell reads
/// <c>((cmd) ...)</c>, and no context starts, so an opening inside is read as in any command. A run that is
/// never closed starts no context, so it cannot swallow what follows. One read-ahead answers every run it passes, so
/// each position is read once; a read-ahead is started afresh only for a run no earlier one passed in code, and
/// those have a per-file budget proportional to the text. A bracket in a
/// literal or a comment is not counted. Inside the context a nested command substitution (a bracket after
/// the profile's substitution opening, not itself a run) is a command context again, where an opening
/// stands, with its own parentheses and arm blocks.
/// </summary>
public sealed record ArithmeticContextRule
{
	/// <summary>The run of one bracket character, two or more, that opens the expression (a shell's <c>((</c>).</summary>
	public required string Opening { get; init; }

	/// <summary>
	/// The run of the partner bracket, as long as the opening, that closes it (a shell's <c>))</c>). The declaration
	/// is validated to have the opening's length; the scanner ends the expression after exactly that many closing
	/// brackets and accepts a longer run, whose further brackets are not part of the expression.
	/// </summary>
	public required string Closing { get; init; }
}

/// <summary>
/// Line comment rule. <see cref="RequiresWordBoundary"/> is the positional form of the
/// context predicate: an opener that is a word only opens a comment when it stands as a whole
/// word, or an identifier that merely begins with those letters swallows the line.
/// </summary>
public sealed record LineCommentRule
{
	public required string Opening { get; init; }

	public bool RequiresWordBoundary { get; init; }

	public PositionalAnchoring OpeningAnchoring { get; init; } = PositionalAnchoring.Anywhere;

	public DelimiterContextPredicate? OpeningContextPredicate { get; init; }
}

/// <summary>Block comment rule, with positional anchoring on both ends.</summary>
public sealed record BlockCommentRule
{
	public required string Opening { get; init; }

	public required string Closing { get; init; }

	public PositionalAnchoring OpeningAnchoring { get; init; } = PositionalAnchoring.Anywhere;

	public PositionalAnchoring ClosingAnchoring { get; init; } = PositionalAnchoring.Anywhere;

	public DelimiterContextPredicate? OpeningContextPredicate { get; init; }
}

/// <summary>
/// Quote rule. One quoting construct, carrying the whole vocabulary that applies to a
/// literal: opening, closing, escape, doubled close, newline policy, interpolation, variable
/// length, literal prefixes, suspended sub-range, positional anchoring and captured terminator.
///
/// What is deliberately absent: the unterminated-construct policy and the recursion bound.
/// Both belong to the engine and are uniform across every rule, so there is no field for
/// either and an author cannot supply one.
/// </summary>
public sealed record QuoteRule
{
	public required string Opening { get; init; }

	/// <summary>
	/// Empty exactly when <see cref="CapturedTerminator"/> is declared: the closing is then
	/// text captured at the opening rather than a constant of the rule.
	/// </summary>
	public required string Closing { get; init; }

	public required NewlinePolicy NewlinePolicy { get; init; }

	public char? EscapeCharacter { get; init; }

	public bool DoubledClosingEscapes { get; init; }

	/// <summary>
	/// The sigil or bracket that opens an interpolation hole. Which KIND of hole it opens is
	/// decided by which of the two fields below is declared beside it, and exactly one of them
	/// must be.
	/// </summary>
	public string? InterpolationOpening { get; init; }

	/// <summary>
	/// The FIRST hole kind, the bracketed one: the hole ends at this token, found by the same
	/// recursive descent that scans the literal, so that a closing brace belonging to a nested
	/// string inside the hole does not close it.
	/// </summary>
	public string? InterpolationClosing { get; init; }

	/// <summary>
	/// A token that opens a NESTED level inside a bracketed hole, legal only
	/// beside <see cref="InterpolationClosing"/>. While the scanner reads the hole's code region it
	/// counts this token and accepts the closing only at depth zero, so a closing token that belongs
	/// to a nested pair does not end the hole. A token inside a nested literal or comment is skipped,
	/// because those are scanned by the literal and comment halves and never reach the count. Null
	/// keeps the first-closing behaviour, which is what every profile that does not nest declares.
	///
	/// RECOVERY. If the depth has not returned to zero by the end of the hole, the hole falls back to
	/// the first closing and the file records one lexical anomaly (<see cref="Lexical.LexicalAnomalyReason.UnbalancedInterpolationNesting"/>)
	/// instead of abandoning the rest of the file.
	/// </summary>
	public string? InterpolationNestingOpening { get; init; }

	/// <summary>
	/// A keyword pair inside which a closing token with no opening to match is an arm terminator and does not
	/// end the bracketed hole (see <see cref="ArmBlockRule"/>). Legal only beside
	/// <see cref="InterpolationNestingOpening"/>. Null, the default, declares no arm block.
	/// </summary>
	public ArmBlockRule? InterpolationArmBlock { get; init; }

	/// <summary>
	/// A doubled <see cref="InterpolationOpening"/> in the literal's body is literal text and
	/// opens no hole, as in a C# interpolated string where <c>{{</c> is one literal brace. The
	/// scanner pairs openers from the left, so <c>{{{x}}}</c> reads the pair, a hole, then two
	/// closing body characters. Legal only beside both <see cref="InterpolationOpening"/> and
	/// <see cref="InterpolationClosing"/>. Default <c>false</c>: a doubled opener has no special reading.
	/// </summary>
	public bool DoubledInterpolationOpeningIsLiteral { get; init; }

	/// <summary>
	/// The SECOND hole kind, an identifier-extent kind whose end is a declared identifier
	/// expression, handed over already constructed.
	///
	/// A sigil followed by an identifier, with NO CLOSING TOKEN AT ALL. The hole runs from the
	/// end of <see cref="InterpolationOpening"/> to the end of whatever this expression matches
	/// there, and the expression is required to match AT that position rather than anywhere
	/// after it. Where it does not match, the sigil is ordinary literal text and opens nothing -
	/// which is the common case of a currency sign or a shell variable sigil before a space.
	/// </summary>
	public Regex? InterpolationIdentifierExtent { get; init; }

	public DelimiterLength DelimiterLength { get; init; } = DelimiterLength.Fixed;

	public PositionalAnchoring OpeningAnchoring { get; init; } = PositionalAnchoring.Anywhere;

	public PositionalAnchoring ClosingAnchoring { get; init; } = PositionalAnchoring.Anywhere;

	public ImmutableArray<LiteralPrefixRule> LiteralPrefixes { get; init; } = [];

	public SuspendedSubRange? SuspendedSubRange { get; init; }

	public CapturedTerminator? CapturedTerminator { get; init; }

	public DelimiterContextPredicate? OpeningContextPredicate { get; init; }
}
