using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Text.RegularExpressions;

namespace MigrateAuth.Cli.Catalog.Languages;

/// <summary>
/// The expressions of the four flow signals that exist for `TypeScript` alone, two <see cref="FlowSignalKind.InteractiveUser"/> and two <see cref="FlowSignalKind.FixedCredential"/>. This file is another part of
/// the `JavaScript` expression class, so it reads the shared credential vocabulary, the windows, the blanks and the form receiver by name, and no `JavaScript` file is edited. Every expression is a
/// <c>[GeneratedRegex]</c> carrying the policy match timeout and is evaluated by the engine alone; the anchor is the optional group <c>anchor</c>, placed on the callee, the decorator name, the property or the
/// declared name the signal stands on, so a <see cref="FlowSignalZone.CodeOnly"/> signal is admitted only when that token is code.
///
/// NO LAZY BOUNDED LOOP. The .NET 10 source generator backtracks wrongly on a lazy bounded quantifier, so every window below is a greedy bounded loop or sits inside a look-ahead, where only whether a match
/// exists is asked and the greedy form answers as the lazy one does. Each expression opens at a literal and reads its receiver, where it reads one, by a look-behind placed after a look-ahead on that literal.
///
/// A LITERAL CREDENTIAL IS NOT FLOW EVIDENCE OF AN INTERACTIVE KIND. The one signal here that reads a credential written into source, <c>TypedCredentialNamedLiteralAssignment</c>, is the typed form of the inherited
/// <c>CredentialNamedLiteralAssignment</c>, which cannot read a declaration with a type annotation; it does not filter the placeholder vocabulary.
/// </summary>
internal static partial class JavaScriptPatternExpressions
{
	/// <summary>The credential-named identifier by the shared whole-token rule, written with a greedy look-ahead window.</summary>
	private const string _TS_CREDENTIAL_NAME =
		@"(?:(?=[\w$]{0,64}" + _CREDENTIAL_WORD + @")[\w$]{1,64})";

	/// <summary>What a form control's read ends with: an optional chain or non-null mark, then <c>.value</c>.</summary>
	private const string _FORM_CONTROL_READ_TAIL =
		@"\??!?\.value(?![\w$])";

	/// <summary>The field name of the password control, quoted.</summary>
	private const string _QUOTED_PASSWORD_FIELD =
		@"[\x27\x22]password[\x27\x22]";

	/// <summary>The receiver of a raw form value read held in a local name: the binding, then at most thirty further lines of at most 400 characters before the read.</summary>
	private const string _RAW_VALUE_BINDING_BEHIND =
		@"(?<=(?<![\w$.])(?:(?:const|let|var)[ \t]{1,8})?\k<receiver>" + _BLANKS + @"(?::[^=\n]{1,100})?=" + _BLANKS + @"(?:await[ \t]{1,8})?" + _FORM_RECEIVER + @"\.getRawValue" + _SPACE + @"\(" + _SPACE + @"\)[^\n]{0,400}(?:\n[^\n]{0,400}){0,30})";

	/// <summary>The password read of a body decorator's parameter, held in a name: a read on the name within 1,500 characters of the decorator.</summary>
	private const string _BODY_PARAMETER_PASSWORD_READ =
		@"(?=[\s\S]{0,1500}(?<![\w$.])\k<receiver>\??!?\.(?:" + _TS_CREDENTIAL_NAME + @")(?![\w$]))";

	/// <summary>
	/// <c>AngularFormPasswordRead</c>: a reactive-form password read with its value read, on a receiver whose name holds <c>form</c>: <c>get('password')</c> with a <c>.value</c> within 200 characters on the same
	/// statement, <c>controls.password</c> or <c>controls['password']</c> followed by <c>.value</c>, <c>value.password</c>, and the raw form value (<c>getRawValue()</c>) read for the <c>password</c> control, directly or
	/// through a name the raw value is bound to. A form definition with no read, and a read of a field that is not the password, do not match.
	/// </summary>
	private const string _EX_TS_120 =
		@"(?:\.(?<anchor>get)(?![\w$])(?=" + _SPACE + @"\(" + _SPACE + _QUOTED_PASSWORD_FIELD + _SPACE + @"\)[^;\n]{0,200}" + _FORM_CONTROL_READ_TAIL + @")(?<=" + _FORM_RECEIVER + @"\.get)"
		+ @"|\.(?<anchor>controls)(?![\w$])(?=(?:\[" + _SPACE + _QUOTED_PASSWORD_FIELD + _SPACE + @"\]|\.password(?![\w$]))" + _FORM_CONTROL_READ_TAIL + @")(?<=" + _FORM_RECEIVER + @"\.controls)"
		+ @"|\.(?<anchor>value)(?=\.password(?![\w$]))(?<=" + _FORM_RECEIVER + @"\.value)"
		+ @"|\.(?<anchor>getRawValue)(?![\w$])(?=" + _SPACE + @"\(" + _SPACE + @"\)\??!?(?:\.password(?![\w$])|\[" + _SPACE + _QUOTED_PASSWORD_FIELD + _SPACE + @"\]))(?<=" + _FORM_RECEIVER + @"\.getRawValue)"
		+ @"|\.(?<anchor>password)(?![\w$])(?<=(?<![\w$.])(?<receiver>[A-Za-z_$][\w$]{0,40})\??!?\.password)" + _RAW_VALUE_BINDING_BEHIND + @")";

