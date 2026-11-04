using System.Collections.Frozen;
using System.Collections.Immutable;

using MigrateAuth.Cli.Findings;
using MigrateAuth.Cli.Reporting;

namespace MigrateAuth.Cli.Catalog.Languages;

/// <summary>One edit that turns a `JavaScript` text into its `TypeScript` form: the text to find, which must occur exactly once, and what replaces it.</summary>
/// <param name="Name">What the edit does, for the test that reports it.</param>
/// <param name="Find">The text of the `JavaScript` constant the edit stands on.</param>
/// <param name="Replacement">The text that stands in its place.</param>
internal sealed record TemplateSubstitution(string Name, string Find, string Replacement);

/// <summary>
/// The recommendation text that `TypeScript` writes for itself: the typed blocks of the token acquisition, the two framework shells, the override of the one `JavaScript` class whose use site differs, the two classes
/// that exist for `TypeScript` alone, and the two generic fallback texts. Everything else is the `JavaScript` text read, never copied or split: the `JavaScript` constants are the source of every sentence of a
/// token-acquisition part, and each edit that makes one a `TypeScript` text is a <see cref="TemplateSubstitution"/> that must find its text exactly once, so that a change to a `JavaScript` constant that moves the text
/// an edit stands on fails the build of the templates instead of silently leaving a block unreplaced.
///
/// WHAT `TypeScript` SETTLES IS WORDING AND TYPING ONLY: the three application types and their preconditions are those of `JavaScript`, the code is written in erasable `TypeScript` (types that Node.js strips without
/// transforming), an interface describes each token response, and an assertion on a parsed response validates nothing at run time. The browser never holds a client secret or a RelativityOne token: the browser
/// blocks hold only a navigation to the page's own origin and calls to it. The Angular and NestJS shells are thin callers of the erasable functions above them; every framework name in a shell is unchecked.
///
/// A SEPARATE FILE BECAUSE <c>TypeScriptAnalyzer</c> IS <c>file</c>-SCOPED: the text lives in this <c>internal</c> holder and the analyzer's provider reads it by the same identifiers.
/// </summary>
internal static class TypeScriptRecommendationTemplates
{
	private const string _JAVASCRIPT_FENCE_OPEN = "```javascript\n";
	private const string _TYPESCRIPT_FENCE_OPEN = "```typescript\n";
	private const string _FENCE_CLOSE = "\n```";
	private const string _PARAGRAPH_SEPARATOR = "\n\n";

	/// <summary>The question every template opens its target form with: the engine cannot see which host the code calls. The same sentence as the `JavaScript` templates open with.</summary>
	internal const string DESTINATION_QUESTION =
		"**First, the destination:** if this Relativity instance is not RelativityOne — including a Relativity Server instance, whose own documentation governs — this finding is informational and the rest of this template does not apply.";

	/// <summary>The first line of every verification note.</summary>
	internal const string TOKEN_LIFETIME =
		"**A token has a finite lifetime.**";

	/// <summary>The rotation statement of the classes that expose a credential. The same text as the `JavaScript` templates carry.</summary>
	internal const string ROTATION_STATEMENT =
		"> Treat this credential as compromised. Rotate it, and assume the repository history has published it. Completing the migration does not close this exposure; rotation does.";

	/// <summary>The rotation statement for a credential that browser code carries. The same text as the `JavaScript` templates carry.</summary>
	internal const string BROWSER_ROTATION =
		"**If this code runs in a browser, the credential has been public for as long as the code has shipped.** The exposure and the rotation are stated here and the tool does not escalate it:\n\n"
		+ ROTATION_STATEMENT;

	/// <summary>The sentence the `TypeScript` token acquisition adds after a block that parses a token response.</summary>
	internal const string TYPING_SENTENCE =
		"**Typing.** An interface describes each response in these blocks, and an `as` assertion on a parsed response validates nothing at run time: a real component checks the shape it needs before it keeps a token.";

	/// <summary>The block that acquires and caches a Client Credentials token in a Node.js process that acts as itself, by the built-in <c>fetch</c>, with the secret read by a function that throws a named error.</summary>
	internal const string CLIENT_CREDENTIALS_NODE_PROCESS_BLOCK =
		"const IDENTITY_TOKEN_URL = \"https://HOST/Relativity/Identity/connect/token\";\n"
		+ "const EXPIRY_MARGIN_SECONDS = 60;\n"
		+ "const MILLISECONDS_PER_SECOND = 1000;\n"
		+ "\n"
		+ "interface CachedToken {\n"
		+ "\tvalue: string;\n"
		+ "\texpiresAtMs: number;\n"
		+ "}\n"
		+ "\n"
		+ "interface ClientCredentialsResponse {\n"
		+ "\taccess_token: string;\n"
		+ "\texpires_in: number;\n"
		+ "}\n"
		+ "\n"
		+ "let cachedToken: CachedToken | null = null;\n"
		+ "\n"
		+ "function requireEnvironmentValue(name: string): string {\n"
		+ "\tconst value = process.env[name];\n"
		+ "\tif (value === undefined || value === \"\") {\n"
		+ "\t\tthrow new Error(`Environment variable ${name} is not set`);\n"
		+ "\t}\n"
		+ "\treturn value;\n"
		+ "}\n"
		+ "\n"
		+ "export async function getAccessToken(): Promise<string> {\n"
		+ "\tconst now = Date.now();\n"
		+ "\tif (cachedToken === null || cachedToken.expiresAtMs - now < EXPIRY_MARGIN_SECONDS * MILLISECONDS_PER_SECOND) {\n"
		+ "\t\tconst response = await fetch(IDENTITY_TOKEN_URL, {\n"
		+ "\t\t\tmethod: \"POST\",\n"
		+ "\t\t\theaders: { \"Content-Type\": \"application/x-www-form-urlencoded\" },\n"
		+ "\t\t\tbody: new URLSearchParams({\n"
		+ "\t\t\t\tgrant_type: \"client_credentials\",\n"
		+ "\t\t\t\tclient_id: requireEnvironmentValue(\"RELATIVITY_CLIENT_ID\"),\n"
		+ "\t\t\t\tclient_secret: requireEnvironmentValue(\"RELATIVITY_CLIENT_SECRET\"),\n"
		+ "\t\t\t}),\n"
		+ "\t\t});\n"
		+ "\t\tconst payload = (await response.json()) as ClientCredentialsResponse;\n"
		+ "\t\tcachedToken = {\n"
		+ "\t\t\tvalue: payload.access_token,\n"
		+ "\t\t\texpiresAtMs: now + payload.expires_in * MILLISECONDS_PER_SECOND,\n"
		+ "\t\t};\n"
		+ "\t}\n"
		+ "\treturn cachedToken.value;\n"
		+ "}";

