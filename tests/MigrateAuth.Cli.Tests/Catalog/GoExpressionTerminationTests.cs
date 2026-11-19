using System.Collections.Immutable;
using System.Globalization;
using System.Text.RegularExpressions;

using MigrateAuth.Cli.Catalog;

using Xunit;

namespace MigrateAuth.Cli.Tests.Catalog;

/// <summary>
/// THE TERMINATION AND AGREEMENT CHECKS OF THE `Go` EXPRESSIONS, run over the SHIPPED expressions of the analyzer (<see cref="GoRegisteredExpressions"/>), on the pattern of
/// <see cref="ShellExpressionTerminationTests"/>. The interpreted matcher is built from the pattern text of each shipped expression, so what is proved is about the binary and not about a copy.
///
/// THE CHECKS. (a) Every expression DECLARES THE MATCH BOUND of the policy that evaluates it (no `Go` expression is a lexical one: the profile has no predicate and no first-line rule).
/// (b) Every expression TERMINATES on the worst-case inputs of its own shapes, opened over and over without being closed, without raising the timeout. (c) Every expression AGREES between the generator and
/// the interpreter on the same pattern, in the index, length and groups of every match, over the positive fixtures, the near-misses and the worst-case texts. (d) A structural scan states that no pattern
/// wraps a lazy bounded loop in an optional group, the shape the source generator mishandles.
///
/// These are not timing tests: nothing here asserts a figure, only that no match raises the timeout. The wall-clock cost is the shipped cost gate's.
/// </summary>
public sealed class GoExpressionTerminationTests
{
	private const int _MATCH_ENUMERATION_CAP = 5_000;
	private const int _CLASS_COUNT = 13;

	/// <summary>The constant declaration, the member declaration, the generation marker and the two content markers.</summary>
	private const int _SUPPORT_EXPRESSION_COUNT = 1 + 1 + 1 + 2;
	private const int _FEW_REPETITIONS = 3;
	private const int _SOME_REPETITIONS = 20;
	private const int _LINE_LENGTH_CEILING = 9_900;
	private const int _LONG_TEXT_LENGTH_CEILING = 3_000;
	private const int _EXPRESSION_ANALYSIS_TIMEOUT_SECONDS = 5;
	private const int _WORST_CASE_REPETITIONS = 600;

