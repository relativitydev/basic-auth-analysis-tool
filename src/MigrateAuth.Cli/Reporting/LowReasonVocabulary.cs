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

	private const string _PATTERN_CLASS_VALUE = "patternClass";
	private const string _COMMENT_OR_STRING_LITERAL_VALUE = "commentOrStringLiteral";
	private const string _DEMOTED_FILE_VALUE = "demotedFile";
	private const string _DEMOTED_VENDORED_FILE_VALUE = "demotedVendoredFile";
	private const string _SENTENCE_SEPARATOR = "; ";

	private static readonly ImmutableArray<LowConfidenceReasons> _reasonsInDeclaredOrder =
	[
		LowConfidenceReasons.PatternClass,
		LowConfidenceReasons.CommentOrStringLiteral,
		LowConfidenceReasons.DemotedFile,
		LowConfidenceReasons.DemotedVendoredFile
	];

	private static readonly FrozenDictionary<LowConfidenceReasons, string> _valuesByReason =
		new Dictionary<LowConfidenceReasons, string>
		{
			[LowConfidenceReasons.PatternClass] = _PATTERN_CLASS_VALUE,
			[LowConfidenceReasons.CommentOrStringLiteral] = _COMMENT_OR_STRING_LITERAL_VALUE,
			[LowConfidenceReasons.DemotedFile] = _DEMOTED_FILE_VALUE,
			[LowConfidenceReasons.DemotedVendoredFile] = _DEMOTED_VENDORED_FILE_VALUE
		}.ToFrozenDictionary();

	private static readonly FrozenDictionary<string, LowConfidenceReasons> _reasonsByValue =
		_valuesByReason.ToFrozenDictionary(entry => entry.Value, entry => entry.Key, StringComparer.Ordinal);

	private static readonly FrozenDictionary<LowConfidenceReasons, string> _sentencesByReason =
		new Dictionary<LowConfidenceReasons, string>
		{
			[LowConfidenceReasons.PatternClass] =
				"this pattern class is Low by definition: it recognizes a construct that is rarely live authenticating code",
			[LowConfidenceReasons.CommentOrStringLiteral] =
				"the match sits in a comment or a string literal, so it is not executable code",
			[LowConfidenceReasons.DemotedFile] =
				"the file's name, folder or content marks it as a test, sample, generated or deployment file",
			[LowConfidenceReasons.DemotedVendoredFile] =
				"the file sits in a vendor or third-party folder, so it is code the repository carries rather than its own"
		}.ToFrozenDictionary();

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
	/// statement that none was recorded.
	/// </summary>
	public static string SentenceOf(LowConfidenceReasons reasons)
	{
		string[] sentences =
		[
			.. _reasonsInDeclaredOrder.Where(reason => reasons.HasFlag(reason)).Select(reason => _sentencesByReason[reason])
		];

		return sentences.Length == 0 ? REASON_NOT_RECORDED_TEXT : string.Join(_SENTENCE_SEPARATOR, sentences);
	}
}
