using MigrateAuth.Cli.Emission;

using Xunit;

namespace MigrateAuth.Cli.Tests.Emission;

/// <summary>
/// The identifier's shape is enforced on construction, so no line of source code, credential or
/// matched text can be represented by one.
/// </summary>
public sealed class RuleIdentifierTests
{
	[Theory]
	[InlineData("CS-001")]
	[InlineData("JS-004")]
	[InlineData("ABCDEF-999")]
	public void ACatalogIdentifierIsAccepted(string identifier)
	{
		bool accepted = RuleIdentifier.TryFromPatternClass(identifier, out RuleIdentifier rule);

		Assert.True(accepted);
		Assert.True(rule.IsDeclared);
		Assert.Equal(identifier, rule.Value);
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData(" ")]
	[InlineData("C-001")]
	[InlineData("ABCDEFG-001")]
	[InlineData("cs-001")]
	[InlineData("CS-01")]
	[InlineData("CS-0001")]
	[InlineData("CS001")]
	[InlineData("CS-001 ")]
	[InlineData(" CS-001")]
	[InlineData("CS-00A")]
	[InlineData("password = hunter2")]
	public void ATextThatIsNotACatalogIdentifierIsRefused(string? text)
	{
		bool accepted = RuleIdentifier.TryFromPatternClass(text!, out RuleIdentifier rule);

		Assert.False(accepted);
		Assert.False(rule.IsDeclared);
		Assert.Equal(default, rule);
	}

	[Fact]
	public void TheDefaultInstanceCarriesNoIdentifier()
	{
		RuleIdentifier rule = default;

		Assert.False(rule.IsDeclared);
		Assert.Null(rule.Value);
	}

	[Fact]
	public void TwoIdentifiersOfTheSameTextAreEqual()
	{
		Assert.True(RuleIdentifier.TryFromPatternClass("CS-001", out RuleIdentifier first));
		Assert.True(RuleIdentifier.TryFromPatternClass("CS-001", out RuleIdentifier second));

		Assert.Equal(first, second);
	}
}
