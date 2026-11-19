using System.Diagnostics;
using System.Globalization;

using MigrateAuth.Cli.Session;

namespace MigrateAuth.Cli.Agent;

/// <summary>
/// WHAT THE THREE LAUNCHERS SHARE: resolving the agent once, answering whether the launch can be offered,
/// building the one prompt argument, and starting the child.
///
/// The argument is the static sentence, a space, and the absolute path of the launch copy framed as a code
/// span that the path itself cannot close. Nothing else is passed that carries meaning for the agent's
/// permissions: no flag that relaxes a permission mode and none that tightens it, because a command-line mode
/// overrides the mode the user configured, so even a restrictive flag can loosen a stricter choice. The tool's
/// own prompts ask the user for the one file. A subclass decides only how the prompt argument is arranged in
/// the argument vector.
///
/// The launch uses the exact path the availability check resolved. A launch that cannot be made safely, or
/// whose process cannot be started, reports <see cref="AgentTerminationOutcome.NotFound"/> and says nothing
/// more: no exception text leaves this class.
/// </summary>
public abstract class ExternalAgentLauncherBase : IExternalAgentLauncher
{
	/// <summary>
	/// The sentence every launch starts with. It begins with a letter, so no agent reads it as a command,
	/// a shell escape, a file mention or a memory note, and it holds only letters, spaces and one colon, so it
	/// satisfies the allow-list a wrapper command line is held to.
	/// </summary>
	public const string LAUNCH_SENTENCE =
		"Carry out the review described in the prompt pack file at this path and follow its instructions exactly:";

	private const string _SENTENCE_SEPARATOR = " ";

	private readonly AgentSearchPathSnapshot _searchPath;
	private readonly AgentExecutableResolver _resolver;
	private readonly RepositoryFolderPath _repository;
	private readonly TimeSpan? _testBudget;
	private readonly ConsoleModeGuard _consoleGuard = ConsoleModeGuard.ForThisPlatform();

	/// <summary>Creates a launcher for <paramref name="environment"/>, which names the command it resolves.</summary>
	protected ExternalAgentLauncherBase(
		AgentEnvironment environment,
		IAgentSearchPathProvider searchPathProvider,
		RepositoryFolderPath repository,
		TimeSpan? testBudget)
	{
		ArgumentNullException.ThrowIfNull(searchPathProvider);

		_searchPath = new AgentSearchPathSnapshot(searchPathProvider);
		_repository = repository;
		_testBudget = testBudget;
		_resolver = new AgentExecutableResolver(
			_searchPath,
			repository,
			AgentEnvironmentCatalog.Describe(environment).CommandName);
	}

	/// <summary>The prompt argument for a launch copy at <paramref name="launchCopyPath"/>.</summary>
	public static string BuildPromptArgument(string launchCopyPath)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(launchCopyPath);

