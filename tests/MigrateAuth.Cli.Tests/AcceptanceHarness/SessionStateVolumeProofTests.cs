using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Findings;
using MigrateAuth.Cli.Reading;
using MigrateAuth.Cli.Recognition;
using MigrateAuth.Cli.Reporting;
using MigrateAuth.Cli.Scanning;
using MigrateAuth.Cli.Session;
using MigrateAuth.Cli.Tests.Reporting;
using MigrateAuth.Cli.Tests.Session;

using Xunit;
using Xunit.Abstractions;

namespace MigrateAuth.Cli.Tests.AcceptanceHarness;

/// <summary>
/// The volume proofs run one at a time, and alone. They read the process's working set, and any
/// other test running in the same host at the same moment would be counted in it.
/// </summary>
[CollectionDefinition(SessionStateVolumeProofTests.COLLECTION_NAME, DisableParallelization = true)]
public sealed class SessionStateVolumeProofCollection
{
}

/// <summary>
/// THE SESSION STATE AT THE LARGEST SUPPORTED SIZE, WRITTEN AND READ BY THE REAL STORE.
///
/// The document is the one term of the product that grows with the repository: a path per candidate
/// and a finding per retained construct. The worst case is 500,000 persisted candidate paths
/// beside 50,000 findings, and two proofs stand on it.
///
/// The first proof puts 100,000 flow-bearing findings in a state and reloads it. The global findings cap is
/// 50,000, so 100,000 is TWICE THE CAP: the headroom convention, not a number the tool can produce
/// today.
///
/// The second puts the same 100,000 findings beside 500,000 candidate paths of 150 ASCII characters,
/// writes the state, and reloads it. The size of that file is what the ceiling
/// (<see cref="SessionStateStore.MAXIMUM_STATE_FILE_LENGTH_IN_BYTES"/>) is sized from - the size
/// rounded up to the next 16 MiB - and the same run measures the peak working set and the duration
/// of the save and of the load against the one-gigabyte memory limit.
///
/// WHAT THE FIGURES ARE. The synthetic state carries every term a state carries, in the shape the
/// engine produces it, so nothing is left out that would make the file smaller than a real one. The
/// working set is sampled (see <see cref="PeakWorkingSetSampler"/>) in this process, with the save
/// measured while the state it writes is live, as it is when a stage commits, and the load measured
/// after that state has been released, as it is at the start of a session. The figures are printed
/// with the test output; record them from a run of these tests alone.
/// </summary>
[Collection(COLLECTION_NAME)]
public sealed class SessionStateVolumeProofTests
{
	internal const string COLLECTION_NAME = "SessionStateVolumeProof";

	private const int _CANDIDATE_COUNT = 500_000;
	private const int _CANDIDATE_PATH_LENGTH = 150;
	private const int _FINDING_COUNT = 100_000;

	/// <summary>Where a finding's file sits in the candidate set: every fifth candidate, so the findings spread across it.</summary>
	private const int _CANDIDATES_PER_FINDING = _CANDIDATE_COUNT / _FINDING_COUNT;

	private const int _FINDINGS_FOR_THE_HEADROOM_PROOF_CANDIDATE_COUNT = 1_000;

	private const long _ROUNDING_UNIT_IN_BYTES = 16L * 1024 * 1024;
	private const long _MEMORY_LIMIT_IN_BYTES = 1024L * 1024 * 1024;
	private const double _BYTES_PER_MEBIBYTE = 1024.0 * 1024.0;
	private const int _LINES_PER_FILE_CYCLE = 1_000;

	private const string _PATH_FORMAT = "src/module-{0:D6}/";
	private const string _PATH_EXTENSION = ".cs";
	private const char _PATH_FILLER_CHARACTER = 'a';
	private const string _LANGUAGE_NAME = "CSharp";
	private const string _PATTERN_CLASS = "CS-001";

	private readonly ITestOutputHelper _output;

	public SessionStateVolumeProofTests(ITestOutputHelper output)
	{
		_output = output;
	}

