using System.Collections.Frozen;

namespace MigrateAuth.Cli.Catalog;

/// <summary>
/// The language analyzer contract: the type surface through which a language catalog
/// is expressed to the engine. It has exactly six required top-level elements and one optional seventh,
/// one per member below, and nothing else.
///
/// 1. <see cref="LexicalProfile"/>
/// 2. <see cref="LanguageIdentification"/>
/// 3. <see cref="DetectionRulesByPatternClass"/>, whose rule element carries eight fields
/// 4. <see cref="DemotionSignals"/>
/// 5. <see cref="CandidateSelectionAndRecommendation"/>
/// 6. <see cref="SubsumptionRelation"/>
/// 7. <see cref="ConstructPrecedence"/>, optional, absent for every shipped profile
///
/// The design also requires that an analyzer never orders, numbers or writes. That is enforced by
/// the shape of this type rather than by a comment forbidding it:
///
/// - It has no methods. Every member is a read-only property returning data, so there is no
///   point at which an analyzer is invoked with anything to act on.
/// - It is handed nothing. No member accepts a file, a finding, a writer, a stream or the
///   engine itself, so an analyzer has no sink to write to and nothing to write about.
/// - Its collections are unordered by type - <see cref="FrozenSet{T}"/> and
///   <see cref="FrozenDictionary{TKey, TValue}"/> - so an analyzer's ordering of rules,
///   anchors, extensions or subsumptions cannot survive into the engine. The one sequence a
///   profile accepts is discarded on construction and reordered by the engine.
/// - It carries no integer identifier, ordinal or sequence anywhere. Positions inside a match
///   are named capture groups, and only the engine turns a name into a number.
/// </summary>
public interface ILanguageAnalyzer
{
	/// <summary>Element 1. The lexical profile, including its constant declaration expression.</summary>
	LexicalProfile LexicalProfile { get; }

	/// <summary>Element 2. Extensions, the first-line rule, and the language name derivation.</summary>
	LanguageIdentification LanguageIdentification { get; }

	/// <summary>Element 3. The detection rule set, keyed by pattern class identifier.</summary>
	FrozenDictionary<string, DetectionRule> DetectionRulesByPatternClass { get; }

	/// <summary>Element 4. File demotion signals, language-shaped only.</summary>
	LanguageShapedDemotionSignals DemotionSignals { get; }

	/// <summary>
	/// Element 5. Preselection anchors, the template provider indexed by class, and
	/// this language's optional generic-fallback text for the generic recommendation route.
	/// </summary>
	CandidateSelectionAndRecommendation CandidateSelectionAndRecommendation { get; }

	/// <summary>Element 6. The subsumption relation, as declared triples.</summary>
	FrozenSet<SubsumptionRule> SubsumptionRelation { get; }

	/// <summary>
	/// Element 7, optional. How several classes reading one statement become one finding. Null, the default, declares
	/// none.
	/// </summary>
	ConstructPrecedence? ConstructPrecedence => null;
}