	/// <summary>The browser variant of Authorization Code: a navigation to a route on the page's own origin and a typed call to it. It holds no secret and no token.</summary>
	internal const string AUTHORIZATION_CODE_BROWSER_BLOCK =
		"const LOGIN_ROUTE = \"/auth/login\";\n"
		+ "const API_ROUTE = \"/api/documents\";\n"
		+ "\n"
		+ "interface DocumentSummary {\n"
		+ "\tid: number;\n"
		+ "\ttitle: string;\n"
		+ "}\n"
		+ "\n"
		+ "export function signIn(): void {\n"
		+ "\twindow.location.assign(LOGIN_ROUTE);\n"
		+ "}\n"
		+ "\n"
		+ "export async function loadDocuments(): Promise<DocumentSummary[]> {\n"
		+ "\tconst response = await fetch(API_ROUTE, { credentials: \"same-origin\" });\n"
		+ "\treturn (await response.json()) as DocumentSummary[];\n"
		+ "}";

	/// <summary>The thin Angular caller of the browser block: decorators and a parameter property, which type stripping does not run, around a call that holds no credential.</summary>
	internal const string ANGULAR_SHELL_BLOCK =
		"import { HttpClient } from \"@angular/common/http\";\n"
		+ "import { Injectable } from \"@angular/core\";\n"
		+ "import { Observable } from \"rxjs\";\n"
		+ "\n"
		+ "@Injectable({ providedIn: \"root\" })\n"
		+ "export class DocumentService {\n"
		+ "\tconstructor(private readonly http: HttpClient) {}\n"
		+ "\n"
		+ "\tlist(): Observable<DocumentSummary[]> {\n"
		+ "\t\treturn this.http.get<DocumentSummary[]>(API_ROUTE);\n"
		+ "\t}\n"
		+ "}";

