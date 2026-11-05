using MigrateAuth.Cli.Reporting;

namespace MigrateAuth.Cli.Catalog.Languages;

/// <summary>
/// The catalog's recommendation text for `CSharp`, encoded from the catalog
/// (its version is <see cref="LanguageCatalog.CurrentVersion"/>): seventeen classes, both flows, and both generic fallback texts.
/// CS-011 and CS-019 carry no entry: the catalog's "Classes without a template" routes both to
/// manual review.
///
/// HOW A RECOMMENDATION IS COMPOSED. Part 1 is the class's four fields and is flow-neutral, so the
/// source form, the target form and the verification note are the same text for both flows. The
/// credential provenance is the class's own share followed by part 2 for the finding's flow, held
/// once here as <see cref="CLIENT_CREDENTIALS_TOKEN_ACQUISITION"/> and
/// <see cref="AUTHORIZATION_CODE_TOKEN_ACQUISITION"/>. Part 3, the OAuth2 Client block, is appended
/// by the shared engine's composer and is not repeated here.
///
/// ENCODING. Every secret-handling sentence names
/// <see cref="SecretManagementPointer.PART_B_NAME"/> and the label of a fact NSerio verified but
/// Relativity does not document is <see cref="OAuth2ClientConfigurationBlocks.NSERIO_VERIFIED_LABEL"/>,
/// both through the constants the shared engine already owns.
///
/// A SEPARATE FILE BECAUSE <see cref="CSharpAnalyzer"/> IS <c>file</c>-SCOPED. The analyzer type in
/// <c>EmbeddedLanguageAnalyzers.CSharp.cs</c> cannot be split across files, so the text lives in this
/// <c>internal</c> holder and the analyzer's template provider reads it by the same identifiers.
/// </summary>
internal static class CSharpRecommendationTemplates
{
	/// <summary>The Client Credentials generic fallback prose, the catalog's "Generic fallback content for CSharp", with the secret-handling sentence naming the Part B pointer.</summary>
	public const string CSHARP_LANGUAGE_FALLBACK_TEXT =
		"The tool reports Basic Authentication at this location but did not recognize the construct, so the change has not been characterized and no before-and-after form is offered here. If the destination is not RelativityOne, Relativity's identity endpoint does not issue tokens for it: the migration is that service's own, and this finding is informational. If the destination is RelativityOne, Basic Authentication is no longer documented for its REST API. The documented destination in C# is either the Relativity SDK's bearer-token credential type, where the code uses the SDK, or a bearer `Authorization` header, where it builds requests itself; either carries a token obtained from the platform's identity endpoint with the client-credentials grant. The client ID and secret that obtain that token belong in the process environment or a secret store, per " + SecretManagementPointer.PART_B_NAME + ", and never in source or tracked configuration. Beyond those facts this is a pointer and not an instruction: the migration must be designed from the code by whoever owns it, rather than applied from this text.";

	/// <summary>The Authorization Code generic fallback prose, the catalog's "Generic fallback content for CSharp, Authorization Code".</summary>
	public const string CSHARP_CODE_LANGUAGE_FALLBACK_TEXT =
		"The tool reports Basic Authentication at this location but did not recognize the construct, so the change has not been characterized and no before-and-after form is offered here. If the destination is not RelativityOne, Relativity's identity endpoint does not issue tokens for it: the migration is that service's own, and this finding is informational. If the destination is RelativityOne, Basic Authentication is no longer documented for its REST API, and this finding is assigned the Authorization Code flow, in which the application sends the user to RelativityOne to sign in, receives an authorization code at a Redirect URL registered on its OAuth2 Client, and exchanges the code for an access token that it sends as a Bearer token. The authorize endpoint is `<host>/Relativity/Identity/connect/authorize` and the code is exchanged at `<host>/Relativity/Identity/connect/token`; both are " + OAuth2ClientConfigurationBlocks.NSERIO_VERIFIED_LABEL + ". PKCE is required by Relativity's own documentation to obtain a refresh token, and not otherwise. The client ID and secret belong in the process environment or a secret store, per " + SecretManagementPointer.PART_B_NAME + ", and never in source or tracked configuration; Code requires a client secret. After the change the application no longer collects or forwards the user's password. Beyond those facts this is a pointer and not an instruction: the migration must be designed from the code by whoever owns it, rather than applied from this text.";

	/// <summary>The catalog's "Part 2, Client Credentials": how the token is obtained and kept. Appended to every class's credential-provenance share for a Client Credentials finding.</summary>
	public const string CLIENT_CREDENTIALS_TOKEN_ACQUISITION =
		"**Where the token comes from.** Create an OAuth2 Client with Flow Grant Type set to Client Credentials (part 3 lists its settings), then send a form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=client_credentials`, `client_id`, `client_secret` and a `scope`. The token endpoint is **documented** for this grant. The token is then sent as `Authorization: Bearer <token>`, with `X-CSRF-Header: -` on REST and Kepler calls — the header is documented as required, and a Kepler call without it returns 404.\n\n**The scope depends on what the integration needs, and no single scope is required.** Relativity's documentation shows two values for this grant: `SystemUserInfo` on the REST authentication page, the Usage Reports page and in the official Transfer SDK sample, and `UserInfoAccess` in the Kepler .NET samples. No page says which is correct, whether both work, or what either grants, and the OAuth2 Client's `Scopes` property is documented as one that cannot be set. Use the value the API you call documents, and confirm it.\n\n**Cache the token, and re-acquire it on expiry — do not refresh.** Keep the token for its lifetime (part 3 recommends 60 minutes, Relativity's own wording) and request a new one with the client secret when it expires or a call is rejected for authentication. Client Credentials does not offer refresh tokens: **" + OAuth2ClientConfigurationBlocks.NSERIO_VERIFIED_LABEL + "** — the Allow Refresh Tokens control does not appear for this grant in the RelativityOne user interface. Relativity's own tables and text on refresh tokens conflict with each other, so re-acquisition with the secret is the advice that holds whichever way they are read.\n\n**Long-running processes that use the Relativity SDK.** A `ServiceFactory` built once from a bearer token keeps that token after it expires. Rebuild the `ServiceFactory` and its proxies with a fresh token before expiry. This is the only advice consistent with the documentation: the package `Relativity.Kepler.Client.SDK` (documented) exposes a settable `Token` and an `HttpMessageHandler` overload but no token-provider overload, and no page documents renewing a token behind a factory.\n\n**The client secret.** It is displayed once, at creation or reset. Store it as " + SecretManagementPointer.PART_B_NAME + " describes; never in source, never in tracked configuration. A reset invalidates the previous secret immediately, so plan the cut-over before resetting it.\n\n**Before deploying, confirm the calling identity.** Every call now runs as the OAuth2 Client's **Context User** — not as the user in the replaced credential, and for an import or a job as much as for a request. If that user has different workspace permissions or item-level security, the code returns fewer rows or `Unauthorized` in production with no change to its logic, and Relativity's audit attributes every call to that one account. Confirm the Context User's permissions match those of the previous identity. Relativity's REST API authentication page describes the Context User as usually a member of the Relativity Administrators group; the OAuth2 Client block (part 3) gives NSerio's recommendation, which differs.\n\n**When the integration acts for a signed-in user.** Client Credentials is one service identity by construction. An integration that acts on behalf of a signed-in user is the Authorization Code case, and moving to it means a new OAuth2 Client, because the flow is fixed when a client is created. The finding's flow note says when to make that choice.";

