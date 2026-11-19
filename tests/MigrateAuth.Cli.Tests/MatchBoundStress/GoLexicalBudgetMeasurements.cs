using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.Text;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Lexical;
using MigrateAuth.Cli.Tests.Catalog;

namespace MigrateAuth.Cli.Tests.MatchBoundStress;

/// <summary>What the lexical-budget gate measured for one `Go` stress shape.</summary>
internal sealed record GoShippedLexicalTiming(
	string ShapeName,
	int SizeInCharacters,
	double ChargedMilliseconds,
	double WholeScanMilliseconds,
	bool Abandoned,
	bool TrippedTheBudget);

/// <summary>
/// THE MEASUREMENTS OF THE `Go` LEXICAL-BUDGET GATE, on the pattern of <see cref="PythonLexicalBudgetMeasurements"/>. The profile measured is the SHIPPED profile, the lexical profile of the registered `Go` analyzer
/// (<see cref="GoRegisteredExpressions.Analyzer"/>: three quote rules, the interpreted string, the rune literal and the raw string, two comment rules, the line comment and the block comment, and no predicate, no prefix and no
/// hole). A `Go` file has no first-line rule and the catalog declares no extension beyond <c>.go</c>, so there is no first-line expression to time. What is measured, per stress shape and each the worst of the measured
/// iterations: the time the lexical budget is charged, read from <see cref="ChargeRecordingTimeProvider"/> through a scan (a `Go` profile declares no here-document opening and no interpolation extent, so the figure is what the
/// budget watch is asked to charge and is expected to be nothing); and the whole scan, for context.
///
/// THE SHAPE THE DECISION NAMES is the identifier-dense file at the 2 MiB ceiling with lines at the 10,000-character ceiling (the first of <see cref="BuildShapes"/>). Beside it, the shapes that put each rule to work: the three
/// literals closed and never closed (a raw string that opens and never closes carries the rest of the file, an interpreted string or a rune that never closes is recovered at the line end), the escapes of the two literals
/// that have one, the line comments that hold the openers of the other rules, the block comments (closed, and one that never closes and abandons the file), the division operator against the comment openers, and ordinary code.
/// </summary>
internal static class GoLexicalBudgetMeasurements
{
	/// <summary>The name of the shape the decision names: identifier-dense, 2 MiB, lines at the ceiling.</summary>
	internal const string IDENTIFIER_DENSE_SHAPE_NAME = "go-lexical-identifier-dense-at-the-ceiling";

	internal const string SHAPE_NAME_PREFIX = "go-lexical-";

	internal const int WARM_UP_ITERATIONS = 1;

	internal const int MEASURED_ITERATIONS = 3;

	private const int _ABOUT_EIGHTY_LINE_LENGTH_IN_CHARACTERS = 78;
	private const int _BUILDER_SLACK_IN_CHARACTERS = 4096;
	private const string _LINE_BREAK = "\n";

	private const string _IDENTIFIER_DENSE_GROUP =
		"reader writer buffer builder filter factory transform total result return unit upper request response format fetch bytes record route runeValue rawText quoteCount ";

	private static readonly ImmutableArray<(string Slug, string Group)> _LONG_LINE_SHAPES =
	[
		("interpreted-strings-closed", "\"a b\" \"c\\\"d\" \"e\\\\\" \"\" \"f g h i\" "),
		("interpreted-strings-with-escapes", "\"\\\"\\\"\\\"\\\\\\\"\\n\\t\\x41\\u00e9\" "),
		("rune-literals", "'a' '\\'' '\"' '/' '\\\\' '`' "),
		("raw-strings-closed", "`a \\ b` `c // d` `e /* f */` `` "),
		("quote-characters-inside-the-other-literals", "\"it's `raw`\" '\"' `say \"hi\" it's` "),
		("comment-openers-inside-literals", "\"// not a comment\" `/* not a comment */` '/' "),
		("line-comments-holding-the-other-openers", "a := 1 // it's a \"comment\" with `ticks` and /* an opener\n"),
		("block-comments-closed", "/* a block comment with \"quotes\" and `ticks` and // slashes */ "),
		("block-comment-closers-without-an-opening", "*/ */ */ a */ "),
		("division-against-the-comment-openers", "a/b/c/d/e/f/g/h/i/j/k/l/m/n/o/p "),
		("slash-star-runs", "/*/*/*/*/*/*/*/*/*/*/*/*/*/*/*/*/ ")
	];

	private static readonly ImmutableArray<(string Slug, string Line)> _CEILING_LINE_SHAPES =
	[
		("interpreted-strings-never-closed", "x := \"unclosed interpreted string, recovered at the line end\n"),
		("rune-literals-never-closed", "x := 'unclosed rune literal, recovered at the line end\n"),
		("escaped-line-feeds-in-literals", "x := \"a line that ends with an escape\\\n"),
		("backslashes-before-the-closing-back-quote", "x := `a backslash before the closing back quote\\` + y\n"),
		("hash-and-percent-words-near-the-quote-rules", "x := fmt.Sprintf(\"%s #%d {%v}\", a, b, c) // note\n"),
		("generated-table-rows", "\t{\"entry00001\", 1, 0x0001, \"value 00001\"},\n")
	];

