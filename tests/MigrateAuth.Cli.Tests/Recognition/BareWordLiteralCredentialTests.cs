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
/// THE BARE-WORD READING OF THE LITERAL-CREDENTIAL SIGNAL, over the test-local exemplar's two rules: one
/// declares the reading and one declares the credential position the ordinary way.
///
/// The engine trusts the EXPRESSION to keep every expansion out of the credential group, so the
/// exemplar's expression does, and an expansion therefore recognizes nothing at all. What the engine
/// itself decides, and what these cases pin, is the zones: a bare word in code is literal under the
/// declared reading and not otherwise, a capture of delimiters only is never literal, and a position
/// that is a comment or is untrustworthy is never literal.
/// </summary>
public sealed class BareWordLiteralCredentialTests
{
	private const string _CREDENTIAL_CAPTURE_GROUP = "credential";

	[Fact]
	public void ABareLiteralWordIsLiteralUnderTheDeclaredReading()
	{
		RecognizedConstruct construct = Single("setpass hunter2\n", HereDocumentExemplarAnalyzer.BARE_WORD_PATTERN_CLASS);

		Assert.True(construct.LiteralCredentialSignal);
	}

	[Fact]
	public void ABareLiteralWordIsNotLiteralWhereTheRuleDoesNotDeclareTheReading()
	{
		RecognizedConstruct construct = Single("setplain hunter2\n", HereDocumentExemplarAnalyzer.ORDINARY_PATTERN_CLASS);

		Assert.False(construct.LiteralCredentialSignal);
	}

	[Theory]
	[InlineData("setpass 'hunter2'\n", HereDocumentExemplarAnalyzer.BARE_WORD_PATTERN_CLASS)]
	[InlineData("setplain 'hunter2'\n", HereDocumentExemplarAnalyzer.ORDINARY_PATTERN_CLASS)]
	public void AQuotedLiteralIsLiteralWhetherOrNotTheReadingIsDeclared(string source, string patternClass)
	{
		Assert.True(Single(source, patternClass).LiteralCredentialSignal);
	}

	[Theory]
	[InlineData("setpass ''\n", HereDocumentExemplarAnalyzer.BARE_WORD_PATTERN_CLASS)]
	[InlineData("setplain ''\n", HereDocumentExemplarAnalyzer.ORDINARY_PATTERN_CLASS)]
	public void ACaptureOfDelimitersOnlyIsNeverLiteral(string source, string patternClass)
	{
		Assert.False(Single(source, patternClass).LiteralCredentialSignal);
	}

	/// <summary>The expression refuses every expansion, so none of these recognizes a credential and none can signal a literal.</summary>
	[Theory]
	[InlineData("setpass $P\n")]
	[InlineData("setpass ${P}\n")]
	[InlineData("setpass $(cat f)\n")]
	[InlineData("setpass `cat f`\n")]
	[InlineData("setpass \\$P\n")]
	[InlineData("setpass u:'p'$x\n")]
	[InlineData("setpass {a,b}\n")]
	[InlineData("setpass *.txt\n")]
	[InlineData("setpass ~/word\n")]
	[InlineData("setpass \"a$x\"\n")]
	public void AnExpansionIsNeverReadAsALiteralCredential(string source)
	{
		ImmutableArray<RecognizedConstruct> constructs = Recognize(source);

		Assert.DoesNotContain(constructs, construct => construct.LiteralCredentialSignal);
		Assert.Empty(constructs);
	}

	[Fact]
	public void ABareWordInsideAHereDocumentBodyIsLiteralUnderBothReadings()
	{
		const string source = "cat <<EOF\nsetpass hunter2\nsetplain hunter2\nEOF\n";

		Assert.True(Single(source, HereDocumentExemplarAnalyzer.BARE_WORD_PATTERN_CLASS).LiteralCredentialSignal);
		Assert.True(Single(source, HereDocumentExemplarAnalyzer.ORDINARY_PATTERN_CLASS).LiteralCredentialSignal);
	}

	[Fact]
	public void ABareWordInACommentIsNeverLiteralEvenUnderTheDeclaredReading()
	{
		RecognizedConstruct construct = Single("# setpass hunter2\n", HereDocumentExemplarAnalyzer.BARE_WORD_PATTERN_CLASS);

		Assert.False(construct.LiteralCredentialSignal);
	}

	[Fact]
	public void AnUntrustworthyPositionIsNeverLiteralEvenUnderTheDeclaredReading()
	{
		const string source = "cat <<EOF\nsetpass hunter2\n";

		RecognizedConstruct construct = Single(source, HereDocumentExemplarAnalyzer.BARE_WORD_PATTERN_CLASS);

		Assert.False(construct.LiteralCredentialSignal);
	}

