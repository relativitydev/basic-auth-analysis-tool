using MigrateAuth.Cli.Emission;
using MigrateAuth.Cli.Session;
using MigrateAuth.Cli.Tests.Session;

using Xunit;

namespace MigrateAuth.Cli.Tests.Agent;

/// <summary>
/// SETTLES WHETHER A TAG CHARACTER (A SURROGATE PAIR OF THE FORMAT CATEGORY) PASSES THE CONSOLE FILTER AND THE EMITTED PATH
/// TEXT. RESULT: IT PASSES BOTH, UNCHANGED.
///
/// The console filter looks at one UTF-16 unit at a time and removes a unit that is a control or of the format category. A
/// tag character such as U+E0041 is held as two surrogate units, and a surrogate unit is neither, so the pair survives. A
/// formatting character in the basic plane, such as U+200B, is removed, which the control case below shows so the gap is
/// not mistaken for a broken filter. The gap is recorded here and not closed: the four assessment stages' output has to
/// stay byte-identical, and the pack writer's own per-rune escaping (which shows the character as a visible escape) is what
/// protects the pack.
/// </summary>
public sealed class ConsoleTextSafetyTagCharacterGapTests
{
	private const string _TAG_LATIN_SMALL_A = "\U000E0041";
	private const string _ZERO_WIDTH_SPACE = "\u200B";
	private const string _FILE_NAME_FORMAT = "name{0}.cs";

	[Fact]
	public void ATagCharacterSurrogatePairPassesTheConsoleFilterUnchangedWhileABasicPlaneFormatCharacterIsRemoved()
	{
		string withTag = string.Format(System.Globalization.CultureInfo.InvariantCulture, _FILE_NAME_FORMAT, _TAG_LATIN_SMALL_A);
		string withZeroWidthSpace = string.Format(System.Globalization.CultureInfo.InvariantCulture, _FILE_NAME_FORMAT, _ZERO_WIDTH_SPACE);

		Assert.Equal(withTag, ConsoleTextSafety.StripUnsafeCharacters(withTag));
		Assert.Equal("name.cs", ConsoleTextSafety.StripUnsafeCharacters(withZeroWidthSpace));
	}

	[Theory]
	[InlineData(EmissionDestination.Terminal)]
	[InlineData(EmissionDestination.Markdown)]
	[InlineData(EmissionDestination.StructuredData)]
	public void ATagCharacterInAPathPassesTheEmittedPathTextForEveryDestinationUnchanged(EmissionDestination destination)
	{
		using TemporaryFolder repositoryFolder = TemporaryFolder.Create();

		RepositoryFolderPath repository = RepositoryFolderPath.FromSuppliedText(repositoryFolder.Path);
		string name = string.Format(System.Globalization.CultureInfo.InvariantCulture, _FILE_NAME_FORMAT, _TAG_LATIN_SMALL_A);

		Assert.True(RepositoryRelativePath.TryFromScannedPath(repository, Path.Combine(repositoryFolder.Path, name), out RepositoryRelativePath path));
		Assert.Equal(name, EmittedPathText.ForDestination(path, destination));
	}
}
