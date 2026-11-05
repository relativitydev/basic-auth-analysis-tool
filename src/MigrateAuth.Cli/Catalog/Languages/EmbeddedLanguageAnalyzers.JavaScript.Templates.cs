using System.Collections.Frozen;

using MigrateAuth.Cli.Findings;
using MigrateAuth.Cli.Reporting;

namespace MigrateAuth.Cli.Catalog.Languages;

/// <summary>
/// The recommendation text for `JavaScript`: twelve classes, both flows and both generic fallback texts. The five classes that are Low by definition (the calls that send Basic
/// only after the server challenges for it, the non-HTTP shape and the helper definition) carry no entry: they are routed to manual review, and their reason sentences are the
/// shared Low vocabulary's.
///
/// HOW A RECOMMENDATION IS COMPOSED. Part 1 is the class's four fields and is flow-neutral, so the source form, the target form and the verification note are the same text for both
/// flows. The credential provenance is the class's own share followed by part 2 for the finding's flow, held once here as <see cref="CLIENT_CREDENTIALS_TOKEN_ACQUISITION"/> and
/// <see cref="AUTHORIZATION_CODE_TOKEN_ACQUISITION"/>. Part 3, the OAuth2 Client block, is appended by the shared engine's composer and is not repeated here.
///
/// THE APPLICATION TYPE IS NOT KNOWN TO THE ENGINE, so a finding receives every variant of its flow and each variant opens with its own precondition, which lets the reader discard the
/// others by reading one sentence each: two variants for Client Credentials (a Node.js process, browser code) and three for Authorization Code (a browser with a server-side component,
/// a Node.js server, a Node.js command line or desktop shell). No browser variant places a client secret or a RelativityOne token in browser code: the browser blocks hold only a
/// navigation to the page's own origin and calls to it.
///
/// ENCODING CHOICES. Source and analysis identifiers are dropped, because the reader of a report has no document to resolve them against: a fact is labelled in words. Every secret-handling
/// sentence names <see cref="SecretManagementPointer.PART_B_NAME"/>, and the label of a fact NSerio verified but Relativity does not document is
/// <see cref="OAuth2ClientConfigurationBlocks.NSERIO_VERIFIED_LABEL"/>, both through the constants the shared engine already owns. Every block is fenced <c>javascript</c> and is indented
/// with tabs only. A template never names a Relativity fact it cannot state in words; the destination is always asked first, because the engine cannot see which host a script calls.
/// Whether RelativityOne serves cross-origin requests to browser callers is stated both ways and is not verified.
///
/// A SEPARATE FILE BECAUSE <c>JavaScriptAnalyzer</c> IS <c>file</c>-SCOPED: its type cannot be split across files, so the text lives in this <c>internal</c> holder and the analyzer's
/// template provider reads it by the same identifiers.
/// </summary>
internal static class JavaScriptRecommendationTemplates
{
	private const string _FENCE_JS_OPEN = "```javascript\n";
	private const string _FENCE_CLOSE = "\n```";

	/// <summary>The question every template opens its target form with: the engine cannot see which host the code calls.</summary>
	private const string _DESTINATION_QUESTION =
		"**First, the destination:** if this Relativity instance is not RelativityOne — including a Relativity Server instance, whose own documentation governs — this finding is informational and the rest of this template does not apply.";

	/// <summary>The first line of every verification note.</summary>
	private const string _TOKEN_LIFETIME =
		"**A token has a finite lifetime.**";

	/// <summary>The class share of the credential provenance every template carries.</summary>
	private const string _ROUTE_BY_DESTINATION =
		"**Where the credentials come from.** The text establishes that a client sends Basic, not that the destination is RelativityOne. If it is not RelativityOne, the migration is that service's own token mechanism, this finding is informational for this tool, and the reader stops here. If it is RelativityOne, the token comes from the token-acquisition part that follows, and the user name and password are replaced by the OAuth2 Client's client ID and secret, kept as " + SecretManagementPointer.PART_B_NAME + " describes, never in source, in a tracked `.env` file or in a bundle.";

	/// <summary>The rotation statement of the classes that expose a credential.</summary>
	private const string _ROTATION_STATEMENT =
		"> Treat this credential as compromised. Rotate it, and assume the repository history has published it. Completing the migration does not close this exposure; rotation does.";

	/// <summary>The rotation statement for a credential that browser code carries.</summary>
	private const string _BROWSER_ROTATION =
		"**If this code runs in a browser, the credential has been public for as long as the code has shipped.** The exposure and the rotation are stated here and the tool does not escalate it:\n\n"
		+ _ROTATION_STATEMENT;

	/// <summary>The block that acquires and caches a Client Credentials token in a Node.js process that acts as itself, by the built-in <c>fetch</c>.</summary>
	public const string CLIENT_CREDENTIALS_NODE_PROCESS_BLOCK =
		"const IDENTITY_TOKEN_URL = \"https://HOST/Relativity/Identity/connect/token\";\n"
		+ "const EXPIRY_MARGIN_SECONDS = 60;\n"
		+ "const MILLISECONDS_PER_SECOND = 1000;\n"
		+ "let cachedToken = null;\n"
		+ "\n"
		+ "async function getAccessToken() {\n"
		+ "\tconst now = Date.now();\n"
		+ "\tif (cachedToken === null || cachedToken.expiresAtMs - now < EXPIRY_MARGIN_SECONDS * MILLISECONDS_PER_SECOND) {\n"
		+ "\t\tconst response = await fetch(IDENTITY_TOKEN_URL, {\n"
		+ "\t\t\tmethod: \"POST\",\n"
		+ "\t\t\theaders: { \"Content-Type\": \"application/x-www-form-urlencoded\" },\n"
		+ "\t\t\tbody: new URLSearchParams({\n"
		+ "\t\t\t\tgrant_type: \"client_credentials\",\n"
		+ "\t\t\t\tclient_id: process.env.RELATIVITY_CLIENT_ID,\n"
		+ "\t\t\t\tclient_secret: process.env.RELATIVITY_CLIENT_SECRET,\n"
		+ "\t\t\t}),\n"
		+ "\t\t});\n"
		+ "\t\tconst payload = await response.json();\n"
		+ "\t\tcachedToken = {\n"
		+ "\t\t\tvalue: payload.access_token,\n"
		+ "\t\t\texpiresAtMs: now + payload.expires_in * MILLISECONDS_PER_SECOND,\n"
		+ "\t\t};\n"
		+ "\t}\n"
		+ "\treturn cachedToken.value;\n"
		+ "}";

