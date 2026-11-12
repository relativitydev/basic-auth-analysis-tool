using System.Globalization;
using System.Text.RegularExpressions;

using MigrateAuth.Cli.Agent;
using MigrateAuth.Cli.Session;
using MigrateAuth.Cli.Tests.Session;
using MigrateAuth.Cli.Writing;

using Xunit;

namespace MigrateAuth.Cli.Tests.Writing;

/// <summary>
/// The prompt pack as the third artifact kind: the name comes from the compiled stem, carries no date,
/// takes <c>-2</c>, <c>-3</c> on a collision on any day, is written through the same atomic path with the handling header
/// as its first line, and never comes from the other overloads.
/// </summary>
public sealed class PromptPackArtifactNamingTests
{
	private const string _PACK_BODY = "Instruction text.\n\nEntry one.\n";
	private const string _DATE_SHAPE_PATTERN = @"\d{4}-\d{2}-\d{2}";
	private const string _MARKDOWN_EXTENSION = ".md";
	private const int _THIRD_ORDINAL = 3;
	private const int _SECOND_ORDINAL = 2;

	public static TheoryData<AgentEnvironment, string> CompiledStems => new()
	{
		{ AgentEnvironment.ClaudeCode, "ai-assist-claude-code" },
		{ AgentEnvironment.OpenAiCodex, "ai-assist-openai-codex" },
		{ AgentEnvironment.GitHubCopilotCli, "ai-assist-copilot-cli" }
	};

	[Theory]
	[MemberData(nameof(CompiledStems))]
	public void ThePackIsWrittenAsTheCompiledStemWithNoDate(AgentEnvironment environment, string expectedStem)
	{
		using TemporaryFolder repositoryFolder = TemporaryFolder.Create();
		using TemporaryFolder destinationFolder = TemporaryFolder.Create();

		string stem = AgentEnvironmentCatalog.Describe(environment).PackFileNameStem;

		ArtifactWriteResult result = ReportAppendFixture.BuildWriter(repositoryFolder, destinationFolder)
			.WritePromptPackStreaming(stem, sink => sink.Write(_PACK_BODY));

		Assert.Equal(expectedStem, stem);
		Assert.True(result.Wrote);
		Assert.Equal(ArtifactWriteRefusal.None, result.Refusal);
		Assert.Equal($"{expectedStem}{_MARKDOWN_EXTENSION}",result.FileNameWritten);
		Assert.DoesNotMatch(_DATE_SHAPE_PATTERN, result.FileNameWritten);
		Assert.True(File.Exists(destinationFolder.Combine(result.FileNameWritten)));
	}

	/// <summary>There is no date parameter, so "on any day" is the same ordinal rule: the next run takes the next suffix.</summary>
	[Theory]
	[MemberData(nameof(CompiledStems))]
	public void ASecondAndThirdRunForTheSameEnvironmentTakeTheNextSuffixAndLeaveTheFirstUntouched(
		AgentEnvironment environment,
		string expectedStem)
	{
		using TemporaryFolder repositoryFolder = TemporaryFolder.Create();
		using TemporaryFolder destinationFolder = TemporaryFolder.Create();

		string stem = AgentEnvironmentCatalog.Describe(environment).PackFileNameStem;
		AtomicArtifactWriter writer = ReportAppendFixture.BuildWriter(repositoryFolder, destinationFolder);

		ArtifactWriteResult first = writer.WritePromptPackStreaming(stem, sink => sink.Write(_PACK_BODY));
		byte[] firstBytes = File.ReadAllBytes(destinationFolder.Combine(first.FileNameWritten));
		ArtifactWriteResult second = writer.WritePromptPackStreaming(stem, sink => sink.Write("Another pack.\n"));
		ArtifactWriteResult third = writer.WritePromptPackStreaming(stem, sink => sink.Write("A third pack.\n"));

		Assert.Equal($"{expectedStem}{_MARKDOWN_EXTENSION}", first.FileNameWritten);
		Assert.Equal($"{expectedStem}-{_SECOND_ORDINAL}{_MARKDOWN_EXTENSION}", second.FileNameWritten);
		Assert.Equal($"{expectedStem}-{_THIRD_ORDINAL}{_MARKDOWN_EXTENSION}", third.FileNameWritten);
		Assert.Equal(firstBytes, File.ReadAllBytes(destinationFolder.Combine(first.FileNameWritten)));
	}

