using MigrateAuth.Cli.Reporting;

namespace MigrateAuth.Cli.Catalog.Languages;

/// <summary>
/// The catalog's recommendation text for `VBNet`: seventeen classes, both flows, and both generic fallback texts. The two Low classes (the
/// credential sent to a non-HTTP sink and the helper definition) carry no entry: the catalog routes both to manual review.
///
/// HOW A RECOMMENDATION IS COMPOSED. Part 1 is the class's four fields and is flow-neutral, so the source form, the target form and the verification
/// note are the same text for both flows. The credential provenance is the class's own share followed by part 2 for the finding's flow, held once here
/// as <see cref="CLIENT_CREDENTIALS_TOKEN_ACQUISITION"/> and <see cref="AUTHORIZATION_CODE_TOKEN_ACQUISITION"/>. Part 3, the OAuth2 Client block, is
/// appended by the shared engine's composer and is not repeated here.
///
/// ENCODING CHOICES. Every secret-handling sentence names <see cref="SecretManagementPointer.PART_B_NAME"/> and the label of a fact NSerio verified
/// but Relativity does not document is <see cref="OAuth2ClientConfigurationBlocks.NSERIO_VERIFIED_LABEL"/>, both through the constants the shared
/// engine already owns. Each class names the runtimes it applies to in its own text, and where the two differ the text carries labelled sub-blocks and
/// not a second template.
///
/// A SEPARATE FILE BECAUSE <c>VBNetAnalyzer</c> IS <c>file</c>-SCOPED: its type cannot be split across files, so the text lives in this
/// <c>internal</c> holder and the analyzer's template provider reads it by the same identifiers.
/// </summary>
internal static class VBNetRecommendationTemplates
{
	/// <summary>The Client Credentials generic fallback prose for a VBNet finding, with the secret-handling sentence naming the Part B pointer.</summary>
	public const string VBNET_LANGUAGE_FALLBACK_TEXT =
		"The tool reports Basic Authentication at this location but did not recognize the construct, so the change has not been characterized and no before-and-after form is offered here. If the destination is not RelativityOne, Relativity's identity endpoint does not issue tokens for it: the migration is that service's own, and this finding is informational. If the destination is RelativityOne, Basic Authentication is no longer documented for its REST API. The documented destination in VB.NET is either the Relativity SDK's bearer-token credential type, where the code uses the SDK, or a bearer `Authorization` header, where it builds requests itself; either carries a token obtained from the platform's identity endpoint with the client-credentials grant. The client ID and secret that obtain that token belong in the process environment or a secret store, per " + SecretManagementPointer.PART_B_NAME + ", and never in source or tracked configuration, which in a VB.NET estate includes `My.Settings`, `app.config` and `web.config`. Beyond those facts this is a pointer and not an instruction: the migration must be designed from the code by whoever owns it, rather than applied from this text.";

	/// <summary>The Authorization Code generic fallback prose for a VBNet finding.</summary>
	public const string VBNET_CODE_LANGUAGE_FALLBACK_TEXT =
		"The tool reports Basic Authentication at this location but did not recognize the construct, so the change has not been characterized and no before-and-after form is offered here. If the destination is not RelativityOne, Relativity's identity endpoint does not issue tokens for it: the migration is that service's own, and this finding is informational. If the destination is RelativityOne, Basic Authentication is no longer documented for its REST API, and this finding is assigned the Authorization Code flow, in which the application sends the user to RelativityOne to sign in, receives an authorization code at a Redirect URL registered on its OAuth2 Client, and exchanges the code for an access token that it sends as a Bearer token. The authorize endpoint is `<host>/Relativity/Identity/connect/authorize` and the code is exchanged at `<host>/Relativity/Identity/connect/token`; both are " + OAuth2ClientConfigurationBlocks.NSERIO_VERIFIED_LABEL + ". PKCE is required by Relativity's own documentation to obtain a refresh token, and not otherwise. The client ID and secret belong in the process environment or a secret store, per " + SecretManagementPointer.PART_B_NAME + ", and never in source or tracked configuration; Code requires a client secret. After the change the application no longer collects or forwards the user's password. Beyond those facts this is a pointer and not an instruction: the migration must be designed from the code by whoever owns it, rather than applied from this text.";

