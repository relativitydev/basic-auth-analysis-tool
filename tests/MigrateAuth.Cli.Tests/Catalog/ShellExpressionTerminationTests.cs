using System.Collections.Immutable;
using System.Globalization;
using System.Text.RegularExpressions;

using MigrateAuth.Cli.Catalog;

using Xunit;

namespace MigrateAuth.Cli.Tests.Catalog;

/// <summary>
/// THE TERMINATION AND AGREEMENT CHECKS OF THE `Shell` EXPRESSIONS, run over the SHIPPED expressions of the analyzer (<see cref="ShellRegisteredExpressions"/>), on
/// the pattern of <see cref="VBNetExpressionTerminationTests"/>. The interpreted matcher is built from the pattern text of each shipped expression, so what is
/// proved is about the binary and not about a copy.
///
/// THE CHECKS. (a) Every expression DECLARES THE TIMEOUT OF THE POLICY that evaluates it: the lexical budget for the openings and the first-line rule, the match bound
/// for every other. (b) Every expression TERMINATES on the worst-case inputs of its own shapes, at the line-length ceiling, without raising the timeout. (c) Every
/// expression AGREES between the generator and the interpreter on the same pattern, in the index, length and groups of every match, over the positive fixtures, the
/// near-misses and the worst-case texts. (d) A structural scan states that no pattern wraps a lazy bounded loop in an optional group, the shape the source generator
/// mishandles.
/// </summary>
public sealed class ShellExpressionTerminationTests
{
	private const int _MATCH_ENUMERATION_CAP = 5_000;

	private const int _LINE_LENGTH_CEILING = 9_900;
	private const int _FEW_LINE_REPETITIONS = 3;
	private const int _SOME_LINE_REPETITIONS = 40;
	private const int _MANY_LINE_REPETITIONS = 400;
	private const int _LONG_TEXT_LENGTH_CEILING = 3_000;
	private const int _EXPRESSION_ANALYSIS_TIMEOUT_SECONDS = 5;
	private const int _WORST_CASE_REPETITIONS = 1_500;

	private static readonly ImmutableArray<string> _lexicalExpressions =
	[
		ShellRegisteredExpressions.PLAIN_OPENING_IDENTIFIER,
		ShellRegisteredExpressions.TAB_STRIPPING_OPENING_IDENTIFIER,
		ShellRegisteredExpressions.FIRST_LINE_IDENTIFIER
	];

	/// <summary>Expression, text, the group to read ("-" for none), and the expected value of the group, its captures joined as the scanner joins them ("NOMATCH" for no match). A match is searched from the start of the text.</summary>
	private static readonly ImmutableArray<(string Expression, string Text, string Group, string Expected)> _fixtures =
	[
		(ShellRegisteredExpressions.PLAIN_OPENING_IDENTIFIER, "<<EOF", "terminator", "EOF"),
		(ShellRegisteredExpressions.PLAIN_OPENING_IDENTIFIER, "<< EOF", "terminator", "EOF"),
		(ShellRegisteredExpressions.PLAIN_OPENING_IDENTIFIER, "<<'EOF'", "terminator", "EOF"),
		(ShellRegisteredExpressions.PLAIN_OPENING_IDENTIFIER, "<<\"EOF\"", "terminator", "EOF"),
		(ShellRegisteredExpressions.PLAIN_OPENING_IDENTIFIER, "<<\\EOF", "terminator", "EOF"),
		(ShellRegisteredExpressions.PLAIN_OPENING_IDENTIFIER, "<<E\"O\"F", "terminator", "EOF"),
		(ShellRegisteredExpressions.PLAIN_OPENING_IDENTIFIER, "<<$'EOF'", "ansic", "EOF"),
		(ShellRegisteredExpressions.PLAIN_OPENING_IDENTIFIER, "<<''", "terminator", ""),
		(ShellRegisteredExpressions.PLAIN_OPENING_IDENTIFIER, "<<EOF; echo x", "terminator", "EOF"),
		(ShellRegisteredExpressions.PLAIN_OPENING_IDENTIFIER, "<<<EOF", "-", "NOMATCH"),
		(ShellRegisteredExpressions.PLAIN_OPENING_IDENTIFIER, "<<-EOF", "-", "NOMATCH"),
		(ShellRegisteredExpressions.PLAIN_OPENING_IDENTIFIER, "<<", "-", "NOMATCH"),
		(ShellRegisteredExpressions.PLAIN_OPENING_IDENTIFIER, "cat <<EOF", "-", "NOMATCH"),
		(ShellRegisteredExpressions.TAB_STRIPPING_OPENING_IDENTIFIER, "<<-EOF", "terminator", "EOF"),
		(ShellRegisteredExpressions.TAB_STRIPPING_OPENING_IDENTIFIER, "<<- 'EOF'", "terminator", "EOF"),
		(ShellRegisteredExpressions.TAB_STRIPPING_OPENING_IDENTIFIER, "<<-\"\"", "terminator", ""),
		(ShellRegisteredExpressions.TAB_STRIPPING_OPENING_IDENTIFIER, "<<-<EOF", "-", "NOMATCH"),
		(ShellRegisteredExpressions.TAB_STRIPPING_OPENING_IDENTIFIER, "<<EOF", "-", "NOMATCH"),
		(ShellRegisteredExpressions.FIRST_LINE_IDENTIFIER, "#!/bin/sh", "-", ""),
		(ShellRegisteredExpressions.FIRST_LINE_IDENTIFIER, "#!/usr/bin/env bash", "-", ""),
		(ShellRegisteredExpressions.FIRST_LINE_IDENTIFIER, "#!/usr/bin/env -S zsh -e", "-", ""),
		(ShellRegisteredExpressions.FIRST_LINE_IDENTIFIER, "#!/bin/dash", "-", "NOMATCH"),
		(ShellRegisteredExpressions.FIRST_LINE_IDENTIFIER, "#!/usr/bin/env FOO=1 bash", "-", "NOMATCH"),
		(ShellRegisteredExpressions.FIRST_LINE_IDENTIFIER, "#!/bin/bashful", "-", "NOMATCH"),
		(ShellRegisteredExpressions.CONSTANT_DECLARATION_IDENTIFIER, "SCHEME=Basic", "value", "Basic"),
		(ShellRegisteredExpressions.CONSTANT_DECLARATION_IDENTIFIER, "readonly SCHEME='Basic'", "name", "SCHEME"),
		(ShellRegisteredExpressions.CONSTANT_DECLARATION_IDENTIFIER, "export -r SCHEME=\"Basic\"", "value", "Basic"),
		(ShellRegisteredExpressions.CONSTANT_DECLARATION_IDENTIFIER, "SCHEME=$other", "-", "NOMATCH"),
		(ShellRegisteredExpressions.CONSTANT_DECLARATION_IDENTIFIER, "echo SCHEME=Basic", "-", "NOMATCH"),
		(ShellRegisteredExpressions.CONSTANT_DECLARATION_IDENTIFIER, "SCHEME=\"a $b\"", "-", "NOMATCH")
	];

