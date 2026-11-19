using MigrateAuth.Cli.Session;
using MigrateAuth.Cli.Tests.Session;
using MigrateAuth.Cli.Writing;

using Xunit;

namespace MigrateAuth.Cli.Tests.Writing;

/// <summary>
/// The write itself: refused when the boundary says so, atomic when it happens, and never
/// overwriting a report that is already there.
///
/// The same-day double report is the edge case the requirements name verbatim, and what it asserts
/// is not only that the second file is suffixed but that the FIRST is byte-for-byte unchanged -
/// which is the part a check-then-create implementation gets wrong under a race and an
/// overwriting one gets wrong always.
/// </summary>
public sealed class AtomicArtifactWriterTests
{
	private const string _FIRST_BODY = "# Report one\n\nCoverage: nothing was scanned.\n";
	private const string _SECOND_BODY = "# Report two\n\nCoverage: nothing was scanned either.\n";
	private const string _TEMPORARY_FILE_SEARCH_PATTERN = "*.writing";
	private const string _OUTPUT_FOLDER_NAME = "output";
	private const string _RELATIVE_DESTINATION_TEXT = "./Outputs";

	private static readonly DateOnly _artifactDate = new(2026, 9, 18);

	[Fact]
	public void TheFirstReportOfTheDayCarriesNoSuffix()
	{
		using TemporaryFolder repositoryFolder = TemporaryFolder.Create();
		using TemporaryFolder destinationFolder = TemporaryFolder.Create();

		ArtifactWriteResult result = BuildWriter(repositoryFolder, destinationFolder)
			.WriteShareableArtifact(OutputArtifactKind.MarkdownReport, _artifactDate, _FIRST_BODY);

		Assert.True(result.Wrote);
		Assert.Equal("basic-auth-migration-2026-09-18.md", result.FileNameWritten);
		Assert.Equal(ArtifactWriteRefusal.None, result.Refusal);
	}

	/// <summary>The same-day double report edge case.</summary>
	[Fact]
	public void ReportTwiceOnTheSameDayProducesTwoFilesAndLeavesTheFirstUnchanged()
	{
		using TemporaryFolder repositoryFolder = TemporaryFolder.Create();
		using TemporaryFolder destinationFolder = TemporaryFolder.Create();

		AtomicArtifactWriter writer = BuildWriter(repositoryFolder, destinationFolder);

		ArtifactWriteResult first = writer.WriteShareableArtifact(OutputArtifactKind.MarkdownReport, _artifactDate, _FIRST_BODY);
		byte[] firstBytesAfterFirstWrite = File.ReadAllBytes(destinationFolder.Combine(first.FileNameWritten));

		ArtifactWriteResult second = writer.WriteShareableArtifact(OutputArtifactKind.MarkdownReport, _artifactDate, _SECOND_BODY);

		Assert.Equal("basic-auth-migration-2026-09-18.md", first.FileNameWritten);
		Assert.Equal("basic-auth-migration-2026-09-18-2.md", second.FileNameWritten);
		Assert.Equal(firstBytesAfterFirstWrite, File.ReadAllBytes(destinationFolder.Combine(first.FileNameWritten)));
		Assert.Contains(_SECOND_BODY, File.ReadAllText(destinationFolder.Combine(second.FileNameWritten)), StringComparison.Ordinal);
	}

	[Fact]
	public void TheThirdReportOfTheDayIsSuffixedThree()
	{
		using TemporaryFolder repositoryFolder = TemporaryFolder.Create();
		using TemporaryFolder destinationFolder = TemporaryFolder.Create();

		AtomicArtifactWriter writer = BuildWriter(repositoryFolder, destinationFolder);

		writer.WriteShareableArtifact(OutputArtifactKind.MarkdownReport, _artifactDate, _FIRST_BODY);
		writer.WriteShareableArtifact(OutputArtifactKind.MarkdownReport, _artifactDate, _FIRST_BODY);

		ArtifactWriteResult third = writer.WriteShareableArtifact(OutputArtifactKind.MarkdownReport, _artifactDate, _SECOND_BODY);

		Assert.Equal("basic-auth-migration-2026-09-18-3.md", third.FileNameWritten);
	}

