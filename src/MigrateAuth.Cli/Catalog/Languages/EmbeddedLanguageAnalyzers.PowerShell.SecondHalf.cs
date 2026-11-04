using System.Text.RegularExpressions;

namespace MigrateAuth.Cli.Catalog.Languages;

/// <summary>
/// The second half of the `PowerShell` expressions: the credential cache, the credential on an HTTP type, the two Base64 classes, the
/// three Low classes, the constant declaration, the first-line detection and the content markers of the demotion signals. The shared
/// vocabulary they read is in <c>EmbeddedLanguageAnalyzers.PowerShell.Expressions.cs</c>.
/// </summary>
internal static partial class PowerShellPatternExpressions
{
	// ------------------------------------------------------------------------------------------
	// The credential cache
	// ------------------------------------------------------------------------------------------

	/// <summary>The scheme of a cache entry: an assembled scheme (no scheme group, so the verdict is indeterminate), a quoted literal the verdict reads, or a variable the symbol table resolves.</summary>
	private const string _CACHE_SCHEME_ARGUMENT = @"(?:" + _ASSEMBLED_SCHEME_ARGUMENT + @"|(?<scheme>" + _STRING_LITERAL + @")|" + _RESOLVABLE_VARIABLE_OPENING + @")";

	/// <summary>
	/// PS-008. <c>.Add(address, 'Basic', credential)</c> and the four-argument overload <c>.Add(host, port, 'Basic', credential)</c>. The
	/// scheme is an argument, and the third argument is a credential by the call's own signature, so the credential is accepted as
	/// a construction, a <c>.GetNetworkCredential()</c> call or a variable.
	/// An <c>Add</c> of two arguments is another collection and does not match. The anchor is the <c>Add</c> token.
	/// </summary>
	[GeneratedRegex(
		@"\.(?<construct>Add)[ \t]*\([ \t\n]*" + _ARGUMENT + @"[ \t\n]*,[ \t\n]*(?:" + _ARGUMENT + @"[ \t\n]*,[ \t\n]*)?" + _CACHE_SCHEME_ARGUMENT + @"[ \t\n]*,[ \t\n]*(?:" + _CREDENTIAL_ASSIGNED + @"|(?<credential>" + _ARGUMENT + @"))[ \t\n]*\)",
		RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture,
		matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex CredentialCacheBasicSchemeConstruction();

	// ------------------------------------------------------------------------------------------
	// The credential on an HTTP type
	// ------------------------------------------------------------------------------------------

	private const string _HTTP_RECEIVER_NAMES = @"WebClient|HttpClientHandler|SocketsHttpHandler|WinHttpHandler|HttpWebRequest";

	private const string _HTTP_RECEIVER_TYPE =
		@"(?:" + _NAMESPACE_PREFIX + @"(?:" + _HTTP_RECEIVER_NAMES + @")(?!\w)|" + _QUOTE_OPENING + _NAMESPACE_PREFIX + @"(?:" + _HTTP_RECEIVER_NAMES + @")" + _QUOTE_CLOSING + @")";

	/// <summary>
	/// The receiver of the class, created: by <c>New-Object</c> or <c>::new()</c>, or by the request factories of the base library, which
	/// both editions offer for the request type (it has a public constructor on the older edition only). A request cast to the
	/// request type still ends in the factory call.
	/// </summary>
	private const string _HTTP_RECEIVER_CREATION =
		@"(?:" + _NEW_OBJECT + _GAP + @"(?:" + _PARAMETER_TYPE_NAME + _PARAMETER_VALUE_SEPARATOR + @")?" + _HTTP_RECEIVER_TYPE
		+ @"|\[" + _NAMESPACE_PREFIX + @"(?:" + _HTTP_RECEIVER_NAMES + @")\]::new"
		+ @"|\[" + _NAMESPACE_PREFIX + @"(?:WebRequest|HttpWebRequest)\]::Create(?:Http)?[ \t]*\()";

	/// <summary>
	/// The receiver given its credentials in its initializer: the hashtable cast, or <c>-Property @{ … }</c>. A type name is tried only where a creation lets one
	/// stand: directly after <c>New-Object</c> and its gap, after <c>-TypeName</c> wherever that parameter stands in the 200 characters after it, or inside brackets.
	/// </summary>
	private const string _HTTP_RECEIVER_INITIALIZER =
		@"(?:\[" + _NAMESPACE_PREFIX + @"(?:" + _HTTP_RECEIVER_NAMES + @")\][ \t]*|" + _NEW_OBJECT
		+ @"(?:" + _GAP + @"(?:" + _PARAMETER_TYPE_NAME + _PARAMETER_VALUE_SEPARATOR + @")?" + _HTTP_RECEIVER_TYPE + @"|[^\n]{0,200}?" + _PARAMETER_TYPE_NAME + _PARAMETER_VALUE_SEPARATOR + _HTTP_RECEIVER_TYPE + @")"
		+ @"[^\n]{0,200}?" + _PARAMETER_PROPERTY + _PARAMETER_VALUE_SEPARATOR + @")@\{";

	/// <summary>The hashtable keys before the one that is wanted: any text, bounded, because a key is only ever bare inside a table.</summary>
	private const string _HASHTABLE_WINDOW = @"[\s\S]{0,600}?";

	private const string _CREDENTIALS_ASSIGNMENT_TAIL = @"(?<construct>Credentials)(?![\w])[ \t]*=(?!=)[ \t]*";

	/// <summary>
	/// THE ASSIGNMENT OF A CREDENTIAL TO A MEMBER, the rare conjunct of a receiver and its assignment: <c>Credentials = </c> with its credential value, read as one group
	/// so that the receiver can be looked for behind it once the value has been accepted, which is the only moment it is worth the search.
	/// THE VALUE IS ATOMIC. The receiver look-behind that follows reads only the text before the member name and the value's own text read back, so
	/// whether it holds is the same for every extent the value could take: the first extent the value finds either lets the look-behind hold, and the match is that
	/// one, or no extent does. An atomic group keeps the first extent and its captures and gives up the others, which could never have been the answer; it spares the
	/// look-behind from being run again, window and all, for each of them.
	/// </summary>
	private const string _CREDENTIALS_ASSIGNED_WITH_A_VALUE = _CREDENTIALS_ASSIGNMENT_TAIL + @"(?<value>(?>" + _CREDENTIAL_ASSIGNED + @"))";

	/// <summary>The text of the assignment, as the receiver look-behind ends on it: the member, the sign and the value group read again.</summary>
	private const string _CREDENTIALS_ASSIGNMENT_READ_BACK = @"Credentials[ \t]*=[ \t]*(?-i:\k<value>)";

	/// <summary>
	/// The HTTP receiver read back from its assigned credential: created within the eight lines before the member, or the assignment standing in its
	/// initializer. The look-behind ends on the assignment's own text, so it starts at the assignment and goes back, once, when the value has been accepted.
	/// </summary>
	private const string _HTTP_RECEIVER_BEHIND_THE_ASSIGNMENT =
		@"(?:(?<=" + _HTTP_RECEIVER_CREATION + _LINE_WINDOW_EIGHT + @"\." + _CREDENTIALS_ASSIGNMENT_READ_BACK + @")"
		+ @"|(?<=" + _HTTP_RECEIVER_INITIALIZER + _HASHTABLE_WINDOW + @"(?<![\w\-.$])" + _CREDENTIALS_ASSIGNMENT_READ_BACK + @"))";

	/// <summary>
	/// PS-009. A credential construction assigned to the <c>Credentials</c> of a receiver on the allow-list of HTTP types: a web client, a request
	/// made by the base library's factories, or a handler. The receiver is created within the eight lines before the assignment, a
	/// required window, or the assignment sits in the receiver's initializer. The credential must be present in the assignment (a
	/// construction, a <c>.GetNetworkCredential()</c> call or the cast of a credential variable): the default credentials and
	/// <c>UseDefaultCredentials</c> are integrated Windows authentication and never match. The anchor is the member name.
	/// THE SHAPE: the expression opens at the assignment, the rare conjunct, accepts its credential value, and only then looks back for the receiver.
	/// </summary>
	[GeneratedRegex(
		_CREDENTIALS_ASSIGNED_WITH_A_VALUE + _HTTP_RECEIVER_BEHIND_THE_ASSIGNMENT,
		RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture,
		matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex NetworkCredentialOnHttpTypeConstruction();

	// ------------------------------------------------------------------------------------------
	// The Low classes
	// ------------------------------------------------------------------------------------------

	private const string _WEB_SESSION_NAME = "WebRequestSession";

	private const string _WEB_SESSION_TYPE =
		@"(?:" + _NAMESPACE_PREFIX + _WEB_SESSION_NAME + @"(?!\w)|" + _QUOTE_OPENING + _NAMESPACE_PREFIX + _WEB_SESSION_NAME + _QUOTE_CLOSING + @")";

	/// <summary>
	/// A web request session created in place, or named by <c>-SessionVariable</c> on a web cmdlet. The second form opens at the parameter that decides it and
	/// reads the web cmdlet back over the logical line of the command, bracket continuation included.
	/// </summary>
	private const string _WEB_SESSION_CREATION =
		@"(?:" + _NEW_OBJECT + _GAP + @"(?:" + _PARAMETER_TYPE_NAME + _PARAMETER_VALUE_SEPARATOR + @")?" + _WEB_SESSION_TYPE
		+ @"|\[" + _NAMESPACE_PREFIX + _WEB_SESSION_NAME + @"\]::new"
		+ @"|" + _PARAMETER_START + @"(?=" + _SESSION_VARIABLE_NAME_PREFIXES + _PARAMETER_END + @")(?<=" + _WEB_CMDLET + _LOGICAL_LINE_WINDOW + _DASH + @")" + _SESSION_VARIABLE_NAME_PREFIXES + _PARAMETER_END + @")";

	private const string _WEB_SESSION_INITIALIZER =
		@"(?:\[" + _NAMESPACE_PREFIX + _WEB_SESSION_NAME + @"\][ \t]*|" + _NEW_OBJECT
		+ @"(?:" + _GAP + @"(?:" + _PARAMETER_TYPE_NAME + _PARAMETER_VALUE_SEPARATOR + @")?" + _WEB_SESSION_TYPE + @"|[^\n]{0,200}?" + _PARAMETER_TYPE_NAME + _PARAMETER_VALUE_SEPARATOR + _WEB_SESSION_TYPE + @")"
		+ @"[^\n]{0,200}?" + _PARAMETER_PROPERTY + _PARAMETER_VALUE_SEPARATOR + @")@\{";

	/// <summary>Any value but the default credentials: integrated Windows authentication, the opposite of Basic.</summary>
	private const string _NOT_THE_DEFAULT_CREDENTIALS = @"(?!\[(?:[A-Za-z_]\w*\.){0,6}CredentialCache\]::DefaultCredentials)(?=\S)";

	private const string _NEW_WEB_SERVICE_PROXY = _COMMAND_START + @"New-WebServiceProxy(?![\w\-])";

	/// <summary>A command that carries <c>-Authentication</c> anywhere on its logical line, bracket continuation included, is the explicit class's, not this one's.</summary>
	private const string _AUTHENTICATION_LATER_ON_THE_LINE = @"(?!" + _LOGICAL_LINE_FORWARD_WINDOW + _PARAMETER_AUTHENTICATION + @")";

	private const string _CREDENTIAL_KEY =
		@"(?:(?<![\w\-.$])(?<construct>" + _CREDENTIAL_NAME_PREFIXES + @")(?![\w\-])|(?<construct>" + _QUOTE_OPENING + @")" + _CREDENTIAL_NAME_PREFIXES + _QUOTE_CLOSING + @")";

	/// <summary>The command name of the class read back from a parameter's dash over the logical line, which refuses to cross <c>-Authentication</c>.</summary>
	private const string _WEB_COMMAND_BEHIND_A_DASH_BEFORE_AUTHENTICATION =
		@"(?<=(?:" + _WEB_CMDLET + @"|" + _NEW_WEB_SERVICE_PROXY + @")" + _LOGICAL_LINE_WINDOW_BEFORE_AUTHENTICATION + _DASH + @")";

	private const string _WEB_CMDLET_OR_WEB_SERVICE_PROXY = @"(?:" + _WEB_CMDLET + @"|" + _NEW_WEB_SERVICE_PROXY + @")";

	/// <summary>
	/// PS-012. A credential on a web cmdlet without <c>-Authentication</c>: <c>-Credential</c> under every prefix from <c>-Cr</c>, the colon syntax, a
	/// backtick continuation, the key of a splatted hashtable; and the <c>Credentials</c> assignment on a web request session, with
	/// any value but the default credentials. Low, for manual review: as written, the call does not send Basic to RelativityOne. A
	/// command that also carries <c>-Authentication</c> is not matched. The anchor is the parameter token, the key, or the member name.
	/// THE SHAPE: the command form opens at the parameter that decides the class and reads the command name back; the splat form opens at the splat and reads the
	/// table and its key back; the session form opens at the session's creation, which is the rarer token, and looks forward for the assignment.
	/// </summary>
	[GeneratedRegex(
		@"(?:(?<construct>" + _PARAMETER_START + @"(?=" + _CREDENTIAL_NAME_PREFIXES + _PARAMETER_END + @")" + _WEB_COMMAND_BEHIND_A_DASH_BEFORE_AUTHENTICATION + _CREDENTIAL_NAME_PREFIXES + _PARAMETER_END + @")" + _AUTHENTICATION_LATER_ON_THE_LINE + _REST_OF_THE_COMMAND
		+ @"|" + _SPLAT_OF_A_VARIABLE + @"(?<=" + _SPLAT_TABLE_OPENING_OF_THE_SPLATTED_VARIABLE + @"[\s\S]{0,800}?" + _CREDENTIAL_KEY + @"[ \t]*=[ \t]*\S" + _LINE_WINDOW_TWENTY + _WEB_CMDLET_OR_WEB_SERVICE_PROXY + _SPLAT_USE_BEHIND + @")" + _REST_OF_THE_COMMAND
		+ @"|(?:" + _WEB_SESSION_CREATION + _LINE_WINDOW_EIGHT + @"\.|" + _WEB_SESSION_INITIALIZER + _HASHTABLE_WINDOW + @"(?<![\w\-.$]))" + _CREDENTIALS_ASSIGNMENT_TAIL + _NOT_THE_DEFAULT_CREDENTIALS + @")",
		RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture,
		matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex CredentialOnWebCmdletWithoutSchemeConstruction();

	/// <summary>The non-HTTP credential consumers of the closed set: remoting, drive, process, job, directory-session and mail cmdlets.</summary>
	private const string _NON_HTTP_SINK_CMDLET =
		@"(?:Invoke-Command|Enter-PSSession|New-PSSession|New-PSDrive|Start-Process|Start-Job|New-CimSession|Get-CimInstance|Get-WmiObject|Invoke-WmiMethod|Send-MailMessage)";

	private const string _NON_HTTP_RECEIVER_NAMES = @"SmtpClient|WebProxy";

	private const string _NON_HTTP_RECEIVER_TYPE =
		@"(?:" + _NAMESPACE_PREFIX + @"(?:" + _NON_HTTP_RECEIVER_NAMES + @")(?!\w)|" + _QUOTE_OPENING + _NAMESPACE_PREFIX + @"(?:" + _NON_HTTP_RECEIVER_NAMES + @")" + _QUOTE_CLOSING + @")";

	/// <summary>The mail client and the proxy, created; and the file-transfer request, made by the request factory and cast to its own type.</summary>
	private const string _NON_HTTP_RECEIVER_CREATION =
		@"(?:" + _NEW_OBJECT + _GAP + @"(?:" + _PARAMETER_TYPE_NAME + _PARAMETER_VALUE_SEPARATOR + @")?" + _NON_HTTP_RECEIVER_TYPE
		+ @"|\[" + _NAMESPACE_PREFIX + @"(?:" + _NON_HTTP_RECEIVER_NAMES + @")\]::new"
		+ @"|\[" + _NAMESPACE_PREFIX + @"FtpWebRequest\][ \t]*\[" + _NAMESPACE_PREFIX + @"WebRequest\]::Create(?:Http)?[ \t]*\()";

	private const string _NON_HTTP_RECEIVER_INITIALIZER =
		@"(?:\[" + _NAMESPACE_PREFIX + @"(?:" + _NON_HTTP_RECEIVER_NAMES + @")\][ \t]*|" + _NEW_OBJECT
		+ @"(?:" + _GAP + @"(?:" + _PARAMETER_TYPE_NAME + _PARAMETER_VALUE_SEPARATOR + @")?" + _NON_HTTP_RECEIVER_TYPE + @"|[^\n]{0,200}?" + _PARAMETER_TYPE_NAME + _PARAMETER_VALUE_SEPARATOR + _NON_HTTP_RECEIVER_TYPE + @")"
		+ @"[^\n]{0,200}?" + _PARAMETER_PROPERTY + _PARAMETER_VALUE_SEPARATOR + @")@\{";

	/// <summary>The non-HTTP receiver read back from its assigned credential, as <see cref="_HTTP_RECEIVER_BEHIND_THE_ASSIGNMENT"/> does for the HTTP one.</summary>
	private const string _NON_HTTP_RECEIVER_BEHIND_THE_ASSIGNMENT =
		@"(?:(?<=" + _NON_HTTP_RECEIVER_CREATION + _LINE_WINDOW_EIGHT + @"\." + _CREDENTIALS_ASSIGNMENT_READ_BACK + @")"
		+ @"|(?<=" + _NON_HTTP_RECEIVER_INITIALIZER + _HASHTABLE_WINDOW + @"(?<![\w\-.$])" + _CREDENTIALS_ASSIGNMENT_READ_BACK + @"))";

	private const string _DIRECTORY_ENTRY_NAME = "DirectoryEntry";

	private const string _DIRECTORY_ENTRY_TYPE =
		@"(?:" + _NAMESPACE_PREFIX + @"(?<construct>" + _DIRECTORY_ENTRY_NAME + @")(?!\w)|(?<construct>" + _QUOTE_OPENING + @")" + _NAMESPACE_PREFIX + _DIRECTORY_ENTRY_NAME + _QUOTE_CLOSING + @")";

	/// <summary>A directory entry takes its credential as text: a path, a user name and a password. The directory searcher takes none of its own and is reached through this entry.</summary>
	private const string _DIRECTORY_ENTRY_TRIPLE =
		_CODE_ARGUMENT + @"\s*,\s*" + _CODE_ARGUMENT + @"\s*,\s*" + _PASSWORD_ARGUMENT;

	private const string _DIRECTORY_ENTRY_CREATION =
		_CREATION_HEAD_OPENING + _DIRECTORY_ENTRY_TYPE + _CREATION_HEAD_AFTER_TYPE
		+ @"|" + _GAP + _PARAMETER_ARGUMENT_LIST + _PARAMETER_VALUE_SEPARATOR + @"(?=" + _COMMAND_WINDOW + _PARAMETER_TYPE_NAME + _PARAMETER_VALUE_SEPARATOR + _DIRECTORY_ENTRY_TYPE + @"))"
		+ @"|\[" + _NAMESPACE_PREFIX + @"(?<construct>" + _DIRECTORY_ENTRY_NAME + @")\]::new[ \t]*\(\s*)(?:@\(\s*)?" + _DIRECTORY_ENTRY_TRIPLE;

	/// <summary>
	/// PS-013. The credential consumers that authenticate to Windows or to a mail server and not to RelativityOne over HTTP: the cmdlets of the closed
	/// set that carry <c>-Credential</c>, and the .NET sinks (a mail client, a proxy, a file-transfer request, a directory entry) given a credential in
	/// place. A near-miss class, Low, outside the recall denominator. The anchor is the sink cmdlet, the sink's <c>Credentials</c> member, or the directory
	/// entry's type.
	/// THE SHAPE: the cmdlet form keeps its opening at the cmdlet, which is its anchor (an anchor outside the match costs the engine a second, interpreted reading of
	/// every match) and reads the logical line forward, brackets included; the receiver form opens at the assignment, accepts its credential value and only then
	/// looks back for the receiver.
	/// </summary>
	[GeneratedRegex(
		@"(?:" + _COMMAND_START + @"(?<construct>" + _NON_HTTP_SINK_CMDLET + @")(?![\w\-])" + _LOGICAL_LINE_FORWARD_WINDOW + _PARAMETER_CREDENTIAL
		+ @"|" + _CREDENTIALS_ASSIGNED_WITH_A_VALUE + _NON_HTTP_RECEIVER_BEHIND_THE_ASSIGNMENT
		+ @"|" + _DIRECTORY_ENTRY_CREATION + @")",
		RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture,
		matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex NonHttpCredentialSinkConstruction();

	/// <summary>
	/// PS-014. The definition of the script's own authentication helper: a function or filter whose name carries the scheme word (not followed by
	/// a lower-case letter) and a role word. A definition and not a call site, so its call sites will not appear as findings. The anchor is the
	/// function's name.
	/// </summary>
	[GeneratedRegex(
		_COMMAND_START + @"(?:function|filter)[ \t]+(?:(?:global|script|local|private):)?(?=[\w\-]*Basic(?-i:(?![a-z])))(?=[\w\-]*(?:Auth|Header|Cred))(?<declaration>[A-Za-z_][\w\-]*)(?![\w\-])",
		RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture,
		matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex AuthenticationHelperDefinition();

	// ------------------------------------------------------------------------------------------
	// Constant declaration, first line and content markers
	// ------------------------------------------------------------------------------------------

	/// <summary>
	/// The constant declaration that feeds the file-scoped symbol table. The name group holds the BARE name: the dollar sign, the braces and a scope
	/// qualifier stand outside it, so <c>$x</c>, <c>${x}</c> and <c>$script:x</c> read one key. A value is a quoted literal with no interpolation, at the
	/// end of its statement. A declaration after a comma or an opening parenthesis is a parameter default, which a caller can override: it is not read.
	/// </summary>
	[GeneratedRegex(
		@"(?<![\w$.:])(?<![,(][ \t\n]*(?:\[[^\]\n]{1,80}\][ \t]*)*)\$(?:\{[ \t]*(?:" + _SCOPE + @")?(?<name>[^\s{}:$'""`]{1,80})[ \t]*\}|(?:" + _SCOPE + @")?(?<name>" + _NAME + @"))[ \t]*=[ \t]*(?:'(?<value>[^'\n]*)'(?!')|""(?<value>[^""$`\n]*)""(?!""))(?=[ \t]*(?:[;\n}]|#|\z))",
		RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture,
		matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex ConstantDeclaration();

	/// <summary>
	/// The first-line detection of an extensionless script: a shebang that names <c>pwsh</c> or <c>powershell</c>, directly or through <c>env</c> with
	/// its flags. Case-sensitive, as the shared rule is, and it carries the lexical budget as its timeout.
	/// </summary>
	[GeneratedRegex(
		@"^#![ \t]*(?:[^\s/]*/)*(?:env[ \t]+(?:-\S*[ \t]+)*)?(?:pwsh|powershell)(?:[ \t]|$)",
		RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture,
		matchTimeoutMilliseconds: LexicalEnginePolicy.PROVISIONAL_LEXICAL_BUDGET_IN_MILLISECONDS)]
	public static partial Regex ShebangFirstLine();

	/// <summary>
	/// The generated-script header content marker, <c>AnyZone</c> because the header is itself a comment: <c>auto-generated</c> or <c>generated by</c>
	/// within the first ten lines, in a line comment or in a block comment that opens the file.
	/// </summary>
	[GeneratedRegex(
		@"\A(?:(?:[^\n]*\n){0,9}[ \t]*#[^\n]*?(?:auto[\- ]?generated|generated[ \t]+by)|[ \t\n]*<#(?:(?:[^#]|#(?!>))" + _BACKTRACK_POINT + @"){0,300}?(?:auto[\- ]?generated|generated[ \t]+by))",
		RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture,
		matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex GeneratedHeaderContentMarker();

	/// <summary>
	/// The Pester content marker, <c>CodeOnly</c>: a <c>Describe</c> or <c>Context</c> block opened with a quoted name, followed by an <c>It</c> block with a
	/// quoted name and a <c>Should</c> assertion. The three words in a comment or a string are not a test.
	/// </summary>
	[GeneratedRegex(
		_COMMAND_START + @"(?:Describe|Context)(?![\w\-])[ \t]+(?:-Name[ \t]+)?" + _QUOTE_OPENING + @"[^\n{]*\{" + @"(?:(?:[^\n]*\n)" + _BACKTRACK_POINT + @"){0,100}?[^\n]*?" + _COMMAND_START + @"It(?![\w\-])[ \t]+" + _QUOTE_OPENING + @"[^\n{]*\{" + @"(?:(?:[^\n]*\n)" + _BACKTRACK_POINT + @"){0,100}?[^\n]*?" + _COMMAND_START + @"Should(?![\w\-])",
		RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture,
		matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex PesterTestContentMarker();
}
