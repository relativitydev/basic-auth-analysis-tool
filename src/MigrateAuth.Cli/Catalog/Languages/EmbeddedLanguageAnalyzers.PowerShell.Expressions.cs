using System.Text.RegularExpressions;

namespace MigrateAuth.Cli.Catalog.Languages;

/// <summary>
/// Every expression the `PowerShell` analyzer hands over, generated at build time, in two files: the shared
/// vocabulary and the first seven classes here, the rest in <c>EmbeddedLanguageAnalyzers.PowerShell.SecondHalf.cs</c>.
/// Each declares the policy match timeout, <see cref="RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS"/>,
/// and <c>IgnoreCase | CultureInvariant</c>: a script writes <c>New-Object</c> and <c>new-object</c> alike, and the case
/// is never the author's decision.
///
/// WHAT THE SHARED VOCABULARY FIXES, ONCE. The parameter dash is any of four characters and never the dash inside a
/// command name; a parameter is accepted under every prefix the language resolves; an argument is one shared shape,
/// whatever class reads it; every window is bounded, required and greedy or lazy over a single character class, and
/// none sits inside an optional group, because the source generator mishandles that shape (the defect recorded in
/// <c>EmbeddedLanguageAnalyzers.CSharp.SecondHalf.cs</c>). Every alternative opens on a literal or a word start, so a
/// file of two megabytes with one construct on every line stays inside the match bound.
///
/// THE ANCHOR OPENS ON THE DELIMITER where the identifying token lies inside a literal (a quoted header key, a quoted
/// type name): the delimiter's zone counts as code, the literal's body does not.
/// </summary>
internal static partial class PowerShellPatternExpressions
{
	/// <summary>
	/// A zero-width point that always matches and gives backtracking exactly one way to fail: one of a word boundary and a non-boundary holds at every position.
	/// It is appended inside the body of a lazy bounded loop over a group, <c>(?:body POINT){m,n}?</c>, because the regular-expression source generator of .NET 10
	/// (generator 10.0.14.15411) emits a defective cleanup of its backtracking stack for such a loop when the body has no backtracking construct of its own
	/// (fixed upstream in dotnet/runtime PR 129628, not backported to release/10.0). It changes no match.
	/// </summary>
	private const string _BACKTRACK_POINT = @"(?:\b|\B)";

	// ------------------------------------------------------------------------------------------
	// Parameters
	// ------------------------------------------------------------------------------------------

	/// <summary>The parameter dash: the hyphen, the en dash, the em dash and the horizontal bar.</summary>
	private const string _DASH = @"[\-\u2013\u2014\u2015]";

	/// <summary>
	/// The dash opens a parameter only where no name character or dash stands before it, so a command name such as <c>Get-Credential</c> is not one. The dash is read
	/// first and its left neighbour after it, so that a search for a parameter runs on the dash and not on a look-behind at every character.
	/// </summary>
	private const string _PARAMETER_START = _DASH + @"(?<![\w\-\u2013\u2014\u2015]" + _DASH + @")";

	/// <summary>A parameter name ends where no name character follows; the colon syntax <c>-Name:value</c> ends on the colon.</summary>
	private const string _PARAMETER_END = @"(?![A-Za-z0-9_])";

	/// <summary>The name of <c>-Credential</c> under every prefix from <c>Cr</c> (the shortest unique one on both editions), without its dash: also a hashtable key.</summary>
	private const string _CREDENTIAL_NAME_PREFIXES = @"Cr(?:edential|edentia|edenti|edent|eden|ede|ed|e)?";

	/// <summary>The name of <c>-Authentication</c> under every prefix from <c>Au</c>, without its dash: also a hashtable key.</summary>
	private const string _AUTHENTICATION_NAME_PREFIXES = @"Au(?:thentication|thenticatio|thenticati|thenticat|thentica|thentic|thenti|thent|then|the|th|t)?";

	/// <summary>The name of <c>-SessionVariable</c> of the web cmdlets under every prefix from <c>Se</c>, and its alias, without the dash.</summary>
	private const string _SESSION_VARIABLE_NAME_PREFIXES = @"(?:Se(?:ssionVariable|ssionVariabl|ssionVariab|ssionVaria|ssionVari|ssionVar|ssionVa|ssionV|ssion|ssio|ssi|ss|s)?|SV)";

	/// <summary><c>-Credential</c> under every prefix from <c>-Cr</c> (the shortest unique one on both editions).</summary>
	private const string _PARAMETER_CREDENTIAL = _PARAMETER_START + _CREDENTIAL_NAME_PREFIXES + _PARAMETER_END;

	/// <summary><c>-Authentication</c> under every prefix from <c>-Au</c>.</summary>
	private const string _PARAMETER_AUTHENTICATION = _PARAMETER_START + _AUTHENTICATION_NAME_PREFIXES + _PARAMETER_END;

	/// <summary><c>-TypeName</c> of <c>New-Object</c> under every prefix from <c>-T</c>.</summary>
	private const string _PARAMETER_TYPE_NAME =
		_PARAMETER_START + @"T(?:ypeName|ypeNam|ypeNa|ypeN|ype|yp|y)?" + _PARAMETER_END;

	/// <summary><c>-ArgumentList</c> of <c>New-Object</c> under every prefix from <c>-A</c>.</summary>
	private const string _PARAMETER_ARGUMENT_LIST =
		_PARAMETER_START + @"A(?:rgumentList|rgumentLis|rgumentLi|rgumentL|rgument|rgumen|rgume|rgum|rgu|rg|r)?" + _PARAMETER_END;

	/// <summary><c>-Property</c> of <c>New-Object</c> under every prefix from <c>-Pr</c> (the wider set of the two editions).</summary>
	private const string _PARAMETER_PROPERTY =
		_PARAMETER_START + @"Pr(?:operty|opert|oper|ope|op|o)?" + _PARAMETER_END;

	/// <summary>The characters between a parameter and its value: the colon syntax, or blanks, a continuation or an inline comment.</summary>
	private const string _PARAMETER_VALUE_SEPARATOR = @"(?::[ \t]*|" + _GAP + ")";

	// ------------------------------------------------------------------------------------------
	// Gaps inside one logical line
	// ------------------------------------------------------------------------------------------

	/// <summary>A block comment between the tokens of a command, bounded so an unclosed opening cannot scan to the end of the file.</summary>
	private const string _INLINE_COMMENT = @"<#(?>(?>[^#]|#(?!>)){0,500})#>";

	/// <summary>Blanks, a backtick continuation or an inline comment: what may stand between two tokens of one command.</summary>
	private const string _GAP = @"(?:[ \t]|`[ \t]*\n|" + _INLINE_COMMENT + @")+";

	/// <summary>
	/// The rest of one logical line of a command: any character but a line end, a pipe or a comment opening, a
	/// backtick continuing the line, or an inline comment. Bounded at 1200 characters and lazy, so the first parameter
	/// that satisfies the expression ends the window.
	/// </summary>
	private const string _COMMAND_WINDOW = @"(?:(?:(?>[^\n`|<\-\u2013\u2014\u2015]+)|`\n|`.|<(?!#)|" + _INLINE_COMMENT + @"|" + _DASH + @")" + _BACKTRACK_POINT + @"){0,150}?";

