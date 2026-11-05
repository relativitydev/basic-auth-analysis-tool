using System.Text.RegularExpressions;

namespace MigrateAuth.Cli.Catalog.Languages;

/// <summary>
/// The settings-property form of an <c>Authorization</c> header whose value comes from the environment (class JS-019, Low only): the member expression that feeds the second dictionary of the file
/// symbol table, and the use-site expression that joins to it.
///
/// THE MEMBER EXPRESSION (EX-JS-102) is anchored at a line start and reads one member per line, in either of two shapes: an object-literal member line <c>name: value</c> (the key bare or quoted) and a
/// property-assignment line <c>receiver.name = value</c> (the receiver a chain of one to eight identifiers, <c>this</c> included). The name ends with <c>authorization</c> in any case and is not a flag key
/// (<c>require</c>, <c>skip</c>, <c>enable</c>, <c>disable</c>, <c>is</c>, <c>has</c>, <c>use</c> at a camel boundary). The value is either an environment read in one of the five forms of the class JS-018
/// (the read alone, the read with one fallback literal, a template whose only hole is the read) whose variable name holds one of the credential words, which marks the record with the empty group
/// <c>environment</c> and carries the word of its fallback in <c>fallbackWord</c>; or any other value, which is collected without the marker so that a name assigned twice, in any form, is indeterminate.
/// An environment read whose variable name holds no credential word is not a record at all. A member that only declares a type (<c>name: string;</c> in an interface, a type literal or a class property
/// without an initializer) is no assignment and is not a record either; a class field with an initializer, annotated or not, is collected as another form and can never resolve (declared not read). The expression has no look-behind, no unbounded quantifier and no lazy loop, captures nothing outside
/// its own span and runs on the file's match bound.
///
/// THE USE-SITE EXPRESSION (EX-JS-019) is a carrier of JS-018 (K1 to K5), a value <c>receiver.property</c> whose property ends with <c>authorization</c> (the group <c>scheme</c> holds the property
/// name, which is what the member dictionary is keyed by), and the guard of JS-018 as the same constant. The receiver is a chain of one to eight identifiers, no segment of which is <c>headers</c> and
/// none of which opens an environment read. A value of JS-018 is an environment read or an identifier that no dot follows, and a value of JS-019 is a dotted chain that is not an environment read, so
/// no span carries both classes.
/// </summary>
internal static partial class JavaScriptPatternExpressions
{
	/// <summary>The word every member name ends with, in any case.</summary>
	private const string _SETTINGS_PROPERTY_SUFFIX = @"(?i:authorization)";

	/// <summary>
	/// A member name that ends with the suffix, at most 64 characters, as one look-ahead over the word and the name itself: the look-ahead walks the identifier characters and gives back to the suffix,
	/// the name takes the same characters, and the end of the word is asserted after both. A flag-key prefix at a camel boundary refuses the name before anything else is read.
	/// </summary>
	private const string _SETTINGS_PROPERTY_NAME =
		@"(?!(?i:require|skip|enable|disable|is|has|use)(?![a-z]))(?=[\w$]{0,63}" + _SETTINGS_PROPERTY_SUFFIX + @"(?![\w$]))[A-Za-z_$][\w$]{0,63}(?![\w$])";

	/// <summary>The name of the property at the use: the same word, without the flag-key refusal (a flag key has no member record, so the use is indeterminate and silent).</summary>
	private const string _SETTINGS_PROPERTY_NAME_AT_THE_USE =
		@"(?=[\w$]{0,63}" + _SETTINGS_PROPERTY_SUFFIX + @"(?![\w$]))[A-Za-z_$][\w$]{0,63}";

	/// <summary>The receiver of a property assignment: one to eight identifiers, each followed by a dot, the last dot directly before the member name.</summary>
	private const string _MEMBER_RECEIVER =
		@"(?:[A-Za-z_$][\w$]{0,63}\.){1,8}";

	/// <summary>The words of an environment variable name that make an assignment a record: a credential, a token, a secret, a key, a password or a basic value, in any case.</summary>
	private const string _CREDENTIAL_WORDS_OF_AN_ENVIRONMENT_NAME =
		@"(?i:auth|token|secret|credential|key|pass|basic)";

	/// <summary>
	/// The environment read of a member, in the five forms of <see cref="_ENVIRONMENT_READ"/>, written without the look-behind that one carries: a member value starts after the separator and the blanks
	/// that follow it, so it always stands at a token boundary.
	/// </summary>
	private const string _MEMBER_ENVIRONMENT_READ =
		@"(?:(?:process\.env|import\.meta\.env|Bun\.env)(?:\." + _ENVIRONMENT_NAME + @"|\[" + _ENVIRONMENT_KEY + @"\])|Deno\.env\.get[ \t]{0,8}\([ \t]{0,8}" + _ENVIRONMENT_KEY + @"[ \t]{0,8}\))";

