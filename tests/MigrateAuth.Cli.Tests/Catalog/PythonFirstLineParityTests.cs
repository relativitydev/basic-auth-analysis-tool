using System.Globalization;
using System.Text;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Scanning;

using Xunit;
using Xunit.Abstractions;

namespace MigrateAuth.Cli.Tests.Catalog;

/// <summary>
/// THE SHIPPED FIRST-LINE RULE OF `Python` AGREES WITH THE BASE SHEBANG TABLE over a generated set of first lines, except where the catalog declares that it does not. The base table splits a first line on any white
/// space and reads the interpreter from the first word, or, through <c>env</c>, from the first word that is not a flag; the rule is a regular expression, so the separators are where the two could differ. The set is the
/// grammar prefix x path x env form x interpreter x terminator of the catalog, and then every character of the basic plane, one at a time, in each position where a separator may stand.
///
/// THE ONE DELIBERATE DISAGREEMENT. The base table reads the first word and attributes <c>#!/usr/bin/python -S</c> to `Python`; the expression refuses an interpreter that is followed by blanks and the flag
/// <c>-S</c>, so the file is counted and not assessed. Every line whose terminator is that flag is pinned as "base accepts, expression refuses" where the base accepts it and as "both refuse" where it does not.
/// <c>env -S python3</c> (the flag before the interpreter) is not an exception and is pinned as "both accept". The forms <c>python2.{digits}</c>, <c>pypy</c> and <c>env VAR=1 python</c> are declared: the base
/// refuses them and so does the expression. Any other disagreement is a failure, and it is resolved in the expression, never in the base table.
/// </summary>
public sealed class PythonFirstLineParityTests
{
	private const string _SEPARATOR_MARK = "{S}";
	private const string _INTERPRETER_MARK = "{I}";

	private const int _BASIC_PLANE_SIZE = 0x10000;
	private const int _MINIMUM_LINES_OF_EACH_VERDICT = 100;
	private const int _DIVERGENCES_DESCRIBED = 10;
	private const int _CHARACTERS_DESCRIBED_PER_DIVERGENCE = 3;

	private const string _FLAG_AFTER_THE_INTERPRETER = " -S";
	private const string _FLAG_AFTER_THE_INTERPRETER_WITH_AN_ARGUMENT = " -S extra";

	private static readonly string[] _prefixes = ["#!", "#! ", "#!\t"];

	private static readonly string[] _paths = ["", "/usr/bin/", "/usr/local/bin/", "./", "/usr/./bin/"];

	private static readonly string[] _envForms = ["", "env ", "env -S ", "env -i -S ", "env VAR=1 "];

	private static readonly string[] _interpreters =
	[
		"python", "python2", "python3", "python3.9", "python3.12", "python3.", "python3.x", "python3.1a", "python2.7", "python30", "pypy", "pypy3", "Python3", "PYTHON", "python3.٣", "pythonx", ""
	];

	private static readonly string[] _terminators =
	[
		"", " ", "\t", "\r", _FLAG_AFTER_THE_INTERPRETER, _FLAG_AFTER_THE_INTERPRETER_WITH_AN_ARGUMENT, " -e", " -Sx", "\f", "\v", " ", " "
	];

	/// <summary>The separator positions of the single-character sweep: before the path, between the flag and the interpreter, after the interpreter, and before and after a flag on the interpreter.</summary>
	private static readonly string[] _shapes =
	[
		"#!{I}",
		"#!/bin/{I}",
		"#!/usr/bin/{I}",
		"#!{S}/bin/{I}",
		"#!{S}{S}/bin/{I}",
		"#!/bin/{I}{S}",
		"#!/bin/{I}{S}-u",
		"#!/bin/{I}{S}-S",
		"#!/bin/{I}{S}-S{S}x",
		"#!/bin/{I}x",
		"#!/bin/{I}/",
		"#!/usr/bin/env",
		"#!/usr/bin/env{S}{I}",
		"#!/usr/bin/env{S}{S}{I}",
		"#!/usr/bin/env{S}{I}{S}-u",
		"#!/usr/bin/env{S}-S{S}{I}",
		"#!/usr/bin/env{S}-i{S}-S{S}{I}",
		"#!/usr/bin/env{S}-{S}{I}",
		"#!/usr/bin/env{S}VAR=1{S}{I}",
		"#!env{S}{I}",
		"#!{S}env{S}{I}",
		"#!/usr/bin/envx{S}{I}",
		"# /bin/{I}",
		" #!/bin/{I}",
		"!#/bin/{I}"
	];

