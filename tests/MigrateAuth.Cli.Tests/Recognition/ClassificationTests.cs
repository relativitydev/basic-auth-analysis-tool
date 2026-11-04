using System.Collections.Immutable;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Lexical;
using MigrateAuth.Cli.Recognition;

using Xunit;

namespace MigrateAuth.Cli.Tests.Recognition;

/// <summary>
/// THE COMPOSITION OF CONFIDENCE: THE MINIMUM OF A SET OF CANDIDATES, NEVER A CHAIN OF DECREMENTS.
///
/// <see cref="AClassAtMediumThatIsNeitherSelfSufficientNorResolvedStaysAtMedium"/> is the case that shows the
/// difference: base Medium, not self-sufficient, indeterminate verdict. Under successive demotion that lands at
/// Low, out of the recall numerator, which counts High or Medium, and without a recommendation under the
/// manual-review rule. Under a minimum over {Medium, Medium, Medium} it lands at Medium.
/// </summary>
public sealed class ClassificationTests
{
	private const string _CREDENTIAL_TYPE_PATTERN_CLASS = "EXM-001";
	private const string _HAND_ASSEMBLED_HEADER_PATTERN_CLASS = "EXM-002";

	private const string _CREDENTIAL_CONSTRUCTION_IN_CODE = """
		let credentials = NewExemplarCredentials(operatorName, "hunter2")
		""";

	/// <summary>The same construct, inside a line comment. The rubric's Low population, exactly.</summary>
	private const string _CREDENTIAL_CONSTRUCTION_IN_A_COMMENT = """
		-- let credentials = NewExemplarCredentials(operatorName, "hunter2")
		""";

	/// <summary>The same construct, inside a raw literal's body.</summary>
	private const string _CREDENTIAL_CONSTRUCTION_IN_A_LITERAL_BODY = """
		let documentation = ```
		let credentials = NewExemplarCredentials(operatorName, "hunter2")
		```
		""";

	/// <summary>
	/// Base Medium, not self-sufficient, and an indeterminate verdict.
	/// </summary>
	private const string _HEADER_ASSEMBLED_FROM_AN_UNRESOLVABLE_CONSTANT = """
		const AUTH_SCHEME = "Basic"
		const AUTH_SCHEME = "Digest"
		let request = SetHeader("Authorization", BuildScheme(AUTH_SCHEME))
		""";

	/// <summary>
	/// A self-sufficient class in executable code reaches High, which is the only route to High the
	/// engine has: the rubric declares its second Medium condition unreachable without flow
	/// analysis, so every class that needs the call path established is capped at Medium by its own
	/// declaration rather than by anything observed in the file.
	/// </summary>
	[Fact]
	public void ASelfSufficientClassAnchoredInCodeReachesHigh()
	{
		RecognitionHarness harness = new();

		ImmutableArray<RecognizedConstruct> recognized = harness.Recognize(_CREDENTIAL_CONSTRUCTION_IN_CODE);

		RecognizedConstruct construct = Assert.Single(recognized);
		Assert.Equal(_CREDENTIAL_TYPE_PATTERN_CLASS, construct.PatternClassIdentifier);
		Assert.Equal(ConfidenceLevel.High, construct.Level);
	}

	/// <summary>
	/// The zone is an INPUT to classification and never a filter.
	/// </summary>
	[Theory]
	[InlineData(_CREDENTIAL_CONSTRUCTION_IN_A_COMMENT)]
	[InlineData(_CREDENTIAL_CONSTRUCTION_IN_A_LITERAL_BODY)]
	public void AnAnchorOutsideCodeProposesLowEvenWhereTheClassIsHigh(string sourceText)
	{
		RecognitionHarness harness = new();

		ImmutableArray<RecognizedConstruct> recognized = harness.Recognize(sourceText);

		RecognizedConstruct construct = Assert.Single(recognized);
		Assert.Equal(_CREDENTIAL_TYPE_PATTERN_CLASS, construct.PatternClassIdentifier);
		Assert.Equal(ConfidenceLevel.Low, construct.Level);
	}

	/// <summary>
	/// Three signals each proposing Medium compose to Medium.
	/// If this test ever reads Low, the composition has become a decrement chain and the
	/// dominant classes have silently left the recall numerator.
	/// </summary>
	[Fact]
	public void AClassAtMediumThatIsNeitherSelfSufficientNorResolvedStaysAtMedium()
	{
		RecognitionHarness harness = new();

		ImmutableArray<RecognizedConstruct> recognized =
			harness.Recognize(_HEADER_ASSEMBLED_FROM_AN_UNRESOLVABLE_CONSTANT);

		RecognizedConstruct construct = Assert.Single(recognized);
		Assert.Equal(_HAND_ASSEMBLED_HEADER_PATTERN_CLASS, construct.PatternClassIdentifier);
		Assert.Equal(ConfidenceLevel.Medium, construct.Level);
	}

	/// <summary>
	/// The anchor position, end to end rather than as a resolved offset. The hand-assembled
	/// header's match STARTS on the opening quote of a header name - inside a literal - while its
	/// anchor lies on executable code further along, and a rule tested at its match start could
	/// never produce anything but Low here.
	/// </summary>
	[Fact]
	public void TheZoneThatDecidesIsTheAnchorsAndNotTheMatchStarts()
	{
		RecognitionHarness harness = new();
		FileRecognitionContext context = harness.ContextFor(_HEADER_ASSEMBLED_FROM_AN_UNRESOLVABLE_CONSTANT);

		ImmutableArray<RecognizedConstruct> recognized =
			harness.Recognize(_HEADER_ASSEMBLED_FROM_AN_UNRESOLVABLE_CONSTANT);

		RecognizedConstruct construct = Assert.Single(recognized);
		Assert.Equal(LexicalZone.Code, context.Mask.ZoneAt(construct.AnchorOffset));
		Assert.NotEqual(ConfidenceLevel.Low, construct.Level);
	}

	/// <summary>
	/// The anchor offset is what ordering and numbering use, so it has to point at the
	/// construct rather than at the start of the expression that found it.
	/// </summary>
	[Fact]
	public void TheAnchorOffsetPointsAtTheConstructTheRuleAnchoredOn()
	{
		RecognitionHarness harness = new();
		string normalizedText = RecognitionHarness.Normalize(_CREDENTIAL_CONSTRUCTION_IN_CODE);

		ImmutableArray<RecognizedConstruct> recognized = harness.Recognize(_CREDENTIAL_CONSTRUCTION_IN_CODE);

		RecognizedConstruct construct = Assert.Single(recognized);
		Assert.Equal(
			normalizedText.IndexOf("NewExemplarCredentials", StringComparison.Ordinal),
			construct.AnchorOffset);
	}
}
