using System.Collections.Immutable;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Recognition;
using MigrateAuth.Cli.Tests.Recognition;

using Xunit;

namespace MigrateAuth.Cli.Tests.Catalog;

/// <summary>
/// THE IDENTIFIER-SUFFIX RULE OF `CS-009`. A half of the colon-joined pair counts as user-named or
/// secret-named only if its credential word is not directly followed by an identifier suffix segment (`Id`, `Ids`, `Identifier`, `Guid`,
/// `Uuid`, `Number`, `Index`, `Code`, compared whole and ignoring letter case). A pair with no counting half reads Low with the existing
/// reason; one counting half keeps it Medium. Every source here is invented: the names are generic and no fragment of any repository
/// appears in it.
/// </summary>
public sealed class CSharpIdentifierSuffixLevelTests
{
	private const string _CS_009 = "CS-009";
	private const string _NEWLINE = "\n";
	private const string _ENCODE_THE_PAIR = "string encoded = Convert.ToBase64String(Encoding.ASCII.GetBytes(pair));";
	private const string _NEUTRAL_HALF = "width";
	private const string _CONCATENATION = "concatenation";
	private const string _INTERPOLATION = "interpolation";
	private const string _FORMAT = "format";
	private const string _JOIN = "join";

	private static readonly ImmutableArray<string> _SPELLINGS = [_CONCATENATION, _INTERPOLATION, _FORMAT, _JOIN];

	private static readonly ImmutableArray<string> _SUFFIXES = ["Id", "Ids", "Identifier", "Guid", "Uuid", "Number", "Index", "Code"];

	private static readonly ImmutableArray<string> _USER_WORDS = ["user", "login", "account", "email", "mail", "usr", "uname", "principal"];

	private static readonly ImmutableArray<string> _SECRET_WORDS = ["password", "passwd", "passphrase", "passcode", "secret", "credential", "token", "pass", "pwd", "pw", "apikey"];

	public static TheoryData<string, string, string> EverySuffixOnEachSide()
	{
		TheoryData<string, string, string> data = [];

		foreach (string suffix in _SUFFIXES)
		{
			data.Add(_CONCATENATION, $"account{suffix}", _NEUTRAL_HALF);
			data.Add(_CONCATENATION, _NEUTRAL_HALF, $"token{suffix}");
			data.Add(_INTERPOLATION, $"login{suffix}", _NEUTRAL_HALF);
			data.Add(_JOIN, _NEUTRAL_HALF, $"secret{suffix}");
		}

		return data;
	}

	public static TheoryData<string, string, string> EveryCredentialWordFollowedByASuffix()
	{
		TheoryData<string, string, string> data = [];

		foreach (string word in _USER_WORDS)
		{
			data.Add(_CONCATENATION, $"{word}Id", _NEUTRAL_HALF);
			data.Add(_FORMAT, $"{word}_code", _NEUTRAL_HALF);
		}

		foreach (string word in _SECRET_WORDS)
		{
			data.Add(_CONCATENATION, _NEUTRAL_HALF, $"{word}Guid");
			data.Add(_INTERPOLATION, _NEUTRAL_HALF, $"{word}_index");
		}

		return data;
	}

	public static TheoryData<string, string, string> EverySpellingOfEachControlAndResidual()
	{
		TheoryData<string, string, string> data = [];
		(string Left, string Right)[] pairs =
		[
			("userId", "password"),
			("userName", "value"),
			("x", "apiKey"),
			("accountId", "secret"),
			("login", "apiKeyId"),
			("accountFields", "value"),
			("user", "passwordIndex"),
			("userIdentity", "value"),
			("accountCodec", "value"),
			("userid", "value"),
			("accountName", "tenantName"),
			("login", "region"),
			("email", "domain"),
			("userName", "displayName"),
			("region", "tokenCount"),
			("width", "passLength"),
			("userid", "tenantid"),
			("ACCOUNTID", "value"),
			("accountsId", "value"),
			("userId2", "value")
		];

		foreach (string spelling in _SPELLINGS)
		{
			foreach ((string left, string right) in pairs)
			{
				data.Add(spelling, left, right);
			}
		}

		return data;
	}

	public static TheoryData<string, string, string> EverySpellingOfEachSuffixedFamily()
	{
		TheoryData<string, string, string> data = [];
		(string Left, string Right)[] pairs =
		[
			("accountId", "tenantId"),
			("userGuid", "apiKeyId"),
			("loginNumber", "tokenIndex"),
			("user_id", "secret_code"),
			("accountIds", "width"),
			("width", "passwordIdentifier"),
			("userUuid", "width")
		];

		foreach (string spelling in _SPELLINGS)
		{
			foreach ((string left, string right) in pairs)
			{
				data.Add(spelling, left, right);
			}
		}

		return data;
	}

