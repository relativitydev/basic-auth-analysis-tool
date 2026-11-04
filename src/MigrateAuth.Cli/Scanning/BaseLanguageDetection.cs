using System.Collections.Frozen;

using MigrateAuth.Cli.Catalog;

namespace MigrateAuth.Cli.Scanning;

/// <summary>
/// the EXTENSION AND SHEBANG TABLE FOR ALL EIGHT LANGUAGES, OWNED BY THE ENGINE AND
/// NEVER BY A CATALOG.
///
/// A HIT HERE NEVER PRODUCES AN ANALYZER. `LanguageAttribution` hands a base-table hit back as a
/// named language with a null <see cref="ILanguageAnalyzer"/>, so it is counted in the inventory
/// under its canonical name and excluded from candidate selection on exactly the same path an
/// unrecognized file already takes - <c>Attribute</c>'s own caller never learns whether a name came
/// from a registered analyzer or from this table, because the two are indistinguishable in every
/// way that matters to a stage downstream of Scan.
///
/// BOTH TABLES ARE DECLARED DATA, NEVER A BRANCH PER LANGUAGE, for the reason the analyzer contract
/// already gives for a catalog's own contract: a table a maintainer edits by adding a row is auditable in a way a
/// growing if/else chain is not, and it is the shape that lets a reviewer confirm all eight
/// languages are named by counting entries rather than reading control flow.
///
/// THE EXTENSION TABLE IS CASE-INSENSITIVE, matching <see cref="LanguageCatalog"/>'s own extension
/// index exactly, so a repository written on a case-insensitive volume inventories the same way on a
/// case-sensitive one.
///
/// THE SHEBANG TABLE APPLIES ONLY WHERE the OWN FIRST-LINE RULE APPLIES: a file with no
/// extension, and only after every registered analyzer's own first-line rule has had first refusal
/// - <see cref="LanguageAttribution"/> enforces the ordering, this type only supplies the engine's own
/// fallback answer. Interpreter names are matched case-sensitively and without locale data.
/// </summary>
public static class BaseLanguageDetection
{
	private const string _SHEBANG_PREFIX = "#!";
	private const char _PATH_SEGMENT_SEPARATOR = '/';
	private const char _FLAG_PREFIX_CHARACTER = '-';
	private const string _ENV_INTERPRETER_NAME = "env";
	private const string _PYTHON3_VERSIONED_PREFIX = "python3.";

	private const string _CSHARP_EXTENSION = ".cs";
	private const string _VBNET_EXTENSION = ".vb";
	private const string _JAVASCRIPT_EXTENSION = ".js";
	private const string _JAVASCRIPT_MODULE_EXTENSION = ".mjs";
	private const string _JAVASCRIPT_COMMONJS_EXTENSION = ".cjs";
	private const string _JAVASCRIPT_JSX_EXTENSION = ".jsx";
	private const string _TYPESCRIPT_EXTENSION = ".ts";
	private const string _TYPESCRIPT_JSX_EXTENSION = ".tsx";
	private const string _TYPESCRIPT_MODULE_EXTENSION = ".mts";
	private const string _TYPESCRIPT_COMMONJS_EXTENSION = ".cts";
	private const string _PYTHON_EXTENSION = ".py";
	private const string _PYTHON_WINDOWS_EXTENSION = ".pyw";
	private const string _GO_EXTENSION = ".go";
	private const string _POWERSHELL_SCRIPT_EXTENSION = ".ps1";
	private const string _POWERSHELL_MODULE_EXTENSION = ".psm1";
	private const string _POWERSHELL_MANIFEST_EXTENSION = ".psd1";
	private const string _SHELL_SCRIPT_EXTENSION = ".sh";
	private const string _BASH_EXTENSION = ".bash";
	private const string _ZSH_EXTENSION = ".zsh";
	private const string _KSH_EXTENSION = ".ksh";

