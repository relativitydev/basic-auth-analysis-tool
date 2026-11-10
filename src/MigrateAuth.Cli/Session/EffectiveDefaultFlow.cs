using MigrateAuth.Cli.Configuration;
using MigrateAuth.Cli.Findings;

namespace MigrateAuth.Cli.Session;

/// <summary>
/// The default flow Analyze applied when it decided the flows, with where it came from. Held
/// on the session state and persisted with it, so that a report or a suggestion produced in a
/// resumed session describes the default the persisted decisions were made under, and not whatever
/// the configuration says by the time the session resumes.
/// </summary>
/// <param name="Flow">The effective default flow.</param>
/// <param name="Origin">
/// Where it came from: the built-in default or the user profile. The repository's own file is never
/// a source.
/// </param>
public readonly record struct EffectiveDefaultFlow(CredentialFlow Flow, ConfigurationSource Origin)
{
	/// <summary>What a session holds until Analyze has decided the flows: the built-in default.</summary>
	public static EffectiveDefaultFlow BuiltIn { get; } =
		new(CredentialFlows.BUILT_IN_DEFAULT, ConfigurationSource.BuiltInDefault);
}
