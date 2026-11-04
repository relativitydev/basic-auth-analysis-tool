using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Text.RegularExpressions;

using MigrateAuth.Cli.Findings;

namespace MigrateAuth.Cli.Catalog;

/// <summary>What an analyzer got wrong, as a code rather than as prose.</summary>
public enum AnalyzerContractViolationCode
{
	ProfileNameMissing = 0,
	QuoteDelimiterEmpty = 1,
	CommentDelimiterEmpty = 2,
	FileExtensionSetEmpty = 3,
	FileExtensionMalformed = 4,
	DeclaredLanguageNameSetEmpty = 5,
	DerivedLanguageNameNotDeclared = 6,
	DetectionRuleSetEmpty = 7,
	AnchorCaptureGroupAbsentFromExpression = 8,
	CredentialCaptureGroupAbsentFromExpression = 9,
	SchemeCaptureGroupAbsentFromExpression = 10,
	ExpectedSchemeTokenMissing = 11,
	SymbolicResponseDeclaredWithoutSchemeArgument = 12,
	CapturedTerminatorGroupAbsentFromExpression = 13,
	SubsumptionReferencesUnknownPatternClass = 14,
	SubsumptionIsReflexive = 15,
	PreselectionAnchorSetEmpty = 16,
	PreselectionAnchorEmpty = 17,
	RecommendationTemplateIncomplete = 18,

	/// <summary>
	/// The two hole kinds are exhaustive and exclusive. A rule declaring an interpolation
	/// opening has to say WHICH kind it opens - a closing token, or an identifier extent - and a
	/// rule declaring neither would scan as though it did not interpolate at all, which is a
	/// silent loss of every construct written inside a hole rather than a visible failure.
	/// </summary>
	InterpolationHoleKindNotDeclared = 19,

	/// <summary>A rule's <see cref="DetectionRule.DisplayName"/> is null, empty or whitespace.</summary>
	DisplayNameMissing = 20,

	/// <summary>
	/// Two pattern classes of the same analyzer declare the same
	/// <see cref="DetectionRule.DisplayName"/>, by ordinal comparison. The report renders this
	/// name in place of the identifier; two classes sharing one name would make that rendering
	/// ambiguous exactly where the identifier still needs to disambiguate them.
	/// </summary>
	DisplayNameNotUnique = 21,

	/// <summary>A <see cref="DeclaredUnsupportedCoverage.Construct"/> is null, empty or whitespace.</summary>
	DeclaredUnsupportedConstructMissing = 22,

	/// <summary>A <see cref="DeclaredUnsupportedCoverage.Reason"/> is null, empty or whitespace.</summary>
	DeclaredUnsupportedReasonMissing = 23,

	/// <summary>
	/// Two entries of <see cref="LanguageIdentification.DeclaredUnsupportedCoverage"/>
	/// name the same <see cref="DeclaredUnsupportedCoverage.Construct"/>, by ordinal comparison, with
	/// a different <see cref="DeclaredUnsupportedCoverage.Reason"/>. The set's own value equality
	/// already dedups an exact repeat; this is the case it cannot catch on its own.
	/// </summary>
	DeclaredUnsupportedConstructDuplicated = 24,

	/// <summary>
	/// A <see cref="ContentMarker.Expression"/> declares a
	/// <see cref="System.Text.RegularExpressions.Regex.MatchTimeout"/> other than
	/// <see cref="RecognitionEnginePolicy.MATCH_BOUND"/>. The engine evaluates content markers inside the
	/// engine's existing match-bound try, exactly as it evaluates every detection rule, so a marker
	/// carrying its own timeout - including the CLR default of
	/// <see cref="System.Text.RegularExpressions.Regex.InfiniteMatchTimeout"/> when none is declared -
	/// would let one expression outlast the file-level bound and defeat the guarantee the try
	/// exists to give every other expression evaluated alongside it.
	/// </summary>
	ContentMarkerMatchTimeoutNotPolicyBound = 25,

	/// <summary>
	/// A value of <see cref="CandidateSelectionAndRecommendation.LanguageFallbackTextByFlow"/> is
	/// declared - not null - but empty or whitespace-only (the per-flow fallback slot). The slot is optional;
	/// declaring it and giving a flow nothing to say is not a state this contract allows, the same
	/// rule already applied to a declared preselection anchor.
	/// </summary>
	LanguageFallbackTextBlank = 26,

	/// <summary>
	/// A <see cref="LexicalProfile.ConstantDeclarationExpression"/>
	/// declares a <see cref="System.Text.RegularExpressions.Regex.MatchTimeout"/> other than
	/// <see cref="RecognitionEnginePolicy.MATCH_BOUND"/>. <see cref="Recognition.FileSymbolTable"/>'s
	/// <c>TryCollect</c> evaluates it inside the engine's own match-bound bracket
	/// (<see cref="Recognition.TimedRegexEvaluation"/>), on the same terms
	/// <see cref="ContentMarkerMatchTimeoutNotPolicyBound"/> states for a content marker: a
	/// timeout other than the policy bound would let this one expression outlast the file's own
	/// budget every other analyzer-declared expression is held to.
	/// </summary>
	ConstantDeclarationExpressionMatchTimeoutNotPolicyBound = 27,

	/// <summary>
	/// A member of
	/// <see cref="LanguageShapedDemotionSignals.GenerationMarkerExpressions"/> declares a match
	/// timeout other than <see cref="RecognitionEnginePolicy.MATCH_BOUND"/>, on the same terms as
	/// <see cref="ConstantDeclarationExpressionMatchTimeoutNotPolicyBound"/> -
	/// <see cref="Recognition.FileDemotionSignals"/> evaluates every generation marker inside the
	/// same bracket.
	/// </summary>
	GenerationMarkerExpressionMatchTimeoutNotPolicyBound = 28,

	/// <summary>
	/// A <see cref="DetectionRule.Expression"/> declares a match
	/// timeout other than <see cref="RecognitionEnginePolicy.MATCH_BOUND"/>. A detection rule is as
	/// capable of outlasting the bound as a content marker is.
	/// </summary>
	DetectionRuleMatchTimeoutNotPolicyBound = 29,

	/// <summary>
	/// A <see cref="FlowSignal.Kind"/> is <see cref="FlowSignalKind.Undeclared"/>: the signal's
	/// author never said which flow it is evidence of, and the engine would have nothing to assign.
	/// </summary>
	FlowSignalKindUndeclared = 30,

	/// <summary>
	/// A <see cref="FlowSignal.Zone"/> is <see cref="FlowSignalZone.Undeclared"/>: the signal
	/// declares no zone it may fire in, and firing everywhere is not a default this contract offers.
	/// </summary>
	FlowSignalZoneUndeclared = 31,

	/// <summary>A <see cref="FlowSignal.Expression"/> is empty or whitespace-only and would match at every position.</summary>
	FlowSignalExpressionBlank = 32,

	/// <summary>
	/// A <see cref="FlowSignal.Expression"/> declares a match timeout other than
	/// <see cref="RecognitionEnginePolicy.MATCH_BOUND"/>, on the same terms as
	/// <see cref="DetectionRuleMatchTimeoutNotPolicyBound"/>: flow signals are evaluated inside the
	/// file's own match bound and one expression carrying another timeout would outlast it.
	/// </summary>
	FlowSignalMatchTimeoutNotPolicyBound = 33,

	/// <summary>Two flow signals of the same analyzer declare the same <see cref="FlowSignal.Name"/>, by ordinal comparison.</summary>
	FlowSignalNameDuplicated = 34,

	/// <summary>
	/// A <see cref="CapturedTerminator.OpeningExpression"/> declares a match timeout other than
	/// <see cref="LexicalEnginePolicy.LEXICAL_BUDGET"/>. The lexical scan charges the expression to the
	/// file's lexical budget, but a cooperative budget cannot interrupt one stuck <c>Match</c> call: the
	/// expression's own timeout is what does, so it has to be the budget.
	/// </summary>
	HereDocumentOpeningMatchTimeoutNotLexicalBudget = 35,

	/// <summary>
	/// A <see cref="QuoteRule.InterpolationIdentifierExtent"/> declares a match timeout other than
	/// <see cref="LexicalEnginePolicy.LEXICAL_BUDGET"/>, on the same terms as
	/// <see cref="HereDocumentOpeningMatchTimeoutNotLexicalBudget"/>.
	/// </summary>
	InterpolationIdentifierExtentMatchTimeoutNotLexicalBudget = 36,

	/// <summary>
	/// A <see cref="LanguageIdentification.FirstLineDetectionExpression"/>, where one is
	/// declared, declares a match timeout other than <see cref="LexicalEnginePolicy.LEXICAL_BUDGET"/>,
	/// on the same terms as <see cref="HereDocumentOpeningMatchTimeoutNotLexicalBudget"/>. Language
	/// attribution charges it to the file's lexical budget.
	/// </summary>
	FirstLineDetectionMatchTimeoutNotLexicalBudget = 37,

