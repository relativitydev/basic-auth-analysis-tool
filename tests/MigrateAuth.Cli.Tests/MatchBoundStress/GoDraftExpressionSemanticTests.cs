using System.Collections.Immutable;
using System.Text;
using System.Text.RegularExpressions;

using MigrateAuth.Cli.Lexical;
using MigrateAuth.Cli.Reading;
using MigrateAuth.Cli.Tests.Lexical;

using Xunit;

namespace MigrateAuth.Cli.Tests.MatchBoundStress;

/// <summary>
/// THE SEMANTIC CHECKS OF THE `Go` DRAFT EXPRESSIONS. Every class of the draft catalog (`GO-001` to `GO-013`), the constant declaration, the member declaration, the two content markers and the eleven flow
/// signals have positive and negative samples here, each a shape the catalog names: the forms of the call and the carriers of a header that a class reads, the near-miss families it
/// refuses (`inbound-authorization-read`, `bearer-or-api-key`, `digest-auth`, `non-http-credentials`, `oauth-client-credentials`, `base64-non-credential`, `proxy-credentials`,
/// `non-http-userinfo`, `userinfo-read`, `definition-not-call`) with their fixture text, the demotion markers, the flow signals and their near-miss forms. A case says which expression, the text,
/// whether the expression matches it, and for the cases that name one, what a capture group holds. The generated and the interpreted form of every expression must agree on every case.
/// The window boundaries (the argument window, the definition window, the statement window, the header-comment window) are tested at the bound and one past it.
///
/// THE COMMENT-AND-STRING NEAR-MISS (`basic-in-comment-string-or-unreachable`) IS NOT AN EXPRESSION'S TO REFUSE: an expression reads the decoded text, and the mask of the scanner puts the
/// anchor in a comment or a string body, which composes to Low. The check of that family therefore asks the test-local profile of <see cref="GoShapedProfile"/> for the zone of the anchor.
/// </summary>
public sealed class GoDraftExpressionSemanticTests
{
	private const string _LINE_FEED = "\n";
	private const string _SIGNAL_GROUP = "anchor";
	private const string _CONSTRUCT_GROUP = "construct";
	private const string _FLOW_SIGNAL_PREFIX_120S = "EX-GO-12";
	private const string _FLOW_SIGNAL_PREFIX_130S = "EX-GO-13";
	private const string _CONSTANT_DECLARATION_ID = "EX-GO-101";
	private const string _MEMBER_DECLARATION_ID = "EX-GO-102";
	private const string _GENERATED_MARKER_ID = "EX-GO-110";
	private const string _EXAMPLE_MARKER_ID = "EX-GO-111";
	private const string _CLASS_PREFIX = "EX-GO-0";
	private const int _CLASS_COUNT = 13;
	private const int _EXPRESSION_COUNT = 28;
	private const int _ABBREVIATION_MAXIMUM_LENGTH = 160;
	private const char _FILLER_UNIT = 'x';
	private const int _FILLER_UNITS_AT_THE_ARGUMENT_WINDOW_BOUND = 1_498;
	private const int _FILLER_UNITS_BEYOND_THE_ARGUMENT_WINDOW_BOUND = 1_500;
	private const int _FILLER_LINES_AT_THE_DEFINITION_WINDOW_BOUND = 29;
	private const int _FILLER_LINES_BEYOND_THE_DEFINITION_WINDOW_BOUND = 30;
	private const int _COMMENT_LINES_AT_THE_HEADER_BOUND = 50;
	private const int _COMMENT_LINES_BEYOND_THE_HEADER_BOUND = 51;
	private const int _STATEMENT_WINDOW_IN_CHARACTERS = 300;
	private const int _BLANKS_BEYOND_THE_CLOSING_TAIL_BOUND = 201;
	private const int _DISTANCE_WITHIN_THE_STATEMENT_WINDOW = 280;
	private const int _DISTANCE_BEYOND_THE_STATEMENT_WINDOW = 330;
	private const int _LONG_CHAIN_LINK_COUNT = 4_000;
	private const int _LONG_PADDING_LENGTH = 9_000;
	private const string _ENCODER_CALL_PREFIX = "x := base64.StdEncoding.EncodeToString(";
	private const string _ENCODER_CALL_REST = "[]byte(first + \":\" + second))";
	private const string _TRANSPORT_HEAD = "func (transport *basicTransport) RoundTrip(request *http.Request) (*http.Response, error) {\n";
	private const string _HELPER_HEAD = "func basicHeader(userName string, secret string) string {\n";
	private const string _FILLER_LINE = "\tcount := 1\n";

	internal sealed record Case(string Id, string Text, bool Matches, string Group = "", string Value = "", int Start = 0);

	private static readonly ImmutableArray<Case> _CASES =
	[
		.. DeclarationCases(),
		.. CasesOfTheMethod(),
		.. CasesOfTheHeaderWrites(),
		.. CasesOfTheHeaderLiterals(),
		.. CasesOfTheUrlForms(),
		.. CasesOfTheEncoderAndTheUserinfo(),
		.. CasesOfTheSymbolicClasses(),
		.. CasesOfTheDefinitionClasses(),
		.. CasesOfTheLocalHelperAndTheMemberDeclaration(),
		.. CasesOfTheClosingTailsAndTheWholeValueToken(),
		.. CasesOfTheDemotionMarkers(),
		.. FlowCasesOfTheCommandLineSignals(),
		.. FlowCasesOfTheWebServerSignals(),
		.. FlowCasesOfTheFixedSignals(),
		.. FlowCasesOfTheSecretStoreSignals()
	];

	private static readonly ImmutableDictionary<string, Regex> _EXPRESSIONS = BuildExpressionIndex();

	/// <summary>Every sample of the draft, for the check that the shipped expressions mean what the draft meant.</summary>
	internal static ImmutableArray<Case> Cases => _CASES;

	[Fact]
	public void EveryDraftExpressionHasPositiveAndNegativeSamples()
	{
		Assert.All(GoDraftExpressions.MatchBoundExpressions.Select(entry => entry.Identifier), identifier =>
		{
			Assert.Contains(_CASES, candidate => candidate.Id == identifier && candidate.Matches);
			Assert.Contains(_CASES, candidate => candidate.Id == identifier && !candidate.Matches);
		});
	}

	[Fact]
	public void EveryClassOfTheCatalogIsCitedByItsIdentifierAndTheTableHoldsTwentyEightExpressions()
	{
		string[] classIdentifiers = [.. Enumerable.Range(1, _CLASS_COUNT).Select(number => $"{_CLASS_PREFIX}{number:D2}")];

		Assert.Equal(_EXPRESSION_COUNT, GoDraftExpressions.MatchBoundExpressions.Length);
		Assert.All(classIdentifiers, identifier => Assert.Contains(GoDraftExpressions.MatchBoundExpressions, entry => entry.Identifier == identifier));
		Assert.Equal(_EXPRESSION_COUNT, GoDraftExpressions.MatchBoundExpressions.Select(entry => entry.Identifier).Distinct(StringComparer.Ordinal).Count());
	}

	[Fact]
	public void EveryDraftExpressionMatchesExactlyTheSamplesItShould()
	{
		List<string> wrong = [];

		foreach (Case sample in _CASES)
		{
			Match match = _EXPRESSIONS[sample.Id].Match(sample.Text, sample.Start);
			bool captured = sample.Group.Length == 0 || (match.Success && match.Groups[sample.Group].Value == sample.Value);

			if (match.Success != sample.Matches || !captured)
			{
				wrong.Add($"{sample.Id} on [{Escape(Abbreviate(sample.Text))}] expected {sample.Matches} {sample.Group}={sample.Value}, got {match.Success} {(sample.Group.Length == 0 ? string.Empty : match.Groups[sample.Group].Value)}");
			}
		}

		Assert.Empty(wrong);
	}

	[Fact]
	public void TheGeneratedAndTheInterpretedFormOfEveryDraftExpressionAgreeOnEverySample()
	{
		List<string> disagreements = [];

		foreach (Case sample in _CASES)
		{
			Regex generated = _EXPRESSIONS[sample.Id];
			Regex interpreted = new(generated.ToString(), generated.Options, generated.MatchTimeout);

			if (generated.Match(sample.Text, sample.Start).Success != interpreted.Match(sample.Text, sample.Start).Success)
			{
				disagreements.Add($"{sample.Id} on {Escape(Abbreviate(sample.Text))}");
			}
		}

		Assert.Empty(disagreements);
	}

	[Fact]
	public void EveryClassExpressionDeclaresTheConstructGroupAndEveryFlowSignalTheAnchorGroup()
	{
		List<string> missing = [];

		foreach ((string identifier, Regex expression) in GoDraftExpressions.MatchBoundExpressions)
		{
			string anchorGroup = identifier.StartsWith(_FLOW_SIGNAL_PREFIX_120S, StringComparison.Ordinal) || identifier.StartsWith(_FLOW_SIGNAL_PREFIX_130S, StringComparison.Ordinal) ? _SIGNAL_GROUP : _CONSTRUCT_GROUP;
			bool hasNoAnchor = identifier is _CONSTANT_DECLARATION_ID or _MEMBER_DECLARATION_ID or _GENERATED_MARKER_ID or _EXAMPLE_MARKER_ID;

			if (!hasNoAnchor && !expression.GetGroupNames().Contains(anchorGroup))
			{
				missing.Add($"{identifier} lacks {anchorGroup}");
			}
		}

		Assert.Empty(missing);
	}

	[Fact]
	public void EveryDraftExpressionDeclaresThePolicyMatchTimeout()
	{
		Assert.All(GoDraftExpressions.MatchBoundExpressions, entry => Assert.Equal(MigrateAuth.Cli.Catalog.RecognitionEnginePolicy.MATCH_BOUND, entry.Expression.MatchTimeout));
	}

	[Fact]
	public void TheArgumentWindowReadsAUrlAtTheBoundAndNotBeyondIt()
	{
		string url = " \"https://user:secret@host.example/path\")";

		Assert.Matches(GoDraftExpressions.Ex005(), $"http.Get({new string(_FILLER_UNIT, _FILLER_UNITS_AT_THE_ARGUMENT_WINDOW_BOUND)}{url}");
		Assert.DoesNotMatch(GoDraftExpressions.Ex005(), $"http.Get({new string(_FILLER_UNIT, _FILLER_UNITS_BEYOND_THE_ARGUMENT_WINDOW_BOUND)}{url}");
	}

