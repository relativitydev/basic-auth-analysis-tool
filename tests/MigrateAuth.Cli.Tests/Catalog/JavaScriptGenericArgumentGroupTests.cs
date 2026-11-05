using System.Collections.Immutable;
using System.Text;
using System.Text.RegularExpressions;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Recognition;
using MigrateAuth.Cli.Tests.Recognition;

using Xunit;
using Xunit.Abstractions;

namespace MigrateAuth.Cli.Tests.Catalog;

/// <summary>
/// THE SEMANTIC CHECKS OF THE GENERIC-ARGUMENT GROUP OF THE CALLEE EXPRESSIONS OF `JS-001`, `JS-003`, `JS-015` AND `JS-016`, run over the SHIPPED expressions of the analyzer
/// (<see cref="JavaScriptRegisteredExpressions"/>). The expression without the group is rebuilt from the shipped pattern by removing the group, once, so the comparison is between the binary and the same
/// binary without the group, never between the binary and a copy.
///
/// EVERY CASE STATES, FOR THE `JavaScript` ANALYZER THAT SHARES THE EXPRESSION, WHETHER THE RESULT EQUALS THE RESULT WITHOUT THE GROUP. There are three kinds. <see cref="CaseKind.Unchanged"/>: the result
/// equals the result without the group (a call with no angle bracket, a comparison chain with a blank before the opening angle bracket, a depth beyond three, a list past the length bound, an unbalanced or
/// line-spanning list, a Flow or other annotation that is not between the callee and the parenthesis, JSX text). <see cref="CaseKind.TypeScriptGenericCall"/>: the form the group exists to read; it is read with the group and
/// not without it. <see cref="CaseKind.TightComparisonChain"/>: the same characters as a generic call, with a `JavaScript` reading as a comparison chain (`callee&lt;a&gt;(...)`) or, under Flow, as an explicit type
/// argument; a regular expression cannot separate it from the generic call, so the result is NOT equal to the result without the group for `JavaScript` text, and the case is recorded as such.
/// No case ever loses a result.
/// </summary>
public sealed class JavaScriptGenericArgumentGroupTests
{
	private const string _UNITS_PER_LEVEL_TEXT = "120";
	private const string _LEVEL_1 = $@"<(?>(?:(?>=>|[^<>\n])){{0,{_UNITS_PER_LEVEL_TEXT}}})>";
	private const string _LEVEL_2 = $@"<(?>(?:(?>=>|[^<>\n]|{_LEVEL_1})){{0,{_UNITS_PER_LEVEL_TEXT}}})>";
	private const string _LEVEL_3 = $@"<(?>(?:(?>=>|[^<>\n]|{_LEVEL_2})){{0,{_UNITS_PER_LEVEL_TEXT}}})>";

	/// <summary>The group as the four callee expressions carry it; each shipped pattern holds it exactly once.</summary>
	internal const string GROUP_TEXT = $@"(?>(?:{_LEVEL_3})?)";

	private const int _UNITS_PER_LEVEL = 120;
	private const int _LEVELS = 3;
	private const int _ONE_UNIT = 1;
	private const int _ONCE = 1;
	private const int _NEVER = 0;
	private const int _ATOMIC_GROUPS_IN_THE_GROUP = 7;
	private const int _DIAGNOSTIC_TEXT_LENGTH = 60;
	private const string _FILLER_UNIT = "x";
	private const string _CLIENT_AUTH_OBJECT = "{ auth: { username: u, password: p } }";
	private const string _REQUEST_AUTH_OBJECT = "{ auth: { user: u, pass: p, sendImmediately: false } }";
	private const string _USERINFO_URL = "'https://exampleUser:exampleSecret@host.example/path'";
	private const string _GOT_OPTIONS = "{ username: u, password: 'p' }";
	private const string _CLIENT_AUTH_ID = "JS-001";
	private const string _URL_ID = "JS-003";
	private const string _GOT_ID = "JS-015";
	private const string _REQUEST_ID = "JS-016";
	private const string _UNTYPED_CLIENT_PATH = "src/client.js";

