using System.Collections.Immutable;
using System.Globalization;
using System.Text.RegularExpressions;

using MigrateAuth.Cli.Catalog;

namespace MigrateAuth.Cli.Tests.Documentation;

/// <summary>One search a Part A class gives: the class it belongs to and the expression, with whether the command ignores letter case.</summary>
internal sealed record DocumentedSearch(string PatternClassIdentifier, string Expression, bool IgnoresCase);

/// <summary>One form the analyzer covers, as source text, kept under the class whose finding it must produce.</summary>
internal sealed record SearchableForm(string PatternClassIdentifier, string Label, string Source);

/// <summary>
/// THE SEARCHES OF PART A AS FUNCTIONS OF TEXT: READ THEM OUT OF THE DOCUMENT, RUN THEM OVER SOURCE, AND NAME THE FORMS THEY MISS.
///
/// A search of Part A is an <c>rg</c> command whose last quoted argument is an expression. Running it here uses the same
/// expression through .NET's engine, line by line, which is how <c>rg</c> and <c>grep -E</c> read a file; the expressions
/// are kept to the syntax all three share, and a check refuses any that is not (a look-around or a back-reference).
/// </summary>
internal static class PartASearchChecks
{
	private const string _SEARCH_LABEL = "**How to search.**";
	private const string _SEARCH_FENCE = "```bash";
	private const string _FENCE_MARKER = "```";
	private const string _COMMAND_PREFIX = "rg ";
	private const string _IGNORE_CASE_FLAG = " -i ";
	private const string _SEARCH_COMMAND_PATTERN = "^rg\\s.*\\s'(?<expression>[^']*)'\\s*$";
	private const string _EXPRESSION_GROUP = "expression";
	private const string _LINE_BREAK = "\n";
	private const string _EXAMPLE_FENCE_PATTERN = "```{0}\\n(?<code>.*?)\\n```";
	private const string _CODE_GROUP = "code";
	private const int _MATCH_TIMEOUT_SECONDS = 5;
	private const int _LABEL_EXCERPT_LENGTH = 70;
	private const string _EXCERPT_ELLIPSIS = "...";
	private const string _UNSHARED_SYNTAX_PATTERN = "\\(\\?[<=!]|\\\\[1-9]";

	private static readonly Regex _searchCommand = new(_SEARCH_COMMAND_PATTERN, RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture);
	private static readonly Regex _unsharedSyntax = new(_UNSHARED_SYNTAX_PATTERN, RegexOptions.CultureInvariant);

	/// <summary>Every search the class sections of Part A give, with the class each belongs to.</summary>
	public static ImmutableArray<DocumentedSearch> SearchesOf(string partA, IReadOnlyDictionary<string, DetectionRule> rulesByPatternClass)
	{
		ImmutableArray<DocumentedSearch>.Builder searches = ImmutableArray.CreateBuilder<DocumentedSearch>();

		foreach ((string patternClassIdentifier, DetectionRule rule) in rulesByPatternClass.OrderBy(entry => entry.Key, StringComparer.Ordinal))
		{
			string section = PublicDocumentChecks.SectionOf(partA, PublicDocumentChecks.ClassHeading(patternClassIdentifier, rule.DisplayName)) ?? string.Empty;

			foreach (string commandLine in SearchCommandsOf(section))
			{
				Match command = _searchCommand.Match(commandLine);

				if (command.Success)
				{
					bool ignoresCase = commandLine.Contains(_IGNORE_CASE_FLAG, StringComparison.Ordinal);

					searches.Add(new DocumentedSearch(patternClassIdentifier, command.Groups[_EXPRESSION_GROUP].Value, ignoresCase));
				}
			}
		}

		return searches.ToImmutable();
	}

	/// <summary>The searches that use a construct <c>rg</c>'s default engine or <c>grep -E</c> would not read the same way.</summary>
	public static ImmutableArray<string> UnsharedSyntaxProblems(IEnumerable<DocumentedSearch> searches)
	{
		return
		[
			.. searches
				.Where(search => _unsharedSyntax.IsMatch(search.Expression))
				.Select(search => $"The search of {search.PatternClassIdentifier} uses a look-around or a back-reference, which grep -E does not read: {search.Expression}")
		];
	}

