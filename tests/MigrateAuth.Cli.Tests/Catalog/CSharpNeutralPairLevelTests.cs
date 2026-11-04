using System.Collections.Immutable;
using System.Text.RegularExpressions;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Recognition;
using MigrateAuth.Cli.Tests.Recognition;

using Xunit;

namespace MigrateAuth.Cli.Tests.Catalog;

/// <summary>
/// THE LEVEL RULE OF `CS-009` FOR A COLON-JOINED PAIR. The class stays Medium when at least one half of the pair is named like a
/// credential and reads Low, with the neutral-pair reason, when neither is. Every source here is invented: the names are generic and
/// no fragment of any repository appears in it.
///
/// The words are tested on the whole captured text of each half, after the match, by the condition the rule declares; the left half
/// of a concatenation is the single argument that ends directly before the operator, so in <c>a + b + ":" + c</c> it is <c>b</c>.
/// </summary>
public sealed class CSharpNeutralPairLevelTests
{
	private const char _HYPHEN = '-';
	private const char _SPACE = ' ';
	private const char _UNDERSCORE = '_';
	private const char _DOT = '.';
	private const string _CS_009 = "CS-009";
	private const string _NEWLINE = "\n";
	private const string _ENCODE_THE_PAIR = "string encoded = Convert.ToBase64String(Encoding.ASCII.GetBytes(pair));";
	private const string _SWITCHED_OFF_BLOCK_OPENING = "#if FEAT_NEVER_DEFINED_HERE";
	private const string _END_OF_GROUP = "#endif";
	private const string _LEFT_OPERAND_GROUP = "leftOperand";
	private const string _CREDENTIAL_GROUP = "credential";
	private const string _CSHARP_LANGUAGE_NAME = "CSharp";
	private const string _LEFT_OPERAND_GROUP_OPENING = "(?<leftOperand>";
	private const int _LEFT_OPERAND_PLACEMENT_COUNT = 8;
	private const string _FORWARD_LITERAL_BEFORE_THE_LOOKBEHIND = @"\+(?=\s*"":""\s*\+)(?<=(?<![A-Za-z0-9_])(?<leftOperand>";
	private const string _CONCATENATION = "concatenation";
	private const string _INTERPOLATION = "interpolation";
	private const string _FORMAT = "format";
	private const string _JOIN = "join";

	private static readonly ImmutableArray<string> _SPELLINGS = [_CONCATENATION, _INTERPOLATION, _FORMAT, _JOIN];

	/// <summary>The near-miss families: a size or a ratio, a host and a port, a time or a range, a composite key.</summary>
	private static readonly ImmutableArray<(string Left, string Right)> _NEUTRAL_PAIRS =
	[
		("width", "height"),
		("numerator", "denominator"),
		("hostName", "portNumber"),
		("startTime", "endTime"),
		("lowerBound", "upperBound"),
		("orderKey", "lineKey"),
		("tenantId", "regionId")
	];

	private static readonly ImmutableArray<(string Left, string Right)> _NAMED_PAIRS =
	[
		("userName", "width"),
		("width", "tokenValue"),
		("loginName", "passwordText"),
		("customerEmail", "apiToken"),
		("accountName", "sharedSecret")
	];

	public static TheoryData<string, string, string> NeutralPairsInEverySpelling()
	{
		TheoryData<string, string, string> data = [];

		foreach (string spelling in _SPELLINGS)
		{
			foreach ((string left, string right) in _NEUTRAL_PAIRS)
			{
				data.Add(spelling, left, right);
			}
		}

		return data;
	}

	public static TheoryData<string, string, string> CredentialNamedPairsInEverySpelling()
	{
		TheoryData<string, string, string> data = [];

		foreach (string spelling in _SPELLINGS)
		{
			foreach ((string left, string right) in _NAMED_PAIRS)
			{
				data.Add(spelling, left, right);
			}
		}

		return data;
	}

