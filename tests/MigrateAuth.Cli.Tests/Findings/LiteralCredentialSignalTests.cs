using System.Collections.Immutable;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Findings;
using MigrateAuth.Cli.Lexical;
using MigrateAuth.Cli.Recognition;
using MigrateAuth.Cli.Tests.Fixtures;
using MigrateAuth.Cli.Tests.Recognition;

using Xunit;

namespace MigrateAuth.Cli.Tests.Findings;

/// <summary>
/// THE LITERAL-CREDENTIAL SIGNAL, THROUGH REAL RECOGNITION RATHER THAN A SYNTHETIC CONSTRUCT.
///
/// It is the one field of a finding that CANNOT be built by hand and still mean anything: it is
/// read off the zone at an offset resolved from a named capture group of a live match, so a
/// fixture that set the boolean directly would be asserting the value it had just chosen. These
/// tests therefore run the reference analyzer over source text and read the signal out the far
/// end - which is also what makes them the only tests here that exercise the mask.
///
/// THE SIGNAL IS COMPUTED FROM THE ZONE AND NEVER FROM THE VALUE, which is what makes it
/// compatible with the rule that the finding says a secret sits at a location, never what it is.
/// Nothing in this file asserts on a credential's text, and nothing could - the finding does not
/// carry it.
/// </summary>
public sealed class LiteralCredentialSignalTests
{
	private const string _REFERENCE_LANGUAGE_NAME = "ExemplarOne";

	/// <summary>
	/// The credential argument is an identifier in executable code - a value supplied at run time,
	/// which is the case the signal exists to distinguish itself FROM.
	/// </summary>
	private const string _CREDENTIAL_SUPPLIED_AS_AN_IDENTIFIER = """
		let credentials = NewExemplarCredentials(operatorName, secretValue)
		""";

	/// <summary>
	/// The credential argument is a quoted literal in executable code. The declared capture group
	/// includes the opening quote, so the offset it resolves to is the literal's DELIMITER - see
	/// the test that pins this.
	/// </summary>
	private const string _CREDENTIAL_WRITTEN_AS_A_QUOTED_LITERAL = """
		let credentials = NewExemplarCredentials(operatorName, "hunter2")
		""";

	/// <summary>The whole construct inside a raw literal's body, credential argument and all.</summary>
	private const string _CREDENTIAL_INSIDE_A_LITERAL_BODY = """
		let documentation = ```
		let credentials = NewExemplarCredentials(operatorName, "hunter2")
		```
		""";

	/// <summary>
	/// The credential argument is a quoted literal whose body itself carries an interpolation hole.
	/// The declared capture group is one alternation - a whole quoted literal or a bare identifier -
	/// so this reference grammar has no way to write `user + ":" + password` into the credential
	/// position; an interpolation hole is the reachable construct that puts a
	/// <see cref="LexicalZone.Code"/> position inside an otherwise-literal capture, which is the
	/// same shape a concatenation would produce.
	/// </summary>
	private const string _CREDENTIAL_WRITTEN_WITH_AN_INTERPOLATION_HOLE = """
		let credentials = NewExemplarCredentials(operatorName, "pass${x}")
		""";

	/// <summary>
	/// The credential construction sits after a raw literal that opens and never closes. The scanner
	/// abandons the mask at the literal's opening position and fills everything past it
	/// <see cref="LexicalZone.StringBody"/> in the demoting direction - so the entire second line,
	/// credential argument included, lies in the untrustworthy region the scanner does not vouch
	/// for.
	/// </summary>
	private const string _CREDENTIAL_PAST_AN_UNTERMINATED_LITERAL = """
		let documentation = ```
		let credentials = NewExemplarCredentials(operatorName, "hunter2")
		""";

	/// <summary>The credential argument is a literal with an empty body - opening and closing quote, nothing between.</summary>
	private const string _CREDENTIAL_WRITTEN_AS_AN_EMPTY_LITERAL = """
		let credentials = NewExemplarCredentials(operatorName, "")
		""";

