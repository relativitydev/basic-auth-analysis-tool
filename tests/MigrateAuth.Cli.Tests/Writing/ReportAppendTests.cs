using System.Security.Cryptography;
using System.Text;

using MigrateAuth.Cli.Session;
using MigrateAuth.Cli.Tests.Session;
using MigrateAuth.Cli.Writing;

using Xunit;

namespace MigrateAuth.Cli.Tests.Writing;

/// <summary>
/// What the report-append tests share: a writer wired to a holder, a report written through it, and the
/// byte and hash helpers that say "the report is exactly as it was".
/// </summary>
internal static class ReportAppendFixture
{
	internal const string REPORT_BODY = "# Report one\n\nCoverage: nothing was scanned.\n";
	internal const string TEMPORARY_FILE_SEARCH_PATTERN = "*.writing";

	internal static readonly DateOnly ArtifactDate = new(2026, 9, 18);

	internal static AtomicArtifactWriter BuildWriter(
		TemporaryFolder repositoryFolder,
		TemporaryFolder destinationFolder,
		ReportWriteHolder? holder = null,
		Func<IncrementalHash>? hashFactory = null,
		Action<ReportAppendStep>? appendStepHook = null) =>
		new(
			RepositoryFolderPath.FromSuppliedText(repositoryFolder.Path),
			DocumentationDestinationFolderPath.FromSuppliedText(destinationFolder.Path),
			holder,
			hashFactory,
			appendStepHook);

	/// <summary>Writes the Markdown report through the streaming write and returns its path.</summary>
	internal static string WriteReport(
		AtomicArtifactWriter writer,
		TemporaryFolder destinationFolder,
		string body = REPORT_BODY)
	{
		ArtifactWriteResult result = writer.WriteShareableArtifactStreaming(
			OutputArtifactKind.MarkdownReport,
			ArtifactDate,
			sink => sink.Write(body));

		Assert.True(result.Wrote);

		return destinationFolder.Combine(result.FileNameWritten);
	}

	internal static byte[] Hash(byte[] bytes) => SHA256.HashData(bytes);

	internal static byte[] Utf8(string text) => new UTF8Encoding(encoderShouldEmitUTF8Identifier: false).GetBytes(text);

	internal static byte[] Concatenate(byte[] first, byte[] second) => [.. first, .. second];
}

/// <summary>
/// The same-process report replacement at writer level: the append adds a
/// section and nothing else, a second append continues from the first, and every refusal and every failure at every
/// step leaves the report exactly as it was.
/// </summary>
public sealed class ReportAppendTests
{
	private const string _SECTION_ONE = "## Section one\n\nFirst appended text.\n";
	private const string _SECTION_TWO = "## Section two\n\nSecond appended text.\n";
	private const string _UNTERMINATED_BODY = "# Report without a final line feed";
	private const string _INJECTED_FAILURE_MESSAGE = "Injected failure.";
	private const string _TAMPERED_BODY = "# Report ONE\n\nCoverage: nothing was scanned.\n";
	private const string _SHORTER_BODY = "# Report\n";
	private const string _LINK_TARGET_FILE_NAME = "target.md";
	private const string _MANUAL_REPORT_FILE_NAME = "manual-report.md";
	private const string _DATA_ARTIFACT_BODY = "{\"findings\":[]}";
	private const int _MODIFIED_HOURS = 3;
	private const string _NESTED_DESTINATION_NAME = "destination";
	private const string _ESCAPED_FILE_NAME = "escaped.md";

