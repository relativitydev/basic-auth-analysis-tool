using System.Collections.Immutable;
using System.Globalization;
using System.Text.RegularExpressions;

using MigrateAuth.Cli.Tests.Documentation;

using Xunit;

namespace MigrateAuth.Cli.Tests.Release;

/// <summary>
/// THE PUBLIC SOURCE CARRIES NO INTERNAL REFERENCE.
///
/// The source under <c>src/</c>, this test project and the worst-case generator is published with the tool, so no
/// comment, string or file in it may carry a reference that a reader of the published source cannot resolve. The
/// same holds, with a different list, for the root files, the release scripts and <c>docs/public/</c>.
///
/// THIS FILE CARRIES THE MECHANISM ONLY. The lists of what is forbidden are not written here: they are filled in by
/// a companion part of this class that is left out of the published set. Where the companion part is absent, every
/// list is empty and the checks below pass over nothing, which is the intended behavior of the published source.
/// </summary>
public sealed partial class PublicSourceIdentifierTests
{
	private const string _SOURCE_FOLDER_NAME = "src";
	private const string _TEST_PROJECT_FOLDER_PATH = "tests/MigrateAuth.Cli.Tests";
	private const string _GENERATOR_PROJECT_FOLDER_PATH = "tests/WorstCaseGenerator";
	private const string _PUBLIC_DOCUMENTS_FOLDER_PATH = "docs/public";
	private const string _SCRIPTS_FOLDER_NAME = "scripts";
	private const string _RELEASE_SCRIPT_SEARCH_PATTERN = "build-release.*";
	private const string _BUILD_OUTPUT_FOLDER_NAME = "bin";
	private const string _INTERMEDIATE_OUTPUT_FOLDER_NAME = "obj";
	private const string _ALL_FILES_SEARCH_PATTERN = "*";
	private const string _FOUND_MESSAGE = "{0}:{1}: found the forbidden text '{2}'";
	private const string _SELF_CHECK_SAMPLE_TEMPLATE = "See {0} and also {1} for the reasons.";
	private const string _CLEAN_SAMPLE = "This comment explains the reasoning and cites nothing internal.";
	private const string _SYNTHETIC_FILE_NAME = "synthetic";
	private const string _ORDINARY_VOCABULARY_SAMPLE =
		"A linear scan, a degrading run, the product of two counts, upgrading, an incremental write and a hex value 0xM5 are ordinary words.";
	private const string _LEGITIMATE_NAMES_SAMPLE =
		"Pattern classes CS-011, PS-003, JS-001, EXM-001, PIP-002, TST-001, PRB-001, MLF-001, LOA-001, LOB-001, XX-001 and C-001; "
		+ "encodings UTF-8, UTF-16, ISO-8859-1 and Latin-1; the hash name SHA-256; section 4 of the report; "
		+ "numeric formats {0:D6} and {0:D5}.";
	private const char _PATH_SEPARATOR_FOR_DISPLAY = '/';
	private const char _LINE_SEPARATOR = '\n';

	/// <summary>The root files and the scripts, besides the release scripts, that the public set publishes.</summary>
	private static readonly string[] _publishedLooseFilePaths =
	[
		"Directory.Build.props",
		"global.json",
		"LICENSE",
		"migrate-auth.sln",
		"scripts/check-public-links.ps1",
		"scripts/sha256sums.sh"
	];

	/// <summary>The extensions of the text files that ship with the source; anything else is a binary or generated file.</summary>
	private static readonly ImmutableHashSet<string> _textFileExtensions = ImmutableHashSet.Create(
		StringComparer.OrdinalIgnoreCase,
		".cs", ".csproj", ".props", ".targets", ".sln", ".json", ".xml", ".manifest", ".md", ".txt", ".resx", ".editorconfig", ".config", ".yml", ".yaml", ".ps1", ".sh");

	private static readonly InternalVocabulary _vocabulary = LoadVocabulary();

