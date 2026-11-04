using System.Collections.Immutable;
using System.Linq;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Recognition;
using MigrateAuth.Cli.Tests.Calibration;
using MigrateAuth.Cli.Tests.Recognition;

using Xunit;

namespace MigrateAuth.Cli.Tests.Catalog;

/// <summary>
/// The `CSharp` analyzer's detection of indexer arguments, through the real embedded analyzer and the real
/// recognition engine, together with the header enumeration's <c>Add</c> and <c>Set</c> writes, a
/// colon-joined pair written inside the encoder call, the credential cache's variable address and
/// four-argument overload, the legacy client's constructed first argument, and the cache entry's
/// subsumption of the bare credential construction. Each form has a positive fixture and a near-miss that
/// must not match. Every fixture is invented, with generic names only.
/// </summary>
public sealed class CSharpIndexerAndInlinePairTests
{
	private const string _CS_001 = "CS-001";
	private const string _CS_002 = "CS-002";
	private const string _CS_003 = "CS-003";
	private const string _CS_005 = "CS-005";
	private const string _CS_006 = "CS-006";
	private const string _CS_008 = "CS-008";
	private const string _CS_009 = "CS-009";
	private const string _CS_012 = "CS-012";
	private const string _CS_018 = "CS-018";
	private const int _MAXIMUM_INDEXER_CONTENT_LENGTH = 120;
	private const int _MAXIMUM_ENCODER_WINDOW_LENGTH = 200;
	private const int _MAXIMUM_ENUMERATION_ITERATIONS = 100_000;

	private static readonly ImmutableArray<string> _CLASSES_CHANGED_BY_THE_SHARED_ARGUMENT_FORM =
	[
		"CS-001", "CS-002", "CS-003", "CS-004", "CS-005", "CS-006", "CS-007", "CS-008",
		"CS-012", "CS-013", "CS-014", "CS-015", "CS-016", "CS-017", "CS-018", "CS-019"
	];

	private static readonly ImmutableArray<string> _SHAPES_CARRYING_THE_CHANGED_EXPRESSIONS =
	[
		"every-class-at-match-density",
		"open-proximity-windows",
		"indexer-and-inline-pair-near-misses"
	];

	// ------------------------------------------------------------------------------------------
	// Indexer arguments.
	// ------------------------------------------------------------------------------------------

	[Fact]
	public void CS001ReadsAConfigurationIndexerPairAtHighWithNoLiteralCredentialSignal()
	{
		const string source = "var credentials = new UsernamePasswordCredentials(ConfigurationManager.AppSettings[\"User\"], ConfigurationManager.AppSettings[\"Password\"]);";

		RecognizedConstruct construct = Assert.Single(new CSharpRecognitionHarness().Recognize(source));

		Assert.Equal(_CS_001, construct.PatternClassIdentifier);
		Assert.Equal(ConfidenceLevel.High, construct.Level);
		Assert.False(construct.LiteralCredentialSignal);
	}

	[Fact]
	public void CS001StillSetsTheLiteralCredentialSignalForALiteralPair()
	{
		const string source = "var credentials = new UsernamePasswordCredentials(\"invented-user\", \"invented-secret\");";

		RecognizedConstruct construct = Assert.Single(new CSharpRecognitionHarness().Recognize(source));

		Assert.Equal(_CS_001, construct.PatternClassIdentifier);
		Assert.True(construct.LiteralCredentialSignal);
	}

	[Fact]
	public void CS008ReadsAPairReadFromConfigurationSections()
	{
		const string source = "var credential = new NetworkCredential(configuration[\"Section:User\"], configuration[\"Section:Password\"]);";

		RecognizedConstruct construct = Assert.Single(new CSharpRecognitionHarness().Recognize(source));

		Assert.Equal(_CS_008, construct.PatternClassIdentifier);
		Assert.False(construct.LiteralCredentialSignal);
	}

