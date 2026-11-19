using System.Security.Cryptography;
using System.Text;

using MigrateAuth.Cli.Agent;
using MigrateAuth.Cli.Session;
using MigrateAuth.Cli.Writing;

using Xunit;

namespace MigrateAuth.Cli.Tests.Agent;

/// <summary>An appender that refuses in the way a test names and touches nothing.</summary>
internal sealed class RefusingAppender(ReportAppendRefusal refusal) : IReportAppender
{
	internal int CallCount { get; private set; }

	public ReportAppendResult AppendToReport(ReportWriteHolder holder, Action<TextWriter> writeSection)
	{
		CallCount++;

		return new ReportAppendResult { Appended = false, Refusal = refusal, FileName = string.Empty };
	}
}

/// <summary>
/// THE SUPPLEMENTAL SECTION AT THE STAGE: appended after a successful generation, naming the pack and repeating the review
/// label; each refusal prints its own fixed line, keeps the pack and lets the session continue; a resumed run appends
/// nothing and says so; a report that could not be recorded says that instead; and a second run appends a second section.
/// The refusals that cannot arise naturally are driven through an appender double.
/// </summary>
public sealed class AiAssistReportSectionTests
{
	private const string _CHANGED_LINE =
		"ai-assist: the report was changed since this run wrote it, so no section was added to it and it was left as it was. The prompt pack was written.";

	private const string _REPLACED_OR_LINKED_LINE =
		"ai-assist: the report was replaced by another file or by a link since this run wrote it, so no section was added to it and it was left as it was. The prompt pack was written.";

	private const string _OUTSIDE_BOUNDARY_LINE =
		"ai-assist: the report is outside the documentation destination folder, so no section was added to it and it was left as it was. The prompt pack was written.";

	private const string _NOT_WRITABLE_LINE =
		"ai-assist: the report could not be written, so the supplemental section was not added and the report was left as it was. The prompt pack was written.";

	private const string _RESUMED_LINE =
		"ai-assist: no section was added to the report, because a section is added only to a report written in this same run. The prompt pack was written. Run 'report' in this run, then 'ai-assist', to have the section added.";

	private const string _NOT_RECORDED_LINE =
		"ai-assist: this run could not record the report it wrote, so no section was added to it. The prompt pack was written. Run 'report' again, then 'ai-assist'.";

	public static TheoryData<ReportAppendRefusal, string> Refusals => new()
	{
		{ ReportAppendRefusal.NotWrittenByThisProcess, _RESUMED_LINE },
		{ ReportAppendRefusal.OutsideBoundary, _OUTSIDE_BOUNDARY_LINE },
		{ ReportAppendRefusal.ReportReplacedOrLinked, _REPLACED_OR_LINKED_LINE },
		{ ReportAppendRefusal.ReportChanged, _CHANGED_LINE },
		{ ReportAppendRefusal.NotWritable, _NOT_WRITABLE_LINE }
	};

	[Fact]
	public void AfterAGenerationTheReportEndsWithTheSectionNamingThePackAndRepeatingTheLabel()
	{
		using AiAssistHarness harness = new(["1"]);

		harness.IsTerminal = false;
		harness.Run();

		string report = Encoding.UTF8.GetString(harness.ReportBytes());
		int heading = report.IndexOf("## Supplemental: AI-assisted review - Claude Code", StringComparison.Ordinal);

		Assert.True(heading > 0);
		Assert.Contains("AI-assisted findings require engineering review before implementation.", report[heading..], StringComparison.Ordinal);
		Assert.Contains("was written as ai-assist-claude-code.md, in the documentation destination folder. It lists 6 entries. This report is not part of the pack.", report[heading..], StringComparison.Ordinal);
		Assert.EndsWith(AiAssistHarness.LINE_FEED, report, StringComparison.Ordinal);
		Assert.Contains("ai-assist: a supplemental section naming ai-assist-claude-code.md was added to the report.", harness.Transcript, StringComparison.Ordinal);
	}

	[Fact]
	public void TheFiveRefusalsHaveFiveDifferentLines()
	{
		string[] lines =
		[
			AiAssistTexts.REPORT_NOT_WRITTEN_BY_THIS_RUN,
			AiAssistTexts.APPEND_REFUSED_OUTSIDE_BOUNDARY,
			AiAssistTexts.APPEND_REFUSED_REPLACED_OR_LINKED,
			AiAssistTexts.APPEND_REFUSED_REPORT_CHANGED,
			AiAssistTexts.APPEND_REFUSED_NOT_WRITABLE
		];

		Assert.Equal(Refusals.Count, lines.Length);
		Assert.Equal(lines.Length, lines.Distinct(StringComparer.Ordinal).Count());
	}

	[Theory]
	[MemberData(nameof(Refusals))]
	public void EveryRefusalPrintsItsOwnFixedLineKeepsThePackAndTheSessionContinues(ReportAppendRefusal refusal, string expectedLine)
	{
		using AiAssistHarness harness = new(["1", "2"]);
		RefusingAppender appender = new(refusal);
		byte[] reportBefore = harness.ReportBytes();

		harness.IsTerminal = false;
		harness.AppenderOverride = appender;

		StageRunOutcome first = harness.Run();
		StageRunOutcome second = harness.Run();

		Assert.Equal(StageRunOutcome.SelfReported, first);
		Assert.Equal(StageRunOutcome.SelfReported, second);
		Assert.Equal(2, appender.CallCount);
		Assert.Equal(2, CountOccurrences(harness.Transcript, expectedLine));
		Assert.Equal([AiAssistHarness.CLAUDE_PACK_FILE_NAME, AiAssistHarness.CODEX_PACK_FILE_NAME], harness.PackFiles());
		Assert.Equal(reportBefore, harness.ReportBytes());
	}

