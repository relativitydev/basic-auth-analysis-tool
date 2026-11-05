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
/// THE VOLUME PROOF, THROUGH THE REAL EMBEDDED `VBNet` ANALYZER AND THE REAL ANALYSIS PATH: ALL NINETEEN DETECTION RULES AND ALL TWENTY FLOW
/// SIGNALS REGISTERED, NEVER A HAND-BUILT CONSTRUCT.
///
/// FIVE THOUSAND CANDIDATE FILES, GENERATED AT RUN TIME INTO A TEMPORARY DIRECTORY OUTSIDE THE REPOSITORY AND DELETED AFTERWARDS. Every generated file carries a
/// preselection anchor, so every one is a real candidate - never a file the walk enumerates but Scan declines to open.
///
/// THREE FILE SHAPES, as the first language's proof generates them. Most files carry exactly one of the nineteen classes' own canonical construct, cycled so
/// every class is exercised at volume, and every class's own fixture is one finding: the legacy client's subsumes its credential, the explicit scheme
/// parameter's subsumes the credential parameter, and the token class and the pair class cannot overlap. A small set carry an anchor in prose only - a real
/// candidate that must yield no finding. And a minority open a string that never closes, so their scan is abandoned and the lexical anomaly is counted:
/// the number the proof records rather than works around.
///
/// FLOW-SIGNAL EVALUATION AT THE SAME VOLUME. Every positive file also carries one of three signal shapes, cycled by its index: an interactive-user signal, a
/// fixed-credential signal, or none. Every candidate file holding a surviving High or Medium construct triggers the twenty signals, so the count of files
/// evaluated must equal the number of files with such a finding, no evaluation may trip its bound, and the evidence each retained finding carries must be the
/// one its file's shape generated.
///
/// ONLY ROBUST PROPERTIES ARE ASSERTED - zero match-bound trips, every file analysed, the exact lexical-anomaly count, exact per-class finding counts and
/// the flow-signal counts above. Never a timing threshold: the elapsed time and the peak working set are recorded to the test output, not asserted.
/// </summary>
public sealed class VBNetAnalyzerVolumeProofTests
{
	private const int _NOISE_FILE_COUNT = 50;
	private const int _UNTERMINATED_STRING_FILE_COUNT = 50;
	private const int _POSITIVE_FILE_COUNT = 4_900;
	private const int _TOTAL_FILE_COUNT = _POSITIVE_FILE_COUNT + _NOISE_FILE_COUNT + _UNTERMINATED_STRING_FILE_COUNT;

	private const string _VB_001 = "VB-001";

	private const int _SIGNAL_SHAPE_COUNT = 3;
	private const int _INTERACTIVE_SHAPE_POSITION = 0;
	private const int _FIXED_SHAPE_POSITION = 1;
	private const string _INTERACTIVE_SIGNAL_LINE = "Dim typed As String = InputBox(\"Enter your password\")";
	private const string _FIXED_SIGNAL_LINE = "Dim stored = My.Settings.ApiPassword";
	private const string _POSITIVE_FILE_FOLDER_PREFIX = "positive-";
	private const string _PATH_SEPARATOR = "/";
	private const string _POSITIVE_FILE_NAME_FORMAT = "positive-{0:D5}/source/module.vb";
	private const string _NOISE_FILE_NAME_FORMAT = "noise-{0:D5}/source/module.vb";
	private const string _UNTERMINATED_FILE_NAME_FORMAT = "unterminated-{0:D5}/source/module.vb";

	/// <summary>One canonical construct per class, in catalog order, each proved isolated to exactly its class by the analyzer's own tests.</summary>
	private static readonly ImmutableArray<(string PatternClass, string Source)> _CANONICAL_CONSTRUCTS =
	[
		("VB-001", "Dim credentials As New UsernamePasswordCredentials(userName, password)"),
		("VB-002", "Dim proxy As New Vendor.Sdk.RSAPIClient(New Uri(serviceAddress), New Vendor.Sdk.UsernamePasswordCredentials(userName, password))"),
		("VB-003", "cache.Add(uri, \"Basic\", GetCredential())"),
		("VB-004", "client.Authenticator = New HttpBasicAuthenticator(userName, password)"),
		("VB-005", "Dim configured = builder.WithBasicAuthentication(userName, password)"),
		("VB-006", "client.DefaultRequestHeaders.Authorization = New AuthenticationHeaderValue(\"Basic\", encoded)"),
		("VB-007", "client.DefaultRequestHeaders.Add(\"Authorization\", \"Basic \" & encoded)"),
		("VB-008", "request.Headers.Add(HttpRequestHeader.Authorization, \"Basic \" & encoded)"),
		("VB-009", "binding.Security.Transport.ClientCredentialType = HttpClientCredentialType.Basic"),
		("VB-010", "Dim api As New ImportAPI(userName, password, webServiceUrl)"),
		("VB-011", "settings.RelativityUsername = userName\nsettings.RelativityPassword = password"),
		("VB-012", "Dim handler As New HttpClientHandler With {.Credentials = New NetworkCredential(userName, password)}"),
		("VB-013", "Dim credential As New NetworkCredential(userName, password)"),
		("VB-014", "Dim encoded = Convert.ToBase64String(Encoding.ASCII.GetBytes(userName & \":\" & password))"),
		("VB-015", "Dim encoded = Convert.ToBase64String(Encoding.ASCII.GetBytes(String.Empty & \":\" & token))"),
		("VB-016", "My.Computer.Network.DownloadFile(address, destination, userName, password)"),
		("VB-017", "Dim http As Object = CreateObject(\"MSXML2.ServerXMLHTTP.6.0\")\nhttp.Open(\"POST\", address, False, userName, password)"),
		("VB-018", "Dim proxy As New WebProxy(address) With {.Credentials = New NetworkCredential(userName, password)}"),
		("VB-019", "Public Function GetBasicAuthorization(userName As String) As String\nEnd Function\nDim credential As String")
	];

	private readonly ITestOutputHelper _output;

	public VBNetAnalyzerVolumeProofTests(ITestOutputHelper output)
	{
		_output = output;
	}

	[Fact]
	public void FiveThousandCandidateFilesAcrossAllNineteenClassesAnalyseCleanlyThroughTheRealPath()
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
		Assert.Equal(_UNTERMINATED_STRING_FILE_COUNT, harness.SkippedItems.TallyOf(SkippedItemCategory.LexicalAnomaly).Count);
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
				$"' file {fileIndex} mentions Credential and Authorization only in prose, never in code");
		}

		for (int fileIndex = 0; fileIndex < _UNTERMINATED_STRING_FILE_COUNT; fileIndex++)
		{
			harness.WriteSourceFile(
				string.Format(CultureInfo.InvariantCulture, _UNTERMINATED_FILE_NAME_FORMAT, fileIndex),
				"Dim text As String = \"never closes\nDim credentials As New UsernamePasswordCredentials(userName, password)\n");
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

			// The credential class also arrives, at Low, from the files whose string never closes.
			bool isExact = !string.Equals(patternClass, _VB_001, StringComparison.Ordinal);

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

		_output.WriteLine("VBNet analyzer volume proof (largest supported size, all nineteen rules and twenty signals)");
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
