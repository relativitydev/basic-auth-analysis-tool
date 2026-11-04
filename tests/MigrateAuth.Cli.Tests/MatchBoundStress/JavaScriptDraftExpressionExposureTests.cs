using System.Text;
using System.Text.RegularExpressions;

using MigrateAuth.Cli.Tests.Catalog;

using Xunit;
using Xunit.Abstractions;

namespace MigrateAuth.Cli.Tests.MatchBoundStress;

/// <summary>
/// THE EXPOSURE SCAN OF THE `JavaScript` DRAFT EXPRESSIONS: the defect it looks for is a lazy bounded loop wrapped in an optional group, which makes the
/// source-generated matcher backtrack past its bound. Each expression's pattern is read for its lazy bounded loops and its optional groups, and none may be inside the other. Beside that
/// it records, for every expression, the bound of its widest window, the number of atomic groups (the units of a window are atomic, so a failed unit is never
/// re-entered), and the number of unbounded quantifiers (the first-line rule and the constant declaration, which read one line, are the only two that carry any; every class, content marker and flow signal is
/// bounded in full). The table is written where <see cref="REPORT_PATH_VARIABLE_NAME"/> points.
/// </summary>
public sealed partial class JavaScriptDraftExpressionExposureTests
{
	internal const string REPORT_PATH_VARIABLE_NAME = "MIGRATE_AUTH_EXPOSURE_REPORT_JAVASCRIPT";

	/// <summary>The widest window the catalog allows: the argument window, bounded at 1,500 units.</summary>
	internal const int WIDEST_WINDOW_BOUND = 1_500;

	/// <summary>The bound of the chain window, in links.</summary>
	internal const int CHAIN_WINDOW_BOUND = 20;

	private const string _UPPER_BOUND_GROUP = "upper";

	private static readonly string _ARGUMENT_WINDOW_LOOP = $"){{0,{WIDEST_WINDOW_BOUND}}}?";
	private static readonly string _CHAIN_WINDOW_LOOP = $"){{0,{CHAIN_WINDOW_BOUND}}}?";

	private static readonly HashSet<string> _EXPRESSIONS_ALLOWED_AN_UNBOUNDED_QUANTIFIER = new(StringComparer.Ordinal) { "EX-JS-100", "EX-JS-101" };

	private readonly ITestOutputHelper _output;

	public JavaScriptDraftExpressionExposureTests(ITestOutputHelper output)
	{
		_output = output;
	}

	[GeneratedRegex(@"\{\d+,(?<upper>\d+)\}")]
	private static partial Regex BoundedQuantifier();

	[Fact]
	public void NoJavaScriptDraftExpressionCarriesALazyBoundedLoopInsideAnOptionalGroupAndTheExposureTableIsRecorded()
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

		Assert.True(exposed.Count == 0, $"Exposed to the lazy-window-in-an-optional-group defect: {string.Join(", ", exposed)}");
	}

	[Fact]
	public void EveryWindowOfEveryJavaScriptDraftExpressionIsBoundedAtTheWidestWindowOrLess()
	{
		List<string> unbounded = [];

		foreach ((string identifier, Regex expression) in AllExpressions())
		{
			string pattern = expression.ToString();
			int widest = BoundedQuantifier().Matches(pattern).Select(match => int.Parse(match.Groups[_UPPER_BOUND_GROUP].Value, System.Globalization.CultureInfo.InvariantCulture)).DefaultIfEmpty(0).Max();

			if (widest > WIDEST_WINDOW_BOUND)
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
	public void EveryWindowUnitOfTheClassExpressionsThatReadAWindowIsAtomic()
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

	private static IEnumerable<(string Identifier, Regex Expression)> AllExpressions()
	{
		return JavaScriptDraftExpressions.LexicalExpressions.Concat(JavaScriptDraftExpressions.MatchBoundExpressions);
	}

	private static int CountAtomicGroups(string pattern)
	{
		return pattern.Split("(?>", StringSplitOptions.None).Length - 1;
	}

	/// <summary>The number of <c>*</c> and <c>+</c> quantifiers outside character classes and escapes; every other repetition in these expressions carries an explicit bound.</summary>
	private static int CountUnboundedQuantifiers(string pattern)
	{
		int count = 0;
		int index = 0;

		while (index < pattern.Length)
		{
			char current = pattern[index];

			if (current == '\\')
			{
				index += 2;
			}
			else if (current == '[')
			{
				index = SkipCharacterClass(pattern, index);
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