	[Fact]
	public void AnAppendAddsTheSectionAfterTheOriginalBytesAndReturnsTheReportName()
	{
		using TemporaryFolder repositoryFolder = TemporaryFolder.Create();
		using TemporaryFolder destinationFolder = TemporaryFolder.Create();

		ReportWriteHolder holder = new();
		AtomicArtifactWriter writer = ReportAppendFixture.BuildWriter(repositoryFolder, destinationFolder, holder);
		string reportPath = ReportAppendFixture.WriteReport(writer, destinationFolder);
		byte[] original = File.ReadAllBytes(reportPath);

		ReportAppendResult result = writer.AppendToReport(holder, sink => sink.Write(_SECTION_ONE));

		Assert.True(result.Appended);
		Assert.Equal(ReportAppendRefusal.None, result.Refusal);
		Assert.Equal(Path.GetFileName(reportPath), result.FileName);
		Assert.Equal(
			ReportAppendFixture.Concatenate(original, ReportAppendFixture.Utf8(_SECTION_ONE)),
			File.ReadAllBytes(reportPath));
		Assert.Empty(Directory.GetFiles(destinationFolder.Path, ReportAppendFixture.TEMPORARY_FILE_SEARCH_PATTERN));
	}

	/// <summary>At writer level: the second append starts from the length and hash the first one left.</summary>
	[Fact]
	public void ASecondAppendContinuesFromTheFirstAndKeepsTheFirstSectionsBytes()
	{
		using TemporaryFolder repositoryFolder = TemporaryFolder.Create();
		using TemporaryFolder destinationFolder = TemporaryFolder.Create();

		ReportWriteHolder holder = new();
		AtomicArtifactWriter writer = ReportAppendFixture.BuildWriter(repositoryFolder, destinationFolder, holder);
		string reportPath = ReportAppendFixture.WriteReport(writer, destinationFolder);

		writer.AppendToReport(holder, sink => sink.Write(_SECTION_ONE));

		byte[] afterFirst = File.ReadAllBytes(reportPath);

		Assert.Equal(afterFirst.LongLength, holder.Current!.Length);
		Assert.Equal(ReportAppendFixture.Hash(afterFirst), holder.Current.Sha256);

		ReportAppendResult second = writer.AppendToReport(holder, sink => sink.Write(_SECTION_TWO));
		byte[] afterSecond = File.ReadAllBytes(reportPath);

		Assert.True(second.Appended);
		Assert.Equal(afterFirst, afterSecond[..afterFirst.Length]);
		Assert.Equal(ReportAppendFixture.Utf8(_SECTION_TWO), afterSecond[afterFirst.Length..]);
		Assert.Equal(afterSecond.LongLength, holder.Current.Length);
		Assert.Equal(ReportAppendFixture.Hash(afterSecond), holder.Current.Sha256);
	}

	[Fact]
	public void AnOriginalThatDoesNotEndWithALineFeedGetsOneBeforeTheSection()
	{
		using TemporaryFolder repositoryFolder = TemporaryFolder.Create();
		using TemporaryFolder destinationFolder = TemporaryFolder.Create();

		ReportWriteHolder holder = new();
		AtomicArtifactWriter writer = ReportAppendFixture.BuildWriter(repositoryFolder, destinationFolder, holder);
		string reportPath = ReportAppendFixture.WriteReport(writer, destinationFolder, _UNTERMINATED_BODY);
		byte[] original = File.ReadAllBytes(reportPath);

		writer.AppendToReport(holder, sink => sink.Write(_SECTION_ONE));

		Assert.Equal(
			ReportAppendFixture.Concatenate(original, ReportAppendFixture.Utf8($"\n{_SECTION_ONE}")),
			File.ReadAllBytes(reportPath));
	}

	[Fact]
	public void TheSectionIsWrittenWithLineFeedNewlinesAndNoByteOrderMark()
	{
		using TemporaryFolder repositoryFolder = TemporaryFolder.Create();
		using TemporaryFolder destinationFolder = TemporaryFolder.Create();

		ReportWriteHolder holder = new();
		AtomicArtifactWriter writer = ReportAppendFixture.BuildWriter(repositoryFolder, destinationFolder, holder);
		string reportPath = ReportAppendFixture.WriteReport(writer, destinationFolder);
		int originalLength = File.ReadAllBytes(reportPath).Length;

		writer.AppendToReport(holder, sink => sink.WriteLine("café"));

		byte[] appended = File.ReadAllBytes(reportPath)[originalLength..];

		Assert.Equal(ReportAppendFixture.Utf8("café\n"), appended);
	}

