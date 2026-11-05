using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Text.RegularExpressions;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Findings;
using MigrateAuth.Cli.Lexical;
using MigrateAuth.Cli.Recognition;
using MigrateAuth.Cli.Scanning;
using MigrateAuth.Cli.Tests.Recognition;
using MigrateAuth.Cli.Tests.Scanning;

using Xunit;

namespace MigrateAuth.Cli.Tests.Catalog;

/// <summary>
/// The `JavaScript` analyzer against its catalog, through the analyzer the binary ships and the real recognition engine: never through a hand-built construct, because what is
/// under test is whether the expressions the analyzer declares recognize the forms the catalog states and decline the exclusions it states.
///
/// Every script below is INVENTED from the catalog's own prose, with placeholder names throughout, and carries no fragment, path or third-party name from any repository.
/// </summary>
public sealed class JavaScriptAnalyzerTests
{
	private const string _CLASS_PREFIX = "JS-";
	private const int _CLASS_COUNT = 17;
	private const int _CLASSES_THAT_CAN_REACH_HIGH_OR_MEDIUM = 12;
	private const int _LOW_BY_DEFINITION_CLASS_COUNT = 5;
	private const int _ANCHOR_COUNT = 9;
	private const int _SUBSUMPTION_COUNT = 7;
	private const string _CONSTRUCT_GROUP = "construct";
	private const string _CREDENTIAL_GROUP = "credential";
	private const string _SCHEME_GROUP = "scheme";
	private const string _EXPECTED_SCHEME_TOKEN_BASIC = "Basic";
	private const int _EXPECTED_CATALOG_MAJOR = 1;
	private const int _EXPECTED_CATALOG_MINOR = 35;
	private const int _EXPECTED_CATALOG_PATCH = 0;
	private const int _FLOW_SIGNAL_COUNT = 16;

	private static readonly ImmutableArray<string> _everyClass =
		[.. Enumerable.Range(1, _CLASS_COUNT).Select(number => string.Create(System.Globalization.CultureInfo.InvariantCulture, $"{_CLASS_PREFIX}{number:D3}"))];

	private static readonly ImmutableArray<string> _lowClasses = ["JS-007", "JS-009", "JS-011", "JS-014", "JS-016"];

	private static readonly ImmutableArray<string> _highClasses = ["JS-001", "JS-002", "JS-003", "JS-004", "JS-008", "JS-015"];

	private static readonly ImmutableArray<string> _mediumClasses = ["JS-005", "JS-006", "JS-010", "JS-012", "JS-013", "JS-017"];

	/// <summary>The classes that declare a literal credential position.</summary>
	private static readonly ImmutableArray<string> _credentialClasses =
	[
		"JS-001", "JS-002", "JS-003", "JS-004", "JS-005", "JS-006", "JS-008", "JS-009", "JS-010", "JS-011", "JS-013", "JS-014", "JS-015"
	];

	// ------------------------------------------------------------------------------------------
	// The contract
	// ------------------------------------------------------------------------------------------

	[Fact]
	public void TheJavaScriptAnalyzerFitsTheContractWithNoViolations()
	{
		Assert.Empty(AnalyzerContractValidator.Validate(ReadAnalyzer()));
	}

	[Fact]
	public void SeventeenClassesAreDeclaredAndTwelveCanReachHighOrMedium()
	{
		ILanguageAnalyzer analyzer = ReadAnalyzer();

		Assert.Equal(_CLASS_COUNT, analyzer.DetectionRulesByPatternClass.Count);
		Assert.Equal(_everyClass.Order(StringComparer.Ordinal), analyzer.DetectionRulesByPatternClass.Keys.Order(StringComparer.Ordinal));
		Assert.Equal(_lowClasses.AsEnumerable(), ClassesAt(analyzer, ConfidenceLevel.Low).AsEnumerable());
		Assert.Equal(_highClasses.AsEnumerable(), ClassesAt(analyzer, ConfidenceLevel.High).AsEnumerable());
		Assert.Equal(_mediumClasses.AsEnumerable(), ClassesAt(analyzer, ConfidenceLevel.Medium).AsEnumerable());
		Assert.Equal(_CLASSES_THAT_CAN_REACH_HIGH_OR_MEDIUM, _highClasses.Length + _mediumClasses.Length);
		Assert.Equal(_LOW_BY_DEFINITION_CLASS_COUNT, _lowClasses.Length);
	}

	[Fact]
	public void TheHighClassesDeclareTheirSchemeAndCarrierAndTheOthersNeedTheCallPath()
	{
		Assert.All(ReadAnalyzer().DetectionRulesByPatternClass, entry =>
			Assert.Equal(
				_highClasses.Contains(entry.Key) ? SelfSufficiency.DeclaresSchemeAndCredentialCarrier : SelfSufficiency.RequiresCallPathToBeEstablished,
				entry.Value.SelfSufficiency));
	}

