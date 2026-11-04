using System.Collections.Frozen;
using System.Text.RegularExpressions;

namespace MigrateAuth.Cli.Catalog;

/// <summary>
/// Contract element 2. How a finding's language value is derived for a file this analyzer owns.
/// The profile-to-language relation is not one to one - two languages may share one lexical
/// profile - so the derivation is declared rather than assumed, and the first line may
/// override the extension.
/// </summary>
/// <param name="fileExtension">The file's extension, including its leading dot.</param>
/// <param name="firstLine">The file's first line, or null when it could not be read.</param>
public delegate string LanguageNameDerivation(string fileExtension, string? firstLine);

/// <summary>
/// Contract element 2. Extension-to-language mapping, the first-line detection rule, and the
/// derivation that turns the two into the language name a finding carries. Without the
/// derivation, findings in one language would be emitted under the other's name.
///
/// Extensions and language names are unordered sets. An analyzer cannot express a precedence
/// between them, because precedence is a decision about output and output is not its business.
/// </summary>
public sealed class LanguageIdentification
{
	/// <summary>The extensions this analyzer owns, each including its leading dot.</summary>
	public required FrozenSet<string> FileExtensions { get; init; }

	/// <summary>
	/// Every language name this analyzer may emit. More than one entry is the declaration
	/// that two languages share this profile.
	/// </summary>
	public required FrozenSet<string> DeclaredLanguageNames { get; init; }

	/// <summary>
	/// The first-line detection rule, handed over already constructed, or
	/// null where the extension settles the question on its own.
	/// </summary>
	public Regex? FirstLineDetectionExpression { get; init; }

	/// <summary>How the emitted language value is derived. Must return a declared name.</summary>
	public required LanguageNameDerivation LanguageNameDerivation { get; init; }

	/// <summary>
	/// What this analyzer's catalog declares it does NOT attempt to recognize, and
	/// why - the declared-unsupported discipline as a field of the contract. Optional, and empty by
	/// default: an analyzer with nothing to declare is a real state, not an omission.
	///
	/// Unordered, because an analyzer never orders. The report and the artifact sort it
	/// themselves, ordinally by <see cref="DeclaredUnsupportedCoverage.Construct"/>, at
	/// render time.
	///
	/// Where two languages share this one profile (this element's own not-one-to-one relation),
	/// every declared language name this analyzer emits shows the same entries: the coverage is a
	/// property of the analyzer's catalog, not of which of its declared names a particular file
	/// happened to resolve to.
	/// </summary>
	public FrozenSet<DeclaredUnsupportedCoverage> DeclaredUnsupportedCoverage { get; init; } =
		FrozenSet<DeclaredUnsupportedCoverage>.Empty;
}
