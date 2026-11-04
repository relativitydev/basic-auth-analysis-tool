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
	/// <see cref="DetectionRule.DisplayName"/>, by ordinal comparison. The report renders this name in place of the identifier; two classes sharing one name would make that rendering
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
	/// declared - not null - but empty or whitespace-only. The slot is optional;
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
	FirstLineDetectionMatchTimeoutNotLexicalBudget = 37
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
		}

		foreach (BlockCommentRule blockCommentRule in profile.BlockCommentRulesInEngineOrder)
		{
			if (string.IsNullOrEmpty(blockCommentRule.Opening) || string.IsNullOrEmpty(blockCommentRule.Closing))
			{
				Record(violations, AnalyzerContractViolationCode.CommentDelimiterEmpty, profile.ProfileName);
			}
		}
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
	/// choice for a field that stays stable while it is read, exactly like every
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
			|| interpretation.WhenIndeterminate != SymbolicVerdictResponse.NoEffect;

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

		ValidateFlowSignals(element.FlowSignals, violations);
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
		ImmutableArray<FlowSignal> flowSignals,
		ImmutableArray<AnalyzerContractViolation>.Builder violations)
	{
		HashSet<string> declaredNames = new(StringComparer.Ordinal);

		foreach (FlowSignal flowSignal in flowSignals.IsDefault ? [] : flowSignals)
		{
			ValidateFlowSignal(flowSignal, violations);

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