	public static TheoryData<string> EverySpelling()
	{
		TheoryData<string> data = [];

		foreach (string spelling in _SPELLINGS)
		{
			data.Add(spelling);
		}

		return data;
	}

	[Theory]
	[MemberData(nameof(NeutralPairsInEverySpelling))]
	public void APairOfNeutrallyNamedHalvesReadsLowWithTheNeutralPairReasonInEverySpelling(string spelling, string left, string right)
	{
		RecognizedConstruct construct = Assert.Single(Recognize(Pair(spelling, left, right)));

		Assert.Equal(_CS_009, construct.PatternClassIdentifier);
		Assert.Equal(ConfidenceLevel.Low, construct.Level);
		Assert.Equal(LowConfidenceReasons.NeutralOperandPair, construct.LowReasons);
	}

	[Theory]
	[MemberData(nameof(CredentialNamedPairsInEverySpelling))]
	public void APairWithAtLeastOneCredentialNamedHalfStaysMediumWithNoReasonInEverySpelling(string spelling, string left, string right)
	{
		RecognizedConstruct construct = Assert.Single(Recognize(Pair(spelling, left, right)));

		Assert.Equal(_CS_009, construct.PatternClassIdentifier);
		Assert.Equal(ConfidenceLevel.Medium, construct.Level);
		Assert.Equal(LowConfidenceReasons.None, construct.LowReasons);
	}

	[Theory]
	[MemberData(nameof(EverySpelling))]
	public void AGenuinePairUsingACredentialWordOutsideTheListsReadsLowAndIsReportedForReview(string spelling)
	{
		RecognizedConstruct construct = Assert.Single(Recognize(Pair(spelling, "identityName", "sharedKey")));

		Assert.Equal(_CS_009, construct.PatternClassIdentifier);
		Assert.Equal(ConfidenceLevel.Low, construct.Level);
		Assert.Equal(LowConfidenceReasons.NeutralOperandPair, construct.LowReasons);
	}

	[Theory]
	[MemberData(nameof(EverySpelling))]
	public void WhenOnlyTheUserHalfIsNamedThePairStaysMedium(string spelling)
	{
		RecognizedConstruct construct = Assert.Single(Recognize(Pair(spelling, "userLabel", "heightValue")));

		Assert.Equal(ConfidenceLevel.Medium, construct.Level);
	}

	[Theory]
	[MemberData(nameof(EverySpelling))]
	public void WhenOnlyTheSecretHalfIsNamedThePairStaysMedium(string spelling)
	{
		RecognizedConstruct construct = Assert.Single(Recognize(Pair(spelling, "widthValue", "secretLabel")));

		Assert.Equal(ConfidenceLevel.Medium, construct.Level);
	}

	[Theory]
	[InlineData("width", "dbPass", ConfidenceLevel.Medium)]
	[InlineData("width", "db_pwd", ConfidenceLevel.Medium)]
	[InlineData("width", "dbPw", ConfidenceLevel.Medium)]
	[InlineData("width", "apiKey", ConfidenceLevel.Medium)]
	[InlineData("width", "api_key", ConfidenceLevel.Medium)]
	[InlineData("width", "API_KEY", ConfidenceLevel.Medium)]
	[InlineData("width", "apikeyValue", ConfidenceLevel.Medium)]
	[InlineData("width", "newPassphrase", ConfidenceLevel.Medium)]
	[InlineData("width", "oldPasscode", ConfidenceLevel.Medium)]
	[InlineData("width", "storedPasswd", ConfidenceLevel.Medium)]
	[InlineData("width", "clientCredential", ConfidenceLevel.Medium)]
	[InlineData("bypass", "compass", ConfidenceLevel.Low)]
	[InlineData("width", "passage", ConfidenceLevel.Low)]
	[InlineData("width", "pwdx", ConfidenceLevel.Low)]
	[InlineData("width", "apikeys", ConfidenceLevel.Low)]
	[InlineData("apiary", "monkey", ConfidenceLevel.Low)]
	[InlineData("usrName", "width", ConfidenceLevel.Medium)]
	[InlineData("unameValue", "width", ConfidenceLevel.Medium)]
	[InlineData("principalName", "width", ConfidenceLevel.Medium)]
	[InlineData("mailBox", "width", ConfidenceLevel.Medium)]
	[InlineData("loginCount", "width", ConfidenceLevel.Medium)]
	[InlineData("accountName", "width", ConfidenceLevel.Medium)]
	[InlineData("usrx", "width", ConfidenceLevel.Low)]
	[InlineData("unamed", "width", ConfidenceLevel.Low)]
	public void TheWholeTokenWordsMatchOnlyAsATokenAndTheSubstringWordsMatchAnywhere(string left, string right, ConfidenceLevel expected)
	{
		RecognizedConstruct construct = Assert.Single(Recognize(Pair(_CONCATENATION, left, right)));

		Assert.Equal(expected, construct.Level);
	}