	private static readonly string[] _sweptInterpreters = ["python", "python3", "python3.11", "python2"];

	private readonly ITestOutputHelper _output;

	public PythonFirstLineParityTests(ITestOutputHelper output)
	{
		_output = output;
	}

	[Fact]
	public void TheGeneratedGridHoldsBothAcceptedAndRefusedLines()
	{
		List<FirstLineCase> cases = [.. TheGrid()];
		int accepted = cases.Count(testCase => BaseSaysPython(testCase.Line));

		_output.WriteLine($"{cases.Count} lines in the grid; the base table reads {accepted} of them as Python.");

		Assert.True(accepted > _MINIMUM_LINES_OF_EACH_VERDICT);
		Assert.True(cases.Count - accepted > _MINIMUM_LINES_OF_EACH_VERDICT);
	}

	[Fact]
	public void TheShippedFirstLineExpressionAgreesWithTheBaseTableOverTheGridExceptForTheFlagAfterTheInterpreter()
	{
		List<string> divergences = [];
		int compared = 0;

		foreach (FirstLineCase testCase in TheGrid().Where(candidate => !candidate.EndsWithTheDeclaredFlag))
		{
			compared++;

			if (BaseSaysPython(testCase.Line) != ShippedSaysPython(testCase.Line))
			{
				divergences.Add(testCase.Line);
			}
		}

		_output.WriteLine($"{compared} lines compared; {divergences.Count} divergences.");
		Assert.True(divergences.Count == 0, Describe(divergences));
	}

	[Fact]
	public void TheShippedFirstLineExpressionRefusesEveryLineThatEndsInTheFlagAfterTheInterpreterAndTheBaseTableAcceptsTheRealInterpreters()
	{
		List<string> accepted = [];
		int baseAccepted = 0;

		foreach (FirstLineCase testCase in TheGrid().Where(candidate => candidate.EndsWithTheDeclaredFlag))
		{
			if (ShippedSaysPython(testCase.Line))
			{
				accepted.Add(testCase.Line);
			}

			if (BaseSaysPython(testCase.Line))
			{
				baseAccepted++;
			}
		}

		_output.WriteLine($"{baseAccepted} lines ending in the flag are attributed by the base table; the expression accepts {accepted.Count} of the flagged lines.");

		Assert.True(accepted.Count == 0, Describe(accepted));
		Assert.True(baseAccepted > _MINIMUM_LINES_OF_EACH_VERDICT);
	}

	[Theory]
	[InlineData("#!/usr/bin/python -S")]
	[InlineData("#!/usr/bin/python2 -S")]
	[InlineData("#!/usr/bin/python3 -S")]
	[InlineData("#!/usr/bin/python3.11 -S")]
	[InlineData("#!/usr/bin/env python -S")]
	[InlineData("#!/usr/bin/env python3 -S")]
	[InlineData("#!/usr/bin/env python3.9 -S")]
	[InlineData("#!/usr/bin/python3 -S extra")]
	[InlineData("#!/usr/bin/env -S python3 -S")]
	public void TheFlagAfterTheInterpreterIsTheDeclaredExceptionTheBaseTableAcceptsAndTheExpressionRefuses(string firstLine)
	{
		Assert.True(BaseSaysPython(firstLine));
		Assert.False(ShippedSaysPython(firstLine));
	}

	[Theory]
	[InlineData("#!/usr/bin/env -S python3")]
	[InlineData("#!/usr/bin/env -S python3 --flag")]
	[InlineData("#!/usr/bin/env -i -S python")]
	[InlineData("#! /usr/bin/env python")]
	[InlineData("#!python3")]
	[InlineData("#!/usr/bin/python3 -Sx")]
	public void TheFlagBeforeTheInterpreterAndALongerFlagAreNotTheExceptionAndBothAccept(string firstLine)
	{
		Assert.True(BaseSaysPython(firstLine));
		Assert.True(ShippedSaysPython(firstLine));
	}

