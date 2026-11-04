using System.Collections.Frozen;

namespace MigrateAuth.Cli.Catalog;

/// <summary>
/// The catalog: every embedded language analyzer, indexed for the engine, together
/// with the catalog's identity.
///
/// The version travels with the catalog type. There is no constructor that does not take a
/// version, so a catalog without an identity cannot exist.
/// </summary>
public sealed class LanguageCatalog
{
	private const int _CURRENT_VERSION_MAJOR = 1;
	private const int _CURRENT_VERSION_MINOR = 13;
	private const int _CURRENT_VERSION_REVISION = 0;

	private readonly FrozenDictionary<string, ILanguageAnalyzer> _analyzersByFileExtension;

	public LanguageCatalog(CatalogVersion version, IEnumerable<ILanguageAnalyzer> analyzers)
	{
		ArgumentNullException.ThrowIfNull(analyzers);

		ILanguageAnalyzer[] registeredAnalyzers = [.. analyzers];

		Version = version;
		Analyzers = [.. registeredAnalyzers];
		AnalyzersByLanguageName = BuildLanguageNameIndex(registeredAnalyzers);
		_analyzersByFileExtension = BuildFileExtensionIndex(registeredAnalyzers);
	}

	/// <summary>
	/// The version this build's embedded catalog carries. Numbered independently of the
	/// product version. A session started under a different catalog version is started fresh.
	///
	/// <b>Its relation to the product version.</b> This number and the product version are
	/// counted on two SEPARATE scales, not one scale under two names: this one versions the
	/// runtime type <see cref="LanguageCatalog"/> actually ships - the embedded analyzers, their
	/// expressions, their detection rules. A change that touches no runtime field (prose, a
	/// corrected class placement) does not move this constant; a contract change moves it, and so
	/// does a change to an analyzer's recognition expressions, declared coverage or recommendation
	/// text. Reading either number as standing in for the other is a mistake.
	/// </summary>
	public static CatalogVersion CurrentVersion { get; } =
		new(_CURRENT_VERSION_MAJOR, _CURRENT_VERSION_MINOR, _CURRENT_VERSION_REVISION);

	/// <summary>The identity of this catalog instance. Printed in all output.</summary>
	public CatalogVersion Version { get; }

	/// <summary>Every registered analyzer, as an unordered set.</summary>
	public FrozenSet<ILanguageAnalyzer> Analyzers { get; }

	/// <summary>
	/// Every declared language name, mapped to the analyzer that emits it. Two names may map
	/// to one analyzer, which is contract element 2's not-one-to-one relation seen from here.
	/// </summary>
	public FrozenDictionary<string, ILanguageAnalyzer> AnalyzersByLanguageName { get; }

	/// <summary>Finds the analyzer that owns a file extension. Extensions are compared without case.</summary>
	public bool TryGetAnalyzerForFileExtension(string fileExtension, out ILanguageAnalyzer? analyzer)
	{
		ArgumentNullException.ThrowIfNull(fileExtension);

		bool found = _analyzersByFileExtension.TryGetValue(fileExtension, out ILanguageAnalyzer? matched);
		analyzer = matched;

		return found;
	}

	/// <summary>
	/// The detection rule for one pattern class, from the analyzer that declares the
	/// supplied language name - the same two-step lookup
	/// <see cref="MigrateAuth.Cli.Reporting.RecommendationRenderer"/> already performs to find a
	/// class's template, exposed here so a renderer can also read the
	/// rule's <see cref="DetectionRule.DisplayName"/> without duplicating that lookup. A language
	/// with no analyzer, or an analyzer with no rule for that identifier, is not an error: the
	/// caller reads it exactly as it reads a template miss, and falls back to the identifier.
	/// </summary>
	public bool TryGetDetectionRule(string languageName, string patternClassIdentifier, out DetectionRule? rule)
	{
		ArgumentNullException.ThrowIfNull(languageName);
		ArgumentNullException.ThrowIfNull(patternClassIdentifier);

		rule = null;
		bool found = false;

		if (AnalyzersByLanguageName.TryGetValue(languageName, out ILanguageAnalyzer? analyzer)
			&& analyzer.DetectionRulesByPatternClass.TryGetValue(patternClassIdentifier, out DetectionRule? matched))
		{
			rule = matched;
			found = true;
		}

		return found;
	}

	private static FrozenDictionary<string, ILanguageAnalyzer> BuildLanguageNameIndex(
		IReadOnlyCollection<ILanguageAnalyzer> analyzers)
	{
		Dictionary<string, ILanguageAnalyzer> index = new(StringComparer.Ordinal);

		foreach (ILanguageAnalyzer analyzer in analyzers)
		{
			foreach (string languageName in analyzer.LanguageIdentification.DeclaredLanguageNames)
			{
				if (!index.TryAdd(languageName, analyzer))
				{
					throw new ArgumentException(
						$"Two analyzers declare the same language name: {languageName}",
						nameof(analyzers));
				}
			}
		}

		return index.ToFrozenDictionary(StringComparer.Ordinal);
	}

	private static FrozenDictionary<string, ILanguageAnalyzer> BuildFileExtensionIndex(
		IReadOnlyCollection<ILanguageAnalyzer> analyzers)
	{
		Dictionary<string, ILanguageAnalyzer> index = new(StringComparer.OrdinalIgnoreCase);

		foreach (ILanguageAnalyzer analyzer in analyzers)
		{
			foreach (string fileExtension in analyzer.LanguageIdentification.FileExtensions)
			{
				if (!index.TryAdd(fileExtension, analyzer))
				{
					throw new ArgumentException(
						$"Two analyzers claim the same file extension: {fileExtension}",
						nameof(analyzers));
				}
			}
		}

		return index.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
	}
}