	/// <summary>The browser variant of Authorization Code: a navigation to a route on the page's own origin and a call to it. It holds no secret and no token.</summary>
	public const string AUTHORIZATION_CODE_BROWSER_BLOCK =
		"const LOGIN_ROUTE = \"/auth/login\";\n"
		+ "const API_ROUTE = \"/api/documents\";\n"
		+ "\n"
		+ "function signIn() {\n"
		+ "\twindow.location.assign(LOGIN_ROUTE);\n"
		+ "}\n"
		+ "\n"
		+ "async function loadDocuments() {\n"
		+ "\tconst response = await fetch(API_ROUTE, { credentials: \"same-origin\" });\n"
		+ "\treturn response.json();\n"
		+ "}";

	/// <summary>The Node.js server variant of Authorization Code: the login route builds the authorize address, the callback route checks the state and exchanges the code.</summary>
	public const string AUTHORIZATION_CODE_NODE_SERVER_BLOCK =
		"import crypto from \"node:crypto\";\n"
		+ "\n"
		+ "const IDENTITY_BASE = \"https://HOST/Relativity/Identity/connect\";\n"
		+ "const REDIRECT_URI = \"https://app.example.test/auth/callback\";\n"
		+ "const CLIENT_ID = process.env.RELATIVITY_CLIENT_ID;\n"
		+ "const CLIENT_SECRET = process.env.RELATIVITY_CLIENT_SECRET;\n"
		+ "const RANDOM_BYTES = 32;\n"
		+ "const pendingBySession = new Map();\n"
		+ "\n"
		+ "function base64Url(buffer) {\n"
		+ "\treturn buffer.toString(\"base64url\");\n"
		+ "}\n"
		+ "\n"
		+ "function beginLogin(sessionId, wantRefreshToken) {\n"
		+ "\tconst state = base64Url(crypto.randomBytes(RANDOM_BYTES));\n"
		+ "\tconst verifier = base64Url(crypto.randomBytes(RANDOM_BYTES));\n"
		+ "\tconst challenge = base64Url(crypto.createHash(\"sha256\").update(verifier).digest());\n"
		+ "\tpendingBySession.set(sessionId, { state, verifier });\n"
		+ "\tconst query = new URLSearchParams({\n"
		+ "\t\tresponse_type: \"code\",\n"
		+ "\t\tclient_id: CLIENT_ID,\n"
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
		+ "async function completeLogin(sessionId, returnedCode, returnedState) {\n"
		+ "\tconst pending = pendingBySession.get(sessionId);\n"
		+ "\tpendingBySession.delete(sessionId);\n"
		+ "\tconst stateMatches = pending !== undefined\n"
		+ "\t\t&& returnedState !== undefined\n"
		+ "\t\t&& Buffer.byteLength(returnedState) === Buffer.byteLength(pending.state)\n"
		+ "\t\t&& crypto.timingSafeEqual(Buffer.from(returnedState), Buffer.from(pending.state));\n"
		+ "\tlet tokens = null;\n"
		+ "\tif (stateMatches) {\n"
		+ "\t\tconst response = await fetch(`${IDENTITY_BASE}/token`, {\n"
		+ "\t\t\tmethod: \"POST\",\n"
		+ "\t\t\theaders: { \"Content-Type\": \"application/x-www-form-urlencoded\" },\n"
		+ "\t\t\tbody: new URLSearchParams({\n"
		+ "\t\t\t\tgrant_type: \"authorization_code\",\n"
		+ "\t\t\t\tcode: returnedCode,\n"
		+ "\t\t\t\tredirect_uri: REDIRECT_URI,\n"
		+ "\t\t\t\tclient_id: CLIENT_ID,\n"
		+ "\t\t\t\tclient_secret: CLIENT_SECRET,\n"
		+ "\t\t\t\tcode_verifier: pending.verifier,\n"
		+ "\t\t\t}),\n"
		+ "\t\t});\n"
		+ "\t\ttokens = response.ok ? await response.json() : null;\n"
		+ "\t}\n"
		+ "\treturn tokens;\n"
		+ "}";

	/// <summary>The Node.js command-line variant of Authorization Code: a loopback listener on the registered fixed port that accepts one request.</summary>
	public const string AUTHORIZATION_CODE_NODE_CLI_BLOCK =
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
		+ "function waitForRedirect() {\n"
		+ "\treturn new Promise((resolve, reject) => {\n"
		+ "\t\tconst server = http.createServer((request, response) => {\n"
		+ "\t\t\tconst address = new URL(request.url, BASE_FOR_PARSING);\n"
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