	[Fact]
	public void AnEmptyHolderRefusesBecauseThisProcessWroteNoReport()
	{
		using TemporaryFolder repositoryFolder = TemporaryFolder.Create();
		using TemporaryFolder destinationFolder = TemporaryFolder.Create();

		AtomicArtifactWriter writer = ReportAppendFixture.BuildWriter(repositoryFolder, destinationFolder);

		ReportAppendResult result = writer.AppendToReport(new ReportWriteHolder(), sink => sink.Write(_SECTION_ONE));

		Assert.False(result.Appended);
		Assert.Equal(ReportAppendRefusal.NotWrittenByThisProcess, result.Refusal);
		Assert.Equal(string.Empty, result.FileName);
	}

	[Fact]
	public void AReportWrittenThroughTheStringOverloadCannotBeAppendedTo()
	{
		using TemporaryFolder repositoryFolder = TemporaryFolder.Create();
		using TemporaryFolder destinationFolder = TemporaryFolder.Create();

		ReportWriteHolder holder = new();
		AtomicArtifactWriter writer = ReportAppendFixture.BuildWriter(repositoryFolder, destinationFolder, holder);

		ArtifactWriteResult written = writer.WriteShareableArtifact(
			OutputArtifactKind.MarkdownReport,
			ReportAppendFixture.ArtifactDate,
			ReportAppendFixture.REPORT_BODY);
		byte[] original = File.ReadAllBytes(destinationFolder.Combine(written.FileNameWritten));

		ReportAppendResult result = writer.AppendToReport(holder, sink => sink.Write(_SECTION_ONE));

		Assert.Equal(ReportAppendRefusal.NotWrittenByThisProcess, result.Refusal);
		Assert.Equal(original, File.ReadAllBytes(destinationFolder.Combine(written.FileNameWritten)));
	}

	/// <summary>
	/// Defence in depth: even if a holder somehow carried a name that leaves the destination (its public method refuses one,
	/// so the value is put in through reflection), the append refuses as outside the boundary and touches nothing.
	/// </summary>
	[Fact]
	public void AHeldNameThatResolvesOutsideTheDestinationIsRefusedAndTheFileThereIsUntouched()
	{
		using TemporaryFolder repositoryFolder = TemporaryFolder.Create();
		using TemporaryFolder outerFolder = TemporaryFolder.Create();

		string destinationPath = Path.Combine(outerFolder.Path, _NESTED_DESTINATION_NAME);
		string escapedPath = outerFolder.Combine(_ESCAPED_FILE_NAME);
		byte[] original = ReportAppendFixture.Utf8(ReportAppendFixture.REPORT_BODY);

		Directory.CreateDirectory(destinationPath);
		File.WriteAllBytes(escapedPath, original);

		ReportWriteHolder holder = new();

		typeof(ReportWriteHolder).GetProperty(nameof(ReportWriteHolder.Current))!.SetValue(
			holder,
			new WrittenReport(Path.Combine("..", _ESCAPED_FILE_NAME), original.LongLength, ReportAppendFixture.Hash(original)));

		AtomicArtifactWriter writer = new(
			RepositoryFolderPath.FromSuppliedText(repositoryFolder.Path),
			DocumentationDestinationFolderPath.FromSuppliedText(destinationPath),
			holder);

		ReportAppendResult result = writer.AppendToReport(holder, sink => sink.Write(_SECTION_ONE));

		Assert.False(result.Appended);
		Assert.Equal(ReportAppendRefusal.OutsideBoundary, result.Refusal);
		Assert.Equal(original, File.ReadAllBytes(escapedPath));
		Assert.Empty(Directory.GetFiles(destinationPath));
	}