	[Fact]
	public void CS008ReadsTwoBracketSuffixesAndAnExpressionIndex()
	{
		const string source = "var credential = new NetworkCredential(values[0][1], parts[index + 1]);";

		RecognizedConstruct construct = Assert.Single(new CSharpRecognitionHarness().Recognize(source));

		Assert.Equal(_CS_008, construct.PatternClassIdentifier);
	}

	[Fact]
	public void CS005ReadsACredentialSplitFromAString()
	{
		const string source = "request.Headers.Authorization = new AuthenticationHeaderValue(\"Basic\", parts[1]);";

		AssertSingleConstruct(source, _CS_005, ConfidenceLevel.Medium);
	}

	[Theory]
	[InlineData("var credential = new NetworkCredential(values[keys[0]], password);")]
	[InlineData("var credential = new NetworkCredential(values[keys[0]], values[keys[1]]);")]
	[InlineData("var credential = new NetworkCredential(parts[1, 2], password);")]
	[InlineData("var credential = new NetworkCredential([userName], [password]);")]
	[InlineData("var credential = new NetworkCredential(userName, values['k']);")]
	[InlineData("var credential = new NetworkCredential(userName, values[first; second]);")]
	[InlineData("var credential = new NetworkCredential(userName, values[index)]);")]
	[InlineData("var credential = new NetworkCredential(userName, values[0][1][2]);")]
	[InlineData("var credential = new NetworkCredential(userName, values [0]);")]
	public void AnIndexerNearMissIsNotRead(string source)
	{
		Assert.Empty(new CSharpRecognitionHarness().Recognize(source));
	}

	[Fact]
	public void AnIndexerLongerThanTheBoundIsNotReadAndOneAtTheBoundIs()
	{
		string atTheBound = "var credential = new NetworkCredential(userName, values[" + new string('a', _MAXIMUM_INDEXER_CONTENT_LENGTH) + "]);";
		string pastTheBound = "var credential = new NetworkCredential(userName, values[" + new string('a', _MAXIMUM_INDEXER_CONTENT_LENGTH + 1) + "]);";

		Assert.Single(new CSharpRecognitionHarness().Recognize(atTheBound));
		Assert.Empty(new CSharpRecognitionHarness().Recognize(pastTheBound));
	}

	[Theory]
	[InlineData("var authenticator = new HttpBasicAuthenticator(settings[\"User\"], settings[\"Password\"]);", "CS-004")]
	[InlineData("var handler = new HttpClientHandler { Credentials = new NetworkCredential(settings[\"User\"], settings[\"Password\"]) };", "CS-007")]
	[InlineData("var result = client.WithBasicAuth(settings[\"User\"], settings[\"Password\"]);", "CS-014")]
	[InlineData("var importApi = new ImportAPI(settings[\"User\"], settings[\"Password\"], webServiceUrl);", "CS-015")]
	[InlineData("importJob.Settings.RelativityUsername = settings[\"User\"];\nimportJob.Settings.RelativityPassword = settings[\"Password\"];", "CS-017")]
	[InlineData("NetworkCredential assembled = new NetworkCredential { UserName = settings[\"User\"] };\nassembled.Password = settings[\"Password\"];", "CS-016")]
	[InlineData("binding.Security.Transport.ClientCredentialType = HttpClientCredentialType.Basic;\nclient.ClientCredentials.UserName.Password = settings[\"Password\"];", "CS-013")]
	[InlineData("client.DefaultRequestHeaders.Add(\"Authorization\", \"Basic \" + settings[\"Token\"]);", "CS-006")]
	[InlineData("client.Headers[HttpRequestHeader.Authorization] = \"Basic \" + settings[\"Token\"];", "CS-012")]
	[InlineData("var sink = new SmtpClient(host, port);\nsink.Credentials = new NetworkCredential(settings[\"User\"], settings[\"Password\"]);", "CS-019")]
	[InlineData("var client = new RSAPIClient(rsapiUri, new UsernamePasswordCredentials(settings[\"User\"], settings[\"Password\"]));", "CS-002")]
	[InlineData("cache.Add(new Uri(instanceUrl), \"Basic\", new NetworkCredential(settings[\"User\"], settings[\"Password\"]));", "CS-003")]
	[InlineData("string tokenPair = \"\" + \":\" + settings[\"Token\"];\nstring tokenEncoded = Convert.ToBase64String(Encoding.ASCII.GetBytes(tokenPair));", "CS-018")]
	public void EveryClassThatCapturesAnArgumentThroughTheSharedFormReadsAnIndexer(string source, string patternClass)
	{
		ImmutableArray<RecognizedConstruct> recognized = new CSharpRecognitionHarness().Recognize(source);

		Assert.Contains(recognized, construct => construct.PatternClassIdentifier == patternClass);
		Assert.All(recognized.Where(construct => construct.PatternClassIdentifier == patternClass), construct => Assert.False(construct.LiteralCredentialSignal));
	}

