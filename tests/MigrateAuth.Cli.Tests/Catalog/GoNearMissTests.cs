using System.Collections.Immutable;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Recognition;
using MigrateAuth.Cli.Tests.Recognition;

using Xunit;

namespace MigrateAuth.Cli.Tests.Catalog;

/// <summary>
/// THE NEAR-MISS FAMILIES OF THE `Go` CATALOG, one fixture text per form the catalog lists, run through the shipped analyzer and the real recognition engine: the seven mandated families
/// (`inbound-authorization-read`, `bearer-or-api-key`, `digest-auth`, `non-http-credentials`, `oauth-client-credentials`, `basic-in-comment-string-or-unreachable`,
/// `base64-non-credential`), the four the catalog adds (`proxy-credentials`, `non-http-userinfo`, `userinfo-read`, `definition-not-call`) and the refusals the expressions carry that are not families.
/// A near-miss is kept out of every class, or read at Low where its anchor is a comment or a string body. Every text is invented, with generic names.
/// </summary>
public sealed class GoNearMissTests
{
	private const string _TEST_FILE_PATH = "src/orders_test.go";

	[Theory]
	// inbound-authorization-read
	[InlineData("user, secret, found := request.BasicAuth()\n")]
	[InlineData("value := request.Header.Get(\"Authorization\")\nraw, err := base64.StdEncoding.DecodeString(strings.TrimPrefix(value, \"Basic \"))\n")]
	[InlineData("writer.Header().Set(\"WWW-Authenticate\", \"Basic realm=\\\"restricted\\\"\")\n")]
	[InlineData("func challenge(realm string) string {\n\treturn \"Basic realm=\\\"\" + realm + \"\\\"\"\n}\n")]
	[InlineData("func challenge(realm string) string {\n\tnonce := base64.StdEncoding.EncodeToString(rawNonce)\n\treturn \"Basic realm=\\\"\" + realm + \"\\\"\"\n}\n")]
	// bearer-or-api-key: the Bearer helper and its call give nothing at the call
	[InlineData("func bearerHeader(token string) string {\n\treturn \"Bearer \" + token\n}\nfunc send() {\n\trequest.Header.Set(\"Authorization\", bearerHeader(token))\n}\n")]
	// basic-in-comment-string-or-unreachable: a bare mention is no match and gives no finding
	[InlineData("message := \"call SetBasicAuth to authenticate\"\n")]
	[InlineData("// SetBasicAuth(userName, secret) is deprecated here\n")]
	[InlineData("request.Header.Set(\"Authorization\", \"Bearer \"+token)\n")]
	[InlineData("headers := http.Header{\"Authorization\": {\"Bearer \" + accessToken}}\n")]
	[InlineData("flat := map[string]string{\"Authorization\": \"Bearer \" + accessToken}\n")]
	[InlineData("request.Header.Set(\"X-Api-Key\", apiKey)\n")]
	[InlineData("header := \"Bearer \" + token\nrequest.Header.Set(\"Authorization\", header)\n")]
	[InlineData("request.Header[\"Authorization\"] = []string{\"Bearer \" + token}\n")]
	// digest-auth
	[InlineData("request.Header.Set(\"Authorization\", `Digest username=\"user\", realm=\"restricted\"`)\n")]
	[InlineData("headers := http.Header{\"Authorization\": {\"Digest \" + response}}\n")]
	// non-http-credentials
	[InlineData("auth := smtp.PlainAuth(\"\", userName, secret, \"smtp.host.example\")\n")]
	[InlineData("connection, err := sql.Open(\"driver\", \"user=name password=secret host=db.example\")\n")]
	[InlineData("options := Options{Username: userName, Password: secret}\n")]
	// oauth-client-credentials
	[InlineData("form := url.Values{\"grant_type\": {\"client_credentials\"}, \"client_id\": {clientID}, \"client_secret\": {secret}}\nresponse, err := http.PostForm(tokenURL, form)\n")]
	[InlineData("challenge := base64.RawURLEncoding.EncodeToString(digest[:])\n")]
	// base64-non-credential
	[InlineData("text := base64.StdEncoding.EncodeToString(payload)\n")]
	[InlineData("key := base64.StdEncoding.EncodeToString([]byte(host + \":\" + port))\n")]
	[InlineData("cacheKey := base64.URLEncoding.EncodeToString(raw)\n")]
	[InlineData("hashed := base64.RawURLEncoding.EncodeToString(digest[:])\n")]
	// proxy-credentials
	[InlineData("request.Header.Set(\"Proxy-Authorization\", \"Basic \"+encoded)\n")]
	[InlineData("encoded := base64.StdEncoding.EncodeToString([]byte(userName + \":\" + secret))\nrequest.Header.Set(\"Proxy-Authorization\", \"Basic \"+encoded)\n")]
	[InlineData("proxy, err := http.ProxyURL(&url.URL{Scheme: \"http\", Host: \"proxy.example:3128\", User: url.UserPassword(userName, secret)})\n")]
	[InlineData("proxyURL := \"http://user:secret@proxy.example:3128\"\n")]
	[InlineData("transport := &http.Transport{ProxyConnectHeader: http.Header{\"Proxy-Authorization\": {\"Basic \" + encoded}}}\n")]
	// non-http-userinfo
	[InlineData("dsn := \"postgres://user:secret@host.example/db\"\n")]
	[InlineData("broker := \"amqp://user:secret@broker.example/\"\n")]
	[InlineData("location := \"ftp://user:secret@host.example/file\"\n")]
	[InlineData("address, err := url.Parse(\"ldap://user:secret@directory.example\")\n")]
	[InlineData("address := &url.URL{Scheme: \"postgres\", User: url.UserPassword(userName, secret)}\n")]
	// userinfo-read
	[InlineData("secret, _ := address.User.Password()\n")]
	[InlineData("name := address.User.Username()\n")]
	[InlineData("safe := address.Redacted()\n")]
	[InlineData("request.URL.User = nil\n")]
	// definition-not-call
	[InlineData("func (client *Client) SetBasicAuth(userName string, secret string) {\n\tclient.userName = userName\n\tclient.secret = secret\n}\n")]
	[InlineData("func UserPassword(left string, right string) string {\n\treturn left + right\n}\n")]
	[InlineData("type SetBasicAuth func(string, string)\n")]
	// the refusals the expressions carry that are not families
	[InlineData("address.User = url.User(\"name\")\n")]
	[InlineData("response, err := http.Get(\"https://user@host.example/\")\n")]
	[InlineData("encoder := base64.NewEncoding(alphabet)\ntext := encoder.EncodeToString([]byte(userName + \":\" + secret))\n")]
	[InlineData("request.Header.Set(\"Accept\", \"application/json\")\n")]
	[InlineData("request.Header.Set(\"X-Authorization\", \"Basic \"+encoded)\n")]
	public void ANearMissThatTheCatalogKeepsOutProducesNoConstructAtAll(string source)
	{
		ImmutableArray<RecognizedConstruct> constructs = new GoRecognitionHarness().Recognize(source);

		Assert.True(constructs.IsEmpty, $"[{Escape(source)}] produced [{Describe(constructs)}]");
	}