	/// <summary>Part 2 for Client Credentials: how the token is obtained and kept, in two variants that each state their precondition. Appended to every class's credential-provenance share for a Client Credentials finding.</summary>
	public const string CLIENT_CREDENTIALS_TOKEN_ACQUISITION =
		"**Where the token comes from.** Create an OAuth2 Client with Flow Grant Type set to Client Credentials (part 3 lists its settings), then send a form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=client_credentials`, `client_id` and `client_secret`. The token endpoint is **documented** for this grant. The token is then sent as `Authorization: Bearer <token>`, with `X-CSRF-Header: -` on REST and Kepler calls. **The scope depends on what the integration needs, and no single scope is required.** Relativity's documentation shows `SystemUserInfo` and `UserInfoAccess` and no page says which is correct or what either grants; use the value the API you call documents, and confirm it. The text has two variants, by where the code runs, and each opens with the precondition it is for: read that sentence and discard the variant that does not apply.\n\n**Variant: a Node.js process acting as itself**\n\n**Precondition: this variant is for a Node.js job, service or script that does not act for a signed-in person.**\n\n"
		+ _FENCE_JS_OPEN + CLIENT_CREDENTIALS_NODE_PROCESS_BLOCK + _FENCE_CLOSE + "\n\n**Cache the token and re-acquire it on expiry — do not refresh.** Client Credentials offers no refresh tokens: **" + OAuth2ClientConfigurationBlocks.NSERIO_VERIFIED_LABEL + "** — the Allow Refresh Tokens control does not appear for this grant in the RelativityOne user interface. The recommended Access Token Lifetime is listed in part 3. The block uses the built-in `fetch` and no package. **The client secret** belongs in the process environment or a secret store, as " + SecretManagementPointer.PART_B_NAME + " describes, **never in source, in a tracked `.env` file or in a bundle**. A reset invalidates the previous secret immediately, so plan the cut-over before resetting it. **Before deploying, confirm the calling identity:** every call now runs as the OAuth2 Client's Context User, not as the user in the replaced credential, and a non-administrator Context User is NSerio's recommendation. Trusted IPs restrict where a token can be requested, not a token already issued.\n\n**Variant: browser code that does not act for a signed-in user**\n\n**Precondition: this variant is for browser code that calls RelativityOne with no person signed in to it, so no person's identity exists to preserve. If a person signs in, use Authorization Code, variant 1.**\n\nThe Relativity request moves behind a **server-side component** that holds the secret and performs the token exchange (the Node.js process variant above); the browser calls that component on its own origin. **The component must authenticate and authorize its own callers, and the OAuth2 Client's Context User must hold the minimum permissions the operation needs. Without both, the result is an unauthenticated relay into RelativityOne:** every caller reaching it acts with the Context User's full rights, which is strictly worse than the starting point. The relay returns results to the browser, **never the token** and never the secret. For browser code the credential has been public for as long as the code has shipped; the tool does not escalate it, so this text states the exposure and the rotation:\n\n"
		+ _ROTATION_STATEMENT + "\n\n**When the code acts for a signed-in user.** Client Credentials is one service identity by construction. Code that asks a person for a password and sends it is the Authorization Code case, and moving to it means a new OAuth2 Client, because the flow is fixed when a client is created. The finding's flow note says when to make that choice.";

	/// <summary>Part 2 for Authorization Code: the common part and the three variants that each state their precondition. Appended to every class's credential-provenance share for an Authorization Code finding.</summary>
	public const string AUTHORIZATION_CODE_TOKEN_ACQUISITION =
		"**Common to all three variants.** The text has a common part and **three variants by application type**, each opening with the precondition it is for: a browser with a server-side component, a Node.js server, and a Node.js command-line tool or desktop shell. Read the precondition of each and discard the variants that do not apply.\n\n**Sign in.** Replace the code that collected the person's credentials with a redirect to the authorize endpoint `<host>/Relativity/Identity/connect/authorize` (**" + OAuth2ClientConfigurationBlocks.NSERIO_VERIFIED_LABEL + "**), which leads to RelativityOne's login page. The request carries the standard authorization-code parameters — `client_id`, `response_type=code`, the registered `redirect_uri`, `state` checked on return, and `code_challenge` with `code_challenge_method=S256` when PKCE is used; these are standard OAuth and Relativity documents none of them.\n\n**PKCE.** PKCE is required by Relativity's own documentation to obtain a refresh token and not otherwise: a Code request that does not ask for a refresh token works without it, **" + OAuth2ClientConfigurationBlocks.NSERIO_VERIFIED_LABEL + "**. NSerio recommends `S256` (NSerio recommendation) and does not claim that Relativity accepts any particular method. `state` is generated, stored against the session and compared on return; `nonce` is not used, and Relativity does not document whether it enforces either.\n\n**The client secret is required.** Code does not work without a client secret: tested and rejected, **" + OAuth2ClientConfigurationBlocks.NSERIO_VERIFIED_LABEL + "**, which confirms Relativity's own statement. **Any secret shipped to a browser is public**, and bundling, minifying or obfuscating changes nothing, so the exchange runs where a secret can be kept.\n\n**Exchange the code.** A form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=authorization_code`, the code, the same `redirect_uri`, the client ID and the secret, plus the `code_verifier` when PKCE is used (**" + OAuth2ClientConfigurationBlocks.NSERIO_VERIFIED_LABEL + "**).\n\n**Redirect URL.** A server-side application registers **one exact HTTPS URL per environment, no wildcards**; the matching rules are not documented and no claim is made about them. A plain-HTTP loopback Redirect URL `http://127.0.0.1:<port>/<path>` on a **fixed registered port** is accepted, **" + OAuth2ClientConfigurationBlocks.NSERIO_VERIFIED_LABEL + "**, and is the desktop shape.\n\n**On expiry.** With Allow Refresh Tokens enabled and PKCE used, the code holds a refresh token and sends `grant_type=refresh_token` in place of the code exchange: *inferred* from the standard OAuth shape, not tested. Relativity does not document a refresh token's lifetime, rotation or revocation, nor what disabling the client does to tokens already issued. **A refresh token is a secret.** **Single sign-on:** sign-in happens on RelativityOne's own page, so the instance's single sign-on and two-factor rules apply unchanged (*inferred*). **The code stops collecting the password:** after this change nothing receives, stores or forwards the person's user name and password, and the code holds only tokens. Every call now runs as the person who signed in, so **the calling identity changes**. **Scope:** no scope set is prescribed for Code; `openid RelativityRequestOrigin UserInfo UserInfoAccess AuthenticationID RelativityWeb` works, **" + OAuth2ClientConfigurationBlocks.NSERIO_VERIFIED_LABEL + "**, as working and not as minimal.\n\n**Variant 1 — Browser with a server-side component**\n\n**Precondition: this variant is for code that runs in a web browser and signs a person in to RelativityOne. If your code runs in Node.js, use variant 2 or variant 3.**\n\nA browser cannot hold the secret the Code grant requires, so **the browser never calls RelativityOne**: it navigates to a route on **the application's own origin**, and a **server-side component** (variant 2's code) creates `state`, redirects to the authorize endpoint, receives the code at its registered HTTPS Redirect URL, exchanges it, keeps the tokens and gives the browser only its own session. **The browser holds a session identifier with that component and nothing from RelativityOne.** The authorize step is a **top-level navigation** (a full-window redirect, not a `fetch`).\n\n"
		+ _FENCE_JS_OPEN + AUTHORIZATION_CODE_BROWSER_BLOCK + _FENCE_CLOSE + "\n\nBoth calls go to the page's own origin. `credentials: \"include\"` is a cookie policy and is not recommended here, because the architecture needs no cross-origin call. **Cross-origin access, both ways, both not verified:** NSerio has not run a preflight against a live instance. *If RelativityOne serves CORS to browser callers*, the legacy browser Basic call could have worked from another origin, and a browser holding a Bearer token could in principle call it directly; **this text still does not recommend that**, because the only grant that yields a token for a person needs the secret the browser cannot hold. *If it does not*, the legacy call worked only from the same origin or through a proxy the client already runs. **The recommended architecture holds under either answer.** **The component keeps the tokens on the server**, never in browser storage, which any script on the page can read (a browser behaviour, unchecked); the recommended session cookie is `HttpOnly`, `Secure` and `SameSite` (NSerio recommendation; unchecked).\n\n**Variant 2 — Node.js server**\n\n**Precondition: this variant is for code that runs in Node.js as a web server and acts for people who sign in to it. It is not for browser code (variant 1) or a command-line tool (variant 3).**\n\nStandard server-side Code flow. A login route builds the authorize address with `state` (and the PKCE verifier where a refresh token is wanted), stores them against the visitor's session and redirects; RelativityOne redirects to the **callback route**, registered as an exact HTTPS URL; the callback checks `state` and exchanges the code with the secret. This is the component variant 1 relies on.\n\n"
		+ _FENCE_JS_OPEN + AUTHORIZATION_CODE_NODE_SERVER_BLOCK + _FENCE_CLOSE + "\n\n`state` is consumed once and compared on return; a request with no pending entry or a different `state` gets no exchange. **The scope is not prescribed** (the sketch sends none). **Where the secret travels in the token request** (the body, as sketched, or HTTP Basic client authentication) is **not verified**. The sketch does not show the HTTP server, the session cookie or a session store that survives more than one process. **Tokens stay on the server**, in a session store that is not exposed to a request.\n\n**Variant 3 — Node.js command line or desktop**\n\n**Precondition: this variant is for code that runs on a person's own machine, as a command-line tool or a desktop shell, and signs that person in. It is not for a web server (variant 2) or browser code (variant 1).**\n\nThe process starts a listener from the built-in `node:http` module, bound to **`127.0.0.1` only**, on the **fixed registered port**, for **one request**; opens the system browser on the authorize address **and always prints the address too**, so a machine with no browser, an SSH session or a failed launch has a manual route; reads `code` and `state` from the one request, closes the listener, checks `state`, and exchanges the code as variant 2's `completeLogin` does.\n\n"
		+ _FENCE_JS_OPEN + AUTHORIZATION_CODE_NODE_CLI_BLOCK + _FENCE_CLOSE + "\n\n**Rules for the listener:** bind to loopback only; accept one request; **check `state`** (another local process can connect to a fixed, advertised port); close the listener after the request; and handle a busy port (a second server on a port in use raises `EADDRINUSE`, so fall back to the printed address). **Opening the system browser:** Node.js core has no function for it; run the platform's own launcher with a fixed program and an argument array, never a shell string built from the address (unchecked on every system). **Where tokens are kept:** a file with restricted permissions or the operating system's keychain; on Windows the POSIX mode argument of Node.js did not restrict a file when it was tried (*checked by NSerio locally* on Node.js v24.11.1), so a restricted file there needs an access-control change by other means, and on Linux and macOS it is unchecked. **The client secret of a distributed tool is extractable:** a command-line tool or desktop shell shipped to people carries its client secret in a form anyone who holds the distribution can extract, and it protects nothing against the person running it. **A known issue:** Relativity documents no recommendation for a distributed client; this design is what NSerio verified works. **Compensating guidance** (NSerio recommendation, subject to your own security policy): Trusted IPs where the users' outbound addresses are known and stable (checked at token issuance; applicability to Code *inferred*); the recommended short Access Token Lifetime; refresh tokens only when needed; and **one OAuth2 Client per distribution**, so a leaked secret can be disabled and reset alone.";