	[Fact]
	public void TheHeaderLiteralWindowReadsTheKeyAtTheBoundAndNotBeyondIt()
	{
		string key = " \"Authorization\": {\"Basic \" + encoded}}";

		Assert.Matches(GoDraftExpressions.Ex003(), $"http.Header{{{new string(_FILLER_UNIT, _FILLER_UNITS_AT_THE_ARGUMENT_WINDOW_BOUND)}{key}");
		Assert.DoesNotMatch(GoDraftExpressions.Ex003(), $"http.Header{{{new string(_FILLER_UNIT, _FILLER_UNITS_BEYOND_THE_ARGUMENT_WINDOW_BOUND)}{key}");
	}

	[Fact]
	public void TheDefinitionWindowOfARoundTripperIsThirtyLines()
	{
		string use = "\trequest.SetBasicAuth(userName, secret)\n}\n";

		Assert.Matches(GoDraftExpressions.Ex010(), _TRANSPORT_HEAD + Repeat(_FILLER_LINE, _FILLER_LINES_AT_THE_DEFINITION_WINDOW_BOUND) + use);
		Assert.DoesNotMatch(GoDraftExpressions.Ex010(), _TRANSPORT_HEAD + Repeat(_FILLER_LINE, _FILLER_LINES_BEYOND_THE_DEFINITION_WINDOW_BOUND) + use);
	}

	[Fact]
	public void TheDefinitionWindowOfAHelperIsThirtyLines()
	{
		string use = "\trequest.Header.Set(\"Authorization\", \"Basic \"+userName)\n}\n";

		Assert.Matches(GoDraftExpressions.Ex011(), _HELPER_HEAD + Repeat(_FILLER_LINE, _FILLER_LINES_AT_THE_DEFINITION_WINDOW_BOUND) + use);
		Assert.DoesNotMatch(GoDraftExpressions.Ex011(), _HELPER_HEAD + Repeat(_FILLER_LINE, _FILLER_LINES_BEYOND_THE_DEFINITION_WINDOW_BOUND) + use);
	}

	[Fact]
	public void TheStatementWindowOfTheEncoderIsThreeHundredCharactersEitherSide()
	{
		int distanceFromTheUseWordToThePositionAfterTheCall = _ENCODER_CALL_PREFIX.Length + 1;
		string within = "// Authorization" + new string('y', _DISTANCE_WITHIN_THE_STATEMENT_WINDOW - distanceFromTheUseWordToThePositionAfterTheCall) + _LINE_FEED + _ENCODER_CALL_PREFIX + _ENCODER_CALL_REST;
		string beyond = "// Authorization" + new string('y', _DISTANCE_BEYOND_THE_STATEMENT_WINDOW) + _LINE_FEED + _ENCODER_CALL_PREFIX + _ENCODER_CALL_REST;
		string ahead = _ENCODER_CALL_PREFIX + _ENCODER_CALL_REST + _LINE_FEED + "// " + new string('y', _DISTANCE_WITHIN_THE_STATEMENT_WINDOW - _ENCODER_CALL_REST.Length) + " Authorization";

		Assert.True(_STATEMENT_WINDOW_IN_CHARACTERS > _DISTANCE_WITHIN_THE_STATEMENT_WINDOW);
		Assert.Matches(GoDraftExpressions.Ex006(), within);
		Assert.DoesNotMatch(GoDraftExpressions.Ex006(), beyond);
		Assert.Matches(GoDraftExpressions.Ex006(), ahead);
	}

	[Fact]
	public void TheGeneratedAndTheInterpretedFormAgreeOnALongLineForTheConfigurationSignal()
	{
		string longLine = "creds := " + Repeat("a.", _LONG_CHAIN_LINK_COUNT) + "Settings[k]\n" + Repeat("x", _LONG_PADDING_LENGTH) + " creds := cfg.Account.Secret\n";
		Regex generated = GoDraftExpressions.Ex131();
		Regex interpreted = new(generated.ToString(), generated.Options, generated.MatchTimeout);

		Assert.Equal(interpreted.IsMatch(longLine), generated.IsMatch(longLine));
	}

	[Fact]
	public void TheGeneratedAndTheInterpretedFormAgreeAtEveryWindowBound()
	{
		string helperUse = "\trequest.Header.Set(\"Authorization\", \"Basic \"+userName)\n}\n";
		string transportUse = "\trequest.SetBasicAuth(userName, secret)\n}\n";
		(Regex Generated, string Text)[] texts =
		[
			(GoDraftExpressions.Ex011(), _HELPER_HEAD + Repeat(_FILLER_LINE, _FILLER_LINES_AT_THE_DEFINITION_WINDOW_BOUND) + helperUse),
			(GoDraftExpressions.Ex011(), _HELPER_HEAD + Repeat(_FILLER_LINE, _FILLER_LINES_BEYOND_THE_DEFINITION_WINDOW_BOUND) + helperUse),
			(GoDraftExpressions.Ex010(), _TRANSPORT_HEAD + Repeat(_FILLER_LINE, _FILLER_LINES_AT_THE_DEFINITION_WINDOW_BOUND) + transportUse),
			(GoDraftExpressions.Ex010(), _TRANSPORT_HEAD + Repeat(_FILLER_LINE, _FILLER_LINES_BEYOND_THE_DEFINITION_WINDOW_BOUND) + transportUse)
		];

		foreach ((Regex generated, string text) in texts)
		{
			Regex interpreted = new(generated.ToString(), generated.Options, generated.MatchTimeout);

			Assert.Equal(interpreted.IsMatch(text), generated.IsMatch(text));
		}
	}

	[Fact]
	public void TheGeneratedHeaderIsReadAfterAtMostFiftyBlankOrCommentLines()
	{
		string header = "// Code generated by a tool. DO NOT EDIT.\npackage sample\n";

		Assert.Matches(GoDraftExpressions.Ex110(), Repeat("// note\n", _COMMENT_LINES_AT_THE_HEADER_BOUND) + header);
		Assert.DoesNotMatch(GoDraftExpressions.Ex110(), Repeat("// note\n", _COMMENT_LINES_BEYOND_THE_HEADER_BOUND) + header);
	}

	[Fact]
	public void ACommentOrAStringBodyHoldsTheAnchorOfTheNearMissFamilyAndNeverCode()
	{
		(string Text, string Expression, string Anchor)[] samples =
		[
			("// request.SetBasicAuth(userName, secret)\n", "EX-GO-001", "SetBasicAuth"),
			("/* h.Set(\"Authorization\", \"Basic \" + encoded) */\n", "EX-GO-002", "Set"),
			("const help = `request.Header.Set(\"Authorization\", \"Basic \"+x)`\n", "EX-GO-002", "Set"),
			("// address, err := url.Parse(\"https://user:secret@host.example/path\")\n", "EX-GO-008", "Parse")
		];

		foreach ((string text, string expressionId, string anchor) in samples)
		{
			Match match = _EXPRESSIONS[expressionId].Match(text);
			ZoneMask mask = GoShapedProfile.Scan(text);

			Assert.True(match.Success, text);
			Assert.NotEqual(LexicalZone.Code, mask.ZoneAt(match.Groups[_CONSTRUCT_GROUP].Index));
			Assert.Equal(anchor, match.Groups[_CONSTRUCT_GROUP].Value);
		}
	}

	[Fact]
	public void ACodeAnchorOfAPositiveSampleLiesInCodeInTheScannersMask()
	{
		(string Text, string Expression)[] samples =
		[
			("request.SetBasicAuth(userName, secret)\n", "EX-GO-001"),
			("request.Header.Set(\"Authorization\", \"Basic \"+encoded)\n", "EX-GO-002"),
			("headers := http.Header{\"Authorization\": {\"Basic \" + encoded}}\n", "EX-GO-003"),
			("headers := map[string]string{\"Authorization\": \"Basic \" + encoded}\n", "EX-GO-004"),
			("response, err := http.Get(\"https://user:secret@host.example/path\")\n", "EX-GO-005"),
			("pair := base64.StdEncoding.EncodeToString([]byte(userName + \":\" + secret)) // Basic\n", "EX-GO-006"),
			("address.User = url.UserPassword(userName, secret)\n", "EX-GO-007")
		];

		foreach ((string text, string expressionId) in samples)
		{
			Match match = _EXPRESSIONS[expressionId].Match(text);

			Assert.True(match.Success, text);
			Assert.Equal(LexicalZone.Code, GoShapedProfile.Scan(text).ZoneAt(match.Groups[_CONSTRUCT_GROUP].Index));
		}
	}

	[Fact]
	public void EveryStressShapeIsLegalForTheReaderAndFillsTheFileToJustUnderTheCeiling()
	{
		List<string> refused = [];

		foreach (MatchBoundStressShape shape in GoDraftCostGateStressSet.Build())
		{
			bool tooLarge = Encoding.UTF8.GetByteCount(shape.Text) > ReadingCeilings.MAXIMUM_FILE_SIZE_IN_BYTES;
			bool lineTooLong = shape.Text.Split(_LINE_FEED).Any(line => line.Length > ReadingCeilings.MAXIMUM_LINE_LENGTH_IN_CHARACTERS);

			if (tooLarge || lineTooLong || shape.Text.Contains('\r') || shape.FileExtension != GoDraftCostGateStressSet.FILE_EXTENSION)
			{
				refused.Add($"{shape.Name}: tooLarge={tooLarge} lineTooLong={lineTooLong}");
			}
		}

		Assert.Empty(refused);
	}

	[Fact]
	public void EveryStressShapeNameIsUniqueAndTheSetHoldsTheTypicalTheWorstAndTheLongLineShapes()
	{
		ImmutableArray<MatchBoundStressShape> shapes = GoDraftCostGateStressSet.Build();

		Assert.Equal(shapes.Length, shapes.Select(shape => shape.Name).Distinct(StringComparer.Ordinal).Count());
		Assert.Contains(shapes, shape => shape.Name.StartsWith(GoDraftCostGateStressSet.TYPICAL_PREFIX, StringComparison.Ordinal));
		Assert.Contains(shapes, shape => shape.Name.StartsWith(GoDraftCostGateStressSet.WORST_PREFIX, StringComparison.Ordinal));
		Assert.Contains(shapes, shape => shape.Name.StartsWith(GoDraftCostGateStressSet.LONG_LINE_PREFIX, StringComparison.Ordinal));
		Assert.Contains(shapes, shape => shape.Name.Contains("setbasicauth", StringComparison.Ordinal));
		Assert.Contains(shapes, shape => shape.Name.Contains("authorization", StringComparison.Ordinal));
	}