	/// <summary>Part 2 for Client Credentials: how the token is obtained and kept. Appended to every class's credential-provenance share for a Client Credentials finding.</summary>
	public const string CLIENT_CREDENTIALS_TOKEN_ACQUISITION =
		"**Where the token comes from.** Create an OAuth2 Client with Flow Grant Type set to Client Credentials (part 3 lists its settings), then send a form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=client_credentials`, `client_id`, `client_secret` and a `scope`. The token endpoint is **documented** for this grant. The token is then sent as `Authorization: Bearer <token>`, with `X-CSRF-Header: -` on REST and Kepler calls.\n\n**.NET (both runtimes' `HttpClient`; `System.Text.Json` on .NET)**\n\n```vbnet\nImports System.Net.Http\nImports System.Text.Json\n\nPublic Async Function AcquireAccessTokenAsync(host As String, clientId As String, clientSecret As String, scope As String) As Task(Of String)\n\tDim accessToken As String\n\tUsing client As New HttpClient()\n\t\tDim form As New FormUrlEncodedContent(New Dictionary(Of String, String) From {\n\t\t\t{\"grant_type\", \"client_credentials\"},\n\t\t\t{\"client_id\", clientId},\n\t\t\t{\"client_secret\", clientSecret},\n\t\t\t{\"scope\", scope}})\n\t\tUsing response As HttpResponseMessage = Await client.PostAsync(host & \"/Relativity/Identity/connect/token\", form)\n\t\t\tresponse.EnsureSuccessStatusCode()\n\t\t\tUsing document As JsonDocument = JsonDocument.Parse(Await response.Content.ReadAsStringAsync())\n\t\t\t\taccessToken = document.RootElement.GetProperty(\"access_token\").GetString()\n\t\t\tEnd Using\n\t\tEnd Using\n\tEnd Using\n\tReturn accessToken\nEnd Function\n```\n\n**.NET Framework 4.7.2** — the same call with the serializer the runtime ships (`System.Web.Extensions`); `HttpClient` is in `System.Net.Http`:\n\n```vbnet\nImports System.Net.Http\nImports System.Web.Script.Serialization\n\nPublic Async Function AcquireAccessTokenAsync(host As String, clientId As String, clientSecret As String, scope As String) As Task(Of String)\n\tDim accessToken As String\n\tUsing client As New HttpClient()\n\t\tDim form As New FormUrlEncodedContent(New Dictionary(Of String, String) From {\n\t\t\t{\"grant_type\", \"client_credentials\"},\n\t\t\t{\"client_id\", clientId},\n\t\t\t{\"client_secret\", clientSecret},\n\t\t\t{\"scope\", scope}})\n\t\tUsing response As HttpResponseMessage = Await client.PostAsync(host & \"/Relativity/Identity/connect/token\", form)\n\t\t\tresponse.EnsureSuccessStatusCode()\n\t\t\tDim body As String = Await response.Content.ReadAsStringAsync()\n\t\t\tDim fields As Dictionary(Of String, Object) = New JavaScriptSerializer().Deserialize(Of Dictionary(Of String, Object))(body)\n\t\t\taccessToken = fields(\"access_token\").ToString()\n\t\tEnd Using\n\tEnd Using\n\tReturn accessToken\nEnd Function\n```\n\n**The scope depends on what the integration needs, and no single scope is required.** Relativity's documentation shows two values for this grant: `SystemUserInfo` on the REST authentication page, the Usage Reports page and in the official Transfer SDK sample, and `UserInfoAccess` in the Kepler .NET samples. No page says which is correct, whether both work, or what either grants, and the OAuth2 Client's `Scopes` property is documented as one that cannot be set. Use the value the API you call documents, and confirm it.\n\n**Cache the token, and re-acquire it on expiry — do not refresh.** Keep the token for its lifetime (part 3 recommends 60 minutes, Relativity's own wording) and request a new one with the client secret when it expires or a call is rejected for authentication. Client Credentials does not offer refresh tokens: **" + OAuth2ClientConfigurationBlocks.NSERIO_VERIFIED_LABEL + "** — the Allow Refresh Tokens control does not appear for this grant in the RelativityOne user interface. Relativity's own tables and text on refresh tokens conflict with each other, so re-acquisition with the secret is the advice that holds whichever way they are read.\n\n**Long-running processes that use the Relativity SDK.** A `ServiceFactory` built once from a bearer token keeps that token after it expires. Rebuild the `ServiceFactory` and its proxies with a fresh token before expiry. This is the only advice consistent with the documentation: the package `Relativity.Kepler.Client.SDK` (documented) exposes a settable `Token` and an `HttpMessageHandler` overload but no token-provider overload, and no page documents renewing a token behind a factory.\n\n**The client secret.** It is displayed once, at creation or reset. Store it as " + SecretManagementPointer.PART_B_NAME + " describes; never in source, never in tracked configuration, which in a VB.NET estate includes `app.config`, `web.config` and `My.Settings`. A reset invalidates the previous secret immediately, so plan the cut-over before resetting it.\n\n**Before deploying, confirm the calling identity.** Every call now runs as the OAuth2 Client's **Context User** — not as the user in the replaced credential, and for an import or a job as much as for a request. If that user has different workspace permissions or item-level security, the code returns fewer rows or `Unauthorized` in production with no change to its logic, and Relativity's audit attributes every call to that one account. Confirm the Context User's permissions match those of the previous identity. Relativity describes the Context User as usually a member of the Relativity Administrators group; the OAuth2 Client block (part 3) gives NSerio's recommendation, which differs.\n\n**When the integration acts for a signed-in user.** Client Credentials is one service identity by construction. An integration that acts on behalf of a signed-in user is the Authorization Code case, and moving to it means a new OAuth2 Client, because the flow is fixed when a client is created. The finding's flow note says when to make that choice.";