	/// <summary>Whether any single line of <paramref name="source"/> matches the search, which is what a line-oriented search reports.</summary>
	public static bool Finds(DocumentedSearch search, string source)
	{
		RegexOptions options = RegexOptions.CultureInvariant | (search.IgnoresCase ? RegexOptions.IgnoreCase : RegexOptions.None);
		Regex expression = new(search.Expression, options, TimeSpan.FromSeconds(_MATCH_TIMEOUT_SECONDS));

		return source.Split(_LINE_BREAK).Any(expression.IsMatch);
	}

	/// <summary>Every covered form that no search of its own class finds.</summary>
	public static ImmutableArray<string> MissedFormProblems(IEnumerable<DocumentedSearch> searches, IEnumerable<SearchableForm> forms)
	{
		ILookup<string, DocumentedSearch> searchesByClass = searches.ToLookup(search => search.PatternClassIdentifier, StringComparer.Ordinal);
		ImmutableArray<string>.Builder problems = ImmutableArray.CreateBuilder<string>();

		foreach (SearchableForm form in forms)
		{
			if (!searchesByClass[form.PatternClassIdentifier].Any(search => Finds(search, form.Source)))
			{
				problems.Add($"No search of {form.PatternClassIdentifier} finds the form '{form.Label}': {Excerpt(form.Source)}");
			}
		}

		return problems.ToImmutable();
	}

	/// <summary>Every search that finds none of the forms of its own class, which a search that never runs would also satisfy.</summary>
	public static ImmutableArray<string> SearchesThatFindNothingProblems(IEnumerable<DocumentedSearch> searches, IEnumerable<SearchableForm> forms)
	{
		ILookup<string, SearchableForm> formsByClass = forms.ToLookup(form => form.PatternClassIdentifier, StringComparer.Ordinal);

		return
		[
			.. searches
				.Where(search => !formsByClass[search.PatternClassIdentifier].Any(form => Finds(search, form.Source)))
				.Select(search => $"The search of {search.PatternClassIdentifier} finds none of its forms: {search.Expression}")
		];
	}

	/// <summary>The placeholder examples of every class section, as forms of that class: the code Part A itself shows a reader to compare.</summary>
	public static ImmutableArray<SearchableForm> ExamplesOf(string partA, IReadOnlyDictionary<string, DetectionRule> rulesByPatternClass, string exampleFenceLabel)
	{
		ImmutableArray<SearchableForm>.Builder examples = ImmutableArray.CreateBuilder<SearchableForm>();
		string fencePattern = string.Format(CultureInfo.InvariantCulture, _EXAMPLE_FENCE_PATTERN, exampleFenceLabel);

		foreach ((string patternClassIdentifier, DetectionRule rule) in rulesByPatternClass.OrderBy(entry => entry.Key, StringComparer.Ordinal))
		{
			string section = PublicDocumentChecks.SectionOf(partA, PublicDocumentChecks.ClassHeading(patternClassIdentifier, rule.DisplayName)) ?? string.Empty;
			int blockNumber = 0;

			foreach (Match block in Regex.Matches(section, fencePattern, RegexOptions.Singleline | RegexOptions.CultureInvariant))
			{
				blockNumber++;
				examples.Add(new SearchableForm(patternClassIdentifier, $"{patternClassIdentifier} example {blockNumber}", block.Groups[_CODE_GROUP].Value.Trim()));
			}
		}

		return examples.ToImmutable();
	}

	private static List<string> SearchCommandsOf(string section)
	{
		List<string> commands = [];
		int labelAt = section.IndexOf(_SEARCH_LABEL, StringComparison.Ordinal);
		int fenceAt = labelAt < 0 ? -1 : section.IndexOf(_SEARCH_FENCE, labelAt, StringComparison.Ordinal);

		if (fenceAt >= 0)
		{
			string[] lines = section[(fenceAt + _SEARCH_FENCE.Length)..].Split(_LINE_BREAK);

			foreach (string line in lines.Skip(1).TakeWhile(line => !line.StartsWith(_FENCE_MARKER, StringComparison.Ordinal)))
			{
				if (line.StartsWith(_COMMAND_PREFIX, StringComparison.Ordinal))
				{
					commands.Add(line.TrimEnd());
				}
			}
		}

		return commands;
	}

	private static string Excerpt(string source)
	{
		string singleLine = PublicDocumentChecks.NormalizeWhitespace(source);

		return singleLine.Length <= _LABEL_EXCERPT_LENGTH ? singleLine : string.Concat(singleLine.AsSpan(0, _LABEL_EXCERPT_LENGTH), _EXCERPT_ELLIPSIS);
	}
}
