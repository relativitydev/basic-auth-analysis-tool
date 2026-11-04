using System.Text.RegularExpressions;

namespace MigrateAuth.Cli.Catalog.Languages;

/// <summary>The second half of the `VBNet` expressions: classes <c>VB-010</c> to <c>VB-019</c>, the constant declaration and the content markers. See the first file for the rules they keep.</summary>
internal static partial class VBNetPatternExpressions
{
	/// <summary>
	/// The password argument of VB-010 is not an identifier named for a token without a password word: a system or bearer token handed to the entry point is
	/// not a Basic pair. Read on the last segment of the argument; a name with <c>password</c>, <c>secret</c> or <c>pwd</c> in it is still read.
	/// </summary>
	private const string _NOT_A_TOKEN_HELD_AS_PASSWORD =
		@"(?!(?:" + _NAME + @"\.)*(?![A-Za-z0-9_]*(?:password|secret|pwd))[A-Za-z0-9_]*token[A-Za-z0-9_]*(?![A-Za-z0-9_(.]))";

	/// <summary>VB-010 · SdkConstructorWithUserAndPassword (the type name may end a longer one; arity three)</summary>
	[GeneratedRegex(
		@"\bNew[ \t]+" + _QUALIFIER + @"(?<construct>[A-Za-z0-9_]*ImportAPI)(?![A-Za-z0-9_])[ \t]*\(" + _GAP_IMPLICIT + _ARGUMENT_NAMED + _COMMA + @"(?<credential>" + _NAMED_PREFIX + _NOT_A_TOKEN_HELD_AS_PASSWORD + _ARGUMENT + @")" + _COMMA + _NAMED_PREFIX + @"(?:" + _STRING + @"|(?:" + _STATEMENT_UNIT + @"){1,200}?)" + _GAP_IMPLICIT + @"\)",
		OPTIONS, matchTimeoutMilliseconds: TIMEOUT_IN_MILLISECONDS)]
	public static partial Regex SdkConstructorWithUserAndPassword();

	/// <summary>VB-011 · SdkSettingsCredentialProperties (both members, either order, within five logical lines)</summary>
	[GeneratedRegex(
		@"\.(?<construct>RelativityUsername)[ \t]*=" + _LINE_WINDOW_FIVE + @"(?=\.)\.RelativityPassword[ \t]*=[ \t]*(?<credential>" + _ARGUMENT + @")"
		+ @"|\.(?<construct>RelativityPassword)[ \t]*=[ \t]*(?<credential>" + _ARGUMENT + @")" + _LINE_WINDOW_FIVE + @"(?=\.)\.RelativityUsername[ \t]*=",
		OPTIONS, matchTimeoutMilliseconds: TIMEOUT_IN_MILLISECONDS)]
	public static partial Regex SdkSettingsCredentialProperties();

	/// <summary>The credential's two arguments, in parentheses: the password is the credential.</summary>
	private const string _NETWORK_CREDENTIAL_PAIR = @"[ \t]*\(" + _GAP_IMPLICIT + _ARGUMENT_NAMED + _COMMA + @"(?<credential>" + _ARGUMENT_NAMED + @")" + _GAP_IMPLICIT + @"\)";

	/// <summary>The credential's object initializer carrying both halves, either member order, bounded to its own braces (one statement); the constructor's own arguments may precede it.</summary>
	private const string _NETWORK_CREDENTIAL_INITIALIZER =
		@"(?:[ \t]*" + _GROUP + @")?[ \t]*With[ \t]*\{" + _GAP_IMPLICIT + @"(?:\.UserName[ \t]*=[ \t]*(?:" + _ARGUMENT + @")" + _COMMA + @"\.Password[ \t]*=[ \t]*(?<credential>" + _ARGUMENT + @")"
		+ @"|\.Password[ \t]*=[ \t]*(?<credential>" + _ARGUMENT + @")" + _COMMA + @"\.UserName[ \t]*=[ \t]*(?:" + _ARGUMENT + @"))" + _GAP_IMPLICIT + @"\}";

	/// <summary>The credential construction a sink reads, after the type name: the pair constructor or the initializer.</summary>
	private const string _NETWORK_CREDENTIAL_TAIL = @"(?![A-Za-z0-9_])(?:" + _NETWORK_CREDENTIAL_PAIR + @"|" + _NETWORK_CREDENTIAL_INITIALIZER + @")";

	/// <summary>A cast of a value to a type, read up to the comma that precedes the type: the keyword and its first argument (one level of brackets inside, at most 200 characters).</summary>
	private const string _CAST_BEFORE_THE_TYPE = @"\b(?:CType|DirectCast|TryCast)[ \t]*\((?:[^\n()]|\([^\n()]*\)){0,200},[ \t]*";

