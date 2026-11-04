using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Reporting;

using Xunit;

namespace MigrateAuth.Cli.Tests.Reporting;

/// <summary>
/// The sentence the report gives for why a finding is Low. A class that is Low by definition says why for
/// that class: remoting and mail credentials are live code that does not authenticate to RelativityOne with
/// Basic, which is not the same statement as a definition that builds a header, or a credential with no scheme.
/// </summary>
public sealed class LowReasonVocabularyTests
{
	private const string _NON_HTTP_SINK_SENTENCE =
		"this pattern class is Low by definition: the credential goes to a sink that is not an HTTP client, such as remoting, mail, file transfer, a directory or a proxy; that is live code, but not Basic Authentication towards RelativityOne";
	private const string _HELPER_SENTENCE =
		"this pattern class is Low by definition: it is a definition that builds a credential or an authentication header, not a call, so the migration belongs in the helper and its callers show no credential";
	private const string _UNSCHEMED_SENTENCE =
		"this pattern class is Low by definition: the credential is passed to a web command with no scheme named; against a server that answers with a bearer challenge, as RelativityOne does, it is not sent as Basic, but against a server that asks for Basic it is, so confirm where the call points";
	private const string _CHALLENGE_DEPENDENT_SENTENCE =
		"this pattern class is Low by definition: the client sends the credential as Basic only after the server challenges with Basic, or the join between the credential and the call is by text; against a server that answers with a bearer challenge, as RelativityOne does, it is not sent as Basic, but against a server that asks for Basic it is, so confirm where the call points and whether a preemptive option or a hand-built header is also used";
	private const string _GENERIC_SENTENCE =
		"this pattern class is Low by definition: as written it does not establish Basic Authentication towards RelativityOne";
	private const string _COMMENT_SENTENCE =
		"the match sits in a comment or a string literal, so it is not executable code";
	private const string _MOCK_SENTENCE =
		"the file's name, folder or content marks it as a mock, fake or stub, a test double, so it stands in for the real service rather than calling it";
	private const string _SAMPLE_SENTENCE =
		"the file's name or folder marks it as a sample or example, so it illustrates a pattern rather than running in the application";
	private const string _VENDORED_SENTENCE =
		"the file sits in a vendor or third-party folder, so it is code the repository carries rather than its own";
	private const string _GENERATED_SENTENCE =
		"the file's name or content marks it as generated code, so the fix belongs in what it is generated from rather than in the file";
	private const string _DEPLOYMENT_FOLDER_SENTENCE =
		"the file sits in a deployment folder, so it is installation or release tooling rather than the application's own code";
	private const string _REMOVED_WORDING = "rarely live authenticating code";
	private const string _DEPLOYMENT_WORD = "deployment";
	private const string _DEMOTED_FILE_CSHARP_SENTENCE =
		"the file's name, folder or content marks it as a test, sample, generated or deployment file";
	private const string _DEMOTED_FILE_POWERSHELL_SENTENCE =
		"the file's name, folder or content marks it as a test, sample or generated file";

	private static readonly string[] _declaredOrder =
	[
		"patternClass",
		"commentOrStringLiteral",
		"demotedFile",
		"demotedMockFile",
		"demotedSampleFile",
		"demotedVendoredFile",
		"demotedGeneratedFile",
		"demotedDeploymentFolder"
	];

	[Theory]
	[InlineData("CS-011", _HELPER_SENTENCE)]
	[InlineData("CS-019", _NON_HTTP_SINK_SENTENCE)]
	[InlineData("PS-012", _UNSCHEMED_SENTENCE)]
	[InlineData("PS-013", _NON_HTTP_SINK_SENTENCE)]
	[InlineData("PS-014", _HELPER_SENTENCE)]
	[InlineData("VB-018", _NON_HTTP_SINK_SENTENCE)]
	[InlineData("VB-019", _HELPER_SENTENCE)]
	[InlineData("SH-011", _CHALLENGE_DEPENDENT_SENTENCE)]
	[InlineData("SH-012", _HELPER_SENTENCE)]
	[InlineData("SH-015", _CHALLENGE_DEPENDENT_SENTENCE)]
	[InlineData("SH-017", _CHALLENGE_DEPENDENT_SENTENCE)]
	[InlineData("SH-020", _CHALLENGE_DEPENDENT_SENTENCE)]
	[InlineData("SH-022", _CHALLENGE_DEPENDENT_SENTENCE)]
	[InlineData("SH-023", _CHALLENGE_DEPENDENT_SENTENCE)]
	[InlineData("TST-001", _GENERIC_SENTENCE)]
	public void AClassThatIsLowByDefinitionStatesItsOwnReason(string identifier, string expectedSentence)
	{
		string sentence = LowReasonVocabulary.SentenceOf(LowConfidenceReasons.PatternClass, identifier);

		Assert.Equal(expectedSentence, sentence);
		Assert.DoesNotContain(_REMOVED_WORDING, sentence, StringComparison.Ordinal);
	}

