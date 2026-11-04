using System.Collections.Immutable;
using System.Text.RegularExpressions;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Emission;
using MigrateAuth.Cli.Lexical;

namespace MigrateAuth.Cli.Scanning;

/// <summary>
/// What one file was detected as, and the analyzer that owns it. The two travel together because
/// the inventory needs the name and preselection needs the analyzer, and deriving them separately
/// is how a file gets counted under one language and preselected with another's anchors.
/// </summary>
public readonly record struct AttributedLanguage
{
	internal AttributedLanguage(string languageName, ILanguageAnalyzer? analyzer, bool firstLineBudgetExceeded = false)
	{
		LanguageName = languageName;
		Analyzer = analyzer;
		FirstLineBudgetExceeded = firstLineBudgetExceeded;
	}

	/// <summary>The name the inventory counts this file under. Never empty.</summary>
	public string LanguageName { get; }

	/// <summary>The owning analyzer, or null where no registered analyzer claims the file.</summary>
	public ILanguageAnalyzer? Analyzer { get; }

	/// <summary>Whether a registered analyzer claims this file. The detection rule excludes the rest from candidate selection.</summary>
	public bool IsRecognized => Analyzer is not null;

	/// <summary>
	/// Whether this file's first-line detection tripped the lexical budget, so that the file
	/// was attributed without its first line - by its extension alone - rather than by what its
	/// first line said. The inventory counts these, so the fallback is visible rather than silent.
	/// </summary>
	public bool FirstLineBudgetExceeded { get; }
}

/// <summary>
/// The detection rule made into a lookup: a registered analyzer's extension first, then - where there
/// is no extension - a registered analyzer's own first-line rule, then the base's own table (extension,
/// or shebang for an extensionless file), and an unrecognized bucket for everything that answers none
/// of those.
///
/// Every input to a registered analyzer's decision comes off the analyzer contract - the extension
/// set, the first-line expression and the name derivation - so a language added to the catalog is
/// attributed without this file changing. <see cref="BaseLanguageDetection"/> is the one exception: it
/// lets the inventory name all eight languages, but it yields no analyzer -
/// <see cref="AttributedLanguage.Analyzer"/> is null for every file that path names, so candidate
/// selection and the recognition engine never see those names.
///
/// The first-line fallback - an analyzer's own, and the base's - is scoped to files with no extension,
/// so a first line never claims a file whose extension already said otherwise.
///
/// Where two analyzers could claim the same first line, the tie is broken by the ordinal order of
/// their declared names rather than by the catalog's enumeration order, so the outcome is identical
/// on every platform and every run. A registered analyzer's first-line rule always wins over the
/// base's own shebang table, as a registered analyzer's extension wins over the base's own extension
/// table: precedence runs from the specific to the generic.
///
/// The first-line expressions are charged to the lexical budget, one budget per file in this stage.
/// Where the budget is spent, or an expression exceeds its own timeout, the file is treated as not
/// recognized by its first line: it is attributed by its extension and the fallback is carried on
/// <see cref="AttributedLanguage.FirstLineBudgetExceeded"/>. This is the only place a first line is
/// matched against an analyzer's expression - Analyze reuses the attribution Scan persisted and never
/// repeats it, so a trip cannot give one file two languages.
/// </summary>
public sealed class LanguageAttribution
{
	/// <summary>
	/// What the detection rule calls a file matching neither rule. It is a row in the inventory rather
	/// than an absence, because a repository that is nine tenths unrecognized is a fact about the
	/// assessment's coverage and the reader must not have to infer it from a total that does not
	/// add up.
	/// </summary>
	public const string UNRECOGNIZED_LANGUAGE_NAME = "unrecognized";

	private readonly LanguageCatalog _catalog;
	private readonly ImmutableArray<ILanguageAnalyzer> _analyzersWithAFirstLineRule;
	private readonly LexicalBudgetWatch _budgetWatch;

	public LanguageAttribution(LanguageCatalog catalog)
		: this(catalog, TimeProvider.System)
	{
	}

	/// <summary>The clock is an input so that the lexical budget is testable without waiting.</summary>
	public LanguageAttribution(LanguageCatalog catalog, TimeProvider timeProvider)
	{
		ArgumentNullException.ThrowIfNull(catalog);
		ArgumentNullException.ThrowIfNull(timeProvider);

		_catalog = catalog;
		_budgetWatch = new LexicalBudgetWatch(timeProvider);
		_analyzersWithAFirstLineRule = OrderAnalyzersWithAFirstLineRule(catalog);
	}