	/// <summary>The catalog's "Part 2, Authorization Code": the common part and both variants, each element under its bold lead-in. Appended to every class's credential-provenance share for an Authorization Code finding.</summary>
	public const string AUTHORIZATION_CODE_TOKEN_ACQUISITION =
		"**Common to both variants**\n\n**Sign in.** Replace the code that collected the user's credentials with a redirect: send the user's browser to the authorize endpoint `<host>/Relativity/Identity/connect/authorize`, which leads to the RelativityOne login page. The authorize endpoint is **" + OAuth2ClientConfigurationBlocks.NSERIO_VERIFIED_LABEL + "**. The request carries the standard authorization-code parameters — `client_id`, `response_type=code`, the registered `redirect_uri`, a `scope`, and a `state` value the application checks on return; these are standard OAuth, and Relativity documents none of them. No discovery document is relied on.\n\n**Receive the code.** The browser returns to the Redirect URL registered on the OAuth2 Client, with the authorization code and the `state` value. Check the `state` before using the code. The Redirect URL must be exactly the one registered (the variants say how).\n\n**Exchange the code.** Send a form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=authorization_code`, the `code`, the same `redirect_uri`, the `client_id` and the `client_secret`, plus the `code_verifier` when PKCE is used. The code is exchanged at that endpoint, which Relativity documents for the Client Credentials grant only; the exchange is **" + OAuth2ClientConfigurationBlocks.NSERIO_VERIFIED_LABEL + "**.\n\n**Use the token.** Send `Authorization: Bearer <access token>` — the change at the use site in part 1 — with `X-CSRF-Header: -` on REST and Kepler calls.\n\n**On expiry.** With Allow Refresh Tokens enabled on the OAuth2 Client and PKCE used, the application holds a refresh token and, when the access token expires, sends a `grant_type=refresh_token` request with it and the client credentials in place of the code exchange — *inferred* from the standard OAuth shape, not tested. Without a refresh token the user signs in again. Relativity does not document a refresh token's lifetime, whether it rotates, how it is revoked, or whether a revocation endpoint exists, nor what disabling or deleting the client does to tokens already issued. A refresh token is a secret: store it as " + SecretManagementPointer.PART_B_NAME + " describes, and on compromise disable the client and replace it. Enable refresh tokens only if the application must keep a session beyond one token lifetime without asking the user to sign in again.\n\n**PKCE.** PKCE is required by Relativity's own documentation to obtain a refresh token, and not otherwise: a Code request that does not ask for a refresh token works without it, **" + OAuth2ClientConfigurationBlocks.NSERIO_VERIFIED_LABEL + "**. It is never a blanket requirement. NSerio recommends PKCE with the `S256` method in both variants (NSerio recommendation), and does not claim that Relativity accepts any particular method, which it does not document.\n\n**Scope.** No scope set is prescribed for Code. The scope below works — `openid RelativityRequestOrigin UserInfo UserInfoAccess AuthenticationID RelativityWeb` — **" + OAuth2ClientConfigurationBlocks.NSERIO_VERIFIED_LABEL + "**, verified as working and not as minimal. `state` is sent as standard OAuth practice and `nonce` is not used; Relativity does not document whether it enforces either.\n\n**The application stops collecting the password.** After this change the application never receives, stores or forwards the user's username and password: the user signs in on RelativityOne's own page, and the application holds only tokens. Remove the credential prompt, the form field and any storage of the password that the replaced code needed.\n\n**Single sign-on.** Sign-in happens on RelativityOne's own login page, so the instance's single sign-on and two-factor rules apply unchanged — *inferred* from where the sign-in happens, and not documented for clients that customers create.\n\n**The client secret is required.** Code does not work without a client secret: tested and rejected, **" + OAuth2ClientConfigurationBlocks.NSERIO_VERIFIED_LABEL + "**, which confirms Relativity's own statement that the grant requires one. It is displayed once and is stored as " + SecretManagementPointer.PART_B_NAME + " describes.\n\n**Server-side variant.** For a web application whose code runs on a server:\n\n- **Redirect.** The application receives the code at one exact HTTPS URL per environment, a route of the application, with no wildcards (NSerio recommendation; Relativity's matching rules are not documented). The Relativity administrator registers the exact URL on the OAuth2 Client.\n- **Secret and tokens.** The client secret stays on the server and never reaches the browser. The user's tokens are held server-side, keyed to the session, and never in the browser.\n- **Exchange.** The server performs the code exchange itself.\n\n**Desktop variant.** For a desktop or command-line application run by a user on their own machine:\n\n- **The system browser.** Open the authorize URL in the user's default browser and not in a web view the application hosts, so the application never sees the password (NSerio recommendation).\n- **A loopback redirect on a fixed port.** Listen on the loopback address only, on the registered port, for one request. The Redirect URL is `http://127.0.0.1:<port>/<path>`: a plain-HTTP loopback URL is accepted, **" + OAuth2ClientConfigurationBlocks.NSERIO_VERIFIED_LABEL + "**. The Relativity administrator registers that exact URL; because it is registered in advance, the port is fixed and not chosen at runtime.\n- **The secret is extractable.** Code requires a client secret (above), so a distributed desktop application has to embed one, and **a secret shipped inside a distributed binary can be extracted by anyone who holds the binary**. It protects nothing against the person running the application.\n- **Compensating guidance** (NSerio recommendation, subject to your own security policy): set Trusted IPs where the users' outbound addresses are known and stable — they restrict where a token can be *requested*, and are checked when a token is issued against the client's ID and secret; use the recommended short Access Token Lifetime of 60 minutes; enable refresh tokens only when needed; and create one OAuth2 Client per distribution, so a leaked secret can be disabled without affecting the others.\n- **A known issue.** Relativity documents no recommendation for a distributed desktop application; this design is what NSerio verified works, and Relativity's own recommendation is tracked as a known issue of the release.";

