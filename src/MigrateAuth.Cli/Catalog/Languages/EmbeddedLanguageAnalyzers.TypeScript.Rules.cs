using System.Collections.Frozen;
using System.Text.RegularExpressions;

namespace MigrateAuth.Cli.Catalog.Languages;

/// <summary>
/// The detection rules of the `TypeScript` analyzer: the nineteen inherited `JavaScript` classes `JS-001` to `JS-019`, taken by reference from <see cref="JavaScriptRuleDeclarations"/> and filtered by an
/// explicit allow-list, registered under their `JS-` keys so a `TypeScript` finding of `JS-004` carries `PatternClass = JS-004`; and the two classes that exist for `TypeScript` alone.
///
/// THE ALLOW-LIST. The builders of `JavaScript` are called once and their result is filtered, so a class added to `JavaScript` later is not inherited until this list names it. Seven of the nineteen are Low
/// only (`JS-007`, `JS-009`, `JS-011`, `JS-014`, `JS-016`, `JS-018` and `JS-019`).
///
/// THE TWO CLASSES. `TS-001` (Medium, <see cref="SelfSufficiency.RequiresCallPathToBeEstablished"/>) reads an object literal whose declared or asserted type names a cited configuration or credentials type of
/// the client and whose `auth` object holds a literal user and password; it anchors on the `auth` key, as `JS-001` and `JS-011` do, so the subsumption triples can hold. `TS-003` (High,
/// <see cref="SelfSufficiency.DeclaresSchemeAndCredentialCarrier"/>) reads the same object in the arguments of a documented `HttpService` method on a receiver whose name contains `http`, a generic list allowed by
/// construction.
///
/// SUBSUMPTION is the `JavaScript` relation plus four triples, each at identical anchors: `JS-001` over `TS-001`, `TS-001` over `JS-011`, `TS-003` over `JS-011` and `TS-003` over `TS-001`.
/// </summary>
internal static class TypeScriptRuleDeclarations
{
	private const string _TS_001 = "TS-001";
	private const string _TS_003 = "TS-003";

	private const string _CONSTRUCT_CAPTURE_GROUP = "construct";

	private const string _JS_001 = "JS-001";
	private const string _JS_011 = "JS-011";

	/// <summary>The inherited class identifiers, in the catalog's order.</summary>
	internal static FrozenSet<string> InheritedClassAllowList { get; } = new[]
	{
		"JS-001", "JS-002", "JS-003", "JS-004", "JS-005", "JS-006", "JS-007", "JS-008", "JS-009",
		"JS-010", "JS-011", "JS-012", "JS-013", "JS-014", "JS-015", "JS-016", "JS-017", "JS-018", "JS-019"
	}.ToFrozenSet(StringComparer.Ordinal);

	internal static FrozenDictionary<string, DetectionRule> BuildDetectionRules()
	{
		Dictionary<string, DetectionRule> rules = JavaScriptRuleDeclarations.BuildDetectionRules()
			.Where(entry => InheritedClassAllowList.Contains(entry.Key))
			.ToDictionary(entry => entry.Key, entry => entry.Value, StringComparer.Ordinal);

		rules[_TS_001] = BuildTypeScriptRule(
			JavaScriptPatternExpressions.TypedCredentialObject(),
			"TypedBasicCredentialAnnotation",
			ConfidenceLevel.Medium,
			SelfSufficiency.RequiresCallPathToBeEstablished);
		rules[_TS_003] = BuildTypeScriptRule(
			JavaScriptPatternExpressions.InjectedHttpServiceAuthObject(),
			"NestHttpServiceAuthObject",
			ConfidenceLevel.High,
			SelfSufficiency.DeclaresSchemeAndCredentialCarrier);

		return rules.ToFrozenDictionary(StringComparer.Ordinal);
	}

	/// <summary>A script has no type that names Relativity, so every destination is unknown. The literal password member is the credential position, optional and whole-literal only.</summary>
	private static DetectionRule BuildTypeScriptRule(Regex expression, string displayName, ConfidenceLevel baseLevel, SelfSufficiency selfSufficiency)
	{
		DetectionRule rule = new()
		{
			Expression = expression,
			AnchorPosition = MatchAnchor.AtCaptureGroup(_CONSTRUCT_CAPTURE_GROUP),
			BaseLevel = baseLevel,
			SelfSufficiency = selfSufficiency,
			SymbolicVerdictInterpretation = SymbolicVerdictInterpretation.NotSubjectToSymbolicResolution,
			CredentialPosition = CredentialPosition.AtCaptureGroup(JavaScriptRuleDeclarations.CREDENTIAL_CAPTURE_GROUP),
			DestinationCertainty = DestinationCertainty.Unknown,
			DisplayName = displayName
		};

		return rule;
	}

	/// <summary>The `JavaScript` relation restricted to the allow-list, and the four triples of `TypeScript`.</summary>
	internal static FrozenSet<SubsumptionRule> BuildSubsumptionRelation()
	{
		IEnumerable<SubsumptionRule> inherited = JavaScriptRuleDeclarations.BuildSubsumptionRelation()
			.Where(rule => InheritedClassAllowList.Contains(rule.SubsumingPatternClassIdentifier) && InheritedClassAllowList.Contains(rule.SubsumedPatternClassIdentifier));

		IEnumerable<SubsumptionRule> added =
		[
			Subsume(_JS_001, _TS_001),
			Subsume(_TS_001, _JS_011),
			Subsume(_TS_003, _JS_011),
			Subsume(_TS_003, _TS_001)
		];

		return inherited.Concat(added).ToFrozenSet();
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

	/// <summary>The `JavaScript` anchors, by reference: `TypeScript` adds none, because both of its classes anchor on the key `auth`, which is already one.</summary>
	internal static FrozenSet<PreselectionAnchor> BuildPreselectionAnchors()
	{
		return JavaScriptRuleDeclarations.BuildPreselectionAnchors();
	}
}
