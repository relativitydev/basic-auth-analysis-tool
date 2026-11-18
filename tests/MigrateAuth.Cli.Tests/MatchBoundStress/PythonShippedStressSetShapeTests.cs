using System.Collections.Immutable;
using System.Reflection;
using System.Text;

using MigrateAuth.Cli.Catalog.Languages;
using MigrateAuth.Cli.Reading;

using Xunit;

namespace MigrateAuth.Cli.Tests.MatchBoundStress;

/// <summary>
/// THE SHAPES OF THE SHIPPED `Python` STRESS SET, WITHOUT A CLOCK: every shape is legal for the reader (under the file ceiling, no line over the line ceiling, no carriage return, a `Python` extension), every name is unique,
/// the set holds every shape the catalog names, the argument-window family stands the window at and past each of its bounds, the per-file worst shapes are present at the ceiling, and the bounds the shapes are built from are
/// the ones the vocabulary of the expressions declares. The set is timed only in a timing window; this class runs in the normal suite and times nothing.
/// </summary>
public sealed class PythonShippedStressSetShapeTests
{
	private const string _VOCABULARY_TYPE_NAME = "MigrateAuth.Cli.Catalog.Languages.PythonPatternExpressions";
	private const string _LINE_FEED = "\n";
	private const string _GENERATED_STUB_SUFFIX = "_pb2.py";
	private const int _CEILING_FRACTION_DIVISOR = 2;

	private static readonly ImmutableArray<string> _catalogFamilies =
	[
		"long-line-auth-tuple-openers-that-never-close", "long-line-named-callee-openers", "long-line-authorization-words", "long-line-f-string-heavy", "long-line-nested-brackets-at-the-depth-bound",
		"long-line-nested-brackets-past-the-depth-bound", "header-mapping-with-thousands-of-keys", "vendored-library-shape", "long-line-userinfo-urls", "long-line-dense-method-calls",
		"anchor-dense-near-misses", "every-class-at-match-density", "open-windows", "ordinary-at-ceiling", "two-megabytes-of-docstrings-with-anchors", "long-line-input-prompts",
		"input-prompts-in-full-lookbehind-windows", "encoder-pairs-with-use-words-at-the-window-edge", "loopback-servers-in-full-spans", "long-line-quote-bearing-f-string-pairs",
		"long-line-quote-bearing-f-string-fields-without-a-colon", "long-line-held-name-openers", "held-name-seams-at-the-bound", "compound-held-declarations", "held-scheme-carriers-in-full-lookbehind-windows",
		"held-literal-declarations-before-carriers"
	];

	private static readonly ImmutableArray<string> _argumentWindowFamilies =
	[
		"at-the-unit-bound", "past-the-unit-bound", "capped-at-the-line-ceiling-of-characters", "strings-at-the-string-unit-bound", "strings-past-the-string-unit-bound", "nested-to-the-depth-bound",
		"nested-past-the-depth-bound", "group-at-the-unit-bound-per-level", "group-past-the-unit-bound-per-level", "guarded-by-the-token-request-word-at-the-end-of-a-full-window",
		"opened-across-lines-and-never-closed", "dense-openers-of-every-named-callee"
	];

	private static readonly ImmutableArray<string> _perFileWorstFamilies =
	[
		"per-file-generated-protocol-stub_pb2", "per-file-long-dictionary-literal-of-seventeen-thousand-entries", "per-file-dictionaries-on-one-line-at-the-line-ceiling", "per-file-two-megabyte-data-file"
	];

	private static readonly ImmutableArray<string> _flowSignalFamilies =
	[
		"long-line-getpass-calls", "long-line-webbrowser-calls", "long-line-webbrowser-names-without-a-call", "long-line-loopback-servers-with-query-parsers-and-no-code-key",
		"long-line-loopback-servers-after-code-keys-without-a-query-parser", "long-line-loopback-servers-with-code-reads", "long-line-request-form-credential-reads", "long-line-oauth-form-names",
		"long-line-authenticate-and-login-calls", "long-line-authenticate-openers-without-a-close", "long-line-framework-session-subscripts", "long-line-environment-openers-without-a-key",
		"long-line-configuration-receivers-with-credential-members", "long-line-configuration-receivers-with-long-near-miss-members", "long-line-configuration-subscripts-and-gets",
		"long-line-configuration-receivers-without-a-key", "long-line-authenticators-calls", "long-line-credential-named-assignments-with-f-strings", "long-line-credential-names-with-annotations-at-the-bound",
		"long-line-credential-name-prefixes-without-an-assignment", "long-line-form-openers-after-a-credential-annotation", "anchor-dense-near-misses", "open-windows",
		"input-prompts-with-local-declarations-in-full-lookbehind-windows", "loopback-servers-with-query-parsers-in-full-spans", "authenticate-window-at-the-unit-bound", "authenticate-window-past-the-unit-bound"
	];