	/// <summary>
	/// The artifact follows the same pattern and the same collision rule with its own
	/// extension, and the two do not collide with one another.
	/// </summary>
	[Fact]
	public void TheDataArtifactHasItsOwnNameSequence()
	{
		using TemporaryFolder repositoryFolder = TemporaryFolder.Create();
		using TemporaryFolder destinationFolder = TemporaryFolder.Create();

		AtomicArtifactWriter writer = BuildWriter(repositoryFolder, destinationFolder);

		writer.WriteShareableArtifact(OutputArtifactKind.MarkdownReport, _artifactDate, _FIRST_BODY);

		ArtifactWriteResult artifact = writer.WriteShareableArtifact(
			OutputArtifactKind.StructuredDataArtifact,
			_artifactDate,
			"{}");

		Assert.Equal("basic-auth-migration-2026-09-18.json", artifact.FileNameWritten);
	}

	[Fact]
	public void TheReportCarriesTheHandlingHeaderAsItsFirstLine()
	{
		using TemporaryFolder repositoryFolder = TemporaryFolder.Create();
		using TemporaryFolder destinationFolder = TemporaryFolder.Create();

		ArtifactWriteResult result = BuildWriter(repositoryFolder, destinationFolder)
			.WriteShareableArtifact(OutputArtifactKind.MarkdownReport, _artifactDate, _FIRST_BODY);

		string[] lines = File.ReadAllLines(destinationFolder.Combine(result.FileNameWritten));

		Assert.Equal(ArtifactHandlingNotice.MarkdownHeader, lines[0]);
		Assert.Contains("credential material", ArtifactHandlingNotice.NOTICE_TEXT, StringComparison.Ordinal);
	}

	/// <summary>
	/// The structured artifact carries the same notice, and carries it as a field its own
	/// serializer places rather than as text prepended to the bytes - which would make the
	/// document invalid for any consumer that parses it.
	/// </summary>
	[Fact]
	public void TheDataArtifactBodyIsHandedOnUntouched()
	{
		using TemporaryFolder repositoryFolder = TemporaryFolder.Create();
		using TemporaryFolder destinationFolder = TemporaryFolder.Create();

		ArtifactWriteResult result = BuildWriter(repositoryFolder, destinationFolder)
			.WriteShareableArtifact(OutputArtifactKind.StructuredDataArtifact, _artifactDate, "{\"findings\":[]}");

		Assert.Equal("{\"findings\":[]}", File.ReadAllText(destinationFolder.Combine(result.FileNameWritten)));
		Assert.Equal("handlingNotice", ArtifactHandlingNotice.STRUCTURED_DATA_FIELD_NAME);
	}

	[Fact]
	public void NoTemporaryFileSurvivesAWrite()
	{
		using TemporaryFolder repositoryFolder = TemporaryFolder.Create();
		using TemporaryFolder destinationFolder = TemporaryFolder.Create();

		BuildWriter(repositoryFolder, destinationFolder)
			.WriteShareableArtifact(OutputArtifactKind.MarkdownReport, _artifactDate, _FIRST_BODY);

		Assert.Empty(Directory.GetFiles(destinationFolder.Path, _TEMPORARY_FILE_SEARCH_PATTERN));
	}

	[Fact]
	public void AWriteIntoTheRepositoryIsRefusedAndNothingIsCreated()
	{
		using TemporaryFolder repositoryFolder = TemporaryFolder.Create();

		string destinationPath = Path.Combine(repositoryFolder.Path, _OUTPUT_FOLDER_NAME);
		AtomicArtifactWriter writer = new(
			RepositoryFolderPath.FromSuppliedText(repositoryFolder.Path),
			DocumentationDestinationFolderPath.FromSuppliedText(destinationPath));

		ArtifactWriteResult result = writer.WriteShareableArtifact(
			OutputArtifactKind.MarkdownReport,
			_artifactDate,
			_FIRST_BODY);

		Assert.False(result.Wrote);
		Assert.Equal(ArtifactWriteRefusal.DestinationOverlapsRepository, result.Refusal);
		Assert.Equal(string.Empty, result.FileNameWritten);
		Assert.False(Directory.Exists(destinationPath));
		Assert.Contains(WriteBoundary.REFUSAL_REMEDY_MESSAGE, result.RefusalMessage, StringComparison.Ordinal);
	}

