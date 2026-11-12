using System.Runtime.Versioning;
using System.Security.AccessControl;
using System.Security.Principal;

using MigrateAuth.Cli.Agent;
using MigrateAuth.Cli.Tests.Session;
using MigrateAuth.Cli.Writing;

using Xunit;

namespace MigrateAuth.Cli.Tests.Writing;

/// <summary>
/// The pack's permissions: on Linux the writer's <c>0600</c>, and on a replacement through
/// an append the mode is reset to <c>0600</c> whatever the original had; on Windows no mode exists and the file inherits the
/// destination folder's access control list, which is asserted as such. The Linux assertions compile everywhere and are
/// no-ops on Windows; their Linux run is the Linux suite.
/// </summary>
public sealed class PromptPackPermissionTests
{
	private const string _PACK_BODY = "Instruction text.\n";
	private const string _APPENDED_SECTION = "## Appended\n";
	private const string _CONTROL_FILE_NAME = "control.txt";
	private const string _NESTED_DESTINATION_NAME = "created-by-the-writer";
	private const UnixFileMode _OPEN_FILE_MODE =
		UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.GroupRead | UnixFileMode.OtherRead;

	[Theory]
	[InlineData(AgentEnvironment.ClaudeCode)]
	[InlineData(AgentEnvironment.OpenAiCodex)]
	[InlineData(AgentEnvironment.GitHubCopilotCli)]
	public void OnLinuxEveryPackIsCreatedWithTheRestrictiveFileMode(AgentEnvironment environment)
	{
		using TemporaryFolder repositoryFolder = TemporaryFolder.Create();
		using TemporaryFolder destinationFolder = TemporaryFolder.Create();

		ArtifactWriteResult result = ReportAppendFixture.BuildWriter(repositoryFolder, destinationFolder)
			.WritePromptPackStreaming(
				AgentEnvironmentCatalog.Describe(environment).PackFileNameStem,
				sink => sink.Write(_PACK_BODY));

		FileInfo pack = new(destinationFolder.Combine(result.FileNameWritten));

		Assert.True(pack.Exists);

		if (!OperatingSystem.IsWindows())
		{
			Assert.Equal(RestrictiveFileCreation.RESTRICTIVE_UNIX_FILE_MODE, pack.UnixFileMode);
		}
	}

	/// <summary>A second pack, with a suffix, gets the same mode: the restrictive creation is the temporary's, not the name's.</summary>
	[Fact]
	public void OnLinuxASuffixedPackIsRestrictiveToo()
	{
		using TemporaryFolder repositoryFolder = TemporaryFolder.Create();
		using TemporaryFolder destinationFolder = TemporaryFolder.Create();

		AtomicArtifactWriter writer = ReportAppendFixture.BuildWriter(repositoryFolder, destinationFolder);

		writer.WritePromptPackStreaming(AgentEnvironmentCatalog.CLAUDE_CODE_PACK_STEM, sink => sink.Write(_PACK_BODY));

		ArtifactWriteResult second = writer.WritePromptPackStreaming(
			AgentEnvironmentCatalog.CLAUDE_CODE_PACK_STEM,
			sink => sink.Write(_PACK_BODY));

		Assert.EndsWith("-2.md", second.FileNameWritten, StringComparison.Ordinal);

		if (!OperatingSystem.IsWindows())
		{
			Assert.Equal(
				RestrictiveFileCreation.RESTRICTIVE_UNIX_FILE_MODE,
				new FileInfo(destinationFolder.Combine(second.FileNameWritten)).UnixFileMode);
		}
	}

	/// <summary>The replacement of an append is a restrictively created temporary, so a looser original mode is reset.</summary>
	[Fact]
	public void OnLinuxAnAppendResetsALooserModeOfTheReportToTheRestrictiveOne()
	{
		using TemporaryFolder repositoryFolder = TemporaryFolder.Create();
		using TemporaryFolder destinationFolder = TemporaryFolder.Create();

		ReportWriteHolder holder = new();
		AtomicArtifactWriter writer = ReportAppendFixture.BuildWriter(repositoryFolder, destinationFolder, holder);
		string reportPath = ReportAppendFixture.WriteReport(writer, destinationFolder);

		if (!OperatingSystem.IsWindows())
		{
			File.SetUnixFileMode(reportPath, _OPEN_FILE_MODE);
		}

		ReportAppendResult result = writer.AppendToReport(holder, sink => sink.Write(_APPENDED_SECTION));

		Assert.True(result.Appended);

		if (!OperatingSystem.IsWindows())
		{
			Assert.Equal(RestrictiveFileCreation.RESTRICTIVE_UNIX_FILE_MODE, new FileInfo(reportPath).UnixFileMode);
		}
	}

	[Fact]
	public void OnLinuxADirectoryCreatedRestrictivelyHasTheOwnerOnlyMode()
	{
		using TemporaryFolder parentFolder = TemporaryFolder.Create();

		DirectoryInfo created = RestrictiveFileCreation.CreateDirectory(Path.Combine(parentFolder.Path, _NESTED_DESTINATION_NAME));

		Assert.True(created.Exists);

		if (!OperatingSystem.IsWindows())
		{
			Assert.Equal(RestrictiveFileCreation.RESTRICTIVE_UNIX_DIRECTORY_MODE, created.UnixFileMode);
		}
	}

	/// <summary>Windows: no mode is applied; the pack carries the access rules its folder hands down, exactly as an ordinary file does.</summary>
	[Fact]
	public void OnWindowsThePackInheritsTheDestinationFoldersAccessControlList()
	{
		using TemporaryFolder repositoryFolder = TemporaryFolder.Create();
		using TemporaryFolder destinationFolder = TemporaryFolder.Create();

		ArtifactWriteResult result = ReportAppendFixture.BuildWriter(repositoryFolder, destinationFolder)
			.WritePromptPackStreaming(AgentEnvironmentCatalog.OPENAI_CODEX_PACK_STEM, sink => sink.Write(_PACK_BODY));

		string controlPath = destinationFolder.Combine(_CONTROL_FILE_NAME);

		File.WriteAllText(controlPath, _PACK_BODY);

		if (OperatingSystem.IsWindows())
		{
			AssertInheritsLikeAnOrdinaryFile(destinationFolder.Combine(result.FileNameWritten), controlPath);
		}
	}

	[SupportedOSPlatform("windows")]
	private static void AssertInheritsLikeAnOrdinaryFile(string packPath, string controlPath)
	{
		FileSecurity packSecurity = new FileInfo(packPath).GetAccessControl();
		FileSecurity controlSecurity = new FileInfo(controlPath).GetAccessControl();

		Assert.False(packSecurity.AreAccessRulesProtected);
		Assert.Equal(DescribeInheritedRules(controlSecurity), DescribeInheritedRules(packSecurity));
	}

	[SupportedOSPlatform("windows")]
	private static List<string> DescribeInheritedRules(FileSecurity security)
	{
		List<string> rules =
		[
			.. security.GetAccessRules(includeExplicit: false, includeInherited: true, typeof(SecurityIdentifier))
				.Cast<FileSystemAccessRule>()
				.Select(rule => $"{rule.IdentityReference.Value}|{rule.FileSystemRights}|{rule.AccessControlType}")
				.Order(StringComparer.Ordinal)
		];

		return rules;
	}
}