	/// <summary>What types the receiver of an assignment after creation, read backwards from the type name: a declaration, a creation, a With New header, or a cast.</summary>
	private const string _RECEIVER_TYPING_BEFORE_THE_TYPE = @"(?:\bAs[ \t]+New[ \t]+|=[ \t]*New[ \t]+|\bWith[ \t]+New[ \t]+|\bAs[ \t]+|=[ \t]+|" + _CAST_BEFORE_THE_TYPE + @")(?:[A-Za-z_]\w*\.)*";

	/// <summary>VB-012 · NetworkCredentialOnHttpType (the initializer; the assignment after a typed creation, a With New header or a cast within five logical lines; the assignment on a cast receiver)</summary>
	[GeneratedRegex(
		@"(?:\bNew[ \t]+" + _QUALIFIER + _HTTP_TYPES + @"(?![A-Za-z0-9_])" + _CALL_WINDOW + @"(?<construct>\.?Credentials)"
		+ @"|" + _HTTP_TYPES + @"(?![A-Za-z0-9_])(?<=" + _RECEIVER_TYPING_BEFORE_THE_TYPE + _HTTP_TYPES + @")" + _LINE_WINDOW_FIVE + @"(?=\.)(?<construct>\.Credentials)"
		+ @"|" + _CAST_BEFORE_THE_TYPE + _QUALIFIER + _HTTP_TYPES + @"(?![A-Za-z0-9_])[ \t]*\)(?<construct>\.Credentials))"
		+ @"[ \t]*=[ \t]*New[ \t]+" + _QUALIFIER + @"NetworkCredential" + _NETWORK_CREDENTIAL_TAIL,
		OPTIONS, matchTimeoutMilliseconds: TIMEOUT_IN_MILLISECONDS)]
	public static partial Regex NetworkCredentialOnHttpType();

	/// <summary>VB-013 · NetworkCredentialConstruction, the residue: two arguments or the initializer carrying both halves; declines the last argument of a cache Add with a quoted scheme.</summary>
	[GeneratedRegex(
		@"(?<!\.Add[ \t]*\((?:[^\n()]|\([^\n()]*\)){0,200},[ \t]*)\bNew[ \t]+" + _QUALIFIER + @"(?<construct>NetworkCredential)" + _NETWORK_CREDENTIAL_TAIL,
		OPTIONS, matchTimeoutMilliseconds: TIMEOUT_IN_MILLISECONDS)]
	public static partial Regex NetworkCredentialConstruction();

	/// <summary>VB-014 · Base64UserPasswordPair, both halves non-empty: the pair first and the encoder within five logical lines, or the pair inside the encoder call.</summary>
	[GeneratedRegex(
		_QUOTE + @":" + _QUOTE + @"(?<=(?<![A-Za-z0-9_.])(?<half1>[A-Za-z0-9_.]+(?:\([^()\n]*\))?)" + _NOT_PORT_HALF + @"[ \t]*[&+][ \t]*" + _QUOTE + @":" + _QUOTE + @")c?[ \t]*[&+][ \t]*(?<credential>(?:" + _NAME + @"\.)*" + _NAME + @"(?:" + _GROUP + @")?)" + _NOT_SIZE_HALF + @"(?![A-Za-z0-9_]|\.[A-Za-z_])" + _LINE_WINDOW_FIVE + @"(?=B)(?<encoder>Base64[A-Za-z0-9_]*)[ \t]*\("
		+ @"|(?<encoder>Base64[A-Za-z0-9_]*)[ \t]*\((?:(?=(?>[^\n:]{0,303}):)(?=[^\n]{0,300}" + _QUOTE + @":" + _QUOTE + @")" + _CALL_WINDOW + @"(?<half1>(?:" + _NAME + @"\.)*" + _NAME + @")" + _NOT_PORT_HALF + @"[ \t]*(?:&|\+)[ \t]*" + _QUOTE + @":" + _QUOTE + @"[ \t]*(?:&|\+)[ \t]*(?<credential>(?:" + _NAME + @"\.)*" + _NAME + @")" + _NOT_PORT_HALF + @""
		+ @"|(?=(?>[^\n:]{0,303}):)(?=[^\n]{0,300}\}:\{)" + _CALL_WINDOW + @"\$" + _QUOTE + @"\{[ \t]*(?<half1>" + _ARGUMENT + @")" + _NOT_PORT_HALF + @"[ \t]*\}:\{[ \t]*(?<credential>" + _ARGUMENT + @")" + _NOT_PORT_HALF + @"[ \t]*\}" + _QUOTE
		+ @"|(?=[^\n]{0,300}String\.(?:Concat|Join))" + _CALL_WINDOW + @"String\.(?:Concat|Join)[ \t]*\([ \t]*(?:" + _QUOTE + @":" + _QUOTE + @",[ \t]*)?(?<half1>" + _ARGUMENT + @")" + _NOT_PORT_HALF + @"[ \t]*,[ \t]*(?:" + _QUOTE + @":" + _QUOTE + @"[ \t]*,[ \t]*)?(?<credential>" + _ARGUMENT + @")" + _NOT_PORT_HALF + @"[ \t]*\)"
		+ @"|(?=(?>[^\n:]{0,303}):)(?=[^\n]{0,300}\{0\}:\{1\})" + _CALL_WINDOW + @"(?:String\.Format|Format)[ \t]*\([ \t]*" + _QUOTE + @"\{0\}:\{1\}" + _QUOTE + _COMMA + @"(?<half1>" + _PAIR_HALF + @")" + _COMMA + @"(?<credential>" + _PAIR_HALF + @")" + _GAP_IMPLICIT + @"\))"
		+ @"|(?:" + _PAIR_ASSEMBLY + @")" + _LINE_WINDOW_FIVE + @"(?=B)(?<encoder>Base64[A-Za-z0-9_]*)[ \t]*\(",
		OPTIONS, matchTimeoutMilliseconds: TIMEOUT_IN_MILLISECONDS)]
	public static partial Regex Base64UserPasswordPair();

