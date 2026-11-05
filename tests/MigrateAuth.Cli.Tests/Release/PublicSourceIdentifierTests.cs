using System.Collections.Immutable;
using System.Globalization;
using System.Text.RegularExpressions;

using MigrateAuth.Cli.Tests.Documentation;

using Xunit;

namespace MigrateAuth.Cli.Tests.Release;

/// <summary>
/// THE PUBLIC SOURCE CARRIES NO INTERNAL IDENTIFIER.
///
/// The source under <c>src/</c>, this test project and the worst-case generator is published with the tool,
/// so no comment, string or file in it may name a work item, an open condition, an internal question, a
/// requirement or acceptance-criterion number of the internal definition documents, a section or feature
/// number of the requirements document, a path under the internal documentation folder or an internal
/// document by name, a delivery-tracking or review-round tag, or the calibration repositories, or quote the
/// internal handling markers. Decision-record citations are the one
/// kind of internal reference that stays. Every forbidden pattern and every sample used to prove it is
/// ASSEMBLED FROM FRAGMENTS, so the literal strings never appear in this file and this file does not trip the
/// very check it defines.
/// </summary>
public sealed class PublicSourceIdentifierTests
{
	private const string _SOURCE_FOLDER_NAME = "src";
	private const string _TEST_PROJECT_FOLDER_PATH = "tests/MigrateAuth.Cli.Tests";
	private const string _GENERATOR_PROJECT_FOLDER_PATH = "tests/WorstCaseGenerator";
	private const string _BUILD_OUTPUT_FOLDER_NAME = "bin";
	private const string _INTERMEDIATE_OUTPUT_FOLDER_NAME = "obj";
	private const string _ALL_FILES_SEARCH_PATTERN = "*";
	private const string _FOUND_MESSAGE = "{0}:{1}: found the forbidden text '{2}'";
	private const string _SELF_CHECK_SAMPLE_TEMPLATE = "See {0} and also {1} for the reasons.";
	private const string _CLEAN_SAMPLE = "This comment explains the reasoning and cites DR-12 only.";
	private const string _SYNTHETIC_FILE_NAME = "synthetic";
	private const char _PATH_SEPARATOR_FOR_DISPLAY = '/';
	private const char _LINE_SEPARATOR = '\n';

	/// <summary>
	/// The one file of this test project the public set leaves out on copy: it reads records that are not
	/// published, so the paths it names are never in the published source.
	/// </summary>
	private const string _UNPUBLISHED_TEST_FILE_NAME = "InternalRecordChecklistTests.cs";

	/// <summary>The extensions of the text files that ship with the source; anything else is a binary or generated file.</summary>
	private static readonly ImmutableHashSet<string> _textFileExtensions = ImmutableHashSet.Create(
		StringComparer.OrdinalIgnoreCase,
		".cs", ".csproj", ".props", ".targets", ".sln", ".json", ".xml", ".manifest", ".md", ".txt", ".resx", ".editorconfig", ".config", ".yml", ".yaml");

