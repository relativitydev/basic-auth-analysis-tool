using System.Collections.Immutable;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Recognition;
using MigrateAuth.Cli.Tests.Recognition;

using Xunit;

namespace MigrateAuth.Cli.Tests.Catalog;

/// <summary>
/// THE DEMOTION SIGNALS OF THE SHIPPED `JavaScript` ANALYZER, one fixture per kind, run through the real recognition engine.
///
/// The same High construct sits in every file, so the level read measures the kinds that fired and nothing else: a test kind alone proposes Medium and carries the test-code
/// mark, any other kind proposes Low, the minimum is taken once, and a folder named for a deployment demotes nothing. A mock-server matcher inside a test file is a near-miss
/// and not a construct, so it is reported as nothing at all, never as a test-demoted Medium. Every script is invented, with placeholder names.
/// </summary>
public sealed class JavaScriptDemotionTests
{
	private const string _HIGH_CONSTRUCT = "await fetch(url, { headers: { Authorization: `Basic ${encoded}` } });\n";
	private const string _HIGH_CLASS = "JS-004";
	private const int _SUFFIX_SIGNAL_COUNT = 20;
	private const int _PREFIX_SIGNAL_COUNT = 1;
	private const int _FILE_NAME_SIGNAL_COUNT = _SUFFIX_SIGNAL_COUNT + _PREFIX_SIGNAL_COUNT;
	private const int _FOLDER_NAME_SIGNAL_COUNT = 25;
	private const int _CONTENT_MARKER_COUNT = 2;

	// ------------------------------------------------------------------------------------------
	// What is declared
	// ------------------------------------------------------------------------------------------

	[Fact]
	public void EverySignalIsInsensitiveAndTheCountsAreThoseOfTheCatalog()
	{
		LanguageShapedDemotionSignals signals = JavaScriptRegisteredExpressions.Analyzer.DemotionSignals;

		Assert.Equal(_FILE_NAME_SIGNAL_COUNT, signals.FileNameSignals.Count);
		Assert.Equal(_FOLDER_NAME_SIGNAL_COUNT, signals.FolderNameSignals.Count);
		Assert.Equal(_CONTENT_MARKER_COUNT, signals.ContentMarkers.Length);
		Assert.Single(signals.GenerationMarkerExpressions);
		Assert.Equal(_SUFFIX_SIGNAL_COUNT, signals.FileNameSignals.Count(signal => signal.Match == FileNameMatch.Suffix));
		Assert.Equal(_PREFIX_SIGNAL_COUNT, signals.FileNameSignals.Count(signal => signal.Match == FileNameMatch.Prefix && signal.Kind == DemotionKind.Mock));
		Assert.All(signals.FileNameSignals, signal => Assert.Equal(CaseSensitivity.Insensitive, signal.Sensitivity));
		Assert.All(signals.FolderNameSignals, signal => Assert.Equal(CaseSensitivity.Insensitive, signal.Sensitivity));
		Assert.All(signals.FolderNameSignals, signal => Assert.Equal(FolderNameMatch.ExactName, signal.Match));
	}

	[Fact]
	public void NoDeploymentSignalAndNoSignalForAnExcludedFolderIsDeclaredAndTheKindsAreTheOnesTheCatalogNames()
	{
		LanguageShapedDemotionSignals signals = JavaScriptRegisteredExpressions.Analyzer.DemotionSignals;
		IEnumerable<DemotionKind> kinds = signals.FileNameSignals.Select(signal => signal.Kind)
			.Concat(signals.FolderNameSignals.Select(signal => signal.Kind))
			.Concat(signals.ContentMarkers.Select(marker => marker.Kind));
		string[] undeclaredFolders = ["Deployment", "node_modules", "dist", "build", "Dist", "BUILD"];

		Assert.DoesNotContain(DemotionKind.Deployment, kinds);
		Assert.All(undeclaredFolders, folder => Assert.DoesNotContain(signals.FolderNameSignals, signal => signal.FolderName.Equals(folder, StringComparison.OrdinalIgnoreCase)));
		Assert.Equal(
			new[] { DemotionKind.Generated, DemotionKind.Mock, DemotionKind.Sample, DemotionKind.Test, DemotionKind.Vendored }.Order(),
			kinds.Distinct().Order());
	}

