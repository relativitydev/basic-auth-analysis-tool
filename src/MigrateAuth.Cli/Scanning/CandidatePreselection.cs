using MigrateAuth.Cli.Catalog;

namespace MigrateAuth.Cli.Scanning;

/// <summary>
/// THE CANDIDATE TEST, AND IT RUNS OVER DECODED TEXT.
///
/// That is the decision, not the implementation: a byte buffer transliterated one byte per
/// character contains no anchor at all in a two-byte encoding, and the failure is silent in a
/// specific way. The file was read successfully, so no counter fires; it simply never becomes a
/// candidate, and a whole encoding's worth of a repository is never analyzed while every number
/// in the report looks healthy. The inventory pass therefore decodes once with the shared decode and
/// hands the TEXT here - which is why this method takes a string and has no overload that takes
/// bytes.
///
/// EVERY ANCHOR CARRIES ITS OWN CASE RULE, because the profile's case field is scoped to keywords and
/// delimiters rather than to anchors, and two of the eight languages are case-insensitive. The
/// rule is read off the anchor and never off the profile, so one analyzer can hold a
/// case-sensitive type name and a case-insensitive header name at the same time - which the
/// reference profile does, deliberately.
///
/// IF IN-CODE SECRET DETECTION EVER ENTERS THIS PRODUCT, IT CANNOT SHARE THIS PATH. A
/// credential usually lives in a configuration file belonging to none of the eight languages, so
/// the detection rule has already excluded it before any anchor is tested. Widening the anchor set does
/// not reach it, and nothing here should be built as though it could.
/// </summary>
public static class CandidatePreselection
{
	/// <summary>
	/// Whether the decoded text contains at least one of the analyzer's anchors. One is enough:
	/// preselection decides whether the file is worth analyzing, and how many anchors it holds is
	/// not a question this stage asks.
	/// </summary>
	public static bool IsCandidate(ILanguageAnalyzer analyzer, string decodedText)
	{
		ArgumentNullException.ThrowIfNull(analyzer);
		ArgumentNullException.ThrowIfNull(decodedText);

		bool isCandidate = false;

		foreach (PreselectionAnchor anchor in analyzer.CandidateSelectionAndRecommendation.PreselectionAnchors)
		{
			if (decodedText.Contains(anchor.Text, ComparisonFor(anchor.Sensitivity)))
			{
				isCandidate = true;
				break;
			}
		}

		return isCandidate;
	}

	/// <summary>
	/// Ordinal either way. The binary is built with invariant globalization precisely so that
	/// casing does not vary with the host's installed locale data, and a culture-aware comparison
	/// here would put a language-dependent answer into a candidate set that must be
	/// identical on every platform.
	/// </summary>
	private static StringComparison ComparisonFor(CaseSensitivity sensitivity)
	{
		return sensitivity is CaseSensitivity.Insensitive
			? StringComparison.OrdinalIgnoreCase
			: StringComparison.Ordinal;
	}
}