	/// <summary>The texts that open each expression's shapes, with nothing that closes them.</summary>
	private static readonly ImmutableArray<string> _openers =
	[
		"cat <<",
		"cat <<-",
		"cat << \\",
		"cat <<\"",
		"cat <<'",
		"cat <<$'",
		"cat <<E\"O\"F\"",
		"cat <<EOF",
		"cat <<-\\\n\\\n\\\n",
		"#!/usr/bin/env -S -i -S ",
		"#!",
		"#!/",
		"#!/bin/",
		"export -r -x -y ",
		"declare ",
		"A=\"",
		"A='"
	];

	private static readonly ImmutableArray<string> _fillers =
	[
		"\tx=1\n",
		"\n",
		"-- -- -- ",
		"a\\\n",
		"\"'\"'\"",
		"$$$$ "
	];

	public static TheoryData<string, string, string, string> Fixtures()
	{
		TheoryData<string, string, string, string> data = [];

		foreach ((string expression, string text, string group, string expected) in _fixtures)
		{
			data.Add(expression, text, group, expected);
		}

		return data;
	}

	public static TheoryData<string> OpenerIndexes()
	{
		TheoryData<string> data = [];

		for (int index = 0; index < _openers.Length; index++)
		{
			data.Add(index.ToString(CultureInfo.InvariantCulture));
		}

		return data;
	}

	[Fact]
	public void TheShippedExpressionsAreCountedAndEveryOneIsReadFromTheBinary()
	{
		Assert.True(ShellRegisteredExpressions.All.Count >= _lexicalExpressions.Length + 1);
		Assert.Equal(ShellRegisteredExpressions.All.Count, ShellRegisteredExpressions.All.Select(pair => pair.Identifier).Distinct(StringComparer.Ordinal).Count());
	}

	[Fact]
	public void EveryShippedExpressionDeclaresTheTimeoutOfThePolicyThatEvaluatesIt()
	{
		foreach ((string identifier, Regex expression) in ShellRegisteredExpressions.All)
		{
			TimeSpan policy = _lexicalExpressions.Contains(identifier) ? LexicalEnginePolicy.LEXICAL_BUDGET : RecognitionEnginePolicy.MATCH_BOUND;

			Assert.True(expression.MatchTimeout == policy, $"{identifier} declares {expression.MatchTimeout}, the policy says {policy}.");
		}
	}

	[Theory]
	[MemberData(nameof(Fixtures))]
	public void TheShippedExpressionReadsTheFormTheCatalogStatesAndDeclinesTheNearMiss(string expression, string text, string group, string expected)
	{
		Regex regex = ShellRegisteredExpressions.All.Single(pair => pair.Identifier == expression).Expression;
		Match match = regex.Match(text);

		if (expected == "NOMATCH")
		{
			Assert.False(match.Success, $"{expression} matched '{text}' as '{match.Value}'.");
		}
		else
		{
			Assert.True(match.Success, $"{expression} did not match '{text}'.");

			if (group != "-")
			{
				Assert.Equal(expected, string.Concat(match.Groups[group].Captures.Select(capture => capture.Value)));
			}
		}
	}