	[Fact]
	public void ADestinationInsideTheRepositoryRefusesAndLeavesTheReportUntouched()
	{
		using TemporaryFolder repositoryFolder = TemporaryFolder.Create();

		string insideDestination = Path.Combine(repositoryFolder.Path, "out");
		string reportPath = Path.Combine(insideDestination, _MANUAL_REPORT_FILE_NAME);
		byte[] original = ReportAppendFixture.Utf8(ReportAppendFixture.REPORT_BODY);

		Directory.CreateDirectory(insideDestination);
		File.WriteAllBytes(reportPath, original);

		ReportWriteHolder holder = new();

		holder.Record(_MANUAL_REPORT_FILE_NAME, new ReportContentDigest(original.LongLength, ReportAppendFixture.Hash(original)));

		AtomicArtifactWriter writer = new(
			RepositoryFolderPath.FromSuppliedText(repositoryFolder.Path),
			DocumentationDestinationFolderPath.FromSuppliedText(insideDestination),
			holder);

		ReportAppendResult result = writer.AppendToReport(holder, sink => sink.Write(_SECTION_ONE));

		Assert.Equal(ReportAppendRefusal.OutsideBoundary, result.Refusal);
		Assert.Equal(original, File.ReadAllBytes(reportPath));
		Assert.Empty(Directory.GetFiles(insideDestination, ReportAppendFixture.TEMPORARY_FILE_SEARCH_PATTERN));
	}

	[Fact]
	public void AReportThatGrewSinceItWasWrittenIsRefusedAndLeftUntouched()
	{
		AssertChangedReportIsRefused(
			(reportPath, original) => File.AppendAllText(reportPath, "extra\n"),
			ReportAppendRefusal.ReportChanged);
	}

	[Fact]
	public void AReportThatShrankSinceItWasWrittenIsRefusedAndLeftUntouched()
	{
		AssertChangedReportIsRefused(
			(reportPath, original) => File.WriteAllText(reportPath, _SHORTER_BODY),
			ReportAppendRefusal.ReportChanged);
	}

	/// <summary>The same length and different bytes: only the prefix hash catches it.</summary>
	[Fact]
	public void AReportWithTheSameLengthAndADifferentHashIsRefusedAsAPrefixMismatch()
	{
		AssertChangedReportIsRefused(
			(reportPath, original) =>
			{
				byte[] tampered = ReportAppendFixture.Utf8(_TAMPERED_BODY);

				File.WriteAllBytes(
					reportPath,
					ReportAppendFixture.Concatenate(
						tampered,
						new byte[original.Length - tampered.Length]));
			},
			ReportAppendRefusal.ReportChanged);
	}

	[Fact]
	public void AReportThatWasDeletedIsRefusedAsChanged()
	{
		AssertChangedReportIsRefused((reportPath, original) => File.Delete(reportPath), ReportAppendRefusal.ReportChanged);
	}

	/// <summary>A directory occupying the report's name is refused and what it holds is not touched.</summary>
	[Fact]
	public void AReportReplacedByADirectoryIsRefusedAndItsContentsAreUntouched()
	{
		using TemporaryFolder repositoryFolder = TemporaryFolder.Create();
		using TemporaryFolder destinationFolder = TemporaryFolder.Create();

		ReportWriteHolder holder = new();
		AtomicArtifactWriter writer = ReportAppendFixture.BuildWriter(repositoryFolder, destinationFolder, holder);
		string reportPath = ReportAppendFixture.WriteReport(writer, destinationFolder);
		string markerPath = Path.Combine(reportPath, _LINK_TARGET_FILE_NAME);

		File.Delete(reportPath);
		Directory.CreateDirectory(reportPath);
		File.WriteAllText(markerPath, ReportAppendFixture.REPORT_BODY);

		ReportAppendResult result = writer.AppendToReport(holder, sink => sink.Write(_SECTION_ONE));

		Assert.Equal(ReportAppendRefusal.ReportReplacedOrLinked, result.Refusal);
		Assert.Equal(ReportAppendFixture.REPORT_BODY, File.ReadAllText(markerPath));
		Assert.Single(Directory.GetFileSystemEntries(reportPath));
		Assert.Empty(Directory.GetFiles(destinationFolder.Path, ReportAppendFixture.TEMPORARY_FILE_SEARCH_PATTERN));
	}

