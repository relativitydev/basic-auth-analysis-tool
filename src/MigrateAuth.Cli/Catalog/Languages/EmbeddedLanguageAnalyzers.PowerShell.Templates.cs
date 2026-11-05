using MigrateAuth.Cli.Reporting;

namespace MigrateAuth.Cli.Catalog.Languages;

/// <summary>
/// The catalog's recommendation text for `PowerShell`: eleven classes, both flows, and both generic fallback texts. The three Low classes
/// (the credential on a web cmdlet without a scheme, the non-HTTP sink and the helper definition) carry no entry: the catalog routes all three to
/// manual review.
///
/// HOW A RECOMMENDATION IS COMPOSED. Part 1 is the class's four fields and is flow-neutral, so the source form, the target form and the verification
/// note are the same text for both flows. The credential provenance is the class's own share followed by part 2 for the finding's flow, held once here
/// as <see cref="CLIENT_CREDENTIALS_TOKEN_ACQUISITION"/> and <see cref="AUTHORIZATION_CODE_TOKEN_ACQUISITION"/>. Part 3, the OAuth2 Client block, is
/// appended by the shared engine's composer and is not repeated here.
///
/// ENCODING CHOICES. Every secret-handling sentence names <see cref="SecretManagementPointer.PART_B_NAME"/> and the label of a fact NSerio verified
/// but Relativity does not document is <see cref="OAuth2ClientConfigurationBlocks.NSERIO_VERIFIED_LABEL"/>, both through the constants the shared
/// engine already owns. Each class names the editions it applies to in its own text, and where the two editions differ the text carries labelled
/// sub-blocks and not a second template.
///
/// A SEPARATE FILE BECAUSE <c>PowerShellAnalyzer</c> IS <c>file</c>-SCOPED: its type cannot be split across files, so the text lives in this
/// <c>internal</c> holder and the analyzer's template provider reads it by the same identifiers.
/// </summary>
internal static class PowerShellRecommendationTemplates
{
	/// <summary>The Client Credentials generic fallback prose for a PowerShell finding, with the secret-handling sentence naming the Part B pointer.</summary>
	public const string POWERSHELL_LANGUAGE_FALLBACK_TEXT =
		"The tool reports Basic Authentication at this location but did not recognize the construct, so the change has not been characterized and no before-and-after form is offered here. If the destination is not RelativityOne, Relativity's identity endpoint does not issue tokens for it: the migration is that service's own, and this finding is informational. If the destination is RelativityOne, Basic Authentication is no longer documented for its REST API. The documented destination in PowerShell is a bearer `Authorization` header on the request, or, where the script loads a Relativity .NET SDK assembly, the SDK's bearer-token credential type; either carries a token obtained from the platform's identity endpoint with the client-credentials grant. The client ID and secret that obtain that token belong in the process environment or a secret store, per " + SecretManagementPointer.PART_B_NAME + ", and never in the script or in tracked configuration. Beyond those facts this is a pointer and not an instruction: the migration must be designed from the code by whoever owns it, rather than applied from this text.";

	/// <summary>The Authorization Code generic fallback prose for a PowerShell finding.</summary>
	public const string POWERSHELL_CODE_LANGUAGE_FALLBACK_TEXT =
		"The tool reports Basic Authentication at this location but did not recognize the construct, so the change has not been characterized and no before-and-after form is offered here. If the destination is not RelativityOne, Relativity's identity endpoint does not issue tokens for it: the migration is that service's own, and this finding is informational. If the destination is RelativityOne, Basic Authentication is no longer documented for its REST API, and this finding is assigned the Authorization Code flow, in which the script sends the user to RelativityOne to sign in, receives an authorization code at a Redirect URL registered on its OAuth2 Client, and exchanges the code for an access token that it sends as a Bearer token. The authorize endpoint is `<host>/Relativity/Identity/connect/authorize` and the code is exchanged at `<host>/Relativity/Identity/connect/token`; both are verified by NSerio against a live instance; not documented by Relativity. PKCE is required by Relativity's own documentation to obtain a refresh token, and not otherwise. The client ID and secret belong in the process environment or a secret store, per " + SecretManagementPointer.PART_B_NAME + ", and never in the script or in tracked configuration; Code requires a client secret, and a secret in a distributed script can be read by anyone who holds it. After the change the script no longer collects or forwards the user's password. Beyond those facts this is a pointer and not an instruction: the migration must be designed from the code by whoever owns it, rather than applied from this text.";

