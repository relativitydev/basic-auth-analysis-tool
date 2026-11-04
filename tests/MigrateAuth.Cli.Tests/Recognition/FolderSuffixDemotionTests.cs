using System.Collections.Frozen;
using System.Collections.Immutable;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Emission;
using MigrateAuth.Cli.Recognition;

using Xunit;

namespace MigrateAuth.Cli.Tests.Recognition;

/// <summary>
/// <see cref="FolderNameMatch.Suffix"/>, the .NET project-folder
/// convention a whole path segment ending in, for example, <c>.Tests</c> - which
/// <see cref="FolderNameMatch.ExactName"/> can never reach, since the segment is not the text
/// <c>Tests</c>, it merely ends with it.
///
/// Every case here goes through the real <see cref="RecognitionEngine"/> over the real Exemplar
/// grammar via <see cref="DemotionSignalProbe"/>, with only the analyzer's declared
/// <see cref="LanguageShapedDemotionSignals"/> replaced per test - the reference fixture's
/// own folder signal is <see cref="FolderNameMatch.ExactName"/>, so it cannot exercise this.
/// </summary>
public sealed class FolderSuffixDemotionTests
{
	private const string _CREDENTIAL_TYPE_PATTERN_CLASS = "EXM-001";

	private const string _SUFFIX_FOLDER_SIGNAL_TEXT = ".Tests";
	private const string _EXACT_FOLDER_SIGNAL_TEXT = "Tests";

	private const string _SEGMENT_ENDING_IN_THE_SUFFIX_PATH = "src/Product.Core.Tests/module.ex1";
	private const string _SEGMENT_MERELY_CONTAINING_THE_SUFFIX_PATH = "src/Product.Tests.Extra/module.ex1";
	private const string _FILE_NAME_ENDING_IN_THE_SUFFIX_TEXT_PATH = "source/module.Tests";
	private const string _SEGMENT_MERELY_ENDING_IN_THE_EXACT_SIGNALS_TEXT_PATH = "src/MyTests/module.ex1";

	private const string _CREDENTIAL_CONSTRUCTION_IN_CODE = """
		let credentials = NewExemplarCredentials(operatorName, "hunter2")
		""";

	[Fact]
	public void ASuffixFolderSignalFiresOnASegmentEndingWithTheSuffix()
	{
		Assert.Equal(ConfidenceLevel.Medium, LevelUnderSuffixSignal(_SEGMENT_ENDING_IN_THE_SUFFIX_PATH));
	}

	/// <summary>
	/// The comparison is over the WHOLE segment, so a segment that merely contains the suffix text
	/// somewhere in its middle - and does not end with it - is not the signal.
	/// </summary>
	[Fact]
	public void ASuffixFolderSignalDoesNotFireOnASegmentMerelyContainingTheSuffixMidName()
	{
		Assert.Equal(ConfidenceLevel.High, LevelUnderSuffixSignal(_SEGMENT_MERELY_CONTAINING_THE_SUFFIX_PATH));
	}

	/// <summary>
	/// A folder signal is evaluated over folder segments only, never over the file name -
	/// <see cref="FileDemotionSignals"/>'s own decomposition keeps the two separate - so a file
	/// whose own NAME happens to end with the suffix text must not demote through this signal.
	/// </summary>
	[Fact]
	public void ASuffixFolderSignalDoesNotFireOnTheFileName()
	{
		Assert.Equal(ConfidenceLevel.High, LevelUnderSuffixSignal(_FILE_NAME_ENDING_IN_THE_SUFFIX_TEXT_PATH));
	}

	/// <summary>
	/// The suffix match changes nothing about <see cref="FolderNameMatch.ExactName"/>, the default: a
	/// segment that only ENDS WITH the exact signal's text is not equal to it, so it does not fire.
	/// </summary>
	[Fact]
	public void AnExactNameFolderSignalDoesNotMatchASegmentThatOnlyEndsWithItsText()
	{
		Assert.Equal(
			ConfidenceLevel.High,
			LevelUnder(BuildDemotionSignals(FolderNameMatch.ExactName), _SEGMENT_MERELY_ENDING_IN_THE_EXACT_SIGNALS_TEXT_PATH));
	}

	private static ConfidenceLevel LevelUnderSuffixSignal(string relativePath)
	{
		return LevelUnder(BuildDemotionSignals(FolderNameMatch.Suffix), relativePath);
	}

	private static ConfidenceLevel LevelUnder(LanguageShapedDemotionSignals demotionSignals, string relativePath)
	{
		ILanguageAnalyzer analyzer = DemotionSignalProbe.BuildAnalyzer(demotionSignals);
		RecordingEmissionSink sink = new();
		EmissionPoint emissionPoint = new([sink]);
		RecognitionEngine engine = new(emissionPoint);

		FileRecognitionContext context =
			DemotionSignalProbe.ContextFor(analyzer, emissionPoint, _CREDENTIAL_CONSTRUCTION_IN_CODE, relativePath);
		ImmutableArray<RecognizedConstruct> recognized = engine.Recognize(context).Constructs;

		RecognizedConstruct construct = Assert.Single(recognized);
		Assert.Equal(_CREDENTIAL_TYPE_PATTERN_CLASS, construct.PatternClassIdentifier);

		return construct.Level;
	}

	private static LanguageShapedDemotionSignals BuildDemotionSignals(FolderNameMatch match)
	{
		string signalText = match is FolderNameMatch.Suffix ? _SUFFIX_FOLDER_SIGNAL_TEXT : _EXACT_FOLDER_SIGNAL_TEXT;

		return new LanguageShapedDemotionSignals
		{
			FileNameSignals = FrozenSet<FileNameSignal>.Empty,
			FolderNameSignals = new[]
			{
				new FolderNameSignal
				{
					FolderName = signalText,
					Match = match,
					Sensitivity = CaseSensitivity.Insensitive,
					Kind = DemotionKind.Test
				}
			}.ToFrozenSet(),
			GenerationMarkerExpressions = [],
			ContentMarkers = []
		};
	}
}
