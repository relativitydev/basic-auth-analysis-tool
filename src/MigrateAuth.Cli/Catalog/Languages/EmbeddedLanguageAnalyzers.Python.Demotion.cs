using System.Collections.Frozen;
using System.Collections.Immutable;

namespace MigrateAuth.Cli.Catalog.Languages;

/// <summary>
/// The demotion signals of the `Python` analyzer, each with the kind it declares: a fired Test kind proposes Medium and any other fired kind proposes Low, the minimum taken once, and a finding carries the test-code mark
/// whenever Test fired. Every file-name and folder-name signal is declared <c>Insensitive</c>, because file and folder names are case-insensitive on Windows and macOS. A demotion is never an exclusion.
///
/// A folder named <c>Deployment</c> is deliberately not a signal: a deployment folder holds the code that authenticates. <c>__pycache__</c>, <c>build</c> and <c>dist</c> are not signals either (they hold no source the
/// analyzer attributes, or are never walked), and neither are <c>env</c>, <c>virtualenv</c>, <c>.nox</c>, <c>.eggs</c> and a versioned name such as <c>venv3</c>, which are walked and declared as a cost. A mock, fake or stub
/// is recognized by folder or file name only, and a test that uses a mock library is a test by its name or its test class, never by the library it imports. The sample signals are folders only: a file prefix would reach a
/// source file named after the word.
///
/// THE TEST CLASS. A class statement whose base is a test-case class is the one content signal of kind Test; it reads code only, so the words in a comment or a string are not a test. The generated-file banner is a comment and
/// reads every zone. The file names <c>_pb2.py</c> and <c>_pb2_grpc.py</c> are one generation marker. The five vendored-environment folders besides <c>vendor</c> and <c>site-packages</c> are signals because the walk reads them.
/// </summary>
internal static class PythonDemotionDeclarations
{
	private const string _TEST_FILE_PREFIX = "test_";

	private static readonly ImmutableArray<string> _testFileSuffixes = ["_test.py", "_test.pyw"];

	private static readonly ImmutableArray<string> _testFileNames = ["conftest.py", "tests.py"];

	private static readonly ImmutableArray<string> _mockFilePrefixes = ["mock_", "stub_", "fake_"];

	private static readonly ImmutableArray<string> _mockFileSuffixes = ["_mock.py", "_stub.py", "_fake.py"];

	private static readonly ImmutableArray<string> _mockFileNames = ["mock.py", "mocks.py", "fakes.py"];

	private static readonly ImmutableArray<string> _testFolderNames = ["test", "tests", "testing", "fixture", "fixtures", "e2e"];

	private static readonly ImmutableArray<string> _mockFolderNames = ["mock", "mocks", "stub", "stubs", "fake", "fakes"];

	private static readonly ImmutableArray<string> _sampleFolderNames = ["sample", "samples", "example", "examples", "demo", "demos"];

	private static readonly ImmutableArray<string> _vendoredFolderNames = ["vendor", "site-packages", "venv", ".venv", "dist-packages", "__pypackages__", ".tox"];

	internal static LanguageShapedDemotionSignals Build()
	{
		LanguageShapedDemotionSignals signals = new()
		{
			FileNameSignals = BuildFileNameSignals(),
			FolderNameSignals = BuildFolderNameSignals(),
			GenerationMarkerExpressions = [PythonPatternExpressions.GeneratedStubFileNameMarker()],
			ContentMarkers = BuildContentMarkers()
		};

		return signals;
	}

	private static FrozenSet<FileNameSignal> BuildFileNameSignals()
	{
		IEnumerable<FileNameSignal> signals = FileNamesOf(FileNameMatch.Prefix, [_TEST_FILE_PREFIX], DemotionKind.Test)
			.Concat(FileNamesOf(FileNameMatch.Suffix, _testFileSuffixes, DemotionKind.Test))
			.Concat(FileNamesOf(FileNameMatch.ExactName, _testFileNames, DemotionKind.Test))
			.Concat(FileNamesOf(FileNameMatch.Prefix, _mockFilePrefixes, DemotionKind.Mock))
			.Concat(FileNamesOf(FileNameMatch.Suffix, _mockFileSuffixes, DemotionKind.Mock))
			.Concat(FileNamesOf(FileNameMatch.ExactName, _mockFileNames, DemotionKind.Mock));

		return signals.ToFrozenSet();
	}

	private static IEnumerable<FileNameSignal> FileNamesOf(FileNameMatch match, ImmutableArray<string> texts, DemotionKind kind)
	{
		return texts.Select(text => new FileNameSignal { Match = match, Text = text, Sensitivity = CaseSensitivity.Insensitive, Kind = kind });
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

	/// <summary>The generated-file banner is a comment, so it needs every zone; the test-case class is a statement, so it reads code only and the words in a comment or a string are not a test.</summary>
	private static ImmutableArray<ContentMarker> BuildContentMarkers()
	{
		ImmutableArray<ContentMarker> markers =
		[
			new ContentMarker { Expression = PythonPatternExpressions.GeneratedFileBannerContentMarker(), Zone = ContentMarkerZone.AnyZone, Kind = DemotionKind.Generated },
			new ContentMarker { Expression = PythonPatternExpressions.UnitTestCaseContentMarker(), Zone = ContentMarkerZone.CodeOnly, Kind = DemotionKind.Test }
		];

		return markers;
	}
}
