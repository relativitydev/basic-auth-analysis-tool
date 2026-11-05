using MigrateAuth.Cli.Catalog;

namespace MigrateAuth.Cli.Recognition;

/// <summary>
/// ONE DIALECT PER FILE, RESOLVED FROM THE LANGUAGE'S DECLARATION AND NOTHING ELSE. The engine treats a
/// dialect as an opaque identifier: it never learns what a language's shells (or any other family of
/// interpreters) mean by it, only which identifier a file's first line or extension maps to.
///
/// THE ORDER, EACH STEP FALLING THROUGH TO THE NEXT. (1) The first line, when it starts with <c>#!</c>: the
/// interpreter is the base name of its first word, or, when that is <c>env</c>, of the first word that is not
/// an option (<c>-S</c>, <c>-i</c>, <c>--</c>, any <c>-x</c> flag) and not a <c>NAME=value</c> assignment; a
/// <c>.exe</c> suffix is dropped; the base name is looked up in the declared interpreters. (2) The file's
/// extension, lower-cased, looked up in the declared extensions. (3) The declared default dialect. A language
/// that declares nothing has no dialect at all (null).
/// </summary>
public static class FileDialectResolution
{
	private const string _SHEBANG_OPENING = "#!";
	private const string _ENVIRONMENT_LAUNCHER = "env";
	private const string _EXECUTABLE_SUFFIX = ".exe";
	private const char _OPTION_PREFIX = '-';
	private const char _ASSIGNMENT_SEPARATOR = '=';
	private const char _LINE_FEED = '\n';

	private static readonly char[] _wordSeparators = [' ', '\t'];
	private static readonly char[] _pathSeparators = ['/', '\\'];
	private static readonly char[] _quoteCharacters = ['"', '\''];

	/// <summary>The file's dialect, or null where <paramref name="declaration"/> is null.</summary>
	/// <param name="declaration">The language's declared dialects.</param>
	/// <param name="normalizedText">The file's normalized text; only its first line is read.</param>
	/// <param name="relativePath">The file's repository-relative path; only its extension is read.</param>
	public static string? Resolve(FileDialects? declaration, string normalizedText, string relativePath)
	{
		ArgumentNullException.ThrowIfNull(normalizedText);
		ArgumentNullException.ThrowIfNull(relativePath);

		string? dialect = null;

		if (declaration is not null)
		{
			dialect = DialectOfInterpreter(declaration, normalizedText)
				?? DialectOfExtension(declaration, relativePath)
				?? declaration.DefaultDialect;
		}

		return dialect;
	}

	private static string? DialectOfInterpreter(FileDialects declaration, string normalizedText)
	{
		string? interpreter = InterpreterOf(normalizedText);
		string? dialect = null;

		if (interpreter is not null && declaration.ShebangInterpreters.TryGetValue(interpreter, out string? declared))
		{
			dialect = declared;
		}

		return dialect;
	}

	private static string? DialectOfExtension(FileDialects declaration, string relativePath)
	{
		string extension = Path.GetExtension(relativePath).ToLowerInvariant();
		string? dialect = null;

		if (extension.Length > 0 && declaration.ExtensionDialects.TryGetValue(extension, out string? declared))
		{
			dialect = declared;
		}

		return dialect;
	}

	/// <summary>The interpreter base name the first line names, or null where there is no first-line interpreter.</summary>
	private static string? InterpreterOf(string normalizedText)
	{
		string? interpreter = null;

		if (normalizedText.StartsWith(_SHEBANG_OPENING, StringComparison.Ordinal))
		{
			int lineEnd = normalizedText.IndexOf(_LINE_FEED, StringComparison.Ordinal);
			string line = lineEnd < 0 ? normalizedText : normalizedText[..lineEnd];
			string[] words = line[_SHEBANG_OPENING.Length..]
				.Split(_wordSeparators, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

			interpreter = words.Length == 0 ? null : InterpreterOfWords(words);
		}

		return interpreter;
	}

	private static string? InterpreterOfWords(string[] words)
	{
		string first = BaseNameOf(words[0]);
		string? interpreter = first;

		if (string.Equals(first, _ENVIRONMENT_LAUNCHER, StringComparison.Ordinal))
		{
			string? launched = words
				.Skip(1)
				.FirstOrDefault(word => word[0] != _OPTION_PREFIX && !word.Contains(_ASSIGNMENT_SEPARATOR));

			interpreter = launched is null ? null : BaseNameOf(launched);
		}

		return interpreter;
	}

	private static string BaseNameOf(string word)
	{
		string unquoted = word.Trim(_quoteCharacters);
		string baseName = unquoted[(unquoted.LastIndexOfAny(_pathSeparators) + 1)..];

		return baseName.EndsWith(_EXECUTABLE_SUFFIX, StringComparison.OrdinalIgnoreCase)
			? baseName[..^_EXECUTABLE_SUFFIX.Length]
			: baseName;
	}
}