	[Theory]
	[InlineData("Lookup(userName)", "height")]
	[InlineData("account.Width", "height")]
	[InlineData("pairs[userKey]", "height")]
	[InlineData("width", "values.Get(tokenValue)")]
	[InlineData("width", "settings.Secrets[0]")]
	public void TheWordsAreTestedOnTheWholeCapturedTextOfAHalfAndNotOnItsLastIdentifier(string left, string right)
	{
		RecognizedConstruct construct = Assert.Single(Recognize(Pair(_CONCATENATION, left, right)));

		Assert.Equal(ConfidenceLevel.Medium, construct.Level);
	}

	[Fact]
	public void InAChainOfConcatenationsTheLeftHalfIsTheSingleArgumentBeforeTheOperatorAndNeverTheWholeChain()
	{
		const string neutralLeft = "string pair = userName + width + \":\" + height;";
		const string namedLeft = "string pair = width + userName + \":\" + height;";

		RecognizedConstruct neutral = Assert.Single(Recognize(neutralLeft));
		RecognizedConstruct named = Assert.Single(Recognize(namedLeft));

		Assert.Equal(ConfidenceLevel.Low, neutral.Level);
		Assert.Equal(ConfidenceLevel.Medium, named.Level);
	}

	[Theory]
	[InlineData("string pair = a + b + \":\" + c;\nstring encoded = Convert.ToBase64String(pair);", "b")]
	[InlineData("string pair = $\"{b}:{c}\";\nstring encoded = Convert.ToBase64String(pair);", "b")]
	[InlineData("string pair = string.Format(\"{0}:{1}\", b, c);\nstring encoded = Convert.ToBase64String(pair);", "b")]
	[InlineData("string pair = string.Join(\":\", b, c);\nstring encoded = Convert.ToBase64String(pair);", "b")]
	[InlineData("string encoded = Convert.ToBase64String(Encoding.ASCII.GetBytes(a + b + \":\" + c));", "b")]
	[InlineData("string encoded = Convert.ToBase64String(Encoding.ASCII.GetBytes($\"{b}:{c}\"));", "b")]
	[InlineData("string encoded = Convert.ToBase64String(Encoding.ASCII.GetBytes(string.Format(\"{0}:{1}\", b, c)));", "b")]
	[InlineData("string encoded = Convert.ToBase64String(Encoding.ASCII.GetBytes(string.Join(\":\", b, c)));", "b")]
	[InlineData("string pair = owner.Inner.Get(first)[2] + \":\" + c;\nstring encoded = Convert.ToBase64String(pair);", "owner.Inner.Get(first)[2]")]
	public void TheLeftHalfIsCapturedAtEachOfItsFourSpellingsInBothFormsAndTheRightHalfIsTheCredentialGroup(string source, string expectedLeft)
	{
		Match match = Cs009Rule().Expression.Match(source);

		Assert.True(match.Success);
		Assert.True(match.Groups[_LEFT_OPERAND_GROUP].Success);
		Assert.Equal(expectedLeft, match.Groups[_LEFT_OPERAND_GROUP].Value);
		Assert.Equal("c", match.Groups[_CREDENTIAL_GROUP].Value);
	}

