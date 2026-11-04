namespace MigrateAuth.Cli.Findings;

/// <summary>
/// What one file's flow signals established: nothing, a fixed credential, or an interactive
/// user. One value per file. An interactive signal wins over a fixed one, so a file that fired both
/// reads <see cref="Interactive"/>.
///
/// A file whose flow-signal evaluation timed out reads <see cref="None"/> as well: partial signal
/// evidence is discarded rather than kept.
/// </summary>
public enum FlowEvidence
{
	None = 0,
	Fixed = 1,
	Interactive = 2
}
