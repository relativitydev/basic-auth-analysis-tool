using System.Collections.Immutable;

namespace MigrateAuth.Cli.Catalog;

/// <summary>
/// THE EXECUTED-STRING CONTEXT: a declaration, per lexical profile, of the places where text that the file holds
/// as a string zone is in fact a command that a shell runs (the profile's declared <c>eval "..."</c>, <c>bash -c '...'</c>,
/// <c>ssh host "..."</c>, <c>su -c "..."</c>, a here-document fed to a shell, a here-document written to a file with a script
/// extension or opening with a shebang, a
/// backtick or a parameter word inside a double-quoted string). The scanner re-reads the body of such a literal as
/// CODE, so a covered construct inside it is a code-zone anchor and takes its class's own level instead of the
/// Low that an anchor inside quotes proposes.
///
/// WHAT THE SCANNER DOES WITH IT. It recognizes a context lexically, from the words that stand before the string
/// at a command position, as this declaration names them. It then reads the body (its escapes removed as the outer
/// quoting removes them) as a nested code region of its own and writes the zones back to the positions the
/// characters have in the file. A body it cannot read soundly (an unterminated construct, a here-document body
/// pending at its end, a recovered anomaly, the nesting bound) stays what the outer scan made it, a string zone,
/// with no anomaly recorded. No new zone kind exists: the re-read body is Code, Comment, StringBody and Delimiter
/// like any other text.
///
/// A profile that declares none never takes any of these branches.
/// </summary>
public sealed record ExecutedStringContext
{
	/// <summary>
	/// The quoted forms whose bodies may be read as code, one per quote rule, named by the rule's opening token
	/// (the profile's declared single quote, double quote and the two here-document openings). A form that is not listed is
	/// never read as code, whatever words stand before it.
	/// </summary>
	public required ImmutableArray<ExecutedBodyQuote> ReadableBodies { get; init; }

	/// <summary>
	/// The characters after which, with blanks skipped, a command begins (the profile's declared line feed, <c>;</c>, <c>&amp;</c>,
	/// <c>|</c> and opening parenthesis). The words of a command are read from the nearest such character that
	/// stands in code, within the lookback bound of the engine.
	/// </summary>
	public required string CommandPositionPredecessors { get; init; }

	/// <summary>
	/// The word that introduces a command string among the options of a shell (the profile's declared <c>-c</c>): a hyphen and
	/// one letter. A cluster of option letters that holds the letter introduces it too (<c>-lc</c>).
	/// </summary>
	public required string StringOption { get; init; }

	/// <summary>The characters that separate the words of a command (the declared blanks, normally a space and a tab). The line feed is the normalized text's own line end and is not declared.</summary>
	public required string BlankCharacters { get; init; }

	/// <summary>The characters that introduce an option word.</summary>
	public required string OptionIntroducers { get; init; }

	/// <summary>The character between a variable name and its value in an assignment word.</summary>
	public required char AssignmentOperator { get; init; }

	/// <summary>The character other than letters and digits that a variable name may hold.</summary>
	public required char NameConnectorCharacter { get; init; }

	/// <summary>The character that separates the segments of a path.</summary>
	public required char PathSeparator { get; init; }

	/// <summary>The characters that open and close a quoted word, each closing on itself.</summary>
	public required string QuoteCharacters { get; init; }

	/// <summary>The characters that begin and make up a redirection operator, input and output alike.</summary>
	public required string RedirectionCharacters { get; init; }

	/// <summary>The characters that may follow a redirection operator and belong to it (a descriptor duplication, a clobber).</summary>
	public required string RedirectionModifierCharacters { get; init; }

	/// <summary>The modifier that duplicates a descriptor, after which no target word follows.</summary>
	public required char DescriptorDuplicationCharacter { get; init; }

	/// <summary>The descriptor, as the digits that name it, that an output redirection with no descriptor writes to.</summary>
	public required string StandardOutputDescriptor { get; init; }

	/// <summary>The word that ends option parsing: every word after it is an argument, never an option.</summary>
	public required string EndOfOptionsWord { get; init; }

	/// <summary>
	/// The operators that join the stages of a pipeline, in any order. A here-document whose line pipes it
	/// on to a command that reads standard input as a script is fed to that command; only the last stage is asked.
	/// </summary>
	public required ImmutableArray<string> PipeOperators { get; init; }

	/// <summary>The characters that begin a list or pipeline operator and so end the words of a command on a line.</summary>
	public required string ListOperatorCharacters { get; init; }

	/// <summary>The two-character list operators that end the words of a command on a line.</summary>
	public ImmutableArray<string> ListOperatorPairs { get; init; } = [];

	/// <summary>The character that begins a comment, which ends the words of a command on a line.</summary>
	public required char CommentStart { get; init; }

	/// <summary>Reserved words that license a command after them (the profile's declared <c>then</c>, <c>do</c>, <c>!</c>), skipped before the command word.</summary>
	public ImmutableArray<string> LicensingWords { get; init; } = [];

	/// <summary>Words that run each quoted argument as code (the profile's declared <c>eval</c>).</summary>
	public ImmutableArray<string> EvalWords { get; init; } = [];

	/// <summary>
	/// The shells: words that run the string after <see cref="StringOption"/> as code and, with no string, run
	/// their standard input as code (a here-document fed to them).
	/// </summary>
	public ImmutableArray<string> ShellWords { get; init; } = [];