	/// <summary>
	/// A <see cref="QuoteRule.InterpolationNestingOpening"/> is declared without
	/// an <see cref="QuoteRule.InterpolationClosing"/> to nest against, or is empty. Nesting is a
	/// property of a bracketed hole only: an identifier-extent hole has no closing token to count
	/// against, and an empty opener would match at every position.
	/// </summary>
	InterpolationNestingOpeningWithoutClosing = 38,

	/// <summary>
	/// <see cref="QuoteRule.DoubledInterpolationOpeningIsLiteral"/> is set without both an
	/// <see cref="QuoteRule.InterpolationOpening"/> and an <see cref="QuoteRule.InterpolationClosing"/>.
	/// A doubled opener can only be read as literal where a bracketed hole exists to be opened.
	/// </summary>
	DoubledInterpolationOpeningWithoutBracketedHole = 39,

	/// <summary>
	/// <see cref="PositionalAnchoring.WholeLine"/> or <see cref="PositionalAnchoring.WholeLineAfterLeadingTabs"/>
	/// is the OPENING anchoring of a quote, line-comment or block-comment rule. Both values describe
	/// a terminator standing alone on its line and mean nothing for an opening.
	/// </summary>
	WholeLineAnchoringOnAnOpening = 40,

	/// <summary>
	/// A <see cref="CapturedTerminator"/> declares <see cref="HereDocumentBodyStart.AfterTheOpeningLine"/>
	/// with a closing anchoring other than the two whole-line values. A body that starts on the next
	/// line closes only on a whole line, or a terminator word inside a body line would end it.
	/// </summary>
	DeferredBodyStartWithoutWholeLineClosing = 41,

	/// <summary>
	/// A <see cref="CapturedTerminator"/> declares <see cref="HereDocumentBodyStart.AfterTheOpeningLine"/>
	/// on a rule whose <see cref="QuoteRule.NewlinePolicy"/> is not <see cref="NewlinePolicy.Allowed"/>.
	/// A body that starts on the next line is made of lines.
	/// </summary>
	DeferredBodyStartWithoutAllowedNewlines = 42,

	/// <summary>
	/// A <see cref="CapturedTerminator"/> declares <see cref="HereDocumentBodyStart.AfterTheOpeningLine"/>
	/// on a rule whose opening anchoring is not <see cref="PositionalAnchoring.Anywhere"/>. The rest of
	/// the opening line is code by definition, so an anchoring that reads it contradicts the deferral.
	/// </summary>
	DeferredBodyStartWithOpeningAnchoring = 43,

	/// <summary>
	/// A <see cref="QuoteRule.InterpolationArmBlock"/> is declared without an
	/// <see cref="QuoteRule.InterpolationNestingOpening"/> to measure against, with an empty keyword, or with no
	/// command-position predecessor. An arm terminator is a closing token met at the depth the block opened at.
	/// </summary>
	ArmBlockWithoutNestingOpening = 44,

	/// <summary>
	/// A <see cref="LexicalProfile.ArithmeticContext"/> whose opening is not a run of at least two of one bracket
	/// character, or whose closing is not a run, of the same length, of a different single character.
	/// </summary>
	ArithmeticContextMalformed = 45,

	/// <summary>
	/// A <see cref="LexicalProfile.CodeEscapeCharacter"/> that is a whitespace character. A blank or a line feed as
	/// the escape would make every blank or every line of the code an escape.
	/// </summary>
	CodeEscapeCharacterIsWhitespace = 46,

	/// <summary>
	/// A profile whose quote rules declare two different <see cref="QuoteRule.InterpolationArmBlock"/> rules. The
	/// arithmetic read-ahead reads one block for the whole profile, so the holes must agree on it.
	/// </summary>
	DifferentArmBlocksDeclared = 47,

	/// <summary>A <see cref="FileDialects.DefaultDialect"/> that is null, empty or whitespace.</summary>
	FileDialectsDefaultDialectBlank = 48,

	/// <summary>A key of <see cref="FileDialects.ShebangInterpreters"/> or <see cref="FileDialects.ExtensionDialects"/> that is null, empty or whitespace.</summary>
	FileDialectsMapKeyEmpty = 49,

	/// <summary>A value of <see cref="FileDialects.ShebangInterpreters"/> or <see cref="FileDialects.ExtensionDialects"/> that is null, empty or whitespace.</summary>
	FileDialectsMapValueBlank = 50,

	/// <summary>
	/// Two keys of <see cref="FileDialects.ExtensionDialects"/> that differ only by case. The engine lower-cases a
	/// file extension before it looks it up, so the two declarations contradict each other.
	/// </summary>
	FileDialectsExtensionKeysConflict = 51,

	/// <summary>
	/// A key of <see cref="FileDialects.ExtensionDialects"/> that is not a lower-case extension with its leading dot.
	/// The engine looks up a lower-cased extension, so such a key could never match.
	/// </summary>
	FileDialectsExtensionKeyMalformed = 52,

	/// <summary>
	/// A <see cref="FlowSignal.Dialects"/> or <see cref="FlowSignal.ExcludedDialects"/> entry that is not a dialect the
	/// analyzer declares (the declared dialects are the default and every value of the two maps; a language that
	/// declares no <see cref="FileDialects"/> declares none).
	/// </summary>
	FlowSignalDialectNotDeclared = 53,

	/// <summary>A dialect that a <see cref="FlowSignal"/> both includes and excludes: it could never fire.</summary>
	FlowSignalDialectIncludedAndExcluded = 54,

	/// <summary>
	/// A <see cref="FlowSignalVariableUse.VariableGroupName"/> that the signal expression does not declare, where the
	/// signal names no <see cref="FlowSignalVariableUse.DefaultVariableName"/> to fall back on, or a declared default name
	/// that is empty or whitespace.
	/// </summary>
	FlowSignalVariableUseGroupAbsentFromExpression = 55,

	/// <summary>A <see cref="FlowSignalVariableUse.UsePattern"/> that is empty or whitespace-only and would match every line.</summary>
	FlowSignalVariableUsePatternBlank = 56,

	/// <summary>A <see cref="FlowSignalVariableUse.UsePattern"/> whose match timeout is not <see cref="RecognitionEnginePolicy.MATCH_BOUND"/>.</summary>
	FlowSignalVariableUseMatchTimeoutNotPolicyBound = 57,

	/// <summary>A window, logical-line length or alias depth of a <see cref="FlowSignalVariableUse"/> that is not within the bounds of the engine.</summary>
	FlowSignalVariableUseBoundInvalid = 58,

	/// <summary>A <see cref="RuleVariableUse.VariableGroupName"/> that the rule expression does not declare.</summary>
	RuleVariableUseGroupAbsentFromExpression = 59,

	/// <summary>An expression of <see cref="WrapperTracking"/> whose match timeout is not <see cref="RecognitionEnginePolicy.MATCH_BOUND"/>.</summary>
	WrapperTrackingMatchTimeoutNotPolicyBound = 60,

	/// <summary>A capture group a <see cref="WrapperTracking"/> expression must declare (its subject names the expression and the group) that it does not.</summary>
	WrapperTrackingGroupAbsentFromExpression = 61,

	/// <summary>A <see cref="WrapperTracking.MaxBodyLines"/> or <see cref="WrapperTracking.MaxCalls"/> that is not within the bounds of the engine.</summary>
	WrapperTrackingBoundInvalid = 62,

	/// <summary>A <see cref="FlowSignalVariableUse.StatementSeparators"/> entry that is null, empty or whitespace: it would end a segment everywhere.</summary>
	FlowSignalVariableUseStatementSeparatorBlank = 63,

	/// <summary>
	/// A <see cref="ConstructPrecedence"/> that lists a pattern class the analyzer does not declare, or lists one class
	/// twice. A precedence over a class that cannot match decides nothing, and a repeated class has two ranks.
	/// </summary>
	ConstructPrecedenceListsAnUnknownOrRepeatedPatternClass = 64,

	/// <summary>
	/// A statement separator or a nesting token that is empty or contains a line feed (a line feed always ends a
	/// statement and is not declared), or a <see cref="StatementNesting"/> whose opening and closing are the same
	/// text, which cannot be counted without reading the language.
	/// </summary>
	ConstructPrecedenceTokenMalformed = 65,

	/// <summary>
	/// A profile declares an <see cref="ExecutedStringContext"/> that reads nothing: no readable body, no command
	/// position predecessor, no word that runs a string or a script, or a string option that is not a hyphen and
	/// one letter. The scanner could never recognize a context from it.
	/// </summary>
	ExecutedStringContextMalformed = 66,

