using System.Collections.Frozen;
using System.Collections.Immutable;

using MigrateAuth.Cli.Findings;
using MigrateAuth.Cli.Reading;

namespace MigrateAuth.Cli.Catalog.Languages;

/// <summary>
/// The `JavaScript` analyzer: the lexical profile of ECMAScript source (line and block comments, the hashbang that opens only
/// at the first position, the two quoted strings that continue on an escaped line feed, the template literal with its nesting holes, and the regular-expression literal whose
/// opening a backward predicate decides), the identification of its files, its seventeen pattern classes, its demotion signals and its declared coverage.
///
/// FILES. The analyzer type is <c>file</c>-scoped, so its expressions live in <see cref="JavaScriptPatternExpressions"/>, its detection rules in
/// <see cref="JavaScriptRuleDeclarations"/>, its demotion signals in <see cref="JavaScriptDemotionDeclarations"/> and its declared coverage in
/// <see cref="JavaScriptDeclaredCoverage"/>. The registration is the partial method below.
///
/// FLOW, DIALECTS AND TEMPLATES. The sixteen flow signals live in <see cref="JavaScriptFlowSignalDeclarations"/>, the recommendation templates and the language fallback text of each
/// flow in <see cref="JavaScriptTemplateDeclarations"/>, and the dialects of a file are declared below: <c>script</c> by default and <c>node</c> for a Node.js shebang or the
/// <c>.cjs</c> extension, with none for <c>.jsx</c> or <c>.mjs</c>. No construct precedence, wrapper tracking, variable use, arithmetic context or executed-string context is
/// declared, because `JavaScript` needs none.
/// </summary>
public static partial class EmbeddedLanguageAnalyzers
{
	static partial void AddJavaScriptAnalyzer(ICollection<ILanguageAnalyzer> analyzers)
	{
		analyzers.Add(new JavaScriptAnalyzer());
	}
}

