using System.Diagnostics;
using System.Globalization;

using MigrateAuth.Cli.Tests.Agent;
using MigrateAuth.Cli.Tests.Writing;

namespace MigrateAuth.Cli.Tests.AcceptanceHarness;

/// <summary>The duration and peak working set of one measured operation, and the working set it started from.</summary>
internal readonly record struct OperationFigures(TimeSpan Duration, long PeakWorkingSetInBytes, long WorkingSetBeforeInBytes);

/// <summary>
/// THE VOLUME PROOFS THAT ASSERT A WORKING SET UNDER 1 GB MEASURE IN A PROCESS THAT HAS DONE NOTHING ELSE.
///
/// The working set is the process's, not the operation's. Measured inside the test host, an operation is measured beside
/// everything the earlier tests left resident: managed memory the collector keeps, and on Linux the native allocator's arenas,
/// which no collection returns.
///
/// So the test assembly starts itself as a child process with one operation named on the command line. The child builds the
/// operation's inputs, measures the operation with <see cref="PeakWorkingSetSampler"/>, checks its result, and prints the figures
/// on one line; a failed check ends the child with an error, which the caller raises.
/// </summary>
internal static class VolumeProofChildProcess
{
	internal const string SESSION_STATE_SAVE_OPERATION = "session-state-save";
	internal const string ORDERING_SORT_OPERATION = "ordering-sort";
	internal const string ORDERING_ADMISSION_OPERATION = "ordering-admission";
	internal const string REPORT_SUMMARY_RENDER_OPERATION = "report-summary-render";

	private const string _OPERATION_OPTION = "--volume-proof-operation";
	private const string _FIGURES_LINE_PREFIX = "volume-proof-figures";
	private const char _FIGURES_SEPARATOR = ' ';
	private const int _FIGURES_FIELD_COUNT = 4;
	private const int _TIMEOUT_IN_SECONDS = 600;
	private const int _SUCCESS_EXIT_CODE = 0;
	private const int _UNKNOWN_OPERATION_EXIT_CODE = 2;
	private const int _OPTION_AND_OPERATION_ARGUMENT_COUNT = 2;

	private static readonly Dictionary<string, Func<string[], OperationFigures>> _operations = new(StringComparer.Ordinal)
	{
		[SESSION_STATE_SAVE_OPERATION] = SessionStateVolumeProofTests.SaveTheLargestState,
		[ORDERING_SORT_OPERATION] = FindingsCapVolumeProofTests.SortHalfAMillionPaths,
		[ORDERING_ADMISSION_OPERATION] = FindingsCapVolumeProofTests.AdmitHalfAMillionFiles,
		[REPORT_SUMMARY_RENDER_OPERATION] = ReportSummaryVolumeProofTests.RenderTheFullReport,
		[ReportAppendVolumeProofTests.APPEND_OPERATION] = ReportAppendVolumeProofTests.AppendToTheLargestReport,
		[ReportAppendVolumeProofTests.PACK_STREAMING_OPERATION] = ReportAppendVolumeProofTests.WriteTheLargestPackBody,
		[PromptPackVolumeProofTests.PACK_GENERATION_OPERATION] = PromptPackVolumeProofTests.GenerateThePackAtTheFindingsCap
	};

	/// <summary>Runs one operation in a child process and returns the figures it reported.</summary>
	internal static OperationFigures Run(string operation, params string[] operationArguments)
	{
		return RunAndReadDetails(operation, operationArguments).Figures;
	}