	// ------------------------------------------------------------------------------------------
	// GO-013 and the member declaration (0.5)
	// ------------------------------------------------------------------------------------------

	private static IEnumerable<Case> CasesOfTheLocalHelperAndTheMemberDeclaration()
	{
		const string CALL = "EX-GO-013";
		const string MEMBER = _MEMBER_DECLARATION_ID;

		return
		[
			Yes(CALL, "request.Header.Set(\"Authorization\", basicHeader(userName, secret))\n", "name", "basicHeader"),
			Yes(CALL, "request.Header.Set(\"Authorization\", basicHeader(userName, secret))\n", "construct", "Set"),
			Yes(CALL, "request.Header.Add(\"authorization\", basicHeader(userName, secret))\n", "name", "basicHeader"),
			Yes(CALL, "request.Header.Set(`Authorization`, basicHeader(userName, secret))\n", "name", "basicHeader"),
			Yes(CALL, "request.Header[\"Authorization\"] = []string{basicHeader(userName, secret)}\n", "name", "basicHeader"),
			Yes(CALL, "request.Header[\"Authorization\"] = []string{basicHeader(userName, secret)}\n", "construct", "Header"),
			Yes(CALL, "request.Header[\"Authorization\"] = append(request.Header[\"Authorization\"], basicHeader(userName, secret))\n", "name", "basicHeader"),
			Yes(CALL, "request.Header.Set(\"Authorization\", basicHeader(first(a, b), \"c\"))\n", "name", "basicHeader"),
			Yes(CALL, "request.Header.Set(\"Authorization\", string(basicHeader(a)))\n", "name", "string"),
			Yes(CALL, "request.Header.Set(\"Authorization\",\n\tbasicHeader(\n\t\tuserName,\n\t\tsecret,\n\t))\n", "name", "basicHeader"),
			No(CALL, "request.Header.Set(\"Authorization\", client.basicHeader(userName, secret))\n"),
			No(CALL, "request.Header.Set(\"Authorization\", pkg.basicHeader(userName, secret))\n"),
			No(CALL, "request.Header.Set(\"Authorization\", basicHeader(userName, secret) + suffix)\n"),
			No(CALL, "request.Header.Set(\"Authorization\", (basicHeader(userName, secret)))\n"),
			No(CALL, "request.Header.Set(\"Authorization\", basicHeader)\n"),
			No(CALL, "request.Header.Set(\"Authorization\", basicHeader(userName, secret), extra)\n"),
			No(CALL, "request.Header.Set(\"Authorization\", \"Basic \"+encoded)\n"),
			No(CALL, "request.Header.Set(\"Proxy-Authorization\", basicHeader(userName, secret))\n"),
			No(CALL, "request.Header.Set(\"X-Api-Key\", basicHeader(userName, secret))\n"),
			No(CALL, "headers := http.Header{\"Authorization\": {basicHeader(userName, secret)}}\n"),
			No(CALL, "flat := map[string]string{\"Authorization\": basicHeader(userName, secret)}\n"),
			No(CALL, "value := basicHeader(userName, secret)\n"),
			No(CALL, "request.Header[\"Authorization\"] = basicHeader(userName, secret)\n"),
			No(CALL, "request.Header.Set(\"Authorization\", [3]string{}[0])\n"),

			Yes(MEMBER, "func basicHeader(userName string, secret string) string {\n\treturn \"Basic \" + base64.StdEncoding.EncodeToString([]byte(userName+\":\"+secret))\n}\n", "name", "basicHeader"),
			Yes(MEMBER, "func (client *Client) authorize(request *http.Request, token string) {\n\trequest.Header.Set(\"Authorization\", \"Basic \"+token)\n}\n", "name", "authorize"),
			Yes(MEMBER, "\tfunc nested(a int) {\n", "name", "nested"),
			Yes(MEMBER, "func plain(userName string) string {\n\treturn userName\n}\n", "name", "plain"),
			Yes(MEMBER, "func basicHeader() string {\n\treturn \"Basic \" + base64.StdEncoding.EncodeToString(data)\n}\n", "name", "basicHeader"),
			Yes(MEMBER, "func basicHeader[T any](raw T) string {\n\treturn \"Basic \" + base64.StdEncoding.EncodeToString(data)\n}\n", "name", "basicHeader"),
			Yes(MEMBER, "func (transport *basicTransport) RoundTrip(request *http.Request) (*http.Response, error) {\n\trequest.SetBasicAuth(transport.userName, transport.secret)\n\treturn nil, nil\n}\n", "name", "RoundTrip"),
			Yes(MEMBER, "func (list *List[T]) Push(item T) {\n", "name", "Push"),
			No(MEMBER, "var handler = func(request *http.Request) {\n"),
			No(MEMBER, "request.Header.Set(\"Authorization\", func(a int) string { return \"\" }(1))\n"),
			No(MEMBER, "// func commented(a int) {\n"),
			No(MEMBER, "type basicHeader func(string, string) string\n"),
			No(MEMBER, "func(x int) {\n"),
			No(MEMBER, "func helper {\n")
		];
	}

	/// <summary>The helper that GO-011 reads is marked in its member record: the empty groups <c>environment</c>, <c>fallbackWord</c> and <c>basicHelper</c> take part in the match; every other record is unmarked.</summary>
	[Theory]
	[InlineData("func basicHeader(userName string, secret string) string {\n\treturn \"Basic \" + base64.StdEncoding.EncodeToString([]byte(userName+\":\"+secret))\n}\n", true)]
	[InlineData("func basicHeader(pair string) string {\n\treturn \"Basic \" + base64.StdEncoding.EncodeToString([]byte(pair))\n}\n", true)]
	[InlineData("func (client *Client) authorize(request *http.Request, token string) {\n\trequest.Header.Set(\"Authorization\", \"Basic \"+token)\n}\n", true)]
	[InlineData("func bearerHeader(token string) string {\n\treturn \"Bearer \" + token\n}\n", false)]
	[InlineData("func challenge(realm string) string {\n\treturn \"Basic realm=\\\"\" + realm + \"\\\"\"\n}\n", false)]
	[InlineData("func basicHeader() string {\n\treturn \"Basic \" + base64.StdEncoding.EncodeToString(data)\n}\n", false)]
	[InlineData("func basicHeader[T any](raw T) string {\n\treturn \"Basic \" + base64.StdEncoding.EncodeToString(raw)\n}\n", false)]
	[InlineData("func (transport *basicTransport) RoundTrip(request *http.Request) (*http.Response, error) {\n\trequest.Header.Set(\"Authorization\", \"Basic \"+transport.token)\n\treturn nil, nil\n}\n", false)]
	[InlineData("func plain(userName string) string {\n\treturn userName\n}\n", false)]
	public void TheMemberRecordOfAFunctionIsMarkedExactlyWhenTheHelperRowOrACountedConstructIsRead(string text, bool marked)
	{
		Match match = GoDraftExpressions.Ex102().Match(text);

		Assert.True(match.Success, text);
		Assert.Equal(marked, match.Groups["environment"].Success);
		Assert.Equal(marked, match.Groups["fallbackWord"].Success);
		Assert.Equal(marked, match.Groups["basicHelper"].Success);
		Assert.Equal(string.Empty, match.Groups["fallbackWord"].Value);
	}

	// ------------------------------------------------------------------------------------------
	// The closing tails of GO-001 and GO-007 and the whole-value token of GO-002 (0.5)
	// ------------------------------------------------------------------------------------------

	private static IEnumerable<Case> CasesOfTheClosingTailsAndTheWholeValueToken()
	{
		const string METHOD = "EX-GO-001";
		const string USERINFO = "EX-GO-007";
		const string HEADER = "EX-GO-002";

		return
		[
			Yes(METHOD, "request.SetBasicAuth(\n\tuserName,\n\t\"exampleSecret\",\n)\n", "credential", "exampleSecret"),
			Yes(METHOD, "request.SetBasicAuth(userName, \"exampleSecret\",)\n", "credential", "exampleSecret"),
			Yes(METHOD, "request.SetBasicAuth(userName, \"exampleSecret\" /* note */)\n", "credential", "exampleSecret"),
			Yes(METHOD, "request.SetBasicAuth(\n\tuserName,\n\t\"exampleSecret\", // note\n)\n", "credential", "exampleSecret"),
			Yes(METHOD, "request.SetBasicAuth(\n\tuserName,\n\t\"exampleSecret\" /* a */, /* b */\n\t// c\n)\n", "credential", "exampleSecret"),
			Yes(METHOD, "request.SetBasicAuth(userName, \"exampleSecret\",,)\n", "credential", string.Empty),
			Yes(METHOD, $"request.SetBasicAuth(userName, \"exampleSecret\"{new string(' ', _BLANKS_BEYOND_THE_CLOSING_TAIL_BOUND)})\n", "credential", string.Empty),
			Yes(METHOD, "request.SetBasicAuth(userName, \"exampleSecret\" + suffix)\n", "credential", string.Empty),

			Yes(USERINFO, "address.User = url.UserPassword(userName, secret,)\n", "construct", "UserPassword"),
			Yes(USERINFO, "address.User = url.UserPassword(\n\tuserName,\n\t\"exampleSecret\",\n)\n", "credential", "exampleSecret"),
			Yes(USERINFO, "address.User = url.UserPassword(userName, \"exampleSecret\",)\n", "credential", "exampleSecret"),
			Yes(USERINFO, "address.User = url.UserPassword(\n\tuserName,\n\tsecret,\n)\n", "construct", "UserPassword"),
			No(USERINFO, "address.User = url.UserPassword(userName, secret, extra)\n"),

			Yes(HEADER, "request.Header.Set(\"Authorization\", \"Basic dXNlcjpwYXNz\")\n", "credential", "dXNlcjpwYXNz"),
			Yes(HEADER, "request.Header.Set(\"Authorization\", \"Basic dXNlcjpwYXNz\"+suffix)\n", "credential", string.Empty),
			Yes(HEADER, "request.Header.Set(\"Authorization\", \"Basic dXNlcjpwYXNz\" + suffix)\n", "credential", string.Empty),
			Yes(HEADER, "request.Header.Set(\"Authorization\", \"Basic dXNlcjpwYXNz\"\n)\n", "credential", "dXNlcjpwYXNz"),
			Yes("EX-GO-003", "headers := http.Header{\"Authorization\": {\"Basic dXNlcjpwYXNz\"}}\n", "credential", "dXNlcjpwYXNz"),
			Yes("EX-GO-003", "headers := http.Header{\"Authorization\": {\"Basic dXNlcjpwYXNz\" + suffix}}\n", "credential", string.Empty)
		];
	}

