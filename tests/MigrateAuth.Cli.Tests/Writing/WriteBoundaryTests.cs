using MigrateAuth.Cli.Session;
using MigrateAuth.Cli.Tests.Session;
using MigrateAuth.Cli.Writing;

using Xunit;

namespace MigrateAuth.Cli.Tests.Writing;

/// <summary>
/// The boundary that keeps the tool out of the repository it audits. Three rejection cases,
/// and the cases a string prefix would get wrong.
///
/// The case-insensitivity assertions compare with ordinal case folding against the invariant
/// culture and call no platform API, so their result cannot vary with the host. What they do not
/// cover is the FILESYSTEM's behaviour around it - whether a given volume would have created two
/// folders differing only in case for the fold to matter.
/// </summary>
public sealed class WriteBoundaryTests
{
	private const string _OUTPUT_FOLDER_NAME = "output";
	private const string _NESTED_FOLDER_NAME = "docs";
	private const string _PARENT_SEGMENT = "..";

	[Fact]
	public void TheRefusalExitCodeIsFour()
	{
		Assert.Equal(ExitCode.OutputDestinationNotWritable, WriteBoundary.REFUSAL_EXIT_CODE);
		Assert.Equal(4, (int)WriteBoundary.REFUSAL_EXIT_CODE);
	}

	[Fact]
	public void ADestinationInsideTheRepositoryIsRefused()
	{
		using TemporaryFolder repositoryFolder = TemporaryFolder.Create();

		string destinationPath = Path.Combine(repositoryFolder.Path, _NESTED_FOLDER_NAME, _OUTPUT_FOLDER_NAME);
		WriteBoundaryVerdict verdict = Evaluate(repositoryFolder.Path, destinationPath);

		Assert.False(verdict.PermitsWriting);
		Assert.Equal(PathOverlap.DestinationInsideRepository, verdict.Overlap);
	}

	[Fact]
	public void ARepositoryInsideTheDestinationIsRefused()
	{
		using TemporaryFolder destinationFolder = TemporaryFolder.Create();

		string repositoryPath = Path.Combine(destinationFolder.Path, "checkout");

		Directory.CreateDirectory(repositoryPath);

		WriteBoundaryVerdict verdict = Evaluate(repositoryPath, destinationFolder.Path);

		Assert.False(verdict.PermitsWriting);
		Assert.Equal(PathOverlap.RepositoryInsideDestination, verdict.Overlap);
	}

	/// <summary>
	/// The ordinary case: the default destination is the working
	/// directory, and the obvious way to run this tool is from inside the repository.
	/// </summary>
	[Fact]
	public void ADestinationThatCoincidesWithTheRepositoryIsRefused()
	{
		using TemporaryFolder repositoryFolder = TemporaryFolder.Create();

		WriteBoundaryVerdict verdict = Evaluate(repositoryFolder.Path, repositoryFolder.Path);

		Assert.False(verdict.PermitsWriting);
		Assert.Equal(PathOverlap.SamePath, verdict.Overlap);
	}

	[Fact]
	public void TwoUnrelatedFoldersArePermitted()
	{
		using TemporaryFolder repositoryFolder = TemporaryFolder.Create();
		using TemporaryFolder destinationFolder = TemporaryFolder.Create();

		WriteBoundaryVerdict verdict = Evaluate(repositoryFolder.Path, destinationFolder.Path);

		Assert.True(verdict.PermitsWriting);
		Assert.Equal(PathOverlap.Separate, verdict.Overlap);
		Assert.Equal(string.Empty, verdict.RefusalMessage);
	}

	/// <summary>
	/// The first thing a string prefix gets wrong. <c>repository-backup</c> starts with
	/// <c>repository</c> as text and is a sibling as a path.
	/// </summary>
	[Fact]
	public void ASiblingWhoseNameStartsWithTheRepositorysIsPermitted()
	{
		using TemporaryFolder parentFolder = TemporaryFolder.Create();

		string repositoryPath = Path.Combine(parentFolder.Path, "repository");
		string destinationPath = Path.Combine(parentFolder.Path, "repository-backup");

		Directory.CreateDirectory(repositoryPath);
		Directory.CreateDirectory(destinationPath);

		Assert.True(Evaluate(repositoryPath, destinationPath).PermitsWriting);
	}

