using System.Collections.Immutable;
using System.Globalization;
using System.Text.RegularExpressions;

using MigrateAuth.Cli.Catalog;

using Xunit;

namespace MigrateAuth.Cli.Tests.Catalog;

/// <summary>
/// THE TERMINATION AND AGREEMENT CHECKS OF THE `JavaScript` EXPRESSIONS, run over the SHIPPED expressions of the analyzer (<see cref="JavaScriptRegisteredExpressions"/>), on the pattern of
/// <see cref="VBNetExpressionTerminationTests"/> and <see cref="ShellExpressionTerminationTests"/>. The interpreted matcher is built from the pattern text of each shipped
/// expression, so what is proved is about the binary and not about a copy.
///
/// THE CHECKS. (a) Every expression DECLARES THE TIMEOUT OF THE POLICY that evaluates it: the lexical budget for the first-line rule (the only expression the budget charges), the match bound for every other.
/// (b) Every expression TERMINATES on the worst-case inputs of its own windows (a window that exhausts its bound and then fails its continuation, repeated, at the line-length ceiling) without raising the timeout.
/// (c) Every expression AGREES between the generator and the interpreter on the same pattern, in the index, length and groups of every match, over the fixtures, the near-misses and the worst-case texts.
/// (d) A structural scan states that no pattern wraps a lazy bounded loop in an optional group, the shape the source generator mishandles.
/// </summary>
public sealed class JavaScriptExpressionTerminationTests
{
	private const string _CONSTANT_DECLARATION_IDENTIFIER = "ConstantDeclaration";
	private const string _FIRST_LINE_IDENTIFIER = "ShebangFirstLine";
	private const string _CONTENT_MARKER_IDENTIFIER_PREFIX = "ContentMarker";
	private const string _NOT_A_MATCH = "NOMATCH";
	private const string _NO_GROUP = "-";

	private const int _MATCH_ENUMERATION_CAP = 5_000;

	private const string _GROUP_SEPARATOR = "|";

	private const int _LINE_LENGTH_CEILING = 9_900;
	private const int _FEW_LINE_REPETITIONS = 3;
	private const int _SOME_LINE_REPETITIONS = 40;
	private const int _MANY_LINE_REPETITIONS = 400;
	private const int _LONG_TEXT_LENGTH_CEILING = 3_000;
	private const int _EXPRESSION_ANALYSIS_TIMEOUT_SECONDS = 5;
	private const int _WORST_CASE_REPETITIONS = 1_500;
	private const int _MINIMUM_EXPRESSION_COUNT = 30;

	/// <summary>Every shipped expression, by identifier: the pattern classes, the constant declaration, the content markers, the first-line rule and the flow signals, read from the registered analyzer.</summary>
	private static readonly ImmutableArray<(string Identifier, Regex Expression)> _all = ReadEveryShippedExpression();