	/// <summary>CS-001 generic source form.</summary>
	public const string CS_001_SOURCE_FORM =
		"```csharp\nCredentials credentials = new UsernamePasswordCredentials(userName, password);\nServiceFactorySettings settings = new ServiceFactorySettings(restEndpointUri, credentials);\n```";

	/// <summary>CS-001 target form under token authentication.</summary>
	public const string CS_001_TARGET_FORM =
		"**First, the destination:** if this Relativity instance is not RelativityOne — including a Relativity Server instance, whose own documentation governs — this finding is informational and the rest of this template does not apply.\n\n```csharp\nServiceFactorySettings settings = new ServiceFactorySettings(\n\trestEndpointUri, new BearerTokenCredentials(accessToken));\n```\n\nThe REST URI is unchanged: `<scheme>://<machine name>/Relativity.Rest/api`.";

	/// <summary>CS-001 class share of the credential provenance.</summary>
	public const string CS_001_CREDENTIAL_PROVENANCE =
		"**Where the credentials come from.** The SDK type establishes that the destination is Relativity, not that it is RelativityOne; if it is not RelativityOne — including a Relativity Server instance, whose own documentation governs — the migration is that service's own token mechanism, this finding is informational for this tool, and no migration to RelativityOne applies. If it is RelativityOne, the token comes from the token-acquisition part that follows.";

	/// <summary>CS-001 verification note.</summary>
	public const string CS_001_VERIFICATION_NOTE =
		"**Verification note — two things, and the first can break working code.**\n\n**A token has a finite lifetime**, set per client in minutes, and this construct holds it. A `ServiceFactory` built once from `BearerTokenCredentials(accessToken)` and kept for the life of a process keeps that token after it expires, and the calls made through it then fail authentication. Rebuild the settings and the factory with a current token within the token's lifetime; the previous credential never expired, so nothing in the existing code does this.\n\n**Confirm the SDK version in use provides `BearerTokenCredentials`.** Where it does not, this is an SDK upgrade before it is a code change.";

	/// <summary>CS-002 generic source form.</summary>
	public const string CS_002_SOURCE_FORM =
		"```csharp\nvar client = new RSAPIClient(rsapiUri, new UsernamePasswordCredentials(userName, password));\n```";

	/// <summary>CS-002 target form under token authentication.</summary>
	public const string CS_002_TARGET_FORM =
		"**First, the destination:** if this Relativity instance is not RelativityOne — including a Relativity Server instance, whose own documentation governs — this finding is informational and the rest of this template does not apply.\n\n**Two steps, not one, and in this order.** First, **move the call to Object Manager**: the RSAPI client is retired, so swapping the credential inside the RSAPI construction migrates the scheme of a call that should not survive. Rewrite the operation as the equivalent Object Manager call, reached through a service factory. Second, apply the token credential to that rewritten call:\n\n```csharp\nServiceFactorySettings settings = new ServiceFactorySettings(\n\trestEndpointUri, new BearerTokenCredentials(accessToken));\n```\n\nThe REST URI is `<scheme>://<machine name>/Relativity.Rest/api`, which is not the RSAPI endpoint the source form addressed.";

	/// <summary>CS-002 class share of the credential provenance.</summary>
	public const string CS_002_CREDENTIAL_PROVENANCE =
		"**Where the credentials come from.** The SDK type establishes that the destination is Relativity, not that it is RelativityOne; if it is not RelativityOne — including a Relativity Server instance, whose own documentation governs — the migration is that service's own token mechanism, this finding is informational for this tool, and no migration to RelativityOne applies. If it is RelativityOne, the token comes from the token-acquisition part that follows.";

	/// <summary>CS-002 verification note.</summary>
	public const string CS_002_VERIFICATION_NOTE =
		"**Verification note — four things, and the first two can break working code.**\n\n**Verify equivalence before rewriting.** Not every RSAPI operation has a direct Object Manager counterpart. Establish that each operation this client performs has one before starting; where one does not, this is a redesign of that operation, not a migration of it.\n\n**A token has a finite lifetime**, set per client in minutes. A service factory built once from the token and kept for the life of a process keeps it after it expires; rebuild it with a current token within that lifetime, which the RSAPI credential never required.\n\n**Other RSAPI uses may not appear as findings.** This tool reports an RSAPI client only where it is constructed with a username-and-password credential; an RSAPI client obtained without one is not covered. Migrating this call does not establish that the codebase has no RSAPI left.\n\n**Confirm the SDK version in use provides `BearerTokenCredentials` and the Object Manager service.** Where it does not, this is an SDK upgrade before it is a code change.";

	/// <summary>CS-003 generic source form.</summary>
	public const string CS_003_SOURCE_FORM =
		"```csharp\ncache.Add(new Uri(instanceUrl), \"Basic\", new NetworkCredential(userName, password));\n```";

	/// <summary>CS-003 target form under token authentication.</summary>
	public const string CS_003_TARGET_FORM =
		"**First, the destination:** if it is not RelativityOne, this finding is informational — that service's own token mechanism is the migration — and the rest of this template does not apply.\n\nRemove **this cache entry only** — the `Add` for this host with the `Basic` scheme. A `CredentialCache` usually holds several entries for several hosts, and the others stay. The requests to this host then carry the token, set on each request, in the form the consumer of the cache accepts.\n\nWhere the cache serves `HttpClient`, on each `HttpRequestMessage`:\n\n```csharp\nrequest.Headers.Authorization = new AuthenticationHeaderValue(\"Bearer\", accessToken);\n```\n\nWhere it serves `HttpWebRequest` or `WebClient`, whose headers have no typed `Authorization` property:\n\n```csharp\nrequest.Headers[HttpRequestHeader.Authorization] = $\"Bearer {accessToken}\";   // HttpWebRequest\nclient.Headers[HttpRequestHeader.Authorization] = $\"Bearer {accessToken}\";    // WebClient, before each call\n```";

	/// <summary>CS-003 class share of the credential provenance.</summary>
	public const string CS_003_CREDENTIAL_PROVENANCE =
		"**Where the credentials come from.** The destination decides whether the migration applies. If the destination is not RelativityOne, the migration is that service's own token mechanism, this finding is informational for this tool, and no migration to RelativityOne applies. If it is RelativityOne, the token comes from the token-acquisition part that follows.";

