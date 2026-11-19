using System.Text;

namespace MigrateAuth.Cli.Tests.MatchBoundStress;

/// <summary>Writes the timing table of a `Python` timing test to the file named by an environment variable, when the variable is set.</summary>
internal static class PythonTimingReportWriter
{
	internal static void WriteWhereRequested(string reportPathVariableName, string report)
	{
		string? reportPath = Environment.GetEnvironmentVariable(reportPathVariableName);

		if (!string.IsNullOrWhiteSpace(reportPath))
		{
			File.WriteAllText(reportPath, report, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
		}
	}
}
