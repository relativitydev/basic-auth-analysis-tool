using System.Collections.Frozen;
using System.Text.RegularExpressions;

using MigrateAuth.Cli.Findings;

namespace MigrateAuth.Cli.Catalog.Languages;

/// <summary>
/// The detection rules of the `JavaScript` analyzer: the nineteen pattern classes JS-001 to JS-019, each with its level, its self-sufficiency, its credential
/// position and its display name, and the declarations that go with them: the subsumption relation and the preselection anchors.
///
/// LEVELS COMPOSE BY MINIMUM, so the level a rule declares is a ceiling and not a prediction. Twelve classes can reach High or Medium; seven are Low by definition and
/// carry no recommendation: the three whose call sends Basic only after the server challenges for it (an XHR open, a jQuery request and a request client with the preemptive
/// switch off), the shape of an authentication object that no HTTP client is adjacent to, the helper definition, the header whose whole value comes from the environment, and the header whose
/// whole value is a settings property that the file fills from an environment value.
///
/// THE CREDENTIAL POSITION. A class that reads a literal credential declares the capture group named <see cref="CREDENTIAL_CAPTURE_GROUP"/>. Every such group takes no part in the
/// match unless the value is a string or a template with no hole and not a placeholder, so an identifier, a concatenation or a call is never read as a literal.
///
/// THE DESTINATION IS UNKNOWN for every class: a script has no type that names Relativity. Only JS-017, JS-018 and JS-019 declare a scheme group, because only their value can be an identifier that the
/// file symbol table resolves (JS-019's, a member name that the table's member dictionary resolves).
/// </summary>
internal static class JavaScriptRuleDeclarations
{
	internal const string CREDENTIAL_CAPTURE_GROUP = "credential";

	private const string _CONSTRUCT_CAPTURE_GROUP = "construct";
	private const string _SCHEME_CAPTURE_GROUP = "scheme";
	private const string _EXPECTED_SCHEME_TOKEN = "Basic";

	private const string _JS_001 = "JS-001";
	private const string _JS_002 = "JS-002";
	private const string _JS_003 = "JS-003";
	private const string _JS_004 = "JS-004";
	private const string _JS_005 = "JS-005";
	private const string _JS_006 = "JS-006";
	private const string _JS_007 = "JS-007";
	private const string _JS_008 = "JS-008";
	private const string _JS_009 = "JS-009";
	private const string _JS_010 = "JS-010";
	private const string _JS_011 = "JS-011";
	private const string _JS_012 = "JS-012";
	private const string _JS_013 = "JS-013";
	private const string _JS_014 = "JS-014";
	private const string _JS_015 = "JS-015";
	private const string _JS_016 = "JS-016";
	private const string _JS_017 = "JS-017";
	private const string _JS_018 = "JS-018";
	private const string _JS_019 = "JS-019";

	private const string _AUTH_ANCHOR = "auth";
	private const string _PASSWORD_ANCHOR = "password";
	private const string _USERNAME_ANCHOR = "username";
	private const string _BTOA_ANCHOR = "btoa";
	private const string _BUFFER_FROM_ANCHOR = "Buffer.from(";
	private const string _BUFFER_CALL_ANCHOR = "Buffer(";
	private const string _URL_SCHEME_ANCHOR = "://";
	private const string _XML_HTTP_REQUEST_ANCHOR = "XMLHttpRequest";
	private const string _XHR_ANCHOR = "xhr";

	internal static FrozenDictionary<string, DetectionRule> BuildDetectionRules()
	{
		Dictionary<string, DetectionRule> rules = new(StringComparer.Ordinal);

		AddHighRules(rules);
		AddMediumRules(rules);
		AddLowRules(rules);

		return rules.ToFrozenDictionary(StringComparer.Ordinal);
	}