	/// <summary>The Node.js server variant of Authorization Code: the login route builds the authorize address, the callback route checks the state and exchanges the code.</summary>
	internal const string AUTHORIZATION_CODE_NODE_SERVER_BLOCK =
		"import crypto from \"node:crypto\";\n"
		+ "\n"
		+ "const IDENTITY_BASE = \"https://HOST/Relativity/Identity/connect\";\n"
		+ "const REDIRECT_URI = \"https://app.example.test/auth/callback\";\n"
		+ "const RANDOM_BYTES = 32;\n"
		+ "\n"
		+ "interface PendingLogin {\n"
		+ "\tstate: string;\n"
		+ "\tverifier: string;\n"
		+ "}\n"
		+ "\n"
		+ "interface TokenResponse {\n"
		+ "\taccess_token: string;\n"
		+ "\texpires_in: number;\n"
		+ "\trefresh_token?: string;\n"
		+ "}\n"
		+ "\n"
		+ "const pendingBySession = new Map<string, PendingLogin>();\n"
		+ "\n"
		+ "function requireEnvironmentValue(name: string): string {\n"
		+ "\tconst value = process.env[name];\n"
		+ "\tif (value === undefined || value === \"\") {\n"
		+ "\t\tthrow new Error(`Environment variable ${name} is not set`);\n"
		+ "\t}\n"
		+ "\treturn value;\n"
		+ "}\n"
		+ "\n"
		+ "function base64Url(buffer: Buffer): string {\n"
		+ "\treturn buffer.toString(\"base64url\");\n"
		+ "}\n"
		+ "\n"
		+ "export function beginLogin(sessionId: string, wantRefreshToken: boolean): string {\n"
		+ "\tconst state = base64Url(crypto.randomBytes(RANDOM_BYTES));\n"
		+ "\tconst verifier = base64Url(crypto.randomBytes(RANDOM_BYTES));\n"
		+ "\tconst challenge = base64Url(crypto.createHash(\"sha256\").update(verifier).digest());\n"
		+ "\tpendingBySession.set(sessionId, { state, verifier });\n"
		+ "\tconst query = new URLSearchParams({\n"
		+ "\t\tresponse_type: \"code\",\n"
		+ "\t\tclient_id: requireEnvironmentValue(\"RELATIVITY_CLIENT_ID\"),\n"
		+ "\t\tredirect_uri: REDIRECT_URI,\n"
		+ "\t\tstate,\n"
		+ "\t});\n"
		+ "\tif (wantRefreshToken) {\n"
		+ "\t\tquery.set(\"code_challenge\", challenge);\n"
		+ "\t\tquery.set(\"code_challenge_method\", \"S256\");\n"
		+ "\t}\n"
		+ "\treturn `${IDENTITY_BASE}/authorize?${query}`;\n"
		+ "}\n"
		+ "\n"
		+ "export async function completeLogin(\n"
		+ "\tsessionId: string,\n"
		+ "\treturnedCode: string,\n"
		+ "\treturnedState: string | undefined,\n"
		+ "): Promise<TokenResponse | null> {\n"
		+ "\tconst pending = pendingBySession.get(sessionId);\n"
		+ "\tpendingBySession.delete(sessionId);\n"
		+ "\tconst stateMatches = pending !== undefined\n"
		+ "\t\t&& returnedState !== undefined\n"
		+ "\t\t&& Buffer.byteLength(returnedState) === Buffer.byteLength(pending.state)\n"
		+ "\t\t&& crypto.timingSafeEqual(Buffer.from(returnedState), Buffer.from(pending.state));\n"
		+ "\tlet tokens: TokenResponse | null = null;\n"
		+ "\tif (pending !== undefined && stateMatches) {\n"
		+ "\t\tconst response = await fetch(`${IDENTITY_BASE}/token`, {\n"
		+ "\t\t\tmethod: \"POST\",\n"
		+ "\t\t\theaders: { \"Content-Type\": \"application/x-www-form-urlencoded\" },\n"
		+ "\t\t\tbody: new URLSearchParams({\n"
		+ "\t\t\t\tgrant_type: \"authorization_code\",\n"
		+ "\t\t\t\tcode: returnedCode,\n"
		+ "\t\t\t\tredirect_uri: REDIRECT_URI,\n"
		+ "\t\t\t\tclient_id: requireEnvironmentValue(\"RELATIVITY_CLIENT_ID\"),\n"
		+ "\t\t\t\tclient_secret: requireEnvironmentValue(\"RELATIVITY_CLIENT_SECRET\"),\n"
		+ "\t\t\t\tcode_verifier: pending.verifier,\n"
		+ "\t\t\t}),\n"
		+ "\t\t});\n"
		+ "\t\ttokens = response.ok ? (await response.json()) as TokenResponse : null;\n"
		+ "\t}\n"
		+ "\treturn tokens;\n"
		+ "}";

	/// <summary>The thin NestJS caller of the two server functions: decorators around calls that hold no credential. <c>SessionRequest</c> is an application-defined type, not a framework type.</summary>
	internal const string NEST_SHELL_BLOCK =
		"import { Controller, Get, Query, Redirect, Req } from \"@nestjs/common\";\n"
		+ "import { beginLogin, completeLogin } from \"./server-component\";\n"
		+ "\n"
		+ "const WANT_REFRESH_TOKEN = false;\n"
		+ "\n"
		+ "interface SessionRequest {\n"
		+ "\tsessionId: string;\n"
		+ "}\n"
		+ "\n"
		+ "@Controller(\"auth\")\n"
		+ "export class AuthController {\n"
		+ "\t@Get(\"login\")\n"
		+ "\t@Redirect()\n"
		+ "\tlogin(@Req() request: SessionRequest): { url: string } {\n"
		+ "\t\treturn { url: beginLogin(request.sessionId, WANT_REFRESH_TOKEN) };\n"
		+ "\t}\n"
		+ "\n"
		+ "\t@Get(\"callback\")\n"
		+ "\tasync callback(\n"
		+ "\t\t@Req() request: SessionRequest,\n"
		+ "\t\t@Query(\"code\") code: string,\n"
		+ "\t\t@Query(\"state\") state?: string,\n"
		+ "\t): Promise<void> {\n"
		+ "\t\tawait completeLogin(request.sessionId, code, state);\n"
		+ "\t}\n"
		+ "}";

