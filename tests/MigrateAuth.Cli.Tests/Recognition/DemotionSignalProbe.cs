using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Emission;
using MigrateAuth.Cli.Lexical;
using MigrateAuth.Cli.Recognition;
using MigrateAuth.Cli.Session;

using MigrateAuth.Cli.Tests.Fixtures;

using Moq;

namespace MigrateAuth.Cli.Tests.Recognition;

/// <summary>
/// A SECOND ROUTE TO A REAL <see cref="FileRecognitionContext"/>, FOR THE DEMOTION SIGNALS THE
/// EXEMPLAR FIXTURE'S OWN DECLARATION DOES NOT EXERCISE.
///
/// <see cref="RecognitionHarness"/> is built around ONE fixed analyzer - the reference
/// fixture analyzer, exactly as declared - which is right for most of this project and wrong for a test
/// that needs a DIFFERENT <see cref="LanguageShapedDemotionSignals"/> declaration: a folder signal
/// matched by suffix, or a content marker built to trip the match bound. This follows
/// <see cref="MatchBoundTests"/>'s own precedent instead: a Moq analyzer built over the real
/// reference profile's lexical grammar and detection rules - so scanning and classification stay
/// real - with only <see cref="ILanguageAnalyzer.DemotionSignals"/> replaced by what one test
/// wants to prove.
/// </summary>
internal static class DemotionSignalProbe
{
	private const string _REFERENCE_LANGUAGE_NAME = "ExemplarOne";
	private const string _REPOSITORY_ROOT = "demotion-signal-probe-repository";

	private static readonly RepositoryFolderPath _repository =
		RepositoryFolderPath.FromSuppliedText(Path.Combine(Path.GetTempPath(), _REPOSITORY_ROOT));

	/// <summary>The real Exemplar grammar and detection rules, with the demotion signals supplied.</summary>
	internal static ILanguageAnalyzer BuildAnalyzer(LanguageShapedDemotionSignals demotionSignals)
	{
		ILanguageAnalyzer reference =
			ExemplarFixtureCatalog.Build().AnalyzersByLanguageName[_REFERENCE_LANGUAGE_NAME];
		Mock<ILanguageAnalyzer> analyzer = new();

		analyzer.SetupGet(supplied => supplied.LexicalProfile).Returns(reference.LexicalProfile);
		analyzer.SetupGet(supplied => supplied.LanguageIdentification).Returns(reference.LanguageIdentification);
		analyzer.SetupGet(supplied => supplied.DetectionRulesByPatternClass)
			.Returns(reference.DetectionRulesByPatternClass);
		analyzer.SetupGet(supplied => supplied.DemotionSignals).Returns(demotionSignals);
		analyzer.SetupGet(supplied => supplied.CandidateSelectionAndRecommendation)
			.Returns(reference.CandidateSelectionAndRecommendation);
		analyzer.SetupGet(supplied => supplied.SubsumptionRelation).Returns(reference.SubsumptionRelation);

		return analyzer.Object;
	}

	internal static RepositoryRelativePath PathOf(string relativePath)
	{
		if (!RepositoryRelativePath.TryFromScannedPath(_repository, relativePath, out RepositoryRelativePath path))
		{
			throw new InvalidOperationException($"The probe could not build a relative path from '{relativePath}'.");
		}

		return path;
	}

	/// <summary>One file, scanned by the supplied analyzer's own lexical profile.</summary>
	internal static FileRecognitionContext ContextFor(
		ILanguageAnalyzer analyzer,
		EmissionPoint emissionPoint,
		string sourceText,
		string relativePath)
	{
		string normalizedText = RecognitionHarness.Normalize(sourceText);
		RepositoryRelativePath path = PathOf(relativePath);
		ZoneMask mask = new FileLexicalScanner(emissionPoint).Scan(analyzer.LexicalProfile, normalizedText, path);

		return new FileRecognitionContext
		{
			Analyzer = analyzer,
			NormalizedText = normalizedText,
			Mask = mask,
			Path = path
		};
	}
}
