using System.Collections.Frozen;
using System.Collections.Immutable;

namespace MigrateAuth.Cli.Catalog.Languages;

/// <summary>
/// The declared unsupported coverage of the `TypeScript` analyzer, the statement the client's report carries when the language is enabled. It is `TypeScript`'s own list, not
/// <see cref="JavaScriptDeclaredCoverage.Build"/> passed through: every `JavaScript` entry is present, adapted or on the allow-list of `JavaScript`-only entries, and the entries that exist for `TypeScript`
/// alone are added. Written for the client reading the report: each construct names in plain words what the tool does not detect or cannot tell, and each reason says why. No internal design term, class
/// identifier or section reference belongs in either field.
///
/// THE PARITY RULE. An entry of the `JavaScript` list is identified by the start of its construct text. <see cref="JavaScriptOnlyConstructStarts"/> names those that do not apply to `TypeScript` text;
/// <see cref="AdaptedEntries"/> pairs the start of those whose wording changes with the wording `TypeScript` carries instead. Every other entry passes through unchanged. A test requires that every
/// `JavaScript` entry is passed through, adapted or on the allow-list.
/// </summary>
internal static class TypeScriptDeclaredCoverage
{
	/// <summary>The start of the construct text of each `JavaScript` entry that does not apply to `TypeScript` text.</summary>
	internal static ImmutableArray<string> JavaScriptOnlyConstructStarts { get; } =
	[
		"HTML-like comments (the opener <!-- and the closer -->) in a non-module script"
	];

	/// <summary>Each `JavaScript` entry whose wording changes for `TypeScript`, by the start of its construct text, with the entry `TypeScript` carries in its place.</summary>
	internal static ImmutableArray<(string JavaScriptConstructStart, DeclaredUnsupportedCoverage Replacement)> AdaptedEntries { get; } = BuildAdaptedEntries();

	internal static FrozenSet<DeclaredUnsupportedCoverage> Build()
	{
		IEnumerable<DeclaredUnsupportedCoverage> inherited = JavaScriptDeclaredCoverage.Build()
			.Where(entry => !IsJavaScriptOnly(entry) && !IsAdapted(entry));

		IEnumerable<DeclaredUnsupportedCoverage> entries = inherited
			.Concat(AdaptedEntries.Select(adapted => adapted.Replacement))
			.Concat(ForFilesAndAttribution())
			.Concat(ForTheLexicalReading())
			.Concat(ForGenericsAndDeclaredTypes())
			.Concat(ForClassesAndReceivers())
			.Concat(ForHeadersAndEnvironmentReads())
			.Concat(ForFlowsAndTails());

		return entries.ToFrozenSet();
	}

	private static bool IsJavaScriptOnly(DeclaredUnsupportedCoverage entry)
	{
		return JavaScriptOnlyConstructStarts.Any(start => entry.Construct.StartsWith(start, StringComparison.Ordinal));
	}

	private static bool IsAdapted(DeclaredUnsupportedCoverage entry)
	{
		return AdaptedEntries.Any(adapted => entry.Construct.StartsWith(adapted.JavaScriptConstructStart, StringComparison.Ordinal));
	}

	private static DeclaredUnsupportedCoverage Entry(string construct, string reason)
	{
		return new DeclaredUnsupportedCoverage { Construct = construct, Reason = reason };
	}