	/// <summary>
	/// An <see cref="ExecutedBodyQuote"/> or an <see cref="InlineExecutedContext"/> names a quote opening that no quote
	/// rule of the profile declares, or two executed bodies name the same quote opening.
	/// </summary>
	ExecutedBodyQuoteNamesNoQuoteRule = 67,

	/// <summary>
	/// A word of an <see cref="ExecutedStringContext"/> is empty or holds whitespace or a quote character, or a script file
	/// extension does not begin with a dot, or an option that takes an argument does not begin with a hyphen or a plus.
	/// The reader splits a command on blanks and compares whole words, so such a word could never match.
	/// </summary>
	ExecutedStringWordMalformed = 68,

	/// <summary>
	/// An <see cref="InlineExecutedContext"/> with an empty opening or closing, or whose word operators include an
	/// empty one. The scanner could not find where its content starts or ends.
	/// </summary>
	InlineExecutedContextMalformed = 69,

	/// <summary>
	/// An <see cref="ExecutedStringContext"/> is declared on a profile that declares no <see cref="LexicalProfile.CodeEscapeCharacter"/>.
	/// The reader treats an escaped predecessor and an escaped line feed as the shell does, which needs the escape.
	/// </summary>
	ExecutedStringContextWithoutCodeEscape = 70,

	/// <summary>
	/// The grammar an <see cref="ExecutedStringContext"/> declares is incomplete or inconsistent: an empty blank,
	/// option-introducer, quote, redirection or list-operator set, an unset character, a blank that is not whitespace, a
	/// blank that is also a quote, a modifier set that does not hold the descriptor duplication, or a list operator pair
	/// that is not two characters.
	/// </summary>
	ExecutedStringGrammarMalformed = 71,

	/// <summary>
	/// A <see cref="FileNameSignal.Kind"/>, <see cref="FolderNameSignal.Kind"/> or <see cref="ContentMarker.Kind"/>
	/// is <see cref="DemotionKind.Undeclared"/>: the signal's kind decides the level its firing proposes and the
	/// reason the report gives, so a signal that never said what it is cannot ship.
	/// </summary>
	DemotionSignalKindUndeclared = 72
}

/// <summary>One violation, naming its code and the subject it was found on.</summary>
public sealed record AnalyzerContractViolation
{
	public required AnalyzerContractViolationCode Code { get; init; }

	/// <summary>The pattern class, extension or delimiter the violation was found on.</summary>
	public required string Subject { get; init; }
}

/// <summary>
/// The engine's consumer of the language analyzer contract. It is the component that establishes that a
/// language analyzer is usable, and it knows no language: everything it checks is read back
/// through the contract itself, including the constructed expressions, whose group names it
/// interrogates, the language name derivation, which it invokes over the declared extensions,
/// and the template provider, which it queries per declared pattern class.
/// </summary>
public static class AnalyzerContractValidator
{
	private const char _FILE_EXTENSION_LEADING_CHARACTER = '.';
	private const int _MINIMUM_FILE_EXTENSION_LENGTH = 2;
	private const int _MINIMUM_ARITHMETIC_OPENING_RUN_LENGTH = 2;
	private const int _STRING_OPTION_LENGTH = 2;
	private const int _STRING_OPTION_LETTER_INDEX = 1;
	private const int _STRING_OPTION_INTRODUCER_INDEX = 0;
	private const int _FIRST_CHARACTER_INDEX = 0;
	private const int _LIST_OPERATOR_PAIR_LENGTH = 2;
	private const string _LINE_FEED = "\n";
	private const string _SUBJECT_SEPARATOR = ":";

	/// <summary>Returns every violation found. Never throws for a merely invalid analyzer.</summary>
	public static ImmutableArray<AnalyzerContractViolation> Validate(ILanguageAnalyzer analyzer)
	{
		ArgumentNullException.ThrowIfNull(analyzer);

		ImmutableArray<AnalyzerContractViolation>.Builder violations =
			ImmutableArray.CreateBuilder<AnalyzerContractViolation>();

		ValidateLexicalProfile(analyzer.LexicalProfile, violations);
		ValidateLanguageIdentification(analyzer.LanguageIdentification, violations);
		ValidateDetectionRules(analyzer, violations);
		ValidateDemotionSignals(analyzer.DemotionSignals, violations);
		ValidateSubsumptionRelation(analyzer, violations);
		ValidateConstructPrecedence(analyzer, violations);
		ValidateCandidateSelectionAndRecommendation(analyzer, violations);

		return violations.ToImmutable();
	}

	private static void ValidateLexicalProfile(
		LexicalProfile profile,
		ImmutableArray<AnalyzerContractViolation>.Builder violations)
	{
		if (string.IsNullOrWhiteSpace(profile.ProfileName))
		{
			Record(violations, AnalyzerContractViolationCode.ProfileNameMissing, profile.ProfileName);
		}

		if (profile.ConstantDeclarationExpression.MatchTimeout != RecognitionEnginePolicy.MATCH_BOUND)
		{
			Record(
				violations,
				AnalyzerContractViolationCode.ConstantDeclarationExpressionMatchTimeoutNotPolicyBound,
				profile.ProfileName);
		}

		ValidateArithmeticContext(profile, violations);
		ValidateCodeEscape(profile, violations);
		ValidateArmBlocks(profile, violations);
		ValidateExecutedStrings(profile, violations);

		foreach (QuoteRule quoteRule in profile.QuoteRulesInEngineOrder)
		{
			ValidateQuoteRule(quoteRule, violations);
		}

		foreach (LineCommentRule lineCommentRule in profile.LineCommentRulesInEngineOrder)
		{
			if (string.IsNullOrEmpty(lineCommentRule.Opening))
			{
				Record(violations, AnalyzerContractViolationCode.CommentDelimiterEmpty, profile.ProfileName);
			}

			ValidateOpeningAnchoring(lineCommentRule.OpeningAnchoring, lineCommentRule.Opening, violations);
		}

		foreach (BlockCommentRule blockCommentRule in profile.BlockCommentRulesInEngineOrder)
		{
			if (string.IsNullOrEmpty(blockCommentRule.Opening) || string.IsNullOrEmpty(blockCommentRule.Closing))
			{
				Record(violations, AnalyzerContractViolationCode.CommentDelimiterEmpty, profile.ProfileName);
			}

			ValidateOpeningAnchoring(blockCommentRule.OpeningAnchoring, blockCommentRule.Opening, violations);
		}
	}

	/// <summary>A code escape is not a whitespace character (code 46).</summary>
	private static void ValidateCodeEscape(
		LexicalProfile profile,
		ImmutableArray<AnalyzerContractViolation>.Builder violations)
	{
		if (profile.CodeEscapeCharacter is char escape && char.IsWhiteSpace(escape))
		{
			Record(violations, AnalyzerContractViolationCode.CodeEscapeCharacterIsWhitespace, profile.ProfileName);
		}
	}

	/// <summary>Every arm block of a profile has the same content (code 47).</summary>
	private static void ValidateArmBlocks(
		LexicalProfile profile,
		ImmutableArray<AnalyzerContractViolation>.Builder violations)
	{
		ArmBlockRule[] declared = [.. profile.QuoteRulesInEngineOrder.Select(rule => rule.InterpolationArmBlock).OfType<ArmBlockRule>()];

		if (declared.Skip(1).Any(block => !HaveTheSameContent(declared[0], block)))
		{
			Record(violations, AnalyzerContractViolationCode.DifferentArmBlocksDeclared, profile.ProfileName);
		}
	}

	/// <summary>
	/// The executed-string context reads something, names only quote rules the profile declares, holds only words the
	/// reader can match, declares a complete grammar, and sits on a profile with a code escape (codes 48 to 53).
	/// </summary>
	private static void ValidateExecutedStrings(
		LexicalProfile profile,
		ImmutableArray<AnalyzerContractViolation>.Builder violations)
	{
		if (profile.ExecutedStrings is ExecutedStringContext declaration)
		{
			ValidateTheExecutedStringDeclaration(declaration, violations, profile.ProfileName);

			if (profile.CodeEscapeCharacter is null)
			{
				Record(violations, AnalyzerContractViolationCode.ExecutedStringContextWithoutCodeEscape, profile.ProfileName);
			}

			string[] openings = [.. profile.QuoteRulesInEngineOrder.Select(rule => rule.Opening)];
			IEnumerable<string> namedOpenings = declaration.ReadableBodies.Select(body => body.QuoteOpening)
				.Concat(declaration.InlineContexts.Select(inline => inline.QuoteOpening));
			bool namesOnlyDeclaredRules = namedOpenings.All(openings.Contains);
			bool namesAnOpeningTwice = declaration.ReadableBodies.GroupBy(body => body.QuoteOpening).Any(group => group.Count() > 1);

			if (!namesOnlyDeclaredRules || namesAnOpeningTwice)
			{
				Record(violations, AnalyzerContractViolationCode.ExecutedBodyQuoteNamesNoQuoteRule, profile.ProfileName);
			}
		}
	}