	[Fact]
	public void TheLeftOperandGroupIsDeclaredOncePerSpellingInEachOfTheTwoFormsOfThePattern()
	{
		string pattern = Cs009Rule().Expression.ToString();
		int placements = Regex.Matches(pattern, Regex.Escape(_LEFT_OPERAND_GROUP_OPENING)).Count;

		Assert.Equal(_LEFT_OPERAND_PLACEMENT_COUNT, placements);
	}

	[Fact]
	public void TheForwardLiteralOfTheConcatenatedSpellingIsPlacedBeforeTheLookbehind()
	{
		string pattern = Cs009Rule().Expression.ToString();

		Assert.Contains(_FORWARD_LITERAL_BEFORE_THE_LOOKBEHIND, pattern, StringComparison.Ordinal);
		Assert.DoesNotContain(@"(?<=(?<![A-Za-z0-9_])[A-Za-z_]", pattern, StringComparison.Ordinal);
	}

	[Theory]
	[InlineData("string encoded = Convert.ToBase64String(Encoding.ASCII.GetBytes(width + \":\" + height));", ConfidenceLevel.Low)]
	[InlineData("string encoded = Convert.ToBase64String(Encoding.ASCII.GetBytes($\"{width}:{height}\"));", ConfidenceLevel.Low)]
	[InlineData("string encoded = Convert.ToBase64String(Encoding.ASCII.GetBytes(string.Format(\"{0}:{1}\", width, height)));", ConfidenceLevel.Low)]
	[InlineData("string encoded = Convert.ToBase64String(Encoding.ASCII.GetBytes(string.Join(\":\", width, height)));", ConfidenceLevel.Low)]
	[InlineData("string encoded = Convert.ToBase64String(Encoding.ASCII.GetBytes(userName + \":\" + width));", ConfidenceLevel.Medium)]
	[InlineData("string encoded = Convert.ToBase64String(Encoding.ASCII.GetBytes($\"{width}:{tokenValue}\"));", ConfidenceLevel.Medium)]
	public void APairWrittenInsideTheEncoderCallIsReadByTheSameRule(string source, ConfidenceLevel expected)
	{
		RecognizedConstruct construct = Assert.Single(new CSharpRecognitionHarness().Recognize(source));

		Assert.Equal(_CS_009, construct.PatternClassIdentifier);
		Assert.Equal(expected, construct.Level);
	}

	[Fact]
	public void ANeutralPairInsideASwitchedOffBlockCarriesBothReasonsAtLow()
	{
		string source = Wrapped($"{Pair(_CONCATENATION, "width", "height")}{_NEWLINE}{_ENCODE_THE_PAIR}");

		RecognizedConstruct construct = Assert.Single(new CSharpRecognitionHarness().Recognize(source));

		Assert.Equal(ConfidenceLevel.Low, construct.Level);
		Assert.Equal(LowConfidenceReasons.NeutralOperandPair | LowConfidenceReasons.InactivePreprocessorRegion, construct.LowReasons);
	}

	[Fact]
	public void ACredentialNamedPairInsideASwitchedOffBlockCarriesOnlyTheInactiveReason()
	{
		string source = Wrapped($"{Pair(_CONCATENATION, "userName", "sharedSecret")}{_NEWLINE}{_ENCODE_THE_PAIR}");

		RecognizedConstruct construct = Assert.Single(new CSharpRecognitionHarness().Recognize(source));

		Assert.Equal(ConfidenceLevel.Low, construct.Level);
		Assert.Equal(LowConfidenceReasons.InactivePreprocessorRegion, construct.LowReasons);
	}

