using System.Collections.Immutable;
using System.Text;
using System.Text.RegularExpressions;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Recognition;
using MigrateAuth.Cli.Tests.Recognition;

using Xunit;

namespace MigrateAuth.Cli.Tests.Catalog;

/// <summary>
/// THE TEXT OF AN ARGUMENT ARRAY (`SH-009`) IS READ UP TO THE BOUND OF THE DEFINITION WINDOW.
/// The array and its credential option may stand on one physical line of up to the 10,000-character line ceiling, or across up to thirty physical lines of at most 400
/// characters, and the option may stand anywhere in it. An array longer than that, a header-only array and a use beyond the window are not read. Every script is invented
/// with placeholder names, through the shipped `Shell` analyzer and the real recognition engine.
/// </summary>
public sealed class ShellLongArrayTests
{
	private const string _CLASS = "SH-009";
	private const string _QUERY_OPTION_PAIR = " --url-query 'col=0001'";
	private const string _USE_LINE = "curl \"${fields[@]}\" \"https://bulk.example.com/api/v1/report\"\n";
	private const int _PAIRS_OF_THE_LONG_ARRAY = 399;
	private const int _PAIRS_BEFORE_THE_OPTION = 30;
	private const int _LINE_CEILING = 10000;
	private const int _MAXIMUM_LINE_LENGTH_IN_AN_ARRAY = 400;
	private const int _LONG_LINE_LENGTH = 399;
	private const int _LINES_OF_THE_LARGEST_ARRAY = 30;
	private const int _LINES_BETWEEN_A_DEFINITION_AND_A_USE_AT_THE_BOUND = 30;

	// ---- Covered: one physical line up to the ceiling, the option anywhere on it ----

	[Fact]
	public void AnArrayOnOneLineOfAboutNineThousandCharactersWithTheOptionAtItsStartIsReadAtMedium()
	{
		string source = "#!/bin/bash\nfields=(--silent --show-error -u user:pass -H 'Accept: application/json'" + Repeat(_QUERY_OPTION_PAIR, _PAIRS_OF_THE_LONG_ARRAY) + ")\n" + _USE_LINE;

		Assert.True(source.Split('\n')[1].Length > 9000);
		AssertRead(source);
	}

	[Fact]
	public void TheOptionAfterSixHundredCharactersOfOtherOptionsIsReadWhenTheUseIsOnTheFollowingLine()
	{
		string source = "fields=(--silent" + Repeat(_QUERY_OPTION_PAIR, _PAIRS_BEFORE_THE_OPTION) + " -u user:pass --show-error)\n" + _USE_LINE;

		Assert.True(source.IndexOf("-u user", StringComparison.Ordinal) > 600);
		AssertRead(source);
	}

	[Fact]
	public void AnArrayThatFitsTheOldSixHundredCharacterBoundIsStillRead()
	{
		AssertRead("fields=(--silent -u user:pass --show-error)\n" + _USE_LINE);
	}

	[Fact]
	public void AnArrayOnOneLineJustWithinTheCeilingIsRead()
	{
		string head = "fields=(-u user:pass ";
		string source = head + new string('x', _LINE_CEILING - head.Length - ")".Length) + ")\n" + _USE_LINE;

		Assert.Equal(_LINE_CEILING, source.IndexOf('\n'));
		AssertRead(source);
	}

	[Fact]
	public void AnArrayOnOneLineBeyondTheCeilingIsNotRead()
	{
		string head = "fields=(-u user:pass ";
		string source = head + new string('x', _LINE_CEILING + 1) + ")\n" + _USE_LINE;

		AssertNotRead(Expression().IsMatch(source), source);
	}

	// ---- Covered: up to thirty lines of at most 400 characters ----

	[Fact]
	public void AnArrayOfThirtyLinesWithTheOptionOnTheSecondIsRead()
	{
		AssertRead(ArrayOfLines(_LINES_OF_THE_LARGEST_ARRAY, "\t-u user:pass") + _USE_LINE);
	}

	[Fact]
	public void AnArrayOfThirtyLinesWithTheOptionOnTheLastLineBeforeTheClosingIsRead()
	{
		AssertRead(ArrayOfLines(_LINES_OF_THE_LARGEST_ARRAY, null, lastLine: "\t-u user:pass") + _USE_LINE);
	}