	private static readonly ImmutableArray<string> _ORDINARY_LINES =
	[
		"package sample",
		"",
		"func total(first int, second int) int {",
		"\treturn first + second",
		"}",
		"",
		"type Reader struct {",
		"\tbuffer []byte // the raw bytes",
		"\tlabel  string `json:\"label\"`",
		"}",
		"",
		"var result = map[string]int{\"name\": 1, \"items\": 2}"
	];

	/// <summary>The stress shapes of the scan: the identifier-dense file first, then the shapes that put each rule to work.</summary>
	internal static ImmutableArray<MatchBoundStressShape> BuildShapes()
	{
		ImmutableArray<MatchBoundStressShape>.Builder shapes = ImmutableArray.CreateBuilder<MatchBoundStressShape>();

		shapes.Add(Shape(IDENTIFIER_DENSE_SHAPE_NAME, LongLines(_IDENTIFIER_DENSE_GROUP)));
		shapes.Add(Shape($"{SHAPE_NAME_PREFIX}identifier-dense-lines-of-about-eighty-characters", FillToCeilingWithLines(string.Concat(_IDENTIFIER_DENSE_GROUP.AsSpan(0, _ABOUT_EIGHTY_LINE_LENGTH_IN_CHARACTERS), _LINE_BREAK))));

		foreach ((string slug, string group) in _LONG_LINE_SHAPES)
		{
			shapes.Add(Shape($"{SHAPE_NAME_PREFIX}{slug}", LongLines(group)));
		}

		foreach ((string slug, string line) in _CEILING_LINE_SHAPES)
		{
			shapes.Add(Shape($"{SHAPE_NAME_PREFIX}{slug}", FillToCeilingWithLines(line)));
		}

		shapes.Add(Shape($"{SHAPE_NAME_PREFIX}raw-string-opened-and-never-closed", $"x := `{FillToCeilingWithLines("a line inside a raw string that never closes\n")}"));
		shapes.Add(Shape($"{SHAPE_NAME_PREFIX}block-comment-opened-and-never-closed", $"/* never closed\n{FillToCeilingWithLines("a line inside a block comment that never closes\n")}"));
		shapes.Add(Shape($"{SHAPE_NAME_PREFIX}ordinary-code-at-the-ceiling", FillToCeilingWithLines($"{string.Join(_LINE_BREAK, _ORDINARY_LINES)}{_LINE_BREAK}")));

		return shapes.ToImmutable();
	}

	internal static ImmutableArray<GoShippedLexicalTiming> MeasureEveryShape(ImmutableArray<MatchBoundStressShape> shapes)
	{
		LexicalProfile profile = GoRegisteredExpressions.Analyzer.LexicalProfile;
		ImmutableArray<GoShippedLexicalTiming> timings = [.. shapes.Select(shape => Measure(profile, shape))];

		return timings;
	}

	private static GoShippedLexicalTiming Measure(LexicalProfile profile, MatchBoundStressShape shape)
	{
		ChargeRecordingTimeProvider clock = new();
		LexicalScanner scanner = new(profile, clock);
		double worstCharged = 0.0;
		double worstScan = 0.0;
		bool abandoned = false;
		bool tripped = false;

		for (int iteration = 0; iteration < WARM_UP_ITERATIONS + MEASURED_ITERATIONS; iteration++)
		{
			clock.Reset();

			Stopwatch stopwatch = Stopwatch.StartNew();
			ZoneMask mask = scanner.Scan(shape.Text);

			stopwatch.Stop();

			abandoned |= mask.WasAbandonedMidFile;
			tripped |= mask.AnomalyReason is LexicalAnomalyReason.LexicalBudgetExceeded;

			if (iteration >= WARM_UP_ITERATIONS)
			{
				worstCharged = Math.Max(worstCharged, clock.ChargedMilliseconds);
				worstScan = Math.Max(worstScan, stopwatch.Elapsed.TotalMilliseconds);
			}
		}

		return new GoShippedLexicalTiming(shape.Name, shape.Text.Length, worstCharged, worstScan, abandoned, tripped);
	}

	private static MatchBoundStressShape Shape(string name, string text)
	{
		return new MatchBoundStressShape { Name = name, Text = text, FileExtension = GoShippedCostGateStressSet.FILE_EXTENSION };
	}

