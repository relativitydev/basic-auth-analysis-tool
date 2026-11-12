using System.Text.RegularExpressions;

using MigrateAuth.Cli.Agent;
using MigrateAuth.Cli.Tests.Session;

using Xunit;

namespace MigrateAuth.Cli.Tests.Agent;

/// <summary>
/// The launch copy layout: the folder is named by a fixed prefix and 32 hexadecimal digits, a new name is
/// unique, the predicted path has the shape of a real one and the same character profile apart from those
/// digits, and a created folder sits under the destination.
/// </summary>
public sealed partial class LaunchCopyLayoutTests
{
	private const string _EXPECTED_PREFIX = "ai-assist-launch-";
	private const int _EXPECTED_IDENTIFIER_LENGTH = 32;
	private const string _PACK_FILE_NAME = "review-pack.md";
	private const string _DESTINATION_FOLDER_NAME = "output folder";
	private const int _NAME_COUNT = 50;
	private const char _HEX_PLACEHOLDER = 'x';

	[GeneratedRegex("^ai-assist-launch-[0-9a-f]{32}$")]
	private static partial Regex LaunchFolderNameShape();

	[Fact]
	public void ANewFolderNameIsThePrefixAndThirtyTwoLowerCaseHexDigits()
	{
		string name = LaunchCopyLayout.NewFolderName();

		Assert.Matches(LaunchFolderNameShape(), name);
		Assert.StartsWith(_EXPECTED_PREFIX, name, StringComparison.Ordinal);
		Assert.Equal(_EXPECTED_PREFIX.Length + _EXPECTED_IDENTIFIER_LENGTH, name.Length);
	}

	[Fact]
	public void NewFolderNamesAreUnique()
	{
		HashSet<string> names = [];

		for (int index = 0; index < _NAME_COUNT; index++)
		{
			names.Add(LaunchCopyLayout.NewFolderName());
		}

		Assert.Equal(_NAME_COUNT, names.Count);
	}

	[Fact]
	public void ThePredictedFolderPathHasTheShapeOfARealOne()
	{
		string predicted = LaunchCopyLayout.PredictFolderPath(Path.Combine(Path.GetTempPath(), _DESTINATION_FOLDER_NAME));

		Assert.True(LaunchCopyLayout.HasLaunchFolderShape(Path.GetFileName(predicted)));
	}

	[Fact]
	public void ThePredictedCopyPathIsThePredictedFolderPlusThePackFileName()
	{
		string destination = Path.Combine(Path.GetTempPath(), _DESTINATION_FOLDER_NAME);

		string predicted = LaunchCopyLayout.PredictCopyPath(destination, _PACK_FILE_NAME);

		Assert.Equal(Path.Combine(LaunchCopyLayout.PredictFolderPath(destination), _PACK_FILE_NAME), predicted);
	}

	[Fact]
	public void ThePredictedAndTheRealPathsDifferOnlyInTheIdentifierDigits()
	{
		string destination = Path.Combine(Path.GetTempPath(), _DESTINATION_FOLDER_NAME);
		string predicted = LaunchCopyLayout.PredictCopyPath(destination, _PACK_FILE_NAME);
		string real = Path.Combine(destination, LaunchCopyLayout.NewFolderName(), _PACK_FILE_NAME);

		Assert.Equal(predicted.Length, real.Length);
		Assert.Equal(MaskIdentifier(predicted), MaskIdentifier(real));
	}

	[Fact]
	public void ThePredictedPathCarriesNoCharacterThatARealPathCouldNotCarry()
	{
		string predictedFolderName = Path.GetFileName(LaunchCopyLayout.PredictFolderPath(Path.GetTempPath()));
		string identifier = predictedFolderName[_EXPECTED_PREFIX.Length..];

		Assert.All(identifier, digit => Assert.True(char.IsAsciiHexDigit(digit)));
	}

	[Theory]
	[InlineData("ai-assist-launch-0123456789abcdef0123456789abcdef", true)]
	[InlineData("ai-assist-launch-0123456789ABCDEF0123456789ABCDEF", false)]
	[InlineData("ai-assist-launch-0123456789abcdef0123456789abcde", false)]
	[InlineData("ai-assist-launch-0123456789abcdef0123456789abcdefa", false)]
	[InlineData("ai-assist-launch-0123456789abcdef0123456789abcdeg", false)]
	[InlineData("assist-launch-0123456789abcdef0123456789abcdef", false)]
	public void TheShapeCheckAcceptsOnlyThePrefixAndThirtyTwoLowerCaseHexDigits(string folderName, bool expected)
	{
		Assert.Equal(expected, LaunchCopyLayout.HasLaunchFolderShape(folderName));
	}

	[Fact]
	public void ACreatedFolderExistsDirectlyUnderTheDestination()
	{
		using TemporaryFolder destination = TemporaryFolder.Create();

		string folderPath = LaunchCopyLayout.NewFolder(destination.Path);

		Assert.True(Directory.Exists(folderPath));
		Assert.Equal(destination.Path, Path.GetDirectoryName(folderPath));
		Assert.True(LaunchCopyLayout.HasLaunchFolderShape(Path.GetFileName(folderPath)));
	}

	[Fact]
	public void ACreatedFolderIsReadableAndWritableByItsOwnerOnlyOnLinux()
	{
		if (!OperatingSystem.IsWindows())
		{
			using TemporaryFolder destination = TemporaryFolder.Create();

			string folderPath = LaunchCopyLayout.NewFolder(destination.Path);

			Assert.Equal(
				UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute,
				File.GetUnixFileMode(folderPath));
		}
	}

	[Fact]
	public void ADestinationThatIsEmptyIsRefused()
	{
		Assert.Throws<ArgumentException>(() => LaunchCopyLayout.PredictFolderPath(string.Empty));
		Assert.Throws<ArgumentException>(() => LaunchCopyLayout.NewFolder(" "));
	}

	private static string MaskIdentifier(string path)
	{
		return Regex.Replace(path, $"[0-9a-f]{{{_EXPECTED_IDENTIFIER_LENGTH}}}", new string(_HEX_PLACEHOLDER, _EXPECTED_IDENTIFIER_LENGTH));
	}
}
