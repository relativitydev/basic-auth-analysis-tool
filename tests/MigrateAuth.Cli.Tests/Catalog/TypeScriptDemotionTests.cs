using System.Collections.Immutable;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Recognition;
using MigrateAuth.Cli.Tests.Recognition;

using Xunit;

namespace MigrateAuth.Cli.Tests.Catalog;

/// <summary>
/// THE DEMOTION SIGNALS OF THE SHIPPED `TypeScript` ANALYZER, one fixture per kind, run through the real recognition engine.
///
/// The same High construct sits in every file, so the level read measures the kinds that fired and nothing else: a test kind alone proposes Medium and carries the test-code mark, any other
/// kind proposes Low, the minimum is taken once. The folder signals and the two content markers are the `JavaScript` ones by reference; the file-name suffix signals are rebuilt from the five
/// stems crossed with the four `TypeScript` extensions and held to the `JavaScript` ones by the parity test below; the three declaration-file suffixes are Generated. A test that expects a
/// request and reads its header is a near-miss and not a test-demoted Medium. Every script is invented, with placeholder names.
/// </summary>
public sealed class TypeScriptDemotionTests
{
	private const string _HIGH_CONSTRUCT = "await fetch(url, { headers: { Authorization: `Basic ${encoded}` } });\n";
	private const string _HIGH_CLASS = "JS-004";
	private const int _TEST_SUFFIX_COUNT = 8;
	private const int _MOCK_SUFFIX_COUNT = 12;
	private const int _DECLARATION_SUFFIX_COUNT = 3;
	private const int _PREFIX_SIGNAL_COUNT = 1;
	private const int _FILE_NAME_SIGNAL_COUNT = _TEST_SUFFIX_COUNT + _MOCK_SUFFIX_COUNT + _DECLARATION_SUFFIX_COUNT + _PREFIX_SIGNAL_COUNT;
	private const int _FOLDER_NAME_SIGNAL_COUNT = 25;
	private const int _CONTENT_MARKER_COUNT = 2;

	private static readonly ImmutableDictionary<string, string> _typeScriptExtensionOfEachJavaScriptOne = ImmutableDictionary<string, string>.Empty
		.Add(".js", ".ts")
		.Add(".mjs", ".mts")
		.Add(".cjs", ".cts")
		.Add(".jsx", ".tsx");

	// ------------------------------------------------------------------------------------------
	// What is declared
	// ------------------------------------------------------------------------------------------

	[Fact]
	public void EverySignalIsInsensitiveAndTheCountsAreThoseOfTheCatalog()
	{
		LanguageShapedDemotionSignals signals = TypeScriptRegisteredExpressions.Analyzer.DemotionSignals;

		Assert.Equal(_FILE_NAME_SIGNAL_COUNT, signals.FileNameSignals.Count);
		Assert.Equal(_FOLDER_NAME_SIGNAL_COUNT, signals.FolderNameSignals.Count);
		Assert.Equal(_CONTENT_MARKER_COUNT, signals.ContentMarkers.Length);
		Assert.Equal(_TEST_SUFFIX_COUNT, SuffixSignals(signals, DemotionKind.Test).Count());
		Assert.Equal(_MOCK_SUFFIX_COUNT, SuffixSignals(signals, DemotionKind.Mock).Count());
		Assert.Equal(_DECLARATION_SUFFIX_COUNT, SuffixSignals(signals, DemotionKind.Generated).Count());
		Assert.Equal(_PREFIX_SIGNAL_COUNT, signals.FileNameSignals.Count(signal => signal.Match == FileNameMatch.Prefix && signal.Kind == DemotionKind.Mock));
		Assert.All(signals.FileNameSignals, signal => Assert.Equal(CaseSensitivity.Insensitive, signal.Sensitivity));
		Assert.All(signals.FolderNameSignals, signal => Assert.Equal(CaseSensitivity.Insensitive, signal.Sensitivity));
	}