	private static ImmutableDictionary<string, Regex> BuildExpressionIndex()
	{
		ImmutableDictionary<string, Regex>.Builder builder = ImmutableDictionary.CreateBuilder<string, Regex>(StringComparer.Ordinal);

		foreach ((string identifier, Regex expression) in GoDraftExpressions.MatchBoundExpressions)
		{
			builder.Add(identifier, expression);
		}

		return builder.ToImmutable();
	}

	private static string Repeat(string line, int times)
	{
		return string.Concat(Enumerable.Repeat(line, times));
	}

	private static string Abbreviate(string text)
	{
		return text.Length <= _ABBREVIATION_MAXIMUM_LENGTH ? text : string.Concat(text.AsSpan(0, _ABBREVIATION_MAXIMUM_LENGTH), "...");
	}

	private static string Escape(string text)
	{
		return text.Replace("\n", "\\n", StringComparison.Ordinal).Replace("\t", "\\t", StringComparison.Ordinal);
	}

	private static Case Yes(string id, string text, string group = "", string value = "")
	{
		return new Case(id, text, Matches: true, group, value);
	}

	private static Case No(string id, string text)
	{
		return new Case(id, text, Matches: false);
	}

	// ------------------------------------------------------------------------------------------
	// The constant declaration (EX-GO-101)
	// ------------------------------------------------------------------------------------------

	private static IEnumerable<Case> DeclarationCases()
	{
		const string ID = "EX-GO-101";

		return
		[
			Yes(ID, "const scheme = \"Basic\"\n", "name", "scheme"),
			Yes(ID, "const scheme = \"Basic\"\n", "value", "Basic"),
			Yes(ID, "var scheme string = \"Basic\"\n", "value", "Basic"),
			Yes(ID, "var scheme = \"Bearer\"\n", "value", "Bearer"),
			Yes(ID, "\thdr := \"Basic \" + encoded\n", "value", "Basic"),
			Yes(ID, "\thdr := \"Bearer \" + token\n", "value", "Bearer"),
			Yes(ID, "scheme := `Basic `\n", "value", "Basic"),
			Yes(ID, "\tscheme = \"Digest\"\n", "name", "scheme"),
			Yes(ID, "\tscheme string = \"Basic\"\n", "value", "Basic"),
			Yes(ID, "token := os.Getenv(\"SERVICE_TOKEN\")\n", "name", "token"),
			Yes(ID, "var token = os.Getenv(\"SERVICE_TOKEN\")\n", "environment", ""),
			Yes(ID, "\ttoken = os.Getenv(\"SERVICE_TOKEN\")\n", "name", "token"),
			Yes(ID, "token := env.Getenv(\"SERVICE_TOKEN\")\n", "name", "token"),
			Yes(ID, "token := Getenv(\"SERVICE_TOKEN\")\n", "name", "token"),
			Yes(ID, "value, found := os.LookupEnv(\"SERVICE_AUTH_HEADER\")\n", "name", "value"),
			Yes(ID, "value, found := os.LookupEnv(\"SERVICE_AUTH_HEADER\")\n", "environment", ""),
			No(ID, "request.Header = \"x\"\n"),
			No(ID, "if value == \"Basic\" {\n"),
			No(ID, "fmt.Println(\"Basic\")\n"),
			No(ID, "scheme := \"unterminated\n"),
			No(ID, "scheme := compute(\"Basic\")\n")
		];
	}

	// ------------------------------------------------------------------------------------------
	// GO-001, GO-002: the method and the header writes of the carriers K1 and K2
	// ------------------------------------------------------------------------------------------

	private static IEnumerable<Case> CasesOfTheMethod()
	{
		const string METHOD = "EX-GO-001";

		return
		[
			Yes(METHOD, "request.SetBasicAuth(userName, secret)\n", "construct", "SetBasicAuth"),
			Yes(METHOD, "request, err := http.NewRequest(http.MethodGet, endpoint, nil)\nrequest.SetBasicAuth(userName, secret)\n", "construct", "SetBasicAuth"),
			Yes(METHOD, "request.SetBasicAuth(userName, \"exampleSecret\")\n", "credential", "exampleSecret"),
			Yes(METHOD, "request.SetBasicAuth(userName, `exampleSecret`)\n", "credential", "exampleSecret"),
			Yes(METHOD, "request.SetBasicAuth(userName, \"pre\"+suffix)\n", "credential", string.Empty),
			Yes(METHOD, "request.SetBasicAuth(userName, secretFor(\"a\", \"b\"))\n", "credential", string.Empty),
			Yes(METHOD, "request.SetBasicAuth(userName, \"changeme\")\n", "credential", string.Empty),
			Yes(METHOD, "request.SetBasicAuth(userName, \"****\")\n", "credential", string.Empty),
			Yes(METHOD, "(*http.Request).SetBasicAuth(request, userName, \"exampleSecret\")\n", "credential", "exampleSecret"),
			Yes(METHOD, "req := &http.Request{Method: \"GET\"}\nreq.SetBasicAuth(u, p)\n", "construct", "SetBasicAuth"),
			Yes(METHOD, "other.client.SetBasicAuth (u, p)\n", "construct", "SetBasicAuth"),
			No(METHOD, "user, secret, found := request.BasicAuth()\n"),
			No(METHOD, "func (client *Client) SetBasicAuth(userName string, secret string) {\n"),
			No(METHOD, "type SetBasicAuth func(string, string)\n"),
			No(METHOD, "set := request.SetBasicAuth\n"),
			No(METHOD, "message := \"call SetBasicAuth to authenticate\"\n"),
			No(METHOD, "request.SetBasicAuthentication(u, p)\n")
		];
	}

	private static IEnumerable<Case> CasesOfTheHeaderWrites()
	{
		const string HEADER = "EX-GO-002";

		return
		[
			Yes(HEADER, "request.Header.Set(\"Authorization\", \"Basic \"+encoded)\n", "construct", "Set"),
			Yes(HEADER, "request.Header.Add(\"Authorization\", \"Basic \" + encoded)\n", "construct", "Add"),
			Yes(HEADER, "h.Set(\"authorization\", `Basic ` + encoded)\n", "construct", "Set"),
			Yes(HEADER, "h.Set(`Authorization`, \"Basic \"+encoded)\n", "construct", "Set"),
			Yes(HEADER, "request.Header[\"Authorization\"] = []string{\"Basic \" + encoded}\n", "construct", "Header"),
			Yes(HEADER, "headers[\"authorization\"] = []string{\"Basic \" + encoded}\n", "construct", "headers"),
			Yes(HEADER, "request.Header[\"Authorization\"] = append(request.Header[\"Authorization\"], \"Basic \"+encoded)\n", "construct", "Header"),
			Yes(HEADER, "request.Header.Set(\"Authorization\", fmt.Sprintf(\"Basic %s\", encoded))\n", "construct", "Set"),
			Yes(HEADER, "request.Header.Set(\"Authorization\", \"Basic dXNlcjpzZWNyZXQ=\")\n", "credential", "dXNlcjpzZWNyZXQ="),
			Yes(HEADER, "request.Header.Set(\"Authorization\", \"Basic changeme\")\n", "credential", string.Empty),
			Yes(HEADER, "request.Header.Set(\"Authorization\", \"Basic \"+base64.StdEncoding.EncodeToString([]byte(userName+\":\"+secret)))\n", "construct", "Set"),
			Yes(HEADER, "request.Header.Set(\"Authorization\", \"BASIC \"+encoded)\n", "construct", "Set"),
			No(HEADER, "request.Header.Set(\"Authorization\", \"Bearer \"+token)\n"),
			No(HEADER, "request.Header.Set(\"Authorization\", \"Digest username=\\\"user\\\", realm=\\\"restricted\\\"\")\n"),
			No(HEADER, "request.Header.Set(\"Authorization\", `Digest username=\"user\", realm=\"restricted\"`)\n"),
			No(HEADER, "request.Header.Set(\"Proxy-Authorization\", \"Basic \"+encoded)\n"),
			No(HEADER, "request.Header.Set(\"X-Authorization\", \"Basic \"+encoded)\n"),
			No(HEADER, "request.Header.Set(\"X-Api-Key\", apiKey)\n"),
			No(HEADER, "request.Header.Set(\"Authorization\", header)\n"),
			No(HEADER, "request.Header.Set(\"Authorization\", \"Basicauth \"+encoded)\n"),
			No(HEADER, "value := request.Header.Get(\"Authorization\")\n"),
			No(HEADER, "writer.Header().Set(\"WWW-Authenticate\", \"Basic realm=\\\"restricted\\\"\")\n"),
			No(HEADER, "if request.Header[\"Authorization\"] == nil {\n"),
			No(HEADER, "value := request.Header[\"Authorization\"]\n"),
			No(HEADER, "request.Header[\"Authorization\"] = []string{\"Bearer \" + token}\n")
		];
	}

	// ------------------------------------------------------------------------------------------
	// GO-003, GO-004: the header literal (K3) and the unnamed map literal (K4)
	// ------------------------------------------------------------------------------------------