	/// <summary>The opening of an environment read, up to where the variable name starts, with the hole opening of a template where one stands.</summary>
	private const string _MEMBER_ENVIRONMENT_OPENING =
		@"(?:`\$\{)?(?:(?:process\.env|import\.meta\.env|Bun\.env)(?:\.|\[[\x27\x22])|Deno\.env\.get[ \t]{0,8}\([ \t]{0,8}[\x27\x22])";

	/// <summary>A look-ahead that holds where the variable name of the environment read that opens here holds a credential word (the name read is bounded at 128 characters).</summary>
	private const string _MEMBER_ENVIRONMENT_NAME_HOLDS_A_CREDENTIAL_WORD =
		@"(?=(?:`\$\{)?(?:(?:process\.env|import\.meta\.env|Bun\.env)(?:\.[\w$]{0,127}|\[[\x27\x22][^\x27\x22\n]{0,127})|Deno\.env\.get[ \t]{0,8}\([ \t]{0,8}[\x27\x22][^\x27\x22\n]{0,127})" + _CREDENTIAL_WORDS_OF_AN_ENVIRONMENT_NAME + @")";

	/// <summary>
	/// The environment value of a member that is a record: the environment read whose name holds a credential word, the read alone, with one fallback literal, or as the only hole of a template. It
	/// marks the record with the empty group <c>environment</c> and gives the leading word of the fallback (<c>Basic</c> or <c>Bearer</c>, empty otherwise) in the group <c>fallbackWord</c>, as the
	/// environment declaration of the variable table does.
	/// </summary>
	private const string _MEMBER_ENVIRONMENT_DECLARATION =
		_MEMBER_ENVIRONMENT_NAME_HOLDS_A_CREDENTIAL_WORD + @"(?<environment>)(?:" + _MEMBER_ENVIRONMENT_READ + @"(?:(?<fallbackWord>)|" + _FALLBACK_OPERATOR + @"(?:\x27(?<fallbackWord>(?i:basic|bearer)(?!\w)|)(?>[^\x27\\\n]|\\.){0,200}\x27|\x22(?<fallbackWord>(?i:basic|bearer)(?!\w)|)(?>[^\x22\\\n]|\\.){0,200}\x22|`(?<fallbackWord>(?i:basic|bearer)(?!\w)|)(?>[^`$\\\n]|\$(?!\{)|\\.){0,200}`))|`\$\{" + _MEMBER_ENVIRONMENT_READ + @"\}`(?<fallbackWord>))";

	/// <summary>The end of a member's line: a comma or a semicolon, the end of the line, or the start of a comment, after blanks. Several members on one line are not read.</summary>
	private const string _MEMBER_VALUE_TERMINATOR =
		@"(?=[ \t]{0,40}(?:[;,]|$|//|/\*))";

	/// <summary>
	/// Any value that is not an environment read: collected without the marker, so that a name assigned a literal, a call or a second environment read is counted. An environment read whose variable name holds
	/// no credential word is refused here too, so it is no record of any kind.
	/// </summary>
	private const string _MEMBER_VALUE_OF_ANY_OTHER_FORM =
		@"(?!(?=" + _MEMBER_ENVIRONMENT_OPENING + @")(?!" + _MEMBER_ENVIRONMENT_NAME_HOLDS_A_CREDENTIAL_WORD + @"))(?=\S)";

	/// <summary>The modifiers a class property can carry before its name, at most four, each followed by a blank.</summary>
	private const string _MEMBER_MODIFIERS =
		@"(?:(?:public|protected|private|static|readonly|override|declare)[ \t]{1,8}){0,4}";

	/// <summary>
	/// What follows the colon of a member that only declares a type, and is therefore no assignment, in three readings. First, a primitive type word with nothing after it on the line that assigns. Second,
	/// any text up to a semicolon that holds no equals sign and no parenthesis (an object literal never ends a member with a semicolon). Third, a type text that runs to the end of the line with no
	/// semicolon, no equals sign, no parenthesis and no trailing comma: it starts with an identifier or a type operator and not with an environment read, a quote, a backtick, a digit, an opening brace or
	/// bracket or a sign, or else it holds a union or intersection operator (a quoted union such as <c>'Basic' | 'Bearer'</c> is a type). A value that is a quote, a digit, a brace or a bracket, holds an equals
	/// sign or a parenthesis, or ends with a comma stays an assignment. A class property with an initializer holds an equals sign, so it is collected as any other assignment. Where the shape is
	/// ambiguous (the last member of an object literal that is a bare name or dotted path, with no comma) the member is read as a type and is not collected.
	/// </summary>
	private const string _MEMBER_TYPE_ONLY_DECLARATION =
		@"(?:(?:string|number|boolean|bigint|symbol|unknown|any|never)(?![\w$])[^\n=(]{0,200}$|[^\n=(;]{1,200};|(?![ \t])(?!" + _MEMBER_ENVIRONMENT_OPENING + @")(?:(?![\x27\x22`\d{\[\-\x2B!~/\\])|(?=[^\n=(;]{0,200}[|&]))(?:[^\n=(;,]|,[ \t]{0,8}(?=[^\s,])){1,200}$)";

