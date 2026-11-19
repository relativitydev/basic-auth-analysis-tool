using System.Collections.Immutable;
using System.Text.RegularExpressions;

using MigrateAuth.Cli.Recognition;
using MigrateAuth.Cli.Tests.Recognition;

using Xunit;

namespace MigrateAuth.Cli.Tests.Catalog;

/// <summary>
/// The declared subsumption of the header classes over the encoder class. The canonical nested form of a Basic header is
/// one construct, so it gives one finding, at the header class: the span of GO-002, GO-003 and GO-004 covers the encoder call when the value is built with the encoder in the same expression, so
/// the overlap with GO-006 holds. A split construction (the encoder in one statement, the header in another) is two facts at two places and gives two findings. The
/// value that is not built with the encoder, the near-misses and every other sample are matched as before: the widening is a tail that is optional.
/// </summary>
public sealed class GoNestedEncoderSubsumptionTests
{
	private const string _HEADER_CLASS = "GO-002";
	private const string _TYPED_LITERAL_CLASS = "GO-003";
	private const string _MAP_CLASS = "GO-004";
	private const string _ENCODER_CLASS = "GO-006";
	private const string _ENCODER_OPENING = "EncodeToString(";

	[Theory]
	[InlineData("request.Header.Set(\"Authorization\", \"Basic \"+base64.StdEncoding.EncodeToString([]byte(userName+\":\"+secret)))\n", _HEADER_CLASS)]
	[InlineData("request.Header.Add(\"Authorization\", \"Basic \" + base64.StdEncoding.EncodeToString([]byte(userName + \":\" + secret)))\n", _HEADER_CLASS)]
	[InlineData("request.Header.Set(\"Authorization\", \"Basic \"+b64.RawStdEncoding.EncodeToString([]byte(userName+\":\"+secret)))\n", _HEADER_CLASS)]
	[InlineData("request.Header.Set(\"Authorization\", fmt.Sprintf(\"Basic %s\", base64.StdEncoding.EncodeToString([]byte(userName+\":\"+secret))))\n", _HEADER_CLASS)]
	[InlineData("request.Header[\"Authorization\"] = []string{\"Basic \" + base64.StdEncoding.EncodeToString([]byte(userName+\":\"+secret))}\n", _HEADER_CLASS)]
	[InlineData("headers := http.Header{\"Authorization\": {\"Basic \" + base64.StdEncoding.EncodeToString([]byte(userName+\":\"+secret))}}\n", _TYPED_LITERAL_CLASS)]
	[InlineData("flat := map[string]string{\"Authorization\": \"Basic \" + base64.StdEncoding.EncodeToString([]byte(userName+\":\"+secret))}\n", _MAP_CLASS)]
	public void TheNestedFormGivesOneFindingAtTheHeaderClass(string source, string headerClass)
	{
		ImmutableArray<RecognizedConstruct> constructs = new GoRecognitionHarness().Recognize(source);

		RecognizedConstruct only = Assert.Single(constructs);
		Assert.Equal(headerClass, only.PatternClassIdentifier);
	}

	[Theory]
	[InlineData("request.Header[\"Authorization\"] = []string{\"Basic \", base64.StdEncoding.EncodeToString([]byte(userName+\":\"+secret))}\n", _HEADER_CLASS)]
	[InlineData("request.Header[\"Authorization\"] = append(request.Header[\"Authorization\"], \"Basic \", base64.StdEncoding.EncodeToString([]byte(userName+\":\"+secret)))\n", _HEADER_CLASS)]
	[InlineData("headers := http.Header{\"Authorization\": {\"Basic \", base64.StdEncoding.EncodeToString([]byte(userName+\":\"+secret))}}\n", _TYPED_LITERAL_CLASS)]
	[InlineData("flat := map[string][]string{\"Authorization\": {\"Basic\", base64.StdEncoding.EncodeToString([]byte(userName+\":\"+secret))}}\n", _MAP_CLASS)]
	public void AListFormWithACommaGivesTwoFindingsAndTheCommaDoesNotExtendTheHeaderClass(string source, string headerClass)
	{
		ImmutableArray<RecognizedConstruct> constructs = new GoRecognitionHarness().Recognize(source);
		Match header = ExpressionOf(headerClass).Match(source);

		Assert.Equal([headerClass, _ENCODER_CLASS], constructs.Select(construct => construct.PatternClassIdentifier).Order(StringComparer.Ordinal));
		Assert.True(header.Success);
		Assert.DoesNotContain(_ENCODER_OPENING, header.Value, StringComparison.Ordinal);
	}

