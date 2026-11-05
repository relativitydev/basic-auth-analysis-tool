using System.Collections.Immutable;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Emission;
using MigrateAuth.Cli.Findings;
using MigrateAuth.Cli.Recognition;
using MigrateAuth.Cli.Session;

using MigrateAuth.Cli.Tests.Fixtures;

namespace MigrateAuth.Cli.Tests.Findings;

/// <summary>
/// A SYNTHETIC CONSTRUCT SET, A REAL ANALYZER, AND A PATH THAT WENT THROUGH THE WRITE BOUNDARY.
///
/// The constructs are hand-built rather than recognized, and that is the right fixture for THIS
/// set rather than a shortcut. What is under test is ordering, identity and numbering over a set
/// of constructs - properties of the set's shape, not of how any construct came to be in it - and
/// a fixture built by running recognition could only ever produce the construct arrangements the
/// reference profile's expressions happen to produce. A thousand findings across four files with
/// a deliberate Unicode collision is not one of them.
///
/// The ANALYZER is real, and is reached through the registry rather than by naming its type - the
/// same route the golden test takes, because the reference analyzer class is file-local. It is
/// here for one reason: destination certainty is read off its declared rules, and a fake analyzer
/// would be a fixture asserting what the test already assumed.
///
/// The PATH goes through <see cref="RepositoryRelativePath.TryFromScannedPath"/> because that is
/// the only way to obtain one. It names files that do not exist, which is deliberate and is fine:
/// containment is decided from the text of the path, and nothing in these tests opens anything.
/// </summary>
internal sealed class FindingHarness
{
	private const string _REFERENCE_LANGUAGE_NAME = "ExemplarOne";
	private const string _REPOSITORY_ROOT = "finding-harness-repository";

	/// <summary>Any character that is not a line feed, since only line feeds affect a position.</summary>
	private const char _FILLER_CHARACTER = 'x';

	/// <summary>Destination certainty Established, credential position declared.</summary>
	internal const string CREDENTIAL_TYPE_PATTERN_CLASS = "EXM-001";

	/// <summary>Destination certainty Unknown, and no credential position at all.</summary>
	internal const string HAND_ASSEMBLED_HEADER_PATTERN_CLASS = "EXM-002";

	/// <summary>Destination certainty Established.</summary>
	internal const string LEGACY_CLIENT_PATTERN_CLASS = "EXM-003";

	private readonly RepositoryFolderPath _repository;

	internal FindingHarness()
	{
		_repository = RepositoryFolderPath.FromSuppliedText(Path.Combine(Path.GetTempPath(), _REPOSITORY_ROOT));
		Analyzer = ExemplarFixtureCatalog.Build().AnalyzersByLanguageName[_REFERENCE_LANGUAGE_NAME];
	}

	internal ILanguageAnalyzer Analyzer { get; }

	/// <summary>One construct, placed where a test wants it and classified how a test wants it.</summary>
	internal static RecognizedConstruct ConstructAt(
		string patternClassIdentifier,
		int anchorOffset,
		ConfidenceLevel level = ConfidenceLevel.High,
		bool literalCredentialSignal = false)
	{
		RecognizedConstruct construct = new()
		{
			PatternClassIdentifier = patternClassIdentifier,
			AnchorOffset = anchorOffset,
			Level = level,
			LiteralCredentialSignal = literalCredentialSignal
		};

		return construct;
	}

	/// <summary>
	/// One file, with its constructs in the order recognition would have delivered them - by anchor
	/// offset, then by pattern class identifier. The harness sorts them so that a test naming its
	/// constructs in whatever order reads best cannot accidentally be testing what happens when a
	/// caller breaks that contract.
	/// </summary>
	internal RecognizedFile FileOf(
		string relativePath,
		string normalizedText,
		params RecognizedConstruct[] constructs)
	{
		RecognizedFile file = new()
		{
			Path = PathOf(relativePath),
			Language = AssessedLanguage.CSharp,
			NormalizedText = normalizedText,
			Analyzer = Analyzer,
			Constructs =
			[
				.. constructs
					.OrderBy(construct => construct.AnchorOffset)
					.ThenBy(construct => construct.PatternClassIdentifier, StringComparer.Ordinal)
			]
		};

		return file;
	}

	internal RepositoryRelativePath PathOf(string relativePath)
	{
		if (!RepositoryRelativePath.TryFromScannedPath(_repository, relativePath, out RepositoryRelativePath path))
		{
			throw new InvalidOperationException($"The harness could not build a relative path from '{relativePath}'.");
		}

		return path;
	}

	/// <summary>
	/// Text long enough to place a construct anywhere a test asks for, on lines of equal length so
	/// that an offset's expected line and column stay arithmetic rather than counted by hand. Tests
	/// asserting on a SPECIFIC line and column build their own text instead.
	/// </summary>
	internal static string FillerText(int lineCount, int lineLength)
	{
		string line = new(_FILLER_CHARACTER, lineLength);

		return string.Join('\n', Enumerable.Repeat(line, lineCount));
	}

	/// <summary>
	/// Every finding's identifier, in the order composition returned them.
	///
	/// IT RETURNS AN ARRAY AND NOT AN <see cref="ImmutableArray{T}"/> ON PURPOSE. That type is a
	/// struct implementing <see cref="IEquatable{T}"/> over the identity of its backing array, so
	/// an equality assertion on two of them compares REFERENCES and fails on two sequences that are
	/// element for element the same - reporting "collections differ" while printing two identical
	/// collections. Every helper here hands back something compared by content.
	/// </summary>
	internal static string[] IdentifiersOf(ImmutableArray<Finding> findings)
	{
		return [.. findings.Select(finding => finding.Identifier)];
	}

	/// <summary>Every finding's fingerprint in its string form, in the order composition returned them.</summary>
	internal static string[] FingerprintsOf(ImmutableArray<Finding> findings)
	{
		return [.. findings.Select(finding => finding.Fingerprint.Text)];
	}
}
