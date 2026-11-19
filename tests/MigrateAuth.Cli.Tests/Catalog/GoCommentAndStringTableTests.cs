using System.Collections.Immutable;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Recognition;
using MigrateAuth.Cli.Tests.Recognition;

using Xunit;

namespace MigrateAuth.Cli.Tests.Catalog;

/// <summary>
/// THE COMMENT AND STRING FORMS OF THE `Go` CATALOG, CELL BY CELL (version 0.5 of the catalog, the comment and string forms per class). Only an anchor-shaped construct inside a comment or a string body composes to Low; a bare mention gives no
/// finding. Every cell of the per-class table is pinned here: the five columns are a line comment, a block comment, a raw string body, an interpreted string body whose inner quotes are escaped, and an interpreted
/// string body whose inner literals are written with back quotes; a cell reads Low (the construct's own class at Low) or none (no finding of that class), and a column the catalog did not sweep is not written
/// and has no cell here. The classes that need a fact outside the construct (`GO-009` a declaration, `GO-013` a helper) have it written in code before the construct. The bare mentions of the second table
/// each give no finding. Every text is invented, with generic names.
/// </summary>
public sealed class GoCommentAndStringTableTests
{
	private const string _LOW = "L";
	private const string _NONE = "N";
	private const string _NOT_SWEPT = "-";
	private const int _ROW_COUNT = 15;
	private const int _CLASS_COUNT = 13;
	private const int _BACK_QUOTE_COLUMN_INDEX = 4;
	private const string _LINE_COMMENT_COLUMN = "//";
	private const string _BLOCK_COMMENT_COLUMN = "/* */";
	private const string _RAW_COLUMN = "raw";
	private const string _ESCAPED_COLUMN = "esc";
	private const string _BACK_QUOTE_COLUMN = "bq";

	private const string _HELPER_IN_CODE =
		"func basicHeader(userName string, secret string) string {\n\treturn \"Basic \" + base64.StdEncoding.EncodeToString([]byte(userName+\":\"+secret))\n}\n\n";

	private const string _DECLARATION_IN_CODE = "header := \"Basic \" + encoded\n";

	/// <summary>
	/// One row of the table: the label, the classes the row is about, the code that precedes the construct, the construct with double-quoted literals, the construct with back-quoted literals (empty where the column is not
	/// swept), and the five cells in the order of the columns.
	/// </summary>
	private sealed record Row(string Label, string[] Classes, string Prefix, string Construct, string BackQuoteConstruct, string[] Cells);

	private static readonly ImmutableArray<Row> _rows =
	[
		new("GO-001", ["GO-001"], string.Empty, "request.SetBasicAuth(userName, secret)", string.Empty, [_LOW, _LOW, _LOW, _LOW, _NOT_SWEPT]),
		new("GO-002 K1", ["GO-002"], string.Empty, "request.Header.Set(\"Authorization\", \"Basic \"+encoded)", "request.Header.Set(`Authorization`, `Basic `+encoded)", [_LOW, _LOW, _LOW, _NONE, _LOW]),
		new("GO-002 K2", ["GO-002"], string.Empty, "request.Header[\"Authorization\"] = []string{\"Basic \" + encoded}", "request.Header[`Authorization`] = []string{`Basic ` + encoded}", [_LOW, _LOW, _LOW, _NONE, _LOW]),
		new("GO-003", ["GO-003"], string.Empty, "headers := http.Header{\"Authorization\": {\"Basic \" + encoded}}", "headers := http.Header{`Authorization`: {`Basic ` + encoded}}", [_LOW, _LOW, _LOW, _NONE, _LOW]),
		new("GO-004", ["GO-004"], string.Empty, "flat := map[string]string{\"Authorization\": \"Basic \" + encoded}", "flat := map[string]string{`Authorization`: `Basic ` + encoded}", [_LOW, _LOW, _LOW, _NONE, _LOW]),
		new("GO-005", ["GO-005"], string.Empty, "response, err := http.Get(\"https://user:secret@host.example/path\")", "response, err := http.Get(`https://user:secret@host.example/path`)", [_LOW, _LOW, _LOW, _LOW, _LOW]),
		new("GO-006", ["GO-006"], string.Empty, "value := \"Basic \" + base64.StdEncoding.EncodeToString([]byte(userName + \":\" + secret))", string.Empty, [_LOW, _LOW, _LOW, _NONE, _NOT_SWEPT]),
		new("GO-007", ["GO-007"], string.Empty, "address.User = url.UserPassword(userName, secret)", string.Empty, [_LOW, _LOW, _LOW, _LOW, _NOT_SWEPT]),
		new("GO-008 call", ["GO-008"], string.Empty, "address, err := url.Parse(\"https://user:secret@host.example/path\")", "address, err := url.Parse(`https://user:secret@host.example/path`)", [_LOW, _LOW, _LOW, _NONE, _LOW]),
		new("GO-008 declaration", ["GO-008"], string.Empty, "serviceURL := \"https://user:secret@host.example/path\"", string.Empty, [_NONE, _NONE, _NONE, _NONE, _NOT_SWEPT]),
		new("GO-009", ["GO-009"], _DECLARATION_IN_CODE, "request.Header.Set(\"Authorization\", header)", "request.Header.Set(`Authorization`, header)", [_LOW, _LOW, _NONE, _NONE, _NONE]),
		new("GO-010", ["GO-010", "GO-001"], string.Empty, "func (t *basicTransport) RoundTrip(r *http.Request) { r.SetBasicAuth(userName, secret) }", string.Empty, [_LOW, _LOW, _LOW, _LOW, _NOT_SWEPT]),
		new("GO-011", ["GO-011", "GO-006"], string.Empty, "func build(userName string, secret string) string { return \"Basic \" + base64.StdEncoding.EncodeToString([]byte(userName + \":\" + secret)) }", string.Empty, [_LOW, _LOW, _LOW, _NONE, _NOT_SWEPT]),
		new("GO-012", ["GO-012"], string.Empty, "request.Header.Set(\"Authorization\", os.Getenv(\"SERVICE_AUTH_HEADER\"))", "request.Header.Set(`Authorization`, os.Getenv(`SERVICE_AUTH_HEADER`))", [_LOW, _LOW, _LOW, _NONE, _LOW]),
		new("GO-013", ["GO-013"], _HELPER_IN_CODE, "request.Header.Set(\"Authorization\", basicHeader(userName, secret))", "request.Header.Set(`Authorization`, basicHeader(userName, secret))", [_LOW, _LOW, _NONE, _NONE, _NONE])
	];