	/// <summary>CS-003 verification note.</summary>
	public const string CS_003_VERIFICATION_NOTE =
		"**Verification note — five things.**\n\n**The 401-challenge response is lost.** A `CredentialCache` entry answers the server's challenge through the handler's credential path; a bearer header is sent up front and answers nothing. Code or a server that depended on that exchange behaves differently after the change.\n\n**Redirects stop carrying the credential.** Cache credentials are re-applied to a redirect target; a manually set `Authorization` header is not. .NET documents that `HttpWebRequest` clears the `Authorization` header when it follows a redirect automatically, and the `HttpClient` handler drops it on redirect. Code that reached this host through a redirect starts returning 401 after the migration; address the final URI, or handle the redirect explicitly.\n\n**Removing the wrong entry breaks another host.** The cache is keyed by URI and scheme. Remove only the entry for this host and `Basic`; an entry for another host or another scheme is not part of this finding.\n\n**A token has a finite lifetime, and a header set once on a long-lived client cannot be refreshed.** Set it per request, not on a shared client's default headers, or the code works for the token's lifetime and then returns 401 until the process restarts.\n\n**Confirm the endpoint in use is the RelativityOne REST API.** That is the API whose bearer token this template describes; an endpoint elsewhere reached from the same code may accept only Basic and reject the migrated call.";

	/// <summary>CS-004 generic source form.</summary>
	public const string CS_004_SOURCE_FORM =
		"```csharp\nvar authenticator = new HttpBasicAuthenticator(userName, password);\n```";

	/// <summary>CS-004 target form under token authentication.</summary>
	public const string CS_004_TARGET_FORM =
		"**First, the destination:** if it is not RelativityOne, this finding is informational — that service's own token mechanism is the migration — and the rest of this template does not apply.\n\nReplace the Basic authenticator with **the same library's bearer or token authenticator, carrying the access token** in place of the credential pair. This template names no target type: the construct occurs in more than one library, and the token authenticator's name differs between them and between versions.";

	/// <summary>CS-004 class share of the credential provenance.</summary>
	public const string CS_004_CREDENTIAL_PROVENANCE =
		"**Where the credentials come from.** The destination decides whether the migration applies. If the destination is not RelativityOne, the migration is that service's own token mechanism, this finding is informational for this tool, and no migration to RelativityOne applies. If it is RelativityOne, the token comes from the token-acquisition part that follows.";

	/// <summary>CS-004 verification note.</summary>
	public const string CS_004_VERIFICATION_NOTE =
		"**Verification note — three things.**\n\n**Confirm the library version in use offers a token authenticator.** Where it does not, this is a library upgrade before it is a code change, with whatever that upgrade carries.\n\n**A token has a finite lifetime.** An authenticator constructed once with a fixed token string and attached to a long-lived client holds that token after it expires. Whether the library can refresh it is the library's behaviour and is not established here; where it cannot, construct the authenticator with a current token for each request or each short-lived client.\n\n**Confirm the endpoint in use is the RelativityOne REST API.** The authenticator changes the scheme on the wire, and an endpoint elsewhere reached from the same code may accept only Basic and reject the migrated call.";

	/// <summary>CS-005 generic source form.</summary>
	public const string CS_005_SOURCE_FORM =
		"```csharp\nclient.DefaultRequestHeaders.Authorization =\n\tnew AuthenticationHeaderValue(BASIC_SCHEME, encodedCredentials);\n```";

	/// <summary>CS-005 target form under token authentication.</summary>
	public const string CS_005_TARGET_FORM =
		"**First, the destination:** if it is not RelativityOne, this finding is informational — that service's own token mechanism is the migration — and the rest of this template does not apply.\n\n`\"Bearer\"` and the access token in place of the scheme constant and the encoded pair, **set per request** on the request message rather than on the long-lived client's default headers:\n\n```csharp\nrequest.Headers.Authorization = new AuthenticationHeaderValue(\"Bearer\", accessToken);\n```\n\nA per-request header needs an `HttpRequestMessage` sent with `SendAsync`; the `GetAsync` and `PostAsync` convenience calls take no request message, so calls written with them change shape. The code that produced `encodedCredentials` has no counterpart: a bearer token travels verbatim.";

	/// <summary>CS-005 class share of the credential provenance.</summary>
	public const string CS_005_CREDENTIAL_PROVENANCE =
		"**Where the credentials come from.** The destination decides whether the migration applies. If the destination is not RelativityOne, the migration is that service's own token mechanism, this finding is informational for this tool, and no migration to RelativityOne applies. If it is RelativityOne, the token comes from the token-acquisition part that follows.";

	/// <summary>CS-005 verification note.</summary>
	public const string CS_005_VERIFICATION_NOTE =
		"**Verification note — three things.**\n\n**Moving the header from the client to the request is part of the migration, not a style choice.** A token has a finite lifetime, and a header set once on `DefaultRequestHeaders` cannot be refreshed without mutating shared state; the code works for the token's lifetime and then returns 401 until the process restarts.\n\n**The pair's encoding may be a separate finding.** Where `encodedCredentials` is built in the same file, the tool reports the encoding and this header as two findings at two locations. They are one migration, and both locations change together.\n\n**Confirm the endpoint in use is the RelativityOne REST API.** That is the API whose bearer token this template describes; an endpoint elsewhere reached from the same code may accept only Basic and reject the migrated call.";

	/// <summary>CS-006 generic source form.</summary>
	public const string CS_006_SOURCE_FORM =
		"```csharp\nclient.DefaultRequestHeaders.Add(\"Authorization\", $\"Basic {encodedCredentials}\");\n```";

	/// <summary>CS-006 target form under token authentication.</summary>
	public const string CS_006_TARGET_FORM =
		"**First, the destination:** if it is not RelativityOne, this finding is informational — that service's own token mechanism is the migration — and the rest of this template does not apply.\n\n`Bearer` and the access token in place of `Basic` and the encoded pair, **set per request** rather than on the long-lived client's default headers:\n\n```csharp\nrequest.Headers.Add(\"Authorization\", $\"Bearer {accessToken}\");\n```\n\nA per-request header needs an `HttpRequestMessage` sent with `SendAsync`; the `GetAsync` and `PostAsync` convenience calls take no request message, so calls written with them change shape. The code that produced `encodedCredentials` has no counterpart: a bearer token travels verbatim.";

