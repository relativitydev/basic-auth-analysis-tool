using System.Collections.Immutable;
using System.Linq;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Recognition;
using MigrateAuth.Cli.Tests.Calibration;
using MigrateAuth.Cli.Tests.Recognition;

using Xunit;

namespace MigrateAuth.Cli.Tests.Catalog;

/// <summary>
/// The `CSharp` analyzer's detection of further header, encoder, SDK and non-HTTP sink forms, through the
/// real embedded analyzer and the real recognition engine. Each form has a positive fixture
/// and a near-miss that must not match. Every fixture is invented, with generic names only.
/// </summary>
public sealed class CSharpDetectionGapTests
{
	private const string _CS_006 = "CS-006";
	private const string _CS_012 = "CS-012";
	private const string _CS_015 = "CS-015";
	private const string _CS_016 = "CS-016";
	private const string _CS_018 = "CS-018";
	private const string _CS_019 = "CS-019";
	private const string _CONSTRUCT_CAPTURE_GROUP_NAME = "construct";
	private const string _TEST_FILE_RELATIVE_PATH = "src/Sample.Tests/Sample.cs";
	private const int _FILLER_LINE_COUNT_BEYOND_THE_WINDOW = 9;
	private const int _MANY_REPEATED_STATEMENTS = 500;
	private const int _MANY_FILLER_LINES = 4000;
	private const int _MAXIMUM_ENUMERATION_ITERATIONS = 10_000;
	private const int _MATCH_COUNT_HEADROOM = 10;

	private static readonly ImmutableArray<string> _SHAPES_CARRYING_THE_CHANGED_WINDOWS =
	[
		"every-class-at-match-density",
		"open-proximity-windows"
	];

	// ------------------------------------------------------------------------------------------
	// CS-006 - a header value formatted with the scheme word, and a header value in a variable.
	// ------------------------------------------------------------------------------------------

	[Fact]
	public void CS006MatchesAFormattedSchemeValueBuiltAroundAnEncodingHelper()
	{
		const string source = """
			client.DefaultRequestHeaders.Add("Authorization", string.Format("Basic {0}", EncodingHelper.ToBase64(pair)));
			""";

		AssertSingleConstruct(source, _CS_006, ConfidenceLevel.Medium);
	}

	[Fact]
	public void CS006MatchesAFormattedSchemeValueAttachedThroughAVariableAndARequestAddHeader()
	{
		const string source = """
			var headerValue = string.Format("Basic {0}", EncodingHelper.ToBase64(pair));
			request.AddHeader("Authorization", headerValue);
			""";

		AssertSingleConstruct(source, _CS_006, ConfidenceLevel.Medium);
	}

	[Fact]
	public void CS006MatchesAnInterpolatedSchemeValueAttachedThroughAVariable()
	{
		const string source = """
			string headerValue = $"Basic {encoded}";
			client.DefaultRequestHeaders.Add("Authorization", headerValue);
			""";

		AssertSingleConstruct(source, _CS_006, ConfidenceLevel.Medium);
	}

	[Fact]
	public void CS006MatchesAConcatenatedSchemeValueAttachedThroughAVariable()
	{
		const string source = """
			string headerValue = "Basic " + encoded;
			client.DefaultRequestHeaders.Add("Authorization", headerValue);
			""";

		AssertSingleConstruct(source, _CS_006, ConfidenceLevel.Medium);
	}

	[Fact]
	public void CS006MatchesAConcatenatedSchemeValueWrittenThroughARequestAddHeader()
	{
		const string source = """
			request.AddHeader("Authorization", "Basic " + encoded);
			""";

		AssertSingleConstruct(source, _CS_006, ConfidenceLevel.Medium);
	}

	[Fact]
	public void CS006DoesNotMatchAFormattedValueWithAnotherScheme()
	{
		const string source = """
			request.AddHeader("Authorization", string.Format("Bearer {0}", token));
			""";

		Assert.Empty(new CSharpRecognitionHarness().Recognize(source));
	}

