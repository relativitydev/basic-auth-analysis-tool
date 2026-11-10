using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Findings;
using MigrateAuth.Cli.Reading;
using MigrateAuth.Cli.Session;
using MigrateAuth.Cli.Tests.Pipeline;

using Xunit;
using Xunit.Abstractions;

namespace MigrateAuth.Cli.Tests.AcceptanceHarness;

/// <summary>
/// THE VOLUME PROOF, THROUGH THE REAL EMBEDDED `PowerShell` ANALYZER AND THE REAL ANALYSIS PATH: ALL FOURTEEN DETECTION RULES AND ALL NINE FLOW
/// SIGNALS REGISTERED, NEVER A HAND-BUILT CONSTRUCT.
///
/// FIVE THOUSAND CANDIDATE FILES, GENERATED AT RUN TIME INTO A TEMPORARY DIRECTORY OUTSIDE THE REPOSITORY AND DELETED AFTERWARDS. Every generated file carries a
/// preselection anchor, so every one is a real candidate - never a file the walk enumerates but Scan declines to open.
///
/// THREE FILE SHAPES, as the first language's proof generates them. Most files carry exactly one of the fourteen classes' own canonical construct, cycled so
/// every class is exercised at volume, and every class's own fixture is one finding: the legacy client's subsumes its credential, the explicit scheme
/// parameter's subsumes the credential parameter, and the token class and the pair class cannot overlap. A small set carry an anchor in prose only - a real
/// candidate that must yield no finding. And a minority open a here-string that never closes, so their scan is abandoned and the lexical anomaly is counted:
/// the number the proof records rather than works around.
///
/// FLOW-SIGNAL EVALUATION AT THE SAME VOLUME. Every positive file also carries one of three signal shapes, cycled by its index: an interactive-user signal, a
/// fixed-credential signal, or none. Every candidate file holding a surviving High or Medium construct triggers the nine signals, so the count of files
/// evaluated must equal the number of files with such a finding, no evaluation may trip its bound, and the evidence each retained finding carries must be the
/// one its file's shape generated.
///
/// ONLY ROBUST PROPERTIES ARE ASSERTED - zero match-bound trips, every file analysed, the exact lexical-anomaly count, exact per-class finding counts and
/// the flow-signal counts above. Never a timing threshold: the elapsed time and the peak working set are recorded to the test output, not asserted.
/// </summary>
[Collection(SessionStateVolumeProofTests.COLLECTION_NAME)]
public sealed class PowerShellAnalyzerVolumeProofTests : IClassFixture<VolumeProofMemoryRelease>
{
	private const int _NOISE_FILE_COUNT = 50;
	private const int _UNTERMINATED_HERE_STRING_FILE_COUNT = 50;
	private const int _POSITIVE_FILE_COUNT = 4_900;
	private const int _TOTAL_FILE_COUNT = _POSITIVE_FILE_COUNT + _NOISE_FILE_COUNT + _UNTERMINATED_HERE_STRING_FILE_COUNT;

	private const string _PS_001 = "PS-001";

	private const int _SIGNAL_SHAPE_COUNT = 3;
	private const int _INTERACTIVE_SHAPE_POSITION = 0;
	private const int _FIXED_SHAPE_POSITION = 1;
	private const string _INTERACTIVE_SIGNAL_LINE = "$typed = Read-Host -Prompt 'Enter your password' -AsSecureString";
	private const string _FIXED_SIGNAL_LINE = "$stored = $env:SERVICE_PWD";
	private const string _POSITIVE_FILE_FOLDER_PREFIX = "positive-";
	private const string _PATH_SEPARATOR = "/";
	private const string _POSITIVE_FILE_NAME_FORMAT = "positive-{0:D5}/source/module.ps1";
	private const string _NOISE_FILE_NAME_FORMAT = "noise-{0:D5}/source/module.ps1";
	private const string _UNTERMINATED_FILE_NAME_FORMAT = "unterminated-{0:D5}/source/module.ps1";

