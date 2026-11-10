using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Text.RegularExpressions;

namespace MigrateAuth.Cli.Catalog;

/// <summary>How a file name signal is compared against a file's name.</summary>
public enum FileNameMatch
{
	Suffix = 0,
	Prefix = 1,
	ExactName = 2
}

/// <summary>
/// How a folder name signal is compared against a path segment. A suffix match reaches the
/// .NET project-folder convention - a segment such as <c>Product.Core.Tests</c> - which is a
/// suffix on the whole segment rather than the whole segment itself.
/// </summary>
public enum FolderNameMatch
{
	ExactName = 0,
	Suffix = 1
}

/// <summary>A language's own file naming convention: its test files, its generated files.</summary>
public sealed record FileNameSignal
{
	public required FileNameMatch Match { get; init; }

	public required string Text { get; init; }

	public CaseSensitivity Sensitivity { get; init; } = CaseSensitivity.Sensitive;
}

/// <summary>A folder convention belonging to the language itself, not to repository layout.</summary>
public sealed record FolderNameSignal
{
	public required string FolderName { get; init; }

	/// <summary>
	/// Exact name by default. <see cref="FolderNameMatch.Suffix"/> reaches the .NET
	/// project-folder convention - a whole segment ending in, for example, <c>.Tests</c> - which an
	/// exact-name comparison can never match.
	/// </summary>
	public FolderNameMatch Match { get; init; } = FolderNameMatch.ExactName;

	public CaseSensitivity Sensitivity { get; init; } = CaseSensitivity.Sensitive;

	/// <summary>
	/// Whether this folder names third-party code the repository carries rather than its own. It
	/// changes nothing about the level a firing signal proposes - Low, like every other one - and
	/// only which reason the report gives: <see cref="LowConfidenceReasons.DemotedVendoredFile"/>
	/// instead of <see cref="LowConfidenceReasons.DemotedFile"/>.
	/// </summary>
	public bool MarksVendoredCode { get; init; }
}

/// <summary>
/// Where a <see cref="ContentMarker"/> is permitted to fire: anywhere in the decoded text, or only
/// where the scanner's mask reads <see cref="MigrateAuth.Cli.Lexical.LexicalZone.Code"/>. A generated-code header
/// is itself a comment and needs <see cref="AnyZone"/> to be reachable at all; a test-attribute-like
/// token must not demote a file merely because the token appears inside a comment or a string, which
/// is what <see cref="CodeOnly"/> is for.
/// </summary>
public enum ContentMarkerZone
{
	AnyZone = 0,
	CodeOnly = 1
}

/// <summary>
/// One content-shaped demotion signal: an expression the engine evaluates over the file's
/// decoded text, never the analyzer, and the zone it is permitted to fire in. Every marker
/// declares a zone; there is no default.
/// </summary>
public sealed record ContentMarker
{
	public required Regex Expression { get; init; }

	public required ContentMarkerZone Zone { get; init; }
}

/// <summary>
/// Contract element 4. File demotion signals, language-shaped only: the language's own name,
/// folder, generation-marker and content rules.
///
/// The path-shaped, language-neutral signals belong to the shared engine and are not suppliable here.
///
/// <see cref="FileNameSignals"/>, <see cref="FolderNameSignals"/> and
/// <see cref="GenerationMarkerExpressions"/> are name-based data and not predicates: an analyzer
/// cannot be handed a path to inspect, and nothing here can see an absolute path, because the
/// engine evaluates these against a file name and its folder segments alone.
///
/// <see cref="ContentMarkers"/> is the one field evaluated against the file's decoded text. The
/// analyzer never receives the file: it hands over an already-constructed expression and a
/// declared zone, and only the engine - <see cref="MigrateAuth.Cli.Recognition.FileDemotionSignals"/>,
/// inside its caller's existing match-bound try - ever calls that expression against content.
/// </summary>
public sealed class LanguageShapedDemotionSignals
{
	/// <summary>A language that declares no signal of its own. Not an empty contract element.</summary>
	public static LanguageShapedDemotionSignals NoneDeclared { get; } = new()
	{
		FileNameSignals = FrozenSet<FileNameSignal>.Empty,
		FolderNameSignals = FrozenSet<FolderNameSignal>.Empty,
		GenerationMarkerExpressions = [],
		ContentMarkers = []
	};

	public required FrozenSet<FileNameSignal> FileNameSignals { get; init; }

	public required FrozenSet<FolderNameSignal> FolderNameSignals { get; init; }

	/// <summary>
	/// Generation markers, handed over already constructed. Evaluated as a disjunction, so
	/// their position in this sequence carries no meaning and cannot reach output. Evaluated
	/// against the file name and folder segments; <see cref="ContentMarkers"/> is the separate, content-evaluated field.
	/// </summary>
	public required ImmutableArray<Regex> GenerationMarkerExpressions { get; init; }

	/// <summary>
	/// Content-shaped demotion signals. Evaluated as a disjunction over the file's decoded text,
	/// one <see cref="ContentMarkerZone"/> per marker, with no short circuit among them: every
	/// marker runs whatever its position in this array.
	/// </summary>
	public required ImmutableArray<ContentMarker> ContentMarkers { get; init; }
}