	/// <summary>The `JavaScript` entries whose wording names a `JavaScript` extension or form that `TypeScript` words differently.</summary>
	private static ImmutableArray<(string JavaScriptConstructStart, DeclaredUnsupportedCoverage Replacement)> BuildAdaptedEntries()
	{
		ImmutableArray<(string JavaScriptConstructStart, DeclaredUnsupportedCoverage Replacement)> entries =
		[
			(
				"a .js, .mjs, .cjs or .jsx file with any line over 10,000 characters",
				Entry(
					"a .ts, .tsx, .mts or .cts file with any line over 10,000 characters, which includes a minified single-line bundle",
					"this is an internal limit that keeps the cost bounded: such a file is skipped whole and counted in the report, so the loss is visible as a number; a shorter file is read")),
			(
				"JavaScript embedded in other files",
				Entry(
					"TypeScript embedded in other files: single-file components (script lang=\"ts\"), HTML templates, notebooks and Markdown",
					"the containing file is not a TypeScript file, so its text is not read as one; search such files separately")),
			(
				"a test, mock, sample or vendored file with a neutral name",
				Entry(
					"a test, mock, sample or vendored file with a neutral name",
					"no name or folder says the file is one and no text shows it, so it is reported at its full level and may be a false positive; review such findings. A TypeScript file has no minified form that is recognised by name")),
			(
				"proxy credentials: the Proxy-Authorization header",
				Entry(
					"proxy credentials: the Proxy-Authorization header, the credential member of a proxy option and user information in a proxy URL",
					"this is Basic towards a proxy and not towards the target, which is not the migration this tool serves; such text is kept out of the findings when the name is written without a type annotation or is a key. A URL with user information that is held under a name containing proxy is read as a proxy URL and is not reported, even when it points at the target service; review such a name by hand. A declaration with a type annotation, such as const proxyUrl: string = a URL with user information, is read as a service address and reported at Medium, so review a proxy address written that way")),
			(
				"generated API clients",
				Entry(
					"generated API clients (OpenAPI generators, typed SDKs)",
					"the call surface is hidden behind generated methods and no list can name every one; a generated-file banner is still read as a sign that the file is generated")),
			(
				"JSX beyond the two rules",
				Entry(
					"what .tsx inherits from the JSX limits: a backtick or /* in element text abandons the tail of the file; a template nested nine deep abandons it; an apostrophe is a recovered literal",
					"element text is not modelled; a backtick, a block-comment opener or a ninth level of template abandons the rest of the file, which is reported as not fully read, and an apostrophe damages only its own line")),
			(
				"a constant declaration that follows a brace or a semicolon on the same line",
				Entry(
					"a constant declaration that follows a brace or a semicolon on the same line, a literal continued over lines by a backslash, a template with a hole, and a declared value that carries a TypeScript tail",
					"the declaration is read one line at a time from the start of the line, which keeps the cost linear, and a name assigned from an environment read is read the same way, so a declaration of it that follows a brace or a semicolon on the same line is not counted either; a value followed by a non-null mark, a cast, a satisfies clause or an angle-bracket assertion, a value that reads another name or a call, and an annotation that holds an equals sign are not counted either; a name with one counted and one uncounted declaration can be resolved to the wrong value, which is a missed or misplaced finding")),
			(
				"a settings property used as the value of an Authorization header whose name does not end with authorization",
				Entry(
					"a property of a settings interface, type or class used as the value of an Authorization header whose name does not end with authorization (for example auth or authHeader), although it is filled from an environment value",
					"the tool joins the property to the environment value by a name that ends with authorization, which is the form it was shown to occur in; a property under another name is not read, so such a header is not reported; review headers whose value comes from a typed settings object by hand")),
			(
				"a settings property read by bracket access",
				Entry(
					"a settings property read by bracket access, by destructuring, by optional chaining or as object shorthand, a call written as the receiver, several members written on one line, a class field assigned from an environment value, a property filled from a call, from an alias of the environment object or from another file, a property filled from an environment value with a scheme word as the fallback, and a property filled from an environment variable whose name has none of the words auth, token, secret, credential, key, pass or basic",
					"the tool reads one member per line, written with a plain key or a dot (a this receiver included), and filled directly from one of the five environment forms by a variable whose name suggests a credential; it does not read a class field, an object shorthand, a call as the receiver, a call, an alias, another file or an environment variable named for something else, and a fallback that names a scheme is not reported by any rule; review such settings by hand")),
			(
				"a user name and a password joined by a colon and given to the project's own encoder function",
				Entry(
					"a user name and a password joined by a colon and given to the project's own encoder function, typed or not, in a file that never names the Basic scheme",
					"the tool recognises the standard encoders only; a function of the project's own that is not shown to encode anything, with no scheme word and no header beside it, gives the tool nothing to anchor on, so such a pair is not reported, whether or not the function carries types; review the project's own encoding helpers by hand")),
			(
				"a credential object, an options object or a header object chosen by a selector",
				Entry(
					"a credential object, an options object or a header object chosen by a selector (a TypeScript enumeration, a switch or a map) and returned from a function, with no client visible in the same statement",
					"the tool reads one expression at a time and does not follow a value from where it is chosen to where it is used, so such an object is not reported even when it carries a credential; review functions that return credentials or client options by hand"))
		];

		return entries;
	}