	[Fact]
	public void TheContentMarkersDeclareTheZonesTheCatalogGivesThemAndEveryExpressionCarriesThePolicyBound()
	{
		LanguageShapedDemotionSignals signals = JavaScriptRegisteredExpressions.Analyzer.DemotionSignals;

		ContentMarker generated = Assert.Single(signals.ContentMarkers, marker => marker.Kind == DemotionKind.Generated);
		ContentMarker test = Assert.Single(signals.ContentMarkers, marker => marker.Kind == DemotionKind.Test);
		Assert.Equal(ContentMarkerZone.AnyZone, generated.Zone);
		Assert.Equal(ContentMarkerZone.CodeOnly, test.Zone);
		Assert.All(signals.ContentMarkers, marker => Assert.Equal(RecognitionEnginePolicy.MATCH_BOUND, marker.Expression.MatchTimeout));
		Assert.All(signals.GenerationMarkerExpressions, expression => Assert.Equal(RecognitionEnginePolicy.MATCH_BOUND, expression.MatchTimeout));
	}

	// ------------------------------------------------------------------------------------------
	// A fixture per kind, by file and folder name
	// ------------------------------------------------------------------------------------------

	[Theory]
	[InlineData("src/client.js", ConfidenceLevel.High, false)]
	[InlineData("src/helper.mjs", ConfidenceLevel.High, false)]
	[InlineData("Deployment/client.js", ConfidenceLevel.High, false)]
	[InlineData("deployment/scripts/client.js", ConfidenceLevel.High, false)]
	[InlineData("Dist/client.js", ConfidenceLevel.High, false)]
	[InlineData("BUILD/client.js", ConfidenceLevel.High, false)]
	[InlineData("integration/client.js", ConfidenceLevel.High, false)]
	[InlineData("tests/client.js", ConfidenceLevel.Medium, true)]
	[InlineData("test/client.js", ConfidenceLevel.Medium, true)]
	[InlineData("__tests__/client.js", ConfidenceLevel.Medium, true)]
	[InlineData("spec/client.js", ConfidenceLevel.Medium, true)]
	[InlineData("e2e/client.js", ConfidenceLevel.Medium, true)]
	[InlineData("fixture/client.js", ConfidenceLevel.Medium, true)]
	[InlineData("fixtures/client.js", ConfidenceLevel.Medium, true)]
	[InlineData("cypress/client.js", ConfidenceLevel.Medium, true)]
	[InlineData("Tests/Client.js", ConfidenceLevel.Medium, true)]
	[InlineData("src/client.test.js", ConfidenceLevel.Medium, true)]
	[InlineData("src/client.test.mjs", ConfidenceLevel.Medium, true)]
	[InlineData("src/client.test.cjs", ConfidenceLevel.Medium, true)]
	[InlineData("src/client.test.jsx", ConfidenceLevel.Medium, true)]
	[InlineData("src/client.spec.js", ConfidenceLevel.Medium, true)]
	[InlineData("src/client.spec.mjs", ConfidenceLevel.Medium, true)]
	[InlineData("src/client.spec.cjs", ConfidenceLevel.Medium, true)]
	[InlineData("src/login.spec.jsx", ConfidenceLevel.Medium, true)]
	[InlineData("src/CLIENT.TEST.JS", ConfidenceLevel.Medium, true)]
	[InlineData("src/latest.js", ConfidenceLevel.High, false)]
	[InlineData("src/contest.js", ConfidenceLevel.High, false)]
	[InlineData("src/testing.js", ConfidenceLevel.High, false)]
	[InlineData("src/inspect.js", ConfidenceLevel.High, false)]
	[InlineData("mocks/client.js", ConfidenceLevel.Low, false)]
	[InlineData("mocks/server.test.js", ConfidenceLevel.Low, true)]
	[InlineData("mock/client.js", ConfidenceLevel.Low, false)]
	[InlineData("__mocks__/client.js", ConfidenceLevel.Low, false)]
	[InlineData("stub/client.js", ConfidenceLevel.Low, false)]
	[InlineData("stubs/client.js", ConfidenceLevel.Low, false)]
	[InlineData("fake/client.js", ConfidenceLevel.Low, false)]
	[InlineData("fakes/client.js", ConfidenceLevel.Low, false)]
	[InlineData("src/client.mock.js", ConfidenceLevel.Low, false)]
	[InlineData("src/client.stub.mjs", ConfidenceLevel.Low, false)]
	[InlineData("src/client.fake.cjs", ConfidenceLevel.Low, false)]
	[InlineData("src/client.mock.jsx", ConfidenceLevel.Low, false)]
	[InlineData("samples/client.js", ConfidenceLevel.Low, false)]
	[InlineData("sample/client.js", ConfidenceLevel.Low, false)]
	[InlineData("example/client.js", ConfidenceLevel.Low, false)]
	[InlineData("examples/client.js", ConfidenceLevel.Low, false)]
	[InlineData("demo/client.js", ConfidenceLevel.Low, false)]
	[InlineData("demos/client.js", ConfidenceLevel.Low, false)]
	[InlineData("src/exampleClient.js", ConfidenceLevel.High, false)]
	[InlineData("vendor/client.js", ConfidenceLevel.Low, false)]
	[InlineData("third-party/client.js", ConfidenceLevel.Low, false)]
	[InlineData("third_party/client.js", ConfidenceLevel.Low, false)]
	[InlineData("bower_components/client.js", ConfidenceLevel.Low, false)]
	[InlineData("static/jquery.min.js", ConfidenceLevel.Low, false)]
	[InlineData("static/app.bundle.js", ConfidenceLevel.Low, false)]
	[InlineData("static/app.chunk.js", ConfidenceLevel.Low, false)]
	[InlineData("static/APP.MIN.JS", ConfidenceLevel.Low, false)]
	[InlineData("static/app.min.mjs", ConfidenceLevel.High, false)]
	[InlineData("tests/mocks/client.js", ConfidenceLevel.Low, true)]
	[InlineData("src/mock_server.test.js", ConfidenceLevel.Low, true)]
	[InlineData("src/mock-server.spec.mjs", ConfidenceLevel.Medium, true)]
	[InlineData("src/mock_client.js", ConfidenceLevel.Low, false)]
	[InlineData("src/mockery.js", ConfidenceLevel.High, false)]
	[InlineData("src/server.mock.test.js", ConfidenceLevel.Medium, true)]
	[InlineData("spec/vendor/client.js", ConfidenceLevel.Low, true)]
	[InlineData("tests/app.min.js", ConfidenceLevel.Low, true)]
	public void AHighConstructIsDemotedByTheNameOfItsFileAndFolderAndByNothingElse(string relativePath, ConfidenceLevel expected, bool expectedTestCode)
	{
		RecognizedConstruct construct = Recognize(_HIGH_CONSTRUCT, relativePath);

		Assert.Equal(expected, construct.Level);
		Assert.Equal(expectedTestCode, construct.TestCode);
	}

