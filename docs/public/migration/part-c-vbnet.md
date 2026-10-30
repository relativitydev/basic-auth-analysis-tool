# Migration Suggestions for VB.NET (Part C of the Migration Documentation)

Part C tells you what to do when a Part A pattern is found in your VB.NET code. It gives the same recommendation the tool gives for that finding, because the recommendations below are produced by the same recommendation catalog the tool reports from, and are not written separately.

This document has three kinds of text:

- **Hand-written guidance.** How to confirm which OAuth 2.0 flow applies, and how to judge a match the tool marked Low confidence.
- **Recommendations.** One entry for each pattern class and each flow, and one generic recommendation for each flow. Each sits between a pair of `generated` markers, which are invisible when the document is rendered.
- **Pointers.** Part A ([`part-a-vbnet.md`](part-a-vbnet.md)) describes each pattern and how to search for it. Secret Management (Part B of the Migration Documentation, [`part-b-secret-management.md`](part-b-secret-management.md)) says where the new secrets go.

Every example uses placeholders such as `<host>`, `<client id>` and `<client secret>`. Replace them with your own values, and keep real secrets out of source code.

The two flows are the only ones the tool recommends:

- **Client Credentials** is the default. The application sends its client ID and client secret to the token endpoint and receives an access token. No user is involved, and every call runs as the OAuth2 Client's Context User.
- **Authorization Code** is called "Code" in RelativityOne's OAuth2 Client form. The application redirects a signed-in user to RelativityOne, receives an authorization code at a registered Redirect URL, and exchanges it for a token that carries that user's permissions.

VB.NET runs on .NET Framework and on .NET, and where they differ an entry says so. The code in the entries is VB.NET and was compiled before it was written here. The token-acquisition code was run against a local mock server only, not against a live RelativityOne instance.

## Confirming the flow

Ask one question of the file that holds the match: **who supplies the credential at run time?**

- If a person types it or signs in, the integration acts for that person. Use **Authorization Code**.
- If it comes from configuration, an environment variable, a secret store or a setting, and no person is present, use **Client Credentials**.

