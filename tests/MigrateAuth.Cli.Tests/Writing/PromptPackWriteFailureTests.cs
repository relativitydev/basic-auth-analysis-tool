using MigrateAuth.Cli.Agent;
using MigrateAuth.Cli.Session;
using MigrateAuth.Cli.Tests.Session;
using MigrateAuth.Cli.Writing;

using Xunit;

namespace MigrateAuth.Cli.Tests.Writing;

/// <summary>
/// At writer level: a failure injected in the pack write - the destination, the temporary write,
/// the rename - leaves no partial pack and leaves an existing report and data artifact byte-identical.
/// </summary>
public sealed class PromptPackWriteFailureTests
{
	private const string _STEM = AgentEnvironmentCatalog.CLAUDE_CODE_PACK_STEM;
	private const string _PARTIAL_TEXT = "Half of the instruction text";
	private const string _INJECTED_FAILURE_MESSAGE = "Injected failure.";
	private const string _DATA_BODY = "{\"findings\":[]}";
	private const string _BLOCKER_FILE_NAME = "blocker.txt";

	[Fact]
	public void ADestinationThatCannotBeCreatedLeavesNoPackAndTheNeighbouringArtifactsUntouched()
	{
		using TemporaryFolder repositoryFolder = TemporaryFolder.Create();
		using TemporaryFolder destinationFolder = TemporaryFolder.Create();
		using TemporaryFolder blockingFolder = TemporaryFolder.Create();

		string blockingFile = blockingFolder.Combine(_BLOCKER_FILE_NAME);

		File.WriteAllText(blockingFile, "placeholder");

		AtomicArtifactWriter existingWriter = ReportAppendFixture.BuildWriter(repositoryFolder, destinationFolder);
		ExistingArtifacts existing = WriteExistingArtifacts(existingWriter, destinationFolder);
		AtomicArtifactWriter blockedWriter = new(
			RepositoryFolderPath.FromSuppliedText(repositoryFolder.Path),
			DocumentationDestinationFolderPath.FromSuppliedText(blockingFile));

		ArtifactWriteResult result = blockedWriter.WritePromptPackStreaming(_STEM, sink => sink.Write(_PARTIAL_TEXT));

		Assert.False(result.Wrote);
		Assert.Equal(ArtifactWriteRefusal.DestinationNotWritable, result.Refusal);
		Assert.Equal(string.Empty, result.FileNameWritten);
		Assert.Equal("placeholder", File.ReadAllText(blockingFile));
		existing.AssertUnchanged();
	}

	[Theory]
	[InlineData(typeof(IOException))]
	[InlineData(typeof(UnauthorizedAccessException))]
	[InlineData(typeof(NotSupportedException))]
	public void AFailureWhileTheTemporaryIsWrittenLeavesNoPartialPackAndTheNeighbouringArtifactsUntouched(Type failureType)
	{
		using TemporaryFolder repositoryFolder = TemporaryFolder.Create();
		using TemporaryFolder destinationFolder = TemporaryFolder.Create();

		AtomicArtifactWriter writer = ReportAppendFixture.BuildWriter(repositoryFolder, destinationFolder);
		ExistingArtifacts existing = WriteExistingArtifacts(writer, destinationFolder);

		ArtifactWriteResult result = writer.WritePromptPackStreaming(
			_STEM,
			sink =>
			{
				sink.Write(_PARTIAL_TEXT);
				sink.Flush();

				throw (Exception)Activator.CreateInstance(failureType, _INJECTED_FAILURE_MESSAGE)!;
			});

		Assert.False(result.Wrote);
		Assert.Equal(ArtifactWriteRefusal.DestinationNotWritable, result.Refusal);
		Assert.False(File.Exists(destinationFolder.Combine(OutputArtifactNaming.BuildPromptPackFileName(_STEM, 1))));
		Assert.Empty(Directory.GetFiles(destinationFolder.Path, ReportAppendFixture.TEMPORARY_FILE_SEARCH_PATTERN));
		existing.AssertUnchanged();
	}