	/// <summary>
	/// <c>NestBodyPasswordForwarded</c>: a request-body password read in a handler: the body decorator with a credential-named field (<c>@Body('password')</c>), the decorator with a destructured credential-named
	/// member, or the decorator's parameter name read for a credential-named member within 1,500 characters. A declaration of the member with no decorator read, a body decorator of another field and a query
	/// decorator do not match.
	/// </summary>
	private const string _EX_TS_121 =
		@"@(?<anchor>Body)(?![\w$])" + _SPACE + @"\(" + _SPACE + @"(?:[\x27\x22](?:" + _TS_CREDENTIAL_NAME + @")[\x27\x22]" + _SPACE + @"\)"
		+ @"|\)" + _SPACE + @"\{[^{}\n]{0,200}(?<![\w$])" + _TS_CREDENTIAL_NAME + @"(?![\w$])[^{}\n]{0,200}\}"
		+ @"|\)" + _SPACE + @"(?<receiver>[A-Za-z_$][\w$]{0,40})(?![\w$])" + _BODY_PARAMETER_PASSWORD_READ + @")";

	/// <summary>
	/// <c>NestConfigServiceCredentialRead</c>: <c>get</c> or <c>getOrThrow</c>, with an optional generic list, on a receiver whose name holds <c>config</c>, the first argument a quoted literal that holds a credential
	/// word. A key that names no credential and a receiver with no <c>config</c> in its name do not match.
	/// </summary>
	private const string _EX_TS_122 =
		@"(?i:config)[\w$]{0,40}\.(?<anchor>getOrThrow|get)(?![\w$])" + _GENERIC_ARGUMENTS + _SPACE + @"\(" + _SPACE + @"[\x27\x22`][^\x27\x22`\n]{0,100}" + _CREDENTIAL_WORD;

	/// <summary>
	/// <c>TypedCredentialNamedLiteralAssignment</c>: a typed declaration, class property or typed member whose name carries a credential word and whose value is a non-empty string literal or a hole-free template
	/// literal. The Fetch-mode values and the label-word endings are refused as the untyped signal refuses them, and a member with a type and no value does not match.
	/// </summary>
	private const string _EX_TS_123 =
		@"(?<![\w$])(?:(?:export|declare|public|private|protected|readonly|static|const|let|var)[ \t]{1,8}){0,4}(?<anchor>" + _TS_CREDENTIAL_NAME + @")(?<!(?:Label|Placeholder|Field|Name|Pattern|Regex|Length|Hint|Message|Error))" + _BLANKS + @":(?>[^=\n,;\x27\x22`]{1,100})=(?![=>])" + _SPACE
		+ @"(?:\x27(?!(?:include|omit|same-origin)\x27)[^\x27\n]{1,400}\x27|\x22(?!(?:include|omit|same-origin)\x22)[^\x22\n]{1,400}\x22|`(?!(?:include|omit|same-origin)`)[^`$\n]{1,400}`)";

