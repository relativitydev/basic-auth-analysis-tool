# Migration Suggestions for JavaScript (Part C of the Migration Documentation)

Part C tells you what to do when a Part A pattern is found in your JavaScript code. It gives the same recommendation the tool gives for that finding, because the recommendations below are produced by the same recommendation catalog the tool reports from, and are not written separately.

This document has three kinds of text:

- **Hand-written guidance.** How to confirm which OAuth 2.0 flow applies, which variant of a recommendation fits your application, and how to judge a match the tool marked Low confidence.
- **Recommendations.** One entry for each pattern class and each flow, and one generic recommendation for each flow. Each sits between a pair of `generated` markers, which are invisible when the document is rendered.
- **Pointers.** Part A ([`part-a-javascript.md`](part-a-javascript.md)) describes each pattern and how to search for it. Secret Management (Part B of the Migration Documentation, [`part-b-secret-management.md`](part-b-secret-management.md)) says where the new secrets go.

Every example uses placeholders such as `HOST`, `userName`, `secret` and `accessToken`. Replace them with your own values, and keep real secrets out of source code, out of a tracked `.env` file and out of a bundle.

The two flows are the only ones the tool recommends:

- **Client Credentials** is the default. The code sends its client ID and client secret to the token endpoint and receives an access token. No user is involved, and every call runs as the OAuth2 Client's Context User.
- **Authorization Code** is the flow that the RelativityOne OAuth2 Client form names Code. The code sends a signed-in user to RelativityOne, receives an authorization code at a registered Redirect URL, and exchanges it for a token that carries that user's permissions.

These entries assume that JavaScript reaches RelativityOne through its REST endpoints with an HTTP client such as `fetch`; that Relativity publishes no JavaScript client or browser-side surface is **not verified**. Where the entries show code, the blocks are fenced `javascript`, and a token is sent as `Authorization: Bearer`, with `X-CSRF-Header: -` on REST and Kepler calls. That header requirement is carried from the Shell migration guidance and has not been re-verified for JavaScript or against your instance. **Every entry asks first whether the destination is RelativityOne**, because the tool cannot see which host a call goes to: a Basic credential sent to another service is a real exposure, and the correct migration for that service is its own.

## Confirming the flow

Ask one question of the file that holds the match: **who supplies the credential at run time?**

- If a person types it or signs in, the integration acts for that person. Use **Authorization Code**.
- If it comes from an environment variable, a configuration setting, a secret store or a constant, and no person is present, use **Client Credentials**.

