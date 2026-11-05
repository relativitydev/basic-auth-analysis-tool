using System.Collections.Immutable;
using System.Text.RegularExpressions;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Findings;
using MigrateAuth.Cli.Lexical;

namespace MigrateAuth.Cli.Recognition;

/// <summary>
/// A FILE'S FLOW SIGNALS, EVALUATED - shaped like <see cref="FileDemotionSignals"/>: an
/// analyzer hands over names, kinds, zones and expressions, and this is the only place those
/// expressions are ever called against the file's text.
///
/// EVERY MATCH GOES THROUGH <see cref="TimedRegexEvaluation"/> and charges the same per-file
/// <see cref="MatchBoundWatch"/> a detection rule charges. EVERY SIGNAL IS EVALUATED, WITH NO SHORT
/// CIRCUIT BETWEEN THEM - unless the bound has already tripped: stopping at the first signal that
/// fires would let an analyzer's declared order decide whether a later signal's own timeout ever
/// trips, which is the catalog-authoring accident the match bound keeps out of engine behaviour.
///
/// THE ANSWER IS ONE <see cref="FlowEvidence"/> PER FILE. An interactive-user signal wins over a
/// fixed-credential one. Where any phase does not finish inside the file's budget the evaluation
/// reports that it did not complete and the caller discards the partial evidence.
///
/// A SIGNAL FIRES ONLY AT A TRUSTWORTHY ANCHOR. The anchor is the optional capture group
/// named <see cref="FlowSignal.AnchorGroupName"/>, or else the start of the match. A position past
/// <see cref="ZoneMask.ValidThroughPosition"/> is one the scanner does not vouch for and fires
/// nothing. <see cref="FlowSignalZone.CodeOnly"/> admits the code zone only - a delimiter is not
/// code - and <see cref="FlowSignalZone.CodeOrStringBody"/> admits every zone except a comment.
///
/// A LITERAL CREDENTIAL IS NOT FLOW EVIDENCE. Nothing here reads the recognized
/// construct's literal-credential signal; the evidence is derived from the signals' own
/// expressions alone.
///
/// THIS TYPE NEVER CATCHES ANYTHING. A <see cref="RegexMatchTimeoutException"/> propagates to
/// <see cref="RecognitionEngine"/>, which counts the trip and discards the partial evidence.
/// </summary>
internal static class FlowSignalEvaluation
{
	/// <summary>
	/// Whether the file holds a construct the flow decision is made for: a surviving High or Medium
	/// one. A file of Low constructs alone is never evaluated.
	/// </summary>
	internal static bool IsRequiredFor(ImmutableArray<RecognizedConstruct> constructs)
	{
		bool required = false;

		foreach (RecognizedConstruct construct in constructs)
		{
			required = required || construct.Level is ConfidenceLevel.High or ConfidenceLevel.Medium;
		}

		return required;
	}

	/// <summary>
	/// Every flow signal of the file's analyzer, over the file's text. <paramref name="evidence"/> is
	/// meaningful only when this returns <see langword="true"/> - the convention
	/// <see cref="FileDemotionSignals.TryEvaluate"/> already uses.
	/// </summary>
	internal static bool TryEvaluate(FileRecognitionContext context, MatchBoundWatch matchBoundWatch, out FlowEvidence evidence)
	{
		ArgumentNullException.ThrowIfNull(context);
		ArgumentNullException.ThrowIfNull(matchBoundWatch);

		ImmutableArray<FlowSignal> signals = context.Analyzer.CandidateSelectionAndRecommendation.FlowSignals;
		bool interactiveFires = false;
		bool fixedFires = false;
		bool completed = true;

		foreach (FlowSignal signal in signals.IsDefault ? [] : signals)
		{
			completed = !matchBoundWatch.HasBeenExceeded();

			if (!completed)
			{
				break;
			}

			bool fires = SignalFires(signal, context, matchBoundWatch, out completed);

			// Polled after the signal's last attempt too, on the same terms a detection rule's own
			// loop polls after its last match: a budget spent by this signal is a trip, whether or
			// not another attempt was still to come.
			completed = completed && !matchBoundWatch.HasBeenExceeded();

			interactiveFires = interactiveFires || (fires && signal.Kind is FlowSignalKind.InteractiveUser);
			fixedFires = fixedFires || (fires && signal.Kind is FlowSignalKind.FixedCredential);

			if (!completed)
			{
				break;
			}
		}

		evidence = completed ? EvidenceOf(interactiveFires, fixedFires) : FlowEvidence.None;

		return completed;
	}

	private static FlowEvidence EvidenceOf(bool interactiveFires, bool fixedFires)
	{
		FlowEvidence evidence = FlowEvidence.None;

		if (interactiveFires)
		{
			evidence = FlowEvidence.Interactive;
		}
		else if (fixedFires)
		{
			evidence = FlowEvidence.Fixed;
		}

		return evidence;
	}

	/// <summary>
	/// Walks the signal's matches until one lands on an admitted anchor. Every <c>Match</c> and
	/// <c>NextMatch</c> is timed, and the bound is polled between successive matches, which is every
	/// point at which this code is in control.
	/// </summary>
	private static bool SignalFires(
		FlowSignal signal,
		FileRecognitionContext context,
		MatchBoundWatch matchBoundWatch,
		out bool completed)
	{
		bool fires = false;
		completed = true;
		Match match = TimedRegexEvaluation.Match(matchBoundWatch, signal.Expression, context.NormalizedText);

		while (match.Success && !fires)
		{
			fires = AnchorIsAdmitted(signal, match, context.Mask);

			if (!fires)
			{
				completed = !matchBoundWatch.HasBeenExceeded();
				match = completed ? TimedRegexEvaluation.NextMatch(matchBoundWatch, match) : Match.Empty;
			}
		}

		return fires;
	}

	private static bool AnchorIsAdmitted(FlowSignal signal, Match match, ZoneMask mask)
	{
		Group anchorGroup = match.Groups[FlowSignal.AnchorGroupName];
		int anchorPosition = anchorGroup.Success ? anchorGroup.Index : match.Index;

		return signal.Zone is FlowSignalZone.CodeOnly
			? FileDemotionSignals.IsTrustworthyCodeAt(mask, anchorPosition)
			: FileDemotionSignals.IsTrustworthyOutsideACommentAt(mask, anchorPosition);
	}
}