	/// <summary>
	/// The hand-assembled header class, which declares NO credential position at all - the field is
	/// optional and none is the honest answer for a class where no argument is the credential.
	/// </summary>
	private const string _HEADER_WITH_NO_CREDENTIAL_POSITION_DECLARED = """
		const AUTH_SCHEME = "Basic"
		const AUTH_SCHEME = "Digest"
		let request = SetHeader("Authorization", BuildScheme(AUTH_SCHEME))
		""";

	/// <summary>
	/// TRUE WHERE THE CREDENTIAL ARGUMENT LIES IN A STRING BODY. Here the whole construct sits
	/// inside a raw literal, so the credential's position carries
	/// <see cref="LexicalZone.StringBody"/> and the signal fires.
	/// </summary>
	[Fact]
	public void TheSignalIsTrueWhereTheCredentialArgumentLiesInAStringBody()
	{
		Finding finding = OnlyFindingFrom(_CREDENTIAL_INSIDE_A_LITERAL_BODY);

		Assert.True(finding.LiteralCredentialSignal);
	}

	/// <summary>
	/// FALSE WHERE THE CREDENTIAL ARGUMENT LIES IN CODE. An identifier is a value supplied at run
	/// time, which is precisely what the signal's absence means.
	/// </summary>
	[Fact]
	public void TheSignalIsFalseWhereTheCredentialArgumentLiesInCode()
	{
		Finding finding = OnlyFindingFrom(_CREDENTIAL_SUPPLIED_AS_AN_IDENTIFIER);

		Assert.False(finding.LiteralCredentialSignal);
	}

	/// <summary>
	/// FALSE, AND NOT AN ERROR, WHERE THE RULE DECLARES NO CREDENTIAL POSITION. The field is
	/// optional with none as the default, so the common case - a class where no argument is
	/// the credential - must pass through recognition without an exception and without a claim.
	/// </summary>
	[Fact]
	public void ARuleThatDeclaresNoCredentialPositionSignalsFalseRatherThanFailing()
	{
		Finding finding = OnlyFindingFrom(_HEADER_WITH_NO_CREDENTIAL_POSITION_DECLARED);

		Assert.Equal(FindingHarness.HAND_ASSEMBLED_HEADER_PATTERN_CLASS, finding.PatternClass.Value);
		Assert.False(RuleFor(finding.PatternClass.Value).CredentialPosition.IsDeclared);
		Assert.False(finding.LiteralCredentialSignal);
	}

	/// <summary>
	/// A CAPTURE THAT INCLUDES ITS OWN DELIMITERS SIGNALS TRUE. The opening and closing tokens of a
	/// literal are in the <see cref="LexicalZone.Delimiter"/> zone, and the delimiter COUNTS AS CODE -
	/// but the signal reads the credential group's WHOLE captured extent rather than one offset, and
	/// accepts <see cref="LexicalZone.Delimiter"/> alongside <see cref="LexicalZone.StringBody"/>
	/// across that extent. The reference profile declares its credential group as
	/// <c>(?&lt;credential&gt;"..."|identifier)</c> - INCLUDING the quotes - so this is the ordinary
	/// case of a credential written as a string literal in code, and it signals true.
	/// </summary>
	[Fact]
	public void TheSignalIsTrueWhereTheCredentialCaptureIncludesItsOwnDelimiters()
	{
		RecognitionHarness harness = new();
		FileRecognitionContext context = harness.ContextFor(_CREDENTIAL_WRITTEN_AS_A_QUOTED_LITERAL);
		string normalizedText = RecognitionHarness.Normalize(_CREDENTIAL_WRITTEN_AS_A_QUOTED_LITERAL);
		int openingQuoteOffset = normalizedText.IndexOf("\"hunter2\"", StringComparison.Ordinal);

		Finding finding = OnlyFindingFrom(_CREDENTIAL_WRITTEN_AS_A_QUOTED_LITERAL);

		Assert.Equal(LexicalZone.Delimiter, context.Mask.ZoneAt(openingQuoteOffset));
		Assert.Equal(LexicalZone.StringBody, context.Mask.ZoneAt(openingQuoteOffset + 1));
		Assert.True(finding.LiteralCredentialSignal);
	}

