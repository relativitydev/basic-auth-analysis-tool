using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Text.RegularExpressions;

namespace MigrateAuth.Cli.Catalog;

/// <summary>
/// The path-shaped, language-neutral demotion signals, owned by the shared engine.
///
/// It is not a member of <see cref="ILanguageAnalyzer"/>: it is a single static value the
/// recognition engine consults for every file whatever its language, with no supplier and no
/// second instance.
///
/// It is not configurable: there is no constructor, no setter and no reader of
/// <c>EffectiveConfiguration</c> anywhere near it.
///
/// The set is empty: folder conventions such as <c>Deployment</c> are declared by each language
/// among its own language-shaped folder signals.
/// </summary>
public sealed class PathShapedDemotionSignals
{
	private PathShapedDemotionSignals()
	{
		FileNameSignals = FrozenSet<FileNameSignal>.Empty;
		FolderNameSignals = FrozenSet<FolderNameSignal>.Empty;
		GenerationMarkerExpressions = [];
	}

	/// <summary>The shared set. There is one, it is built once, and nothing can supply another.</summary>
	public static PathShapedDemotionSignals TheBaseSet { get; } = new();

	/// <summary>
	/// None: the shared engine declares no language-neutral file name convention.
	/// </summary>
	public FrozenSet<FileNameSignal> FileNameSignals { get; }

	/// <summary>
	/// The folder conventions the shared engine owns: none. Each language analyzer declares its own.
	/// </summary>
	public FrozenSet<FolderNameSignal> FolderNameSignals { get; }

	/// <summary>
	/// None: the shared engine declares no universal generated-file marker.
	/// </summary>
	public ImmutableArray<Regex> GenerationMarkerExpressions { get; }
}