	[Theory]
	[InlineData("request.Header.Set(\"Authorization\", \"Basic \"+base64.StdEncoding.EncodeToString([]byte(userName+\":\"+secret)))\n", _HEADER_CLASS)]
	[InlineData("headers := http.Header{\"Authorization\": {\"Basic \" + base64.StdEncoding.EncodeToString([]byte(userName+\":\"+secret))}}\n", _TYPED_LITERAL_CLASS)]
	[InlineData("flat := map[string]string{\"Authorization\": \"Basic \" + base64.StdEncoding.EncodeToString([]byte(userName+\":\"+secret))}\n", _MAP_CLASS)]
	public void TheSpanOfTheHeaderClassCoversTheEncoderCallInTheNestedForm(string source, string headerClass)
	{
		Match header = ExpressionOf(headerClass).Match(source);
		Match encoder = ExpressionOf(_ENCODER_CLASS).Match(source);
		int openingEnd = source.IndexOf(_ENCODER_OPENING, StringComparison.Ordinal) + _ENCODER_OPENING.Length;

		Assert.True(header.Success);
		Assert.True(encoder.Success);
		Assert.True(header.Index < encoder.Index);
		Assert.Equal(openingEnd, header.Index + header.Length);
		Assert.True(header.Index + header.Length > encoder.Index);
	}

	[Theory]
	[InlineData("pair := base64.StdEncoding.EncodeToString([]byte(userName + \":\" + secret))\nrequest.Header.Set(\"Authorization\", \"Basic \"+pair)\n", _HEADER_CLASS)]
	[InlineData("pair := base64.StdEncoding.EncodeToString([]byte(userName + \":\" + secret))\nheaders := http.Header{\"Authorization\": {\"Basic \" + pair}}\n", _TYPED_LITERAL_CLASS)]
	[InlineData("pair := base64.StdEncoding.EncodeToString([]byte(userName + \":\" + secret))\nflat := map[string]string{\"Authorization\": \"Basic \" + pair}\n", _MAP_CLASS)]
	public void ASplitConstructionGivesTwoFindings(string source, string headerClass)
	{
		ImmutableArray<RecognizedConstruct> constructs = new GoRecognitionHarness().Recognize(source);

		Assert.Equal([headerClass, _ENCODER_CLASS], constructs.Select(construct => construct.PatternClassIdentifier).Order(StringComparer.Ordinal));
	}

	[Theory]
	[InlineData("request.Header.Set(\"Authorization\", \"Basic \" base64.StdEncoding.EncodeToString([]byte(userName+\":\"+secret)))\n", _HEADER_CLASS)]
	[InlineData("headers := http.Header{\"Authorization\": {\"Basic \" base64.StdEncoding.EncodeToString([]byte(userName+\":\"+secret))}}\n", _TYPED_LITERAL_CLASS)]
	[InlineData("flat := map[string]string{\"Authorization\": \"Basic \" base64.StdEncoding.EncodeToString([]byte(userName+\":\"+secret))}\n", _MAP_CLASS)]
	public void AJuxtapositionWithoutAnOperatorDoesNotExtendTheHeaderClassAndGivesTwoFindings(string source, string headerClass)
	{
		ImmutableArray<RecognizedConstruct> constructs = new GoRecognitionHarness().Recognize(source);
		Match header = ExpressionOf(headerClass).Match(source);

		Assert.Equal([headerClass, _ENCODER_CLASS], constructs.Select(construct => construct.PatternClassIdentifier).Order(StringComparer.Ordinal));
		Assert.True(header.Success);
		Assert.DoesNotContain(_ENCODER_OPENING, header.Value, StringComparison.Ordinal);
	}

