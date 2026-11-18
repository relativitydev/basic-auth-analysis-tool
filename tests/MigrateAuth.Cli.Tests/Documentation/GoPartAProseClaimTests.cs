using System.Collections.Immutable;
using System.Globalization;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Recognition;
using MigrateAuth.Cli.Tests.Recognition;

using Xunit;

namespace MigrateAuth.Cli.Tests.Documentation;

/// <summary>
/// Part A of Go says, for the classes whose rules are subtle, what the tool reads and what it does not, and a reader acts on those sentences. Each sentence below is a
/// claim, a table row: the section whose prose makes it, a phrase copied from that prose, an invented input, and what the analyzer must do with it (read at a level, or not read, by the
/// named class). Every phrase must still stand in its section, so a rewritten sentence is re-checked. Every input is invented from the prose, with placeholder names. The limits of the
/// reading (an unterminated block comment, a match in a comment, the demotion of a test data folder and the drop order of the findings cap) are checked as well, against the analyzer and against the cap's own constants.
/// </summary>
public sealed class GoPartAProseClaimTests
{
	private const string _REQUEST_CLASS = "GO-001";
	private const string _HEADER_CALL_CLASS = "GO-002";
	private const string _HEADER_LITERAL_CLASS = "GO-003";
	private const string _MAP_LITERAL_CLASS = "GO-004";
	private const string _URL_CALL_CLASS = "GO-005";
	private const string _ENCODER_CLASS = "GO-006";
	private const string _USERINFO_CLASS = "GO-007";
	private const string _HELD_URL_CLASS = "GO-008";
	private const string _HELD_HEADER_CLASS = "GO-009";
	private const string _TRANSPORT_CLASS = GoPartATestConstants.TRANSPORT_CLASS;
	private const string _HELPER_CLASS = GoPartATestConstants.HELPER_CLASS;
	private const string _ENVIRONMENT_CLASS = GoPartATestConstants.ENVIRONMENT_CLASS;
	private const string _HELPER_CALL_CLASS = GoPartATestConstants.HELPER_CALL_CLASS;
	private const string _SET_CALL = "func send() {\n\trequest.Header.Set(\"Authorization\", basicHeader(userName, secret))\n}\n";
	private const string _PAIR_REQUEST = "request.SetBasicAuth(userName, \"exampleSecret\")\n";
	private const string _TEST_DATA_PATH = "testdata/login_case.go";

	private sealed record ProseClaim(string Section, string Phrase, string PatternClass, bool IsRead, ConfidenceLevel Level, string Source);