	[Fact]
	public void TheThreeEnvironmentsDoNotCollideWithEachOtherOrWithTheReport()
	{
		using TemporaryFolder repositoryFolder = TemporaryFolder.Create();
		using TemporaryFolder destinationFolder = TemporaryFolder.Create();

		AtomicArtifactWriter writer = ReportAppendFixture.BuildWriter(repositoryFolder, destinationFolder);

		ReportAppendFixture.WriteReport(writer, destinationFolder);

		List<string> written =
		[
			.. AgentEnvironmentCatalog.All.Select(descriptor =>
				writer.WritePromptPackStreaming(descriptor.PackFileNameStem, sink => sink.Write(_PACK_BODY)).FileNameWritten)
		];

		Assert.Equal(AgentEnvironmentCatalog.All.Length, written.Distinct(StringComparer.Ordinal).Count());
		Assert.All(written, name => Assert.DoesNotContain("-2", name, StringComparison.Ordinal));
	}

	[Theory]
	[InlineData(1, "ai-assist-claude-code.md")]
	[InlineData(2, "ai-assist-claude-code-2.md")]
	[InlineData(10, "ai-assist-claude-code-10.md")]
	public void TheNamingRuleIsTheStemWithACollisionSuffixAndNoDate(int ordinal, string expectedFileName)
	{
		Assert.Equal(expectedFileName, OutputArtifactNaming.BuildPromptPackFileName("ai-assist-claude-code", ordinal));
	}

	[Fact]
	public void TheNamingRuleRefusesABlankStemAndAnOrdinalBelowOne()
	{
		Assert.Throws<ArgumentException>(() => OutputArtifactNaming.BuildPromptPackFileName(" ", 1));
		Assert.Throws<ArgumentOutOfRangeException>(() => OutputArtifactNaming.BuildPromptPackFileName("stem", 0));
	}

	[Fact]
	public void ThePackCarriesTheHandlingHeaderAsItsFirstLineAndTheBodyAfterIt()
	{
		using TemporaryFolder repositoryFolder = TemporaryFolder.Create();
		using TemporaryFolder destinationFolder = TemporaryFolder.Create();

		ArtifactWriteResult result = ReportAppendFixture.BuildWriter(repositoryFolder, destinationFolder)
			.WritePromptPackStreaming(AgentEnvironmentCatalog.CLAUDE_CODE_PACK_STEM, sink => sink.Write(_PACK_BODY));

		string text = File.ReadAllText(destinationFolder.Combine(result.FileNameWritten));

		Assert.StartsWith($"{ArtifactHandlingNotice.MarkdownHeader}\n\n", text, StringComparison.Ordinal);
		Assert.EndsWith(_PACK_BODY, text, StringComparison.Ordinal);
	}

	[Theory]
	[InlineData("")]
	[InlineData("  ")]
	[InlineData("..\\outside")]
	[InlineData("../outside")]
	[InlineData("folder/stem")]
	[InlineData("stem:stream")]
	[InlineData("stem|pipe")]
	public void AStemThatIsNotABareFileNameIsRefusedBeforeAnythingIsWritten(string stem)
	{
		using TemporaryFolder repositoryFolder = TemporaryFolder.Create();
		using TemporaryFolder destinationFolder = TemporaryFolder.Create();

		AtomicArtifactWriter writer = ReportAppendFixture.BuildWriter(repositoryFolder, destinationFolder);

		Assert.ThrowsAny<ArgumentException>(() => writer.WritePromptPackStreaming(stem, sink => sink.Write(_PACK_BODY)));
		Assert.Empty(Directory.GetFileSystemEntries(destinationFolder.Path));
	}

	[Fact]
	public void AMissingBodyDelegateIsRefused()
	{
		using TemporaryFolder repositoryFolder = TemporaryFolder.Create();
		using TemporaryFolder destinationFolder = TemporaryFolder.Create();

		AtomicArtifactWriter writer = ReportAppendFixture.BuildWriter(repositoryFolder, destinationFolder);

		Assert.Throws<ArgumentNullException>(() => writer.WritePromptPackStreaming("stem", null!));
	}

