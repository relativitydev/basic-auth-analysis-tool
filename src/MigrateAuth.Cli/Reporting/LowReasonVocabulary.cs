using System.Collections.Frozen;
using System.Collections.Immutable;

using MigrateAuth.Cli.Catalog;

namespace MigrateAuth.Cli.Reporting;

/// <summary>
/// THE WORDS FOR WHY A FINDING IS LOW, DECLARED ONCE FOR THE REPORT, THE DATA ARTIFACT AND THE
/// SESSION STATE FILE.
///
/// Each reason has a stable value, which the data artifact and the state file carry, and a sentence,
/// which the Markdown report prints. Both are this product's own closed vocabulary: a value read back
/// from a state file that is not one of them is refused rather than carried into a report, so the
/// text of an edited file can never reach the output through this route.
/// </summary>
public static class LowReasonVocabulary
{
	/// <summary>What the Markdown report says for a Low finding whose reason was not recorded.</summary>
	public const string REASON_NOT_RECORDED_TEXT = "the reason was not recorded";

	/// <summary>The label of the per-finding line that states the reason.</summary>
	public const string REASON_LABEL = "Low because";

	/// <summary>The label of the per-finding line that marks a finding as test code.</summary>
	public const string TEST_CODE_LABEL = "Test code";

	/// <summary>
	/// What the Markdown report says on the test-code line. The line is written only for a finding whose file the Test
	/// demotion kind marked, and the sentence says why the finding is not reported at a higher level than Medium.
	/// </summary>
	public const string TEST_CODE_TEXT =
		"yes; the file's name, folder or a test-framework marker marks it as test code, so the finding is reported at most at Medium";

	private const string _PATTERN_CLASS_VALUE = "patternClass";
	private const string _COMMENT_OR_STRING_LITERAL_VALUE = "commentOrStringLiteral";
	private const string _DEMOTED_FILE_VALUE = "demotedFile";
	private const string _DEMOTED_MOCK_FILE_VALUE = "demotedMockFile";
	private const string _DEMOTED_SAMPLE_FILE_VALUE = "demotedSampleFile";
	private const string _DEMOTED_VENDORED_FILE_VALUE = "demotedVendoredFile";
	private const string _DEMOTED_GENERATED_FILE_VALUE = "demotedGeneratedFile";
	private const string _DEMOTED_DEPLOYMENT_FOLDER_VALUE = "demotedDeploymentFolder";
	private const string _SENTENCE_SEPARATOR = "; ";
	private const string _DEMOTED_FILE_WITH_DEPLOYMENT_SENTENCE =
		"the file's name, folder or content marks it as a test, sample, generated or deployment file";
	private const string _DEMOTED_FILE_WITHOUT_DEPLOYMENT_SENTENCE =
		"the file's name, folder or content marks it as a test, sample or generated file";
	private const string _HELPER_DEFINITION_SENTENCE =
		PATTERN_CLASS_SENTENCE_LEAD + "it is a definition that builds a credential or an authentication header, not a call, so the migration belongs in the helper and its callers show no credential";
	private const string _NON_HTTP_SINK_SENTENCE =
		PATTERN_CLASS_SENTENCE_LEAD + "the credential goes to a sink that is not an HTTP client, such as remoting, mail, file transfer, a directory or a proxy; that is live code, but not Basic Authentication towards RelativityOne";
	private const string _CHALLENGE_DEPENDENT_CREDENTIAL_SENTENCE =
		$"{PATTERN_CLASS_SENTENCE_LEAD}the client sends the credential as Basic only after the server challenges with Basic, or the join between the credential and the call is by text; against a server that answers with a bearer challenge, as RelativityOne does, it is not sent as Basic, but against a server that asks for Basic it is, so confirm where the call points and whether a preemptive option or a hand-built header is also used";
	private const string _UNSCHEMED_WEB_CREDENTIAL_SENTENCE =
		PATTERN_CLASS_SENTENCE_LEAD + "the credential is passed to a web command with no scheme named; against a server that answers with a bearer challenge, as RelativityOne does, it is not sent as Basic, but against a server that asks for Basic it is, so confirm where the call points";