	/// <summary>Part 2 for Authorization Code: sign in, receive and exchange the code, and the two variants. Appended to every class's provenance share for a Code finding.</summary>
	public const string AUTHORIZATION_CODE_TOKEN_ACQUISITION =
		"The text a finding assigned Authorization Code (\"Code\") receives after part 1. It has a common part and two variants, **server-side** and **desktop**. The bold lead-in of each element is its marker. Every fact Relativity does not document but NSerio verified carries the exact label **" + OAuth2ClientConfigurationBlocks.NSERIO_VERIFIED_LABEL + "**.\n\n**Common to both variants**\n\n**Sign in.** Replace the code that collected the user's credentials with a redirect: send the user's browser to the authorize endpoint `<host>/Relativity/Identity/connect/authorize`, which leads to the RelativityOne login page. The authorize endpoint is **" + OAuth2ClientConfigurationBlocks.NSERIO_VERIFIED_LABEL + "**. The request carries the standard authorization-code parameters — `client_id`, `response_type=code`, the registered `redirect_uri`, a `scope`, and a `state` value the application checks on return; these are standard OAuth, and Relativity documents none of them. No discovery document is relied on.\n\n**Receive the code.** The browser returns to the Redirect URL registered on the OAuth2 Client, with the authorization code and the `state` value. Check the `state` before using the code. The Redirect URL must be exactly the one registered (the variants say how).\n\n**Exchange the code.** Send a form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=authorization_code`, the `code`, the same `redirect_uri`, the `client_id` and the `client_secret`, plus the `code_verifier` when PKCE is used. The code is exchanged at that endpoint, which Relativity documents for the Client Credentials grant only; the exchange is **" + OAuth2ClientConfigurationBlocks.NSERIO_VERIFIED_LABEL + "**.\n\n```vbnet\nDim form As New FormUrlEncodedContent(New Dictionary(Of String, String) From {\n\t{\"grant_type\", \"authorization_code\"},\n\t{\"code\", authorizationCode},\n\t{\"redirect_uri\", redirectUri},\n\t{\"client_id\", clientId},\n\t{\"client_secret\", clientSecret},\n\t{\"code_verifier\", codeVerifier}})\nUsing response As HttpResponseMessage = Await client.PostAsync(host & \"/Relativity/Identity/connect/token\", form)\n\tresponse.EnsureSuccessStatusCode()\nEnd Using\n```\n\n**Use the token.** Send `Authorization: Bearer <access token>` — the change at the use site in part 1 — with `X-CSRF-Header: -` on REST and Kepler calls.\n\n**On expiry.** With Allow Refresh Tokens enabled on the OAuth2 Client and PKCE used, the application holds a refresh token and, when the access token expires, sends a `grant_type=refresh_token` request with it and the client credentials in place of the code exchange — *inferred* from the standard OAuth shape, not tested. Without a refresh token the user signs in again. Relativity does not document a refresh token's lifetime, whether it rotates, how it is revoked, or whether a revocation endpoint exists, nor what disabling or deleting the client does to tokens already issued. A refresh token is a secret: store it as " + SecretManagementPointer.PART_B_NAME + " describes, and on compromise disable the client and replace it. Enable refresh tokens only if the application must keep a session beyond one token lifetime without asking the user to sign in again.\n\n**PKCE.** PKCE is required by Relativity's own documentation to obtain a refresh token, and not otherwise: a Code request that does not ask for a refresh token works without it, **" + OAuth2ClientConfigurationBlocks.NSERIO_VERIFIED_LABEL + "**. It is never a blanket requirement. NSerio recommends PKCE with the `S256` method in both variants (NSerio recommendation), and does not claim that Relativity accepts any particular method, which it does not document.\n\n**Scope.** No scope set is prescribed for Code. The scope below works — `openid RelativityRequestOrigin UserInfo UserInfoAccess AuthenticationID RelativityWeb` — **" + OAuth2ClientConfigurationBlocks.NSERIO_VERIFIED_LABEL + "**, verified as working and not as minimal. `state` is sent as standard OAuth practice and `nonce` is not used; Relativity does not document whether it enforces either.\n\n**The application stops collecting the password.** After this change the application never receives, stores or forwards the user's username and password: the user signs in on RelativityOne's own page, and the application holds only tokens. Remove the credential prompt, the form field and any storage of the password that the replaced code needed (in a VB.NET estate: the `InputBox`, the login form's password control, the Web Forms login control's handler).\n\n**Single sign-on.** Sign-in happens on RelativityOne's own login page, so the instance's single sign-on and two-factor rules apply unchanged — *inferred* from where the sign-in happens, and not documented for clients that customers create.\n\n**The client secret is required.** Code does not work without a client secret: tested and rejected, **" + OAuth2ClientConfigurationBlocks.NSERIO_VERIFIED_LABEL + "**, which confirms Relativity's own statement that the grant requires one. It is displayed once and is stored as " + SecretManagementPointer.PART_B_NAME + " describes.\n\n**Server-side variant.** For a web application whose code runs on a server — in VB.NET, an ASP.NET Web Forms page or a custom page whose code-behind sends the redirect (`Response.Redirect(authorizeUrl)`) and receives the code in a route of the application:\n\n- **Redirect.** The application receives the code at one exact HTTPS URL per environment, a route of the application, with no wildcards (NSerio recommendation; Relativity's matching rules are not documented). The Relativity administrator registers the exact URL on the OAuth2 Client.\n- **Secret and tokens.** The client secret stays on the server and never reaches the browser. The user's tokens are held server-side, keyed to the session, and never in the browser.\n- **Exchange.** The server performs the code exchange itself, from the code-behind or a handler.\n\n**Desktop variant.** For a desktop or command-line application run by a user on their own machine (WinForms, WPF, console):\n\n- **The system browser.** Open the authorize URL in the user's default browser and not in a web view the application hosts, so the application never sees the password (NSerio recommendation).\n- **A loopback redirect on a fixed port.** Listen on the loopback address only, on the registered port, for one request. The Redirect URL is `http://127.0.0.1:<port>/<path>`: a plain-HTTP loopback URL is accepted, **" + OAuth2ClientConfigurationBlocks.NSERIO_VERIFIED_LABEL + "**. The Relativity administrator registers that exact URL; because it is registered in advance, the port is fixed and not chosen at runtime.\n\n```vbnet\nUsing listener As New HttpListener()\n\tlistener.Prefixes.Add(\"http://127.0.0.1:<port>/<path>/\")\n\tlistener.Start()\n\tProcess.Start(New ProcessStartInfo(authorizeUrl) With {.UseShellExecute = True})\n\tDim context As HttpListenerContext = Await listener.GetContextAsync()\n\tDim authorizationCode As String = context.Request.QueryString(\"code\")\n\tDim returnedState As String = context.Request.QueryString(\"state\")\n\tcontext.Response.Close()\nEnd Using\n```\n\n- **The secret is extractable.** Code requires a client secret (above), so a distributed desktop application has to embed one, and **a secret shipped inside a distributed binary can be extracted by anyone who holds the binary**. It protects nothing against the person running the application.\n- **Compensating guidance** (NSerio recommendation, subject to your own security policy): set Trusted IPs where the users' outbound addresses are known and stable — they restrict where a token can be *requested*, and are checked when a token is issued against the client's ID and secret; use the recommended short Access Token Lifetime of 60 minutes; enable refresh tokens only when needed; and create one OAuth2 Client per distribution, so a leaked secret can be disabled without affecting the others.\n- **A known issue.** Relativity documents no recommendation for a distributed desktop application; this design is what NSerio verified works, and Relativity's own recommendation is tracked as a known issue of the release.";

	/// <summary>VB-001 generic source form.</summary>
	public const string VB_001_SOURCE_FORM =
		"**Applies to:** both (the SDK package is the same).\n\n```vbnet\nDim credentials As Credentials = New UsernamePasswordCredentials(userName, password)\nDim settings As New ServiceFactorySettings(restEndpointUri, credentials)\n```";

	/// <summary>VB-001 target form under token authentication.</summary>
	public const string VB_001_TARGET_FORM =
		"**First, the destination:** if this Relativity instance is not RelativityOne — including a Relativity Server instance, whose own documentation governs — this finding is informational and the rest of this template does not apply.\n\n```vbnet\nDim settings As New ServiceFactorySettings(restEndpointUri, New BearerTokenCredentials(accessToken))\n```\n\nThe REST URI is unchanged: `<scheme>://<machine name>/Relativity.Rest/api`.";

	/// <summary>VB-001 class share of the credential provenance.</summary>
	public const string VB_001_CREDENTIAL_PROVENANCE =
		"**Where the credentials come from.** The SDK type establishes that the destination is Relativity, not that it is RelativityOne; if it is not RelativityOne — including a Relativity Server instance, whose own documentation governs — the migration is that service's own token mechanism, this finding is informational for this tool, and the reader stops here. If it is RelativityOne, the token comes from the token-acquisition part that follows.";