	private static void AddHighRules(Dictionary<string, DetectionRule> rules)
	{
		rules[_JS_001] = BuildRule(
			JavaScriptPatternExpressions.ClientAuthCredentialObject(),
			"ClientAuthCredentialObject",
			ConfidenceLevel.High,
			true,
			CredentialPosition.AtBareWordOrLiteralCaptureGroup(CREDENTIAL_CAPTURE_GROUP));
		rules[_JS_002] = BuildRule(JavaScriptPatternExpressions.ClientAuthMethodCall(), "ClientAuthMethodCall", ConfidenceLevel.High, true, LiteralCredential());
		rules[_JS_003] = BuildRule(JavaScriptPatternExpressions.CredentialsInRequestUrl(), "CredentialsInRequestUrl", ConfidenceLevel.High, true, LiteralCredential());
		rules[_JS_004] = BuildRule(JavaScriptPatternExpressions.AuthorizationHeaderScheme(), "AuthorizationHeaderScheme", ConfidenceLevel.High, true, LiteralCredential());
		rules[_JS_008] = BuildRule(JavaScriptPatternExpressions.NodeCoreAuthString(), "NodeCoreAuthString", ConfidenceLevel.High, true, LiteralCredential());
		rules[_JS_015] = BuildRule(JavaScriptPatternExpressions.GotCredentialOptions(), "GotCredentialOptions", ConfidenceLevel.High, true, LiteralCredential());
	}

	private static void AddMediumRules(Dictionary<string, DetectionRule> rules)
	{
		rules[_JS_005] = BuildRule(JavaScriptPatternExpressions.Base64UserPasswordPair(), "Base64UserPasswordPair", ConfidenceLevel.Medium, false, LiteralCredential());
		rules[_JS_006] = BuildRule(JavaScriptPatternExpressions.NodeBufferBase64Pair(), "NodeBufferBase64Pair", ConfidenceLevel.Medium, false, LiteralCredential());
		rules[_JS_010] = BuildRule(JavaScriptPatternExpressions.UrlCredentialAssignment(), "UrlCredentialAssignment", ConfidenceLevel.Medium, false, LiteralCredential());
		rules[_JS_012] = BuildRule(JavaScriptPatternExpressions.ClientAuthMethodSingleArgument(), "ClientAuthMethodSingleArgument", ConfidenceLevel.Medium, false);
		rules[_JS_013] = BuildRule(JavaScriptPatternExpressions.UrlUserinfoHeldLiteral(), "UrlUserinfoHeldLiteral", ConfidenceLevel.Medium, false, LiteralCredential());
		rules[_JS_017] = BuildRuleWithSymbolicVerdict(
			JavaScriptPatternExpressions.AuthorizationHeaderValueHeldInVariable(),
			"AuthorizationHeaderValueHeldInVariable",
			ConfidenceLevel.Medium,
			false,
			BuildSchemeVerdictInterpretation());
	}

	private static void AddLowRules(Dictionary<string, DetectionRule> rules)
	{
		rules[_JS_007] = BuildRule(JavaScriptPatternExpressions.AuthenticationHelperDefinition(), "AuthenticationHelperDefinition", ConfidenceLevel.Low, false);
		rules[_JS_009] = BuildRule(JavaScriptPatternExpressions.XhrOpenCredentials(), "XhrOpenCredentials", ConfidenceLevel.Low, false, LiteralCredential());
		rules[_JS_011] = BuildRule(JavaScriptPatternExpressions.NonHttpAuthObject(), "NonHttpAuthObject", ConfidenceLevel.Low, false, LiteralCredential());
		rules[_JS_014] = BuildRule(JavaScriptPatternExpressions.JqueryAjaxCredentials(), "JqueryAjaxCredentials", ConfidenceLevel.Low, false, LiteralCredential());
		rules[_JS_016] = BuildRule(JavaScriptPatternExpressions.RequestChallengeDependentAuth(), "RequestChallengeDependentAuth", ConfidenceLevel.Low, false);
		rules[_JS_018] = BuildRuleWithSymbolicVerdict(
			JavaScriptPatternExpressions.AuthorizationHeaderValueFromEnvironment(),
			"AuthorizationHeaderValueFromEnvironment",
			ConfidenceLevel.Low,
			false,
			BuildEnvironmentVerdictInterpretation());
		rules[_JS_019] = BuildRuleWithSymbolicVerdict(
			JavaScriptPatternExpressions.AuthorizationHeaderValueFromSettingsProperty(),
			"AuthorizationHeaderValueFromSettingsProperty",
			ConfidenceLevel.Low,
			false,
			BuildSettingsPropertyVerdictInterpretation());
	}

