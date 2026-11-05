# Migration Suggestions for C# (Part C of the Migration Documentation)

Part C tells you what to do when a Part A pattern is found in your C# code. It gives the same recommendation the tool gives for that finding, because the recommendations below are produced by the same recommendation catalog the tool reports from, and are not written separately.

This document has three kinds of text:

- **Hand-written guidance.** How to confirm which OAuth 2.0 flow applies, and how to judge a match the tool marked Low confidence.
- **Recommendations.** One entry for each pattern class and each flow, and one generic recommendation for each flow. Each sits between a pair of `generated` markers, which are invisible when the document is rendered.
- **Pointers.** Part A ([`part-a-csharp.md`](part-a-csharp.md)) describes each pattern and how to search for it. Secret Management (Part B of the Migration Documentation, [`part-b-secret-management.md`](part-b-secret-management.md)) says where the new secrets go.

Every example uses placeholders such as `<host>`, `<client id>` and `<client secret>`. Replace them with your own values, and keep real secrets out of source code.

The two flows are the only ones the tool recommends:

- **Client Credentials** is the default. The application sends its client ID and client secret to the token endpoint and receives an access token. No user is involved, and every call runs as the OAuth2 Client's Context User.
- **Authorization Code** is called "Code" in RelativityOne's OAuth2 Client form. The application redirects a signed-in user to RelativityOne, receives an authorization code at a registered Redirect URL, and exchanges it for a token that carries that user's permissions.

## Confirming the flow

Ask one question of the code that holds the match: **who supplies the credential at run time?**

- If a person types it or signs in, the integration acts for that person. Use **Authorization Code**.
- If it comes from configuration, settings, an environment variable or a secret store, and no person is present, use **Client Credentials**.

