using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

using MigrateAuth.Cli.Tests.Catalog;

using Xunit;
using Xunit.Abstractions;

namespace MigrateAuth.Cli.Tests.MatchBoundStress;

/// <summary>
/// THE EXPOSURE SCAN OF THE DRAFT `TypeScript` EXPRESSIONS: the defect it looks for is a lazy bounded loop wrapped in an optional group, which makes the
/// source-generated matcher backtrack past its bound. Each expression's pattern is read for its lazy bounded loops and its optional groups, and none may be inside the other. The scan covers the whole set one
/// `TypeScript` file is charged (the inherited expressions that stay and the twelve new or amended ones), so the optional generic-argument group that the callee expressions gain is read like every other optional group.
/// Beside that it records, for every expression, its figures: the bound of its widest window, the number of atomic groups, and the number of unbounded quantifiers (only the typed constant declaration,
/// which reads one line, carries any; every class, content marker and flow signal is bounded in full). A further check states the shape of the generic-argument group: three levels, each atomic and bounded at 120 units.
/// The table is written where <see cref="REPORT_PATH_VARIABLE_NAME"/> points.
/// </summary>
public sealed partial class TypeScriptDraftExpressionExposureTests
{
	internal const string REPORT_PATH_VARIABLE_NAME = "MIGRATE_AUTH_EXPOSURE_REPORT_TYPESCRIPT";

	/// <summary>Where the composed text of the twelve new or amended expressions is written, one per line.</summary>
	internal const string TEXT_REPORT_PATH_VARIABLE_NAME = "MIGRATE_AUTH_EXPRESSION_TEXT_REPORT_TYPESCRIPT";

	private const string _UPPER_BOUND_GROUP = "upper";
	private const string _ATOMIC_GROUP_OPENING = "(?>";
	private const string _GENERIC_LEVEL_OPENING = "<(?>";
	private const int _GENERIC_LEVELS = 3;
	private const int _ESCAPE_WIDTH = 2;
	private const string _TYPED_CONSTANT_IDENTIFIER = "EX-TS-101";
	private const string _ENVIRONMENT_FORWARD_GUARD = @"(?=(?:process|import|Deno|Bun)(?![\w$])|`\$\{)";

	private static readonly string _ARGUMENT_WINDOW_LOOP = $"){{0,{JavaScriptDraftExpressionExposureTests.WIDEST_WINDOW_BOUND}}}";
	private static readonly string _CHAIN_WINDOW_LOOP = $"){{0,{JavaScriptDraftExpressionExposureTests.CHAIN_WINDOW_BOUND}}}";
	private static readonly string _GENERIC_LEVEL_LOOP = $"{{0,{JavaScriptDraftExpressions.GENERIC_UNITS_PER_LEVEL}}})>";

	/// <summary>
	/// Two expressions are allowed an unbounded quantifier, and each for one stated reason. `EX-TS-101` reads one line (the declaration is anchored at the start of the line and its value runs to the end of that line).
	/// `EX-JS-013` is a shipped expression that this file charges and cannot edit: its proxy-name refusal is <c>[\w$]*</c>, which runs from the start of a name that a look-behind has already limited to 64 characters, and
	/// only after the forward guard on the quote and scheme of a URL has matched, so in effect it is bounded at 64. Allowing it here records that fact; it is not a claim that the expression is bounded in text.
	/// </summary>
	private static readonly HashSet<string> _EXPRESSIONS_ALLOWED_AN_UNBOUNDED_QUANTIFIER = new(StringComparer.Ordinal) { "EX-TS-101", "EX-JS-013" };

	private static readonly string[] _EXPRESSIONS_WITH_THE_GENERIC_GROUP = ["EX-TS-G-001", "EX-TS-G-003", "EX-TS-G-015", "EX-TS-G-016", "EX-TS-003", "EX-TS-122"];

	private readonly ITestOutputHelper _output;

	public TypeScriptDraftExpressionExposureTests(ITestOutputHelper output)
	{
		_output = output;
	}

