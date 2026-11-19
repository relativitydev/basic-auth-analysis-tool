namespace MigrateAuth.Cli.Tests.MatchBoundStress;

/// <summary>The constants that the `Python` draft tests share: the iteration counts of the timing procedures, the extension of a `Python` file and the opening of its line comment.</summary>
internal static class PythonTestDraftConstants
{
	internal const int WARM_UP_ITERATIONS = 1;

	internal const int MEASURED_ITERATIONS = 3;

	internal const string FILE_EXTENSION = ".py";

	internal const string LINE_COMMENT_OPENING = "#";
}