	internal enum CaseKind
	{
		Unchanged,
		TypeScriptGenericCall,
		TightComparisonChain
	}

	internal sealed record Case(string Id, string Name, string Text, bool ReadNow, bool ReadBefore, CaseKind Kind);

	private static readonly ImmutableArray<string> _FOUR_IDS = [_CLIENT_AUTH_ID, _URL_ID, _GOT_ID, _REQUEST_ID];

	private static readonly ImmutableArray<Case> _CASES = [.. BuildCases()];

	private readonly ITestOutputHelper _output;

	public JavaScriptGenericArgumentGroupTests(ITestOutputHelper output)
	{
		_output = output;
	}

	[Fact]
	public void EachOfTheFourCalleeExpressionsCarriesTheGroupExactlyOnceAndNoOtherClassCarriesIt()
	{
		IReadOnlyDictionary<string, DetectionRule> rules = JavaScriptRegisteredExpressions.Analyzer.DetectionRulesByPatternClass;

		foreach (KeyValuePair<string, DetectionRule> rule in rules)
		{
			int occurrences = CountOccurrences(rule.Value.Expression.ToString(), GROUP_TEXT);

			Assert.Equal(_FOUR_IDS.Contains(rule.Key) ? _ONCE : _NEVER, occurrences);
		}
	}

	[Fact]
	public void TheGroupIsThreeLevelsOfAtomicPossessiveBodiesBoundedAtOneHundredTwentyUnitsWithNoLazyLoopAndNoUnboundedQuantifier()
	{
		Assert.Equal(_UNITS_PER_LEVEL, int.Parse(_UNITS_PER_LEVEL_TEXT, System.Globalization.CultureInfo.InvariantCulture));
		Assert.Equal(_LEVELS, CountOccurrences(GROUP_TEXT, "<(?>"));
		Assert.Equal(_LEVELS, CountOccurrences(GROUP_TEXT, $"{{0,{_UNITS_PER_LEVEL}}}"));
		Assert.Equal(_ATOMIC_GROUPS_IN_THE_GROUP, CountOccurrences(GROUP_TEXT, "(?>"));
		Assert.False(Regex.IsMatch(GROUP_TEXT, @"\}\?|[*+]"), "The group carries a lazy or an unbounded quantifier.");
		Assert.StartsWith("(?>", GROUP_TEXT, StringComparison.Ordinal);
	}

	[Fact]
	public void TheExpressionBeforeTheChangeIsTheShippedOneWithoutTheGroupAndTheDeltaIsRecorded()
	{
		StringBuilder delta = new();

		foreach (string id in _FOUR_IDS)
		{
			string now = ShippedExpression(id).ToString();
			string before = PreChangePattern(id);

			Assert.NotEqual(now, before);
			Assert.DoesNotContain("<(?>", before, StringComparison.Ordinal);
			Assert.Equal(now.Length - GROUP_TEXT.Length, before.Length);
			delta.AppendLine($"{id}: the pre-change pattern is the shipped pattern with the {GROUP_TEXT.Length}-character group removed once, directly before the blanks and the opening parenthesis.");
		}

		_output.WriteLine(delta.ToString());
	}

	[Fact]
	public void EveryCaseIsReadNowAsStatedAndWasReadBeforeAsStated()
	{
		List<string> wrong = [];

		foreach (Case sample in _CASES)
		{
			bool now = ShippedExpression(sample.Id).IsMatch(sample.Text);
			bool before = PreChangeExpression(sample.Id).IsMatch(sample.Text);

			if (now != sample.ReadNow || before != sample.ReadBefore)
			{
				wrong.Add($"{sample.Id} {sample.Name}: now {now} (stated {sample.ReadNow}), before {before} (stated {sample.ReadBefore})");
			}
		}

		Assert.True(wrong.Count == 0, string.Join(Environment.NewLine, wrong));
	}

