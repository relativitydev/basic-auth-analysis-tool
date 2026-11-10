using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.Text;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Findings;
using MigrateAuth.Cli.Reading;
using MigrateAuth.Cli.Session;
using MigrateAuth.Cli.Tests.Pipeline;

using Xunit;
using Xunit.Abstractions;

namespace MigrateAuth.Cli.Tests.AcceptanceHarness;

/// <summary>
/// THE VOLUME PROOF, THROUGH THE REAL EMBEDDED `CSharp` ANALYZER AND THE REAL
/// ANALYSIS PATH - ALL NINETEEN DETECTION RULES REGISTERED, NEVER A
/// HAND-BUILT `RecognizedConstruct`.
///
/// FIVE THOUSAND CANDIDATE FILES, GENERATED AT RUN TIME INTO A TEMPORARY DIRECTORY OUTSIDE THE
/// REPOSITORY AND DELETED AFTERWARDS. <see cref="PipelineHarness.CreateOverTheEmbeddedCatalog"/>'s
/// own fixture already writes files under <see cref="Path.GetTempPath"/> (see
/// <c>MigrateAuth.Cli.Tests.Reading.RepositoryFixture</c>) and disposes recursively; nothing here
/// is committed. Every generated file carries a preselection anchor, so every one is a real candidate
/// - never a file the walk enumerates but Scan declines to open.
///
/// THREE FILE SHAPES. Most files carry exactly one of the nineteen classes' own canonical
/// construct, cycled so every class is exercised at volume. A small set carry an anchor in prose
/// only - a real candidate that must yield no finding, proving the anchor alone does not manufacture
/// one. And a deliberate minority prepend a verbatim string literal that the shared scanner's known
/// defect (<c>LexicalScanner.Literals.cs</c>'s <c>MatchAConstantOpening</c>) never closes, so their
/// scan is abandoned mid-file and the skipped-item list's
/// <see cref="SkippedItemCategory.LexicalAnomaly"/> counts them - the number this proof records
/// rather than works around.
///
/// ONE CANONICAL CONSTRUCT IS NOT SELF-CONTAINED, AND THE EXPECTED COUNTS SAY SO RATHER THAN
/// ASSUME OTHERWISE. CS-011's own construct co-fires CS-001 (no subsumption between a helper's
/// declaration and the credential construction inside it). The addition is folded into the expected
/// per-class tallies as it is generated, never asserted as an afterthought. CS-003's own construct
/// subsumes CS-008, so a cache entry in the generated files is one finding.
///
/// FLOW-SIGNAL EVALUATION AT THE SAME VOLUME. Every positive file also carries
/// one of three signal shapes, cycled by its index: an interactive-user signal, a fixed-credential
/// signal, or none. Every candidate file holding a surviving High or Medium construct triggers the
/// seventeen signals, so the count of files evaluated must equal the number of files with such a
/// finding - taken before the global cap, which nothing here reaches - and no evaluation may trip its
/// bound. The evidence each retained High or Medium finding carries must be the one its file's shape
/// generated.
///
/// ONLY ROBUST PROPERTIES ARE ASSERTED - zero match-bound trips, every file analysed, the exact
/// lexical-anomaly count, exact or lower-bound per-class finding counts, and the flow-signal counts
/// above. Never a timing threshold: elapsed time and the peak working set of the sampled process are
/// recorded to the test output, not asserted.
/// </summary>
[Collection(SessionStateVolumeProofTests.COLLECTION_NAME)]
public sealed class CSharpAnalyzerVolumeProofTests : IClassFixture<VolumeProofMemoryRelease>
{
	private const int _NOISE_FILE_COUNT = 50;
	private const int _UNTERMINATED_LITERAL_FILE_COUNT = 50;
	private const int _POSITIVE_FILE_COUNT = 4_900;
	private const int _TOTAL_FILE_COUNT = _POSITIVE_FILE_COUNT + _NOISE_FILE_COUNT + _UNTERMINATED_LITERAL_FILE_COUNT;

