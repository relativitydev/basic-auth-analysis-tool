using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Lexical;
using MigrateAuth.Cli.Reading;
using MigrateAuth.Cli.Tests.Catalog;
using MigrateAuth.Cli.Tests.Lexical;

namespace MigrateAuth.Cli.Tests.MatchBoundStress;

/// <summary>What the lexical-budget gate measured for one `Python` stress shape.</summary>
internal sealed record PythonLexicalTiming(
	string ShapeName,
	int SizeInCharacters,
	double ChargedMilliseconds,
	double WholeScanMilliseconds,
	bool Abandoned,
	bool TrippedTheBudget);

/// <summary>One first line the draft first-line expression is timed on, and whether the line is a form the expression accepts.</summary>
internal sealed record PythonFirstLineShape(string Name, string Line, bool Accepted);

/// <summary>What the lexical-budget gate measured for one first-line shape.</summary>
internal sealed record PythonFirstLineTiming(string Name, int LengthInCharacters, double Milliseconds, bool Matched, bool Accepted);

/// <summary>
/// THE MEASUREMENTS OF THE `Python` LEXICAL-BUDGET GATE, on the pattern of <see cref="TypeScriptLexicalBudgetMeasurements"/> and <see cref="ShellLexicalBudgetMeasurements"/>. The profile measured is the
/// SHIPPED profile, the lexical profile of the registered `Python` analyzer (<see cref="PythonShippedAnalyzer"/>: 84 quote rules, the two opening-context predicates, the bracketed hole with brace nesting), and the first-line
/// expression is the shipped one, the first-line rule of the registered analyzer. The test-local draft (<see cref="PythonShapedProfile"/>) is held equal to the shipped profile by the profile-equality tests and is no longer timed. What is measured, per stress shape and each the worst of the measured iterations: the time the lexical budget is charged, read from
/// <see cref="ChargeRecordingTimeProvider"/> through a scan (a `Python` profile declares no here-document opening and no interpolation extent, so the figure is what the budget watch is asked to charge and is expected to be
/// nothing; the first-line expression is charged on attribution and is measured as its own rows); and the whole scan, for context, because the cost the lexical-budget decision names is the cost of 84 rules (about 140 index candidates
/// on the hottest buckets) on text the rules index by first character.
///
/// THE SHAPE THE DECISION NAMES is the identifier-dense file at the 2 MiB ceiling with lines at the 10,000-character ceiling (the first of <see cref="BuildShapes"/>): words that begin with the letters of the quote prefixes
/// (`r`, `b`, `u`, `f`, `t`, either case), so that the rule index offers its candidate list at nearly every position. Beside it, the shapes that put the candidate list to work: quote-prefix runs with no quote, adjacent quotes after a
/// word (the prefix predicate decides each), f-strings and template strings with nested fields, triple-quoted runs, comment-heavy text with the format-specification predecessors of the `#` predicate, and ordinary code.
/// </summary>
internal static class PythonLexicalBudgetMeasurements
{
	/// <summary>What the first lines of <see cref="PythonFirstLineStressSet"/> are named with in the timing table, beside the first lines of this class.</summary>
	internal const string STRESS_SET_SHAPE_NAME_PREFIX = "stress-set-";

	internal const int FIRST_LINE_WARM_UP_ITERATIONS = 3;

	internal const int FIRST_LINE_MEASURED_ITERATIONS = 200;

	/// <summary>The name of the shape the decision names: identifier-dense, 2 MiB, lines at the ceiling.</summary>
	internal const string IDENTIFIER_DENSE_SHAPE_NAME = "py-lexical-identifier-dense-at-the-ceiling";

	private const int _ABOUT_EIGHTY_LINE_LENGTH_IN_CHARACTERS = 78;
	private const int _HALF_LINE_DIVISOR = 2;
	private const string _SLASH_SEGMENT = "a/";
	private const string _ENVIRONMENT_FLAG = "-x ";
	private const string _INTERPRETER_NAME = "python";
	private const string _FIRST_LINE_START = "#!";
	private const string _ENVIRONMENT_FLAGS_START = "#!/usr/bin/env ";
	private const string _INTERPRETER_START = "#!/usr/bin/python3.";

	private const string _IDENTIFIER_DENSE_GROUP =
		"reader writer buffer builder filter factory transform total result return unit upper request response format fetch bytes record route rb_value br_value rf_name tr_name Rb_Value Fr_Name fb tb ub bf bu ";

