using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Text.RegularExpressions;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Emission;
using MigrateAuth.Cli.Lexical;
using MigrateAuth.Cli.Reading;
using MigrateAuth.Cli.Recognition;
using MigrateAuth.Cli.Tests.Fixtures;

using Xunit;

namespace MigrateAuth.Cli.Tests.Recognition;

/// <summary>
/// THE FIFTH LITERAL-CREDENTIAL FORM: a credential position whose captured text is a literal
/// pair (<c>user:password</c>) or a literal Basic token, with no expansion. The signal is read from the zone
/// by the bare-word reading, so these cases pin the form on a TEST-LOCAL profile: no shipped profile declares
/// the position for a pair or a token.
///
/// Three things are pinned. A literal pair and a literal Basic token, written in every way the exemplar's
/// lexical profile separates, signal true. Every expansion signals false, because the expression of a rule
/// that declares the reading keeps expansions out of the credential group and so recognizes nothing at all.
/// And the token is never decoded: whether its text is valid Base64 decides nothing, the zone does
/// (the signal never reads the value).
///
/// Every source below is invented, with placeholder names throughout.
/// </summary>
public sealed class LiteralPairAndBasicTokenCredentialTests
{
	private const string _CREDENTIAL_CAPTURE_GROUP = "credential";
	private const string _POWERSHELL_ENCODED_PAIR_PATTERN_CLASS = "PS-010";

	[Theory]
	[InlineData("sendpair user:secret\n")]
	[InlineData("sendpair 'user:secret'\n")]
	[InlineData("sendpair \"user:secret\"\n")]
	[InlineData("sendpair first.last@host:s3cret-word\n")]
	[InlineData("sendpair 'first last:secret word'\n")]
	public void ALiteralPairIsLiteral(string source)
	{
		RecognizedConstruct construct = Single(source, LiteralPairProfileAnalyzer.PAIR_PATTERN_CLASS);

		Assert.True(construct.LiteralCredentialSignal);
	}

	[Theory]
	[InlineData("echo -n user:secret | base64\n")]
	[InlineData("echo -n 'user:secret' | base64\n")]
	[InlineData("echo -n \"user:secret\" | base64\n")]
	[InlineData("printf %s user:secret | base64\n")]
	public void ALiteralPairInAnEncodingPipelineIsLiteral(string source)
	{
		RecognizedConstruct construct = Single(source, LiteralPairProfileAnalyzer.PIPELINE_PATTERN_CLASS);

		Assert.True(construct.LiteralCredentialSignal);
	}

	[Theory]
	[InlineData("Authorization: Basic dXNlcjpzZWNyZXQ=\n")]
	[InlineData("send 'Authorization: Basic dXNlcjpzZWNyZXQ='\n")]
	[InlineData("send \"Authorization: Basic dXNlcjpzZWNyZXQ=\"\n")]
	public void ALiteralBasicTokenInAHeaderIsLiteral(string source)
	{
		RecognizedConstruct construct = Single(source, LiteralPairProfileAnalyzer.HEADER_TOKEN_PATTERN_CLASS);

		Assert.True(construct.LiteralCredentialSignal);
	}

	[Theory]
	[InlineData("AUTH='Basic dXNlcjpzZWNyZXQ='\n")]
	[InlineData("AUTH=\"Basic dXNlcjpzZWNyZXQ=\"\n")]
	public void ALiteralBasicTokenAssignedToAVariableIsLiteral(string source)
	{
		RecognizedConstruct construct = Single(source, LiteralPairProfileAnalyzer.ASSIGNED_TOKEN_PATTERN_CLASS);

		Assert.True(construct.LiteralCredentialSignal);
	}

	/// <summary>The token is never decoded: a text that no Base64 decoder would accept signals exactly as a valid one does, because the zone decides.</summary>
	[Theory]
	[InlineData("AUTH='Basic a'\n")]
	[InlineData("AUTH='Basic abc'\n")]
	[InlineData("AUTH='Basic ====='\n")]
	[InlineData("AUTH='Basic dXNlcjpzZWNyZXQ='\n")]
	public void TheTokenIsLiteralWhetherOrNotItDecodes(string source)
	{
		Assert.True(Single(source, LiteralPairProfileAnalyzer.ASSIGNED_TOKEN_PATTERN_CLASS).LiteralCredentialSignal);
	}