	/// <summary>Which files are attributed and read at all.</summary>
	private static ImmutableArray<DeclaredUnsupportedCoverage> ForFilesAndAttribution()
	{
		ImmutableArray<DeclaredUnsupportedCoverage> entries =
		[
			Entry(
				"an extensionless runner script whose first line names ts-node, tsx or deno (#!/usr/bin/env ts-node, #!/usr/bin/env tsx, #!/usr/bin/env -S deno run)",
				"the file has no TypeScript extension and the tool attributes a language by its extension or by a shebang that names a listed interpreter, which these third-party runners are not; such a script is not read, so search for it separately"),
			Entry(
				"the generated-file sentence for hand-written ambient typing in a declaration file (.d.ts, .d.mts or .d.cts)",
				"a declaration file is reported at Low for manual review because of its suffix, with the sentence written for generated files; the level is right but the sentence is wrong for a hand-written declaration, and the tool cannot tell the two apart"),
			Entry(
				"inbound Basic Authentication: a guard, middleware, interceptor or test controller that checks or reads a Basic header",
				"this is a different construct, because the tool reads what a program sends and not what it receives; such code is kept out of the findings by its shape, but a test whose expected header is written as an object literal cannot be told from a header write and is reported at Medium as test code"),
			Entry(
				"a Relativity-published TypeScript client or typings, and Relativity's extensibility surfaces (reviewapi, ConvenienceApi.RelativityHttpClient)",
				"no server-side TypeScript client or type package for Relativity REST was found, so raw HTTP is what the tool reads; these surfaces were documented only in part, run in the browser, and no class reads a call through either")
		];

		return entries;
	}

	/// <summary>The reading costs of the TypeScript lexical profile.</summary>
	private static ImmutableArray<DeclaredUnsupportedCoverage> ForTheLexicalReading()
	{
		ImmutableArray<DeclaredUnsupportedCoverage> entries =
		[
			Entry(
				"a blank before the postfix non-null mark: x !/2, x ! / 2",
				"the tool decides from the character directly before the mark whether a slash divides, so a blank makes the mark a logical not and the slash opens a regular expression; it is recovered at the line end, and when a later slash on the same line closes it the code between the two slashes is read as a string, so a construct there may be demoted or missed with no anomaly counted"),
			Entry(
				"a closing brace before the postfix non-null mark: }!/2",
				"a closing brace is not read as the end of an operand, so the slash opens a regular expression with the same consequence as a blank before the mark"),
			Entry(
				"two postfix non-null marks in a row: x!!/2",
				"the second mark follows a mark and not an operand, so the slash opens a regular expression with the same consequence as a blank before the mark; this is rare in code"),
			Entry(
				"the postfix non-null mark and a slash in .tsx element text: wow!/ directly after a word is read correctly, and the damage is with a blank before the mark (wow !/</p>)",
				"element text is not modelled; the slash opens a regular expression that the slash of the closing tag closes one character later, so the damage is one character wide and no anomaly is counted"),
			Entry(
				"the postfix non-null mark read as a regular-expression opener inside the argument window of a call: axios.get(a!/b, { auth: ... }) // .../",
				"the argument window of a call reads a slash after an exclamation mark as the opening of a regular expression, so a later slash on the same line can close it over the object the class looks for; a finding is missed, never a false one"),
			Entry(
				"an instantiation expression followed by a division: id<number> / 2",
				"a slash after a greater-than sign is read as the opening of a regular expression, which is recovered at the line end; one line is damaged and the case is rare")
		];

		return entries;
	}