	private const string _LINE_WINDOW_FIVE = @"(?:(?:[^\n]*\n)" + _BACKTRACK_POINT + @"){0,5}?[^\n]*?";
	private const string _LINE_WINDOW_EIGHT = @"(?:(?:[^\n]*\n)" + _BACKTRACK_POINT + @"){0,8}?[^\n]*?";
	private const string _LINE_WINDOW_TEN = @"(?:(?:[^\n]*\n)" + _BACKTRACK_POINT + @"){0,10}?[^\n]*?";
	private const string _LINE_WINDOW_TWENTY = @"(?:(?:[^\n]*\n)" + _BACKTRACK_POINT + @"){0,20}?[^\n]*?";

	// ------------------------------------------------------------------------------------------
	// The logical line of a command, with the bracket continuation
	// ------------------------------------------------------------------------------------------

	/// <summary>
	/// A balanced bracket group, up to four levels deep, that may span lines and hold a pipe: <c>( )</c>, <c>@( )</c>, <c>$( )</c>, <c>@{ }</c>, <c>{ }</c> and <c>[ ]</c>
	/// are one shape here, because a logical line continues across a line break or a pipe inside any of them. The closing character is not
	/// matched to the opening one: what the window needs is the extent. Atomic at every level, and each level's run holds no bracket, so a group
	/// can be read in one way only.
	/// </summary>
	private const string _BRACKET_GROUP_INNERMOST = @"[(\[{][^(){}\[\]]*[)\]}]";

	private const string _BRACKET_GROUP_THIRD = @"[(\[{](?>[^(){}\[\]]+|" + _BRACKET_GROUP_INNERMOST + @")*[)\]}]";

	private const string _BRACKET_GROUP_SECOND = @"[(\[{](?>[^(){}\[\]]+|" + _BRACKET_GROUP_THIRD + @")*[)\]}]";

	private const string _BRACKET_GROUP = @"[(\[{](?>[^(){}\[\]]+|" + _BRACKET_GROUP_SECOND + @")*[)\]}]";

	/// <summary>An inline block comment between the tokens of a command, bounded; written without an atomic body so that it reads the same from either end.</summary>
	private const string _BLOCK_COMMENT_BETWEEN_TOKENS = @"<#(?:[^#]|#(?!>)){0,500}#>";

	/// <summary>
	/// One character, or one indivisible token, of the logical line of a command, the dash apart: an ordinary character; a backtick escape; a backtick
	/// continuing the line; a comment opening that is not a block comment; an inline block comment; and a bracket group, which may hold line breaks and pipes. A
	/// bracket that never closes is an ordinary character, so the window then ends at the line end or at its bound. A line end or a pipe
	/// outside every bracket ends the command. A unit is never a run, so that the same window can be read from either end without a choice of where a run stops.
	/// </summary>
	private const string _LOGICAL_LINE_UNIT_WITHOUT_DASH =
		@"`[^\n]|[^\n`|<(){}\[\]\-\u2013\u2014\u2015]|`\n|<(?!#)|" + _BLOCK_COMMENT_BETWEEN_TOKENS + @"|(?>" + _BRACKET_GROUP + @"|[(){}\[\]])";

	/// <summary>
	/// The logical line of a command, bounded at 1200 units and lazy, and the same language read forward from the command name and backward from a parameter. The bound
	/// is in characters and bracket groups: a group is one unit however long.
	/// </summary>
	private const string _LOGICAL_LINE_WINDOW = @"(?:(?:" + _LOGICAL_LINE_UNIT_WITHOUT_DASH + @"|" + _DASH + @")" + _BACKTRACK_POINT + @"){0,1200}?";

	/// <summary>
	/// The logical line of a command read forward only, from a parameter to the end of its command: the same language as <see cref="_LOGICAL_LINE_WINDOW"/> with the ordinary
	/// characters taken in atomic runs, which a forward read can do and a backward one cannot, so a long line costs a run and not a character. Bounded at 150 units.
	/// </summary>
	private const string _LOGICAL_LINE_FORWARD_WINDOW =
		@"(?:(?:(?>[^\n`|<(){}\[\]\-\u2013\u2014\u2015]+)|`[^\n]|`\n|<(?!#)|" + _BLOCK_COMMENT_BETWEEN_TOKENS + @"|(?>" + _BRACKET_GROUP + @"|[(){}\[\]])|" + _DASH + @")" + _BACKTRACK_POINT + @"){0,150}?";

	/// <summary>The backward window, which refuses to cross a <c>-Authentication</c> parameter: a command that carries it before the wanted parameter is the explicit class's.</summary>
	private const string _LOGICAL_LINE_WINDOW_BEFORE_AUTHENTICATION =
		@"(?:(?:" + _LOGICAL_LINE_UNIT_WITHOUT_DASH + @"|(?!" + _PARAMETER_AUTHENTICATION + @")" + _DASH + @")" + _BACKTRACK_POINT + @"){0,1200}?";

	// ------------------------------------------------------------------------------------------
	// Names and variables
	// ------------------------------------------------------------------------------------------

	private const string _NAME = @"[A-Za-z_]\w*";

	/// <summary>The scope qualifiers that denote the same name inside one file (<c>$script:x</c> is <c>$x</c>).</summary>
	private const string _SCOPE = @"(?:script|global|local|private):";

	/// <summary>
	/// A variable in any spelling: <c>$x</c>, <c>${x}</c>, <c>$script:x</c>, <c>$env:NAME</c>, <c>${env:NAME}</c>. The shared
	/// argument form reads all of them.
	/// </summary>
	private const string _VARIABLE = @"\$(?:\{[^{}\n]{1,120}\}|[A-Za-z_]\w*(?::[A-Za-z_]\w*)?)";

	/// <summary>
	/// A variable that the symbol table can resolve, captured as its BARE NAME in the named group: the dollar sign, the
	/// braces and a scope qualifier stand outside the group, so <c>$x</c>, <c>${x}</c> and <c>$script:x</c> read one key. Any other
	/// colon form (<c>$using:x</c>, <c>$env:x</c>) is not a plain name and is read by the shared argument instead.
	/// </summary>
	private const string _RESOLVABLE_VARIABLE_OPENING = @"\$(?:\{[ \t]*(?:" + _SCOPE + @")?(?<scheme>" + _NAME + @")[ \t]*\}|(?:" + _SCOPE + @")?(?<scheme>" + _NAME + @"))(?![\w:.\[(])";

	private const string _CALL_ARGUMENTS = @"\((?>[^()\n]|\([^()\n]*\))*\)";

	private const string _MEMBER_CHAIN = @"(?>(?:\.|::)[A-Za-z_]\w*(?:" + _CALL_ARGUMENTS + @")?)*";

	private const string _INDEXER = @"\[(?:[ \t]*(?:'[^'\n]*'|""[^""\n]*"")[ \t]*|[^\[\](),""';\n]{1,120})\]";

	private const string _INDEXERS = @"(?:" + _INDEXER + @"){0,2}";

	private const string _TYPE_LITERAL = @"\[[A-Za-z_][\w.]*(?:\[\])?\]";

	private const string _STATIC_MEMBER = @"\[[A-Za-z_][\w.]*\]::[A-Za-z_]\w*(?:" + _CALL_ARGUMENTS + @")?" + _MEMBER_CHAIN;

	// ------------------------------------------------------------------------------------------
	// Quoted literals: the six kinds
	// ------------------------------------------------------------------------------------------

