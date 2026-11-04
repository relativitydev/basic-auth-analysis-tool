using MigrateAuth.Cli.Configuration;

using Xunit;

namespace MigrateAuth.Cli.Tests.Configuration;

/// <summary>
/// A leading <c>~</c> in the output path read from the profile file. The home folder is
/// handed in as a delegate, so no test here changes the process environment.
/// </summary>
public sealed class OutputPathHomeExpansionTests
{
	private const string _OUTPUTS_FOLDER_NAME = "Outputs";
	private const string _NESTED_FOLDER_NAME = "nested";

	private static readonly string _fakeHomeFolder = Path.Combine(Path.GetTempPath(), "placeholder-home-folder");

	[Theory]
	[InlineData("~")]
	[InlineData("~/")]
	[InlineData("~\\")]
	public void AHomeMarkerAloneExpandsToTheHomeFolder(string configuredText)
	{
		OutputPathExpansion expansion = OutputPathHomeExpansion.Expand(configuredText, () => _fakeHomeFolder);

		Assert.Equal(OutputPathExpansionOutcome.Expanded, expansion.Outcome);
		Assert.Equal(_fakeHomeFolder, expansion.Text);
	}

	[Theory]
	[InlineData("~/Outputs")]
	[InlineData("~\\Outputs")]
	[InlineData("~//Outputs")]
	public void AHomeMarkerFollowedByAFolderExpandsToThatFolderUnderTheHome(string configuredText)
	{
		OutputPathExpansion expansion = OutputPathHomeExpansion.Expand(configuredText, () => _fakeHomeFolder);

		Assert.Equal(OutputPathExpansionOutcome.Expanded, expansion.Outcome);
		Assert.Equal(Path.Combine(_fakeHomeFolder, _OUTPUTS_FOLDER_NAME), expansion.Text);
	}

	[Fact]
	public void ANestedFolderUnderTheHomeKeepsEveryLevel()
	{
		OutputPathExpansion expansion = OutputPathHomeExpansion.Expand(
			$"~/{_OUTPUTS_FOLDER_NAME}/{_NESTED_FOLDER_NAME}",
			() => _fakeHomeFolder);

		Assert.Equal(OutputPathExpansionOutcome.Expanded, expansion.Outcome);
		Assert.Equal(
			Path.GetFullPath(Path.Combine(_fakeHomeFolder, _OUTPUTS_FOLDER_NAME, _NESTED_FOLDER_NAME)),
			Path.GetFullPath(expansion.Text));
	}

	[Theory]
	[InlineData("Outputs")]
	[InlineData("./Outputs")]
	[InlineData("a~b")]
	[InlineData("Outputs/~")]
	[InlineData(" ~/Outputs")]
	public void ATextThatDoesNotStartWithAHomeMarkerIsReturnedUntouchedAndTheHomeIsNeverAsked(string configuredText)
	{
		int homeRequests = 0;

		OutputPathExpansion expansion = OutputPathHomeExpansion.Expand(
			configuredText,
			() =>
			{
				homeRequests++;

				return _fakeHomeFolder;
			});

		Assert.Equal(OutputPathExpansionOutcome.NotApplicable, expansion.Outcome);
		Assert.Equal(configuredText, expansion.Text);
		Assert.Equal(0, homeRequests);
	}

	[Theory]
	[InlineData("~alice")]
	[InlineData("~alice/Outputs")]
	[InlineData("~alice\\Outputs")]
	[InlineData("~.")]
	public void AnotherUsersHomeFormIsRefused(string configuredText)
	{
		OutputPathExpansion expansion = OutputPathHomeExpansion.Expand(configuredText, () => _fakeHomeFolder);

		Assert.Equal(OutputPathExpansionOutcome.OtherUserFormRefused, expansion.Outcome);
		Assert.Equal(configuredText, expansion.Text);
	}

	[Theory]
	[InlineData("")]
	[InlineData("   ")]
	[InlineData(null)]
	public void AHomeFolderThatCannotBeResolvedIsRefusedForEveryOwnHomeForm(string? unresolvedHome)
	{
		foreach (string configuredText in new[] { "~", "~/", "~\\", "~/Outputs", "~\\Outputs" })
		{
			OutputPathExpansion expansion = OutputPathHomeExpansion.Expand(configuredText, () => unresolvedHome!);

			Assert.Equal(OutputPathExpansionOutcome.HomeFolderUnresolved, expansion.Outcome);
			Assert.Equal(configuredText, expansion.Text);
		}
	}

	[Fact]
	public void TheProductionProviderReadsTheUserProfileFolder()
	{
		Assert.Equal(
			Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
			OutputPathHomeExpansion.ReadUserProfileFolder());
	}

	[Fact]
	public void ANullTextOrANullProviderIsRejected()
	{
		Assert.Throws<ArgumentNullException>(() => OutputPathHomeExpansion.Expand(null!, () => _fakeHomeFolder));
		Assert.Throws<ArgumentNullException>(() => OutputPathHomeExpansion.Expand("~", null!));
	}
}