/// <summary>
/// The `JavaScript` analyzer. File-local per the registration pattern every embedded language follows, so no engine type can name it and no special
/// case can be written against it.
/// </summary>
file sealed class JavaScriptAnalyzer : ILanguageAnalyzer
{
	private const string _PROFILE_NAME = "JavaScript";
	private const string _LANGUAGE_NAME = "JavaScript";

	private const string _SCRIPT_EXTENSION = ".js";
	private const string _MODULE_EXTENSION = ".mjs";
	private const string _COMMONJS_EXTENSION = ".cjs";
	private const string _JSX_EXTENSION = ".jsx";

	private const string _NODE_INTERPRETER = "node";
	private const string _NODEJS_INTERPRETER = "nodejs";

	private const string _LINE_COMMENT_OPENING = "//";
	private const string _BLOCK_COMMENT_OPENING = "/*";
	private const string _BLOCK_COMMENT_CLOSING = "*/";
	private const string _HASHBANG_OPENING = "#!";
	private const string _SINGLE_QUOTE = "'";
	private const string _DOUBLE_QUOTE = "\"";
	private const string _TEMPLATE_QUOTE = "`";
	private const string _TEMPLATE_HOLE_OPENING = "${";
	private const string _TEMPLATE_HOLE_CLOSING = "}";
	private const string _TEMPLATE_HOLE_NESTING_OPENING = "{";
	private const string _REGEX_DELIMITER = "/";
	private const string _CLASS_OPENING = "[";
	private const string _CLASS_CLOSING = "]";

	private const char _ESCAPE_CHARACTER = '\\';
	private const char _LESS_THAN = '<';
	private const char _PLUS = '+';
	private const char _MINUS = '-';
	private const char _DOT = '.';
	private const char _UNDERSCORE = '_';
	private const char _DOLLAR = '$';
	private const int _HASHBANG_POSITION = 0;
	private const int _NOT_FOUND = -1;
	private const int _ONE_CHARACTER_BEFORE = 1;

	/// <summary>The characters after which a slash divides: the closers of a group, an index, a block, a literal or a regular expression, and the slash itself (the end of a regular expression or of a block comment).</summary>
	private const string _DIVIDING_CHARACTERS = ")]}/'\"`";

	/// <summary>The words after which a slash opens a regular expression: the keywords that stand where an operand is expected.</summary>
	private static readonly FrozenSet<string> _regexOpeningKeywords = new[]
	{
		"return", "typeof", "instanceof", "in", "of", "new", "delete", "void", "throw", "case", "do", "else", "yield", "await"
	}.ToFrozenSet(StringComparer.Ordinal);

	public LexicalProfile LexicalProfile { get; } = BuildLexicalProfile();

	public LanguageIdentification LanguageIdentification { get; } = BuildLanguageIdentification();

	public FrozenDictionary<string, DetectionRule> DetectionRulesByPatternClass { get; } = JavaScriptRuleDeclarations.BuildDetectionRules();

	public LanguageShapedDemotionSignals DemotionSignals { get; } = JavaScriptDemotionDeclarations.Build();

	public CandidateSelectionAndRecommendation CandidateSelectionAndRecommendation { get; } = BuildCandidateSelectionAndRecommendation();

	public FrozenSet<SubsumptionRule> SubsumptionRelation { get; } = JavaScriptRuleDeclarations.BuildSubsumptionRelation();

	// ------------------------------------------------------------------------------------------
	// Element 1: the lexical profile
	// ------------------------------------------------------------------------------------------

	private static LexicalProfile BuildLexicalProfile()
	{
		LexicalProfile profile = new()
		{
			ProfileName = _PROFILE_NAME,
			ConstantDeclarationExpression = JavaScriptPatternExpressions.ConstantDeclaration(),
			KeywordCaseSensitivity = CaseSensitivity.Sensitive,
			DelimiterCaseSensitivity = CaseSensitivity.Sensitive,
			IdentifierCaseSensitivity = IdentifierCaseSensitivityMode.Sensitive,
			LineCommentRules =
			[
				new LineCommentRule { Opening = _LINE_COMMENT_OPENING },
				new LineCommentRule { Opening = _HASHBANG_OPENING, OpeningContextPredicate = HashbangStandsAtTheStartOfTheText }
			],
			BlockCommentRules =
			[
				new BlockCommentRule { Opening = _BLOCK_COMMENT_OPENING, Closing = _BLOCK_COMMENT_CLOSING }
			],
			QuoteRules = BuildQuoteRules()
		};

		return profile;
	}

	/// <summary>
	/// Four quote rules. The single and double quoted strings continue on an escaped line feed and end at a raw one; the template takes newlines, escapes with a
	/// backslash and has one bracketed hole whose braces nest; the regular expression forbids a newline, escapes with a backslash, suspends its closing inside a character class
	/// and opens only where the backward predicate says a regular expression can start. A raw template, a tagged template and a private name need no rule of their own.
	/// </summary>
	private static ImmutableArray<QuoteRule> BuildQuoteRules()
	{
		return
		[
			BuildStringRule(_SINGLE_QUOTE),
			BuildStringRule(_DOUBLE_QUOTE),
			new QuoteRule
			{
				Opening = _TEMPLATE_QUOTE,
				Closing = _TEMPLATE_QUOTE,
				NewlinePolicy = NewlinePolicy.Allowed,
				EscapeCharacter = _ESCAPE_CHARACTER,
				InterpolationOpening = _TEMPLATE_HOLE_OPENING,
				InterpolationClosing = _TEMPLATE_HOLE_CLOSING,
				InterpolationNestingOpening = _TEMPLATE_HOLE_NESTING_OPENING
			},
			new QuoteRule
			{
				Opening = _REGEX_DELIMITER,
				Closing = _REGEX_DELIMITER,
				NewlinePolicy = NewlinePolicy.Forbidden,
				EscapeCharacter = _ESCAPE_CHARACTER,
				SuspendedSubRange = new SuspendedSubRange { Opening = _CLASS_OPENING, Closing = _CLASS_CLOSING },
				OpeningContextPredicate = RegexOpeningStandsHere
			}
		];
	}

	private static QuoteRule BuildStringRule(string quote)
	{
		return new QuoteRule
		{
			Opening = quote,
			Closing = quote,
			NewlinePolicy = NewlinePolicy.EscapedContinues,
			EscapeCharacter = _ESCAPE_CHARACTER
		};
	}

	/// <summary>The hashbang opens only at the very first position of the text: the decoder strips a byte-order mark, so position zero is the first character of the file, and an <c>#!</c> anywhere else is code.</summary>
	private static bool HashbangStandsAtTheStartOfTheText(ReadOnlySpan<char> text, int delimiterPosition)
	{
		return delimiterPosition == _HASHBANG_POSITION;
	}

	/// <summary>
	/// THE BACKWARD PREDICATE, one pure function of the text and the position. True when the slash at <paramref name="delimiterPosition"/> opens a regular expression
	/// literal, false when it divides. It reads backwards from the slash over white space, line feeds included, for at most
	/// <see cref="ReadingCeilings.MAXIMUM_LINE_LENGTH_IN_CHARACTERS"/> characters, and answers "opens" when it reaches the start of the text or that ceiling. The previous
	/// significant character decides: an operator, an opening bracket, a comma, a colon, a semicolon, a question mark, an exclamation mark or a greater-than sign opens; a
	/// less-than sign opens except a slash directly after it (a closing tag); a closing parenthesis, bracket or brace divides (the closing brace keeps a self-closing tag after an expression hole out of the regular-expression reading); <c>++</c> and <c>--</c> divide; a slash divides; a
	/// string, template or identifier character divides unless the word is one of the keywords that stand where an operand is expected and is not a property name; any other character opens, because a
	/// slash read as a regular expression that is really a division recovers at the line end, while the opposite error can abandon the tail of the file.
	/// </summary>
	private static bool RegexOpeningStandsHere(ReadOnlySpan<char> text, int delimiterPosition)
	{
		int previous = FindThePreviousSignificantCharacter(text, delimiterPosition);
		bool opens = true;

		if (previous != _NOT_FOUND)
		{
			opens = PreviousCharacterOpensARegularExpression(text, previous, delimiterPosition);
		}

		return opens;
	}

	/// <summary>The position of the nearest non-white-space character before the slash, or <see cref="_NOT_FOUND"/> at the start of the text or when the gap reaches the ceiling.</summary>
	private static int FindThePreviousSignificantCharacter(ReadOnlySpan<char> text, int delimiterPosition)
	{
		int found = _NOT_FOUND;
		int offset = _ONE_CHARACTER_BEFORE;

		while (found == _NOT_FOUND && offset <= ReadingCeilings.MAXIMUM_LINE_LENGTH_IN_CHARACTERS && delimiterPosition - offset >= 0)
		{
			int candidate = delimiterPosition - offset;

			found = char.IsWhiteSpace(text[candidate]) ? _NOT_FOUND : candidate;
			offset++;
		}

		return found;
	}

	private static bool PreviousCharacterOpensARegularExpression(ReadOnlySpan<char> text, int previous, int slashPosition)
	{
		char character = text[previous];
		bool opens;

		if (character == _LESS_THAN)
		{
			opens = previous != slashPosition - _ONE_CHARACTER_BEFORE;
		}
		else if (character is _PLUS or _MINUS)
		{
			opens = previous == 0 || text[previous - _ONE_CHARACTER_BEFORE] != character;
		}
		else if (_DIVIDING_CHARACTERS.Contains(character, StringComparison.Ordinal))
		{
			opens = false;
		}
		else if (IsIdentifierCharacter(character))
		{
			opens = WordIsAnOpeningKeyword(text, previous);
		}
		else
		{
			opens = true;
		}

		return opens;
	}

	/// <summary>The identifier word that ends at the position is an opening keyword and is not a property name (not preceded by a dot, which also covers <c>?.</c>).</summary>
	private static bool WordIsAnOpeningKeyword(ReadOnlySpan<char> text, int lastCharacter)
	{
		int start = lastCharacter;
		int length = 1;

		while (start > 0 && length <= ReadingCeilings.MAXIMUM_LINE_LENGTH_IN_CHARACTERS && IsIdentifierCharacter(text[start - _ONE_CHARACTER_BEFORE]))
		{
			start--;
			length++;
		}

		bool afterADot = start > 0 && text[start - _ONE_CHARACTER_BEFORE] == _DOT;

		return !afterADot && _regexOpeningKeywords.GetAlternateLookup<ReadOnlySpan<char>>().Contains(text.Slice(start, length));
	}

	private static bool IsIdentifierCharacter(char character)
	{
		return char.IsLetterOrDigit(character) || character == _UNDERSCORE || character == _DOLLAR;
	}

	// ------------------------------------------------------------------------------------------
	// Element 2: identification
	// ------------------------------------------------------------------------------------------

	private static LanguageIdentification BuildLanguageIdentification()
	{
		LanguageIdentification identification = new()
		{
			FileExtensions = new[] { _SCRIPT_EXTENSION, _MODULE_EXTENSION, _COMMONJS_EXTENSION, _JSX_EXTENSION }.ToFrozenSet(StringComparer.OrdinalIgnoreCase),
			DeclaredLanguageNames = new[] { _LANGUAGE_NAME }.ToFrozenSet(StringComparer.Ordinal),
			FirstLineDetectionExpression = JavaScriptPatternExpressions.ShebangFirstLine(),
			LanguageNameDerivation = static (_, _) => _LANGUAGE_NAME,
			DeclaredUnsupportedCoverage = JavaScriptDeclaredCoverage.Build()
		};

		return identification;
	}

	// ------------------------------------------------------------------------------------------
	// Element 5: candidate selection, templates, flow signals and the dialects of a file
	// ------------------------------------------------------------------------------------------

	private static CandidateSelectionAndRecommendation BuildCandidateSelectionAndRecommendation()
	{
		CandidateSelectionAndRecommendation element = new()
		{
			PreselectionAnchors = JavaScriptRuleDeclarations.BuildPreselectionAnchors(),
			TemplateProvider = JavaScriptTemplateDeclarations.Provide,
			FlowSignals = JavaScriptFlowSignalDeclarations.Build(),
			LanguageFallbackTextByFlow = JavaScriptTemplateDeclarations.FallbackTextByFlow,
			FileDialects = BuildFileDialects()
		};

		return element;
	}

	/// <summary>
	/// The dialect a file is read in, resolved once per file: from the interpreter its first line names, then from its extension, then the default. A shebang that names
	/// <c>node</c> or <c>nodejs</c> and the <c>.cjs</c> extension resolve to <c>node</c>; every other file is <c>script</c>. A <c>.jsx</c> file and a <c>.mjs</c> file get no
	/// dialect of their own.
	/// </summary>
	private static FileDialects BuildFileDialects()
	{
		return new FileDialects
		{
			DefaultDialect = JavaScriptDialectNames.SCRIPT,
			ShebangInterpreters = new Dictionary<string, string>(StringComparer.Ordinal)
			{
				[_NODE_INTERPRETER] = JavaScriptDialectNames.NODE,
				[_NODEJS_INTERPRETER] = JavaScriptDialectNames.NODE
			}.ToFrozenDictionary(StringComparer.Ordinal),
			ExtensionDialects = new Dictionary<string, string>(StringComparer.Ordinal)
			{
				[_COMMONJS_EXTENSION] = JavaScriptDialectNames.NODE
			}.ToFrozenDictionary(StringComparer.Ordinal)
		};
	}
}