	private static readonly ImmutableArray<ProseClaim> _claims =
	[
		// GO-001: the forms the method class does not read
		new(_REQUEST_CLASS, "a declaration `func (c *T) SetBasicAuth(`", _REQUEST_CLASS, false, ConfidenceLevel.High, "func (c *T) SetBasicAuth(userName string, secret string) {\n}\n"),
		new(_REQUEST_CLASS, "a method value that is not called (`f := request.SetBasicAuth`)", _REQUEST_CLASS, false, ConfidenceLevel.High, "f := request.SetBasicAuth\n"),
		new(_REQUEST_CLASS, "through the method expression `(*http.Request).SetBasicAuth(request, userName, secret)`", _REQUEST_CLASS, true, ConfidenceLevel.High, "(*http.Request).SetBasicAuth(request, userName, secret)\n"),
		// GO-002: the header call, the values it reads and the values it does not
		new(_HEADER_CALL_CLASS, "a leading word other than `Basic` (`Bearer`, `Digest`)", _HEADER_CALL_CLASS, false, ConfidenceLevel.High, "request.Header.Set(\"Authorization\", \"Bearer \"+accessToken)\n"),
		new(_HEADER_CALL_CLASS, "the names `Proxy-Authorization` and `X-Authorization`", _HEADER_CALL_CLASS, false, ConfidenceLevel.High, "request.Header.Set(\"Proxy-Authorization\", \"Basic \"+encoded)\n"),
		new(_HEADER_CALL_CLASS, "a header name held in a constant", _HEADER_CALL_CLASS, false, ConfidenceLevel.High, "request.Header.Set(headerName, \"Basic \"+encoded)\n"),
		new(_HEADER_CALL_CLASS, "the method expression `http.Header.Set(h, ...)`", _HEADER_CALL_CLASS, false, ConfidenceLevel.High, "http.Header.Set(h, \"Authorization\", \"Basic \"+encoded)\n"),
		new(_HEADER_CALL_CLASS, "A read of the incoming header (`request.Header.Get(\"Authorization\")`)", _HEADER_CALL_CLASS, false, ConfidenceLevel.High, "value := request.Header.Get(\"Authorization\")\n"),
		new(_HEADER_CALL_CLASS, "a call of `Sprintf` whose format literal starts so (`fmt.Sprintf(\"Basic %s\", encoded)`)", _HEADER_CALL_CLASS, true, ConfidenceLevel.High, "request.Header.Set(\"Authorization\", fmt.Sprintf(\"Basic %s\", encoded))\n"),
		new(_HEADER_CALL_CLASS, "`Add` is read as well as `Set`", _HEADER_CALL_CLASS, true, ConfidenceLevel.High, "request.Header.Add(\"Authorization\", \"Basic \"+encoded)\n"),
		// GO-003 and GO-004: the literals
		new(_HEADER_LITERAL_CLASS, "the key `Proxy-Authorization`", _HEADER_LITERAL_CLASS, false, ConfidenceLevel.High, "headers := http.Header{\"Proxy-Authorization\": {\"Basic \" + encoded}}\n"),
		new(_HEADER_LITERAL_CLASS, "Not read: a leading word other than `Basic`", _HEADER_LITERAL_CLASS, false, ConfidenceLevel.High, "headers := http.Header{\"Authorization\": {\"Bearer \" + accessToken}}\n"),
		new(_MAP_LITERAL_CLASS, "Not read: a leading word other than `Basic`.", _MAP_LITERAL_CLASS, false, ConfidenceLevel.Medium, "flat := map[string]string{\"Authorization\": \"Bearer \" + accessToken}\n"),
		new(_MAP_LITERAL_CLASS, "The conversion `http.Header(map[string][]string{...})` is read through the literal inside it.", _MAP_LITERAL_CLASS, true, ConfidenceLevel.Medium, "headers := http.Header(map[string][]string{\"Authorization\": {\"Basic \" + encoded}})\n"),
		// GO-005: the schemes, the nesting and the assembled address
		new(_URL_CALL_CLASS, "every other scheme", _URL_CALL_CLASS, false, ConfidenceLevel.High, "response, err := client.Get(\"ftp://user:secret@host.example/path\")\n"),
		new(_URL_CALL_CLASS, "an address with no password (`user@host`)", _URL_CALL_CLASS, false, ConfidenceLevel.High, "response, err := http.Get(\"https://user@host.example/path\")\n"),
		new(_URL_CALL_CLASS, "a literal inside a call that is itself an argument (`f(\"https://...\")`)", _URL_CALL_CLASS, false, ConfidenceLevel.High, "response, err := http.Get(build(\"https://user:secret@host.example/path\"))\n"),
		new(_URL_CALL_CLASS, "an address assembled from parts", _URL_CALL_CLASS, false, ConfidenceLevel.High, "response, err := http.Get(\"https://\" + userName + \":\" + secret + \"@host.example/path\")\n"),
		// GO-006: the alphabets, the use word and the proxy refusal
		new(_ENCODER_CLASS, "`URLEncoding` and `RawURLEncoding`", _ENCODER_CLASS, false, ConfidenceLevel.Medium, "pair := base64.URLEncoding.EncodeToString([]byte(userName + \":\" + secret))\nvalue := \"Basic \" + pair\n"),
		new(_ENCODER_CLASS, "every decoder", _ENCODER_CLASS, false, ConfidenceLevel.Medium, "raw, err := base64.StdEncoding.DecodeString(strings.TrimPrefix(value, \"Basic \"))\n"),
		new(_ENCODER_CLASS, "so a cache key or a `host:port` text is not read", _ENCODER_CLASS, false, ConfidenceLevel.Medium, "key := base64.StdEncoding.EncodeToString([]byte(userName + \":\" + secret))\n"),
		new(_ENCODER_CLASS, "it is not read when `Proxy-Authorization` stands within the same 300 characters on that side", _ENCODER_CLASS, false, ConfidenceLevel.Medium, "request.Header.Set(\"Proxy-Authorization\", \"Basic \"+base64.StdEncoding.EncodeToString([]byte(userName+\":\"+secret)))\n"),
		new(_ENCODER_CLASS, "a pair built in another statement and encoded in a later one", _ENCODER_CLASS, false, ConfidenceLevel.Medium, "pair := userName + \":\" + secret\nencoded := base64.StdEncoding.EncodeToString([]byte(pair))\nvalue := \"Basic \" + encoded\n"),
		new(_ENCODER_CLASS, "a single whole literal that holds a colon (`\"user:secret\"`)", _ENCODER_CLASS, true, ConfidenceLevel.Medium, "pair := base64.StdEncoding.EncodeToString([]byte(\"user:secret\"))\nvalue := \"Basic \" + pair\n"),
		// GO-007: the refusals
		new(_USERINFO_CLASS, "`url.User(name)`, which has no password", _USERINFO_CLASS, false, ConfidenceLevel.Medium, "address.User = url.User(userName)\n"),
		new(_USERINFO_CLASS, "a call that begins within 400 characters after the opening parenthesis of a `ProxyURL(`", _USERINFO_CLASS, false, ConfidenceLevel.Medium, "transport.Proxy = http.ProxyURL(&url.URL{Host: \"proxy.example\", User: url.UserPassword(userName, secret)})\n"),
		new(_USERINFO_CLASS, "a call that follows, on the same line, a name that contains `proxy` or `proxies`", _USERINFO_CLASS, false, ConfidenceLevel.Medium, "proxyAddress.User = url.UserPassword(userName, secret)\n"),
		new(_USERINFO_CLASS, "a `Scheme:` field whose value is a literal other than `http` or `https`", _USERINFO_CLASS, false, ConfidenceLevel.Medium, "address := &url.URL{Scheme: \"postgres\", User: url.UserPassword(userName, secret), Host: \"db.example\"}\n"),
		new(_USERINFO_CLASS, "one comma may follow the second argument before the closing parenthesis", _USERINFO_CLASS, true, ConfidenceLevel.Medium, "address.User = url.UserPassword(\n\tuserName,\n\tsecret,\n)\n"),
		// GO-008: the held address
		new(_HELD_URL_CLASS, "a name that contains `proxy` or `proxies`, which is a credential for a proxy", _HELD_URL_CLASS, false, ConfidenceLevel.Medium, "const proxyURL = \"http://user:secret@proxy.example:3128\"\n"),
		new(_HELD_URL_CLASS, "an address held as the value of a struct field key or of a map key", _HELD_URL_CLASS, false, ConfidenceLevel.Medium, "config := Options{BaseURL: \"https://user:secret@host.example/path\"}\n"),
		new(_HELD_URL_CLASS, "any scheme other than `http` or `https`", _HELD_URL_CLASS, false, ConfidenceLevel.Medium, "const serviceURL = \"ftp://user:secret@host.example/path\"\n"),
		new(_HELD_URL_CLASS, "the literal may follow on the next line", _HELD_URL_CLASS, true, ConfidenceLevel.Medium, "const serviceURL =\n\t\"https://user:secret@host.example/path\"\n"),
		// GO-009: the held header value
		new(_HELD_HEADER_CLASS, "A value that begins with another word (`Bearer`, `Token`) is the migrated form and is not reported", _HELD_HEADER_CLASS, false, ConfidenceLevel.Medium, "header := \"Bearer \" + accessToken\nrequest.Header.Set(\"Authorization\", header)\n"),
		new(_HELD_HEADER_CLASS, "A declaration that no header uses gives no finding", _HELD_HEADER_CLASS, false, ConfidenceLevel.Medium, "header := \"Basic \" + encoded\n"),
		new(_HELD_HEADER_CLASS, "a name the file cannot resolve (a parameter, an import, a call, a declaration in another file)", _HELD_HEADER_CLASS, false, ConfidenceLevel.Medium, "func send(request *http.Request, header string) {\n\trequest.Header.Set(\"Authorization\", header)\n}\n"),
		new(_HELD_HEADER_CLASS, "a name that resolves to an environment read is [GO-012](#go-012), and this class stays silent for it", _HELD_HEADER_CLASS, false, ConfidenceLevel.Medium, "value, found := os.LookupEnv(\"SERVICE_AUTH_HEADER\")\nrequest.Header.Set(\"Authorization\", value)\n"),
		new(_HELD_HEADER_CLASS, "a name that is declared more than once", _HELD_HEADER_CLASS, false, ConfidenceLevel.Medium, "header := \"Basic \" + encoded\nheader = \"Basic \" + other\nrequest.Header.Set(\"Authorization\", header)\n"),
		// GO-010 and GO-011: the definitions
		new(_TRANSPORT_CLASS, "The fields of the receiver count as credentials (`transport.userName`).", _TRANSPORT_CLASS, true, ConfidenceLevel.Low, "func (transport *basicTransport) RoundTrip(request *http.Request) (*http.Response, error) {\n\trequest.SetBasicAuth(transport.userName, transport.secret)\n\treturn transport.base.RoundTrip(request)\n}\n"),
		new(_HELPER_CLASS, "a function with no parameter", _HELPER_CLASS, false, ConfidenceLevel.Low, "func basicHeader() string {\n\treturn \"Basic \" + base64.StdEncoding.EncodeToString([]byte(\"user:secret\"))\n}\n"),
		new(_HELPER_CLASS, "a generic function", _HELPER_CLASS, false, ConfidenceLevel.Low, "func basicHeader[T any](userName T, secret T) string {\n\treturn \"Basic \" + base64.StdEncoding.EncodeToString([]byte(userName+\":\"+secret))\n}\n"),
		new(_HELPER_CLASS, "A server's challenge builder (`\"Basic realm=...\"`)", _HELPER_CLASS, false, ConfidenceLevel.Low, "func challenge(realm string) string {\n\treturn \"Basic realm=\\\"\" + realm + \"\\\"\"\n}\n"),
		new(_HELPER_CLASS, "a function that holds only the `\"Basic \"` literal with no encoder and no header write", _HELPER_CLASS, false, ConfidenceLevel.Low, "func prefix(token string) string {\n\treturn \"Basic \" + token\n}\n"),
		// GO-012: the environment value
		new(_ENVIRONMENT_CLASS, "`\"Basic \"+os.Getenv(\"X\")` has a literal leading word", _ENVIRONMENT_CLASS, false, ConfidenceLevel.Low, "request.Header.Set(\"Authorization\", \"Basic \"+os.Getenv(\"X\"))\n"),
		new(_ENVIRONMENT_CLASS, "an environment name held in a constant or a variable (`os.Getenv(headerName)`)", _ENVIRONMENT_CLASS, false, ConfidenceLevel.Low, "request.Header.Set(\"Authorization\", os.Getenv(headerName))\n"),
		new(_ENVIRONMENT_CLASS, "or whose value is a name that the same file declares as an environment read", _ENVIRONMENT_CLASS, true, ConfidenceLevel.Low, "value, found := os.LookupEnv(\"SERVICE_AUTH_HEADER\")\nrequest.Header.Set(\"Authorization\", value)\n"),
		// GO-013: the call to a helper of the same file
		new(_HELPER_CALL_CLASS, "before or after the call", _HELPER_CALL_CLASS, true, ConfidenceLevel.Medium, $"{_SET_CALL}\n{GoPartATestConstants.BASIC_HELPER_DECLARATION}"),
		new(_HELPER_CALL_CLASS, "so `basicHeader(userName, secret) + suffix` is not read", _HELPER_CALL_CLASS, false, ConfidenceLevel.Medium, $"{GoPartATestConstants.BASIC_HELPER_DECLARATION}func send() {{\n\trequest.Header.Set(\"Authorization\", basicHeader(userName, secret) + suffix)\n}}\n"),
		new(_HELPER_CALL_CLASS, "a method call (`client.basicHeader(...)`)", _HELPER_CALL_CLASS, false, ConfidenceLevel.Medium, $"{GoPartATestConstants.BASIC_HELPER_DECLARATION}func send() {{\n\trequest.Header.Set(\"Authorization\", client.basicHeader(userName, secret))\n}}\n"),
		new(_HELPER_CALL_CLASS, "A helper that builds a `Bearer` value", _HELPER_CALL_CLASS, false, ConfidenceLevel.Medium, "func bearerHeader(token string) string {\n\treturn \"Bearer \" + token\n}\n\nfunc send() {\n\trequest.Header.Set(\"Authorization\", bearerHeader(accessToken))\n}\n"),
		new(_HELPER_CALL_CLASS, "a name declared twice, such as a function and a method of the same name", _HELPER_CALL_CLASS, false, ConfidenceLevel.Medium, $"{GoPartATestConstants.BASIC_HELPER_DECLARATION}{GoPartATestConstants.BASIC_HELPER_METHOD_DECLARATION}{_SET_CALL}"),
		new(_HELPER_CALL_CLASS, "a helper in another file or package", _HELPER_CALL_CLASS, false, ConfidenceLevel.Medium, _SET_CALL)
	];

