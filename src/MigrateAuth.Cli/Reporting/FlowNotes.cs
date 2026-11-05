using MigrateAuth.Cli.Findings;

namespace MigrateAuth.Cli.Reporting;

/// <summary>
/// THE NOTE A HIGH OR MEDIUM FINDING CARRIES BESIDE ITS FLOW, ONE NAMED CONSTANT PER
/// (DECISION, FLOW) PAIR.
///
/// Every finding without an interactive signal carries the DEFAULT note - both a finding defaulted
/// for lack of any signal and one assigned Client Credentials by a fixed-credential signal. A
/// finding an interactive signal decided carries the shorter CONFIRM note instead, which asks the
/// reader to check the Authorization Code assignment.
///
/// THE NOTE IS DERIVED AT RENDER TIME AND NEVER PERSISTED. A saved session stores the decision and
/// the flow; the text comes from this class, which ships in the binary, so a resumed session renders
/// what the analysis decided without the note being a second copy that could drift from these tables.
/// It lives beside the finding in its own field and is never appended into a recommendation.
/// </summary>
public static class FlowNotes
{
	/// <summary>Defaulted to Client Credentials: no signal showed which flow applies.</summary>
	public const string DEFAULT_CLIENT_CREDENTIALS_NOTE =
		"Who supplies the credential at run time? If a person types it or signs in, the integration acts for that person: use Authorization Code. If it comes from configuration, settings, an environment variable or a secret store: use Client Credentials. No signal in this file showed which applies, so Client Credentials, the default, is recommended. Use Authorization Code instead if the integration acts on behalf of a signed-in user, or if your RelativityOne instance already has a Code-flow OAuth2 Client for this integration.";

	/// <summary>Defaulted to Authorization Code by the profile's <c>defaultFlow</c>: no signal showed which flow applies.</summary>
	public const string DEFAULT_AUTHORIZATION_CODE_NOTE =
		"Who supplies the credential at run time? If a person types it or signs in, the integration acts for that person: use Authorization Code. If it comes from configuration, settings, an environment variable or a secret store: use Client Credentials. No signal in this file showed which applies, so Authorization Code, the configured default, is recommended. Keep Authorization Code if the integration acts on behalf of a signed-in user, or if your RelativityOne instance already has a Code-flow OAuth2 Client for this integration; otherwise use Client Credentials instead.";

	/// <summary>Assigned Client Credentials by a fixed-credential signal, which carries the default note's reach.</summary>
	public const string FIXED_SIGNAL_CLIENT_CREDENTIALS_NOTE =
		"A fixed-credential signal in this file points to Client Credentials, but how often that kind of signal is right has not been measured. Use Authorization Code instead if the integration acts on behalf of a signed-in user, or if your RelativityOne instance already has a Code-flow OAuth2 Client for this integration.";

	/// <summary>Assigned Authorization Code by an interactive-user signal: the shorter note asking the reader to confirm.</summary>
	public const string INTERACTIVE_SIGNAL_AUTHORIZATION_CODE_NOTE =
		"An interactive-user signal in this file points to Authorization Code. Confirm the flow: use Client Credentials instead if the integration runs with credentials fixed for the integration and no signed-in user.";

	/// <summary>
	/// The note for one finding's decision and assigned flow. Total over both enumerations: an
	/// interactive or fixed decision always carries its own note, because its flow is determined by
	/// the decision, and a defaulted decision carries the note of the flow the default assigned.
	/// </summary>
	public static string NoteFor(FlowDecision decision, CredentialFlow flow)
	{
		string note = decision switch
		{
			FlowDecision.InteractiveSignal => INTERACTIVE_SIGNAL_AUTHORIZATION_CODE_NOTE,
			FlowDecision.FixedSignal => FIXED_SIGNAL_CLIENT_CREDENTIALS_NOTE,
			FlowDecision.Default => DefaultNoteFor(flow),
			_ => throw new ArgumentOutOfRangeException(nameof(decision), decision, "No note is declared for this flow decision.")
		};

		return note;
	}

	private static string DefaultNoteFor(CredentialFlow flow)
	{
		string note = flow switch
		{
			CredentialFlow.ClientCredentials => DEFAULT_CLIENT_CREDENTIALS_NOTE,
			CredentialFlow.AuthorizationCode => DEFAULT_AUTHORIZATION_CODE_NOTE,
			_ => throw new ArgumentOutOfRangeException(nameof(flow), flow, "No note is declared for this flow.")
		};

		return note;
	}
}