	/// <summary>Part 2 for Client Credentials: how the token is obtained and kept. Appended to every class's credential-provenance share for a Client Credentials finding.</summary>
	public const string CLIENT_CREDENTIALS_TOKEN_ACQUISITION =
		"**Where the token comes from.** Create an OAuth2 Client with Flow Grant Type set to Client Credentials (part 3 lists its settings), then send a form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=client_credentials`, `client_id`, `client_secret` and a `scope`. The token endpoint is **documented** for this grant. The token is then sent as `Authorization: Bearer <token>`, with `X-CSRF-Header: -` on REST and Kepler calls — the header is documented as required, and a Kepler call without it returns 404.\n\n```powershell\n# Windows PowerShell 5.1 and PowerShell 7.x. $clientId and $clientSecret come from the process\n# environment or a secret store (" + SecretManagementPointer.PART_B_NAME + "), never from this script.\n$tokenBody = @{\n\tgrant_type    = 'client_credentials'\n\tclient_id     = $clientId\n\tclient_secret = $clientSecret\n\tscope         = $scope\n}\n$tokenResponse = Invoke-RestMethod -Method Post -Uri \"$instanceUrl/Relativity/Identity/connect/token\" -Body $tokenBody -ContentType 'application/x-www-form-urlencoded'\n$accessToken = $tokenResponse.access_token\n$headerTable = @{ Authorization = \"Bearer $accessToken\"; 'X-CSRF-Header' = '-' }\n```\n\n```powershell\n# Windows PowerShell 5.1 only, before the first request, if the host does not already negotiate TLS 1.2\n# (inferred from the .NET Framework default; the local mock-server run is plain http, so it does not exercise TLS).\n[Net.ServicePointManager]::SecurityProtocol = [Net.ServicePointManager]::SecurityProtocol -bor [Net.SecurityProtocolType]::Tls12\n```\n\n```powershell\n# PowerShell 7.x alternative to the header: the cmdlet sends the bearer token itself\n# (checked by NSerio locally, 7.6.6: Authorization: Bearer is sent pre-emptively).\n$secureToken = ConvertTo-SecureString $accessToken -AsPlainText -Force\nInvoke-RestMethod -Uri $uri -Authentication Bearer -Token $secureToken -Headers @{ 'X-CSRF-Header' = '-' }\n```\n\n**The scope depends on what the integration needs, and no single scope is required.** Relativity's documentation shows two values for this grant: `SystemUserInfo` on the REST authentication page, the Usage Reports page and in the official Transfer SDK sample, and `UserInfoAccess` in the Kepler .NET samples. No page says which is correct, whether both work, or what either grants, and the OAuth2 Client's `Scopes` property is documented as one that cannot be set. Use the value the API you call documents, and confirm it.\n\n**Cache the token, and re-acquire it on expiry — do not refresh.** Keep it in a script-scoped variable for its lifetime (part 3 recommends 60 minutes, Relativity's own wording), not on disk, and request a new one with the client secret when it expires or a call is rejected for authentication. Client Credentials offers no refresh tokens: **" + OAuth2ClientConfigurationBlocks.NSERIO_VERIFIED_LABEL + "** — the Allow Refresh Tokens control does not appear for this grant in the RelativityOne user interface.\n\n**The client secret.** It is displayed once, at creation or reset. Store it as " + SecretManagementPointer.PART_B_NAME + " describes; never in the script, never in tracked configuration. A credential saved with `Export-Clixml` is bound to the user and machine that saved it on Windows and is not protected on Linux or macOS (documented by Microsoft). A reset invalidates the previous secret immediately, so plan the cut-over before resetting it.\n\n**Before deploying, confirm the calling identity.** Every call now runs as the OAuth2 Client's **Context User**, not as the user in the replaced credential. If that user has different workspace permissions or item-level security, the script returns fewer rows or `Unauthorized` with no change to its logic, and Relativity's audit attributes every call to that account. Confirm the Context User's permissions match those of the previous identity.\n\n**When the integration acts for a signed-in user.** Client Credentials is one service identity by construction. That is the Authorization Code case, and moving to it means a new OAuth2 Client, because the flow is fixed when a client is created. The finding's flow note says when to make that choice.";

