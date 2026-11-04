using System.Globalization;

namespace MigrateAuth.WorstCase;

/// <summary>
/// Command line for the generator: <c>--output &lt;folder&gt; [--files &lt;n&gt;] [--seed &lt;n&gt;] [--oversize-files &lt;n&gt;] [--powershell-basis-points &lt;n&gt;] [--vbnet-basis-points &lt;n&gt;] [--shell-basis-points &lt;n&gt;]</c>.
/// </summary>
internal static class Program
{
	private const int _EXIT_OK = 0;
	private const int _EXIT_USAGE = 1;
	private const int _EXIT_REFUSED = 2;
	private const string _OUTPUT_OPTION = "--output";
	private const string _FILES_OPTION = "--files";
	private const string _SEED_OPTION = "--seed";
	private const string _OVERSIZE_OPTION = "--oversize-files";
	private const string _NO_SHARE_DEFAULT = "0";
	private const string _POWERSHELL_OPTION = "--powershell-basis-points";
	private const string _VBNET_OPTION = "--vbnet-basis-points";
	private const string _SHELL_OPTION = "--shell-basis-points";
	private const string _USAGE = "Usage: worst-case-generator --output <folder outside any repository> [--files <n, default 50000>] [--seed <n>] [--oversize-files <n, default 101>] [--powershell-basis-points <n of every 10,000 ordinary files, default 0>] [--vbnet-basis-points <n of every 10,000 ordinary files, default 0>] [--shell-basis-points <n of every 10,000 ordinary files, default 0>]";
	private const string _DONE_FORMAT = "Done: {0:N0} files, {1:N0} bytes";
	private const int _DEFAULT_OVERSIZE_FILE_COUNT = WorstCaseTreeOptions.RETAINED_NAMES_PER_CATEGORY + 1;

	private static int Main(string[] arguments)
	{
		int exitCode;

		try
		{
			WorstCaseTreeSummary summary = WorstCaseTreeGenerator.Generate(ReadOptions(ParseOptions(arguments)));

			Console.WriteLine(string.Format(CultureInfo.InvariantCulture, _DONE_FORMAT, summary.FilesWritten, summary.BytesWritten));
			exitCode = _EXIT_OK;
		}
		catch (ArgumentException exception)
		{
			exitCode = HandleRefused(exception);
		}
		catch (IOException exception)
		{
			exitCode = HandleRefused(exception);
		}
		catch (KeyNotFoundException)
		{
			exitCode = HandleUsage();
		}
		catch (FormatException)
		{
			exitCode = HandleUsage();
		}
		catch (OverflowException)
		{
			exitCode = HandleUsage();
		}

		return exitCode;
	}

	private static int HandleRefused(Exception exception)
	{
		Console.Error.WriteLine(exception.Message);
		return _EXIT_REFUSED;
	}

	private static int HandleUsage()
	{
		Console.Error.WriteLine(_USAGE);
		return _EXIT_USAGE;
	}

	private static WorstCaseTreeOptions ReadOptions(Dictionary<string, string> values)
	{
		return new WorstCaseTreeOptions
		{
			OutputRoot = values[_OUTPUT_OPTION],
			FileCount = int.Parse(values.GetValueOrDefault(_FILES_OPTION, WorstCaseTreeOptions.MINIMUM_SCENARIO_FILE_COUNT.ToString(CultureInfo.InvariantCulture)), CultureInfo.InvariantCulture),
			Seed = long.Parse(values.GetValueOrDefault(_SEED_OPTION, WorstCaseTreeOptions.DEFAULT_SEED.ToString(CultureInfo.InvariantCulture)), CultureInfo.InvariantCulture),
			OversizeFileCount = int.Parse(values.GetValueOrDefault(_OVERSIZE_OPTION, _DEFAULT_OVERSIZE_FILE_COUNT.ToString(CultureInfo.InvariantCulture)), CultureInfo.InvariantCulture),
			PowerShellScriptBasisPoints = int.Parse(values.GetValueOrDefault(_POWERSHELL_OPTION, _NO_SHARE_DEFAULT), CultureInfo.InvariantCulture),
			VBNetSourceBasisPoints = int.Parse(values.GetValueOrDefault(_VBNET_OPTION, _NO_SHARE_DEFAULT), CultureInfo.InvariantCulture),
			ShellScriptBasisPoints = int.Parse(values.GetValueOrDefault(_SHELL_OPTION, _NO_SHARE_DEFAULT), CultureInfo.InvariantCulture),
			ReportProgress = Console.WriteLine
		};
	}

	private static Dictionary<string, string> ParseOptions(string[] arguments)
	{
		Dictionary<string, string> values = [];

		for (int position = 0; position + 1 < arguments.Length; position += 2)
		{
			values[arguments[position]] = arguments[position + 1];
		}

		return values;
	}
}
