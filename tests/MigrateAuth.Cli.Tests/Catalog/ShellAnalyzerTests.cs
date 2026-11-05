using System.Collections.Frozen;
using System.Collections.Immutable;
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
/// The `Shell` analyzer against its catalog, through the analyzer the binary ships and the real recognition engine: never through a hand-built construct,
/// because what is under test is whether the expressions the analyzer declares recognize the forms the catalog states and decline the exclusions it states.
///
/// Every script below is INVENTED from the catalog's own prose, with placeholder names throughout, and carries no fragment, path or third-party name from
/// any repository.
/// </summary>
public sealed class ShellAnalyzerTests
{
	private const string _CLASS_PREFIX = "SH-";
	private const int _CLASS_COUNT = 25;
	private const int _CLASSES_THAT_CAN_REACH_HIGH_OR_MEDIUM = 18;
	private const int _LOW_BY_DEFINITION_CLASS_COUNT = 7;
	private const int _ANCHOR_COUNT = 11;
	private const string _CONSTRUCT_GROUP = "construct";
	private const string _CREDENTIAL_GROUP = "credential";
	private const string _LOW_REASON_FIELD_NAME = "_patternClassSentencesByIdentifier";

	private static readonly ImmutableArray<string> _everyClass =
		[.. Enumerable.Range(1, _CLASS_COUNT).Select(number => string.Create(System.Globalization.CultureInfo.InvariantCulture, $"{_CLASS_PREFIX}{number:D3}"))];

	private static readonly ImmutableArray<string> _lowClasses = ["SH-011", "SH-012", "SH-015", "SH-017", "SH-020", "SH-022", "SH-023"];

	private static readonly ImmutableArray<string> _highClasses = ["SH-001", "SH-002", "SH-003", "SH-004", "SH-005", "SH-013", "SH-014", "SH-016"];

	private static readonly ImmutableArray<string> _mediumClasses = ["SH-006", "SH-007", "SH-008", "SH-009", "SH-010", "SH-018", "SH-019", "SH-021", "SH-024", "SH-025"];

	/// <summary>The classes whose credential group reads a bare word as literal: the three forms a bare word can carry, and the fifth form.</summary>
	private static readonly ImmutableArray<string> _bareWordCredentialClasses =
	[
		"SH-001", "SH-004", "SH-005", "SH-007", "SH-008", "SH-010", "SH-011", "SH-013", "SH-014", "SH-015", "SH-016", "SH-017", "SH-019", "SH-020", "SH-023"
	];

	/// <summary>The two classes whose credential is the token of a literal header, which stands in a quoted string.</summary>
	private static readonly ImmutableArray<string> _tokenCredentialClasses = ["SH-003", "SH-018"];

	// ------------------------------------------------------------------------------------------
	// The contract
	// ------------------------------------------------------------------------------------------

	[Fact]
	public void TheShellAnalyzerFitsTheContractWithNoViolations()
	{
		Assert.Empty(AnalyzerContractValidator.Validate(ReadAnalyzer()));
	}