	[Fact]
	public void TheKindOfEveryCaseAgreesWithItsResultsAndNoCaseLosesAResult()
	{
		foreach (Case sample in _CASES)
		{
			Assert.False(sample.ReadBefore && !sample.ReadNow, $"{sample.Id} {sample.Name}: a result was lost.");

			if (sample.Kind == CaseKind.Unchanged)
			{
				Assert.True(sample.ReadNow == sample.ReadBefore, $"{sample.Id} {sample.Name}: an unchanged case moved.");
			}
			else
			{
				Assert.True(sample.ReadNow && !sample.ReadBefore, $"{sample.Id} {sample.Name}: a group case must be newly read.");
			}
		}
	}

	[Fact]
	public void EveryFormTheGroupReadsIsOneOfTheTwoKindsAndTheTightComparisonChainsAreNamedForTheEquivalenceProof()
	{
		string[] moved = [.. _CASES.Where(sample => sample.ReadNow != sample.ReadBefore && sample.Kind == CaseKind.TightComparisonChain).Select(sample => $"{sample.Id} {sample.Name}")];

		_output.WriteLine("Forms whose result for JavaScript text is not equal to the pre-change result (text identical to a generic call):");

		foreach (string name in moved)
		{
			_output.WriteLine($"  {name}");
		}

		Assert.Equal(_FOUR_IDS.Length, moved.Length);
		Assert.Equal(_FOUR_IDS.Order(StringComparer.Ordinal), moved.Select(entry => entry[.._CLIENT_AUTH_ID.Length]).Order(StringComparer.Ordinal));
	}

	[Fact]
	public void TheGeneratedAndTheInterpretedFormsOfEveryExpressionAgreeOnEveryCase()
	{
		foreach (Case sample in _CASES)
		{
			Regex generated = ShippedExpression(sample.Id);
			Regex interpreted = new(generated.ToString(), generated.Options, generated.MatchTimeout);
			Match fromGenerated = generated.Match(sample.Text);
			Match fromInterpreted = interpreted.Match(sample.Text);

			Assert.Equal(fromInterpreted.Success, fromGenerated.Success);
			Assert.Equal(fromInterpreted.Index, fromGenerated.Index);
			Assert.Equal(fromInterpreted.Length, fromGenerated.Length);
		}
	}

	[Fact]
	public void AGenericCallIsReadWithItsCalleeAsTheConstructAndTheMatchOpensAtTheCalleeAndNotAtTheAngleBracket()
	{
		Match match = ShippedExpression(_GOT_ID).Match($"got<Reply>(url, {_GOT_OPTIONS});");

		Assert.True(match.Success);
		Assert.Equal(_NEVER, match.Index);
		Assert.Equal("got", match.Groups["construct"].Value);
	}

	[Fact]
	public void ADepthOfFourAndAListPastTheLengthBoundAreNotGroupsAndTheCalleeIsNotReadAtEveryLevel()
	{
		string levelOneOver = Filler(_UNITS_PER_LEVEL + _ONE_UNIT);
		string levelTwoOver = $"{Filler(_UNITS_PER_LEVEL)}<{Filler(_ONE_UNIT)}>";
		string levelThreeOver = $"{Filler(_UNITS_PER_LEVEL)}<{levelTwoOver}>";
		string[] refused =
		[
			$"axios.get<{levelOneOver}>(url, {_CLIENT_AUTH_OBJECT});",
			$"axios.get<A<{levelOneOver}>>(url, {_CLIENT_AUTH_OBJECT});",
			$"axios.get<A<B<{levelOneOver}>>>(url, {_CLIENT_AUTH_OBJECT});",
			$"axios.get<A<{levelTwoOver}>>(url, {_CLIENT_AUTH_OBJECT});",
			$"axios.get<{levelThreeOver}>(url, {_CLIENT_AUTH_OBJECT});",
			$"axios.get<A<B<C<D>>>>(url, {_CLIENT_AUTH_OBJECT});"
		];

		Assert.All(refused, text => Assert.False(ShippedExpression(_CLIENT_AUTH_ID).IsMatch(text), text[..Math.Min(text.Length, _DIAGNOSTIC_TEXT_LENGTH)]));
	}