	/// <summary>
	/// A directory symbolic link at the report's name is refused; the link's target is not touched. Created without starting any
	/// process, so a Windows account without the symbolic-link privilege cannot create one and the test is then a no-op;
	/// the junction itself is not exercised here.
	/// </summary>
	[Fact]
	public void AReportReplacedByADirectorySymbolicLinkIsRefusedAndTheTargetIsUntouched()
	{
		using TemporaryFolder repositoryFolder = TemporaryFolder.Create();
		using TemporaryFolder destinationFolder = TemporaryFolder.Create();
		using TemporaryFolder targetFolder = TemporaryFolder.Create();

		ReportWriteHolder holder = new();
		AtomicArtifactWriter writer = ReportAppendFixture.BuildWriter(repositoryFolder, destinationFolder, holder);
		string reportPath = ReportAppendFixture.WriteReport(writer, destinationFolder);
		string markerPath = targetFolder.Combine(_LINK_TARGET_FILE_NAME);

		File.WriteAllText(markerPath, ReportAppendFixture.REPORT_BODY);
		File.Delete(reportPath);

		if (TryCreateDirectoryLink(reportPath, targetFolder.Path))
		{
			ReportAppendResult result = writer.AppendToReport(holder, sink => sink.Write(_SECTION_ONE));

			Assert.Equal(ReportAppendRefusal.ReportReplacedOrLinked, result.Refusal);
			Assert.NotNull(new FileInfo(reportPath).LinkTarget);
			Assert.Equal(ReportAppendFixture.REPORT_BODY, File.ReadAllText(markerPath));
			Assert.Single(Directory.GetFileSystemEntries(targetFolder.Path));
			Assert.Empty(Directory.GetFiles(destinationFolder.Path, ReportAppendFixture.TEMPORARY_FILE_SEARCH_PATTERN));
		}
	}

	/// <summary>
	/// A symbolic link to an identical file is refused on <c>LinkTarget</c> alone, although its length and hash match.
	/// A Windows account without the symbolic-link privilege cannot create one, so there the directory test above is the
	/// proof of the same refusal.
	/// </summary>
	[Fact]
	public void AReportReplacedByASymbolicLinkToIdenticalBytesIsRefusedAndTheTargetIsUntouched()
	{
		using TemporaryFolder repositoryFolder = TemporaryFolder.Create();
		using TemporaryFolder destinationFolder = TemporaryFolder.Create();
		using TemporaryFolder targetFolder = TemporaryFolder.Create();

		ReportWriteHolder holder = new();
		AtomicArtifactWriter writer = ReportAppendFixture.BuildWriter(repositoryFolder, destinationFolder, holder);
		string reportPath = ReportAppendFixture.WriteReport(writer, destinationFolder);
		string targetPath = targetFolder.Combine(_LINK_TARGET_FILE_NAME);
		byte[] original = File.ReadAllBytes(reportPath);

		File.WriteAllBytes(targetPath, original);
		File.Delete(reportPath);

		if (TryCreateFileLink(reportPath, targetPath))
		{
			ReportAppendResult result = writer.AppendToReport(holder, sink => sink.Write(_SECTION_ONE));

			Assert.Equal(ReportAppendRefusal.ReportReplacedOrLinked, result.Refusal);
			Assert.Equal(original, File.ReadAllBytes(targetPath));
			Assert.Empty(Directory.GetFiles(destinationFolder.Path, ReportAppendFixture.TEMPORARY_FILE_SEARCH_PATTERN));
		}
	}