The tool answers that question by looking for signals in the **same file** as the match. Part A lists every signal by name under [Flow signals](part-a-csharp.md#flow-signals). The rules it applies are these:

1. **An interactive-user signal decides Authorization Code.** Interactive-user signals are a console prompt that asks for a password, a desktop password control, and a web login form or login handler. If an interactive-user signal and a fixed-credential signal are both present, the interactive one wins.
2. **A fixed-credential signal decides Client Credentials.** Fixed-credential signals are a read of a credential-named key from configuration, from an environment variable, or from a secret store. The recommendation carries a note asking you to check, because nothing yet measures how often such a signal points the wrong way.
3. **No signal means the default.** With no signal in the file, the tool recommends Client Credentials and carries the same note. This is the expected result for most real integrations and not a failure of the tool, for the reason below.

**The tool reads one file at a time.** A credential prompt, a login page or a login handler that sits in a different file from the match is not seen, and neither is a credential that reaches the match through dependency injection or configuration binding. Such a match is decided by the default even when the integration acts for a signed-in user. When you review a finding, look beyond its file: follow the credential back to where it comes from, and if a person supplies it, use Authorization Code even though the tool recommended Client Credentials.

Two more rules the tool applies, so that you can reproduce its choice by hand:

- A credential written literally into the source is **not** evidence of either flow. It is a secret to remove, and Part B explains how. A file whose only credential is a literal is decided by the default.
- The tool works from the scanned code and local configuration only. It never contacts RelativityOne, so it cannot tell you what OAuth2 Clients your instance already has.

**Confirm against your instance.** If your RelativityOne instance already has an OAuth2 Client with the Code flow for this integration, use Authorization Code, whatever the tool recommended. If it has one with Client Credentials for this integration, use Client Credentials. The flow is fixed when an OAuth2 Client is created, so moving an integration from one flow to the other means creating a new OAuth2 Client.

**Changing the default.** If most of your integrations are user-facing, you can make Authorization Code the default. Set `defaultFlow` to `AuthorizationCode` in the `.migrate-auth.json` file in your user profile folder; the accepted values are `ClientCredentials` and `AuthorizationCode`. The tool reads this setting from the user profile file only. A `defaultFlow` in the scanned repository's own file is ignored and recorded as ignored, so a repository cannot change the recommendation for the findings it holds. The setting changes only the default; a signal in the file still decides the flow.

## Judging a Low-confidence match

A match is **Low** confidence when it is present in the text but unlikely to be live authenticating code. The tool gives a Low match no flow and no recommendation, and lists it for manual review. **A Low level is not evidence that the code does not authenticate.** It means a person has to look. The paragraphs below say how to judge each cause and when the match still needs a migration. When it does, find the live construct it belongs to, identify its class in Part A, and follow that class's entry in [Migration suggestions by pattern class](#migration-suggestions-by-pattern-class).

### Comments

The match is inside a `//` or `/* */` comment, so no code runs. Read the comment. If it is commented-out code, nothing needs migrating today, but delete it or migrate it before anyone restores it, because a restored line brings Basic Authentication back. If it describes code that exists elsewhere, search for that code with Part A and migrate the live code. If it only illustrates a pattern with placeholders, no migration is needed.

### Documentation strings

The match is inside text that documents or explains, such as an XML documentation comment, a help message, an error message or a sample shown to the user. The string does not authenticate anything. Check that the behaviour it describes has been migrated, and update the wording afterwards so that the documentation stops describing Basic Authentication. The string itself needs no migration.

### Test fixtures and mocks

**Test code is not Low.** A match in a file the tool recognizes as test code is reported at **Medium** at most, with the recommendation of its class, and the report marks the finding `Test code: yes` (the JSON data file sets `testCode` to `true`). The tool recognizes test code by a folder named `test`, `tests`, `spec` or `fixtures`, a folder whose name ends in `.Test` or `.Tests`, a file name that ends in `.Tests.cs`, `.Test.cs` or `.Spec.cs`, or a test attribute on the code. Judge by what the test talks to. If it feeds a mock and never reaches RelativityOne, no migration is needed, though you may want the mock to model the token flow so the test still represents the code it covers. If it is an integration test that authenticates to a real RelativityOne instance, it needs migrating with everything else, because it stops working when Basic Authentication is switched off: follow the entry of its class. A credential written literally into a test is still a secret to remove.

**A mock is Low.** A match in a file the tool recognizes as a mock, fake or stub is reported at Low, with no recommendation: a file name that begins or ends with `Fake`, `Mock` or `Stub`. The tool recognizes a mock by its file name and by nothing else, so a mock that has a neutral name is reported at the level of its class, or at Medium when it also sits in test code; review it as you would a test. A file that is test code and also a mock, a sample, vendored or generated is Low, and the report still says it is test code. For a Low mock, judge by what it stands in for: if nothing runs it against RelativityOne, no migration is needed.

### Sample and example code

The match sits in a folder named `sample`, `samples`, `example` or `examples`, in any letter case, or in a file whose name begins with `Sample` or `Example`. Those are the only names the tool recognizes: a sample kept in a folder called `demo`, or in a file whose name merely contains the word, is not demoted and is reported at its ordinary level, so review it as live code. Judge by who uses it. If nobody runs the sample, it needs no migration. If people copy it as a starting point, migrate it: an example that teaches Basic Authentication spreads it to new code. Use the recommendation for the class the sample matches, and prefer the flow that fits the scenario the sample shows.

### Vendored and generated files

The match sits in a file produced by a tool (a generated-code header, a generated-code attribute, or a generated or designer file name) or in a folder named `vendor` or `third-party`, which holds third-party code. **Do not edit a generated file by hand:** the next generation overwrites the edit. Fix the source it is generated from (the template, the schema or the generator settings) and regenerate. For vendored code, upgrade the library to a version that supports token authentication, or replace it. A migration is needed when the file runs against RelativityOne in your application. It is not needed when the file is never called, or when it is only reference material.

### Deployment folders

The match sits in a folder named `Deployment`, in any letter case, which the tool treats as installation and release tooling rather than the application's own code. Read the file before you decide, because a deployment folder is also where an installer or a release script that talks to RelativityOne is kept. A migration is needed when the file is the one that runs against RelativityOne in your application or in the deployment itself. It is not needed when the file is never run, or when it only illustrates a pattern. Judge by who runs it and against which server: a deployment script that signs in to RelativityOne with a user name and a password stops working when Basic Authentication is switched off, and it is a script people copy from one environment to the next, so migrate it with everything else. A credential written literally into a deployment script is still a secret to remove.

### CS-011 · AuthenticationHelperDefinition

<a id="cs-011-judging"></a>

A method that builds the credential or the authentication header for the project is a helper. The tool marks its definition Low because the advice for a helper is not a mechanical code change. **The helper is the place to migrate, and it hides its callers:** the tool reports the helper's definition once, and its call sites do not appear as findings because they contain no visible credential. Search for the helper's name to find every caller. If the helper builds Basic Authentication for calls to RelativityOne, migrate it once, so that it obtains and caches an access token and sends a Bearer header. Decide the flow from who supplies the credential to its callers: a person means Authorization Code, and a fixed credential means Client Credentials. Then follow the entry for the construct the helper contains, if it has one. If the helper builds a credential for another service, it needs no migration.

### CS-019 · NonHttpCredentialSink

<a id="cs-019-judging"></a>

A credential object that is handed to a sink that is not an HTTP client, such as a mail client, a file-transfer request, a directory entry or searcher, or a network proxy. The tool marks it Low because it does not believe it is Basic Authentication towards RelativityOne, and it exists so that mail, file-transfer, directory and proxy credentials stop being reported as migration targets. Confirm the sink. If the credential really goes to a mail server, a file server, a directory or a proxy, no migration to RelativityOne is needed, and the credential's secret handling is still governed by your own policy. If the same credential is also used to call RelativityOne, that use is reported separately under the class that matches it, and needs migrating.

## Migration suggestions by pattern class

Each pattern class has two entries, one for each flow. Choose the entry for the flow you confirmed above. Every entry contains the code being replaced, the migrated code, a description of token acquisition, caching and expiry handling, how to confirm the flow and the default, the OAuth2 Client to create or confirm with its settings, and the pointer to Secret Management (Part B of the Migration Documentation). Every Authorization Code entry adds a description of refresh-token handling, of PKCE, and of a server-side variant and a desktop variant.

**An entry describes token acquisition, caching and expiry; it contains no token-handling code.** The code in an entry is the change at the point of use. C# code examples for acquiring, caching and renewing a token are planned after the alpha.

**Part 1, part 2 and part 3.** The text of an entry refers to its own pieces by number. The same names appear in the report's recommendations. Each piece has a label in the entry:

- **Part 1** is the change at the point of use: the text under the label *Under token authentication*, with its migrated code.
- **Part 2** is the token acquisition: the part of the text under the label *Credentials* that follows the paragraph *Where the credentials come from*. It describes the request to the token endpoint, the caching of the token and what to do when it expires. A Client Credentials entry starts it with *Where the token comes from*, and an Authorization Code entry with *Common to both variants*.
- **Part 3** is the OAuth2 Client settings, under the label *OAuth2 Client* at the end of the entry.

The labels *Source form* (the construct being replaced) and *Verification* (what to check before you deploy) are not numbered.

The two Low classes, CS-011 and CS-019, have no entry because they produce no recommendation. They are covered under [Judging a Low-confidence match](#judging-a-low-confidence-match).

### CS-001 · SdkUserNamePasswordCredentials

<a id="cs-001-migration"></a>

#### Client Credentials

<!-- generated:begin CS-001 client-credentials -->
**Confirming the flow.** When no signal in a file decides the flow, the tool assigns Client Credentials, the default, with this note: "Who supplies the credential at run time? If a person types it or signs in, the integration acts for that person: use Authorization Code. If it comes from configuration, settings, an environment variable or a secret store: use Client Credentials. No signal in this file showed which applies, so Client Credentials, the default, is recommended. Use Authorization Code instead if the integration acts on behalf of a signed-in user, or if your RelativityOne instance already has a Code-flow OAuth2 Client for this integration." A fixed-credential signal assigns it with this note: "A fixed-credential signal in this file points to Client Credentials, but how often that kind of signal is right has not been measured. Use Authorization Code instead if the integration acts on behalf of a signed-in user, or if your RelativityOne instance already has a Code-flow OAuth2 Client for this integration." See [Confirming the flow](#confirming-the-flow) and the "Which flow" paragraph of CS-001 in [Part A](part-a-csharp.md#cs-001).

Source form:
```csharp
Credentials credentials = new UsernamePasswordCredentials(userName, password);
ServiceFactorySettings settings = new ServiceFactorySettings(restEndpointUri, credentials);
```

Under token authentication: **First, the destination:** if this Relativity instance is not RelativityOne — including a Relativity Server instance, whose own documentation governs — this finding is informational and the rest of this template does not apply.

```csharp
ServiceFactorySettings settings = new ServiceFactorySettings(
	restEndpointUri, new BearerTokenCredentials(accessToken));
```

The REST URI is unchanged: `<scheme>://<machine name>/Relativity.Rest/api`.

Credentials: **Where the credentials come from.** The SDK type establishes that the destination is Relativity, not that it is RelativityOne; if it is not RelativityOne — including a Relativity Server instance, whose own documentation governs — the migration is that service's own token mechanism, this finding is informational for this tool, and no migration to RelativityOne applies. If it is RelativityOne, the token comes from the token-acquisition part that follows.

**Where the token comes from.** Create an OAuth2 Client with Flow Grant Type set to Client Credentials (part 3 lists its settings), then send a form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=client_credentials`, `client_id`, `client_secret` and a `scope`. The token endpoint is **documented** for this grant. The token is then sent as `Authorization: Bearer <token>`, with `X-CSRF-Header: -` on REST and Kepler calls — the header is documented as required, and a Kepler call without it returns 404.

**The scope depends on what the integration needs, and no single scope is required.** Relativity's documentation shows two values for this grant: `SystemUserInfo` on the REST authentication page, the Usage Reports page and in the official Transfer SDK sample, and `UserInfoAccess` in the Kepler .NET samples. No page says which is correct, whether both work, or what either grants, and the OAuth2 Client's `Scopes` property is documented as one that cannot be set. Use the value the API you call documents, and confirm it.

**Cache the token, and re-acquire it on expiry — do not refresh.** Keep the token for its lifetime (part 3 recommends 60 minutes, Relativity's own wording) and request a new one with the client secret when it expires or a call is rejected for authentication. Client Credentials does not offer refresh tokens: **verified by NSerio against a live instance; not documented by Relativity** — the Allow Refresh Tokens control does not appear for this grant in the RelativityOne user interface. Relativity's own tables and text on refresh tokens conflict with each other, so re-acquisition with the secret is the advice that holds whichever way they are read.

**Long-running processes that use the Relativity SDK.** A `ServiceFactory` built once from a bearer token keeps that token after it expires. Rebuild the `ServiceFactory` and its proxies with a fresh token before expiry. This is the only advice consistent with the documentation: the package `Relativity.Kepler.Client.SDK` (documented) exposes a settable `Token` and an `HttpMessageHandler` overload but no token-provider overload, and no page documents renewing a token behind a factory.

**The client secret.** It is displayed once, at creation or reset. Store it as Secret Management (Part B of the Migration Documentation) describes; never in source, never in tracked configuration. A reset invalidates the previous secret immediately, so plan the cut-over before resetting it.

**Before deploying, confirm the calling identity.** Every call now runs as the OAuth2 Client's **Context User** — not as the user in the replaced credential, and for an import or a job as much as for a request. If that user has different workspace permissions or item-level security, the code returns fewer rows or `Unauthorized` in production with no change to its logic, and Relativity's audit attributes every call to that one account. Confirm the Context User's permissions match those of the previous identity. Relativity's REST API authentication page describes the Context User as usually a member of the Relativity Administrators group; the OAuth2 Client block (part 3) gives NSerio's recommendation, which differs.

**When the integration acts for a signed-in user.** Client Credentials is one service identity by construction. An integration that acts on behalf of a signed-in user is the Authorization Code case, and moving to it means a new OAuth2 Client, because the flow is fixed when a client is created. The finding's flow note says when to make that choice.

Verification: **Verification note — two things, and the first can break working code.**

**A token has a finite lifetime**, set per client in minutes, and this construct holds it. A `ServiceFactory` built once from `BearerTokenCredentials(accessToken)` and kept for the life of a process keeps that token after it expires, and the calls made through it then fail authentication. Rebuild the settings and the factory with a current token within the token's lifetime; the previous credential never expired, so nothing in the existing code does this.

**Confirm the SDK version in use provides `BearerTokenCredentials`.** Where it does not, this is an SDK upgrade before it is a code change.

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
<!-- generated:end CS-001 client-credentials -->

#### Authorization Code

<!-- generated:begin CS-001 authorization-code -->
**Confirming the flow.** An interactive-user signal in a file assigns Authorization Code, with this note: "An interactive-user signal in this file points to Authorization Code. Confirm the flow: use Client Credentials instead if the integration runs with credentials fixed for the integration and no signed-in user." Your user profile can also make Authorization Code the default; see Changing the default in [Confirming the flow](#confirming-the-flow). See [Confirming the flow](#confirming-the-flow) and the "Which flow" paragraph of CS-001 in [Part A](part-a-csharp.md#cs-001).

Source form:
```csharp
Credentials credentials = new UsernamePasswordCredentials(userName, password);
ServiceFactorySettings settings = new ServiceFactorySettings(restEndpointUri, credentials);
```

Under token authentication: **First, the destination:** if this Relativity instance is not RelativityOne — including a Relativity Server instance, whose own documentation governs — this finding is informational and the rest of this template does not apply.

```csharp
ServiceFactorySettings settings = new ServiceFactorySettings(
	restEndpointUri, new BearerTokenCredentials(accessToken));
```

The REST URI is unchanged: `<scheme>://<machine name>/Relativity.Rest/api`.

Credentials: **Where the credentials come from.** The SDK type establishes that the destination is Relativity, not that it is RelativityOne; if it is not RelativityOne — including a Relativity Server instance, whose own documentation governs — the migration is that service's own token mechanism, this finding is informational for this tool, and no migration to RelativityOne applies. If it is RelativityOne, the token comes from the token-acquisition part that follows.

**Common to both variants**

**Sign in.** Replace the code that collected the user's credentials with a redirect: send the user's browser to the authorize endpoint `<host>/Relativity/Identity/connect/authorize`, which leads to the RelativityOne login page. The authorize endpoint is **verified by NSerio against a live instance; not documented by Relativity**. The request carries the standard authorization-code parameters — `client_id`, `response_type=code`, the registered `redirect_uri`, a `scope`, and a `state` value the application checks on return; these are standard OAuth, and Relativity documents none of them. No discovery document is relied on.

**Receive the code.** The browser returns to the Redirect URL registered on the OAuth2 Client, with the authorization code and the `state` value. Check the `state` before using the code. The Redirect URL must be exactly the one registered (the variants say how).

**Exchange the code.** Send a form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=authorization_code`, the `code`, the same `redirect_uri`, the `client_id` and the `client_secret`, plus the `code_verifier` when PKCE is used. The code is exchanged at that endpoint, which Relativity documents for the Client Credentials grant only; the exchange is **verified by NSerio against a live instance; not documented by Relativity**.

**Use the token.** Send `Authorization: Bearer <access token>` — the change at the use site in part 1 — with `X-CSRF-Header: -` on REST and Kepler calls.

**On expiry.** With Allow Refresh Tokens enabled on the OAuth2 Client and PKCE used, the application holds a refresh token and, when the access token expires, sends a `grant_type=refresh_token` request with it and the client credentials in place of the code exchange — *inferred* from the standard OAuth shape, not tested. Without a refresh token the user signs in again. Relativity does not document a refresh token's lifetime, whether it rotates, how it is revoked, or whether a revocation endpoint exists, nor what disabling or deleting the client does to tokens already issued. A refresh token is a secret: store it as Secret Management (Part B of the Migration Documentation) describes, and on compromise disable the client and replace it. Enable refresh tokens only if the application must keep a session beyond one token lifetime without asking the user to sign in again.

**PKCE.** PKCE is required by Relativity's own documentation to obtain a refresh token, and not otherwise: a Code request that does not ask for a refresh token works without it, **verified by NSerio against a live instance; not documented by Relativity**. It is never a blanket requirement. NSerio recommends PKCE with the `S256` method in both variants (NSerio recommendation), and does not claim that Relativity accepts any particular method, which it does not document.

**Scope.** No scope set is prescribed for Code. The scope below works — `openid RelativityRequestOrigin UserInfo UserInfoAccess AuthenticationID RelativityWeb` — **verified by NSerio against a live instance; not documented by Relativity**, verified as working and not as minimal. `state` is sent as standard OAuth practice and `nonce` is not used; Relativity does not document whether it enforces either.

**The application stops collecting the password.** After this change the application never receives, stores or forwards the user's username and password: the user signs in on RelativityOne's own page, and the application holds only tokens. Remove the credential prompt, the form field and any storage of the password that the replaced code needed.

**Single sign-on.** Sign-in happens on RelativityOne's own login page, so the instance's single sign-on and two-factor rules apply unchanged — *inferred* from where the sign-in happens, and not documented for clients that customers create.

**The client secret is required.** Code does not work without a client secret: tested and rejected, **verified by NSerio against a live instance; not documented by Relativity**, which confirms Relativity's own statement that the grant requires one. It is displayed once and is stored as Secret Management (Part B of the Migration Documentation) describes.

**Server-side variant.** For a web application whose code runs on a server:

- **Redirect.** The application receives the code at one exact HTTPS URL per environment, a route of the application, with no wildcards (NSerio recommendation; Relativity's matching rules are not documented). The Relativity administrator registers the exact URL on the OAuth2 Client.
- **Secret and tokens.** The client secret stays on the server and never reaches the browser. The user's tokens are held server-side, keyed to the session, and never in the browser.
- **Exchange.** The server performs the code exchange itself.

**Desktop variant.** For a desktop or command-line application run by a user on their own machine:

- **The system browser.** Open the authorize URL in the user's default browser and not in a web view the application hosts, so the application never sees the password (NSerio recommendation).
- **A loopback redirect on a fixed port.** Listen on the loopback address only, on the registered port, for one request. The Redirect URL is `http://127.0.0.1:<port>/<path>`: a plain-HTTP loopback URL is accepted, **verified by NSerio against a live instance; not documented by Relativity**. The Relativity administrator registers that exact URL; because it is registered in advance, the port is fixed and not chosen at runtime.
- **The secret is extractable.** Code requires a client secret (above), so a distributed desktop application has to embed one, and **a secret shipped inside a distributed binary can be extracted by anyone who holds the binary**. It protects nothing against the person running the application.
- **Compensating guidance** (NSerio recommendation, subject to your own security policy): set Trusted IPs where the users' outbound addresses are known and stable — they restrict where a token can be *requested*, and are checked when a token is issued against the client's ID and secret; use the recommended short Access Token Lifetime of 60 minutes; enable refresh tokens only when needed; and create one OAuth2 Client per distribution, so a leaked secret can be disabled without affecting the others.
- **A known issue.** Relativity documents no recommendation for a distributed desktop application; this design is what NSerio verified works, and Relativity's own recommendation is tracked as a known issue of the release.

Verification: **Verification note — two things, and the first can break working code.**

**A token has a finite lifetime**, set per client in minutes, and this construct holds it. A `ServiceFactory` built once from `BearerTokenCredentials(accessToken)` and kept for the life of a process keeps that token after it expires, and the calls made through it then fail authentication. Rebuild the settings and the factory with a current token within the token's lifetime; the previous credential never expired, so nothing in the existing code does this.

**Confirm the SDK version in use provides `BearerTokenCredentials`.** Where it does not, this is an SDK upgrade before it is a code change.

OAuth2 Client: Create or confirm this OAuth2 Client in RelativityOne (NSerio's observation: the menu path is Home > Authentication > OAuth2 Client Manager, as Relativity's REST API authentication page names it). This tool never connects to RelativityOne and never creates or changes the OAuth2 Client; the settings below are guidance.
- Name: a descriptive name identifying the application it serves.
- Flow Grant Type: Code
- Redirect URLs: Relativity documentation: each URL includes the http or https protocol prefix. The exact callback URL is registered by the Relativity administrator. Server-side web applications, NSerio's recommendation: one exact HTTPS URL per environment, with no wildcards. Desktop and CLI applications: a plain-HTTP loopback URL, `http://127.0.0.1:<port>/<path>`, on a fixed port because the URL is registered in advance and the port cannot be chosen at runtime (verified by NSerio against a live instance; not documented by Relativity).
- Access Token Lifetime: 60 minutes, stated in minutes because the field is in minutes. Relativity documentation: tokens for this flow must have a short lifetime, and the recommended value matches the Identity Server default.
- Allow refresh tokens: NSerio's recommendation: leave it off unless the application must keep a user session active beyond the access token lifetime without asking the user to sign in again. If it is on, the refresh tokens are stored as secrets, per Secret Management (Part B of the Migration Documentation).
- Enabled: on.
<!-- generated:end CS-001 authorization-code -->

### CS-002 · SdkLegacyRsapiCredentials

<a id="cs-002-migration"></a>

#### Client Credentials

<!-- generated:begin CS-002 client-credentials -->
**Confirming the flow.** When no signal in a file decides the flow, the tool assigns Client Credentials, the default, with this note: "Who supplies the credential at run time? If a person types it or signs in, the integration acts for that person: use Authorization Code. If it comes from configuration, settings, an environment variable or a secret store: use Client Credentials. No signal in this file showed which applies, so Client Credentials, the default, is recommended. Use Authorization Code instead if the integration acts on behalf of a signed-in user, or if your RelativityOne instance already has a Code-flow OAuth2 Client for this integration." A fixed-credential signal assigns it with this note: "A fixed-credential signal in this file points to Client Credentials, but how often that kind of signal is right has not been measured. Use Authorization Code instead if the integration acts on behalf of a signed-in user, or if your RelativityOne instance already has a Code-flow OAuth2 Client for this integration." See [Confirming the flow](#confirming-the-flow) and the "Which flow" paragraph of CS-002 in [Part A](part-a-csharp.md#cs-002).

Source form:
```csharp
var client = new RSAPIClient(rsapiUri, new UsernamePasswordCredentials(userName, password));
```

Under token authentication: **First, the destination:** if this Relativity instance is not RelativityOne — including a Relativity Server instance, whose own documentation governs — this finding is informational and the rest of this template does not apply.

**Two steps, not one, and in this order.** First, **move the call to Object Manager**: the RSAPI client is retired, so swapping the credential inside the RSAPI construction migrates the scheme of a call that should not survive. Rewrite the operation as the equivalent Object Manager call, reached through a service factory. Second, apply the token credential to that rewritten call:

```csharp
ServiceFactorySettings settings = new ServiceFactorySettings(
	restEndpointUri, new BearerTokenCredentials(accessToken));
```

The REST URI is `<scheme>://<machine name>/Relativity.Rest/api`, which is not the RSAPI endpoint the source form addressed.

Credentials: **Where the credentials come from.** The SDK type establishes that the destination is Relativity, not that it is RelativityOne; if it is not RelativityOne — including a Relativity Server instance, whose own documentation governs — the migration is that service's own token mechanism, this finding is informational for this tool, and no migration to RelativityOne applies. If it is RelativityOne, the token comes from the token-acquisition part that follows.

**Where the token comes from.** Create an OAuth2 Client with Flow Grant Type set to Client Credentials (part 3 lists its settings), then send a form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=client_credentials`, `client_id`, `client_secret` and a `scope`. The token endpoint is **documented** for this grant. The token is then sent as `Authorization: Bearer <token>`, with `X-CSRF-Header: -` on REST and Kepler calls — the header is documented as required, and a Kepler call without it returns 404.

**The scope depends on what the integration needs, and no single scope is required.** Relativity's documentation shows two values for this grant: `SystemUserInfo` on the REST authentication page, the Usage Reports page and in the official Transfer SDK sample, and `UserInfoAccess` in the Kepler .NET samples. No page says which is correct, whether both work, or what either grants, and the OAuth2 Client's `Scopes` property is documented as one that cannot be set. Use the value the API you call documents, and confirm it.

**Cache the token, and re-acquire it on expiry — do not refresh.** Keep the token for its lifetime (part 3 recommends 60 minutes, Relativity's own wording) and request a new one with the client secret when it expires or a call is rejected for authentication. Client Credentials does not offer refresh tokens: **verified by NSerio against a live instance; not documented by Relativity** — the Allow Refresh Tokens control does not appear for this grant in the RelativityOne user interface. Relativity's own tables and text on refresh tokens conflict with each other, so re-acquisition with the secret is the advice that holds whichever way they are read.

**Long-running processes that use the Relativity SDK.** A `ServiceFactory` built once from a bearer token keeps that token after it expires. Rebuild the `ServiceFactory` and its proxies with a fresh token before expiry. This is the only advice consistent with the documentation: the package `Relativity.Kepler.Client.SDK` (documented) exposes a settable `Token` and an `HttpMessageHandler` overload but no token-provider overload, and no page documents renewing a token behind a factory.

**The client secret.** It is displayed once, at creation or reset. Store it as Secret Management (Part B of the Migration Documentation) describes; never in source, never in tracked configuration. A reset invalidates the previous secret immediately, so plan the cut-over before resetting it.

**Before deploying, confirm the calling identity.** Every call now runs as the OAuth2 Client's **Context User** — not as the user in the replaced credential, and for an import or a job as much as for a request. If that user has different workspace permissions or item-level security, the code returns fewer rows or `Unauthorized` in production with no change to its logic, and Relativity's audit attributes every call to that one account. Confirm the Context User's permissions match those of the previous identity. Relativity's REST API authentication page describes the Context User as usually a member of the Relativity Administrators group; the OAuth2 Client block (part 3) gives NSerio's recommendation, which differs.

**When the integration acts for a signed-in user.** Client Credentials is one service identity by construction. An integration that acts on behalf of a signed-in user is the Authorization Code case, and moving to it means a new OAuth2 Client, because the flow is fixed when a client is created. The finding's flow note says when to make that choice.

Verification: **Verification note — four things, and the first two can break working code.**

**Verify equivalence before rewriting.** Not every RSAPI operation has a direct Object Manager counterpart. Establish that each operation this client performs has one before starting; where one does not, this is a redesign of that operation, not a migration of it.

**A token has a finite lifetime**, set per client in minutes. A service factory built once from the token and kept for the life of a process keeps it after it expires; rebuild it with a current token within that lifetime, which the RSAPI credential never required.

**Other RSAPI uses may not appear as findings.** This tool reports an RSAPI client only where it is constructed with a username-and-password credential; an RSAPI client obtained without one is not covered. Migrating this call does not establish that the codebase has no RSAPI left.

**Confirm the SDK version in use provides `BearerTokenCredentials` and the Object Manager service.** Where it does not, this is an SDK upgrade before it is a code change.

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
<!-- generated:end CS-002 client-credentials -->

#### Authorization Code

<!-- generated:begin CS-002 authorization-code -->
**Confirming the flow.** An interactive-user signal in a file assigns Authorization Code, with this note: "An interactive-user signal in this file points to Authorization Code. Confirm the flow: use Client Credentials instead if the integration runs with credentials fixed for the integration and no signed-in user." Your user profile can also make Authorization Code the default; see Changing the default in [Confirming the flow](#confirming-the-flow). See [Confirming the flow](#confirming-the-flow) and the "Which flow" paragraph of CS-002 in [Part A](part-a-csharp.md#cs-002).

Source form:
```csharp
var client = new RSAPIClient(rsapiUri, new UsernamePasswordCredentials(userName, password));
```

Under token authentication: **First, the destination:** if this Relativity instance is not RelativityOne — including a Relativity Server instance, whose own documentation governs — this finding is informational and the rest of this template does not apply.

**Two steps, not one, and in this order.** First, **move the call to Object Manager**: the RSAPI client is retired, so swapping the credential inside the RSAPI construction migrates the scheme of a call that should not survive. Rewrite the operation as the equivalent Object Manager call, reached through a service factory. Second, apply the token credential to that rewritten call:

```csharp
ServiceFactorySettings settings = new ServiceFactorySettings(
	restEndpointUri, new BearerTokenCredentials(accessToken));
```

The REST URI is `<scheme>://<machine name>/Relativity.Rest/api`, which is not the RSAPI endpoint the source form addressed.

Credentials: **Where the credentials come from.** The SDK type establishes that the destination is Relativity, not that it is RelativityOne; if it is not RelativityOne — including a Relativity Server instance, whose own documentation governs — the migration is that service's own token mechanism, this finding is informational for this tool, and no migration to RelativityOne applies. If it is RelativityOne, the token comes from the token-acquisition part that follows.

**Common to both variants**

**Sign in.** Replace the code that collected the user's credentials with a redirect: send the user's browser to the authorize endpoint `<host>/Relativity/Identity/connect/authorize`, which leads to the RelativityOne login page. The authorize endpoint is **verified by NSerio against a live instance; not documented by Relativity**. The request carries the standard authorization-code parameters — `client_id`, `response_type=code`, the registered `redirect_uri`, a `scope`, and a `state` value the application checks on return; these are standard OAuth, and Relativity documents none of them. No discovery document is relied on.

**Receive the code.** The browser returns to the Redirect URL registered on the OAuth2 Client, with the authorization code and the `state` value. Check the `state` before using the code. The Redirect URL must be exactly the one registered (the variants say how).

**Exchange the code.** Send a form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=authorization_code`, the `code`, the same `redirect_uri`, the `client_id` and the `client_secret`, plus the `code_verifier` when PKCE is used. The code is exchanged at that endpoint, which Relativity documents for the Client Credentials grant only; the exchange is **verified by NSerio against a live instance; not documented by Relativity**.

**Use the token.** Send `Authorization: Bearer <access token>` — the change at the use site in part 1 — with `X-CSRF-Header: -` on REST and Kepler calls.

**On expiry.** With Allow Refresh Tokens enabled on the OAuth2 Client and PKCE used, the application holds a refresh token and, when the access token expires, sends a `grant_type=refresh_token` request with it and the client credentials in place of the code exchange — *inferred* from the standard OAuth shape, not tested. Without a refresh token the user signs in again. Relativity does not document a refresh token's lifetime, whether it rotates, how it is revoked, or whether a revocation endpoint exists, nor what disabling or deleting the client does to tokens already issued. A refresh token is a secret: store it as Secret Management (Part B of the Migration Documentation) describes, and on compromise disable the client and replace it. Enable refresh tokens only if the application must keep a session beyond one token lifetime without asking the user to sign in again.

**PKCE.** PKCE is required by Relativity's own documentation to obtain a refresh token, and not otherwise: a Code request that does not ask for a refresh token works without it, **verified by NSerio against a live instance; not documented by Relativity**. It is never a blanket requirement. NSerio recommends PKCE with the `S256` method in both variants (NSerio recommendation), and does not claim that Relativity accepts any particular method, which it does not document.

**Scope.** No scope set is prescribed for Code. The scope below works — `openid RelativityRequestOrigin UserInfo UserInfoAccess AuthenticationID RelativityWeb` — **verified by NSerio against a live instance; not documented by Relativity**, verified as working and not as minimal. `state` is sent as standard OAuth practice and `nonce` is not used; Relativity does not document whether it enforces either.

**The application stops collecting the password.** After this change the application never receives, stores or forwards the user's username and password: the user signs in on RelativityOne's own page, and the application holds only tokens. Remove the credential prompt, the form field and any storage of the password that the replaced code needed.

**Single sign-on.** Sign-in happens on RelativityOne's own login page, so the instance's single sign-on and two-factor rules apply unchanged — *inferred* from where the sign-in happens, and not documented for clients that customers create.

**The client secret is required.** Code does not work without a client secret: tested and rejected, **verified by NSerio against a live instance; not documented by Relativity**, which confirms Relativity's own statement that the grant requires one. It is displayed once and is stored as Secret Management (Part B of the Migration Documentation) describes.

**Server-side variant.** For a web application whose code runs on a server:

- **Redirect.** The application receives the code at one exact HTTPS URL per environment, a route of the application, with no wildcards (NSerio recommendation; Relativity's matching rules are not documented). The Relativity administrator registers the exact URL on the OAuth2 Client.
- **Secret and tokens.** The client secret stays on the server and never reaches the browser. The user's tokens are held server-side, keyed to the session, and never in the browser.
- **Exchange.** The server performs the code exchange itself.

**Desktop variant.** For a desktop or command-line application run by a user on their own machine:

- **The system browser.** Open the authorize URL in the user's default browser and not in a web view the application hosts, so the application never sees the password (NSerio recommendation).
- **A loopback redirect on a fixed port.** Listen on the loopback address only, on the registered port, for one request. The Redirect URL is `http://127.0.0.1:<port>/<path>`: a plain-HTTP loopback URL is accepted, **verified by NSerio against a live instance; not documented by Relativity**. The Relativity administrator registers that exact URL; because it is registered in advance, the port is fixed and not chosen at runtime.
- **The secret is extractable.** Code requires a client secret (above), so a distributed desktop application has to embed one, and **a secret shipped inside a distributed binary can be extracted by anyone who holds the binary**. It protects nothing against the person running the application.
- **Compensating guidance** (NSerio recommendation, subject to your own security policy): set Trusted IPs where the users' outbound addresses are known and stable — they restrict where a token can be *requested*, and are checked when a token is issued against the client's ID and secret; use the recommended short Access Token Lifetime of 60 minutes; enable refresh tokens only when needed; and create one OAuth2 Client per distribution, so a leaked secret can be disabled without affecting the others.
- **A known issue.** Relativity documents no recommendation for a distributed desktop application; this design is what NSerio verified works, and Relativity's own recommendation is tracked as a known issue of the release.

Verification: **Verification note — four things, and the first two can break working code.**

**Verify equivalence before rewriting.** Not every RSAPI operation has a direct Object Manager counterpart. Establish that each operation this client performs has one before starting; where one does not, this is a redesign of that operation, not a migration of it.

**A token has a finite lifetime**, set per client in minutes. A service factory built once from the token and kept for the life of a process keeps it after it expires; rebuild it with a current token within that lifetime, which the RSAPI credential never required.

**Other RSAPI uses may not appear as findings.** This tool reports an RSAPI client only where it is constructed with a username-and-password credential; an RSAPI client obtained without one is not covered. Migrating this call does not establish that the codebase has no RSAPI left.

**Confirm the SDK version in use provides `BearerTokenCredentials` and the Object Manager service.** Where it does not, this is an SDK upgrade before it is a code change.

OAuth2 Client: Create or confirm this OAuth2 Client in RelativityOne (NSerio's observation: the menu path is Home > Authentication > OAuth2 Client Manager, as Relativity's REST API authentication page names it). This tool never connects to RelativityOne and never creates or changes the OAuth2 Client; the settings below are guidance.
- Name: a descriptive name identifying the application it serves.
- Flow Grant Type: Code
- Redirect URLs: Relativity documentation: each URL includes the http or https protocol prefix. The exact callback URL is registered by the Relativity administrator. Server-side web applications, NSerio's recommendation: one exact HTTPS URL per environment, with no wildcards. Desktop and CLI applications: a plain-HTTP loopback URL, `http://127.0.0.1:<port>/<path>`, on a fixed port because the URL is registered in advance and the port cannot be chosen at runtime (verified by NSerio against a live instance; not documented by Relativity).
- Access Token Lifetime: 60 minutes, stated in minutes because the field is in minutes. Relativity documentation: tokens for this flow must have a short lifetime, and the recommended value matches the Identity Server default.
- Allow refresh tokens: NSerio's recommendation: leave it off unless the application must keep a user session active beyond the access token lifetime without asking the user to sign in again. If it is on, the refresh tokens are stored as secrets, per Secret Management (Part B of the Migration Documentation).
- Enabled: on.
<!-- generated:end CS-002 authorization-code -->

### CS-003 · CredentialCacheBasicScheme

<a id="cs-003-migration"></a>

#### Client Credentials

<!-- generated:begin CS-003 client-credentials -->
**Confirming the flow.** When no signal in a file decides the flow, the tool assigns Client Credentials, the default, with this note: "Who supplies the credential at run time? If a person types it or signs in, the integration acts for that person: use Authorization Code. If it comes from configuration, settings, an environment variable or a secret store: use Client Credentials. No signal in this file showed which applies, so Client Credentials, the default, is recommended. Use Authorization Code instead if the integration acts on behalf of a signed-in user, or if your RelativityOne instance already has a Code-flow OAuth2 Client for this integration." A fixed-credential signal assigns it with this note: "A fixed-credential signal in this file points to Client Credentials, but how often that kind of signal is right has not been measured. Use Authorization Code instead if the integration acts on behalf of a signed-in user, or if your RelativityOne instance already has a Code-flow OAuth2 Client for this integration." See [Confirming the flow](#confirming-the-flow) and the "Which flow" paragraph of CS-003 in [Part A](part-a-csharp.md#cs-003).

Source form:
```csharp
cache.Add(new Uri(instanceUrl), "Basic", new NetworkCredential(userName, password));
```

Under token authentication: **First, the destination:** if it is not RelativityOne, this finding is informational — that service's own token mechanism is the migration — and the rest of this template does not apply.

Remove **this cache entry only** — the `Add` for this host with the `Basic` scheme. A `CredentialCache` usually holds several entries for several hosts, and the others stay. The requests to this host then carry the token, set on each request, in the form the consumer of the cache accepts.

Where the cache serves `HttpClient`, on each `HttpRequestMessage`:

```csharp
request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
```

Where it serves `HttpWebRequest` or `WebClient`, whose headers have no typed `Authorization` property:

```csharp
request.Headers[HttpRequestHeader.Authorization] = $"Bearer {accessToken}";   // HttpWebRequest
client.Headers[HttpRequestHeader.Authorization] = $"Bearer {accessToken}";    // WebClient, before each call
```

Credentials: **Where the credentials come from.** The destination decides whether the migration applies. If the destination is not RelativityOne, the migration is that service's own token mechanism, this finding is informational for this tool, and no migration to RelativityOne applies. If it is RelativityOne, the token comes from the token-acquisition part that follows.

**Where the token comes from.** Create an OAuth2 Client with Flow Grant Type set to Client Credentials (part 3 lists its settings), then send a form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=client_credentials`, `client_id`, `client_secret` and a `scope`. The token endpoint is **documented** for this grant. The token is then sent as `Authorization: Bearer <token>`, with `X-CSRF-Header: -` on REST and Kepler calls — the header is documented as required, and a Kepler call without it returns 404.

**The scope depends on what the integration needs, and no single scope is required.** Relativity's documentation shows two values for this grant: `SystemUserInfo` on the REST authentication page, the Usage Reports page and in the official Transfer SDK sample, and `UserInfoAccess` in the Kepler .NET samples. No page says which is correct, whether both work, or what either grants, and the OAuth2 Client's `Scopes` property is documented as one that cannot be set. Use the value the API you call documents, and confirm it.

**Cache the token, and re-acquire it on expiry — do not refresh.** Keep the token for its lifetime (part 3 recommends 60 minutes, Relativity's own wording) and request a new one with the client secret when it expires or a call is rejected for authentication. Client Credentials does not offer refresh tokens: **verified by NSerio against a live instance; not documented by Relativity** — the Allow Refresh Tokens control does not appear for this grant in the RelativityOne user interface. Relativity's own tables and text on refresh tokens conflict with each other, so re-acquisition with the secret is the advice that holds whichever way they are read.

**Long-running processes that use the Relativity SDK.** A `ServiceFactory` built once from a bearer token keeps that token after it expires. Rebuild the `ServiceFactory` and its proxies with a fresh token before expiry. This is the only advice consistent with the documentation: the package `Relativity.Kepler.Client.SDK` (documented) exposes a settable `Token` and an `HttpMessageHandler` overload but no token-provider overload, and no page documents renewing a token behind a factory.

**The client secret.** It is displayed once, at creation or reset. Store it as Secret Management (Part B of the Migration Documentation) describes; never in source, never in tracked configuration. A reset invalidates the previous secret immediately, so plan the cut-over before resetting it.

**Before deploying, confirm the calling identity.** Every call now runs as the OAuth2 Client's **Context User** — not as the user in the replaced credential, and for an import or a job as much as for a request. If that user has different workspace permissions or item-level security, the code returns fewer rows or `Unauthorized` in production with no change to its logic, and Relativity's audit attributes every call to that one account. Confirm the Context User's permissions match those of the previous identity. Relativity's REST API authentication page describes the Context User as usually a member of the Relativity Administrators group; the OAuth2 Client block (part 3) gives NSerio's recommendation, which differs.

**When the integration acts for a signed-in user.** Client Credentials is one service identity by construction. An integration that acts on behalf of a signed-in user is the Authorization Code case, and moving to it means a new OAuth2 Client, because the flow is fixed when a client is created. The finding's flow note says when to make that choice.

Verification: **Verification note — five things.**

**The 401-challenge response is lost.** A `CredentialCache` entry answers the server's challenge through the handler's credential path; a bearer header is sent up front and answers nothing. Code or a server that depended on that exchange behaves differently after the change.

**Redirects stop carrying the credential.** Cache credentials are re-applied to a redirect target; a manually set `Authorization` header is not. .NET documents that `HttpWebRequest` clears the `Authorization` header when it follows a redirect automatically, and the `HttpClient` handler drops it on redirect. Code that reached this host through a redirect starts returning 401 after the migration; address the final URI, or handle the redirect explicitly.

**Removing the wrong entry breaks another host.** The cache is keyed by URI and scheme. Remove only the entry for this host and `Basic`; an entry for another host or another scheme is not part of this finding.

**A token has a finite lifetime, and a header set once on a long-lived client cannot be refreshed.** Set it per request, not on a shared client's default headers, or the code works for the token's lifetime and then returns 401 until the process restarts.

**Confirm the endpoint in use is the RelativityOne REST API.** That is the API whose bearer token this template describes; an endpoint elsewhere reached from the same code may accept only Basic and reject the migrated call.

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
<!-- generated:end CS-003 client-credentials -->

#### Authorization Code

<!-- generated:begin CS-003 authorization-code -->
**Confirming the flow.** An interactive-user signal in a file assigns Authorization Code, with this note: "An interactive-user signal in this file points to Authorization Code. Confirm the flow: use Client Credentials instead if the integration runs with credentials fixed for the integration and no signed-in user." Your user profile can also make Authorization Code the default; see Changing the default in [Confirming the flow](#confirming-the-flow). See [Confirming the flow](#confirming-the-flow) and the "Which flow" paragraph of CS-003 in [Part A](part-a-csharp.md#cs-003).

Source form:
```csharp
cache.Add(new Uri(instanceUrl), "Basic", new NetworkCredential(userName, password));
```

Under token authentication: **First, the destination:** if it is not RelativityOne, this finding is informational — that service's own token mechanism is the migration — and the rest of this template does not apply.

Remove **this cache entry only** — the `Add` for this host with the `Basic` scheme. A `CredentialCache` usually holds several entries for several hosts, and the others stay. The requests to this host then carry the token, set on each request, in the form the consumer of the cache accepts.

Where the cache serves `HttpClient`, on each `HttpRequestMessage`:

```csharp
request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
```

Where it serves `HttpWebRequest` or `WebClient`, whose headers have no typed `Authorization` property:

```csharp
request.Headers[HttpRequestHeader.Authorization] = $"Bearer {accessToken}";   // HttpWebRequest
client.Headers[HttpRequestHeader.Authorization] = $"Bearer {accessToken}";    // WebClient, before each call
```

Credentials: **Where the credentials come from.** The destination decides whether the migration applies. If the destination is not RelativityOne, the migration is that service's own token mechanism, this finding is informational for this tool, and no migration to RelativityOne applies. If it is RelativityOne, the token comes from the token-acquisition part that follows.

**Common to both variants**

**Sign in.** Replace the code that collected the user's credentials with a redirect: send the user's browser to the authorize endpoint `<host>/Relativity/Identity/connect/authorize`, which leads to the RelativityOne login page. The authorize endpoint is **verified by NSerio against a live instance; not documented by Relativity**. The request carries the standard authorization-code parameters — `client_id`, `response_type=code`, the registered `redirect_uri`, a `scope`, and a `state` value the application checks on return; these are standard OAuth, and Relativity documents none of them. No discovery document is relied on.

**Receive the code.** The browser returns to the Redirect URL registered on the OAuth2 Client, with the authorization code and the `state` value. Check the `state` before using the code. The Redirect URL must be exactly the one registered (the variants say how).

**Exchange the code.** Send a form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=authorization_code`, the `code`, the same `redirect_uri`, the `client_id` and the `client_secret`, plus the `code_verifier` when PKCE is used. The code is exchanged at that endpoint, which Relativity documents for the Client Credentials grant only; the exchange is **verified by NSerio against a live instance; not documented by Relativity**.

**Use the token.** Send `Authorization: Bearer <access token>` — the change at the use site in part 1 — with `X-CSRF-Header: -` on REST and Kepler calls.

**On expiry.** With Allow Refresh Tokens enabled on the OAuth2 Client and PKCE used, the application holds a refresh token and, when the access token expires, sends a `grant_type=refresh_token` request with it and the client credentials in place of the code exchange — *inferred* from the standard OAuth shape, not tested. Without a refresh token the user signs in again. Relativity does not document a refresh token's lifetime, whether it rotates, how it is revoked, or whether a revocation endpoint exists, nor what disabling or deleting the client does to tokens already issued. A refresh token is a secret: store it as Secret Management (Part B of the Migration Documentation) describes, and on compromise disable the client and replace it. Enable refresh tokens only if the application must keep a session beyond one token lifetime without asking the user to sign in again.

**PKCE.** PKCE is required by Relativity's own documentation to obtain a refresh token, and not otherwise: a Code request that does not ask for a refresh token works without it, **verified by NSerio against a live instance; not documented by Relativity**. It is never a blanket requirement. NSerio recommends PKCE with the `S256` method in both variants (NSerio recommendation), and does not claim that Relativity accepts any particular method, which it does not document.

**Scope.** No scope set is prescribed for Code. The scope below works — `openid RelativityRequestOrigin UserInfo UserInfoAccess AuthenticationID RelativityWeb` — **verified by NSerio against a live instance; not documented by Relativity**, verified as working and not as minimal. `state` is sent as standard OAuth practice and `nonce` is not used; Relativity does not document whether it enforces either.

**The application stops collecting the password.** After this change the application never receives, stores or forwards the user's username and password: the user signs in on RelativityOne's own page, and the application holds only tokens. Remove the credential prompt, the form field and any storage of the password that the replaced code needed.

**Single sign-on.** Sign-in happens on RelativityOne's own login page, so the instance's single sign-on and two-factor rules apply unchanged — *inferred* from where the sign-in happens, and not documented for clients that customers create.

**The client secret is required.** Code does not work without a client secret: tested and rejected, **verified by NSerio against a live instance; not documented by Relativity**, which confirms Relativity's own statement that the grant requires one. It is displayed once and is stored as Secret Management (Part B of the Migration Documentation) describes.

**Server-side variant.** For a web application whose code runs on a server:

- **Redirect.** The application receives the code at one exact HTTPS URL per environment, a route of the application, with no wildcards (NSerio recommendation; Relativity's matching rules are not documented). The Relativity administrator registers the exact URL on the OAuth2 Client.
- **Secret and tokens.** The client secret stays on the server and never reaches the browser. The user's tokens are held server-side, keyed to the session, and never in the browser.
- **Exchange.** The server performs the code exchange itself.

**Desktop variant.** For a desktop or command-line application run by a user on their own machine:

- **The system browser.** Open the authorize URL in the user's default browser and not in a web view the application hosts, so the application never sees the password (NSerio recommendation).
- **A loopback redirect on a fixed port.** Listen on the loopback address only, on the registered port, for one request. The Redirect URL is `http://127.0.0.1:<port>/<path>`: a plain-HTTP loopback URL is accepted, **verified by NSerio against a live instance; not documented by Relativity**. The Relativity administrator registers that exact URL; because it is registered in advance, the port is fixed and not chosen at runtime.
- **The secret is extractable.** Code requires a client secret (above), so a distributed desktop application has to embed one, and **a secret shipped inside a distributed binary can be extracted by anyone who holds the binary**. It protects nothing against the person running the application.
- **Compensating guidance** (NSerio recommendation, subject to your own security policy): set Trusted IPs where the users' outbound addresses are known and stable — they restrict where a token can be *requested*, and are checked when a token is issued against the client's ID and secret; use the recommended short Access Token Lifetime of 60 minutes; enable refresh tokens only when needed; and create one OAuth2 Client per distribution, so a leaked secret can be disabled without affecting the others.
- **A known issue.** Relativity documents no recommendation for a distributed desktop application; this design is what NSerio verified works, and Relativity's own recommendation is tracked as a known issue of the release.

Verification: **Verification note — five things.**

**The 401-challenge response is lost.** A `CredentialCache` entry answers the server's challenge through the handler's credential path; a bearer header is sent up front and answers nothing. Code or a server that depended on that exchange behaves differently after the change.

**Redirects stop carrying the credential.** Cache credentials are re-applied to a redirect target; a manually set `Authorization` header is not. .NET documents that `HttpWebRequest` clears the `Authorization` header when it follows a redirect automatically, and the `HttpClient` handler drops it on redirect. Code that reached this host through a redirect starts returning 401 after the migration; address the final URI, or handle the redirect explicitly.

**Removing the wrong entry breaks another host.** The cache is keyed by URI and scheme. Remove only the entry for this host and `Basic`; an entry for another host or another scheme is not part of this finding.

**A token has a finite lifetime, and a header set once on a long-lived client cannot be refreshed.** Set it per request, not on a shared client's default headers, or the code works for the token's lifetime and then returns 401 until the process restarts.

**Confirm the endpoint in use is the RelativityOne REST API.** That is the API whose bearer token this template describes; an endpoint elsewhere reached from the same code may accept only Basic and reject the migrated call.

OAuth2 Client: Create or confirm this OAuth2 Client in RelativityOne (NSerio's observation: the menu path is Home > Authentication > OAuth2 Client Manager, as Relativity's REST API authentication page names it). This tool never connects to RelativityOne and never creates or changes the OAuth2 Client; the settings below are guidance.
- Name: a descriptive name identifying the application it serves.
- Flow Grant Type: Code
- Redirect URLs: Relativity documentation: each URL includes the http or https protocol prefix. The exact callback URL is registered by the Relativity administrator. Server-side web applications, NSerio's recommendation: one exact HTTPS URL per environment, with no wildcards. Desktop and CLI applications: a plain-HTTP loopback URL, `http://127.0.0.1:<port>/<path>`, on a fixed port because the URL is registered in advance and the port cannot be chosen at runtime (verified by NSerio against a live instance; not documented by Relativity).
- Access Token Lifetime: 60 minutes, stated in minutes because the field is in minutes. Relativity documentation: tokens for this flow must have a short lifetime, and the recommended value matches the Identity Server default.
- Allow refresh tokens: NSerio's recommendation: leave it off unless the application must keep a user session active beyond the access token lifetime without asking the user to sign in again. If it is on, the refresh tokens are stored as secrets, per Secret Management (Part B of the Migration Documentation).
- Enabled: on.
<!-- generated:end CS-003 authorization-code -->

### CS-004 · ThirdPartyBasicAuthenticatorType

<a id="cs-004-migration"></a>

#### Client Credentials

<!-- generated:begin CS-004 client-credentials -->
**Confirming the flow.** When no signal in a file decides the flow, the tool assigns Client Credentials, the default, with this note: "Who supplies the credential at run time? If a person types it or signs in, the integration acts for that person: use Authorization Code. If it comes from configuration, settings, an environment variable or a secret store: use Client Credentials. No signal in this file showed which applies, so Client Credentials, the default, is recommended. Use Authorization Code instead if the integration acts on behalf of a signed-in user, or if your RelativityOne instance already has a Code-flow OAuth2 Client for this integration." A fixed-credential signal assigns it with this note: "A fixed-credential signal in this file points to Client Credentials, but how often that kind of signal is right has not been measured. Use Authorization Code instead if the integration acts on behalf of a signed-in user, or if your RelativityOne instance already has a Code-flow OAuth2 Client for this integration." See [Confirming the flow](#confirming-the-flow) and the "Which flow" paragraph of CS-004 in [Part A](part-a-csharp.md#cs-004).

Source form:
```csharp
var authenticator = new HttpBasicAuthenticator(userName, password);
```

Under token authentication: **First, the destination:** if it is not RelativityOne, this finding is informational — that service's own token mechanism is the migration — and the rest of this template does not apply.

Replace the Basic authenticator with **the same library's bearer or token authenticator, carrying the access token** in place of the credential pair. This template names no target type: the construct occurs in more than one library, and the token authenticator's name differs between them and between versions.

Credentials: **Where the credentials come from.** The destination decides whether the migration applies. If the destination is not RelativityOne, the migration is that service's own token mechanism, this finding is informational for this tool, and no migration to RelativityOne applies. If it is RelativityOne, the token comes from the token-acquisition part that follows.

**Where the token comes from.** Create an OAuth2 Client with Flow Grant Type set to Client Credentials (part 3 lists its settings), then send a form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=client_credentials`, `client_id`, `client_secret` and a `scope`. The token endpoint is **documented** for this grant. The token is then sent as `Authorization: Bearer <token>`, with `X-CSRF-Header: -` on REST and Kepler calls — the header is documented as required, and a Kepler call without it returns 404.

**The scope depends on what the integration needs, and no single scope is required.** Relativity's documentation shows two values for this grant: `SystemUserInfo` on the REST authentication page, the Usage Reports page and in the official Transfer SDK sample, and `UserInfoAccess` in the Kepler .NET samples. No page says which is correct, whether both work, or what either grants, and the OAuth2 Client's `Scopes` property is documented as one that cannot be set. Use the value the API you call documents, and confirm it.

**Cache the token, and re-acquire it on expiry — do not refresh.** Keep the token for its lifetime (part 3 recommends 60 minutes, Relativity's own wording) and request a new one with the client secret when it expires or a call is rejected for authentication. Client Credentials does not offer refresh tokens: **verified by NSerio against a live instance; not documented by Relativity** — the Allow Refresh Tokens control does not appear for this grant in the RelativityOne user interface. Relativity's own tables and text on refresh tokens conflict with each other, so re-acquisition with the secret is the advice that holds whichever way they are read.

**Long-running processes that use the Relativity SDK.** A `ServiceFactory` built once from a bearer token keeps that token after it expires. Rebuild the `ServiceFactory` and its proxies with a fresh token before expiry. This is the only advice consistent with the documentation: the package `Relativity.Kepler.Client.SDK` (documented) exposes a settable `Token` and an `HttpMessageHandler` overload but no token-provider overload, and no page documents renewing a token behind a factory.

**The client secret.** It is displayed once, at creation or reset. Store it as Secret Management (Part B of the Migration Documentation) describes; never in source, never in tracked configuration. A reset invalidates the previous secret immediately, so plan the cut-over before resetting it.

**Before deploying, confirm the calling identity.** Every call now runs as the OAuth2 Client's **Context User** — not as the user in the replaced credential, and for an import or a job as much as for a request. If that user has different workspace permissions or item-level security, the code returns fewer rows or `Unauthorized` in production with no change to its logic, and Relativity's audit attributes every call to that one account. Confirm the Context User's permissions match those of the previous identity. Relativity's REST API authentication page describes the Context User as usually a member of the Relativity Administrators group; the OAuth2 Client block (part 3) gives NSerio's recommendation, which differs.

**When the integration acts for a signed-in user.** Client Credentials is one service identity by construction. An integration that acts on behalf of a signed-in user is the Authorization Code case, and moving to it means a new OAuth2 Client, because the flow is fixed when a client is created. The finding's flow note says when to make that choice.

Verification: **Verification note — three things.**

**Confirm the library version in use offers a token authenticator.** Where it does not, this is a library upgrade before it is a code change, with whatever that upgrade carries.

**A token has a finite lifetime.** An authenticator constructed once with a fixed token string and attached to a long-lived client holds that token after it expires. Whether the library can refresh it is the library's behaviour and is not established here; where it cannot, construct the authenticator with a current token for each request or each short-lived client.

**Confirm the endpoint in use is the RelativityOne REST API.** The authenticator changes the scheme on the wire, and an endpoint elsewhere reached from the same code may accept only Basic and reject the migrated call.

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
<!-- generated:end CS-004 client-credentials -->

#### Authorization Code

<!-- generated:begin CS-004 authorization-code -->
**Confirming the flow.** An interactive-user signal in a file assigns Authorization Code, with this note: "An interactive-user signal in this file points to Authorization Code. Confirm the flow: use Client Credentials instead if the integration runs with credentials fixed for the integration and no signed-in user." Your user profile can also make Authorization Code the default; see Changing the default in [Confirming the flow](#confirming-the-flow). See [Confirming the flow](#confirming-the-flow) and the "Which flow" paragraph of CS-004 in [Part A](part-a-csharp.md#cs-004).

Source form:
```csharp
var authenticator = new HttpBasicAuthenticator(userName, password);
```

Under token authentication: **First, the destination:** if it is not RelativityOne, this finding is informational — that service's own token mechanism is the migration — and the rest of this template does not apply.

Replace the Basic authenticator with **the same library's bearer or token authenticator, carrying the access token** in place of the credential pair. This template names no target type: the construct occurs in more than one library, and the token authenticator's name differs between them and between versions.

Credentials: **Where the credentials come from.** The destination decides whether the migration applies. If the destination is not RelativityOne, the migration is that service's own token mechanism, this finding is informational for this tool, and no migration to RelativityOne applies. If it is RelativityOne, the token comes from the token-acquisition part that follows.

**Common to both variants**

**Sign in.** Replace the code that collected the user's credentials with a redirect: send the user's browser to the authorize endpoint `<host>/Relativity/Identity/connect/authorize`, which leads to the RelativityOne login page. The authorize endpoint is **verified by NSerio against a live instance; not documented by Relativity**. The request carries the standard authorization-code parameters — `client_id`, `response_type=code`, the registered `redirect_uri`, a `scope`, and a `state` value the application checks on return; these are standard OAuth, and Relativity documents none of them. No discovery document is relied on.

**Receive the code.** The browser returns to the Redirect URL registered on the OAuth2 Client, with the authorization code and the `state` value. Check the `state` before using the code. The Redirect URL must be exactly the one registered (the variants say how).

**Exchange the code.** Send a form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=authorization_code`, the `code`, the same `redirect_uri`, the `client_id` and the `client_secret`, plus the `code_verifier` when PKCE is used. The code is exchanged at that endpoint, which Relativity documents for the Client Credentials grant only; the exchange is **verified by NSerio against a live instance; not documented by Relativity**.

**Use the token.** Send `Authorization: Bearer <access token>` — the change at the use site in part 1 — with `X-CSRF-Header: -` on REST and Kepler calls.

**On expiry.** With Allow Refresh Tokens enabled on the OAuth2 Client and PKCE used, the application holds a refresh token and, when the access token expires, sends a `grant_type=refresh_token` request with it and the client credentials in place of the code exchange — *inferred* from the standard OAuth shape, not tested. Without a refresh token the user signs in again. Relativity does not document a refresh token's lifetime, whether it rotates, how it is revoked, or whether a revocation endpoint exists, nor what disabling or deleting the client does to tokens already issued. A refresh token is a secret: store it as Secret Management (Part B of the Migration Documentation) describes, and on compromise disable the client and replace it. Enable refresh tokens only if the application must keep a session beyond one token lifetime without asking the user to sign in again.

**PKCE.** PKCE is required by Relativity's own documentation to obtain a refresh token, and not otherwise: a Code request that does not ask for a refresh token works without it, **verified by NSerio against a live instance; not documented by Relativity**. It is never a blanket requirement. NSerio recommends PKCE with the `S256` method in both variants (NSerio recommendation), and does not claim that Relativity accepts any particular method, which it does not document.

**Scope.** No scope set is prescribed for Code. The scope below works — `openid RelativityRequestOrigin UserInfo UserInfoAccess AuthenticationID RelativityWeb` — **verified by NSerio against a live instance; not documented by Relativity**, verified as working and not as minimal. `state` is sent as standard OAuth practice and `nonce` is not used; Relativity does not document whether it enforces either.

**The application stops collecting the password.** After this change the application never receives, stores or forwards the user's username and password: the user signs in on RelativityOne's own page, and the application holds only tokens. Remove the credential prompt, the form field and any storage of the password that the replaced code needed.

**Single sign-on.** Sign-in happens on RelativityOne's own login page, so the instance's single sign-on and two-factor rules apply unchanged — *inferred* from where the sign-in happens, and not documented for clients that customers create.

**The client secret is required.** Code does not work without a client secret: tested and rejected, **verified by NSerio against a live instance; not documented by Relativity**, which confirms Relativity's own statement that the grant requires one. It is displayed once and is stored as Secret Management (Part B of the Migration Documentation) describes.

**Server-side variant.** For a web application whose code runs on a server:

- **Redirect.** The application receives the code at one exact HTTPS URL per environment, a route of the application, with no wildcards (NSerio recommendation; Relativity's matching rules are not documented). The Relativity administrator registers the exact URL on the OAuth2 Client.
- **Secret and tokens.** The client secret stays on the server and never reaches the browser. The user's tokens are held server-side, keyed to the session, and never in the browser.
- **Exchange.** The server performs the code exchange itself.

**Desktop variant.** For a desktop or command-line application run by a user on their own machine:

- **The system browser.** Open the authorize URL in the user's default browser and not in a web view the application hosts, so the application never sees the password (NSerio recommendation).
- **A loopback redirect on a fixed port.** Listen on the loopback address only, on the registered port, for one request. The Redirect URL is `http://127.0.0.1:<port>/<path>`: a plain-HTTP loopback URL is accepted, **verified by NSerio against a live instance; not documented by Relativity**. The Relativity administrator registers that exact URL; because it is registered in advance, the port is fixed and not chosen at runtime.
- **The secret is extractable.** Code requires a client secret (above), so a distributed desktop application has to embed one, and **a secret shipped inside a distributed binary can be extracted by anyone who holds the binary**. It protects nothing against the person running the application.
- **Compensating guidance** (NSerio recommendation, subject to your own security policy): set Trusted IPs where the users' outbound addresses are known and stable — they restrict where a token can be *requested*, and are checked when a token is issued against the client's ID and secret; use the recommended short Access Token Lifetime of 60 minutes; enable refresh tokens only when needed; and create one OAuth2 Client per distribution, so a leaked secret can be disabled without affecting the others.
- **A known issue.** Relativity documents no recommendation for a distributed desktop application; this design is what NSerio verified works, and Relativity's own recommendation is tracked as a known issue of the release.

Verification: **Verification note — three things.**

**Confirm the library version in use offers a token authenticator.** Where it does not, this is a library upgrade before it is a code change, with whatever that upgrade carries.

**A token has a finite lifetime.** An authenticator constructed once with a fixed token string and attached to a long-lived client holds that token after it expires. Whether the library can refresh it is the library's behaviour and is not established here; where it cannot, construct the authenticator with a current token for each request or each short-lived client.

**Confirm the endpoint in use is the RelativityOne REST API.** The authenticator changes the scheme on the wire, and an endpoint elsewhere reached from the same code may accept only Basic and reject the migrated call.

OAuth2 Client: Create or confirm this OAuth2 Client in RelativityOne (NSerio's observation: the menu path is Home > Authentication > OAuth2 Client Manager, as Relativity's REST API authentication page names it). This tool never connects to RelativityOne and never creates or changes the OAuth2 Client; the settings below are guidance.
- Name: a descriptive name identifying the application it serves.
- Flow Grant Type: Code
- Redirect URLs: Relativity documentation: each URL includes the http or https protocol prefix. The exact callback URL is registered by the Relativity administrator. Server-side web applications, NSerio's recommendation: one exact HTTPS URL per environment, with no wildcards. Desktop and CLI applications: a plain-HTTP loopback URL, `http://127.0.0.1:<port>/<path>`, on a fixed port because the URL is registered in advance and the port cannot be chosen at runtime (verified by NSerio against a live instance; not documented by Relativity).
- Access Token Lifetime: 60 minutes, stated in minutes because the field is in minutes. Relativity documentation: tokens for this flow must have a short lifetime, and the recommended value matches the Identity Server default.
- Allow refresh tokens: NSerio's recommendation: leave it off unless the application must keep a user session active beyond the access token lifetime without asking the user to sign in again. If it is on, the refresh tokens are stored as secrets, per Secret Management (Part B of the Migration Documentation).
- Enabled: on.
<!-- generated:end CS-004 authorization-code -->

### CS-005 · AuthorizationHeaderValueScheme

<a id="cs-005-migration"></a>

#### Client Credentials

<!-- generated:begin CS-005 client-credentials -->
**Confirming the flow.** When no signal in a file decides the flow, the tool assigns Client Credentials, the default, with this note: "Who supplies the credential at run time? If a person types it or signs in, the integration acts for that person: use Authorization Code. If it comes from configuration, settings, an environment variable or a secret store: use Client Credentials. No signal in this file showed which applies, so Client Credentials, the default, is recommended. Use Authorization Code instead if the integration acts on behalf of a signed-in user, or if your RelativityOne instance already has a Code-flow OAuth2 Client for this integration." A fixed-credential signal assigns it with this note: "A fixed-credential signal in this file points to Client Credentials, but how often that kind of signal is right has not been measured. Use Authorization Code instead if the integration acts on behalf of a signed-in user, or if your RelativityOne instance already has a Code-flow OAuth2 Client for this integration." See [Confirming the flow](#confirming-the-flow) and the "Which flow" paragraph of CS-005 in [Part A](part-a-csharp.md#cs-005).

Source form:
```csharp
client.DefaultRequestHeaders.Authorization =
	new AuthenticationHeaderValue(BASIC_SCHEME, encodedCredentials);
```

Under token authentication: **First, the destination:** if it is not RelativityOne, this finding is informational — that service's own token mechanism is the migration — and the rest of this template does not apply.

`"Bearer"` and the access token in place of the scheme constant and the encoded pair, **set per request** on the request message rather than on the long-lived client's default headers:

```csharp
request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
```

A per-request header needs an `HttpRequestMessage` sent with `SendAsync`; the `GetAsync` and `PostAsync` convenience calls take no request message, so calls written with them change shape. The code that produced `encodedCredentials` has no counterpart: a bearer token travels verbatim.

Credentials: **Where the credentials come from.** The destination decides whether the migration applies. If the destination is not RelativityOne, the migration is that service's own token mechanism, this finding is informational for this tool, and no migration to RelativityOne applies. If it is RelativityOne, the token comes from the token-acquisition part that follows.

**Where the token comes from.** Create an OAuth2 Client with Flow Grant Type set to Client Credentials (part 3 lists its settings), then send a form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=client_credentials`, `client_id`, `client_secret` and a `scope`. The token endpoint is **documented** for this grant. The token is then sent as `Authorization: Bearer <token>`, with `X-CSRF-Header: -` on REST and Kepler calls — the header is documented as required, and a Kepler call without it returns 404.

**The scope depends on what the integration needs, and no single scope is required.** Relativity's documentation shows two values for this grant: `SystemUserInfo` on the REST authentication page, the Usage Reports page and in the official Transfer SDK sample, and `UserInfoAccess` in the Kepler .NET samples. No page says which is correct, whether both work, or what either grants, and the OAuth2 Client's `Scopes` property is documented as one that cannot be set. Use the value the API you call documents, and confirm it.

**Cache the token, and re-acquire it on expiry — do not refresh.** Keep the token for its lifetime (part 3 recommends 60 minutes, Relativity's own wording) and request a new one with the client secret when it expires or a call is rejected for authentication. Client Credentials does not offer refresh tokens: **verified by NSerio against a live instance; not documented by Relativity** — the Allow Refresh Tokens control does not appear for this grant in the RelativityOne user interface. Relativity's own tables and text on refresh tokens conflict with each other, so re-acquisition with the secret is the advice that holds whichever way they are read.

**Long-running processes that use the Relativity SDK.** A `ServiceFactory` built once from a bearer token keeps that token after it expires. Rebuild the `ServiceFactory` and its proxies with a fresh token before expiry. This is the only advice consistent with the documentation: the package `Relativity.Kepler.Client.SDK` (documented) exposes a settable `Token` and an `HttpMessageHandler` overload but no token-provider overload, and no page documents renewing a token behind a factory.

**The client secret.** It is displayed once, at creation or reset. Store it as Secret Management (Part B of the Migration Documentation) describes; never in source, never in tracked configuration. A reset invalidates the previous secret immediately, so plan the cut-over before resetting it.

**Before deploying, confirm the calling identity.** Every call now runs as the OAuth2 Client's **Context User** — not as the user in the replaced credential, and for an import or a job as much as for a request. If that user has different workspace permissions or item-level security, the code returns fewer rows or `Unauthorized` in production with no change to its logic, and Relativity's audit attributes every call to that one account. Confirm the Context User's permissions match those of the previous identity. Relativity's REST API authentication page describes the Context User as usually a member of the Relativity Administrators group; the OAuth2 Client block (part 3) gives NSerio's recommendation, which differs.

**When the integration acts for a signed-in user.** Client Credentials is one service identity by construction. An integration that acts on behalf of a signed-in user is the Authorization Code case, and moving to it means a new OAuth2 Client, because the flow is fixed when a client is created. The finding's flow note says when to make that choice.

Verification: **Verification note — three things.**

**Moving the header from the client to the request is part of the migration, not a style choice.** A token has a finite lifetime, and a header set once on `DefaultRequestHeaders` cannot be refreshed without mutating shared state; the code works for the token's lifetime and then returns 401 until the process restarts.

**The pair's encoding may be a separate finding.** Where `encodedCredentials` is built in the same file, the tool reports the encoding and this header as two findings at two locations. They are one migration, and both locations change together.

**Confirm the endpoint in use is the RelativityOne REST API.** That is the API whose bearer token this template describes; an endpoint elsewhere reached from the same code may accept only Basic and reject the migrated call.

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
<!-- generated:end CS-005 client-credentials -->

#### Authorization Code

<!-- generated:begin CS-005 authorization-code -->
**Confirming the flow.** An interactive-user signal in a file assigns Authorization Code, with this note: "An interactive-user signal in this file points to Authorization Code. Confirm the flow: use Client Credentials instead if the integration runs with credentials fixed for the integration and no signed-in user." Your user profile can also make Authorization Code the default; see Changing the default in [Confirming the flow](#confirming-the-flow). See [Confirming the flow](#confirming-the-flow) and the "Which flow" paragraph of CS-005 in [Part A](part-a-csharp.md#cs-005).

Source form:
```csharp
client.DefaultRequestHeaders.Authorization =
	new AuthenticationHeaderValue(BASIC_SCHEME, encodedCredentials);
```

Under token authentication: **First, the destination:** if it is not RelativityOne, this finding is informational — that service's own token mechanism is the migration — and the rest of this template does not apply.

`"Bearer"` and the access token in place of the scheme constant and the encoded pair, **set per request** on the request message rather than on the long-lived client's default headers:

```csharp
request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
```

A per-request header needs an `HttpRequestMessage` sent with `SendAsync`; the `GetAsync` and `PostAsync` convenience calls take no request message, so calls written with them change shape. The code that produced `encodedCredentials` has no counterpart: a bearer token travels verbatim.

Credentials: **Where the credentials come from.** The destination decides whether the migration applies. If the destination is not RelativityOne, the migration is that service's own token mechanism, this finding is informational for this tool, and no migration to RelativityOne applies. If it is RelativityOne, the token comes from the token-acquisition part that follows.

**Common to both variants**

**Sign in.** Replace the code that collected the user's credentials with a redirect: send the user's browser to the authorize endpoint `<host>/Relativity/Identity/connect/authorize`, which leads to the RelativityOne login page. The authorize endpoint is **verified by NSerio against a live instance; not documented by Relativity**. The request carries the standard authorization-code parameters — `client_id`, `response_type=code`, the registered `redirect_uri`, a `scope`, and a `state` value the application checks on return; these are standard OAuth, and Relativity documents none of them. No discovery document is relied on.

**Receive the code.** The browser returns to the Redirect URL registered on the OAuth2 Client, with the authorization code and the `state` value. Check the `state` before using the code. The Redirect URL must be exactly the one registered (the variants say how).

**Exchange the code.** Send a form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=authorization_code`, the `code`, the same `redirect_uri`, the `client_id` and the `client_secret`, plus the `code_verifier` when PKCE is used. The code is exchanged at that endpoint, which Relativity documents for the Client Credentials grant only; the exchange is **verified by NSerio against a live instance; not documented by Relativity**.

**Use the token.** Send `Authorization: Bearer <access token>` — the change at the use site in part 1 — with `X-CSRF-Header: -` on REST and Kepler calls.

**On expiry.** With Allow Refresh Tokens enabled on the OAuth2 Client and PKCE used, the application holds a refresh token and, when the access token expires, sends a `grant_type=refresh_token` request with it and the client credentials in place of the code exchange — *inferred* from the standard OAuth shape, not tested. Without a refresh token the user signs in again. Relativity does not document a refresh token's lifetime, whether it rotates, how it is revoked, or whether a revocation endpoint exists, nor what disabling or deleting the client does to tokens already issued. A refresh token is a secret: store it as Secret Management (Part B of the Migration Documentation) describes, and on compromise disable the client and replace it. Enable refresh tokens only if the application must keep a session beyond one token lifetime without asking the user to sign in again.

**PKCE.** PKCE is required by Relativity's own documentation to obtain a refresh token, and not otherwise: a Code request that does not ask for a refresh token works without it, **verified by NSerio against a live instance; not documented by Relativity**. It is never a blanket requirement. NSerio recommends PKCE with the `S256` method in both variants (NSerio recommendation), and does not claim that Relativity accepts any particular method, which it does not document.

**Scope.** No scope set is prescribed for Code. The scope below works — `openid RelativityRequestOrigin UserInfo UserInfoAccess AuthenticationID RelativityWeb` — **verified by NSerio against a live instance; not documented by Relativity**, verified as working and not as minimal. `state` is sent as standard OAuth practice and `nonce` is not used; Relativity does not document whether it enforces either.

**The application stops collecting the password.** After this change the application never receives, stores or forwards the user's username and password: the user signs in on RelativityOne's own page, and the application holds only tokens. Remove the credential prompt, the form field and any storage of the password that the replaced code needed.

**Single sign-on.** Sign-in happens on RelativityOne's own login page, so the instance's single sign-on and two-factor rules apply unchanged — *inferred* from where the sign-in happens, and not documented for clients that customers create.

**The client secret is required.** Code does not work without a client secret: tested and rejected, **verified by NSerio against a live instance; not documented by Relativity**, which confirms Relativity's own statement that the grant requires one. It is displayed once and is stored as Secret Management (Part B of the Migration Documentation) describes.

**Server-side variant.** For a web application whose code runs on a server:

- **Redirect.** The application receives the code at one exact HTTPS URL per environment, a route of the application, with no wildcards (NSerio recommendation; Relativity's matching rules are not documented). The Relativity administrator registers the exact URL on the OAuth2 Client.
- **Secret and tokens.** The client secret stays on the server and never reaches the browser. The user's tokens are held server-side, keyed to the session, and never in the browser.
- **Exchange.** The server performs the code exchange itself.

**Desktop variant.** For a desktop or command-line application run by a user on their own machine:

- **The system browser.** Open the authorize URL in the user's default browser and not in a web view the application hosts, so the application never sees the password (NSerio recommendation).
- **A loopback redirect on a fixed port.** Listen on the loopback address only, on the registered port, for one request. The Redirect URL is `http://127.0.0.1:<port>/<path>`: a plain-HTTP loopback URL is accepted, **verified by NSerio against a live instance; not documented by Relativity**. The Relativity administrator registers that exact URL; because it is registered in advance, the port is fixed and not chosen at runtime.
- **The secret is extractable.** Code requires a client secret (above), so a distributed desktop application has to embed one, and **a secret shipped inside a distributed binary can be extracted by anyone who holds the binary**. It protects nothing against the person running the application.
- **Compensating guidance** (NSerio recommendation, subject to your own security policy): set Trusted IPs where the users' outbound addresses are known and stable — they restrict where a token can be *requested*, and are checked when a token is issued against the client's ID and secret; use the recommended short Access Token Lifetime of 60 minutes; enable refresh tokens only when needed; and create one OAuth2 Client per distribution, so a leaked secret can be disabled without affecting the others.
- **A known issue.** Relativity documents no recommendation for a distributed desktop application; this design is what NSerio verified works, and Relativity's own recommendation is tracked as a known issue of the release.

Verification: **Verification note — three things.**

**Moving the header from the client to the request is part of the migration, not a style choice.** A token has a finite lifetime, and a header set once on `DefaultRequestHeaders` cannot be refreshed without mutating shared state; the code works for the token's lifetime and then returns 401 until the process restarts.

**The pair's encoding may be a separate finding.** Where `encodedCredentials` is built in the same file, the tool reports the encoding and this header as two findings at two locations. They are one migration, and both locations change together.

**Confirm the endpoint in use is the RelativityOne REST API.** That is the API whose bearer token this template describes; an endpoint elsewhere reached from the same code may accept only Basic and reject the migrated call.

OAuth2 Client: Create or confirm this OAuth2 Client in RelativityOne (NSerio's observation: the menu path is Home > Authentication > OAuth2 Client Manager, as Relativity's REST API authentication page names it). This tool never connects to RelativityOne and never creates or changes the OAuth2 Client; the settings below are guidance.
- Name: a descriptive name identifying the application it serves.
- Flow Grant Type: Code
- Redirect URLs: Relativity documentation: each URL includes the http or https protocol prefix. The exact callback URL is registered by the Relativity administrator. Server-side web applications, NSerio's recommendation: one exact HTTPS URL per environment, with no wildcards. Desktop and CLI applications: a plain-HTTP loopback URL, `http://127.0.0.1:<port>/<path>`, on a fixed port because the URL is registered in advance and the port cannot be chosen at runtime (verified by NSerio against a live instance; not documented by Relativity).
- Access Token Lifetime: 60 minutes, stated in minutes because the field is in minutes. Relativity documentation: tokens for this flow must have a short lifetime, and the recommended value matches the Identity Server default.
- Allow refresh tokens: NSerio's recommendation: leave it off unless the application must keep a user session active beyond the access token lifetime without asking the user to sign in again. If it is on, the refresh tokens are stored as secrets, per Secret Management (Part B of the Migration Documentation).
- Enabled: on.
<!-- generated:end CS-005 authorization-code -->

### CS-006 · AuthorizationHeaderStringLiteral

<a id="cs-006-migration"></a>

#### Client Credentials

<!-- generated:begin CS-006 client-credentials -->
**Confirming the flow.** When no signal in a file decides the flow, the tool assigns Client Credentials, the default, with this note: "Who supplies the credential at run time? If a person types it or signs in, the integration acts for that person: use Authorization Code. If it comes from configuration, settings, an environment variable or a secret store: use Client Credentials. No signal in this file showed which applies, so Client Credentials, the default, is recommended. Use Authorization Code instead if the integration acts on behalf of a signed-in user, or if your RelativityOne instance already has a Code-flow OAuth2 Client for this integration." A fixed-credential signal assigns it with this note: "A fixed-credential signal in this file points to Client Credentials, but how often that kind of signal is right has not been measured. Use Authorization Code instead if the integration acts on behalf of a signed-in user, or if your RelativityOne instance already has a Code-flow OAuth2 Client for this integration." See [Confirming the flow](#confirming-the-flow) and the "Which flow" paragraph of CS-006 in [Part A](part-a-csharp.md#cs-006).

Source form:
```csharp
client.DefaultRequestHeaders.Add("Authorization", $"Basic {encodedCredentials}");
```

Under token authentication: **First, the destination:** if it is not RelativityOne, this finding is informational — that service's own token mechanism is the migration — and the rest of this template does not apply.

`Bearer` and the access token in place of `Basic` and the encoded pair, **set per request** rather than on the long-lived client's default headers:

```csharp
request.Headers.Add("Authorization", $"Bearer {accessToken}");
```

A per-request header needs an `HttpRequestMessage` sent with `SendAsync`; the `GetAsync` and `PostAsync` convenience calls take no request message, so calls written with them change shape. The code that produced `encodedCredentials` has no counterpart: a bearer token travels verbatim.

Credentials: **Where the credentials come from.** The destination decides whether the migration applies. If the destination is not RelativityOne, the migration is that service's own token mechanism, this finding is informational for this tool, and no migration to RelativityOne applies. If it is RelativityOne, the token comes from the token-acquisition part that follows.

**Where the token comes from.** Create an OAuth2 Client with Flow Grant Type set to Client Credentials (part 3 lists its settings), then send a form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=client_credentials`, `client_id`, `client_secret` and a `scope`. The token endpoint is **documented** for this grant. The token is then sent as `Authorization: Bearer <token>`, with `X-CSRF-Header: -` on REST and Kepler calls — the header is documented as required, and a Kepler call without it returns 404.

**The scope depends on what the integration needs, and no single scope is required.** Relativity's documentation shows two values for this grant: `SystemUserInfo` on the REST authentication page, the Usage Reports page and in the official Transfer SDK sample, and `UserInfoAccess` in the Kepler .NET samples. No page says which is correct, whether both work, or what either grants, and the OAuth2 Client's `Scopes` property is documented as one that cannot be set. Use the value the API you call documents, and confirm it.

**Cache the token, and re-acquire it on expiry — do not refresh.** Keep the token for its lifetime (part 3 recommends 60 minutes, Relativity's own wording) and request a new one with the client secret when it expires or a call is rejected for authentication. Client Credentials does not offer refresh tokens: **verified by NSerio against a live instance; not documented by Relativity** — the Allow Refresh Tokens control does not appear for this grant in the RelativityOne user interface. Relativity's own tables and text on refresh tokens conflict with each other, so re-acquisition with the secret is the advice that holds whichever way they are read.

**Long-running processes that use the Relativity SDK.** A `ServiceFactory` built once from a bearer token keeps that token after it expires. Rebuild the `ServiceFactory` and its proxies with a fresh token before expiry. This is the only advice consistent with the documentation: the package `Relativity.Kepler.Client.SDK` (documented) exposes a settable `Token` and an `HttpMessageHandler` overload but no token-provider overload, and no page documents renewing a token behind a factory.

**The client secret.** It is displayed once, at creation or reset. Store it as Secret Management (Part B of the Migration Documentation) describes; never in source, never in tracked configuration. A reset invalidates the previous secret immediately, so plan the cut-over before resetting it.

**Before deploying, confirm the calling identity.** Every call now runs as the OAuth2 Client's **Context User** — not as the user in the replaced credential, and for an import or a job as much as for a request. If that user has different workspace permissions or item-level security, the code returns fewer rows or `Unauthorized` in production with no change to its logic, and Relativity's audit attributes every call to that one account. Confirm the Context User's permissions match those of the previous identity. Relativity's REST API authentication page describes the Context User as usually a member of the Relativity Administrators group; the OAuth2 Client block (part 3) gives NSerio's recommendation, which differs.

**When the integration acts for a signed-in user.** Client Credentials is one service identity by construction. An integration that acts on behalf of a signed-in user is the Authorization Code case, and moving to it means a new OAuth2 Client, because the flow is fixed when a client is created. The finding's flow note says when to make that choice.

Verification: **Verification note — three things.**

**Moving the header from the client to the request is part of the migration, not a style choice.** A token has a finite lifetime, and a header added once to `DefaultRequestHeaders` cannot be refreshed without mutating shared state; the code works for the token's lifetime and then returns 401 until the process restarts.

**The pair's encoding may be a separate finding.** Where `encodedCredentials` is built in the same file, the tool reports the encoding and this header as two findings at two locations. They are one migration, and both locations change together.

**Confirm the endpoint in use is the RelativityOne REST API.** That is the API whose bearer token this template describes; an endpoint elsewhere reached from the same code may accept only Basic and reject the migrated call.

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
<!-- generated:end CS-006 client-credentials -->

#### Authorization Code

<!-- generated:begin CS-006 authorization-code -->
**Confirming the flow.** An interactive-user signal in a file assigns Authorization Code, with this note: "An interactive-user signal in this file points to Authorization Code. Confirm the flow: use Client Credentials instead if the integration runs with credentials fixed for the integration and no signed-in user." Your user profile can also make Authorization Code the default; see Changing the default in [Confirming the flow](#confirming-the-flow). See [Confirming the flow](#confirming-the-flow) and the "Which flow" paragraph of CS-006 in [Part A](part-a-csharp.md#cs-006).

Source form:
```csharp
client.DefaultRequestHeaders.Add("Authorization", $"Basic {encodedCredentials}");
```

Under token authentication: **First, the destination:** if it is not RelativityOne, this finding is informational — that service's own token mechanism is the migration — and the rest of this template does not apply.

`Bearer` and the access token in place of `Basic` and the encoded pair, **set per request** rather than on the long-lived client's default headers:

```csharp
request.Headers.Add("Authorization", $"Bearer {accessToken}");
```

A per-request header needs an `HttpRequestMessage` sent with `SendAsync`; the `GetAsync` and `PostAsync` convenience calls take no request message, so calls written with them change shape. The code that produced `encodedCredentials` has no counterpart: a bearer token travels verbatim.

Credentials: **Where the credentials come from.** The destination decides whether the migration applies. If the destination is not RelativityOne, the migration is that service's own token mechanism, this finding is informational for this tool, and no migration to RelativityOne applies. If it is RelativityOne, the token comes from the token-acquisition part that follows.

**Common to both variants**

**Sign in.** Replace the code that collected the user's credentials with a redirect: send the user's browser to the authorize endpoint `<host>/Relativity/Identity/connect/authorize`, which leads to the RelativityOne login page. The authorize endpoint is **verified by NSerio against a live instance; not documented by Relativity**. The request carries the standard authorization-code parameters — `client_id`, `response_type=code`, the registered `redirect_uri`, a `scope`, and a `state` value the application checks on return; these are standard OAuth, and Relativity documents none of them. No discovery document is relied on.

**Receive the code.** The browser returns to the Redirect URL registered on the OAuth2 Client, with the authorization code and the `state` value. Check the `state` before using the code. The Redirect URL must be exactly the one registered (the variants say how).

**Exchange the code.** Send a form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=authorization_code`, the `code`, the same `redirect_uri`, the `client_id` and the `client_secret`, plus the `code_verifier` when PKCE is used. The code is exchanged at that endpoint, which Relativity documents for the Client Credentials grant only; the exchange is **verified by NSerio against a live instance; not documented by Relativity**.

**Use the token.** Send `Authorization: Bearer <access token>` — the change at the use site in part 1 — with `X-CSRF-Header: -` on REST and Kepler calls.

**On expiry.** With Allow Refresh Tokens enabled on the OAuth2 Client and PKCE used, the application holds a refresh token and, when the access token expires, sends a `grant_type=refresh_token` request with it and the client credentials in place of the code exchange — *inferred* from the standard OAuth shape, not tested. Without a refresh token the user signs in again. Relativity does not document a refresh token's lifetime, whether it rotates, how it is revoked, or whether a revocation endpoint exists, nor what disabling or deleting the client does to tokens already issued. A refresh token is a secret: store it as Secret Management (Part B of the Migration Documentation) describes, and on compromise disable the client and replace it. Enable refresh tokens only if the application must keep a session beyond one token lifetime without asking the user to sign in again.

**PKCE.** PKCE is required by Relativity's own documentation to obtain a refresh token, and not otherwise: a Code request that does not ask for a refresh token works without it, **verified by NSerio against a live instance; not documented by Relativity**. It is never a blanket requirement. NSerio recommends PKCE with the `S256` method in both variants (NSerio recommendation), and does not claim that Relativity accepts any particular method, which it does not document.

**Scope.** No scope set is prescribed for Code. The scope below works — `openid RelativityRequestOrigin UserInfo UserInfoAccess AuthenticationID RelativityWeb` — **verified by NSerio against a live instance; not documented by Relativity**, verified as working and not as minimal. `state` is sent as standard OAuth practice and `nonce` is not used; Relativity does not document whether it enforces either.

**The application stops collecting the password.** After this change the application never receives, stores or forwards the user's username and password: the user signs in on RelativityOne's own page, and the application holds only tokens. Remove the credential prompt, the form field and any storage of the password that the replaced code needed.

**Single sign-on.** Sign-in happens on RelativityOne's own login page, so the instance's single sign-on and two-factor rules apply unchanged — *inferred* from where the sign-in happens, and not documented for clients that customers create.

**The client secret is required.** Code does not work without a client secret: tested and rejected, **verified by NSerio against a live instance; not documented by Relativity**, which confirms Relativity's own statement that the grant requires one. It is displayed once and is stored as Secret Management (Part B of the Migration Documentation) describes.

**Server-side variant.** For a web application whose code runs on a server:

- **Redirect.** The application receives the code at one exact HTTPS URL per environment, a route of the application, with no wildcards (NSerio recommendation; Relativity's matching rules are not documented). The Relativity administrator registers the exact URL on the OAuth2 Client.
- **Secret and tokens.** The client secret stays on the server and never reaches the browser. The user's tokens are held server-side, keyed to the session, and never in the browser.
- **Exchange.** The server performs the code exchange itself.

**Desktop variant.** For a desktop or command-line application run by a user on their own machine:

- **The system browser.** Open the authorize URL in the user's default browser and not in a web view the application hosts, so the application never sees the password (NSerio recommendation).
- **A loopback redirect on a fixed port.** Listen on the loopback address only, on the registered port, for one request. The Redirect URL is `http://127.0.0.1:<port>/<path>`: a plain-HTTP loopback URL is accepted, **verified by NSerio against a live instance; not documented by Relativity**. The Relativity administrator registers that exact URL; because it is registered in advance, the port is fixed and not chosen at runtime.
- **The secret is extractable.** Code requires a client secret (above), so a distributed desktop application has to embed one, and **a secret shipped inside a distributed binary can be extracted by anyone who holds the binary**. It protects nothing against the person running the application.
- **Compensating guidance** (NSerio recommendation, subject to your own security policy): set Trusted IPs where the users' outbound addresses are known and stable — they restrict where a token can be *requested*, and are checked when a token is issued against the client's ID and secret; use the recommended short Access Token Lifetime of 60 minutes; enable refresh tokens only when needed; and create one OAuth2 Client per distribution, so a leaked secret can be disabled without affecting the others.
- **A known issue.** Relativity documents no recommendation for a distributed desktop application; this design is what NSerio verified works, and Relativity's own recommendation is tracked as a known issue of the release.

Verification: **Verification note — three things.**

**Moving the header from the client to the request is part of the migration, not a style choice.** A token has a finite lifetime, and a header added once to `DefaultRequestHeaders` cannot be refreshed without mutating shared state; the code works for the token's lifetime and then returns 401 until the process restarts.

**The pair's encoding may be a separate finding.** Where `encodedCredentials` is built in the same file, the tool reports the encoding and this header as two findings at two locations. They are one migration, and both locations change together.

**Confirm the endpoint in use is the RelativityOne REST API.** That is the API whose bearer token this template describes; an endpoint elsewhere reached from the same code may accept only Basic and reject the migrated call.

OAuth2 Client: Create or confirm this OAuth2 Client in RelativityOne (NSerio's observation: the menu path is Home > Authentication > OAuth2 Client Manager, as Relativity's REST API authentication page names it). This tool never connects to RelativityOne and never creates or changes the OAuth2 Client; the settings below are guidance.
- Name: a descriptive name identifying the application it serves.
- Flow Grant Type: Code
- Redirect URLs: Relativity documentation: each URL includes the http or https protocol prefix. The exact callback URL is registered by the Relativity administrator. Server-side web applications, NSerio's recommendation: one exact HTTPS URL per environment, with no wildcards. Desktop and CLI applications: a plain-HTTP loopback URL, `http://127.0.0.1:<port>/<path>`, on a fixed port because the URL is registered in advance and the port cannot be chosen at runtime (verified by NSerio against a live instance; not documented by Relativity).
- Access Token Lifetime: 60 minutes, stated in minutes because the field is in minutes. Relativity documentation: tokens for this flow must have a short lifetime, and the recommended value matches the Identity Server default.
- Allow refresh tokens: NSerio's recommendation: leave it off unless the application must keep a user session active beyond the access token lifetime without asking the user to sign in again. If it is on, the refresh tokens are stored as secrets, per Secret Management (Part B of the Migration Documentation).
- Enabled: on.
<!-- generated:end CS-006 authorization-code -->

### CS-007 · NetworkCredentialOnHttpHandler

<a id="cs-007-migration"></a>

#### Client Credentials

<!-- generated:begin CS-007 client-credentials -->
**Confirming the flow.** When no signal in a file decides the flow, the tool assigns Client Credentials, the default, with this note: "Who supplies the credential at run time? If a person types it or signs in, the integration acts for that person: use Authorization Code. If it comes from configuration, settings, an environment variable or a secret store: use Client Credentials. No signal in this file showed which applies, so Client Credentials, the default, is recommended. Use Authorization Code instead if the integration acts on behalf of a signed-in user, or if your RelativityOne instance already has a Code-flow OAuth2 Client for this integration." A fixed-credential signal assigns it with this note: "A fixed-credential signal in this file points to Client Credentials, but how often that kind of signal is right has not been measured. Use Authorization Code instead if the integration acts on behalf of a signed-in user, or if your RelativityOne instance already has a Code-flow OAuth2 Client for this integration." See [Confirming the flow](#confirming-the-flow) and the "Which flow" paragraph of CS-007 in [Part A](part-a-csharp.md#cs-007).

Source form:
```csharp
var handler = new HttpClientHandler { Credentials = new NetworkCredential(userName, password) };
```

Under token authentication: **First, the destination:** if it is not RelativityOne, this finding is informational — that service's own token mechanism is the migration — and the rest of this template does not apply.

**Drop the `Credentials` assignment** from the handler, keeping its other settings, and **set the bearer header per request**:

```csharp
var handler = new HttpClientHandler();
request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
```

Credentials: **Where the credentials come from.** The destination decides whether the migration applies. If the destination is not RelativityOne, the migration is that service's own token mechanism, this finding is informational for this tool, and no migration to RelativityOne applies. If it is RelativityOne, the token comes from the token-acquisition part that follows.

**Where the token comes from.** Create an OAuth2 Client with Flow Grant Type set to Client Credentials (part 3 lists its settings), then send a form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=client_credentials`, `client_id`, `client_secret` and a `scope`. The token endpoint is **documented** for this grant. The token is then sent as `Authorization: Bearer <token>`, with `X-CSRF-Header: -` on REST and Kepler calls — the header is documented as required, and a Kepler call without it returns 404.

**The scope depends on what the integration needs, and no single scope is required.** Relativity's documentation shows two values for this grant: `SystemUserInfo` on the REST authentication page, the Usage Reports page and in the official Transfer SDK sample, and `UserInfoAccess` in the Kepler .NET samples. No page says which is correct, whether both work, or what either grants, and the OAuth2 Client's `Scopes` property is documented as one that cannot be set. Use the value the API you call documents, and confirm it.

**Cache the token, and re-acquire it on expiry — do not refresh.** Keep the token for its lifetime (part 3 recommends 60 minutes, Relativity's own wording) and request a new one with the client secret when it expires or a call is rejected for authentication. Client Credentials does not offer refresh tokens: **verified by NSerio against a live instance; not documented by Relativity** — the Allow Refresh Tokens control does not appear for this grant in the RelativityOne user interface. Relativity's own tables and text on refresh tokens conflict with each other, so re-acquisition with the secret is the advice that holds whichever way they are read.

**Long-running processes that use the Relativity SDK.** A `ServiceFactory` built once from a bearer token keeps that token after it expires. Rebuild the `ServiceFactory` and its proxies with a fresh token before expiry. This is the only advice consistent with the documentation: the package `Relativity.Kepler.Client.SDK` (documented) exposes a settable `Token` and an `HttpMessageHandler` overload but no token-provider overload, and no page documents renewing a token behind a factory.

**The client secret.** It is displayed once, at creation or reset. Store it as Secret Management (Part B of the Migration Documentation) describes; never in source, never in tracked configuration. A reset invalidates the previous secret immediately, so plan the cut-over before resetting it.

**Before deploying, confirm the calling identity.** Every call now runs as the OAuth2 Client's **Context User** — not as the user in the replaced credential, and for an import or a job as much as for a request. If that user has different workspace permissions or item-level security, the code returns fewer rows or `Unauthorized` in production with no change to its logic, and Relativity's audit attributes every call to that one account. Confirm the Context User's permissions match those of the previous identity. Relativity's REST API authentication page describes the Context User as usually a member of the Relativity Administrators group; the OAuth2 Client block (part 3) gives NSerio's recommendation, which differs.

**When the integration acts for a signed-in user.** Client Credentials is one service identity by construction. An integration that acts on behalf of a signed-in user is the Authorization Code case, and moving to it means a new OAuth2 Client, because the flow is fixed when a client is created. The finding's flow note says when to make that choice.

Verification: **Verification note — five things.**

**Confirm the exchange was Basic.** `NetworkCredential` declares no scheme, and a handler carrying one serves integrated Windows authentication as readily as Basic. If the server negotiated an integrated scheme, this construct is not Basic Authentication and the migration does not apply.

**The 401-challenge response is lost.** A handler's credentials answer the server's challenge; a bearer header is sent up front and answers nothing. Code or a server that depended on that exchange behaves differently after the change.

**Redirects stop carrying the credential.** Depending on the runtime and the credential type, handler credentials may be re-applied to a redirect target; a manually set `Authorization` header never is — .NET documents that the `HttpClient` handler drops it on redirect. Code that relied on a redirect can start returning 401 after the migration; address the final URI, or handle the redirect explicitly.

**A token has a finite lifetime, and a header set once on a long-lived client cannot be refreshed.** Set it per request, not on the client's default headers, or the code works for the token's lifetime and then returns 401 until the process restarts.

**Confirm the endpoint in use is the RelativityOne REST API.** That is the API whose bearer token this template describes; an endpoint elsewhere reached from the same code may accept only Basic and reject the migrated call.

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
<!-- generated:end CS-007 client-credentials -->

#### Authorization Code

<!-- generated:begin CS-007 authorization-code -->
**Confirming the flow.** An interactive-user signal in a file assigns Authorization Code, with this note: "An interactive-user signal in this file points to Authorization Code. Confirm the flow: use Client Credentials instead if the integration runs with credentials fixed for the integration and no signed-in user." Your user profile can also make Authorization Code the default; see Changing the default in [Confirming the flow](#confirming-the-flow). See [Confirming the flow](#confirming-the-flow) and the "Which flow" paragraph of CS-007 in [Part A](part-a-csharp.md#cs-007).

Source form:
```csharp
var handler = new HttpClientHandler { Credentials = new NetworkCredential(userName, password) };
```

Under token authentication: **First, the destination:** if it is not RelativityOne, this finding is informational — that service's own token mechanism is the migration — and the rest of this template does not apply.

**Drop the `Credentials` assignment** from the handler, keeping its other settings, and **set the bearer header per request**:

```csharp
var handler = new HttpClientHandler();
request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
```

Credentials: **Where the credentials come from.** The destination decides whether the migration applies. If the destination is not RelativityOne, the migration is that service's own token mechanism, this finding is informational for this tool, and no migration to RelativityOne applies. If it is RelativityOne, the token comes from the token-acquisition part that follows.

**Common to both variants**

**Sign in.** Replace the code that collected the user's credentials with a redirect: send the user's browser to the authorize endpoint `<host>/Relativity/Identity/connect/authorize`, which leads to the RelativityOne login page. The authorize endpoint is **verified by NSerio against a live instance; not documented by Relativity**. The request carries the standard authorization-code parameters — `client_id`, `response_type=code`, the registered `redirect_uri`, a `scope`, and a `state` value the application checks on return; these are standard OAuth, and Relativity documents none of them. No discovery document is relied on.

**Receive the code.** The browser returns to the Redirect URL registered on the OAuth2 Client, with the authorization code and the `state` value. Check the `state` before using the code. The Redirect URL must be exactly the one registered (the variants say how).

**Exchange the code.** Send a form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=authorization_code`, the `code`, the same `redirect_uri`, the `client_id` and the `client_secret`, plus the `code_verifier` when PKCE is used. The code is exchanged at that endpoint, which Relativity documents for the Client Credentials grant only; the exchange is **verified by NSerio against a live instance; not documented by Relativity**.

**Use the token.** Send `Authorization: Bearer <access token>` — the change at the use site in part 1 — with `X-CSRF-Header: -` on REST and Kepler calls.

**On expiry.** With Allow Refresh Tokens enabled on the OAuth2 Client and PKCE used, the application holds a refresh token and, when the access token expires, sends a `grant_type=refresh_token` request with it and the client credentials in place of the code exchange — *inferred* from the standard OAuth shape, not tested. Without a refresh token the user signs in again. Relativity does not document a refresh token's lifetime, whether it rotates, how it is revoked, or whether a revocation endpoint exists, nor what disabling or deleting the client does to tokens already issued. A refresh token is a secret: store it as Secret Management (Part B of the Migration Documentation) describes, and on compromise disable the client and replace it. Enable refresh tokens only if the application must keep a session beyond one token lifetime without asking the user to sign in again.

**PKCE.** PKCE is required by Relativity's own documentation to obtain a refresh token, and not otherwise: a Code request that does not ask for a refresh token works without it, **verified by NSerio against a live instance; not documented by Relativity**. It is never a blanket requirement. NSerio recommends PKCE with the `S256` method in both variants (NSerio recommendation), and does not claim that Relativity accepts any particular method, which it does not document.

**Scope.** No scope set is prescribed for Code. The scope below works — `openid RelativityRequestOrigin UserInfo UserInfoAccess AuthenticationID RelativityWeb` — **verified by NSerio against a live instance; not documented by Relativity**, verified as working and not as minimal. `state` is sent as standard OAuth practice and `nonce` is not used; Relativity does not document whether it enforces either.

**The application stops collecting the password.** After this change the application never receives, stores or forwards the user's username and password: the user signs in on RelativityOne's own page, and the application holds only tokens. Remove the credential prompt, the form field and any storage of the password that the replaced code needed.

**Single sign-on.** Sign-in happens on RelativityOne's own login page, so the instance's single sign-on and two-factor rules apply unchanged — *inferred* from where the sign-in happens, and not documented for clients that customers create.

**The client secret is required.** Code does not work without a client secret: tested and rejected, **verified by NSerio against a live instance; not documented by Relativity**, which confirms Relativity's own statement that the grant requires one. It is displayed once and is stored as Secret Management (Part B of the Migration Documentation) describes.

**Server-side variant.** For a web application whose code runs on a server:

- **Redirect.** The application receives the code at one exact HTTPS URL per environment, a route of the application, with no wildcards (NSerio recommendation; Relativity's matching rules are not documented). The Relativity administrator registers the exact URL on the OAuth2 Client.
- **Secret and tokens.** The client secret stays on the server and never reaches the browser. The user's tokens are held server-side, keyed to the session, and never in the browser.
- **Exchange.** The server performs the code exchange itself.

**Desktop variant.** For a desktop or command-line application run by a user on their own machine:

- **The system browser.** Open the authorize URL in the user's default browser and not in a web view the application hosts, so the application never sees the password (NSerio recommendation).
- **A loopback redirect on a fixed port.** Listen on the loopback address only, on the registered port, for one request. The Redirect URL is `http://127.0.0.1:<port>/<path>`: a plain-HTTP loopback URL is accepted, **verified by NSerio against a live instance; not documented by Relativity**. The Relativity administrator registers that exact URL; because it is registered in advance, the port is fixed and not chosen at runtime.
- **The secret is extractable.** Code requires a client secret (above), so a distributed desktop application has to embed one, and **a secret shipped inside a distributed binary can be extracted by anyone who holds the binary**. It protects nothing against the person running the application.
- **Compensating guidance** (NSerio recommendation, subject to your own security policy): set Trusted IPs where the users' outbound addresses are known and stable — they restrict where a token can be *requested*, and are checked when a token is issued against the client's ID and secret; use the recommended short Access Token Lifetime of 60 minutes; enable refresh tokens only when needed; and create one OAuth2 Client per distribution, so a leaked secret can be disabled without affecting the others.
- **A known issue.** Relativity documents no recommendation for a distributed desktop application; this design is what NSerio verified works, and Relativity's own recommendation is tracked as a known issue of the release.

Verification: **Verification note — five things.**

**Confirm the exchange was Basic.** `NetworkCredential` declares no scheme, and a handler carrying one serves integrated Windows authentication as readily as Basic. If the server negotiated an integrated scheme, this construct is not Basic Authentication and the migration does not apply.

**The 401-challenge response is lost.** A handler's credentials answer the server's challenge; a bearer header is sent up front and answers nothing. Code or a server that depended on that exchange behaves differently after the change.

**Redirects stop carrying the credential.** Depending on the runtime and the credential type, handler credentials may be re-applied to a redirect target; a manually set `Authorization` header never is — .NET documents that the `HttpClient` handler drops it on redirect. Code that relied on a redirect can start returning 401 after the migration; address the final URI, or handle the redirect explicitly.

**A token has a finite lifetime, and a header set once on a long-lived client cannot be refreshed.** Set it per request, not on the client's default headers, or the code works for the token's lifetime and then returns 401 until the process restarts.

**Confirm the endpoint in use is the RelativityOne REST API.** That is the API whose bearer token this template describes; an endpoint elsewhere reached from the same code may accept only Basic and reject the migrated call.

OAuth2 Client: Create or confirm this OAuth2 Client in RelativityOne (NSerio's observation: the menu path is Home > Authentication > OAuth2 Client Manager, as Relativity's REST API authentication page names it). This tool never connects to RelativityOne and never creates or changes the OAuth2 Client; the settings below are guidance.
- Name: a descriptive name identifying the application it serves.
- Flow Grant Type: Code
- Redirect URLs: Relativity documentation: each URL includes the http or https protocol prefix. The exact callback URL is registered by the Relativity administrator. Server-side web applications, NSerio's recommendation: one exact HTTPS URL per environment, with no wildcards. Desktop and CLI applications: a plain-HTTP loopback URL, `http://127.0.0.1:<port>/<path>`, on a fixed port because the URL is registered in advance and the port cannot be chosen at runtime (verified by NSerio against a live instance; not documented by Relativity).
- Access Token Lifetime: 60 minutes, stated in minutes because the field is in minutes. Relativity documentation: tokens for this flow must have a short lifetime, and the recommended value matches the Identity Server default.
- Allow refresh tokens: NSerio's recommendation: leave it off unless the application must keep a user session active beyond the access token lifetime without asking the user to sign in again. If it is on, the refresh tokens are stored as secrets, per Secret Management (Part B of the Migration Documentation).
- Enabled: on.
<!-- generated:end CS-007 authorization-code -->

### CS-008 · NetworkCredentialConstruction

<a id="cs-008-migration"></a>

#### Client Credentials

<!-- generated:begin CS-008 client-credentials -->
**Confirming the flow.** When no signal in a file decides the flow, the tool assigns Client Credentials, the default, with this note: "Who supplies the credential at run time? If a person types it or signs in, the integration acts for that person: use Authorization Code. If it comes from configuration, settings, an environment variable or a secret store: use Client Credentials. No signal in this file showed which applies, so Client Credentials, the default, is recommended. Use Authorization Code instead if the integration acts on behalf of a signed-in user, or if your RelativityOne instance already has a Code-flow OAuth2 Client for this integration." A fixed-credential signal assigns it with this note: "A fixed-credential signal in this file points to Client Credentials, but how often that kind of signal is right has not been measured. Use Authorization Code instead if the integration acts on behalf of a signed-in user, or if your RelativityOne instance already has a Code-flow OAuth2 Client for this integration." See [Confirming the flow](#confirming-the-flow) and the "Which flow" paragraph of CS-008 in [Part A](part-a-csharp.md#cs-008).

Source form:
```csharp
var credential = new NetworkCredential(userName, password);
```

Under token authentication: **First, the destination:** if it is not RelativityOne, this finding is informational — that service's own token mechanism is the migration — and the rest of this template does not apply.

**Identify the consumer first** — the fix may be outside the reported file, because this finding reports a credential whose consumer the tool did not recognize in it. The form of the change depends on what consumes the credential:

- an `HttpClient` handler: remove the credential from the handler and set the `Authorization` header to `Bearer` and the token on each request message;
- `WebClient` or `HttpWebRequest`: remove the credential and set the `Authorization` header to `Bearer` and the token on each request;
- a SOAP proxy: the binding changes, and the service may not accept bearer at all;
- a mail, file-transfer, directory or proxy consumer: not a migration target for this tool.

No code form is given, because none is correct for all four.

Credentials: **Where the credentials come from.** The destination decides whether the migration applies. If the destination is not RelativityOne, the migration is that service's own token mechanism, this finding is informational for this tool, and no migration to RelativityOne applies. If it is RelativityOne, the token comes from the token-acquisition part that follows.

**Where the token comes from.** Create an OAuth2 Client with Flow Grant Type set to Client Credentials (part 3 lists its settings), then send a form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=client_credentials`, `client_id`, `client_secret` and a `scope`. The token endpoint is **documented** for this grant. The token is then sent as `Authorization: Bearer <token>`, with `X-CSRF-Header: -` on REST and Kepler calls — the header is documented as required, and a Kepler call without it returns 404.

**The scope depends on what the integration needs, and no single scope is required.** Relativity's documentation shows two values for this grant: `SystemUserInfo` on the REST authentication page, the Usage Reports page and in the official Transfer SDK sample, and `UserInfoAccess` in the Kepler .NET samples. No page says which is correct, whether both work, or what either grants, and the OAuth2 Client's `Scopes` property is documented as one that cannot be set. Use the value the API you call documents, and confirm it.

**Cache the token, and re-acquire it on expiry — do not refresh.** Keep the token for its lifetime (part 3 recommends 60 minutes, Relativity's own wording) and request a new one with the client secret when it expires or a call is rejected for authentication. Client Credentials does not offer refresh tokens: **verified by NSerio against a live instance; not documented by Relativity** — the Allow Refresh Tokens control does not appear for this grant in the RelativityOne user interface. Relativity's own tables and text on refresh tokens conflict with each other, so re-acquisition with the secret is the advice that holds whichever way they are read.

**Long-running processes that use the Relativity SDK.** A `ServiceFactory` built once from a bearer token keeps that token after it expires. Rebuild the `ServiceFactory` and its proxies with a fresh token before expiry. This is the only advice consistent with the documentation: the package `Relativity.Kepler.Client.SDK` (documented) exposes a settable `Token` and an `HttpMessageHandler` overload but no token-provider overload, and no page documents renewing a token behind a factory.

**The client secret.** It is displayed once, at creation or reset. Store it as Secret Management (Part B of the Migration Documentation) describes; never in source, never in tracked configuration. A reset invalidates the previous secret immediately, so plan the cut-over before resetting it.

**Before deploying, confirm the calling identity.** Every call now runs as the OAuth2 Client's **Context User** — not as the user in the replaced credential, and for an import or a job as much as for a request. If that user has different workspace permissions or item-level security, the code returns fewer rows or `Unauthorized` in production with no change to its logic, and Relativity's audit attributes every call to that one account. Confirm the Context User's permissions match those of the previous identity. Relativity's REST API authentication page describes the Context User as usually a member of the Relativity Administrators group; the OAuth2 Client block (part 3) gives NSerio's recommendation, which differs.

**When the integration acts for a signed-in user.** Client Credentials is one service identity by construction. An integration that acts on behalf of a signed-in user is the Authorization Code case, and moving to it means a new OAuth2 Client, because the flow is fixed when a client is created. The finding's flow note says when to make that choice.

Verification: **Verification note — seven things.**

**This finding is the least characterized of any template.** The consumer was not recognized, so neither the scheme nor the destination has been established. Treat it as the location of a credential to trace, not as a construct whose migration is known.

**Confirm the exchange was Basic.** `NetworkCredential` declares no scheme; the same type serves integrated Windows authentication, and the consumers above perform the exchange themselves with no `Basic` in the source. If the exchange is integrated, the migration does not apply.

**The 401-challenge response is lost** wherever the consumer answered a challenge with this credential; a bearer header is sent up front and answers nothing.

**Redirects stop carrying the credential.** Depending on the runtime and the credential type, a consumer's credentials may be re-applied to a redirect target; a manually set `Authorization` header never is. .NET documents that `HttpWebRequest` clears the `Authorization` header when it follows a redirect automatically, and the `HttpClient` handler drops it on redirect. Code that relied on a redirect can start returning 401 after the migration; address the final URI, or handle the redirect explicitly.

**A SOAP service may not accept bearer at all.** Confirm it does before changing a binding.

**Confirm the endpoint in use is the RelativityOne REST API.** That is the API whose bearer token this template describes; an endpoint elsewhere reached from the same code may accept only Basic and reject the migrated call.

**A token has a finite lifetime.** A header must be set per request to be refreshable: a header set once on a long-lived consumer works for the token's lifetime and then returns 401 until the process restarts.

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
<!-- generated:end CS-008 client-credentials -->

#### Authorization Code

<!-- generated:begin CS-008 authorization-code -->
**Confirming the flow.** An interactive-user signal in a file assigns Authorization Code, with this note: "An interactive-user signal in this file points to Authorization Code. Confirm the flow: use Client Credentials instead if the integration runs with credentials fixed for the integration and no signed-in user." Your user profile can also make Authorization Code the default; see Changing the default in [Confirming the flow](#confirming-the-flow). See [Confirming the flow](#confirming-the-flow) and the "Which flow" paragraph of CS-008 in [Part A](part-a-csharp.md#cs-008).

Source form:
```csharp
var credential = new NetworkCredential(userName, password);
```

Under token authentication: **First, the destination:** if it is not RelativityOne, this finding is informational — that service's own token mechanism is the migration — and the rest of this template does not apply.

**Identify the consumer first** — the fix may be outside the reported file, because this finding reports a credential whose consumer the tool did not recognize in it. The form of the change depends on what consumes the credential:

- an `HttpClient` handler: remove the credential from the handler and set the `Authorization` header to `Bearer` and the token on each request message;
- `WebClient` or `HttpWebRequest`: remove the credential and set the `Authorization` header to `Bearer` and the token on each request;
- a SOAP proxy: the binding changes, and the service may not accept bearer at all;
- a mail, file-transfer, directory or proxy consumer: not a migration target for this tool.

No code form is given, because none is correct for all four.

Credentials: **Where the credentials come from.** The destination decides whether the migration applies. If the destination is not RelativityOne, the migration is that service's own token mechanism, this finding is informational for this tool, and no migration to RelativityOne applies. If it is RelativityOne, the token comes from the token-acquisition part that follows.

**Common to both variants**

**Sign in.** Replace the code that collected the user's credentials with a redirect: send the user's browser to the authorize endpoint `<host>/Relativity/Identity/connect/authorize`, which leads to the RelativityOne login page. The authorize endpoint is **verified by NSerio against a live instance; not documented by Relativity**. The request carries the standard authorization-code parameters — `client_id`, `response_type=code`, the registered `redirect_uri`, a `scope`, and a `state` value the application checks on return; these are standard OAuth, and Relativity documents none of them. No discovery document is relied on.

**Receive the code.** The browser returns to the Redirect URL registered on the OAuth2 Client, with the authorization code and the `state` value. Check the `state` before using the code. The Redirect URL must be exactly the one registered (the variants say how).

**Exchange the code.** Send a form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=authorization_code`, the `code`, the same `redirect_uri`, the `client_id` and the `client_secret`, plus the `code_verifier` when PKCE is used. The code is exchanged at that endpoint, which Relativity documents for the Client Credentials grant only; the exchange is **verified by NSerio against a live instance; not documented by Relativity**.

**Use the token.** Send `Authorization: Bearer <access token>` — the change at the use site in part 1 — with `X-CSRF-Header: -` on REST and Kepler calls.

**On expiry.** With Allow Refresh Tokens enabled on the OAuth2 Client and PKCE used, the application holds a refresh token and, when the access token expires, sends a `grant_type=refresh_token` request with it and the client credentials in place of the code exchange — *inferred* from the standard OAuth shape, not tested. Without a refresh token the user signs in again. Relativity does not document a refresh token's lifetime, whether it rotates, how it is revoked, or whether a revocation endpoint exists, nor what disabling or deleting the client does to tokens already issued. A refresh token is a secret: store it as Secret Management (Part B of the Migration Documentation) describes, and on compromise disable the client and replace it. Enable refresh tokens only if the application must keep a session beyond one token lifetime without asking the user to sign in again.

**PKCE.** PKCE is required by Relativity's own documentation to obtain a refresh token, and not otherwise: a Code request that does not ask for a refresh token works without it, **verified by NSerio against a live instance; not documented by Relativity**. It is never a blanket requirement. NSerio recommends PKCE with the `S256` method in both variants (NSerio recommendation), and does not claim that Relativity accepts any particular method, which it does not document.

**Scope.** No scope set is prescribed for Code. The scope below works — `openid RelativityRequestOrigin UserInfo UserInfoAccess AuthenticationID RelativityWeb` — **verified by NSerio against a live instance; not documented by Relativity**, verified as working and not as minimal. `state` is sent as standard OAuth practice and `nonce` is not used; Relativity does not document whether it enforces either.

**The application stops collecting the password.** After this change the application never receives, stores or forwards the user's username and password: the user signs in on RelativityOne's own page, and the application holds only tokens. Remove the credential prompt, the form field and any storage of the password that the replaced code needed.

**Single sign-on.** Sign-in happens on RelativityOne's own login page, so the instance's single sign-on and two-factor rules apply unchanged — *inferred* from where the sign-in happens, and not documented for clients that customers create.

**The client secret is required.** Code does not work without a client secret: tested and rejected, **verified by NSerio against a live instance; not documented by Relativity**, which confirms Relativity's own statement that the grant requires one. It is displayed once and is stored as Secret Management (Part B of the Migration Documentation) describes.

**Server-side variant.** For a web application whose code runs on a server:

- **Redirect.** The application receives the code at one exact HTTPS URL per environment, a route of the application, with no wildcards (NSerio recommendation; Relativity's matching rules are not documented). The Relativity administrator registers the exact URL on the OAuth2 Client.
- **Secret and tokens.** The client secret stays on the server and never reaches the browser. The user's tokens are held server-side, keyed to the session, and never in the browser.
- **Exchange.** The server performs the code exchange itself.

**Desktop variant.** For a desktop or command-line application run by a user on their own machine:

- **The system browser.** Open the authorize URL in the user's default browser and not in a web view the application hosts, so the application never sees the password (NSerio recommendation).
- **A loopback redirect on a fixed port.** Listen on the loopback address only, on the registered port, for one request. The Redirect URL is `http://127.0.0.1:<port>/<path>`: a plain-HTTP loopback URL is accepted, **verified by NSerio against a live instance; not documented by Relativity**. The Relativity administrator registers that exact URL; because it is registered in advance, the port is fixed and not chosen at runtime.
- **The secret is extractable.** Code requires a client secret (above), so a distributed desktop application has to embed one, and **a secret shipped inside a distributed binary can be extracted by anyone who holds the binary**. It protects nothing against the person running the application.
- **Compensating guidance** (NSerio recommendation, subject to your own security policy): set Trusted IPs where the users' outbound addresses are known and stable — they restrict where a token can be *requested*, and are checked when a token is issued against the client's ID and secret; use the recommended short Access Token Lifetime of 60 minutes; enable refresh tokens only when needed; and create one OAuth2 Client per distribution, so a leaked secret can be disabled without affecting the others.
- **A known issue.** Relativity documents no recommendation for a distributed desktop application; this design is what NSerio verified works, and Relativity's own recommendation is tracked as a known issue of the release.

Verification: **Verification note — seven things.**

**This finding is the least characterized of any template.** The consumer was not recognized, so neither the scheme nor the destination has been established. Treat it as the location of a credential to trace, not as a construct whose migration is known.

**Confirm the exchange was Basic.** `NetworkCredential` declares no scheme; the same type serves integrated Windows authentication, and the consumers above perform the exchange themselves with no `Basic` in the source. If the exchange is integrated, the migration does not apply.

**The 401-challenge response is lost** wherever the consumer answered a challenge with this credential; a bearer header is sent up front and answers nothing.

**Redirects stop carrying the credential.** Depending on the runtime and the credential type, a consumer's credentials may be re-applied to a redirect target; a manually set `Authorization` header never is. .NET documents that `HttpWebRequest` clears the `Authorization` header when it follows a redirect automatically, and the `HttpClient` handler drops it on redirect. Code that relied on a redirect can start returning 401 after the migration; address the final URI, or handle the redirect explicitly.

**A SOAP service may not accept bearer at all.** Confirm it does before changing a binding.

**Confirm the endpoint in use is the RelativityOne REST API.** That is the API whose bearer token this template describes; an endpoint elsewhere reached from the same code may accept only Basic and reject the migrated call.

**A token has a finite lifetime.** A header must be set per request to be refreshable: a header set once on a long-lived consumer works for the token's lifetime and then returns 401 until the process restarts.

OAuth2 Client: Create or confirm this OAuth2 Client in RelativityOne (NSerio's observation: the menu path is Home > Authentication > OAuth2 Client Manager, as Relativity's REST API authentication page names it). This tool never connects to RelativityOne and never creates or changes the OAuth2 Client; the settings below are guidance.
- Name: a descriptive name identifying the application it serves.
- Flow Grant Type: Code
- Redirect URLs: Relativity documentation: each URL includes the http or https protocol prefix. The exact callback URL is registered by the Relativity administrator. Server-side web applications, NSerio's recommendation: one exact HTTPS URL per environment, with no wildcards. Desktop and CLI applications: a plain-HTTP loopback URL, `http://127.0.0.1:<port>/<path>`, on a fixed port because the URL is registered in advance and the port cannot be chosen at runtime (verified by NSerio against a live instance; not documented by Relativity).
- Access Token Lifetime: 60 minutes, stated in minutes because the field is in minutes. Relativity documentation: tokens for this flow must have a short lifetime, and the recommended value matches the Identity Server default.
- Allow refresh tokens: NSerio's recommendation: leave it off unless the application must keep a user session active beyond the access token lifetime without asking the user to sign in again. If it is on, the refresh tokens are stored as secrets, per Secret Management (Part B of the Migration Documentation).
- Enabled: on.
<!-- generated:end CS-008 authorization-code -->

### CS-009 · Base64UserPasswordPair

<a id="cs-009-migration"></a>

#### Client Credentials

<!-- generated:begin CS-009 client-credentials -->
**Confirming the flow.** When no signal in a file decides the flow, the tool assigns Client Credentials, the default, with this note: "Who supplies the credential at run time? If a person types it or signs in, the integration acts for that person: use Authorization Code. If it comes from configuration, settings, an environment variable or a secret store: use Client Credentials. No signal in this file showed which applies, so Client Credentials, the default, is recommended. Use Authorization Code instead if the integration acts on behalf of a signed-in user, or if your RelativityOne instance already has a Code-flow OAuth2 Client for this integration." A fixed-credential signal assigns it with this note: "A fixed-credential signal in this file points to Client Credentials, but how often that kind of signal is right has not been measured. Use Authorization Code instead if the integration acts on behalf of a signed-in user, or if your RelativityOne instance already has a Code-flow OAuth2 Client for this integration." See [Confirming the flow](#confirming-the-flow) and the "Which flow" paragraph of CS-009 in [Part A](part-a-csharp.md#cs-009).

Source form:
```csharp
string pair = $"{userName}:{password}";
string encoded = Convert.ToBase64String(Encoding.ASCII.GetBytes(pair));
```

Under token authentication: **First, the destination:** if it is not RelativityOne, this finding is informational — that service's own token mechanism is the migration — and the rest of this template does not apply.

**Only if this encoding feeds an `Authorization` header.** Base64 over a colon-separated pair is not always a credential: the same shape encodes other data, and it is Basic Authentication only where the encoded value becomes the header. Where it does, **delete the encoding**: bearer carries the token verbatim, with no Base64 of a credential pair, and the header that received `encoded` receives the token, set per request:

```csharp
request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
```

Credentials: **Where the credentials come from.** The destination decides whether the migration applies. If the destination is not RelativityOne, the migration is that service's own token mechanism, this finding is informational for this tool, and no migration to RelativityOne applies. If it is RelativityOne, the token comes from the token-acquisition part that follows.

**Where the token comes from.** Create an OAuth2 Client with Flow Grant Type set to Client Credentials (part 3 lists its settings), then send a form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=client_credentials`, `client_id`, `client_secret` and a `scope`. The token endpoint is **documented** for this grant. The token is then sent as `Authorization: Bearer <token>`, with `X-CSRF-Header: -` on REST and Kepler calls — the header is documented as required, and a Kepler call without it returns 404.

**The scope depends on what the integration needs, and no single scope is required.** Relativity's documentation shows two values for this grant: `SystemUserInfo` on the REST authentication page, the Usage Reports page and in the official Transfer SDK sample, and `UserInfoAccess` in the Kepler .NET samples. No page says which is correct, whether both work, or what either grants, and the OAuth2 Client's `Scopes` property is documented as one that cannot be set. Use the value the API you call documents, and confirm it.

**Cache the token, and re-acquire it on expiry — do not refresh.** Keep the token for its lifetime (part 3 recommends 60 minutes, Relativity's own wording) and request a new one with the client secret when it expires or a call is rejected for authentication. Client Credentials does not offer refresh tokens: **verified by NSerio against a live instance; not documented by Relativity** — the Allow Refresh Tokens control does not appear for this grant in the RelativityOne user interface. Relativity's own tables and text on refresh tokens conflict with each other, so re-acquisition with the secret is the advice that holds whichever way they are read.

**Long-running processes that use the Relativity SDK.** A `ServiceFactory` built once from a bearer token keeps that token after it expires. Rebuild the `ServiceFactory` and its proxies with a fresh token before expiry. This is the only advice consistent with the documentation: the package `Relativity.Kepler.Client.SDK` (documented) exposes a settable `Token` and an `HttpMessageHandler` overload but no token-provider overload, and no page documents renewing a token behind a factory.

**The client secret.** It is displayed once, at creation or reset. Store it as Secret Management (Part B of the Migration Documentation) describes; never in source, never in tracked configuration. A reset invalidates the previous secret immediately, so plan the cut-over before resetting it.

**Before deploying, confirm the calling identity.** Every call now runs as the OAuth2 Client's **Context User** — not as the user in the replaced credential, and for an import or a job as much as for a request. If that user has different workspace permissions or item-level security, the code returns fewer rows or `Unauthorized` in production with no change to its logic, and Relativity's audit attributes every call to that one account. Confirm the Context User's permissions match those of the previous identity. Relativity's REST API authentication page describes the Context User as usually a member of the Relativity Administrators group; the OAuth2 Client block (part 3) gives NSerio's recommendation, which differs.

**When the integration acts for a signed-in user.** Client Credentials is one service identity by construction. An integration that acts on behalf of a signed-in user is the Authorization Code case, and moving to it means a new OAuth2 Client, because the flow is fixed when a client is created. The finding's flow note says when to make that choice.

Verification: **Verification note — four things.**

**Confirm the pair is a credential sent outward.** A service of the client's that *checks* Basic from its own callers is inbound authentication, which has nothing to migrate to Relativity OAuth2, and this tool does not cover it; it can still surface here, and such a finding is not a migration.

**The header write may be a separate finding.** Where the header is written in the same file, the tool reports this encoding and that header as two findings at two locations. They are one migration, and both locations change together.

**A token has a finite lifetime, and a header set once on a long-lived client cannot be refreshed.** Set it per request, or the code works for the token's lifetime and then returns 401 until the process restarts.

**Confirm the endpoint in use is the RelativityOne REST API.** That is the API whose bearer token this template describes; an endpoint elsewhere reached from the same code may accept only Basic and reject the migrated call.

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
<!-- generated:end CS-009 client-credentials -->

#### Authorization Code

<!-- generated:begin CS-009 authorization-code -->
**Confirming the flow.** An interactive-user signal in a file assigns Authorization Code, with this note: "An interactive-user signal in this file points to Authorization Code. Confirm the flow: use Client Credentials instead if the integration runs with credentials fixed for the integration and no signed-in user." Your user profile can also make Authorization Code the default; see Changing the default in [Confirming the flow](#confirming-the-flow). See [Confirming the flow](#confirming-the-flow) and the "Which flow" paragraph of CS-009 in [Part A](part-a-csharp.md#cs-009).

Source form:
```csharp
string pair = $"{userName}:{password}";
string encoded = Convert.ToBase64String(Encoding.ASCII.GetBytes(pair));
```

Under token authentication: **First, the destination:** if it is not RelativityOne, this finding is informational — that service's own token mechanism is the migration — and the rest of this template does not apply.

**Only if this encoding feeds an `Authorization` header.** Base64 over a colon-separated pair is not always a credential: the same shape encodes other data, and it is Basic Authentication only where the encoded value becomes the header. Where it does, **delete the encoding**: bearer carries the token verbatim, with no Base64 of a credential pair, and the header that received `encoded` receives the token, set per request:

```csharp
request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
```

Credentials: **Where the credentials come from.** The destination decides whether the migration applies. If the destination is not RelativityOne, the migration is that service's own token mechanism, this finding is informational for this tool, and no migration to RelativityOne applies. If it is RelativityOne, the token comes from the token-acquisition part that follows.

**Common to both variants**

**Sign in.** Replace the code that collected the user's credentials with a redirect: send the user's browser to the authorize endpoint `<host>/Relativity/Identity/connect/authorize`, which leads to the RelativityOne login page. The authorize endpoint is **verified by NSerio against a live instance; not documented by Relativity**. The request carries the standard authorization-code parameters — `client_id`, `response_type=code`, the registered `redirect_uri`, a `scope`, and a `state` value the application checks on return; these are standard OAuth, and Relativity documents none of them. No discovery document is relied on.

**Receive the code.** The browser returns to the Redirect URL registered on the OAuth2 Client, with the authorization code and the `state` value. Check the `state` before using the code. The Redirect URL must be exactly the one registered (the variants say how).

**Exchange the code.** Send a form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=authorization_code`, the `code`, the same `redirect_uri`, the `client_id` and the `client_secret`, plus the `code_verifier` when PKCE is used. The code is exchanged at that endpoint, which Relativity documents for the Client Credentials grant only; the exchange is **verified by NSerio against a live instance; not documented by Relativity**.

**Use the token.** Send `Authorization: Bearer <access token>` — the change at the use site in part 1 — with `X-CSRF-Header: -` on REST and Kepler calls.

**On expiry.** With Allow Refresh Tokens enabled on the OAuth2 Client and PKCE used, the application holds a refresh token and, when the access token expires, sends a `grant_type=refresh_token` request with it and the client credentials in place of the code exchange — *inferred* from the standard OAuth shape, not tested. Without a refresh token the user signs in again. Relativity does not document a refresh token's lifetime, whether it rotates, how it is revoked, or whether a revocation endpoint exists, nor what disabling or deleting the client does to tokens already issued. A refresh token is a secret: store it as Secret Management (Part B of the Migration Documentation) describes, and on compromise disable the client and replace it. Enable refresh tokens only if the application must keep a session beyond one token lifetime without asking the user to sign in again.

**PKCE.** PKCE is required by Relativity's own documentation to obtain a refresh token, and not otherwise: a Code request that does not ask for a refresh token works without it, **verified by NSerio against a live instance; not documented by Relativity**. It is never a blanket requirement. NSerio recommends PKCE with the `S256` method in both variants (NSerio recommendation), and does not claim that Relativity accepts any particular method, which it does not document.

**Scope.** No scope set is prescribed for Code. The scope below works — `openid RelativityRequestOrigin UserInfo UserInfoAccess AuthenticationID RelativityWeb` — **verified by NSerio against a live instance; not documented by Relativity**, verified as working and not as minimal. `state` is sent as standard OAuth practice and `nonce` is not used; Relativity does not document whether it enforces either.

**The application stops collecting the password.** After this change the application never receives, stores or forwards the user's username and password: the user signs in on RelativityOne's own page, and the application holds only tokens. Remove the credential prompt, the form field and any storage of the password that the replaced code needed.

**Single sign-on.** Sign-in happens on RelativityOne's own login page, so the instance's single sign-on and two-factor rules apply unchanged — *inferred* from where the sign-in happens, and not documented for clients that customers create.

**The client secret is required.** Code does not work without a client secret: tested and rejected, **verified by NSerio against a live instance; not documented by Relativity**, which confirms Relativity's own statement that the grant requires one. It is displayed once and is stored as Secret Management (Part B of the Migration Documentation) describes.

**Server-side variant.** For a web application whose code runs on a server:

- **Redirect.** The application receives the code at one exact HTTPS URL per environment, a route of the application, with no wildcards (NSerio recommendation; Relativity's matching rules are not documented). The Relativity administrator registers the exact URL on the OAuth2 Client.
- **Secret and tokens.** The client secret stays on the server and never reaches the browser. The user's tokens are held server-side, keyed to the session, and never in the browser.
- **Exchange.** The server performs the code exchange itself.

**Desktop variant.** For a desktop or command-line application run by a user on their own machine:

- **The system browser.** Open the authorize URL in the user's default browser and not in a web view the application hosts, so the application never sees the password (NSerio recommendation).
- **A loopback redirect on a fixed port.** Listen on the loopback address only, on the registered port, for one request. The Redirect URL is `http://127.0.0.1:<port>/<path>`: a plain-HTTP loopback URL is accepted, **verified by NSerio against a live instance; not documented by Relativity**. The Relativity administrator registers that exact URL; because it is registered in advance, the port is fixed and not chosen at runtime.
- **The secret is extractable.** Code requires a client secret (above), so a distributed desktop application has to embed one, and **a secret shipped inside a distributed binary can be extracted by anyone who holds the binary**. It protects nothing against the person running the application.
- **Compensating guidance** (NSerio recommendation, subject to your own security policy): set Trusted IPs where the users' outbound addresses are known and stable — they restrict where a token can be *requested*, and are checked when a token is issued against the client's ID and secret; use the recommended short Access Token Lifetime of 60 minutes; enable refresh tokens only when needed; and create one OAuth2 Client per distribution, so a leaked secret can be disabled without affecting the others.
- **A known issue.** Relativity documents no recommendation for a distributed desktop application; this design is what NSerio verified works, and Relativity's own recommendation is tracked as a known issue of the release.

Verification: **Verification note — four things.**

**Confirm the pair is a credential sent outward.** A service of the client's that *checks* Basic from its own callers is inbound authentication, which has nothing to migrate to Relativity OAuth2, and this tool does not cover it; it can still surface here, and such a finding is not a migration.

**The header write may be a separate finding.** Where the header is written in the same file, the tool reports this encoding and that header as two findings at two locations. They are one migration, and both locations change together.

**A token has a finite lifetime, and a header set once on a long-lived client cannot be refreshed.** Set it per request, or the code works for the token's lifetime and then returns 401 until the process restarts.

**Confirm the endpoint in use is the RelativityOne REST API.** That is the API whose bearer token this template describes; an endpoint elsewhere reached from the same code may accept only Basic and reject the migrated call.

OAuth2 Client: Create or confirm this OAuth2 Client in RelativityOne (NSerio's observation: the menu path is Home > Authentication > OAuth2 Client Manager, as Relativity's REST API authentication page names it). This tool never connects to RelativityOne and never creates or changes the OAuth2 Client; the settings below are guidance.
- Name: a descriptive name identifying the application it serves.
- Flow Grant Type: Code
- Redirect URLs: Relativity documentation: each URL includes the http or https protocol prefix. The exact callback URL is registered by the Relativity administrator. Server-side web applications, NSerio's recommendation: one exact HTTPS URL per environment, with no wildcards. Desktop and CLI applications: a plain-HTTP loopback URL, `http://127.0.0.1:<port>/<path>`, on a fixed port because the URL is registered in advance and the port cannot be chosen at runtime (verified by NSerio against a live instance; not documented by Relativity).
- Access Token Lifetime: 60 minutes, stated in minutes because the field is in minutes. Relativity documentation: tokens for this flow must have a short lifetime, and the recommended value matches the Identity Server default.
- Allow refresh tokens: NSerio's recommendation: leave it off unless the application must keep a user session active beyond the access token lifetime without asking the user to sign in again. If it is on, the refresh tokens are stored as secrets, per Secret Management (Part B of the Migration Documentation).
- Enabled: on.
<!-- generated:end CS-009 authorization-code -->

### CS-012 · HttpRequestHeaderEnumAuthorization

<a id="cs-012-migration"></a>

#### Client Credentials

<!-- generated:begin CS-012 client-credentials -->
**Confirming the flow.** When no signal in a file decides the flow, the tool assigns Client Credentials, the default, with this note: "Who supplies the credential at run time? If a person types it or signs in, the integration acts for that person: use Authorization Code. If it comes from configuration, settings, an environment variable or a secret store: use Client Credentials. No signal in this file showed which applies, so Client Credentials, the default, is recommended. Use Authorization Code instead if the integration acts on behalf of a signed-in user, or if your RelativityOne instance already has a Code-flow OAuth2 Client for this integration." A fixed-credential signal assigns it with this note: "A fixed-credential signal in this file points to Client Credentials, but how often that kind of signal is right has not been measured. Use Authorization Code instead if the integration acts on behalf of a signed-in user, or if your RelativityOne instance already has a Code-flow OAuth2 Client for this integration." See [Confirming the flow](#confirming-the-flow) and the "Which flow" paragraph of CS-012 in [Part A](part-a-csharp.md#cs-012).

Source form:
```csharp
client.Headers[HttpRequestHeader.Authorization] = "Basic " + encoded;    // WebClient
request.Headers[HttpRequestHeader.Authorization] = "Basic " + encoded;   // HttpWebRequest
```

Under token authentication: **First, the destination:** if it is not RelativityOne, this finding is informational — that service's own token mechanism is the migration — and the rest of this template does not apply.

`"Bearer "` and the access token in place of `"Basic "` and the encoded pair, **set per request**:

```csharp
client.Headers[HttpRequestHeader.Authorization] = "Bearer " + accessToken;    // WebClient, before each call
request.Headers[HttpRequestHeader.Authorization] = "Bearer " + accessToken;   // HttpWebRequest
```

The code that produced `encoded` has no counterpart: a bearer token travels verbatim.

Credentials: **Where the credentials come from.** The destination decides whether the migration applies. If the destination is not RelativityOne, the migration is that service's own token mechanism, this finding is informational for this tool, and no migration to RelativityOne applies. If it is RelativityOne, the token comes from the token-acquisition part that follows.

**Where the token comes from.** Create an OAuth2 Client with Flow Grant Type set to Client Credentials (part 3 lists its settings), then send a form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=client_credentials`, `client_id`, `client_secret` and a `scope`. The token endpoint is **documented** for this grant. The token is then sent as `Authorization: Bearer <token>`, with `X-CSRF-Header: -` on REST and Kepler calls — the header is documented as required, and a Kepler call without it returns 404.

**The scope depends on what the integration needs, and no single scope is required.** Relativity's documentation shows two values for this grant: `SystemUserInfo` on the REST authentication page, the Usage Reports page and in the official Transfer SDK sample, and `UserInfoAccess` in the Kepler .NET samples. No page says which is correct, whether both work, or what either grants, and the OAuth2 Client's `Scopes` property is documented as one that cannot be set. Use the value the API you call documents, and confirm it.

**Cache the token, and re-acquire it on expiry — do not refresh.** Keep the token for its lifetime (part 3 recommends 60 minutes, Relativity's own wording) and request a new one with the client secret when it expires or a call is rejected for authentication. Client Credentials does not offer refresh tokens: **verified by NSerio against a live instance; not documented by Relativity** — the Allow Refresh Tokens control does not appear for this grant in the RelativityOne user interface. Relativity's own tables and text on refresh tokens conflict with each other, so re-acquisition with the secret is the advice that holds whichever way they are read.

**Long-running processes that use the Relativity SDK.** A `ServiceFactory` built once from a bearer token keeps that token after it expires. Rebuild the `ServiceFactory` and its proxies with a fresh token before expiry. This is the only advice consistent with the documentation: the package `Relativity.Kepler.Client.SDK` (documented) exposes a settable `Token` and an `HttpMessageHandler` overload but no token-provider overload, and no page documents renewing a token behind a factory.

**The client secret.** It is displayed once, at creation or reset. Store it as Secret Management (Part B of the Migration Documentation) describes; never in source, never in tracked configuration. A reset invalidates the previous secret immediately, so plan the cut-over before resetting it.

**Before deploying, confirm the calling identity.** Every call now runs as the OAuth2 Client's **Context User** — not as the user in the replaced credential, and for an import or a job as much as for a request. If that user has different workspace permissions or item-level security, the code returns fewer rows or `Unauthorized` in production with no change to its logic, and Relativity's audit attributes every call to that one account. Confirm the Context User's permissions match those of the previous identity. Relativity's REST API authentication page describes the Context User as usually a member of the Relativity Administrators group; the OAuth2 Client block (part 3) gives NSerio's recommendation, which differs.

**When the integration acts for a signed-in user.** Client Credentials is one service identity by construction. An integration that acts on behalf of a signed-in user is the Authorization Code case, and moving to it means a new OAuth2 Client, because the flow is fixed when a client is created. The finding's flow note says when to make that choice.

Verification: **Verification note — three things.**

**A token has a finite lifetime, and a header set once cannot be refreshed.** An `HttpWebRequest` is one request, so its header is already per request. A `WebClient` instance reused across calls is the long-lived case: set the header before each call with a current token, or the code works for the token's lifetime and then returns 401.

**The pair's encoding may be a separate finding.** Where `encoded` is built in the same file, the tool reports the encoding and this header as two findings at two locations. They are one migration, and both locations change together.

**Confirm the endpoint in use is the RelativityOne REST API.** That is the API whose bearer token this template describes; an endpoint elsewhere reached from the same code may accept only Basic and reject the migrated call.

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
<!-- generated:end CS-012 client-credentials -->

#### Authorization Code

<!-- generated:begin CS-012 authorization-code -->
**Confirming the flow.** An interactive-user signal in a file assigns Authorization Code, with this note: "An interactive-user signal in this file points to Authorization Code. Confirm the flow: use Client Credentials instead if the integration runs with credentials fixed for the integration and no signed-in user." Your user profile can also make Authorization Code the default; see Changing the default in [Confirming the flow](#confirming-the-flow). See [Confirming the flow](#confirming-the-flow) and the "Which flow" paragraph of CS-012 in [Part A](part-a-csharp.md#cs-012).

Source form:
```csharp
client.Headers[HttpRequestHeader.Authorization] = "Basic " + encoded;    // WebClient
request.Headers[HttpRequestHeader.Authorization] = "Basic " + encoded;   // HttpWebRequest
```

Under token authentication: **First, the destination:** if it is not RelativityOne, this finding is informational — that service's own token mechanism is the migration — and the rest of this template does not apply.

`"Bearer "` and the access token in place of `"Basic "` and the encoded pair, **set per request**:

```csharp
client.Headers[HttpRequestHeader.Authorization] = "Bearer " + accessToken;    // WebClient, before each call
request.Headers[HttpRequestHeader.Authorization] = "Bearer " + accessToken;   // HttpWebRequest
```

The code that produced `encoded` has no counterpart: a bearer token travels verbatim.

Credentials: **Where the credentials come from.** The destination decides whether the migration applies. If the destination is not RelativityOne, the migration is that service's own token mechanism, this finding is informational for this tool, and no migration to RelativityOne applies. If it is RelativityOne, the token comes from the token-acquisition part that follows.

**Common to both variants**

**Sign in.** Replace the code that collected the user's credentials with a redirect: send the user's browser to the authorize endpoint `<host>/Relativity/Identity/connect/authorize`, which leads to the RelativityOne login page. The authorize endpoint is **verified by NSerio against a live instance; not documented by Relativity**. The request carries the standard authorization-code parameters — `client_id`, `response_type=code`, the registered `redirect_uri`, a `scope`, and a `state` value the application checks on return; these are standard OAuth, and Relativity documents none of them. No discovery document is relied on.

**Receive the code.** The browser returns to the Redirect URL registered on the OAuth2 Client, with the authorization code and the `state` value. Check the `state` before using the code. The Redirect URL must be exactly the one registered (the variants say how).

**Exchange the code.** Send a form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=authorization_code`, the `code`, the same `redirect_uri`, the `client_id` and the `client_secret`, plus the `code_verifier` when PKCE is used. The code is exchanged at that endpoint, which Relativity documents for the Client Credentials grant only; the exchange is **verified by NSerio against a live instance; not documented by Relativity**.

**Use the token.** Send `Authorization: Bearer <access token>` — the change at the use site in part 1 — with `X-CSRF-Header: -` on REST and Kepler calls.

**On expiry.** With Allow Refresh Tokens enabled on the OAuth2 Client and PKCE used, the application holds a refresh token and, when the access token expires, sends a `grant_type=refresh_token` request with it and the client credentials in place of the code exchange — *inferred* from the standard OAuth shape, not tested. Without a refresh token the user signs in again. Relativity does not document a refresh token's lifetime, whether it rotates, how it is revoked, or whether a revocation endpoint exists, nor what disabling or deleting the client does to tokens already issued. A refresh token is a secret: store it as Secret Management (Part B of the Migration Documentation) describes, and on compromise disable the client and replace it. Enable refresh tokens only if the application must keep a session beyond one token lifetime without asking the user to sign in again.

**PKCE.** PKCE is required by Relativity's own documentation to obtain a refresh token, and not otherwise: a Code request that does not ask for a refresh token works without it, **verified by NSerio against a live instance; not documented by Relativity**. It is never a blanket requirement. NSerio recommends PKCE with the `S256` method in both variants (NSerio recommendation), and does not claim that Relativity accepts any particular method, which it does not document.

**Scope.** No scope set is prescribed for Code. The scope below works — `openid RelativityRequestOrigin UserInfo UserInfoAccess AuthenticationID RelativityWeb` — **verified by NSerio against a live instance; not documented by Relativity**, verified as working and not as minimal. `state` is sent as standard OAuth practice and `nonce` is not used; Relativity does not document whether it enforces either.

**The application stops collecting the password.** After this change the application never receives, stores or forwards the user's username and password: the user signs in on RelativityOne's own page, and the application holds only tokens. Remove the credential prompt, the form field and any storage of the password that the replaced code needed.

**Single sign-on.** Sign-in happens on RelativityOne's own login page, so the instance's single sign-on and two-factor rules apply unchanged — *inferred* from where the sign-in happens, and not documented for clients that customers create.

**The client secret is required.** Code does not work without a client secret: tested and rejected, **verified by NSerio against a live instance; not documented by Relativity**, which confirms Relativity's own statement that the grant requires one. It is displayed once and is stored as Secret Management (Part B of the Migration Documentation) describes.

**Server-side variant.** For a web application whose code runs on a server:

- **Redirect.** The application receives the code at one exact HTTPS URL per environment, a route of the application, with no wildcards (NSerio recommendation; Relativity's matching rules are not documented). The Relativity administrator registers the exact URL on the OAuth2 Client.
- **Secret and tokens.** The client secret stays on the server and never reaches the browser. The user's tokens are held server-side, keyed to the session, and never in the browser.
- **Exchange.** The server performs the code exchange itself.

**Desktop variant.** For a desktop or command-line application run by a user on their own machine:

- **The system browser.** Open the authorize URL in the user's default browser and not in a web view the application hosts, so the application never sees the password (NSerio recommendation).
- **A loopback redirect on a fixed port.** Listen on the loopback address only, on the registered port, for one request. The Redirect URL is `http://127.0.0.1:<port>/<path>`: a plain-HTTP loopback URL is accepted, **verified by NSerio against a live instance; not documented by Relativity**. The Relativity administrator registers that exact URL; because it is registered in advance, the port is fixed and not chosen at runtime.
- **The secret is extractable.** Code requires a client secret (above), so a distributed desktop application has to embed one, and **a secret shipped inside a distributed binary can be extracted by anyone who holds the binary**. It protects nothing against the person running the application.
- **Compensating guidance** (NSerio recommendation, subject to your own security policy): set Trusted IPs where the users' outbound addresses are known and stable — they restrict where a token can be *requested*, and are checked when a token is issued against the client's ID and secret; use the recommended short Access Token Lifetime of 60 minutes; enable refresh tokens only when needed; and create one OAuth2 Client per distribution, so a leaked secret can be disabled without affecting the others.
- **A known issue.** Relativity documents no recommendation for a distributed desktop application; this design is what NSerio verified works, and Relativity's own recommendation is tracked as a known issue of the release.

Verification: **Verification note — three things.**

**A token has a finite lifetime, and a header set once cannot be refreshed.** An `HttpWebRequest` is one request, so its header is already per request. A `WebClient` instance reused across calls is the long-lived case: set the header before each call with a current token, or the code works for the token's lifetime and then returns 401.

**The pair's encoding may be a separate finding.** Where `encoded` is built in the same file, the tool reports the encoding and this header as two findings at two locations. They are one migration, and both locations change together.

**Confirm the endpoint in use is the RelativityOne REST API.** That is the API whose bearer token this template describes; an endpoint elsewhere reached from the same code may accept only Basic and reject the migrated call.

OAuth2 Client: Create or confirm this OAuth2 Client in RelativityOne (NSerio's observation: the menu path is Home > Authentication > OAuth2 Client Manager, as Relativity's REST API authentication page names it). This tool never connects to RelativityOne and never creates or changes the OAuth2 Client; the settings below are guidance.
- Name: a descriptive name identifying the application it serves.
- Flow Grant Type: Code
- Redirect URLs: Relativity documentation: each URL includes the http or https protocol prefix. The exact callback URL is registered by the Relativity administrator. Server-side web applications, NSerio's recommendation: one exact HTTPS URL per environment, with no wildcards. Desktop and CLI applications: a plain-HTTP loopback URL, `http://127.0.0.1:<port>/<path>`, on a fixed port because the URL is registered in advance and the port cannot be chosen at runtime (verified by NSerio against a live instance; not documented by Relativity).
- Access Token Lifetime: 60 minutes, stated in minutes because the field is in minutes. Relativity documentation: tokens for this flow must have a short lifetime, and the recommended value matches the Identity Server default.
- Allow refresh tokens: NSerio's recommendation: leave it off unless the application must keep a user session active beyond the access token lifetime without asking the user to sign in again. If it is on, the refresh tokens are stored as secrets, per Secret Management (Part B of the Migration Documentation).
- Enabled: on.
<!-- generated:end CS-012 authorization-code -->

### CS-013 · WcfBasicTransportCredential

<a id="cs-013-migration"></a>

#### Client Credentials

<!-- generated:begin CS-013 client-credentials -->
**Confirming the flow.** When no signal in a file decides the flow, the tool assigns Client Credentials, the default, with this note: "Who supplies the credential at run time? If a person types it or signs in, the integration acts for that person: use Authorization Code. If it comes from configuration, settings, an environment variable or a secret store: use Client Credentials. No signal in this file showed which applies, so Client Credentials, the default, is recommended. Use Authorization Code instead if the integration acts on behalf of a signed-in user, or if your RelativityOne instance already has a Code-flow OAuth2 Client for this integration." A fixed-credential signal assigns it with this note: "A fixed-credential signal in this file points to Client Credentials, but how often that kind of signal is right has not been measured. Use Authorization Code instead if the integration acts on behalf of a signed-in user, or if your RelativityOne instance already has a Code-flow OAuth2 Client for this integration." See [Confirming the flow](#confirming-the-flow) and the "Which flow" paragraph of CS-013 in [Part A](part-a-csharp.md#cs-013).

Source form:
```csharp
binding.Security.Transport.ClientCredentialType = HttpClientCredentialType.Basic;
client.ClientCredentials.UserName.UserName = userName;
client.ClientCredentials.UserName.Password = password;
```

Under token authentication: **First, the destination:** if it is not RelativityOne, this finding is informational — that service's own token mechanism is the migration — and the rest of this template does not apply.

The binding's **transport credential type becomes the token form that binding supports**, and the username and password assignments are removed — from code, and from configuration where the credential lives there instead. No code form is given: which token form a binding and a service accept is decided by the service, not by the client.

**If the SOAP service is Relativity's, confirm it accepts bearer at all before changing the binding.** Where it does not, there is no scheme to migrate to on that service, and the migration is the move to a service that does.

Credentials: **Where the credentials come from.** The destination decides whether the migration applies. If the destination is not RelativityOne, the migration is that service's own token mechanism, this finding is informational for this tool, and no migration to RelativityOne applies. If it is RelativityOne, the token comes from the token-acquisition part that follows.

**Where the token comes from.** Create an OAuth2 Client with Flow Grant Type set to Client Credentials (part 3 lists its settings), then send a form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=client_credentials`, `client_id`, `client_secret` and a `scope`. The token endpoint is **documented** for this grant. The token is then sent as `Authorization: Bearer <token>`, with `X-CSRF-Header: -` on REST and Kepler calls — the header is documented as required, and a Kepler call without it returns 404.

**The scope depends on what the integration needs, and no single scope is required.** Relativity's documentation shows two values for this grant: `SystemUserInfo` on the REST authentication page, the Usage Reports page and in the official Transfer SDK sample, and `UserInfoAccess` in the Kepler .NET samples. No page says which is correct, whether both work, or what either grants, and the OAuth2 Client's `Scopes` property is documented as one that cannot be set. Use the value the API you call documents, and confirm it.

**Cache the token, and re-acquire it on expiry — do not refresh.** Keep the token for its lifetime (part 3 recommends 60 minutes, Relativity's own wording) and request a new one with the client secret when it expires or a call is rejected for authentication. Client Credentials does not offer refresh tokens: **verified by NSerio against a live instance; not documented by Relativity** — the Allow Refresh Tokens control does not appear for this grant in the RelativityOne user interface. Relativity's own tables and text on refresh tokens conflict with each other, so re-acquisition with the secret is the advice that holds whichever way they are read.

**Long-running processes that use the Relativity SDK.** A `ServiceFactory` built once from a bearer token keeps that token after it expires. Rebuild the `ServiceFactory` and its proxies with a fresh token before expiry. This is the only advice consistent with the documentation: the package `Relativity.Kepler.Client.SDK` (documented) exposes a settable `Token` and an `HttpMessageHandler` overload but no token-provider overload, and no page documents renewing a token behind a factory.

**The client secret.** It is displayed once, at creation or reset. Store it as Secret Management (Part B of the Migration Documentation) describes; never in source, never in tracked configuration. A reset invalidates the previous secret immediately, so plan the cut-over before resetting it.

**Before deploying, confirm the calling identity.** Every call now runs as the OAuth2 Client's **Context User** — not as the user in the replaced credential, and for an import or a job as much as for a request. If that user has different workspace permissions or item-level security, the code returns fewer rows or `Unauthorized` in production with no change to its logic, and Relativity's audit attributes every call to that one account. Confirm the Context User's permissions match those of the previous identity. Relativity's REST API authentication page describes the Context User as usually a member of the Relativity Administrators group; the OAuth2 Client block (part 3) gives NSerio's recommendation, which differs.

**When the integration acts for a signed-in user.** Client Credentials is one service identity by construction. An integration that acts on behalf of a signed-in user is the Authorization Code case, and moving to it means a new OAuth2 Client, because the flow is fixed when a client is created. The finding's flow note says when to make that choice.

Verification: **Verification note — three things.**

**A SOAP or WCF service may not accept bearer at all.** This is the first check, not the last: changing the binding against a service that accepts only Basic breaks a working integration.

**The credential may not be in this file.** The binding is what this finding reports; its username and password may live in configuration files, which this tool does not scan. A migration that changes the binding and leaves a configured password behind leaves the secret in place.

**A token has a finite lifetime.** A client proxy configured once and kept for the process's life holds a token that expires; within that lifetime the token on the proxy must be replaced, or the proxy recreated.

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
<!-- generated:end CS-013 client-credentials -->

#### Authorization Code

<!-- generated:begin CS-013 authorization-code -->
**Confirming the flow.** An interactive-user signal in a file assigns Authorization Code, with this note: "An interactive-user signal in this file points to Authorization Code. Confirm the flow: use Client Credentials instead if the integration runs with credentials fixed for the integration and no signed-in user." Your user profile can also make Authorization Code the default; see Changing the default in [Confirming the flow](#confirming-the-flow). See [Confirming the flow](#confirming-the-flow) and the "Which flow" paragraph of CS-013 in [Part A](part-a-csharp.md#cs-013).

Source form:
```csharp
binding.Security.Transport.ClientCredentialType = HttpClientCredentialType.Basic;
client.ClientCredentials.UserName.UserName = userName;
client.ClientCredentials.UserName.Password = password;
```

Under token authentication: **First, the destination:** if it is not RelativityOne, this finding is informational — that service's own token mechanism is the migration — and the rest of this template does not apply.

The binding's **transport credential type becomes the token form that binding supports**, and the username and password assignments are removed — from code, and from configuration where the credential lives there instead. No code form is given: which token form a binding and a service accept is decided by the service, not by the client.

**If the SOAP service is Relativity's, confirm it accepts bearer at all before changing the binding.** Where it does not, there is no scheme to migrate to on that service, and the migration is the move to a service that does.

Credentials: **Where the credentials come from.** The destination decides whether the migration applies. If the destination is not RelativityOne, the migration is that service's own token mechanism, this finding is informational for this tool, and no migration to RelativityOne applies. If it is RelativityOne, the token comes from the token-acquisition part that follows.

**Common to both variants**

**Sign in.** Replace the code that collected the user's credentials with a redirect: send the user's browser to the authorize endpoint `<host>/Relativity/Identity/connect/authorize`, which leads to the RelativityOne login page. The authorize endpoint is **verified by NSerio against a live instance; not documented by Relativity**. The request carries the standard authorization-code parameters — `client_id`, `response_type=code`, the registered `redirect_uri`, a `scope`, and a `state` value the application checks on return; these are standard OAuth, and Relativity documents none of them. No discovery document is relied on.

**Receive the code.** The browser returns to the Redirect URL registered on the OAuth2 Client, with the authorization code and the `state` value. Check the `state` before using the code. The Redirect URL must be exactly the one registered (the variants say how).

**Exchange the code.** Send a form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=authorization_code`, the `code`, the same `redirect_uri`, the `client_id` and the `client_secret`, plus the `code_verifier` when PKCE is used. The code is exchanged at that endpoint, which Relativity documents for the Client Credentials grant only; the exchange is **verified by NSerio against a live instance; not documented by Relativity**.

**Use the token.** Send `Authorization: Bearer <access token>` — the change at the use site in part 1 — with `X-CSRF-Header: -` on REST and Kepler calls.

**On expiry.** With Allow Refresh Tokens enabled on the OAuth2 Client and PKCE used, the application holds a refresh token and, when the access token expires, sends a `grant_type=refresh_token` request with it and the client credentials in place of the code exchange — *inferred* from the standard OAuth shape, not tested. Without a refresh token the user signs in again. Relativity does not document a refresh token's lifetime, whether it rotates, how it is revoked, or whether a revocation endpoint exists, nor what disabling or deleting the client does to tokens already issued. A refresh token is a secret: store it as Secret Management (Part B of the Migration Documentation) describes, and on compromise disable the client and replace it. Enable refresh tokens only if the application must keep a session beyond one token lifetime without asking the user to sign in again.

**PKCE.** PKCE is required by Relativity's own documentation to obtain a refresh token, and not otherwise: a Code request that does not ask for a refresh token works without it, **verified by NSerio against a live instance; not documented by Relativity**. It is never a blanket requirement. NSerio recommends PKCE with the `S256` method in both variants (NSerio recommendation), and does not claim that Relativity accepts any particular method, which it does not document.

**Scope.** No scope set is prescribed for Code. The scope below works — `openid RelativityRequestOrigin UserInfo UserInfoAccess AuthenticationID RelativityWeb` — **verified by NSerio against a live instance; not documented by Relativity**, verified as working and not as minimal. `state` is sent as standard OAuth practice and `nonce` is not used; Relativity does not document whether it enforces either.

**The application stops collecting the password.** After this change the application never receives, stores or forwards the user's username and password: the user signs in on RelativityOne's own page, and the application holds only tokens. Remove the credential prompt, the form field and any storage of the password that the replaced code needed.

**Single sign-on.** Sign-in happens on RelativityOne's own login page, so the instance's single sign-on and two-factor rules apply unchanged — *inferred* from where the sign-in happens, and not documented for clients that customers create.

**The client secret is required.** Code does not work without a client secret: tested and rejected, **verified by NSerio against a live instance; not documented by Relativity**, which confirms Relativity's own statement that the grant requires one. It is displayed once and is stored as Secret Management (Part B of the Migration Documentation) describes.

**Server-side variant.** For a web application whose code runs on a server:

- **Redirect.** The application receives the code at one exact HTTPS URL per environment, a route of the application, with no wildcards (NSerio recommendation; Relativity's matching rules are not documented). The Relativity administrator registers the exact URL on the OAuth2 Client.
- **Secret and tokens.** The client secret stays on the server and never reaches the browser. The user's tokens are held server-side, keyed to the session, and never in the browser.
- **Exchange.** The server performs the code exchange itself.

**Desktop variant.** For a desktop or command-line application run by a user on their own machine:

- **The system browser.** Open the authorize URL in the user's default browser and not in a web view the application hosts, so the application never sees the password (NSerio recommendation).
- **A loopback redirect on a fixed port.** Listen on the loopback address only, on the registered port, for one request. The Redirect URL is `http://127.0.0.1:<port>/<path>`: a plain-HTTP loopback URL is accepted, **verified by NSerio against a live instance; not documented by Relativity**. The Relativity administrator registers that exact URL; because it is registered in advance, the port is fixed and not chosen at runtime.
- **The secret is extractable.** Code requires a client secret (above), so a distributed desktop application has to embed one, and **a secret shipped inside a distributed binary can be extracted by anyone who holds the binary**. It protects nothing against the person running the application.
- **Compensating guidance** (NSerio recommendation, subject to your own security policy): set Trusted IPs where the users' outbound addresses are known and stable — they restrict where a token can be *requested*, and are checked when a token is issued against the client's ID and secret; use the recommended short Access Token Lifetime of 60 minutes; enable refresh tokens only when needed; and create one OAuth2 Client per distribution, so a leaked secret can be disabled without affecting the others.
- **A known issue.** Relativity documents no recommendation for a distributed desktop application; this design is what NSerio verified works, and Relativity's own recommendation is tracked as a known issue of the release.

Verification: **Verification note — three things.**

**A SOAP or WCF service may not accept bearer at all.** This is the first check, not the last: changing the binding against a service that accepts only Basic breaks a working integration.

**The credential may not be in this file.** The binding is what this finding reports; its username and password may live in configuration files, which this tool does not scan. A migration that changes the binding and leaves a configured password behind leaves the secret in place.

**A token has a finite lifetime.** A client proxy configured once and kept for the process's life holds a token that expires; within that lifetime the token on the proxy must be replaced, or the proxy recreated.

OAuth2 Client: Create or confirm this OAuth2 Client in RelativityOne (NSerio's observation: the menu path is Home > Authentication > OAuth2 Client Manager, as Relativity's REST API authentication page names it). This tool never connects to RelativityOne and never creates or changes the OAuth2 Client; the settings below are guidance.
- Name: a descriptive name identifying the application it serves.
- Flow Grant Type: Code
- Redirect URLs: Relativity documentation: each URL includes the http or https protocol prefix. The exact callback URL is registered by the Relativity administrator. Server-side web applications, NSerio's recommendation: one exact HTTPS URL per environment, with no wildcards. Desktop and CLI applications: a plain-HTTP loopback URL, `http://127.0.0.1:<port>/<path>`, on a fixed port because the URL is registered in advance and the port cannot be chosen at runtime (verified by NSerio against a live instance; not documented by Relativity).
- Access Token Lifetime: 60 minutes, stated in minutes because the field is in minutes. Relativity documentation: tokens for this flow must have a short lifetime, and the recommended value matches the Identity Server default.
- Allow refresh tokens: NSerio's recommendation: leave it off unless the application must keep a user session active beyond the access token lifetime without asking the user to sign in again. If it is on, the refresh tokens are stored as secrets, per Secret Management (Part B of the Migration Documentation).
- Enabled: on.
<!-- generated:end CS-013 authorization-code -->

### CS-014 · SchemeNamingExtensionMethod

<a id="cs-014-migration"></a>

#### Client Credentials

<!-- generated:begin CS-014 client-credentials -->
**Confirming the flow.** When no signal in a file decides the flow, the tool assigns Client Credentials, the default, with this note: "Who supplies the credential at run time? If a person types it or signs in, the integration acts for that person: use Authorization Code. If it comes from configuration, settings, an environment variable or a secret store: use Client Credentials. No signal in this file showed which applies, so Client Credentials, the default, is recommended. Use Authorization Code instead if the integration acts on behalf of a signed-in user, or if your RelativityOne instance already has a Code-flow OAuth2 Client for this integration." A fixed-credential signal assigns it with this note: "A fixed-credential signal in this file points to Client Credentials, but how often that kind of signal is right has not been measured. Use Authorization Code instead if the integration acts on behalf of a signed-in user, or if your RelativityOne instance already has a Code-flow OAuth2 Client for this integration." See [Confirming the flow](#confirming-the-flow) and the "Which flow" paragraph of CS-014 in [Part A](part-a-csharp.md#cs-014).

Source form:
```csharp
var authenticatedRequest = request.WithBasicAuth(userName, password);
```

Under token authentication: **First, the destination:** if it is not RelativityOne, this finding is informational — that service's own token mechanism is the migration — and the rest of this template does not apply.

Replace the Basic method with **the same library's bearer or token method, carrying the access token** in place of the credential pair. This template names no target method: the construct is a method whose name names the scheme, and the token method's name differs between libraries and versions.

Credentials: **Where the credentials come from.** The destination decides whether the migration applies. If the destination is not RelativityOne, the migration is that service's own token mechanism, this finding is informational for this tool, and no migration to RelativityOne applies. If it is RelativityOne, the token comes from the token-acquisition part that follows.

**Where the token comes from.** Create an OAuth2 Client with Flow Grant Type set to Client Credentials (part 3 lists its settings), then send a form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=client_credentials`, `client_id`, `client_secret` and a `scope`. The token endpoint is **documented** for this grant. The token is then sent as `Authorization: Bearer <token>`, with `X-CSRF-Header: -` on REST and Kepler calls — the header is documented as required, and a Kepler call without it returns 404.

**The scope depends on what the integration needs, and no single scope is required.** Relativity's documentation shows two values for this grant: `SystemUserInfo` on the REST authentication page, the Usage Reports page and in the official Transfer SDK sample, and `UserInfoAccess` in the Kepler .NET samples. No page says which is correct, whether both work, or what either grants, and the OAuth2 Client's `Scopes` property is documented as one that cannot be set. Use the value the API you call documents, and confirm it.

**Cache the token, and re-acquire it on expiry — do not refresh.** Keep the token for its lifetime (part 3 recommends 60 minutes, Relativity's own wording) and request a new one with the client secret when it expires or a call is rejected for authentication. Client Credentials does not offer refresh tokens: **verified by NSerio against a live instance; not documented by Relativity** — the Allow Refresh Tokens control does not appear for this grant in the RelativityOne user interface. Relativity's own tables and text on refresh tokens conflict with each other, so re-acquisition with the secret is the advice that holds whichever way they are read.

**Long-running processes that use the Relativity SDK.** A `ServiceFactory` built once from a bearer token keeps that token after it expires. Rebuild the `ServiceFactory` and its proxies with a fresh token before expiry. This is the only advice consistent with the documentation: the package `Relativity.Kepler.Client.SDK` (documented) exposes a settable `Token` and an `HttpMessageHandler` overload but no token-provider overload, and no page documents renewing a token behind a factory.

**The client secret.** It is displayed once, at creation or reset. Store it as Secret Management (Part B of the Migration Documentation) describes; never in source, never in tracked configuration. A reset invalidates the previous secret immediately, so plan the cut-over before resetting it.

**Before deploying, confirm the calling identity.** Every call now runs as the OAuth2 Client's **Context User** — not as the user in the replaced credential, and for an import or a job as much as for a request. If that user has different workspace permissions or item-level security, the code returns fewer rows or `Unauthorized` in production with no change to its logic, and Relativity's audit attributes every call to that one account. Confirm the Context User's permissions match those of the previous identity. Relativity's REST API authentication page describes the Context User as usually a member of the Relativity Administrators group; the OAuth2 Client block (part 3) gives NSerio's recommendation, which differs.

**When the integration acts for a signed-in user.** Client Credentials is one service identity by construction. An integration that acts on behalf of a signed-in user is the Authorization Code case, and moving to it means a new OAuth2 Client, because the flow is fixed when a client is created. The finding's flow note says when to make that choice.

Verification: **Verification note — three things.**

**Confirm the library version in use offers a token method.** Where it does not, this is a library upgrade before it is a code change.

**A token has a finite lifetime.** Applied to one request, the method is per request already. Applied once to a client or configuration shared across requests, it holds a token that expires, and the code works for the token's lifetime and then returns 401; pass a current token where the request is built.

**Confirm the endpoint in use is the RelativityOne REST API.** That is the API whose bearer token this template describes; an endpoint elsewhere reached from the same code may accept only Basic and reject the migrated call.

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
<!-- generated:end CS-014 client-credentials -->

#### Authorization Code

<!-- generated:begin CS-014 authorization-code -->
**Confirming the flow.** An interactive-user signal in a file assigns Authorization Code, with this note: "An interactive-user signal in this file points to Authorization Code. Confirm the flow: use Client Credentials instead if the integration runs with credentials fixed for the integration and no signed-in user." Your user profile can also make Authorization Code the default; see Changing the default in [Confirming the flow](#confirming-the-flow). See [Confirming the flow](#confirming-the-flow) and the "Which flow" paragraph of CS-014 in [Part A](part-a-csharp.md#cs-014).

Source form:
```csharp
var authenticatedRequest = request.WithBasicAuth(userName, password);
```

Under token authentication: **First, the destination:** if it is not RelativityOne, this finding is informational — that service's own token mechanism is the migration — and the rest of this template does not apply.

Replace the Basic method with **the same library's bearer or token method, carrying the access token** in place of the credential pair. This template names no target method: the construct is a method whose name names the scheme, and the token method's name differs between libraries and versions.

Credentials: **Where the credentials come from.** The destination decides whether the migration applies. If the destination is not RelativityOne, the migration is that service's own token mechanism, this finding is informational for this tool, and no migration to RelativityOne applies. If it is RelativityOne, the token comes from the token-acquisition part that follows.

**Common to both variants**

**Sign in.** Replace the code that collected the user's credentials with a redirect: send the user's browser to the authorize endpoint `<host>/Relativity/Identity/connect/authorize`, which leads to the RelativityOne login page. The authorize endpoint is **verified by NSerio against a live instance; not documented by Relativity**. The request carries the standard authorization-code parameters — `client_id`, `response_type=code`, the registered `redirect_uri`, a `scope`, and a `state` value the application checks on return; these are standard OAuth, and Relativity documents none of them. No discovery document is relied on.

**Receive the code.** The browser returns to the Redirect URL registered on the OAuth2 Client, with the authorization code and the `state` value. Check the `state` before using the code. The Redirect URL must be exactly the one registered (the variants say how).

**Exchange the code.** Send a form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=authorization_code`, the `code`, the same `redirect_uri`, the `client_id` and the `client_secret`, plus the `code_verifier` when PKCE is used. The code is exchanged at that endpoint, which Relativity documents for the Client Credentials grant only; the exchange is **verified by NSerio against a live instance; not documented by Relativity**.

**Use the token.** Send `Authorization: Bearer <access token>` — the change at the use site in part 1 — with `X-CSRF-Header: -` on REST and Kepler calls.

**On expiry.** With Allow Refresh Tokens enabled on the OAuth2 Client and PKCE used, the application holds a refresh token and, when the access token expires, sends a `grant_type=refresh_token` request with it and the client credentials in place of the code exchange — *inferred* from the standard OAuth shape, not tested. Without a refresh token the user signs in again. Relativity does not document a refresh token's lifetime, whether it rotates, how it is revoked, or whether a revocation endpoint exists, nor what disabling or deleting the client does to tokens already issued. A refresh token is a secret: store it as Secret Management (Part B of the Migration Documentation) describes, and on compromise disable the client and replace it. Enable refresh tokens only if the application must keep a session beyond one token lifetime without asking the user to sign in again.

**PKCE.** PKCE is required by Relativity's own documentation to obtain a refresh token, and not otherwise: a Code request that does not ask for a refresh token works without it, **verified by NSerio against a live instance; not documented by Relativity**. It is never a blanket requirement. NSerio recommends PKCE with the `S256` method in both variants (NSerio recommendation), and does not claim that Relativity accepts any particular method, which it does not document.

**Scope.** No scope set is prescribed for Code. The scope below works — `openid RelativityRequestOrigin UserInfo UserInfoAccess AuthenticationID RelativityWeb` — **verified by NSerio against a live instance; not documented by Relativity**, verified as working and not as minimal. `state` is sent as standard OAuth practice and `nonce` is not used; Relativity does not document whether it enforces either.

**The application stops collecting the password.** After this change the application never receives, stores or forwards the user's username and password: the user signs in on RelativityOne's own page, and the application holds only tokens. Remove the credential prompt, the form field and any storage of the password that the replaced code needed.

**Single sign-on.** Sign-in happens on RelativityOne's own login page, so the instance's single sign-on and two-factor rules apply unchanged — *inferred* from where the sign-in happens, and not documented for clients that customers create.

**The client secret is required.** Code does not work without a client secret: tested and rejected, **verified by NSerio against a live instance; not documented by Relativity**, which confirms Relativity's own statement that the grant requires one. It is displayed once and is stored as Secret Management (Part B of the Migration Documentation) describes.

**Server-side variant.** For a web application whose code runs on a server:

- **Redirect.** The application receives the code at one exact HTTPS URL per environment, a route of the application, with no wildcards (NSerio recommendation; Relativity's matching rules are not documented). The Relativity administrator registers the exact URL on the OAuth2 Client.
- **Secret and tokens.** The client secret stays on the server and never reaches the browser. The user's tokens are held server-side, keyed to the session, and never in the browser.
- **Exchange.** The server performs the code exchange itself.

**Desktop variant.** For a desktop or command-line application run by a user on their own machine:

- **The system browser.** Open the authorize URL in the user's default browser and not in a web view the application hosts, so the application never sees the password (NSerio recommendation).
- **A loopback redirect on a fixed port.** Listen on the loopback address only, on the registered port, for one request. The Redirect URL is `http://127.0.0.1:<port>/<path>`: a plain-HTTP loopback URL is accepted, **verified by NSerio against a live instance; not documented by Relativity**. The Relativity administrator registers that exact URL; because it is registered in advance, the port is fixed and not chosen at runtime.
- **The secret is extractable.** Code requires a client secret (above), so a distributed desktop application has to embed one, and **a secret shipped inside a distributed binary can be extracted by anyone who holds the binary**. It protects nothing against the person running the application.
- **Compensating guidance** (NSerio recommendation, subject to your own security policy): set Trusted IPs where the users' outbound addresses are known and stable — they restrict where a token can be *requested*, and are checked when a token is issued against the client's ID and secret; use the recommended short Access Token Lifetime of 60 minutes; enable refresh tokens only when needed; and create one OAuth2 Client per distribution, so a leaked secret can be disabled without affecting the others.
- **A known issue.** Relativity documents no recommendation for a distributed desktop application; this design is what NSerio verified works, and Relativity's own recommendation is tracked as a known issue of the release.

Verification: **Verification note — three things.**

**Confirm the library version in use offers a token method.** Where it does not, this is a library upgrade before it is a code change.

**A token has a finite lifetime.** Applied to one request, the method is per request already. Applied once to a client or configuration shared across requests, it holds a token that expires, and the code works for the token's lifetime and then returns 401; pass a current token where the request is built.

**Confirm the endpoint in use is the RelativityOne REST API.** That is the API whose bearer token this template describes; an endpoint elsewhere reached from the same code may accept only Basic and reject the migrated call.

OAuth2 Client: Create or confirm this OAuth2 Client in RelativityOne (NSerio's observation: the menu path is Home > Authentication > OAuth2 Client Manager, as Relativity's REST API authentication page names it). This tool never connects to RelativityOne and never creates or changes the OAuth2 Client; the settings below are guidance.
- Name: a descriptive name identifying the application it serves.
- Flow Grant Type: Code
- Redirect URLs: Relativity documentation: each URL includes the http or https protocol prefix. The exact callback URL is registered by the Relativity administrator. Server-side web applications, NSerio's recommendation: one exact HTTPS URL per environment, with no wildcards. Desktop and CLI applications: a plain-HTTP loopback URL, `http://127.0.0.1:<port>/<path>`, on a fixed port because the URL is registered in advance and the port cannot be chosen at runtime (verified by NSerio against a live instance; not documented by Relativity).
- Access Token Lifetime: 60 minutes, stated in minutes because the field is in minutes. Relativity documentation: tokens for this flow must have a short lifetime, and the recommended value matches the Identity Server default.
- Allow refresh tokens: NSerio's recommendation: leave it off unless the application must keep a user session active beyond the access token lifetime without asking the user to sign in again. If it is on, the refresh tokens are stored as secrets, per Secret Management (Part B of the Migration Documentation).
- Enabled: on.
<!-- generated:end CS-014 authorization-code -->

### CS-015 · SdkConstructorWithUserAndPassword

<a id="cs-015-migration"></a>

#### Client Credentials

<!-- generated:begin CS-015 client-credentials -->
**Confirming the flow.** When no signal in a file decides the flow, the tool assigns Client Credentials, the default, with this note: "Who supplies the credential at run time? If a person types it or signs in, the integration acts for that person: use Authorization Code. If it comes from configuration, settings, an environment variable or a secret store: use Client Credentials. No signal in this file showed which applies, so Client Credentials, the default, is recommended. Use Authorization Code instead if the integration acts on behalf of a signed-in user, or if your RelativityOne instance already has a Code-flow OAuth2 Client for this integration." A fixed-credential signal assigns it with this note: "A fixed-credential signal in this file points to Client Credentials, but how often that kind of signal is right has not been measured. Use Authorization Code instead if the integration acts on behalf of a signed-in user, or if your RelativityOne instance already has a Code-flow OAuth2 Client for this integration." See [Confirming the flow](#confirming-the-flow) and the "Which flow" paragraph of CS-015 in [Part A](part-a-csharp.md#cs-015).

Source form:
```csharp
var importApi = new ImportAPI(userName, password, webServiceUrl);
```

Under token authentication: **First, the destination:** if this Relativity instance is not RelativityOne — including a Relativity Server instance, whose own documentation governs — this finding is informational and the rest of this template does not apply.

**Change what the entry point receives, not the call site.** Relativity's Import API documentation names one static factory on the type, `ImportAPI.CreateByRsaBearerToken(webServiceUrl)`. **It takes the web service URL and no token**: it uses the bearer token of the identity of the process that runs it, and the documentation says it should be used only by processes hosted by the Relativity Service Account, such as an agent. Where the code runs in such a host, the construction above is replaced by:

```csharp
var importApi = ImportAPI.CreateByRsaBearerToken(webServiceUrl);
```

**For a standalone or desktop process that would obtain an OAuth2 token itself, this template gives no code form.** The documentation names no `ImportAPI` factory that accepts a token acquired by the caller, and it lists bearer-token authentication as not supported for a desktop application, where user name and password is the preferred form; that is a known issue of this release, listed in its release notes, and not something this text resolves. Whether the code that uses `importApi` afterwards needs any change is not established here; confirm it against the SDK's documentation for the version in use. The migrated construction must not keep the three-argument form with a credential pair, which is the one this finding reports.

Credentials: **Where the credentials come from.** The SDK type establishes that the destination is Relativity, not that it is RelativityOne; if it is not RelativityOne — including a Relativity Server instance, whose own documentation governs — the migration is that service's own token mechanism, this finding is informational for this tool, and no migration to RelativityOne applies. If it is RelativityOne, the factory above carries no token of the code's own: the identity comes from the hosting process, so the token-acquisition part that follows applies only to code that obtains a token itself, and for the entry point that case is the known issue stated above.

**Where the token comes from.** Create an OAuth2 Client with Flow Grant Type set to Client Credentials (part 3 lists its settings), then send a form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=client_credentials`, `client_id`, `client_secret` and a `scope`. The token endpoint is **documented** for this grant. The token is then sent as `Authorization: Bearer <token>`, with `X-CSRF-Header: -` on REST and Kepler calls — the header is documented as required, and a Kepler call without it returns 404.

**The scope depends on what the integration needs, and no single scope is required.** Relativity's documentation shows two values for this grant: `SystemUserInfo` on the REST authentication page, the Usage Reports page and in the official Transfer SDK sample, and `UserInfoAccess` in the Kepler .NET samples. No page says which is correct, whether both work, or what either grants, and the OAuth2 Client's `Scopes` property is documented as one that cannot be set. Use the value the API you call documents, and confirm it.

**Cache the token, and re-acquire it on expiry — do not refresh.** Keep the token for its lifetime (part 3 recommends 60 minutes, Relativity's own wording) and request a new one with the client secret when it expires or a call is rejected for authentication. Client Credentials does not offer refresh tokens: **verified by NSerio against a live instance; not documented by Relativity** — the Allow Refresh Tokens control does not appear for this grant in the RelativityOne user interface. Relativity's own tables and text on refresh tokens conflict with each other, so re-acquisition with the secret is the advice that holds whichever way they are read.

**Long-running processes that use the Relativity SDK.** A `ServiceFactory` built once from a bearer token keeps that token after it expires. Rebuild the `ServiceFactory` and its proxies with a fresh token before expiry. This is the only advice consistent with the documentation: the package `Relativity.Kepler.Client.SDK` (documented) exposes a settable `Token` and an `HttpMessageHandler` overload but no token-provider overload, and no page documents renewing a token behind a factory.

**The client secret.** It is displayed once, at creation or reset. Store it as Secret Management (Part B of the Migration Documentation) describes; never in source, never in tracked configuration. A reset invalidates the previous secret immediately, so plan the cut-over before resetting it.

**Before deploying, confirm the calling identity.** Every call now runs as the OAuth2 Client's **Context User** — not as the user in the replaced credential, and for an import or a job as much as for a request. If that user has different workspace permissions or item-level security, the code returns fewer rows or `Unauthorized` in production with no change to its logic, and Relativity's audit attributes every call to that one account. Confirm the Context User's permissions match those of the previous identity. Relativity's REST API authentication page describes the Context User as usually a member of the Relativity Administrators group; the OAuth2 Client block (part 3) gives NSerio's recommendation, which differs.

**When the integration acts for a signed-in user.** Client Credentials is one service identity by construction. An integration that acts on behalf of a signed-in user is the Authorization Code case, and moving to it means a new OAuth2 Client, because the flow is fixed when a client is created. The finding's flow note says when to make that choice.

Verification: **Verification note — two things, and the first can break working code.**

**The factory is documented for hosted processes only, and for Relativity Server.** It is meant for an agent or another process hosted by the Relativity Service Account; a process that runs elsewhere has no such identity to hand it. The two documentation pages that describe it are Relativity Server pages, and Relativity's RelativityOne documentation does not describe this class, so confirm that the SDK version in use for RelativityOne exposes it before relying on it.

**Confirm the SDK version in use exposes `CreateByRsaBearerToken`.** Where it does not, this is an SDK upgrade before it is a code change. The documentation states that the factory handles authentication and token expiration itself, so the finite-lifetime concern of the other templates is the factory's to manage here, not the code's.

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
<!-- generated:end CS-015 client-credentials -->

#### Authorization Code

<!-- generated:begin CS-015 authorization-code -->
**Confirming the flow.** An interactive-user signal in a file assigns Authorization Code, with this note: "An interactive-user signal in this file points to Authorization Code. Confirm the flow: use Client Credentials instead if the integration runs with credentials fixed for the integration and no signed-in user." Your user profile can also make Authorization Code the default; see Changing the default in [Confirming the flow](#confirming-the-flow). See [Confirming the flow](#confirming-the-flow) and the "Which flow" paragraph of CS-015 in [Part A](part-a-csharp.md#cs-015).

Source form:
```csharp
var importApi = new ImportAPI(userName, password, webServiceUrl);
```

Under token authentication: **First, the destination:** if this Relativity instance is not RelativityOne — including a Relativity Server instance, whose own documentation governs — this finding is informational and the rest of this template does not apply.

**Change what the entry point receives, not the call site.** Relativity's Import API documentation names one static factory on the type, `ImportAPI.CreateByRsaBearerToken(webServiceUrl)`. **It takes the web service URL and no token**: it uses the bearer token of the identity of the process that runs it, and the documentation says it should be used only by processes hosted by the Relativity Service Account, such as an agent. Where the code runs in such a host, the construction above is replaced by:

```csharp
var importApi = ImportAPI.CreateByRsaBearerToken(webServiceUrl);
```

**For a standalone or desktop process that would obtain an OAuth2 token itself, this template gives no code form.** The documentation names no `ImportAPI` factory that accepts a token acquired by the caller, and it lists bearer-token authentication as not supported for a desktop application, where user name and password is the preferred form; that is a known issue of this release, listed in its release notes, and not something this text resolves. Whether the code that uses `importApi` afterwards needs any change is not established here; confirm it against the SDK's documentation for the version in use. The migrated construction must not keep the three-argument form with a credential pair, which is the one this finding reports.

Credentials: **Where the credentials come from.** The SDK type establishes that the destination is Relativity, not that it is RelativityOne; if it is not RelativityOne — including a Relativity Server instance, whose own documentation governs — the migration is that service's own token mechanism, this finding is informational for this tool, and no migration to RelativityOne applies. If it is RelativityOne, the factory above carries no token of the code's own: the identity comes from the hosting process, so the token-acquisition part that follows applies only to code that obtains a token itself, and for the entry point that case is the known issue stated above.

**Common to both variants**

**Sign in.** Replace the code that collected the user's credentials with a redirect: send the user's browser to the authorize endpoint `<host>/Relativity/Identity/connect/authorize`, which leads to the RelativityOne login page. The authorize endpoint is **verified by NSerio against a live instance; not documented by Relativity**. The request carries the standard authorization-code parameters — `client_id`, `response_type=code`, the registered `redirect_uri`, a `scope`, and a `state` value the application checks on return; these are standard OAuth, and Relativity documents none of them. No discovery document is relied on.

**Receive the code.** The browser returns to the Redirect URL registered on the OAuth2 Client, with the authorization code and the `state` value. Check the `state` before using the code. The Redirect URL must be exactly the one registered (the variants say how).

**Exchange the code.** Send a form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=authorization_code`, the `code`, the same `redirect_uri`, the `client_id` and the `client_secret`, plus the `code_verifier` when PKCE is used. The code is exchanged at that endpoint, which Relativity documents for the Client Credentials grant only; the exchange is **verified by NSerio against a live instance; not documented by Relativity**.

**Use the token.** Send `Authorization: Bearer <access token>` — the change at the use site in part 1 — with `X-CSRF-Header: -` on REST and Kepler calls.

**On expiry.** With Allow Refresh Tokens enabled on the OAuth2 Client and PKCE used, the application holds a refresh token and, when the access token expires, sends a `grant_type=refresh_token` request with it and the client credentials in place of the code exchange — *inferred* from the standard OAuth shape, not tested. Without a refresh token the user signs in again. Relativity does not document a refresh token's lifetime, whether it rotates, how it is revoked, or whether a revocation endpoint exists, nor what disabling or deleting the client does to tokens already issued. A refresh token is a secret: store it as Secret Management (Part B of the Migration Documentation) describes, and on compromise disable the client and replace it. Enable refresh tokens only if the application must keep a session beyond one token lifetime without asking the user to sign in again.

**PKCE.** PKCE is required by Relativity's own documentation to obtain a refresh token, and not otherwise: a Code request that does not ask for a refresh token works without it, **verified by NSerio against a live instance; not documented by Relativity**. It is never a blanket requirement. NSerio recommends PKCE with the `S256` method in both variants (NSerio recommendation), and does not claim that Relativity accepts any particular method, which it does not document.

**Scope.** No scope set is prescribed for Code. The scope below works — `openid RelativityRequestOrigin UserInfo UserInfoAccess AuthenticationID RelativityWeb` — **verified by NSerio against a live instance; not documented by Relativity**, verified as working and not as minimal. `state` is sent as standard OAuth practice and `nonce` is not used; Relativity does not document whether it enforces either.

**The application stops collecting the password.** After this change the application never receives, stores or forwards the user's username and password: the user signs in on RelativityOne's own page, and the application holds only tokens. Remove the credential prompt, the form field and any storage of the password that the replaced code needed.

**Single sign-on.** Sign-in happens on RelativityOne's own login page, so the instance's single sign-on and two-factor rules apply unchanged — *inferred* from where the sign-in happens, and not documented for clients that customers create.

**The client secret is required.** Code does not work without a client secret: tested and rejected, **verified by NSerio against a live instance; not documented by Relativity**, which confirms Relativity's own statement that the grant requires one. It is displayed once and is stored as Secret Management (Part B of the Migration Documentation) describes.

**Server-side variant.** For a web application whose code runs on a server:

- **Redirect.** The application receives the code at one exact HTTPS URL per environment, a route of the application, with no wildcards (NSerio recommendation; Relativity's matching rules are not documented). The Relativity administrator registers the exact URL on the OAuth2 Client.
- **Secret and tokens.** The client secret stays on the server and never reaches the browser. The user's tokens are held server-side, keyed to the session, and never in the browser.
- **Exchange.** The server performs the code exchange itself.

**Desktop variant.** For a desktop or command-line application run by a user on their own machine:

- **The system browser.** Open the authorize URL in the user's default browser and not in a web view the application hosts, so the application never sees the password (NSerio recommendation).
- **A loopback redirect on a fixed port.** Listen on the loopback address only, on the registered port, for one request. The Redirect URL is `http://127.0.0.1:<port>/<path>`: a plain-HTTP loopback URL is accepted, **verified by NSerio against a live instance; not documented by Relativity**. The Relativity administrator registers that exact URL; because it is registered in advance, the port is fixed and not chosen at runtime.
- **The secret is extractable.** Code requires a client secret (above), so a distributed desktop application has to embed one, and **a secret shipped inside a distributed binary can be extracted by anyone who holds the binary**. It protects nothing against the person running the application.
- **Compensating guidance** (NSerio recommendation, subject to your own security policy): set Trusted IPs where the users' outbound addresses are known and stable — they restrict where a token can be *requested*, and are checked when a token is issued against the client's ID and secret; use the recommended short Access Token Lifetime of 60 minutes; enable refresh tokens only when needed; and create one OAuth2 Client per distribution, so a leaked secret can be disabled without affecting the others.
- **A known issue.** Relativity documents no recommendation for a distributed desktop application; this design is what NSerio verified works, and Relativity's own recommendation is tracked as a known issue of the release.

Verification: **Verification note — two things, and the first can break working code.**

**The factory is documented for hosted processes only, and for Relativity Server.** It is meant for an agent or another process hosted by the Relativity Service Account; a process that runs elsewhere has no such identity to hand it. The two documentation pages that describe it are Relativity Server pages, and Relativity's RelativityOne documentation does not describe this class, so confirm that the SDK version in use for RelativityOne exposes it before relying on it.

**Confirm the SDK version in use exposes `CreateByRsaBearerToken`.** Where it does not, this is an SDK upgrade before it is a code change. The documentation states that the factory handles authentication and token expiration itself, so the finite-lifetime concern of the other templates is the factory's to manage here, not the code's.

OAuth2 Client: Create or confirm this OAuth2 Client in RelativityOne (NSerio's observation: the menu path is Home > Authentication > OAuth2 Client Manager, as Relativity's REST API authentication page names it). This tool never connects to RelativityOne and never creates or changes the OAuth2 Client; the settings below are guidance.
- Name: a descriptive name identifying the application it serves.
- Flow Grant Type: Code
- Redirect URLs: Relativity documentation: each URL includes the http or https protocol prefix. The exact callback URL is registered by the Relativity administrator. Server-side web applications, NSerio's recommendation: one exact HTTPS URL per environment, with no wildcards. Desktop and CLI applications: a plain-HTTP loopback URL, `http://127.0.0.1:<port>/<path>`, on a fixed port because the URL is registered in advance and the port cannot be chosen at runtime (verified by NSerio against a live instance; not documented by Relativity).
- Access Token Lifetime: 60 minutes, stated in minutes because the field is in minutes. Relativity documentation: tokens for this flow must have a short lifetime, and the recommended value matches the Identity Server default.
- Allow refresh tokens: NSerio's recommendation: leave it off unless the application must keep a user session active beyond the access token lifetime without asking the user to sign in again. If it is on, the refresh tokens are stored as secrets, per Secret Management (Part B of the Migration Documentation).
- Enabled: on.
<!-- generated:end CS-015 authorization-code -->

### CS-016 · CredentialAssembledAcrossStatements

<a id="cs-016-migration"></a>

#### Client Credentials

<!-- generated:begin CS-016 client-credentials -->
**Confirming the flow.** When no signal in a file decides the flow, the tool assigns Client Credentials, the default, with this note: "Who supplies the credential at run time? If a person types it or signs in, the integration acts for that person: use Authorization Code. If it comes from configuration, settings, an environment variable or a secret store: use Client Credentials. No signal in this file showed which applies, so Client Credentials, the default, is recommended. Use Authorization Code instead if the integration acts on behalf of a signed-in user, or if your RelativityOne instance already has a Code-flow OAuth2 Client for this integration." A fixed-credential signal assigns it with this note: "A fixed-credential signal in this file points to Client Credentials, but how often that kind of signal is right has not been measured. Use Authorization Code instead if the integration acts on behalf of a signed-in user, or if your RelativityOne instance already has a Code-flow OAuth2 Client for this integration." See [Confirming the flow](#confirming-the-flow) and the "Which flow" paragraph of CS-016 in [Part A](part-a-csharp.md#cs-016).

Source form:
```csharp
NetworkCredential credential = new NetworkCredential { UserName = userName };
credential.Password = password;
```

Under token authentication: **First, the destination:** if it is not RelativityOne, this finding is informational — that service's own token mechanism is the migration — and the rest of this template does not apply.

**Identify the destination first**, because a credential assembled across statements is usually consumed somewhere other than where it is built. Then **remove the credential from its consumer and set the bearer header on each request** that consumer sends:

```csharp
request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
```

Credentials: **Where the credentials come from.** The destination decides whether the migration applies. If the destination is not RelativityOne, the migration is that service's own token mechanism, this finding is informational for this tool, and no migration to RelativityOne applies. If it is RelativityOne, the token comes from the token-acquisition part that follows.

**Where the token comes from.** Create an OAuth2 Client with Flow Grant Type set to Client Credentials (part 3 lists its settings), then send a form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=client_credentials`, `client_id`, `client_secret` and a `scope`. The token endpoint is **documented** for this grant. The token is then sent as `Authorization: Bearer <token>`, with `X-CSRF-Header: -` on REST and Kepler calls — the header is documented as required, and a Kepler call without it returns 404.

**The scope depends on what the integration needs, and no single scope is required.** Relativity's documentation shows two values for this grant: `SystemUserInfo` on the REST authentication page, the Usage Reports page and in the official Transfer SDK sample, and `UserInfoAccess` in the Kepler .NET samples. No page says which is correct, whether both work, or what either grants, and the OAuth2 Client's `Scopes` property is documented as one that cannot be set. Use the value the API you call documents, and confirm it.

**Cache the token, and re-acquire it on expiry — do not refresh.** Keep the token for its lifetime (part 3 recommends 60 minutes, Relativity's own wording) and request a new one with the client secret when it expires or a call is rejected for authentication. Client Credentials does not offer refresh tokens: **verified by NSerio against a live instance; not documented by Relativity** — the Allow Refresh Tokens control does not appear for this grant in the RelativityOne user interface. Relativity's own tables and text on refresh tokens conflict with each other, so re-acquisition with the secret is the advice that holds whichever way they are read.

**Long-running processes that use the Relativity SDK.** A `ServiceFactory` built once from a bearer token keeps that token after it expires. Rebuild the `ServiceFactory` and its proxies with a fresh token before expiry. This is the only advice consistent with the documentation: the package `Relativity.Kepler.Client.SDK` (documented) exposes a settable `Token` and an `HttpMessageHandler` overload but no token-provider overload, and no page documents renewing a token behind a factory.

**The client secret.** It is displayed once, at creation or reset. Store it as Secret Management (Part B of the Migration Documentation) describes; never in source, never in tracked configuration. A reset invalidates the previous secret immediately, so plan the cut-over before resetting it.

**Before deploying, confirm the calling identity.** Every call now runs as the OAuth2 Client's **Context User** — not as the user in the replaced credential, and for an import or a job as much as for a request. If that user has different workspace permissions or item-level security, the code returns fewer rows or `Unauthorized` in production with no change to its logic, and Relativity's audit attributes every call to that one account. Confirm the Context User's permissions match those of the previous identity. Relativity's REST API authentication page describes the Context User as usually a member of the Relativity Administrators group; the OAuth2 Client block (part 3) gives NSerio's recommendation, which differs.

**When the integration acts for a signed-in user.** Client Credentials is one service identity by construction. An integration that acts on behalf of a signed-in user is the Authorization Code case, and moving to it means a new OAuth2 Client, because the flow is fixed when a client is created. The finding's flow note says when to make that choice.

Verification: **Verification note — six things.**

**Confirm the consumer is an HTTP destination.** The same form bound to a mail, file-transfer or directory client is not a migration target for this tool, and the construct alone does not show which consumer it has.

**Confirm the exchange was Basic.** `NetworkCredential` declares no scheme and serves integrated Windows authentication as readily as Basic; if the exchange is integrated, the migration does not apply.

**The 401-challenge response is lost** wherever the consumer answered a challenge with this credential; a bearer header is sent up front and answers nothing.

**Redirects stop carrying the credential.** Depending on the runtime and the credential type, a consumer's credentials may be re-applied to a redirect target; a manually set `Authorization` header never is. .NET documents that `HttpWebRequest` clears the `Authorization` header when it follows a redirect automatically, and the `HttpClient` handler drops it on redirect. Code that relied on a redirect can start returning 401 after the migration; address the final URI, or handle the redirect explicitly.

**A token has a finite lifetime, and a header set once on a long-lived client cannot be refreshed.** Set it per request, or the code works for the token's lifetime and then returns 401 until the process restarts.

**Confirm the endpoint in use is the RelativityOne REST API.** That is the API whose bearer token this template describes; an endpoint elsewhere reached from the same code may accept only Basic and reject the migrated call.

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
<!-- generated:end CS-016 client-credentials -->

#### Authorization Code

<!-- generated:begin CS-016 authorization-code -->
**Confirming the flow.** An interactive-user signal in a file assigns Authorization Code, with this note: "An interactive-user signal in this file points to Authorization Code. Confirm the flow: use Client Credentials instead if the integration runs with credentials fixed for the integration and no signed-in user." Your user profile can also make Authorization Code the default; see Changing the default in [Confirming the flow](#confirming-the-flow). See [Confirming the flow](#confirming-the-flow) and the "Which flow" paragraph of CS-016 in [Part A](part-a-csharp.md#cs-016).

Source form:
```csharp
NetworkCredential credential = new NetworkCredential { UserName = userName };
credential.Password = password;
```

Under token authentication: **First, the destination:** if it is not RelativityOne, this finding is informational — that service's own token mechanism is the migration — and the rest of this template does not apply.

**Identify the destination first**, because a credential assembled across statements is usually consumed somewhere other than where it is built. Then **remove the credential from its consumer and set the bearer header on each request** that consumer sends:

```csharp
request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
```

Credentials: **Where the credentials come from.** The destination decides whether the migration applies. If the destination is not RelativityOne, the migration is that service's own token mechanism, this finding is informational for this tool, and no migration to RelativityOne applies. If it is RelativityOne, the token comes from the token-acquisition part that follows.

**Common to both variants**

**Sign in.** Replace the code that collected the user's credentials with a redirect: send the user's browser to the authorize endpoint `<host>/Relativity/Identity/connect/authorize`, which leads to the RelativityOne login page. The authorize endpoint is **verified by NSerio against a live instance; not documented by Relativity**. The request carries the standard authorization-code parameters — `client_id`, `response_type=code`, the registered `redirect_uri`, a `scope`, and a `state` value the application checks on return; these are standard OAuth, and Relativity documents none of them. No discovery document is relied on.

**Receive the code.** The browser returns to the Redirect URL registered on the OAuth2 Client, with the authorization code and the `state` value. Check the `state` before using the code. The Redirect URL must be exactly the one registered (the variants say how).

**Exchange the code.** Send a form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=authorization_code`, the `code`, the same `redirect_uri`, the `client_id` and the `client_secret`, plus the `code_verifier` when PKCE is used. The code is exchanged at that endpoint, which Relativity documents for the Client Credentials grant only; the exchange is **verified by NSerio against a live instance; not documented by Relativity**.

**Use the token.** Send `Authorization: Bearer <access token>` — the change at the use site in part 1 — with `X-CSRF-Header: -` on REST and Kepler calls.

**On expiry.** With Allow Refresh Tokens enabled on the OAuth2 Client and PKCE used, the application holds a refresh token and, when the access token expires, sends a `grant_type=refresh_token` request with it and the client credentials in place of the code exchange — *inferred* from the standard OAuth shape, not tested. Without a refresh token the user signs in again. Relativity does not document a refresh token's lifetime, whether it rotates, how it is revoked, or whether a revocation endpoint exists, nor what disabling or deleting the client does to tokens already issued. A refresh token is a secret: store it as Secret Management (Part B of the Migration Documentation) describes, and on compromise disable the client and replace it. Enable refresh tokens only if the application must keep a session beyond one token lifetime without asking the user to sign in again.

**PKCE.** PKCE is required by Relativity's own documentation to obtain a refresh token, and not otherwise: a Code request that does not ask for a refresh token works without it, **verified by NSerio against a live instance; not documented by Relativity**. It is never a blanket requirement. NSerio recommends PKCE with the `S256` method in both variants (NSerio recommendation), and does not claim that Relativity accepts any particular method, which it does not document.

**Scope.** No scope set is prescribed for Code. The scope below works — `openid RelativityRequestOrigin UserInfo UserInfoAccess AuthenticationID RelativityWeb` — **verified by NSerio against a live instance; not documented by Relativity**, verified as working and not as minimal. `state` is sent as standard OAuth practice and `nonce` is not used; Relativity does not document whether it enforces either.

**The application stops collecting the password.** After this change the application never receives, stores or forwards the user's username and password: the user signs in on RelativityOne's own page, and the application holds only tokens. Remove the credential prompt, the form field and any storage of the password that the replaced code needed.

**Single sign-on.** Sign-in happens on RelativityOne's own login page, so the instance's single sign-on and two-factor rules apply unchanged — *inferred* from where the sign-in happens, and not documented for clients that customers create.

**The client secret is required.** Code does not work without a client secret: tested and rejected, **verified by NSerio against a live instance; not documented by Relativity**, which confirms Relativity's own statement that the grant requires one. It is displayed once and is stored as Secret Management (Part B of the Migration Documentation) describes.

**Server-side variant.** For a web application whose code runs on a server:

- **Redirect.** The application receives the code at one exact HTTPS URL per environment, a route of the application, with no wildcards (NSerio recommendation; Relativity's matching rules are not documented). The Relativity administrator registers the exact URL on the OAuth2 Client.
- **Secret and tokens.** The client secret stays on the server and never reaches the browser. The user's tokens are held server-side, keyed to the session, and never in the browser.
- **Exchange.** The server performs the code exchange itself.

**Desktop variant.** For a desktop or command-line application run by a user on their own machine:

- **The system browser.** Open the authorize URL in the user's default browser and not in a web view the application hosts, so the application never sees the password (NSerio recommendation).
- **A loopback redirect on a fixed port.** Listen on the loopback address only, on the registered port, for one request. The Redirect URL is `http://127.0.0.1:<port>/<path>`: a plain-HTTP loopback URL is accepted, **verified by NSerio against a live instance; not documented by Relativity**. The Relativity administrator registers that exact URL; because it is registered in advance, the port is fixed and not chosen at runtime.
- **The secret is extractable.** Code requires a client secret (above), so a distributed desktop application has to embed one, and **a secret shipped inside a distributed binary can be extracted by anyone who holds the binary**. It protects nothing against the person running the application.
- **Compensating guidance** (NSerio recommendation, subject to your own security policy): set Trusted IPs where the users' outbound addresses are known and stable — they restrict where a token can be *requested*, and are checked when a token is issued against the client's ID and secret; use the recommended short Access Token Lifetime of 60 minutes; enable refresh tokens only when needed; and create one OAuth2 Client per distribution, so a leaked secret can be disabled without affecting the others.
- **A known issue.** Relativity documents no recommendation for a distributed desktop application; this design is what NSerio verified works, and Relativity's own recommendation is tracked as a known issue of the release.

Verification: **Verification note — six things.**

**Confirm the consumer is an HTTP destination.** The same form bound to a mail, file-transfer or directory client is not a migration target for this tool, and the construct alone does not show which consumer it has.

**Confirm the exchange was Basic.** `NetworkCredential` declares no scheme and serves integrated Windows authentication as readily as Basic; if the exchange is integrated, the migration does not apply.

**The 401-challenge response is lost** wherever the consumer answered a challenge with this credential; a bearer header is sent up front and answers nothing.

**Redirects stop carrying the credential.** Depending on the runtime and the credential type, a consumer's credentials may be re-applied to a redirect target; a manually set `Authorization` header never is. .NET documents that `HttpWebRequest` clears the `Authorization` header when it follows a redirect automatically, and the `HttpClient` handler drops it on redirect. Code that relied on a redirect can start returning 401 after the migration; address the final URI, or handle the redirect explicitly.

**A token has a finite lifetime, and a header set once on a long-lived client cannot be refreshed.** Set it per request, or the code works for the token's lifetime and then returns 401 until the process restarts.

**Confirm the endpoint in use is the RelativityOne REST API.** That is the API whose bearer token this template describes; an endpoint elsewhere reached from the same code may accept only Basic and reject the migrated call.

OAuth2 Client: Create or confirm this OAuth2 Client in RelativityOne (NSerio's observation: the menu path is Home > Authentication > OAuth2 Client Manager, as Relativity's REST API authentication page names it). This tool never connects to RelativityOne and never creates or changes the OAuth2 Client; the settings below are guidance.
- Name: a descriptive name identifying the application it serves.
- Flow Grant Type: Code
- Redirect URLs: Relativity documentation: each URL includes the http or https protocol prefix. The exact callback URL is registered by the Relativity administrator. Server-side web applications, NSerio's recommendation: one exact HTTPS URL per environment, with no wildcards. Desktop and CLI applications: a plain-HTTP loopback URL, `http://127.0.0.1:<port>/<path>`, on a fixed port because the URL is registered in advance and the port cannot be chosen at runtime (verified by NSerio against a live instance; not documented by Relativity).
- Access Token Lifetime: 60 minutes, stated in minutes because the field is in minutes. Relativity documentation: tokens for this flow must have a short lifetime, and the recommended value matches the Identity Server default.
- Allow refresh tokens: NSerio's recommendation: leave it off unless the application must keep a user session active beyond the access token lifetime without asking the user to sign in again. If it is on, the refresh tokens are stored as secrets, per Secret Management (Part B of the Migration Documentation).
- Enabled: on.
<!-- generated:end CS-016 authorization-code -->

### CS-017 · SdkSettingsCredentialProperties

<a id="cs-017-migration"></a>

#### Client Credentials

<!-- generated:begin CS-017 client-credentials -->
**Confirming the flow.** When no signal in a file decides the flow, the tool assigns Client Credentials, the default, with this note: "Who supplies the credential at run time? If a person types it or signs in, the integration acts for that person: use Authorization Code. If it comes from configuration, settings, an environment variable or a secret store: use Client Credentials. No signal in this file showed which applies, so Client Credentials, the default, is recommended. Use Authorization Code instead if the integration acts on behalf of a signed-in user, or if your RelativityOne instance already has a Code-flow OAuth2 Client for this integration." A fixed-credential signal assigns it with this note: "A fixed-credential signal in this file points to Client Credentials, but how often that kind of signal is right has not been measured. Use Authorization Code instead if the integration acts on behalf of a signed-in user, or if your RelativityOne instance already has a Code-flow OAuth2 Client for this integration." See [Confirming the flow](#confirming-the-flow) and the "Which flow" paragraph of CS-017 in [Part A](part-a-csharp.md#cs-017).

Source form:
```csharp
importJob.Settings.RelativityUsername = userName;
importJob.Settings.RelativityPassword = password;
```

Under token authentication: **First, the destination:** if this Relativity instance is not RelativityOne — including a Relativity Server instance, whose own documentation governs — this finding is informational and the rest of this template does not apply.

Replace the username and password properties with **the settings object's token-bearing equivalent**, assigned the access token, and remove both credential assignments. This template does not name that member, because which one exists depends on the SDK version. **Confirm the SDK version in use exposes one; where it does not, this is an SDK upgrade before it is a code change.**

Credentials: **Where the credentials come from.** The property names establish that the destination is Relativity, not that it is RelativityOne; if it is not RelativityOne — including a Relativity Server instance, whose own documentation governs — the migration is that service's own token mechanism, this finding is informational for this tool, and no migration to RelativityOne applies. If it is RelativityOne, the token comes from the token-acquisition part that follows.

**Where the token comes from.** Create an OAuth2 Client with Flow Grant Type set to Client Credentials (part 3 lists its settings), then send a form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=client_credentials`, `client_id`, `client_secret` and a `scope`. The token endpoint is **documented** for this grant. The token is then sent as `Authorization: Bearer <token>`, with `X-CSRF-Header: -` on REST and Kepler calls — the header is documented as required, and a Kepler call without it returns 404.

**The scope depends on what the integration needs, and no single scope is required.** Relativity's documentation shows two values for this grant: `SystemUserInfo` on the REST authentication page, the Usage Reports page and in the official Transfer SDK sample, and `UserInfoAccess` in the Kepler .NET samples. No page says which is correct, whether both work, or what either grants, and the OAuth2 Client's `Scopes` property is documented as one that cannot be set. Use the value the API you call documents, and confirm it.

**Cache the token, and re-acquire it on expiry — do not refresh.** Keep the token for its lifetime (part 3 recommends 60 minutes, Relativity's own wording) and request a new one with the client secret when it expires or a call is rejected for authentication. Client Credentials does not offer refresh tokens: **verified by NSerio against a live instance; not documented by Relativity** — the Allow Refresh Tokens control does not appear for this grant in the RelativityOne user interface. Relativity's own tables and text on refresh tokens conflict with each other, so re-acquisition with the secret is the advice that holds whichever way they are read.

**Long-running processes that use the Relativity SDK.** A `ServiceFactory` built once from a bearer token keeps that token after it expires. Rebuild the `ServiceFactory` and its proxies with a fresh token before expiry. This is the only advice consistent with the documentation: the package `Relativity.Kepler.Client.SDK` (documented) exposes a settable `Token` and an `HttpMessageHandler` overload but no token-provider overload, and no page documents renewing a token behind a factory.

**The client secret.** It is displayed once, at creation or reset. Store it as Secret Management (Part B of the Migration Documentation) describes; never in source, never in tracked configuration. A reset invalidates the previous secret immediately, so plan the cut-over before resetting it.

**Before deploying, confirm the calling identity.** Every call now runs as the OAuth2 Client's **Context User** — not as the user in the replaced credential, and for an import or a job as much as for a request. If that user has different workspace permissions or item-level security, the code returns fewer rows or `Unauthorized` in production with no change to its logic, and Relativity's audit attributes every call to that one account. Confirm the Context User's permissions match those of the previous identity. Relativity's REST API authentication page describes the Context User as usually a member of the Relativity Administrators group; the OAuth2 Client block (part 3) gives NSerio's recommendation, which differs.

**When the integration acts for a signed-in user.** Client Credentials is one service identity by construction. An integration that acts on behalf of a signed-in user is the Authorization Code case, and moving to it means a new OAuth2 Client, because the flow is fixed when a client is created. The finding's flow note says when to make that choice.

Verification: **Verification note — two things. The first decides whether the change can be made at all.**

**SDK version support comes first.** Where the version in use has no token-bearing settings member, the change cannot be made at this call site at all, and the upgrade carries its own verification.

**A token has a finite lifetime.** A settings object populated once and reused across long or repeated jobs holds a token that expires; populate it with a current token for each job.

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
<!-- generated:end CS-017 client-credentials -->

#### Authorization Code

<!-- generated:begin CS-017 authorization-code -->
**Confirming the flow.** An interactive-user signal in a file assigns Authorization Code, with this note: "An interactive-user signal in this file points to Authorization Code. Confirm the flow: use Client Credentials instead if the integration runs with credentials fixed for the integration and no signed-in user." Your user profile can also make Authorization Code the default; see Changing the default in [Confirming the flow](#confirming-the-flow). See [Confirming the flow](#confirming-the-flow) and the "Which flow" paragraph of CS-017 in [Part A](part-a-csharp.md#cs-017).

Source form:
```csharp
importJob.Settings.RelativityUsername = userName;
importJob.Settings.RelativityPassword = password;
```

Under token authentication: **First, the destination:** if this Relativity instance is not RelativityOne — including a Relativity Server instance, whose own documentation governs — this finding is informational and the rest of this template does not apply.

Replace the username and password properties with **the settings object's token-bearing equivalent**, assigned the access token, and remove both credential assignments. This template does not name that member, because which one exists depends on the SDK version. **Confirm the SDK version in use exposes one; where it does not, this is an SDK upgrade before it is a code change.**

Credentials: **Where the credentials come from.** The property names establish that the destination is Relativity, not that it is RelativityOne; if it is not RelativityOne — including a Relativity Server instance, whose own documentation governs — the migration is that service's own token mechanism, this finding is informational for this tool, and no migration to RelativityOne applies. If it is RelativityOne, the token comes from the token-acquisition part that follows.

**Common to both variants**

**Sign in.** Replace the code that collected the user's credentials with a redirect: send the user's browser to the authorize endpoint `<host>/Relativity/Identity/connect/authorize`, which leads to the RelativityOne login page. The authorize endpoint is **verified by NSerio against a live instance; not documented by Relativity**. The request carries the standard authorization-code parameters — `client_id`, `response_type=code`, the registered `redirect_uri`, a `scope`, and a `state` value the application checks on return; these are standard OAuth, and Relativity documents none of them. No discovery document is relied on.

**Receive the code.** The browser returns to the Redirect URL registered on the OAuth2 Client, with the authorization code and the `state` value. Check the `state` before using the code. The Redirect URL must be exactly the one registered (the variants say how).

**Exchange the code.** Send a form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=authorization_code`, the `code`, the same `redirect_uri`, the `client_id` and the `client_secret`, plus the `code_verifier` when PKCE is used. The code is exchanged at that endpoint, which Relativity documents for the Client Credentials grant only; the exchange is **verified by NSerio against a live instance; not documented by Relativity**.

**Use the token.** Send `Authorization: Bearer <access token>` — the change at the use site in part 1 — with `X-CSRF-Header: -` on REST and Kepler calls.

**On expiry.** With Allow Refresh Tokens enabled on the OAuth2 Client and PKCE used, the application holds a refresh token and, when the access token expires, sends a `grant_type=refresh_token` request with it and the client credentials in place of the code exchange — *inferred* from the standard OAuth shape, not tested. Without a refresh token the user signs in again. Relativity does not document a refresh token's lifetime, whether it rotates, how it is revoked, or whether a revocation endpoint exists, nor what disabling or deleting the client does to tokens already issued. A refresh token is a secret: store it as Secret Management (Part B of the Migration Documentation) describes, and on compromise disable the client and replace it. Enable refresh tokens only if the application must keep a session beyond one token lifetime without asking the user to sign in again.

**PKCE.** PKCE is required by Relativity's own documentation to obtain a refresh token, and not otherwise: a Code request that does not ask for a refresh token works without it, **verified by NSerio against a live instance; not documented by Relativity**. It is never a blanket requirement. NSerio recommends PKCE with the `S256` method in both variants (NSerio recommendation), and does not claim that Relativity accepts any particular method, which it does not document.

**Scope.** No scope set is prescribed for Code. The scope below works — `openid RelativityRequestOrigin UserInfo UserInfoAccess AuthenticationID RelativityWeb` — **verified by NSerio against a live instance; not documented by Relativity**, verified as working and not as minimal. `state` is sent as standard OAuth practice and `nonce` is not used; Relativity does not document whether it enforces either.

**The application stops collecting the password.** After this change the application never receives, stores or forwards the user's username and password: the user signs in on RelativityOne's own page, and the application holds only tokens. Remove the credential prompt, the form field and any storage of the password that the replaced code needed.

**Single sign-on.** Sign-in happens on RelativityOne's own login page, so the instance's single sign-on and two-factor rules apply unchanged — *inferred* from where the sign-in happens, and not documented for clients that customers create.

**The client secret is required.** Code does not work without a client secret: tested and rejected, **verified by NSerio against a live instance; not documented by Relativity**, which confirms Relativity's own statement that the grant requires one. It is displayed once and is stored as Secret Management (Part B of the Migration Documentation) describes.

**Server-side variant.** For a web application whose code runs on a server:

- **Redirect.** The application receives the code at one exact HTTPS URL per environment, a route of the application, with no wildcards (NSerio recommendation; Relativity's matching rules are not documented). The Relativity administrator registers the exact URL on the OAuth2 Client.
- **Secret and tokens.** The client secret stays on the server and never reaches the browser. The user's tokens are held server-side, keyed to the session, and never in the browser.
- **Exchange.** The server performs the code exchange itself.

**Desktop variant.** For a desktop or command-line application run by a user on their own machine:

- **The system browser.** Open the authorize URL in the user's default browser and not in a web view the application hosts, so the application never sees the password (NSerio recommendation).
- **A loopback redirect on a fixed port.** Listen on the loopback address only, on the registered port, for one request. The Redirect URL is `http://127.0.0.1:<port>/<path>`: a plain-HTTP loopback URL is accepted, **verified by NSerio against a live instance; not documented by Relativity**. The Relativity administrator registers that exact URL; because it is registered in advance, the port is fixed and not chosen at runtime.
- **The secret is extractable.** Code requires a client secret (above), so a distributed desktop application has to embed one, and **a secret shipped inside a distributed binary can be extracted by anyone who holds the binary**. It protects nothing against the person running the application.
- **Compensating guidance** (NSerio recommendation, subject to your own security policy): set Trusted IPs where the users' outbound addresses are known and stable — they restrict where a token can be *requested*, and are checked when a token is issued against the client's ID and secret; use the recommended short Access Token Lifetime of 60 minutes; enable refresh tokens only when needed; and create one OAuth2 Client per distribution, so a leaked secret can be disabled without affecting the others.
- **A known issue.** Relativity documents no recommendation for a distributed desktop application; this design is what NSerio verified works, and Relativity's own recommendation is tracked as a known issue of the release.

Verification: **Verification note — two things. The first decides whether the change can be made at all.**

**SDK version support comes first.** Where the version in use has no token-bearing settings member, the change cannot be made at this call site at all, and the upgrade carries its own verification.

**A token has a finite lifetime.** A settings object populated once and reused across long or repeated jobs holds a token that expires; populate it with a current token for each job.

OAuth2 Client: Create or confirm this OAuth2 Client in RelativityOne (NSerio's observation: the menu path is Home > Authentication > OAuth2 Client Manager, as Relativity's REST API authentication page names it). This tool never connects to RelativityOne and never creates or changes the OAuth2 Client; the settings below are guidance.
- Name: a descriptive name identifying the application it serves.
- Flow Grant Type: Code
- Redirect URLs: Relativity documentation: each URL includes the http or https protocol prefix. The exact callback URL is registered by the Relativity administrator. Server-side web applications, NSerio's recommendation: one exact HTTPS URL per environment, with no wildcards. Desktop and CLI applications: a plain-HTTP loopback URL, `http://127.0.0.1:<port>/<path>`, on a fixed port because the URL is registered in advance and the port cannot be chosen at runtime (verified by NSerio against a live instance; not documented by Relativity).
- Access Token Lifetime: 60 minutes, stated in minutes because the field is in minutes. Relativity documentation: tokens for this flow must have a short lifetime, and the recommended value matches the Identity Server default.
- Allow refresh tokens: NSerio's recommendation: leave it off unless the application must keep a user session active beyond the access token lifetime without asking the user to sign in again. If it is on, the refresh tokens are stored as secrets, per Secret Management (Part B of the Migration Documentation).
- Enabled: on.
<!-- generated:end CS-017 authorization-code -->

### CS-018 · TokenCarriedUnderBasicScheme

<a id="cs-018-migration"></a>

#### Client Credentials

<!-- generated:begin CS-018 client-credentials -->
**Confirming the flow.** When no signal in a file decides the flow, the tool assigns Client Credentials, the default, with this note: "Who supplies the credential at run time? If a person types it or signs in, the integration acts for that person: use Authorization Code. If it comes from configuration, settings, an environment variable or a secret store: use Client Credentials. No signal in this file showed which applies, so Client Credentials, the default, is recommended. Use Authorization Code instead if the integration acts on behalf of a signed-in user, or if your RelativityOne instance already has a Code-flow OAuth2 Client for this integration." A fixed-credential signal assigns it with this note: "A fixed-credential signal in this file points to Client Credentials, but how often that kind of signal is right has not been measured. Use Authorization Code instead if the integration acts on behalf of a signed-in user, or if your RelativityOne instance already has a Code-flow OAuth2 Client for this integration." See [Confirming the flow](#confirming-the-flow) and the "Which flow" paragraph of CS-018 in [Part A](part-a-csharp.md#cs-018).

Source form:
```csharp
string pair = "" + ":" + accessToken;
string encoded = Convert.ToBase64String(Encoding.ASCII.GetBytes(pair));
```

Under token authentication: **This change applies to any destination that accepts `Bearer`**, RelativityOne or not: no credential is exchanged, so only the token's provenance differs when the destination is not RelativityOne.

**Delete the encoding and change the prefix.** The token already in hand is carried verbatim:

```
Authorization: Basic <base64 of ":" + token>      ->      Authorization: Bearer <token>
```

```csharp
request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
```

No credential is exchanged and no client is registered — this is the cheapest remediation of any template.

Credentials: **Where the credentials come from.** The prefix change applies to any destination that accepts `Bearer`. If the destination is RelativityOne, the token that belongs in the header is obtained as the token-acquisition part that follows describes. If it is not RelativityOne, the token's provenance is that service's and does not change: the token already in hand is the one that moves to the `Bearer` prefix, issued however that service issues it.

**Where the token comes from.** Create an OAuth2 Client with Flow Grant Type set to Client Credentials (part 3 lists its settings), then send a form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=client_credentials`, `client_id`, `client_secret` and a `scope`. The token endpoint is **documented** for this grant. The token is then sent as `Authorization: Bearer <token>`, with `X-CSRF-Header: -` on REST and Kepler calls — the header is documented as required, and a Kepler call without it returns 404.

**The scope depends on what the integration needs, and no single scope is required.** Relativity's documentation shows two values for this grant: `SystemUserInfo` on the REST authentication page, the Usage Reports page and in the official Transfer SDK sample, and `UserInfoAccess` in the Kepler .NET samples. No page says which is correct, whether both work, or what either grants, and the OAuth2 Client's `Scopes` property is documented as one that cannot be set. Use the value the API you call documents, and confirm it.

**Cache the token, and re-acquire it on expiry — do not refresh.** Keep the token for its lifetime (part 3 recommends 60 minutes, Relativity's own wording) and request a new one with the client secret when it expires or a call is rejected for authentication. Client Credentials does not offer refresh tokens: **verified by NSerio against a live instance; not documented by Relativity** — the Allow Refresh Tokens control does not appear for this grant in the RelativityOne user interface. Relativity's own tables and text on refresh tokens conflict with each other, so re-acquisition with the secret is the advice that holds whichever way they are read.

**Long-running processes that use the Relativity SDK.** A `ServiceFactory` built once from a bearer token keeps that token after it expires. Rebuild the `ServiceFactory` and its proxies with a fresh token before expiry. This is the only advice consistent with the documentation: the package `Relativity.Kepler.Client.SDK` (documented) exposes a settable `Token` and an `HttpMessageHandler` overload but no token-provider overload, and no page documents renewing a token behind a factory.

**The client secret.** It is displayed once, at creation or reset. Store it as Secret Management (Part B of the Migration Documentation) describes; never in source, never in tracked configuration. A reset invalidates the previous secret immediately, so plan the cut-over before resetting it.

**Before deploying, confirm the calling identity.** Every call now runs as the OAuth2 Client's **Context User** — not as the user in the replaced credential, and for an import or a job as much as for a request. If that user has different workspace permissions or item-level security, the code returns fewer rows or `Unauthorized` in production with no change to its logic, and Relativity's audit attributes every call to that one account. Confirm the Context User's permissions match those of the previous identity. Relativity's REST API authentication page describes the Context User as usually a member of the Relativity Administrators group; the OAuth2 Client block (part 3) gives NSerio's recommendation, which differs.

**When the integration acts for a signed-in user.** Client Credentials is one service identity by construction. An integration that acts on behalf of a signed-in user is the Authorization Code case, and moving to it means a new OAuth2 Client, because the flow is fixed when a client is created. The finding's flow note says when to make that choice.

Verification: **Verification note — two things.**

**Verify the destination accepts `Bearer` before changing the prefix.** Some services accept only Basic, and there the prefix change breaks working code.

**A header set once on a long-lived client cannot be refreshed.** If the token has a lifetime, set the header per request with a current token rather than on a shared client's default headers.

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
<!-- generated:end CS-018 client-credentials -->

#### Authorization Code

<!-- generated:begin CS-018 authorization-code -->
**Confirming the flow.** An interactive-user signal in a file assigns Authorization Code, with this note: "An interactive-user signal in this file points to Authorization Code. Confirm the flow: use Client Credentials instead if the integration runs with credentials fixed for the integration and no signed-in user." Your user profile can also make Authorization Code the default; see Changing the default in [Confirming the flow](#confirming-the-flow). See [Confirming the flow](#confirming-the-flow) and the "Which flow" paragraph of CS-018 in [Part A](part-a-csharp.md#cs-018).

Source form:
```csharp
string pair = "" + ":" + accessToken;
string encoded = Convert.ToBase64String(Encoding.ASCII.GetBytes(pair));
```

Under token authentication: **This change applies to any destination that accepts `Bearer`**, RelativityOne or not: no credential is exchanged, so only the token's provenance differs when the destination is not RelativityOne.

**Delete the encoding and change the prefix.** The token already in hand is carried verbatim:

```
Authorization: Basic <base64 of ":" + token>      ->      Authorization: Bearer <token>
```

```csharp
request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
```

No credential is exchanged and no client is registered — this is the cheapest remediation of any template.

Credentials: **Where the credentials come from.** The prefix change applies to any destination that accepts `Bearer`. If the destination is RelativityOne, the token that belongs in the header is obtained as the token-acquisition part that follows describes. If it is not RelativityOne, the token's provenance is that service's and does not change: the token already in hand is the one that moves to the `Bearer` prefix, issued however that service issues it.

**Common to both variants**

**Sign in.** Replace the code that collected the user's credentials with a redirect: send the user's browser to the authorize endpoint `<host>/Relativity/Identity/connect/authorize`, which leads to the RelativityOne login page. The authorize endpoint is **verified by NSerio against a live instance; not documented by Relativity**. The request carries the standard authorization-code parameters — `client_id`, `response_type=code`, the registered `redirect_uri`, a `scope`, and a `state` value the application checks on return; these are standard OAuth, and Relativity documents none of them. No discovery document is relied on.

**Receive the code.** The browser returns to the Redirect URL registered on the OAuth2 Client, with the authorization code and the `state` value. Check the `state` before using the code. The Redirect URL must be exactly the one registered (the variants say how).

**Exchange the code.** Send a form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=authorization_code`, the `code`, the same `redirect_uri`, the `client_id` and the `client_secret`, plus the `code_verifier` when PKCE is used. The code is exchanged at that endpoint, which Relativity documents for the Client Credentials grant only; the exchange is **verified by NSerio against a live instance; not documented by Relativity**.

**Use the token.** Send `Authorization: Bearer <access token>` — the change at the use site in part 1 — with `X-CSRF-Header: -` on REST and Kepler calls.

**On expiry.** With Allow Refresh Tokens enabled on the OAuth2 Client and PKCE used, the application holds a refresh token and, when the access token expires, sends a `grant_type=refresh_token` request with it and the client credentials in place of the code exchange — *inferred* from the standard OAuth shape, not tested. Without a refresh token the user signs in again. Relativity does not document a refresh token's lifetime, whether it rotates, how it is revoked, or whether a revocation endpoint exists, nor what disabling or deleting the client does to tokens already issued. A refresh token is a secret: store it as Secret Management (Part B of the Migration Documentation) describes, and on compromise disable the client and replace it. Enable refresh tokens only if the application must keep a session beyond one token lifetime without asking the user to sign in again.

**PKCE.** PKCE is required by Relativity's own documentation to obtain a refresh token, and not otherwise: a Code request that does not ask for a refresh token works without it, **verified by NSerio against a live instance; not documented by Relativity**. It is never a blanket requirement. NSerio recommends PKCE with the `S256` method in both variants (NSerio recommendation), and does not claim that Relativity accepts any particular method, which it does not document.

**Scope.** No scope set is prescribed for Code. The scope below works — `openid RelativityRequestOrigin UserInfo UserInfoAccess AuthenticationID RelativityWeb` — **verified by NSerio against a live instance; not documented by Relativity**, verified as working and not as minimal. `state` is sent as standard OAuth practice and `nonce` is not used; Relativity does not document whether it enforces either.

**The application stops collecting the password.** After this change the application never receives, stores or forwards the user's username and password: the user signs in on RelativityOne's own page, and the application holds only tokens. Remove the credential prompt, the form field and any storage of the password that the replaced code needed.

**Single sign-on.** Sign-in happens on RelativityOne's own login page, so the instance's single sign-on and two-factor rules apply unchanged — *inferred* from where the sign-in happens, and not documented for clients that customers create.

**The client secret is required.** Code does not work without a client secret: tested and rejected, **verified by NSerio against a live instance; not documented by Relativity**, which confirms Relativity's own statement that the grant requires one. It is displayed once and is stored as Secret Management (Part B of the Migration Documentation) describes.

**Server-side variant.** For a web application whose code runs on a server:

- **Redirect.** The application receives the code at one exact HTTPS URL per environment, a route of the application, with no wildcards (NSerio recommendation; Relativity's matching rules are not documented). The Relativity administrator registers the exact URL on the OAuth2 Client.
- **Secret and tokens.** The client secret stays on the server and never reaches the browser. The user's tokens are held server-side, keyed to the session, and never in the browser.
- **Exchange.** The server performs the code exchange itself.

**Desktop variant.** For a desktop or command-line application run by a user on their own machine:

- **The system browser.** Open the authorize URL in the user's default browser and not in a web view the application hosts, so the application never sees the password (NSerio recommendation).
- **A loopback redirect on a fixed port.** Listen on the loopback address only, on the registered port, for one request. The Redirect URL is `http://127.0.0.1:<port>/<path>`: a plain-HTTP loopback URL is accepted, **verified by NSerio against a live instance; not documented by Relativity**. The Relativity administrator registers that exact URL; because it is registered in advance, the port is fixed and not chosen at runtime.
- **The secret is extractable.** Code requires a client secret (above), so a distributed desktop application has to embed one, and **a secret shipped inside a distributed binary can be extracted by anyone who holds the binary**. It protects nothing against the person running the application.
- **Compensating guidance** (NSerio recommendation, subject to your own security policy): set Trusted IPs where the users' outbound addresses are known and stable — they restrict where a token can be *requested*, and are checked when a token is issued against the client's ID and secret; use the recommended short Access Token Lifetime of 60 minutes; enable refresh tokens only when needed; and create one OAuth2 Client per distribution, so a leaked secret can be disabled without affecting the others.
- **A known issue.** Relativity documents no recommendation for a distributed desktop application; this design is what NSerio verified works, and Relativity's own recommendation is tracked as a known issue of the release.

Verification: **Verification note — two things.**

**Verify the destination accepts `Bearer` before changing the prefix.** Some services accept only Basic, and there the prefix change breaks working code.

**A header set once on a long-lived client cannot be refreshed.** If the token has a lifetime, set the header per request with a current token rather than on a shared client's default headers.

OAuth2 Client: Create or confirm this OAuth2 Client in RelativityOne (NSerio's observation: the menu path is Home > Authentication > OAuth2 Client Manager, as Relativity's REST API authentication page names it). This tool never connects to RelativityOne and never creates or changes the OAuth2 Client; the settings below are guidance.
- Name: a descriptive name identifying the application it serves.
- Flow Grant Type: Code
- Redirect URLs: Relativity documentation: each URL includes the http or https protocol prefix. The exact callback URL is registered by the Relativity administrator. Server-side web applications, NSerio's recommendation: one exact HTTPS URL per environment, with no wildcards. Desktop and CLI applications: a plain-HTTP loopback URL, `http://127.0.0.1:<port>/<path>`, on a fixed port because the URL is registered in advance and the port cannot be chosen at runtime (verified by NSerio against a live instance; not documented by Relativity).
- Access Token Lifetime: 60 minutes, stated in minutes because the field is in minutes. Relativity documentation: tokens for this flow must have a short lifetime, and the recommended value matches the Identity Server default.
- Allow refresh tokens: NSerio's recommendation: leave it off unless the application must keep a user session active beyond the access token lifetime without asking the user to sign in again. If it is on, the refresh tokens are stored as secrets, per Secret Management (Part B of the Migration Documentation).
- Enabled: on.
<!-- generated:end CS-018 authorization-code -->


## Generic suggestion

The tool gives a generic recommendation when a match is reported at High or Medium confidence but no pattern-specific entry applies. It is shorter than a class entry: it does not show a before-and-after form, because the construct was not characterized. Use it as a starting point, and read the Part A description of the pattern class if you can identify the construct.

### Client Credentials

<!-- generated:begin generic client-credentials -->
**Confirming the flow.** When no signal in a file decides the flow, the tool assigns Client Credentials, the default, with this note: "Who supplies the credential at run time? If a person types it or signs in, the integration acts for that person: use Authorization Code. If it comes from configuration, settings, an environment variable or a secret store: use Client Credentials. No signal in this file showed which applies, so Client Credentials, the default, is recommended. Use Authorization Code instead if the integration acts on behalf of a signed-in user, or if your RelativityOne instance already has a Code-flow OAuth2 Client for this integration." A fixed-credential signal assigns it with this note: "A fixed-credential signal in this file points to Client Credentials, but how often that kind of signal is right has not been measured. Use Authorization Code instead if the integration acts on behalf of a signed-in user, or if your RelativityOne instance already has a Code-flow OAuth2 Client for this integration." See [Confirming the flow](#confirming-the-flow).

The tool reports Basic Authentication at this location but did not recognize the construct, so the change has not been characterized and no before-and-after form is offered here. If the destination is not RelativityOne, Relativity's identity endpoint does not issue tokens for it: the migration is that service's own, and this finding is informational. If the destination is RelativityOne, Basic Authentication is no longer documented for its REST API. The documented destination in C# is either the Relativity SDK's bearer-token credential type, where the code uses the SDK, or a bearer `Authorization` header, where it builds requests itself; either carries a token obtained from the platform's identity endpoint with the client-credentials grant. The client ID and secret that obtain that token belong in the process environment or a secret store, per Secret Management (Part B of the Migration Documentation), and never in source or tracked configuration. Beyond those facts this is a pointer and not an instruction: the migration must be designed from the code by whoever owns it, rather than applied from this text.

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

The tool reports Basic Authentication at this location but did not recognize the construct, so the change has not been characterized and no before-and-after form is offered here. If the destination is not RelativityOne, Relativity's identity endpoint does not issue tokens for it: the migration is that service's own, and this finding is informational. If the destination is RelativityOne, Basic Authentication is no longer documented for its REST API, and this finding is assigned the Authorization Code flow, in which the application sends the user to RelativityOne to sign in, receives an authorization code at a Redirect URL registered on its OAuth2 Client, and exchanges the code for an access token that it sends as a Bearer token. The authorize endpoint is `<host>/Relativity/Identity/connect/authorize` and the code is exchanged at `<host>/Relativity/Identity/connect/token`; both are verified by NSerio against a live instance; not documented by Relativity. PKCE is required by Relativity's own documentation to obtain a refresh token, and not otherwise. The client ID and secret belong in the process environment or a secret store, per Secret Management (Part B of the Migration Documentation), and never in source or tracked configuration; Code requires a client secret. After the change the application no longer collects or forwards the user's password. Beyond those facts this is a pointer and not an instruction: the migration must be designed from the code by whoever owns it, rather than applied from this text.

OAuth2 Client: Create or confirm this OAuth2 Client in RelativityOne (NSerio's observation: the menu path is Home > Authentication > OAuth2 Client Manager, as Relativity's REST API authentication page names it). This tool never connects to RelativityOne and never creates or changes the OAuth2 Client; the settings below are guidance.
- Name: a descriptive name identifying the application it serves.
- Flow Grant Type: Code
- Redirect URLs: Relativity documentation: each URL includes the http or https protocol prefix. The exact callback URL is registered by the Relativity administrator. Server-side web applications, NSerio's recommendation: one exact HTTPS URL per environment, with no wildcards. Desktop and CLI applications: a plain-HTTP loopback URL, `http://127.0.0.1:<port>/<path>`, on a fixed port because the URL is registered in advance and the port cannot be chosen at runtime (verified by NSerio against a live instance; not documented by Relativity).
- Access Token Lifetime: 60 minutes, stated in minutes because the field is in minutes. Relativity documentation: tokens for this flow must have a short lifetime, and the recommended value matches the Identity Server default.
- Allow refresh tokens: NSerio's recommendation: leave it off unless the application must keep a user session active beyond the access token lifetime without asking the user to sign in again. If it is on, the refresh tokens are stored as secrets, per Secret Management (Part B of the Migration Documentation).
- Enabled: on.
<!-- generated:end generic authorization-code -->