	[Fact]
	public void EveryExpressionIsCaseSensitiveCarriesTheInvariantOptionsAndThePolicyTimeout()
	{
		const RegexOptions required = RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture;

		Assert.All(ReadAnalyzer().DetectionRulesByPatternClass, entry =>
		{
			Assert.Equal(required, entry.Value.Expression.Options & required);
			Assert.Equal(RegexOptions.None, entry.Value.Expression.Options & RegexOptions.IgnoreCase);
			Assert.Equal(RecognitionEnginePolicy.MATCH_BOUND, entry.Value.Expression.MatchTimeout);
		});
	}

	[Fact]
	public void EveryRuleAnchorsOnTheConstructGroupAndOnlyTheHeldHeaderClassDeclaresASchemeGroup()
	{
		Assert.All(ReadAnalyzer().DetectionRulesByPatternClass, entry =>
		{
			Assert.Equal(_CONSTRUCT_GROUP, entry.Value.AnchorPosition.CaptureGroupName);
			Assert.Contains(_CONSTRUCT_GROUP, entry.Value.Expression.GetGroupNames());
			Assert.Equal(DestinationCertainty.Unknown, entry.Value.DestinationCertainty);
			Assert.Equal(entry.Key == "JS-017", entry.Value.SymbolicVerdictInterpretation.IsSubjectToSymbolicResolution);
		});
	}

	[Fact]
	public void TheHeldHeaderClassConfirmsOnBasicAndSuppressesOnAnotherSchemeAndOnAnUnresolvedName()
	{
		SymbolicVerdictInterpretation verdict = ReadAnalyzer().DetectionRulesByPatternClass["JS-017"].SymbolicVerdictInterpretation;

		Assert.Equal(_SCHEME_GROUP, verdict.SchemeArgumentCaptureGroupName);
		Assert.Contains(_SCHEME_GROUP, ReadAnalyzer().DetectionRulesByPatternClass["JS-017"].Expression.GetGroupNames());
		Assert.Equal(_EXPECTED_SCHEME_TOKEN_BASIC, verdict.ExpectedSchemeToken);
		Assert.Equal(SymbolicVerdictResponse.ConfirmsPatternClass, verdict.WhenResolvedMatching);
		Assert.Equal(SymbolicVerdictResponse.SuppressesFinding, verdict.WhenResolvedNotMatching);
		Assert.Equal(SymbolicVerdictResponse.SuppressesFinding, verdict.WhenIndeterminate);
	}

	[Fact]
	public void TheCredentialPositionsAreTheOnesTheCatalogDeclares()
	{
		Assert.All(ReadAnalyzer().DetectionRulesByPatternClass, entry =>
		{
			CredentialPosition position = entry.Value.CredentialPosition;

			if (_credentialClasses.Contains(entry.Key))
			{
				Assert.Equal(_CREDENTIAL_GROUP, position.CaptureGroupName);
				Assert.Equal(entry.Key == "JS-001", position.ReadsABareWordAsLiteral);
			}
			else
			{
				Assert.False(position.IsDeclared, entry.Key);
			}
		});
	}

	[Fact]
	public void TheSubsumptionRelationIsTheSevenTriplesOfTheCatalog()
	{
		(string Subsuming, string Subsumed, SubsumptionCondition When)[] expected =
		[
			("JS-004", "JS-005", SubsumptionCondition.MatchSpansOverlap),
			("JS-004", "JS-006", SubsumptionCondition.MatchSpansOverlap),
			("JS-002", "JS-005", SubsumptionCondition.MatchSpansOverlap),
			("JS-002", "JS-006", SubsumptionCondition.MatchSpansOverlap),
			("JS-001", "JS-011", SubsumptionCondition.AnchorPositionsAreIdentical),
			("JS-016", "JS-011", SubsumptionCondition.AnchorPositionsAreIdentical),
			("JS-003", "JS-013", SubsumptionCondition.MatchSpansOverlap)
		];
		FrozenSet<SubsumptionRule> relation = ReadAnalyzer().SubsumptionRelation;

		Assert.Equal(_SUBSUMPTION_COUNT, relation.Count);
		Assert.Equal(
			expected.Order().ToArray(),
			relation.Select(rule => (rule.SubsumingPatternClassIdentifier, rule.SubsumedPatternClassIdentifier, rule.When)).Order().ToArray());
	}

	[Fact]
	public void NoConstructPrecedenceWrapperTrackingOrVariableUseIsDeclared()
	{
		ILanguageAnalyzer analyzer = ReadAnalyzer();

		Assert.Null(analyzer.ConstructPrecedence);
		Assert.Null(analyzer.CandidateSelectionAndRecommendation.WrapperTracking);
		Assert.All(analyzer.DetectionRulesByPatternClass.Values, rule => Assert.Null(rule.VariableUse));
	}

