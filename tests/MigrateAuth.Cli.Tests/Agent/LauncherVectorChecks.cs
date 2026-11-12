using System.Globalization;

using MigrateAuth.Cli.Agent;

using MigrateAuth.FakeAgent;

using Xunit;

namespace MigrateAuth.Cli.Tests.Agent;

/// <summary>
/// The checks the three launcher test classes share: the argument vector a launcher passes to its fake has the
/// documented count and shape and none of the flags the tool's documentation names as forbidden, the child
/// starts in the repository, and nothing but the launch copy's own path reaches the documentation destination.
/// Each launcher class supplies its own documented shape and its own deny-list; the stamp check keeps the
/// provenance of both in the class that owns them.
/// </summary>
internal static class LauncherVectorChecks
{
	private const string _DESTINATION_WITH_SPACES = "audit docs";
	private const string _DOCUMENTATION_DATE_FORMAT = "yyyy-MM-dd";
	private const string _FLAG_PREFIX = "-";
	private const string _HTTPS_SCHEME = "https";

	/// <summary>
	/// The permission-mode values a command-line mode flag takes. None may appear as an argument, in any
	/// launcher: the launch carries no flag that relaxes or tightens a permission mode.
	/// </summary>
	internal static readonly string[] PermissionModeValues =
		["default", "plan", "auto", "acceptEdits", "bypassPermissions", "dontAsk", "interactive", "autopilot"];

	/// <summary>Launches the environment's fake and checks the recorded vector against the documented shape.</summary>
	internal static void AssertDocumentedVector(
		AgentEnvironment environment,
		string[] documentedLeadingArguments,
		string[] forbiddenArguments)
	{
		string commandName = AgentEnvironmentCatalog.Describe(environment).CommandName;

		using LauncherScenario scenario = new(_DESTINATION_WITH_SPACES);

		PromptPackFilePath launchCopy = scenario.WriteLaunchCopy();
		string executable = scenario.Fakes.Install(commandName, new FakeAgentBehavior { PackPath = launchCopy.AbsolutePath });
		IExternalAgentLauncher launcher = scenario.CreateLauncher(environment);

		Assert.Equal(AgentAvailability.Available, launcher.CheckAvailability());
		Assert.Equal(AgentLaunchEligibility.Launchable, launcher.CheckLaunchable(launchCopy));

		AgentTerminationStatus status = launcher.Launch(launchCopy);
		FakeAgentRecord record = FakeAgentRecord.ReadFrom($"{executable}{FakeAgentRecord.FILE_SUFFIX}");

		Assert.Equal(AgentTerminationStatus.ExitedWithCode(0), status);
		AssertShape(record.Arguments, documentedLeadingArguments, launchCopy.AbsolutePath);
		AssertNoForbiddenArgument(record.Arguments, forbiddenArguments);
		AssertOnlyTheCopyReachesTheDestination(record.Arguments, scenario.DestinationPath, launchCopy.AbsolutePath);
		Assert.Equal(scenario.RepositoryPath, record.WorkingDirectory);
		Assert.True(record.PackExists);
	}

	/// <summary>The stamp of a launcher test class: the documentation it was built from, with title, address and access date.</summary>
	internal static void AssertStamp(string title, string url, string accessDate)
	{
		Assert.False(string.IsNullOrWhiteSpace(title));
		Assert.True(Uri.TryCreate(url, UriKind.Absolute, out Uri? address));
		Assert.Equal(_HTTPS_SCHEME, address!.Scheme);
		Assert.True(DateOnly.TryParseExact(accessDate, _DOCUMENTATION_DATE_FORMAT, CultureInfo.InvariantCulture, DateTimeStyles.None, out _));
	}

	private static void AssertShape(string[] arguments, string[] documentedLeadingArguments, string launchCopyPath)
	{
		Assert.Equal(documentedLeadingArguments.Length + 1, arguments.Length);
		Assert.Equal(documentedLeadingArguments, arguments[..documentedLeadingArguments.Length]);
		Assert.Equal(ExternalAgentLauncherBase.BuildPromptArgument(launchCopyPath), arguments[^1]);
		Assert.All(arguments[..^1], argument => Assert.StartsWith(_FLAG_PREFIX, argument, StringComparison.Ordinal));
	}

	private static void AssertNoForbiddenArgument(string[] arguments, string[] forbiddenArguments)
	{
		foreach (string argument in arguments)
		{
			Assert.DoesNotContain(argument, forbiddenArguments, StringComparer.OrdinalIgnoreCase);
			Assert.DoesNotContain(argument, PermissionModeValues, StringComparer.OrdinalIgnoreCase);
			Assert.All(
				forbiddenArguments.Where(forbidden => forbidden.StartsWith(_FLAG_PREFIX, StringComparison.Ordinal)),
				forbidden => Assert.False(argument.StartsWith($"{forbidden}=", StringComparison.OrdinalIgnoreCase)));
		}
	}

	private static void AssertOnlyTheCopyReachesTheDestination(string[] arguments, string destinationPath, string launchCopyPath)
	{
		string launchFolder = Path.GetDirectoryName(launchCopyPath)!;

		foreach (string argument in arguments)
		{
			string withoutCopy = argument.Replace(launchCopyPath, string.Empty, StringComparison.Ordinal)
				.Replace(launchFolder, string.Empty, StringComparison.Ordinal);

			Assert.DoesNotContain(destinationPath, withoutCopy, StringComparison.Ordinal);
		}
	}
}
