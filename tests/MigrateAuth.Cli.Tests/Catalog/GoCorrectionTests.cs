using System.Collections.Immutable;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Recognition;
using MigrateAuth.Cli.Tests.Recognition;

using Xunit;

namespace MigrateAuth.Cli.Tests.Catalog;

/// <summary>
/// THE CORRECTION OF THE `Go` CATALOG AT 0.5, through the shipped analyzer and the real recognition engine. Each sentence the analyzer must meet is a test: the encoded Basic helper row of `GO-011`, the new class `GO-013`
/// and its member table, the literal flag of the multi-line `SetBasicAuth` call (conformance a), the trailing comma of `UserPassword` (conformance b), the literal flag of the `GO-002` token and of the nested whole-literal pair
/// (conformance c), the withdrawn triple of `GO-005` over `GO-008` (conformance d) and the test assertion on an encoded Basic header (decision 5). Every text is invented, with generic names.
/// </summary>
public sealed class GoCorrectionTests
{
	private const string _CLASS_HELPER = "GO-011";
	private const string _CLASS_LOCAL_HELPER = "GO-013";
	private const string _TEST_FILE_PATH = "src/orders_test.go";
	private const int _CLOSING_TAIL_BOUND = 200;
	private const int _DOTTED_SELECTOR_COUNT = 20;
	private const int _CLOSING_TAIL_BOUND_PLUS_ONE = _CLOSING_TAIL_BOUND + 1;
	private const int _BLANKS_WITHIN_THE_TAIL_BOUND = 195;
	private const int _LINE_COMMENT_FRAME_LENGTH = 4;
	private const int _BLOCK_COMMENT_FRAME_LENGTH = 6;

	private const string _BASIC_HELPER =
		"func basicHeader(userName string, secret string) string {\n\treturn \"Basic \" + base64.StdEncoding.EncodeToString([]byte(userName+\":\"+secret))\n}\n";

	private const string _ENCODED_HELPER =
		"func basicHeader(raw []byte) string {\n\tencoded := base64.StdEncoding.EncodeToString(raw)\n\treturn \"Basic \" + encoded\n}\n";

	private const string _BEARER_HELPER =
		"func bearerHeader(token string) string {\n\treturn \"Bearer \" + token\n}\n";

	private const string _SEND_ONE_LINE_TRAILING_COMMA =
		"func send() {\n\trequest.Header.Set(\"Authorization\", basicHeader(userName, secret),)\n}\n";

	private const string _SEND_MULTI_LINE_TRAILING_COMMA =
		"func send() {\n\trequest.Header.Set(\n\t\t\"Authorization\",\n\t\tbasicHeader(userName, secret),\n\t)\n}\n";

	private const string _SEND_K2_ONE_LINE_TRAILING_COMMA =
		"func send() {\n\trequest.Header[\"Authorization\"] = []string{basicHeader(userName, secret),}\n}\n";

	private const string _SEND_K2_MULTI_LINE_TRAILING_COMMA =
		"func send() {\n\trequest.Header[\"Authorization\"] = []string{\n\t\tbasicHeader(userName, secret),\n\t}\n}\n";

	private const string _SEND_APPEND_TRAILING_COMMA =
		"func send() {\n\trequest.Header[\"Authorization\"] = append(\n\t\trequest.Header[\"Authorization\"],\n\t\tbasicHeader(userName, secret),\n\t)\n}\n";

	private const string _SEND_BEARER_TRAILING_COMMA =
		"func send() {\n\trequest.Header.Set(\n\t\t\"Authorization\",\n\t\tbearerHeader(token),\n\t)\n}\n";

	private const string _SEND_EXTRA_ARGUMENT =
		"func send() {\n\trequest.Header.Set(\"Authorization\", basicHeader(userName, secret), extra)\n}\n";

	private const string _SEND_THROUGH_K1 = "func send() {\n\trequest.Header.Set(\"Authorization\", basicHeader(userName, secret))\n}\n";