	/// <summary>The Node.js command-line variant of Authorization Code: a loopback listener on the registered fixed port that accepts one request.</summary>
	internal const string AUTHORIZATION_CODE_NODE_CLI_BLOCK =
		"import http from \"node:http\";\n"
		+ "\n"
		+ "const LOOPBACK_HOST = \"127.0.0.1\";\n"
		+ "const REGISTERED_PORT = 8123;\n"
		+ "const CALLBACK_PATH = \"/callback\";\n"
		+ "const BASE_FOR_PARSING = `http://${LOOPBACK_HOST}:${REGISTERED_PORT}`;\n"
		+ "const WAIT_MILLISECONDS = 120000;\n"
		+ "const STATUS_OK = 200;\n"
		+ "const STATUS_NOT_FOUND = 404;\n"
		+ "\n"
		+ "interface RedirectParameters {\n"
		+ "\tcode: string | null;\n"
		+ "\tstate: string | null;\n"
		+ "}\n"
		+ "\n"
		+ "export function waitForRedirect(): Promise<RedirectParameters> {\n"
		+ "\treturn new Promise<RedirectParameters>((resolve, reject) => {\n"
		+ "\t\tconst server = http.createServer((request, response) => {\n"
		+ "\t\t\tconst address = new URL(request.url ?? \"/\", BASE_FOR_PARSING);\n"
		+ "\t\t\tconst isCallback = address.pathname === CALLBACK_PATH;\n"
		+ "\t\t\tresponse.statusCode = isCallback ? STATUS_OK : STATUS_NOT_FOUND;\n"
		+ "\t\t\tresponse.end(isCallback ? \"You can close this window.\" : \"Not found\");\n"
		+ "\t\t\tif (isCallback) {\n"
		+ "\t\t\t\tserver.close();\n"
		+ "\t\t\t\tresolve({\n"
		+ "\t\t\t\t\tcode: address.searchParams.get(\"code\"),\n"
		+ "\t\t\t\t\tstate: address.searchParams.get(\"state\"),\n"
		+ "\t\t\t\t});\n"
		+ "\t\t\t}\n"
		+ "\t\t});\n"
		+ "\t\tserver.on(\"error\", reject);\n"
		+ "\t\tserver.listen(REGISTERED_PORT, LOOPBACK_HOST);\n"
		+ "\t\tsetTimeout(() => {\n"
		+ "\t\t\tserver.close();\n"
		+ "\t\t\treject(new Error(\"No redirect arrived\"));\n"
		+ "\t\t}, WAIT_MILLISECONDS).unref();\n"
		+ "\t});\n"
		+ "}";

	/// <summary>The paragraph that introduces the Angular shell.</summary>
	internal const string ANGULAR_SHELL_INTRODUCTION =
		"**The Angular shell.** An Angular service whose method calls the page's own origin is a thin caller of the functions above. Its decorator, its injection and its parameter property are framework forms that Node.js type stripping does not run, so the shell is syntax-reviewed only, and every Angular name in it is unchecked. The credential-bearing statements are in the erasable functions above, and the shell only calls them:";

	/// <summary>The paragraph that introduces the NestJS shell.</summary>
	internal const string NEST_SHELL_INTRODUCTION =
		"**A NestJS controller that calls the two functions is a shell.** Its decorators are framework forms that Node.js type stripping does not run, so it is syntax-reviewed only, and every NestJS name in it is unchecked; `SessionRequest` is an application-defined type, not a framework type:";

	/// <summary>The sentence of the `JavaScript` browser variant that the Angular shell is inserted before, with the sentence that opens the next paragraph.</summary>
	private const string _ANGULAR_SHELL_ANCHOR =
		"because the architecture needs no cross-origin call. **Cross-origin access";

	/// <summary>The sentence of the `JavaScript` Node.js server variant that the NestJS shell is inserted after, with the label that opens the next variant.</summary>
	private const string _NEST_SHELL_ANCHOR =
		"in a session store that is not exposed to a request.\n\n**Variant 3";

	/// <summary>The edits that make the `JavaScript` Client Credentials token acquisition a `TypeScript` text.</summary>
	internal static ImmutableArray<TemplateSubstitution> ClientCredentialsSubstitutions { get; } =
	[
		BlockSubstitution(
			"the Node.js process block",
			JavaScriptRecommendationTemplates.CLIENT_CREDENTIALS_NODE_PROCESS_BLOCK,
			CLIENT_CREDENTIALS_NODE_PROCESS_BLOCK,
			_PARAGRAPH_SEPARATOR + TYPING_SENTENCE)
	];

	/// <summary>The edits that make the `JavaScript` Authorization Code token acquisition a `TypeScript` text.</summary>
	internal static ImmutableArray<TemplateSubstitution> AuthorizationCodeSubstitutions { get; } =
	[
		BlockSubstitution(
			"the browser block",
			JavaScriptRecommendationTemplates.AUTHORIZATION_CODE_BROWSER_BLOCK,
			AUTHORIZATION_CODE_BROWSER_BLOCK,
			string.Empty),
		new TemplateSubstitution(
			"the Angular shell after the browser block",
			_ANGULAR_SHELL_ANCHOR,
			"because the architecture needs no cross-origin call.\n\n"
				+ ANGULAR_SHELL_INTRODUCTION + _PARAGRAPH_SEPARATOR
				+ _TYPESCRIPT_FENCE_OPEN + ANGULAR_SHELL_BLOCK + _FENCE_CLOSE + _PARAGRAPH_SEPARATOR
				+ "**Cross-origin access"),
		BlockSubstitution(
			"the Node.js server block",
			JavaScriptRecommendationTemplates.AUTHORIZATION_CODE_NODE_SERVER_BLOCK,
			AUTHORIZATION_CODE_NODE_SERVER_BLOCK,
			_PARAGRAPH_SEPARATOR + TYPING_SENTENCE),
		new TemplateSubstitution(
			"the NestJS shell after the Node.js server variant",
			_NEST_SHELL_ANCHOR,
			"in a session store that is not exposed to a request.\n\n"
				+ NEST_SHELL_INTRODUCTION + _PARAGRAPH_SEPARATOR
				+ _TYPESCRIPT_FENCE_OPEN + NEST_SHELL_BLOCK + _FENCE_CLOSE + _PARAGRAPH_SEPARATOR
				+ "**Variant 3"),
		BlockSubstitution(
			"the Node.js command-line block",
			JavaScriptRecommendationTemplates.AUTHORIZATION_CODE_NODE_CLI_BLOCK,
			AUTHORIZATION_CODE_NODE_CLI_BLOCK,
			string.Empty)
	];