	[Theory]
	[InlineData("// request.SetBasicAuth(userName, secret)\n")]
	[InlineData("/* request.Header.Set(\"Authorization\", \"Basic \"+encoded) */\n")]
	[InlineData("/*\n\theaders := http.Header{\"Authorization\": {\"Basic \" + encoded}}\n*/\n")]
	[InlineData("// address, err := url.Parse(\"https://user:secret@host.example/path\")\n")]
	[InlineData("const help = `request.Header.Set(\"Authorization\", \"Basic \"+x)`\n")]
	[InlineData("const help = `\nrequest.SetBasicAuth(userName, secret)\n`\n")]
	[InlineData("message := \"call request.SetBasicAuth(userName, secret) to authenticate\"\n")]
	public void ABasicConstructInACommentOrAStringBodyIsReadAtLowAndNeverAtHighOrMedium(string source)
	{
		ImmutableArray<RecognizedConstruct> constructs = new GoRecognitionHarness().Recognize(source);

		Assert.NotEmpty(constructs);
		Assert.True(constructs.All(construct => construct.Level == ConfidenceLevel.Low), $"[{Escape(source)}] produced [{Describe(constructs)}]");
	}

	[Fact]
	public void ADeadBranchIsNotKeptOutBecauseReachabilityNeedsControlFlowAndIsADeclaredCost()
	{
		ImmutableArray<RecognizedConstruct> constructs = new GoRecognitionHarness().Recognize("if false {\n\trequest.SetBasicAuth(userName, secret)\n}\n");

		Assert.Contains(constructs, construct => construct.PatternClassIdentifier == "GO-001" && construct.Level == ConfidenceLevel.High);
	}

	[Fact]
	public void AnHttptestHandlerThatReadsTheBasicCredentialInATestFileReportsNothingAtHighOrMedium()
	{
		const string source = "func TestHandler(test *testing.T) {\n"
			+ "\thandler := http.HandlerFunc(func(writer http.ResponseWriter, request *http.Request) {\n"
			+ "\t\tuser, secret, found := request.BasicAuth()\n"
			+ "\t\tif !found || user != \"name\" || secret != \"value\" {\n"
			+ "\t\t\twriter.Header().Set(\"WWW-Authenticate\", \"Basic realm=\\\"restricted\\\"\")\n"
			+ "\t\t\twriter.WriteHeader(http.StatusUnauthorized)\n"
			+ "\t\t}\n"
			+ "\t})\n"
			+ "\tserver := httptest.NewServer(handler)\n"
			+ "\tdefer server.Close()\n"
			+ "}\n";
		ImmutableArray<RecognizedConstruct> constructs = new GoRecognitionHarness().Recognize(source, _TEST_FILE_PATH);

		Assert.True(constructs.IsEmpty, $"produced [{Describe(constructs)}]");
	}

