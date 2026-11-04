using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Text.RegularExpressions;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Emission;
using MigrateAuth.Cli.Lexical;

namespace MigrateAuth.Cli.Recognition;

/// <summary>
/// THE FILE DEMOTION SIGNALS, EVALUATED - THREE SHAPES, ONE ANSWER.
///
/// The base's path-shaped set and the analyzer's language-shaped name/folder/generation-marker
/// set are read here and nowhere else, and either of them firing proposes Low. They are evaluated
/// identically because they are the same KIND of statement about a file; what differs is who is
/// entitled to declare one, and that is settled by which type it lives on rather than by anything
/// this code does.
///
/// PATH SIGNALS ARE DATA AND THIS IS THE PREDICATE. An analyzer hands over names and expressions,
/// never a function that inspects a path - which is what lets the rule "an analyzer never orders,
/// numbers or writes" extend to "and never sees the file".
///
/// NOTHING HERE SEES AN ABSOLUTE PATH. The path input is a <see cref="RepositoryRelativePath"/>,
/// whose only constructor is the write boundary's containment primitive, and it is decomposed into
/// a file name and folder segments. A signal matching on a segment of the path ABOVE the
/// repository root would make a finding's level depend on where the repository happens to be
/// checked out, which breaks the guarantee that identical content produces identical output.
///
/// CONTENT MARKERS ARE THE ONE SIGNAL THIS TYPE EVALUATES AGAINST TEXT. An analyzer hands over only
/// an already-constructed expression and a declared <see cref="ContentMarkerZone"/> - never a
/// function, never the file - and this is the only place that expression is ever called against
/// <see cref="FileRecognitionContext.NormalizedText"/>. EVERY content marker is
/// evaluated, WITH NO SHORT CIRCUIT AMONG THEM - see <see cref="AnyContentMarkerFires"/>.
///
/// THE ANSWER IS PER FILE AND IS COMPUTED ONCE. A positional signal - evaluated at the finding's
/// own position rather than the file's - is not modelled here; a file-scoped predicate that
/// quietly accepted a position would be the place it got built by accident.
///
/// THIS TYPE NEVER CATCHES ANYTHING. A content marker's <c>Regex</c> may carry its own match
/// timeout, and a timeout it trips is a <see cref="System.Text.RegularExpressions.RegexMatchTimeoutException"/>
/// that must propagate to <see cref="RecognitionEngine"/>'s own <c>try</c> - the caller this
/// method already runs inside - and be discarded there under the data invariant. Catching it
/// here would be catching it a second time, at a site with less context about why.
///
/// Every regular expression this type evaluates - a generation marker over a file name or folder
/// segment, a content marker over the whole file - runs through <see cref="TimedRegexEvaluation"/>
/// and charges the same per-file <see cref="MatchBoundWatch"/> a detection rule charges.
/// <see cref="TryEvaluate"/> returns whether evaluation completed within budget, on the same terms
/// <see cref="RecognitionEngine.RecognizeOneRule"/> reports for a rule: a trip here discards the
/// file exactly as a rule's own trip would, through the same
/// <see cref="Reading.SkippedItemCategory.MatchBoundExceeded"/> category.
/// </summary>
internal static class FileDemotionSignals
{
	private const char _PORTABLE_DIRECTORY_SEPARATOR = '/';

