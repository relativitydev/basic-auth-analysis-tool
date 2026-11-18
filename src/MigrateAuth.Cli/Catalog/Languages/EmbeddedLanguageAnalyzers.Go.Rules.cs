using System.Collections.Frozen;
using System.Text.RegularExpressions;

using MigrateAuth.Cli.Findings;

namespace MigrateAuth.Cli.Catalog.Languages;

/// <summary>
/// The detection rules of the `Go` analyzer: the thirteen pattern classes GO-001 to GO-013, each with its level, its self-sufficiency, its credential position and its display name, and the
/// declarations that go with them: the subsumption relation and the preselection anchors.
///
/// LEVELS COMPOSE BY MINIMUM, so the level a rule declares is a ceiling and not a prediction. Ten classes can reach High or Medium; three are Low by definition and carry no recommendation: the
/// definition of a transport that writes Basic, the definition of a helper that builds a Basic header, and the header whose whole value is an environment read.
///
/// THE CREDENTIAL POSITION. A class that reads a literal credential declares the capture group named <see cref="CREDENTIAL_CAPTURE_GROUP"/>. Every such group takes no part in the match unless the
/// value is a whole string literal and not a placeholder, so an identifier, a concatenation or a call is never read as a literal.
///
/// THE DESTINATION IS UNKNOWN for every class: a `Go` program has no type that names Relativity. Only GO-009, GO-012 and GO-013 declare a scheme group, because only their value can be an identifier (GO-013: a called name, resolved in the member dictionary) that the
/// file symbol table resolves.
/// </summary>
internal static class GoRuleDeclarations
{
	internal const string CREDENTIAL_CAPTURE_GROUP = "credential";

	private const string _CONSTRUCT_CAPTURE_GROUP = "construct";
	private const string _SCHEME_CAPTURE_GROUP = "scheme";
	private const string _CALLED_NAME_CAPTURE_GROUP = "name";
	private const string _EXPECTED_SCHEME_TOKEN = "Basic";

	private const string _GO_001 = "GO-001";
	private const string _GO_002 = "GO-002";
	private const string _GO_003 = "GO-003";
	private const string _GO_004 = "GO-004";
	private const string _GO_005 = "GO-005";
	private const string _GO_006 = "GO-006";
	private const string _GO_007 = "GO-007";
	private const string _GO_008 = "GO-008";
	private const string _GO_009 = "GO-009";
	private const string _GO_010 = "GO-010";
	private const string _GO_011 = "GO-011";
	private const string _GO_012 = "GO-012";
	private const string _GO_013 = "GO-013";

