using System.Collections.Immutable;
using System.Text.RegularExpressions;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Recognition;
using MigrateAuth.Cli.Tests.Recognition;

using Xunit;
using Xunit.Abstractions;

namespace MigrateAuth.Cli.Tests.Catalog;

/// <summary>
/// THE SEMANTIC CHECKS OF THE K6 TAIL REFUSAL OF `JS-004`, run over the SHIPPED expression of the analyzer (<see cref="JavaScriptRegisteredExpressions"/>). The expression without the refusal is rebuilt from the shipped
/// pattern by removing the refusal, once, so the comparison is between the binary and the same binary without the refusal, never between the binary and a copy.
///
/// EVERY CASE STATES THE RESULT WITHOUT THE REFUSAL AND THE RESULT WITH IT, FOR THE `JavaScript` ANALYZER THAT SHARES THE EXPRESSION. <see cref="CaseKind.Unchanged"/>: the result is the same with and without it (a `Basic` literal
/// ended by a closing brace, a comma or a line end, a doubled vertical bar, a template hole with another body, K5 and every other carrier, a literal that does not close within the bound). <see cref="CaseKind.Refused"/>:
/// the refused tail, read without the refusal and not with it. No case is read with it and not without it. The forms a tail refusal cannot separate are the unchanged cases that end a literal with a closing brace, a comma or a line end.
/// </summary>
public sealed class JavaScriptK6TailRefusalTests
{
	private const string _EXPRESSION_ID = "JS-004";
	private const string _UNTYPED_CLIENT_PATH = "src/client.js";
	private const string _LITERAL_BOUND_TEXT = "300";
	private const string _BLANKS_BOUND_TEXT = "8";
	private const string _REFUSAL_TEXT =
		$@"(?![\x27\x22`](?i:basic)(?!\w)[^\x27\x22`\n]{{0,{_LITERAL_BOUND_TEXT}}}[\x27\x22`][ \t]{{0,{_BLANKS_BOUND_TEXT}}}(?:;|\|(?!\|)))(?!`(?i:basic)[ \t]{{0,{_BLANKS_BOUND_TEXT}}}\$\{{[ \t]{{0,{_BLANKS_BOUND_TEXT}}}(?:string|number)[ \t]{{0,{_BLANKS_BOUND_TEXT}}}\}})";
	private const int _LITERAL_BOUND = 300;
	private const int _ONCE = 1;
	private const int _NEVER = 0;
	private const string _FILLER_UNIT = "x";
	private const string _K6_JS_PREFIX = "const o = { ";
	private const string _K6_JS_SUFFIX = " };";

	internal enum CaseKind
	{
		Unchanged,
		Refused
	}

	internal sealed record Case(string Name, string Text, bool ReadAfter, bool ReadBefore, CaseKind Kind);

	private static readonly ImmutableArray<Case> _CASES = [.. BuildCases()];

	private readonly ITestOutputHelper _output;

	public JavaScriptK6TailRefusalTests(ITestOutputHelper output)
	{
		_output = output;
	}

	[Fact]
	public void OnlyTheExpressionOfJs004CarriesTheRefusalExactlyOnce()
	{
		foreach (KeyValuePair<string, DetectionRule> rule in JavaScriptRegisteredExpressions.Analyzer.DetectionRulesByPatternClass)
		{
			int occurrences = CountOccurrences(rule.Value.Expression.ToString(), _REFUSAL_TEXT);

			Assert.Equal(rule.Key == _EXPRESSION_ID ? _ONCE : _NEVER, occurrences);
		}
	}

	[Fact]
	public void TheRefusalIsTwoNegativeLookAheadsOverAGreedyBoundedClassWithNoLazyLoopAndNoUnboundedQuantifier()
	{
		Assert.Equal(_LITERAL_BOUND, int.Parse(_LITERAL_BOUND_TEXT, System.Globalization.CultureInfo.InvariantCulture));
		Assert.False(Regex.IsMatch(_REFUSAL_TEXT, @"\}\?|[*+]"), "The refusal carries a lazy or an unbounded quantifier.");
		Assert.StartsWith("(?!", _REFUSAL_TEXT, StringComparison.Ordinal);
	}