	/// <summary>The overlap refusal names the destination as supplied, never its canonical path.</summary>
	[Fact]
	public void AnOverlapRefusalNamesTheSuppliedDestinationAndNotTheCanonicalPath()
	{
		using TemporaryFolder repositoryFolder = TemporaryFolder.Create();

		string destinationPath = Path.Combine(repositoryFolder.Path, _OUTPUT_FOLDER_NAME);
		AtomicArtifactWriter writer = new(
			RepositoryFolderPath.FromSuppliedText(repositoryFolder.Path),
			DocumentationDestinationFolderPath.FromResolvedText(destinationPath, _RELATIVE_DESTINATION_TEXT));

		ArtifactWriteResult result = writer.WriteShareableArtifact(OutputArtifactKind.MarkdownReport, _artifactDate, _FIRST_BODY);

		Assert.Equal(ArtifactWriteRefusal.DestinationOverlapsRepository, result.Refusal);
		Assert.Contains(_RELATIVE_DESTINATION_TEXT, result.RefusalMessage, StringComparison.Ordinal);
		Assert.DoesNotContain(repositoryFolder.Path, result.RefusalMessage, StringComparison.OrdinalIgnoreCase);
	}

	/// <summary>The cannot-be-written-to text names the folder as supplied (relative here), not its label or canonical path.</summary>
	[Fact]
	public void ADestinationThatCannotBeWrittenToIsNamedAsSuppliedRelativeToTheWorkingFolder()
	{
		using TemporaryFolder repositoryFolder = TemporaryFolder.Create();
		using TemporaryFolder blockingFolder = TemporaryFolder.Create();

		string blockingFile = blockingFolder.Combine("blocker.txt");
		File.WriteAllText(blockingFile, "placeholder");
		AtomicArtifactWriter writer = new(
			RepositoryFolderPath.FromSuppliedText(repositoryFolder.Path),
			DocumentationDestinationFolderPath.FromResolvedText(blockingFile, _RELATIVE_DESTINATION_TEXT));

		ArtifactWriteResult result = writer.WriteShareableArtifact(OutputArtifactKind.MarkdownReport, _artifactDate, _FIRST_BODY);

		Assert.False(result.Wrote);
		Assert.Equal(ArtifactWriteRefusal.DestinationNotWritable, result.Refusal);
		Assert.Equal(
			string.Format(
				System.Globalization.CultureInfo.InvariantCulture,
				AtomicArtifactWriter.DESTINATION_NOT_WRITABLE_MESSAGE_FORMAT,
				_RELATIVE_DESTINATION_TEXT),
			result.RefusalMessage);
		Assert.DoesNotContain(blockingFile, result.RefusalMessage, StringComparison.OrdinalIgnoreCase);
	}

	[Fact]
	public void ADestinationThatCannotBeWrittenToIsNamedAsSuppliedWhenItIsAbsolute()
	{
		using TemporaryFolder repositoryFolder = TemporaryFolder.Create();
		using TemporaryFolder blockingFolder = TemporaryFolder.Create();

		string blockingFile = blockingFolder.Combine("blocker.txt");
		File.WriteAllText(blockingFile, "placeholder");
		AtomicArtifactWriter writer = new(
			RepositoryFolderPath.FromSuppliedText(repositoryFolder.Path),
			DocumentationDestinationFolderPath.FromSuppliedText(blockingFile));

		ArtifactWriteResult result = writer.WriteShareableArtifact(OutputArtifactKind.MarkdownReport, _artifactDate, _FIRST_BODY);

		Assert.Equal(ArtifactWriteRefusal.DestinationNotWritable, result.Refusal);
		Assert.Contains(blockingFile, result.RefusalMessage, StringComparison.Ordinal);
	}

