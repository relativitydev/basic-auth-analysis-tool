using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Lexical;
using MigrateAuth.Cli.Recognition;
using MigrateAuth.Cli.Reporting;
using MigrateAuth.Cli.Tests.Recognition;

using Xunit;

namespace MigrateAuth.Cli.Tests.Catalog;

/// <summary>
/// The `Go` analyzer against its catalog, through the analyzer the binary ships and the real recognition engine: never through a hand-built construct, because what is under test is
/// whether the expressions the analyzer declares recognize the forms the catalog states and decline the exclusions it states.
///
/// The contract part holds what the analyzer declares: thirteen classes (four High, six Medium, three Low), their self-sufficiency, credential positions, symbolic verdicts, subsumption
/// triples and preselection anchors, and that it declares no first-line rule, no flow signal and no template yet. The fixture part holds one invented source per class and the level it ends at.
/// Every source below is invented from the catalog's own prose, with placeholder names throughout.
/// </summary>
public sealed class GoAnalyzerTests
{
	private const string _CLASS_PREFIX = "GO-";
	private const int _CLASS_COUNT = 13;
	private const int _CLASSES_THAT_CAN_REACH_HIGH_OR_MEDIUM = 10;
	private const int _LOW_BY_DEFINITION_CLASS_COUNT = 3;
	private const int _ANCHOR_COUNT = 6;
	private const int _SUBSUMPTION_TRIPLE_COUNT = 3;
	private const string _CONSTRUCT_GROUP = "construct";
	private const string _CREDENTIAL_GROUP = "credential";
	private const string _SCHEME_GROUP = "scheme";
	private const string _CALLED_NAME_GROUP = "name";
	private const string _LOW_REASON_FIELD_NAME = "_patternClassSentencesByIdentifier";

	private static readonly ImmutableArray<string> _everyClass =
		[.. Enumerable.Range(1, _CLASS_COUNT).Select(number => string.Create(CultureInfo.InvariantCulture, $"{_CLASS_PREFIX}{number:D3}"))];

	private static readonly ImmutableArray<string> _highClasses = ["GO-001", "GO-002", "GO-003", "GO-005"];

	private static readonly ImmutableArray<string> _mediumClasses = ["GO-004", "GO-006", "GO-007", "GO-008", "GO-009", "GO-013"];

	private static readonly ImmutableArray<string> _lowClasses = ["GO-010", "GO-011", "GO-012"];

	/// <summary>The classes that declare a literal credential: the group named <c>credential</c>.</summary>
	private static readonly ImmutableArray<string> _literalCredentialClasses = ["GO-001", "GO-002", "GO-003", "GO-004", "GO-005", "GO-006", "GO-007", "GO-008"];

	// ------------------------------------------------------------------------------------------
	// The contract
	// ------------------------------------------------------------------------------------------

	[Fact]
	public void TheGoAnalyzerFitsTheContractWithNoViolations()
	{
		Assert.Empty(AnalyzerContractValidator.Validate(ReadAnalyzer()));
	}

	[Fact]
	public void ThirteenClassesAreDeclaredAndTenCanReachHighOrMedium()
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
		ILanguageAnalyzer analyzer = ReadAnalyzer();

