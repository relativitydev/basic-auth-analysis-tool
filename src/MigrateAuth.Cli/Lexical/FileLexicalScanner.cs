using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Emission;
using MigrateAuth.Cli.Reading;

namespace MigrateAuth.Cli.Lexical;

/// <summary>
/// THE SCANNER, WIRED TO THE ONE DOOR EVERYTHING LEAVES THIS PROCESS BY.
///
/// <see cref="LexicalScanner"/> is pure and reports nothing; this is where an abandoned scan
/// becomes <see cref="SkippedItemCategory.LexicalAnomaly"/>. It goes through
/// <see cref="EmissionPoint.Emit"/> exactly as the walk's own skips do, so there is no second
/// reporting path for a hole in the coverage - the same reason <see cref="RepositoryFileReader"/>
/// emits rather than returns its skips.
///
/// A SCAN ABANDONED BY THE LEXICAL BUDGET (<see cref="LexicalAnomalyReason.LexicalBudgetExceeded"/>)
/// IS REPORTED THE SAME WAY: the mask is sound up to the trip, the findings established there are
/// kept, and the file is counted once under the same category.
///
/// IT REPORTS THE ABANDONED SCAN AND NOT THE RECOVERED ONE. The category means "a scan
/// abandoned mid-file, leaving it partially covered". A literal ended by a raw newline its rule
/// does not admit is an anomaly and is counted on the mask, but the file is covered
/// end to end and reporting it as a skipped item would put a fully analyzed file into the count a
/// reader uses to size the holes.
///
/// ONE SCANNER PER PROFILE, BUILT ONCE. The rule index behind a scanner is derived work and is
/// immutable, so rebuilding it per file would pay the profile's whole construction cost against
/// every one of a large repository's files for no gain.
/// </summary>
public sealed class FileLexicalScanner
{
	private readonly EmissionPoint _emissionPoint;
	private readonly TimeProvider _timeProvider;
	private readonly Dictionary<LexicalProfile, LexicalScanner> _scannersByProfile;

	public FileLexicalScanner(EmissionPoint emissionPoint)
		: this(emissionPoint, TimeProvider.System)
	{
	}

	/// <summary>
	/// The clock every scanner built here reads, so that the lexical budget is testable
	/// without waiting. The budget is one per file per stage: this is the Analyze stage's, and it
	/// charges the lexical scan only.
	/// </summary>
	public FileLexicalScanner(EmissionPoint emissionPoint, TimeProvider timeProvider)
	{
		ArgumentNullException.ThrowIfNull(emissionPoint);
		ArgumentNullException.ThrowIfNull(timeProvider);

		_emissionPoint = emissionPoint;
		_timeProvider = timeProvider;
		_scannersByProfile = [];
	}

	/// <summary>
	/// Builds one file's zone mask and reports the file as a lexical anomaly where the scan was
	/// abandoned before the end of it. The mask is returned either way: the part of it below
	/// <see cref="ZoneMask.ValidThroughPosition"/> is sound and the findings established there are
	/// kept rather than thrown away with the rest.
	/// </summary>
	public ZoneMask Scan(LexicalProfile profile, string normalizedText, RepositoryRelativePath path)
	{
		ArgumentNullException.ThrowIfNull(profile);
		ArgumentNullException.ThrowIfNull(normalizedText);

		ZoneMask mask = ScannerFor(profile).Scan(normalizedText);

		if (mask.WasAbandonedMidFile)
		{
			_emissionPoint.Emit(new EmittedFact
			{
				Category = EmissionCategory.SkippedItem,
				SkippedItem = SkippedItemCategory.LexicalAnomaly,
				Path = path
			});
		}

		return mask;
	}

	private LexicalScanner ScannerFor(LexicalProfile profile)
	{
		if (!_scannersByProfile.TryGetValue(profile, out LexicalScanner? scanner))
		{
			scanner = new LexicalScanner(profile, _timeProvider);
			_scannersByProfile[profile] = scanner;
		}

		return scanner;
	}
}