	/// <summary>Part 2 for Client Credentials: the `JavaScript` text with its block replaced by the typed block, in two variants that each state their precondition.</summary>
	internal static string ClientCredentialsTokenAcquisition { get; } =
		Substitute(JavaScriptRecommendationTemplates.CLIENT_CREDENTIALS_TOKEN_ACQUISITION, ClientCredentialsSubstitutions);

	/// <summary>Part 2 for Authorization Code: the `JavaScript` text with its three blocks replaced by the typed blocks and the two framework shells added, the three variants each stating their precondition.</summary>
	internal static string AuthorizationCodeTokenAcquisition { get; } =
		Substitute(JavaScriptRecommendationTemplates.AUTHORIZATION_CODE_TOKEN_ACQUISITION, AuthorizationCodeSubstitutions);

	/// <summary>The `JavaScript` fence that a template of `TypeScript` does not carry.</summary>
	internal static string JavaScriptFenceOpen => _JAVASCRIPT_FENCE_OPEN;

	/// <summary>The fence every `TypeScript` code block carries.</summary>
	internal static string TypeScriptFenceOpen => _TYPESCRIPT_FENCE_OPEN;

	/// <summary>The end of a fenced block.</summary>
	internal static string FenceClose => _FENCE_CLOSE;

	/// <summary>The Angular `HttpClient` form of the header class, source form: the header built in an <c>HttpHeaders</c> object.</summary>
	internal const string ANGULAR_HEADER_SOURCE_BLOCK =
		"const headers = new HttpHeaders({ Authorization: \"Basic \" + encoded });\n"
		+ "this.http.get<Reply>(url, { headers });";

	/// <summary>The Angular `HttpClient` form of the header class, target form: a call to the application's own origin with no header.</summary>
	internal const string ANGULAR_HEADER_TARGET_BLOCK =
		"this.http.get<Reply>(API_ROUTE);";

	/// <summary>The class that holds a typed credential object: the source form.</summary>
	internal const string TS_001_SOURCE_FORM =
		"**Applies to:** browser and Node.js.\n\n"
		+ _TYPESCRIPT_FENCE_OPEN + "const config: AxiosRequestConfig = { baseURL: host, auth: { username: userName, password: secret } };" + _FENCE_CLOSE;

	/// <summary>The class that holds a typed credential object: the target form under token authentication.</summary>
	internal const string TS_001_TARGET_FORM =
		DESTINATION_QUESTION + "\n\n*Node.js:*\n\n"
		+ _TYPESCRIPT_FENCE_OPEN
		+ "const CSRF_HEADER_VALUE = \"-\";\n"
		+ "const config: AxiosRequestConfig = {\n"
		+ "\tbaseURL: host,\n"
		+ "\theaders: { Authorization: `Bearer ${accessToken}`, \"X-CSRF-Header\": CSRF_HEADER_VALUE },\n"
		+ "};"
		+ _FENCE_CLOSE + "\n\n*Browser:* the call moves behind the server-side component and the browser calls its own origin with no `Authorization` header, as above. Remove the `auth` member from the typed object, also from a configuration passed to `axios.create`.";

	/// <summary>The class that holds a typed credential object: the verification note.</summary>
	internal const string TS_001_VERIFICATION_NOTE =
		"**Verification note.** **The tool saw an object literal typed as a client configuration holding an `auth` pair; it did not see where the object is used**: it may be held and handed to a client elsewhere, and **a type name is a token the tool does not resolve**. With `axios`, `auth` overwrites a custom `Authorization` header when both are present (the documentation of the package version read for this text; other versions unchecked), so it must go entirely. The type name in the target form (`AxiosRequestConfig`) is one that the package's published declarations name, its members `baseURL` and `headers` are the option names the package documents for the version read, and the block was not type-checked. "
		+ TOKEN_LIFETIME + " **The calling identity changes.**\n\n"
		+ BROWSER_ROTATION;

	/// <summary>The class that calls an injected `HttpService` with an `auth` object: the source form.</summary>
	internal const string TS_003_SOURCE_FORM =
		"**Applies to:** Node.js, a NestJS server.\n\n"
		+ _TYPESCRIPT_FENCE_OPEN + "const reply = await firstValueFrom(this.httpService.get(url, { auth: { username: userName, password: secret } }));" + _FENCE_CLOSE;

	/// <summary>The class that calls an injected `HttpService` with an `auth` object: the target form under token authentication.</summary>
	internal const string TS_003_TARGET_FORM =
		DESTINATION_QUESTION + "\n\n"
		+ _TYPESCRIPT_FENCE_OPEN
		+ "const CSRF_HEADER_VALUE = \"-\";\n"
		+ "const reply = await firstValueFrom(\n"
		+ "\tthis.httpService.get(url, { headers: { Authorization: `Bearer ${accessToken}`, \"X-CSRF-Header\": CSRF_HEADER_VALUE } }),\n"
		+ ");"
		+ _FENCE_CLOSE + "\n\nRemove the `auth` option. **`HttpService` wraps `axios`, and its `request` and `get` signatures take the client's request configuration and return an observable** (documented for the package version read for this text; the signatures of its other methods were not read). `firstValueFrom` and the injection of the service are unchecked, and the shell is syntax-reviewed only.";