	/// <summary>
	/// A script has no type that names Relativity, so every destination is unknown. A class that declares its scheme and carrier on its own is self-sufficient; the
	/// others need the call path established.
	/// </summary>
	private static DetectionRule BuildRule(
		Regex expression,
		string displayName,
		ConfidenceLevel baseLevel,
		bool declaresSchemeAndCarrier,
		CredentialPosition? credentialPosition = null)
	{
		return Assemble(
			expression,
			displayName,
			baseLevel,
			declaresSchemeAndCarrier,
			new RulePositions(credentialPosition ?? CredentialPosition.None(), SymbolicVerdictInterpretation.NotSubjectToSymbolicResolution));
	}

	/// <summary>A rule whose scheme argument the file symbol table resolves. It declares no credential position: the token is elsewhere.</summary>
	private static DetectionRule BuildRuleWithSymbolicVerdict(
		Regex expression,
		string displayName,
		ConfidenceLevel baseLevel,
		bool declaresSchemeAndCarrier,
		SymbolicVerdictInterpretation symbolicVerdictInterpretation)
	{
		return Assemble(expression, displayName, baseLevel, declaresSchemeAndCarrier, new RulePositions(CredentialPosition.None(), symbolicVerdictInterpretation));
	}

	/// <summary>The credential position and the symbolic verdict a rule declares, held together so a rule is built from five values.</summary>
	private readonly record struct RulePositions(CredentialPosition Credential, SymbolicVerdictInterpretation Verdict);

	private static DetectionRule Assemble(
		Regex expression,
		string displayName,
		ConfidenceLevel baseLevel,
		bool declaresSchemeAndCarrier,
		RulePositions positions)
	{
		DetectionRule rule = new()
		{
			Expression = expression,
			AnchorPosition = MatchAnchor.AtCaptureGroup(_CONSTRUCT_CAPTURE_GROUP),
			BaseLevel = baseLevel,
			SelfSufficiency = declaresSchemeAndCarrier ? SelfSufficiency.DeclaresSchemeAndCredentialCarrier : SelfSufficiency.RequiresCallPathToBeEstablished,
			SymbolicVerdictInterpretation = positions.Verdict,
			CredentialPosition = positions.Credential,
			DestinationCertainty = DestinationCertainty.Unknown,
			DisplayName = displayName
		};

		return rule;
	}

	/// <summary>The literal credential of a string or of a template with no hole: the zone of the capture decides, as it does for any quoted literal.</summary>
	private static CredentialPosition LiteralCredential()
	{
		return CredentialPosition.AtCaptureGroup(CREDENTIAL_CAPTURE_GROUP);
	}

	/// <summary>
	/// The scheme of a header value held in a variable. A resolved <c>Basic</c> confirms the class and never promotes it; a resolved other scheme is the migrated form and a name the
	/// file symbol table cannot resolve (a call, a parameter, another module) is silent too.
	/// </summary>
	private static SymbolicVerdictInterpretation BuildSchemeVerdictInterpretation()
	{
		return new SymbolicVerdictInterpretation
		{
			SchemeArgumentCaptureGroupName = _SCHEME_CAPTURE_GROUP,
			ExpectedSchemeToken = _EXPECTED_SCHEME_TOKEN,
			WhenResolvedMatching = SymbolicVerdictResponse.ConfirmsPatternClass,
			WhenResolvedNotMatching = SymbolicVerdictResponse.SuppressesFinding,
			WhenIndeterminate = SymbolicVerdictResponse.SuppressesFinding
		};
	}

	/// <summary>
	/// The cell of the class that reads an environment value held in a name. A name that resolves to an environment declaration whose fallback word is empty (no fallback, or a fallback that is
	/// neither a Basic nor a bearer scheme, which the declaration expression records as empty) confirms the class; a name that resolves to anything else, to a literal, or to nothing, suppresses it.
	/// A match of the alternative that reads the environment value itself has no scheme argument and passes through. The expected token is blank on purpose: it reads the empty word.
	/// </summary>
	private static SymbolicVerdictInterpretation BuildEnvironmentVerdictInterpretation()
	{
		return new SymbolicVerdictInterpretation
		{
			SchemeArgumentCaptureGroupName = _SCHEME_CAPTURE_GROUP,
			ExpectedSourceKind = SymbolSourceKind.Environment,
			WhenResolvedMatching = SymbolicVerdictResponse.ConfirmsPatternClass,
			WhenResolvedNotMatching = SymbolicVerdictResponse.SuppressesFinding,
			WhenIndeterminate = SymbolicVerdictResponse.SuppressesFinding,
			WhenSchemeArgumentDidNotParticipate = SymbolicVerdictResponse.NoEffect
		};
	}

