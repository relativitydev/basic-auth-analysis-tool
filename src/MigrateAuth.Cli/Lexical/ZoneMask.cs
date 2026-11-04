namespace MigrateAuth.Cli.Lexical;

/// <summary>
/// WHY A SCAN STOPPED SHORT, OR WHY IT RECOVERED. Two of the four are terminal and one is not,
/// and the difference is the whole of how the unterminated-construct policy differs from
/// the file-size and line-length ceilings.
/// </summary>
public enum LexicalAnomalyReason
{
	/// <summary>The scan reached the end of the file with every construct closed.</summary>
	None = 0,

	/// <summary>
	/// A literal was still open at the end of the file. TERMINAL: the mask is correct up
	/// to the literal's opening position and undefined after it.
	/// </summary>
	UnterminatedLiteral = 1,

	/// <summary>
	/// A block comment was still open at the end of the file. TERMINAL, on the same terms.
	/// </summary>
	UnterminatedBlockComment = 2,

	/// <summary>
	/// The recursion bound, refused BEFORE the descent that would have exceeded it. TERMINAL.
	/// It is never a stack overflow: that failure kills the process on this platform without
	/// producing any defined exit code and without diagnostics, which is the entire reason
	/// the bound exists.
	/// </summary>
	InterpolationDepthExceeded = 3,

	/// <summary>
	/// The newline policy, in its <see cref="Catalog.NewlinePolicy.Forbidden"/> and
	/// <see cref="Catalog.NewlinePolicy.EscapedContinues"/> forms: a RAW newline met inside a
	/// literal that does not admit one. such a newline "ends the literal and is an
	/// anomaly", and both halves are honoured - NOT TERMINAL, because the scanner recovers at a
	/// known line boundary and the mask after it is as sound as the mask before it.
	/// </summary>
	NewlineInsideALiteral = 4,

	/// <summary>
	/// The lexical budget: the cumulative time this file's here-document-opening and
	/// interpolation-identifier-extent expressions spent matching reached
	/// <see cref="Catalog.LexicalEnginePolicy.LEXICAL_BUDGET"/>, or one of them exceeded its own
	/// regex timeout. TERMINAL: the mask is correct up to the opening of the outermost construct
	/// being read when the budget tripped and undefined after it.
	/// </summary>
	LexicalBudgetExceeded = 5,

	/// <summary>
	/// The nested-hole recovery. A bracketed interpolation hole that counts nested openers did not return
	/// to depth zero, so the hole fell back to its first closing token. NOT TERMINAL: the scanner
	/// re-reads the hole under the first-closing rule and the mask is as sound as one without nesting.
	/// </summary>
	UnbalancedInterpolationNesting = 6
}

/// <summary>
/// ONE ZONE PER UTF-16 CODE UNIT OF THE DECODED, NORMALIZED TEXT, PLUS THE BOUNDARY PAST WHICH
/// THE SCANNER DOES NOT VOUCH FOR ITSELF.
///
/// THE BOUNDARY IS WHY THIS IS A TYPE RATHER THAN AN ARRAY.
/// The file-size and line-length ceilings abandon a whole file, and they do it BEFORE
/// scanning starts, so a caller never sees a partial answer from them. An unterminated
/// construct is not that shape: the mask is CORRECT up to the opening position of the construct
/// that never closed and UNDEFINED after it. <see cref="ValidThroughPosition"/> is where those
/// two regions meet, and recognition running over this mask must trust it only below that value.
///
/// THE UNDEFINED REGION IS STILL FILLED, AND IT IS FILLED IN THE DEMOTING DIRECTION. Every
/// position carries exactly one zone - that is the mask's contract and it holds above the boundary
/// too - and the value written there is <see cref="LexicalZone.StringBody"/> rather than
/// <see cref="LexicalZone.Code"/>. A consumer that ignores the boundary therefore errs towards
/// demoting a finding it should have kept, never towards promoting one it should not have had.
/// </summary>
public sealed class ZoneMask
{
	private const string _POSITION_OUT_OF_RANGE_MESSAGE = "The position lies outside the masked text.";

	private readonly LexicalZone[] _zones;