	[Fact]
	public void TheExpressionBeforeTheChangeIsTheShippedOneWithoutTheRefusalAndTheDeltaIsRecorded()
	{
		string now = ShippedExpression().ToString();
		string before = PreChangePattern();

		Assert.NotEqual(now, before);
		Assert.Equal(now.Length - _REFUSAL_TEXT.Length, before.Length);
		_output.WriteLine($"{_EXPRESSION_ID}: the pre-change pattern is the shipped pattern with the {_REFUSAL_TEXT.Length}-character refusal removed once, after the K6 key, the optional tag and the blanks, before the value.");
	}

	[Fact]
	public void EveryCaseIsReadAfterAsStatedAndWasReadBeforeAsStated()
	{
		List<string> wrong = [];

		foreach (Case sample in _CASES)
		{
			bool after = ShippedExpression().IsMatch(sample.Text);
			bool before = PreChangeExpression().IsMatch(sample.Text);

			if (after != sample.ReadAfter || before != sample.ReadBefore)
			{
				wrong.Add($"{sample.Name}: after {after} (stated {sample.ReadAfter}), before {before} (stated {sample.ReadBefore})");
			}

			_output.WriteLine($"{sample.Kind} before {sample.ReadBefore} after {sample.ReadAfter} {sample.Name}");
		}

		Assert.True(wrong.Count == _NEVER, string.Join(Environment.NewLine, wrong));
	}

	[Fact]
	public void TheKindOfEveryCaseAgreesWithItsResultsAndNoCaseGainsAResult()
	{
		foreach (Case sample in _CASES)
		{
			Assert.False(sample.ReadAfter && !sample.ReadBefore, $"{sample.Name}: a result was gained.");

			if (sample.Kind == CaseKind.Unchanged)
			{
				Assert.True(sample.ReadAfter == sample.ReadBefore, $"{sample.Name}: an unchanged case moved.");
			}
			else
			{
				Assert.True(sample.ReadBefore && !sample.ReadAfter, $"{sample.Name}: a refused case must be read before and not after.");
			}
		}
	}

	[Fact]
	public void TheGeneratedAndTheInterpretedFormsOfTheExpressionAgreeOnEveryCase()
	{
		Regex generated = ShippedExpression();
		Regex interpreted = new(generated.ToString(), generated.Options, generated.MatchTimeout);

		foreach (Case sample in _CASES)
		{
			Match fromGenerated = generated.Match(sample.Text);
			Match fromInterpreted = interpreted.Match(sample.Text);

			Assert.Equal(fromInterpreted.Success, fromGenerated.Success);
			Assert.Equal(fromInterpreted.Index, fromGenerated.Index);
			Assert.Equal(fromInterpreted.Length, fromGenerated.Length);
		}
	}

	[Fact]
	public void AJavaScriptFileWithEveryNonRefusedTailKeepsItsFindingAndEveryRefusedTailLosesIt()
	{
		foreach (Case sample in _CASES)
		{
			ImmutableArray<RecognizedConstruct> constructs = new JavaScriptRecognitionHarness().Recognize(sample.Text, _UNTYPED_CLIENT_PATH);
			bool read = constructs.Any(construct => construct.PatternClassIdentifier == _EXPRESSION_ID);

			Assert.True(read == sample.ReadAfter, $"{sample.Name}: the analyzer reads {read}, the expression reads {sample.ReadAfter}.");
		}
	}

	private static IEnumerable<Case> BuildCases()
	{
		return [.. RefusedCases(), .. NotRefusedCases(), .. OtherCarrierCases()];
	}

	private static IEnumerable<Case> RefusedCases()
	{
		yield return Refused("semicolon", K6("'Basic';"));
		yield return Refused("semicolon-after-blanks", K6("'Basic' \t;"));
		yield return Refused("semicolon-after-a-literal-with-a-credential", K6("'Basic dGVzdDp0ZXN0';"));
		yield return Refused("semicolon-after-a-double-quoted-literal", K6("\"Basic\";"));
		yield return Refused("single-bar-and-a-space", K6("'Basic' | 'Bearer'"));
		yield return Refused("single-bar-with-no-blank", K6("'Basic'|'Bearer'"));
		yield return Refused("single-bar-after-blanks", K6("'Basic'  |"));
		yield return Refused("template-hole-string", K6("`Basic ${string}`"));
		yield return Refused("template-hole-number", K6("`Basic ${number}`"));
		yield return Refused("template-hole-string-with-blanks", K6("`Basic ${ string }`"));
		yield return Refused("template-hole-string-after-a-tag", K6("tag`Basic ${string}`"));
		yield return Refused("template-hole-string-then-more-text", K6("`Basic ${string} tail`"));
		yield return Refused("template-literal-then-semicolon", K6("`Basic ${token}`;"));
		yield return Refused("quoted-key-with-a-semicolon", "const o = { 'Authorization': 'Basic'; };");
		yield return Refused("literal-closing-at-the-bound-then-semicolon", K6($"'Basic {Filler(_LITERAL_BOUND - _ONCE)}';"));
	}

