using System.Collections.Immutable;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Recognition;

using Xunit;

namespace MigrateAuth.Cli.Tests.Recognition;

/// <summary>
/// FILE DEMOTION SIGNALS, SPLIT BY SHAPE: THE ENGINE'S PATH-SHAPED ONE AND THE ANALYZER'S
/// LANGUAGE-SHAPED ONES, BOTH REACHING THE SAME COMPOSITION.
///
/// The engine's path-shaped set is EMPTY. The <c>Deployment/</c> folder is one of <c>CSharp</c>'s own
/// language-shaped folder signals, and it stays a demotion and never an exclusion.
///
/// The negative case is as load-bearing as the positives. Demotion is the primary lever for
/// meeting the false-positive threshold, and a lever that fires on every file is not a lever.
/// </summary>
public sealed class DemotionSignalTests
{
	private const string _CREDENTIAL_TYPE_PATTERN_CLASS = "EXM-001";

	private const string _UNDEMOTED_PATH = "source/module.ex1";
	private const string _DEPLOYMENT_FOLDER_PATH = "Deployment/module.ex1";
	private const string _DEPLOYMENT_FOLDER_PATH_IN_OTHER_CASING = "src/deployment/module.ex1";
	private const string _ANALYZER_DECLARED_FOLDER_PATH = "src/exemplar-generated/module.ex1";
	private const string _ANALYZER_DECLARED_FILE_NAME_PATH = "source/module.spec.ex1";
	private const string _FOLDER_MERELY_CONTAINING_THE_SIGNAL_PATH = "src/predeployment/module.ex1";
	private const string _CSHARP_DEPLOYMENT_FOLDER_PATH = "Deployment/Module.cs";
	private const string _CSHARP_DEPLOYMENT_FOLDER_PATH_IN_OTHER_CASING = "src/deployment/Module.cs";
	private const string _CSHARP_FOLDER_MERELY_CONTAINING_THE_SIGNAL_PATH = "src/predeployment/Module.cs";
	private const string _CSHARP_CREDENTIAL_CONSTRUCTION_IN_CODE = """
		class A
		{
			void B()
			{
				var credentials = new UsernamePasswordCredentials(options.UserName, options.Password);
			}
		}
		""";

	private const string _CREDENTIAL_CONSTRUCTION_IN_CODE = """
		let credentials = NewExemplarCredentials(operatorName, "hunter2")
		""";

	/// <summary>
	/// Without a signal this construct is High, so every demoted case below is a measurement of
	/// the signal and not of anything else the file happens to contain.
	/// </summary>
	[Fact]
	public void AFileCarryingNoSignalIsNotDemoted()
	{
		Assert.Equal(ConfidenceLevel.High, LevelUnder(_UNDEMOTED_PATH));
	}

	/// <summary>
	/// The shared engine does not demote the <c>Deployment</c> folder, so an analyzer that declares no
	/// such signal of its own (the reference analyzer here) reads the same construct at its own level.
	/// </summary>
	[Theory]
	[InlineData(_DEPLOYMENT_FOLDER_PATH)]
	[InlineData(_DEPLOYMENT_FOLDER_PATH_IN_OTHER_CASING)]
	public void TheEngineNoLongerDemotesTheDeploymentFolderForAnAnalyzerThatDeclaresNoSuchSignal(string relativePath)
	{
		Assert.Equal(ConfidenceLevel.High, LevelUnder(relativePath));
	}

	/// <summary>
	/// The signal lives in <c>CSharp</c>'s language-shaped folder signals, matched without
	/// regard to case, and reaches the composition through the same door as any analyzer-declared one.
	/// </summary>
	[Theory]
	[InlineData(_CSHARP_DEPLOYMENT_FOLDER_PATH)]
	[InlineData(_CSHARP_DEPLOYMENT_FOLDER_PATH_IN_OTHER_CASING)]
	public void CSharpDemotesTheDeploymentFolderCaseInsensitively(string relativePath)
	{
		RecognizedConstruct construct = Assert.Single(
			new CSharpRecognitionHarness().Recognize(_CSHARP_CREDENTIAL_CONSTRUCTION_IN_CODE, relativePath));

		Assert.Equal(ConfidenceLevel.Low, construct.Level);
		Assert.Equal(LowConfidenceReasons.DemotedDeploymentFolder, construct.LowReasons);
	}