	// ------------------------------------------------------------------------------------------
	// GO-011: the encoded Basic helper row
	// ------------------------------------------------------------------------------------------

	[Theory]
	[InlineData("func basicHeader(userName string, secret string) string {\n\tpair := userName + \":\" + secret\n\treturn \"Basic \" + base64.StdEncoding.EncodeToString([]byte(pair))\n}\n")]
	[InlineData("func basicHeader(pair string) string {\n\treturn \"Basic \" + base64.StdEncoding.EncodeToString([]byte(pair))\n}\n")]
	[InlineData("func basicHeader(raw []byte) string {\n\tencoded := base64.StdEncoding.EncodeToString(raw)\n\treturn \"Basic \" + encoded\n}\n")]
	[InlineData("func basicHeader(raw []byte) string {\n\treturn \"Basic \" + base64.RawStdEncoding.EncodeToString(raw)\n}\n")]
	[InlineData("func basicHeader(raw []byte) string {\n\tprefix := \"Basic \" + transport.token\n\treturn prefix + base64.StdEncoding.EncodeToString(raw)\n}\n")]
	[InlineData("func (client *Client) basicHeader(raw []byte) string {\n\treturn `Basic ` + base64.StdEncoding.EncodeToString(raw)\n}\n")]
	public void AnEncodedBasicHelperIsReadAtLowByTheDefinitionClass(string source)
	{
		ImmutableArray<RecognizedConstruct> constructs = new GoRecognitionHarness().Recognize(source);

		Assert.Contains(constructs, construct => construct.PatternClassIdentifier == _CLASS_HELPER && construct.Level == ConfidenceLevel.Low);
	}

	[Theory]
	[InlineData("func basicHeader(userName string, secret string) string {\n\treturn base64.StdEncoding.EncodeToString([]byte(userName + \":\" + secret))\n}\n")]
	[InlineData("func basicHeader(token string) string {\n\treturn fmt.Sprintf(\"Basic %s\", token)\n}\n")]
	[InlineData("func prefix(token string) string {\n\treturn \"Basic \" + token\n}\n")]
	[InlineData("func challenge(realm string) string {\n\treturn \"Basic realm=\\\"\" + realm + \"\\\"\"\n}\n")]
	[InlineData("func challenge(realm string) string {\n\tnonce := base64.StdEncoding.EncodeToString(raw)\n\treturn \"Basic realm=\\\"\" + realm + nonce + \"\\\"\"\n}\n")]
	[InlineData("func basicHeader(token string) string {\n\tencoded := base64.URLEncoding.EncodeToString(raw)\n\treturn \"Basic \" + encoded + token\n}\n")]
	[InlineData("func basicHeader() string {\n\tencoded := base64.StdEncoding.EncodeToString(raw)\n\treturn \"Basic \" + encoded\n}\n")]
	[InlineData("func basicHeader[T any](raw T) string {\n\tencoded := base64.StdEncoding.EncodeToString(data)\n\treturn \"Basic \" + encoded\n}\n")]
	[InlineData("func basicHeader(token string) string {\n\tencoded := base64.StdEncoding.EncodeToString(raw)\n\treturn \"Basic \" + encoded\n}\n")]
	public void AHelperThatTheEncodedRowDoesNotReadGivesNoDefinitionFindingAndAChallengeBuilderGivesNothing(string source)
	{
		ImmutableArray<RecognizedConstruct> constructs = new GoRecognitionHarness().Recognize(source);

		Assert.DoesNotContain(constructs, construct => construct.PatternClassIdentifier == _CLASS_HELPER);
	}

	[Theory]
	[InlineData("func challenge(realm string) string { return \"Basic realm=\\\"\" + realm + \"\\\"\" }\n")]
	[InlineData("func challenge(realm string) string {\n\tnonce := base64.StdEncoding.EncodeToString(rawNonce)\n\treturn \"Basic realm=\\\"\" + realm + \"\\\"\"\n}\n")]
	public void AServersChallengeBuilderWithOrWithoutAnEncoderOverANonceGivesNoFindingAtAll(string source)
	{
		Assert.True(new GoRecognitionHarness().Recognize(source).IsEmpty);
	}

