using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Text.RegularExpressions;

namespace MigrateAuth.Cli.Catalog.Languages;

/// <summary>The generation marker of the `JavaScript` analyzer, evaluated over a file name and the folder segments of its path.</summary>
internal static partial class JavaScriptPatternExpressions
{
	/// <summary>A file name that ends in a minified, bundled or chunked output suffix, compared without regard to case: <c>.min.js</c>, <c>.bundle.js</c> or <c>.chunk.js</c>.</summary>
	[GeneratedRegex(
		@"\.(?:min|bundle|chunk)\.js\z",
		RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture | RegexOptions.IgnoreCase,
		matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex GeneratedOutputSuffix();
}

/// <summary>
/// The demotion signals of the `JavaScript` analyzer, each with the kind it declares: a fired Test kind proposes Medium and any other fired kind proposes Low, the
/// minimum taken once, and a finding carries the test-code mark whenever Test fired. Every signal is declared <c>Insensitive</c>, because file and folder names are
/// case-insensitive on Windows and macOS. A folder named <c>Deployment</c> is not a signal, and neither are <c>node_modules</c>, <c>dist</c> and <c>build</c>
/// (the walk never reads them) nor the upper-case <c>Dist</c> and <c>BUILD</c>.
///
/// A mock, fake or stub is recognized by folder or file name only, so a file with a neutral name that is in fact a mock reports at its full level. A test that uses a
/// mock-server library is a test by its folder, its suffix or its framework marker, never by the library it imports. The sample signals are folders only.
/// </summary>
internal static class JavaScriptDemotionDeclarations
{
	private static readonly ImmutableArray<string> _javaScriptExtensions = [".js", ".mjs", ".cjs", ".jsx"];

	private static readonly ImmutableArray<string> _testFileStems = [".test", ".spec"];

	private static readonly ImmutableArray<string> _mockFileStems = [".mock", ".stub", ".fake"];

	/// <summary>A file whose name starts with the word and an underscore, so <c>mock_server.test.js</c> reads as a test and a mock at once. No other prefix is declared.</summary>
	private static readonly ImmutableArray<string> _mockFilePrefixes = ["mock_"];

	private static readonly ImmutableArray<string> _testFolderNames = ["test", "tests", "__tests__", "spec", "e2e", "fixture", "fixtures", "cypress"];

	private static readonly ImmutableArray<string> _mockFolderNames = ["__mocks__", "mock", "mocks", "stub", "stubs", "fake", "fakes"];

	private static readonly ImmutableArray<string> _sampleFolderNames = ["sample", "samples", "example", "examples", "demo", "demos"];

	private static readonly ImmutableArray<string> _vendoredFolderNames = ["vendor", "third-party", "third_party", "bower_components"];

	internal static LanguageShapedDemotionSignals Build()
	{
		LanguageShapedDemotionSignals signals = new()
		{
			FileNameSignals = BuildFileNameSignals(),
			FolderNameSignals = BuildFolderNameSignals(),
			GenerationMarkerExpressions = [JavaScriptPatternExpressions.GeneratedOutputSuffix()],
			ContentMarkers = BuildContentMarkers()
		};

		return signals;
	}

	private static FrozenSet<FileNameSignal> BuildFileNameSignals()
	{
		IEnumerable<FileNameSignal> signals = SuffixesOf(_testFileStems, DemotionKind.Test)
			.Concat(SuffixesOf(_mockFileStems, DemotionKind.Mock))
			.Concat(_mockFilePrefixes.Select(prefix => new FileNameSignal { Match = FileNameMatch.Prefix, Text = prefix, Sensitivity = CaseSensitivity.Insensitive, Kind = DemotionKind.Mock }));

		return signals.ToFrozenSet();
	}

	/// <summary>One suffix per stem and per extension, the separator part of the suffix, so <c>.test</c> gives <c>.test.js</c>, <c>.test.mjs</c>, <c>.test.cjs</c> and <c>.test.jsx</c> and <c>latest.js</c> does not end in one.</summary>
	private static IEnumerable<FileNameSignal> SuffixesOf(ImmutableArray<string> stems, DemotionKind kind)
	{
		return stems.SelectMany(
			stem => _javaScriptExtensions,
			(stem, extension) => new FileNameSignal { Match = FileNameMatch.Suffix, Text = stem + extension, Sensitivity = CaseSensitivity.Insensitive, Kind = kind });
	}

	private static FrozenSet<FolderNameSignal> BuildFolderNameSignals()
	{
		IEnumerable<FolderNameSignal> signals = FoldersOf(_testFolderNames, DemotionKind.Test)
			.Concat(FoldersOf(_mockFolderNames, DemotionKind.Mock))
			.Concat(FoldersOf(_sampleFolderNames, DemotionKind.Sample))
			.Concat(FoldersOf(_vendoredFolderNames, DemotionKind.Vendored));

		return signals.ToFrozenSet();
	}

	private static IEnumerable<FolderNameSignal> FoldersOf(ImmutableArray<string> folderNames, DemotionKind kind)
	{
		return folderNames.Select(folderName => new FolderNameSignal { FolderName = folderName, Sensitivity = CaseSensitivity.Insensitive, Kind = kind });
	}

	/// <summary>The generated-file banner is a comment, so it needs every zone; the test-framework marker is a call, so it reads code only and the three words in a comment or a string are not a test.</summary>
	private static ImmutableArray<ContentMarker> BuildContentMarkers()
	{
		ImmutableArray<ContentMarker> markers =
		[
			new ContentMarker { Expression = JavaScriptPatternExpressions.GeneratedFileBannerContentMarker(), Zone = ContentMarkerZone.AnyZone, Kind = DemotionKind.Generated },
			new ContentMarker { Expression = JavaScriptPatternExpressions.TestFrameworkContentMarker(), Zone = ContentMarkerZone.CodeOnly, Kind = DemotionKind.Test }
		];

		return markers;
	}
}
