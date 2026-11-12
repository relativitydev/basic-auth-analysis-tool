using System.Runtime.InteropServices;

namespace MigrateAuth.Cli.Agent;

/// <summary>
/// KEEPS THE TOOL ALIVE WHILE THE AGENT HANDLES A QUIT SIGNAL. The keyboard interrupt is already survived by the
/// session's own handler; on Linux the quit signal, which the terminal also delivers to the whole foreground
/// group, would end the tool as well as the agent. For the length of one launch the signal is registered with
/// its default action cancelled, and the registration is removed when the shield is disposed. Where the
/// platform cannot register that signal, the shield does nothing and says so through
/// <see cref="IsActive"/>.
/// </summary>
public sealed class InterruptShield : IDisposable
{
	private PosixSignalRegistration? _registration;
	private int _interceptedCount;

	private InterruptShield()
	{
	}

	/// <summary>Whether the signal is registered at this moment.</summary>
	public bool IsActive => _registration is not null;

	/// <summary>How many times the signal arrived and was cancelled while the shield was active.</summary>
	public int InterceptedCount => Volatile.Read(ref _interceptedCount);

	/// <summary>Starts shielding for one launch. Dispose the result when the launch is over.</summary>
	public static InterruptShield Begin()
	{
		InterruptShield shield = new();

		if (!OperatingSystem.IsWindows())
		{
			shield.Register();
		}

		return shield;
	}

	/// <summary>Removes the registration. Safe to call more than once.</summary>
	public void Dispose()
	{
		PosixSignalRegistration? registration = Interlocked.Exchange(ref _registration, null);

		registration?.Dispose();
	}

	private void Register()
	{
		try
		{
			_registration = PosixSignalRegistration.Create(PosixSignal.SIGQUIT, Cancel);
		}
		catch (PlatformNotSupportedException)
		{
			_registration = null;
		}
	}

	private void Cancel(PosixSignalContext context)
	{
		context.Cancel = true;
		Interlocked.Add(ref _interceptedCount, 1);
	}
}