	/// <summary>
	/// The engine trusts the expression, so a permissive one is the case the exemplar cannot show: a
	/// capture that reaches a comment, or that stands where the scanner does not vouch for the text,
	/// is never literal even under the declared reading.
	/// </summary>
	[Theory]
	[InlineData("setpass hunter2 # note\n")]
	[InlineData("setpass $HOME # note\n")]
	[InlineData("cat <<EOF\nsetpass $(x)\n")]
	public void APermissiveExpressionThatCapturesACommentOrAnUntrustworthyPositionIsNeverLiteral(string source)
	{
		ImmutableArray<RecognizedConstruct> constructs = RecognizeWith(new PermissiveBareWordAnalyzer(), source);

		RecognizedConstruct construct = Assert.Single(constructs);
		Assert.False(construct.LiteralCredentialSignal);
	}

	[Fact]
	public void APermissiveExpressionThatCapturesACodeZoneBareWordIsLiteralUnderTheDeclaredReading()
	{
		ImmutableArray<RecognizedConstruct> constructs = RecognizeWith(new PermissiveBareWordAnalyzer(), "setpass hunter2\n");

		Assert.True(Assert.Single(constructs).LiteralCredentialSignal);
	}

	[Fact]
	public void TheDeclarationNamesItsGroupAndTheOtherFormsDoNotDeclareTheReading()
	{
		CredentialPosition bareWord = CredentialPosition.AtBareWordOrLiteralCaptureGroup(_CREDENTIAL_CAPTURE_GROUP);
		CredentialPosition ordinary = CredentialPosition.AtCaptureGroup(_CREDENTIAL_CAPTURE_GROUP);

		Assert.True(bareWord.IsDeclared);
		Assert.True(bareWord.ReadsABareWordAsLiteral);
		Assert.Equal(_CREDENTIAL_CAPTURE_GROUP, bareWord.CaptureGroupName);
		Assert.False(ordinary.ReadsABareWordAsLiteral);
		Assert.False(CredentialPosition.None().ReadsABareWordAsLiteral);
		Assert.NotEqual(ordinary, bareWord);
		Assert.Throws<ArgumentException>(() => CredentialPosition.AtBareWordOrLiteralCaptureGroup(" "));
	}

	private static RecognizedConstruct Single(string source, string patternClass)
	{
		return Assert.Single(Recognize(source), construct => construct.PatternClassIdentifier == patternClass);
	}

	private static ImmutableArray<RecognizedConstruct> Recognize(string source)
	{
		return RecognizeWith(new HereDocumentExemplarAnalyzer(), source);
	}

	private static ImmutableArray<RecognizedConstruct> RecognizeWith(ILanguageAnalyzer analyzer, string source)
	{
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

/// <summary>The expression of the permissive rule: it captures the rest of the line, so it keeps nothing out of the credential group.</summary>
internal static partial class PermissiveBareWordExpressions
{
	[GeneratedRegex(
		@"\b(?<construct>setpass)[ \t]+(?<credential>[^\n]+)",
		RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture | RegexOptions.Multiline,
		matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex RestOfLine();
}

/// <summary>
/// The exemplar with its bare-word rule's expression swapped for the permissive one, so the engine's
/// own zone reading is what decides. Everything else is the exemplar's own.
/// </summary>
internal sealed class PermissiveBareWordAnalyzer : ILanguageAnalyzer
{
	private readonly HereDocumentExemplarAnalyzer _inner = new();

	public PermissiveBareWordAnalyzer()
	{
		DetectionRule original = _inner.DetectionRulesByPatternClass[HereDocumentExemplarAnalyzer.BARE_WORD_PATTERN_CLASS];
		DetectionRule permissive = new()
		{
			Expression = PermissiveBareWordExpressions.RestOfLine(),
			AnchorPosition = original.AnchorPosition,
			BaseLevel = original.BaseLevel,
			SelfSufficiency = original.SelfSufficiency,
			SymbolicVerdictInterpretation = original.SymbolicVerdictInterpretation,
			CredentialPosition = original.CredentialPosition,
			DestinationCertainty = original.DestinationCertainty,
			DisplayName = original.DisplayName
		};

		DetectionRulesByPatternClass = new Dictionary<string, DetectionRule>(StringComparer.Ordinal)
		{
			[HereDocumentExemplarAnalyzer.BARE_WORD_PATTERN_CLASS] = permissive
		}.ToFrozenDictionary(StringComparer.Ordinal);
	}

	public LexicalProfile LexicalProfile => _inner.LexicalProfile;

	public LanguageIdentification LanguageIdentification => _inner.LanguageIdentification;

	public FrozenDictionary<string, DetectionRule> DetectionRulesByPatternClass { get; }

	public LanguageShapedDemotionSignals DemotionSignals => _inner.DemotionSignals;

	public CandidateSelectionAndRecommendation CandidateSelectionAndRecommendation => _inner.CandidateSelectionAndRecommendation;

	public FrozenSet<SubsumptionRule> SubsumptionRelation => _inner.SubsumptionRelation;
}