	// ------------------------------------------------------------------------------------------
	// CS-012 through Headers.Add and Headers.Set with the header enumeration.
	// ------------------------------------------------------------------------------------------

	[Theory]
	[InlineData("request.Headers.Add(HttpRequestHeader.Authorization, \"Basic \" + encoded);")]
	[InlineData("request.Headers.Set(HttpRequestHeader.Authorization, \"Basic \" + encoded);")]
	[InlineData("client.Headers.Add(HttpRequestHeader.Authorization, \"Basic \" + parts[1]);")]
	[InlineData("client.Headers.Add(HttpRequestHeader.Authorization, \"Basic ZGVtbzpodW50ZXIy\");")]
	[InlineData("client.Headers.Add(HttpRequestHeader.Authorization, \"Basic abc\" + suffix);")]
	[InlineData("client.Headers.Set(HttpRequestHeader.Authorization, CreateBasicValue(userName, password));")]
	public void CS012ReadsTheHeaderEnumerationThroughAddAndSet(string source)
	{
		AssertSingleConstruct(source, _CS_012, ConfidenceLevel.Medium);
	}

	[Fact]
	public void CS012ReadsANamedConstantSchemeThroughHeadersAdd()
	{
		const string source = "private const string Scheme = \"Basic\";\nclient.Headers.Add(HttpRequestHeader.Authorization, Scheme + \" \" + encoded);";

		AssertSingleConstruct(source, _CS_012, ConfidenceLevel.Medium);
	}

	[Theory]
	[InlineData("client.Headers.Add(HttpRequestHeader.ContentType, \"Basic \" + encoded);")]
	[InlineData("var current = client.Headers.Get(HttpRequestHeader.Authorization);")]
	[InlineData("client.Headers.Add(HttpRequestHeader.Authorization, \"Bearer \" + token);")]
	[InlineData("client.Headers.Set(HttpRequestHeader.Authorization, \"Bearer \" + token);")]
	[InlineData("client.Headers.Remove(HttpRequestHeader.Authorization, \"Basic \" + encoded);")]
	public void CS012DoesNotReadANearMissOfTheHeaderEnumerationWrite(string source)
	{
		Assert.Empty(new CSharpRecognitionHarness().Recognize(source));
	}

	[Fact]
	public void TheStringNamedHeadersAddStaysCS006AndNeverReachesCS012()
	{
		const string source = "client.Headers.Add(\"Authorization\", \"Basic \" + encoded);";

		AssertSingleConstruct(source, _CS_006, ConfidenceLevel.Medium);
	}

	// ------------------------------------------------------------------------------------------
	// CS-009 with the pair written inside the encoder call.
	// ------------------------------------------------------------------------------------------