	[Fact]
	public void ATestKindAloneReadsMediumWithNoLowReasonAndEveryOtherKindReadsLowWithItsOwnReason()
	{
		Assert.Equal(LowConfidenceReasons.None, Recognize(_HIGH_CONSTRUCT, "tests/client.js").LowReasons);
		Assert.Equal(LowConfidenceReasons.DemotedMockFile, Recognize(_HIGH_CONSTRUCT, "mocks/client.js").LowReasons);
		Assert.Equal(LowConfidenceReasons.DemotedSampleFile, Recognize(_HIGH_CONSTRUCT, "samples/client.js").LowReasons);
		Assert.Equal(LowConfidenceReasons.DemotedVendoredFile, Recognize(_HIGH_CONSTRUCT, "vendor/client.js").LowReasons);
		Assert.Equal(LowConfidenceReasons.DemotedGeneratedFile, Recognize(_HIGH_CONSTRUCT, "static/app.min.js").LowReasons);
	}

	[Fact]
	public void ATestKindWithAMockKindReadsLowAndKeepsTheTestCodeMark()
	{
		RecognizedConstruct construct = Recognize(_HIGH_CONSTRUCT, "tests/mocks/client.js");

		Assert.Equal(ConfidenceLevel.Low, construct.Level);
		Assert.True(construct.TestCode);
		Assert.Equal(LowConfidenceReasons.DemotedMockFile, construct.LowReasons);
	}

	[Fact]
	public void AFolderNamedDeploymentDemotesNothingAndAHighConstructInItReadsHigh()
	{
		RecognizedConstruct construct = Recognize(_HIGH_CONSTRUCT, "deployment/scripts/client.js");

		Assert.Equal(ConfidenceLevel.High, construct.Level);
		Assert.False(construct.TestCode);
		Assert.Equal(LowConfidenceReasons.None, construct.LowReasons);
	}

	// ------------------------------------------------------------------------------------------
	// A fixture per content marker
	// ------------------------------------------------------------------------------------------

