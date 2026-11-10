using System.Collections.Immutable;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Emission;
using MigrateAuth.Cli.Lexical;
using MigrateAuth.Cli.Reading;
using MigrateAuth.Cli.Recognition;
using MigrateAuth.Cli.Session;
using MigrateAuth.Cli.Tests.Catalog;

namespace MigrateAuth.Cli.Tests.Recognition;

/// <summary>
/// THE SHIPPED `TypeScript` ANALYZER, A REAL SCAN AND THE REAL RECOGNITION ENGINE: <see cref="JavaScriptRecognitionHarness"/>'s shape retargeted at the analyzer that
/// <see cref="TypeScriptRegisteredExpressions"/> reads from the binary. Every script driven through it is invented for these tests from the forms and exclusions the catalog
/// states, with placeholder names, and carries no fragment, path or third-party name from any repository.
/// </summary>
internal sealed class TypeScriptRecognitionHarness
{
	private const string _REPOSITORY_ROOT = "typescript-recognition-harness-repository";

	/// <summary>A path carrying no demotion signal, which most tests want.</summary>
	internal const string UNDEMOTED_RELATIVE_PATH = "src/client.ts";

	private readonly RepositoryFolderPath _repository;

	internal TypeScriptRecognitionHarness()
	{
		_repository = RepositoryFolderPath.FromSuppliedText(Path.Combine(Path.GetTempPath(), _REPOSITORY_ROOT));
		Sink = new RecordingEmissionSink();
		EmissionPoint = new EmissionPoint([Sink]);
	}

	internal ILanguageAnalyzer Analyzer => TypeScriptRegisteredExpressions.Analyzer;

	internal RecordingEmissionSink Sink { get; }

	internal EmissionPoint EmissionPoint { get; }

	/// <summary>Recognition on a clock that never moves, which is every test but the bound's own.</summary>
	internal ImmutableArray<RecognizedConstruct> Recognize(string sourceText, string relativePath = UNDEMOTED_RELATIVE_PATH)
	{
		return RecognizeFully(sourceText, relativePath).Constructs;
	}

	/// <summary>The whole recognition result.</summary>
	internal RecognitionResult RecognizeFully(string sourceText, string relativePath = UNDEMOTED_RELATIVE_PATH)
	{
		RecognitionEngine engine = new(EmissionPoint, new FrozenTimeProvider());

		return engine.Recognize(ContextFor(sourceText, relativePath));
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