	[Fact]
	public void EveryShapeIsLegalForTheReaderAndFillsTheFileToJustUnderTheCeiling()
	{
		List<string> refused = [];

		foreach (MatchBoundStressShape shape in PythonShippedCostGateStressSet.Build())
		{
			int bytes = Encoding.UTF8.GetByteCount(shape.Text);
			bool tooLarge = bytes > ReadingCeilings.MAXIMUM_FILE_SIZE_IN_BYTES;
			bool tooSmall = bytes < ReadingCeilings.MAXIMUM_FILE_SIZE_IN_BYTES / _CEILING_FRACTION_DIVISOR;
			bool lineTooLong = shape.Text.Split(_LINE_FEED).Any(line => line.Length > ReadingCeilings.MAXIMUM_LINE_LENGTH_IN_CHARACTERS);
			bool illegal = shape.Text.Contains('\r') || shape.FileExtension != PythonShippedCostGateStressSet.FILE_EXTENSION;

			if (tooLarge || tooSmall || lineTooLong || illegal)
			{
				refused.Add($"{shape.Name}: bytes={bytes} lineTooLong={lineTooLong} extension={shape.FileExtension}");
			}
		}

		Assert.Empty(refused);
	}

	[Fact]
	public void EveryShapeNameIsUniqueAndCarriesThePrefixOfTheShippedSet()
	{
		ImmutableArray<MatchBoundStressShape> shapes = PythonShippedCostGateStressSet.Build();

		Assert.Equal(shapes.Length, shapes.Select(shape => shape.Name).Distinct(StringComparer.Ordinal).Count());
		Assert.All(shapes, shape => Assert.StartsWith(PythonShippedCostGateStressSet.SHAPE_NAME_PREFIX, shape.Name, StringComparison.Ordinal));
	}

	[Fact]
	public void TheSetHoldsEveryShapeTheCatalogNames()
	{
		ImmutableArray<MatchBoundStressShape> shapes = PythonShippedCostGateStressSet.Build();

		Assert.All(_catalogFamilies, family => Assert.Contains(shapes, shape => shape.Name.Contains(family, StringComparison.Ordinal)));
	}

	[Fact]
	public void TheArgumentWindowFamilyStandsTheWindowAtAndPastEachOfItsBoundsAndItsShapesAreExactlyThoseOfTheBuilder()
	{
		ImmutableArray<MatchBoundStressShape> window = PythonShippedCostGateStressSet.BuildTheArgumentWindowShapes();
		ImmutableArray<MatchBoundStressShape> all = PythonShippedCostGateStressSet.Build();

		Assert.Equal(_argumentWindowFamilies.Length, window.Length);
		Assert.All(_argumentWindowFamilies, family =>
			Assert.Single(window, shape => shape.Name == $"{PythonShippedCostGateStressSet.ARGUMENT_WINDOW_SHAPE_NAME_PREFIX}{family}"));
		Assert.All(window, shape => Assert.Contains(all, candidate => candidate.Name == shape.Name && candidate.Text == shape.Text));
	}

	[Fact]
	public void TheFlowSignalShapesAreExactlyTheFlowSignalFamiliesAndAreTimedWithTheRemainingShapesOfTheSet()
	{
		ImmutableArray<MatchBoundStressShape> flow = PythonShippedCostGateStressSet.BuildTheFlowSignalShapes();
		ImmutableArray<MatchBoundStressShape> all = PythonShippedCostGateStressSet.Build();
		ImmutableArray<MatchBoundStressShape> argumentWindow = PythonShippedCostGateStressSet.BuildTheArgumentWindowShapes();

		Assert.Equal(_flowSignalFamilies.Length, flow.Length);
		Assert.All(_flowSignalFamilies, family => Assert.Single(flow, shape => shape.Name == $"{PythonShippedCostGateStressSet.FLOW_SIGNAL_SHAPE_NAME_PREFIX}{family}"));
		Assert.All(flow, shape => Assert.StartsWith(PythonShippedCostGateStressSet.FLOW_SIGNAL_SHAPE_NAME_PREFIX, shape.Name, StringComparison.Ordinal));
		Assert.All(flow, shape => Assert.Contains(all, candidate => candidate.Name == shape.Name && candidate.Text == shape.Text));
		Assert.All(flow, shape => Assert.DoesNotContain(argumentWindow, window => window.Name == shape.Name));
	}