	// ------------------------------------------------------------------------------------------
	// VB-015: the empty half in every spelling read
	// ------------------------------------------------------------------------------------------

	/// <summary>An empty-string operand: the empty literal, String.Empty or vbNullString, whole.</summary>
	private const string _EMPTY_OPERAND = @"(?:" + _QUOTE + _QUOTE + @"(?!" + _QUOTE + @")|String\.Empty(?![A-Za-z0-9_])|vbNullString(?![A-Za-z0-9_]))";

	/// <summary>The empty operands that can stand inside an interpolation hole.</summary>
	private const string _EMPTY_INTERPOLATED = @"(?:String\.Empty|vbNullString)";

	/// <summary>The variable assigned an empty literal, read back from the group <c>e</c>.</summary>
	private const string _EMPTY_VARIABLE_READ_BACK = @"\k<e>(?![A-Za-z0-9_.(])";

	/// <summary>The non-empty half: a dotted name, with the arguments of a call if it is one, at a word start.</summary>
	private const string _CREDENTIAL_HALF = @"(?<![A-Za-z0-9_.])(?<credential>(?:" + _NAME + @"\.)*" + _NAME + @")(?:" + _GROUP + @")?";

	/// <summary>The statement that assigns a variable an empty literal, at a statement start, and nothing else on the right: read forward then back, group <c>e</c>.</summary>
	private const string _EMPTY_VARIABLE_ASSIGNMENT =
		@"=(?=[ \t]*" + _EMPTY_OPERAND + @"(?![ \t]*[&+.(]))(?<=(?:\A|[\n:])[ \t]*(?:(?:Dim|Static|Private|Public|Friend|Protected|Shared)[ \t]+)*(?<e>" + _NAME + @")(?:[ \t]+As[ \t]+[A-Za-z_][A-Za-z0-9_.]*)?[ \t]*=)[ \t]*" + _EMPTY_OPERAND;

	/// <summary>Ten lines, each read to at most 200 characters: the window between the empty assignment and the pair.</summary>
	private const string _LINE_WINDOW_TEN = @"(?:[^\n]{0,200}\n){0,10}?[^\n]{0,200}?";

	/// <summary>The lookahead that opens an inline pair: the colon as a literal, or the two placeholders, or the interpolation's colon, within the line.</summary>
	private const string _INLINE_PAIR_SIGN = @"(?=(?>[^\n:]{0,303}):)(?:(?=[^\n]{0,300}" + _QUOTE + @":" + _QUOTE + @")|(?=[^\n]{0,300}\}:\{)|(?=[^\n]{0,300}\{0\}:\{1\}))";

	/// <summary>A format with the two placeholders and an empty literal operand as one half.</summary>
	private const string _FORMAT_WITH_EMPTY_LITERAL =
		@"(?:String\.Format|Format)[ \t]*\([ \t]*" + _QUOTE + @"\{0\}:\{1\}" + _QUOTE + _COMMA + @"(?:" + _EMPTY_OPERAND + _COMMA + _CREDENTIAL_HALF + @"|" + _CREDENTIAL_HALF + _COMMA + _EMPTY_OPERAND + @")" + _GAP_IMPLICIT + @"\)";

	/// <summary>An interpolation with an empty literal operand as one half.</summary>
	private const string _INTERPOLATION_WITH_EMPTY_LITERAL =
		@"\$" + _QUOTE + @"\{[ \t]*(?:" + _EMPTY_INTERPOLATED + @"[ \t]*\}:\{[ \t]*" + _CREDENTIAL_HALF + @"|" + _CREDENTIAL_HALF + @"[ \t]*\}:\{[ \t]*" + _EMPTY_INTERPOLATED + @")[ \t]*\}" + _QUOTE;