	[Fact]
	public void TheEncodedRowReadsAHelperWhoseParameterAppearsOnAnyLineOfTheWindowAndInEitherOrderOfTheConjuncts()
	{
		string parameterOnAnotherLine = "func basicHeader(token string) string {\n\tencoded := base64.StdEncoding.EncodeToString(data)\n\n\tprefix := \"Basic \" + encoded\n\treturn log(token, prefix)\n}\n";
		string literalFirst = "func basicHeader(token string) string {\n\tprefix := \"Basic \" + token\n\treturn prefix + base64.StdEncoding.EncodeToString(data)\n}\n";

		Assert.Contains(new GoRecognitionHarness().Recognize(parameterOnAnotherLine), construct => construct.PatternClassIdentifier == _CLASS_HELPER);
		Assert.Contains(new GoRecognitionHarness().Recognize(literalFirst), construct => construct.PatternClassIdentifier == _CLASS_HELPER);
	}

	// ------------------------------------------------------------------------------------------
	// GO-013: the call to a same-file Basic helper
	// ------------------------------------------------------------------------------------------

	[Theory]
	[InlineData(_BASIC_HELPER + _SEND_THROUGH_K1)]
	[InlineData(_SEND_THROUGH_K1 + _BASIC_HELPER)]
	[InlineData(_ENCODED_HELPER + "func send() {\n\trequest.Header.Add(\"authorization\", basicHeader(raw))\n}\n")]
	[InlineData(_ENCODED_HELPER + "func send() {\n\trequest.Header[\"Authorization\"] = []string{basicHeader(raw)}\n}\n")]
	[InlineData(_ENCODED_HELPER + "func send() {\n\trequest.Header[\"Authorization\"] = append(request.Header[\"Authorization\"], basicHeader(raw))\n}\n")]
	[InlineData(_ENCODED_HELPER + "func send() {\n\trequest.Header.Set(\n\t\t\"Authorization\",\n\t\tbasicHeader(raw))\n}\n")]
	public void AK1OrK2CallToASameFileBasicHelperReadsMediumAtTheCarrierWithNoLiteralFlag(string source)
	{
		ImmutableArray<RecognizedConstruct> constructs = new GoRecognitionHarness().Recognize(source);
		RecognizedConstruct call = Assert.Single(constructs, construct => construct.PatternClassIdentifier == _CLASS_LOCAL_HELPER);

		Assert.Equal(ConfidenceLevel.Medium, call.Level);
		Assert.False(call.LiteralCredentialSignal);
		Assert.Contains(constructs, construct => construct.PatternClassIdentifier == _CLASS_HELPER && construct.Level == ConfidenceLevel.Low);
	}

	[Fact]
	public void AFileThatAddsACallToAHelperGainsExactlyOneFindingAndTheFindingsAtTheHelperAreNotChanged()
	{
		GoRecognitionHarness harness = new();
		string[] withoutTheCall = [.. harness.Recognize(_BASIC_HELPER).Select(Describe).Order(StringComparer.Ordinal)];
		string[] withTheCall = [.. harness.Recognize(_BASIC_HELPER + _SEND_THROUGH_K1).Select(Describe).Order(StringComparer.Ordinal)];

		Assert.Equal(withoutTheCall.Length + 1, withTheCall.Length);
		Assert.Equal([.. withoutTheCall.Append("GO-013:Medium").Order(StringComparer.Ordinal)], withTheCall);
		Assert.Contains("GO-011:Low", withoutTheCall);
		Assert.Contains("GO-006:Medium", withoutTheCall);
	}

