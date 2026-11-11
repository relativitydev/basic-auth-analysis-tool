using System.Text.RegularExpressions;

using Xunit;

namespace MigrateAuth.Cli.Tests.Agent;

/// <summary>
/// RULES READ FROM THE PRODUCT'S OWN SOURCE FOR THE AI-ASSIST STAGE: it reads no key press, reads the environment in one
/// file only, and reads no credential and no agent configuration file; and the pack's scope never materializes a collection
/// of findings. The catalog folder is left out of the text rules because its analyzers hold patterns that describe code in
/// other people's repositories, and those patterns name the very calls the product itself must never make.
/// </summary>
public sealed class AiAssistSourceRulesTests
{
	private const string _SCOPE_FILE = "PromptPackScope.cs";
	private const string _PROVIDER_FILE = "PathEnvironmentSearchPathProvider.cs";
	private const string _AI_ASSIST_SESSION_PREFIX = "AiAssist";

	private static readonly Regex _keyRead = new(@"\bReadKey\b", RegexOptions.CultureInvariant);

	private static readonly Regex _environmentRead = new(
		@"\bEnvironment\s*\.\s*(GetEnvironmentVariable|GetEnvironmentVariables|ExpandEnvironmentVariables)\b",
		RegexOptions.CultureInvariant);

	private static readonly Regex _fileRead = new(
		@"\b(File\s*\.\s*(ReadAll\w*|Open\w*|ReadLines|ReadLinesAsync)|new\s+StreamReader|new\s+FileStream|FileInfo\s*\([^)]*\)\s*\.\s*Open\w*)",
		RegexOptions.CultureInvariant);

	/// <summary>Names of credential stores and agent configuration that no file of the stage may mention.</summary>
	private static readonly string[] _credentialAndConfigurationNames =
	[
		".claude",
		".codex",
		".copilot",
		"credentials.json",
		"auth.json",
		"settings.json",
		"config.toml",
		"hosts.yml",
		".netrc",
		".npmrc",
		".git-credentials",
		".aws",
		".ssh"
	];

	/// <summary>The folder lookups that lead to a user's profile, where such files live.</summary>
	private static readonly string[] _profileFolderLookups = ["SpecialFolder", "GetFolderPath"];

	private static readonly Regex _stringLiteral = new("\"(?:[^\"\\\\\\r\\n]|\\\\.)*\"", RegexOptions.CultureInvariant);

	/// <summary>
	/// The scope walks the findings and never collects them: a second copy of up to fifty thousand findings at the
	/// moment a streamed write is meant to stay incremental is what the memory bound forbids. No operator that
	/// materializes a sequence, and no collection expression spread, appears in the file.
	/// </summary>
	[Fact]
	public void ThePromptPackScopeMaterializesNoCollectionOfFindings()
	{
		string source = File.ReadAllText(Path.Combine(ProductSourceFiles.ProductRoot, ProductSourceFiles.AGENT_FOLDER, _SCOPE_FILE));
		Regex materializingCall = new(
			@"\.\s*(ToList|ToArray|ToImmutableArray|ToImmutableList|ToImmutableHashSet|ToHashSet|ToDictionary|ToFrozenSet|ToFrozenDictionary|ToLookup)\s*\(",
			RegexOptions.CultureInvariant);
		Regex collectionSpread = new(@"\[\s*\.\.", RegexOptions.CultureInvariant);

		Assert.DoesNotMatch(materializingCall, source);
		Assert.DoesNotMatch(collectionSpread, source);
		Assert.DoesNotContain("new List<", source, StringComparison.Ordinal);
		Assert.DoesNotContain("ImmutableArray.CreateBuilder", source, StringComparison.Ordinal);
	}

	/// <summary>The stage reads whole lines and nothing else: no key press is read anywhere in the product.</summary>
	[Fact]
	public void NoFileOfTheProductReadsAKeyPress()
	{
		string[] offenders = [.. ProductFilesOutsideTheCatalog().Where(path => _keyRead.IsMatch(ProductSourceFiles.Read(path))).Select(ProductSourceFiles.RelativePathOf)];

		Assert.Empty(offenders);
	}

	/// <summary>The search path provider is the one reader of the environment; the analyzers' patterns are not code that runs.</summary>
	[Fact]
	public void NoFileOutsideTheProviderReadsTheEnvironment()
	{
		string[] offenders =
		[
			.. ProductFilesOutsideTheCatalog()
				.Where(path => _environmentRead.IsMatch(ProductSourceFiles.Read(path)))
				.Select(ProductSourceFiles.RelativePathOf)
		];

		Assert.Equal([$"{ProductSourceFiles.AGENT_FOLDER}/{_PROVIDER_FILE}"], offenders);
	}

	/// <summary>
	/// The stage's own files neither open a file for reading nor name a credential store or an agent's configuration, so
	/// nothing it does can read a secret or learn how the user configured the agent.
	/// </summary>
	[Fact]
	public void TheStageReadsNoCredentialAndNoAgentConfigurationFile()
	{
		foreach (string path in StageFiles())
		{
			string source = ProductSourceFiles.Read(path);
			string relative = ProductSourceFiles.RelativePathOf(path);

			Assert.False(_fileRead.IsMatch(source), $"{relative} opens a file for reading.");

			string code = string.Join(AiAssistHarness.LINE_FEED_CHARACTER, source.Split(AiAssistHarness.LINE_FEED_CHARACTER).Where(line => !line.TrimStart().StartsWith("//", StringComparison.Ordinal)));
			string[] literals = [.. _stringLiteral.Matches(code).Select(match => match.Value)];

			foreach (string name in _credentialAndConfigurationNames)
			{
				Assert.False(literals.Any(literal => literal.Contains(name, StringComparison.OrdinalIgnoreCase)), $"{relative} has a string that names {name}.");
			}

			foreach (string lookup in _profileFolderLookups)
			{
				Assert.False(code.Contains(lookup, StringComparison.Ordinal), $"{relative} looks up {lookup}.");
			}
		}
	}

	private static IEnumerable<string> ProductFilesOutsideTheCatalog()
	{
		return ProductSourceFiles.All().Where(path => ProductSourceFiles.TopFolderOf(path) != ProductSourceFiles.CATALOG_FOLDER);
	}

	/// <summary>The agent boundary and the stage's own files of the session folder.</summary>
	private static IEnumerable<string> StageFiles()
	{
		return ProductSourceFiles.All().Where(path =>
			ProductSourceFiles.TopFolderOf(path) == ProductSourceFiles.AGENT_FOLDER
			|| (ProductSourceFiles.TopFolderOf(path) == ProductSourceFiles.SESSION_FOLDER
				&& Path.GetFileName(path).StartsWith(_AI_ASSIST_SESSION_PREFIX, StringComparison.Ordinal)));
	}
}