	/// <summary>The single-quoted family: the ASCII quote and the typographic pair, the doubled close an escape, newlines content.</summary>
	private const string _SINGLE_QUOTED = @"['\u2018](?>[^'\u2018\u2019\n]|['\u2019]{2})*['\u2019]";

	/// <summary>The double-quoted family: the ASCII quote and the typographic pair, the backtick and the doubled close escapes.</summary>
	private const string _DOUBLE_QUOTED = @"[""\u201C](?>[^""\u201C\u201D`\n]|`.|[""\u201D]{2})*[""\u201D]";

	/// <summary>A here-string of up to thirty lines: the closer stands at the start of a line, and the body holds no line that begins with it.</summary>
	private const string _HERE_STRING = @"@['""][ \t]*\n(?>(?!['""]@)[^\n]*\n){0,30}['""]@";

	private const string _STRING_LITERAL =
		@"(?:" + _SINGLE_QUOTED + @"|" + _DOUBLE_QUOTED + @"|" + _HERE_STRING + @")";

	private const string _SUB_EXPRESSION = @"\$\((?>[^()\n]|\((?>[^()\n]|\([^()\n]*\))*\))*\)";

	private const string _PARENTHESISED = @"\((?>[^()\n]|\([^()\n]*\))*\)";

	private const string _BRACKETED = @"\[(?>[^\[\]\n]|\[[^\[\]\n]*\])*\]";

	private const string _BRACED = @"\{[^{}\n]*\}";

	/// <summary>A run of the characters an expression is made of between its delimiters: names, dots, colons, a dollar sign, digits, an operator attached to its operand.</summary>
	private const string _CODE_RUN = @"[^\s,;|(){}\[\]'""`&<>+\u2018\u2019\u201C\u201D]+";

	/// <summary>
	/// THE SHARED ARGUMENT FORM. One argument is a sequence of units with no top-level comma, blank or statement end: a quoted literal of any of
	/// the six kinds, a parenthesised expression (a sub-expression is its dollar sign and this), an indexer or a type literal in brackets, a braced
	/// group (a braced variable name, a script block, a hashtable body) and a run of the characters in between. What the form promises is the
	/// ARGUMENT'S EXTENT, never its spelling, so a member chain with calls, a cast, an environment variable and a static call are all one argument;
	/// an argument that carries any unit but a literal is code, never a literal. It is atomic: no unit can begin with a character another
	/// ends on, so it cannot be split in more than one way, and the arguments of a creation read on the source generator's output without growth.
	/// </summary>
	private const string _ARGUMENT =
		@"(?>(?:" + _STRING_LITERAL + @"|" + _PARENTHESISED + @"|" + _BRACKETED + @"|" + _BRACED + @"|" + _CODE_RUN + @")+)";

	/// <summary>The same extent without a quoted literal: the argument of a half that must not be a literal, so a quote ends it.</summary>
	private const string _CODE_ARGUMENT =
		@"(?>(?:" + _PARENTHESISED + @"|" + _BRACKETED + @"|" + _BRACED + @"|" + _CODE_RUN + @")+)";

	// ------------------------------------------------------------------------------------------
	// The assembled scheme
	// ------------------------------------------------------------------------------------------

	/// <summary>A quoted word with no blank in it: a fragment of a scheme, never a scheme followed by its token.</summary>
	private const string _QUOTED_WORD = _QUOTE_OPENING + @"[A-Za-z0-9_\-]*" + _QUOTE_CLOSING;

	private const string _QUOTED_LINE = @"(?:" + _SINGLE_QUOTED + @"|" + _DOUBLE_QUOTED + @")";

	/// <summary>An operator or a method that changes the literal before it: <c>-replace</c>, <c>-join</c>, <c>-split</c>, <c>-f</c>, <c>.ToUpper()</c>.</summary>
	private const string _ASSEMBLING_OPERATOR_OR_METHOD =
		@"[ \t]*(?:-(?:replace|creplace|ireplace|join|split|csplit|isplit|f)(?![A-Za-z0-9_])|\.[A-Za-z_])";

	/// <summary>A concatenation with a literal that continues the word: <c>'Ba' + 'sic'</c>. A literal that begins with a blank is the token of a scheme, never its continuation.</summary>
	private const string _ASSEMBLING_CONCATENATION = @"[ \t]*\+[ \t]*" + _QUOTE_OPENING + @"[A-Za-z0-9]";

	/// <summary>A scheme word that something follows to change it.</summary>
	private const string _ASSEMBLED_WORD = _QUOTED_WORD + @"(?=" + _ASSEMBLING_OPERATOR_OR_METHOD + @"|" + _ASSEMBLING_CONCATENATION + @")";

	/// <summary>An array of literals joined with nothing between them: <c>('Ba', 'sic') -join ''</c>.</summary>
	private const string _ASSEMBLED_JOIN =
		@"\([ \t]*" + _QUOTED_LINE + @"(?:[ \t]*,[ \t]*" + _QUOTED_LINE + @")+[ \t]*\)(?=[ \t]*-join(?![A-Za-z0-9_])[ \t]*" + _QUOTE_OPENING + _QUOTE_CLOSING + @")";

	/// <summary>A format whose first item is a placeholder, so the scheme comes from the arguments: <c>'{0}' -f …</c>.</summary>
	private const string _ASSEMBLED_FORMAT =
		_QUOTE_OPENING + @"\{[0-9]+\}[^'""‘’“”\n]*" + _QUOTE_CLOSING + @"(?=[ \t]*-f(?![A-Za-z0-9_]))";

	/// <summary>
	/// THE ASSEMBLED-SCHEME RULE. A scheme literal followed by an operator or a method that changes it is an assembled value, and its verdict is
	/// INDETERMINATE: never a comparison of its first fragment. The engine reads a literal in the scheme capture as the whole body between its first and
	/// last string-body position, so no literal is ever put in the scheme group of an assembled value: the group does not participate, and the
	/// verdict is the engine's indeterminate one.
	/// </summary>
	private const string _ASSEMBLED_SCHEME = @"(?:" + _ASSEMBLED_WORD + @"|" + _ASSEMBLED_FORMAT + @"|" + _ASSEMBLED_JOIN + @")";

	/// <summary>An assembled scheme as a value whose own extent nothing reads: an optional opening parenthesis, then the assertion.</summary>
	private const string _ASSEMBLED_SCHEME_VALUE = @"(?:\([ \t]*)?(?=" + _ASSEMBLED_SCHEME + @")";

	/// <summary>An assembled scheme as one argument of a call: the assertion, then the extent of the argument up to its comma.</summary>
	private const string _ASSEMBLED_SCHEME_ARGUMENT =
		@"(?=" + _ASSEMBLED_SCHEME + @")(?>(?:" + _STRING_LITERAL + @"|" + _PARENTHESISED + @"|" + _BRACKETED + @"|[^\n,;()\[\]{}'""‘’“”])+)";

	// ------------------------------------------------------------------------------------------
	// Creation forms
	// ------------------------------------------------------------------------------------------

	private const string _NEW_OBJECT = @"(?<![\w\-$.])New-Object(?![\w\-])";

	private const string _NAMESPACE_PREFIX = @"(?:[A-Za-z_]\w*\.){0,8}";

	private const string _QUOTE_OPENING = @"[""'\u2018\u201C]";

	private const string _QUOTE_CLOSING = @"[""'\u2019\u201D]";