	[Fact]
	public void TheCallAndTheHelperAreTwoFactsAtTwoAnchorsAndNothingSubsumesTheCall()
	{
		ImmutableArray<RecognizedConstruct> constructs = new GoRecognitionHarness().Recognize(_BASIC_HELPER + _SEND_THROUGH_K1);
		RecognizedConstruct call = Assert.Single(constructs, construct => construct.PatternClassIdentifier == _CLASS_LOCAL_HELPER);

		Assert.All(constructs.Where(construct => construct.PatternClassIdentifier != _CLASS_LOCAL_HELPER), construct => Assert.NotEqual(call.AnchorOffset, construct.AnchorOffset));
	}

	[Fact]
	public void ABearerHelperAndItsCallGiveNothingAtTheCall()
	{
		ImmutableArray<RecognizedConstruct> constructs = new GoRecognitionHarness().Recognize(_BEARER_HELPER + "func send() {\n\trequest.Header.Set(\"Authorization\", bearerHeader(token))\n}\n");

		Assert.True(constructs.IsEmpty, string.Join(", ", constructs.Select(Describe)));
	}

	[Theory]
	[InlineData("a method call", _BASIC_HELPER + "func send() {\n\trequest.Header.Set(\"Authorization\", client.basicHeader(userName, secret))\n}\n")]
	[InlineData("a package call", _BASIC_HELPER + "func send() {\n\trequest.Header.Set(\"Authorization\", pkg.basicHeader(userName, secret))\n}\n")]
	[InlineData("a function and a method of one name", _BASIC_HELPER + "func (client *Client) basicHeader(userName string, secret string) string {\n\treturn \"\"\n}\n" + _SEND_THROUGH_K1)]
	[InlineData("a repeated function", _BASIC_HELPER + _BASIC_HELPER + _SEND_THROUGH_K1)]
	[InlineData("a method on two receivers", "func (a *A) basicHeader(u string, p string) string {\n\treturn \"Basic \" + base64.StdEncoding.EncodeToString([]byte(u+\":\"+p))\n}\nfunc (b *B) basicHeader(u string, p string) string {\n\treturn \"\"\n}\n" + _SEND_THROUGH_K1)]
	[InlineData("a helper in another file", _SEND_THROUGH_K1)]
	[InlineData("a function value held in a variable", "var basicHeader = func(u string, p string) string { return \"\" }\n" + _SEND_THROUGH_K1)]
	[InlineData("a helper with no parameter", "func basicHeader() string {\n\treturn \"Basic \" + base64.StdEncoding.EncodeToString(data)\n}\nfunc send() {\n\trequest.Header.Set(\"Authorization\", basicHeader())\n}\n")]
	[InlineData("a generic helper", "func basicHeader[T any](raw T) string {\n\treturn \"Basic \" + base64.StdEncoding.EncodeToString(data)\n}\nfunc send() {\n\trequest.Header.Set(\"Authorization\", basicHeader(raw))\n}\n")]
	[InlineData("a Sprintf helper", "func basicHeader(token string) string {\n\treturn fmt.Sprintf(\"Basic %s\", token)\n}\n" + _SEND_THROUGH_K1)]
	[InlineData("a call with more than the call", _BASIC_HELPER + "func send() {\n\trequest.Header.Set(\"Authorization\", basicHeader(userName, secret) + suffix)\n}\n")]
	[InlineData("a parenthesised call", _BASIC_HELPER + "func send() {\n\trequest.Header.Set(\"Authorization\", (basicHeader(userName, secret)))\n}\n")]
	[InlineData("a header literal value", _BASIC_HELPER + "func send() {\n\theaders := http.Header{\"Authorization\": {basicHeader(userName, secret)}}\n}\n")]
	[InlineData("an unnamed map value", _BASIC_HELPER + "func send() {\n\tflat := map[string]string{\"Authorization\": basicHeader(userName, secret)}\n}\n")]
	[InlineData("another header name", _BASIC_HELPER + "func send() {\n\trequest.Header.Set(\"X-Api-Key\", basicHeader(userName, secret))\n}\n")]
	[InlineData("a proxy header", _BASIC_HELPER + "func send() {\n\trequest.Header.Set(\"Proxy-Authorization\", basicHeader(userName, secret))\n}\n")]
	public void AFormTheCatalogDeclaresGivesNoFindingAtTheCall(string form, string source)
	{
		ImmutableArray<RecognizedConstruct> constructs = new GoRecognitionHarness().Recognize(source);

		Assert.False(constructs.Any(construct => construct.PatternClassIdentifier == _CLASS_LOCAL_HELPER), $"{form} gave a finding at the call");
	}