	/// <summary>
	/// Whether any demotion signal, from any owner or shape, fires on this file, and whether every
	/// phase that decides that finished inside the file's match-bound budget. <paramref
	/// name="demotion"/> is meaningful only when this returns <see langword="true"/> - exactly
	/// the convention <see cref="RecognitionEngine.RecognizeOneRule"/> already uses for a rule's own
	/// classified matches, so a caller discarding the file on a trip never has to ask which half of
	/// the return value to trust.
	/// </summary>
	internal static bool TryEvaluate(FileRecognitionContext context, MatchBoundWatch matchBoundWatch, out FileDemotion demotion)
	{
		ArgumentNullException.ThrowIfNull(context);
		ArgumentNullException.ThrowIfNull(matchBoundWatch);

		FileNameAndFolders parts = FileNameAndFolders.Of(context.Path);
		SignalEvaluation baseSignals = AnySignalFires(
			PathShapedDemotionSignals.TheBaseSet.FileNameSignals,
			PathShapedDemotionSignals.TheBaseSet.FolderNameSignals,
			PathShapedDemotionSignals.TheBaseSet.GenerationMarkerExpressions,
			parts,
			matchBoundWatch);

		SignalEvaluation evaluation = baseSignals.Completed
			? EvaluateAnalyzerAndContentSignals(context, parts, baseSignals, matchBoundWatch)
			: baseSignals;

		demotion = evaluation.Completed
			? new FileDemotion
			{
				FiresOnVendoredFolder = evaluation.FiresOnVendoredFolder,
				FiresOtherwise = evaluation.FiresOtherThanVendoredFolder
			}
			: FileDemotion.None;

		return evaluation.Completed;
	}

	/// <summary>
	/// The analyzer's own path-shaped signals, then - REGARDLESS OF WHETHER EITHER OWNER'S PATH
	/// SIGNAL ALREADY FIRED - every content marker, on unconditional-evaluation terms: a path signal firing early must not let a later content
	/// marker's own match-bound trip go unnoticed, since discarding the file is the correct outcome
	/// either way and skipping content evaluation would make that outcome depend on which signal
	/// happened to be declared first.
	/// </summary>
	private static SignalEvaluation EvaluateAnalyzerAndContentSignals(
		FileRecognitionContext context,
		FileNameAndFolders parts,
		SignalEvaluation baseSignals,
		MatchBoundWatch matchBoundWatch)
	{
		SignalEvaluation analyzerSignals = AnySignalFires(
			context.Analyzer.DemotionSignals.FileNameSignals,
			context.Analyzer.DemotionSignals.FolderNameSignals,
			context.Analyzer.DemotionSignals.GenerationMarkerExpressions,
			parts,
			matchBoundWatch);

		SignalEvaluation evaluation = analyzerSignals;

		if (analyzerSignals.Completed)
		{
			SignalEvaluation contentSignals = AnyContentMarkerFires(
				context.Analyzer.DemotionSignals.ContentMarkers,
				context.NormalizedText,
				context.Mask,
				matchBoundWatch);

			evaluation = new SignalEvaluation
			{
				Completed = contentSignals.Completed,
				FiresOtherThanVendoredFolder = baseSignals.FiresOtherThanVendoredFolder
					|| analyzerSignals.FiresOtherThanVendoredFolder
					|| contentSignals.FiresOtherThanVendoredFolder,
				FiresOnVendoredFolder = baseSignals.FiresOnVendoredFolder || analyzerSignals.FiresOnVendoredFolder
			};
		}

		return evaluation;
	}

	private static SignalEvaluation AnySignalFires(
		FrozenSet<FileNameSignal> fileNameSignals,
		FrozenSet<FolderNameSignal> folderNameSignals,
		ImmutableArray<Regex> generationMarkerExpressions,
		FileNameAndFolders parts,
		MatchBoundWatch matchBoundWatch)
	{
		bool otherNamedSignalFires =
			AnyFileNameSignalFires(fileNameSignals, parts.FileName)
			|| AnyFolderNameSignalFires(folderNameSignals, parts.FolderSegments, marksVendoredCode: false);
		bool vendoredFolderSignalFires =
			AnyFolderNameSignalFires(folderNameSignals, parts.FolderSegments, marksVendoredCode: true);

		return otherNamedSignalFires || vendoredFolderSignalFires
			? new SignalEvaluation
			{
				Completed = true,
				FiresOtherThanVendoredFolder = otherNamedSignalFires,
				FiresOnVendoredFolder = vendoredFolderSignalFires
			}
			: AnyGenerationMarkerFires(generationMarkerExpressions, parts, matchBoundWatch);
	}

	private static bool AnyFileNameSignalFires(FrozenSet<FileNameSignal> signals, string fileName)
	{
		bool fires = false;

		foreach (FileNameSignal signal in signals)
		{
			if (FileNameMatches(signal, fileName))
			{
				fires = true;
				break;
			}
		}

		return fires;
	}