	/// <summary>Part 2 for Authorization Code: the common part and both variants. Appended to every class's credential-provenance share for an Authorization Code finding.</summary>
	public const string AUTHORIZATION_CODE_TOKEN_ACQUISITION =
		"**Common to both variants**\n\n**Sign in.** Replace the code that collected the user's credentials with a redirect: send the user's browser to the authorize endpoint `<host>/Relativity/Identity/connect/authorize`, which leads to the RelativityOne login page. The authorize endpoint is **" + OAuth2ClientConfigurationBlocks.NSERIO_VERIFIED_LABEL + "**. The request carries the standard authorization-code parameters — `client_id`, `response_type=code`, the registered `redirect_uri`, a `scope`, and a `state` value the script checks on return; these are standard OAuth, and Relativity documents none of them.\n\n**Receive the code.** The browser returns to the Redirect URL registered on the OAuth2 Client, with the authorization code and the `state` value. Check the `state` before using the code.\n\n**Exchange the code.** Send a form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=authorization_code`, the `code`, the same `redirect_uri`, the `client_id` and the `client_secret`, plus the `code_verifier` when PKCE is used. The exchange is **" + OAuth2ClientConfigurationBlocks.NSERIO_VERIFIED_LABEL + "**.\n\n**Use the token.** Send `Authorization: Bearer <access token>` — the change at the use site in part 1 — with `X-CSRF-Header: -` on REST and Kepler calls.\n\n**On expiry.** With Allow Refresh Tokens enabled on the OAuth2 Client and PKCE used, the script holds a refresh token and, when the access token expires, sends a `grant_type=refresh_token` request with it in place of the code exchange — *inferred* from the standard OAuth shape, not tested. Without a refresh token the user signs in again. Relativity does not document a refresh token's lifetime, whether it rotates, or how it is revoked. A refresh token is a secret: store it as " + SecretManagementPointer.PART_B_NAME + " describes, not in a plain file.\n\n**PKCE.** PKCE is required by Relativity's own documentation to obtain a refresh token, and not otherwise: a Code request that does not ask for a refresh token works without it, **" + OAuth2ClientConfigurationBlocks.NSERIO_VERIFIED_LABEL + "**. NSerio recommends PKCE with the `S256` method (NSerio recommendation), and does not claim that Relativity accepts any particular method, which it does not document.\n\n**Scope.** No scope set is prescribed for Code. The set NSerio used works, **" + OAuth2ClientConfigurationBlocks.NSERIO_VERIFIED_LABEL + "**, verified as working and not as minimal.\n\n**The script stops collecting the password.** After this change the script never receives, stores or forwards the user's user name and password: remove the prompt (`Get-Credential`, `Read-Host`) and any storage of the password.\n\n**Single sign-on.** Sign-in happens on RelativityOne's own login page, so the instance's single sign-on and two-factor rules apply unchanged — *inferred* from where the sign-in happens, and not documented for clients that customers create.\n\n**The client secret is required.** Code does not work without a client secret: tested and rejected, **" + OAuth2ClientConfigurationBlocks.NSERIO_VERIFIED_LABEL + "**, which confirms Relativity's own statement that the grant requires one. It is displayed once and is stored as " + SecretManagementPointer.PART_B_NAME + " describes.\n\n**Server-side variant.** For a script that runs on a server and receives the callback on a route of an application: one exact HTTPS Redirect URL per environment, no wildcards, registered by the Relativity administrator (NSerio recommendation; Relativity's matching rules are not documented); the secret and the user's tokens stay on the server. PowerShell is rarely the host of such a route; the server-side variant of the other languages applies unchanged.\n\n**Desktop variant.** For a script a person runs on their own machine:\n\n- **The system browser.** Open the authorize URL in the default browser, not in a web view the script hosts, so the script never sees the password (NSerio recommendation).\n- **A loopback redirect on a fixed port.** Listen on the loopback address only, on the registered port, for one request. The Redirect URL is `http://127.0.0.1:<port>/<path>`: a plain-HTTP loopback URL is accepted, **" + OAuth2ClientConfigurationBlocks.NSERIO_VERIFIED_LABEL + "**. The Relativity administrator registers that exact URL, so the port is fixed and not chosen at runtime. **`HttpListener` accepts only a prefix that ends in `/`**, so the script appends one to the prefix it listens on and leaves the registered Redirect URL as registered: a request for `<path>?code=…` is received by the prefix `<path>/` (*checked by NSerio locally*, Windows PowerShell 5.1 and PowerShell 7.6.6: `Prefixes.Add` rejects a prefix without the slash, and the prefix with it answered both `/<path>?code=…` and `/<path>/?code=…`). `HttpListener` starts on the loopback address from a non-elevated process on both editions (*checked by NSerio locally*; a true standard account is not yet tested).\n- **The secret is extractable.** Code requires a client secret, so a distributed script has to embed one, and **a script is text: anyone who can read the file has the secret.** It protects nothing against the person who runs the script.\n- **Compensating guidance** (NSerio recommendation, subject to your own security policy): set Trusted IPs where outbound addresses are known and stable — they restrict where a token can be *requested* — use the recommended short Access Token Lifetime of 60 minutes, enable refresh tokens only when needed, and create one OAuth2 Client per distribution.\n- **A known issue.** Relativity documents no recommendation for a distributed desktop application; this design is what NSerio verified works.\n\n```powershell\n# Windows PowerShell 5.1 and PowerShell 7.x. Executed against a local mock server on both, not against a live instance.\n# $redirectUri is the exact URL registered on the OAuth2 Client, with a fixed port.\n$state = [guid]::NewGuid().ToString('N')\n$generator = [System.Security.Cryptography.RandomNumberGenerator]::Create()\n$verifierBytes = New-Object byte[] 32\n$generator.GetBytes($verifierBytes)\n$verifier = [Convert]::ToBase64String($verifierBytes).TrimEnd('=').Replace('+', '-').Replace('/', '_')\n$hasher = [System.Security.Cryptography.SHA256]::Create()\n$challengeBytes = $hasher.ComputeHash([Text.Encoding]::ASCII.GetBytes($verifier))\n$challenge = [Convert]::ToBase64String($challengeBytes).TrimEnd('=').Replace('+', '-').Replace('/', '_')\n\n$listener = New-Object System.Net.HttpListener\n$listener.Prefixes.Add($redirectUri.TrimEnd('/') + '/')   # a prefix must end in '/'; the registered URL is unchanged\n$listener.Start()\n$authorizeUrl = \"$instanceUrl/Relativity/Identity/connect/authorize?client_id=$clientId&response_type=code\" +\n\t\"&redirect_uri=$([uri]::EscapeDataString($redirectUri))&scope=$([uri]::EscapeDataString($scope))\" +\n\t\"&state=$state&code_challenge=$challenge&code_challenge_method=S256\"\nStart-Process $authorizeUrl   # on Linux or macOS under 7.x use the platform opener instead (inferred)\n$context = $listener.GetContext()\n$query = $context.Request.QueryString\n$page = [Text.Encoding]::UTF8.GetBytes('You can close this window.')\n$context.Response.Close($page, $true)   # sends the page and waits until it is written, before the listener stops\n$listener.Stop()\nif ($query['state'] -ne $state) { throw 'The state value does not match.' }\n$tokenResponse = Invoke-RestMethod -Method Post -Uri \"$instanceUrl/Relativity/Identity/connect/token\" -Body @{\n\tgrant_type    = 'authorization_code'\n\tcode          = $query['code']\n\tredirect_uri  = $redirectUri\n\tclient_id     = $clientId\n\tclient_secret = $clientSecret\n\tcode_verifier = $verifier\n}\n$accessToken = $tokenResponse.access_token\n```\n\n**Why this form.** The two short .NET helpers for the hash and for random bytes exist on 7.x only, so the script creates the objects and calls their instance methods, which run on both editions (documented .NET API).";