	[Fact]
	public void AReportEditedAfterItWasWrittenIsRefusedByTheRealWriterAndLeftAsItIs()
	{
		using AiAssistHarness harness = new(["1"]);

		harness.IsTerminal = false;

		byte[] edited = harness.EditTheReportInPlace();
		harness.Run();

		Assert.Contains(_CHANGED_LINE, harness.Transcript, StringComparison.Ordinal);
		Assert.Equal(edited, harness.ReportBytes());
		Assert.Equal([AiAssistHarness.CLAUDE_PACK_FILE_NAME], harness.PackFiles());
	}

	/// <summary>Where the platform refuses to make a link, for lack of the privilege, this test is a declared no-op.</summary>
	[Fact]
	public void AReportReplacedByALinkIsRefusedByTheRealWriterAndTheTargetIsLeftAsItIs()
	{
		using AiAssistHarness harness = new(["1"]);
		string targetPath = harness.DestinationFolder.Combine("target-copy.md");

		harness.IsTerminal = false;
		File.Copy(harness.ReportPath, targetPath);
		File.Delete(harness.ReportPath);

		if (TryCreateFileLink(harness.ReportPath, targetPath))
		{
			byte[] targetBefore = File.ReadAllBytes(targetPath);

			harness.Run();

			Assert.Contains(_REPLACED_OR_LINKED_LINE, harness.Transcript, StringComparison.Ordinal);
			Assert.Equal(targetBefore, File.ReadAllBytes(targetPath));
		}
	}

	[Fact]
	public void AResumedRunWritesThePackAppendsNothingAndPrintsTheResumedLine()
	{
		using AiAssistHarness harness = new(["1"], writeReport: false);

		harness.IsTerminal = false;
		harness.Run();

		string pack = File.ReadAllText(harness.DestinationFolder.Combine(AiAssistHarness.CLAUDE_PACK_FILE_NAME));

		Assert.Contains(_RESUMED_LINE, harness.Transcript, StringComparison.Ordinal);
		Assert.DoesNotContain(_NOT_RECORDED_LINE, harness.Transcript, StringComparison.Ordinal);
		Assert.Contains("No report file is named for this run.", pack, StringComparison.Ordinal);
		Assert.Empty(Directory.GetFiles(harness.DestinationFolder.Path, "basic-auth-migration*"));
	}

	[Fact]
	public void AReportThatCouldNotBeRecordedPrintsItsOwnLineAndNotTheResumedOne()
	{
		using AiAssistHarness harness = new(["1"], hashFactory: () => throw new CryptographicException("The hash is unavailable."));

		harness.IsTerminal = false;
		byte[] reportBefore = harness.ReportBytes();

		harness.Run();

		Assert.True(harness.Holder.RecordingWasLost);
		Assert.Contains(_NOT_RECORDED_LINE, harness.Transcript, StringComparison.Ordinal);
		Assert.DoesNotContain(_RESUMED_LINE, harness.Transcript, StringComparison.Ordinal);
		Assert.Equal(reportBefore, harness.ReportBytes());
		Assert.Equal([AiAssistHarness.CLAUDE_PACK_FILE_NAME], harness.PackFiles());
	}

	[Fact]
	public void ASecondRunAppendsASecondSectionAfterTheFirstAndKeepsTheFirstSectionsBytes()
	{
		using AiAssistHarness harness = new(["1", "2"]);

		harness.IsTerminal = false;
		harness.Run();

		byte[] afterFirst = harness.ReportBytes();

		harness.Run();

		byte[] afterSecond = harness.ReportBytes();
		string appended = Encoding.UTF8.GetString(afterSecond[afterFirst.Length..]);

		Assert.Equal(afterFirst, afterSecond[..afterFirst.Length]);
		Assert.Contains("## Supplemental: AI-assisted review - OpenAI Codex", appended, StringComparison.Ordinal);
		Assert.Equal(2, CountOccurrences(Encoding.UTF8.GetString(afterSecond), "## Supplemental: AI-assisted review - "));
		Assert.Equal(SHA256.HashData(afterSecond), harness.Holder.Current!.Sha256);
	}

	[Fact]
	public void TheSameEnvironmentTwiceWritesASecondPackWithADashTwoAndAppendsASecondSection()
	{
		using AiAssistHarness harness = new(["1", "1"]);

		harness.IsTerminal = false;
		harness.Run();
		harness.Run();

		Assert.Equal([AiAssistHarness.CLAUDE_SECOND_PACK_FILE_NAME, AiAssistHarness.CLAUDE_PACK_FILE_NAME], harness.PackFiles());
		Assert.Contains("naming ai-assist-claude-code-2.md was added", harness.Transcript, StringComparison.Ordinal);
		Assert.Equal(2, CountOccurrences(Encoding.UTF8.GetString(harness.ReportBytes()), "## Supplemental: AI-assisted review - Claude Code"));
	}

	private static bool TryCreateFileLink(string linkPath, string targetPath)
	{
		bool created;

		try
		{
			File.CreateSymbolicLink(linkPath, targetPath);
			created = true;
		}
		catch (UnauthorizedAccessException) when (OperatingSystem.IsWindows())
		{
			created = false;
		}
		catch (IOException) when (OperatingSystem.IsWindows())
		{
			created = false;
		}

		return created;
	}

	private static int CountOccurrences(string text, string fragment)
	{
		int count = 0;
		int index = text.IndexOf(fragment, StringComparison.Ordinal);

		while (index >= 0)
		{
			count++;
			index = text.IndexOf(fragment, index + fragment.Length, StringComparison.Ordinal);
		}

		return count;
	}
}