	/// <summary>The Client Credentials generic fallback prose for a `JavaScript` finding, with the secret-handling sentence naming the Part B pointer.</summary>
	public const string JAVASCRIPT_LANGUAGE_FALLBACK_TEXT =
		"The tool reports Basic Authentication at this location but did not recognize the construct, so the change has not been characterized and no before-and-after form is offered here. If the destination is not RelativityOne, the migration is that service's own and this finding is informational (that Relativity's identity endpoint issues no tokens for another service is unverified). If the destination is RelativityOne, Relativity's documentation recommends bearer tokens and states that Basic Authentication is no longer supported for named APIs, with no date for its removal asserted (the platform's direction beyond that is NSerio's reading, unverified). The documented destination in JavaScript is a bearer `Authorization` header sent by an HTTP client such as `fetch`, carrying a token obtained from the platform's identity endpoint with the client-credentials grant, from a server-side component or a Node.js process. The client ID and secret that obtain that token belong in the process environment or a secret store, per " + SecretManagementPointer.PART_B_NAME + ", and never in source, in a bundle or in browser code: a secret shipped to a browser is public. Beyond those facts this is a pointer and not an instruction: the migration must be designed from the code by whoever owns it, rather than applied from this text.";

	/// <summary>The Authorization Code generic fallback prose for a `JavaScript` finding.</summary>
	public const string JAVASCRIPT_CODE_LANGUAGE_FALLBACK_TEXT =
		"The tool reports Basic Authentication at this location but did not recognize the construct, so the change has not been characterized and no before-and-after form is offered here. If the destination is not RelativityOne, the migration is that service's own and this finding is informational (that Relativity's identity endpoint issues no tokens for another service is unverified). If the destination is RelativityOne, Relativity's documentation recommends bearer tokens and states that Basic Authentication is no longer supported for named APIs, with no date for its removal asserted (the platform's direction beyond that is NSerio's reading, unverified), and this finding is assigned the Authorization Code flow, in which the code sends the person to RelativityOne to sign in, receives an authorization code at a Redirect URL registered on its OAuth2 Client, and exchanges the code for an access token that it sends as a Bearer token. Code requires a client secret, so the exchange runs on a server-side component or a Node.js process and never in browser code: for code that runs in a browser the browser navigates to a route on the application's own origin and the server-side component performs the sign-in and keeps the tokens; for a Node.js server the callback route performs it; for a command-line tool or a desktop process a loopback listener on the registered fixed port receives the code. The authorize endpoint is `<host>/Relativity/Identity/connect/authorize` and the code is exchanged at `<host>/Relativity/Identity/connect/token`; both are " + OAuth2ClientConfigurationBlocks.NSERIO_VERIFIED_LABEL + ". PKCE is required by Relativity's own documentation to obtain a refresh token, and not otherwise. The client ID and secret belong in the process environment or a secret store, per " + SecretManagementPointer.PART_B_NAME + ", never in source or in a bundle. After the change the code no longer collects or forwards the person's password. Beyond those facts this is a pointer and not an instruction: the migration must be designed from the code by whoever owns it, rather than applied from this text.";