	private const string _PYTHON_INTERPRETER = "python";
	private const string _PYTHON2_INTERPRETER = "python2";
	private const string _PYTHON3_INTERPRETER = "python3";
	private const string _BASH_INTERPRETER = "bash";
	private const string _SH_INTERPRETER = "sh";
	private const string _ZSH_INTERPRETER = "zsh";
	private const string _KSH_INTERPRETER = "ksh";
	private const string _PWSH_INTERPRETER = "pwsh";
	private const string _POWERSHELL_INTERPRETER = "powershell";
	private const string _NODE_INTERPRETER = "node";
	private const string _NODEJS_INTERPRETER = "nodejs";

	private static readonly (string Extension, AssessedLanguage Language)[] _extensionEntries =
	[
		(_CSHARP_EXTENSION, AssessedLanguage.CSharp),
		(_VBNET_EXTENSION, AssessedLanguage.VBNet),
		(_JAVASCRIPT_EXTENSION, AssessedLanguage.JavaScript),
		(_JAVASCRIPT_MODULE_EXTENSION, AssessedLanguage.JavaScript),
		(_JAVASCRIPT_COMMONJS_EXTENSION, AssessedLanguage.JavaScript),
		(_JAVASCRIPT_JSX_EXTENSION, AssessedLanguage.JavaScript),
		(_TYPESCRIPT_EXTENSION, AssessedLanguage.TypeScript),
		(_TYPESCRIPT_JSX_EXTENSION, AssessedLanguage.TypeScript),
		(_TYPESCRIPT_MODULE_EXTENSION, AssessedLanguage.TypeScript),
		(_TYPESCRIPT_COMMONJS_EXTENSION, AssessedLanguage.TypeScript),
		(_PYTHON_EXTENSION, AssessedLanguage.Python),
		(_PYTHON_WINDOWS_EXTENSION, AssessedLanguage.Python),
		(_GO_EXTENSION, AssessedLanguage.Go),
		(_POWERSHELL_SCRIPT_EXTENSION, AssessedLanguage.PowerShell),
		(_POWERSHELL_MODULE_EXTENSION, AssessedLanguage.PowerShell),
		(_POWERSHELL_MANIFEST_EXTENSION, AssessedLanguage.PowerShell),
		(_SHELL_SCRIPT_EXTENSION, AssessedLanguage.Shell),
		(_BASH_EXTENSION, AssessedLanguage.Shell),
		(_ZSH_EXTENSION, AssessedLanguage.Shell),
		(_KSH_EXTENSION, AssessedLanguage.Shell)
	];

	private static readonly (string Interpreter, AssessedLanguage Language)[] _shebangEntries =
	[
		(_PYTHON_INTERPRETER, AssessedLanguage.Python),
		(_PYTHON2_INTERPRETER, AssessedLanguage.Python),
		(_PYTHON3_INTERPRETER, AssessedLanguage.Python),
		(_BASH_INTERPRETER, AssessedLanguage.Shell),
		(_SH_INTERPRETER, AssessedLanguage.Shell),
		(_ZSH_INTERPRETER, AssessedLanguage.Shell),
		(_KSH_INTERPRETER, AssessedLanguage.Shell),
		(_PWSH_INTERPRETER, AssessedLanguage.PowerShell),
		(_POWERSHELL_INTERPRETER, AssessedLanguage.PowerShell),
		(_NODE_INTERPRETER, AssessedLanguage.JavaScript),
		(_NODEJS_INTERPRETER, AssessedLanguage.JavaScript)
	];

	private static readonly FrozenDictionary<string, AssessedLanguage> _languagesByExtension =
		_extensionEntries.ToFrozenDictionary(entry => entry.Extension, entry => entry.Language, StringComparer.OrdinalIgnoreCase);

	private static readonly FrozenDictionary<string, AssessedLanguage> _languagesByShebangInterpreter =
		_shebangEntries.ToFrozenDictionary(entry => entry.Interpreter, entry => entry.Language, StringComparer.Ordinal);