	private const string _CS_001 = "CS-001";
	private const string _CS_011 = "CS-011";

	private const int _SIGNAL_SHAPE_COUNT = 3;
	private const int _INTERACTIVE_SHAPE_POSITION = 0;
	private const int _FIXED_SHAPE_POSITION = 1;
	private const string _INTERACTIVE_SIGNAL_LINE = "Console.Write(\"Enter your password: \");";
	private const string _FIXED_SIGNAL_LINE = "var stored = Environment.GetEnvironmentVariable(\"SERVICE_PWD\");";
	private const string _POSITIVE_FILE_FOLDER_PREFIX = "positive-";
	private const string _PATH_SEPARATOR = "/";
	private const string _POSITIVE_FILE_NAME_FORMAT = "positive-{0:D5}/source/module.cs";
	private const string _NOISE_FILE_NAME_FORMAT = "noise-{0:D5}/source/module.cs";
	private const string _UNTERMINATED_LITERAL_FILE_NAME_FORMAT = "unterminated-{0:D5}/source/module.cs";

	/// <summary>
	/// One canonical construct per class, in catalog order, taken verbatim from the fixtures
	/// `CSharpAnalyzerTests` already proves isolated to exactly this class (with the two declared
	/// exceptions above). Reusing proven fixtures is what keeps this proof's own expected counts
	/// trustworthy without re-deriving each regex's behaviour here.
	/// </summary>
	private static readonly ImmutableArray<(string PatternClass, string Source)> _CANONICAL_CONSTRUCTS =
	[
		("CS-001", "Credentials credentials = new UsernamePasswordCredentials(userName, password);"),
		("CS-002", "var client = new RSAPIClient(rsapiUri, new UsernamePasswordCredentials(userName, password));"),
		("CS-003", "cache.Add(new Uri(instanceUrl), \"Basic\", new NetworkCredential(userName, password));"),
		("CS-004", "var authenticator = new HttpBasicAuthenticator(userName, password);"),
		("CS-005", "client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(\"Basic\", encodedCredentials);"),
		// The interpolated form ($"Basic {credential}") is deliberately NOT used here: it is the
		// asymmetric-opening shape the known scanner defect never closes (see this class's own
		// header note), and at the volume this fixture generates it would flag every one of ~270 files
		// as a lexical anomaly. The named-constant CONCATENATION form matches the same class (see
		// `CSharpAnalyzerTests.CS006MatchesTheNamedConstantConcatenationForm`) using only
		// single-character, symmetric `"..."` literals, which the defect does not reach.
		("CS-006", "const string SCHEME = \"Basic\";\nclient.DefaultRequestHeaders.Add(\"Authorization\", SCHEME + \" \" + encodedCredentials);"),
		("CS-007", "var handler = new HttpClientHandler { Credentials = new NetworkCredential(userName, password) };"),
		("CS-008", "var credential = new NetworkCredential(userName, password);"),
		("CS-009", "string pair = identifier + \":\" + secret;\nstring encoded = Convert.ToBase64String(Encoding.ASCII.GetBytes(pair));"),
		("CS-011", "private ServiceFactorySettings BuildServiceFactoryCredentials(string userName, string password)\n{\n\tCredentials credentials = new UsernamePasswordCredentials(userName, password);\n\tServiceFactorySettings settings = new ServiceFactorySettings(restEndpointUri, credentials);\n\treturn settings;\n}"),
		("CS-012", "client.Headers[HttpRequestHeader.Authorization] = \"Basic \" + encoded;"),
		("CS-013", "binding.Security.Transport.ClientCredentialType = HttpClientCredentialType.Basic;\nclient.ClientCredentials.UserName.UserName = userName;\nclient.ClientCredentials.UserName.Password = password;"),
		("CS-014", "var result = client.WithBasicAuth(userName, password);"),
		("CS-015", "var importApi = new ImportAPI(userName, password, webServiceUrl);"),
		("CS-016", "NetworkCredential credential = new NetworkCredential { UserName = userName };\ncredential.Password = password;"),
		("CS-017", "importJob.Settings.RelativityUsername = userName;\nimportJob.Settings.RelativityPassword = password;"),
		("CS-018", "string pair = \"\" + \":\" + accessToken;\nstring encoded = Convert.ToBase64String(Encoding.ASCII.GetBytes(pair));"),
		("CS-019", "var sink = new SmtpClient(host, port);\nsink.Credentials = new NetworkCredential(userName, password);"),
		("CS-020", "var proxy = helper.GetProxy<IExampleService>(userName, password);")
	];

