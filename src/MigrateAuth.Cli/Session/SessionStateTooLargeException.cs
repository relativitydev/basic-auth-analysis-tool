namespace MigrateAuth.Cli.Session;

/// <summary>
/// Thrown by <see cref="SessionStateStore.Save"/> when the document it is writing passes the
/// ceiling on a state file. The message is the user-facing reason and names the ceiling. The save is
/// abandoned before the atomic rename, so the previous state file is untouched and a resume restarts
/// from the last stage that saved.
///
/// It is an <see cref="IOException"/> because, to a caller that does not know about it, it is a
/// failure to persist like any other and is handled as one.
/// </summary>
public sealed class SessionStateTooLargeException : IOException
{
	public SessionStateTooLargeException(string message)
		: base(message)
	{
	}
}