	/// <summary>A concatenation or a join call with an empty literal operand as one half.</summary>
	private const string _CALL_WITH_EMPTY_LITERAL =
		@"(?:String\.Concat|String\.Join|Concat|Join)[ \t]*\([ \t]*(?:" + _QUOTE + @":" + _QUOTE + @",[ \t]*)?(?:" + _EMPTY_OPERAND + @"[ \t]*,[ \t]*(?:" + _QUOTE + @":" + _QUOTE + @"[ \t]*,[ \t]*)?" + _CREDENTIAL_HALF + @"|" + _CREDENTIAL_HALF + @"[ \t]*,[ \t]*(?:" + _QUOTE + @":" + _QUOTE + @"[ \t]*,[ \t]*)?" + _EMPTY_OPERAND + @")[ \t]*\)";

	/// <summary>The three assembled spellings with an empty literal operand as one half.</summary>
	private const string _ASSEMBLY_WITH_EMPTY_LITERAL = _FORMAT_WITH_EMPTY_LITERAL + @"|" + _INTERPOLATION_WITH_EMPTY_LITERAL + @"|" + _CALL_WITH_EMPTY_LITERAL;

	/// <summary>A format with the two placeholders and the variable assigned an empty literal as one half.</summary>
	private const string _FORMAT_WITH_EMPTY_VARIABLE =
		@"(?:String\.Format|Format)[ \t]*\([ \t]*" + _QUOTE + @"\{0\}:\{1\}" + _QUOTE + _COMMA + @"(?:" + _EMPTY_VARIABLE_READ_BACK + _COMMA + _CREDENTIAL_HALF + @"|" + _CREDENTIAL_HALF + _COMMA + _EMPTY_VARIABLE_READ_BACK + @")" + _GAP_IMPLICIT + @"\)";

	/// <summary>An interpolation with the variable assigned an empty literal as one half.</summary>
	private const string _INTERPOLATION_WITH_EMPTY_VARIABLE =
		@"\$" + _QUOTE + @"\{[ \t]*(?:" + _EMPTY_VARIABLE_READ_BACK + @"[ \t]*\}:\{[ \t]*" + _CREDENTIAL_HALF + @"|" + _CREDENTIAL_HALF + @"[ \t]*\}:\{[ \t]*" + _EMPTY_VARIABLE_READ_BACK + @")[ \t]*\}" + _QUOTE;

	/// <summary>A concatenation or a join call with the variable assigned an empty literal as one half.</summary>
	private const string _CALL_WITH_EMPTY_VARIABLE =
		@"(?:String\.Concat|String\.Join|Concat|Join)[ \t]*\([ \t]*(?:" + _QUOTE + @":" + _QUOTE + @",[ \t]*)?(?:" + _EMPTY_VARIABLE_READ_BACK + @"[ \t]*,[ \t]*(?:" + _QUOTE + @":" + _QUOTE + @"[ \t]*,[ \t]*)?" + _CREDENTIAL_HALF + @"|" + _CREDENTIAL_HALF + @"[ \t]*,[ \t]*(?:" + _QUOTE + @":" + _QUOTE + @"[ \t]*,[ \t]*)?" + _EMPTY_VARIABLE_READ_BACK + @")[ \t]*\)";

	/// <summary>The three assembled spellings with the variable assigned an empty literal as one half.</summary>
	private const string _ASSEMBLY_WITH_EMPTY_VARIABLE = _FORMAT_WITH_EMPTY_VARIABLE + @"|" + _INTERPOLATION_WITH_EMPTY_VARIABLE + @"|" + _CALL_WITH_EMPTY_VARIABLE;

	/// <summary>What stands before a colon literal that ends the left half of a pair: a name (a call's arguments allowed), the operator and the colon, read backwards from the colon.</summary>
	private const string _NAME_BEFORE_THE_COLON = @"(?<![A-Za-z0-9_.])(?<credential>(?:" + _NAME + @"\.)*" + _NAME + @")(?:\([^()\n]*\))?[ \t]*[&+][ \t]*" + _QUOTE + @":" + _QUOTE;

	/// <summary>The concatenation with the empty literal as the right half, opening on the colon literal: <c>credential &amp; ":" &amp; ""</c>.</summary>
	private const string _CONCATENATION_WITH_EMPTY_LAST = _QUOTE + @":" + _QUOTE + @"(?<=" + _NAME_BEFORE_THE_COLON + @")[ \t]*[&+][ \t]*" + _EMPTY_OPERAND;

