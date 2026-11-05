using System.Reflection;

using MigrateAuth.Cli.Catalog;

using Xunit;

namespace MigrateAuth.Cli.Tests.Catalog;

/// <summary>
/// The catalog version travels with the catalog type: a catalog version is a property of a
/// catalog.
/// </summary>
public sealed class CatalogIdentityTests
{
	private const int _ARBITRARY_MAJOR = 3;
	private const int _ARBITRARY_MINOR = 7;
	private const int _ARBITRARY_REVISION = 2;
	private const string _ARBITRARY_DISPLAY_VALUE = "3.7.2";
	private const int _NEGATIVE_COMPONENT = -1;

	[Fact]
	public void ACatalogCannotBeConstructedWithoutAVersion()
	{
		ConstructorInfo[] constructors = typeof(LanguageCatalog).GetConstructors(BindingFlags.Public | BindingFlags.Instance);

		Assert.NotEmpty(constructors);
		Assert.All(constructors, constructor => Assert.Contains(
			constructor.GetParameters(),
			parameter => parameter.ParameterType == typeof(CatalogVersion)));
	}

	[Fact]
	public void EveryCatalogInstanceCarriesTheVersionItWasStampedWith()
	{
		CatalogVersion stamped = new(_ARBITRARY_MAJOR, _ARBITRARY_MINOR, _ARBITRARY_REVISION);

		LanguageCatalog catalog = new(stamped, []);

		Assert.Equal(stamped, catalog.Version);
	}

	[Fact]
	public void TheEmbeddedCatalogIsStampedWithTheCurrentCatalogVersion()
	{
		LanguageCatalog catalog = LanguageCatalogRegistry.BuildEmbeddedCatalog();

		Assert.Equal(LanguageCatalog.CurrentVersion, catalog.Version);
	}

	[Fact]
	public void TheVersionIsPrintableAsASingleNumberForOutput()
	{
		CatalogVersion version = new(_ARBITRARY_MAJOR, _ARBITRARY_MINOR, _ARBITRARY_REVISION);

		Assert.Equal(_ARBITRARY_DISPLAY_VALUE, version.DisplayValue);
		Assert.Equal(_ARBITRARY_DISPLAY_VALUE, version.ToString());
	}

	/// <summary>
	/// A session state produced under another catalog version starts a fresh session. That
	/// decision is a comparison of two versions, so the version has to be comparable rather than
	/// merely printable.
	/// </summary>
	[Fact]
	public void TwoVersionsCompareByComponentRatherThanByText()
	{
		CatalogVersion earlier = new(1, 9, 0);
		CatalogVersion later = new(1, 10, 0);

		Assert.True(earlier < later);
		Assert.NotEqual(earlier, later);
	}

	[Fact]
	public void AVersionComponentCannotBeNegative()
	{
		Assert.Throws<ArgumentOutOfRangeException>(
			() => new CatalogVersion(_NEGATIVE_COMPONENT, _ARBITRARY_MINOR, _ARBITRARY_REVISION));
	}
}