	[Theory]
	[InlineData($"{_BASIC_HELPER}{_SEND_ONE_LINE_TRAILING_COMMA}")]
	[InlineData($"{_BASIC_HELPER}{_SEND_MULTI_LINE_TRAILING_COMMA}")]
	[InlineData($"{_BASIC_HELPER}{_SEND_K2_ONE_LINE_TRAILING_COMMA}")]
	[InlineData($"{_BASIC_HELPER}{_SEND_K2_MULTI_LINE_TRAILING_COMMA}")]
	[InlineData($"{_BASIC_HELPER}{_SEND_APPEND_TRAILING_COMMA}")]
	public void AGofmtMultiLineCallWithATrailingCommaIsReadAsTheSameFileHelperCall(string source)
	{
		Assert.Single(new GoRecognitionHarness().Recognize(source), construct => construct.PatternClassIdentifier == _CLASS_LOCAL_HELPER);
	}

	[Theory]
	[InlineData($"{_BEARER_HELPER}{_SEND_BEARER_TRAILING_COMMA}")]
	[InlineData($"{_BASIC_HELPER}{_BASIC_HELPER}{_SEND_MULTI_LINE_TRAILING_COMMA}")]
	[InlineData($"{_BASIC_HELPER}{_SEND_EXTRA_ARGUMENT}")]
	public void ATrailingCommaCallWhoseHelperIsNotARecordedBasicHelperOrHasMoreArgumentsGivesNoFindingAtTheCall(string source)
	{
		Assert.DoesNotContain(new GoRecognitionHarness().Recognize(source), construct => construct.PatternClassIdentifier == _CLASS_LOCAL_HELPER);
	}

	[Fact]
	public void ACallFromATestFileReadsMediumWithTheTestMark()
	{
		RecognizedConstruct call = Assert.Single(
			new GoRecognitionHarness().Recognize(_BASIC_HELPER + _SEND_THROUGH_K1, _TEST_FILE_PATH),
			construct => construct.PatternClassIdentifier == _CLASS_LOCAL_HELPER);

		Assert.Equal(ConfidenceLevel.Medium, call.Level);
		Assert.True(call.TestCode);
	}

	[Fact]
	public void ThePackageLevelHelperIsVisibleThroughoutItsFileSoACallBeforeTheDeclarationResolvesAsACallAfterItDoes()
	{
		GoRecognitionHarness harness = new();

		Assert.Contains(harness.Recognize(_SEND_THROUGH_K1 + _BASIC_HELPER), construct => construct.PatternClassIdentifier == _CLASS_LOCAL_HELPER);
		Assert.Contains(harness.Recognize(_BASIC_HELPER + _SEND_THROUGH_K1), construct => construct.PatternClassIdentifier == _CLASS_LOCAL_HELPER);
	}

	[Fact]
	public void AHelperDeclaredInACommentIsNotARecordAndTheCallToItIsSilent()
	{
		string source = "// func basicHeader(userName string, secret string) string {\n" + _SEND_THROUGH_K1;

		Assert.DoesNotContain(new GoRecognitionHarness().Recognize(source), construct => construct.PatternClassIdentifier == _CLASS_LOCAL_HELPER);
	}