	[Theory]
	[InlineData("string value = Convert.ToBase64String(Encoding.ASCII.GetBytes(userName + \":\" + password));")]
	[InlineData("string value = Convert.ToBase64String(Encoding.ASCII.GetBytes($\"{userName}:{password}\"));")]
	[InlineData("string value = Convert.ToBase64String(Encoding.ASCII.GetBytes(string.Format(\"{0}:{1}\", userName, password)));")]
	[InlineData("string value = Convert.ToBase64String(Encoding.ASCII.GetBytes(String.Join(\":\", userName, password)));")]
	public void CS009ReadsANonEmptyPairInsideTheEncoderCallInEachSpelling(string source)
	{
		AssertSingleConstruct(source, _CS_009, ConfidenceLevel.Medium);
	}

	[Theory]
	[InlineData("string value = Convert.ToBase64String(Encoding.ASCII.GetBytes(\"\" + \":\" + token));")]
	[InlineData("string value = Convert.ToBase64String(Encoding.UTF8.GetBytes($\"{\"\"}:{token}\"));")]
	public void AnEmptyHalfInsideTheEncoderCallStaysCS018AndNeverCS009(string source)
	{
		AssertSingleConstruct(source, _CS_018, ConfidenceLevel.Medium);
	}

	[Theory]
	[InlineData("string value = Convert.ToBase64String(Encoding.ASCII.GetBytes(userName));")]
	[InlineData("string value = Convert.ToBase64String(bytes); string joined = userName + \":\" + password;")]
	[InlineData("string value = Convert.ToBase64String(Encoding.ASCII.GetBytes(userName + password));")]
	[InlineData("string value = Convert.ToBase64String(Encoding.ASCII.GetBytes(string.Format(\"{0}-{1}\", userName, password)));")]
	public void CS009DoesNotReadANearMissOfTheInlinePair(string source)
	{
		Assert.Empty(new CSharpRecognitionHarness().Recognize(source));
	}

	[Fact]
	public void CS009DoesNotReadAPairBeyondTwoHundredCharactersOfTheEncoderCall()
	{
		string filler = new('x', _MAXIMUM_ENCODER_WINDOW_LENGTH + 1);
		string source = "string value = Convert.ToBase64String(" + filler + "(userName + \":\" + password));";

		Assert.Empty(new CSharpRecognitionHarness().Recognize(source));
	}

	[Fact]
	public void CS009StillReadsThePairAssignedToAVariableFirst()
	{
		const string source = "string pair = userName + \":\" + password;\nstring value = Convert.ToBase64String(Encoding.ASCII.GetBytes(pair));";

		AssertSingleConstruct(source, _CS_009, ConfidenceLevel.Medium);
	}

	// ------------------------------------------------------------------------------------------
	// CS-003 and CS-002 first arguments, and the four-argument cache entry.
	// ------------------------------------------------------------------------------------------

	[Theory]
	[InlineData("cache.Add(endpoint, \"Basic\", new NetworkCredential(userName, password));")]
	[InlineData("cache.Add(settings[\"Host\"], \"Basic\", new NetworkCredential(userName, password));")]
	[InlineData("cache.Add(new Uri(instanceUrl), \"Basic\", new NetworkCredential(userName, password));")]
	[InlineData("cache.Add(host, port, \"Basic\", new NetworkCredential(userName, password));")]
	[InlineData("cache.Add(settings[\"Host\"], ports[0], \"Basic\", new NetworkCredential(settings[\"User\"], settings[\"Password\"]));")]
	public void CS003ReadsAVariableAddressAndTheFourArgumentOverloadAsOneFinding(string source)
	{
		AssertSingleConstruct(source, _CS_003, ConfidenceLevel.High);
	}

	[Theory]
	[InlineData("cache.Add(endpoint, \"Digest\", new NetworkCredential(userName, password));")]
	[InlineData("cache.Add(host, port, \"Digest\", new NetworkCredential(userName, password));")]
	public void CS003DoesNotReportASchemeOtherThanBasic(string source)
	{
		Assert.DoesNotContain(new CSharpRecognitionHarness().Recognize(source), construct => construct.PatternClassIdentifier == _CS_003);
	}

