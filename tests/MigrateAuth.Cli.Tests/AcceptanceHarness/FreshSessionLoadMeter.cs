using System.Diagnostics;
using System.Text;

using MigrateAuth.Cli.Session;

namespace MigrateAuth.Cli.Tests.AcceptanceHarness;

/// <summary>What a fresh session reported once it had read a saved state, and how much memory it took to do so.</summary>
/// <param name="Resumed">Whether the session started from the saved state rather than a fresh one.</param>
/// <param name="PeakWorkingSetInBytes">The child process's own peak working set, read once the state was loaded.</param>
/// <param name="TimeToSessionReady">From starting the process to the resumed-session announcement, runtime start-up included.</param>
internal readonly record struct FreshSessionLoad(bool Resumed, long PeakWorkingSetInBytes, TimeSpan TimeToSessionReady);

/// <summary>
/// THE LOAD MEASURED WHERE IT ACTUALLY HAPPENS: AT THE START OF A SESSION, IN A PROCESS THAT HAS DONE
/// NOTHING ELSE.
///
/// A saved state is read once per session, at startup, by the process the user just launched. Measured
/// inside a test host, the same read is measured beside everything the host and the earlier tests left
/// resident, and beside a state the test itself built. So the session is started as a child process
/// against the saved state, and its peak working set is read from the operating system once it has
/// announced the resumed session - the peak is a high-water mark, so reading it after the load has
/// finished is reading the load's peak - and only then is it told to exit.
/// </summary>
internal static class FreshSessionLoadMeter
{
	private const int _READ_TIMEOUT_IN_SECONDS = 180;
	private const int _EXIT_TIMEOUT_IN_SECONDS = 30;
	private const string _EXIT_COMMAND = "exit";
	private const string _COMPLETED_STAGES_LINE_PREFIX = "Completed stages:";
	private const string _FRESH_SESSION_LINE_PREFIX = "Starting a fresh session";

	internal static FreshSessionLoad Measure(string repositoryFolderPath, string destinationFolderPath)
	{
		ProcessStartInfo startInfo = BuildStartInfo(repositoryFolderPath, destinationFolderPath);
		Stopwatch stopwatch = Stopwatch.StartNew();
		FreshSessionLoad measured;

		using Process session = Process.Start(startInfo)
			?? throw new InvalidOperationException("The session process could not be started.");

		try
		{
			bool resumed = ReadUntilTheSessionAnnouncesItsStart(session);
			TimeSpan timeToReady = stopwatch.Elapsed;

			session.Refresh();

			measured = new FreshSessionLoad(resumed, session.PeakWorkingSet64, timeToReady);

			EndTheSession(session);
		}
		finally
		{
			if (!session.HasExited)
			{
				session.Kill(entireProcessTree: true);
			}
		}

		return measured;
	}

	private static ProcessStartInfo BuildStartInfo(string repositoryFolderPath, string destinationFolderPath)
	{
		ProcessStartInfo startInfo = new()
		{
			FileName = CliProcessHarness.HostPath,
			WorkingDirectory = AppContext.BaseDirectory,
			RedirectStandardInput = true,
			RedirectStandardOutput = true,
			RedirectStandardError = true,
			UseShellExecute = false,
			CreateNoWindow = true,
			StandardInputEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false)
		};

		startInfo.ArgumentList.Add(CliProcessHarness.EntryAssemblyPath);
		startInfo.ArgumentList.Add(CommandLineParser.REPOSITORY_FOLDER_PATH_OPTION);
		startInfo.ArgumentList.Add(repositoryFolderPath);
		startInfo.ArgumentList.Add(CommandLineParser.DOCUMENTATION_DESTINATION_FOLDER_PATH_OPTION);
		startInfo.ArgumentList.Add(destinationFolderPath);

		return startInfo;
	}

	/// <summary>
	/// True when the session says it resumed, false when it says it started fresh. The stage list is the
	/// last line of the announcement, so by the time it is read the state has been loaded and accepted.
	/// </summary>
	private static bool ReadUntilTheSessionAnnouncesItsStart(Process session)
	{
		bool resumed = false;
		bool announced = false;
		DateTime deadline = DateTime.UtcNow.AddSeconds(_READ_TIMEOUT_IN_SECONDS);

		while (!announced)
		{
			Task<string?> nextLine = session.StandardOutput.ReadLineAsync();

			if (!nextLine.Wait(deadline - DateTime.UtcNow))
			{
				throw new TimeoutException("The session did not announce its start in time.");
			}

			string? line = nextLine.Result;

			if (line is null)
			{
				announced = true;
			}
			else if (line.StartsWith(_COMPLETED_STAGES_LINE_PREFIX, StringComparison.Ordinal))
			{
				resumed = true;
				announced = true;
			}
			else if (line.StartsWith(_FRESH_SESSION_LINE_PREFIX, StringComparison.Ordinal))
			{
				announced = true;
			}
		}

		return resumed;
	}

	private static void EndTheSession(Process session)
	{
		session.StandardInput.WriteLine(_EXIT_COMMAND);
		session.StandardInput.Close();
		session.WaitForExit(TimeSpan.FromSeconds(_EXIT_TIMEOUT_IN_SECONDS));
	}
}