	/// <summary>JS-001 generic source form.</summary>
	public const string JS_001_SOURCE_FORM =
		"**Applies to:** browser and Node.js; `axios` (both), `request` (Node.js).\n\n"
		+ _FENCE_JS_OPEN + "await axios.get(url, { auth: { username: userName, password: secret } });" + _FENCE_CLOSE;

	/// <summary>JS-001 target form under token authentication.</summary>
	public const string JS_001_TARGET_FORM =
		_DESTINATION_QUESTION + "\n\n*Node.js:*\n\n"
		+ _FENCE_JS_OPEN + "const CSRF_HEADER_VALUE = \"-\";\nawait axios.get(url, { headers: { Authorization: `Bearer ${accessToken}`, \"X-CSRF-Header\": CSRF_HEADER_VALUE } });" + _FENCE_CLOSE + "\n\n*Browser:* the call moves behind the server-side component (Authorization Code, variant 1; or the browser variant of Client Credentials when no person signs in), and the browser calls its own origin with no `Authorization` header:\n\n"
		+ _FENCE_JS_OPEN + "await fetch(API_ROUTE, { credentials: \"same-origin\" });" + _FENCE_CLOSE + "\n\nRemove the `auth` member, also from `axios.create` and from `axios.defaults`, and from a `request` options object. **With `axios`, `auth` overwrites a custom `Authorization` header when both are present** (the documentation of the package version read for this text; other versions unchecked), so it must go entirely.";

	/// <summary>JS-001 class share of the credential provenance.</summary>
	public const string JS_001_CREDENTIAL_PROVENANCE =
		_ROUTE_BY_DESTINATION;

	/// <summary>JS-001 verification note.</summary>
	public const string JS_001_VERIFICATION_NOTE =
		"**Verification note.** " + _TOKEN_LIFETIME + " **The calling identity changes**; the token-acquisition part says to whom. **Other uses may not appear as findings**: an options object held in a variable and handed to a client elsewhere, and an alias of the client, are not read.\n\n"
		+ _BROWSER_ROTATION;

	/// <summary>JS-002 generic source form.</summary>
	public const string JS_002_SOURCE_FORM =
		"**Applies to:** browser and Node.js; `superagent` (both), `request` (Node.js).\n\n"
		+ _FENCE_JS_OPEN + "await superagent.get(url).auth(userName, secret);" + _FENCE_CLOSE;

	/// <summary>JS-002 target form under token authentication.</summary>
	public const string JS_002_TARGET_FORM =
		_DESTINATION_QUESTION + "\n\n*Node.js:*\n\n"
		+ _FENCE_JS_OPEN + "const CSRF_HEADER_VALUE = \"-\";\nawait superagent.get(url).set(\"Authorization\", `Bearer ${accessToken}`).set(\"X-CSRF-Header\", CSRF_HEADER_VALUE);" + _FENCE_CLOSE + "\n\n(`superagent` also offers `.auth(token, { type: \"bearer\" })`, documented for the package version read for this text; the fourth `auth` argument of `request` is its bearer form; other versions unchecked.) *Browser:* the call moves behind the server-side component (Authorization Code, variant 1; or the browser variant of Client Credentials when no person signs in), and the browser calls its own origin with no `Authorization` header:\n\n"
		+ _FENCE_JS_OPEN + "await fetch(API_ROUTE, { credentials: \"same-origin\" });" + _FENCE_CLOSE;

	/// <summary>JS-002 class share of the credential provenance.</summary>
	public const string JS_002_CREDENTIAL_PROVENANCE =
		_ROUTE_BY_DESTINATION;

	/// <summary>JS-002 verification note.</summary>
	public const string JS_002_VERIFICATION_NOTE =
		"**Verification note.** " + _TOKEN_LIFETIME + " **The calling identity changes.** **The scheme changes from `Basic` to `Bearer`**: a Bearer header is not read by this tool again.\n\n"
		+ _BROWSER_ROTATION;

	/// <summary>JS-003 generic source form.</summary>
	public const string JS_003_SOURCE_FORM =
		"**Applies to:** Node.js (the userinfo is sent); browser (`fetch` throws).\n\n"
		+ _FENCE_JS_OPEN + "http.get(\"https://exampleUser:exampleSecret@host.example/path\", handler);" + _FENCE_CLOSE;

	/// <summary>JS-003 target form under token authentication.</summary>
	public const string JS_003_TARGET_FORM =
		_DESTINATION_QUESTION + "\n\n"
		+ _FENCE_JS_OPEN + "const CSRF_HEADER_VALUE = \"-\";\nhttps.get(\"https://host.example/path\", { headers: { Authorization: `Bearer ${accessToken}`, \"X-CSRF-Header\": CSRF_HEADER_VALUE } }, handler);" + _FENCE_CLOSE + "\n\nRemove the userinfo from the URL. **Where the consumer is `fetch`, this call cannot have authenticated: it throws a `TypeError`** (*checked by NSerio locally* on Node.js v24.11.1: \"Request cannot be constructed from a URL that includes credentials\"), so this finding is a bug report as much as a migration item; do not look for a working Basic call to migrate.";

	/// <summary>JS-003 class share of the credential provenance.</summary>
	public const string JS_003_CREDENTIAL_PROVENANCE =
		_ROUTE_BY_DESTINATION;

	/// <summary>JS-003 verification note.</summary>
	public const string JS_003_VERIFICATION_NOTE =
		"**Verification note.** " + _TOKEN_LIFETIME + " **The calling identity changes.** What each client does with userinfo in a URL differs: `got`, `node-fetch` and the `request` function of `undici` are unchecked. **A credential in a URL is exposed whatever you do next:**\n\n"
		+ _ROTATION_STATEMENT;

	/// <summary>JS-004 generic source form.</summary>
	public const string JS_004_SOURCE_FORM =
		"**Applies to:** browser and Node.js.\n\n"
		+ _FENCE_JS_OPEN + "await fetch(url, { headers: { Authorization: `Basic ${encoded}` } });\nxhr.setRequestHeader(\"Authorization\", \"Basic \" + encoded);" + _FENCE_CLOSE;

