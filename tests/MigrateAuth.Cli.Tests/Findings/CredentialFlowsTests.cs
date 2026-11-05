using MigrateAuth.Cli.Findings;

using Xunit;

namespace MigrateAuth.Cli.Tests.Findings;

/// <summary>
/// The names a flow and a decision carry in a configuration file and in output are declared
/// constants, so these assertions pin the declared values and the parse rules rather than the
/// enumeration member names.
/// </summary>
public sealed class CredentialFlowsTests
{
	[Theory]
	[InlineData(CredentialFlow.ClientCredentials, "ClientCredentials")]
	[InlineData(CredentialFlow.AuthorizationCode, "AuthorizationCode")]
	public void AFlowCarriesItsDeclaredName(CredentialFlow flow, string expectedName)
	{
		Assert.Equal(expectedName, CredentialFlows.Name(flow));
	}

	[Theory]
	[InlineData(FlowDecision.InteractiveSignal, "InteractiveSignal")]
	[InlineData(FlowDecision.FixedSignal, "FixedSignal")]
	[InlineData(FlowDecision.Default, "Default")]
	public void ADecisionCarriesItsDeclaredName(FlowDecision decision, string expectedName)
	{
		Assert.Equal(expectedName, CredentialFlows.Name(decision));
	}

	[Fact]
	public void AFlowOutsideTheDeclaredSetHasNoName()
	{
		Assert.Throws<ArgumentOutOfRangeException>(() => CredentialFlows.Name((CredentialFlow)int.MaxValue));
	}

	[Fact]
	public void ADecisionOutsideTheDeclaredSetHasNoName()
	{
		Assert.Throws<ArgumentOutOfRangeException>(() => CredentialFlows.Name((FlowDecision)int.MaxValue));
	}

	[Theory]
	[InlineData("ClientCredentials", CredentialFlow.ClientCredentials)]
	[InlineData("clientcredentials", CredentialFlow.ClientCredentials)]
	[InlineData("  AuthorizationCode  ", CredentialFlow.AuthorizationCode)]
	[InlineData("AUTHORIZATIONCODE", CredentialFlow.AuthorizationCode)]
	public void ADeclaredNameParsesIgnoringCaseAndSurroundingWhitespace(string text, CredentialFlow expectedFlow)
	{
		bool parsed = CredentialFlows.TryParse(text, out CredentialFlow flow);

		Assert.True(parsed);
		Assert.Equal(expectedFlow, flow);
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData("   ")]
	[InlineData("Implicit")]
	[InlineData("Client Credentials")]
	public void AnUnrecognizedNameIsRefusedAndLeavesTheBuiltInDefault(string? text)
	{
		bool parsed = CredentialFlows.TryParse(text, out CredentialFlow flow);

		Assert.False(parsed);
		Assert.Equal(CredentialFlows.BUILT_IN_DEFAULT, flow);
	}

	[Fact]
	public void EveryFlowNameParsesBackToItsFlow()
	{
		foreach (CredentialFlow declared in Enum.GetValues<CredentialFlow>())
		{
			Assert.True(CredentialFlows.TryParse(CredentialFlows.Name(declared), out CredentialFlow parsed));
			Assert.Equal(declared, parsed);
		}
	}
}