	/// <summary>CS-006 class share of the credential provenance.</summary>
	public const string CS_006_CREDENTIAL_PROVENANCE =
		"**Where the credentials come from.** The destination decides whether the migration applies. If the destination is not RelativityOne, the migration is that service's own token mechanism, this finding is informational for this tool, and no migration to RelativityOne applies. If it is RelativityOne, the token comes from the token-acquisition part that follows.";

	/// <summary>CS-006 verification note.</summary>
	public const string CS_006_VERIFICATION_NOTE =
		"**Verification note — three things.**\n\n**Moving the header from the client to the request is part of the migration, not a style choice.** A token has a finite lifetime, and a header added once to `DefaultRequestHeaders` cannot be refreshed without mutating shared state; the code works for the token's lifetime and then returns 401 until the process restarts.\n\n**The pair's encoding may be a separate finding.** Where `encodedCredentials` is built in the same file, the tool reports the encoding and this header as two findings at two locations. They are one migration, and both locations change together.\n\n**Confirm the endpoint in use is the RelativityOne REST API.** That is the API whose bearer token this template describes; an endpoint elsewhere reached from the same code may accept only Basic and reject the migrated call.";

	/// <summary>CS-007 generic source form.</summary>
	public const string CS_007_SOURCE_FORM =
		"```csharp\nvar handler = new HttpClientHandler { Credentials = new NetworkCredential(userName, password) };\n```";

	/// <summary>CS-007 target form under token authentication.</summary>
	public const string CS_007_TARGET_FORM =
		"**First, the destination:** if it is not RelativityOne, this finding is informational — that service's own token mechanism is the migration — and the rest of this template does not apply.\n\n**Drop the `Credentials` assignment** from the handler, keeping its other settings, and **set the bearer header per request**:\n\n```csharp\nvar handler = new HttpClientHandler();\nrequest.Headers.Authorization = new AuthenticationHeaderValue(\"Bearer\", accessToken);\n```";

	/// <summary>CS-007 class share of the credential provenance.</summary>
	public const string CS_007_CREDENTIAL_PROVENANCE =
		"**Where the credentials come from.** The destination decides whether the migration applies. If the destination is not RelativityOne, the migration is that service's own token mechanism, this finding is informational for this tool, and no migration to RelativityOne applies. If it is RelativityOne, the token comes from the token-acquisition part that follows.";

	/// <summary>CS-007 verification note.</summary>
	public const string CS_007_VERIFICATION_NOTE =
		"**Verification note — five things.**\n\n**Confirm the exchange was Basic.** `NetworkCredential` declares no scheme, and a handler carrying one serves integrated Windows authentication as readily as Basic. If the server negotiated an integrated scheme, this construct is not Basic Authentication and the migration does not apply.\n\n**The 401-challenge response is lost.** A handler's credentials answer the server's challenge; a bearer header is sent up front and answers nothing. Code or a server that depended on that exchange behaves differently after the change.\n\n**Redirects stop carrying the credential.** Depending on the runtime and the credential type, handler credentials may be re-applied to a redirect target; a manually set `Authorization` header never is — .NET documents that the `HttpClient` handler drops it on redirect. Code that relied on a redirect can start returning 401 after the migration; address the final URI, or handle the redirect explicitly.\n\n**A token has a finite lifetime, and a header set once on a long-lived client cannot be refreshed.** Set it per request, not on the client's default headers, or the code works for the token's lifetime and then returns 401 until the process restarts.\n\n**Confirm the endpoint in use is the RelativityOne REST API.** That is the API whose bearer token this template describes; an endpoint elsewhere reached from the same code may accept only Basic and reject the migrated call.";

	/// <summary>CS-008 generic source form.</summary>
	public const string CS_008_SOURCE_FORM =
		"```csharp\nvar credential = new NetworkCredential(userName, password);\n```";

	/// <summary>CS-008 target form under token authentication.</summary>
	public const string CS_008_TARGET_FORM =
		"**First, the destination:** if it is not RelativityOne, this finding is informational — that service's own token mechanism is the migration — and the rest of this template does not apply.\n\n**Identify the consumer first** — the fix may be outside the reported file, because this finding reports a credential whose consumer the tool did not recognize in it. The form of the change depends on what consumes the credential:\n\n- an `HttpClient` handler: remove the credential from the handler and set the `Authorization` header to `Bearer` and the token on each request message;\n- `WebClient` or `HttpWebRequest`: remove the credential and set the `Authorization` header to `Bearer` and the token on each request;\n- a SOAP proxy: the binding changes, and the service may not accept bearer at all;\n- a mail, file-transfer, directory or proxy consumer: not a migration target for this tool.\n\nNo code form is given, because none is correct for all four.";

	/// <summary>CS-008 class share of the credential provenance.</summary>
	public const string CS_008_CREDENTIAL_PROVENANCE =
		"**Where the credentials come from.** The destination decides whether the migration applies. If the destination is not RelativityOne, the migration is that service's own token mechanism, this finding is informational for this tool, and no migration to RelativityOne applies. If it is RelativityOne, the token comes from the token-acquisition part that follows.";

	/// <summary>CS-008 verification note.</summary>
	public const string CS_008_VERIFICATION_NOTE =
		"**Verification note — seven things.**\n\n**This finding is the least characterized of any template.** The consumer was not recognized, so neither the scheme nor the destination has been established. Treat it as the location of a credential to trace, not as a construct whose migration is known.\n\n**Confirm the exchange was Basic.** `NetworkCredential` declares no scheme; the same type serves integrated Windows authentication, and the consumers above perform the exchange themselves with no `Basic` in the source. If the exchange is integrated, the migration does not apply.\n\n**The 401-challenge response is lost** wherever the consumer answered a challenge with this credential; a bearer header is sent up front and answers nothing.\n\n**Redirects stop carrying the credential.** Depending on the runtime and the credential type, a consumer's credentials may be re-applied to a redirect target; a manually set `Authorization` header never is. .NET documents that `HttpWebRequest` clears the `Authorization` header when it follows a redirect automatically, and the `HttpClient` handler drops it on redirect. Code that relied on a redirect can start returning 401 after the migration; address the final URI, or handle the redirect explicitly.\n\n**A SOAP service may not accept bearer at all.** Confirm it does before changing a binding.\n\n**Confirm the endpoint in use is the RelativityOne REST API.** That is the API whose bearer token this template describes; an endpoint elsewhere reached from the same code may accept only Basic and reject the migrated call.\n\n**A token has a finite lifetime.** A header must be set per request to be refreshable: a header set once on a long-lived consumer works for the token's lifetime and then returns 401 until the process restarts.";