	/// <summary>The two Low classes of VBNet say why they are Low, as their C# and PowerShell counterparts do.</summary>
	[Theory]
	[InlineData("VB-018", AssessedLanguage.VBNet, _NON_HTTP_SINK_SENTENCE)]
	[InlineData("VB-019", AssessedLanguage.VBNet, _HELPER_SENTENCE)]
	public void ALowVBNetClassStatesItsOwnReasonAndNotTheGenericOne(string identifier, AssessedLanguage language, string expectedSentence)
	{
		string sentence = LowReasonVocabulary.SentenceOf(LowConfidenceReasons.PatternClass, identifier, language);

		Assert.Equal(expectedSentence, sentence);
		Assert.NotEqual(_GENERIC_SENTENCE, sentence);
	}

	[Theory]
	[InlineData(LowConfidenceReasons.DemotedMockFile, "demotedMockFile", _MOCK_SENTENCE)]
	[InlineData(LowConfidenceReasons.DemotedSampleFile, "demotedSampleFile", _SAMPLE_SENTENCE)]
	[InlineData(LowConfidenceReasons.DemotedVendoredFile, "demotedVendoredFile", _VENDORED_SENTENCE)]
	[InlineData(LowConfidenceReasons.DemotedGeneratedFile, "demotedGeneratedFile", _GENERATED_SENTENCE)]
	[InlineData(LowConfidenceReasons.DemotedDeploymentFolder, "demotedDeploymentFolder", _DEPLOYMENT_FOLDER_SENTENCE)]
	public void EachDemotionKindHasItsOwnValueAndItsOwnSentenceInEveryLanguage(
		LowConfidenceReasons reason,
		string expectedValue,
		string expectedSentence)
	{
		foreach (AssessedLanguage language in Enum.GetValues<AssessedLanguage>())
		{
			Assert.Equal(expectedSentence, LowReasonVocabulary.SentenceOf(reason, null, language));
		}

		Assert.Equal([expectedValue], LowReasonVocabulary.ValuesOf(reason));
		Assert.True(LowReasonVocabulary.TryParse([expectedValue], out LowConfidenceReasons parsed));
		Assert.Equal(reason, parsed);
	}

	/// <summary>The general demoted-file sentence and its C# deployment branch stay beside the per-kind ones.</summary>
	[Fact]
	public void TheGeneralDemotedFileSentencesStayBesideThePerKindOnes()
	{
		Assert.Equal(
			_DEMOTED_FILE_CSHARP_SENTENCE,
			LowReasonVocabulary.SentenceOf(LowConfidenceReasons.DemotedFile, null, AssessedLanguage.CSharp));
		Assert.Equal(
			_DEMOTED_FILE_POWERSHELL_SENTENCE,
			LowReasonVocabulary.SentenceOf(LowConfidenceReasons.DemotedFile, null, AssessedLanguage.VBNet));
		Assert.Equal(["demotedFile"], LowReasonVocabulary.ValuesOf(LowConfidenceReasons.DemotedFile));
	}

	/// <summary>The declared order is the order of the values in the artifact, the sentences in the report and the rows of the User Guide.</summary>
	[Fact]
	public void TheValuesAndTheSentencesFollowTheDeclaredOrderWhateverTheFlagOrder()
	{
		LowConfidenceReasons everyReason =
			LowConfidenceReasons.DemotedDeploymentFolder
			| LowConfidenceReasons.DemotedGeneratedFile
			| LowConfidenceReasons.DemotedVendoredFile
			| LowConfidenceReasons.DemotedSampleFile
			| LowConfidenceReasons.DemotedMockFile
			| LowConfidenceReasons.DemotedFile
			| LowConfidenceReasons.CommentOrStringLiteral
			| LowConfidenceReasons.PatternClass;

		Assert.Equal(_declaredOrder, LowReasonVocabulary.ValuesOf(everyReason));
		Assert.Equal(
			[_GENERIC_SENTENCE, _COMMENT_SENTENCE, _DEMOTED_FILE_CSHARP_SENTENCE, _MOCK_SENTENCE, _SAMPLE_SENTENCE, _VENDORED_SENTENCE, _GENERATED_SENTENCE, _DEPLOYMENT_FOLDER_SENTENCE],
			LowReasonVocabulary.SentenceOf(everyReason).Split("; "));
	}

