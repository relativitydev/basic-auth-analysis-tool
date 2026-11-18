using System.Collections.Immutable;
using System.Reflection;
using System.Text.RegularExpressions;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Catalog.Languages;
using MigrateAuth.Cli.Recognition;
using MigrateAuth.Cli.Tests.Recognition;

using Xunit;

namespace MigrateAuth.Cli.Tests.Catalog;

/// <summary>
/// THE SPAN OF THE `Python` HEADER CLASS OVER THE ENCODER IT IS BUILT FROM. The header class (PY-003) reads the carrier, the header name and the scheme word, and its match now continues over the concatenation or the
/// replacement field and the encoder callee that follow the word, so the encoder class (PY-007) and the quoted-key class (PY-014), which anchor at or after the callee, overlap it and are subsumed: one finding, at the header.
/// An encoder in another statement is not within the value and stays a second finding. The equivalence test reads the expression as it stood before the tail, from the vocabulary constants, and shows that every sample
/// without an encoder tail matches exactly as before.
/// </summary>
public sealed class PythonHeaderEncoderSpanTests
{
	private const string _VOCABULARY_TYPE_NAME = "MigrateAuth.Cli.Catalog.Languages.PythonPatternExpressions";
	private const BindingFlags _NON_PUBLIC_STATIC = BindingFlags.NonPublic | BindingFlags.Static;

	[Theory]
	[InlineData("response = requests.get(url, headers={\"Authorization\": \"Basic \" + b64encode(user_name + \":\" + secret_value)})\n")]
	[InlineData("response = requests.get(url, headers={\"Authorization\": \"Basic \" + base64.b64encode(f\"{user_name}:{secret_value}\".encode()).decode()})\n")]
	[InlineData("session.headers[\"Authorization\"] = \"Basic \" + base64.standard_b64encode(user_name + \":\" + secret_value)\n")]
	[InlineData("headers = {\"Authorization\": f\"Basic {base64.b64encode(user_name + ':' + secret_value)}\"}\n")]
	public void AHeaderValueBuiltFromAnEncoderIsOneFindingAtTheHeaderClassAtHigh(string source)
	{
		ImmutableArray<RecognizedConstruct> constructs = new PythonRecognitionHarness().Recognize(source);

		RecognizedConstruct construct = Assert.Single(constructs);
		Assert.Equal("PY-003", construct.PatternClassIdentifier);
		Assert.Equal(ConfidenceLevel.High, construct.Level);
	}

	[Fact]
	public void AnEncoderInAnotherStatementThanItsHeaderIsTwoFindings()
	{
		const string source = "token = base64.b64encode(f\"{user_name}:{secret_value}\".encode()).decode()\nheaders = {\"Authorization\": \"Basic \" + token}\n";
		ImmutableArray<RecognizedConstruct> constructs = new PythonRecognitionHarness().Recognize(source);

		Assert.Equal(["PY-003", "PY-007"], constructs.Select(construct => construct.PatternClassIdentifier).Order(StringComparer.Ordinal));
	}

	[Fact]
	public void AnEncoderOutsideACarrierMappingIsOneFindingAtTheEncoderClassAndTheQuotedKeyClassIsSubsumed()
	{
		const string source = "def build():\n\treturn {\"Authorization\": \"Basic \" + b64encode(user_name + \":\" + secret_value)}\n";
		ImmutableArray<RecognizedConstruct> constructs = new PythonRecognitionHarness().Recognize(source);

		RecognizedConstruct construct = Assert.Single(constructs);
		Assert.Equal("PY-007", construct.PatternClassIdentifier);
		Assert.Equal(ConfidenceLevel.Medium, construct.Level);
	}