	[Fact]
	public void TheNineAnchorsAreInsensitiveAndTwoOfThemHoldAPunctuationMark()
	{
		FrozenSet<PreselectionAnchor> anchors = ReadAnalyzer().CandidateSelectionAndRecommendation.PreselectionAnchors;

		Assert.Equal(_ANCHOR_COUNT, anchors.Count);
		Assert.All(anchors, anchor => Assert.Equal(CaseSensitivity.Insensitive, anchor.Sensitivity));
		Assert.Equal(
			new[] { "://", "Buffer(", "Buffer.from(", "XMLHttpRequest", "auth", "btoa", "password", "username", "xhr" }.Order(StringComparer.Ordinal),
			anchors.Select(anchor => anchor.Text).Order(StringComparer.Ordinal));
		Assert.DoesNotContain(anchors, anchor => anchor.Text.Contains("Basic", StringComparison.OrdinalIgnoreCase));
	}

	[Fact]
	public void TheFlowSignalsTheTemplatesAndTheDialectsAreDeclared()
	{
		CandidateSelectionAndRecommendation element = ReadAnalyzer().CandidateSelectionAndRecommendation;

		Assert.Equal(_FLOW_SIGNAL_COUNT, element.FlowSignals.Length);
		Assert.NotNull(element.FileDialects);
		Assert.NotNull(element.LanguageFallbackTextByFlow);
		Assert.All(_everyClass.Except(_lowClasses), patternClass => Assert.NotNull(element.TemplateProvider(patternClass, CredentialFlow.ClientCredentials)));
		Assert.All(_lowClasses, patternClass => Assert.Null(element.TemplateProvider(patternClass, CredentialFlow.ClientCredentials)));
	}

	[Fact]
	public void TheCatalogVersionIsOneThirtyFour()
	{
		Assert.Equal(new CatalogVersion(_EXPECTED_CATALOG_MAJOR, _EXPECTED_CATALOG_MINOR, _EXPECTED_CATALOG_PATCH), LanguageCatalog.CurrentVersion);
	}

	// ------------------------------------------------------------------------------------------
	// Identification: extensions, and the extensionless node script
	// ------------------------------------------------------------------------------------------

	[Theory]
	[InlineData(".js")]
	[InlineData(".mjs")]
	[InlineData(".cjs")]
	[InlineData(".jsx")]
	[InlineData(".JS")]
	[InlineData(".Jsx")]
	public void TheFourJavaScriptExtensionsAreAssessedWithoutCase(string extension)
	{
		LanguageCatalog catalog = LanguageCatalogRegistry.BuildEmbeddedCatalog();

		Assert.True(catalog.TryGetAnalyzerForFileExtension(extension, out ILanguageAnalyzer? analyzer));
		Assert.Contains(JavaScriptRegisteredExpressions.LANGUAGE_NAME, (ISet<string>)analyzer!.LanguageIdentification.DeclaredLanguageNames);
		Assert.Equal(JavaScriptRegisteredExpressions.LANGUAGE_NAME, analyzer.LanguageIdentification.LanguageNameDerivation(extension, null));
	}

	[Theory]
	[InlineData(".ts")]
	[InlineData(".tsx")]
	[InlineData(".mts")]
	[InlineData(".cts")]
	[InlineData(".json")]
	[InlineData(".html")]
	[InlineData(".vue")]
	[InlineData("")]
	public void TypeScriptAndEveryOtherExtensionAreNotAssessed(string extension)
	{
		LanguageCatalog catalog = LanguageCatalogRegistry.BuildEmbeddedCatalog();

		Assert.False(catalog.TryGetAnalyzerForFileExtension(extension, out _));
	}

	[Theory]
	[InlineData("#!/usr/bin/env node")]
	[InlineData("#!/usr/bin/node")]
	[InlineData("#!/usr/bin/env -S node --no-warnings")]
	[InlineData("#!/usr/local/bin/nodejs")]
	public void TheFirstLineRuleAttributesAnExtensionlessNodeScript(string firstLine)
	{
		Regex rule = ReadAnalyzer().LanguageIdentification.FirstLineDetectionExpression!;

		Assert.Equal(LexicalEnginePolicy.LEXICAL_BUDGET, rule.MatchTimeout);
		Assert.Matches(rule, firstLine);
	}

	[Theory]
	[InlineData("#!/bin/bash")]
	[InlineData("#!/usr/bin/env nodemon")]
	[InlineData("#!/usr/bin/env python3")]
	[InlineData("// #!/usr/bin/env node")]
	[InlineData("/usr/bin/env node")]
	public void TheFirstLineRuleDoesNotAttributeWhatTheBaseTableDoesNotReadAsNode(string firstLine)
	{
		Assert.DoesNotMatch(ReadAnalyzer().LanguageIdentification.FirstLineDetectionExpression!, firstLine);
	}

