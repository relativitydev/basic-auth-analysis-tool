using System.Collections.Immutable;
using System.Text.RegularExpressions;

namespace MigrateAuth.Cli.Catalog.Languages;

/// <summary>
/// The expressions that exist for `TypeScript` alone: the typed constant declaration of the file symbol table (<c>EX-TS-101</c>), the typed credential object (<c>EX-TS-001</c>, `TS-001`) and the
/// call on an injected `HttpService` (<c>EX-TS-003</c>, `TS-003`). This file is another part of the `JavaScript` expression class, so it composes the shared literals, windows, blanks, credential
/// literal, user-information URL and environment declaration by name, and no `JavaScript` file is edited. Every expression is a <c>[GeneratedRegex]</c> carrying the policy match timeout.
///
/// THE SHARED CONSTANTS IT RELIES ON are listed in <see cref="ReliedOnSharedConstants"/>, which names each of them, so that renaming or removing one in a `JavaScript` file fails the `TypeScript`
/// compile and the list is the review checklist of any change to the shared vocabulary.
///
/// NO LAZY BOUNDED LOOP. The .NET 10 source generator backtracks wrongly on a lazy bounded quantifier, so the windows below are the greedy forms of the shared ones, with the same unit and the same bound.
/// A greedy bounded loop followed by its target reaches the same set of matches as the lazy one; inside a look-ahead the answer is identical. Where a window stands in the main match and an object holds two
/// matching keys, the greedy form reports the last of them where the lazy form would report the first.
/// </summary>
internal static partial class JavaScriptPatternExpressions
{
	/// <summary>
	/// The shared constants of the `JavaScript` vocabulary that the `TypeScript` expressions are built from, by value. The array is a compile-time reference: each element names a shared constant, so a rename in a
	/// `JavaScript` file fails this file's compile. Nothing reads the array at run time except the test that pins it.
	/// </summary>
	internal static ImmutableArray<string> ReliedOnSharedConstants { get; } =
	[
		_SQ_LITERAL,
		_DQ_LITERAL,
		_TEMPLATE_LITERAL,
		_ARGUMENT_UNIT,
		_SPACE,
		_BLANKS,
		_PLACEHOLDER_BODY,
		_VALUE_TERMINATOR,
		_CREDENTIAL_LITERAL,
		_ENVIRONMENT_DECLARATION,
		_GENERIC_ARGUMENTS
	];

	/// <summary>The greedy argument window of one call: at most 1,500 units, the same unit as the shared window.</summary>
	private const string _TS_ARGUMENT_WINDOW =
		@"(?:" + _ARGUMENT_UNIT + @"){0,1500}";

	/// <summary>
	/// The tail of a bare member name (a shorthand property): the name ends there, and blanks and line ends only lie before a comma or a closing brace. The credential group is never part of it.
	/// </summary>
	private const string _TS_SHORTHAND_MEMBER_TAIL =
		@"(?![\w$])" + _SPACE + @"[,}]";

	/// <summary>
	/// The user member of the `auth` object, which `TS-001` and `TS-003` both read: the key `username` (optionally quoted) and a colon, WHATEVER FOLLOWS THE COLON (a literal, a name, a member access, a call, a template),
	/// or the bare name `username` directly after the opening brace (already consumed where this look-ahead runs) or after a comma at depth one reached through the window. A name that is the value of another key
	/// (<c>label: username</c>) is preceded by a colon, a name in a nested group is consumed whole by the window, and a key written with a question mark (a type member) has no colon after the name, so none is a member.
	/// No credential is captured. Each candidate costs one forward step after the window.
	/// </summary>
	private const string _TS_OBJECT_USER_MEMBER =
		@"(?=(?:" + _SPACE + @"username" + _TS_SHORTHAND_MEMBER_TAIL + @"|" + _TS_ARGUMENT_WINDOW + @"(?:(?<![\w$.])[\x27\x22]?username[\x27\x22]?" + _SPACE + @":|," + _SPACE + @"username" + _TS_SHORTHAND_MEMBER_TAIL + @")))";

	/// <summary>
	/// The password member under the whole-literal rule, with the same key, colon and shorthand reading as the user member and no gate on the value: the group `credential` is captured only when the value is a
	/// whole literal (an optional alternative after the colon), so a name, a member access, a call or a shorthand member captures nothing and raises no secret warning.
	/// </summary>
	private const string _TS_OBJECT_PASSWORD_MEMBER =
		@"(?=(?:" + _SPACE + @"password" + _TS_SHORTHAND_MEMBER_TAIL + @"|" + _TS_ARGUMENT_WINDOW + @"(?:(?<![\w$.])[\x27\x22]?password[\x27\x22]?" + _SPACE + @":" + _SPACE + @"(?:" + _CREDENTIAL_LITERAL + @")?|," + _SPACE + @"password" + _TS_SHORTHAND_MEMBER_TAIL + @")))";

	// ------------------------------------------------------------------------------------------
	// The typed constant declaration (the symbol table). The annotation is greedy and cannot cross an equals sign or a line end, so it needs no lazy loop. It accepts a run of up to six modifiers,
	// an optional `: type` annotation of at most 200 characters, and, after the equals sign, a literal or the environment declaration of the shared vocabulary (the groups `environment` and `fallbackWord`).
	// ------------------------------------------------------------------------------------------

