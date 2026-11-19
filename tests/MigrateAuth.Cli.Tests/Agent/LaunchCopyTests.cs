using MigrateAuth.Cli.Agent;
using MigrateAuth.Cli.Session;

using Xunit;

namespace MigrateAuth.Cli.Tests.Agent;

/// <summary>
/// THE LAUNCH COPY, THROUGH THE STAGE. On confirmation only, the pack body is written again into a new folder under the
/// destination; the copy is byte-identical to the pack; no argument the agent receives names the destination except by
/// the copy's path; the folder is named on the console by the destination as the user supplied it and is left in place;
/// and a copy that cannot be written is a generation failure that starts nothing.
/// </summary>
public sealed class LaunchCopyTests
{
	private const string _PACK_FILE_NAME = AiAssistHarness.CLAUDE_PACK_FILE_NAME;
	private const int _IDENTIFIER_LENGTH = 32;

	[Fact]
	public void NoLaunchFolderExistsUntilTheUserConfirms()
	{
		using AiAssistHarness harness = new(["1", "n"]);

		harness.InstallFake(AgentEnvironment.ClaudeCode);
		harness.Run();

		Assert.Empty(harness.LaunchFolders());
	}

	[Fact]
	public void ConfirmingWritesOneFolderHoldingACopyThatIsByteIdenticalToThePack()
	{
		using AiAssistHarness harness = new(["1", "y"]);

		harness.InstallFake(AgentEnvironment.ClaudeCode);
		harness.Run();

		string folder = Assert.Single(harness.LaunchFolders());
		string copyPath = Path.Combine(folder, _PACK_FILE_NAME);

		Assert.True(LaunchCopyLayout.HasLaunchFolderShape(Path.GetFileName(folder)));
		Assert.Equal(_IDENTIFIER_LENGTH + LaunchCopyLayout.FOLDER_NAME_PREFIX.Length, Path.GetFileName(folder).Length);
		Assert.Equal(File.ReadAllBytes(harness.DestinationFolder.Combine(_PACK_FILE_NAME)), File.ReadAllBytes(copyPath));
	}

	[Fact]
	public void TheFolderIsLeftInPlaceAndNamedOnTheConsoleByTheDestinationAsTheUserSuppliedIt()
	{
		using AiAssistHarness harness = new(["1", "y"]);

		harness.InstallFake(AgentEnvironment.ClaudeCode);
		harness.Run();

		string folderName = Path.GetFileName(Assert.Single(harness.LaunchFolders()));
		string expected = string.Format(
			System.Globalization.CultureInfo.InvariantCulture,
			AiAssistTexts.LAUNCH_FOLDER_FORMAT,
			AiAssistTexts.LaunchFolderText(AiAssistHarness.DESTINATION_CONSOLE_TEXT, folderName));

		Assert.Contains(expected, harness.Transcript, StringComparison.Ordinal);
		Assert.True(Directory.Exists(Path.Combine(harness.DestinationFolder.Path, folderName)));
	}

	[Fact]
	public void NoArgumentNamesTheDestinationExceptByTheCopyPathOrItsFolder()
	{
		using AiAssistHarness harness = new(["1", "y"]);

		harness.InstallFake(AgentEnvironment.ClaudeCode);
		harness.Run();

		string folder = Assert.Single(harness.LaunchFolders());
		string copyPath = Path.Combine(folder, _PACK_FILE_NAME);
		string[] arguments = harness.RecordOf(AgentEnvironment.ClaudeCode).Arguments;

		Assert.Contains(arguments, argument => argument.Contains(copyPath, StringComparison.Ordinal));

		foreach (string argument in arguments)
		{
			string remaining = argument.Replace(copyPath, string.Empty, StringComparison.Ordinal).Replace(folder, string.Empty, StringComparison.Ordinal);

			Assert.DoesNotContain(harness.DestinationFolder.Path, remaining, StringComparison.Ordinal);
		}

		Assert.DoesNotContain(
			arguments,
			argument => argument.Contains(harness.DestinationFolder.Combine(_PACK_FILE_NAME), StringComparison.Ordinal));
	}

	[Fact]
	public void ASecondConfirmedRunMakesASecondFolderAndTheFirstStaysWithItsCopy()
	{
		using AiAssistHarness harness = new(["1", "y", "1", "y"]);

		harness.InstallFake(AgentEnvironment.ClaudeCode);
		harness.Run();

		string firstFolder = Assert.Single(harness.LaunchFolders());

		File.Delete(harness.RecordPathOf(AgentEnvironment.ClaudeCode));
		harness.Run();

		Assert.Equal(2, harness.LaunchFolders().Length);
		Assert.Contains(firstFolder, harness.LaunchFolders());
		Assert.True(File.Exists(Path.Combine(firstFolder, _PACK_FILE_NAME)));
		Assert.Equal([AiAssistHarness.CLAUDE_SECOND_PACK_FILE_NAME, AiAssistHarness.CLAUDE_PACK_FILE_NAME], harness.PackFiles());
	}

	/// <summary>The destination is replaced by a file after the pack was written, so the launch folder cannot be made.</summary>
	[Fact]
	public void ACopyThatCannotBeWrittenIsAGenerationFailureAndNothingIsStarted()
	{
		using AiAssistHarness harness = new(["1", "y"]);

		harness.InstallFake(AgentEnvironment.ClaudeCode);
		harness.Input.OnRead = index =>
		{
			if (index == AiAssistHarness.CONFIRMATION_READ_INDEX)
			{
				harness.ReplaceDestinationWithFile();
			}
		};

		StageRunOutcome outcome = harness.Run();

		Assert.Equal(StageRunOutcome.SelfReported, outcome);
		Assert.Contains(AiAssistTexts.PACK_GENERATION_FAILED, harness.Transcript, StringComparison.Ordinal);
		Assert.False(harness.AnyFakeWasStarted());
	}
}