	/// <summary>
	/// A password given as a conversion of a literal: <c>(ConvertTo-SecureString '…' -AsPlainText -Force)</c>, in any order of the string and the switches
	/// with the plain-text switch present. The credential capture is that LITERAL, so the literal-credential signal comes from its zone; the same
	/// conversion of a value that is not a literal is read as a code argument and sets nothing.
	/// </summary>
	private const string _SECURE_STRING_FROM_LITERAL =
		@"\([ \t\n]*ConvertTo-SecureString(?![\w\-])(?=(?:(?:[^()\n]|\([^()\n]*\))" + _BACKTRACK_POINT + @"){0,300}?" + _PARAMETER_AS_PLAIN_TEXT + @")"
		+ @"(?:[ \t]+(?:" + _PARAMETER_AS_PLAIN_TEXT + @"|" + _PARAMETER_FORCE + @"|" + _PARAMETER_STRING + @"[ \t]*:?))*[ \t]*:?[ \t]*(?<credential>" + _STRING_LITERAL + @")"
		+ @"(?:[ \t]+(?:" + _PARAMETER_AS_PLAIN_TEXT + @"|" + _PARAMETER_FORCE + @"))*[ \t\n]*\)";

	/// <summary>The password of a credential: a conversion of a literal, whose credential capture is the literal, or any argument.</summary>
	private const string _PASSWORD_ARGUMENT = @"(?:" + _SECURE_STRING_FROM_LITERAL + @"|(?<credential>" + _ARGUMENT + @"))";

	/// <summary>A pair of arguments, the user name and the password.</summary>
	private const string _PAIR = @"(?<user>" + _ARGUMENT + @")\s*,\s*" + _PASSWORD_ARGUMENT;

	/// <summary>
	/// What the argument list of a creation takes: a pair as a comma list or as <c>@( … )</c>, or a single variable holding the array. A
	/// variable cannot be split, so it is the credential capture and no literal comes from it.
	/// </summary>
	private const string _ARGUMENT_LIST_VALUE =
		@"(?:(?:@\(\s*)?" + _PAIR + @"(?:\s*\))?|(?<credential>" + _VARIABLE + @"))";

	/// <summary>
	/// The heads of a creation of one type, before its argument list: <c>New-Object T(</c> and <c>New-Object T </c> with or without the named
	/// parameters in front of either, the named list FIRST (the type is then read by a look-ahead, so one argument list serves both orders), and
	/// <c>[T]::new(</c>. The type fragment carries the anchor.
	/// </summary>
	private const string _CREATION_HEAD_OPENING = @"(?:" + _NEW_OBJECT + @"(?:" + _GAP + @"(?:" + _PARAMETER_TYPE_NAME + _PARAMETER_VALUE_SEPARATOR + @")?";

	private const string _CREATION_HEAD_AFTER_TYPE = @"(?:[ \t]*\(\s*|" + _GAP + @"(?:" + _PARAMETER_ARGUMENT_LIST + _PARAMETER_VALUE_SEPARATOR + @")?)";

	private const string _UPC_NAME = "UsernamePasswordCredentials";

	/// <summary>The SDK credential type: bare, partly or fully qualified, or quoted. The anchor is the name or the opening delimiter of the quoted name.</summary>
	private const string _UPC_TYPE =
		@"(?:" + _NAMESPACE_PREFIX + @"(?<construct>" + _UPC_NAME + @")(?!\w)|(?<construct>" + _QUOTE_OPENING + @")" + _NAMESPACE_PREFIX + _UPC_NAME + _QUOTE_CLOSING + @")";

	private const string _UPC_BRACKETED = @"\[" + _NAMESPACE_PREFIX + @"(?<construct>" + _UPC_NAME + @")\]";

	/// <summary>
	/// Every creation form of the SDK credential type with a user name and a password: <c>New-Object T(a, b)</c>, <c>New-Object T (a, b)</c>,
	/// the positional <c>New-Object T a, b</c>, <c>-TypeName</c> and <c>-ArgumentList</c> in either order under every abbreviation, and
	/// <c>[T]::new(a, b)</c>. ONE argument list serves every head.
	/// </summary>
	private const string _UPC_CREATION =
		_CREATION_HEAD_OPENING + _UPC_TYPE + _CREATION_HEAD_AFTER_TYPE
		+ @"|" + _GAP + _PARAMETER_ARGUMENT_LIST + _PARAMETER_VALUE_SEPARATOR + @"(?=" + _COMMAND_WINDOW + _PARAMETER_TYPE_NAME + _PARAMETER_VALUE_SEPARATOR + _UPC_TYPE + @"))"
		+ @"|" + _UPC_BRACKETED + @"::new[ \t]*\(\s*)" + _ARGUMENT_LIST_VALUE + @"(?:\s*\))?";

	private const string _NETWORK_CREDENTIAL_NAME = "NetworkCredential";

	private const string _NETWORK_CREDENTIAL_TYPE =
		@"(?:" + _NAMESPACE_PREFIX + _NETWORK_CREDENTIAL_NAME + @"(?!\w)|" + _QUOTE_OPENING + _NAMESPACE_PREFIX + _NETWORK_CREDENTIAL_NAME + _QUOTE_CLOSING + @")";

	private const string _NETWORK_CREDENTIAL_BRACKETED = @"\[" + _NAMESPACE_PREFIX + _NETWORK_CREDENTIAL_NAME + @"\]";

	/// <summary>The pair of a network credential, with the optional third argument (the domain), which the class does not read.</summary>
	private const string _NETWORK_CREDENTIAL_PAIR = _PAIR + @"(?:\s*,\s*" + _CODE_ARGUMENT + @")?";

	private const string _NETWORK_CREDENTIAL_ARGUMENT_LIST_VALUE =
		@"(?:(?:@\(\s*)?" + _NETWORK_CREDENTIAL_PAIR + @"(?:\s*\))?|(?<credential>" + _VARIABLE + @"))";

	/// <summary>Every creation form of a network credential. Construction alone is not a class: the sink that receives it decides.</summary>
	private const string _NETWORK_CREDENTIAL_CREATION =
		_CREATION_HEAD_OPENING + _NETWORK_CREDENTIAL_TYPE + _CREATION_HEAD_AFTER_TYPE
		+ @"|" + _GAP + _PARAMETER_ARGUMENT_LIST + _PARAMETER_VALUE_SEPARATOR + @"(?=" + _COMMAND_WINDOW + _PARAMETER_TYPE_NAME + _PARAMETER_VALUE_SEPARATOR + _NETWORK_CREDENTIAL_TYPE + @"))"
		+ @"|" + _NETWORK_CREDENTIAL_BRACKETED + @"::new[ \t]*\(\s*)" + _NETWORK_CREDENTIAL_ARGUMENT_LIST_VALUE + @"(?:\s*\))?";

	/// <summary>
	/// What a handler, a client or a sink takes as its <c>Credentials</c>: a network credential constructed in place (parenthesised or not),
	/// a <c>.GetNetworkCredential()</c> call, or the cast of a credential variable to the type. The default credentials and
	/// <c>$true</c> are integrated Windows authentication and are none of these.
	/// </summary>
	private const string _CREDENTIAL_ASSIGNED =
		@"(?:(?:\([ \t\n]*)?" + _NETWORK_CREDENTIAL_CREATION + @"(?:[ \t\n]*\))?"
		+ @"|(?<credential>" + _VARIABLE + _MEMBER_CHAIN + @"\.GetNetworkCredential[ \t]*\([ \t]*\))"
		+ @"|" + _NETWORK_CREDENTIAL_BRACKETED + @"[ \t]*(?<credential>" + _VARIABLE + @"))";

