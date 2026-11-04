using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Configuration;
using MigrateAuth.Cli.Emission;
using MigrateAuth.Cli.Progress;
using MigrateAuth.Cli.Reading;
using MigrateAuth.Cli.Session;

namespace MigrateAuth.Cli.Scanning;

/// <summary>
/// What the enumeration pass established: every file the walk reached, and whether it finished.
/// </summary>
public sealed record EnumerationResult
{
	/// <summary>
	/// The bounded path list both passes run over. It is exposed as a read-only list over the
	/// list that was built rather than copied into an immutable one, because at the worst
	/// case of half a million files the copy is a second list of the same size held at the same
	/// moment - a peak this stage has no reason to pay for a guarantee no caller needs.
	/// </summary>
	public required IReadOnlyList<RepositoryRelativePath> FilePaths { get; init; }

	/// <summary>Whether the user interrupted the pass. A cancelled enumeration is not a scan.</summary>
	public required bool WasCancelled { get; init; }

	/// <summary>The folders the walk met and did not enter: a count and a bounded list, apart from the skipped items.</summary>
	public ExcludedFolderSummary ExcludedFolders { get; init; } = ExcludedFolderSummary.None;
}

/// <summary>What the inventory pass established.</summary>
public sealed record InventoryResult
{
	/// <summary>the per-language aggregate.</summary>
	public required LanguageInventory Inventory { get; init; }

	/// <summary>
	/// the candidate set, as repository-relative text ready for persistence. It
	/// is the one term that grows with the repository and it is deliberately not capped:
	/// capping it means silently declining to analyze files, which is worse than the memory it
	/// saves. Paths only, never content.
	///
	/// ORDERED ORDINALLY, which the walk itself is not and could not be - it holds a directory
	/// stack rather than the tree, so its order is the platform's. The list is already held here,
	/// so ordering it costs nothing structural and buys two things determinism asks for: the state file
	/// two identical runs write is identical, and the order in which a later stage analyzes
	/// candidates - which is what decides WHICH findings a findings cap drops - is a property of
	/// the repository rather than of the filesystem that enumerated it.
	/// </summary>
	public required IReadOnlyList<string> CandidateRelativePaths { get; init; }

	/// <summary>
	/// The language name this pass's first-line attribution gave each candidate, parallel to
	/// <see cref="CandidateRelativePaths"/> and in the same order. It is persisted with the candidate
	/// set and reused by Analysis, which attributes nothing itself.
	/// </summary>
	public required IReadOnlyList<string> CandidateLanguageNames { get; init; }

	/// <summary>Whether the user interrupted the pass.</summary>
	public required bool WasCancelled { get; init; }

	/// <summary>The files this pass read as a legacy code page: a count and a bounded list.</summary>
	public LegacyEncodedFileSummary LegacyEncodedFiles { get; init; } = LegacyEncodedFileSummary.None;
}

/// <summary>
/// TWO PASSES OVER ONE WALK.
///
/// THE TREE IS WALKED ONCE. The advance warning must fire past 50,000 files BEFORE scanning
/// begins, which needs the total before the work - and the total is only knowable by enumerating.
/// Walking twice would double the traversal and, worse, would let the two passes disagree about
/// which files exist in a repository being edited during the scan. So the walk runs once into a
/// bounded path list and both passes run over that list.
///
/// WHAT THE LIST HOLDS IS THE SMALLEST THING THAT WORKS: one <see cref="RepositoryRelativePath"/>
/// per file, which is one string reference and nothing else. The walk's own
/// <see cref="WalkedFile"/> carries the absolute path as well, and holding half a million of
/// those would roughly double a term that is already the largest thing this stage allocates. The
/// absolute path is rebuilt per file in the second pass by combining the canonical root with the
/// relative path - a string operation against a value the walk itself derived from that same root.
/// The typed relative path is CARRIED rather than re-derived, so the containment guarantee the
/// walk established is not re-litigated and cannot come out differently the second time.
///
/// AGGREGATION ONLY. No per-file record survives a loop iteration in the second pass: the
/// inventory takes three numbers, the candidate list takes a path, and the decoded text is
/// released with the iteration that decoded it.
/// </summary>
public sealed class RepositoryInventoryScan
{
	/// <summary>What ends a line in normalized text, which is the only kind this class sees.</summary>
	private const char _LINE_FEED = '\n';

	/// <summary>The separator an emittable relative path carries, on every platform.</summary>
	private const char _PORTABLE_DIRECTORY_SEPARATOR = '/';

