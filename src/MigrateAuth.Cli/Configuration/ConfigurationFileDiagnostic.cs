namespace MigrateAuth.Cli.Configuration;

/// <summary>What one thing noticed about a configuration file was.</summary>
public enum ConfigurationDiagnosticKind
{
	/// <summary>The text is not JSON. Carries the line and position the parser stopped at, when it knows them.</summary>
	InvalidJson = 0,

	/// <summary>It is JSON, and the top-level value is not an object.</summary>
	NotAnObject = 1,

	/// <summary>A key this version reads holds a value it cannot use. The file is still applied, and that key is ignored as if it were absent.</summary>
	InvalidValue = 2,

	/// <summary>The file is larger than the size this tool reads at all.</summary>
	TooLarge = 3,

	/// <summary>A key this version does not read. The file is still applied, and the key is ignored.</summary>
	UnknownKey = 4,

	/// <summary>A language name this version does not know. The file is still applied, and the name is ignored.</summary>
	UnknownLanguage = 5
}

/// <summary>
/// One specific thing noticed while reading one configuration file: why a file was found
/// malformed, or what was skipped in a file that was applied.
///
/// IT IS FOR THE CONSOLE AND NEVER FOR THE REPORT. <see cref="Subject"/> and <see cref="Value"/>
/// are text the user typed - a key, a language name, a level - already stripped of characters
/// that can act on a terminal and cut to a fixed length. Text from the audited
/// repository out of the report and the data artifact unless it belongs to this product's closed
/// vocabularies, and a typed key does not, so this record deliberately lives on
/// <see cref="EffectiveConfiguration"/> and not inside <see cref="ConfigurationProvenance"/>,
/// which the renderers read.
/// </summary>
public sealed record ConfigurationFileDiagnostic
{
	/// <summary>Which of the two files.</summary>
	public required ConfigurationFileLocation Location { get; init; }

	/// <summary>What was noticed.</summary>
	public required ConfigurationDiagnosticKind Kind { get; init; }

	/// <summary>The key or language name concerned, or empty when the diagnostic is about the file as a whole.</summary>
	public string Subject { get; init; } = string.Empty;

	/// <summary>The offending value of <see cref="Subject"/>, or empty when repeating it would add nothing.</summary>
	public string Value { get; init; } = string.Empty;

	/// <summary>The line the parser stopped at, counted from 1, when known.</summary>
	public long? LineNumber { get; init; }

	/// <summary>The position in that line the parser stopped at, counted from 1, when known.</summary>
	public long? PositionInLine { get; init; }
}
