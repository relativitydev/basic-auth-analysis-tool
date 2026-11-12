using System.Security.Cryptography;

using MigrateAuth.Cli.Agent;
using MigrateAuth.Cli.Writing;

using MigrateAuth.FakeAgent;

namespace MigrateAuth.Cli.Tests.Agent;

/// <summary>One scenario's console transcript, and every absolute path that belonged to it and must not appear there.</summary>
internal sealed record ScenarioResult(string Name, string Transcript, string[] AbsolutePaths);

/// <summary>
/// EVERY BRANCH OF THE STAGE THAT PRINTS, RUN ONCE: selection, cancellation, generation, each append outcome, each
/// availability and no-offer line, the notices for all three environments, the confirmation and every way a launch can end.
/// The launch branches use the fakes as real child processes except the few a platform or a clock cannot give (a signal on
/// Windows, an expired budget, a start failure), which use a stub launcher over a real resolution. The results are computed
/// once and shared by the tests that read them, so the console-string and the no-absolute-path tests look at the same runs.
/// </summary>
internal static class AiAssistScenarios
{
	private const int _FAKE_EXIT_CODE = 7;
	private const int _SIGNAL_NUMBER = 15;
	private static readonly string _HOSTILE_ANSWER = $"{AiAssistHarness.ESCAPE_CHARACTER}[31mHOSTILE-ANSWER{AiAssistHarness.BELL_CHARACTER}";

	private static readonly Lazy<IReadOnlyList<ScenarioResult>> _all = new(RunAll);

	internal static IReadOnlyList<ScenarioResult> All => _all.Value;

	private static List<ScenarioResult> RunAll()
	{
		List<ScenarioResult> results = [];

		AddSelectionScenarios(results);
		AddGenerationScenarios(results);
		AddAvailabilityScenarios(results);
		AddLaunchScenarios(results);

		return results;
	}

	private static void AddSelectionScenarios(List<ScenarioResult> results)
	{
		results.Add(Run("cursor then cancel", ["cursor", _HOSTILE_ANSWER], harness => { }));
		results.Add(Run("reask then select", ["x", "1"], harness => harness.IsTerminal = false));
		results.Add(Run("empty line", [string.Empty], harness => { }));
		results.Add(Run("interrupted", ["1"], harness => { }, cancelled: true));
	}

	private static void AddGenerationScenarios(List<ScenarioResult> results)
	{
		results.Add(Run("generate only", ["2"], harness => GenerateOnly(harness)));
		results.Add(Run("generation failure", ["1"], harness => harness.ReplaceDestinationWithFile()));
		results.Add(Run("append refused, report changed", ["1"], harness => EditTheReport(harness)));
		results.Add(Run("append refused, not writable", ["1"], harness => harness.AppenderOverride = new RefusingAppender(ReportAppendRefusal.NotWritable)));
		results.Add(Run("append refused, outside the boundary", ["1"], harness => harness.AppenderOverride = new RefusingAppender(ReportAppendRefusal.OutsideBoundary)));
		results.Add(Run("append refused, replaced or linked", ["1"], harness => harness.AppenderOverride = new RefusingAppender(ReportAppendRefusal.ReportReplacedOrLinked)));
		results.Add(Run("resumed", ["1"], harness => harness.IsTerminal = false, writeReport: false));
		results.Add(Run("report not recorded", ["1"], harness => harness.IsTerminal = false, hashFactory: () => throw new CryptographicException("unavailable")));
	}

	private static void AddAvailabilityScenarios(List<ScenarioResult> results)
	{
		results.Add(Run("not found", ["1"], harness => { }));
		results.Add(Run("match inside repository", ["1"], harness => PlaceMatchInsideTheRepository(harness)));

		foreach (AgentLaunchEligibility eligibility in (AgentLaunchEligibility[])[
			AgentLaunchEligibility.UnsafeCommandLine,
			AgentLaunchEligibility.PathContainsControlCharacter,
			AgentLaunchEligibility.WrapperNeedsLocalRepository])
		{
			results.Add(Run(
				$"withheld: {eligibility}",
				["1"],
				harness => harness.LauncherOverride = new StubLauncher(AgentAvailability.Available, eligibility, AgentTerminationStatus.NotFound())));
		}
	}

