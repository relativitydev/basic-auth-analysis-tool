using MigrateAuth.Cli.Emission;
using MigrateAuth.Cli.Reading;
using MigrateAuth.Cli.Session;
using MigrateAuth.Cli.Tests.Session;

using Xunit;

namespace MigrateAuth.Cli.Tests.Reading;

/// <summary>
/// THE COUNT IS WHOLE AND THE LIST IS BOUNDED, because a repository of half a million files in a legacy
/// code page must hold a hundred names and not half a million.
/// </summary>
public sealed class LegacyEncodedFileTallyTests
{
	private const int _FILES_ABOVE_THE_CEILING = 150;
	private const string _FILE_NAME_FORMAT = "src/file{0:D4}.cs";

	[Fact]
	public void TheCountIsWholeAndTheNamedPathsAreTheFirstByOrdinalOrderUpToTheCeiling()
	{
		using TemporaryFolder repositoryFolder = TemporaryFolder.Create();
		RepositoryFolderPath repository = RepositoryFolderPath.FromSuppliedText(repositoryFolder.Path);
		LegacyEncodedFileTally tally = new();

		for (int number = _FILES_ABOVE_THE_CEILING; number >= 1; number--)
		{
			tally.Record(PathOf(repository, repositoryFolder, string.Format(System.Globalization.CultureInfo.InvariantCulture, _FILE_NAME_FORMAT, number)));
		}

		LegacyEncodedFileSummary summary = tally.ToSummary();

		Assert.Equal(_FILES_ABOVE_THE_CEILING, summary.OccurrenceCount);
		Assert.Equal(ReadingCeilings.ENUMERATED_SKIPPED_PATHS_PER_CATEGORY, summary.Paths.Length);
		Assert.Equal(_FILES_ABOVE_THE_CEILING - ReadingCeilings.ENUMERATED_SKIPPED_PATHS_PER_CATEGORY, summary.OmittedPathCount);
		Assert.Equal(
			summary.Paths.Select(path => path.Value).Order(StringComparer.Ordinal),
			summary.Paths.Select(path => path.Value));
		Assert.Equal(
			string.Format(System.Globalization.CultureInfo.InvariantCulture, _FILE_NAME_FORMAT, 1),
			summary.Paths[0].Value);
	}

	[Fact]
	public void ATallyThatRecordedNothingIsTheNoneSummary()
	{
		Assert.Equal(LegacyEncodedFileSummary.None, new LegacyEncodedFileTally().ToSummary());
	}

	private static RepositoryRelativePath PathOf(RepositoryFolderPath repository, TemporaryFolder folder, string relativePath)
	{
		Assert.True(RepositoryRelativePath.TryFromScannedPath(repository, Path.Combine(folder.Path, relativePath), out RepositoryRelativePath path));

		return path;
	}
}