	[Fact]
	public void TheVariableTableIsUntouchedByTheMemberTableSoAHeldValueStillResolvesAsItDid()
	{
		string source = "header := \"Basic \" + encoded\nrequest.Header.Set(\"Authorization\", header)\n" + _BASIC_HELPER;
		ImmutableArray<RecognizedConstruct> constructs = new GoRecognitionHarness().Recognize(source);

		Assert.Single(constructs, construct => construct.PatternClassIdentifier == "GO-009");
		Assert.DoesNotContain(constructs, construct => construct.PatternClassIdentifier == _CLASS_LOCAL_HELPER);
	}

	// ------------------------------------------------------------------------------------------
	// Conformance (a), (b), (c), (d) and the test assertion
	// ------------------------------------------------------------------------------------------

	[Theory]
	[InlineData("request.SetBasicAuth(\n\tuserName,\n\t\"exampleSecret\",\n)\n", true)]
	[InlineData("request.SetBasicAuth(userName, \"exampleSecret\" /* note */)\n", true)]
	[InlineData("request.SetBasicAuth(\n\tuserName,\n\t\"exampleSecret\", // note\n)\n", true)]
	[InlineData("request.SetBasicAuth(userName, \"exampleSecret\",)\n", true)]
	[InlineData("request.SetBasicAuth(\n\tuserName,\n\t\"exampleSecret\"\n)\n", true)]
	[InlineData("request.SetBasicAuth(userName, \"exampleSecret\") // note\n", true)]
	[InlineData("request.SetBasicAuth(userName, \"exampleSecret\",,)\n", false)]
	[InlineData("request.SetBasicAuth(userName, secret,\n)\n", false)]
	[InlineData("request.SetBasicAuth(userName, \"exampleSecret\" + suffix,\n)\n", false)]
	public void TheLiteralFlagOfTheMultiLineSetBasicAuthCallAdmitsOneCommaAndCommentsBeforeTheParenthesis(string source, bool expected)
	{
		RecognizedConstruct construct = Assert.Single(new GoRecognitionHarness().Recognize(source), candidate => candidate.PatternClassIdentifier == "GO-001");

		Assert.Equal(ConfidenceLevel.High, construct.Level);
		Assert.Equal(expected, construct.LiteralCredentialSignal);
	}

	[Fact]
	public void TheTailOfTheSetBasicAuthCallIsAtMostTwoHundredCharacters()
	{
		string within = $"request.SetBasicAuth(userName, \"exampleSecret\"{new string(' ', _BLANKS_WITHIN_THE_TAIL_BOUND)},\n)\n";
		string beyond = $"request.SetBasicAuth(userName, \"exampleSecret\"{new string(' ', _CLOSING_TAIL_BOUND_PLUS_ONE)})\n";

		Assert.True(Assert.Single(new GoRecognitionHarness().Recognize(within), candidate => candidate.PatternClassIdentifier == "GO-001").LiteralCredentialSignal);
		Assert.False(Assert.Single(new GoRecognitionHarness().Recognize(beyond), candidate => candidate.PatternClassIdentifier == "GO-001").LiteralCredentialSignal);
	}

	[Theory]
	[InlineData(false, _CLOSING_TAIL_BOUND, true)]
	[InlineData(false, _CLOSING_TAIL_BOUND_PLUS_ONE, false)]
	[InlineData(true, _CLOSING_TAIL_BOUND, true)]
	[InlineData(true, _CLOSING_TAIL_BOUND_PLUS_ONE, false)]
	public void ACommentThatHoldsAParenthesisCountsTowardTheTwoHundredCharactersOfTheTail(bool blockComment, int tailLength, bool expected)
	{
		int frameLength = blockComment ? _BLOCK_COMMENT_FRAME_LENGTH : _LINE_COMMENT_FRAME_LENGTH;
		string body = $"){new string('x', tailLength - frameLength - 1)}";
		string tail = blockComment ? $"\n/*{body}*/\n" : $"\n//{body}\n";
		string source = $"request.SetBasicAuth(userName, \"exampleSecret\"{tail})\n";

		Assert.Equal(tailLength, tail.Length);
		Assert.Equal(expected, Assert.Single(new GoRecognitionHarness().Recognize(source), candidate => candidate.PatternClassIdentifier == "GO-001").LiteralCredentialSignal);
	}