	private readonly RepositoryFolderPath _repository;
	private readonly RepositoryWalk _walk;
	private readonly RepositoryFileReader _reader;
	private readonly LanguageAttribution _attribution;
	private readonly IReadOnlySet<AssessedLanguage> _enabledLanguages;

	/// <summary>
	/// <paramref name="enabledLanguages"/> narrows candidate selection - PASS 2's own
	/// <see cref="ReadInventory"/> - without narrowing attribution. A file in a disabled language is
	/// still counted by name in the inventory; it is only ever excluded from
	/// <see cref="InventoryResult.CandidateRelativePaths"/>, which is the one output Analysis reads.
	/// </summary>
	public RepositoryInventoryScan(
		RepositoryFolderPath repository,
		LanguageCatalog catalog,
		EmissionPoint emissionPoint,
		IReadOnlySet<AssessedLanguage> enabledLanguages)
		: this(repository, catalog, emissionPoint, enabledLanguages, TimeProvider.System)
	{
	}

	/// <summary>
	/// <paramref name="timeProvider"/> is the clock the first-line attribution's lexical budget
	/// reads, so that a trip can be forced in a test without waiting.
	/// </summary>
	public RepositoryInventoryScan(
		RepositoryFolderPath repository,
		LanguageCatalog catalog,
		EmissionPoint emissionPoint,
		IReadOnlySet<AssessedLanguage> enabledLanguages,
		TimeProvider timeProvider)
	{
		ArgumentNullException.ThrowIfNull(catalog);
		ArgumentNullException.ThrowIfNull(emissionPoint);
		ArgumentNullException.ThrowIfNull(enabledLanguages);
		ArgumentNullException.ThrowIfNull(timeProvider);

		_repository = repository;
		_walk = new RepositoryWalk(repository, emissionPoint);
		_reader = new RepositoryFileReader(emissionPoint);
		_attribution = new LanguageAttribution(catalog, timeProvider);
		_enabledLanguages = enabledLanguages;
	}

	/// <summary>
	/// PASS 1. Produces the total file count, and OPENS NOTHING. Every category the reader can
	/// raise - unreadable, undecodable, oversize, over-long line - is therefore impossible during
	/// this pass, which is how "the warning fired before a single file was opened" is demonstrable
	/// rather than asserted.
	///
	/// It does not produce the per-language distribution: that needs lines of code and the first-line
	/// detection, neither of which exists without opening the file, so the inventory pass produces it.
	/// The number the warning needs is the total, and the total is what this pass produces.
	/// </summary>
	public EnumerationResult Enumerate(StageProgressReporter progress, CancellationToken cancellationToken)
	{
		ArgumentNullException.ThrowIfNull(progress);

		List<RepositoryRelativePath> filePaths = [];
		bool wasCancelled = false;

		foreach (WalkedFile file in _walk.Enumerate())
		{
			if (cancellationToken.IsCancellationRequested)
			{
				wasCancelled = true;
				break;
			}

			if (!IsRepositoryConfigurationFile(file.Path))
			{
				filePaths.Add(file.Path);
				progress.ItemCompleted();
			}
		}

		return new EnumerationResult
		{
			FilePaths = filePaths,
			WasCancelled = wasCancelled,
			ExcludedFolders = _walk.ExcludedFolders()
		};
	}

	/// <summary>
	/// The tool's own configuration file, at the repository root and only there. It is an input of
	/// the assessment rather than part of what is assessed, so it is neither counted in the inventory
	/// as a file of no recognized language nor offered as a candidate. A file of that name in a
	/// subfolder is an ordinary file: the tool never reads it as configuration.
	/// </summary>
	private static bool IsRepositoryConfigurationFile(RepositoryRelativePath path)
	{
		return string.Equals(path.Value, ConfigurationResolver.CONFIGURATION_FILE_NAME, StringComparison.Ordinal);
	}