	private static void AddLaunchScenarios(List<ScenarioResult> results)
	{
		foreach (AgentEnvironment environment in Enum.GetValues<AgentEnvironment>())
		{
			string selection = AgentEnvironmentCatalog.Describe(environment).SelectionNumber.ToString(System.Globalization.CultureInfo.InvariantCulture);

			results.Add(Run($"declined {environment}", [selection, _HOSTILE_ANSWER], harness => harness.InstallFake(environment)));
		}

		results.Add(Run("exit zero", ["1", "y"], harness => harness.InstallFake(AgentEnvironment.ClaudeCode)));
		results.Add(Run("exit non-zero", ["2", "y"], harness => harness.InstallFake(AgentEnvironment.OpenAiCodex, new FakeAgentBehavior { ExitCode = _FAKE_EXIT_CODE })));
		results.Add(Run("interrupted by the user", ["3", "y"], harness => harness.InstallFake(AgentEnvironment.GitHubCopilotCli, new FakeAgentBehavior { Ending = FakeAgentEnding.Interrupt })));
		results.Add(Run("signal", ["1", "y"], harness => LaunchWithStatus(harness, AgentTerminationStatus.TerminatedBySignal(_SIGNAL_NUMBER))));
		results.Add(Run("timed out", ["1", "y"], harness => LaunchWithStatus(harness, AgentTerminationStatus.TimedOut())));
		results.Add(Run("could not be started", ["1", "y"], harness => LaunchWithStatus(harness, AgentTerminationStatus.NotFound())));

		if (OperatingSystem.IsWindows())
		{
			results.Add(Run("wrapper, declined", ["1", "n"], harness => harness.Fakes.InstallCmdOnly(AiAssistHarness.ClaudeCommandName, new FakeAgentBehavior())));
		}
	}

	private static void GenerateOnly(AiAssistHarness harness)
	{
		harness.IsTerminal = false;
		harness.InstallFake(AgentEnvironment.OpenAiCodex);
	}

	private static void LaunchWithStatus(AiAssistHarness harness, AgentTerminationStatus status)
	{
		harness.InstallFake(AgentEnvironment.ClaudeCode);
		harness.LauncherOverride = new StubLauncher(AgentAvailability.Available, AgentLaunchEligibility.Launchable, status);
	}

	private static void EditTheReport(AiAssistHarness harness)
	{
		harness.EditTheReportInPlace();
		harness.IsTerminal = false;
	}

	private static void PlaceMatchInsideTheRepository(AiAssistHarness harness)
	{
		string insideRepository = Path.Combine(harness.Repository.Path, AiAssistHarness.REPOSITORY_TOOLS_FOLDER);

		Directory.CreateDirectory(insideRepository);
		TestAgentResolver.WriteCommandFile(insideRepository, FakeAgentFolder.ExecutableName(AiAssistHarness.ClaudeCommandName));
		harness.SearchPath = new TestAgentSearchPathProvider([insideRepository]);
	}

	private static ScenarioResult Run(
		string name,
		string[] inputs,
		Action<AiAssistHarness> arrange,
		bool cancelled = false,
		bool writeReport = true,
		Func<IncrementalHash>? hashFactory = null)
	{
		using AiAssistHarness harness = new(inputs, hashFactory, writeReport);
		using CancellationTokenSource cancellation = new();

		arrange(harness);

		if (cancelled)
		{
			harness.Input.OnRead = _ => cancellation.Cancel();
		}

		harness.Run(cancellation.Token);

		return new ScenarioResult(name, harness.Transcript, [.. harness.KnownAbsolutePaths(), .. harness.LaunchFolders()]);
	}
}
