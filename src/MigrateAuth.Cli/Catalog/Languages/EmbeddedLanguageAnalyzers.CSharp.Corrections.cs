using System.Text.RegularExpressions;

namespace MigrateAuth.Cli.Catalog.Languages;

/// <summary>
/// Additional detection expressions of the C# catalog: the class CS-020, the <c>Rsapi</c> pair of CS-017 and the two tails of CS-019. Each is a set of
/// named constants here, appended to the expression of its class. The expressions of CS-017 and CS-019 stay in their own files; only what they append lives here.
/// </summary>
internal static partial class CSharpPatternExpressions
{
	/// <summary>
	/// The text of one call argument, read from its start to the first comma, closing parenthesis, slash or line break. It is the run that
	/// the second-signal tests of CS-020 scan lazily, so a word is looked for only inside the argument it belongs to.
	/// </summary>
	private const string _ARGUMENT_TEXT_RUN = @"[^,)/\r\n]*?";

	/// <summary>
	/// Where a name word may begin: at the start of the text, after a character that is neither a letter nor a digit, or at a change from a
	/// lower-case letter to an upper-case one.
	/// </summary>
	private const string _NAME_WORD_START = @"(?:(?<![A-Za-z0-9])|(?<=[a-z])(?=[A-Z]))";

	/// <summary>Where a name word may end: at the end of the text, before a character that is neither a letter nor a digit, or at a change from lower case to upper case.</summary>
	private const string _NAME_WORD_END = @"(?:(?![A-Za-z0-9])|(?<=[a-z])(?=[A-Z]))";

	/// <summary>
	/// A credential-named text: it contains <c>password</c>, <c>passwd</c> or <c>secret</c> in any case, or <c>pass</c>, <c>pwd</c> or <c>pw</c> as a whole word,
	/// so <c>userPass</c> and <c>adminPw</c> are credential-named and <c>bypassFlag</c>, <c>compass</c> and <c>passport</c> are not.
	/// </summary>
	private const string _CREDENTIAL_NAMED_TEXT =
		@"(?:(?i:password|passwd|secret)|" + _NAME_WORD_START + @"(?i:pass|pwd|pw)" + _NAME_WORD_END + @")";

	/// <summary>A user-named text: it contains <c>user</c>, <c>username</c>, <c>usr</c> or <c>login</c> as a whole word, in any case. The refusal of a text that ends in an identifier word is made after the argument is read.</summary>
	private const string _USER_NAMED_TEXT = _NAME_WORD_START + @"(?i:username|user|usr|login)" + _NAME_WORD_END;

	/// <summary>A token-named text: it contains <c>token</c> or <c>bearer</c> in any case, or <c>apikey</c> or <c>api_key</c> as a whole word.</summary>
	private const string _TOKEN_NAMED_TEXT =
		@"(?:(?i:token|bearer)|" + _NAME_WORD_START + @"(?i:apikey|api_key)" + _NAME_WORD_END + @")";

	/// <summary>The endings that stop a user-named argument, read from the end of its text: an identifier, a key or a unique identifier is not a user name.</summary>
	private const string _IDENTIFIER_WORD_ENDING = @"(?:Id|ID|Key|Guid)";

	/// <summary>An argument name and its colon, <c>userName:</c>, which is part of the text a name is read from and no part of the credential.</summary>
	private const string _ARGUMENT_NAME_LABEL = @"(?:" + _IDENTIFIER + _ARGUMENT_LIST_GAP + @":(?!:)" + _ARGUMENT_LIST_GAP + @")?";

	/// <summary>One dotted type name, which may begin with <c>global::</c>.</summary>
	private const string _DOTTED_TYPE_NAME = @"(?:global::)?" + _IDENTIFIER + @"(?:\." + _IDENTIFIER + @")*";

	/// <summary>
	/// The one type argument of the factory call: a dotted type name with at most one further level of angle brackets, <c>IExampleService&lt;Item&gt;</c>.
	/// A using alias is just a name; two type arguments, an array, a nullable and a second nesting are not read.
	/// </summary>
	private const string _PROXY_TYPE_ARGUMENT =
		_DOTTED_TYPE_NAME + @"(?:" + _ARGUMENT_LIST_GAP + @"<" + _ARGUMENT_LIST_GAP + _DOTTED_TYPE_NAME + _ARGUMENT_LIST_GAP + @">)?";

