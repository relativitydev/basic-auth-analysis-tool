using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Configuration;
using MigrateAuth.Cli.Session;
using MigrateAuth.Cli.Tests.Reporting;

using Xunit;

namespace MigrateAuth.Cli.Tests.Session;

/// <summary>
/// What the launch prints for arguments it rejects, and for a path it cannot make sense of.
/// </summary>
public sealed class LaunchArgumentUsageTests
{
	private const string _UNKNOWN_OPTION = "--bogus";
	private const int _UNUSABLE_PATH_LENGTH = 40_000;

	/// <summary>An unknown option with no repository named still prints the usage line exactly once.</summary>
	[Fact]
	public void AnUnknownOptionPrintsTheUsageLineOnce()
	{
		using StringWriter output = new();
		CommandLineParseResult parsed = CommandLineParser.Parse([_UNKNOWN_OPTION]);

		SessionStartup.ReportRejectedArguments(parsed, output);
		bool resolved = SessionStartup.TryResolveRepositoryFolder(parsed, output, out _);

		Assert.False(resolved);
		Assert.Equal(1, CountOccurrences(output.ToString(), CommandLineParser.UsageLine));
	}

	/// <summary>With no rejected argument, the missing repository still prints the usage line once.</summary>
	[Fact]
	public void AMissingRepositoryWithNoOtherProblemPrintsTheUsageLineOnce()
	{
		using StringWriter output = new();
		CommandLineParseResult parsed = CommandLineParser.Parse([]);

		SessionStartup.ReportRejectedArguments(parsed, output);
		SessionStartup.TryResolveRepositoryFolder(parsed, output, out _);

		Assert.Equal(1, CountOccurrences(output.ToString(), CommandLineParser.UsageLine));
	}

	/// <summary>A repository path too long for the platform is said to be unusable, and does not escape as a failure.</summary>
	[Fact]
	public void ARepositoryPathTooLongForThePlatformIsReportedAsUnusable()
	{
		using StringWriter output = new();
		CommandLineParseResult parsed = CommandLineParser.Parse(
			[$"{CommandLineParser.REPOSITORY_FOLDER_PATH_OPTION}={BuildUnusablyLongPath()}"]);

		bool resolved = SessionStartup.TryResolveRepositoryFolder(parsed, output, out _);

		Assert.False(resolved);
		Assert.Contains(SessionStartup.REPOSITORY_FOLDER_PATH_MALFORMED_MESSAGE, output.ToString(), StringComparison.Ordinal);
	}

	/// <summary>
	/// A repository path that does not exist is named in full, as it was supplied, and not by its last segment,
	/// so the user can see which path was refused and whether it was mistyped higher up.
	/// </summary>
	[Fact]
	public void ANonexistentRepositoryPathIsNamedInFullAsSupplied()
	{
		using TemporaryFolder parent = TemporaryFolder.Create();
		string suppliedPath = Path.Combine(parent.Path, "levelOne", "nothing");
		using StringWriter output = new();
		CommandLineParseResult parsed = CommandLineParser.Parse(
			[$"{CommandLineParser.REPOSITORY_FOLDER_PATH_OPTION}={suppliedPath}"]);

		bool resolved = SessionStartup.TryResolveRepositoryFolder(parsed, output, out _);

		Assert.False(resolved);
		Assert.Equal(
			$"The repository folder does not exist: {suppliedPath}.{Environment.NewLine}",
			output.ToString());
	}

	/// <summary>A repository path that names a file is named in full, as supplied.</summary>
	[Fact]
	public void ARepositoryPathThatIsAFileIsNamedInFullAsSupplied()
	{
		using TemporaryFolder parent = TemporaryFolder.Create();
		string suppliedPath = Path.Combine(parent.Path, "plain.txt");
		File.WriteAllText(suppliedPath, "content");
		using StringWriter output = new();
		CommandLineParseResult parsed = CommandLineParser.Parse(
			[$"{CommandLineParser.REPOSITORY_FOLDER_PATH_OPTION}={suppliedPath}"]);

		bool resolved = SessionStartup.TryResolveRepositoryFolder(parsed, output, out _);

		Assert.False(resolved);
		Assert.Equal($"The repository path is not a folder: {suppliedPath}.{Environment.NewLine}", output.ToString());
	}

	/// <summary>The same for the output folder, which can arrive from the user profile file as well as the command line.</summary>
	[Fact]
	public void ADestinationPathTooLongForThePlatformIsReportedAsUnusable()
	{
		using StringWriter output = new();
		EffectiveConfiguration configuration = ReportHarness.ConfigurationWith(ConfidenceLevel.Low) with
		{
			OutputPathText = BuildUnusablyLongPath()
		};

		bool canonicalized = SessionStartup.TryCanonicalizeDestinationFolder(configuration, output, out _);

		Assert.False(canonicalized);
		Assert.Contains(SessionStartup.DESTINATION_FOLDER_PATH_MALFORMED_MESSAGE, output.ToString(), StringComparison.Ordinal);
	}

	private static string BuildUnusablyLongPath()
	{
		return Path.Combine(Path.GetTempPath(), new string('a', _UNUSABLE_PATH_LENGTH));
	}

	private static int CountOccurrences(string text, string value)
	{
		int count = 0;
		int index = text.IndexOf(value, StringComparison.Ordinal);

		while (index >= 0)
		{
			count++;
			index = text.IndexOf(value, index + value.Length, StringComparison.Ordinal);
		}

		return count;
	}
}
