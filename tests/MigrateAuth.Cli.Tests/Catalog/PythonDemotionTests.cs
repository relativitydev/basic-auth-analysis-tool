using System.Collections.Immutable;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Recognition;
using MigrateAuth.Cli.Tests.Recognition;

using Xunit;

namespace MigrateAuth.Cli.Tests.Catalog;

/// <summary>
/// THE DEMOTION SIGNALS OF THE SHIPPED `Python` ANALYZER, one fixture per kind, run through the real recognition engine.
///
/// The same High construct sits in every file, so the level read measures the kinds that fired and nothing else: a test kind alone proposes Medium and carries the test-code mark, any other kind proposes Low, the minimum is
/// taken once, and a folder named for a deployment demotes nothing. Every script is invented, with placeholder names.
/// </summary>
public sealed class PythonDemotionTests
{
	private const string _HIGH_CONSTRUCT = "response = requests.get(url, auth=HTTPBasicAuth(\"user\", \"secret\"))\n";
	private const string _HIGH_CLASS = "PY-001";
	private const int _FILE_NAME_SIGNAL_COUNT = 14;
	private const int _FOLDER_NAME_SIGNAL_COUNT = 25;
	private const int _CONTENT_MARKER_COUNT = 2;

	// ------------------------------------------------------------------------------------------
	// What is declared
	// ------------------------------------------------------------------------------------------

	[Fact]
	public void EverySignalIsInsensitiveAndTheCountsAreThoseOfTheCatalog()
	{
		LanguageShapedDemotionSignals signals = PythonShippedAnalyzer.Instance.DemotionSignals;

		Assert.Equal(_FILE_NAME_SIGNAL_COUNT, signals.FileNameSignals.Count);
		Assert.Equal(_FOLDER_NAME_SIGNAL_COUNT, signals.FolderNameSignals.Count);
		Assert.Equal(_CONTENT_MARKER_COUNT, signals.ContentMarkers.Length);
		Assert.Single(signals.GenerationMarkerExpressions);
		Assert.All(signals.FileNameSignals, signal => Assert.Equal(CaseSensitivity.Insensitive, signal.Sensitivity));
		Assert.All(signals.FolderNameSignals, signal => Assert.Equal(CaseSensitivity.Insensitive, signal.Sensitivity));
	}

	[Fact]
	public void NoDeploymentSignalIsDeclaredAndTheKindsAreTheOnesTheCatalogNames()
	{
		LanguageShapedDemotionSignals signals = PythonShippedAnalyzer.Instance.DemotionSignals;
		IEnumerable<DemotionKind> kinds = signals.FileNameSignals.Select(signal => signal.Kind)
			.Concat(signals.FolderNameSignals.Select(signal => signal.Kind))
			.Concat(signals.ContentMarkers.Select(marker => marker.Kind));

		Assert.DoesNotContain(DemotionKind.Deployment, kinds);
		Assert.DoesNotContain(signals.FolderNameSignals, signal => signal.FolderName.Equals("Deployment", StringComparison.OrdinalIgnoreCase));
		Assert.Equal(
			new[] { DemotionKind.Generated, DemotionKind.Mock, DemotionKind.Sample, DemotionKind.Test, DemotionKind.Vendored }.Order(),
			kinds.Distinct().Order());
	}