	/// <summary>CS-009 generic source form.</summary>
	public const string CS_009_SOURCE_FORM =
		"```csharp\nstring pair = $\"{userName}:{password}\";\nstring encoded = Convert.ToBase64String(Encoding.ASCII.GetBytes(pair));\n```";

	/// <summary>CS-009 target form under token authentication.</summary>
	public const string CS_009_TARGET_FORM =
		"**First, the destination:** if it is not RelativityOne, this finding is informational — that service's own token mechanism is the migration — and the rest of this template does not apply.\n\n**Only if this encoding feeds an `Authorization` header.** Base64 over a colon-separated pair is not always a credential: the same shape encodes other data, and it is Basic Authentication only where the encoded value becomes the header. Where it does, **delete the encoding**: bearer carries the token verbatim, with no Base64 of a credential pair, and the header that received `encoded` receives the token, set per request:\n\n```csharp\nrequest.Headers.Authorization = new AuthenticationHeaderValue(\"Bearer\", accessToken);\n```";

	/// <summary>CS-009 class share of the credential provenance.</summary>
	public const string CS_009_CREDENTIAL_PROVENANCE =
		"**Where the credentials come from.** The destination decides whether the migration applies. If the destination is not RelativityOne, the migration is that service's own token mechanism, this finding is informational for this tool, and no migration to RelativityOne applies. If it is RelativityOne, the token comes from the token-acquisition part that follows.";

	/// <summary>CS-009 verification note.</summary>
	public const string CS_009_VERIFICATION_NOTE =
		"**Verification note — four things.**\n\n**Confirm the pair is a credential sent outward.** A service of the client's that *checks* Basic from its own callers is inbound authentication, which has nothing to migrate to Relativity OAuth2, and this tool does not cover it; it can still surface here, and such a finding is not a migration.\n\n**The header write may be a separate finding.** Where the header is written in the same file, the tool reports this encoding and that header as two findings at two locations. They are one migration, and both locations change together.\n\n**A token has a finite lifetime, and a header set once on a long-lived client cannot be refreshed.** Set it per request, or the code works for the token's lifetime and then returns 401 until the process restarts.\n\n**Confirm the endpoint in use is the RelativityOne REST API.** That is the API whose bearer token this template describes; an endpoint elsewhere reached from the same code may accept only Basic and reject the migrated call.";

	/// <summary>CS-012 generic source form.</summary>
	public const string CS_012_SOURCE_FORM =
		"```csharp\nclient.Headers[HttpRequestHeader.Authorization] = \"Basic \" + encoded;    // WebClient\nrequest.Headers[HttpRequestHeader.Authorization] = \"Basic \" + encoded;   // HttpWebRequest\n```";

	/// <summary>CS-012 target form under token authentication.</summary>
	public const string CS_012_TARGET_FORM =
		"**First, the destination:** if it is not RelativityOne, this finding is informational — that service's own token mechanism is the migration — and the rest of this template does not apply.\n\n`\"Bearer \"` and the access token in place of `\"Basic \"` and the encoded pair, **set per request**:\n\n```csharp\nclient.Headers[HttpRequestHeader.Authorization] = \"Bearer \" + accessToken;    // WebClient, before each call\nrequest.Headers[HttpRequestHeader.Authorization] = \"Bearer \" + accessToken;   // HttpWebRequest\n```\n\nThe code that produced `encoded` has no counterpart: a bearer token travels verbatim.";

	/// <summary>CS-012 class share of the credential provenance.</summary>
	public const string CS_012_CREDENTIAL_PROVENANCE =
		"**Where the credentials come from.** The destination decides whether the migration applies. If the destination is not RelativityOne, the migration is that service's own token mechanism, this finding is informational for this tool, and no migration to RelativityOne applies. If it is RelativityOne, the token comes from the token-acquisition part that follows.";

	/// <summary>CS-012 verification note.</summary>
	public const string CS_012_VERIFICATION_NOTE =
		"**Verification note — three things.**\n\n**A token has a finite lifetime, and a header set once cannot be refreshed.** An `HttpWebRequest` is one request, so its header is already per request. A `WebClient` instance reused across calls is the long-lived case: set the header before each call with a current token, or the code works for the token's lifetime and then returns 401.\n\n**The pair's encoding may be a separate finding.** Where `encoded` is built in the same file, the tool reports the encoding and this header as two findings at two locations. They are one migration, and both locations change together.\n\n**Confirm the endpoint in use is the RelativityOne REST API.** That is the API whose bearer token this template describes; an endpoint elsewhere reached from the same code may accept only Basic and reject the migrated call.";

	/// <summary>CS-013 generic source form.</summary>
	public const string CS_013_SOURCE_FORM =
		"```csharp\nbinding.Security.Transport.ClientCredentialType = HttpClientCredentialType.Basic;\nclient.ClientCredentials.UserName.UserName = userName;\nclient.ClientCredentials.UserName.Password = password;\n```";

	/// <summary>CS-013 target form under token authentication.</summary>
	public const string CS_013_TARGET_FORM =
		"**First, the destination:** if it is not RelativityOne, this finding is informational — that service's own token mechanism is the migration — and the rest of this template does not apply.\n\nThe binding's **transport credential type becomes the token form that binding supports**, and the username and password assignments are removed — from code, and from configuration where the credential lives there instead. No code form is given: which token form a binding and a service accept is decided by the service, not by the client.\n\n**If the SOAP service is Relativity's, confirm it accepts bearer at all before changing the binding.** Where it does not, there is no scheme to migrate to on that service, and the migration is the move to a service that does.";

	/// <summary>CS-013 class share of the credential provenance.</summary>
	public const string CS_013_CREDENTIAL_PROVENANCE =
		"**Where the credentials come from.** The destination decides whether the migration applies. If the destination is not RelativityOne, the migration is that service's own token mechanism, this finding is informational for this tool, and no migration to RelativityOne applies. If it is RelativityOne, the token comes from the token-acquisition part that follows.";

	/// <summary>CS-013 verification note.</summary>
	public const string CS_013_VERIFICATION_NOTE =
		"**Verification note — three things.**\n\n**A SOAP or WCF service may not accept bearer at all.** This is the first check, not the last: changing the binding against a service that accepts only Basic breaks a working integration.\n\n**The credential may not be in this file.** The binding is what this finding reports; its username and password may live in configuration files, which this tool does not scan. A migration that changes the binding and leaves a configured password behind leaves the secret in place.\n\n**A token has a finite lifetime.** A client proxy configured once and kept for the process's life holds a token that expires; within that lifetime the token on the proxy must be replaced, or the proxy recreated.";