	private static IEnumerable<Case> CasesOfTheHeaderLiterals()
	{
		const string TYPED = "EX-GO-003";
		const string UNNAMED = "EX-GO-004";

		return
		[
			Yes(TYPED, "headers := http.Header{\"Authorization\": {\"Basic \" + encoded}}\n", "construct", "Header"),
			Yes(TYPED, "other := h.Header{\"authorization\": []string{\"Basic \" + encoded}}\n", "construct", "Header"),
			Yes(TYPED, "headers := Header{\"Authorization\": {\"Basic \" + encoded}}\n", "construct", "Header"),
			Yes(TYPED, "http.Header{\"Accept\": {\"text/plain\"}, \"Authorization\": {\"Basic \" + encoded}}\n", "construct", "Header"),
			Yes(TYPED, "request := &http.Request{Header: http.Header{\"Authorization\": {\"Basic \" + encoded}}}\n", "construct", "Header"),
			Yes(TYPED, "http.Header{\n\t\"Authorization\": {\"Basic dXNlcjpzZWNyZXQ=\"},\n}\n", "credential", "dXNlcjpzZWNyZXQ="),
			No(TYPED, "headers := http.Header{\"Authorization\": {\"Bearer \" + accessToken}}\n"),
			No(TYPED, "http.Header{\"Authorization\": {\"Digest \" + response}}\n"),
			No(TYPED, "http.Header{\"Proxy-Authorization\": {\"Basic \" + encoded}}\n"),
			No(TYPED, "transport := &http.Transport{ProxyConnectHeader: http.Header{\"Proxy-Authorization\": {\"Basic \" + encoded}}}\n"),
			No(TYPED, "headers := http.Header{\"Accept\": {\"application/json\"}}\n"),
			No(TYPED, "var authHeader Header\n"),

			Yes(UNNAMED, "headers := map[string][]string{\"Authorization\": {\"Basic \" + encoded}}\n", "construct", "map"),
			Yes(UNNAMED, "flat := map[string]string{\"Authorization\": \"Basic \" + encoded}\n", "construct", "map"),
			Yes(UNNAMED, "flat := map[string]string{\"authorization\": `Basic ` + encoded}\n", "construct", "map"),
			Yes(UNNAMED, "http.Header(map[string][]string{\"Authorization\": []string{\"Basic \" + encoded}})\n", "construct", "map"),
			No(UNNAMED, "flat := map[string]string{\"Authorization\": \"Bearer \" + token}\n"),
			No(UNNAMED, "flat := map[string]string{\"Accept\": \"application/json\"}\n"),
			No(UNNAMED, "flat := map[string]int{\"Authorization\": 1}\n"),
			No(UNNAMED, "// flat := map[string]string{ \"Authorization\" is documented }\n")
		];
	}

	// ------------------------------------------------------------------------------------------
	// GO-005, GO-008: a credentialed URL given to a call, or held
	// ------------------------------------------------------------------------------------------

	private static IEnumerable<Case> CasesOfTheUrlForms()
	{
		const string CALL = "EX-GO-005";
		const string HELD = "EX-GO-008";

		return
		[
			Yes(CALL, "response, err := http.Get(\"https://user:secret@host.example/path\")\n", "construct", "Get"),
			Yes(CALL, "response, err := http.Get(\"https://user:secret@host.example/path\")\n", "credential", "secret"),
			Yes(CALL, "request, err := http.NewRequest(http.MethodGet, \"https://user:secret@host.example/path\", nil)\n", "construct", "NewRequest"),
			Yes(CALL, "request, err := http.NewRequestWithContext(ctx, http.MethodGet, \"http://user:secret@host.example/path\", nil)\n", "construct", "NewRequestWithContext"),
			Yes(CALL, "response, err := client.Post(\"http://user:secret@host.example/x\", \"text/plain\", body)\n", "construct", "Post"),
			Yes(CALL, "response, err := client.Head(`HTTPS://user:secret@host.example/x`)\n", "construct", "Head"),
			Yes(CALL, "response, err := Get(\"https://user:secret@host.example/path\")\n", "construct", "Get"),
			Yes(CALL, "response, err := http.PostForm(\"https://user:secret@host.example/path\", form)\n", "construct", "PostForm"),
			Yes(CALL, "response, err := http.Get(\"https://user:changeme@host.example/path\")\n", "credential", string.Empty),
			No(CALL, "response, err := http.Get(\"https://host.example/path\")\n"),
			No(CALL, "response, err := http.Get(\"https://user@host.example/path\")\n"),
			No(CALL, "response, err := http.Get(\"https://host.example:8080/path@x\")\n"),
			No(CALL, "conn, err := sql.Open(\"postgres\", \"postgres://user:secret@host.example/db\")\n"),
			No(CALL, "client.Get(\"ftp://user:secret@host.example/file\")\n"),
			No(CALL, "client.Get(\"amqp://user:secret@broker.example/\")\n"),
			No(CALL, "value := request.Header.Get(\"Authorization\")\n"),

			Yes(HELD, "address, err := url.Parse(\"https://user:secret@host.example/path\")\n", "construct", "Parse"),
			Yes(HELD, "address, err := url.ParseRequestURI(\"http://user:secret@host.example/path\")\n", "construct", "ParseRequestURI"),
			Yes(HELD, "address, err := Parse(\"https://user:secret@host.example/path\")\n", "construct", "Parse"),
			Yes(HELD, "const serviceURL = \"https://user:secret@host.example/path\"\n", "construct", "serviceURL"),
			Yes(HELD, "var serviceURL string = \"http://user:secret@host.example/path\"\n", "construct", "serviceURL"),
			Yes(HELD, "serviceURL := \"https://user:secret@host.example/path\"\n", "construct", "serviceURL"),
			Yes(HELD, "serviceURL := \"https://user:secret@host.example/path\"\n", "credential", "secret"),
			No(HELD, "address, err := url.Parse(\"redis://user:secret@cache.example\")\n"),
			No(HELD, "address, err := url.Parse(\"https://user@host.example\")\n"),
			No(HELD, "address, err := url.Parse(raw)\n"),
			No(HELD, "proxyURL := \"http://user:secret@proxy.example:3128\"\n"),
			No(HELD, "const upstreamProxy = \"https://user:secret@proxy.example:3128\"\n"),
			No(HELD, "serviceURL := \"https://host.example/path\"\n"),
			No(HELD, "if serviceURL == \"https://user:secret@host.example/path\" {\n")
		];
	}

	// ------------------------------------------------------------------------------------------
	// GO-006, GO-007: the encoder and the Userinfo
	// ------------------------------------------------------------------------------------------

	private static IEnumerable<Case> CasesOfTheEncoderAndTheUserinfo()
	{
		const string ENCODER = "EX-GO-006";
		const string USERINFO = "EX-GO-007";
		const string USE = "\nrequest.Header.Set(\"Authorization\", \"Basic \"+pair)\n";

		return
		[
			Yes(ENCODER, "pair := base64.StdEncoding.EncodeToString([]byte(userName + \":\" + secret))" + USE, "construct", "StdEncoding"),
			Yes(ENCODER, "pair := base64.RawStdEncoding.EncodeToString([]byte(userName+\":\"+secret))" + USE, "construct", "RawStdEncoding"),
			Yes(ENCODER, "pair := b64.StdEncoding.EncodeToString([]byte(userName + \":\" + secret))" + USE, "construct", "StdEncoding"),
			Yes(ENCODER, "pair := StdEncoding.EncodeToString([]byte(userName + \":\" + secret))" + USE, "construct", "StdEncoding"),
			Yes(ENCODER, "pair := base64.StdEncoding.EncodeToString([]byte(fmt.Sprintf(\"%s:%s\", userName, secret)))" + USE, "construct", "StdEncoding"),
			Yes(ENCODER, "pair := base64.StdEncoding.EncodeToString([]byte(fmt.Sprintf(\"%v:%v\", userName, secret)))" + USE, "construct", "StdEncoding"),
			Yes(ENCODER, "pair := base64.StdEncoding.EncodeToString([]byte(strings.Join(parts, \":\")))" + USE, "construct", "StdEncoding"),
			Yes(ENCODER, "pair := base64.StdEncoding.EncodeToString([]byte(\"user:exampleSecret\"))" + USE, "credential", "exampleSecret"),
			Yes(ENCODER, "pair := base64.StdEncoding.EncodeToString([]byte(\"user:changeme\"))" + USE, "credential", string.Empty),
			Yes(ENCODER, "base64.StdEncoding.Encode(buffer, []byte(userName + \":\" + secret))" + USE, "construct", "StdEncoding"),
			Yes(ENCODER, "buffer = base64.StdEncoding.AppendEncode(nil, []byte(userName + \":\" + secret))" + USE, "construct", "StdEncoding"),
			Yes(ENCODER, "request.Header.Set(\"Authorization\", \"Basic \"+base64.StdEncoding.EncodeToString([]byte(userName+\":\"+secret)))\n", "construct", "StdEncoding"),
			No(ENCODER, "value := base64.StdEncoding.EncodeToString(payload)" + USE),
			No(ENCODER, "key := base64.StdEncoding.EncodeToString([]byte(host + \":\" + port))\n"),
			No(ENCODER, "key := base64.StdEncoding.EncodeToString([]byte(host + \":\" + port))\nlog.Println(\"cache\")\n"),
			No(ENCODER, "value := base64.URLEncoding.EncodeToString([]byte(userName + \":\" + secret))" + USE),
			No(ENCODER, "value := base64.RawURLEncoding.EncodeToString(digest[:])" + USE),
			No(ENCODER, "encoding := base64.NewEncoding(alphabet)\nvalue := encoding.EncodeToString([]byte(userName + \":\" + secret))" + USE),
			No(ENCODER, "decoded, err := base64.StdEncoding.DecodeString(strings.TrimPrefix(value, \"Basic \"))\n"),
			No(ENCODER, "value := base64.StdEncoding.EncodeToString([]byte(userName + secret))" + USE),
			No(ENCODER, "pair := base64.StdEncoding.EncodeToString([]byte(userName + \":\" + secret))\nrequest.Header.Set(\"Proxy-Authorization\", pair)\n"),
			No(ENCODER, "pair := base64.StdEncoding.EncodeToString([]byte(userName + \":\" + secret))\nrequest.Header.Set(\"PROXY-AUTHORIZATION\", pair)\n"),
			No(ENCODER, "pair := base64.StdEncoding.EncodeToString([]byte(userName + \":\" + secret))\nrequest.Header.Set(\"Proxy-Authorization\", \"Basic \"+encoded)\n"),
			No(ENCODER, "pair := base64.StdEncoding.EncodeToString([]byte(userName + \":\" + secret))\nrequest.Header.Set(\"PROXY-AUTHORIZATION\", \"Basic \"+encoded)\n"),
			No(ENCODER, "request.Header.Set(\"Proxy-Authorization\", \"Basic \"+encoded)\npair := base64.StdEncoding.EncodeToString([]byte(userName + \":\" + secret))\n"),
			Yes(ENCODER, "pair := base64.StdEncoding.EncodeToString([]byte(userName + \":\" + secret))\nrequest.Header.Set(\"Authorization\", \"Basic \"+encoded)\n", "construct", "StdEncoding"),
			Yes(ENCODER, "pair := base64.StdEncoding.EncodeToString([]byte(userName + \":\" + secret))\nrequest.Header.Set(\"Authorization\", \"Basic \"+encoded)\n//" + new string('x', 320) + "\nrequest.Header.Set(\"Proxy-Authorization\", other)\n", "construct", "StdEncoding"),
			Yes(ENCODER, "request.Header.Set(\"Proxy-Authorization\", other)\n//" + new string('x', 320) + "\nrequest.Header.Set(\"Authorization\", \"Basic \"+encoded)\npair := base64.StdEncoding.EncodeToString([]byte(userName + \":\" + secret))\n", "construct", "StdEncoding"),
			Yes(ENCODER, "pair := base64.StdEncoding.EncodeToString([]byte(userName + \":\" + secret))\nrequest.Header.Set(\"Authorization\", pair)\n", "construct", "StdEncoding"),
			Yes(ENCODER, "pair := base64.StdEncoding.EncodeToString([]byte(userName + \":\" + secret))\nrequest.Header.Set(\"ProxyAuthorizationHeader\", pair)\n", "construct", "StdEncoding"),

			Yes(USERINFO, "address.User = url.UserPassword(userName, secret)\n", "construct", "UserPassword"),
			Yes(USERINFO, "address.User = url.UserPassword(userName, \"exampleSecret\")\n", "credential", "exampleSecret"),
			Yes(USERINFO, "address.User = url.UserPassword(userName, \"pre\"+suffix)\n", "credential", string.Empty),
			Yes(USERINFO, "address := &url.URL{Scheme: \"https\", User: url.UserPassword(userName, secret), Host: \"host.example\"}\n", "construct", "UserPassword"),
			Yes(USERINFO, "address.User = neturl.UserPassword(userName, secret)\n", "construct", "UserPassword"),
			Yes(USERINFO, "address.User = UserPassword(userName, secret)\n", "construct", "UserPassword"),
			Yes(USERINFO, "request.URL.User = url.UserPassword(first(a, b), secret)\n", "construct", "UserPassword"),
			No(USERINFO, "address.User = url.User(\"name\")\n"),
			No(USERINFO, "address.User = url.UserPassword(userName)\n"),
			No(USERINFO, "func UserPassword(left string, right string) string {\n"),
			No(USERINFO, "func (store *Store) UserPassword(left string, right string) string {\n"),
			No(USERINFO, "http.ProxyURL(&url.URL{Scheme: \"http\", Host: \"proxy.example:3128\", User: url.UserPassword(userName, secret)})\n"),
			No(USERINFO, "proxyURL.User = url.UserPassword(userName, secret)\n"),
			No(USERINFO, "address := &url.URL{Scheme: \"postgres\", User: url.UserPassword(userName, secret)}\n"),
			Yes(USERINFO, "address := &url.URL{Scheme: \"HTTPS\", User: url.UserPassword(userName, secret), Host: \"host.example\"}\n", "construct", "UserPassword"),
			Yes(USERINFO, "address := &url.URL{Scheme: \"Http\", User: url.UserPassword(userName, secret), Host: \"host.example\"}\n", "construct", "UserPassword"),
			No(USERINFO, "address := &url.URL{Scheme: \"POSTGRES\", User: url.UserPassword(userName, secret)}\n"),
			No(USERINFO, "secret, _ := address.User.Password()\n"),
			No(USERINFO, "name := address.User.Username()\n")
		];
	}