	/// <summary>
	/// A state holding 100,000 findings with flow fields is written and
	/// reloaded under the ceiling. 100,000 is twice the global cap and therefore a headroom proof.
	/// </summary>
	[Fact]
	public void AStateHolding100000FlowBearingFindingsIsWrittenAndReloadedUnderTheCeiling()
	{
		Assert.Equal(2 * FindingsCapPolicy.PROVISIONAL_GLOBAL_FINDINGS_CAP, _FINDING_COUNT);

		using TemporaryFolder destination = TemporaryFolder.Create();
		using TemporaryFolder repositoryFolder = TemporaryFolder.Create();

		RepositoryFolderPath repository = RepositoryFolderPath.FromSuppliedText(repositoryFolder.Path);
		SessionStateStore store = new(DocumentationDestinationFolderPath.FromSuppliedText(destination.Path));
		SessionState state = BuildSyntheticState(repository, _FINDINGS_FOR_THE_HEADROOM_PROOF_CANDIDATE_COUNT, _FINDING_COUNT);

		store.Save(repository, state);

		long fileLength = new FileInfo(destination.Combine(SessionStateStore.BuildStateFileName(repository))).Length;
		SessionStateLoad reloaded = store.Load(repository, LanguageCatalogRegistry.BuildEmbeddedCatalog());

		_output.WriteLine(FormattedLine("Findings with flow fields", _FINDING_COUNT));
		_output.WriteLine(FormattedLine("Serialized size (bytes)", fileLength));

		Assert.True(fileLength < SessionStateStore.MAXIMUM_STATE_FILE_LENGTH_IN_BYTES);
		Assert.True(reloaded.WasResumed, reloaded.RejectionReason.ToString());
		Assert.Equal(_FINDING_COUNT, reloaded.State.Findings.Length);
		Assert.Equal(state.Findings.AsEnumerable(), reloaded.State.Findings.AsEnumerable());
		Assert.All(reloaded.State.Findings, finding => Assert.NotNull(finding.Flow));
	}

	/// <summary>
	/// 500,000 candidate paths of 150 characters
	/// and 100,000 flow-bearing findings are written and reloaded under the ceiling; the ceiling is
	/// the measured size rounded up to the next 16 MiB; and the peak working set of the save and of
	/// the load stays below 1 GB.
	/// </summary>
	[Fact]
	public void FiveHundredThousandCandidatePathsAndOneHundredThousandFindingsFitTheCeilingAndBothOperationsStayUnderOneGigabyte()
	{
		using TemporaryFolder destination = TemporaryFolder.Create();
		using TemporaryFolder repositoryFolder = TemporaryFolder.Create();

		RepositoryFolderPath repository = RepositoryFolderPath.FromSuppliedText(repositoryFolder.Path);
		SessionStateStore store = new(DocumentationDestinationFolderPath.FromSuppliedText(destination.Path));
		string statePath = destination.Combine(SessionStateStore.BuildStateFileName(repository));
		LanguageCatalog catalog = LanguageCatalogRegistry.BuildEmbeddedCatalog();

		SessionState state = BuildSyntheticState(repository, _CANDIDATE_COUNT, _FINDING_COUNT);
		OperationFigures save = Measure(() => store.Save(repository, state));
		long fileLength = new FileInfo(statePath).Length;
		string lastCandidate = state.CandidateRelativePaths[^1];

		state = null!;

		FreshSessionLoad freshSession = FreshSessionLoadMeter.Measure(repositoryFolder.Path, destination.Path);
		SessionStateLoad reloaded = null!;
		OperationFigures load = Measure(() => reloaded = store.Load(repository, catalog));

		ReportFigures(fileLength, save, load, freshSession);

		Assert.True(freshSession.Resumed, "The fresh session did not resume from the saved state.");
		Assert.True(reloaded.WasResumed, reloaded.RejectionReason.ToString());
		Assert.Equal(_CANDIDATE_COUNT, reloaded.State.CandidateRelativePaths.Count);
		Assert.Equal(_CANDIDATE_PATH_LENGTH, lastCandidate.Length);
		Assert.Equal(lastCandidate, reloaded.State.CandidateRelativePaths[^1]);
		Assert.Equal(_CANDIDATE_COUNT, reloaded.State.CandidateLanguageNames.Count);
		Assert.Equal(
			BuildFindings(BuildCandidatePaths(_CANDIDATE_COUNT), _FINDING_COUNT).AsEnumerable(),
			reloaded.State.Findings.AsEnumerable());

		AssertTheCeilingIsTheMeasuredSizeRoundedUp(fileLength);
		Assert.True(save.PeakWorkingSetInBytes < _MEMORY_LIMIT_IN_BYTES, "The save passed 1 GB of working set.");
		Assert.True(freshSession.PeakWorkingSetInBytes < _MEMORY_LIMIT_IN_BYTES, "The load in a fresh session passed 1 GB of working set.");
	}

