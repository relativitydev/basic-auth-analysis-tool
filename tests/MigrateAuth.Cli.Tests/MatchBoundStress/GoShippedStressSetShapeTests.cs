using System.Collections.Immutable;
using System.Text;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Findings;
using MigrateAuth.Cli.Reading;
using MigrateAuth.Cli.Recognition;
using MigrateAuth.Cli.Tests.Recognition;

using Xunit;

namespace MigrateAuth.Cli.Tests.MatchBoundStress;

/// <summary>
/// THE SHAPES OF THE SHIPPED `Go` COST GATE, CHECKED WITHOUT TIMING. The timing tests are run only in a separate timing run, so what can be checked here is checked here: every shape is legal for the reader (under the
/// file ceiling, no line over the line ceiling, no carriage return, the `.go` extension), the names are unique, the four families are disjoint and are together exactly the set, the set holds the typical, the worst, the
/// argument-window, the long-line, the definition-window and the vendored and test-data shapes, and a shape under a demoted folder stands at a path that demotes it.
/// </summary>
public sealed class GoShippedStressSetShapeTests
{
	private const char _CARRIAGE_RETURN = '\r';
	private const char _LINE_FEED = '\n';
	private const int _WINDOW_CLASS_COUNT = 5;
	private const int _SHAPES_PER_WINDOW_CLASS = 3;
	private const int _EVIDENCE_PROBE_LENGTH_IN_CHARACTERS = 3_000;
	private const char _FILLER_CHARACTER = 'x';
	private const int _ONE_PAST_THE_BOUND = 1;
	private const string _MARKUP_OPENING = "<input";
	private const string _TYPE_WORD = "type";
	private const string _HIGH_OPENING_CLASS = "GO-001";

	[Fact]
	public void EveryStressShapeIsLegalForTheReaderAndFillsTheFileToJustUnderTheCeiling()
	{
		List<string> refused = [];

		foreach (MatchBoundStressShape shape in GoShippedCostGateStressSet.Build())
		{
			bool tooLarge = Encoding.UTF8.GetByteCount(shape.Text) > ReadingCeilings.MAXIMUM_FILE_SIZE_IN_BYTES;
			bool lineTooLong = shape.Text.Split(_LINE_FEED).Any(line => line.Length > ReadingCeilings.MAXIMUM_LINE_LENGTH_IN_CHARACTERS);

			if (tooLarge || lineTooLong || shape.Text.Contains(_CARRIAGE_RETURN) || shape.FileExtension != GoShippedCostGateStressSet.FILE_EXTENSION)
			{
				refused.Add($"{shape.Name}: tooLarge={tooLarge} lineTooLong={lineTooLong}");
			}
		}

		Assert.Empty(refused);
	}

	[Fact]
	public void EveryStressShapeNameIsUniqueAndTheFamiliesAreDisjointAndTogetherExactlyTheSet()
	{
		ImmutableArray<MatchBoundStressShape> shapes = GoShippedCostGateStressSet.Build();
		string[] byFamily =
		[
			.. Enum.GetValues<GoShippedCostGateStressSet.ShippedShapeFamily>()
				.SelectMany(family => GoShippedCostGateStressSet.BuildTheFamily(family))
				.Select(shape => shape.Name)
		];

		Assert.Equal(shapes.Length, shapes.Select(shape => shape.Name).Distinct(StringComparer.Ordinal).Count());
		Assert.Equal(shapes.Select(shape => shape.Name), byFamily);
	}

	[Fact]
	public void TheSetHoldsTheTypicalTheWorstTheArgumentWindowTheLongLineTheDefinitionWindowAndTheDemotedFolderShapes()
	{
		ImmutableArray<MatchBoundStressShape> shapes = GoShippedCostGateStressSet.Build();

		Assert.Contains(shapes, shape => shape.Name.StartsWith(GoShippedCostGateStressSet.TYPICAL_PREFIX, StringComparison.Ordinal));
		Assert.Contains(shapes, shape => shape.Name.StartsWith(GoShippedCostGateStressSet.WORST_PREFIX, StringComparison.Ordinal));
		Assert.Contains(shapes, shape => shape.Name.StartsWith(GoShippedCostGateStressSet.LONG_LINE_PREFIX, StringComparison.Ordinal));
		Assert.Contains(shapes, shape => shape.Name.StartsWith(GoShippedCostGateStressSet.VENDORED_PREFIX, StringComparison.Ordinal));
		Assert.Contains(shapes, shape => shape.Name.StartsWith(GoShippedCostGateStressSet.TEST_DATA_PREFIX, StringComparison.Ordinal));
		Assert.Contains(shapes, shape => shape.Name.Contains("definition-round-tripper", StringComparison.Ordinal));
		Assert.Contains(shapes, shape => shape.Name.Contains("definition-helper-head", StringComparison.Ordinal));
		Assert.Contains(shapes, shape => shape.Name.Contains("setbasicauth", StringComparison.Ordinal));
		Assert.Contains(shapes, shape => shape.Name.Contains("authorization", StringComparison.Ordinal));
		Assert.Equal(
			_WINDOW_CLASS_COUNT * _SHAPES_PER_WINDOW_CLASS,
			shapes.Count(shape => shape.Name.StartsWith(GoShippedCostGateStressSet.WINDOW_PREFIX, StringComparison.Ordinal)));
	}