	/// <summary>The two concatenations with the variable assigned an empty literal as one half, each opening on the colon literal.</summary>
	private const string _CONCATENATION_WITH_EMPTY_VARIABLE =
		_QUOTE + @":" + _QUOTE + @"(?<=" + _NAME_BEFORE_THE_COLON + @")[ \t]*[&+][ \t]*" + _EMPTY_VARIABLE_READ_BACK
		+ @"|" + _QUOTE + @":" + _QUOTE + @"(?<=(?<![A-Za-z0-9_.])\k<e>[ \t]*[&+][ \t]*" + _QUOTE + @":" + _QUOTE + @")[ \t]*[&+][ \t]*" + _CREDENTIAL_HALF;

	/// <summary>
	/// The colon written last on its own as the whole of the pair, opening on the colon literal: the credential before it, preceded by no operand, no further operand continuing,
	/// and not the variable the statement assigns (<c>pair = pair &amp; ":"</c> is an accumulation, read by neither pair class). The checks run right to left inside the look-behind.
	/// </summary>
	private const string _COLON_LAST =
		_QUOTE + @":" + _QUOTE + @"(?<=(?!\k<t>(?![A-Za-z0-9_]))(?>(?<=\b(?<t>" + _NAME + @")[ \t]*=[ \t]*)|)(?<![A-Za-z0-9_.]|[&+][ \t]*)(?<credential>(?:" + _NAME + @"\.)*" + _NAME + @")(?:\([^()\n]*\))?[ \t]*[&+][ \t]*" + _QUOTE + @":" + _QUOTE + @")(?!" + _GAP + @"?[&+])";

	/// <summary>
	/// The encoder call and what a pair inside it can be, read from the opening parenthesis by the one prefix and one look-ahead for a pair marker on the line (the colon literal, or the
	/// colon between two interpolation or format placeholders), so a call without a marker does not walk the window. One window, then the alternatives, each opening on a literal:
	/// the empty operand first, a format, an interpolation, a concatenation or a join, the credential then the colon then an empty last, the colon first, and the colon last.
	/// </summary>
	private const string _ENCODER_CALL_HOLDING_THE_PAIR =
		@"(?<encoder>Base64[A-Za-z0-9_]*)[ \t]*\((?=(?>[^\n:]{0,303}):)(?:(?=[^\n]{0,300}" + _QUOTE + @":" + _QUOTE + @")|(?=[^\n]{0,300}\}:\{))" + _CALL_WINDOW + @"(?:"
		+ @"(?:" + _QUOTE + _QUOTE + @"|String\.Empty)[ \t]*(?:&|\+)[ \t]*" + _QUOTE + @":" + _QUOTE + @"[ \t]*(?:&|\+)[ \t]*(?<credential>(?:" + _NAME + @"\.)*" + _NAME + @")"
		+ @"|" + _FORMAT_WITH_EMPTY_LITERAL
		+ @"|" + _INTERPOLATION_WITH_EMPTY_LITERAL
		+ @"|" + _CALL_WITH_EMPTY_LITERAL
		+ @"|" + _CONCATENATION_WITH_EMPTY_LAST
		+ @"|" + _QUOTE + @":" + _QUOTE + @"(?<![A-Za-z0-9_)\]}" + _QUOTE + @"][ \t]*[&+][ \t]*" + _QUOTE + @":" + _QUOTE + @")[ \t]*[&+][ \t]*" + _CREDENTIAL_HALF + @"(?![A-Za-z0-9_]|" + _GAP + @"?[&+])"
		+ @"|" + _COLON_LAST + @"(?=[ \t]*\))"
		+ @")";

	/// <summary>The variable assigned an empty literal, then the pair using it, then the encoder, or the encoder holding the pair, within the ten-line and five-line windows.</summary>
	private const string _EMPTY_VARIABLE_THEN_THE_PAIR =
		_EMPTY_VARIABLE_ASSIGNMENT + _LINE_WINDOW_TEN + @"(?:(?:" + _ASSEMBLY_WITH_EMPTY_VARIABLE + @"|" + _CONCATENATION_WITH_EMPTY_VARIABLE + @")" + _LINE_WINDOW_FIVE + @"(?=B)(?<encoder>Base64[A-Za-z0-9_]*)[ \t]*\("
		+ @"|(?<encoder>Base64[A-Za-z0-9_]*)[ \t]*\(" + _INLINE_PAIR_SIGN + _CALL_WINDOW + @"(?:" + _ASSEMBLY_WITH_EMPTY_VARIABLE + @"|" + _CONCATENATION_WITH_EMPTY_VARIABLE + @"))";

