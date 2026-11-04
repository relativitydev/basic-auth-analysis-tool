using MigrateAuth.Cli.Agent;
using MigrateAuth.Cli.Session;
using MigrateAuth.Cli.Writing;

using Xunit;

namespace MigrateAuth.Cli.Tests.Agent;

/// <summary>
/// THE ONE PATH THAT CROSSES THE BOUNDARY, AND WHAT IT REFUSES TO BE BUILT FROM.
///
/// The launch operation takes a prompt pack path, and a path is the shape of the hole the pack's
/// content rule exists to close. The type accepts only the documentation destination plus a
/// COMPLETED write, so a pack path cannot name a file that was never produced and cannot be aimed at
/// the repository under assessment - the write boundary already refused to produce one there.
///
/// The bare-name requirement is the part that does not rest on the writer being the only author. An
/// <see cref="ArtifactWriteResult"/> is a public type and a caller can hand-build one; what the
/// checks below guarantee is that whatever name it carries can only ever be one component INSIDE the
/// destination folder, never a route out of it.
/// </summary>
public sealed class PromptPackFilePathTests
{
	private const string _DESTINATION_FOLDER_NAME = "reports";
	private const string _PACK_FILE_NAME = "basic-auth-migration-2026-09-18.pack.md";

	[Fact]
	public void ACompletedWriteInTheDestinationProducesAPackPath()
	{
		DocumentationDestinationFolderPath destination = BuildDestination();

		Assert.True(PromptPackFilePath.TryForWrittenPack(destination, Wrote(_PACK_FILE_NAME), out PromptPackFilePath packPath));
		Assert.True(packPath.IsDeclared);
		Assert.Equal(_PACK_FILE_NAME, packPath.FileName);
		Assert.Equal(Path.Combine(destination.CanonicalPath, _PACK_FILE_NAME), packPath.AbsolutePath);
	}

	[Fact]
	public void AWriteThatDidNotHappenProducesNoPackPath()
	{
		ArtifactWriteResult refused = new()
		{
			Wrote = false,
			FileNameWritten = string.Empty,
			Refusal = ArtifactWriteRefusal.DestinationOverlapsRepository,
			RefusalMessage = "refused"
		};

		Assert.False(PromptPackFilePath.TryForWrittenPack(BuildDestination(), refused, out PromptPackFilePath packPath));
		Assert.False(packPath.IsDeclared);
	}

	/// <summary>
	/// Both separators are refused on every platform, not only the platform's own: a name carrying
	/// the other one is still a caller composing a path where a name was asked for.
	/// </summary>
	[Theory]
	[InlineData("")]
	[InlineData("   ")]
	[InlineData("nested/pack.md")]
	[InlineData("nested\\pack.md")]
	[InlineData("../pack.md")]
	[InlineData("..\\pack.md")]
	public void ANameThatIsNotOneComponentProducesNoPackPath(string fileNameWritten)
	{
		Assert.False(
			PromptPackFilePath.TryForWrittenPack(BuildDestination(), Wrote(fileNameWritten), out PromptPackFilePath packPath));
		Assert.False(packPath.IsDeclared);
	}

	[Fact]
	public void AnAbsoluteNameProducesNoPackPath()
	{
		string rootedName = Path.Combine(Path.GetTempPath(), _PACK_FILE_NAME);

		Assert.False(
			PromptPackFilePath.TryForWrittenPack(BuildDestination(), Wrote(rootedName), out PromptPackFilePath packPath));
		Assert.False(packPath.IsDeclared);
	}

	[Fact]
	public void AnEstablishedDestinationIsRequired()
	{
		Assert.False(
			PromptPackFilePath.TryForWrittenPack(default, Wrote(_PACK_FILE_NAME), out PromptPackFilePath packPath));
		Assert.False(packPath.IsDeclared);
	}

	[Fact]
	public void TheDefaultPackPathNamesNothing()
	{
		PromptPackFilePath packPath = default;

		Assert.False(packPath.IsDeclared);
		Assert.Empty(packPath.AbsolutePath ?? string.Empty);
	}

	[Fact]
	public void AWriteResultIsRequired()
	{
		Assert.Throws<ArgumentNullException>(() =>
			PromptPackFilePath.TryForWrittenPack(BuildDestination(), null!, out PromptPackFilePath _));
	}

	private static DocumentationDestinationFolderPath BuildDestination()
	{
		return DocumentationDestinationFolderPath.FromSuppliedText(
			Path.Combine(Path.GetTempPath(), _DESTINATION_FOLDER_NAME));
	}

	private static ArtifactWriteResult Wrote(string fileNameWritten)
	{
		return new ArtifactWriteResult
		{
			Wrote = true,
			FileNameWritten = fileNameWritten,
			Refusal = ArtifactWriteRefusal.None,
			RefusalMessage = string.Empty
		};
	}
}
