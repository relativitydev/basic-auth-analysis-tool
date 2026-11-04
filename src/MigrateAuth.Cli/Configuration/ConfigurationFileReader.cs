using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Text;
using System.Text.Json;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Findings;
using MigrateAuth.Cli.Session;

namespace MigrateAuth.Cli.Configuration;

/// <summary>
/// A document, the condition of the file it came from, and what was noticed on the way.
/// The document is meaningful only when the condition is accepted.
/// </summary>
internal readonly record struct ConfigurationFileRead(
	ConfigurationDocument? Document,
	ConfigurationFileCondition Condition,
	ImmutableArray<ConfigurationFileDiagnostic> Diagnostics);

/// <summary>
/// Reads one <c>.migrate-auth.json</c> from one folder and says what condition it was in.
///
/// It resolves nothing and decides nothing: the resolution order belongs to
/// <see cref="ConfigurationResolver"/>, and the messages belong to <see cref="PreflightValidation"/>.
/// This class only turns a folder into one of four conditions, which is what lets the same code
/// read the repository's file and the user profile's without either location leaking into the
/// other's handling. It also says WHY a file was found malformed, and what an accepted file
/// contained that this version skips or could not use, so that the message that follows can name it.
///
/// A MISSING FILE IS NOT AN ERROR AT ANY LEVEL. It is the ordinary case: most repositories
/// carry no configuration and most developers have none in
/// their profile either. Absence produces a condition, no message and no degradation.
///
/// THE FILE IS UNTRUSTED AND ONE OF THE TWO LOCATIONS IS THE AUDITED REPOSITORY. It is therefore
/// size-capped before it is read at all, for the same reason the session state file is: a
/// deserializer pointed at an arbitrarily large document is a denial of service the audited
/// object controls. A file above the cap does not have the expected structure, which is what
/// the degradation message already covers.
/// </summary>
internal static class ConfigurationFileReader
{
	/// <summary>
	/// The cap. A configuration naming eight languages, one level and one path is a few hundred
	/// bytes; a quarter of a megabyte is four orders of magnitude of headroom and still bounded.
	/// </summary>
	internal const int MAXIMUM_CONFIGURATION_FILE_LENGTH_IN_BYTES = 262144;

	/// <summary>The key of <see cref="ConfigurationDocument.EnabledLanguages"/>.</summary>
	internal const string ENABLED_LANGUAGES_KEY = "enabledLanguages";

	/// <summary>The key of <see cref="ConfigurationDocument.MinimumConfidenceLevel"/>.</summary>
	internal const string MINIMUM_CONFIDENCE_LEVEL_KEY = "minimumConfidenceLevel";

	/// <summary>The key of <see cref="ConfigurationDocument.OutputPath"/>.</summary>
	internal const string OUTPUT_PATH_KEY = "outputPath";

	/// <summary>The key of <see cref="ConfigurationDocument.DefaultFlow"/>.</summary>
	internal const string DEFAULT_FLOW_KEY = "defaultFlow";

	/// <summary>
	/// The most warnings one file may raise. A hostile file can carry thousands of unknown keys,
	/// and the console is not a place to be flooded by the object being audited.
	/// </summary>
	private const int _MAXIMUM_WARNINGS_PER_FILE = 10;

	/// <summary>The longest stretch of typed text a diagnostic repeats.</summary>
	private const int _MAXIMUM_ECHOED_TEXT_LENGTH = 64;

	private const int _FIRST_POSITION = 1;

	private static readonly UTF8Encoding _utf8WithoutByteOrderMark = new(encoderShouldEmitUTF8Identifier: false);

	private static readonly FrozenSet<string> _knownKeys = FrozenSet.ToFrozenSet(
		[ENABLED_LANGUAGES_KEY, MINIMUM_CONFIDENCE_LEVEL_KEY, OUTPUT_PATH_KEY, DEFAULT_FLOW_KEY],
		StringComparer.Ordinal);