	private static bool FileNameMatches(FileNameSignal signal, string fileName)
	{
		StringComparison comparison = ComparisonFor(signal.Sensitivity);

		bool matches = signal.Match switch
		{
			FileNameMatch.Prefix => fileName.StartsWith(signal.Text, comparison),
			FileNameMatch.ExactName => string.Equals(fileName, signal.Text, comparison),
			_ => fileName.EndsWith(signal.Text, comparison)
		};

		return matches;
	}

	private static bool AnyFolderNameSignalFires(
		FrozenSet<FolderNameSignal> signals,
		ImmutableArray<string> folderSegments,
		bool marksVendoredCode)
	{
		bool fires = false;

		foreach (FolderNameSignal signal in signals)
		{
			if (signal.MarksVendoredCode == marksVendoredCode && AnySegmentMatches(signal, folderSegments))
			{
				fires = true;
				break;
			}
		}

		return fires;
	}

	private static bool AnySegmentMatches(FolderNameSignal signal, ImmutableArray<string> folderSegments)
	{
		StringComparison comparison = ComparisonFor(signal.Sensitivity);
		bool matches = false;

		foreach (string segment in folderSegments)
		{
			if (FolderSegmentMatches(signal, segment, comparison))
			{
				matches = true;
				break;
			}
		}

		return matches;
	}

	/// <summary>
	/// <see cref="FolderNameMatch.Suffix"/> reaches the .NET project-folder
	/// convention a segment such as <c>Product.Core.Tests</c> is: the WHOLE segment ending in the
	/// signal's text, never a match on the file name and never a match above the repository root,
	/// since this is called only over <see cref="FileNameAndFolders.FolderSegments"/>.
	/// </summary>
	private static bool FolderSegmentMatches(FolderNameSignal signal, string segment, StringComparison comparison)
	{
		bool matches = signal.Match switch
		{
			FolderNameMatch.Suffix => segment.EndsWith(signal.FolderName, comparison),
			_ => string.Equals(segment, signal.FolderName, comparison)
		};

		return matches;
	}

	/// <summary>
	/// The generation markers, over the file name and each folder segment - name-based. They are
	/// evaluated over the same decomposed path as the other two name-based signals rather than over
	/// the file's text; a marker that needs content is what <see cref="AnyContentMarkerFires"/> is for.
	///
	/// Every expression's evaluation runs through
	/// <see cref="TimedRegexEvaluation"/> and is polled against <paramref name="matchBoundWatch"/>
	/// before it starts, on the same terms <see cref="RecognitionEngine.RecognizeOneRule"/>
	/// polls before a rule's own attempt - a small string (a file name, one folder segment) makes a
	/// runaway expression unlikely, not impossible, and the bound covers every
	/// analyzer-declared expression.
	/// </summary>
	private static SignalEvaluation AnyGenerationMarkerFires(
		ImmutableArray<Regex> generationMarkerExpressions,
		FileNameAndFolders parts,
		MatchBoundWatch matchBoundWatch)
	{
		bool fires = false;
		bool completed = true;

		foreach (Regex expression in generationMarkerExpressions)
		{
			if (matchBoundWatch.HasBeenExceeded())
			{
				completed = false;
				break;
			}

			fires = GenerationMarkerFiresOnFileNameOrAnySegment(expression, parts, matchBoundWatch, out completed);

			if (fires || !completed)
			{
				break;
			}
		}

		return new SignalEvaluation { Completed = completed, FiresOtherThanVendoredFolder = fires };
	}

	private static bool GenerationMarkerFiresOnFileNameOrAnySegment(
		Regex expression,
		FileNameAndFolders parts,
		MatchBoundWatch matchBoundWatch,
		out bool completed)
	{
		completed = true;
		bool fires = TimedRegexEvaluation.IsMatch(matchBoundWatch, expression, parts.FileName);

		if (!fires)
		{
			fires = AnySegmentMatches(expression, parts.FolderSegments, matchBoundWatch, out completed);
		}

		return fires;
	}