	[Fact]
	public void AJavaScriptFileWithBlankSeparatedComparisonChainsAndAnnotationsAroundACallProducesTheSameConstructsAsBefore()
	{
		const string SOURCE =
			$"const r: Promise<Reply> = axios.get(url, {_CLIENT_AUTH_OBJECT});\nif (a < b && c > (d)) {{ go(); }}\nconst ok = request < limit > (url, {_REQUEST_AUTH_OBJECT});\nconst view = <p>fetch <b>(see)</b></p>;\n";

		ImmutableArray<RecognizedConstruct> constructs = new JavaScriptRecognitionHarness().Recognize(SOURCE, _UNTYPED_CLIENT_PATH);

		string[] identifiers = [.. constructs.Select(construct => construct.PatternClassIdentifier)];

		Assert.Equal(_ONCE, identifiers.Count(identifier => identifier == _CLIENT_AUTH_ID));
		Assert.DoesNotContain(_URL_ID, identifiers);
		Assert.DoesNotContain(_GOT_ID, identifiers);
		Assert.DoesNotContain(_REQUEST_ID, identifiers);
	}

	private static IEnumerable<Case> BuildCases()
	{
		return [.. ClientAuthCases(), .. UrlCases(), .. GotCases(), .. RequestCases()];
	}

	private static string Filler(int count)
	{
		return new string(_FILLER_UNIT[0], count);
	}