The tool answers that question by looking for signals in the **same file** as the match. Part A lists every signal by name under [Flow signals](part-a-vbnet.md#flow-signals). The rules it applies are these:

1. **An interactive-user signal decides Authorization Code.** Interactive-user signals are a console prompt that asks for a password, a masked key read, an `InputBox` that asks for a credential, a desktop password control, a password sign-in call, a login-page handler or a posted password field. If an interactive-user signal and a fixed-credential signal are both present, the interactive one wins.
2. **A fixed-credential signal decides Client Credentials.** Fixed-credential signals are a read of a credential-named configuration key, application setting, instance setting or environment variable, and a read from a secret store. The recommendation carries a note asking you to check, because nothing yet measures how often such a signal points the wrong way.
3. **No signal means the default.** With no signal in the file, the tool recommends Client Credentials and carries the same note. This is the expected result for most real code, for the reason below.

**The tool reads one file at a time.** A prompt in another file, a partial class or module defined elsewhere, a credential read in a project that this one references, project-level imports and a credential that a caller passes in as a parameter are not seen. Such a match is decided by the default even when the code acts for a signed-in user. When you review a finding, look beyond its file: follow the credential back to where it comes from, and if a person supplies it, use Authorization Code even though the tool recommended Client Credentials.

**Web Forms pages are read file by file as well.** A page whose login control is declared in the `.aspx` markup and whose code is in the `.aspx.vb` file is decided by the code-behind alone. If the code-behind holds no login handler of its own, the tool assigns the default flow, Client Credentials, although the page acts for a signed-in user. For code that serves a signed-in user, use Authorization Code. The markup itself is not scanned.

**A file that holds both kinds of evidence is read as interactive.** A file that uses a stored credential when there is one and falls back to a prompt when there is not holds both kinds of signal, so the interactive one wins and the tool recommends Authorization Code. The same happens to a file that merely contains two unrelated pieces of code, one that serves a signed-in user and one that runs unattended. If the match is in practice an unattended integration, the credential is the stored one and Client Credentials is the right flow. Decide by how the code runs, not by the fallback.

Two more rules the tool applies, so that you can reproduce its choice by hand:

- A credential written literally into the code is **not** evidence of either flow. It is a secret to remove, and Part B explains how. A file whose only credential is a literal is decided by the default.
- The tool works from the scanned code and local configuration only. It never contacts RelativityOne, so it cannot tell you what OAuth2 Clients your instance already has.

**Confirm against your instance.** If your RelativityOne instance already has an OAuth2 Client with the Code flow for this integration, use Authorization Code, whatever the tool recommended. If it has one with Client Credentials for this integration, use Client Credentials. The flow is fixed when an OAuth2 Client is created, so moving an integration from one flow to the other means creating a new OAuth2 Client.

**Changing the default.** If most of your integrations are user-facing, you can make Authorization Code the default. Set `defaultFlow` to `AuthorizationCode` in the `.migrate-auth.json` file in your user profile folder; the accepted values are `ClientCredentials` and `AuthorizationCode`. The tool reads this setting from the user profile file only. A `defaultFlow` in the scanned repository's own file is ignored and recorded as ignored, so a repository cannot change the recommendation for the findings it holds. The setting changes only the default; a signal in the file still decides the flow.

## Judging a Low-confidence match

A match is **Low** confidence when it is present in the text but unlikely to be live authenticating code. The tool gives a Low match no flow and no recommendation, and lists it for manual review. **A Low level is not evidence that the code does not authenticate.** It means a person has to look. The paragraphs below say how to judge each cause and when the match still needs a migration. When it does, find the live construct it belongs to, identify its class in Part A, and follow that class's entry in [Migration suggestions by pattern class](#migration-suggestions-by-pattern-class).

### Comments

The match is inside a comment, written with an apostrophe or with `REM`, so no code runs. Read the comment. If it is commented-out code, nothing needs migrating today, but delete it or migrate it before anyone restores it, because a restored line brings Basic Authentication back. If it describes code that exists elsewhere, search for that code with Part A and migrate the live code. If it only illustrates a pattern with placeholders, no migration is needed. A block that is switched off with a `#If` condition is not a comment: the tool reads it as live code, so a match in it is reported at its ordinary level, and you decide whether the branch ever runs.

### Documentation strings

The match is inside text that documents or explains, such as an XML documentation comment (`'''`), a message shown to the user, an error message, a string that holds a sample, or a usage example. The text does not authenticate anything. Check that the behaviour it describes has been migrated, and update the wording afterwards so that the documentation stops describing Basic Authentication. The text itself needs no migration.

### Test fixtures and mocks

The match sits in a file the tool recognizes as test code: a folder named `test`, `tests`, `spec` or `fixtures`, a folder whose name ends in `.Test` or `.Tests`, a file name that ends in `.Tests.vb`, `.Test.vb`, `.Spec.vb`, `Tests.vb`, `Fake.vb`, `Mock.vb` or `Stub.vb` or begins with `Fake`, `Mock` or `Stub`, or a file that carries a test attribute such as `<TestMethod>`, `<TestClass>`, `<Fact>`, `<Theory>`, `<Test>` or `<TestFixture>` in code. Judge by what the test talks to. If it feeds a mock and never reaches RelativityOne, no migration is needed, though you may want the mock to model the token flow so the test still represents the code it covers. If it is an integration test that authenticates to a real RelativityOne instance, it needs migrating with everything else, because it stops working when Basic Authentication is switched off. A credential written literally into a test is still a secret to remove.

### Sample and example code

The match sits in a folder named `sample`, `samples`, `example` or `examples`, in any letter case, or in a file whose name begins with `Sample` or `Example`. Those are the only names the tool recognizes: a sample kept in a folder called `demo`, or in a file whose name merely contains the word, is not demoted and is reported at its ordinary level, so review it as live code. Judge by who uses it. If nobody runs the sample, it needs no migration. If people copy it as a starting point, migrate it: an example that teaches Basic Authentication spreads it to new code. Use the recommendation for the class the sample matches, and prefer the flow that fits the scenario the sample shows.

### Vendored and generated files

The match sits in a file produced by a tool (a file name that ends in `.Designer.vb`, `.g.vb` or `.generated.vb`, a header that says `auto-generated`, a `GeneratedCode` attribute, or a folder named `Service References` or `Web References`) or in a folder named `vendor` or `third-party`, which holds third-party code. **Do not edit a generated file by hand:** the next generation overwrites the edit. Fix the source it is generated from (the service description, the designer or the generator settings) and regenerate. For vendored code, upgrade the library to a version that supports token authentication, or replace it. A migration is needed when the file runs against RelativityOne in your environment. It is not needed when the file is never called, or when it is only reference material. A folder named `Deployment` is not a signal for VB.NET: a file under it is reported at its ordinary level.

### VB-018 · NonHttpCredentialSink

<a id="vb-018-judging"></a>

A credential that is handed to something that is not an HTTP client: a mail client, a web proxy, an FTP request, a directory object, or a process started with a user name and a password. The tool marks it Low because it does not believe it is Basic Authentication towards RelativityOne, and it exists so that mail, file-transfer, directory and proxy credentials stop being reported as migration targets. Confirm the sink. If the credential really goes to a mail server, a file server, a directory, a proxy or another program, no migration to RelativityOne is needed, and the credential's secret handling is still governed by your own policy. If the same credential is also used to call RelativityOne, that use is reported separately under the class that matches it, and needs migrating. A non-HTTP consumer that the tool does not list is reported as a Medium finding for a constructed credential: dismiss it on review if the consumer is not RelativityOne.

### VB-019 · AuthenticationHelperDefinition

<a id="vb-019-judging"></a>

A `Function` or `Sub` whose name carries the scheme word and an authentication word, such as a method that builds the Basic header or the credential for the application, is a helper. The tool marks its definition Low because the advice for a helper is not a mechanical code change. **The helper is the place to migrate, and it hides its callers:** the tool reports the helper's definition once, and its call sites do not appear as findings because they contain no visible credential. Search for the helper's name to find every caller. If the helper builds Basic Authentication for calls to RelativityOne, migrate it once, so that it obtains and caches an access token and sends a Bearer header. Decide the flow from who supplies the credential to its callers: a person means Authorization Code, and a fixed credential means Client Credentials. Then follow the entry for the construct the helper contains, if it has one. If the helper builds a credential for another service, it needs no migration.

## Migration suggestions by pattern class

Each pattern class has two entries, one for each flow. Choose the entry for the flow you confirmed above. Every entry contains the code being replaced, the migrated code, a description of token acquisition, caching and expiry handling, how to confirm the flow and the default, the OAuth2 Client to create or confirm with its settings, and the pointer to Secret Management (Part B of the Migration Documentation). Every Authorization Code entry adds a description of refresh-token handling, of PKCE, and of a server-side variant and a desktop variant.

**Part 1, part 2 and part 3.** The text of an entry refers to its own pieces by number. The same names appear in the report's recommendations. Each piece has a label in the entry:

- **Part 1** is the change at the point of use: the text under the label *Under token authentication*, with its migrated code.
- **Part 2** is the token acquisition: the part of the text under the label *Credentials* that follows the paragraph *Where the credentials come from*. It describes the request to the token endpoint, the caching of the token and what to do when it expires. A Client Credentials entry starts it with *Where the token comes from*, and an Authorization Code entry with *Common to both variants*.
- **Part 3** is the OAuth2 Client settings, under the label *OAuth2 Client* at the end of the entry.

The labels *Source form* (the construct being replaced) and *Verification* (what to check before you deploy) are not numbered.

The two Low classes, VB-018 and VB-019, have no entry because they produce no recommendation. They are covered under [Judging a Low-confidence match](#judging-a-low-confidence-match).

### VB-001 · SdkUserNamePasswordCredentials

<a id="vb-001-migration"></a>

#### Client Credentials

<!-- generated:begin VB-001 client-credentials -->
**Confirming the flow.** When no signal in a file decides the flow, the tool assigns Client Credentials, the default, with this note: "Who supplies the credential at run time? If a person types it or signs in, the integration acts for that person: use Authorization Code. If it comes from configuration, settings, an environment variable or a secret store: use Client Credentials. No signal in this file showed which applies, so Client Credentials, the default, is recommended. Use Authorization Code instead if the integration acts on behalf of a signed-in user, or if your RelativityOne instance already has a Code-flow OAuth2 Client for this integration." A fixed-credential signal assigns it with this note: "A fixed-credential signal in this file points to Client Credentials, but how often that kind of signal is right has not been measured. Use Authorization Code instead if the integration acts on behalf of a signed-in user, or if your RelativityOne instance already has a Code-flow OAuth2 Client for this integration." See [Confirming the flow](#confirming-the-flow) and the "Which flow" paragraph of VB-001 in [Part A](part-a-vbnet.md#vb-001).

Source form: **Applies to:** both (the SDK package is the same).

```vbnet
Dim credentials As Credentials = New UsernamePasswordCredentials(userName, password)
Dim settings As New ServiceFactorySettings(restEndpointUri, credentials)
```

Under token authentication: **First, the destination:** if this Relativity instance is not RelativityOne — including a Relativity Server instance, whose own documentation governs — this finding is informational and the rest of this template does not apply.

```vbnet
Dim settings As New ServiceFactorySettings(restEndpointUri, New BearerTokenCredentials(accessToken))
```

The REST URI is unchanged: `<scheme>://<machine name>/Relativity.Rest/api`.

Credentials: **Where the credentials come from.** The SDK type establishes that the destination is Relativity, not that it is RelativityOne; if it is not RelativityOne — including a Relativity Server instance, whose own documentation governs — the migration is that service's own token mechanism, this finding is informational for this tool, and the reader stops here. If it is RelativityOne, the token comes from the token-acquisition part that follows.

**Where the token comes from.** Create an OAuth2 Client with Flow Grant Type set to Client Credentials (part 3 lists its settings), then send a form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=client_credentials`, `client_id`, `client_secret` and a `scope`. The token endpoint is **documented** for this grant. The token is then sent as `Authorization: Bearer <token>`, with `X-CSRF-Header: -` on REST and Kepler calls.

**.NET (both runtimes' `HttpClient`; `System.Text.Json` on .NET)**

```vbnet
Imports System.Net.Http
Imports System.Text.Json

Public Async Function AcquireAccessTokenAsync(host As String, clientId As String, clientSecret As String, scope As String) As Task(Of String)
	Dim accessToken As String
	Using client As New HttpClient()
		Dim form As New FormUrlEncodedContent(New Dictionary(Of String, String) From {
			{"grant_type", "client_credentials"},
			{"client_id", clientId},
			{"client_secret", clientSecret},
			{"scope", scope}})
		Using response As HttpResponseMessage = Await client.PostAsync(host & "/Relativity/Identity/connect/token", form)
			response.EnsureSuccessStatusCode()
			Using document As JsonDocument = JsonDocument.Parse(Await response.Content.ReadAsStringAsync())
				accessToken = document.RootElement.GetProperty("access_token").GetString()
			End Using
		End Using
	End Using
	Return accessToken
End Function
```

**.NET Framework 4.7.2** — the same call with the serializer the runtime ships (`System.Web.Extensions`); `HttpClient` is in `System.Net.Http`:

```vbnet
Imports System.Net.Http
Imports System.Web.Script.Serialization

Public Async Function AcquireAccessTokenAsync(host As String, clientId As String, clientSecret As String, scope As String) As Task(Of String)
	Dim accessToken As String
	Using client As New HttpClient()
		Dim form As New FormUrlEncodedContent(New Dictionary(Of String, String) From {
			{"grant_type", "client_credentials"},
			{"client_id", clientId},
			{"client_secret", clientSecret},
			{"scope", scope}})
		Using response As HttpResponseMessage = Await client.PostAsync(host & "/Relativity/Identity/connect/token", form)
			response.EnsureSuccessStatusCode()
			Dim body As String = Await response.Content.ReadAsStringAsync()
			Dim fields As Dictionary(Of String, Object) = New JavaScriptSerializer().Deserialize(Of Dictionary(Of String, Object))(body)
			accessToken = fields("access_token").ToString()
		End Using
	End Using
	Return accessToken
End Function
```

**The scope depends on what the integration needs, and no single scope is required.** Relativity's documentation shows two values for this grant: `SystemUserInfo` on the REST authentication page, the Usage Reports page and in the official Transfer SDK sample, and `UserInfoAccess` in the Kepler .NET samples. No page says which is correct, whether both work, or what either grants, and the OAuth2 Client's `Scopes` property is documented as one that cannot be set. Use the value the API you call documents, and confirm it.

**Cache the token, and re-acquire it on expiry — do not refresh.** Keep the token for its lifetime (part 3 recommends 60 minutes, Relativity's own wording) and request a new one with the client secret when it expires or a call is rejected for authentication. Client Credentials does not offer refresh tokens: **verified by NSerio against a live instance; not documented by Relativity** — the Allow Refresh Tokens control does not appear for this grant in the RelativityOne user interface. Relativity's own tables and text on refresh tokens conflict with each other, so re-acquisition with the secret is the advice that holds whichever way they are read.

**Long-running processes that use the Relativity SDK.** A `ServiceFactory` built once from a bearer token keeps that token after it expires. Rebuild the `ServiceFactory` and its proxies with a fresh token before expiry. This is the only advice consistent with the documentation: the package `Relativity.Kepler.Client.SDK` (documented) exposes a settable `Token` and an `HttpMessageHandler` overload but no token-provider overload, and no page documents renewing a token behind a factory.

**The client secret.** It is displayed once, at creation or reset. Store it as Secret Management (Part B of the Migration Documentation) describes; never in source, never in tracked configuration, which in a VB.NET estate includes `app.config`, `web.config` and `My.Settings`. A reset invalidates the previous secret immediately, so plan the cut-over before resetting it.

**Before deploying, confirm the calling identity.** Every call now runs as the OAuth2 Client's **Context User** — not as the user in the replaced credential, and for an import or a job as much as for a request. If that user has different workspace permissions or item-level security, the code returns fewer rows or `Unauthorized` in production with no change to its logic, and Relativity's audit attributes every call to that one account. Confirm the Context User's permissions match those of the previous identity. Relativity describes the Context User as usually a member of the Relativity Administrators group; the OAuth2 Client block (part 3) gives NSerio's recommendation, which differs.

**When the integration acts for a signed-in user.** Client Credentials is one service identity by construction. An integration that acts on behalf of a signed-in user is the Authorization Code case, and moving to it means a new OAuth2 Client, because the flow is fixed when a client is created. The finding's flow note says when to make that choice.

Verification: **Verification note — two things, and the first can break working code.** **A token has a finite lifetime**, set per client in minutes, and this construct holds it. A `ServiceFactory` built once from `BearerTokenCredentials(accessToken)` and kept for the life of a process keeps that token after it expires, and the calls made through it then fail authentication. Rebuild the settings and the factory with a current token within the token's lifetime; the previous credential never expired, so nothing in the existing code does this. **Confirm the SDK version in use provides `BearerTokenCredentials`.** Where it does not, this is an SDK upgrade before it is a code change.

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
<!-- generated:end VB-001 client-credentials -->

#### Authorization Code

<!-- generated:begin VB-001 authorization-code -->
**Confirming the flow.** An interactive-user signal in a file assigns Authorization Code, with this note: "An interactive-user signal in this file points to Authorization Code. Confirm the flow: use Client Credentials instead if the integration runs with credentials fixed for the integration and no signed-in user." Your user profile can also make Authorization Code the default; see Changing the default in [Confirming the flow](#confirming-the-flow). See [Confirming the flow](#confirming-the-flow) and the "Which flow" paragraph of VB-001 in [Part A](part-a-vbnet.md#vb-001).

Source form: **Applies to:** both (the SDK package is the same).

```vbnet
Dim credentials As Credentials = New UsernamePasswordCredentials(userName, password)
Dim settings As New ServiceFactorySettings(restEndpointUri, credentials)
```

Under token authentication: **First, the destination:** if this Relativity instance is not RelativityOne — including a Relativity Server instance, whose own documentation governs — this finding is informational and the rest of this template does not apply.

```vbnet
Dim settings As New ServiceFactorySettings(restEndpointUri, New BearerTokenCredentials(accessToken))
```

The REST URI is unchanged: `<scheme>://<machine name>/Relativity.Rest/api`.

Credentials: **Where the credentials come from.** The SDK type establishes that the destination is Relativity, not that it is RelativityOne; if it is not RelativityOne — including a Relativity Server instance, whose own documentation governs — the migration is that service's own token mechanism, this finding is informational for this tool, and the reader stops here. If it is RelativityOne, the token comes from the token-acquisition part that follows.

The text a finding assigned Authorization Code ("Code") receives after part 1. It has a common part and two variants, **server-side** and **desktop**. The bold lead-in of each element is its marker. Every fact Relativity does not document but NSerio verified carries the exact label **verified by NSerio against a live instance; not documented by Relativity**.

**Common to both variants**

**Sign in.** Replace the code that collected the user's credentials with a redirect: send the user's browser to the authorize endpoint `<host>/Relativity/Identity/connect/authorize`, which leads to the RelativityOne login page. The authorize endpoint is **verified by NSerio against a live instance; not documented by Relativity**. The request carries the standard authorization-code parameters — `client_id`, `response_type=code`, the registered `redirect_uri`, a `scope`, and a `state` value the application checks on return; these are standard OAuth, and Relativity documents none of them. No discovery document is relied on.

**Receive the code.** The browser returns to the Redirect URL registered on the OAuth2 Client, with the authorization code and the `state` value. Check the `state` before using the code. The Redirect URL must be exactly the one registered (the variants say how).

**Exchange the code.** Send a form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=authorization_code`, the `code`, the same `redirect_uri`, the `client_id` and the `client_secret`, plus the `code_verifier` when PKCE is used. The code is exchanged at that endpoint, which Relativity documents for the Client Credentials grant only; the exchange is **verified by NSerio against a live instance; not documented by Relativity**.

```vbnet
Dim form As New FormUrlEncodedContent(New Dictionary(Of String, String) From {
	{"grant_type", "authorization_code"},
	{"code", authorizationCode},
	{"redirect_uri", redirectUri},
	{"client_id", clientId},
	{"client_secret", clientSecret},
	{"code_verifier", codeVerifier}})
Using response As HttpResponseMessage = Await client.PostAsync(host & "/Relativity/Identity/connect/token", form)
	response.EnsureSuccessStatusCode()
End Using
```

**Use the token.** Send `Authorization: Bearer <access token>` — the change at the use site in part 1 — with `X-CSRF-Header: -` on REST and Kepler calls.

**On expiry.** With Allow Refresh Tokens enabled on the OAuth2 Client and PKCE used, the application holds a refresh token and, when the access token expires, sends a `grant_type=refresh_token` request with it and the client credentials in place of the code exchange — *inferred* from the standard OAuth shape, not tested. Without a refresh token the user signs in again. Relativity does not document a refresh token's lifetime, whether it rotates, how it is revoked, or whether a revocation endpoint exists, nor what disabling or deleting the client does to tokens already issued. A refresh token is a secret: store it as Secret Management (Part B of the Migration Documentation) describes, and on compromise disable the client and replace it. Enable refresh tokens only if the application must keep a session beyond one token lifetime without asking the user to sign in again.

**PKCE.** PKCE is required by Relativity's own documentation to obtain a refresh token, and not otherwise: a Code request that does not ask for a refresh token works without it, **verified by NSerio against a live instance; not documented by Relativity**. It is never a blanket requirement. NSerio recommends PKCE with the `S256` method in both variants (NSerio recommendation), and does not claim that Relativity accepts any particular method, which it does not document.

**Scope.** No scope set is prescribed for Code. The scope below works — `openid RelativityRequestOrigin UserInfo UserInfoAccess AuthenticationID RelativityWeb` — **verified by NSerio against a live instance; not documented by Relativity**, verified as working and not as minimal. `state` is sent as standard OAuth practice and `nonce` is not used; Relativity does not document whether it enforces either.

**The application stops collecting the password.** After this change the application never receives, stores or forwards the user's username and password: the user signs in on RelativityOne's own page, and the application holds only tokens. Remove the credential prompt, the form field and any storage of the password that the replaced code needed (in a VB.NET estate: the `InputBox`, the login form's password control, the Web Forms login control's handler).

**Single sign-on.** Sign-in happens on RelativityOne's own login page, so the instance's single sign-on and two-factor rules apply unchanged — *inferred* from where the sign-in happens, and not documented for clients that customers create.

**The client secret is required.** Code does not work without a client secret: tested and rejected, **verified by NSerio against a live instance; not documented by Relativity**, which confirms Relativity's own statement that the grant requires one. It is displayed once and is stored as Secret Management (Part B of the Migration Documentation) describes.

**Server-side variant.** For a web application whose code runs on a server — in VB.NET, an ASP.NET Web Forms page or a custom page whose code-behind sends the redirect (`Response.Redirect(authorizeUrl)`) and receives the code in a route of the application:

- **Redirect.** The application receives the code at one exact HTTPS URL per environment, a route of the application, with no wildcards (NSerio recommendation; Relativity's matching rules are not documented). The Relativity administrator registers the exact URL on the OAuth2 Client.
- **Secret and tokens.** The client secret stays on the server and never reaches the browser. The user's tokens are held server-side, keyed to the session, and never in the browser.
- **Exchange.** The server performs the code exchange itself, from the code-behind or a handler.

**Desktop variant.** For a desktop or command-line application run by a user on their own machine (WinForms, WPF, console):

- **The system browser.** Open the authorize URL in the user's default browser and not in a web view the application hosts, so the application never sees the password (NSerio recommendation).
- **A loopback redirect on a fixed port.** Listen on the loopback address only, on the registered port, for one request. The Redirect URL is `http://127.0.0.1:<port>/<path>`: a plain-HTTP loopback URL is accepted, **verified by NSerio against a live instance; not documented by Relativity**. The Relativity administrator registers that exact URL; because it is registered in advance, the port is fixed and not chosen at runtime.

```vbnet
Using listener As New HttpListener()
	listener.Prefixes.Add("http://127.0.0.1:<port>/<path>/")
	listener.Start()
	Process.Start(New ProcessStartInfo(authorizeUrl) With {.UseShellExecute = True})
	Dim context As HttpListenerContext = Await listener.GetContextAsync()
	Dim authorizationCode As String = context.Request.QueryString("code")
	Dim returnedState As String = context.Request.QueryString("state")
	context.Response.Close()
End Using
```

- **The secret is extractable.** Code requires a client secret (above), so a distributed desktop application has to embed one, and **a secret shipped inside a distributed binary can be extracted by anyone who holds the binary**. It protects nothing against the person running the application.
- **Compensating guidance** (NSerio recommendation, subject to your own security policy): set Trusted IPs where the users' outbound addresses are known and stable — they restrict where a token can be *requested*, and are checked when a token is issued against the client's ID and secret; use the recommended short Access Token Lifetime of 60 minutes; enable refresh tokens only when needed; and create one OAuth2 Client per distribution, so a leaked secret can be disabled without affecting the others.
- **A known issue.** Relativity documents no recommendation for a distributed desktop application; this design is what NSerio verified works, and Relativity's own recommendation is tracked as a known issue of the release.

Verification: **Verification note — two things, and the first can break working code.** **A token has a finite lifetime**, set per client in minutes, and this construct holds it. A `ServiceFactory` built once from `BearerTokenCredentials(accessToken)` and kept for the life of a process keeps that token after it expires, and the calls made through it then fail authentication. Rebuild the settings and the factory with a current token within the token's lifetime; the previous credential never expired, so nothing in the existing code does this. **Confirm the SDK version in use provides `BearerTokenCredentials`.** Where it does not, this is an SDK upgrade before it is a code change.

OAuth2 Client: Create or confirm this OAuth2 Client in RelativityOne (NSerio's observation: the menu path is Home > Authentication > OAuth2 Client Manager, as Relativity's REST API authentication page names it). This tool never connects to RelativityOne and never creates or changes the OAuth2 Client; the settings below are guidance.
- Name: a descriptive name identifying the application it serves.
- Flow Grant Type: Code
- Redirect URLs: Relativity documentation: each URL includes the http or https protocol prefix. The exact callback URL is registered by the Relativity administrator. Server-side web applications, NSerio's recommendation: one exact HTTPS URL per environment, with no wildcards. Desktop and CLI applications: a plain-HTTP loopback URL, `http://127.0.0.1:<port>/<path>`, on a fixed port because the URL is registered in advance and the port cannot be chosen at runtime (verified by NSerio against a live instance; not documented by Relativity).
- Access Token Lifetime: 60 minutes, stated in minutes because the field is in minutes. Relativity documentation: tokens for this flow must have a short lifetime, and the recommended value matches the Identity Server default.
- Allow refresh tokens: NSerio's recommendation: leave it off unless the application must keep a user session active beyond the access token lifetime without asking the user to sign in again. If it is on, the refresh tokens are stored as secrets, per Secret Management (Part B of the Migration Documentation).
- Enabled: on.
<!-- generated:end VB-001 authorization-code -->

### VB-002 · SdkLegacyRsapiCredentials

<a id="vb-002-migration"></a>

#### Client Credentials

<!-- generated:begin VB-002 client-credentials -->
**Confirming the flow.** When no signal in a file decides the flow, the tool assigns Client Credentials, the default, with this note: "Who supplies the credential at run time? If a person types it or signs in, the integration acts for that person: use Authorization Code. If it comes from configuration, settings, an environment variable or a secret store: use Client Credentials. No signal in this file showed which applies, so Client Credentials, the default, is recommended. Use Authorization Code instead if the integration acts on behalf of a signed-in user, or if your RelativityOne instance already has a Code-flow OAuth2 Client for this integration." A fixed-credential signal assigns it with this note: "A fixed-credential signal in this file points to Client Credentials, but how often that kind of signal is right has not been measured. Use Authorization Code instead if the integration acts on behalf of a signed-in user, or if your RelativityOne instance already has a Code-flow OAuth2 Client for this integration." See [Confirming the flow](#confirming-the-flow) and the "Which flow" paragraph of VB-002 in [Part A](part-a-vbnet.md#vb-002).

Source form: **Applies to:** .NET Framework in practice (the legacy client assembly).

```vbnet
Dim client As New RSAPIClient(rsapiUri, New UsernamePasswordCredentials(userName, password))
```

Under token authentication: **First, the destination:** if this Relativity instance is not RelativityOne — including a Relativity Server instance, whose own documentation governs — this finding is informational and the rest of this template does not apply.

**Two steps, not one, and in this order.** First, **move the call to Object Manager**: the RSAPI client is retired, so swapping the credential inside the RSAPI construction migrates the scheme of a call that should not survive. Rewrite the operation as the equivalent Object Manager call, reached through a service factory. Second, apply the token credential to that rewritten call:

```vbnet
Dim settings As New ServiceFactorySettings(restEndpointUri, New BearerTokenCredentials(accessToken))
```

The REST URI is `<scheme>://<machine name>/Relativity.Rest/api`, which is not the RSAPI endpoint the source form addressed.

Credentials: **Where the credentials come from.** The SDK type establishes that the destination is Relativity, not that it is RelativityOne; if it is not RelativityOne — including a Relativity Server instance, whose own documentation governs — the migration is that service's own token mechanism, this finding is informational for this tool, and the reader stops here. If it is RelativityOne, the token comes from the token-acquisition part that follows.

**Where the token comes from.** Create an OAuth2 Client with Flow Grant Type set to Client Credentials (part 3 lists its settings), then send a form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=client_credentials`, `client_id`, `client_secret` and a `scope`. The token endpoint is **documented** for this grant. The token is then sent as `Authorization: Bearer <token>`, with `X-CSRF-Header: -` on REST and Kepler calls.

**.NET (both runtimes' `HttpClient`; `System.Text.Json` on .NET)**

```vbnet
Imports System.Net.Http
Imports System.Text.Json

Public Async Function AcquireAccessTokenAsync(host As String, clientId As String, clientSecret As String, scope As String) As Task(Of String)
	Dim accessToken As String
	Using client As New HttpClient()
		Dim form As New FormUrlEncodedContent(New Dictionary(Of String, String) From {
			{"grant_type", "client_credentials"},
			{"client_id", clientId},
			{"client_secret", clientSecret},
			{"scope", scope}})
		Using response As HttpResponseMessage = Await client.PostAsync(host & "/Relativity/Identity/connect/token", form)
			response.EnsureSuccessStatusCode()
			Using document As JsonDocument = JsonDocument.Parse(Await response.Content.ReadAsStringAsync())
				accessToken = document.RootElement.GetProperty("access_token").GetString()
			End Using
		End Using
	End Using
	Return accessToken
End Function
```

**.NET Framework 4.7.2** — the same call with the serializer the runtime ships (`System.Web.Extensions`); `HttpClient` is in `System.Net.Http`:

```vbnet
Imports System.Net.Http
Imports System.Web.Script.Serialization

Public Async Function AcquireAccessTokenAsync(host As String, clientId As String, clientSecret As String, scope As String) As Task(Of String)
	Dim accessToken As String
	Using client As New HttpClient()
		Dim form As New FormUrlEncodedContent(New Dictionary(Of String, String) From {
			{"grant_type", "client_credentials"},
			{"client_id", clientId},
			{"client_secret", clientSecret},
			{"scope", scope}})
		Using response As HttpResponseMessage = Await client.PostAsync(host & "/Relativity/Identity/connect/token", form)
			response.EnsureSuccessStatusCode()
			Dim body As String = Await response.Content.ReadAsStringAsync()
			Dim fields As Dictionary(Of String, Object) = New JavaScriptSerializer().Deserialize(Of Dictionary(Of String, Object))(body)
			accessToken = fields("access_token").ToString()
		End Using
	End Using
	Return accessToken
End Function
```

**The scope depends on what the integration needs, and no single scope is required.** Relativity's documentation shows two values for this grant: `SystemUserInfo` on the REST authentication page, the Usage Reports page and in the official Transfer SDK sample, and `UserInfoAccess` in the Kepler .NET samples. No page says which is correct, whether both work, or what either grants, and the OAuth2 Client's `Scopes` property is documented as one that cannot be set. Use the value the API you call documents, and confirm it.

**Cache the token, and re-acquire it on expiry — do not refresh.** Keep the token for its lifetime (part 3 recommends 60 minutes, Relativity's own wording) and request a new one with the client secret when it expires or a call is rejected for authentication. Client Credentials does not offer refresh tokens: **verified by NSerio against a live instance; not documented by Relativity** — the Allow Refresh Tokens control does not appear for this grant in the RelativityOne user interface. Relativity's own tables and text on refresh tokens conflict with each other, so re-acquisition with the secret is the advice that holds whichever way they are read.

**Long-running processes that use the Relativity SDK.** A `ServiceFactory` built once from a bearer token keeps that token after it expires. Rebuild the `ServiceFactory` and its proxies with a fresh token before expiry. This is the only advice consistent with the documentation: the package `Relativity.Kepler.Client.SDK` (documented) exposes a settable `Token` and an `HttpMessageHandler` overload but no token-provider overload, and no page documents renewing a token behind a factory.

**The client secret.** It is displayed once, at creation or reset. Store it as Secret Management (Part B of the Migration Documentation) describes; never in source, never in tracked configuration, which in a VB.NET estate includes `app.config`, `web.config` and `My.Settings`. A reset invalidates the previous secret immediately, so plan the cut-over before resetting it.

**Before deploying, confirm the calling identity.** Every call now runs as the OAuth2 Client's **Context User** — not as the user in the replaced credential, and for an import or a job as much as for a request. If that user has different workspace permissions or item-level security, the code returns fewer rows or `Unauthorized` in production with no change to its logic, and Relativity's audit attributes every call to that one account. Confirm the Context User's permissions match those of the previous identity. Relativity describes the Context User as usually a member of the Relativity Administrators group; the OAuth2 Client block (part 3) gives NSerio's recommendation, which differs.

**When the integration acts for a signed-in user.** Client Credentials is one service identity by construction. An integration that acts on behalf of a signed-in user is the Authorization Code case, and moving to it means a new OAuth2 Client, because the flow is fixed when a client is created. The finding's flow note says when to make that choice.

Verification: **Verification note — four things, and the first two can break working code.** **Verify equivalence before rewriting.** Not every RSAPI operation has a direct Object Manager counterpart; where one does not, this is a redesign of that operation, not a migration of it. **A token has a finite lifetime**: a service factory built once from the token and kept for the life of a process keeps it after it expires; rebuild it with a current token within that lifetime. **Other RSAPI uses may not appear as findings.** This tool reports an RSAPI client only where it is constructed with a username-and-password credential. **Confirm the SDK version in use provides `BearerTokenCredentials` and the Object Manager service.**

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
<!-- generated:end VB-002 client-credentials -->

#### Authorization Code

<!-- generated:begin VB-002 authorization-code -->
**Confirming the flow.** An interactive-user signal in a file assigns Authorization Code, with this note: "An interactive-user signal in this file points to Authorization Code. Confirm the flow: use Client Credentials instead if the integration runs with credentials fixed for the integration and no signed-in user." Your user profile can also make Authorization Code the default; see Changing the default in [Confirming the flow](#confirming-the-flow). See [Confirming the flow](#confirming-the-flow) and the "Which flow" paragraph of VB-002 in [Part A](part-a-vbnet.md#vb-002).

Source form: **Applies to:** .NET Framework in practice (the legacy client assembly).

```vbnet
Dim client As New RSAPIClient(rsapiUri, New UsernamePasswordCredentials(userName, password))
```

Under token authentication: **First, the destination:** if this Relativity instance is not RelativityOne — including a Relativity Server instance, whose own documentation governs — this finding is informational and the rest of this template does not apply.

**Two steps, not one, and in this order.** First, **move the call to Object Manager**: the RSAPI client is retired, so swapping the credential inside the RSAPI construction migrates the scheme of a call that should not survive. Rewrite the operation as the equivalent Object Manager call, reached through a service factory. Second, apply the token credential to that rewritten call:

```vbnet
Dim settings As New ServiceFactorySettings(restEndpointUri, New BearerTokenCredentials(accessToken))
```

The REST URI is `<scheme>://<machine name>/Relativity.Rest/api`, which is not the RSAPI endpoint the source form addressed.

Credentials: **Where the credentials come from.** The SDK type establishes that the destination is Relativity, not that it is RelativityOne; if it is not RelativityOne — including a Relativity Server instance, whose own documentation governs — the migration is that service's own token mechanism, this finding is informational for this tool, and the reader stops here. If it is RelativityOne, the token comes from the token-acquisition part that follows.

The text a finding assigned Authorization Code ("Code") receives after part 1. It has a common part and two variants, **server-side** and **desktop**. The bold lead-in of each element is its marker. Every fact Relativity does not document but NSerio verified carries the exact label **verified by NSerio against a live instance; not documented by Relativity**.

**Common to both variants**

**Sign in.** Replace the code that collected the user's credentials with a redirect: send the user's browser to the authorize endpoint `<host>/Relativity/Identity/connect/authorize`, which leads to the RelativityOne login page. The authorize endpoint is **verified by NSerio against a live instance; not documented by Relativity**. The request carries the standard authorization-code parameters — `client_id`, `response_type=code`, the registered `redirect_uri`, a `scope`, and a `state` value the application checks on return; these are standard OAuth, and Relativity documents none of them. No discovery document is relied on.

**Receive the code.** The browser returns to the Redirect URL registered on the OAuth2 Client, with the authorization code and the `state` value. Check the `state` before using the code. The Redirect URL must be exactly the one registered (the variants say how).

**Exchange the code.** Send a form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=authorization_code`, the `code`, the same `redirect_uri`, the `client_id` and the `client_secret`, plus the `code_verifier` when PKCE is used. The code is exchanged at that endpoint, which Relativity documents for the Client Credentials grant only; the exchange is **verified by NSerio against a live instance; not documented by Relativity**.

```vbnet
Dim form As New FormUrlEncodedContent(New Dictionary(Of String, String) From {
	{"grant_type", "authorization_code"},
	{"code", authorizationCode},
	{"redirect_uri", redirectUri},
	{"client_id", clientId},
	{"client_secret", clientSecret},
	{"code_verifier", codeVerifier}})
Using response As HttpResponseMessage = Await client.PostAsync(host & "/Relativity/Identity/connect/token", form)
	response.EnsureSuccessStatusCode()
End Using
```

**Use the token.** Send `Authorization: Bearer <access token>` — the change at the use site in part 1 — with `X-CSRF-Header: -` on REST and Kepler calls.

**On expiry.** With Allow Refresh Tokens enabled on the OAuth2 Client and PKCE used, the application holds a refresh token and, when the access token expires, sends a `grant_type=refresh_token` request with it and the client credentials in place of the code exchange — *inferred* from the standard OAuth shape, not tested. Without a refresh token the user signs in again. Relativity does not document a refresh token's lifetime, whether it rotates, how it is revoked, or whether a revocation endpoint exists, nor what disabling or deleting the client does to tokens already issued. A refresh token is a secret: store it as Secret Management (Part B of the Migration Documentation) describes, and on compromise disable the client and replace it. Enable refresh tokens only if the application must keep a session beyond one token lifetime without asking the user to sign in again.

**PKCE.** PKCE is required by Relativity's own documentation to obtain a refresh token, and not otherwise: a Code request that does not ask for a refresh token works without it, **verified by NSerio against a live instance; not documented by Relativity**. It is never a blanket requirement. NSerio recommends PKCE with the `S256` method in both variants (NSerio recommendation), and does not claim that Relativity accepts any particular method, which it does not document.

**Scope.** No scope set is prescribed for Code. The scope below works — `openid RelativityRequestOrigin UserInfo UserInfoAccess AuthenticationID RelativityWeb` — **verified by NSerio against a live instance; not documented by Relativity**, verified as working and not as minimal. `state` is sent as standard OAuth practice and `nonce` is not used; Relativity does not document whether it enforces either.

**The application stops collecting the password.** After this change the application never receives, stores or forwards the user's username and password: the user signs in on RelativityOne's own page, and the application holds only tokens. Remove the credential prompt, the form field and any storage of the password that the replaced code needed (in a VB.NET estate: the `InputBox`, the login form's password control, the Web Forms login control's handler).

**Single sign-on.** Sign-in happens on RelativityOne's own login page, so the instance's single sign-on and two-factor rules apply unchanged — *inferred* from where the sign-in happens, and not documented for clients that customers create.

**The client secret is required.** Code does not work without a client secret: tested and rejected, **verified by NSerio against a live instance; not documented by Relativity**, which confirms Relativity's own statement that the grant requires one. It is displayed once and is stored as Secret Management (Part B of the Migration Documentation) describes.

**Server-side variant.** For a web application whose code runs on a server — in VB.NET, an ASP.NET Web Forms page or a custom page whose code-behind sends the redirect (`Response.Redirect(authorizeUrl)`) and receives the code in a route of the application:

- **Redirect.** The application receives the code at one exact HTTPS URL per environment, a route of the application, with no wildcards (NSerio recommendation; Relativity's matching rules are not documented). The Relativity administrator registers the exact URL on the OAuth2 Client.
- **Secret and tokens.** The client secret stays on the server and never reaches the browser. The user's tokens are held server-side, keyed to the session, and never in the browser.
- **Exchange.** The server performs the code exchange itself, from the code-behind or a handler.

**Desktop variant.** For a desktop or command-line application run by a user on their own machine (WinForms, WPF, console):

- **The system browser.** Open the authorize URL in the user's default browser and not in a web view the application hosts, so the application never sees the password (NSerio recommendation).
- **A loopback redirect on a fixed port.** Listen on the loopback address only, on the registered port, for one request. The Redirect URL is `http://127.0.0.1:<port>/<path>`: a plain-HTTP loopback URL is accepted, **verified by NSerio against a live instance; not documented by Relativity**. The Relativity administrator registers that exact URL; because it is registered in advance, the port is fixed and not chosen at runtime.

```vbnet
Using listener As New HttpListener()
	listener.Prefixes.Add("http://127.0.0.1:<port>/<path>/")
	listener.Start()
	Process.Start(New ProcessStartInfo(authorizeUrl) With {.UseShellExecute = True})
	Dim context As HttpListenerContext = Await listener.GetContextAsync()
	Dim authorizationCode As String = context.Request.QueryString("code")
	Dim returnedState As String = context.Request.QueryString("state")
	context.Response.Close()
End Using
```

- **The secret is extractable.** Code requires a client secret (above), so a distributed desktop application has to embed one, and **a secret shipped inside a distributed binary can be extracted by anyone who holds the binary**. It protects nothing against the person running the application.
- **Compensating guidance** (NSerio recommendation, subject to your own security policy): set Trusted IPs where the users' outbound addresses are known and stable — they restrict where a token can be *requested*, and are checked when a token is issued against the client's ID and secret; use the recommended short Access Token Lifetime of 60 minutes; enable refresh tokens only when needed; and create one OAuth2 Client per distribution, so a leaked secret can be disabled without affecting the others.
- **A known issue.** Relativity documents no recommendation for a distributed desktop application; this design is what NSerio verified works, and Relativity's own recommendation is tracked as a known issue of the release.

Verification: **Verification note — four things, and the first two can break working code.** **Verify equivalence before rewriting.** Not every RSAPI operation has a direct Object Manager counterpart; where one does not, this is a redesign of that operation, not a migration of it. **A token has a finite lifetime**: a service factory built once from the token and kept for the life of a process keeps it after it expires; rebuild it with a current token within that lifetime. **Other RSAPI uses may not appear as findings.** This tool reports an RSAPI client only where it is constructed with a username-and-password credential. **Confirm the SDK version in use provides `BearerTokenCredentials` and the Object Manager service.**

OAuth2 Client: Create or confirm this OAuth2 Client in RelativityOne (NSerio's observation: the menu path is Home > Authentication > OAuth2 Client Manager, as Relativity's REST API authentication page names it). This tool never connects to RelativityOne and never creates or changes the OAuth2 Client; the settings below are guidance.
- Name: a descriptive name identifying the application it serves.
- Flow Grant Type: Code
- Redirect URLs: Relativity documentation: each URL includes the http or https protocol prefix. The exact callback URL is registered by the Relativity administrator. Server-side web applications, NSerio's recommendation: one exact HTTPS URL per environment, with no wildcards. Desktop and CLI applications: a plain-HTTP loopback URL, `http://127.0.0.1:<port>/<path>`, on a fixed port because the URL is registered in advance and the port cannot be chosen at runtime (verified by NSerio against a live instance; not documented by Relativity).
- Access Token Lifetime: 60 minutes, stated in minutes because the field is in minutes. Relativity documentation: tokens for this flow must have a short lifetime, and the recommended value matches the Identity Server default.
- Allow refresh tokens: NSerio's recommendation: leave it off unless the application must keep a user session active beyond the access token lifetime without asking the user to sign in again. If it is on, the refresh tokens are stored as secrets, per Secret Management (Part B of the Migration Documentation).
- Enabled: on.
<!-- generated:end VB-002 authorization-code -->

### VB-003 · CredentialCacheBasicScheme

<a id="vb-003-migration"></a>

#### Client Credentials

<!-- generated:begin VB-003 client-credentials -->
**Confirming the flow.** When no signal in a file decides the flow, the tool assigns Client Credentials, the default, with this note: "Who supplies the credential at run time? If a person types it or signs in, the integration acts for that person: use Authorization Code. If it comes from configuration, settings, an environment variable or a secret store: use Client Credentials. No signal in this file showed which applies, so Client Credentials, the default, is recommended. Use Authorization Code instead if the integration acts on behalf of a signed-in user, or if your RelativityOne instance already has a Code-flow OAuth2 Client for this integration." A fixed-credential signal assigns it with this note: "A fixed-credential signal in this file points to Client Credentials, but how often that kind of signal is right has not been measured. Use Authorization Code instead if the integration acts on behalf of a signed-in user, or if your RelativityOne instance already has a Code-flow OAuth2 Client for this integration." See [Confirming the flow](#confirming-the-flow) and the "Which flow" paragraph of VB-003 in [Part A](part-a-vbnet.md#vb-003).

Source form: **Applies to:** both.

```vbnet
cache.Add(New Uri(instanceUrl), "Basic", New NetworkCredential(userName, password))
```

Under token authentication: **First, the destination:** if it is not RelativityOne, this finding is informational — that service's own token mechanism is the migration — and the rest of this template does not apply.

Remove the credential-cache entry and **set the bearer header on each request**:

```vbnet
request.Headers.Authorization = New AuthenticationHeaderValue("Bearer", accessToken)
```

Credentials: **Where the credentials come from.** The destination decides whether the migration applies. If the destination is not RelativityOne, the migration is that service's own token mechanism, this finding is informational for this tool, and the reader stops here. If it is RelativityOne, the token comes from the token-acquisition part that follows.

**Where the token comes from.** Create an OAuth2 Client with Flow Grant Type set to Client Credentials (part 3 lists its settings), then send a form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=client_credentials`, `client_id`, `client_secret` and a `scope`. The token endpoint is **documented** for this grant. The token is then sent as `Authorization: Bearer <token>`, with `X-CSRF-Header: -` on REST and Kepler calls.

**.NET (both runtimes' `HttpClient`; `System.Text.Json` on .NET)**

```vbnet
Imports System.Net.Http
Imports System.Text.Json

Public Async Function AcquireAccessTokenAsync(host As String, clientId As String, clientSecret As String, scope As String) As Task(Of String)
	Dim accessToken As String
	Using client As New HttpClient()
		Dim form As New FormUrlEncodedContent(New Dictionary(Of String, String) From {
			{"grant_type", "client_credentials"},
			{"client_id", clientId},
			{"client_secret", clientSecret},
			{"scope", scope}})
		Using response As HttpResponseMessage = Await client.PostAsync(host & "/Relativity/Identity/connect/token", form)
			response.EnsureSuccessStatusCode()
			Using document As JsonDocument = JsonDocument.Parse(Await response.Content.ReadAsStringAsync())
				accessToken = document.RootElement.GetProperty("access_token").GetString()
			End Using
		End Using
	End Using
	Return accessToken
End Function
```

**.NET Framework 4.7.2** — the same call with the serializer the runtime ships (`System.Web.Extensions`); `HttpClient` is in `System.Net.Http`:

```vbnet
Imports System.Net.Http
Imports System.Web.Script.Serialization

Public Async Function AcquireAccessTokenAsync(host As String, clientId As String, clientSecret As String, scope As String) As Task(Of String)
	Dim accessToken As String
	Using client As New HttpClient()
		Dim form As New FormUrlEncodedContent(New Dictionary(Of String, String) From {
			{"grant_type", "client_credentials"},
			{"client_id", clientId},
			{"client_secret", clientSecret},
			{"scope", scope}})
		Using response As HttpResponseMessage = Await client.PostAsync(host & "/Relativity/Identity/connect/token", form)
			response.EnsureSuccessStatusCode()
			Dim body As String = Await response.Content.ReadAsStringAsync()
			Dim fields As Dictionary(Of String, Object) = New JavaScriptSerializer().Deserialize(Of Dictionary(Of String, Object))(body)
			accessToken = fields("access_token").ToString()
		End Using
	End Using
	Return accessToken
End Function
```

**The scope depends on what the integration needs, and no single scope is required.** Relativity's documentation shows two values for this grant: `SystemUserInfo` on the REST authentication page, the Usage Reports page and in the official Transfer SDK sample, and `UserInfoAccess` in the Kepler .NET samples. No page says which is correct, whether both work, or what either grants, and the OAuth2 Client's `Scopes` property is documented as one that cannot be set. Use the value the API you call documents, and confirm it.

**Cache the token, and re-acquire it on expiry — do not refresh.** Keep the token for its lifetime (part 3 recommends 60 minutes, Relativity's own wording) and request a new one with the client secret when it expires or a call is rejected for authentication. Client Credentials does not offer refresh tokens: **verified by NSerio against a live instance; not documented by Relativity** — the Allow Refresh Tokens control does not appear for this grant in the RelativityOne user interface. Relativity's own tables and text on refresh tokens conflict with each other, so re-acquisition with the secret is the advice that holds whichever way they are read.

**Long-running processes that use the Relativity SDK.** A `ServiceFactory` built once from a bearer token keeps that token after it expires. Rebuild the `ServiceFactory` and its proxies with a fresh token before expiry. This is the only advice consistent with the documentation: the package `Relativity.Kepler.Client.SDK` (documented) exposes a settable `Token` and an `HttpMessageHandler` overload but no token-provider overload, and no page documents renewing a token behind a factory.

**The client secret.** It is displayed once, at creation or reset. Store it as Secret Management (Part B of the Migration Documentation) describes; never in source, never in tracked configuration, which in a VB.NET estate includes `app.config`, `web.config` and `My.Settings`. A reset invalidates the previous secret immediately, so plan the cut-over before resetting it.

**Before deploying, confirm the calling identity.** Every call now runs as the OAuth2 Client's **Context User** — not as the user in the replaced credential, and for an import or a job as much as for a request. If that user has different workspace permissions or item-level security, the code returns fewer rows or `Unauthorized` in production with no change to its logic, and Relativity's audit attributes every call to that one account. Confirm the Context User's permissions match those of the previous identity. Relativity describes the Context User as usually a member of the Relativity Administrators group; the OAuth2 Client block (part 3) gives NSerio's recommendation, which differs.

**When the integration acts for a signed-in user.** Client Credentials is one service identity by construction. An integration that acts on behalf of a signed-in user is the Authorization Code case, and moving to it means a new OAuth2 Client, because the flow is fixed when a client is created. The finding's flow note says when to make that choice.

Verification: **Verification note — three things.** **The 401-challenge response is lost**: a cache entry answers a server's challenge, a bearer header is sent up front and answers nothing; code or a server that depended on that exchange behaves differently after the change. **Redirects stop carrying the credential**: a manually set `Authorization` header is never re-applied to a redirect target, so code that relied on a redirect can start returning 401; address the final URI or handle the redirect explicitly. **Confirm the endpoint in use is the RelativityOne REST API**; an endpoint elsewhere reached from the same code may accept only Basic.

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
<!-- generated:end VB-003 client-credentials -->

#### Authorization Code

<!-- generated:begin VB-003 authorization-code -->
**Confirming the flow.** An interactive-user signal in a file assigns Authorization Code, with this note: "An interactive-user signal in this file points to Authorization Code. Confirm the flow: use Client Credentials instead if the integration runs with credentials fixed for the integration and no signed-in user." Your user profile can also make Authorization Code the default; see Changing the default in [Confirming the flow](#confirming-the-flow). See [Confirming the flow](#confirming-the-flow) and the "Which flow" paragraph of VB-003 in [Part A](part-a-vbnet.md#vb-003).

Source form: **Applies to:** both.

```vbnet
cache.Add(New Uri(instanceUrl), "Basic", New NetworkCredential(userName, password))
```

Under token authentication: **First, the destination:** if it is not RelativityOne, this finding is informational — that service's own token mechanism is the migration — and the rest of this template does not apply.

Remove the credential-cache entry and **set the bearer header on each request**:

```vbnet
request.Headers.Authorization = New AuthenticationHeaderValue("Bearer", accessToken)
```

Credentials: **Where the credentials come from.** The destination decides whether the migration applies. If the destination is not RelativityOne, the migration is that service's own token mechanism, this finding is informational for this tool, and the reader stops here. If it is RelativityOne, the token comes from the token-acquisition part that follows.

The text a finding assigned Authorization Code ("Code") receives after part 1. It has a common part and two variants, **server-side** and **desktop**. The bold lead-in of each element is its marker. Every fact Relativity does not document but NSerio verified carries the exact label **verified by NSerio against a live instance; not documented by Relativity**.

**Common to both variants**

**Sign in.** Replace the code that collected the user's credentials with a redirect: send the user's browser to the authorize endpoint `<host>/Relativity/Identity/connect/authorize`, which leads to the RelativityOne login page. The authorize endpoint is **verified by NSerio against a live instance; not documented by Relativity**. The request carries the standard authorization-code parameters — `client_id`, `response_type=code`, the registered `redirect_uri`, a `scope`, and a `state` value the application checks on return; these are standard OAuth, and Relativity documents none of them. No discovery document is relied on.

**Receive the code.** The browser returns to the Redirect URL registered on the OAuth2 Client, with the authorization code and the `state` value. Check the `state` before using the code. The Redirect URL must be exactly the one registered (the variants say how).

**Exchange the code.** Send a form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=authorization_code`, the `code`, the same `redirect_uri`, the `client_id` and the `client_secret`, plus the `code_verifier` when PKCE is used. The code is exchanged at that endpoint, which Relativity documents for the Client Credentials grant only; the exchange is **verified by NSerio against a live instance; not documented by Relativity**.

```vbnet
Dim form As New FormUrlEncodedContent(New Dictionary(Of String, String) From {
	{"grant_type", "authorization_code"},
	{"code", authorizationCode},
	{"redirect_uri", redirectUri},
	{"client_id", clientId},
	{"client_secret", clientSecret},
	{"code_verifier", codeVerifier}})
Using response As HttpResponseMessage = Await client.PostAsync(host & "/Relativity/Identity/connect/token", form)
	response.EnsureSuccessStatusCode()
End Using
```

**Use the token.** Send `Authorization: Bearer <access token>` — the change at the use site in part 1 — with `X-CSRF-Header: -` on REST and Kepler calls.

**On expiry.** With Allow Refresh Tokens enabled on the OAuth2 Client and PKCE used, the application holds a refresh token and, when the access token expires, sends a `grant_type=refresh_token` request with it and the client credentials in place of the code exchange — *inferred* from the standard OAuth shape, not tested. Without a refresh token the user signs in again. Relativity does not document a refresh token's lifetime, whether it rotates, how it is revoked, or whether a revocation endpoint exists, nor what disabling or deleting the client does to tokens already issued. A refresh token is a secret: store it as Secret Management (Part B of the Migration Documentation) describes, and on compromise disable the client and replace it. Enable refresh tokens only if the application must keep a session beyond one token lifetime without asking the user to sign in again.

**PKCE.** PKCE is required by Relativity's own documentation to obtain a refresh token, and not otherwise: a Code request that does not ask for a refresh token works without it, **verified by NSerio against a live instance; not documented by Relativity**. It is never a blanket requirement. NSerio recommends PKCE with the `S256` method in both variants (NSerio recommendation), and does not claim that Relativity accepts any particular method, which it does not document.

**Scope.** No scope set is prescribed for Code. The scope below works — `openid RelativityRequestOrigin UserInfo UserInfoAccess AuthenticationID RelativityWeb` — **verified by NSerio against a live instance; not documented by Relativity**, verified as working and not as minimal. `state` is sent as standard OAuth practice and `nonce` is not used; Relativity does not document whether it enforces either.

**The application stops collecting the password.** After this change the application never receives, stores or forwards the user's username and password: the user signs in on RelativityOne's own page, and the application holds only tokens. Remove the credential prompt, the form field and any storage of the password that the replaced code needed (in a VB.NET estate: the `InputBox`, the login form's password control, the Web Forms login control's handler).

**Single sign-on.** Sign-in happens on RelativityOne's own login page, so the instance's single sign-on and two-factor rules apply unchanged — *inferred* from where the sign-in happens, and not documented for clients that customers create.

**The client secret is required.** Code does not work without a client secret: tested and rejected, **verified by NSerio against a live instance; not documented by Relativity**, which confirms Relativity's own statement that the grant requires one. It is displayed once and is stored as Secret Management (Part B of the Migration Documentation) describes.

**Server-side variant.** For a web application whose code runs on a server — in VB.NET, an ASP.NET Web Forms page or a custom page whose code-behind sends the redirect (`Response.Redirect(authorizeUrl)`) and receives the code in a route of the application:

- **Redirect.** The application receives the code at one exact HTTPS URL per environment, a route of the application, with no wildcards (NSerio recommendation; Relativity's matching rules are not documented). The Relativity administrator registers the exact URL on the OAuth2 Client.
- **Secret and tokens.** The client secret stays on the server and never reaches the browser. The user's tokens are held server-side, keyed to the session, and never in the browser.
- **Exchange.** The server performs the code exchange itself, from the code-behind or a handler.

**Desktop variant.** For a desktop or command-line application run by a user on their own machine (WinForms, WPF, console):

- **The system browser.** Open the authorize URL in the user's default browser and not in a web view the application hosts, so the application never sees the password (NSerio recommendation).
- **A loopback redirect on a fixed port.** Listen on the loopback address only, on the registered port, for one request. The Redirect URL is `http://127.0.0.1:<port>/<path>`: a plain-HTTP loopback URL is accepted, **verified by NSerio against a live instance; not documented by Relativity**. The Relativity administrator registers that exact URL; because it is registered in advance, the port is fixed and not chosen at runtime.

```vbnet
Using listener As New HttpListener()
	listener.Prefixes.Add("http://127.0.0.1:<port>/<path>/")
	listener.Start()
	Process.Start(New ProcessStartInfo(authorizeUrl) With {.UseShellExecute = True})
	Dim context As HttpListenerContext = Await listener.GetContextAsync()
	Dim authorizationCode As String = context.Request.QueryString("code")
	Dim returnedState As String = context.Request.QueryString("state")
	context.Response.Close()
End Using
```

- **The secret is extractable.** Code requires a client secret (above), so a distributed desktop application has to embed one, and **a secret shipped inside a distributed binary can be extracted by anyone who holds the binary**. It protects nothing against the person running the application.
- **Compensating guidance** (NSerio recommendation, subject to your own security policy): set Trusted IPs where the users' outbound addresses are known and stable — they restrict where a token can be *requested*, and are checked when a token is issued against the client's ID and secret; use the recommended short Access Token Lifetime of 60 minutes; enable refresh tokens only when needed; and create one OAuth2 Client per distribution, so a leaked secret can be disabled without affecting the others.
- **A known issue.** Relativity documents no recommendation for a distributed desktop application; this design is what NSerio verified works, and Relativity's own recommendation is tracked as a known issue of the release.

Verification: **Verification note — three things.** **The 401-challenge response is lost**: a cache entry answers a server's challenge, a bearer header is sent up front and answers nothing; code or a server that depended on that exchange behaves differently after the change. **Redirects stop carrying the credential**: a manually set `Authorization` header is never re-applied to a redirect target, so code that relied on a redirect can start returning 401; address the final URI or handle the redirect explicitly. **Confirm the endpoint in use is the RelativityOne REST API**; an endpoint elsewhere reached from the same code may accept only Basic.

OAuth2 Client: Create or confirm this OAuth2 Client in RelativityOne (NSerio's observation: the menu path is Home > Authentication > OAuth2 Client Manager, as Relativity's REST API authentication page names it). This tool never connects to RelativityOne and never creates or changes the OAuth2 Client; the settings below are guidance.
- Name: a descriptive name identifying the application it serves.
- Flow Grant Type: Code
- Redirect URLs: Relativity documentation: each URL includes the http or https protocol prefix. The exact callback URL is registered by the Relativity administrator. Server-side web applications, NSerio's recommendation: one exact HTTPS URL per environment, with no wildcards. Desktop and CLI applications: a plain-HTTP loopback URL, `http://127.0.0.1:<port>/<path>`, on a fixed port because the URL is registered in advance and the port cannot be chosen at runtime (verified by NSerio against a live instance; not documented by Relativity).
- Access Token Lifetime: 60 minutes, stated in minutes because the field is in minutes. Relativity documentation: tokens for this flow must have a short lifetime, and the recommended value matches the Identity Server default.
- Allow refresh tokens: NSerio's recommendation: leave it off unless the application must keep a user session active beyond the access token lifetime without asking the user to sign in again. If it is on, the refresh tokens are stored as secrets, per Secret Management (Part B of the Migration Documentation).
- Enabled: on.
<!-- generated:end VB-003 authorization-code -->

### VB-004 · ThirdPartyBasicAuthenticatorType

<a id="vb-004-migration"></a>

#### Client Credentials

<!-- generated:begin VB-004 client-credentials -->
**Confirming the flow.** When no signal in a file decides the flow, the tool assigns Client Credentials, the default, with this note: "Who supplies the credential at run time? If a person types it or signs in, the integration acts for that person: use Authorization Code. If it comes from configuration, settings, an environment variable or a secret store: use Client Credentials. No signal in this file showed which applies, so Client Credentials, the default, is recommended. Use Authorization Code instead if the integration acts on behalf of a signed-in user, or if your RelativityOne instance already has a Code-flow OAuth2 Client for this integration." A fixed-credential signal assigns it with this note: "A fixed-credential signal in this file points to Client Credentials, but how often that kind of signal is right has not been measured. Use Authorization Code instead if the integration acts on behalf of a signed-in user, or if your RelativityOne instance already has a Code-flow OAuth2 Client for this integration." See [Confirming the flow](#confirming-the-flow) and the "Which flow" paragraph of VB-004 in [Part A](part-a-vbnet.md#vb-004).

Source form: **Applies to:** both.

```vbnet
client.Authenticator = New HttpBasicAuthenticator(userName, password)
```

Under token authentication: **First, the destination:** if it is not RelativityOne, this finding is informational — that service's own token mechanism is the migration — and the rest of this template does not apply.

Replace the Basic authenticator with **the same library's bearer or token authenticator, carrying the access token**. This template names no target type: the construct is a type whose name names the scheme, and the token authenticator's name differs between libraries and versions.

Credentials: **Where the credentials come from.** The destination decides whether the migration applies. If the destination is not RelativityOne, the migration is that service's own token mechanism, this finding is informational for this tool, and the reader stops here. If it is RelativityOne, the token comes from the token-acquisition part that follows.

**Where the token comes from.** Create an OAuth2 Client with Flow Grant Type set to Client Credentials (part 3 lists its settings), then send a form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=client_credentials`, `client_id`, `client_secret` and a `scope`. The token endpoint is **documented** for this grant. The token is then sent as `Authorization: Bearer <token>`, with `X-CSRF-Header: -` on REST and Kepler calls.

**.NET (both runtimes' `HttpClient`; `System.Text.Json` on .NET)**

```vbnet
Imports System.Net.Http
Imports System.Text.Json

Public Async Function AcquireAccessTokenAsync(host As String, clientId As String, clientSecret As String, scope As String) As Task(Of String)
	Dim accessToken As String
	Using client As New HttpClient()
		Dim form As New FormUrlEncodedContent(New Dictionary(Of String, String) From {
			{"grant_type", "client_credentials"},
			{"client_id", clientId},
			{"client_secret", clientSecret},
			{"scope", scope}})
		Using response As HttpResponseMessage = Await client.PostAsync(host & "/Relativity/Identity/connect/token", form)
			response.EnsureSuccessStatusCode()
			Using document As JsonDocument = JsonDocument.Parse(Await response.Content.ReadAsStringAsync())
				accessToken = document.RootElement.GetProperty("access_token").GetString()
			End Using
		End Using
	End Using
	Return accessToken
End Function
```

**.NET Framework 4.7.2** — the same call with the serializer the runtime ships (`System.Web.Extensions`); `HttpClient` is in `System.Net.Http`:

```vbnet
Imports System.Net.Http
Imports System.Web.Script.Serialization

Public Async Function AcquireAccessTokenAsync(host As String, clientId As String, clientSecret As String, scope As String) As Task(Of String)
	Dim accessToken As String
	Using client As New HttpClient()
		Dim form As New FormUrlEncodedContent(New Dictionary(Of String, String) From {
			{"grant_type", "client_credentials"},
			{"client_id", clientId},
			{"client_secret", clientSecret},
			{"scope", scope}})
		Using response As HttpResponseMessage = Await client.PostAsync(host & "/Relativity/Identity/connect/token", form)
			response.EnsureSuccessStatusCode()
			Dim body As String = Await response.Content.ReadAsStringAsync()
			Dim fields As Dictionary(Of String, Object) = New JavaScriptSerializer().Deserialize(Of Dictionary(Of String, Object))(body)
			accessToken = fields("access_token").ToString()
		End Using
	End Using
	Return accessToken
End Function
```

**The scope depends on what the integration needs, and no single scope is required.** Relativity's documentation shows two values for this grant: `SystemUserInfo` on the REST authentication page, the Usage Reports page and in the official Transfer SDK sample, and `UserInfoAccess` in the Kepler .NET samples. No page says which is correct, whether both work, or what either grants, and the OAuth2 Client's `Scopes` property is documented as one that cannot be set. Use the value the API you call documents, and confirm it.

**Cache the token, and re-acquire it on expiry — do not refresh.** Keep the token for its lifetime (part 3 recommends 60 minutes, Relativity's own wording) and request a new one with the client secret when it expires or a call is rejected for authentication. Client Credentials does not offer refresh tokens: **verified by NSerio against a live instance; not documented by Relativity** — the Allow Refresh Tokens control does not appear for this grant in the RelativityOne user interface. Relativity's own tables and text on refresh tokens conflict with each other, so re-acquisition with the secret is the advice that holds whichever way they are read.

**Long-running processes that use the Relativity SDK.** A `ServiceFactory` built once from a bearer token keeps that token after it expires. Rebuild the `ServiceFactory` and its proxies with a fresh token before expiry. This is the only advice consistent with the documentation: the package `Relativity.Kepler.Client.SDK` (documented) exposes a settable `Token` and an `HttpMessageHandler` overload but no token-provider overload, and no page documents renewing a token behind a factory.

**The client secret.** It is displayed once, at creation or reset. Store it as Secret Management (Part B of the Migration Documentation) describes; never in source, never in tracked configuration, which in a VB.NET estate includes `app.config`, `web.config` and `My.Settings`. A reset invalidates the previous secret immediately, so plan the cut-over before resetting it.

**Before deploying, confirm the calling identity.** Every call now runs as the OAuth2 Client's **Context User** — not as the user in the replaced credential, and for an import or a job as much as for a request. If that user has different workspace permissions or item-level security, the code returns fewer rows or `Unauthorized` in production with no change to its logic, and Relativity's audit attributes every call to that one account. Confirm the Context User's permissions match those of the previous identity. Relativity describes the Context User as usually a member of the Relativity Administrators group; the OAuth2 Client block (part 3) gives NSerio's recommendation, which differs.

**When the integration acts for a signed-in user.** Client Credentials is one service identity by construction. An integration that acts on behalf of a signed-in user is the Authorization Code case, and moving to it means a new OAuth2 Client, because the flow is fixed when a client is created. The finding's flow note says when to make that choice.

Verification: **Verification note — three things.** **Confirm the library version in use offers a token authenticator**; where it does not, this is a library upgrade before it is a code change. **A token has a finite lifetime**: an authenticator set once on a client shared across requests holds a token that expires; pass a current token where the request is built. **Confirm the endpoint in use is the RelativityOne REST API.**

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
<!-- generated:end VB-004 client-credentials -->

#### Authorization Code

<!-- generated:begin VB-004 authorization-code -->
**Confirming the flow.** An interactive-user signal in a file assigns Authorization Code, with this note: "An interactive-user signal in this file points to Authorization Code. Confirm the flow: use Client Credentials instead if the integration runs with credentials fixed for the integration and no signed-in user." Your user profile can also make Authorization Code the default; see Changing the default in [Confirming the flow](#confirming-the-flow). See [Confirming the flow](#confirming-the-flow) and the "Which flow" paragraph of VB-004 in [Part A](part-a-vbnet.md#vb-004).

Source form: **Applies to:** both.

```vbnet
client.Authenticator = New HttpBasicAuthenticator(userName, password)
```

Under token authentication: **First, the destination:** if it is not RelativityOne, this finding is informational — that service's own token mechanism is the migration — and the rest of this template does not apply.

Replace the Basic authenticator with **the same library's bearer or token authenticator, carrying the access token**. This template names no target type: the construct is a type whose name names the scheme, and the token authenticator's name differs between libraries and versions.

Credentials: **Where the credentials come from.** The destination decides whether the migration applies. If the destination is not RelativityOne, the migration is that service's own token mechanism, this finding is informational for this tool, and the reader stops here. If it is RelativityOne, the token comes from the token-acquisition part that follows.

The text a finding assigned Authorization Code ("Code") receives after part 1. It has a common part and two variants, **server-side** and **desktop**. The bold lead-in of each element is its marker. Every fact Relativity does not document but NSerio verified carries the exact label **verified by NSerio against a live instance; not documented by Relativity**.

**Common to both variants**

**Sign in.** Replace the code that collected the user's credentials with a redirect: send the user's browser to the authorize endpoint `<host>/Relativity/Identity/connect/authorize`, which leads to the RelativityOne login page. The authorize endpoint is **verified by NSerio against a live instance; not documented by Relativity**. The request carries the standard authorization-code parameters — `client_id`, `response_type=code`, the registered `redirect_uri`, a `scope`, and a `state` value the application checks on return; these are standard OAuth, and Relativity documents none of them. No discovery document is relied on.

**Receive the code.** The browser returns to the Redirect URL registered on the OAuth2 Client, with the authorization code and the `state` value. Check the `state` before using the code. The Redirect URL must be exactly the one registered (the variants say how).

**Exchange the code.** Send a form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=authorization_code`, the `code`, the same `redirect_uri`, the `client_id` and the `client_secret`, plus the `code_verifier` when PKCE is used. The code is exchanged at that endpoint, which Relativity documents for the Client Credentials grant only; the exchange is **verified by NSerio against a live instance; not documented by Relativity**.

```vbnet
Dim form As New FormUrlEncodedContent(New Dictionary(Of String, String) From {
	{"grant_type", "authorization_code"},
	{"code", authorizationCode},
	{"redirect_uri", redirectUri},
	{"client_id", clientId},
	{"client_secret", clientSecret},
	{"code_verifier", codeVerifier}})
Using response As HttpResponseMessage = Await client.PostAsync(host & "/Relativity/Identity/connect/token", form)
	response.EnsureSuccessStatusCode()
End Using
```

**Use the token.** Send `Authorization: Bearer <access token>` — the change at the use site in part 1 — with `X-CSRF-Header: -` on REST and Kepler calls.

**On expiry.** With Allow Refresh Tokens enabled on the OAuth2 Client and PKCE used, the application holds a refresh token and, when the access token expires, sends a `grant_type=refresh_token` request with it and the client credentials in place of the code exchange — *inferred* from the standard OAuth shape, not tested. Without a refresh token the user signs in again. Relativity does not document a refresh token's lifetime, whether it rotates, how it is revoked, or whether a revocation endpoint exists, nor what disabling or deleting the client does to tokens already issued. A refresh token is a secret: store it as Secret Management (Part B of the Migration Documentation) describes, and on compromise disable the client and replace it. Enable refresh tokens only if the application must keep a session beyond one token lifetime without asking the user to sign in again.

**PKCE.** PKCE is required by Relativity's own documentation to obtain a refresh token, and not otherwise: a Code request that does not ask for a refresh token works without it, **verified by NSerio against a live instance; not documented by Relativity**. It is never a blanket requirement. NSerio recommends PKCE with the `S256` method in both variants (NSerio recommendation), and does not claim that Relativity accepts any particular method, which it does not document.

**Scope.** No scope set is prescribed for Code. The scope below works — `openid RelativityRequestOrigin UserInfo UserInfoAccess AuthenticationID RelativityWeb` — **verified by NSerio against a live instance; not documented by Relativity**, verified as working and not as minimal. `state` is sent as standard OAuth practice and `nonce` is not used; Relativity does not document whether it enforces either.

**The application stops collecting the password.** After this change the application never receives, stores or forwards the user's username and password: the user signs in on RelativityOne's own page, and the application holds only tokens. Remove the credential prompt, the form field and any storage of the password that the replaced code needed (in a VB.NET estate: the `InputBox`, the login form's password control, the Web Forms login control's handler).

**Single sign-on.** Sign-in happens on RelativityOne's own login page, so the instance's single sign-on and two-factor rules apply unchanged — *inferred* from where the sign-in happens, and not documented for clients that customers create.

**The client secret is required.** Code does not work without a client secret: tested and rejected, **verified by NSerio against a live instance; not documented by Relativity**, which confirms Relativity's own statement that the grant requires one. It is displayed once and is stored as Secret Management (Part B of the Migration Documentation) describes.

**Server-side variant.** For a web application whose code runs on a server — in VB.NET, an ASP.NET Web Forms page or a custom page whose code-behind sends the redirect (`Response.Redirect(authorizeUrl)`) and receives the code in a route of the application:

- **Redirect.** The application receives the code at one exact HTTPS URL per environment, a route of the application, with no wildcards (NSerio recommendation; Relativity's matching rules are not documented). The Relativity administrator registers the exact URL on the OAuth2 Client.
- **Secret and tokens.** The client secret stays on the server and never reaches the browser. The user's tokens are held server-side, keyed to the session, and never in the browser.
- **Exchange.** The server performs the code exchange itself, from the code-behind or a handler.

**Desktop variant.** For a desktop or command-line application run by a user on their own machine (WinForms, WPF, console):

- **The system browser.** Open the authorize URL in the user's default browser and not in a web view the application hosts, so the application never sees the password (NSerio recommendation).
- **A loopback redirect on a fixed port.** Listen on the loopback address only, on the registered port, for one request. The Redirect URL is `http://127.0.0.1:<port>/<path>`: a plain-HTTP loopback URL is accepted, **verified by NSerio against a live instance; not documented by Relativity**. The Relativity administrator registers that exact URL; because it is registered in advance, the port is fixed and not chosen at runtime.

```vbnet
Using listener As New HttpListener()
	listener.Prefixes.Add("http://127.0.0.1:<port>/<path>/")
	listener.Start()
	Process.Start(New ProcessStartInfo(authorizeUrl) With {.UseShellExecute = True})
	Dim context As HttpListenerContext = Await listener.GetContextAsync()
	Dim authorizationCode As String = context.Request.QueryString("code")
	Dim returnedState As String = context.Request.QueryString("state")
	context.Response.Close()
End Using
```

- **The secret is extractable.** Code requires a client secret (above), so a distributed desktop application has to embed one, and **a secret shipped inside a distributed binary can be extracted by anyone who holds the binary**. It protects nothing against the person running the application.
- **Compensating guidance** (NSerio recommendation, subject to your own security policy): set Trusted IPs where the users' outbound addresses are known and stable — they restrict where a token can be *requested*, and are checked when a token is issued against the client's ID and secret; use the recommended short Access Token Lifetime of 60 minutes; enable refresh tokens only when needed; and create one OAuth2 Client per distribution, so a leaked secret can be disabled without affecting the others.
- **A known issue.** Relativity documents no recommendation for a distributed desktop application; this design is what NSerio verified works, and Relativity's own recommendation is tracked as a known issue of the release.

Verification: **Verification note — three things.** **Confirm the library version in use offers a token authenticator**; where it does not, this is a library upgrade before it is a code change. **A token has a finite lifetime**: an authenticator set once on a client shared across requests holds a token that expires; pass a current token where the request is built. **Confirm the endpoint in use is the RelativityOne REST API.**

OAuth2 Client: Create or confirm this OAuth2 Client in RelativityOne (NSerio's observation: the menu path is Home > Authentication > OAuth2 Client Manager, as Relativity's REST API authentication page names it). This tool never connects to RelativityOne and never creates or changes the OAuth2 Client; the settings below are guidance.
- Name: a descriptive name identifying the application it serves.
- Flow Grant Type: Code
- Redirect URLs: Relativity documentation: each URL includes the http or https protocol prefix. The exact callback URL is registered by the Relativity administrator. Server-side web applications, NSerio's recommendation: one exact HTTPS URL per environment, with no wildcards. Desktop and CLI applications: a plain-HTTP loopback URL, `http://127.0.0.1:<port>/<path>`, on a fixed port because the URL is registered in advance and the port cannot be chosen at runtime (verified by NSerio against a live instance; not documented by Relativity).
- Access Token Lifetime: 60 minutes, stated in minutes because the field is in minutes. Relativity documentation: tokens for this flow must have a short lifetime, and the recommended value matches the Identity Server default.
- Allow refresh tokens: NSerio's recommendation: leave it off unless the application must keep a user session active beyond the access token lifetime without asking the user to sign in again. If it is on, the refresh tokens are stored as secrets, per Secret Management (Part B of the Migration Documentation).
- Enabled: on.
<!-- generated:end VB-004 authorization-code -->

### VB-005 · SchemeNamingExtensionMethod

<a id="vb-005-migration"></a>

#### Client Credentials

<!-- generated:begin VB-005 client-credentials -->
**Confirming the flow.** When no signal in a file decides the flow, the tool assigns Client Credentials, the default, with this note: "Who supplies the credential at run time? If a person types it or signs in, the integration acts for that person: use Authorization Code. If it comes from configuration, settings, an environment variable or a secret store: use Client Credentials. No signal in this file showed which applies, so Client Credentials, the default, is recommended. Use Authorization Code instead if the integration acts on behalf of a signed-in user, or if your RelativityOne instance already has a Code-flow OAuth2 Client for this integration." A fixed-credential signal assigns it with this note: "A fixed-credential signal in this file points to Client Credentials, but how often that kind of signal is right has not been measured. Use Authorization Code instead if the integration acts on behalf of a signed-in user, or if your RelativityOne instance already has a Code-flow OAuth2 Client for this integration." See [Confirming the flow](#confirming-the-flow) and the "Which flow" paragraph of VB-005 in [Part A](part-a-vbnet.md#vb-005).

Source form: **Applies to:** both.

```vbnet
Dim authenticatedRequest = request.WithBasicAuth(userName, password)
```

Under token authentication: **First, the destination:** if it is not RelativityOne, this finding is informational — that service's own token mechanism is the migration — and the rest of this template does not apply.

Replace the Basic method with **the same library's bearer or token method, carrying the access token** in place of the credential pair. This template names no target method: the token method's name differs between libraries and versions.

Credentials: **Where the credentials come from.** The destination decides whether the migration applies. If the destination is not RelativityOne, the migration is that service's own token mechanism, this finding is informational for this tool, and the reader stops here. If it is RelativityOne, the token comes from the token-acquisition part that follows.

**Where the token comes from.** Create an OAuth2 Client with Flow Grant Type set to Client Credentials (part 3 lists its settings), then send a form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=client_credentials`, `client_id`, `client_secret` and a `scope`. The token endpoint is **documented** for this grant. The token is then sent as `Authorization: Bearer <token>`, with `X-CSRF-Header: -` on REST and Kepler calls.

**.NET (both runtimes' `HttpClient`; `System.Text.Json` on .NET)**

```vbnet
Imports System.Net.Http
Imports System.Text.Json

Public Async Function AcquireAccessTokenAsync(host As String, clientId As String, clientSecret As String, scope As String) As Task(Of String)
	Dim accessToken As String
	Using client As New HttpClient()
		Dim form As New FormUrlEncodedContent(New Dictionary(Of String, String) From {
			{"grant_type", "client_credentials"},
			{"client_id", clientId},
			{"client_secret", clientSecret},
			{"scope", scope}})
		Using response As HttpResponseMessage = Await client.PostAsync(host & "/Relativity/Identity/connect/token", form)
			response.EnsureSuccessStatusCode()
			Using document As JsonDocument = JsonDocument.Parse(Await response.Content.ReadAsStringAsync())
				accessToken = document.RootElement.GetProperty("access_token").GetString()
			End Using
		End Using
	End Using
	Return accessToken
End Function
```

**.NET Framework 4.7.2** — the same call with the serializer the runtime ships (`System.Web.Extensions`); `HttpClient` is in `System.Net.Http`:

```vbnet
Imports System.Net.Http
Imports System.Web.Script.Serialization

Public Async Function AcquireAccessTokenAsync(host As String, clientId As String, clientSecret As String, scope As String) As Task(Of String)
	Dim accessToken As String
	Using client As New HttpClient()
		Dim form As New FormUrlEncodedContent(New Dictionary(Of String, String) From {
			{"grant_type", "client_credentials"},
			{"client_id", clientId},
			{"client_secret", clientSecret},
			{"scope", scope}})
		Using response As HttpResponseMessage = Await client.PostAsync(host & "/Relativity/Identity/connect/token", form)
			response.EnsureSuccessStatusCode()
			Dim body As String = Await response.Content.ReadAsStringAsync()
			Dim fields As Dictionary(Of String, Object) = New JavaScriptSerializer().Deserialize(Of Dictionary(Of String, Object))(body)
			accessToken = fields("access_token").ToString()
		End Using
	End Using
	Return accessToken
End Function
```

**The scope depends on what the integration needs, and no single scope is required.** Relativity's documentation shows two values for this grant: `SystemUserInfo` on the REST authentication page, the Usage Reports page and in the official Transfer SDK sample, and `UserInfoAccess` in the Kepler .NET samples. No page says which is correct, whether both work, or what either grants, and the OAuth2 Client's `Scopes` property is documented as one that cannot be set. Use the value the API you call documents, and confirm it.

**Cache the token, and re-acquire it on expiry — do not refresh.** Keep the token for its lifetime (part 3 recommends 60 minutes, Relativity's own wording) and request a new one with the client secret when it expires or a call is rejected for authentication. Client Credentials does not offer refresh tokens: **verified by NSerio against a live instance; not documented by Relativity** — the Allow Refresh Tokens control does not appear for this grant in the RelativityOne user interface. Relativity's own tables and text on refresh tokens conflict with each other, so re-acquisition with the secret is the advice that holds whichever way they are read.

**Long-running processes that use the Relativity SDK.** A `ServiceFactory` built once from a bearer token keeps that token after it expires. Rebuild the `ServiceFactory` and its proxies with a fresh token before expiry. This is the only advice consistent with the documentation: the package `Relativity.Kepler.Client.SDK` (documented) exposes a settable `Token` and an `HttpMessageHandler` overload but no token-provider overload, and no page documents renewing a token behind a factory.

**The client secret.** It is displayed once, at creation or reset. Store it as Secret Management (Part B of the Migration Documentation) describes; never in source, never in tracked configuration, which in a VB.NET estate includes `app.config`, `web.config` and `My.Settings`. A reset invalidates the previous secret immediately, so plan the cut-over before resetting it.

**Before deploying, confirm the calling identity.** Every call now runs as the OAuth2 Client's **Context User** — not as the user in the replaced credential, and for an import or a job as much as for a request. If that user has different workspace permissions or item-level security, the code returns fewer rows or `Unauthorized` in production with no change to its logic, and Relativity's audit attributes every call to that one account. Confirm the Context User's permissions match those of the previous identity. Relativity describes the Context User as usually a member of the Relativity Administrators group; the OAuth2 Client block (part 3) gives NSerio's recommendation, which differs.

**When the integration acts for a signed-in user.** Client Credentials is one service identity by construction. An integration that acts on behalf of a signed-in user is the Authorization Code case, and moving to it means a new OAuth2 Client, because the flow is fixed when a client is created. The finding's flow note says when to make that choice.

Verification: **Verification note — three things.** **Confirm the library version in use offers a token method.** **A token has a finite lifetime**: applied once to a client or configuration shared across requests, the method holds a token that expires; pass a current token where the request is built. **Confirm the endpoint in use is the RelativityOne REST API.**

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
<!-- generated:end VB-005 client-credentials -->

#### Authorization Code

<!-- generated:begin VB-005 authorization-code -->
**Confirming the flow.** An interactive-user signal in a file assigns Authorization Code, with this note: "An interactive-user signal in this file points to Authorization Code. Confirm the flow: use Client Credentials instead if the integration runs with credentials fixed for the integration and no signed-in user." Your user profile can also make Authorization Code the default; see Changing the default in [Confirming the flow](#confirming-the-flow). See [Confirming the flow](#confirming-the-flow) and the "Which flow" paragraph of VB-005 in [Part A](part-a-vbnet.md#vb-005).

Source form: **Applies to:** both.

```vbnet
Dim authenticatedRequest = request.WithBasicAuth(userName, password)
```

Under token authentication: **First, the destination:** if it is not RelativityOne, this finding is informational — that service's own token mechanism is the migration — and the rest of this template does not apply.

Replace the Basic method with **the same library's bearer or token method, carrying the access token** in place of the credential pair. This template names no target method: the token method's name differs between libraries and versions.

Credentials: **Where the credentials come from.** The destination decides whether the migration applies. If the destination is not RelativityOne, the migration is that service's own token mechanism, this finding is informational for this tool, and the reader stops here. If it is RelativityOne, the token comes from the token-acquisition part that follows.

The text a finding assigned Authorization Code ("Code") receives after part 1. It has a common part and two variants, **server-side** and **desktop**. The bold lead-in of each element is its marker. Every fact Relativity does not document but NSerio verified carries the exact label **verified by NSerio against a live instance; not documented by Relativity**.

**Common to both variants**

**Sign in.** Replace the code that collected the user's credentials with a redirect: send the user's browser to the authorize endpoint `<host>/Relativity/Identity/connect/authorize`, which leads to the RelativityOne login page. The authorize endpoint is **verified by NSerio against a live instance; not documented by Relativity**. The request carries the standard authorization-code parameters — `client_id`, `response_type=code`, the registered `redirect_uri`, a `scope`, and a `state` value the application checks on return; these are standard OAuth, and Relativity documents none of them. No discovery document is relied on.

**Receive the code.** The browser returns to the Redirect URL registered on the OAuth2 Client, with the authorization code and the `state` value. Check the `state` before using the code. The Redirect URL must be exactly the one registered (the variants say how).

**Exchange the code.** Send a form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=authorization_code`, the `code`, the same `redirect_uri`, the `client_id` and the `client_secret`, plus the `code_verifier` when PKCE is used. The code is exchanged at that endpoint, which Relativity documents for the Client Credentials grant only; the exchange is **verified by NSerio against a live instance; not documented by Relativity**.

```vbnet
Dim form As New FormUrlEncodedContent(New Dictionary(Of String, String) From {
	{"grant_type", "authorization_code"},
	{"code", authorizationCode},
	{"redirect_uri", redirectUri},
	{"client_id", clientId},
	{"client_secret", clientSecret},
	{"code_verifier", codeVerifier}})
Using response As HttpResponseMessage = Await client.PostAsync(host & "/Relativity/Identity/connect/token", form)
	response.EnsureSuccessStatusCode()
End Using
```

**Use the token.** Send `Authorization: Bearer <access token>` — the change at the use site in part 1 — with `X-CSRF-Header: -` on REST and Kepler calls.

**On expiry.** With Allow Refresh Tokens enabled on the OAuth2 Client and PKCE used, the application holds a refresh token and, when the access token expires, sends a `grant_type=refresh_token` request with it and the client credentials in place of the code exchange — *inferred* from the standard OAuth shape, not tested. Without a refresh token the user signs in again. Relativity does not document a refresh token's lifetime, whether it rotates, how it is revoked, or whether a revocation endpoint exists, nor what disabling or deleting the client does to tokens already issued. A refresh token is a secret: store it as Secret Management (Part B of the Migration Documentation) describes, and on compromise disable the client and replace it. Enable refresh tokens only if the application must keep a session beyond one token lifetime without asking the user to sign in again.

**PKCE.** PKCE is required by Relativity's own documentation to obtain a refresh token, and not otherwise: a Code request that does not ask for a refresh token works without it, **verified by NSerio against a live instance; not documented by Relativity**. It is never a blanket requirement. NSerio recommends PKCE with the `S256` method in both variants (NSerio recommendation), and does not claim that Relativity accepts any particular method, which it does not document.

**Scope.** No scope set is prescribed for Code. The scope below works — `openid RelativityRequestOrigin UserInfo UserInfoAccess AuthenticationID RelativityWeb` — **verified by NSerio against a live instance; not documented by Relativity**, verified as working and not as minimal. `state` is sent as standard OAuth practice and `nonce` is not used; Relativity does not document whether it enforces either.

**The application stops collecting the password.** After this change the application never receives, stores or forwards the user's username and password: the user signs in on RelativityOne's own page, and the application holds only tokens. Remove the credential prompt, the form field and any storage of the password that the replaced code needed (in a VB.NET estate: the `InputBox`, the login form's password control, the Web Forms login control's handler).

**Single sign-on.** Sign-in happens on RelativityOne's own login page, so the instance's single sign-on and two-factor rules apply unchanged — *inferred* from where the sign-in happens, and not documented for clients that customers create.

**The client secret is required.** Code does not work without a client secret: tested and rejected, **verified by NSerio against a live instance; not documented by Relativity**, which confirms Relativity's own statement that the grant requires one. It is displayed once and is stored as Secret Management (Part B of the Migration Documentation) describes.

**Server-side variant.** For a web application whose code runs on a server — in VB.NET, an ASP.NET Web Forms page or a custom page whose code-behind sends the redirect (`Response.Redirect(authorizeUrl)`) and receives the code in a route of the application:

- **Redirect.** The application receives the code at one exact HTTPS URL per environment, a route of the application, with no wildcards (NSerio recommendation; Relativity's matching rules are not documented). The Relativity administrator registers the exact URL on the OAuth2 Client.
- **Secret and tokens.** The client secret stays on the server and never reaches the browser. The user's tokens are held server-side, keyed to the session, and never in the browser.
- **Exchange.** The server performs the code exchange itself, from the code-behind or a handler.

**Desktop variant.** For a desktop or command-line application run by a user on their own machine (WinForms, WPF, console):

- **The system browser.** Open the authorize URL in the user's default browser and not in a web view the application hosts, so the application never sees the password (NSerio recommendation).
- **A loopback redirect on a fixed port.** Listen on the loopback address only, on the registered port, for one request. The Redirect URL is `http://127.0.0.1:<port>/<path>`: a plain-HTTP loopback URL is accepted, **verified by NSerio against a live instance; not documented by Relativity**. The Relativity administrator registers that exact URL; because it is registered in advance, the port is fixed and not chosen at runtime.

```vbnet
Using listener As New HttpListener()
	listener.Prefixes.Add("http://127.0.0.1:<port>/<path>/")
	listener.Start()
	Process.Start(New ProcessStartInfo(authorizeUrl) With {.UseShellExecute = True})
	Dim context As HttpListenerContext = Await listener.GetContextAsync()
	Dim authorizationCode As String = context.Request.QueryString("code")
	Dim returnedState As String = context.Request.QueryString("state")
	context.Response.Close()
End Using
```

- **The secret is extractable.** Code requires a client secret (above), so a distributed desktop application has to embed one, and **a secret shipped inside a distributed binary can be extracted by anyone who holds the binary**. It protects nothing against the person running the application.
- **Compensating guidance** (NSerio recommendation, subject to your own security policy): set Trusted IPs where the users' outbound addresses are known and stable — they restrict where a token can be *requested*, and are checked when a token is issued against the client's ID and secret; use the recommended short Access Token Lifetime of 60 minutes; enable refresh tokens only when needed; and create one OAuth2 Client per distribution, so a leaked secret can be disabled without affecting the others.
- **A known issue.** Relativity documents no recommendation for a distributed desktop application; this design is what NSerio verified works, and Relativity's own recommendation is tracked as a known issue of the release.

Verification: **Verification note — three things.** **Confirm the library version in use offers a token method.** **A token has a finite lifetime**: applied once to a client or configuration shared across requests, the method holds a token that expires; pass a current token where the request is built. **Confirm the endpoint in use is the RelativityOne REST API.**

OAuth2 Client: Create or confirm this OAuth2 Client in RelativityOne (NSerio's observation: the menu path is Home > Authentication > OAuth2 Client Manager, as Relativity's REST API authentication page names it). This tool never connects to RelativityOne and never creates or changes the OAuth2 Client; the settings below are guidance.
- Name: a descriptive name identifying the application it serves.
- Flow Grant Type: Code
- Redirect URLs: Relativity documentation: each URL includes the http or https protocol prefix. The exact callback URL is registered by the Relativity administrator. Server-side web applications, NSerio's recommendation: one exact HTTPS URL per environment, with no wildcards. Desktop and CLI applications: a plain-HTTP loopback URL, `http://127.0.0.1:<port>/<path>`, on a fixed port because the URL is registered in advance and the port cannot be chosen at runtime (verified by NSerio against a live instance; not documented by Relativity).
- Access Token Lifetime: 60 minutes, stated in minutes because the field is in minutes. Relativity documentation: tokens for this flow must have a short lifetime, and the recommended value matches the Identity Server default.
- Allow refresh tokens: NSerio's recommendation: leave it off unless the application must keep a user session active beyond the access token lifetime without asking the user to sign in again. If it is on, the refresh tokens are stored as secrets, per Secret Management (Part B of the Migration Documentation).
- Enabled: on.
<!-- generated:end VB-005 authorization-code -->

### VB-006 · AuthorizationHeaderValueScheme

<a id="vb-006-migration"></a>

#### Client Credentials

<!-- generated:begin VB-006 client-credentials -->
**Confirming the flow.** When no signal in a file decides the flow, the tool assigns Client Credentials, the default, with this note: "Who supplies the credential at run time? If a person types it or signs in, the integration acts for that person: use Authorization Code. If it comes from configuration, settings, an environment variable or a secret store: use Client Credentials. No signal in this file showed which applies, so Client Credentials, the default, is recommended. Use Authorization Code instead if the integration acts on behalf of a signed-in user, or if your RelativityOne instance already has a Code-flow OAuth2 Client for this integration." A fixed-credential signal assigns it with this note: "A fixed-credential signal in this file points to Client Credentials, but how often that kind of signal is right has not been measured. Use Authorization Code instead if the integration acts on behalf of a signed-in user, or if your RelativityOne instance already has a Code-flow OAuth2 Client for this integration." See [Confirming the flow](#confirming-the-flow) and the "Which flow" paragraph of VB-006 in [Part A](part-a-vbnet.md#vb-006).

Source form: **Applies to:** both (`HttpClient` is in both runtimes).

```vbnet
client.DefaultRequestHeaders.Authorization = New AuthenticationHeaderValue("Basic", encodedCredentials)
```

Under token authentication: **First, the destination:** if it is not RelativityOne, this finding is informational — that service's own token mechanism is the migration — and the rest of this template does not apply.

`"Bearer"` and the access token in place of `"Basic"` and the encoded pair, **set per request** rather than on the long-lived client's default headers:

```vbnet
request.Headers.Authorization = New AuthenticationHeaderValue("Bearer", accessToken)
```

A per-request header needs an `HttpRequestMessage` sent with `SendAsync`; the `GetAsync` and `PostAsync` convenience calls take no request message, so calls written with them change shape. The code that produced `encodedCredentials` has no counterpart: a bearer token travels verbatim.

Credentials: **Where the credentials come from.** The destination decides whether the migration applies. If the destination is not RelativityOne, the migration is that service's own token mechanism, this finding is informational for this tool, and the reader stops here. If it is RelativityOne, the token comes from the token-acquisition part that follows.

**Where the token comes from.** Create an OAuth2 Client with Flow Grant Type set to Client Credentials (part 3 lists its settings), then send a form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=client_credentials`, `client_id`, `client_secret` and a `scope`. The token endpoint is **documented** for this grant. The token is then sent as `Authorization: Bearer <token>`, with `X-CSRF-Header: -` on REST and Kepler calls.

**.NET (both runtimes' `HttpClient`; `System.Text.Json` on .NET)**

```vbnet
Imports System.Net.Http
Imports System.Text.Json

Public Async Function AcquireAccessTokenAsync(host As String, clientId As String, clientSecret As String, scope As String) As Task(Of String)
	Dim accessToken As String
	Using client As New HttpClient()
		Dim form As New FormUrlEncodedContent(New Dictionary(Of String, String) From {
			{"grant_type", "client_credentials"},
			{"client_id", clientId},
			{"client_secret", clientSecret},
			{"scope", scope}})
		Using response As HttpResponseMessage = Await client.PostAsync(host & "/Relativity/Identity/connect/token", form)
			response.EnsureSuccessStatusCode()
			Using document As JsonDocument = JsonDocument.Parse(Await response.Content.ReadAsStringAsync())
				accessToken = document.RootElement.GetProperty("access_token").GetString()
			End Using
		End Using
	End Using
	Return accessToken
End Function
```

**.NET Framework 4.7.2** — the same call with the serializer the runtime ships (`System.Web.Extensions`); `HttpClient` is in `System.Net.Http`:

```vbnet
Imports System.Net.Http
Imports System.Web.Script.Serialization

Public Async Function AcquireAccessTokenAsync(host As String, clientId As String, clientSecret As String, scope As String) As Task(Of String)
	Dim accessToken As String
	Using client As New HttpClient()
		Dim form As New FormUrlEncodedContent(New Dictionary(Of String, String) From {
			{"grant_type", "client_credentials"},
			{"client_id", clientId},
			{"client_secret", clientSecret},
			{"scope", scope}})
		Using response As HttpResponseMessage = Await client.PostAsync(host & "/Relativity/Identity/connect/token", form)
			response.EnsureSuccessStatusCode()
			Dim body As String = Await response.Content.ReadAsStringAsync()
			Dim fields As Dictionary(Of String, Object) = New JavaScriptSerializer().Deserialize(Of Dictionary(Of String, Object))(body)
			accessToken = fields("access_token").ToString()
		End Using
	End Using
	Return accessToken
End Function
```

**The scope depends on what the integration needs, and no single scope is required.** Relativity's documentation shows two values for this grant: `SystemUserInfo` on the REST authentication page, the Usage Reports page and in the official Transfer SDK sample, and `UserInfoAccess` in the Kepler .NET samples. No page says which is correct, whether both work, or what either grants, and the OAuth2 Client's `Scopes` property is documented as one that cannot be set. Use the value the API you call documents, and confirm it.

**Cache the token, and re-acquire it on expiry — do not refresh.** Keep the token for its lifetime (part 3 recommends 60 minutes, Relativity's own wording) and request a new one with the client secret when it expires or a call is rejected for authentication. Client Credentials does not offer refresh tokens: **verified by NSerio against a live instance; not documented by Relativity** — the Allow Refresh Tokens control does not appear for this grant in the RelativityOne user interface. Relativity's own tables and text on refresh tokens conflict with each other, so re-acquisition with the secret is the advice that holds whichever way they are read.

**Long-running processes that use the Relativity SDK.** A `ServiceFactory` built once from a bearer token keeps that token after it expires. Rebuild the `ServiceFactory` and its proxies with a fresh token before expiry. This is the only advice consistent with the documentation: the package `Relativity.Kepler.Client.SDK` (documented) exposes a settable `Token` and an `HttpMessageHandler` overload but no token-provider overload, and no page documents renewing a token behind a factory.

**The client secret.** It is displayed once, at creation or reset. Store it as Secret Management (Part B of the Migration Documentation) describes; never in source, never in tracked configuration, which in a VB.NET estate includes `app.config`, `web.config` and `My.Settings`. A reset invalidates the previous secret immediately, so plan the cut-over before resetting it.

**Before deploying, confirm the calling identity.** Every call now runs as the OAuth2 Client's **Context User** — not as the user in the replaced credential, and for an import or a job as much as for a request. If that user has different workspace permissions or item-level security, the code returns fewer rows or `Unauthorized` in production with no change to its logic, and Relativity's audit attributes every call to that one account. Confirm the Context User's permissions match those of the previous identity. Relativity describes the Context User as usually a member of the Relativity Administrators group; the OAuth2 Client block (part 3) gives NSerio's recommendation, which differs.

**When the integration acts for a signed-in user.** Client Credentials is one service identity by construction. An integration that acts on behalf of a signed-in user is the Authorization Code case, and moving to it means a new OAuth2 Client, because the flow is fixed when a client is created. The finding's flow note says when to make that choice.

Verification: **Verification note — three things.** **Moving the header from the client to the request is part of the migration, not a style choice**: a token has a finite lifetime, and a header added once to `DefaultRequestHeaders` cannot be refreshed without mutating shared state. **The pair's encoding may be a separate finding**: where `encodedCredentials` is built in the same file, the tool reports the encoding and this header as two findings at two locations; they are one migration. **Confirm the endpoint in use is the RelativityOne REST API.**

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
<!-- generated:end VB-006 client-credentials -->

#### Authorization Code

<!-- generated:begin VB-006 authorization-code -->
**Confirming the flow.** An interactive-user signal in a file assigns Authorization Code, with this note: "An interactive-user signal in this file points to Authorization Code. Confirm the flow: use Client Credentials instead if the integration runs with credentials fixed for the integration and no signed-in user." Your user profile can also make Authorization Code the default; see Changing the default in [Confirming the flow](#confirming-the-flow). See [Confirming the flow](#confirming-the-flow) and the "Which flow" paragraph of VB-006 in [Part A](part-a-vbnet.md#vb-006).

Source form: **Applies to:** both (`HttpClient` is in both runtimes).

```vbnet
client.DefaultRequestHeaders.Authorization = New AuthenticationHeaderValue("Basic", encodedCredentials)
```

Under token authentication: **First, the destination:** if it is not RelativityOne, this finding is informational — that service's own token mechanism is the migration — and the rest of this template does not apply.

`"Bearer"` and the access token in place of `"Basic"` and the encoded pair, **set per request** rather than on the long-lived client's default headers:

```vbnet
request.Headers.Authorization = New AuthenticationHeaderValue("Bearer", accessToken)
```

A per-request header needs an `HttpRequestMessage` sent with `SendAsync`; the `GetAsync` and `PostAsync` convenience calls take no request message, so calls written with them change shape. The code that produced `encodedCredentials` has no counterpart: a bearer token travels verbatim.

Credentials: **Where the credentials come from.** The destination decides whether the migration applies. If the destination is not RelativityOne, the migration is that service's own token mechanism, this finding is informational for this tool, and the reader stops here. If it is RelativityOne, the token comes from the token-acquisition part that follows.

The text a finding assigned Authorization Code ("Code") receives after part 1. It has a common part and two variants, **server-side** and **desktop**. The bold lead-in of each element is its marker. Every fact Relativity does not document but NSerio verified carries the exact label **verified by NSerio against a live instance; not documented by Relativity**.

**Common to both variants**

**Sign in.** Replace the code that collected the user's credentials with a redirect: send the user's browser to the authorize endpoint `<host>/Relativity/Identity/connect/authorize`, which leads to the RelativityOne login page. The authorize endpoint is **verified by NSerio against a live instance; not documented by Relativity**. The request carries the standard authorization-code parameters — `client_id`, `response_type=code`, the registered `redirect_uri`, a `scope`, and a `state` value the application checks on return; these are standard OAuth, and Relativity documents none of them. No discovery document is relied on.

**Receive the code.** The browser returns to the Redirect URL registered on the OAuth2 Client, with the authorization code and the `state` value. Check the `state` before using the code. The Redirect URL must be exactly the one registered (the variants say how).

**Exchange the code.** Send a form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=authorization_code`, the `code`, the same `redirect_uri`, the `client_id` and the `client_secret`, plus the `code_verifier` when PKCE is used. The code is exchanged at that endpoint, which Relativity documents for the Client Credentials grant only; the exchange is **verified by NSerio against a live instance; not documented by Relativity**.

```vbnet
Dim form As New FormUrlEncodedContent(New Dictionary(Of String, String) From {
	{"grant_type", "authorization_code"},
	{"code", authorizationCode},
	{"redirect_uri", redirectUri},
	{"client_id", clientId},
	{"client_secret", clientSecret},
	{"code_verifier", codeVerifier}})
Using response As HttpResponseMessage = Await client.PostAsync(host & "/Relativity/Identity/connect/token", form)
	response.EnsureSuccessStatusCode()
End Using
```

**Use the token.** Send `Authorization: Bearer <access token>` — the change at the use site in part 1 — with `X-CSRF-Header: -` on REST and Kepler calls.

**On expiry.** With Allow Refresh Tokens enabled on the OAuth2 Client and PKCE used, the application holds a refresh token and, when the access token expires, sends a `grant_type=refresh_token` request with it and the client credentials in place of the code exchange — *inferred* from the standard OAuth shape, not tested. Without a refresh token the user signs in again. Relativity does not document a refresh token's lifetime, whether it rotates, how it is revoked, or whether a revocation endpoint exists, nor what disabling or deleting the client does to tokens already issued. A refresh token is a secret: store it as Secret Management (Part B of the Migration Documentation) describes, and on compromise disable the client and replace it. Enable refresh tokens only if the application must keep a session beyond one token lifetime without asking the user to sign in again.

**PKCE.** PKCE is required by Relativity's own documentation to obtain a refresh token, and not otherwise: a Code request that does not ask for a refresh token works without it, **verified by NSerio against a live instance; not documented by Relativity**. It is never a blanket requirement. NSerio recommends PKCE with the `S256` method in both variants (NSerio recommendation), and does not claim that Relativity accepts any particular method, which it does not document.

**Scope.** No scope set is prescribed for Code. The scope below works — `openid RelativityRequestOrigin UserInfo UserInfoAccess AuthenticationID RelativityWeb` — **verified by NSerio against a live instance; not documented by Relativity**, verified as working and not as minimal. `state` is sent as standard OAuth practice and `nonce` is not used; Relativity does not document whether it enforces either.

**The application stops collecting the password.** After this change the application never receives, stores or forwards the user's username and password: the user signs in on RelativityOne's own page, and the application holds only tokens. Remove the credential prompt, the form field and any storage of the password that the replaced code needed (in a VB.NET estate: the `InputBox`, the login form's password control, the Web Forms login control's handler).

**Single sign-on.** Sign-in happens on RelativityOne's own login page, so the instance's single sign-on and two-factor rules apply unchanged — *inferred* from where the sign-in happens, and not documented for clients that customers create.

**The client secret is required.** Code does not work without a client secret: tested and rejected, **verified by NSerio against a live instance; not documented by Relativity**, which confirms Relativity's own statement that the grant requires one. It is displayed once and is stored as Secret Management (Part B of the Migration Documentation) describes.

**Server-side variant.** For a web application whose code runs on a server — in VB.NET, an ASP.NET Web Forms page or a custom page whose code-behind sends the redirect (`Response.Redirect(authorizeUrl)`) and receives the code in a route of the application:

- **Redirect.** The application receives the code at one exact HTTPS URL per environment, a route of the application, with no wildcards (NSerio recommendation; Relativity's matching rules are not documented). The Relativity administrator registers the exact URL on the OAuth2 Client.
- **Secret and tokens.** The client secret stays on the server and never reaches the browser. The user's tokens are held server-side, keyed to the session, and never in the browser.
- **Exchange.** The server performs the code exchange itself, from the code-behind or a handler.

**Desktop variant.** For a desktop or command-line application run by a user on their own machine (WinForms, WPF, console):

- **The system browser.** Open the authorize URL in the user's default browser and not in a web view the application hosts, so the application never sees the password (NSerio recommendation).
- **A loopback redirect on a fixed port.** Listen on the loopback address only, on the registered port, for one request. The Redirect URL is `http://127.0.0.1:<port>/<path>`: a plain-HTTP loopback URL is accepted, **verified by NSerio against a live instance; not documented by Relativity**. The Relativity administrator registers that exact URL; because it is registered in advance, the port is fixed and not chosen at runtime.

```vbnet
Using listener As New HttpListener()
	listener.Prefixes.Add("http://127.0.0.1:<port>/<path>/")
	listener.Start()
	Process.Start(New ProcessStartInfo(authorizeUrl) With {.UseShellExecute = True})
	Dim context As HttpListenerContext = Await listener.GetContextAsync()
	Dim authorizationCode As String = context.Request.QueryString("code")
	Dim returnedState As String = context.Request.QueryString("state")
	context.Response.Close()
End Using
```

- **The secret is extractable.** Code requires a client secret (above), so a distributed desktop application has to embed one, and **a secret shipped inside a distributed binary can be extracted by anyone who holds the binary**. It protects nothing against the person running the application.
- **Compensating guidance** (NSerio recommendation, subject to your own security policy): set Trusted IPs where the users' outbound addresses are known and stable — they restrict where a token can be *requested*, and are checked when a token is issued against the client's ID and secret; use the recommended short Access Token Lifetime of 60 minutes; enable refresh tokens only when needed; and create one OAuth2 Client per distribution, so a leaked secret can be disabled without affecting the others.
- **A known issue.** Relativity documents no recommendation for a distributed desktop application; this design is what NSerio verified works, and Relativity's own recommendation is tracked as a known issue of the release.

Verification: **Verification note — three things.** **Moving the header from the client to the request is part of the migration, not a style choice**: a token has a finite lifetime, and a header added once to `DefaultRequestHeaders` cannot be refreshed without mutating shared state. **The pair's encoding may be a separate finding**: where `encodedCredentials` is built in the same file, the tool reports the encoding and this header as two findings at two locations; they are one migration. **Confirm the endpoint in use is the RelativityOne REST API.**

OAuth2 Client: Create or confirm this OAuth2 Client in RelativityOne (NSerio's observation: the menu path is Home > Authentication > OAuth2 Client Manager, as Relativity's REST API authentication page names it). This tool never connects to RelativityOne and never creates or changes the OAuth2 Client; the settings below are guidance.
- Name: a descriptive name identifying the application it serves.
- Flow Grant Type: Code
- Redirect URLs: Relativity documentation: each URL includes the http or https protocol prefix. The exact callback URL is registered by the Relativity administrator. Server-side web applications, NSerio's recommendation: one exact HTTPS URL per environment, with no wildcards. Desktop and CLI applications: a plain-HTTP loopback URL, `http://127.0.0.1:<port>/<path>`, on a fixed port because the URL is registered in advance and the port cannot be chosen at runtime (verified by NSerio against a live instance; not documented by Relativity).
- Access Token Lifetime: 60 minutes, stated in minutes because the field is in minutes. Relativity documentation: tokens for this flow must have a short lifetime, and the recommended value matches the Identity Server default.
- Allow refresh tokens: NSerio's recommendation: leave it off unless the application must keep a user session active beyond the access token lifetime without asking the user to sign in again. If it is on, the refresh tokens are stored as secrets, per Secret Management (Part B of the Migration Documentation).
- Enabled: on.
<!-- generated:end VB-006 authorization-code -->

### VB-007 · AuthorizationHeaderWrite

<a id="vb-007-migration"></a>

#### Client Credentials

<!-- generated:begin VB-007 client-credentials -->
**Confirming the flow.** When no signal in a file decides the flow, the tool assigns Client Credentials, the default, with this note: "Who supplies the credential at run time? If a person types it or signs in, the integration acts for that person: use Authorization Code. If it comes from configuration, settings, an environment variable or a secret store: use Client Credentials. No signal in this file showed which applies, so Client Credentials, the default, is recommended. Use Authorization Code instead if the integration acts on behalf of a signed-in user, or if your RelativityOne instance already has a Code-flow OAuth2 Client for this integration." A fixed-credential signal assigns it with this note: "A fixed-credential signal in this file points to Client Credentials, but how often that kind of signal is right has not been measured. Use Authorization Code instead if the integration acts on behalf of a signed-in user, or if your RelativityOne instance already has a Code-flow OAuth2 Client for this integration." See [Confirming the flow](#confirming-the-flow) and the "Which flow" paragraph of VB-007 in [Part A](part-a-vbnet.md#vb-007).

Source form: **Applies to:** both.

```vbnet
client.Headers.Add("Authorization", "Basic " & encodedCredentials)
```

Under token authentication: **First, the destination:** if it is not RelativityOne, this finding is informational — that service's own token mechanism is the migration — and the rest of this template does not apply.

`Bearer` and the access token in place of `Basic` and the encoded pair, **set per request**:

```vbnet
request.Headers.Add("Authorization", "Bearer " & accessToken)
```

Where the header is written on a `WebClient` (**.NET Framework 4.7.2** period form; obsolete on .NET in favour of `HttpClient`), set it before each call; on an `HttpClient`, on the `HttpRequestMessage`. The code that produced `encodedCredentials` has no counterpart: a bearer token travels verbatim.

Credentials: **Where the credentials come from.** The destination decides whether the migration applies. If the destination is not RelativityOne, the migration is that service's own token mechanism, this finding is informational for this tool, and the reader stops here. If it is RelativityOne, the token comes from the token-acquisition part that follows.

**Where the token comes from.** Create an OAuth2 Client with Flow Grant Type set to Client Credentials (part 3 lists its settings), then send a form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=client_credentials`, `client_id`, `client_secret` and a `scope`. The token endpoint is **documented** for this grant. The token is then sent as `Authorization: Bearer <token>`, with `X-CSRF-Header: -` on REST and Kepler calls.

**.NET (both runtimes' `HttpClient`; `System.Text.Json` on .NET)**

```vbnet
Imports System.Net.Http
Imports System.Text.Json

Public Async Function AcquireAccessTokenAsync(host As String, clientId As String, clientSecret As String, scope As String) As Task(Of String)
	Dim accessToken As String
	Using client As New HttpClient()
		Dim form As New FormUrlEncodedContent(New Dictionary(Of String, String) From {
			{"grant_type", "client_credentials"},
			{"client_id", clientId},
			{"client_secret", clientSecret},
			{"scope", scope}})
		Using response As HttpResponseMessage = Await client.PostAsync(host & "/Relativity/Identity/connect/token", form)
			response.EnsureSuccessStatusCode()
			Using document As JsonDocument = JsonDocument.Parse(Await response.Content.ReadAsStringAsync())
				accessToken = document.RootElement.GetProperty("access_token").GetString()
			End Using
		End Using
	End Using
	Return accessToken
End Function
```

**.NET Framework 4.7.2** — the same call with the serializer the runtime ships (`System.Web.Extensions`); `HttpClient` is in `System.Net.Http`:

```vbnet
Imports System.Net.Http
Imports System.Web.Script.Serialization

Public Async Function AcquireAccessTokenAsync(host As String, clientId As String, clientSecret As String, scope As String) As Task(Of String)
	Dim accessToken As String
	Using client As New HttpClient()
		Dim form As New FormUrlEncodedContent(New Dictionary(Of String, String) From {
			{"grant_type", "client_credentials"},
			{"client_id", clientId},
			{"client_secret", clientSecret},
			{"scope", scope}})
		Using response As HttpResponseMessage = Await client.PostAsync(host & "/Relativity/Identity/connect/token", form)
			response.EnsureSuccessStatusCode()
			Dim body As String = Await response.Content.ReadAsStringAsync()
			Dim fields As Dictionary(Of String, Object) = New JavaScriptSerializer().Deserialize(Of Dictionary(Of String, Object))(body)
			accessToken = fields("access_token").ToString()
		End Using
	End Using
	Return accessToken
End Function
```

**The scope depends on what the integration needs, and no single scope is required.** Relativity's documentation shows two values for this grant: `SystemUserInfo` on the REST authentication page, the Usage Reports page and in the official Transfer SDK sample, and `UserInfoAccess` in the Kepler .NET samples. No page says which is correct, whether both work, or what either grants, and the OAuth2 Client's `Scopes` property is documented as one that cannot be set. Use the value the API you call documents, and confirm it.

**Cache the token, and re-acquire it on expiry — do not refresh.** Keep the token for its lifetime (part 3 recommends 60 minutes, Relativity's own wording) and request a new one with the client secret when it expires or a call is rejected for authentication. Client Credentials does not offer refresh tokens: **verified by NSerio against a live instance; not documented by Relativity** — the Allow Refresh Tokens control does not appear for this grant in the RelativityOne user interface. Relativity's own tables and text on refresh tokens conflict with each other, so re-acquisition with the secret is the advice that holds whichever way they are read.

**Long-running processes that use the Relativity SDK.** A `ServiceFactory` built once from a bearer token keeps that token after it expires. Rebuild the `ServiceFactory` and its proxies with a fresh token before expiry. This is the only advice consistent with the documentation: the package `Relativity.Kepler.Client.SDK` (documented) exposes a settable `Token` and an `HttpMessageHandler` overload but no token-provider overload, and no page documents renewing a token behind a factory.

**The client secret.** It is displayed once, at creation or reset. Store it as Secret Management (Part B of the Migration Documentation) describes; never in source, never in tracked configuration, which in a VB.NET estate includes `app.config`, `web.config` and `My.Settings`. A reset invalidates the previous secret immediately, so plan the cut-over before resetting it.

**Before deploying, confirm the calling identity.** Every call now runs as the OAuth2 Client's **Context User** — not as the user in the replaced credential, and for an import or a job as much as for a request. If that user has different workspace permissions or item-level security, the code returns fewer rows or `Unauthorized` in production with no change to its logic, and Relativity's audit attributes every call to that one account. Confirm the Context User's permissions match those of the previous identity. Relativity describes the Context User as usually a member of the Relativity Administrators group; the OAuth2 Client block (part 3) gives NSerio's recommendation, which differs.

**When the integration acts for a signed-in user.** Client Credentials is one service identity by construction. An integration that acts on behalf of a signed-in user is the Authorization Code case, and moving to it means a new OAuth2 Client, because the flow is fixed when a client is created. The finding's flow note says when to make that choice.

Verification: **Verification note — three things.** **A header set once on a long-lived client cannot be refreshed**: set it per request, or the code works for the token's lifetime and then returns 401 until the process restarts. **The pair's encoding may be a separate finding**, at a second location, in the same migration. **Confirm the endpoint in use is the RelativityOne REST API.**

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
<!-- generated:end VB-007 client-credentials -->

#### Authorization Code

<!-- generated:begin VB-007 authorization-code -->
**Confirming the flow.** An interactive-user signal in a file assigns Authorization Code, with this note: "An interactive-user signal in this file points to Authorization Code. Confirm the flow: use Client Credentials instead if the integration runs with credentials fixed for the integration and no signed-in user." Your user profile can also make Authorization Code the default; see Changing the default in [Confirming the flow](#confirming-the-flow). See [Confirming the flow](#confirming-the-flow) and the "Which flow" paragraph of VB-007 in [Part A](part-a-vbnet.md#vb-007).

Source form: **Applies to:** both.

```vbnet
client.Headers.Add("Authorization", "Basic " & encodedCredentials)
```

Under token authentication: **First, the destination:** if it is not RelativityOne, this finding is informational — that service's own token mechanism is the migration — and the rest of this template does not apply.

`Bearer` and the access token in place of `Basic` and the encoded pair, **set per request**:

```vbnet
request.Headers.Add("Authorization", "Bearer " & accessToken)
```

Where the header is written on a `WebClient` (**.NET Framework 4.7.2** period form; obsolete on .NET in favour of `HttpClient`), set it before each call; on an `HttpClient`, on the `HttpRequestMessage`. The code that produced `encodedCredentials` has no counterpart: a bearer token travels verbatim.

Credentials: **Where the credentials come from.** The destination decides whether the migration applies. If the destination is not RelativityOne, the migration is that service's own token mechanism, this finding is informational for this tool, and the reader stops here. If it is RelativityOne, the token comes from the token-acquisition part that follows.

The text a finding assigned Authorization Code ("Code") receives after part 1. It has a common part and two variants, **server-side** and **desktop**. The bold lead-in of each element is its marker. Every fact Relativity does not document but NSerio verified carries the exact label **verified by NSerio against a live instance; not documented by Relativity**.

**Common to both variants**

**Sign in.** Replace the code that collected the user's credentials with a redirect: send the user's browser to the authorize endpoint `<host>/Relativity/Identity/connect/authorize`, which leads to the RelativityOne login page. The authorize endpoint is **verified by NSerio against a live instance; not documented by Relativity**. The request carries the standard authorization-code parameters — `client_id`, `response_type=code`, the registered `redirect_uri`, a `scope`, and a `state` value the application checks on return; these are standard OAuth, and Relativity documents none of them. No discovery document is relied on.

**Receive the code.** The browser returns to the Redirect URL registered on the OAuth2 Client, with the authorization code and the `state` value. Check the `state` before using the code. The Redirect URL must be exactly the one registered (the variants say how).

**Exchange the code.** Send a form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=authorization_code`, the `code`, the same `redirect_uri`, the `client_id` and the `client_secret`, plus the `code_verifier` when PKCE is used. The code is exchanged at that endpoint, which Relativity documents for the Client Credentials grant only; the exchange is **verified by NSerio against a live instance; not documented by Relativity**.

```vbnet
Dim form As New FormUrlEncodedContent(New Dictionary(Of String, String) From {
	{"grant_type", "authorization_code"},
	{"code", authorizationCode},
	{"redirect_uri", redirectUri},
	{"client_id", clientId},
	{"client_secret", clientSecret},
	{"code_verifier", codeVerifier}})
Using response As HttpResponseMessage = Await client.PostAsync(host & "/Relativity/Identity/connect/token", form)
	response.EnsureSuccessStatusCode()
End Using
```

**Use the token.** Send `Authorization: Bearer <access token>` — the change at the use site in part 1 — with `X-CSRF-Header: -` on REST and Kepler calls.

**On expiry.** With Allow Refresh Tokens enabled on the OAuth2 Client and PKCE used, the application holds a refresh token and, when the access token expires, sends a `grant_type=refresh_token` request with it and the client credentials in place of the code exchange — *inferred* from the standard OAuth shape, not tested. Without a refresh token the user signs in again. Relativity does not document a refresh token's lifetime, whether it rotates, how it is revoked, or whether a revocation endpoint exists, nor what disabling or deleting the client does to tokens already issued. A refresh token is a secret: store it as Secret Management (Part B of the Migration Documentation) describes, and on compromise disable the client and replace it. Enable refresh tokens only if the application must keep a session beyond one token lifetime without asking the user to sign in again.

**PKCE.** PKCE is required by Relativity's own documentation to obtain a refresh token, and not otherwise: a Code request that does not ask for a refresh token works without it, **verified by NSerio against a live instance; not documented by Relativity**. It is never a blanket requirement. NSerio recommends PKCE with the `S256` method in both variants (NSerio recommendation), and does not claim that Relativity accepts any particular method, which it does not document.

**Scope.** No scope set is prescribed for Code. The scope below works — `openid RelativityRequestOrigin UserInfo UserInfoAccess AuthenticationID RelativityWeb` — **verified by NSerio against a live instance; not documented by Relativity**, verified as working and not as minimal. `state` is sent as standard OAuth practice and `nonce` is not used; Relativity does not document whether it enforces either.

**The application stops collecting the password.** After this change the application never receives, stores or forwards the user's username and password: the user signs in on RelativityOne's own page, and the application holds only tokens. Remove the credential prompt, the form field and any storage of the password that the replaced code needed (in a VB.NET estate: the `InputBox`, the login form's password control, the Web Forms login control's handler).

**Single sign-on.** Sign-in happens on RelativityOne's own login page, so the instance's single sign-on and two-factor rules apply unchanged — *inferred* from where the sign-in happens, and not documented for clients that customers create.

**The client secret is required.** Code does not work without a client secret: tested and rejected, **verified by NSerio against a live instance; not documented by Relativity**, which confirms Relativity's own statement that the grant requires one. It is displayed once and is stored as Secret Management (Part B of the Migration Documentation) describes.

**Server-side variant.** For a web application whose code runs on a server — in VB.NET, an ASP.NET Web Forms page or a custom page whose code-behind sends the redirect (`Response.Redirect(authorizeUrl)`) and receives the code in a route of the application:

- **Redirect.** The application receives the code at one exact HTTPS URL per environment, a route of the application, with no wildcards (NSerio recommendation; Relativity's matching rules are not documented). The Relativity administrator registers the exact URL on the OAuth2 Client.
- **Secret and tokens.** The client secret stays on the server and never reaches the browser. The user's tokens are held server-side, keyed to the session, and never in the browser.
- **Exchange.** The server performs the code exchange itself, from the code-behind or a handler.

**Desktop variant.** For a desktop or command-line application run by a user on their own machine (WinForms, WPF, console):

- **The system browser.** Open the authorize URL in the user's default browser and not in a web view the application hosts, so the application never sees the password (NSerio recommendation).
- **A loopback redirect on a fixed port.** Listen on the loopback address only, on the registered port, for one request. The Redirect URL is `http://127.0.0.1:<port>/<path>`: a plain-HTTP loopback URL is accepted, **verified by NSerio against a live instance; not documented by Relativity**. The Relativity administrator registers that exact URL; because it is registered in advance, the port is fixed and not chosen at runtime.

```vbnet
Using listener As New HttpListener()
	listener.Prefixes.Add("http://127.0.0.1:<port>/<path>/")
	listener.Start()
	Process.Start(New ProcessStartInfo(authorizeUrl) With {.UseShellExecute = True})
	Dim context As HttpListenerContext = Await listener.GetContextAsync()
	Dim authorizationCode As String = context.Request.QueryString("code")
	Dim returnedState As String = context.Request.QueryString("state")
	context.Response.Close()
End Using
```

- **The secret is extractable.** Code requires a client secret (above), so a distributed desktop application has to embed one, and **a secret shipped inside a distributed binary can be extracted by anyone who holds the binary**. It protects nothing against the person running the application.
- **Compensating guidance** (NSerio recommendation, subject to your own security policy): set Trusted IPs where the users' outbound addresses are known and stable — they restrict where a token can be *requested*, and are checked when a token is issued against the client's ID and secret; use the recommended short Access Token Lifetime of 60 minutes; enable refresh tokens only when needed; and create one OAuth2 Client per distribution, so a leaked secret can be disabled without affecting the others.
- **A known issue.** Relativity documents no recommendation for a distributed desktop application; this design is what NSerio verified works, and Relativity's own recommendation is tracked as a known issue of the release.

Verification: **Verification note — three things.** **A header set once on a long-lived client cannot be refreshed**: set it per request, or the code works for the token's lifetime and then returns 401 until the process restarts. **The pair's encoding may be a separate finding**, at a second location, in the same migration. **Confirm the endpoint in use is the RelativityOne REST API.**

OAuth2 Client: Create or confirm this OAuth2 Client in RelativityOne (NSerio's observation: the menu path is Home > Authentication > OAuth2 Client Manager, as Relativity's REST API authentication page names it). This tool never connects to RelativityOne and never creates or changes the OAuth2 Client; the settings below are guidance.
- Name: a descriptive name identifying the application it serves.
- Flow Grant Type: Code
- Redirect URLs: Relativity documentation: each URL includes the http or https protocol prefix. The exact callback URL is registered by the Relativity administrator. Server-side web applications, NSerio's recommendation: one exact HTTPS URL per environment, with no wildcards. Desktop and CLI applications: a plain-HTTP loopback URL, `http://127.0.0.1:<port>/<path>`, on a fixed port because the URL is registered in advance and the port cannot be chosen at runtime (verified by NSerio against a live instance; not documented by Relativity).
- Access Token Lifetime: 60 minutes, stated in minutes because the field is in minutes. Relativity documentation: tokens for this flow must have a short lifetime, and the recommended value matches the Identity Server default.
- Allow refresh tokens: NSerio's recommendation: leave it off unless the application must keep a user session active beyond the access token lifetime without asking the user to sign in again. If it is on, the refresh tokens are stored as secrets, per Secret Management (Part B of the Migration Documentation).
- Enabled: on.
<!-- generated:end VB-007 authorization-code -->

### VB-008 · HttpRequestHeaderEnumAuthorization

<a id="vb-008-migration"></a>

#### Client Credentials

<!-- generated:begin VB-008 client-credentials -->
**Confirming the flow.** When no signal in a file decides the flow, the tool assigns Client Credentials, the default, with this note: "Who supplies the credential at run time? If a person types it or signs in, the integration acts for that person: use Authorization Code. If it comes from configuration, settings, an environment variable or a secret store: use Client Credentials. No signal in this file showed which applies, so Client Credentials, the default, is recommended. Use Authorization Code instead if the integration acts on behalf of a signed-in user, or if your RelativityOne instance already has a Code-flow OAuth2 Client for this integration." A fixed-credential signal assigns it with this note: "A fixed-credential signal in this file points to Client Credentials, but how often that kind of signal is right has not been measured. Use Authorization Code instead if the integration acts on behalf of a signed-in user, or if your RelativityOne instance already has a Code-flow OAuth2 Client for this integration." See [Confirming the flow](#confirming-the-flow) and the "Which flow" paragraph of VB-008 in [Part A](part-a-vbnet.md#vb-008).

Source form: **Applies to:** .NET Framework 4.7.2 period form (`WebClient`, `HttpWebRequest`); the same lines compile on .NET, where both types are obsolete in favour of `HttpClient`.

```vbnet
client.Headers(HttpRequestHeader.Authorization) = "Basic " & encoded
request.Headers(HttpRequestHeader.Authorization) = "Basic " & encoded
```

Under token authentication: **First, the destination:** if it is not RelativityOne, this finding is informational — that service's own token mechanism is the migration — and the rest of this template does not apply.

`"Bearer "` and the access token in place of `"Basic "` and the encoded pair, **set per request**:

```vbnet
client.Headers(HttpRequestHeader.Authorization) = "Bearer " & accessToken
request.Headers(HttpRequestHeader.Authorization) = "Bearer " & accessToken
```

The code that produced `encoded` has no counterpart: a bearer token travels verbatim.

Credentials: **Where the credentials come from.** The destination decides whether the migration applies. If the destination is not RelativityOne, the migration is that service's own token mechanism, this finding is informational for this tool, and the reader stops here. If it is RelativityOne, the token comes from the token-acquisition part that follows.

**Where the token comes from.** Create an OAuth2 Client with Flow Grant Type set to Client Credentials (part 3 lists its settings), then send a form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=client_credentials`, `client_id`, `client_secret` and a `scope`. The token endpoint is **documented** for this grant. The token is then sent as `Authorization: Bearer <token>`, with `X-CSRF-Header: -` on REST and Kepler calls.

**.NET (both runtimes' `HttpClient`; `System.Text.Json` on .NET)**

```vbnet
Imports System.Net.Http
Imports System.Text.Json

Public Async Function AcquireAccessTokenAsync(host As String, clientId As String, clientSecret As String, scope As String) As Task(Of String)
	Dim accessToken As String
	Using client As New HttpClient()
		Dim form As New FormUrlEncodedContent(New Dictionary(Of String, String) From {
			{"grant_type", "client_credentials"},
			{"client_id", clientId},
			{"client_secret", clientSecret},
			{"scope", scope}})
		Using response As HttpResponseMessage = Await client.PostAsync(host & "/Relativity/Identity/connect/token", form)
			response.EnsureSuccessStatusCode()
			Using document As JsonDocument = JsonDocument.Parse(Await response.Content.ReadAsStringAsync())
				accessToken = document.RootElement.GetProperty("access_token").GetString()
			End Using
		End Using
	End Using
	Return accessToken
End Function
```

**.NET Framework 4.7.2** — the same call with the serializer the runtime ships (`System.Web.Extensions`); `HttpClient` is in `System.Net.Http`:

```vbnet
Imports System.Net.Http
Imports System.Web.Script.Serialization

Public Async Function AcquireAccessTokenAsync(host As String, clientId As String, clientSecret As String, scope As String) As Task(Of String)
	Dim accessToken As String
	Using client As New HttpClient()
		Dim form As New FormUrlEncodedContent(New Dictionary(Of String, String) From {
			{"grant_type", "client_credentials"},
			{"client_id", clientId},
			{"client_secret", clientSecret},
			{"scope", scope}})
		Using response As HttpResponseMessage = Await client.PostAsync(host & "/Relativity/Identity/connect/token", form)
			response.EnsureSuccessStatusCode()
			Dim body As String = Await response.Content.ReadAsStringAsync()
			Dim fields As Dictionary(Of String, Object) = New JavaScriptSerializer().Deserialize(Of Dictionary(Of String, Object))(body)
			accessToken = fields("access_token").ToString()
		End Using
	End Using
	Return accessToken
End Function
```

**The scope depends on what the integration needs, and no single scope is required.** Relativity's documentation shows two values for this grant: `SystemUserInfo` on the REST authentication page, the Usage Reports page and in the official Transfer SDK sample, and `UserInfoAccess` in the Kepler .NET samples. No page says which is correct, whether both work, or what either grants, and the OAuth2 Client's `Scopes` property is documented as one that cannot be set. Use the value the API you call documents, and confirm it.

**Cache the token, and re-acquire it on expiry — do not refresh.** Keep the token for its lifetime (part 3 recommends 60 minutes, Relativity's own wording) and request a new one with the client secret when it expires or a call is rejected for authentication. Client Credentials does not offer refresh tokens: **verified by NSerio against a live instance; not documented by Relativity** — the Allow Refresh Tokens control does not appear for this grant in the RelativityOne user interface. Relativity's own tables and text on refresh tokens conflict with each other, so re-acquisition with the secret is the advice that holds whichever way they are read.

**Long-running processes that use the Relativity SDK.** A `ServiceFactory` built once from a bearer token keeps that token after it expires. Rebuild the `ServiceFactory` and its proxies with a fresh token before expiry. This is the only advice consistent with the documentation: the package `Relativity.Kepler.Client.SDK` (documented) exposes a settable `Token` and an `HttpMessageHandler` overload but no token-provider overload, and no page documents renewing a token behind a factory.

**The client secret.** It is displayed once, at creation or reset. Store it as Secret Management (Part B of the Migration Documentation) describes; never in source, never in tracked configuration, which in a VB.NET estate includes `app.config`, `web.config` and `My.Settings`. A reset invalidates the previous secret immediately, so plan the cut-over before resetting it.

**Before deploying, confirm the calling identity.** Every call now runs as the OAuth2 Client's **Context User** — not as the user in the replaced credential, and for an import or a job as much as for a request. If that user has different workspace permissions or item-level security, the code returns fewer rows or `Unauthorized` in production with no change to its logic, and Relativity's audit attributes every call to that one account. Confirm the Context User's permissions match those of the previous identity. Relativity describes the Context User as usually a member of the Relativity Administrators group; the OAuth2 Client block (part 3) gives NSerio's recommendation, which differs.

**When the integration acts for a signed-in user.** Client Credentials is one service identity by construction. An integration that acts on behalf of a signed-in user is the Authorization Code case, and moving to it means a new OAuth2 Client, because the flow is fixed when a client is created. The finding's flow note says when to make that choice.

Verification: **Verification note — three things.** **A header set once cannot be refreshed**: an `HttpWebRequest` is one request, so its header is already per request; a `WebClient` reused across calls is the long-lived case, so set the header before each call with a current token. **The pair's encoding may be a separate finding.** **Confirm the endpoint in use is the RelativityOne REST API.**

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
<!-- generated:end VB-008 client-credentials -->

#### Authorization Code

<!-- generated:begin VB-008 authorization-code -->
**Confirming the flow.** An interactive-user signal in a file assigns Authorization Code, with this note: "An interactive-user signal in this file points to Authorization Code. Confirm the flow: use Client Credentials instead if the integration runs with credentials fixed for the integration and no signed-in user." Your user profile can also make Authorization Code the default; see Changing the default in [Confirming the flow](#confirming-the-flow). See [Confirming the flow](#confirming-the-flow) and the "Which flow" paragraph of VB-008 in [Part A](part-a-vbnet.md#vb-008).

Source form: **Applies to:** .NET Framework 4.7.2 period form (`WebClient`, `HttpWebRequest`); the same lines compile on .NET, where both types are obsolete in favour of `HttpClient`.

```vbnet
client.Headers(HttpRequestHeader.Authorization) = "Basic " & encoded
request.Headers(HttpRequestHeader.Authorization) = "Basic " & encoded
```

Under token authentication: **First, the destination:** if it is not RelativityOne, this finding is informational — that service's own token mechanism is the migration — and the rest of this template does not apply.

`"Bearer "` and the access token in place of `"Basic "` and the encoded pair, **set per request**:

```vbnet
client.Headers(HttpRequestHeader.Authorization) = "Bearer " & accessToken
request.Headers(HttpRequestHeader.Authorization) = "Bearer " & accessToken
```

The code that produced `encoded` has no counterpart: a bearer token travels verbatim.

Credentials: **Where the credentials come from.** The destination decides whether the migration applies. If the destination is not RelativityOne, the migration is that service's own token mechanism, this finding is informational for this tool, and the reader stops here. If it is RelativityOne, the token comes from the token-acquisition part that follows.

The text a finding assigned Authorization Code ("Code") receives after part 1. It has a common part and two variants, **server-side** and **desktop**. The bold lead-in of each element is its marker. Every fact Relativity does not document but NSerio verified carries the exact label **verified by NSerio against a live instance; not documented by Relativity**.

**Common to both variants**

**Sign in.** Replace the code that collected the user's credentials with a redirect: send the user's browser to the authorize endpoint `<host>/Relativity/Identity/connect/authorize`, which leads to the RelativityOne login page. The authorize endpoint is **verified by NSerio against a live instance; not documented by Relativity**. The request carries the standard authorization-code parameters — `client_id`, `response_type=code`, the registered `redirect_uri`, a `scope`, and a `state` value the application checks on return; these are standard OAuth, and Relativity documents none of them. No discovery document is relied on.

**Receive the code.** The browser returns to the Redirect URL registered on the OAuth2 Client, with the authorization code and the `state` value. Check the `state` before using the code. The Redirect URL must be exactly the one registered (the variants say how).

**Exchange the code.** Send a form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=authorization_code`, the `code`, the same `redirect_uri`, the `client_id` and the `client_secret`, plus the `code_verifier` when PKCE is used. The code is exchanged at that endpoint, which Relativity documents for the Client Credentials grant only; the exchange is **verified by NSerio against a live instance; not documented by Relativity**.

```vbnet
Dim form As New FormUrlEncodedContent(New Dictionary(Of String, String) From {
	{"grant_type", "authorization_code"},
	{"code", authorizationCode},
	{"redirect_uri", redirectUri},
	{"client_id", clientId},
	{"client_secret", clientSecret},
	{"code_verifier", codeVerifier}})
Using response As HttpResponseMessage = Await client.PostAsync(host & "/Relativity/Identity/connect/token", form)
	response.EnsureSuccessStatusCode()
End Using
```

**Use the token.** Send `Authorization: Bearer <access token>` — the change at the use site in part 1 — with `X-CSRF-Header: -` on REST and Kepler calls.

**On expiry.** With Allow Refresh Tokens enabled on the OAuth2 Client and PKCE used, the application holds a refresh token and, when the access token expires, sends a `grant_type=refresh_token` request with it and the client credentials in place of the code exchange — *inferred* from the standard OAuth shape, not tested. Without a refresh token the user signs in again. Relativity does not document a refresh token's lifetime, whether it rotates, how it is revoked, or whether a revocation endpoint exists, nor what disabling or deleting the client does to tokens already issued. A refresh token is a secret: store it as Secret Management (Part B of the Migration Documentation) describes, and on compromise disable the client and replace it. Enable refresh tokens only if the application must keep a session beyond one token lifetime without asking the user to sign in again.

**PKCE.** PKCE is required by Relativity's own documentation to obtain a refresh token, and not otherwise: a Code request that does not ask for a refresh token works without it, **verified by NSerio against a live instance; not documented by Relativity**. It is never a blanket requirement. NSerio recommends PKCE with the `S256` method in both variants (NSerio recommendation), and does not claim that Relativity accepts any particular method, which it does not document.

**Scope.** No scope set is prescribed for Code. The scope below works — `openid RelativityRequestOrigin UserInfo UserInfoAccess AuthenticationID RelativityWeb` — **verified by NSerio against a live instance; not documented by Relativity**, verified as working and not as minimal. `state` is sent as standard OAuth practice and `nonce` is not used; Relativity does not document whether it enforces either.

**The application stops collecting the password.** After this change the application never receives, stores or forwards the user's username and password: the user signs in on RelativityOne's own page, and the application holds only tokens. Remove the credential prompt, the form field and any storage of the password that the replaced code needed (in a VB.NET estate: the `InputBox`, the login form's password control, the Web Forms login control's handler).

**Single sign-on.** Sign-in happens on RelativityOne's own login page, so the instance's single sign-on and two-factor rules apply unchanged — *inferred* from where the sign-in happens, and not documented for clients that customers create.

**The client secret is required.** Code does not work without a client secret: tested and rejected, **verified by NSerio against a live instance; not documented by Relativity**, which confirms Relativity's own statement that the grant requires one. It is displayed once and is stored as Secret Management (Part B of the Migration Documentation) describes.

**Server-side variant.** For a web application whose code runs on a server — in VB.NET, an ASP.NET Web Forms page or a custom page whose code-behind sends the redirect (`Response.Redirect(authorizeUrl)`) and receives the code in a route of the application:

- **Redirect.** The application receives the code at one exact HTTPS URL per environment, a route of the application, with no wildcards (NSerio recommendation; Relativity's matching rules are not documented). The Relativity administrator registers the exact URL on the OAuth2 Client.
- **Secret and tokens.** The client secret stays on the server and never reaches the browser. The user's tokens are held server-side, keyed to the session, and never in the browser.
- **Exchange.** The server performs the code exchange itself, from the code-behind or a handler.

**Desktop variant.** For a desktop or command-line application run by a user on their own machine (WinForms, WPF, console):

- **The system browser.** Open the authorize URL in the user's default browser and not in a web view the application hosts, so the application never sees the password (NSerio recommendation).
- **A loopback redirect on a fixed port.** Listen on the loopback address only, on the registered port, for one request. The Redirect URL is `http://127.0.0.1:<port>/<path>`: a plain-HTTP loopback URL is accepted, **verified by NSerio against a live instance; not documented by Relativity**. The Relativity administrator registers that exact URL; because it is registered in advance, the port is fixed and not chosen at runtime.

```vbnet
Using listener As New HttpListener()
	listener.Prefixes.Add("http://127.0.0.1:<port>/<path>/")
	listener.Start()
	Process.Start(New ProcessStartInfo(authorizeUrl) With {.UseShellExecute = True})
	Dim context As HttpListenerContext = Await listener.GetContextAsync()
	Dim authorizationCode As String = context.Request.QueryString("code")
	Dim returnedState As String = context.Request.QueryString("state")
	context.Response.Close()
End Using
```

- **The secret is extractable.** Code requires a client secret (above), so a distributed desktop application has to embed one, and **a secret shipped inside a distributed binary can be extracted by anyone who holds the binary**. It protects nothing against the person running the application.
- **Compensating guidance** (NSerio recommendation, subject to your own security policy): set Trusted IPs where the users' outbound addresses are known and stable — they restrict where a token can be *requested*, and are checked when a token is issued against the client's ID and secret; use the recommended short Access Token Lifetime of 60 minutes; enable refresh tokens only when needed; and create one OAuth2 Client per distribution, so a leaked secret can be disabled without affecting the others.
- **A known issue.** Relativity documents no recommendation for a distributed desktop application; this design is what NSerio verified works, and Relativity's own recommendation is tracked as a known issue of the release.

Verification: **Verification note — three things.** **A header set once cannot be refreshed**: an `HttpWebRequest` is one request, so its header is already per request; a `WebClient` reused across calls is the long-lived case, so set the header before each call with a current token. **The pair's encoding may be a separate finding.** **Confirm the endpoint in use is the RelativityOne REST API.**

OAuth2 Client: Create or confirm this OAuth2 Client in RelativityOne (NSerio's observation: the menu path is Home > Authentication > OAuth2 Client Manager, as Relativity's REST API authentication page names it). This tool never connects to RelativityOne and never creates or changes the OAuth2 Client; the settings below are guidance.
- Name: a descriptive name identifying the application it serves.
- Flow Grant Type: Code
- Redirect URLs: Relativity documentation: each URL includes the http or https protocol prefix. The exact callback URL is registered by the Relativity administrator. Server-side web applications, NSerio's recommendation: one exact HTTPS URL per environment, with no wildcards. Desktop and CLI applications: a plain-HTTP loopback URL, `http://127.0.0.1:<port>/<path>`, on a fixed port because the URL is registered in advance and the port cannot be chosen at runtime (verified by NSerio against a live instance; not documented by Relativity).
- Access Token Lifetime: 60 minutes, stated in minutes because the field is in minutes. Relativity documentation: tokens for this flow must have a short lifetime, and the recommended value matches the Identity Server default.
- Allow refresh tokens: NSerio's recommendation: leave it off unless the application must keep a user session active beyond the access token lifetime without asking the user to sign in again. If it is on, the refresh tokens are stored as secrets, per Secret Management (Part B of the Migration Documentation).
- Enabled: on.
<!-- generated:end VB-008 authorization-code -->

### VB-009 · WcfBasicTransportCredential

<a id="vb-009-migration"></a>

#### Client Credentials

<!-- generated:begin VB-009 client-credentials -->
**Confirming the flow.** When no signal in a file decides the flow, the tool assigns Client Credentials, the default, with this note: "Who supplies the credential at run time? If a person types it or signs in, the integration acts for that person: use Authorization Code. If it comes from configuration, settings, an environment variable or a secret store: use Client Credentials. No signal in this file showed which applies, so Client Credentials, the default, is recommended. Use Authorization Code instead if the integration acts on behalf of a signed-in user, or if your RelativityOne instance already has a Code-flow OAuth2 Client for this integration." A fixed-credential signal assigns it with this note: "A fixed-credential signal in this file points to Client Credentials, but how often that kind of signal is right has not been measured. Use Authorization Code instead if the integration acts on behalf of a signed-in user, or if your RelativityOne instance already has a Code-flow OAuth2 Client for this integration." See [Confirming the flow](#confirming-the-flow) and the "Which flow" paragraph of VB-009 in [Part A](part-a-vbnet.md#vb-009).

Source form: **Applies to:** .NET Framework in its classic WCF form.

```vbnet
binding.Security.Transport.ClientCredentialType = HttpClientCredentialType.Basic
client.ClientCredentials.UserName.UserName = userName
client.ClientCredentials.UserName.Password = password
```

Under token authentication: **First, the destination:** if it is not RelativityOne, this finding is informational — that service's own token mechanism is the migration — and the rest of this template does not apply.

The binding's **transport credential type becomes the token form that binding supports**, and the user name and password assignments are removed — from code, and from configuration where the credential lives there instead. No code form is given: which token form a binding and a service accept is decided by the service, not by the client. **If the SOAP service is Relativity's, confirm it accepts bearer at all before changing the binding.** Where it does not, there is no scheme to migrate to on that service.

Credentials: **Where the credentials come from.** The destination decides whether the migration applies. If the destination is not RelativityOne, the migration is that service's own token mechanism, this finding is informational for this tool, and the reader stops here. If it is RelativityOne, the token comes from the token-acquisition part that follows.

**Where the token comes from.** Create an OAuth2 Client with Flow Grant Type set to Client Credentials (part 3 lists its settings), then send a form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=client_credentials`, `client_id`, `client_secret` and a `scope`. The token endpoint is **documented** for this grant. The token is then sent as `Authorization: Bearer <token>`, with `X-CSRF-Header: -` on REST and Kepler calls.

**.NET (both runtimes' `HttpClient`; `System.Text.Json` on .NET)**

```vbnet
Imports System.Net.Http
Imports System.Text.Json

Public Async Function AcquireAccessTokenAsync(host As String, clientId As String, clientSecret As String, scope As String) As Task(Of String)
	Dim accessToken As String
	Using client As New HttpClient()
		Dim form As New FormUrlEncodedContent(New Dictionary(Of String, String) From {
			{"grant_type", "client_credentials"},
			{"client_id", clientId},
			{"client_secret", clientSecret},
			{"scope", scope}})
		Using response As HttpResponseMessage = Await client.PostAsync(host & "/Relativity/Identity/connect/token", form)
			response.EnsureSuccessStatusCode()
			Using document As JsonDocument = JsonDocument.Parse(Await response.Content.ReadAsStringAsync())
				accessToken = document.RootElement.GetProperty("access_token").GetString()
			End Using
		End Using
	End Using
	Return accessToken
End Function
```

**.NET Framework 4.7.2** — the same call with the serializer the runtime ships (`System.Web.Extensions`); `HttpClient` is in `System.Net.Http`:

```vbnet
Imports System.Net.Http
Imports System.Web.Script.Serialization

Public Async Function AcquireAccessTokenAsync(host As String, clientId As String, clientSecret As String, scope As String) As Task(Of String)
	Dim accessToken As String
	Using client As New HttpClient()
		Dim form As New FormUrlEncodedContent(New Dictionary(Of String, String) From {
			{"grant_type", "client_credentials"},
			{"client_id", clientId},
			{"client_secret", clientSecret},
			{"scope", scope}})
		Using response As HttpResponseMessage = Await client.PostAsync(host & "/Relativity/Identity/connect/token", form)
			response.EnsureSuccessStatusCode()
			Dim body As String = Await response.Content.ReadAsStringAsync()
			Dim fields As Dictionary(Of String, Object) = New JavaScriptSerializer().Deserialize(Of Dictionary(Of String, Object))(body)
			accessToken = fields("access_token").ToString()
		End Using
	End Using
	Return accessToken
End Function
```

**The scope depends on what the integration needs, and no single scope is required.** Relativity's documentation shows two values for this grant: `SystemUserInfo` on the REST authentication page, the Usage Reports page and in the official Transfer SDK sample, and `UserInfoAccess` in the Kepler .NET samples. No page says which is correct, whether both work, or what either grants, and the OAuth2 Client's `Scopes` property is documented as one that cannot be set. Use the value the API you call documents, and confirm it.

**Cache the token, and re-acquire it on expiry — do not refresh.** Keep the token for its lifetime (part 3 recommends 60 minutes, Relativity's own wording) and request a new one with the client secret when it expires or a call is rejected for authentication. Client Credentials does not offer refresh tokens: **verified by NSerio against a live instance; not documented by Relativity** — the Allow Refresh Tokens control does not appear for this grant in the RelativityOne user interface. Relativity's own tables and text on refresh tokens conflict with each other, so re-acquisition with the secret is the advice that holds whichever way they are read.

**Long-running processes that use the Relativity SDK.** A `ServiceFactory` built once from a bearer token keeps that token after it expires. Rebuild the `ServiceFactory` and its proxies with a fresh token before expiry. This is the only advice consistent with the documentation: the package `Relativity.Kepler.Client.SDK` (documented) exposes a settable `Token` and an `HttpMessageHandler` overload but no token-provider overload, and no page documents renewing a token behind a factory.

**The client secret.** It is displayed once, at creation or reset. Store it as Secret Management (Part B of the Migration Documentation) describes; never in source, never in tracked configuration, which in a VB.NET estate includes `app.config`, `web.config` and `My.Settings`. A reset invalidates the previous secret immediately, so plan the cut-over before resetting it.

**Before deploying, confirm the calling identity.** Every call now runs as the OAuth2 Client's **Context User** — not as the user in the replaced credential, and for an import or a job as much as for a request. If that user has different workspace permissions or item-level security, the code returns fewer rows or `Unauthorized` in production with no change to its logic, and Relativity's audit attributes every call to that one account. Confirm the Context User's permissions match those of the previous identity. Relativity describes the Context User as usually a member of the Relativity Administrators group; the OAuth2 Client block (part 3) gives NSerio's recommendation, which differs.

**When the integration acts for a signed-in user.** Client Credentials is one service identity by construction. An integration that acts on behalf of a signed-in user is the Authorization Code case, and moving to it means a new OAuth2 Client, because the flow is fixed when a client is created. The finding's flow note says when to make that choice.

Verification: **Verification note — three things.** **A SOAP or WCF service may not accept bearer at all**: this is the first check, not the last. **The credential may not be in this file**: the binding is what this finding reports; its user name and password may live in `app.config` or `web.config`, which this tool does not scan. **A token has a finite lifetime**: a client proxy configured once and kept for the process's life holds a token that expires; replace it on the proxy or recreate the proxy.

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
<!-- generated:end VB-009 client-credentials -->

#### Authorization Code

<!-- generated:begin VB-009 authorization-code -->
**Confirming the flow.** An interactive-user signal in a file assigns Authorization Code, with this note: "An interactive-user signal in this file points to Authorization Code. Confirm the flow: use Client Credentials instead if the integration runs with credentials fixed for the integration and no signed-in user." Your user profile can also make Authorization Code the default; see Changing the default in [Confirming the flow](#confirming-the-flow). See [Confirming the flow](#confirming-the-flow) and the "Which flow" paragraph of VB-009 in [Part A](part-a-vbnet.md#vb-009).

Source form: **Applies to:** .NET Framework in its classic WCF form.

```vbnet
binding.Security.Transport.ClientCredentialType = HttpClientCredentialType.Basic
client.ClientCredentials.UserName.UserName = userName
client.ClientCredentials.UserName.Password = password
```

Under token authentication: **First, the destination:** if it is not RelativityOne, this finding is informational — that service's own token mechanism is the migration — and the rest of this template does not apply.

The binding's **transport credential type becomes the token form that binding supports**, and the user name and password assignments are removed — from code, and from configuration where the credential lives there instead. No code form is given: which token form a binding and a service accept is decided by the service, not by the client. **If the SOAP service is Relativity's, confirm it accepts bearer at all before changing the binding.** Where it does not, there is no scheme to migrate to on that service.

Credentials: **Where the credentials come from.** The destination decides whether the migration applies. If the destination is not RelativityOne, the migration is that service's own token mechanism, this finding is informational for this tool, and the reader stops here. If it is RelativityOne, the token comes from the token-acquisition part that follows.

The text a finding assigned Authorization Code ("Code") receives after part 1. It has a common part and two variants, **server-side** and **desktop**. The bold lead-in of each element is its marker. Every fact Relativity does not document but NSerio verified carries the exact label **verified by NSerio against a live instance; not documented by Relativity**.

**Common to both variants**

**Sign in.** Replace the code that collected the user's credentials with a redirect: send the user's browser to the authorize endpoint `<host>/Relativity/Identity/connect/authorize`, which leads to the RelativityOne login page. The authorize endpoint is **verified by NSerio against a live instance; not documented by Relativity**. The request carries the standard authorization-code parameters — `client_id`, `response_type=code`, the registered `redirect_uri`, a `scope`, and a `state` value the application checks on return; these are standard OAuth, and Relativity documents none of them. No discovery document is relied on.

**Receive the code.** The browser returns to the Redirect URL registered on the OAuth2 Client, with the authorization code and the `state` value. Check the `state` before using the code. The Redirect URL must be exactly the one registered (the variants say how).

**Exchange the code.** Send a form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=authorization_code`, the `code`, the same `redirect_uri`, the `client_id` and the `client_secret`, plus the `code_verifier` when PKCE is used. The code is exchanged at that endpoint, which Relativity documents for the Client Credentials grant only; the exchange is **verified by NSerio against a live instance; not documented by Relativity**.

```vbnet
Dim form As New FormUrlEncodedContent(New Dictionary(Of String, String) From {
	{"grant_type", "authorization_code"},
	{"code", authorizationCode},
	{"redirect_uri", redirectUri},
	{"client_id", clientId},
	{"client_secret", clientSecret},
	{"code_verifier", codeVerifier}})
Using response As HttpResponseMessage = Await client.PostAsync(host & "/Relativity/Identity/connect/token", form)
	response.EnsureSuccessStatusCode()
End Using
```

**Use the token.** Send `Authorization: Bearer <access token>` — the change at the use site in part 1 — with `X-CSRF-Header: -` on REST and Kepler calls.

**On expiry.** With Allow Refresh Tokens enabled on the OAuth2 Client and PKCE used, the application holds a refresh token and, when the access token expires, sends a `grant_type=refresh_token` request with it and the client credentials in place of the code exchange — *inferred* from the standard OAuth shape, not tested. Without a refresh token the user signs in again. Relativity does not document a refresh token's lifetime, whether it rotates, how it is revoked, or whether a revocation endpoint exists, nor what disabling or deleting the client does to tokens already issued. A refresh token is a secret: store it as Secret Management (Part B of the Migration Documentation) describes, and on compromise disable the client and replace it. Enable refresh tokens only if the application must keep a session beyond one token lifetime without asking the user to sign in again.

**PKCE.** PKCE is required by Relativity's own documentation to obtain a refresh token, and not otherwise: a Code request that does not ask for a refresh token works without it, **verified by NSerio against a live instance; not documented by Relativity**. It is never a blanket requirement. NSerio recommends PKCE with the `S256` method in both variants (NSerio recommendation), and does not claim that Relativity accepts any particular method, which it does not document.

**Scope.** No scope set is prescribed for Code. The scope below works — `openid RelativityRequestOrigin UserInfo UserInfoAccess AuthenticationID RelativityWeb` — **verified by NSerio against a live instance; not documented by Relativity**, verified as working and not as minimal. `state` is sent as standard OAuth practice and `nonce` is not used; Relativity does not document whether it enforces either.

**The application stops collecting the password.** After this change the application never receives, stores or forwards the user's username and password: the user signs in on RelativityOne's own page, and the application holds only tokens. Remove the credential prompt, the form field and any storage of the password that the replaced code needed (in a VB.NET estate: the `InputBox`, the login form's password control, the Web Forms login control's handler).

**Single sign-on.** Sign-in happens on RelativityOne's own login page, so the instance's single sign-on and two-factor rules apply unchanged — *inferred* from where the sign-in happens, and not documented for clients that customers create.

**The client secret is required.** Code does not work without a client secret: tested and rejected, **verified by NSerio against a live instance; not documented by Relativity**, which confirms Relativity's own statement that the grant requires one. It is displayed once and is stored as Secret Management (Part B of the Migration Documentation) describes.

**Server-side variant.** For a web application whose code runs on a server — in VB.NET, an ASP.NET Web Forms page or a custom page whose code-behind sends the redirect (`Response.Redirect(authorizeUrl)`) and receives the code in a route of the application:

- **Redirect.** The application receives the code at one exact HTTPS URL per environment, a route of the application, with no wildcards (NSerio recommendation; Relativity's matching rules are not documented). The Relativity administrator registers the exact URL on the OAuth2 Client.
- **Secret and tokens.** The client secret stays on the server and never reaches the browser. The user's tokens are held server-side, keyed to the session, and never in the browser.
- **Exchange.** The server performs the code exchange itself, from the code-behind or a handler.

**Desktop variant.** For a desktop or command-line application run by a user on their own machine (WinForms, WPF, console):

- **The system browser.** Open the authorize URL in the user's default browser and not in a web view the application hosts, so the application never sees the password (NSerio recommendation).
- **A loopback redirect on a fixed port.** Listen on the loopback address only, on the registered port, for one request. The Redirect URL is `http://127.0.0.1:<port>/<path>`: a plain-HTTP loopback URL is accepted, **verified by NSerio against a live instance; not documented by Relativity**. The Relativity administrator registers that exact URL; because it is registered in advance, the port is fixed and not chosen at runtime.

```vbnet
Using listener As New HttpListener()
	listener.Prefixes.Add("http://127.0.0.1:<port>/<path>/")
	listener.Start()
	Process.Start(New ProcessStartInfo(authorizeUrl) With {.UseShellExecute = True})
	Dim context As HttpListenerContext = Await listener.GetContextAsync()
	Dim authorizationCode As String = context.Request.QueryString("code")
	Dim returnedState As String = context.Request.QueryString("state")
	context.Response.Close()
End Using
```

- **The secret is extractable.** Code requires a client secret (above), so a distributed desktop application has to embed one, and **a secret shipped inside a distributed binary can be extracted by anyone who holds the binary**. It protects nothing against the person running the application.
- **Compensating guidance** (NSerio recommendation, subject to your own security policy): set Trusted IPs where the users' outbound addresses are known and stable — they restrict where a token can be *requested*, and are checked when a token is issued against the client's ID and secret; use the recommended short Access Token Lifetime of 60 minutes; enable refresh tokens only when needed; and create one OAuth2 Client per distribution, so a leaked secret can be disabled without affecting the others.
- **A known issue.** Relativity documents no recommendation for a distributed desktop application; this design is what NSerio verified works, and Relativity's own recommendation is tracked as a known issue of the release.

Verification: **Verification note — three things.** **A SOAP or WCF service may not accept bearer at all**: this is the first check, not the last. **The credential may not be in this file**: the binding is what this finding reports; its user name and password may live in `app.config` or `web.config`, which this tool does not scan. **A token has a finite lifetime**: a client proxy configured once and kept for the process's life holds a token that expires; replace it on the proxy or recreate the proxy.

OAuth2 Client: Create or confirm this OAuth2 Client in RelativityOne (NSerio's observation: the menu path is Home > Authentication > OAuth2 Client Manager, as Relativity's REST API authentication page names it). This tool never connects to RelativityOne and never creates or changes the OAuth2 Client; the settings below are guidance.
- Name: a descriptive name identifying the application it serves.
- Flow Grant Type: Code
- Redirect URLs: Relativity documentation: each URL includes the http or https protocol prefix. The exact callback URL is registered by the Relativity administrator. Server-side web applications, NSerio's recommendation: one exact HTTPS URL per environment, with no wildcards. Desktop and CLI applications: a plain-HTTP loopback URL, `http://127.0.0.1:<port>/<path>`, on a fixed port because the URL is registered in advance and the port cannot be chosen at runtime (verified by NSerio against a live instance; not documented by Relativity).
- Access Token Lifetime: 60 minutes, stated in minutes because the field is in minutes. Relativity documentation: tokens for this flow must have a short lifetime, and the recommended value matches the Identity Server default.
- Allow refresh tokens: NSerio's recommendation: leave it off unless the application must keep a user session active beyond the access token lifetime without asking the user to sign in again. If it is on, the refresh tokens are stored as secrets, per Secret Management (Part B of the Migration Documentation).
- Enabled: on.
<!-- generated:end VB-009 authorization-code -->

### VB-010 · SdkConstructorWithUserAndPassword

<a id="vb-010-migration"></a>

#### Client Credentials

<!-- generated:begin VB-010 client-credentials -->
**Confirming the flow.** When no signal in a file decides the flow, the tool assigns Client Credentials, the default, with this note: "Who supplies the credential at run time? If a person types it or signs in, the integration acts for that person: use Authorization Code. If it comes from configuration, settings, an environment variable or a secret store: use Client Credentials. No signal in this file showed which applies, so Client Credentials, the default, is recommended. Use Authorization Code instead if the integration acts on behalf of a signed-in user, or if your RelativityOne instance already has a Code-flow OAuth2 Client for this integration." A fixed-credential signal assigns it with this note: "A fixed-credential signal in this file points to Client Credentials, but how often that kind of signal is right has not been measured. Use Authorization Code instead if the integration acts on behalf of a signed-in user, or if your RelativityOne instance already has a Code-flow OAuth2 Client for this integration." See [Confirming the flow](#confirming-the-flow) and the "Which flow" paragraph of VB-010 in [Part A](part-a-vbnet.md#vb-010).

Source form: **Applies to:** both in syntax; the documented factory is a Relativity Server page (below).

```vbnet
Dim importApi As New ImportAPI(userName, password, webServiceUrl)
```

Under token authentication: **First, the destination:** if this Relativity instance is not RelativityOne — including a Relativity Server instance, whose own documentation governs — this finding is informational and the rest of this template does not apply.

**Change what the entry point receives, not the call site.** Relativity's Import API documentation names one static factory on the type, `ImportAPI.CreateByRsaBearerToken(webServiceUrl)`. **It takes the web service URL and no token**: it uses the bearer token of the identity of the process that runs it, and the documentation says it should be used only by processes hosted by the Relativity Service Account, such as an agent. Where the code runs in such a host, the construction above is replaced by:

```vbnet
Dim importApi As ImportAPI = ImportAPI.CreateByRsaBearerToken(webServiceUrl)
```

**For a standalone or desktop process that would obtain an OAuth2 token itself, this template gives no code form.** The documentation names no `ImportAPI` factory that accepts a token acquired by the caller, and it lists bearer-token authentication as not supported for a desktop application, where user name and password is the preferred form; that is a known issue of the release, not something this text resolves. A subtype of the entry point written by the code (a class that inherits it) is changed where its base is constructed. The migrated construction must not keep the three-argument form with a credential pair, which is the one this finding reports.

Credentials: **Where the credentials come from.** The SDK type establishes that the destination is Relativity, not that it is RelativityOne; if it is not RelativityOne the migration is that service's own token mechanism, this finding is informational for this tool, and the reader stops here. If it is RelativityOne, the factory above carries no token of the code's own: the identity comes from the hosting process, so the token-acquisition part that follows applies only to code that obtains a token itself, and for the entry point that case is the known issue stated above.

**Where the token comes from.** Create an OAuth2 Client with Flow Grant Type set to Client Credentials (part 3 lists its settings), then send a form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=client_credentials`, `client_id`, `client_secret` and a `scope`. The token endpoint is **documented** for this grant. The token is then sent as `Authorization: Bearer <token>`, with `X-CSRF-Header: -` on REST and Kepler calls.

**.NET (both runtimes' `HttpClient`; `System.Text.Json` on .NET)**

```vbnet
Imports System.Net.Http
Imports System.Text.Json

Public Async Function AcquireAccessTokenAsync(host As String, clientId As String, clientSecret As String, scope As String) As Task(Of String)
	Dim accessToken As String
	Using client As New HttpClient()
		Dim form As New FormUrlEncodedContent(New Dictionary(Of String, String) From {
			{"grant_type", "client_credentials"},
			{"client_id", clientId},
			{"client_secret", clientSecret},
			{"scope", scope}})
		Using response As HttpResponseMessage = Await client.PostAsync(host & "/Relativity/Identity/connect/token", form)
			response.EnsureSuccessStatusCode()
			Using document As JsonDocument = JsonDocument.Parse(Await response.Content.ReadAsStringAsync())
				accessToken = document.RootElement.GetProperty("access_token").GetString()
			End Using
		End Using
	End Using
	Return accessToken
End Function
```

**.NET Framework 4.7.2** — the same call with the serializer the runtime ships (`System.Web.Extensions`); `HttpClient` is in `System.Net.Http`:

```vbnet
Imports System.Net.Http
Imports System.Web.Script.Serialization

Public Async Function AcquireAccessTokenAsync(host As String, clientId As String, clientSecret As String, scope As String) As Task(Of String)
	Dim accessToken As String
	Using client As New HttpClient()
		Dim form As New FormUrlEncodedContent(New Dictionary(Of String, String) From {
			{"grant_type", "client_credentials"},
			{"client_id", clientId},
			{"client_secret", clientSecret},
			{"scope", scope}})
		Using response As HttpResponseMessage = Await client.PostAsync(host & "/Relativity/Identity/connect/token", form)
			response.EnsureSuccessStatusCode()
			Dim body As String = Await response.Content.ReadAsStringAsync()
			Dim fields As Dictionary(Of String, Object) = New JavaScriptSerializer().Deserialize(Of Dictionary(Of String, Object))(body)
			accessToken = fields("access_token").ToString()
		End Using
	End Using
	Return accessToken
End Function
```

**The scope depends on what the integration needs, and no single scope is required.** Relativity's documentation shows two values for this grant: `SystemUserInfo` on the REST authentication page, the Usage Reports page and in the official Transfer SDK sample, and `UserInfoAccess` in the Kepler .NET samples. No page says which is correct, whether both work, or what either grants, and the OAuth2 Client's `Scopes` property is documented as one that cannot be set. Use the value the API you call documents, and confirm it.

**Cache the token, and re-acquire it on expiry — do not refresh.** Keep the token for its lifetime (part 3 recommends 60 minutes, Relativity's own wording) and request a new one with the client secret when it expires or a call is rejected for authentication. Client Credentials does not offer refresh tokens: **verified by NSerio against a live instance; not documented by Relativity** — the Allow Refresh Tokens control does not appear for this grant in the RelativityOne user interface. Relativity's own tables and text on refresh tokens conflict with each other, so re-acquisition with the secret is the advice that holds whichever way they are read.

**Long-running processes that use the Relativity SDK.** A `ServiceFactory` built once from a bearer token keeps that token after it expires. Rebuild the `ServiceFactory` and its proxies with a fresh token before expiry. This is the only advice consistent with the documentation: the package `Relativity.Kepler.Client.SDK` (documented) exposes a settable `Token` and an `HttpMessageHandler` overload but no token-provider overload, and no page documents renewing a token behind a factory.

**The client secret.** It is displayed once, at creation or reset. Store it as Secret Management (Part B of the Migration Documentation) describes; never in source, never in tracked configuration, which in a VB.NET estate includes `app.config`, `web.config` and `My.Settings`. A reset invalidates the previous secret immediately, so plan the cut-over before resetting it.

**Before deploying, confirm the calling identity.** Every call now runs as the OAuth2 Client's **Context User** — not as the user in the replaced credential, and for an import or a job as much as for a request. If that user has different workspace permissions or item-level security, the code returns fewer rows or `Unauthorized` in production with no change to its logic, and Relativity's audit attributes every call to that one account. Confirm the Context User's permissions match those of the previous identity. Relativity describes the Context User as usually a member of the Relativity Administrators group; the OAuth2 Client block (part 3) gives NSerio's recommendation, which differs.

**When the integration acts for a signed-in user.** Client Credentials is one service identity by construction. An integration that acts on behalf of a signed-in user is the Authorization Code case, and moving to it means a new OAuth2 Client, because the flow is fixed when a client is created. The finding's flow note says when to make that choice.

Verification: **Verification note — two things, and the first can break working code.** **The factory is documented for hosted processes only, and for Relativity Server.** It is meant for an agent or another process hosted by the Relativity Service Account; the two documentation pages that describe it are Relativity Server pages, so confirm that the SDK version in use for RelativityOne exposes it before relying on it. **Confirm the SDK version in use exposes `CreateByRsaBearerToken`.** Where it does not, this is an SDK upgrade before it is a code change; the documentation states that the factory handles authentication and token expiration itself.

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
<!-- generated:end VB-010 client-credentials -->

#### Authorization Code

<!-- generated:begin VB-010 authorization-code -->
**Confirming the flow.** An interactive-user signal in a file assigns Authorization Code, with this note: "An interactive-user signal in this file points to Authorization Code. Confirm the flow: use Client Credentials instead if the integration runs with credentials fixed for the integration and no signed-in user." Your user profile can also make Authorization Code the default; see Changing the default in [Confirming the flow](#confirming-the-flow). See [Confirming the flow](#confirming-the-flow) and the "Which flow" paragraph of VB-010 in [Part A](part-a-vbnet.md#vb-010).

Source form: **Applies to:** both in syntax; the documented factory is a Relativity Server page (below).

```vbnet
Dim importApi As New ImportAPI(userName, password, webServiceUrl)
```

Under token authentication: **First, the destination:** if this Relativity instance is not RelativityOne — including a Relativity Server instance, whose own documentation governs — this finding is informational and the rest of this template does not apply.

**Change what the entry point receives, not the call site.** Relativity's Import API documentation names one static factory on the type, `ImportAPI.CreateByRsaBearerToken(webServiceUrl)`. **It takes the web service URL and no token**: it uses the bearer token of the identity of the process that runs it, and the documentation says it should be used only by processes hosted by the Relativity Service Account, such as an agent. Where the code runs in such a host, the construction above is replaced by:

```vbnet
Dim importApi As ImportAPI = ImportAPI.CreateByRsaBearerToken(webServiceUrl)
```

**For a standalone or desktop process that would obtain an OAuth2 token itself, this template gives no code form.** The documentation names no `ImportAPI` factory that accepts a token acquired by the caller, and it lists bearer-token authentication as not supported for a desktop application, where user name and password is the preferred form; that is a known issue of the release, not something this text resolves. A subtype of the entry point written by the code (a class that inherits it) is changed where its base is constructed. The migrated construction must not keep the three-argument form with a credential pair, which is the one this finding reports.

Credentials: **Where the credentials come from.** The SDK type establishes that the destination is Relativity, not that it is RelativityOne; if it is not RelativityOne the migration is that service's own token mechanism, this finding is informational for this tool, and the reader stops here. If it is RelativityOne, the factory above carries no token of the code's own: the identity comes from the hosting process, so the token-acquisition part that follows applies only to code that obtains a token itself, and for the entry point that case is the known issue stated above.

The text a finding assigned Authorization Code ("Code") receives after part 1. It has a common part and two variants, **server-side** and **desktop**. The bold lead-in of each element is its marker. Every fact Relativity does not document but NSerio verified carries the exact label **verified by NSerio against a live instance; not documented by Relativity**.

**Common to both variants**

**Sign in.** Replace the code that collected the user's credentials with a redirect: send the user's browser to the authorize endpoint `<host>/Relativity/Identity/connect/authorize`, which leads to the RelativityOne login page. The authorize endpoint is **verified by NSerio against a live instance; not documented by Relativity**. The request carries the standard authorization-code parameters — `client_id`, `response_type=code`, the registered `redirect_uri`, a `scope`, and a `state` value the application checks on return; these are standard OAuth, and Relativity documents none of them. No discovery document is relied on.

**Receive the code.** The browser returns to the Redirect URL registered on the OAuth2 Client, with the authorization code and the `state` value. Check the `state` before using the code. The Redirect URL must be exactly the one registered (the variants say how).

**Exchange the code.** Send a form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=authorization_code`, the `code`, the same `redirect_uri`, the `client_id` and the `client_secret`, plus the `code_verifier` when PKCE is used. The code is exchanged at that endpoint, which Relativity documents for the Client Credentials grant only; the exchange is **verified by NSerio against a live instance; not documented by Relativity**.

```vbnet
Dim form As New FormUrlEncodedContent(New Dictionary(Of String, String) From {
	{"grant_type", "authorization_code"},
	{"code", authorizationCode},
	{"redirect_uri", redirectUri},
	{"client_id", clientId},
	{"client_secret", clientSecret},
	{"code_verifier", codeVerifier}})
Using response As HttpResponseMessage = Await client.PostAsync(host & "/Relativity/Identity/connect/token", form)
	response.EnsureSuccessStatusCode()
End Using
```

**Use the token.** Send `Authorization: Bearer <access token>` — the change at the use site in part 1 — with `X-CSRF-Header: -` on REST and Kepler calls.

**On expiry.** With Allow Refresh Tokens enabled on the OAuth2 Client and PKCE used, the application holds a refresh token and, when the access token expires, sends a `grant_type=refresh_token` request with it and the client credentials in place of the code exchange — *inferred* from the standard OAuth shape, not tested. Without a refresh token the user signs in again. Relativity does not document a refresh token's lifetime, whether it rotates, how it is revoked, or whether a revocation endpoint exists, nor what disabling or deleting the client does to tokens already issued. A refresh token is a secret: store it as Secret Management (Part B of the Migration Documentation) describes, and on compromise disable the client and replace it. Enable refresh tokens only if the application must keep a session beyond one token lifetime without asking the user to sign in again.

**PKCE.** PKCE is required by Relativity's own documentation to obtain a refresh token, and not otherwise: a Code request that does not ask for a refresh token works without it, **verified by NSerio against a live instance; not documented by Relativity**. It is never a blanket requirement. NSerio recommends PKCE with the `S256` method in both variants (NSerio recommendation), and does not claim that Relativity accepts any particular method, which it does not document.

**Scope.** No scope set is prescribed for Code. The scope below works — `openid RelativityRequestOrigin UserInfo UserInfoAccess AuthenticationID RelativityWeb` — **verified by NSerio against a live instance; not documented by Relativity**, verified as working and not as minimal. `state` is sent as standard OAuth practice and `nonce` is not used; Relativity does not document whether it enforces either.

**The application stops collecting the password.** After this change the application never receives, stores or forwards the user's username and password: the user signs in on RelativityOne's own page, and the application holds only tokens. Remove the credential prompt, the form field and any storage of the password that the replaced code needed (in a VB.NET estate: the `InputBox`, the login form's password control, the Web Forms login control's handler).

**Single sign-on.** Sign-in happens on RelativityOne's own login page, so the instance's single sign-on and two-factor rules apply unchanged — *inferred* from where the sign-in happens, and not documented for clients that customers create.

**The client secret is required.** Code does not work without a client secret: tested and rejected, **verified by NSerio against a live instance; not documented by Relativity**, which confirms Relativity's own statement that the grant requires one. It is displayed once and is stored as Secret Management (Part B of the Migration Documentation) describes.

**Server-side variant.** For a web application whose code runs on a server — in VB.NET, an ASP.NET Web Forms page or a custom page whose code-behind sends the redirect (`Response.Redirect(authorizeUrl)`) and receives the code in a route of the application:

- **Redirect.** The application receives the code at one exact HTTPS URL per environment, a route of the application, with no wildcards (NSerio recommendation; Relativity's matching rules are not documented). The Relativity administrator registers the exact URL on the OAuth2 Client.
- **Secret and tokens.** The client secret stays on the server and never reaches the browser. The user's tokens are held server-side, keyed to the session, and never in the browser.
- **Exchange.** The server performs the code exchange itself, from the code-behind or a handler.

**Desktop variant.** For a desktop or command-line application run by a user on their own machine (WinForms, WPF, console):

- **The system browser.** Open the authorize URL in the user's default browser and not in a web view the application hosts, so the application never sees the password (NSerio recommendation).
- **A loopback redirect on a fixed port.** Listen on the loopback address only, on the registered port, for one request. The Redirect URL is `http://127.0.0.1:<port>/<path>`: a plain-HTTP loopback URL is accepted, **verified by NSerio against a live instance; not documented by Relativity**. The Relativity administrator registers that exact URL; because it is registered in advance, the port is fixed and not chosen at runtime.

```vbnet
Using listener As New HttpListener()
	listener.Prefixes.Add("http://127.0.0.1:<port>/<path>/")
	listener.Start()
	Process.Start(New ProcessStartInfo(authorizeUrl) With {.UseShellExecute = True})
	Dim context As HttpListenerContext = Await listener.GetContextAsync()
	Dim authorizationCode As String = context.Request.QueryString("code")
	Dim returnedState As String = context.Request.QueryString("state")
	context.Response.Close()
End Using
```

- **The secret is extractable.** Code requires a client secret (above), so a distributed desktop application has to embed one, and **a secret shipped inside a distributed binary can be extracted by anyone who holds the binary**. It protects nothing against the person running the application.
- **Compensating guidance** (NSerio recommendation, subject to your own security policy): set Trusted IPs where the users' outbound addresses are known and stable — they restrict where a token can be *requested*, and are checked when a token is issued against the client's ID and secret; use the recommended short Access Token Lifetime of 60 minutes; enable refresh tokens only when needed; and create one OAuth2 Client per distribution, so a leaked secret can be disabled without affecting the others.
- **A known issue.** Relativity documents no recommendation for a distributed desktop application; this design is what NSerio verified works, and Relativity's own recommendation is tracked as a known issue of the release.

Verification: **Verification note — two things, and the first can break working code.** **The factory is documented for hosted processes only, and for Relativity Server.** It is meant for an agent or another process hosted by the Relativity Service Account; the two documentation pages that describe it are Relativity Server pages, so confirm that the SDK version in use for RelativityOne exposes it before relying on it. **Confirm the SDK version in use exposes `CreateByRsaBearerToken`.** Where it does not, this is an SDK upgrade before it is a code change; the documentation states that the factory handles authentication and token expiration itself.

OAuth2 Client: Create or confirm this OAuth2 Client in RelativityOne (NSerio's observation: the menu path is Home > Authentication > OAuth2 Client Manager, as Relativity's REST API authentication page names it). This tool never connects to RelativityOne and never creates or changes the OAuth2 Client; the settings below are guidance.
- Name: a descriptive name identifying the application it serves.
- Flow Grant Type: Code
- Redirect URLs: Relativity documentation: each URL includes the http or https protocol prefix. The exact callback URL is registered by the Relativity administrator. Server-side web applications, NSerio's recommendation: one exact HTTPS URL per environment, with no wildcards. Desktop and CLI applications: a plain-HTTP loopback URL, `http://127.0.0.1:<port>/<path>`, on a fixed port because the URL is registered in advance and the port cannot be chosen at runtime (verified by NSerio against a live instance; not documented by Relativity).
- Access Token Lifetime: 60 minutes, stated in minutes because the field is in minutes. Relativity documentation: tokens for this flow must have a short lifetime, and the recommended value matches the Identity Server default.
- Allow refresh tokens: NSerio's recommendation: leave it off unless the application must keep a user session active beyond the access token lifetime without asking the user to sign in again. If it is on, the refresh tokens are stored as secrets, per Secret Management (Part B of the Migration Documentation).
- Enabled: on.
<!-- generated:end VB-010 authorization-code -->

### VB-011 · SdkSettingsCredentialProperties

<a id="vb-011-migration"></a>

#### Client Credentials

<!-- generated:begin VB-011 client-credentials -->
**Confirming the flow.** When no signal in a file decides the flow, the tool assigns Client Credentials, the default, with this note: "Who supplies the credential at run time? If a person types it or signs in, the integration acts for that person: use Authorization Code. If it comes from configuration, settings, an environment variable or a secret store: use Client Credentials. No signal in this file showed which applies, so Client Credentials, the default, is recommended. Use Authorization Code instead if the integration acts on behalf of a signed-in user, or if your RelativityOne instance already has a Code-flow OAuth2 Client for this integration." A fixed-credential signal assigns it with this note: "A fixed-credential signal in this file points to Client Credentials, but how often that kind of signal is right has not been measured. Use Authorization Code instead if the integration acts on behalf of a signed-in user, or if your RelativityOne instance already has a Code-flow OAuth2 Client for this integration." See [Confirming the flow](#confirming-the-flow) and the "Which flow" paragraph of VB-011 in [Part A](part-a-vbnet.md#vb-011).

Source form: **Applies to:** both.

```vbnet
With importJob.Settings
	.RelativityUsername = userName
	.RelativityPassword = password
End With
```

Under token authentication: **First, the destination:** if this Relativity instance is not RelativityOne — including a Relativity Server instance, whose own documentation governs — this finding is informational and the rest of this template does not apply.

Replace the user name and password properties with **the settings object's token-bearing equivalent**, assigned the access token, and remove both credential assignments. This template does not name that member, because which one exists depends on the SDK version. **Confirm the SDK version in use exposes one; where it does not, this is an SDK upgrade before it is a code change.**

Credentials: **Where the credentials come from.** The property names establish that the destination is Relativity, not that it is RelativityOne; if it is not RelativityOne the migration is that service's own token mechanism, this finding is informational for this tool, and the reader stops here. If it is RelativityOne, the token comes from the token-acquisition part that follows.

**Where the token comes from.** Create an OAuth2 Client with Flow Grant Type set to Client Credentials (part 3 lists its settings), then send a form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=client_credentials`, `client_id`, `client_secret` and a `scope`. The token endpoint is **documented** for this grant. The token is then sent as `Authorization: Bearer <token>`, with `X-CSRF-Header: -` on REST and Kepler calls.

**.NET (both runtimes' `HttpClient`; `System.Text.Json` on .NET)**

```vbnet
Imports System.Net.Http
Imports System.Text.Json

Public Async Function AcquireAccessTokenAsync(host As String, clientId As String, clientSecret As String, scope As String) As Task(Of String)
	Dim accessToken As String
	Using client As New HttpClient()
		Dim form As New FormUrlEncodedContent(New Dictionary(Of String, String) From {
			{"grant_type", "client_credentials"},
			{"client_id", clientId},
			{"client_secret", clientSecret},
			{"scope", scope}})
		Using response As HttpResponseMessage = Await client.PostAsync(host & "/Relativity/Identity/connect/token", form)
			response.EnsureSuccessStatusCode()
			Using document As JsonDocument = JsonDocument.Parse(Await response.Content.ReadAsStringAsync())
				accessToken = document.RootElement.GetProperty("access_token").GetString()
			End Using
		End Using
	End Using
	Return accessToken
End Function
```

**.NET Framework 4.7.2** — the same call with the serializer the runtime ships (`System.Web.Extensions`); `HttpClient` is in `System.Net.Http`:

```vbnet
Imports System.Net.Http
Imports System.Web.Script.Serialization

Public Async Function AcquireAccessTokenAsync(host As String, clientId As String, clientSecret As String, scope As String) As Task(Of String)
	Dim accessToken As String
	Using client As New HttpClient()
		Dim form As New FormUrlEncodedContent(New Dictionary(Of String, String) From {
			{"grant_type", "client_credentials"},
			{"client_id", clientId},
			{"client_secret", clientSecret},
			{"scope", scope}})
		Using response As HttpResponseMessage = Await client.PostAsync(host & "/Relativity/Identity/connect/token", form)
			response.EnsureSuccessStatusCode()
			Dim body As String = Await response.Content.ReadAsStringAsync()
			Dim fields As Dictionary(Of String, Object) = New JavaScriptSerializer().Deserialize(Of Dictionary(Of String, Object))(body)
			accessToken = fields("access_token").ToString()
		End Using
	End Using
	Return accessToken
End Function
```

**The scope depends on what the integration needs, and no single scope is required.** Relativity's documentation shows two values for this grant: `SystemUserInfo` on the REST authentication page, the Usage Reports page and in the official Transfer SDK sample, and `UserInfoAccess` in the Kepler .NET samples. No page says which is correct, whether both work, or what either grants, and the OAuth2 Client's `Scopes` property is documented as one that cannot be set. Use the value the API you call documents, and confirm it.

**Cache the token, and re-acquire it on expiry — do not refresh.** Keep the token for its lifetime (part 3 recommends 60 minutes, Relativity's own wording) and request a new one with the client secret when it expires or a call is rejected for authentication. Client Credentials does not offer refresh tokens: **verified by NSerio against a live instance; not documented by Relativity** — the Allow Refresh Tokens control does not appear for this grant in the RelativityOne user interface. Relativity's own tables and text on refresh tokens conflict with each other, so re-acquisition with the secret is the advice that holds whichever way they are read.

**Long-running processes that use the Relativity SDK.** A `ServiceFactory` built once from a bearer token keeps that token after it expires. Rebuild the `ServiceFactory` and its proxies with a fresh token before expiry. This is the only advice consistent with the documentation: the package `Relativity.Kepler.Client.SDK` (documented) exposes a settable `Token` and an `HttpMessageHandler` overload but no token-provider overload, and no page documents renewing a token behind a factory.

**The client secret.** It is displayed once, at creation or reset. Store it as Secret Management (Part B of the Migration Documentation) describes; never in source, never in tracked configuration, which in a VB.NET estate includes `app.config`, `web.config` and `My.Settings`. A reset invalidates the previous secret immediately, so plan the cut-over before resetting it.

**Before deploying, confirm the calling identity.** Every call now runs as the OAuth2 Client's **Context User** — not as the user in the replaced credential, and for an import or a job as much as for a request. If that user has different workspace permissions or item-level security, the code returns fewer rows or `Unauthorized` in production with no change to its logic, and Relativity's audit attributes every call to that one account. Confirm the Context User's permissions match those of the previous identity. Relativity describes the Context User as usually a member of the Relativity Administrators group; the OAuth2 Client block (part 3) gives NSerio's recommendation, which differs.

**When the integration acts for a signed-in user.** Client Credentials is one service identity by construction. An integration that acts on behalf of a signed-in user is the Authorization Code case, and moving to it means a new OAuth2 Client, because the flow is fixed when a client is created. The finding's flow note says when to make that choice.

Verification: **Verification note — two things. The first decides whether the change can be made at all.** **SDK version support comes first**: where the version in use has no token-bearing settings member, the change cannot be made at this call site at all. **A token has a finite lifetime**: a settings object populated once and reused across long or repeated jobs holds a token that expires; populate it with a current token for each job.

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
<!-- generated:end VB-011 client-credentials -->

#### Authorization Code

<!-- generated:begin VB-011 authorization-code -->
**Confirming the flow.** An interactive-user signal in a file assigns Authorization Code, with this note: "An interactive-user signal in this file points to Authorization Code. Confirm the flow: use Client Credentials instead if the integration runs with credentials fixed for the integration and no signed-in user." Your user profile can also make Authorization Code the default; see Changing the default in [Confirming the flow](#confirming-the-flow). See [Confirming the flow](#confirming-the-flow) and the "Which flow" paragraph of VB-011 in [Part A](part-a-vbnet.md#vb-011).

Source form: **Applies to:** both.

```vbnet
With importJob.Settings
	.RelativityUsername = userName
	.RelativityPassword = password
End With
```

Under token authentication: **First, the destination:** if this Relativity instance is not RelativityOne — including a Relativity Server instance, whose own documentation governs — this finding is informational and the rest of this template does not apply.

Replace the user name and password properties with **the settings object's token-bearing equivalent**, assigned the access token, and remove both credential assignments. This template does not name that member, because which one exists depends on the SDK version. **Confirm the SDK version in use exposes one; where it does not, this is an SDK upgrade before it is a code change.**

Credentials: **Where the credentials come from.** The property names establish that the destination is Relativity, not that it is RelativityOne; if it is not RelativityOne the migration is that service's own token mechanism, this finding is informational for this tool, and the reader stops here. If it is RelativityOne, the token comes from the token-acquisition part that follows.

The text a finding assigned Authorization Code ("Code") receives after part 1. It has a common part and two variants, **server-side** and **desktop**. The bold lead-in of each element is its marker. Every fact Relativity does not document but NSerio verified carries the exact label **verified by NSerio against a live instance; not documented by Relativity**.

**Common to both variants**

**Sign in.** Replace the code that collected the user's credentials with a redirect: send the user's browser to the authorize endpoint `<host>/Relativity/Identity/connect/authorize`, which leads to the RelativityOne login page. The authorize endpoint is **verified by NSerio against a live instance; not documented by Relativity**. The request carries the standard authorization-code parameters — `client_id`, `response_type=code`, the registered `redirect_uri`, a `scope`, and a `state` value the application checks on return; these are standard OAuth, and Relativity documents none of them. No discovery document is relied on.

**Receive the code.** The browser returns to the Redirect URL registered on the OAuth2 Client, with the authorization code and the `state` value. Check the `state` before using the code. The Redirect URL must be exactly the one registered (the variants say how).

**Exchange the code.** Send a form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=authorization_code`, the `code`, the same `redirect_uri`, the `client_id` and the `client_secret`, plus the `code_verifier` when PKCE is used. The code is exchanged at that endpoint, which Relativity documents for the Client Credentials grant only; the exchange is **verified by NSerio against a live instance; not documented by Relativity**.

```vbnet
Dim form As New FormUrlEncodedContent(New Dictionary(Of String, String) From {
	{"grant_type", "authorization_code"},
	{"code", authorizationCode},
	{"redirect_uri", redirectUri},
	{"client_id", clientId},
	{"client_secret", clientSecret},
	{"code_verifier", codeVerifier}})
Using response As HttpResponseMessage = Await client.PostAsync(host & "/Relativity/Identity/connect/token", form)
	response.EnsureSuccessStatusCode()
End Using
```

**Use the token.** Send `Authorization: Bearer <access token>` — the change at the use site in part 1 — with `X-CSRF-Header: -` on REST and Kepler calls.

**On expiry.** With Allow Refresh Tokens enabled on the OAuth2 Client and PKCE used, the application holds a refresh token and, when the access token expires, sends a `grant_type=refresh_token` request with it and the client credentials in place of the code exchange — *inferred* from the standard OAuth shape, not tested. Without a refresh token the user signs in again. Relativity does not document a refresh token's lifetime, whether it rotates, how it is revoked, or whether a revocation endpoint exists, nor what disabling or deleting the client does to tokens already issued. A refresh token is a secret: store it as Secret Management (Part B of the Migration Documentation) describes, and on compromise disable the client and replace it. Enable refresh tokens only if the application must keep a session beyond one token lifetime without asking the user to sign in again.

**PKCE.** PKCE is required by Relativity's own documentation to obtain a refresh token, and not otherwise: a Code request that does not ask for a refresh token works without it, **verified by NSerio against a live instance; not documented by Relativity**. It is never a blanket requirement. NSerio recommends PKCE with the `S256` method in both variants (NSerio recommendation), and does not claim that Relativity accepts any particular method, which it does not document.

**Scope.** No scope set is prescribed for Code. The scope below works — `openid RelativityRequestOrigin UserInfo UserInfoAccess AuthenticationID RelativityWeb` — **verified by NSerio against a live instance; not documented by Relativity**, verified as working and not as minimal. `state` is sent as standard OAuth practice and `nonce` is not used; Relativity does not document whether it enforces either.

**The application stops collecting the password.** After this change the application never receives, stores or forwards the user's username and password: the user signs in on RelativityOne's own page, and the application holds only tokens. Remove the credential prompt, the form field and any storage of the password that the replaced code needed (in a VB.NET estate: the `InputBox`, the login form's password control, the Web Forms login control's handler).

**Single sign-on.** Sign-in happens on RelativityOne's own login page, so the instance's single sign-on and two-factor rules apply unchanged — *inferred* from where the sign-in happens, and not documented for clients that customers create.

**The client secret is required.** Code does not work without a client secret: tested and rejected, **verified by NSerio against a live instance; not documented by Relativity**, which confirms Relativity's own statement that the grant requires one. It is displayed once and is stored as Secret Management (Part B of the Migration Documentation) describes.

**Server-side variant.** For a web application whose code runs on a server — in VB.NET, an ASP.NET Web Forms page or a custom page whose code-behind sends the redirect (`Response.Redirect(authorizeUrl)`) and receives the code in a route of the application:

- **Redirect.** The application receives the code at one exact HTTPS URL per environment, a route of the application, with no wildcards (NSerio recommendation; Relativity's matching rules are not documented). The Relativity administrator registers the exact URL on the OAuth2 Client.
- **Secret and tokens.** The client secret stays on the server and never reaches the browser. The user's tokens are held server-side, keyed to the session, and never in the browser.
- **Exchange.** The server performs the code exchange itself, from the code-behind or a handler.

**Desktop variant.** For a desktop or command-line application run by a user on their own machine (WinForms, WPF, console):

- **The system browser.** Open the authorize URL in the user's default browser and not in a web view the application hosts, so the application never sees the password (NSerio recommendation).
- **A loopback redirect on a fixed port.** Listen on the loopback address only, on the registered port, for one request. The Redirect URL is `http://127.0.0.1:<port>/<path>`: a plain-HTTP loopback URL is accepted, **verified by NSerio against a live instance; not documented by Relativity**. The Relativity administrator registers that exact URL; because it is registered in advance, the port is fixed and not chosen at runtime.

```vbnet
Using listener As New HttpListener()
	listener.Prefixes.Add("http://127.0.0.1:<port>/<path>/")
	listener.Start()
	Process.Start(New ProcessStartInfo(authorizeUrl) With {.UseShellExecute = True})
	Dim context As HttpListenerContext = Await listener.GetContextAsync()
	Dim authorizationCode As String = context.Request.QueryString("code")
	Dim returnedState As String = context.Request.QueryString("state")
	context.Response.Close()
End Using
```

- **The secret is extractable.** Code requires a client secret (above), so a distributed desktop application has to embed one, and **a secret shipped inside a distributed binary can be extracted by anyone who holds the binary**. It protects nothing against the person running the application.
- **Compensating guidance** (NSerio recommendation, subject to your own security policy): set Trusted IPs where the users' outbound addresses are known and stable — they restrict where a token can be *requested*, and are checked when a token is issued against the client's ID and secret; use the recommended short Access Token Lifetime of 60 minutes; enable refresh tokens only when needed; and create one OAuth2 Client per distribution, so a leaked secret can be disabled without affecting the others.
- **A known issue.** Relativity documents no recommendation for a distributed desktop application; this design is what NSerio verified works, and Relativity's own recommendation is tracked as a known issue of the release.

Verification: **Verification note — two things. The first decides whether the change can be made at all.** **SDK version support comes first**: where the version in use has no token-bearing settings member, the change cannot be made at this call site at all. **A token has a finite lifetime**: a settings object populated once and reused across long or repeated jobs holds a token that expires; populate it with a current token for each job.

OAuth2 Client: Create or confirm this OAuth2 Client in RelativityOne (NSerio's observation: the menu path is Home > Authentication > OAuth2 Client Manager, as Relativity's REST API authentication page names it). This tool never connects to RelativityOne and never creates or changes the OAuth2 Client; the settings below are guidance.
- Name: a descriptive name identifying the application it serves.
- Flow Grant Type: Code
- Redirect URLs: Relativity documentation: each URL includes the http or https protocol prefix. The exact callback URL is registered by the Relativity administrator. Server-side web applications, NSerio's recommendation: one exact HTTPS URL per environment, with no wildcards. Desktop and CLI applications: a plain-HTTP loopback URL, `http://127.0.0.1:<port>/<path>`, on a fixed port because the URL is registered in advance and the port cannot be chosen at runtime (verified by NSerio against a live instance; not documented by Relativity).
- Access Token Lifetime: 60 minutes, stated in minutes because the field is in minutes. Relativity documentation: tokens for this flow must have a short lifetime, and the recommended value matches the Identity Server default.
- Allow refresh tokens: NSerio's recommendation: leave it off unless the application must keep a user session active beyond the access token lifetime without asking the user to sign in again. If it is on, the refresh tokens are stored as secrets, per Secret Management (Part B of the Migration Documentation).
- Enabled: on.
<!-- generated:end VB-011 authorization-code -->

### VB-012 · NetworkCredentialOnHttpType

<a id="vb-012-migration"></a>

#### Client Credentials

<!-- generated:begin VB-012 client-credentials -->
**Confirming the flow.** When no signal in a file decides the flow, the tool assigns Client Credentials, the default, with this note: "Who supplies the credential at run time? If a person types it or signs in, the integration acts for that person: use Authorization Code. If it comes from configuration, settings, an environment variable or a secret store: use Client Credentials. No signal in this file showed which applies, so Client Credentials, the default, is recommended. Use Authorization Code instead if the integration acts on behalf of a signed-in user, or if your RelativityOne instance already has a Code-flow OAuth2 Client for this integration." A fixed-credential signal assigns it with this note: "A fixed-credential signal in this file points to Client Credentials, but how often that kind of signal is right has not been measured. Use Authorization Code instead if the integration acts on behalf of a signed-in user, or if your RelativityOne instance already has a Code-flow OAuth2 Client for this integration." See [Confirming the flow](#confirming-the-flow) and the "Which flow" paragraph of VB-012 in [Part A](part-a-vbnet.md#vb-012).

Source form: **Applies to:** both; the HTTP type differs (`HttpClientHandler` on both; `WebClient` and `HttpWebRequest` period form on .NET Framework).

```vbnet
Dim handler As New HttpClientHandler With {.Credentials = New NetworkCredential(userName, password)}
```

Under token authentication: **First, the destination:** if it is not RelativityOne, this finding is informational — that service's own token mechanism is the migration — and the rest of this template does not apply.

**Drop the `Credentials` assignment** from the handler, keeping its other settings, and **set the bearer header per request**:

```vbnet
Dim handler As New HttpClientHandler()
request.Headers.Authorization = New AuthenticationHeaderValue("Bearer", accessToken)
```

Credentials: **Where the credentials come from.** The destination decides whether the migration applies. If the destination is not RelativityOne, the migration is that service's own token mechanism, this finding is informational for this tool, and the reader stops here. If it is RelativityOne, the token comes from the token-acquisition part that follows.

**Where the token comes from.** Create an OAuth2 Client with Flow Grant Type set to Client Credentials (part 3 lists its settings), then send a form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=client_credentials`, `client_id`, `client_secret` and a `scope`. The token endpoint is **documented** for this grant. The token is then sent as `Authorization: Bearer <token>`, with `X-CSRF-Header: -` on REST and Kepler calls.

**.NET (both runtimes' `HttpClient`; `System.Text.Json` on .NET)**

```vbnet
Imports System.Net.Http
Imports System.Text.Json

Public Async Function AcquireAccessTokenAsync(host As String, clientId As String, clientSecret As String, scope As String) As Task(Of String)
	Dim accessToken As String
	Using client As New HttpClient()
		Dim form As New FormUrlEncodedContent(New Dictionary(Of String, String) From {
			{"grant_type", "client_credentials"},
			{"client_id", clientId},
			{"client_secret", clientSecret},
			{"scope", scope}})
		Using response As HttpResponseMessage = Await client.PostAsync(host & "/Relativity/Identity/connect/token", form)
			response.EnsureSuccessStatusCode()
			Using document As JsonDocument = JsonDocument.Parse(Await response.Content.ReadAsStringAsync())
				accessToken = document.RootElement.GetProperty("access_token").GetString()
			End Using
		End Using
	End Using
	Return accessToken
End Function
```

**.NET Framework 4.7.2** — the same call with the serializer the runtime ships (`System.Web.Extensions`); `HttpClient` is in `System.Net.Http`:

```vbnet
Imports System.Net.Http
Imports System.Web.Script.Serialization

Public Async Function AcquireAccessTokenAsync(host As String, clientId As String, clientSecret As String, scope As String) As Task(Of String)
	Dim accessToken As String
	Using client As New HttpClient()
		Dim form As New FormUrlEncodedContent(New Dictionary(Of String, String) From {
			{"grant_type", "client_credentials"},
			{"client_id", clientId},
			{"client_secret", clientSecret},
			{"scope", scope}})
		Using response As HttpResponseMessage = Await client.PostAsync(host & "/Relativity/Identity/connect/token", form)
			response.EnsureSuccessStatusCode()
			Dim body As String = Await response.Content.ReadAsStringAsync()
			Dim fields As Dictionary(Of String, Object) = New JavaScriptSerializer().Deserialize(Of Dictionary(Of String, Object))(body)
			accessToken = fields("access_token").ToString()
		End Using
	End Using
	Return accessToken
End Function
```

**The scope depends on what the integration needs, and no single scope is required.** Relativity's documentation shows two values for this grant: `SystemUserInfo` on the REST authentication page, the Usage Reports page and in the official Transfer SDK sample, and `UserInfoAccess` in the Kepler .NET samples. No page says which is correct, whether both work, or what either grants, and the OAuth2 Client's `Scopes` property is documented as one that cannot be set. Use the value the API you call documents, and confirm it.

**Cache the token, and re-acquire it on expiry — do not refresh.** Keep the token for its lifetime (part 3 recommends 60 minutes, Relativity's own wording) and request a new one with the client secret when it expires or a call is rejected for authentication. Client Credentials does not offer refresh tokens: **verified by NSerio against a live instance; not documented by Relativity** — the Allow Refresh Tokens control does not appear for this grant in the RelativityOne user interface. Relativity's own tables and text on refresh tokens conflict with each other, so re-acquisition with the secret is the advice that holds whichever way they are read.

**Long-running processes that use the Relativity SDK.** A `ServiceFactory` built once from a bearer token keeps that token after it expires. Rebuild the `ServiceFactory` and its proxies with a fresh token before expiry. This is the only advice consistent with the documentation: the package `Relativity.Kepler.Client.SDK` (documented) exposes a settable `Token` and an `HttpMessageHandler` overload but no token-provider overload, and no page documents renewing a token behind a factory.

**The client secret.** It is displayed once, at creation or reset. Store it as Secret Management (Part B of the Migration Documentation) describes; never in source, never in tracked configuration, which in a VB.NET estate includes `app.config`, `web.config` and `My.Settings`. A reset invalidates the previous secret immediately, so plan the cut-over before resetting it.

**Before deploying, confirm the calling identity.** Every call now runs as the OAuth2 Client's **Context User** — not as the user in the replaced credential, and for an import or a job as much as for a request. If that user has different workspace permissions or item-level security, the code returns fewer rows or `Unauthorized` in production with no change to its logic, and Relativity's audit attributes every call to that one account. Confirm the Context User's permissions match those of the previous identity. Relativity describes the Context User as usually a member of the Relativity Administrators group; the OAuth2 Client block (part 3) gives NSerio's recommendation, which differs.

**When the integration acts for a signed-in user.** Client Credentials is one service identity by construction. An integration that acts on behalf of a signed-in user is the Authorization Code case, and moving to it means a new OAuth2 Client, because the flow is fixed when a client is created. The finding's flow note says when to make that choice.

Verification: **Verification note — five things.** **Confirm the exchange was Basic**: `NetworkCredential` declares no scheme, and a handler carrying one serves integrated Windows authentication as readily as Basic; if the server negotiated an integrated scheme, this construct is not Basic Authentication. **The 401-challenge response is lost**: a bearer header is sent up front and answers nothing. **Redirects stop carrying the credential**: a manually set `Authorization` header is never re-applied; address the final URI or handle the redirect explicitly. **A token has a finite lifetime**: set the header per request, not on the client's default headers. **Confirm the endpoint in use is the RelativityOne REST API.**

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
<!-- generated:end VB-012 client-credentials -->

#### Authorization Code

<!-- generated:begin VB-012 authorization-code -->
**Confirming the flow.** An interactive-user signal in a file assigns Authorization Code, with this note: "An interactive-user signal in this file points to Authorization Code. Confirm the flow: use Client Credentials instead if the integration runs with credentials fixed for the integration and no signed-in user." Your user profile can also make Authorization Code the default; see Changing the default in [Confirming the flow](#confirming-the-flow). See [Confirming the flow](#confirming-the-flow) and the "Which flow" paragraph of VB-012 in [Part A](part-a-vbnet.md#vb-012).

Source form: **Applies to:** both; the HTTP type differs (`HttpClientHandler` on both; `WebClient` and `HttpWebRequest` period form on .NET Framework).

```vbnet
Dim handler As New HttpClientHandler With {.Credentials = New NetworkCredential(userName, password)}
```

Under token authentication: **First, the destination:** if it is not RelativityOne, this finding is informational — that service's own token mechanism is the migration — and the rest of this template does not apply.

**Drop the `Credentials` assignment** from the handler, keeping its other settings, and **set the bearer header per request**:

```vbnet
Dim handler As New HttpClientHandler()
request.Headers.Authorization = New AuthenticationHeaderValue("Bearer", accessToken)
```

Credentials: **Where the credentials come from.** The destination decides whether the migration applies. If the destination is not RelativityOne, the migration is that service's own token mechanism, this finding is informational for this tool, and the reader stops here. If it is RelativityOne, the token comes from the token-acquisition part that follows.

The text a finding assigned Authorization Code ("Code") receives after part 1. It has a common part and two variants, **server-side** and **desktop**. The bold lead-in of each element is its marker. Every fact Relativity does not document but NSerio verified carries the exact label **verified by NSerio against a live instance; not documented by Relativity**.

**Common to both variants**

**Sign in.** Replace the code that collected the user's credentials with a redirect: send the user's browser to the authorize endpoint `<host>/Relativity/Identity/connect/authorize`, which leads to the RelativityOne login page. The authorize endpoint is **verified by NSerio against a live instance; not documented by Relativity**. The request carries the standard authorization-code parameters — `client_id`, `response_type=code`, the registered `redirect_uri`, a `scope`, and a `state` value the application checks on return; these are standard OAuth, and Relativity documents none of them. No discovery document is relied on.

**Receive the code.** The browser returns to the Redirect URL registered on the OAuth2 Client, with the authorization code and the `state` value. Check the `state` before using the code. The Redirect URL must be exactly the one registered (the variants say how).

**Exchange the code.** Send a form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=authorization_code`, the `code`, the same `redirect_uri`, the `client_id` and the `client_secret`, plus the `code_verifier` when PKCE is used. The code is exchanged at that endpoint, which Relativity documents for the Client Credentials grant only; the exchange is **verified by NSerio against a live instance; not documented by Relativity**.

```vbnet
Dim form As New FormUrlEncodedContent(New Dictionary(Of String, String) From {
	{"grant_type", "authorization_code"},
	{"code", authorizationCode},
	{"redirect_uri", redirectUri},
	{"client_id", clientId},
	{"client_secret", clientSecret},
	{"code_verifier", codeVerifier}})
Using response As HttpResponseMessage = Await client.PostAsync(host & "/Relativity/Identity/connect/token", form)
	response.EnsureSuccessStatusCode()
End Using
```

**Use the token.** Send `Authorization: Bearer <access token>` — the change at the use site in part 1 — with `X-CSRF-Header: -` on REST and Kepler calls.

**On expiry.** With Allow Refresh Tokens enabled on the OAuth2 Client and PKCE used, the application holds a refresh token and, when the access token expires, sends a `grant_type=refresh_token` request with it and the client credentials in place of the code exchange — *inferred* from the standard OAuth shape, not tested. Without a refresh token the user signs in again. Relativity does not document a refresh token's lifetime, whether it rotates, how it is revoked, or whether a revocation endpoint exists, nor what disabling or deleting the client does to tokens already issued. A refresh token is a secret: store it as Secret Management (Part B of the Migration Documentation) describes, and on compromise disable the client and replace it. Enable refresh tokens only if the application must keep a session beyond one token lifetime without asking the user to sign in again.

**PKCE.** PKCE is required by Relativity's own documentation to obtain a refresh token, and not otherwise: a Code request that does not ask for a refresh token works without it, **verified by NSerio against a live instance; not documented by Relativity**. It is never a blanket requirement. NSerio recommends PKCE with the `S256` method in both variants (NSerio recommendation), and does not claim that Relativity accepts any particular method, which it does not document.

**Scope.** No scope set is prescribed for Code. The scope below works — `openid RelativityRequestOrigin UserInfo UserInfoAccess AuthenticationID RelativityWeb` — **verified by NSerio against a live instance; not documented by Relativity**, verified as working and not as minimal. `state` is sent as standard OAuth practice and `nonce` is not used; Relativity does not document whether it enforces either.

**The application stops collecting the password.** After this change the application never receives, stores or forwards the user's username and password: the user signs in on RelativityOne's own page, and the application holds only tokens. Remove the credential prompt, the form field and any storage of the password that the replaced code needed (in a VB.NET estate: the `InputBox`, the login form's password control, the Web Forms login control's handler).

**Single sign-on.** Sign-in happens on RelativityOne's own login page, so the instance's single sign-on and two-factor rules apply unchanged — *inferred* from where the sign-in happens, and not documented for clients that customers create.

**The client secret is required.** Code does not work without a client secret: tested and rejected, **verified by NSerio against a live instance; not documented by Relativity**, which confirms Relativity's own statement that the grant requires one. It is displayed once and is stored as Secret Management (Part B of the Migration Documentation) describes.

**Server-side variant.** For a web application whose code runs on a server — in VB.NET, an ASP.NET Web Forms page or a custom page whose code-behind sends the redirect (`Response.Redirect(authorizeUrl)`) and receives the code in a route of the application:

- **Redirect.** The application receives the code at one exact HTTPS URL per environment, a route of the application, with no wildcards (NSerio recommendation; Relativity's matching rules are not documented). The Relativity administrator registers the exact URL on the OAuth2 Client.
- **Secret and tokens.** The client secret stays on the server and never reaches the browser. The user's tokens are held server-side, keyed to the session, and never in the browser.
- **Exchange.** The server performs the code exchange itself, from the code-behind or a handler.

**Desktop variant.** For a desktop or command-line application run by a user on their own machine (WinForms, WPF, console):

- **The system browser.** Open the authorize URL in the user's default browser and not in a web view the application hosts, so the application never sees the password (NSerio recommendation).
- **A loopback redirect on a fixed port.** Listen on the loopback address only, on the registered port, for one request. The Redirect URL is `http://127.0.0.1:<port>/<path>`: a plain-HTTP loopback URL is accepted, **verified by NSerio against a live instance; not documented by Relativity**. The Relativity administrator registers that exact URL; because it is registered in advance, the port is fixed and not chosen at runtime.

```vbnet
Using listener As New HttpListener()
	listener.Prefixes.Add("http://127.0.0.1:<port>/<path>/")
	listener.Start()
	Process.Start(New ProcessStartInfo(authorizeUrl) With {.UseShellExecute = True})
	Dim context As HttpListenerContext = Await listener.GetContextAsync()
	Dim authorizationCode As String = context.Request.QueryString("code")
	Dim returnedState As String = context.Request.QueryString("state")
	context.Response.Close()
End Using
```

- **The secret is extractable.** Code requires a client secret (above), so a distributed desktop application has to embed one, and **a secret shipped inside a distributed binary can be extracted by anyone who holds the binary**. It protects nothing against the person running the application.
- **Compensating guidance** (NSerio recommendation, subject to your own security policy): set Trusted IPs where the users' outbound addresses are known and stable — they restrict where a token can be *requested*, and are checked when a token is issued against the client's ID and secret; use the recommended short Access Token Lifetime of 60 minutes; enable refresh tokens only when needed; and create one OAuth2 Client per distribution, so a leaked secret can be disabled without affecting the others.
- **A known issue.** Relativity documents no recommendation for a distributed desktop application; this design is what NSerio verified works, and Relativity's own recommendation is tracked as a known issue of the release.

Verification: **Verification note — five things.** **Confirm the exchange was Basic**: `NetworkCredential` declares no scheme, and a handler carrying one serves integrated Windows authentication as readily as Basic; if the server negotiated an integrated scheme, this construct is not Basic Authentication. **The 401-challenge response is lost**: a bearer header is sent up front and answers nothing. **Redirects stop carrying the credential**: a manually set `Authorization` header is never re-applied; address the final URI or handle the redirect explicitly. **A token has a finite lifetime**: set the header per request, not on the client's default headers. **Confirm the endpoint in use is the RelativityOne REST API.**

OAuth2 Client: Create or confirm this OAuth2 Client in RelativityOne (NSerio's observation: the menu path is Home > Authentication > OAuth2 Client Manager, as Relativity's REST API authentication page names it). This tool never connects to RelativityOne and never creates or changes the OAuth2 Client; the settings below are guidance.
- Name: a descriptive name identifying the application it serves.
- Flow Grant Type: Code
- Redirect URLs: Relativity documentation: each URL includes the http or https protocol prefix. The exact callback URL is registered by the Relativity administrator. Server-side web applications, NSerio's recommendation: one exact HTTPS URL per environment, with no wildcards. Desktop and CLI applications: a plain-HTTP loopback URL, `http://127.0.0.1:<port>/<path>`, on a fixed port because the URL is registered in advance and the port cannot be chosen at runtime (verified by NSerio against a live instance; not documented by Relativity).
- Access Token Lifetime: 60 minutes, stated in minutes because the field is in minutes. Relativity documentation: tokens for this flow must have a short lifetime, and the recommended value matches the Identity Server default.
- Allow refresh tokens: NSerio's recommendation: leave it off unless the application must keep a user session active beyond the access token lifetime without asking the user to sign in again. If it is on, the refresh tokens are stored as secrets, per Secret Management (Part B of the Migration Documentation).
- Enabled: on.
<!-- generated:end VB-012 authorization-code -->

### VB-013 · NetworkCredentialConstruction

<a id="vb-013-migration"></a>

#### Client Credentials

<!-- generated:begin VB-013 client-credentials -->
**Confirming the flow.** When no signal in a file decides the flow, the tool assigns Client Credentials, the default, with this note: "Who supplies the credential at run time? If a person types it or signs in, the integration acts for that person: use Authorization Code. If it comes from configuration, settings, an environment variable or a secret store: use Client Credentials. No signal in this file showed which applies, so Client Credentials, the default, is recommended. Use Authorization Code instead if the integration acts on behalf of a signed-in user, or if your RelativityOne instance already has a Code-flow OAuth2 Client for this integration." A fixed-credential signal assigns it with this note: "A fixed-credential signal in this file points to Client Credentials, but how often that kind of signal is right has not been measured. Use Authorization Code instead if the integration acts on behalf of a signed-in user, or if your RelativityOne instance already has a Code-flow OAuth2 Client for this integration." See [Confirming the flow](#confirming-the-flow) and the "Which flow" paragraph of VB-013 in [Part A](part-a-vbnet.md#vb-013).

Source form: **Applies to:** both.

```vbnet
Dim credential As New NetworkCredential(userName, password)
```

Under token authentication: **First, the destination:** if it is not RelativityOne, this finding is informational — that service's own token mechanism is the migration — and the rest of this template does not apply.

**Identify the consumer first** — the fix may be outside the reported file, because this finding reports a credential whose consumer the tool did not recognize in it. The form of the change depends on what consumes the credential:

- an `HttpClient` handler: remove the credential from the handler and set the `Authorization` header to `Bearer` and the token on each request message;
- `WebClient` or `HttpWebRequest`: remove the credential and set the `Authorization` header to `Bearer` and the token on each request;
- a SOAP proxy: the binding changes, and the service may not accept bearer at all;
- a platform factory that takes the credential to log a user in: the token-bearing form of that factory, where the SDK offers one;
- a mail, file-transfer, directory or proxy consumer: not a migration target for this tool.

No code form is given, because none is correct for all.

Credentials: **Where the credentials come from.** The destination decides whether the migration applies. If the destination is not RelativityOne, the migration is that service's own token mechanism, this finding is informational for this tool, and the reader stops here. If it is RelativityOne, the token comes from the token-acquisition part that follows.

**Where the token comes from.** Create an OAuth2 Client with Flow Grant Type set to Client Credentials (part 3 lists its settings), then send a form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=client_credentials`, `client_id`, `client_secret` and a `scope`. The token endpoint is **documented** for this grant. The token is then sent as `Authorization: Bearer <token>`, with `X-CSRF-Header: -` on REST and Kepler calls.

**.NET (both runtimes' `HttpClient`; `System.Text.Json` on .NET)**

```vbnet
Imports System.Net.Http
Imports System.Text.Json

Public Async Function AcquireAccessTokenAsync(host As String, clientId As String, clientSecret As String, scope As String) As Task(Of String)
	Dim accessToken As String
	Using client As New HttpClient()
		Dim form As New FormUrlEncodedContent(New Dictionary(Of String, String) From {
			{"grant_type", "client_credentials"},
			{"client_id", clientId},
			{"client_secret", clientSecret},
			{"scope", scope}})
		Using response As HttpResponseMessage = Await client.PostAsync(host & "/Relativity/Identity/connect/token", form)
			response.EnsureSuccessStatusCode()
			Using document As JsonDocument = JsonDocument.Parse(Await response.Content.ReadAsStringAsync())
				accessToken = document.RootElement.GetProperty("access_token").GetString()
			End Using
		End Using
	End Using
	Return accessToken
End Function
```

**.NET Framework 4.7.2** — the same call with the serializer the runtime ships (`System.Web.Extensions`); `HttpClient` is in `System.Net.Http`:

```vbnet
Imports System.Net.Http
Imports System.Web.Script.Serialization

Public Async Function AcquireAccessTokenAsync(host As String, clientId As String, clientSecret As String, scope As String) As Task(Of String)
	Dim accessToken As String
	Using client As New HttpClient()
		Dim form As New FormUrlEncodedContent(New Dictionary(Of String, String) From {
			{"grant_type", "client_credentials"},
			{"client_id", clientId},
			{"client_secret", clientSecret},
			{"scope", scope}})
		Using response As HttpResponseMessage = Await client.PostAsync(host & "/Relativity/Identity/connect/token", form)
			response.EnsureSuccessStatusCode()
			Dim body As String = Await response.Content.ReadAsStringAsync()
			Dim fields As Dictionary(Of String, Object) = New JavaScriptSerializer().Deserialize(Of Dictionary(Of String, Object))(body)
			accessToken = fields("access_token").ToString()
		End Using
	End Using
	Return accessToken
End Function
```

**The scope depends on what the integration needs, and no single scope is required.** Relativity's documentation shows two values for this grant: `SystemUserInfo` on the REST authentication page, the Usage Reports page and in the official Transfer SDK sample, and `UserInfoAccess` in the Kepler .NET samples. No page says which is correct, whether both work, or what either grants, and the OAuth2 Client's `Scopes` property is documented as one that cannot be set. Use the value the API you call documents, and confirm it.

**Cache the token, and re-acquire it on expiry — do not refresh.** Keep the token for its lifetime (part 3 recommends 60 minutes, Relativity's own wording) and request a new one with the client secret when it expires or a call is rejected for authentication. Client Credentials does not offer refresh tokens: **verified by NSerio against a live instance; not documented by Relativity** — the Allow Refresh Tokens control does not appear for this grant in the RelativityOne user interface. Relativity's own tables and text on refresh tokens conflict with each other, so re-acquisition with the secret is the advice that holds whichever way they are read.

**Long-running processes that use the Relativity SDK.** A `ServiceFactory` built once from a bearer token keeps that token after it expires. Rebuild the `ServiceFactory` and its proxies with a fresh token before expiry. This is the only advice consistent with the documentation: the package `Relativity.Kepler.Client.SDK` (documented) exposes a settable `Token` and an `HttpMessageHandler` overload but no token-provider overload, and no page documents renewing a token behind a factory.

**The client secret.** It is displayed once, at creation or reset. Store it as Secret Management (Part B of the Migration Documentation) describes; never in source, never in tracked configuration, which in a VB.NET estate includes `app.config`, `web.config` and `My.Settings`. A reset invalidates the previous secret immediately, so plan the cut-over before resetting it.

**Before deploying, confirm the calling identity.** Every call now runs as the OAuth2 Client's **Context User** — not as the user in the replaced credential, and for an import or a job as much as for a request. If that user has different workspace permissions or item-level security, the code returns fewer rows or `Unauthorized` in production with no change to its logic, and Relativity's audit attributes every call to that one account. Confirm the Context User's permissions match those of the previous identity. Relativity describes the Context User as usually a member of the Relativity Administrators group; the OAuth2 Client block (part 3) gives NSerio's recommendation, which differs.

**When the integration acts for a signed-in user.** Client Credentials is one service identity by construction. An integration that acts on behalf of a signed-in user is the Authorization Code case, and moving to it means a new OAuth2 Client, because the flow is fixed when a client is created. The finding's flow note says when to make that choice.

Verification: **Verification note — seven things.** **This finding is the least characterized of any template**: the consumer was not recognized, so neither the scheme nor the destination has been established; treat it as the location of a credential to trace. **Confirm the exchange was Basic.** **The 401-challenge response is lost** wherever the consumer answered a challenge with this credential. **Redirects stop carrying the credential.** **A SOAP service may not accept bearer at all.** **Confirm the endpoint in use is the RelativityOne REST API.** **A token has a finite lifetime**: a header must be set per request to be refreshable.

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
<!-- generated:end VB-013 client-credentials -->

#### Authorization Code

<!-- generated:begin VB-013 authorization-code -->
**Confirming the flow.** An interactive-user signal in a file assigns Authorization Code, with this note: "An interactive-user signal in this file points to Authorization Code. Confirm the flow: use Client Credentials instead if the integration runs with credentials fixed for the integration and no signed-in user." Your user profile can also make Authorization Code the default; see Changing the default in [Confirming the flow](#confirming-the-flow). See [Confirming the flow](#confirming-the-flow) and the "Which flow" paragraph of VB-013 in [Part A](part-a-vbnet.md#vb-013).

Source form: **Applies to:** both.

```vbnet
Dim credential As New NetworkCredential(userName, password)
```

Under token authentication: **First, the destination:** if it is not RelativityOne, this finding is informational — that service's own token mechanism is the migration — and the rest of this template does not apply.

**Identify the consumer first** — the fix may be outside the reported file, because this finding reports a credential whose consumer the tool did not recognize in it. The form of the change depends on what consumes the credential:

- an `HttpClient` handler: remove the credential from the handler and set the `Authorization` header to `Bearer` and the token on each request message;
- `WebClient` or `HttpWebRequest`: remove the credential and set the `Authorization` header to `Bearer` and the token on each request;
- a SOAP proxy: the binding changes, and the service may not accept bearer at all;
- a platform factory that takes the credential to log a user in: the token-bearing form of that factory, where the SDK offers one;
- a mail, file-transfer, directory or proxy consumer: not a migration target for this tool.

No code form is given, because none is correct for all.

Credentials: **Where the credentials come from.** The destination decides whether the migration applies. If the destination is not RelativityOne, the migration is that service's own token mechanism, this finding is informational for this tool, and the reader stops here. If it is RelativityOne, the token comes from the token-acquisition part that follows.

The text a finding assigned Authorization Code ("Code") receives after part 1. It has a common part and two variants, **server-side** and **desktop**. The bold lead-in of each element is its marker. Every fact Relativity does not document but NSerio verified carries the exact label **verified by NSerio against a live instance; not documented by Relativity**.

**Common to both variants**

**Sign in.** Replace the code that collected the user's credentials with a redirect: send the user's browser to the authorize endpoint `<host>/Relativity/Identity/connect/authorize`, which leads to the RelativityOne login page. The authorize endpoint is **verified by NSerio against a live instance; not documented by Relativity**. The request carries the standard authorization-code parameters — `client_id`, `response_type=code`, the registered `redirect_uri`, a `scope`, and a `state` value the application checks on return; these are standard OAuth, and Relativity documents none of them. No discovery document is relied on.

**Receive the code.** The browser returns to the Redirect URL registered on the OAuth2 Client, with the authorization code and the `state` value. Check the `state` before using the code. The Redirect URL must be exactly the one registered (the variants say how).

**Exchange the code.** Send a form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=authorization_code`, the `code`, the same `redirect_uri`, the `client_id` and the `client_secret`, plus the `code_verifier` when PKCE is used. The code is exchanged at that endpoint, which Relativity documents for the Client Credentials grant only; the exchange is **verified by NSerio against a live instance; not documented by Relativity**.

```vbnet
Dim form As New FormUrlEncodedContent(New Dictionary(Of String, String) From {
	{"grant_type", "authorization_code"},
	{"code", authorizationCode},
	{"redirect_uri", redirectUri},
	{"client_id", clientId},
	{"client_secret", clientSecret},
	{"code_verifier", codeVerifier}})
Using response As HttpResponseMessage = Await client.PostAsync(host & "/Relativity/Identity/connect/token", form)
	response.EnsureSuccessStatusCode()
End Using
```

**Use the token.** Send `Authorization: Bearer <access token>` — the change at the use site in part 1 — with `X-CSRF-Header: -` on REST and Kepler calls.

**On expiry.** With Allow Refresh Tokens enabled on the OAuth2 Client and PKCE used, the application holds a refresh token and, when the access token expires, sends a `grant_type=refresh_token` request with it and the client credentials in place of the code exchange — *inferred* from the standard OAuth shape, not tested. Without a refresh token the user signs in again. Relativity does not document a refresh token's lifetime, whether it rotates, how it is revoked, or whether a revocation endpoint exists, nor what disabling or deleting the client does to tokens already issued. A refresh token is a secret: store it as Secret Management (Part B of the Migration Documentation) describes, and on compromise disable the client and replace it. Enable refresh tokens only if the application must keep a session beyond one token lifetime without asking the user to sign in again.

**PKCE.** PKCE is required by Relativity's own documentation to obtain a refresh token, and not otherwise: a Code request that does not ask for a refresh token works without it, **verified by NSerio against a live instance; not documented by Relativity**. It is never a blanket requirement. NSerio recommends PKCE with the `S256` method in both variants (NSerio recommendation), and does not claim that Relativity accepts any particular method, which it does not document.

**Scope.** No scope set is prescribed for Code. The scope below works — `openid RelativityRequestOrigin UserInfo UserInfoAccess AuthenticationID RelativityWeb` — **verified by NSerio against a live instance; not documented by Relativity**, verified as working and not as minimal. `state` is sent as standard OAuth practice and `nonce` is not used; Relativity does not document whether it enforces either.

**The application stops collecting the password.** After this change the application never receives, stores or forwards the user's username and password: the user signs in on RelativityOne's own page, and the application holds only tokens. Remove the credential prompt, the form field and any storage of the password that the replaced code needed (in a VB.NET estate: the `InputBox`, the login form's password control, the Web Forms login control's handler).

**Single sign-on.** Sign-in happens on RelativityOne's own login page, so the instance's single sign-on and two-factor rules apply unchanged — *inferred* from where the sign-in happens, and not documented for clients that customers create.

**The client secret is required.** Code does not work without a client secret: tested and rejected, **verified by NSerio against a live instance; not documented by Relativity**, which confirms Relativity's own statement that the grant requires one. It is displayed once and is stored as Secret Management (Part B of the Migration Documentation) describes.

**Server-side variant.** For a web application whose code runs on a server — in VB.NET, an ASP.NET Web Forms page or a custom page whose code-behind sends the redirect (`Response.Redirect(authorizeUrl)`) and receives the code in a route of the application:

- **Redirect.** The application receives the code at one exact HTTPS URL per environment, a route of the application, with no wildcards (NSerio recommendation; Relativity's matching rules are not documented). The Relativity administrator registers the exact URL on the OAuth2 Client.
- **Secret and tokens.** The client secret stays on the server and never reaches the browser. The user's tokens are held server-side, keyed to the session, and never in the browser.
- **Exchange.** The server performs the code exchange itself, from the code-behind or a handler.

**Desktop variant.** For a desktop or command-line application run by a user on their own machine (WinForms, WPF, console):

- **The system browser.** Open the authorize URL in the user's default browser and not in a web view the application hosts, so the application never sees the password (NSerio recommendation).
- **A loopback redirect on a fixed port.** Listen on the loopback address only, on the registered port, for one request. The Redirect URL is `http://127.0.0.1:<port>/<path>`: a plain-HTTP loopback URL is accepted, **verified by NSerio against a live instance; not documented by Relativity**. The Relativity administrator registers that exact URL; because it is registered in advance, the port is fixed and not chosen at runtime.

```vbnet
Using listener As New HttpListener()
	listener.Prefixes.Add("http://127.0.0.1:<port>/<path>/")
	listener.Start()
	Process.Start(New ProcessStartInfo(authorizeUrl) With {.UseShellExecute = True})
	Dim context As HttpListenerContext = Await listener.GetContextAsync()
	Dim authorizationCode As String = context.Request.QueryString("code")
	Dim returnedState As String = context.Request.QueryString("state")
	context.Response.Close()
End Using
```

- **The secret is extractable.** Code requires a client secret (above), so a distributed desktop application has to embed one, and **a secret shipped inside a distributed binary can be extracted by anyone who holds the binary**. It protects nothing against the person running the application.
- **Compensating guidance** (NSerio recommendation, subject to your own security policy): set Trusted IPs where the users' outbound addresses are known and stable — they restrict where a token can be *requested*, and are checked when a token is issued against the client's ID and secret; use the recommended short Access Token Lifetime of 60 minutes; enable refresh tokens only when needed; and create one OAuth2 Client per distribution, so a leaked secret can be disabled without affecting the others.
- **A known issue.** Relativity documents no recommendation for a distributed desktop application; this design is what NSerio verified works, and Relativity's own recommendation is tracked as a known issue of the release.

Verification: **Verification note — seven things.** **This finding is the least characterized of any template**: the consumer was not recognized, so neither the scheme nor the destination has been established; treat it as the location of a credential to trace. **Confirm the exchange was Basic.** **The 401-challenge response is lost** wherever the consumer answered a challenge with this credential. **Redirects stop carrying the credential.** **A SOAP service may not accept bearer at all.** **Confirm the endpoint in use is the RelativityOne REST API.** **A token has a finite lifetime**: a header must be set per request to be refreshable.

OAuth2 Client: Create or confirm this OAuth2 Client in RelativityOne (NSerio's observation: the menu path is Home > Authentication > OAuth2 Client Manager, as Relativity's REST API authentication page names it). This tool never connects to RelativityOne and never creates or changes the OAuth2 Client; the settings below are guidance.
- Name: a descriptive name identifying the application it serves.
- Flow Grant Type: Code
- Redirect URLs: Relativity documentation: each URL includes the http or https protocol prefix. The exact callback URL is registered by the Relativity administrator. Server-side web applications, NSerio's recommendation: one exact HTTPS URL per environment, with no wildcards. Desktop and CLI applications: a plain-HTTP loopback URL, `http://127.0.0.1:<port>/<path>`, on a fixed port because the URL is registered in advance and the port cannot be chosen at runtime (verified by NSerio against a live instance; not documented by Relativity).
- Access Token Lifetime: 60 minutes, stated in minutes because the field is in minutes. Relativity documentation: tokens for this flow must have a short lifetime, and the recommended value matches the Identity Server default.
- Allow refresh tokens: NSerio's recommendation: leave it off unless the application must keep a user session active beyond the access token lifetime without asking the user to sign in again. If it is on, the refresh tokens are stored as secrets, per Secret Management (Part B of the Migration Documentation).
- Enabled: on.
<!-- generated:end VB-013 authorization-code -->

### VB-014 · Base64UserPasswordPair

<a id="vb-014-migration"></a>

#### Client Credentials

<!-- generated:begin VB-014 client-credentials -->
**Confirming the flow.** When no signal in a file decides the flow, the tool assigns Client Credentials, the default, with this note: "Who supplies the credential at run time? If a person types it or signs in, the integration acts for that person: use Authorization Code. If it comes from configuration, settings, an environment variable or a secret store: use Client Credentials. No signal in this file showed which applies, so Client Credentials, the default, is recommended. Use Authorization Code instead if the integration acts on behalf of a signed-in user, or if your RelativityOne instance already has a Code-flow OAuth2 Client for this integration." A fixed-credential signal assigns it with this note: "A fixed-credential signal in this file points to Client Credentials, but how often that kind of signal is right has not been measured. Use Authorization Code instead if the integration acts on behalf of a signed-in user, or if your RelativityOne instance already has a Code-flow OAuth2 Client for this integration." See [Confirming the flow](#confirming-the-flow) and the "Which flow" paragraph of VB-014 in [Part A](part-a-vbnet.md#vb-014).

Source form: **Applies to:** both.

```vbnet
Dim pair As String = userName & ":" & password
Dim encoded As String = Convert.ToBase64String(Encoding.ASCII.GetBytes(pair))
```

Under token authentication: **First, the destination:** if it is not RelativityOne, this finding is informational — that service's own token mechanism is the migration — and the rest of this template does not apply.

**Only if this encoding feeds an `Authorization` header.** Base64 over a colon-separated pair is not always a credential: the same shape encodes other data. Where it does, **delete the encoding**: bearer carries the token verbatim, with no Base64 of a credential pair, and the header that received `encoded` receives the token, set per request:

```vbnet
request.Headers.Authorization = New AuthenticationHeaderValue("Bearer", accessToken)
```

Credentials: **Where the credentials come from.** The destination decides whether the migration applies. If the destination is not RelativityOne, the migration is that service's own token mechanism, this finding is informational for this tool, and the reader stops here. If it is RelativityOne, the token comes from the token-acquisition part that follows.

**Where the token comes from.** Create an OAuth2 Client with Flow Grant Type set to Client Credentials (part 3 lists its settings), then send a form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=client_credentials`, `client_id`, `client_secret` and a `scope`. The token endpoint is **documented** for this grant. The token is then sent as `Authorization: Bearer <token>`, with `X-CSRF-Header: -` on REST and Kepler calls.

**.NET (both runtimes' `HttpClient`; `System.Text.Json` on .NET)**

```vbnet
Imports System.Net.Http
Imports System.Text.Json

Public Async Function AcquireAccessTokenAsync(host As String, clientId As String, clientSecret As String, scope As String) As Task(Of String)
	Dim accessToken As String
	Using client As New HttpClient()
		Dim form As New FormUrlEncodedContent(New Dictionary(Of String, String) From {
			{"grant_type", "client_credentials"},
			{"client_id", clientId},
			{"client_secret", clientSecret},
			{"scope", scope}})
		Using response As HttpResponseMessage = Await client.PostAsync(host & "/Relativity/Identity/connect/token", form)
			response.EnsureSuccessStatusCode()
			Using document As JsonDocument = JsonDocument.Parse(Await response.Content.ReadAsStringAsync())
				accessToken = document.RootElement.GetProperty("access_token").GetString()
			End Using
		End Using
	End Using
	Return accessToken
End Function
```

**.NET Framework 4.7.2** — the same call with the serializer the runtime ships (`System.Web.Extensions`); `HttpClient` is in `System.Net.Http`:

```vbnet
Imports System.Net.Http
Imports System.Web.Script.Serialization

Public Async Function AcquireAccessTokenAsync(host As String, clientId As String, clientSecret As String, scope As String) As Task(Of String)
	Dim accessToken As String
	Using client As New HttpClient()
		Dim form As New FormUrlEncodedContent(New Dictionary(Of String, String) From {
			{"grant_type", "client_credentials"},
			{"client_id", clientId},
			{"client_secret", clientSecret},
			{"scope", scope}})
		Using response As HttpResponseMessage = Await client.PostAsync(host & "/Relativity/Identity/connect/token", form)
			response.EnsureSuccessStatusCode()
			Dim body As String = Await response.Content.ReadAsStringAsync()
			Dim fields As Dictionary(Of String, Object) = New JavaScriptSerializer().Deserialize(Of Dictionary(Of String, Object))(body)
			accessToken = fields("access_token").ToString()
		End Using
	End Using
	Return accessToken
End Function
```

**The scope depends on what the integration needs, and no single scope is required.** Relativity's documentation shows two values for this grant: `SystemUserInfo` on the REST authentication page, the Usage Reports page and in the official Transfer SDK sample, and `UserInfoAccess` in the Kepler .NET samples. No page says which is correct, whether both work, or what either grants, and the OAuth2 Client's `Scopes` property is documented as one that cannot be set. Use the value the API you call documents, and confirm it.

**Cache the token, and re-acquire it on expiry — do not refresh.** Keep the token for its lifetime (part 3 recommends 60 minutes, Relativity's own wording) and request a new one with the client secret when it expires or a call is rejected for authentication. Client Credentials does not offer refresh tokens: **verified by NSerio against a live instance; not documented by Relativity** — the Allow Refresh Tokens control does not appear for this grant in the RelativityOne user interface. Relativity's own tables and text on refresh tokens conflict with each other, so re-acquisition with the secret is the advice that holds whichever way they are read.

**Long-running processes that use the Relativity SDK.** A `ServiceFactory` built once from a bearer token keeps that token after it expires. Rebuild the `ServiceFactory` and its proxies with a fresh token before expiry. This is the only advice consistent with the documentation: the package `Relativity.Kepler.Client.SDK` (documented) exposes a settable `Token` and an `HttpMessageHandler` overload but no token-provider overload, and no page documents renewing a token behind a factory.

**The client secret.** It is displayed once, at creation or reset. Store it as Secret Management (Part B of the Migration Documentation) describes; never in source, never in tracked configuration, which in a VB.NET estate includes `app.config`, `web.config` and `My.Settings`. A reset invalidates the previous secret immediately, so plan the cut-over before resetting it.

**Before deploying, confirm the calling identity.** Every call now runs as the OAuth2 Client's **Context User** — not as the user in the replaced credential, and for an import or a job as much as for a request. If that user has different workspace permissions or item-level security, the code returns fewer rows or `Unauthorized` in production with no change to its logic, and Relativity's audit attributes every call to that one account. Confirm the Context User's permissions match those of the previous identity. Relativity describes the Context User as usually a member of the Relativity Administrators group; the OAuth2 Client block (part 3) gives NSerio's recommendation, which differs.

**When the integration acts for a signed-in user.** Client Credentials is one service identity by construction. An integration that acts on behalf of a signed-in user is the Authorization Code case, and moving to it means a new OAuth2 Client, because the flow is fixed when a client is created. The finding's flow note says when to make that choice.

Verification: **Verification note — four things.** **Confirm the pair is a credential sent outward**: a service that *checks* Basic from its own callers is inbound authentication, which has nothing to migrate. **The header write may be a separate finding** at a second location, in the same migration. **A token has a finite lifetime, and a header set once on a long-lived client cannot be refreshed**: set it per request. **Confirm the endpoint in use is the RelativityOne REST API.**

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
<!-- generated:end VB-014 client-credentials -->

#### Authorization Code

<!-- generated:begin VB-014 authorization-code -->
**Confirming the flow.** An interactive-user signal in a file assigns Authorization Code, with this note: "An interactive-user signal in this file points to Authorization Code. Confirm the flow: use Client Credentials instead if the integration runs with credentials fixed for the integration and no signed-in user." Your user profile can also make Authorization Code the default; see Changing the default in [Confirming the flow](#confirming-the-flow). See [Confirming the flow](#confirming-the-flow) and the "Which flow" paragraph of VB-014 in [Part A](part-a-vbnet.md#vb-014).

Source form: **Applies to:** both.

```vbnet
Dim pair As String = userName & ":" & password
Dim encoded As String = Convert.ToBase64String(Encoding.ASCII.GetBytes(pair))
```

Under token authentication: **First, the destination:** if it is not RelativityOne, this finding is informational — that service's own token mechanism is the migration — and the rest of this template does not apply.

**Only if this encoding feeds an `Authorization` header.** Base64 over a colon-separated pair is not always a credential: the same shape encodes other data. Where it does, **delete the encoding**: bearer carries the token verbatim, with no Base64 of a credential pair, and the header that received `encoded` receives the token, set per request:

```vbnet
request.Headers.Authorization = New AuthenticationHeaderValue("Bearer", accessToken)
```

Credentials: **Where the credentials come from.** The destination decides whether the migration applies. If the destination is not RelativityOne, the migration is that service's own token mechanism, this finding is informational for this tool, and the reader stops here. If it is RelativityOne, the token comes from the token-acquisition part that follows.

The text a finding assigned Authorization Code ("Code") receives after part 1. It has a common part and two variants, **server-side** and **desktop**. The bold lead-in of each element is its marker. Every fact Relativity does not document but NSerio verified carries the exact label **verified by NSerio against a live instance; not documented by Relativity**.

**Common to both variants**

**Sign in.** Replace the code that collected the user's credentials with a redirect: send the user's browser to the authorize endpoint `<host>/Relativity/Identity/connect/authorize`, which leads to the RelativityOne login page. The authorize endpoint is **verified by NSerio against a live instance; not documented by Relativity**. The request carries the standard authorization-code parameters — `client_id`, `response_type=code`, the registered `redirect_uri`, a `scope`, and a `state` value the application checks on return; these are standard OAuth, and Relativity documents none of them. No discovery document is relied on.

**Receive the code.** The browser returns to the Redirect URL registered on the OAuth2 Client, with the authorization code and the `state` value. Check the `state` before using the code. The Redirect URL must be exactly the one registered (the variants say how).

**Exchange the code.** Send a form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=authorization_code`, the `code`, the same `redirect_uri`, the `client_id` and the `client_secret`, plus the `code_verifier` when PKCE is used. The code is exchanged at that endpoint, which Relativity documents for the Client Credentials grant only; the exchange is **verified by NSerio against a live instance; not documented by Relativity**.

```vbnet
Dim form As New FormUrlEncodedContent(New Dictionary(Of String, String) From {
	{"grant_type", "authorization_code"},
	{"code", authorizationCode},
	{"redirect_uri", redirectUri},
	{"client_id", clientId},
	{"client_secret", clientSecret},
	{"code_verifier", codeVerifier}})
Using response As HttpResponseMessage = Await client.PostAsync(host & "/Relativity/Identity/connect/token", form)
	response.EnsureSuccessStatusCode()
End Using
```

**Use the token.** Send `Authorization: Bearer <access token>` — the change at the use site in part 1 — with `X-CSRF-Header: -` on REST and Kepler calls.

**On expiry.** With Allow Refresh Tokens enabled on the OAuth2 Client and PKCE used, the application holds a refresh token and, when the access token expires, sends a `grant_type=refresh_token` request with it and the client credentials in place of the code exchange — *inferred* from the standard OAuth shape, not tested. Without a refresh token the user signs in again. Relativity does not document a refresh token's lifetime, whether it rotates, how it is revoked, or whether a revocation endpoint exists, nor what disabling or deleting the client does to tokens already issued. A refresh token is a secret: store it as Secret Management (Part B of the Migration Documentation) describes, and on compromise disable the client and replace it. Enable refresh tokens only if the application must keep a session beyond one token lifetime without asking the user to sign in again.

**PKCE.** PKCE is required by Relativity's own documentation to obtain a refresh token, and not otherwise: a Code request that does not ask for a refresh token works without it, **verified by NSerio against a live instance; not documented by Relativity**. It is never a blanket requirement. NSerio recommends PKCE with the `S256` method in both variants (NSerio recommendation), and does not claim that Relativity accepts any particular method, which it does not document.

**Scope.** No scope set is prescribed for Code. The scope below works — `openid RelativityRequestOrigin UserInfo UserInfoAccess AuthenticationID RelativityWeb` — **verified by NSerio against a live instance; not documented by Relativity**, verified as working and not as minimal. `state` is sent as standard OAuth practice and `nonce` is not used; Relativity does not document whether it enforces either.

**The application stops collecting the password.** After this change the application never receives, stores or forwards the user's username and password: the user signs in on RelativityOne's own page, and the application holds only tokens. Remove the credential prompt, the form field and any storage of the password that the replaced code needed (in a VB.NET estate: the `InputBox`, the login form's password control, the Web Forms login control's handler).

**Single sign-on.** Sign-in happens on RelativityOne's own login page, so the instance's single sign-on and two-factor rules apply unchanged — *inferred* from where the sign-in happens, and not documented for clients that customers create.

**The client secret is required.** Code does not work without a client secret: tested and rejected, **verified by NSerio against a live instance; not documented by Relativity**, which confirms Relativity's own statement that the grant requires one. It is displayed once and is stored as Secret Management (Part B of the Migration Documentation) describes.

**Server-side variant.** For a web application whose code runs on a server — in VB.NET, an ASP.NET Web Forms page or a custom page whose code-behind sends the redirect (`Response.Redirect(authorizeUrl)`) and receives the code in a route of the application:

- **Redirect.** The application receives the code at one exact HTTPS URL per environment, a route of the application, with no wildcards (NSerio recommendation; Relativity's matching rules are not documented). The Relativity administrator registers the exact URL on the OAuth2 Client.
- **Secret and tokens.** The client secret stays on the server and never reaches the browser. The user's tokens are held server-side, keyed to the session, and never in the browser.
- **Exchange.** The server performs the code exchange itself, from the code-behind or a handler.

**Desktop variant.** For a desktop or command-line application run by a user on their own machine (WinForms, WPF, console):

- **The system browser.** Open the authorize URL in the user's default browser and not in a web view the application hosts, so the application never sees the password (NSerio recommendation).
- **A loopback redirect on a fixed port.** Listen on the loopback address only, on the registered port, for one request. The Redirect URL is `http://127.0.0.1:<port>/<path>`: a plain-HTTP loopback URL is accepted, **verified by NSerio against a live instance; not documented by Relativity**. The Relativity administrator registers that exact URL; because it is registered in advance, the port is fixed and not chosen at runtime.

```vbnet
Using listener As New HttpListener()
	listener.Prefixes.Add("http://127.0.0.1:<port>/<path>/")
	listener.Start()
	Process.Start(New ProcessStartInfo(authorizeUrl) With {.UseShellExecute = True})
	Dim context As HttpListenerContext = Await listener.GetContextAsync()
	Dim authorizationCode As String = context.Request.QueryString("code")
	Dim returnedState As String = context.Request.QueryString("state")
	context.Response.Close()
End Using
```

- **The secret is extractable.** Code requires a client secret (above), so a distributed desktop application has to embed one, and **a secret shipped inside a distributed binary can be extracted by anyone who holds the binary**. It protects nothing against the person running the application.
- **Compensating guidance** (NSerio recommendation, subject to your own security policy): set Trusted IPs where the users' outbound addresses are known and stable — they restrict where a token can be *requested*, and are checked when a token is issued against the client's ID and secret; use the recommended short Access Token Lifetime of 60 minutes; enable refresh tokens only when needed; and create one OAuth2 Client per distribution, so a leaked secret can be disabled without affecting the others.
- **A known issue.** Relativity documents no recommendation for a distributed desktop application; this design is what NSerio verified works, and Relativity's own recommendation is tracked as a known issue of the release.

Verification: **Verification note — four things.** **Confirm the pair is a credential sent outward**: a service that *checks* Basic from its own callers is inbound authentication, which has nothing to migrate. **The header write may be a separate finding** at a second location, in the same migration. **A token has a finite lifetime, and a header set once on a long-lived client cannot be refreshed**: set it per request. **Confirm the endpoint in use is the RelativityOne REST API.**

OAuth2 Client: Create or confirm this OAuth2 Client in RelativityOne (NSerio's observation: the menu path is Home > Authentication > OAuth2 Client Manager, as Relativity's REST API authentication page names it). This tool never connects to RelativityOne and never creates or changes the OAuth2 Client; the settings below are guidance.
- Name: a descriptive name identifying the application it serves.
- Flow Grant Type: Code
- Redirect URLs: Relativity documentation: each URL includes the http or https protocol prefix. The exact callback URL is registered by the Relativity administrator. Server-side web applications, NSerio's recommendation: one exact HTTPS URL per environment, with no wildcards. Desktop and CLI applications: a plain-HTTP loopback URL, `http://127.0.0.1:<port>/<path>`, on a fixed port because the URL is registered in advance and the port cannot be chosen at runtime (verified by NSerio against a live instance; not documented by Relativity).
- Access Token Lifetime: 60 minutes, stated in minutes because the field is in minutes. Relativity documentation: tokens for this flow must have a short lifetime, and the recommended value matches the Identity Server default.
- Allow refresh tokens: NSerio's recommendation: leave it off unless the application must keep a user session active beyond the access token lifetime without asking the user to sign in again. If it is on, the refresh tokens are stored as secrets, per Secret Management (Part B of the Migration Documentation).
- Enabled: on.
<!-- generated:end VB-014 authorization-code -->

### VB-015 · TokenCarriedUnderBasicScheme

<a id="vb-015-migration"></a>

#### Client Credentials

<!-- generated:begin VB-015 client-credentials -->
**Confirming the flow.** When no signal in a file decides the flow, the tool assigns Client Credentials, the default, with this note: "Who supplies the credential at run time? If a person types it or signs in, the integration acts for that person: use Authorization Code. If it comes from configuration, settings, an environment variable or a secret store: use Client Credentials. No signal in this file showed which applies, so Client Credentials, the default, is recommended. Use Authorization Code instead if the integration acts on behalf of a signed-in user, or if your RelativityOne instance already has a Code-flow OAuth2 Client for this integration." A fixed-credential signal assigns it with this note: "A fixed-credential signal in this file points to Client Credentials, but how often that kind of signal is right has not been measured. Use Authorization Code instead if the integration acts on behalf of a signed-in user, or if your RelativityOne instance already has a Code-flow OAuth2 Client for this integration." See [Confirming the flow](#confirming-the-flow) and the "Which flow" paragraph of VB-015 in [Part A](part-a-vbnet.md#vb-015).

Source form: **Applies to:** both.

```vbnet
Dim pair As String = "" & ":" & accessToken
Dim encoded As String = Convert.ToBase64String(Encoding.ASCII.GetBytes(pair))
```

Under token authentication: **This change applies to any destination that accepts `Bearer`**, RelativityOne or not: no credential is exchanged, so only the token's provenance differs when the destination is not RelativityOne.

**Delete the encoding and change the prefix.** The token already in hand is carried verbatim:

```text
Authorization: Basic <base64 of ":" + token>      ->      Authorization: Bearer <token>
```

```vbnet
request.Headers.Authorization = New AuthenticationHeaderValue("Bearer", accessToken)
```

No credential is exchanged and no client is registered — this is the cheapest remediation of any template.

Credentials: **Where the credentials come from.** The prefix change applies to any destination that accepts `Bearer`. If the destination is RelativityOne, the token that belongs in the header is obtained as the token-acquisition part that follows describes. If it is not RelativityOne, the token's provenance is that service's and does not change: the token already in hand is the one that moves to the `Bearer` prefix.

**Where the token comes from.** Create an OAuth2 Client with Flow Grant Type set to Client Credentials (part 3 lists its settings), then send a form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=client_credentials`, `client_id`, `client_secret` and a `scope`. The token endpoint is **documented** for this grant. The token is then sent as `Authorization: Bearer <token>`, with `X-CSRF-Header: -` on REST and Kepler calls.

**.NET (both runtimes' `HttpClient`; `System.Text.Json` on .NET)**

```vbnet
Imports System.Net.Http
Imports System.Text.Json

Public Async Function AcquireAccessTokenAsync(host As String, clientId As String, clientSecret As String, scope As String) As Task(Of String)
	Dim accessToken As String
	Using client As New HttpClient()
		Dim form As New FormUrlEncodedContent(New Dictionary(Of String, String) From {
			{"grant_type", "client_credentials"},
			{"client_id", clientId},
			{"client_secret", clientSecret},
			{"scope", scope}})
		Using response As HttpResponseMessage = Await client.PostAsync(host & "/Relativity/Identity/connect/token", form)
			response.EnsureSuccessStatusCode()
			Using document As JsonDocument = JsonDocument.Parse(Await response.Content.ReadAsStringAsync())
				accessToken = document.RootElement.GetProperty("access_token").GetString()
			End Using
		End Using
	End Using
	Return accessToken
End Function
```

**.NET Framework 4.7.2** — the same call with the serializer the runtime ships (`System.Web.Extensions`); `HttpClient` is in `System.Net.Http`:

```vbnet
Imports System.Net.Http
Imports System.Web.Script.Serialization

Public Async Function AcquireAccessTokenAsync(host As String, clientId As String, clientSecret As String, scope As String) As Task(Of String)
	Dim accessToken As String
	Using client As New HttpClient()
		Dim form As New FormUrlEncodedContent(New Dictionary(Of String, String) From {
			{"grant_type", "client_credentials"},
			{"client_id", clientId},
			{"client_secret", clientSecret},
			{"scope", scope}})
		Using response As HttpResponseMessage = Await client.PostAsync(host & "/Relativity/Identity/connect/token", form)
			response.EnsureSuccessStatusCode()
			Dim body As String = Await response.Content.ReadAsStringAsync()
			Dim fields As Dictionary(Of String, Object) = New JavaScriptSerializer().Deserialize(Of Dictionary(Of String, Object))(body)
			accessToken = fields("access_token").ToString()
		End Using
	End Using
	Return accessToken
End Function
```

**The scope depends on what the integration needs, and no single scope is required.** Relativity's documentation shows two values for this grant: `SystemUserInfo` on the REST authentication page, the Usage Reports page and in the official Transfer SDK sample, and `UserInfoAccess` in the Kepler .NET samples. No page says which is correct, whether both work, or what either grants, and the OAuth2 Client's `Scopes` property is documented as one that cannot be set. Use the value the API you call documents, and confirm it.

**Cache the token, and re-acquire it on expiry — do not refresh.** Keep the token for its lifetime (part 3 recommends 60 minutes, Relativity's own wording) and request a new one with the client secret when it expires or a call is rejected for authentication. Client Credentials does not offer refresh tokens: **verified by NSerio against a live instance; not documented by Relativity** — the Allow Refresh Tokens control does not appear for this grant in the RelativityOne user interface. Relativity's own tables and text on refresh tokens conflict with each other, so re-acquisition with the secret is the advice that holds whichever way they are read.

**Long-running processes that use the Relativity SDK.** A `ServiceFactory` built once from a bearer token keeps that token after it expires. Rebuild the `ServiceFactory` and its proxies with a fresh token before expiry. This is the only advice consistent with the documentation: the package `Relativity.Kepler.Client.SDK` (documented) exposes a settable `Token` and an `HttpMessageHandler` overload but no token-provider overload, and no page documents renewing a token behind a factory.

**The client secret.** It is displayed once, at creation or reset. Store it as Secret Management (Part B of the Migration Documentation) describes; never in source, never in tracked configuration, which in a VB.NET estate includes `app.config`, `web.config` and `My.Settings`. A reset invalidates the previous secret immediately, so plan the cut-over before resetting it.

**Before deploying, confirm the calling identity.** Every call now runs as the OAuth2 Client's **Context User** — not as the user in the replaced credential, and for an import or a job as much as for a request. If that user has different workspace permissions or item-level security, the code returns fewer rows or `Unauthorized` in production with no change to its logic, and Relativity's audit attributes every call to that one account. Confirm the Context User's permissions match those of the previous identity. Relativity describes the Context User as usually a member of the Relativity Administrators group; the OAuth2 Client block (part 3) gives NSerio's recommendation, which differs.

**When the integration acts for a signed-in user.** Client Credentials is one service identity by construction. An integration that acts on behalf of a signed-in user is the Authorization Code case, and moving to it means a new OAuth2 Client, because the flow is fixed when a client is created. The finding's flow note says when to make that choice.

Verification: **Verification note — two things.** **Verify the destination accepts `Bearer` before changing the prefix**: some services accept only Basic. **A header set once on a long-lived client cannot be refreshed**: set it per request with a current token.

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
<!-- generated:end VB-015 client-credentials -->

#### Authorization Code

<!-- generated:begin VB-015 authorization-code -->
**Confirming the flow.** An interactive-user signal in a file assigns Authorization Code, with this note: "An interactive-user signal in this file points to Authorization Code. Confirm the flow: use Client Credentials instead if the integration runs with credentials fixed for the integration and no signed-in user." Your user profile can also make Authorization Code the default; see Changing the default in [Confirming the flow](#confirming-the-flow). See [Confirming the flow](#confirming-the-flow) and the "Which flow" paragraph of VB-015 in [Part A](part-a-vbnet.md#vb-015).

Source form: **Applies to:** both.

```vbnet
Dim pair As String = "" & ":" & accessToken
Dim encoded As String = Convert.ToBase64String(Encoding.ASCII.GetBytes(pair))
```

Under token authentication: **This change applies to any destination that accepts `Bearer`**, RelativityOne or not: no credential is exchanged, so only the token's provenance differs when the destination is not RelativityOne.

**Delete the encoding and change the prefix.** The token already in hand is carried verbatim:

```text
Authorization: Basic <base64 of ":" + token>      ->      Authorization: Bearer <token>
```

```vbnet
request.Headers.Authorization = New AuthenticationHeaderValue("Bearer", accessToken)
```

No credential is exchanged and no client is registered — this is the cheapest remediation of any template.

Credentials: **Where the credentials come from.** The prefix change applies to any destination that accepts `Bearer`. If the destination is RelativityOne, the token that belongs in the header is obtained as the token-acquisition part that follows describes. If it is not RelativityOne, the token's provenance is that service's and does not change: the token already in hand is the one that moves to the `Bearer` prefix.

The text a finding assigned Authorization Code ("Code") receives after part 1. It has a common part and two variants, **server-side** and **desktop**. The bold lead-in of each element is its marker. Every fact Relativity does not document but NSerio verified carries the exact label **verified by NSerio against a live instance; not documented by Relativity**.

**Common to both variants**

**Sign in.** Replace the code that collected the user's credentials with a redirect: send the user's browser to the authorize endpoint `<host>/Relativity/Identity/connect/authorize`, which leads to the RelativityOne login page. The authorize endpoint is **verified by NSerio against a live instance; not documented by Relativity**. The request carries the standard authorization-code parameters — `client_id`, `response_type=code`, the registered `redirect_uri`, a `scope`, and a `state` value the application checks on return; these are standard OAuth, and Relativity documents none of them. No discovery document is relied on.

**Receive the code.** The browser returns to the Redirect URL registered on the OAuth2 Client, with the authorization code and the `state` value. Check the `state` before using the code. The Redirect URL must be exactly the one registered (the variants say how).

**Exchange the code.** Send a form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=authorization_code`, the `code`, the same `redirect_uri`, the `client_id` and the `client_secret`, plus the `code_verifier` when PKCE is used. The code is exchanged at that endpoint, which Relativity documents for the Client Credentials grant only; the exchange is **verified by NSerio against a live instance; not documented by Relativity**.

```vbnet
Dim form As New FormUrlEncodedContent(New Dictionary(Of String, String) From {
	{"grant_type", "authorization_code"},
	{"code", authorizationCode},
	{"redirect_uri", redirectUri},
	{"client_id", clientId},
	{"client_secret", clientSecret},
	{"code_verifier", codeVerifier}})
Using response As HttpResponseMessage = Await client.PostAsync(host & "/Relativity/Identity/connect/token", form)
	response.EnsureSuccessStatusCode()
End Using
```

**Use the token.** Send `Authorization: Bearer <access token>` — the change at the use site in part 1 — with `X-CSRF-Header: -` on REST and Kepler calls.

**On expiry.** With Allow Refresh Tokens enabled on the OAuth2 Client and PKCE used, the application holds a refresh token and, when the access token expires, sends a `grant_type=refresh_token` request with it and the client credentials in place of the code exchange — *inferred* from the standard OAuth shape, not tested. Without a refresh token the user signs in again. Relativity does not document a refresh token's lifetime, whether it rotates, how it is revoked, or whether a revocation endpoint exists, nor what disabling or deleting the client does to tokens already issued. A refresh token is a secret: store it as Secret Management (Part B of the Migration Documentation) describes, and on compromise disable the client and replace it. Enable refresh tokens only if the application must keep a session beyond one token lifetime without asking the user to sign in again.

**PKCE.** PKCE is required by Relativity's own documentation to obtain a refresh token, and not otherwise: a Code request that does not ask for a refresh token works without it, **verified by NSerio against a live instance; not documented by Relativity**. It is never a blanket requirement. NSerio recommends PKCE with the `S256` method in both variants (NSerio recommendation), and does not claim that Relativity accepts any particular method, which it does not document.

**Scope.** No scope set is prescribed for Code. The scope below works — `openid RelativityRequestOrigin UserInfo UserInfoAccess AuthenticationID RelativityWeb` — **verified by NSerio against a live instance; not documented by Relativity**, verified as working and not as minimal. `state` is sent as standard OAuth practice and `nonce` is not used; Relativity does not document whether it enforces either.

**The application stops collecting the password.** After this change the application never receives, stores or forwards the user's username and password: the user signs in on RelativityOne's own page, and the application holds only tokens. Remove the credential prompt, the form field and any storage of the password that the replaced code needed (in a VB.NET estate: the `InputBox`, the login form's password control, the Web Forms login control's handler).

**Single sign-on.** Sign-in happens on RelativityOne's own login page, so the instance's single sign-on and two-factor rules apply unchanged — *inferred* from where the sign-in happens, and not documented for clients that customers create.

**The client secret is required.** Code does not work without a client secret: tested and rejected, **verified by NSerio against a live instance; not documented by Relativity**, which confirms Relativity's own statement that the grant requires one. It is displayed once and is stored as Secret Management (Part B of the Migration Documentation) describes.

**Server-side variant.** For a web application whose code runs on a server — in VB.NET, an ASP.NET Web Forms page or a custom page whose code-behind sends the redirect (`Response.Redirect(authorizeUrl)`) and receives the code in a route of the application:

- **Redirect.** The application receives the code at one exact HTTPS URL per environment, a route of the application, with no wildcards (NSerio recommendation; Relativity's matching rules are not documented). The Relativity administrator registers the exact URL on the OAuth2 Client.
- **Secret and tokens.** The client secret stays on the server and never reaches the browser. The user's tokens are held server-side, keyed to the session, and never in the browser.
- **Exchange.** The server performs the code exchange itself, from the code-behind or a handler.

**Desktop variant.** For a desktop or command-line application run by a user on their own machine (WinForms, WPF, console):

- **The system browser.** Open the authorize URL in the user's default browser and not in a web view the application hosts, so the application never sees the password (NSerio recommendation).
- **A loopback redirect on a fixed port.** Listen on the loopback address only, on the registered port, for one request. The Redirect URL is `http://127.0.0.1:<port>/<path>`: a plain-HTTP loopback URL is accepted, **verified by NSerio against a live instance; not documented by Relativity**. The Relativity administrator registers that exact URL; because it is registered in advance, the port is fixed and not chosen at runtime.

```vbnet
Using listener As New HttpListener()
	listener.Prefixes.Add("http://127.0.0.1:<port>/<path>/")
	listener.Start()
	Process.Start(New ProcessStartInfo(authorizeUrl) With {.UseShellExecute = True})
	Dim context As HttpListenerContext = Await listener.GetContextAsync()
	Dim authorizationCode As String = context.Request.QueryString("code")
	Dim returnedState As String = context.Request.QueryString("state")
	context.Response.Close()
End Using
```

- **The secret is extractable.** Code requires a client secret (above), so a distributed desktop application has to embed one, and **a secret shipped inside a distributed binary can be extracted by anyone who holds the binary**. It protects nothing against the person running the application.
- **Compensating guidance** (NSerio recommendation, subject to your own security policy): set Trusted IPs where the users' outbound addresses are known and stable — they restrict where a token can be *requested*, and are checked when a token is issued against the client's ID and secret; use the recommended short Access Token Lifetime of 60 minutes; enable refresh tokens only when needed; and create one OAuth2 Client per distribution, so a leaked secret can be disabled without affecting the others.
- **A known issue.** Relativity documents no recommendation for a distributed desktop application; this design is what NSerio verified works, and Relativity's own recommendation is tracked as a known issue of the release.

Verification: **Verification note — two things.** **Verify the destination accepts `Bearer` before changing the prefix**: some services accept only Basic. **A header set once on a long-lived client cannot be refreshed**: set it per request with a current token.

OAuth2 Client: Create or confirm this OAuth2 Client in RelativityOne (NSerio's observation: the menu path is Home > Authentication > OAuth2 Client Manager, as Relativity's REST API authentication page names it). This tool never connects to RelativityOne and never creates or changes the OAuth2 Client; the settings below are guidance.
- Name: a descriptive name identifying the application it serves.
- Flow Grant Type: Code
- Redirect URLs: Relativity documentation: each URL includes the http or https protocol prefix. The exact callback URL is registered by the Relativity administrator. Server-side web applications, NSerio's recommendation: one exact HTTPS URL per environment, with no wildcards. Desktop and CLI applications: a plain-HTTP loopback URL, `http://127.0.0.1:<port>/<path>`, on a fixed port because the URL is registered in advance and the port cannot be chosen at runtime (verified by NSerio against a live instance; not documented by Relativity).
- Access Token Lifetime: 60 minutes, stated in minutes because the field is in minutes. Relativity documentation: tokens for this flow must have a short lifetime, and the recommended value matches the Identity Server default.
- Allow refresh tokens: NSerio's recommendation: leave it off unless the application must keep a user session active beyond the access token lifetime without asking the user to sign in again. If it is on, the refresh tokens are stored as secrets, per Secret Management (Part B of the Migration Documentation).
- Enabled: on.
<!-- generated:end VB-015 authorization-code -->

### VB-016 · NetworkTransferWithCredentials

<a id="vb-016-migration"></a>

#### Client Credentials

<!-- generated:begin VB-016 client-credentials -->
**Confirming the flow.** When no signal in a file decides the flow, the tool assigns Client Credentials, the default, with this note: "Who supplies the credential at run time? If a person types it or signs in, the integration acts for that person: use Authorization Code. If it comes from configuration, settings, an environment variable or a secret store: use Client Credentials. No signal in this file showed which applies, so Client Credentials, the default, is recommended. Use Authorization Code instead if the integration acts on behalf of a signed-in user, or if your RelativityOne instance already has a Code-flow OAuth2 Client for this integration." A fixed-credential signal assigns it with this note: "A fixed-credential signal in this file points to Client Credentials, but how often that kind of signal is right has not been measured. Use Authorization Code instead if the integration acts on behalf of a signed-in user, or if your RelativityOne instance already has a Code-flow OAuth2 Client for this integration." See [Confirming the flow](#confirming-the-flow) and the "Which flow" paragraph of VB-016 in [Part A](part-a-vbnet.md#vb-016).

Source form: **Applies to:** .NET Framework, and .NET only in Windows desktop applications (`net*-windows` with Windows Forms), where `My.Computer.Network` exists.

```vbnet
My.Computer.Network.DownloadFile(address, destinationPath, userName, password, False, 100000, True)
```

Under token authentication: **First, the destination:** if it is not RelativityOne, this finding is informational — that service's own token mechanism is the migration — and the rest of this template does not apply.

`My.Computer.Network` offers no token-bearing overload. **Replace the call with an `HttpClient` request carrying the bearer header**, set per request, and write the response stream to the destination path:

```vbnet
Using request As New HttpRequestMessage(HttpMethod.Get, address)
	request.Headers.Authorization = New AuthenticationHeaderValue("Bearer", accessToken)
	Using response As HttpResponseMessage = Await client.SendAsync(request)
		response.EnsureSuccessStatusCode()
		Using target As IO.FileStream = IO.File.Create(destinationPath)
			Await response.Content.CopyToAsync(target)
		End Using
	End Using
End Using
```

Credentials: **Where the credentials come from.** The destination decides whether the migration applies. If the destination is not RelativityOne, the migration is that service's own token mechanism, this finding is informational for this tool, and the reader stops here. If it is RelativityOne, the token comes from the token-acquisition part that follows.

**Where the token comes from.** Create an OAuth2 Client with Flow Grant Type set to Client Credentials (part 3 lists its settings), then send a form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=client_credentials`, `client_id`, `client_secret` and a `scope`. The token endpoint is **documented** for this grant. The token is then sent as `Authorization: Bearer <token>`, with `X-CSRF-Header: -` on REST and Kepler calls.

**.NET (both runtimes' `HttpClient`; `System.Text.Json` on .NET)**

```vbnet
Imports System.Net.Http
Imports System.Text.Json

Public Async Function AcquireAccessTokenAsync(host As String, clientId As String, clientSecret As String, scope As String) As Task(Of String)
	Dim accessToken As String
	Using client As New HttpClient()
		Dim form As New FormUrlEncodedContent(New Dictionary(Of String, String) From {
			{"grant_type", "client_credentials"},
			{"client_id", clientId},
			{"client_secret", clientSecret},
			{"scope", scope}})
		Using response As HttpResponseMessage = Await client.PostAsync(host & "/Relativity/Identity/connect/token", form)
			response.EnsureSuccessStatusCode()
			Using document As JsonDocument = JsonDocument.Parse(Await response.Content.ReadAsStringAsync())
				accessToken = document.RootElement.GetProperty("access_token").GetString()
			End Using
		End Using
	End Using
	Return accessToken
End Function
```

**.NET Framework 4.7.2** — the same call with the serializer the runtime ships (`System.Web.Extensions`); `HttpClient` is in `System.Net.Http`:

```vbnet
Imports System.Net.Http
Imports System.Web.Script.Serialization

Public Async Function AcquireAccessTokenAsync(host As String, clientId As String, clientSecret As String, scope As String) As Task(Of String)
	Dim accessToken As String
	Using client As New HttpClient()
		Dim form As New FormUrlEncodedContent(New Dictionary(Of String, String) From {
			{"grant_type", "client_credentials"},
			{"client_id", clientId},
			{"client_secret", clientSecret},
			{"scope", scope}})
		Using response As HttpResponseMessage = Await client.PostAsync(host & "/Relativity/Identity/connect/token", form)
			response.EnsureSuccessStatusCode()
			Dim body As String = Await response.Content.ReadAsStringAsync()
			Dim fields As Dictionary(Of String, Object) = New JavaScriptSerializer().Deserialize(Of Dictionary(Of String, Object))(body)
			accessToken = fields("access_token").ToString()
		End Using
	End Using
	Return accessToken
End Function
```

**The scope depends on what the integration needs, and no single scope is required.** Relativity's documentation shows two values for this grant: `SystemUserInfo` on the REST authentication page, the Usage Reports page and in the official Transfer SDK sample, and `UserInfoAccess` in the Kepler .NET samples. No page says which is correct, whether both work, or what either grants, and the OAuth2 Client's `Scopes` property is documented as one that cannot be set. Use the value the API you call documents, and confirm it.

**Cache the token, and re-acquire it on expiry — do not refresh.** Keep the token for its lifetime (part 3 recommends 60 minutes, Relativity's own wording) and request a new one with the client secret when it expires or a call is rejected for authentication. Client Credentials does not offer refresh tokens: **verified by NSerio against a live instance; not documented by Relativity** — the Allow Refresh Tokens control does not appear for this grant in the RelativityOne user interface. Relativity's own tables and text on refresh tokens conflict with each other, so re-acquisition with the secret is the advice that holds whichever way they are read.

**Long-running processes that use the Relativity SDK.** A `ServiceFactory` built once from a bearer token keeps that token after it expires. Rebuild the `ServiceFactory` and its proxies with a fresh token before expiry. This is the only advice consistent with the documentation: the package `Relativity.Kepler.Client.SDK` (documented) exposes a settable `Token` and an `HttpMessageHandler` overload but no token-provider overload, and no page documents renewing a token behind a factory.

**The client secret.** It is displayed once, at creation or reset. Store it as Secret Management (Part B of the Migration Documentation) describes; never in source, never in tracked configuration, which in a VB.NET estate includes `app.config`, `web.config` and `My.Settings`. A reset invalidates the previous secret immediately, so plan the cut-over before resetting it.

**Before deploying, confirm the calling identity.** Every call now runs as the OAuth2 Client's **Context User** — not as the user in the replaced credential, and for an import or a job as much as for a request. If that user has different workspace permissions or item-level security, the code returns fewer rows or `Unauthorized` in production with no change to its logic, and Relativity's audit attributes every call to that one account. Confirm the Context User's permissions match those of the previous identity. Relativity describes the Context User as usually a member of the Relativity Administrators group; the OAuth2 Client block (part 3) gives NSerio's recommendation, which differs.

**When the integration acts for a signed-in user.** Client Credentials is one service identity by construction. An integration that acts on behalf of a signed-in user is the Authorization Code case, and moving to it means a new OAuth2 Client, because the flow is fixed when a client is created. The finding's flow note says when to make that choice.

Verification: **Verification note — three things.** **Confirm the address is an HTTP address.** The same call transfers over FTP, and a credential given for an `ftp://` address is not Basic over HTTP and not a migration target for this tool. **Confirm the exchange was Basic**: the call answers a server's challenge with the credential, and a server that negotiates an integrated scheme is not Basic Authentication. **The 401-challenge response is lost**: a bearer header is sent up front and answers nothing; progress display and the connection timeout of the original call have no counterpart in the replacement and are set on the client.

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
<!-- generated:end VB-016 client-credentials -->

#### Authorization Code

<!-- generated:begin VB-016 authorization-code -->
**Confirming the flow.** An interactive-user signal in a file assigns Authorization Code, with this note: "An interactive-user signal in this file points to Authorization Code. Confirm the flow: use Client Credentials instead if the integration runs with credentials fixed for the integration and no signed-in user." Your user profile can also make Authorization Code the default; see Changing the default in [Confirming the flow](#confirming-the-flow). See [Confirming the flow](#confirming-the-flow) and the "Which flow" paragraph of VB-016 in [Part A](part-a-vbnet.md#vb-016).

Source form: **Applies to:** .NET Framework, and .NET only in Windows desktop applications (`net*-windows` with Windows Forms), where `My.Computer.Network` exists.

```vbnet
My.Computer.Network.DownloadFile(address, destinationPath, userName, password, False, 100000, True)
```

Under token authentication: **First, the destination:** if it is not RelativityOne, this finding is informational — that service's own token mechanism is the migration — and the rest of this template does not apply.

`My.Computer.Network` offers no token-bearing overload. **Replace the call with an `HttpClient` request carrying the bearer header**, set per request, and write the response stream to the destination path:

```vbnet
Using request As New HttpRequestMessage(HttpMethod.Get, address)
	request.Headers.Authorization = New AuthenticationHeaderValue("Bearer", accessToken)
	Using response As HttpResponseMessage = Await client.SendAsync(request)
		response.EnsureSuccessStatusCode()
		Using target As IO.FileStream = IO.File.Create(destinationPath)
			Await response.Content.CopyToAsync(target)
		End Using
	End Using
End Using
```

Credentials: **Where the credentials come from.** The destination decides whether the migration applies. If the destination is not RelativityOne, the migration is that service's own token mechanism, this finding is informational for this tool, and the reader stops here. If it is RelativityOne, the token comes from the token-acquisition part that follows.

The text a finding assigned Authorization Code ("Code") receives after part 1. It has a common part and two variants, **server-side** and **desktop**. The bold lead-in of each element is its marker. Every fact Relativity does not document but NSerio verified carries the exact label **verified by NSerio against a live instance; not documented by Relativity**.

**Common to both variants**

**Sign in.** Replace the code that collected the user's credentials with a redirect: send the user's browser to the authorize endpoint `<host>/Relativity/Identity/connect/authorize`, which leads to the RelativityOne login page. The authorize endpoint is **verified by NSerio against a live instance; not documented by Relativity**. The request carries the standard authorization-code parameters — `client_id`, `response_type=code`, the registered `redirect_uri`, a `scope`, and a `state` value the application checks on return; these are standard OAuth, and Relativity documents none of them. No discovery document is relied on.

**Receive the code.** The browser returns to the Redirect URL registered on the OAuth2 Client, with the authorization code and the `state` value. Check the `state` before using the code. The Redirect URL must be exactly the one registered (the variants say how).

**Exchange the code.** Send a form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=authorization_code`, the `code`, the same `redirect_uri`, the `client_id` and the `client_secret`, plus the `code_verifier` when PKCE is used. The code is exchanged at that endpoint, which Relativity documents for the Client Credentials grant only; the exchange is **verified by NSerio against a live instance; not documented by Relativity**.

```vbnet
Dim form As New FormUrlEncodedContent(New Dictionary(Of String, String) From {
	{"grant_type", "authorization_code"},
	{"code", authorizationCode},
	{"redirect_uri", redirectUri},
	{"client_id", clientId},
	{"client_secret", clientSecret},
	{"code_verifier", codeVerifier}})
Using response As HttpResponseMessage = Await client.PostAsync(host & "/Relativity/Identity/connect/token", form)
	response.EnsureSuccessStatusCode()
End Using
```

**Use the token.** Send `Authorization: Bearer <access token>` — the change at the use site in part 1 — with `X-CSRF-Header: -` on REST and Kepler calls.

**On expiry.** With Allow Refresh Tokens enabled on the OAuth2 Client and PKCE used, the application holds a refresh token and, when the access token expires, sends a `grant_type=refresh_token` request with it and the client credentials in place of the code exchange — *inferred* from the standard OAuth shape, not tested. Without a refresh token the user signs in again. Relativity does not document a refresh token's lifetime, whether it rotates, how it is revoked, or whether a revocation endpoint exists, nor what disabling or deleting the client does to tokens already issued. A refresh token is a secret: store it as Secret Management (Part B of the Migration Documentation) describes, and on compromise disable the client and replace it. Enable refresh tokens only if the application must keep a session beyond one token lifetime without asking the user to sign in again.

**PKCE.** PKCE is required by Relativity's own documentation to obtain a refresh token, and not otherwise: a Code request that does not ask for a refresh token works without it, **verified by NSerio against a live instance; not documented by Relativity**. It is never a blanket requirement. NSerio recommends PKCE with the `S256` method in both variants (NSerio recommendation), and does not claim that Relativity accepts any particular method, which it does not document.

**Scope.** No scope set is prescribed for Code. The scope below works — `openid RelativityRequestOrigin UserInfo UserInfoAccess AuthenticationID RelativityWeb` — **verified by NSerio against a live instance; not documented by Relativity**, verified as working and not as minimal. `state` is sent as standard OAuth practice and `nonce` is not used; Relativity does not document whether it enforces either.

**The application stops collecting the password.** After this change the application never receives, stores or forwards the user's username and password: the user signs in on RelativityOne's own page, and the application holds only tokens. Remove the credential prompt, the form field and any storage of the password that the replaced code needed (in a VB.NET estate: the `InputBox`, the login form's password control, the Web Forms login control's handler).

**Single sign-on.** Sign-in happens on RelativityOne's own login page, so the instance's single sign-on and two-factor rules apply unchanged — *inferred* from where the sign-in happens, and not documented for clients that customers create.

**The client secret is required.** Code does not work without a client secret: tested and rejected, **verified by NSerio against a live instance; not documented by Relativity**, which confirms Relativity's own statement that the grant requires one. It is displayed once and is stored as Secret Management (Part B of the Migration Documentation) describes.

**Server-side variant.** For a web application whose code runs on a server — in VB.NET, an ASP.NET Web Forms page or a custom page whose code-behind sends the redirect (`Response.Redirect(authorizeUrl)`) and receives the code in a route of the application:

- **Redirect.** The application receives the code at one exact HTTPS URL per environment, a route of the application, with no wildcards (NSerio recommendation; Relativity's matching rules are not documented). The Relativity administrator registers the exact URL on the OAuth2 Client.
- **Secret and tokens.** The client secret stays on the server and never reaches the browser. The user's tokens are held server-side, keyed to the session, and never in the browser.
- **Exchange.** The server performs the code exchange itself, from the code-behind or a handler.

**Desktop variant.** For a desktop or command-line application run by a user on their own machine (WinForms, WPF, console):

- **The system browser.** Open the authorize URL in the user's default browser and not in a web view the application hosts, so the application never sees the password (NSerio recommendation).
- **A loopback redirect on a fixed port.** Listen on the loopback address only, on the registered port, for one request. The Redirect URL is `http://127.0.0.1:<port>/<path>`: a plain-HTTP loopback URL is accepted, **verified by NSerio against a live instance; not documented by Relativity**. The Relativity administrator registers that exact URL; because it is registered in advance, the port is fixed and not chosen at runtime.

```vbnet
Using listener As New HttpListener()
	listener.Prefixes.Add("http://127.0.0.1:<port>/<path>/")
	listener.Start()
	Process.Start(New ProcessStartInfo(authorizeUrl) With {.UseShellExecute = True})
	Dim context As HttpListenerContext = Await listener.GetContextAsync()
	Dim authorizationCode As String = context.Request.QueryString("code")
	Dim returnedState As String = context.Request.QueryString("state")
	context.Response.Close()
End Using
```

- **The secret is extractable.** Code requires a client secret (above), so a distributed desktop application has to embed one, and **a secret shipped inside a distributed binary can be extracted by anyone who holds the binary**. It protects nothing against the person running the application.
- **Compensating guidance** (NSerio recommendation, subject to your own security policy): set Trusted IPs where the users' outbound addresses are known and stable — they restrict where a token can be *requested*, and are checked when a token is issued against the client's ID and secret; use the recommended short Access Token Lifetime of 60 minutes; enable refresh tokens only when needed; and create one OAuth2 Client per distribution, so a leaked secret can be disabled without affecting the others.
- **A known issue.** Relativity documents no recommendation for a distributed desktop application; this design is what NSerio verified works, and Relativity's own recommendation is tracked as a known issue of the release.

Verification: **Verification note — three things.** **Confirm the address is an HTTP address.** The same call transfers over FTP, and a credential given for an `ftp://` address is not Basic over HTTP and not a migration target for this tool. **Confirm the exchange was Basic**: the call answers a server's challenge with the credential, and a server that negotiates an integrated scheme is not Basic Authentication. **The 401-challenge response is lost**: a bearer header is sent up front and answers nothing; progress display and the connection timeout of the original call have no counterpart in the replacement and are set on the client.

OAuth2 Client: Create or confirm this OAuth2 Client in RelativityOne (NSerio's observation: the menu path is Home > Authentication > OAuth2 Client Manager, as Relativity's REST API authentication page names it). This tool never connects to RelativityOne and never creates or changes the OAuth2 Client; the settings below are guidance.
- Name: a descriptive name identifying the application it serves.
- Flow Grant Type: Code
- Redirect URLs: Relativity documentation: each URL includes the http or https protocol prefix. The exact callback URL is registered by the Relativity administrator. Server-side web applications, NSerio's recommendation: one exact HTTPS URL per environment, with no wildcards. Desktop and CLI applications: a plain-HTTP loopback URL, `http://127.0.0.1:<port>/<path>`, on a fixed port because the URL is registered in advance and the port cannot be chosen at runtime (verified by NSerio against a live instance; not documented by Relativity).
- Access Token Lifetime: 60 minutes, stated in minutes because the field is in minutes. Relativity documentation: tokens for this flow must have a short lifetime, and the recommended value matches the Identity Server default.
- Allow refresh tokens: NSerio's recommendation: leave it off unless the application must keep a user session active beyond the access token lifetime without asking the user to sign in again. If it is on, the refresh tokens are stored as secrets, per Secret Management (Part B of the Migration Documentation).
- Enabled: on.
<!-- generated:end VB-016 authorization-code -->

### VB-017 · LateBoundHttpObjectCredentials

<a id="vb-017-migration"></a>

#### Client Credentials

<!-- generated:begin VB-017 client-credentials -->
**Confirming the flow.** When no signal in a file decides the flow, the tool assigns Client Credentials, the default, with this note: "Who supplies the credential at run time? If a person types it or signs in, the integration acts for that person: use Authorization Code. If it comes from configuration, settings, an environment variable or a secret store: use Client Credentials. No signal in this file showed which applies, so Client Credentials, the default, is recommended. Use Authorization Code instead if the integration acts on behalf of a signed-in user, or if your RelativityOne instance already has a Code-flow OAuth2 Client for this integration." A fixed-credential signal assigns it with this note: "A fixed-credential signal in this file points to Client Credentials, but how often that kind of signal is right has not been measured. Use Authorization Code instead if the integration acts on behalf of a signed-in user, or if your RelativityOne instance already has a Code-flow OAuth2 Client for this integration." See [Confirming the flow](#confirming-the-flow) and the "Which flow" paragraph of VB-017 in [Part A](part-a-vbnet.md#vb-017).

Source form: **Applies to:** both in syntax; the COM objects are Windows components.

```vbnet
Dim http As Object = CreateObject("MSXML2.ServerXMLHTTP.6.0")
http.Open("GET", url, False, userName, password)
```

Under token authentication: **First, the destination:** if it is not RelativityOne, this finding is informational — that service's own token mechanism is the migration — and the rest of this template does not apply.

**Drop the user name and password from `Open`** (or remove `SetCredentials`) and **set the bearer header per request** before sending:

```vbnet
Dim http As Object = CreateObject("MSXML2.ServerXMLHTTP.6.0")
http.Open("GET", url, False)
http.setRequestHeader("Authorization", "Bearer " & accessToken)
http.send()
```

Where the code is being moved off late-bound COM, an `HttpClient` call is the target of the Part 1 forms above.

Credentials: **Where the credentials come from.** The destination decides whether the migration applies. If the destination is not RelativityOne, the migration is that service's own token mechanism, this finding is informational for this tool, and the reader stops here. If it is RelativityOne, the token comes from the token-acquisition part that follows.

**Where the token comes from.** Create an OAuth2 Client with Flow Grant Type set to Client Credentials (part 3 lists its settings), then send a form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=client_credentials`, `client_id`, `client_secret` and a `scope`. The token endpoint is **documented** for this grant. The token is then sent as `Authorization: Bearer <token>`, with `X-CSRF-Header: -` on REST and Kepler calls.

**.NET (both runtimes' `HttpClient`; `System.Text.Json` on .NET)**

```vbnet
Imports System.Net.Http
Imports System.Text.Json

Public Async Function AcquireAccessTokenAsync(host As String, clientId As String, clientSecret As String, scope As String) As Task(Of String)
	Dim accessToken As String
	Using client As New HttpClient()
		Dim form As New FormUrlEncodedContent(New Dictionary(Of String, String) From {
			{"grant_type", "client_credentials"},
			{"client_id", clientId},
			{"client_secret", clientSecret},
			{"scope", scope}})
		Using response As HttpResponseMessage = Await client.PostAsync(host & "/Relativity/Identity/connect/token", form)
			response.EnsureSuccessStatusCode()
			Using document As JsonDocument = JsonDocument.Parse(Await response.Content.ReadAsStringAsync())
				accessToken = document.RootElement.GetProperty("access_token").GetString()
			End Using
		End Using
	End Using
	Return accessToken
End Function
```

**.NET Framework 4.7.2** — the same call with the serializer the runtime ships (`System.Web.Extensions`); `HttpClient` is in `System.Net.Http`:

```vbnet
Imports System.Net.Http
Imports System.Web.Script.Serialization

Public Async Function AcquireAccessTokenAsync(host As String, clientId As String, clientSecret As String, scope As String) As Task(Of String)
	Dim accessToken As String
	Using client As New HttpClient()
		Dim form As New FormUrlEncodedContent(New Dictionary(Of String, String) From {
			{"grant_type", "client_credentials"},
			{"client_id", clientId},
			{"client_secret", clientSecret},
			{"scope", scope}})
		Using response As HttpResponseMessage = Await client.PostAsync(host & "/Relativity/Identity/connect/token", form)
			response.EnsureSuccessStatusCode()
			Dim body As String = Await response.Content.ReadAsStringAsync()
			Dim fields As Dictionary(Of String, Object) = New JavaScriptSerializer().Deserialize(Of Dictionary(Of String, Object))(body)
			accessToken = fields("access_token").ToString()
		End Using
	End Using
	Return accessToken
End Function
```

**The scope depends on what the integration needs, and no single scope is required.** Relativity's documentation shows two values for this grant: `SystemUserInfo` on the REST authentication page, the Usage Reports page and in the official Transfer SDK sample, and `UserInfoAccess` in the Kepler .NET samples. No page says which is correct, whether both work, or what either grants, and the OAuth2 Client's `Scopes` property is documented as one that cannot be set. Use the value the API you call documents, and confirm it.

**Cache the token, and re-acquire it on expiry — do not refresh.** Keep the token for its lifetime (part 3 recommends 60 minutes, Relativity's own wording) and request a new one with the client secret when it expires or a call is rejected for authentication. Client Credentials does not offer refresh tokens: **verified by NSerio against a live instance; not documented by Relativity** — the Allow Refresh Tokens control does not appear for this grant in the RelativityOne user interface. Relativity's own tables and text on refresh tokens conflict with each other, so re-acquisition with the secret is the advice that holds whichever way they are read.

**Long-running processes that use the Relativity SDK.** A `ServiceFactory` built once from a bearer token keeps that token after it expires. Rebuild the `ServiceFactory` and its proxies with a fresh token before expiry. This is the only advice consistent with the documentation: the package `Relativity.Kepler.Client.SDK` (documented) exposes a settable `Token` and an `HttpMessageHandler` overload but no token-provider overload, and no page documents renewing a token behind a factory.

**The client secret.** It is displayed once, at creation or reset. Store it as Secret Management (Part B of the Migration Documentation) describes; never in source, never in tracked configuration, which in a VB.NET estate includes `app.config`, `web.config` and `My.Settings`. A reset invalidates the previous secret immediately, so plan the cut-over before resetting it.

**Before deploying, confirm the calling identity.** Every call now runs as the OAuth2 Client's **Context User** — not as the user in the replaced credential, and for an import or a job as much as for a request. If that user has different workspace permissions or item-level security, the code returns fewer rows or `Unauthorized` in production with no change to its logic, and Relativity's audit attributes every call to that one account. Confirm the Context User's permissions match those of the previous identity. Relativity describes the Context User as usually a member of the Relativity Administrators group; the OAuth2 Client block (part 3) gives NSerio's recommendation, which differs.

**When the integration acts for a signed-in user.** Client Credentials is one service identity by construction. An integration that acts on behalf of a signed-in user is the Authorization Code case, and moving to it means a new OAuth2 Client, because the flow is fixed when a client is created. The finding's flow note says when to make that choice.

Verification: **Verification note — three things.** **The 401-challenge response is lost**: the object answered a challenge with the credential; a bearer header is sent up front. **Confirm the exchange was Basic**: the object also negotiates integrated schemes. **A token has a finite lifetime**: set the header on each request, not once on a long-lived object.

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
<!-- generated:end VB-017 client-credentials -->

#### Authorization Code

<!-- generated:begin VB-017 authorization-code -->
**Confirming the flow.** An interactive-user signal in a file assigns Authorization Code, with this note: "An interactive-user signal in this file points to Authorization Code. Confirm the flow: use Client Credentials instead if the integration runs with credentials fixed for the integration and no signed-in user." Your user profile can also make Authorization Code the default; see Changing the default in [Confirming the flow](#confirming-the-flow). See [Confirming the flow](#confirming-the-flow) and the "Which flow" paragraph of VB-017 in [Part A](part-a-vbnet.md#vb-017).

Source form: **Applies to:** both in syntax; the COM objects are Windows components.

```vbnet
Dim http As Object = CreateObject("MSXML2.ServerXMLHTTP.6.0")
http.Open("GET", url, False, userName, password)
```

Under token authentication: **First, the destination:** if it is not RelativityOne, this finding is informational — that service's own token mechanism is the migration — and the rest of this template does not apply.

**Drop the user name and password from `Open`** (or remove `SetCredentials`) and **set the bearer header per request** before sending:

```vbnet
Dim http As Object = CreateObject("MSXML2.ServerXMLHTTP.6.0")
http.Open("GET", url, False)
http.setRequestHeader("Authorization", "Bearer " & accessToken)
http.send()
```

Where the code is being moved off late-bound COM, an `HttpClient` call is the target of the Part 1 forms above.

Credentials: **Where the credentials come from.** The destination decides whether the migration applies. If the destination is not RelativityOne, the migration is that service's own token mechanism, this finding is informational for this tool, and the reader stops here. If it is RelativityOne, the token comes from the token-acquisition part that follows.

The text a finding assigned Authorization Code ("Code") receives after part 1. It has a common part and two variants, **server-side** and **desktop**. The bold lead-in of each element is its marker. Every fact Relativity does not document but NSerio verified carries the exact label **verified by NSerio against a live instance; not documented by Relativity**.

**Common to both variants**

**Sign in.** Replace the code that collected the user's credentials with a redirect: send the user's browser to the authorize endpoint `<host>/Relativity/Identity/connect/authorize`, which leads to the RelativityOne login page. The authorize endpoint is **verified by NSerio against a live instance; not documented by Relativity**. The request carries the standard authorization-code parameters — `client_id`, `response_type=code`, the registered `redirect_uri`, a `scope`, and a `state` value the application checks on return; these are standard OAuth, and Relativity documents none of them. No discovery document is relied on.

**Receive the code.** The browser returns to the Redirect URL registered on the OAuth2 Client, with the authorization code and the `state` value. Check the `state` before using the code. The Redirect URL must be exactly the one registered (the variants say how).

**Exchange the code.** Send a form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=authorization_code`, the `code`, the same `redirect_uri`, the `client_id` and the `client_secret`, plus the `code_verifier` when PKCE is used. The code is exchanged at that endpoint, which Relativity documents for the Client Credentials grant only; the exchange is **verified by NSerio against a live instance; not documented by Relativity**.

```vbnet
Dim form As New FormUrlEncodedContent(New Dictionary(Of String, String) From {
	{"grant_type", "authorization_code"},
	{"code", authorizationCode},
	{"redirect_uri", redirectUri},
	{"client_id", clientId},
	{"client_secret", clientSecret},
	{"code_verifier", codeVerifier}})
Using response As HttpResponseMessage = Await client.PostAsync(host & "/Relativity/Identity/connect/token", form)
	response.EnsureSuccessStatusCode()
End Using
```

**Use the token.** Send `Authorization: Bearer <access token>` — the change at the use site in part 1 — with `X-CSRF-Header: -` on REST and Kepler calls.

**On expiry.** With Allow Refresh Tokens enabled on the OAuth2 Client and PKCE used, the application holds a refresh token and, when the access token expires, sends a `grant_type=refresh_token` request with it and the client credentials in place of the code exchange — *inferred* from the standard OAuth shape, not tested. Without a refresh token the user signs in again. Relativity does not document a refresh token's lifetime, whether it rotates, how it is revoked, or whether a revocation endpoint exists, nor what disabling or deleting the client does to tokens already issued. A refresh token is a secret: store it as Secret Management (Part B of the Migration Documentation) describes, and on compromise disable the client and replace it. Enable refresh tokens only if the application must keep a session beyond one token lifetime without asking the user to sign in again.

**PKCE.** PKCE is required by Relativity's own documentation to obtain a refresh token, and not otherwise: a Code request that does not ask for a refresh token works without it, **verified by NSerio against a live instance; not documented by Relativity**. It is never a blanket requirement. NSerio recommends PKCE with the `S256` method in both variants (NSerio recommendation), and does not claim that Relativity accepts any particular method, which it does not document.

**Scope.** No scope set is prescribed for Code. The scope below works — `openid RelativityRequestOrigin UserInfo UserInfoAccess AuthenticationID RelativityWeb` — **verified by NSerio against a live instance; not documented by Relativity**, verified as working and not as minimal. `state` is sent as standard OAuth practice and `nonce` is not used; Relativity does not document whether it enforces either.

**The application stops collecting the password.** After this change the application never receives, stores or forwards the user's username and password: the user signs in on RelativityOne's own page, and the application holds only tokens. Remove the credential prompt, the form field and any storage of the password that the replaced code needed (in a VB.NET estate: the `InputBox`, the login form's password control, the Web Forms login control's handler).

**Single sign-on.** Sign-in happens on RelativityOne's own login page, so the instance's single sign-on and two-factor rules apply unchanged — *inferred* from where the sign-in happens, and not documented for clients that customers create.

**The client secret is required.** Code does not work without a client secret: tested and rejected, **verified by NSerio against a live instance; not documented by Relativity**, which confirms Relativity's own statement that the grant requires one. It is displayed once and is stored as Secret Management (Part B of the Migration Documentation) describes.

**Server-side variant.** For a web application whose code runs on a server — in VB.NET, an ASP.NET Web Forms page or a custom page whose code-behind sends the redirect (`Response.Redirect(authorizeUrl)`) and receives the code in a route of the application:

- **Redirect.** The application receives the code at one exact HTTPS URL per environment, a route of the application, with no wildcards (NSerio recommendation; Relativity's matching rules are not documented). The Relativity administrator registers the exact URL on the OAuth2 Client.
- **Secret and tokens.** The client secret stays on the server and never reaches the browser. The user's tokens are held server-side, keyed to the session, and never in the browser.
- **Exchange.** The server performs the code exchange itself, from the code-behind or a handler.

**Desktop variant.** For a desktop or command-line application run by a user on their own machine (WinForms, WPF, console):

- **The system browser.** Open the authorize URL in the user's default browser and not in a web view the application hosts, so the application never sees the password (NSerio recommendation).
- **A loopback redirect on a fixed port.** Listen on the loopback address only, on the registered port, for one request. The Redirect URL is `http://127.0.0.1:<port>/<path>`: a plain-HTTP loopback URL is accepted, **verified by NSerio against a live instance; not documented by Relativity**. The Relativity administrator registers that exact URL; because it is registered in advance, the port is fixed and not chosen at runtime.

```vbnet
Using listener As New HttpListener()
	listener.Prefixes.Add("http://127.0.0.1:<port>/<path>/")
	listener.Start()
	Process.Start(New ProcessStartInfo(authorizeUrl) With {.UseShellExecute = True})
	Dim context As HttpListenerContext = Await listener.GetContextAsync()
	Dim authorizationCode As String = context.Request.QueryString("code")
	Dim returnedState As String = context.Request.QueryString("state")
	context.Response.Close()
End Using
```

- **The secret is extractable.** Code requires a client secret (above), so a distributed desktop application has to embed one, and **a secret shipped inside a distributed binary can be extracted by anyone who holds the binary**. It protects nothing against the person running the application.
- **Compensating guidance** (NSerio recommendation, subject to your own security policy): set Trusted IPs where the users' outbound addresses are known and stable — they restrict where a token can be *requested*, and are checked when a token is issued against the client's ID and secret; use the recommended short Access Token Lifetime of 60 minutes; enable refresh tokens only when needed; and create one OAuth2 Client per distribution, so a leaked secret can be disabled without affecting the others.
- **A known issue.** Relativity documents no recommendation for a distributed desktop application; this design is what NSerio verified works, and Relativity's own recommendation is tracked as a known issue of the release.

Verification: **Verification note — three things.** **The 401-challenge response is lost**: the object answered a challenge with the credential; a bearer header is sent up front. **Confirm the exchange was Basic**: the object also negotiates integrated schemes. **A token has a finite lifetime**: set the header on each request, not once on a long-lived object.

OAuth2 Client: Create or confirm this OAuth2 Client in RelativityOne (NSerio's observation: the menu path is Home > Authentication > OAuth2 Client Manager, as Relativity's REST API authentication page names it). This tool never connects to RelativityOne and never creates or changes the OAuth2 Client; the settings below are guidance.
- Name: a descriptive name identifying the application it serves.
- Flow Grant Type: Code
- Redirect URLs: Relativity documentation: each URL includes the http or https protocol prefix. The exact callback URL is registered by the Relativity administrator. Server-side web applications, NSerio's recommendation: one exact HTTPS URL per environment, with no wildcards. Desktop and CLI applications: a plain-HTTP loopback URL, `http://127.0.0.1:<port>/<path>`, on a fixed port because the URL is registered in advance and the port cannot be chosen at runtime (verified by NSerio against a live instance; not documented by Relativity).
- Access Token Lifetime: 60 minutes, stated in minutes because the field is in minutes. Relativity documentation: tokens for this flow must have a short lifetime, and the recommended value matches the Identity Server default.
- Allow refresh tokens: NSerio's recommendation: leave it off unless the application must keep a user session active beyond the access token lifetime without asking the user to sign in again. If it is on, the refresh tokens are stored as secrets, per Secret Management (Part B of the Migration Documentation).
- Enabled: on.
<!-- generated:end VB-017 authorization-code -->

## Generic suggestion

The tool gives a generic recommendation when a match is reported at High or Medium confidence but no pattern-specific entry applies. It is shorter than a class entry: it does not show a before-and-after form, because the construct was not characterized. Use it as a starting point, and read the Part A description of the pattern class if you can identify the construct.

### Client Credentials

<!-- generated:begin generic client-credentials -->
**Confirming the flow.** When no signal in a file decides the flow, the tool assigns Client Credentials, the default, with this note: "Who supplies the credential at run time? If a person types it or signs in, the integration acts for that person: use Authorization Code. If it comes from configuration, settings, an environment variable or a secret store: use Client Credentials. No signal in this file showed which applies, so Client Credentials, the default, is recommended. Use Authorization Code instead if the integration acts on behalf of a signed-in user, or if your RelativityOne instance already has a Code-flow OAuth2 Client for this integration." A fixed-credential signal assigns it with this note: "A fixed-credential signal in this file points to Client Credentials, but how often that kind of signal is right has not been measured. Use Authorization Code instead if the integration acts on behalf of a signed-in user, or if your RelativityOne instance already has a Code-flow OAuth2 Client for this integration." See [Confirming the flow](#confirming-the-flow).

The tool reports Basic Authentication at this location but did not recognize the construct, so the change has not been characterized and no before-and-after form is offered here. If the destination is not RelativityOne, Relativity's identity endpoint does not issue tokens for it: the migration is that service's own, and this finding is informational. If the destination is RelativityOne, Basic Authentication is no longer documented for its REST API. The documented destination in VB.NET is either the Relativity SDK's bearer-token credential type, where the code uses the SDK, or a bearer `Authorization` header, where it builds requests itself; either carries a token obtained from the platform's identity endpoint with the client-credentials grant. The client ID and secret that obtain that token belong in the process environment or a secret store, per Secret Management (Part B of the Migration Documentation), and never in source or tracked configuration, which in a VB.NET estate includes `My.Settings`, `app.config` and `web.config`. Beyond those facts this is a pointer and not an instruction: the migration must be designed from the code by whoever owns it, rather than applied from this text.

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
