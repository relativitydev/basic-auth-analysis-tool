using MigrateAuth.Cli.Agent;

using MigrateAuth.FakeAgent;

using Xunit;

namespace MigrateAuth.Cli.Tests.Agent;

/// <summary>
/// WHAT THE USER IS TOLD BEFORE THE QUESTION, ELEMENT BY ELEMENT, OVER THE THREE ENVIRONMENTS. Each of the twelve
/// elements of the cost notice and the repository-access statement is a fixed phrase, written here as a literal and
/// filled with the environment's own agent and provider names, and each is checked in the console transcript between
/// the resolution line and the confirmation question. The entry count and the provider appear in the notice, and
/// declining, the default, launches nothing.
/// </summary>
public sealed class AiAssistNoticeElementTests
{
	private const int _FIXTURE_ENTRY_COUNT = 6;

	private static readonly (AgentEnvironment Environment, string Agent, string Provider, string PermissionsOwn, string AccessOwn)[] _environments =
	[
		(
			AgentEnvironment.ClaudeCode,
			"Claude Code",
			"Anthropic",
			"In current versions Claude Code may start in a mode in which its own automatic review approves actions instead of asking you; choose a stricter permission mode in Claude Code before launching if you want to approve each action.",
			"Claude Code may ask you to allow reading the copy of the pack, which lies outside the repository."
		),
		(
			AgentEnvironment.OpenAiCodex,
			"OpenAI Codex",
			"OpenAI",
			"In a folder under version control OpenAI Codex may start with write access to the workspace and ask for approval only for some actions; choose a stricter approval and sandbox setting in OpenAI Codex before launching if you want to approve each action.",
			"Access outside the repository follows OpenAI Codex's own sandbox and approval settings."
		),
		(
			AgentEnvironment.GitHubCopilotCli,
			"GitHub Copilot CLI",
			"GitHub",
			"GitHub Copilot CLI asks before it uses a tool unless your settings or the folder's settings allow it; choose stricter settings in GitHub Copilot CLI before launching if you want to approve each action.",
			"GitHub Copilot CLI may also read the system temporary folder under its own defaults."
		)
	];

	private const string _INTERNAL_TAG_PATTERN = @"\[[CR]\d\]";
	private const string _MARKDOWN_BOLD_MARK = "**";
	private const string _CARRIAGE_RETURN = "\r";

	/// <summary>The console text with line feeds only, so a paragraph can be matched on every platform.</summary>
	private static string LinesOnly(string consoleText)
	{
		return consoleText.Replace(_CARRIAGE_RETURN, string.Empty, StringComparison.Ordinal);
	}

	private const string _PERMISSIONS_COMMON =
		"The agent runs under your own agent's permission settings; depending on them, and on the repository's own agent settings once you trust the folder, it may read files, change files or run commands without asking you. The pack asks it to change nothing.";

	private const string _ACCESS_COMMON =
		"Beyond the repository and the folder that holds a copy of the pack, this tool gives the agent access to nothing; what the agent can reach under your own permissions is governed by the agent, not by this tool.";

	/// <summary>The twelve elements, each as the fixed phrase it must be found as, for an agent and a provider.</summary>
	private static readonly (string Element, Func<string, string, string, string, string> Phrase)[] _elements =
	[
		("C1", (agent, provider, _, _) => $"Launching the review runs {agent} under your own subscription or account with {provider}"),
		("C1-charge", (_, provider, _, _) => $"and {provider} may charge you for it"),
		("C2", (_, provider, _, _) => $"Those charges are billed by {provider}; they are not charges for this tool"),
		("C3", (_, _, _, _) => "they are outside this engagement's contract"),
		("C4", (_, _, _, _) => $"The cost grows with the size of the repository and with the number of findings under review; this pack lists {_FIXTURE_ENTRY_COUNT} entries"),
		("C5", (_, provider, _, _) => $"under your own subscription or account with {provider}"),
		("R1", (agent, _, _, _) => $"If you launch, {agent} is started in the repository folder and is given access to its contents, including the places the pack lists where credential material occurs"),
		("R2", (_, provider, _, _) => $"What the agent reads is sent to {provider}"),
		("R3", (agent, _, _, _) => $"{agent} may ask you to trust this folder; trusting it lets the repository's own agent configuration (instructions, settings, hooks and servers) apply, so the agent may follow instructions and run commands defined there, in addition to your own agent settings"),
		("R4", (_, provider, _, _) => $"The full path of the pack file, which may include your user name and folder names, is sent to the agent and to {provider}"),
		("R5", (_, _, _, _) => "Generating the pack alone sends nothing"),
		("R6", (_, _, own, _) => $"{_PERMISSIONS_COMMON} {own}"),
		("R7", (_, _, _, own) => $"{_ACCESS_COMMON} {own}")
	];

