using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Text.RegularExpressions;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Findings;
using MigrateAuth.Cli.Lexical;
using MigrateAuth.Cli.Recognition;
using MigrateAuth.Cli.Reporting;
using MigrateAuth.Cli.Tests.Recognition;

using Xunit;

namespace MigrateAuth.Cli.Tests.Catalog;

/// <summary>
/// The `Python` analyzer against its catalog, through the analyzer the binary ships and the real recognition engine: never through a hand-built construct, because what is under test is whether the expressions the
/// analyzer declares recognize the forms the catalog states and decline the exclusions it states.
///
/// Every script below is INVENTED from the catalog's own prose, with placeholder names throughout (the example.test domain, `user`, `secret`), and carries no fragment, path or third-party name from any repository.
/// </summary>
public sealed class PythonAnalyzerTests
{
	private const string _CLASS_PREFIX = "PY-";
	private const int _CLASS_COUNT = 14;
	private const int _ANCHOR_COUNT = 5;
	private const int _FLOW_SIGNAL_COUNT = 12;
	private const int _FALLBACK_TEXT_COUNT = 2;
	private const string _LOW_CLASS = "PY-014";
	private const string _CONSTRUCT_GROUP = "construct";
	private const string _AUTH_GROUP = "auth";
	private const string _CREDENTIAL_GROUP = "credential";
	private const string _SCHEME_GROUP = "scheme";
	private const string _LOW_REASON_FIELD_NAME = "_patternClassSentencesByIdentifier";

	private static readonly ImmutableArray<string> _everyClass =
		[.. Enumerable.Range(1, _CLASS_COUNT).Select(number => string.Create(System.Globalization.CultureInfo.InvariantCulture, $"{_CLASS_PREFIX}{number:D3}"))];

	private static readonly ImmutableArray<string> _highClasses = ["PY-001", "PY-002", "PY-003", "PY-004", "PY-005"];

	private static readonly ImmutableArray<string> _mediumClasses = ["PY-006", "PY-007", "PY-008", "PY-009", "PY-010", "PY-011", "PY-012", "PY-013"];

	private static readonly ImmutableArray<string> _lowClasses = ["PY-014"];

	/// <summary>The classes that declare the credential capture group (PY-013 since detection catalog 1.50.0, for the token of a held declaration); the other two read no credential.</summary>
	private static readonly ImmutableArray<string> _credentialClasses =
		["PY-001", "PY-002", "PY-003", "PY-005", "PY-006", "PY-007", "PY-008", "PY-009", "PY-011", "PY-012", "PY-013", "PY-014"];

	// ------------------------------------------------------------------------------------------
	// The contract
	// ------------------------------------------------------------------------------------------

	[Fact]
	public void TheValidatorReportsNoViolationForPython()
	{
		Assert.Empty(AnalyzerContractValidator.Validate(PythonShippedAnalyzer.Instance));
	}

	[Fact]
	public void FourteenClassesAreDeclaredFiveHighEightMediumAndOneLow()
	{
		ILanguageAnalyzer analyzer = PythonShippedAnalyzer.Instance;

		Assert.Equal(_CLASS_COUNT, analyzer.DetectionRulesByPatternClass.Count);
		Assert.Equal(_everyClass.Order(StringComparer.Ordinal), analyzer.DetectionRulesByPatternClass.Keys.Order(StringComparer.Ordinal));
		Assert.Equal(_highClasses.AsEnumerable(), ClassesAt(analyzer, ConfidenceLevel.High).AsEnumerable());
		Assert.Equal(_mediumClasses.AsEnumerable(), ClassesAt(analyzer, ConfidenceLevel.Medium).AsEnumerable());
		Assert.Equal(_lowClasses.AsEnumerable(), ClassesAt(analyzer, ConfidenceLevel.Low).AsEnumerable());
	}

	[Fact]
	public void TheHighClassesDeclareTheirSchemeAndCarrierAndTheOthersNeedTheCallPath()
	{
		Assert.All(PythonShippedAnalyzer.Instance.DetectionRulesByPatternClass, entry =>
			Assert.Equal(
				_highClasses.Contains(entry.Key) ? SelfSufficiency.DeclaresSchemeAndCredentialCarrier : SelfSufficiency.RequiresCallPathToBeEstablished,
				entry.Value.SelfSufficiency));
	}

