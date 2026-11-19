using System.Collections.Frozen;
using System.Text.RegularExpressions;

namespace MigrateAuth.Cli.Catalog.Languages;

/// <summary>
/// The detection rules of the `Python` analyzer: the fourteen pattern classes PY-001 to PY-014, each with its level, its self-sufficiency, its credential position and its display name, and the declarations that go
/// with them: the subsumption relation and the preselection anchors.
///
/// LEVELS COMPOSE BY MINIMUM, so the level a rule declares is a ceiling and not a prediction. Five classes can reach High (PY-001 to PY-005), eight are at most Medium (PY-006 to PY-013) and one is Low by definition
/// (PY-014, a header mapping outside any carrier, whose anchor is a quoted key and so a string body). The unnamed tuple class PY-006 is capped at Medium by design: nothing in its text names the client that receives the tuple.
///
/// THE ANCHOR. Every class anchors on the capture group <c>construct</c>, a code token, except PY-002 and PY-006, which anchor on the group <c>auth</c>: the keyword itself, the same position in both, which is what lets the
/// first triple of the subsumption relation apply at identical anchors.
///
/// THE CREDENTIAL POSITION. A class that reads a literal credential declares the capture group named <see cref="CREDENTIAL_CAPTURE_GROUP"/>. Every such group takes no part in the match unless the value is a single-line quoted
/// literal that is the whole value and not a placeholder, so a name, a call, a concatenation or a format operation is never read as a literal. The two classes that read no credential (PY-004 and PY-010) declare none. PY-013 declares it for the held declaration
/// `NAME = "Basic <token>"` of the name its carrier uses (detection catalog 1.50.0): the group is captured from that declaration by a look-behind of the class expression, and takes no part otherwise.
///
/// THE DESTINATION IS UNKNOWN for every class: a script has no type that names Relativity. Only PY-013 declares a scheme group, because only its value can be an identifier that the file symbol table resolves.
/// </summary>
internal static class PythonRuleDeclarations
{
	internal const string CREDENTIAL_CAPTURE_GROUP = "credential";

	private const string _CONSTRUCT_CAPTURE_GROUP = "construct";
	private const string _AUTH_CAPTURE_GROUP = "auth";
	private const string _SCHEME_CAPTURE_GROUP = "scheme";
	private const string _EXPECTED_SCHEME_TOKEN = "Basic";

	private const string _PY_001 = "PY-001";
	private const string _PY_002 = "PY-002";
	private const string _PY_003 = "PY-003";
	private const string _PY_004 = "PY-004";
	private const string _PY_005 = "PY-005";
	private const string _PY_006 = "PY-006";
	private const string _PY_007 = "PY-007";
	private const string _PY_008 = "PY-008";
	private const string _PY_009 = "PY-009";
	private const string _PY_010 = "PY-010";
	private const string _PY_011 = "PY-011";
	private const string _PY_012 = "PY-012";
	private const string _PY_013 = "PY-013";
	private const string _PY_014 = "PY-014";