	/// <summary>VB-015 · TokenCarriedUnderBasicScheme, one half empty</summary>
	[GeneratedRegex(
		@"(?:" + _QUOTE + _QUOTE + @"|String\.Empty|vbNullString)[ \t]*(?:&|\+)[ \t]*" + _QUOTE + @":" + _QUOTE + @"[ \t]*(?:&|\+)[ \t]*(?<credential>(?:" + _NAME + @"\.)*" + _NAME + @")" + _LINE_WINDOW_FIVE + @"(?=B)(?<encoder>Base64[A-Za-z0-9_]*)[ \t]*\("
		+ @"|(?<![A-Za-z0-9_)\]}" + _QUOTE + @"][ \t]*[&+][ \t]*)" + _QUOTE + @":" + _QUOTE + @"[ \t]*(?:&|\+)[ \t]*(?<credential>(?:" + _NAME + @"\.)*" + _NAME + @")" + _LINE_WINDOW_FIVE + @"(?=B)(?<encoder>Base64[A-Za-z0-9_]*)[ \t]*\("
		+ @"|" + _ENCODER_CALL_HOLDING_THE_PAIR
		+ @"|(?:" + _ASSEMBLY_WITH_EMPTY_LITERAL + @"|" + _CONCATENATION_WITH_EMPTY_LAST + @")" + _LINE_WINDOW_FIVE + @"(?=B)(?<encoder>Base64[A-Za-z0-9_]*)[ \t]*\("
		+ @"|" + _EMPTY_VARIABLE_THEN_THE_PAIR
		+ @"|" + _COLON_LAST + _LINE_WINDOW_FIVE + @"(?=B)(?<encoder>Base64[A-Za-z0-9_]*)[ \t]*\(",
		OPTIONS, matchTimeoutMilliseconds: TIMEOUT_IN_MILLISECONDS)]
	public static partial Regex TokenCarriedUnderBasicScheme();

	/// <summary>The first argument of VB-016 is not an ftp, ftps or sftp literal, bare or as the argument of a Uri construction.</summary>
	private const string _NOT_A_NON_HTTP_LITERAL = @"(?!(?:New[ \t]+(?:System\.)?Uri[ \t]*\([ \t]*)?" + _QUOTE + @"(?:ftps?|sftp)://)";

	/// <summary>
	/// The first argument of VB-016 is not an identifier declared, as a Const or a Dim, holding an ftp, ftps or sftp literal, the declaration lying within
	/// the reach of the declaration look-arounds (see <see cref="_DECLARATION_REACH_IN_CHARACTERS"/>) before or after the call. An address held in a parameter or under another declaration
	/// stays read.
	/// </summary>
	private const string _NOT_A_NON_HTTP_CONSTANT =
		@"(?<!" + _NON_HTTP_CONSTANT_DECLARATION + _DECLARATION_REACH_BEHIND + @")(?!" + _DECLARATION_REACH_AHEAD + _NON_HTTP_CONSTANT_DECLARATION + @")";

	private const string _NON_HTTP_CONSTANT_DECLARATION = @"\b(?:Const|Dim)[ \t]+\k<address>(?![A-Za-z0-9_])[^\n=]*=[ \t]*" + _QUOTE + @"(?:ftps?|sftp)://";

	/// <summary>VB-016 · NetworkTransferWithCredentials: positions three and four, or a credential-worded third argument.</summary>
	[GeneratedRegex(
		@"\.(?<construct>DownloadFile|UploadFile)(?<=(?:\bMy\.Computer\.Network|^[ \t]*)\.(?:DownloadFile|UploadFile))[ \t]*\(" + _GAP_IMPLICIT + _NAMED_PREFIX + _NOT_A_NON_HTTP_LITERAL + @"(?<address>" + _ARGUMENT + @")" + _NOT_A_NON_HTTP_CONSTANT + _COMMA + _ARGUMENT_NAMED + _COMMA + @"(?:" + _ARGUMENT_NAMED + _COMMA + @"(?<credential>" + _ARGUMENT_NAMED + @")|" + _CREDENTIAL_NAME + @")",
		OPTIONS | RegexOptions.Multiline, matchTimeoutMilliseconds: TIMEOUT_IN_MILLISECONDS)]
	public static partial Regex NetworkTransferWithCredentials();

	/// <summary>VB-017 · LateBoundHttpObjectCredentials: Open with an HTTP verb and five arguments, or SetCredentials, each argument with an optional in-order name.</summary>
	[GeneratedRegex(
		@"\.(?<construct>Open)[ \t]*\(" + _GAP_IMPLICIT + _NAMED_PREFIX + _QUOTE + @"(?:GET|POST|PUT|DELETE|HEAD|PATCH|OPTIONS)" + _QUOTE + _COMMA + _NAMED_PREFIX + _ARGUMENT + _COMMA + _NAMED_PREFIX + @"(?:True|False)" + _COMMA + _NAMED_PREFIX + _ARGUMENT + _COMMA + _NAMED_PREFIX + @"(?<credential>" + _ARGUMENT + @")" + _GAP_IMPLICIT + @"\)"
		+ @"|\.(?<construct>SetCredentials)[ \t]*\(" + _GAP_IMPLICIT + _NAMED_PREFIX + _ARGUMENT + _COMMA + _NAMED_PREFIX + @"(?<credential>" + _ARGUMENT + @")" + _COMMA + _NAMED_PREFIX + @"(?:\d+|" + _ARGUMENT + @")",
		OPTIONS, matchTimeoutMilliseconds: TIMEOUT_IN_MILLISECONDS)]
	public static partial Regex LateBoundHttpObjectCredentials();