	/// <summary>At writer level: a failure at each step leaves the report byte-identical and the holder as it was.</summary>
	[Theory]
	[InlineData(ReportAppendStep.TemporaryCreation)]
	[InlineData(ReportAppendStep.MidCopy)]
	[InlineData(ReportAppendStep.Rename)]
	public void AFailureAtEachStepOfTheReplacementLeavesTheReportByteIdentical(ReportAppendStep failingStep)
	{
		using TemporaryFolder repositoryFolder = TemporaryFolder.Create();
		using TemporaryFolder destinationFolder = TemporaryFolder.Create();

		ReportWriteHolder holder = new();
		AtomicArtifactWriter writer = ReportAppendFixture.BuildWriter(
			repositoryFolder,
			destinationFolder,
			holder,
			appendStepHook: step => FailAt(step, failingStep));
		string reportPath = ReportAppendFixture.WriteReport(writer, destinationFolder);
		byte[] original = File.ReadAllBytes(reportPath);
		WrittenReport? before = holder.Current;

		ReportAppendResult result = writer.AppendToReport(holder, sink => sink.Write(_SECTION_ONE));

		Assert.False(result.Appended);
		Assert.Equal(ReportAppendRefusal.NotWritable, result.Refusal);
		Assert.Equal(original, File.ReadAllBytes(reportPath));
		Assert.Same(before, holder.Current);
		Assert.Empty(Directory.GetFiles(destinationFolder.Path, ReportAppendFixture.TEMPORARY_FILE_SEARCH_PATTERN));
	}

	[Fact]
	public void AFailureWhileTheSectionIsWrittenLeavesTheReportByteIdentical()
	{
		using TemporaryFolder repositoryFolder = TemporaryFolder.Create();
		using TemporaryFolder destinationFolder = TemporaryFolder.Create();

		ReportWriteHolder holder = new();
		AtomicArtifactWriter writer = ReportAppendFixture.BuildWriter(repositoryFolder, destinationFolder, holder);
		string reportPath = ReportAppendFixture.WriteReport(writer, destinationFolder);
		byte[] original = File.ReadAllBytes(reportPath);

		ReportAppendResult result = writer.AppendToReport(
			holder,
			sink =>
			{
				sink.Write(_SECTION_ONE);
				sink.Flush();

				throw new IOException(_INJECTED_FAILURE_MESSAGE);
			});

		Assert.Equal(ReportAppendRefusal.NotWritable, result.Refusal);
		Assert.Equal(original, File.ReadAllBytes(reportPath));
		Assert.Empty(Directory.GetFiles(destinationFolder.Path, ReportAppendFixture.TEMPORARY_FILE_SEARCH_PATTERN));
	}

	/// <summary>The unlocked window between the copy and the rename: a report touched in it is not replaced.</summary>
	[Fact]
	public void AReportTouchedBetweenTheCopyAndTheReplacementIsRefusedAndLeftUntouched()
	{
		using TemporaryFolder repositoryFolder = TemporaryFolder.Create();
		using TemporaryFolder destinationFolder = TemporaryFolder.Create();

		string reportPath = string.Empty;
		ReportWriteHolder holder = new();
		AtomicArtifactWriter writer = ReportAppendFixture.BuildWriter(
			repositoryFolder,
			destinationFolder,
			holder,
			appendStepHook: step => TouchAt(step, reportPath));

		reportPath = ReportAppendFixture.WriteReport(writer, destinationFolder);

		byte[] original = File.ReadAllBytes(reportPath);

		ReportAppendResult result = writer.AppendToReport(holder, sink => sink.Write(_SECTION_ONE));

		Assert.Equal(ReportAppendRefusal.ReportChanged, result.Refusal);
		Assert.Equal(original, File.ReadAllBytes(reportPath));
		Assert.Empty(Directory.GetFiles(destinationFolder.Path, ReportAppendFixture.TEMPORARY_FILE_SEARCH_PATTERN));
	}

