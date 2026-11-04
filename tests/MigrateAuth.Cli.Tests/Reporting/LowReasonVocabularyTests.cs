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
	private const string _GENERIC_SENTENCE =
		"this pattern class is Low by definition: as written it does not establish Basic Authentication towards RelativityOne";
	private const string _REMOVED_WORDING = "rarely live authenticating code";
	private const string _DEPLOYMENT_WORD = "deployment";
	private const string _DEMOTED_FILE_CSHARP_SENTENCE =
		"the file's name, folder or content marks it as a test, sample, generated or deployment file";
	private const string _DEMOTED_FILE_POWERSHELL_SENTENCE =
		"the file's name, folder or content marks it as a test, sample or generated file";

	[Theory]
	[InlineData("CS-011", _HELPER_SENTENCE)]
	[InlineData("CS-019", _NON_HTTP_SINK_SENTENCE)]
	[InlineData("PS-012", _UNSCHEMED_SENTENCE)]
	[InlineData("PS-013", _NON_HTTP_SINK_SENTENCE)]
	[InlineData("PS-014", _HELPER_SENTENCE)]
	[InlineData("TST-001", _GENERIC_SENTENCE)]
	public void AClassThatIsLowByDefinitionStatesItsOwnReason(string identifier, string expectedSentence)
	{
		string sentence = LowReasonVocabulary.SentenceOf(LowConfidenceReasons.PatternClass, identifier);

		Assert.Equal(expectedSentence, sentence);
		Assert.DoesNotContain(_REMOVED_WORDING, sentence, StringComparison.Ordinal);
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