	[Fact]
	public void CS006DoesNotMatchAFormatStringThatIsNotASchemeFollowedByAPlaceholder()
	{
		const string source = """
			var message = string.Format("Basic auth failed for {0}", userName);
			request.AddHeader("Authorization", message);
			""";

		Assert.Empty(new CSharpRecognitionHarness().Recognize(source));
	}

	[Fact]
	public void CS006DoesNotMatchWhenTheHeaderWritePassesADifferentVariable()
	{
		const string source = """
			var headerValue = string.Format("Basic {0}", encoded);
			request.AddHeader("Authorization", otherValue);
			""";

		Assert.Empty(new CSharpRecognitionHarness().Recognize(source));
	}

	[Fact]
	public void CS006DoesNotMatchAVariableHeaderValueWrittenToAnotherHeader()
	{
		const string source = """
			var headerValue = string.Format("Basic {0}", encoded);
			request.AddHeader("Accept", headerValue);
			""";

		Assert.Empty(new CSharpRecognitionHarness().Recognize(source));
	}

	[Fact]
	public void CS006DoesNotMatchAVariableHeaderValueBeyondTheBoundedWindow()
	{
		string filler = string.Concat(Enumerable.Range(1, _FILLER_LINE_COUNT_BEYOND_THE_WINDOW).Select(index => $"var filler{index} = {index};\n"));
		string source = "var headerValue = string.Format(\"Basic {0}\", encoded);\n" + filler + "request.AddHeader(\"Authorization\", headerValue);";

		Assert.Empty(new CSharpRecognitionHarness().Recognize(source));
	}

	[Fact]
	public void CS006VariableWindowTerminatesWhenExhaustedAndOverThousandsOfLines()
	{
		string exhausted = "var headerValue = string.Format(\"Basic {0}\", encoded);\n"
			+ string.Concat(Enumerable.Range(1, _FILLER_LINE_COUNT_BEYOND_THE_WINDOW + 2).Select(index => $"var filler{index} = {index};\n"));
		string repeated = string.Concat(Enumerable.Range(1, _MANY_REPEATED_STATEMENTS).Select(index => $"var headerValue{index} = string.Format(\"Basic {{0}}\", encoded);\n"));
		string filler = string.Concat(Enumerable.Range(1, _MANY_FILLER_LINES).Select(index => $"var filler{index} = {index};\n"));

		AssertBoundedTermination(exhausted, _CS_006, "header");
		AssertBoundedTermination(repeated, _CS_006, "header");
		AssertBoundedTermination(filler, _CS_006, "header");
	}

	// ------------------------------------------------------------------------------------------
	// CS-018 - a token pair written inside the encoder call.
	// ------------------------------------------------------------------------------------------

	[Fact]
	public void CS018MatchesAnInterpolatedLeftEmptyPairInsideTheEncoderCall()
	{
		const string source = """
			var encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{""}:{accessToken}"));
			""";

		AssertSingleConstruct(source, _CS_018, ConfidenceLevel.Medium);
	}

	[Fact]
	public void CS018MatchesAnInterpolatedRightEmptyPairInsideTheEncoderCall()
	{
		const string source = """
			var encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{accessToken}:"));
			""";

		AssertSingleConstruct(source, _CS_018, ConfidenceLevel.Medium);
	}

	[Fact]
	public void CS018MatchesAConcatenatedLeftEmptyPairWithAnExplicitEmptyOperandInsideTheEncoderCall()
	{
		const string source = """
			var encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes("" + ":" + accessToken));
			""";

		AssertSingleConstruct(source, _CS_018, ConfidenceLevel.Medium);
	}

	[Fact]
	public void CS018MatchesAColonFirstConcatenatedPairInsideTheEncoderCall()
	{
		const string source = """
			var encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(":" + accessToken));
			""";

		AssertSingleConstruct(source, _CS_018, ConfidenceLevel.Medium);
	}