	private const string _AUTH_ANCHOR = "auth";
	private const string _ENCODER_ANCHOR = "b64encode";
	private const string _BYTES_ENCODER_ANCHOR = "encodebytes";
	private const string _ADD_PASSWORD_ANCHOR = "add_password";
	private const string _URL_SCHEME_ANCHOR = "://";

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
		rules[_PY_001] = BuildRule(new RuleDeclaration(PythonPatternExpressions.BasicAuthHelperConstruction(), "BasicAuthHelperConstruction", ConfidenceLevel.High, true, _CONSTRUCT_CAPTURE_GROUP), LiteralCredential());
		rules[_PY_002] = BuildRule(new RuleDeclaration(PythonPatternExpressions.NamedClientAuthTuple(), "NamedClientAuthTuple", ConfidenceLevel.High, true, _AUTH_CAPTURE_GROUP), LiteralCredential());
		rules[_PY_003] = BuildRule(new RuleDeclaration(PythonPatternExpressions.AuthorizationHeaderBasicScheme(), "AuthorizationHeaderBasicScheme", ConfidenceLevel.High, true, _CONSTRUCT_CAPTURE_GROUP), LiteralCredential());
		rules[_PY_004] = BuildRule(new RuleDeclaration(PythonPatternExpressions.UrllibBasicAuthHandlerConstruction(), "UrllibBasicAuthHandlerConstruction", ConfidenceLevel.High, true, _CONSTRUCT_CAPTURE_GROUP));
		rules[_PY_005] = BuildRule(new RuleDeclaration(PythonPatternExpressions.Urllib3MakeHeadersBasicAuth(), "Urllib3MakeHeadersBasicAuth", ConfidenceLevel.High, true, _CONSTRUCT_CAPTURE_GROUP), LiteralCredential());
	}

	private static void AddMediumRules(Dictionary<string, DetectionRule> rules)
	{
		rules[_PY_006] = BuildRule(new RuleDeclaration(PythonPatternExpressions.UnnamedAuthTuple(), "UnnamedAuthTuple", ConfidenceLevel.Medium, false, _AUTH_CAPTURE_GROUP), LiteralCredential());
		rules[_PY_007] = BuildRule(new RuleDeclaration(PythonPatternExpressions.Base64UserPasswordPair(), "Base64UserPasswordPair", ConfidenceLevel.Medium, false, _CONSTRUCT_CAPTURE_GROUP), LiteralCredential());
		rules[_PY_008] = BuildRule(new RuleDeclaration(PythonPatternExpressions.AiohttpEncodeBasicAuth(), "AiohttpEncodeBasicAuth", ConfidenceLevel.Medium, false, _CONSTRUCT_CAPTURE_GROUP), LiteralCredential());
		rules[_PY_009] = BuildRule(new RuleDeclaration(PythonPatternExpressions.PasswordManagerAddPassword(), "PasswordManagerAddPassword", ConfidenceLevel.Medium, false, _CONSTRUCT_CAPTURE_GROUP), LiteralCredential());
		rules[_PY_010] = BuildRule(new RuleDeclaration(PythonPatternExpressions.HttpxNetrcAuth(), "HttpxNetrcAuth", ConfidenceLevel.Medium, false, _CONSTRUCT_CAPTURE_GROUP));
		rules[_PY_011] = BuildRule(new RuleDeclaration(PythonPatternExpressions.CredentialsInRequestUrl(), "CredentialsInRequestUrl", ConfidenceLevel.Medium, false, _CONSTRUCT_CAPTURE_GROUP), LiteralCredential());
		rules[_PY_012] = BuildRule(new RuleDeclaration(PythonPatternExpressions.UrlUserinfoHeldLiteral(), "UrlUserinfoHeldLiteral", ConfidenceLevel.Medium, false, _CONSTRUCT_CAPTURE_GROUP), LiteralCredential());
		rules[_PY_013] = BuildRuleWithSymbolicVerdict(
			new RuleDeclaration(PythonPatternExpressions.AuthorizationHeaderValueHeldInVariable(), "AuthorizationHeaderValueHeldInVariable", ConfidenceLevel.Medium, false, _CONSTRUCT_CAPTURE_GROUP),
			BuildSchemeVerdictInterpretation(),
			LiteralCredential());
	}

	private static void AddLowRules(Dictionary<string, DetectionRule> rules)
	{
		rules[_PY_014] = BuildRule(
			new RuleDeclaration(PythonPatternExpressions.BasicLiteralInAuthorizationMapping(), "BasicLiteralInAuthorizationMapping", ConfidenceLevel.Low, false, _CONSTRUCT_CAPTURE_GROUP),
			LiteralCredential());
	}

	/// <summary>
	/// A script has no type that names Relativity, so every destination is unknown. A class that declares its scheme and carrier on its own is self-sufficient; the others need the call path established.
	/// </summary>
	private static DetectionRule BuildRule(RuleDeclaration declaration, CredentialPosition? credentialPosition = null)
	{
		return Assemble(
			declaration,
			new RulePositions(credentialPosition ?? CredentialPosition.None(), SymbolicVerdictInterpretation.NotSubjectToSymbolicResolution));
	}

	/// <summary>
	/// A rule whose scheme argument the file symbol table resolves. The credential position is the token of a held literal declaration (detection catalog 1.50.0, form (a) of `PY-013`), which the expression captures from the declaration
	/// the carrier's name resolves to; the position is that of the declared literal, and a name whose declaration holds no token has none.
	/// </summary>
	private static DetectionRule BuildRuleWithSymbolicVerdict(RuleDeclaration declaration, SymbolicVerdictInterpretation symbolicVerdictInterpretation, CredentialPosition credentialPosition)
	{
		return Assemble(declaration, new RulePositions(credentialPosition, symbolicVerdictInterpretation));
	}

	/// <summary>What identifies a rule: its expression, name, base level, whether it declares its own scheme and carrier, and the capture group that anchors it.</summary>
	private readonly record struct RuleDeclaration(Regex Expression, string DisplayName, ConfidenceLevel BaseLevel, bool DeclaresSchemeAndCarrier, string AnchorGroupName);

	/// <summary>The credential position and the symbolic verdict a rule declares, held together.</summary>
	private readonly record struct RulePositions(CredentialPosition Credential, SymbolicVerdictInterpretation Verdict);

	private static DetectionRule Assemble(RuleDeclaration declaration, RulePositions positions)
	{
		DetectionRule rule = new()
		{
			Expression = declaration.Expression,
			AnchorPosition = MatchAnchor.AtCaptureGroup(declaration.AnchorGroupName),
			BaseLevel = declaration.BaseLevel,
			SelfSufficiency = declaration.DeclaresSchemeAndCarrier ? SelfSufficiency.DeclaresSchemeAndCredentialCarrier : SelfSufficiency.RequiresCallPathToBeEstablished,
			SymbolicVerdictInterpretation = positions.Verdict,
			CredentialPosition = positions.Credential,
			DestinationCertainty = DestinationCertainty.Unknown,
			DisplayName = declaration.DisplayName
		};

		return rule;
	}

	/// <summary>The literal credential of a single-line quoted literal that is the whole value: the zone of the capture decides, as it does for any quoted literal.</summary>
	private static CredentialPosition LiteralCredential()
	{
		return CredentialPosition.AtCaptureGroup(CREDENTIAL_CAPTURE_GROUP);
	}

	/// <summary>
	/// The scheme of a header value held in a variable. A resolved <c>Basic</c> confirms the class and never promotes it; a resolved other scheme is the migrated form, and a name the file symbol table cannot resolve (a call, a
	/// parameter, an import, another module) is silent too, because the identifier-valued header is the dominant form of the migrated code.
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
	/// The six triples of the catalog. The named tuple class subsumes the unnamed one at the identical `auth` anchor, so a named call is one finding; a header write subsumes the encoder, the Basic-named encoder and the
	/// quoted-key mapping that lie within its matched value; the encoder subsumes the mapping outside a carrier whose value holds it; and a consumer's argument window subsumes the userinfo literal held in it. Every other pair of
	/// classes is disjoint by value shape, by call or by the refusal that separates them, or is two facts at two anchors and is emitted twice.
	/// </summary>
	internal static FrozenSet<SubsumptionRule> BuildSubsumptionRelation()
	{
		FrozenSet<SubsumptionRule> relation = new[]
		{
			Subsume(_PY_002, _PY_006, SubsumptionCondition.AnchorPositionsAreIdentical),
			Subsume(_PY_003, _PY_007, SubsumptionCondition.MatchSpansOverlap),
			Subsume(_PY_003, _PY_008, SubsumptionCondition.MatchSpansOverlap),
			Subsume(_PY_003, _PY_014, SubsumptionCondition.MatchSpansOverlap),
			Subsume(_PY_007, _PY_014, SubsumptionCondition.MatchSpansOverlap),
			Subsume(_PY_011, _PY_012, SubsumptionCondition.MatchSpansOverlap)
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
	/// The five anchors, all insensitive (<c>auth</c>, <c>Auth</c> and <c>AUTH</c> all occur). Preselection is file-level and admits a file on any one, so a file admitted by one is run through every class: every class must bring
	/// its own anchor. <c>auth</c> reaches the helper, tuple, header, handler, <c>make_headers</c>, encoder-named, netrc and held-header classes (the header name contains it); <c>b64encode</c> and <c>encodebytes</c> reach the
	/// encoder, <c>add_password</c> the password manager, and <c>://</c> the two userinfo classes. <c>Basic</c> is deliberately not an anchor, and <c>base64</c> alone would admit every file that decodes data.
	/// </summary>
	internal static FrozenSet<PreselectionAnchor> BuildPreselectionAnchors()
	{
		string[] texts = [_AUTH_ANCHOR, _ENCODER_ANCHOR, _BYTES_ENCODER_ANCHOR, _ADD_PASSWORD_ANCHOR, _URL_SCHEME_ANCHOR];

		return texts.Select(text => new PreselectionAnchor { Text = text, Sensitivity = CaseSensitivity.Insensitive }).ToFrozenSet();
	}
}