	/// <summary>
	/// Runs one operation in a child process and returns the figures it reported together with every other line it printed, so a
	/// proof that records more than the working set can put the child's measurements in its own test output.
	/// </summary>
	internal static (OperationFigures Figures, IReadOnlyList<string> Details) RunAndReadDetails(string operation, params string[] operationArguments)
	{
		ProcessStartInfo startInfo = BuildStartInfo(operation, operationArguments);

		using ChildPathReplacement pathReplacement = ChildPathReplacement.Apply(startInfo, agentFolderPath: null);
		using Process child = Process.Start(startInfo)
			?? throw new InvalidOperationException("The volume-proof process could not be started.");

		Task<string> standardOutput = child.StandardOutput.ReadToEndAsync();
		Task<string> standardError = child.StandardError.ReadToEndAsync();

		if (!child.WaitForExit(TimeSpan.FromSeconds(_TIMEOUT_IN_SECONDS)))
		{
			child.Kill(entireProcessTree: true);

			throw new TimeoutException($"The volume-proof operation '{operation}' did not finish in time.");
		}

		child.WaitForExit();

		if (child.ExitCode != _SUCCESS_EXIT_CODE)
		{
			throw new InvalidOperationException(
				$"The volume-proof operation '{operation}' ended with exit code {child.ExitCode}:{Environment.NewLine}{standardError.Result}");
		}

		return (ParseFigures(operation, standardOutput.Result), ReadDetails(standardOutput.Result));
	}

	/// <summary>The child's side: runs the operation the command line names and prints its figures.</summary>
	internal static int RunRequestedOperation(string[] arguments)
	{
		int exitCode = _UNKNOWN_OPERATION_EXIT_CODE;

		if (arguments.Length >= _OPTION_AND_OPERATION_ARGUMENT_COUNT
			&& string.Equals(arguments[0], _OPERATION_OPTION, StringComparison.Ordinal)
			&& _operations.TryGetValue(arguments[1], out Func<string[], OperationFigures>? operation))
		{
			OperationFigures figures = operation(arguments[_OPTION_AND_OPERATION_ARGUMENT_COUNT..]);

			Console.WriteLine(FormatFigures(figures));
			exitCode = _SUCCESS_EXIT_CODE;
		}

		return exitCode;
	}

	private static ProcessStartInfo BuildStartInfo(string operation, string[] operationArguments)
	{
		ProcessStartInfo startInfo = new()
		{
			FileName = CliProcessHarness.HostPath,
			WorkingDirectory = AppContext.BaseDirectory,
			RedirectStandardOutput = true,
			RedirectStandardError = true,
			UseShellExecute = false,
			CreateNoWindow = true
		};

		startInfo.ArgumentList.Add(typeof(VolumeProofChildProcess).Assembly.Location);
		startInfo.ArgumentList.Add(_OPERATION_OPTION);
		startInfo.ArgumentList.Add(operation);

		foreach (string argument in operationArguments)
		{
			startInfo.ArgumentList.Add(argument);
		}

		return startInfo;
	}

	private static string FormatFigures(OperationFigures figures)
	{
		return string.Join(
			_FIGURES_SEPARATOR,
			_FIGURES_LINE_PREFIX,
			figures.Duration.Ticks.ToString(CultureInfo.InvariantCulture),
			figures.PeakWorkingSetInBytes.ToString(CultureInfo.InvariantCulture),
			figures.WorkingSetBeforeInBytes.ToString(CultureInfo.InvariantCulture));
	}

	private static IReadOnlyList<string> ReadDetails(string standardOutput)
	{
		return
		[
			.. standardOutput
				.Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
				.Where(line => !line.StartsWith(_FIGURES_LINE_PREFIX + _FIGURES_SEPARATOR, StringComparison.Ordinal))
		];
	}

	private static OperationFigures ParseFigures(string operation, string standardOutput)
	{
		string[] fields = standardOutput
			.Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
			.Where(line => line.StartsWith(_FIGURES_LINE_PREFIX + _FIGURES_SEPARATOR, StringComparison.Ordinal))
			.Select(line => line.Split(_FIGURES_SEPARATOR))
			.SingleOrDefault(candidate => candidate.Length == _FIGURES_FIELD_COUNT)
			?? throw new InvalidOperationException($"The volume-proof operation '{operation}' printed no figures:{Environment.NewLine}{standardOutput}");

		return new OperationFigures(
			TimeSpan.FromTicks(long.Parse(fields[1], CultureInfo.InvariantCulture)),
			long.Parse(fields[2], CultureInfo.InvariantCulture),
			long.Parse(fields[3], CultureInfo.InvariantCulture));
	}
}