	/// <summary>EX-TS-120: the expression of the <c>AngularFormPasswordRead</c> flow signal.</summary>
	[GeneratedRegex(_EX_TS_120, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex AngularFormPasswordReadSignal();

	/// <summary>EX-TS-121: the expression of the <c>NestBodyPasswordForwarded</c> flow signal.</summary>
	[GeneratedRegex(_EX_TS_121, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex NestBodyPasswordForwardedSignal();

	/// <summary>EX-TS-122: the expression of the <c>NestConfigServiceCredentialRead</c> flow signal.</summary>
	[GeneratedRegex(_EX_TS_122, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex NestConfigServiceCredentialReadSignal();

	/// <summary>EX-TS-123: the expression of the <c>TypedCredentialNamedLiteralAssignment</c> flow signal.</summary>
	[GeneratedRegex(_EX_TS_123, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex TypedCredentialNamedLiteralAssignmentSignal();
}

/// <summary>
/// The `TypeScript` analyzer's <see cref="CandidateSelectionAndRecommendation.FlowSignals"/>: the sixteen signals of the `JavaScript` analyzer, read on `TypeScript` text as they stand, and four of its own, twenty in
/// all, fourteen interactive and six fixed-credential. Framework markers (a component, an injectable, a controller, a module, a `node:` import) are not signals. The `TypeScript` signals keep
/// the `JavaScript` dialect exclusion: the Angular form read excludes <c>node</c>, and the two NestJS signals and the typed literal exclude nothing.
/// </summary>
internal static class TypeScriptFlowSignalDeclarations
{
	private const string _ANGULAR_FORM_PASSWORD_READ = "AngularFormPasswordRead";
	private const string _NEST_BODY_PASSWORD_FORWARDED = "NestBodyPasswordForwarded";
	private const string _NEST_CONFIG_SERVICE_CREDENTIAL_READ = "NestConfigServiceCredentialRead";
	private const string _TYPED_CREDENTIAL_NAMED_LITERAL_ASSIGNMENT = "TypedCredentialNamedLiteralAssignment";

	/// <summary>The dialect whose files are server or command-line code, and so are not read for the browser form read.</summary>
	private static readonly ImmutableArray<string> _dialectsThatAreNotABrowser = [JavaScriptDialectNames.NODE];

	/// <summary>The sixteen inherited signals, as the `JavaScript` declaration leaves them, then the four of `TypeScript`.</summary>
	internal static ImmutableArray<FlowSignal> Build()
	{
		return [.. JavaScriptFlowSignalDeclarations.Build(), .. TypeScriptOnlySignals()];
	}

	private static ImmutableArray<FlowSignal> TypeScriptOnlySignals()
	{
		ImmutableArray<FlowSignal> signals =
		[
			new FlowSignal
			{
				Name = _ANGULAR_FORM_PASSWORD_READ,
				Kind = FlowSignalKind.InteractiveUser,
				Zone = FlowSignalZone.CodeOnly,
				Expression = JavaScriptPatternExpressions.AngularFormPasswordReadSignal(),
				ExcludedDialects = _dialectsThatAreNotABrowser
			},
			Signal(_NEST_BODY_PASSWORD_FORWARDED, FlowSignalKind.InteractiveUser, JavaScriptPatternExpressions.NestBodyPasswordForwardedSignal()),
			Signal(_NEST_CONFIG_SERVICE_CREDENTIAL_READ, FlowSignalKind.FixedCredential, JavaScriptPatternExpressions.NestConfigServiceCredentialReadSignal()),
			Signal(_TYPED_CREDENTIAL_NAMED_LITERAL_ASSIGNMENT, FlowSignalKind.FixedCredential, JavaScriptPatternExpressions.TypedCredentialNamedLiteralAssignmentSignal())
		];

		return signals;
	}

	private static FlowSignal Signal(string name, FlowSignalKind kind, Regex expression)
	{
		return new FlowSignal
		{
			Name = name,
			Kind = kind,
			Zone = FlowSignalZone.CodeOnly,
			Expression = expression
		};
	}
}

/// <summary>
/// The dialect a `TypeScript` file is read in, resolved once per file: from the interpreter its first line names, then from its extension, then the default <c>script</c>. A shebang that names <c>node</c> or
/// <c>nodejs</c> and the <c>.cts</c> extension (a declaration file ending in <c>.d.cts</c> resolves by its final extension) resolve to <c>node</c>; a shebang that names <c>ts-node</c> or <c>tsx</c> resolves to
/// <c>script</c> and is declared, so that a runner shebang on a <c>.cts</c> file is read as a file that may be a browser module; a <c>.tsx</c> file and a <c>.mts</c> file get no dialect of their own.
/// A server file with no shebang is a <c>.ts</c> file in the <c>script</c> dialect, so the <c>node</c> exclusion of the browser signals cannot silence it. The names are the `JavaScript` ones.
/// </summary>
internal static class TypeScriptDialectDeclarations
{
	private const string _NODE_INTERPRETER = "node";
	private const string _NODEJS_INTERPRETER = "nodejs";
	private const string _TS_NODE_INTERPRETER = "ts-node";
	private const string _TSX_INTERPRETER = "tsx";
	private const string _COMMONJS_EXTENSION = ".cts";

	internal static FileDialects Build()
	{
		return new FileDialects
		{
			DefaultDialect = JavaScriptDialectNames.SCRIPT,
			ShebangInterpreters = new Dictionary<string, string>(StringComparer.Ordinal)
			{
				[_NODE_INTERPRETER] = JavaScriptDialectNames.NODE,
				[_NODEJS_INTERPRETER] = JavaScriptDialectNames.NODE,
				[_TS_NODE_INTERPRETER] = JavaScriptDialectNames.SCRIPT,
				[_TSX_INTERPRETER] = JavaScriptDialectNames.SCRIPT
			}.ToFrozenDictionary(StringComparer.Ordinal),
			ExtensionDialects = new Dictionary<string, string>(StringComparer.Ordinal)
			{
				[_COMMONJS_EXTENSION] = JavaScriptDialectNames.NODE
			}.ToFrozenDictionary(StringComparer.Ordinal)
		};
	}
}