	[Fact]
	public void TwentyFiveClassesAreDeclaredAndEighteenCanReachHighOrMedium()
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
	public void TheLowByDefinitionClassesAreExactlyTheKeysOfTheReportVocabularyForShell()
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
	public void EveryRuleAnchorsOnTheConstructGroupAndNoClassDeclaresASchemeGroup()
	{
		ILanguageAnalyzer analyzer = ReadAnalyzer();

		Assert.All(analyzer.DetectionRulesByPatternClass, entry =>
		{
			Assert.Equal(_CONSTRUCT_GROUP, entry.Value.AnchorPosition.CaptureGroupName);
			Assert.Contains(_CONSTRUCT_GROUP, entry.Value.Expression.GetGroupNames());
			Assert.False(entry.Value.SymbolicVerdictInterpretation.IsSubjectToSymbolicResolution, entry.Key);
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

			if (_bareWordCredentialClasses.Contains(entry.Key))
			{
				Assert.Equal(_CREDENTIAL_GROUP, position.CaptureGroupName);
				Assert.True(position.ReadsABareWordAsLiteral, entry.Key);
			}
			else if (_tokenCredentialClasses.Contains(entry.Key))
			{
				Assert.Equal(_CREDENTIAL_GROUP, position.CaptureGroupName);
				Assert.False(position.ReadsABareWordAsLiteral, entry.Key);
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
		(string Subsuming, string Subsumed)[] expected =
		[
			("SH-004", "SH-011"), ("SH-004", "SH-016"), ("SH-004", "SH-017"), ("SH-016", "SH-017"), ("SH-016", "SH-011"), ("SH-011", "SH-017")
		];
		FrozenSet<SubsumptionRule> relation = ReadAnalyzer().SubsumptionRelation;

		Assert.Equal(expected.Length, relation.Count);
		Assert.All(relation, rule => Assert.Equal(SubsumptionCondition.AnchorPositionsAreIdentical, rule.When));
		Assert.Equal(
			expected.Order().ToArray(),
			relation.Select(rule => (rule.SubsumingPatternClassIdentifier, rule.SubsumedPatternClassIdentifier)).Order().ToArray());
	}

	[Fact]
	public void TheConstructPrecedenceRanksEveryClassOnceAndEndsAStatementAtTheListOperatorsButNotInASubstitution()
	{
		ConstructPrecedence precedence = ReadAnalyzer().ConstructPrecedence ?? throw new InvalidOperationException("No construct precedence is declared.");

		Assert.Equal(_everyClass.Order(StringComparer.Ordinal), precedence.PatternClassesInPrecedenceOrder.Order(StringComparer.Ordinal));
		Assert.Equal(new[] { ";", "&&", "||", "&" }.Order(StringComparer.Ordinal), precedence.StatementSeparators.Order(StringComparer.Ordinal));
		StatementNesting nesting = Assert.Single(precedence.StatementNestings);
		Assert.Equal("$(", nesting.Opening);
		Assert.Equal(")", nesting.Closing);
	}

	[Fact]
	public void TheClassOrderRanksTheOptionBeforeTheUrlAndTheHeaderBeforeTheEncoderInsideIt()
	{
		ImmutableArray<string> order = ReadAnalyzer().ConstructPrecedence!.PatternClassesInPrecedenceOrder;

		Assert.Equal("SH-001", order[0]);
		Assert.Equal("SH-012", order[^1]);
		Assert.True(order.IndexOf("SH-001") < order.IndexOf("SH-005"), "the option's pair ranks before the URL's userinfo");
		Assert.True(order.IndexOf("SH-003") < order.IndexOf("SH-007"), "the header ranks before the encoder inside it");
	}

	[Fact]
	public void TheElevenAnchorsAreInsensitiveAndTwoOfThemHoldABlank()
	{
		FrozenSet<PreselectionAnchor> anchors = ReadAnalyzer().CandidateSelectionAndRecommendation.PreselectionAnchors;

		Assert.Equal(_ANCHOR_COUNT, anchors.Count);
		Assert.All(anchors, anchor => Assert.Equal(CaseSensitivity.Insensitive, anchor.Sensitivity));
		Assert.Equal(
			new[] { "--auth", "://", "Authorization", "aria2c", "base64", "curl", "http -a", "https -a", "lwp-request", "netrc", "wget" }.Order(StringComparer.Ordinal),
			anchors.Select(anchor => anchor.Text).Order(StringComparer.Ordinal));
	}

	[Fact]
	public void OnlyTheEncodedPairRuleDeclaresAVariableUseBecauseTheOthersReadTheirUseInsideTheirOwnMatch()
	{
		ILanguageAnalyzer analyzer = ReadAnalyzer();

		Assert.Equal(["SH-007"], analyzer.DetectionRulesByPatternClass.Where(entry => entry.Value.VariableUse is not null).Select(entry => entry.Key));
		Assert.Equal("assigned", analyzer.DetectionRulesByPatternClass["SH-007"].VariableUse!.VariableGroupName);
		Assert.Contains("assigned", analyzer.DetectionRulesByPatternClass["SH-007"].Expression.GetGroupNames());
	}

	[Fact]
	public void TheAnalyzerDeclaresTheSameFileWrapperTrackingAndTheDialects()
	{
		CandidateSelectionAndRecommendation element = ReadAnalyzer().CandidateSelectionAndRecommendation;

		Assert.NotNull(element.WrapperTracking);
		Assert.NotNull(element.FileDialects);
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
		ShellRecognitionHarness harness = new();

		foreach ((string patternClass, _, string source) in ClassFixtures())
		{
			FileRecognitionContext context = harness.ContextFor(source);
			RecognizedConstruct construct = harness.Recognize(source).First(candidate => candidate.PatternClassIdentifier == patternClass);

			Assert.Equal(LexicalZone.Code, context.Mask.ZoneAt(construct.AnchorOffset));
		}
	}

	// ------------------------------------------------------------------------------------------
	// The near-misses: kept out, or Low
	// ------------------------------------------------------------------------------------------

	[Theory]
	[InlineData("set -u\n")]
	[InlineData("sort -u names.txt\n")]
	[InlineData("sudo -u deploy ./run.sh\n")]
	[InlineData("useradd -u 1001 svc\n")]
	[InlineData("mysql -u admin -pexample db\n")]
	[InlineData("ps -u admin\n")]
	[InlineData("uniq -u names.txt\n")]
	[InlineData("docker login -u admin -p example registry.example\n")]
	[InlineData("docker login --password-stdin registry.example\n")]
	[InlineData("kubectl create secret generic s --password=example\n")]
	[InlineData("helm repo add r https://charts.example --username admin --password example\n")]
	[InlineData("az login -u admin -p example\n")]
	[InlineData("curl -U proxyuser:proxysecret https://h.test/x\n")]
	[InlineData("curl --proxy-user proxyuser:proxysecret https://h.test/x\n")]
	[InlineData("curl --proxy-basic -x http://proxy.test:3128 https://h.test/x\n")]
	[InlineData("wget --proxy-user=proxyuser --proxy-password=proxysecret https://h.test/x\n")]
	[InlineData("ssh -l admin host.test\n")]
	[InlineData("sshpass -p example ssh admin@host.test\n")]
	[InlineData("scp admin@host.test:/tmp/f .\n")]
	[InlineData("curl --digest -u admin:example https://h.test/x\n")]
	[InlineData("curl --ntlm -u admin:example https://h.test/x\n")]
	[InlineData("curl --negotiate -u : https://h.test/x\n")]
	[InlineData("curl --aws-sigv4 \"aws:amz:us-east-1:s3\" --user \"$KEY:$SECRET\" \"$URL\"\n")]
	[InlineData("curl -u admin:example ftp://h.test/f\n")]
	[InlineData("curl -u admin:example sftp://h.test/f\n")]
	[InlineData("curl -H \"Authorization: Bearer abcdefgh\" https://h.test/x\n")]
	[InlineData("curl --user-agent \"tool 1.0\" https://h.test/x\n")]
	[InlineData("curl -A \"tool 1.0\" https://h.test/x\n")]
	[InlineData("git clone https://admin:example@h.test/repo.git\n")]
	[InlineData("# Basic setup of the tool\n")]
	[InlineData("echo \"Basic setup of the tool\"\n")]
	[InlineData("nc -l 8080 <<EOF\nHTTP/1.1 401 Unauthorized\nWWW-Authenticate: Basic realm=\"x\"\nEOF\n")]
	[InlineData("printf 'GET /x HTTP/1.1\\r\\nAuthorization: Basic dXNlcjpwYXNz\\r\\n\\r\\n' | nc -l 8080\n")]
	[InlineData("grep -i '^Authorization: Basic' request.log\n")]
	[InlineData("case \"$HTTP_AUTHORIZATION\" in \"Basic \"*) ;; esac\n")]
	[InlineData("http -A bearer -a \"$TOKEN\" \"$HOST\"\n")]
	[InlineData("http -A digest -a admin:example \"$HOST\"\n")]
	[InlineData("aria2c --http-proxy-user=u --http-proxy-passwd=p \"$URL\"\n")]
	[InlineData("base64 image.png\n")]
	[InlineData("echo \"$x\" | base64\n")]
	[InlineData("printf '%s:%s' \"$HOST\" \"$PORT\" | base64\n")]
	[InlineData("echo -n \"a:b\" | base64 -d\n")]
	[InlineData("openssl dgst -sha256 f | base64\n")]
	public void ANearMissIsKeptOutOrRecognizedBelowHighAndMedium(string source)
	{
		ImmutableArray<RecognizedConstruct> constructs = new ShellRecognitionHarness().Recognize(source);

		Assert.True(
			constructs.All(construct => construct.Level is ConfidenceLevel.Low),
			$"[{source}] produced [{Describe(constructs)}]");
	}

	[Theory]
	[InlineData("set -u\n")]
	[InlineData("curl --digest -u admin:example https://h.test/x\n")]
	[InlineData("curl -u admin:example ftp://h.test/f\n")]
	[InlineData("curl -H \"Authorization: Bearer abcdefgh\" https://h.test/x\n")]
	[InlineData("git clone https://admin:example@h.test/repo.git\n")]
	[InlineData("echo \"Basic setup of the tool\"\n")]
	[InlineData("http -A digest -a admin:example \"$HOST\"\n")]
	public void ANearMissThatTheCatalogKeepsOutProducesNoConstructAtAll(string source)
	{
		AssertSilent(source);
	}

	[Fact]
	public void AnAnyauthCallWithAUserIsReadAtLowByItsOwnClassAndNotAtHighByTheUserOptionClass()
	{
		const string source = "curl --anyauth -u admin:example \"$URL\"\n";

		AssertFires(source, "SH-023", ConfidenceLevel.Low);
		AssertNeverFires(source, "SH-001");
	}

	[Fact]
	public void AnAria2cCallUnderItsChallengeSwitchIsReadAtLowAndNotAtHigh()
	{
		const string source = "aria2c --http-auth-challenge=true --http-user=u --http-passwd=example \"$URL\"\n";

		AssertFires(source, "SH-015", ConfidenceLevel.Low);
		AssertNeverFires(source, "SH-014");
	}

	[Theory]
	[InlineData("WGET=wget\n${WGET} --user=\"$U\" --password=\"$P\" \"$URL\"\n")]
	[InlineData("WGET=wget\n\"$WGET\" --user admin --password example \"$URL\"\n")]
	[InlineData("alias w=wget\nw --user=admin --password=example \"$URL\"\n")]
	public void TheAliasFormOfWgetIsReadAtLowThroughTheLongUserOptionBecauseWgetHasNoShortOne(string source)
	{
		AssertFires(source, "SH-022", ConfidenceLevel.Low);
		AssertNeverFires(source, "SH-021");
	}

	[Fact]
	public void AWgetCallWithACredentialAndNoPreemptiveSwitchIsReadAtLowAndOneWithTheSwitchAtHigh()
	{
		AssertFires("wget --user=u --password=example \"$URL\"\n", "SH-011", ConfidenceLevel.Low);
		AssertFires("wget --auth-no-challenge --user=u --password=example \"$URL\"\n", "SH-004", ConfidenceLevel.High);
		AssertNeverFires("wget --auth-no-challenge --user=u --password=example \"$URL\"\n", "SH-011");
	}

	[Fact]
	public void AnEncodedPairThatReachesNoClientAndAnAssignmentNoClientUsesAreNotRead()
	{
		AssertSilent("ENC=$(echo -n \"$U:$P\" | base64)\necho done\n");
		AssertSilent("URL=\"https://admin:example@h.test/api\"\ngit clone \"$URL\"\n");
		AssertSilent("AUTH=\"Basic dXNlcjpw\"\necho \"$AUTH\"\n");
	}

	// ------------------------------------------------------------------------------------------
	// One finding per command
	// ------------------------------------------------------------------------------------------

	[Fact]
	public void AUserOptionAndAUrlWithUserinfoInOneCommandAreOneFindingAtTheOption()
	{
		ImmutableArray<RecognizedConstruct> constructs = new ShellRecognitionHarness().Recognize("curl -u a:b https://c:d@h.test/x\n");

		RecognizedConstruct construct = Assert.Single(constructs);
		Assert.Equal("SH-001", construct.PatternClassIdentifier);
		Assert.Equal(ConfidenceLevel.High, construct.Level);
	}

	[Fact]
	public void AHeaderThatBuildsItsTokenInASubstitutionIsOneFindingAtTheHeaderOption()
	{
		const string source = "curl -sS -H \"Authorization: Basic $(printf '%s:%s' \"$U\" \"$P\" | base64)\" \"$URL\"\n";

		ImmutableArray<RecognizedConstruct> constructs = new ShellRecognitionHarness().Recognize(source);

		RecognizedConstruct construct = Assert.Single(constructs);
		Assert.Equal("SH-003", construct.PatternClassIdentifier);
	}

	[Fact]
	public void TwoCommandsOnTwoLinesAreTwoFindingsAndTwoCommandsAfterASeparatorAreTwoFindings()
	{
		ShellRecognitionHarness harness = new();

		Assert.Equal(2, harness.Recognize("curl -u a:b \"$U1\"\ncurl -u c:d \"$U2\"\n").Length);
		Assert.Equal(2, harness.Recognize("curl -u a:b \"$U1\"; curl -u c:d \"$U2\"\n").Length);
		Assert.Equal(2, harness.Recognize("curl -u a:b \"$U1\" && curl -u c:d \"$U2\"\n").Length);
	}

	// ------------------------------------------------------------------------------------------
	// The same-file wrapper function
	// ------------------------------------------------------------------------------------------

	[Fact]
	public void AWrapperThatBuildsItsCredentialFromAParameterIsEvidenceAtTheCallThatPassesOne()
	{
		const string source = "api_call() {\n\tcurl -sS -u \"$1\" \"$HOST/x\"\n}\napi_call admin:example\n";

		ImmutableArray<RecognizedConstruct> constructs = new ShellRecognitionHarness().Recognize(source);

		RecognizedConstruct construct = Assert.Single(constructs);
		Assert.Equal("SH-001", construct.PatternClassIdentifier);
		Assert.Equal(source.LastIndexOf("api_call", StringComparison.Ordinal), construct.AnchorOffset);
	}

	[Fact]
	public void AWrapperThatIsNeverCalledWithACredentialYieldsNothing()
	{
		AssertSilent("api_call() {\n\tcurl -sS -u \"$1\" \"$HOST/x\"\n}\n");
		AssertSilent("api_call() {\n\tcurl -sS -u \"$1\" \"$HOST/x\"\n}\napi_call \"$CREDENTIALS\"\n");
	}

	[Fact]
	public void AWrapperReachedThroughACopyOfItsParameterIsFollowed()
	{
		const string source = "api_call() {\n\tlocal pair=\"$1\"\n\tcurl -sS -u \"$pair\" \"$HOST/x\"\n}\napi_call admin:example\n";

		AssertFires(source, "SH-001", ConfidenceLevel.High);
	}

	// ------------------------------------------------------------------------------------------
	// What the engine reads of the file: dialects and executed strings
	// ------------------------------------------------------------------------------------------

	[Theory]
	[InlineData("eval \"curl -u admin:example $URL\"\n")]
	[InlineData("bash -c 'curl -u admin:example \"$URL\"'\n")]
	[InlineData("ssh host.test \"curl -u admin:example $URL\"\n")]
	public void AClientCommandInAStringThatAShellRunsIsRecognizedAsCode(string source)
	{
		Assert.Contains(new ShellRecognitionHarness().Recognize(source), construct => construct.PatternClassIdentifier == "SH-001");
	}

	// ------------------------------------------------------------------------------------------
	// Helpers
	// ------------------------------------------------------------------------------------------

	private static ILanguageAnalyzer ReadAnalyzer()
	{
		return ShellRegisteredExpressions.Analyzer;
	}

	private static ImmutableArray<string> ClassesAt(ILanguageAnalyzer analyzer, ConfidenceLevel level)
	{
		return [.. analyzer.DetectionRulesByPatternClass.Where(entry => entry.Value.BaseLevel == level).Select(entry => entry.Key).Order(StringComparer.Ordinal)];
	}

	private static void AssertFires(string source, string patternClass, ConfidenceLevel level)
	{
		ImmutableArray<RecognizedConstruct> constructs = new ShellRecognitionHarness().Recognize(source);

		Assert.True(
			constructs.Any(construct => construct.PatternClassIdentifier == patternClass && construct.Level == level),
			$"{patternClass} did not fire at {level} on [{source}]; got [{Describe(constructs)}]");
	}

	private static void AssertNeverFires(string source, string patternClass)
	{
		ImmutableArray<RecognizedConstruct> constructs = new ShellRecognitionHarness().Recognize(source);

		Assert.False(
			constructs.Any(construct => construct.PatternClassIdentifier == patternClass),
			$"{patternClass} fired on [{source}]; got [{Describe(constructs)}]");
	}

	private static void AssertSilent(string source)
	{
		ImmutableArray<RecognizedConstruct> constructs = new ShellRecognitionHarness().Recognize(source);

		Assert.True(constructs.IsEmpty, $"[{source}] produced [{Describe(constructs)}]");
	}

	private static string Describe(ImmutableArray<RecognizedConstruct> constructs)
	{
		return string.Join(", ", constructs.Select(construct => $"{construct.PatternClassIdentifier}:{construct.Level}"));
	}

	/// <summary>One invented fixture per class, with the level the catalog gives it. Shared with the shape tests.</summary>
	internal static ImmutableArray<(string PatternClass, ConfidenceLevel Level, string Source)> ClassFixtures()
	{
		return
		[
			("SH-001", ConfidenceLevel.High, "curl -sS -u admin:example \"$HOST/api\"\n"),
			("SH-002", ConfidenceLevel.High, "curl -u admin \"$HOST/api\"\n"),
			("SH-003", ConfidenceLevel.High, "curl -sS -H \"Authorization: Basic dXNlcjpwYXNz\" \"$HOST/api\"\n"),
			("SH-004", ConfidenceLevel.High, "wget --auth-no-challenge --user=u --password=example \"$HOST/api\"\n"),
			("SH-005", ConfidenceLevel.High, "curl https://admin:example@host.test/api\n"),
			("SH-006", ConfidenceLevel.Medium, "curl --netrc \"$HOST/api\"\n"),
			("SH-007", ConfidenceLevel.Medium, "ENC=$(printf '%s:%s' \"$U\" \"$P\" | base64 -w0)\ncurl -H \"Authorization: Basic $ENC\" \"$HOST/api\"\n"),
			("SH-008", ConfidenceLevel.Medium, "AUTH=\"Basic ${ENCODED}\"\ncurl -sS -H \"Authorization: ${AUTH}\" \"$HOST/api\"\n"),
			("SH-009", ConfidenceLevel.Medium, "options=(-sS -u \"$U:$P\")\ncurl \"${options[@]}\" \"$HOST/api\"\n"),
			("SH-010", ConfidenceLevel.Medium, "API_URL=\"https://${U}:${P}@host.test/api\"\ncurl -sS \"${API_URL}/x\"\n"),
			("SH-011", ConfidenceLevel.Low, "wget --user=u --password=example \"$HOST/api\"\n"),
			("SH-012", ConfidenceLevel.Low, "build_basic_auth_header() {\n\tprintf 'Authorization: Basic %s' \"$(printf '%s:%s' \"$1\" \"$2\" | base64)\"\n}\n"),
			("SH-013", ConfidenceLevel.High, "http -a admin:example \"$HOST/api\"\n"),
			("SH-014", ConfidenceLevel.High, "aria2c --http-user=u --http-passwd=example \"$URL\"\n"),
			("SH-015", ConfidenceLevel.Low, "lwp-request -C admin:example \"$URL\"\n"),
			("SH-016", ConfidenceLevel.High, "wget --auth-no-challenge -e http_user=u -e http_password=example \"$URL\"\n"),
			("SH-017", ConfidenceLevel.Low, "wget -e http_user=u -e http_password=example \"$URL\"\n"),
			("SH-018", ConfidenceLevel.Medium, "exec 3<>/dev/tcp/host.test/80\nprintf 'GET /x HTTP/1.1\\r\\nAuthorization: Basic dXNlcjpwYXNz\\r\\n\\r\\n' >&3\n"),
			("SH-019", ConfidenceLevel.Medium, "cat > \"$cfg\" <<EOF\nuser = \"${U}:${P}\"\nEOF\ncurl -K \"$cfg\" \"$URL\"\n"),
			("SH-020", ConfidenceLevel.Low, "printf 'http_password = %s\\n' \"$P\" > \"$rc\"\nexport WGETRC=\"$rc\"\n"),
			("SH-021", ConfidenceLevel.Medium, "CURL=curl\n$CURL -sS -u \"$U:$P\" \"$URL\"\n"),
			("SH-022", ConfidenceLevel.Low, "WGET=wget\n${WGET} --user=\"$U\" --password=\"$P\" \"$URL\"\n"),
			("SH-023", ConfidenceLevel.Low, "curl --anyauth -u admin:example \"$URL\"\n"),
			("SH-024", ConfidenceLevel.Medium, "pair=\"${U}:${P}\"\nENC=$(printf '%s' \"$pair\" | base64 | tr -d '\\n')\ncurl -H \"Authorization: Basic $ENC\" \"$URL\"\n"),
			("SH-025", ConfidenceLevel.Medium, "CURL_OPTIONS=\"-sS -u ${U}:${P}\"\ncurl $CURL_OPTIONS \"$URL\"\n")
		];
	}
}
