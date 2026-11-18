using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Text.RegularExpressions;

namespace MigrateAuth.Cli.Catalog.Languages;

/// <summary>The generation marker and the two content markers of the `Go` analyzer.</summary>
internal static partial class GoPatternExpressions
{
	/// <summary>
	/// The generated-file header: the line-comment form on a line of its own, preceded by nothing but blank lines and line comments (at most fifty of them, so the marker stands on line 51 at the
	/// latest). A header inside a block comment, after the first line of code, or without the closing words is not matched.
	/// </summary>
	private const string _EX_GO_110 =
		@"\A(?:[ \t]{0,8}(?://[^\n]{0,300})?\n){0,50}[ \t]{0,8}//[ ]Code generated [^\n]{0,300}[ ]DO NOT EDIT\.[ \t]{0,8}(?:\n|\z)";

	/// <summary>A documentation example: `func` at the line start, the name (`Example`, `ExampleName`, `ExampleType_Method`, `Example_suffix`), then `(` and `)`.</summary>
	private const string _EX_GO_111 =
		@"^func[ \t]{1,8}Example(?:[_\p{Lu}][\p{L}\p{N}_]{0,60})?[ \t]{0,8}\([ \t]{0,8}\)";

	/// <summary>
	/// A file name that follows the naming convention of a code generator, compared without regard to case: the suffixes <c>.pb.go</c>, <c>.pb.gw.go</c>, <c>_generated.go</c>,
	/// <c>.generated.go</c>, <c>_gen.go</c> and <c>.gen.go</c>, and the prefix <c>zz_generated.</c>.
	/// </summary>
	[GeneratedRegex(
		@"(?:\.pb(?:\.gw)?|[_.]generated|[_.]gen)\.go\z|\Azz_generated\.",
		RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture | RegexOptions.IgnoreCase,
		matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex GeneratedFileName();

	/// <summary>EX-GO-110: the generated-file header, read in every zone because the header is a comment.</summary>
	[GeneratedRegex(_EX_GO_110, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex GeneratedFileHeaderContentMarker();

	/// <summary>EX-GO-111: the documentation example function, read in code only.</summary>
	[GeneratedRegex(_EX_GO_111, _LINE_OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex ExampleFunctionContentMarker();
}

/// <summary>
/// The demotion signals of the `Go` analyzer, each with the kind it declares: a fired Test kind proposes Medium and any other fired kind proposes Low, the minimum taken once, and a finding
/// carries the test-code mark whenever Test fired. Every signal is declared <c>Insensitive</c>, because file and folder names are case-insensitive on Windows and macOS.
///
/// The test signals are the <c>_test.go</c> suffix and the folders <c>testdata</c>, <c>test</c>, <c>tests</c>, <c>e2e</c>, <c>fixture</c>, <c>fixtures</c>, <c>testutil</c> and <c>testutils</c>: a file under
/// <c>testdata</c> reads at most Medium, as every other test file does, and there is no separate kind for fixture data. The documentation examples are samples by the <c>example_test.go</c> suffix, by an
/// <c>Example</c> function in any file, and by the example folders. <c>vendor</c> and its siblings are walked, so the vendored folders are the only lever for them. Generated code is reached by the name
/// conventions of its generators and by the header comment. A mock, fake or stub is recognized by name only, so a file with a neutral name that is in fact one reports at its full level.
///
/// A folder named <c>Deployment</c> is deliberately not a signal, because that folder holds the code that authenticates. The lower-case folders <c>bin</c>, <c>obj</c>, <c>build</c>, <c>dist</c> and
/// <c>packages</c> are not signals either: the walk excludes them. A test that uses a mock-server package is a test by its name or its folder, never by the package it imports.
/// </summary>
internal static class GoDemotionDeclarations
{
	private const string _TEST_FILE_SUFFIX = "_test.go";
	private const string _EXAMPLE_TEST_FILE_SUFFIX = "example_test.go";

	private static readonly ImmutableArray<string> _testFolderNames = ["testdata", "test", "tests", "e2e", "fixture", "fixtures", "testutil", "testutils"];

	private static readonly ImmutableArray<string> _sampleFolderNames = ["example", "examples", "_example", "_examples", "sample", "samples", "demo", "demos"];

	private static readonly ImmutableArray<string> _vendoredFolderNames = ["vendor", "third_party", "third-party", "thirdparty", "Godeps"];

	private static readonly ImmutableArray<string> _mockFolderNames = ["mock", "mocks", "fake", "fakes", "stub", "stubs"];

	private static readonly ImmutableArray<string> _mockFileSuffixes = ["_mock.go", "_mocks.go", "_fake.go", "_stub.go"];

	private static readonly ImmutableArray<string> _mockFilePrefixes = ["mock_", "mocks_", "fake_", "stub_"];

	private static readonly ImmutableArray<string> _mockFileExactNames = ["mock.go", "mocks.go", "fake.go", "fakes.go", "stub.go", "stubs.go"];

	internal static LanguageShapedDemotionSignals Build()
	{
		LanguageShapedDemotionSignals signals = new()
		{
			FileNameSignals = BuildFileNameSignals(),
			FolderNameSignals = BuildFolderNameSignals(),
			GenerationMarkerExpressions = [GoPatternExpressions.GeneratedFileName()],
			ContentMarkers = BuildContentMarkers()
		};

		return signals;
	}

	private static FrozenSet<FileNameSignal> BuildFileNameSignals()
	{
		IEnumerable<FileNameSignal> signals = new[]
			{
				FileNameOf(FileNameMatch.Suffix, _TEST_FILE_SUFFIX, DemotionKind.Test),
				FileNameOf(FileNameMatch.Suffix, _EXAMPLE_TEST_FILE_SUFFIX, DemotionKind.Sample)
			}
			.Concat(_mockFileSuffixes.Select(text => FileNameOf(FileNameMatch.Suffix, text, DemotionKind.Mock)))
			.Concat(_mockFilePrefixes.Select(text => FileNameOf(FileNameMatch.Prefix, text, DemotionKind.Mock)))
			.Concat(_mockFileExactNames.Select(text => FileNameOf(FileNameMatch.ExactName, text, DemotionKind.Mock)));

		return signals.ToFrozenSet();
	}

	private static FileNameSignal FileNameOf(FileNameMatch match, string text, DemotionKind kind)
	{
		return new FileNameSignal { Match = match, Text = text, Sensitivity = CaseSensitivity.Insensitive, Kind = kind };
	}

	private static FrozenSet<FolderNameSignal> BuildFolderNameSignals()
	{
		IEnumerable<FolderNameSignal> signals = FoldersOf(_testFolderNames, DemotionKind.Test)
			.Concat(FoldersOf(_sampleFolderNames, DemotionKind.Sample))
			.Concat(FoldersOf(_vendoredFolderNames, DemotionKind.Vendored))
			.Concat(FoldersOf(_mockFolderNames, DemotionKind.Mock));

		return signals.ToFrozenSet();
	}

	private static IEnumerable<FolderNameSignal> FoldersOf(ImmutableArray<string> folderNames, DemotionKind kind)
	{
		return folderNames.Select(folderName => new FolderNameSignal { FolderName = folderName, Sensitivity = CaseSensitivity.Insensitive, Kind = kind });
	}

	/// <summary>The generated-file header is a comment, so it needs every zone; the documentation example is a declaration, so it reads code only and the word in a comment or a string is not a function.</summary>
	private static ImmutableArray<ContentMarker> BuildContentMarkers()
	{
		ImmutableArray<ContentMarker> markers =
		[
			new ContentMarker { Expression = GoPatternExpressions.GeneratedFileHeaderContentMarker(), Zone = ContentMarkerZone.AnyZone, Kind = DemotionKind.Generated },
			new ContentMarker { Expression = GoPatternExpressions.ExampleFunctionContentMarker(), Zone = ContentMarkerZone.CodeOnly, Kind = DemotionKind.Sample }
		];

		return markers;
	}
}