	/// <summary>Expression, text, the group to read ("-" for none), and the expected value of the group ("NOMATCH" for no match). A match is searched from the start of the text.</summary>
	private static readonly ImmutableArray<(string Expression, string Text, string Group, string Expected)> _fixtures =
	[
		(_FIRST_LINE_IDENTIFIER, "#!/usr/bin/node", _NO_GROUP, string.Empty),
		(_FIRST_LINE_IDENTIFIER, "#!/usr/bin/env node", _NO_GROUP, string.Empty),
		(_FIRST_LINE_IDENTIFIER, "#!/usr/bin/env -S nodejs --no-warnings", _NO_GROUP, string.Empty),
		(_FIRST_LINE_IDENTIFIER, "#!/usr/bin/nodex", _NO_GROUP, _NOT_A_MATCH),
		(_FIRST_LINE_IDENTIFIER, "#!/usr/bin/env bash", _NO_GROUP, _NOT_A_MATCH),
		(_FIRST_LINE_IDENTIFIER, "#!/usr/bin/env FOO=1 node", _NO_GROUP, _NOT_A_MATCH),
		(_FIRST_LINE_IDENTIFIER, "// #!/usr/bin/node", _NO_GROUP, _NOT_A_MATCH),
		(_CONSTANT_DECLARATION_IDENTIFIER, "const SCHEME = 'Basic';", "value", "Basic"),
		(_CONSTANT_DECLARATION_IDENTIFIER, "const SCHEME = 'Basic';", "name", "SCHEME"),
		(_CONSTANT_DECLARATION_IDENTIFIER, "export const SCHEME = \"Basic\";", "value", "Basic"),
		(_CONSTANT_DECLARATION_IDENTIFIER, "let header = 'Basic ' + encoded", "value", "Basic"),
		(_CONSTANT_DECLARATION_IDENTIFIER, "const SCHEME = other;", _NO_GROUP, _NOT_A_MATCH),
		(_CONSTANT_DECLARATION_IDENTIFIER, "if (SCHEME == 'Basic') {", _NO_GROUP, _NOT_A_MATCH),
		("JS-001", "axios.get(url, { auth: { username: userName, password: secret } });", _NO_GROUP, string.Empty),
		("JS-001", "axios.get(url, { headers: { Accept: 'text/plain' } });", _NO_GROUP, _NOT_A_MATCH),
		("JS-004", "fetch(url, { headers: { Authorization: 'Basic abc' } });", _NO_GROUP, string.Empty),
		("JS-004", "fetch(url, { headers: { Authorization: 'Bearer abc' } });", _NO_GROUP, _NOT_A_MATCH),
		("JS-005", "const header = 'Basic ' + btoa(userName + ':' + secret);", _NO_GROUP, string.Empty),
		("JS-005", "const key = btoa(host + ':' + port);", _NO_GROUP, _NOT_A_MATCH)
	];

	/// <summary>The texts that open each expression's windows, with nothing that closes them.</summary>
	private static readonly ImmutableArray<string> _openers =
	[
		"axios.get(url, {",
		"axios.get(url, { auth: {",
		"request.get(url).auth(",
		"superagent.get(url)",
		"const headers = { Authorization:",
		"new Headers({",
		"btoa(",
		"Buffer.from(",
		"Buffer(",
		"new URL(base);\n\ttarget.password =",
		"function build(user, pass) {",
		"xhr.open('GET', url, true,",
		"$.ajax({ username: u,",
		"got(url, {",
		"https://user:",
		"describe('suite', () => {",
		"const text = `${",
		"const a = '",
		"const a = \"",
		"const a = /",
		"#!/usr/bin/env -S -i -S ",
		"#!",
		"#!/usr/bin/",
		"const SCHEME = '",
		"document.querySelector('input[type=password",
		"window.prompt('Enter your password",
		"<input type=\"password"
	];

	private static readonly ImmutableArray<string> _fillers =
	[
		"\tx = 1;\n",
		"\n",
		", , , ",
		"a\\\n",
		"'\"`'\"`",
		"$$$$ ",
		"((((",
		"}}}} "
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
		Assert.True(_all.Length >= _MINIMUM_EXPRESSION_COUNT, $"{_all.Length} expressions read from the shipped analyzer.");
		Assert.Equal(_all.Length, _all.Select(pair => pair.Identifier).Distinct(StringComparer.Ordinal).Count());
		Assert.Contains(_all, pair => pair.Identifier == _FIRST_LINE_IDENTIFIER);
		Assert.Contains(_all, pair => pair.Identifier == _CONSTANT_DECLARATION_IDENTIFIER);
	}