	/// <summary>VB-001 verification note.</summary>
	public const string VB_001_VERIFICATION_NOTE =
		"**Verification note — two things, and the first can break working code.** **A token has a finite lifetime**, set per client in minutes, and this construct holds it. A `ServiceFactory` built once from `BearerTokenCredentials(accessToken)` and kept for the life of a process keeps that token after it expires, and the calls made through it then fail authentication. Rebuild the settings and the factory with a current token within the token's lifetime; the previous credential never expired, so nothing in the existing code does this. **Confirm the SDK version in use provides `BearerTokenCredentials`.** Where it does not, this is an SDK upgrade before it is a code change.";

	/// <summary>VB-002 generic source form.</summary>
	public const string VB_002_SOURCE_FORM =
		"**Applies to:** .NET Framework in practice (the legacy client assembly).\n\n```vbnet\nDim client As New RSAPIClient(rsapiUri, New UsernamePasswordCredentials(userName, password))\n```";

	/// <summary>VB-002 target form under token authentication.</summary>
	public const string VB_002_TARGET_FORM =
		"**First, the destination:** if this Relativity instance is not RelativityOne — including a Relativity Server instance, whose own documentation governs — this finding is informational and the rest of this template does not apply.\n\n**Two steps, not one, and in this order.** First, **move the call to Object Manager**: the RSAPI client is retired, so swapping the credential inside the RSAPI construction migrates the scheme of a call that should not survive. Rewrite the operation as the equivalent Object Manager call, reached through a service factory. Second, apply the token credential to that rewritten call:\n\n```vbnet\nDim settings As New ServiceFactorySettings(restEndpointUri, New BearerTokenCredentials(accessToken))\n```\n\nThe REST URI is `<scheme>://<machine name>/Relativity.Rest/api`, which is not the RSAPI endpoint the source form addressed.";

	/// <summary>VB-002 class share of the credential provenance.</summary>
	public const string VB_002_CREDENTIAL_PROVENANCE =
		"**Where the credentials come from.** The SDK type establishes that the destination is Relativity, not that it is RelativityOne; if it is not RelativityOne — including a Relativity Server instance, whose own documentation governs — the migration is that service's own token mechanism, this finding is informational for this tool, and the reader stops here. If it is RelativityOne, the token comes from the token-acquisition part that follows.";

	/// <summary>VB-002 verification note.</summary>
	public const string VB_002_VERIFICATION_NOTE =
		"**Verification note — four things, and the first two can break working code.** **Verify equivalence before rewriting.** Not every RSAPI operation has a direct Object Manager counterpart; where one does not, this is a redesign of that operation, not a migration of it. **A token has a finite lifetime**: a service factory built once from the token and kept for the life of a process keeps it after it expires; rebuild it with a current token within that lifetime. **Other RSAPI uses may not appear as findings.** This tool reports an RSAPI client only where it is constructed with a username-and-password credential. **Confirm the SDK version in use provides `BearerTokenCredentials` and the Object Manager service.**";

	/// <summary>VB-003 generic source form.</summary>
	public const string VB_003_SOURCE_FORM =
		"**Applies to:** both.\n\n```vbnet\ncache.Add(New Uri(instanceUrl), \"Basic\", New NetworkCredential(userName, password))\n```";

	/// <summary>VB-003 target form under token authentication.</summary>
	public const string VB_003_TARGET_FORM =
		"**First, the destination:** if it is not RelativityOne, this finding is informational — that service's own token mechanism is the migration — and the rest of this template does not apply.\n\nRemove the credential-cache entry and **set the bearer header on each request**:\n\n```vbnet\nrequest.Headers.Authorization = New AuthenticationHeaderValue(\"Bearer\", accessToken)\n```";

	/// <summary>VB-003 class share of the credential provenance.</summary>
	public const string VB_003_CREDENTIAL_PROVENANCE =
		"**Where the credentials come from.** The destination decides whether the migration applies. If the destination is not RelativityOne, the migration is that service's own token mechanism, this finding is informational for this tool, and the reader stops here. If it is RelativityOne, the token comes from the token-acquisition part that follows.";

	/// <summary>VB-003 verification note.</summary>
	public const string VB_003_VERIFICATION_NOTE =
		"**Verification note — three things.** **The 401-challenge response is lost**: a cache entry answers a server's challenge, a bearer header is sent up front and answers nothing; code or a server that depended on that exchange behaves differently after the change. **Redirects stop carrying the credential**: a manually set `Authorization` header is never re-applied to a redirect target, so code that relied on a redirect can start returning 401; address the final URI or handle the redirect explicitly. **Confirm the endpoint in use is the RelativityOne REST API**; an endpoint elsewhere reached from the same code may accept only Basic.";

	/// <summary>VB-004 generic source form.</summary>
	public const string VB_004_SOURCE_FORM =
		"**Applies to:** both.\n\n```vbnet\nclient.Authenticator = New HttpBasicAuthenticator(userName, password)\n```";

	/// <summary>VB-004 target form under token authentication.</summary>
	public const string VB_004_TARGET_FORM =
		"**First, the destination:** if it is not RelativityOne, this finding is informational — that service's own token mechanism is the migration — and the rest of this template does not apply.\n\nReplace the Basic authenticator with **the same library's bearer or token authenticator, carrying the access token**. This template names no target type: the construct is a type whose name names the scheme, and the token authenticator's name differs between libraries and versions.";

	/// <summary>VB-004 class share of the credential provenance.</summary>
	public const string VB_004_CREDENTIAL_PROVENANCE =
		"**Where the credentials come from.** The destination decides whether the migration applies. If the destination is not RelativityOne, the migration is that service's own token mechanism, this finding is informational for this tool, and the reader stops here. If it is RelativityOne, the token comes from the token-acquisition part that follows.";

	/// <summary>VB-004 verification note.</summary>
	public const string VB_004_VERIFICATION_NOTE =
		"**Verification note — three things.** **Confirm the library version in use offers a token authenticator**; where it does not, this is a library upgrade before it is a code change. **A token has a finite lifetime**: an authenticator set once on a client shared across requests holds a token that expires; pass a current token where the request is built. **Confirm the endpoint in use is the RelativityOne REST API.**";

	/// <summary>VB-005 generic source form.</summary>
	public const string VB_005_SOURCE_FORM =
		"**Applies to:** both.\n\n```vbnet\nDim authenticatedRequest = request.WithBasicAuth(userName, password)\n```";

