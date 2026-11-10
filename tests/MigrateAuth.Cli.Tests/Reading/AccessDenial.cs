using System.Runtime.Versioning;
using System.Security.AccessControl;
using System.Security.Principal;

namespace MigrateAuth.Cli.Tests.Reading;

/// <summary>
/// Takes read access away from the running user, and gives it back.
///
/// A TEST MAY INSPECT PERMISSIONS; THE PRODUCT MAY NOT. The product never reads an access control
/// list or a file mode: it has to work out whether it can read something, which attempting answers
/// directly and inspecting answers approximately. This code is doing the opposite job: it is
/// CREATING the condition, which is the one thing the platform API is exactly right for. The
/// product's side of the same fixture never calls anything in this file.
///
/// It is written for both permission models. The Windows half adds a deny entry for the running
/// user's own security identifier - a deny entry is evaluated before any allow entry, including one
/// inherited from the Administrators group, so it denies even a user who owns the file. The Unix
/// half removes every mode bit.
/// </summary>
internal static class AccessDenial
{
	private const string _DENIAL_DID_NOT_TAKE_MESSAGE =
		"The fixture could not make this path unreadable, so the test would have proven nothing.";

	/// <summary>Makes a file or directory unreadable by the running user, or throws.</summary>
	internal static void Apply(string path)
	{
		if (OperatingSystem.IsWindows())
		{
			ChangeWindowsDenial(path, denied: true);
		}
		else
		{
			File.SetUnixFileMode(path, UnixFileMode.None);
		}
	}

	/// <summary>Gives access back, so that the temporary folder can be removed afterwards.</summary>
	internal static void Revoke(string path)
	{
		if (OperatingSystem.IsWindows())
		{
			ChangeWindowsDenial(path, denied: false);
		}
		else
		{
			File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
		}
	}

	/// <summary>
	/// Confirms the denial actually took effect. A fixture whose precondition quietly failed is a
	/// test that reports success for an unexercised claim, which is the failure mode the
	/// directory-link helper in this suite was already written to avoid.
	/// </summary>
	internal static void ConfirmUnreadable(Func<bool> readAttempt)
	{
		if (readAttempt())
		{
			throw new InvalidOperationException(_DENIAL_DID_NOT_TAKE_MESSAGE);
		}
	}

	[SupportedOSPlatform("windows")]
	private static void ChangeWindowsDenial(string path, bool denied)
	{
		SecurityIdentifier runningUser = WindowsIdentity.GetCurrent().User
			?? throw new InvalidOperationException(_DENIAL_DID_NOT_TAKE_MESSAGE);

		if (Directory.Exists(path))
		{
			DirectoryInfo directory = new(path);
			DirectorySecurity security = directory.GetAccessControl();

			ApplyRule(security, BuildRule(runningUser, FileSystemRights.ListDirectory | FileSystemRights.ReadData), denied);
			directory.SetAccessControl(security);
		}
		else
		{
			FileInfo file = new(path);
			FileSecurity security = file.GetAccessControl();

			ApplyRule(security, BuildRule(runningUser, FileSystemRights.Read), denied);
			file.SetAccessControl(security);
		}
	}

	[SupportedOSPlatform("windows")]
	private static FileSystemAccessRule BuildRule(SecurityIdentifier runningUser, FileSystemRights rights)
	{
		return new FileSystemAccessRule(
			runningUser,
			rights,
			InheritanceFlags.None,
			PropagationFlags.None,
			AccessControlType.Deny);
	}

	[SupportedOSPlatform("windows")]
	private static void ApplyRule(FileSystemSecurity security, FileSystemAccessRule rule, bool denied)
	{
		if (denied)
		{
			security.AddAccessRule(rule);
		}
		else
		{
			security.RemoveAccessRule(rule);
		}
	}
}