	private static readonly string[] _columns = [_LINE_COMMENT_COLUMN, _BLOCK_COMMENT_COLUMN, _RAW_COLUMN, _ESCAPED_COLUMN, _BACK_QUOTE_COLUMN];

	/// <summary>The bare mentions: a word or a prose sentence in a comment or a string that is no anchor-shaped construct, one per class.</summary>
	public static TheoryData<string> BareMentions()
	{
		return
		[
			"message := \"call SetBasicAuth to authenticate\"\n",
			"message := \"SetBasicAuth(userName, secret)\"\n",
			"// SetBasicAuth(userName, secret) is deprecated here\n",
			"// call request.SetBasicAuth to authenticate\n",
			"// SetBasicAuth is not used any more; see the bearer transport.\n",
			"// SetBasicAuth\n",
			"// Set(\"Authorization\", \"Basic \"+x)\n",
			"// we used to send an Authorization header with the Basic scheme\n",
			"// Set(\"Authorization\", \"Basic \")\n",
			"message := \"Set the Authorization header to Basic\"\n",
			"message := \"Set(\\\"Authorization\\\", \\\"Basic \\\")\"\n",
			"// Header{ is a type\n",
			"message := \"a Header{...} literal\"\n",
			"// a map[string]string literal carries the header\n",
			"// http.Get takes a URL\n",
			"message := \"http.Get takes a URL\"\n",
			"// StdEncoding.EncodeToString is used for Basic\n",
			"message := \"StdEncoding.EncodeToString is used for Basic\"\n",
			"message := \"use UserPassword to build the Userinfo\"\n",
			"// UserPassword builds a Userinfo\n",
			"// url.Parse reads a URL\n",
			"message := \"url.Parse reads a URL\"\n",
			"// serviceURL := \"https://user:secret@host.example/path\"\n",
			"// Header.Set is used\n",
			"message := \"header := \\\"Basic \\\" + encoded\"\n",
			"// os.Getenv supplies the header\n",
			"message := \"Authorization comes from os.Getenv\"\n",
			"// basicHeader builds the Authorization value\n",
			"message := \"Header.Set(basicHeader(x)) is how it was done\"\n"
		];
	}

	public static TheoryData<int, int> Cells()
	{
		TheoryData<int, int> data = [];

		for (int row = 0; row < _rows.Length; row++)
		{
			for (int column = 0; column < _columns.Length; column++)
			{
				if (_rows[row].Cells[column] != _NOT_SWEPT)
				{
					data.Add(row, column);
				}
			}
		}

		return data;
	}