	[Fact]
	public void EveryClassThatReadsAnArgumentWindowHasAShapeWithTheKeyAtTheBoundOnePastItAndTheWindowNeverClosed()
	{
		string[] names = [.. GoShippedCostGateStressSet.Build().Select(shape => shape.Name).Where(name => name.StartsWith(GoShippedCostGateStressSet.WINDOW_PREFIX, StringComparison.Ordinal))];

		Assert.All(new[] { "header-literal", "map-literal", "request-call", "encoder", "local-helper-call" }, slug =>
		{
			Assert.Contains($"{GoShippedCostGateStressSet.WINDOW_PREFIX}{slug}-key-at-the-bound", names);
			Assert.Contains($"{GoShippedCostGateStressSet.WINDOW_PREFIX}{slug}-key-beyond-the-bound", names);
			Assert.Contains($"{GoShippedCostGateStressSet.WINDOW_PREFIX}{slug}-never-closed", names);
		});
	}

	[Fact]
	public void AShapeStandsUnderTheVendoredFolderOrTheTestDataFolderWhereItsNameSaysSoAndUnderTheStressFolderOtherwise()
	{
		ImmutableArray<MatchBoundStressShape> shapes = GoShippedCostGateStressSet.Build();
		MatchBoundStressShape vendored = shapes.First(shape => shape.Name.StartsWith(GoShippedCostGateStressSet.VENDORED_PREFIX, StringComparison.Ordinal));
		MatchBoundStressShape testData = shapes.First(shape => shape.Name.StartsWith(GoShippedCostGateStressSet.TEST_DATA_PREFIX, StringComparison.Ordinal));
		MatchBoundStressShape ordinary = shapes.First(shape => shape.Name.StartsWith(GoShippedCostGateStressSet.WORST_PREFIX, StringComparison.Ordinal));

		Assert.StartsWith($"{GoShippedCostGateStressSet.VENDORED_FOLDER}/", GoShippedCostGateStressSet.RelativePathOf(vendored), StringComparison.Ordinal);
		Assert.StartsWith($"{GoShippedCostGateStressSet.TEST_DATA_FOLDER}/", GoShippedCostGateStressSet.RelativePathOf(testData), StringComparison.Ordinal);
		Assert.StartsWith($"{GoShippedCostGateStressSet.DEFAULT_FOLDER}/", GoShippedCostGateStressSet.RelativePathOf(ordinary), StringComparison.Ordinal);
	}

	[Fact]
	public void AVendoredShapeIsDemotedToLowAndATestDataShapeToMediumByThePathItStandsAt()
	{
		const string source = "request.SetBasicAuth(userName, secret)\n";
		GoRecognitionHarness harness = new();
		ImmutableArray<MatchBoundStressShape> shapes = GoShippedCostGateStressSet.Build();
		string vendoredPath = GoShippedCostGateStressSet.RelativePathOf(shapes.First(shape => shape.Name.StartsWith(GoShippedCostGateStressSet.VENDORED_PREFIX, StringComparison.Ordinal)));
		string testDataPath = GoShippedCostGateStressSet.RelativePathOf(shapes.First(shape => shape.Name.StartsWith(GoShippedCostGateStressSet.TEST_DATA_PREFIX, StringComparison.Ordinal)));

		Assert.Equal(ConfidenceLevel.Low, Assert.Single(harness.Recognize(source, vendoredPath)).Level);
		Assert.Equal(ConfidenceLevel.Medium, Assert.Single(harness.Recognize(source, testDataPath)).Level);
	}