	/// <summary>VB-005 target form under token authentication.</summary>
	public const string VB_005_TARGET_FORM =
		"**First, the destination:** if it is not RelativityOne, this finding is informational — that service's own token mechanism is the migration — and the rest of this template does not apply.\n\nReplace the Basic method with **the same library's bearer or token method, carrying the access token** in place of the credential pair. This template names no target method: the token method's name differs between libraries and versions.";

	/// <summary>VB-005 class share of the credential provenance.</summary>
	public const string VB_005_CREDENTIAL_PROVENANCE =
		"**Where the credentials come from.** The destination decides whether the migration applies. If the destination is not RelativityOne, the migration is that service's own token mechanism, this finding is informational for this tool, and the reader stops here. If it is RelativityOne, the token comes from the token-acquisition part that follows.";

	/// <summary>VB-005 verification note.</summary>
	public const string VB_005_VERIFICATION_NOTE =
		"**Verification note — three things.** **Confirm the library version in use offers a token method.** **A token has a finite lifetime**: applied once to a client or configuration shared across requests, the method holds a token that expires; pass a current token where the request is built. **Confirm the endpoint in use is the RelativityOne REST API.**";

	/// <summary>VB-006 generic source form.</summary>
	public const string VB_006_SOURCE_FORM =
		"**Applies to:** both (`HttpClient` is in both runtimes).\n\n```vbnet\nclient.DefaultRequestHeaders.Authorization = New AuthenticationHeaderValue(\"Basic\", encodedCredentials)\n```";

	/// <summary>VB-006 target form under token authentication.</summary>
	public const string VB_006_TARGET_FORM =
		"**First, the destination:** if it is not RelativityOne, this finding is informational — that service's own token mechanism is the migration — and the rest of this template does not apply.\n\n`\"Bearer\"` and the access token in place of `\"Basic\"` and the encoded pair, **set per request** rather than on the long-lived client's default headers:\n\n```vbnet\nrequest.Headers.Authorization = New AuthenticationHeaderValue(\"Bearer\", accessToken)\n```\n\nA per-request header needs an `HttpRequestMessage` sent with `SendAsync`; the `GetAsync` and `PostAsync` convenience calls take no request message, so calls written with them change shape. The code that produced `encodedCredentials` has no counterpart: a bearer token travels verbatim.";

	/// <summary>VB-006 class share of the credential provenance.</summary>
	public const string VB_006_CREDENTIAL_PROVENANCE =
		"**Where the credentials come from.** The destination decides whether the migration applies. If the destination is not RelativityOne, the migration is that service's own token mechanism, this finding is informational for this tool, and the reader stops here. If it is RelativityOne, the token comes from the token-acquisition part that follows.";

	/// <summary>VB-006 verification note.</summary>
	public const string VB_006_VERIFICATION_NOTE =
		"**Verification note — three things.** **Moving the header from the client to the request is part of the migration, not a style choice**: a token has a finite lifetime, and a header added once to `DefaultRequestHeaders` cannot be refreshed without mutating shared state. **The pair's encoding may be a separate finding**: where `encodedCredentials` is built in the same file, the tool reports the encoding and this header as two findings at two locations; they are one migration. **Confirm the endpoint in use is the RelativityOne REST API.**";

	/// <summary>VB-007 generic source form.</summary>
	public const string VB_007_SOURCE_FORM =
		"**Applies to:** both.\n\n```vbnet\nclient.Headers.Add(\"Authorization\", \"Basic \" & encodedCredentials)\n```";

	/// <summary>VB-007 target form under token authentication.</summary>
	public const string VB_007_TARGET_FORM =
		"**First, the destination:** if it is not RelativityOne, this finding is informational — that service's own token mechanism is the migration — and the rest of this template does not apply.\n\n`Bearer` and the access token in place of `Basic` and the encoded pair, **set per request**:\n\n```vbnet\nrequest.Headers.Add(\"Authorization\", \"Bearer \" & accessToken)\n```\n\nWhere the header is written on a `WebClient` (**.NET Framework 4.7.2** period form; obsolete on .NET in favour of `HttpClient`), set it before each call; on an `HttpClient`, on the `HttpRequestMessage`. The code that produced `encodedCredentials` has no counterpart: a bearer token travels verbatim.";

	/// <summary>VB-007 class share of the credential provenance.</summary>
	public const string VB_007_CREDENTIAL_PROVENANCE =
		"**Where the credentials come from.** The destination decides whether the migration applies. If the destination is not RelativityOne, the migration is that service's own token mechanism, this finding is informational for this tool, and the reader stops here. If it is RelativityOne, the token comes from the token-acquisition part that follows.";

	/// <summary>VB-007 verification note.</summary>
	public const string VB_007_VERIFICATION_NOTE =
		"**Verification note — three things.** **A header set once on a long-lived client cannot be refreshed**: set it per request, or the code works for the token's lifetime and then returns 401 until the process restarts. **The pair's encoding may be a separate finding**, at a second location, in the same migration. **Confirm the endpoint in use is the RelativityOne REST API.**";

	/// <summary>VB-008 generic source form.</summary>
	public const string VB_008_SOURCE_FORM =
		"**Applies to:** .NET Framework 4.7.2 period form (`WebClient`, `HttpWebRequest`); the same lines compile on .NET, where both types are obsolete in favour of `HttpClient`.\n\n```vbnet\nclient.Headers(HttpRequestHeader.Authorization) = \"Basic \" & encoded\nrequest.Headers(HttpRequestHeader.Authorization) = \"Basic \" & encoded\n```";

	/// <summary>VB-008 target form under token authentication.</summary>
	public const string VB_008_TARGET_FORM =
		"**First, the destination:** if it is not RelativityOne, this finding is informational — that service's own token mechanism is the migration — and the rest of this template does not apply.\n\n`\"Bearer \"` and the access token in place of `\"Basic \"` and the encoded pair, **set per request**:\n\n```vbnet\nclient.Headers(HttpRequestHeader.Authorization) = \"Bearer \" & accessToken\nrequest.Headers(HttpRequestHeader.Authorization) = \"Bearer \" & accessToken\n```\n\nThe code that produced `encoded` has no counterpart: a bearer token travels verbatim.";

