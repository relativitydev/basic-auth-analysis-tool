using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Reporting;
using MigrateAuth.Cli.Session;

using Xunit;

using MigrateAuth.Cli.Tests.Fixtures;

namespace MigrateAuth.Cli.Tests.Session;

/// <summary>
/// A SAVE ONTO A STATE FILE THAT ANOTHER PROCESS HOLDS OPEN FOR A MOMENT MUST NOT BE LOST.
///
/// The rename over the final name fails with a sharing violation while something else (an indexer,
/// a scanner) holds the file just written. The store retries the rename briefly, so a hold that clears
/// is absorbed and a hold that does not clear still fails. On a platform whose rename ignores an open
/// handle neither hold matters and the save succeeds.
/// </summary>
public sealed class SessionStateSaveContentionTests
{
	private const string _STATE_FILE_SEARCH_PATTERN = "migrate-auth.session.*";
	private const string _TEMPORARY_FILE_SUFFIX = ".writing";
	private const int _HOLD_THAT_CLEARS_IN_MILLISECONDS = 100;
	private const int _HOLD_THAT_NEVER_CLEARS_IN_MILLISECONDS = 4_000;

	/// <summary>A hold that clears within the retry window leaves the new state persisted.</summary>
	[Fact]
	public void ASaveOntoAFileHeldBrieflyIsPersistedOnceTheHoldClears()
	{
		using TemporaryFolder destination = TemporaryFolder.Create();
		using TemporaryFolder repositoryFolder = TemporaryFolder.Create();
		(SessionStateStore store, RepositoryFolderPath repository, LanguageCatalog catalog) = Arrange(destination, repositoryFolder);
		store.Save(repository, SessionState.Fresh(catalog.Version, repository.Label));
		string statePath = Directory.GetFiles(destination.Path, _STATE_FILE_SEARCH_PATTERN).Single(path => !path.EndsWith(_TEMPORARY_FILE_SUFFIX, StringComparison.Ordinal));

		using (HoldFor(statePath, _HOLD_THAT_CLEARS_IN_MILLISECONDS))
		{
			store.Save(repository, SessionState.Fresh(catalog.Version, repository.Label));
		}

		Assert.Equal(SessionStateRejectionReason.None, store.Load(repository, catalog).RejectionReason);
		Assert.Empty(Directory.GetFiles(destination.Path, "*" + _TEMPORARY_FILE_SUFFIX));
	}

	/// <summary>A hold that outlasts the retries fails, and leaves no temporary file behind.</summary>
	[Fact]
	public void ASaveOntoAFileThatStaysHeldFailsAsBeforeAndLeavesNoTemporary()
	{
		using TemporaryFolder destination = TemporaryFolder.Create();
		using TemporaryFolder repositoryFolder = TemporaryFolder.Create();
		(SessionStateStore store, RepositoryFolderPath repository, LanguageCatalog catalog) = Arrange(destination, repositoryFolder);
		store.Save(repository, SessionState.Fresh(catalog.Version, repository.Label));
		string statePath = Directory.GetFiles(destination.Path, _STATE_FILE_SEARCH_PATTERN).Single(path => !path.EndsWith(_TEMPORARY_FILE_SUFFIX, StringComparison.Ordinal));

		using (HoldFor(statePath, _HOLD_THAT_NEVER_CLEARS_IN_MILLISECONDS))
		{
			if (OperatingSystem.IsWindows())
			{
				Assert.ThrowsAny<Exception>(() => store.Save(repository, SessionState.Fresh(catalog.Version, repository.Label)));
			}
			else
			{
				store.Save(repository, SessionState.Fresh(catalog.Version, repository.Label));
			}
		}

		Assert.Empty(Directory.GetFiles(destination.Path, "*" + _TEMPORARY_FILE_SUFFIX));
	}

	private static (SessionStateStore Store, RepositoryFolderPath Repository, LanguageCatalog Catalog) Arrange(TemporaryFolder destination, TemporaryFolder repositoryFolder)
	{
		return (
			new SessionStateStore(DocumentationDestinationFolderPath.FromSuppliedText(destination.Path)),
			RepositoryFolderPath.FromSuppliedText(repositoryFolder.Path),
			ExemplarFixtureCatalog.Build());
	}

	/// <summary>
	/// Opens the file with no sharing and releases it after the given time, or when disposed if that comes first. A dedicated thread
	/// does the waiting, so a thread pool busy with other tests cannot delay the release.
	/// </summary>
	private static IDisposable HoldFor(string path, int milliseconds)
	{
		FileStream held = new(path, FileMode.Open, FileAccess.Read, FileShare.None);
		ManualResetEventSlim disposed = new(false);
		Thread release = new(() =>
		{
			disposed.Wait(milliseconds);
			held.Dispose();
		})
		{
			IsBackground = true
		};
		release.Start();

		return new Hold(held, disposed, release);
	}

	private sealed class Hold(FileStream held, ManualResetEventSlim disposed, Thread release) : IDisposable
	{
		public void Dispose()
		{
			disposed.Set();
			release.Join();
			held.Dispose();
			disposed.Dispose();
		}
	}
}
