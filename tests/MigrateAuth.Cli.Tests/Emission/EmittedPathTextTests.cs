using MigrateAuth.Cli.Emission;
using MigrateAuth.Cli.Session;
using MigrateAuth.Cli.Tests.Session;

using Xunit;

namespace MigrateAuth.Cli.Tests.Emission;

/// <summary>
/// A path is untrusted text on the way out. The invariant permits the relative path and says
/// nothing about its content, and the content belongs to the repository - so a name can carry
/// a terminal escape, a bidirectional override, or Markdown that closes a code fence and opens
/// a link. Each destination is wrong about different characters, and these assertions are that
/// each one is escaped for where it is going rather than once and for all.
/// </summary>
public sealed class EmittedPathTextTests
{
	private const char _ESCAPE_CHARACTER = '';
	private const char _RIGHT_TO_LEFT_OVERRIDE = '‮';
	private const string _ORDINARY_RELATIVE_PATH = "src/Client/HttpFactory.cs";

	[Fact]
	public void AnOrdinaryPathIsLeftAloneInEveryDestination()
	{
		RepositoryRelativePath path = BuildRelativePath(_ORDINARY_RELATIVE_PATH);

		Assert.Equal(_ORDINARY_RELATIVE_PATH, EmittedPathText.ForDestination(path, EmissionDestination.Terminal));
		Assert.Equal(_ORDINARY_RELATIVE_PATH, EmittedPathText.ForDestination(path, EmissionDestination.Markdown));
		Assert.Equal(_ORDINARY_RELATIVE_PATH, EmittedPathText.ForDestination(path, EmissionDestination.StructuredData));
	}

	[Fact]
	public void AControlCharacterNeverReachesTheTerminal()
	{
		string hostilePath = $"src/{_ESCAPE_CHARACTER}[2Jcleared.cs";
		RepositoryRelativePath path = BuildRelativePath(hostilePath);

		string rendered = EmittedPathText.ForDestination(path, EmissionDestination.Terminal);

		Assert.DoesNotContain(_ESCAPE_CHARACTER, rendered);
		Assert.Equal("src/[2Jcleared.cs", rendered);
	}

	[Fact]
	public void ALineEndingNeverReachesTheTerminalAndCannotForgeALineOfOutput()
	{
		RepositoryRelativePath path = BuildRelativePath("src/a\r\nScan complete: 0 findings.cs");

		string rendered = EmittedPathText.ForDestination(path, EmissionDestination.Terminal);

		Assert.DoesNotContain('\n', rendered);
		Assert.DoesNotContain('\r', rendered);
	}

	/// <summary>
	/// A bidirectional override is a FORMATTING character rather than a control character, so a
	/// control-character filter alone lets it through and a name renders as a different name.
	/// </summary>
	[Fact]
	public void ABidirectionalOverrideNeverReachesTheTerminalOrTheReport()
	{
		RepositoryRelativePath path = BuildRelativePath($"src/harmless{_RIGHT_TO_LEFT_OVERRIDE}sc.exe");

		Assert.DoesNotContain(_RIGHT_TO_LEFT_OVERRIDE, EmittedPathText.ForDestination(path, EmissionDestination.Terminal));
		Assert.DoesNotContain(_RIGHT_TO_LEFT_OVERRIDE, EmittedPathText.ForDestination(path, EmissionDestination.Markdown));
	}

	[Theory]
	[InlineData("src/a]( http.cs", @"src/a\]\( http.cs")]
	[InlineData("src/a`b.cs", @"src/a\`b.cs")]
	[InlineData("src/*a*.cs", @"src/\*a\*.cs")]
	[InlineData("src/a|b.cs", @"src/a\|b.cs")]
	[InlineData("src/a<b>.cs", @"src/a\<b\>.cs")]
	[InlineData("src/a&b.cs", @"src/a\&b.cs")]
	[InlineData("src/a~b.cs", @"src/a\~b.cs")]
	[InlineData("src/a_b_.cs", @"src/a\_b\_.cs")]
	public void AMarkdownMetacharacterIsEscapedForTheReport(string relativePath, string expectedRendering)
	{
		RepositoryRelativePath path = BuildRelativePath(relativePath);

		Assert.Equal(expectedRendering, EmittedPathText.ForDestination(path, EmissionDestination.Markdown));
	}

	[Theory]
	[InlineData("#heading.cs", @"\#heading.cs")]
	[InlineData("-bullet.cs", @"\-bullet.cs")]
	[InlineData("2. ordered.cs", @"2\. ordered.cs")]
	public void AMetacharacterThatOnlyOpensABlockIsEscapedOnlyWhereItWould(string relativePath, string expectedRendering)
	{
		RepositoryRelativePath path = BuildRelativePath(relativePath);

		Assert.Equal(expectedRendering, EmittedPathText.ForDestination(path, EmissionDestination.Markdown));
	}

	/// <summary>
	/// A hyphen inside a name is not a bullet and a dot inside a name is not a list marker, so
	/// escaping them everywhere would make every ordinary path unreadable for no gain.
	/// </summary>
	[Fact]
	public void AHyphenAndADotInsideANameAreNotEscaped()
	{
		RepositoryRelativePath path = BuildRelativePath("src/basic-auth.cs");

		Assert.Equal("src/basic-auth.cs", EmittedPathText.ForDestination(path, EmissionDestination.Markdown));
	}

	/// <summary>
	/// The data artifact relies on its own encoder, so escaping before it would double-escape
	/// and produce a path a grader could not match against the repository.
	/// </summary>
	[Fact]
	public void TheDataArtifactReceivesThePathUnescaped()
	{
		RepositoryRelativePath path = BuildRelativePath("src/a]( http.cs");

		Assert.Equal("src/a]( http.cs", EmittedPathText.ForDestination(path, EmissionDestination.StructuredData));
	}

	[Fact]
	public void AnUndeclaredPathRendersAsNothingRatherThanAsAPlaceholder()
	{
		Assert.Equal(string.Empty, EmittedPathText.ForDestination(default, EmissionDestination.Markdown));
	}

	[Fact]
	public void SeparatorsAreNormalizedSoTheEmittedPathIsTheSameOnEveryPlatform()
	{
		using TemporaryFolder repositoryFolder = TemporaryFolder.Create();

		RepositoryFolderPath repository = RepositoryFolderPath.FromSuppliedText(repositoryFolder.Path);
		string nativePath = System.IO.Path.Combine("src", "Client", "HttpFactory.cs");

		Assert.True(RepositoryRelativePath.TryFromScannedPath(repository, nativePath, out RepositoryRelativePath path));
		Assert.Equal(_ORDINARY_RELATIVE_PATH, path.Value);
	}

	private static RepositoryRelativePath BuildRelativePath(string relativePath)
	{
		using TemporaryFolder repositoryFolder = TemporaryFolder.Create();

		RepositoryFolderPath repository = RepositoryFolderPath.FromSuppliedText(repositoryFolder.Path);

		Assert.True(RepositoryRelativePath.TryFromScannedPath(repository, relativePath, out RepositoryRelativePath path));

		return path;
	}
}