		return $"{LAUNCH_SENTENCE}{_SENTENCE_SEPARATOR}{PathCodeSpan.Frame(launchCopyPath)}";
	}

	/// <inheritdoc />
	public AgentAvailability CheckAvailability()
	{
		AgentResolution resolution = _resolver.Resolve();
		AgentAvailability availability = AgentAvailability.NotFound;

		if (resolution.WasFound)
		{
			availability = AgentAvailability.Available;
		}
		else if (resolution.MatchInsideRepositoryWasIgnored)
		{
			availability = AgentAvailability.NotFoundMatchInsideRepositoryIgnored;
		}

		return availability;
	}

	/// <inheritdoc />
	public AgentLaunchEligibility CheckLaunchable(PromptPackFilePath writtenPack)
	{
		RequireDeclared(writtenPack);

		AgentResolution resolution = _resolver.Resolve();

		if (!resolution.WasFound)
		{
			throw new InvalidOperationException("No agent was resolved, so there is nothing to offer.");
		}

		string destinationFolder = Path.GetDirectoryName(writtenPack.AbsolutePath) ?? string.Empty;

		return Evaluate(resolution, LaunchCopyLayout.PredictCopyPath(destinationFolder, writtenPack.FileName));
	}

	/// <inheritdoc />
	public AgentResolutionDescription DescribeResolution()
	{
		AgentResolution resolution = _resolver.Resolve();

		if (!resolution.WasFound)
		{
			throw new InvalidOperationException("No agent was resolved, so there is nothing to describe.");
		}

		return new AgentResolutionDescription(
			resolution.ExecutableFileName,
			resolution.IsWrapperScript ? AgentStartKind.ThroughCommandInterpreter : AgentStartKind.Direct,
			resolution.EntryPosition,
			resolution.EntryCount);
	}

	/// <inheritdoc />
	public AgentTerminationStatus Launch(PromptPackFilePath launchCopy)
	{
		RequireDeclared(launchCopy);

		AgentResolution resolution = _resolver.Resolve();
		AgentTerminationStatus status = AgentTerminationStatus.NotFound();

		if (resolution.WasFound && Evaluate(resolution, launchCopy.AbsolutePath) == AgentLaunchEligibility.Launchable)
		{
			ProcessStartInfo startInfo = AgentChildProcess.CreateStartInfo(
				resolution,
				ArrangeArguments(BuildPromptArgument(launchCopy.AbsolutePath)),
				_repository.CanonicalPath,
				_searchPath.GetSearchPathEntries());

			status = RunShielded(startInfo);
		}

		return status;
	}

	/// <summary>
	/// The argument vector for the prompt argument: the prompt argument alone, or preceded by the one
	/// documented switch that makes the tool start an interactive session with it.
	/// </summary>
	protected abstract IReadOnlyList<string> ArrangeArguments(string promptArgument);

	/// <summary>
	/// Runs the child with the quit signal shielded and the console modes saved before and restored after,
	/// for this launch only.
	/// </summary>
	private AgentTerminationStatus RunShielded(ProcessStartInfo startInfo)
	{
		using InterruptShield shield = InterruptShield.Begin();

		return _consoleGuard.Run(() => ToStatus(AgentChildProcess.Run(startInfo, _testBudget)));
	}

	private AgentLaunchEligibility Evaluate(AgentResolution resolution, string copyPath)
	{
		AgentLaunchEligibility eligibility = AgentLaunchEligibility.Launchable;

		if (ContainsControlCharacter(copyPath))
		{
			eligibility = AgentLaunchEligibility.PathContainsControlCharacter;
		}
		else if (resolution.IsWrapperScript)
		{
			eligibility = EvaluateWrapper(resolution, copyPath);
		}

		return eligibility;
	}

	private AgentLaunchEligibility EvaluateWrapper(AgentResolution resolution, string copyPath)
	{
		IReadOnlyList<string> arguments = ArrangeArguments(BuildPromptArgument(copyPath));
		AgentLaunchEligibility eligibility = AgentLaunchEligibility.Launchable;

		if (!AgentCommandLineSafety.IsWrapperLaunchSafe(resolution.ExecutablePath, copyPath, arguments))
		{
			eligibility = AgentLaunchEligibility.UnsafeCommandLine;
		}
		else if (!WrapperWorkingDirectoryRule.AllowsWrapperLaunch(_repository.CanonicalPath))
		{
			eligibility = AgentLaunchEligibility.WrapperNeedsLocalRepository;
		}

		return eligibility;
	}

	private static bool ContainsControlCharacter(string path)
	{
		bool found = false;

		foreach (char character in path)
		{
			found |= char.IsControl(character)
				|| CharUnicodeInfo.GetUnicodeCategory(character) is UnicodeCategory.LineSeparator or UnicodeCategory.ParagraphSeparator;
		}

		return found;
	}

	private static void RequireDeclared(PromptPackFilePath pack)
	{
		if (!pack.IsDeclared)
		{
			throw new ArgumentException("The pack path names no file.", nameof(pack));
		}
	}

	private static AgentTerminationStatus ToStatus(AgentChildResult result)
	{
		AgentTerminationStatus status;

		if (!result.Started)
		{
			status = AgentTerminationStatus.NotFound();
		}
		else if (result.TimedOut)
		{
			status = AgentTerminationStatus.TimedOut();
		}
		else
		{
			status = AgentExitMapping.Map(result.ExitCode, OperatingSystem.IsWindows());
		}

		return status;
	}
}