	/// <summary>One canonical construct per class, in catalog order, each proved isolated to exactly its class by the analyzer's own tests.</summary>
	private static readonly ImmutableArray<(string PatternClass, string Source)> _CANONICAL_CONSTRUCTS =
	[
		("PS-001", "$credentials = New-Object Vendor.Sdk.UsernamePasswordCredentials($loginName, $loginSecret)"),
		("PS-002", "$proxy = New-Object Vendor.Sdk.RSAPIClient ((New-Object System.Uri -ArgumentList $serviceAddress), (New-Object Vendor.Sdk.UsernamePasswordCredentials($loginName, $loginSecret)))"),
		("PS-003", "Invoke-RestMethod -Uri $uri -Credential $credential -Authentication Basic"),
		("PS-004", "Invoke-RestMethod -Uri $uri -Headers @{ Authorization = \"Basic $encoded\" }"),
		("PS-005", "$headerTable.Add('Authorization', 'Basic ' + $encoded)"),
		("PS-006", "$request.Headers.Add([System.Net.HttpRequestHeader]::Authorization, \"Basic $encoded\")"),
		("PS-007", "$httpClient.DefaultRequestHeaders.Authorization = [System.Net.Http.Headers.AuthenticationHeaderValue]::new('Basic', $encoded)"),
		("PS-008", "$cache = New-Object System.Net.CredentialCache\n$cache.Add([Uri]$uri, 'Basic', (New-Object System.Net.NetworkCredential($loginName, $loginSecret)))"),
		("PS-009", "$handler = New-Object System.Net.Http.HttpClientHandler\n$handler.Credentials = New-Object System.Net.NetworkCredential($loginName, $loginSecret)"),
		("PS-010", "$encoded = [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes(\"$($loginName):$($loginSecret)\"))"),
		("PS-011", "$pair = \":\" + $accessToken\n$encoded = [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes($pair))"),
		("PS-012", "Invoke-RestMethod -Uri $uri -Credential $credential"),
		("PS-013", "Invoke-Command -ComputerName $server -Credential $credential -ScriptBlock { hostname }"),
		("PS-014", "function New-BasicAuthorizationHeader {\n\tparam($loginName, $loginSecret)\n}")
	];

	private readonly ITestOutputHelper _output;

	public PowerShellAnalyzerVolumeProofTests(ITestOutputHelper output)
	{
		_output = output;
	}

	[Fact]
	public void FiveThousandCandidateFilesAcrossAllFourteenClassesAnalyseCleanlyThroughTheRealPath()
	{
		using PipelineHarness harness = PipelineHarness.CreateOverTheEmbeddedCatalog();

		Dictionary<string, int> expectedCounts = PlantTree(harness);

		harness.RunAndCommit(CommandId.Scan);

		PeakWorkingSetSampler.ReleaseUnusedMemory();

		PeakWorkingSetSampler sampler = PeakWorkingSetSampler.Start();
		Stopwatch stopwatch = Stopwatch.StartNew();
		StageRunOutcome outcome = harness.Run(CommandId.Analyze);
		stopwatch.Stop();
		long peakWorkingSetInBytes = sampler.StopAndReadPeakInBytes();

		ReportFigures(harness, stopwatch.Elapsed, peakWorkingSetInBytes, expectedCounts);

		Assert.Equal(StageRunOutcome.Completed, outcome);
		Assert.Equal(_TOTAL_FILE_COUNT, harness.State.ScanOutcome.FileCount);
		Assert.Equal(_TOTAL_FILE_COUNT, harness.State.ScanOutcome.CandidateFileCount);
		AssertNothingWasSkippedForAnyOtherReason(harness);
		Assert.Equal(0, harness.SkippedItems.TallyOf(SkippedItemCategory.MatchBoundExceeded).Count);
		Assert.Equal(_UNTERMINATED_HERE_STRING_FILE_COUNT, harness.SkippedItems.TallyOf(SkippedItemCategory.LexicalAnomaly).Count);
		AssertPerClassFindingCounts(harness, expectedCounts);
		AssertEveryHighOrMediumFileWasEvaluatedForFlowSignals(harness);
		Assert.False(harness.State.FindingsCapOutcome.IsTruncated);
	}

