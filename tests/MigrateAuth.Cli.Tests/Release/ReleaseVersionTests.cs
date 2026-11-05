using System.Reflection;
using System.Xml.Linq;

using MigrateAuth.Cli.Reporting;

using Xunit;

namespace MigrateAuth.Cli.Tests.Release;

/// <summary>
/// THE RELEASE NUMBER IS DECLARED ONCE IN THE BUILD PROPERTIES OF THE CLI PROJECT, AND EVERYTHING THAT REPEATS IT
/// IS TIED TO THAT ONE DECLARATION.
///
/// The product constant, the version resource stamped into the binary and the names of the release assets
/// must all read the same number. The product code never reads the assembly's attributes, so the tie is made
/// here, where reflection is affordable.
/// </summary>
public sealed class ReleaseVersionTests
{
	private const string _SOLUTION_FILE_PATTERN = "*.sln";
	private const string _PROJECT_RELATIVE_PATH = "src/MigrateAuth.Cli/MigrateAuth.Cli.csproj";
	private const string _POWERSHELL_SCRIPT_RELATIVE_PATH = "scripts/build-release.ps1";
	private const string _BASH_SCRIPT_RELATIVE_PATH = "scripts/build-release.sh";
	private const string _VERSION_ELEMENT = "Version";
	private const string _FILE_VERSION_ELEMENT = "FileVersion";
	private const string _COMPANY_ELEMENT = "Company";
	private const string _PRODUCT_ELEMENT = "Product";
	private const string _COPYRIGHT_ELEMENT = "Copyright";
	private const string _SOURCE_REVISION_ELEMENT = "IncludeSourceRevisionInInformationalVersion";
	private const string _EXPECTED_COMPANY = "NSerio";
	private const string _EXPECTED_PRODUCT = "migrate-auth";
	private const string _EXPECTED_COPYRIGHT = "Copyright (c) 2026 Relativity ODA LLC";
	private const string _PRE_RELEASE_SEPARATOR = "-";
	private const string _NUMERIC_FILE_VERSION_SUFFIX = ".0";
	private const string _PROJECT_VERSION_READ_MARKER = "<Version>";

	[Fact]
	public void TheProjectVersionEqualsTheProductVersionConstant()
	{
		Assert.Equal(ToolVersion.CURRENT, ReadProjectProperty(_VERSION_ELEMENT));
	}

	[Fact]
	public void TheBuiltAssemblyCarriesTheProductVersionAndNothingElseAsItsInformationalVersion()
	{
		AssemblyInformationalVersionAttribute? attribute = typeof(ToolVersion).Assembly
			.GetCustomAttribute<AssemblyInformationalVersionAttribute>();

		Assert.NotNull(attribute);
		Assert.Equal(ToolVersion.CURRENT, attribute.InformationalVersion);
	}

	[Fact]
	public void TheNumericFileVersionIsTheProductVersionWithoutItsPreReleaseLabel()
	{
		string releaseNumber = ToolVersion.CURRENT.Split(_PRE_RELEASE_SEPARATOR)[0];

		Assert.Equal(releaseNumber + _NUMERIC_FILE_VERSION_SUFFIX, ReadProjectProperty(_FILE_VERSION_ELEMENT));
		Assert.Equal(releaseNumber + _NUMERIC_FILE_VERSION_SUFFIX, typeof(ToolVersion).Assembly.GetName().Version?.ToString());
	}

	[Fact]
	public void TheProjectStampsTheCompanyProductAndCopyright()
	{
		Assert.Equal(_EXPECTED_COMPANY, ReadProjectProperty(_COMPANY_ELEMENT));
		Assert.Equal(_EXPECTED_PRODUCT, ReadProjectProperty(_PRODUCT_ELEMENT));
		Assert.Equal(_EXPECTED_COPYRIGHT, ReadProjectProperty(_COPYRIGHT_ELEMENT));
		Assert.Equal(bool.FalseString, ReadProjectProperty(_SOURCE_REVISION_ELEMENT), ignoreCase: true);
	}

	[Theory]
	[InlineData(_POWERSHELL_SCRIPT_RELATIVE_PATH)]
	[InlineData(_BASH_SCRIPT_RELATIVE_PATH)]
	public void TheReleaseScriptsReadTheVersionFromTheProjectAndHoldNoCopyOfIt(string scriptRelativePath)
	{
		string scriptText = File.ReadAllText(Path.Combine(LocateRepositoryRoot(), scriptRelativePath));

		Assert.Contains(_PROJECT_VERSION_READ_MARKER, scriptText, StringComparison.Ordinal);
		Assert.DoesNotContain(ToolVersion.CURRENT, scriptText, StringComparison.Ordinal);
	}

	private static string ReadProjectProperty(string elementName)
	{
		XDocument project = XDocument.Load(Path.Combine(LocateRepositoryRoot(), _PROJECT_RELATIVE_PATH));
		string? value = project.Descendants(elementName).Select(element => element.Value.Trim()).SingleOrDefault();

		return value ?? throw new InvalidOperationException($"The project declares no {elementName} property.");
	}

	private static string LocateRepositoryRoot()
	{
		DirectoryInfo? candidate = new(AppContext.BaseDirectory);

		while (candidate is not null && candidate.GetFiles(_SOLUTION_FILE_PATTERN).Length == 0)
		{
			candidate = candidate.Parent;
		}

		return candidate?.FullName
			?? throw new DirectoryNotFoundException("No folder holding a solution file sits above the test assembly.");
	}
}
