namespace MigrateAuth.Cli.Catalog;

/// <summary>
/// The catalog's own version number. The catalog is a versioned artifact with a life
/// of its own: it is numbered independently of the product, and that number is printed in all
/// output.
///
/// It is a value and not a string, so that comparing two of them is a comparison of numbers
/// rather than of formatting. The comparison is what a later session needs: a session state
/// produced under a different catalog version starts a fresh session.
/// </summary>
public readonly record struct CatalogVersion : IComparable<CatalogVersion>
{
	private const string _COMPONENT_SEPARATOR = ".";

	public CatalogVersion(int major, int minor, int revision)
	{
		ArgumentOutOfRangeException.ThrowIfNegative(major);
		ArgumentOutOfRangeException.ThrowIfNegative(minor);
		ArgumentOutOfRangeException.ThrowIfNegative(revision);

		Major = major;
		Minor = minor;
		Revision = revision;
	}

	public int Major { get; }

	public int Minor { get; }

	public int Revision { get; }

	/// <summary>The form printed in output.</summary>
	public string DisplayValue => $"{Major}{_COMPONENT_SEPARATOR}{Minor}{_COMPONENT_SEPARATOR}{Revision}";

	public int CompareTo(CatalogVersion other)
	{
		int comparison = Major.CompareTo(other.Major);

		if (comparison == 0)
		{
			comparison = Minor.CompareTo(other.Minor);
		}

		if (comparison == 0)
		{
			comparison = Revision.CompareTo(other.Revision);
		}

		return comparison;
	}

	public override string ToString()
	{
		return DisplayValue;
	}

	public static bool operator <(CatalogVersion left, CatalogVersion right) => left.CompareTo(right) < 0;

	public static bool operator <=(CatalogVersion left, CatalogVersion right) => left.CompareTo(right) <= 0;

	public static bool operator >(CatalogVersion left, CatalogVersion right) => left.CompareTo(right) > 0;

	public static bool operator >=(CatalogVersion left, CatalogVersion right) => left.CompareTo(right) >= 0;
}
