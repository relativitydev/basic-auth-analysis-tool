namespace MigrateAuth.Cli.Configuration;

/// <summary>
/// The persisted shape of <c>.migrate-auth.json</c>, holding three fields:
/// enabled languages, confidence thresholds and output paths.
///
/// Every member is nullable because absent and empty are different answers here. An absent
/// <see cref="EnabledLanguages"/> means "this file says nothing about languages" and falls
/// through to the next source; an empty array means "none", which is the configuration
/// Pre-flight Validation refuses to start a session on. Collapsing the two would make the second
/// unrepresentable.
///
/// THIS DOCUMENT IS THE TOLERANT READ, and the contrast with the session state file is
/// deliberate. The configuration read is tolerant of unknown fields and of version differences:
/// a field a later release adds is ignored rather than making the whole file
/// malformed. <see cref="ConfigurationFileReader"/> builds this record key by key from the parsed
/// file, so that a key holding a value it cannot use is ignored on its own as well, and the file's
/// other keys still apply. The session state document is the hostile read and disallows exactly
/// that, because it is state this product wrote and claims to have validated.
///
/// The property names, in camel case, are the file's keys.
///
/// Both files are nonetheless untrusted input. One of the two locations this document is read
/// from is the root of the repository under assessment, which is the object being audited.
/// </summary>
internal sealed record ConfigurationDocument
{
	/// <summary>
	/// The canonical names of the languages to assess, per
	/// <see cref="Catalog.AssessedLanguages.CanonicalName"/>. A name this build does not know is
	/// ignored rather than rejected, so that a file written for a later release still loads.
	/// </summary>
	public string[]? EnabledLanguages { get; init; }

	/// <summary>
	/// The confidence threshold, expressed as the lowest level that appears in output. The
	/// vocabulary is fixed - the same three confidence levels.
	///
	/// It stays strictly outside the rubric: the level a finding receives is assigned by
	/// the rubric alone and this value only decides which assigned levels are shown. A threshold
	/// that participated in assignment would make determinism and the recall denominator
	/// depend on a configuration file.
	/// </summary>
	public string? MinimumConfidenceLevel { get; init; }

	/// <summary>
	/// Where the report and the data artifact are written. Read from the user profile
	/// only: a copy of it in the repository's own file is recorded as ignored, because the
	/// audited object does not decide where its audit lands.
	/// </summary>
	public string? OutputPath { get; init; }

	/// <summary>
	/// The flow a finding is assigned when no flow signal decided it: <c>ClientCredentials</c> or
	/// <c>AuthorizationCode</c>. Read from the user profile only, on the output path's shape - a
	/// repository that set it would change the recommendation of every defaulted finding it holds.
	/// The value is validated on the profile read path alone: a value in the repository's
	/// file is recorded as present and never inspected.
	/// </summary>
	public string? DefaultFlow { get; init; }
}