	public static TheoryData<AgentEnvironment, string> EnvironmentsByElement
	{
		get
		{
			TheoryData<AgentEnvironment, string> data = [];

			foreach ((AgentEnvironment environment, _, _, _, _) in _environments)
			{
				foreach ((string element, _) in _elements)
				{
					data.Add(environment, element);
				}
			}

			return data;
		}
	}

	[Theory]
	[MemberData(nameof(EnvironmentsByElement))]
	public void EachElementOfTheNoticesIsOnTheConsoleBeforeTheQuestion(AgentEnvironment environment, string element)
	{
		(AgentEnvironment _, string agent, string provider, string permissionsOwn, string accessOwn) = _environments.Single(row => row.Environment == environment);
		string phrase = _elements.Single(row => row.Element == element).Phrase(agent, provider, permissionsOwn, accessOwn);
		string selection = AgentEnvironmentCatalog.Describe(environment).SelectionNumber.ToString(System.Globalization.CultureInfo.InvariantCulture);

		using AiAssistHarness harness = new([selection, string.Empty]);

		harness.InstallFake(environment);
		harness.Run();

		string beforeQuestion = harness.Input.OutputBeforeEachRead[^1];

		Assert.Contains(phrase, beforeQuestion, StringComparison.Ordinal);
		Assert.EndsWith(AiAssistTexts.Confirmation(environment), beforeQuestion, StringComparison.Ordinal);
	}

	[Theory]
	[InlineData(AgentEnvironment.ClaudeCode)]
	[InlineData(AgentEnvironment.OpenAiCodex)]
	[InlineData(AgentEnvironment.GitHubCopilotCli)]
	public void TheNoticesComeInOrderAfterTheResolutionLineAndTheCountLineClosesThemBeforeTheQuestion(AgentEnvironment environment)
	{
		string selection = AgentEnvironmentCatalog.Describe(environment).SelectionNumber.ToString(System.Globalization.CultureInfo.InvariantCulture);

		using AiAssistHarness harness = new([selection, string.Empty]);

		harness.InstallFake(environment);
		harness.Run();

		string beforeQuestion = LinesOnly(harness.Input.OutputBeforeEachRead[^1]);
		int resolution = beforeQuestion.IndexOf("ai-assist: found ", StringComparison.Ordinal);
		int cost = beforeQuestion.IndexOf($"{AiAssistHarness.LINE_FEED}{AiAssistTexts.COST_NOTICE_TITLE}{AiAssistHarness.LINE_FEED}", StringComparison.Ordinal);
		int access = beforeQuestion.IndexOf($"{AiAssistHarness.LINE_FEED}{AiAssistTexts.REPOSITORY_ACCESS_TITLE}{AiAssistHarness.LINE_FEED}", StringComparison.Ordinal);
		int settings = beforeQuestion.IndexOf($"{AiAssistHarness.LINE_FEED}{AiAssistTexts.AGENT_SETTINGS_TITLE}{AiAssistHarness.LINE_FEED}", StringComparison.Ordinal);
		int count = beforeQuestion.IndexOf("ai-assist: the pack lists 6 entries:", StringComparison.Ordinal);
		int question = beforeQuestion.IndexOf("Launch ", StringComparison.Ordinal);

		Assert.True(resolution >= 0 && resolution < cost && cost < access && access < settings && settings < count && count < question);
		Assert.Contains("6 entries: 3 ambiguous cases, 1 finding whose flow was defaulted, 2 others for confirmation.", beforeQuestion, StringComparison.Ordinal);
	}

	[Fact]
	public void TheCostNoticeOnTheConsoleIsTheSameFunctionTheWrittenPackUses()
	{
		using AiAssistHarness harness = new(["1", string.Empty]);

		harness.InstallFake(AgentEnvironment.ClaudeCode);
		harness.Run();

		string pack = File.ReadAllText(harness.DestinationFolder.Combine(AiAssistHarness.CLAUDE_PACK_FILE_NAME));
		string body = AiAssistTexts.CostNoticeBody(AgentEnvironment.ClaudeCode, _FIXTURE_ENTRY_COUNT);

		Assert.Contains(body, harness.Transcript, StringComparison.Ordinal);
		Assert.Contains(AiAssistTexts.CostNotice(AgentEnvironment.ClaudeCode, _FIXTURE_ENTRY_COUNT), pack, StringComparison.Ordinal);
		Assert.EndsWith(body, AiAssistTexts.CostNotice(AgentEnvironment.ClaudeCode, _FIXTURE_ENTRY_COUNT), StringComparison.Ordinal);
	}