	/// <summary>
	/// The second thing a string prefix gets wrong, and the one the fold exists for.
	/// </summary>
	[Fact]
	public void ADestinationDifferingOnlyInCaseIsStillInsideTheRepository()
	{
		using TemporaryFolder repositoryFolder = TemporaryFolder.Create();

		string destinationPath = Path.Combine(repositoryFolder.Path.ToUpperInvariant(), _OUTPUT_FOLDER_NAME);
		WriteBoundaryVerdict verdict = Evaluate(repositoryFolder.Path, destinationPath);

		Assert.False(verdict.PermitsWriting);
		Assert.Equal(PathOverlap.DestinationInsideRepository, verdict.Overlap);
	}

	[Fact]
	public void ACoincidenceDifferingOnlyInCaseIsStillACoincidence()
	{
		using TemporaryFolder repositoryFolder = TemporaryFolder.Create();

		WriteBoundaryVerdict verdict = Evaluate(repositoryFolder.Path, repositoryFolder.Path.ToUpperInvariant());

		Assert.Equal(PathOverlap.SamePath, verdict.Overlap);
	}

	/// <summary>The third: a traversal that lands back inside the tree.</summary>
	[Fact]
	public void ATraversalThatReturnsInsideTheRepositoryIsRefused()
	{
		using TemporaryFolder repositoryFolder = TemporaryFolder.Create();

		string destinationPath = Path.Combine(
			repositoryFolder.Path,
			_NESTED_FOLDER_NAME,
			_PARENT_SEGMENT,
			_OUTPUT_FOLDER_NAME);

		Assert.Equal(PathOverlap.DestinationInsideRepository, Evaluate(repositoryFolder.Path, destinationPath).Overlap);
	}

	[Fact]
	public void ATraversalThatLeavesTheRepositoryIsPermitted()
	{
		using TemporaryFolder repositoryFolder = TemporaryFolder.Create();

		string destinationPath = Path.Combine(repositoryFolder.Path, _PARENT_SEGMENT, "elsewhere-for-this-test");

		Assert.True(Evaluate(repositoryFolder.Path, destinationPath).PermitsWriting);
	}

	/// <summary>
	/// The fourth: a directory link whose target lands inside the tree. Canonicalization
	/// follows links along the WHOLE path, not only at its last component, which is what makes
	/// this refusable at all.
	/// </summary>
	[Fact]
	public void ADestinationReachedThroughADirectoryLinkIntoTheRepositoryIsRefused()
	{
		using TemporaryFolder repositoryFolder = TemporaryFolder.Create();
		using TemporaryFolder linkHostFolder = TemporaryFolder.Create();

		string linkPath = Path.Combine(linkHostFolder.Path, "shortcut");

		DirectoryLink.Create(linkPath, repositoryFolder.Path);

		string destinationPath = Path.Combine(linkPath, _OUTPUT_FOLDER_NAME);
		WriteBoundaryVerdict verdict = Evaluate(repositoryFolder.Path, destinationPath);

		Assert.False(verdict.PermitsWriting);
		Assert.Equal(PathOverlap.DestinationInsideRepository, verdict.Overlap);
	}

	[Fact]
	public void ATrailingSeparatorChangesNothing()
	{
		using TemporaryFolder repositoryFolder = TemporaryFolder.Create();

		string destinationPath = $"{repositoryFolder.Path}{Path.DirectorySeparatorChar}";

		Assert.Equal(PathOverlap.SamePath, Evaluate(repositoryFolder.Path, destinationPath).Overlap);
	}

	/// <summary>
	/// Every refusal names both locations by label and never by path, because no
	/// absolute path may appear in anything the product emits - a refusal message included. It also says what
	/// to do, because the collision is what happens on the most natural invocation there is.
	/// </summary>
	[Theory]
	[InlineData(PathOverlap.SamePath)]
	[InlineData(PathOverlap.DestinationInsideRepository)]
	[InlineData(PathOverlap.RepositoryInsideDestination)]
	public void EveryRefusalNamesLabelsAndNeverPathsAndSaysWhatToDo(PathOverlap expectedOverlap)
	{
		using TemporaryFolder repositoryFolder = TemporaryFolder.Create();

		(string repositoryPath, string destinationPath) = BuildOverlappingPair(repositoryFolder.Path, expectedOverlap);
		WriteBoundaryVerdict verdict = Evaluate(repositoryPath, destinationPath);

		Assert.Equal(expectedOverlap, verdict.Overlap);
		Assert.DoesNotContain(repositoryFolder.Path, verdict.RefusalMessage, StringComparison.OrdinalIgnoreCase);
		Assert.Contains(WriteBoundary.REFUSAL_REMEDY_MESSAGE, verdict.RefusalMessage, StringComparison.Ordinal);
	}