	[Fact]
	public void TheOtherWritesRefuseThePackKindBecauseItIsNotNamedByADate()
	{
		using TemporaryFolder repositoryFolder = TemporaryFolder.Create();
		using TemporaryFolder destinationFolder = TemporaryFolder.Create();

		AtomicArtifactWriter writer = ReportAppendFixture.BuildWriter(repositoryFolder, destinationFolder);

		Assert.Throws<ArgumentOutOfRangeException>(
			() => writer.WriteShareableArtifact(OutputArtifactKind.PromptPack, ReportAppendFixture.ArtifactDate, _PACK_BODY));
		Assert.Throws<ArgumentOutOfRangeException>(
			() => writer.WriteShareableArtifactStreaming(
				OutputArtifactKind.PromptPack,
				ReportAppendFixture.ArtifactDate,
				sink => sink.Write(_PACK_BODY)));
		Assert.Empty(Directory.GetFileSystemEntries(destinationFolder.Path));
	}

	[Fact]
	public void ADestinationInsideTheRepositoryIsRefusedAndTheBodyNeverRuns()
	{
		using TemporaryFolder repositoryFolder = TemporaryFolder.Create();

		string destinationPath = Path.Combine(repositoryFolder.Path, "output");
		AtomicArtifactWriter writer = new(
			RepositoryFolderPath.FromSuppliedText(repositoryFolder.Path),
			DocumentationDestinationFolderPath.FromSuppliedText(destinationPath));
		bool bodyRan = false;

		ArtifactWriteResult result = writer.WritePromptPackStreaming(
			AgentEnvironmentCatalog.OPENAI_CODEX_PACK_STEM,
			sink =>
			{
				bodyRan = true;
				sink.Write(_PACK_BODY);
			});

		Assert.False(result.Wrote);
		Assert.Equal(ArtifactWriteRefusal.DestinationOverlapsRepository, result.Refusal);
		Assert.False(bodyRan);
		Assert.False(Directory.Exists(destinationPath));
	}

	/// <summary>Every name taken: the refusal says "prompt pack", not "today's report".</summary>
	[Fact]
	public void WhenEveryNameIsTakenTheRefusalIsThePackMessageNotTheReportMessage()
	{
		using TemporaryFolder repositoryFolder = TemporaryFolder.Create();
		using TemporaryFolder destinationFolder = TemporaryFolder.Create();

		string stem = AgentEnvironmentCatalog.COPILOT_CLI_PACK_STEM;

		File.WriteAllText(destinationFolder.Combine(OutputArtifactNaming.BuildPromptPackFileName(stem, OutputArtifactNaming.UNSUFFIXED_ORDINAL)), "taken");

		for (int ordinal = OutputArtifactNaming.FIRST_COLLISION_SUFFIX; ordinal <= AtomicArtifactWriter.MAXIMUM_NAME_ORDINAL; ordinal++)
		{
			File.WriteAllText(destinationFolder.Combine(OutputArtifactNaming.BuildPromptPackFileName(stem, ordinal)), "taken");
		}

		ArtifactWriteResult result = ReportAppendFixture.BuildWriter(repositoryFolder, destinationFolder)
			.WritePromptPackStreaming(stem, sink => sink.Write(_PACK_BODY));

		Assert.False(result.Wrote);
		Assert.Equal(ArtifactWriteRefusal.NameCollisionLimitReached, result.Refusal);
		Assert.Equal(
			string.Format(
				CultureInfo.InvariantCulture,
				AtomicArtifactWriter.PACK_NAME_COLLISION_LIMIT_MESSAGE_FORMAT,
				destinationFolder.Path),
			result.RefusalMessage);
		Assert.DoesNotContain("today", result.RefusalMessage, StringComparison.OrdinalIgnoreCase);
		Assert.Matches(new Regex("prompt pack", RegexOptions.None, TimeSpan.FromSeconds(1)), result.RefusalMessage);
		Assert.Empty(Directory.GetFiles(destinationFolder.Path, ReportAppendFixture.TEMPORARY_FILE_SEARCH_PATTERN));
	}
}