	private static IEnumerable<Case> NotRefusedCases()
	{
		yield return Same("closing-brace-after-the-literal", K6("'Basic'"), true);
		yield return Same("comma-after-the-literal", "const o = { Authorization: 'Basic', other: 1 };", true);
		yield return Same("line-end-after-the-literal", "const o = {\n\tAuthorization: 'Basic'\n};", true);
		yield return Same("literal-with-a-credential-and-a-closing-brace", K6("'Basic dGVzdDp0ZXN0'"), true);
		yield return Same("doubled-vertical-bar", K6("'Basic' || fallback"), true);
		yield return Same("doubled-vertical-bar-after-blanks", K6("'Basic'  || fallback"), true);
		yield return Same("template-hole-with-another-body", K6("`Basic ${token}`"), true);
		yield return Same("template-hole-with-a-longer-body", K6("`Basic ${stringValue}`"), true);
		yield return Same("template-hole-string-inside-a-call", K6("`Basic ${string(x)}`"), true);
		yield return Same("string-concatenation", K6("'Basic ' + encoded"), true);
		yield return Same("newline-before-the-semicolon", "const o = {\n\tAuthorization: 'Basic'\n;\n};", true);
		yield return Same("literal-not-closing-within-the-bound-then-semicolon", K6($"'Basic {Filler(_LITERAL_BOUND)}';"), true);
		yield return Same("environment-fallback-literal-then-semicolon", K6("process.env.TOKEN || 'Basic';"), true);
		yield return Same("environment-fallback-literal-then-single-bar", K6("process.env.TOKEN || 'Basic' | x"), false);
		yield return Same("another-scheme-then-semicolon", K6("'Bearer';"), false);
		yield return Same("proxy-key-then-semicolon", "const o = { 'Proxy-Authorization': 'Basic'; };", false);
	}

	private static IEnumerable<Case> OtherCarrierCases()
	{
		yield return Same("k5-assignment-then-semicolon", "headers.Authorization = 'Basic';", true);
		yield return Same("k5-bracket-assignment-then-semicolon", "headers['Authorization'] = 'Basic';", true);
		yield return Same("k2-setter-then-single-bar", "request.setHeader('Authorization', 'Basic' | x);", true);
		yield return Same("k4-set-then-template-hole-string", "h.set('Authorization', `Basic ${string}`);", true);
		yield return Same("k1-headers-member-then-semicolon", "const o = { headers: { Authorization: 'Basic'; } };", true);
		yield return Same("k3-new-headers-then-single-bar", "new Headers({ Authorization: 'Basic' | x });", true);
	}

	private static string K6(string value)
	{
		return $"{_K6_JS_PREFIX}Authorization: {value}{_K6_JS_SUFFIX}";
	}

	private static string Filler(int count)
	{
		return new string(_FILLER_UNIT[_NEVER], count);
	}

	private static Case Refused(string name, string text)
	{
		return new Case(name, text, ReadAfter: false, ReadBefore: true, CaseKind.Refused);
	}

	private static Case Same(string name, string text, bool read)
	{
		return new Case(name, text, read, read, CaseKind.Unchanged);
	}

	private static Regex ShippedExpression()
	{
		return JavaScriptRegisteredExpressions.Analyzer.DetectionRulesByPatternClass[_EXPRESSION_ID].Expression;
	}

	internal static string PreChangePattern()
	{
		string shipped = ShippedExpression().ToString();

		Assert.Equal(_ONCE, CountOccurrences(shipped, _REFUSAL_TEXT));

		return shipped.Replace(_REFUSAL_TEXT, string.Empty, StringComparison.Ordinal);
	}

	internal static Regex PreChangeExpression()
	{
		Regex shipped = ShippedExpression();

		return new Regex(PreChangePattern(), shipped.Options, shipped.MatchTimeout);
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