	[Theory]
	[InlineData("cache.Add(endpoint, \"Basic\", credential);")]
	[InlineData("cache.Add(host, port, \"Basic\", credential);")]
	[InlineData("cache.Add(host, port, extra, \"Basic\", new NetworkCredential(userName, password));")]
	public void CS003DoesNotMatchWhenTheCredentialIsNotConstructedInTheCall(string source)
	{
		Assert.DoesNotContain(new CSharpRecognitionHarness().Recognize(source), construct => construct.PatternClassIdentifier == _CS_003);
	}

	[Fact]
	public void CS002ClassifiesAnInlineAddressConstructionAsCS002AndNotCS001()
	{
		const string source = "var client = new RSAPIClient(new Uri(\"https://invented.example\"), new UsernamePasswordCredentials(userName, password));";

		AssertSingleConstruct(source, _CS_002, ConfidenceLevel.High);
	}

	[Fact]
	public void CS002ReadsAConstructionWithOneFurtherNestedLevel()
	{
		const string source = "var client = new RSAPIClient(new Uri(BuildAddress(host)), new UsernamePasswordCredentials(userName, password));";

		AssertSingleConstruct(source, _CS_002, ConfidenceLevel.High);
	}

	[Fact]
	public void CS002ReadsAnIndexerFirstArgument()
	{
		const string source = "var client = new RSAPIClient(settings[\"Address\"], new UsernamePasswordCredentials(userName, password));";

		AssertSingleConstruct(source, _CS_002, ConfidenceLevel.High);
	}

	[Fact]
	public void CS002StillDeclinesWhenTheSecondArgumentIsNotTheNestedCredentialType()
	{
		const string source = "var client = new RSAPIClient(uri, bearerCredentials);";

		Assert.Empty(new CSharpRecognitionHarness().Recognize(source));
	}

	[Fact]
	public void CS002DoesNotReadAConstructionNestedDeeperThanOneLevel()
	{
		const string source = "var client = new RSAPIClient(new Uri(Outer(Middle(Inner(host)))), new UsernamePasswordCredentials(userName, password));";

		AssertSingleConstruct(source, _CS_001, ConfidenceLevel.High);
	}

	// ------------------------------------------------------------------------------------------
	// CS-003 subsumes CS-008.
	// ------------------------------------------------------------------------------------------

	[Fact]
	public void TheAnalyzerDeclaresCS003AsSubsumingCS008AtOverlappingSpans()
	{
		ILanguageAnalyzer analyzer = new CSharpRecognitionHarness().Analyzer;

		Assert.Contains(
			analyzer.SubsumptionRelation,
			rule => rule.SubsumingPatternClassIdentifier == _CS_003
				&& rule.SubsumedPatternClassIdentifier == _CS_008
				&& rule.When == SubsumptionCondition.MatchSpansOverlap);
	}

	[Fact]
	public void ACredentialConstructedInAnotherStatementAndPassedByVariableStillYieldsCS008()
	{
		const string source = "var credential = new NetworkCredential(userName, password);\ncache.Add(endpoint, \"Basic\", credential);";

		AssertSingleConstruct(source, _CS_008, ConfidenceLevel.Medium);
	}

	[Fact]
	public void ACredentialConstructedElsewhereInTheFileStillYieldsCS008BesideTheCacheEntry()
	{
		const string source = "cache.Add(endpoint, \"Basic\", new NetworkCredential(userName, password));\nvar other = new NetworkCredential(otherUser, otherPassword);";

		ImmutableArray<RecognizedConstruct> recognized = new CSharpRecognitionHarness().Recognize(source);

		Assert.Equal(2, recognized.Length);
		Assert.Contains(recognized, construct => construct.PatternClassIdentifier == _CS_003);
		Assert.Contains(recognized, construct => construct.PatternClassIdentifier == _CS_008);
	}

	// ------------------------------------------------------------------------------------------
	// The source-generated and the interpreted form of every expression under test read alike, and
	// every expression under test terminates over the near-miss shapes.
	// ------------------------------------------------------------------------------------------