	[Theory]
	[InlineData("request.Header.Set(\"Authorization\", \"Basic \"+base64.StdEncoding.EncodeToString([]byte(\"exampleUser:exampleSecret\")))\n", "GO-002", true)]
	[InlineData("headers := http.Header{\"Authorization\": {\"Basic \" + base64.StdEncoding.EncodeToString([]byte(\"exampleUser:exampleSecret\"))}}\n", "GO-003", false)]
	[InlineData("flat := map[string]string{\"Authorization\": \"Basic \" + base64.StdEncoding.EncodeToString([]byte(\"exampleUser:exampleSecret\"))}\n", "GO-004", false)]
	public void TheNestedWholeLiteralPairIsTheCredentialOfTheAuthorizationHeaderClassOnly(string source, string patternClass, bool expected)
	{
		RecognizedConstruct construct = Assert.Single(new GoRecognitionHarness().Recognize(source), candidate => candidate.PatternClassIdentifier == patternClass);

		Assert.Equal(expected, construct.LiteralCredentialSignal);
	}

	[Fact]
	public void TheEncodedHelperLiteralAcceptsALongChainOfDottedSelectors()
	{
		string selectors = string.Concat(Enumerable.Repeat("a.", _DOTTED_SELECTOR_COUNT));
		string source = $"func basicHeader(raw []byte) string {{\n\tencoded := base64.StdEncoding.EncodeToString(raw)\n\treturn \"Basic \" + {selectors}token\n}}\n";

		Assert.Contains(new GoRecognitionHarness().Recognize(source), construct => construct.PatternClassIdentifier == _CLASS_HELPER);
	}

	[Theory]
	[InlineData("address.User = url.UserPassword(userName, secret,)\n", false)]
	[InlineData("address.User = url.UserPassword(\n\tuserName,\n\tsecret,\n)\n", false)]
	[InlineData("address.User = url.UserPassword(\n\tuserName,\n\t\"exampleSecret\",\n)\n", true)]
	[InlineData("address.User = url.UserPassword(userName, \"exampleSecret\",)\n", true)]
	[InlineData("address.User = url.UserPassword(\n\tuserName,\n\t\"exampleSecret\"\n)\n", true)]
	public void UserPasswordAdmitsATrailingCommaAfterTheSecondArgumentAndStaysMedium(string source, bool expectedFlag)
	{
		RecognizedConstruct construct = Assert.Single(new GoRecognitionHarness().Recognize(source), candidate => candidate.PatternClassIdentifier == "GO-007");

		Assert.Equal(ConfidenceLevel.Medium, construct.Level);
		Assert.Equal(expectedFlag, construct.LiteralCredentialSignal);
	}

	[Theory]
	[InlineData("address.User = url.UserPassword(userName, secret, extra)\n")]
	[InlineData("address.User = url.UserPassword(userName, secret, extra,)\n")]
	public void UserPasswordWithAThirdArgumentIsStillNotRead(string source)
	{
		Assert.DoesNotContain(new GoRecognitionHarness().Recognize(source), candidate => candidate.PatternClassIdentifier == "GO-007");
	}

