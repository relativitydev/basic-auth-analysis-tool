using System.Text.RegularExpressions;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Catalog.Languages;

using Xunit;
using Xunit.Abstractions;

namespace MigrateAuth.Cli.Tests.Catalog;

/// <summary>
/// THE FOUR FIRST-LINE EXPRESSIONS ARE DISJOINT. Attribution of an extensionless file is by the earliest declared language name, ordinal (`JavaScript`, `PowerShell`, `Python`, `Shell`), first claim wins; that order
/// is only a tie-break, and the catalog requires that the four expressions never claim the same line, so the order decides nothing. The set crosses every shape of a shebang with the interpreters of the four
/// languages and with near misses of each name, with the usual separators.
/// </summary>
public sealed class PythonFirstLineDisjointnessTests
{
	private const string _INTERPRETER_MARK = "{I}";
	private const string _SEPARATOR_MARK = "{S}";
	private const string _JAVASCRIPT_LANGUAGE_NAME = "JavaScript";
	private const string _POWERSHELL_LANGUAGE_NAME = "PowerShell";
	private const string _SHELL_LANGUAGE_NAME = "Shell";
	private const int _MINIMUM_LINES_CLAIMED_BY_EACH = 20;
	private const int _LINES_DESCRIBED = 10;

	private static readonly string[] _interpreters =
	[
		"python", "python2", "python3", "python3.11", "python2.7", "python3.", "pypy", "Python3",
		"node", "nodejs", "node18", "deno", "bun",
		"pwsh", "powershell", "pwsh7", "Powershell",
		"bash", "sh", "zsh", "ksh", "dash", "fish", "bashful", "perl", "ruby", ""
	];

	private static readonly string[] _shapes =
	[
		"#!{I}",
		"#!/usr/bin/{I}",
		"#!/usr/local/bin/{I}",
		"#!{S}/usr/bin/{I}",
		"#!/usr/bin/{I}{S}-e",
		"#!/usr/bin/{I}{S}-S",
		"#!/usr/bin/{I}{S}",
		"#!/usr/bin/env{S}{I}",
		"#!/usr/bin/env{S}-S{S}{I}",
		"#!/usr/bin/env{S}-S{S}{I}{S}-S",
		"#!/usr/bin/env{S}-i{S}-S{S}{I}",
		"#!/usr/bin/env{S}VAR=1{S}{I}",
		"#!env{S}{I}",
		"/usr/bin/{I}",
		"# /usr/bin/{I}"
	];

	private static readonly string[] _separators = [" ", "\t", "  ", " \t"];

	private readonly ITestOutputHelper _output;

	public PythonFirstLineDisjointnessTests(ITestOutputHelper output)
	{
		_output = output;
	}

	[Fact]
	public void NoGeneratedFirstLineIsClaimedByTwoOfTheFourExpressions()
	{
		(string Name, Regex Expression)[] expressions = TheFourExpressions();
		List<string> doubleClaims = [];
		int total = 0;

		foreach (string line in TheLines())
		{
			total++;

			string[] claimants = [.. expressions.Where(entry => entry.Expression.IsMatch(line)).Select(entry => entry.Name)];

			if (claimants.Length > 1)
			{
				doubleClaims.Add($"[{line}] by {string.Join(" and ", claimants)}");
			}
		}

		_output.WriteLine($"{total} lines; {doubleClaims.Count} claimed by more than one expression.");
		Assert.True(doubleClaims.Count == 0, string.Join("; ", doubleClaims.Take(_LINES_DESCRIBED)));
	}

	[Fact]
	public void EachOfTheFourExpressionsClaimsLinesOfItsOwnInTheSameSet()
	{
		(string Name, Regex Expression)[] expressions = TheFourExpressions();
		List<string> lines = [.. TheLines()];

		foreach ((string name, Regex expression) in expressions)
		{
			int claimed = lines.Count(expression.IsMatch);

			_output.WriteLine($"{name} claims {claimed} of {lines.Count} lines.");
			Assert.True(claimed >= _MINIMUM_LINES_CLAIMED_BY_EACH, $"{name} claims only {claimed} lines of the set, so the disjointness check proves nothing about it.");
		}
	}

	[Theory]
	[InlineData("#!/usr/bin/env python3", PythonShippedAnalyzer.LANGUAGE_NAME)]
	[InlineData("#!/usr/bin/python", PythonShippedAnalyzer.LANGUAGE_NAME)]
	[InlineData("#!/usr/bin/env node", _JAVASCRIPT_LANGUAGE_NAME)]
	[InlineData("#!/usr/bin/env pwsh", _POWERSHELL_LANGUAGE_NAME)]
	[InlineData("#!/bin/bash", _SHELL_LANGUAGE_NAME)]
	[InlineData("#!/bin/sh", _SHELL_LANGUAGE_NAME)]
	public void ALineOfOneLanguageIsClaimedByThatLanguageAlone(string firstLine, string expectedClaimant)
	{
		string[] claimants = [.. TheFourExpressions().Where(entry => entry.Expression.IsMatch(firstLine)).Select(entry => entry.Name)];

		Assert.Equal([expectedClaimant], claimants);
	}

	private static (string Name, Regex Expression)[] TheFourExpressions()
	{
		string[] names = [_JAVASCRIPT_LANGUAGE_NAME, _POWERSHELL_LANGUAGE_NAME, PythonShippedAnalyzer.LANGUAGE_NAME, _SHELL_LANGUAGE_NAME];
		ILanguageAnalyzer[] analyzers = [.. EmbeddedLanguageAnalyzers.CreateAll()];

		return
		[
			.. names.Select(name => (
				name,
				analyzers.Single(analyzer => analyzer.LanguageIdentification.DeclaredLanguageNames.Contains(name)).LanguageIdentification.FirstLineDetectionExpression!))
		];
	}

	private static IEnumerable<string> TheLines()
	{
		foreach (string shape in _shapes)
		{
			foreach (string interpreter in _interpreters)
			{
				foreach (string separator in _separators)
				{
					yield return shape.Replace(_INTERPRETER_MARK, interpreter, StringComparison.Ordinal).Replace(_SEPARATOR_MARK, separator, StringComparison.Ordinal);
				}
			}
		}
	}
}