	private static void ValidateTheExecutedStringDeclaration(
		ExecutedStringContext declaration,
		ImmutableArray<AnalyzerContractViolation>.Builder violations,
		string subject)
	{
		if (!ReadsSomething(declaration))
		{
			Record(violations, AnalyzerContractViolationCode.ExecutedStringContextMalformed, subject);
		}

		if (!HasOnlyMatchableWords(declaration))
		{
			Record(violations, AnalyzerContractViolationCode.ExecutedStringWordMalformed, subject);
		}

		if (declaration.InlineContexts.Any(IsAMalformedInlineContext))
		{
			Record(violations, AnalyzerContractViolationCode.InlineExecutedContextMalformed, subject);
		}

		if (!HasACompleteGrammar(declaration))
		{
			Record(violations, AnalyzerContractViolationCode.ExecutedStringGrammarMalformed, subject);
		}
	}

	private static bool IsAMalformedInlineContext(InlineExecutedContext inline)
	{
		bool hasNoOpening = string.IsNullOrEmpty(inline.Opening);
		bool hasNoClosing = string.IsNullOrEmpty(inline.Closing);
		bool hasAnEmptyOperator = inline.WordOperators.Any(string.IsNullOrEmpty);

		return hasNoOpening || hasNoClosing || hasAnEmptyOperator;
	}

	private static bool ReadsSomething(ExecutedStringContext declaration)
	{
		bool namesACommand = !declaration.EvalWords.IsEmpty
			|| !declaration.ShellWords.IsEmpty
			|| !declaration.CommandStringWords.IsEmpty
			|| !declaration.RemoteShellWords.IsEmpty;
		bool hasAStringOption = declaration.StringOption is { Length: _STRING_OPTION_LENGTH } option
			&& declaration.OptionIntroducers.Contains(option[_STRING_OPTION_INTRODUCER_INDEX])
			&& char.IsAsciiLetter(option[_STRING_OPTION_LETTER_INDEX]);
		bool hasSomethingToRead = namesACommand || !declaration.InlineContexts.IsEmpty;
		bool hasPlacesToRead = !declaration.ReadableBodies.IsEmpty && !string.IsNullOrEmpty(declaration.CommandPositionPredecessors);

		return hasSomethingToRead && hasPlacesToRead && hasAStringOption;
	}

	private static bool HasOnlyMatchableWords(ExecutedStringContext declaration)
	{
		IEnumerable<string> words =
		[
			.. declaration.LicensingWords,
			.. declaration.EvalWords,
			.. declaration.ShellWords,
			.. declaration.CommandStringWords,
			.. declaration.PrivilegeWords,
			.. declaration.RemoteShellWords,
			.. declaration.OutputRedirections,
			.. declaration.ShebangLauncherWords,
			.. declaration.PipeOperators,
			declaration.EndOfOptionsWord
		];
		IEnumerable<string> optionWords =
		[
			.. declaration.ShellOptionsWithArgument,
			.. declaration.PrivilegeOptionsWithArgument,
			.. declaration.RemoteOptionsWithArgument,
			.. declaration.StdinOption is null ? [] : new[] { declaration.StdinOption }
		];
		bool wordsMatch = words.All(word => IsAMatchableWord(declaration, word));
		bool optionsMatch = optionWords.All(word => IsAMatchableWord(declaration, word) && declaration.OptionIntroducers.Contains(word[_STRING_OPTION_INTRODUCER_INDEX]));
		bool extensionsMatch = declaration.ScriptFileExtensions.All(extension =>
			IsAMatchableWord(declaration, extension)
			&& extension.Length >= _MINIMUM_FILE_EXTENSION_LENGTH
			&& extension[_FIRST_CHARACTER_INDEX] == _FILE_EXTENSION_LEADING_CHARACTER);
		bool shebangMatches = declaration.ShebangOpening is null || IsAMatchableWord(declaration, declaration.ShebangOpening);

		return wordsMatch && optionsMatch && extensionsMatch && shebangMatches;
	}

	private static bool IsAMatchableWord(ExecutedStringContext declaration, string word)
	{
		return !string.IsNullOrEmpty(word)
			&& word.All(character => !char.IsWhiteSpace(character) && !declaration.QuoteCharacters.Contains(character));
	}

	/// <summary>
	/// The declared grammar is complete: every set is non-empty, the end-of-options word begins with an option introducer, the descriptor is digits, a pipe operator is declared, every single character is set, the blanks are whitespace
	/// and none is a quote, the modifiers hold the descriptor duplication, and a list operator pair is two characters.
	/// </summary>
	private static bool HasACompleteGrammar(ExecutedStringContext declaration)
	{
		string[] sets =
		[
			declaration.BlankCharacters,
			declaration.OptionIntroducers,
			declaration.QuoteCharacters,
			declaration.RedirectionCharacters,
			declaration.RedirectionModifierCharacters,
			declaration.ListOperatorCharacters
		];
		char[] singles =
		[
			declaration.AssignmentOperator,
			declaration.NameConnectorCharacter,
			declaration.PathSeparator,
			declaration.DescriptorDuplicationCharacter,
			declaration.CommentStart
		];
		bool setsAreSet = sets.All(set => !string.IsNullOrEmpty(set));
		bool endOfOptionsIsAnOption = declaration.EndOfOptionsWord is { Length: > 0 } endOfOptions
			&& declaration.OptionIntroducers.Contains(endOfOptions[_FIRST_CHARACTER_INDEX]);
		bool descriptorIsDigits = declaration.StandardOutputDescriptor is { Length: > 0 } descriptor && descriptor.All(char.IsAsciiDigit);
		bool pipesAreDeclared = !declaration.PipeOperators.IsEmpty;
		bool singlesAreSet = singles.All(single => single != default);
		bool blanksAreWhitespace = declaration.BlankCharacters.All(char.IsWhiteSpace)
			&& !declaration.BlankCharacters.Any(declaration.QuoteCharacters.Contains);
		bool modifiersHoldTheDuplication = declaration.RedirectionModifierCharacters.Contains(declaration.DescriptorDuplicationCharacter);
		bool pairsAreTwoCharacters = declaration.ListOperatorPairs.All(pair => pair.Length == _LIST_OPERATOR_PAIR_LENGTH);

		return setsAreSet && endOfOptionsIsAnOption && descriptorIsDigits && pipesAreDeclared && singlesAreSet && blanksAreWhitespace && modifiersHoldTheDuplication && pairsAreTwoCharacters;
	}

	private static bool HaveTheSameContent(ArmBlockRule first, ArmBlockRule second)
	{
		return first.OpeningKeyword == second.OpeningKeyword
			&& first.ClosingKeyword == second.ClosingKeyword
			&& first.CommandPositionPredecessors == second.CommandPositionPredecessors
			&& first.CommandPositionKeywords.SequenceEqual(second.CommandPositionKeywords)
			&& first.OptionTakingKeywords.SequenceEqual(second.OptionTakingKeywords);
	}

	/// <summary>An arithmetic context is a run of one bracket and a run of another (code 45).</summary>
	private static void ValidateArithmeticContext(
		LexicalProfile profile,
		ImmutableArray<AnalyzerContractViolation>.Builder violations)
	{
		if (profile.ArithmeticContext is ArithmeticContextRule context
			&& !IsAWellFormedArithmeticContext(context))
		{
			Record(violations, AnalyzerContractViolationCode.ArithmeticContextMalformed, profile.ProfileName);
		}
	}

	private static bool IsAWellFormedArithmeticContext(ArithmeticContextRule context)
	{
		bool wellFormed =
			context.Opening.Length >= _MINIMUM_ARITHMETIC_OPENING_RUN_LENGTH
			&& context.Opening.All(character => character == context.Opening[0])
			&& context.Closing.Length == context.Opening.Length
			&& context.Closing.All(character => character == context.Closing[0])
			&& context.Closing[0] != context.Opening[0];

		return wellFormed;
	}

	/// <summary>The two whole-line values are closing-only: an opening declaring either is refused (code 40).</summary>
	private static void ValidateOpeningAnchoring(
		PositionalAnchoring openingAnchoring,
		string subject,
		ImmutableArray<AnalyzerContractViolation>.Builder violations)
	{
		if (IsWholeLineAnchoring(openingAnchoring))
		{
			Record(violations, AnalyzerContractViolationCode.WholeLineAnchoringOnAnOpening, subject);
		}
	}

	private static bool IsWholeLineAnchoring(PositionalAnchoring anchoring)
	{
		bool isWholeLine = anchoring is PositionalAnchoring.WholeLine or PositionalAnchoring.WholeLineAfterLeadingTabs;

		return isWholeLine;
	}