	/// <summary>How the sentence for a class that is Low by definition starts; the clause after it says why for that class.</summary>
	public const string PATTERN_CLASS_SENTENCE_LEAD = "this pattern class is Low by definition: ";

	private static readonly ImmutableArray<LowConfidenceReasons> _reasonsInDeclaredOrder =
	[
		LowConfidenceReasons.PatternClass,
		LowConfidenceReasons.CommentOrStringLiteral,
		LowConfidenceReasons.DemotedFile,
		LowConfidenceReasons.DemotedMockFile,
		LowConfidenceReasons.DemotedSampleFile,
		LowConfidenceReasons.DemotedVendoredFile,
		LowConfidenceReasons.DemotedGeneratedFile,
		LowConfidenceReasons.DemotedDeploymentFolder
	];

	private static readonly FrozenDictionary<LowConfidenceReasons, string> _valuesByReason =
		new Dictionary<LowConfidenceReasons, string>
		{
			[LowConfidenceReasons.PatternClass] = _PATTERN_CLASS_VALUE,
			[LowConfidenceReasons.CommentOrStringLiteral] = _COMMENT_OR_STRING_LITERAL_VALUE,
			[LowConfidenceReasons.DemotedFile] = _DEMOTED_FILE_VALUE,
			[LowConfidenceReasons.DemotedMockFile] = _DEMOTED_MOCK_FILE_VALUE,
			[LowConfidenceReasons.DemotedSampleFile] = _DEMOTED_SAMPLE_FILE_VALUE,
			[LowConfidenceReasons.DemotedVendoredFile] = _DEMOTED_VENDORED_FILE_VALUE,
			[LowConfidenceReasons.DemotedGeneratedFile] = _DEMOTED_GENERATED_FILE_VALUE,
			[LowConfidenceReasons.DemotedDeploymentFolder] = _DEMOTED_DEPLOYMENT_FOLDER_VALUE
		}.ToFrozenDictionary();

	private static readonly FrozenDictionary<string, LowConfidenceReasons> _reasonsByValue =
		_valuesByReason.ToFrozenDictionary(entry => entry.Value, entry => entry.Key, StringComparer.Ordinal);

	private static readonly FrozenDictionary<LowConfidenceReasons, string> _sentencesByReason =
		new Dictionary<LowConfidenceReasons, string>
		{
			[LowConfidenceReasons.PatternClass] =
				PATTERN_CLASS_SENTENCE_LEAD + "as written it does not establish Basic Authentication towards RelativityOne",
			[LowConfidenceReasons.CommentOrStringLiteral] =
				"the match sits in a comment or a string literal, so it is not executable code",
			[LowConfidenceReasons.DemotedFile] =
				_DEMOTED_FILE_WITH_DEPLOYMENT_SENTENCE,
			[LowConfidenceReasons.DemotedMockFile] =
				"the file's name, folder or content marks it as a mock, fake or stub, a test double, so it stands in for the real service rather than calling it",
			[LowConfidenceReasons.DemotedSampleFile] =
				"the file's name or folder marks it as a sample or example, so it illustrates a pattern rather than running in the application",
			[LowConfidenceReasons.DemotedVendoredFile] =
				"the file sits in a vendor or third-party folder, so it is code the repository carries rather than its own",
			[LowConfidenceReasons.DemotedGeneratedFile] =
				"the file's name or content marks it as generated code, so the fix belongs in what it is generated from rather than in the file",
			[LowConfidenceReasons.DemotedDeploymentFolder] =
				"the file sits in a deployment folder, so it is installation or release tooling rather than the application's own code"
		}.ToFrozenDictionary();