	/// <summary>The rename cannot take the final name because a folder holds it: a failure that is not a name collision.</summary>
	[Fact]
	public void AFailureAtTheRenameLeavesNoPartialPackAndTheNeighbouringArtifactsUntouched()
	{
		using TemporaryFolder repositoryFolder = TemporaryFolder.Create();
		using TemporaryFolder destinationFolder = TemporaryFolder.Create();

		AtomicArtifactWriter writer = ReportAppendFixture.BuildWriter(repositoryFolder, destinationFolder);
		ExistingArtifacts existing = WriteExistingArtifacts(writer, destinationFolder);
		string blockedName = destinationFolder.Combine(OutputArtifactNaming.BuildPromptPackFileName(_STEM, 1));

		Directory.CreateDirectory(blockedName);

		ArtifactWriteResult result = writer.WritePromptPackStreaming(_STEM, sink => sink.Write(_PARTIAL_TEXT));

		Assert.False(result.Wrote);
		Assert.Equal(ArtifactWriteRefusal.DestinationNotWritable, result.Refusal);
		Assert.True(Directory.Exists(blockedName));
		Assert.Empty(Directory.GetFileSystemEntries(blockedName));
		Assert.Empty(Directory.GetFiles(destinationFolder.Path, ReportAppendFixture.TEMPORARY_FILE_SEARCH_PATTERN));
		existing.AssertUnchanged();
	}

	[Fact]
	public void ABodyThatThrowsSomethingElseLeavesNoTemporaryAndNoPack()
	{
		using TemporaryFolder repositoryFolder = TemporaryFolder.Create();
		using TemporaryFolder destinationFolder = TemporaryFolder.Create();

		AtomicArtifactWriter writer = ReportAppendFixture.BuildWriter(repositoryFolder, destinationFolder);
		ExistingArtifacts existing = WriteExistingArtifacts(writer, destinationFolder);

		Assert.Throws<InvalidOperationException>(() => writer.WritePromptPackStreaming(
			_STEM,
			sink =>
			{
				sink.Write(_PARTIAL_TEXT);

				throw new InvalidOperationException(_INJECTED_FAILURE_MESSAGE);
			}));

		Assert.False(File.Exists(destinationFolder.Combine(OutputArtifactNaming.BuildPromptPackFileName(_STEM, 1))));
		Assert.Empty(Directory.GetFiles(destinationFolder.Path, ReportAppendFixture.TEMPORARY_FILE_SEARCH_PATTERN));
		existing.AssertUnchanged();
	}

	/// <summary>A failed pack write does not consume a name: the next success takes the unsuffixed one.</summary>
	[Fact]
	public void AFailedPackWriteDoesNotConsumeAName()
	{
		using TemporaryFolder repositoryFolder = TemporaryFolder.Create();
		using TemporaryFolder destinationFolder = TemporaryFolder.Create();

		AtomicArtifactWriter writer = ReportAppendFixture.BuildWriter(repositoryFolder, destinationFolder);

		writer.WritePromptPackStreaming(_STEM, _ => throw new IOException(_INJECTED_FAILURE_MESSAGE));

		ArtifactWriteResult next = writer.WritePromptPackStreaming(_STEM, sink => sink.Write(_PARTIAL_TEXT));

		Assert.Equal(OutputArtifactNaming.BuildPromptPackFileName(_STEM, 1), next.FileNameWritten);
	}

	private static ExistingArtifacts WriteExistingArtifacts(AtomicArtifactWriter writer, TemporaryFolder destinationFolder)
	{
		string reportPath = ReportAppendFixture.WriteReport(writer, destinationFolder);
		ArtifactWriteResult data = writer.WriteShareableArtifact(
			OutputArtifactKind.StructuredDataArtifact,
			ReportAppendFixture.ArtifactDate,
			_DATA_BODY);
		string dataPath = destinationFolder.Combine(data.FileNameWritten);

		return new ExistingArtifacts(reportPath, File.ReadAllBytes(reportPath), dataPath, File.ReadAllBytes(dataPath));
	}

	private sealed record ExistingArtifacts(string ReportPath, byte[] ReportBytes, string DataPath, byte[] DataBytes)
	{
		internal void AssertUnchanged()
		{
			Assert.Equal(ReportBytes, File.ReadAllBytes(ReportPath));
			Assert.Equal(DataBytes, File.ReadAllBytes(DataPath));
		}
	}
}