	/// <summary>PS-001 generic source form.</summary>
	public const string PS_001_SOURCE_FORM =
		"**Editions:** Windows PowerShell 5.1 and PowerShell 7.x; whether the SDK assembly loads under 7.x depends on how it is built and is not established here.\n\n```powershell\n$credentials = New-Object <SdkNamespace>.UsernamePasswordCredentials($loginName, $loginSecret)\n$settings = New-Object <SdkNamespace>.ServiceFactorySettings($restEndpointUri, $credentials)\n```";

	/// <summary>PS-001 target form under token authentication.</summary>
	public const string PS_001_TARGET_FORM =
		"**First, the destination:** if this Relativity instance is not RelativityOne — including a Relativity Server instance, whose own documentation governs — this finding is informational and the rest of this template does not apply.\n\n```powershell\n$bearer = New-Object <SdkNamespace>.BearerTokenCredentials($accessToken)\n$settings = New-Object <SdkNamespace>.ServiceFactorySettings($restEndpointUri, $bearer)\n```";

	/// <summary>PS-001 class share of the credential provenance.</summary>
	public const string PS_001_CREDENTIAL_PROVENANCE =
		"**Where the credentials come from.** The SDK type establishes that the destination is Relativity, not that it is RelativityOne. If it is not RelativityOne the migration is that service's own token mechanism, this finding is informational for this tool, and the reader stops here. If it is RelativityOne, the token comes from the token-acquisition part that follows.";

