using System.Collections.Immutable;
using System.Security.Cryptography;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Configuration;
using MigrateAuth.Cli.Findings;
using MigrateAuth.Cli.Reporting;
using MigrateAuth.Cli.Tests.Reporting;
using MigrateAuth.Cli.Tests.Session;
using MigrateAuth.Cli.Writing;

using Xunit;

namespace MigrateAuth.Cli.Tests.Writing;

/// <summary>
/// The report holder and the recording of the report in it: what it keeps, what never touches it,
/// that the bytes of the report are the same with and without it, and that a hasher that throws clears it and never
/// fails the write.
/// </summary>
public sealed class ReportWriteHolderTests
{
	private const string _RECORDED_FILE_NAME = "report.md";
	private const string _APPENDED_SECTION = "## Appended\n";
	private const string _PACK_STEM = "ai-assist-claude-code";
	private const string _DATA_BODY = "{}";
	private const string _HASHER_FAILURE_MESSAGE = "The hash is unavailable.";
	private const int _FIXTURE_FINDING_COUNT = 12;
	private const int _FIRST_FINDING_LINE = 3;
	private const string _FINDING_PATH_FORMAT = "module-{0}/File{0}.cs";

	[Fact]
	public void AFreshHolderIsEmptyAndRecordingKeepsTheNameLengthAndHash()
	{
		ReportWriteHolder holder = new();
		byte[] hash = ReportAppendFixture.Hash([1, 2, 3]);

		Assert.Null(holder.Current);

		holder.Record(_RECORDED_FILE_NAME, new ReportContentDigest(3, hash));

		Assert.Equal(_RECORDED_FILE_NAME, holder.Current!.FileName);
		Assert.Equal(3, holder.Current.Length);
		Assert.Equal(hash, holder.Current.Sha256);
	}

	[Fact]
	public void ClearForgetsTheRecordedReport()
	{
		ReportWriteHolder holder = new();

		holder.Record(_RECORDED_FILE_NAME, new ReportContentDigest(1, ReportAppendFixture.Hash([1])));
		holder.Clear();

		Assert.Null(holder.Current);
	}

	[Fact]
	public void RecordingRefusesABlankNameOrAMissingHash()
	{
		ReportWriteHolder holder = new();

		Assert.Throws<ArgumentException>(() => holder.Record(" ", new ReportContentDigest(1, [1])));
		Assert.Throws<ArgumentNullException>(() => holder.Record(_RECORDED_FILE_NAME, new ReportContentDigest(1, null!)));
	}

	[Theory]
	[InlineData("..\\outside.md")]
	[InlineData("../outside.md")]
	[InlineData("sub/report.md")]
	[InlineData("sub\\report.md")]
	[InlineData("..")]
	[InlineData("report..md")]
	[InlineData("/rooted.md")]
	[InlineData("C:\\rooted.md")]
	[InlineData("bad|name.md")]
	[InlineData("bad:name.md")]
	[InlineData("bad\0name.md")]
	public void ANameThatIsNotABareFileNameIsRefusedAndTheHolderKeepsItsValue(string fileName)
	{
		ReportWriteHolder holder = new();

		holder.Record(_RECORDED_FILE_NAME, new ReportContentDigest(1, ReportAppendFixture.Hash([1])));

		WrittenReport? before = holder.Current;

		Assert.Throws<ArgumentException>(() => holder.Record(fileName, new ReportContentDigest(1, ReportAppendFixture.Hash([1]))));
		Assert.Same(before, holder.Current);
	}

	[Fact]
	public void AStreamingReportWriteRecordsItsNameLengthAndSha256()
	{
		using TemporaryFolder repositoryFolder = TemporaryFolder.Create();
		using TemporaryFolder destinationFolder = TemporaryFolder.Create();

		ReportWriteHolder holder = new();
		AtomicArtifactWriter writer = ReportAppendFixture.BuildWriter(repositoryFolder, destinationFolder, holder);
		string reportPath = ReportAppendFixture.WriteReport(writer, destinationFolder);
		byte[] bytes = File.ReadAllBytes(reportPath);

		Assert.Equal(Path.GetFileName(reportPath), holder.Current!.FileName);
		Assert.Equal(bytes.LongLength, holder.Current.Length);
		Assert.Equal(ReportAppendFixture.Hash(bytes), holder.Current.Sha256);
	}

