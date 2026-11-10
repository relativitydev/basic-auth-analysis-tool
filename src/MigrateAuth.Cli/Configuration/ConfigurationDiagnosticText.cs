using System.Globalization;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Findings;

namespace MigrateAuth.Cli.Configuration;

/// <summary>
/// The one line that says what was wrong with a configuration file, or what an applied file
/// carried that this version skips.
///
/// A malformed file gets the degradation message and then exactly one of the detail lines here, so
/// the user is told which part to fix instead of only that something is wrong. A skipped key or
/// language is a warning and not a refusal: keys and language names this version does not know
/// are ignored so that a file written for a later release still loads, and the warning is what
/// stops a misspelt key from being ignored without anyone noticing. A known key holding a value
/// this version cannot use is a warning too: that key alone is ignored, and the warning says the
/// rest of the file is applied, so that nobody reads it as the whole file being dropped.
///
/// These lines are console text. They repeat a key, a language name or a value the user typed,
/// already made safe by <see cref="Session.ConsoleTextSafety"/>, and they are never rendered into
/// the report or the data artifact.
/// </summary>
public static class ConfigurationDiagnosticText
{
	/// <summary>A syntax error, with the place the parser stopped.</summary>
	public const string INVALID_JSON_WITH_POSITION_MESSAGE_FORMAT =
		"Detail: the file is not valid JSON; the parser stopped at line {0}, position {1}.";

	/// <summary>A syntax error the parser gave no place for.</summary>
	public const string INVALID_JSON_MESSAGE = "Detail: the file is not valid JSON.";

	/// <summary>Valid JSON whose top-level value is not an object.</summary>
	public const string NOT_AN_OBJECT_MESSAGE = "Detail: the top-level value of the file must be a JSON object.";

	/// <summary>A file above the size this tool reads.</summary>
	public const string TOO_LARGE_MESSAGE_FORMAT =
		"Detail: the file is too large; this tool reads a configuration file of at most {0} bytes.";

	/// <summary>A wrong value for a key, when the value is not repeated. Only that key is ignored.</summary>
	public const string MUST_BE_MESSAGE_FORMAT =
		"Warning: in the configuration file {0}, {1} must be {2}; that key is ignored and the rest of the file is applied.";

	/// <summary>A wrong value for a key, repeating what was typed. Only that key is ignored.</summary>
	public const string IS_BUT_MUST_BE_MESSAGE_FORMAT =
		"Warning: in the configuration file {0}, {1} is \"{2}\"; it must be {3}. That key is ignored and the rest of the file is applied.";

	/// <summary>A key this version does not use.</summary>
	public const string UNKNOWN_KEY_MESSAGE_FORMAT =
		"Warning: the configuration file {0} has the key \"{1}\", which this version does not use; it is ignored.";

	/// <summary>A language name this version does not know.</summary>
	public const string UNKNOWN_LANGUAGE_MESSAGE_FORMAT =
		"Warning: the configuration file {0} names the language \"{1}\", which this version does not know; it is ignored.";

	private const string _ENABLED_LANGUAGES_REQUIREMENT = "an array of language names";
	private const string _TEXT_REQUIREMENT = "a text value";
	private const string _LAST_ALTERNATIVE_SEPARATOR = " or ";
	private const string _ALTERNATIVE_SEPARATOR = ", ";

	/// <summary>The line for one diagnostic.</summary>
	public static string Describe(ConfigurationFileDiagnostic diagnostic)
	{
		ArgumentNullException.ThrowIfNull(diagnostic);

		string location = PreflightValidation.DescribeLocation(diagnostic.Location);

		return diagnostic.Kind switch
		{
			ConfigurationDiagnosticKind.InvalidJson => DescribeInvalidJson(diagnostic),
			ConfigurationDiagnosticKind.NotAnObject => NOT_AN_OBJECT_MESSAGE,
			ConfigurationDiagnosticKind.InvalidValue => DescribeInvalidValue(diagnostic, location),
			ConfigurationDiagnosticKind.TooLarge => Format(
				TOO_LARGE_MESSAGE_FORMAT,
				ConfigurationFileReader.MAXIMUM_CONFIGURATION_FILE_LENGTH_IN_BYTES.ToString(CultureInfo.InvariantCulture)),
			ConfigurationDiagnosticKind.UnknownKey => Format(UNKNOWN_KEY_MESSAGE_FORMAT, location, diagnostic.Subject),
			ConfigurationDiagnosticKind.UnknownLanguage => Format(UNKNOWN_LANGUAGE_MESSAGE_FORMAT, location, diagnostic.Subject),
			_ => throw new ArgumentOutOfRangeException(nameof(diagnostic), diagnostic.Kind, "No message is declared for this diagnostic.")
		};
	}

	/// <summary>Whether the diagnostic explains a malformed file, as opposed to warning about a skipped item in an applied one.</summary>
	public static bool ExplainsMalformedFile(ConfigurationFileDiagnostic diagnostic)
	{
		ArgumentNullException.ThrowIfNull(diagnostic);

		return diagnostic.Kind is not (ConfigurationDiagnosticKind.UnknownKey
			or ConfigurationDiagnosticKind.UnknownLanguage
			or ConfigurationDiagnosticKind.InvalidValue);
	}

	private static string DescribeInvalidJson(ConfigurationFileDiagnostic diagnostic) =>
		diagnostic.LineNumber is long line && diagnostic.PositionInLine is long position
			? Format(
				INVALID_JSON_WITH_POSITION_MESSAGE_FORMAT,
				line.ToString(CultureInfo.InvariantCulture),
				position.ToString(CultureInfo.InvariantCulture))
			: INVALID_JSON_MESSAGE;

	/// <summary>The subject is always one of the four keys: the reader raises this diagnostic for no other.</summary>
	private static string DescribeInvalidValue(ConfigurationFileDiagnostic diagnostic, string location)
	{
		string requirement = diagnostic.Subject switch
		{
			ConfigurationFileReader.ENABLED_LANGUAGES_KEY => _ENABLED_LANGUAGES_REQUIREMENT,
			ConfigurationFileReader.MINIMUM_CONFIDENCE_LEVEL_KEY => ListAlternatives(
				Enum.GetValues<ConfidenceLevel>().Select(ConfidenceLevels.Name)),
			ConfigurationFileReader.DEFAULT_FLOW_KEY => ListAlternatives(
				Enum.GetValues<CredentialFlow>().Select(CredentialFlows.Name)),
			_ => _TEXT_REQUIREMENT
		};

		return diagnostic.Value.Length == 0
			? Format(MUST_BE_MESSAGE_FORMAT, location, diagnostic.Subject, requirement)
			: Format(IS_BUT_MUST_BE_MESSAGE_FORMAT, location, diagnostic.Subject, diagnostic.Value, requirement);
	}

	private static string ListAlternatives(IEnumerable<string> names)
	{
		string[] alternatives = [.. names];

		return alternatives.Length < 2
			? string.Join(_ALTERNATIVE_SEPARATOR, alternatives)
			: string.Join(_ALTERNATIVE_SEPARATOR, alternatives[..^1]) + _LAST_ALTERNATIVE_SEPARATOR + alternatives[^1];
	}

	private static string Format(string messageFormat, params string[] arguments) =>
		string.Format(CultureInfo.InvariantCulture, messageFormat, arguments);
}