	[Fact]
	public void TheHeaderSpanReachesTheEncoderCalleeOfTheCombinedForm()
	{
		const string source = "response = requests.get(url, headers={\"Authorization\": \"Basic \" + b64encode(user_name + \":\" + secret_value)})\n";
		PythonRecognitionHarness harness = new();
		FileRecognitionContext context = harness.ContextFor(source);
		Regex header = PythonShippedAnalyzer.Instance.DetectionRulesByPatternClass["PY-003"].Expression;
		Regex encoder = PythonShippedAnalyzer.Instance.DetectionRulesByPatternClass["PY-007"].Expression;
		Match headerMatch = header.Match(context.NormalizedText);
		Match encoderMatch = encoder.Match(context.NormalizedText);

		Assert.True(headerMatch.Success && encoderMatch.Success);
		Assert.True(headerMatch.Index + headerMatch.Length > encoderMatch.Index, "the header span reaches the encoder callee");
		Assert.True(encoderMatch.Index >= headerMatch.Index);
	}

	[Theory]
	[InlineData("headers = {\"Authorization\": \"Bearer \" + b64encode(token)}\n")]
	[InlineData("headers = {\"Proxy-Authorization\": \"Basic \" + b64encode(user_name + \":\" + secret_value)}\n")]
	[InlineData("value = request.headers[\"Authorization\"] == \"Basic \" + b64encode(expected)\n")]
	public void ANearMissStillProducesNoHeaderFinding(string source)
	{
		ImmutableArray<RecognizedConstruct> constructs = new PythonRecognitionHarness().Recognize(source);

		Assert.DoesNotContain(constructs, construct => construct.PatternClassIdentifier == "PY-003" && construct.Level == ConfidenceLevel.High);
	}

	[Theory]
	[InlineData("PY-003", "response = requests.get(url, headers={\"Authorization\": \"Basic \" + b64encode(user_name + \":\" + secret_value)})")]
	[InlineData("PY-003", "headers = {\"Authorization\": f\"Basic {base64.b64encode(user_name + ':' + secret_value)}\"}")]
	[InlineData("PY-014", "return {\"Authorization\": \"Basic \" + b64encode(user_name + \":\" + secret_value)}")]
	[InlineData("PY-003", "headers = {\"Authorization\": \"Basic dXNlcjpzZWNyZXQ=\"}")]
	[InlineData("PY-014", "return {\"Authorization\": \"Basic dXNlcjpzZWNyZXQ=\"}")]
	public void TheGeneratedAndTheInterpretedEngineAgree(string patternClass, string text)
	{
		Regex generated = PythonShippedAnalyzer.Instance.DetectionRulesByPatternClass[patternClass].Expression;
		Regex interpreted = new(generated.ToString(), generated.Options, generated.MatchTimeout);
		Match fromGenerated = generated.Match(text);
		Match fromInterpreted = interpreted.Match(text);

		Assert.True(fromGenerated.Success);
		Assert.Equal(fromInterpreted.Success, fromGenerated.Success);
		Assert.Equal(fromInterpreted.Index, fromGenerated.Index);
		Assert.Equal(fromInterpreted.Length, fromGenerated.Length);
	}