	private static readonly ImmutableArray<(string Slug, string Group, bool Ceiling)> _LONG_LINE_SHAPES =
	[
		("identifier-dense-with-adjacent-quotes", "buffer\"x\" filter'y' if\"a\" rf\"{z}\" format'{w}' tr\"{v}\" bytes\"b\" ", true),
		("quote-prefixes-with-no-quote", "rbrbrbrbrfrfrfrftrtrtrtrbububububrRbRBrBfFtTuU ", true),
		("quote-prefix-openings", "rb\"a\" Rb'b' f\"{x}\" t'{y}' rf\"{z}\" Tr'{w}' BR\"q\" u'v' ", true),
		("f-strings-with-nested-fields", "f\"{a}:{b!r:>{w}} {c['k']} {{x}} {d:%Y-%m-%d} {f'{e}'}\" ", true),
		("template-strings-with-nested-fields", "t\"{a} {b:>{w}} {c['k']}\" rt'{d}{e}' ", true),
		("triple-quoted-runs", "\"\"\"a\"\"\" '''b''' f\"\"\"{c}\"\"\" ", true),
		("hash-after-format-specification-characters", "x = f\"{a:#x} {b:<#5} {c:+#} {d:=#}\"  # note\n", false),
		("comments-with-quotes", "x = 1  # it's a \"comment\" with 'quotes' and f\"{braces}\" ", true),
		("unclosed-quotes-and-prefixes", "rb\" f' t\"\"\" u' ", true),
		("brace-runs", "f\"{{{{{{{{{{{{{{{{ }}}}}}}}}}}}}}}}\" ", true)
	];

	private static readonly ImmutableArray<string> _ORDINARY_LINES =
	[
		"def total(first: int, second: int) -> int:",
		"\treturn first + second",
		"class Reader:",
		"\tdef __init__(self, buffer: bytes) -> None:",
		"\t\tself.buffer = buffer  # the raw bytes",
		"\t\tself.label = f\"reader-{len(buffer)}\"",
		"result = {\"name\": \"value\", \"items\": [1, 2, 3]}"
	];

	/// <summary>The stress shapes of the scan: the identifier-dense file first, then the shapes that put the candidate list to work.</summary>
	internal static ImmutableArray<MatchBoundStressShape> BuildShapes()
	{
		ImmutableArray<MatchBoundStressShape>.Builder shapes = ImmutableArray.CreateBuilder<MatchBoundStressShape>();

		shapes.Add(Shape(IDENTIFIER_DENSE_SHAPE_NAME, PythonDraftCostGateStressSet.LongLines(_IDENTIFIER_DENSE_GROUP)));
		shapes.Add(Shape("py-lexical-identifier-dense-lines-of-about-eighty-characters", PythonDraftCostGateStressSet.FillToCeilingWithLines(string.Concat(_IDENTIFIER_DENSE_GROUP.AsSpan(0, _ABOUT_EIGHTY_LINE_LENGTH_IN_CHARACTERS), PythonDraftCostGateStressSet.LINE_BREAK))));

		foreach ((string slug, string group, bool ceiling) in _LONG_LINE_SHAPES)
		{
			shapes.Add(Shape($"py-lexical-{slug}", ceiling ? PythonDraftCostGateStressSet.LongLines(group) : PythonDraftCostGateStressSet.FillToCeilingWithLines(group)));
		}

		shapes.Add(Shape("py-lexical-ordinary-code-at-the-ceiling", PythonDraftCostGateStressSet.FillToCeilingWithLines($"{string.Join(PythonDraftCostGateStressSet.LINE_BREAK, _ORDINARY_LINES)}{PythonDraftCostGateStressSet.LINE_BREAK}")));

		return shapes.ToImmutable();
	}

