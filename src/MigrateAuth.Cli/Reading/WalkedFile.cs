using MigrateAuth.Cli.Emission;

namespace MigrateAuth.Cli.Reading;

/// <summary>
/// One file the walk reached: readable as far as the walk could tell, not excluded, not a link,
/// and expressible relative to the repository root.
///
/// IT IS NOT A CANDIDATE. Deciding which of these files is worth analyzing belongs to a later stage -
/// preselection anchors, the per-language inventory, the line-count aggregation - and none of
/// that is here. The walk's output is a stream of files, and what a later stage does with each
/// one is that stage's business.
///
/// It carries the path twice on purpose, and the two are not interchangeable. The absolute one
/// is working data: it is what opens the file, and no absolute path is permitted in anything
/// the product emits. The relative one is the only form that may leave the process, and it
/// exists as a type that only the write boundary's containment primitive can produce - so a
/// caller reaching for the wrong one has to do it deliberately.
/// </summary>
public readonly record struct WalkedFile
{
	internal WalkedFile(RepositoryRelativePath path, string absolutePath)
	{
		Path = path;
		AbsolutePath = absolutePath;
	}

	/// <summary>The emittable form. Relative to the repository root, portable separators.</summary>
	public RepositoryRelativePath Path { get; }

	/// <summary>Working data, never emitted: what an open call is given.</summary>
	public string AbsolutePath { get; }
}