	[Fact]
	public void TheFlowSignalShapesAreExactlyTheFlowSignalFamilyAndAreTimedAsItsOwnPart()
	{
		ImmutableArray<MatchBoundStressShape> flow = GoShippedCostGateStressSet.BuildTheFlowSignalShapes();
		ImmutableArray<MatchBoundStressShape> all = GoShippedCostGateStressSet.Build();

		Assert.NotEmpty(flow);
		Assert.All(flow, shape => Assert.StartsWith(GoShippedCostGateStressSet.FLOW_SIGNAL_PREFIX, shape.Name, StringComparison.Ordinal));
		Assert.All(flow, shape => Assert.Contains(all, candidate => candidate.Name == shape.Name && candidate.Text == shape.Text));
		Assert.Equal(flow.Length, all.Count(shape => shape.Name.StartsWith(GoShippedCostGateStressSet.FLOW_SIGNAL_PREFIX, StringComparison.Ordinal)));
		Assert.All(
			Enum.GetValues<GoShippedCostGateStressSet.ShippedShapeFamily>().Where(family => family != GoShippedCostGateStressSet.ShippedShapeFamily.FlowSignals),
			family => Assert.DoesNotContain(GoShippedCostGateStressSet.BuildTheFamily(family), shape => shape.Name.StartsWith(GoShippedCostGateStressSet.FLOW_SIGNAL_PREFIX, StringComparison.Ordinal)));
	}

	/// <summary>The engine evaluates the flow signals only over a file with a High or Medium construct, so every flow shape opens with one: it is recognized as one High construct of class GO-001, (a surviving High or Medium construct is what the engine evaluates the flow signals for) and it gives no flow evidence of its own.</summary>
	[Fact]
	public void EveryFlowSignalShapeOpensWithTheOneHighConstructThatMakesTheEngineEvaluateTheFlowSignalsAndThatGivesNoEvidenceItself()
	{
		RecognitionResult opening = new GoRecognitionHarness().RecognizeFully(GoShippedCostGateStressSet.FLOW_SHAPE_HIGH_CONSTRUCT);
		RecognizedConstruct construct = Assert.Single(opening.Constructs);

		Assert.Equal(_HIGH_OPENING_CLASS, construct.PatternClassIdentifier);
		Assert.Equal(ConfidenceLevel.High, construct.Level);
		Assert.Equal(FlowEvidence.None, opening.FlowEvidence);
		Assert.All(GoShippedCostGateStressSet.BuildTheFlowSignalShapes(), shape => Assert.StartsWith(GoShippedCostGateStressSet.FLOW_SHAPE_HIGH_CONSTRUCT, shape.Text, StringComparison.Ordinal));
	}

	/// <summary>Every one of the eleven flow-signal expressions has a shape whose first group the shipped analyzer reads as that signal's evidence, so the timing gate stands a window of each at the ceiling of the line.</summary>
	[Theory]
	[InlineData("EX-GO-120", "long-line-terminal-password-reads", FlowEvidence.Interactive)]
	[InlineData("EX-GO-121", "long-line-standard-input-reads-with-credential-prompts", FlowEvidence.Interactive)]
	[InlineData("EX-GO-122", "long-line-formatted-scans-into-credential-variables", FlowEvidence.Interactive)]
	[InlineData("EX-GO-123", "long-line-form-credential-reads", FlowEvidence.Interactive)]
	[InlineData("EX-GO-124", "long-line-markup-password-inputs", FlowEvidence.Interactive)]
	[InlineData("EX-GO-125", "long-line-login-route-registrations", FlowEvidence.Interactive)]
	[InlineData("EX-GO-130", "long-line-environment-credential-reads", FlowEvidence.Fixed)]
	[InlineData("EX-GO-131", "long-line-configuration-credential-members", FlowEvidence.Fixed)]
	[InlineData("EX-GO-132", "long-line-secret-manager-calls", FlowEvidence.Fixed)]
	[InlineData("EX-GO-133", "long-line-credential-named-literal-assignments", FlowEvidence.Fixed)]
	[InlineData("EX-GO-134", "long-line-secret-file-reads", FlowEvidence.Fixed)]
	public void EveryFlowSignalExpressionHasAShapeWhoseFirstGroupTheShippedAnalyzerReadsAsItsEvidence(string expressionId, string shapeSlug, FlowEvidence expected)
	{
		MatchBoundStressShape shape = Assert.Single(GoShippedCostGateStressSet.Build(), candidate => candidate.Name == $"{GoShippedCostGateStressSet.FLOW_SIGNAL_PREFIX}{shapeSlug}");
		string firstLines = shape.Text[..Math.Min(shape.Text.Length, _EVIDENCE_PROBE_LENGTH_IN_CHARACTERS)];

		Assert.Equal(expected, new GoRecognitionHarness().RecognizeFully(firstLines).FlowEvidence);
		Assert.True(shape.Name.Contains(shapeSlug, StringComparison.Ordinal), expressionId);
	}