	private static void ValidateQuoteRule(
		QuoteRule quoteRule,
		ImmutableArray<AnalyzerContractViolation>.Builder violations)
	{
		// A closing may be empty exactly when it is captured at the opening: a here-document's
		// closing is not a constant of the rule but the token the author chose at the opening.
		bool closingIsDeclared =
			!string.IsNullOrEmpty(quoteRule.Closing) || quoteRule.CapturedTerminator is not null;

		if (string.IsNullOrEmpty(quoteRule.Opening) || !closingIsDeclared)
		{
			Record(violations, AnalyzerContractViolationCode.QuoteDelimiterEmpty, quoteRule.Opening);
		}

		ValidateInterpolation(quoteRule, violations);
		ValidateOpeningAnchoring(quoteRule.OpeningAnchoring, quoteRule.Opening, violations);

		if (quoteRule.InterpolationIdentifierExtent is Regex identifierExtent
			&& identifierExtent.MatchTimeout != LexicalEnginePolicy.LEXICAL_BUDGET)
		{
			Record(
				violations,
				AnalyzerContractViolationCode.InterpolationIdentifierExtentMatchTimeoutNotLexicalBudget,
				quoteRule.Opening);
		}

		if (quoteRule.CapturedTerminator is CapturedTerminator capturedTerminator)
		{
			ValidateCapturedTerminator(quoteRule, capturedTerminator, violations);
		}
	}

	/// <summary>
	/// Exactly one of the two hole kinds, where a hole is declared at all. Declaring both is
	/// as unreadable as declaring neither - the scanner would have to pick, and picking is the
	/// author's job.
	/// </summary>
	private static void ValidateInterpolation(
		QuoteRule quoteRule,
		ImmutableArray<AnalyzerContractViolation>.Builder violations)
	{
		bool declaresBracketedHole = quoteRule.InterpolationClosing is not null;
		bool declaresIdentifierExtentHole = quoteRule.InterpolationIdentifierExtent is not null;
		bool declaresExactlyOneKind = declaresBracketedHole ^ declaresIdentifierExtentHole;

		if (quoteRule.InterpolationOpening is not null && !declaresExactlyOneKind)
		{
			Record(violations, AnalyzerContractViolationCode.InterpolationHoleKindNotDeclared, quoteRule.Opening);
		}

		if (quoteRule.InterpolationOpening is null && (declaresBracketedHole || declaresIdentifierExtentHole))
		{
			Record(violations, AnalyzerContractViolationCode.InterpolationHoleKindNotDeclared, quoteRule.Opening);
		}

		if (quoteRule.InterpolationNestingOpening is not null
			&& (string.IsNullOrEmpty(quoteRule.InterpolationNestingOpening) || !declaresBracketedHole))
		{
			Record(violations, AnalyzerContractViolationCode.InterpolationNestingOpeningWithoutClosing, quoteRule.Opening);
		}

		if (quoteRule.DoubledInterpolationOpeningIsLiteral
			&& (string.IsNullOrEmpty(quoteRule.InterpolationOpening) || !declaresBracketedHole))
		{
			Record(violations, AnalyzerContractViolationCode.DoubledInterpolationOpeningWithoutBracketedHole, quoteRule.Opening);
		}

		if (quoteRule.InterpolationArmBlock is ArmBlockRule armBlock
			&& (string.IsNullOrEmpty(quoteRule.InterpolationNestingOpening)
				|| string.IsNullOrEmpty(armBlock.OpeningKeyword)
				|| string.IsNullOrEmpty(armBlock.ClosingKeyword)
				|| string.IsNullOrEmpty(armBlock.CommandPositionPredecessors)))
		{
			Record(violations, AnalyzerContractViolationCode.ArmBlockWithoutNestingOpening, quoteRule.Opening);
		}
	}

	private static void ValidateCapturedTerminator(
		QuoteRule quoteRule,
		CapturedTerminator capturedTerminator,
		ImmutableArray<AnalyzerContractViolation>.Builder violations)
	{
		string[] declaredGroups = capturedTerminator.OpeningExpression.GetGroupNames();

		if (capturedTerminator.OpeningExpression.MatchTimeout != LexicalEnginePolicy.LEXICAL_BUDGET)
		{
			Record(
				violations,
				AnalyzerContractViolationCode.HereDocumentOpeningMatchTimeoutNotLexicalBudget,
				quoteRule.Opening);
		}

		if (!declaredGroups.Contains(capturedTerminator.TerminatorCaptureGroupName, StringComparer.Ordinal))
		{
			Record(violations, AnalyzerContractViolationCode.CapturedTerminatorGroupAbsentFromExpression, quoteRule.Opening);
		}

		if (capturedTerminator.NonInterpolatingQuoteCaptureGroupName is string nonInterpolatingGroup
			&& !declaredGroups.Contains(nonInterpolatingGroup, StringComparer.Ordinal))
		{
			Record(violations, AnalyzerContractViolationCode.CapturedTerminatorGroupAbsentFromExpression, quoteRule.Opening);
		}

		foreach (string? decodedGroup in new[] { capturedTerminator.AnsiCQuotedCaptureGroupName, capturedTerminator.DoubleQuotedCaptureGroupName })
		{
			if (decodedGroup is not null && !declaredGroups.Contains(decodedGroup, StringComparer.Ordinal))
			{
				Record(violations, AnalyzerContractViolationCode.CapturedTerminatorGroupAbsentFromExpression, quoteRule.Opening);
			}
		}

		ValidateDeferredBodyStart(quoteRule, capturedTerminator, violations);
	}

	/// <summary>
	/// A body that starts after the opening line is consistent only with a whole-line closing,
	/// newlines that are content, and an opening that does not read the line it leaves to code.
	/// </summary>
	private static void ValidateDeferredBodyStart(
		QuoteRule quoteRule,
		CapturedTerminator capturedTerminator,
		ImmutableArray<AnalyzerContractViolation>.Builder violations)
	{
		if (capturedTerminator.BodyStart is HereDocumentBodyStart.AfterTheOpeningLine)
		{
			if (!IsWholeLineAnchoring(quoteRule.ClosingAnchoring))
			{
				Record(violations, AnalyzerContractViolationCode.DeferredBodyStartWithoutWholeLineClosing, quoteRule.Opening);
			}

			if (quoteRule.NewlinePolicy is not NewlinePolicy.Allowed)
			{
				Record(violations, AnalyzerContractViolationCode.DeferredBodyStartWithoutAllowedNewlines, quoteRule.Opening);
			}

			if (quoteRule.OpeningAnchoring is not PositionalAnchoring.Anywhere)
			{
				Record(violations, AnalyzerContractViolationCode.DeferredBodyStartWithOpeningAnchoring, quoteRule.Opening);
			}
		}
	}

	private static void ValidateLanguageIdentification(
		LanguageIdentification identification,
		ImmutableArray<AnalyzerContractViolation>.Builder violations)
	{
		if (identification.FileExtensions.Count == 0)
		{
			Record(violations, AnalyzerContractViolationCode.FileExtensionSetEmpty, string.Empty);
		}

		if (identification.DeclaredLanguageNames.Count == 0)
		{
			Record(violations, AnalyzerContractViolationCode.DeclaredLanguageNameSetEmpty, string.Empty);
		}

		foreach (string fileExtension in identification.FileExtensions)
		{
			ValidateFileExtension(identification, fileExtension, violations);
		}

		if (identification.FirstLineDetectionExpression is Regex firstLineExpression
			&& firstLineExpression.MatchTimeout != LexicalEnginePolicy.LEXICAL_BUDGET)
		{
			Record(
				violations,
				AnalyzerContractViolationCode.FirstLineDetectionMatchTimeoutNotLexicalBudget,
				string.Join(',', identification.DeclaredLanguageNames.Order(StringComparer.Ordinal)));
		}

		ValidateDeclaredUnsupportedCoverage(identification.DeclaredUnsupportedCoverage, violations);
	}

	/// <summary>
	/// Both strings non-empty, and no two entries naming the same
	/// <see cref="DeclaredUnsupportedCoverage.Construct"/> - by ordinal comparison, the same choice
	/// <see cref="DetectionRule.DisplayName"/> uniqueness makes below - with a different
	/// <see cref="DeclaredUnsupportedCoverage.Reason"/>. The set's own value equality already
	/// removes an exact duplicate record; this catches the case it cannot, where the reason text
	/// differs and the two records are therefore unequal.
	/// </summary>
	private static void ValidateDeclaredUnsupportedCoverage(
		FrozenSet<DeclaredUnsupportedCoverage> declaredUnsupportedCoverage,
		ImmutableArray<AnalyzerContractViolation>.Builder violations)
	{
		HashSet<string> seenConstructs = new(StringComparer.Ordinal);

		foreach (DeclaredUnsupportedCoverage entry in declaredUnsupportedCoverage)
		{
			if (string.IsNullOrWhiteSpace(entry.Construct))
			{
				Record(violations, AnalyzerContractViolationCode.DeclaredUnsupportedConstructMissing, entry.Construct);
			}

			if (string.IsNullOrWhiteSpace(entry.Reason))
			{
				Record(violations, AnalyzerContractViolationCode.DeclaredUnsupportedReasonMissing, entry.Construct);
			}

			if (!seenConstructs.Add(entry.Construct))
			{
				Record(violations, AnalyzerContractViolationCode.DeclaredUnsupportedConstructDuplicated, entry.Construct);
			}
		}
	}