	/// <summary>Expression, text, the group to read ("-" for none), and the expected value of the group ("NOMATCH" for no match). A match is searched from the start of the text.</summary>
	private static readonly ImmutableArray<(string Expression, string Text, string Group, string Expected)> _fixtures =
	[
		("GO-001", "request.SetBasicAuth(userName, \"exampleSecret\")", "credential", "exampleSecret"),
		("GO-001", "request.SetBasicAuth(userName, secret)", "construct", "SetBasicAuth"),
		("GO-001", "func (client *Client) SetBasicAuth(userName string, secret string) {", "-", "NOMATCH"),
		("GO-002", "h.Set(\"Authorization\", \"Basic dXNlcjpwYXNz\")", "credential", "dXNlcjpwYXNz"),
		("GO-002", "h.Add(\"authorization\", \"Basic \"+x)", "construct", "Add"),
		("GO-002", "h.Set(\"Authorization\", \"Bearer \"+x)", "-", "NOMATCH"),
		("GO-002", "h.Set(\"Proxy-Authorization\", \"Basic \"+x)", "-", "NOMATCH"),
		("GO-003", "headers := http.Header{\"Authorization\": {\"Basic \" + x}}", "construct", "Header"),
		("GO-003", "headers := http.Header{\"Accept\": {\"x\"}}", "-", "NOMATCH"),
		("GO-004", "flat := map[string]string{\"Authorization\": \"Basic \" + x}", "construct", "map"),
		("GO-005", "http.Get(\"https://user:exampleSecret@host.example/path\")", "credential", "exampleSecret"),
		("GO-005", "http.Get(\"postgres://user:exampleSecret@host.example/path\")", "-", "NOMATCH"),
		("GO-006", "pair := base64.StdEncoding.EncodeToString([]byte(a + \":\" + b)) // Basic", "construct", "StdEncoding"),
		("GO-006", "pair := base64.URLEncoding.EncodeToString([]byte(a + \":\" + b)) // Basic", "-", "NOMATCH"),
		("GO-007", "address.User = url.UserPassword(userName, \"exampleSecret\")", "credential", "exampleSecret"),
		("GO-007", "func UserPassword(left string, right string) string {", "-", "NOMATCH"),
		("GO-008", "const serviceURL = \"https://user:exampleSecret@host.example/path\"", "credential", "exampleSecret"),
		("GO-008", "const proxyURL = \"https://user:exampleSecret@host.example/path\"", "-", "NOMATCH"),
		("GO-009", "h.Set(\"Authorization\", header)", "scheme", "header"),
		("GO-009", "h.Set(\"Authorization\", header())", "-", "NOMATCH"),
		("GO-010", "func (t *T) RoundTrip(r *http.Request) {\n\tr.SetBasicAuth(a, b)\n}", "construct", "RoundTrip"),
		("GO-010", "func (t *T) RoundTrip(r *http.Request) {\n\treturn nil\n}", "-", "NOMATCH"),
		("GO-011", "func build(user string) string {\n\treturn h.Set(\"Authorization\", \"Basic \"+user)\n}", "construct", "build"),
		("GO-012", "h.Set(\"Authorization\", os.Getenv(\"AUTH_HEADER\"))", "-", ""),
		("GO-013", "h.Set(\"Authorization\", basicHeader(user, secret))", "name", "basicHeader"),
		("GO-013", "h.Set(\"Authorization\", client.basicHeader(user, secret))", "-", "NOMATCH"),
		("GO-011", "func basicHeader(raw []byte) string {\n\treturn \"Basic \" + base64.StdEncoding.EncodeToString(raw)\n}", "construct", "basicHeader"),
		("GO-011", "func challenge(realm string) string {\n\treturn \"Basic realm=\\\"\" + realm\n}", "-", "NOMATCH"),
		(GoRegisteredExpressions.MEMBER_DECLARATION_IDENTIFIER, "func (client *Client) basicHeader(raw []byte) string {\n\treturn \"Basic \" + base64.StdEncoding.EncodeToString(raw)\n}", "name", "basicHeader"),
		(GoRegisteredExpressions.MEMBER_DECLARATION_IDENTIFIER, "x := func(raw []byte) string {", "-", "NOMATCH"),
		(GoRegisteredExpressions.CONSTANT_DECLARATION_IDENTIFIER, "const scheme = \"Basic\"", "value", "Basic"),
		(GoRegisteredExpressions.CONSTANT_DECLARATION_IDENTIFIER, "scheme := `Bearer`", "name", "scheme"),
		(GoRegisteredExpressions.CONSTANT_DECLARATION_IDENTIFIER, "scheme := other", "-", "NOMATCH"),
		(GoRegisteredExpressions.GENERATION_MARKER_IDENTIFIER_PREFIX + "0", "service.pb.go", "-", ""),
		(GoRegisteredExpressions.GENERATION_MARKER_IDENTIFIER_PREFIX + "0", "service.go", "-", "NOMATCH"),
		(GoRegisteredExpressions.CONTENT_MARKER_IDENTIFIER_PREFIX + "0", "// Code generated by a tool. DO NOT EDIT.\n", "-", ""),
		(GoRegisteredExpressions.CONTENT_MARKER_IDENTIFIER_PREFIX + "0", "// Code generated by a tool.\n", "-", "NOMATCH"),
		(GoRegisteredExpressions.CONTENT_MARKER_IDENTIFIER_PREFIX + "1", "func ExampleClient() {", "-", ""),
		(GoRegisteredExpressions.CONTENT_MARKER_IDENTIFIER_PREFIX + "1", "func Exampled() {", "-", "NOMATCH")
	];