	private static IEnumerable<Case> ClientAuthCases()
	{
		string levelOne = Filler(_UNITS_PER_LEVEL);
		string levelTwo = $"{Filler(_UNITS_PER_LEVEL - _ONE_UNIT)}<{levelOne}>";
		string levelThree = $"{Filler(_UNITS_PER_LEVEL - _ONE_UNIT)}<{levelTwo}>";
		string levelOneOver = Filler(_UNITS_PER_LEVEL + _ONE_UNIT);

		yield return Group(_CLIENT_AUTH_ID, "create-with-one-argument", $"axios.create<Config>({_CLIENT_AUTH_OBJECT});");
		yield return Group(_CLIENT_AUTH_ID, "depth-one-two-arguments", $"axios.get<A, B>(url, {_CLIENT_AUTH_OBJECT});");
		yield return Group(_CLIENT_AUTH_ID, "depth-two", $"axios.get<A<B>>(url, {_CLIENT_AUTH_OBJECT});");
		yield return Group(_CLIENT_AUTH_ID, "depth-three", $"axios.get<A<B<C>>>(url, {_CLIENT_AUTH_OBJECT});");
		yield return Group(_CLIENT_AUTH_ID, "level-one-at-the-length-bound", $"axios.get<{levelOne}>(url, {_CLIENT_AUTH_OBJECT});");
		yield return Group(_CLIENT_AUTH_ID, "every-level-at-the-length-bound", $"axios.get<{levelThree}>(url, {_CLIENT_AUTH_OBJECT});");
		yield return Group(_CLIENT_AUTH_ID, "arrow-function-type", $"axios.get<(a: A) => B>(url, {_CLIENT_AUTH_OBJECT});");
		yield return Group(_CLIENT_AUTH_ID, "object-type-literal", $"axios.get<{{ a: string }}>(url, {_CLIENT_AUTH_OBJECT});");
		yield return Group(_CLIENT_AUTH_ID, "blank-after-the-list", $"axios.get<A> (url, {_CLIENT_AUTH_OBJECT});");
		yield return Group(_CLIENT_AUTH_ID, "request-callee", $"request.get<A<B>>(url, {_CLIENT_AUTH_OBJECT});");
		yield return Tight(_CLIENT_AUTH_ID, "tight-comparison-chain-on-request", $"request<a>(url, {_CLIENT_AUTH_OBJECT});");
		yield return Same(_CLIENT_AUTH_ID, "depth-four", $"axios.get<A<B<C<D>>>>(url, {_CLIENT_AUTH_OBJECT});", false);
		yield return Same(_CLIENT_AUTH_ID, "level-one-past-the-length-bound", $"axios.get<{levelOneOver}>(url, {_CLIENT_AUTH_OBJECT});", false);
		yield return Same(_CLIENT_AUTH_ID, "unbalanced-opening", $"axios.get<A(url, {_CLIENT_AUTH_OBJECT});", false);
		yield return Same(_CLIENT_AUTH_ID, "outer-level-never-closed", $"axios.get<A<B>(url, {_CLIENT_AUTH_OBJECT});", false);
		yield return Same(_CLIENT_AUTH_ID, "list-spans-a-line", $"axios.get<A\nB>(url, {_CLIENT_AUTH_OBJECT});", false);
		yield return Same(_CLIENT_AUTH_ID, "blank-before-the-angle-bracket", $"axios.get <A>(url, {_CLIENT_AUTH_OBJECT});", false);
		yield return Same(_CLIENT_AUTH_ID, "spaced-comparison-chain", $"axios.get < a > (url, {_CLIENT_AUTH_OBJECT});", false);
		yield return Same(_CLIENT_AUTH_ID, "spaced-comparison-chain-on-request", $"request < a > (url, {_CLIENT_AUTH_OBJECT});", false);
		yield return Same(_CLIENT_AUTH_ID, "comparison-in-a-condition-before-a-plain-call", $"if (a < b && c > (d)) {{ axios.get(url, {_CLIENT_AUTH_OBJECT}); }}", true);
		yield return Same(_CLIENT_AUTH_ID, "annotation-before-the-callee", $"const r: Promise<Reply> = axios.get(url, {_CLIENT_AUTH_OBJECT});", true);
		yield return Same(_CLIENT_AUTH_ID, "return-annotation-around-a-call", $"function f(): Promise<Reply> {{ return axios.get(url, {_CLIENT_AUTH_OBJECT}); }}", true);
		yield return Same(_CLIENT_AUTH_ID, "jsx-text-with-a-tag-after-a-name", "<p>request<b>(see)</b> { auth: { username: u, password: p } }</p>", false);
		yield return Same(_CLIENT_AUTH_ID, "plain-call", $"axios.get(url, {_CLIENT_AUTH_OBJECT});", true);
		yield return Same(_CLIENT_AUTH_ID, "generic-call-without-the-auth-key", "axios.get<Reply>(url, { headers: { Accept: 'x' } });", false);
	}

	private static IEnumerable<Case> UrlCases()
	{
		yield return Group(_URL_ID, "get-with-a-userinfo-url", $"axios.get<Reply>({_USERINFO_URL});");
		yield return Group(_URL_ID, "fetch-depth-two", $"fetch<Reply<Item>>({_USERINFO_URL});");
		yield return Group(_URL_ID, "new-request", $"new Request<Reply>({_USERINFO_URL});");
		yield return Tight(_URL_ID, "tight-comparison-chain-on-fetch", $"fetch<a>({_USERINFO_URL});");
		yield return Same(_URL_ID, "depth-four", $"axios.get<A<B<C<D>>>>({_USERINFO_URL});", false);
		yield return Same(_URL_ID, "no-userinfo", "axios.get<Reply>('https://host.example/path');", false);
		yield return Same(_URL_ID, "spaced-comparison-chain", $"fetch < a > ({_USERINFO_URL});", false);
		yield return Same(_URL_ID, "plain-call", $"http.get({_USERINFO_URL}, handler);", true);
		yield return Same(_URL_ID, "annotation-after-the-callee-name-on-a-declaration", $"const r: Reply<Item> = fetch({_USERINFO_URL});", true);
	}