	private static void AssertNothingWasSkippedForAnyOtherReason(PipelineHarness harness)
	{
		SkippedItemCategory[] otherReasons =
		[
			SkippedItemCategory.UnreadableFile,
			SkippedItemCategory.UnlistableDirectory,
			SkippedItemCategory.LinkNotFollowed,
			SkippedItemCategory.Undecodable,
			SkippedItemCategory.OversizeFile,
			SkippedItemCategory.OverLongLine,
			SkippedItemCategory.PathTooLong,
			SkippedItemCategory.MissingCandidate
		];

		Assert.All(otherReasons, category => Assert.Equal(0, harness.SkippedItems.TallyOf(category).Count));
	}

	private static void AssertEveryHighOrMediumFileWasEvaluatedForFlowSignals(PipelineHarness harness)
	{
		ImmutableArray<Finding> highOrMedium =
		[
			.. harness.State.Findings.Where(finding => finding.Level is ConfidenceLevel.High or ConfidenceLevel.Medium)
		];
		int fileCount = highOrMedium.Select(finding => finding.Path).Distinct().Count();

		Assert.Equal(fileCount, harness.State.FlowEvaluationDiagnostics.FilesEvaluated);
		Assert.Equal(0, harness.State.FlowEvaluationDiagnostics.SignalTripCount);
		Assert.Contains(highOrMedium, finding => finding.FlowEvidence == FlowEvidence.Interactive);
		Assert.Contains(highOrMedium, finding => finding.FlowEvidence == FlowEvidence.Fixed);
		Assert.Contains(highOrMedium, finding => finding.FlowEvidence == FlowEvidence.None);

		foreach (Finding finding in highOrMedium.Where(candidate => candidate.Path.Value.StartsWith(_POSITIVE_FILE_FOLDER_PREFIX, StringComparison.Ordinal)))
		{
			Assert.True(
				finding.FlowEvidence == ExpectedEvidenceOf(finding),
				$"{finding.Path.Value} ({finding.PatternClass.Value}): expected {ExpectedEvidenceOf(finding)}, found {finding.FlowEvidence}.");
		}
	}

	private static FlowEvidence ExpectedEvidenceOf(Finding finding)
	{
		string path = finding.Path.Value;
		string folder = path[..path.IndexOf(_PATH_SEPARATOR, StringComparison.Ordinal)];
		int fileIndex = int.Parse(folder[_POSITIVE_FILE_FOLDER_PREFIX.Length..], CultureInfo.InvariantCulture);

		return (fileIndex % _SIGNAL_SHAPE_COUNT) switch
		{
			_INTERACTIVE_SHAPE_POSITION => FlowEvidence.Interactive,
			_FIXED_SHAPE_POSITION => FlowEvidence.Fixed,
			_ => FlowEvidence.None
		};
	}

	private static string SignalShapeOf(int fileIndex)
	{
		return (fileIndex % _SIGNAL_SHAPE_COUNT) switch
		{
			_INTERACTIVE_SHAPE_POSITION => _INTERACTIVE_SIGNAL_LINE,
			_FIXED_SHAPE_POSITION => _FIXED_SIGNAL_LINE,
			_ => string.Empty
		};
	}

	private static Dictionary<string, int> PlantTree(PipelineHarness harness)
	{
		Dictionary<string, int> expectedCounts = _CANONICAL_CONSTRUCTS.ToDictionary(entry => entry.PatternClass, _ => 0, StringComparer.Ordinal);

		for (int fileIndex = 0; fileIndex < _POSITIVE_FILE_COUNT; fileIndex++)
		{
			(string patternClass, string source) = _CANONICAL_CONSTRUCTS[fileIndex % _CANONICAL_CONSTRUCTS.Length];

			harness.WriteSourceFile(
				string.Format(CultureInfo.InvariantCulture, _POSITIVE_FILE_NAME_FORMAT, fileIndex),
				source + "\n" + SignalShapeOf(fileIndex));
			expectedCounts[patternClass]++;
		}

		for (int fileIndex = 0; fileIndex < _NOISE_FILE_COUNT; fileIndex++)
		{
			harness.WriteSourceFile(
				string.Format(CultureInfo.InvariantCulture, _NOISE_FILE_NAME_FORMAT, fileIndex),
				$"# file {fileIndex} mentions Credential and Authorization only in prose, never in code");
		}

		for (int fileIndex = 0; fileIndex < _UNTERMINATED_HERE_STRING_FILE_COUNT; fileIndex++)
		{
			harness.WriteSourceFile(
				string.Format(CultureInfo.InvariantCulture, _UNTERMINATED_FILE_NAME_FORMAT, fileIndex),
				"$text = @\"\nnever closes\n$credentials = New-Object Vendor.Sdk.UsernamePasswordCredentials($loginName, $loginSecret)\n");
		}

		return expectedCounts;
	}