	[Fact]
	public void TheSuffixSignalsAreTheFiveStemsCrossedWithTheFourExtensionsAndTheThreeDeclarationSuffixes()
	{
		LanguageShapedDemotionSignals signals = TypeScriptRegisteredExpressions.Analyzer.DemotionSignals;
		string[] extensions = [".ts", ".tsx", ".mts", ".cts"];

		Assert.Equal(
			new[] { ".test", ".spec" }.SelectMany(stem => extensions.Select(extension => $"{stem}{extension}")).Order(StringComparer.Ordinal),
			SuffixSignals(signals, DemotionKind.Test).Select(signal => signal.Text).Order(StringComparer.Ordinal));
		Assert.Equal(
			new[] { ".mock", ".stub", ".fake" }.SelectMany(stem => extensions.Select(extension => $"{stem}{extension}")).Order(StringComparer.Ordinal),
			SuffixSignals(signals, DemotionKind.Mock).Select(signal => signal.Text).Order(StringComparer.Ordinal));
		Assert.Equal(
			new[] { ".d.cts", ".d.mts", ".d.ts" },
			SuffixSignals(signals, DemotionKind.Generated).Select(signal => signal.Text).Order(StringComparer.Ordinal));
	}

	/// <summary>The parity guard: each file-name signal of `JavaScript` has its `TypeScript` twin and no other suffix signal exists but the declaration files, so a stem added to one is a decision for both.</summary>
	[Fact]
	public void TheSuffixSignalsMirrorTheFileNameSignalsOfJavaScriptWithTheExtensionsMapped()
	{
		LanguageShapedDemotionSignals javaScript = JavaScriptRegisteredExpressions.Analyzer.DemotionSignals;
		LanguageShapedDemotionSignals typeScript = TypeScriptRegisteredExpressions.Analyzer.DemotionSignals;

		(string Text, DemotionKind Kind)[] expected =
		[
			.. javaScript.FileNameSignals
				.Where(signal => signal.Match == FileNameMatch.Suffix)
				.Select(signal => (MapExtension(signal.Text), signal.Kind))
		];
		(string Text, DemotionKind Kind)[] actual =
		[
			.. typeScript.FileNameSignals
				.Where(signal => signal.Match == FileNameMatch.Suffix && !IsADeclarationSuffix(signal))
				.Select(signal => (signal.Text, signal.Kind))
		];

		Assert.NotEmpty(expected);
		Assert.Equal(expected.Order().ToArray(), actual.Order().ToArray());
		Assert.Equal(
			javaScript.FileNameSignals.Where(signal => signal.Match == FileNameMatch.Prefix).Select(signal => (signal.Text, signal.Kind, signal.Sensitivity)).Order().ToArray(),
			typeScript.FileNameSignals.Where(signal => signal.Match == FileNameMatch.Prefix).Select(signal => (signal.Text, signal.Kind, signal.Sensitivity)).Order().ToArray());
	}

	[Fact]
	public void TheFolderSignalsAndTheContentMarkersAreTheJavaScriptOnesByReference()
	{
		LanguageShapedDemotionSignals javaScript = JavaScriptRegisteredExpressions.Analyzer.DemotionSignals;
		LanguageShapedDemotionSignals typeScript = TypeScriptRegisteredExpressions.Analyzer.DemotionSignals;

		Assert.Equal(
			javaScript.FolderNameSignals.Select(signal => (signal.FolderName, signal.Kind, signal.Sensitivity)).Order().ToArray(),
			typeScript.FolderNameSignals.Select(signal => (signal.FolderName, signal.Kind, signal.Sensitivity)).Order().ToArray());
		Assert.Equal(javaScript.ContentMarkers.Length, typeScript.ContentMarkers.Length);

		for (int index = 0; index < javaScript.ContentMarkers.Length; index++)
		{
			Assert.Same(javaScript.ContentMarkers[index].Expression, typeScript.ContentMarkers[index].Expression);
			Assert.Equal(javaScript.ContentMarkers[index].Kind, typeScript.ContentMarkers[index].Kind);
			Assert.Equal(javaScript.ContentMarkers[index].Zone, typeScript.ContentMarkers[index].Zone);
		}
	}