	/// <summary>The class that calls an injected `HttpService` with an `auth` object: the verification note.</summary>
	internal const string TS_003_VERIFICATION_NOTE =
		"**Verification note.** **The tool saw an `auth` pair in the options of a call on a receiver whose name contains `http`; it did not resolve the receiver's type.** "
		+ TOKEN_LIFETIME + " **The calling identity changes.**";

	/// <summary>The Angular form that the header class adds: the header built in an <c>HttpHeaders</c> object, a <c>setHeaders</c> option or an interceptor.</summary>
	internal const string ANGULAR_HEADER_SOURCE_LEAD =
		"**Applies to (Angular):** browser code in an Angular application; every Angular name below is unchecked and the block is syntax-reviewed only.";

	/// <summary>The target of the Angular form: the service calls its own origin and the interceptor that added the header is removed.</summary>
	internal const string ANGULAR_HEADER_TARGET_LEAD =
		"*Angular `HttpClient`, browser:* the call moves behind the server-side component (Authorization Code, variant 1; or the browser variant of Client Credentials when no person signs in), and the service calls **its own origin with no `Authorization` header**:";

	/// <summary>What follows the target block of the Angular form.</summary>
	internal const string ANGULAR_HEADER_TARGET_TAIL =
		"An interceptor that adds the Basic header (`req.clone({ setHeaders: { Authorization: … } })`) is removed, not rewritten to add a Bearer header: **a browser never holds a RelativityOne token**.";

	/// <summary>What the verification note of the header class says about the Angular forms, appended to the `JavaScript` note.</summary>
	internal const string ANGULAR_HEADER_VERIFICATION_NOTE =
		"**Angular.** The tool also reads an `Authorization` key with a literal `Basic` start in an `HttpHeaders` object, in a `setHeaders` option and in a `.set(` or `.append(` call. **A header value held in an identifier is not read for an object key.**";

	/// <summary>The Client Credentials generic fallback prose for a `TypeScript` finding, with the secret-handling sentence naming the Part B pointer.</summary>
	internal const string TYPESCRIPT_LANGUAGE_FALLBACK_TEXT =
		"The tool reports Basic Authentication at this location but did not recognize the construct, so the change has not been characterized and no before-and-after form is offered here. If the destination is not RelativityOne, the migration is that service's own and this finding is informational (that Relativity's identity endpoint issues no tokens for another service is unverified). If the destination is RelativityOne, Relativity's documentation recommends bearer tokens and states that Basic Authentication is no longer supported for named APIs, with no date for its removal asserted (the platform's direction beyond that is NSerio's reading, unverified). The documented destination in TypeScript is a bearer `Authorization` header sent by an HTTP client such as `fetch`, carrying a token obtained from the platform's identity endpoint with the client-credentials grant, from a server-side component or a Node.js process. The client ID and secret that obtain that token belong in the process environment or a secret store, per " + SecretManagementPointer.PART_B_NAME + ", and never in source, in a bundle or in browser code: a secret shipped to a browser is public. Beyond those facts this is a pointer and not an instruction: the migration must be designed from the code by whoever owns it, rather than applied from this text.";

	/// <summary>The Authorization Code generic fallback prose for a `TypeScript` finding.</summary>
	internal const string TYPESCRIPT_CODE_LANGUAGE_FALLBACK_TEXT =
		"The tool reports Basic Authentication at this location but did not recognize the construct, so the change has not been characterized and no before-and-after form is offered here. If the destination is not RelativityOne, the migration is that service's own and this finding is informational (that Relativity's identity endpoint issues no tokens for another service is unverified). If the destination is RelativityOne, Relativity's documentation recommends bearer tokens and states that Basic Authentication is no longer supported for named APIs, with no date for its removal asserted (the platform's direction beyond that is NSerio's reading, unverified), and this finding is assigned the Authorization Code flow, in which the code sends the person to RelativityOne to sign in, receives an authorization code at a Redirect URL registered on its OAuth2 Client, and exchanges the code for an access token that it sends as a Bearer token. Code requires a client secret, so the exchange runs on a server-side component or a Node.js process and never in browser code: for TypeScript that runs in a browser the browser navigates to a route on the application's own origin and the server-side component performs the sign-in and keeps the tokens; for a Node.js server the callback route performs it; for a command-line tool or a desktop process a loopback listener on the registered fixed port receives the code. The authorize endpoint is `<host>/Relativity/Identity/connect/authorize` and the code is exchanged at `<host>/Relativity/Identity/connect/token`; both are " + OAuth2ClientConfigurationBlocks.NSERIO_VERIFIED_LABEL + ". PKCE is required by Relativity's own documentation to obtain a refresh token, and not otherwise. The client ID and secret belong in the process environment or a secret store, per " + SecretManagementPointer.PART_B_NAME + ", never in source or in a bundle. After the change the code no longer collects or forwards the person's password. Beyond those facts this is a pointer and not an instruction: the migration must be designed from the code by whoever owns it, rather than applied from this text.";

