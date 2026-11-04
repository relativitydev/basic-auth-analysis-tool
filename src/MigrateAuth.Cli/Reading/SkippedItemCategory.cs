using System.Collections.Frozen;

namespace MigrateAuth.Cli.Reading;

/// <summary>
/// THE CLOSED ENUMERATION OF SKIPPED-ITEM CATEGORIES, OWNED HERE AND NOWHERE ELSE.
///
/// Every skipped item carries exactly one of these, and a component may not invent one locally.
///
/// It is NOT <see cref="Emission.EmissionCategory"/>, which says what KIND of fact is being
/// emitted. This one says WHY an item was skipped, and it reaches output as a further field of
/// <see cref="Emission.EmittedFact"/> rather than as a widening of that list.
///
/// FOUR OF THE ELEVEN ARE DECLARED HERE AND RAISED ELSEWHERE.
/// <see cref="LexicalAnomaly"/> belongs to the lexical scanner and is raised in
/// <c>Lexical/FileLexicalScanner.cs</c>; <see cref="MissingCandidate"/> belongs to the
/// candidate staleness check and is raised in <c>Pipeline/AnalyzeStage.cs</c>;
/// <see cref="MatchBoundExceeded"/> and <see cref="MalformedMatch"/> belong to the match bound
/// and are raised in <c>Recognition/RecognitionEngine.cs</c>. A stage that raises one finds the
/// category already declared here.
/// </summary>
public enum SkippedItemCategory
{
	/// <summary>
	/// No category. The value of <c>default</c>, which no emission accepts. It is not a twelfth
	/// category - it is the absence of one, and the emission point refuses it.
	/// </summary>
	None = 0,

	/// <summary>
	/// A file that could not be opened for reading. The hole is KNOWN: exactly one file,
	/// named by its repository-relative path.
	/// </summary>
	UnreadableFile = 1,

	/// <summary>
	/// A directory that could not be enumerated. The hole is UNKNOWN: its contents were
	/// never enumerable, so this counts ONE DIRECTORY and never the files it may hold.
	/// </summary>
	UnlistableDirectory = 2,

	/// <summary>A directory link, junction or reparse point, which the walk never follows.</summary>
	LinkNotFollowed = 3,

	/// <summary>Failed strict UTF-8 and failed the markless two-byte retry.</summary>
	Undecodable = 4,

	/// <summary>Above the file-size ceiling, so it was never read.</summary>
	OversizeFile = 5,

	/// <summary>A line above the line-length ceiling. The whole file is skipped, not the line.</summary>
	OverLongLine = 6,

	/// <summary>The platform refused the path for its length.</summary>
	PathTooLong = 7,

	/// <summary>
	/// A scan abandoned mid-file, leaving it partially covered. Declared here, raised by
	/// <c>Lexical/FileLexicalScanner.cs</c> when the zone mask it builds reports a scan abandoned
	/// mid-file; the CSharp analyzer's lexical profile is the one that can trip it in this build.
	/// </summary>
	LexicalAnomaly = 8,

	/// <summary>
	/// A path recorded in Scan that no longer exists when Analysis opens it. Declared
	/// here, raised by <c>Pipeline/AnalyzeStage.cs</c> when a candidate recorded during Scan can
	/// no longer be opened. It is a skipped item and never an error, because the tool does not
	/// watch the repository for change.
	/// </summary>
	MissingCandidate = 9,

	/// <summary>
	/// The match bound - the declared exception to determinism - stopped matching on
	/// this file before every rule had run. Declared here, raised by
	/// <c>Recognition/RecognitionEngine.cs</c> once a language's catalog carries detection rules
	/// for a file to time out against - the CSharp analyzer's catalog is the first to do so in
	/// this build. Counts the whole file rather than the one rule that timed out: which rules had
	/// already run before the bound tripped is itself machine-speed-dependent, so the finding SET
	/// for the file is already compromised the moment any rule on it times out, and the exception is stated
	/// at the level of the file for exactly that reason.
	/// </summary>
	MatchBoundExceeded = 10,