	[Fact]
	public void NoDeploymentSignalNoMinifiedAnalogueAndNoSignalForATestingModuleIsDeclared()
	{
		LanguageShapedDemotionSignals signals = TypeScriptRegisteredExpressions.Analyzer.DemotionSignals;
		IEnumerable<DemotionKind> kinds = signals.FileNameSignals.Select(signal => signal.Kind)
			.Concat(signals.FolderNameSignals.Select(signal => signal.Kind))
			.Concat(signals.ContentMarkers.Select(marker => marker.Kind));

		Assert.DoesNotContain(DemotionKind.Deployment, kinds);
		Assert.Empty(signals.GenerationMarkerExpressions);
		Assert.DoesNotContain(signals.FileNameSignals, signal => signal.Text.Contains(".min.", StringComparison.OrdinalIgnoreCase));
		Assert.DoesNotContain(signals.FileNameSignals, signal => signal.Text.Contains("Testing", StringComparison.OrdinalIgnoreCase));
		Assert.All(signals.ContentMarkers, marker => Assert.Equal(RecognitionEnginePolicy.MATCH_BOUND, marker.Expression.MatchTimeout));
	}

	// ------------------------------------------------------------------------------------------
	// A fixture per kind, by file and folder name
	// ------------------------------------------------------------------------------------------

	[Theory]
	[InlineData("src/client.ts", ConfidenceLevel.High, false)]
	[InlineData("src/helper.mts", ConfidenceLevel.High, false)]
	[InlineData("src/helper.cts", ConfidenceLevel.High, false)]
	[InlineData("src/view.tsx", ConfidenceLevel.High, false)]
	[InlineData("Deployment/client.ts", ConfidenceLevel.High, false)]
	[InlineData("deployment/scripts/client.ts", ConfidenceLevel.High, false)]
	[InlineData("integration/client.ts", ConfidenceLevel.High, false)]
	[InlineData("tests/client.ts", ConfidenceLevel.Medium, true)]
	[InlineData("test/client.ts", ConfidenceLevel.Medium, true)]
	[InlineData("__tests__/client.ts", ConfidenceLevel.Medium, true)]
	[InlineData("spec/client.ts", ConfidenceLevel.Medium, true)]
	[InlineData("e2e/client.ts", ConfidenceLevel.Medium, true)]
	[InlineData("fixtures/client.ts", ConfidenceLevel.Medium, true)]
	[InlineData("cypress/client.ts", ConfidenceLevel.Medium, true)]
	[InlineData("src/client.test.ts", ConfidenceLevel.Medium, true)]
	[InlineData("src/client.test.tsx", ConfidenceLevel.Medium, true)]
	[InlineData("src/client.test.mts", ConfidenceLevel.Medium, true)]
	[InlineData("src/client.test.cts", ConfidenceLevel.Medium, true)]
	[InlineData("src/login.spec.ts", ConfidenceLevel.Medium, true)]
	[InlineData("src/login.spec.tsx", ConfidenceLevel.Medium, true)]
	[InlineData("src/login.spec.mts", ConfidenceLevel.Medium, true)]
	[InlineData("src/login.spec.cts", ConfidenceLevel.Medium, true)]
	[InlineData("src/LOGIN.SPEC.TS", ConfidenceLevel.Medium, true)]
	[InlineData("src/latest.ts", ConfidenceLevel.High, false)]
	[InlineData("src/contest.ts", ConfidenceLevel.High, false)]
	[InlineData("src/testing.ts", ConfidenceLevel.High, false)]
	[InlineData("src/inspect.ts", ConfidenceLevel.High, false)]
	[InlineData("mocks/client.ts", ConfidenceLevel.Low, false)]
	[InlineData("__mocks__/client.ts", ConfidenceLevel.Low, false)]
	[InlineData("stubs/client.ts", ConfidenceLevel.Low, false)]
	[InlineData("fakes/client.ts", ConfidenceLevel.Low, false)]
	[InlineData("src/client.mock.ts", ConfidenceLevel.Low, false)]
	[InlineData("src/client.stub.mts", ConfidenceLevel.Low, false)]
	[InlineData("src/client.fake.cts", ConfidenceLevel.Low, false)]
	[InlineData("src/client.mock.tsx", ConfidenceLevel.Low, false)]
	[InlineData("samples/client.ts", ConfidenceLevel.Low, false)]
	[InlineData("examples/client.ts", ConfidenceLevel.Low, false)]
	[InlineData("demo/client.ts", ConfidenceLevel.Low, false)]
	[InlineData("src/exampleClient.ts", ConfidenceLevel.High, false)]
	[InlineData("vendor/client.ts", ConfidenceLevel.Low, false)]
	[InlineData("third_party/client.ts", ConfidenceLevel.Low, false)]
	[InlineData("tests/mocks/client.ts", ConfidenceLevel.Low, true)]
	[InlineData("src/mock_server.test.ts", ConfidenceLevel.Low, true)]
	[InlineData("src/mock-server.spec.mts", ConfidenceLevel.Medium, true)]
	[InlineData("src/mock_client.ts", ConfidenceLevel.Low, false)]
	[InlineData("src/mockery.ts", ConfidenceLevel.High, false)]
	[InlineData("spec/vendor/client.ts", ConfidenceLevel.Low, true)]
	[InlineData("src/credentials.d.ts", ConfidenceLevel.Low, false)]
	[InlineData("src/types.d.mts", ConfidenceLevel.Low, false)]
	[InlineData("src/legacy.d.cts", ConfidenceLevel.Low, false)]
	[InlineData("src/CREDENTIALS.D.TS", ConfidenceLevel.Low, false)]
	[InlineData("tests/credentials.d.ts", ConfidenceLevel.Low, true)]
	[InlineData("src/xd.ts", ConfidenceLevel.High, false)]
	public void AHighConstructIsDemotedByTheNameOfItsFileAndFolderAndByNothingElse(string relativePath, ConfidenceLevel expected, bool expectedTestCode)
	{
		RecognizedConstruct construct = Recognize(_HIGH_CONSTRUCT, relativePath);

		Assert.Equal(expected, construct.Level);
		Assert.Equal(expectedTestCode, construct.TestCode);
	}

