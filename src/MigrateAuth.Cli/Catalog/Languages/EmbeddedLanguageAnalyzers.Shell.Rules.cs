using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Text.RegularExpressions;

using MigrateAuth.Cli.Findings;

namespace MigrateAuth.Cli.Catalog.Languages;

/// <summary>
/// The detection rules of the `Shell` analyzer: the twenty-five pattern classes SH-001 to SH-025, each with its level, its self-sufficiency, its credential
/// position and its display name, and the declarations that go with them: the subsumption relation, the construct precedence, the preselection anchors and the
/// variable use of the one rule whose construct builds into a variable that the match itself does not read.
///
/// LEVELS COMPOSE BY MINIMUM, so the level a rule declares is a ceiling and not a prediction. Eighteen classes can reach High or Medium; seven are Low by
/// definition and carry no recommendation: the classes whose call sends Basic only after the server challenges for it, and the helper definition.
///
/// THE CREDENTIAL POSITION. A class that reads a literal credential declares the capture group named <see cref="CREDENTIAL_CAPTURE_GROUP"/>. Every such group
/// excludes <c>$</c>, the backtick and the backslash, or takes no part in the match, so an expansion is never read as a literal; the classes of the three
/// forms that a bare word can carry declare the bare-word reading, and the classes of an encoded pair, a Basic token held in a variable and a written
/// configuration line declare the position of a literal pair or token.
/// </summary>
internal static class ShellRuleDeclarations
{
	internal const string CREDENTIAL_CAPTURE_GROUP = "credential";

	private const string _CONSTRUCT_CAPTURE_GROUP = "construct";
	private const string _ASSIGNED_VARIABLE_CAPTURE_GROUP = "assigned";

	private const string _SH_001 = "SH-001";
	private const string _SH_002 = "SH-002";
	private const string _SH_003 = "SH-003";
	private const string _SH_004 = "SH-004";
	private const string _SH_005 = "SH-005";
	private const string _SH_006 = "SH-006";
	private const string _SH_007 = "SH-007";
	private const string _SH_008 = "SH-008";
	private const string _SH_009 = "SH-009";
	private const string _SH_010 = "SH-010";
	private const string _SH_011 = "SH-011";
	private const string _SH_012 = "SH-012";
	private const string _SH_013 = "SH-013";
	private const string _SH_014 = "SH-014";
	private const string _SH_015 = "SH-015";
	private const string _SH_016 = "SH-016";
	private const string _SH_017 = "SH-017";
	private const string _SH_018 = "SH-018";
	private const string _SH_019 = "SH-019";
	private const string _SH_020 = "SH-020";
	private const string _SH_021 = "SH-021";
	private const string _SH_022 = "SH-022";
	private const string _SH_023 = "SH-023";
	private const string _SH_024 = "SH-024";
	private const string _SH_025 = "SH-025";

	private const string _AND_LIST = "&&";
	private const string _OR_LIST = "||";
	private const string _SEQUENCE = ";";
	private const string _BACKGROUND = "&";
	private const string _SUBSTITUTION_OPENING = "$(";
	private const string _SUBSTITUTION_CLOSING = ")";

	private const string _CURL_ANCHOR = "curl";
	private const string _WGET_ANCHOR = "wget";
	private const string _AUTHORIZATION_ANCHOR = "Authorization";
	private const string _BASE64_ANCHOR = "base64";
	private const string _NETRC_ANCHOR = "netrc";
	private const string _URL_SCHEME_ANCHOR = "://";
	private const string _LWP_ANCHOR = "lwp-request";
	private const string _ARIA2C_ANCHOR = "aria2c";
	private const string _AUTH_OPTION_ANCHOR = "--auth";
	private const string _HTTP_SHORT_AUTH_ANCHOR = "http -a";
	private const string _HTTPS_SHORT_AUTH_ANCHOR = "https -a";

