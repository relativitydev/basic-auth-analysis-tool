using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Text.RegularExpressions;

namespace MigrateAuth.Cli.Catalog;

/// <summary>
/// THE OTHER HALF OF THE DEMOTION SIGNALS: THE PATH-SHAPED, LANGUAGE-NEUTRAL ONES, OWNED BY
/// THE BASE.
///
/// It is NOT a member of <see cref="ILanguageAnalyzer"/>, because these signals belong to no
/// language: a single static value the recognition engine consults for every file whatever its
/// language, with no supplier and no second instance.
///
/// IT IS NOT CONFIGURABLE. The configuration file has no field that carries a demotion: a demotion
/// narrows, unstated, so a configurable one would let a repository silence its own audit. There is
/// therefore no constructor, no setter and no reader of <c>EffectiveConfiguration</c> anywhere near it.
///
/// THE SPLIT IS BY SHAPE: a folder named <c>Deployment</c> is a REPOSITORY-LAYOUT fact rather than
/// a language fact, and what it holds varies by project rather than by language.
/// </summary>
public sealed class PathShapedDemotionSignals
{
	/// <summary>
	/// A folder that commonly holds minified build output. It is a DEMOTION and never an exclusion,
	/// because a folder of that name may also hold deployment scripts in PowerShell or Shell.
	/// </summary>
	private const string _DEPLOYMENT_FOLDER_NAME = "Deployment";

	private PathShapedDemotionSignals()
	{
		FileNameSignals = FrozenSet<FileNameSignal>.Empty;
		FolderNameSignals = new[]
		{
			new FolderNameSignal
			{
				FolderName = _DEPLOYMENT_FOLDER_NAME,
				Sensitivity = CaseSensitivity.Insensitive
			}
		}.ToFrozenSet();
		GenerationMarkerExpressions = [];
	}

	/// <summary>The base's set. There is one, it is built once, and nothing can supply another.</summary>
	public static PathShapedDemotionSignals TheBaseSet { get; } = new();

	/// <summary>
	/// None: the shared engine declares no language-neutral file name convention.
	/// </summary>
	public FrozenSet<FileNameSignal> FileNameSignals { get; }

	/// <summary>The folder conventions the base owns. Exactly one.</summary>
	public FrozenSet<FolderNameSignal> FolderNameSignals { get; }

	/// <summary>
	/// DECLARED AS ABSENT. A universal generated-file marker is a category belonging to the base,
	/// and no marker text or expression ships for it, so the category stays named and its content
	/// stays empty.
	/// </summary>
	public ImmutableArray<Regex> GenerationMarkerExpressions { get; }
}