	/// <summary>No published text file under <c>src/</c>, this test project or the generator carries a forbidden identifier or decision code.</summary>
	[Fact]
	public void NoTextFileOfThePublicSourceNamesAnInternalIdentifier()
	{
		string root = PublicDocuments.RepositoryRoot();
		string[] roots =
		[
			Path.Combine(root, _SOURCE_FOLDER_NAME),
			Path.Combine(root, _TEST_PROJECT_FOLDER_PATH),
			Path.Combine(root, _GENERATOR_PROJECT_FOLDER_PATH)
		];

		string[] files = [.. roots.SelectMany(folder => SourceFilesUnder(folder))];
		string[] problems = [.. files.SelectMany(path => ProblemsInFile(root, path, InternalIdentifierPatterns()))];

		Assert.NotEmpty(files);
		Assert.True(problems.Length == 0, string.Join(Environment.NewLine, problems));
	}

	/// <summary>No file of the whole published set, the folders above plus the root files, the scripts and the public documents, carries forbidden wording.</summary>
	[Fact]
	public void NoFileOfThePublishedSetNamesInternalProcessWording()
	{
		string root = PublicDocuments.RepositoryRoot();
		string[] files =
		[
			.. new[] { _SOURCE_FOLDER_NAME, _TEST_PROJECT_FOLDER_PATH, _GENERATOR_PROJECT_FOLDER_PATH, _PUBLIC_DOCUMENTS_FOLDER_PATH }
				.SelectMany(folder => SourceFilesUnder(Path.Combine(root, folder))),
			.. Directory.EnumerateFiles(Path.Combine(root, _SCRIPTS_FOLDER_NAME), _RELEASE_SCRIPT_SEARCH_PATTERN),
			.. _publishedLooseFilePaths.Select(relativePath => Path.Combine(root, relativePath))
		];
		ImmutableArray<Regex> patterns = [.. ToPatterns(_vocabulary.WholeSetWording), .. ToPatterns(_vocabulary.DecisionCodes)];
		string[] problems = [.. files.SelectMany(path => ProblemsInFile(root, path, patterns))];

		Assert.NotEmpty(files);
		Assert.True(problems.Length == 0, string.Join(Environment.NewLine, problems));
	}

	/// <summary>No file of the published set outside the public documents, which explain the product to a client in their own words, carries wording reserved for code.</summary>
	[Fact]
	public void NoCodeFileOfThePublishedSetNamesInternalRecordsOrProcessVocabulary()
	{
		string root = PublicDocuments.RepositoryRoot();
		string[] files =
		[
			.. new[] { _SOURCE_FOLDER_NAME, _TEST_PROJECT_FOLDER_PATH, _GENERATOR_PROJECT_FOLDER_PATH }
				.SelectMany(folder => SourceFilesUnder(Path.Combine(root, folder))),
			.. Directory.EnumerateFiles(Path.Combine(root, _SCRIPTS_FOLDER_NAME), _RELEASE_SCRIPT_SEARCH_PATTERN),
			.. _publishedLooseFilePaths.Select(relativePath => Path.Combine(root, relativePath))
		];
		ImmutableArray<Regex> patterns = ToPatterns(_vocabulary.CodeOnlyWording);
		string[] problems = [.. files.SelectMany(path => ProblemsInFile(root, path, patterns))];

		Assert.NotEmpty(files);
		Assert.True(problems.Length == 0, string.Join(Environment.NewLine, problems));
	}

	/// <summary>The identifier and decision-code check fires on every pattern it is built from, on its own sample, and passes clean text.</summary>
	[Fact]
	public void TheIdentifierCheckReportsEveryPatternItIsBuiltFromAndPassesCleanText()
	{
		AssertEveryPatternFiresOnItsOwnSample([.. _vocabulary.CodeIdentifiers, .. _vocabulary.DecisionCodes]);
		Assert.Empty(ProblemsInText(_SYNTHETIC_FILE_NAME, _CLEAN_SAMPLE, InternalIdentifierPatterns()));
	}

	/// <summary>The whole-set wording check fires on every pattern it is built from, on its own sample, and passes clean text.</summary>
	[Fact]
	public void TheWholeSetWordingCheckReportsEveryPatternItIsBuiltFromAndPassesCleanText()
	{
		AssertEveryPatternFiresOnItsOwnSample(_vocabulary.WholeSetWording);
		Assert.Empty(ProblemsInText(_SYNTHETIC_FILE_NAME, _CLEAN_SAMPLE, ToPatterns(_vocabulary.WholeSetWording)));
	}

