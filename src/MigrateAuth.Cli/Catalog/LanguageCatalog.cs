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
	private const int _CURRENT_VERSION_MINOR = 50;
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
	/// product version.
	///
	/// <b>1.46.0 (the match bound moved to 115,000 ms).</b> The single-match timeout that every expression of every analyzer carries moves from 112,000 ms to 115,000 ms, by the rule already in force: ten times the worst benign per-file figure over the six shipped languages and both systems, rounded up to the next 250 ms. The worst figure, the largest of four quiet runs on Linux, is 11,478.2 ms, the `TypeScript` match-bound timing class on Linux (9,497.9 ms on Windows); ten times it is 114,782 ms, which rounds up to 115,000 ms. The `Go` draft figure of those runs is not used and the bound is not raised for it. No expression, class, level, flow, identifier or declared-coverage text changes, so no finding moves; the version moves to the next minor because the bound is part of the engine's declared behaviour a report is read under, so a session started under 1.45.1 is started fresh on the same terms as at each earlier move. This bump is the bound move only: 1.44.0 stays reserved for the first catalog set of the `Python` analyzer, which is why this number follows 1.45.1 and does not take 1.44.0.
	///
	/// <b>1.47.0 (the conditional-region reading of the constant table and the flow evidence, and the `Python` analyzer, the seventh embedded language).</b> Two changes share this number.
	/// First, two readings of the recognition engine change for `CSharp`, the only language that declares the conditional-compilation element. A constant declared in a block that the file's own symbols switch off is not collected by the file symbol table, so it is neither counted nor resolved: a name declared once in a live branch and once in a switched-off one now resolves to the live declaration, and a name declared only in a switched-off block no longer resolves. A flow signal written in such a block is not evidence of a flow, as a signal past the trustworthy extent of the zone mask is not. A block whose condition the tool cannot decide is still read as live for both, and the construct itself is still read at Low in a switched-off block. The engine builds the region map once per file, before the symbol table and the flow evaluation, and passes it to both; for every other language the map is empty. Findings may move in both directions for these shapes; the contract and every expression are unchanged and the declared-coverage text of `CSharp` changes for the entry on a declaration or a flow signal in a conditional block.
	/// Second, `Python` is registered as its own analyzer: the `.py` and `.pyw` files, and an extensionless file whose first line names `python`, `python2`, `python3` or `python3.` and digits, are assessed and no longer counted and not assessed. Its fourteen classes, its lexical profile, its demotion signals and its declared coverage are read; its recommendation templates and flow signals are not declared yet, so a finding takes the generic text and the default flow. `Go` stays counted and not assessed. No expression, class, level, flow or declared-coverage text of another language changes because of the registration.
	/// A session started under 1.46.0 is started fresh on the same terms as at each earlier move. The number is the higher minor plus one: the earlier reservation of 1.44.0 for the first catalog set of the `Python` analyzer lapses, because this line moved past it through 1.45.0, 1.45.1 and 1.46.0.
	///
	/// <b>1.48.0 (`Python` templates and flow signals).</b> The `Python` analyzer declares its recommendation templates, its two generic fallback texts and its twelve flow signals, eight interactive and four fixed-credential. Thirteen classes carry a template under both flows and the one Low class stays routed to manual review. A High or Medium `Python` finding now renders the template of its class with the token acquisition of its flow, and takes its flow from the signals: an interactive signal gives Authorization Code, otherwise a fixed-credential signal gives Client Credentials, otherwise the default. No expression, class, level, demotion or declared-coverage text changes, so no `Python` finding moves in class or in level; its flow and its recommendation text move, and no other language changes. A session started under 1.47.0 is started fresh on the same terms as at each earlier move.
	///
	/// <b>1.49.0 (the AI-assisted fallback joins the `Python` analyzer).</b> The catalog that carries the `Python` analyzer, numbered 1.48.0, is joined with the one that carries the AI-assisted fallback, whose detection catalog is numbered 1.45.1. No expression, class, level, flow, demotion, template or declared-coverage text of any language changes, so no finding moves; only the number moves. The number is the higher minor plus one: 1.48.0 over 1.45.1. A session started under 1.48.0 is started fresh on the same terms as at each earlier move.
	///
	/// <b>1.50.0 (three readings of the `Python` analyzer).</b> Three readings of `Python` change; no class, level, flow, demotion, template or declared-coverage identifier of any language changes, and no other
	/// language changes. `PY-007` reads the colon pair of an `f`-string whose replacement fields hold quotes (only a literal whose prefix contains f or F has fields; a name chain with up to three subscript or call trailers, one level of nesting, a quoted string of either kind of
	/// quote, the literal's own included), and the held-name shape when the encoder call spans lines (a run of at most 40 blanks, tabs and line breaks in each seam, and a comment in a seam is not read). `PY-013` reads a held
	/// scheme value that begins with the scheme word (the literal alone, or with `+`, `%` or `.format(` and an operand, the annotated declaration included), takes the leading word as the verdict, and declares the credential
	/// position of a held literal `NAME = "Basic token"`: the token of the declared literal, which the finding at the carrier carries as a literal credential. The declared unsupported coverage entry for a held scheme that is more than the scheme word is retired. The operand of a held scheme value takes up to three call groups, each bounded at 120 units, where a unit is one character or a nested group of at most 120 characters. Findings move up for these
	/// constructs (a pair or a held scheme that was not read is read at Medium), and the credential signal moves for the held literal with a token; a construct read before is read as before. A session started under 1.49.0 is
	/// started fresh on the same terms as at each earlier move.
	///
	/// <b>Its relation to the product version.</b> This number and the product version are
	/// counted on two SEPARATE scales, not one scale under two names: this one versions the
	/// runtime type <see cref="LanguageCatalog"/> actually ships - the embedded analyzers, their
	/// expressions, their detection rules. A change that touches no runtime field (prose, a
	/// corrected class placement) does not move this constant, and a contract change
	/// moves it. Reading either number as standing in for the other is a mistake.
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