The tool answers that question by looking for signals in the **same file** as the match. Part A lists every signal by name under [Flow signals](part-a-javascript.md#flow-signals). The rules it applies are these:

1. **An interactive-user signal decides Authorization Code.** There are twelve interactive-user signals: a lookup of a password field in the page, a password read from a form, a browser prompt, a credential read from browser storage, a five-argument `XMLHttpRequest` `open`, markup with a password input, a password read from a request body in a route handler, the muted-output idioms of a terminal prompt (`_writeToOutput`, `setRawMode`, a muted `Writable`), a prompt option of type `password`, a prompt whose wording asks for a password, and a question asked on a `readline` interface. If an interactive-user signal and a fixed-credential signal are both present, the interactive one wins.
2. **A fixed-credential signal decides Client Credentials.** There are four fixed-credential signals: a credential-named variable read from `process.env`, a credential-named setting read from a configuration object, a call to a secret store, and a credential-named constant or key assigned a literal. The recommendation carries a note asking you to check, because nothing yet measures how often such a signal points the wrong way.
3. **No signal means the default.** With no signal in the file, the tool recommends Client Credentials and carries the same note. This is the expected result for most real files and not a failure of the tool, for the reason below.

**The tool reads one file at a time.** A password typed in another module, a credential read in another file, and a credential that a caller passes in are not seen. Such a match is decided by the default even when the code acts for a signed-in user. When you review a finding, look beyond its file: follow the credential back to where it comes from, and if a person supplies it, use Authorization Code even though the tool recommended Client Credentials.

**A file that does both is read as interactive.** A file that uses an environment credential when there is one and falls back to a hidden prompt when there is not holds both kinds of signal, so the interactive one wins and the tool recommends Authorization Code. If that file is in practice an unattended integration, the credential is the environment one and Client Credentials is the right flow. Decide by how the code runs, not by the fallback.

**The tool does not know what kind of application it is reading.** Browser code is user-facing by construction, a Node.js server may act for people who sign in to it, a Node.js command-line tool may ask a person at a terminal, and a job or a service acts as itself; the tool sees only the signals in the file. That is why each recommendation carries every variant of its flow, each opening with the precondition it is for, and you discard the variants that do not describe your application. See [Choosing the variant for your application](#choosing-the-variant-for-your-application). The markers of a server alone (`require(`, `module.exports`, a `node:` import, `__dirname`) are not signals, because a Node.js login handler acts for a person.

**Browser signals and Node.js files.** A file whose first line names `node` or `nodejs`, and a file with the extension `.cjs`, is read as a Node.js file, and the browser signals and the prompt-wording signal do not fire in it. A plain `.js` or `.mjs` file is not marked, because an ES module runs in both environments, so a Node.js server file that renders a page with a password field into a string is read as interactive, and a Node.js command-line file whose only evidence is the wording of a prompt is read as the default. Check the flow of such a file by hand.

**Things that are not signals.** The `credentials` member of `fetch` (`include`, `omit`, `same-origin`) and `withCredentials` are cookie policies and not a credential. A validation message that holds the word `password` is not a prompt. A literal credential written into the code is **not** evidence of either flow: it is a secret to remove, and Part B explains how.

Two more rules the tool applies, so that you can reproduce its choice by hand:

- A file whose only credential is a literal is decided by the default, unless a credential-named constant or key holds that literal, which is a fixed-credential signal.
- The tool works from the scanned code and local configuration only. It never contacts RelativityOne, so it cannot tell you what OAuth2 Clients your instance already has.

**Confirm against your instance.** If your RelativityOne instance already has an OAuth2 Client with the Code flow for this integration, use Authorization Code, whatever the tool recommended. If it has one with Client Credentials for this integration, use Client Credentials. Whether an existing OAuth2 Client can be moved from one flow to the other is not verified here; check it in your instance before assuming it can.

**Changing the default.** If most of your integrations are user-facing, you can make Authorization Code the default. Set `defaultFlow` to `AuthorizationCode` in the `.migrate-auth.json` file in your user profile folder; the accepted values are `ClientCredentials` and `AuthorizationCode`. The tool reads this setting from the user profile file only. A `defaultFlow` in the scanned repository's own file is ignored and recorded as ignored, so a repository cannot change the recommendation for the findings it holds. The setting changes only the default; a signal in the file still decides the flow.

## Choosing the variant for your application

The tool does not know whether a file runs in a browser, in a Node.js server or in a command-line tool, so the token-acquisition text of a recommendation has **variants by application type**, and each variant opens with a **precondition** sentence. Read the precondition of each variant and discard the ones that do not describe your code. The same variants appear in every class entry of the flow.

### Client Credentials: two variants

- **A Node.js process acting as itself.** For a Node.js job, service or script that does not act for a signed-in person. The process sends the form-encoded `POST` to the token endpoint with its client ID and client secret (the placement of the secret in the request body and the names of the response fields are the standard OAuth shape and are not verified against RelativityOne), caches the token, and acquires a new one on expiry. Client Credentials offers no refresh tokens, so you re-acquire and do not refresh. The client ID and client secret come from the process environment or a secret store, never from source, a tracked `.env` file or a bundle.
- **Browser code that does not act for a signed-in user.** For browser code that calls RelativityOne with no person signed in to it, so no person's identity exists to preserve. If a person signs in, use Authorization Code, variant 1. The Relativity request **moves behind a server-side component** that holds the secret and performs the token exchange, and the browser calls that component on its own origin. **The component must authenticate and authorize its own callers, and the OAuth2 Client's Context User must hold the minimum permissions the operation needs. Without both, the result is an unauthenticated relay into RelativityOne:** every caller reaching it acts with the Context User's full rights, which is strictly worse than the starting point. The relay returns results to the browser, never the token and never the secret.

### Authorization Code: three variants

- **Variant 1: a browser with a server-side component.** For code that runs in a web browser and signs a person in to RelativityOne. A browser cannot hold the client secret that the Code grant requires, so **the browser never calls RelativityOne**: it navigates to a route on the application's own origin, and a server-side component creates the `state` value, redirects to the authorize endpoint, receives the code at its registered HTTPS Redirect URL, exchanges it, keeps the tokens and gives the browser only its own session. The authorize step is a top-level navigation, not a `fetch`. Whether RelativityOne serves cross-origin requests to browser callers has **not been verified**; the recommended architecture holds either way, and the recommendation states both cases. The tokens stay on the server, never in browser storage, which any script on the page can read.
- **Variant 2: a Node.js server.** For code that runs in Node.js as a web server and acts for people who sign in to it. A login route builds the authorize address with `state` (and the PKCE verifier where a refresh token is wanted), stores them against the visitor's session and redirects; the callback route, registered as one exact HTTPS URL, checks `state` and exchanges the code with the secret. This is the component that variant 1 relies on.
- **Variant 3: a Node.js command line or desktop.** For code that runs on a person's own machine and signs that person in. The process starts a listener from the built-in `node:http` module, bound to `127.0.0.1` only, on the fixed registered port, for one request; it opens the system browser on the authorize address **and always prints the address too**; it reads `code` and `state` from the one request, closes the listener, checks `state`, and exchanges the code. A client secret shipped inside a distributed command-line tool can be extracted by anyone who holds the distribution, so the recommendation adds compensating guidance. Opening the system browser is done with the platform's own launcher, and that step is unchecked on every system. Where tokens are kept, a file with restricted permissions or the operating system's keychain: on Windows the POSIX mode argument of Node.js did not restrict a file when it was tried (checked by NSerio locally on one Node.js version), so a restricted file there needs an access-control change by other means, and on Linux and macOS it is unchecked.

**Common to all of them.** No variant places a client secret or a RelativityOne token in browser code, and a secret shipped to a browser is public whatever bundling, minifying or obfuscating is applied. **For browser code the credential has been public for as long as the code has shipped.** The tool does not escalate it; the recommendation states the exposure and the rotation, and whoever leads the onboarding should raise it with the client:

> Treat this credential as compromised. Rotate it, and assume the repository history has published it. Completing the migration does not close this exposure; rotation does.

Device Code and Resource Owner are never recommended.

## Judging a Low-confidence match

A match is **Low** confidence when it is present in the text but unlikely to be live authenticating code. The tool gives a Low match no flow and no recommendation, and lists it for manual review. **A Low level is not evidence that the code does not authenticate.** It means a person has to look. The paragraphs below say how to judge each cause and when the match still needs a migration. When it does, find the live construct it belongs to, identify its class in Part A, and follow that class's entry in [Migration suggestions by pattern class](#migration-suggestions-by-pattern-class).

### Comments

The match is inside a `//` or `/* ... */` comment, so no code runs. Read the comment. If it is commented-out code, nothing needs migrating today, but delete it or migrate it before anyone restores it, because a restored line brings Basic Authentication back. If it describes code that exists elsewhere, search for that code with Part A and migrate the live code. If it only illustrates a pattern with placeholders, no migration is needed.

### Documentation strings

The match is inside text that documents or explains, such as a help message, an error message, a template literal that holds a sample, or a label. The text does not authenticate anything. Check that the behaviour it describes has been migrated, and update the wording afterwards so that the documentation stops describing Basic Authentication. The text itself needs no migration. One case is different: a command inside a string that a program runs, such as a `curl` line given to `child_process`, is not read as code and may be live. Read such a match as a command before you dismiss it.

### Test fixtures and mocks

**Test code is not Low.** A match in a file the tool recognizes as test code is reported at **Medium** at most, with the recommendation of its class, and the report marks the finding `Test code: yes` (the JSON data file sets `testCode` to `true`). The tool recognizes test code by a folder named `test`, `tests`, `__tests__`, `spec`, `e2e`, `fixture`, `fixtures` or `cypress`, by a file name that ends in `.test.` or `.spec.` followed by `js`, `mjs`, `cjs` or `jsx`, and by a file that calls `describe(` and then `it(` or `test(`. A test that uses a mock-server library is test code because of its folder, its name or its calls, and not because of the library. Judge by what the test talks to. If it feeds a mock and never reaches RelativityOne, no migration is needed, though you may want the mock to model the token flow so the test still represents the code it covers. If it is an integration test that authenticates to a real RelativityOne instance, it needs migrating with everything else, because it stops working when Basic Authentication is switched off: follow the entry of its class. A mock-server matcher that checks an incoming Basic header is inbound authentication and is not a finding; if it sits in an object literal whose `Authorization` key holds a literal `Basic` value, the tool cannot tell it from a header write and reports it at Medium in a test file, and no migration is needed for it. A credential written literally into a test is still a secret to remove.

**A mock is Low.** A match in a file the tool recognizes as a mock, fake or stub is reported at Low, with no recommendation: a folder named `__mocks__`, `mock`, `mocks`, `stub`, `stubs`, `fake` or `fakes`, or a file name that ends in `.mock.`, `.stub.` or `.fake.` followed by `js`, `mjs`, `cjs` or `jsx`. The tool recognizes a mock by its folder or file name and by nothing else, so a mock that has a neutral name is reported at the level of its class, or at Medium when it also sits in a test folder; review it as you would a test. A file that is test code and also a mock, a sample, vendored or generated is Low, and the report still says it is test code. For a Low mock, judge by what it stands in for. If nothing runs it against RelativityOne, no migration is needed.

### Sample and example code

The match sits in a folder named `sample`, `samples`, `example`, `examples`, `demo` or `demos`, in any letter case. Those are the only names the tool recognizes, and only as folders: a sample kept in a file whose name merely begins with the word is not demoted and is reported at its ordinary level, so review it as live code. Judge by who uses it. If nobody runs the sample, it needs no migration. If people copy it as a starting point, migrate it: an example that teaches Basic Authentication spreads it to new code. Use the recommendation for the class the sample matches, and prefer the flow that fits the scenario the sample shows.

### Vendored and generated files

The match sits in a folder named `vendor`, `third-party`, `third_party` or `bower_components`, which holds third-party code, or in a file produced by a tool: a name that ends in `.min.js`, `.bundle.js` or `.chunk.js`, or a first or second line, or a line within the first ten, that says the file is generated or must not be edited. **Do not edit a generated or minified file by hand:** the next build overwrites the edit. Fix the source it is built from (the module, the template or the build settings) and rebuild. For vendored code, upgrade the library to a version that supports token authentication, or replace it. A migration is needed when the file runs against RelativityOne in your environment. It is not needed when the file is never called, or when it is only reference material. A minified file with any line over 10,000 characters is skipped whole and counted in the report, so it produces no finding at all: search such a bundle's source instead.

### Deployment folders

A folder named `Deployment` does not lower a JavaScript finding: in JavaScript that folder holds code that authenticates, so a file under it is reported at its ordinary level and this cause does not arise for it. If a Low match is reported for a deployment folder all the same, read the file before you decide. A migration is needed when the file is the one that runs against RelativityOne in your application or in the deployment itself. It is not needed when the file is never run, or when it only illustrates a pattern. A credential written literally into a deployment file is still a secret to remove.

### JS-007 · AuthenticationHelperDefinition

<a id="js-007-judging"></a>

A function that builds the credential or the authentication header for the code is a helper. The tool marks its definition Low because the advice for a helper is not a mechanical code change. **The helper is the place to migrate, and it hides its callers:** the tool reports the helper's definition once, at the function, and the construct inside it at its own location; the calls to the helper do not appear as findings because they contain no visible credential. Search for the helper's name to find every caller. If the helper builds Basic Authentication for calls to RelativityOne, migrate it once, so that it obtains and caches an access token and sends a Bearer header. Decide the flow from who supplies the credential to its callers: a person means Authorization Code, and a fixed credential means Client Credentials. Then follow the entry for the construct the helper contains, which is reported at its own location. If the helper builds a credential for another service, it needs no migration.

### JS-009 · XhrOpenCredentials

<a id="js-009-judging"></a>

An `XMLHttpRequest` `open` call that is given a user name and a password as its fourth and fifth arguments, or an address with user information. The tool marks it Low because, as the web platform's specification for the request is written, the credentials given this way are sent only after the server answers with a Basic challenge, and RelativityOne does not challenge with Basic (this rests on a single RelativityOne instance observed for the earlier languages, is not documented by Relativity, and was not probed on Relativity Server), so as written the call does not send Basic to it. This reading rests on the text of the specification and has not been run in a browser, so what a given browser does is not verified. Confirm where the call points. If it points at RelativityOne, the call already fails to authenticate with that credential, and you should look for the header write or the other construct the code also uses, which is reported under its own class, and migrate that. If it points at another server that asks for Basic, the call is that service's own authentication and needs no migration to RelativityOne. A credential written literally into the call is still a secret to remove, and in browser code it has been public for as long as the code has shipped.

### JS-011 · NonHttpAuthObject

<a id="js-011-judging"></a>

An `auth` object with a user name and a password that is not given to an HTTP client the tool knows. A database driver, a mail transport and a message broker use the same shape for their own login, so the tool marks it Low: it cannot tie the object to HTTP, and it does not propose a migration. Look at what the object is passed to. If it is a connection to a database, a mail server or a broker, it needs no migration to RelativityOne, though its password is a secret that belongs in a secret store and not in source. If the object is given, through a variable or a wrapper, to an HTTP client that sends Basic to RelativityOne, the live construct is the client's own option: find that call and follow the entry of its class. A literal password in the object is still a secret to remove; the warning is kept at Low.

### JS-014 · JqueryAjaxCredentials

<a id="js-014-judging"></a>

A jQuery `ajax` or `ajaxSetup` call with `username` and `password` settings. A jQuery request goes through `XMLHttpRequest`, and the library's documentation describes the two settings as credentials for answering an authentication challenge, so the tool marks the call Low for the reason of `JS-009`: as written it sends Basic only to a server that challenges with Basic, and RelativityOne does not challenge with Basic (this rests on a single RelativityOne instance observed for the earlier languages, is not documented by Relativity, and was not probed on Relativity Server), so it does not send Basic to it. Confirm where the call points. If it points at RelativityOne, the call does not authenticate with that credential; look for the `beforeSend` function that sets a header, or the header written in the `headers` setting, which is reported under its own class, and migrate that. If it points at another server that asks for Basic, the call is that service's own authentication and needs no migration to RelativityOne. A credential written literally into the call is still a secret to remove, and it is public if the page ships it.

### JS-016 · RequestChallengeDependentAuth

<a id="js-016-judging"></a>

A `request` call whose `auth` option or method has the immediate send turned off (`sendImmediately` false). With it off, the client sends Basic only after it receives a 401 answer, and RelativityOne does not challenge with Basic (this rests on a single RelativityOne instance observed for the earlier languages, is not documented by Relativity, and was not probed on Relativity Server), so as written the call does not send Basic to it. Confirm where the call points. If it points at RelativityOne, the call does not authenticate with that credential, and you should look for the header write or the `auth` option with the immediate send on that the code also uses, which is reported under its own class, and migrate that. If it points at another server that asks for Basic, the call is that service's own authentication and needs no migration to RelativityOne. The `request` package has been deprecated since 2020, so a repository that still uses it should plan to replace it, and the replacement is a good place to apply the Bearer form. A credential written literally into the call is still a secret to remove.

### JS-018 · AuthorizationHeaderValueFromEnvironment

<a id="js-018-judging"></a>

An `Authorization` header whose whole value is read from the process environment (`process.env`, `import.meta.env`, `Deno.env.get` or `Bun.env`), alone, with a `||` or `??` fallback, as the only hole of a template, or through a name assigned from such a read in the same file. The tool marks it Low because the code does not show what the variable holds: an environment variable that holds a whole `Authorization` value is, in real code, most often a bearer token, which is the migrated form, and the tool does not read the environment. Find out what the deployed value is. If it holds `Basic` followed by an encoded pair, the header is Basic Authentication: find the place that sets the variable (a configuration file, a deployment setting, a secret store, a build step), replace the credential with a client for token authentication, and change the code that sends the header so that it obtains and caches an access token and sends a `Bearer` header; follow the entry of the class that matches that code. If it holds a bearer token, no migration is needed. If a fallback literal is present and is not a bearer token or a Basic header, check that it does not hold a credential. The value is held in the environment or in the file that sets it, so a secret that is written into that file is still a secret to remove.

### JS-019 · AuthorizationHeaderValueFromSettingsProperty

<a id="js-019-judging"></a>

An `Authorization` header whose whole value is a property of a settings object (`settings.serviceAuthorization`), where the file fills that property from an environment value (`process.env`, `import.meta.env`, `Deno.env.get` or `Bun.env`). The tool marks it Low because the code does not show what the variable holds: a variable that holds a whole `Authorization` value is, in real code, most often a bearer token, which is the migrated form, and the tool does not read the environment. Find out what the deployed value is. If it holds `Basic` followed by an encoded pair, the header is Basic Authentication: find the place that sets the variable (a configuration file, a deployment setting, a secret store, a build step), replace the credential with a client for token authentication, and change the code that sends the header so that it obtains and caches an access token and sends a `Bearer` header; follow the entry of the class that matches that code. If it holds a bearer token, no migration is needed. The tool joins the property to the environment value by its name alone, so confirm that the property you found is the one the header uses, and look at settings written in forms the tool does not read (a different property name, a call). The value is held in the environment or in the file that sets it, so a secret that is written into that file is still a secret to remove.

## Migration suggestions by pattern class

Each pattern class has two entries, one for each flow. Choose the entry for the flow you confirmed above. Every entry contains the code being replaced, the migrated code, the question of whether the destination is RelativityOne, a description of token acquisition, caching and expiry handling in the variants of [Choosing the variant for your application](#choosing-the-variant-for-your-application), how to confirm the flow and the default, the OAuth2 Client to create or confirm with its settings, and the pointer to Secret Management (Part B of the Migration Documentation). Every Authorization Code entry adds a description of refresh-token handling, of PKCE, and of its three variants.

**Part 1, part 2 and part 3.** The text of an entry refers to its own pieces by number. The same names appear in the report's recommendations. Each piece has a label in the entry:

- **Part 1** is the change at the point of use: the text under the label *Under token authentication*, with its migrated code.
- **Part 2** is the token acquisition: the part of the text under the label *Credentials* that follows the paragraph *Where the credentials come from*. It describes the request to the token endpoint, the caching of the token and what to do when it expires. A Client Credentials entry starts it with *Where the token comes from*, and an Authorization Code entry with *Common to all three variants*.
- **Part 3** is the OAuth2 Client settings, under the label *OAuth2 Client* at the end of the entry.

The labels *Source form* (the construct being replaced) and *Verification* (what to check before you deploy) are not numbered.

The seven Low classes, JS-007, JS-009, JS-011, JS-014, JS-016, JS-018 and JS-019, have no entry because they produce no recommendation. They are covered under [Judging a Low-confidence match](#judging-a-low-confidence-match).

### JS-001 · ClientAuthCredentialObject

<a id="js-001-migration"></a>

Remove the `auth` option, also from `axios.create`, from `axios.defaults` and from a `request` options object, and send a Bearer token in an `Authorization` header instead; in browser code the call moves behind the server-side component.

#### Client Credentials

<!-- generated:begin JS-001 client-credentials -->
**Confirming the flow.** When no signal in a file decides the flow, the tool assigns Client Credentials, the default, with this note: "Who supplies the credential at run time? If a person types it or signs in, the integration acts for that person: use Authorization Code. If it comes from configuration, settings, an environment variable or a secret store: use Client Credentials. No signal in this file showed which applies, so Client Credentials, the default, is recommended. Use Authorization Code instead if the integration acts on behalf of a signed-in user, or if your RelativityOne instance already has a Code-flow OAuth2 Client for this integration." A fixed-credential signal assigns it with this note: "A fixed-credential signal in this file points to Client Credentials, but how often that kind of signal is right has not been measured. Use Authorization Code instead if the integration acts on behalf of a signed-in user, or if your RelativityOne instance already has a Code-flow OAuth2 Client for this integration." See [Confirming the flow](#confirming-the-flow) and the "Which flow" paragraph of JS-001 in [Part A](part-a-javascript.md#js-001).

Source form: **Applies to:** browser and Node.js; `axios` (both), `request` (Node.js).

```javascript
await axios.get(url, { auth: { username: userName, password: secret } });
```

Under token authentication: **First, the destination:** if this Relativity instance is not RelativityOne — including a Relativity Server instance, whose own documentation governs — this finding is informational and the rest of this template does not apply.

*Node.js:*

```javascript
const CSRF_HEADER_VALUE = "-";
await axios.get(url, { headers: { Authorization: `Bearer ${accessToken}`, "X-CSRF-Header": CSRF_HEADER_VALUE } });
```

*Browser:* the call moves behind the server-side component (Authorization Code, variant 1; or the browser variant of Client Credentials when no person signs in), and the browser calls its own origin with no `Authorization` header:

```javascript
await fetch(API_ROUTE, { credentials: "same-origin" });
```

Remove the `auth` member, also from `axios.create` and from `axios.defaults`, and from a `request` options object. **With `axios`, `auth` overwrites a custom `Authorization` header when both are present** (the documentation of the package version read for this text; other versions unchecked), so it must go entirely.

Credentials: **Where the credentials come from.** The text establishes that a client sends Basic, not that the destination is RelativityOne. If it is not RelativityOne, the migration is that service's own token mechanism, this finding is informational for this tool, and the reader stops here. If it is RelativityOne, the token comes from the token-acquisition part that follows, and the user name and password are replaced by the OAuth2 Client's client ID and secret, kept as Secret Management (Part B of the Migration Documentation) describes, never in source, in a tracked `.env` file or in a bundle.

**Where the token comes from.** Create an OAuth2 Client with Flow Grant Type set to Client Credentials (part 3 lists its settings), then send a form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=client_credentials`, `client_id` and `client_secret`. The token endpoint is **documented** for this grant. The token is then sent as `Authorization: Bearer <token>`, with `X-CSRF-Header: -` on REST and Kepler calls. **The scope depends on what the integration needs, and no single scope is required.** Relativity's documentation shows `SystemUserInfo` and `UserInfoAccess` and no page says which is correct or what either grants; use the value the API you call documents, and confirm it. The text has two variants, by where the code runs, and each opens with the precondition it is for: read that sentence and discard the variant that does not apply.

**Variant: a Node.js process acting as itself**

**Precondition: this variant is for a Node.js job, service or script that does not act for a signed-in person.**

```javascript
const IDENTITY_TOKEN_URL = "https://HOST/Relativity/Identity/connect/token";
const EXPIRY_MARGIN_SECONDS = 60;
const MILLISECONDS_PER_SECOND = 1000;
let cachedToken = null;

async function getAccessToken() {
	const now = Date.now();
	if (cachedToken === null || cachedToken.expiresAtMs - now < EXPIRY_MARGIN_SECONDS * MILLISECONDS_PER_SECOND) {
		const response = await fetch(IDENTITY_TOKEN_URL, {
			method: "POST",
			headers: { "Content-Type": "application/x-www-form-urlencoded" },
			body: new URLSearchParams({
				grant_type: "client_credentials",
				client_id: process.env.RELATIVITY_CLIENT_ID,
				client_secret: process.env.RELATIVITY_CLIENT_SECRET,
			}),
		});
		const payload = await response.json();
		cachedToken = {
			value: payload.access_token,
			expiresAtMs: now + payload.expires_in * MILLISECONDS_PER_SECOND,
		};
	}
	return cachedToken.value;
}
```

**Cache the token and re-acquire it on expiry — do not refresh.** Client Credentials offers no refresh tokens: **verified by NSerio against a live instance; not documented by Relativity** — the Allow Refresh Tokens control does not appear for this grant in the RelativityOne user interface. The recommended Access Token Lifetime is listed in part 3. The block uses the built-in `fetch` and no package. **The client secret** belongs in the process environment or a secret store, as Secret Management (Part B of the Migration Documentation) describes, **never in source, in a tracked `.env` file or in a bundle**. A reset invalidates the previous secret immediately, so plan the cut-over before resetting it. **Before deploying, confirm the calling identity:** every call now runs as the OAuth2 Client's Context User, not as the user in the replaced credential, and a non-administrator Context User is NSerio's recommendation. Trusted IPs restrict where a token can be requested, not a token already issued.

**Variant: browser code that does not act for a signed-in user**

**Precondition: this variant is for browser code that calls RelativityOne with no person signed in to it, so no person's identity exists to preserve. If a person signs in, use Authorization Code, variant 1.**

The Relativity request moves behind a **server-side component** that holds the secret and performs the token exchange (the Node.js process variant above); the browser calls that component on its own origin. **The component must authenticate and authorize its own callers, and the OAuth2 Client's Context User must hold the minimum permissions the operation needs. Without both, the result is an unauthenticated relay into RelativityOne:** every caller reaching it acts with the Context User's full rights, which is strictly worse than the starting point. The relay returns results to the browser, **never the token** and never the secret. For browser code the credential has been public for as long as the code has shipped; the tool does not escalate it, so this text states the exposure and the rotation:

> Treat this credential as compromised. Rotate it, and assume the repository history has published it. Completing the migration does not close this exposure; rotation does.

**When the code acts for a signed-in user.** Client Credentials is one service identity by construction. Code that asks a person for a password and sends it is the Authorization Code case, and moving to it means a new OAuth2 Client, because the flow is fixed when a client is created. The finding's flow note says when to make that choice.

Verification: **Verification note.** **A token has a finite lifetime.** **The calling identity changes**; the token-acquisition part says to whom. **Other uses may not appear as findings**: an options object held in a variable and handed to a client elsewhere, and an alias of the client, are not read.

**If this code runs in a browser, the credential has been public for as long as the code has shipped.** The exposure and the rotation are stated here and the tool does not escalate it:

> Treat this credential as compromised. Rotate it, and assume the repository history has published it. Completing the migration does not close this exposure; rotation does.

OAuth2 Client: Create or confirm this OAuth2 Client in RelativityOne (NSerio's observation: the menu path is Home > Authentication > OAuth2 Client Manager, as Relativity's REST API authentication page names it). This tool never connects to RelativityOne and never creates or changes the OAuth2 Client; the settings below are guidance.
- Name: a descriptive name identifying the integration it serves. Use one OAuth2 Client per integration and do not share it across integrations.
- Flow Grant Type: Client Credentials
- Context User: NSerio's recommendation: a dedicated integration user with the minimum permissions the integration needs, and not a personal account and not an administrator, because every call runs with that user's permissions and an administrator Context User gives the integration an administrator's reach. Relativity documentation (REST API authentication page): describes the Context User as usually a member of the Relativity Administrators group, though it could differ depending on the API or operation invoked; NSerio recommends against that default for an integration.
- Access Token Lifetime: 60 minutes, stated in minutes because the field is in minutes. Relativity documentation: tokens for this flow must have a short lifetime, and the recommended value matches the Identity Server default.
- Trusted IPs: Relativity documentation: the address list is checked whenever a token is requested with the OAuth2 Client's ID and secret. NSerio's recommendation: populate it with the integration's outbound IP addresses, one per line, wherever they are known and stable. It restricts where a token can be requested, checked when the token is issued; it does not restrict a token already issued, which travels in the request header and is valid from anywhere until it expires.
- Allow refresh tokens: not used by this flow. The integration requests a new access token when the current one expires.
- Enabled: on.
- Every call made with this OAuth2 Client runs with the Context User's permissions.
- Relativity documentation: the flow is fixed when the OAuth2 Client is created and cannot be changed when editing it, so moving this integration to Authorization Code later means creating a new OAuth2 Client.
<!-- generated:end JS-001 client-credentials -->

#### Authorization Code

<!-- generated:begin JS-001 authorization-code -->
**Confirming the flow.** An interactive-user signal in a file assigns Authorization Code, with this note: "An interactive-user signal in this file points to Authorization Code. Confirm the flow: use Client Credentials instead if the integration runs with credentials fixed for the integration and no signed-in user." Your user profile can also make Authorization Code the default; see Changing the default in [Confirming the flow](#confirming-the-flow). See [Confirming the flow](#confirming-the-flow) and the "Which flow" paragraph of JS-001 in [Part A](part-a-javascript.md#js-001).

Source form: **Applies to:** browser and Node.js; `axios` (both), `request` (Node.js).

```javascript
await axios.get(url, { auth: { username: userName, password: secret } });
```

Under token authentication: **First, the destination:** if this Relativity instance is not RelativityOne — including a Relativity Server instance, whose own documentation governs — this finding is informational and the rest of this template does not apply.

*Node.js:*

```javascript
const CSRF_HEADER_VALUE = "-";
await axios.get(url, { headers: { Authorization: `Bearer ${accessToken}`, "X-CSRF-Header": CSRF_HEADER_VALUE } });
```

*Browser:* the call moves behind the server-side component (Authorization Code, variant 1; or the browser variant of Client Credentials when no person signs in), and the browser calls its own origin with no `Authorization` header:

```javascript
await fetch(API_ROUTE, { credentials: "same-origin" });
```

Remove the `auth` member, also from `axios.create` and from `axios.defaults`, and from a `request` options object. **With `axios`, `auth` overwrites a custom `Authorization` header when both are present** (the documentation of the package version read for this text; other versions unchecked), so it must go entirely.

Credentials: **Where the credentials come from.** The text establishes that a client sends Basic, not that the destination is RelativityOne. If it is not RelativityOne, the migration is that service's own token mechanism, this finding is informational for this tool, and the reader stops here. If it is RelativityOne, the token comes from the token-acquisition part that follows, and the user name and password are replaced by the OAuth2 Client's client ID and secret, kept as Secret Management (Part B of the Migration Documentation) describes, never in source, in a tracked `.env` file or in a bundle.

**Common to all three variants.** The text has a common part and **three variants by application type**, each opening with the precondition it is for: a browser with a server-side component, a Node.js server, and a Node.js command-line tool or desktop shell. Read the precondition of each and discard the variants that do not apply.

**Sign in.** Replace the code that collected the person's credentials with a redirect to the authorize endpoint `<host>/Relativity/Identity/connect/authorize` (**verified by NSerio against a live instance; not documented by Relativity**), which leads to RelativityOne's login page. The request carries the standard authorization-code parameters — `client_id`, `response_type=code`, the registered `redirect_uri`, `state` checked on return, and `code_challenge` with `code_challenge_method=S256` when PKCE is used; these are standard OAuth and Relativity documents none of them.

**PKCE.** PKCE is required by Relativity's own documentation to obtain a refresh token and not otherwise: a Code request that does not ask for a refresh token works without it, **verified by NSerio against a live instance; not documented by Relativity**. NSerio recommends `S256` (NSerio recommendation) and does not claim that Relativity accepts any particular method. `state` is generated, stored against the session and compared on return; `nonce` is not used, and Relativity does not document whether it enforces either.

**The client secret is required.** Code does not work without a client secret: tested and rejected, **verified by NSerio against a live instance; not documented by Relativity**, which confirms Relativity's own statement. **Any secret shipped to a browser is public**, and bundling, minifying or obfuscating changes nothing, so the exchange runs where a secret can be kept.

**Exchange the code.** A form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=authorization_code`, the code, the same `redirect_uri`, the client ID and the secret, plus the `code_verifier` when PKCE is used (**verified by NSerio against a live instance; not documented by Relativity**).

**Redirect URL.** A server-side application registers **one exact HTTPS URL per environment, no wildcards**; the matching rules are not documented and no claim is made about them. A plain-HTTP loopback Redirect URL `http://127.0.0.1:<port>/<path>` on a **fixed registered port** is accepted, **verified by NSerio against a live instance; not documented by Relativity**, and is the desktop shape.

**On expiry.** With Allow Refresh Tokens enabled and PKCE used, the code holds a refresh token and sends `grant_type=refresh_token` in place of the code exchange: *inferred* from the standard OAuth shape, not tested. Relativity does not document a refresh token's lifetime, rotation or revocation, nor what disabling the client does to tokens already issued. **A refresh token is a secret.** **Single sign-on:** sign-in happens on RelativityOne's own page, so the instance's single sign-on and two-factor rules apply unchanged (*inferred*). **The code stops collecting the password:** after this change nothing receives, stores or forwards the person's user name and password, and the code holds only tokens. Every call now runs as the person who signed in, so **the calling identity changes**. **Scope:** no scope set is prescribed for Code; `openid RelativityRequestOrigin UserInfo UserInfoAccess AuthenticationID RelativityWeb` works, **verified by NSerio against a live instance; not documented by Relativity**, as working and not as minimal.

**Variant 1 — Browser with a server-side component**

**Precondition: this variant is for code that runs in a web browser and signs a person in to RelativityOne. If your code runs in Node.js, use variant 2 or variant 3.**

A browser cannot hold the secret the Code grant requires, so **the browser never calls RelativityOne**: it navigates to a route on **the application's own origin**, and a **server-side component** (variant 2's code) creates `state`, redirects to the authorize endpoint, receives the code at its registered HTTPS Redirect URL, exchanges it, keeps the tokens and gives the browser only its own session. **The browser holds a session identifier with that component and nothing from RelativityOne.** The authorize step is a **top-level navigation** (a full-window redirect, not a `fetch`).

```javascript
const LOGIN_ROUTE = "/auth/login";
const API_ROUTE = "/api/documents";

function signIn() {
	window.location.assign(LOGIN_ROUTE);
}

async function loadDocuments() {
	const response = await fetch(API_ROUTE, { credentials: "same-origin" });
	return response.json();
}
```

Both calls go to the page's own origin. `credentials: "include"` is a cookie policy and is not recommended here, because the architecture needs no cross-origin call. **Cross-origin access, both ways, both not verified:** NSerio has not run a preflight against a live instance. *If RelativityOne serves CORS to browser callers*, the legacy browser Basic call could have worked from another origin, and a browser holding a Bearer token could in principle call it directly; **this text still does not recommend that**, because the only grant that yields a token for a person needs the secret the browser cannot hold. *If it does not*, the legacy call worked only from the same origin or through a proxy the client already runs. **The recommended architecture holds under either answer.** **The component keeps the tokens on the server**, never in browser storage, which any script on the page can read (a browser behaviour, unchecked); the recommended session cookie is `HttpOnly`, `Secure` and `SameSite` (NSerio recommendation; unchecked).

**Variant 2 — Node.js server**

**Precondition: this variant is for code that runs in Node.js as a web server and acts for people who sign in to it. It is not for browser code (variant 1) or a command-line tool (variant 3).**

Standard server-side Code flow. A login route builds the authorize address with `state` (and the PKCE verifier where a refresh token is wanted), stores them against the visitor's session and redirects; RelativityOne redirects to the **callback route**, registered as an exact HTTPS URL; the callback checks `state` and exchanges the code with the secret. This is the component variant 1 relies on.

```javascript
import crypto from "node:crypto";

const IDENTITY_BASE = "https://HOST/Relativity/Identity/connect";
const REDIRECT_URI = "https://app.example.test/auth/callback";
const CLIENT_ID = process.env.RELATIVITY_CLIENT_ID;
const CLIENT_SECRET = process.env.RELATIVITY_CLIENT_SECRET;
const RANDOM_BYTES = 32;
const pendingBySession = new Map();

function base64Url(buffer) {
	return buffer.toString("base64url");
}

function beginLogin(sessionId, wantRefreshToken) {
	const state = base64Url(crypto.randomBytes(RANDOM_BYTES));
	const verifier = base64Url(crypto.randomBytes(RANDOM_BYTES));
	const challenge = base64Url(crypto.createHash("sha256").update(verifier).digest());
	pendingBySession.set(sessionId, { state, verifier });
	const query = new URLSearchParams({
		response_type: "code",
		client_id: CLIENT_ID,
		redirect_uri: REDIRECT_URI,
		state,
	});
	if (wantRefreshToken) {
		query.set("code_challenge", challenge);
		query.set("code_challenge_method", "S256");
	}
	return `${IDENTITY_BASE}/authorize?${query}`;
}

async function completeLogin(sessionId, returnedCode, returnedState) {
	const pending = pendingBySession.get(sessionId);
	pendingBySession.delete(sessionId);
	const stateMatches = pending !== undefined
		&& returnedState !== undefined
		&& Buffer.byteLength(returnedState) === Buffer.byteLength(pending.state)
		&& crypto.timingSafeEqual(Buffer.from(returnedState), Buffer.from(pending.state));
	let tokens = null;
	if (stateMatches) {
		const response = await fetch(`${IDENTITY_BASE}/token`, {
			method: "POST",
			headers: { "Content-Type": "application/x-www-form-urlencoded" },
			body: new URLSearchParams({
				grant_type: "authorization_code",
				code: returnedCode,
				redirect_uri: REDIRECT_URI,
				client_id: CLIENT_ID,
				client_secret: CLIENT_SECRET,
				code_verifier: pending.verifier,
			}),
		});
		tokens = response.ok ? await response.json() : null;
	}
	return tokens;
}
```

`state` is consumed once and compared on return; a request with no pending entry or a different `state` gets no exchange. **The scope is not prescribed** (the sketch sends none). **Where the secret travels in the token request** (the body, as sketched, or HTTP Basic client authentication) is **not verified**. The sketch does not show the HTTP server, the session cookie or a session store that survives more than one process. **Tokens stay on the server**, in a session store that is not exposed to a request.

**Variant 3 — Node.js command line or desktop**

**Precondition: this variant is for code that runs on a person's own machine, as a command-line tool or a desktop shell, and signs that person in. It is not for a web server (variant 2) or browser code (variant 1).**

The process starts a listener from the built-in `node:http` module, bound to **`127.0.0.1` only**, on the **fixed registered port**, for **one request**; opens the system browser on the authorize address **and always prints the address too**, so a machine with no browser, an SSH session or a failed launch has a manual route; reads `code` and `state` from the one request, closes the listener, checks `state`, and exchanges the code as variant 2's `completeLogin` does.

```javascript
import http from "node:http";

const LOOPBACK_HOST = "127.0.0.1";
const REGISTERED_PORT = 8123;
const CALLBACK_PATH = "/callback";
const BASE_FOR_PARSING = `http://${LOOPBACK_HOST}:${REGISTERED_PORT}`;
const WAIT_MILLISECONDS = 120000;
const STATUS_OK = 200;
const STATUS_NOT_FOUND = 404;

function waitForRedirect() {
	return new Promise((resolve, reject) => {
		const server = http.createServer((request, response) => {
			const address = new URL(request.url, BASE_FOR_PARSING);
			const isCallback = address.pathname === CALLBACK_PATH;
			response.statusCode = isCallback ? STATUS_OK : STATUS_NOT_FOUND;
			response.end(isCallback ? "You can close this window." : "Not found");
			if (isCallback) {
				server.close();
				resolve({
					code: address.searchParams.get("code"),
					state: address.searchParams.get("state"),
				});
			}
		});
		server.on("error", reject);
		server.listen(REGISTERED_PORT, LOOPBACK_HOST);
		setTimeout(() => {
			server.close();
			reject(new Error("No redirect arrived"));
		}, WAIT_MILLISECONDS).unref();
	});
}
```

**Rules for the listener:** bind to loopback only; accept one request; **check `state`** (another local process can connect to a fixed, advertised port); close the listener after the request; and handle a busy port (a second server on a port in use raises `EADDRINUSE`, so fall back to the printed address). **Opening the system browser:** Node.js core has no function for it; run the platform's own launcher with a fixed program and an argument array, never a shell string built from the address (unchecked on every system). **Where tokens are kept:** a file with restricted permissions or the operating system's keychain; on Windows the POSIX mode argument of Node.js did not restrict a file when it was tried (*checked by NSerio locally* on Node.js v24.11.1), so a restricted file there needs an access-control change by other means, and on Linux and macOS it is unchecked. **The client secret of a distributed tool is extractable:** a command-line tool or desktop shell shipped to people carries its client secret in a form anyone who holds the distribution can extract, and it protects nothing against the person running it. **A known issue:** Relativity documents no recommendation for a distributed client; this design is what NSerio verified works. **Compensating guidance** (NSerio recommendation, subject to your own security policy): Trusted IPs where the users' outbound addresses are known and stable (checked at token issuance; applicability to Code *inferred*); the recommended short Access Token Lifetime; refresh tokens only when needed; and **one OAuth2 Client per distribution**, so a leaked secret can be disabled and reset alone.

Verification: **Verification note.** **A token has a finite lifetime.** **The calling identity changes**; the token-acquisition part says to whom. **Other uses may not appear as findings**: an options object held in a variable and handed to a client elsewhere, and an alias of the client, are not read.

**If this code runs in a browser, the credential has been public for as long as the code has shipped.** The exposure and the rotation are stated here and the tool does not escalate it:

> Treat this credential as compromised. Rotate it, and assume the repository history has published it. Completing the migration does not close this exposure; rotation does.

OAuth2 Client: Create or confirm this OAuth2 Client in RelativityOne (NSerio's observation: the menu path is Home > Authentication > OAuth2 Client Manager, as Relativity's REST API authentication page names it). This tool never connects to RelativityOne and never creates or changes the OAuth2 Client; the settings below are guidance.
- Name: a descriptive name identifying the application it serves.
- Flow Grant Type: Code
- Redirect URLs: Relativity documentation: each URL includes the http or https protocol prefix. The exact callback URL is registered by the Relativity administrator. Server-side web applications, NSerio's recommendation: one exact HTTPS URL per environment, with no wildcards. Desktop and CLI applications: a plain-HTTP loopback URL, `http://127.0.0.1:<port>/<path>`, on a fixed port because the URL is registered in advance and the port cannot be chosen at runtime (verified by NSerio against a live instance; not documented by Relativity).
- Access Token Lifetime: 60 minutes, stated in minutes because the field is in minutes. Relativity documentation: tokens for this flow must have a short lifetime, and the recommended value matches the Identity Server default.
- Allow refresh tokens: NSerio's recommendation: leave it off unless the application must keep a user session active beyond the access token lifetime without asking the user to sign in again. If it is on, the refresh tokens are stored as secrets, per Secret Management (Part B of the Migration Documentation).
- Enabled: on.
<!-- generated:end JS-001 authorization-code -->

### JS-002 · ClientAuthMethodCall

<a id="js-002-migration"></a>

Replace the `.auth(` call with a Bearer `Authorization` header set on the request; the scheme changes from `Basic` to `Bearer`, and a Bearer header is not read by the tool again.

#### Client Credentials

<!-- generated:begin JS-002 client-credentials -->
**Confirming the flow.** When no signal in a file decides the flow, the tool assigns Client Credentials, the default, with this note: "Who supplies the credential at run time? If a person types it or signs in, the integration acts for that person: use Authorization Code. If it comes from configuration, settings, an environment variable or a secret store: use Client Credentials. No signal in this file showed which applies, so Client Credentials, the default, is recommended. Use Authorization Code instead if the integration acts on behalf of a signed-in user, or if your RelativityOne instance already has a Code-flow OAuth2 Client for this integration." A fixed-credential signal assigns it with this note: "A fixed-credential signal in this file points to Client Credentials, but how often that kind of signal is right has not been measured. Use Authorization Code instead if the integration acts on behalf of a signed-in user, or if your RelativityOne instance already has a Code-flow OAuth2 Client for this integration." See [Confirming the flow](#confirming-the-flow) and the "Which flow" paragraph of JS-002 in [Part A](part-a-javascript.md#js-002).

Source form: **Applies to:** browser and Node.js; `superagent` (both), `request` (Node.js).

```javascript
await superagent.get(url).auth(userName, secret);
```

Under token authentication: **First, the destination:** if this Relativity instance is not RelativityOne — including a Relativity Server instance, whose own documentation governs — this finding is informational and the rest of this template does not apply.

*Node.js:*

```javascript
const CSRF_HEADER_VALUE = "-";
await superagent.get(url).set("Authorization", `Bearer ${accessToken}`).set("X-CSRF-Header", CSRF_HEADER_VALUE);
```

(`superagent` also offers `.auth(token, { type: "bearer" })`, documented for the package version read for this text; the fourth `auth` argument of `request` is its bearer form; other versions unchecked.) *Browser:* the call moves behind the server-side component (Authorization Code, variant 1; or the browser variant of Client Credentials when no person signs in), and the browser calls its own origin with no `Authorization` header:

```javascript
await fetch(API_ROUTE, { credentials: "same-origin" });
```

Credentials: **Where the credentials come from.** The text establishes that a client sends Basic, not that the destination is RelativityOne. If it is not RelativityOne, the migration is that service's own token mechanism, this finding is informational for this tool, and the reader stops here. If it is RelativityOne, the token comes from the token-acquisition part that follows, and the user name and password are replaced by the OAuth2 Client's client ID and secret, kept as Secret Management (Part B of the Migration Documentation) describes, never in source, in a tracked `.env` file or in a bundle.

**Where the token comes from.** Create an OAuth2 Client with Flow Grant Type set to Client Credentials (part 3 lists its settings), then send a form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=client_credentials`, `client_id` and `client_secret`. The token endpoint is **documented** for this grant. The token is then sent as `Authorization: Bearer <token>`, with `X-CSRF-Header: -` on REST and Kepler calls. **The scope depends on what the integration needs, and no single scope is required.** Relativity's documentation shows `SystemUserInfo` and `UserInfoAccess` and no page says which is correct or what either grants; use the value the API you call documents, and confirm it. The text has two variants, by where the code runs, and each opens with the precondition it is for: read that sentence and discard the variant that does not apply.

**Variant: a Node.js process acting as itself**

**Precondition: this variant is for a Node.js job, service or script that does not act for a signed-in person.**

```javascript
const IDENTITY_TOKEN_URL = "https://HOST/Relativity/Identity/connect/token";
const EXPIRY_MARGIN_SECONDS = 60;
const MILLISECONDS_PER_SECOND = 1000;
let cachedToken = null;

async function getAccessToken() {
	const now = Date.now();
	if (cachedToken === null || cachedToken.expiresAtMs - now < EXPIRY_MARGIN_SECONDS * MILLISECONDS_PER_SECOND) {
		const response = await fetch(IDENTITY_TOKEN_URL, {
			method: "POST",
			headers: { "Content-Type": "application/x-www-form-urlencoded" },
			body: new URLSearchParams({
				grant_type: "client_credentials",
				client_id: process.env.RELATIVITY_CLIENT_ID,
				client_secret: process.env.RELATIVITY_CLIENT_SECRET,
			}),
		});
		const payload = await response.json();
		cachedToken = {
			value: payload.access_token,
			expiresAtMs: now + payload.expires_in * MILLISECONDS_PER_SECOND,
		};
	}
	return cachedToken.value;
}
```

**Cache the token and re-acquire it on expiry — do not refresh.** Client Credentials offers no refresh tokens: **verified by NSerio against a live instance; not documented by Relativity** — the Allow Refresh Tokens control does not appear for this grant in the RelativityOne user interface. The recommended Access Token Lifetime is listed in part 3. The block uses the built-in `fetch` and no package. **The client secret** belongs in the process environment or a secret store, as Secret Management (Part B of the Migration Documentation) describes, **never in source, in a tracked `.env` file or in a bundle**. A reset invalidates the previous secret immediately, so plan the cut-over before resetting it. **Before deploying, confirm the calling identity:** every call now runs as the OAuth2 Client's Context User, not as the user in the replaced credential, and a non-administrator Context User is NSerio's recommendation. Trusted IPs restrict where a token can be requested, not a token already issued.

**Variant: browser code that does not act for a signed-in user**

**Precondition: this variant is for browser code that calls RelativityOne with no person signed in to it, so no person's identity exists to preserve. If a person signs in, use Authorization Code, variant 1.**

The Relativity request moves behind a **server-side component** that holds the secret and performs the token exchange (the Node.js process variant above); the browser calls that component on its own origin. **The component must authenticate and authorize its own callers, and the OAuth2 Client's Context User must hold the minimum permissions the operation needs. Without both, the result is an unauthenticated relay into RelativityOne:** every caller reaching it acts with the Context User's full rights, which is strictly worse than the starting point. The relay returns results to the browser, **never the token** and never the secret. For browser code the credential has been public for as long as the code has shipped; the tool does not escalate it, so this text states the exposure and the rotation:

> Treat this credential as compromised. Rotate it, and assume the repository history has published it. Completing the migration does not close this exposure; rotation does.

**When the code acts for a signed-in user.** Client Credentials is one service identity by construction. Code that asks a person for a password and sends it is the Authorization Code case, and moving to it means a new OAuth2 Client, because the flow is fixed when a client is created. The finding's flow note says when to make that choice.

Verification: **Verification note.** **A token has a finite lifetime.** **The calling identity changes.** **The scheme changes from `Basic` to `Bearer`**: a Bearer header is not read by this tool again.

**If this code runs in a browser, the credential has been public for as long as the code has shipped.** The exposure and the rotation are stated here and the tool does not escalate it:

> Treat this credential as compromised. Rotate it, and assume the repository history has published it. Completing the migration does not close this exposure; rotation does.

OAuth2 Client: Create or confirm this OAuth2 Client in RelativityOne (NSerio's observation: the menu path is Home > Authentication > OAuth2 Client Manager, as Relativity's REST API authentication page names it). This tool never connects to RelativityOne and never creates or changes the OAuth2 Client; the settings below are guidance.
- Name: a descriptive name identifying the integration it serves. Use one OAuth2 Client per integration and do not share it across integrations.
- Flow Grant Type: Client Credentials
- Context User: NSerio's recommendation: a dedicated integration user with the minimum permissions the integration needs, and not a personal account and not an administrator, because every call runs with that user's permissions and an administrator Context User gives the integration an administrator's reach. Relativity documentation (REST API authentication page): describes the Context User as usually a member of the Relativity Administrators group, though it could differ depending on the API or operation invoked; NSerio recommends against that default for an integration.
- Access Token Lifetime: 60 minutes, stated in minutes because the field is in minutes. Relativity documentation: tokens for this flow must have a short lifetime, and the recommended value matches the Identity Server default.
- Trusted IPs: Relativity documentation: the address list is checked whenever a token is requested with the OAuth2 Client's ID and secret. NSerio's recommendation: populate it with the integration's outbound IP addresses, one per line, wherever they are known and stable. It restricts where a token can be requested, checked when the token is issued; it does not restrict a token already issued, which travels in the request header and is valid from anywhere until it expires.
- Allow refresh tokens: not used by this flow. The integration requests a new access token when the current one expires.
- Enabled: on.
- Every call made with this OAuth2 Client runs with the Context User's permissions.
- Relativity documentation: the flow is fixed when the OAuth2 Client is created and cannot be changed when editing it, so moving this integration to Authorization Code later means creating a new OAuth2 Client.
<!-- generated:end JS-002 client-credentials -->

#### Authorization Code

<!-- generated:begin JS-002 authorization-code -->
**Confirming the flow.** An interactive-user signal in a file assigns Authorization Code, with this note: "An interactive-user signal in this file points to Authorization Code. Confirm the flow: use Client Credentials instead if the integration runs with credentials fixed for the integration and no signed-in user." Your user profile can also make Authorization Code the default; see Changing the default in [Confirming the flow](#confirming-the-flow). See [Confirming the flow](#confirming-the-flow) and the "Which flow" paragraph of JS-002 in [Part A](part-a-javascript.md#js-002).

Source form: **Applies to:** browser and Node.js; `superagent` (both), `request` (Node.js).

```javascript
await superagent.get(url).auth(userName, secret);
```

Under token authentication: **First, the destination:** if this Relativity instance is not RelativityOne — including a Relativity Server instance, whose own documentation governs — this finding is informational and the rest of this template does not apply.

*Node.js:*

```javascript
const CSRF_HEADER_VALUE = "-";
await superagent.get(url).set("Authorization", `Bearer ${accessToken}`).set("X-CSRF-Header", CSRF_HEADER_VALUE);
```

(`superagent` also offers `.auth(token, { type: "bearer" })`, documented for the package version read for this text; the fourth `auth` argument of `request` is its bearer form; other versions unchecked.) *Browser:* the call moves behind the server-side component (Authorization Code, variant 1; or the browser variant of Client Credentials when no person signs in), and the browser calls its own origin with no `Authorization` header:

```javascript
await fetch(API_ROUTE, { credentials: "same-origin" });
```

Credentials: **Where the credentials come from.** The text establishes that a client sends Basic, not that the destination is RelativityOne. If it is not RelativityOne, the migration is that service's own token mechanism, this finding is informational for this tool, and the reader stops here. If it is RelativityOne, the token comes from the token-acquisition part that follows, and the user name and password are replaced by the OAuth2 Client's client ID and secret, kept as Secret Management (Part B of the Migration Documentation) describes, never in source, in a tracked `.env` file or in a bundle.

**Common to all three variants.** The text has a common part and **three variants by application type**, each opening with the precondition it is for: a browser with a server-side component, a Node.js server, and a Node.js command-line tool or desktop shell. Read the precondition of each and discard the variants that do not apply.

**Sign in.** Replace the code that collected the person's credentials with a redirect to the authorize endpoint `<host>/Relativity/Identity/connect/authorize` (**verified by NSerio against a live instance; not documented by Relativity**), which leads to RelativityOne's login page. The request carries the standard authorization-code parameters — `client_id`, `response_type=code`, the registered `redirect_uri`, `state` checked on return, and `code_challenge` with `code_challenge_method=S256` when PKCE is used; these are standard OAuth and Relativity documents none of them.

**PKCE.** PKCE is required by Relativity's own documentation to obtain a refresh token and not otherwise: a Code request that does not ask for a refresh token works without it, **verified by NSerio against a live instance; not documented by Relativity**. NSerio recommends `S256` (NSerio recommendation) and does not claim that Relativity accepts any particular method. `state` is generated, stored against the session and compared on return; `nonce` is not used, and Relativity does not document whether it enforces either.

**The client secret is required.** Code does not work without a client secret: tested and rejected, **verified by NSerio against a live instance; not documented by Relativity**, which confirms Relativity's own statement. **Any secret shipped to a browser is public**, and bundling, minifying or obfuscating changes nothing, so the exchange runs where a secret can be kept.

**Exchange the code.** A form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=authorization_code`, the code, the same `redirect_uri`, the client ID and the secret, plus the `code_verifier` when PKCE is used (**verified by NSerio against a live instance; not documented by Relativity**).

**Redirect URL.** A server-side application registers **one exact HTTPS URL per environment, no wildcards**; the matching rules are not documented and no claim is made about them. A plain-HTTP loopback Redirect URL `http://127.0.0.1:<port>/<path>` on a **fixed registered port** is accepted, **verified by NSerio against a live instance; not documented by Relativity**, and is the desktop shape.

**On expiry.** With Allow Refresh Tokens enabled and PKCE used, the code holds a refresh token and sends `grant_type=refresh_token` in place of the code exchange: *inferred* from the standard OAuth shape, not tested. Relativity does not document a refresh token's lifetime, rotation or revocation, nor what disabling the client does to tokens already issued. **A refresh token is a secret.** **Single sign-on:** sign-in happens on RelativityOne's own page, so the instance's single sign-on and two-factor rules apply unchanged (*inferred*). **The code stops collecting the password:** after this change nothing receives, stores or forwards the person's user name and password, and the code holds only tokens. Every call now runs as the person who signed in, so **the calling identity changes**. **Scope:** no scope set is prescribed for Code; `openid RelativityRequestOrigin UserInfo UserInfoAccess AuthenticationID RelativityWeb` works, **verified by NSerio against a live instance; not documented by Relativity**, as working and not as minimal.

**Variant 1 — Browser with a server-side component**

**Precondition: this variant is for code that runs in a web browser and signs a person in to RelativityOne. If your code runs in Node.js, use variant 2 or variant 3.**

A browser cannot hold the secret the Code grant requires, so **the browser never calls RelativityOne**: it navigates to a route on **the application's own origin**, and a **server-side component** (variant 2's code) creates `state`, redirects to the authorize endpoint, receives the code at its registered HTTPS Redirect URL, exchanges it, keeps the tokens and gives the browser only its own session. **The browser holds a session identifier with that component and nothing from RelativityOne.** The authorize step is a **top-level navigation** (a full-window redirect, not a `fetch`).

```javascript
const LOGIN_ROUTE = "/auth/login";
const API_ROUTE = "/api/documents";

function signIn() {
	window.location.assign(LOGIN_ROUTE);
}

async function loadDocuments() {
	const response = await fetch(API_ROUTE, { credentials: "same-origin" });
	return response.json();
}
```

Both calls go to the page's own origin. `credentials: "include"` is a cookie policy and is not recommended here, because the architecture needs no cross-origin call. **Cross-origin access, both ways, both not verified:** NSerio has not run a preflight against a live instance. *If RelativityOne serves CORS to browser callers*, the legacy browser Basic call could have worked from another origin, and a browser holding a Bearer token could in principle call it directly; **this text still does not recommend that**, because the only grant that yields a token for a person needs the secret the browser cannot hold. *If it does not*, the legacy call worked only from the same origin or through a proxy the client already runs. **The recommended architecture holds under either answer.** **The component keeps the tokens on the server**, never in browser storage, which any script on the page can read (a browser behaviour, unchecked); the recommended session cookie is `HttpOnly`, `Secure` and `SameSite` (NSerio recommendation; unchecked).

**Variant 2 — Node.js server**

**Precondition: this variant is for code that runs in Node.js as a web server and acts for people who sign in to it. It is not for browser code (variant 1) or a command-line tool (variant 3).**

Standard server-side Code flow. A login route builds the authorize address with `state` (and the PKCE verifier where a refresh token is wanted), stores them against the visitor's session and redirects; RelativityOne redirects to the **callback route**, registered as an exact HTTPS URL; the callback checks `state` and exchanges the code with the secret. This is the component variant 1 relies on.

```javascript
import crypto from "node:crypto";

const IDENTITY_BASE = "https://HOST/Relativity/Identity/connect";
const REDIRECT_URI = "https://app.example.test/auth/callback";
const CLIENT_ID = process.env.RELATIVITY_CLIENT_ID;
const CLIENT_SECRET = process.env.RELATIVITY_CLIENT_SECRET;
const RANDOM_BYTES = 32;
const pendingBySession = new Map();

function base64Url(buffer) {
	return buffer.toString("base64url");
}

function beginLogin(sessionId, wantRefreshToken) {
	const state = base64Url(crypto.randomBytes(RANDOM_BYTES));
	const verifier = base64Url(crypto.randomBytes(RANDOM_BYTES));
	const challenge = base64Url(crypto.createHash("sha256").update(verifier).digest());
	pendingBySession.set(sessionId, { state, verifier });
	const query = new URLSearchParams({
		response_type: "code",
		client_id: CLIENT_ID,
		redirect_uri: REDIRECT_URI,
		state,
	});
	if (wantRefreshToken) {
		query.set("code_challenge", challenge);
		query.set("code_challenge_method", "S256");
	}
	return `${IDENTITY_BASE}/authorize?${query}`;
}

async function completeLogin(sessionId, returnedCode, returnedState) {
	const pending = pendingBySession.get(sessionId);
	pendingBySession.delete(sessionId);
	const stateMatches = pending !== undefined
		&& returnedState !== undefined
		&& Buffer.byteLength(returnedState) === Buffer.byteLength(pending.state)
		&& crypto.timingSafeEqual(Buffer.from(returnedState), Buffer.from(pending.state));
	let tokens = null;
	if (stateMatches) {
		const response = await fetch(`${IDENTITY_BASE}/token`, {
			method: "POST",
			headers: { "Content-Type": "application/x-www-form-urlencoded" },
			body: new URLSearchParams({
				grant_type: "authorization_code",
				code: returnedCode,
				redirect_uri: REDIRECT_URI,
				client_id: CLIENT_ID,
				client_secret: CLIENT_SECRET,
				code_verifier: pending.verifier,
			}),
		});
		tokens = response.ok ? await response.json() : null;
	}
	return tokens;
}
```

`state` is consumed once and compared on return; a request with no pending entry or a different `state` gets no exchange. **The scope is not prescribed** (the sketch sends none). **Where the secret travels in the token request** (the body, as sketched, or HTTP Basic client authentication) is **not verified**. The sketch does not show the HTTP server, the session cookie or a session store that survives more than one process. **Tokens stay on the server**, in a session store that is not exposed to a request.

**Variant 3 — Node.js command line or desktop**

**Precondition: this variant is for code that runs on a person's own machine, as a command-line tool or a desktop shell, and signs that person in. It is not for a web server (variant 2) or browser code (variant 1).**

The process starts a listener from the built-in `node:http` module, bound to **`127.0.0.1` only**, on the **fixed registered port**, for **one request**; opens the system browser on the authorize address **and always prints the address too**, so a machine with no browser, an SSH session or a failed launch has a manual route; reads `code` and `state` from the one request, closes the listener, checks `state`, and exchanges the code as variant 2's `completeLogin` does.

```javascript
import http from "node:http";

const LOOPBACK_HOST = "127.0.0.1";
const REGISTERED_PORT = 8123;
const CALLBACK_PATH = "/callback";
const BASE_FOR_PARSING = `http://${LOOPBACK_HOST}:${REGISTERED_PORT}`;
const WAIT_MILLISECONDS = 120000;
const STATUS_OK = 200;
const STATUS_NOT_FOUND = 404;

function waitForRedirect() {
	return new Promise((resolve, reject) => {
		const server = http.createServer((request, response) => {
			const address = new URL(request.url, BASE_FOR_PARSING);
			const isCallback = address.pathname === CALLBACK_PATH;
			response.statusCode = isCallback ? STATUS_OK : STATUS_NOT_FOUND;
			response.end(isCallback ? "You can close this window." : "Not found");
			if (isCallback) {
				server.close();
				resolve({
					code: address.searchParams.get("code"),
					state: address.searchParams.get("state"),
				});
			}
		});
		server.on("error", reject);
		server.listen(REGISTERED_PORT, LOOPBACK_HOST);
		setTimeout(() => {
			server.close();
			reject(new Error("No redirect arrived"));
		}, WAIT_MILLISECONDS).unref();
	});
}
```

**Rules for the listener:** bind to loopback only; accept one request; **check `state`** (another local process can connect to a fixed, advertised port); close the listener after the request; and handle a busy port (a second server on a port in use raises `EADDRINUSE`, so fall back to the printed address). **Opening the system browser:** Node.js core has no function for it; run the platform's own launcher with a fixed program and an argument array, never a shell string built from the address (unchecked on every system). **Where tokens are kept:** a file with restricted permissions or the operating system's keychain; on Windows the POSIX mode argument of Node.js did not restrict a file when it was tried (*checked by NSerio locally* on Node.js v24.11.1), so a restricted file there needs an access-control change by other means, and on Linux and macOS it is unchecked. **The client secret of a distributed tool is extractable:** a command-line tool or desktop shell shipped to people carries its client secret in a form anyone who holds the distribution can extract, and it protects nothing against the person running it. **A known issue:** Relativity documents no recommendation for a distributed client; this design is what NSerio verified works. **Compensating guidance** (NSerio recommendation, subject to your own security policy): Trusted IPs where the users' outbound addresses are known and stable (checked at token issuance; applicability to Code *inferred*); the recommended short Access Token Lifetime; refresh tokens only when needed; and **one OAuth2 Client per distribution**, so a leaked secret can be disabled and reset alone.

Verification: **Verification note.** **A token has a finite lifetime.** **The calling identity changes.** **The scheme changes from `Basic` to `Bearer`**: a Bearer header is not read by this tool again.

**If this code runs in a browser, the credential has been public for as long as the code has shipped.** The exposure and the rotation are stated here and the tool does not escalate it:

> Treat this credential as compromised. Rotate it, and assume the repository history has published it. Completing the migration does not close this exposure; rotation does.

OAuth2 Client: Create or confirm this OAuth2 Client in RelativityOne (NSerio's observation: the menu path is Home > Authentication > OAuth2 Client Manager, as Relativity's REST API authentication page names it). This tool never connects to RelativityOne and never creates or changes the OAuth2 Client; the settings below are guidance.
- Name: a descriptive name identifying the application it serves.
- Flow Grant Type: Code
- Redirect URLs: Relativity documentation: each URL includes the http or https protocol prefix. The exact callback URL is registered by the Relativity administrator. Server-side web applications, NSerio's recommendation: one exact HTTPS URL per environment, with no wildcards. Desktop and CLI applications: a plain-HTTP loopback URL, `http://127.0.0.1:<port>/<path>`, on a fixed port because the URL is registered in advance and the port cannot be chosen at runtime (verified by NSerio against a live instance; not documented by Relativity).
- Access Token Lifetime: 60 minutes, stated in minutes because the field is in minutes. Relativity documentation: tokens for this flow must have a short lifetime, and the recommended value matches the Identity Server default.
- Allow refresh tokens: NSerio's recommendation: leave it off unless the application must keep a user session active beyond the access token lifetime without asking the user to sign in again. If it is on, the refresh tokens are stored as secrets, per Secret Management (Part B of the Migration Documentation).
- Enabled: on.
<!-- generated:end JS-002 authorization-code -->

### JS-003 · CredentialsInRequestUrl

<a id="js-003-migration"></a>

Remove the user information from the address and send a Bearer header. Where the consumer is `fetch`, the call cannot have authenticated, because it throws a `TypeError` (checked by NSerio locally on one Node.js version); the entry says so, and the credential in the address is exposed whatever you do next, so rotate it.

#### Client Credentials

<!-- generated:begin JS-003 client-credentials -->
**Confirming the flow.** When no signal in a file decides the flow, the tool assigns Client Credentials, the default, with this note: "Who supplies the credential at run time? If a person types it or signs in, the integration acts for that person: use Authorization Code. If it comes from configuration, settings, an environment variable or a secret store: use Client Credentials. No signal in this file showed which applies, so Client Credentials, the default, is recommended. Use Authorization Code instead if the integration acts on behalf of a signed-in user, or if your RelativityOne instance already has a Code-flow OAuth2 Client for this integration." A fixed-credential signal assigns it with this note: "A fixed-credential signal in this file points to Client Credentials, but how often that kind of signal is right has not been measured. Use Authorization Code instead if the integration acts on behalf of a signed-in user, or if your RelativityOne instance already has a Code-flow OAuth2 Client for this integration." See [Confirming the flow](#confirming-the-flow) and the "Which flow" paragraph of JS-003 in [Part A](part-a-javascript.md#js-003).

Source form: **Applies to:** Node.js (the userinfo is sent); browser (`fetch` throws).

```javascript
http.get("https://exampleUser:exampleSecret@host.example/path", handler);
```

Under token authentication: **First, the destination:** if this Relativity instance is not RelativityOne — including a Relativity Server instance, whose own documentation governs — this finding is informational and the rest of this template does not apply.

```javascript
const CSRF_HEADER_VALUE = "-";
https.get("https://host.example/path", { headers: { Authorization: `Bearer ${accessToken}`, "X-CSRF-Header": CSRF_HEADER_VALUE } }, handler);
```

Remove the userinfo from the URL. **Where the consumer is `fetch`, this call cannot have authenticated: it throws a `TypeError`** (*checked by NSerio locally* on Node.js v24.11.1: "Request cannot be constructed from a URL that includes credentials"), so this finding is a bug report as much as a migration item; do not look for a working Basic call to migrate.

Credentials: **Where the credentials come from.** The text establishes that a client sends Basic, not that the destination is RelativityOne. If it is not RelativityOne, the migration is that service's own token mechanism, this finding is informational for this tool, and the reader stops here. If it is RelativityOne, the token comes from the token-acquisition part that follows, and the user name and password are replaced by the OAuth2 Client's client ID and secret, kept as Secret Management (Part B of the Migration Documentation) describes, never in source, in a tracked `.env` file or in a bundle.

**Where the token comes from.** Create an OAuth2 Client with Flow Grant Type set to Client Credentials (part 3 lists its settings), then send a form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=client_credentials`, `client_id` and `client_secret`. The token endpoint is **documented** for this grant. The token is then sent as `Authorization: Bearer <token>`, with `X-CSRF-Header: -` on REST and Kepler calls. **The scope depends on what the integration needs, and no single scope is required.** Relativity's documentation shows `SystemUserInfo` and `UserInfoAccess` and no page says which is correct or what either grants; use the value the API you call documents, and confirm it. The text has two variants, by where the code runs, and each opens with the precondition it is for: read that sentence and discard the variant that does not apply.

**Variant: a Node.js process acting as itself**

**Precondition: this variant is for a Node.js job, service or script that does not act for a signed-in person.**

```javascript
const IDENTITY_TOKEN_URL = "https://HOST/Relativity/Identity/connect/token";
const EXPIRY_MARGIN_SECONDS = 60;
const MILLISECONDS_PER_SECOND = 1000;
let cachedToken = null;

async function getAccessToken() {
	const now = Date.now();
	if (cachedToken === null || cachedToken.expiresAtMs - now < EXPIRY_MARGIN_SECONDS * MILLISECONDS_PER_SECOND) {
		const response = await fetch(IDENTITY_TOKEN_URL, {
			method: "POST",
			headers: { "Content-Type": "application/x-www-form-urlencoded" },
			body: new URLSearchParams({
				grant_type: "client_credentials",
				client_id: process.env.RELATIVITY_CLIENT_ID,
				client_secret: process.env.RELATIVITY_CLIENT_SECRET,
			}),
		});
		const payload = await response.json();
		cachedToken = {
			value: payload.access_token,
			expiresAtMs: now + payload.expires_in * MILLISECONDS_PER_SECOND,
		};
	}
	return cachedToken.value;
}
```

**Cache the token and re-acquire it on expiry — do not refresh.** Client Credentials offers no refresh tokens: **verified by NSerio against a live instance; not documented by Relativity** — the Allow Refresh Tokens control does not appear for this grant in the RelativityOne user interface. The recommended Access Token Lifetime is listed in part 3. The block uses the built-in `fetch` and no package. **The client secret** belongs in the process environment or a secret store, as Secret Management (Part B of the Migration Documentation) describes, **never in source, in a tracked `.env` file or in a bundle**. A reset invalidates the previous secret immediately, so plan the cut-over before resetting it. **Before deploying, confirm the calling identity:** every call now runs as the OAuth2 Client's Context User, not as the user in the replaced credential, and a non-administrator Context User is NSerio's recommendation. Trusted IPs restrict where a token can be requested, not a token already issued.

**Variant: browser code that does not act for a signed-in user**

**Precondition: this variant is for browser code that calls RelativityOne with no person signed in to it, so no person's identity exists to preserve. If a person signs in, use Authorization Code, variant 1.**

The Relativity request moves behind a **server-side component** that holds the secret and performs the token exchange (the Node.js process variant above); the browser calls that component on its own origin. **The component must authenticate and authorize its own callers, and the OAuth2 Client's Context User must hold the minimum permissions the operation needs. Without both, the result is an unauthenticated relay into RelativityOne:** every caller reaching it acts with the Context User's full rights, which is strictly worse than the starting point. The relay returns results to the browser, **never the token** and never the secret. For browser code the credential has been public for as long as the code has shipped; the tool does not escalate it, so this text states the exposure and the rotation:

> Treat this credential as compromised. Rotate it, and assume the repository history has published it. Completing the migration does not close this exposure; rotation does.

**When the code acts for a signed-in user.** Client Credentials is one service identity by construction. Code that asks a person for a password and sends it is the Authorization Code case, and moving to it means a new OAuth2 Client, because the flow is fixed when a client is created. The finding's flow note says when to make that choice.

Verification: **Verification note.** **A token has a finite lifetime.** **The calling identity changes.** What each client does with userinfo in a URL differs: `got`, `node-fetch` and the `request` function of `undici` are unchecked. **A credential in a URL is exposed whatever you do next:**

> Treat this credential as compromised. Rotate it, and assume the repository history has published it. Completing the migration does not close this exposure; rotation does.

OAuth2 Client: Create or confirm this OAuth2 Client in RelativityOne (NSerio's observation: the menu path is Home > Authentication > OAuth2 Client Manager, as Relativity's REST API authentication page names it). This tool never connects to RelativityOne and never creates or changes the OAuth2 Client; the settings below are guidance.
- Name: a descriptive name identifying the integration it serves. Use one OAuth2 Client per integration and do not share it across integrations.
- Flow Grant Type: Client Credentials
- Context User: NSerio's recommendation: a dedicated integration user with the minimum permissions the integration needs, and not a personal account and not an administrator, because every call runs with that user's permissions and an administrator Context User gives the integration an administrator's reach. Relativity documentation (REST API authentication page): describes the Context User as usually a member of the Relativity Administrators group, though it could differ depending on the API or operation invoked; NSerio recommends against that default for an integration.
- Access Token Lifetime: 60 minutes, stated in minutes because the field is in minutes. Relativity documentation: tokens for this flow must have a short lifetime, and the recommended value matches the Identity Server default.
- Trusted IPs: Relativity documentation: the address list is checked whenever a token is requested with the OAuth2 Client's ID and secret. NSerio's recommendation: populate it with the integration's outbound IP addresses, one per line, wherever they are known and stable. It restricts where a token can be requested, checked when the token is issued; it does not restrict a token already issued, which travels in the request header and is valid from anywhere until it expires.
- Allow refresh tokens: not used by this flow. The integration requests a new access token when the current one expires.
- Enabled: on.
- Every call made with this OAuth2 Client runs with the Context User's permissions.
- Relativity documentation: the flow is fixed when the OAuth2 Client is created and cannot be changed when editing it, so moving this integration to Authorization Code later means creating a new OAuth2 Client.
<!-- generated:end JS-003 client-credentials -->

#### Authorization Code

<!-- generated:begin JS-003 authorization-code -->
**Confirming the flow.** An interactive-user signal in a file assigns Authorization Code, with this note: "An interactive-user signal in this file points to Authorization Code. Confirm the flow: use Client Credentials instead if the integration runs with credentials fixed for the integration and no signed-in user." Your user profile can also make Authorization Code the default; see Changing the default in [Confirming the flow](#confirming-the-flow). See [Confirming the flow](#confirming-the-flow) and the "Which flow" paragraph of JS-003 in [Part A](part-a-javascript.md#js-003).

Source form: **Applies to:** Node.js (the userinfo is sent); browser (`fetch` throws).

```javascript
http.get("https://exampleUser:exampleSecret@host.example/path", handler);
```

Under token authentication: **First, the destination:** if this Relativity instance is not RelativityOne — including a Relativity Server instance, whose own documentation governs — this finding is informational and the rest of this template does not apply.

```javascript
const CSRF_HEADER_VALUE = "-";
https.get("https://host.example/path", { headers: { Authorization: `Bearer ${accessToken}`, "X-CSRF-Header": CSRF_HEADER_VALUE } }, handler);
```

Remove the userinfo from the URL. **Where the consumer is `fetch`, this call cannot have authenticated: it throws a `TypeError`** (*checked by NSerio locally* on Node.js v24.11.1: "Request cannot be constructed from a URL that includes credentials"), so this finding is a bug report as much as a migration item; do not look for a working Basic call to migrate.

Credentials: **Where the credentials come from.** The text establishes that a client sends Basic, not that the destination is RelativityOne. If it is not RelativityOne, the migration is that service's own token mechanism, this finding is informational for this tool, and the reader stops here. If it is RelativityOne, the token comes from the token-acquisition part that follows, and the user name and password are replaced by the OAuth2 Client's client ID and secret, kept as Secret Management (Part B of the Migration Documentation) describes, never in source, in a tracked `.env` file or in a bundle.

**Common to all three variants.** The text has a common part and **three variants by application type**, each opening with the precondition it is for: a browser with a server-side component, a Node.js server, and a Node.js command-line tool or desktop shell. Read the precondition of each and discard the variants that do not apply.

**Sign in.** Replace the code that collected the person's credentials with a redirect to the authorize endpoint `<host>/Relativity/Identity/connect/authorize` (**verified by NSerio against a live instance; not documented by Relativity**), which leads to RelativityOne's login page. The request carries the standard authorization-code parameters — `client_id`, `response_type=code`, the registered `redirect_uri`, `state` checked on return, and `code_challenge` with `code_challenge_method=S256` when PKCE is used; these are standard OAuth and Relativity documents none of them.

**PKCE.** PKCE is required by Relativity's own documentation to obtain a refresh token and not otherwise: a Code request that does not ask for a refresh token works without it, **verified by NSerio against a live instance; not documented by Relativity**. NSerio recommends `S256` (NSerio recommendation) and does not claim that Relativity accepts any particular method. `state` is generated, stored against the session and compared on return; `nonce` is not used, and Relativity does not document whether it enforces either.

**The client secret is required.** Code does not work without a client secret: tested and rejected, **verified by NSerio against a live instance; not documented by Relativity**, which confirms Relativity's own statement. **Any secret shipped to a browser is public**, and bundling, minifying or obfuscating changes nothing, so the exchange runs where a secret can be kept.

**Exchange the code.** A form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=authorization_code`, the code, the same `redirect_uri`, the client ID and the secret, plus the `code_verifier` when PKCE is used (**verified by NSerio against a live instance; not documented by Relativity**).

**Redirect URL.** A server-side application registers **one exact HTTPS URL per environment, no wildcards**; the matching rules are not documented and no claim is made about them. A plain-HTTP loopback Redirect URL `http://127.0.0.1:<port>/<path>` on a **fixed registered port** is accepted, **verified by NSerio against a live instance; not documented by Relativity**, and is the desktop shape.

**On expiry.** With Allow Refresh Tokens enabled and PKCE used, the code holds a refresh token and sends `grant_type=refresh_token` in place of the code exchange: *inferred* from the standard OAuth shape, not tested. Relativity does not document a refresh token's lifetime, rotation or revocation, nor what disabling the client does to tokens already issued. **A refresh token is a secret.** **Single sign-on:** sign-in happens on RelativityOne's own page, so the instance's single sign-on and two-factor rules apply unchanged (*inferred*). **The code stops collecting the password:** after this change nothing receives, stores or forwards the person's user name and password, and the code holds only tokens. Every call now runs as the person who signed in, so **the calling identity changes**. **Scope:** no scope set is prescribed for Code; `openid RelativityRequestOrigin UserInfo UserInfoAccess AuthenticationID RelativityWeb` works, **verified by NSerio against a live instance; not documented by Relativity**, as working and not as minimal.

**Variant 1 — Browser with a server-side component**

**Precondition: this variant is for code that runs in a web browser and signs a person in to RelativityOne. If your code runs in Node.js, use variant 2 or variant 3.**

A browser cannot hold the secret the Code grant requires, so **the browser never calls RelativityOne**: it navigates to a route on **the application's own origin**, and a **server-side component** (variant 2's code) creates `state`, redirects to the authorize endpoint, receives the code at its registered HTTPS Redirect URL, exchanges it, keeps the tokens and gives the browser only its own session. **The browser holds a session identifier with that component and nothing from RelativityOne.** The authorize step is a **top-level navigation** (a full-window redirect, not a `fetch`).

```javascript
const LOGIN_ROUTE = "/auth/login";
const API_ROUTE = "/api/documents";

function signIn() {
	window.location.assign(LOGIN_ROUTE);
}

async function loadDocuments() {
	const response = await fetch(API_ROUTE, { credentials: "same-origin" });
	return response.json();
}
```

Both calls go to the page's own origin. `credentials: "include"` is a cookie policy and is not recommended here, because the architecture needs no cross-origin call. **Cross-origin access, both ways, both not verified:** NSerio has not run a preflight against a live instance. *If RelativityOne serves CORS to browser callers*, the legacy browser Basic call could have worked from another origin, and a browser holding a Bearer token could in principle call it directly; **this text still does not recommend that**, because the only grant that yields a token for a person needs the secret the browser cannot hold. *If it does not*, the legacy call worked only from the same origin or through a proxy the client already runs. **The recommended architecture holds under either answer.** **The component keeps the tokens on the server**, never in browser storage, which any script on the page can read (a browser behaviour, unchecked); the recommended session cookie is `HttpOnly`, `Secure` and `SameSite` (NSerio recommendation; unchecked).

**Variant 2 — Node.js server**

**Precondition: this variant is for code that runs in Node.js as a web server and acts for people who sign in to it. It is not for browser code (variant 1) or a command-line tool (variant 3).**

Standard server-side Code flow. A login route builds the authorize address with `state` (and the PKCE verifier where a refresh token is wanted), stores them against the visitor's session and redirects; RelativityOne redirects to the **callback route**, registered as an exact HTTPS URL; the callback checks `state` and exchanges the code with the secret. This is the component variant 1 relies on.

```javascript
import crypto from "node:crypto";

const IDENTITY_BASE = "https://HOST/Relativity/Identity/connect";
const REDIRECT_URI = "https://app.example.test/auth/callback";
const CLIENT_ID = process.env.RELATIVITY_CLIENT_ID;
const CLIENT_SECRET = process.env.RELATIVITY_CLIENT_SECRET;
const RANDOM_BYTES = 32;
const pendingBySession = new Map();

function base64Url(buffer) {
	return buffer.toString("base64url");
}

function beginLogin(sessionId, wantRefreshToken) {
	const state = base64Url(crypto.randomBytes(RANDOM_BYTES));
	const verifier = base64Url(crypto.randomBytes(RANDOM_BYTES));
	const challenge = base64Url(crypto.createHash("sha256").update(verifier).digest());
	pendingBySession.set(sessionId, { state, verifier });
	const query = new URLSearchParams({
		response_type: "code",
		client_id: CLIENT_ID,
		redirect_uri: REDIRECT_URI,
		state,
	});
	if (wantRefreshToken) {
		query.set("code_challenge", challenge);
		query.set("code_challenge_method", "S256");
	}
	return `${IDENTITY_BASE}/authorize?${query}`;
}

async function completeLogin(sessionId, returnedCode, returnedState) {
	const pending = pendingBySession.get(sessionId);
	pendingBySession.delete(sessionId);
	const stateMatches = pending !== undefined
		&& returnedState !== undefined
		&& Buffer.byteLength(returnedState) === Buffer.byteLength(pending.state)
		&& crypto.timingSafeEqual(Buffer.from(returnedState), Buffer.from(pending.state));
	let tokens = null;
	if (stateMatches) {
		const response = await fetch(`${IDENTITY_BASE}/token`, {
			method: "POST",
			headers: { "Content-Type": "application/x-www-form-urlencoded" },
			body: new URLSearchParams({
				grant_type: "authorization_code",
				code: returnedCode,
				redirect_uri: REDIRECT_URI,
				client_id: CLIENT_ID,
				client_secret: CLIENT_SECRET,
				code_verifier: pending.verifier,
			}),
		});
		tokens = response.ok ? await response.json() : null;
	}
	return tokens;
}
```

`state` is consumed once and compared on return; a request with no pending entry or a different `state` gets no exchange. **The scope is not prescribed** (the sketch sends none). **Where the secret travels in the token request** (the body, as sketched, or HTTP Basic client authentication) is **not verified**. The sketch does not show the HTTP server, the session cookie or a session store that survives more than one process. **Tokens stay on the server**, in a session store that is not exposed to a request.

**Variant 3 — Node.js command line or desktop**

**Precondition: this variant is for code that runs on a person's own machine, as a command-line tool or a desktop shell, and signs that person in. It is not for a web server (variant 2) or browser code (variant 1).**

The process starts a listener from the built-in `node:http` module, bound to **`127.0.0.1` only**, on the **fixed registered port**, for **one request**; opens the system browser on the authorize address **and always prints the address too**, so a machine with no browser, an SSH session or a failed launch has a manual route; reads `code` and `state` from the one request, closes the listener, checks `state`, and exchanges the code as variant 2's `completeLogin` does.

```javascript
import http from "node:http";

const LOOPBACK_HOST = "127.0.0.1";
const REGISTERED_PORT = 8123;
const CALLBACK_PATH = "/callback";
const BASE_FOR_PARSING = `http://${LOOPBACK_HOST}:${REGISTERED_PORT}`;
const WAIT_MILLISECONDS = 120000;
const STATUS_OK = 200;
const STATUS_NOT_FOUND = 404;

function waitForRedirect() {
	return new Promise((resolve, reject) => {
		const server = http.createServer((request, response) => {
			const address = new URL(request.url, BASE_FOR_PARSING);
			const isCallback = address.pathname === CALLBACK_PATH;
			response.statusCode = isCallback ? STATUS_OK : STATUS_NOT_FOUND;
			response.end(isCallback ? "You can close this window." : "Not found");
			if (isCallback) {
				server.close();
				resolve({
					code: address.searchParams.get("code"),
					state: address.searchParams.get("state"),
				});
			}
		});
		server.on("error", reject);
		server.listen(REGISTERED_PORT, LOOPBACK_HOST);
		setTimeout(() => {
			server.close();
			reject(new Error("No redirect arrived"));
		}, WAIT_MILLISECONDS).unref();
	});
}
```

**Rules for the listener:** bind to loopback only; accept one request; **check `state`** (another local process can connect to a fixed, advertised port); close the listener after the request; and handle a busy port (a second server on a port in use raises `EADDRINUSE`, so fall back to the printed address). **Opening the system browser:** Node.js core has no function for it; run the platform's own launcher with a fixed program and an argument array, never a shell string built from the address (unchecked on every system). **Where tokens are kept:** a file with restricted permissions or the operating system's keychain; on Windows the POSIX mode argument of Node.js did not restrict a file when it was tried (*checked by NSerio locally* on Node.js v24.11.1), so a restricted file there needs an access-control change by other means, and on Linux and macOS it is unchecked. **The client secret of a distributed tool is extractable:** a command-line tool or desktop shell shipped to people carries its client secret in a form anyone who holds the distribution can extract, and it protects nothing against the person running it. **A known issue:** Relativity documents no recommendation for a distributed client; this design is what NSerio verified works. **Compensating guidance** (NSerio recommendation, subject to your own security policy): Trusted IPs where the users' outbound addresses are known and stable (checked at token issuance; applicability to Code *inferred*); the recommended short Access Token Lifetime; refresh tokens only when needed; and **one OAuth2 Client per distribution**, so a leaked secret can be disabled and reset alone.

Verification: **Verification note.** **A token has a finite lifetime.** **The calling identity changes.** What each client does with userinfo in a URL differs: `got`, `node-fetch` and the `request` function of `undici` are unchecked. **A credential in a URL is exposed whatever you do next:**

> Treat this credential as compromised. Rotate it, and assume the repository history has published it. Completing the migration does not close this exposure; rotation does.

OAuth2 Client: Create or confirm this OAuth2 Client in RelativityOne (NSerio's observation: the menu path is Home > Authentication > OAuth2 Client Manager, as Relativity's REST API authentication page names it). This tool never connects to RelativityOne and never creates or changes the OAuth2 Client; the settings below are guidance.
- Name: a descriptive name identifying the application it serves.
- Flow Grant Type: Code
- Redirect URLs: Relativity documentation: each URL includes the http or https protocol prefix. The exact callback URL is registered by the Relativity administrator. Server-side web applications, NSerio's recommendation: one exact HTTPS URL per environment, with no wildcards. Desktop and CLI applications: a plain-HTTP loopback URL, `http://127.0.0.1:<port>/<path>`, on a fixed port because the URL is registered in advance and the port cannot be chosen at runtime (verified by NSerio against a live instance; not documented by Relativity).
- Access Token Lifetime: 60 minutes, stated in minutes because the field is in minutes. Relativity documentation: tokens for this flow must have a short lifetime, and the recommended value matches the Identity Server default.
- Allow refresh tokens: NSerio's recommendation: leave it off unless the application must keep a user session active beyond the access token lifetime without asking the user to sign in again. If it is on, the refresh tokens are stored as secrets, per Secret Management (Part B of the Migration Documentation).
- Enabled: on.
<!-- generated:end JS-003 authorization-code -->

### JS-004 · AuthorizationHeaderScheme

<a id="js-004-migration"></a>

Change the scheme word from `Basic` to `Bearer`, send the access token, and remove the code that encoded the user name and password. A header set by a function call, a hook or an interceptor is not read, so check those places by hand.

#### Client Credentials

<!-- generated:begin JS-004 client-credentials -->
**Confirming the flow.** When no signal in a file decides the flow, the tool assigns Client Credentials, the default, with this note: "Who supplies the credential at run time? If a person types it or signs in, the integration acts for that person: use Authorization Code. If it comes from configuration, settings, an environment variable or a secret store: use Client Credentials. No signal in this file showed which applies, so Client Credentials, the default, is recommended. Use Authorization Code instead if the integration acts on behalf of a signed-in user, or if your RelativityOne instance already has a Code-flow OAuth2 Client for this integration." A fixed-credential signal assigns it with this note: "A fixed-credential signal in this file points to Client Credentials, but how often that kind of signal is right has not been measured. Use Authorization Code instead if the integration acts on behalf of a signed-in user, or if your RelativityOne instance already has a Code-flow OAuth2 Client for this integration." See [Confirming the flow](#confirming-the-flow) and the "Which flow" paragraph of JS-004 in [Part A](part-a-javascript.md#js-004).

Source form: **Applies to:** browser and Node.js.

```javascript
await fetch(url, { headers: { Authorization: `Basic ${encoded}` } });
xhr.setRequestHeader("Authorization", "Basic " + encoded);
```

Under token authentication: **First, the destination:** if this Relativity instance is not RelativityOne — including a Relativity Server instance, whose own documentation governs — this finding is informational and the rest of this template does not apply.

*Node.js:*

```javascript
const CSRF_HEADER_VALUE = "-";
await fetch(url, { headers: { Authorization: `Bearer ${accessToken}`, "X-CSRF-Header": CSRF_HEADER_VALUE } });
```

*Browser:* the call moves behind the server-side component and the browser calls its own origin with no `Authorization` header, as above. Remove the code that encoded the user name and password.

Credentials: **Where the credentials come from.** The text establishes that a client sends Basic, not that the destination is RelativityOne. If it is not RelativityOne, the migration is that service's own token mechanism, this finding is informational for this tool, and the reader stops here. If it is RelativityOne, the token comes from the token-acquisition part that follows, and the user name and password are replaced by the OAuth2 Client's client ID and secret, kept as Secret Management (Part B of the Migration Documentation) describes, never in source, in a tracked `.env` file or in a bundle.

**Where the token comes from.** Create an OAuth2 Client with Flow Grant Type set to Client Credentials (part 3 lists its settings), then send a form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=client_credentials`, `client_id` and `client_secret`. The token endpoint is **documented** for this grant. The token is then sent as `Authorization: Bearer <token>`, with `X-CSRF-Header: -` on REST and Kepler calls. **The scope depends on what the integration needs, and no single scope is required.** Relativity's documentation shows `SystemUserInfo` and `UserInfoAccess` and no page says which is correct or what either grants; use the value the API you call documents, and confirm it. The text has two variants, by where the code runs, and each opens with the precondition it is for: read that sentence and discard the variant that does not apply.

**Variant: a Node.js process acting as itself**

**Precondition: this variant is for a Node.js job, service or script that does not act for a signed-in person.**

```javascript
const IDENTITY_TOKEN_URL = "https://HOST/Relativity/Identity/connect/token";
const EXPIRY_MARGIN_SECONDS = 60;
const MILLISECONDS_PER_SECOND = 1000;
let cachedToken = null;

async function getAccessToken() {
	const now = Date.now();
	if (cachedToken === null || cachedToken.expiresAtMs - now < EXPIRY_MARGIN_SECONDS * MILLISECONDS_PER_SECOND) {
		const response = await fetch(IDENTITY_TOKEN_URL, {
			method: "POST",
			headers: { "Content-Type": "application/x-www-form-urlencoded" },
			body: new URLSearchParams({
				grant_type: "client_credentials",
				client_id: process.env.RELATIVITY_CLIENT_ID,
				client_secret: process.env.RELATIVITY_CLIENT_SECRET,
			}),
		});
		const payload = await response.json();
		cachedToken = {
			value: payload.access_token,
			expiresAtMs: now + payload.expires_in * MILLISECONDS_PER_SECOND,
		};
	}
	return cachedToken.value;
}
```

**Cache the token and re-acquire it on expiry — do not refresh.** Client Credentials offers no refresh tokens: **verified by NSerio against a live instance; not documented by Relativity** — the Allow Refresh Tokens control does not appear for this grant in the RelativityOne user interface. The recommended Access Token Lifetime is listed in part 3. The block uses the built-in `fetch` and no package. **The client secret** belongs in the process environment or a secret store, as Secret Management (Part B of the Migration Documentation) describes, **never in source, in a tracked `.env` file or in a bundle**. A reset invalidates the previous secret immediately, so plan the cut-over before resetting it. **Before deploying, confirm the calling identity:** every call now runs as the OAuth2 Client's Context User, not as the user in the replaced credential, and a non-administrator Context User is NSerio's recommendation. Trusted IPs restrict where a token can be requested, not a token already issued.

**Variant: browser code that does not act for a signed-in user**

**Precondition: this variant is for browser code that calls RelativityOne with no person signed in to it, so no person's identity exists to preserve. If a person signs in, use Authorization Code, variant 1.**

The Relativity request moves behind a **server-side component** that holds the secret and performs the token exchange (the Node.js process variant above); the browser calls that component on its own origin. **The component must authenticate and authorize its own callers, and the OAuth2 Client's Context User must hold the minimum permissions the operation needs. Without both, the result is an unauthenticated relay into RelativityOne:** every caller reaching it acts with the Context User's full rights, which is strictly worse than the starting point. The relay returns results to the browser, **never the token** and never the secret. For browser code the credential has been public for as long as the code has shipped; the tool does not escalate it, so this text states the exposure and the rotation:

> Treat this credential as compromised. Rotate it, and assume the repository history has published it. Completing the migration does not close this exposure; rotation does.

**When the code acts for a signed-in user.** Client Credentials is one service identity by construction. Code that asks a person for a password and sends it is the Authorization Code case, and moving to it means a new OAuth2 Client, because the flow is fixed when a client is created. The finding's flow note says when to make that choice.

Verification: **Verification note.** **A token has a finite lifetime.** **The calling identity changes.** **The scheme word changes from `Basic` to `Bearer`.** **A header set by a function call, a hook or an interceptor is not read.**

**If this code runs in a browser, the credential has been public for as long as the code has shipped.** The exposure and the rotation are stated here and the tool does not escalate it:

> Treat this credential as compromised. Rotate it, and assume the repository history has published it. Completing the migration does not close this exposure; rotation does.

OAuth2 Client: Create or confirm this OAuth2 Client in RelativityOne (NSerio's observation: the menu path is Home > Authentication > OAuth2 Client Manager, as Relativity's REST API authentication page names it). This tool never connects to RelativityOne and never creates or changes the OAuth2 Client; the settings below are guidance.
- Name: a descriptive name identifying the integration it serves. Use one OAuth2 Client per integration and do not share it across integrations.
- Flow Grant Type: Client Credentials
- Context User: NSerio's recommendation: a dedicated integration user with the minimum permissions the integration needs, and not a personal account and not an administrator, because every call runs with that user's permissions and an administrator Context User gives the integration an administrator's reach. Relativity documentation (REST API authentication page): describes the Context User as usually a member of the Relativity Administrators group, though it could differ depending on the API or operation invoked; NSerio recommends against that default for an integration.
- Access Token Lifetime: 60 minutes, stated in minutes because the field is in minutes. Relativity documentation: tokens for this flow must have a short lifetime, and the recommended value matches the Identity Server default.
- Trusted IPs: Relativity documentation: the address list is checked whenever a token is requested with the OAuth2 Client's ID and secret. NSerio's recommendation: populate it with the integration's outbound IP addresses, one per line, wherever they are known and stable. It restricts where a token can be requested, checked when the token is issued; it does not restrict a token already issued, which travels in the request header and is valid from anywhere until it expires.
- Allow refresh tokens: not used by this flow. The integration requests a new access token when the current one expires.
- Enabled: on.
- Every call made with this OAuth2 Client runs with the Context User's permissions.
- Relativity documentation: the flow is fixed when the OAuth2 Client is created and cannot be changed when editing it, so moving this integration to Authorization Code later means creating a new OAuth2 Client.
<!-- generated:end JS-004 client-credentials -->

#### Authorization Code

<!-- generated:begin JS-004 authorization-code -->
**Confirming the flow.** An interactive-user signal in a file assigns Authorization Code, with this note: "An interactive-user signal in this file points to Authorization Code. Confirm the flow: use Client Credentials instead if the integration runs with credentials fixed for the integration and no signed-in user." Your user profile can also make Authorization Code the default; see Changing the default in [Confirming the flow](#confirming-the-flow). See [Confirming the flow](#confirming-the-flow) and the "Which flow" paragraph of JS-004 in [Part A](part-a-javascript.md#js-004).

Source form: **Applies to:** browser and Node.js.

```javascript
await fetch(url, { headers: { Authorization: `Basic ${encoded}` } });
xhr.setRequestHeader("Authorization", "Basic " + encoded);
```

Under token authentication: **First, the destination:** if this Relativity instance is not RelativityOne — including a Relativity Server instance, whose own documentation governs — this finding is informational and the rest of this template does not apply.

*Node.js:*

```javascript
const CSRF_HEADER_VALUE = "-";
await fetch(url, { headers: { Authorization: `Bearer ${accessToken}`, "X-CSRF-Header": CSRF_HEADER_VALUE } });
```

*Browser:* the call moves behind the server-side component and the browser calls its own origin with no `Authorization` header, as above. Remove the code that encoded the user name and password.

Credentials: **Where the credentials come from.** The text establishes that a client sends Basic, not that the destination is RelativityOne. If it is not RelativityOne, the migration is that service's own token mechanism, this finding is informational for this tool, and the reader stops here. If it is RelativityOne, the token comes from the token-acquisition part that follows, and the user name and password are replaced by the OAuth2 Client's client ID and secret, kept as Secret Management (Part B of the Migration Documentation) describes, never in source, in a tracked `.env` file or in a bundle.

**Common to all three variants.** The text has a common part and **three variants by application type**, each opening with the precondition it is for: a browser with a server-side component, a Node.js server, and a Node.js command-line tool or desktop shell. Read the precondition of each and discard the variants that do not apply.

**Sign in.** Replace the code that collected the person's credentials with a redirect to the authorize endpoint `<host>/Relativity/Identity/connect/authorize` (**verified by NSerio against a live instance; not documented by Relativity**), which leads to RelativityOne's login page. The request carries the standard authorization-code parameters — `client_id`, `response_type=code`, the registered `redirect_uri`, `state` checked on return, and `code_challenge` with `code_challenge_method=S256` when PKCE is used; these are standard OAuth and Relativity documents none of them.

**PKCE.** PKCE is required by Relativity's own documentation to obtain a refresh token and not otherwise: a Code request that does not ask for a refresh token works without it, **verified by NSerio against a live instance; not documented by Relativity**. NSerio recommends `S256` (NSerio recommendation) and does not claim that Relativity accepts any particular method. `state` is generated, stored against the session and compared on return; `nonce` is not used, and Relativity does not document whether it enforces either.

**The client secret is required.** Code does not work without a client secret: tested and rejected, **verified by NSerio against a live instance; not documented by Relativity**, which confirms Relativity's own statement. **Any secret shipped to a browser is public**, and bundling, minifying or obfuscating changes nothing, so the exchange runs where a secret can be kept.

**Exchange the code.** A form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=authorization_code`, the code, the same `redirect_uri`, the client ID and the secret, plus the `code_verifier` when PKCE is used (**verified by NSerio against a live instance; not documented by Relativity**).

**Redirect URL.** A server-side application registers **one exact HTTPS URL per environment, no wildcards**; the matching rules are not documented and no claim is made about them. A plain-HTTP loopback Redirect URL `http://127.0.0.1:<port>/<path>` on a **fixed registered port** is accepted, **verified by NSerio against a live instance; not documented by Relativity**, and is the desktop shape.

**On expiry.** With Allow Refresh Tokens enabled and PKCE used, the code holds a refresh token and sends `grant_type=refresh_token` in place of the code exchange: *inferred* from the standard OAuth shape, not tested. Relativity does not document a refresh token's lifetime, rotation or revocation, nor what disabling the client does to tokens already issued. **A refresh token is a secret.** **Single sign-on:** sign-in happens on RelativityOne's own page, so the instance's single sign-on and two-factor rules apply unchanged (*inferred*). **The code stops collecting the password:** after this change nothing receives, stores or forwards the person's user name and password, and the code holds only tokens. Every call now runs as the person who signed in, so **the calling identity changes**. **Scope:** no scope set is prescribed for Code; `openid RelativityRequestOrigin UserInfo UserInfoAccess AuthenticationID RelativityWeb` works, **verified by NSerio against a live instance; not documented by Relativity**, as working and not as minimal.

**Variant 1 — Browser with a server-side component**

**Precondition: this variant is for code that runs in a web browser and signs a person in to RelativityOne. If your code runs in Node.js, use variant 2 or variant 3.**

A browser cannot hold the secret the Code grant requires, so **the browser never calls RelativityOne**: it navigates to a route on **the application's own origin**, and a **server-side component** (variant 2's code) creates `state`, redirects to the authorize endpoint, receives the code at its registered HTTPS Redirect URL, exchanges it, keeps the tokens and gives the browser only its own session. **The browser holds a session identifier with that component and nothing from RelativityOne.** The authorize step is a **top-level navigation** (a full-window redirect, not a `fetch`).

```javascript
const LOGIN_ROUTE = "/auth/login";
const API_ROUTE = "/api/documents";

function signIn() {
	window.location.assign(LOGIN_ROUTE);
}

async function loadDocuments() {
	const response = await fetch(API_ROUTE, { credentials: "same-origin" });
	return response.json();
}
```

Both calls go to the page's own origin. `credentials: "include"` is a cookie policy and is not recommended here, because the architecture needs no cross-origin call. **Cross-origin access, both ways, both not verified:** NSerio has not run a preflight against a live instance. *If RelativityOne serves CORS to browser callers*, the legacy browser Basic call could have worked from another origin, and a browser holding a Bearer token could in principle call it directly; **this text still does not recommend that**, because the only grant that yields a token for a person needs the secret the browser cannot hold. *If it does not*, the legacy call worked only from the same origin or through a proxy the client already runs. **The recommended architecture holds under either answer.** **The component keeps the tokens on the server**, never in browser storage, which any script on the page can read (a browser behaviour, unchecked); the recommended session cookie is `HttpOnly`, `Secure` and `SameSite` (NSerio recommendation; unchecked).

**Variant 2 — Node.js server**

**Precondition: this variant is for code that runs in Node.js as a web server and acts for people who sign in to it. It is not for browser code (variant 1) or a command-line tool (variant 3).**

Standard server-side Code flow. A login route builds the authorize address with `state` (and the PKCE verifier where a refresh token is wanted), stores them against the visitor's session and redirects; RelativityOne redirects to the **callback route**, registered as an exact HTTPS URL; the callback checks `state` and exchanges the code with the secret. This is the component variant 1 relies on.

```javascript
import crypto from "node:crypto";

const IDENTITY_BASE = "https://HOST/Relativity/Identity/connect";
const REDIRECT_URI = "https://app.example.test/auth/callback";
const CLIENT_ID = process.env.RELATIVITY_CLIENT_ID;
const CLIENT_SECRET = process.env.RELATIVITY_CLIENT_SECRET;
const RANDOM_BYTES = 32;
const pendingBySession = new Map();

function base64Url(buffer) {
	return buffer.toString("base64url");
}

function beginLogin(sessionId, wantRefreshToken) {
	const state = base64Url(crypto.randomBytes(RANDOM_BYTES));
	const verifier = base64Url(crypto.randomBytes(RANDOM_BYTES));
	const challenge = base64Url(crypto.createHash("sha256").update(verifier).digest());
	pendingBySession.set(sessionId, { state, verifier });
	const query = new URLSearchParams({
		response_type: "code",
		client_id: CLIENT_ID,
		redirect_uri: REDIRECT_URI,
		state,
	});
	if (wantRefreshToken) {
		query.set("code_challenge", challenge);
		query.set("code_challenge_method", "S256");
	}
	return `${IDENTITY_BASE}/authorize?${query}`;
}

async function completeLogin(sessionId, returnedCode, returnedState) {
	const pending = pendingBySession.get(sessionId);
	pendingBySession.delete(sessionId);
	const stateMatches = pending !== undefined
		&& returnedState !== undefined
		&& Buffer.byteLength(returnedState) === Buffer.byteLength(pending.state)
		&& crypto.timingSafeEqual(Buffer.from(returnedState), Buffer.from(pending.state));
	let tokens = null;
	if (stateMatches) {
		const response = await fetch(`${IDENTITY_BASE}/token`, {
			method: "POST",
			headers: { "Content-Type": "application/x-www-form-urlencoded" },
			body: new URLSearchParams({
				grant_type: "authorization_code",
				code: returnedCode,
				redirect_uri: REDIRECT_URI,
				client_id: CLIENT_ID,
				client_secret: CLIENT_SECRET,
				code_verifier: pending.verifier,
			}),
		});
		tokens = response.ok ? await response.json() : null;
	}
	return tokens;
}
```

`state` is consumed once and compared on return; a request with no pending entry or a different `state` gets no exchange. **The scope is not prescribed** (the sketch sends none). **Where the secret travels in the token request** (the body, as sketched, or HTTP Basic client authentication) is **not verified**. The sketch does not show the HTTP server, the session cookie or a session store that survives more than one process. **Tokens stay on the server**, in a session store that is not exposed to a request.

**Variant 3 — Node.js command line or desktop**

**Precondition: this variant is for code that runs on a person's own machine, as a command-line tool or a desktop shell, and signs that person in. It is not for a web server (variant 2) or browser code (variant 1).**

The process starts a listener from the built-in `node:http` module, bound to **`127.0.0.1` only**, on the **fixed registered port**, for **one request**; opens the system browser on the authorize address **and always prints the address too**, so a machine with no browser, an SSH session or a failed launch has a manual route; reads `code` and `state` from the one request, closes the listener, checks `state`, and exchanges the code as variant 2's `completeLogin` does.

```javascript
import http from "node:http";

const LOOPBACK_HOST = "127.0.0.1";
const REGISTERED_PORT = 8123;
const CALLBACK_PATH = "/callback";
const BASE_FOR_PARSING = `http://${LOOPBACK_HOST}:${REGISTERED_PORT}`;
const WAIT_MILLISECONDS = 120000;
const STATUS_OK = 200;
const STATUS_NOT_FOUND = 404;

function waitForRedirect() {
	return new Promise((resolve, reject) => {
		const server = http.createServer((request, response) => {
			const address = new URL(request.url, BASE_FOR_PARSING);
			const isCallback = address.pathname === CALLBACK_PATH;
			response.statusCode = isCallback ? STATUS_OK : STATUS_NOT_FOUND;
			response.end(isCallback ? "You can close this window." : "Not found");
			if (isCallback) {
				server.close();
				resolve({
					code: address.searchParams.get("code"),
					state: address.searchParams.get("state"),
				});
			}
		});
		server.on("error", reject);
		server.listen(REGISTERED_PORT, LOOPBACK_HOST);
		setTimeout(() => {
			server.close();
			reject(new Error("No redirect arrived"));
		}, WAIT_MILLISECONDS).unref();
	});
}
```

**Rules for the listener:** bind to loopback only; accept one request; **check `state`** (another local process can connect to a fixed, advertised port); close the listener after the request; and handle a busy port (a second server on a port in use raises `EADDRINUSE`, so fall back to the printed address). **Opening the system browser:** Node.js core has no function for it; run the platform's own launcher with a fixed program and an argument array, never a shell string built from the address (unchecked on every system). **Where tokens are kept:** a file with restricted permissions or the operating system's keychain; on Windows the POSIX mode argument of Node.js did not restrict a file when it was tried (*checked by NSerio locally* on Node.js v24.11.1), so a restricted file there needs an access-control change by other means, and on Linux and macOS it is unchecked. **The client secret of a distributed tool is extractable:** a command-line tool or desktop shell shipped to people carries its client secret in a form anyone who holds the distribution can extract, and it protects nothing against the person running it. **A known issue:** Relativity documents no recommendation for a distributed client; this design is what NSerio verified works. **Compensating guidance** (NSerio recommendation, subject to your own security policy): Trusted IPs where the users' outbound addresses are known and stable (checked at token issuance; applicability to Code *inferred*); the recommended short Access Token Lifetime; refresh tokens only when needed; and **one OAuth2 Client per distribution**, so a leaked secret can be disabled and reset alone.

Verification: **Verification note.** **A token has a finite lifetime.** **The calling identity changes.** **The scheme word changes from `Basic` to `Bearer`.** **A header set by a function call, a hook or an interceptor is not read.**

**If this code runs in a browser, the credential has been public for as long as the code has shipped.** The exposure and the rotation are stated here and the tool does not escalate it:

> Treat this credential as compromised. Rotate it, and assume the repository history has published it. Completing the migration does not close this exposure; rotation does.

OAuth2 Client: Create or confirm this OAuth2 Client in RelativityOne (NSerio's observation: the menu path is Home > Authentication > OAuth2 Client Manager, as Relativity's REST API authentication page names it). This tool never connects to RelativityOne and never creates or changes the OAuth2 Client; the settings below are guidance.
- Name: a descriptive name identifying the application it serves.
- Flow Grant Type: Code
- Redirect URLs: Relativity documentation: each URL includes the http or https protocol prefix. The exact callback URL is registered by the Relativity administrator. Server-side web applications, NSerio's recommendation: one exact HTTPS URL per environment, with no wildcards. Desktop and CLI applications: a plain-HTTP loopback URL, `http://127.0.0.1:<port>/<path>`, on a fixed port because the URL is registered in advance and the port cannot be chosen at runtime (verified by NSerio against a live instance; not documented by Relativity).
- Access Token Lifetime: 60 minutes, stated in minutes because the field is in minutes. Relativity documentation: tokens for this flow must have a short lifetime, and the recommended value matches the Identity Server default.
- Allow refresh tokens: NSerio's recommendation: leave it off unless the application must keep a user session active beyond the access token lifetime without asking the user to sign in again. If it is on, the refresh tokens are stored as secrets, per Secret Management (Part B of the Migration Documentation).
- Enabled: on.
<!-- generated:end JS-004 authorization-code -->

### JS-005 · Base64UserPasswordPair

<a id="js-005-migration"></a>

Remove the pair, the `btoa` encoding and the variable that holds it, and send the token. If the encoded pair is not a credential, the finding is a false positive and nothing changes. Follow the encoded value to every place it is sent and change those uses too.

#### Client Credentials

<!-- generated:begin JS-005 client-credentials -->
**Confirming the flow.** When no signal in a file decides the flow, the tool assigns Client Credentials, the default, with this note: "Who supplies the credential at run time? If a person types it or signs in, the integration acts for that person: use Authorization Code. If it comes from configuration, settings, an environment variable or a secret store: use Client Credentials. No signal in this file showed which applies, so Client Credentials, the default, is recommended. Use Authorization Code instead if the integration acts on behalf of a signed-in user, or if your RelativityOne instance already has a Code-flow OAuth2 Client for this integration." A fixed-credential signal assigns it with this note: "A fixed-credential signal in this file points to Client Credentials, but how often that kind of signal is right has not been measured. Use Authorization Code instead if the integration acts on behalf of a signed-in user, or if your RelativityOne instance already has a Code-flow OAuth2 Client for this integration." See [Confirming the flow](#confirming-the-flow) and the "Which flow" paragraph of JS-005 in [Part A](part-a-javascript.md#js-005).

Source form: **Applies to:** browser and Node.js.

```javascript
const token = btoa(userName + ":" + secret);
const header = "Basic " + token;
```

Under token authentication: **First, the destination:** if this Relativity instance is not RelativityOne — including a Relativity Server instance, whose own documentation governs — this finding is informational and the rest of this template does not apply. **If the encoded pair is not a credential, this finding is a false positive and nothing changes.**

Remove the pair, the encoding and the variable, and send the token: `Authorization: Bearer ${accessToken}` with `X-CSRF-Header: -`. In a browser the call moves behind the server-side component.

Credentials: **Where the credentials come from.** The text establishes that a client sends Basic, not that the destination is RelativityOne. If it is not RelativityOne, the migration is that service's own token mechanism, this finding is informational for this tool, and the reader stops here. If it is RelativityOne, the token comes from the token-acquisition part that follows, and the user name and password are replaced by the OAuth2 Client's client ID and secret, kept as Secret Management (Part B of the Migration Documentation) describes, never in source, in a tracked `.env` file or in a bundle.

**Where the token comes from.** Create an OAuth2 Client with Flow Grant Type set to Client Credentials (part 3 lists its settings), then send a form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=client_credentials`, `client_id` and `client_secret`. The token endpoint is **documented** for this grant. The token is then sent as `Authorization: Bearer <token>`, with `X-CSRF-Header: -` on REST and Kepler calls. **The scope depends on what the integration needs, and no single scope is required.** Relativity's documentation shows `SystemUserInfo` and `UserInfoAccess` and no page says which is correct or what either grants; use the value the API you call documents, and confirm it. The text has two variants, by where the code runs, and each opens with the precondition it is for: read that sentence and discard the variant that does not apply.

**Variant: a Node.js process acting as itself**

**Precondition: this variant is for a Node.js job, service or script that does not act for a signed-in person.**

```javascript
const IDENTITY_TOKEN_URL = "https://HOST/Relativity/Identity/connect/token";
const EXPIRY_MARGIN_SECONDS = 60;
const MILLISECONDS_PER_SECOND = 1000;
let cachedToken = null;

async function getAccessToken() {
	const now = Date.now();
	if (cachedToken === null || cachedToken.expiresAtMs - now < EXPIRY_MARGIN_SECONDS * MILLISECONDS_PER_SECOND) {
		const response = await fetch(IDENTITY_TOKEN_URL, {
			method: "POST",
			headers: { "Content-Type": "application/x-www-form-urlencoded" },
			body: new URLSearchParams({
				grant_type: "client_credentials",
				client_id: process.env.RELATIVITY_CLIENT_ID,
				client_secret: process.env.RELATIVITY_CLIENT_SECRET,
			}),
		});
		const payload = await response.json();
		cachedToken = {
			value: payload.access_token,
			expiresAtMs: now + payload.expires_in * MILLISECONDS_PER_SECOND,
		};
	}
	return cachedToken.value;
}
```

**Cache the token and re-acquire it on expiry — do not refresh.** Client Credentials offers no refresh tokens: **verified by NSerio against a live instance; not documented by Relativity** — the Allow Refresh Tokens control does not appear for this grant in the RelativityOne user interface. The recommended Access Token Lifetime is listed in part 3. The block uses the built-in `fetch` and no package. **The client secret** belongs in the process environment or a secret store, as Secret Management (Part B of the Migration Documentation) describes, **never in source, in a tracked `.env` file or in a bundle**. A reset invalidates the previous secret immediately, so plan the cut-over before resetting it. **Before deploying, confirm the calling identity:** every call now runs as the OAuth2 Client's Context User, not as the user in the replaced credential, and a non-administrator Context User is NSerio's recommendation. Trusted IPs restrict where a token can be requested, not a token already issued.

**Variant: browser code that does not act for a signed-in user**

**Precondition: this variant is for browser code that calls RelativityOne with no person signed in to it, so no person's identity exists to preserve. If a person signs in, use Authorization Code, variant 1.**

The Relativity request moves behind a **server-side component** that holds the secret and performs the token exchange (the Node.js process variant above); the browser calls that component on its own origin. **The component must authenticate and authorize its own callers, and the OAuth2 Client's Context User must hold the minimum permissions the operation needs. Without both, the result is an unauthenticated relay into RelativityOne:** every caller reaching it acts with the Context User's full rights, which is strictly worse than the starting point. The relay returns results to the browser, **never the token** and never the secret. For browser code the credential has been public for as long as the code has shipped; the tool does not escalate it, so this text states the exposure and the rotation:

> Treat this credential as compromised. Rotate it, and assume the repository history has published it. Completing the migration does not close this exposure; rotation does.

**When the code acts for a signed-in user.** Client Credentials is one service identity by construction. Code that asks a person for a password and sends it is the Authorization Code case, and moving to it means a new OAuth2 Client, because the flow is fixed when a client is created. The finding's flow note says when to make that choice.

Verification: **Verification note.** **The tool saw a colon pair encoded with `btoa` and the word `Basic` or `Authorization` within a statement's distance**: follow the encoded value to every place it is sent and change those uses too. **A token has a finite lifetime.**

**If this code runs in a browser, the credential has been public for as long as the code has shipped.** The exposure and the rotation are stated here and the tool does not escalate it:

> Treat this credential as compromised. Rotate it, and assume the repository history has published it. Completing the migration does not close this exposure; rotation does.

OAuth2 Client: Create or confirm this OAuth2 Client in RelativityOne (NSerio's observation: the menu path is Home > Authentication > OAuth2 Client Manager, as Relativity's REST API authentication page names it). This tool never connects to RelativityOne and never creates or changes the OAuth2 Client; the settings below are guidance.
- Name: a descriptive name identifying the integration it serves. Use one OAuth2 Client per integration and do not share it across integrations.
- Flow Grant Type: Client Credentials
- Context User: NSerio's recommendation: a dedicated integration user with the minimum permissions the integration needs, and not a personal account and not an administrator, because every call runs with that user's permissions and an administrator Context User gives the integration an administrator's reach. Relativity documentation (REST API authentication page): describes the Context User as usually a member of the Relativity Administrators group, though it could differ depending on the API or operation invoked; NSerio recommends against that default for an integration.
- Access Token Lifetime: 60 minutes, stated in minutes because the field is in minutes. Relativity documentation: tokens for this flow must have a short lifetime, and the recommended value matches the Identity Server default.
- Trusted IPs: Relativity documentation: the address list is checked whenever a token is requested with the OAuth2 Client's ID and secret. NSerio's recommendation: populate it with the integration's outbound IP addresses, one per line, wherever they are known and stable. It restricts where a token can be requested, checked when the token is issued; it does not restrict a token already issued, which travels in the request header and is valid from anywhere until it expires.
- Allow refresh tokens: not used by this flow. The integration requests a new access token when the current one expires.
- Enabled: on.
- Every call made with this OAuth2 Client runs with the Context User's permissions.
- Relativity documentation: the flow is fixed when the OAuth2 Client is created and cannot be changed when editing it, so moving this integration to Authorization Code later means creating a new OAuth2 Client.
<!-- generated:end JS-005 client-credentials -->

#### Authorization Code

<!-- generated:begin JS-005 authorization-code -->
**Confirming the flow.** An interactive-user signal in a file assigns Authorization Code, with this note: "An interactive-user signal in this file points to Authorization Code. Confirm the flow: use Client Credentials instead if the integration runs with credentials fixed for the integration and no signed-in user." Your user profile can also make Authorization Code the default; see Changing the default in [Confirming the flow](#confirming-the-flow). See [Confirming the flow](#confirming-the-flow) and the "Which flow" paragraph of JS-005 in [Part A](part-a-javascript.md#js-005).

Source form: **Applies to:** browser and Node.js.

```javascript
const token = btoa(userName + ":" + secret);
const header = "Basic " + token;
```

Under token authentication: **First, the destination:** if this Relativity instance is not RelativityOne — including a Relativity Server instance, whose own documentation governs — this finding is informational and the rest of this template does not apply. **If the encoded pair is not a credential, this finding is a false positive and nothing changes.**

Remove the pair, the encoding and the variable, and send the token: `Authorization: Bearer ${accessToken}` with `X-CSRF-Header: -`. In a browser the call moves behind the server-side component.

Credentials: **Where the credentials come from.** The text establishes that a client sends Basic, not that the destination is RelativityOne. If it is not RelativityOne, the migration is that service's own token mechanism, this finding is informational for this tool, and the reader stops here. If it is RelativityOne, the token comes from the token-acquisition part that follows, and the user name and password are replaced by the OAuth2 Client's client ID and secret, kept as Secret Management (Part B of the Migration Documentation) describes, never in source, in a tracked `.env` file or in a bundle.

**Common to all three variants.** The text has a common part and **three variants by application type**, each opening with the precondition it is for: a browser with a server-side component, a Node.js server, and a Node.js command-line tool or desktop shell. Read the precondition of each and discard the variants that do not apply.

**Sign in.** Replace the code that collected the person's credentials with a redirect to the authorize endpoint `<host>/Relativity/Identity/connect/authorize` (**verified by NSerio against a live instance; not documented by Relativity**), which leads to RelativityOne's login page. The request carries the standard authorization-code parameters — `client_id`, `response_type=code`, the registered `redirect_uri`, `state` checked on return, and `code_challenge` with `code_challenge_method=S256` when PKCE is used; these are standard OAuth and Relativity documents none of them.

**PKCE.** PKCE is required by Relativity's own documentation to obtain a refresh token and not otherwise: a Code request that does not ask for a refresh token works without it, **verified by NSerio against a live instance; not documented by Relativity**. NSerio recommends `S256` (NSerio recommendation) and does not claim that Relativity accepts any particular method. `state` is generated, stored against the session and compared on return; `nonce` is not used, and Relativity does not document whether it enforces either.

**The client secret is required.** Code does not work without a client secret: tested and rejected, **verified by NSerio against a live instance; not documented by Relativity**, which confirms Relativity's own statement. **Any secret shipped to a browser is public**, and bundling, minifying or obfuscating changes nothing, so the exchange runs where a secret can be kept.

**Exchange the code.** A form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=authorization_code`, the code, the same `redirect_uri`, the client ID and the secret, plus the `code_verifier` when PKCE is used (**verified by NSerio against a live instance; not documented by Relativity**).

**Redirect URL.** A server-side application registers **one exact HTTPS URL per environment, no wildcards**; the matching rules are not documented and no claim is made about them. A plain-HTTP loopback Redirect URL `http://127.0.0.1:<port>/<path>` on a **fixed registered port** is accepted, **verified by NSerio against a live instance; not documented by Relativity**, and is the desktop shape.

**On expiry.** With Allow Refresh Tokens enabled and PKCE used, the code holds a refresh token and sends `grant_type=refresh_token` in place of the code exchange: *inferred* from the standard OAuth shape, not tested. Relativity does not document a refresh token's lifetime, rotation or revocation, nor what disabling the client does to tokens already issued. **A refresh token is a secret.** **Single sign-on:** sign-in happens on RelativityOne's own page, so the instance's single sign-on and two-factor rules apply unchanged (*inferred*). **The code stops collecting the password:** after this change nothing receives, stores or forwards the person's user name and password, and the code holds only tokens. Every call now runs as the person who signed in, so **the calling identity changes**. **Scope:** no scope set is prescribed for Code; `openid RelativityRequestOrigin UserInfo UserInfoAccess AuthenticationID RelativityWeb` works, **verified by NSerio against a live instance; not documented by Relativity**, as working and not as minimal.

**Variant 1 — Browser with a server-side component**

**Precondition: this variant is for code that runs in a web browser and signs a person in to RelativityOne. If your code runs in Node.js, use variant 2 or variant 3.**

A browser cannot hold the secret the Code grant requires, so **the browser never calls RelativityOne**: it navigates to a route on **the application's own origin**, and a **server-side component** (variant 2's code) creates `state`, redirects to the authorize endpoint, receives the code at its registered HTTPS Redirect URL, exchanges it, keeps the tokens and gives the browser only its own session. **The browser holds a session identifier with that component and nothing from RelativityOne.** The authorize step is a **top-level navigation** (a full-window redirect, not a `fetch`).

```javascript
const LOGIN_ROUTE = "/auth/login";
const API_ROUTE = "/api/documents";

function signIn() {
	window.location.assign(LOGIN_ROUTE);
}

async function loadDocuments() {
	const response = await fetch(API_ROUTE, { credentials: "same-origin" });
	return response.json();
}
```

Both calls go to the page's own origin. `credentials: "include"` is a cookie policy and is not recommended here, because the architecture needs no cross-origin call. **Cross-origin access, both ways, both not verified:** NSerio has not run a preflight against a live instance. *If RelativityOne serves CORS to browser callers*, the legacy browser Basic call could have worked from another origin, and a browser holding a Bearer token could in principle call it directly; **this text still does not recommend that**, because the only grant that yields a token for a person needs the secret the browser cannot hold. *If it does not*, the legacy call worked only from the same origin or through a proxy the client already runs. **The recommended architecture holds under either answer.** **The component keeps the tokens on the server**, never in browser storage, which any script on the page can read (a browser behaviour, unchecked); the recommended session cookie is `HttpOnly`, `Secure` and `SameSite` (NSerio recommendation; unchecked).

**Variant 2 — Node.js server**

**Precondition: this variant is for code that runs in Node.js as a web server and acts for people who sign in to it. It is not for browser code (variant 1) or a command-line tool (variant 3).**

Standard server-side Code flow. A login route builds the authorize address with `state` (and the PKCE verifier where a refresh token is wanted), stores them against the visitor's session and redirects; RelativityOne redirects to the **callback route**, registered as an exact HTTPS URL; the callback checks `state` and exchanges the code with the secret. This is the component variant 1 relies on.

```javascript
import crypto from "node:crypto";

const IDENTITY_BASE = "https://HOST/Relativity/Identity/connect";
const REDIRECT_URI = "https://app.example.test/auth/callback";
const CLIENT_ID = process.env.RELATIVITY_CLIENT_ID;
const CLIENT_SECRET = process.env.RELATIVITY_CLIENT_SECRET;
const RANDOM_BYTES = 32;
const pendingBySession = new Map();

function base64Url(buffer) {
	return buffer.toString("base64url");
}

function beginLogin(sessionId, wantRefreshToken) {
	const state = base64Url(crypto.randomBytes(RANDOM_BYTES));
	const verifier = base64Url(crypto.randomBytes(RANDOM_BYTES));
	const challenge = base64Url(crypto.createHash("sha256").update(verifier).digest());
	pendingBySession.set(sessionId, { state, verifier });
	const query = new URLSearchParams({
		response_type: "code",
		client_id: CLIENT_ID,
		redirect_uri: REDIRECT_URI,
		state,
	});
	if (wantRefreshToken) {
		query.set("code_challenge", challenge);
		query.set("code_challenge_method", "S256");
	}
	return `${IDENTITY_BASE}/authorize?${query}`;
}

async function completeLogin(sessionId, returnedCode, returnedState) {
	const pending = pendingBySession.get(sessionId);
	pendingBySession.delete(sessionId);
	const stateMatches = pending !== undefined
		&& returnedState !== undefined
		&& Buffer.byteLength(returnedState) === Buffer.byteLength(pending.state)
		&& crypto.timingSafeEqual(Buffer.from(returnedState), Buffer.from(pending.state));
	let tokens = null;
	if (stateMatches) {
		const response = await fetch(`${IDENTITY_BASE}/token`, {
			method: "POST",
			headers: { "Content-Type": "application/x-www-form-urlencoded" },
			body: new URLSearchParams({
				grant_type: "authorization_code",
				code: returnedCode,
				redirect_uri: REDIRECT_URI,
				client_id: CLIENT_ID,
				client_secret: CLIENT_SECRET,
				code_verifier: pending.verifier,
			}),
		});
		tokens = response.ok ? await response.json() : null;
	}
	return tokens;
}
```

`state` is consumed once and compared on return; a request with no pending entry or a different `state` gets no exchange. **The scope is not prescribed** (the sketch sends none). **Where the secret travels in the token request** (the body, as sketched, or HTTP Basic client authentication) is **not verified**. The sketch does not show the HTTP server, the session cookie or a session store that survives more than one process. **Tokens stay on the server**, in a session store that is not exposed to a request.

**Variant 3 — Node.js command line or desktop**

**Precondition: this variant is for code that runs on a person's own machine, as a command-line tool or a desktop shell, and signs that person in. It is not for a web server (variant 2) or browser code (variant 1).**

The process starts a listener from the built-in `node:http` module, bound to **`127.0.0.1` only**, on the **fixed registered port**, for **one request**; opens the system browser on the authorize address **and always prints the address too**, so a machine with no browser, an SSH session or a failed launch has a manual route; reads `code` and `state` from the one request, closes the listener, checks `state`, and exchanges the code as variant 2's `completeLogin` does.

```javascript
import http from "node:http";

const LOOPBACK_HOST = "127.0.0.1";
const REGISTERED_PORT = 8123;
const CALLBACK_PATH = "/callback";
const BASE_FOR_PARSING = `http://${LOOPBACK_HOST}:${REGISTERED_PORT}`;
const WAIT_MILLISECONDS = 120000;
const STATUS_OK = 200;
const STATUS_NOT_FOUND = 404;

function waitForRedirect() {
	return new Promise((resolve, reject) => {
		const server = http.createServer((request, response) => {
			const address = new URL(request.url, BASE_FOR_PARSING);
			const isCallback = address.pathname === CALLBACK_PATH;
			response.statusCode = isCallback ? STATUS_OK : STATUS_NOT_FOUND;
			response.end(isCallback ? "You can close this window." : "Not found");
			if (isCallback) {
				server.close();
				resolve({
					code: address.searchParams.get("code"),
					state: address.searchParams.get("state"),
				});
			}
		});
		server.on("error", reject);
		server.listen(REGISTERED_PORT, LOOPBACK_HOST);
		setTimeout(() => {
			server.close();
			reject(new Error("No redirect arrived"));
		}, WAIT_MILLISECONDS).unref();
	});
}
```

**Rules for the listener:** bind to loopback only; accept one request; **check `state`** (another local process can connect to a fixed, advertised port); close the listener after the request; and handle a busy port (a second server on a port in use raises `EADDRINUSE`, so fall back to the printed address). **Opening the system browser:** Node.js core has no function for it; run the platform's own launcher with a fixed program and an argument array, never a shell string built from the address (unchecked on every system). **Where tokens are kept:** a file with restricted permissions or the operating system's keychain; on Windows the POSIX mode argument of Node.js did not restrict a file when it was tried (*checked by NSerio locally* on Node.js v24.11.1), so a restricted file there needs an access-control change by other means, and on Linux and macOS it is unchecked. **The client secret of a distributed tool is extractable:** a command-line tool or desktop shell shipped to people carries its client secret in a form anyone who holds the distribution can extract, and it protects nothing against the person running it. **A known issue:** Relativity documents no recommendation for a distributed client; this design is what NSerio verified works. **Compensating guidance** (NSerio recommendation, subject to your own security policy): Trusted IPs where the users' outbound addresses are known and stable (checked at token issuance; applicability to Code *inferred*); the recommended short Access Token Lifetime; refresh tokens only when needed; and **one OAuth2 Client per distribution**, so a leaked secret can be disabled and reset alone.

Verification: **Verification note.** **The tool saw a colon pair encoded with `btoa` and the word `Basic` or `Authorization` within a statement's distance**: follow the encoded value to every place it is sent and change those uses too. **A token has a finite lifetime.**

**If this code runs in a browser, the credential has been public for as long as the code has shipped.** The exposure and the rotation are stated here and the tool does not escalate it:

> Treat this credential as compromised. Rotate it, and assume the repository history has published it. Completing the migration does not close this exposure; rotation does.

OAuth2 Client: Create or confirm this OAuth2 Client in RelativityOne (NSerio's observation: the menu path is Home > Authentication > OAuth2 Client Manager, as Relativity's REST API authentication page names it). This tool never connects to RelativityOne and never creates or changes the OAuth2 Client; the settings below are guidance.
- Name: a descriptive name identifying the application it serves.
- Flow Grant Type: Code
- Redirect URLs: Relativity documentation: each URL includes the http or https protocol prefix. The exact callback URL is registered by the Relativity administrator. Server-side web applications, NSerio's recommendation: one exact HTTPS URL per environment, with no wildcards. Desktop and CLI applications: a plain-HTTP loopback URL, `http://127.0.0.1:<port>/<path>`, on a fixed port because the URL is registered in advance and the port cannot be chosen at runtime (verified by NSerio against a live instance; not documented by Relativity).
- Access Token Lifetime: 60 minutes, stated in minutes because the field is in minutes. Relativity documentation: tokens for this flow must have a short lifetime, and the recommended value matches the Identity Server default.
- Allow refresh tokens: NSerio's recommendation: leave it off unless the application must keep a user session active beyond the access token lifetime without asking the user to sign in again. If it is on, the refresh tokens are stored as secrets, per Secret Management (Part B of the Migration Documentation).
- Enabled: on.
<!-- generated:end JS-005 authorization-code -->

### JS-006 · NodeBufferBase64Pair

<a id="js-006-migration"></a>

Remove the pair and the `Buffer` encoding, a legacy `new Buffer(...)` included, and send the token. If the encoded pair is not a credential, the finding is a false positive and nothing changes.

#### Client Credentials

<!-- generated:begin JS-006 client-credentials -->
**Confirming the flow.** When no signal in a file decides the flow, the tool assigns Client Credentials, the default, with this note: "Who supplies the credential at run time? If a person types it or signs in, the integration acts for that person: use Authorization Code. If it comes from configuration, settings, an environment variable or a secret store: use Client Credentials. No signal in this file showed which applies, so Client Credentials, the default, is recommended. Use Authorization Code instead if the integration acts on behalf of a signed-in user, or if your RelativityOne instance already has a Code-flow OAuth2 Client for this integration." A fixed-credential signal assigns it with this note: "A fixed-credential signal in this file points to Client Credentials, but how often that kind of signal is right has not been measured. Use Authorization Code instead if the integration acts on behalf of a signed-in user, or if your RelativityOne instance already has a Code-flow OAuth2 Client for this integration." See [Confirming the flow](#confirming-the-flow) and the "Which flow" paragraph of JS-006 in [Part A](part-a-javascript.md#js-006).

Source form: **Applies to:** Node.js; a browser bundle that polyfills `Buffer`.

```javascript
const token = Buffer.from(`${userName}:${secret}`).toString("base64");
const header = "Basic " + token;
```

Under token authentication: **First, the destination:** if this Relativity instance is not RelativityOne — including a Relativity Server instance, whose own documentation governs — this finding is informational and the rest of this template does not apply. **If the encoded pair is not a credential, this finding is a false positive and nothing changes.**

Remove the pair and the encoding (including a legacy `new Buffer(…)`), and send the token: `Authorization: Bearer ${accessToken}` with `X-CSRF-Header: -`.

Credentials: **Where the credentials come from.** The text establishes that a client sends Basic, not that the destination is RelativityOne. If it is not RelativityOne, the migration is that service's own token mechanism, this finding is informational for this tool, and the reader stops here. If it is RelativityOne, the token comes from the token-acquisition part that follows, and the user name and password are replaced by the OAuth2 Client's client ID and secret, kept as Secret Management (Part B of the Migration Documentation) describes, never in source, in a tracked `.env` file or in a bundle.

**Where the token comes from.** Create an OAuth2 Client with Flow Grant Type set to Client Credentials (part 3 lists its settings), then send a form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=client_credentials`, `client_id` and `client_secret`. The token endpoint is **documented** for this grant. The token is then sent as `Authorization: Bearer <token>`, with `X-CSRF-Header: -` on REST and Kepler calls. **The scope depends on what the integration needs, and no single scope is required.** Relativity's documentation shows `SystemUserInfo` and `UserInfoAccess` and no page says which is correct or what either grants; use the value the API you call documents, and confirm it. The text has two variants, by where the code runs, and each opens with the precondition it is for: read that sentence and discard the variant that does not apply.

**Variant: a Node.js process acting as itself**

**Precondition: this variant is for a Node.js job, service or script that does not act for a signed-in person.**

```javascript
const IDENTITY_TOKEN_URL = "https://HOST/Relativity/Identity/connect/token";
const EXPIRY_MARGIN_SECONDS = 60;
const MILLISECONDS_PER_SECOND = 1000;
let cachedToken = null;

async function getAccessToken() {
	const now = Date.now();
	if (cachedToken === null || cachedToken.expiresAtMs - now < EXPIRY_MARGIN_SECONDS * MILLISECONDS_PER_SECOND) {
		const response = await fetch(IDENTITY_TOKEN_URL, {
			method: "POST",
			headers: { "Content-Type": "application/x-www-form-urlencoded" },
			body: new URLSearchParams({
				grant_type: "client_credentials",
				client_id: process.env.RELATIVITY_CLIENT_ID,
				client_secret: process.env.RELATIVITY_CLIENT_SECRET,
			}),
		});
		const payload = await response.json();
		cachedToken = {
			value: payload.access_token,
			expiresAtMs: now + payload.expires_in * MILLISECONDS_PER_SECOND,
		};
	}
	return cachedToken.value;
}
```

**Cache the token and re-acquire it on expiry — do not refresh.** Client Credentials offers no refresh tokens: **verified by NSerio against a live instance; not documented by Relativity** — the Allow Refresh Tokens control does not appear for this grant in the RelativityOne user interface. The recommended Access Token Lifetime is listed in part 3. The block uses the built-in `fetch` and no package. **The client secret** belongs in the process environment or a secret store, as Secret Management (Part B of the Migration Documentation) describes, **never in source, in a tracked `.env` file or in a bundle**. A reset invalidates the previous secret immediately, so plan the cut-over before resetting it. **Before deploying, confirm the calling identity:** every call now runs as the OAuth2 Client's Context User, not as the user in the replaced credential, and a non-administrator Context User is NSerio's recommendation. Trusted IPs restrict where a token can be requested, not a token already issued.

**Variant: browser code that does not act for a signed-in user**

**Precondition: this variant is for browser code that calls RelativityOne with no person signed in to it, so no person's identity exists to preserve. If a person signs in, use Authorization Code, variant 1.**

The Relativity request moves behind a **server-side component** that holds the secret and performs the token exchange (the Node.js process variant above); the browser calls that component on its own origin. **The component must authenticate and authorize its own callers, and the OAuth2 Client's Context User must hold the minimum permissions the operation needs. Without both, the result is an unauthenticated relay into RelativityOne:** every caller reaching it acts with the Context User's full rights, which is strictly worse than the starting point. The relay returns results to the browser, **never the token** and never the secret. For browser code the credential has been public for as long as the code has shipped; the tool does not escalate it, so this text states the exposure and the rotation:

> Treat this credential as compromised. Rotate it, and assume the repository history has published it. Completing the migration does not close this exposure; rotation does.

**When the code acts for a signed-in user.** Client Credentials is one service identity by construction. Code that asks a person for a password and sends it is the Authorization Code case, and moving to it means a new OAuth2 Client, because the flow is fixed when a client is created. The finding's flow note says when to make that choice.

Verification: **Verification note.** **The tool read the colon pair and a following `.toString("base64")`** (a `base64url` value is not Basic and is not read). **A token has a finite lifetime.**

**If this code runs in a browser, the credential has been public for as long as the code has shipped.** The exposure and the rotation are stated here and the tool does not escalate it:

> Treat this credential as compromised. Rotate it, and assume the repository history has published it. Completing the migration does not close this exposure; rotation does.

OAuth2 Client: Create or confirm this OAuth2 Client in RelativityOne (NSerio's observation: the menu path is Home > Authentication > OAuth2 Client Manager, as Relativity's REST API authentication page names it). This tool never connects to RelativityOne and never creates or changes the OAuth2 Client; the settings below are guidance.
- Name: a descriptive name identifying the integration it serves. Use one OAuth2 Client per integration and do not share it across integrations.
- Flow Grant Type: Client Credentials
- Context User: NSerio's recommendation: a dedicated integration user with the minimum permissions the integration needs, and not a personal account and not an administrator, because every call runs with that user's permissions and an administrator Context User gives the integration an administrator's reach. Relativity documentation (REST API authentication page): describes the Context User as usually a member of the Relativity Administrators group, though it could differ depending on the API or operation invoked; NSerio recommends against that default for an integration.
- Access Token Lifetime: 60 minutes, stated in minutes because the field is in minutes. Relativity documentation: tokens for this flow must have a short lifetime, and the recommended value matches the Identity Server default.
- Trusted IPs: Relativity documentation: the address list is checked whenever a token is requested with the OAuth2 Client's ID and secret. NSerio's recommendation: populate it with the integration's outbound IP addresses, one per line, wherever they are known and stable. It restricts where a token can be requested, checked when the token is issued; it does not restrict a token already issued, which travels in the request header and is valid from anywhere until it expires.
- Allow refresh tokens: not used by this flow. The integration requests a new access token when the current one expires.
- Enabled: on.
- Every call made with this OAuth2 Client runs with the Context User's permissions.
- Relativity documentation: the flow is fixed when the OAuth2 Client is created and cannot be changed when editing it, so moving this integration to Authorization Code later means creating a new OAuth2 Client.
<!-- generated:end JS-006 client-credentials -->

#### Authorization Code

<!-- generated:begin JS-006 authorization-code -->
**Confirming the flow.** An interactive-user signal in a file assigns Authorization Code, with this note: "An interactive-user signal in this file points to Authorization Code. Confirm the flow: use Client Credentials instead if the integration runs with credentials fixed for the integration and no signed-in user." Your user profile can also make Authorization Code the default; see Changing the default in [Confirming the flow](#confirming-the-flow). See [Confirming the flow](#confirming-the-flow) and the "Which flow" paragraph of JS-006 in [Part A](part-a-javascript.md#js-006).

Source form: **Applies to:** Node.js; a browser bundle that polyfills `Buffer`.

```javascript
const token = Buffer.from(`${userName}:${secret}`).toString("base64");
const header = "Basic " + token;
```

Under token authentication: **First, the destination:** if this Relativity instance is not RelativityOne — including a Relativity Server instance, whose own documentation governs — this finding is informational and the rest of this template does not apply. **If the encoded pair is not a credential, this finding is a false positive and nothing changes.**

Remove the pair and the encoding (including a legacy `new Buffer(…)`), and send the token: `Authorization: Bearer ${accessToken}` with `X-CSRF-Header: -`.

Credentials: **Where the credentials come from.** The text establishes that a client sends Basic, not that the destination is RelativityOne. If it is not RelativityOne, the migration is that service's own token mechanism, this finding is informational for this tool, and the reader stops here. If it is RelativityOne, the token comes from the token-acquisition part that follows, and the user name and password are replaced by the OAuth2 Client's client ID and secret, kept as Secret Management (Part B of the Migration Documentation) describes, never in source, in a tracked `.env` file or in a bundle.

**Common to all three variants.** The text has a common part and **three variants by application type**, each opening with the precondition it is for: a browser with a server-side component, a Node.js server, and a Node.js command-line tool or desktop shell. Read the precondition of each and discard the variants that do not apply.

**Sign in.** Replace the code that collected the person's credentials with a redirect to the authorize endpoint `<host>/Relativity/Identity/connect/authorize` (**verified by NSerio against a live instance; not documented by Relativity**), which leads to RelativityOne's login page. The request carries the standard authorization-code parameters — `client_id`, `response_type=code`, the registered `redirect_uri`, `state` checked on return, and `code_challenge` with `code_challenge_method=S256` when PKCE is used; these are standard OAuth and Relativity documents none of them.

**PKCE.** PKCE is required by Relativity's own documentation to obtain a refresh token and not otherwise: a Code request that does not ask for a refresh token works without it, **verified by NSerio against a live instance; not documented by Relativity**. NSerio recommends `S256` (NSerio recommendation) and does not claim that Relativity accepts any particular method. `state` is generated, stored against the session and compared on return; `nonce` is not used, and Relativity does not document whether it enforces either.

**The client secret is required.** Code does not work without a client secret: tested and rejected, **verified by NSerio against a live instance; not documented by Relativity**, which confirms Relativity's own statement. **Any secret shipped to a browser is public**, and bundling, minifying or obfuscating changes nothing, so the exchange runs where a secret can be kept.

**Exchange the code.** A form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=authorization_code`, the code, the same `redirect_uri`, the client ID and the secret, plus the `code_verifier` when PKCE is used (**verified by NSerio against a live instance; not documented by Relativity**).

**Redirect URL.** A server-side application registers **one exact HTTPS URL per environment, no wildcards**; the matching rules are not documented and no claim is made about them. A plain-HTTP loopback Redirect URL `http://127.0.0.1:<port>/<path>` on a **fixed registered port** is accepted, **verified by NSerio against a live instance; not documented by Relativity**, and is the desktop shape.

**On expiry.** With Allow Refresh Tokens enabled and PKCE used, the code holds a refresh token and sends `grant_type=refresh_token` in place of the code exchange: *inferred* from the standard OAuth shape, not tested. Relativity does not document a refresh token's lifetime, rotation or revocation, nor what disabling the client does to tokens already issued. **A refresh token is a secret.** **Single sign-on:** sign-in happens on RelativityOne's own page, so the instance's single sign-on and two-factor rules apply unchanged (*inferred*). **The code stops collecting the password:** after this change nothing receives, stores or forwards the person's user name and password, and the code holds only tokens. Every call now runs as the person who signed in, so **the calling identity changes**. **Scope:** no scope set is prescribed for Code; `openid RelativityRequestOrigin UserInfo UserInfoAccess AuthenticationID RelativityWeb` works, **verified by NSerio against a live instance; not documented by Relativity**, as working and not as minimal.

**Variant 1 — Browser with a server-side component**

**Precondition: this variant is for code that runs in a web browser and signs a person in to RelativityOne. If your code runs in Node.js, use variant 2 or variant 3.**

A browser cannot hold the secret the Code grant requires, so **the browser never calls RelativityOne**: it navigates to a route on **the application's own origin**, and a **server-side component** (variant 2's code) creates `state`, redirects to the authorize endpoint, receives the code at its registered HTTPS Redirect URL, exchanges it, keeps the tokens and gives the browser only its own session. **The browser holds a session identifier with that component and nothing from RelativityOne.** The authorize step is a **top-level navigation** (a full-window redirect, not a `fetch`).

```javascript
const LOGIN_ROUTE = "/auth/login";
const API_ROUTE = "/api/documents";

function signIn() {
	window.location.assign(LOGIN_ROUTE);
}

async function loadDocuments() {
	const response = await fetch(API_ROUTE, { credentials: "same-origin" });
	return response.json();
}
```

Both calls go to the page's own origin. `credentials: "include"` is a cookie policy and is not recommended here, because the architecture needs no cross-origin call. **Cross-origin access, both ways, both not verified:** NSerio has not run a preflight against a live instance. *If RelativityOne serves CORS to browser callers*, the legacy browser Basic call could have worked from another origin, and a browser holding a Bearer token could in principle call it directly; **this text still does not recommend that**, because the only grant that yields a token for a person needs the secret the browser cannot hold. *If it does not*, the legacy call worked only from the same origin or through a proxy the client already runs. **The recommended architecture holds under either answer.** **The component keeps the tokens on the server**, never in browser storage, which any script on the page can read (a browser behaviour, unchecked); the recommended session cookie is `HttpOnly`, `Secure` and `SameSite` (NSerio recommendation; unchecked).

**Variant 2 — Node.js server**

**Precondition: this variant is for code that runs in Node.js as a web server and acts for people who sign in to it. It is not for browser code (variant 1) or a command-line tool (variant 3).**

Standard server-side Code flow. A login route builds the authorize address with `state` (and the PKCE verifier where a refresh token is wanted), stores them against the visitor's session and redirects; RelativityOne redirects to the **callback route**, registered as an exact HTTPS URL; the callback checks `state` and exchanges the code with the secret. This is the component variant 1 relies on.

```javascript
import crypto from "node:crypto";

const IDENTITY_BASE = "https://HOST/Relativity/Identity/connect";
const REDIRECT_URI = "https://app.example.test/auth/callback";
const CLIENT_ID = process.env.RELATIVITY_CLIENT_ID;
const CLIENT_SECRET = process.env.RELATIVITY_CLIENT_SECRET;
const RANDOM_BYTES = 32;
const pendingBySession = new Map();

function base64Url(buffer) {
	return buffer.toString("base64url");
}

function beginLogin(sessionId, wantRefreshToken) {
	const state = base64Url(crypto.randomBytes(RANDOM_BYTES));
	const verifier = base64Url(crypto.randomBytes(RANDOM_BYTES));
	const challenge = base64Url(crypto.createHash("sha256").update(verifier).digest());
	pendingBySession.set(sessionId, { state, verifier });
	const query = new URLSearchParams({
		response_type: "code",
		client_id: CLIENT_ID,
		redirect_uri: REDIRECT_URI,
		state,
	});
	if (wantRefreshToken) {
		query.set("code_challenge", challenge);
		query.set("code_challenge_method", "S256");
	}
	return `${IDENTITY_BASE}/authorize?${query}`;
}

async function completeLogin(sessionId, returnedCode, returnedState) {
	const pending = pendingBySession.get(sessionId);
	pendingBySession.delete(sessionId);
	const stateMatches = pending !== undefined
		&& returnedState !== undefined
		&& Buffer.byteLength(returnedState) === Buffer.byteLength(pending.state)
		&& crypto.timingSafeEqual(Buffer.from(returnedState), Buffer.from(pending.state));
	let tokens = null;
	if (stateMatches) {
		const response = await fetch(`${IDENTITY_BASE}/token`, {
			method: "POST",
			headers: { "Content-Type": "application/x-www-form-urlencoded" },
			body: new URLSearchParams({
				grant_type: "authorization_code",
				code: returnedCode,
				redirect_uri: REDIRECT_URI,
				client_id: CLIENT_ID,
				client_secret: CLIENT_SECRET,
				code_verifier: pending.verifier,
			}),
		});
		tokens = response.ok ? await response.json() : null;
	}
	return tokens;
}
```

`state` is consumed once and compared on return; a request with no pending entry or a different `state` gets no exchange. **The scope is not prescribed** (the sketch sends none). **Where the secret travels in the token request** (the body, as sketched, or HTTP Basic client authentication) is **not verified**. The sketch does not show the HTTP server, the session cookie or a session store that survives more than one process. **Tokens stay on the server**, in a session store that is not exposed to a request.

**Variant 3 — Node.js command line or desktop**

**Precondition: this variant is for code that runs on a person's own machine, as a command-line tool or a desktop shell, and signs that person in. It is not for a web server (variant 2) or browser code (variant 1).**

The process starts a listener from the built-in `node:http` module, bound to **`127.0.0.1` only**, on the **fixed registered port**, for **one request**; opens the system browser on the authorize address **and always prints the address too**, so a machine with no browser, an SSH session or a failed launch has a manual route; reads `code` and `state` from the one request, closes the listener, checks `state`, and exchanges the code as variant 2's `completeLogin` does.

```javascript
import http from "node:http";

const LOOPBACK_HOST = "127.0.0.1";
const REGISTERED_PORT = 8123;
const CALLBACK_PATH = "/callback";
const BASE_FOR_PARSING = `http://${LOOPBACK_HOST}:${REGISTERED_PORT}`;
const WAIT_MILLISECONDS = 120000;
const STATUS_OK = 200;
const STATUS_NOT_FOUND = 404;

function waitForRedirect() {
	return new Promise((resolve, reject) => {
		const server = http.createServer((request, response) => {
			const address = new URL(request.url, BASE_FOR_PARSING);
			const isCallback = address.pathname === CALLBACK_PATH;
			response.statusCode = isCallback ? STATUS_OK : STATUS_NOT_FOUND;
			response.end(isCallback ? "You can close this window." : "Not found");
			if (isCallback) {
				server.close();
				resolve({
					code: address.searchParams.get("code"),
					state: address.searchParams.get("state"),
				});
			}
		});
		server.on("error", reject);
		server.listen(REGISTERED_PORT, LOOPBACK_HOST);
		setTimeout(() => {
			server.close();
			reject(new Error("No redirect arrived"));
		}, WAIT_MILLISECONDS).unref();
	});
}
```

**Rules for the listener:** bind to loopback only; accept one request; **check `state`** (another local process can connect to a fixed, advertised port); close the listener after the request; and handle a busy port (a second server on a port in use raises `EADDRINUSE`, so fall back to the printed address). **Opening the system browser:** Node.js core has no function for it; run the platform's own launcher with a fixed program and an argument array, never a shell string built from the address (unchecked on every system). **Where tokens are kept:** a file with restricted permissions or the operating system's keychain; on Windows the POSIX mode argument of Node.js did not restrict a file when it was tried (*checked by NSerio locally* on Node.js v24.11.1), so a restricted file there needs an access-control change by other means, and on Linux and macOS it is unchecked. **The client secret of a distributed tool is extractable:** a command-line tool or desktop shell shipped to people carries its client secret in a form anyone who holds the distribution can extract, and it protects nothing against the person running it. **A known issue:** Relativity documents no recommendation for a distributed client; this design is what NSerio verified works. **Compensating guidance** (NSerio recommendation, subject to your own security policy): Trusted IPs where the users' outbound addresses are known and stable (checked at token issuance; applicability to Code *inferred*); the recommended short Access Token Lifetime; refresh tokens only when needed; and **one OAuth2 Client per distribution**, so a leaked secret can be disabled and reset alone.

Verification: **Verification note.** **The tool read the colon pair and a following `.toString("base64")`** (a `base64url` value is not Basic and is not read). **A token has a finite lifetime.**

**If this code runs in a browser, the credential has been public for as long as the code has shipped.** The exposure and the rotation are stated here and the tool does not escalate it:

> Treat this credential as compromised. Rotate it, and assume the repository history has published it. Completing the migration does not close this exposure; rotation does.

OAuth2 Client: Create or confirm this OAuth2 Client in RelativityOne (NSerio's observation: the menu path is Home > Authentication > OAuth2 Client Manager, as Relativity's REST API authentication page names it). This tool never connects to RelativityOne and never creates or changes the OAuth2 Client; the settings below are guidance.
- Name: a descriptive name identifying the application it serves.
- Flow Grant Type: Code
- Redirect URLs: Relativity documentation: each URL includes the http or https protocol prefix. The exact callback URL is registered by the Relativity administrator. Server-side web applications, NSerio's recommendation: one exact HTTPS URL per environment, with no wildcards. Desktop and CLI applications: a plain-HTTP loopback URL, `http://127.0.0.1:<port>/<path>`, on a fixed port because the URL is registered in advance and the port cannot be chosen at runtime (verified by NSerio against a live instance; not documented by Relativity).
- Access Token Lifetime: 60 minutes, stated in minutes because the field is in minutes. Relativity documentation: tokens for this flow must have a short lifetime, and the recommended value matches the Identity Server default.
- Allow refresh tokens: NSerio's recommendation: leave it off unless the application must keep a user session active beyond the access token lifetime without asking the user to sign in again. If it is on, the refresh tokens are stored as secrets, per Secret Management (Part B of the Migration Documentation).
- Enabled: on.
<!-- generated:end JS-006 authorization-code -->

### JS-008 · NodeCoreAuthString

<a id="js-008-migration"></a>

Remove the `auth` option of the Node.js `http` or `https` call and send a Bearer header in the `headers` option. Node.js documents that sending an `Authorization` header overrides the `auth` option, but remove the option anyway so that the credential is not left in the source.

#### Client Credentials

<!-- generated:begin JS-008 client-credentials -->
**Confirming the flow.** When no signal in a file decides the flow, the tool assigns Client Credentials, the default, with this note: "Who supplies the credential at run time? If a person types it or signs in, the integration acts for that person: use Authorization Code. If it comes from configuration, settings, an environment variable or a secret store: use Client Credentials. No signal in this file showed which applies, so Client Credentials, the default, is recommended. Use Authorization Code instead if the integration acts on behalf of a signed-in user, or if your RelativityOne instance already has a Code-flow OAuth2 Client for this integration." A fixed-credential signal assigns it with this note: "A fixed-credential signal in this file points to Client Credentials, but how often that kind of signal is right has not been measured. Use Authorization Code instead if the integration acts on behalf of a signed-in user, or if your RelativityOne instance already has a Code-flow OAuth2 Client for this integration." See [Confirming the flow](#confirming-the-flow) and the "Which flow" paragraph of JS-008 in [Part A](part-a-javascript.md#js-008).

Source form: **Applies to:** Node.js (`http` and `https`).

```javascript
http.request({ host: hostName, path: "/path", auth: `${userName}:${secret}` }, handler);
```

Under token authentication: **First, the destination:** if this Relativity instance is not RelativityOne — including a Relativity Server instance, whose own documentation governs — this finding is informational and the rest of this template does not apply.

```javascript
const CSRF_HEADER_VALUE = "-";
https.request({ host: hostName, path: "/path", headers: { Authorization: `Bearer ${accessToken}`, "X-CSRF-Header": CSRF_HEADER_VALUE } }, handler);
```

Remove the `auth` option. Node.js documents that sending an `Authorization` header overrides the `auth` option, but remove it anyway so the credential is not left in the source.

Credentials: **Where the credentials come from.** The text establishes that a client sends Basic, not that the destination is RelativityOne. If it is not RelativityOne, the migration is that service's own token mechanism, this finding is informational for this tool, and the reader stops here. If it is RelativityOne, the token comes from the token-acquisition part that follows, and the user name and password are replaced by the OAuth2 Client's client ID and secret, kept as Secret Management (Part B of the Migration Documentation) describes, never in source, in a tracked `.env` file or in a bundle.

**Where the token comes from.** Create an OAuth2 Client with Flow Grant Type set to Client Credentials (part 3 lists its settings), then send a form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=client_credentials`, `client_id` and `client_secret`. The token endpoint is **documented** for this grant. The token is then sent as `Authorization: Bearer <token>`, with `X-CSRF-Header: -` on REST and Kepler calls. **The scope depends on what the integration needs, and no single scope is required.** Relativity's documentation shows `SystemUserInfo` and `UserInfoAccess` and no page says which is correct or what either grants; use the value the API you call documents, and confirm it. The text has two variants, by where the code runs, and each opens with the precondition it is for: read that sentence and discard the variant that does not apply.

**Variant: a Node.js process acting as itself**

**Precondition: this variant is for a Node.js job, service or script that does not act for a signed-in person.**

```javascript
const IDENTITY_TOKEN_URL = "https://HOST/Relativity/Identity/connect/token";
const EXPIRY_MARGIN_SECONDS = 60;
const MILLISECONDS_PER_SECOND = 1000;
let cachedToken = null;

async function getAccessToken() {
	const now = Date.now();
	if (cachedToken === null || cachedToken.expiresAtMs - now < EXPIRY_MARGIN_SECONDS * MILLISECONDS_PER_SECOND) {
		const response = await fetch(IDENTITY_TOKEN_URL, {
			method: "POST",
			headers: { "Content-Type": "application/x-www-form-urlencoded" },
			body: new URLSearchParams({
				grant_type: "client_credentials",
				client_id: process.env.RELATIVITY_CLIENT_ID,
				client_secret: process.env.RELATIVITY_CLIENT_SECRET,
			}),
		});
		const payload = await response.json();
		cachedToken = {
			value: payload.access_token,
			expiresAtMs: now + payload.expires_in * MILLISECONDS_PER_SECOND,
		};
	}
	return cachedToken.value;
}
```

**Cache the token and re-acquire it on expiry — do not refresh.** Client Credentials offers no refresh tokens: **verified by NSerio against a live instance; not documented by Relativity** — the Allow Refresh Tokens control does not appear for this grant in the RelativityOne user interface. The recommended Access Token Lifetime is listed in part 3. The block uses the built-in `fetch` and no package. **The client secret** belongs in the process environment or a secret store, as Secret Management (Part B of the Migration Documentation) describes, **never in source, in a tracked `.env` file or in a bundle**. A reset invalidates the previous secret immediately, so plan the cut-over before resetting it. **Before deploying, confirm the calling identity:** every call now runs as the OAuth2 Client's Context User, not as the user in the replaced credential, and a non-administrator Context User is NSerio's recommendation. Trusted IPs restrict where a token can be requested, not a token already issued.

**Variant: browser code that does not act for a signed-in user**

**Precondition: this variant is for browser code that calls RelativityOne with no person signed in to it, so no person's identity exists to preserve. If a person signs in, use Authorization Code, variant 1.**

The Relativity request moves behind a **server-side component** that holds the secret and performs the token exchange (the Node.js process variant above); the browser calls that component on its own origin. **The component must authenticate and authorize its own callers, and the OAuth2 Client's Context User must hold the minimum permissions the operation needs. Without both, the result is an unauthenticated relay into RelativityOne:** every caller reaching it acts with the Context User's full rights, which is strictly worse than the starting point. The relay returns results to the browser, **never the token** and never the secret. For browser code the credential has been public for as long as the code has shipped; the tool does not escalate it, so this text states the exposure and the rotation:

> Treat this credential as compromised. Rotate it, and assume the repository history has published it. Completing the migration does not close this exposure; rotation does.

**When the code acts for a signed-in user.** Client Credentials is one service identity by construction. Code that asks a person for a password and sends it is the Authorization Code case, and moving to it means a new OAuth2 Client, because the flow is fixed when a client is created. The finding's flow note says when to make that choice.

Verification: **Verification note.** **A token has a finite lifetime.** **The calling identity changes.** An options object held in a variable and passed later is not read.

OAuth2 Client: Create or confirm this OAuth2 Client in RelativityOne (NSerio's observation: the menu path is Home > Authentication > OAuth2 Client Manager, as Relativity's REST API authentication page names it). This tool never connects to RelativityOne and never creates or changes the OAuth2 Client; the settings below are guidance.
- Name: a descriptive name identifying the integration it serves. Use one OAuth2 Client per integration and do not share it across integrations.
- Flow Grant Type: Client Credentials
- Context User: NSerio's recommendation: a dedicated integration user with the minimum permissions the integration needs, and not a personal account and not an administrator, because every call runs with that user's permissions and an administrator Context User gives the integration an administrator's reach. Relativity documentation (REST API authentication page): describes the Context User as usually a member of the Relativity Administrators group, though it could differ depending on the API or operation invoked; NSerio recommends against that default for an integration.
- Access Token Lifetime: 60 minutes, stated in minutes because the field is in minutes. Relativity documentation: tokens for this flow must have a short lifetime, and the recommended value matches the Identity Server default.
- Trusted IPs: Relativity documentation: the address list is checked whenever a token is requested with the OAuth2 Client's ID and secret. NSerio's recommendation: populate it with the integration's outbound IP addresses, one per line, wherever they are known and stable. It restricts where a token can be requested, checked when the token is issued; it does not restrict a token already issued, which travels in the request header and is valid from anywhere until it expires.
- Allow refresh tokens: not used by this flow. The integration requests a new access token when the current one expires.
- Enabled: on.
- Every call made with this OAuth2 Client runs with the Context User's permissions.
- Relativity documentation: the flow is fixed when the OAuth2 Client is created and cannot be changed when editing it, so moving this integration to Authorization Code later means creating a new OAuth2 Client.
<!-- generated:end JS-008 client-credentials -->

#### Authorization Code

<!-- generated:begin JS-008 authorization-code -->
**Confirming the flow.** An interactive-user signal in a file assigns Authorization Code, with this note: "An interactive-user signal in this file points to Authorization Code. Confirm the flow: use Client Credentials instead if the integration runs with credentials fixed for the integration and no signed-in user." Your user profile can also make Authorization Code the default; see Changing the default in [Confirming the flow](#confirming-the-flow). See [Confirming the flow](#confirming-the-flow) and the "Which flow" paragraph of JS-008 in [Part A](part-a-javascript.md#js-008).

Source form: **Applies to:** Node.js (`http` and `https`).

```javascript
http.request({ host: hostName, path: "/path", auth: `${userName}:${secret}` }, handler);
```

Under token authentication: **First, the destination:** if this Relativity instance is not RelativityOne — including a Relativity Server instance, whose own documentation governs — this finding is informational and the rest of this template does not apply.

```javascript
const CSRF_HEADER_VALUE = "-";
https.request({ host: hostName, path: "/path", headers: { Authorization: `Bearer ${accessToken}`, "X-CSRF-Header": CSRF_HEADER_VALUE } }, handler);
```

Remove the `auth` option. Node.js documents that sending an `Authorization` header overrides the `auth` option, but remove it anyway so the credential is not left in the source.

Credentials: **Where the credentials come from.** The text establishes that a client sends Basic, not that the destination is RelativityOne. If it is not RelativityOne, the migration is that service's own token mechanism, this finding is informational for this tool, and the reader stops here. If it is RelativityOne, the token comes from the token-acquisition part that follows, and the user name and password are replaced by the OAuth2 Client's client ID and secret, kept as Secret Management (Part B of the Migration Documentation) describes, never in source, in a tracked `.env` file or in a bundle.

**Common to all three variants.** The text has a common part and **three variants by application type**, each opening with the precondition it is for: a browser with a server-side component, a Node.js server, and a Node.js command-line tool or desktop shell. Read the precondition of each and discard the variants that do not apply.

**Sign in.** Replace the code that collected the person's credentials with a redirect to the authorize endpoint `<host>/Relativity/Identity/connect/authorize` (**verified by NSerio against a live instance; not documented by Relativity**), which leads to RelativityOne's login page. The request carries the standard authorization-code parameters — `client_id`, `response_type=code`, the registered `redirect_uri`, `state` checked on return, and `code_challenge` with `code_challenge_method=S256` when PKCE is used; these are standard OAuth and Relativity documents none of them.

**PKCE.** PKCE is required by Relativity's own documentation to obtain a refresh token and not otherwise: a Code request that does not ask for a refresh token works without it, **verified by NSerio against a live instance; not documented by Relativity**. NSerio recommends `S256` (NSerio recommendation) and does not claim that Relativity accepts any particular method. `state` is generated, stored against the session and compared on return; `nonce` is not used, and Relativity does not document whether it enforces either.

**The client secret is required.** Code does not work without a client secret: tested and rejected, **verified by NSerio against a live instance; not documented by Relativity**, which confirms Relativity's own statement. **Any secret shipped to a browser is public**, and bundling, minifying or obfuscating changes nothing, so the exchange runs where a secret can be kept.

**Exchange the code.** A form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=authorization_code`, the code, the same `redirect_uri`, the client ID and the secret, plus the `code_verifier` when PKCE is used (**verified by NSerio against a live instance; not documented by Relativity**).

**Redirect URL.** A server-side application registers **one exact HTTPS URL per environment, no wildcards**; the matching rules are not documented and no claim is made about them. A plain-HTTP loopback Redirect URL `http://127.0.0.1:<port>/<path>` on a **fixed registered port** is accepted, **verified by NSerio against a live instance; not documented by Relativity**, and is the desktop shape.

**On expiry.** With Allow Refresh Tokens enabled and PKCE used, the code holds a refresh token and sends `grant_type=refresh_token` in place of the code exchange: *inferred* from the standard OAuth shape, not tested. Relativity does not document a refresh token's lifetime, rotation or revocation, nor what disabling the client does to tokens already issued. **A refresh token is a secret.** **Single sign-on:** sign-in happens on RelativityOne's own page, so the instance's single sign-on and two-factor rules apply unchanged (*inferred*). **The code stops collecting the password:** after this change nothing receives, stores or forwards the person's user name and password, and the code holds only tokens. Every call now runs as the person who signed in, so **the calling identity changes**. **Scope:** no scope set is prescribed for Code; `openid RelativityRequestOrigin UserInfo UserInfoAccess AuthenticationID RelativityWeb` works, **verified by NSerio against a live instance; not documented by Relativity**, as working and not as minimal.

**Variant 1 — Browser with a server-side component**

**Precondition: this variant is for code that runs in a web browser and signs a person in to RelativityOne. If your code runs in Node.js, use variant 2 or variant 3.**

A browser cannot hold the secret the Code grant requires, so **the browser never calls RelativityOne**: it navigates to a route on **the application's own origin**, and a **server-side component** (variant 2's code) creates `state`, redirects to the authorize endpoint, receives the code at its registered HTTPS Redirect URL, exchanges it, keeps the tokens and gives the browser only its own session. **The browser holds a session identifier with that component and nothing from RelativityOne.** The authorize step is a **top-level navigation** (a full-window redirect, not a `fetch`).

```javascript
const LOGIN_ROUTE = "/auth/login";
const API_ROUTE = "/api/documents";

function signIn() {
	window.location.assign(LOGIN_ROUTE);
}

async function loadDocuments() {
	const response = await fetch(API_ROUTE, { credentials: "same-origin" });
	return response.json();
}
```

Both calls go to the page's own origin. `credentials: "include"` is a cookie policy and is not recommended here, because the architecture needs no cross-origin call. **Cross-origin access, both ways, both not verified:** NSerio has not run a preflight against a live instance. *If RelativityOne serves CORS to browser callers*, the legacy browser Basic call could have worked from another origin, and a browser holding a Bearer token could in principle call it directly; **this text still does not recommend that**, because the only grant that yields a token for a person needs the secret the browser cannot hold. *If it does not*, the legacy call worked only from the same origin or through a proxy the client already runs. **The recommended architecture holds under either answer.** **The component keeps the tokens on the server**, never in browser storage, which any script on the page can read (a browser behaviour, unchecked); the recommended session cookie is `HttpOnly`, `Secure` and `SameSite` (NSerio recommendation; unchecked).

**Variant 2 — Node.js server**

**Precondition: this variant is for code that runs in Node.js as a web server and acts for people who sign in to it. It is not for browser code (variant 1) or a command-line tool (variant 3).**

Standard server-side Code flow. A login route builds the authorize address with `state` (and the PKCE verifier where a refresh token is wanted), stores them against the visitor's session and redirects; RelativityOne redirects to the **callback route**, registered as an exact HTTPS URL; the callback checks `state` and exchanges the code with the secret. This is the component variant 1 relies on.

```javascript
import crypto from "node:crypto";

const IDENTITY_BASE = "https://HOST/Relativity/Identity/connect";
const REDIRECT_URI = "https://app.example.test/auth/callback";
const CLIENT_ID = process.env.RELATIVITY_CLIENT_ID;
const CLIENT_SECRET = process.env.RELATIVITY_CLIENT_SECRET;
const RANDOM_BYTES = 32;
const pendingBySession = new Map();

function base64Url(buffer) {
	return buffer.toString("base64url");
}

function beginLogin(sessionId, wantRefreshToken) {
	const state = base64Url(crypto.randomBytes(RANDOM_BYTES));
	const verifier = base64Url(crypto.randomBytes(RANDOM_BYTES));
	const challenge = base64Url(crypto.createHash("sha256").update(verifier).digest());
	pendingBySession.set(sessionId, { state, verifier });
	const query = new URLSearchParams({
		response_type: "code",
		client_id: CLIENT_ID,
		redirect_uri: REDIRECT_URI,
		state,
	});
	if (wantRefreshToken) {
		query.set("code_challenge", challenge);
		query.set("code_challenge_method", "S256");
	}
	return `${IDENTITY_BASE}/authorize?${query}`;
}

async function completeLogin(sessionId, returnedCode, returnedState) {
	const pending = pendingBySession.get(sessionId);
	pendingBySession.delete(sessionId);
	const stateMatches = pending !== undefined
		&& returnedState !== undefined
		&& Buffer.byteLength(returnedState) === Buffer.byteLength(pending.state)
		&& crypto.timingSafeEqual(Buffer.from(returnedState), Buffer.from(pending.state));
	let tokens = null;
	if (stateMatches) {
		const response = await fetch(`${IDENTITY_BASE}/token`, {
			method: "POST",
			headers: { "Content-Type": "application/x-www-form-urlencoded" },
			body: new URLSearchParams({
				grant_type: "authorization_code",
				code: returnedCode,
				redirect_uri: REDIRECT_URI,
				client_id: CLIENT_ID,
				client_secret: CLIENT_SECRET,
				code_verifier: pending.verifier,
			}),
		});
		tokens = response.ok ? await response.json() : null;
	}
	return tokens;
}
```

`state` is consumed once and compared on return; a request with no pending entry or a different `state` gets no exchange. **The scope is not prescribed** (the sketch sends none). **Where the secret travels in the token request** (the body, as sketched, or HTTP Basic client authentication) is **not verified**. The sketch does not show the HTTP server, the session cookie or a session store that survives more than one process. **Tokens stay on the server**, in a session store that is not exposed to a request.

**Variant 3 — Node.js command line or desktop**

**Precondition: this variant is for code that runs on a person's own machine, as a command-line tool or a desktop shell, and signs that person in. It is not for a web server (variant 2) or browser code (variant 1).**

The process starts a listener from the built-in `node:http` module, bound to **`127.0.0.1` only**, on the **fixed registered port**, for **one request**; opens the system browser on the authorize address **and always prints the address too**, so a machine with no browser, an SSH session or a failed launch has a manual route; reads `code` and `state` from the one request, closes the listener, checks `state`, and exchanges the code as variant 2's `completeLogin` does.

```javascript
import http from "node:http";

const LOOPBACK_HOST = "127.0.0.1";
const REGISTERED_PORT = 8123;
const CALLBACK_PATH = "/callback";
const BASE_FOR_PARSING = `http://${LOOPBACK_HOST}:${REGISTERED_PORT}`;
const WAIT_MILLISECONDS = 120000;
const STATUS_OK = 200;
const STATUS_NOT_FOUND = 404;

function waitForRedirect() {
	return new Promise((resolve, reject) => {
		const server = http.createServer((request, response) => {
			const address = new URL(request.url, BASE_FOR_PARSING);
			const isCallback = address.pathname === CALLBACK_PATH;
			response.statusCode = isCallback ? STATUS_OK : STATUS_NOT_FOUND;
			response.end(isCallback ? "You can close this window." : "Not found");
			if (isCallback) {
				server.close();
				resolve({
					code: address.searchParams.get("code"),
					state: address.searchParams.get("state"),
				});
			}
		});
		server.on("error", reject);
		server.listen(REGISTERED_PORT, LOOPBACK_HOST);
		setTimeout(() => {
			server.close();
			reject(new Error("No redirect arrived"));
		}, WAIT_MILLISECONDS).unref();
	});
}
```

**Rules for the listener:** bind to loopback only; accept one request; **check `state`** (another local process can connect to a fixed, advertised port); close the listener after the request; and handle a busy port (a second server on a port in use raises `EADDRINUSE`, so fall back to the printed address). **Opening the system browser:** Node.js core has no function for it; run the platform's own launcher with a fixed program and an argument array, never a shell string built from the address (unchecked on every system). **Where tokens are kept:** a file with restricted permissions or the operating system's keychain; on Windows the POSIX mode argument of Node.js did not restrict a file when it was tried (*checked by NSerio locally* on Node.js v24.11.1), so a restricted file there needs an access-control change by other means, and on Linux and macOS it is unchecked. **The client secret of a distributed tool is extractable:** a command-line tool or desktop shell shipped to people carries its client secret in a form anyone who holds the distribution can extract, and it protects nothing against the person running it. **A known issue:** Relativity documents no recommendation for a distributed client; this design is what NSerio verified works. **Compensating guidance** (NSerio recommendation, subject to your own security policy): Trusted IPs where the users' outbound addresses are known and stable (checked at token issuance; applicability to Code *inferred*); the recommended short Access Token Lifetime; refresh tokens only when needed; and **one OAuth2 Client per distribution**, so a leaked secret can be disabled and reset alone.

Verification: **Verification note.** **A token has a finite lifetime.** **The calling identity changes.** An options object held in a variable and passed later is not read.

OAuth2 Client: Create or confirm this OAuth2 Client in RelativityOne (NSerio's observation: the menu path is Home > Authentication > OAuth2 Client Manager, as Relativity's REST API authentication page names it). This tool never connects to RelativityOne and never creates or changes the OAuth2 Client; the settings below are guidance.
- Name: a descriptive name identifying the application it serves.
- Flow Grant Type: Code
- Redirect URLs: Relativity documentation: each URL includes the http or https protocol prefix. The exact callback URL is registered by the Relativity administrator. Server-side web applications, NSerio's recommendation: one exact HTTPS URL per environment, with no wildcards. Desktop and CLI applications: a plain-HTTP loopback URL, `http://127.0.0.1:<port>/<path>`, on a fixed port because the URL is registered in advance and the port cannot be chosen at runtime (verified by NSerio against a live instance; not documented by Relativity).
- Access Token Lifetime: 60 minutes, stated in minutes because the field is in minutes. Relativity documentation: tokens for this flow must have a short lifetime, and the recommended value matches the Identity Server default.
- Allow refresh tokens: NSerio's recommendation: leave it off unless the application must keep a user session active beyond the access token lifetime without asking the user to sign in again. If it is on, the refresh tokens are stored as secrets, per Secret Management (Part B of the Migration Documentation).
- Enabled: on.
<!-- generated:end JS-008 authorization-code -->

### JS-010 · UrlCredentialAssignment

<a id="js-010-migration"></a>

Remove the two assignments to the `URL` object and send a Bearer header. If the `URL` is passed to `fetch` it throws (checked by NSerio locally on one Node.js version), and to the Node.js `http` module it authenticates, so check where it is used. A credential assembled into an address is exposed whatever you do next: rotate it.

#### Client Credentials

<!-- generated:begin JS-010 client-credentials -->
**Confirming the flow.** When no signal in a file decides the flow, the tool assigns Client Credentials, the default, with this note: "Who supplies the credential at run time? If a person types it or signs in, the integration acts for that person: use Authorization Code. If it comes from configuration, settings, an environment variable or a secret store: use Client Credentials. No signal in this file showed which applies, so Client Credentials, the default, is recommended. Use Authorization Code instead if the integration acts on behalf of a signed-in user, or if your RelativityOne instance already has a Code-flow OAuth2 Client for this integration." A fixed-credential signal assigns it with this note: "A fixed-credential signal in this file points to Client Credentials, but how often that kind of signal is right has not been measured. Use Authorization Code instead if the integration acts on behalf of a signed-in user, or if your RelativityOne instance already has a Code-flow OAuth2 Client for this integration." See [Confirming the flow](#confirming-the-flow) and the "Which flow" paragraph of JS-010 in [Part A](part-a-javascript.md#js-010).

Source form: **Applies to:** browser and Node.js.

```javascript
const target = new URL(base);
target.username = userName;
target.password = secret;
```

Under token authentication: **First, the destination:** if this Relativity instance is not RelativityOne — including a Relativity Server instance, whose own documentation governs — this finding is informational and the rest of this template does not apply.

Remove the two assignments, and send the token: `Authorization: Bearer ${accessToken}` with `X-CSRF-Header: -`.

Credentials: **Where the credentials come from.** The text establishes that a client sends Basic, not that the destination is RelativityOne. If it is not RelativityOne, the migration is that service's own token mechanism, this finding is informational for this tool, and the reader stops here. If it is RelativityOne, the token comes from the token-acquisition part that follows, and the user name and password are replaced by the OAuth2 Client's client ID and secret, kept as Secret Management (Part B of the Migration Documentation) describes, never in source, in a tracked `.env` file or in a bundle.

**Where the token comes from.** Create an OAuth2 Client with Flow Grant Type set to Client Credentials (part 3 lists its settings), then send a form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=client_credentials`, `client_id` and `client_secret`. The token endpoint is **documented** for this grant. The token is then sent as `Authorization: Bearer <token>`, with `X-CSRF-Header: -` on REST and Kepler calls. **The scope depends on what the integration needs, and no single scope is required.** Relativity's documentation shows `SystemUserInfo` and `UserInfoAccess` and no page says which is correct or what either grants; use the value the API you call documents, and confirm it. The text has two variants, by where the code runs, and each opens with the precondition it is for: read that sentence and discard the variant that does not apply.

**Variant: a Node.js process acting as itself**

**Precondition: this variant is for a Node.js job, service or script that does not act for a signed-in person.**

```javascript
const IDENTITY_TOKEN_URL = "https://HOST/Relativity/Identity/connect/token";
const EXPIRY_MARGIN_SECONDS = 60;
const MILLISECONDS_PER_SECOND = 1000;
let cachedToken = null;

async function getAccessToken() {
	const now = Date.now();
	if (cachedToken === null || cachedToken.expiresAtMs - now < EXPIRY_MARGIN_SECONDS * MILLISECONDS_PER_SECOND) {
		const response = await fetch(IDENTITY_TOKEN_URL, {
			method: "POST",
			headers: { "Content-Type": "application/x-www-form-urlencoded" },
			body: new URLSearchParams({
				grant_type: "client_credentials",
				client_id: process.env.RELATIVITY_CLIENT_ID,
				client_secret: process.env.RELATIVITY_CLIENT_SECRET,
			}),
		});
		const payload = await response.json();
		cachedToken = {
			value: payload.access_token,
			expiresAtMs: now + payload.expires_in * MILLISECONDS_PER_SECOND,
		};
	}
	return cachedToken.value;
}
```

**Cache the token and re-acquire it on expiry — do not refresh.** Client Credentials offers no refresh tokens: **verified by NSerio against a live instance; not documented by Relativity** — the Allow Refresh Tokens control does not appear for this grant in the RelativityOne user interface. The recommended Access Token Lifetime is listed in part 3. The block uses the built-in `fetch` and no package. **The client secret** belongs in the process environment or a secret store, as Secret Management (Part B of the Migration Documentation) describes, **never in source, in a tracked `.env` file or in a bundle**. A reset invalidates the previous secret immediately, so plan the cut-over before resetting it. **Before deploying, confirm the calling identity:** every call now runs as the OAuth2 Client's Context User, not as the user in the replaced credential, and a non-administrator Context User is NSerio's recommendation. Trusted IPs restrict where a token can be requested, not a token already issued.

**Variant: browser code that does not act for a signed-in user**

**Precondition: this variant is for browser code that calls RelativityOne with no person signed in to it, so no person's identity exists to preserve. If a person signs in, use Authorization Code, variant 1.**

The Relativity request moves behind a **server-side component** that holds the secret and performs the token exchange (the Node.js process variant above); the browser calls that component on its own origin. **The component must authenticate and authorize its own callers, and the OAuth2 Client's Context User must hold the minimum permissions the operation needs. Without both, the result is an unauthenticated relay into RelativityOne:** every caller reaching it acts with the Context User's full rights, which is strictly worse than the starting point. The relay returns results to the browser, **never the token** and never the secret. For browser code the credential has been public for as long as the code has shipped; the tool does not escalate it, so this text states the exposure and the rotation:

> Treat this credential as compromised. Rotate it, and assume the repository history has published it. Completing the migration does not close this exposure; rotation does.

**When the code acts for a signed-in user.** Client Credentials is one service identity by construction. Code that asks a person for a password and sends it is the Authorization Code case, and moving to it means a new OAuth2 Client, because the flow is fixed when a client is created. The finding's flow note says when to make that choice.

Verification: **Verification note.** **A token has a finite lifetime.** **If this `URL` is passed to `fetch` it throws** (*checked by NSerio locally* on Node.js v24.11.1), and to the `http` module of Node.js it authenticates; check where it is used. A credential assembled into a URL is exposed whatever you do next:

> Treat this credential as compromised. Rotate it, and assume the repository history has published it. Completing the migration does not close this exposure; rotation does.

OAuth2 Client: Create or confirm this OAuth2 Client in RelativityOne (NSerio's observation: the menu path is Home > Authentication > OAuth2 Client Manager, as Relativity's REST API authentication page names it). This tool never connects to RelativityOne and never creates or changes the OAuth2 Client; the settings below are guidance.
- Name: a descriptive name identifying the integration it serves. Use one OAuth2 Client per integration and do not share it across integrations.
- Flow Grant Type: Client Credentials
- Context User: NSerio's recommendation: a dedicated integration user with the minimum permissions the integration needs, and not a personal account and not an administrator, because every call runs with that user's permissions and an administrator Context User gives the integration an administrator's reach. Relativity documentation (REST API authentication page): describes the Context User as usually a member of the Relativity Administrators group, though it could differ depending on the API or operation invoked; NSerio recommends against that default for an integration.
- Access Token Lifetime: 60 minutes, stated in minutes because the field is in minutes. Relativity documentation: tokens for this flow must have a short lifetime, and the recommended value matches the Identity Server default.
- Trusted IPs: Relativity documentation: the address list is checked whenever a token is requested with the OAuth2 Client's ID and secret. NSerio's recommendation: populate it with the integration's outbound IP addresses, one per line, wherever they are known and stable. It restricts where a token can be requested, checked when the token is issued; it does not restrict a token already issued, which travels in the request header and is valid from anywhere until it expires.
- Allow refresh tokens: not used by this flow. The integration requests a new access token when the current one expires.
- Enabled: on.
- Every call made with this OAuth2 Client runs with the Context User's permissions.
- Relativity documentation: the flow is fixed when the OAuth2 Client is created and cannot be changed when editing it, so moving this integration to Authorization Code later means creating a new OAuth2 Client.
<!-- generated:end JS-010 client-credentials -->

#### Authorization Code

<!-- generated:begin JS-010 authorization-code -->
**Confirming the flow.** An interactive-user signal in a file assigns Authorization Code, with this note: "An interactive-user signal in this file points to Authorization Code. Confirm the flow: use Client Credentials instead if the integration runs with credentials fixed for the integration and no signed-in user." Your user profile can also make Authorization Code the default; see Changing the default in [Confirming the flow](#confirming-the-flow). See [Confirming the flow](#confirming-the-flow) and the "Which flow" paragraph of JS-010 in [Part A](part-a-javascript.md#js-010).

Source form: **Applies to:** browser and Node.js.

```javascript
const target = new URL(base);
target.username = userName;
target.password = secret;
```

Under token authentication: **First, the destination:** if this Relativity instance is not RelativityOne — including a Relativity Server instance, whose own documentation governs — this finding is informational and the rest of this template does not apply.

Remove the two assignments, and send the token: `Authorization: Bearer ${accessToken}` with `X-CSRF-Header: -`.

Credentials: **Where the credentials come from.** The text establishes that a client sends Basic, not that the destination is RelativityOne. If it is not RelativityOne, the migration is that service's own token mechanism, this finding is informational for this tool, and the reader stops here. If it is RelativityOne, the token comes from the token-acquisition part that follows, and the user name and password are replaced by the OAuth2 Client's client ID and secret, kept as Secret Management (Part B of the Migration Documentation) describes, never in source, in a tracked `.env` file or in a bundle.

**Common to all three variants.** The text has a common part and **three variants by application type**, each opening with the precondition it is for: a browser with a server-side component, a Node.js server, and a Node.js command-line tool or desktop shell. Read the precondition of each and discard the variants that do not apply.

**Sign in.** Replace the code that collected the person's credentials with a redirect to the authorize endpoint `<host>/Relativity/Identity/connect/authorize` (**verified by NSerio against a live instance; not documented by Relativity**), which leads to RelativityOne's login page. The request carries the standard authorization-code parameters — `client_id`, `response_type=code`, the registered `redirect_uri`, `state` checked on return, and `code_challenge` with `code_challenge_method=S256` when PKCE is used; these are standard OAuth and Relativity documents none of them.

**PKCE.** PKCE is required by Relativity's own documentation to obtain a refresh token and not otherwise: a Code request that does not ask for a refresh token works without it, **verified by NSerio against a live instance; not documented by Relativity**. NSerio recommends `S256` (NSerio recommendation) and does not claim that Relativity accepts any particular method. `state` is generated, stored against the session and compared on return; `nonce` is not used, and Relativity does not document whether it enforces either.

**The client secret is required.** Code does not work without a client secret: tested and rejected, **verified by NSerio against a live instance; not documented by Relativity**, which confirms Relativity's own statement. **Any secret shipped to a browser is public**, and bundling, minifying or obfuscating changes nothing, so the exchange runs where a secret can be kept.

**Exchange the code.** A form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=authorization_code`, the code, the same `redirect_uri`, the client ID and the secret, plus the `code_verifier` when PKCE is used (**verified by NSerio against a live instance; not documented by Relativity**).

**Redirect URL.** A server-side application registers **one exact HTTPS URL per environment, no wildcards**; the matching rules are not documented and no claim is made about them. A plain-HTTP loopback Redirect URL `http://127.0.0.1:<port>/<path>` on a **fixed registered port** is accepted, **verified by NSerio against a live instance; not documented by Relativity**, and is the desktop shape.

**On expiry.** With Allow Refresh Tokens enabled and PKCE used, the code holds a refresh token and sends `grant_type=refresh_token` in place of the code exchange: *inferred* from the standard OAuth shape, not tested. Relativity does not document a refresh token's lifetime, rotation or revocation, nor what disabling the client does to tokens already issued. **A refresh token is a secret.** **Single sign-on:** sign-in happens on RelativityOne's own page, so the instance's single sign-on and two-factor rules apply unchanged (*inferred*). **The code stops collecting the password:** after this change nothing receives, stores or forwards the person's user name and password, and the code holds only tokens. Every call now runs as the person who signed in, so **the calling identity changes**. **Scope:** no scope set is prescribed for Code; `openid RelativityRequestOrigin UserInfo UserInfoAccess AuthenticationID RelativityWeb` works, **verified by NSerio against a live instance; not documented by Relativity**, as working and not as minimal.

**Variant 1 — Browser with a server-side component**

**Precondition: this variant is for code that runs in a web browser and signs a person in to RelativityOne. If your code runs in Node.js, use variant 2 or variant 3.**

A browser cannot hold the secret the Code grant requires, so **the browser never calls RelativityOne**: it navigates to a route on **the application's own origin**, and a **server-side component** (variant 2's code) creates `state`, redirects to the authorize endpoint, receives the code at its registered HTTPS Redirect URL, exchanges it, keeps the tokens and gives the browser only its own session. **The browser holds a session identifier with that component and nothing from RelativityOne.** The authorize step is a **top-level navigation** (a full-window redirect, not a `fetch`).

```javascript
const LOGIN_ROUTE = "/auth/login";
const API_ROUTE = "/api/documents";

function signIn() {
	window.location.assign(LOGIN_ROUTE);
}

async function loadDocuments() {
	const response = await fetch(API_ROUTE, { credentials: "same-origin" });
	return response.json();
}
```

Both calls go to the page's own origin. `credentials: "include"` is a cookie policy and is not recommended here, because the architecture needs no cross-origin call. **Cross-origin access, both ways, both not verified:** NSerio has not run a preflight against a live instance. *If RelativityOne serves CORS to browser callers*, the legacy browser Basic call could have worked from another origin, and a browser holding a Bearer token could in principle call it directly; **this text still does not recommend that**, because the only grant that yields a token for a person needs the secret the browser cannot hold. *If it does not*, the legacy call worked only from the same origin or through a proxy the client already runs. **The recommended architecture holds under either answer.** **The component keeps the tokens on the server**, never in browser storage, which any script on the page can read (a browser behaviour, unchecked); the recommended session cookie is `HttpOnly`, `Secure` and `SameSite` (NSerio recommendation; unchecked).

**Variant 2 — Node.js server**

**Precondition: this variant is for code that runs in Node.js as a web server and acts for people who sign in to it. It is not for browser code (variant 1) or a command-line tool (variant 3).**

Standard server-side Code flow. A login route builds the authorize address with `state` (and the PKCE verifier where a refresh token is wanted), stores them against the visitor's session and redirects; RelativityOne redirects to the **callback route**, registered as an exact HTTPS URL; the callback checks `state` and exchanges the code with the secret. This is the component variant 1 relies on.

```javascript
import crypto from "node:crypto";

const IDENTITY_BASE = "https://HOST/Relativity/Identity/connect";
const REDIRECT_URI = "https://app.example.test/auth/callback";
const CLIENT_ID = process.env.RELATIVITY_CLIENT_ID;
const CLIENT_SECRET = process.env.RELATIVITY_CLIENT_SECRET;
const RANDOM_BYTES = 32;
const pendingBySession = new Map();

function base64Url(buffer) {
	return buffer.toString("base64url");
}

function beginLogin(sessionId, wantRefreshToken) {
	const state = base64Url(crypto.randomBytes(RANDOM_BYTES));
	const verifier = base64Url(crypto.randomBytes(RANDOM_BYTES));
	const challenge = base64Url(crypto.createHash("sha256").update(verifier).digest());
	pendingBySession.set(sessionId, { state, verifier });
	const query = new URLSearchParams({
		response_type: "code",
		client_id: CLIENT_ID,
		redirect_uri: REDIRECT_URI,
		state,
	});
	if (wantRefreshToken) {
		query.set("code_challenge", challenge);
		query.set("code_challenge_method", "S256");
	}
	return `${IDENTITY_BASE}/authorize?${query}`;
}

async function completeLogin(sessionId, returnedCode, returnedState) {
	const pending = pendingBySession.get(sessionId);
	pendingBySession.delete(sessionId);
	const stateMatches = pending !== undefined
		&& returnedState !== undefined
		&& Buffer.byteLength(returnedState) === Buffer.byteLength(pending.state)
		&& crypto.timingSafeEqual(Buffer.from(returnedState), Buffer.from(pending.state));
	let tokens = null;
	if (stateMatches) {
		const response = await fetch(`${IDENTITY_BASE}/token`, {
			method: "POST",
			headers: { "Content-Type": "application/x-www-form-urlencoded" },
			body: new URLSearchParams({
				grant_type: "authorization_code",
				code: returnedCode,
				redirect_uri: REDIRECT_URI,
				client_id: CLIENT_ID,
				client_secret: CLIENT_SECRET,
				code_verifier: pending.verifier,
			}),
		});
		tokens = response.ok ? await response.json() : null;
	}
	return tokens;
}
```

`state` is consumed once and compared on return; a request with no pending entry or a different `state` gets no exchange. **The scope is not prescribed** (the sketch sends none). **Where the secret travels in the token request** (the body, as sketched, or HTTP Basic client authentication) is **not verified**. The sketch does not show the HTTP server, the session cookie or a session store that survives more than one process. **Tokens stay on the server**, in a session store that is not exposed to a request.

**Variant 3 — Node.js command line or desktop**

**Precondition: this variant is for code that runs on a person's own machine, as a command-line tool or a desktop shell, and signs that person in. It is not for a web server (variant 2) or browser code (variant 1).**

The process starts a listener from the built-in `node:http` module, bound to **`127.0.0.1` only**, on the **fixed registered port**, for **one request**; opens the system browser on the authorize address **and always prints the address too**, so a machine with no browser, an SSH session or a failed launch has a manual route; reads `code` and `state` from the one request, closes the listener, checks `state`, and exchanges the code as variant 2's `completeLogin` does.

```javascript
import http from "node:http";

const LOOPBACK_HOST = "127.0.0.1";
const REGISTERED_PORT = 8123;
const CALLBACK_PATH = "/callback";
const BASE_FOR_PARSING = `http://${LOOPBACK_HOST}:${REGISTERED_PORT}`;
const WAIT_MILLISECONDS = 120000;
const STATUS_OK = 200;
const STATUS_NOT_FOUND = 404;

function waitForRedirect() {
	return new Promise((resolve, reject) => {
		const server = http.createServer((request, response) => {
			const address = new URL(request.url, BASE_FOR_PARSING);
			const isCallback = address.pathname === CALLBACK_PATH;
			response.statusCode = isCallback ? STATUS_OK : STATUS_NOT_FOUND;
			response.end(isCallback ? "You can close this window." : "Not found");
			if (isCallback) {
				server.close();
				resolve({
					code: address.searchParams.get("code"),
					state: address.searchParams.get("state"),
				});
			}
		});
		server.on("error", reject);
		server.listen(REGISTERED_PORT, LOOPBACK_HOST);
		setTimeout(() => {
			server.close();
			reject(new Error("No redirect arrived"));
		}, WAIT_MILLISECONDS).unref();
	});
}
```

**Rules for the listener:** bind to loopback only; accept one request; **check `state`** (another local process can connect to a fixed, advertised port); close the listener after the request; and handle a busy port (a second server on a port in use raises `EADDRINUSE`, so fall back to the printed address). **Opening the system browser:** Node.js core has no function for it; run the platform's own launcher with a fixed program and an argument array, never a shell string built from the address (unchecked on every system). **Where tokens are kept:** a file with restricted permissions or the operating system's keychain; on Windows the POSIX mode argument of Node.js did not restrict a file when it was tried (*checked by NSerio locally* on Node.js v24.11.1), so a restricted file there needs an access-control change by other means, and on Linux and macOS it is unchecked. **The client secret of a distributed tool is extractable:** a command-line tool or desktop shell shipped to people carries its client secret in a form anyone who holds the distribution can extract, and it protects nothing against the person running it. **A known issue:** Relativity documents no recommendation for a distributed client; this design is what NSerio verified works. **Compensating guidance** (NSerio recommendation, subject to your own security policy): Trusted IPs where the users' outbound addresses are known and stable (checked at token issuance; applicability to Code *inferred*); the recommended short Access Token Lifetime; refresh tokens only when needed; and **one OAuth2 Client per distribution**, so a leaked secret can be disabled and reset alone.

Verification: **Verification note.** **A token has a finite lifetime.** **If this `URL` is passed to `fetch` it throws** (*checked by NSerio locally* on Node.js v24.11.1), and to the `http` module of Node.js it authenticates; check where it is used. A credential assembled into a URL is exposed whatever you do next:

> Treat this credential as compromised. Rotate it, and assume the repository history has published it. Completing the migration does not close this exposure; rotation does.

OAuth2 Client: Create or confirm this OAuth2 Client in RelativityOne (NSerio's observation: the menu path is Home > Authentication > OAuth2 Client Manager, as Relativity's REST API authentication page names it). This tool never connects to RelativityOne and never creates or changes the OAuth2 Client; the settings below are guidance.
- Name: a descriptive name identifying the application it serves.
- Flow Grant Type: Code
- Redirect URLs: Relativity documentation: each URL includes the http or https protocol prefix. The exact callback URL is registered by the Relativity administrator. Server-side web applications, NSerio's recommendation: one exact HTTPS URL per environment, with no wildcards. Desktop and CLI applications: a plain-HTTP loopback URL, `http://127.0.0.1:<port>/<path>`, on a fixed port because the URL is registered in advance and the port cannot be chosen at runtime (verified by NSerio against a live instance; not documented by Relativity).
- Access Token Lifetime: 60 minutes, stated in minutes because the field is in minutes. Relativity documentation: tokens for this flow must have a short lifetime, and the recommended value matches the Identity Server default.
- Allow refresh tokens: NSerio's recommendation: leave it off unless the application must keep a user session active beyond the access token lifetime without asking the user to sign in again. If it is on, the refresh tokens are stored as secrets, per Secret Management (Part B of the Migration Documentation).
- Enabled: on.
<!-- generated:end JS-010 authorization-code -->

### JS-012 · ClientAuthMethodSingleArgument

<a id="js-012-migration"></a>

Replace the one-argument `.auth(` call, or the three-argument call that selects `type` `auto`, with a Bearer `Authorization` header set on the request. A call that selects `bearer` is the migrated code and is not read.

#### Client Credentials

<!-- generated:begin JS-012 client-credentials -->
**Confirming the flow.** When no signal in a file decides the flow, the tool assigns Client Credentials, the default, with this note: "Who supplies the credential at run time? If a person types it or signs in, the integration acts for that person: use Authorization Code. If it comes from configuration, settings, an environment variable or a secret store: use Client Credentials. No signal in this file showed which applies, so Client Credentials, the default, is recommended. Use Authorization Code instead if the integration acts on behalf of a signed-in user, or if your RelativityOne instance already has a Code-flow OAuth2 Client for this integration." A fixed-credential signal assigns it with this note: "A fixed-credential signal in this file points to Client Credentials, but how often that kind of signal is right has not been measured. Use Authorization Code instead if the integration acts on behalf of a signed-in user, or if your RelativityOne instance already has a Code-flow OAuth2 Client for this integration." See [Confirming the flow](#confirming-the-flow) and the "Which flow" paragraph of JS-012 in [Part A](part-a-javascript.md#js-012).

Source form: **Applies to:** browser and Node.js; `superagent`.

```javascript
await superagent.get(url).auth(credentialPair);
```

Under token authentication: **First, the destination:** if this Relativity instance is not RelativityOne — including a Relativity Server instance, whose own documentation governs — this finding is informational and the rest of this template does not apply.

```javascript
const CSRF_HEADER_VALUE = "-";
await superagent.get(url).set("Authorization", `Bearer ${accessToken}`).set("X-CSRF-Header", CSRF_HEADER_VALUE);
```

Credentials: **Where the credentials come from.** The text establishes that a client sends Basic, not that the destination is RelativityOne. If it is not RelativityOne, the migration is that service's own token mechanism, this finding is informational for this tool, and the reader stops here. If it is RelativityOne, the token comes from the token-acquisition part that follows, and the user name and password are replaced by the OAuth2 Client's client ID and secret, kept as Secret Management (Part B of the Migration Documentation) describes, never in source, in a tracked `.env` file or in a bundle.

**Where the token comes from.** Create an OAuth2 Client with Flow Grant Type set to Client Credentials (part 3 lists its settings), then send a form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=client_credentials`, `client_id` and `client_secret`. The token endpoint is **documented** for this grant. The token is then sent as `Authorization: Bearer <token>`, with `X-CSRF-Header: -` on REST and Kepler calls. **The scope depends on what the integration needs, and no single scope is required.** Relativity's documentation shows `SystemUserInfo` and `UserInfoAccess` and no page says which is correct or what either grants; use the value the API you call documents, and confirm it. The text has two variants, by where the code runs, and each opens with the precondition it is for: read that sentence and discard the variant that does not apply.

**Variant: a Node.js process acting as itself**

**Precondition: this variant is for a Node.js job, service or script that does not act for a signed-in person.**

```javascript
const IDENTITY_TOKEN_URL = "https://HOST/Relativity/Identity/connect/token";
const EXPIRY_MARGIN_SECONDS = 60;
const MILLISECONDS_PER_SECOND = 1000;
let cachedToken = null;

async function getAccessToken() {
	const now = Date.now();
	if (cachedToken === null || cachedToken.expiresAtMs - now < EXPIRY_MARGIN_SECONDS * MILLISECONDS_PER_SECOND) {
		const response = await fetch(IDENTITY_TOKEN_URL, {
			method: "POST",
			headers: { "Content-Type": "application/x-www-form-urlencoded" },
			body: new URLSearchParams({
				grant_type: "client_credentials",
				client_id: process.env.RELATIVITY_CLIENT_ID,
				client_secret: process.env.RELATIVITY_CLIENT_SECRET,
			}),
		});
		const payload = await response.json();
		cachedToken = {
			value: payload.access_token,
			expiresAtMs: now + payload.expires_in * MILLISECONDS_PER_SECOND,
		};
	}
	return cachedToken.value;
}
```

**Cache the token and re-acquire it on expiry — do not refresh.** Client Credentials offers no refresh tokens: **verified by NSerio against a live instance; not documented by Relativity** — the Allow Refresh Tokens control does not appear for this grant in the RelativityOne user interface. The recommended Access Token Lifetime is listed in part 3. The block uses the built-in `fetch` and no package. **The client secret** belongs in the process environment or a secret store, as Secret Management (Part B of the Migration Documentation) describes, **never in source, in a tracked `.env` file or in a bundle**. A reset invalidates the previous secret immediately, so plan the cut-over before resetting it. **Before deploying, confirm the calling identity:** every call now runs as the OAuth2 Client's Context User, not as the user in the replaced credential, and a non-administrator Context User is NSerio's recommendation. Trusted IPs restrict where a token can be requested, not a token already issued.

**Variant: browser code that does not act for a signed-in user**

**Precondition: this variant is for browser code that calls RelativityOne with no person signed in to it, so no person's identity exists to preserve. If a person signs in, use Authorization Code, variant 1.**

The Relativity request moves behind a **server-side component** that holds the secret and performs the token exchange (the Node.js process variant above); the browser calls that component on its own origin. **The component must authenticate and authorize its own callers, and the OAuth2 Client's Context User must hold the minimum permissions the operation needs. Without both, the result is an unauthenticated relay into RelativityOne:** every caller reaching it acts with the Context User's full rights, which is strictly worse than the starting point. The relay returns results to the browser, **never the token** and never the secret. For browser code the credential has been public for as long as the code has shipped; the tool does not escalate it, so this text states the exposure and the rotation:

> Treat this credential as compromised. Rotate it, and assume the repository history has published it. Completing the migration does not close this exposure; rotation does.

**When the code acts for a signed-in user.** Client Credentials is one service identity by construction. Code that asks a person for a password and sends it is the Authorization Code case, and moving to it means a new OAuth2 Client, because the flow is fixed when a client is created. The finding's flow note says when to make that choice.

Verification: **Verification note.** **The tool saw a single-argument `.auth(` on a request chain, or a three-argument one whose options select `type: "auto"`.** The single-argument form defaults to Basic at the package version read; the `auto` form leaves the scheme to the user agent and does not fix it in the text, which is why both are reported at Medium. **A call that selects `bearer` is the migrated code and is not read.** **A token has a finite lifetime.**

**If this code runs in a browser, the credential has been public for as long as the code has shipped.** The exposure and the rotation are stated here and the tool does not escalate it:

> Treat this credential as compromised. Rotate it, and assume the repository history has published it. Completing the migration does not close this exposure; rotation does.

OAuth2 Client: Create or confirm this OAuth2 Client in RelativityOne (NSerio's observation: the menu path is Home > Authentication > OAuth2 Client Manager, as Relativity's REST API authentication page names it). This tool never connects to RelativityOne and never creates or changes the OAuth2 Client; the settings below are guidance.
- Name: a descriptive name identifying the integration it serves. Use one OAuth2 Client per integration and do not share it across integrations.
- Flow Grant Type: Client Credentials
- Context User: NSerio's recommendation: a dedicated integration user with the minimum permissions the integration needs, and not a personal account and not an administrator, because every call runs with that user's permissions and an administrator Context User gives the integration an administrator's reach. Relativity documentation (REST API authentication page): describes the Context User as usually a member of the Relativity Administrators group, though it could differ depending on the API or operation invoked; NSerio recommends against that default for an integration.
- Access Token Lifetime: 60 minutes, stated in minutes because the field is in minutes. Relativity documentation: tokens for this flow must have a short lifetime, and the recommended value matches the Identity Server default.
- Trusted IPs: Relativity documentation: the address list is checked whenever a token is requested with the OAuth2 Client's ID and secret. NSerio's recommendation: populate it with the integration's outbound IP addresses, one per line, wherever they are known and stable. It restricts where a token can be requested, checked when the token is issued; it does not restrict a token already issued, which travels in the request header and is valid from anywhere until it expires.
- Allow refresh tokens: not used by this flow. The integration requests a new access token when the current one expires.
- Enabled: on.
- Every call made with this OAuth2 Client runs with the Context User's permissions.
- Relativity documentation: the flow is fixed when the OAuth2 Client is created and cannot be changed when editing it, so moving this integration to Authorization Code later means creating a new OAuth2 Client.
<!-- generated:end JS-012 client-credentials -->

#### Authorization Code

<!-- generated:begin JS-012 authorization-code -->
**Confirming the flow.** An interactive-user signal in a file assigns Authorization Code, with this note: "An interactive-user signal in this file points to Authorization Code. Confirm the flow: use Client Credentials instead if the integration runs with credentials fixed for the integration and no signed-in user." Your user profile can also make Authorization Code the default; see Changing the default in [Confirming the flow](#confirming-the-flow). See [Confirming the flow](#confirming-the-flow) and the "Which flow" paragraph of JS-012 in [Part A](part-a-javascript.md#js-012).

Source form: **Applies to:** browser and Node.js; `superagent`.

```javascript
await superagent.get(url).auth(credentialPair);
```

Under token authentication: **First, the destination:** if this Relativity instance is not RelativityOne — including a Relativity Server instance, whose own documentation governs — this finding is informational and the rest of this template does not apply.

```javascript
const CSRF_HEADER_VALUE = "-";
await superagent.get(url).set("Authorization", `Bearer ${accessToken}`).set("X-CSRF-Header", CSRF_HEADER_VALUE);
```

Credentials: **Where the credentials come from.** The text establishes that a client sends Basic, not that the destination is RelativityOne. If it is not RelativityOne, the migration is that service's own token mechanism, this finding is informational for this tool, and the reader stops here. If it is RelativityOne, the token comes from the token-acquisition part that follows, and the user name and password are replaced by the OAuth2 Client's client ID and secret, kept as Secret Management (Part B of the Migration Documentation) describes, never in source, in a tracked `.env` file or in a bundle.

**Common to all three variants.** The text has a common part and **three variants by application type**, each opening with the precondition it is for: a browser with a server-side component, a Node.js server, and a Node.js command-line tool or desktop shell. Read the precondition of each and discard the variants that do not apply.

**Sign in.** Replace the code that collected the person's credentials with a redirect to the authorize endpoint `<host>/Relativity/Identity/connect/authorize` (**verified by NSerio against a live instance; not documented by Relativity**), which leads to RelativityOne's login page. The request carries the standard authorization-code parameters — `client_id`, `response_type=code`, the registered `redirect_uri`, `state` checked on return, and `code_challenge` with `code_challenge_method=S256` when PKCE is used; these are standard OAuth and Relativity documents none of them.

**PKCE.** PKCE is required by Relativity's own documentation to obtain a refresh token and not otherwise: a Code request that does not ask for a refresh token works without it, **verified by NSerio against a live instance; not documented by Relativity**. NSerio recommends `S256` (NSerio recommendation) and does not claim that Relativity accepts any particular method. `state` is generated, stored against the session and compared on return; `nonce` is not used, and Relativity does not document whether it enforces either.

**The client secret is required.** Code does not work without a client secret: tested and rejected, **verified by NSerio against a live instance; not documented by Relativity**, which confirms Relativity's own statement. **Any secret shipped to a browser is public**, and bundling, minifying or obfuscating changes nothing, so the exchange runs where a secret can be kept.

**Exchange the code.** A form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=authorization_code`, the code, the same `redirect_uri`, the client ID and the secret, plus the `code_verifier` when PKCE is used (**verified by NSerio against a live instance; not documented by Relativity**).

**Redirect URL.** A server-side application registers **one exact HTTPS URL per environment, no wildcards**; the matching rules are not documented and no claim is made about them. A plain-HTTP loopback Redirect URL `http://127.0.0.1:<port>/<path>` on a **fixed registered port** is accepted, **verified by NSerio against a live instance; not documented by Relativity**, and is the desktop shape.

**On expiry.** With Allow Refresh Tokens enabled and PKCE used, the code holds a refresh token and sends `grant_type=refresh_token` in place of the code exchange: *inferred* from the standard OAuth shape, not tested. Relativity does not document a refresh token's lifetime, rotation or revocation, nor what disabling the client does to tokens already issued. **A refresh token is a secret.** **Single sign-on:** sign-in happens on RelativityOne's own page, so the instance's single sign-on and two-factor rules apply unchanged (*inferred*). **The code stops collecting the password:** after this change nothing receives, stores or forwards the person's user name and password, and the code holds only tokens. Every call now runs as the person who signed in, so **the calling identity changes**. **Scope:** no scope set is prescribed for Code; `openid RelativityRequestOrigin UserInfo UserInfoAccess AuthenticationID RelativityWeb` works, **verified by NSerio against a live instance; not documented by Relativity**, as working and not as minimal.

**Variant 1 — Browser with a server-side component**

**Precondition: this variant is for code that runs in a web browser and signs a person in to RelativityOne. If your code runs in Node.js, use variant 2 or variant 3.**

A browser cannot hold the secret the Code grant requires, so **the browser never calls RelativityOne**: it navigates to a route on **the application's own origin**, and a **server-side component** (variant 2's code) creates `state`, redirects to the authorize endpoint, receives the code at its registered HTTPS Redirect URL, exchanges it, keeps the tokens and gives the browser only its own session. **The browser holds a session identifier with that component and nothing from RelativityOne.** The authorize step is a **top-level navigation** (a full-window redirect, not a `fetch`).

```javascript
const LOGIN_ROUTE = "/auth/login";
const API_ROUTE = "/api/documents";

function signIn() {
	window.location.assign(LOGIN_ROUTE);
}

async function loadDocuments() {
	const response = await fetch(API_ROUTE, { credentials: "same-origin" });
	return response.json();
}
```

Both calls go to the page's own origin. `credentials: "include"` is a cookie policy and is not recommended here, because the architecture needs no cross-origin call. **Cross-origin access, both ways, both not verified:** NSerio has not run a preflight against a live instance. *If RelativityOne serves CORS to browser callers*, the legacy browser Basic call could have worked from another origin, and a browser holding a Bearer token could in principle call it directly; **this text still does not recommend that**, because the only grant that yields a token for a person needs the secret the browser cannot hold. *If it does not*, the legacy call worked only from the same origin or through a proxy the client already runs. **The recommended architecture holds under either answer.** **The component keeps the tokens on the server**, never in browser storage, which any script on the page can read (a browser behaviour, unchecked); the recommended session cookie is `HttpOnly`, `Secure` and `SameSite` (NSerio recommendation; unchecked).

**Variant 2 — Node.js server**

**Precondition: this variant is for code that runs in Node.js as a web server and acts for people who sign in to it. It is not for browser code (variant 1) or a command-line tool (variant 3).**

Standard server-side Code flow. A login route builds the authorize address with `state` (and the PKCE verifier where a refresh token is wanted), stores them against the visitor's session and redirects; RelativityOne redirects to the **callback route**, registered as an exact HTTPS URL; the callback checks `state` and exchanges the code with the secret. This is the component variant 1 relies on.

```javascript
import crypto from "node:crypto";

const IDENTITY_BASE = "https://HOST/Relativity/Identity/connect";
const REDIRECT_URI = "https://app.example.test/auth/callback";
const CLIENT_ID = process.env.RELATIVITY_CLIENT_ID;
const CLIENT_SECRET = process.env.RELATIVITY_CLIENT_SECRET;
const RANDOM_BYTES = 32;
const pendingBySession = new Map();

function base64Url(buffer) {
	return buffer.toString("base64url");
}

function beginLogin(sessionId, wantRefreshToken) {
	const state = base64Url(crypto.randomBytes(RANDOM_BYTES));
	const verifier = base64Url(crypto.randomBytes(RANDOM_BYTES));
	const challenge = base64Url(crypto.createHash("sha256").update(verifier).digest());
	pendingBySession.set(sessionId, { state, verifier });
	const query = new URLSearchParams({
		response_type: "code",
		client_id: CLIENT_ID,
		redirect_uri: REDIRECT_URI,
		state,
	});
	if (wantRefreshToken) {
		query.set("code_challenge", challenge);
		query.set("code_challenge_method", "S256");
	}
	return `${IDENTITY_BASE}/authorize?${query}`;
}

async function completeLogin(sessionId, returnedCode, returnedState) {
	const pending = pendingBySession.get(sessionId);
	pendingBySession.delete(sessionId);
	const stateMatches = pending !== undefined
		&& returnedState !== undefined
		&& Buffer.byteLength(returnedState) === Buffer.byteLength(pending.state)
		&& crypto.timingSafeEqual(Buffer.from(returnedState), Buffer.from(pending.state));
	let tokens = null;
	if (stateMatches) {
		const response = await fetch(`${IDENTITY_BASE}/token`, {
			method: "POST",
			headers: { "Content-Type": "application/x-www-form-urlencoded" },
			body: new URLSearchParams({
				grant_type: "authorization_code",
				code: returnedCode,
				redirect_uri: REDIRECT_URI,
				client_id: CLIENT_ID,
				client_secret: CLIENT_SECRET,
				code_verifier: pending.verifier,
			}),
		});
		tokens = response.ok ? await response.json() : null;
	}
	return tokens;
}
```

`state` is consumed once and compared on return; a request with no pending entry or a different `state` gets no exchange. **The scope is not prescribed** (the sketch sends none). **Where the secret travels in the token request** (the body, as sketched, or HTTP Basic client authentication) is **not verified**. The sketch does not show the HTTP server, the session cookie or a session store that survives more than one process. **Tokens stay on the server**, in a session store that is not exposed to a request.

**Variant 3 — Node.js command line or desktop**

**Precondition: this variant is for code that runs on a person's own machine, as a command-line tool or a desktop shell, and signs that person in. It is not for a web server (variant 2) or browser code (variant 1).**

The process starts a listener from the built-in `node:http` module, bound to **`127.0.0.1` only**, on the **fixed registered port**, for **one request**; opens the system browser on the authorize address **and always prints the address too**, so a machine with no browser, an SSH session or a failed launch has a manual route; reads `code` and `state` from the one request, closes the listener, checks `state`, and exchanges the code as variant 2's `completeLogin` does.

```javascript
import http from "node:http";

const LOOPBACK_HOST = "127.0.0.1";
const REGISTERED_PORT = 8123;
const CALLBACK_PATH = "/callback";
const BASE_FOR_PARSING = `http://${LOOPBACK_HOST}:${REGISTERED_PORT}`;
const WAIT_MILLISECONDS = 120000;
const STATUS_OK = 200;
const STATUS_NOT_FOUND = 404;

function waitForRedirect() {
	return new Promise((resolve, reject) => {
		const server = http.createServer((request, response) => {
			const address = new URL(request.url, BASE_FOR_PARSING);
			const isCallback = address.pathname === CALLBACK_PATH;
			response.statusCode = isCallback ? STATUS_OK : STATUS_NOT_FOUND;
			response.end(isCallback ? "You can close this window." : "Not found");
			if (isCallback) {
				server.close();
				resolve({
					code: address.searchParams.get("code"),
					state: address.searchParams.get("state"),
				});
			}
		});
		server.on("error", reject);
		server.listen(REGISTERED_PORT, LOOPBACK_HOST);
		setTimeout(() => {
			server.close();
			reject(new Error("No redirect arrived"));
		}, WAIT_MILLISECONDS).unref();
	});
}
```

**Rules for the listener:** bind to loopback only; accept one request; **check `state`** (another local process can connect to a fixed, advertised port); close the listener after the request; and handle a busy port (a second server on a port in use raises `EADDRINUSE`, so fall back to the printed address). **Opening the system browser:** Node.js core has no function for it; run the platform's own launcher with a fixed program and an argument array, never a shell string built from the address (unchecked on every system). **Where tokens are kept:** a file with restricted permissions or the operating system's keychain; on Windows the POSIX mode argument of Node.js did not restrict a file when it was tried (*checked by NSerio locally* on Node.js v24.11.1), so a restricted file there needs an access-control change by other means, and on Linux and macOS it is unchecked. **The client secret of a distributed tool is extractable:** a command-line tool or desktop shell shipped to people carries its client secret in a form anyone who holds the distribution can extract, and it protects nothing against the person running it. **A known issue:** Relativity documents no recommendation for a distributed client; this design is what NSerio verified works. **Compensating guidance** (NSerio recommendation, subject to your own security policy): Trusted IPs where the users' outbound addresses are known and stable (checked at token issuance; applicability to Code *inferred*); the recommended short Access Token Lifetime; refresh tokens only when needed; and **one OAuth2 Client per distribution**, so a leaked secret can be disabled and reset alone.

Verification: **Verification note.** **The tool saw a single-argument `.auth(` on a request chain, or a three-argument one whose options select `type: "auto"`.** The single-argument form defaults to Basic at the package version read; the `auto` form leaves the scheme to the user agent and does not fix it in the text, which is why both are reported at Medium. **A call that selects `bearer` is the migrated code and is not read.** **A token has a finite lifetime.**

**If this code runs in a browser, the credential has been public for as long as the code has shipped.** The exposure and the rotation are stated here and the tool does not escalate it:

> Treat this credential as compromised. Rotate it, and assume the repository history has published it. Completing the migration does not close this exposure; rotation does.

OAuth2 Client: Create or confirm this OAuth2 Client in RelativityOne (NSerio's observation: the menu path is Home > Authentication > OAuth2 Client Manager, as Relativity's REST API authentication page names it). This tool never connects to RelativityOne and never creates or changes the OAuth2 Client; the settings below are guidance.
- Name: a descriptive name identifying the application it serves.
- Flow Grant Type: Code
- Redirect URLs: Relativity documentation: each URL includes the http or https protocol prefix. The exact callback URL is registered by the Relativity administrator. Server-side web applications, NSerio's recommendation: one exact HTTPS URL per environment, with no wildcards. Desktop and CLI applications: a plain-HTTP loopback URL, `http://127.0.0.1:<port>/<path>`, on a fixed port because the URL is registered in advance and the port cannot be chosen at runtime (verified by NSerio against a live instance; not documented by Relativity).
- Access Token Lifetime: 60 minutes, stated in minutes because the field is in minutes. Relativity documentation: tokens for this flow must have a short lifetime, and the recommended value matches the Identity Server default.
- Allow refresh tokens: NSerio's recommendation: leave it off unless the application must keep a user session active beyond the access token lifetime without asking the user to sign in again. If it is on, the refresh tokens are stored as secrets, per Secret Management (Part B of the Migration Documentation).
- Enabled: on.
<!-- generated:end JS-012 authorization-code -->

### JS-013 · UrlUserinfoHeldLiteral

<a id="js-013-migration"></a>

Remove the user information from the held address, and send a Bearer header wherever the address is used. The tool did not see where the address is used, so it may be a `git` remote or an unused constant; a use elsewhere is not followed. The credential in the address is exposed whatever you do next: rotate it.

#### Client Credentials

<!-- generated:begin JS-013 client-credentials -->
**Confirming the flow.** When no signal in a file decides the flow, the tool assigns Client Credentials, the default, with this note: "Who supplies the credential at run time? If a person types it or signs in, the integration acts for that person: use Authorization Code. If it comes from configuration, settings, an environment variable or a secret store: use Client Credentials. No signal in this file showed which applies, so Client Credentials, the default, is recommended. Use Authorization Code instead if the integration acts on behalf of a signed-in user, or if your RelativityOne instance already has a Code-flow OAuth2 Client for this integration." A fixed-credential signal assigns it with this note: "A fixed-credential signal in this file points to Client Credentials, but how often that kind of signal is right has not been measured. Use Authorization Code instead if the integration acts on behalf of a signed-in user, or if your RelativityOne instance already has a Code-flow OAuth2 Client for this integration." See [Confirming the flow](#confirming-the-flow) and the "Which flow" paragraph of JS-013 in [Part A](part-a-javascript.md#js-013).

Source form: **Applies to:** browser and Node.js.

```javascript
const baseUrl = "https://exampleUser:exampleSecret@host.example/path";
await axios.get(baseUrl);
```

Under token authentication: **First, the destination:** if this Relativity instance is not RelativityOne — including a Relativity Server instance, whose own documentation governs — this finding is informational and the rest of this template does not apply.

```javascript
const CSRF_HEADER_VALUE = "-";
const baseUrl = "https://host.example/path";
await axios.get(baseUrl, { headers: { Authorization: `Bearer ${accessToken}`, "X-CSRF-Header": CSRF_HEADER_VALUE } });
```

Remove the userinfo, and send the token wherever the URL is used: `Authorization: Bearer ${accessToken}` with `X-CSRF-Header: -`.

Credentials: **Where the credentials come from.** The text establishes that a client sends Basic, not that the destination is RelativityOne. If it is not RelativityOne, the migration is that service's own token mechanism, this finding is informational for this tool, and the reader stops here. If it is RelativityOne, the token comes from the token-acquisition part that follows, and the user name and password are replaced by the OAuth2 Client's client ID and secret, kept as Secret Management (Part B of the Migration Documentation) describes, never in source, in a tracked `.env` file or in a bundle.

**Where the token comes from.** Create an OAuth2 Client with Flow Grant Type set to Client Credentials (part 3 lists its settings), then send a form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=client_credentials`, `client_id` and `client_secret`. The token endpoint is **documented** for this grant. The token is then sent as `Authorization: Bearer <token>`, with `X-CSRF-Header: -` on REST and Kepler calls. **The scope depends on what the integration needs, and no single scope is required.** Relativity's documentation shows `SystemUserInfo` and `UserInfoAccess` and no page says which is correct or what either grants; use the value the API you call documents, and confirm it. The text has two variants, by where the code runs, and each opens with the precondition it is for: read that sentence and discard the variant that does not apply.

**Variant: a Node.js process acting as itself**

**Precondition: this variant is for a Node.js job, service or script that does not act for a signed-in person.**

```javascript
const IDENTITY_TOKEN_URL = "https://HOST/Relativity/Identity/connect/token";
const EXPIRY_MARGIN_SECONDS = 60;
const MILLISECONDS_PER_SECOND = 1000;
let cachedToken = null;

async function getAccessToken() {
	const now = Date.now();
	if (cachedToken === null || cachedToken.expiresAtMs - now < EXPIRY_MARGIN_SECONDS * MILLISECONDS_PER_SECOND) {
		const response = await fetch(IDENTITY_TOKEN_URL, {
			method: "POST",
			headers: { "Content-Type": "application/x-www-form-urlencoded" },
			body: new URLSearchParams({
				grant_type: "client_credentials",
				client_id: process.env.RELATIVITY_CLIENT_ID,
				client_secret: process.env.RELATIVITY_CLIENT_SECRET,
			}),
		});
		const payload = await response.json();
		cachedToken = {
			value: payload.access_token,
			expiresAtMs: now + payload.expires_in * MILLISECONDS_PER_SECOND,
		};
	}
	return cachedToken.value;
}
```

**Cache the token and re-acquire it on expiry — do not refresh.** Client Credentials offers no refresh tokens: **verified by NSerio against a live instance; not documented by Relativity** — the Allow Refresh Tokens control does not appear for this grant in the RelativityOne user interface. The recommended Access Token Lifetime is listed in part 3. The block uses the built-in `fetch` and no package. **The client secret** belongs in the process environment or a secret store, as Secret Management (Part B of the Migration Documentation) describes, **never in source, in a tracked `.env` file or in a bundle**. A reset invalidates the previous secret immediately, so plan the cut-over before resetting it. **Before deploying, confirm the calling identity:** every call now runs as the OAuth2 Client's Context User, not as the user in the replaced credential, and a non-administrator Context User is NSerio's recommendation. Trusted IPs restrict where a token can be requested, not a token already issued.

**Variant: browser code that does not act for a signed-in user**

**Precondition: this variant is for browser code that calls RelativityOne with no person signed in to it, so no person's identity exists to preserve. If a person signs in, use Authorization Code, variant 1.**

The Relativity request moves behind a **server-side component** that holds the secret and performs the token exchange (the Node.js process variant above); the browser calls that component on its own origin. **The component must authenticate and authorize its own callers, and the OAuth2 Client's Context User must hold the minimum permissions the operation needs. Without both, the result is an unauthenticated relay into RelativityOne:** every caller reaching it acts with the Context User's full rights, which is strictly worse than the starting point. The relay returns results to the browser, **never the token** and never the secret. For browser code the credential has been public for as long as the code has shipped; the tool does not escalate it, so this text states the exposure and the rotation:

> Treat this credential as compromised. Rotate it, and assume the repository history has published it. Completing the migration does not close this exposure; rotation does.

**When the code acts for a signed-in user.** Client Credentials is one service identity by construction. Code that asks a person for a password and sends it is the Authorization Code case, and moving to it means a new OAuth2 Client, because the flow is fixed when a client is created. The finding's flow note says when to make that choice.

Verification: **Verification note.** **The tool read a URL with userinfo held in a declaration or a key; it did not see where the URL is used**: it may be a `git` remote or an unused constant, and a use elsewhere is not followed. **A token has a finite lifetime.**

> Treat this credential as compromised. Rotate it, and assume the repository history has published it. Completing the migration does not close this exposure; rotation does.

OAuth2 Client: Create or confirm this OAuth2 Client in RelativityOne (NSerio's observation: the menu path is Home > Authentication > OAuth2 Client Manager, as Relativity's REST API authentication page names it). This tool never connects to RelativityOne and never creates or changes the OAuth2 Client; the settings below are guidance.
- Name: a descriptive name identifying the integration it serves. Use one OAuth2 Client per integration and do not share it across integrations.
- Flow Grant Type: Client Credentials
- Context User: NSerio's recommendation: a dedicated integration user with the minimum permissions the integration needs, and not a personal account and not an administrator, because every call runs with that user's permissions and an administrator Context User gives the integration an administrator's reach. Relativity documentation (REST API authentication page): describes the Context User as usually a member of the Relativity Administrators group, though it could differ depending on the API or operation invoked; NSerio recommends against that default for an integration.
- Access Token Lifetime: 60 minutes, stated in minutes because the field is in minutes. Relativity documentation: tokens for this flow must have a short lifetime, and the recommended value matches the Identity Server default.
- Trusted IPs: Relativity documentation: the address list is checked whenever a token is requested with the OAuth2 Client's ID and secret. NSerio's recommendation: populate it with the integration's outbound IP addresses, one per line, wherever they are known and stable. It restricts where a token can be requested, checked when the token is issued; it does not restrict a token already issued, which travels in the request header and is valid from anywhere until it expires.
- Allow refresh tokens: not used by this flow. The integration requests a new access token when the current one expires.
- Enabled: on.
- Every call made with this OAuth2 Client runs with the Context User's permissions.
- Relativity documentation: the flow is fixed when the OAuth2 Client is created and cannot be changed when editing it, so moving this integration to Authorization Code later means creating a new OAuth2 Client.
<!-- generated:end JS-013 client-credentials -->

#### Authorization Code

<!-- generated:begin JS-013 authorization-code -->
**Confirming the flow.** An interactive-user signal in a file assigns Authorization Code, with this note: "An interactive-user signal in this file points to Authorization Code. Confirm the flow: use Client Credentials instead if the integration runs with credentials fixed for the integration and no signed-in user." Your user profile can also make Authorization Code the default; see Changing the default in [Confirming the flow](#confirming-the-flow). See [Confirming the flow](#confirming-the-flow) and the "Which flow" paragraph of JS-013 in [Part A](part-a-javascript.md#js-013).

Source form: **Applies to:** browser and Node.js.

```javascript
const baseUrl = "https://exampleUser:exampleSecret@host.example/path";
await axios.get(baseUrl);
```

Under token authentication: **First, the destination:** if this Relativity instance is not RelativityOne — including a Relativity Server instance, whose own documentation governs — this finding is informational and the rest of this template does not apply.

```javascript
const CSRF_HEADER_VALUE = "-";
const baseUrl = "https://host.example/path";
await axios.get(baseUrl, { headers: { Authorization: `Bearer ${accessToken}`, "X-CSRF-Header": CSRF_HEADER_VALUE } });
```

Remove the userinfo, and send the token wherever the URL is used: `Authorization: Bearer ${accessToken}` with `X-CSRF-Header: -`.

Credentials: **Where the credentials come from.** The text establishes that a client sends Basic, not that the destination is RelativityOne. If it is not RelativityOne, the migration is that service's own token mechanism, this finding is informational for this tool, and the reader stops here. If it is RelativityOne, the token comes from the token-acquisition part that follows, and the user name and password are replaced by the OAuth2 Client's client ID and secret, kept as Secret Management (Part B of the Migration Documentation) describes, never in source, in a tracked `.env` file or in a bundle.

**Common to all three variants.** The text has a common part and **three variants by application type**, each opening with the precondition it is for: a browser with a server-side component, a Node.js server, and a Node.js command-line tool or desktop shell. Read the precondition of each and discard the variants that do not apply.

**Sign in.** Replace the code that collected the person's credentials with a redirect to the authorize endpoint `<host>/Relativity/Identity/connect/authorize` (**verified by NSerio against a live instance; not documented by Relativity**), which leads to RelativityOne's login page. The request carries the standard authorization-code parameters — `client_id`, `response_type=code`, the registered `redirect_uri`, `state` checked on return, and `code_challenge` with `code_challenge_method=S256` when PKCE is used; these are standard OAuth and Relativity documents none of them.

**PKCE.** PKCE is required by Relativity's own documentation to obtain a refresh token and not otherwise: a Code request that does not ask for a refresh token works without it, **verified by NSerio against a live instance; not documented by Relativity**. NSerio recommends `S256` (NSerio recommendation) and does not claim that Relativity accepts any particular method. `state` is generated, stored against the session and compared on return; `nonce` is not used, and Relativity does not document whether it enforces either.

**The client secret is required.** Code does not work without a client secret: tested and rejected, **verified by NSerio against a live instance; not documented by Relativity**, which confirms Relativity's own statement. **Any secret shipped to a browser is public**, and bundling, minifying or obfuscating changes nothing, so the exchange runs where a secret can be kept.

**Exchange the code.** A form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=authorization_code`, the code, the same `redirect_uri`, the client ID and the secret, plus the `code_verifier` when PKCE is used (**verified by NSerio against a live instance; not documented by Relativity**).

**Redirect URL.** A server-side application registers **one exact HTTPS URL per environment, no wildcards**; the matching rules are not documented and no claim is made about them. A plain-HTTP loopback Redirect URL `http://127.0.0.1:<port>/<path>` on a **fixed registered port** is accepted, **verified by NSerio against a live instance; not documented by Relativity**, and is the desktop shape.

**On expiry.** With Allow Refresh Tokens enabled and PKCE used, the code holds a refresh token and sends `grant_type=refresh_token` in place of the code exchange: *inferred* from the standard OAuth shape, not tested. Relativity does not document a refresh token's lifetime, rotation or revocation, nor what disabling the client does to tokens already issued. **A refresh token is a secret.** **Single sign-on:** sign-in happens on RelativityOne's own page, so the instance's single sign-on and two-factor rules apply unchanged (*inferred*). **The code stops collecting the password:** after this change nothing receives, stores or forwards the person's user name and password, and the code holds only tokens. Every call now runs as the person who signed in, so **the calling identity changes**. **Scope:** no scope set is prescribed for Code; `openid RelativityRequestOrigin UserInfo UserInfoAccess AuthenticationID RelativityWeb` works, **verified by NSerio against a live instance; not documented by Relativity**, as working and not as minimal.

**Variant 1 — Browser with a server-side component**

**Precondition: this variant is for code that runs in a web browser and signs a person in to RelativityOne. If your code runs in Node.js, use variant 2 or variant 3.**

A browser cannot hold the secret the Code grant requires, so **the browser never calls RelativityOne**: it navigates to a route on **the application's own origin**, and a **server-side component** (variant 2's code) creates `state`, redirects to the authorize endpoint, receives the code at its registered HTTPS Redirect URL, exchanges it, keeps the tokens and gives the browser only its own session. **The browser holds a session identifier with that component and nothing from RelativityOne.** The authorize step is a **top-level navigation** (a full-window redirect, not a `fetch`).

```javascript
const LOGIN_ROUTE = "/auth/login";
const API_ROUTE = "/api/documents";

function signIn() {
	window.location.assign(LOGIN_ROUTE);
}

async function loadDocuments() {
	const response = await fetch(API_ROUTE, { credentials: "same-origin" });
	return response.json();
}
```

Both calls go to the page's own origin. `credentials: "include"` is a cookie policy and is not recommended here, because the architecture needs no cross-origin call. **Cross-origin access, both ways, both not verified:** NSerio has not run a preflight against a live instance. *If RelativityOne serves CORS to browser callers*, the legacy browser Basic call could have worked from another origin, and a browser holding a Bearer token could in principle call it directly; **this text still does not recommend that**, because the only grant that yields a token for a person needs the secret the browser cannot hold. *If it does not*, the legacy call worked only from the same origin or through a proxy the client already runs. **The recommended architecture holds under either answer.** **The component keeps the tokens on the server**, never in browser storage, which any script on the page can read (a browser behaviour, unchecked); the recommended session cookie is `HttpOnly`, `Secure` and `SameSite` (NSerio recommendation; unchecked).

**Variant 2 — Node.js server**

**Precondition: this variant is for code that runs in Node.js as a web server and acts for people who sign in to it. It is not for browser code (variant 1) or a command-line tool (variant 3).**

Standard server-side Code flow. A login route builds the authorize address with `state` (and the PKCE verifier where a refresh token is wanted), stores them against the visitor's session and redirects; RelativityOne redirects to the **callback route**, registered as an exact HTTPS URL; the callback checks `state` and exchanges the code with the secret. This is the component variant 1 relies on.

```javascript
import crypto from "node:crypto";

const IDENTITY_BASE = "https://HOST/Relativity/Identity/connect";
const REDIRECT_URI = "https://app.example.test/auth/callback";
const CLIENT_ID = process.env.RELATIVITY_CLIENT_ID;
const CLIENT_SECRET = process.env.RELATIVITY_CLIENT_SECRET;
const RANDOM_BYTES = 32;
const pendingBySession = new Map();

function base64Url(buffer) {
	return buffer.toString("base64url");
}

function beginLogin(sessionId, wantRefreshToken) {
	const state = base64Url(crypto.randomBytes(RANDOM_BYTES));
	const verifier = base64Url(crypto.randomBytes(RANDOM_BYTES));
	const challenge = base64Url(crypto.createHash("sha256").update(verifier).digest());
	pendingBySession.set(sessionId, { state, verifier });
	const query = new URLSearchParams({
		response_type: "code",
		client_id: CLIENT_ID,
		redirect_uri: REDIRECT_URI,
		state,
	});
	if (wantRefreshToken) {
		query.set("code_challenge", challenge);
		query.set("code_challenge_method", "S256");
	}
	return `${IDENTITY_BASE}/authorize?${query}`;
}

async function completeLogin(sessionId, returnedCode, returnedState) {
	const pending = pendingBySession.get(sessionId);
	pendingBySession.delete(sessionId);
	const stateMatches = pending !== undefined
		&& returnedState !== undefined
		&& Buffer.byteLength(returnedState) === Buffer.byteLength(pending.state)
		&& crypto.timingSafeEqual(Buffer.from(returnedState), Buffer.from(pending.state));
	let tokens = null;
	if (stateMatches) {
		const response = await fetch(`${IDENTITY_BASE}/token`, {
			method: "POST",
			headers: { "Content-Type": "application/x-www-form-urlencoded" },
			body: new URLSearchParams({
				grant_type: "authorization_code",
				code: returnedCode,
				redirect_uri: REDIRECT_URI,
				client_id: CLIENT_ID,
				client_secret: CLIENT_SECRET,
				code_verifier: pending.verifier,
			}),
		});
		tokens = response.ok ? await response.json() : null;
	}
	return tokens;
}
```

`state` is consumed once and compared on return; a request with no pending entry or a different `state` gets no exchange. **The scope is not prescribed** (the sketch sends none). **Where the secret travels in the token request** (the body, as sketched, or HTTP Basic client authentication) is **not verified**. The sketch does not show the HTTP server, the session cookie or a session store that survives more than one process. **Tokens stay on the server**, in a session store that is not exposed to a request.

**Variant 3 — Node.js command line or desktop**

**Precondition: this variant is for code that runs on a person's own machine, as a command-line tool or a desktop shell, and signs that person in. It is not for a web server (variant 2) or browser code (variant 1).**

The process starts a listener from the built-in `node:http` module, bound to **`127.0.0.1` only**, on the **fixed registered port**, for **one request**; opens the system browser on the authorize address **and always prints the address too**, so a machine with no browser, an SSH session or a failed launch has a manual route; reads `code` and `state` from the one request, closes the listener, checks `state`, and exchanges the code as variant 2's `completeLogin` does.

```javascript
import http from "node:http";

const LOOPBACK_HOST = "127.0.0.1";
const REGISTERED_PORT = 8123;
const CALLBACK_PATH = "/callback";
const BASE_FOR_PARSING = `http://${LOOPBACK_HOST}:${REGISTERED_PORT}`;
const WAIT_MILLISECONDS = 120000;
const STATUS_OK = 200;
const STATUS_NOT_FOUND = 404;

function waitForRedirect() {
	return new Promise((resolve, reject) => {
		const server = http.createServer((request, response) => {
			const address = new URL(request.url, BASE_FOR_PARSING);
			const isCallback = address.pathname === CALLBACK_PATH;
			response.statusCode = isCallback ? STATUS_OK : STATUS_NOT_FOUND;
			response.end(isCallback ? "You can close this window." : "Not found");
			if (isCallback) {
				server.close();
				resolve({
					code: address.searchParams.get("code"),
					state: address.searchParams.get("state"),
				});
			}
		});
		server.on("error", reject);
		server.listen(REGISTERED_PORT, LOOPBACK_HOST);
		setTimeout(() => {
			server.close();
			reject(new Error("No redirect arrived"));
		}, WAIT_MILLISECONDS).unref();
	});
}
```

**Rules for the listener:** bind to loopback only; accept one request; **check `state`** (another local process can connect to a fixed, advertised port); close the listener after the request; and handle a busy port (a second server on a port in use raises `EADDRINUSE`, so fall back to the printed address). **Opening the system browser:** Node.js core has no function for it; run the platform's own launcher with a fixed program and an argument array, never a shell string built from the address (unchecked on every system). **Where tokens are kept:** a file with restricted permissions or the operating system's keychain; on Windows the POSIX mode argument of Node.js did not restrict a file when it was tried (*checked by NSerio locally* on Node.js v24.11.1), so a restricted file there needs an access-control change by other means, and on Linux and macOS it is unchecked. **The client secret of a distributed tool is extractable:** a command-line tool or desktop shell shipped to people carries its client secret in a form anyone who holds the distribution can extract, and it protects nothing against the person running it. **A known issue:** Relativity documents no recommendation for a distributed client; this design is what NSerio verified works. **Compensating guidance** (NSerio recommendation, subject to your own security policy): Trusted IPs where the users' outbound addresses are known and stable (checked at token issuance; applicability to Code *inferred*); the recommended short Access Token Lifetime; refresh tokens only when needed; and **one OAuth2 Client per distribution**, so a leaked secret can be disabled and reset alone.

Verification: **Verification note.** **The tool read a URL with userinfo held in a declaration or a key; it did not see where the URL is used**: it may be a `git` remote or an unused constant, and a use elsewhere is not followed. **A token has a finite lifetime.**

> Treat this credential as compromised. Rotate it, and assume the repository history has published it. Completing the migration does not close this exposure; rotation does.

OAuth2 Client: Create or confirm this OAuth2 Client in RelativityOne (NSerio's observation: the menu path is Home > Authentication > OAuth2 Client Manager, as Relativity's REST API authentication page names it). This tool never connects to RelativityOne and never creates or changes the OAuth2 Client; the settings below are guidance.
- Name: a descriptive name identifying the application it serves.
- Flow Grant Type: Code
- Redirect URLs: Relativity documentation: each URL includes the http or https protocol prefix. The exact callback URL is registered by the Relativity administrator. Server-side web applications, NSerio's recommendation: one exact HTTPS URL per environment, with no wildcards. Desktop and CLI applications: a plain-HTTP loopback URL, `http://127.0.0.1:<port>/<path>`, on a fixed port because the URL is registered in advance and the port cannot be chosen at runtime (verified by NSerio against a live instance; not documented by Relativity).
- Access Token Lifetime: 60 minutes, stated in minutes because the field is in minutes. Relativity documentation: tokens for this flow must have a short lifetime, and the recommended value matches the Identity Server default.
- Allow refresh tokens: NSerio's recommendation: leave it off unless the application must keep a user session active beyond the access token lifetime without asking the user to sign in again. If it is on, the refresh tokens are stored as secrets, per Secret Management (Part B of the Migration Documentation).
- Enabled: on.
<!-- generated:end JS-013 authorization-code -->

### JS-015 · GotCredentialOptions

<a id="js-015-migration"></a>

Remove the `username` and `password` options of the `got` call and send a Bearer header in its `headers` option.

#### Client Credentials

<!-- generated:begin JS-015 client-credentials -->
**Confirming the flow.** When no signal in a file decides the flow, the tool assigns Client Credentials, the default, with this note: "Who supplies the credential at run time? If a person types it or signs in, the integration acts for that person: use Authorization Code. If it comes from configuration, settings, an environment variable or a secret store: use Client Credentials. No signal in this file showed which applies, so Client Credentials, the default, is recommended. Use Authorization Code instead if the integration acts on behalf of a signed-in user, or if your RelativityOne instance already has a Code-flow OAuth2 Client for this integration." A fixed-credential signal assigns it with this note: "A fixed-credential signal in this file points to Client Credentials, but how often that kind of signal is right has not been measured. Use Authorization Code instead if the integration acts on behalf of a signed-in user, or if your RelativityOne instance already has a Code-flow OAuth2 Client for this integration." See [Confirming the flow](#confirming-the-flow) and the "Which flow" paragraph of JS-015 in [Part A](part-a-javascript.md#js-015).

Source form: **Applies to:** Node.js; `got`.

```javascript
await got(url, { username: userName, password: secret });
```

Under token authentication: **First, the destination:** if this Relativity instance is not RelativityOne — including a Relativity Server instance, whose own documentation governs — this finding is informational and the rest of this template does not apply.

```javascript
const CSRF_HEADER_VALUE = "-";
await got(url, { headers: { Authorization: `Bearer ${accessToken}`, "X-CSRF-Header": CSRF_HEADER_VALUE } });
```

Remove the `username` and `password` options.

Credentials: **Where the credentials come from.** The text establishes that a client sends Basic, not that the destination is RelativityOne. If it is not RelativityOne, the migration is that service's own token mechanism, this finding is informational for this tool, and the reader stops here. If it is RelativityOne, the token comes from the token-acquisition part that follows, and the user name and password are replaced by the OAuth2 Client's client ID and secret, kept as Secret Management (Part B of the Migration Documentation) describes, never in source, in a tracked `.env` file or in a bundle.

**Where the token comes from.** Create an OAuth2 Client with Flow Grant Type set to Client Credentials (part 3 lists its settings), then send a form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=client_credentials`, `client_id` and `client_secret`. The token endpoint is **documented** for this grant. The token is then sent as `Authorization: Bearer <token>`, with `X-CSRF-Header: -` on REST and Kepler calls. **The scope depends on what the integration needs, and no single scope is required.** Relativity's documentation shows `SystemUserInfo` and `UserInfoAccess` and no page says which is correct or what either grants; use the value the API you call documents, and confirm it. The text has two variants, by where the code runs, and each opens with the precondition it is for: read that sentence and discard the variant that does not apply.

**Variant: a Node.js process acting as itself**

**Precondition: this variant is for a Node.js job, service or script that does not act for a signed-in person.**

```javascript
const IDENTITY_TOKEN_URL = "https://HOST/Relativity/Identity/connect/token";
const EXPIRY_MARGIN_SECONDS = 60;
const MILLISECONDS_PER_SECOND = 1000;
let cachedToken = null;

async function getAccessToken() {
	const now = Date.now();
	if (cachedToken === null || cachedToken.expiresAtMs - now < EXPIRY_MARGIN_SECONDS * MILLISECONDS_PER_SECOND) {
		const response = await fetch(IDENTITY_TOKEN_URL, {
			method: "POST",
			headers: { "Content-Type": "application/x-www-form-urlencoded" },
			body: new URLSearchParams({
				grant_type: "client_credentials",
				client_id: process.env.RELATIVITY_CLIENT_ID,
				client_secret: process.env.RELATIVITY_CLIENT_SECRET,
			}),
		});
		const payload = await response.json();
		cachedToken = {
			value: payload.access_token,
			expiresAtMs: now + payload.expires_in * MILLISECONDS_PER_SECOND,
		};
	}
	return cachedToken.value;
}
```

**Cache the token and re-acquire it on expiry — do not refresh.** Client Credentials offers no refresh tokens: **verified by NSerio against a live instance; not documented by Relativity** — the Allow Refresh Tokens control does not appear for this grant in the RelativityOne user interface. The recommended Access Token Lifetime is listed in part 3. The block uses the built-in `fetch` and no package. **The client secret** belongs in the process environment or a secret store, as Secret Management (Part B of the Migration Documentation) describes, **never in source, in a tracked `.env` file or in a bundle**. A reset invalidates the previous secret immediately, so plan the cut-over before resetting it. **Before deploying, confirm the calling identity:** every call now runs as the OAuth2 Client's Context User, not as the user in the replaced credential, and a non-administrator Context User is NSerio's recommendation. Trusted IPs restrict where a token can be requested, not a token already issued.

**Variant: browser code that does not act for a signed-in user**

**Precondition: this variant is for browser code that calls RelativityOne with no person signed in to it, so no person's identity exists to preserve. If a person signs in, use Authorization Code, variant 1.**

The Relativity request moves behind a **server-side component** that holds the secret and performs the token exchange (the Node.js process variant above); the browser calls that component on its own origin. **The component must authenticate and authorize its own callers, and the OAuth2 Client's Context User must hold the minimum permissions the operation needs. Without both, the result is an unauthenticated relay into RelativityOne:** every caller reaching it acts with the Context User's full rights, which is strictly worse than the starting point. The relay returns results to the browser, **never the token** and never the secret. For browser code the credential has been public for as long as the code has shipped; the tool does not escalate it, so this text states the exposure and the rotation:

> Treat this credential as compromised. Rotate it, and assume the repository history has published it. Completing the migration does not close this exposure; rotation does.

**When the code acts for a signed-in user.** Client Credentials is one service identity by construction. Code that asks a person for a password and sends it is the Authorization Code case, and moving to it means a new OAuth2 Client, because the flow is fixed when a client is created. The finding's flow note says when to make that choice.

Verification: **Verification note.** **A token has a finite lifetime.** **The calling identity changes.** The two options are documented for the package version read; earlier major versions are unchecked, and whether `got` sends Basic on the first request is not stated in the page read.

OAuth2 Client: Create or confirm this OAuth2 Client in RelativityOne (NSerio's observation: the menu path is Home > Authentication > OAuth2 Client Manager, as Relativity's REST API authentication page names it). This tool never connects to RelativityOne and never creates or changes the OAuth2 Client; the settings below are guidance.
- Name: a descriptive name identifying the integration it serves. Use one OAuth2 Client per integration and do not share it across integrations.
- Flow Grant Type: Client Credentials
- Context User: NSerio's recommendation: a dedicated integration user with the minimum permissions the integration needs, and not a personal account and not an administrator, because every call runs with that user's permissions and an administrator Context User gives the integration an administrator's reach. Relativity documentation (REST API authentication page): describes the Context User as usually a member of the Relativity Administrators group, though it could differ depending on the API or operation invoked; NSerio recommends against that default for an integration.
- Access Token Lifetime: 60 minutes, stated in minutes because the field is in minutes. Relativity documentation: tokens for this flow must have a short lifetime, and the recommended value matches the Identity Server default.
- Trusted IPs: Relativity documentation: the address list is checked whenever a token is requested with the OAuth2 Client's ID and secret. NSerio's recommendation: populate it with the integration's outbound IP addresses, one per line, wherever they are known and stable. It restricts where a token can be requested, checked when the token is issued; it does not restrict a token already issued, which travels in the request header and is valid from anywhere until it expires.
- Allow refresh tokens: not used by this flow. The integration requests a new access token when the current one expires.
- Enabled: on.
- Every call made with this OAuth2 Client runs with the Context User's permissions.
- Relativity documentation: the flow is fixed when the OAuth2 Client is created and cannot be changed when editing it, so moving this integration to Authorization Code later means creating a new OAuth2 Client.
<!-- generated:end JS-015 client-credentials -->

#### Authorization Code

<!-- generated:begin JS-015 authorization-code -->
**Confirming the flow.** An interactive-user signal in a file assigns Authorization Code, with this note: "An interactive-user signal in this file points to Authorization Code. Confirm the flow: use Client Credentials instead if the integration runs with credentials fixed for the integration and no signed-in user." Your user profile can also make Authorization Code the default; see Changing the default in [Confirming the flow](#confirming-the-flow). See [Confirming the flow](#confirming-the-flow) and the "Which flow" paragraph of JS-015 in [Part A](part-a-javascript.md#js-015).

Source form: **Applies to:** Node.js; `got`.

```javascript
await got(url, { username: userName, password: secret });
```

Under token authentication: **First, the destination:** if this Relativity instance is not RelativityOne — including a Relativity Server instance, whose own documentation governs — this finding is informational and the rest of this template does not apply.

```javascript
const CSRF_HEADER_VALUE = "-";
await got(url, { headers: { Authorization: `Bearer ${accessToken}`, "X-CSRF-Header": CSRF_HEADER_VALUE } });
```

Remove the `username` and `password` options.

Credentials: **Where the credentials come from.** The text establishes that a client sends Basic, not that the destination is RelativityOne. If it is not RelativityOne, the migration is that service's own token mechanism, this finding is informational for this tool, and the reader stops here. If it is RelativityOne, the token comes from the token-acquisition part that follows, and the user name and password are replaced by the OAuth2 Client's client ID and secret, kept as Secret Management (Part B of the Migration Documentation) describes, never in source, in a tracked `.env` file or in a bundle.

**Common to all three variants.** The text has a common part and **three variants by application type**, each opening with the precondition it is for: a browser with a server-side component, a Node.js server, and a Node.js command-line tool or desktop shell. Read the precondition of each and discard the variants that do not apply.

**Sign in.** Replace the code that collected the person's credentials with a redirect to the authorize endpoint `<host>/Relativity/Identity/connect/authorize` (**verified by NSerio against a live instance; not documented by Relativity**), which leads to RelativityOne's login page. The request carries the standard authorization-code parameters — `client_id`, `response_type=code`, the registered `redirect_uri`, `state` checked on return, and `code_challenge` with `code_challenge_method=S256` when PKCE is used; these are standard OAuth and Relativity documents none of them.

**PKCE.** PKCE is required by Relativity's own documentation to obtain a refresh token and not otherwise: a Code request that does not ask for a refresh token works without it, **verified by NSerio against a live instance; not documented by Relativity**. NSerio recommends `S256` (NSerio recommendation) and does not claim that Relativity accepts any particular method. `state` is generated, stored against the session and compared on return; `nonce` is not used, and Relativity does not document whether it enforces either.

**The client secret is required.** Code does not work without a client secret: tested and rejected, **verified by NSerio against a live instance; not documented by Relativity**, which confirms Relativity's own statement. **Any secret shipped to a browser is public**, and bundling, minifying or obfuscating changes nothing, so the exchange runs where a secret can be kept.

**Exchange the code.** A form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=authorization_code`, the code, the same `redirect_uri`, the client ID and the secret, plus the `code_verifier` when PKCE is used (**verified by NSerio against a live instance; not documented by Relativity**).

**Redirect URL.** A server-side application registers **one exact HTTPS URL per environment, no wildcards**; the matching rules are not documented and no claim is made about them. A plain-HTTP loopback Redirect URL `http://127.0.0.1:<port>/<path>` on a **fixed registered port** is accepted, **verified by NSerio against a live instance; not documented by Relativity**, and is the desktop shape.

**On expiry.** With Allow Refresh Tokens enabled and PKCE used, the code holds a refresh token and sends `grant_type=refresh_token` in place of the code exchange: *inferred* from the standard OAuth shape, not tested. Relativity does not document a refresh token's lifetime, rotation or revocation, nor what disabling the client does to tokens already issued. **A refresh token is a secret.** **Single sign-on:** sign-in happens on RelativityOne's own page, so the instance's single sign-on and two-factor rules apply unchanged (*inferred*). **The code stops collecting the password:** after this change nothing receives, stores or forwards the person's user name and password, and the code holds only tokens. Every call now runs as the person who signed in, so **the calling identity changes**. **Scope:** no scope set is prescribed for Code; `openid RelativityRequestOrigin UserInfo UserInfoAccess AuthenticationID RelativityWeb` works, **verified by NSerio against a live instance; not documented by Relativity**, as working and not as minimal.

**Variant 1 — Browser with a server-side component**

**Precondition: this variant is for code that runs in a web browser and signs a person in to RelativityOne. If your code runs in Node.js, use variant 2 or variant 3.**

A browser cannot hold the secret the Code grant requires, so **the browser never calls RelativityOne**: it navigates to a route on **the application's own origin**, and a **server-side component** (variant 2's code) creates `state`, redirects to the authorize endpoint, receives the code at its registered HTTPS Redirect URL, exchanges it, keeps the tokens and gives the browser only its own session. **The browser holds a session identifier with that component and nothing from RelativityOne.** The authorize step is a **top-level navigation** (a full-window redirect, not a `fetch`).

```javascript
const LOGIN_ROUTE = "/auth/login";
const API_ROUTE = "/api/documents";

function signIn() {
	window.location.assign(LOGIN_ROUTE);
}

async function loadDocuments() {
	const response = await fetch(API_ROUTE, { credentials: "same-origin" });
	return response.json();
}
```

Both calls go to the page's own origin. `credentials: "include"` is a cookie policy and is not recommended here, because the architecture needs no cross-origin call. **Cross-origin access, both ways, both not verified:** NSerio has not run a preflight against a live instance. *If RelativityOne serves CORS to browser callers*, the legacy browser Basic call could have worked from another origin, and a browser holding a Bearer token could in principle call it directly; **this text still does not recommend that**, because the only grant that yields a token for a person needs the secret the browser cannot hold. *If it does not*, the legacy call worked only from the same origin or through a proxy the client already runs. **The recommended architecture holds under either answer.** **The component keeps the tokens on the server**, never in browser storage, which any script on the page can read (a browser behaviour, unchecked); the recommended session cookie is `HttpOnly`, `Secure` and `SameSite` (NSerio recommendation; unchecked).

**Variant 2 — Node.js server**

**Precondition: this variant is for code that runs in Node.js as a web server and acts for people who sign in to it. It is not for browser code (variant 1) or a command-line tool (variant 3).**

Standard server-side Code flow. A login route builds the authorize address with `state` (and the PKCE verifier where a refresh token is wanted), stores them against the visitor's session and redirects; RelativityOne redirects to the **callback route**, registered as an exact HTTPS URL; the callback checks `state` and exchanges the code with the secret. This is the component variant 1 relies on.

```javascript
import crypto from "node:crypto";

const IDENTITY_BASE = "https://HOST/Relativity/Identity/connect";
const REDIRECT_URI = "https://app.example.test/auth/callback";
const CLIENT_ID = process.env.RELATIVITY_CLIENT_ID;
const CLIENT_SECRET = process.env.RELATIVITY_CLIENT_SECRET;
const RANDOM_BYTES = 32;
const pendingBySession = new Map();

function base64Url(buffer) {
	return buffer.toString("base64url");
}

function beginLogin(sessionId, wantRefreshToken) {
	const state = base64Url(crypto.randomBytes(RANDOM_BYTES));
	const verifier = base64Url(crypto.randomBytes(RANDOM_BYTES));
	const challenge = base64Url(crypto.createHash("sha256").update(verifier).digest());
	pendingBySession.set(sessionId, { state, verifier });
	const query = new URLSearchParams({
		response_type: "code",
		client_id: CLIENT_ID,
		redirect_uri: REDIRECT_URI,
		state,
	});
	if (wantRefreshToken) {
		query.set("code_challenge", challenge);
		query.set("code_challenge_method", "S256");
	}
	return `${IDENTITY_BASE}/authorize?${query}`;
}

async function completeLogin(sessionId, returnedCode, returnedState) {
	const pending = pendingBySession.get(sessionId);
	pendingBySession.delete(sessionId);
	const stateMatches = pending !== undefined
		&& returnedState !== undefined
		&& Buffer.byteLength(returnedState) === Buffer.byteLength(pending.state)
		&& crypto.timingSafeEqual(Buffer.from(returnedState), Buffer.from(pending.state));
	let tokens = null;
	if (stateMatches) {
		const response = await fetch(`${IDENTITY_BASE}/token`, {
			method: "POST",
			headers: { "Content-Type": "application/x-www-form-urlencoded" },
			body: new URLSearchParams({
				grant_type: "authorization_code",
				code: returnedCode,
				redirect_uri: REDIRECT_URI,
				client_id: CLIENT_ID,
				client_secret: CLIENT_SECRET,
				code_verifier: pending.verifier,
			}),
		});
		tokens = response.ok ? await response.json() : null;
	}
	return tokens;
}
```

`state` is consumed once and compared on return; a request with no pending entry or a different `state` gets no exchange. **The scope is not prescribed** (the sketch sends none). **Where the secret travels in the token request** (the body, as sketched, or HTTP Basic client authentication) is **not verified**. The sketch does not show the HTTP server, the session cookie or a session store that survives more than one process. **Tokens stay on the server**, in a session store that is not exposed to a request.

**Variant 3 — Node.js command line or desktop**

**Precondition: this variant is for code that runs on a person's own machine, as a command-line tool or a desktop shell, and signs that person in. It is not for a web server (variant 2) or browser code (variant 1).**

The process starts a listener from the built-in `node:http` module, bound to **`127.0.0.1` only**, on the **fixed registered port**, for **one request**; opens the system browser on the authorize address **and always prints the address too**, so a machine with no browser, an SSH session or a failed launch has a manual route; reads `code` and `state` from the one request, closes the listener, checks `state`, and exchanges the code as variant 2's `completeLogin` does.

```javascript
import http from "node:http";

const LOOPBACK_HOST = "127.0.0.1";
const REGISTERED_PORT = 8123;
const CALLBACK_PATH = "/callback";
const BASE_FOR_PARSING = `http://${LOOPBACK_HOST}:${REGISTERED_PORT}`;
const WAIT_MILLISECONDS = 120000;
const STATUS_OK = 200;
const STATUS_NOT_FOUND = 404;

function waitForRedirect() {
	return new Promise((resolve, reject) => {
		const server = http.createServer((request, response) => {
			const address = new URL(request.url, BASE_FOR_PARSING);
			const isCallback = address.pathname === CALLBACK_PATH;
			response.statusCode = isCallback ? STATUS_OK : STATUS_NOT_FOUND;
			response.end(isCallback ? "You can close this window." : "Not found");
			if (isCallback) {
				server.close();
				resolve({
					code: address.searchParams.get("code"),
					state: address.searchParams.get("state"),
				});
			}
		});
		server.on("error", reject);
		server.listen(REGISTERED_PORT, LOOPBACK_HOST);
		setTimeout(() => {
			server.close();
			reject(new Error("No redirect arrived"));
		}, WAIT_MILLISECONDS).unref();
	});
}
```

**Rules for the listener:** bind to loopback only; accept one request; **check `state`** (another local process can connect to a fixed, advertised port); close the listener after the request; and handle a busy port (a second server on a port in use raises `EADDRINUSE`, so fall back to the printed address). **Opening the system browser:** Node.js core has no function for it; run the platform's own launcher with a fixed program and an argument array, never a shell string built from the address (unchecked on every system). **Where tokens are kept:** a file with restricted permissions or the operating system's keychain; on Windows the POSIX mode argument of Node.js did not restrict a file when it was tried (*checked by NSerio locally* on Node.js v24.11.1), so a restricted file there needs an access-control change by other means, and on Linux and macOS it is unchecked. **The client secret of a distributed tool is extractable:** a command-line tool or desktop shell shipped to people carries its client secret in a form anyone who holds the distribution can extract, and it protects nothing against the person running it. **A known issue:** Relativity documents no recommendation for a distributed client; this design is what NSerio verified works. **Compensating guidance** (NSerio recommendation, subject to your own security policy): Trusted IPs where the users' outbound addresses are known and stable (checked at token issuance; applicability to Code *inferred*); the recommended short Access Token Lifetime; refresh tokens only when needed; and **one OAuth2 Client per distribution**, so a leaked secret can be disabled and reset alone.

Verification: **Verification note.** **A token has a finite lifetime.** **The calling identity changes.** The two options are documented for the package version read; earlier major versions are unchecked, and whether `got` sends Basic on the first request is not stated in the page read.

OAuth2 Client: Create or confirm this OAuth2 Client in RelativityOne (NSerio's observation: the menu path is Home > Authentication > OAuth2 Client Manager, as Relativity's REST API authentication page names it). This tool never connects to RelativityOne and never creates or changes the OAuth2 Client; the settings below are guidance.
- Name: a descriptive name identifying the application it serves.
- Flow Grant Type: Code
- Redirect URLs: Relativity documentation: each URL includes the http or https protocol prefix. The exact callback URL is registered by the Relativity administrator. Server-side web applications, NSerio's recommendation: one exact HTTPS URL per environment, with no wildcards. Desktop and CLI applications: a plain-HTTP loopback URL, `http://127.0.0.1:<port>/<path>`, on a fixed port because the URL is registered in advance and the port cannot be chosen at runtime (verified by NSerio against a live instance; not documented by Relativity).
- Access Token Lifetime: 60 minutes, stated in minutes because the field is in minutes. Relativity documentation: tokens for this flow must have a short lifetime, and the recommended value matches the Identity Server default.
- Allow refresh tokens: NSerio's recommendation: leave it off unless the application must keep a user session active beyond the access token lifetime without asking the user to sign in again. If it is on, the refresh tokens are stored as secrets, per Secret Management (Part B of the Migration Documentation).
- Enabled: on.
<!-- generated:end JS-015 authorization-code -->

### JS-017 · AuthorizationHeaderValueHeldInVariable

<a id="js-017-migration"></a>

Remove the variable that holds the `Basic` value and the code that built the pair, and send a Bearer header. A header whose variable the tool cannot resolve is not read, so a precomputed token built by a call, taken from a parameter or declared in another module has to be found by hand.

#### Client Credentials

<!-- generated:begin JS-017 client-credentials -->
**Confirming the flow.** When no signal in a file decides the flow, the tool assigns Client Credentials, the default, with this note: "Who supplies the credential at run time? If a person types it or signs in, the integration acts for that person: use Authorization Code. If it comes from configuration, settings, an environment variable or a secret store: use Client Credentials. No signal in this file showed which applies, so Client Credentials, the default, is recommended. Use Authorization Code instead if the integration acts on behalf of a signed-in user, or if your RelativityOne instance already has a Code-flow OAuth2 Client for this integration." A fixed-credential signal assigns it with this note: "A fixed-credential signal in this file points to Client Credentials, but how often that kind of signal is right has not been measured. Use Authorization Code instead if the integration acts on behalf of a signed-in user, or if your RelativityOne instance already has a Code-flow OAuth2 Client for this integration." See [Confirming the flow](#confirming-the-flow) and the "Which flow" paragraph of JS-017 in [Part A](part-a-javascript.md#js-017).

Source form: **Applies to:** browser and Node.js.

```javascript
const encodedHeader = "Basic " + encodedPair;
await fetch(url, { headers: { Authorization: encodedHeader } });
```

Under token authentication: **First, the destination:** if this Relativity instance is not RelativityOne — including a Relativity Server instance, whose own documentation governs — this finding is informational and the rest of this template does not apply.

```javascript
const CSRF_HEADER_VALUE = "-";
await fetch(url, { headers: { Authorization: `Bearer ${accessToken}`, "X-CSRF-Header": CSRF_HEADER_VALUE } });
```

Remove the variable and the code that built the pair. In a browser the call moves behind the server-side component.

Credentials: **Where the credentials come from.** The text establishes that a client sends Basic, not that the destination is RelativityOne. If it is not RelativityOne, the migration is that service's own token mechanism, this finding is informational for this tool, and the reader stops here. If it is RelativityOne, the token comes from the token-acquisition part that follows, and the user name and password are replaced by the OAuth2 Client's client ID and secret, kept as Secret Management (Part B of the Migration Documentation) describes, never in source, in a tracked `.env` file or in a bundle.

**Where the token comes from.** Create an OAuth2 Client with Flow Grant Type set to Client Credentials (part 3 lists its settings), then send a form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=client_credentials`, `client_id` and `client_secret`. The token endpoint is **documented** for this grant. The token is then sent as `Authorization: Bearer <token>`, with `X-CSRF-Header: -` on REST and Kepler calls. **The scope depends on what the integration needs, and no single scope is required.** Relativity's documentation shows `SystemUserInfo` and `UserInfoAccess` and no page says which is correct or what either grants; use the value the API you call documents, and confirm it. The text has two variants, by where the code runs, and each opens with the precondition it is for: read that sentence and discard the variant that does not apply.

**Variant: a Node.js process acting as itself**

**Precondition: this variant is for a Node.js job, service or script that does not act for a signed-in person.**

```javascript
const IDENTITY_TOKEN_URL = "https://HOST/Relativity/Identity/connect/token";
const EXPIRY_MARGIN_SECONDS = 60;
const MILLISECONDS_PER_SECOND = 1000;
let cachedToken = null;

async function getAccessToken() {
	const now = Date.now();
	if (cachedToken === null || cachedToken.expiresAtMs - now < EXPIRY_MARGIN_SECONDS * MILLISECONDS_PER_SECOND) {
		const response = await fetch(IDENTITY_TOKEN_URL, {
			method: "POST",
			headers: { "Content-Type": "application/x-www-form-urlencoded" },
			body: new URLSearchParams({
				grant_type: "client_credentials",
				client_id: process.env.RELATIVITY_CLIENT_ID,
				client_secret: process.env.RELATIVITY_CLIENT_SECRET,
			}),
		});
		const payload = await response.json();
		cachedToken = {
			value: payload.access_token,
			expiresAtMs: now + payload.expires_in * MILLISECONDS_PER_SECOND,
		};
	}
	return cachedToken.value;
}
```

**Cache the token and re-acquire it on expiry — do not refresh.** Client Credentials offers no refresh tokens: **verified by NSerio against a live instance; not documented by Relativity** — the Allow Refresh Tokens control does not appear for this grant in the RelativityOne user interface. The recommended Access Token Lifetime is listed in part 3. The block uses the built-in `fetch` and no package. **The client secret** belongs in the process environment or a secret store, as Secret Management (Part B of the Migration Documentation) describes, **never in source, in a tracked `.env` file or in a bundle**. A reset invalidates the previous secret immediately, so plan the cut-over before resetting it. **Before deploying, confirm the calling identity:** every call now runs as the OAuth2 Client's Context User, not as the user in the replaced credential, and a non-administrator Context User is NSerio's recommendation. Trusted IPs restrict where a token can be requested, not a token already issued.

**Variant: browser code that does not act for a signed-in user**

**Precondition: this variant is for browser code that calls RelativityOne with no person signed in to it, so no person's identity exists to preserve. If a person signs in, use Authorization Code, variant 1.**

The Relativity request moves behind a **server-side component** that holds the secret and performs the token exchange (the Node.js process variant above); the browser calls that component on its own origin. **The component must authenticate and authorize its own callers, and the OAuth2 Client's Context User must hold the minimum permissions the operation needs. Without both, the result is an unauthenticated relay into RelativityOne:** every caller reaching it acts with the Context User's full rights, which is strictly worse than the starting point. The relay returns results to the browser, **never the token** and never the secret. For browser code the credential has been public for as long as the code has shipped; the tool does not escalate it, so this text states the exposure and the rotation:

> Treat this credential as compromised. Rotate it, and assume the repository history has published it. Completing the migration does not close this exposure; rotation does.

**When the code acts for a signed-in user.** Client Credentials is one service identity by construction. Code that asks a person for a password and sends it is the Authorization Code case, and moving to it means a new OAuth2 Client, because the flow is fixed when a client is created. The finding's flow note says when to make that choice.

Verification: **Verification note.** **The tool resolved the header's variable to a scheme of `Basic` in this file** (one declaration, before the use); a variable it cannot resolve is not read. **A token has a finite lifetime.**

**If this code runs in a browser, the credential has been public for as long as the code has shipped.** The exposure and the rotation are stated here and the tool does not escalate it:

> Treat this credential as compromised. Rotate it, and assume the repository history has published it. Completing the migration does not close this exposure; rotation does.

OAuth2 Client: Create or confirm this OAuth2 Client in RelativityOne (NSerio's observation: the menu path is Home > Authentication > OAuth2 Client Manager, as Relativity's REST API authentication page names it). This tool never connects to RelativityOne and never creates or changes the OAuth2 Client; the settings below are guidance.
- Name: a descriptive name identifying the integration it serves. Use one OAuth2 Client per integration and do not share it across integrations.
- Flow Grant Type: Client Credentials
- Context User: NSerio's recommendation: a dedicated integration user with the minimum permissions the integration needs, and not a personal account and not an administrator, because every call runs with that user's permissions and an administrator Context User gives the integration an administrator's reach. Relativity documentation (REST API authentication page): describes the Context User as usually a member of the Relativity Administrators group, though it could differ depending on the API or operation invoked; NSerio recommends against that default for an integration.
- Access Token Lifetime: 60 minutes, stated in minutes because the field is in minutes. Relativity documentation: tokens for this flow must have a short lifetime, and the recommended value matches the Identity Server default.
- Trusted IPs: Relativity documentation: the address list is checked whenever a token is requested with the OAuth2 Client's ID and secret. NSerio's recommendation: populate it with the integration's outbound IP addresses, one per line, wherever they are known and stable. It restricts where a token can be requested, checked when the token is issued; it does not restrict a token already issued, which travels in the request header and is valid from anywhere until it expires.
- Allow refresh tokens: not used by this flow. The integration requests a new access token when the current one expires.
- Enabled: on.
- Every call made with this OAuth2 Client runs with the Context User's permissions.
- Relativity documentation: the flow is fixed when the OAuth2 Client is created and cannot be changed when editing it, so moving this integration to Authorization Code later means creating a new OAuth2 Client.
<!-- generated:end JS-017 client-credentials -->

#### Authorization Code

<!-- generated:begin JS-017 authorization-code -->
**Confirming the flow.** An interactive-user signal in a file assigns Authorization Code, with this note: "An interactive-user signal in this file points to Authorization Code. Confirm the flow: use Client Credentials instead if the integration runs with credentials fixed for the integration and no signed-in user." Your user profile can also make Authorization Code the default; see Changing the default in [Confirming the flow](#confirming-the-flow). See [Confirming the flow](#confirming-the-flow) and the "Which flow" paragraph of JS-017 in [Part A](part-a-javascript.md#js-017).

Source form: **Applies to:** browser and Node.js.

```javascript
const encodedHeader = "Basic " + encodedPair;
await fetch(url, { headers: { Authorization: encodedHeader } });
```

Under token authentication: **First, the destination:** if this Relativity instance is not RelativityOne — including a Relativity Server instance, whose own documentation governs — this finding is informational and the rest of this template does not apply.

```javascript
const CSRF_HEADER_VALUE = "-";
await fetch(url, { headers: { Authorization: `Bearer ${accessToken}`, "X-CSRF-Header": CSRF_HEADER_VALUE } });
```

Remove the variable and the code that built the pair. In a browser the call moves behind the server-side component.

Credentials: **Where the credentials come from.** The text establishes that a client sends Basic, not that the destination is RelativityOne. If it is not RelativityOne, the migration is that service's own token mechanism, this finding is informational for this tool, and the reader stops here. If it is RelativityOne, the token comes from the token-acquisition part that follows, and the user name and password are replaced by the OAuth2 Client's client ID and secret, kept as Secret Management (Part B of the Migration Documentation) describes, never in source, in a tracked `.env` file or in a bundle.

**Common to all three variants.** The text has a common part and **three variants by application type**, each opening with the precondition it is for: a browser with a server-side component, a Node.js server, and a Node.js command-line tool or desktop shell. Read the precondition of each and discard the variants that do not apply.

**Sign in.** Replace the code that collected the person's credentials with a redirect to the authorize endpoint `<host>/Relativity/Identity/connect/authorize` (**verified by NSerio against a live instance; not documented by Relativity**), which leads to RelativityOne's login page. The request carries the standard authorization-code parameters — `client_id`, `response_type=code`, the registered `redirect_uri`, `state` checked on return, and `code_challenge` with `code_challenge_method=S256` when PKCE is used; these are standard OAuth and Relativity documents none of them.

**PKCE.** PKCE is required by Relativity's own documentation to obtain a refresh token and not otherwise: a Code request that does not ask for a refresh token works without it, **verified by NSerio against a live instance; not documented by Relativity**. NSerio recommends `S256` (NSerio recommendation) and does not claim that Relativity accepts any particular method. `state` is generated, stored against the session and compared on return; `nonce` is not used, and Relativity does not document whether it enforces either.

**The client secret is required.** Code does not work without a client secret: tested and rejected, **verified by NSerio against a live instance; not documented by Relativity**, which confirms Relativity's own statement. **Any secret shipped to a browser is public**, and bundling, minifying or obfuscating changes nothing, so the exchange runs where a secret can be kept.

**Exchange the code.** A form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=authorization_code`, the code, the same `redirect_uri`, the client ID and the secret, plus the `code_verifier` when PKCE is used (**verified by NSerio against a live instance; not documented by Relativity**).

**Redirect URL.** A server-side application registers **one exact HTTPS URL per environment, no wildcards**; the matching rules are not documented and no claim is made about them. A plain-HTTP loopback Redirect URL `http://127.0.0.1:<port>/<path>` on a **fixed registered port** is accepted, **verified by NSerio against a live instance; not documented by Relativity**, and is the desktop shape.

**On expiry.** With Allow Refresh Tokens enabled and PKCE used, the code holds a refresh token and sends `grant_type=refresh_token` in place of the code exchange: *inferred* from the standard OAuth shape, not tested. Relativity does not document a refresh token's lifetime, rotation or revocation, nor what disabling the client does to tokens already issued. **A refresh token is a secret.** **Single sign-on:** sign-in happens on RelativityOne's own page, so the instance's single sign-on and two-factor rules apply unchanged (*inferred*). **The code stops collecting the password:** after this change nothing receives, stores or forwards the person's user name and password, and the code holds only tokens. Every call now runs as the person who signed in, so **the calling identity changes**. **Scope:** no scope set is prescribed for Code; `openid RelativityRequestOrigin UserInfo UserInfoAccess AuthenticationID RelativityWeb` works, **verified by NSerio against a live instance; not documented by Relativity**, as working and not as minimal.

**Variant 1 — Browser with a server-side component**

**Precondition: this variant is for code that runs in a web browser and signs a person in to RelativityOne. If your code runs in Node.js, use variant 2 or variant 3.**

A browser cannot hold the secret the Code grant requires, so **the browser never calls RelativityOne**: it navigates to a route on **the application's own origin**, and a **server-side component** (variant 2's code) creates `state`, redirects to the authorize endpoint, receives the code at its registered HTTPS Redirect URL, exchanges it, keeps the tokens and gives the browser only its own session. **The browser holds a session identifier with that component and nothing from RelativityOne.** The authorize step is a **top-level navigation** (a full-window redirect, not a `fetch`).

```javascript
const LOGIN_ROUTE = "/auth/login";
const API_ROUTE = "/api/documents";

function signIn() {
	window.location.assign(LOGIN_ROUTE);
}

async function loadDocuments() {
	const response = await fetch(API_ROUTE, { credentials: "same-origin" });
	return response.json();
}
```

Both calls go to the page's own origin. `credentials: "include"` is a cookie policy and is not recommended here, because the architecture needs no cross-origin call. **Cross-origin access, both ways, both not verified:** NSerio has not run a preflight against a live instance. *If RelativityOne serves CORS to browser callers*, the legacy browser Basic call could have worked from another origin, and a browser holding a Bearer token could in principle call it directly; **this text still does not recommend that**, because the only grant that yields a token for a person needs the secret the browser cannot hold. *If it does not*, the legacy call worked only from the same origin or through a proxy the client already runs. **The recommended architecture holds under either answer.** **The component keeps the tokens on the server**, never in browser storage, which any script on the page can read (a browser behaviour, unchecked); the recommended session cookie is `HttpOnly`, `Secure` and `SameSite` (NSerio recommendation; unchecked).

**Variant 2 — Node.js server**

**Precondition: this variant is for code that runs in Node.js as a web server and acts for people who sign in to it. It is not for browser code (variant 1) or a command-line tool (variant 3).**

Standard server-side Code flow. A login route builds the authorize address with `state` (and the PKCE verifier where a refresh token is wanted), stores them against the visitor's session and redirects; RelativityOne redirects to the **callback route**, registered as an exact HTTPS URL; the callback checks `state` and exchanges the code with the secret. This is the component variant 1 relies on.

```javascript
import crypto from "node:crypto";

const IDENTITY_BASE = "https://HOST/Relativity/Identity/connect";
const REDIRECT_URI = "https://app.example.test/auth/callback";
const CLIENT_ID = process.env.RELATIVITY_CLIENT_ID;
const CLIENT_SECRET = process.env.RELATIVITY_CLIENT_SECRET;
const RANDOM_BYTES = 32;
const pendingBySession = new Map();

function base64Url(buffer) {
	return buffer.toString("base64url");
}

function beginLogin(sessionId, wantRefreshToken) {
	const state = base64Url(crypto.randomBytes(RANDOM_BYTES));
	const verifier = base64Url(crypto.randomBytes(RANDOM_BYTES));
	const challenge = base64Url(crypto.createHash("sha256").update(verifier).digest());
	pendingBySession.set(sessionId, { state, verifier });
	const query = new URLSearchParams({
		response_type: "code",
		client_id: CLIENT_ID,
		redirect_uri: REDIRECT_URI,
		state,
	});
	if (wantRefreshToken) {
		query.set("code_challenge", challenge);
		query.set("code_challenge_method", "S256");
	}
	return `${IDENTITY_BASE}/authorize?${query}`;
}

async function completeLogin(sessionId, returnedCode, returnedState) {
	const pending = pendingBySession.get(sessionId);
	pendingBySession.delete(sessionId);
	const stateMatches = pending !== undefined
		&& returnedState !== undefined
		&& Buffer.byteLength(returnedState) === Buffer.byteLength(pending.state)
		&& crypto.timingSafeEqual(Buffer.from(returnedState), Buffer.from(pending.state));
	let tokens = null;
	if (stateMatches) {
		const response = await fetch(`${IDENTITY_BASE}/token`, {
			method: "POST",
			headers: { "Content-Type": "application/x-www-form-urlencoded" },
			body: new URLSearchParams({
				grant_type: "authorization_code",
				code: returnedCode,
				redirect_uri: REDIRECT_URI,
				client_id: CLIENT_ID,
				client_secret: CLIENT_SECRET,
				code_verifier: pending.verifier,
			}),
		});
		tokens = response.ok ? await response.json() : null;
	}
	return tokens;
}
```

`state` is consumed once and compared on return; a request with no pending entry or a different `state` gets no exchange. **The scope is not prescribed** (the sketch sends none). **Where the secret travels in the token request** (the body, as sketched, or HTTP Basic client authentication) is **not verified**. The sketch does not show the HTTP server, the session cookie or a session store that survives more than one process. **Tokens stay on the server**, in a session store that is not exposed to a request.

**Variant 3 — Node.js command line or desktop**

**Precondition: this variant is for code that runs on a person's own machine, as a command-line tool or a desktop shell, and signs that person in. It is not for a web server (variant 2) or browser code (variant 1).**

The process starts a listener from the built-in `node:http` module, bound to **`127.0.0.1` only**, on the **fixed registered port**, for **one request**; opens the system browser on the authorize address **and always prints the address too**, so a machine with no browser, an SSH session or a failed launch has a manual route; reads `code` and `state` from the one request, closes the listener, checks `state`, and exchanges the code as variant 2's `completeLogin` does.

```javascript
import http from "node:http";

const LOOPBACK_HOST = "127.0.0.1";
const REGISTERED_PORT = 8123;
const CALLBACK_PATH = "/callback";
const BASE_FOR_PARSING = `http://${LOOPBACK_HOST}:${REGISTERED_PORT}`;
const WAIT_MILLISECONDS = 120000;
const STATUS_OK = 200;
const STATUS_NOT_FOUND = 404;

function waitForRedirect() {
	return new Promise((resolve, reject) => {
		const server = http.createServer((request, response) => {
			const address = new URL(request.url, BASE_FOR_PARSING);
			const isCallback = address.pathname === CALLBACK_PATH;
			response.statusCode = isCallback ? STATUS_OK : STATUS_NOT_FOUND;
			response.end(isCallback ? "You can close this window." : "Not found");
			if (isCallback) {
				server.close();
				resolve({
					code: address.searchParams.get("code"),
					state: address.searchParams.get("state"),
				});
			}
		});
		server.on("error", reject);
		server.listen(REGISTERED_PORT, LOOPBACK_HOST);
		setTimeout(() => {
			server.close();
			reject(new Error("No redirect arrived"));
		}, WAIT_MILLISECONDS).unref();
	});
}
```

**Rules for the listener:** bind to loopback only; accept one request; **check `state`** (another local process can connect to a fixed, advertised port); close the listener after the request; and handle a busy port (a second server on a port in use raises `EADDRINUSE`, so fall back to the printed address). **Opening the system browser:** Node.js core has no function for it; run the platform's own launcher with a fixed program and an argument array, never a shell string built from the address (unchecked on every system). **Where tokens are kept:** a file with restricted permissions or the operating system's keychain; on Windows the POSIX mode argument of Node.js did not restrict a file when it was tried (*checked by NSerio locally* on Node.js v24.11.1), so a restricted file there needs an access-control change by other means, and on Linux and macOS it is unchecked. **The client secret of a distributed tool is extractable:** a command-line tool or desktop shell shipped to people carries its client secret in a form anyone who holds the distribution can extract, and it protects nothing against the person running it. **A known issue:** Relativity documents no recommendation for a distributed client; this design is what NSerio verified works. **Compensating guidance** (NSerio recommendation, subject to your own security policy): Trusted IPs where the users' outbound addresses are known and stable (checked at token issuance; applicability to Code *inferred*); the recommended short Access Token Lifetime; refresh tokens only when needed; and **one OAuth2 Client per distribution**, so a leaked secret can be disabled and reset alone.

Verification: **Verification note.** **The tool resolved the header's variable to a scheme of `Basic` in this file** (one declaration, before the use); a variable it cannot resolve is not read. **A token has a finite lifetime.**

**If this code runs in a browser, the credential has been public for as long as the code has shipped.** The exposure and the rotation are stated here and the tool does not escalate it:

> Treat this credential as compromised. Rotate it, and assume the repository history has published it. Completing the migration does not close this exposure; rotation does.

OAuth2 Client: Create or confirm this OAuth2 Client in RelativityOne (NSerio's observation: the menu path is Home > Authentication > OAuth2 Client Manager, as Relativity's REST API authentication page names it). This tool never connects to RelativityOne and never creates or changes the OAuth2 Client; the settings below are guidance.
- Name: a descriptive name identifying the application it serves.
- Flow Grant Type: Code
- Redirect URLs: Relativity documentation: each URL includes the http or https protocol prefix. The exact callback URL is registered by the Relativity administrator. Server-side web applications, NSerio's recommendation: one exact HTTPS URL per environment, with no wildcards. Desktop and CLI applications: a plain-HTTP loopback URL, `http://127.0.0.1:<port>/<path>`, on a fixed port because the URL is registered in advance and the port cannot be chosen at runtime (verified by NSerio against a live instance; not documented by Relativity).
- Access Token Lifetime: 60 minutes, stated in minutes because the field is in minutes. Relativity documentation: tokens for this flow must have a short lifetime, and the recommended value matches the Identity Server default.
- Allow refresh tokens: NSerio's recommendation: leave it off unless the application must keep a user session active beyond the access token lifetime without asking the user to sign in again. If it is on, the refresh tokens are stored as secrets, per Secret Management (Part B of the Migration Documentation).
- Enabled: on.
<!-- generated:end JS-017 authorization-code -->

## Generic suggestion

The generic suggestion is what the tool gives a High or Medium finding of a construct its catalog has no entry for. In JavaScript every class that can reach High or Medium has its own entries above, so the generic suggestion covers a class that a later release adds without one. It is a pointer and not an instruction: the migration must be designed from the code by whoever owns it.

### Client Credentials

<!-- generated:begin generic client-credentials -->
**Confirming the flow.** When no signal in a file decides the flow, the tool assigns Client Credentials, the default, with this note: "Who supplies the credential at run time? If a person types it or signs in, the integration acts for that person: use Authorization Code. If it comes from configuration, settings, an environment variable or a secret store: use Client Credentials. No signal in this file showed which applies, so Client Credentials, the default, is recommended. Use Authorization Code instead if the integration acts on behalf of a signed-in user, or if your RelativityOne instance already has a Code-flow OAuth2 Client for this integration." A fixed-credential signal assigns it with this note: "A fixed-credential signal in this file points to Client Credentials, but how often that kind of signal is right has not been measured. Use Authorization Code instead if the integration acts on behalf of a signed-in user, or if your RelativityOne instance already has a Code-flow OAuth2 Client for this integration." See [Confirming the flow](#confirming-the-flow).

The tool reports Basic Authentication at this location but did not recognize the construct, so the change has not been characterized and no before-and-after form is offered here. If the destination is not RelativityOne, the migration is that service's own and this finding is informational (that Relativity's identity endpoint issues no tokens for another service is unverified). If the destination is RelativityOne, Relativity's documentation recommends bearer tokens and states that Basic Authentication is no longer supported for named APIs, with no date for its removal asserted (the platform's direction beyond that is NSerio's reading, unverified). The documented destination in JavaScript is a bearer `Authorization` header sent by an HTTP client such as `fetch`, carrying a token obtained from the platform's identity endpoint with the client-credentials grant, from a server-side component or a Node.js process. The client ID and secret that obtain that token belong in the process environment or a secret store, per Secret Management (Part B of the Migration Documentation), and never in source, in a bundle or in browser code: a secret shipped to a browser is public. Beyond those facts this is a pointer and not an instruction: the migration must be designed from the code by whoever owns it, rather than applied from this text.

OAuth2 Client: Create or confirm this OAuth2 Client in RelativityOne (NSerio's observation: the menu path is Home > Authentication > OAuth2 Client Manager, as Relativity's REST API authentication page names it). This tool never connects to RelativityOne and never creates or changes the OAuth2 Client; the settings below are guidance.
- Name: a descriptive name identifying the integration it serves. Use one OAuth2 Client per integration and do not share it across integrations.
- Flow Grant Type: Client Credentials
- Context User: NSerio's recommendation: a dedicated integration user with the minimum permissions the integration needs, and not a personal account and not an administrator, because every call runs with that user's permissions and an administrator Context User gives the integration an administrator's reach. Relativity documentation (REST API authentication page): describes the Context User as usually a member of the Relativity Administrators group, though it could differ depending on the API or operation invoked; NSerio recommends against that default for an integration.
- Access Token Lifetime: 60 minutes, stated in minutes because the field is in minutes. Relativity documentation: tokens for this flow must have a short lifetime, and the recommended value matches the Identity Server default.
- Trusted IPs: Relativity documentation: the address list is checked whenever a token is requested with the OAuth2 Client's ID and secret. NSerio's recommendation: populate it with the integration's outbound IP addresses, one per line, wherever they are known and stable. It restricts where a token can be requested, checked when the token is issued; it does not restrict a token already issued, which travels in the request header and is valid from anywhere until it expires.
- Allow refresh tokens: not used by this flow. The integration requests a new access token when the current one expires.
- Enabled: on.
- Every call made with this OAuth2 Client runs with the Context User's permissions.
- Relativity documentation: the flow is fixed when the OAuth2 Client is created and cannot be changed when editing it, so moving this integration to Authorization Code later means creating a new OAuth2 Client.
<!-- generated:end generic client-credentials -->

### Authorization Code

<!-- generated:begin generic authorization-code -->
**Confirming the flow.** An interactive-user signal in a file assigns Authorization Code, with this note: "An interactive-user signal in this file points to Authorization Code. Confirm the flow: use Client Credentials instead if the integration runs with credentials fixed for the integration and no signed-in user." Your user profile can also make Authorization Code the default; see Changing the default in [Confirming the flow](#confirming-the-flow). See [Confirming the flow](#confirming-the-flow).

The tool reports Basic Authentication at this location but did not recognize the construct, so the change has not been characterized and no before-and-after form is offered here. If the destination is not RelativityOne, the migration is that service's own and this finding is informational (that Relativity's identity endpoint issues no tokens for another service is unverified). If the destination is RelativityOne, Relativity's documentation recommends bearer tokens and states that Basic Authentication is no longer supported for named APIs, with no date for its removal asserted (the platform's direction beyond that is NSerio's reading, unverified), and this finding is assigned the Authorization Code flow, in which the code sends the person to RelativityOne to sign in, receives an authorization code at a Redirect URL registered on its OAuth2 Client, and exchanges the code for an access token that it sends as a Bearer token. Code requires a client secret, so the exchange runs on a server-side component or a Node.js process and never in browser code: for code that runs in a browser the browser navigates to a route on the application's own origin and the server-side component performs the sign-in and keeps the tokens; for a Node.js server the callback route performs it; for a command-line tool or a desktop process a loopback listener on the registered fixed port receives the code. The authorize endpoint is `<host>/Relativity/Identity/connect/authorize` and the code is exchanged at `<host>/Relativity/Identity/connect/token`; both are verified by NSerio against a live instance; not documented by Relativity. PKCE is required by Relativity's own documentation to obtain a refresh token, and not otherwise. The client ID and secret belong in the process environment or a secret store, per Secret Management (Part B of the Migration Documentation), never in source or in a bundle. After the change the code no longer collects or forwards the person's password. Beyond those facts this is a pointer and not an instruction: the migration must be designed from the code by whoever owns it, rather than applied from this text.

OAuth2 Client: Create or confirm this OAuth2 Client in RelativityOne (NSerio's observation: the menu path is Home > Authentication > OAuth2 Client Manager, as Relativity's REST API authentication page names it). This tool never connects to RelativityOne and never creates or changes the OAuth2 Client; the settings below are guidance.
- Name: a descriptive name identifying the application it serves.
- Flow Grant Type: Code
- Redirect URLs: Relativity documentation: each URL includes the http or https protocol prefix. The exact callback URL is registered by the Relativity administrator. Server-side web applications, NSerio's recommendation: one exact HTTPS URL per environment, with no wildcards. Desktop and CLI applications: a plain-HTTP loopback URL, `http://127.0.0.1:<port>/<path>`, on a fixed port because the URL is registered in advance and the port cannot be chosen at runtime (verified by NSerio against a live instance; not documented by Relativity).
- Access Token Lifetime: 60 minutes, stated in minutes because the field is in minutes. Relativity documentation: tokens for this flow must have a short lifetime, and the recommended value matches the Identity Server default.
- Allow refresh tokens: NSerio's recommendation: leave it off unless the application must keep a user session active beyond the access token lifetime without asking the user to sign in again. If it is on, the refresh tokens are stored as secrets, per Secret Management (Part B of the Migration Documentation).
- Enabled: on.
<!-- generated:end generic authorization-code -->