	private static readonly FrozenDictionary<string, string> _patternClassSentencesByIdentifier =
		new Dictionary<string, string>(StringComparer.Ordinal)
		{
			["CS-011"] = _HELPER_DEFINITION_SENTENCE,
			["CS-019"] = _NON_HTTP_SINK_SENTENCE,
			["VB-018"] = _NON_HTTP_SINK_SENTENCE,
			["VB-019"] = _HELPER_DEFINITION_SENTENCE,
			["PS-012"] = _UNSCHEMED_WEB_CREDENTIAL_SENTENCE,
			["PS-013"] = _NON_HTTP_SINK_SENTENCE,
			["PS-014"] = _HELPER_DEFINITION_SENTENCE,
			["SH-011"] = _CHALLENGE_DEPENDENT_CREDENTIAL_SENTENCE,
			["SH-012"] = _HELPER_DEFINITION_SENTENCE,
			["SH-015"] = _CHALLENGE_DEPENDENT_CREDENTIAL_SENTENCE,
			["SH-017"] = _CHALLENGE_DEPENDENT_CREDENTIAL_SENTENCE,
			["SH-020"] = _CHALLENGE_DEPENDENT_CREDENTIAL_SENTENCE,
			["SH-022"] = _CHALLENGE_DEPENDENT_CREDENTIAL_SENTENCE,
			["SH-023"] = _CHALLENGE_DEPENDENT_CREDENTIAL_SENTENCE
		}.ToFrozenDictionary(StringComparer.Ordinal);

	/// <summary>The stable values of the reasons that apply, in the declared order. Empty when none do.</summary>
	public static string[] ValuesOf(LowConfidenceReasons reasons)
	{
		return [.. _reasonsInDeclaredOrder.Where(reason => reasons.HasFlag(reason)).Select(reason => _valuesByReason[reason])];
	}

	/// <summary>
	/// The reasons the values name. False when any value is not one of the declared ones, so a file
	/// carrying anything else is refused rather than partly read.
	/// </summary>
	public static bool TryParse(IEnumerable<string?>? values, out LowConfidenceReasons reasons)
	{
		reasons = LowConfidenceReasons.None;

		bool allKnown = true;

		foreach (string? value in values ?? [])
		{
			if (value is not null && _reasonsByValue.TryGetValue(value, out LowConfidenceReasons reason))
			{
				reasons |= reason;
			}
			else
			{
				allKnown = false;
			}
		}

		return allKnown;
	}

	/// <summary>
	/// The sentence the report prints for a Low finding: every reason that applies, joined, or the
	/// statement that none was recorded. When the class itself is Low and the finding's class is named,
	/// the sentence says why for that class rather than for Low classes in general.
	/// </summary>
	public static string SentenceOf(
		LowConfidenceReasons reasons,
		string? patternClassIdentifier = null,
		AssessedLanguage language = AssessedLanguage.CSharp)
	{
		string[] sentences =
		[
			.. _reasonsInDeclaredOrder.Where(reason => reasons.HasFlag(reason)).Select(reason => SentenceFor(reason, patternClassIdentifier, language))
		];

		return sentences.Length == 0 ? REASON_NOT_RECORDED_TEXT : string.Join(_SENTENCE_SEPARATOR, sentences);
	}

	private static string SentenceFor(LowConfidenceReasons reason, string? patternClassIdentifier, AssessedLanguage language)
	{
		string sentence = _sentencesByReason[reason];

		if (reason == LowConfidenceReasons.DemotedFile && language != AssessedLanguage.CSharp)
		{
			sentence = _DEMOTED_FILE_WITHOUT_DEPLOYMENT_SENTENCE;
		}

		if (reason == LowConfidenceReasons.PatternClass
			&& patternClassIdentifier is not null
			&& _patternClassSentencesByIdentifier.TryGetValue(patternClassIdentifier, out string? classSentence))
		{
			sentence = classSentence;
		}

		return sentence;
	}
}