	/// <summary>
	/// The built-in extension table's answer for one file extension, including its leading dot. Never
	/// consulted where a registered analyzer already claims the extension - <see cref="LanguageAttribution"/>
	/// owns that precedence.
	/// </summary>
	public static bool TryGetByExtension(string fileExtension, out AssessedLanguage language)
	{
		ArgumentNullException.ThrowIfNull(fileExtension);

		return _languagesByExtension.TryGetValue(fileExtension, out language);
	}

	/// <summary>
	/// The built-in shebang table's answer for an extensionless file's first line. Never consulted where
	/// a registered analyzer's own first-line rule already claimed the file - <see cref="LanguageAttribution"/>
	/// owns that precedence, and this method only ever sees what every registered analyzer refused.
	/// </summary>
	public static bool TryGetByShebangFirstLine(string firstLine, out AssessedLanguage language)
	{
		ArgumentNullException.ThrowIfNull(firstLine);

		return TryMapInterpreter(InterpreterOf(firstLine), out language);
	}

	/// <summary>
	/// The shebang's own interpreter name: the basename of the first token after <c>#!</c>, or -
	/// where that basename is <c>env</c> - the first following token that is not a flag. Null where
	/// the line is not a shebang at all or carries no token to read.
	/// </summary>
	private static string? InterpreterOf(string firstLine)
	{
		string? interpreter = null;

		if (firstLine.StartsWith(_SHEBANG_PREFIX, StringComparison.Ordinal))
		{
			string[] tokens = firstLine[_SHEBANG_PREFIX.Length..].Split(
				(char[]?)null,
				StringSplitOptions.RemoveEmptyEntries);

			if (tokens.Length > 0)
			{
				string basename = BasenameOf(tokens[0]);

				interpreter = string.Equals(basename, _ENV_INTERPRETER_NAME, StringComparison.Ordinal)
					? FirstNonFlagToken(tokens, startIndex: 1)
					: basename;
			}
		}

		return interpreter;
	}

	/// <summary>The last POSIX path segment of a shebang token - shebang paths are POSIX, always.</summary>
	private static string BasenameOf(string token)
	{
		int lastSeparatorIndex = token.LastIndexOf(_PATH_SEGMENT_SEPARATOR);

		return lastSeparatorIndex < 0 ? token : token[(lastSeparatorIndex + 1)..];
	}

	private static string? FirstNonFlagToken(string[] tokens, int startIndex)
	{
		string? match = null;

		for (int index = startIndex; index < tokens.Length; index++)
		{
			if (!tokens[index].StartsWith(_FLAG_PREFIX_CHARACTER))
			{
				match = tokens[index];
				break;
			}
		}

		return match;
	}

	private static bool TryMapInterpreter(string? interpreter, out AssessedLanguage language)
	{
		bool found = false;

		language = default;

		if (interpreter is not null)
		{
			found = _languagesByShebangInterpreter.TryGetValue(interpreter, out language)
				|| TryMapVersionedPython(interpreter, out language);
		}

		return found;
	}

	/// <summary>
	/// <c>python3.&lt;digits&gt;</c>, the one interpreter form the fixed table cannot enumerate in
	/// advance because the minor version is open-ended.
	/// </summary>
	private static bool TryMapVersionedPython(string interpreter, out AssessedLanguage language)
	{
		bool isVersionedPython =
			interpreter.StartsWith(_PYTHON3_VERSIONED_PREFIX, StringComparison.Ordinal)
			&& IsAllAsciiDigits(interpreter.AsSpan(_PYTHON3_VERSIONED_PREFIX.Length));

		language = isVersionedPython ? AssessedLanguage.Python : default;

		return isVersionedPython;
	}

	private static bool IsAllAsciiDigits(ReadOnlySpan<char> text)
	{
		bool allDigits = text.Length > 0;

		foreach (char character in text)
		{
			allDigits &= char.IsAsciiDigit(character);
		}

		return allDigits;
	}
}