	[Fact]
	public void TheGeneratedAndTheInterpretedFormOfEveryChangedExpressionAgreeOverTheStressShapes()
	{
		CSharpRecognitionHarness harness = new();
		ImmutableArray<string> patternClasses = [.. _CLASSES_CHANGED_BY_THE_SHARED_ARGUMENT_FORM, "CS-009"];

		foreach (string patternClass in patternClasses)
		{
			DetectionRule rule = harness.Analyzer.DetectionRulesByPatternClass[patternClass];
			System.Text.RegularExpressions.Regex interpreted = new(rule.Expression.ToString(), rule.Expression.Options, System.Text.RegularExpressions.Regex.InfiniteMatchTimeout);

			foreach (string shapeName in _SHAPES_CARRYING_THE_CHANGED_EXPRESSIONS)
			{
				string text = RecognitionHarness.Normalize(MatchBoundStressSet.Build().Single(shape => shape.Name == shapeName).Text);

				ImmutableArray<(int Index, int Length)> expected = SummarizeMatches(interpreted, text);
				ImmutableArray<(int Index, int Length)> actual = SummarizeMatches(rule.Expression, text);
				int firstDifference = Enumerable.Range(0, Math.Min(expected.Length, actual.Length)).Where(index => expected[index] != actual[index]).DefaultIfEmpty(-1).First();
				Assert.True(expected.Length == actual.Length && firstDifference < 0, $"{patternClass} over {shapeName}: {expected.Length} vs {actual.Length}, first difference at {firstDifference}");
			}
		}
	}

	[Fact]
	public void TheGeneratedAndTheInterpretedFormAgreeOverTheNewShapesWrittenInPlain()
	{
		const string text = "cache.Add(host, port, \"Basic\", new NetworkCredential(settings[\"User\"], values[0][1]));\n"
			+ "request.Headers.Set(HttpRequestHeader.Authorization, \"Basic \" + parts[1]);\n"
			+ "string v = Convert.ToBase64String(Encoding.ASCII.GetBytes(string.Join(\":\", userName, password)));\n"
			+ "var c = new RSAPIClient(new Uri(BuildAddress(host)), new UsernamePasswordCredentials(a[\"u\"], a[\"p\"]));\n";
		CSharpRecognitionHarness harness = new();
		ImmutableArray<string> patternClasses = ["CS-002", "CS-003", "CS-008", "CS-009", "CS-012"];

		foreach (string patternClass in patternClasses)
		{
			DetectionRule rule = harness.Analyzer.DetectionRulesByPatternClass[patternClass];
			System.Text.RegularExpressions.Regex interpreted = new(rule.Expression.ToString(), rule.Expression.Options, System.Text.RegularExpressions.Regex.InfiniteMatchTimeout);

			ImmutableArray<(int Index, int Length)> expected = SummarizeMatches(interpreted, text);

			Assert.NotEmpty(expected);
			Assert.True(expected.SequenceEqual(SummarizeMatches(rule.Expression, text)), patternClass);
		}
	}

	// ------------------------------------------------------------------------------------------
	// helpers
	// ------------------------------------------------------------------------------------------

	private static ImmutableArray<(int Index, int Length)> SummarizeMatches(System.Text.RegularExpressions.Regex expression, string text)
	{
		List<(int Index, int Length)> summary = [];
		System.Text.RegularExpressions.Match match = expression.Match(text);

		while (match.Success && summary.Count < _MAXIMUM_ENUMERATION_ITERATIONS)
		{
			summary.Add((match.Index, match.Length));
			match = match.NextMatch();
		}

		return [.. summary];
	}

	private static void AssertSingleConstruct(string source, string patternClassIdentifier, ConfidenceLevel level)
	{
		RecognizedConstruct construct = Assert.Single(new CSharpRecognitionHarness().Recognize(source));

		Assert.Equal(patternClassIdentifier, construct.PatternClassIdentifier);
		Assert.Equal(level, construct.Level);
	}
}