	[Fact]
	public void AnImageSizeAndRatioEncodedToBase64ReadsLowAndNothingElseFires()
	{
		const string source = """
			int widthInPixels = 640;
			int heightInPixels = 480;
			string size = widthInPixels + ":" + heightInPixels;
			string encodedSize = Convert.ToBase64String(Encoding.UTF8.GetBytes(size));
			""";

		RecognizedConstruct construct = Assert.Single(new CSharpRecognitionHarness().Recognize(source));

		Assert.Equal(_CS_009, construct.PatternClassIdentifier);
		Assert.Equal(ConfidenceLevel.Low, construct.Level);
		Assert.Equal(LowConfidenceReasons.NeutralOperandPair, construct.LowReasons);
	}

	[Fact]
	public void ARatioWrittenAsAnInterpolationEncodedToBase64ReadsLow()
	{
		const string source = """
			string ratio = $"{numerator}:{denominator}";
			string encodedRatio = Convert.ToBase64String(Encoding.UTF8.GetBytes(ratio));
			""";

		RecognizedConstruct construct = Assert.Single(new CSharpRecognitionHarness().Recognize(source));

		Assert.Equal(ConfidenceLevel.Low, construct.Level);
		Assert.Equal(LowConfidenceReasons.NeutralOperandPair, construct.LowReasons);
	}

	[Fact]
	public void OnlyTheCS009RuleOfTheCSharpAnalyzerDeclaresTheNeutralOperandPairCondition()
	{
		List<string> declaring = [];

		foreach ((string language, ILanguageAnalyzer analyzer) in LanguageCatalogRegistry.BuildEmbeddedCatalog().AnalyzersByLanguageName)
		{
			declaring.AddRange(analyzer.DetectionRulesByPatternClass
				.Where(entry => entry.Value.LowWhenBothOperandsAreNeutral is not null)
				.Select(entry => $"{language}/{entry.Key}"));
		}

		string[] expected = [$"{_CSHARP_LANGUAGE_NAME}/{_CS_009}"];

		Assert.Equal(expected, declaring.ToArray());
	}

	[Fact]
	public void TheConditionOfCS009ReadsTheLeftOperandAndTheCredentialGroupsWithBoundedWordExpressions()
	{
		NeutralOperandPairCondition condition = Cs009Rule().LowWhenBothOperandsAreNeutral!;
		DetectionRule rule = Cs009Rule();

		Assert.Equal(_LEFT_OPERAND_GROUP, condition.FirstOperandGroupName);
		Assert.Equal(_CREDENTIAL_GROUP, condition.SecondOperandGroupName);
		Assert.Contains(_LEFT_OPERAND_GROUP, rule.Expression.GetGroupNames());
		Assert.Equal(RecognitionEnginePolicy.MATCH_BOUND, condition.FirstOperandWordExpression.MatchTimeout);
		Assert.Equal(RecognitionEnginePolicy.MATCH_BOUND, condition.SecondOperandWordExpression.MatchTimeout);
		Assert.Empty(AnalyzerContractValidator.Validate(LanguageCatalogRegistry.BuildEmbeddedCatalog().AnalyzersByLanguageName[_CSHARP_LANGUAGE_NAME]));
	}

	[Theory]
	[InlineData("user", true)]
	[InlineData("LOGIN", true)]
	[InlineData("customerAccount", true)]
	[InlineData("email", true)]
	[InlineData("fooMail", true)]
	[InlineData("usr", true)]
	[InlineData("is_usr", true)]
	[InlineData("uname", true)]
	[InlineData("principal", true)]
	[InlineData("usrage", false)]
	[InlineData("unamed", false)]
	[InlineData("principally", false)]
	[InlineData("width", false)]
	[InlineData("", false)]
	public void TheUserHalfWordsAreSubstringsAndWholeTokensAsStated(string text, bool expected)
	{
		Regex words = Cs009Rule().LowWhenBothOperandsAreNeutral!.FirstOperandWordExpression;

		Assert.Equal(expected, words.IsMatch(text));
	}