	// ------------------------------------------------------------------------------------------
	// GO-009, GO-012: a header value held in a variable, or read from the environment
	// ------------------------------------------------------------------------------------------

	private static IEnumerable<Case> CasesOfTheSymbolicClasses()
	{
		const string HELD = "EX-GO-009";
		const string ENVIRONMENT = "EX-GO-012";

		return
		[
			Yes(HELD, "request.Header.Set(\"Authorization\", header)\n", "scheme", "header"),
			Yes(HELD, "request.Header.Add(\"Authorization\", header)\n", "scheme", "header"),
			Yes(HELD, "request.Header[\"Authorization\"] = []string{header}\n", "scheme", "header"),
			Yes(HELD, "request.Header[\"Authorization\"] = append(request.Header[\"Authorization\"], header)\n", "scheme", "header"),
			Yes(HELD, "header := \"Bearer \" + token\nrequest.Header.Set(\"Authorization\", header)\n", "scheme", "header"),
			No(HELD, "request.Header.Set(\"Authorization\", \"Basic \"+encoded)\n"),
			No(HELD, "request.Header.Set(\"Authorization\", os.Getenv(\"SERVICE_AUTH_HEADER\"))\n"),
			No(HELD, "request.Header.Set(\"Authorization\", build(header))\n"),
			No(HELD, "request.Header.Set(\"Authorization\", settings.Header)\n"),
			No(HELD, "request.Header.Set(\"Authorization\", values[0])\n"),
			No(HELD, "request.Header.Set(\"X-Api-Key\", apiKey)\n"),
			No(HELD, "request.Header.Set(\"Proxy-Authorization\", header)\n"),

			Yes(ENVIRONMENT, "request.Header.Set(\"Authorization\", os.Getenv(\"SERVICE_AUTH_HEADER\"))\n", "construct", "Set"),
			Yes(ENVIRONMENT, "request.Header.Set(\"Authorization\", os.LookupEnv(\"SERVICE_AUTH_HEADER\"))\n", "construct", "Set"),
			Yes(ENVIRONMENT, "request.Header[\"Authorization\"] = []string{os.Getenv(\"SERVICE_AUTH_HEADER\")}\n", "construct", "Header"),
			Yes(ENVIRONMENT, "request.Header.Set(\"Authorization\", Getenv(\"SERVICE_AUTH_HEADER\"))\n", "construct", "Set"),
			Yes(ENVIRONMENT, "request.Header.Set(\"Authorization\", fromEnvironment)\n", "scheme", "fromEnvironment"),
			No(ENVIRONMENT, "request.Header.Set(\"Authorization\", \"Basic \"+os.Getenv(\"SERVICE_SECRET\"))\n"),
			No(ENVIRONMENT, "request.Header.Set(\"X-Api-Key\", os.Getenv(\"SERVICE_KEY\"))\n"),
			No(ENVIRONMENT, "port := os.Getenv(\"PORT\")\n")
		];
	}

	// ------------------------------------------------------------------------------------------
	// GO-010, GO-011: the definitions
	// ------------------------------------------------------------------------------------------

	private static IEnumerable<Case> CasesOfTheDefinitionClasses()
	{
		return [.. CasesOfTheTransport(), .. CasesOfTheHelper(), .. CasesOfTheEncodedBasicHelperRow()];
	}

	private static IEnumerable<Case> CasesOfTheTransport()
	{
		const string TRANSPORT = "EX-GO-010";

		return
		[
			Yes(TRANSPORT, "func (transport *basicTransport) RoundTrip(request *http.Request) (*http.Response, error) {\n\trequest.SetBasicAuth(transport.userName, transport.secret)\n\treturn transport.base.RoundTrip(request)\n}\n", "construct", "RoundTrip"),
			Yes(TRANSPORT, "func (transport basicTransport) RoundTrip(request *http.Request) (*http.Response, error) {\n\trequest.Header.Set(\"Authorization\", \"Basic \"+transport.encoded)\n\treturn transport.base.RoundTrip(request)\n}\n", "construct", "RoundTrip"),
			Yes(TRANSPORT, "func (t *T) RoundTrip(r *http.Request) (*http.Response, error) {\n\tr.Header[\"Authorization\"] = []string{\"Basic \" + header}\n\treturn nil, nil\n}\n", "construct", "RoundTrip"),
			No(TRANSPORT, "func (t *T) RoundTrip(r *http.Request) (*http.Response, error) {\n\tr.Header[\"Authorization\"] = []string{header}\n\treturn nil, nil\n}\n"),
			No(TRANSPORT, "func (t *T) RoundTrip(r *http.Request) (*http.Response, error) {\n\tprefix := \"Basic \"\n\treturn nil, nil\n}\n"),
			No(TRANSPORT, "func (t *T) RoundTrip(r *http.Request) (*http.Response, error) {\n\tr.Header.Set(\"Authorization\", os.Getenv(\"SERVICE_AUTH_HEADER\"))\n\treturn nil, nil\n}\n"),
			No(TRANSPORT, "func (t *T) RoundTrip(r *http.Request) (*http.Response, error) {\n\tresponse, err := http.Get(\"https://user:secret@host.example/p\")\n\treturn response, err\n}\n"),
			Yes(TRANSPORT, "func (t *T) RoundTrip(r *http.Request) (*http.Response, error) {\n\tr.Header = map[string][]string{\"Authorization\": {\"Basic \" + x}}\n\treturn nil, nil\n}\n", "construct", "RoundTrip"),
			Yes(TRANSPORT, "func (t *T) RoundTrip(r *http.Request) (*http.Response, error) {\n\tr.Header = http.Header{\"Authorization\": {\"Basic \" + x}}\n\treturn nil, nil\n}\n", "construct", "RoundTrip"),
			Yes(TRANSPORT, "func (t *T) RoundTrip(r *http.Request) (*http.Response, error) {\n\tv := base64.StdEncoding.EncodeToString([]byte(a + \":\" + b)) // Basic\n\treturn nil, nil\n}\n", "construct", "RoundTrip"),
			No(TRANSPORT, "func (t *T) RoundTrip(r *http.Request) (*http.Response, error) {\n\tv := base64.StdEncoding.EncodeToString([]byte(a + \":\" + b))\n\treturn nil, nil\n}\n"),
			No(TRANSPORT, "func (transport *loggingTransport) RoundTrip(request *http.Request) (*http.Response, error) {\n\tlog.Println(request.URL)\n\treturn transport.base.RoundTrip(request)\n}\n"),
			No(TRANSPORT, "func (transport *basicTransport) Send(request *http.Request) error {\n\trequest.SetBasicAuth(transport.userName, transport.secret)\n\treturn nil\n}\n"),
			No(TRANSPORT, "func RoundTrip(request *http.Request) {\n\trequest.SetBasicAuth(u, p)\n}\n"),
			No(TRANSPORT, "func (f roundTripperFunc) RoundTrip(request *http.Request) (*http.Response, error) {\n\treturn f(request), nil\n}\n")
		];
	}