	/// <summary>The generic argument lists, the type names and the typed forms the tool does not resolve.</summary>
	private static ImmutableArray<DeclaredUnsupportedCoverage> ForGenericsAndDeclaredTypes()
	{
		ImmutableArray<DeclaredUnsupportedCoverage> entries =
		[
			Entry(
				"generic nesting beyond depth 3, a level longer than 120 units, a list spanning a line feed, and a blank before the opening angle bracket, such as axios.get <T>(",
				"the list is read at a bounded depth and length and only when it follows the callee directly, so a callee with a longer, deeper, multi-line or spaced list is not read; a finding is missed, never a false one"),
			Entry(
				"a typed URL binding: const target: URL = new URL(base)",
				"the expression reads a URL binding without a type annotation, so the user name and password setters of an annotated binding are not read"),
			Entry(
				"type resolution across files, path aliases, same-file aliases and project-declared types",
				"a type name is a token in the text: the tool never resolves a type across files, reads tsconfig.json, follows an import or knows what an alias expands to, so an object typed by a name the tool does not know is not read as typed; a known name followed by an explicit generic argument list is read after an as or satisfies clause and not after an annotation, a typed default or an angle-bracket assertion"),
			Entry(
				"a typed credential object whose type name the tool does not know, such as one typed by a request, jQuery or Node.js typing",
				"only the names the tool has documentation for are read: AxiosRequestConfig, AxiosBasicCredentials, CreateAxiosDefaults and InternalAxiosRequestConfig; an object typed by another name is reported at Low where its shape matches, or not at all"),
			Entry(
				"a typed credential object whose auth key follows a nested brace in the same object",
				"the object before the auth key is read flat, because a bounded, linear reading cannot balance braces, so a headers member placed before auth hides the type; a finding is missed and the object is reported at Low where its shape matches"),
			Entry(
				"Angular and NestJS API names not read (HttpClient, HttpHeaders, interceptors, HttpTestingController, ConfigService, routing decorators; HttpService at any version other than @nestjs/axios 12.0.1)",
				"a name is read only after its documentation and version range were checked, so an unchecked name is left out rather than guessed")
		];

		return entries;
	}

	/// <summary>The receivers and modules of the injected-client classes.</summary>
	private static ImmutableArray<DeclaredUnsupportedCoverage> ForClassesAndReceivers()
	{
		ImmutableArray<DeclaredUnsupportedCoverage> entries =
		[
			Entry(
				"an injected HttpService whose receiver name does not contain http (this.client.get(..., { auth: ... }))",
				"the tool reads the name of the receiver and never its type; a call on such a receiver is missed at High and the auth object is reported at Low where its shape matches"),
			Entry(
				"an injected client as the receiver of a callee-anchored class, such as this.http.get with a credentialed URL, or a call through the service's axiosRef",
				"the callee lists of the callee-anchored classes name no injected receiver, so the finding of those classes is missed; header writes are not tied to a callee and are still read"),
			Entry(
				"an auth option in a module configuration, a property assignment on a request-configuration object inside an interceptor callback, and a call whose receiver and member are split by a line break",
				"each is a form the tool has no class for: a configuration is not a call, an assignment is not a key, and the callee lists allow no white space before the dot; a High finding is missed, and the configuration form is read at Low where its shape matches"),
			Entry(
				"the fetch-based HttpClient that replaces the axios-based module (@nestjs/http-client)",
				"the package is not encoded because its version and its documentation of Basic Authentication were not checked; an Authorization header written through it is still read, and an http-named client given an auth pair may be read at High although that package does not document the pair"),
			Entry(
				"a type-position keyed literal that cannot be told from a header value: an interface member ending in a closing brace, a comma or a line end ({ Authorization: 'Basic' }, Authorization: 'Basic', ...)",
				"nothing in the text of such a member differs from a header object written as a value, so it is reported at High in a file that is not a declaration file and at Low in one, which is a false positive; a member ending in a semicolon or a single vertical bar is refused"),
			Entry(
				"a type-position literal inside a headers: member (an interface member headers: { Authorization: 'Basic'; })",
				"the header carrier anchors on the headers key and the tail refusal does not reach it, so the member is reported at High in a file that is not a declaration file, which is a false positive, and at Low in one"),
			Entry(
				"an Angular HttpClient or another client injected under a name that contains http, given an auth object with a username and a password, which is read as a call on the NestJS HttpService",
				"the receiver is recognised by its name, because no page documents a naming convention for the injected service; such a call is reported at the High level although the client may not send Basic Authentication, so review each one before acting")
		];

		return entries;
	}