	/// <summary>PS-001 verification note.</summary>
	public const string PS_001_VERIFICATION_NOTE =
		"**Verification note — two things, and the first can break working code.** **A token has a finite lifetime**, set per client in minutes, and this construct holds it: a factory built once and kept for the life of a script keeps the token after it expires, and the calls then fail authentication. Rebuild the settings and the factory with a current token. **Confirm the SDK version in use provides `BearerTokenCredentials`**; where it does not, this is an SDK upgrade before it is a code change.";

	/// <summary>PS-002 generic source form.</summary>
	public const string PS_002_SOURCE_FORM =
		"**Editions:** Windows PowerShell 5.1 in practice (the legacy client is a .NET Framework library); 7.x if the assembly loads.\n\n```powershell\n$proxy = New-Object <SdkNamespace>.RSAPIClient ((New-Object System.Uri -ArgumentList $serviceAddress), (New-Object <SdkNamespace>.UsernamePasswordCredentials($loginName, $loginSecret)))\n```";

	/// <summary>PS-002 target form under token authentication.</summary>
	public const string PS_002_TARGET_FORM =
		"**First, the destination:** if this Relativity instance is not RelativityOne — including a Relativity Server instance, whose own documentation governs — this finding is informational and the rest of this template does not apply.\n\n**Two steps, not one, and in this order.** First, **move the call to Object Manager**: the legacy client is retired, so swapping the credential inside its construction migrates the scheme of a call that should not survive. Rewrite the operation as the equivalent Object Manager call. Second, apply the bearer token to that rewritten call:\n\n```powershell\n$bearer = New-Object <SdkNamespace>.BearerTokenCredentials($accessToken)\n$settings = New-Object <SdkNamespace>.ServiceFactorySettings($restEndpointUri, $bearer)\n```";

	/// <summary>PS-002 class share of the credential provenance.</summary>
	public const string PS_002_CREDENTIAL_PROVENANCE =
		"**Where the credentials come from.** As for the SDK credential type: the type establishes Relativity, not RelativityOne; if it is not RelativityOne the finding is informational for this tool and the reader stops here. If it is RelativityOne, the token comes from the token-acquisition part that follows.";

	/// <summary>PS-002 verification note.</summary>
	public const string PS_002_VERIFICATION_NOTE =
		"**Verification note — four things, and the first two can break working code.** **Verify equivalence before rewriting:** not every legacy operation has an Object Manager counterpart; where one does not, this is a redesign. **A token has a finite lifetime**: rebuild the factory with a current token. **Other uses of the legacy client may not appear as findings:** this tool reports it only where it is constructed with a user-name-and-password credential. **Confirm the SDK version provides `BearerTokenCredentials` and the Object Manager service.**";

	/// <summary>PS-003 generic source form.</summary>
	public const string PS_003_SOURCE_FORM =
		"**Editions:** PowerShell 7.x for `Invoke-RestMethod` and `Invoke-WebRequest`, where `-Authentication` exists; Windows PowerShell 5.1 and 7.x on Windows for `Start-BitsTransfer`. The 5.1 target for the web cmdlets is the header form (5.1 has no `-Authentication`).\n\n```powershell\nInvoke-RestMethod -Uri $uri -Credential $credential -Authentication Basic\n```";

	/// <summary>PS-003 target form under token authentication.</summary>
	public const string PS_003_TARGET_FORM =
		"**First, the destination:** if this is not RelativityOne — including a Relativity Server instance, whose own documentation governs — this finding is informational and the rest of this template does not apply.\n\n```powershell\n# PowerShell 7.x. $secureToken is the access token as a SecureString, as in the token-acquisition part.\nInvoke-RestMethod -Uri $uri -Authentication Bearer -Token $secureToken -Headers @{ 'X-CSRF-Header' = '-' }\n```\n\n```powershell\n# Windows PowerShell 5.1, which has no -Authentication, and any edition that builds the header itself\nInvoke-RestMethod -Uri $uri -Headers @{ Authorization = \"Bearer $accessToken\"; 'X-CSRF-Header' = '-' }\n```";

