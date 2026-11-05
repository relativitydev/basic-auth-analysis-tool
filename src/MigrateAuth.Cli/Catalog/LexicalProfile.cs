using System.Collections.Immutable;
using System.Text.RegularExpressions;

namespace MigrateAuth.Cli.Catalog;

/// <summary>
/// Contract element 1. The lexical shape of a language as declarative data, in the
/// lexical vocabulary, including its constant declaration expression. One scanner is parameterized
/// by this; there is no scanner per language.
///
/// The three rules the vocabulary assigns to the engine are unrepresentable here by construction:
///
/// - The author hands its rules over through set-only properties. There is no accessor that
///   returns them in the order they were supplied, so the order an author wrote cannot reach
///   the engine at all. What the engine reads is the order the engine itself imposed, longest
///   opening first, through <see cref="LexicalEnginePolicy"/>.
/// - There is no unterminated-construct field, and no recursion-bound field. Both live on
///   <see cref="LexicalEnginePolicy"/>, where a profile cannot reach them.
/// </summary>
public sealed class LexicalProfile
{
	private ImmutableArray<LineCommentRule> _lineCommentRulesInEngineOrder = [];
	private ImmutableArray<BlockCommentRule> _blockCommentRulesInEngineOrder = [];
	private ImmutableArray<QuoteRule> _quoteRulesInEngineOrder = [];

	/// <summary>
	/// Names the profile, not the language. Two languages may share one profile, which is
	/// why the language name is derived by contract element 2 rather than read from here.
	/// </summary>
	public required string ProfileName { get; init; }

	/// <summary>
	/// Contract element 1: the profile carries the constant declaration expression, handed over
	/// already constructed, which is what file-scoped symbolic resolution collects with.
	/// </summary>
	public required Regex ConstantDeclarationExpression { get; init; }

	/// <summary>
	/// Optional, null by default: the member-declaration expression, which feeds the second dictionary of the file symbol table (the member records, kept apart from the variable
	/// records of <see cref="ConstantDeclarationExpression"/>). It names the member in the group <c>name</c>, marks an environment-read value with the empty group
	/// <see cref="EnvironmentDeclarationGroups.MARKER"/> and carries the word of the fallback in <see cref="EnvironmentDeclarationGroups.FALLBACK_WORD"/>. It must carry the match
	/// bound, is anchored at a line start, reads one member per line, uses no look-behind and captures nothing outside its own span. It is run by the symbol-collection phase through
	/// the same timed door as the constant-declaration expression. A profile that reuses another profile's declarations declares this one itself: the field is never inherited.
	/// </summary>
	public Regex? MemberDeclarationExpression { get; init; }

	/// <summary>Case sensitivity, in keywords.</summary>
	public CaseSensitivity KeywordCaseSensitivity { get; init; } = CaseSensitivity.Sensitive;

	/// <summary>Case sensitivity, in delimiters.</summary>
	public CaseSensitivity DelimiterCaseSensitivity { get; init; } = CaseSensitivity.Sensitive;

	/// <summary>
	/// Selects the comparer of the file-scoped symbol table. Defaults to
	/// <see cref="IdentifierCaseSensitivityMode.Sensitive"/>, the ordinal comparison.
	/// </summary>
	public IdentifierCaseSensitivityMode IdentifierCaseSensitivity { get; init; } = IdentifierCaseSensitivityMode.Sensitive;

	/// <summary>
	/// The character that escapes the next one in code (a shell's backslash), or null where the language has none. Where
	/// it is declared, the code walk, in the file's own code and in
	/// every hole's interior, steps over the escape and the character after it as one: the escaped character opens no
	/// literal, no comment and no here-document, closes no hole, counts as no bracket and starts no keyword, so
	/// <c>\)</c> is text, <c>\"</c> opens no string, <c>\#</c> is no comment and <c>\&lt;&lt;EOF</c> opens no here-document. An
	/// escaped line feed joins the lines and drains nothing. The arithmetic read-ahead steps over escapes the same way.
	/// A whitespace character is refused by the validator.
	/// </summary>
	public char? CodeEscapeCharacter { get; init; }

	/// <summary>
	/// The arithmetic context, inside which a quote rule that defers its body start opens nothing, or null where
	/// the language has none. See <see cref="ArithmeticContextRule"/>.
	/// </summary>
	public ArithmeticContextRule? ArithmeticContext { get; init; }

	/// <summary>
	/// The executed-string context, or null where the language has none. Where it is declared, the body of a string
	/// that a shell runs as a command is read as code. See
	/// <see cref="ExecutedStringContext"/>.
	/// </summary>
	public ExecutedStringContext? ExecutedStrings { get; init; }

	/// <summary>
	/// The code-span elements, empty where the language has none, which is every shipped profile but `Shell`. A span
	/// is a run of code that one token opens and closes on the same physical line, whose content is a command of its own. See <see cref="CodeSpanElement"/>.
	/// </summary>
	public ImmutableArray<CodeSpanElement> CodeSpans { get; init; } = [];

	/// <summary>
	/// The conditional-compilation element, or null where the language has none, which is every shipped profile
	/// but `CSharp`. Where it is declared, the engine reads the file's own directives into a
	/// region map and a match whose anchor lies in a branch the file's own symbols switch off proposes Low. See <see cref="ConditionalCompilationRule"/>.
	/// </summary>
	public ConditionalCompilationRule? ConditionalCompilation { get; init; }

	/// <summary>Write-only on purpose: the order supplied here is discarded, never read back.</summary>
	public required IEnumerable<LineCommentRule> LineCommentRules
	{
		init => _lineCommentRulesInEngineOrder = LexicalEnginePolicy.OrderLineCommentRules(value);
	}

	/// <summary>Write-only on purpose: the order supplied here is discarded, never read back.</summary>
	public required IEnumerable<BlockCommentRule> BlockCommentRules
	{
		init => _blockCommentRulesInEngineOrder = LexicalEnginePolicy.OrderBlockCommentRules(value);
	}

	/// <summary>Write-only on purpose: the order supplied here is discarded, never read back.</summary>
	public required IEnumerable<QuoteRule> QuoteRules
	{
		init => _quoteRulesInEngineOrder = LexicalEnginePolicy.OrderQuoteRules(value);
	}

	/// <summary>The only readable order, and it is the engine's: longest opening first.</summary>
	public ImmutableArray<LineCommentRule> LineCommentRulesInEngineOrder => _lineCommentRulesInEngineOrder;

	/// <summary>The only readable order, and it is the engine's: longest opening first.</summary>
	public ImmutableArray<BlockCommentRule> BlockCommentRulesInEngineOrder => _blockCommentRulesInEngineOrder;

	/// <summary>The only readable order, and it is the engine's: longest opening first.</summary>
	public ImmutableArray<QuoteRule> QuoteRulesInEngineOrder => _quoteRulesInEngineOrder;
}