	[Fact]
	public void TheFileNameSignalsAreTheThreeTestFormsTheTwoTestNamesAndTheNineMockForms()
	{
		ImmutableArray<(FileNameMatch Match, string Text, DemotionKind Kind)> declared =
		[
			.. PythonShippedAnalyzer.Instance.DemotionSignals.FileNameSignals.Select(signal => (signal.Match, signal.Text, signal.Kind)).Order()
		];
		(FileNameMatch Match, string Text, DemotionKind Kind)[] expected =
		[
			(FileNameMatch.Prefix, "test_", DemotionKind.Test),
			(FileNameMatch.Suffix, "_test.py", DemotionKind.Test),
			(FileNameMatch.Suffix, "_test.pyw", DemotionKind.Test),
			(FileNameMatch.ExactName, "conftest.py", DemotionKind.Test),
			(FileNameMatch.ExactName, "tests.py", DemotionKind.Test),
			(FileNameMatch.Prefix, "mock_", DemotionKind.Mock),
			(FileNameMatch.Prefix, "stub_", DemotionKind.Mock),
			(FileNameMatch.Prefix, "fake_", DemotionKind.Mock),
			(FileNameMatch.Suffix, "_mock.py", DemotionKind.Mock),
			(FileNameMatch.Suffix, "_stub.py", DemotionKind.Mock),
			(FileNameMatch.Suffix, "_fake.py", DemotionKind.Mock),
			(FileNameMatch.ExactName, "mock.py", DemotionKind.Mock),
			(FileNameMatch.ExactName, "mocks.py", DemotionKind.Mock),
			(FileNameMatch.ExactName, "fakes.py", DemotionKind.Mock)
		];

		Assert.Equal(expected.Order().ToArray(), declared.ToArray());
	}

	[Fact]
	public void TheContentMarkersDeclareTheZonesTheCatalogGivesThem()
	{
		ImmutableArray<ContentMarker> markers = PythonShippedAnalyzer.Instance.DemotionSignals.ContentMarkers;

		ContentMarker generated = Assert.Single(markers, marker => marker.Kind == DemotionKind.Generated);
		ContentMarker test = Assert.Single(markers, marker => marker.Kind == DemotionKind.Test);

		Assert.Equal(ContentMarkerZone.AnyZone, generated.Zone);
		Assert.Equal(ContentMarkerZone.CodeOnly, test.Zone);
		Assert.Equal(RecognitionEnginePolicy.MATCH_BOUND, generated.Expression.MatchTimeout);
		Assert.Equal(RecognitionEnginePolicy.MATCH_BOUND, test.Expression.MatchTimeout);
		Assert.Equal(RecognitionEnginePolicy.MATCH_BOUND, Assert.Single(PythonShippedAnalyzer.Instance.DemotionSignals.GenerationMarkerExpressions).MatchTimeout);
	}

	// ------------------------------------------------------------------------------------------
	// A fixture per kind, by file and folder name
	// ------------------------------------------------------------------------------------------

