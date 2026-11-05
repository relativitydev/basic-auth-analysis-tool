using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Configuration;
using MigrateAuth.Cli.Pipeline;
using MigrateAuth.Cli.Reporting;
using MigrateAuth.Cli.Session;
using MigrateAuth.Cli.Writing;

using MigrateAuth.Cli.Tests.Reading;
using MigrateAuth.Cli.Tests.Session;

using Xunit;

namespace MigrateAuth.Cli.Tests.Pipeline;

/// <summary>
/// The run date <see cref="ReportContent.RunDate"/> carries, read once from
/// <see cref="ReportStageContext.TimeProvider"/> and written into both artifacts. A fake clock is
/// what this test needs rather than <see cref="Progress.SteppingTimeProvider"/> - that fake never
/// overrides <c>GetUtcNow</c> or <c>LocalTimeZone</c>, so it answers the real wall clock for exactly
/// the calls this test is about, which is the one thing a date assertion cannot tolerate.
/// </summary>
public sealed class ReportStageRunDateTests
{
	private static readonly DateTimeOffset _fixedNow = new(2026, 9, 23, 10, 0, 0, TimeSpan.Zero);

	[Fact]
	public void TheMarkdownReportAndTheDataArtifactCarryTheSameRunDateFromTheClock()
	{
		using RepositoryFixture fixture = RepositoryFixture.CreateEmpty();
		using TemporaryFolder destination = TemporaryFolder.Create();

		RepositoryFolderPath repository = fixture.Repository;
		LanguageCatalog catalog = PipelineLanguageAnalyzer.BuildCatalog();
		EffectiveConfiguration configuration = PipelineHarness.BuildConfiguration(ConfidenceLevels.WIDEST);
		DocumentationDestinationFolderPath destinationFolder =
			DocumentationDestinationFolderPath.FromSuppliedText(destination.Path);

		ReportStage report = new(new ReportStageContext
		{
			Catalog = catalog,
			Configuration = configuration,
			RepositoryLabel = repository.Label,
			ArtifactWriter = new AtomicArtifactWriter(repository, destinationFolder),
			GenericRecommendations = new GenericRecommendationProvider(),
			Output = new StringWriter(),
			TimeProvider = new FixedTimeProvider(_fixedNow)
		});

		SessionState state = SessionState.Fresh(catalog.Version, repository.Label);

		Assert.Equal(StageRunOutcome.Completed, report.Run(state, CancellationToken.None));

		string markdown = File.ReadAllText(Directory.EnumerateFiles(destination.Path, "*.md").Single());
		string json = File.ReadAllText(Directory.EnumerateFiles(destination.Path, "*.json").Single());

		Assert.Contains("- Run date (local): 2026-09-23", markdown, StringComparison.Ordinal);
		Assert.Contains("\"runDate\":\"2026-09-23\"", json, StringComparison.Ordinal);
	}

	/// <summary>
	/// A clock fixed at both halves <see cref="TimeProvider.GetLocalNow"/> reads: the instant, and
	/// the zone it is converted through. Overriding only <c>GetUtcNow</c> would leave the conversion
	/// at the machine's own local zone, which is exactly the flakiness a date assertion cannot carry.
	/// </summary>
	private sealed class FixedTimeProvider : TimeProvider
	{
		private readonly DateTimeOffset _now;

		internal FixedTimeProvider(DateTimeOffset now)
		{
			_now = now;
		}

		public override DateTimeOffset GetUtcNow() => _now;

		public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
	}
}