	private static IEnumerable<Case> CasesOfTheHelper()
	{
		const string HELPER = "EX-GO-011";

		return
		[
			Yes(HELPER, "func basicHeader(userName string, secret string) string {\n\treturn \"Basic \" + base64.StdEncoding.EncodeToString([]byte(userName+\":\"+secret))\n}\n", "construct", "basicHeader"),
			No(HELPER, "func basicHeader(userName, secret string) string {\n\treturn \"Basic \" + encode(secret)\n}\n"),
			Yes(HELPER, "func basicHeader(userName, secret string) string {\n\treturn http.Header{\"Authorization\": {\"Basic \" + encode(secret)}}\n}\n", "construct", "basicHeader"),
			Yes(HELPER, "func headers(userName, secret string) map[string]string {\n\treturn map[string]string{\"Authorization\": \"Basic \" + secret}\n}\n", "construct", "headers"),
			Yes(HELPER, "func apply(h http.Header, token string) {\n\th[\"Authorization\"] = []string{\"Basic \" + token}\n}\n", "construct", "apply"),
			No(HELPER, "func apply(h http.Header, token string) {\n\th[\"Authorization\"] = []string{\"Basic \" + other}\n}\n"),
			No(HELPER, "func apply(request *http.Request, token string) {\n\trequest.SetBasicAuth(token, token)\n}\n"),
			No(HELPER, "func apply(request *http.Request, token string) {\n\trequest.Header.Set(\"Authorization\", token)\n}\n"),
			No(HELPER, "func fetch(token string) {\n\tresponse, err := http.Get(\"https://user:\" + token + \"@host.example/p\")\n}\n"),
			Yes(HELPER, "func (client *Client) authorize(request *http.Request, token string) {\n\trequest.Header.Set(\"Authorization\", \"Basic \"+token)\n}\n", "construct", "authorize"),
			Yes(HELPER, "func pair(userName string, secret string) string {\n\t// Basic\n\treturn base64.StdEncoding.EncodeToString([]byte(userName + \":\" + secret))\n}\n", "construct", "pair"),
			No(HELPER, "func pair(userName string, secret string) string {\n\treturn base64.StdEncoding.EncodeToString([]byte(userName + \":\" + secret))\n}\n"),
			No(HELPER, "func staticHeader() string {\n\treturn \"Basic \" + stored\n}\n"),
			No(HELPER, "func describe(userName string) string {\n\treturn \"Basic \" + stored\n}\n"),
			No(HELPER, "func (transport *basicTransport) RoundTrip(request *http.Request) (*http.Response, error) {\n\trequest.Header.Set(\"Authorization\", \"Basic \"+request.Host)\n\treturn nil, nil\n}\n"),
			No(HELPER, "func plain(userName string) string {\n\treturn userName\n}\n"),
			Yes(HELPER, "func apply(h http.Header, a, b, c, token string) {\n\th[\"Authorization\"] = []string{\"Basic \" + token}\n}\n", "construct", "apply"),
			Yes(HELPER, "func apply(h http.Header, a string, b int, c bool, d string, e string, token string) {\n\th[\"Authorization\"] = []string{\"Basic \" + token}\n}\n", "construct", "apply"),
			No(HELPER, "func apply(h http.Header, token string) {\n\th[\"Authorization\"] = []string{\"Basic \" + other}; log(token)\n}\n"),
			No(HELPER, "func apply(h http.Header, token string) {\n\th[\"Authorization\"] = []string{\"Basic \" + tokenized}\n}\n"),
			No(HELPER, "func apply(h http.Header, token string) {\n\th[\"Authorization\"] = []string{\"Basic \" + x.token}\n}\n"),
			Yes(HELPER, "func apply(h http.Header, a string, b int, c bool, d string, e string, f int, token string) {\n\th[\"Authorization\"] = []string{\"Basic \" + token}\n}\n", "construct", "apply"),
			Yes(HELPER, "func apply(a int, b int, c int, d int, e int, f int, g int, token string) {\n\th[\"Authorization\"] = []string{\"Basic \" + token}\n}\n", "construct", "apply"),
			No(HELPER, "func apply(a int, b int, c int, d int, e int, f int, g int, token string, extra string) {\n\th[\"Authorization\"] = []string{\"Basic \" + extra}\n}\n"),
			Yes(HELPER, "func login(user, pass string) string {\n\treturn http.Header{\"Authorization\": {\"Basic \" + pass}}\n}\n", "construct", "login"),
			No(HELPER, "func login(user, pass string) string {\n\treturn http.Header{\"Authorization\": {\"Basic \" + passed}}\n}\n"),
			Yes(HELPER, "func apply(h http.Header, parts ...string) {\n\th[\"Authorization\"] = []string{\"Basic \" + parts}\n}\n", "construct", "apply"),
			No(HELPER, "func apply(h http.Header, parts ...string) {\n\th[\"Authorization\"] = []string{\"Basic \" + parted}\n}\n")
		];
	}

	private static IEnumerable<Case> CasesOfTheEncodedBasicHelperRow()
	{
		const string HELPER = "EX-GO-011";

		return
		[
			Yes(HELPER, "func basicHeader(userName string, secret string) string {\n\tpair := userName + \":\" + secret\n\treturn \"Basic \" + base64.StdEncoding.EncodeToString([]byte(pair))\n}\n", "construct", "basicHeader"),
			Yes(HELPER, "func basicHeader(pair string) string {\n\treturn \"Basic \" + base64.StdEncoding.EncodeToString([]byte(pair))\n}\n", "construct", "basicHeader"),
			Yes(HELPER, "func basicHeader(raw []byte) string {\n\tencoded := base64.StdEncoding.EncodeToString(raw)\n\treturn \"Basic \" + encoded\n}\n", "construct", "basicHeader"),
			Yes(HELPER, "func basicHeader(raw []byte) string {\n\treturn `Basic ` + base64.RawStdEncoding.EncodeToString(raw)\n}\n", "construct", "basicHeader"),
			Yes(HELPER, "func basicHeader(raw []byte) string {\n\treturn \"BASIC \" + transport.token + base64.StdEncoding.AppendEncode(nil, raw)\n}\n", "construct", "basicHeader"),
			Yes(HELPER, "func basicHeader(token string) string {\n\tprefix := \"Basic \" + token\n\treturn prefix + base64.StdEncoding.EncodeToString(data)\n}\n", "construct", "basicHeader"),
			Yes(HELPER, "func basicHeader(token string) string {\n\tencoded := base64.StdEncoding.EncodeToString(data)\n\n\tprefix := \"Basic \" + encoded\n\treturn log(token, prefix)\n}\n", "construct", "basicHeader"),
			No(HELPER, "func basicHeader(userName string, secret string) string {\n\treturn base64.StdEncoding.EncodeToString([]byte(userName + \":\" + secret))\n}\n"),
			No(HELPER, "func basicHeader(token string) string {\n\treturn fmt.Sprintf(\"Basic %s\", token)\n}\n"),
			No(HELPER, "func basicHeader(token string) string {\n\tencoded := base64.URLEncoding.EncodeToString(data)\n\treturn \"Basic \" + encoded + token\n}\n"),
			No(HELPER, "func basicHeader(token string) string {\n\tencoded := base64.StdEncoding.EncodeToString(data)\n\treturn \"Basic \" + encoded\n}\n"),
			No(HELPER, "func challenge(realm string) string {\n\treturn \"Basic realm=\\\"\" + realm + \"\\\"\"\n}\n"),
			No(HELPER, "func challenge(realm string) string {\n\tnonce := base64.StdEncoding.EncodeToString(raw)\n\treturn \"Basic realm=\\\"\" + realm + nonce + \"\\\"\"\n}\n"),
			No(HELPER, "func basicHeader() string {\n\tencoded := base64.StdEncoding.EncodeToString(data)\n\treturn \"Basic \" + encoded\n}\n"),
			No(HELPER, "func basicHeader[T any](raw T) string {\n\tencoded := base64.StdEncoding.EncodeToString(data)\n\treturn \"Basic \" + encoded + raw\n}\n"),
			No(HELPER, "func basicHeader(token string) string {\n\tencoded := base64.StdEncoding.EncodeToString(data)\n\treturn \"Basic  \" + encoded + token\n}\n")
		];
	}

	// ------------------------------------------------------------------------------------------
	// The two content markers
	// ------------------------------------------------------------------------------------------

	private static IEnumerable<Case> CasesOfTheDemotionMarkers()
	{
		return
		[
			Yes(_GENERATED_MARKER_ID, "// Code generated by a tool. DO NOT EDIT.\n\npackage sample\n"),
			Yes(_GENERATED_MARKER_ID, "// Copyright notice\n//go:build linux\n\n// Code generated from a schema, DO NOT EDIT.\npackage sample\n"),
			Yes(_GENERATED_MARKER_ID, "\n\n// Code generated by a tool. DO NOT EDIT.\n"),
			Yes(_GENERATED_MARKER_ID, "// Code generated by a tool. DO NOT EDIT."),
			No(_GENERATED_MARKER_ID, "package sample\n\n// Code generated by a tool. DO NOT EDIT.\n"),
			No(_GENERATED_MARKER_ID, "/* Code generated by a tool. DO NOT EDIT. */\npackage sample\n"),
			No(_GENERATED_MARKER_ID, "// Code generated by a tool.\npackage sample\n"),
			No(_GENERATED_MARKER_ID, "// This code was not generated. DO NOT EDIT.\n"),
			No(_GENERATED_MARKER_ID, "// Code generated by a tool. DO NOT EDIT. Really.\n"),

			Yes(_EXAMPLE_MARKER_ID, "func Example() {\n"),
			Yes(_EXAMPLE_MARKER_ID, "func ExampleClient() {\n"),
			Yes(_EXAMPLE_MARKER_ID, "func ExampleClient_Do() {\n"),
			Yes(_EXAMPLE_MARKER_ID, "func Example_suffix() {\n"),
			Yes(_EXAMPLE_MARKER_ID, "package sample\n\nfunc ExampleNew() {\n"),
			No(_EXAMPLE_MARKER_ID, "func Examples() {\n"),
			No(_EXAMPLE_MARKER_ID, "// func Example() {\n"),
			No(_EXAMPLE_MARKER_ID, "func ExampleClient(t *testing.T) {\n"),
			No(_EXAMPLE_MARKER_ID, "\tfunc Example() {\n"),
			No(_EXAMPLE_MARKER_ID, "func TestExample() {\n")
		];
	}