	public static TheoryData<int> ClaimIndexes()
	{
		TheoryData<int> indexes = [];

		for (int index = 0; index < _claims.Length; index++)
		{
			indexes.Add(index);
		}

		return indexes;
	}

	[Theory]
	[MemberData(nameof(ClaimIndexes))]
	public void EveryClaimOfPartAIsTrueOfTheRegisteredRule(int index)
	{
		ProseClaim claim = _claims[index];
		ImmutableArray<RecognizedConstruct> constructs = new GoRecognitionHarness().Recognize(claim.Source);

		if (claim.IsRead)
		{
			Assert.Contains(constructs, construct => construct.PatternClassIdentifier == claim.PatternClass && construct.Level == claim.Level);
		}
		else
		{
			Assert.DoesNotContain(constructs, construct => construct.PatternClassIdentifier == claim.PatternClass);
		}
	}

	/// <summary>The literal rule of Part A: a literal is a credential literal only when it is the whole value, and a literal token of a held declaration raises the in-code secret warning.</summary>
	[Theory]
	[InlineData("request.SetBasicAuth(userName, \"exampleSecret\")\n", _REQUEST_CLASS, true)]
	[InlineData("request.SetBasicAuth(userName, secret)\n", _REQUEST_CLASS, false)]
	[InlineData("request.SetBasicAuth(userName, \"changeme\")\n", _REQUEST_CLASS, false)]
	[InlineData("request.SetBasicAuth(\n\tuserName,\n\t\"exampleSecret\",\n)\n", _REQUEST_CLASS, true)]
	[InlineData("request.Header.Set(\"Authorization\", \"Basic dXNlcjpwYXNz\")\n", _HEADER_CALL_CLASS, true)]
	[InlineData("request.Header.Set(\"Authorization\", \"Basic dXNlcjpwYXNz\"+suffix)\n", _HEADER_CALL_CLASS, false)]
	[InlineData("request.Header.Set(\"Authorization\", \"Basic \"+encoded)\n", _HEADER_CALL_CLASS, false)]
	[InlineData("request.Header.Set(\"Authorization\", \"Basic \"+base64.StdEncoding.EncodeToString([]byte(\"exampleUser:exampleSecret\")))\n", _HEADER_CALL_CLASS, true)]
	[InlineData("address.User = url.UserPassword(userName, \"exampleSecret\")\n", _USERINFO_CLASS, true)]
	[InlineData("address.User = url.UserPassword(userName, os.Getenv(\"SERVICE_SECRET\"))\n", _USERINFO_CLASS, false)]
	[InlineData("response, err := http.Get(\"https://user:exampleSecret@host.example/path\")\n", _URL_CALL_CLASS, true)]
	[InlineData("pair := base64.StdEncoding.EncodeToString([]byte(userName + \":\" + secret))\nvalue := \"Basic \" + pair\n", _ENCODER_CLASS, false)]
	[InlineData("pair := base64.StdEncoding.EncodeToString([]byte(\"user:exampleSecret\"))\nvalue := \"Basic \" + pair\n", _ENCODER_CLASS, true)]
	public void TheLiteralRuleAndTheSecretWarningOfPartAHold(string source, string patternClass, bool expectedWarning)
	{
		string partA = File.ReadAllText(DocumentedLanguage.Go.PartAPath());
		RecognizedConstruct construct = Assert.Single(new GoRecognitionHarness().Recognize(source), candidate => candidate.PatternClassIdentifier == patternClass);

		Assert.Contains("a concatenation of a literal with code is not literal", partA, StringComparison.Ordinal);
		Assert.Contains("the tool also raises an in-code secret warning", partA, StringComparison.Ordinal);
		Assert.Equal(expectedWarning, construct.LiteralCredentialSignal);
	}