	[Theory]
	[InlineData("src/client.py", ConfidenceLevel.High, false)]
	[InlineData("src/client.pyw", ConfidenceLevel.High, false)]
	[InlineData("Deployment/client.py", ConfidenceLevel.High, false)]
	[InlineData("deployment/scripts/client.py", ConfidenceLevel.High, false)]
	[InlineData("tests/client.py", ConfidenceLevel.Medium, true)]
	[InlineData("test/client.py", ConfidenceLevel.Medium, true)]
	[InlineData("testing/client.py", ConfidenceLevel.Medium, true)]
	[InlineData("fixture/client.py", ConfidenceLevel.Medium, true)]
	[InlineData("fixtures/client.py", ConfidenceLevel.Medium, true)]
	[InlineData("e2e/client.py", ConfidenceLevel.Medium, true)]
	[InlineData("Tests/Client.py", ConfidenceLevel.Medium, true)]
	[InlineData("src/test_orders.py", ConfidenceLevel.Medium, true)]
	[InlineData("src/TEST_ORDERS.PY", ConfidenceLevel.Medium, true)]
	[InlineData("src/orders_test.py", ConfidenceLevel.Medium, true)]
	[InlineData("src/orders_test.pyw", ConfidenceLevel.Medium, true)]
	[InlineData("src/conftest.py", ConfidenceLevel.Medium, true)]
	[InlineData("src/tests.py", ConfidenceLevel.Medium, true)]
	[InlineData("src/latest.py", ConfidenceLevel.High, false)]
	[InlineData("src/contest.py", ConfidenceLevel.High, false)]
	[InlineData("src/testing.py", ConfidenceLevel.High, false)]
	[InlineData("src/conftests.py", ConfidenceLevel.High, false)]
	[InlineData("mock/client.py", ConfidenceLevel.Low, false)]
	[InlineData("mocks/client.py", ConfidenceLevel.Low, false)]
	[InlineData("stub/client.py", ConfidenceLevel.Low, false)]
	[InlineData("stubs/client.py", ConfidenceLevel.Low, false)]
	[InlineData("fake/client.py", ConfidenceLevel.Low, false)]
	[InlineData("fakes/client.py", ConfidenceLevel.Low, false)]
	[InlineData("src/mock_client.py", ConfidenceLevel.Low, false)]
	[InlineData("src/stub_client.py", ConfidenceLevel.Low, false)]
	[InlineData("src/fake_client.py", ConfidenceLevel.Low, false)]
	[InlineData("src/client_mock.py", ConfidenceLevel.Low, false)]
	[InlineData("src/client_stub.py", ConfidenceLevel.Low, false)]
	[InlineData("src/client_fake.py", ConfidenceLevel.Low, false)]
	[InlineData("src/mock.py", ConfidenceLevel.Low, false)]
	[InlineData("src/mocks.py", ConfidenceLevel.Low, false)]
	[InlineData("src/fakes.py", ConfidenceLevel.Low, false)]
	[InlineData("src/test_fakes.py", ConfidenceLevel.Medium, true)]
	[InlineData("src/fake.py", ConfidenceLevel.High, false)]
	[InlineData("sample/client.py", ConfidenceLevel.Low, false)]
	[InlineData("samples/client.py", ConfidenceLevel.Low, false)]
	[InlineData("example/client.py", ConfidenceLevel.Low, false)]
	[InlineData("examples/client.py", ConfidenceLevel.Low, false)]
	[InlineData("demo/client.py", ConfidenceLevel.Low, false)]
	[InlineData("demos/client.py", ConfidenceLevel.Low, false)]
	[InlineData("src/example_client.py", ConfidenceLevel.High, false)]
	[InlineData("src/sample_client.py", ConfidenceLevel.High, false)]
	[InlineData("vendor/client.py", ConfidenceLevel.Low, false)]
	[InlineData("site-packages/client.py", ConfidenceLevel.Low, false)]
	[InlineData("venv/client.py", ConfidenceLevel.Low, false)]
	[InlineData(".venv/client.py", ConfidenceLevel.Low, false)]
	[InlineData("dist-packages/client.py", ConfidenceLevel.Low, false)]
	[InlineData("__pypackages__/client.py", ConfidenceLevel.Low, false)]
	[InlineData(".tox/client.py", ConfidenceLevel.Low, false)]
	[InlineData("VENV/Lib/client.py", ConfidenceLevel.Low, false)]
	[InlineData("venvs/client.py", ConfidenceLevel.High, false)]
	[InlineData("env/client.py", ConfidenceLevel.High, false)]
	[InlineData("virtualenv/client.py", ConfidenceLevel.High, false)]
	[InlineData(".nox/client.py", ConfidenceLevel.High, false)]
	[InlineData("src/login_pb2.py", ConfidenceLevel.Low, false)]
	[InlineData("src/LOGIN_PB2.PY", ConfidenceLevel.Low, false)]
	[InlineData("src/login_pb2_grpc.py", ConfidenceLevel.Low, false)]
	[InlineData("src/LOGIN_PB2_GRPC.PY", ConfidenceLevel.Low, false)]
	[InlineData("src/login_pb2_grpc_extra.py", ConfidenceLevel.High, false)]
	[InlineData("src/login_grpc.py", ConfidenceLevel.High, false)]
	[InlineData("tests/mocks/client.py", ConfidenceLevel.Low, true)]
	[InlineData("src/mock_server_test.py", ConfidenceLevel.Low, true)]
	[InlineData("site-packages/pkg/tests/test_orders.py", ConfidenceLevel.Low, true)]
	[InlineData("examples/test_orders.py", ConfidenceLevel.Low, true)]
	public void AHighConstructIsDemotedByTheNameOfItsFileAndFolderAndByNothingElse(string relativePath, ConfidenceLevel expected, bool expectedTestCode)
	{
		RecognizedConstruct construct = Recognize(_HIGH_CONSTRUCT, relativePath);

		Assert.Equal(expected, construct.Level);
		Assert.Equal(expectedTestCode, construct.TestCode);
	}