	/// <summary>
	/// Attributes one file. The first line is null during the enumeration pass, which opens
	/// nothing, and is the decoded first line during the inventory pass - so the same file can be
	/// attributed provisionally without a read and definitively with one.
	///
	/// Where no registered analyzer claims the file, <see cref="BaseLanguageDetection"/> is
	/// tried before the file falls to <see cref="UNRECOGNIZED_LANGUAGE_NAME"/> - a base-table hit
	/// carries a null analyzer, so it is named in the inventory and excluded from candidate
	/// selection on the same path an unrecognized file already takes.
	/// </summary>
	public AttributedLanguage Attribute(RepositoryRelativePath path, string? firstLine)
	{
		string fileExtension = Path.GetExtension(path.Value);

		_budgetWatch.BeginFile();

		ILanguageAnalyzer? analyzer = FindAnalyzer(fileExtension, firstLine);
		bool firstLineBudgetExceeded = _budgetWatch.HasBeenExceeded;
		string? usableFirstLine = firstLineBudgetExceeded ? null : firstLine;
		AttributedLanguage attribution;

		if (analyzer is not null)
		{
			attribution = new AttributedLanguage(
				analyzer.LanguageIdentification.LanguageNameDerivation(fileExtension, usableFirstLine),
				analyzer);
		}
		else if (TryAttributeByBaseTable(fileExtension, usableFirstLine, out string baseLanguageName))
		{
			attribution = new AttributedLanguage(baseLanguageName, analyzer: null, firstLineBudgetExceeded);
		}
		else
		{
			attribution = new AttributedLanguage(UNRECOGNIZED_LANGUAGE_NAME, null, firstLineBudgetExceeded);
		}

		return attribution;
	}

	private static ImmutableArray<ILanguageAnalyzer> OrderAnalyzersWithAFirstLineRule(LanguageCatalog catalog)
	{
		return
		[
			.. catalog.Analyzers
				.Where(analyzer => analyzer.LanguageIdentification.FirstLineDetectionExpression is not null)
				.OrderBy(EarliestDeclaredName, StringComparer.Ordinal)
		];
	}

	private static string EarliestDeclaredName(ILanguageAnalyzer analyzer)
	{
		return analyzer.LanguageIdentification.DeclaredLanguageNames.Order(StringComparer.Ordinal).First();
	}

	private ILanguageAnalyzer? FindAnalyzer(string fileExtension, string? firstLine)
	{
		ILanguageAnalyzer? analyzer;

		if (_catalog.TryGetAnalyzerForFileExtension(fileExtension, out ILanguageAnalyzer? byExtension))
		{
			analyzer = byExtension;
		}
		else if (fileExtension.Length == 0 && firstLine is not null)
		{
			analyzer = FindByFirstLine(firstLine);
		}
		else
		{
			analyzer = null;
		}

		return analyzer;
	}

	/// <summary>
	/// Consulted only after every registered analyzer has refused the file: the base's own
	/// extension table, or - for an extensionless file - the base's own shebang table, exactly the
	/// same "extensionless only" scoping `FindAnalyzer` already applies to a registered analyzer's
	/// first-line rule.
	/// </summary>
	private static bool TryAttributeByBaseTable(string fileExtension, string? firstLine, out string languageName)
	{
		bool found;
		AssessedLanguage language;

		if (fileExtension.Length == 0 && firstLine is not null)
		{
			found = BaseLanguageDetection.TryGetByShebangFirstLine(firstLine, out language);
		}
		else
		{
			found = BaseLanguageDetection.TryGetByExtension(fileExtension, out language);
		}

		languageName = found ? AssessedLanguages.CanonicalName(language) : string.Empty;

		return found;
	}

	private ILanguageAnalyzer? FindByFirstLine(string firstLine)
	{
		ILanguageAnalyzer? matched = null;

		foreach (ILanguageAnalyzer analyzer in _analyzersWithAFirstLineRule)
		{
			bool claims = MatchFirstLine(analyzer.LanguageIdentification.FirstLineDetectionExpression!, firstLine);

			if (_budgetWatch.HasBeenExceeded)
			{
				break;
			}

			if (claims)
			{
				matched = analyzer;
				break;
			}
		}

		return matched;
	}

	/// <summary>
	/// THE ONE PLACE A FIRST LINE IS EVALUATED AGAINST AN ANALYZER-DECLARED EXPRESSION, charged to the
	/// file's lexical budget. A search that exceeds the expression's own timeout reads as no match and
	/// trips the budget, which <see cref="FindByFirstLine"/> checks straight after.
	/// </summary>
	private bool MatchFirstLine(Regex firstLineDetectionExpression, string firstLine)
	{
		return _budgetWatch.IsMatch(firstLineDetectionExpression, firstLine);
	}
}
