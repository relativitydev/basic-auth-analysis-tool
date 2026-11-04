using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Text.RegularExpressions;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Findings;

namespace MigrateAuth.Cli.Tests.Fixtures;

/// <summary>
/// The expressions of the here-document exemplar, generated at build time. The two opening expressions
/// carry the shape the catalog of a shell requires of a here-document opening: anchored with
/// <c>\G</c>, refusing the here-string's three angle brackets, and capturing the terminator in its
/// unquoted, single-quoted, double-quoted and backslash-led forms, the empty quoted forms included.
/// The two credential expressions exclude every expansion from the credential group, because the
/// bare-word reading of the literal-credential signal trusts the expression to do that.
/// </summary>
internal static partial class HereDocumentExemplarPatternExpressions
{
	[GeneratedRegex(
		@"\G<<(?!<)(?:'(?<terminator>[^'\n]*)'(?<quoted>)|""(?<terminator>[^""\n]*)""(?<quoted>)|\\(?<terminator>\w+)(?<quoted>)|(?<terminator>\w+))",
		RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture,
		matchTimeoutMilliseconds: LexicalEnginePolicy.PROVISIONAL_LEXICAL_BUDGET_IN_MILLISECONDS)]
	public static partial Regex PlainOpening();

	[GeneratedRegex(
		@"\G<<-(?:'(?<terminator>[^'\n]*)'(?<quoted>)|""(?<terminator>[^""\n]*)""(?<quoted>)|\\(?<terminator>\w+)(?<quoted>)|(?<terminator>\w+))",
		RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture,
		matchTimeoutMilliseconds: LexicalEnginePolicy.PROVISIONAL_LEXICAL_BUDGET_IN_MILLISECONDS)]
	public static partial Regex TabStrippingOpening();

	/// <summary>The bare two-character delimiter with an empty captured terminator: the shortest opening a deferred rule can match.</summary>
	[GeneratedRegex(
		@"\G<<(?<terminator>)",
		RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture,
		matchTimeoutMilliseconds: LexicalEnginePolicy.PROVISIONAL_LEXICAL_BUDGET_IN_MILLISECONDS)]
	public static partial Regex BareDelimiterOpening();