	[Theory]
	[InlineData("run-client.js")]
	[InlineData("run-client.mjs")]
	[InlineData("run-client.cjs")]
	[InlineData("view.jsx")]
	public void AFileWithAJavaScriptExtensionIsAttributedToJavaScriptAndAssessed(string relativePath)
	{
		using ScanHarness harness = ScanHarness.CreateEmpty();

		AttributedLanguage attributed = new LanguageAttribution(harness.Catalog).Attribute(harness.PathOf(relativePath), firstLine: null);

		Assert.Equal(JavaScriptRegisteredExpressions.LANGUAGE_NAME, attributed.LanguageName);
		Assert.True(attributed.IsRecognized);
		Assert.NotNull(attributed.Analyzer);
	}

	[Theory]
	[InlineData("client.ts")]
	[InlineData("view.tsx")]
	[InlineData("client.mts")]
	[InlineData("client.cts")]
	public void ATypeScriptFileIsNamedTypeScriptAndIsNotAssessedEvenWithANodeFirstLine(string relativePath)
	{
		using ScanHarness harness = ScanHarness.CreateEmpty();

		AttributedLanguage attributed = new LanguageAttribution(harness.Catalog).Attribute(harness.PathOf(relativePath), "#!/usr/bin/env node");

		Assert.Equal(AssessedLanguages.CanonicalName(AssessedLanguage.TypeScript), attributed.LanguageName);
		Assert.False(attributed.IsRecognized);
		Assert.Null(attributed.Analyzer);
	}

	[Theory]
	[InlineData("#!/usr/bin/env node")]
	[InlineData("#!/usr/bin/node")]
	[InlineData("#!/usr/bin/env -S node --no-warnings")]
	[InlineData("#!/usr/local/bin/nodejs")]
	public void AnExtensionlessNodeScriptIsAttributedToJavaScriptByItsFirstLineAndAssessed(string firstLine)
	{
		using ScanHarness harness = ScanHarness.CreateEmpty();

		AttributedLanguage attributed = new LanguageAttribution(harness.Catalog).Attribute(harness.PathOf("run-client"), firstLine);

		Assert.Equal(JavaScriptRegisteredExpressions.LANGUAGE_NAME, attributed.LanguageName);
		Assert.True(attributed.IsRecognized);
		Assert.NotNull(attributed.Analyzer);
		Assert.False(attributed.FirstLineBudgetExceeded);
	}

	[Fact]
	public void AnExtensionlessScriptWhoseFirstLineNamesAnotherInterpreterIsNotAttributedToJavaScript()
	{
		using ScanHarness harness = ScanHarness.CreateEmpty();

		AttributedLanguage attributed = new LanguageAttribution(harness.Catalog).Attribute(harness.PathOf("run-client"), "#!/usr/bin/env python3");

		Assert.NotEqual(JavaScriptRegisteredExpressions.LANGUAGE_NAME, attributed.LanguageName);
	}