	[Fact]
	public void ASecondReportOfTheDayReplacesTheRecordWithItsCollisionName()
	{
		using TemporaryFolder repositoryFolder = TemporaryFolder.Create();
		using TemporaryFolder destinationFolder = TemporaryFolder.Create();

		ReportWriteHolder holder = new();
		AtomicArtifactWriter writer = ReportAppendFixture.BuildWriter(repositoryFolder, destinationFolder, holder);

		ReportAppendFixture.WriteReport(writer, destinationFolder);

		string secondPath = ReportAppendFixture.WriteReport(writer, destinationFolder);

		Assert.EndsWith("-2.md", holder.Current!.FileName, StringComparison.Ordinal);
		Assert.Equal(Path.GetFileName(secondPath), holder.Current.FileName);
	}

	[Fact]
	public void NothingButTheStreamingReportWriteTouchesTheHolder()
	{
		using TemporaryFolder repositoryFolder = TemporaryFolder.Create();
		using TemporaryFolder destinationFolder = TemporaryFolder.Create();

		ReportWriteHolder holder = new();
		AtomicArtifactWriter writer = ReportAppendFixture.BuildWriter(repositoryFolder, destinationFolder, holder);

		writer.WriteShareableArtifact(OutputArtifactKind.MarkdownReport, ReportAppendFixture.ArtifactDate, ReportAppendFixture.REPORT_BODY);
		writer.WriteShareableArtifact(OutputArtifactKind.StructuredDataArtifact, ReportAppendFixture.ArtifactDate, _DATA_BODY);
		writer.WriteStructuredDataArtifactStreaming(ReportAppendFixture.ArtifactDate, stream => stream.WriteByte(1));
		writer.WriteShareableArtifactStreaming(
			OutputArtifactKind.StructuredDataArtifact,
			ReportAppendFixture.ArtifactDate,
			sink => sink.Write(_DATA_BODY));
		writer.WritePromptPackStreaming(_PACK_STEM, sink => sink.Write(ReportAppendFixture.REPORT_BODY));

		Assert.Null(holder.Current);

		ReportAppendFixture.WriteReport(writer, destinationFolder);

		WrittenReport? recorded = holder.Current;

		writer.WritePromptPackStreaming(_PACK_STEM, sink => sink.Write(ReportAppendFixture.REPORT_BODY));
		writer.WriteShareableArtifact(OutputArtifactKind.StructuredDataArtifact, ReportAppendFixture.ArtifactDate, _DATA_BODY);

		Assert.Same(recorded, holder.Current);
	}

	[Fact]
	public void AFailedReportWriteLeavesThePreviousRecordInPlace()
	{
		using TemporaryFolder repositoryFolder = TemporaryFolder.Create();
		using TemporaryFolder destinationFolder = TemporaryFolder.Create();

		ReportWriteHolder holder = new();
		AtomicArtifactWriter writer = ReportAppendFixture.BuildWriter(repositoryFolder, destinationFolder, holder);

		ReportAppendFixture.WriteReport(writer, destinationFolder);

		WrittenReport? recorded = holder.Current;

		ArtifactWriteResult failed = writer.WriteShareableArtifactStreaming(
			OutputArtifactKind.MarkdownReport,
			ReportAppendFixture.ArtifactDate,
			_ => throw new IOException(_HASHER_FAILURE_MESSAGE));

		Assert.False(failed.Wrote);
		Assert.Same(recorded, holder.Current);
	}

