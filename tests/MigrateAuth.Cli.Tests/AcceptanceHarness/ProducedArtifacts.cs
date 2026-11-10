using System.Collections.Immutable;
using System.Security.Cryptography;

using MigrateAuth.Cli.Session;
using MigrateAuth.Cli.Writing;

namespace MigrateAuth.Cli.Tests.AcceptanceHarness;

/// <summary>
/// WHAT A FINISHED SESSION LEFT IN THE DESTINATION FOLDER, READ FROM OUTSIDE IT.
///
/// The pipeline set reads its artifacts through its own harness, which knows what it wrote. A run
/// through the real process knows nothing: the destination folder is the only channel, and these
/// helpers are deliberately limited to what someone inspecting that folder could establish - the
/// file names, their bytes, and the state document the session persisted for its own resumption.
///
/// The bytes are read rather than the text, because the determinism claim is byte
/// identity. Reading the text and comparing strings would silently normalize a line ending, and a
/// line ending that differed between runs is exactly the kind of defect this is looking for.
/// </summary>
internal static class ProducedArtifacts
{
	/// <summary>Every artifact this product names, in a stable order, with nothing else in the folder.</summary>
	internal static ImmutableArray<string> NamesIn(string destinationFolderPath)
	{
		return
		[
			.. Directory.EnumerateFiles(destinationFolderPath)
				.Select(Path.GetFileName)
				.OfType<string>()
				.Where(fileName => fileName.StartsWith(OutputArtifactNaming.FILE_NAME_STEM, StringComparison.Ordinal))
				.Order(StringComparer.Ordinal)
		];
	}

	/// <summary>The names of one kind, in a stable order. Plural, because a second run collides.</summary>
	internal static ImmutableArray<string> NamesOf(string destinationFolderPath, OutputArtifactKind kind)
	{
		string extension = DescribeExtension(kind);

		return
		[
			.. NamesIn(destinationFolderPath).Where(name => name.EndsWith(extension, StringComparison.Ordinal))
		];
	}

	/// <summary>The one artifact of this kind the folder holds, as bytes. Throws where there are two.</summary>
	internal static byte[] BytesOfTheOnly(string destinationFolderPath, OutputArtifactKind kind)
	{
		return File.ReadAllBytes(
			Path.Combine(destinationFolderPath, NamesOf(destinationFolderPath, kind).Single()));
	}

	/// <summary>One named artifact, as bytes.</summary>
	internal static byte[] BytesOf(string destinationFolderPath, string fileName)
	{
		return File.ReadAllBytes(Path.Combine(destinationFolderPath, fileName));
	}

	/// <summary>
	/// One artifact's digest, which is how a determinism claim is stated so that it can be read.
	///
	/// It is a digest rather than an element-wise comparison for the failure message alone: two
	/// hexadecimal strings that differ say "these runs disagreed" in one line, where two byte arrays
	/// say it in several thousand. The claim is the same claim - a digest over the bytes is byte
	/// identity.
	/// </summary>
	internal static string DigestOf(string destinationFolderPath, string fileName)
	{
		return Convert.ToHexStringLower(SHA256.HashData(BytesOf(destinationFolderPath, fileName)));
	}

	/// <summary>The one artifact of this kind the folder holds, as text, for a section assertion.</summary>
	internal static string TextOfTheOnly(string destinationFolderPath, OutputArtifactKind kind)
	{
		return File.ReadAllText(
			Path.Combine(destinationFolderPath, NamesOf(destinationFolderPath, kind).Single()));
	}

	/// <summary>
	/// The session state document for a repository, found the way a resuming process finds it: by
	/// the keyed hash of the repository path that the store itself derives.
	/// </summary>
	internal static string ReadStateDocument(string destinationFolderPath, RepositoryFolderPath repository)
	{
		return File.ReadAllText(
			Path.Combine(destinationFolderPath, SessionStateStore.BuildStateFileName(repository)));
	}

	private static string DescribeExtension(OutputArtifactKind kind)
	{
		return kind is OutputArtifactKind.MarkdownReport
			? OutputArtifactNaming.MARKDOWN_FILE_EXTENSION
			: OutputArtifactNaming.STRUCTURED_DATA_FILE_EXTENSION;
	}
}