	[Fact]
	public void ASpecFileHoldingAHighConstructReadsMediumAndCarriesTheTestCodeMark()
	{
		RecognizedConstruct construct = Recognize(_HIGH_CONSTRUCT, "src/login.spec.ts");

		Assert.Equal(ConfidenceLevel.Medium, construct.Level);
		Assert.True(construct.TestCode);
		Assert.Equal(LowConfidenceReasons.None, construct.LowReasons);
	}

	[Fact]
	public void ATestKindWithAMockKindReadsLowAndKeepsTheTestCodeMark()
	{
		RecognizedConstruct construct = Recognize(_HIGH_CONSTRUCT, "tests/mocks/client.ts");

		Assert.Equal(ConfidenceLevel.Low, construct.Level);
		Assert.True(construct.TestCode);
		Assert.Equal(LowConfidenceReasons.DemotedMockFile, construct.LowReasons);
	}

	[Fact]
	public void AMockFileReadsLowWithTheMockReasonAndAFolderNamedSpecHoldingASourceFileReadsMediumInsteadOfHigh()
	{
		Assert.Equal(LowConfidenceReasons.DemotedMockFile, Recognize(_HIGH_CONSTRUCT, "src/client.mock.ts").LowReasons);
		Assert.Equal(ConfidenceLevel.Medium, Recognize(_HIGH_CONSTRUCT, "spec/client.ts").Level);
	}

	[Theory]
	[InlineData("src/credentials.d.ts")]
	[InlineData("src/types.d.mts")]
	[InlineData("src/legacy.d.cts")]
	public void AFindingInADeclarationFileReadsLowWithTheGeneratedReasonAndNoTestCodeMark(string relativePath)
	{
		RecognizedConstruct construct = Recognize(_HIGH_CONSTRUCT, relativePath);

		Assert.Equal(ConfidenceLevel.Low, construct.Level);
		Assert.False(construct.TestCode);
		Assert.Equal(LowConfidenceReasons.DemotedGeneratedFile, construct.LowReasons);
	}

	[Fact]
	public void AFolderNamedDeploymentDemotesNothingAndAHighConstructInItReadsHigh()
	{
		RecognizedConstruct construct = Recognize(_HIGH_CONSTRUCT, "deployment/scripts/client.ts");

		Assert.Equal(ConfidenceLevel.High, construct.Level);
		Assert.False(construct.TestCode);
		Assert.Equal(LowConfidenceReasons.None, construct.LowReasons);
	}