	/// <summary>An edit that replaces a `JavaScript` block, with the fence that opens and closes it, by the typed block and what follows it.</summary>
	private static TemplateSubstitution BlockSubstitution(string name, string javaScriptBlock, string typeScriptBlock, string afterTheBlock)
	{
		return new TemplateSubstitution(
			name,
			_JAVASCRIPT_FENCE_OPEN + javaScriptBlock + _FENCE_CLOSE,
			_TYPESCRIPT_FENCE_OPEN + typeScriptBlock + _FENCE_CLOSE + afterTheBlock);
	}

	/// <summary>Applies every edit in order. Each must find its text exactly once in the text as the edits before it leave it, or the type does not load.</summary>
	internal static string Substitute(string javaScriptText, ImmutableArray<TemplateSubstitution> substitutions)
	{
		string text = javaScriptText;

		foreach (TemplateSubstitution substitution in substitutions)
		{
			int first = text.IndexOf(substitution.Find, StringComparison.Ordinal);

			if (first < 0 || text.IndexOf(substitution.Find, first + 1, StringComparison.Ordinal) >= 0)
			{
				throw new InvalidOperationException($"The `TypeScript` edit '{substitution.Name}' does not find its `JavaScript` text exactly once.");
			}

			text = string.Concat(text.AsSpan(0, first), substitution.Replacement, text.AsSpan(first + substitution.Find.Length));
		}

		return text;
	}
}

/// <summary>
/// The `TypeScript` analyzer's template provider and fallback texts. A class whose use site differs answers its own entry (the header class, with the Angular form added, and the two classes of `TypeScript`);
/// every other class answers the `JavaScript` entry read through <see cref="JavaScriptTemplateDeclarations.Provide"/> and rendered with the `typescript` fence and the token acquisition of `TypeScript`. The six
/// classes that are Low by definition, and any identifier the analyzer does not declare, answer null, which routes a Low finding to manual review and sends a High or Medium finding of an undeclared class to the
/// language's fallback text.
/// </summary>
internal static class TypeScriptTemplateDeclarations
{
	private const string _PARAGRAPH_SEPARATOR = "\n\n";

	private const string _JS_001 = "JS-001";
	private const string _JS_002 = "JS-002";
	private const string _JS_003 = "JS-003";
	private const string _JS_004 = "JS-004";
	private const string _JS_005 = "JS-005";
	private const string _JS_006 = "JS-006";
	private const string _JS_008 = "JS-008";
	private const string _JS_010 = "JS-010";
	private const string _JS_012 = "JS-012";
	private const string _JS_013 = "JS-013";
	private const string _JS_015 = "JS-015";
	private const string _JS_017 = "JS-017";
	private const string _TS_001 = "TS-001";
	private const string _TS_003 = "TS-003";

	/// <summary>The twelve inherited classes that carry a template, in the order the `JavaScript` analyzer declares them.</summary>
	private static readonly ImmutableArray<string> _inheritedTemplatedClasses =
		[_JS_001, _JS_002, _JS_003, _JS_004, _JS_005, _JS_006, _JS_008, _JS_010, _JS_012, _JS_013, _JS_015, _JS_017];

	private static readonly FrozenDictionary<string, RecommendationTemplate> _clientCredentialsTemplatesByPatternClass =
		BuildTemplates(CredentialFlow.ClientCredentials, TypeScriptRecommendationTemplates.ClientCredentialsTokenAcquisition, JavaScriptRecommendationTemplates.CLIENT_CREDENTIALS_TOKEN_ACQUISITION);

	private static readonly FrozenDictionary<string, RecommendationTemplate> _authorizationCodeTemplatesByPatternClass =
		BuildTemplates(CredentialFlow.AuthorizationCode, TypeScriptRecommendationTemplates.AuthorizationCodeTokenAcquisition, JavaScriptRecommendationTemplates.AUTHORIZATION_CODE_TOKEN_ACQUISITION);

	/// <summary>The language's own text for the generic recommendation route, per flow.</summary>
	internal static FrozenDictionary<CredentialFlow, string> FallbackTextByFlow { get; } = new Dictionary<CredentialFlow, string>
	{
		[CredentialFlow.ClientCredentials] = TypeScriptRecommendationTemplates.TYPESCRIPT_LANGUAGE_FALLBACK_TEXT,
		[CredentialFlow.AuthorizationCode] = TypeScriptRecommendationTemplates.TYPESCRIPT_CODE_LANGUAGE_FALLBACK_TEXT
	}.ToFrozenDictionary();

	/// <summary>The template of a class for a flow, or null for a Low class and for a class this analyzer does not declare.</summary>
	internal static RecommendationTemplate? Provide(string patternClassIdentifier, CredentialFlow flow)
	{
		FrozenDictionary<string, RecommendationTemplate> templatesByPatternClass =
			flow is CredentialFlow.AuthorizationCode
				? _authorizationCodeTemplatesByPatternClass
				: _clientCredentialsTemplatesByPatternClass;

		templatesByPatternClass.TryGetValue(patternClassIdentifier, out RecommendationTemplate? found);

		return found;
	}

