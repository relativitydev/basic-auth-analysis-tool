using System.Reflection;
using System.Text;

using MigrateAuth.Cli.Session;
using MigrateAuth.Cli.Tests.Session;
using MigrateAuth.Cli.Writing;

using Xunit;

namespace MigrateAuth.Cli.Tests.Writing;

/// <summary>
/// The streaming siblings the writing contract requires, and the proof that they are the SAME write rather than a
/// second one.
///
/// The contract states the artifact is written streaming, "not serialized to a string in memory". WHAT IS
/// ASSERTED HERE IS THE SHAPE, NOT THE SIZE: that the signature cannot accept a whole body, and that the
/// temporary-then-rename, the handling header and the collision rule behave identically to the
/// method already in use. A unit test measuring process memory would be a slow test
/// asserting a number the runtime is free to change.
/// </summary>
public sealed class AtomicArtifactStreamingTests
{
	private const string _CONTROL_BODY = "# Report\n\nCoverage: nothing was scanned.\n";
	private const string _TEMPORARY_FILE_SEARCH_PATTERN = "*.writing";

	private static readonly DateOnly _artifactDate = new(2026, 9, 18);

	/// <summary>
	/// The structural half of the claim. A caller CANNOT hand this method a completed body, because
	/// the only parameter it has is a sink to write into - which is what makes "streaming" a
	/// property of the signature rather than of a convention someone follows.
	/// </summary>
	[Fact]
	public void TheStreamingWriteTakesASinkRatherThanACompletedBody()
	{
		MethodInfo streaming = typeof(AtomicArtifactWriter)
			.GetMethod(nameof(AtomicArtifactWriter.WriteShareableArtifactStreaming))!;
		MethodInfo structuredStreaming = typeof(AtomicArtifactWriter)
			.GetMethod(nameof(AtomicArtifactWriter.WriteStructuredDataArtifactStreaming))!;

		Assert.Equal(typeof(Action<TextWriter>), streaming.GetParameters()[^1].ParameterType);
		Assert.Equal(typeof(Action<Stream>), structuredStreaming.GetParameters()[^1].ParameterType);
		Assert.DoesNotContain(streaming.GetParameters(), parameter => parameter.ParameterType == typeof(string));
		Assert.DoesNotContain(structuredStreaming.GetParameters(), parameter => parameter.ParameterType == typeof(string));
	}

	[Fact]
	public void AStreamedReportIsByteForByteWhatTheStringWriteWouldHaveProduced()
	{
		using TemporaryFolder repositoryFolder = TemporaryFolder.Create();
		using TemporaryFolder destinationFolder = TemporaryFolder.Create();
		using TemporaryFolder secondDestinationFolder = TemporaryFolder.Create();

		ArtifactWriteResult streamed = BuildWriter(repositoryFolder, destinationFolder)
			.WriteShareableArtifactStreaming(
				OutputArtifactKind.MarkdownReport,
				_artifactDate,
				writer => writer.Write(_CONTROL_BODY));

		ArtifactWriteResult asString = BuildWriter(repositoryFolder, secondDestinationFolder)
			.WriteShareableArtifact(OutputArtifactKind.MarkdownReport, _artifactDate, _CONTROL_BODY);

		Assert.Equal(asString.FileNameWritten, streamed.FileNameWritten);
		Assert.Equal(
			File.ReadAllBytes(secondDestinationFolder.Combine(asString.FileNameWritten)),
			File.ReadAllBytes(destinationFolder.Combine(streamed.FileNameWritten)));
	}

	[Fact]
	public void AStreamedReportCarriesTheHandlingHeaderAsItsFirstLine()
	{
		using TemporaryFolder repositoryFolder = TemporaryFolder.Create();
		using TemporaryFolder destinationFolder = TemporaryFolder.Create();

		ArtifactWriteResult result = BuildWriter(repositoryFolder, destinationFolder)
			.WriteShareableArtifactStreaming(
				OutputArtifactKind.MarkdownReport,
				_artifactDate,
				writer => writer.Write(_CONTROL_BODY));

		string[] lines = File.ReadAllLines(destinationFolder.Combine(result.FileNameWritten));

		Assert.Equal(ArtifactHandlingNotice.MarkdownHeader, lines[0]);
	}

	/// <summary>
	/// The artifact's notice is a field its own serializer places, so nothing is prepended here -
	/// text before the bytes would make the document invalid for the consumers it exists to serve.
	/// </summary>
	[Fact]
	public void AStreamedDataArtifactGetsNoPrependedHeader()
	{
		using TemporaryFolder repositoryFolder = TemporaryFolder.Create();
		using TemporaryFolder destinationFolder = TemporaryFolder.Create();

		ArtifactWriteResult result = BuildWriter(repositoryFolder, destinationFolder)
			.WriteStructuredDataArtifactStreaming(
				_artifactDate,
				stream => stream.Write(Encoding.UTF8.GetBytes("{\"findings\":[]}")));

		Assert.Equal("basic-auth-migration-2026-09-18.json", result.FileNameWritten);
		Assert.Equal("{\"findings\":[]}", File.ReadAllText(destinationFolder.Combine(result.FileNameWritten)));
	}