	/// <summary>
	/// The primitive's other call site. The primitive is callable with nothing but two folder paths,
	/// before any stage has run and against a destination that does not exist yet - which is
	/// what Pre-flight Validation needs, and is why it is not entangled with write machinery.
	/// </summary>
	[Fact]
	public void ThePrimitiveAnswersForADestinationThatHasNotBeenCreatedYet()
	{
		using TemporaryFolder repositoryFolder = TemporaryFolder.Create();

		string destinationPath = Path.Combine(repositoryFolder.Path, "not-created-yet");

		Assert.False(Directory.Exists(destinationPath));
		Assert.Equal(PathOverlap.DestinationInsideRepository, Evaluate(repositoryFolder.Path, destinationPath).Overlap);
	}

	[Fact]
	public void TheReadPathPermitsACandidateInsideTheRepository()
	{
		using TemporaryFolder repositoryFolder = TemporaryFolder.Create();

		RepositoryFolderPath repository = RepositoryFolderPath.FromSuppliedText(repositoryFolder.Path);

		Assert.True(WriteBoundary.PermitsReading(repository, Path.Combine("src", "Client.cs")));
		Assert.True(WriteBoundary.PermitsReading(repository, Path.Combine(repositoryFolder.Path, "src", "Client.cs")));
	}

	/// <summary>
	/// The state file is untrusted input and a path escaping the repository root is
	/// schema-valid, so analysis would otherwise open it. Every candidate goes through the same
	/// primitive before anything is opened.
	/// </summary>
	[Theory]
	[InlineData("")]
	[InlineData("   ")]
	[InlineData("../outside.cs")]
	[InlineData("src/../../outside.cs")]
	public void TheReadPathRefusesACandidateThatEscapesTheRepository(string candidatePath)
	{
		using TemporaryFolder repositoryFolder = TemporaryFolder.Create();

		RepositoryFolderPath repository = RepositoryFolderPath.FromSuppliedText(repositoryFolder.Path);

		Assert.False(WriteBoundary.PermitsReading(repository, candidatePath));
	}

	[Fact]
	public void TheReadPathRefusesAnAbsoluteCandidateElsewhere()
	{
		using TemporaryFolder repositoryFolder = TemporaryFolder.Create();
		using TemporaryFolder otherFolder = TemporaryFolder.Create();

		RepositoryFolderPath repository = RepositoryFolderPath.FromSuppliedText(repositoryFolder.Path);

		Assert.False(WriteBoundary.PermitsReading(repository, Path.Combine(otherFolder.Path, "outside.cs")));
	}

	[Fact]
	public void TheReadPathRefusesTheRepositoryRootItself()
	{
		using TemporaryFolder repositoryFolder = TemporaryFolder.Create();

		RepositoryFolderPath repository = RepositoryFolderPath.FromSuppliedText(repositoryFolder.Path);

		Assert.False(WriteBoundary.PermitsReading(repository, repositoryFolder.Path));
	}

	private static (string RepositoryPath, string DestinationPath) BuildOverlappingPair(
		string temporaryPath,
		PathOverlap overlap)
	{
		string nestedPath = Path.Combine(temporaryPath, _NESTED_FOLDER_NAME);

		Directory.CreateDirectory(nestedPath);

		(string RepositoryPath, string DestinationPath) pair = overlap switch
		{
			PathOverlap.SamePath => (temporaryPath, temporaryPath),
			PathOverlap.DestinationInsideRepository => (temporaryPath, nestedPath),
			_ => (nestedPath, temporaryPath)
		};

		return pair;
	}

	private static WriteBoundaryVerdict Evaluate(string repositoryPath, string destinationPath) =>
		WriteBoundary.Evaluate(
			RepositoryFolderPath.FromSuppliedText(repositoryPath),
			DocumentationDestinationFolderPath.FromSuppliedText(destinationPath));
}
