using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Emission;
using MigrateAuth.Cli.Lexical;

namespace MigrateAuth.Cli.Recognition;

/// <summary>
/// ONE FILE, AS RECOGNITION NEEDS IT: the text, the mask already built over that text, the path it
/// is named by, and the analyzer whose catalog applies to it.
///
/// THE MASK ARRIVES BUILT RATHER THAN BEING BUILT HERE, and that is not merely an efficiency. The
/// scanner is where an abandoned scan becomes
/// <see cref="Reading.SkippedItemCategory.LexicalAnomaly"/>, and it emits that itself; a
/// recognition component that scanned the file again would either report the same file twice or
/// have to know not to, and both are worse than taking the mask as an input.
///
/// A MASK THAT WAS ABANDONED MID-FILE IS STILL USABLE AND IS DELIBERATELY NOT REFUSED. Findings established in the sound region are kept rather than thrown away with the
/// rest, and the undefined region above <see cref="ZoneMask.ValidThroughPosition"/> is filled in
/// the DEMOTING direction - so a classification that ignores the boundary errs towards demoting a
/// finding it should have kept, never towards promoting one it should not have had.
/// </summary>
public sealed record FileRecognitionContext
{
	/// <summary>The analyzer for this file's language, the whole of the catalog this file sees.</summary>
	public required ILanguageAnalyzer Analyzer { get; init; }

	/// <summary>The decoded, normalized text. The mask and every offset index into exactly this.</summary>
	public required string NormalizedText { get; init; }

	/// <summary>The zone mask for that text, as the scanner produced it.</summary>
	public required ZoneMask Mask { get; init; }

	/// <summary>Where the file sits, relative to the repository root. The only path form permitted.</summary>
	public required RepositoryRelativePath Path { get; init; }
}