	[Theory]
	[MemberData(nameof(EverySuffixOnEachSide))]
	public void EachOfTheEightSuffixesAfterACredentialWordLeavesTheHalfUnnamedAndThePairReadsLow(string spelling, string left, string right)
	{
		AssertLow(spelling, left, right);
	}

	[Theory]
	[MemberData(nameof(EveryCredentialWordFollowedByASuffix))]
	public void EveryUserWordAndEverySecretWordFollowedByASuffixDoesNotNameACredential(string spelling, string left, string right)
	{
		AssertLow(spelling, left, right);
	}

	[Theory]
	[MemberData(nameof(EverySpellingOfEachSuffixedFamily))]
	public void TheSuffixedFamiliesReadLowInEverySpelling(string spelling, string left, string right)
	{
		AssertLow(spelling, left, right);
	}

	[Theory]
	[InlineData("userId", "password", ConfidenceLevel.Medium)]
	[InlineData("userName", "value", ConfidenceLevel.Medium)]
	[InlineData("x", "apiKey", ConfidenceLevel.Medium)]
	[InlineData("accountId", "secret", ConfidenceLevel.Medium)]
	[InlineData("login", "apiKeyId", ConfidenceLevel.Medium)]
	[InlineData("accountFields", "value", ConfidenceLevel.Medium)]
	[InlineData("user", "passwordIndex", ConfidenceLevel.Medium)]
	[InlineData("userIdentity", "value", ConfidenceLevel.Medium)]
	[InlineData("accountCodec", "value", ConfidenceLevel.Medium)]
	[InlineData("userid", "value", ConfidenceLevel.Medium)]
	[InlineData("accountId", "tenantId", ConfidenceLevel.Low)]
	[InlineData("accountId", "keyId", ConfidenceLevel.Low)]
	[InlineData("width", "keyId", ConfidenceLevel.Low)]
	public void TheControlsM1ToM9StayMediumAndTheBareKeyIdentifierIsNeverNamed(string left, string right, ConfidenceLevel expected)
	{
		RecognizedConstruct construct = Assert.Single(Recognize(Pair(_CONCATENATION, left, right)));

		Assert.Equal(expected, construct.Level);
	}

	[Theory]
	[MemberData(nameof(EverySpellingOfEachControlAndResidual))]
	public void TheControlsAndTheResidualsStayMediumWithNoReasonInEverySpelling(string spelling, string left, string right)
	{
		RecognizedConstruct construct = Assert.Single(Recognize(Pair(spelling, left, right)));

		Assert.Equal(_CS_009, construct.PatternClassIdentifier);
		Assert.Equal(ConfidenceLevel.Medium, construct.Level);
		Assert.Equal(LowConfidenceReasons.None, construct.LowReasons);
	}

	[Theory]
	[InlineData("user_id", "width", ConfidenceLevel.Low)]
	[InlineData("userId", "width", ConfidenceLevel.Low)]
	[InlineData("UserId", "width", ConfidenceLevel.Low)]
	[InlineData("AccountId", "width", ConfidenceLevel.Low)]
	[InlineData("account.Id", "width", ConfidenceLevel.Low)]
	[InlineData("account.guid", "width", ConfidenceLevel.Low)]
	[InlineData("userID", "width", ConfidenceLevel.Low)]
	[InlineData("userIDValue", "width", ConfidenceLevel.Low)]
	[InlineData("ACCOUNTIdValue", "width", ConfidenceLevel.Low)]
	[InlineData("user_GUID", "width", ConfidenceLevel.Low)]
	[InlineData("userId_x", "width", ConfidenceLevel.Low)]
	[InlineData("userid", "width", ConfidenceLevel.Medium)]
	[InlineData("USERID", "width", ConfidenceLevel.Medium)]
	[InlineData("accountsId", "width", ConfidenceLevel.Medium)]
	[InlineData("width", "credentials.Id", ConfidenceLevel.Medium)]
	[InlineData("usrId", "width", ConfidenceLevel.Low)]
	[InlineData("userId2", "width", ConfidenceLevel.Medium)]
	[InlineData("user_Id2", "width", ConfidenceLevel.Medium)]
	[InlineData("userName.Id", "width", ConfidenceLevel.Medium)]
	[InlineData("userIdentity", "width", ConfidenceLevel.Medium)]
	[InlineData("userNumbered", "width", ConfidenceLevel.Medium)]
	[InlineData("userIndexer", "width", ConfidenceLevel.Medium)]
	[InlineData("userCodec", "width", ConfidenceLevel.Medium)]
	public void EachSegmentationAndItsNearMissesIsReadOnIdentifierWordSegments(string left, string right, ConfidenceLevel expected)
	{
		RecognizedConstruct construct = Assert.Single(Recognize(Pair(_CONCATENATION, left, right)));

		Assert.Equal(expected, construct.Level);
	}