	private static void AssertPerClassFindingCounts(PipelineHarness harness, Dictionary<string, int> expectedCounts)
	{
		ILookup<string, Finding> findingsByPatternClass = harness.State.Findings.ToLookup(
			finding => finding.PatternClass.Value,
			StringComparer.Ordinal);

		foreach ((string patternClass, string _) in _CANONICAL_CONSTRUCTS)
		{
			int actualCount = findingsByPatternClass[patternClass].Count();
			int expectedCount = expectedCounts[patternClass];

			// The credential class also arrives, at Low, from the files whose here-string never closes.
			bool isExact = !string.Equals(patternClass, _PS_001, StringComparison.Ordinal);

			Assert.True(
				isExact ? actualCount == expectedCount : actualCount >= expectedCount,
				$"{patternClass}: expected {(isExact ? "exactly" : "at least")} {expectedCount}, found {actualCount}.");
		}
	}

	private void ReportFigures(PipelineHarness harness, TimeSpan elapsed, long peakWorkingSetInBytes, Dictionary<string, int> expectedCounts)
	{
		ILookup<string, Finding> findingsByPatternClass = harness.State.Findings.ToLookup(
			finding => finding.PatternClass.Value,
			StringComparer.Ordinal);

		_output.WriteLine("PowerShell analyzer volume proof (largest supported size, all fourteen rules and nine signals)");
		_output.WriteLine(string.Format(CultureInfo.InvariantCulture, "Candidate file count: {0:N0}", harness.State.ScanOutcome.CandidateFileCount));
		_output.WriteLine(string.Format(CultureInfo.InvariantCulture, "Elapsed (Analyze stage, wall clock): {0:N3} s", elapsed.TotalSeconds));
		_output.WriteLine(string.Format(CultureInfo.InvariantCulture, "Files analysed per second: {0:N0}", harness.State.ScanOutcome.CandidateFileCount / Math.Max(elapsed.TotalSeconds, double.Epsilon)));
		_output.WriteLine(string.Format(CultureInfo.InvariantCulture, "Peak working set of the sampled process (bytes): {0:N0}", peakWorkingSetInBytes));
		_output.WriteLine(string.Format(CultureInfo.InvariantCulture, "Files evaluated for flow signals: {0:N0}", harness.State.FlowEvaluationDiagnostics.FilesEvaluated));
		_output.WriteLine(string.Format(CultureInfo.InvariantCulture, "Flow-signal trips: {0:N0}", harness.State.FlowEvaluationDiagnostics.SignalTripCount));
		_output.WriteLine(string.Format(CultureInfo.InvariantCulture, "Match-bound trips: {0:N0}", harness.SkippedItems.TallyOf(SkippedItemCategory.MatchBoundExceeded).Count));
		_output.WriteLine(string.Format(CultureInfo.InvariantCulture, "Lexical anomalies: {0:N0}", harness.SkippedItems.TallyOf(SkippedItemCategory.LexicalAnomaly).Count));

		foreach ((string patternClass, string _) in _CANONICAL_CONSTRUCTS)
		{
			_output.WriteLine(string.Format(
				CultureInfo.InvariantCulture,
				"Findings [{0}] (expected {1}): {2:N0}",
				patternClass,
				expectedCounts[patternClass],
				findingsByPatternClass[patternClass].Count()));
		}

		_output.WriteLine(string.Format(CultureInfo.InvariantCulture, "Total findings: {0:N0}", harness.State.Findings.Length));
	}
}