	[Theory]
	[InlineData("request.Header.Set(\"Authorization\", \"Bearer \"+base64.StdEncoding.EncodeToString([]byte(userName+\":\"+secret)))\n")]
	[InlineData("request.Header.Set(\"Proxy-Authorization\", \"Basic \"+base64.StdEncoding.EncodeToString([]byte(userName+\":\"+secret)))\n")]
	[InlineData("request.Header.Set(\"Authorization\", \"Digest \"+base64.StdEncoding.EncodeToString([]byte(userName+\":\"+secret)))\n")]
	[InlineData("headers := http.Header{\"Accept\": {\"Basic \" + base64.StdEncoding.EncodeToString([]byte(userName+\":\"+secret))}}\n")]
	[InlineData("flat := map[string]string{\"X-Token\": \"Basic \" + base64.StdEncoding.EncodeToString([]byte(userName+\":\"+secret))}\n")]
	public void TheNearMissesAreStillRefusedByTheHeaderClasses(string source)
	{
		Assert.DoesNotMatch(ExpressionOf(_HEADER_CLASS), source);
		Assert.DoesNotMatch(ExpressionOf(_TYPED_LITERAL_CLASS), source);
		Assert.DoesNotMatch(ExpressionOf(_MAP_CLASS), source);
	}

	[Theory]
	[InlineData(_HEADER_CLASS, "request.Header.Set(\"Authorization\", \"Basic \"+base64.StdEncoding.EncodeToString([]byte(userName+\":\"+secret)))\n")]
	[InlineData(_HEADER_CLASS, "request.Header.Set(\"Authorization\", fmt.Sprintf(\"Basic %s\", base64.StdEncoding.EncodeToString([]byte(a+\":\"+b))))\n")]
	[InlineData(_HEADER_CLASS, "request.Header.Set(\"Authorization\", \"Basic \" base64.StdEncoding.EncodeToString([]byte(a+\":\"+b)))\n")]
	[InlineData(_HEADER_CLASS, "request.Header.Set(\"Authorization\", \"Basic dXNlcjpwYXNz\")\n")]
	[InlineData(_HEADER_CLASS, "request.Header.Set(\"Authorization\", \"Basic \"+encoded)\n")]
	[InlineData(_HEADER_CLASS, "request.Header.Set(\"Authorization\", \"Basic \" + other.Encode(x))\n")]
	[InlineData(_TYPED_LITERAL_CLASS, "headers := http.Header{\"Authorization\": {\"Basic \" + base64.StdEncoding.EncodeToString([]byte(a+\":\"+b))}}\n")]
	[InlineData(_MAP_CLASS, "flat := map[string]string{\"Authorization\": \"Basic \" + base64.StdEncoding.EncodeToString([]byte(a+\":\"+b))}\n")]
	public void TheGeneratedAndTheInterpretedFormAgreeOnTheMatchAndItsGroups(string patternClass, string source)
	{
		Regex generated = ExpressionOf(patternClass);
		Regex interpreted = GoRegisteredExpressions.Interpreted(generated);
		Match fromGenerated = generated.Match(source);
		Match fromInterpreted = interpreted.Match(source);

		Assert.Equal(fromGenerated.Success, fromInterpreted.Success);
		Assert.Equal(fromGenerated.Index, fromInterpreted.Index);
		Assert.Equal(fromGenerated.Length, fromInterpreted.Length);
		Assert.Equal(fromGenerated.Groups["credential"].Value, fromInterpreted.Groups["credential"].Value);
		Assert.Equal(fromGenerated.Groups["construct"].Value, fromInterpreted.Groups["construct"].Value);
	}

	[Fact]
	public void AValueNotBuiltWithTheEncoderKeepsItsSpanAndItsCredential()
	{
		Match scheme = ExpressionOf(_HEADER_CLASS).Match("request.Header.Set(\"Authorization\", \"Basic \"+encoded)\n");
		Match credential = ExpressionOf(_HEADER_CLASS).Match("request.Header.Set(\"Authorization\", \"Basic dXNlcjpwYXNz\")\n");

		Assert.EndsWith("\"Basic", scheme.Value, StringComparison.Ordinal);
		Assert.Equal("dXNlcjpwYXNz", credential.Groups["credential"].Value);
		Assert.EndsWith("\"Basic dXNlcjpwYXNz", credential.Value, StringComparison.Ordinal);
	}

	private static Regex ExpressionOf(string patternClass)
	{
		return GoRegisteredExpressions.Classes.Single(pair => pair.Identifier == patternClass).Expression;
	}
}