	[Fact]
	public void TheFlagsOfTheReasonsAreOneBitEachAndTheNewOnesFollowTheVendoredFlag()
	{
		Assert.Equal(8, (int)LowConfidenceReasons.DemotedVendoredFile);
		Assert.Equal(16, (int)LowConfidenceReasons.DemotedMockFile);
		Assert.Equal(32, (int)LowConfidenceReasons.DemotedSampleFile);
		Assert.Equal(64, (int)LowConfidenceReasons.DemotedGeneratedFile);
		Assert.Equal(128, (int)LowConfidenceReasons.DemotedDeploymentFolder);
		Assert.Equal(4, (int)LowConfidenceReasons.DemotedFile);
	}

	[Fact]
	public void AValueOutsideTheClosedVocabularyIsRefused()
	{
		Assert.False(LowReasonVocabulary.TryParse(["demotedMockFile", "demotedTestFile"], out _));
	}

	[Fact]
	public void TheTestCodeLineIsLabelledAndStatesWhyTheFindingIsAtMostMedium()
	{
		Assert.Equal("Test code", LowReasonVocabulary.TEST_CODE_LABEL);
		Assert.Contains("test code", LowReasonVocabulary.TEST_CODE_TEXT, StringComparison.Ordinal);
		Assert.Contains("at most at Medium", LowReasonVocabulary.TEST_CODE_TEXT, StringComparison.Ordinal);
	}

	[Fact]
	public void TheClassIsNotNeededForAReasonThatIsNotTheClass()
	{
		Assert.Equal(
			"the match sits in a comment or a string literal, so it is not executable code",
			LowReasonVocabulary.SentenceOf(LowConfidenceReasons.CommentOrStringLiteral, "PS-012"));
		Assert.Equal(_GENERIC_SENTENCE, LowReasonVocabulary.SentenceOf(LowConfidenceReasons.PatternClass));
	}

	[Fact]
	public void ADemotedCSharpFileMayBeADeploymentFile()
	{
		string sentence = LowReasonVocabulary.SentenceOf(LowConfidenceReasons.DemotedFile, null, AssessedLanguage.CSharp);

		Assert.Equal(_DEMOTED_FILE_CSHARP_SENTENCE, sentence);
		Assert.Contains(_DEPLOYMENT_WORD, sentence, StringComparison.Ordinal);
	}

	[Fact]
	public void ADemotedPowerShellFileIsNeverCalledADeploymentFile()
	{
		string sentence = LowReasonVocabulary.SentenceOf(LowConfidenceReasons.DemotedFile, null, AssessedLanguage.PowerShell);
		string joined = LowReasonVocabulary.SentenceOf(
			LowConfidenceReasons.PatternClass | LowConfidenceReasons.DemotedFile,
			"PS-013",
			AssessedLanguage.PowerShell);

		Assert.Equal(_DEMOTED_FILE_POWERSHELL_SENTENCE, sentence);
		Assert.DoesNotContain(_DEPLOYMENT_WORD, sentence, StringComparison.Ordinal);
		Assert.DoesNotContain(_DEPLOYMENT_WORD, joined, StringComparison.Ordinal);
	}

	/// <summary>The demoted-file sentence of VBNet leaves out "deployment", as for every language but C#.</summary>
	[Fact]
	public void ADemotedVBNetFileIsNeverCalledADeploymentFile()
	{
		string sentence = LowReasonVocabulary.SentenceOf(LowConfidenceReasons.DemotedFile, null, AssessedLanguage.VBNet);

		Assert.Equal(_DEMOTED_FILE_POWERSHELL_SENTENCE, sentence);
		Assert.DoesNotContain(_DEPLOYMENT_WORD, sentence, StringComparison.Ordinal);
	}

	[Fact]
	public void ThePs012ReasonDoesNotClaimThatNoBasicCredentialIsSent()
	{
		string sentence = LowReasonVocabulary.SentenceOf(LowConfidenceReasons.PatternClass, "PS-012");

		Assert.Contains("against a server that asks for Basic it is", sentence, StringComparison.Ordinal);
		Assert.DoesNotContain("so as written the call does not send it", sentence, StringComparison.Ordinal);
	}

	[Fact]
	public void TheReasonOfAClassThatIsLowIsJoinedToTheDemotionThatAlsoApplied()
	{
		string sentence = LowReasonVocabulary.SentenceOf(LowConfidenceReasons.PatternClass | LowConfidenceReasons.DemotedFile, "PS-013");

		Assert.Equal(
			_NON_HTTP_SINK_SENTENCE + "; the file's name, folder or content marks it as a test, sample, generated or deployment file",
			sentence);
	}
}
