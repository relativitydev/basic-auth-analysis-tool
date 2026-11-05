using System.Collections.Immutable;

namespace MigrateAuth.Cli.Catalog.Languages;

/// <summary>
/// THE EXECUTED-STRING CONTEXT of the bash family of shells, declared from the vocabulary of <see cref="ExecutedStringContext"/>: the words that run a
/// string or a script as code, the escapes the outer quoting removes, and the inline constructs of a double-quoted body. The engine names none of it;
/// every word and every punctuation character the engine uses to read a body as code is a declaration here.
/// </summary>
internal static class ShellExecutedStringDeclarations
{
	private const string _SINGLE_QUOTE = "'";
	private const string _DOUBLE_QUOTE = "\"";
	private const string _PLAIN_HERE_DOCUMENT_OPENING = "<<";
	private const string _TAB_STRIPPING_HERE_DOCUMENT_OPENING = "<<-";
	private const string _DOUBLE_QUOTED_ESCAPABLES = "$`\"\\";
	private const string _UNQUOTED_HERE_DOCUMENT_ESCAPABLES = "$`\\";
	private const string _NOTHING_ESCAPABLE = "";
	private const string _COMMAND_POSITION_PREDECESSORS = "\n;&|(";
	private const string _STRING_OPTION = "-c";
	private const string _STDIN_OPTION = "-s";
	private const string _BACKTICK = "`";
	private const string _PARAMETER_OPENING = "${";
	private const string _PARAMETER_CLOSING = "}";
	private const string _SHEBANG_OPENING = "#!";
	private const string _BLANKS = " \t";
	private const string _OPTION_INTRODUCERS = "-+";
	private const string _QUOTES = "'\"";
	private const string _REDIRECTION_CHARACTERS = "<>";
	private const string _REDIRECTION_MODIFIERS = "&|";
	private const string _LIST_OPERATOR_CHARACTERS = ";|";
	private const string _AND_LIST = "&&";
	private const string _OR_LIST = "||";
	private const string _PIPE = "|";
	private const string _PIPE_WITH_STANDARD_ERROR = "|&";
	private const string _END_OF_OPTIONS_WORD = "--";
	private const string _STANDARD_OUTPUT_DESCRIPTOR = "1";
	private const string _EVAL_WORD = "eval";
	private const string _SU_WORD = "su";
	private const string _SUDO_WORD = "sudo";
	private const string _SSH_WORD = "ssh";
	private const string _ENV_WORD = "env";
	private const string _OUTPUT_REDIRECTION = ">";
	private const string _APPENDING_OUTPUT_REDIRECTION = ">>";
	private const char _ASSIGNMENT_OPERATOR = '=';
	private const char _NAME_CONNECTOR = '_';
	private const char _PATH_SEPARATOR = '/';
	private const char _DESCRIPTOR_DUPLICATION = '&';
	private const char _COMMENT_START = '#';

	private static readonly string[] _licensingWords = ["{", "!", "then", "do", "else", "elif", "if", "while", "until", "time", "coproc"];

	private static readonly string[] _shellWords = ["bash", "sh", "zsh", "ksh", "dash", "mksh"];

	private static readonly string[] _shellOptionsWithArgument = ["-o", "+o", "-O", "+O"];

	private static readonly string[] _privilegeOptionsWithArgument = ["-u", "-g", "-h", "-p", "-C", "-D", "-r", "-t", "-U"];

	private static readonly string[] _remoteOptionsWithArgument =
		["-b", "-c", "-D", "-E", "-e", "-F", "-I", "-i", "-J", "-L", "-l", "-m", "-O", "-o", "-p", "-Q", "-R", "-S", "-W", "-w"];

	private static readonly string[] _scriptFileExtensions = [".sh", ".bash", ".zsh", ".ksh", ".dash"];

	private static readonly string[] _parameterWordOperators = [":-", ":=", ":?", ":+", "-", "=", "?", "+"];