	/// <summary>
	/// The cell of the class that reads a settings property filled from an environment value. The captured name is looked up in the member dictionary of the file symbol table, order-free: the one
	/// member record of the name, an environment read with an empty fallback word, confirms the class; a record with a scheme fallback word, no record, two or more records, or a single record that is
	/// not an environment read, suppresses it. The class is Low by base level, so nothing in its cell can propose higher.
	/// </summary>
	private static SymbolicVerdictInterpretation BuildSettingsPropertyVerdictInterpretation()
	{
		return new SymbolicVerdictInterpretation
		{
			SchemeArgumentCaptureGroupName = _SCHEME_CAPTURE_GROUP,
			ExpectedSourceKind = SymbolSourceKind.Environment,
			SymbolRecordKind = SymbolRecordKind.Member,
			WhenResolvedMatching = SymbolicVerdictResponse.ConfirmsPatternClass,
			WhenResolvedNotMatching = SymbolicVerdictResponse.SuppressesFinding,
			WhenIndeterminate = SymbolicVerdictResponse.SuppressesFinding,
			WhenSchemeArgumentDidNotParticipate = SymbolicVerdictResponse.NoEffect
		};
	}

	/// <summary>
	/// The seven triples of the catalog. A header write subsumes the encoder nested in its value, and a `.auth(` call subsumes the encoder that is its argument, so one construction is one
	/// finding; the adjacent-client reading of an authentication object subsumes the Low one at the same anchor; a consumer's argument window subsumes the userinfo literal held in it. Every
	/// other pair of classes is disjoint by value shape, by arity, by call or by the refusal that separates them.
	/// </summary>
	internal static FrozenSet<SubsumptionRule> BuildSubsumptionRelation()
	{
		FrozenSet<SubsumptionRule> relation = new[]
		{
			Subsume(_JS_004, _JS_005, SubsumptionCondition.MatchSpansOverlap),
			Subsume(_JS_004, _JS_006, SubsumptionCondition.MatchSpansOverlap),
			Subsume(_JS_002, _JS_005, SubsumptionCondition.MatchSpansOverlap),
			Subsume(_JS_002, _JS_006, SubsumptionCondition.MatchSpansOverlap),
			Subsume(_JS_001, _JS_011, SubsumptionCondition.AnchorPositionsAreIdentical),
			Subsume(_JS_016, _JS_011, SubsumptionCondition.AnchorPositionsAreIdentical),
			Subsume(_JS_003, _JS_013, SubsumptionCondition.MatchSpansOverlap)
		}.ToFrozenSet();

		return relation;
	}

	private static SubsumptionRule Subsume(string subsuming, string subsumed, SubsumptionCondition condition)
	{
		return new SubsumptionRule
		{
			SubsumingPatternClassIdentifier = subsuming,
			SubsumedPatternClassIdentifier = subsumed,
			When = condition
		};
	}

	/// <summary>
	/// The nine anchors, all insensitive (<c>auth</c>, <c>Auth</c> and <c>AUTH</c> all occur). Preselection is file-level and admits a file on any one, so a file admitted by one is run
	/// through every class: every class must bring its own anchor. <c>Basic</c> is not an anchor, <c>Buffer</c> is an anchor only in its two call
	/// forms, and <c>XMLHttpRequest</c> and <c>xhr</c> reach the open call of a request object.
	/// </summary>
	internal static FrozenSet<PreselectionAnchor> BuildPreselectionAnchors()
	{
		string[] texts =
		[
			_AUTH_ANCHOR, _PASSWORD_ANCHOR, _USERNAME_ANCHOR, _BTOA_ANCHOR, _BUFFER_FROM_ANCHOR, _BUFFER_CALL_ANCHOR, _URL_SCHEME_ANCHOR, _XML_HTTP_REQUEST_ANCHOR, _XHR_ANCHOR
		];

		return texts.Select(text => new PreselectionAnchor { Text = text, Sensitivity = CaseSensitivity.Insensitive }).ToFrozenSet();
	}
}