	[Fact]
	public void CSharpDeploymentSignalMatchesWholeFolderNamesOnly()
	{
		Assert.Equal(ConfidenceLevel.High, CSharpLevelUnder(_CSHARP_FOLDER_MERELY_CONTAINING_THE_SIGNAL_PATH));
	}

	/// <summary>
	/// A folder whose NAME merely contains the signal is not the signal. The comparison is over
	/// whole path segments, so a project folder called something else that happens to embed the
	/// word does not silently drop every finding under it to Low.
	/// </summary>
	[Fact]
	public void AFolderNameMerelyContainingTheSignalIsNotTheSignal()
	{
		Assert.Equal(ConfidenceLevel.High, LevelUnder(_FOLDER_MERELY_CONTAINING_THE_SIGNAL_PATH));
	}

	/// <summary>
	/// The analyzer's own signals reach the same composition through the same door. Which of the
	/// two sets a signal came from changes who was entitled to declare it and changes nothing
	/// about what it does.
	/// </summary>
	[Fact]
	public void TheAnalyzersOwnGeneratedFolderSignalDemotesToLowWithTheGeneratedReason()
	{
		RecognizedConstruct construct = Assert.Single(
			new RecognitionHarness().Recognize(_CREDENTIAL_CONSTRUCTION_IN_CODE, _ANALYZER_DECLARED_FOLDER_PATH));

		Assert.Equal(ConfidenceLevel.Low, construct.Level);
		Assert.Equal(LowConfidenceReasons.DemotedGeneratedFile, construct.LowReasons);
		Assert.False(construct.TestCode);
	}

	[Fact]
	public void TheAnalyzersOwnTestFileNameSignalDemotesToMediumAndMarksTestCode()
	{
		RecognizedConstruct construct = Assert.Single(
			new RecognitionHarness().Recognize(_CREDENTIAL_CONSTRUCTION_IN_CODE, _ANALYZER_DECLARED_FILE_NAME_PATH));

		Assert.Equal(ConfidenceLevel.Medium, construct.Level);
		Assert.Equal(LowConfidenceReasons.None, construct.LowReasons);
		Assert.True(construct.TestCode);
	}

	/// <summary>
	/// A property of the type rather than of a comment: the engine's set is not suppliable
	/// through the analyzer contract, so no catalog can add to it or take from it.
	/// </summary>
	[Fact]
	public void TheBasesSetIsNotAMemberOfTheAnalyzerContract()
	{
		Assert.DoesNotContain(
			typeof(ILanguageAnalyzer).GetProperties(),
			property => property.PropertyType == typeof(PathShapedDemotionSignals));
	}

	/// <summary>
	/// A demotion narrows what is reported and the effective configuration gives the repository-resolved file
	/// no field that carries one, so the engine's set carries no way to be supplied one: no public
	/// constructor, and no setter on anything it holds.
	/// </summary>
	[Fact]
	public void TheBasesSetCannotBeConstructedOrReplaced()
	{
		Assert.Empty(typeof(PathShapedDemotionSignals).GetConstructors());
		Assert.NotNull(PathShapedDemotionSignals.TheBaseSet);
		Assert.Empty(PathShapedDemotionSignals.TheBaseSet.FolderNameSignals);
	}

	/// <summary>
	/// The generated-marker category is declared and EMPTY, which is a different statement from
	/// missing: the classification rules name the category and name no text that would be one.
	/// </summary>
	[Fact]
	public void TheEnginesGeneratedMarkerCategoryIsDeclaredAndDeliberatelyEmpty()
	{
		Assert.True(PathShapedDemotionSignals.TheBaseSet.GenerationMarkerExpressions.IsEmpty);
		Assert.Empty(PathShapedDemotionSignals.TheBaseSet.FileNameSignals);
	}

	private static ConfidenceLevel CSharpLevelUnder(string relativePath)
	{
		CSharpRecognitionHarness harness = new();

		ImmutableArray<RecognizedConstruct> recognized =
			harness.Recognize(_CSHARP_CREDENTIAL_CONSTRUCTION_IN_CODE, relativePath);

		return Assert.Single(recognized).Level;
	}

	private static ConfidenceLevel LevelUnder(string relativePath)
	{
		RecognitionHarness harness = new();

		ImmutableArray<RecognizedConstruct> recognized =
			harness.Recognize(_CREDENTIAL_CONSTRUCTION_IN_CODE, relativePath);

		RecognizedConstruct construct = Assert.Single(recognized);
		Assert.Equal(_CREDENTIAL_TYPE_PATTERN_CLASS, construct.PatternClassIdentifier);

		return construct.Level;
	}
}