	/// <summary>
	/// CS-020 up to and including the opening parenthesis: the method name exactly <c>GetProxy</c> (anchored as <c>construct</c>), one type argument,
	/// and a line break, a line comment or a block comment allowed between any two tokens. The receiver is not read.
	/// </summary>
	private const string _CS_020_CALL_OPENING =
		@"\b(?<construct>GetProxy)" + _ARGUMENT_LIST_GAP + @"<" + _ARGUMENT_LIST_GAP + _PROXY_TYPE_ARGUMENT + _ARGUMENT_LIST_GAP + @">" + _ARGUMENT_LIST_GAP + @"\(" + _ARGUMENT_LIST_GAP;

	/// <summary>An argument of the shared argument form, optionally preceded by its name label.</summary>
	private const string _CS_020_ANY_ARGUMENT = _ARGUMENT_NAME_LABEL + @"(?:" + _ARGUMENT_EXPRESSION + @")";

	/// <summary>An ordinary quoted literal as an argument, optionally preceded by its name label.</summary>
	private const string _CS_020_QUOTED_ARGUMENT = _ARGUMENT_NAME_LABEL + _QUOTED_LITERAL_ARGUMENT;

	/// <summary>The comma between the two arguments.</summary>
	private const string _CS_020_ARGUMENT_SEPARATOR = _ARGUMENT_LIST_GAP + @"," + _ARGUMENT_LIST_GAP;

	/// <summary>A second argument that is not token-named: a token-named second argument stops the call whatever the first argument is.</summary>
	private const string _CS_020_SECOND_ARGUMENT_IS_NOT_TOKEN_NAMED = @"(?!" + _ARGUMENT_TEXT_RUN + _TOKEN_NAMED_TEXT + @")";

	/// <summary>The second argument, whose value without its label is the credential position.</summary>
	private const string _CS_020_SECOND_ARGUMENT = _ARGUMENT_NAME_LABEL + @"(?<credential>" + _ARGUMENT_EXPRESSION + @")";

	/// <summary>
	/// The second signal, in three ways. (a) The second argument is credential-named. (b) The first argument is
	/// user-named, and its text does not end in <c>Id</c>, <c>ID</c>, <c>Key</c> or <c>Guid</c>. (c) The second argument is an ordinary quoted
	/// literal and the first is a quoted literal. A literal second argument alone is not a signal, and every alternative refuses a token-named second argument.
	/// </summary>
	private const string _CS_020_SECOND_SIGNAL =
		@"(?:" + _CS_020_ANY_ARGUMENT + _CS_020_ARGUMENT_SEPARATOR + @"(?=" + _ARGUMENT_TEXT_RUN + _CREDENTIAL_NAMED_TEXT + @")" + _CS_020_SECOND_ARGUMENT_IS_NOT_TOKEN_NAMED + _CS_020_SECOND_ARGUMENT
		+ @"|(?=" + _ARGUMENT_TEXT_RUN + _USER_NAMED_TEXT + @")" + _CS_020_ANY_ARGUMENT + @"(?<!" + _IDENTIFIER_WORD_ENDING + @")" + _CS_020_ARGUMENT_SEPARATOR + _CS_020_SECOND_ARGUMENT_IS_NOT_TOKEN_NAMED + _CS_020_SECOND_ARGUMENT
		+ @"|" + _CS_020_QUOTED_ARGUMENT + _CS_020_ARGUMENT_SEPARATOR + _CS_020_SECOND_ARGUMENT_IS_NOT_TOKEN_NAMED + _ARGUMENT_NAME_LABEL + @"(?<credential>" + _QUOTED_LITERAL_ARGUMENT + @"))";