	private static void ValidateFileExtension(
		LanguageIdentification identification,
		string fileExtension,
		ImmutableArray<AnalyzerContractViolation>.Builder violations)
	{
		bool isWellFormed =
			fileExtension.Length >= _MINIMUM_FILE_EXTENSION_LENGTH
			&& fileExtension[0] == _FILE_EXTENSION_LEADING_CHARACTER;

		if (!isWellFormed)
		{
			Record(violations, AnalyzerContractViolationCode.FileExtensionMalformed, fileExtension);
		}
		else
		{
			string derivedLanguageName = identification.LanguageNameDerivation(fileExtension, firstLine: null);

			if (!identification.DeclaredLanguageNames.Contains(derivedLanguageName))
			{
				Record(violations, AnalyzerContractViolationCode.DerivedLanguageNameNotDeclared, fileExtension);
			}
		}
	}

	private static void ValidateDetectionRules(
		ILanguageAnalyzer analyzer,
		ImmutableArray<AnalyzerContractViolation>.Builder violations)
	{
		if (analyzer.DetectionRulesByPatternClass.Count == 0)
		{
			Record(violations, AnalyzerContractViolationCode.DetectionRuleSetEmpty, string.Empty);
		}

		HashSet<string> seenDisplayNames = new(StringComparer.Ordinal);

		foreach (KeyValuePair<string, DetectionRule> entry in analyzer.DetectionRulesByPatternClass)
		{
			ValidateDetectionRule(entry.Key, entry.Value, seenDisplayNames, violations);
		}
	}

	/// <summary>
	/// Non-empty, and unique within the analyzer - by ordinal comparison, the natural
	/// choice for a field that the report promises stays stable while it is read, exactly like every
	/// other identity comparison this contract already makes ordinally (the language name index,
	/// the pattern class dictionaries).
	/// </summary>
	private static void ValidateDisplayName(
		string patternClassIdentifier,
		DetectionRule rule,
		HashSet<string> seenDisplayNames,
		ImmutableArray<AnalyzerContractViolation>.Builder violations)
	{
		if (string.IsNullOrWhiteSpace(rule.DisplayName))
		{
			Record(violations, AnalyzerContractViolationCode.DisplayNameMissing, patternClassIdentifier);
		}
		else if (!seenDisplayNames.Add(rule.DisplayName))
		{
			Record(violations, AnalyzerContractViolationCode.DisplayNameNotUnique, patternClassIdentifier);
		}
	}

	private static void ValidateDetectionRule(
		string patternClassIdentifier,
		DetectionRule rule,
		HashSet<string> seenDisplayNames,
		ImmutableArray<AnalyzerContractViolation>.Builder violations)
	{
		string[] declaredGroups = rule.Expression.GetGroupNames();

		ValidateDisplayName(patternClassIdentifier, rule, seenDisplayNames, violations);

		if (rule.Expression.MatchTimeout != RecognitionEnginePolicy.MATCH_BOUND)
		{
			Record(violations, AnalyzerContractViolationCode.DetectionRuleMatchTimeoutNotPolicyBound, patternClassIdentifier);
		}

		if (rule.AnchorPosition.CaptureGroupName is string anchorGroup
			&& !declaredGroups.Contains(anchorGroup, StringComparer.Ordinal))
		{
			Record(violations, AnalyzerContractViolationCode.AnchorCaptureGroupAbsentFromExpression, patternClassIdentifier);
		}

		if (rule.CredentialPosition.CaptureGroupName is string credentialGroup
			&& !declaredGroups.Contains(credentialGroup, StringComparer.Ordinal))
		{
			Record(violations, AnalyzerContractViolationCode.CredentialCaptureGroupAbsentFromExpression, patternClassIdentifier);
		}

		if (rule.VariableUse is RuleVariableUse variableUse
			&& !declaredGroups.Contains(variableUse.VariableGroupName, StringComparer.Ordinal))
		{
			Record(violations, AnalyzerContractViolationCode.RuleVariableUseGroupAbsentFromExpression, patternClassIdentifier);
		}

		ValidateSymbolicVerdictInterpretation(patternClassIdentifier, rule, declaredGroups, violations);
	}

	private static void ValidateSymbolicVerdictInterpretation(
		string patternClassIdentifier,
		DetectionRule rule,
		string[] declaredGroups,
		ImmutableArray<AnalyzerContractViolation>.Builder violations)
	{
		SymbolicVerdictInterpretation interpretation = rule.SymbolicVerdictInterpretation;

		if (interpretation.SchemeArgumentCaptureGroupName is string schemeGroup)
		{
			if (!declaredGroups.Contains(schemeGroup, StringComparer.Ordinal))
			{
				Record(violations, AnalyzerContractViolationCode.SchemeCaptureGroupAbsentFromExpression, patternClassIdentifier);
			}

			if (string.IsNullOrWhiteSpace(interpretation.ExpectedSchemeToken))
			{
				Record(violations, AnalyzerContractViolationCode.ExpectedSchemeTokenMissing, patternClassIdentifier);
			}
		}
		else if (DeclaresAnySymbolicResponse(interpretation))
		{
			Record(violations, AnalyzerContractViolationCode.SymbolicResponseDeclaredWithoutSchemeArgument, patternClassIdentifier);
		}
	}

	private static bool DeclaresAnySymbolicResponse(SymbolicVerdictInterpretation interpretation)
	{
		bool declaresResponse =
			interpretation.WhenResolvedMatching != SymbolicVerdictResponse.NoEffect
			|| interpretation.WhenResolvedNotMatching != SymbolicVerdictResponse.NoEffect
			|| interpretation.WhenIndeterminate != SymbolicVerdictResponse.NoEffect
			|| interpretation.WhenSchemeArgumentDidNotParticipate is not null;

		return declaresResponse;
	}

	/// <summary>
	/// Contract element 4's structurally checked properties: every
	/// <see cref="ContentMarker.Expression"/> must declare <see cref="RecognitionEnginePolicy.MATCH_BOUND"/>
	/// as its match timeout, the same bound <see cref="ValidateDetectionRule"/> enforces for
	/// a detection rule's own expression.
	///
	/// THE SAME RULE EXTENDS TO <see cref="LanguageShapedDemotionSignals.GenerationMarkerExpressions"/>:
	/// <see cref="Recognition.FileDemotionSignals"/> evaluates a generation marker inside the same
	/// match-bound bracket as a content marker, so it is held to the same declared timeout.
	/// <see cref="LanguageShapedDemotionSignals.FileNameSignals"/> and
	/// <see cref="LanguageShapedDemotionSignals.FolderNameSignals"/> carry no expression at all and
	/// stay outside this method for that reason alone.
	/// </summary>
	private static void ValidateDemotionSignals(
		LanguageShapedDemotionSignals demotionSignals,
		ImmutableArray<AnalyzerContractViolation>.Builder violations)
	{
		ValidateDemotionSignalKinds(demotionSignals, violations);

		foreach (ContentMarker contentMarker in demotionSignals.ContentMarkers)
		{
			if (contentMarker.Expression.MatchTimeout != RecognitionEnginePolicy.MATCH_BOUND)
			{
				Record(
					violations,
					AnalyzerContractViolationCode.ContentMarkerMatchTimeoutNotPolicyBound,
					contentMarker.Expression.ToString());
			}
		}

		foreach (Regex generationMarkerExpression in demotionSignals.GenerationMarkerExpressions)
		{
			if (generationMarkerExpression.MatchTimeout != RecognitionEnginePolicy.MATCH_BOUND)
			{
				Record(
					violations,
					AnalyzerContractViolationCode.GenerationMarkerExpressionMatchTimeoutNotPolicyBound,
					generationMarkerExpression.ToString());
			}
		}
	}

	private static void ValidateDemotionSignalKinds(
		LanguageShapedDemotionSignals demotionSignals,
		ImmutableArray<AnalyzerContractViolation>.Builder violations)
	{
		foreach (FileNameSignal fileNameSignal in demotionSignals.FileNameSignals)
		{
			RecordWhenKindUndeclared(violations, fileNameSignal.Kind, fileNameSignal.Text);
		}

		foreach (FolderNameSignal folderNameSignal in demotionSignals.FolderNameSignals)
		{
			RecordWhenKindUndeclared(violations, folderNameSignal.Kind, folderNameSignal.FolderName);
		}

		foreach (ContentMarker contentMarker in demotionSignals.ContentMarkers)
		{
			RecordWhenKindUndeclared(violations, contentMarker.Kind, contentMarker.Expression.ToString());
		}
	}