	/// <summary>JS-004 target form under token authentication.</summary>
	public const string JS_004_TARGET_FORM =
		_DESTINATION_QUESTION + "\n\n*Node.js:*\n\n"
		+ _FENCE_JS_OPEN + "const CSRF_HEADER_VALUE = \"-\";\nawait fetch(url, { headers: { Authorization: `Bearer ${accessToken}`, \"X-CSRF-Header\": CSRF_HEADER_VALUE } });" + _FENCE_CLOSE + "\n\n*Browser:* the call moves behind the server-side component and the browser calls its own origin with no `Authorization` header, as above. Remove the code that encoded the user name and password.";

	/// <summary>JS-004 class share of the credential provenance.</summary>
	public const string JS_004_CREDENTIAL_PROVENANCE =
		_ROUTE_BY_DESTINATION;

	/// <summary>JS-004 verification note.</summary>
	public const string JS_004_VERIFICATION_NOTE =
		"**Verification note.** " + _TOKEN_LIFETIME + " **The calling identity changes.** **The scheme word changes from `Basic` to `Bearer`.** **A header set by a function call, a hook or an interceptor is not read.**\n\n"
		+ _BROWSER_ROTATION;

	/// <summary>JS-005 generic source form.</summary>
	public const string JS_005_SOURCE_FORM =
		"**Applies to:** browser and Node.js.\n\n"
		+ _FENCE_JS_OPEN + "const token = btoa(userName + \":\" + secret);\nconst header = \"Basic \" + token;" + _FENCE_CLOSE;

	/// <summary>JS-005 target form under token authentication.</summary>
	public const string JS_005_TARGET_FORM =
		_DESTINATION_QUESTION + " **If the encoded pair is not a credential, this finding is a false positive and nothing changes.**\n\nRemove the pair, the encoding and the variable, and send the token: `Authorization: Bearer ${accessToken}` with `X-CSRF-Header: -`. In a browser the call moves behind the server-side component.";

	/// <summary>JS-005 class share of the credential provenance.</summary>
	public const string JS_005_CREDENTIAL_PROVENANCE =
		_ROUTE_BY_DESTINATION;

	/// <summary>JS-005 verification note.</summary>
	public const string JS_005_VERIFICATION_NOTE =
		"**Verification note.** **The tool saw a colon pair encoded with `btoa` and the word `Basic` or `Authorization` within a statement's distance**: follow the encoded value to every place it is sent and change those uses too. " + _TOKEN_LIFETIME + "\n\n"
		+ _BROWSER_ROTATION;

	/// <summary>JS-006 generic source form.</summary>
	public const string JS_006_SOURCE_FORM =
		"**Applies to:** Node.js; a browser bundle that polyfills `Buffer`.\n\n"
		+ _FENCE_JS_OPEN + "const token = Buffer.from(`${userName}:${secret}`).toString(\"base64\");\nconst header = \"Basic \" + token;" + _FENCE_CLOSE;

	/// <summary>JS-006 target form under token authentication.</summary>
	public const string JS_006_TARGET_FORM =
		_DESTINATION_QUESTION + " **If the encoded pair is not a credential, this finding is a false positive and nothing changes.**\n\nRemove the pair and the encoding (including a legacy `new Buffer(…)`), and send the token: `Authorization: Bearer ${accessToken}` with `X-CSRF-Header: -`.";

	/// <summary>JS-006 class share of the credential provenance.</summary>
	public const string JS_006_CREDENTIAL_PROVENANCE =
		_ROUTE_BY_DESTINATION;

	/// <summary>JS-006 verification note.</summary>
	public const string JS_006_VERIFICATION_NOTE =
		"**Verification note.** **The tool read the colon pair and a following `.toString(\"base64\")`** (a `base64url` value is not Basic and is not read). " + _TOKEN_LIFETIME + "\n\n"
		+ _BROWSER_ROTATION;

	/// <summary>JS-008 generic source form.</summary>
	public const string JS_008_SOURCE_FORM =
		"**Applies to:** Node.js (`http` and `https`).\n\n"
		+ _FENCE_JS_OPEN + "http.request({ host: hostName, path: \"/path\", auth: `${userName}:${secret}` }, handler);" + _FENCE_CLOSE;

	/// <summary>JS-008 target form under token authentication.</summary>
	public const string JS_008_TARGET_FORM =
		_DESTINATION_QUESTION + "\n\n"
		+ _FENCE_JS_OPEN + "const CSRF_HEADER_VALUE = \"-\";\nhttps.request({ host: hostName, path: \"/path\", headers: { Authorization: `Bearer ${accessToken}`, \"X-CSRF-Header\": CSRF_HEADER_VALUE } }, handler);" + _FENCE_CLOSE + "\n\nRemove the `auth` option. Node.js documents that sending an `Authorization` header overrides the `auth` option, but remove it anyway so the credential is not left in the source.";

	/// <summary>JS-008 class share of the credential provenance.</summary>
	public const string JS_008_CREDENTIAL_PROVENANCE =
		_ROUTE_BY_DESTINATION;

	/// <summary>JS-008 verification note.</summary>
	public const string JS_008_VERIFICATION_NOTE =
		"**Verification note.** " + _TOKEN_LIFETIME + " **The calling identity changes.** An options object held in a variable and passed later is not read.";

	/// <summary>JS-010 generic source form.</summary>
	public const string JS_010_SOURCE_FORM =
		"**Applies to:** browser and Node.js.\n\n"
		+ _FENCE_JS_OPEN + "const target = new URL(base);\ntarget.username = userName;\ntarget.password = secret;" + _FENCE_CLOSE;

	/// <summary>JS-010 target form under token authentication.</summary>
	public const string JS_010_TARGET_FORM =
		_DESTINATION_QUESTION + "\n\nRemove the two assignments, and send the token: `Authorization: Bearer ${accessToken}` with `X-CSRF-Header: -`.";

	/// <summary>JS-010 class share of the credential provenance.</summary>
	public const string JS_010_CREDENTIAL_PROVENANCE =
		_ROUTE_BY_DESTINATION;