	/// <summary>
	/// A rule's
	/// <c>[GeneratedRegex]</c> expression returned a match that either failed to advance past the
	/// one before it (the rule resumes past the stuck position rather than stopping outright), or,
	/// at any length, placed its own participating anchor group outside the match span AND was
	/// rejected by the interpreted cross-check, which re-runs the attempt on the interpreted engine
	/// and finds a different match (that one match alone is dropped) - a confirmed source-generator
	/// defect (SDK 10.0.201 / runtime 10.0.5) that <see cref="Recognition.RecognitionEngine"/> drops
	/// rather than trusts. An anchor group legitimately outside its own match's span - a lookahead
	/// or lookbehind anchor, which the analyzer contract does not forbid, including a zero-length one - is confirmed
	/// by the interpreted engine and classified normally; it is NOT this category. Declared here, raised beside
	/// <see cref="MatchBoundExceeded"/> in <c>Recognition/RecognitionEngine.cs</c>. Counts the whole
	/// FILE, once, no matter how many malformed matches, resumptions or rules produce one: the
	/// category exists to say a hole is there, not to size it twice over.
	///
	/// UNLIKE <see cref="MatchBoundExceeded"/>, A FILE COUNTED HERE IS NOT NECESSARILY EMPTY OF
	/// FINDINGS. This category drops one match or resumes past one stuck position; every other
	/// match, from this rule and every other rule, is kept and can still produce findings for the
	/// same file. The defect is also deterministic - the same expression against the same text on
	/// the same runtime reproduces identically every run - so keeping the file's other findings does
	/// not cost determinism the way keeping a match-bound remainder would.
	///
	/// THE CATEGORIES ARE NOT GUARANTEED DISJOINT PER FILE. A file can in principle be counted here
	/// AND under <see cref="MatchBoundExceeded"/> in the same run - this category on one rule, the
	/// match bound on a different rule that genuinely times out and discards the whole recognized
	/// set - since neither guards against the other. A reader summing skipped-item tallies across
	/// categories is therefore not counting distinct files.
	/// </summary>
	MalformedMatch = 11
}

/// <summary>
/// What a category counts. It exists as a type because the mistake it guards against is an
/// ARITHMETIC one: adding an unlistable directory into a file count reports a hole
/// of unknown size as a hole of size one, and "3 items skipped" then reads as a near-complete
/// scan over a subtree of four thousand unreachable files.
///
/// Carrying the unit on every tally is what makes the sum refuse to typecheck in a reader's head
/// before it refuses anywhere else.
/// </summary>
public enum SkippedItemUnit
{
	/// <summary>The tally counts files. Ten of the eleven categories do.</summary>
	Files = 0,

	/// <summary>The tally counts directories whose contents are unknown. Only one category does.</summary>
	Directories = 1
}

/// <summary>The closed list as data: its members, in one order, and what each of them counts.</summary>
public static class SkippedItemCategories
{
	/// <summary>
	/// The eleven, in declaration order, which is the order anything enumerating them uses. It is
	/// fixed here rather than derived from the enum at runtime, because the output needs an order that
	/// does not depend on reflection ordering and because an AOT binary should not need it.
	/// <see cref="SkippedItemCategory.MatchBoundExceeded"/> sits beside
	/// <see cref="SkippedItemCategory.LexicalAnomaly"/> on one side, the other category recognition
	/// itself raises rather than reading or walking, and beside
	/// <see cref="SkippedItemCategory.MalformedMatch"/> on the other, raised by the same engine for
	/// the same reason: a rule whose own match cannot be trusted.
	/// </summary>
	public static readonly SkippedItemCategory[] ALL =
	[
		SkippedItemCategory.UnreadableFile,
		SkippedItemCategory.UnlistableDirectory,
		SkippedItemCategory.LinkNotFollowed,
		SkippedItemCategory.Undecodable,
		SkippedItemCategory.OversizeFile,
		SkippedItemCategory.OverLongLine,
		SkippedItemCategory.PathTooLong,
		SkippedItemCategory.LexicalAnomaly,
		SkippedItemCategory.MatchBoundExceeded,
		SkippedItemCategory.MalformedMatch,
		SkippedItemCategory.MissingCandidate
	];

	/// <summary>
	/// What the category counts. Only <see cref="SkippedItemCategory.UnlistableDirectory"/>
	/// counts directories; every other category counts files.
	/// </summary>
	public static SkippedItemUnit UnitOf(SkippedItemCategory category)
	{
		return category is SkippedItemCategory.UnlistableDirectory
			? SkippedItemUnit.Directories
			: SkippedItemUnit.Files;
	}
}