	[Fact]
	public void CS018MatchesAColonLastConcatenatedPairInsideTheEncoderCall()
	{
		const string source = """
			var encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(accessToken + ":"));
			""";

		AssertSingleConstruct(source, _CS_018, ConfidenceLevel.Medium);
	}

	[Fact]
	public void CS018MatchesTheInlinePairAndTheHeaderWrittenThroughARequestAddHeader()
	{
		const string source = """
			var encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{""}:{accessToken}"));
			request.AddHeader("Authorization", $"Basic {encoded}");
			""";

		ImmutableArray<RecognizedConstruct> recognized = new CSharpRecognitionHarness().Recognize(source);

		Assert.Equal(new[] { _CS_006, _CS_018 }, recognized.Select(construct => construct.PatternClassIdentifier).Order().ToArray());
		Assert.All(recognized, construct => Assert.Equal(ConfidenceLevel.Medium, construct.Level));
	}

	[Fact]
	public void CS018DoesNotMatchAPairWithBothHalvesNonEmptyInsideTheEncoderCall()
	{
		const string source = """
			var encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{userName}:{password}"));
			""";

		Assert.DoesNotContain(new CSharpRecognitionHarness().Recognize(source), construct => construct.PatternClassIdentifier == _CS_018);
	}

	[Fact]
	public void CS018DoesNotMatchAConcatenatedPairWithBothHalvesNonEmptyInsideTheEncoderCall()
	{
		const string source = """
			var encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(userName + ":" + password));
			""";

		Assert.DoesNotContain(new CSharpRecognitionHarness().Recognize(source), construct => construct.PatternClassIdentifier == _CS_018);
	}

	[Fact]
	public void CS018DoesNotMatchAnEncoderCallWithNoPairInIt()
	{
		const string source = """
			var encoded = Convert.ToBase64String(payloadBytes);
			""";

		Assert.Empty(new CSharpRecognitionHarness().Recognize(source));
	}

	[Fact]
	public void CS018DoesNotMatchAPairThatOnlyOpensInTheNextStatement()
	{
		const string source = """
			var encoded = Convert.ToBase64String(payloadBytes); var nextPair = $"{""}:{accessToken}";
			""";

		Assert.DoesNotContain(new CSharpRecognitionHarness().Recognize(source), construct => construct.PatternClassIdentifier == _CS_018);
	}

	[Fact]
	public void CS018InlineShapesTerminateOverRepeatedAndUnfinishedInput()
	{
		string repeated = string.Concat(Enumerable.Range(1, _MANY_REPEATED_STATEMENTS).Select(index => $"var encoded{index} = Convert.ToBase64String(Encoding.UTF8.GetBytes(first{index} + \":\" + second{index}));\n"));
		string unfinished = string.Concat(Enumerable.Range(1, _MANY_REPEATED_STATEMENTS).Select(index => $"var encoded{index} = Convert.ToBase64String(Encoding.UTF8.GetBytes(($\"{{\"\"}}\n"));

		AssertBoundedTermination(repeated, _CS_018, "encoder");
		AssertBoundedTermination(unfinished, _CS_018, "encoder");
	}

	// ------------------------------------------------------------------------------------------
	// CS-012 - a header-enum value produced by a helper named for the scheme.
	// ------------------------------------------------------------------------------------------

	[Fact]
	public void CS012MatchesAValueProducedByAHelperNamedForTheScheme()
	{
		const string source = """
			client.Headers[HttpRequestHeader.Authorization] = CreateBasicValue(userName, password);
			""";

		AssertSingleConstruct(source, _CS_012, ConfidenceLevel.Medium);
	}

	[Fact]
	public void CS012MatchesAValueProducedByAHelperNamedForTheSchemeThroughAMemberAccessChain()
	{
		const string source = """
			request.Headers[HttpRequestHeader.Authorization] = headerFactory.BuildBasicHeader(account);
			""";

		AssertSingleConstruct(source, _CS_012, ConfidenceLevel.Medium);
	}