	/// <summary>The code-only wording check fires on every pattern it is built from, on its own sample, and passes clean text.</summary>
	[Fact]
	public void TheCodeOnlyWordingCheckReportsEveryPatternItIsBuiltFromAndPassesCleanText()
	{
		AssertEveryPatternFiresOnItsOwnSample(_vocabulary.CodeOnlyWording);
		Assert.Empty(ProblemsInText(_SYNTHETIC_FILE_NAME, _CLEAN_SAMPLE, ToPatterns(_vocabulary.CodeOnlyWording)));
	}

	/// <summary>Ordinary technical words are not mistaken for forbidden wording by any list.</summary>
	[Fact]
	public void OrdinaryTechnicalVocabularyIsNotForbiddenWording()
	{
		Assert.Empty(ProblemsInText(_SYNTHETIC_FILE_NAME, _ORDINARY_VOCABULARY_SAMPLE, AllPatterns()));
	}

	/// <summary>
	/// What is legitimate is not mistaken for an internal reference: pattern class identifiers, character encodings, hash names,
	/// the report's own section numbers, and numeric format strings.
	/// </summary>
	[Fact]
	public void PatternClassIdentifiersEncodingsHashNamesAndFormatStringsAreNotForbidden()
	{
		Assert.Empty(ProblemsInText(_SYNTHETIC_FILE_NAME, _LEGITIMATE_NAMES_SAMPLE, AllPatterns()));
	}

	/// <summary>
	/// The internal-identifier patterns this check is built from, for the tests that hold client-facing text to the same list, so that the list
	/// has one source and no other test writes a forbidden identifier out.
	/// </summary>
	internal static ImmutableArray<Regex> InternalIdentifierPatterns()
	{
		return [.. ToPatterns(_vocabulary.CodeIdentifiers), .. ToPatterns(_vocabulary.DecisionCodes)];
	}

	/// <summary>One sample per internal-identifier pattern, for the tests that prove their check still fires.</summary>
	internal static ImmutableArray<string> InternalIdentifierSamples()
	{
		return [.. _vocabulary.CodeIdentifiers.Concat(_vocabulary.DecisionCodes).Select(entry => entry.Sample)];
	}

	/// <summary>The extra patterns a template text must not match, besides the internal identifiers.</summary>
	internal static ImmutableArray<Regex> InternalTemplateTermPatterns()
	{
		return ToPatterns(_vocabulary.TemplateTerms);
	}

	/// <summary>The extra identifier patterns a client-facing template text must not match, besides the internal identifiers.</summary>
	internal static ImmutableArray<Regex> InternalTemplateIdentifierPatterns()
	{
		return ToPatterns(_vocabulary.TemplateIdentifiers);
	}

	/// <summary>The plain terms a declared unsupported-coverage text must not contain.</summary>
	internal static ImmutableArray<string> InternalCoverageTerms()
	{
		return [.. _vocabulary.CoverageTerms];
	}

	/// <summary>The fragments a client-facing description must not contain, matched without regard to case.</summary>
	internal static ImmutableArray<string> InternalClientFacingFragments()
	{
		return [.. _vocabulary.ClientFacingFragments];
	}

	/// <summary>The fragments a documentation link target must not contain.</summary>
	internal static ImmutableArray<string> InternalLinkTargetFragments()
	{
		return [.. _vocabulary.LinkTargetFragments];
	}

	/// <summary>
	/// Fills the lists of what is forbidden. The companion part of this class implements it; where that part is absent, the call is removed
	/// at compile time and every list stays empty.
	/// </summary>
	static partial void FillInternalVocabulary(InternalVocabulary vocabulary);

	private static InternalVocabulary LoadVocabulary()
	{
		InternalVocabulary vocabulary = new();

		FillInternalVocabulary(vocabulary);

		return vocabulary;
	}

	private static ImmutableArray<Regex> ToPatterns(IEnumerable<(string Pattern, string Sample)> entries)
	{
		return [.. entries.Select(entry => new Regex(entry.Pattern, RegexOptions.CultureInvariant))];
	}

	private static ImmutableArray<Regex> AllPatterns()
	{
		return
		[
			.. ToPatterns(_vocabulary.CodeIdentifiers),
			.. ToPatterns(_vocabulary.DecisionCodes),
			.. ToPatterns(_vocabulary.WholeSetWording),
			.. ToPatterns(_vocabulary.CodeOnlyWording)
		];
	}