	/// <summary>
	/// The classes in the order the construct precedence ranks them, the most specific evidence first: the client's own credential option, then the
	/// credential in a header, in a URL and in a hand-built request, then the forms that join a definition to a use, then the Low classes. The level decides
	/// before this order does, so the order only separates classes of one level in one statement.
	/// </summary>
	private static readonly ImmutableArray<string> _classesInPrecedenceOrder =
	[
		_SH_001, _SH_002, _SH_004, _SH_013, _SH_014, _SH_016, _SH_003, _SH_005,
		_SH_019, _SH_018, _SH_021, _SH_007, _SH_024, _SH_008, _SH_009, _SH_025, _SH_010, _SH_006,
		_SH_023, _SH_011, _SH_017, _SH_015, _SH_022, _SH_020, _SH_012
	];

	internal static FrozenDictionary<string, DetectionRule> BuildDetectionRules()
	{
		Dictionary<string, DetectionRule> rules = new(StringComparer.Ordinal)
		{
			[_SH_001] = BuildRule(ShellPatternExpressions.CurlUserPassword(), "CurlUserPassword", ConfidenceLevel.High, true, BareWordCredential()),
			[_SH_002] = BuildRule(ShellPatternExpressions.CurlUserWithoutPassword(), "CurlUserWithoutPassword", ConfidenceLevel.High, true),
			[_SH_003] = BuildRule(ShellPatternExpressions.AuthorizationHeaderBasic(), "AuthorizationHeaderBasic", ConfidenceLevel.High, true, TokenCredential()),
			[_SH_004] = BuildRule(ShellPatternExpressions.WgetPreemptiveCredentials(), "WgetPreemptiveCredentials", ConfidenceLevel.High, true, BareWordCredential()),
			[_SH_005] = BuildRule(ShellPatternExpressions.CurlUrlUserinfo(), "CurlUrlUserinfo", ConfidenceLevel.High, true, BareWordCredential()),
			[_SH_006] = BuildRule(ShellPatternExpressions.CurlNetrcOption(), "CurlNetrcOption", ConfidenceLevel.Medium, false),
			[_SH_007] = BuildRuleWithPositions(
				ShellPatternExpressions.EncodedCredentialPair(),
				"EncodedCredentialPair",
				ConfidenceLevel.Medium,
				false,
				new RulePositions(BareWordCredential(), new RuleVariableUse { VariableGroupName = _ASSIGNED_VARIABLE_CAPTURE_GROUP })),
			[_SH_008] = BuildRule(ShellPatternExpressions.BasicTokenInVariable(), "BasicTokenInVariable", ConfidenceLevel.Medium, false, BareWordCredential()),
			[_SH_009] = BuildRule(ShellPatternExpressions.ArgumentArrayCredentials(), "ArgumentArrayCredentials", ConfidenceLevel.Medium, false),
			[_SH_010] = BuildRule(ShellPatternExpressions.UrlUserinfoAssignment(), "UrlUserinfoAssignment", ConfidenceLevel.Medium, false, BareWordCredential()),
			[_SH_011] = BuildRule(
				ShellPatternExpressions.WgetChallengeDependentCredentials(),
				"WgetChallengeDependentCredentials",
				ConfidenceLevel.Low,
				false,
				BareWordCredential()),
			[_SH_012] = BuildRule(ShellPatternExpressions.AuthenticationHelperDefinition(), "AuthenticationHelperDefinition", ConfidenceLevel.Low, false)
		};

		AddSecondHalfRules(rules);

		return rules.ToFrozenDictionary(StringComparer.Ordinal);
	}

