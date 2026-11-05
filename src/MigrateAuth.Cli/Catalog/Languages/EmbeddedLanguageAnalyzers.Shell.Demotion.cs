using System.Collections.Frozen;
using System.Collections.Immutable;

namespace MigrateAuth.Cli.Catalog.Languages;

/// <summary>
/// The demotion signals of the `Shell` analyzer, each with the kind it declares: a fired Test kind proposes Medium and any other fired kind proposes Low, the
/// minimum taken once, and a finding carries the test-code mark whenever Test fired. Every signal is declared <c>Insensitive</c>, because file and folder names
/// are case-insensitive on Windows and macOS.
///
/// A mock, fake or stub is recognized by folder or file name only. A script with a neutral name that is in fact a mock reports at its full level, because no
/// text says it is one. The sample and mock prefixes are the stems themselves, so a file whose name merely starts with the word is read as one and still
/// reported, at Low, for manual review.
/// </summary>
internal static class ShellDemotionDeclarations
{
	private static readonly ImmutableArray<string> _shellExtensions = [".sh", ".bash", ".zsh", ".ksh"];

	private static readonly ImmutableArray<string> _testFileStems = ["_test", "-test", ".test", "_spec"];

	private static readonly ImmutableArray<string> _testFilePrefixes = ["test_", "test-"];

	private static readonly ImmutableArray<string> _mockFileStems = ["_mock", "_fake", "_stub"];

	private static readonly ImmutableArray<string> _mockFilePrefixes = ["mock_", "fake_", "stub_"];

	private static readonly ImmutableArray<string> _sampleFilePrefixes = ["sample", "example"];

	private static readonly ImmutableArray<string> _testFolderNames = ["test", "tests", "spec", "fixture", "fixtures"];

	private static readonly ImmutableArray<string> _mockFolderNames = ["mock", "mocks", "stub", "stubs", "fake", "fakes"];

	private static readonly ImmutableArray<string> _sampleFolderNames = ["sample", "samples", "example", "examples"];

	private static readonly ImmutableArray<string> _vendoredFolderNames = ["vendor", "third-party", "third_party"];

	internal static LanguageShapedDemotionSignals Build()
	{
		LanguageShapedDemotionSignals signals = new()
		{
			FileNameSignals = BuildFileNameSignals(),
			FolderNameSignals = BuildFolderNameSignals(),
			GenerationMarkerExpressions = [],
			ContentMarkers = BuildContentMarkers()
		};

		return signals;
	}

	private static FrozenSet<FileNameSignal> BuildFileNameSignals()
	{
		IEnumerable<FileNameSignal> signals = SuffixesOf(_testFileStems, DemotionKind.Test)
			.Concat(PrefixesOf(_testFilePrefixes, DemotionKind.Test))
			.Concat(SuffixesOf(_mockFileStems, DemotionKind.Mock))
			.Concat(PrefixesOf(_mockFilePrefixes, DemotionKind.Mock))
			.Concat(PrefixesOf(_sampleFilePrefixes, DemotionKind.Sample));

		return signals.ToFrozenSet();
	}

	/// <summary>One suffix per stem and per shell extension, so <c>_test</c> gives <c>_test.sh</c>, <c>_test.bash</c>, <c>_test.zsh</c> and <c>_test.ksh</c>.</summary>
	private static IEnumerable<FileNameSignal> SuffixesOf(ImmutableArray<string> stems, DemotionKind kind)
	{
		return stems.SelectMany(
			stem => _shellExtensions,
			(stem, extension) => new FileNameSignal { Match = FileNameMatch.Suffix, Text = stem + extension, Sensitivity = CaseSensitivity.Insensitive, Kind = kind });
	}

	private static IEnumerable<FileNameSignal> PrefixesOf(ImmutableArray<string> prefixes, DemotionKind kind)
	{
		return prefixes.Select(prefix => new FileNameSignal { Match = FileNameMatch.Prefix, Text = prefix, Sensitivity = CaseSensitivity.Insensitive, Kind = kind });
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

	private static ImmutableArray<ContentMarker> BuildContentMarkers()
	{
		ImmutableArray<ContentMarker> markers =
		[
			new ContentMarker { Expression = ShellPatternExpressions.GeneratedHeaderContentMarker(), Zone = ContentMarkerZone.AnyZone, Kind = DemotionKind.Generated },
			new ContentMarker { Expression = ShellPatternExpressions.SourcedTestLibraryContentMarker(), Zone = ContentMarkerZone.CodeOnly, Kind = DemotionKind.Test },
			new ContentMarker { Expression = ShellPatternExpressions.BehaviourDrivenTestContentMarker(), Zone = ContentMarkerZone.CodeOnly, Kind = DemotionKind.Test }
		];

		return markers;
	}
}
