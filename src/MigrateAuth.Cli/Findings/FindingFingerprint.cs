using System.Globalization;

using MigrateAuth.Cli.Emission;

namespace MigrateAuth.Cli.Findings;

/// <summary>
/// THE CONTENT-FREE IDENTITY FINGERPRINT. THREE PARTS: the normalized relative path, the
/// pattern class, and the ordinal of that class's occurrence within the file. No digest function.
/// No material derived from the file's text.
///
/// WHAT IT IS NOT IS THE POINT OF IT. A digest of the finding's syntactic neighbourhood would be
/// a CREDENTIAL VERIFICATION ORACLE: for a High-confidence finding the construct's argument IS the
/// credential, so any neighbourhood centred on the match includes it; determinism forbids a salt;
/// the code is open source; and the report already publishes path and pattern class, which are
/// two of the three terms. The only unknown left would be the secret, over a known template.
///
/// So the disclosure risk is gone BY CONSTRUCTION rather than by promise: nothing here
/// fingerprints, hashes or otherwise tracks the repository's source content across sessions.
///
/// WHAT THE ORDINAL BUYS AND WHAT IT COSTS. It resolves collision between two identical
/// occurrences of one class in one file, which a path and a class alone cannot. And because it
/// counts OCCURRENCES rather than positions, the fingerprint survives inserting or deleting lines
/// anywhere in the file.
///
/// What it does not survive is a change to the population it counts. Insert a new occurrence of
/// the SAME CLASS earlier in the same file and every later occurrence of that class shifts by
/// one; delete one and they shift back. Two occurrences that swap order, and a finding that moved
/// to another file, are not recognized as the same.
///
/// IT IS A VALUE TYPE for the reason <see cref="Finding"/> gives, and equality here is
/// structural - two fingerprints are equal exactly when all their parts are equal.
/// </summary>
public readonly record struct FindingFingerprint
{
	/// <summary>
	/// The ordinal the FIRST occurrence of a class in a file carries.
	///
	/// ONE RATHER THAN ZERO, AND IT IS DECLARED HERE BECAUSE IT IS PUBLISHED. Where the fingerprint
	/// appears in output, this number is read by a human beside the finding's own one-based
	/// identifier, line and column. A single
	/// component counting from zero among three counting from one is the kind of inconsistency
	/// nobody notices until someone acts on it.
	/// </summary>
	public const int FIRST_OCCURRENCE_ORDINAL = 1;

	/// <summary>
	/// The character between the three parts of the string form.
	///
	/// IT CANNOT BE UNAMBIGUOUS AND THAT IS WHY THE PARSE IS DECLARED. A relative path may contain
	/// any character but the separator on at least one of the three platforms, so no delimiter
	/// exists that a path cannot hold. The form is therefore read FROM THE RIGHT: the last field
	/// is the ordinal, the one before it is the pattern class, and everything left of those is the
	/// path. That is sound without a further assumption, because a pattern class identifier is two
	/// to six upper-case letters, a hyphen and three digits - a shape that contains no separator -
	/// and an ordinal is digits.
	/// </summary>
	private const char _COMPONENT_SEPARATOR = '#';

	/// <summary>
	/// The disambiguator value that means the path needed none, which is every path in a repository
	/// where no two raw paths share a key. It adds nothing to the string form.
	/// </summary>
	public const int NO_PATH_DISAMBIGUATOR = 0;

	/// <summary>
	/// The character between the path and its disambiguator inside the path field. It is not the
	/// component separator, so the form still has three fields and is still read from the right.
	/// </summary>
	private const char _DISAMBIGUATOR_SEPARATOR = '~';

	/// <summary>Part one. The relative path with forward slashes, as the platform returned it.</summary>
	public required NormalizedPathKey Path { get; init; }

	/// <summary>
	/// Part one's disambiguator: which of several raw paths that map to one key this is,
	/// counted from one in the raw paths' ordinal order, or <see cref="NO_PATH_DISAMBIGUATOR"/> when
	/// the key belongs to one raw path only. It is DERIVED from the set of raw paths, by
	/// <see cref="PathDisambiguation"/>, at composition and again at load, and is never persisted.
	/// </summary>
	public int PathDisambiguator { get; init; }

	/// <summary>Part two. The pattern class identifier, not its name.</summary>
	public required RuleIdentifier PatternClass { get; init; }

	/// <summary>Part three. Which occurrence of that class within that file this is, counted from one.</summary>
	public required int OccurrenceOrdinal { get; init; }

	/// <summary>
	/// The deterministic string form, for the set that serializes it.
	///
	/// It is rendered here rather than by the writer so that there is one form rather than one per
	/// destination, and it is built from the invariant culture so that the ordinal's digits do not
	/// vary with a host's number formatting.
	/// </summary>
	public string Text =>
		string.Create(
			CultureInfo.InvariantCulture,
			$"{Path.Value}{DisambiguatorSuffix}{_COMPONENT_SEPARATOR}{PatternClass.Value}{_COMPONENT_SEPARATOR}{OccurrenceOrdinal}");

	private string DisambiguatorSuffix =>
		PathDisambiguator == NO_PATH_DISAMBIGUATOR
			? string.Empty
			: string.Create(CultureInfo.InvariantCulture, $"{_DISAMBIGUATOR_SEPARATOR}{PathDisambiguator}");
}