	/// <summary>A successful write carries the supplied form of the folder for the "Written" line, and never the canonical path.</summary>
	[Fact]
	public void ASuccessfulWriteCarriesTheSuppliedFolderTextForTheConsole()
	{
		using TemporaryFolder repositoryFolder = TemporaryFolder.Create();
		using TemporaryFolder destinationFolder = TemporaryFolder.Create();

		AtomicArtifactWriter writer = new(
			RepositoryFolderPath.FromSuppliedText(repositoryFolder.Path),
			DocumentationDestinationFolderPath.FromResolvedText(destinationFolder.Path, _RELATIVE_DESTINATION_TEXT));

		ArtifactWriteResult result = writer.WriteShareableArtifact(OutputArtifactKind.MarkdownReport, _artifactDate, _FIRST_BODY);

		Assert.True(result.Wrote);
		Assert.Equal(_RELATIVE_DESTINATION_TEXT, result.DestinationConsoleText);
	}

	[Fact]
	public void AWriteWhereTheDestinationIsTheRepositoryIsRefused()
	{
		using TemporaryFolder repositoryFolder = TemporaryFolder.Create();

		AtomicArtifactWriter writer = new(
			RepositoryFolderPath.FromSuppliedText(repositoryFolder.Path),
			DocumentationDestinationFolderPath.FromSuppliedText(repositoryFolder.Path));

		ArtifactWriteResult result = writer.WriteShareableArtifact(
			OutputArtifactKind.MarkdownReport,
			_artifactDate,
			_FIRST_BODY);

		Assert.False(result.Wrote);
		Assert.Empty(Directory.GetFiles(repositoryFolder.Path));
	}

	/// <summary>
	/// The report and the artifact carry the state file's permissions, where the platform
	/// expresses permissions as a file mode. On Windows nothing is applied and the file inherits
	/// the destination folder's access control list - see <see cref="RestrictiveFileCreation"/>.
	/// This assertion therefore measures the Unix half and states the Windows half.
	/// </summary>
	[Fact]
	public void TheShareableArtifactsCarryTheStateFilesPermissionsWhereThePlatformHasThem()
	{
		using TemporaryFolder repositoryFolder = TemporaryFolder.Create();
		using TemporaryFolder destinationFolder = TemporaryFolder.Create();

		ArtifactWriteResult result = BuildWriter(repositoryFolder, destinationFolder)
			.WriteShareableArtifact(OutputArtifactKind.MarkdownReport, _artifactDate, _FIRST_BODY);

		FileInfo writtenFile = new(destinationFolder.Combine(result.FileNameWritten));

		Assert.True(writtenFile.Exists);

		if (!OperatingSystem.IsWindows())
		{
			Assert.Equal(RestrictiveFileCreation.RESTRICTIVE_UNIX_FILE_MODE, writtenFile.UnixFileMode);
		}
	}

	[Theory]
	[InlineData(OutputArtifactKind.MarkdownReport, 1, "basic-auth-migration-2026-09-18.md")]
	[InlineData(OutputArtifactKind.MarkdownReport, 2, "basic-auth-migration-2026-09-18-2.md")]
	[InlineData(OutputArtifactKind.StructuredDataArtifact, 1, "basic-auth-migration-2026-09-18.json")]
	[InlineData(OutputArtifactKind.StructuredDataArtifact, 7, "basic-auth-migration-2026-09-18-7.json")]
	public void TheNamingRuleIsTheDatedFileNameWithACollisionSuffix(OutputArtifactKind kind, int ordinal, string expectedFileName)
	{
		Assert.Equal(expectedFileName, OutputArtifactNaming.BuildFileName(kind, _artifactDate, ordinal));
	}