	private static void RecordWhenKindUndeclared(
		ImmutableArray<AnalyzerContractViolation>.Builder violations,
		DemotionKind kind,
		string subject)
	{
		if (kind is DemotionKind.Undeclared)
		{
			Record(violations, AnalyzerContractViolationCode.DemotionSignalKindUndeclared, subject);
		}
	}

	private static void ValidateSubsumptionRelation(
		ILanguageAnalyzer analyzer,
		ImmutableArray<AnalyzerContractViolation>.Builder violations)
	{
		foreach (SubsumptionRule subsumption in analyzer.SubsumptionRelation)
		{
			bool referencesKnownClasses =
				analyzer.DetectionRulesByPatternClass.ContainsKey(subsumption.SubsumingPatternClassIdentifier)
				&& analyzer.DetectionRulesByPatternClass.ContainsKey(subsumption.SubsumedPatternClassIdentifier);

			if (!referencesKnownClasses)
			{
				Record(violations, AnalyzerContractViolationCode.SubsumptionReferencesUnknownPatternClass, subsumption.SubsumingPatternClassIdentifier);
			}

			if (string.Equals(subsumption.SubsumingPatternClassIdentifier, subsumption.SubsumedPatternClassIdentifier, StringComparison.Ordinal))
			{
				Record(violations, AnalyzerContractViolationCode.SubsumptionIsReflexive, subsumption.SubsumingPatternClassIdentifier);
			}
		}
	}

	private static void ValidateConstructPrecedence(
		ILanguageAnalyzer analyzer,
		ImmutableArray<AnalyzerContractViolation>.Builder violations)
	{
		ConstructPrecedence? precedence = analyzer.ConstructPrecedence;

		if (precedence is not null)
		{
			HashSet<string> listed = new(StringComparer.Ordinal);

			foreach (string patternClass in precedence.PatternClassesInPrecedenceOrder)
			{
				if (!analyzer.DetectionRulesByPatternClass.ContainsKey(patternClass) || !listed.Add(patternClass))
				{
					Record(violations, AnalyzerContractViolationCode.ConstructPrecedenceListsAnUnknownOrRepeatedPatternClass, patternClass);
				}
			}

			ValidateConstructBoundaryTokens(precedence, violations);
		}
	}

	private static void ValidateConstructBoundaryTokens(
		ConstructPrecedence precedence,
		ImmutableArray<AnalyzerContractViolation>.Builder violations)
	{
		IEnumerable<string> tokens =
		[
			.. precedence.StatementSeparators,
			.. precedence.StatementNestings.SelectMany(nesting => new[] { nesting.Opening, nesting.Closing })
		];

		foreach (string token in tokens)
		{
			if (string.IsNullOrEmpty(token) || token.Contains(_LINE_FEED, StringComparison.Ordinal))
			{
				Record(violations, AnalyzerContractViolationCode.ConstructPrecedenceTokenMalformed, token);
			}
		}

		foreach (StatementNesting nesting in precedence.StatementNestings)
		{
			if (string.Equals(nesting.Opening, nesting.Closing, StringComparison.Ordinal))
			{
				Record(violations, AnalyzerContractViolationCode.ConstructPrecedenceTokenMalformed, nesting.Opening);
			}
		}
	}

	private static void ValidateCandidateSelectionAndRecommendation(
		ILanguageAnalyzer analyzer,
		ImmutableArray<AnalyzerContractViolation>.Builder violations)
	{
		CandidateSelectionAndRecommendation element = analyzer.CandidateSelectionAndRecommendation;

		if (element.PreselectionAnchors.Count == 0)
		{
			Record(violations, AnalyzerContractViolationCode.PreselectionAnchorSetEmpty, string.Empty);
		}

		foreach (PreselectionAnchor anchor in element.PreselectionAnchors)
		{
			if (string.IsNullOrWhiteSpace(anchor.Text))
			{
				Record(violations, AnalyzerContractViolationCode.PreselectionAnchorEmpty, anchor.Text);
			}
		}

		ValidateLanguageFallbackTexts(element, violations);

		foreach (string patternClassIdentifier in analyzer.DetectionRulesByPatternClass.Keys)
		{
			ValidateRecommendationTemplate(element, patternClassIdentifier, violations);
		}

		ValidateFlowSignals(element, violations);
		ValidateFileDialects(element.FileDialects, violations);
		ValidateWrapperTracking(element.WrapperTracking, violations);
	}

	/// <summary>
	/// Every value of the per-flow fallback dictionary must be non-blank; one violation is
	/// recorded per blank value, its subject the flow it was declared for.
	/// </summary>
	private static void ValidateLanguageFallbackTexts(
		CandidateSelectionAndRecommendation element,
		ImmutableArray<AnalyzerContractViolation>.Builder violations)
	{
		if (element.LanguageFallbackTextByFlow is not null)
		{
			foreach (KeyValuePair<CredentialFlow, string> declared in element.LanguageFallbackTextByFlow)
			{
				if (string.IsNullOrWhiteSpace(declared.Value))
				{
					Record(violations, AnalyzerContractViolationCode.LanguageFallbackTextBlank, CredentialFlows.Name(declared.Key));
				}
			}
		}
	}

	/// <summary>
	/// Contract element 5's flow signals: each one declares a kind, a zone, a non-blank
	/// expression carrying the policy match bound, and a name no other signal of the analyzer shares.
	/// </summary>
	private static void ValidateFlowSignals(
		CandidateSelectionAndRecommendation element,
		ImmutableArray<AnalyzerContractViolation>.Builder violations)
	{
		HashSet<string> declaredNames = new(StringComparer.Ordinal);
		HashSet<string> declaredDialects = DeclaredDialectsOf(element.FileDialects);

		foreach (FlowSignal flowSignal in element.FlowSignals.IsDefault ? [] : element.FlowSignals)
		{
			ValidateFlowSignal(flowSignal, violations);
			ValidateFlowSignalDialects(flowSignal, declaredDialects, violations);
			ValidateFlowSignalVariableUse(flowSignal, violations);

			if (!declaredNames.Add(flowSignal.Name))
			{
				Record(violations, AnalyzerContractViolationCode.FlowSignalNameDuplicated, flowSignal.Name);
			}
		}
	}

	private static void ValidateFlowSignal(
		FlowSignal flowSignal,
		ImmutableArray<AnalyzerContractViolation>.Builder violations)
	{
		if (flowSignal.Kind is FlowSignalKind.Undeclared)
		{
			Record(violations, AnalyzerContractViolationCode.FlowSignalKindUndeclared, flowSignal.Name);
		}

		if (flowSignal.Zone is FlowSignalZone.Undeclared)
		{
			Record(violations, AnalyzerContractViolationCode.FlowSignalZoneUndeclared, flowSignal.Name);
		}

		if (string.IsNullOrWhiteSpace(flowSignal.Expression.ToString()))
		{
			Record(violations, AnalyzerContractViolationCode.FlowSignalExpressionBlank, flowSignal.Name);
		}

		if (flowSignal.Expression.MatchTimeout != RecognitionEnginePolicy.MATCH_BOUND)
		{
			Record(violations, AnalyzerContractViolationCode.FlowSignalMatchTimeoutNotPolicyBound, flowSignal.Name);
		}
	}

	private static HashSet<string> DeclaredDialectsOf(FileDialects? fileDialects)
	{
		HashSet<string> declared = new(StringComparer.Ordinal);

		if (fileDialects is not null)
		{
			declared.Add(fileDialects.DefaultDialect);
			declared.UnionWith(fileDialects.ShebangInterpreters.Values);
			declared.UnionWith(fileDialects.ExtensionDialects.Values);
		}

		return declared;
	}

	private static void ValidateFlowSignalDialects(
		FlowSignal flowSignal,
		HashSet<string> declaredDialects,
		ImmutableArray<AnalyzerContractViolation>.Builder violations)
	{
		ImmutableArray<string> included = flowSignal.Dialects.IsDefault ? [] : flowSignal.Dialects;
		ImmutableArray<string> excluded = flowSignal.ExcludedDialects.IsDefault ? [] : flowSignal.ExcludedDialects;

		foreach (string dialect in included.Concat(excluded).Where(dialect => !declaredDialects.Contains(dialect)))
		{
			Record(violations, AnalyzerContractViolationCode.FlowSignalDialectNotDeclared, $"{flowSignal.Name}{_SUBJECT_SEPARATOR}{dialect}");
		}

		foreach (string dialect in included.Intersect(excluded, StringComparer.Ordinal))
		{
			Record(violations, AnalyzerContractViolationCode.FlowSignalDialectIncludedAndExcluded, $"{flowSignal.Name}{_SUBJECT_SEPARATOR}{dialect}");
		}
	}

