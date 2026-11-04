using System.Diagnostics;
using System.Globalization;

namespace MigrateAuth.Cli.Tests.Writing;

/// <summary>
/// A directory link, created for a test that has to prove canonicalization follows one.
///
/// Two mechanisms, because Windows needs both. A directory symbolic link requires a privilege
/// an ordinary developer account does not hold, so on Windows the fallback is a JUNCTION,
/// which any account can create and which the filesystem reports as the same kind of reparse
/// point. Everywhere else the framework call is enough.
/// </summary>
internal static class DirectoryLink
{
	private const string _WINDOWS_SHELL_EXECUTABLE = "cmd.exe";
	private const string _JUNCTION_COMMAND_FORMAT = "/c mklink /J \"{0}\" \"{1}\"";
	private const int _PROCESS_TIMEOUT_IN_MILLISECONDS = 15000;
	private const string _SHELL_DID_NOT_START_MESSAGE = "The shell that creates a junction did not start.";
	private const string _JUNCTION_NOT_CREATED_MESSAGE = "A junction could not be created for this test.";

	/// <summary>
	/// Creates a link at <paramref name="linkPath"/> pointing at <paramref name="targetPath"/>,
	/// or throws. It throws rather than skipping, because a test that quietly does nothing when
	/// its precondition is unavailable is a test that reports success for an unexercised claim.
	/// </summary>
	internal static void Create(string linkPath, string targetPath)
	{
		try
		{
			Directory.CreateSymbolicLink(linkPath, targetPath);
		}
		catch (IOException) when (OperatingSystem.IsWindows())
		{
			CreateJunction(linkPath, targetPath);
		}
		catch (UnauthorizedAccessException) when (OperatingSystem.IsWindows())
		{
			CreateJunction(linkPath, targetPath);
		}
	}

	private static void CreateJunction(string linkPath, string targetPath)
	{
		ProcessStartInfo startInformation = new(_WINDOWS_SHELL_EXECUTABLE)
		{
			Arguments = string.Format(
				CultureInfo.InvariantCulture,
				_JUNCTION_COMMAND_FORMAT,
				linkPath,
				targetPath),
			UseShellExecute = false,
			RedirectStandardOutput = true,
			RedirectStandardError = true,
			CreateNoWindow = true
		};

		using Process shell = Process.Start(startInformation)
			?? throw new InvalidOperationException(_SHELL_DID_NOT_START_MESSAGE);

		shell.WaitForExit(_PROCESS_TIMEOUT_IN_MILLISECONDS);

		if (!Directory.Exists(linkPath))
		{
			throw new InvalidOperationException(_JUNCTION_NOT_CREATED_MESSAGE);
		}
	}
}