/// <summary>
/// THE CLOSED LIST'S STABLE, DECLARED NAMES, FOR THE ONE PLACE A CATEGORY NOW CROSSES A
/// TRUST BOUNDARY: THE SESSION STATE FILE.
///
/// <see cref="Session.SessionStateStore"/> persists a skipped-item tally per stage and reads it back
/// as hostile input, on the same terms already applied to a completed stage's name and to a
/// finding's language. Neither an enum's numeric value nor its member name (<c>ToString</c>) is
/// fit for that: the numeric value is reflection-free but meaningless to a human reading the file by
/// hand, and the member name is exactly the reflective path that Native AOT forbids - a
/// trimmed build is not guaranteed to keep it. So the name is declared here as data, the same
/// discipline <see cref="Catalog.AssessedLanguages"/> already applies to the eight languages.
/// </summary>
public static class SkippedItemCategoryNames
{
	private const string _UNREADABLE_FILE_NAME = "unreadableFile";
	private const string _UNLISTABLE_DIRECTORY_NAME = "unlistableDirectory";
	private const string _LINK_NOT_FOLLOWED_NAME = "linkNotFollowed";
	private const string _UNDECODABLE_NAME = "undecodable";
	private const string _OVERSIZE_FILE_NAME = "oversizeFile";
	private const string _OVER_LONG_LINE_NAME = "overLongLine";
	private const string _PATH_TOO_LONG_NAME = "pathTooLong";
	private const string _LEXICAL_ANOMALY_NAME = "lexicalAnomaly";
	private const string _MATCH_BOUND_EXCEEDED_NAME = "matchBoundExceeded";
	private const string _MALFORMED_MATCH_NAME = "malformedMatch";
	private const string _MISSING_CANDIDATE_NAME = "missingCandidate";

	private static readonly FrozenDictionary<SkippedItemCategory, string> _namesByCategory =
		BuildValidatedNames();

	private static readonly FrozenDictionary<string, SkippedItemCategory> _categoriesByName =
		_namesByCategory.ToFrozenDictionary(entry => entry.Value, entry => entry.Key, StringComparer.Ordinal);

	/// <summary>The declared name a tally is written under. Every one of the eleven has one.</summary>
	public static string NameOf(SkippedItemCategory category)
	{
		if (!_namesByCategory.TryGetValue(category, out string? name))
		{
			throw new ArgumentOutOfRangeException(nameof(category), category, "No declared name for this category.");
		}

		return name;
	}

	/// <summary>
	/// Reads a name back. Ordinal and exact, because this is the tool's own state file rather than a
	/// client-edited configuration - there is no case a legitimate document could vary on.
	/// </summary>
	public static bool TryParse(string? name, out SkippedItemCategory category)
	{
		return name is not null && _categoriesByName.TryGetValue(name, out category) ? true : Fail(out category);
	}

	private static bool Fail(out SkippedItemCategory category)
	{
		category = default;

		return false;
	}

	private static FrozenDictionary<SkippedItemCategory, string> BuildValidatedNames()
	{
		Dictionary<SkippedItemCategory, string> names = new()
		{
			[SkippedItemCategory.UnreadableFile] = _UNREADABLE_FILE_NAME,
			[SkippedItemCategory.UnlistableDirectory] = _UNLISTABLE_DIRECTORY_NAME,
			[SkippedItemCategory.LinkNotFollowed] = _LINK_NOT_FOLLOWED_NAME,
			[SkippedItemCategory.Undecodable] = _UNDECODABLE_NAME,
			[SkippedItemCategory.OversizeFile] = _OVERSIZE_FILE_NAME,
			[SkippedItemCategory.OverLongLine] = _OVER_LONG_LINE_NAME,
			[SkippedItemCategory.PathTooLong] = _PATH_TOO_LONG_NAME,
			[SkippedItemCategory.LexicalAnomaly] = _LEXICAL_ANOMALY_NAME,
			[SkippedItemCategory.MatchBoundExceeded] = _MATCH_BOUND_EXCEEDED_NAME,
			[SkippedItemCategory.MalformedMatch] = _MALFORMED_MATCH_NAME,
			[SkippedItemCategory.MissingCandidate] = _MISSING_CANDIDATE_NAME
		};

		foreach (SkippedItemCategory category in SkippedItemCategories.ALL)
		{
			if (!names.ContainsKey(category))
			{
				throw new InvalidOperationException($"Category {category} carries no declared name.");
			}
		}

		return names.ToFrozenDictionary();
	}
}