	/// <summary>The engine evaluates the flow signals only over a file that holds a High or Medium construct (<c>FilesEvaluatedForFlowSignals</c>), so each flow shape must give exactly one flow evaluation. No clock is read: the engine runs on a frozen one.</summary>
	[Fact]
	public void EveryFlowSignalShapeGivesOneFlowEvaluationOfTheEngine()
	{
		GoRecognitionHarness harness = new();
		ImmutableArray<MatchBoundStressShape> flow = GoShippedCostGateStressSet.BuildTheFlowSignalShapes();
		List<string> withoutEvaluation = [];
		int evaluations = 0;

		foreach (MatchBoundStressShape shape in flow)
		{
			RecognitionEngine engine = new(harness.EmissionPoint, new FrozenTimeProvider());

			engine.Recognize(harness.ContextFor(shape.Text, GoShippedCostGateStressSet.RelativePathOf(shape)));
			evaluations += engine.FilesEvaluatedForFlowSignals;

			if (engine.FilesEvaluatedForFlowSignals != 1)
			{
				withoutEvaluation.Add(shape.Name);
			}
		}

		Assert.True(withoutEvaluation.Count == 0, $"flow evaluations {evaluations} over {flow.Length} flow shapes; without one: {string.Join(", ", withoutEvaluation)}");
	}

	/// <summary>The flow shapes that are bounds of a window stand the window at and one past its bound: the formatted scan at its 1,500 units and the markup tag at its 200 characters.</summary>
	[Fact]
	public void TheFormattedScanWindowAndTheMarkupTagStandAtTheirBoundsAndOnePastThem()
	{
		ImmutableArray<MatchBoundStressShape> flow = GoShippedCostGateStressSet.BuildTheFlowSignalShapes();
		string prefix = GoShippedCostGateStressSet.FLOW_SIGNAL_PREFIX;
		string scanAtTheBound = $"fmt.Scan({new string(_FILLER_CHARACTER, GoShippedCostGateStressSet.ARGUMENT_WINDOW_UNITS)}&password)";
		string scanPastTheBound = $"fmt.Scan({new string(_FILLER_CHARACTER, GoShippedCostGateStressSet.ARGUMENT_WINDOW_UNITS + _ONE_PAST_THE_BOUND)}&password)";

		Assert.Contains(scanAtTheBound, Assert.Single(flow, shape => shape.Name == $"{prefix}formatted-scan-window-at-the-unit-bound").Text, StringComparison.Ordinal);
		Assert.Contains(scanPastTheBound, Assert.Single(flow, shape => shape.Name == $"{prefix}formatted-scan-window-past-the-unit-bound").Text, StringComparison.Ordinal);
	}

	/// <summary>The look-ahead of EX-GO-124 reads up to 200 characters after the opening of the tag before the attribute word, so the at-bound shape has exactly 200 characters between them and fires, and the one-past shape has exactly 201 and gives no evidence. Read over the first lines: every line of a shape is the same.</summary>
	[Fact]
	public void TheMarkupTagStandsExactlyAtTheLookAheadBoundAndOneCharacterPastItAndOnlyTheFirstFires()
	{
		ImmutableArray<MatchBoundStressShape> flow = GoShippedCostGateStressSet.BuildTheFlowSignalShapes();
		MatchBoundStressShape atTheBound = Assert.Single(flow, shape => shape.Name == $"{GoShippedCostGateStressSet.FLOW_SIGNAL_PREFIX}markup-input-tag-at-the-bound");
		MatchBoundStressShape pastTheBound = Assert.Single(flow, shape => shape.Name == $"{GoShippedCostGateStressSet.FLOW_SIGNAL_PREFIX}markup-input-tag-past-the-bound");

		Assert.Equal(GoShippedCostGateStressSet.MARKUP_TAG_CHARACTERS, CharactersBetweenTheOpeningAndTheTypeWord(atTheBound.Text));
		Assert.Equal(GoShippedCostGateStressSet.MARKUP_TAG_CHARACTERS + _ONE_PAST_THE_BOUND, CharactersBetweenTheOpeningAndTheTypeWord(pastTheBound.Text));
		Assert.Equal(FlowEvidence.Interactive, new GoRecognitionHarness().RecognizeFully(atTheBound.Text[.._EVIDENCE_PROBE_LENGTH_IN_CHARACTERS]).FlowEvidence);
		Assert.Equal(FlowEvidence.None, new GoRecognitionHarness().RecognizeFully(pastTheBound.Text[.._EVIDENCE_PROBE_LENGTH_IN_CHARACTERS]).FlowEvidence);
	}

	private static int CharactersBetweenTheOpeningAndTheTypeWord(string text)
	{
		int openingEnd = text.IndexOf(_MARKUP_OPENING, StringComparison.Ordinal) + _MARKUP_OPENING.Length;

		return text.IndexOf(_TYPE_WORD, openingEnd, StringComparison.Ordinal) - openingEnd;
	}
}