	[Fact]
	public void TheLowByDefinitionClassIsAKeyOfTheReportVocabularyOrTheVocabularyHoldsNoPythonRow()
	{
		System.Reflection.FieldInfo field = typeof(LowReasonVocabulary).GetField(_LOW_REASON_FIELD_NAME, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
			?? throw new InvalidOperationException("The pattern class sentences of the vocabulary were not found.");
		FrozenDictionary<string, string> sentences = (FrozenDictionary<string, string>)field.GetValue(null)!;
		string[] vocabularyKeys = [.. sentences.Keys.Where(key => key.StartsWith(_CLASS_PREFIX, StringComparison.Ordinal)).Order(StringComparer.Ordinal)];

		Assert.True(vocabularyKeys.Length == 0 || vocabularyKeys.SequenceEqual(_lowClasses), $"The vocabulary rows [{string.Join(", ", vocabularyKeys)}] are not the Low class of Python.");
	}

	[Fact]
	public void EveryExpressionIsCaseSensitiveCarriesTheInvariantOptionsAndThePolicyTimeout()
	{
		const RegexOptions required = RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture;

		Assert.All(PythonShippedAnalyzer.Instance.DetectionRulesByPatternClass, entry =>
		{
			Assert.Equal(required, entry.Value.Expression.Options & required);
			Assert.Equal(RegexOptions.None, entry.Value.Expression.Options & RegexOptions.IgnoreCase);
			Assert.Equal(RecognitionEnginePolicy.MATCH_BOUND, entry.Value.Expression.MatchTimeout);
		});
	}

	[Fact]
	public void EveryRuleAnchorsOnTheGroupTheCatalogNamesAndTheTupleClassesShareTheAuthAnchor()
	{
		Assert.All(PythonShippedAnalyzer.Instance.DetectionRulesByPatternClass, entry =>
		{
			string anchorGroup = entry.Key is "PY-002" or "PY-006" ? _AUTH_GROUP : _CONSTRUCT_GROUP;

			Assert.Equal(anchorGroup, entry.Value.AnchorPosition.CaptureGroupName);
			Assert.Contains(anchorGroup, entry.Value.Expression.GetGroupNames());
			Assert.Equal(DestinationCertainty.Unknown, entry.Value.DestinationCertainty);
		});
	}

	[Fact]
	public void OnlyTheHeldHeaderClassDeclaresASchemeGroupAndItsCellIsTheCatalogsCell()
	{
		Assert.All(PythonShippedAnalyzer.Instance.DetectionRulesByPatternClass, entry =>
			Assert.Equal(entry.Key == "PY-013", entry.Value.SymbolicVerdictInterpretation.IsSubjectToSymbolicResolution));

		SymbolicVerdictInterpretation cell = PythonShippedAnalyzer.Instance.DetectionRulesByPatternClass["PY-013"].SymbolicVerdictInterpretation;

		Assert.Equal(_SCHEME_GROUP, cell.SchemeArgumentCaptureGroupName);
		Assert.Equal("Basic", cell.ExpectedSchemeToken);
		Assert.Equal(SymbolicVerdictResponse.ConfirmsPatternClass, cell.WhenResolvedMatching);
		Assert.Equal(SymbolicVerdictResponse.SuppressesFinding, cell.WhenResolvedNotMatching);
		Assert.Equal(SymbolicVerdictResponse.SuppressesFinding, cell.WhenIndeterminate);
		Assert.Contains(_SCHEME_GROUP, PythonShippedAnalyzer.Instance.DetectionRulesByPatternClass["PY-013"].Expression.GetGroupNames());
	}

	[Fact]
	public void TheCredentialPositionsAreTheOnesTheCatalogDeclares()
	{
		Assert.All(PythonShippedAnalyzer.Instance.DetectionRulesByPatternClass, entry =>
		{
			CredentialPosition position = entry.Value.CredentialPosition;

			if (_credentialClasses.Contains(entry.Key))
			{
				Assert.Equal(_CREDENTIAL_GROUP, position.CaptureGroupName);
				Assert.False(position.ReadsABareWordAsLiteral, entry.Key);
				Assert.Contains(_CREDENTIAL_GROUP, entry.Value.Expression.GetGroupNames());
			}
			else
			{
				Assert.False(position.IsDeclared, entry.Key);
			}
		});
	}

	[Fact]
	public void TheSubsumptionRelationIsTheSixTriplesOfTheCatalog()
	{
		(string Subsuming, string Subsumed, SubsumptionCondition When)[] expected =
		[
			("PY-002", "PY-006", SubsumptionCondition.AnchorPositionsAreIdentical),
			("PY-003", "PY-007", SubsumptionCondition.MatchSpansOverlap),
			("PY-003", "PY-008", SubsumptionCondition.MatchSpansOverlap),
			("PY-003", "PY-014", SubsumptionCondition.MatchSpansOverlap),
			("PY-007", "PY-014", SubsumptionCondition.MatchSpansOverlap),
			("PY-011", "PY-012", SubsumptionCondition.MatchSpansOverlap)
		];
		FrozenSet<SubsumptionRule> relation = PythonShippedAnalyzer.Instance.SubsumptionRelation;

		Assert.Equal(expected.Length, relation.Count);
		Assert.Equal(
			expected.Select(triple => $"{triple.Subsuming}>{triple.Subsumed}:{triple.When}").Order(StringComparer.Ordinal),
			relation.Select(rule => $"{rule.SubsumingPatternClassIdentifier}>{rule.SubsumedPatternClassIdentifier}:{rule.When}").Order(StringComparer.Ordinal));
	}

	[Fact]
	public void TheFiveAnchorsAreInsensitiveAndBasicIsNotOne()
	{
		FrozenSet<PreselectionAnchor> anchors = PythonShippedAnalyzer.Instance.CandidateSelectionAndRecommendation.PreselectionAnchors;

		Assert.Equal(_ANCHOR_COUNT, anchors.Count);
		Assert.All(anchors, anchor => Assert.Equal(CaseSensitivity.Insensitive, anchor.Sensitivity));
		Assert.Equal(new[] { "://", "add_password", "auth", "b64encode", "encodebytes" }.Order(StringComparer.Ordinal), anchors.Select(anchor => anchor.Text).Order(StringComparer.Ordinal));
	}

	[Fact]
	public void TheTemplatesAndTheFlowSignalsAreDeclaredAndOnlyTheLowClassCarriesNoTemplate()
	{
		CandidateSelectionAndRecommendation element = PythonShippedAnalyzer.Instance.CandidateSelectionAndRecommendation;

		Assert.All(_everyClass.Where(patternClass => patternClass != _LOW_CLASS), patternClass =>
		{
			Assert.NotNull(element.TemplateProvider(patternClass, CredentialFlow.ClientCredentials));
			Assert.NotNull(element.TemplateProvider(patternClass, CredentialFlow.AuthorizationCode));
		});
		Assert.Null(element.TemplateProvider(_LOW_CLASS, CredentialFlow.ClientCredentials));
		Assert.Null(element.TemplateProvider(_LOW_CLASS, CredentialFlow.AuthorizationCode));
		Assert.Equal(_FLOW_SIGNAL_COUNT, element.FlowSignals.Length);
		Assert.Equal(_FALLBACK_TEXT_COUNT, element.LanguageFallbackTextByFlow!.Count);
	}

	[Fact]
	public void TheConstantDeclarationReadsTheDeclarationsTheSymbolTableCountsAndDeclaresTheFourGroupsTheEngineKeysOn()
	{
		Regex declaration = PythonShippedAnalyzer.Instance.LexicalProfile.ConstantDeclarationExpression;

		Assert.Equal(["environment", "fallbackWord", "name", "value"], declaration.GetGroupNames().Where(group => !int.TryParse(group, out _)).Order(StringComparer.Ordinal));
		Assert.Equal("scheme", declaration.Match("scheme = \"Basic\"").Groups["name"].Value);
		Assert.Equal("SCHEME", declaration.Match("SCHEME: str = 'bearer'").Groups["name"].Value);
		Assert.Equal("Basic", declaration.Match("mode = os.environ.get(\"AUTH_VALUE\", \"Basic dGVzdDp0ZXN0\")").Groups["fallbackWord"].Value);
		Assert.DoesNotMatch(declaration, "if scheme == \"Basic\":");
		Assert.DoesNotMatch(declaration, "scheme = \"Basic\" + suffix + more");
	}

	// ------------------------------------------------------------------------------------------
	// A fixture per class, and the level it ends at
	// ------------------------------------------------------------------------------------------

	[Fact]
	public void EveryClassHasAFixtureAndTheFixtureFiresAtTheLevelOfItsClass()
	{
		Assert.Equal(_everyClass.AsEnumerable(), ClassFixtures().Select(fixture => fixture.PatternClass).Order(StringComparer.Ordinal));

		foreach ((string patternClass, ConfidenceLevel level, string source) in ClassFixtures())
		{
			AssertFires(source, patternClass, level);
		}
	}

	[Fact]
	public void TheAnchorOfEveryClassFixtureIsCodeExceptTheQuotedKeyOfTheLowClass()
	{
		PythonRecognitionHarness harness = new();

		foreach ((string patternClass, _, string source) in ClassFixtures())
		{
			FileRecognitionContext context = harness.ContextFor(source);
			RecognizedConstruct construct = harness.Recognize(source).First(candidate => candidate.PatternClassIdentifier == patternClass);
			LexicalZone zone = context.Mask.ZoneAt(construct.AnchorOffset);

			Assert.True(patternClass == "PY-014" ? zone == LexicalZone.StringBody : zone == LexicalZone.Code, $"{patternClass}: anchor in {zone}");
		}
	}

	[Theory]
	[InlineData("response = requests.get(url, auth=HTTPBasicAuth(\"user\", \"secret\"))\n", "PY-001")]
	[InlineData("client = httpx.Client(auth=httpx.BasicAuth(username=\"user\", password=\"secret\"))\n", "PY-001")]
	[InlineData("session = aiohttp.ClientSession(auth=aiohttp.BasicAuth(\"user\", \"secret\"))\n", "PY-001")]
	[InlineData("auth = rq.auth.HTTPBasicAuth(user_name, secret_value)\n", "PY-001")]
	[InlineData("response = requests.post(url, auth=(\"user\", \"secret\"))\n", "PY-002")]
	[InlineData("client = httpx.AsyncClient(auth=(\"user\", \"secret\"))\n", "PY-002")]
	[InlineData("response = requests.post(\n\t\"https://example.test/items\",\n\tjson={\"name\": [1, 2]},\n\tauth=(\"user\", \"secret\"),\n)\n", "PY-002")]
	[InlineData("session.headers[\"Authorization\"] = f\"Basic {encoded}\"\n", "PY-003")]
	[InlineData("request.add_header(\"Authorization\", \"Basic %s\" % encoded)\n", "PY-003")]
	[InlineData("headers = dict(Authorization=\"Basic {}\".format(encoded))\n", "PY-003")]
	[InlineData("session = aiohttp.ClientSession(headers={\"Authorization\": encode_basic_auth(\"user\", \"secret\")})\n", "PY-003")]
	[InlineData("session.headers.update({\"authorization\": \"basic dXNlcjpzZWNyZXQ=\"})\n", "PY-003")]
	[InlineData("connection.putheader(\"Authorization\", \"Basic dXNlcjpzZWNyZXQ=\")\n", "PY-003")]
	[InlineData("opener = build_opener(HTTPBasicAuthHandler())\n", "PY-004")]
	[InlineData("http = urllib3.PoolManager(headers=make_headers(basic_auth=f\"{user_name}:{secret_value}\"))\n", "PY-005")]
	public void EachHighFormOfTheCatalogsClassTableReadsHigh(string source, string patternClass)
	{
		AssertFires(source, patternClass, ConfidenceLevel.High);
	}

	[Theory]
	[InlineData("response = session.get(\"https://example.test/items\", auth=(\"user\", \"secret\"))\n", "PY-006")]
	[InlineData("response = rq.get(url, auth=(\"user\", \"secret\"))\n", "PY-006")]
	[InlineData("auth = (\"user\", \"secret\")\n", "PY-006")]
	[InlineData("pair = b64encode(user_name + \":\" + secret_value)  # Basic header\n", "PY-007")]
	[InlineData("value = base64.encodebytes(\"%s:%s\" % (user_name, secret_value))  # sent as a Basic header\n", "PY-007")]
	[InlineData("encoded = b64encode(PAIR)  # sent as a Basic header\n", "PY-007")]
	[InlineData("proxy_headers = {\"Proxy-Authorization\": encode_basic_auth(\"user\", password=\"secret\")}\n", "PY-008")]
	[InlineData("handler_manager.add_password(None, \"https://example.test/\", \"user\", \"secret\", is_authenticated=True)\n", "PY-009")]
	[InlineData("client = httpx.Client(auth=NetRCAuth(file=\"/path/to/.netrc\"))\n", "PY-010")]
	[InlineData("async with session.get(\"https://user:secret@example.test/items\") as response:\n\tpass\n", "PY-011")]
	[InlineData("response = urllib.request.urlopen(\"https://user:secret@example.test/items\")\n", "PY-011")]
	[InlineData("client = Client(base_url=\"https://user:secret@example.test/\")\n", "PY-012")]
	public void EachMediumFormOfTheCatalogsClassTableReadsMedium(string source, string patternClass)
	{
		AssertFires(source, patternClass, ConfidenceLevel.Medium);
	}

	[Fact]
	public void TheCredentialOfAHelperTupleAndHeaderIsTheLiteralCapturedAndSignalsASecret()
	{
		PythonRecognitionHarness harness = new();

		Assert.True(Single(harness.Recognize("auth = HTTPBasicAuth(\"user\", \"secret\")\n"), "PY-001").LiteralCredentialSignal);
		Assert.True(Single(harness.Recognize("requests.get(url, auth=(\"user\", \"secret\"))\n"), "PY-002").LiteralCredentialSignal);
		Assert.True(Single(harness.Recognize("session.headers.update({\"authorization\": \"basic dXNlcjpzZWNyZXQ=\"})\n"), "PY-003").LiteralCredentialSignal);
		Assert.False(Single(harness.Recognize("auth = HTTPBasicAuth(user_name, secret_value)\n"), "PY-001").LiteralCredentialSignal);
		Assert.False(Single(harness.Recognize("auth = HTTPBasicAuth(\"user\", f\"{secret_value}\")\n"), "PY-001").LiteralCredentialSignal);
		Assert.False(Single(harness.Recognize("auth = HTTPBasicAuth(\"user\", \"<password>\")\n"), "PY-001").LiteralCredentialSignal);
	}

	// ------------------------------------------------------------------------------------------
	// The held header value: the symbol table decides
	// ------------------------------------------------------------------------------------------

	[Fact]
	public void AHeaderValueHeldInANameThatResolvesToBasicIsReadAtMediumAndOneThatResolvesToBearerIsSilent()
	{
		const string call = "response = requests.get(url, headers={\"Authorization\": scheme_value})\n";

		AssertFires("scheme_value = \"Basic\"\n" + call, "PY-013", ConfidenceLevel.Medium);
		AssertFires("scheme_value = \"Basic \"\n" + call, "PY-013", ConfidenceLevel.Medium);
		AssertSilent("scheme_value = \"Bearer dG9rZW4=\"\n" + call);
		AssertSilent(call);
		AssertSilent("def build(scheme_value):\n\treturn requests.get(url, headers={\"Authorization\": scheme_value})\n");
	}

	// ------------------------------------------------------------------------------------------
	// The near-miss families: kept out, or Low
	// ------------------------------------------------------------------------------------------

	[Theory]
	[InlineData("credentials = request.authorization\n")]
	[InlineData("value = request.headers[\"Authorization\"]\n")]
	[InlineData("value = request.META[\"HTTP_AUTHORIZATION\"]\n")]
	[InlineData("credentials = BasicAuth.decode(header_value)\n")]
	[InlineData("credentials = aiohttp.BasicAuth.from_url(url)\n")]
	[InlineData("security = HTTPBasic()\n")]
	[InlineData("decoded = base64.b64decode(header_value.split()[1])\n")]
	[InlineData("response[\"WWW-Authenticate\"] = 'Basic realm=\"example\"'\n")]
	[InlineData("raise HTTPException(401, headers={\"WWW-Authenticate\": \"Basic\"})\n")]
	[InlineData("SAMPLE_RECEIVED = \"Basic dXNlcjpzZWNyZXQ=\"\n")]
	[InlineData("credentials = parse_basic(\"Basic dXNlcjpzZWNyZXQ=\")\n")]
	[InlineData("headers = {\"Authorization\": f\"Bearer {token}\"}\n")]
	[InlineData("headers = {\"X-API-Key\": api_key}\n")]
	[InlineData("response = requests.get(url, headers={API_KEY_HEADER: api_key})\n")]
	[InlineData("response = requests.get(url, auth=BearerAuth(token))\n")]
	[InlineData("session.headers[\"Authorization\"] = \"Token \" + key\n")]
	[InlineData("scheme_value = \"Bearer dG9rZW4=\"\nresponse = requests.get(url, headers={\"Authorization\": scheme_value})\n")]
	[InlineData("response = requests.get(url, auth=HTTPDigestAuth(\"user\", \"secret\"))\n")]
	[InlineData("client = httpx.Client(auth=httpx.DigestAuth(\"user\", \"secret\"))\n")]
	[InlineData("middleware = aiohttp.DigestAuthMiddleware(login=\"user\", password=\"secret\")\n")]
	[InlineData("handler = urllib.request.HTTPDigestAuthHandler(manager)\n")]
	[InlineData("server = smtplib.SMTP(\"mail.example.test\")\nserver.login(\"user\", \"secret\")\n")]
	[InlineData("connection = ftplib.FTP(\"example.test\", \"user\", \"secret\")\n")]
	[InlineData("connect(host=\"db.example.test\", user=\"user\", password=\"secret\")\n")]
	[InlineData("response = requests.post(token_url, data={\"grant_type\": \"client_credentials\", \"client_id\": client_id, \"client_secret\": secret})\n")]
	[InlineData("response = requests.post(token_url, auth=(\"client_id\", \"client_value\"), data={\"grant_type\": \"client_credentials\"})\n")]
	[InlineData("response = requests.post(token_url, data={\"grant_type\": \"client_credentials\"}, auth=(\"client_id\", \"client_value\"))\n")]
	[InlineData("response = rq.post(token_url, auth=(\"client_id\", \"client_value\"), data={\"grant_type\": \"client_credentials\"})\n")]
	[InlineData("response = httpx.post(token_url, data=payload)\n")]
	[InlineData("# Basic auth is deprecated\n")]
	[InlineData("note = \"use HTTPBasicAuth later\"\n")]
	[InlineData("SCHEMES = {\"basic\": \"Basic (username and password)\"}\n")]
	[InlineData("value = base64.b64encode(json_payload)\n")]
	[InlineData("value = base64.b64encode(f\"{host_name}:{port_number}\".encode())\n")]
	[InlineData("value = base64.urlsafe_b64encode(cache_key)\n")]
	[InlineData("value = base64.b32encode(identifier)\n")]
	[InlineData("signature = base64.b64encode(digest)  # Authorization header of a signed request\n")]
	[InlineData("encoded = base64.b64encode(PAIR)\n")]
	[InlineData("response = requests.get(url, auth=HTTPProxyAuth(\"user\", \"secret\"))\n")]
	[InlineData("proxy_headers = {\"Proxy-Authorization\": \"Basic \" + token}\n")]
	[InlineData("headers = urllib3.util.make_headers(proxy_basic_auth=\"user:secret\")\n")]
	[InlineData("handler = urllib.request.ProxyBasicAuthHandler(manager)\n")]
	[InlineData("response = requests.get(url, proxies={\"https\": \"http://user:secret@proxy.example.test:3128\"})\n")]
	[InlineData("response = requests.get(url, proxy=\"http://user:secret@proxy.example.test:3128\")\n")]
	[InlineData("proxy_url = \"http://user:secret@proxy.example.test:3128\"\n")]
	[InlineData("async with session.get(url, proxy_auth=aiohttp.BasicAuth(\"user\", \"secret\")) as response:\n\tpass\n")]
	[InlineData("database = connect(\"scheme-one://user:secret@db.example.test/data\")\n")]
	[InlineData("broker_url = \"scheme-two://user:secret@broker.example.test\"\n")]
	[InlineData("response = requests.get(\"ftp://user:secret@example.test/file\")\n")]
	public void ANearMissIsKeptOutOrRecognizedBelowHighAndMedium(string source)
	{
		ImmutableArray<RecognizedConstruct> constructs = new PythonRecognitionHarness().Recognize(source);

		Assert.True(constructs.All(construct => construct.Level is ConfidenceLevel.Low), $"[{source}] produced [{Describe(constructs)}]");
	}

	[Theory]
	[InlineData("credentials = request.authorization\n")]
	[InlineData("credentials = BasicAuth.decode(header_value)\n")]
	[InlineData("headers = {\"Authorization\": f\"Bearer {token}\"}\n")]
	[InlineData("response = requests.get(url, auth=HTTPDigestAuth(\"user\", \"secret\"))\n")]
	[InlineData("response = requests.post(token_url, auth=(\"client_id\", \"client_value\"), data={\"grant_type\": \"client_credentials\"})\n")]
	[InlineData("headers = {\"Proxy-Authorization\": \"Basic \" + token}\n")]
	[InlineData("headers = urllib3.util.make_headers(proxy_basic_auth=\"user:secret\")\n")]
	[InlineData("value = base64.urlsafe_b64encode(cache_key)\n")]
	[InlineData("database = connect(\"scheme-one://user:secret@db.example.test/data\")\n")]
	[InlineData("auth = HTTPBasicAuth()\n")]
	[InlineData("if isinstance(auth, HTTPBasicAuth):\n\tpass\n")]
	[InlineData("class CustomAuth(HTTPBasicAuth):\n\tpass\n")]
	[InlineData("response = requests.get(url, auth=creds)\n")]
	[InlineData("response = requests.get(url, auth=[\"user\", \"secret\"])\n")]
	[InlineData("response = requests.get(url, auth=(\"user\", \"secret\", \"extra\"))\n")]
	[InlineData("headers = {\"Authorization\": t\"Basic {encoded}\"}\n")]
	public void ANearMissThatTheCatalogKeepsOutProducesNoConstructAtAll(string source)
	{
		AssertSilent(source);
	}

	[Fact]
	public void AnAuthTupleGivenToANonHttpClientIsReadAtMediumAsTheDeclaredCostSays()
	{
		AssertFires("connection = connect(host=\"db.example.test\", auth=(\"user\", \"secret\"))\n", "PY-006", ConfidenceLevel.Medium);
	}

	[Fact]
	public void ABasicConstructBuiltAndNeverSentIsReadAsLiveAsTheDeclaredCostSays()
	{
		AssertFires("if False:\n\trequests.get(url, auth=(\"user\", \"secret\"))\n", "PY-002", ConfidenceLevel.High);
	}

	// ------------------------------------------------------------------------------------------
	// A docstring is a string literal: Low by the existing reason
	// ------------------------------------------------------------------------------------------

	[Fact]
	public void ADocstringThatSpellsACredentialFormReadsLowByTheCommentOrStringLiteralReasonAndTheSameCallInCodeReadsHigh()
	{
		const string docstring = "def fetch_items(url):\n\t\"\"\"Fetch the items.\n\n\tThe legacy client sent the request as requests.get(url, auth=(\"user\", \"secret\")).\n\t\"\"\"\n\treturn None\n";
		ImmutableArray<RecognizedConstruct> inDocstring = new PythonRecognitionHarness().Recognize(docstring);

		Assert.NotEmpty(inDocstring);
		Assert.All(inDocstring, construct =>
		{
			Assert.Equal(ConfidenceLevel.Low, construct.Level);
			Assert.True(construct.LowReasons.HasFlag(LowConfidenceReasons.CommentOrStringLiteral), Describe(inDocstring));
		});
		AssertFires("def fetch_items(url):\n\treturn requests.get(url, auth=(\"user\", \"secret\"))\n", "PY-002", ConfidenceLevel.High);
		AssertFires("value = f\"{requests.get(url, auth=(\"user\", \"secret\"))}\"\n", "PY-002", ConfidenceLevel.High);
	}

	[Fact]
	public void AModuleDocstringAndAnInlineCommentThatSpellACredentialFormReadLow()
	{
		const string moduleDocstring = "\"\"\"Module notes: headers = {\"Authorization\": \"Basic dXNlcjpzZWNyZXQ=\"} was the old form.\"\"\"\nvalue = 1\n";
		ImmutableArray<RecognizedConstruct> constructs = new PythonRecognitionHarness().Recognize(moduleDocstring + "# requests.get(url, auth=(\"user\", \"secret\"))\n");

		Assert.NotEmpty(constructs);
		Assert.All(constructs, construct => Assert.Equal(ConfidenceLevel.Low, construct.Level));
	}

	// ------------------------------------------------------------------------------------------
	// One finding per construct: the subsumption triples
	// ------------------------------------------------------------------------------------------

	[Fact]
	public void ANamedCallIsOneFindingAtHighAndTheUnnamedReceiversAreReadAtMediumByTheUnnamedClassAlone()
	{
		const string source = "import requests\nrequests.get(\"https://example.test/a\", auth=(\"user\", \"secret\"))\nsession = make_session()\nsession.get(\"https://example.test/b\", auth=(\"user\", \"secret\"))\nsession.auth = (\"user\", \"secret\")\n";
		ImmutableArray<RecognizedConstruct> constructs = new PythonRecognitionHarness().Recognize(source);

		Assert.Single(constructs, construct => construct.PatternClassIdentifier == "PY-002" && construct.Level == ConfidenceLevel.High);
		Assert.Equal(2, constructs.Count(construct => construct.PatternClassIdentifier == "PY-006" && construct.Level == ConfidenceLevel.Medium));
		Assert.Equal(3, constructs.Length);
	}

	[Fact]
	public void WithoutTheTripleANamedCallIsEmittedTwiceAtOneAuthAnchorWhichIsWhatTheTripleRemoves()
	{
		const string source = "response = requests.get(\"https://example.test/a\", auth=(\"user\", \"secret\"))\n";
		ImmutableArray<RecognizedConstruct> withTheTriple = new PythonRecognitionHarness().Recognize(source);
		ImmutableArray<RecognizedConstruct> withoutTheTriple = RecognizeWithoutSubsumption(source);

		Assert.Single(withTheTriple);
		Assert.Equal(2, withoutTheTriple.Length);
		Assert.Equal(withoutTheTriple[0].AnchorOffset, withoutTheTriple[1].AnchorOffset);
		Assert.Equal(["PY-002", "PY-006"], withoutTheTriple.Select(construct => construct.PatternClassIdentifier).Order(StringComparer.Ordinal));
	}

	[Theory]
	[InlineData("response = requests.get(url, headers={\"Authorization\": encode_basic_auth(\"user\", \"secret\")})\n", "PY-003", "PY-008")]
	[InlineData("response = requests.get(url, headers={\"Authorization\": \"Basic dXNlcjpzZWNyZXQ=\"})\n", "PY-003", "PY-014")]
	[InlineData("response = requests.get(\"https://user:secret@example.test/items\")\n", "PY-011", "PY-012")]
	public void ASubsumedClassIsNotEmittedBesideItsSubsumingClassOverTheSameSpan(string source, string subsuming, string subsumed)
	{
		ImmutableArray<RecognizedConstruct> constructs = new PythonRecognitionHarness().Recognize(source);

		Assert.Contains(constructs, construct => construct.PatternClassIdentifier == subsuming);
		Assert.DoesNotContain(constructs, construct => construct.PatternClassIdentifier == subsumed);
	}

	[Fact]
	public void AHandlerConstructionAndTheCredentialGivenToItsPasswordManagerAreTwoFindingsAtTwoAnchors()
	{
		const string source = "manager = urllib.request.HTTPPasswordMgrWithDefaultRealm()\nmanager.add_password(None, \"https://example.test/\", \"user\", \"secret\")\nhandler = urllib.request.HTTPBasicAuthHandler(manager)\n";
		ImmutableArray<RecognizedConstruct> constructs = new PythonRecognitionHarness().Recognize(source);

		Assert.Equal(["PY-004", "PY-009"], constructs.Select(construct => construct.PatternClassIdentifier).Order(StringComparer.Ordinal));
	}

	// ------------------------------------------------------------------------------------------
	// Fixtures
	// ------------------------------------------------------------------------------------------

	/// <summary>One fixture per class: its class, the level it must read at, and an invented source that fires it.</summary>
	internal static ImmutableArray<(string PatternClass, ConfidenceLevel Level, string Source)> ClassFixtures()
	{
		return
		[
			("PY-001", ConfidenceLevel.High, "response = requests.get(\"https://example.test/items\", auth=HTTPBasicAuth(\"user\", \"secret\"))\n"),
			("PY-002", ConfidenceLevel.High, "response = requests.get(\"https://example.test/items\", auth=(\"user\", \"secret\"))\n"),
			("PY-003", ConfidenceLevel.High, "response = requests.get(url, headers={\"Authorization\": \"Basic \" + encoded})\n"),
			("PY-004", ConfidenceLevel.High, "handler = urllib.request.HTTPBasicAuthHandler(password_manager)\n"),
			("PY-005", ConfidenceLevel.High, "headers = urllib3.util.make_headers(basic_auth=\"user:secret\")\n"),
			("PY-006", ConfidenceLevel.Medium, "session.auth = (\"user\", \"secret\")\n"),
			("PY-007", ConfidenceLevel.Medium, "token = base64.b64encode(f\"{user_name}:{secret_value}\".encode()).decode()\nheaders = {\"Accept\": \"text/plain\", \"Authorization\": \"Basic \" + token}\n"),
			("PY-008", ConfidenceLevel.Medium, "value = aiohttp.encode_basic_auth(\"user\", \"secret\")\n"),
			("PY-009", ConfidenceLevel.Medium, "manager.add_password(None, \"https://example.test/\", \"user\", \"secret\")\n"),
			("PY-010", ConfidenceLevel.Medium, "client = httpx.Client(auth=httpx.NetRCAuth())\n"),
			("PY-011", ConfidenceLevel.Medium, "response = requests.get(\"https://user:secret@example.test/items\")\n"),
			("PY-012", ConfidenceLevel.Medium, "BASE_URL = \"https://user:secret@example.test/items\"\n"),
			("PY-013", ConfidenceLevel.Medium, "scheme_value = \"Basic\"\nresponse = requests.get(url, headers={\"Authorization\": scheme_value})\n"),
			("PY-014", ConfidenceLevel.Low, "def build_headers():\n\treturn {\"Authorization\": \"Basic dXNlcjpzZWNyZXQ=\"}\n")
		];
	}

	// ------------------------------------------------------------------------------------------
	// Helpers
	// ------------------------------------------------------------------------------------------

	private static ImmutableArray<string> ClassesAt(ILanguageAnalyzer analyzer, ConfidenceLevel level)
	{
		return [.. analyzer.DetectionRulesByPatternClass.Where(entry => entry.Value.BaseLevel == level).Select(entry => entry.Key).Order(StringComparer.Ordinal)];
	}

	private static RecognizedConstruct Single(ImmutableArray<RecognizedConstruct> constructs, string patternClass)
	{
		return Assert.Single(constructs, construct => construct.PatternClassIdentifier == patternClass);
	}

	private static void AssertFires(string source, string patternClass, ConfidenceLevel level)
	{
		ImmutableArray<RecognizedConstruct> constructs = new PythonRecognitionHarness().Recognize(source);

		Assert.True(
			constructs.Any(construct => construct.PatternClassIdentifier == patternClass && construct.Level == level),
			$"{patternClass} did not fire at {level} on [{source}]; got [{Describe(constructs)}]");
	}

	private static void AssertSilent(string source)
	{
		ImmutableArray<RecognizedConstruct> constructs = new PythonRecognitionHarness().Recognize(source);

		Assert.True(constructs.IsEmpty, $"[{source}] produced [{Describe(constructs)}]");
	}

	private static string Describe(ImmutableArray<RecognizedConstruct> constructs)
	{
		return string.Join(", ", constructs.Select(construct => $"{construct.PatternClassIdentifier}@{construct.Level}"));
	}

	/// <summary>The shipped analyzer with the subsumption relation emptied: the negative control that shows the triple is what removes the duplicate.</summary>
	private static ImmutableArray<RecognizedConstruct> RecognizeWithoutSubsumption(string source)
	{
		PythonRecognitionHarness harness = new();
		FileRecognitionContext context = harness.ContextFor(source);
		FileRecognitionContext withoutTheTriple = new()
		{
			Analyzer = new AnalyzerWithoutSubsumption(PythonShippedAnalyzer.Instance),
			NormalizedText = context.NormalizedText,
			Mask = context.Mask,
			Path = context.Path
		};

		return new RecognitionEngine(harness.EmissionPoint, new FrozenTimeProvider()).Recognize(withoutTheTriple).Constructs;
	}

	private sealed class AnalyzerWithoutSubsumption : ILanguageAnalyzer
	{
		private readonly ILanguageAnalyzer _inner;

		internal AnalyzerWithoutSubsumption(ILanguageAnalyzer inner)
		{
			_inner = inner;
		}

		public LexicalProfile LexicalProfile => _inner.LexicalProfile;

		public LanguageIdentification LanguageIdentification => _inner.LanguageIdentification;

		public FrozenDictionary<string, DetectionRule> DetectionRulesByPatternClass => _inner.DetectionRulesByPatternClass;

		public LanguageShapedDemotionSignals DemotionSignals => _inner.DemotionSignals;

		public CandidateSelectionAndRecommendation CandidateSelectionAndRecommendation => _inner.CandidateSelectionAndRecommendation;

		public FrozenSet<SubsumptionRule> SubsumptionRelation => FrozenSet<SubsumptionRule>.Empty;
	}
}