	/// <summary>
	/// Reads the configuration file of a folder, if there is one. The location decides one thing
	/// only: whether a <c>defaultFlow</c> value is validated, which happens on the user
	/// profile path and never on the repository's.
	/// </summary>
	internal static ConfigurationFileRead ReadFrom(string folderPath, ConfigurationFileLocation location)
	{
		ConfigurationFileRead read;

		if (string.IsNullOrWhiteSpace(folderPath))
		{
			read = Without(ConfigurationFileCondition.Absent);
		}
		else
		{
			read = ReadFile(Path.Combine(folderPath, ConfigurationResolver.CONFIGURATION_FILE_NAME), location);
		}

		return read;
	}

	private static ConfigurationFileRead ReadFile(string filePath, ConfigurationFileLocation location)
	{
		ConfigurationFileRead read;

		try
		{
			FileInfo configurationFile = new(filePath);

			if (!configurationFile.Exists)
			{
				read = Without(ConfigurationFileCondition.Absent);
			}
			else
			{
				read = ReadWithinCeiling(filePath, location);
			}
		}
		catch (IOException)
		{
			read = Without(ConfigurationFileCondition.Unreadable);
		}
		catch (UnauthorizedAccessException)
		{
			read = Without(ConfigurationFileCondition.Unreadable);
		}
		catch (NotSupportedException)
		{
			read = Without(ConfigurationFileCondition.Unreadable);
		}

		return read;
	}