	private static bool AnySegmentMatches(
		Regex expression,
		ImmutableArray<string> folderSegments,
		MatchBoundWatch matchBoundWatch,
		out bool completed)
	{
		bool matches = false;
		completed = true;

		foreach (string segment in folderSegments)
		{
			if (matchBoundWatch.HasBeenExceeded())
			{
				completed = false;
				break;
			}

			matches = TimedRegexEvaluation.IsMatch(matchBoundWatch, expression, segment);

			if (matches)
			{
				break;
			}
		}

		return matches;
	}

	/// <summary>
	/// The content markers, evaluated over the file's decoded text -
	/// the engine's own job, never the analyzer's: an analyzer hands over an expression and a
	/// declared <see cref="ContentMarkerZone"/>; only this method ever calls that expression
	/// against <paramref name="normalizedText"/>.
	///
	/// EVERY MARKER IS EVALUATED, WITH NO SHORT CIRCUIT AMONG THEM - UNLESS THE BOUND HAS
	/// ALREADY TRIPPED. Stopping at the first marker that fires would let an analyzer's declared
	/// order decide whether a LATER marker ever runs - and therefore whether that later marker's own
	/// match-bound timeout ever trips. Once <paramref name="matchBoundWatch"/> reports the file's
	/// budget spent, the file is going to be discarded regardless of what a later marker would have
	/// said, so evaluating it would only spend more of a budget that has already decided the
	/// outcome. A <see cref="RegexMatchTimeoutException"/> raised by any one marker is not caught
	/// here: it propagates to the caller's own match-bound <c>try</c>, exactly like a detection
	/// rule's.
	/// </summary>
	private static SignalEvaluation AnyContentMarkerFires(
		ImmutableArray<ContentMarker> markers,
		string normalizedText,
		ZoneMask mask,
		MatchBoundWatch matchBoundWatch)
	{
		bool fires = false;
		bool completed = true;

		foreach (ContentMarker marker in markers)
		{
			if (!completed)
			{
				break;
			}

			fires = ContentMarkerFires(marker, normalizedText, mask, matchBoundWatch, out completed) || fires;
		}

		return new SignalEvaluation { Completed = completed, FiresOtherThanVendoredFolder = fires };
	}

	private static bool ContentMarkerFires(
		ContentMarker marker,
		string normalizedText,
		ZoneMask mask,
		MatchBoundWatch matchBoundWatch,
		out bool completed)
	{
		bool fires;

		if (marker.Zone is ContentMarkerZone.CodeOnly)
		{
			fires = CodeOnlyContentMarkerFires(marker.Expression, normalizedText, mask, matchBoundWatch, out completed);
		}
		else
		{
			completed = !matchBoundWatch.HasBeenExceeded();
			fires = completed && TimedRegexEvaluation.IsMatch(matchBoundWatch, marker.Expression, normalizedText);
		}

		return fires;
	}

	/// <summary>
	/// A generated-code header is itself a comment and a test attribute inside a comment or a
	/// string must not demote a file, so this walks every match of the marker's own expression
	/// until one lands on a trustworthy <see cref="LexicalZone.Code"/> position - never on the
	/// first match regardless of its zone.
	///
	/// AN UNGUARDED ENUMERATION, LIKE <see cref="FileSymbolTable.TryCollect"/>'S. Every
	/// <c>Match</c> and <c>NextMatch</c> call here runs through <see cref="TimedRegexEvaluation"/> and
	/// so is STILL TIMED against the file's own <see cref="MatchBoundWatch"/> budget exactly like a
	/// detection rule's. What is absent is the malformed-match guard: nothing here compares a match's
	/// own position against where the previous one ended, so a corrupted, non-advancing match is
	/// never detected as such and never resumed past - it is simply enumerated until either a
	/// trustworthy code-zone match is found or the wall-clock bound trips and the file is stopped and
	/// counted under <see cref="Reading.SkippedItemCategory.MatchBoundExceeded"/> instead of
	/// <see cref="Reading.SkippedItemCategory.MalformedMatch"/>.
	/// </summary>
	private static bool CodeOnlyContentMarkerFires(
		Regex expression,
		string normalizedText,
		ZoneMask mask,
		MatchBoundWatch matchBoundWatch,
		out bool completed)
	{
		bool fires = false;
		completed = !matchBoundWatch.HasBeenExceeded();
		Match match = completed
			? TimedRegexEvaluation.Match(matchBoundWatch, expression, normalizedText)
			: Match.Empty;

		while (completed && !fires && match.Success)
		{
			if (IsTrustworthyCodeAt(mask, match.Index))
			{
				fires = true;
			}
			else
			{
				completed = !matchBoundWatch.HasBeenExceeded();
				match = completed ? TimedRegexEvaluation.NextMatch(matchBoundWatch, match) : Match.Empty;
			}
		}

		return fires;
	}