	/// <summary>CS-014 generic source form.</summary>
	public const string CS_014_SOURCE_FORM =
		"```csharp\nvar authenticatedRequest = request.WithBasicAuth(userName, password);\n```";

	/// <summary>CS-014 target form under token authentication.</summary>
	public const string CS_014_TARGET_FORM =
		"**First, the destination:** if it is not RelativityOne, this finding is informational — that service's own token mechanism is the migration — and the rest of this template does not apply.\n\nReplace the Basic method with **the same library's bearer or token method, carrying the access token** in place of the credential pair. This template names no target method: the construct is a method whose name names the scheme, and the token method's name differs between libraries and versions.";

	/// <summary>CS-014 class share of the credential provenance.</summary>
	public const string CS_014_CREDENTIAL_PROVENANCE =
		"**Where the credentials come from.** The destination decides whether the migration applies. If the destination is not RelativityOne, the migration is that service's own token mechanism, this finding is informational for this tool, and no migration to RelativityOne applies. If it is RelativityOne, the token comes from the token-acquisition part that follows.";

	/// <summary>CS-014 verification note.</summary>
	public const string CS_014_VERIFICATION_NOTE =
		"**Verification note — three things.**\n\n**Confirm the library version in use offers a token method.** Where it does not, this is a library upgrade before it is a code change.\n\n**A token has a finite lifetime.** Applied to one request, the method is per request already. Applied once to a client or configuration shared across requests, it holds a token that expires, and the code works for the token's lifetime and then returns 401; pass a current token where the request is built.\n\n**Confirm the endpoint in use is the RelativityOne REST API.** That is the API whose bearer token this template describes; an endpoint elsewhere reached from the same code may accept only Basic and reject the migrated call.";

	/// <summary>CS-015 generic source form.</summary>
	public const string CS_015_SOURCE_FORM =
		"```csharp\nvar importApi = new ImportAPI(userName, password, webServiceUrl);\n```";

	/// <summary>CS-015 target form under token authentication.</summary>
	public const string CS_015_TARGET_FORM =
		"**First, the destination:** if this Relativity instance is not RelativityOne — including a Relativity Server instance, whose own documentation governs — this finding is informational and the rest of this template does not apply.\n\n**Change what the entry point receives, not the call site.** Relativity's Import API documentation names one static factory on the type, `ImportAPI.CreateByRsaBearerToken(webServiceUrl)`. **It takes the web service URL and no token**: it uses the bearer token of the identity of the process that runs it, and the documentation says it should be used only by processes hosted by the Relativity Service Account, such as an agent. Where the code runs in such a host, the construction above is replaced by:\n\n```csharp\nvar importApi = ImportAPI.CreateByRsaBearerToken(webServiceUrl);\n```\n\n**For a standalone or desktop process that would obtain an OAuth2 token itself, this template gives no code form.** The documentation names no `ImportAPI` factory that accepts a token acquired by the caller, and it lists bearer-token authentication as not supported for a desktop application, where user name and password is the preferred form; that is a known issue of this release, listed in its release notes, and not something this text resolves. Whether the code that uses `importApi` afterwards needs any change is not established here; confirm it against the SDK's documentation for the version in use. The migrated construction must not keep the three-argument form with a credential pair, which is the one this finding reports.";

	/// <summary>CS-015 class share of the credential provenance.</summary>
	public const string CS_015_CREDENTIAL_PROVENANCE =
		"**Where the credentials come from.** The SDK type establishes that the destination is Relativity, not that it is RelativityOne; if it is not RelativityOne — including a Relativity Server instance, whose own documentation governs — the migration is that service's own token mechanism, this finding is informational for this tool, and no migration to RelativityOne applies. If it is RelativityOne, the factory above carries no token of the code's own: the identity comes from the hosting process, so the token-acquisition part that follows applies only to code that obtains a token itself, and for the entry point that case is the known issue stated above.";

	/// <summary>CS-015 verification note.</summary>
	public const string CS_015_VERIFICATION_NOTE =
		"**Verification note — two things, and the first can break working code.**\n\n**The factory is documented for hosted processes only, and for Relativity Server.** It is meant for an agent or another process hosted by the Relativity Service Account; a process that runs elsewhere has no such identity to hand it. The two documentation pages that describe it are Relativity Server pages, and Relativity's RelativityOne documentation does not describe this class, so confirm that the SDK version in use for RelativityOne exposes it before relying on it.\n\n**Confirm the SDK version in use exposes `CreateByRsaBearerToken`.** Where it does not, this is an SDK upgrade before it is a code change. The documentation states that the factory handles authentication and token expiration itself, so the finite-lifetime concern of the other templates is the factory's to manage here, not the code's.";

	/// <summary>CS-016 generic source form.</summary>
	public const string CS_016_SOURCE_FORM =
		"```csharp\nNetworkCredential credential = new NetworkCredential { UserName = userName };\ncredential.Password = password;\n```";

	/// <summary>CS-016 target form under token authentication.</summary>
	public const string CS_016_TARGET_FORM =
		"**First, the destination:** if it is not RelativityOne, this finding is informational — that service's own token mechanism is the migration — and the rest of this template does not apply.\n\n**Identify the destination first**, because a credential assembled across statements is usually consumed somewhere other than where it is built. Then **remove the credential from its consumer and set the bearer header on each request** that consumer sends:\n\n```csharp\nrequest.Headers.Authorization = new AuthenticationHeaderValue(\"Bearer\", accessToken);\n```";

	/// <summary>CS-016 class share of the credential provenance.</summary>
	public const string CS_016_CREDENTIAL_PROVENANCE =
		"**Where the credentials come from.** The destination decides whether the migration applies. If the destination is not RelativityOne, the migration is that service's own token mechanism, this finding is informational for this tool, and no migration to RelativityOne applies. If it is RelativityOne, the token comes from the token-acquisition part that follows.";