	[Theory]
	[InlineData("response = requests.get(url, headers={\"Authorization\": \"Basic \" + encoded})")]
	[InlineData("session.headers[\"Authorization\"] = f\"Basic {encoded}\"")]
	[InlineData("request.add_header(\"Authorization\", \"Basic %s\" % encoded)")]
	[InlineData("headers = dict(Authorization=\"Basic {}\".format(encoded))")]
	[InlineData("session = aiohttp.ClientSession(headers={\"Authorization\": encode_basic_auth(\"user\", \"secret\")})")]
	[InlineData("session.headers.update({\"authorization\": \"basic dXNlcjpzZWNyZXQ=\"})")]
	[InlineData("connection.putheader(\"Authorization\", \"Basic dXNlcjpzZWNyZXQ=\")")]
	[InlineData("request.add_unredirected_header(\"Authorization\", \"Basic \" + encoded)")]
	[InlineData("headers = {\"Accept\": \"text/plain\", \"Authorization\": \"Basic dXNlcjpzZWNyZXQ=\"}")]
	[InlineData("headers: dict = {\"Authorization\": b\"Basic \" + encoded}")]
	[InlineData("headers = {\"Authorization\": \"Basic\" + suffix}")]
	[InlineData("headers = {\"Authorization\": \"Bearer \" + token}")]
	[InlineData("headers = {\"Authorization\": \"Basic \" + urlsafe_b64encode(user_name + \":\" + secret_value)}")]
	[InlineData("headers = {\"Authorization\": \"Basic \" + b64decode(value)}")]
	[InlineData("headers = {\"Proxy-Authorization\": \"Basic \" + token}")]
	[InlineData("value = request.headers[\"Authorization\"]")]
	[InlineData("headers = {\"Authorization\": token}")]
	[InlineData("headers = {\"Authorization\": t\"Basic {encoded}\"}")]
	public void EverySampleWithoutAnEncoderTailMatchesExactlyAsTheExpressionBeforeTheTailDid(string text)
	{
		Regex shipped = PythonShippedAnalyzer.Instance.DetectionRulesByPatternClass["PY-003"].Expression;
		Regex before = TheExpressionBeforeTheTail();
		Match now = shipped.Match(text);
		Match then = before.Match(text);

		Assert.Equal(then.Success, now.Success);
		Assert.Equal(then.Index, now.Index);
		Assert.Equal(then.Length, now.Length);
		Assert.Equal(then.Groups["credential"].Value, now.Groups["credential"].Value);
		Assert.Equal(then.Groups["construct"].Value, now.Groups["construct"].Value);
	}

	[Fact]
	public void TheTailOnlyLengthensAMatchTheEarlierExpressionAlreadyMadeAtTheSamePosition()
	{
		const string text = "headers = {\"Authorization\": \"Basic \" + b64encode(user_name + \":\" + secret_value)}";
		Match now = PythonShippedAnalyzer.Instance.DetectionRulesByPatternClass["PY-003"].Expression.Match(text);
		Match then = TheExpressionBeforeTheTail().Match(text);

		Assert.True(then.Success && now.Success);
		Assert.Equal(then.Index, now.Index);
		Assert.True(now.Length > then.Length);
		Assert.Equal("headers", now.Groups["construct"].Value);
	}

	[Fact]
	public void NoExpressionOfTheAnalyzerHasALazyQuantifierAndTheTailIsBounded()
	{
		Assert.All(PythonShippedAnalyzer.Instance.DetectionRulesByPatternClass, entry =>
			Assert.DoesNotMatch(@"[*+?}]\?", entry.Value.Expression.ToString().Replace("(?", string.Empty, StringComparison.Ordinal).Replace("\\?", string.Empty, StringComparison.Ordinal)));

		string tail = Assert.IsType<string>(ConstantOf("_BASIC_ENCODER_TAIL"));

		Assert.DoesNotContain("*", tail, StringComparison.Ordinal);
		Assert.DoesNotContain("+", tail.Replace("\\+", string.Empty, StringComparison.Ordinal), StringComparison.Ordinal);
	}

	/// <summary>The header expression as it stood before the encoder tail: the carriers and the Basic value, read from the vocabulary so no copy of the text exists in the test.</summary>
	private static Regex TheExpressionBeforeTheTail()
	{
		string pattern = Assert.IsType<string>(ConstantOf("_CARRIERS")) + Assert.IsType<string>(ConstantOf("_BASIC_VALUE"));
		Regex shipped = PythonShippedAnalyzer.Instance.DetectionRulesByPatternClass["PY-003"].Expression;

		return new Regex(pattern, shipped.Options, shipped.MatchTimeout);
	}

	private static object? ConstantOf(string name)
	{
		Type vocabulary = typeof(EmbeddedLanguageAnalyzers).Assembly.GetType(_VOCABULARY_TYPE_NAME, throwOnError: true)!;
		FieldInfo field = vocabulary.GetField(name, _NON_PUBLIC_STATIC) ?? throw new InvalidOperationException($"The expressions declare no constant named {name}.");

		return field.GetRawConstantValue();
	}
}
