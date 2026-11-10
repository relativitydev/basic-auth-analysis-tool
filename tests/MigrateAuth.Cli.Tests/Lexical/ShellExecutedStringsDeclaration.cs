using MigrateAuth.Cli.Catalog;

namespace MigrateAuth.Cli.Tests.Lexical;

/// <summary>
/// THE TEST-LOCAL EXECUTED-STRING CONTEXT of the bash family of shells, built here from the declaration vocabulary and
/// not read from any shipped catalog: the words that run a string or a script as code, the escapes the outer quoting
/// removes, and the inline constructs of a double-quoted body. Nothing under <c>src/</c> declares it.
/// </summary>
internal static class ShellExecutedStringsDeclaration
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

	internal static ExecutedStringContext Build()
	{
		return new ExecutedStringContext
		{
			ReadableBodies =
			[
				new ExecutedBodyQuote { QuoteOpening = _SINGLE_QUOTE, EscapableCharacters = _NOTHING_ESCAPABLE },
				new ExecutedBodyQuote { QuoteOpening = _DOUBLE_QUOTE, EscapableCharacters = _DOUBLE_QUOTED_ESCAPABLES },
				new ExecutedBodyQuote { QuoteOpening = _PLAIN_HERE_DOCUMENT_OPENING, EscapableCharacters = _UNQUOTED_HERE_DOCUMENT_ESCAPABLES },
				new ExecutedBodyQuote { QuoteOpening = _TAB_STRIPPING_HERE_DOCUMENT_OPENING, EscapableCharacters = _UNQUOTED_HERE_DOCUMENT_ESCAPABLES }
			],
			CommandPositionPredecessors = _COMMAND_POSITION_PREDECESSORS,
			BlankCharacters = _BLANKS,
			OptionIntroducers = _OPTION_INTRODUCERS,
			AssignmentOperator = '=',
			NameConnectorCharacter = '_',
			PathSeparator = '/',
			QuoteCharacters = _QUOTES,
			RedirectionCharacters = _REDIRECTION_CHARACTERS,
			RedirectionModifierCharacters = _REDIRECTION_MODIFIERS,
			DescriptorDuplicationCharacter = '&',
			StandardOutputDescriptor = "1",
			EndOfOptionsWord = "--",
			PipeOperators = ["|", "|&"],
			ListOperatorCharacters = _LIST_OPERATOR_CHARACTERS,
			ListOperatorPairs = [_AND_LIST, _OR_LIST],
			CommentStart = '#',
			StringOption = _STRING_OPTION,
			StdinOption = _STDIN_OPTION,
			LicensingWords = ["{", "!", "then", "do", "else", "elif", "if", "while", "until", "time", "coproc"],
			EvalWords = ["eval"],
			ShellWords = ["bash", "sh", "zsh", "ksh", "dash", "mksh"],
			ShellOptionsWithArgument = ["-o", "+o", "-O", "+O"],
			CommandStringWords = ["su"],
			PrivilegeWords = ["sudo"],
			PrivilegeOptionsWithArgument = ["-u", "-g", "-h", "-p", "-C", "-D", "-r", "-t", "-U"],
			RemoteShellWords = ["ssh"],
			RemoteOptionsWithArgument = ["-b", "-c", "-D", "-E", "-e", "-F", "-I", "-i", "-J", "-L", "-l", "-m", "-O", "-o", "-p", "-Q", "-R", "-S", "-W", "-w"],
			OutputRedirections = [">", ">>"],
			ScriptFileExtensions = [".sh", ".bash", ".zsh", ".ksh", ".dash"],
			ShebangOpening = _SHEBANG_OPENING,
			ShebangLauncherWords = ["env"],
			InlineContexts =
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
					WordOperators = [":-", ":=", ":?", ":+", "-", "=", "?", "+"]
				}
			]
		};
	}
}