	// ------------------------------------------------------------------------------------------
	// The Authorization header: key, value shapes V1 to V8
	// ------------------------------------------------------------------------------------------

	/// <summary>A scheme word: the first word of a header value.</summary>
	private const string _SCHEME_WORD = @"[A-Za-z][A-Za-z0-9_\-]*";

	private const string _TOKEN_FIRST_CHARACTER = @"[^""'\u2018\u2019\u201C\u201D\s{$`]";

	private const string _TOKEN_REMAINDER = @"[^""'\u2018\u2019\u201C\u201D\n]*";

	/// <summary>An interpolation written bare: <c>$name</c> or <c>${name}</c>. It sits in the body of the string, so it is never captured as the credential.</summary>
	private const string _BARE_INTERPOLATION = @"\$(?:\{[^{}\n]{1,120}\}|[A-Za-z_]\w*(?::[A-Za-z_]\w*)?)";

	/// <summary>
	/// THE VALUE SHAPES OF A SCHEME-BEARING HEADER, V1 to V7 (V8 is the variable that carries one of them). Each captures
	/// the scheme word inside the literal, or the scheme variable by its bare name, and the credential where it is code or a
	/// literal token. A credential that is a bare variable inside a double-quoted string is not captured: it sits in the string
	/// body, and capturing it would read an interpolated variable as a literal credential. The shapes that end in an argument
	/// (V2, V4 and both spellings of V6) read it once, behind their own openings.
	/// </summary>
	private const string _HEADER_VALUE =
		// V2, V4 and V6: an opening, then the credential argument.
		@"(?:(?:(?!" + _ASSEMBLED_WORD + @")(?:" + _QUOTE_OPENING + @"(?<scheme>" + _SCHEME_WORD + @")[ \t]*" + _QUOTE_CLOSING + @"[ \t]*\+[ \t]*"
		+ @"|[""'](?<scheme>" + _SCHEME_WORD + @")[ \t]+\{0\}[""'][ \t]*-f[ \t]*"
		+ @"|" + _RESOLVABLE_VARIABLE_OPENING + @"[ \t]*\+[ \t]*[""'][ \t]*[""'][ \t]*\+[ \t]*"
		+ @"|" + _VARIABLE + _MEMBER_CHAIN + _INDEXERS + @"[ \t]*\+[ \t]*[""'][ \t]*[""'][ \t]*\+[ \t]*))(?<credential>" + _ARGUMENT + @")"
		// V1: one fully literal string, 'Basic <token>' or "Basic <token>"; partly literal, the string is followed by a concatenated expression, so the credential reaches into code.
		+ @"|" + _QUOTE_OPENING + @"(?<scheme>" + _SCHEME_WORD + @")[ \t]+(?:(?<credential>" + _TOKEN_FIRST_CHARACTER + _TOKEN_REMAINDER + @")" + _QUOTE_CLOSING + @"(?![ \t]*\+)"
		+ @"|(?<credential>" + _TOKEN_FIRST_CHARACTER + _TOKEN_REMAINDER + _QUOTE_CLOSING + @"[ \t]*\+[ \t]*" + _ARGUMENT + @"))"
		// V3: an interpolated string whose first word is the scheme.
		+ @"|[""\u201C](?<scheme>" + _SCHEME_WORD + @")[ \t]+(?:(?<credential>" + _SUB_EXPRESSION + @")|" + _BARE_INTERPOLATION + @")[^""\u201D\n]*[""\u201D]"
		// V5: a format or a join method.
		+ @"|\[(?:System\.)?string\]::Format[ \t]*\([ \t]*[""'](?<scheme>" + _SCHEME_WORD + @")[ \t]+\{0\}[""'][ \t]*,[ \t]*(?<credential>" + _ARGUMENT + @")[ \t]*\)"
		+ @"|(?!" + _ASSEMBLED_JOIN + @")\([ \t]*[""'](?<scheme>" + _SCHEME_WORD + @")[""'][ \t]*,[ \t]*(?<credential>" + _ARGUMENT + @")[ \t]*\)[ \t]*-join[ \t]*[""'][ \t]*[""']"
		// V6 interpolated, the scheme variable inside a sub-expression, which is code.
		+ @"|[""\u201C]\$\([ \t]*\$(?:" + _SCOPE + @")?(?<scheme>" + _NAME + @")[ \t]*\)[ \t]+(?:(?<credential>" + _SUB_EXPRESSION + @")|" + _BARE_INTERPOLATION + @")[^""\u201D\n]*[""\u201D]"
		// V6 interpolated, the scheme variable written bare: it sits in the string body, where the symbol table is not
		// consulted as code, so no scheme group is declared and the verdict is indeterminate (the finding is kept at Medium).
		+ @"|[""\u201C]" + _BARE_INTERPOLATION + @"[ \t]+(?:(?<credential>" + _SUB_EXPRESSION + @")|" + _BARE_INTERPOLATION + @"|[^""\u201D$`\n]+)[^""\u201D\n]*[""\u201D]"
		// V7: a call to a function or cmdlet whose name carries the scheme word as a name component.
		+ @"|\(?[ \t]*(?<![\w\-$.@])(?=[\w\-]*Basic(?-i:(?![a-z])))[A-Za-z_][\w\-]*(?![\w\-])"
		// An assembled scheme: no scheme group, so the verdict is indeterminate and the finding is kept.
		+ @"|" + _ASSEMBLED_SCHEME_VALUE + @")";

	/// <summary>The header name as a key: bare, or quoted with the anchor on the opening delimiter. The whole word <c>Authorization</c> only.</summary>
	private const string _AUTHORIZATION_KEY =
		@"(?:(?<header>Authorization)(?![\w\-])|(?<header>" + _QUOTE_OPENING + @")Authorization" + _QUOTE_CLOSING + @")";

	/// <summary>The header name as the first argument of a write: a quoted key, anchored on the opening delimiter.</summary>
	private const string _AUTHORIZATION_KEY_LITERAL = @"(?<header>" + _QUOTE_OPENING + @")Authorization" + _QUOTE_CLOSING;

	/// <summary>
	/// The writes of a header on any receiver (its type is not recoverable from one file), up to the value: the methods
	/// <c>Add</c>, <c>Set</c>, <c>Append</c>, <c>TryAddWithoutValidation</c>, <c>AddHeader</c> and the request object's <c>setRequestHeader</c>; the
	/// indexer; and the dot-assignment, whose anchor is the member name.
	/// </summary>
	private const string _HEADER_KEY_WRITE =
		@"(?:\.(?:Add|Set|Append|TryAddWithoutValidation|AddHeader|setRequestHeader)[ \t]*\([ \t\n]*" + _AUTHORIZATION_KEY_LITERAL + @"[ \t\n]*,[ \t\n]*"
		+ @"|\[[ \t]*" + _AUTHORIZATION_KEY_LITERAL + @"[ \t]*\][ \t]*=(?!=)[ \t]*"
		+ @"|\.(?<header>Authorization)(?![\w\-])[ \t]*=(?!=)[ \t]*)";