	/// <summary>Each expansion keeps the credential group from matching, so nothing is recognized and nothing can signal a literal.</summary>
	[Theory]
	[InlineData("sendpair $U:$P\n")]
	[InlineData("sendpair user:$P\n")]
	[InlineData("sendpair $U:secret\n")]
	[InlineData("sendpair \"$U:$P\"\n")]
	[InlineData("sendpair \"user:${P}\"\n")]
	[InlineData("sendpair \"user:$(cat file)\"\n")]
	[InlineData("sendpair user:`cat file`\n")]
	[InlineData("sendpair 'user':$P\n")]
	[InlineData("sendpair user:\\$P\n")]
	[InlineData("echo -n $U:$P | base64\n")]
	[InlineData("echo -n \"$U:$P\" | base64\n")]
	[InlineData("echo -n \"${U}:secret\" | base64\n")]
	[InlineData("Authorization: Basic $T\n")]
	[InlineData("send \"Authorization: Basic $T\"\n")]
	[InlineData("send \"Authorization: Basic ${T}\"\n")]
	[InlineData("send \"Authorization: Basic $(make-token)\"\n")]
	[InlineData("AUTH=\"Basic $T\"\n")]
	[InlineData("AUTH=\"Basic ${T}\"\n")]
	[InlineData("AUTH=\"Basic `make-token`\"\n")]
	public void AnExpansionIsNeverReadAsALiteralPairOrToken(string source)
	{
		ImmutableArray<RecognizedConstruct> constructs = Recognize(source);

		Assert.DoesNotContain(constructs, construct => construct.LiteralCredentialSignal);
		Assert.Empty(constructs);
	}

	/// <summary>A pair with an empty half is not a literal pair, and a token with nothing after the scheme is not a token.</summary>
	[Theory]
	[InlineData("sendpair user:\n")]
	[InlineData("sendpair :secret\n")]
	[InlineData("sendpair ''\n")]
	[InlineData("sendpair 'user:'\n")]
	[InlineData("Authorization: Basic \n")]
	[InlineData("AUTH='Basic '\n")]
	public void AnEmptyHalfOrAnEmptyTokenIsNeverReadAsALiteral(string source)
	{
		Assert.Empty(Recognize(source));
	}

	[Theory]
	[InlineData("# sendpair user:secret\n", LiteralPairProfileAnalyzer.PAIR_PATTERN_CLASS)]
	[InlineData("# AUTH='Basic dXNlcjpzZWNyZXQ='\n", LiteralPairProfileAnalyzer.ASSIGNED_TOKEN_PATTERN_CLASS)]
	public void APairOrATokenInACommentIsNeverLiteral(string source, string patternClass)
	{
		Assert.False(Single(source, patternClass).LiteralCredentialSignal);
	}

	[Fact]
	public void APairInAnUntrustworthyPositionIsNeverLiteral()
	{
		const string source = "cat <<EOF\nsendpair user:secret\n";

		Assert.False(Single(source, LiteralPairProfileAnalyzer.PAIR_PATTERN_CLASS).LiteralCredentialSignal);
	}

	[Fact]
	public void APairInAHereDocumentBodyIsLiteral()
	{
		const string source = "cat <<EOF\nsendpair user:secret\nEOF\n";

		Assert.True(Single(source, LiteralPairProfileAnalyzer.PAIR_PATTERN_CLASS).LiteralCredentialSignal);
	}

	/// <summary>
	/// A rule that does not declare the reading reads the pair as usual: written as a bare word it is
	/// code and does not signal, and written in quotes it signals.
	/// </summary>
	[Theory]
	[InlineData("setplainpair user:secret\n", false)]
	[InlineData("setplainpair 'user:secret'\n", true)]
	[InlineData("setplainpair \"user:secret\"\n", true)]
	public void ARuleThatDoesNotDeclareTheReadingIsUnchanged(string source, bool expected)
	{
		RecognizedConstruct construct = Single(source, LiteralPairProfileAnalyzer.UNDECLARED_PAIR_PATTERN_CLASS);

		Assert.Equal(expected, construct.LiteralCredentialSignal);
	}