	[Theory]
	[InlineData("request.Header.Set(\"Authorization\", \"Basic dXNlcjpwYXNz\"+suffix)\n", false)]
	[InlineData("request.Header.Set(\"Authorization\", \"Basic dXNlcjpwYXNz\" + suffix)\n", false)]
	[InlineData("request.Header.Set(\"Authorization\", \"Basic dXNlcjpwYXNz\")\n", true)]
	[InlineData("request.Header.Set(\"Authorization\", `Basic dXNlcjpwYXNz`)\n", true)]
	[InlineData("request.Header.Set(\"Authorization\", \"Basic abc\")\n", false)]
	[InlineData("request.Header.Set(\"Authorization\", \"Basic changeme\")\n", false)]
	[InlineData("request.Header.Set(\"Authorization\", \"Basic \"+base64.StdEncoding.EncodeToString([]byte(\"exampleUser:exampleSecret\")))\n", true)]
	[InlineData("request.Header.Set(\"Authorization\", \"Basic \"+base64.StdEncoding.EncodeToString([]byte(\"exampleUser:changeme\")))\n", false)]
	[InlineData("request.Header.Set(\"Authorization\", \"Basic \"+base64.StdEncoding.EncodeToString([]byte(userName+\":\"+secret)))\n", false)]
	[InlineData("request.Header.Set(\"Authorization\", \"Basic \"+base64.StdEncoding.EncodeToString([]byte(\"host: port\")))\n", false)]
	[InlineData("request.Header.Set(\"Authorization\", fmt.Sprintf(\"Basic %s\", base64.StdEncoding.EncodeToString([]byte(\"exampleUser:exampleSecret\"))))\n", true)]
	public void TheLiteralFlagOfTheAuthorizationHeaderIsTrueOnlyForAWholeLiteralTokenOrAWholeLiteralPairInsideTheNestedEncoder(string source, bool expected)
	{
		ImmutableArray<RecognizedConstruct> constructs = new GoRecognitionHarness().Recognize(source);
		RecognizedConstruct header = Assert.Single(constructs, construct => construct.PatternClassIdentifier == "GO-002");

		Assert.Equal(ConfidenceLevel.High, header.Level);
		Assert.Equal(expected, header.LiteralCredentialSignal);
		Assert.DoesNotContain(constructs, construct => construct.PatternClassIdentifier == "GO-006");
	}

	[Fact]
	public void ThereIsNoTripleWithGo005AsTheSubsumingClassSoAWrapperThatTakesTheTwoValuesOfParseReadsGo008AloneAtMediumWithTheFlag()
	{
		string source = "func mustParse(address *url.URL, err error) *url.URL {\n\tif err != nil {\n\t\tpanic(err)\n\t}\n\treturn address\n}\n\nfunc f() {\n\tresponse, err := http.Get(mustParse(url.Parse(\"https://user:exampleSecret@host.example/path\")).String())\n\t_, _ = response, err\n}\n";
		ImmutableArray<RecognizedConstruct> constructs = new GoRecognitionHarness().Recognize(source);
		RecognizedConstruct parse = Assert.Single(constructs, construct => construct.PatternClassIdentifier == "GO-008");

		Assert.Equal(ConfidenceLevel.Medium, parse.Level);
		Assert.True(parse.LiteralCredentialSignal);
		Assert.DoesNotContain(constructs, construct => construct.PatternClassIdentifier == "GO-005");
	}

	[Fact]
	public void ATestAssertionOnAnEncodedBasicHeaderReadsGo006MediumWithTheTestMarkAndIsNotRefused()
	{
		string source = "func TestSend(t *testing.T) {\n\texpected := \"Basic \" + base64.StdEncoding.EncodeToString([]byte(\"exampleUser:exampleSecret\"))\n\tif received != expected {\n\t\tt.Fatal(received)\n\t}\n}\n";
		GoRecognitionHarness harness = new();
		RecognizedConstruct inTest = Assert.Single(harness.Recognize(source, _TEST_FILE_PATH), construct => construct.PatternClassIdentifier == "GO-006");
		RecognizedConstruct outsideTest = Assert.Single(harness.Recognize(source), construct => construct.PatternClassIdentifier == "GO-006");

		Assert.Equal(ConfidenceLevel.Medium, inTest.Level);
		Assert.True(inTest.TestCode);
		Assert.True(inTest.LiteralCredentialSignal);
		Assert.Equal(ConfidenceLevel.Medium, outsideTest.Level);
		Assert.False(outsideTest.TestCode);
	}

	private static string Describe(RecognizedConstruct construct)
	{
		return $"{construct.PatternClassIdentifier}:{construct.Level}";
	}
}
