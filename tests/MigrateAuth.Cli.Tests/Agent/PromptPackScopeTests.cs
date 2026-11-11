using MigrateAuth.Cli.Agent;
using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Reporting;

using Xunit;

namespace MigrateAuth.Cli.Tests.Agent;

/// <summary>
/// WHICH FINDINGS THE PACK LISTS AND WHERE: the report's own filter, the first section a finding matches, the report's
/// order inside a section, and counts that agree with what the lazy passes produce.
/// </summary>
public sealed class PromptPackScopeTests
{
	[Fact]
	public void TheCountsComeFromOnePassAndAgreeWithThePassesThatWriteTheEntries()
	{
		PromptPackScope scope = PromptPackFixture.Scope();

		Assert.Equal(6, scope.TotalCount);

		foreach (PromptPackSection section in Enum.GetValues<PromptPackSection>())
		{
			Assert.Equal(scope.CountOf(section), scope.EntriesOf(section).Count());
		}
	}

	[Fact]
	public void EachFindingIsInTheFirstSectionItMatches()
	{
		PromptPackScope scope = PromptPackFixture.Scope();

		Assert.Equal([4, 5, 3], scope.EntriesOf(PromptPackSection.AmbiguousCases).Select(entry => entry.SequentialIndex));
		Assert.Equal([2], scope.EntriesOf(PromptPackSection.DefaultedFlow).Select(entry => entry.SequentialIndex));
		Assert.Equal([1, 6], scope.EntriesOf(PromptPackSection.ForConfirmation).Select(entry => entry.SequentialIndex));
	}

	[Fact]
	public void TheRouteOfEachEntryIsTheCatalogsRouteAndLowIsManualReview()
	{
		PromptPackScope scope = PromptPackFixture.Scope();
		Dictionary<int, RecommendationKind> kinds = scope
			.EntriesOf(PromptPackSection.AmbiguousCases)
			.Concat(scope.EntriesOf(PromptPackSection.DefaultedFlow))
			.Concat(scope.EntriesOf(PromptPackSection.ForConfirmation))
			.ToDictionary(entry => entry.SequentialIndex, entry => entry.Guidance);

		Assert.Equal(RecommendationKind.ManualReview, kinds[4]);
		Assert.Equal(RecommendationKind.Generic, kinds[5]);
		Assert.Equal(RecommendationKind.Generic, kinds[3]);
		Assert.Equal(RecommendationKind.FromTemplate, kinds[2]);
		Assert.Equal(RecommendationKind.FromTemplate, kinds[1]);
	}

	[Fact]
	public void TheKindsEachSectionHoldsAreRecordedByTheCountingPass()
	{
		PromptPackScope scope = PromptPackFixture.Scope();

		Assert.True(scope.ContainsKind(PromptPackSection.AmbiguousCases, RecommendationKind.ManualReview));
		Assert.True(scope.ContainsKind(PromptPackSection.AmbiguousCases, RecommendationKind.Generic));
		Assert.False(scope.ContainsKind(PromptPackSection.AmbiguousCases, RecommendationKind.FromTemplate));
		Assert.True(scope.ContainsKind(PromptPackSection.DefaultedFlow, RecommendationKind.FromTemplate));
		Assert.False(scope.ContainsKind(PromptPackSection.DefaultedFlow, RecommendationKind.Generic));
	}

	[Theory]
	[InlineData(ConfidenceLevel.Low, 6)]
	[InlineData(ConfidenceLevel.Medium, 5)]
	[InlineData(ConfidenceLevel.High, 2)]
	public void OnlyTheFindingsTheReportListsAtThatMinimumAreIncluded(ConfidenceLevel minimum, int expected)
	{
		PromptPackScope scope = PromptPackFixture.Scope(minimum);

		Assert.Equal(expected, scope.TotalCount);
		Assert.Equal(minimum, scope.MinimumLevel);
		Assert.Equal(
			PromptPackFixture.Findings().Where(finding => ThresholdFilter.IsReported(finding, minimum)).Select(finding => finding.SequentialIndex).Order(),
			Enum.GetValues<PromptPackSection>().SelectMany(scope.EntriesOf).Select(entry => entry.SequentialIndex).Order());
	}

	[Fact]
	public void APassCanBeEnumeratedMoreThanOnceWithTheSameResult()
	{
		PromptPackScope scope = PromptPackFixture.Scope();

		Assert.Equal(
			scope.EntriesOf(PromptPackSection.AmbiguousCases).Select(entry => entry.Identifier),
			scope.EntriesOf(PromptPackSection.AmbiguousCases).Select(entry => entry.Identifier));
	}

	[Fact]
	public void AZeroFindingScopeHasZeroEverywhere()
	{
		PromptPackScope scope = new([], ConfidenceLevel.Low, PromptPackFixture.Catalog);

		Assert.Equal(0, scope.TotalCount);
		Assert.All(Enum.GetValues<PromptPackSection>(), section => Assert.Empty(scope.EntriesOf(section)));
	}
}