	private static void AssertEveryPatternFiresOnItsOwnSample(IEnumerable<(string Pattern, string Sample)> entries)
	{
		foreach ((string pattern, string sample) in entries)
		{
			Regex regex = new(pattern, RegexOptions.CultureInvariant);
			string text = string.Format(CultureInfo.InvariantCulture, _SELF_CHECK_SAMPLE_TEMPLATE, sample, _CLEAN_SAMPLE);

			Assert.NotEmpty(ProblemsInText(_SYNTHETIC_FILE_NAME, text, [regex]));
		}
	}

	private static IEnumerable<string> SourceFilesUnder(string folder)
	{
		return Directory.EnumerateFiles(folder, _ALL_FILES_SEARCH_PATTERN, SearchOption.AllDirectories)
			.Where(path => !IsBuildOutput(folder, path)
				&& _textFileExtensions.Contains(Path.GetExtension(path))
				&& !_vocabulary.FilesLeftOutOfTheScan.Contains(Path.GetFileName(path), StringComparer.Ordinal))
			.Order(StringComparer.Ordinal);
	}

	private static bool IsBuildOutput(string folder, string path)
	{
		string[] segments = Path.GetRelativePath(folder, path).Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

		return segments.Any(segment => segment.Equals(_BUILD_OUTPUT_FOLDER_NAME, StringComparison.OrdinalIgnoreCase)
			|| segment.Equals(_INTERMEDIATE_OUTPUT_FOLDER_NAME, StringComparison.OrdinalIgnoreCase));
	}

	private static ImmutableArray<string> ProblemsInFile(string root, string path, ImmutableArray<Regex> patterns)
	{
		string displayName = Path.GetRelativePath(root, path).Replace(Path.DirectorySeparatorChar, _PATH_SEPARATOR_FOR_DISPLAY);

		return ProblemsInText(displayName, File.ReadAllText(path), patterns);
	}

	private static ImmutableArray<string> ProblemsInText(string name, string text, ImmutableArray<Regex> patterns)
	{
		ImmutableArray<string>.Builder problems = ImmutableArray.CreateBuilder<string>();
		string[] lines = text.Split(_LINE_SEPARATOR);

		for (int index = 0; index < lines.Length; index++)
		{
			foreach (Regex pattern in patterns)
			{
				foreach (Match match in pattern.Matches(lines[index]))
				{
					problems.Add(string.Format(CultureInfo.InvariantCulture, _FOUND_MESSAGE, name, index + 1, match.Value));
				}
			}
		}

		return problems.ToImmutable();
	}

	/// <summary>The lists the checks are built from. Each pattern entry pairs a regular expression with one sample the expression must match.</summary>
	private sealed class InternalVocabulary
	{
		/// <summary>Identifiers forbidden in the three scanned code folders.</summary>
		public List<(string Pattern, string Sample)> CodeIdentifiers { get; } = [];

		/// <summary>Decision codes forbidden in the three scanned code folders and in the whole published set.</summary>
		public List<(string Pattern, string Sample)> DecisionCodes { get; } = [];

		/// <summary>Wording forbidden in the whole published set, public documents included.</summary>
		public List<(string Pattern, string Sample)> WholeSetWording { get; } = [];

		/// <summary>Wording forbidden in code, scripts and root files, but not in the public documents.</summary>
		public List<(string Pattern, string Sample)> CodeOnlyWording { get; } = [];

		/// <summary>Extra patterns a template text must not match.</summary>
		public List<(string Pattern, string Sample)> TemplateTerms { get; } = [];

		/// <summary>Extra identifier patterns a client-facing template text must not match.</summary>
		public List<(string Pattern, string Sample)> TemplateIdentifiers { get; } = [];

		/// <summary>Plain terms a declared unsupported-coverage text must not contain.</summary>
		public List<string> CoverageTerms { get; } = [];

		/// <summary>Fragments a client-facing description must not contain.</summary>
		public List<string> ClientFacingFragments { get; } = [];

		/// <summary>Fragments a documentation link target must not contain.</summary>
		public List<string> LinkTargetFragments { get; } = [];

		/// <summary>File names the scans leave out.</summary>
		public List<string> FilesLeftOutOfTheScan { get; } = [];
	}
}
