namespace MigrateAuth.FakeAgent;

/// <summary>How a fake agent ends.</summary>
public enum FakeAgentEnding
{
	/// <summary>Exits with the chosen exit code.</summary>
	Exit,

	/// <summary>Ends by the chosen signal (Linux only).</summary>
	Signal,

	/// <summary>Ends the way a process interrupted from the keyboard ends on this platform.</summary>
	Interrupt,

	/// <summary>Stays alive, and ends itself with the chosen exit code when its maximum lifetime passes.</summary>
	Hang,
}
