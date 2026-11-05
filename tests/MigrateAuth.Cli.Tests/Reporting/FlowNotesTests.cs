using MigrateAuth.Cli.Findings;
using MigrateAuth.Cli.Reporting;

using Xunit;

namespace MigrateAuth.Cli.Tests.Reporting;

/// <summary>
/// THE FOUR FLOW NOTES: ONE NAMED CONSTANT PER (DECISION, FLOW) PAIR AND A TOTAL FUNCTION
/// OVER THEM.
///
/// A note on a finding no interactive signal decided has to name the flow that was not assigned
/// and both conditions under which the developer should switch to it; the shorter note on a finding
/// an interactive signal decided has to name Client Credentials and the condition that applies.
/// </summary>
public sealed class FlowNotesTests
{
	private const string _CLIENT_CREDENTIALS_TEXT = "Client Credentials";
	private const string _AUTHORIZATION_CODE_TEXT = "Authorization Code";
	private const string _ACTS_ON_BEHALF_OF_A_USER_CONDITION = "acts on behalf of a signed-in user";
	private const string _EXISTING_CODE_CLIENT_CONDITION = "already has a Code-flow OAuth2 Client";
	private const string _FIXED_CREDENTIALS_CONDITION = "credentials fixed for the integration and no signed-in user";
	private const string _DECIDING_QUESTION = "Who supplies the credential at run time?";
	private const string _PERSON_TO_AUTHORIZATION_CODE_MAPPING = "If a person types it or signs in, the integration acts for that person: use Authorization Code.";
	private const string _CONFIGURATION_TO_CLIENT_CREDENTIALS_MAPPING = "If it comes from configuration, settings, an environment variable or a secret store: use Client Credentials.";
	private const string _CONFIRM_TEXT = "Confirm the flow";

	/// <summary>Both conditions of the default rule, and the flow that was not assigned.</summary>
	[Fact]
	public void TheDefaultNoteForClientCredentialsNamesAuthorizationCodeAndBothConditions()
	{
		string note = FlowNotes.NoteFor(FlowDecision.Default, CredentialFlow.ClientCredentials);

		Assert.Contains(_AUTHORIZATION_CODE_TEXT, note, StringComparison.Ordinal);
		Assert.Contains(_ACTS_ON_BEHALF_OF_A_USER_CONDITION, note, StringComparison.Ordinal);
		Assert.Contains(_EXISTING_CODE_CLIENT_CONDITION, note, StringComparison.Ordinal);
	}

	/// <summary>The same note for a default of Authorization Code names Client Credentials, the flow not assigned.</summary>
	[Fact]
	public void TheDefaultNoteForAuthorizationCodeNamesClientCredentialsAndBothConditions()
	{
		string note = FlowNotes.NoteFor(FlowDecision.Default, CredentialFlow.AuthorizationCode);

		Assert.Contains(_CLIENT_CREDENTIALS_TEXT, note, StringComparison.Ordinal);
		Assert.Contains(_ACTS_ON_BEHALF_OF_A_USER_CONDITION, note, StringComparison.Ordinal);
		Assert.Contains(_EXISTING_CODE_CLIENT_CONDITION, note, StringComparison.Ordinal);
	}

	/// <summary>Each default note opens with the question that decides the flow.</summary>
	[Theory]
	[InlineData(CredentialFlow.ClientCredentials)]
	[InlineData(CredentialFlow.AuthorizationCode)]
	public void EachDefaultNoteOpensWithTheWhoSuppliesTheCredentialQuestion(CredentialFlow flow)
	{
		string note = FlowNotes.NoteFor(FlowDecision.Default, flow);

		Assert.StartsWith(_DECIDING_QUESTION, note, StringComparison.Ordinal);
	}

	/// <summary>Each default note maps a person to Authorization Code and configuration sources to Client Credentials.</summary>
	[Theory]
	[InlineData(CredentialFlow.ClientCredentials)]
	[InlineData(CredentialFlow.AuthorizationCode)]
	public void EachDefaultNoteMapsThePersonAndTheConfigurationSourcesToTheirFlows(CredentialFlow flow)
	{
		string note = FlowNotes.NoteFor(FlowDecision.Default, flow);

		Assert.Contains(_PERSON_TO_AUTHORIZATION_CODE_MAPPING, note, StringComparison.Ordinal);
		Assert.Contains(_CONFIGURATION_TO_CLIENT_CREDENTIALS_MAPPING, note, StringComparison.Ordinal);
	}