	/// <summary>A match in a comment or in a raw string is Low and the same call in code keeps its level, and Part A says so.</summary>
	[Fact]
	public void AMatchInACommentOrInAStringIsLowAndTheSameCallInCodeKeepsItsLevel()
	{
		string partA = File.ReadAllText(DocumentedLanguage.Go.PartAPath());
		GoRecognitionHarness harness = new();

		ImmutableArray<RecognizedConstruct> inComment = harness.Recognize($"// {_PAIR_REQUEST}");
		ImmutableArray<RecognizedConstruct> inRawString = harness.Recognize($"legacy := `{_PAIR_REQUEST.TrimEnd()}`\n");
		ImmutableArray<RecognizedConstruct> inCode = harness.Recognize(_PAIR_REQUEST);

		Assert.Contains("**A match in a comment or in a string is Low.**", partA, StringComparison.Ordinal);
		Assert.All(inComment, construct => Assert.Equal(ConfidenceLevel.Low, construct.Level));
		Assert.All(inRawString, construct => Assert.Equal(ConfidenceLevel.Low, construct.Level));
		Assert.Contains(inCode, construct => construct.PatternClassIdentifier == _REQUEST_CLASS && construct.Level == ConfidenceLevel.High);
	}

	/// <summary>An unterminated block comment abandons the rest of the file: a construct after it is read at Low with no secret warning, and the same file without the comment reads at High with the warning. Part A states it.</summary>
	[Fact]
	public void AnUnterminatedBlockCommentDemotesTheConstructsAfterItAndRaisesNoSecretWarningThere()
	{
		string partA = File.ReadAllText(DocumentedLanguage.Go.PartAPath());
		GoRecognitionHarness harness = new();

		ImmutableArray<RecognizedConstruct> abandoned = harness.Recognize($"/* a note that nothing closes\n{_PAIR_REQUEST}");
		ImmutableArray<RecognizedConstruct> control = harness.Recognize(_PAIR_REQUEST);

		Assert.Contains("An unterminated block comment is different: the tool gives up on the file from the opening of the comment.", partA, StringComparison.Ordinal);
		Assert.Contains("a literal credential there carries no in-code secret warning", partA, StringComparison.Ordinal);
		Assert.DoesNotContain(abandoned, construct => construct.Level != ConfidenceLevel.Low);
		Assert.All(abandoned, construct => Assert.False(construct.LiteralCredentialSignal));
		Assert.Contains(control, construct => construct.PatternClassIdentifier == _REQUEST_CLASS && construct.Level == ConfidenceLevel.High && construct.LiteralCredentialSignal);
	}

