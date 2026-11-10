using System.Collections.Frozen;

namespace MigrateAuth.Cli.Catalog;

/// <summary>
/// OPTIONAL. The dialects a language's files may be read in, declared as data. A dialect is an opaque
/// identifier the engine never interprets: it compares identifiers and nothing else, so what a dialect
/// MEANS (which constructs a flow signal is evidence of under it) is the catalog's declaration on
/// <see cref="FlowSignal.Dialects"/> and <see cref="FlowSignal.ExcludedDialects"/>.
///
/// The engine resolves ONE dialect per file, in this order: the interpreter named by the file's
/// first line when it starts with <c>#!</c> and is a key of <see cref="ShebangInterpreters"/>; then the
/// file's extension when it is a key of <see cref="ExtensionDialects"/>; then <see cref="DefaultDialect"/>.
/// A language that declares no <see cref="FileDialects"/> has no dialect, and every flow signal that names
/// an included dialect never fires for it.
/// </summary>
public sealed record FileDialects
{
	/// <summary>The dialect a file reads in when neither its first line nor its extension names another.</summary>
	public required string DefaultDialect { get; init; }

	/// <summary>
	/// Interpreter base name (without directory and without a <c>.exe</c> suffix) to dialect identifier.
	/// Ordinal. Unordered by type, like every collection of the contract.
	/// </summary>
	public FrozenDictionary<string, string> ShebangInterpreters { get; init; } = FrozenDictionary<string, string>.Empty;

	/// <summary>
	/// Lower-case extension, leading dot included, to dialect identifier. The engine lower-cases the file's
	/// extension before it looks it up.
	/// </summary>
	public FrozenDictionary<string, string> ExtensionDialects { get; init; } = FrozenDictionary<string, string>.Empty;
}