	/// <summary>VB-008 class share of the credential provenance.</summary>
	public const string VB_008_CREDENTIAL_PROVENANCE =
		"**Where the credentials come from.** The destination decides whether the migration applies. If the destination is not RelativityOne, the migration is that service's own token mechanism, this finding is informational for this tool, and the reader stops here. If it is RelativityOne, the token comes from the token-acquisition part that follows.";

	/// <summary>VB-008 verification note.</summary>
	public const string VB_008_VERIFICATION_NOTE =
		"**Verification note — three things.** **A header set once cannot be refreshed**: an `HttpWebRequest` is one request, so its header is already per request; a `WebClient` reused across calls is the long-lived case, so set the header before each call with a current token. **The pair's encoding may be a separate finding.** **Confirm the endpoint in use is the RelativityOne REST API.**";

	/// <summary>VB-009 generic source form.</summary>
	public const string VB_009_SOURCE_FORM =
		"**Applies to:** .NET Framework in its classic WCF form.\n\n```vbnet\nbinding.Security.Transport.ClientCredentialType = HttpClientCredentialType.Basic\nclient.ClientCredentials.UserName.UserName = userName\nclient.ClientCredentials.UserName.Password = password\n```";

	/// <summary>VB-009 target form under token authentication.</summary>
	public const string VB_009_TARGET_FORM =
		"**First, the destination:** if it is not RelativityOne, this finding is informational — that service's own token mechanism is the migration — and the rest of this template does not apply.\n\nThe binding's **transport credential type becomes the token form that binding supports**, and the user name and password assignments are removed — from code, and from configuration where the credential lives there instead. No code form is given: which token form a binding and a service accept is decided by the service, not by the client. **If the SOAP service is Relativity's, confirm it accepts bearer at all before changing the binding.** Where it does not, there is no scheme to migrate to on that service.";

	/// <summary>VB-009 class share of the credential provenance.</summary>
	public const string VB_009_CREDENTIAL_PROVENANCE =
		"**Where the credentials come from.** The destination decides whether the migration applies. If the destination is not RelativityOne, the migration is that service's own token mechanism, this finding is informational for this tool, and the reader stops here. If it is RelativityOne, the token comes from the token-acquisition part that follows.";

	/// <summary>VB-009 verification note.</summary>
	public const string VB_009_VERIFICATION_NOTE =
		"**Verification note — three things.** **A SOAP or WCF service may not accept bearer at all**: this is the first check, not the last. **The credential may not be in this file**: the binding is what this finding reports; its user name and password may live in `app.config` or `web.config`, which this tool does not scan. **A token has a finite lifetime**: a client proxy configured once and kept for the process's life holds a token that expires; replace it on the proxy or recreate the proxy.";

	/// <summary>VB-010 generic source form.</summary>
	public const string VB_010_SOURCE_FORM =
		"**Applies to:** both in syntax; the documented factory is a Relativity Server page (below).\n\n```vbnet\nDim importApi As New ImportAPI(userName, password, webServiceUrl)\n```";

	/// <summary>VB-010 target form under token authentication.</summary>
	public const string VB_010_TARGET_FORM =
		"**First, the destination:** if this Relativity instance is not RelativityOne — including a Relativity Server instance, whose own documentation governs — this finding is informational and the rest of this template does not apply.\n\n**Change what the entry point receives, not the call site.** Relativity's Import API documentation names one static factory on the type, `ImportAPI.CreateByRsaBearerToken(webServiceUrl)`. **It takes the web service URL and no token**: it uses the bearer token of the identity of the process that runs it, and the documentation says it should be used only by processes hosted by the Relativity Service Account, such as an agent. Where the code runs in such a host, the construction above is replaced by:\n\n```vbnet\nDim importApi As ImportAPI = ImportAPI.CreateByRsaBearerToken(webServiceUrl)\n```\n\n**For a standalone or desktop process that would obtain an OAuth2 token itself, this template gives no code form.** The documentation names no `ImportAPI` factory that accepts a token acquired by the caller, and it lists bearer-token authentication as not supported for a desktop application, where user name and password is the preferred form; that is a known issue of the release, not something this text resolves. A subtype of the entry point written by the code (a class that inherits it) is changed where its base is constructed. The migrated construction must not keep the three-argument form with a credential pair, which is the one this finding reports.";

	/// <summary>VB-010 class share of the credential provenance.</summary>
	public const string VB_010_CREDENTIAL_PROVENANCE =
		"**Where the credentials come from.** The SDK type establishes that the destination is Relativity, not that it is RelativityOne; if it is not RelativityOne the migration is that service's own token mechanism, this finding is informational for this tool, and the reader stops here. If it is RelativityOne, the factory above carries no token of the code's own: the identity comes from the hosting process, so the token-acquisition part that follows applies only to code that obtains a token itself, and for the entry point that case is the known issue stated above.";

	/// <summary>VB-010 verification note.</summary>
	public const string VB_010_VERIFICATION_NOTE =
		"**Verification note — two things, and the first can break working code.** **The factory is documented for hosted processes only, and for Relativity Server.** It is meant for an agent or another process hosted by the Relativity Service Account; the two documentation pages that describe it are Relativity Server pages, so confirm that the SDK version in use for RelativityOne exposes it before relying on it. **Confirm the SDK version in use exposes `CreateByRsaBearerToken`.** Where it does not, this is an SDK upgrade before it is a code change; the documentation states that the factory handles authentication and token expiration itself.";

	/// <summary>VB-011 generic source form.</summary>
	public const string VB_011_SOURCE_FORM =
		"**Applies to:** both.\n\n```vbnet\nWith importJob.Settings\n\t.RelativityUsername = userName\n\t.RelativityPassword = password\nEnd With\n```";

	/// <summary>VB-011 target form under token authentication.</summary>
	public const string VB_011_TARGET_FORM =
		"**First, the destination:** if this Relativity instance is not RelativityOne — including a Relativity Server instance, whose own documentation governs — this finding is informational and the rest of this template does not apply.\n\nReplace the user name and password properties with **the settings object's token-bearing equivalent**, assigned the access token, and remove both credential assignments. This template does not name that member, because which one exists depends on the SDK version. **Confirm the SDK version in use exposes one; where it does not, this is an SDK upgrade before it is a code change.**";

	/// <summary>VB-011 class share of the credential provenance.</summary>
	public const string VB_011_CREDENTIAL_PROVENANCE =
		"**Where the credentials come from.** The property names establish that the destination is Relativity, not that it is RelativityOne; if it is not RelativityOne the migration is that service's own token mechanism, this finding is informational for this tool, and the reader stops here. If it is RelativityOne, the token comes from the token-acquisition part that follows.";