	/// <summary>The collision rule is the same one, so a second report of the day is suffixed.</summary>
	[Fact]
	public void TheCollisionRuleAppliesToAStreamedWriteToo()
	{
		using TemporaryFolder repositoryFolder = TemporaryFolder.Create();
		using TemporaryFolder destinationFolder = TemporaryFolder.Create();

		AtomicArtifactWriter writer = BuildWriter(repositoryFolder, destinationFolder);

		ArtifactWriteResult first = writer.WriteShareableArtifactStreaming(
			OutputArtifactKind.MarkdownReport,
			_artifactDate,
			sink => sink.Write(_CONTROL_BODY));
		byte[] firstBytes = File.ReadAllBytes(destinationFolder.Combine(first.FileNameWritten));

		ArtifactWriteResult second = writer.WriteShareableArtifactStreaming(
			OutputArtifactKind.MarkdownReport,
			_artifactDate,
			sink => sink.Write("# Another report\n"));

		Assert.Equal("basic-auth-migration-2026-09-18.md", first.FileNameWritten);
		Assert.Equal("basic-auth-migration-2026-09-18-2.md", second.FileNameWritten);
		Assert.Equal(firstBytes, File.ReadAllBytes(destinationFolder.Combine(first.FileNameWritten)));
	}

	[Fact]
	public void NoTemporaryFileSurvivesAStreamedWrite()
	{
		using TemporaryFolder repositoryFolder = TemporaryFolder.Create();
		using TemporaryFolder destinationFolder = TemporaryFolder.Create();

		BuildWriter(repositoryFolder, destinationFolder).WriteShareableArtifactStreaming(
			OutputArtifactKind.MarkdownReport,
			_artifactDate,
			writer => writer.Write(_CONTROL_BODY));

		Assert.Empty(Directory.GetFiles(destinationFolder.Path, _TEMPORARY_FILE_SEARCH_PATTERN));
	}

	/// <summary>
	/// The boundary is checked before the temporary is created, so a refused streamed write leaves
	/// the destination uncreated exactly as the string write does - and the body delegate never
	/// runs at all.
	/// </summary>
	[Fact]
	public void AStreamedWriteIntoTheRepositoryIsRefusedAndTheBodyNeverRuns()
	{
		using TemporaryFolder repositoryFolder = TemporaryFolder.Create();

		string destinationPath = Path.Combine(repositoryFolder.Path, "output");
		AtomicArtifactWriter writer = new(
			RepositoryFolderPath.FromSuppliedText(repositoryFolder.Path),
			DocumentationDestinationFolderPath.FromSuppliedText(destinationPath));

		bool bodyRan = false;

		ArtifactWriteResult result = writer.WriteShareableArtifactStreaming(
			OutputArtifactKind.MarkdownReport,
			_artifactDate,
			sink =>
			{
				bodyRan = true;
				sink.Write(_CONTROL_BODY);
			});

		Assert.False(result.Wrote);
		Assert.Equal(ArtifactWriteRefusal.DestinationOverlapsRepository, result.Refusal);
		Assert.False(bodyRan);
		Assert.False(Directory.Exists(destinationPath));
	}

	/// <summary>
	/// Both shareable artifacts carry the state file's permissions where the platform expresses
	/// them as a file mode, and the streaming opener is the same one - which is what this asserts
	/// on the Unix half and states on the Windows half, exactly as the existing write does.
	/// </summary>
	[Fact]
	public void AStreamedArtifactCarriesTheSamePermissionsAsTheStringWrite()
	{
		using TemporaryFolder repositoryFolder = TemporaryFolder.Create();
		using TemporaryFolder destinationFolder = TemporaryFolder.Create();

		ArtifactWriteResult result = BuildWriter(repositoryFolder, destinationFolder)
			.WriteShareableArtifactStreaming(
				OutputArtifactKind.MarkdownReport,
				_artifactDate,
				writer => writer.Write(_CONTROL_BODY));

		FileInfo writtenFile = new(destinationFolder.Combine(result.FileNameWritten));

		Assert.True(writtenFile.Exists);

		if (!OperatingSystem.IsWindows())
		{
			Assert.Equal(RestrictiveFileCreation.RESTRICTIVE_UNIX_FILE_MODE, writtenFile.UnixFileMode);
		}
	}

	private static AtomicArtifactWriter BuildWriter(TemporaryFolder repositoryFolder, TemporaryFolder destinationFolder) =>
		new(
			RepositoryFolderPath.FromSuppliedText(repositoryFolder.Path),
			DocumentationDestinationFolderPath.FromSuppliedText(destinationFolder.Path));
}