	[Theory]
	[MemberData(nameof(Cells))]
	public void EveryCellOfTheCommentAndStringTableReadsLowOrNoneAsTheCatalogStates(int rowIndex, int columnIndex)
	{
		Row row = _rows[rowIndex];
		string column = _columns[columnIndex];
		string source = $"{row.Prefix}{Wrap(row, column)}";
		ImmutableArray<RecognizedConstruct> constructs = new GoRecognitionHarness().Recognize(source);
		string[] found = [.. constructs.Where(construct => row.Classes.Contains(construct.PatternClassIdentifier)).Select(construct => $"{construct.PatternClassIdentifier}:{construct.Level}").Order(StringComparer.Ordinal)];
		string[] expected = row.Cells[columnIndex] == _LOW
			? [.. row.Classes.Select(patternClass => $"{patternClass}:{ConfidenceLevel.Low}").Order(StringComparer.Ordinal)]
			: [];

		Assert.True(
			found.SequenceEqual(expected, StringComparer.Ordinal),
			$"{row.Label} in the {column} column: expected [{string.Join(", ", expected)}], got [{string.Join(", ", found)}] on [{source.Replace("\n", "\\n", StringComparison.Ordinal)}]");
	}

	[Fact]
	public void TheTableHoldsEveryClassAndEveryColumnTheCatalogSweptAndOnlyThose()
	{
		string[] classes = [.. _rows.SelectMany(row => row.Classes).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal)];

		Assert.Equal(_ROW_COUNT, _rows.Length);
		Assert.Equal(_CLASS_COUNT, classes.Length);
		Assert.All(_rows, row => Assert.Equal(_columns.Length, row.Cells.Length));
		Assert.Equal(_ROW_COUNT * _columns.Length - _rows.Sum(row => row.Cells.Count(cell => cell == _NOT_SWEPT)), Cells().Count());
		Assert.All(_rows.Where(row => row.Cells[_BACK_QUOTE_COLUMN_INDEX] != _NOT_SWEPT), row => Assert.NotEmpty(row.BackQuoteConstruct));
	}

	[Theory]
	[MemberData(nameof(BareMentions))]
	public void ABareMentionOfAnAnchorGivesNoFindingAtAnyClass(string source)
	{
		ImmutableArray<RecognizedConstruct> constructs = new GoRecognitionHarness().Recognize(source);

		Assert.True(constructs.IsEmpty, $"[{source.Replace("\n", "\\n", StringComparison.Ordinal)}] produced [{string.Join(", ", constructs.Select(construct => $"{construct.PatternClassIdentifier}:{construct.Level}"))}]");
	}

	/// <summary>The literal flag follows the zone of the capture: false in a comment, true in a string body that holds the literal, false for an escaped inner literal of `GO-001`.</summary>
	[Theory]
	[InlineData("// request.SetBasicAuth(userName, \"exampleSecret\")\n", "GO-001", false)]
	[InlineData("const note = `request.SetBasicAuth(userName, \"exampleSecret\")`\n", "GO-001", true)]
	[InlineData("message := \"request.SetBasicAuth(userName, \\\"exampleSecret\\\")\"\n", "GO-001", false)]
	[InlineData("// response, err := http.Get(\"https://user:exampleSecret@host.example/path\")\n", "GO-005", false)]
	[InlineData("/* response, err := http.Get(\"https://user:exampleSecret@host.example/path\") */\n", "GO-005", false)]
	[InlineData("const note = `response, err := http.Get(\"https://user:exampleSecret@host.example/path\")`\n", "GO-005", true)]
	[InlineData("message := \"response, err := http.Get(\\\"https://user:exampleSecret@host.example/path\\\")\"\n", "GO-005", true)]
	[InlineData("// address, err := url.Parse(\"https://user:exampleSecret@host.example/path\")\n", "GO-008", false)]
	[InlineData("const note = `address, err := url.Parse(\"https://user:exampleSecret@host.example/path\")`\n", "GO-008", true)]
	public void TheLiteralFlagOfALowCellFollowsTheZoneOfTheCapture(string source, string patternClass, bool expected)
	{
		RecognizedConstruct construct = Assert.Single(new GoRecognitionHarness().Recognize(source), candidate => candidate.PatternClassIdentifier == patternClass);

		Assert.Equal(ConfidenceLevel.Low, construct.Level);
		Assert.Equal(expected, construct.LiteralCredentialSignal);
	}

	private static string Wrap(Row row, string column)
	{
		string wrapped;

		switch (column)
		{
			case _LINE_COMMENT_COLUMN:
				wrapped = $"{string.Join("\n", row.Construct.Split('\n').Select(line => $"// {line}"))}\n";
				break;
			case _BLOCK_COMMENT_COLUMN:
				wrapped = $"/* {row.Construct} */\n";
				break;
			case _RAW_COLUMN:
				wrapped = $"const note = `{row.Construct}`\n";
				break;
			case _ESCAPED_COLUMN:
				wrapped = $"message := \"{row.Construct.Replace("\"", "\\\"", StringComparison.Ordinal)}\"\n";
				break;
			default:
				wrapped = $"message := \"{row.BackQuoteConstruct}\"\n";
				break;
		}

		return wrapped;
	}
}
