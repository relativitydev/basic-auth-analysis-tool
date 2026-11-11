using MigrateAuth.Cli.Agent;
using MigrateAuth.Cli.Session;

using Xunit;

namespace MigrateAuth.Cli.Tests.Agent;

/// <summary>
/// THE FIVE CASES IN WHICH NO LAUNCH IS OFFERED: not a terminal, the wrapper's command line outside the allow-list, a
/// control character in the copy's path, a match only inside the repository, and a wrapper from a repository that is not
/// on a local drive. In each the fixed line says why, the pack is written and the section is appended, no notice and no
/// question is printed, no launch folder is made and no agent is started. The three eligibility cases are driven through
/// a stub launcher, because a real wrapper exists only on Windows.
/// </summary>
public sealed class AiAssistNoOfferTests
{
	private const string _CONFIRMATION_PROMPT_START = "Launch Claude Code against the pack now?";
	private const string _COST_NOTICE_START = "Launching the review runs ";
	private static readonly string _COMMAND = AiAssistHarness.ClaudeCommandName;

	[Fact]
	public void NotATerminalWithholdsTheOfferAndSaysWhy()
	{
		using AiAssistHarness harness = new(["1", "y"]);

		harness.InstallFake(AgentEnvironment.ClaudeCode);
		harness.IsTerminal = false;

		StageRunOutcome outcome = harness.Run();

		Assert.Equal(StageRunOutcome.SelfReported, outcome);
		Assert.Contains(AiAssistTexts.NO_TERMINAL, harness.Transcript, StringComparison.Ordinal);
		AssertNoOffer(harness);
	}

	[Fact]
	public void ACommandLineOutsideTheAllowListWithholdsTheOfferAndSaysWhy()
	{
		AssertEligibilityLine(
			AgentLaunchEligibility.UnsafeCommandLine,
			$"ai-assist: {_COMMAND} is installed as a script wrapper, and the command line needed to start it would contain a character that the Windows command interpreter cannot carry safely (for example in a folder name). The prompt pack was written; nothing was launched.");
	}

	[Fact]
	public void AControlCharacterInThePathWithholdsTheOfferAndSaysWhy()
	{
		AssertEligibilityLine(
			AgentLaunchEligibility.PathContainsControlCharacter,
			"ai-assist: the path of the pack contains a control or formatting character, so a launch is not offered. The prompt pack was written; nothing was launched.");
	}

	[Fact]
	public void AWrapperFromARepositoryThatIsNotLocalWithholdsTheOfferAndSaysWhy()
	{
		AssertEligibilityLine(
			AgentLaunchEligibility.WrapperNeedsLocalRepository,
			$"ai-assist: {_COMMAND} is installed as a script wrapper, and the Windows command interpreter cannot start it in a repository that is not on a local drive letter (for example a network path), so a launch is not offered. Map the share to a drive letter, or copy the repository to a local folder, and start the session from there. The prompt pack was written; nothing was launched.");
	}

	/// <summary>The only command of that name lies inside the repository: it is ignored and the line says so.</summary>
	[Fact]
	public void AMatchOnlyInsideTheRepositoryWithholdsTheOfferAndSaysWhy()
	{
		using AiAssistHarness harness = new(["1", "y"]);
		string insideRepository = Path.Combine(harness.Repository.Path, AiAssistHarness.REPOSITORY_TOOLS_FOLDER);

		Directory.CreateDirectory(insideRepository);
		TestAgentResolver.WriteCommandFile(insideRepository, FakeAgentFolder.ExecutableName(_COMMAND));
		harness.SearchPath = new TestAgentSearchPathProvider([insideRepository]);

		StageRunOutcome outcome = harness.Run();

		Assert.Equal(StageRunOutcome.SelfReported, outcome);
		Assert.Contains(
			$"ai-assist: {_COMMAND} was found only inside the repository being assessed, and a match there is ignored, because the repository must not choose what runs. The prompt pack was written; nothing was launched.",
			harness.Transcript,
			StringComparison.Ordinal);
		AssertNoOffer(harness);
	}

	private static void AssertEligibilityLine(AgentLaunchEligibility eligibility, string expectedLine)
	{
		using AiAssistHarness harness = new(["1", "y"]);
		StubLauncher launcher = new(AgentAvailability.Available, eligibility, AgentTerminationStatus.NotFound());

		harness.InstallFake(AgentEnvironment.ClaudeCode);
		harness.LauncherOverride = launcher;

		StageRunOutcome outcome = harness.Run();

		Assert.Equal(StageRunOutcome.SelfReported, outcome);
		Assert.Contains(expectedLine, harness.Transcript, StringComparison.Ordinal);
		Assert.Equal(0, launcher.LaunchCount);
		AssertNoOffer(harness);
	}

	private static void AssertNoOffer(AiAssistHarness harness)
	{
		byte[] report = harness.ReportBytes();

		Assert.Equal([AiAssistHarness.CLAUDE_PACK_FILE_NAME], harness.PackFiles());
		Assert.Contains(AiAssistTexts.SECTION_APPENDED_FORMAT[..AiAssistTexts.SECTION_APPENDED_FORMAT.IndexOf("{0}", StringComparison.Ordinal)], harness.Transcript, StringComparison.Ordinal);
		Assert.EndsWith($"This report is not part of the pack.{AiAssistHarness.LINE_FEED}", System.Text.Encoding.UTF8.GetString(report), StringComparison.Ordinal);
		Assert.DoesNotContain(_CONFIRMATION_PROMPT_START, harness.Transcript, StringComparison.Ordinal);
		Assert.DoesNotContain(_COST_NOTICE_START, harness.Transcript, StringComparison.Ordinal);
		Assert.Equal(1, harness.Input.ReadCount);
		harness.AssertNothingWasLaunched();
	}
}