	[Fact]
	public void TheLowestCollisionSuffixIsTwo()
	{
		Assert.Equal(2, OutputArtifactNaming.FIRST_COLLISION_SUFFIX);
		Assert.Equal(
			$"{OutputArtifactNaming.FILE_NAME_STEM}-2026-09-18{OutputArtifactNaming.MARKDOWN_FILE_EXTENSION}",
			OutputArtifactNaming.BuildFileName(OutputArtifactKind.MarkdownReport, _artifactDate, 1));
	}

	/// <summary>The string overload records nothing, even for a writer built with a holder.</summary>
	[Fact]
	public void TheStringOverloadDoesNotRecordTheReportInAHolderTheWriterWasBuiltWith()
	{
		using TemporaryFolder repositoryFolder = TemporaryFolder.Create();
		using TemporaryFolder destinationFolder = TemporaryFolder.Create();

		ReportWriteHolder holder = new();
		AtomicArtifactWriter writer = new(
			RepositoryFolderPath.FromSuppliedText(repositoryFolder.Path),
			DocumentationDestinationFolderPath.FromSuppliedText(destinationFolder.Path),
			holder);

		ArtifactWriteResult result = writer.WriteShareableArtifact(OutputArtifactKind.MarkdownReport, _artifactDate, _FIRST_BODY);

		Assert.True(result.Wrote);
		Assert.Null(holder.Current);
	}

	[Fact]
	public void AWriterBuiltWithAHolderWritesTheStringOverloadByteForByteLikeOneWithout()
	{
		using TemporaryFolder repositoryFolder = TemporaryFolder.Create();
		using TemporaryFolder plainFolder = TemporaryFolder.Create();
		using TemporaryFolder holderFolder = TemporaryFolder.Create();

		ArtifactWriteResult plain = BuildWriter(repositoryFolder, plainFolder)
			.WriteShareableArtifact(OutputArtifactKind.MarkdownReport, _artifactDate, _FIRST_BODY);
		ArtifactWriteResult withHolder = new AtomicArtifactWriter(
			RepositoryFolderPath.FromSuppliedText(repositoryFolder.Path),
			DocumentationDestinationFolderPath.FromSuppliedText(holderFolder.Path),
			new ReportWriteHolder())
			.WriteShareableArtifact(OutputArtifactKind.MarkdownReport, _artifactDate, _FIRST_BODY);

		Assert.Equal(plain.FileNameWritten, withHolder.FileNameWritten);
		Assert.Equal(
			File.ReadAllBytes(plainFolder.Combine(plain.FileNameWritten)),
			File.ReadAllBytes(holderFolder.Combine(withHolder.FileNameWritten)));
	}

	[Fact]
	public void TheStringOverloadRefusesThePromptPackKind()
	{
		using TemporaryFolder repositoryFolder = TemporaryFolder.Create();
		using TemporaryFolder destinationFolder = TemporaryFolder.Create();

		Assert.Throws<ArgumentOutOfRangeException>(
			() => BuildWriter(repositoryFolder, destinationFolder)
				.WriteShareableArtifact(OutputArtifactKind.PromptPack, _artifactDate, _FIRST_BODY));
		Assert.Empty(Directory.GetFileSystemEntries(destinationFolder.Path));
	}

	[Fact]
	public void ThePackCollisionMessageSaysPromptPackWhereTheReportMessageSaysReport()
	{
		Assert.Contains("prompt pack", AtomicArtifactWriter.PACK_NAME_COLLISION_LIMIT_MESSAGE_FORMAT, StringComparison.Ordinal);
		Assert.DoesNotContain("today", AtomicArtifactWriter.PACK_NAME_COLLISION_LIMIT_MESSAGE_FORMAT, StringComparison.Ordinal);
		Assert.Contains("today's report", AtomicArtifactWriter.NAME_COLLISION_LIMIT_MESSAGE_FORMAT, StringComparison.Ordinal);
	}

	private static AtomicArtifactWriter BuildWriter(TemporaryFolder repositoryFolder, TemporaryFolder destinationFolder) =>
		new(
			RepositoryFolderPath.FromSuppliedText(repositoryFolder.Path),
			DocumentationDestinationFolderPath.FromSuppliedText(destinationFolder.Path));
}