	[Fact]
	public void ATestKindAloneReadsMediumWithNoLowReasonAndEveryOtherKindReadsLowWithItsOwnReason()
	{
		Assert.Equal(LowConfidenceReasons.None, Recognize(_HIGH_CONSTRUCT, "tests/client.py").LowReasons);
		Assert.Equal(LowConfidenceReasons.DemotedMockFile, Recognize(_HIGH_CONSTRUCT, "mocks/client.py").LowReasons);
		Assert.Equal(LowConfidenceReasons.DemotedSampleFile, Recognize(_HIGH_CONSTRUCT, "samples/client.py").LowReasons);
		Assert.Equal(LowConfidenceReasons.DemotedGeneratedFile, Recognize(_HIGH_CONSTRUCT, "src/login_pb2.py").LowReasons);
	}

	[Theory]
	[InlineData("vendor/client.py")]
	[InlineData("site-packages/client.py")]
	[InlineData("venv/client.py")]
	[InlineData(".venv/client.py")]
	[InlineData("dist-packages/client.py")]
	[InlineData("__pypackages__/client.py")]
	[InlineData(".tox/client.py")]
	public void EachVendoredFolderReadsLowWithTheVendoredReason(string relativePath)
	{
		RecognizedConstruct construct = Recognize(_HIGH_CONSTRUCT, relativePath);

		Assert.Equal(ConfidenceLevel.Low, construct.Level);
		Assert.Equal(LowConfidenceReasons.DemotedVendoredFile, construct.LowReasons);
	}

	[Fact]
	public void ATestKindWithAnotherKindReadsLowAndKeepsTheTestCodeMark()
	{
		RecognizedConstruct construct = Recognize(_HIGH_CONSTRUCT, "tests/mocks/client.py");

		Assert.Equal(ConfidenceLevel.Low, construct.Level);
		Assert.True(construct.TestCode);
		Assert.Equal(LowConfidenceReasons.DemotedMockFile, construct.LowReasons);
	}

	[Fact]
	public void AFolderNamedDeploymentDemotesNothingAndAHighConstructInItReadsHigh()
	{
		RecognizedConstruct construct = Recognize(_HIGH_CONSTRUCT, "Deployment/scripts/client.py");

		Assert.Equal(ConfidenceLevel.High, construct.Level);
		Assert.False(construct.TestCode);
		Assert.Equal(LowConfidenceReasons.None, construct.LowReasons);
	}

	// ------------------------------------------------------------------------------------------
	// A fixture per content marker
	// ------------------------------------------------------------------------------------------