	[Theory]
	[InlineData(AgentEnvironment.ClaudeCode)]
	[InlineData(AgentEnvironment.OpenAiCodex)]
	[InlineData(AgentEnvironment.GitHubCopilotCli)]
	public void EachNoticeIsItsOwnParagraphWithItsTitleOnALineAndNoMarkdownMark(AgentEnvironment environment)
	{
		string selection = AgentEnvironmentCatalog.Describe(environment).SelectionNumber.ToString(System.Globalization.CultureInfo.InvariantCulture);
		string blankLine = $"{AiAssistHarness.LINE_FEED}{AiAssistHarness.LINE_FEED}";

		using AiAssistHarness harness = new([selection, string.Empty]);

		harness.InstallFake(environment);
		harness.Run();

		string beforeQuestion = LinesOnly(harness.Input.OutputBeforeEachRead[^1]);
		string costParagraph = string.Join(AiAssistHarness.LINE_FEED, AiAssistTexts.CostNoticeLines(environment, _FIXTURE_ENTRY_COUNT));
		string accessParagraph = string.Join(AiAssistHarness.LINE_FEED, AiAssistTexts.RepositoryAccessLines(environment));
		string settingsParagraph = string.Join(AiAssistHarness.LINE_FEED, AiAssistTexts.AgentSettingsLines(environment));

		Assert.DoesNotContain(_MARKDOWN_BOLD_MARK, beforeQuestion, StringComparison.Ordinal);
		Assert.Contains($"{blankLine}{costParagraph}{blankLine}{accessParagraph}{blankLine}{settingsParagraph}{blankLine}", beforeQuestion, StringComparison.Ordinal);
		Assert.EndsWith($"{blankLine}{AiAssistTexts.Confirmation(environment)}", beforeQuestion, StringComparison.Ordinal);
		Assert.All(AiAssistTexts.RepositoryAccessPoints(environment), point => Assert.Contains($"{AiAssistHarness.LINE_FEED}{AiAssistTexts.NOTICE_POINT_MARK}{point}{AiAssistHarness.LINE_FEED}", beforeQuestion, StringComparison.Ordinal));
	}

	[Theory]
	[InlineData(AgentEnvironment.ClaudeCode)]
	[InlineData(AgentEnvironment.OpenAiCodex)]
	[InlineData(AgentEnvironment.GitHubCopilotCli)]
	public void NoShippedNoticeOnTheConsoleOrInThePackCarriesAnInternalTag(AgentEnvironment environment)
	{
		string selection = AgentEnvironmentCatalog.Describe(environment).SelectionNumber.ToString(System.Globalization.CultureInfo.InvariantCulture);

		using AiAssistHarness harness = new([selection, string.Empty]);

		harness.InstallFake(environment);
		harness.Run();

		string[] texts =
		[
			AiAssistTexts.CostNotice(environment, _FIXTURE_ENTRY_COUNT),
			AiAssistTexts.RepositoryAccessStatement(environment),
			harness.Transcript
		];

		foreach (string text in texts)
		{
			Assert.DoesNotMatch(_INTERNAL_TAG_PATTERN, text);
		}

		foreach (string packFile in harness.PackFiles())
		{
			Assert.DoesNotMatch(_INTERNAL_TAG_PATTERN, File.ReadAllText(harness.DestinationFolder.Combine(packFile)));
		}
	}

	[Theory]
	[InlineData("")]
	[InlineData("n")]
	[InlineData("no")]
	[InlineData("anything else")]
	public void DecliningLaunchesNothingAndLeavesThePackInPlace(string answer)
	{
		using AiAssistHarness harness = new(["1", answer]);

		harness.InstallFake(AgentEnvironment.ClaudeCode, new FakeAgentBehavior());
		harness.Run();

		Assert.Contains(AiAssistTexts.LAUNCH_DECLINED, harness.Transcript, StringComparison.Ordinal);
		Assert.Equal([AiAssistHarness.CLAUDE_PACK_FILE_NAME], harness.PackFiles());
		harness.AssertNothingWasLaunched();
	}
}