	[Theory]
	[MemberData(nameof(OpenerIndexes))]
	public void EveryShippedExpressionTerminatesWhereItsShapeExhaustsItsBoundAndTheContinuationFails(string openerIndex)
	{
		string opener = _openers[int.Parse(openerIndex, CultureInfo.InvariantCulture)];

		foreach (string text in WorstCaseTexts(opener))
		{
			foreach ((string identifier, Regex expression) in ShellRegisteredExpressions.All)
			{
				ImmutableArray<Match> matches = Enumerate(expression, text);

				Assert.True(matches.Length < _MATCH_ENUMERATION_CAP, $"{identifier} did not terminate on a worst-case text after '{opener}'.");
			}
		}
	}

	[Theory]
	[MemberData(nameof(OpenerIndexes))]
	public void TheGeneratedMatcherAndTheInterpreterAgreeOnEveryMatchAndEveryGroup(string openerIndex)
	{
		string opener = _openers[int.Parse(openerIndex, CultureInfo.InvariantCulture)];
		List<string> texts = [.. _fixtures.Select(fixture => fixture.Text), .. WorstCaseTexts(opener).Where(text => text.Length < _LONG_TEXT_LENGTH_CEILING)];

		foreach ((string identifier, Regex generated) in ShellRegisteredExpressions.All)
		{
			Regex interpreted = ShellRegisteredExpressions.Interpreted(generated);

			foreach (string text in texts)
			{
				ImmutableArray<Match> fromGenerated = Enumerate(generated, text);
				ImmutableArray<Match> fromInterpreter = Enumerate(interpreted, text);

				Assert.True(fromGenerated.Length == fromInterpreter.Length, $"{identifier}: {fromGenerated.Length} matches generated, {fromInterpreter.Length} interpreted.");

				for (int index = 0; index < fromGenerated.Length; index++)
				{
					Assert.Equal(Render(fromInterpreter[index]), Render(fromGenerated[index]));
				}
			}
		}
	}

	[Fact]
	public void NoShippedPatternWrapsALazyBoundedLoopInAnOptionalGroup()
	{
		Regex lazyBoundedLoopInOptionalGroup = new(@"\((?:\?:)?[^()]*\{\d+,\d+\}\?[^()]*\)(?:\?|\*|\{0,)", RegexOptions.CultureInvariant, TimeSpan.FromSeconds(_EXPRESSION_ANALYSIS_TIMEOUT_SECONDS));

		foreach ((string identifier, Regex expression) in ShellRegisteredExpressions.All)
		{
			Assert.False(lazyBoundedLoopInOptionalGroup.IsMatch(expression.ToString()), $"{identifier} wraps a lazy bounded loop in an optional group.");
		}
	}

	private static IEnumerable<string> WorstCaseTexts(string opener)
	{
		foreach (string filler in _fillers)
		{
			foreach (int repetitions in new[] { _FEW_LINE_REPETITIONS, _SOME_LINE_REPETITIONS, _MANY_LINE_REPETITIONS })
			{
				yield return Repeat($"{opener}{filler}\n", repetitions);
			}
		}

		string longWord = new('x', _LINE_LENGTH_CEILING);
		string longBlank = new(' ', _LINE_LENGTH_CEILING);
		string escapedLineFeeds = Repeat("\\\n", _WORST_CASE_REPETITIONS);
		string emptyDoubleQuotes = Repeat("\"\"", _WORST_CASE_REPETITIONS);
		string quotedLetters = Repeat("'a'", _WORST_CASE_REPETITIONS);

		yield return $"{opener}{longWord}";
		yield return $"{opener}{longBlank}x";
		yield return $"{opener}{escapedLineFeeds}";
		yield return $"{opener}{emptyDoubleQuotes}|";
		yield return $"{opener}{quotedLetters}|";
		yield return Repeat(opener, _MANY_LINE_REPETITIONS);
		yield return Repeat($"{opener} \\\n", _MANY_LINE_REPETITIONS);
	}

	private static string Repeat(string text, int times)
	{
		return string.Concat(Enumerable.Repeat(text, times));
	}

	private static ImmutableArray<Match> Enumerate(Regex expression, string text)
	{
		ImmutableArray<Match>.Builder matches = ImmutableArray.CreateBuilder<Match>();

		foreach (Match match in expression.Matches(text))
		{
			matches.Add(match);

			if (matches.Count >= _MATCH_ENUMERATION_CAP)
			{
				break;
			}
		}

		return matches.ToImmutable();
	}

	private static string Render(Match match)
	{
		return string.Join(
			"|",
			match.Groups.Cast<Group>().Select(group => string.Create(CultureInfo.InvariantCulture, $"{group.Name}:{group.Success}:{group.Index}:{group.Length}")));
	}
}