	[Theory]
	[InlineData("CreateBearerValue(token)")]
	[InlineData("Basically(token)")]
	[InlineData("CreateHeaderValue(userName, password)")]
	[InlineData("BasicHeaderText")]
	public void CS012DoesNotMatchAValueThatIsNotACallToAHelperNamedForTheScheme(string value)
	{
		string source = $"client.Headers[HttpRequestHeader.Authorization] = {value};";

		Assert.Empty(new CSharpRecognitionHarness().Recognize(source));
	}

	[Fact]
	public void CS012DoesNotMatchAReadOfTheHeaderThroughAHelperNamedForTheScheme()
	{
		const string source = """
			var current = client.Headers[HttpRequestHeader.Authorization]; var value = CreateBasicValue(userName, password);
			""";

		Assert.Empty(new CSharpRecognitionHarness().Recognize(source));
	}

	// ------------------------------------------------------------------------------------------
	// CS-015 - the SDK constructor with its arguments spread over lines.
	// ------------------------------------------------------------------------------------------

	[Fact]
	public void CS015MatchesArgumentsSpreadOverLinesWithTrailingLineComments()
	{
		const string source = """
			var importApi = new ImportAPI(
				userName, // the operator
				password, // the secret
				webServiceUrl // the service
			);
			""";

		AssertSingleConstruct(source, _CS_015, ConfidenceLevel.High);
	}

	[Fact]
	public void CS015MatchesArgumentsSpreadOverLinesWithBlockCommentsInATestFileAtLow()
	{
		const string source = """
			var importApi = new ImportAPI(
				/* operator */ userName,
				/* secret */ password,
				/* service */ webServiceUrl);
			""";

		RecognizedConstruct construct = Assert.Single(new CSharpRecognitionHarness().Recognize(source, _TEST_FILE_RELATIVE_PATH));

		Assert.Equal(_CS_015, construct.PatternClassIdentifier);
		Assert.Equal(ConfidenceLevel.Low, construct.Level);
	}

	[Theory]
	[InlineData("Combine(rootUrl, servicePath)")]
	[InlineData("new Uri(rootUrl).ToString()")]
	[InlineData("settings[\"ServiceUrl\"]")]
	[InlineData("rootUrl + \"/service\"")]
	[InlineData("\"https://service.example/a,b\"")]
	public void CS015MatchesAThirdArgumentWrittenAsAnyExpression(string thirdArgument)
	{
		string source = $"var importApi = new ImportAPI(\n\tuserName,\n\tpassword,\n\t{thirdArgument});";

		AssertSingleConstruct(source, _CS_015, ConfidenceLevel.High);
	}

	[Fact]
	public void CS015DoesNotMatchTheTwoArgumentFormSpreadOverLinesWithComments()
	{
		const string source = """
			var importApi = new ImportAPI(
				userName, // the operator
				webServiceUrl // the service
			);
			""";

		Assert.Empty(new CSharpRecognitionHarness().Recognize(source));
	}

	[Fact]
	public void CS015DoesNotMatchAFourArgumentFormSpreadOverLines()
	{
		const string source = """
			var importApi = new ImportAPI(
				userName,
				password,
				webServiceUrl,
				extraOption);
			""";

		Assert.Empty(new CSharpRecognitionHarness().Recognize(source));
	}

	[Fact]
	public void CS015DoesNotMatchALongerTypeNameSpreadOverLines()
	{
		const string source = """
			var importApi = new ImportAPIWrapper(
				userName,
				password,
				webServiceUrl);
			""";

		Assert.Empty(new CSharpRecognitionHarness().Recognize(source));
	}

	[Fact]
	public void CS015TerminatesOverUnfinishedAndRepeatedCalls()
	{
		string unfinished = string.Concat(Enumerable.Range(1, _MANY_REPEATED_STATEMENTS).Select(index => $"var importApi{index} = new ImportAPI(\n\tuserName, // note\n\tpassword, /* open\n"));
		string longComment = "var importApi = new ImportAPI(userName, password, " + new string('/', _MANY_FILLER_LINES) + "\n";

		AssertBoundedTermination(unfinished, _CS_015, _CONSTRUCT_CAPTURE_GROUP_NAME);
		AssertBoundedTermination(longComment, _CS_015, _CONSTRUCT_CAPTURE_GROUP_NAME);
	}