	/// <summary>The process-start information with a user name and a password as text: the user name and the password in either order within one logical line (the initializer), or within the five-line windows after a typed receiver.</summary>
	private const string _PROCESS_START_INFO_WITH_CREDENTIAL =
		@"\bNew[ \t]+" + _QUALIFIER + @"(?<construct>ProcessStartInfo)(?![A-Za-z0-9_])" + _CALL_WINDOW + @"(?:\.UserName[ \t]*=" + _CALL_WINDOW + @"\.Password[ \t]*=[ \t]*(?<credential>" + _ARGUMENT + @")"
		+ @"|\.Password[ \t]*=[ \t]*(?<credential>" + _ARGUMENT + @")" + _CALL_WINDOW + @"\.UserName[ \t]*=)"
		+ @"|(?<construct>ProcessStartInfo)(?![A-Za-z0-9_])(?<=" + _RECEIVER_TYPING_BEFORE_THE_TYPE + @"ProcessStartInfo)" + _LINE_WINDOW_FIVE + @"(?=\.)(?:\.UserName[ \t]*=" + _LINE_WINDOW_FIVE + @"(?=\.)\.Password[ \t]*=[ \t]*(?<credential>" + _ARGUMENT + @")"
		+ @"|\.Password[ \t]*=[ \t]*(?<credential>" + _ARGUMENT + @")" + _LINE_WINDOW_FIVE + @"(?=\.)\.UserName[ \t]*=)";

	/// <summary>The directory sinks with the credential as text arguments: the entry's third argument, the same entry as the searcher's first argument, and the process start with a user name and a password.</summary>
	private const string _DIRECTORY_AND_PROCESS_SINKS =
		@"\bNew[ \t]+" + _QUALIFIER + @"(?<construct>DirectoryEntry)(?![A-Za-z0-9_])[ \t]*\(" + _GAP_IMPLICIT + _ARGUMENT_NAMED + _COMMA + _ARGUMENT_NAMED + _COMMA + _NAMED_PREFIX + @"(?<credential>" + _ARGUMENT + @")"
		+ @"|\bNew[ \t]+" + _QUALIFIER + @"(?<construct>DirectorySearcher)(?![A-Za-z0-9_])[ \t]*\(" + _GAP_IMPLICIT + _NAMED_PREFIX + @"New[ \t]+" + _QUALIFIER + @"DirectoryEntry[ \t]*\(" + _GAP_IMPLICIT + _ARGUMENT_NAMED + _COMMA + _ARGUMENT_NAMED + _COMMA + _NAMED_PREFIX + @"(?<credential>" + _ARGUMENT + @")"
		+ @"|" + _PROCESS_START_INFO_WITH_CREDENTIAL
		+ @"|\bProcess\.(?<construct>Start)[ \t]*\(" + _GAP_IMPLICIT + _ARGUMENT_NAMED + _COMMA + _ARGUMENT_NAMED + _COMMA + _NAMED_PREFIX + @"(?<credential>" + _ARGUMENT + @")" + _COMMA + _ARGUMENT_NAMED + _GAP_IMPLICIT + @"\)";

	/// <summary>VB-018 · NonHttpCredentialSink: the sink with the credential in the same statement, or bound within five logical lines (by the variable, a With New header or a leading-dot assignment); the cast FtpWebRequest; the host-and-port cache entry; the directory and process-start sinks with the credential as text arguments.</summary>
	[GeneratedRegex(
		@"(?:\bNew[ \t]+" + _QUALIFIER + @"(?<construct>SmtpClient|WebProxy)(?![A-Za-z0-9_])" + _CALL_WINDOW + @"\.?Credentials"
		+ @"|(?<construct>SmtpClient|WebProxy)(?![A-Za-z0-9_])(?<=(?:\bAs[ \t]+New[ \t]+|=[ \t]*New[ \t]+|\bWith[ \t]+New[ \t]+|\bAs[ \t]+|=[ \t]+)(?:[A-Za-z_]\w*\.)*(?:SmtpClient|WebProxy))" + _LINE_WINDOW_FIVE + @"(?=\.)\.Credentials"
		+ @"|(?:CType|DirectCast|TryCast)(?<=\b(?:Dim[ \t]+(?<v>" + _NAME + @")(?:[ \t]+As[ \t]+[A-Za-z0-9_.]+)?|(?<v>" + _NAME + @"))[ \t]*=[ \t]*(?:CType|DirectCast|TryCast))[ \t]*\([ \t]*" + _QUALIFIER + @"(?:Ftp)?(?:Web)?Request\.Create" + _GROUP + @"[ \t]*,[ \t]*" + _QUALIFIER + @"(?<construct>FtpWebRequest)[ \t]*\)" + _LINE_WINDOW_FIVE + @"(?=\.)\.Credentials)[ \t]*=[ \t]*New[ \t]+" + _QUALIFIER + @"NetworkCredential" + _NETWORK_CREDENTIAL_PAIR
		+ @"|\.Add(?=[^\n]*NetworkCredential)[ \t]*\(" + _GAP_IMPLICIT + _ARGUMENT_NAMED + _COMMA + @"(?:\d+|" + _ARGUMENT_NAMED + @")" + _COMMA + _QUOTE + _SCHEME_WORD + _QUOTE + _COMMA + @"(?<construct>New)[ \t]+" + _QUALIFIER + @"NetworkCredential" + _NETWORK_CREDENTIAL_PAIR
		+ @"|" + _DIRECTORY_AND_PROCESS_SINKS,
		OPTIONS, matchTimeoutMilliseconds: TIMEOUT_IN_MILLISECONDS)]
	public static partial Regex NonHttpCredentialSink();

