using System.Collections.Immutable;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Recognition;
using MigrateAuth.Cli.Tests.Recognition;

using Xunit;

namespace MigrateAuth.Cli.Tests.Catalog;

/// <summary>
/// THE DEMOTION SIGNALS OF THE SHIPPED `Shell` ANALYZER, one fixture per kind, run through the real recognition engine.
///
/// The same High construct sits in every file, so the level read measures the kinds that fired and nothing else: a test kind alone proposes Medium and
/// carries the test-code mark, any other kind proposes Low, the minimum is taken once, and a folder named for a deployment demotes nothing. Every script is
/// invented, with placeholder names.
/// </summary>
public sealed class ShellDemotionTests
{
	private const string _HIGH_CONSTRUCT = "curl -sS -u admin:example \"$HOST/api\"\n";
	private const string _HIGH_CLASS = "SH-001";
	private const int _FILE_NAME_SIGNAL_COUNT = 35;
	private const int _FOLDER_NAME_SIGNAL_COUNT = 18;
	private const int _CONTENT_MARKER_COUNT = 3;
	private const int _SUFFIX_SIGNAL_COUNT = 28;
	private const int _PREFIX_SIGNAL_COUNT = 7;

	// ------------------------------------------------------------------------------------------
	// What is declared
	// ------------------------------------------------------------------------------------------

	[Fact]
	public void EverySignalIsInsensitiveAndTheCountsAreThoseOfTheCatalog()
	{
		LanguageShapedDemotionSignals signals = ShellRegisteredExpressions.Analyzer.DemotionSignals;

		Assert.Equal(_FILE_NAME_SIGNAL_COUNT, signals.FileNameSignals.Count);
		Assert.Equal(_FOLDER_NAME_SIGNAL_COUNT, signals.FolderNameSignals.Count);
		Assert.Equal(_CONTENT_MARKER_COUNT, signals.ContentMarkers.Length);
		Assert.Equal(_SUFFIX_SIGNAL_COUNT, signals.FileNameSignals.Count(signal => signal.Match == FileNameMatch.Suffix));
		Assert.Equal(_PREFIX_SIGNAL_COUNT, signals.FileNameSignals.Count(signal => signal.Match == FileNameMatch.Prefix));
		Assert.Empty(signals.GenerationMarkerExpressions);
		Assert.All(signals.FileNameSignals, signal => Assert.Equal(CaseSensitivity.Insensitive, signal.Sensitivity));
		Assert.All(signals.FolderNameSignals, signal => Assert.Equal(CaseSensitivity.Insensitive, signal.Sensitivity));
	}

