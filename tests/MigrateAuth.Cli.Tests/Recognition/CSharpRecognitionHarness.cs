using System.Collections.Immutable;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Emission;
using MigrateAuth.Cli.Lexical;
using MigrateAuth.Cli.Reading;
using MigrateAuth.Cli.Recognition;
using MigrateAuth.Cli.Session;

namespace MigrateAuth.Cli.Tests.Recognition;

/// <summary>
/// THE REAL EMBEDDED `CSharp` ANALYZER, A REAL SCAN, AND A PATH THAT WENT THROUGH THE
/// WRITE BOUNDARY.
///
/// This is <see cref="RecognitionHarness"/>'s own shape, retargeted at the `CSharp` language name
/// rather than the reference fixture's `ExemplarOne`: <see cref="RecognitionHarness"/> is hard-coded
/// to the reference language, which is right for the shared engine's own golden tests and wrong for a real
/// language's own set. Every synthetic source text driven through this harness is written for this
/// set, from the catalog's own stated forms and exclusions, and carries no fragment, path or
/// third-party name from any repository.
/// </summary>
internal sealed class CSharpRecognitionHarness
{
	private const string _LANGUAGE_NAME = "CSharp";
	private const string _REPOSITORY_ROOT = "csharp-recognition-harness-repository";

	/// <summary>A path carrying no demotion signal, which most tests want.</summary>
	internal const string UNDEMOTED_RELATIVE_PATH = "source/Module.cs";

	private readonly RepositoryFolderPath _repository;

	internal CSharpRecognitionHarness()
	{
		_repository = RepositoryFolderPath.FromSuppliedText(Path.Combine(Path.GetTempPath(), _REPOSITORY_ROOT));
		Analyzer = LanguageCatalogRegistry.BuildEmbeddedCatalog().AnalyzersByLanguageName[_LANGUAGE_NAME];
		Sink = new RecordingEmissionSink();
		EmissionPoint = new EmissionPoint([Sink]);
	}

	internal ILanguageAnalyzer Analyzer { get; }

	internal RecordingEmissionSink Sink { get; }

	internal EmissionPoint EmissionPoint { get; }

	/// <summary>Recognition on a clock that never moves, which is every test but the bound's own.</summary>
	internal ImmutableArray<RecognizedConstruct> Recognize(
		string sourceText,
		string relativePath = UNDEMOTED_RELATIVE_PATH)
	{
		RecognitionEngine engine = new(EmissionPoint, new FrozenTimeProvider());

		return engine.Recognize(ContextFor(sourceText, relativePath)).Constructs;
	}

	/// <summary>One file, scanned exactly as the pipeline scans it.</summary>
	internal FileRecognitionContext ContextFor(string sourceText, string relativePath = UNDEMOTED_RELATIVE_PATH)
	{
		string normalizedText = TextNormalization.Normalize(sourceText);
		RepositoryRelativePath path = PathOf(relativePath);
		ZoneMask mask = new FileLexicalScanner(EmissionPoint).Scan(Analyzer.LexicalProfile, normalizedText, path);

		return new FileRecognitionContext
		{
			Analyzer = Analyzer,
			NormalizedText = normalizedText,
			Mask = mask,
			Path = path
		};
	}

	internal RepositoryRelativePath PathOf(string relativePath)
	{
		if (!RepositoryRelativePath.TryFromScannedPath(_repository, relativePath, out RepositoryRelativePath path))
		{
			throw new InvalidOperationException($"The harness could not build a relative path from '{relativePath}'.");
		}

		return path;
	}
}
