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

	private static AtomicArtifactWriter BuildWriter(TemporaryFolder repositoryFolder, TemporaryFolder destinationFolder) =>
		new(
			RepositoryFolderPath.FromSuppliedText(repositoryFolder.Path),
			DocumentationDestinationFolderPath.FromSuppliedText(destinationFolder.Path));
}
