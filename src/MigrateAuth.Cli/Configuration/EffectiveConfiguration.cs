using System.Collections.Immutable;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Findings;

namespace MigrateAuth.Cli.Configuration;

/// <summary>
/// The configuration this session runs under, resolved once and never again.
///
/// This is a SESSION-SCOPED CONSTANT and that is a behavioural guarantee rather than an
/// implementation convenience. Pre-flight Validation runs once, before <c>scan</c> becomes
/// available, and is not repeated per stage invocation - so the two checks
/// it performs are performed against this value, and every later stage reads this same value
/// rather than the file. A configuration re-read per stage would make a session's behaviour
/// change underneath it when the audited repository edits its own file mid-run, and would make
/// the provenance record a statement about one stage instead of about the session.
///
/// It is therefore immutable, it is constructed only by <see cref="ConfigurationResolver"/>, and
/// it is passed down rather than looked up.
///
/// WHAT IT DOES NOT DO: it does not filter anything. <see cref="MinimumConfidenceLevel"/> is the
/// effective value and its provenance; applying it is the report's, because the filter comes
/// after the rubric has assigned every level and after every finding has been numbered.
/// There is no rubric in this type to filter.
/// </summary>
public sealed record EffectiveConfiguration
{
	/// <summary>
	/// Every one of the eight languages and whether this session assesses it. All eight are
	/// present always: zero findings in a language must be distinguishable from that
	/// language being disabled, and the coverage statement iterates this enumeration rather
	/// than the catalog registry so that a language with no analyzer is named rather than absent.
	/// </summary>
	public required ImmutableArray<LanguageEnablement> LanguageEnablement { get; init; }

	/// <summary>
	/// The lowest confidence level that appears in output. An output filter only - never an input
	/// to the rubric.
	/// </summary>
	public required ConfidenceLevel MinimumConfidenceLevel { get; init; }

	/// <summary>
	/// The output destination supplied by the command line or by the user profile, or null when
	/// neither did. Null resolves to the working directory captured at launch,
	/// which <see cref="Session.SessionStartup"/> owns - the fallback is not re-derived here.
	///
	/// It is never read from the repository's own file, so the audited repository cannot decide
	/// where its audit lands.
	/// </summary>
	public string? OutputPathText { get; init; }

	/// <summary>
	/// The flow a finding is assigned when no flow signal decided it. Read from the user profile
	/// only and never from the repository's own file, on the output path's shape; its origin is
	/// <see cref="ConfigurationProvenance.DefaultFlowOrigin"/>. Client Credentials unless the
	/// profile says otherwise.
	/// </summary>
	public CredentialFlow DefaultFlow { get; init; } = CredentialFlows.BUILT_IN_DEFAULT;

	/// <summary>Where each field came from, and what the repository's file asked for in vain.</summary>
	public required ConfigurationProvenance Provenance { get; init; }

	/// <summary>
	/// What was noticed about the two files while reading them: why one was malformed, and which
	/// keys and language names an applied one carried that this version skips. Text the user typed
	/// can appear in it, so it is for the console only and is kept out of
	/// <see cref="Provenance"/>, which the report and the data artifact render.
	/// </summary>
	public ImmutableArray<ConfigurationFileDiagnostic> FileDiagnostics { get; init; } = [];

	/// <summary>
	/// Whether this session assesses anything at all. The language check reads exactly this, and
	/// it is false only when a configuration disabled every one of the eight.
	/// </summary>
	public bool HasAnyEnabledLanguage => LanguageEnablement.Any(enablement => enablement.IsEnabled);

	/// <summary>How many of the eight this session assesses.</summary>
	public int EnabledLanguageCount => LanguageEnablement.Count(enablement => enablement.IsEnabled);

	/// <summary>Whether this session assesses one particular language.</summary>
	public bool IsEnabled(AssessedLanguage language) =>
		LanguageEnablement.Any(enablement => enablement.Language == language && enablement.IsEnabled);

	/// <summary>
	/// The enabled languages, in <see cref="AssessedLanguages.AllInDeclaredOrder"/>'s
	/// order - what <see cref="Session.SessionState.RecordScanOutcome"/> persists as the set Scan's
	/// own candidate-selection pass actually used, and what a comparison against a later session's
	/// configuration is made against.
	/// </summary>
	public ImmutableArray<AssessedLanguage> EnabledLanguagesInDeclaredOrder =>
	[
		.. LanguageEnablement.Where(enablement => enablement.IsEnabled).Select(enablement => enablement.Language)
	];

	/// <summary>
	/// The same set as <see cref="EnabledLanguagesInDeclaredOrder"/>, as a set rather
	/// than an ordered sequence. IT IS BUILT ON EVERY ACCESS and not cached: this is a record, and a
	/// copy made with a <c>with</c> expression would carry a cached set that no longer matches its
	/// changed <see cref="LanguageEnablement"/>. A caller that consults it per file reads the
	/// property once and holds the result, as Scan does for its per-file candidate-selection gate.
	/// </summary>
	public IReadOnlySet<AssessedLanguage> EnabledLanguageSet => EnabledLanguagesInDeclaredOrder.ToHashSet();
}