	[GeneratedRegex(@"\{\d+,(?<upper>\d+)\}")]
	private static partial Regex BoundedQuantifier();

	[Fact]
	public void NoTypeScriptDraftExpressionCarriesALazyBoundedLoopInsideAnOptionalGroupAndTheExposureTableIsRecorded()
	{
		StringBuilder table = new();
		List<string> exposed = [];

		table.AppendLine("expression\tlazy_bounded_loops\tbounds\toptional_groups\tlazy_loops_in_optional_group\tatomic_groups\tunbounded_quantifiers\tpattern_length\treading");

		foreach ((string identifier, Regex expression) in AllExpressions())
		{
			string pattern = expression.ToString();
			PatternShape shape = PatternShape.Analyze(pattern);

			table.Append(identifier).Append('\t').Append(shape.LazyBoundedLoops.Count).Append('\t').Append(string.Join(' ', shape.LazyBoundedLoops.Select(loop => loop.Bounds)))
				.Append('\t').Append(shape.OptionalGroupCount).Append('\t').Append(shape.LazyLoopsInOptionalGroups).Append('\t').Append(CountAtomicGroups(pattern)).Append('\t')
				.Append(CountUnboundedQuantifiers(pattern)).Append('\t').Append(pattern.Length).Append('\t').AppendLine(shape.LazyLoopsInOptionalGroups == 0
					? (shape.LazyBoundedLoops.Count == 0 ? "no lazy bounded loop" : "every lazy bounded loop is in a required position")
					: "EXPOSED");

			if (shape.LazyLoopsInOptionalGroups > 0)
			{
				exposed.Add(identifier);
			}
		}

		_output.WriteLine(table.ToString());

		string? path = Environment.GetEnvironmentVariable(REPORT_PATH_VARIABLE_NAME);

		if (!string.IsNullOrWhiteSpace(path))
		{
			File.WriteAllText(path, table.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
		}

		WriteTheExpressionTextWhereRequested();

		Assert.True(exposed.Count == 0, $"Exposed to the lazy-window-in-an-optional-group defect: {string.Join(", ", exposed)}");
	}

	[Fact]
	public void EveryWindowOfEveryTypeScriptDraftExpressionIsBoundedAtTheWidestWindowOrLess()
	{
		List<string> unbounded = [];

		foreach ((string identifier, Regex expression) in AllExpressions())
		{
			string pattern = expression.ToString();
			int widest = BoundedQuantifier().Matches(pattern).Select(match => int.Parse(match.Groups[_UPPER_BOUND_GROUP].Value, CultureInfo.InvariantCulture)).DefaultIfEmpty(0).Max();

			if (widest > JavaScriptDraftExpressionExposureTests.WIDEST_WINDOW_BOUND)
			{
				unbounded.Add($"{identifier}: a bound of {widest}");
			}

			if (CountUnboundedQuantifiers(pattern) > 0 && !_EXPRESSIONS_ALLOWED_AN_UNBOUNDED_QUANTIFIER.Contains(identifier))
			{
				unbounded.Add($"{identifier}: {CountUnboundedQuantifiers(pattern)} unbounded quantifiers");
			}
		}

		Assert.Empty(unbounded);
	}

	[Fact]
	public void EveryWindowUnitOfTheExpressionsThatReadAWindowIsAtomic()
	{
		List<string> notAtomic = [];

		foreach ((string identifier, Regex expression) in AllExpressions())
		{
			string pattern = expression.ToString();
			bool readsAWindow = pattern.Contains(_ARGUMENT_WINDOW_LOOP, StringComparison.Ordinal) || pattern.Contains(_CHAIN_WINDOW_LOOP, StringComparison.Ordinal);

			if (readsAWindow && CountAtomicGroups(pattern) == 0)
			{
				notAtomic.Add(identifier);
			}
		}

		Assert.Empty(notAtomic);
	}

	/// <summary>The generic-argument group is three levels deep, and every level is one atomic group bounded at the stated number of units, so a failed group costs its own length once and a fourth level is not read.</summary>
	[Fact]
	public void TheGenericArgumentGroupIsThreeAtomicLevelsEachBoundedAtTheStatedUnits()
	{
		List<string> wrong = [];

		foreach ((string identifier, Regex expression) in TypeScriptDraftExpressions.NewAndAmendedExpressions.Where(entry => _EXPRESSIONS_WITH_THE_GENERIC_GROUP.Contains(entry.Identifier)))
		{
			string pattern = expression.ToString();
			int levels = CountOccurrences(pattern, _GENERIC_LEVEL_OPENING);
			int boundedLevels = CountOccurrences(pattern, _GENERIC_LEVEL_LOOP);

			if (levels != _GENERIC_LEVELS || boundedLevels != _GENERIC_LEVELS)
			{
				wrong.Add($"{identifier}: {levels} levels, {boundedLevels} bounded at {JavaScriptDraftExpressions.GENERIC_UNITS_PER_LEVEL}");
			}
		}

		Assert.Empty(wrong);
	}

	/// <summary>The environment alternative of the typed constant declaration opens behind a forward guard on the rare words that open an environment read (the five roots, or the template whose hole holds one), so the file with no such word pays nothing for it.</summary>
	[Fact]
	public void TheTypedConstantDeclarationOpensItsEnvironmentAlternativeBehindAForwardGuardOnTheFiveReadRoots()
	{
		string pattern = TypeScriptDraftExpressions.NewAndAmendedExpressions.Single(entry => entry.Identifier == _TYPED_CONSTANT_IDENTIFIER).Expression.ToString();

		Assert.Contains(_ENVIRONMENT_FORWARD_GUARD, pattern, StringComparison.Ordinal);
	}

	/// <summary>
	/// Known defect: the .NET 10 regex source generator backtracks wrongly on a lazy bounded quantifier (<c>{n,m}?</c>, or <c>*?</c> and <c>+?</c>), wherever it stands. The scan reads every TypeScript draft expression (the twelve new or
	/// amended ones) outside character classes and escapes and asserts zero sites.
	/// </summary>
	[Fact]
	public void NoTypeScriptDraftExpressionCarriesAnyLazyBoundedQuantifier()
	{
		List<string> sites = [.. TypeScriptDraftExpressions.NewAndAmendedExpressions.Select(entry => (entry.Identifier, Count: CountLazyQuantifiers(entry.Expression.ToString()))).Where(entry => entry.Count > 0).Select(entry => $"{entry.Identifier}: {entry.Count}")];

		Assert.Empty(sites);
	}

	/// <summary>The scan itself is checked on patterns with known sites, so that a zero result means the scan reads what it should.</summary>
	[Theory]
	[InlineData(@"a(?:b){0,5}?c", 1)]
	[InlineData(@"a[^x]{0,5}?b*?c+?", 3)]
	[InlineData(@"a(?:b){0,5}c(?>d)*e\{0,5\}?", 0)]
	[InlineData(@"a[{0,5}?]b(?:c)?d", 0)]
	public void TheLazyQuantifierScanCountsExactlyTheLazyQuantifiersOutsideClassesAndEscapes(string pattern, int expected)
	{
		Assert.Equal(expected, CountLazyQuantifiers(pattern));
	}

	/// <summary>
	/// Recorded, never asserted: the lazy quantifier sites of the shipped inherited expressions that the gate charges and that no test-local file may edit. One line per expression that has any,
	/// as <c>identifier: sites</c>; the same scan, so the figures are comparable with the zero asserted for the TypeScript expressions.
	/// </summary>
	[Fact]
	public void TheLazyQuantifierSitesOfTheInheritedShippedExpressionsAreRecorded()
	{
		string[] lines = [.. TypeScriptDraftExpressions.InheritedShippedExpressions.Select(entry => (entry.Identifier, Count: CountLazyQuantifiers(entry.Expression.ToString()))).Where(entry => entry.Count > 0).Select(entry => $"{entry.Identifier}: {entry.Count}")];

		_output.WriteLine($"INHERITED-LAZY {TypeScriptDraftExpressions.InheritedShippedExpressions.Length} scanned; {lines.Length} with sites; {string.Join("; ", lines)}");
		Assert.NotNull(lines);
	}

	/// <summary>The number of lazy quantifiers outside character classes and escapes: a bounded <c>{n,m}?</c> or <c>{n}?</c>, and <c>*?</c> and <c>+?</c>.</summary>
	internal static int CountLazyQuantifiers(string pattern)
	{
		int count = 0;
		int index = 0;

		while (index < pattern.Length)
		{
			char current = pattern[index];

			if (current == '\\')
			{
				index += _ESCAPE_WIDTH;
			}
			else if (current == '[')
			{
				index = SkipCharacterClass(pattern, index);
			}
			else
			{
				bool followedByLazyMark = index + 1 < pattern.Length && pattern[index + 1] == '?';

				count += followedByLazyMark && (current is '*' or '+' || (current == '}' && IsABoundedQuantifierEnd(pattern, index))) ? 1 : 0;
				index++;
			}
		}

		return count;
	}

	private static bool IsABoundedQuantifierEnd(string pattern, int closingBraceIndex)
	{
		int opening = pattern.LastIndexOf('{', closingBraceIndex);

		return opening >= 0 && pattern.AsSpan(opening + 1, closingBraceIndex - opening - 1).ToString().All(character => char.IsDigit(character) || character == ',');
	}

	private static void WriteTheExpressionTextWhereRequested()
	{
		string? path = Environment.GetEnvironmentVariable(TEXT_REPORT_PATH_VARIABLE_NAME);

		if (!string.IsNullOrWhiteSpace(path))
		{
			File.WriteAllLines(path, TypeScriptDraftExpressions.NewAndAmendedExpressions.Select(entry => $"{entry.Identifier}	{entry.Expression}"), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
		}
	}

	private static IEnumerable<(string Identifier, Regex Expression)> AllExpressions()
	{
		return TypeScriptDraftExpressions.MatchBoundExpressions;
	}

	private static int CountAtomicGroups(string pattern)
	{
		return CountOccurrences(pattern, _ATOMIC_GROUP_OPENING);
	}

	private static int CountOccurrences(string pattern, string fragment)
	{
		return pattern.Split(fragment, StringSplitOptions.None).Length - 1;
	}

	/// <summary>
	/// The number of <c>*</c> and <c>+</c> quantifiers outside character classes and escapes; every other repetition in these expressions carries an explicit bound. The one declared exception is the blank run of the
	/// whole-literal terminator, <c>(?=[ \t]*(?:...))</c>. Reason, plainly: the shipped vocabulary writes that blank run with <c>*</c>, so every shipped expression that reads a credential literal carries one, and this file
	/// cannot edit them; the run is a look-ahead over blanks that stands only after a closing quote, so it scans at most the blanks of one line once per literal. Counting it out keeps the guard strict for every other quantifier.
	/// </summary>
	private static int CountUnboundedQuantifiers(string pattern)
	{
		string counted = pattern.Replace(JavaScriptDraftExpressions.MERGED_VALUE_TERMINATOR, string.Empty, StringComparison.Ordinal);
		int count = 0;
		int index = 0;

		while (index < counted.Length)
		{
			char current = counted[index];

			if (current == '\\')
			{
				index += 2;
			}
			else if (current == '[')
			{
				index = SkipCharacterClass(counted, index);
			}
			else
			{
				count += current is '*' or '+' ? 1 : 0;
				index++;
			}
		}

		return count;
	}

	private static int SkipCharacterClass(string pattern, int start)
	{
		int index = start + 1;

		if (index < pattern.Length && pattern[index] == '^')
		{
			index++;
		}

		if (index < pattern.Length && pattern[index] == ']')
		{
			index++;
		}

		while (index < pattern.Length && pattern[index] != ']')
		{
			index += pattern[index] == '\\' ? 2 : 1;
		}

		return index + 1;
	}
}