	[Fact]
	public void AnArrayWhoseClosingParenthesisIsOnTheThirtyFirstLineIsNotRead()
	{
		AssertNotRead(Expression().IsMatch(ArrayOfLines(_LINES_OF_THE_LARGEST_ARRAY + 1, "\t-u user:pass") + _USE_LINE), "thirty-one lines");
	}

	[Fact]
	public void AnArrayWithALineOfMoreThanFourHundredCharactersAcrossSeveralLinesIsNotRead()
	{
		string wideLine = "\t" + new string('x', _MAXIMUM_LINE_LENGTH_IN_AN_ARRAY + 1);
		string source = "fields=(\n\t-u user:pass\n" + wideLine + "\n)\n" + _USE_LINE;

		AssertNotRead(Expression().IsMatch(source), "a wide line");
	}

	// ---- Controls that keep their outcome ----

	[Fact]
	public void AnArrayHoldingOnlyAnAcceptHeaderAndLongQueryOptionsIsNotRead()
	{
		string source = "fields=(-H 'Accept: application/json'" + Repeat(_QUERY_OPTION_PAIR, _PAIRS_OF_THE_LONG_ARRAY) + ")\n" + _USE_LINE;

		AssertNotRead(Expression().IsMatch(source), "a header-only array");
	}

	[Fact]
	public void AUseThirtyOneLinesAfterTheArrayIsNotRead()
	{
		string source = "fields=(" + Repeat(_QUERY_OPTION_PAIR, _PAIRS_OF_THE_LONG_ARRAY) + " -u user:pass)\n" + Filler(_LINES_BETWEEN_A_DEFINITION_AND_A_USE_AT_THE_BOUND + 1) + _USE_LINE;

		AssertNotRead(Expression().IsMatch(source), "a distant use");
	}

	[Fact]
	public void AUseAtTheBoundOfTheDefinitionWindowIsRead()
	{
		string source = "fields=(" + Repeat(_QUERY_OPTION_PAIR, _PAIRS_OF_THE_LONG_ARRAY) + " -u user:pass)\n" + Filler(_LINES_BETWEEN_A_DEFINITION_AND_A_USE_AT_THE_BOUND - 1) + _USE_LINE;

		AssertRead(source);
	}

	[Fact]
	public void AnArrayOfQueryOptionsWithNoCredentialOptionIsNotRead()
	{
		string source = "fields=(--silent" + Repeat(_QUERY_OPTION_PAIR, _PAIRS_OF_THE_LONG_ARRAY) + ")\n" + _USE_LINE;

		AssertNotRead(Expression().IsMatch(source), "no credential option");
	}

	[Fact]
	public void AFileOfUnfinishedArrayLinesCostsNothingAndMatchesNothing()
	{
		string source = Repeat("x=(\n", 5000) + _USE_LINE;

		AssertNotRead(Expression().IsMatch(source), "unfinished arrays");
	}

	// ---- Helpers ----

	private static void AssertRead(string source)
	{
		ImmutableArray<RecognizedConstruct> constructs = new ShellRecognitionHarness().Recognize(source);

		Assert.True(
			constructs.Any(construct => construct.PatternClassIdentifier == _CLASS && construct.Level == ConfidenceLevel.Medium),
			$"{_CLASS} did not fire at Medium; got [{string.Join(", ", constructs.Select(construct => $"{construct.PatternClassIdentifier}:{construct.Level}"))}]");
	}

	private static void AssertNotRead(bool matched, string what)
	{
		Assert.False(matched, $"{_CLASS} matched {what}.");
	}

	private static Regex Expression()
	{
		return ShellRegisteredExpressions.Analyzer.DetectionRulesByPatternClass[_CLASS].Expression;
	}

	/// <summary>An array of the given number of lines, the first being the one that opens it and the last the one that closes it, each of the others 399 characters.</summary>
	private static string ArrayOfLines(int lines, string? secondLine, string? lastLine = null)
	{
		StringBuilder builder = new();

		builder.Append("fields=(\n");

		for (int line = 2; line < lines; line++)
		{
			builder.Append(line == 2 && secondLine is not null ? secondLine : new string('x', _LONG_LINE_LENGTH)).Append('\n');
		}

		builder.Append(lastLine ?? string.Empty).Append(")\n");

		return builder.ToString();
	}

	private static string Repeat(string text, int times)
	{
		return string.Concat(Enumerable.Repeat(text, times));
	}

	private static string Filler(int lines)
	{
		return Repeat("echo filler\n", lines);
	}
}