	private static IEnumerable<Case> GotCases()
	{
		yield return Group(_GOT_ID, "depth-one", $"got<Reply>(url, {_GOT_OPTIONS});");
		yield return Group(_GOT_ID, "method-depth-two", $"got.post<Reply<Item>>(url, {_GOT_OPTIONS});");
		yield return Group(_GOT_ID, "depth-three", $"got<A<B<C>>>(url, {_GOT_OPTIONS});");
		yield return Tight(_GOT_ID, "tight-comparison-chain", $"got<a>(url, {_GOT_OPTIONS});");
		yield return Same(_GOT_ID, "depth-four", $"got<A<B<C<D>>>>(url, {_GOT_OPTIONS});", false);
		yield return Same(_GOT_ID, "password-missing", "got<Reply>(url, { username: u });", false);
		yield return Same(_GOT_ID, "spaced-comparison-chain", $"got < a > (url, {_GOT_OPTIONS});", false);
		yield return Same(_GOT_ID, "plain-call", $"got(url, {_GOT_OPTIONS});", true);
	}

	private static IEnumerable<Case> RequestCases()
	{
		yield return Group(_REQUEST_ID, "depth-one", $"request<Reply>(url, {_REQUEST_AUTH_OBJECT});");
		yield return Group(_REQUEST_ID, "method-depth-two", $"request.get<A<B>>(url, {_REQUEST_AUTH_OBJECT});");
		yield return Group(_REQUEST_ID, "depth-three", $"request<A<B<C>>>(url, {_REQUEST_AUTH_OBJECT});");
		yield return Tight(_REQUEST_ID, "tight-comparison-chain", $"request<a>(url, {_REQUEST_AUTH_OBJECT});");
		yield return Same(_REQUEST_ID, "depth-four", $"request<A<B<C<D>>>>(url, {_REQUEST_AUTH_OBJECT});", false);
		yield return Same(_REQUEST_ID, "send-immediately-true", "request<Reply>(url, { auth: { user: u, pass: p, sendImmediately: true } });", false);
		yield return Same(_REQUEST_ID, "spaced-comparison-chain", $"request < a > (url, {_REQUEST_AUTH_OBJECT});", false);
		yield return Same(_REQUEST_ID, "plain-call", $"request(url, {_REQUEST_AUTH_OBJECT});", true);
	}

	private static Case Group(string id, string name, string text)
	{
		return new Case(id, name, text, ReadNow: true, ReadBefore: false, CaseKind.TypeScriptGenericCall);
	}

	private static Case Tight(string id, string name, string text)
	{
		return new Case(id, name, text, ReadNow: true, ReadBefore: false, CaseKind.TightComparisonChain);
	}

	private static Case Same(string id, string name, string text, bool read)
	{
		return new Case(id, name, text, read, read, CaseKind.Unchanged);
	}

	private static Regex ShippedExpression(string id)
	{
		return JavaScriptRegisteredExpressions.Analyzer.DetectionRulesByPatternClass[id].Expression;
	}

	internal static string PreChangePattern(string id)
	{
		string shipped = ShippedExpression(id).ToString();

		Assert.Equal(_ONCE, CountOccurrences(shipped, GROUP_TEXT));

		return shipped.Replace(GROUP_TEXT, string.Empty, StringComparison.Ordinal);
	}

	internal static Regex PreChangeExpression(string id)
	{
		Regex shipped = ShippedExpression(id);

		return new Regex(PreChangePattern(id), shipped.Options, shipped.MatchTimeout);
	}

	private static int CountOccurrences(string text, string fragment)
	{
		int count = 0;
		int position = text.IndexOf(fragment, StringComparison.Ordinal);

		while (position >= 0)
		{
			count++;
			position = text.IndexOf(fragment, position + fragment.Length, StringComparison.Ordinal);
		}

		return count;
	}
}