	[Fact]
	public void TheDataArtifactIsNeverOpenedOrChangedByAnAppend()
	{
		using TemporaryFolder repositoryFolder = TemporaryFolder.Create();
		using TemporaryFolder destinationFolder = TemporaryFolder.Create();

		ReportWriteHolder holder = new();
		AtomicArtifactWriter writer = ReportAppendFixture.BuildWriter(repositoryFolder, destinationFolder, holder);

		ReportAppendFixture.WriteReport(writer, destinationFolder);

		ArtifactWriteResult data = writer.WriteShareableArtifact(
			OutputArtifactKind.StructuredDataArtifact,
			ReportAppendFixture.ArtifactDate,
			_DATA_ARTIFACT_BODY);
		string dataPath = destinationFolder.Combine(data.FileNameWritten);
		byte[] dataBytes = File.ReadAllBytes(dataPath);
		DateTime dataWriteTime = File.GetLastWriteTimeUtc(dataPath);

		writer.AppendToReport(holder, sink => sink.Write(_SECTION_ONE));

		Assert.Equal(dataBytes, File.ReadAllBytes(dataPath));
		Assert.Equal(dataWriteTime, File.GetLastWriteTimeUtc(dataPath));
	}

	[Fact]
	public void TheArgumentsAreValidated()
	{
		using TemporaryFolder repositoryFolder = TemporaryFolder.Create();
		using TemporaryFolder destinationFolder = TemporaryFolder.Create();

		AtomicArtifactWriter writer = ReportAppendFixture.BuildWriter(repositoryFolder, destinationFolder);

		Assert.Throws<ArgumentNullException>(() => writer.AppendToReport(null!, sink => sink.Write(_SECTION_ONE)));
		Assert.Throws<ArgumentNullException>(() => writer.AppendToReport(new ReportWriteHolder(), null!));
	}

	private static void AssertChangedReportIsRefused(Action<string, byte[]> change, ReportAppendRefusal expected)
	{
		using TemporaryFolder repositoryFolder = TemporaryFolder.Create();
		using TemporaryFolder destinationFolder = TemporaryFolder.Create();

		ReportWriteHolder holder = new();
		AtomicArtifactWriter writer = ReportAppendFixture.BuildWriter(repositoryFolder, destinationFolder, holder);
		string reportPath = ReportAppendFixture.WriteReport(writer, destinationFolder);
		byte[] original = File.ReadAllBytes(reportPath);

		change(reportPath, original);

		bool existed = File.Exists(reportPath);
		byte[] changed = existed ? File.ReadAllBytes(reportPath) : [];

		ReportAppendResult result = writer.AppendToReport(holder, sink => sink.Write(_SECTION_ONE));

		Assert.False(result.Appended);
		Assert.Equal(expected, result.Refusal);
		Assert.Equal(existed, File.Exists(reportPath));
		Assert.Equal(changed, existed ? File.ReadAllBytes(reportPath) : []);
		Assert.Empty(Directory.GetFiles(destinationFolder.Path, ReportAppendFixture.TEMPORARY_FILE_SEARCH_PATTERN));
	}

	private static void FailAt(ReportAppendStep reached, ReportAppendStep failing)
	{
		if (reached == failing)
		{
			throw new IOException(_INJECTED_FAILURE_MESSAGE);
		}
	}

	private static void TouchAt(ReportAppendStep reached, string reportPath)
	{
		if (reached == ReportAppendStep.Rename)
		{
			File.SetLastWriteTimeUtc(reportPath, File.GetLastWriteTimeUtc(reportPath).AddHours(_MODIFIED_HOURS));
		}
	}

	private static bool TryCreateDirectoryLink(string linkPath, string targetPath)
	{
		bool created;

		try
		{
			Directory.CreateSymbolicLink(linkPath, targetPath);

			created = true;
		}
		catch (IOException) when (OperatingSystem.IsWindows())
		{
			created = false;
		}
		catch (UnauthorizedAccessException) when (OperatingSystem.IsWindows())
		{
			created = false;
		}

		return created;
	}

	private static bool TryCreateFileLink(string linkPath, string targetPath)
	{
		bool created;

		try
		{
			File.CreateSymbolicLink(linkPath, targetPath);

			created = true;
		}
		catch (IOException) when (OperatingSystem.IsWindows())
		{
			created = false;
		}
		catch (UnauthorizedAccessException) when (OperatingSystem.IsWindows())
		{
			created = false;
		}

		return created;
	}
}
