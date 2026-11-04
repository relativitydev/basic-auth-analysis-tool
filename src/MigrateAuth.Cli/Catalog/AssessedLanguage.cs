using System.Collections.Frozen;
using System.Collections.Immutable;

namespace MigrateAuth.Cli.Catalog;

/// <summary>
/// The eight languages this product assesses. It is the product's <c>language</c> enum, and
/// it lives here rather than in the configuration set because it is finding vocabulary first:
/// configuration only filters over it.
///
/// This enumeration is load-bearing rather than descriptive. The coverage statement
/// iterates THESE eight values and never the catalog registry, so a language that has no
/// analyzer is still named in the report.
///
/// Nothing reads meaning into the numeric values beyond stability.
/// </summary>
public enum AssessedLanguage
{
	/// <summary>C#.</summary>
	CSharp = 0,

	/// <summary>Visual Basic .NET.</summary>
	VBNet = 1,

	/// <summary>JavaScript.</summary>
	JavaScript = 2,

	/// <summary>TypeScript.</summary>
	TypeScript = 3,

	/// <summary>Python.</summary>
	Python = 4,

	/// <summary>Go.</summary>
	Go = 5,

	/// <summary>PowerShell.</summary>
	PowerShell = 6,

	/// <summary>Shell (sh, bash, zsh, ksh).</summary>
	Shell = 7
}

/// <summary>
/// The enumeration's canonical names and the parse that reads one back.
///
/// The names are declared here as data rather than derived by reflection from the member names,
/// for the reason reflection-based serialization is avoided: under ahead-of-time
/// compilation a reflective path is the failure that compiles, publishes and breaks on the
/// client's machine. It also makes the emitted name a decision that a rename cannot change by
/// accident, which matters because the name is what appears in a finding record and in a
/// configuration file a client writes by hand.
///
/// The table refuses to build unless every enumeration value carries a name, so a language added
/// to <see cref="AssessedLanguage"/> without a name here does not start - the same structural
/// guarantee the command registry holds over its own descriptors.
/// </summary>
public static class AssessedLanguages
{
	/// <summary>
	/// The number of supported languages. It is asserted against the enumeration below rather
	/// than trusted.
	/// </summary>
	public const int SUPPORTED_LANGUAGE_COUNT = 8;

	private static readonly FrozenDictionary<AssessedLanguage, string> _canonicalNamesByLanguage =
		BuildValidatedCanonicalNames();

	private static readonly ImmutableArray<AssessedLanguage> _allInDeclaredOrder =
	[
		.. Enum.GetValues<AssessedLanguage>().Order()
	];

	private static readonly FrozenDictionary<string, AssessedLanguage> _languagesByCanonicalName =
		_canonicalNamesByLanguage.ToFrozenDictionary(
			entry => entry.Value,
			entry => entry.Key,
			StringComparer.OrdinalIgnoreCase);

	/// <summary>Every language, in the order this enumeration declares them.</summary>
	public static ImmutableArray<AssessedLanguage> AllInDeclaredOrder => _allInDeclaredOrder;

	/// <summary>The name this language carries in a finding record and in a configuration file.</summary>
	public static string CanonicalName(AssessedLanguage language)
	{
		if (!_canonicalNamesByLanguage.TryGetValue(language, out string? canonicalName))
		{
			throw new ArgumentOutOfRangeException(
				nameof(language),
				language,
				"No canonical name is declared for this language.");
		}

		return canonicalName;
	}

	/// <summary>
	/// Reads a name from a configuration file. Case-insensitive, because a client writes this by
	/// hand; ordinal, because every comparison in this product is kept free of locale data.
	/// A name this build does not know is not a failure - the configuration read is required
	/// to tolerate version differences, and a newer release's language is exactly that.
	/// </summary>
	public static bool TryParse(string canonicalName, out AssessedLanguage language)
	{
		bool parsed = false;
		language = default;

		if (!string.IsNullOrWhiteSpace(canonicalName))
		{
			parsed = _languagesByCanonicalName.TryGetValue(canonicalName.Trim(), out language);
		}

		return parsed;
	}

	private static FrozenDictionary<AssessedLanguage, string> BuildValidatedCanonicalNames()
	{
		Dictionary<AssessedLanguage, string> canonicalNames = new()
		{
			[AssessedLanguage.CSharp] = "CSharp",
			[AssessedLanguage.VBNet] = "VBNet",
			[AssessedLanguage.JavaScript] = "JavaScript",
			[AssessedLanguage.TypeScript] = "TypeScript",
			[AssessedLanguage.Python] = "Python",
			[AssessedLanguage.Go] = "Go",
			[AssessedLanguage.PowerShell] = "PowerShell",
			[AssessedLanguage.Shell] = "Shell"
		};

		RequireOneNamePerLanguage(canonicalNames);

		return canonicalNames.ToFrozenDictionary();
	}

	private static void RequireOneNamePerLanguage(Dictionary<AssessedLanguage, string> canonicalNames)
	{
		AssessedLanguage[] declaredLanguages = Enum.GetValues<AssessedLanguage>();

		if (declaredLanguages.Length != SUPPORTED_LANGUAGE_COUNT)
		{
			throw new InvalidOperationException(
				$"The product assesses {SUPPORTED_LANGUAGE_COUNT} languages and the enumeration declares {declaredLanguages.Length}.");
		}

		foreach (AssessedLanguage language in declaredLanguages)
		{
			if (!canonicalNames.ContainsKey(language))
			{
				throw new InvalidOperationException($"Language {language} carries no canonical name.");
			}
		}
	}
}