	/// <summary>
	/// PASS 2. Opens each file once and, in the same pass, decodes it, counts its lines and
	/// evaluates candidate preselection - all three over the SAME decoded text, which is what
	/// stops the inventory's line count and the analysis's line numbers deriving from different
	/// rules.
	///
	/// The two per-file ceilings are consumed rather than re-implemented: an oversize file and a file
	/// with an over-long line come back from the reader as skip categories, already emitted and
	/// already counted by the machinery that owns them.
	/// </summary>
	public InventoryResult ReadInventory(
		IReadOnlyList<RepositoryRelativePath> filePaths,
		StageProgressReporter progress,
		CancellationToken cancellationToken)
	{
		ArgumentNullException.ThrowIfNull(filePaths);
		ArgumentNullException.ThrowIfNull(progress);

		LanguageInventory inventory = new();
		LegacyEncodedFileTally legacyEncodedFiles = new();
		List<ScannedCandidate> candidates = [];
		bool wasCancelled = false;

		foreach (RepositoryRelativePath path in filePaths)
		{
			if (cancellationToken.IsCancellationRequested)
			{
				wasCancelled = true;
				break;
			}

			InventoryOneFile(path, inventory, candidates, legacyEncodedFiles);
			progress.ItemCompleted();
		}

		candidates.Sort(CompareByPathOrdinal);

		return new InventoryResult
		{
			Inventory = inventory,
			CandidateRelativePaths = [.. candidates.Select(candidate => candidate.RelativePath)],
			CandidateLanguageNames = [.. candidates.Select(candidate => candidate.LanguageName)],
			WasCancelled = wasCancelled,
			LegacyEncodedFiles = legacyEncodedFiles.ToSummary()
		};
	}

	/// <summary>A candidate and the language its first-line attribution gave it, kept together until the set is ordered.</summary>
	private readonly record struct ScannedCandidate(string RelativePath, string LanguageName);

	private static int CompareByPathOrdinal(ScannedCandidate left, ScannedCandidate right)
	{
		return string.CompareOrdinal(left.RelativePath, right.RelativePath);
	}

	/// <summary>
	/// The first line of the decoded text, which is what the detection rule is applied
	/// to. It is bounded by the line-length ceiling the reader already enforced, so a file with no
	/// line ending at all cannot produce an unbounded first line here.
	/// </summary>
	private static string FirstLineOf(string normalizedText)
	{
		int lineFeedPosition = normalizedText.IndexOf(_LINE_FEED);

		return lineFeedPosition < 0 ? normalizedText : normalizedText[..lineFeedPosition];
	}

	private void InventoryOneFile(
		RepositoryRelativePath path,
		LanguageInventory inventory,
		List<ScannedCandidate> candidates,
		LegacyEncodedFileTally legacyEncodedFiles)
	{
		FileReadResult read = _reader.Read(new WalkedFile(path, RebuildAbsolutePath(path)));

		if (read.Encoding == DetectedEncoding.Windows1252)
		{
			legacyEncodedFiles.Record(path);
		}

		string? firstLine = read.WasRead ? FirstLineOf(read.NormalizedText) : null;
		AttributedLanguage attribution = _attribution.Attribute(path, firstLine);

		bool isCandidate =
			read.WasRead
			&& attribution.Analyzer is not null
			&& CandidatePreselection.IsCandidate(attribution.Analyzer, read.NormalizedText)
			&& IsLanguageEnabled(attribution.LanguageName);

		inventory.RecordFile(attribution.LanguageName, read.Measurement.LineCount, isCandidate);

		if (attribution.FirstLineBudgetExceeded)
		{
			inventory.RecordFirstLineFallback();
		}

		if (isCandidate)
		{
			candidates.Add(new ScannedCandidate(path.Value, attribution.LanguageName));
		}
	}

	/// <summary>
	/// The configuration's candidate-selection gate. A name outside the closed set of eight - the one route left
	/// after <see cref="LanguageAttribution.Attribute"/> already required a non-null analyzer above,
	/// which the reference profile used in tests still exercises with an invented name - carries no
	/// enablement of its own and is never blocked by it: the effective configuration only ever
	/// narrows the eight assessed languages, never a name outside them.
	/// </summary>
	private bool IsLanguageEnabled(string languageName)
	{
		return !AssessedLanguages.TryParse(languageName, out AssessedLanguage language)
			|| _enabledLanguages.Contains(language);
	}

	/// <summary>
	/// The one thing the list does not carry. The relative path's separators are the portable form
	/// the emitted value uses, so they are put back to the platform's before the combine - on a
	/// platform where the two are the same character this changes nothing, and on the one where
	/// they differ it is the difference between a path that opens and one that does not.
	/// </summary>
	private string RebuildAbsolutePath(RepositoryRelativePath path)
	{
		return Path.Combine(
			_repository.CanonicalPath,
			path.Value.Replace(_PORTABLE_DIRECTORY_SEPARATOR, Path.DirectorySeparatorChar));
	}
}