	/// <summary>A fixed-credential finding carries the default note's reach: the flow not assigned and both conditions.</summary>
	[Fact]
	public void TheFixedSignalNoteNamesAuthorizationCodeAndBothConditions()
	{
		string note = FlowNotes.NoteFor(FlowDecision.FixedSignal, CredentialFlow.ClientCredentials);

		Assert.Contains(_AUTHORIZATION_CODE_TEXT, note, StringComparison.Ordinal);
		Assert.Contains(_ACTS_ON_BEHALF_OF_A_USER_CONDITION, note, StringComparison.Ordinal);
		Assert.Contains(_EXISTING_CODE_CLIENT_CONDITION, note, StringComparison.Ordinal);
	}

	/// <summary>The "confirm the flow" note names Client Credentials and the condition it applies under.</summary>
	[Fact]
	public void TheConfirmNoteNamesClientCredentialsAndTheConditionUnderWhichItApplies()
	{
		string note = FlowNotes.NoteFor(FlowDecision.InteractiveSignal, CredentialFlow.AuthorizationCode);

		Assert.Contains(_CONFIRM_TEXT, note, StringComparison.Ordinal);
		Assert.Contains(_CLIENT_CREDENTIALS_TEXT, note, StringComparison.Ordinal);
		Assert.Contains(_FIXED_CREDENTIALS_CONDITION, note, StringComparison.Ordinal);
		Assert.NotEqual(
			FlowNotes.NoteFor(FlowDecision.Default, CredentialFlow.ClientCredentials),
			note);
	}

	[Fact]
	public void EachOfTheFourPairsResolvesToItsOwnNamedConstant()
	{
		Assert.Equal(
			FlowNotes.DEFAULT_CLIENT_CREDENTIALS_NOTE,
			FlowNotes.NoteFor(FlowDecision.Default, CredentialFlow.ClientCredentials));
		Assert.Equal(
			FlowNotes.DEFAULT_AUTHORIZATION_CODE_NOTE,
			FlowNotes.NoteFor(FlowDecision.Default, CredentialFlow.AuthorizationCode));
		Assert.Equal(
			FlowNotes.FIXED_SIGNAL_CLIENT_CREDENTIALS_NOTE,
			FlowNotes.NoteFor(FlowDecision.FixedSignal, CredentialFlow.ClientCredentials));
		Assert.Equal(
			FlowNotes.INTERACTIVE_SIGNAL_AUTHORIZATION_CODE_NOTE,
			FlowNotes.NoteFor(FlowDecision.InteractiveSignal, CredentialFlow.AuthorizationCode));

		string[] distinct =
		[
			FlowNotes.DEFAULT_CLIENT_CREDENTIALS_NOTE,
			FlowNotes.DEFAULT_AUTHORIZATION_CODE_NOTE,
			FlowNotes.FIXED_SIGNAL_CLIENT_CREDENTIALS_NOTE,
			FlowNotes.INTERACTIVE_SIGNAL_AUTHORIZATION_CODE_NOTE
		];

		Assert.Equal(distinct.Length, distinct.Distinct(StringComparer.Ordinal).Count());
	}

	[Fact]
	public void TheFunctionIsTotalOverEveryDecisionAndFlow()
	{
		foreach (FlowDecision decision in Enum.GetValues<FlowDecision>())
		{
			foreach (CredentialFlow flow in Enum.GetValues<CredentialFlow>())
			{
				Assert.False(string.IsNullOrWhiteSpace(FlowNotes.NoteFor(decision, flow)));
			}
		}
	}

	[Fact]
	public void AnUndefinedDecisionOrFlowIsRefusedRatherThanGivenANote()
	{
		Assert.Throws<ArgumentOutOfRangeException>(
			() => FlowNotes.NoteFor((FlowDecision)int.MaxValue, CredentialFlow.ClientCredentials));
		Assert.Throws<ArgumentOutOfRangeException>(
			() => FlowNotes.NoteFor(FlowDecision.Default, (CredentialFlow)int.MaxValue));
	}
}