	/// <summary>
	/// A CAPTURE WHOSE EXTENT MIXES A CODE POSITION INTO A LITERAL RUN SIGNALS FALSE. The
	/// reference profile's credential group cannot itself express `user + ":" + password` - it is
	/// one alternation, a whole quoted literal or a bare identifier, with no operator - so an
	/// interpolation hole is used here as the reachable construct with the same shape: the extent
	/// contains both a <see cref="LexicalZone.StringBody"/> run (`pass`) and a
	/// <see cref="LexicalZone.Code"/> position (the hole's identifier), which is precisely what a
	/// concatenation would also put inside one extent. Rejecting it is what tells this rule apart
	/// from an implementation that signals true on "any position is StringBody".
	/// </summary>
	[Fact]
	public void ACaptureThatMixesACodePositionIntoALiteralRunSignalsFalse()
	{
		Finding finding = OnlyFindingFrom(_CREDENTIAL_WRITTEN_WITH_AN_INTERPOLATION_HOLE);

		Assert.False(finding.LiteralCredentialSignal);
	}

	/// <summary>
	/// A CAPTURE PAST AN UNTERMINATED LITERAL SIGNALS FALSE. The raw literal opened by the
	/// documentation line never closes, so the scanner abandons the mask at its opening position and
	/// fills everything past it - the whole second line, credential argument included -
	/// <see cref="LexicalZone.StringBody"/> in the demoting direction. Requiring
	/// <see cref="ZoneMask.IsTrustworthyAt"/> across the whole extent keeps such a position from
	/// reading as a literal credential.
	/// </summary>
	[Fact]
	public void ACaptureThatFallsPastAnUnterminatedLiteralSignalsFalse()
	{
		RecognitionHarness harness = new();
		FileRecognitionContext context = harness.ContextFor(_CREDENTIAL_PAST_AN_UNTERMINATED_LITERAL);

		Finding finding = OnlyFindingFrom(_CREDENTIAL_PAST_AN_UNTERMINATED_LITERAL);

		Assert.False(context.Mask.IsCompletelyValid);
		Assert.Equal(LexicalAnomalyReason.UnterminatedLiteral, context.Mask.AnomalyReason);
		Assert.False(finding.LiteralCredentialSignal);
	}

	/// <summary>
	/// THE "AT LEAST ONE STRING-BODY POSITION" CLAUSE, STATED EXPLICITLY. An empty literal's
	/// captured extent is two delimiters - the opening and closing quote - and nothing else: every
	/// position qualifies as <see cref="LexicalZone.StringBody"/> or <see cref="LexicalZone.Delimiter"/>,
	/// but none of them IS <see cref="LexicalZone.StringBody"/>, so the rule's fourth clause is what
	/// decides this case and the signal is false.
	/// </summary>
	[Fact]
	public void AnEmptyLiteralCaptureSignalsFalse()
	{
		Finding finding = OnlyFindingFrom(_CREDENTIAL_WRITTEN_AS_AN_EMPTY_LITERAL);

		Assert.False(finding.LiteralCredentialSignal);
	}