	private static void AddSecondHalfRules(Dictionary<string, DetectionRule> rules)
	{
		rules[_SH_013] = BuildRule(ShellPatternExpressions.HttpieAuthentication(), "HttpieAuthentication", ConfidenceLevel.High, true, BareWordCredential());
		rules[_SH_014] = BuildRule(ShellPatternExpressions.Aria2cCredentials(), "Aria2cCredentials", ConfidenceLevel.High, true, BareWordCredential());
		rules[_SH_015] = BuildRule(
			ShellPatternExpressions.ChallengeDependentClientCredentials(),
			"ChallengeDependentClientCredentials",
			ConfidenceLevel.Low,
			false,
			BareWordCredential());
		rules[_SH_016] = BuildRule(
			ShellPatternExpressions.WgetExecuteCredentialsPreemptive(),
			"WgetExecuteCredentialsPreemptive",
			ConfidenceLevel.High,
			true,
			BareWordCredential());
		rules[_SH_017] = BuildRule(
			ShellPatternExpressions.WgetExecuteCredentialsChallengeDependent(),
			"WgetExecuteCredentialsChallengeDependent",
			ConfidenceLevel.Low,
			false,
			BareWordCredential());
		rules[_SH_018] = BuildRule(ShellPatternExpressions.HandBuiltBasicRequest(), "HandBuiltBasicRequest", ConfidenceLevel.Medium, false, TokenCredential());
		rules[_SH_019] = BuildRule(
			ShellPatternExpressions.CurlConfigurationWrittenByScript(),
			"CurlConfigurationWrittenByScript",
			ConfidenceLevel.Medium,
			false,
			BareWordCredential());
		rules[_SH_020] = BuildRule(
			ShellPatternExpressions.WgetConfigurationWrittenByScript(),
			"WgetConfigurationWrittenByScript",
			ConfidenceLevel.Low,
			false,
			BareWordCredential());
		rules[_SH_021] = BuildRule(
			ShellPatternExpressions.CurlViaSameFileAliasOrVariable(),
			"CurlViaSameFileAliasOrVariable",
			ConfidenceLevel.Medium,
			false);
		rules[_SH_022] = BuildRule(
			ShellPatternExpressions.WgetViaSameFileAliasOrVariable(),
			"WgetViaSameFileAliasOrVariable",
			ConfidenceLevel.Low,
			false);
		rules[_SH_023] = BuildRule(ShellPatternExpressions.CurlAnyauthWithUser(), "CurlAnyauthWithUser", ConfidenceLevel.Low, false, BareWordCredential());
		rules[_SH_024] = BuildRule(ShellPatternExpressions.EncodedPairHeldInVariable(), "EncodedPairHeldInVariable", ConfidenceLevel.Medium, false);
		rules[_SH_025] = BuildRule(ShellPatternExpressions.OptionsHeldInStringVariable(), "OptionsHeldInStringVariable", ConfidenceLevel.Medium, false);
	}

	/// <summary>
	/// A script has no type that names Relativity, so every destination is unknown and the scheme is never a capture group: no verdict of symbolic resolution
	/// applies to a Shell class. A class that declares its scheme and carrier on its own is self-sufficient; the others need the call path established.
	/// </summary>
	private static DetectionRule BuildRule(
		Regex expression,
		string displayName,
		ConfidenceLevel baseLevel,
		bool declaresSchemeAndCarrier,
		CredentialPosition? credentialPosition = null)
	{
		return BuildRuleWithPositions(
			expression,
			displayName,
			baseLevel,
			declaresSchemeAndCarrier,
			new RulePositions(credentialPosition ?? CredentialPosition.None(), null));
	}

	/// <summary>The credential position and the variable use a rule declares, held together so a rule is built from five values.</summary>
	private readonly record struct RulePositions(CredentialPosition Credential, RuleVariableUse? VariableUse);

	private static DetectionRule BuildRuleWithPositions(
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
			SymbolicVerdictInterpretation = SymbolicVerdictInterpretation.NotSubjectToSymbolicResolution,
			CredentialPosition = positions.Credential,
			DestinationCertainty = DestinationCertainty.Unknown,
			DisplayName = displayName,
			VariableUse = positions.VariableUse
		};

