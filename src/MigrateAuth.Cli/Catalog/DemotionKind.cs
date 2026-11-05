namespace MigrateAuth.Cli.Catalog;

/// <summary>
/// What a file demotion signal says about the file it fires on. Every file name, folder and content
/// signal declares one; <see cref="Undeclared"/> is the zero value that a validator refuses, so a
/// signal that forgot to say cannot ship. A generation marker has no field for it: its kind is
/// <see cref="Generated"/> by the definition of its shape.
/// </summary>
public enum DemotionKind
{
	Undeclared = 0,
	Test,
	Mock,
	Sample,
	Vendored,
	Generated,
	Deployment
}

/// <summary>The set of <see cref="DemotionKind"/> values that fired on one file, one bit per kind.</summary>
[Flags]
public enum DemotionKinds
{
	None = 0,
	Test = 1 << 0,
	Mock = 1 << 1,
	Sample = 1 << 2,
	Vendored = 1 << 3,
	Generated = 1 << 4,
	Deployment = 1 << 5
}