	/// <summary>Every flow-signal expression has a shape whose text holds the anchor of the expression, so the timing gate stands a window of each one at the ceiling of the line.</summary>
	[Theory]
	[InlineData("EX-PY-120", "flow-long-line-getpass-calls", "getpass.getpass(")]
	[InlineData("EX-PY-121", "long-line-input-prompts", "input(")]
	[InlineData("EX-PY-121", "flow-input-prompts-with-local-declarations-in-full-lookbehind-windows", "def run(input)")]
	[InlineData("EX-PY-122", "flow-long-line-webbrowser-calls", "webbrowser.open(")]
	[InlineData("EX-PY-123", "flow-long-line-loopback-servers-with-code-reads", "TCPServer((")]
	[InlineData("EX-PY-123", "flow-loopback-servers-with-query-parsers-in-full-spans", "HTTPServer((")]
	[InlineData("EX-PY-124", "flow-long-line-request-form-credential-reads", "request.form[")]
	[InlineData("EX-PY-125", "flow-long-line-oauth-form-names", "OAuth2PasswordRequestForm")]
	[InlineData("EX-PY-125", "flow-long-line-form-openers-after-a-credential-annotation", "def f(password: str = Form(")]
	[InlineData("EX-PY-126", "flow-long-line-authenticate-and-login-calls", "authenticate(request")]
	[InlineData("EX-PY-126", "flow-authenticate-window-at-the-unit-bound", "authenticate(")]
	[InlineData("EX-PY-127", "flow-long-line-framework-session-subscripts", "request.session[")]
	[InlineData("EX-PY-130", "flow-long-line-environment-openers-without-a-key", "os.environ[")]
	[InlineData("EX-PY-131", "flow-long-line-configuration-receivers-with-credential-members", "config.db_password")]
	[InlineData("EX-PY-132", "flow-long-line-authenticators-calls", ".authenticators(")]
	[InlineData("EX-PY-133", "flow-long-line-credential-named-assignments-with-f-strings", "api_token = f")]
	public void EveryFlowSignalExpressionHasAShapeThatHoldsItsAnchor(string expressionId, string shapeSlug, string anchorText)
	{
		MatchBoundStressShape shape = Assert.Single(PythonShippedCostGateStressSet.Build(), candidate => candidate.Name == $"{PythonShippedCostGateStressSet.SHAPE_NAME_PREFIX}{shapeSlug}");

		Assert.True(shape.Text.Contains(anchorText, StringComparison.Ordinal), $"{expressionId}: {shape.Name} holds no {anchorText}");
	}

	[Fact]
	public void TheLineCeilingShapeOfTheArgumentWindowHasNoLineShorterThanTheCharacterSpanThatCapsTheWindow()
	{
		MatchBoundStressShape capped = Assert.Single(
			PythonShippedCostGateStressSet.BuildTheArgumentWindowShapes(),
			shape => shape.Name.EndsWith("capped-at-the-line-ceiling-of-characters", StringComparison.Ordinal));

		Assert.All(
			capped.Text.Split(_LINE_FEED, StringSplitOptions.RemoveEmptyEntries),
			line => Assert.InRange(line.Length, PythonShippedCostGateStressSet.LONG_LINE_LENGTH_IN_CHARACTERS - 2, PythonShippedCostGateStressSet.LONG_LINE_LENGTH_IN_CHARACTERS));
	}

	[Fact]
	public void ThePerFileWorstShapesArePresentAtTheCeilingAndTheGeneratedStubStandsAtTheGeneratedSuffix()
	{
		ImmutableArray<MatchBoundStressShape> perFile = PythonShippedCostGateStressSet.BuildThePerFileWorstShapes();

		Assert.Equal(_perFileWorstFamilies.Length, perFile.Length);
		Assert.All(_perFileWorstFamilies, family => Assert.Single(perFile, shape => shape.Name == $"{PythonShippedCostGateStressSet.SHAPE_NAME_PREFIX}{family}"));
		Assert.All(perFile, shape => Assert.True(
			shape.Text.Length > ReadingCeilings.MAXIMUM_FILE_SIZE_IN_BYTES / _CEILING_FRACTION_DIVISOR,
			$"{shape.Name} is {shape.Text.Length} characters"));
		Assert.EndsWith(
			_GENERATED_STUB_SUFFIX,
			PythonShippedCostGateStressSet.RelativePathOf(Assert.Single(perFile, shape => shape.Name == PythonShippedCostGateStressSet.GENERATED_STUB_SHAPE_NAME)),
			StringComparison.Ordinal);
	}

	[Theory]
	[InlineData("ARGUMENT_WINDOW_UNITS", "_ARGUMENT_WINDOW_UNITS")]
	[InlineData("NESTING_DEPTH", "_NESTING_DEPTH")]
	[InlineData("NESTED_GROUP_UNITS", "_NESTED_GROUP_UNITS")]
	[InlineData("STRING_UNIT_CHARACTERS", "_STRING_BODY_CHARACTERS")]
	public void TheBoundsTheShapesAreBuiltFromAreTheOnesTheVocabularyOfTheExpressionsDeclares(string shapeConstantName, string vocabularyConstantName)
	{
		Type vocabulary = typeof(EmbeddedLanguageAnalyzers).Assembly.GetType(_VOCABULARY_TYPE_NAME, throwOnError: true)!;
		FieldInfo field = vocabulary.GetField(vocabularyConstantName, BindingFlags.NonPublic | BindingFlags.Static)
			?? throw new InvalidOperationException($"The vocabulary declares no constant named {vocabularyConstantName}.");
		FieldInfo shapeField = typeof(PythonShippedCostGateStressSet).GetField(shapeConstantName, BindingFlags.NonPublic | BindingFlags.Static)
			?? throw new InvalidOperationException($"The stress set declares no constant named {shapeConstantName}.");

		Assert.Equal(field.GetRawConstantValue(), shapeField.GetRawConstantValue());
	}
}