	[Fact]
	public void EveryShippedExpressionDeclaresTheTimeoutOfThePolicyThatEvaluatesItAndOnlyTheFirstLineRuleIsChargedToTheLexicalBudget()
	{
		foreach ((string identifier, Regex expression) in _all)
		{
			TimeSpan policy = identifier == _FIRST_LINE_IDENTIFIER ? LexicalEnginePolicy.LEXICAL_BUDGET : RecognitionEnginePolicy.MATCH_BOUND;

			Assert.True(expression.MatchTimeout == policy, $"{identifier} declares {expression.MatchTimeout}, the policy says {policy}.");
		}

		Assert.Single(_all, pair => pair.Expression.MatchTimeout == LexicalEnginePolicy.LEXICAL_BUDGET);
	}

	[Theory]
	[MemberData(nameof(Fixtures))]
	public void TheShippedExpressionReadsTheFormTheCatalogStatesAndDeclinesTheNearMiss(string expression, string text, string group, string expected)
	{
		Regex regex = _all.Single(pair => pair.Identifier == expression).Expression;
		Match match = regex.Match(text);

		if (expected == _NOT_A_MATCH)
		{
			Assert.False(match.Success, $"{expression} matched '{text}' as '{match.Value}'.");
		}
		else
		{
			Assert.True(match.Success, $"{expression} did not match '{text}'.");

			if (group != _NO_GROUP)
			{
				Assert.Equal(expected, string.Concat(match.Groups[group].Captures.Select(capture => capture.Value)));
			}
		}
	}

	[Theory]
	[MemberData(nameof(OpenerIndexes))]
	public void EveryShippedExpressionTerminatesWhereItsWindowExhaustsItsBoundAndTheContinuationFails(string openerIndex)
	{
		string opener = _openers[int.Parse(openerIndex, CultureInfo.InvariantCulture)];

		foreach (string text in WorstCaseTexts(opener))
		{
			foreach ((string identifier, Regex expression) in _all)
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

		foreach ((string identifier, Regex generated) in _all)
		{
			Regex interpreted = Interpreted(generated);

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

		foreach ((string identifier, Regex expression) in _all)
		{
			Assert.False(lazyBoundedLoopInOptionalGroup.IsMatch(expression.ToString()), $"{identifier} wraps a lazy bounded loop in an optional group.");
		}
	}

	private static ImmutableArray<(string Identifier, Regex Expression)> ReadEveryShippedExpression()
	{
		ILanguageAnalyzer analyzer = JavaScriptRegisteredExpressions.Analyzer;
		ImmutableArray<(string Identifier, Regex Expression)> expressions =
		[
			.. analyzer.DetectionRulesByPatternClass.OrderBy(entry => entry.Key, StringComparer.Ordinal).Select(entry => (entry.Key, entry.Value.Expression)),
			(_CONSTANT_DECLARATION_IDENTIFIER, analyzer.LexicalProfile.ConstantDeclarationExpression),
			.. analyzer.DemotionSignals.ContentMarkers.Select((marker, index) => ($"{_CONTENT_MARKER_IDENTIFIER_PREFIX}{index}", marker.Expression)),
			(_FIRST_LINE_IDENTIFIER, analyzer.LanguageIdentification.FirstLineDetectionExpression!),
			.. analyzer.CandidateSelectionAndRecommendation.FlowSignals.Select(signal => (signal.Name, signal.Expression))
		];

		return expressions;
	}

	private static Regex Interpreted(Regex generated)
	{
		return new Regex(generated.ToString(), generated.Options, generated.MatchTimeout);
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
		string nestedGroups = Repeat("(", _WORST_CASE_REPETITIONS);

		yield return $"{opener}{longWord}";
		yield return $"{opener}{longBlank}x";
		yield return $"{opener}{escapedLineFeeds}";
		yield return $"{opener}{emptyDoubleQuotes}|";
		yield return $"{opener}{quotedLetters}|";
		yield return $"{opener}{nestedGroups}";
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
			_GROUP_SEPARATOR,
			match.Groups.Cast<Group>().Select(group => string.Create(CultureInfo.InvariantCulture, $"{group.Name}:{group.Success}:{group.Index}:{group.Length}")));
	}
}