	/// <summary>VB-011 verification note.</summary>
	public const string VB_011_VERIFICATION_NOTE =
		"**Verification note — two things. The first decides whether the change can be made at all.** **SDK version support comes first**: where the version in use has no token-bearing settings member, the change cannot be made at this call site at all. **A token has a finite lifetime**: a settings object populated once and reused across long or repeated jobs holds a token that expires; populate it with a current token for each job.";

	/// <summary>VB-012 generic source form.</summary>
	public const string VB_012_SOURCE_FORM =
		"**Applies to:** both; the HTTP type differs (`HttpClientHandler` on both; `WebClient` and `HttpWebRequest` period form on .NET Framework).\n\n```vbnet\nDim handler As New HttpClientHandler With {.Credentials = New NetworkCredential(userName, password)}\n```";

	/// <summary>VB-012 target form under token authentication.</summary>
	public const string VB_012_TARGET_FORM =
		"**First, the destination:** if it is not RelativityOne, this finding is informational — that service's own token mechanism is the migration — and the rest of this template does not apply.\n\n**Drop the `Credentials` assignment** from the handler, keeping its other settings, and **set the bearer header per request**:\n\n```vbnet\nDim handler As New HttpClientHandler()\nrequest.Headers.Authorization = New AuthenticationHeaderValue(\"Bearer\", accessToken)\n```";

	/// <summary>VB-012 class share of the credential provenance.</summary>
	public const string VB_012_CREDENTIAL_PROVENANCE =
		"**Where the credentials come from.** The destination decides whether the migration applies. If the destination is not RelativityOne, the migration is that service's own token mechanism, this finding is informational for this tool, and the reader stops here. If it is RelativityOne, the token comes from the token-acquisition part that follows.";

	/// <summary>VB-012 verification note.</summary>
	public const string VB_012_VERIFICATION_NOTE =
		"**Verification note — five things.** **Confirm the exchange was Basic**: `NetworkCredential` declares no scheme, and a handler carrying one serves integrated Windows authentication as readily as Basic; if the server negotiated an integrated scheme, this construct is not Basic Authentication. **The 401-challenge response is lost**: a bearer header is sent up front and answers nothing. **Redirects stop carrying the credential**: a manually set `Authorization` header is never re-applied; address the final URI or handle the redirect explicitly. **A token has a finite lifetime**: set the header per request, not on the client's default headers. **Confirm the endpoint in use is the RelativityOne REST API.**";

	/// <summary>VB-013 generic source form.</summary>
	public const string VB_013_SOURCE_FORM =
		"**Applies to:** both.\n\n```vbnet\nDim credential As New NetworkCredential(userName, password)\n```";

	/// <summary>VB-013 target form under token authentication.</summary>
	public const string VB_013_TARGET_FORM =
		"**First, the destination:** if it is not RelativityOne, this finding is informational — that service's own token mechanism is the migration — and the rest of this template does not apply.\n\n**Identify the consumer first** — the fix may be outside the reported file, because this finding reports a credential whose consumer the tool did not recognize in it. The form of the change depends on what consumes the credential:\n\n- an `HttpClient` handler: remove the credential from the handler and set the `Authorization` header to `Bearer` and the token on each request message;\n- `WebClient` or `HttpWebRequest`: remove the credential and set the `Authorization` header to `Bearer` and the token on each request;\n- a SOAP proxy: the binding changes, and the service may not accept bearer at all;\n- a platform factory that takes the credential to log a user in: the token-bearing form of that factory, where the SDK offers one;\n- a mail, file-transfer, directory or proxy consumer: not a migration target for this tool.\n\nNo code form is given, because none is correct for all.";

	/// <summary>VB-013 class share of the credential provenance.</summary>
	public const string VB_013_CREDENTIAL_PROVENANCE =
		"**Where the credentials come from.** The destination decides whether the migration applies. If the destination is not RelativityOne, the migration is that service's own token mechanism, this finding is informational for this tool, and the reader stops here. If it is RelativityOne, the token comes from the token-acquisition part that follows.";

	/// <summary>VB-013 verification note.</summary>
	public const string VB_013_VERIFICATION_NOTE =
		"**Verification note — seven things.** **This finding is the least characterized of any template**: the consumer was not recognized, so neither the scheme nor the destination has been established; treat it as the location of a credential to trace. **Confirm the exchange was Basic.** **The 401-challenge response is lost** wherever the consumer answered a challenge with this credential. **Redirects stop carrying the credential.** **A SOAP service may not accept bearer at all.** **Confirm the endpoint in use is the RelativityOne REST API.** **A token has a finite lifetime**: a header must be set per request to be refreshable.";

	/// <summary>VB-014 generic source form.</summary>
	public const string VB_014_SOURCE_FORM =
		"**Applies to:** both.\n\n```vbnet\nDim pair As String = userName & \":\" & password\nDim encoded As String = Convert.ToBase64String(Encoding.ASCII.GetBytes(pair))\n```";

	/// <summary>VB-014 target form under token authentication.</summary>
	public const string VB_014_TARGET_FORM =
		"**First, the destination:** if it is not RelativityOne, this finding is informational — that service's own token mechanism is the migration — and the rest of this template does not apply.\n\n**Only if this encoding feeds an `Authorization` header.** Base64 over a colon-separated pair is not always a credential: the same shape encodes other data. Where it does, **delete the encoding**: bearer carries the token verbatim, with no Base64 of a credential pair, and the header that received `encoded` receives the token, set per request:\n\n```vbnet\nrequest.Headers.Authorization = New AuthenticationHeaderValue(\"Bearer\", accessToken)\n```";

	/// <summary>VB-014 class share of the credential provenance.</summary>
	public const string VB_014_CREDENTIAL_PROVENANCE =
		"**Where the credentials come from.** The destination decides whether the migration applies. If the destination is not RelativityOne, the migration is that service's own token mechanism, this finding is informational for this tool, and the reader stops here. If it is RelativityOne, the token comes from the token-acquisition part that follows.";