	/// <summary>The writer with a holder produces the very bytes the writer without one produces, over several rendered reports.</summary>
	[Fact]
	public void TheReportIsByteIdenticalWithAndWithoutTheHolderOverTheDeterminismFixtures()
	{
		foreach (ReportContent content in BuildFixtureContents())
		{
			using TemporaryFolder repositoryFolder = TemporaryFolder.Create();
			using TemporaryFolder withoutFolder = TemporaryFolder.Create();
			using TemporaryFolder withFolder = TemporaryFolder.Create();

			string withoutPath = ReportAppendFixture.WriteReport(
				ReportAppendFixture.BuildWriter(repositoryFolder, withoutFolder),
				withoutFolder,
				RenderToText(content));
			ReportWriteHolder holder = new();
			string withPath = ReportAppendFixture.WriteReport(
				ReportAppendFixture.BuildWriter(repositoryFolder, withFolder, holder),
				withFolder,
				RenderToText(content));

			Assert.Equal(Path.GetFileName(withoutPath), Path.GetFileName(withPath));
			Assert.Equal(File.ReadAllBytes(withoutPath), File.ReadAllBytes(withPath));
			Assert.NotNull(holder.Current);
		}
	}

	/// <summary>The renderer's own streaming call into the writer: the bytes are those of the streamed body without a holder, too.</summary>
	[Fact]
	public void TheStreamingBodyIsByteIdenticalWithAndWithoutTheHolderWhenTheRendererWritesIntoTheSink()
	{
		ReportContent content = BuildFixtureContents()[1];

		using TemporaryFolder repositoryFolder = TemporaryFolder.Create();
		using TemporaryFolder withoutFolder = TemporaryFolder.Create();
		using TemporaryFolder withFolder = TemporaryFolder.Create();

		ArtifactWriteResult without = ReportAppendFixture.BuildWriter(repositoryFolder, withoutFolder)
			.WriteShareableArtifactStreaming(
				OutputArtifactKind.MarkdownReport,
				ReportAppendFixture.ArtifactDate,
				sink => MarkdownReportRenderer.Render(sink, content, new UnwiredGenericRecommendationProvider()));
		ArtifactWriteResult with = ReportAppendFixture.BuildWriter(repositoryFolder, withFolder, new ReportWriteHolder())
			.WriteShareableArtifactStreaming(
				OutputArtifactKind.MarkdownReport,
				ReportAppendFixture.ArtifactDate,
				sink => MarkdownReportRenderer.Render(sink, content, new UnwiredGenericRecommendationProvider()));

		Assert.Equal(
			File.ReadAllBytes(withoutFolder.Combine(without.FileNameWritten)),
			File.ReadAllBytes(withFolder.Combine(with.FileNameWritten)));
	}

	[Theory]
	[InlineData(typeof(CryptographicException))]
	[InlineData(typeof(PlatformNotSupportedException))]
	public void AHasherThatThrowsClearsTheHolderAndDoesNotFailTheWrite(Type exceptionType)
	{
		using TemporaryFolder repositoryFolder = TemporaryFolder.Create();
		using TemporaryFolder withoutFolder = TemporaryFolder.Create();
		using TemporaryFolder destinationFolder = TemporaryFolder.Create();

		ReportWriteHolder holder = new();

		holder.Record(_RECORDED_FILE_NAME, new ReportContentDigest(1, ReportAppendFixture.Hash([1])));

		AtomicArtifactWriter writer = ReportAppendFixture.BuildWriter(
			repositoryFolder,
			destinationFolder,
			holder,
			() => throw (Exception)Activator.CreateInstance(exceptionType, _HASHER_FAILURE_MESSAGE)!);

		string reportPath = ReportAppendFixture.WriteReport(writer, destinationFolder);
		string controlPath = ReportAppendFixture.WriteReport(
			ReportAppendFixture.BuildWriter(repositoryFolder, withoutFolder),
			withoutFolder);

		Assert.Null(holder.Current);
		Assert.Equal(File.ReadAllBytes(controlPath), File.ReadAllBytes(reportPath));
		Assert.Empty(Directory.GetFiles(destinationFolder.Path, ReportAppendFixture.TEMPORARY_FILE_SEARCH_PATTERN));
	}