	/// <summary>The first lines the draft first-line expression is timed on: forms it accepts, and the lines at the line ceiling that put each loop of the expression to work.</summary>
	internal static ImmutableArray<PythonFirstLineShape> BuildFirstLineShapes()
	{
		string segments = string.Concat(Enumerable.Repeat(_SLASH_SEGMENT, PythonDraftCostGateStressSet.LONG_LINE_LENGTH_IN_CHARACTERS / _SLASH_SEGMENT.Length));
		string flags = string.Concat(Enumerable.Repeat(_ENVIRONMENT_FLAG, PythonDraftCostGateStressSet.LONG_LINE_LENGTH_IN_CHARACTERS / _ENVIRONMENT_FLAG.Length));
		string digits = new('1', PythonDraftCostGateStressSet.LONG_LINE_LENGTH_IN_CHARACTERS - _INTERPRETER_START.Length - 1);

		return
		[
			new PythonFirstLineShape("valid-env-with-a-flag", "#!/usr/bin/env -S python3", true),
			new PythonFirstLineShape("valid-versioned-interpreter", "#!/usr/bin/python3.12", true),
			new PythonFirstLineShape("valid-long-digit-run", $"{_INTERPRETER_START}{digits} ", true),
			new PythonFirstLineShape("refused-interpreter-with-the-s-flag", "#!/usr/bin/python -S", false),
			new PythonFirstLineShape("refused-slash-segments-at-the-line-ceiling", $"{_FIRST_LINE_START}{segments}", false),
			new PythonFirstLineShape("refused-env-flags-at-the-line-ceiling", $"{_ENVIRONMENT_FLAGS_START}{flags}", false),
			new PythonFirstLineShape("refused-blanks-at-the-line-ceiling", $"{_FIRST_LINE_START}{new string(' ', PythonDraftCostGateStressSet.LONG_LINE_LENGTH_IN_CHARACTERS)}x", false),
			new PythonFirstLineShape("refused-digit-run-then-a-letter", $"{_INTERPRETER_START}{digits}x", false),
			new PythonFirstLineShape("refused-interpreter-name-run", $"{_FIRST_LINE_START}{string.Concat(Enumerable.Repeat(_INTERPRETER_NAME, PythonDraftCostGateStressSet.LONG_LINE_LENGTH_IN_CHARACTERS / _INTERPRETER_NAME.Length))}", false),
			new PythonFirstLineShape("refused-slash-segments-then-the-s-flag", $"{_FIRST_LINE_START}{string.Concat(Enumerable.Repeat(_SLASH_SEGMENT, PythonDraftCostGateStressSet.LONG_LINE_LENGTH_IN_CHARACTERS / (_SLASH_SEGMENT.Length * _HALF_LINE_DIVISOR)))}python -S", false),
			.. PythonFirstLineStressSet.Build().Select(shape => new PythonFirstLineShape($"{STRESS_SET_SHAPE_NAME_PREFIX}{shape.Name}", shape.Line, shape.Name.StartsWith(PythonFirstLineStressSet.VALID_SHAPE_NAME_PREFIX, StringComparison.Ordinal)))
		];
	}

	internal static ImmutableArray<PythonLexicalTiming> MeasureEveryShape(ImmutableArray<MatchBoundStressShape> shapes)
	{
		LexicalProfile profile = PythonShippedAnalyzer.Instance.LexicalProfile;
		ImmutableArray<PythonLexicalTiming> timings = [.. shapes.Select(shape => Measure(profile, shape))];

		return timings;
	}

	internal static ImmutableArray<PythonFirstLineTiming> MeasureEveryFirstLine(ImmutableArray<PythonFirstLineShape> shapes)
	{
		Regex expression = PythonShippedAnalyzer.Instance.LanguageIdentification.FirstLineDetectionExpression!;
		ImmutableArray<PythonFirstLineTiming> timings = [.. shapes.Select(shape => MeasureFirstLine(expression, shape))];

		return timings;
	}

	private static PythonLexicalTiming Measure(LexicalProfile profile, MatchBoundStressShape shape)
	{
		ChargeRecordingTimeProvider clock = new();
		LexicalScanner scanner = new(profile, clock);
		double worstCharged = 0.0;
		double worstScan = 0.0;
		bool abandoned = false;
		bool tripped = false;

		for (int iteration = 0; iteration < PythonTestDraftConstants.WARM_UP_ITERATIONS + PythonTestDraftConstants.MEASURED_ITERATIONS; iteration++)
		{
			clock.Reset();

			Stopwatch stopwatch = Stopwatch.StartNew();
			ZoneMask mask = scanner.Scan(shape.Text);

			stopwatch.Stop();

			abandoned |= mask.WasAbandonedMidFile;
			tripped |= mask.AnomalyReason is LexicalAnomalyReason.LexicalBudgetExceeded;

			if (iteration >= PythonTestDraftConstants.WARM_UP_ITERATIONS)
			{
				worstCharged = Math.Max(worstCharged, clock.ChargedMilliseconds);
				worstScan = Math.Max(worstScan, stopwatch.Elapsed.TotalMilliseconds);
			}
		}

		return new PythonLexicalTiming(shape.Name, shape.Text.Length, worstCharged, worstScan, abandoned, tripped);
	}

