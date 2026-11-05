using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Findings;
using MigrateAuth.Cli.Pipeline;
using MigrateAuth.Cli.Tests.MatchBoundStress;

using Xunit;
using Xunit.Abstractions;

namespace MigrateAuth.Cli.Tests.Pipeline;

/// <summary>
/// The volume evidence of the Suggest console: the row-limit behaviour at fifty thousand findings, with the duration recorded and bounded.
/// It reuses the helpers of <see cref="SuggestConsoleTests"/>.
///
/// Timing-sensitive: it asserts on, or is bounded by, wall-clock time, so it runs alone in the <c>TimingSensitiveStressCollection</c> collection and its name carries "Timing".
/// </summary>
[Collection(TimingSensitiveStressCollection.NAME)]
public sealed class SuggestConsoleVolumeTimingTests
{
	private const int _VOLUME_FINDING_COUNT = 50_000;
	private const long _VOLUME_DURATION_CEILING_IN_MILLISECONDS = 30_000;

	private readonly ITestOutputHelper _output;

	public SuggestConsoleVolumeTimingTests(ITestOutputHelper output)
	{
		_output = output;
	}

	/// <summary>
	/// VOLUME EVIDENCE. The row-limit behaviour at fifty thousand findings,
	/// in-process: one hundred rows, one summary line, and the duration recorded in the test output.
	/// </summary>
	[Fact]
	public void FiftyThousandFindingsPrintTheLimitOfRowsAndOneSummaryLineAndTheDurationIsRecorded()
	{
		Finding[] findings = new Finding[_VOLUME_FINDING_COUNT];

		for (int index = 0; index < findings.Length; index++)
		{
			findings[index] = SuggestConsoleTests.FindingWith(index + 1, CredentialFlow.ClientCredentials, index % 2 == 0 ? FlowDecision.Default : FlowDecision.FixedSignal);
		}

		ImmutableArray<Finding> findingSet = [.. findings];
		Stopwatch stopwatch = Stopwatch.StartNew();
		string console = SuggestConsoleTests.RunSuggest(findingSet, ConfidenceLevel.Low);

		stopwatch.Stop();
		_output.WriteLine(string.Format(
			CultureInfo.InvariantCulture,
			"Suggest over {0:N0} findings: {1:N0} ms, {2:N0} console rows, {3:N0} console characters.",
			_VOLUME_FINDING_COUNT,
			stopwatch.ElapsedMilliseconds,
			SuggestConsoleTests.RowLines(console).Length,
			console.Length));

		Assert.Equal(SuggestStage.SUGGEST_CONSOLE_FINDING_ROW_LIMIT, SuggestConsoleTests.RowLines(console).Length);
		Assert.Equal(1, SuggestConsoleTests.CountOccurrences(console, "further findings have no row"));
		Assert.Contains("49,900 further findings", console, StringComparison.Ordinal);
		Assert.True(
			stopwatch.ElapsedMilliseconds < _VOLUME_DURATION_CEILING_IN_MILLISECONDS,
			$"Suggest over {_VOLUME_FINDING_COUNT:N0} findings took {stopwatch.ElapsedMilliseconds:N0} ms.");
	}
}
