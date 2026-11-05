namespace MigrateAuth.Cli.Findings;

/// <summary>
/// The two OAuth 2.0 flows a High or Medium finding's recommendation can target, and the only two
/// there are: the tool never recommends another grant type. <see cref="ClientCredentials"/> is the
/// zero value because it is the built-in default.
/// </summary>
public enum CredentialFlow
{
	ClientCredentials = 0,
	AuthorizationCode = 1
}

/// <summary>
/// Why a finding was assigned the flow it carries: an interactive-user signal in its file, a
/// fixed-credential signal in its file, or neither, in which case the effective default applied. A
/// file whose flow-signal evaluation timed out is decided <see cref="Default"/> as well.
/// </summary>
public enum FlowDecision
{
	InteractiveSignal = 0,
	FixedSignal = 1,
	Default = 2
}