	private static PythonFirstLineTiming MeasureFirstLine(Regex expression, PythonFirstLineShape shape)
	{
		double worst = 0.0;
		bool matched = false;

		for (int iteration = 0; iteration < FIRST_LINE_WARM_UP_ITERATIONS + FIRST_LINE_MEASURED_ITERATIONS; iteration++)
		{
			Stopwatch stopwatch = Stopwatch.StartNew();

			matched = expression.IsMatch(shape.Line);

			stopwatch.Stop();

			if (iteration >= FIRST_LINE_WARM_UP_ITERATIONS)
			{
				worst = Math.Max(worst, stopwatch.Elapsed.TotalMilliseconds);
			}
		}

		return new PythonFirstLineTiming(shape.Name, shape.Line.Length, worst, matched, shape.Accepted);
	}

	private static MatchBoundStressShape Shape(string name, string text)
	{
		return new MatchBoundStressShape { Name = name, Text = text, FileExtension = PythonTestDraftConstants.FILE_EXTENSION };
	}

	internal static string Describe(ImmutableArray<PythonLexicalTiming> scans, ImmutableArray<PythonFirstLineTiming> firstLines, double ruleFigure, double roundedBudget, string build)
	{
		StringBuilder report = new();

		AppendHeader(report, build);
		AppendScanRows(report, scans);
		AppendFirstLineRows(report, firstLines);
		AppendScanSummary(report, scans);
		AppendBudgetFigures(report, ruleFigure, roundedBudget);

		return report.ToString();
	}

	private static void AppendHeader(StringBuilder report, string build)
	{
		report.AppendLine(string.Format(
			CultureInfo.InvariantCulture,
			"# Lexical budget of the shipped-shape Python profile (84 quote rules): {0} warm-up and {1} measured iterations per shape, the worst reported; first lines {2} and {3}; build {4}",
			PythonTestDraftConstants.WARM_UP_ITERATIONS,
			PythonTestDraftConstants.MEASURED_ITERATIONS,
			FIRST_LINE_WARM_UP_ITERATIONS,
			FIRST_LINE_MEASURED_ITERATIONS,
			build));
		report.AppendLine(string.Format(
			CultureInfo.InvariantCulture,
			"# Environment: {0}; {1}; {2} logical processors; runtime {3}",
			Environment.OSVersion,
			System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture,
			Environment.ProcessorCount,
			Environment.Version));
	}

	private static void AppendScanRows(StringBuilder report, ImmutableArray<PythonLexicalTiming> scans)
	{
		report.AppendLine("shape\tcharacters\tcharged_ms\twhole_scan_ms\tabandoned\ttripped");

		foreach (PythonLexicalTiming scan in scans)
		{
			report.AppendLine(string.Format(CultureInfo.InvariantCulture, "{0}\t{1}\t{2:F4}\t{3:F1}\t{4}\t{5}", scan.ShapeName, scan.SizeInCharacters, scan.ChargedMilliseconds, scan.WholeScanMilliseconds, scan.Abandoned, scan.TrippedTheBudget));
		}
	}

	private static void AppendFirstLineRows(StringBuilder report, ImmutableArray<PythonFirstLineTiming> firstLines)
	{
		report.AppendLine("first_line\tcharacters\tms\tmatched\taccepted");

		foreach (PythonFirstLineTiming firstLine in firstLines)
		{
			report.AppendLine(string.Format(CultureInfo.InvariantCulture, "{0}\t{1}\t{2:F4}\t{3}\t{4}", firstLine.Name, firstLine.LengthInCharacters, firstLine.Milliseconds, firstLine.Matched, firstLine.Accepted));
		}
	}

	private static void AppendScanSummary(StringBuilder report, ImmutableArray<PythonLexicalTiming> scans)
	{
		PythonLexicalTiming slowest = scans.OrderByDescending(scan => scan.WholeScanMilliseconds).First();
		PythonLexicalTiming identifierDense = scans.Single(scan => scan.ShapeName == IDENTIFIER_DENSE_SHAPE_NAME);

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
			"Python lexical-budget figure (worst benign charged per file plus the worst first line): {0:F4} ms; x{1} = {2:F4} ms; rounded up to a multiple of {3} ms = {4:F0} ms; budget in force = {5} ms",
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
