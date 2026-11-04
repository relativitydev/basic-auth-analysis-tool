using System.Collections.Immutable;
using System.Text.RegularExpressions;

using MigrateAuth.Cli.Catalog;

namespace MigrateAuth.Cli.Recognition;

/// <summary>
/// THE SAME-FILE WALK OF A FLOW SIGNAL'S VARIABLE: after a match names a variable, is it later used for
/// something that sends it. The question is asked of the logical lines after the match's own position, inside
/// the window the signal declares (<see cref="FlowSignalVariableUse.WindowLines"/> physical lines, each logical
/// line capped at <see cref="FlowSignalVariableUse.MaxLogicalLineLength"/> characters, continuations joined).
///
/// A LINE IS A USE STATEMENT WHEN IT REFERENCES THE VARIABLE (<see cref="VariableReferenceIndex"/>: <c>$NAME</c>
/// or <c>${NAME}</c>, boundary-checked, at a trustworthy position that is not a comment) AND the signal's
/// <see cref="FlowSignalVariableUse.UsePattern"/> matches it. Where the signal declares
/// <see cref="FlowSignalVariableUse.StatementSeparators"/>, "it" is only the SEGMENT of the logical line that holds
/// the reference, bounded by the nearest declared separator before and after the reference; without separators it is the whole
/// logical line (and then a use on the same line after a copy is not followed). The separators are plain
/// text and the search is QUOTE-UNAWARE: a separator inside a string or a here-string ends a segment all the same.
/// A referencing segment that is a plain copy into
/// another variable (<see cref="SimpleCopyAssignment"/>) is followed for up to
/// <see cref="FlowSignalVariableUse.AliasDepth"/> hops: the copy's target is then tracked from what follows the copy.
/// EVERY MATCH OF THE USE PATTERN GOES THROUGH <see cref="TimedRegexEvaluation"/>, and the bound is polled after each
/// one; a trip is reported as not completed, which the caller reads as every other flow-phase trip.
///
/// THE PENDING ALIAS QUEUE IS BOUNDED ONLY BY THE MATCH-BOUND WATCH. Each tracked name can enqueue one
/// copy target per referencing segment of the window, so the fan-out is up to AliasDepth times WindowLines names;
/// nothing else caps it, and the watch is polled after every use-pattern match.
///
/// WHAT IT DOES NOT FOLLOW: a use in a function that is never called, in a branch that cannot run
/// (<c>if false</c>) or after an <c>exit</c> is read as any other use; a variable that leaves the file is not followed.
/// </summary>
internal static class VariableUseTracking
{
	/// <summary>The file, its reference index and the match bound one walk reads, carried together.</summary>
	internal sealed record VariableUseWalk(FileRecognitionContext Context, VariableReferenceIndex References, MatchBoundWatch MatchBoundWatch);

	/// <summary>
	/// Whether the variable the match names is sent by a later statement of the window. False where the match
	/// names no variable. <paramref name="completed"/> is false when the match bound was reached.
	/// </summary>
	internal static bool IsSent(FlowSignalVariableUse use, Match match, VariableUseWalk walk, out bool completed)
	{
		completed = true;

		string? name = VariableNameOf(use, match);
		bool sent = false;

		if (name is not null)
		{
			int matchEnd = match.Index + match.Length;
			int windowEnd = LogicalLines.WindowEnd(walk.Context.NormalizedText, matchEnd, use.WindowLines);
			Queue<TrackedName> pending = new();

			pending.Enqueue(new TrackedName(name, matchEnd, 0));

			while (!sent && completed && pending.Count > 0)
			{
				sent = WalkOneName(new NameWalk(use, walk, pending, windowEnd), pending.Dequeue(), out completed);
			}
		}

		return sent;
	}

	private static string? VariableNameOf(FlowSignalVariableUse use, Match match)
	{
		Group group = match.Groups[use.VariableGroupName];

		return group.Success && group.Length > 0 ? group.Value : use.DefaultVariableName;
	}

	private static bool WalkOneName(NameWalk state, TrackedName tracked, out bool completed)
	{
		string text = state.Walk.Context.NormalizedText;
		bool sent = false;
		int readThrough = tracked.From;

		completed = true;

		foreach (int offset in state.Walk.References.LiveReferencesIn(tracked.Name, tracked.From, state.WindowEnd))
		{
			if (!sent && completed && offset >= readThrough)
			{
				int lineStart = LogicalLines.StartOf(text, offset);
				int lineEnd = LogicalLines.EndOf(text, lineStart);
				(int segmentStart, int segmentEnd) = SegmentAround(text, lineStart, lineEnd, offset, state.Use.StatementSeparators);
				string segment = LogicalLines.JoinedText(text, segmentStart, segmentEnd, state.Use.MaxLogicalLineLength);

				readThrough = segmentEnd;
				sent = TimedRegexEvaluation.IsMatch(state.Walk.MatchBoundWatch, state.Use.UsePattern, segment);
				completed = !state.Walk.MatchBoundWatch.HasBeenExceeded();

				if (!sent && completed)
				{
					FollowACopy(state, tracked, segment, segmentEnd);
				}
			}
		}

		return sent;
	}

	private static void FollowACopy(NameWalk state, TrackedName tracked, string segment, int segmentEnd)
	{
		if (tracked.Hops < state.Use.AliasDepth && SimpleCopyAssignment.TryReadTarget(segment, tracked.Name, out string target))
		{
			state.Pending.Enqueue(new TrackedName(target, segmentEnd, tracked.Hops + 1));
		}
	}

	/// <summary>
	/// The span of the logical line the reference at <paramref name="offset"/> belongs to: the whole line where no
	/// separator is declared, else the text between the nearest separator before and the nearest after the reference.
	/// </summary>
	private static (int Start, int End) SegmentAround(
		string text,
		int lineStart,
		int lineEnd,
		int offset,
		ImmutableArray<string> separators)
	{
		int start = lineStart;
		int end = lineEnd;

		if (!separators.IsDefaultOrEmpty)
		{
			string[] longestFirst = [.. separators.OrderByDescending(separator => separator.Length)];

			for (int position = offset - 1; position >= lineStart && start == lineStart; position--)
			{
				bool endsHere = longestFirst.Any(separator => EndsAt(text, separator, position, lineStart));

				start = endsHere ? position + 1 : lineStart;
			}

			for (int position = offset; position < lineEnd && end == lineEnd; position++)
			{
				bool startsHere = longestFirst.Any(separator => StartsAt(text, separator, position, lineEnd));

				end = startsHere ? position : lineEnd;
			}
		}

		return (start, end);
	}

	private static bool EndsAt(string text, string separator, int lastCharacterPosition, int lineStart)
	{
		int separatorStart = lastCharacterPosition - separator.Length + 1;

		return separatorStart >= lineStart && string.CompareOrdinal(text, separatorStart, separator, 0, separator.Length) == 0;
	}

	private static bool StartsAt(string text, string separator, int position, int lineEnd)
	{
		return position + separator.Length <= lineEnd && string.CompareOrdinal(text, position, separator, 0, separator.Length) == 0;
	}

	/// <summary>A variable to look for, from where, and how many copies deep it already is.</summary>
	private readonly record struct TrackedName(string Name, int From, int Hops);

	/// <summary>What one name's walk reads: the signal's declaration, the file, the queue it feeds and where the window ends.</summary>
	private sealed record NameWalk(FlowSignalVariableUse Use, VariableUseWalk Walk, Queue<TrackedName> Pending, int WindowEnd);
}