	/// <summary>PS-003 class share of the credential provenance.</summary>
	public const string PS_003_CREDENTIAL_PROVENANCE =
		"**Where the credentials come from.** This is the explicit form: `-Authentication Basic` sends Basic pre-emptively even to a server that challenges Bearer (*checked by NSerio locally*, 7.6.6), so on RelativityOne the credential is sent where RelativityOne documents bearer authentication only. If the destination is not RelativityOne, the migration is that service's own. If it is, the token comes from the token-acquisition part that follows.";

	/// <summary>PS-003 verification note.</summary>
	public const string PS_003_VERIFICATION_NOTE =
		"**Verification note — three things.** **`-Token` takes a `SecureString`** (documented by Microsoft), so the access token is converted before the call. **A script that sent Basic over `http://` needed `-AllowUnencryptedAuthentication`** (7.x refuses the credential otherwise, *checked by NSerio locally*): remove it with the Basic credential, and use HTTPS for the bearer token. **For `Start-BitsTransfer`**, whose `-Authentication` offers no Bearer value, the header route through `-CustomHeaders` is the expected one (*inferred*; not tested).";

	/// <summary>PS-004 generic source form.</summary>
	public const string PS_004_SOURCE_FORM =
		"**Editions:** Windows PowerShell 5.1 and PowerShell 7.x.\n\n```powershell\nInvoke-RestMethod -Uri $uri -Headers @{ Authorization = \"Basic $encoded\" }\n```";

	/// <summary>PS-004 target form under token authentication.</summary>
	public const string PS_004_TARGET_FORM =
		"**First, the destination:** if this is not RelativityOne — including a Relativity Server instance, whose own documentation governs — this finding is informational and the rest of this template does not apply.\n\n```powershell\nInvoke-RestMethod -Uri $uri -Headers @{ Authorization = \"Bearer $accessToken\"; 'X-CSRF-Header' = '-' }\n```";

	/// <summary>PS-004 class share of the credential provenance.</summary>
	public const string PS_004_CREDENTIAL_PROVENANCE =
		"**Where the credentials come from.** A hand-built header establishes nothing about the destination or about where the Base64 value came from. If the destination is RelativityOne, delete the encoding step that produced `$encoded`, and obtain the token as the token-acquisition part describes.";

	/// <summary>PS-004 verification note.</summary>
	public const string PS_004_VERIFICATION_NOTE =
		"**Verification note — two things.** **`X-CSRF-Header` is required on REST and Kepler calls** and a Kepler call without it returns 404: a Basic header that worked without it will fail with it missing. **A token has a finite lifetime**: rebuild the header with a current token.";

	/// <summary>PS-005 generic source form.</summary>
	public const string PS_005_SOURCE_FORM =
		"**Editions:** Windows PowerShell 5.1 and PowerShell 7.x.\n\n```powershell\n$headerTable.Add('Authorization', 'Basic ' + $encoded)\n```";

	/// <summary>PS-005 target form under token authentication.</summary>
	public const string PS_005_TARGET_FORM =
		"**First, the destination:** if this is not RelativityOne — including a Relativity Server instance, whose own documentation governs — this finding is informational and the rest of this template does not apply.\n\n```powershell\n$headerTable.Add('Authorization', \"Bearer $accessToken\")\n$headerTable['X-CSRF-Header'] = '-'\n```";

	/// <summary>PS-005 class share of the credential provenance.</summary>
	public const string PS_005_CREDENTIAL_PROVENANCE =
		"**Where the credentials come from.** As for a hand-built header table: the write establishes nothing about the destination.";

	/// <summary>PS-005 verification note.</summary>
	public const string PS_005_VERIFICATION_NOTE =
		"**Verification note.** The same two points: `X-CSRF-Header` is required, and a token expires. If the headers collection is built once and reused for the life of the script, rebuild it with the new token.";

	/// <summary>PS-006 generic source form.</summary>
	public const string PS_006_SOURCE_FORM =
		"**Editions:** Windows PowerShell 5.1 and PowerShell 7.x.\n\n```powershell\n$request.Headers.Add([System.Net.HttpRequestHeader]::Authorization, \"Basic $encoded\")\n```";