	/// <summary>The header values and environment reads the tool declines on TypeScript text.</summary>
	private static ImmutableArray<DeclaredUnsupportedCoverage> ForHeadersAndEnvironmentReads()
	{
		ImmutableArray<DeclaredUnsupportedCoverage> entries =
		[
			Entry(
				"an identifier value in an Angular Authorization object (new HttpHeaders({ Authorization: header }), setHeaders: { Authorization: header })",
				"an object key is read only when its value starts with a Basic literal, so an identifier value is not read there by design; the same name given to set or append is read"),
			Entry(
				"the value tails this.x, x! and x as T of a header value held in a name",
				"the identifier value reads a bare name and refuses a member access, a non-null mark and a cast, and only a module-level constant or a class field with a literal is counted; the name is then not resolved and nothing is reported"),
			Entry(
				"an Authorization header whose value is an environment read with a TypeScript tail, such as process.env.X!, process.env.X as string or process.env.X satisfies string, and an environment read outside the five forms",
				"the direct form and a name copied from the read refuse a tail, so the form strict code usually writes is the one that is silent; a read outside process.env, import.meta.env, Deno.env.get and Bun.env, such as a destructuring, an alias or a read through a configuration service, is not read either, so no finding is raised for them"),
			Entry(
				"a typed declaration whose name denotes a proxy: const proxyUrl: string = a URL with user information",
				"the expression reads the word of the type annotation and not the declared name, so the refusal of a name that denotes a proxy does not reach it and the address is reported at Medium, which is a false positive"),
			Entry(
				"a credential literal followed by a TypeScript tail, such as password: 'x' as string, password: 'x' satisfies Secret or password: 'x'!",
				"a literal is a credential literal only when it is the whole value, so with a tail the finding and its level are unchanged but the warning that the secret is written in the code is not raised"),
			Entry(
				"a settings property with a typed tail (a non-null mark or a type assertion) at its use, and the environment read that fills it with such a tail",
				"the tool ends the value at a non-null mark or a type assertion, so such a header is not reported; review typed settings by hand")
		];

		return entries;
	}

	/// <summary>The flow evidence and the shared-reading effects specific to TypeScript.</summary>
	private static ImmutableArray<DeclaredUnsupportedCoverage> ForFlowsAndTails()
	{
		ImmutableArray<DeclaredUnsupportedCoverage> entries =
		[
			Entry(
				"the flow misreads TypeScript's layout makes likelier: a .ts server in the script dialect read by the browser signals; a password typed in another module (a component holds the form and a service makes the call); .tsx and .mts carry no dialect; a ts-node or tsx shebang resolves to script",
				"the node dialect separates only a file that declares itself, and signals are read per file, so a server written as a plain .ts file is read as a browser file and may be assigned the interactive flow, and a form held in a component with the call in a service is assigned the default flow"),
			Entry(
				"a generic call read in JavaScript text: a tight comparison chain, or a Flow explicit type argument, with no blank before <",
				"the generic argument group is the same characters as a comparison chain, and a regular expression cannot separate them, so such text followed by the credential shape of a callee class may be reported, which is a false positive"),
			Entry(
				"a header-object finding lost on JavaScript text that carries a refused tail: a Basic literal in a value position followed by a semicolon, a single vertical bar or, in Flow text, the ending of an object type",
				"the tail refusal reads the characters after the literal and cannot tell a type member from a value with the same characters, so a finding of an Authorization key can be missed, never a false one")
		];

		return entries;
	}
}