	private const string _EX_TS_101 =
		@"^[ \t]*(?:(?:export|declare|public|private|protected|readonly|static)[ \t]+){0,6}(?:(?:const|let|var)[ \t]+)?(?<name>[A-Za-z_$][A-Za-z0-9_$]*)[ \t]*(?::[^=\n]{1,200})?=(?!=)[ \t]*(?:(?:\x27(?<value>(?>[^\s\x27\\]*))(?>(?:[^\x27\\\n]|\\.)*)\x27|\x22(?<value>(?>[^\s\x22\\]*))(?>(?:[^\x22\\\n]|\\.)*)\x22|`(?<value>(?>[^\s`$\\]*))(?>(?:[^`$\\\n]|\$(?!\{)|\\.)*)`)(?=[ \t]*(?:[;,+]|$|//|/\*))|" + _ENVIRONMENT_DECLARATION + @"(?=[ \t]*(?:[;,]|$|//|/\*)))";

	// ------------------------------------------------------------------------------------------
	// TS-001: an object literal whose declared or asserted type names a documented request-configuration or credentials type of the client, with a username member and a password member of any value form (a literal, a name, a member access, a call; a shorthand member too). The match opens at the
	// `auth` key, reads its two members by look-ahead, and only then reads the type, by one look-behind (the annotation, the typed parameter default or the angle-bracket assertion stand before the
	// object) or one look-ahead (the `as` and `satisfies` forms stand after it). The four names are those of the client's published declaration file: AxiosRequestConfig, AxiosBasicCredentials,
	// CreateAxiosDefaults and InternalAxiosRequestConfig. The object before the `auth` key is read flat.
	// ------------------------------------------------------------------------------------------

	private const string _TS_QUALIFIER =
		@"(?:[A-Za-z_$][\w$]{0,40}\.){0,2}";

	private const string _TS_CONFIGURATION_TYPE =
		_TS_QUALIFIER + @"(?:AxiosRequestConfig|InternalAxiosRequestConfig|CreateAxiosDefaults)(?![\w$])";

	private const string _TS_CREDENTIALS_TYPE =
		_TS_QUALIFIER + @"AxiosBasicCredentials(?![\w$])";

	private const string _TS_CONFIGURATION_BEFORE_THE_AUTH_KEY =
		@"(?<=(?::" + _BLANKS + _TS_CONFIGURATION_TYPE + _BLANKS + @"=|<" + _TS_CONFIGURATION_TYPE + @">)" + _BLANKS + @"\{[^{}]{0,300}(?<![\w$.])auth" + _SPACE + @":" + _SPACE + @"\{)";

	private const string _TS_CREDENTIALS_BEFORE_THE_AUTH_OBJECT =
		@"(?<=<" + _TS_CREDENTIALS_TYPE + @">" + _BLANKS + @"\{)";

	private const string _TS_CONFIGURATION_AFTER_THE_OBJECT =
		@"(?=" + _TS_ARGUMENT_WINDOW + @"\}[^{}]{0,300}\}" + _BLANKS + @"(?:as|satisfies)[ \t]{1,8}" + _TS_CONFIGURATION_TYPE + @")";

	private const string _TS_CREDENTIALS_AFTER_THE_AUTH_OBJECT =
		@"(?=" + _TS_ARGUMENT_WINDOW + @"\}" + _BLANKS + @"(?:as|satisfies)[ \t]{1,8}" + _TS_CREDENTIALS_TYPE + @")";

	private const string _EX_TS_001 =
		@"(?<![\w$.])(?<construct>auth)(?![\w$])" + _SPACE + @":" + _SPACE + @"(?:<" + _TS_CREDENTIALS_TYPE + @">" + _BLANKS + @")?\{" + _TS_OBJECT_USER_MEMBER + _TS_OBJECT_PASSWORD_MEMBER
			+ @"(?:" + _TS_CONFIGURATION_BEFORE_THE_AUTH_KEY + @"|" + _TS_CREDENTIALS_BEFORE_THE_AUTH_OBJECT + @"|" + _TS_CONFIGURATION_AFTER_THE_OBJECT + @"|" + _TS_CREDENTIALS_AFTER_THE_AUTH_OBJECT + @")";

	// ------------------------------------------------------------------------------------------
	// TS-003: a receiver whose name contains `http` (case-insensitive), then a method of the documented `HttpService`, a generic list allowed, then the options object whose `auth` key holds a user member
	// and a password member. The match opens at the literal `http`, so the name that contains it is not read backwards. The eleven methods are those of the class in the wrapped-client package at
	// 12.0.1 (there is no `options` method); the receiver rule is a heuristic.
	// ------------------------------------------------------------------------------------------

	private const string _EX_TS_003 =
		@"(?i:http)[\w$]{0,40}\.(?:postForm|putForm|patchForm|post|put|patch|get|delete|head|request|query)(?![\w$])" + _GENERIC_ARGUMENTS + _SPACE + @"\(" + _TS_ARGUMENT_WINDOW + @"\{" + _TS_ARGUMENT_WINDOW + @"(?<![\w$.])(?<construct>auth)(?![\w$])" + _SPACE + @":" + _SPACE + @"\{"
			+ _TS_OBJECT_USER_MEMBER + _TS_OBJECT_PASSWORD_MEMBER;

	/// <summary>EX-TS-101: the constant declaration of the `TypeScript` file symbol table, with the typed forms and the environment alternative.</summary>
	[GeneratedRegex(_EX_TS_101, _LINE_OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex TypeScriptConstantDeclaration();

	/// <summary>EX-TS-001: the expression of TS-001, TypedBasicCredentialAnnotation.</summary>
	[GeneratedRegex(_EX_TS_001, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex TypedCredentialObject();

	/// <summary>EX-TS-003: the expression of TS-003, NestHttpServiceAuthObject.</summary>
	[GeneratedRegex(_EX_TS_003, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex InjectedHttpServiceAuthObject();
}