	/// <summary>
	/// The one canonical positive per pattern class, exposed so another test can run every rule over the
	/// same fixtures instead of keeping its own copy.
	/// </summary>
	internal static ImmutableArray<(string PatternClass, string Source)> CanonicalConstructs => _CANONICAL_CONSTRUCTS;

	private readonly ITestOutputHelper _output;

	public CSharpAnalyzerVolumeProofTests(ITestOutputHelper output)
	{
		_output = output;
	}

	[Fact]
	public void FiveThousandCandidateFilesAcrossAllNineteenClassesAnalyseCleanlyThroughTheRealPath()
	{
		using PipelineHarness harness = PipelineHarness.CreateOverTheEmbeddedCatalog();

		Dictionary<string, int> expectedMinimumCounts = PlantTree(harness);

		harness.RunAndCommit(CommandId.Scan);

		PeakWorkingSetSampler.ReleaseUnusedMemory();

		PeakWorkingSetSampler sampler = PeakWorkingSetSampler.Start();
		Stopwatch stopwatch = Stopwatch.StartNew();
		StageRunOutcome outcome = harness.Run(CommandId.Analyze);
		stopwatch.Stop();
		long peakWorkingSetInBytes = sampler.StopAndReadPeakInBytes();

		ReportFigures(harness, stopwatch.Elapsed, peakWorkingSetInBytes, expectedMinimumCounts);

		Assert.Equal(StageRunOutcome.Completed, outcome);
		AssertEveryFileWasACandidateAndNoneWasSkippedForAnyOtherReason(harness);
		AssertNoMatchBoundTrip(harness);
		AssertExactLexicalAnomalyCount(harness);
		AssertPerClassFindingCounts(harness, expectedMinimumCounts);
		AssertEveryHighOrMediumFileWasEvaluatedForFlowSignals(harness);
		Assert.False(harness.State.FindingsCapOutcome.IsTruncated);
	}

	/// <summary>
	/// The files-evaluated counter equals the number of files holding a High or Medium finding, no
	/// evaluation tripped, and the retained High and Medium findings carry the evidence their file's
	/// signal shape generated - all three kinds of evidence occur.
	/// </summary>
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

	/// <summary>
	/// Generates all three shapes and returns the MINIMUM finding count each of the nineteen classes
	/// must reach - exact for every class except <see cref="_CS_001"/>, whose fixtures also arrive as a
	/// side effect of the fifty unterminated-literal files.
	/// </summary>
	private static Dictionary<string, int> PlantTree(PipelineHarness harness)
	{
		Dictionary<string, int> expectedMinimumCounts = _CANONICAL_CONSTRUCTS
			.ToDictionary(entry => entry.PatternClass, _ => 0, StringComparer.Ordinal);

		PlantPositiveFiles(harness, expectedMinimumCounts);
		PlantNoiseFiles(harness);
		PlantUnterminatedLiteralFiles(harness, expectedMinimumCounts);

		return expectedMinimumCounts;
	}

	private static void PlantPositiveFiles(PipelineHarness harness, Dictionary<string, int> expectedMinimumCounts)
	{
		for (int fileIndex = 0; fileIndex < _POSITIVE_FILE_COUNT; fileIndex++)
		{
			(string patternClass, string source) = _CANONICAL_CONSTRUCTS[fileIndex % _CANONICAL_CONSTRUCTS.Length];

			harness.WriteSourceFile(
				string.Format(CultureInfo.InvariantCulture, _POSITIVE_FILE_NAME_FORMAT, fileIndex),
				source + "\n" + SignalShapeOf(fileIndex));

			expectedMinimumCounts[patternClass]++;
			FoldInCoEmission(expectedMinimumCounts, patternClass);
		}
	}