	[Theory]
	[InlineData("userAccountId", "width", ConfidenceLevel.Medium)]
	[InlineData("accountIdUser", "width", ConfidenceLevel.Medium)]
	[InlineData("userIdAccountId", "width", ConfidenceLevel.Low)]
	[InlineData("userIdAccountIdCode", "width", ConfidenceLevel.Low)]
	[InlineData("width", "tokenSecretId", ConfidenceLevel.Medium)]
	[InlineData("width", "passwordIdTokenId", ConfidenceLevel.Low)]
	public void AHalfWithSeveralCredentialWordsCountsIfAnyOccurrenceIsNotFollowedByASuffix(string left, string right, ConfidenceLevel expected)
	{
		RecognizedConstruct construct = Assert.Single(Recognize(Pair(_CONCATENATION, left, right)));

		Assert.Equal(expected, construct.Level);
	}

	[Theory]
	[InlineData("width", "apiKeyId", ConfidenceLevel.Low)]
	[InlineData("width", "api_key_id", ConfidenceLevel.Low)]
	[InlineData("width", "apikeyId", ConfidenceLevel.Low)]
	[InlineData("width", "apiKey", ConfidenceLevel.Medium)]
	[InlineData("width", "api_key_name", ConfidenceLevel.Medium)]
	[InlineData("width", "apiKeyIdText", ConfidenceLevel.Low)]
	public void TheAdjacentTokensApiAndKeyEndAtKeyAndTheSuffixFollowsThere(string left, string right, ConfidenceLevel expected)
	{
		RecognizedConstruct construct = Assert.Single(Recognize(Pair(_CONCATENATION, left, right)));

		Assert.Equal(expected, construct.Level);
	}

	[Theory]
	[InlineData("Lookup(accountId)", "width", ConfidenceLevel.Low)]
	[InlineData("Lookup(accountId).Name", "width", ConfidenceLevel.Low)]
	[InlineData("Lookup(accountName)", "width", ConfidenceLevel.Medium)]
	[InlineData("width", "values.Get(tokenIndex)", ConfidenceLevel.Low)]
	[InlineData("width", "settings.Secrets[0]", ConfidenceLevel.Medium)]
	[InlineData("pairs[userIndex]", "width", ConfidenceLevel.Low)]
	public void AMemberChainACallOrAnIndexerIsJudgedPerOccurrenceByTheSegmentRule(string left, string right, ConfidenceLevel expected)
	{
		RecognizedConstruct construct = Assert.Single(Recognize(Pair(_CONCATENATION, left, right)));

		Assert.Equal(expected, construct.Level);
	}

	[Theory]
	[InlineData("accountId", "keyId")]
	[InlineData("userGuid", "apiKeyId")]
	public void ABothHalvesSuffixedPairInsideTheEncoderCallReadsLow(string left, string right)
	{
		string source = $"string encoded = Convert.ToBase64String(Encoding.ASCII.GetBytes({left} + \":\" + {right}));";

		RecognizedConstruct construct = Assert.Single(new CSharpRecognitionHarness().Recognize(source));

		Assert.Equal(ConfidenceLevel.Low, construct.Level);
		Assert.Equal(LowConfidenceReasons.NeutralOperandPair, construct.LowReasons);
	}

	[Fact]
	public void OnlyTheCS009ConditionDeclaresTheClosedListOfEightSuffixes()
	{
		NeutralOperandPairCondition condition = LanguageCatalogRegistry.BuildEmbeddedCatalog().AnalyzersByLanguageName["CSharp"].DetectionRulesByPatternClass[_CS_009].LowWhenBothOperandsAreNeutral!;

		Assert.Equal(_SUFFIXES.Order(StringComparer.Ordinal), condition.IdentifierSuffixes.Order(StringComparer.Ordinal));
	}

	[Fact]
	public void ThePairReasonIsTheExistingOneAndTheNextReasonValueIsStillTheInactiveRegion()
	{
		// The value persisted in session state and JSON; it must not change.
		const int PersistedNeutralOperandPairValue = 256;
		// The value persisted in session state and JSON; it must not change.
		const int PersistedInactivePreprocessorRegionValue = 512;

		Assert.Equal(PersistedNeutralOperandPairValue, (int)LowConfidenceReasons.NeutralOperandPair);
		Assert.Equal(PersistedInactivePreprocessorRegionValue, (int)LowConfidenceReasons.InactivePreprocessorRegion);
	}

	private static void AssertLow(string spelling, string left, string right)
	{
		RecognizedConstruct construct = Assert.Single(Recognize(Pair(spelling, left, right)));

		Assert.Equal(_CS_009, construct.PatternClassIdentifier);
		Assert.Equal(ConfidenceLevel.Low, construct.Level);
		Assert.Equal(LowConfidenceReasons.NeutralOperandPair, construct.LowReasons);
	}

	private static ImmutableArray<RecognizedConstruct> Recognize(string pairLine)
	{
		return new CSharpRecognitionHarness().Recognize($"{pairLine}{_NEWLINE}{_ENCODE_THE_PAIR}");
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
