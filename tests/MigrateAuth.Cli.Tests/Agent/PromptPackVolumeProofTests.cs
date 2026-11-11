using System.Collections.Immutable;
using System.Globalization;

using MigrateAuth.Cli.Agent;
using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Findings;
using MigrateAuth.Cli.Tests.AcceptanceHarness;
using MigrateAuth.Cli.Tests.Session;
using MigrateAuth.Cli.Tests.Writing;
using MigrateAuth.Cli.Writing;

using Xunit;
using Xunit.Abstractions;

namespace MigrateAuth.Cli.Tests.Agent;

/// <summary>
/// THE REAL PACK AT THE FINDINGS CAP. Fifty thousand findings go through the scope, the pack writer and the atomic writer
/// in the existing volume-proof child process, and the peak working set of the generation stays under 1 GiB, as every other
/// volume proof of this suite is held to. The pack lists every finding, and the entry count the console states is the scope's
/// total. NO TIME IS ASSERTED. Run alone: the working set is process-wide.
/// </summary>
[Collection(SessionStateVolumeProofTests.COLLECTION_NAME)]
public sealed class PromptPackVolumeProofTests : IClassFixture<VolumeProofMemoryRelease>
{
	internal const string PACK_GENERATION_OPERATION = "pack-generation";

	private const int _FINDING_COUNT = 50_000;
	private const long _MEMORY_LIMIT_IN_BYTES = 1024L * 1024 * 1024;
	private const double _BYTES_PER_MEBIBYTE = 1024.0 * 1024.0;

	private readonly ITestOutputHelper _output;

	public PromptPackVolumeProofTests(ITestOutputHelper output)
	{
		_output = output;
	}

	[Fact]
	public void GeneratingThePackAtTheFindingsCapStaysUnderOneGibibyteAndListsEveryFinding()
	{
		Assert.Equal(FindingsCapPolicy.PROVISIONAL_GLOBAL_FINDINGS_CAP, _FINDING_COUNT);

		(OperationFigures figures, IReadOnlyList<string> details) = VolumeProofChildProcess.RunAndReadDetails(PACK_GENERATION_OPERATION);

		_output.WriteLine("Prompt pack generation volume proof (findings cap)");

		foreach (string detail in details)
		{
			_output.WriteLine(detail);
		}

		_output.WriteLine(string.Format(
			CultureInfo.InvariantCulture,
			"Operation (child process): peak working set {0:N0} bytes ({1:F1} MiB), working set before {2:N0} bytes; limit {3:N0} bytes",
			figures.PeakWorkingSetInBytes,
			figures.PeakWorkingSetInBytes / _BYTES_PER_MEBIBYTE,
			figures.WorkingSetBeforeInBytes,
			_MEMORY_LIMIT_IN_BYTES));

		Assert.True(figures.PeakWorkingSetInBytes < _MEMORY_LIMIT_IN_BYTES, "Generating the prompt pack passed 1 GiB of working set.");
	}

	/// <summary>The generation as the child process runs it: scope, then the pack through the atomic writer.</summary>
	internal static OperationFigures GenerateThePackAtTheFindingsCap(string[] arguments)
	{
		using TemporaryFolder repositoryFolder = TemporaryFolder.Create();
		using TemporaryFolder destinationFolder = TemporaryFolder.Create();

		AtomicArtifactWriter writer = ReportAppendFixture.BuildWriter(repositoryFolder, destinationFolder);
		ImmutableArray<Finding> findings = BuildFindings(_FINDING_COUNT);

		PeakWorkingSetSampler.ReleaseUnusedMemory();

		PeakWorkingSetSampler sampler = PeakWorkingSetSampler.Start();
		long workingSetBefore = Environment.WorkingSet;

		PromptPackScope scope = new(findings, ConfidenceLevel.Low, PromptPackFixture.Catalog);
		ArtifactWriteResult result = writer.WritePromptPackStreaming(
			AgentEnvironmentCatalog.CLAUDE_CODE_PACK_STEM,
			sink => PromptPackWriter.WriteBody(sink, AgentEnvironment.ClaudeCode, scope, PromptPackFixture.REPORT_FILE_NAME));

		OperationFigures figures = new(TimeSpan.Zero, sampler.StopAndReadPeakInBytes(), workingSetBefore);
		long length = new FileInfo(destinationFolder.Combine(result.FileNameWritten)).Length;

		Assert.True(result.Wrote);
		Assert.Equal(_FINDING_COUNT, scope.TotalCount);

		Console.WriteLine(AiAssistTexts.EntryCountLine(scope));
		Console.WriteLine(string.Format(CultureInfo.InvariantCulture, "Prompt pack size (bytes): {0:N0}", length));
		Console.WriteLine(string.Format(CultureInfo.InvariantCulture, "Prompt pack size (MiB): {0:F2}", length / _BYTES_PER_MEBIBYTE));

		return figures;
	}

	private static ImmutableArray<Finding> BuildFindings(int count)
	{
		ImmutableArray<Finding> template = PromptPackFixture.Findings();
		ImmutableArray<Finding>.Builder builder = ImmutableArray.CreateBuilder<Finding>(count);

		for (int index = 1; index <= count; index++)
		{
			builder.Add(template[(index - 1) % template.Length] with
			{
				SequentialIndex = index,
				Identifier = FindingIdentifiers.Of(index)
			});
		}

		return builder.MoveToImmutable();
	}
}
