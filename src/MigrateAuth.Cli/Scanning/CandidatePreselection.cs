using MigrateAuth.Cli.Catalog;

namespace MigrateAuth.Cli.Scanning;

/// <summary>
/// The candidate test, run over decoded text.
///
/// A byte buffer transliterated one byte per character contains no anchor at all in a two-byte
/// encoding, and the file would silently never become a candidate. The inventory pass therefore
/// decodes once and hands the text here - which is why this method takes a string and has no overload
/// that takes bytes.
///
/// Every anchor carries its own case rule, read off the anchor and never off the profile, so one
/// analyzer can hold a case-sensitive type name and a case-insensitive header name at the same time.
///
/// A credential stored in a configuration file that belongs to none of the eight languages is
/// excluded before any anchor is tested; widening the anchor set does not reach it.
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
	/// Ordinal either way. The binary is built with invariant globalization so that casing does not vary
	/// with the host's installed locale data, and a culture-aware comparison here would put a
	/// language-dependent answer into a candidate set that must be identical on every platform.
	/// </summary>
	private static StringComparison ComparisonFor(CaseSensitivity sensitivity)
	{
		return sensitivity is CaseSensitivity.Insensitive
			? StringComparison.OrdinalIgnoreCase
			: StringComparison.Ordinal;
	}
}