	/// <summary>CS-011 co-fires CS-001 (declared no-subsumption).</summary>
	private static void FoldInCoEmission(Dictionary<string, int> expectedMinimumCounts, string patternClass)
	{
		if (string.Equals(patternClass, _CS_011, StringComparison.Ordinal))
		{
			expectedMinimumCounts[_CS_001]++;
		}
	}

	/// <summary>An anchor in prose only - a real candidate, structurally incapable of matching any of the nineteen rules.</summary>
	private static void PlantNoiseFiles(PipelineHarness harness)
	{
		for (int fileIndex = 0; fileIndex < _NOISE_FILE_COUNT; fileIndex++)
		{
			string source = $"// file {fileIndex} mentions Credential and Authorization only in prose, never in code";

			harness.WriteSourceFile(string.Format(CultureInfo.InvariantCulture, _NOISE_FILE_NAME_FORMAT, fileIndex), source);
		}
	}

	/// <summary>
	/// A verbatim literal that never closes, per the known base defect, followed by CS-001's own
	/// construct. The construct's own outcome after the abandoned scan is not asserted exactly -
	/// only that the file is counted as a lexical anomaly, and that CS-001's overall count is not
	/// reduced below its clean baseline.
	/// </summary>
	private static void PlantUnterminatedLiteralFiles(PipelineHarness harness, Dictionary<string, int> expectedMinimumCounts)
	{
		for (int fileIndex = 0; fileIndex < _UNTERMINATED_LITERAL_FILE_COUNT; fileIndex++)
		{
			string source = BuildUnterminatedLiteralSource();

			harness.WriteSourceFile(
				string.Format(CultureInfo.InvariantCulture, _UNTERMINATED_LITERAL_FILE_NAME_FORMAT, fileIndex),
				source);
		}

		_ = expectedMinimumCounts;
	}

	private static string BuildUnterminatedLiteralSource()
	{
		StringBuilder builder = new();

		builder.AppendLine("string broken = @\"never closes");
		builder.AppendLine("Credentials credentials = new UsernamePasswordCredentials(userName, password);");

		return builder.ToString();
	}

	private static void AssertEveryFileWasACandidateAndNoneWasSkippedForAnyOtherReason(PipelineHarness harness)
	{
		Assert.Equal(_TOTAL_FILE_COUNT, harness.State.ScanOutcome.FileCount);
		Assert.Equal(_TOTAL_FILE_COUNT, harness.State.ScanOutcome.CandidateFileCount);
		Assert.Equal(0, harness.SkippedItems.TallyOf(SkippedItemCategory.UnreadableFile).Count);
		Assert.Equal(0, harness.SkippedItems.TallyOf(SkippedItemCategory.UnlistableDirectory).Count);
		Assert.Equal(0, harness.SkippedItems.TallyOf(SkippedItemCategory.LinkNotFollowed).Count);
		Assert.Equal(0, harness.SkippedItems.TallyOf(SkippedItemCategory.Undecodable).Count);
		Assert.Equal(0, harness.SkippedItems.TallyOf(SkippedItemCategory.OversizeFile).Count);
		Assert.Equal(0, harness.SkippedItems.TallyOf(SkippedItemCategory.OverLongLine).Count);
		Assert.Equal(0, harness.SkippedItems.TallyOf(SkippedItemCategory.PathTooLong).Count);
		Assert.Equal(0, harness.SkippedItems.TallyOf(SkippedItemCategory.MissingCandidate).Count);
	}

	private static void AssertNoMatchBoundTrip(PipelineHarness harness)
	{
		Assert.Equal(0, harness.SkippedItems.TallyOf(SkippedItemCategory.MatchBoundExceeded).Count);
	}