	/// <summary>
	/// The ceiling has one basis, and this is where it is held to it: the file this synthetic state
	/// writes fits under the ceiling, and the ceiling is no more than one rounding unit above it. A
	/// change to the document that makes the file larger fails here and asks for the basis to be
	/// measured again, where it would otherwise pass until a real repository met the write check.
	/// </summary>
	private static void AssertTheCeilingIsTheMeasuredSizeRoundedUp(long fileLength)
	{
		long roundedUpLength = (fileLength + _ROUNDING_UNIT_IN_BYTES - 1) / _ROUNDING_UNIT_IN_BYTES * _ROUNDING_UNIT_IN_BYTES;

		Assert.Equal(SessionStateStore.MAXIMUM_STATE_FILE_LENGTH_IN_BYTES, roundedUpLength);
	}

	private void ReportFigures(long fileLength, OperationFigures save, OperationFigures load, FreshSessionLoad freshSession)
	{
		long roundedUpLength = (fileLength + _ROUNDING_UNIT_IN_BYTES - 1) / _ROUNDING_UNIT_IN_BYTES * _ROUNDING_UNIT_IN_BYTES;

		_output.WriteLine("Session state volume proof (largest supported size)");
		_output.WriteLine($"Platform: {System.Runtime.InteropServices.RuntimeInformation.OSDescription}");
		_output.WriteLine(FormattedLine("Candidate paths", _CANDIDATE_COUNT));
		_output.WriteLine(FormattedLine("Characters per candidate path", _CANDIDATE_PATH_LENGTH));
		_output.WriteLine(FormattedLine("Flow-bearing findings", _FINDING_COUNT));
		_output.WriteLine(FormattedLine("Serialized size (bytes)", fileLength));
		_output.WriteLine(string.Format(CultureInfo.InvariantCulture, "Serialized size (MiB): {0:F2}", fileLength / _BYTES_PER_MEBIBYTE));
		_output.WriteLine(FormattedLine("Size rounded up to the next 16 MiB (bytes)", roundedUpLength));
		_output.WriteLine(FormattedLine("Ceiling in the product (bytes)", SessionStateStore.MAXIMUM_STATE_FILE_LENGTH_IN_BYTES));
		_output.WriteLine(FormattedFigures("Save (in this process, state live)", save));
		_output.WriteLine(FormattedFigures("Load (in this process, upper bound: memory left resident by earlier phases counts)", load));
		_output.WriteLine(string.Format(
			CultureInfo.InvariantCulture,
			"Load (fresh session process, the figure that answers the resume-cost question): time to session ready {0:F0} ms including runtime start, peak working set {1:N0} bytes ({2:F1} MiB)",
			freshSession.TimeToSessionReady.TotalMilliseconds,
			freshSession.PeakWorkingSetInBytes,
			freshSession.PeakWorkingSetInBytes / _BYTES_PER_MEBIBYTE));
		_output.WriteLine(FormattedLine("Limit (1 GB taken as 1 GiB, bytes)", _MEMORY_LIMIT_IN_BYTES));
	}

	private static string FormattedFigures(string operation, OperationFigures figures)
	{
		return string.Format(
			CultureInfo.InvariantCulture,
			"{0}: duration {1:F0} ms, peak working set {2:N0} bytes ({3:F1} MiB), working set before {4:N0} bytes",
			operation,
			figures.Duration.TotalMilliseconds,
			figures.PeakWorkingSetInBytes,
			figures.PeakWorkingSetInBytes / _BYTES_PER_MEBIBYTE,
			figures.WorkingSetBeforeInBytes);
	}

	private static string FormattedLine(string label, long value)
	{
		return string.Format(CultureInfo.InvariantCulture, "{0}: {1:N0}", label, value);
	}

	private static OperationFigures Measure(Action operation)
	{
		PeakWorkingSetSampler.ReleaseUnusedMemory();

		PeakWorkingSetSampler sampler = PeakWorkingSetSampler.Start();
		long workingSetBefore = Environment.WorkingSet;
		Stopwatch stopwatch = Stopwatch.StartNew();

		operation();

		stopwatch.Stop();

		return new OperationFigures(stopwatch.Elapsed, sampler.StopAndReadPeakInBytes(), workingSetBefore);
	}