	private const string _SET_BASIC_AUTH_ANCHOR = "SetBasicAuth";
	private const string _AUTHORIZATION_ANCHOR = "Authorization";
	private const string _USER_PASSWORD_ANCHOR = "UserPassword";
	private const string _STANDARD_ENCODING_ANCHOR = "StdEncoding";
	private const string _URL_SCHEME_ANCHOR = "://";
	private const string _ROUND_TRIP_ANCHOR = "RoundTrip";

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
		rules[_GO_001] = BuildRule(
			GoPatternExpressions.RequestSetBasicAuth(),
			"RequestSetBasicAuth",
			ConfidenceLevel.High,
			true,
			CredentialPosition.AtBareWordOrLiteralCaptureGroup(CREDENTIAL_CAPTURE_GROUP));
		rules[_GO_002] = BuildRule(GoPatternExpressions.AuthorizationHeaderBasicScheme(), "AuthorizationHeaderBasicScheme", ConfidenceLevel.High, true, LiteralCredential());
		rules[_GO_003] = BuildRule(GoPatternExpressions.HeaderLiteralBasicScheme(), "HeaderLiteralBasicScheme", ConfidenceLevel.High, true, LiteralCredential());
		rules[_GO_005] = BuildRule(GoPatternExpressions.UrlUserinfoInRequestCall(), "URLUserinfoInRequestCall", ConfidenceLevel.High, true, LiteralCredential());
	}

	private static void AddMediumRules(Dictionary<string, DetectionRule> rules)
	{
		rules[_GO_004] = BuildRule(GoPatternExpressions.MapLiteralBasicScheme(), "MapLiteralBasicScheme", ConfidenceLevel.Medium, false, LiteralCredential());
		rules[_GO_006] = BuildRule(GoPatternExpressions.Base64UserPasswordPair(), "Base64UserPasswordPair", ConfidenceLevel.Medium, false, LiteralCredential());
		rules[_GO_007] = BuildRule(GoPatternExpressions.UserinfoUserPassword(), "UserinfoUserPassword", ConfidenceLevel.Medium, false, LiteralCredential());
		rules[_GO_008] = BuildRule(GoPatternExpressions.UrlUserinfoHeldLiteral(), "URLUserinfoHeldLiteral", ConfidenceLevel.Medium, false, LiteralCredential());
		rules[_GO_009] = BuildRuleWithSymbolicVerdict(
			GoPatternExpressions.AuthorizationHeaderValueHeldInVariable(),
			"AuthorizationHeaderValueHeldInVariable",
			ConfidenceLevel.Medium,
			false,
			BuildSchemeVerdictInterpretation());
		rules[_GO_013] = BuildRuleWithSymbolicVerdict(
			GoPatternExpressions.AuthorizationHeaderValueFromLocalHelper(),
			"AuthorizationHeaderValueFromLocalHelper",
			ConfidenceLevel.Medium,
			false,
			BuildLocalHelperVerdictInterpretation());
	}

	private static void AddLowRules(Dictionary<string, DetectionRule> rules)
	{
		rules[_GO_010] = BuildRule(GoPatternExpressions.RoundTripperBasicAuthDefinition(), "RoundTripperBasicAuthDefinition", ConfidenceLevel.Low, false);
		rules[_GO_011] = BuildRule(GoPatternExpressions.AuthenticationHelperDefinition(), "AuthenticationHelperDefinition", ConfidenceLevel.Low, false);
		rules[_GO_012] = BuildRuleWithSymbolicVerdict(
			GoPatternExpressions.AuthorizationHeaderValueFromEnvironment(),
			"AuthorizationHeaderValueFromEnvironment",
			ConfidenceLevel.Low,
			false,
			BuildEnvironmentVerdictInterpretation());
	}

	/// <summary>
	/// A `Go` program has no type that names Relativity, so every destination is unknown. A class that declares its scheme and carrier on its own is self-sufficient; the others need the call path
	/// established.
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

	/// <summary>The literal credential of a whole string literal: the zone of the capture decides, as it does for any quoted literal.</summary>
	private static CredentialPosition LiteralCredential()
	{
		return CredentialPosition.AtCaptureGroup(CREDENTIAL_CAPTURE_GROUP);
	}

	/// <summary>
	/// The scheme of a header value held in a variable. A resolved <c>Basic</c> confirms the class and never promotes it; a resolved other scheme is the migrated form, and a name the file symbol
	/// table cannot resolve (a call, a parameter, another file) is silent too, because the identifier-valued header is the dominant form of the migrated code. A name that resolves to an
	/// environment read is a record of the other kind, so it is not matching and the class stays silent: that value belongs to GO-012.
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
	/// The cell of the class that reads an environment value held in a name. A name that resolves to an environment declaration whose fallback word is empty confirms the class; a name that
	/// resolves to anything else, to a literal, or to nothing, suppresses it. A match of the alternative that reads the environment value itself has no scheme argument and passes through. The
	/// expected token is blank on purpose: it reads the empty word.
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
	/// The cell of the class that reads a call to a same-file helper. The called name is looked up in the member dictionary of the file symbol table, order-free: the one member record of the name that carries the
	/// mark GO-011's reading sets (an empty environment-style marker with an empty fallback word) confirms the class and never promotes it; a record with no mark, a name recorded twice and a name not recorded
	/// at all are indeterminate and suppress it. The name always takes part in the match, so the unset-group response is never read.
	/// </summary>
	private static SymbolicVerdictInterpretation BuildLocalHelperVerdictInterpretation()
	{
		return new SymbolicVerdictInterpretation
		{
			SchemeArgumentCaptureGroupName = _CALLED_NAME_CAPTURE_GROUP,
			ExpectedSourceKind = SymbolSourceKind.Environment,
			SymbolRecordKind = SymbolRecordKind.Member,
			WhenResolvedMatching = SymbolicVerdictResponse.ConfirmsPatternClass,
			WhenResolvedNotMatching = SymbolicVerdictResponse.SuppressesFinding,
			WhenIndeterminate = SymbolicVerdictResponse.SuppressesFinding
		};
	}

	/// <summary>
	/// The three triples of the catalog. A header write, a header literal and an unnamed map literal each subsume the encoder nested in their value, so the canonical form of a Basic header, whose value
	/// joins the scheme literal with the encoder in the same expression, is one finding. The triple of a request call over the parse call (0.4) is withdrawn at 0.5: the two cannot overlap. Every other pair of
	/// classes is disjoint by value shape, by carrier or by construct, and the definition classes and the call to a helper are two facts at two anchors, so both are emitted.
	/// </summary>
	internal static FrozenSet<SubsumptionRule> BuildSubsumptionRelation()
	{
		FrozenSet<SubsumptionRule> relation = new[]
		{
			Subsume(_GO_002, _GO_006, SubsumptionCondition.MatchSpansOverlap),
			Subsume(_GO_003, _GO_006, SubsumptionCondition.MatchSpansOverlap),
			Subsume(_GO_004, _GO_006, SubsumptionCondition.MatchSpansOverlap)
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
	/// The six anchors, all insensitive. Preselection is file-level and admits a file on any one, so a file admitted by one is run through every class: every class must bring its own anchor.
	/// <c>Basic</c> is deliberately not an anchor, and neither is <c>EncodeToString</c>, which every file that handles bytes holds: <c>StdEncoding</c> (which <c>RawStdEncoding</c> contains) is narrower.
	/// <c>Authorization</c> and <c>://</c> are broad and are the open lever of the candidate ratio.
	/// </summary>
	internal static FrozenSet<PreselectionAnchor> BuildPreselectionAnchors()
	{
		string[] texts =
		[
			_SET_BASIC_AUTH_ANCHOR, _AUTHORIZATION_ANCHOR, _USER_PASSWORD_ANCHOR, _STANDARD_ENCODING_ANCHOR, _URL_SCHEME_ANCHOR, _ROUND_TRIP_ANCHOR
		];

		return texts.Select(text => new PreselectionAnchor { Text = text, Sensitivity = CaseSensitivity.Insensitive }).ToFrozenSet();
	}
}