	/// <summary>PS-006 target form under token authentication.</summary>
	public const string PS_006_TARGET_FORM =
		"**First, the destination:** if this is not RelativityOne — including a Relativity Server instance, whose own documentation governs — this finding is informational and the rest of this template does not apply.\n\n```powershell\n$request.Headers.Add([System.Net.HttpRequestHeader]::Authorization, \"Bearer $accessToken\")\n$request.Headers.Add('X-CSRF-Header', '-')\n```";

	/// <summary>PS-006 class share of the credential provenance.</summary>
	public const string PS_006_CREDENTIAL_PROVENANCE =
		"**Where the credentials come from.** As for a hand-built header table: the write establishes nothing about the destination.";

	/// <summary>PS-006 verification note.</summary>
	public const string PS_006_VERIFICATION_NOTE =
		"**Verification note.** `X-CSRF-Header` is required and a token expires. The request object is usually single-use, so the token is applied to each new request.";

	/// <summary>PS-007 generic source form.</summary>
	public const string PS_007_SOURCE_FORM =
		"**Editions:** Windows PowerShell 5.1 (after `Add-Type -AssemblyName System.Net.Http` where the type is not loaded) and PowerShell 7.x.\n\n```powershell\n$httpClient.DefaultRequestHeaders.Authorization = [System.Net.Http.Headers.AuthenticationHeaderValue]::new('Basic', $encoded)\n```";

	/// <summary>PS-007 target form under token authentication.</summary>
	public const string PS_007_TARGET_FORM =
		"**First, the destination:** if this is not RelativityOne — including a Relativity Server instance, whose own documentation governs — this finding is informational and the rest of this template does not apply.\n\n```powershell\n$httpClient.DefaultRequestHeaders.Authorization = [System.Net.Http.Headers.AuthenticationHeaderValue]::new('Bearer', $accessToken)\n$httpClient.DefaultRequestHeaders.Add('X-CSRF-Header', '-')\n```";

	/// <summary>PS-007 class share of the credential provenance.</summary>
	public const string PS_007_CREDENTIAL_PROVENANCE =
		"**Where the credentials come from.** As for a hand-built header table: the construction establishes nothing about the destination.";

	/// <summary>PS-007 verification note.</summary>
	public const string PS_007_VERIFICATION_NOTE =
		"**Verification note — two things, and the first can break working code.** **A default header on a client lives as long as the client:** a client built once keeps the expired token, so rebuild the header with a current token within its lifetime. `X-CSRF-Header` is required. On Windows PowerShell 5.1 the type is loaded with `Add-Type -AssemblyName System.Net.Http` where it is not already.";

	/// <summary>PS-008 generic source form.</summary>
	public const string PS_008_SOURCE_FORM =
		"**Editions:** Windows PowerShell 5.1 and PowerShell 7.x.\n\n```powershell\n$cache.Add([Uri]$uri, 'Basic', $networkCredential)\n```";

	/// <summary>PS-008 target form under token authentication.</summary>
	public const string PS_008_TARGET_FORM =
		"**First, the destination:** if this is not RelativityOne — including a Relativity Server instance, whose own documentation governs — this finding is informational and the rest of this template does not apply.\n\nRemove the cache entry, and send the bearer header on the request instead:\n\n```powershell\nInvoke-RestMethod -Uri $uri -Headers @{ Authorization = \"Bearer $accessToken\"; 'X-CSRF-Header' = '-' }\n```";

	/// <summary>PS-008 class share of the credential provenance.</summary>
	public const string PS_008_CREDENTIAL_PROVENANCE =
		"**Where the credentials come from.** The call names the scheme but not the destination. If it is RelativityOne, the token comes from the token-acquisition part that follows, and the user name and password that built the `NetworkCredential` are no longer used.";

	/// <summary>PS-008 verification note.</summary>
	public const string PS_008_VERIFICATION_NOTE =
		"**Verification note.** A credential cache can serve several requests and addresses; **check every request that used the cache** before removing it, because the cache is shared state. `X-CSRF-Header` is required and a token expires.";

	/// <summary>PS-009 generic source form.</summary>
	public const string PS_009_SOURCE_FORM =
		"**Editions:** Windows PowerShell 5.1 and PowerShell 7.x.\n\n```powershell\n$handler.Credentials = New-Object System.Net.NetworkCredential($loginName, $loginSecret)\n```";