	/// <summary>JS-010 verification note.</summary>
	public const string JS_010_VERIFICATION_NOTE =
		"**Verification note.** " + _TOKEN_LIFETIME + " **If this `URL` is passed to `fetch` it throws** (*checked by NSerio locally* on Node.js v24.11.1), and to the `http` module of Node.js it authenticates; check where it is used. A credential assembled into a URL is exposed whatever you do next:\n\n"
		+ _ROTATION_STATEMENT;

	/// <summary>JS-012 generic source form.</summary>
	public const string JS_012_SOURCE_FORM =
		"**Applies to:** browser and Node.js; `superagent`.\n\n"
		+ _FENCE_JS_OPEN + "await superagent.get(url).auth(credentialPair);" + _FENCE_CLOSE;

	/// <summary>JS-012 target form under token authentication.</summary>
	public const string JS_012_TARGET_FORM =
		_DESTINATION_QUESTION + "\n\n"
		+ _FENCE_JS_OPEN + "const CSRF_HEADER_VALUE = \"-\";\nawait superagent.get(url).set(\"Authorization\", `Bearer ${accessToken}`).set(\"X-CSRF-Header\", CSRF_HEADER_VALUE);" + _FENCE_CLOSE;

	/// <summary>JS-012 class share of the credential provenance.</summary>
	public const string JS_012_CREDENTIAL_PROVENANCE =
		_ROUTE_BY_DESTINATION;

	/// <summary>JS-012 verification note.</summary>
	public const string JS_012_VERIFICATION_NOTE =
		"**Verification note.** **The tool saw a single-argument `.auth(` on a request chain, or a three-argument one whose options select `type: \"auto\"`.** The single-argument form defaults to Basic at the package version read; the `auto` form leaves the scheme to the user agent and does not fix it in the text, which is why both are reported at Medium. **A call that selects `bearer` is the migrated code and is not read.** " + _TOKEN_LIFETIME + "\n\n"
		+ _BROWSER_ROTATION;

	/// <summary>JS-013 generic source form.</summary>
	public const string JS_013_SOURCE_FORM =
		"**Applies to:** browser and Node.js.\n\n"
		+ _FENCE_JS_OPEN + "const baseUrl = \"https://exampleUser:exampleSecret@host.example/path\";\nawait axios.get(baseUrl);" + _FENCE_CLOSE;

	/// <summary>JS-013 target form under token authentication.</summary>
	public const string JS_013_TARGET_FORM =
		_DESTINATION_QUESTION + "\n\n"
		+ _FENCE_JS_OPEN + "const CSRF_HEADER_VALUE = \"-\";\nconst baseUrl = \"https://host.example/path\";\nawait axios.get(baseUrl, { headers: { Authorization: `Bearer ${accessToken}`, \"X-CSRF-Header\": CSRF_HEADER_VALUE } });" + _FENCE_CLOSE + "\n\nRemove the userinfo, and send the token wherever the URL is used: `Authorization: Bearer ${accessToken}` with `X-CSRF-Header: -`.";

	/// <summary>JS-013 class share of the credential provenance.</summary>
	public const string JS_013_CREDENTIAL_PROVENANCE =
		_ROUTE_BY_DESTINATION;

	/// <summary>JS-013 verification note.</summary>
	public const string JS_013_VERIFICATION_NOTE =
		"**Verification note.** **The tool read a URL with userinfo held in a declaration or a key; it did not see where the URL is used**: it may be a `git` remote or an unused constant, and a use elsewhere is not followed. " + _TOKEN_LIFETIME + "\n\n"
		+ _ROTATION_STATEMENT;

	/// <summary>JS-015 generic source form.</summary>
	public const string JS_015_SOURCE_FORM =
		"**Applies to:** Node.js; `got`.\n\n"
		+ _FENCE_JS_OPEN + "await got(url, { username: userName, password: secret });" + _FENCE_CLOSE;

	/// <summary>JS-015 target form under token authentication.</summary>
	public const string JS_015_TARGET_FORM =
		_DESTINATION_QUESTION + "\n\n"
		+ _FENCE_JS_OPEN + "const CSRF_HEADER_VALUE = \"-\";\nawait got(url, { headers: { Authorization: `Bearer ${accessToken}`, \"X-CSRF-Header\": CSRF_HEADER_VALUE } });" + _FENCE_CLOSE + "\n\nRemove the `username` and `password` options.";

	/// <summary>JS-015 class share of the credential provenance.</summary>
	public const string JS_015_CREDENTIAL_PROVENANCE =
		_ROUTE_BY_DESTINATION;

	/// <summary>JS-015 verification note.</summary>
	public const string JS_015_VERIFICATION_NOTE =
		"**Verification note.** " + _TOKEN_LIFETIME + " **The calling identity changes.** The two options are documented for the package version read; earlier major versions are unchecked, and whether `got` sends Basic on the first request is not stated in the page read.";

	/// <summary>JS-017 generic source form.</summary>
	public const string JS_017_SOURCE_FORM =
		"**Applies to:** browser and Node.js.\n\n"
		+ _FENCE_JS_OPEN + "const encodedHeader = \"Basic \" + encodedPair;\nawait fetch(url, { headers: { Authorization: encodedHeader } });" + _FENCE_CLOSE;

	/// <summary>JS-017 target form under token authentication.</summary>
	public const string JS_017_TARGET_FORM =
		_DESTINATION_QUESTION + "\n\n"
		+ _FENCE_JS_OPEN + "const CSRF_HEADER_VALUE = \"-\";\nawait fetch(url, { headers: { Authorization: `Bearer ${accessToken}`, \"X-CSRF-Header\": CSRF_HEADER_VALUE } });" + _FENCE_CLOSE + "\n\nRemove the variable and the code that built the pair. In a browser the call moves behind the server-side component.";

	/// <summary>JS-017 class share of the credential provenance.</summary>
	public const string JS_017_CREDENTIAL_PROVENANCE =
		_ROUTE_BY_DESTINATION;

	/// <summary>JS-017 verification note.</summary>
	public const string JS_017_VERIFICATION_NOTE =
		"**Verification note.** **The tool resolved the header's variable to a scheme of `Basic` in this file** (one declaration, before the use); a variable it cannot resolve is not read. " + _TOKEN_LIFETIME + "\n\n"
		+ _BROWSER_ROTATION;
}