		Assert.All(analyzer.DetectionRulesByPatternClass, entry =>
			Assert.Equal(
				_highClasses.Contains(entry.Key) ? SelfSufficiency.DeclaresSchemeAndCredentialCarrier : SelfSufficiency.RequiresCallPathToBeEstablished,
				entry.Value.SelfSufficiency));
	}

	[Fact]
	public void TheLowByDefinitionClassesAreExactlyTheKeysOfTheReportVocabularyForGo()
	{
		FieldInfo field = typeof(LowReasonVocabulary).GetField(_LOW_REASON_FIELD_NAME, BindingFlags.NonPublic | BindingFlags.Static)
			?? throw new InvalidOperationException("The pattern class sentences of the vocabulary were not found.");
		FrozenDictionary<string, string> sentences = (FrozenDictionary<string, string>)field.GetValue(null)!;
		string[] vocabularyKeys = [.. sentences.Keys.Where(key => key.StartsWith(_CLASS_PREFIX, StringComparison.Ordinal)).Order(StringComparer.Ordinal)];

		Assert.Equal(vocabularyKeys, ClassesAt(ReadAnalyzer(), ConfidenceLevel.Low).AsEnumerable());
	}

	[Fact]
	public void EveryExpressionIsCaseSensitiveCarriesTheInvariantOptionsAndThePolicyTimeout()
	{
		ILanguageAnalyzer analyzer = ReadAnalyzer();
		const RegexOptions required = RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture;

		Assert.All(analyzer.DetectionRulesByPatternClass, entry =>
		{
			Assert.Equal(required, entry.Value.Expression.Options & required);
			Assert.Equal(RegexOptions.None, entry.Value.Expression.Options & RegexOptions.IgnoreCase);
			Assert.Equal(RecognitionEnginePolicy.MATCH_BOUND, entry.Value.Expression.MatchTimeout);
		});
	}

	[Fact]
	public void EveryRuleAnchorsOnTheConstructGroupAndEveryDestinationIsUnknown()
	{
		ILanguageAnalyzer analyzer = ReadAnalyzer();

		Assert.All(analyzer.DetectionRulesByPatternClass, entry =>
		{
			Assert.Equal(_CONSTRUCT_GROUP, entry.Value.AnchorPosition.CaptureGroupName);
			Assert.Contains(_CONSTRUCT_GROUP, entry.Value.Expression.GetGroupNames());
			Assert.Equal(DestinationCertainty.Unknown, entry.Value.DestinationCertainty);
		});
	}

	[Fact]
	public void TheCredentialPositionsAreTheOnesTheCatalogDeclares()
	{
		ILanguageAnalyzer analyzer = ReadAnalyzer();

		Assert.All(analyzer.DetectionRulesByPatternClass, entry =>
		{
			CredentialPosition position = entry.Value.CredentialPosition;

			if (_literalCredentialClasses.Contains(entry.Key))
			{
				Assert.Equal(_CREDENTIAL_GROUP, position.CaptureGroupName);
				Assert.Contains(_CREDENTIAL_GROUP, entry.Value.Expression.GetGroupNames());
			}
			else
			{
				Assert.False(position.IsDeclared, entry.Key);
			}
		});
		Assert.True(analyzer.DetectionRulesByPatternClass["GO-001"].CredentialPosition.ReadsABareWordAsLiteral);
	}

	[Fact]
	public void OnlyTheHeldValueTheEnvironmentValueAndTheLocalHelperClassesAreSubjectToSymbolicResolutionAndTheirCellsAreTheOnesTheCatalogDeclares()
	{
		ILanguageAnalyzer analyzer = ReadAnalyzer();
		string[] resolved = [.. analyzer.DetectionRulesByPatternClass.Where(entry => entry.Value.SymbolicVerdictInterpretation.IsSubjectToSymbolicResolution).Select(entry => entry.Key).Order(StringComparer.Ordinal)];
		SymbolicVerdictInterpretation held = analyzer.DetectionRulesByPatternClass["GO-009"].SymbolicVerdictInterpretation;
		SymbolicVerdictInterpretation environment = analyzer.DetectionRulesByPatternClass["GO-012"].SymbolicVerdictInterpretation;
		SymbolicVerdictInterpretation localHelper = analyzer.DetectionRulesByPatternClass["GO-013"].SymbolicVerdictInterpretation;

		Assert.Equal(["GO-009", "GO-012", "GO-013"], resolved);
		Assert.Equal(_CALLED_NAME_GROUP, localHelper.SchemeArgumentCaptureGroupName);
		Assert.Equal(SymbolRecordKind.Member, localHelper.SymbolRecordKind);
		Assert.Equal(SymbolSourceKind.Environment, localHelper.ExpectedSourceKind);
		Assert.Equal(SymbolicVerdictResponse.ConfirmsPatternClass, localHelper.WhenResolvedMatching);
		Assert.Equal(SymbolicVerdictResponse.SuppressesFinding, localHelper.WhenResolvedNotMatching);
		Assert.Equal(SymbolicVerdictResponse.SuppressesFinding, localHelper.WhenIndeterminate);
		Assert.Equal(SymbolRecordKind.Variable, held.SymbolRecordKind);
		Assert.Equal(_SCHEME_GROUP, held.SchemeArgumentCaptureGroupName);
		Assert.Equal("Basic", held.ExpectedSchemeToken);
		Assert.Equal(SymbolicVerdictResponse.ConfirmsPatternClass, held.WhenResolvedMatching);
		Assert.Equal(SymbolicVerdictResponse.SuppressesFinding, held.WhenResolvedNotMatching);
		Assert.Equal(SymbolicVerdictResponse.SuppressesFinding, held.WhenIndeterminate);
		Assert.Equal(_SCHEME_GROUP, environment.SchemeArgumentCaptureGroupName);
		Assert.Equal(SymbolSourceKind.Environment, environment.ExpectedSourceKind);
		Assert.Equal(SymbolicVerdictResponse.NoEffect, environment.WhenSchemeArgumentDidNotParticipate);
	}

	[Fact]
	public void TheSubsumptionRelationIsTheThreeTriplesOfTheCatalogAndDeclaresNoTripleWithGo005AsTheSubsumingClass()
	{
		(string Subsuming, string Subsumed)[] expected = [("GO-002", "GO-006"), ("GO-003", "GO-006"), ("GO-004", "GO-006")];
		FrozenSet<SubsumptionRule> relation = ReadAnalyzer().SubsumptionRelation;

		Assert.Equal(_SUBSUMPTION_TRIPLE_COUNT, relation.Count);
		Assert.All(relation, rule => Assert.Equal(SubsumptionCondition.MatchSpansOverlap, rule.When));
		Assert.Equal(
			expected.Order().ToArray(),
			relation.Select(rule => (rule.SubsumingPatternClassIdentifier, rule.SubsumedPatternClassIdentifier)).Order().ToArray());
		Assert.DoesNotContain(relation, rule => rule.SubsumingPatternClassIdentifier == "GO-005");
		Assert.DoesNotContain(relation, rule => rule.SubsumingPatternClassIdentifier == "GO-013" || rule.SubsumedPatternClassIdentifier == "GO-013");
	}

	[Fact]
	public void TheSixAnchorsAreInsensitiveAndAreTheOnesTheCatalogNames()
	{
		FrozenSet<PreselectionAnchor> anchors = ReadAnalyzer().CandidateSelectionAndRecommendation.PreselectionAnchors;

		Assert.Equal(_ANCHOR_COUNT, anchors.Count);
		Assert.All(anchors, anchor => Assert.Equal(CaseSensitivity.Insensitive, anchor.Sensitivity));
		Assert.Equal(
			new[] { "://", "Authorization", "RoundTrip", "SetBasicAuth", "StdEncoding", "UserPassword" }.Order(StringComparer.Ordinal),
			anchors.Select(anchor => anchor.Text).Order(StringComparer.Ordinal));
	}

	[Fact]
	public void NoBasicAnchorAndNoEncoderNameAnchorIsDeclaredBecauseBothAdmitFarTooMuch()
	{
		FrozenSet<PreselectionAnchor> anchors = ReadAnalyzer().CandidateSelectionAndRecommendation.PreselectionAnchors;

		Assert.DoesNotContain(anchors, anchor => anchor.Text.Equals("Basic", StringComparison.OrdinalIgnoreCase));
		Assert.DoesNotContain(anchors, anchor => anchor.Text.Equals("EncodeToString", StringComparison.OrdinalIgnoreCase));
	}

	[Fact]
	public void TheAnalyzerDeclaresNoFirstLineRuleNoConstructPrecedenceNoDialectNoWrapperTrackingAndNoVariableUse()
	{
		ILanguageAnalyzer analyzer = ReadAnalyzer();
		CandidateSelectionAndRecommendation element = analyzer.CandidateSelectionAndRecommendation;

		Assert.Null(analyzer.LanguageIdentification.FirstLineDetectionExpression);
		Assert.Null(analyzer.ConstructPrecedence);
		Assert.Null(element.FileDialects);
		Assert.Null(element.WrapperTracking);
		Assert.All(analyzer.DetectionRulesByPatternClass, entry => Assert.Null(entry.Value.VariableUse));
	}

	[Fact]
	public void TheAnalyzerIdentifiesFilesByTheGoExtensionAloneAndWithoutRegardToCase()
	{
		LanguageIdentification identification = ReadAnalyzer().LanguageIdentification;

		Assert.Equal([".go"], identification.FileExtensions.ToArray());
		Assert.Contains(".GO", (ISet<string>)identification.FileExtensions);
		Assert.Equal([GoRegisteredExpressions.LANGUAGE_NAME], identification.DeclaredLanguageNames.ToArray());
		Assert.Equal(GoRegisteredExpressions.LANGUAGE_NAME, identification.LanguageNameDerivation("main.go", string.Empty));
	}

	[Fact]
	public void TheEmbeddedCatalogResolvesAGoFileToTheGoAnalyzer()
	{
		LanguageCatalog catalog = LanguageCatalogRegistry.BuildEmbeddedCatalog();

		Assert.True(catalog.TryGetAnalyzerForFileExtension(".go", out ILanguageAnalyzer? analyzer));
		Assert.Contains(GoRegisteredExpressions.LANGUAGE_NAME, (ISet<string>)analyzer!.LanguageIdentification.DeclaredLanguageNames);
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
		GoRecognitionHarness harness = new();

		foreach ((string patternClass, _, string source) in ClassFixtures())
		{
			FileRecognitionContext context = harness.ContextFor(source);
			RecognizedConstruct construct = harness.Recognize(source).First(candidate => candidate.PatternClassIdentifier == patternClass);

			Assert.Equal(LexicalZone.Code, context.Mask.ZoneAt(construct.AnchorOffset));
		}
	}

	[Theory]
	[InlineData("GO-001", "request.SetBasicAuth(userName, secret)\n")]
	[InlineData("GO-001", "client.SetBasicAuth(userName, secret)\n")]
	[InlineData("GO-001", "http.Request.SetBasicAuth(request, userName, secret)\n")]
	[InlineData("GO-001", "(*http.Request).SetBasicAuth(request, userName, secret)\n")]
	[InlineData("GO-001", "request.SetBasicAuth(\n\tuserName,\n\tsecret,\n)\n")]
	[InlineData("GO-002", "request.Header.Set(\"Authorization\", \"Basic \"+encoded)\n")]
	[InlineData("GO-002", "request.Header.Add(\"authorization\", \"Basic \"+encoded)\n")]
	[InlineData("GO-002", "h.Set(`Authorization`, `Basic ` + encoded)\n")]
	[InlineData("GO-002", "request.Header.Set(\"Authorization\", \"BASIC \"+encoded)\n")]
	[InlineData("GO-002", "request.Header.Set(\"Authorization\", fmt.Sprintf(\"Basic %s\", encoded))\n")]
	[InlineData("GO-002", "request.Header.Set(\"Authorization\", \"Basic dXNlcjpwYXNz\")\n")]
	[InlineData("GO-002", "request.Header[\"Authorization\"] = []string{\"Basic \" + encoded}\n")]
	[InlineData("GO-002", "headers[\"authorization\"] = append(headers[\"authorization\"], \"Basic \"+encoded)\n")]
	[InlineData("GO-003", "headers := http.Header{\"Authorization\": {\"Basic \" + encoded}}\n")]
	[InlineData("GO-003", "other := h.Header{\"authorization\": []string{\"Basic \" + encoded}}\n")]
	[InlineData("GO-003", "headers := Header{\"Authorization\": {\"Basic \" + encoded}}\n")]
	[InlineData("GO-004", "flat := map[string]string{\"Authorization\": \"Basic \" + encoded}\n")]
	[InlineData("GO-004", "headers := map[string][]string{\"Authorization\": {\"Basic \" + encoded}}\n")]
	[InlineData("GO-005", "request, err := http.NewRequest(http.MethodGet, \"https://user:secret@host.example/path\", nil)\n")]
	[InlineData("GO-005", "response, err := client.Get(\"HTTPS://user:secret@host.example/path\")\n")]
	[InlineData("GO-005", "response, err := http.PostForm(\"http://user:secret@host.example/path\", form)\n")]
	[InlineData("GO-006", "pair := b64.StdEncoding.EncodeToString([]byte(fmt.Sprintf(\"%s:%s\", userName, secret)))\nvalue := \"Basic \" + pair\n")]
	[InlineData("GO-006", "pair := base64.RawStdEncoding.EncodeToString([]byte(strings.Join(parts, \":\")))\nvalue := \"Basic \" + pair\n")]
	[InlineData("GO-006", "pair := base64.StdEncoding.EncodeToString([]byte(\"user:secret\"))\nvalue := \"Basic \" + pair\n")]
	[InlineData("GO-006", "pair := StdEncoding.EncodeToString([]byte(userName + \":\" + secret)) // Authorization\n")]
	[InlineData("GO-007", "address := &url.URL{Scheme: \"https\", User: url.UserPassword(userName, secret), Host: \"host.example\"}\n")]
	[InlineData("GO-007", "address.User = neturl.UserPassword(userName, \"exampleSecret\")\n")]
	[InlineData("GO-008", "const serviceURL = \"https://user:secret@host.example/path\"\n")]
	[InlineData("GO-008", "var serviceURL string = \"http://user:secret@host.example/path\"\n")]
	[InlineData("GO-008", "address, err := url.ParseRequestURI(\"https://user:secret@host.example/path\")\n")]
	[InlineData("GO-009", "header := \"Basic \" + encoded\nrequest.Header.Set(\"Authorization\", header)\n")]
	[InlineData("GO-009", "const value = `Basic ` + encoded\nheaders[\"Authorization\"] = []string{value}\n")]
	[InlineData("GO-012", "value, found := os.LookupEnv(\"SERVICE_AUTH_HEADER\")\nrequest.Header.Set(\"Authorization\", value)\n")]
	[InlineData("GO-012", "request.Header.Set(\"Authorization\", os.Getenv(`SERVICE_AUTH_HEADER`))\n")]
	[InlineData("GO-013", "func basicHeader(raw []byte) string {\n\treturn \"Basic \" + base64.StdEncoding.EncodeToString(raw)\n}\nfunc send() {\n\trequest.Header.Add(\"Authorization\", basicHeader(raw))\n}\n")]
	[InlineData("GO-013", "func send() {\n\trequest.Header[\"Authorization\"] = []string{basicHeader(raw)}\n}\nfunc basicHeader(raw []byte) string {\n\treturn \"Basic \" + base64.StdEncoding.EncodeToString(raw)\n}\n")]
	[InlineData("GO-013", "func basicHeader(raw []byte) string {\n\treturn \"Basic \" + base64.StdEncoding.EncodeToString(raw)\n}\nfunc send() {\n\trequest.Header[\"Authorization\"] = append(request.Header[\"Authorization\"], basicHeader(raw))\n}\n")]
	public void AFormTheCatalogStatesAsReadIsRecognizedByItsClass(string patternClass, string source)
	{
		ILanguageAnalyzer analyzer = ReadAnalyzer();
		ConfidenceLevel level = analyzer.DetectionRulesByPatternClass[patternClass].BaseLevel;

		AssertFires(source, patternClass, level);
	}

	[Theory]
	[InlineData("func (transport *basicTransport) RoundTrip(request *http.Request) (*http.Response, error) {\n\trequest.Header.Set(\"Authorization\", \"Basic \"+transport.token)\n\treturn transport.base.RoundTrip(request)\n}\n")]
	[InlineData("func (transport basicTransport) RoundTrip(request *http.Request) (*http.Response, error) {\n\trequest.SetBasicAuth(transport.userName, transport.secret)\n\treturn nil, nil\n}\n")]
	public void ATransportThatWritesBasicIsReadAtLowByTheDefinitionClassAndItsInnerConstructByItsOwnClass(string source)
	{
		AssertFires(source, "GO-010", ConfidenceLevel.Low);
		Assert.Contains(new GoRecognitionHarness().Recognize(source), construct => construct.PatternClassIdentifier != "GO-010" && construct.Level == ConfidenceLevel.High);
	}

	[Fact]
	public void ATransportWhoseBodyHoldsNoBasicConstructIsSilentAndABareBasicLiteralCountsForNeitherDefinitionClass()
	{
		AssertSilent("func (transport *basicTransport) RoundTrip(request *http.Request) (*http.Response, error) {\n\treturn transport.base.RoundTrip(request)\n}\n");
		AssertSilent("func prefix(token string) string {\n\treturn \"Basic \" + token\n}\n");
	}

	[Fact]
	public void AHelperThatBuildsABasicHeaderFromItsOwnParameterIsReadAtLowAndOneThatUsesOnlyItsOwnLocalsIsNot()
	{
		AssertFires("func applyBasic(request *http.Request, encoded string) {\n\trequest.Header.Set(\"Authorization\", \"Basic \"+encoded)\n}\n", "GO-011", ConfidenceLevel.Low);
		Assert.DoesNotContain(
			new GoRecognitionHarness().Recognize("func applyBasic(request *http.Request) {\n\tencoded := load()\n\trequest.Header.Set(\"Authorization\", \"Basic \"+encoded)\n}\n"),
			construct => construct.PatternClassIdentifier == "GO-011");
	}

	// ------------------------------------------------------------------------------------------
	// One finding per construct, and the subsumption triples that make it so
	// ------------------------------------------------------------------------------------------

	[Theory]
	[InlineData("request.Header.Set(\"Authorization\", \"Basic \"+base64.StdEncoding.EncodeToString([]byte(userName+\":\"+secret)))\n", "GO-002", ConfidenceLevel.High)]
	[InlineData("headers := http.Header{\"Authorization\": {\"Basic \" + base64.StdEncoding.EncodeToString([]byte(userName+\":\"+secret))}}\n", "GO-003", ConfidenceLevel.High)]
	[InlineData("flat := map[string]string{\"Authorization\": \"Basic \" + base64.StdEncoding.EncodeToString([]byte(userName+\":\"+secret))}\n", "GO-004", ConfidenceLevel.Medium)]
	public void ABasicHeaderWhoseValueNestsTheEncoderIsReadAtTheCarrierAtTheLevelOfItsClass(string source, string patternClass, ConfidenceLevel level)
	{
		AssertFires(source, patternClass, level);
	}

	[Fact]
	public void ASplitConstructionGivesTwoFindingsAndTwoDistinctCallsOnTwoLinesGiveTwoFindings()
	{
		GoRecognitionHarness harness = new();

		Assert.Equal(2, harness.Recognize("pair := base64.StdEncoding.EncodeToString([]byte(userName + \":\" + secret))\nrequest.Header.Set(\"Authorization\", \"Basic \"+pair)\n").Length);
		Assert.Equal(2, harness.Recognize("first.SetBasicAuth(a, b)\nsecond.SetBasicAuth(c, d)\n").Length);
	}

	// ------------------------------------------------------------------------------------------
	// The literal credential, and the warning that goes with it
	// ------------------------------------------------------------------------------------------

	[Theory]
	[InlineData("GO-001", "request.SetBasicAuth(userName, \"exampleSecret\")\n", true)]
	[InlineData("GO-001", "request.SetBasicAuth(userName, secret)\n", false)]
	[InlineData("GO-001", "request.SetBasicAuth(userName, \"changeme\")\n", false)]
	[InlineData("GO-001", "request.SetBasicAuth(userName, \"****\")\n", false)]
	[InlineData("GO-001", "request.SetBasicAuth(userName, \"<password>\")\n", false)]
	[InlineData("GO-001", "request.SetBasicAuth(userName, os.Getenv(\"SERVICE_SECRET\"))\n", false)]
	[InlineData("GO-001", "request.SetBasicAuth(userName, \"abc\"+suffix)\n", false)]
	[InlineData("GO-002", "request.Header.Set(\"Authorization\", \"Basic dXNlcjpwYXNz\")\n", true)]
	[InlineData("GO-002", "request.Header.Set(\"Authorization\", \"Basic \"+encoded)\n", false)]
	[InlineData("GO-002", "request.Header.Set(\"Authorization\", \"Basic \"+os.Getenv(\"TOKEN\"))\n", false)]
	[InlineData("GO-005", "response, err := http.Get(\"https://user:exampleSecret@host.example/path\")\n", true)]
	[InlineData("GO-005", "response, err := http.Get(\"https://user:changeme@host.example/path\")\n", false)]
	[InlineData("GO-007", "address.User = url.UserPassword(userName, \"exampleSecret\")\n", true)]
	[InlineData("GO-007", "address.User = url.UserPassword(userName, secret)\n", false)]
	[InlineData("GO-008", "const serviceURL = \"https://user:exampleSecret@host.example/path\"\n", true)]
	[InlineData("GO-006", "pair := base64.StdEncoding.EncodeToString([]byte(\"user:exampleSecret\")) // Basic\n", true)]
	[InlineData("GO-006", "pair := base64.StdEncoding.EncodeToString([]byte(userName + \":\" + secret)) // Basic\n", false)]
	public void TheLiteralCredentialSignalIsTrueOnlyWhereTheCredentialIsTheWholeLiteralAndNotAPlaceholder(string patternClass, string source, bool expected)
	{
		RecognizedConstruct construct = new GoRecognitionHarness().Recognize(source).First(candidate => candidate.PatternClassIdentifier == patternClass);

		Assert.Equal(expected, construct.LiteralCredentialSignal);
	}

	// ------------------------------------------------------------------------------------------
	// The symbol table: an identifier that holds the header value
	// ------------------------------------------------------------------------------------------

	[Theory]
	[InlineData("header := \"Basic \" + encoded\nrequest.Header.Set(\"Authorization\", header)\n", ConfidenceLevel.Medium, "GO-009")]
	[InlineData("const scheme = `Basic `\nrequest.Header.Set(\"Authorization\", scheme)\n", ConfidenceLevel.Medium, "GO-009")]
	[InlineData("value := os.Getenv(\"SERVICE_AUTH_HEADER\")\nrequest.Header.Set(\"Authorization\", value)\n", ConfidenceLevel.Low, "GO-012")]
	public void AnIdentifierValueIsClassifiedByWhatTheFileSymbolTableResolvesItTo(string source, ConfidenceLevel level, string patternClass)
	{
		RecognizedConstruct only = Assert.Single(new GoRecognitionHarness().Recognize(source), construct => construct.PatternClassIdentifier is "GO-009" or "GO-012");

		Assert.Equal(patternClass, only.PatternClassIdentifier);
		Assert.Equal(level, only.Level);
	}

	[Theory]
	[InlineData("header := \"Bearer \" + token\nrequest.Header.Set(\"Authorization\", header)\n")]
	[InlineData("header := loadValue()\nrequest.Header.Set(\"Authorization\", header)\n")]
	[InlineData("request.Header.Set(\"Authorization\", header)\n")]
	[InlineData("header := \"Basic \" + encoded\nheader = \"fixed\"\nrequest.Header.Set(\"Authorization\", header)\n")]
	public void ABearerValueAnIndeterminateNameAndAnUndeclaredNameAreSilent(string source)
	{
		AssertSilent(source);
	}

	// ------------------------------------------------------------------------------------------
	// Helpers
	// ------------------------------------------------------------------------------------------

	private static ILanguageAnalyzer ReadAnalyzer()
	{
		return GoRegisteredExpressions.Analyzer;
	}

	private static ImmutableArray<string> ClassesAt(ILanguageAnalyzer analyzer, ConfidenceLevel level)
	{
		return [.. analyzer.DetectionRulesByPatternClass.Where(entry => entry.Value.BaseLevel == level).Select(entry => entry.Key).Order(StringComparer.Ordinal)];
	}

	private static void AssertFires(string source, string patternClass, ConfidenceLevel level)
	{
		ImmutableArray<RecognizedConstruct> constructs = new GoRecognitionHarness().Recognize(source);

		Assert.True(
			constructs.Any(construct => construct.PatternClassIdentifier == patternClass && construct.Level == level),
			$"{patternClass} did not fire at {level} on [{Escape(source)}]; got [{Describe(constructs)}]");
	}

	private static void AssertSilent(string source)
	{
		ImmutableArray<RecognizedConstruct> constructs = new GoRecognitionHarness().Recognize(source);

		Assert.True(constructs.IsEmpty, $"[{Escape(source)}] produced [{Describe(constructs)}]");
	}

	private static string Describe(ImmutableArray<RecognizedConstruct> constructs)
	{
		return string.Join(", ", constructs.Select(construct => $"{construct.PatternClassIdentifier}:{construct.Level}"));
	}

	private static string Escape(string text)
	{
		return text.Replace("\n", "\\n", StringComparison.Ordinal).Replace("\t", "\\t", StringComparison.Ordinal);
	}

	/// <summary>One invented fixture per class, with the level the catalog gives it. Shared with the candidate-selection tests.</summary>
	internal static ImmutableArray<(string PatternClass, ConfidenceLevel Level, string Source)> ClassFixtures()
	{
		return
		[
			("GO-001", ConfidenceLevel.High, "request, err := http.NewRequest(http.MethodGet, endpoint, nil)\nrequest.SetBasicAuth(userName, secret)\n"),
			("GO-002", ConfidenceLevel.High, "request.Header.Set(\"Authorization\", \"Basic \"+encoded)\nrequest.Header[\"Authorization\"] = []string{\"Basic \" + encoded}\n"),
			("GO-003", ConfidenceLevel.High, "headers := http.Header{\"Authorization\": {\"Basic \" + encoded}}\n"),
			("GO-004", ConfidenceLevel.Medium, "flat := map[string]string{\"Authorization\": \"Basic \" + encoded}\n"),
			("GO-005", ConfidenceLevel.High, "response, err := http.Get(\"https://user:secret@host.example/path\")\n"),
			("GO-006", ConfidenceLevel.Medium, "pair := base64.StdEncoding.EncodeToString([]byte(userName + \":\" + secret))\nvalue := \"Basic \" + pair\n"),
			("GO-007", ConfidenceLevel.Medium, "address.User = url.UserPassword(userName, secret)\n"),
			("GO-008", ConfidenceLevel.Medium, "address, err := url.Parse(\"https://user:secret@host.example/path\")\n"),
			("GO-009", ConfidenceLevel.Medium, "header := \"Basic \" + encoded\nrequest.Header.Set(\"Authorization\", header)\n"),
			("GO-010", ConfidenceLevel.Low, "func (transport *basicTransport) RoundTrip(request *http.Request) (*http.Response, error) {\n\trequest.SetBasicAuth(transport.userName, transport.secret)\n\treturn transport.base.RoundTrip(request)\n}\n"),
			("GO-011", ConfidenceLevel.Low, "func basicHeader(userName string, secret string) string {\n\treturn \"Basic \" + base64.StdEncoding.EncodeToString([]byte(userName+\":\"+secret))\n}\n"),
			("GO-012", ConfidenceLevel.Low, "request.Header.Set(\"Authorization\", os.Getenv(\"SERVICE_AUTH_HEADER\"))\n"),
			("GO-013", ConfidenceLevel.Medium, "func basicHeader(userName string, secret string) string {\n\treturn \"Basic \" + base64.StdEncoding.EncodeToString([]byte(userName+\":\"+secret))\n}\n\nfunc send() {\n\trequest.Header.Set(\"Authorization\", basicHeader(userName, secret))\n}\n")
		];
	}
}