	/// <summary>Each forbidden identifier as pattern fragments, and one sample the pattern must match, also as fragments.</summary>
	private static readonly (string PatternHead, string PatternTail, string SampleHead, string SampleTail)[] _forbiddenIdentifiers =
	[
		// Work items, open conditions, product decisions, planning decisions and internal questions.
		("\\bREL", "-\\d+\\b", "REL", "-42"),
		("\\bCA", "-\\d+\\b", "CA", "-10"),
		("\\bD", "-R\\d+\\b", "D", "-R6"),
		("\\bD", "-P\\d+\\b", "D", "-P18"),
		("\\bQ", "-(REL|PM|PO|ARCH)", "Q", "-PM3"),
		// The internal handling markers and the requirements document's appendix.
		("\\bAppendix", " A\\b", "Appendix", " A"),
		("\\bDO NOT", " SHARE\\b", "DO NOT", " SHARE"),
		// Paths under the internal documentation folder, with either separator.
		("docs[/\\\\]", "v1\\b", "docs/", "v1/design/base.md"),
		// Derived requirements, alpha-release requirements and increment requirements.
		("\\bE", "R-\\d+", "E", "R-24"),
		("\\bR-M", "0-\\d+", "R-M", "0-54"),
		("\\bR-C", "S-\\d+", "R-C", "S-17"),
		// Acceptance criteria in both written forms.
		("\\bA", "C-\\d+", "A", "C-22.3"),
		("\\bA", "C#\\d+", "A", "C#7"),
		// Definition-document numbers.
		("\\bFR", "D-\\d+", "FR", "D-03"),
		// Functional-requirement numbers of the requirements document, written out and folded into a name.
		("\\bF", "R-4\\.\\d", "F", "R-4.1.7"),
		("F[Rr]", "4\\d{2,}", "TheRuleF", "r4115States"),
		// The requirements document itself, as a word and inside a constant name.
		("\\bP", "RD\\b", "P", "RD says so"),
		("_P", "RD_", "_P", "RD_MEMORY_LIMIT"),
		// Delivery-tracking tags: implementation sets, their short form, defect tags, mentions of the
		// tracked unit of work, and deliverable numbers.
		("\\bSe", "t \\d+[a-z]?\\b", "Se", "t 12a"),
		("\\bS", "\\d{1,2}\\b", "S", "11"),
		("\\bDEFECT", " D\\d+\\b", "DEFECT", " D6"),
		("\\b[Ww]ork", " unit\\b", "this work", " unit"),
		("\\bDeliverable", " \\d+\\b", "Deliverable", " 9"),
		// Coded calibration repositories, the half of the calibration kept back for a single reading, and
		// the repository the calibration was allowed to read.
		("\\bR-", "0\\d\\b", "R-", "05"),
		("\\b[Hh]eld", "-out\\b", "the held", "-out set"),
		("\\b[Aa]uthori", "[sz]ed repositor", "the authori", "zed repository"),
		// Requirements-document numbering written without the document's name: numbered sections, the
		// grading section and feature numbers.
		("\\b6", "[A-F]\\.\\d", "6", "D.2"),
		("\\b[Ss](?:ECTION|ection) \\d+", "\\.\\d+", "Section 3", ".1"),
		("(?:[Ss]ection |the |of |under )8", "\\.1\\b", "the 8", ".1 threshold"),
		("\\bFeature", " 4\\.\\d", "Feature", " 4.2"),
		// Internal review rounds and the items they numbered.
		("\\b[Rr](?:EVIEW|eview) [Rr](?:OUND|ound)", " \\d", "REVIEW ROUND", " 1"),
		("\\bROUND", " \\d\\b", "ROUND", " 2"),
		("\\bITEM", " \\d+\\b", "ITEM", " 12"),
		// The design catalog's numbered outputs and its document versions.
		("\\bOutput", " \\d\\b", "the base's Output", " 5"),
		("\\b[Cc]atalog", " 0\\.\\d", "Catalog", " 0.16"),
		// Tracked units of work, internal questions and requirement rows of the plan.
		("\\bWU", ":?\\d", "WU", "1"),
		("\\bQ", "\\d\\b", "the figure that answers Q", "5"),
		("\\bR", "\\d{2,3}\\b", "R", "150"),
		// Implementation sets and the internal records around them.
		("\\b[Tt]his", " set\\b", "the gap this", " set closes"),
		("[Bb]ase-", "hardening", "the base-", "hardening change"),
		("BASE-", "HARDENING", "THE BASE-", "HARDENING CHANGE"),
		("\\bF", "RD\\b", "the F", "RD's table"),
		("\\bA", "DR\\b", "the A", "DR says"),
		("\\b[Ee]vidence", " pack\\b", "the evidence", " pack"),
		("\\b[Mm]easurement", " note\\b", "the measurement", " note")
	];

	/// <summary>No published text file under <c>src/</c>, this test project or the generator names any internal identifier.</summary>
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
		string[] problems = [.. files.SelectMany(path => ProblemsInFile(root, path, ForbiddenPatterns()))];

		Assert.NotEmpty(files);
		Assert.True(problems.Length == 0, string.Join(Environment.NewLine, problems));
	}

	/// <summary>The check fires on every pattern it is built from, shown on samples assembled at run time, and passes clean text.</summary>
	[Fact]
	public void TheCheckReportsEveryPatternItIsBuiltFromAndPassesCleanText()
	{
		foreach ((string patternHead, string patternTail, string sampleHead, string sampleTail) in _forbiddenIdentifiers)
		{
			Regex pattern = new(patternHead + patternTail, RegexOptions.CultureInvariant);
			string sample = string.Format(CultureInfo.InvariantCulture, _SELF_CHECK_SAMPLE_TEMPLATE, sampleHead + sampleTail, _CLEAN_SAMPLE);

			Assert.NotEmpty(ProblemsInText(_SYNTHETIC_FILE_NAME, sample, [pattern]));
		}

		Assert.Empty(ProblemsInText(_SYNTHETIC_FILE_NAME, _CLEAN_SAMPLE, ForbiddenPatterns()));
	}

	/// <summary>
	/// What stays is not mistaken for a forbidden identifier: decision-record citations in both numbering
	/// forms, the plain-words qualifier that replaced a definition-document number, a pattern class
	/// identifier, the report's own section numbers, and numeric format strings.
	/// </summary>
	[Fact]
	public void TheCitationsAndVocabularyThatStayAreNotForbidden()
	{
		string sample = string.Concat(
			"DR", "-12 and DR", "-07 and alpha design DR", "-40, pattern class CS", "-011, section 4 of the report, ",
			"a folder formatted with {0:D", "6} and a file formatted with {0:D", "5}");

		Assert.Empty(ProblemsInText(_SYNTHETIC_FILE_NAME, sample, ForbiddenPatterns()));
	}

	private static ImmutableArray<Regex> ForbiddenPatterns()
	{
		return [.. _forbiddenIdentifiers.Select(identifier => new Regex(identifier.PatternHead + identifier.PatternTail, RegexOptions.CultureInvariant))];
	}

	private static IEnumerable<string> SourceFilesUnder(string folder)
	{
		return Directory.EnumerateFiles(folder, _ALL_FILES_SEARCH_PATTERN, SearchOption.AllDirectories)
			.Where(path => !IsBuildOutput(folder, path)
				&& _textFileExtensions.Contains(Path.GetExtension(path))
				&& !Path.GetFileName(path).Equals(_UNPUBLISHED_TEST_FILE_NAME, StringComparison.Ordinal))
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
}