	[Fact]
	public void AProxyPairThatSitsMoreThanTheStatementWindowAwayFromTheProxyHeaderIsStillReadAtMedium()
	{
		string source = "encoded := base64.StdEncoding.EncodeToString([]byte(userName + \":\" + secret)) // Authorization\n"
			+ new string('x', 400) + "\n"
			+ "request.Header.Set(\"Proxy-Authorization\", \"Basic \"+encoded)\n";
		ImmutableArray<RecognizedConstruct> constructs = new GoRecognitionHarness().Recognize(source);

		Assert.Contains(constructs, construct => construct.PatternClassIdentifier == "GO-006" && construct.Level == ConfidenceLevel.Medium);
	}

	[Fact]
	public void AProxyUserInfoMoreThanFourHundredCharactersIntoTheProxyCallIsReadAtMediumAsTheCatalogDeclares()
	{
		string source = "proxy, err := http.ProxyURL(" + new string('x', 450) + " + url.UserPassword(userName, secret))\n";
		ImmutableArray<RecognizedConstruct> constructs = new GoRecognitionHarness().Recognize(source);

		Assert.Contains(constructs, construct => construct.PatternClassIdentifier == "GO-007" && construct.Level == ConfidenceLevel.Medium);
	}

	[Fact]
	public void ALiteralWhoseUserinfoHoldsAnEscapeIsNotReadByTheUrlClassesAsTheCatalogDeclares()
	{
		AssertNoClassFires("response, err := http.Get(\"https://us\\x41er:secret@host.example/path\")\n", "GO-005");
		AssertNoClassFires("const serviceURL = \"https://user:sec\\x41ret@host.example/path\"\n", "GO-008");
	}

	[Fact]
	public void ASchemeWordInAnyCaseIsReadAsHttpAndAnotherSchemeIsRefusedBeforeUserPassword()
	{
		GoRecognitionHarness harness = new();

		Assert.Contains(harness.Recognize("address := &url.URL{Scheme: \"HTTPS\", User: url.UserPassword(userName, secret)}\n"), construct => construct.PatternClassIdentifier == "GO-007");
		Assert.Contains(harness.Recognize("address := &url.URL{Scheme: \"Http\", User: url.UserPassword(userName, secret)}\n"), construct => construct.PatternClassIdentifier == "GO-007");
		Assert.DoesNotContain(harness.Recognize("address := &url.URL{Scheme: \"amqp\", User: url.UserPassword(userName, secret)}\n"), construct => construct.PatternClassIdentifier == "GO-007");
	}

	[Fact]
	public void AProxyHeaderIsRefusedInAnyCaseAndByTheStatementLevelRefusalWhicheverUseWordMeetsTheRequirement()
	{
		GoRecognitionHarness harness = new();

		AssertNoClassFires("encoded := base64.StdEncoding.EncodeToString([]byte(userName + \":\" + secret))\nrequest.Header.Set(\"PROXY-AUTHORIZATION\", \"Basic \"+encoded)\n", "GO-006");
		AssertNoClassFires("encoded := base64.StdEncoding.EncodeToString([]byte(userName + \":\" + secret))\nrequest.Header.Set(\"proxy-authorization\", encoded)\n", "GO-006");
		Assert.Contains(
			harness.Recognize("encoded := base64.StdEncoding.EncodeToString([]byte(userName + \":\" + secret))\nname := \"ProxyAuthorizationHeader\"\nrequest.Header.Set(\"Authorization\", \"Basic \"+encoded)\n"),
			construct => construct.PatternClassIdentifier == "GO-006");
	}

	// ------------------------------------------------------------------------------------------
	// Helpers
	// ------------------------------------------------------------------------------------------

	private static void AssertNoClassFires(string source, string patternClass)
	{
		ImmutableArray<RecognizedConstruct> constructs = new GoRecognitionHarness().Recognize(source);

		Assert.DoesNotContain(constructs, construct => construct.PatternClassIdentifier == patternClass);
	}

	private static string Describe(ImmutableArray<RecognizedConstruct> constructs)
	{
		return string.Join(", ", constructs.Select(construct => $"{construct.PatternClassIdentifier}:{construct.Level}"));
	}

	private static string Escape(string text)
	{
		return text.Replace("\n", "\\n", StringComparison.Ordinal).Replace("\t", "\\t", StringComparison.Ordinal);
	}
}