	/// <summary>A file under testdata is test code, so a High construct there reads at Medium with the test-code mark and not at Low, and Part A says so.</summary>
	[Fact]
	public void AFileUnderTestdataIsTestCodeAndReadsAtMediumWithTheMark()
	{
		string partA = File.ReadAllText(DocumentedLanguage.Go.PartAPath());
		GoRecognitionHarness harness = new();

		RecognizedConstruct underTestdata = Assert.Single(harness.Recognize(_PAIR_REQUEST, _TEST_DATA_PATH), construct => construct.PatternClassIdentifier == _REQUEST_CLASS);
		RecognizedConstruct ordinary = Assert.Single(harness.Recognize(_PAIR_REQUEST), construct => construct.PatternClassIdentifier == _REQUEST_CLASS);

		Assert.Contains("**A file under `testdata/` is test code and reads at Medium at most**", partA, StringComparison.Ordinal);
		Assert.Equal(ConfidenceLevel.Medium, underTestdata.Level);
		Assert.True(underTestdata.TestCode);
		Assert.Equal(ConfidenceLevel.High, ordinary.Level);
		Assert.False(ordinary.TestCode);
	}

	/// <summary>The drop order of the findings cap, as Part A states it: by path and not by confidence, with the two figures the cap's own constants give.</summary>
	[Fact]
	public void TheFindingsCapDropOrderStatedInPartAIsByPathAndTheFiguresAreTheCapsOwn()
	{
		string partA = File.ReadAllText(DocumentedLanguage.Go.PartAPath());
		string perFile = FindingsCapPolicy.PROVISIONAL_PER_FILE_FINDINGS_CAP.ToString("N0", CultureInfo.InvariantCulture);
		string global = FindingsCapPolicy.PROVISIONAL_GLOBAL_FINDINGS_CAP.ToString("N0", CultureInfo.InvariantCulture);

		Assert.Contains("the drop order is by path prefix and not by confidence", partA, StringComparison.Ordinal);
		Assert.Contains($"more than {global} findings", partA, StringComparison.Ordinal);
		Assert.Contains($"keeps at most {perFile} findings from any one file", partA, StringComparison.Ordinal);
		Assert.Contains("drops the rest, by path and not by confidence", partA, StringComparison.Ordinal);
		Assert.Contains("A Low finding that sorts early is therefore kept ahead of a High finding that sorts late.", partA, StringComparison.Ordinal);
		Assert.Contains("(for example `testdata/`, `third_party/` or `vendor/`)", partA, StringComparison.Ordinal);
	}

	[Fact]
	public void EveryPhraseOfEveryClaimStillStandsInTheSectionThatMakesIt()
	{
		string partA = File.ReadAllText(DocumentedLanguage.Go.PartAPath());
		IReadOnlyDictionary<string, DetectionRule> rules = DocumentedLanguage.Go.Analyzer().DetectionRulesByPatternClass;

		foreach (ProseClaim claim in _claims)
		{
			string section = PublicDocumentChecks.SectionOf(partA, PublicDocumentChecks.ClassHeading(claim.Section, rules[claim.Section].DisplayName)) ?? string.Empty;

			Assert.True(section.Contains(claim.Phrase, StringComparison.Ordinal), $"The section of {claim.Section} no longer holds the phrase: {claim.Phrase}");
		}
	}

	[Fact]
	public void TheClaimsCoverEveryClassOfTheAnalyzer()
	{
		Assert.Equal(
			DocumentedLanguage.Go.Analyzer().DetectionRulesByPatternClass.Keys.Order(StringComparer.Ordinal),
			_claims.Select(claim => claim.Section).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal));
	}
}
