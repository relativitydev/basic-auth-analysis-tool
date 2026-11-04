using System.Collections.Immutable;

namespace MigrateAuth.Cli.Findings;

/// <summary>
/// THE DISAMBIGUATOR: the one place that decides which raw paths sharing a key are told apart
/// in a fingerprint, and how.
///
/// IT IS DERIVED AND NEVER STORED. The session state persists a finding's path and ordinal only
/// (schema 5 is fixed), and this derivation runs over the raw paths of the whole finding set - once
/// when composition finishes and once again when the set is loaded - so a fingerprint rebuilt from a
/// saved session is the fingerprint the composing run produced. There is one function for both
/// places because two would be one more way for a saved fingerprint to differ from a live one.
///
/// THE RULE: the distinct raw paths that share one key are ordered by ordinal comparison and
/// numbered from one; a key that belongs to one raw path gets no disambiguator, so no existing
/// fingerprint changes. The number depends on which colliding paths carry findings, which is stated
/// rather than hidden: it is stable for a repository and a configuration, and it is not a promise
/// across runs that change which of the colliding files have findings.
/// </summary>
public static class PathDisambiguation
{
	/// <summary>
	/// The disambiguator of every raw path whose key it shares with another raw path, by raw path.
	/// A raw path that is not in the result needs none.
	/// </summary>
	public static Dictionary<string, int> Derive(IEnumerable<(string KeyText, string RawPath)> paths)
	{
		ArgumentNullException.ThrowIfNull(paths);

		Dictionary<string, SortedSet<string>> rawPathsByKey = new(StringComparer.Ordinal);

		foreach ((string keyText, string rawPath) in paths)
		{
			if (!rawPathsByKey.TryGetValue(keyText, out SortedSet<string>? rawPaths))
			{
				rawPaths = new SortedSet<string>(StringComparer.Ordinal);
				rawPathsByKey[keyText] = rawPaths;
			}

			rawPaths.Add(rawPath);
		}

		Dictionary<string, int> disambiguatorByRawPath = new(StringComparer.Ordinal);

		foreach (SortedSet<string> rawPaths in rawPathsByKey.Values.Where(rawPaths => rawPaths.Count > 1))
		{
			int disambiguator = FindingFingerprint.FIRST_OCCURRENCE_ORDINAL;

			foreach (string rawPath in rawPaths)
			{
				disambiguatorByRawPath[rawPath] = disambiguator;
				disambiguator++;
			}
		}

		return disambiguatorByRawPath;
	}

	/// <summary>
	/// The finding set with each colliding finding's fingerprint carrying its disambiguator. A set
	/// in which no two raw paths share a key is returned as it came.
	/// </summary>
	public static ImmutableArray<Finding> Apply(ImmutableArray<Finding> findings)
	{
		Dictionary<string, int> disambiguatorByRawPath = Derive(
			findings.Select(finding => (finding.Fingerprint.Path.Value, finding.Path.Value)));

		ImmutableArray<Finding> disambiguated = findings;

		if (disambiguatorByRawPath.Count > 0)
		{
			disambiguated = [.. findings.Select(finding => WithDisambiguator(finding, disambiguatorByRawPath))];
		}

		return disambiguated;
	}

	private static Finding WithDisambiguator(Finding finding, Dictionary<string, int> disambiguatorByRawPath)
	{
		return disambiguatorByRawPath.TryGetValue(finding.Path.Value, out int disambiguator)
			? finding with { Fingerprint = finding.Fingerprint with { PathDisambiguator = disambiguator } }
			: finding;
	}
}