	/// <summary>Lines of the group repeated up to the line-length ceiling, each line ended by a line feed, filling the file to just under the file-size ceiling.</summary>
	private static string LongLines(string group)
	{
		StringBuilder builder = new(GoShippedCostGateStressSet.WORST_SIZE_IN_CHARACTERS + _BUILDER_SLACK_IN_CHARACTERS);

		while (builder.Length + GoShippedCostGateStressSet.LONG_LINE_LENGTH_IN_CHARACTERS < GoShippedCostGateStressSet.WORST_SIZE_IN_CHARACTERS)
		{
			int lineStart = builder.Length;

			while (builder.Length - lineStart + group.Length < GoShippedCostGateStressSet.LONG_LINE_LENGTH_IN_CHARACTERS)
			{
				builder.Append(group);
			}

			builder.Append(_LINE_BREAK);
		}

		return builder.ToString();
	}

	private static string FillToCeilingWithLines(string line)
	{
		StringBuilder builder = new(GoShippedCostGateStressSet.WORST_SIZE_IN_CHARACTERS + _BUILDER_SLACK_IN_CHARACTERS);

		while (builder.Length + line.Length < GoShippedCostGateStressSet.WORST_SIZE_IN_CHARACTERS)
		{
			builder.Append(line);
		}

		return builder.ToString();
	}

	internal static string Describe(ImmutableArray<GoShippedLexicalTiming> scans, double ruleFigure, double roundedBudget, string build)
	{
		StringBuilder report = new();

		AppendHeader(report, build);
		AppendScanRows(report, scans);
		AppendScanSummary(report, scans);
		AppendBudgetFigures(report, ruleFigure, roundedBudget);

		return report.ToString();
	}

	private static void AppendHeader(StringBuilder report, string build)
	{
		report.AppendLine(string.Format(
			CultureInfo.InvariantCulture,
			"# Lexical budget of the shipped Go profile (3 quote rules, 2 comment rules, no predicate): {0} warm-up and {1} measured iterations per shape, the worst reported; build {2}",
			WARM_UP_ITERATIONS,
			MEASURED_ITERATIONS,
			build));
		report.AppendLine(string.Format(
			CultureInfo.InvariantCulture,
			"# Environment: {0}; {1}; {2} logical processors; runtime {3}",
			Environment.OSVersion,
			System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture,
			Environment.ProcessorCount,
			Environment.Version));
	}

	private static void AppendScanRows(StringBuilder report, ImmutableArray<GoShippedLexicalTiming> scans)
	{
		report.AppendLine("shape\tcharacters\tcharged_ms\twhole_scan_ms\tabandoned\ttripped");

		foreach (GoShippedLexicalTiming scan in scans)
		{
			report.AppendLine(string.Format(CultureInfo.InvariantCulture, "{0}\t{1}\t{2:F4}\t{3:F1}\t{4}\t{5}", scan.ShapeName, scan.SizeInCharacters, scan.ChargedMilliseconds, scan.WholeScanMilliseconds, scan.Abandoned, scan.TrippedTheBudget));
		}
	}

	private static void AppendScanSummary(StringBuilder report, ImmutableArray<GoShippedLexicalTiming> scans)
	{
		GoShippedLexicalTiming slowest = scans.OrderByDescending(scan => scan.WholeScanMilliseconds).First();
		GoShippedLexicalTiming identifierDense = scans.Single(scan => scan.ShapeName == IDENTIFIER_DENSE_SHAPE_NAME);

		report.AppendLine(string.Format(
			CultureInfo.InvariantCulture,
			"Slowest whole scan {0:F1} ms on {1}; identifier-dense 2 MiB file ({2} characters): whole scan {3:F1} ms, charged {4:F4} ms",
			slowest.WholeScanMilliseconds,
			slowest.ShapeName,
			identifierDense.SizeInCharacters,
			identifierDense.WholeScanMilliseconds,
			identifierDense.ChargedMilliseconds));
	}

	private static void AppendBudgetFigures(StringBuilder report, double ruleFigure, double roundedBudget)
	{
		report.AppendLine(string.Format(
			CultureInfo.InvariantCulture,
			"Go lexical-budget figure (worst benign charged per file): {0:F4} ms; x{1} = {2:F4} ms; rounded up to a multiple of {3} ms = {4:F0} ms; budget in force = {5} ms",
			ruleFigure,
			MatchBoundTimingTests.DECLARED_SAFETY_MULTIPLE,
			ruleFigure * MatchBoundTimingTests.DECLARED_SAFETY_MULTIPLE,
			MatchBoundTimingTests.ROUNDING_STEP_IN_MILLISECONDS,
			roundedBudget,
			LexicalEnginePolicy.PROVISIONAL_LEXICAL_BUDGET_IN_MILLISECONDS));
		report.AppendLine(string.Format(
			CultureInfo.InvariantCulture,
			"Rule of the budget ({0} times the worst benign figure, rounded up to the next {1} ms, within the budget in force): {2}",
			MatchBoundTimingTests.DECLARED_SAFETY_MULTIPLE,
			MatchBoundTimingTests.ROUNDING_STEP_IN_MILLISECONDS,
			roundedBudget <= LexicalEnginePolicy.PROVISIONAL_LEXICAL_BUDGET_IN_MILLISECONDS ? "holds" : "does not hold"));
	}
}