	/// <summary>
	/// Reads the file through its OPENED stream, bounded at the cap: the length is checked on the
	/// opened file and every byte delivered is counted, so a file that grows after the open cannot
	/// get past the cap the way it could past a length read from the directory entry beforehand.
	/// </summary>
	private static ConfigurationFileRead ReadWithinCeiling(string filePath, ConfigurationFileLocation location)
	{
		ConfigurationFileRead read;

		try
		{
			using FileStream openedFile = new(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
			using ReadCeilingEnforcingStream boundedStream =
				ReadCeilingEnforcingStream.Wrap(openedFile, MAXIMUM_CONFIGURATION_FILE_LENGTH_IN_BYTES);
			using StreamReader reader = new(boundedStream, _utf8WithoutByteOrderMark);

			read = Interpret(reader.ReadToEnd(), location);
		}
		catch (ReadCeilingExceededException)
		{
			read = Malformed(new ConfigurationFileDiagnostic
			{
				Location = location,
				Kind = ConfigurationDiagnosticKind.TooLarge
			});
		}

		return read;
	}

	/// <summary>
	/// Parses first and binds second, so that a syntax error and a wrong value are told apart:
	/// the first has a line and a position and costs the whole file, and the second has a key and
	/// costs only that key.
	/// </summary>
	private static ConfigurationFileRead Interpret(string text, ConfigurationFileLocation location)
	{
		ConfigurationFileRead read;

		try
		{
			using JsonDocument parsed = JsonDocument.Parse(text);

			read = parsed.RootElement.ValueKind is JsonValueKind.Object
				? Bind(parsed.RootElement, location)
				: Malformed(new ConfigurationFileDiagnostic
				{
					Location = location,
					Kind = ConfigurationDiagnosticKind.NotAnObject
				});
		}
		catch (JsonException syntaxError)
		{
			read = Malformed(new ConfigurationFileDiagnostic
			{
				Location = location,
				Kind = ConfigurationDiagnosticKind.InvalidJson,
				LineNumber = syntaxError.LineNumber + _FIRST_POSITION,
				PositionInLine = syntaxError.BytePositionInLine + _FIRST_POSITION
			});
		}

		return read;
	}

	/// <summary>
	/// Binds the four keys one at a time, so that a wrong value costs its own key and nothing else.
	///
	/// A KEY WITH A VALUE THIS VERSION CANNOT USE IS IGNORED, AS IF IT WERE ABSENT, AND IS NAMED IN A
	/// WARNING; THE REST OF THE FILE IS APPLIED. The vocabularies of the level and the flow are closed
	/// - the rubric defines exactly three levels and the tool never recommends a third grant type - so
	/// a value outside them cannot be honoured or safely guessed, and the key falls through to the
	/// next source exactly as an absent key does. What it must not do is take the file's other keys
	/// with it: a misspelt flow is not a reason to write the report somewhere else. A null or blank
	/// entry inside the language array makes the array unusable, so the key is ignored as a whole
	/// rather than read as a shorter list.
	///
	/// THE TWO KEYS NEVER READ FROM THE REPOSITORY ARE NOT VALIDATED THERE. Their presence is
	/// all the resolver records, so a value of any shape counts as present and none is inspected.
	///
	/// A key that appears twice keeps its last value, which is what the JSON deserializer does.
	/// </summary>
	private static ConfigurationFileRead Bind(JsonElement root, ConfigurationFileLocation location)
	{
		Dictionary<string, JsonElement> knownValues = new(StringComparer.Ordinal);

		foreach (JsonProperty property in root.EnumerateObject())
		{
			if (_knownKeys.Contains(property.Name))
			{
				knownValues[property.Name] = property.Value;
			}
		}

		List<ConfigurationFileDiagnostic> ignoredValues = [];

		ConfigurationDocument document = new()
		{
			EnabledLanguages = ReadLanguageList(knownValues, location, ignoredValues),
			MinimumConfidenceLevel = ReadClosedVocabularyText(
				knownValues,
				MINIMUM_CONFIDENCE_LEVEL_KEY,
				name => ConfidenceLevels.TryParse(name, out _),
				location,
				ignoredValues),
			OutputPath = ReadProfileOnlyText(knownValues, OUTPUT_PATH_KEY, isAcceptable: null, location, ignoredValues),
			DefaultFlow = ReadProfileOnlyText(
				knownValues,
				DEFAULT_FLOW_KEY,
				name => CredentialFlows.TryParse(name, out _),
				location,
				ignoredValues)
		};

		ImmutableArray<ConfigurationFileDiagnostic> diagnostics =
			[.. ignoredValues.Concat(FindSkippedItems(document, root, location)).Take(_MAXIMUM_WARNINGS_PER_FILE)];

		return new ConfigurationFileRead(document, ConfigurationFileCondition.Accepted, diagnostics);
	}

	/// <summary>
	/// The language list, or null when the key is absent, null, or unusable. An unknown language name
	/// is not unusable: it is skipped later, on its own, so that a file written for a later release still loads.
	/// </summary>
	private static string[]? ReadLanguageList(
		Dictionary<string, JsonElement> knownValues,
		ConfigurationFileLocation location,
		List<ConfigurationFileDiagnostic> ignoredValues)
	{
		string[]? languages = null;

		if (knownValues.TryGetValue(ENABLED_LANGUAGES_KEY, out JsonElement value)
			&& value.ValueKind is not JsonValueKind.Null)
		{
			bool usable = value.ValueKind is JsonValueKind.Array
				&& value.EnumerateArray().All(IsNonBlankText);

			if (usable)
			{
				languages = [.. value.EnumerateArray().Select(entry => entry.GetString()!)];
			}
			else
			{
				ignoredValues.Add(IgnoredValue(location, ENABLED_LANGUAGES_KEY, string.Empty));
			}
		}

		return languages;
	}

	/// <summary>
	/// A key read from both files whose value must be one of a closed set of names. A blank string
	/// is kept as it is: it names nothing and the resolver already treats it as absent.
	/// </summary>
	private static string? ReadClosedVocabularyText(
		Dictionary<string, JsonElement> knownValues,
		string key,
		Func<string, bool> isAcceptable,
		ConfigurationFileLocation location,
		List<ConfigurationFileDiagnostic> ignoredValues)
	{
		string? text = null;

		if (knownValues.TryGetValue(key, out JsonElement value) && value.ValueKind is not JsonValueKind.Null)
		{
			if (value.ValueKind is not JsonValueKind.String)
			{
				ignoredValues.Add(IgnoredValue(location, key, string.Empty));
			}
			else if (!string.IsNullOrWhiteSpace(value.GetString()) && !isAcceptable(value.GetString()!))
			{
				ignoredValues.Add(IgnoredValue(location, key, value.GetString()!));
			}
			else
			{
				text = value.GetString();
			}
		}

		return text;
	}

	/// <summary>
	/// A key read from the user profile only. In the profile it is validated like any other key; in
	/// the repository's file it is never inspected, and any value of it counts as present so that the
	/// resolver records it as ignored. The raw text kept for a value that is not a string is never
	/// used for anything else.
	/// </summary>
	private static string? ReadProfileOnlyText(
		Dictionary<string, JsonElement> knownValues,
		string key,
		Func<string, bool>? isAcceptable,
		ConfigurationFileLocation location,
		List<ConfigurationFileDiagnostic> ignoredValues)
	{
		string? text;

		if (location is ConfigurationFileLocation.UserProfile)
		{
			text = ReadClosedVocabularyText(knownValues, key, isAcceptable ?? AcceptsAnyText, location, ignoredValues);
		}
		else if (knownValues.TryGetValue(key, out JsonElement value) && value.ValueKind is not JsonValueKind.Null)
		{
			text = value.ValueKind is JsonValueKind.String ? value.GetString() : value.GetRawText();
		}
		else
		{
			text = null;
		}

		return text;
	}

	private static bool AcceptsAnyText(string text) => true;

	private static bool IsNonBlankText(JsonElement entry) =>
		entry.ValueKind is JsonValueKind.String && !string.IsNullOrWhiteSpace(entry.GetString());

	/// <summary>The keys and language names of an accepted file that this version skips.</summary>
	private static IEnumerable<ConfigurationFileDiagnostic> FindSkippedItems(
		ConfigurationDocument document,
		JsonElement root,
		ConfigurationFileLocation location)
	{
		IEnumerable<ConfigurationFileDiagnostic> unknownKeys = root.EnumerateObject()
			.Where(property => !_knownKeys.Contains(property.Name))
			.Select(property => Skipped(location, ConfigurationDiagnosticKind.UnknownKey, property.Name));

		IEnumerable<ConfigurationFileDiagnostic> unknownLanguages = (document.EnabledLanguages ?? [])
			.Where(name => !AssessedLanguages.TryParse(name, out _))
			.Select(name => Skipped(location, ConfigurationDiagnosticKind.UnknownLanguage, name));

		return unknownKeys.Concat(unknownLanguages);
	}

	private static ConfigurationFileDiagnostic IgnoredValue(
		ConfigurationFileLocation location,
		string key,
		string value) =>
		new()
		{
			Location = location,
			Kind = ConfigurationDiagnosticKind.InvalidValue,
			Subject = key,
			Value = MakeEchoable(value)
		};

	private static ConfigurationFileDiagnostic Skipped(
		ConfigurationFileLocation location,
		ConfigurationDiagnosticKind kind,
		string typedName) =>
		new()
		{
			Location = location,
			Kind = kind,
			Subject = MakeEchoable(typedName)
		};

	private static string MakeEchoable(string typedText)
	{
		string safeText = ConsoleTextSafety.StripUnsafeCharacters(typedText);

		return safeText.Length > _MAXIMUM_ECHOED_TEXT_LENGTH ? safeText[.._MAXIMUM_ECHOED_TEXT_LENGTH] : safeText;
	}

	private static ConfigurationFileRead Without(ConfigurationFileCondition condition) =>
		new(null, condition, []);

	private static ConfigurationFileRead Malformed(ConfigurationFileDiagnostic reason) =>
		new(null, ConfigurationFileCondition.Malformed, [reason]);
}