	internal ZoneMask(
		LexicalZone[] zones,
		int validThroughPosition,
		LexicalAnomalyReason anomalyReason,
		int newlineTerminatedLiteralCount,
		int unbalancedNestingCount)
	{
		_zones = zones;
		ValidThroughPosition = validThroughPosition;
		AnomalyReason = anomalyReason;
		NewlineTerminatedLiteralCount = newlineTerminatedLiteralCount;
		UnbalancedNestingCount = unbalancedNestingCount;
	}

	/// <summary>The masked length, in UTF-16 code units, which is the decoded text's own length.</summary>
	public int Length => _zones.Length;

	/// <summary>
	/// The exclusive upper bound of the region the scanner vouches for. Equal to
	/// <see cref="Length"/> on a complete scan, and equal to the opening position of the
	/// outermost construct that never closed otherwise.
	/// </summary>
	public int ValidThroughPosition { get; }

	/// <summary>
	/// Why the scan stopped short, or <see cref="LexicalAnomalyReason.NewlineInsideALiteral"/>
	/// where it recovered instead. Read <see cref="IsCompletelyValid"/> to tell the two apart -
	/// the reason alone does not, and conflating them is what would turn a recovered literal into
	/// a file reported as partially covered.
	/// </summary>
	public LexicalAnomalyReason AnomalyReason { get; }

	/// <summary>
	/// How many literals were closed by a raw newline their rule does not admit. It is a counter
	/// rather than a flag because its VALUE is the tuning signal: a profile run over real
	/// files with a rising count here is a profile that models its language's newline policy
	/// wrongly. It never abandons the file.
	/// </summary>
	public int NewlineTerminatedLiteralCount { get; }

	/// <summary>
	/// How many bracketed holes fell back to their first closing because their nesting depth did not
	/// return to zero (<see cref="LexicalAnomalyReason.UnbalancedInterpolationNesting"/>). Like
	/// <see cref="NewlineTerminatedLiteralCount"/> it is a recovered anomaly and never abandons the file.
	/// </summary>
	public int UnbalancedNestingCount { get; }

	/// <summary>Whether the whole file is masked soundly, which is the common case.</summary>
	public bool IsCompletelyValid => ValidThroughPosition >= _zones.Length;

	/// <summary>
	/// Whether this file must be counted under
	/// <see cref="Reading.SkippedItemCategory.LexicalAnomaly"/>. That category's own words are "a
	/// scan abandoned mid-file, leaving it partially covered", and a literal recovered at a line
	/// boundary is neither abandoned nor partially covered - so the recovered case is counted on
	/// <see cref="NewlineTerminatedLiteralCount"/> and is deliberately NOT reported as a skip.
	/// </summary>
	public bool WasAbandonedMidFile => !IsCompletelyValid;

	/// <summary>The zone at one position.</summary>
	public LexicalZone ZoneAt(int position)
	{
		if (position < 0 || position >= _zones.Length)
		{
			throw new ArgumentOutOfRangeException(nameof(position), _POSITION_OUT_OF_RANGE_MESSAGE);
		}

		return _zones[position];
	}

	/// <summary>
	/// Whether a finding may be emitted at one position. It is true everywhere except after a lexical
	/// budget trip (<see cref="LexicalAnomalyReason.LexicalBudgetExceeded"/>): the text past that trip
	/// was never read under the watch, so it is left unanalyzed rather than demoted. Every other
	/// anomaly keeps its demoting policy, so those masks answer true at every position.
	/// </summary>
	public bool IsAnalyzableAt(int position)
	{
		return AnomalyReason is not LexicalAnomalyReason.LexicalBudgetExceeded || position < ValidThroughPosition;
	}

	/// <summary>Whether the scanner vouches for the zone at one position.</summary>
	public bool IsTrustworthyAt(int position)
	{
		return position >= 0 && position < ValidThroughPosition;
	}

	/// <summary>
	/// The whole mask, without a copy. It is a span rather than an array so that a consumer
	/// cannot write into the scanner's own buffer.
	/// </summary>
	public ReadOnlySpan<LexicalZone> Zones => _zones;
}
