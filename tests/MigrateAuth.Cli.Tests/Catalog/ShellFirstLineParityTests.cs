using System.Text;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Scanning;

using Xunit;
using Xunit.Abstractions;

namespace MigrateAuth.Cli.Tests.Catalog;

/// <summary>
/// THE SHIPPED FIRST-LINE RULE OF `Shell` AGREES WITH THE BASE SHEBANG TABLE over a generated set of first lines. The base table splits a
/// first line on any white space and reads the interpreter from the first word, or, through <c>env</c>, from the first word that is not a flag; the rule is a regular
/// expression, so the separators are where the two could differ. The set crosses every shape of a shebang with the two usual separators, and then puts EVERY
/// character of the basic plane, one at a time, in each position where a separator may stand: a vertical tab, a form feed, a no-break space and the rest of the
/// white-space characters must separate here as they do there, and every character that is not white space must not. Any divergence is resolved in the `Shell`
/// expression, never in the base table.
/// </summary>
public sealed class ShellFirstLineParityTests
{
	private const string _SEPARATOR_MARK = "{S}";

	private const int _BASIC_PLANE_SIZE = 0x10000;
	private const int _MINIMUM_LINES_OF_EACH_VERDICT = 100;
	private const int _DIVERGENCES_DESCRIBED = 10;
	private const int _CHARACTERS_DESCRIBED_PER_DIVERGENCE = 3;

	private static readonly string[] _interpreters =
	[
		"sh", "bash", "zsh", "ksh", "dash", "mksh", "ash", "fish", "csh", "pwsh", "powershell", "python", "python3", "python3.11", "perl", "bashful", "shx", "xsh", "bash5", "Bash", "SH", ""
	];

	private static readonly string[] _shapes =
	[
		"#!{I}",
		"#!/bin/{I}",
		"#!/usr/bin/{I}",
		"#!/usr/local/bin/{I}",
		"#!{S}/bin/{I}",
		"#!{S}{S}/bin/{I}",
		"#!/bin/{I}{S}-e",
		"#!/bin/{I}{S}-e{S}-u",
		"#!/bin/{I}{S}",
		"#!/bin/{I}x",
		"#!/bin/{I}/",
		"#!/bin/sh/{I}",
		"#!/usr/bin/env",
		"#!/usr/bin/env{S}{I}",
		"#!/usr/bin/env{S}{S}{I}",
		"#!/usr/bin/env{S}{I}{S}-e",
		"#!/usr/bin/env{S}-S{S}{I}",
		"#!/usr/bin/env{S}-S{S}{I}{S}-e",
		"#!/usr/bin/env{S}-i{S}-S{S}{I}",
		"#!/usr/bin/env{S}-{S}{I}",
		"#!/usr/bin/env{S}--{S}{I}",
		"#!/usr/bin/env{S}FOO=1{S}{I}",
		"#!/usr/bin/env{S}/bin/{I}",
		"#!env{S}{I}",
		"#!{S}env{S}{I}",
		"#!/usr/bin/envx{S}{I}",
		"#!/usr/bin/xenv{S}{I}",
		"# /bin/{I}",
		"#/bin/{I}",
		" #!/bin/{I}",
		"/bin/{I}",
		"!#/bin/{I}",
		"#!!/bin/{I}"
	];

	private static readonly string[] _usualSeparators = [" ", "\t", "  ", " \t"];

	private readonly ITestOutputHelper _output;

	public ShellFirstLineParityTests(ITestOutputHelper output)
	{
		_output = output;
	}

	[Fact]
	public void TheGeneratedSetOfFirstLinesHoldsBothAcceptedAndRefusedLines()
	{
		List<string> lines = [.. UsualLines()];
		int accepted = lines.Count(BaseSaysShell);

		_output.WriteLine($"{lines.Count} lines with the usual separators; the base table reads {accepted} of them as Shell.");

		Assert.True(accepted > _MINIMUM_LINES_OF_EACH_VERDICT);
		Assert.True(lines.Count - accepted > _MINIMUM_LINES_OF_EACH_VERDICT);
	}

	[Fact]
	public void TheShippedFirstLineExpressionAgreesWithTheBaseTableOverTheLinesWithTheUsualSeparators()
	{
		List<string> divergences = [];
		int total = 0;

		foreach (string line in UsualLines())
		{
			total++;

			if (BaseSaysShell(line) != ShippedSaysShell(line))
			{
				divergences.Add(line);
			}
		}

		_output.WriteLine($"{total} lines with the usual separators; {divergences.Count} divergences.");
		Assert.True(divergences.Count == 0, Describe(divergences));
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
				foreach (string interpreter in new[] { "sh", "ksh", "dash" })
				{
					string line = shape.Replace("{I}", interpreter, StringComparison.Ordinal).Replace(_SEPARATOR_MARK, separator, StringComparison.Ordinal);

					total++;

					if (BaseSaysShell(line) != ShippedSaysShell(line))
					{
						divergences.Add(line);
					}
				}
			}
		}

		_output.WriteLine($"{total} lines with a single separator character in every position; {divergences.Count} divergences.");
		Assert.True(divergences.Count == 0, Describe(divergences));
	}

	[Theory]
	[InlineData("#!/bin/sh\u000B-e")]
	[InlineData("#!/bin/sh\u000C-e")]
	[InlineData("#!/bin/sh -e")]
	[InlineData("#! /bin/bash")]
	[InlineData("#!/usr/bin/env\u000Bbash")]
	[InlineData("#!/usr/bin/env -S\u000Cbash")]
	[InlineData("#!/bin/sh\u200B")]
	[InlineData("#!/bin/sh\u001F")]
	public void TheUnusualSeparatorsAgreeWithTheBaseTable(string firstLine)
	{
		Assert.Equal(BaseSaysShell(firstLine), ShippedSaysShell(firstLine));
	}

	private static IEnumerable<string> UsualLines()
	{
		foreach (string shape in _shapes)
		{
			foreach (string interpreter in _interpreters)
			{
				foreach (string separator in _usualSeparators)
				{
					yield return shape.Replace("{I}", interpreter, StringComparison.Ordinal).Replace(_SEPARATOR_MARK, separator, StringComparison.Ordinal);
				}
			}
		}
	}

	private static bool BaseSaysShell(string firstLine)
	{
		return BaseLanguageDetection.TryGetByShebangFirstLine(firstLine, out AssessedLanguage language) && language == AssessedLanguage.Shell;
	}

	private static bool ShippedSaysShell(string firstLine)
	{
		return ShellRegisteredExpressions.Analyzer.LanguageIdentification.FirstLineDetectionExpression!.IsMatch(firstLine);
	}

	private static string Describe(List<string> divergences)
	{
		StringBuilder description = new();

		description.Append(divergences.Count).Append(" divergences; the first ones: ");

		foreach (string line in divergences.Take(_DIVERGENCES_DESCRIBED))
		{
			description.Append('[').Append(line.Replace("\0", "\\0", StringComparison.Ordinal)).Append("] (U+").Append(string.Join(",", line.Where(character => character > '~' || character < ' ').Take(_CHARACTERS_DESCRIBED_PER_DIVERGENCE).Select(character => ((int)character).ToString("X4", System.Globalization.CultureInfo.InvariantCulture)))).Append(") ");
		}

		return description.ToString();
	}
}