	/// <summary>The options of a shell that take the next word as their argument (the profile's declared <c>-o</c>, <c>+o</c>, <c>-O</c>, <c>+O</c>).</summary>
	public ImmutableArray<string> ShellOptionsWithArgument { get; init; } = [];

	/// <summary>
	/// The option after which the words of a shell are arguments of the script it reads from standard input and no
	/// longer a script file (the profile's declared <c>-s</c>), or null.
	/// </summary>
	public string? StdinOption { get; init; }

	/// <summary>Words that run the string after <see cref="StringOption"/> as a command but read no here-document as code (the profile's declared <c>su</c>).</summary>
	public ImmutableArray<string> CommandStringWords { get; init; } = [];

	/// <summary>Words that run another command, after options (the profile's declared <c>sudo</c>).</summary>
	public ImmutableArray<string> PrivilegeWords { get; init; } = [];

	/// <summary>The options of a <see cref="PrivilegeWords"/> word that take the next word as their argument (<c>-u</c>, <c>-g</c>).</summary>
	public ImmutableArray<string> PrivilegeOptionsWithArgument { get; init; } = [];

	/// <summary>
	/// Words that run a command on another host, the first word after their options being the host (the profile's declared
	/// <c>ssh</c>): a string right after the host is the remote command, and a here-document with no remote command
	/// is fed to the remote shell.
	/// </summary>
	public ImmutableArray<string> RemoteShellWords { get; init; } = [];

	/// <summary>The options of a <see cref="RemoteShellWords"/> word that take the next word as their argument.</summary>
	public ImmutableArray<string> RemoteOptionsWithArgument { get; init; } = [];

	/// <summary>
	/// The output redirections whose target word names a file (the profile's declared <c>&gt;</c> and <c>&gt;&gt;</c>). A
	/// here-document of a command that is not a shell is read as code when such a target ends in one of
	/// <see cref="ScriptFileExtensions"/>.
	/// </summary>
	public ImmutableArray<string> OutputRedirections { get; init; } = [];

	/// <summary>The file extensions of a script, each with its leading dot (the profile's declared <c>.sh</c>, <c>.bash</c>, <c>.zsh</c>, <c>.ksh</c>, <c>.dash</c>).</summary>
	public ImmutableArray<string> ScriptFileExtensions { get; init; } = [];

	/// <summary>
	/// The two characters that open a shebang line (the profile's declared <c>#!</c>), or null where the body's first line is
	/// not consulted. A here-document of a command that is not a shell is read as code when its first line is a
	/// shebang that names one of <see cref="ShellWords"/>, directly or through one of <see cref="ShebangLauncherWords"/>.
	/// </summary>
	public string? ShebangOpening { get; init; }

	/// <summary>Words through which a shebang names its interpreter (the profile's declared <c>env</c>).</summary>
	public ImmutableArray<string> ShebangLauncherWords { get; init; } = [];

	/// <summary>
	/// Constructs inside a double-quoted body that the outer shell runs or expands as code (the profile's declared backticks and
	/// the word of a parameter expansion), which are string body in the outer scan.
	/// </summary>
	public ImmutableArray<InlineExecutedContext> InlineContexts { get; init; } = [];
}

/// <summary>
/// One quoted form whose body may be read as code, and how the outer quoting escapes its text.
/// </summary>
public sealed record ExecutedBodyQuote
{
	/// <summary>The opening token of the quote rule, exactly as the rule declares it.</summary>
	public required string QuoteOpening { get; init; }

	/// <summary>
	/// The characters that the rule's escape character removes in front of when the outer shell reads the body: in a
	/// double-quoted string the dollar sign, the backtick, the double quote and the escape itself, in an unquoted
	/// here-document body the same without the double quote. The escape and a line feed after it are removed
	/// together. Empty for a body the outer shell takes literally (a single-quoted string, where a backslash and a line
	/// feed both stay, and a quoted here-document): nothing is then removed, whatever escape the quote rule declares.
	/// An escape in front of any other character stays.
	/// </summary>
	public required string EscapableCharacters { get; init; }
}

/// <summary>
/// A construct inside the body of a double-quoted string that is read as code: from <see cref="Opening"/> to
/// <see cref="Closing"/>, with the parameter name and operator that begin its word skipped where
/// <see cref="WordOperators"/> is declared.
/// </summary>
public sealed record InlineExecutedContext
{
	/// <summary>The opening token of the quote rule whose body holds the construct.</summary>
	public required string QuoteOpening { get; init; }

	/// <summary>The token that opens the construct (a backtick, or a dollar sign and an opening brace).</summary>
	public required string Opening { get; init; }

	/// <summary>
	/// The token that closes it. Where it equals <see cref="Opening"/> the first unescaped one closes (a backtick);
	/// otherwise nested openings are counted.
	/// </summary>
	public required string Closing { get; init; }

	/// <summary>
	/// The characters that the escape character removes in front of inside the construct (inside backticks within a double-quoted
	/// string the dollar sign, the backtick, the double quote and the escape itself: the outer shell removes the escape
	/// before a double quote there).
	/// </summary>
	public required string EscapableCharacters { get; init; }

	/// <summary>
	/// The operators after a parameter name that begin the word (<c>:-</c>, <c>:=</c>, <c>:?</c>, <c>:+</c>, <c>-</c>,
	/// <c>=</c>, <c>?</c>, <c>+</c>), or empty where the whole content is the command. When declared, a construct whose
	/// name is not followed by one of them has no word and is left as string body.
	/// </summary>
	public ImmutableArray<string> WordOperators { get; init; } = [];
}
