using MigrateAuth.Cli.Agent;
using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Emission;
using MigrateAuth.Cli.Findings;
using MigrateAuth.Cli.Reporting;

using Xunit;

namespace MigrateAuth.Cli.Tests.Agent;

/// <summary>
/// THE FIELDS AN ENTRY CARRIES: the identifier the report shows, the flow, and the default-flow mark - present exactly
/// when the finding's flow was defaulted - with the invariants that keep a Low entry free of both.
/// </summary>
public sealed class PromptPackEntryTests
{
	private const int _SEQUENTIAL_INDEX = 12;

	[Theory]
	[InlineData(1, "001")]
	[InlineData(12, "012")]
	[InlineData(999, "999")]
	[InlineData(1000, "1000")]
	[InlineData(50000, "50000")]
	public void TheIdentifierIsTheOneTheReportRenders(int index, string expected)
	{
		PromptPackFindingEntry entry = Build(ConfidenceLevel.High, CredentialFlow.ClientCredentials, false, index);

		Assert.Equal(expected, entry.Identifier);
		Assert.Equal(FindingIdentifiers.Of(index), entry.Identifier);
		Assert.Equal(index, entry.SequentialIndex);
	}

	[Theory]
	[InlineData(ConfidenceLevel.High, CredentialFlow.ClientCredentials, false)]
	[InlineData(ConfidenceLevel.High, CredentialFlow.AuthorizationCode, true)]
	[InlineData(ConfidenceLevel.Medium, CredentialFlow.ClientCredentials, true)]
	[InlineData(ConfidenceLevel.Medium, CredentialFlow.AuthorizationCode, false)]
	public void AHighOrMediumEntryCarriesItsFlowAndTheMarkAsGiven(ConfidenceLevel level, CredentialFlow flow, bool defaulted)
	{
		PromptPackFindingEntry entry = Build(level, flow, defaulted, _SEQUENTIAL_INDEX);

		Assert.Equal(flow, entry.Flow);
		Assert.Equal(defaulted, entry.FlowWasDefaulted);
	}

	[Theory]
	[InlineData(ConfidenceLevel.High)]
	[InlineData(ConfidenceLevel.Medium)]
	public void AHighOrMediumEntryWithoutAFlowIsRefused(ConfidenceLevel level)
	{
		Assert.Throws<ArgumentException>(() => Build(level, null, false, _SEQUENTIAL_INDEX));
	}

	[Fact]
	public void ALowEntryCarriesNoFlowAndNoMark()
	{
		PromptPackFindingEntry entry = Build(ConfidenceLevel.Low, null, false, _SEQUENTIAL_INDEX);

		Assert.Null(entry.Flow);
		Assert.False(entry.FlowWasDefaulted);
	}

	[Theory]
	[InlineData(CredentialFlow.ClientCredentials, false)]
	[InlineData(null, true)]
	[InlineData(CredentialFlow.AuthorizationCode, true)]
	public void ALowEntryWithAFlowOrAMarkIsRefused(CredentialFlow? flow, bool defaulted)
	{
		Assert.Throws<ArgumentException>(() => Build(ConfidenceLevel.Low, flow, defaulted, _SEQUENTIAL_INDEX));
	}

	[Theory]
	[InlineData(0)]
	[InlineData(-3)]
	public void AnIndexBelowOneIsRefused(int index)
	{
		Assert.Throws<ArgumentOutOfRangeException>(() => Build(ConfidenceLevel.High, CredentialFlow.ClientCredentials, false, index));
	}

	/// <summary>The mark in the pack is present exactly when the finding's decision was the default.</summary>
	[Fact]
	public void TheMarkIsPresentExactlyWhenTheFlowDecisionWasTheDefault()
	{
		PromptPackScope scope = PromptPackFixture.Scope();
		PromptPackFindingEntry[] entries =
		[
			.. scope.EntriesOf(PromptPackSection.AmbiguousCases),
			.. scope.EntriesOf(PromptPackSection.DefaultedFlow),
			.. scope.EntriesOf(PromptPackSection.ForConfirmation)
		];
		Finding[] findings = [.. PromptPackFixture.Findings()];

		Assert.Equal(findings.Length, entries.Length);
		Assert.All(entries, entry =>
		{
			Finding finding = findings.Single(candidate => candidate.SequentialIndex == entry.SequentialIndex);

			Assert.Equal(finding.FlowDecision == FlowDecision.Default, entry.FlowWasDefaulted);
			Assert.Equal(finding.Level == ConfidenceLevel.Low, entry.Flow is null);
			Assert.Equal(finding.Identifier, entry.Identifier);
		});
	}

	private static PromptPackFindingEntry Build(ConfidenceLevel level, CredentialFlow? flow, bool defaulted, int index)
	{
		Assert.True(RuleIdentifier.TryFromPatternClass("TST-001", out RuleIdentifier patternClass));

		return new PromptPackFindingEntry(
			patternClass,
			PromptPackFixture.RelativePath("src/Client.cs"),
			1,
			level,
			level == ConfidenceLevel.Low ? RecommendationKind.ManualReview : RecommendationKind.FromTemplate,
			index,
			flow,
			defaulted);
	}
}