	// ------------------------------------------------------------------------------------------
	// A fixture per content marker
	// ------------------------------------------------------------------------------------------

	[Theory]
	[InlineData($"// This file was auto-generated\n{_HIGH_CONSTRUCT}", ConfidenceLevel.Low, false)]
	[InlineData($"/* @generated */\n{_HIGH_CONSTRUCT}", ConfidenceLevel.Low, false)]
	[InlineData($"'use strict';\n// DO NOT EDIT - changes will be overwritten\n{_HIGH_CONSTRUCT}", ConfidenceLevel.Low, false)]
	[InlineData($"{_HIGH_CONSTRUCT}// Generated by build.ts\n", ConfidenceLevel.High, false)]
	[InlineData($"describe('client', () => {{\n\tit('sends a request', async () => {{\n\t\t{_HIGH_CONSTRUCT}\t}});\n}});\n", ConfidenceLevel.Medium, true)]
	[InlineData($"describe('client', () => {{\n\ttest('sends a request', async () => {{\n\t\t{_HIGH_CONSTRUCT}\t}});\n}});\n", ConfidenceLevel.Medium, true)]
	[InlineData($"describe('client', () => {{\n\tconsole.log('ready');\n}});\n{_HIGH_CONSTRUCT}", ConfidenceLevel.High, false)]
	public void AHighConstructIsDemotedByTheContentMarkersWhereTheyStandInTheirZone(string source, ConfidenceLevel expected, bool expectedTestCode)
	{
		RecognizedConstruct construct = Recognize(source);

		Assert.Equal(expected, construct.Level);
		Assert.Equal(expectedTestCode, construct.TestCode);
	}

	// ------------------------------------------------------------------------------------------
	// A request read in a test is a near-miss, not a test-demoted Medium
	// ------------------------------------------------------------------------------------------

	[Theory]
	[InlineData("const req = httpMock.expectOne(url);\nexpect(req.request.headers.get('Authorization')).toBe(expected);\n")]
	[InlineData("const req = controller.expectOne(url);\nconst header = req.request.headers.get('authorization');\nexpect(header).toMatch(/^Basic /);\n")]
	[InlineData("nock(host).matchHeader('authorization', value => value.startsWith('Basic')).reply(200);\n")]
	public void AnAngularTestControllerOrAMockServerMatcherInASpecFileReportsNothingAndIsNotReportedAsATestConstruct(string source)
	{
		ImmutableArray<RecognizedConstruct> constructs = new TypeScriptRecognitionHarness().Recognize(source, "src/client.spec.ts");

		Assert.True(constructs.IsEmpty, $"[{source}] produced [{string.Join(", ", constructs.Select(construct => $"{construct.PatternClassIdentifier}:{construct.Level}"))}]");
	}

	// ------------------------------------------------------------------------------------------
	// Helpers
	// ------------------------------------------------------------------------------------------

	private static IEnumerable<FileNameSignal> SuffixSignals(LanguageShapedDemotionSignals signals, DemotionKind kind)
	{
		return signals.FileNameSignals.Where(signal => signal.Match == FileNameMatch.Suffix && signal.Kind == kind);
	}

	private static bool IsADeclarationSuffix(FileNameSignal signal)
	{
		return signal.Kind == DemotionKind.Generated && signal.Text.StartsWith(".d.", StringComparison.Ordinal);
	}

	private static string MapExtension(string javaScriptSuffix)
	{
		int lastDot = javaScriptSuffix.LastIndexOf('.');

		return string.Concat(javaScriptSuffix.AsSpan(0, lastDot), _typeScriptExtensionOfEachJavaScriptOne[javaScriptSuffix[lastDot..]]);
	}

	private static RecognizedConstruct Recognize(string source, string relativePath = TypeScriptRecognitionHarness.UNDEMOTED_RELATIVE_PATH)
	{
		ImmutableArray<RecognizedConstruct> constructs = new TypeScriptRecognitionHarness().Recognize(source, relativePath);

		return constructs.Single(construct => construct.PatternClassIdentifier == _HIGH_CLASS);
	}
}