	[Fact]
	public void NoDeploymentSignalIsDeclaredAndTheKindsAreTheOnesTheCatalogNames()
	{
		LanguageShapedDemotionSignals signals = ShellRegisteredExpressions.Analyzer.DemotionSignals;
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
	public void TheContentMarkersDeclareTheZonesTheCatalogGivesThem()
	{
		ImmutableArray<ContentMarker> markers = ShellRegisteredExpressions.Analyzer.DemotionSignals.ContentMarkers;

		ContentMarker generated = Assert.Single(markers, marker => marker.Kind == DemotionKind.Generated);
		Assert.Equal(ContentMarkerZone.AnyZone, generated.Zone);
		Assert.Equal(2, markers.Count(marker => marker.Kind == DemotionKind.Test && marker.Zone == ContentMarkerZone.CodeOnly));
	}

	// ------------------------------------------------------------------------------------------
	// A fixture per kind, by file and folder name
	// ------------------------------------------------------------------------------------------

	[Theory]
	[InlineData("scripts/run.sh", ConfidenceLevel.High, false)]
	[InlineData("Deployment/run.sh", ConfidenceLevel.High, false)]
	[InlineData("deployment/scripts/run.sh", ConfidenceLevel.High, false)]
	[InlineData("scripts/helper.sh", ConfidenceLevel.High, false)]
	[InlineData("tests/run.sh", ConfidenceLevel.Medium, true)]
	[InlineData("test/run.sh", ConfidenceLevel.Medium, true)]
	[InlineData("spec/run.sh", ConfidenceLevel.Medium, true)]
	[InlineData("fixture/run.sh", ConfidenceLevel.Medium, true)]
	[InlineData("fixtures/run.sh", ConfidenceLevel.Medium, true)]
	[InlineData("Tests/Run.sh", ConfidenceLevel.Medium, true)]
	[InlineData("scripts/run_test.sh", ConfidenceLevel.Medium, true)]
	[InlineData("scripts/run-test.sh", ConfidenceLevel.Medium, true)]
	[InlineData("scripts/run.test.sh", ConfidenceLevel.Medium, true)]
	[InlineData("scripts/run_spec.sh", ConfidenceLevel.Medium, true)]
	[InlineData("scripts/run_test.bash", ConfidenceLevel.Medium, true)]
	[InlineData("scripts/run_test.zsh", ConfidenceLevel.Medium, true)]
	[InlineData("scripts/run_test.ksh", ConfidenceLevel.Medium, true)]
	[InlineData("scripts/RUN_TEST.SH", ConfidenceLevel.Medium, true)]
	[InlineData("scripts/test_run.sh", ConfidenceLevel.Medium, true)]
	[InlineData("scripts/test-run.sh", ConfidenceLevel.Medium, true)]
	[InlineData("scripts/test_connection.sh", ConfidenceLevel.Medium, true)]
	[InlineData("scripts/latest.sh", ConfidenceLevel.High, false)]
	[InlineData("scripts/contest.sh", ConfidenceLevel.High, false)]
	[InlineData("scripts/testing.sh", ConfidenceLevel.High, false)]
	[InlineData("mocks/run.sh", ConfidenceLevel.Low, false)]
	[InlineData("mock/run.sh", ConfidenceLevel.Low, false)]
	[InlineData("stub/run.sh", ConfidenceLevel.Low, false)]
	[InlineData("stubs/run.sh", ConfidenceLevel.Low, false)]
	[InlineData("fake/run.sh", ConfidenceLevel.Low, false)]
	[InlineData("fakes/run.sh", ConfidenceLevel.Low, false)]
	[InlineData("scripts/mock_run.sh", ConfidenceLevel.Low, false)]
	[InlineData("scripts/fake_run.sh", ConfidenceLevel.Low, false)]
	[InlineData("scripts/stub_run.sh", ConfidenceLevel.Low, false)]
	[InlineData("scripts/run_mock.sh", ConfidenceLevel.Low, false)]
	[InlineData("scripts/run_fake.bash", ConfidenceLevel.Low, false)]
	[InlineData("scripts/run_stub.zsh", ConfidenceLevel.Low, false)]
	[InlineData("scripts/run_mock.ksh", ConfidenceLevel.Low, false)]
	[InlineData("samples/run.sh", ConfidenceLevel.Low, false)]
	[InlineData("sample/run.sh", ConfidenceLevel.Low, false)]
	[InlineData("example/run.sh", ConfidenceLevel.Low, false)]
	[InlineData("examples/run.sh", ConfidenceLevel.Low, false)]
	[InlineData("scripts/sample_run.sh", ConfidenceLevel.Low, false)]
	[InlineData("scripts/example.sh", ConfidenceLevel.Low, false)]
	[InlineData("scripts/sampler.sh", ConfidenceLevel.Low, false)]
	[InlineData("vendor/run.sh", ConfidenceLevel.Low, false)]
	[InlineData("third-party/run.sh", ConfidenceLevel.Low, false)]
	[InlineData("third_party/run.sh", ConfidenceLevel.Low, false)]
	[InlineData("tests/mocks/run.sh", ConfidenceLevel.Low, true)]
	[InlineData("scripts/mock_server_test.sh", ConfidenceLevel.Low, true)]
	[InlineData("spec/vendor/run.sh", ConfidenceLevel.Low, true)]
	public void AHighConstructIsDemotedByTheNameOfItsFileAndFolderAndByNothingElse(string relativePath, ConfidenceLevel expected, bool expectedTestCode)
	{
		RecognizedConstruct construct = Recognize(_HIGH_CONSTRUCT, relativePath);

		Assert.Equal(expected, construct.Level);
		Assert.Equal(expectedTestCode, construct.TestCode);
	}

	[Fact]
	public void ATestKindAloneReadsMediumWithNoLowReasonAndEveryOtherKindReadsLowWithItsOwnReason()
	{
		Assert.Equal(LowConfidenceReasons.None, Recognize(_HIGH_CONSTRUCT, "tests/run.sh").LowReasons);
		Assert.Equal(LowConfidenceReasons.DemotedMockFile, Recognize(_HIGH_CONSTRUCT, "mocks/run.sh").LowReasons);
		Assert.Equal(LowConfidenceReasons.DemotedSampleFile, Recognize(_HIGH_CONSTRUCT, "samples/run.sh").LowReasons);
		Assert.Equal(LowConfidenceReasons.DemotedVendoredFile, Recognize(_HIGH_CONSTRUCT, "vendor/run.sh").LowReasons);
	}

	[Fact]
	public void ATestKindWithAnotherKindReadsLowAndKeepsTheTestCodeMark()
	{
		RecognizedConstruct construct = Recognize(_HIGH_CONSTRUCT, "tests/mocks/run.sh");

		Assert.Equal(ConfidenceLevel.Low, construct.Level);
		Assert.True(construct.TestCode);
		Assert.Equal(LowConfidenceReasons.DemotedMockFile, construct.LowReasons);
	}

	[Fact]
	public void AFolderNamedDeploymentDemotesNothingAndAHighConstructInItReadsHigh()
	{
		RecognizedConstruct construct = Recognize(_HIGH_CONSTRUCT, "Deployment/scripts/run.sh");

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
	[InlineData("#!/bin/sh\n# Generated by build.sh\n" + _HIGH_CONSTRUCT, ConfidenceLevel.Low, false)]
	[InlineData("#!/bin/sh\n#\n#\n#\n# DO NOT EDIT - changes will be overwritten\n" + _HIGH_CONSTRUCT, ConfidenceLevel.Low, false)]
	[InlineData("#!/bin/sh\necho start\n# Report generated by this script\n" + _HIGH_CONSTRUCT, ConfidenceLevel.High, false)]
	[InlineData("echo \"generated by\"\n" + _HIGH_CONSTRUCT, ConfidenceLevel.High, false)]
	[InlineData("#!/bin/sh\n#\n# Generated by a person\n" + _HIGH_CONSTRUCT, ConfidenceLevel.High, false)]
	[InlineData(_HIGH_CONSTRUCT + "# Generated by build.sh\n", ConfidenceLevel.High, false)]
	[InlineData(". ./lib/shunit2\n" + _HIGH_CONSTRUCT, ConfidenceLevel.Medium, true)]
	[InlineData("source ../vendor/bashunit/assert.sh\n" + _HIGH_CONSTRUCT, ConfidenceLevel.Medium, true)]
	[InlineData("source ./bats-assert/load.bash\n" + _HIGH_CONSTRUCT, ConfidenceLevel.Medium, true)]
	[InlineData("# . ./lib/shunit2\n" + _HIGH_CONSTRUCT, ConfidenceLevel.High, false)]
	[InlineData("echo '\n. ./lib/shunit2\n'\n" + _HIGH_CONSTRUCT, ConfidenceLevel.High, false)]
	[InlineData(". ./lib/common.sh\n" + _HIGH_CONSTRUCT, ConfidenceLevel.High, false)]
	[InlineData("Describe 'client'\n\tIt 'sends a request'\n\t\t" + _HIGH_CONSTRUCT + "\tEnd\nEnd\n", ConfidenceLevel.Medium, true)]
	[InlineData("Describe 'client'\n\techo ready\n" + _HIGH_CONSTRUCT, ConfidenceLevel.High, false)]
	[InlineData("# Describe 'client'\n# It 'sends a request'\n" + _HIGH_CONSTRUCT, ConfidenceLevel.High, false)]
	public void AHighConstructIsDemotedByTheContentMarkersWhereTheyStandInCode(string source, ConfidenceLevel expected, bool expectedTestCode)
	{
		RecognizedConstruct construct = Recognize(source);

		Assert.Equal(expected, construct.Level);
		Assert.Equal(expectedTestCode, construct.TestCode);
	}

	[Fact]
	public void AGeneratedHeaderReadsLowWithTheGeneratedReasonAndAGeneratedTestLibraryScriptReadsLowAndKeepsTheMark()
	{
		RecognizedConstruct generated = Recognize("# auto-generated\n" + _HIGH_CONSTRUCT);
		RecognizedConstruct generatedTest = Recognize("# auto-generated\n. ./lib/shunit2\n" + _HIGH_CONSTRUCT);

		Assert.Equal(LowConfidenceReasons.DemotedGeneratedFile, generated.LowReasons);
		Assert.Equal(ConfidenceLevel.Low, generatedTest.Level);
		Assert.True(generatedTest.TestCode);
	}

	// ------------------------------------------------------------------------------------------
	// Helpers
	// ------------------------------------------------------------------------------------------

	private static RecognizedConstruct Recognize(string source, string relativePath = ShellRecognitionHarness.UNDEMOTED_RELATIVE_PATH)
	{
		ImmutableArray<RecognizedConstruct> constructs = new ShellRecognitionHarness().Recognize(source, relativePath);

		return constructs.Single(construct => construct.PatternClassIdentifier == _HIGH_CLASS);
	}
}