	[Fact]
	public void AnExtensionlessNodeScriptsConstructIsReadByTheEngine()
	{
		const string source = "#!/usr/bin/env node\nconst response = await got(url, { username: userName, password: secret });\n";

		AssertFires(source, "JS-015", ConfidenceLevel.High, "tools/run-client");
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
	public void TheAnchorOfEveryClassFixtureIsCode()
	{
		JavaScriptRecognitionHarness harness = new();

		foreach ((string patternClass, _, string source) in ClassFixtures())
		{
			FileRecognitionContext context = harness.ContextFor(source);
			RecognizedConstruct construct = harness.Recognize(source).First(candidate => candidate.PatternClassIdentifier == patternClass);

			Assert.Equal(LexicalZone.Code, context.Mask.ZoneAt(construct.AnchorOffset));
		}
	}

	[Theory]
	[InlineData(".js")]
	[InlineData(".mjs")]
	[InlineData(".cjs")]
	[InlineData(".jsx")]
	public void AHeaderConstructIsFoundInEveryJavaScriptExtension(string extension)
	{
		AssertFires("await fetch(url, { headers: { Authorization: `Basic ${encoded}` } });\n", "JS-004", ConfidenceLevel.High, $"src/client{extension}");
	}

	[Fact]
	public void AJsxFileReadsAConstructAfterAClosingTagAndAfterASelfClosingTag()
	{
		const string source =
			"const el = <a href={u}>go</a>;\nconst el2 = <Img src={u} />;\nawait fetch(url, { headers: { Authorization: `Basic ${encoded}` } });\n";

		AssertFires(source, "JS-004", ConfidenceLevel.High, "src/view.jsx");
	}

	[Fact]
	public void AJsxFileReadsAConstructAfterAnApostropheInTextBecauseTheLineEndRecoversIt()
	{
		const string source = "const el = <p>It's text</p>;\nawait fetch(url, { headers: { Authorization: `Basic ${encoded}` } });\n";

		AssertFires(source, "JS-004", ConfidenceLevel.High, "src/view.jsx");
	}

	// ------------------------------------------------------------------------------------------
	// The near-misses: kept out, or Low
	// ------------------------------------------------------------------------------------------

	[Theory]
	[InlineData("await fetch(url, { headers: { Authorization: `Bearer ${token}` } });\n")]
	[InlineData("xhr.setRequestHeader('Authorization', 'Bearer ' + token);\n")]
	[InlineData("superagent.get(url).auth(token, { type: 'bearer' });\n")]
	[InlineData("request.get(url, { auth: { bearer: token } });\n")]
	[InlineData("const bearerHeader = 'Bearer ' + token;\nawait fetch(url, { headers: { Authorization: bearerHeader } });\n")]
	[InlineData("const bearerHeader = getToken();\nawait fetch(url, { headers: { Authorization: bearerHeader } });\n")]
	[InlineData("const target = 'mongodb://exampleUser:exampleSecret@host.example/db';\n")]
	[InlineData("const response = await fetch('postgres://exampleUser:exampleSecret@host.example/db');\n")]
	[InlineData("fetch(url, { credentials: 'include' });\n")]
	[InlineData("const xhr = new XMLHttpRequest(); xhr.withCredentials = true;\n")]
	[InlineData("const value = req.headers.authorization;\n")]
	[InlineData("const value = headers.get('authorization');\n")]
	[InlineData("if (req.headers.authorization === 'Basic abc') { allow(); }\n")]
	[InlineData("const decoded = atob(value);\nconst text = Buffer.from(value, 'base64').toString();\n")]
	[InlineData("const key = btoa(host + ':' + port);\n")]
	[InlineData("const header = 'Basic ' + btoa(JSON.stringify(payload));\n")]
	[InlineData("const cacheKey = Buffer.from(cacheText).toString('base64');\n")]
	[InlineData("const header = 'Basic ' + Buffer.from(`${a}:${b}`).toString('base64url');\n")]
	[InlineData("// Basic auth is deprecated\nconst BASIC_PLAN = 1;\nconst o = { menu: { title: 'Basic' } };\n")]
	[InlineData("const o = { headers: { 'Proxy-Authorization': 'Basic x' } };\n")]
	[InlineData("const o = { 'X-Authorization': 'Basic x' };\n")]
	[InlineData("user.password = value;\n")]
	[InlineData("window.open(a, b, c, d, e);\ndocument.open();\n")]
	[InlineData("xhr.open('GET', url, true);\n")]
	[InlineData("sdk.auth(session);\n")]
	[InlineData("const target = new URL('mongodb://host.example');\ntarget.password = secret;\n")]
	[InlineData("http.request({ host: hostName }, handler);\n")]
	public void ANearMissThatTheCatalogKeepsOutProducesNoConstructAtAll(string source)
	{
		AssertSilent(source);
	}

	[Theory]
	[InlineData("const transport = openMailTransport({ host: mailHost, auth: { user: userName, pass: secret } });\n")]
	[InlineData("const pool = openDatabase({ host: dbHost, auth: { username: userName, password: secret } });\n")]
	[InlineData("const broker = connectBroker({ url: brokerUrl, auth: { login: userName, pwd: secret } });\n")]
	[InlineData("axios.get(url, { proxy: { host: h, port: 1, auth: { username: u, password: p } } });\n")]
	public void ANearMissThatTheCatalogRoutesToManualReviewIsRecognizedAtLowAndNeverAtHighOrMedium(string source)
	{
		ImmutableArray<RecognizedConstruct> constructs = new JavaScriptRecognitionHarness().Recognize(source);

		Assert.NotEmpty(constructs);
		Assert.True(constructs.All(construct => construct.Level is ConfidenceLevel.Low), $"[{source}] produced [{Describe(constructs)}]");
		Assert.Contains(constructs, construct => construct.PatternClassIdentifier == "JS-011");
	}

	[Fact]
	public void AnAuthObjectAdjacentToAnEnumeratedClientIsReadAtHighByItsOwnClassAndTheLowOneIsSubsumed()
	{
		const string source = "axios.get(url, { auth: { username: userName, password: secret } });\n";

		AssertFires(source, "JS-001", ConfidenceLevel.High);
		AssertNeverFires(source, "JS-011");
	}

	[Fact]
	public void ARequestClientUnderThePreemptiveSwitchOffIsReadAtLowAndNotAtHigh()
	{
		const string source = "request.get(url, { auth: { user: userName, pass: secret, sendImmediately: false } });\n";

		AssertFires(source, "JS-016", ConfidenceLevel.Low);
		AssertNeverFires(source, "JS-001");
		AssertNeverFires(source, "JS-011");
	}

	[Fact]
	public void AnAuthMethodWithOneArgumentIsReadAtMediumAndOneWithTwoAtHigh()
	{
		AssertFires("await superagent.get(url).auth(credentialPair);\n", "JS-012", ConfidenceLevel.Medium);
		AssertNeverFires("await superagent.get(url).auth(credentialPair);\n", "JS-002");
		AssertFires("await superagent.get(url).auth(userName, secret);\n", "JS-002", ConfidenceLevel.High);
		AssertNeverFires("await superagent.get(url).auth(userName, secret);\n", "JS-012");
	}

	[Fact]
	public void AnXhrOpenWithCredentialsAndAJqueryRequestWithThemAreReadAtLowOnlyBecauseTheySendNothingUntilChallenged()
	{
		AssertFires("xhr.open('GET', url, true, userName, secret);\n", "JS-009", ConfidenceLevel.Low);
		AssertFires("$.ajax({ url: apiUrl, username: userName, password: secret });\n", "JS-014", ConfidenceLevel.Low);
	}

	[Fact]
	public void ABasicHeaderBuiltInAHeaderTemplateWithTheEncoderInsideIsOneFindingAtTheHeaderAndTheEncoderIsSubsumed()
	{
		const string source = "const h = { headers: { Authorization: `Basic ${Buffer.from(`${userName}:${secret}`).toString('base64')}` } };\n";

		ImmutableArray<RecognizedConstruct> constructs = new JavaScriptRecognitionHarness().Recognize(source);

		Assert.Contains(constructs, construct => construct.PatternClassIdentifier == "JS-004" && construct.Level == ConfidenceLevel.High);
		Assert.DoesNotContain(constructs, construct => construct.PatternClassIdentifier == "JS-006");
	}

	[Theory]
	[InlineData("const h = { headers: { Authorization: 'Basic ' + Buffer.from(userName + ':' + secret).toString('base64') } };\n")]
	[InlineData("headers['Authorization'] = 'Basic ' + new Buffer(userName + ':' + secret).toString('base64');\n")]
	[InlineData("xhr.setRequestHeader('Authorization', 'Basic ' + Buffer.from(userName + ':' + secret).toString('base64'));\n")]
	[InlineData("res.setHeader('Authorization', `Basic ${Buffer.from(`${userName}:${secret}`).toString('base64')}`);\n")]
	[InlineData("headers.set('Authorization', 'Basic ' + Buffer.from(userName + ':' + secret).toString('base64'));\n")]
	[InlineData("request.append('Authorization', 'Basic ' + Buffer.from(userName + ':' + secret).toString('base64'));\n")]
	[InlineData("axios.defaults.headers.common['Authorization'] = 'Basic ' + Buffer.from(userName + ':' + secret).toString('base64');\n")]
	[InlineData("headers.authorization = 'Basic ' + Buffer.from(userName + ':' + secret).toString('base64');\n")]
	public void AnEncoderConcatenatedIntoABasicHeaderValueIsSubsumedByTheHeader(string source)
	{
		AssertFires(source, "JS-004", ConfidenceLevel.High);
		AssertNeverFires(source, "JS-006");
	}

	[Theory]
	[InlineData("const h = { 'Content-Type': 'text/plain', Authorization: 'Basic ' + Buffer.from(userName + ':' + secret).toString('base64') };\n")]
	[InlineData("const h = new Headers({ Accept: 'x', Authorization: 'Basic ' + Buffer.from(userName + ':' + secret).toString('base64') });\n")]
	public void AnEncoderInANonFirstAuthorizationKeyIsSubsumedBecauseTheKeyFormHasNoPositionalAnchor(string source)
	{
		AssertFires(source, "JS-004", ConfidenceLevel.High);
		AssertNeverFires(source, "JS-006");
	}

	/// <summary>The one declared form the merge does not mirror: the array form of the headers constructor. The header is read and the encoder stays, beside it.</summary>
	[Fact]
	public void AnEncoderInTheArrayFormOfTheHeadersConstructorIsNotSubsumedAndIsEmittedBesideTheHeaderFinding()
	{
		const string source = "const h = new Headers([['Authorization', 'Basic ' + Buffer.from(userName + ':' + secret).toString('base64')]]);\n";

		AssertFires(source, "JS-004", ConfidenceLevel.High);
		AssertFires(source, "JS-006", ConfidenceLevel.Medium);
	}

	[Fact]
	public void AnEncoderThatIsNotInsideABasicHeaderValueIsStillReadAtMedium()
	{
		AssertFires("const header = 'Basic ' + Buffer.from(`${userName}:${secret}`).toString('base64');\n", "JS-006", ConfidenceLevel.Medium);
		AssertFires("const token = Buffer.from(`${userName}:${secret}`).toString('base64'); // Authorization\n", "JS-006", ConfidenceLevel.Medium);
	}

	[Theory]
	[InlineData("// the Authorization scheme is Basic\nconst token = 'Basic ' + Buffer.from(userName + ':' + secret).toString('base64');\n")]
	[InlineData("const authorizationUrl = 'Basic ' + Buffer.from(userName + ':' + secret).toString('base64');\n")]
	[InlineData("const o = { authorizationUrl: 'Basic ' + Buffer.from(userName + ':' + secret).toString('base64') };\n")]
	[InlineData("const o = { 'Proxy-Authorization': 'Basic ' + Buffer.from(userName + ':' + secret).toString('base64') };\n")]
	[InlineData("logger.info('Authorization', 'Basic ' + Buffer.from(userName + ':' + secret).toString('base64'));\n")]
	[InlineData("req.authorization = 'Basic ' + Buffer.from(userName + ':' + secret).toString('base64');\n")]
	[InlineData("request['Authorization'] = 'Basic ' + Buffer.from(userName + ':' + secret).toString('base64');\n")]
	public void AnEncoderAfterAMentionOfTheWordWithNoHeaderWriteIsNotSubsumedAndIsReadAtMediumWithNoHeaderFinding(string source)
	{
		AssertFires(source, "JS-006", ConfidenceLevel.Medium);
		AssertNeverFires(source, "JS-004");
	}


	[Fact]
	public void AUserinfoLiteralHeldInTheArgumentWindowOfAConsumerIsOneFindingAtTheConsumer()
	{
		const string source = "http.get(target = 'https://exampleUser:exampleSecret@host.example/path', handler);\n";

		AssertFires(source, "JS-003", ConfidenceLevel.High);
		AssertNeverFires(source, "JS-013");
	}

	[Fact]
	public void AHeaderValueHeldInANameThatResolvesToBasicIsReadAtMediumAndOneThatResolvesToBearerIsSilent()
	{
		AssertFires("const encodedHeader = 'Basic ' + encodedPair;\nawait fetch(url, { headers: { Authorization: encodedHeader } });\n", "JS-017", ConfidenceLevel.Medium);
		AssertSilent("const encodedHeader = 'Bearer ' + token;\nawait fetch(url, { headers: { Authorization: encodedHeader } });\n");
		AssertSilent("let encodedHeader = 'Basic ' + a;\nencodedHeader = 'Bearer ' + b;\nawait fetch(url, { headers: { Authorization: encodedHeader } });\n");
	}

	[Fact]
	public void AHelperThatBuildsABasicHeaderFromItsParametersIsReportedAtLowAtTheDefinitionAndItsInnerConstructAtItsOwnAnchor()
	{
		const string source = "function buildBasicHeader(userName, secret) {\n\treturn { Authorization: 'Basic ' + btoa(userName + ':' + secret) };\n}\n";

		ImmutableArray<RecognizedConstruct> constructs = new JavaScriptRecognitionHarness().Recognize(source);

		Assert.Contains(constructs, construct => construct.PatternClassIdentifier == "JS-007" && construct.Level == ConfidenceLevel.Low);
		Assert.Contains(constructs, construct => construct.PatternClassIdentifier == "JS-004" && construct.Level == ConfidenceLevel.High);
	}

	[Fact]
	public void ATemplateWithNoHoleHoldingACredentialIsALiteralAndOneWithAHoleOrAPlaceholderIsNot()
	{
		RecognizedConstruct literal = Single("axios.get(url, { auth: { username: u, password: 'exampleSecret' } });\n", "JS-001");
		RecognizedConstruct template = Single("axios.get(url, { auth: { username: u, password: `exampleSecret` } });\n", "JS-001");
		RecognizedConstruct withAHole = Single("axios.get(url, { auth: { username: u, password: `${secret}` } });\n", "JS-001");
		RecognizedConstruct placeholder = Single("axios.get(url, { auth: { username: u, password: 'changeme' } });\n", "JS-001");
		RecognizedConstruct variable = Single("axios.get(url, { auth: { username: u, password: secret } });\n", "JS-001");

		Assert.True(literal.LiteralCredentialSignal);
		Assert.True(template.LiteralCredentialSignal);
		Assert.False(withAHole.LiteralCredentialSignal);
		Assert.False(placeholder.LiteralCredentialSignal);
		Assert.False(variable.LiteralCredentialSignal);
	}

	// ------------------------------------------------------------------------------------------
	// Helpers
	// ------------------------------------------------------------------------------------------

	private static ILanguageAnalyzer ReadAnalyzer()
	{
		return JavaScriptRegisteredExpressions.Analyzer;
	}

	private static ImmutableArray<string> ClassesAt(ILanguageAnalyzer analyzer, ConfidenceLevel level)
	{
		return [.. analyzer.DetectionRulesByPatternClass.Where(entry => entry.Value.BaseLevel == level).Select(entry => entry.Key).Order(StringComparer.Ordinal)];
	}

	private static RecognizedConstruct Single(string source, string patternClass)
	{
		return new JavaScriptRecognitionHarness().Recognize(source).Single(construct => construct.PatternClassIdentifier == patternClass);
	}

	private static void AssertFires(string source, string patternClass, ConfidenceLevel level, string relativePath = JavaScriptRecognitionHarness.UNDEMOTED_RELATIVE_PATH)
	{
		ImmutableArray<RecognizedConstruct> constructs = new JavaScriptRecognitionHarness().Recognize(source, relativePath);

		Assert.True(
			constructs.Any(construct => construct.PatternClassIdentifier == patternClass && construct.Level == level),
			$"{patternClass} did not fire at {level} on [{source}]; got [{Describe(constructs)}]");
	}

	private static void AssertNeverFires(string source, string patternClass)
	{
		ImmutableArray<RecognizedConstruct> constructs = new JavaScriptRecognitionHarness().Recognize(source);

		Assert.False(
			constructs.Any(construct => construct.PatternClassIdentifier == patternClass),
			$"{patternClass} fired on [{source}]; got [{Describe(constructs)}]");
	}

	private static void AssertSilent(string source)
	{
		ImmutableArray<RecognizedConstruct> constructs = new JavaScriptRecognitionHarness().Recognize(source);

		Assert.True(constructs.IsEmpty, $"[{source}] produced [{Describe(constructs)}]");
	}

	private static string Describe(ImmutableArray<RecognizedConstruct> constructs)
	{
		return string.Join(", ", constructs.Select(construct => $"{construct.PatternClassIdentifier}:{construct.Level}"));
	}

	/// <summary>One invented fixture per class, with the level the catalog gives it.</summary>
	internal static ImmutableArray<(string PatternClass, ConfidenceLevel Level, string Source)> ClassFixtures()
	{
		return
		[
			("JS-001", ConfidenceLevel.High, "const response = await axios.get(url, { auth: { username: userName, password: secret } });\n"),
			("JS-002", ConfidenceLevel.High, "const response = await superagent.get(url).auth(userName, secret);\n"),
			("JS-003", ConfidenceLevel.High, "const response = await fetch('https://exampleUser:exampleSecret@host.example/path');\n"),
			("JS-004", ConfidenceLevel.High, "await fetch(url, { headers: { Authorization: `Basic ${encoded}` } });\n"),
			("JS-005", ConfidenceLevel.Medium, "const header = 'Basic ' + btoa(userName + ':' + secret);\n"),
			("JS-006", ConfidenceLevel.Medium, "const header = 'Basic ' + Buffer.from(`${userName}:${secret}`).toString('base64');\n"),
			("JS-007", ConfidenceLevel.Low, "function buildBasicHeader(userName, secret) {\n\treturn { Authorization: 'Basic ' + btoa(userName + ':' + secret) };\n}\n"),
			("JS-008", ConfidenceLevel.High, "const req = http.request({ host: hostName, path: '/path', auth: `${userName}:${secret}` }, handler);\n"),
			("JS-009", ConfidenceLevel.Low, "xhr.open('GET', url, true, userName, secret);\n"),
			("JS-010", ConfidenceLevel.Medium, "const target = new URL(base);\ntarget.username = userName;\ntarget.password = secret;\n"),
			("JS-011", ConfidenceLevel.Low, "const transport = openMailTransport({ host: mailHost, port: mailPort, auth: { user: userName, pass: secret } });\n"),
			("JS-012", ConfidenceLevel.Medium, "const response = await superagent.get(url).auth(credentialPair);\n"),
			("JS-013", ConfidenceLevel.Medium, "const baseUrl = 'https://exampleUser:exampleSecret@host.example/path';\n"),
			("JS-014", ConfidenceLevel.Low, "$.ajax({ url: apiUrl, username: userName, password: secret });\n"),
			("JS-015", ConfidenceLevel.High, "const response = await got(url, { username: userName, password: secret });\n"),
			("JS-016", ConfidenceLevel.Low, "request.get(url, { auth: { user: userName, pass: secret, sendImmediately: false } });\n"),
			("JS-017", ConfidenceLevel.Medium, "const encodedHeader = 'Basic ' + encodedPair;\nawait fetch(url, { headers: { Authorization: encodedHeader } });\n")
		];
	}
}