	// ------------------------------------------------------------------------------------------
	// CS-019 - a credential assembled across statements and bound to a non-HTTP sink.
	// ------------------------------------------------------------------------------------------

	[Fact]
	public void CS019ReportsACredentialAssembledAcrossStatementsAndBoundInASinkInitializerAsOneLowFinding()
	{
		const string source = """
			var credential = new NetworkCredential { UserName = userName };
			credential.Password = password;
			var mailer = new SmtpClient(host, port) { Credentials = credential };
			""";

		AssertSingleConstruct(source, _CS_019, ConfidenceLevel.Low);
	}

	[Fact]
	public void CS019ReportsACredentialAssembledAcrossStatementsAndBoundThroughACredentialsAssignmentAsOneLowFinding()
	{
		const string source = """
			var mailer = new SmtpClient(host, port);
			var credential = new NetworkCredential { UserName = userName };
			credential.Password = password;
			mailer.Credentials = credential;
			""";

		AssertSingleConstruct(source, _CS_019, ConfidenceLevel.Low);
	}

	[Fact]
	public void CS019ReportsTheSameShapeBoundToAProxyAsOneLowFinding()
	{
		const string source = """
			var credential = new NetworkCredential { UserName = userName };
			credential.Password = password;
			var route = new WebProxy(address) { Credentials = credential };
			""";

		AssertSingleConstruct(source, _CS_019, ConfidenceLevel.Low);
	}

	[Fact]
	public void CS016StillReportsTheSameShapeBoundToAnHttpHandlerAtMedium()
	{
		const string source = """
			var handler = new HttpClientHandler();
			var credential = new NetworkCredential { UserName = userName };
			credential.Password = password;
			handler.Credentials = credential;
			""";

		AssertSingleConstruct(source, _CS_016, ConfidenceLevel.Medium);
	}

	[Fact]
	public void CS016StillReportsTheShapeWhenTheMailClientBindsADifferentCredential()
	{
		const string source = """
			var mailer = new SmtpClient(host, port);
			var credential = new NetworkCredential { UserName = userName };
			credential.Password = password;
			mailer.Credentials = otherCredential;
			""";

		ImmutableArray<RecognizedConstruct> recognized = new CSharpRecognitionHarness().Recognize(source);

		RecognizedConstruct construct = Assert.Single(recognized);
		Assert.Equal(_CS_016, construct.PatternClassIdentifier);
		Assert.Equal(ConfidenceLevel.Medium, construct.Level);
	}

	[Fact]
	public void CS016StillReportsTheShapeWhenTheMailClientIsBeyondTheBoundedWindow()
	{
		string filler = string.Concat(Enumerable.Range(1, _FILLER_LINE_COUNT_BEYOND_THE_WINDOW).Select(index => $"var filler{index} = {index};\n"));
		string source = "var credential = new NetworkCredential { UserName = userName };\ncredential.Password = password;\n"
			+ filler + "var mailer = new SmtpClient(host, port) { Credentials = credential };";

		RecognizedConstruct construct = Assert.Single(new CSharpRecognitionHarness().Recognize(source));

		Assert.Equal(_CS_016, construct.PatternClassIdentifier);
		Assert.Equal(ConfidenceLevel.Medium, construct.Level);
	}

	[Fact]
	public void TheAnalyzerDeclaresCS019AsSubsumingCS016AtOverlappingSpans()
	{
		ILanguageAnalyzer analyzer = new CSharpRecognitionHarness().Analyzer;

		Assert.Contains(
			analyzer.SubsumptionRelation,
			rule => rule.SubsumingPatternClassIdentifier == _CS_019
				&& rule.SubsumedPatternClassIdentifier == _CS_016
				&& rule.When == SubsumptionCondition.MatchSpansOverlap);
	}