	[Theory]
	[InlineData("#!/usr/bin/python2.7")]
	[InlineData("#!/usr/bin/env python2.7")]
	[InlineData("#!/usr/bin/pypy")]
	[InlineData("#!/usr/bin/pypy3")]
	[InlineData("#!/usr/bin/env pypy")]
	[InlineData("#!/usr/bin/env VAR=1 python")]
	[InlineData("#!/usr/bin/env VAR=1 python3")]
	public void TheDeclaredFormsAreRefusedByTheBaseTableAndByTheExpression(string firstLine)
	{
		Assert.False(BaseSaysPython(firstLine));
		Assert.False(ShippedSaysPython(firstLine));
	}

	[Fact]
	public void TheShippedFirstLineExpressionAgreesWithTheBaseTableForEveryCharacterOfTheBasicPlaneInEverySeparatorPosition()
	{
		List<string> divergences = [];
		int total = 0;

		for (int code = 0; code < _BASIC_PLANE_SIZE; code++)
		{
			string separator = ((char)code).ToString();

			foreach (string shape in _shapes)
			{
				foreach (string interpreter in _sweptInterpreters)
				{
					string line = shape.Replace(_INTERPRETER_MARK, interpreter, StringComparison.Ordinal).Replace(_SEPARATOR_MARK, separator, StringComparison.Ordinal);

					total++;

					if (BaseSaysPython(line) != ShippedSaysPython(line) && !IsTheDeclaredFlagException(line))
					{
						divergences.Add(line);
					}
				}
			}
		}

		_output.WriteLine($"{total} lines with a single separator character in every position; {divergences.Count} divergences.");
		Assert.True(divergences.Count == 0, Describe(divergences));
	}

	/// <summary>A divergence of the sweep is the declared one only when the base table accepts the line, the expression refuses it and the line has the flag after the interpreter, a blank and then <c>-S</c>.</summary>
	private static bool IsTheDeclaredFlagException(string line)
	{
		return BaseSaysPython(line) && !ShippedSaysPython(line) && line.Contains("-S", StringComparison.Ordinal) && EndsTheInterpreterWithTheFlag(line);
	}

	private static bool EndsTheInterpreterWithTheFlag(string line)
	{
		string[] tokens = line["#!".Length..].Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
		int interpreterIndex = Array.FindIndex(tokens, token => token.Split('/')[^1].StartsWith("python", StringComparison.Ordinal));

		return interpreterIndex >= 0 && interpreterIndex + 1 < tokens.Length && tokens[interpreterIndex + 1] == "-S";
	}

	private static IEnumerable<FirstLineCase> TheGrid()
	{
		foreach (string prefix in _prefixes)
		{
			foreach (string path in _paths)
			{
				foreach (string envForm in _envForms)
				{
					foreach (string interpreter in _interpreters)
					{
						foreach (string terminator in _terminators)
						{
							yield return new FirstLineCase(prefix + path + envForm + interpreter + terminator, terminator);
						}
					}
				}
			}
		}
	}

	private static bool BaseSaysPython(string firstLine)
	{
		return BaseLanguageDetection.TryGetByShebangFirstLine(firstLine, out AssessedLanguage language) && language == AssessedLanguage.Python;
	}

	private static bool ShippedSaysPython(string firstLine)
	{
		return PythonShippedAnalyzer.Instance.LanguageIdentification.FirstLineDetectionExpression!.IsMatch(firstLine);
	}

	private static string Describe(List<string> divergences)
	{
		StringBuilder description = new();

		description.Append(divergences.Count).Append(" divergences; the first ones: ");

		foreach (string line in divergences.Take(_DIVERGENCES_DESCRIBED))
		{
			description.Append('[').Append(line.Replace("\0", "\\0", StringComparison.Ordinal)).Append("] (U+").Append(string.Join(",", line.Where(character => character > '~' || character < ' ').Take(_CHARACTERS_DESCRIBED_PER_DIVERGENCE).Select(character => ((int)character).ToString("X4", CultureInfo.InvariantCulture)))).Append(") ");
		}

		return description.ToString();
	}

	private sealed record FirstLineCase(string Line, string Terminator)
	{
		/// <summary>True when the terminator is the flag <c>-S</c> after the interpreter, alone or with an argument after it.</summary>
		internal bool EndsWithTheDeclaredFlag => Terminator is _FLAG_AFTER_THE_INTERPRETER or _FLAG_AFTER_THE_INTERPRETER_WITH_AN_ARGUMENT;
	}
}