	private static void AssertExactLexicalAnomalyCount(PipelineHarness harness)
	{
		Assert.Equal(
			_UNTERMINATED_LITERAL_FILE_COUNT,
			harness.SkippedItems.TallyOf(SkippedItemCategory.LexicalAnomaly).Count);
	}

	private static void AssertPerClassFindingCounts(PipelineHarness harness, Dictionary<string, int> expectedMinimumCounts)
	{
		ILookup<string, Finding> findingsByPatternClass = harness.State.Findings.ToLookup(
			finding => finding.PatternClass.Value,
			StringComparer.Ordinal);

		foreach ((string patternClass, string _) in _CANONICAL_CONSTRUCTS)
		{
			int actualCount = findingsByPatternClass[patternClass].Count();
			int expectedMinimumCount = expectedMinimumCounts[patternClass];

			bool isExact = !string.Equals(patternClass, _CS_001, StringComparison.Ordinal);

			if (isExact)
			{
				Assert.True(
					actualCount == expectedMinimumCount,
					$"{patternClass}: expected exactly {expectedMinimumCount}, found {actualCount}.");
			}
			else
			{
				Assert.True(
					actualCount >= expectedMinimumCount,
					$"{patternClass}: expected at least {expectedMinimumCount}, found {actualCount}.");
			}
		}
	}

	private void ReportFigures(
		PipelineHarness harness,
		TimeSpan elapsed,
		long peakWorkingSetInBytes,
		Dictionary<string, int> expectedMinimumCounts)
	{
		ILookup<string, Finding> findingsByPatternClass = harness.State.Findings.ToLookup(
			finding => finding.PatternClass.Value,
			StringComparer.Ordinal);

		_output.WriteLine("CSharp analyzer volume proof (largest supported size, all nineteen rules)");
		_output.WriteLine(FormattedLine("Candidate file count", harness.State.ScanOutcome.CandidateFileCount));
		_output.WriteLine(FormattedLine("Total file count", harness.State.ScanOutcome.FileCount));
		_output.WriteLine($"Elapsed (Analyze stage, wall clock): {elapsed.TotalSeconds:N3} s");
		_output.WriteLine(string.Format(CultureInfo.InvariantCulture, "Elapsed (Analyze stage, milliseconds): {0:N0}", (long)elapsed.TotalMilliseconds));
		_output.WriteLine(string.Format(CultureInfo.InvariantCulture, "Peak working set of the sampled process (bytes): {0:N0}", peakWorkingSetInBytes));
		_output.WriteLine(FormattedLine("Files evaluated for flow signals", harness.State.FlowEvaluationDiagnostics.FilesEvaluated));
		_output.WriteLine(FormattedLine("Flow-signal trips", harness.State.FlowEvaluationDiagnostics.SignalTripCount));
		_output.WriteLine(FormattedLine("Match-bound trips", harness.SkippedItems.TallyOf(SkippedItemCategory.MatchBoundExceeded).Count));
		_output.WriteLine(FormattedLine("Lexical anomalies (UnterminatedLiteral)", harness.SkippedItems.TallyOf(SkippedItemCategory.LexicalAnomaly).Count));

		foreach (SkippedItemCategory category in SkippedItemCategories.ALL)
		{
			_output.WriteLine(FormattedLine($"Skipped [{category}]", harness.SkippedItems.TallyOf(category).Count));
		}

		foreach ((string patternClass, string _) in _CANONICAL_CONSTRUCTS)
		{
			int actualCount = findingsByPatternClass[patternClass].Count();

			_output.WriteLine(FormattedLine($"Findings [{patternClass}] (expected minimum {expectedMinimumCounts[patternClass]})", actualCount));
		}

		_output.WriteLine(FormattedLine("Total findings", harness.State.Findings.Length));
	}

	private static string FormattedLine(string label, int value)
	{
		return string.Format(CultureInfo.InvariantCulture, "{0}: {1:N0}", label, value);
	}
}
