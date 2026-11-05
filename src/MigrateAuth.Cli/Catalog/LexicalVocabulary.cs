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
	EndOfLineAfterOptionalBlanks = 3
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
	/// group that captures the terminator token.
	/// </summary>
	public required Regex OpeningExpression { get; init; }

	public required string TerminatorCaptureGroupName { get; init; }

	/// <summary>
	/// The group whose participation at an occurrence says that occurrence's body does not
	/// interpolate, or null where every occurrence behaves the same way.
	/// </summary>
	public string? NonInterpolatingQuoteCaptureGroupName { get; init; }
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