	/// <summary>PS-009 target form under token authentication.</summary>
	public const string PS_009_TARGET_FORM =
		"**First, the destination:** if this is not RelativityOne — including a Relativity Server instance, whose own documentation governs — this finding is informational and the rest of this template does not apply.\n\nRemove the credential assignment and send the bearer header with the requests the client makes:\n\n```powershell\n$httpClient.DefaultRequestHeaders.Authorization = [System.Net.Http.Headers.AuthenticationHeaderValue]::new('Bearer', $accessToken)\n$httpClient.DefaultRequestHeaders.Add('X-CSRF-Header', '-')\n```";

	/// <summary>PS-009 class share of the credential provenance.</summary>
	public const string PS_009_CREDENTIAL_PROVENANCE =
		"**Where the credentials come from.** `NetworkCredential` declares no scheme: the type serves integrated authentication, mail, file transfer and proxies. This finding is reported because the credential is bound to an HTTP client or handler; **it may not be Basic at all.** Establish that, and the destination, before changing it.";

	/// <summary>PS-009 verification note.</summary>
	public const string PS_009_VERIFICATION_NOTE =
		"**Verification note.** **Confirm that this client really sends Basic.** A handler holding a credential is documented to answer an authentication challenge (*inferred* for this construct); against a server that challenges with Bearer only, as RelativityOne does (probed for the web cmdlets, not for this type), it may send nothing. If it sends nothing, the script already authenticates by other means and this change is not needed. `X-CSRF-Header` is required; a token expires.";

	/// <summary>PS-010 generic source form.</summary>
	public const string PS_010_SOURCE_FORM =
		"**Editions:** Windows PowerShell 5.1 and PowerShell 7.x.\n\n```powershell\n$encoded = [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes(\"$($loginName):$($loginSecret)\"))\n```";

	/// <summary>PS-010 target form under token authentication.</summary>
	public const string PS_010_TARGET_FORM =
		"**First, the destination:** if this is not RelativityOne — including a Relativity Server instance, whose own documentation governs — this finding is informational and the rest of this template does not apply.\n\nDelete the encoding. A user name and password are not exchanged on each call: obtain a token and send it.\n\n```powershell\n$headerTable = @{ Authorization = \"Bearer $accessToken\"; 'X-CSRF-Header' = '-' }\n```";

	/// <summary>PS-010 class share of the credential provenance.</summary>
	public const string PS_010_CREDENTIAL_PROVENANCE =
		"**Where the credentials come from.** A colon-joined pair that is Base64-encoded is the Basic credential. If it is for RelativityOne, the OAuth2 Client replaces it and its secret is displayed once; the user name and password stop being used by this script. If the encoded pair is not a credential (a host and a port, a key and a value), this finding does not apply.";

	/// <summary>PS-010 verification note.</summary>
	public const string PS_010_VERIFICATION_NOTE =
		"**Verification note — two things.** **The pair may not be a credential**: the encoded value could be any colon-joined pair, a cost this tool declares. **The calling identity changes** with the flow chosen, as the token-acquisition part states.";

	/// <summary>PS-011 generic source form.</summary>
	public const string PS_011_SOURCE_FORM =
		"**Editions:** Windows PowerShell 5.1 and PowerShell 7.x.\n\n```powershell\n$pair = \":\" + $accessToken\n$encoded = [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes($pair))\n```";

	/// <summary>PS-011 target form under token authentication.</summary>
	public const string PS_011_TARGET_FORM =
		"A token is being sent under the `Basic` prefix. The migration exchanges no credential and does not depend on the destination being RelativityOne: **delete the Base64 step and change the prefix to `Bearer`**.\n\n```powershell\n$headerTable = @{ Authorization = \"Bearer $accessToken\" }\n```";

	/// <summary>PS-011 class share of the credential provenance.</summary>
	public const string PS_011_CREDENTIAL_PROVENANCE =
		"**Where the credentials come from.** The token's provenance depends on the destination. If the destination is RelativityOne, the token must be one that RelativityOne issued, from the token-acquisition part that follows; a token issued by another service is that service's own concern, and the prefix change applies to any destination that accepts `Bearer`.";

	/// <summary>PS-011 verification note.</summary>
	public const string PS_011_VERIFICATION_NOTE =
		"**Verification note.** **Confirm the destination accepts `Bearer`** with that token. On RelativityOne, add `X-CSRF-Header: -` on REST and Kepler calls and expect the token to expire.";
}
