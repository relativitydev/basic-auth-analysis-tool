using System.Collections.Immutable;

using MigrateAuth.Cli.Agent;
using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Emission;
using MigrateAuth.Cli.Findings;
using MigrateAuth.Cli.Tests.Reporting;
using MigrateAuth.Cli.Session;
using MigrateAuth.Cli.Tests.Session;

using Xunit;

namespace MigrateAuth.Cli.Tests.Agent;

/// <summary>
/// NO REPOSITORY CONTENT REACHES THE PACK. A repository is made on disk whose file CONTENT carries one marker and whose file
/// NAME carries another. The pack is built from the finding record alone, so the content marker is absent and the name marker
/// is present only inside the path field of an entry line.
/// </summary>
public sealed class PromptPackContentLeakTests
{
	private const string _CONTENT_MARKER = "CONTENT-MARKER-7f3a";
	private const string _NAME_MARKER = "NAME-MARKER-91bc";
	private const string _FILE_NAME = $"{_NAME_MARKER}.cs";

	[Fact]
	public void ContentMarkersNeverReachThePackAndNameMarkersAppearOnlyInThePathField()
	{
		using TemporaryFolder repository = TemporaryFolder.Create();

		File.WriteAllText(Path.Combine(repository.Path, _FILE_NAME), $"// {_CONTENT_MARKER}{AiAssistHarness.LINE_FEED}var credential = \"{_CONTENT_MARKER}\";{AiAssistHarness.LINE_FEED}");

		RepositoryFolderPath repositoryPath = RepositoryFolderPath.FromSuppliedText(repository.Path);

		Assert.True(RepositoryRelativePath.TryFromScannedPath(repositoryPath, Path.Combine(repository.Path, _FILE_NAME), out RepositoryRelativePath path));

		Finding finding = ReportHarness.FindingAt(1, "src/placeholder.cs", 1, ReportHarness.TEMPLATED_PATTERN_CLASS, ConfidenceLevel.High, flow: CredentialFlow.ClientCredentials, flowDecision: FlowDecision.FixedSignal) with
		{
			Path = path
		};

		string pack = PromptPackFixture.Body(
			AgentEnvironment.ClaudeCode,
			new PromptPackScope(ImmutableArray.Create(finding), ConfidenceLevel.Low, PromptPackFixture.Catalog));

		Assert.DoesNotContain(_CONTENT_MARKER, pack, StringComparison.Ordinal);
		Assert.DoesNotContain(repository.Path, pack, StringComparison.OrdinalIgnoreCase);

		string[] linesWithTheName = [.. pack.Split(AiAssistHarness.LINE_FEED_CHARACTER).Where(candidate => candidate.Contains(_NAME_MARKER, StringComparison.Ordinal))];
		string line = Assert.Single(linesWithTheName);

		Assert.StartsWith("- 001 | ", line, StringComparison.Ordinal);
		Assert.EndsWith($"| ` {_FILE_NAME} `", line, StringComparison.Ordinal);
	}

	[Fact]
	public void EachInstructionTextIsWrittenOncePerSectionThatHoldsItsKind()
	{
		string pack = PromptPackFixture.Body(AgentEnvironment.ClaudeCode, PromptPackFixture.Scope());

		Assert.Equal(1, CountOf(pack, PromptPackInstructionText.MANUAL_REVIEW_TEXT));
		Assert.Equal(1, CountOf(pack, PromptPackInstructionText.UNCHARACTERIZED_CONSTRUCT_TEXT));
		Assert.Equal(2, CountOf(pack, PromptPackInstructionText.CHARACTERIZED_CONSTRUCT_TEXT));
	}

	private static int CountOf(string text, string fragment)
	{
		int count = 0;
		int position = text.IndexOf(fragment, StringComparison.Ordinal);

		while (position >= 0)
		{
			count++;
			position = text.IndexOf(fragment, position + fragment.Length, StringComparison.Ordinal);
		}

		return count;
	}
}
