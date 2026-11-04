namespace MigrateAuth.Cli.Findings;

/// <summary>
/// The names <see cref="CredentialFlow"/> and <see cref="FlowDecision"/> carry in a configuration
/// file and in the data artifact. They are declared here rather than taken from the enumeration
/// member names, so that renaming a member cannot silently rename a value a consumer keys on.
/// </summary>
public static class CredentialFlows
{
	/// <summary>The name of <see cref="CredentialFlow.ClientCredentials"/>.</summary>
	public const string CLIENT_CREDENTIALS_NAME = "ClientCredentials";

	/// <summary>The name of <see cref="CredentialFlow.AuthorizationCode"/>.</summary>
	public const string AUTHORIZATION_CODE_NAME = "AuthorizationCode";

	/// <summary>The name of <see cref="FlowDecision.InteractiveSignal"/>.</summary>
	public const string INTERACTIVE_SIGNAL_NAME = "InteractiveSignal";

	/// <summary>The name of <see cref="FlowDecision.FixedSignal"/>.</summary>
	public const string FIXED_SIGNAL_NAME = "FixedSignal";

	/// <summary>The name of <see cref="FlowDecision.Default"/>.</summary>
	public const string DEFAULT_DECISION_NAME = "Default";

	/// <summary>The flow the built-in configuration applies when nothing sets one.</summary>
	public const CredentialFlow BUILT_IN_DEFAULT = CredentialFlow.ClientCredentials;

	/// <summary>The name a flow carries in a configuration file and in output.</summary>
	public static string Name(CredentialFlow flow)
	{
		string name = flow switch
		{
			CredentialFlow.ClientCredentials => CLIENT_CREDENTIALS_NAME,
			CredentialFlow.AuthorizationCode => AUTHORIZATION_CODE_NAME,
			_ => throw new ArgumentOutOfRangeException(nameof(flow), flow, "No name is declared for this flow.")
		};

		return name;
	}

	/// <summary>The name a decision carries in output.</summary>
	public static string Name(FlowDecision decision)
	{
		string name = decision switch
		{
			FlowDecision.InteractiveSignal => INTERACTIVE_SIGNAL_NAME,
			FlowDecision.FixedSignal => FIXED_SIGNAL_NAME,
			FlowDecision.Default => DEFAULT_DECISION_NAME,
			_ => throw new ArgumentOutOfRangeException(nameof(decision), decision, "No name is declared for this decision.")
		};

		return name;
	}

	/// <summary>
	/// Reads a flow from a configuration file. Case-insensitive and ordinal, as everywhere else in
	/// this product. A name outside the two is not tolerated: the tool never recommends a third
	/// grant type, so an unrecognized value is a structural error rather than a version difference.
	/// </summary>
	public static bool TryParse(string? name, out CredentialFlow flow)
	{
		string trimmed = name?.Trim() ?? string.Empty;
		bool parsed = true;
		flow = BUILT_IN_DEFAULT;

		if (string.Equals(trimmed, CLIENT_CREDENTIALS_NAME, StringComparison.OrdinalIgnoreCase))
		{
			flow = CredentialFlow.ClientCredentials;
		}
		else if (string.Equals(trimmed, AUTHORIZATION_CODE_NAME, StringComparison.OrdinalIgnoreCase))
		{
			flow = CredentialFlow.AuthorizationCode;
		}
		else
		{
			parsed = false;
		}

		return parsed;
	}
}