	/// <summary>
	/// THE SHARED ZONE PREDICATE. Whether <paramref name="position"/> is one the scanner vouches for
	/// AND reads <see cref="LexicalZone.Code"/> - the zone a code-only content marker and a code-only
	/// flow signal are admitted in. A delimiter is not code, and a position past
	/// <see cref="ZoneMask.ValidThroughPosition"/> is untrustworthy and admitted nowhere.
	/// </summary>
	internal static bool IsTrustworthyCodeAt(ZoneMask mask, int position)
	{
		return position >= 0
			&& position < mask.Length
			&& mask.IsTrustworthyAt(position)
			&& mask.ZoneAt(position) is LexicalZone.Code;
	}

	/// <summary>
	/// The wider half of the shared zone predicate: a trustworthy position in any zone except
	/// <see cref="LexicalZone.Comment"/>, which is what a flow signal declared to fire in code or in a
	/// string body is admitted in.
	/// </summary>
	internal static bool IsTrustworthyOutsideACommentAt(ZoneMask mask, int position)
	{
		return position >= 0
			&& position < mask.Length
			&& mask.IsTrustworthyAt(position)
			&& mask.ZoneAt(position) is not LexicalZone.Comment;
	}

	/// <summary>
	/// Ordinal either way: the binary is built with
	/// invariant globalization precisely so that casing does not vary with the host's installed
	/// locale data, and a culture-aware comparison would put a language-dependent answer into a
	/// level that must be identical on every platform.
	/// </summary>
	private static StringComparison ComparisonFor(CaseSensitivity sensitivity)
	{
		return sensitivity is CaseSensitivity.Insensitive
			? StringComparison.OrdinalIgnoreCase
			: StringComparison.Ordinal;
	}

	/// <summary>
	/// A relative path split into the two things a demotion signal is allowed to look at. The
	/// separator is the portable one because <see cref="RepositoryRelativePath"/> normalizes to it
	/// on construction, which is also what makes this split identical on every platform.
	/// </summary>
	private readonly record struct FileNameAndFolders
	{
		private FileNameAndFolders(string fileName, ImmutableArray<string> folderSegments)
		{
			FileName = fileName;
			FolderSegments = folderSegments;
		}

		internal string FileName { get; }

		internal ImmutableArray<string> FolderSegments { get; }

		internal static FileNameAndFolders Of(RepositoryRelativePath path)
		{
			FileNameAndFolders parts;

			if (path.IsDeclared)
			{
				string[] segments = path.Value.Split(_PORTABLE_DIRECTORY_SEPARATOR, StringSplitOptions.RemoveEmptyEntries);

				parts = segments.Length == 0
					? new FileNameAndFolders(string.Empty, [])
					: new FileNameAndFolders(segments[^1], [.. segments[..^1]]);
			}
			else
			{
				parts = new FileNameAndFolders(string.Empty, []);
			}

			return parts;
		}
	}

	/// <summary>
	/// What one phase of this evaluation decided, and whether it decided it inside the file's
	/// match-bound budget.
	/// </summary>
	private readonly record struct SignalEvaluation
	{
		internal required bool Completed { get; init; }

		internal required bool FiresOtherThanVendoredFolder { get; init; }

		internal bool FiresOnVendoredFolder { get; init; }

		internal bool Fires => FiresOtherThanVendoredFolder || FiresOnVendoredFolder;
	}
}