	/// <summary>VB-019 · AuthenticationHelperDefinition, by name, on a declaration.</summary>
	[GeneratedRegex(
		@"^[ \t]*(?:(?:Public|Private|Friend|Protected|Shared|Overridable|Overrides|Async|Iterator)[ \t]+)*(?:Function|Sub)[ \t]+(?<declaration>[A-Za-z0-9_]*Basic(?-i:(?![a-z]))[A-Za-z0-9_]*(?:Auth|Authentication|Authorization|Header|Credential|Credentials)[A-Za-z0-9_]*)[ \t]*\(",
		OPTIONS | RegexOptions.Multiline, matchTimeoutMilliseconds: TIMEOUT_IN_MILLISECONDS)]
	public static partial Regex AuthenticationHelperDefinition();

	// ------------------------------------------------------------------------------------------
	// The symbol table's constant declaration and the content markers
	// ------------------------------------------------------------------------------------------

	/// <summary>The profile's constant declaration: the groups <c>name</c> and <c>value</c>.</summary>
	[GeneratedRegex(
		@"(?:^|:)[ \t]*(?:(?:(?:Public|Private|Friend|Protected|Shared|ReadOnly)[ \t]+)*(?:Const|Dim)[ \t]+\[?(?<name>[A-Za-z_][A-Za-z0-9_]*)\]?\$?(?:[ \t]+As[ \t]+String)?|\[?(?<name>[A-Za-z_][A-Za-z0-9_]*)\]?)[ \t]*=[ \t]*" + _QUOTE + @"(?<value>(?>[^" + _QUOTE + @"\n]+|" + _QUOTE + _QUOTE + @")*)" + _QUOTE + @"(?![ \t]*[&+])",
		OPTIONS | RegexOptions.Multiline, matchTimeoutMilliseconds: TIMEOUT_IN_MILLISECONDS)]
	public static partial Regex ConstantDeclaration();

	/// <summary>
	/// The generated-code header content marker, <c>AnyZone</c>: the header opens with the language's own comment token, so it lands in the comment zone,
	/// which an <c>AnyZone</c> marker must still reach. Bounded to the head of the file: blank and comment lines (at most twenty) may precede it.
	/// </summary>
	[GeneratedRegex(
		@"\A(?:[ \t]*(?:" + _COMMENT_OPENER + @"[^\n]*)?\r?\n){0,20}[ \t]*" + _COMMENT_OPENER + @"[ \t]*<auto-generated",
		OPTIONS, matchTimeoutMilliseconds: TIMEOUT_IN_MILLISECONDS)]
	public static partial Regex GeneratedHeaderContentMarker();

	/// <summary>The <c>GeneratedCode</c> attribute content marker, <c>CodeOnly</c>, with VB's angle brackets: the attribute written in a comment or a string does not demote.</summary>
	[GeneratedRegex(@"<(?:Global\.)?(?:System\.CodeDom\.Compiler\.)?GeneratedCode(?:Attribute)?[ \t]*\(", OPTIONS, matchTimeoutMilliseconds: TIMEOUT_IN_MILLISECONDS)]
	public static partial Regex GeneratedCodeAttributeContentMarker();

	/// <summary>The test-attribute content marker, <c>CodeOnly</c>, with VB's angle brackets.</summary>
	[GeneratedRegex(@"<(?:TestMethod|TestClass|Fact|Theory|Test|TestFixture)(?![A-Za-z0-9_])", OPTIONS, matchTimeoutMilliseconds: TIMEOUT_IN_MILLISECONDS)]
	public static partial Regex TestAttributeContentMarker();
}