	[Theory]
	[InlineData("# This file was auto-generated\n" + _HIGH_CONSTRUCT, ConfidenceLevel.Low, false)]
	[InlineData("# AUTOGENERATED\n" + _HIGH_CONSTRUCT, ConfidenceLevel.Low, false)]
	[InlineData("# @generated\n" + _HIGH_CONSTRUCT, ConfidenceLevel.Low, false)]
	[InlineData("#!/usr/bin/env python3\n# -*- coding: utf-8 -*-\n# Generated by build.py\n" + _HIGH_CONSTRUCT, ConfidenceLevel.Low, false)]
	[InlineData("value = 1\nvalue = 2\n# DO NOT EDIT - changes will be overwritten\n" + _HIGH_CONSTRUCT, ConfidenceLevel.Low, false)]
	[InlineData("value = 1\nvalue = 2\nvalue = 3\n# auto-generated file\n" + _HIGH_CONSTRUCT, ConfidenceLevel.High, false)]
	[InlineData("import os\nx = 1  # generated by hand\n" + _HIGH_CONSTRUCT, ConfidenceLevel.High, false)]
	[InlineData("\"\"\"This module is auto-generated.\"\"\"\n" + _HIGH_CONSTRUCT, ConfidenceLevel.High, false)]
	[InlineData(_HIGH_CONSTRUCT + "value = 1\nvalue = 2\n# Generated by build.py\n", ConfidenceLevel.High, false)]
	[InlineData(_HIGH_CONSTRUCT + "# Generated by build.py\n", ConfidenceLevel.Low, false)]
	[InlineData("class ClientTests(unittest.TestCase):\n\tdef test_get(self):\n\t\t" + _HIGH_CONSTRUCT, ConfidenceLevel.Medium, true)]
	[InlineData("class Case(TestCase):\n\tpass\n" + _HIGH_CONSTRUCT, ConfidenceLevel.Medium, true)]
	[InlineData("class AsyncCase(IsolatedAsyncioTestCase):\n\tpass\n" + _HIGH_CONSTRUCT, ConfidenceLevel.Medium, true)]
	[InlineData("# class Case(TestCase):\n" + _HIGH_CONSTRUCT, ConfidenceLevel.High, false)]
	[InlineData("note = \"class Case(TestCase):\"\n" + _HIGH_CONSTRUCT, ConfidenceLevel.High, false)]
	[InlineData("class Client(Base):\n\tpass\n" + _HIGH_CONSTRUCT, ConfidenceLevel.High, false)]
	[InlineData("class Case(MyTestCase):\n\tpass\n" + _HIGH_CONSTRUCT, ConfidenceLevel.High, false)]
	public void AHighConstructIsDemotedByTheContentMarkersWhereTheyStandInTheirZone(string source, ConfidenceLevel expected, bool expectedTestCode)
	{
		RecognizedConstruct construct = Recognize(source);

		Assert.Equal(expected, construct.Level);
		Assert.Equal(expectedTestCode, construct.TestCode);
	}

	[Fact]
	public void AGeneratedBannerReadsLowWithTheGeneratedReasonAndAGeneratedTestClassReadsLowAndKeepsTheMark()
	{
		RecognizedConstruct generated = Recognize("# auto-generated\n" + _HIGH_CONSTRUCT);
		RecognizedConstruct generatedTest = Recognize("# auto-generated\nclass Case(TestCase):\n\tpass\n" + _HIGH_CONSTRUCT);

		Assert.Equal(LowConfidenceReasons.DemotedGeneratedFile, generated.LowReasons);
		Assert.Equal(ConfidenceLevel.Low, generatedTest.Level);
		Assert.True(generatedTest.TestCode);
	}

	[Fact]
	public void ATestClassMarkerDoesNotDemoteATestWithANeutralNameThatDeclaresNoClassAsTheDeclaredCostSays()
	{
		RecognizedConstruct construct = Recognize("def check():\n\t" + _HIGH_CONSTRUCT, "src/helper.py");

		Assert.Equal(ConfidenceLevel.High, construct.Level);
	}

	// ------------------------------------------------------------------------------------------
	// Helpers
	// ------------------------------------------------------------------------------------------

	private static RecognizedConstruct Recognize(string source, string relativePath = PythonRecognitionHarness.UNDEMOTED_RELATIVE_PATH)
	{
		ImmutableArray<RecognizedConstruct> constructs = new PythonRecognitionHarness().Recognize(source, relativePath);

		return constructs.Single(construct => construct.PatternClassIdentifier == _HIGH_CLASS);
	}
}