	/// <summary>The texts that open each expression's shapes, with nothing that closes them.</summary>
	private static readonly ImmutableArray<string> _openers =
	[
		"request.SetBasicAuth(",
		"request.Header.Set(\"Authorization\",",
		"request.Header[\"Authorization\"] = append(",
		"headers := http.Header{",
		"flat := map[string]string{",
		"response, err := http.Get(",
		"pair := base64.StdEncoding.EncodeToString([]byte(",
		"address.User = url.UserPassword(",
		"address, err := url.Parse(",
		"func (transport *basicTransport) RoundTrip(request *http.Request) (*http.Response, error) {",
		"func basicHeader(userName string, secret string) string {",
		"request.Header.Set(\"Authorization\", basicHeader(",
		"func basicHeader(raw []byte) string {\n\tencoded := base64.StdEncoding.EncodeToString(raw)\n\treturn \"Basic \" + ",
		"x := `"
	];

	private static readonly ImmutableArray<string> _fillers =
	[
		"\tx := 1\n",
		"\n",
		"(((((a)))))[[[[[b]]]]]",
		"\"'`/*",
		"\\\n",
		"Basic Authorization "
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
		Assert.Equal(_CLASS_COUNT, GoRegisteredExpressions.Classes.Count);
		Assert.Equal(_SUPPORT_EXPRESSION_COUNT, GoRegisteredExpressions.Support.Count);
		Assert.Equal(GoRegisteredExpressions.All.Count, GoRegisteredExpressions.All.Select(pair => pair.Identifier).Distinct(StringComparer.Ordinal).Count());
	}

	[Fact]
	public void EveryShippedExpressionDeclaresTheMatchBoundOfThePolicy()
	{
		foreach ((string identifier, Regex expression) in GoRegisteredExpressions.All)
		{
			Assert.True(expression.MatchTimeout == RecognitionEnginePolicy.MATCH_BOUND, $"{identifier} declares {expression.MatchTimeout}, the policy says {RecognitionEnginePolicy.MATCH_BOUND}.");
		}
	}

	[Theory]
	[MemberData(nameof(Fixtures))]
	public void TheShippedExpressionReadsTheFormTheCatalogStatesAndDeclinesTheNearMiss(string expression, string text, string group, string expected)
	{
		Regex regex = GoRegisteredExpressions.All.Single(pair => pair.Identifier == expression).Expression;
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
			foreach ((string identifier, Regex expression) in GoRegisteredExpressions.All)
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

		foreach ((string identifier, Regex generated) in GoRegisteredExpressions.All)
		{
			Regex interpreted = GoRegisteredExpressions.Interpreted(generated);

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

		foreach ((string identifier, Regex expression) in GoRegisteredExpressions.All)
		{
			Assert.False(lazyBoundedLoopInOptionalGroup.IsMatch(expression.ToString()), $"{identifier} wraps a lazy bounded loop in an optional group.");
		}
	}

	private static IEnumerable<string> WorstCaseTexts(string opener)
	{
		foreach (string filler in _fillers)
		{
			foreach (int repetitions in new[] { _FEW_REPETITIONS, _SOME_REPETITIONS })
			{
				yield return Repeat($"{opener}{filler}\n", repetitions);
			}
		}

		string longWord = new('x', _LINE_LENGTH_CEILING);
		string longBlank = new(' ', _LINE_LENGTH_CEILING);
		string emptyDoubleQuotes = Repeat("\"\"", _WORST_CASE_REPETITIONS);
		string quotedLetters = Repeat("'a'", _WORST_CASE_REPETITIONS);

		yield return $"{opener}{longWord}";
		yield return $"{opener}{longBlank}x";
		yield return $"{opener}{emptyDoubleQuotes}|";
		yield return $"{opener}{quotedLetters}|";
		yield return Repeat(opener, _SOME_REPETITIONS);
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