	/// <summary>
	/// CS-020. A call of the method <c>GetProxy</c> with one type argument and exactly two arguments, reported only when a second signal holds
	/// (<see cref="_CS_020_SECOND_SIGNAL"/>). The expression opens on the literal method name, which is rare, so no alternative is tried at a word that
	/// cannot begin it, and it holds no bounded loop over a group. The anchor is the method name and the credential position is the second argument's value.
	/// </summary>
	[GeneratedRegex(
		_CS_020_CALL_OPENING + _CS_020_SECOND_SIGNAL + _ARGUMENT_LIST_GAP + @"\)",
		RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture,
		matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex SdkProxyFactoryWithUserAndPasswordConstruction();

	/// <summary>
	/// A member name is not read when a letter, a digit or an underscore stands directly in front of it. It is a lookbehind placed after the literal <c>Rsapi</c> is consumed, and it reads
	/// that literal back, so the expression opens on the rare literal and the lookbehind runs only where the literal stands.
	/// </summary>
	private const string _RSAPI_NAME_NOT_CONTINUED_BACKWARDS = @"(?<![A-Za-z0-9_]Rsapi)";

	/// <summary>
	/// A member name is not read when it follows a type and blanks on the same line: a name that follows an identifier character, <c>&gt;</c>, <c>]</c> or <c>?</c> and one or more
	/// spaces or tabs is a property or field declaration. The lookbehind reads one line only, so a declaration written over two lines is read (a declared cost).
	/// </summary>
	private const string _RSAPI_NAME_NOT_A_DECLARATION = @"(?<![A-Za-z0-9_>\]?][ \t]+Rsapi)";

	/// <summary>The literal <c>Rsapi</c> and the two lookbehinds that decide whether a name starts here.</summary>
	private const string _RSAPI_NAME_START = @"Rsapi" + _RSAPI_NAME_NOT_CONTINUED_BACKWARDS + _RSAPI_NAME_NOT_A_DECLARATION;

	/// <summary>
	/// The user-name half: <c>Rsapi&lt;x&gt;UserName</c> or <c>Rsapi&lt;x&gt;Username</c> (anchored whole as <c>construct</c>), where <c>&lt;x&gt;</c> is a run of letters, digits and underscores that may be
	/// empty and is captured as <c>x</c>, then its assignment or initializer entry with the value in the shared argument form.
	/// </summary>
	private const string _RSAPI_USER_NAME_HALF =
		@"(?<construct>" + _RSAPI_NAME_START + @"(?<x>[A-Za-z0-9_]*)(?:UserName|Username))\s*=\s*(?<user>" + _ARGUMENT_EXPRESSION + @")";

	/// <summary>
	/// A password entry is not read as the second of two assignments when it is an entry of an initializer: a name that a brace or a comma, and any blanks, stand in front of is an entry of an
	/// initializer, and one half in an initializer with the other assigned is the mixed form, which is not read.
	/// </summary>
	private const string _RSAPI_NAME_NOT_AN_INITIALIZER_ENTRY = @"(?<![{,]\s*Rsapi)";

	/// <summary>
	/// The rest of the password half, after the literal <c>Rsapi</c> and its lookbehinds: the same <c>&lt;x&gt;</c> as the user-name half, then the value in the shared argument form as the credential, which
	/// must end where an assignment or an initializer entry ends, so a value that goes on as a calculation or a conditional is not read.
	/// </summary>
	private const string _RSAPI_PASSWORD_HALF_AFTER_ITS_NAME_START =
		@"\k<x>Password\s*=\s*(?<credential>" + _ARGUMENT_EXPRESSION + @")(?=\s*(?:[;,}]|\z))";

	/// <summary>The longest run of characters the password half of one initializer may stand from the user-name half, as regular-expression text.</summary>
	private const string _RSAPI_INITIALIZER_MAXIMUM_DISTANCE = "600";

	/// <summary>
	/// The pair of the Relativity API settings, in two forms. Two assignments: the user-name assignment ends in a semicolon and the password assignment follows within the five-line
	/// window <c>CS-017</c> has for its first pair (<see cref="_FIVE_LINE_WINDOW"/>). One initializer: the user-name entry ends in a comma, and the password entry follows within 600
	/// characters, with no brace and no statement end between them, so other members may stand between and a nested brace or a later statement ends the read. Both halves are required, with the same
	/// <c>&lt;x&gt;</c>, the user name first. The window and the distance are required, never inside an optional group.
	/// </summary>
	private const string _RSAPI_SETTINGS_PAIR =
		_RSAPI_USER_NAME_HALF
		+ @"(?:\s*;" + _FIVE_LINE_WINDOW + _RSAPI_NAME_START + _RSAPI_NAME_NOT_AN_INITIALIZER_ENTRY
		+ @"|\s*,[^{};]{0," + _RSAPI_INITIALIZER_MAXIMUM_DISTANCE + @"}?" + _RSAPI_NAME_START + @")"
		+ _RSAPI_PASSWORD_HALF_AFTER_ITS_NAME_START;

	/// <summary>The most line breaks the tempered window of the member-bound credential may cross, as regular-expression text.</summary>
	private const string _WINDOW_D4_MAXIMUM_LINE_BREAKS = "30";

	/// <summary>The most characters a line of the tempered window may hold, line break excluded, as regular-expression text.</summary>
	private const string _WINDOW_D4_MAXIMUM_LINE_LENGTH = "400";

	/// <summary>
	/// The tempered window between two links of the member-bound credential: up to 30 whole lines, each of at most 400 characters and none holding the text <c>NetworkCredential</c>, so the
	/// chain of one credential cannot cross the store of another, then the part of the line on which the next link begins, again of at most 400 characters. The zero-width point
	/// (<see cref="_BACKTRACK_POINT"/>) sits in the body of the lazy loop over the group of lines; the single-class tails carry none, as in <see cref="_FIVE_LINE_WINDOW"/>. The window is required wherever it is used, never inside an optional group.
	/// </summary>
	private const string _WINDOW_D4 =
		@"(?:(?:(?![^\n]{0," + _WINDOW_D4_MAXIMUM_LINE_LENGTH + @"}NetworkCredential)[^\n]{0," + _WINDOW_D4_MAXIMUM_LINE_LENGTH + @"}\n)" + _BACKTRACK_POINT + @"){0," + _WINDOW_D4_MAXIMUM_LINE_BREAKS + @"}?[^\n]{0," + _WINDOW_D4_MAXIMUM_LINE_LENGTH + @"}?";

	/// <summary>A dotted name of at least two names, <c>settings.MailCredential</c>, <c>this.settings.MailCredential</c>.</summary>
	private const string _DOTTED_MEMBER_NAME = _IDENTIFIER + @"(?:\." + _IDENTIFIER + @")+";

	/// <summary>The member must be matched whole where it is bound: no identifier character and no dot may continue it, so <c>settings.MailCredential.Inner</c> is not the same name.</summary>
	private const string _MEMBER_NAME_ENDS_HERE = @"(?![A-Za-z0-9_.])";

	/// <summary>
	/// The first link of the member-bound credential: <c>&lt;member&gt; = new NetworkCredential(user, password);</c>. It opens on the equals sign, after a forward guard on the creation of the
	/// credential type, which is rare, and only then reads the member's name backwards in a lookbehind, captured as <c>m</c> and read back at the binding. The member is a dotted name of at least two
	/// names that no identifier character or dot precedes, and the credential type is written plain or qualified.
	/// </summary>
	private const string _MEMBER_STORED_CREDENTIAL =
		@"=(?=\s*" + _NETWORK_CREDENTIAL_CREATION + @")(?<=(?<![A-Za-z0-9_.])(?<m>" + _DOTTED_MEMBER_NAME + @")\s*=)\s*" + _NETWORK_CREDENTIAL_CREATION + _CREDENTIAL_ARGUMENT_PAIR + @"\s*;";

	/// <summary>The member bound in the object initializer of a sink: <c>{ ... Credentials = &lt;member&gt; ... }</c>, the member being the one stored at the first link.</summary>
	private const string _MEMBER_BOUND_IN_AN_INITIALIZER = @"\s*\{[^{}]*?\bCredentials\s*=\s*\k<m>" + _MEMBER_NAME_ENDS_HERE;

	/// <summary>
	/// Tail A, the sink created in the binding statement (<c>SmtpClient</c> and <c>WebProxy</c> only): <c>new SmtpClient(host, port) { Credentials = &lt;member&gt; }</c>, with or without constructor
	/// parentheses, or the target-typed <c>SmtpClient mailer = new(host, port) { Credentials = &lt;member&gt; }</c>. The sink type name is the anchor.
	/// </summary>
	private const string _MEMBER_BOUND_TAIL_SINK_IN_THE_BINDING_STATEMENT =
		@"(?:\bnew\s+" + _MAIL_OR_PROXY_SINK_CREATION + @"\b(?:\s*" + _CONSTRUCTOR_ARGUMENT_TEXT + @")?"
		+ @"|\b(?<construct>" + _MAIL_OR_PROXY_SINK_TYPE_NAMES + @")" + _TARGET_TYPED_CREATION_SUFFIX + @"\s*" + _CONSTRUCTOR_ARGUMENT_TEXT + @")"
		+ _MEMBER_BOUND_IN_AN_INITIALIZER;

	/// <summary>
	/// The factory call that obtains a file-transfer request, with its argument list read as one balanced group so that a declaration in a <c>using (...)</c> header ends where the header does:
	/// <c>Create</c> called through <c>WebRequest</c> or <c>FtpWebRequest</c>, either written plain or qualified.
	/// </summary>
	private const string _FILE_TRANSFER_REQUEST_FACTORY_CALL_WITH_BALANCED_ARGUMENTS =
		_TYPE_QUALIFIER + @"(?:Ftp)?WebRequest\.Create\s*" + _CONSTRUCTOR_ARGUMENT_TEXT;

	/// <summary>The end of the statement that declares the receiver: a semicolon, or, in a <c>using (...)</c> header, the closing parenthesis and the opening brace.</summary>
	private const string _RECEIVER_DECLARATION_END = @"\s*(?:;|\)\s*\{)";

	/// <summary>
	/// The declaration of the receiver of tail B, read between the two links: <c>var mailer = new SmtpClient(...)</c> and <c>SmtpClient mailer = new SmtpClient(...)</c> (the variable read backwards from the
	/// equals sign, captured as <c>v</c>), <c>SmtpClient mailer = new(...)</c>, and for a file-transfer request <c>var request = (FtpWebRequest)WebRequest.Create(...)</c>, written with a cast, with
	/// <c>as FtpWebRequest</c> or with the factory called through <c>FtpWebRequest</c>. The sink type name is the anchor.
	/// </summary>
	private const string _RECEIVER_DECLARATION =
		@"(?:=(?<=\b(?<v>" + _IDENTIFIER + @")\s*=)\s*(?:new\s+" + _MAIL_OR_PROXY_SINK_CREATION + @"\b\s*" + _CONSTRUCTOR_ARGUMENT_TEXT
		+ @"|\(\s*" + _TYPE_QUALIFIER + @"(?<construct>FtpWebRequest)\s*\)\s*" + _FILE_TRANSFER_REQUEST_FACTORY_CALL_WITH_BALANCED_ARGUMENTS
		+ @"|" + _FILE_TRANSFER_REQUEST_FACTORY_CALL_WITH_BALANCED_ARGUMENTS + @"\s*as\s+" + _TYPE_QUALIFIER + @"(?<construct>FtpWebRequest))"
		+ @"|\b(?<construct>" + _MAIL_OR_PROXY_SINK_TYPE_NAMES + @")\s+(?<v>" + _IDENTIFIER + @")\s*=\s*new\s*" + _CONSTRUCTOR_ARGUMENT_TEXT + @")" + _RECEIVER_DECLARATION_END;

	/// <summary>The binding through the receiver of tail B: <c>mailer.Credentials = &lt;member&gt;</c>, the receiver being the variable declared between the links and no other.</summary>
	private const string _MEMBER_BOUND_TO_THE_RECEIVER =
		@"(?<![A-Za-z0-9_.])\k<v>\.Credentials\s*=\s*\k<m>" + _MEMBER_NAME_ENDS_HERE;

	/// <summary>
	/// Tail B, the sink in the receiver's declaration (all three sinks): a receiver variable is declared between the first link and the binding, within the tempered window of each, and that same
	/// variable is bound to the member. <c>FtpWebRequest</c> has no public constructor and is read only here.
	/// </summary>
	private const string _MEMBER_BOUND_TAIL_RECEIVER_DECLARED_BETWEEN_THE_LINKS =
		_RECEIVER_DECLARATION + _WINDOW_D4 + _MEMBER_BOUND_TO_THE_RECEIVER;

	/// <summary>
	/// The two tails of <c>CS-019</c> for a credential stored in a member and bound to a non-HTTP sink through that member: the first link, the tempered window, and then tail A or tail B. The span of a
	/// match contains the <c>new NetworkCredential(...)</c> text of the first link, so the existing triple of <c>CS-019</c> over <c>CS-008</c> at overlapping spans removes the <c>CS-008</c> finding of that
	/// credential. The finding is <c>CS-019</c> at Low, anchored on the sink type name. The same first link bound to an HTTP handler matches neither tail.
	/// </summary>
	private const string _MEMBER_BOUND_CREDENTIAL_TAILS =
		_MEMBER_STORED_CREDENTIAL + _WINDOW_D4 + @"(?:" + _MEMBER_BOUND_TAIL_SINK_IN_THE_BINDING_STATEMENT + @"|" + _MEMBER_BOUND_TAIL_RECEIVER_DECLARED_BETWEEN_THE_LINKS + @")";
}