	private static void ValidateFlowSignalVariableUse(
		FlowSignal flowSignal,
		ImmutableArray<AnalyzerContractViolation>.Builder violations)
	{
		if (flowSignal.VariableUse is FlowSignalVariableUse use)
		{
			bool groupDeclared = flowSignal.Expression.GetGroupNames().Contains(use.VariableGroupName, StringComparer.Ordinal);
			bool defaultBlank = use.DefaultVariableName is not null && string.IsNullOrWhiteSpace(use.DefaultVariableName);

			if ((!groupDeclared && use.DefaultVariableName is null) || defaultBlank)
			{
				Record(violations, AnalyzerContractViolationCode.FlowSignalVariableUseGroupAbsentFromExpression, flowSignal.Name);
			}

			if (string.IsNullOrWhiteSpace(use.UsePattern.ToString()))
			{
				Record(violations, AnalyzerContractViolationCode.FlowSignalVariableUsePatternBlank, flowSignal.Name);
			}

			if (use.UsePattern.MatchTimeout != RecognitionEnginePolicy.MATCH_BOUND)
			{
				Record(violations, AnalyzerContractViolationCode.FlowSignalVariableUseMatchTimeoutNotPolicyBound, flowSignal.Name);
			}

			if (!use.StatementSeparators.IsDefaultOrEmpty && use.StatementSeparators.Any(string.IsNullOrWhiteSpace))
			{
				Record(violations, AnalyzerContractViolationCode.FlowSignalVariableUseStatementSeparatorBlank, flowSignal.Name);
			}

			if (!IsWithin(use.WindowLines, 1, FlowSignalVariableUse.MAXIMUM_WINDOW_LINES)
				|| !IsWithin(use.MaxLogicalLineLength, 1, FlowSignalVariableUse.MAXIMUM_LOGICAL_LINE_LENGTH)
				|| !IsWithin(use.AliasDepth, 0, FlowSignalVariableUse.MAXIMUM_ALIAS_DEPTH))
			{
				Record(violations, AnalyzerContractViolationCode.FlowSignalVariableUseBoundInvalid, flowSignal.Name);
			}
		}
	}

	private static bool IsWithin(int value, int minimum, int maximum)
	{
		return value >= minimum && value <= maximum;
	}

	/// <summary>Contract element 5 optional dialect declaration: a non-blank default, non-empty keys and values, consistent extension keys.</summary>
	private static void ValidateFileDialects(
		FileDialects? fileDialects,
		ImmutableArray<AnalyzerContractViolation>.Builder violations)
	{
		if (fileDialects is not null)
		{
			if (string.IsNullOrWhiteSpace(fileDialects.DefaultDialect))
			{
				Record(violations, AnalyzerContractViolationCode.FileDialectsDefaultDialectBlank, string.Empty);
			}

			ValidateDialectMap(fileDialects.ShebangInterpreters, violations);
			ValidateDialectMap(fileDialects.ExtensionDialects, violations);
			ValidateExtensionKeys(fileDialects.ExtensionDialects, violations);
		}
	}

	private static void ValidateDialectMap(
		FrozenDictionary<string, string> map,
		ImmutableArray<AnalyzerContractViolation>.Builder violations)
	{
		foreach (KeyValuePair<string, string> entry in map)
		{
			if (string.IsNullOrWhiteSpace(entry.Key))
			{
				Record(violations, AnalyzerContractViolationCode.FileDialectsMapKeyEmpty, entry.Key ?? string.Empty);
			}

			if (string.IsNullOrWhiteSpace(entry.Value))
			{
				Record(violations, AnalyzerContractViolationCode.FileDialectsMapValueBlank, entry.Key ?? string.Empty);
			}
		}
	}

	private static void ValidateExtensionKeys(
		FrozenDictionary<string, string> extensionDialects,
		ImmutableArray<AnalyzerContractViolation>.Builder violations)
	{
		HashSet<string> seen = new(StringComparer.OrdinalIgnoreCase);

		foreach (string extension in extensionDialects.Keys.Where(key => !string.IsNullOrWhiteSpace(key)))
		{
			bool wellFormed = extension.Length >= _MINIMUM_FILE_EXTENSION_LENGTH
				&& extension[0] == _FILE_EXTENSION_LEADING_CHARACTER
				&& string.Equals(extension, extension.ToLowerInvariant(), StringComparison.Ordinal);

			if (!wellFormed)
			{
				Record(violations, AnalyzerContractViolationCode.FileDialectsExtensionKeyMalformed, extension);
			}

			if (!seen.Add(extension))
			{
				Record(violations, AnalyzerContractViolationCode.FileDialectsExtensionKeysConflict, extension);
			}
		}
	}

	/// <summary>Contract element 5 optional wrapper tracking: every expression on the policy bound and declaring its groups, bounds within those of the engine.</summary>
	private static void ValidateWrapperTracking(
		WrapperTracking? tracking,
		ImmutableArray<AnalyzerContractViolation>.Builder violations)
	{
		if (tracking is not null)
		{
			(string Name, Regex Expression, string[] Groups)[] expressions =
			[
				(nameof(WrapperTracking.FunctionDefinitionExpression), tracking.FunctionDefinitionExpression, [WrapperTracking.NAME_GROUP_NAME, WrapperTracking.OPEN_GROUP_NAME]),
				(nameof(WrapperTracking.PositionalReferenceExpression), tracking.PositionalReferenceExpression, [WrapperTracking.INDEX_GROUP_NAME]),
				(nameof(WrapperTracking.ParameterAliasExpression), tracking.ParameterAliasExpression, [WrapperTracking.ALIAS_GROUP_NAME, WrapperTracking.INDEX_GROUP_NAME]),
				(nameof(WrapperTracking.CallSiteExpression), tracking.CallSiteExpression, [WrapperTracking.NAME_GROUP_NAME, WrapperTracking.ARGUMENTS_GROUP_NAME]),
				(nameof(WrapperTracking.ArgumentExpression), tracking.ArgumentExpression, []),
				(nameof(WrapperTracking.CredentialBearingArgument), tracking.CredentialBearingArgument, [])
			];

			foreach ((string name, Regex expression, string[] groups) in expressions)
			{
				ValidateWrapperExpression(name, expression, groups, violations);
			}

			if (!IsWithin(tracking.MaxBodyLines, 1, WrapperTracking.MAXIMUM_BODY_LINES)
				|| !IsWithin(tracking.MaxCalls, 1, WrapperTracking.MAXIMUM_CALLS))
			{
				Record(violations, AnalyzerContractViolationCode.WrapperTrackingBoundInvalid, string.Empty);
			}
		}
	}

	private static void ValidateWrapperExpression(
		string name,
		Regex expression,
		string[] requiredGroups,
		ImmutableArray<AnalyzerContractViolation>.Builder violations)
	{
		if (expression.MatchTimeout != RecognitionEnginePolicy.MATCH_BOUND)
		{
			Record(violations, AnalyzerContractViolationCode.WrapperTrackingMatchTimeoutNotPolicyBound, name);
		}

		string[] declaredGroups = expression.GetGroupNames();

		foreach (string group in requiredGroups.Where(group => !declaredGroups.Contains(group, StringComparer.Ordinal)))
		{
			Record(violations, AnalyzerContractViolationCode.WrapperTrackingGroupAbsentFromExpression, $"{name}{_SUBJECT_SEPARATOR}{group}");
		}
	}

	private static void ValidateRecommendationTemplate(
		CandidateSelectionAndRecommendation element,
		string patternClassIdentifier,
		ImmutableArray<AnalyzerContractViolation>.Builder violations)
	{
		foreach (CredentialFlow flow in Enum.GetValues<CredentialFlow>())
		{
			RecommendationTemplate? template = element.TemplateProvider(patternClassIdentifier, flow);

			if (template is not null && !IsComplete(template))
			{
				Record(violations, AnalyzerContractViolationCode.RecommendationTemplateIncomplete, patternClassIdentifier);
			}
		}
	}

	private static bool IsComplete(RecommendationTemplate template)
	{
		bool isComplete =
			!string.IsNullOrWhiteSpace(template.GenericSourceForm)
			&& !string.IsNullOrWhiteSpace(template.TargetFormUnderTokenAuthentication)
			&& !string.IsNullOrWhiteSpace(template.CredentialProvenance)
			&& !string.IsNullOrWhiteSpace(template.VerificationNote);

		return isComplete;
	}

	private static void Record(
		ImmutableArray<AnalyzerContractViolation>.Builder violations,
		AnalyzerContractViolationCode code,
		string subject)
	{
		violations.Add(new AnalyzerContractViolation { Code = code, Subject = subject });
	}
}
