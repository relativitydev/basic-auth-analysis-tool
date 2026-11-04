namespace MigrateAuth.Cli.Reporting;

/// <summary>
/// THE PRODUCT'S OWN VERSION, DECLARED AS A CONSTANT BECAUSE NOTHING ELSE COULD SUPPLY IT.
///
/// The report and the data artifact record the tool version alongside the
/// catalog version, and the two are numbered independently: the catalog
/// is a versioned artifact with a life of its own, numbered in <see cref="Catalog.CatalogVersion"/>.
///
/// IT IS NOT READ FROM THE ASSEMBLY. The assembly's informational version is reachable only
/// through reflection over attributes, and under ahead-of-time compilation a reflective path
/// compiles, publishes, and fails on the client's machine. A constant is read at build time by the
/// compiler and cannot do that, and a single constant read from two renderers is the same number
/// in both artifacts.
/// </summary>
public static class ToolVersion
{
	/// <summary>The version printed in the report and carried in the data artifact.</summary>
	public const string CURRENT = "0.3.0-alpha";
}
