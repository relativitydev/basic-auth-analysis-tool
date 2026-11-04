using MigrateAuth.Cli.Session;
using MigrateAuth.Cli.Writing;

namespace MigrateAuth.Cli.Agent;

/// <summary>
/// THE ONE PATH THAT MAY CROSS THE AGENT BOUNDARY: the prompt pack that has ALREADY BEEN
/// WRITTEN, named as a file rather than as text.
///
/// The launch operation "launches with a prompt pack path", and a path is the exact
/// shape of the hole its content rule exists to close - a `string` parameter called
/// <c>promptPackPath</c> accepts a matched line as readily as it accepts a path, which is the
/// reasoning <see cref="MigrateAuth.Cli.Emission.RepositoryRelativePath"/> already applies to the
/// emission point. So there is no constructor here that takes arbitrary text either.
///
/// THE ONLY WAY IN IS A DESTINATION FOLDER PLUS A COMPLETED WRITE. Both halves are types that
/// cannot carry repository content by construction:
/// <see cref="DocumentationDestinationFolderPath"/> is the folder the session was told to write
/// to and is never the audited repository, and <see cref="ArtifactWriteResult"/> reports a write
/// that has already passed the write boundary, carrying the settled file name and never a path. A
/// pack path therefore cannot be produced for a file that was not written, and cannot be aimed at
/// the repository under assessment - the same file the write boundary already refused to create
/// there cannot be named here.
///
/// The file name is additionally required to be a BARE NAME. A write result is a public type and
/// nothing stops a caller hand-building one, so the guarantee is not left resting on the writer
/// being the only author: whatever the name is, it can only ever be one component inside the
/// destination folder, never a route out of it.
///
/// The type is the parameter of the launcher's signature: whatever writes a pack gets its
/// <see cref="ArtifactWriteResult"/> back and turns it into one of these.
/// </summary>
public readonly record struct PromptPackFilePath
{
	private static readonly char[] _pathComponentSeparators = ['/', '\\'];

	private PromptPackFilePath(string absolutePath, string fileName)
	{
		AbsolutePath = absolutePath;
		FileName = fileName;
	}

	/// <summary>
	/// The absolute path handed to the launched process. Working data only, exactly as
	/// <see cref="DocumentationDestinationFolderPath.CanonicalPath"/> is: no
	/// absolute path may appear in anything the product writes, and nothing here is written.
	/// </summary>
	public string AbsolutePath { get; }

	/// <summary>The settled file name on its own, which is the only form fit to appear in output.</summary>
	public string FileName { get; }

	/// <summary>Whether this instance names a file at all. <c>false</c> on <c>default</c>.</summary>
	public bool IsDeclared => !string.IsNullOrEmpty(AbsolutePath);

	/// <summary>
	/// Accepts the destination folder and the result of the write that produced the pack, and
	/// refuses everything else - a write that did not happen, a name that is not a bare file
	/// name, and a destination that was never established.
	/// </summary>
	public static bool TryForWrittenPack(
		DocumentationDestinationFolderPath destination,
		ArtifactWriteResult packWriteResult,
		out PromptPackFilePath promptPackPath)
	{
		ArgumentNullException.ThrowIfNull(packWriteResult);

		bool accepted =
			packWriteResult.Wrote
			&& !string.IsNullOrEmpty(destination.CanonicalPath)
			&& IsBareFileName(packWriteResult.FileNameWritten);

		promptPackPath = accepted
			? new PromptPackFilePath(
				System.IO.Path.Combine(destination.CanonicalPath, packWriteResult.FileNameWritten),
				packWriteResult.FileNameWritten)
			: default;

		return accepted;
	}

	/// <summary>
	/// Whether the name is one component and nothing else. Both separators are rejected on every
	/// platform rather than only the platform's own, because a name carrying the other one is
	/// still a caller trying to compose a path where a name was asked for.
	/// </summary>
	private static bool IsBareFileName(string candidate)
	{
		bool isBareName =
			!string.IsNullOrWhiteSpace(candidate)
			&& candidate.IndexOfAny(_pathComponentSeparators) < 0
			&& !System.IO.Path.IsPathRooted(candidate);

		return isBareName;
	}
}
