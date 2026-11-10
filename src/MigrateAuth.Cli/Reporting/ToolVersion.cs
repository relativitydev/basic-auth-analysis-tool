namespace MigrateAuth.Cli.Reporting;

/// <summary>
/// The product's own version, declared as a constant.
///
/// The report and the data artifact record the tool version alongside the catalog version
/// (<see cref="Catalog.CatalogVersion"/>); the two are numbered independently.
///
/// It is not read from the assembly. The assembly's informational version is reachable only through
/// reflection over attributes, and under ahead-of-time compilation a reflective path can compile,
/// publish, and fail on the client's machine. A constant is read at build time by the compiler, and a
/// single constant read from both renderers is the same number in both artifacts.
/// </summary>
public static class ToolVersion
{
	/// <summary>The version printed in the report and carried in the data artifact.</summary>
	public const string CURRENT = "0.1.1-alpha";
}
