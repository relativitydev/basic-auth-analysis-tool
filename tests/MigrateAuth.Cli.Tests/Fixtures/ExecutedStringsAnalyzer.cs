using System.Collections.Frozen;
using System.Text.RegularExpressions;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Tests.Lexical;

namespace MigrateAuth.Cli.Tests.Fixtures;

/// <summary>The expressions of the executed-strings analyzer, generated at build time.</summary>
internal static partial class ExecutedStringsExpressions
{
	/// <summary>A command with a user-and-password pair written as a bare word.</summary>
	[GeneratedRegex(
		@"\b(?<construct>curl)[ \t]+-u[ \t]+(?<credential>[A-Za-z0-9_.]+:[A-Za-z0-9_.]+)(?=[ \t'""`)}]|$)",
		RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture | RegexOptions.Multiline,
		matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex BareWordPair();
}

/// <summary>
/// A TEST-LOCAL ANALYZER, BUILT ONLY BY THE TESTS THAT NEED IT: the shell-shaped profile with the executed-string
/// context declared, and two rules over the same construct, one that reads a bare word as a literal credential and one
/// that does not, both at the High level. They let the level a construct takes, and the literal-credential signal, be
/// read after a real scan and a real recognition. Nothing under <c>src/</c> names it.
/// </summary>
public sealed class ExecutedStringsAnalyzer : ILanguageAnalyzer
{
	/// <summary>The rule that reads a bare word as a literal credential.</summary>
	public const string BARE_WORD_PATTERN_CLASS = "ESX-001";

	/// <summary>The rule that declares the credential position the ordinary way.</summary>
	public const string ORDINARY_PATTERN_CLASS = "ESX-002";

	private const string _CONSTRUCT_CAPTURE_GROUP = "construct";
	private const string _CREDENTIAL_CAPTURE_GROUP = "credential";

	private readonly HereDocumentExemplarAnalyzer _exemplar = new();

	public ExecutedStringsAnalyzer(bool declaresExecutedStrings)
	{
		LexicalProfile = declaresExecutedStrings ? ShellShapedProfile.BuildWithExecutedStrings() : ShellShapedProfile.Build();
		DetectionRulesByPatternClass = new Dictionary<string, DetectionRule>(StringComparer.Ordinal)
		{
			[BARE_WORD_PATTERN_CLASS] = BuildRule(CredentialPosition.AtBareWordOrLiteralCaptureGroup(_CREDENTIAL_CAPTURE_GROUP), "Bare-word pair"),
			[ORDINARY_PATTERN_CLASS] = BuildRule(CredentialPosition.AtCaptureGroup(_CREDENTIAL_CAPTURE_GROUP), "Ordinary pair")
		}.ToFrozenDictionary(StringComparer.Ordinal);
	}

	public LexicalProfile LexicalProfile { get; }

	public LanguageIdentification LanguageIdentification => _exemplar.LanguageIdentification;

	public FrozenDictionary<string, DetectionRule> DetectionRulesByPatternClass { get; }

	public LanguageShapedDemotionSignals DemotionSignals => _exemplar.DemotionSignals;

	public CandidateSelectionAndRecommendation CandidateSelectionAndRecommendation => _exemplar.CandidateSelectionAndRecommendation;

	public FrozenSet<SubsumptionRule> SubsumptionRelation => _exemplar.SubsumptionRelation;

	private static DetectionRule BuildRule(CredentialPosition credentialPosition, string displayName)
	{
		return new DetectionRule
		{
			Expression = ExecutedStringsExpressions.BareWordPair(),
			AnchorPosition = MatchAnchor.AtCaptureGroup(_CONSTRUCT_CAPTURE_GROUP),
			BaseLevel = ConfidenceLevel.High,
			SelfSufficiency = SelfSufficiency.DeclaresSchemeAndCredentialCarrier,
			SymbolicVerdictInterpretation = SymbolicVerdictInterpretation.NotSubjectToSymbolicResolution,
			CredentialPosition = credentialPosition,
			DestinationCertainty = DestinationCertainty.Unknown,
			DisplayName = displayName
		};
	}
}