	/// <summary>CS-016 verification note.</summary>
	public const string CS_016_VERIFICATION_NOTE =
		"**Verification note — six things.**\n\n**Confirm the consumer is an HTTP destination.** The same form bound to a mail, file-transfer or directory client is not a migration target for this tool, and the construct alone does not show which consumer it has.\n\n**Confirm the exchange was Basic.** `NetworkCredential` declares no scheme and serves integrated Windows authentication as readily as Basic; if the exchange is integrated, the migration does not apply.\n\n**The 401-challenge response is lost** wherever the consumer answered a challenge with this credential; a bearer header is sent up front and answers nothing.\n\n**Redirects stop carrying the credential.** Depending on the runtime and the credential type, a consumer's credentials may be re-applied to a redirect target; a manually set `Authorization` header never is. .NET documents that `HttpWebRequest` clears the `Authorization` header when it follows a redirect automatically, and the `HttpClient` handler drops it on redirect. Code that relied on a redirect can start returning 401 after the migration; address the final URI, or handle the redirect explicitly.\n\n**A token has a finite lifetime, and a header set once on a long-lived client cannot be refreshed.** Set it per request, or the code works for the token's lifetime and then returns 401 until the process restarts.\n\n**Confirm the endpoint in use is the RelativityOne REST API.** That is the API whose bearer token this template describes; an endpoint elsewhere reached from the same code may accept only Basic and reject the migrated call.";

	/// <summary>CS-017 generic source form.</summary>
	public const string CS_017_SOURCE_FORM =
		"```csharp\nimportJob.Settings.RelativityUsername = userName;\nimportJob.Settings.RelativityPassword = password;\n```";

	/// <summary>CS-017 target form under token authentication.</summary>
	public const string CS_017_TARGET_FORM =
		"**First, the destination:** if this Relativity instance is not RelativityOne — including a Relativity Server instance, whose own documentation governs — this finding is informational and the rest of this template does not apply.\n\nReplace the username and password properties with **the settings object's token-bearing equivalent**, assigned the access token, and remove both credential assignments. This template does not name that member, because which one exists depends on the SDK version. **Confirm the SDK version in use exposes one; where it does not, this is an SDK upgrade before it is a code change.**";

	/// <summary>CS-017 class share of the credential provenance.</summary>
	public const string CS_017_CREDENTIAL_PROVENANCE =
		"**Where the credentials come from.** The property names establish that the destination is Relativity, not that it is RelativityOne; if it is not RelativityOne — including a Relativity Server instance, whose own documentation governs — the migration is that service's own token mechanism, this finding is informational for this tool, and no migration to RelativityOne applies. If it is RelativityOne, the token comes from the token-acquisition part that follows.";

	/// <summary>CS-017 verification note.</summary>
	public const string CS_017_VERIFICATION_NOTE =
		"**Verification note — two things. The first decides whether the change can be made at all.**\n\n**SDK version support comes first.** Where the version in use has no token-bearing settings member, the change cannot be made at this call site at all, and the upgrade carries its own verification.\n\n**A token has a finite lifetime.** A settings object populated once and reused across long or repeated jobs holds a token that expires; populate it with a current token for each job.";

	/// <summary>CS-018 generic source form.</summary>
	public const string CS_018_SOURCE_FORM =
		"```csharp\nstring pair = \"\" + \":\" + accessToken;\nstring encoded = Convert.ToBase64String(Encoding.ASCII.GetBytes(pair));\n```";

	/// <summary>CS-018 target form under token authentication.</summary>
	public const string CS_018_TARGET_FORM =
		"**This change applies to any destination that accepts `Bearer`**, RelativityOne or not: no credential is exchanged, so only the token's provenance differs when the destination is not RelativityOne.\n\n**Delete the encoding and change the prefix.** The token already in hand is carried verbatim:\n\n```\nAuthorization: Basic <base64 of \":\" + token>      ->      Authorization: Bearer <token>\n```\n\n```csharp\nrequest.Headers.Authorization = new AuthenticationHeaderValue(\"Bearer\", accessToken);\n```\n\nNo credential is exchanged and no client is registered — this is the cheapest remediation of any template.";

	/// <summary>CS-018 class share of the credential provenance.</summary>
	public const string CS_018_CREDENTIAL_PROVENANCE =
		"**Where the credentials come from.** The prefix change applies to any destination that accepts `Bearer`. If the destination is RelativityOne, the token that belongs in the header is obtained as the token-acquisition part that follows describes. If it is not RelativityOne, the token's provenance is that service's and does not change: the token already in hand is the one that moves to the `Bearer` prefix, issued however that service issues it.";

	/// <summary>CS-018 verification note.</summary>
	public const string CS_018_VERIFICATION_NOTE =
		"**Verification note — two things.**\n\n**Verify the destination accepts `Bearer` before changing the prefix.** Some services accept only Basic, and there the prefix change breaks working code.\n\n**A header set once on a long-lived client cannot be refreshed.** If the token has a lifetime, set the header per request with a current token rather than on a shared client's default headers.";

	/// <summary>CS-020 generic source form.</summary>
	public const string CS_020_SOURCE_FORM =
		"```csharp\nvar proxy = helper.GetProxy<IExampleService>(userName, password);\n```";

	/// <summary>CS-020 target form under token authentication.</summary>
	public const string CS_020_TARGET_FORM =
		"**First, the destination:** if this Relativity instance is not RelativityOne — including a Relativity Server instance, whose own documentation governs — this finding is informational and the rest of this template does not apply.\n\nReplace the call that passes a user name and a password with **the helper's token-bearing equivalent**, given the access token, and remove the two credential arguments. This template does not name that overload or member, because which one exists depends on the version of the helper or SDK in use. **Confirm the version in use offers one; where it does not, this is an upgrade before it is a code change.**";

	/// <summary>CS-020 class share of the credential provenance.</summary>
	public const string CS_020_CREDENTIAL_PROVENANCE =
		"**Where the credentials come from.** The call shape establishes that a user name and a password are given to a factory, not that the factory connects to RelativityOne; if it is not RelativityOne — including a Relativity Server instance, whose own documentation governs — the migration is that service's own token mechanism, this finding is informational for this tool, and no migration to RelativityOne applies. If it is RelativityOne, the token comes from the token-acquisition part that follows.";

	/// <summary>CS-020 verification note.</summary>
	public const string CS_020_VERIFICATION_NOTE =
		"**Verification note — two things. The first decides whether the change can be made at all.**\n\n**Helper or SDK version support comes first.** Where the version in use has no token-bearing equivalent of the call, the change cannot be made at this call site at all, and the upgrade carries its own verification.\n\n**A token has a finite lifetime, and the method is read by its name.** A proxy obtained once and reused across a long-running process or test run holds a token that expires; obtain it with a current token for each run. Also confirm the call is the Relativity helper: a method of another library with the same name and two arguments is read the same way.";
}
