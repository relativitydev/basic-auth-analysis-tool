using System.Collections.Immutable;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Emission;
using MigrateAuth.Cli.Recognition;

namespace MigrateAuth.Cli.Findings;

/// <summary>
/// ONE RECOGNIZED FILE, AS FINDING COMPOSITION NEEDS IT: what recognition produced for it, plus
/// the four things a construct cannot carry on its own.
///
/// THE CONSTRUCTS ARRIVE RECOGNIZED RATHER THAN BEING RECOGNIZED HERE, for the same reason the
/// mask arrives scanned at <see cref="FileRecognitionContext"/>: recognition emits the
/// skipped-item count for a file the match bound stopped, and a component that ran it again would
/// either double that count or have to know not to.
///
/// THE LANGUAGE IS A PROPERTY OF THE FILE AND NOT OF THE ANALYZER, which is why it is a field
/// here rather than read off the catalog. Two languages may share one profile, and the name is
/// derived per file from the extension and the first line, so an analyzer answers "which catalog"
/// and never "which language".
///
/// THE TEXT IS THE NORMALIZED TEXT AND NOTHING ELSE INDEXES INTO ANYTHING. Every anchor offset a
/// construct carries is an offset into this exact string, so a caller handing over the raw decoded
/// text would produce lines and columns that are wrong by the number of carriage returns above
/// them, silently, on one of the three platforms only.
/// </summary>
public sealed record RecognizedFile
{
	/// <summary>Where the file sits, relative to the repository root.</summary>
	public required RepositoryRelativePath Path { get; init; }

	/// <summary>The language this file was identified as.</summary>
	public required AssessedLanguage Language { get; init; }

	/// <summary>The decoded, normalized text every anchor offset indexes into.</summary>
	public required string NormalizedText { get; init; }

	/// <summary>The analyzer whose catalog applies, read for its declared destination certainty.</summary>
	public required ILanguageAnalyzer Analyzer { get; init; }

	/// <summary>
	/// What recognition produced: subsumption already applied, already ordered by anchor offset
	/// and then by pattern class identifier. The fingerprint's ordinal follows that order, so a
	/// caller reordering this array would be renaming findings.
	/// </summary>
	public required ImmutableArray<RecognizedConstruct> Constructs { get; init; }

	/// <summary>
	/// What the file's flow signals established, one value for the whole file. Copied onto each
	/// High and Medium finding by composition; a Low finding carries none.
	/// </summary>
	public FlowEvidence FlowEvidence { get; init; } = FlowEvidence.None;

	/// <summary>Whether flow-signal evaluation timed out on this file, its evidence then being <see cref="FlowEvidence.None"/>.</summary>
	public bool FlowEvaluationTripped { get; init; }
}
