using System.Collections.Frozen;

namespace MigrateAuth.Cli.Catalog;

/// <summary>
/// The names of <see cref="ConfidenceLevel"/> and the two operations a configuration needs over
/// it: reading one back from a file, and deciding which of two reports more.
///
/// It lives beside the level rather than in the configuration set because the vocabulary is the
/// rubric's. The threshold does not enter the rubric - the level a finding
/// receives is assigned by the rubric alone and the threshold decides only which assigned levels
/// appear - so what configuration owns is the comparison, not the scale.
///
/// The names are declared as data for the reason given in <see cref="AssessedLanguages"/>: a
/// reflective path under ahead-of-time compilation is the failure that survives the build and
/// dies on the client's machine.
/// </summary>
public static class ConfidenceLevels
{
	/// <summary>
	/// The widest threshold there is, and the built-in default. Reporting every level is the
	/// configuration that hides nothing.
	/// </summary>
	public const ConfidenceLevel WIDEST = ConfidenceLevel.Low;

	private static readonly FrozenDictionary<ConfidenceLevel, string> _namesByLevel = BuildValidatedNames();

	private static readonly FrozenDictionary<string, ConfidenceLevel> _levelsByName =
		_namesByLevel.ToFrozenDictionary(entry => entry.Value, entry => entry.Key, StringComparer.OrdinalIgnoreCase);

	/// <summary>The name this level carries in a configuration file and in output.</summary>
	public static string Name(ConfidenceLevel level)
	{
		if (!_namesByLevel.TryGetValue(level, out string? name))
		{
			throw new ArgumentOutOfRangeException(nameof(level), level, "No name is declared for this confidence level.");
		}

		return name;
	}

	/// <summary>
	/// Reads a level from a configuration file. Case-insensitive and ordinal, as everywhere else
	/// in this product. Unlike a language name, a name outside these three is NOT tolerated: the
	/// rubric defines three levels and that vocabulary does not grow with releases, so an
	/// unrecognized one is a structural error rather than a version difference.
	/// </summary>
	public static bool TryParse(string? name, out ConfidenceLevel level)
	{
		bool parsed = false;
		level = WIDEST;

		if (!string.IsNullOrWhiteSpace(name))
		{
			parsed = _levelsByName.TryGetValue(name.Trim(), out level);
		}

		return parsed;
	}

	/// <summary>
	/// Whichever of the two thresholds reports MORE findings, which is the lower level. This is
	/// the arithmetic that states what a repository's threshold changed: the repository's own file
	/// replaces the effective value and may move it either way, so a raise is stated as well as a
	/// lowering.
	/// </summary>
	public static ConfidenceLevel Widest(ConfidenceLevel first, ConfidenceLevel second) =>
		first <= second ? first : second;

	private static FrozenDictionary<ConfidenceLevel, string> BuildValidatedNames()
	{
		Dictionary<ConfidenceLevel, string> names = new()
		{
			[ConfidenceLevel.Low] = "Low",
			[ConfidenceLevel.Medium] = "Medium",
			[ConfidenceLevel.High] = "High"
		};

		foreach (ConfidenceLevel level in Enum.GetValues<ConfidenceLevel>())
		{
			if (!names.ContainsKey(level))
			{
				throw new InvalidOperationException($"Confidence level {level} carries no name.");
			}
		}

		return names.ToFrozenDictionary();
	}
}
