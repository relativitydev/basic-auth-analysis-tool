using System.Security.Cryptography;
using System.Xml.Linq;

using Xunit;

namespace MigrateAuth.Cli.Tests.Release;

/// <summary>
/// THE LICENSE FILE AT THE REPOSITORY ROOT IS THE LICENCE TEXT, UNEDITED, AND ITS FIRST LINE IS THE
/// COPYRIGHT NOTICE THE BUILD STAMPS INTO THE BINARY.
///
/// The text is compared by its SHA-256 against the reference copy of the licence text, so a stray edit, a byte-order mark or
/// a carriage return fails here before it reaches the published tree.
/// </summary>
public sealed class LicenseFileTests
{
	private const string _SOLUTION_FILE_PATTERN = "*.sln";
	private const string _PROJECT_RELATIVE_PATH = "src/MigrateAuth.Cli/MigrateAuth.Cli.csproj";
	private const string _LICENSE_FILE_NAME = "LICENSE";
	private const string _COPYRIGHT_ELEMENT = "Copyright";
	private const string _COMPANY_ELEMENT = "Company";
	private const string _EXPECTED_COMPANY = "NSerio";
	private const string _EXPECTED_COPYRIGHT = "Copyright (c) 2026 Relativity ODA LLC";
	private const string _EXPECTED_LICENSE_SHA256 = "2ac841ca2e0a91536b8079fc57ceb3a9e319726a071336c26876572c5b17bd18";
	private const byte _CARRIAGE_RETURN = 0x0D;
	private const byte _LINE_FEED = 0x0A;
	private static readonly byte[] _Utf8ByteOrderMark = [0xEF, 0xBB, 0xBF];

	[Fact]
	public void TheLicenseFileHoldsTheLicenceTextByItsHash()
	{
		string actualHash = Convert.ToHexString(SHA256.HashData(ReadLicenseBytes())).ToLowerInvariant();

		Assert.Equal(_EXPECTED_LICENSE_SHA256, actualHash);
	}

	[Fact]
	public void TheLicenseFileHasNoByteOrderMarkNoCarriageReturnAndAFinalLineFeed()
	{
		byte[] licenseBytes = ReadLicenseBytes();

		Assert.False(licenseBytes.AsSpan().StartsWith(_Utf8ByteOrderMark), "The licence file starts with a byte-order mark.");
		Assert.DoesNotContain(_CARRIAGE_RETURN, licenseBytes);
		Assert.Equal(_LINE_FEED, licenseBytes[^1]);
	}

	[Fact]
	public void TheFirstLineOfTheLicenseFileIsTheCopyrightTheProjectStamps()
	{
		string firstLine = File.ReadLines(LicensePath()).First();

		Assert.Equal(_EXPECTED_COPYRIGHT, ReadProjectProperty(_COPYRIGHT_ELEMENT));
		Assert.Equal(ReadProjectProperty(_COPYRIGHT_ELEMENT), firstLine);
	}

	[Fact]
	public void TheCompanyStaysTheOneThatBuildsTheTool()
	{
		Assert.Equal(_EXPECTED_COMPANY, ReadProjectProperty(_COMPANY_ELEMENT));
	}

	private static byte[] ReadLicenseBytes()
	{
		return File.ReadAllBytes(LicensePath());
	}

	private static string LicensePath()
	{
		return Path.Combine(LocateRepositoryRoot(), _LICENSE_FILE_NAME);
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