	/// <summary>
	/// A state a completed scan and a completed analysis could have produced: the candidates with their
	/// attribution, a scan summary that agrees with them, the findings numbered from one with the flow
	/// fields every kind of evidence produces, and the counters those runs record.
	/// </summary>
	private static SessionState BuildSyntheticState(RepositoryFolderPath repository, int candidateCount, int findingCount)
	{
		SessionState state = SessionState.Fresh(LanguageCatalog.CurrentVersion, repository.Label);
		string[] candidatePaths = BuildCandidatePaths(candidateCount);

		state.RecordScanOutcome(
			candidatePaths,
			[.. Enumerable.Repeat(_LANGUAGE_NAME, candidateCount)],
			BuildScanSummary(candidateCount),
			SkippedItemTallies.Empty,
			[AssessedLanguage.CSharp]);
		state.MarkCompleted(CommandId.Scan);
		state.RecordAnalysisOutcome(
			BuildFindings(candidatePaths, findingCount),
			BuildUntrippedCapOutcome(findingCount),
			SkippedItemTallies.Empty,
			new FlowEvaluationDiagnostics { FilesEvaluated = Math.Min(findingCount, candidateCount), SignalTripCount = 0 },
			EffectiveDefaultFlow.BuiltIn);
		state.MarkCompleted(CommandId.Analyze);

		return state;
	}

	private static string[] BuildCandidatePaths(int candidateCount)
	{
		string[] paths = new string[candidateCount];
		int fillerLength = _CANDIDATE_PATH_LENGTH - string.Format(CultureInfo.InvariantCulture, _PATH_FORMAT, 0).Length - _PATH_EXTENSION.Length;
		string filler = new(_PATH_FILLER_CHARACTER, fillerLength);

		for (int index = 0; index < candidateCount; index++)
		{
			paths[index] = string.Format(CultureInfo.InvariantCulture, _PATH_FORMAT, index) + filler + _PATH_EXTENSION;
		}

		return paths;
	}

	private static ScanSummary BuildScanSummary(int candidateCount)
	{
		return new ScanSummary
		{
			FileCount = candidateCount,
			LineCount = 0,
			CandidateFileCount = candidateCount,
			Rows =
			[
				new LanguageInventoryRow
				{
					LanguageName = _LANGUAGE_NAME,
					FileCount = candidateCount,
					LineCount = 0,
					CandidateFileCount = candidateCount,
					ShareOfFilesPercentage = 100.0
				}
			]
		};
	}

	/// <summary>
	/// One finding per file, cycling through the three kinds of evidence so that every flow decision is
	/// present, on candidate paths spread across the set. Each carries a path as long as the
	/// candidates', which is what a real finding's path is.
	/// </summary>
	private static ImmutableArray<Finding> BuildFindings(string[] candidatePaths, int findingCount)
	{
		ImmutableArray<Finding>.Builder findings = ImmutableArray.CreateBuilder<Finding>(findingCount);
		int stride = Math.Max(1, candidatePaths.Length / findingCount);

		for (int index = 0; index < findingCount; index++)
		{
			string path = candidatePaths[Math.Min(index * stride, candidatePaths.Length - 1)];
			Finding unassigned = ReportHarness.FindingAt(
				sequentialIndex: index + 1,
				relativePath: path,
				line: 1 + (index % _LINES_PER_FILE_CYCLE),
				patternClassIdentifier: _PATTERN_CLASS,
				level: ConfidenceLevel.High,
				certainty: index % 2 == 0 ? DestinationCertainty.Established : DestinationCertainty.Unknown,
				literalCredentialSignal: index % 3 == 0) with
			{
				FlowEvidence = EvidenceOf(index)
			};

			findings.Add(FlowAssignment.Assign(unassigned, CredentialFlows.BUILT_IN_DEFAULT));
		}

		return findings.MoveToImmutable();
	}

	private static FlowEvidence EvidenceOf(int findingIndex)
	{
		return (findingIndex % Enum.GetValues<FlowEvidence>().Length) switch
		{
			0 => FlowEvidence.None,
			1 => FlowEvidence.Fixed,
			_ => FlowEvidence.Interactive
		};
	}

	private static FindingsCapOutcome BuildUntrippedCapOutcome(int findingCount)
	{
		return new FindingsCapOutcome
		{
			TotalRecognizedCount = findingCount,
			RetainedCount = findingCount,
			PerFileCapTrippedFileCount = 0,
			PerFileOmittedCount = 0,
			GlobalCapTripped = false,
			GlobalOmittedCount = 0
		};
	}

	private readonly record struct OperationFigures(TimeSpan Duration, long PeakWorkingSetInBytes, long WorkingSetBeforeInBytes);
}
