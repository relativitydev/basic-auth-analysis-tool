using MigrateAuth.Cli.Reading;

namespace MigrateAuth.Cli.Tests.Reading;

/// <summary>
/// THE WALK'S EXCLUSION RULE APPLIED TO A PATH ON DISK, for the census and dump harnesses that enumerate a
/// folder themselves and must skip what the walk does not enter. A directory segment is excluded by its
/// name, or - for <c>bin</c> alone - when the folder that holds it lists a compiled project file, which is
/// read from the names of that folder and nothing else.
/// </summary>
internal static class WalkExclusionEmulation
{
	private const char _PATH_SEPARATOR = '/';
	private const string _GIT_DIRECTORY_PREFIX = ".git/";

	internal static bool IsInExcludedFolder(string rootFolder, string relativePath)
	{
		string[] segments = relativePath.Split(_PATH_SEPARATOR);
		string[] directories = segments[..^1];
		bool excluded = relativePath.StartsWith(_GIT_DIRECTORY_PREFIX, StringComparison.Ordinal);

		for (int index = 0; index < directories.Length && !excluded; index++)
		{
			excluded = WalkExclusions.IsExcluded(directories[index]) || IsBuildOutputBin(rootFolder, directories, index);
		}

		return excluded;
	}

	private static bool IsBuildOutputBin(string rootFolder, string[] directories, int index)
	{
		bool buildOutput = false;

		if (string.Equals(directories[index], WalkExclusions.CONDITIONALLY_EXCLUDED_DIRECTORY_NAME, StringComparison.Ordinal))
		{
			string parentFolder = Path.Combine([rootFolder, .. directories[..index]]);

			buildOutput = WalkExclusions.IsBuildOutputBin(directories[index], new DirectoryInfo(parentFolder).GetFileSystemInfos());
		}

		return buildOutput;
	}
}