	[Fact]
	public void EveryRuleOfTheProfileThatCarriesAPairOrATokenDeclaresTheReadingExceptTheControl()
	{
		LiteralPairProfileAnalyzer analyzer = new();

		foreach (string patternClass in new[]
		{
			LiteralPairProfileAnalyzer.PAIR_PATTERN_CLASS,
			LiteralPairProfileAnalyzer.PIPELINE_PATTERN_CLASS,
			LiteralPairProfileAnalyzer.HEADER_TOKEN_PATTERN_CLASS,
			LiteralPairProfileAnalyzer.ASSIGNED_TOKEN_PATTERN_CLASS
		})
		{
			CredentialPosition position = analyzer.DetectionRulesByPatternClass[patternClass].CredentialPosition;

			Assert.True(position.IsDeclared);
			Assert.True(position.ReadsABareWordAsLiteral);
			Assert.Equal(_CREDENTIAL_CAPTURE_GROUP, position.CaptureGroupName);
		}

		Assert.False(analyzer.DetectionRulesByPatternClass[LiteralPairProfileAnalyzer.UNDECLARED_PAIR_PATTERN_CLASS].CredentialPosition.ReadsABareWordAsLiteral);
	}

	/// <summary>
	/// PS-010 is the same fact read by the ordinary position: its literal pair sits in a quoted literal,
	/// so the four-clause rule already signals, and an interpolated pair does not. Nothing about the
	/// fifth form changes a PowerShell finding.
	/// </summary>
	[Theory]
	[InlineData("$pair = 'someone:secret'\n$e = [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes($pair))", true)]
	[InlineData("$e = [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes('someone:secret'))", true)]
	[InlineData("$e = [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes(\"$($u):$($p)\"))", false)]
	public void ThePowerShellLiteralPairReadsTheSameFormThroughTheOrdinaryPosition(string source, bool expected)
	{
		ImmutableArray<RecognizedConstruct> constructs = new PowerShellRecognitionHarness().Recognize(source);

		RecognizedConstruct construct = Assert.Single(constructs, candidate => candidate.PatternClassIdentifier == _POWERSHELL_ENCODED_PAIR_PATTERN_CLASS);
		Assert.Equal(expected, construct.LiteralCredentialSignal);
	}

	private static RecognizedConstruct Single(string source, string patternClass)
	{
		return Assert.Single(Recognize(source), construct => construct.PatternClassIdentifier == patternClass);
	}

	private static ImmutableArray<RecognizedConstruct> Recognize(string source)
	{
		ILanguageAnalyzer analyzer = new LiteralPairProfileAnalyzer();
		RecognitionHarness harness = new();
		string normalizedText = TextNormalization.Normalize(source);
		RepositoryRelativePath path = harness.PathOf("source/script.hdx");
		ZoneMask mask = new FileLexicalScanner(harness.EmissionPoint).Scan(analyzer.LexicalProfile, normalizedText, path);
		FileRecognitionContext context = new()
		{
			Analyzer = analyzer,
			NormalizedText = normalizedText,
			Mask = mask,
			Path = path
		};

		return new RecognitionEngine(harness.EmissionPoint, new FrozenTimeProvider()).Recognize(context).Constructs;
	}
}