	/// <summary>
	/// V8: the value is a variable assigned one of V1 to V7 within the five lines before the write. The match starts at the
	/// assignment's <c>=</c>, which has exactly one place to begin, and reads the variable name back from the text before it. The
	/// name is read back case-sensitively: the source generator has no case-insensitive back-reference, and the cost is that a variable
	/// spelled in two cases is not tied.
	/// </summary>
	private const string _VARIABLE_ASSIGNMENT_OPENING =
		@"=(?<=\$(?:" + _SCOPE + @")?(?<v>" + _NAME + @")[ \t]*=)[ \t]*";

	private const string _VARIABLE_READ_BACK = @"\$(?:" + _SCOPE + @")?(?-i:\k<v>)(?![\w:.\[(])";

	// ------------------------------------------------------------------------------------------
	// Web cmdlets
	// ------------------------------------------------------------------------------------------

	private const string _COMMAND_START = @"(?<![\w\-$.])";

	/// <summary>The web cmdlets and their aliases. <c>curl</c> and <c>wget</c> are read as the alias only where the parameter that decides the class is a PowerShell parameter of two or more letters, which every parameter expression here requires.</summary>
	private const string _WEB_CMDLET = _COMMAND_START + @"(?:Invoke-RestMethod|Invoke-WebRequest|irm|iwr|curl|wget)(?![\w\-])";

	// ------------------------------------------------------------------------------------------
	// The classes
	// ------------------------------------------------------------------------------------------

