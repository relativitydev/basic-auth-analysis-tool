using MigrateAuth.Cli.Session;

using MigrateAuth.Cli.Tests.Reading;
using MigrateAuth.Cli.Tests.Session;

namespace MigrateAuth.Cli.Tests.AcceptanceHarness;

/// <summary>
/// ONE SESSION, RUN ONCE, ASSERTED SEVERAL TIMES.
///
/// The zero-candidate acceptance condition is a claim about a single run - "completes all four stages AND produces a valid report
/// stating zero findings, WITH exit code 0" - so the run happens once in this fixture and the tests
/// that grade it each take one clause of that sentence. Running the process once per assertion
/// would grade four different runs and would still not establish that any one of them satisfied all
/// four clauses at the same time.
///
/// ONE ENVIRONMENTAL DEPENDENCY, DECLARED RATHER THAN DEFENDED AGAINST. The effective configuration
/// is resolved from the user profile as well as the repository, and a process cannot be
/// told to look elsewhere without a mechanism this fixture has no reason to build. A developer whose
/// profile holds a <c>.migrate-auth.json</c> that disables every language would see these tests fail
/// with exit code 6 - correctly, because that configuration refuses a session. Nothing else in a
/// profile file can affect what is asserted here: a narrower threshold changes which findings are
/// reported, and this repository produces none.
///
/// THE REPOSITORY IS ZERO-CANDIDATE RATHER THAN EMPTY. It holds a source file the embedded catalog
/// attributes to nothing and a <c>CSharp</c> file carrying no anchor, so
/// the walk enumerates, the inventory counts and attributes, and preselection admits neither. That
/// is the condition proper; the emptier form - a repository with no files at all - is graded
/// separately, because the two exercise different code.
/// </summary>
public sealed class ZeroCandidateProcessSession : IDisposable
{
	/// <summary>A file of a language the embedded catalog does not recognize at all.</summary>
	internal const string UNATTRIBUTED_FILE_NAME = "ordinary.rb";

	/// <summary>A file the embedded <c>CSharp</c> analyzer DOES attribute, carrying nothing it would select on.</summary>
	internal const string ATTRIBUTED_FILE_NAME = "Ordinary.cs";

	/// <summary>Content with no preselection anchor of any embedded analyzer in it.</summary>
	internal const string UNREMARKABLE_CONTENT = "public sealed class Ordinary\n{\n}\n";

	private readonly RepositoryFixture _fixture;
	private readonly TemporaryFolder _destination;

	public ZeroCandidateProcessSession()
	{
		_fixture = RepositoryFixture.CreateEmpty();
		_fixture.WriteText(UNATTRIBUTED_FILE_NAME, UNREMARKABLE_CONTENT);
		_fixture.WriteText(ATTRIBUTED_FILE_NAME, UNREMARKABLE_CONTENT);

		_destination = TemporaryFolder.Create();

		Outcome = CliProcessHarness.Run(
			_fixture.RootPath,
			_destination.Path,
			CliProcessHarness.TheFourStagesThenExit);
	}

	/// <summary>What the process reported, including the exit code only a process can carry.</summary>
	internal CliProcessOutcome Outcome { get; }

	/// <summary>Where the session was told to write. Read as an outsider would read it.</summary>
	internal string DestinationPath => _destination.Path;

	/// <summary>The audited repository, for finding the state document the session persisted.</summary>
	internal RepositoryFolderPath Repository => _fixture.Repository;

	public void Dispose()
	{
		_destination.Dispose();
		_fixture.Dispose();
	}
}