/// <summary>
/// The expressions of the test-local profile. Every credential group excludes <c>$</c>, the backtick and the
/// backslash, which is the guarantee the bare-word reading trusts the expression to give, and a pair needs two
/// non-empty halves.
/// </summary>
internal static partial class LiteralPairProfileExpressions
{
	[GeneratedRegex(
		@"\b(?<construct>sendpair)[ \t]+(?<credential>'[^'\n$`\\:]+:[^'\n$`\\]+'|""[^""\n$`\\:]+:[^""\n$`\\]+""|[A-Za-z0-9_.@-]+:[A-Za-z0-9_.@-]+)(?=[ \t]|$)",
		RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture | RegexOptions.Multiline,
		matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex Pair();

	[GeneratedRegex(
		@"\b(?<construct>setplainpair)[ \t]+(?<credential>'[^'\n$`\\:]+:[^'\n$`\\]+'|""[^""\n$`\\:]+:[^""\n$`\\]+""|[A-Za-z0-9_.@-]+:[A-Za-z0-9_.@-]+)(?=[ \t]|$)",
		RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture | RegexOptions.Multiline,
		matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex UndeclaredPair();

	[GeneratedRegex(
		@"\b(?<construct>echo|printf)[ \t]+(?:-n[ \t]+)?(?:%s[ \t]+)?(?<credential>'[^'\n$`\\:]+:[^'\n$`\\]+'|""[^""\n$`\\:]+:[^""\n$`\\]+""|[A-Za-z0-9_.@-]+:[A-Za-z0-9_.@-]+)[ \t]*\|[ \t]*base64",
		RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture | RegexOptions.Multiline,
		matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex PairInAPipeline();

	[GeneratedRegex(
		@"\b(?<construct>Authorization):[ \t]*Basic[ \t]+(?<credential>[A-Za-z0-9+/=_-]+)(?=['""]|[ \t]|$)",
		RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture | RegexOptions.Multiline,
		matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex TokenInAHeader();

	[GeneratedRegex(
		@"\b(?<construct>AUTH)=(?<quote>['""])Basic[ \t]+(?<credential>[A-Za-z0-9+/=_-]+)\k<quote>",
		RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture | RegexOptions.Multiline,
		matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex TokenAssignedToAVariable();
}

/// <summary>
/// The exemplar's lexical profile with five rules of its own: four that declare the bare-word reading for a
/// literal pair or a literal Basic token, and a control that carries the same pair under the ordinary position.
/// </summary>
internal sealed class LiteralPairProfileAnalyzer : ILanguageAnalyzer
{
	public const string PAIR_PATTERN_CLASS = "LPT-001";
	public const string PIPELINE_PATTERN_CLASS = "LPT-002";
	public const string HEADER_TOKEN_PATTERN_CLASS = "LPT-003";
	public const string ASSIGNED_TOKEN_PATTERN_CLASS = "LPT-004";
	public const string UNDECLARED_PAIR_PATTERN_CLASS = "LPT-005";

	private const string _CREDENTIAL_CAPTURE_GROUP = "credential";
	private const string _CONSTRUCT_CAPTURE_GROUP = "construct";

	private readonly HereDocumentExemplarAnalyzer _inner = new();

	public LiteralPairProfileAnalyzer()
	{
		DetectionRulesByPatternClass = new Dictionary<string, DetectionRule>(StringComparer.Ordinal)
		{
			[PAIR_PATTERN_CLASS] = Rule(LiteralPairProfileExpressions.Pair(), CredentialPosition.AtBareWordOrLiteralCaptureGroup(_CREDENTIAL_CAPTURE_GROUP)),
			[PIPELINE_PATTERN_CLASS] = Rule(LiteralPairProfileExpressions.PairInAPipeline(), CredentialPosition.AtBareWordOrLiteralCaptureGroup(_CREDENTIAL_CAPTURE_GROUP)),
			[HEADER_TOKEN_PATTERN_CLASS] = Rule(LiteralPairProfileExpressions.TokenInAHeader(), CredentialPosition.AtBareWordOrLiteralCaptureGroup(_CREDENTIAL_CAPTURE_GROUP)),
			[ASSIGNED_TOKEN_PATTERN_CLASS] = Rule(LiteralPairProfileExpressions.TokenAssignedToAVariable(), CredentialPosition.AtBareWordOrLiteralCaptureGroup(_CREDENTIAL_CAPTURE_GROUP)),
			[UNDECLARED_PAIR_PATTERN_CLASS] = Rule(LiteralPairProfileExpressions.UndeclaredPair(), CredentialPosition.AtCaptureGroup(_CREDENTIAL_CAPTURE_GROUP))
		}.ToFrozenDictionary(StringComparer.Ordinal);
	}

	public LexicalProfile LexicalProfile => _inner.LexicalProfile;

	public LanguageIdentification LanguageIdentification => _inner.LanguageIdentification;

	public FrozenDictionary<string, DetectionRule> DetectionRulesByPatternClass { get; }

	public LanguageShapedDemotionSignals DemotionSignals => _inner.DemotionSignals;

	public CandidateSelectionAndRecommendation CandidateSelectionAndRecommendation => _inner.CandidateSelectionAndRecommendation;

	public FrozenSet<SubsumptionRule> SubsumptionRelation => _inner.SubsumptionRelation;

	private static DetectionRule Rule(Regex expression, CredentialPosition credentialPosition)
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
			DisplayName = "Literal pair or token fixture"
		};

		return rule;
	}
}