	[Theory]
	[InlineData("password", true)]
	[InlineData("OldPasswd", true)]
	[InlineData("passphrase", true)]
	[InlineData("passcode", true)]
	[InlineData("secret", true)]
	[InlineData("clientCredentials", true)]
	[InlineData("accessToken", true)]
	[InlineData("pass", true)]
	[InlineData("userPass", true)]
	[InlineData("db_pwd", true)]
	[InlineData("pw", true)]
	[InlineData("apikey", true)]
	[InlineData("Api.Key", true)]
	[InlineData("api key", true)]
	[InlineData("apiKey", true)]
	[InlineData("api    key", true)]
	[InlineData("api_______key", true)]
	[InlineData("api............key", true)]
	[InlineData("api-.-.-.-.-.key", true)]
	[InlineData("apikeys", false)]
	[InlineData("api    keys", false)]
	[InlineData("bypass", false)]
	[InlineData("compass", false)]
	[InlineData("passenger", false)]
	[InlineData("pwn", false)]
	[InlineData("apiary", false)]
	[InlineData("monkey", false)]
	[InlineData("height", false)]
	[InlineData("", false)]
	public void TheSecretHalfWordsAreSubstringsAndWholeTokensAsStated(string text, bool expected)
	{
		Regex words = Cs009Rule().LowWhenBothOperandsAreNeutral!.SecondOperandWordExpression;

		Assert.Equal(expected, words.IsMatch(text));
	}

	[Theory]
	[InlineData(4)]
	[InlineData(7)]
	[InlineData(12)]
	public void TheAdjacentTokensApiAndKeyReadAsNamedWhateverTheNumberOfSeparatorsBetweenThem(int separatorCount)
	{
		string credentialHalf = $"settings[\"api{new string(_HYPHEN, separatorCount)}key\"]";

		RecognizedConstruct construct = Assert.Single(Recognize($"string pair = width + \":\" + {credentialHalf};"));

		Assert.Equal(ConfidenceLevel.Medium, construct.Level);
	}

	[Theory]
	[InlineData(4)]
	[InlineData(7)]
	[InlineData(12)]
	public void TheApiAndKeySecretHalfWordsAgreeBetweenTheGeneratedAndTheInterpretedExpression(int separatorCount)
	{
		Regex generated = Cs009Rule().LowWhenBothOperandsAreNeutral!.SecondOperandWordExpression;
		Regex interpreted = new(generated.ToString(), generated.Options, TimeSpan.FromMinutes(1));
		string[] texts = [$"api{new string(_SPACE, separatorCount)}key", $"api{new string(_UNDERSCORE, separatorCount)}keys", $"x.api{new string(_DOT, separatorCount)}key.y", "apikey", "apikeys"];

		foreach (string text in texts)
		{
			Assert.Equal(interpreted.IsMatch(text), generated.IsMatch(text));
		}
	}

	private static DetectionRule Cs009Rule()
	{
		return LanguageCatalogRegistry.BuildEmbeddedCatalog().AnalyzersByLanguageName[_CSHARP_LANGUAGE_NAME].DetectionRulesByPatternClass[_CS_009];
	}

	private static ImmutableArray<RecognizedConstruct> Recognize(string pairLine)
	{
		return new CSharpRecognitionHarness().Recognize($"{pairLine}{_NEWLINE}{_ENCODE_THE_PAIR}");
	}

	private static string Wrapped(string source)
	{
		return $"{string.Join(_NEWLINE, _SWITCHED_OFF_BLOCK_OPENING, source, _END_OF_GROUP)}{_NEWLINE}";
	}

	private static string Pair(string spelling, string left, string right)
	{
		return spelling switch
		{
			_CONCATENATION => $"string pair = {left} + \":\" + {right};",
			_INTERPOLATION => $"string pair = $\"{{{left}}}:{{{right}}}\";",
			_FORMAT => $"string pair = string.Format(\"{{0}}:{{1}}\", {left}, {right});",
			_ => $"string pair = string.Join(\":\", {left}, {right});"
		};
	}
}