	/// <summary>After a hasher failure the holder is empty, so an append is refused rather than compared against anything.</summary>
	[Fact]
	public void AfterAHasherFailureAnAppendIsRefusedAsNotWrittenByThisProcess()
	{
		using TemporaryFolder repositoryFolder = TemporaryFolder.Create();
		using TemporaryFolder destinationFolder = TemporaryFolder.Create();

		ReportWriteHolder holder = new();
		AtomicArtifactWriter writer = ReportAppendFixture.BuildWriter(
			repositoryFolder,
			destinationFolder,
			holder,
			() => throw new CryptographicException(_HASHER_FAILURE_MESSAGE));
		string reportPath = ReportAppendFixture.WriteReport(writer, destinationFolder);
		byte[] original = File.ReadAllBytes(reportPath);

		ReportAppendResult result = writer.AppendToReport(holder, sink => sink.Write(_APPENDED_SECTION));

		Assert.Equal(ReportAppendRefusal.NotWrittenByThisProcess, result.Refusal);
		Assert.Equal(original, File.ReadAllBytes(reportPath));
	}

	/// <summary>A hasher that works for the report and fails during the append's copy refuses it, and the report stays as it was.</summary>
	[Fact]
	public void AHasherThatFailsDuringTheAppendRefusesItAndLeavesTheReportUntouched()
	{
		using TemporaryFolder repositoryFolder = TemporaryFolder.Create();
		using TemporaryFolder destinationFolder = TemporaryFolder.Create();

		int hashesCreated = 0;
		ReportWriteHolder holder = new();
		AtomicArtifactWriter writer = ReportAppendFixture.BuildWriter(
			repositoryFolder,
			destinationFolder,
			holder,
			() => CreateHashFailingFromTheSecondRequest(ref hashesCreated));
		string reportPath = ReportAppendFixture.WriteReport(writer, destinationFolder);
		byte[] original = File.ReadAllBytes(reportPath);
		WrittenReport? recorded = holder.Current;

		ReportAppendResult result = writer.AppendToReport(holder, sink => sink.Write(_APPENDED_SECTION));

		Assert.Equal(ReportAppendRefusal.NotWritable, result.Refusal);
		Assert.Equal(original, File.ReadAllBytes(reportPath));
		Assert.Same(recorded, holder.Current);
		Assert.Empty(Directory.GetFiles(destinationFolder.Path, ReportAppendFixture.TEMPORARY_FILE_SEARCH_PATTERN));
	}

	private static IncrementalHash CreateHashFailingFromTheSecondRequest(ref int hashesCreated)
	{
		hashesCreated++;

		return hashesCreated == 1
			? IncrementalHash.CreateHash(HashAlgorithmName.SHA256)
			: throw new CryptographicException(_HASHER_FAILURE_MESSAGE);
	}

	private static string RenderToText(ReportContent content)
	{
		using StringWriter text = new();

		MarkdownReportRenderer.Render(text, content, new UnwiredGenericRecommendationProvider());

		return text.ToString();
	}

	private static List<ReportContent> BuildFixtureContents()
	{
		LanguageCatalog catalog = ReportHarness.CatalogWithCSharpOnly();
		EffectiveConfiguration configuration = ReportHarness.ConfigurationWith(ConfidenceLevel.Low);

		return
		[
			ReportHarness.EmptyContent(catalog, configuration),
			ReportHarness.ContentWith(catalog, configuration, BuildFindings()),
			ReportHarness.EmptyContent(ReportHarness.CatalogWithTwoLanguages(), configuration),
			ReportHarness.EmptyContent(ReportHarness.CatalogWithDeclaredUnsupportedCoverage(), configuration)
		];
	}

	private static ImmutableArray<Finding> BuildFindings()
	{
		return
		[
			.. Enumerable.Range(1, _FIXTURE_FINDING_COUNT).Select(index => ReportHarness.FindingAt(
				index,
				string.Format(System.Globalization.CultureInfo.InvariantCulture, _FINDING_PATH_FORMAT, index),
				_FIRST_FINDING_LINE + index,
				ReportHarness.TEMPLATED_PATTERN_CLASS,
				ConfidenceLevel.High))
		];
	}
}