		return rule;
	}

	/// <summary>The credential a bare word can carry: the capture spans a whole unquoted word, or a quoted one, and holds no expansion.</summary>
	private static CredentialPosition BareWordCredential()
	{
		return CredentialPosition.AtBareWordOrLiteralCaptureGroup(CREDENTIAL_CAPTURE_GROUP);
	}

	/// <summary>The token of a literal header, which stands in a quoted string: the zone of the capture decides, as it does for any quoted literal.</summary>
	private static CredentialPosition TokenCredential()
	{
		return CredentialPosition.AtCaptureGroup(CREDENTIAL_CAPTURE_GROUP);
	}

	/// <summary>
	/// The six triples of the catalog: the preemptive `wget` forms subsume the challenge-dependent ones at the same anchor, and the `-e` forms rank between.
	/// Every other pair of classes is disjoint by value shape, by command word or by the refusal that separates them.
	/// </summary>
	internal static FrozenSet<SubsumptionRule> BuildSubsumptionRelation()
	{
		FrozenSet<SubsumptionRule> relation = new[]
		{
			Subsume(_SH_004, _SH_011),
			Subsume(_SH_004, _SH_016),
			Subsume(_SH_004, _SH_017),
			Subsume(_SH_016, _SH_017),
			Subsume(_SH_016, _SH_011),
			Subsume(_SH_011, _SH_017)
		}.ToFrozenSet();

		return relation;
	}

	private static SubsumptionRule Subsume(string subsuming, string subsumed)
	{
		return new SubsumptionRule
		{
			SubsumingPatternClassIdentifier = subsuming,
			SubsumedPatternClassIdentifier = subsumed,
			When = SubsumptionCondition.AnchorPositionsAreIdentical
		};
	}

	/// <summary>
	/// One finding per command. A statement ends at an unescaped line feed, at <c>;</c>, <c>&amp;&amp;</c>, <c>||</c> and <c>&amp;</c>, and not inside a
	/// command substitution, so a header that builds its token in a substitution and a call whose user option and URL both carry credentials are each one
	/// finding. A pipe is not a separator: the stages of a pipeline are read one by one by the classes that anchor on them.
	/// </summary>
	internal static ConstructPrecedence BuildConstructPrecedence()
	{
		return new ConstructPrecedence
		{
			PatternClassesInPrecedenceOrder = _classesInPrecedenceOrder,
			StatementSeparators = new[] { _SEQUENCE, _AND_LIST, _OR_LIST, _BACKGROUND }.ToFrozenSet(StringComparer.Ordinal),
			StatementNestings = new[] { new StatementNesting { Opening = _SUBSTITUTION_OPENING, Closing = _SUBSTITUTION_CLOSING } }.ToFrozenSet()
		};
	}

	/// <summary>
	/// The eleven anchors, all insensitive: a script writes <c>CURL</c>, <c>Curl</c> and <c>curl</c>. Preselection is file-level and admits a file on any one,
	/// so a file admitted by one is run through every class. The two anchors that hold a blank reach HTTPie called with its short option, which names the
	/// host in a variable and holds none of the others; <c>--auth</c> reaches the long option.
	/// </summary>
	internal static FrozenSet<PreselectionAnchor> BuildPreselectionAnchors()
	{
		string[] texts =
		[
			_CURL_ANCHOR, _WGET_ANCHOR, _AUTHORIZATION_ANCHOR, _BASE64_ANCHOR, _NETRC_ANCHOR, _URL_SCHEME_ANCHOR,
			_LWP_ANCHOR, _ARIA2C_ANCHOR, _AUTH_OPTION_ANCHOR, _HTTP_SHORT_AUTH_ANCHOR, _HTTPS_SHORT_AUTH_ANCHOR
		];

		return texts.Select(text => new PreselectionAnchor { Text = text, Sensitivity = CaseSensitivity.Insensitive }).ToFrozenSet();
	}
}