	[Fact]
	public void CS019AcrossStatementsShapesTerminateOverRepeatedAndUnfinishedInput()
	{
		string credentials = string.Concat(Enumerable.Range(1, _MANY_REPEATED_STATEMENTS).Select(index => $"var credential{index} = new NetworkCredential {{ UserName = userName }};\ncredential{index}.Password = password;\n"));
		string sinks = string.Concat(Enumerable.Range(1, _MANY_REPEATED_STATEMENTS).Select(index => $"var mailer{index} = new SmtpClient(host, port);\n"));

		AssertBoundedTermination(credentials, _CS_019, _CONSTRUCT_CAPTURE_GROUP_NAME);
		AssertBoundedTermination(sinks, _CS_019, _CONSTRUCT_CAPTURE_GROUP_NAME);
	}

	// ------------------------------------------------------------------------------------------
	// The source-generated and the interpreted form of every expression under test read alike.
	// ------------------------------------------------------------------------------------------

	[Theory]
	[InlineData(_CS_006)]
	[InlineData(_CS_012)]
	[InlineData(_CS_015)]
	[InlineData(_CS_018)]
	[InlineData(_CS_019)]
	public void TheGeneratedAndTheInterpretedFormOfAChangedExpressionAgreeOverTheStressShapesThatCarryItsWindows(string patternClass)
	{
		DetectionRule rule = new CSharpRecognitionHarness().Analyzer.DetectionRulesByPatternClass[patternClass];
		System.Text.RegularExpressions.Regex interpreted = new(rule.Expression.ToString(), rule.Expression.Options, System.Text.RegularExpressions.Regex.InfiniteMatchTimeout);

		foreach (string shapeName in _SHAPES_CARRYING_THE_CHANGED_WINDOWS)
		{
			string text = RecognitionHarness.Normalize(MatchBoundStressSet.Build().Single(shape => shape.Name == shapeName).Text);

			ImmutableArray<(int Index, int Length)> expected = SummarizeMatches(interpreted, text);
			ImmutableArray<(int Index, int Length)> actual = SummarizeMatches(rule.Expression, text);
			int firstDifference = Enumerable.Range(0, Math.Min(expected.Length, actual.Length)).Where(index => expected[index] != actual[index]).DefaultIfEmpty(-1).First();
			Assert.True(expected.Length == actual.Length && firstDifference < 0, $"{shapeName}: {expected.Length} vs {actual.Length}, first difference at {firstDifference}");
		}
	}

	// ------------------------------------------------------------------------------------------
	// helpers
	// ------------------------------------------------------------------------------------------

	private static ImmutableArray<(int Index, int Length)> SummarizeMatches(System.Text.RegularExpressions.Regex expression, string text)
	{
		List<(int Index, int Length)> summary = [];
		System.Text.RegularExpressions.Match match = expression.Match(text);

		while (match.Success && summary.Count < _MAXIMUM_ENUMERATION_ITERATIONS * _MATCH_COUNT_HEADROOM)
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

	/// <summary>
	/// A rule's own enumeration over the text must stop, each match must carry its named group, and each
	/// match must start beyond the previous one: the guard against a malformed, repeating match.
	/// </summary>
	private static void AssertBoundedTermination(string source, string patternClassIdentifier, string groupName)
	{
		CSharpRecognitionHarness harness = new();
		DetectionRule rule = harness.Analyzer.DetectionRulesByPatternClass[patternClassIdentifier];
		string normalizedText = RecognitionHarness.Normalize(source);
		int iterations = 0;
		int previousIndex = -1;
		System.Text.RegularExpressions.Match match = rule.Expression.Match(normalizedText);

		while (match.Success && iterations < _MAXIMUM_ENUMERATION_ITERATIONS)
		{
			iterations++;
			Assert.True(match.Groups[groupName].Success);
			Assert.True(match.Index > previousIndex);
			previousIndex = match.Index;
			match = match.NextMatch();
		}

		Assert.True(iterations < _MAXIMUM_ENUMERATION_ITERATIONS, "The enumeration did not terminate within the iteration cap.");
	}
}