	/// <summary>
	/// PS-001. The credential type of the SDK with a user name and a password, in every creation form of the shared
	/// vocabulary. Both arguments accept the shared argument form; a literal one is High as a variable is.
	/// </summary>
	[GeneratedRegex(
		_UPC_CREATION,
		RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture,
		matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex SdkUserNamePasswordCredentialsCreation();

	/// <summary>The client's own type name, bare, qualified or quoted. Not an anchor.</summary>
	private const string _LEGACY_CLIENT_NAME = "RSAPIClient";

	private const string _LEGACY_CLIENT_TYPE =
		@"(?:" + _NAMESPACE_PREFIX + _LEGACY_CLIENT_NAME + @"(?!\w)|" + _QUOTE_OPENING + _NAMESPACE_PREFIX + _LEGACY_CLIENT_NAME + _QUOTE_CLOSING + @")";

	/// <summary>The rest of the logical line between the client and the credential nested in it: the endpoint never decides the class.</summary>
	private const string _LEGACY_CLIENT_ARGUMENT_WINDOW = @"(?:(?:[^\n`]|`\n|`.)" + _BACKTRACK_POINT + @"){0,600}?";

	/// <summary>
	/// PS-002. The legacy client created with the SDK credential nested in its arguments. The endpoint, the first argument, is
	/// whatever it is (a parenthesised creation, a cast, a static call); what the class requires is the nested credential. The anchor is the
	/// nested type's own token, so the same anchor position as PS-001 lets one finding stand for both.
	/// </summary>
	[GeneratedRegex(
		@"(?:(?:" + _NEW_OBJECT + @"(?:" + _GAP + @"(?:" + _PARAMETER_TYPE_NAME + _PARAMETER_VALUE_SEPARATOR + @")?" + _LEGACY_CLIENT_TYPE
		+ @"|(?=" + _COMMAND_WINDOW + _PARAMETER_TYPE_NAME + _PARAMETER_VALUE_SEPARATOR + _LEGACY_CLIENT_TYPE + @"))"
		+ @"|\[" + _NAMESPACE_PREFIX + _LEGACY_CLIENT_NAME + @"\]::new[ \t]*\()" + _LEGACY_CLIENT_ARGUMENT_WINDOW + _UPC_CREATION + @")",
		RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture,
		matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex SdkLegacyRsapiCredentialsCreation();

	/// <summary>
	/// A scheme group that takes part in the match with an empty capture. The value written next to it is code the verdict cannot read as a literal and
	/// whose text must never be looked up as a name (a bare <c>Basic</c> would resolve against a script's own <c>$basic</c>), so the group participates
	/// and is indeterminate; only the assembled forms leave the group out, and the rule answers that on its own.
	/// </summary>
	private const string _UNREADABLE_SCHEME = @"(?<scheme>)";

	/// <summary>
	/// The argument of <c>-Authentication</c>: bare <c>Basic</c>, the enumeration member, an assembled scheme (no scheme group: the verdict is that the argument took no part), a quoted literal whose word the symbol-free verdict reads, alone or in parentheses, or a variable the symbol table resolves. A bare
	/// word or an enumeration member is code, which the verdict cannot read as a literal, so those two, and a member chain, take part with an empty
	/// scheme capture and stay indeterminate.
	/// </summary>
	private const string _AUTHENTICATION_VALUE =
		@"(?:" + _UNREADABLE_SCHEME + @"Basic(?![\w\-])"
		+ @"|" + _UNREADABLE_SCHEME + @"\[(?:[A-Za-z_]\w*\.){0,6}WebAuthenticationType\]::Basic(?![\w\-])"
		+ @"|" + _ASSEMBLED_SCHEME_VALUE
		+ @"|\([ \t]*(?<scheme>" + _STRING_LITERAL + @")[ \t]*\)"
		+ @"|(?<scheme>" + _STRING_LITERAL + @")"
		+ @"|" + _RESOLVABLE_VARIABLE_OPENING
		+ @"|" + _UNREADABLE_SCHEME + _VARIABLE + _MEMBER_CHAIN + _INDEXERS + @")";

	/// <summary>The same argument as a splat key's value: a quoted literal, alone or in parentheses, an assembled scheme, the enumeration member or a variable. A bare word is not a value in a hashtable.</summary>
	private const string _SPLAT_AUTHENTICATION_VALUE =
		@"(?:" + _UNREADABLE_SCHEME + @"\[(?:[A-Za-z_]\w*\.){0,6}WebAuthenticationType\]::Basic(?![\w\-])"
		+ @"|" + _ASSEMBLED_SCHEME_VALUE
		+ @"|\([ \t]*(?<scheme>" + _STRING_LITERAL + @")[ \t]*\)"
		+ @"|(?<scheme>" + _STRING_LITERAL + @")"
		+ @"|" + _RESOLVABLE_VARIABLE_OPENING
		+ @"|" + _UNREADABLE_SCHEME + _VARIABLE + _MEMBER_CHAIN + _INDEXERS + @")";

	/// <summary>
	/// THE SPLAT OF A VARIABLE, the token a splat read opens on: <c>@name</c>. It is the rare conjunct of a splat and its table, so the match is the splat itself
	/// and the table, its key and the web cmdlet are read back from it by look-behind (a bounded window of lines), the variable's name being the group.
	/// </summary>
	private const string _SPLAT_OF_A_VARIABLE = @"@(?<h>" + _NAME + @")(?![\w])";

	/// <summary>The opening of a hashtable assigned to the variable the splat names, read back from the group.</summary>
	private const string _SPLAT_TABLE_OPENING_OF_THE_SPLATTED_VARIABLE = @"\$(?:" + _SCOPE + @")?(?-i:\k<h>)[ \t]*=[ \t]*(?:" + _TYPE_LITERAL + @"[ \t]*)?@\{";

	/// <summary>What stands between a web cmdlet and its splat, on the same line, ending on the splat of the variable.</summary>
	private const string _SPLAT_USE_BEHIND = @"[^\n]{0,600}?@(?-i:\k<h>)";

	/// <summary>A bare or quoted hashtable key naming one parameter, anchored on the token or the opening delimiter.</summary>
	private const string _AUTHENTICATION_KEY =
		@"(?:(?<![\w\-.$])(?<construct>" + _AUTHENTICATION_NAME_PREFIXES + @")(?![\w\-])|(?<construct>" + _QUOTE_OPENING + @")" + _AUTHENTICATION_NAME_PREFIXES + _QUOTE_CLOSING + @")";

	/// <summary>
	/// The way a splatted hashtable gets its <c>Authentication</c>: the key inside the literal, or a write to the table after its creation, the dot, the indexer
	/// or the <c>Add</c> method. Each ends on the sign or the comma before the value, and its anchor is the key token, or the opening delimiter of a quoted key.
	/// </summary>
	private const string _SPLAT_AUTHENTICATION_WRITE =
		@"(?:" + _AUTHENTICATION_KEY + @"[ \t]*=[ \t]*"
		+ @"|\$(?:" + _SCOPE + @")?(?-i:\k<h>)(?:\.(?<construct>" + _AUTHENTICATION_NAME_PREFIXES + @")(?![\w\-])[ \t]*=(?!=)[ \t]*"
		+ @"|\[[ \t]*(?<construct>" + _QUOTE_OPENING + @")" + _AUTHENTICATION_NAME_PREFIXES + _QUOTE_CLOSING + @"[ \t]*\][ \t]*=(?!=)[ \t]*"
		+ @"|\.Add[ \t]*\([ \t]*(?<construct>" + _QUOTE_OPENING + @")" + _AUTHENTICATION_NAME_PREFIXES + _QUOTE_CLOSING + @"[ \t]*,[ \t]*))";

	private const string _WEB_CMDLET_OR_BITS_TRANSFER = @"(?:" + _WEB_CMDLET + @"|" + _COMMAND_START + @"Start-BitsTransfer(?![\w\-]))";

	/// <summary>Every command whose parameters the explicit scheme and the web credential classes read.</summary>
	private const string _ANY_WEB_COMMAND = @"(?:" + _WEB_CMDLET + @"|" + _NEW_WEB_SERVICE_PROXY + @"|" + _COMMAND_START + @"Start-BitsTransfer(?![\w\-]))";

	/// <summary>
	/// The rest of the logical line of a command, up to the next web command, which is another command's, a line end or a pipe: brackets are ordinary characters
	/// here, because the end of a command is only taken along to overlap, and an unclosed bracket must not cost a scan; a word is taken whole and asked once whether
	/// it is a web command. A match of the explicit scheme class or of the web
	/// credential class takes it along, because the two classes read the same command from two ends (a parameter, a splat) and the order of the two on the line is
	/// the author's: whichever comes first then spans the other, and the declared subsumption of the second by the first, decided on overlapping spans, keeps its effect.
	/// </summary>
	private const string _REST_OF_THE_COMMAND = @"(?>(?:(?!" + _ANY_WEB_COMMAND + @")[\w\-$.]+|[^\w\-$.\n|`]|`[^\n]|`\n)*)";

	/// <summary>The command name of PS-003 read back from a parameter's dash over the logical line of the command, bracket continuation included.</summary>
	private const string _EXPLICIT_COMMAND_BEHIND_A_DASH = @"(?<=" + _WEB_CMDLET_OR_BITS_TRANSFER + _LOGICAL_LINE_WINDOW + _DASH + @")";

	/// <summary>
	/// PS-003. <c>-Authentication Basic</c> on a web cmdlet (and <c>Start-BitsTransfer</c>), under every parameter prefix, the colon
	/// syntax, a backtick continuation, an inline comment and a bracketed argument that spans lines; the scheme in parentheses; and the key
	/// <c>Authentication = 'Basic'</c> of a hashtable that a web cmdlet splats within twenty lines, whether the key stands in the literal or is written after
	/// its creation. The anchor is the parameter token, or the key. A scheme that resolves to anything but Basic is suppressed by the verdict; an assembled
	/// scheme has no scheme group and is kept; <c>Bearer</c>, <c>OAuth</c>, <c>None</c> and the NTLM values never match as bare words.
	/// THE SHAPE: the command form opens at the parameter that decides the class and reads the command name back; the splat form opens at the splat and reads the
	/// table and its key back.
	/// </summary>
	[GeneratedRegex(
		@"(?:(?<construct>" + _PARAMETER_START + @"(?=" + _AUTHENTICATION_NAME_PREFIXES + _PARAMETER_END + @")" + _EXPLICIT_COMMAND_BEHIND_A_DASH + _AUTHENTICATION_NAME_PREFIXES + _PARAMETER_END + @")" + _PARAMETER_VALUE_SEPARATOR + _AUTHENTICATION_VALUE + _REST_OF_THE_COMMAND
		+ @"|" + _SPLAT_OF_A_VARIABLE + @"(?<=" + _SPLAT_TABLE_OPENING_OF_THE_SPLATTED_VARIABLE + @"[\s\S]{0,800}?" + _SPLAT_AUTHENTICATION_WRITE + @"(?=" + _SPLAT_AUTHENTICATION_VALUE + @")(?<=[=,][ \t]*)" + _LINE_WINDOW_TWENTY + _WEB_CMDLET_OR_BITS_TRANSFER + _SPLAT_USE_BEHIND + @")" + _REST_OF_THE_COMMAND + @")",
		RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture,
		matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex ExplicitBasicAuthenticationParameterConstruction();

	/// <summary>
	/// PS-004. The hashtable literal that holds the header key and a scheme-bearing value, however it is used: inline after
	/// <c>-Headers</c>, assigned to a variable, nested under a <c>Headers</c> key, or never used in the file. The key is the whole word only, so
	/// <c>Proxy-Authorization</c> and <c>X-Authorization-Mode</c> are not it. The anchor is the bare key, or the opening delimiter of a
	/// quoted one. The value is one of V1 to V7, or a variable assigned one of them within five lines before.
	/// </summary>
	[GeneratedRegex(
		@"(?:(?<w>(?<![\w\-.$])" + _AUTHORIZATION_KEY + @"[ \t]*=(?!=)[ \t]*)|" + _VARIABLE_ASSIGNMENT_OPENING + @")" + _HEADER_VALUE
		+ @"(?(w)|" + _LINE_WINDOW_FIVE + @"(?<![\w\-.$])" + _AUTHORIZATION_KEY + @"[ \t]*=(?!=)[ \t]*" + _VARIABLE_READ_BACK + @")",
		RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture,
		matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex AuthorizationHeaderHashtableLiteralConstruction();

	/// <summary>The one-string overload: <c>.Add("Authorization: Basic …")</c>, whose single argument begins with the whole word, a colon and then the value.</summary>
	private const string _ONE_STRING_HEADER_WRITE =
		@"\.(?:Add|Set|Append)[ \t]*\([ \t\n]*(?<header>" + _QUOTE_OPENING + @")Authorization[ \t]*:[ \t]*"
		+ @"(?:(?<scheme>" + _SCHEME_WORD + @")[ \t]+(?:(?<credential>" + _TOKEN_FIRST_CHARACTER + @"[^""'\u2019\u201D\n]*)|(?<credential>" + _SUB_EXPRESSION + @")[^""'\u2019\u201D\n]*|" + _BARE_INTERPOLATION + @"[^""'\u2019\u201D\n]*)"
		+ @"|" + _BARE_INTERPOLATION + @"[ \t]+[^""'\u2019\u201D\n]+)" + _QUOTE_CLOSING;

	/// <summary>
	/// PS-005. A write of the header on any receiver, by one of the methods, the indexer or the dot-assignment, with a value of V1 to V7
	/// or a variable assigned one of them within five lines before; and the one-string overload. A read is never matched, and a value
	/// that is a construction of the typed header is the next class's. The anchor is the opening delimiter of the key literal, the member
	/// name of the dot-assignment, or the opening delimiter of the one-string form.
	/// </summary>
	[GeneratedRegex(
		@"(?:(?:(?<w>" + _HEADER_KEY_WRITE + @")|" + _VARIABLE_ASSIGNMENT_OPENING + @")" + _HEADER_VALUE
		+ @"(?(w)|" + _LINE_WINDOW_FIVE + _HEADER_KEY_WRITE + _VARIABLE_READ_BACK + @")"
		+ @"|" + _ONE_STRING_HEADER_WRITE + @")",
		RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture,
		matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex AuthorizationHeaderStringKeyWriteConstruction();

	/// <summary>The enumeration member of the header name, written fully qualified, partly qualified or by its short name. Its anchor is the member.</summary>
	private const string _HEADER_ENUMERATION_MEMBER =
		@"\[(?:[A-Za-z_]\w*\.){0,6}HttpRequestHeader\]::(?<construct>Authorization)(?![\w\-])";

	private const string _ENUMERATION_HEADER_WRITE =
		@"(?:\.Headers\.(?:Add|Set)[ \t]*\([ \t\n]*" + _HEADER_ENUMERATION_MEMBER + @"[ \t\n]*,[ \t\n]*"
		+ @"|\.Headers[ \t]*\[[ \t]*" + _HEADER_ENUMERATION_MEMBER + @"[ \t]*\][ \t]*=(?!=)[ \t]*)";

	/// <summary>
	/// PS-006. The header written through the enumeration of the base class library, so no <c>"Authorization"</c> string literal
	/// stands anywhere. The anchor is the enumeration member. Another member (<c>ContentType</c>) never matches.
	/// </summary>
	[GeneratedRegex(
		@"(?:(?<w>" + _ENUMERATION_HEADER_WRITE + @")|" + _VARIABLE_ASSIGNMENT_OPENING + @")" + _HEADER_VALUE
		+ @"(?(w)|" + _LINE_WINDOW_FIVE + _ENUMERATION_HEADER_WRITE + _VARIABLE_READ_BACK + @")",
		RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture,
		matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex AuthorizationHeaderEnumWriteConstruction();

	private const string _HEADER_VALUE_NAME = "AuthenticationHeaderValue";

	private const string _HEADER_VALUE_TYPE =
		@"(?:" + _NAMESPACE_PREFIX + @"(?<construct>" + _HEADER_VALUE_NAME + @")(?!\w)|(?<construct>" + _QUOTE_OPENING + @")" + _NAMESPACE_PREFIX + _HEADER_VALUE_NAME + _QUOTE_CLOSING + @")";

	private const string _HEADER_VALUE_BRACKETED = @"\[" + _NAMESPACE_PREFIX + @"(?<construct>" + _HEADER_VALUE_NAME + @")\]";

	/// <summary>
	/// The scheme argument of the typed header's constructor: a quoted literal (captured with its delimiters, whose body the
	/// verdict reads), a variable the symbol table resolves, or any other expression, which declares no scheme group and leaves the verdict indeterminate.
	/// </summary>
	private const string _HEADER_SCHEME_ARGUMENT =
		@"(?:" + _ASSEMBLED_SCHEME_ARGUMENT + @"|(?<scheme>" + _STRING_LITERAL + @")|" + _RESOLVABLE_VARIABLE_OPENING + @"|" + _ARGUMENT + @")";

	private const string _HEADER_SCHEME_PAIR = _HEADER_SCHEME_ARGUMENT + @"\s*,\s*(?<credential>" + _ARGUMENT + @")";

	/// <summary>An array variable given to <c>-ArgumentList</c> cannot be split: the variable is the scheme capture, which never resolves, so the finding stays at Medium.</summary>
	private const string _HEADER_ARGUMENT_LIST_VALUE =
		@"(?:(?:@\(\s*)?" + _HEADER_SCHEME_PAIR + @"(?:\s*\))?|(?<scheme>" + _VARIABLE + @"))";

	/// <summary>
	/// PS-007. The typed header value constructed with a scheme: <c>New-Object T('Basic', $x)</c> in every creation form, <c>[T]::new</c>, the
	/// static <c>[T]::Parse</c> and <c>::TryParse</c> and the string cast, the last three reading the scheme from the string. The scheme is
	/// a literal or a variable, resolved by the verdict. The anchor is the type name, or the opening delimiter of a quoted name.
	/// </summary>
	[GeneratedRegex(
		@"(?:" + _NEW_OBJECT + @"(?:" + _GAP + @"(?:" + _PARAMETER_TYPE_NAME + _PARAMETER_VALUE_SEPARATOR + @")?" + _HEADER_VALUE_TYPE + @"(?:[ \t]*\(\s*|" + _GAP + @"(?:" + _PARAMETER_ARGUMENT_LIST + _PARAMETER_VALUE_SEPARATOR + @")?)" + _HEADER_ARGUMENT_LIST_VALUE
		+ @"|" + _GAP + _PARAMETER_ARGUMENT_LIST + _PARAMETER_VALUE_SEPARATOR + _HEADER_ARGUMENT_LIST_VALUE + _GAP + _PARAMETER_TYPE_NAME + _PARAMETER_VALUE_SEPARATOR + _HEADER_VALUE_TYPE + @")"
		+ @"|" + _HEADER_VALUE_BRACKETED + @"::new[ \t]*\(\s*" + _HEADER_SCHEME_PAIR + @"\s*\)"
		+ @"|" + _HEADER_VALUE_BRACKETED + @"::(?:Try)?Parse[ \t]*\([ \t]*" + _QUOTE_OPENING + @"(?<scheme>" + _SCHEME_WORD + @")[ \t]+(?:(?<credential>" + _TOKEN_FIRST_CHARACTER + @"[^""'\u2019\u201D\n]*)|(?<credential>" + _SUB_EXPRESSION + @")[^""'\u2019\u201D\n]*|" + _BARE_INTERPOLATION + @"[^""'\u2019\u201D\n]*)" + _QUOTE_CLOSING
		+ @"|" + _HEADER_VALUE_BRACKETED + @"[ \t]*" + _QUOTE_OPENING + @"(?<scheme>" + _SCHEME_WORD + @")[ \t]+(?:(?<credential>" + _TOKEN_FIRST_CHARACTER + @"[^""'\u2019\u201D\n]*)|(?<credential>" + _SUB_EXPRESSION + @")[^""'\u2019\u201D\n]*|" + _BARE_INTERPOLATION + @"[^""'\u2019\u201D\n]*)" + _QUOTE_CLOSING + @")",
		RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture,
		matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex AuthenticationHeaderValueSchemeConstruction();
}