	[Theory]
	[InlineData("// This file was auto-generated\n" + _HIGH_CONSTRUCT, ConfidenceLevel.Low, false)]
	[InlineData("// AUTOGENERATED\n" + _HIGH_CONSTRUCT, ConfidenceLevel.Low, false)]
	[InlineData("/* @generated */\n" + _HIGH_CONSTRUCT, ConfidenceLevel.Low, false)]
	[InlineData("#!/usr/bin/env node\n// Generated by build.js\n" + _HIGH_CONSTRUCT, ConfidenceLevel.Low, false)]
	[InlineData("'use strict';\n// DO NOT EDIT - changes will be overwritten\n" + _HIGH_CONSTRUCT, ConfidenceLevel.Low, false)]
	[InlineData("#!/usr/bin/env node\nconst x = 1;\n// Report generated by this script\n" + _HIGH_CONSTRUCT, ConfidenceLevel.High, false)]
	[InlineData("const note = 'generated by';\n" + _HIGH_CONSTRUCT, ConfidenceLevel.High, false)]
	[InlineData(_HIGH_CONSTRUCT + "// Generated by build.js\n", ConfidenceLevel.High, false)]
	[InlineData("describe('client', () => {\n\tit('sends a request', () => {\n\t\t" + _HIGH_CONSTRUCT + "\t});\n});\n", ConfidenceLevel.Medium, true)]
	[InlineData("describe('client', () => {\n\ttest('sends a request', () => {\n\t\t" + _HIGH_CONSTRUCT + "\t});\n});\n", ConfidenceLevel.Medium, true)]
	[InlineData("describe('client', () => {\n\tconsole.log('ready');\n});\n" + _HIGH_CONSTRUCT, ConfidenceLevel.High, false)]
	[InlineData("// describe('client', () => {\n// it('sends a request', () => {});\n" + _HIGH_CONSTRUCT, ConfidenceLevel.High, false)]
	[InlineData("const note = \"describe('x'\";\nconst more = \"it('y'\";\n" + _HIGH_CONSTRUCT, ConfidenceLevel.High, false)]
	public void AHighConstructIsDemotedByTheContentMarkersWhereTheyStandInTheirZone(string source, ConfidenceLevel expected, bool expectedTestCode)
	{
		RecognizedConstruct construct = Recognize(source);

		Assert.Equal(expected, construct.Level);
		Assert.Equal(expectedTestCode, construct.TestCode);
	}

	[Fact]
	public void AGeneratedBannerReadsLowWithTheGeneratedReasonAndAGeneratedTestFileReadsLowAndKeepsTheMark()
	{
		RecognizedConstruct generated = Recognize("// auto-generated\n" + _HIGH_CONSTRUCT);
		RecognizedConstruct generatedTest = Recognize("// auto-generated\ndescribe('c', () => { it('d', () => {}); });\n" + _HIGH_CONSTRUCT);

		Assert.Equal(LowConfidenceReasons.DemotedGeneratedFile, generated.LowReasons);
		Assert.Equal(ConfidenceLevel.Low, generatedTest.Level);
		Assert.True(generatedTest.TestCode);
	}

	[Fact]
	public void ATestFileThatUsesAMockServerLibraryIsATestByItsNameAndNotByTheLibrary()
	{
		const string source = "const nock = require('nock');\n" + _HIGH_CONSTRUCT;

		Assert.Equal(ConfidenceLevel.High, Recognize(source, "src/client.js").Level);
		Assert.Equal(ConfidenceLevel.Medium, Recognize(source, "src/client.test.js").Level);
	}

	// ------------------------------------------------------------------------------------------
	// A mock-server matcher in a test file is a near-miss, not a Test-demoted Medium
	// ------------------------------------------------------------------------------------------

	[Theory]
	[InlineData("nock(host).matchHeader('authorization', value => value.startsWith('Basic')).reply(200);\n")]
	[InlineData("server.use(rest.get(url, (req, res, ctx) => { const header = req.headers.get('authorization'); return res(ctx.status(header ? 200 : 401)); }));\n")]
	[InlineData("const header = request.get('Authorization');\nexpect(header).toMatch(/^Basic /);\n")]
	public void AMockServerMatcherInATestFileReportsNothingAtHighOrMediumAndIsNotReportedAsATestConstruct(string source)
	{
		ImmutableArray<RecognizedConstruct> constructs = new JavaScriptRecognitionHarness().Recognize(source, "tests/client.test.js");

		Assert.True(constructs.IsEmpty, $"[{source}] produced [{string.Join(", ", constructs.Select(construct => $"{construct.PatternClassIdentifier}:{construct.Level}"))}]");
	}

	// ------------------------------------------------------------------------------------------
	// Helpers
	// ------------------------------------------------------------------------------------------

	private static RecognizedConstruct Recognize(string source, string relativePath = JavaScriptRecognitionHarness.UNDEMOTED_RELATIVE_PATH)
	{
		ImmutableArray<RecognizedConstruct> constructs = new JavaScriptRecognitionHarness().Recognize(source, relativePath);

		return constructs.Single(construct => construct.PatternClassIdentifier == _HIGH_CLASS);
	}
}