	// ------------------------------------------------------------------------------------------
	// The flow signals
	// ------------------------------------------------------------------------------------------

	private static IEnumerable<Case> FlowCasesOfTheCommandLineSignals()
	{
		return
		[
			Yes("EX-GO-120", "password, err := term.ReadPassword(int(os.Stdin.Fd()))\n", "anchor", "ReadPassword"),
			Yes("EX-GO-120", "password, err := terminal.ReadPassword(fd)\n", "anchor", "ReadPassword"),
			Yes("EX-GO-120", "password, err := ReadPassword(int(syscall.Stdin))\n", "anchor", "ReadPassword"),
			Yes("EX-GO-120", "password, err := term.ReadPassword(descriptor)\n", "anchor", "ReadPassword"),
			No("EX-GO-120", "password, err := ReadPassword(path)\n"),
			No("EX-GO-120", "func ReadPassword(fd int) ([]byte, error) {\n"),
			No("EX-GO-120", "func (store *Store) ReadPassword(fd int) ([]byte, error) {\n"),

			Yes("EX-GO-121", "reader := bufio.NewReader(os.Stdin)\nfmt.Print(\"Password: \")\n", "anchor", "Stdin"),
			Yes("EX-GO-121", "fmt.Print(\"Enter your password: \")\nscanner := bufio.NewScanner(os.Stdin)\n", "anchor", "Stdin"),
			Yes("EX-GO-121", "fmt.Print(`Passphrase`)\nline, err := reader.ReadString('\\n')\nreader = bufio.NewReader(os.Stdin)\n", "anchor", "Stdin"),
			No("EX-GO-121", "scanner := bufio.NewScanner(os.Stdin)\nfor scanner.Scan() {\n\tlines = append(lines, scanner.Text())\n}\n"),
			No("EX-GO-121", "fmt.Print(\"Password: \")\nvalue := os.Args[1]\n"),
			No("EX-GO-121", "reader := bufio.NewReader(os.Stdin)\nfmt.Print(\"Name: \")\n"),

			Yes("EX-GO-122", "fmt.Scanln(&password)\n", "anchor", "Scanln"),
			Yes("EX-GO-122", "fmt.Scanf(\"%s\", &apiToken)\n", "anchor", "Scanf"),
			Yes("EX-GO-122", "fmt.Scan(&userPass)\n", "anchor", "Scan"),
			Yes("EX-GO-122", "fmt.Fscan(os.Stdin, &secret)\n", "anchor", "Fscan"),
			Yes("EX-GO-122", "fmt.Fscanln(os.Stdin, &password)\n", "anchor", "Fscanln"),
			No("EX-GO-122", "fmt.Scanln(&count)\n"),
			No("EX-GO-122", "fmt.Sscanf(text, \"%d\", &password)\n"),
			No("EX-GO-122", "fmt.Fscan(file, &password)\n"),
			No("EX-GO-122", "fmt.Scanln(&bypass)\n")
		];
	}

	private static IEnumerable<Case> FlowCasesOfTheWebServerSignals()
	{
		return
		[
			Yes("EX-GO-123", "password := request.FormValue(\"password\")\n", "anchor", "FormValue"),
			Yes("EX-GO-123", "password := request.PostFormValue(\"passwd\")\n", "anchor", "PostFormValue"),
			Yes("EX-GO-123", "password := request.PostForm.Get(\"password\")\n", "anchor", "Get"),
			Yes("EX-GO-123", "password := request.Form.Get(\"user_password\")\n", "anchor", "Get"),
			Yes("EX-GO-123", "password := request.Form[\"password\"]\n", "anchor", "Form"),
			Yes("EX-GO-123", "password := request.PostForm[\"password\"]\n", "anchor", "PostForm"),
			No("EX-GO-123", "name := request.FormValue(\"username\")\n"),
			No("EX-GO-123", "target := request.FormValue(\"redirect\")\n"),
			No("EX-GO-123", "password := request.URL.Query().Get(\"password\")\n"),
			No("EX-GO-123", "password := values.Get(\"password\")\n"),

			Yes("EX-GO-124", "const page = `<form><input type=\"password\" name=\"pw\"></form>`\n", "anchor", "<input"),
			Yes("EX-GO-124", "page := \"<input name=\\\"pw\\\" type=\\\"password\\\">\"\n", "anchor", "<input"),
			Yes("EX-GO-124", "page := `<INPUT TYPE='password'>`\n", "anchor", "<INPUT"),
			No("EX-GO-124", "page := `<input type=\"text\" name=\"password-hint\">`\n"),
			No("EX-GO-124", "page := `<input type=\"text\"><label type=\"password\"></label>`\n"),
			No("EX-GO-124", "page := `<label>password</label>`\n"),

			Yes("EX-GO-125", "http.HandleFunc(\"/login\", loginHandler)\n", "anchor", "HandleFunc"),
			Yes("EX-GO-125", "mux.Handle(\"POST /login\", handler)\n", "anchor", "Handle"),
			Yes("EX-GO-125", "mux.HandleFunc(\"/account/sign-in\", handler)\n", "anchor", "HandleFunc"),
			Yes("EX-GO-125", "mux.HandleFunc(\"/logon/{next}\", handler)\n", "anchor", "HandleFunc"),
			No("EX-GO-125", "http.HandleFunc(\"/healthz\", health)\n"),
			No("EX-GO-125", "http.HandleFunc(\"/api/login-events\", events)\n"),
			No("EX-GO-125", "http.HandleFunc(\"/logout\", logout)\n"),
			No("EX-GO-125", "http.HandleFunc(\"/blogin\", other)\n")
		];
	}

	private static IEnumerable<Case> FlowCasesOfTheFixedSignals()
	{
		return
		[
			Yes("EX-GO-130", "secret := os.Getenv(\"SERVICE_PASSWORD\")\n", "anchor", "Getenv"),
			Yes("EX-GO-130", "value, found := os.LookupEnv(\"DB_PASS\")\n", "anchor", "LookupEnv"),
			Yes("EX-GO-130", "value := os.Getenv(\"dbPass\")\n", "anchor", "Getenv"),
			No("EX-GO-130", "port := os.Getenv(\"PORT\")\n"),
			No("EX-GO-130", "home := os.Getenv(\"HOME\")\n"),
			No("EX-GO-130", "path := os.Getenv(\"GOPATH\")\n"),
			No("EX-GO-130", "host := os.Getenv(\"HOST\")\n"),
			No("EX-GO-130", "bypass := os.Getenv(\"BYPASS\")\n"),

			Yes("EX-GO-131", "secret := config.ClientSecret\n", "anchor", "config"),
			Yes("EX-GO-131", "password := cfg.Password\n", "anchor", "cfg"),
			Yes("EX-GO-131", "password := config.Get(\"db.password\")\n", "anchor", "config"),
			Yes("EX-GO-131", "token := app.settings.ApiToken\n", "anchor", "settings"),
			Yes("EX-GO-131", "secret := cfg.Account.Secret\n", "anchor", "cfg"),
			Yes("EX-GO-131", "name := config.Account.Password\n", "anchor", "config"),
			No("EX-GO-131", "name := cfg.Account.Name\n"),
			No("EX-GO-131", "name := cfg.Region.Zone.Secret\n"),
			Yes("EX-GO-131", "creds := svc.Settings[account]\n", "anchor", "creds"),
			Yes("EX-GO-131", "secretPair := app.config[key]\n", "anchor", "secretPair"),
			No("EX-GO-131", "port := svc.Settings[account]\n"),
			No("EX-GO-131", "creds := svc.Settings\n"),
			No("EX-GO-131", "creds := other[tenant]\n"),
			No("EX-GO-131", "creds := reconfig[tenant]\n"),
			No("EX-GO-131", "port := config.Port\n"),
			No("EX-GO-131", "port := config.Get(\"port\")\n"),
			No("EX-GO-131", "secret := reconfig.Secret\n")
		];
	}

	private static IEnumerable<Case> FlowCasesOfTheSecretStoreSignals()
	{
		return
		[

			Yes("EX-GO-132", "value, err := client.GetSecretValue(ctx, input)\n", "anchor", "GetSecretValue"),
			Yes("EX-GO-132", "response, err := client.AccessSecretVersion(ctx, request)\n", "anchor", "AccessSecretVersion"),
			Yes("EX-GO-132", "value, err := store.GetSecret(name)\n", "anchor", "GetSecret"),
			No("EX-GO-132", "values, err := store.GetSecrets(name)\n"),
			No("EX-GO-132", "func (store *Store) GetSecret(name string) (string, error) {\n"),
			No("EX-GO-132", "value := store.GetSecretary\n"),

			Yes("EX-GO-133", "const apiPassword = \"exampleSecretValue\"\n", "anchor", "apiPassword"),
			Yes("EX-GO-133", "options := Options{Password: \"exampleSecret\"}\n", "anchor", "Password"),
			Yes("EX-GO-133", "clientSecret := `exampleSecret`\n", "anchor", "clientSecret"),
			Yes("EX-GO-133", "var token string = \"exampleTokenValue\"\n", "anchor", "token"),
			Yes("EX-GO-133", "password = \"changeme\"\n", "anchor", "password"),
			No("EX-GO-133", "passwordLabel = \"Password\"\n"),
			No("EX-GO-133", "tokenEnvName = \"SERVICE_TOKEN\"\n"),
			No("EX-GO-133", "password := \"\"\n"),
			No("EX-GO-133", "password := os.Getenv(\"SERVICE_PASSWORD\")\n"),
			No("EX-GO-133", "if password == \"admin\" {\n"),
			No("EX-GO-133", "retries := \"three\"\n"),

			Yes("EX-GO-134", "data, err := os.ReadFile(\"/run/secrets/database\")\n", "anchor", "ReadFile"),
			Yes("EX-GO-134", "data, err := ReadFile(\"secret.txt\")\n", "anchor", "ReadFile"),
			Yes("EX-GO-134", "data, err := os.ReadFile(`token.json`)\n", "anchor", "ReadFile"),
			No("EX-GO-134", "data, err := os.ReadFile(\"config.json\")\n"),
			No("EX-GO-134", "data, err := os.ReadFile(templatePath)\n"),
			No("EX-GO-134", "data, err := os.WriteFile(\"secret.txt\", body, 0o600)\n")
		];
	}
}