	private static FrozenDictionary<string, RecommendationTemplate> BuildTemplates(CredentialFlow flow, string tokenAcquisition, string javaScriptTokenAcquisition)
	{
		Dictionary<string, RecommendationTemplate> templates = new(StringComparer.Ordinal);

		foreach (string patternClass in _inheritedTemplatedClasses)
		{
			RecommendationTemplate inherited = JavaScriptTemplateDeclarations.Provide(patternClass, flow)
				?? throw new InvalidOperationException($"The `JavaScript` analyzer declares no template for '{patternClass}'.");

			templates[patternClass] = patternClass == _JS_004
				? HeaderClassWithTheAngularForm(inherited, tokenAcquisition, javaScriptTokenAcquisition)
				: Retargeted(inherited, tokenAcquisition, javaScriptTokenAcquisition);
		}

		templates[_TS_001] = Template(
			TypeScriptRecommendationTemplates.TS_001_SOURCE_FORM,
			TypeScriptRecommendationTemplates.TS_001_TARGET_FORM,
			JavaScriptRecommendationTemplates.JS_001_CREDENTIAL_PROVENANCE,
			TypeScriptRecommendationTemplates.TS_001_VERIFICATION_NOTE,
			tokenAcquisition);
		templates[_TS_003] = Template(
			TypeScriptRecommendationTemplates.TS_003_SOURCE_FORM,
			TypeScriptRecommendationTemplates.TS_003_TARGET_FORM,
			JavaScriptRecommendationTemplates.JS_001_CREDENTIAL_PROVENANCE,
			TypeScriptRecommendationTemplates.TS_003_VERIFICATION_NOTE,
			tokenAcquisition);

		return templates.ToFrozenDictionary(StringComparer.Ordinal);
	}

	/// <summary>The `JavaScript` entry of a class, every `javascript` fence written `typescript` and the token acquisition of `TypeScript` after the class's own share.</summary>
	private static RecommendationTemplate Retargeted(RecommendationTemplate inherited, string tokenAcquisition, string javaScriptTokenAcquisition)
	{
		string provenanceShare = ProvenanceShareOf(inherited.CredentialProvenance, javaScriptTokenAcquisition);

		return Template(
			WithTheTypeScriptFence(inherited.GenericSourceForm),
			WithTheTypeScriptFence(inherited.TargetFormUnderTokenAuthentication),
			provenanceShare,
			WithTheTypeScriptFence(inherited.VerificationNote),
			tokenAcquisition);
	}

	/// <summary>The header class: the `JavaScript` entry for the `fetch` and `setRequestHeader` forms, with the Angular `HttpClient` form added to each of its fields.</summary>
	private static RecommendationTemplate HeaderClassWithTheAngularForm(RecommendationTemplate inherited, string tokenAcquisition, string javaScriptTokenAcquisition)
	{
		string provenanceShare = ProvenanceShareOf(inherited.CredentialProvenance, javaScriptTokenAcquisition);
		string source = $"{WithTheTypeScriptFence(inherited.GenericSourceForm)}{_PARAGRAPH_SEPARATOR}{TypeScriptRecommendationTemplates.ANGULAR_HEADER_SOURCE_LEAD}{_PARAGRAPH_SEPARATOR}{Fenced(TypeScriptRecommendationTemplates.ANGULAR_HEADER_SOURCE_BLOCK)}";
		string target = $"{WithTheTypeScriptFence(inherited.TargetFormUnderTokenAuthentication)}{_PARAGRAPH_SEPARATOR}{TypeScriptRecommendationTemplates.ANGULAR_HEADER_TARGET_LEAD}{_PARAGRAPH_SEPARATOR}{Fenced(TypeScriptRecommendationTemplates.ANGULAR_HEADER_TARGET_BLOCK)}{_PARAGRAPH_SEPARATOR}{TypeScriptRecommendationTemplates.ANGULAR_HEADER_TARGET_TAIL}";
		string note = $"{WithTheTypeScriptFence(inherited.VerificationNote)}{_PARAGRAPH_SEPARATOR}{TypeScriptRecommendationTemplates.ANGULAR_HEADER_VERIFICATION_NOTE}";

		return Template(source, target, provenanceShare, note, tokenAcquisition);
	}

	private static string Fenced(string block)
	{
		return $"{TypeScriptRecommendationTemplates.TypeScriptFenceOpen}{block}{TypeScriptRecommendationTemplates.FenceClose}";
	}

	private static string WithTheTypeScriptFence(string text)
	{
		return text.Replace(TypeScriptRecommendationTemplates.JavaScriptFenceOpen, TypeScriptRecommendationTemplates.TypeScriptFenceOpen, StringComparison.Ordinal);
	}

	/// <summary>The class's own share of a provenance: what stands before the `JavaScript` token acquisition, which every class's provenance ends with.</summary>
	private static string ProvenanceShareOf(string provenance, string javaScriptTokenAcquisition)
	{
		string suffix = $"{_PARAGRAPH_SEPARATOR}{javaScriptTokenAcquisition}";

		if (!provenance.EndsWith(suffix, StringComparison.Ordinal))
		{
			throw new InvalidOperationException("A `JavaScript` provenance does not end with its token acquisition.");
		}

		return provenance[..^suffix.Length];
	}

	/// <summary>The credential provenance is the class's own share followed by part 2 for the flow, as two paragraphs.</summary>
	private static RecommendationTemplate Template(
		string sourceForm,
		string targetForm,
		string credentialProvenanceShare,
		string verificationNote,
		string tokenAcquisition)
	{
		return new RecommendationTemplate
		{
			GenericSourceForm = sourceForm,
			TargetFormUnderTokenAuthentication = targetForm,
			CredentialProvenance = $"{credentialProvenanceShare}{_PARAGRAPH_SEPARATOR}{tokenAcquisition}",
			VerificationNote = verificationNote
		};
	}
}
