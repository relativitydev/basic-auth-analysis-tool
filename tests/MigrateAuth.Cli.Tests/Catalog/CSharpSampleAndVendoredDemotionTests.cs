using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Recognition;
using MigrateAuth.Cli.Tests.Fixtures;
using MigrateAuth.Cli.Tests.Reading;
using MigrateAuth.Cli.Tests.Recognition;

using Xunit;

namespace MigrateAuth.Cli.Tests.Catalog;

/// <summary>
/// THE `CSharp` ANALYZER'S SAMPLE, EXAMPLE AND VENDORED-FOLDER DEMOTION SIGNALS,
/// THROUGH THE REAL EMBEDDED ANALYZER AND THE REAL RECOGNITION ENGINE.
///
/// The same High construct is recognized under different relative paths, so each level read is a
/// measurement of the path alone. Every source text is invented.
/// </summary>
public sealed class CSharpSampleAndVendoredDemotionTests
{
	private const string _HIGH_CONSTRUCT = "var result = client.WithBasicAuth(operatorName, accessKey);";
	private const string _CLIENT_FILE_NAME = "Client.cs";

	[Fact]
	public void TheSameConstructInAnOrdinaryFolderIsHigh()
	{
		Assert.Equal(ConfidenceLevel.High, LevelAt("src/Client.cs"));
	}

	[Fact]
	public void CodeInASamplesFolderIsLow()
	{
		Assert.Equal(ConfidenceLevel.Low, LevelAt("samples/Client.cs"));
	}

	[Theory]
	[InlineData("sample/Client.cs")]
	[InlineData("samples/Client.cs")]
	[InlineData("example/Client.cs")]
	[InlineData("examples/Client.cs")]
	[InlineData("src/Samples/Client.cs")]
	[InlineData("src/EXAMPLES/nested/Client.cs")]
	[InlineData("vendor/Client.cs")]
	[InlineData("src/Vendor/Client.cs")]
	[InlineData("third-party/Client.cs")]
	[InlineData("src/Third-Party/Client.cs")]
	public void AWholeSegmentFolderSignalDemotesInAnyCasing(string relativePath)
	{
		Assert.Equal(ConfidenceLevel.Low, LevelAt(relativePath));
	}

	[Theory]
	[InlineData("src/SamplesCore/Client.cs")]
	[InlineData("src/Sampler/Client.cs")]
	[InlineData("src/ExampleApp/Client.cs")]
	[InlineData("src/VendorPortal/Client.cs")]
	[InlineData("src/third-party-notes/Client.cs")]
	[InlineData("src/third_party/Client.cs")]
	public void AFolderThatOnlyContainsTheWordIsNotDemoted(string relativePath)
	{
		Assert.Equal(ConfidenceLevel.High, LevelAt(relativePath));
	}

	[Theory]
	[InlineData("src/SampleClient.cs")]
	[InlineData("src/ExampleClient.cs")]
	[InlineData("src/Sample.cs")]
	[InlineData("src/SampleRateConverter.cs")]
	public void AFileNameBeginningWithSampleOrExampleIsLow(string relativePath)
	{
		Assert.Equal(ConfidenceLevel.Low, LevelAt(relativePath));
	}

	[Theory]
	[InlineData("src/RateSampleReader.cs")]
	[InlineData("src/CounterExample.cs")]
	[InlineData("src/Vendor.cs")]
	[InlineData("src/ThirdParty.cs")]
	public void AFileNameThatOnlyContainsTheWordFurtherInIsNotDemoted(string relativePath)
	{
		Assert.Equal(ConfidenceLevel.High, LevelAt(relativePath));
	}

	[Theory]
	[InlineData("target")]
	[InlineData(".venv")]
	[InlineData("venv")]
	[InlineData("__pycache__")]
	public void TheFormerWalkExtensionNamesAreNeitherExcludedNorDemoted(string directoryName)
	{
		Assert.Equal(ConfidenceLevel.High, LevelAt($"{directoryName}/{_CLIENT_FILE_NAME}"));
	}

	/// <summary>The file under <c>vendor/</c> is reached by the walk, read, and then yields Low from the same path the walk reported.</summary>
	[Fact]
	public void AFileUnderVendorIsWalkedAndYieldsLow()
	{
		using RepositoryFixture fixture = RepositoryFixture.CreateEmpty();
		string relativePath = $"vendor/{_CLIENT_FILE_NAME}";

		fixture.WriteText(relativePath, _HIGH_CONSTRUCT);

		string walkedPath = Assert.Single(fixture.WalkedPaths());
		Assert.Equal(relativePath, walkedPath);
		Assert.Equal(ConfidenceLevel.Low, LevelAt(walkedPath));
	}

	[Fact]
	public void AFileUnderThirdPartyIsWalkedAndYieldsLow()
	{
		using RepositoryFixture fixture = RepositoryFixture.CreateEmpty();
		string relativePath = $"third-party/{_CLIENT_FILE_NAME}";

		fixture.WriteText(relativePath, _HIGH_CONSTRUCT);

		string walkedPath = Assert.Single(fixture.WalkedPaths());
		Assert.Equal(ConfidenceLevel.Low, LevelAt(walkedPath));
	}

	private static ConfidenceLevel LevelAt(string relativePath)
	{
		RecognizedConstruct construct = Assert.Single(new CSharpRecognitionHarness().Recognize(_HIGH_CONSTRUCT, relativePath));

		return construct.Level;
	}
}