/// <summary>
/// The `JavaScript` analyzer's template provider and fallback texts: twelve classes resolve to their entry under either flow; the five Low classes, and any identifier the
/// analyzer does not declare, resolve to null, which routes a Low finding to manual review and sends a High or Medium finding of an undeclared class to the language's
/// fallback text.
/// </summary>
internal static class JavaScriptTemplateDeclarations
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

	private static readonly FrozenDictionary<string, RecommendationTemplate> _clientCredentialsTemplatesByPatternClass =
		BuildTemplates(JavaScriptRecommendationTemplates.CLIENT_CREDENTIALS_TOKEN_ACQUISITION);

	private static readonly FrozenDictionary<string, RecommendationTemplate> _authorizationCodeTemplatesByPatternClass =
		BuildTemplates(JavaScriptRecommendationTemplates.AUTHORIZATION_CODE_TOKEN_ACQUISITION);

	/// <summary>The language's own text for the generic recommendation route, per flow.</summary>
	internal static FrozenDictionary<CredentialFlow, string> FallbackTextByFlow { get; } = new Dictionary<CredentialFlow, string>
	{
		[CredentialFlow.ClientCredentials] = JavaScriptRecommendationTemplates.JAVASCRIPT_LANGUAGE_FALLBACK_TEXT,
		[CredentialFlow.AuthorizationCode] = JavaScriptRecommendationTemplates.JAVASCRIPT_CODE_LANGUAGE_FALLBACK_TEXT
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

	private static FrozenDictionary<string, RecommendationTemplate> BuildTemplates(string tokenAcquisition)
	{
		Dictionary<string, RecommendationTemplate> templates = new(StringComparer.Ordinal)
		{
			[_JS_001] = Template(JavaScriptRecommendationTemplates.JS_001_SOURCE_FORM, JavaScriptRecommendationTemplates.JS_001_TARGET_FORM, JavaScriptRecommendationTemplates.JS_001_CREDENTIAL_PROVENANCE, JavaScriptRecommendationTemplates.JS_001_VERIFICATION_NOTE, tokenAcquisition),
			[_JS_002] = Template(JavaScriptRecommendationTemplates.JS_002_SOURCE_FORM, JavaScriptRecommendationTemplates.JS_002_TARGET_FORM, JavaScriptRecommendationTemplates.JS_002_CREDENTIAL_PROVENANCE, JavaScriptRecommendationTemplates.JS_002_VERIFICATION_NOTE, tokenAcquisition),
			[_JS_003] = Template(JavaScriptRecommendationTemplates.JS_003_SOURCE_FORM, JavaScriptRecommendationTemplates.JS_003_TARGET_FORM, JavaScriptRecommendationTemplates.JS_003_CREDENTIAL_PROVENANCE, JavaScriptRecommendationTemplates.JS_003_VERIFICATION_NOTE, tokenAcquisition),
			[_JS_004] = Template(JavaScriptRecommendationTemplates.JS_004_SOURCE_FORM, JavaScriptRecommendationTemplates.JS_004_TARGET_FORM, JavaScriptRecommendationTemplates.JS_004_CREDENTIAL_PROVENANCE, JavaScriptRecommendationTemplates.JS_004_VERIFICATION_NOTE, tokenAcquisition),
			[_JS_005] = Template(JavaScriptRecommendationTemplates.JS_005_SOURCE_FORM, JavaScriptRecommendationTemplates.JS_005_TARGET_FORM, JavaScriptRecommendationTemplates.JS_005_CREDENTIAL_PROVENANCE, JavaScriptRecommendationTemplates.JS_005_VERIFICATION_NOTE, tokenAcquisition),
			[_JS_006] = Template(JavaScriptRecommendationTemplates.JS_006_SOURCE_FORM, JavaScriptRecommendationTemplates.JS_006_TARGET_FORM, JavaScriptRecommendationTemplates.JS_006_CREDENTIAL_PROVENANCE, JavaScriptRecommendationTemplates.JS_006_VERIFICATION_NOTE, tokenAcquisition),
			[_JS_008] = Template(JavaScriptRecommendationTemplates.JS_008_SOURCE_FORM, JavaScriptRecommendationTemplates.JS_008_TARGET_FORM, JavaScriptRecommendationTemplates.JS_008_CREDENTIAL_PROVENANCE, JavaScriptRecommendationTemplates.JS_008_VERIFICATION_NOTE, tokenAcquisition),
			[_JS_010] = Template(JavaScriptRecommendationTemplates.JS_010_SOURCE_FORM, JavaScriptRecommendationTemplates.JS_010_TARGET_FORM, JavaScriptRecommendationTemplates.JS_010_CREDENTIAL_PROVENANCE, JavaScriptRecommendationTemplates.JS_010_VERIFICATION_NOTE, tokenAcquisition),
			[_JS_012] = Template(JavaScriptRecommendationTemplates.JS_012_SOURCE_FORM, JavaScriptRecommendationTemplates.JS_012_TARGET_FORM, JavaScriptRecommendationTemplates.JS_012_CREDENTIAL_PROVENANCE, JavaScriptRecommendationTemplates.JS_012_VERIFICATION_NOTE, tokenAcquisition),
			[_JS_013] = Template(JavaScriptRecommendationTemplates.JS_013_SOURCE_FORM, JavaScriptRecommendationTemplates.JS_013_TARGET_FORM, JavaScriptRecommendationTemplates.JS_013_CREDENTIAL_PROVENANCE, JavaScriptRecommendationTemplates.JS_013_VERIFICATION_NOTE, tokenAcquisition),
			[_JS_015] = Template(JavaScriptRecommendationTemplates.JS_015_SOURCE_FORM, JavaScriptRecommendationTemplates.JS_015_TARGET_FORM, JavaScriptRecommendationTemplates.JS_015_CREDENTIAL_PROVENANCE, JavaScriptRecommendationTemplates.JS_015_VERIFICATION_NOTE, tokenAcquisition),
			[_JS_017] = Template(JavaScriptRecommendationTemplates.JS_017_SOURCE_FORM, JavaScriptRecommendationTemplates.JS_017_TARGET_FORM, JavaScriptRecommendationTemplates.JS_017_CREDENTIAL_PROVENANCE, JavaScriptRecommendationTemplates.JS_017_VERIFICATION_NOTE, tokenAcquisition)
		};

		return templates.ToFrozenDictionary(StringComparer.Ordinal);
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
			CredentialProvenance = string.Concat(credentialProvenanceShare, _PARAGRAPH_SEPARATOR, tokenAcquisition),
			VerificationNote = verificationNote
		};
	}
}
