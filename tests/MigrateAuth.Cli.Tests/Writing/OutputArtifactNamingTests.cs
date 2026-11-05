using System.Globalization;

using MigrateAuth.Cli.Writing;

using Xunit;

namespace MigrateAuth.Cli.Tests.Writing;

/// <summary>
/// The file name pattern both produced files share: stem, ISO date, an optional numeric suffix
/// from the second collision on, and the extension of the kind.
/// </summary>
public sealed class OutputArtifactNamingTests
{
	private static readonly DateOnly _artifactDate = new(2026, 3, 9);

	[Fact]
	public void TheFirstReportNameCarriesNoSuffix()
	{
		string name = OutputArtifactNaming.BuildFileName(OutputArtifactKind.MarkdownReport, _artifactDate, OutputArtifactNaming.UNSUFFIXED_ORDINAL);

		Assert.Equal("basic-auth-migration-2026-03-09.md", name);
	}

	[Fact]
	public void TheFirstDataArtifactNameCarriesNoSuffix()
	{
		string name = OutputArtifactNaming.BuildFileName(OutputArtifactKind.StructuredDataArtifact, _artifactDate, OutputArtifactNaming.UNSUFFIXED_ORDINAL);

		Assert.Equal("basic-auth-migration-2026-03-09.json", name);
	}

	[Theory]
	[InlineData(2, "basic-auth-migration-2026-03-09-2.md")]
	[InlineData(3, "basic-auth-migration-2026-03-09-3.md")]
	[InlineData(12, "basic-auth-migration-2026-03-09-12.md")]
	public void ALaterOrdinalAddsItsNumberBeforeTheExtension(int ordinal, string expectedName)
	{
		Assert.Equal(expectedName, OutputArtifactNaming.BuildFileName(OutputArtifactKind.MarkdownReport, _artifactDate, ordinal));
	}

	[Fact]
	public void TheFirstCollisionSuffixIsTwo()
	{
		Assert.Equal(2, OutputArtifactNaming.FIRST_COLLISION_SUFFIX);
		Assert.EndsWith(
			"-2.json",
			OutputArtifactNaming.BuildFileName(OutputArtifactKind.StructuredDataArtifact, _artifactDate, OutputArtifactNaming.FIRST_COLLISION_SUFFIX));
	}

	[Fact]
	public void TheNameDoesNotDependOnTheCurrentCulture()
	{
		CultureInfo original = CultureInfo.CurrentCulture;

		try
		{
			CultureInfo.CurrentCulture = new CultureInfo("ar-SA");

			string name = OutputArtifactNaming.BuildFileName(OutputArtifactKind.MarkdownReport, _artifactDate, 12);

			Assert.Equal("basic-auth-migration-2026-03-09-12.md", name);
		}
		finally
		{
			CultureInfo.CurrentCulture = original;
		}
	}

	[Theory]
	[InlineData(0)]
	[InlineData(-1)]
	public void AnOrdinalBelowTheFirstIsRefused(int ordinal)
	{
		Assert.Throws<ArgumentOutOfRangeException>(
			() => OutputArtifactNaming.BuildFileName(OutputArtifactKind.MarkdownReport, _artifactDate, ordinal));
	}

	[Fact]
	public void AKindOutsideTheDeclaredSetIsRefused()
	{
		Assert.Throws<ArgumentOutOfRangeException>(
			() => OutputArtifactNaming.BuildFileName((OutputArtifactKind)int.MaxValue, _artifactDate, OutputArtifactNaming.UNSUFFIXED_ORDINAL));
	}
}