	/// <summary>
	/// The head of an annotated class property with an initializer, <c>[modifiers] name[?!]: type =</c>: the type text between the colon and the equals sign is bounded at 200 characters and holds no equals
	/// sign, parenthesis or semicolon. It is tried before the colon form so that the initializer, not the type, is the value read: the initializer of a class field is read as an other-form record whatever
	/// it is: a class field is declared not read, so it is collected for the count and never resolves.
	/// </summary>
	private const string _MEMBER_ANNOTATED_PROPERTY_HEAD =
		_MEMBER_MODIFIERS + @"(?<name>" + _SETTINGS_PROPERTY_NAME + @")[?!]?[ \t]{0,8}:(?!:)[^\n=(;]{1,200}=(?![=>])";

	/// <summary>EX-JS-102: the member-declaration expression of the file symbol table's second dictionary.</summary>
	private const string _EX_JS_102 =
		@"^[ \t]{0,64}(?<opening>)(?:(?:" + _MEMBER_RECEIVER + @"(?<name>" + _SETTINGS_PROPERTY_NAME + @")[ \t]{0,8}=(?!=))[ \t]{0,8}(?:" + _MEMBER_ENVIRONMENT_DECLARATION + _MEMBER_VALUE_TERMINATOR + @"|" + _MEMBER_VALUE_OF_ANY_OTHER_FORM + @")|(?:" + _MEMBER_ANNOTATED_PROPERTY_HEAD + @"|" + _MEMBER_MODIFIERS + @"(?<name>" + _SETTINGS_PROPERTY_NAME + @")[ \t]{0,8}=(?!=))[ \t]{0,8}" + _MEMBER_VALUE_OF_ANY_OTHER_FORM + @"|(?:" + _MEMBER_MODIFIERS + @"(?:[\x27\x22](?<name>" + _SETTINGS_PROPERTY_NAME + @")[\x27\x22]|(?<name>" + _SETTINGS_PROPERTY_NAME + @"))[ \t]{0,8}:(?!:)(?![ \t]{0,8}" + _MEMBER_TYPE_ONLY_DECLARATION + @"))[ \t]{0,8}(?:" + _MEMBER_ENVIRONMENT_DECLARATION + _MEMBER_VALUE_TERMINATOR + @"|" + _MEMBER_VALUE_OF_ANY_OTHER_FORM + @"))";

	/// <summary>
	/// The value of the header read by JS-019: a dotted chain of one to eight identifiers whose last segment ends with the suffix, no segment named <c>headers</c> and no chain that opens an environment read, then
	/// a value terminator. The group <c>scheme</c> holds the property name. The property is not followed by a dot, a call or an index.
	/// </summary>
	private const string _SETTINGS_PROPERTY_VALUE =
		@"(?![\x27\x22`])(?!(?:process\.env|import\.meta\.env|Bun\.env|Deno\.env)(?![\w$]))(?:(?!headers(?![\w$]))[A-Za-z_$][\w$]{0,63}\.){1,8}(?<scheme>" + _SETTINGS_PROPERTY_NAME_AT_THE_USE + @")(?![\w$.(\[])(?=" + _SPACE + @"(?:[,})\];]|\z))";

	/// <summary>EX-JS-019: a carrier of JS-018, the settings-property value and the guard of JS-018, which is reused as the same constant.</summary>
	private const string _EX_JS_019 =
		_CARRIERS + _SETTINGS_PROPERTY_VALUE + _BEARER_GUARD;

	/// <summary>EX-JS-102: the member-declaration expression of the file symbol table's second dictionary, shared by `JavaScript` and `TypeScript`.</summary>
	[GeneratedRegex(_EX_JS_102, _LINE_OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex MemberDeclaration();

	/// <summary>EX-JS-019: the expression of JS-019, AuthorizationHeaderValueFromSettingsProperty.</summary>
	[GeneratedRegex(_EX_JS_019, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex AuthorizationHeaderValueFromSettingsProperty();
}