	/// <summary>VB-014 verification note.</summary>
	public const string VB_014_VERIFICATION_NOTE =
		"**Verification note — four things.** **Confirm the pair is a credential sent outward**: a service that *checks* Basic from its own callers is inbound authentication, which has nothing to migrate. **The header write may be a separate finding** at a second location, in the same migration. **A token has a finite lifetime, and a header set once on a long-lived client cannot be refreshed**: set it per request. **Confirm the endpoint in use is the RelativityOne REST API.**";

	/// <summary>VB-015 generic source form.</summary>
	public const string VB_015_SOURCE_FORM =
		"**Applies to:** both.\n\n```vbnet\nDim pair As String = \"\" & \":\" & accessToken\nDim encoded As String = Convert.ToBase64String(Encoding.ASCII.GetBytes(pair))\n```";

	/// <summary>VB-015 target form under token authentication.</summary>
	public const string VB_015_TARGET_FORM =
		"**This change applies to any destination that accepts `Bearer`**, RelativityOne or not: no credential is exchanged, so only the token's provenance differs when the destination is not RelativityOne.\n\n**Delete the encoding and change the prefix.** The token already in hand is carried verbatim:\n\n```text\nAuthorization: Basic <base64 of \":\" + token>      ->      Authorization: Bearer <token>\n```\n\n```vbnet\nrequest.Headers.Authorization = New AuthenticationHeaderValue(\"Bearer\", accessToken)\n```\n\nNo credential is exchanged and no client is registered — this is the cheapest remediation of any template.";

	/// <summary>VB-015 class share of the credential provenance.</summary>
	public const string VB_015_CREDENTIAL_PROVENANCE =
		"**Where the credentials come from.** The prefix change applies to any destination that accepts `Bearer`. If the destination is RelativityOne, the token that belongs in the header is obtained as the token-acquisition part that follows describes. If it is not RelativityOne, the token's provenance is that service's and does not change: the token already in hand is the one that moves to the `Bearer` prefix.";

	/// <summary>VB-015 verification note.</summary>
	public const string VB_015_VERIFICATION_NOTE =
		"**Verification note — two things.** **Verify the destination accepts `Bearer` before changing the prefix**: some services accept only Basic. **A header set once on a long-lived client cannot be refreshed**: set it per request with a current token.";

	/// <summary>VB-016 generic source form.</summary>
	public const string VB_016_SOURCE_FORM =
		"**Applies to:** .NET Framework, and .NET only in Windows desktop applications (`net*-windows` with Windows Forms), where `My.Computer.Network` exists.\n\n```vbnet\nMy.Computer.Network.DownloadFile(address, destinationPath, userName, password, False, 100000, True)\n```";

	/// <summary>VB-016 target form under token authentication.</summary>
	public const string VB_016_TARGET_FORM =
		"**First, the destination:** if it is not RelativityOne, this finding is informational — that service's own token mechanism is the migration — and the rest of this template does not apply.\n\n`My.Computer.Network` offers no token-bearing overload. **Replace the call with an `HttpClient` request carrying the bearer header**, set per request, and write the response stream to the destination path:\n\n```vbnet\nUsing request As New HttpRequestMessage(HttpMethod.Get, address)\n\trequest.Headers.Authorization = New AuthenticationHeaderValue(\"Bearer\", accessToken)\n\tUsing response As HttpResponseMessage = Await client.SendAsync(request)\n\t\tresponse.EnsureSuccessStatusCode()\n\t\tUsing target As IO.FileStream = IO.File.Create(destinationPath)\n\t\t\tAwait response.Content.CopyToAsync(target)\n\t\tEnd Using\n\tEnd Using\nEnd Using\n```";

	/// <summary>VB-016 class share of the credential provenance.</summary>
	public const string VB_016_CREDENTIAL_PROVENANCE =
		"**Where the credentials come from.** The destination decides whether the migration applies. If the destination is not RelativityOne, the migration is that service's own token mechanism, this finding is informational for this tool, and the reader stops here. If it is RelativityOne, the token comes from the token-acquisition part that follows.";

	/// <summary>VB-016 verification note.</summary>
	public const string VB_016_VERIFICATION_NOTE =
		"**Verification note — three things.** **Confirm the address is an HTTP address.** The same call transfers over FTP, and a credential given for an `ftp://` address is not Basic over HTTP and not a migration target for this tool. **Confirm the exchange was Basic**: the call answers a server's challenge with the credential, and a server that negotiates an integrated scheme is not Basic Authentication. **The 401-challenge response is lost**: a bearer header is sent up front and answers nothing; progress display and the connection timeout of the original call have no counterpart in the replacement and are set on the client.";

	/// <summary>VB-017 generic source form.</summary>
	public const string VB_017_SOURCE_FORM =
		"**Applies to:** both in syntax; the COM objects are Windows components.\n\n```vbnet\nDim http As Object = CreateObject(\"MSXML2.ServerXMLHTTP.6.0\")\nhttp.Open(\"GET\", url, False, userName, password)\n```";

	/// <summary>VB-017 target form under token authentication.</summary>
	public const string VB_017_TARGET_FORM =
		"**First, the destination:** if it is not RelativityOne, this finding is informational — that service's own token mechanism is the migration — and the rest of this template does not apply.\n\n**Drop the user name and password from `Open`** (or remove `SetCredentials`) and **set the bearer header per request** before sending:\n\n```vbnet\nDim http As Object = CreateObject(\"MSXML2.ServerXMLHTTP.6.0\")\nhttp.Open(\"GET\", url, False)\nhttp.setRequestHeader(\"Authorization\", \"Bearer \" & accessToken)\nhttp.send()\n```\n\nWhere the code is being moved off late-bound COM, an `HttpClient` call is the target of the Part 1 forms above.";

	/// <summary>VB-017 class share of the credential provenance.</summary>
	public const string VB_017_CREDENTIAL_PROVENANCE =
		"**Where the credentials come from.** The destination decides whether the migration applies. If the destination is not RelativityOne, the migration is that service's own token mechanism, this finding is informational for this tool, and the reader stops here. If it is RelativityOne, the token comes from the token-acquisition part that follows.";

	/// <summary>VB-017 verification note.</summary>
	public const string VB_017_VERIFICATION_NOTE =
		"**Verification note — three things.** **The 401-challenge response is lost**: the object answered a challenge with the credential; a bearer header is sent up front. **Confirm the exchange was Basic**: the object also negotiates integrated schemes. **A token has a finite lifetime**: set the header on each request, not once on a long-lived object.";
}