	[GeneratedRegex(
		@"^\s*const\s+(?<name>[A-Za-z_][A-Za-z0-9_]*)\s*=",
		RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture | RegexOptions.Multiline,
		matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex ConstantDeclaration();

	/// <summary>The credential group is a single-quoted run with no expansion in it, or a bare word of the safe characters ending at a blank or the end of the line.</summary>
	[GeneratedRegex(
		@"\b(?<construct>setpass)[ \t]+(?<credential>'[^'\n$`\\]*'|[A-Za-z0-9_.:@-]+)(?=[ \t]|$)",
		RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture | RegexOptions.Multiline,
		matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex BareWordCredential();

	/// <summary>The same shape under another keyword, for the rule that does not declare the bare-word reading.</summary>
	[GeneratedRegex(
		@"\b(?<construct>setplain)[ \t]+(?<credential>'[^'\n$`\\]*'|[A-Za-z0-9_.:@-]+)(?=[ \t]|$)",
		RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture | RegexOptions.Multiline,
		matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex PlainCredential();
}

/// <summary>
/// A TEST-LOCAL ANALYZER, BUILT ONLY BY THE TESTS THAT NEED IT. It is shaped like a shell: a
/// hash comment, single and double quotes, and the two here-document forms as captured-terminator
/// quote rules with a deferred body start and a whole-line closing. It carries no shell expression
/// the product ships; nothing under <c>src/</c> names it.
///
/// Two detection rules let the bare-word reading of the literal-credential signal be tested: one
/// declares it, one declares the credential position in the ordinary way.
/// </summary>
public sealed class HereDocumentExemplarAnalyzer : ILanguageAnalyzer
{
	/// <summary>The rule that declares the bare-word reading.</summary>
	public const string BARE_WORD_PATTERN_CLASS = "HDX-001";

	/// <summary>The rule that declares the credential position the ordinary way.</summary>
	public const string ORDINARY_PATTERN_CLASS = "HDX-002";

	private const string _PROFILE_NAME = "HereDocumentExemplar";
	private const string _LANGUAGE_NAME = "HereDocumentExemplar";
	private const string _FILE_EXTENSION = ".hdx";
	private const string _BARE_WORD_DISPLAY_NAME = "Credential written as a bare word";
	private const string _ORDINARY_DISPLAY_NAME = "Credential written in the ordinary way";
	private const string _CONSTRUCT_CAPTURE_GROUP = "construct";
	private const string _CREDENTIAL_CAPTURE_GROUP = "credential";
	private const string _TERMINATOR_CAPTURE_GROUP = "terminator";
	private const string _QUOTED_TERMINATOR_CAPTURE_GROUP = "quoted";
	private const string _BARE_WORD_ANCHOR_TEXT = "setpass";
	private const string _ORDINARY_ANCHOR_TEXT = "setplain";
	private const string _LINE_COMMENT_OPENING = "#";
	private const string _SINGLE_QUOTE = "'";
	private const string _DOUBLE_QUOTE = "\"";
	private const string _PLAIN_HERE_DOCUMENT_OPENING = "<<";
	private const string _TAB_STRIPPING_HERE_DOCUMENT_OPENING = "<<-";
	private const string _CLOSING_IS_CAPTURED_AT_THE_OPENING = "";
	private const string _SUBSTITUTION_OPENING = "$(";
	private const string _SUBSTITUTION_CLOSING = ")";
	private const string _NESTING_OPENING = "(";
	private const string _ARITHMETIC_OPENING = "((";
	private const string _ARITHMETIC_CLOSING = "))";
	private const string _LET_KEYWORD = "let";
	private const char _ESCAPE_CHARACTER = '\\';
	private const char _HERE_STRING_ANGLE = '<';
	private const char _LINE_FEED = '\n';
	private const char _SPACE = ' ';
	private const char _TAB = '\t';

	private static readonly FrozenDictionary<string, RecommendationTemplate> _templatesByPatternClass = BuildTemplates();

	public LexicalProfile LexicalProfile { get; } = BuildLexicalProfile();

	public LanguageIdentification LanguageIdentification { get; } = BuildLanguageIdentification();

	public FrozenDictionary<string, DetectionRule> DetectionRulesByPatternClass { get; } = BuildDetectionRules();

	public LanguageShapedDemotionSignals DemotionSignals { get; } = LanguageShapedDemotionSignals.NoneDeclared;

	public CandidateSelectionAndRecommendation CandidateSelectionAndRecommendation { get; } =
		BuildCandidateSelectionAndRecommendation();

	public FrozenSet<SubsumptionRule> SubsumptionRelation { get; } = FrozenSet<SubsumptionRule>.Empty;

	/// <summary>
	/// A here-document rule of this shape, parameterized by what a test varies. The tab-stripping and the
	/// plain form differ in the opening and in the closing anchoring and in nothing else.
	/// </summary>
	internal static QuoteRule BuildHereDocumentRule(
		string opening,
		Regex openingExpression,
		PositionalAnchoring closingAnchoring,
		HereDocumentBodyStart bodyStart)
	{
		QuoteRule rule = new()
		{
			Opening = opening,
			Closing = _CLOSING_IS_CAPTURED_AT_THE_OPENING,
			NewlinePolicy = NewlinePolicy.Allowed,
			EscapeCharacter = _ESCAPE_CHARACTER,
			ClosingAnchoring = closingAnchoring,
			InterpolationOpening = _SUBSTITUTION_OPENING,
			InterpolationClosing = _SUBSTITUTION_CLOSING,
			InterpolationNestingOpening = _NESTING_OPENING,
			OpeningContextPredicate = OpeningStandsOutsideArithmeticAndHereStrings,
			CapturedTerminator = new CapturedTerminator
			{
				OpeningExpression = openingExpression,
				TerminatorCaptureGroupName = _TERMINATOR_CAPTURE_GROUP,
				NonInterpolatingQuoteCaptureGroupName = _QUOTED_TERMINATOR_CAPTURE_GROUP,
				BodyStart = bodyStart
			}
		};

		return rule;
	}

	/// <summary>The profile with the two here-document rules and the two ordinary quote rules, in the order the engine will reorder anyway.</summary>
	internal static LexicalProfile BuildProfileWith(IEnumerable<QuoteRule> hereDocumentRules, char? codeEscapeCharacter = null)
	{
		LexicalProfile profile = new()
		{
			ProfileName = _PROFILE_NAME,
			ConstantDeclarationExpression = HereDocumentExemplarPatternExpressions.ConstantDeclaration(),
			CodeEscapeCharacter = codeEscapeCharacter,
			LineCommentRules = [new LineCommentRule { Opening = _LINE_COMMENT_OPENING }],
			BlockCommentRules = [],
			QuoteRules = [.. BuildOrdinaryQuoteRules(), .. hereDocumentRules]
		};

		return profile;
	}

	private static LexicalProfile BuildLexicalProfile()
	{
		return BuildProfileWith(
		[
			BuildHereDocumentRule(
				_TAB_STRIPPING_HERE_DOCUMENT_OPENING,
				HereDocumentExemplarPatternExpressions.TabStrippingOpening(),
				PositionalAnchoring.WholeLineAfterLeadingTabs,
				HereDocumentBodyStart.AfterTheOpeningLine),
			BuildHereDocumentRule(
				_PLAIN_HERE_DOCUMENT_OPENING,
				HereDocumentExemplarPatternExpressions.PlainOpening(),
				PositionalAnchoring.WholeLine,
				HereDocumentBodyStart.AfterTheOpeningLine)
		]);
	}

	private static ImmutableArray<QuoteRule> BuildOrdinaryQuoteRules()
	{
		ImmutableArray<QuoteRule> rules =
		[
			new QuoteRule
			{
				Opening = _SINGLE_QUOTE,
				Closing = _SINGLE_QUOTE,
				NewlinePolicy = NewlinePolicy.Allowed
			},
			new QuoteRule
			{
				Opening = _DOUBLE_QUOTE,
				Closing = _DOUBLE_QUOTE,
				NewlinePolicy = NewlinePolicy.Allowed,
				EscapeCharacter = _ESCAPE_CHARACTER,
				InterpolationOpening = _SUBSTITUTION_OPENING,
				InterpolationClosing = _SUBSTITUTION_CLOSING,
				InterpolationNestingOpening = _NESTING_OPENING
			}
		];

		return rules;
	}

	/// <summary>
	/// Refuses the opening that follows another angle bracket (the here-string's last two), and any
	/// opening on a line that is in arithmetic: an unclosed double parenthesis earlier on the line, or a
	/// line whose first word is the arithmetic keyword. Bounded to the line.
	/// </summary>
	private static bool OpeningStandsOutsideArithmeticAndHereStrings(ReadOnlySpan<char> text, int delimiterPosition)
	{
		bool followsAnAngle = delimiterPosition > 0 && text[delimiterPosition - 1] == _HERE_STRING_ANGLE;
		int lineStart = text[..delimiterPosition].LastIndexOf(_LINE_FEED) + 1;
		ReadOnlySpan<char> linePrefix = text[lineStart..delimiterPosition];

		return !followsAnAngle && !StartsWithTheArithmeticKeyword(linePrefix) && !HasAnUnclosedArithmeticOpening(linePrefix);
	}

	private static bool StartsWithTheArithmeticKeyword(ReadOnlySpan<char> linePrefix)
	{
		ReadOnlySpan<char> trimmed = linePrefix.TrimStart([_SPACE, _TAB]);
		bool startsWithIt =
			trimmed.StartsWith(_LET_KEYWORD, StringComparison.Ordinal)
			&& (trimmed.Length == _LET_KEYWORD.Length || trimmed[_LET_KEYWORD.Length] is _SPACE or _TAB);

		return startsWithIt;
	}

	private static bool HasAnUnclosedArithmeticOpening(ReadOnlySpan<char> linePrefix)
	{
		int lastOpening = linePrefix.LastIndexOf(_ARITHMETIC_OPENING, StringComparison.Ordinal);

		return lastOpening >= 0
			&& linePrefix[(lastOpening + _ARITHMETIC_OPENING.Length)..].IndexOf(_ARITHMETIC_CLOSING, StringComparison.Ordinal) < 0;
	}

	private static LanguageIdentification BuildLanguageIdentification()
	{
		LanguageIdentification identification = new()
		{
			FileExtensions = new[] { _FILE_EXTENSION }.ToFrozenSet(StringComparer.OrdinalIgnoreCase),
			DeclaredLanguageNames = new[] { _LANGUAGE_NAME }.ToFrozenSet(StringComparer.Ordinal),
			LanguageNameDerivation = (_, _) => _LANGUAGE_NAME,
			DeclaredUnsupportedCoverage = FrozenSet<DeclaredUnsupportedCoverage>.Empty
		};

		return identification;
	}

	private static FrozenDictionary<string, DetectionRule> BuildDetectionRules()
	{
		Dictionary<string, DetectionRule> rules = new(StringComparer.Ordinal)
		{
			[BARE_WORD_PATTERN_CLASS] = BuildCredentialRule(
				HereDocumentExemplarPatternExpressions.BareWordCredential(),
				CredentialPosition.AtBareWordOrLiteralCaptureGroup(_CREDENTIAL_CAPTURE_GROUP),
				_BARE_WORD_DISPLAY_NAME),
			[ORDINARY_PATTERN_CLASS] = BuildCredentialRule(
				HereDocumentExemplarPatternExpressions.PlainCredential(),
				CredentialPosition.AtCaptureGroup(_CREDENTIAL_CAPTURE_GROUP),
				_ORDINARY_DISPLAY_NAME)
		};

		return rules.ToFrozenDictionary(StringComparer.Ordinal);
	}

	private static DetectionRule BuildCredentialRule(Regex expression, CredentialPosition credentialPosition, string displayName)
	{
		DetectionRule rule = new()
		{
			Expression = expression,
			AnchorPosition = MatchAnchor.AtCaptureGroup(_CONSTRUCT_CAPTURE_GROUP),
			BaseLevel = ConfidenceLevel.Medium,
			SelfSufficiency = SelfSufficiency.DeclaresSchemeAndCredentialCarrier,
			SymbolicVerdictInterpretation = SymbolicVerdictInterpretation.NotSubjectToSymbolicResolution,
			CredentialPosition = credentialPosition,
			DestinationCertainty = DestinationCertainty.Unknown,
			DisplayName = displayName
		};

		return rule;
	}

	private static CandidateSelectionAndRecommendation BuildCandidateSelectionAndRecommendation()
	{
		CandidateSelectionAndRecommendation element = new()
		{
			PreselectionAnchors = new[]
			{
				new PreselectionAnchor { Text = _BARE_WORD_ANCHOR_TEXT },
				new PreselectionAnchor { Text = _ORDINARY_ANCHOR_TEXT }
			}.ToFrozenSet(),
			TemplateProvider = ProvideTemplate,
			FlowSignals = []
		};

		return element;
	}

	private static RecommendationTemplate? ProvideTemplate(string patternClassIdentifier, CredentialFlow flow)
	{
		RecommendationTemplate? template = null;

		if (flow is CredentialFlow.ClientCredentials)
		{
			_templatesByPatternClass.TryGetValue(patternClassIdentifier, out template);
		}

		return template;
	}

	private static FrozenDictionary<string, RecommendationTemplate> BuildTemplates()
	{
		RecommendationTemplate template = new()
		{
			GenericSourceForm = "a credential written as a word on a command line",
			TargetFormUnderTokenAuthentication = "a bearer token obtained from the identity endpoint",
			CredentialProvenance = "the process environment or a secret store, never source",
			VerificationNote = "confirm the command still authenticates after the change"
		};

		Dictionary<string, RecommendationTemplate> templates = new(StringComparer.Ordinal)
		{
			[BARE_WORD_PATTERN_CLASS] = template,
			[ORDINARY_PATTERN_CLASS] = template
		};

		return templates.ToFrozenDictionary(StringComparer.Ordinal);
	}
}