	/// <summary>
	/// DESTINATION CERTAINTY IS READ OFF THE RULE THE CATALOG DECLARED IT ON, NEVER RECOMPUTED. The
	/// two classes here declare opposite values, so a component that derived the field - from the
	/// level, from the self-sufficiency, from anything at all - would have to get both right by
	/// coincidence.
	/// </summary>
	[Theory]
	[InlineData(_CREDENTIAL_SUPPLIED_AS_AN_IDENTIFIER, FindingHarness.CREDENTIAL_TYPE_PATTERN_CLASS)]
	[InlineData(_HEADER_WITH_NO_CREDENTIAL_POSITION_DECLARED, FindingHarness.HAND_ASSEMBLED_HEADER_PATTERN_CLASS)]
	public void TheDestinationCertaintyIsTheOneTheRuleDeclares(string sourceText, string expectedPatternClass)
	{
		Finding finding = OnlyFindingFrom(sourceText);

		DetectionRule rule = RuleFor(expectedPatternClass);

		Assert.Equal(expectedPatternClass, finding.PatternClass.Value);
		Assert.Equal(rule.DestinationCertainty, finding.DestinationCertainty);
	}

	/// <summary>
	/// The two declared values are both reachable, which is what makes the test above a comparison
	/// rather than a tautology over one constant.
	/// </summary>
	[Fact]
	public void BothDestinationCertaintiesAreReachable()
	{
		Finding established = OnlyFindingFrom(_CREDENTIAL_SUPPLIED_AS_AN_IDENTIFIER);
		Finding unknown = OnlyFindingFrom(_HEADER_WITH_NO_CREDENTIAL_POSITION_DECLARED);

		Assert.Equal(DestinationCertainty.Established, established.DestinationCertainty);
		Assert.Equal(DestinationCertainty.Unknown, unknown.DestinationCertainty);
	}

	/// <summary>
	/// The level is the one recognition composed, carried through without being touched. The
	/// literal-credential signal does not change it, and the literal-body case is
	/// where a component that conflated the two would show it: that construct is Low because its
	/// ANCHOR is outside code, and it signals true at the same time.
	/// </summary>
	[Fact]
	public void TheLevelIsCarriedFromRecognitionAndTheSignalDoesNotMoveIt()
	{
		Finding inCode = OnlyFindingFrom(_CREDENTIAL_SUPPLIED_AS_AN_IDENTIFIER);
		Finding inALiteralBody = OnlyFindingFrom(_CREDENTIAL_INSIDE_A_LITERAL_BODY);

		Assert.Equal(ConfidenceLevel.High, inCode.Level);
		Assert.False(inCode.LiteralCredentialSignal);
		Assert.Equal(ConfidenceLevel.Low, inALiteralBody.Level);
		Assert.True(inALiteralBody.LiteralCredentialSignal);
	}

	/// <summary>
	/// One source text, recognized for real and composed into exactly one finding. The single
	/// finding is asserted rather than assumed, so a fixture that started matching twice fails here
	/// instead of quietly having its second finding ignored.
	/// </summary>
	private static Finding OnlyFindingFrom(string sourceText)
	{
		RecognitionHarness harness = new();
		FileRecognitionContext context = harness.ContextFor(sourceText);
		ImmutableArray<RecognizedConstruct> constructs = harness.Recognize(sourceText);

		ImmutableArray<Finding> findings = FindingComposition.Compose(
		[
			new RecognizedFile
			{
				Path = context.Path,
				Language = AssessedLanguage.CSharp,
				NormalizedText = context.NormalizedText,
				Analyzer = context.Analyzer,
				Constructs = constructs
			}
		]);

		return Assert.Single(findings);
	}

	/// <summary>
	/// What the CATALOG declared for a class, looked up from the registry. A finding carries a
	/// pattern class identifier and not its analyzer, so a test comparing a finding against its rule
	/// has to go and find the rule.
	/// </summary>
	private static DetectionRule RuleFor(string patternClassIdentifier)
	{
		ILanguageAnalyzer analyzer =
			ExemplarFixtureCatalog.Build().AnalyzersByLanguageName[_REFERENCE_LANGUAGE_NAME];

		return analyzer.DetectionRulesByPatternClass[patternClassIdentifier];
	}
}