	internal static ExecutedStringContext Build()
	{
		return new ExecutedStringContext
		{
			ReadableBodies = BuildReadableBodies(),
			CommandPositionPredecessors = _COMMAND_POSITION_PREDECESSORS,
			BlankCharacters = _BLANKS,
			OptionIntroducers = _OPTION_INTRODUCERS,
			AssignmentOperator = _ASSIGNMENT_OPERATOR,
			NameConnectorCharacter = _NAME_CONNECTOR,
			PathSeparator = _PATH_SEPARATOR,
			QuoteCharacters = _QUOTES,
			RedirectionCharacters = _REDIRECTION_CHARACTERS,
			RedirectionModifierCharacters = _REDIRECTION_MODIFIERS,
			DescriptorDuplicationCharacter = _DESCRIPTOR_DUPLICATION,
			StandardOutputDescriptor = _STANDARD_OUTPUT_DESCRIPTOR,
			EndOfOptionsWord = _END_OF_OPTIONS_WORD,
			PipeOperators = [_PIPE, _PIPE_WITH_STANDARD_ERROR],
			ListOperatorCharacters = _LIST_OPERATOR_CHARACTERS,
			ListOperatorPairs = [_AND_LIST, _OR_LIST],
			CommentStart = _COMMENT_START,
			StringOption = _STRING_OPTION,
			StdinOption = _STDIN_OPTION,
			LicensingWords = [.. _licensingWords],
			EvalWords = [_EVAL_WORD],
			ShellWords = [.. _shellWords],
			ShellOptionsWithArgument = [.. _shellOptionsWithArgument],
			CommandStringWords = [_SU_WORD],
			PrivilegeWords = [_SUDO_WORD],
			PrivilegeOptionsWithArgument = [.. _privilegeOptionsWithArgument],
			RemoteShellWords = [_SSH_WORD],
			RemoteOptionsWithArgument = [.. _remoteOptionsWithArgument],
			OutputRedirections = [_OUTPUT_REDIRECTION, _APPENDING_OUTPUT_REDIRECTION],
			ScriptFileExtensions = [.. _scriptFileExtensions],
			ShebangOpening = _SHEBANG_OPENING,
			ShebangLauncherWords = [_ENV_WORD],
			InlineContexts = BuildInlineContexts()
		};
	}

	/// <summary>The bodies that are read as code: a single-quoted one removes no escape, a double-quoted one removes four, an unquoted here-document three.</summary>
	private static ImmutableArray<ExecutedBodyQuote> BuildReadableBodies()
	{
		return
		[
			new ExecutedBodyQuote { QuoteOpening = _SINGLE_QUOTE, EscapableCharacters = _NOTHING_ESCAPABLE },
			new ExecutedBodyQuote { QuoteOpening = _DOUBLE_QUOTE, EscapableCharacters = _DOUBLE_QUOTED_ESCAPABLES },
			new ExecutedBodyQuote { QuoteOpening = _PLAIN_HERE_DOCUMENT_OPENING, EscapableCharacters = _UNQUOTED_HERE_DOCUMENT_ESCAPABLES },
			new ExecutedBodyQuote { QuoteOpening = _TAB_STRIPPING_HERE_DOCUMENT_OPENING, EscapableCharacters = _UNQUOTED_HERE_DOCUMENT_ESCAPABLES }
		];
	}

	/// <summary>The two inline constructs a double-quoted body reads as code: a backtick substitution and a braced parameter expansion with its operators.</summary>
	private static ImmutableArray<InlineExecutedContext> BuildInlineContexts()
	{
		return
		[
			new InlineExecutedContext
			{
				QuoteOpening = _DOUBLE_QUOTE,
				Opening = _BACKTICK,
				Closing = _BACKTICK,
				EscapableCharacters = _DOUBLE_QUOTED_ESCAPABLES
			},
			new InlineExecutedContext
			{
				QuoteOpening = _DOUBLE_QUOTE,
				Opening = _PARAMETER_OPENING,
				Closing = _PARAMETER_CLOSING,
				EscapableCharacters = _DOUBLE_QUOTED_ESCAPABLES,
				WordOperators = [.. _parameterWordOperators]
			}
		];
	}
}
