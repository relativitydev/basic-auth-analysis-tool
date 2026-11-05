# Migration Suggestions for PowerShell (Part C of the Migration Documentation)

Part C tells you what to do when a Part A pattern is found in your PowerShell scripts. It gives the same recommendation the tool gives for that finding, because the recommendations below are produced by the same recommendation catalog the tool reports from, and are not written separately.

This document has three kinds of text:

- **Hand-written guidance.** How to confirm which OAuth 2.0 flow applies, and how to judge a match the tool marked Low confidence.
- **Recommendations.** One entry for each pattern class and each flow, and one generic recommendation for each flow. Each sits between a pair of `generated` markers, which are invisible when the document is rendered.
- **Pointers.** Part A ([`part-a-powershell.md`](part-a-powershell.md)) describes each pattern and how to search for it. Secret Management (Part B of the Migration Documentation, [`part-b-secret-management.md`](part-b-secret-management.md)) says where the new secrets go.

Every example uses placeholders such as `<host>`, `<client id>` and `<client secret>`. Replace them with your own values, and keep real secrets out of scripts.

The two flows are the only ones the tool recommends:

- **Client Credentials** is the default. The script sends its client ID and client secret to the token endpoint and receives an access token. No user is involved, and every call runs as the OAuth2 Client's Context User.
- **Authorization Code** is called "Code" in RelativityOne's OAuth2 Client form. The script redirects a signed-in user to RelativityOne, receives an authorization code at a registered Redirect URL, and exchanges it for a token that carries that user's permissions.

PowerShell runs in two editions, Windows PowerShell 5.1 and PowerShell 7, and where they differ an entry says so and shows what to write for each.

## Confirming the flow

Ask one question of the script that holds the match: **who supplies the credential at run time?**

- If a person types it or signs in, the integration acts for that person. Use **Authorization Code**.
- If it comes from an environment variable, a stored credential file, a secret store or a parameter, and no person is present, use **Client Credentials**.

The tool answers that question by looking for signals in the **same file** as the match. Part A lists every signal by name under [Flow signals](part-a-powershell.md#flow-signals). The rules it applies are these:

1. **An interactive-user signal decides Authorization Code.** Interactive-user signals are a `Get-Credential` prompt, a `Read-Host` that reads with the echo suppressed or asks for a password, the host's credential dialog, and a desktop password control. If an interactive-user signal and a fixed-credential signal are both present, the interactive one wins.
2. **A fixed-credential signal decides Client Credentials.** Fixed-credential signals are a read of a credential-named environment variable, an `Import-Clixml` read, a call of `Get-Secret` or one of the other two secret-store commands the tool lists, and a `ConvertTo-SecureString -AsPlainText` of a value that is not a quoted literal. The recommendation carries a note asking you to check, because nothing yet measures how often such a signal points the wrong way.
3. **No signal means the default.** With no signal in the file, the tool recommends Client Credentials and carries the same note. This is the expected result for most real scripts and not a failure of the tool, for the reason below.

**The tool reads one file at a time.** A prompt in a script that this one dot-sources, a credential read in an imported module, and a credential that a calling script passes in as a parameter are not seen. Such a match is decided by the default even when the script acts for a signed-in user. When you review a finding, look beyond its file: follow the credential back to where it comes from, and if a person supplies it, use Authorization Code even though the tool recommended Client Credentials.

**A script that does both is read as interactive.** A script that uses a stored or environment credential when there is one and falls back to `Get-Credential` when there is not holds both kinds of signal, so the interactive one wins and the tool recommends Authorization Code. If that script is in practice an unattended integration, the credential is the stored one and Client Credentials is the right flow. Decide by how the script runs, not by the fallback.

**A `Get-Credential` that feeds only a remoting command is not a signal.** When `Get-Credential` is written directly as the `-Credential` argument of `Invoke-Command`, `Enter-PSSession`, `New-PSSession`, `New-PSDrive`, `Start-Process`, `Start-Job`, `New-CimSession` or a similar command, the credential is for Windows and not for RelativityOne, and the tool does not count it. A `Get-Credential` that is first kept in a variable and passed to such a command later is counted.

Two more rules the tool applies, so that you can reproduce its choice by hand:

- A credential written literally into the script is **not** evidence of either flow. It is a secret to remove, and Part B explains how. A file whose only credential is a literal is decided by the default.
- The tool works from the scanned code and local configuration only. It never contacts RelativityOne, so it cannot tell you what OAuth2 Clients your instance already has.

**Confirm against your instance.** If your RelativityOne instance already has an OAuth2 Client with the Code flow for this integration, use Authorization Code, whatever the tool recommended. If it has one with Client Credentials for this integration, use Client Credentials. The flow is fixed when an OAuth2 Client is created, so moving an integration from one flow to the other means creating a new OAuth2 Client.

**Changing the default.** If most of your integrations are user-facing, you can make Authorization Code the default. Set `defaultFlow` to `AuthorizationCode` in the `.migrate-auth.json` file in your user profile folder; the accepted values are `ClientCredentials` and `AuthorizationCode`. The tool reads this setting from the user profile file only. A `defaultFlow` in the scanned repository's own file is ignored and recorded as ignored, so a repository cannot change the recommendation for the findings it holds. The setting changes only the default; a signal in the file still decides the flow.

## Judging a Low-confidence match

A match is **Low** confidence when it is present in the text but unlikely to be live authenticating code. The tool gives a Low match no flow and no recommendation, and lists it for manual review. **A Low level is not evidence that the code does not authenticate.** It means a person has to look. The paragraphs below say how to judge each cause and when the match still needs a migration. When it does, find the live construct it belongs to, identify its class in Part A, and follow that class's entry in [Migration suggestions by pattern class](#migration-suggestions-by-pattern-class).

### Comments

The match is inside a `#` line comment or a `<# #>` block comment, so no code runs. Read the comment. If it is commented-out code, nothing needs migrating today, but delete it or migrate it before anyone restores it, because a restored line brings Basic Authentication back. If it describes code that exists elsewhere, search for that code with Part A and migrate the live code. If it only illustrates a pattern with placeholders, no migration is needed.

### Documentation strings

The match is inside text that documents or explains, such as comment-based help (`.SYNOPSIS`, `.DESCRIPTION` or `.EXAMPLE` inside a `<# #>` block), a help message, an error message, a here-string that holds a sample, or a usage example shown to the user. The text does not authenticate anything. Check that the behaviour it describes has been migrated, and update the wording afterwards so that the documentation stops describing Basic Authentication. The text itself needs no migration.

### Test fixtures and mocks

**Test code is not Low.** A match in a file the tool recognizes as test code is reported at **Medium** at most, with the recommendation of its class, and the report marks the finding `Test code: yes` (the JSON data file sets `testCode` to `true`). The tool recognizes test code by a folder named `test`, `tests`, `fixture` or `fixtures`, a file name that ends in `.Tests.ps1`, or Pester content (a `Describe` or `Context` block opened with a quoted name, with `It` and `Should`). Judge by what the test talks to. If it feeds a mock and never reaches RelativityOne, no migration is needed, though you may want the mock to model the token flow so the test still represents the script it covers. If it is an integration test that authenticates to a real RelativityOne instance, it needs migrating with everything else, because it stops working when Basic Authentication is switched off: follow the entry of its class. A credential written literally into a test is still a secret to remove.

**A mock is Low.** A match in a file the tool recognizes as a mock, fake or stub is reported at Low, with no recommendation: a folder named `mock` or `mocks`. The tool recognizes a mock by its folder and by nothing else, so a mock that has a neutral name is reported at the level of its class, or at Medium when it also sits in test code; review it as you would a test. A file that is test code and also a mock, a sample, vendored or generated is Low, and the report still says it is test code. For a Low mock, judge by what it stands in for: if nothing runs it against RelativityOne, no migration is needed.

### Sample and example code

The match sits in a folder named `sample`, `samples`, `example` or `examples`, in any letter case, or in a file whose name begins with `Sample` or `Example`. Those are the only names the tool recognizes: a sample kept in a folder called `demo`, or in a file whose name merely contains the word, is not demoted and is reported at its ordinary level, so review it as live code. Judge by who uses it. If nobody runs the sample, it needs no migration. If people copy it as a starting point, migrate it: an example that teaches Basic Authentication spreads it to new scripts. Use the recommendation for the class the sample matches, and prefer the flow that fits the scenario the sample shows.

### Vendored and generated files

The match sits in a file produced by a tool (a generated-file header, or a `.generated.ps1` or `.g.ps1` file name) or in a folder named `vendor` or `third-party`, which holds third-party code. **Do not edit a generated file by hand:** the next generation overwrites the edit. Fix the source it is generated from (the template, the schema or the generator settings) and regenerate. For vendored code, upgrade the module to a version that supports token authentication, or replace it. A migration is needed when the file runs against RelativityOne in your environment. It is not needed when the file is never called, or when it is only reference material.

### Deployment folders

A folder named `Deployment` does not lower a PowerShell finding: in PowerShell that folder holds the deployment scripts this tool exists to find, so a script under it is reported at its ordinary level and this cause does not arise for it. If a Low match is reported for a deployment folder all the same, read the file before you decide, because a deployment folder is also where an installer or a release script that talks to RelativityOne is kept. A migration is needed when the file is the one that runs against RelativityOne in your application or in the deployment itself. It is not needed when the file is never run, or when it only illustrates a pattern. Judge by who runs it and against which server: a deployment script that signs in to RelativityOne with a user name and a password stops working when Basic Authentication is switched off, and it is a script people copy from one environment to the next, so migrate it with everything else. A credential written literally into a deployment script is still a secret to remove.

### PS-012 · CredentialOnWebCmdletWithoutScheme

<a id="ps-012-judging"></a>

A web command such as `Invoke-RestMethod` that is given a credential with `-Credential`, or a web request session whose `.Credentials` is assigned, with no scheme named. The tool marks it Low because, as written, the credential is not sent as Basic to RelativityOne: when the server answers a request with a bearer challenge, as RelativityOne does, PowerShell sends no credential at all, and when it answers with a Basic challenge, it sends Basic. Confirm where the call points. If it points at RelativityOne, the call already fails to authenticate with that credential, and you should look for the `-Authentication Basic` parameter or the hand-built header the script also uses, which is reported under its own class, and migrate that. If it points at another server that asks for Basic, the call is that service's own authentication and needs no migration to RelativityOne.

### PS-013 · NonHttpCredentialSink

<a id="ps-013-judging"></a>

A credential that is handed to a command or object that is not an HTTP client: a remoting, drive, process, job, CIM or WMI command, `Send-MailMessage`, or a mail client, web proxy, file-transfer request or directory object. The tool marks it Low because it does not believe it is Basic Authentication towards RelativityOne, and it exists so that remoting, mail, file-transfer, directory and proxy credentials stop being reported as migration targets. Confirm the sink. If the credential really goes to another computer, a mail server, a file server, a directory or a proxy, no migration to RelativityOne is needed, and the credential's secret handling is still governed by your own policy. If the same credential is also used to call RelativityOne, that use is reported separately under the class that matches it, and needs migrating.

### PS-014 · AuthenticationHelperDefinition

<a id="ps-014-judging"></a>

A function or filter that builds the credential or the authentication header for the script is a helper. The tool marks its definition Low because the advice for a helper is not a mechanical code change. **The helper is the place to migrate, and it hides its callers:** the tool reports the helper's definition once, and its call sites do not appear as findings because they contain no visible credential. Search for the helper's name to find every caller. If the helper builds Basic Authentication for calls to RelativityOne, migrate it once, so that it obtains and caches an access token and sends a Bearer header. Decide the flow from who supplies the credential to its callers: a person means Authorization Code, and a fixed credential means Client Credentials. Then follow the entry for the construct the helper contains, if it has one. If the helper builds a credential for another service, it needs no migration.

## Migration suggestions by pattern class

Each pattern class has two entries, one for each flow. Choose the entry for the flow you confirmed above. Every entry contains the code being replaced, the migrated code, a description of token acquisition, caching and expiry handling, how to confirm the flow and the default, the OAuth2 Client to create or confirm with its settings, and the pointer to Secret Management (Part B of the Migration Documentation). Every Authorization Code entry adds a description of refresh-token handling, of PKCE, and of a server-side variant and a desktop variant.

**Part 1, part 2 and part 3.** The text of an entry refers to its own pieces by number. The same names appear in the report's recommendations. Each piece has a label in the entry:

- **Part 1** is the change at the point of use: the text under the label *Under token authentication*, with its migrated code.
- **Part 2** is the token acquisition: the part of the text under the label *Credentials* that follows the paragraph *Where the credentials come from*. It describes the request to the token endpoint, the caching of the token and what to do when it expires. A Client Credentials entry starts it with *Where the token comes from*, and an Authorization Code entry with *Common to both variants*.
- **Part 3** is the OAuth2 Client settings, under the label *OAuth2 Client* at the end of the entry.

The labels *Source form* (the construct being replaced) and *Verification* (what to check before you deploy) are not numbered.

The three Low classes, PS-012, PS-013 and PS-014, have no entry because they produce no recommendation. They are covered under [Judging a Low-confidence match](#judging-a-low-confidence-match).

### PS-001 · SdkUserNamePasswordCredentials

<a id="ps-001-migration"></a>

#### Client Credentials

<!-- generated:begin PS-001 client-credentials -->
**Confirming the flow.** When no signal in a file decides the flow, the tool assigns Client Credentials, the default, with this note: "Who supplies the credential at run time? If a person types it or signs in, the integration acts for that person: use Authorization Code. If it comes from configuration, settings, an environment variable or a secret store: use Client Credentials. No signal in this file showed which applies, so Client Credentials, the default, is recommended. Use Authorization Code instead if the integration acts on behalf of a signed-in user, or if your RelativityOne instance already has a Code-flow OAuth2 Client for this integration." A fixed-credential signal assigns it with this note: "A fixed-credential signal in this file points to Client Credentials, but how often that kind of signal is right has not been measured. Use Authorization Code instead if the integration acts on behalf of a signed-in user, or if your RelativityOne instance already has a Code-flow OAuth2 Client for this integration." See [Confirming the flow](#confirming-the-flow) and the "Which flow" paragraph of PS-001 in [Part A](part-a-powershell.md#ps-001).

Source form: **Editions:** Windows PowerShell 5.1 and PowerShell 7.x; whether the SDK assembly loads under 7.x depends on how it is built and is not established here.

```powershell
$credentials = New-Object <SdkNamespace>.UsernamePasswordCredentials($loginName, $loginSecret)
$settings = New-Object <SdkNamespace>.ServiceFactorySettings($restEndpointUri, $credentials)
```

Under token authentication: **First, the destination:** if this Relativity instance is not RelativityOne — including a Relativity Server instance, whose own documentation governs — this finding is informational and the rest of this template does not apply.

```powershell
$bearer = New-Object <SdkNamespace>.BearerTokenCredentials($accessToken)
$settings = New-Object <SdkNamespace>.ServiceFactorySettings($restEndpointUri, $bearer)
```

Credentials: **Where the credentials come from.** The SDK type establishes that the destination is Relativity, not that it is RelativityOne. If it is not RelativityOne the migration is that service's own token mechanism, this finding is informational for this tool, and the reader stops here. If it is RelativityOne, the token comes from the token-acquisition part that follows.

**Where the token comes from.** Create an OAuth2 Client with Flow Grant Type set to Client Credentials (part 3 lists its settings), then send a form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=client_credentials`, `client_id`, `client_secret` and a `scope`. The token endpoint is **documented** for this grant. The token is then sent as `Authorization: Bearer <token>`, with `X-CSRF-Header: -` on REST and Kepler calls — the header is documented as required, and a Kepler call without it returns 404.

```powershell
# Windows PowerShell 5.1 and PowerShell 7.x. $clientId and $clientSecret come from the process
# environment or a secret store (Secret Management (Part B of the Migration Documentation)), never from this script.
$tokenBody = @{
	grant_type    = 'client_credentials'
	client_id     = $clientId
	client_secret = $clientSecret
	scope         = $scope
}
$tokenResponse = Invoke-RestMethod -Method Post -Uri "$instanceUrl/Relativity/Identity/connect/token" -Body $tokenBody -ContentType 'application/x-www-form-urlencoded'
$accessToken = $tokenResponse.access_token
$headerTable = @{ Authorization = "Bearer $accessToken"; 'X-CSRF-Header' = '-' }
```

```powershell
# Windows PowerShell 5.1 only, before the first request, if the host does not already negotiate TLS 1.2
# (inferred from the .NET Framework default; the local mock-server run is plain http, so it does not exercise TLS).
[Net.ServicePointManager]::SecurityProtocol = [Net.ServicePointManager]::SecurityProtocol -bor [Net.SecurityProtocolType]::Tls12
```

```powershell
# PowerShell 7.x alternative to the header: the cmdlet sends the bearer token itself
# (checked by NSerio locally, 7.6.6: Authorization: Bearer is sent pre-emptively).
$secureToken = ConvertTo-SecureString $accessToken -AsPlainText -Force
Invoke-RestMethod -Uri $uri -Authentication Bearer -Token $secureToken -Headers @{ 'X-CSRF-Header' = '-' }
```

**The scope depends on what the integration needs, and no single scope is required.** Relativity's documentation shows two values for this grant: `SystemUserInfo` on the REST authentication page, the Usage Reports page and in the official Transfer SDK sample, and `UserInfoAccess` in the Kepler .NET samples. No page says which is correct, whether both work, or what either grants, and the OAuth2 Client's `Scopes` property is documented as one that cannot be set. Use the value the API you call documents, and confirm it.

**Cache the token, and re-acquire it on expiry — do not refresh.** Keep it in a script-scoped variable for its lifetime (part 3 recommends 60 minutes, Relativity's own wording), not on disk, and request a new one with the client secret when it expires or a call is rejected for authentication. Client Credentials offers no refresh tokens: **verified by NSerio against a live instance; not documented by Relativity** — the Allow Refresh Tokens control does not appear for this grant in the RelativityOne user interface.

**The client secret.** It is displayed once, at creation or reset. Store it as Secret Management (Part B of the Migration Documentation) describes; never in the script, never in tracked configuration. A credential saved with `Export-Clixml` is bound to the user and machine that saved it on Windows and is not protected on Linux or macOS (documented by Microsoft). A reset invalidates the previous secret immediately, so plan the cut-over before resetting it.

**Before deploying, confirm the calling identity.** Every call now runs as the OAuth2 Client's **Context User**, not as the user in the replaced credential. If that user has different workspace permissions or item-level security, the script returns fewer rows or `Unauthorized` with no change to its logic, and Relativity's audit attributes every call to that account. Confirm the Context User's permissions match those of the previous identity.

**When the integration acts for a signed-in user.** Client Credentials is one service identity by construction. That is the Authorization Code case, and moving to it means a new OAuth2 Client, because the flow is fixed when a client is created. The finding's flow note says when to make that choice.

Verification: **Verification note — two things, and the first can break working code.** **A token has a finite lifetime**, set per client in minutes, and this construct holds it: a factory built once and kept for the life of a script keeps the token after it expires, and the calls then fail authentication. Rebuild the settings and the factory with a current token. **Confirm the SDK version in use provides `BearerTokenCredentials`**; where it does not, this is an SDK upgrade before it is a code change.

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
<!-- generated:end PS-001 client-credentials -->

#### Authorization Code

<!-- generated:begin PS-001 authorization-code -->
**Confirming the flow.** An interactive-user signal in a file assigns Authorization Code, with this note: "An interactive-user signal in this file points to Authorization Code. Confirm the flow: use Client Credentials instead if the integration runs with credentials fixed for the integration and no signed-in user." Your user profile can also make Authorization Code the default; see Changing the default in [Confirming the flow](#confirming-the-flow). See [Confirming the flow](#confirming-the-flow) and the "Which flow" paragraph of PS-001 in [Part A](part-a-powershell.md#ps-001).

Source form: **Editions:** Windows PowerShell 5.1 and PowerShell 7.x; whether the SDK assembly loads under 7.x depends on how it is built and is not established here.

```powershell
$credentials = New-Object <SdkNamespace>.UsernamePasswordCredentials($loginName, $loginSecret)
$settings = New-Object <SdkNamespace>.ServiceFactorySettings($restEndpointUri, $credentials)
```

Under token authentication: **First, the destination:** if this Relativity instance is not RelativityOne — including a Relativity Server instance, whose own documentation governs — this finding is informational and the rest of this template does not apply.

```powershell
$bearer = New-Object <SdkNamespace>.BearerTokenCredentials($accessToken)
$settings = New-Object <SdkNamespace>.ServiceFactorySettings($restEndpointUri, $bearer)
```

Credentials: **Where the credentials come from.** The SDK type establishes that the destination is Relativity, not that it is RelativityOne. If it is not RelativityOne the migration is that service's own token mechanism, this finding is informational for this tool, and the reader stops here. If it is RelativityOne, the token comes from the token-acquisition part that follows.

**Common to both variants**

**Sign in.** Replace the code that collected the user's credentials with a redirect: send the user's browser to the authorize endpoint `<host>/Relativity/Identity/connect/authorize`, which leads to the RelativityOne login page. The authorize endpoint is **verified by NSerio against a live instance; not documented by Relativity**. The request carries the standard authorization-code parameters — `client_id`, `response_type=code`, the registered `redirect_uri`, a `scope`, and a `state` value the script checks on return; these are standard OAuth, and Relativity documents none of them.

**Receive the code.** The browser returns to the Redirect URL registered on the OAuth2 Client, with the authorization code and the `state` value. Check the `state` before using the code.

**Exchange the code.** Send a form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=authorization_code`, the `code`, the same `redirect_uri`, the `client_id` and the `client_secret`, plus the `code_verifier` when PKCE is used. The exchange is **verified by NSerio against a live instance; not documented by Relativity**.

**Use the token.** Send `Authorization: Bearer <access token>` — the change at the use site in part 1 — with `X-CSRF-Header: -` on REST and Kepler calls.

**On expiry.** With Allow Refresh Tokens enabled on the OAuth2 Client and PKCE used, the script holds a refresh token and, when the access token expires, sends a `grant_type=refresh_token` request with it in place of the code exchange — *inferred* from the standard OAuth shape, not tested. Without a refresh token the user signs in again. Relativity does not document a refresh token's lifetime, whether it rotates, or how it is revoked. A refresh token is a secret: store it as Secret Management (Part B of the Migration Documentation) describes, not in a plain file.

**PKCE.** PKCE is required by Relativity's own documentation to obtain a refresh token, and not otherwise: a Code request that does not ask for a refresh token works without it, **verified by NSerio against a live instance; not documented by Relativity**. NSerio recommends PKCE with the `S256` method (NSerio recommendation), and does not claim that Relativity accepts any particular method, which it does not document.

**Scope.** No scope set is prescribed for Code. The set NSerio used works, **verified by NSerio against a live instance; not documented by Relativity**, verified as working and not as minimal.

**The script stops collecting the password.** After this change the script never receives, stores or forwards the user's user name and password: remove the prompt (`Get-Credential`, `Read-Host`) and any storage of the password.

**Single sign-on.** Sign-in happens on RelativityOne's own login page, so the instance's single sign-on and two-factor rules apply unchanged — *inferred* from where the sign-in happens, and not documented for clients that customers create.

**The client secret is required.** Code does not work without a client secret: tested and rejected, **verified by NSerio against a live instance; not documented by Relativity**, which confirms Relativity's own statement that the grant requires one. It is displayed once and is stored as Secret Management (Part B of the Migration Documentation) describes.

**Server-side variant.** For a script that runs on a server and receives the callback on a route of an application: one exact HTTPS Redirect URL per environment, no wildcards, registered by the Relativity administrator (NSerio recommendation; Relativity's matching rules are not documented); the secret and the user's tokens stay on the server. PowerShell is rarely the host of such a route; the server-side variant of the other languages applies unchanged.

**Desktop variant.** For a script a person runs on their own machine:

- **The system browser.** Open the authorize URL in the default browser, not in a web view the script hosts, so the script never sees the password (NSerio recommendation).
- **A loopback redirect on a fixed port.** Listen on the loopback address only, on the registered port, for one request. The Redirect URL is `http://127.0.0.1:<port>/<path>`: a plain-HTTP loopback URL is accepted, **verified by NSerio against a live instance; not documented by Relativity**. The Relativity administrator registers that exact URL, so the port is fixed and not chosen at runtime. **`HttpListener` accepts only a prefix that ends in `/`**, so the script appends one to the prefix it listens on and leaves the registered Redirect URL as registered: a request for `<path>?code=…` is received by the prefix `<path>/` (*checked by NSerio locally*, Windows PowerShell 5.1 and PowerShell 7.6.6: `Prefixes.Add` rejects a prefix without the slash, and the prefix with it answered both `/<path>?code=…` and `/<path>/?code=…`). `HttpListener` starts on the loopback address from a non-elevated process on both editions (*checked by NSerio locally*; a true standard account is not yet tested).
- **The secret is extractable.** Code requires a client secret, so a distributed script has to embed one, and **a script is text: anyone who can read the file has the secret.** It protects nothing against the person who runs the script.
- **Compensating guidance** (NSerio recommendation, subject to your own security policy): set Trusted IPs where outbound addresses are known and stable — they restrict where a token can be *requested* — use the recommended short Access Token Lifetime of 60 minutes, enable refresh tokens only when needed, and create one OAuth2 Client per distribution.
- **A known issue.** Relativity documents no recommendation for a distributed desktop application; this design is what NSerio verified works.

```powershell
# Windows PowerShell 5.1 and PowerShell 7.x. Executed against a local mock server on both, not against a live instance.
# $redirectUri is the exact URL registered on the OAuth2 Client, with a fixed port.
$state = [guid]::NewGuid().ToString('N')
$generator = [System.Security.Cryptography.RandomNumberGenerator]::Create()
$verifierBytes = New-Object byte[] 32
$generator.GetBytes($verifierBytes)
$verifier = [Convert]::ToBase64String($verifierBytes).TrimEnd('=').Replace('+', '-').Replace('/', '_')
$hasher = [System.Security.Cryptography.SHA256]::Create()
$challengeBytes = $hasher.ComputeHash([Text.Encoding]::ASCII.GetBytes($verifier))
$challenge = [Convert]::ToBase64String($challengeBytes).TrimEnd('=').Replace('+', '-').Replace('/', '_')

$listener = New-Object System.Net.HttpListener
$listener.Prefixes.Add($redirectUri.TrimEnd('/') + '/')   # a prefix must end in '/'; the registered URL is unchanged
$listener.Start()
$authorizeUrl = "$instanceUrl/Relativity/Identity/connect/authorize?client_id=$clientId&response_type=code" +
	"&redirect_uri=$([uri]::EscapeDataString($redirectUri))&scope=$([uri]::EscapeDataString($scope))" +
	"&state=$state&code_challenge=$challenge&code_challenge_method=S256"
Start-Process $authorizeUrl   # on Linux or macOS under 7.x use the platform opener instead (inferred)
$context = $listener.GetContext()
$query = $context.Request.QueryString
$page = [Text.Encoding]::UTF8.GetBytes('You can close this window.')
$context.Response.Close($page, $true)   # sends the page and waits until it is written, before the listener stops
$listener.Stop()
if ($query['state'] -ne $state) { throw 'The state value does not match.' }
$tokenResponse = Invoke-RestMethod -Method Post -Uri "$instanceUrl/Relativity/Identity/connect/token" -Body @{
	grant_type    = 'authorization_code'
	code          = $query['code']
	redirect_uri  = $redirectUri
	client_id     = $clientId
	client_secret = $clientSecret
	code_verifier = $verifier
}
$accessToken = $tokenResponse.access_token
```

**Why this form.** The two short .NET helpers for the hash and for random bytes exist on 7.x only, so the script creates the objects and calls their instance methods, which run on both editions (documented .NET API).

Verification: **Verification note — two things, and the first can break working code.** **A token has a finite lifetime**, set per client in minutes, and this construct holds it: a factory built once and kept for the life of a script keeps the token after it expires, and the calls then fail authentication. Rebuild the settings and the factory with a current token. **Confirm the SDK version in use provides `BearerTokenCredentials`**; where it does not, this is an SDK upgrade before it is a code change.

OAuth2 Client: Create or confirm this OAuth2 Client in RelativityOne (NSerio's observation: the menu path is Home > Authentication > OAuth2 Client Manager, as Relativity's REST API authentication page names it). This tool never connects to RelativityOne and never creates or changes the OAuth2 Client; the settings below are guidance.
- Name: a descriptive name identifying the application it serves.
- Flow Grant Type: Code
- Redirect URLs: Relativity documentation: each URL includes the http or https protocol prefix. The exact callback URL is registered by the Relativity administrator. Server-side web applications, NSerio's recommendation: one exact HTTPS URL per environment, with no wildcards. Desktop and CLI applications: a plain-HTTP loopback URL, `http://127.0.0.1:<port>/<path>`, on a fixed port because the URL is registered in advance and the port cannot be chosen at runtime (verified by NSerio against a live instance; not documented by Relativity).
- Access Token Lifetime: 60 minutes, stated in minutes because the field is in minutes. Relativity documentation: tokens for this flow must have a short lifetime, and the recommended value matches the Identity Server default.
- Allow refresh tokens: NSerio's recommendation: leave it off unless the application must keep a user session active beyond the access token lifetime without asking the user to sign in again. If it is on, the refresh tokens are stored as secrets, per Secret Management (Part B of the Migration Documentation).
- Enabled: on.
<!-- generated:end PS-001 authorization-code -->

### PS-002 · SdkLegacyRsapiCredentials

<a id="ps-002-migration"></a>

#### Client Credentials

<!-- generated:begin PS-002 client-credentials -->
**Confirming the flow.** When no signal in a file decides the flow, the tool assigns Client Credentials, the default, with this note: "Who supplies the credential at run time? If a person types it or signs in, the integration acts for that person: use Authorization Code. If it comes from configuration, settings, an environment variable or a secret store: use Client Credentials. No signal in this file showed which applies, so Client Credentials, the default, is recommended. Use Authorization Code instead if the integration acts on behalf of a signed-in user, or if your RelativityOne instance already has a Code-flow OAuth2 Client for this integration." A fixed-credential signal assigns it with this note: "A fixed-credential signal in this file points to Client Credentials, but how often that kind of signal is right has not been measured. Use Authorization Code instead if the integration acts on behalf of a signed-in user, or if your RelativityOne instance already has a Code-flow OAuth2 Client for this integration." See [Confirming the flow](#confirming-the-flow) and the "Which flow" paragraph of PS-002 in [Part A](part-a-powershell.md#ps-002).

Source form: **Editions:** Windows PowerShell 5.1 in practice (the legacy client is a .NET Framework library); 7.x if the assembly loads.

```powershell
$proxy = New-Object <SdkNamespace>.RSAPIClient ((New-Object System.Uri -ArgumentList $serviceAddress), (New-Object <SdkNamespace>.UsernamePasswordCredentials($loginName, $loginSecret)))
```

Under token authentication: **First, the destination:** if this Relativity instance is not RelativityOne — including a Relativity Server instance, whose own documentation governs — this finding is informational and the rest of this template does not apply.

**Two steps, not one, and in this order.** First, **move the call to Object Manager**: the legacy client is retired, so swapping the credential inside its construction migrates the scheme of a call that should not survive. Rewrite the operation as the equivalent Object Manager call. Second, apply the bearer token to that rewritten call:

```powershell
$bearer = New-Object <SdkNamespace>.BearerTokenCredentials($accessToken)
$settings = New-Object <SdkNamespace>.ServiceFactorySettings($restEndpointUri, $bearer)
```

Credentials: **Where the credentials come from.** As for the SDK credential type: the type establishes Relativity, not RelativityOne; if it is not RelativityOne the finding is informational for this tool and the reader stops here. If it is RelativityOne, the token comes from the token-acquisition part that follows.

**Where the token comes from.** Create an OAuth2 Client with Flow Grant Type set to Client Credentials (part 3 lists its settings), then send a form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=client_credentials`, `client_id`, `client_secret` and a `scope`. The token endpoint is **documented** for this grant. The token is then sent as `Authorization: Bearer <token>`, with `X-CSRF-Header: -` on REST and Kepler calls — the header is documented as required, and a Kepler call without it returns 404.

```powershell
# Windows PowerShell 5.1 and PowerShell 7.x. $clientId and $clientSecret come from the process
# environment or a secret store (Secret Management (Part B of the Migration Documentation)), never from this script.
$tokenBody = @{
	grant_type    = 'client_credentials'
	client_id     = $clientId
	client_secret = $clientSecret
	scope         = $scope
}
$tokenResponse = Invoke-RestMethod -Method Post -Uri "$instanceUrl/Relativity/Identity/connect/token" -Body $tokenBody -ContentType 'application/x-www-form-urlencoded'
$accessToken = $tokenResponse.access_token
$headerTable = @{ Authorization = "Bearer $accessToken"; 'X-CSRF-Header' = '-' }
```

```powershell
# Windows PowerShell 5.1 only, before the first request, if the host does not already negotiate TLS 1.2
# (inferred from the .NET Framework default; the local mock-server run is plain http, so it does not exercise TLS).
[Net.ServicePointManager]::SecurityProtocol = [Net.ServicePointManager]::SecurityProtocol -bor [Net.SecurityProtocolType]::Tls12
```

```powershell
# PowerShell 7.x alternative to the header: the cmdlet sends the bearer token itself
# (checked by NSerio locally, 7.6.6: Authorization: Bearer is sent pre-emptively).
$secureToken = ConvertTo-SecureString $accessToken -AsPlainText -Force
Invoke-RestMethod -Uri $uri -Authentication Bearer -Token $secureToken -Headers @{ 'X-CSRF-Header' = '-' }
```

**The scope depends on what the integration needs, and no single scope is required.** Relativity's documentation shows two values for this grant: `SystemUserInfo` on the REST authentication page, the Usage Reports page and in the official Transfer SDK sample, and `UserInfoAccess` in the Kepler .NET samples. No page says which is correct, whether both work, or what either grants, and the OAuth2 Client's `Scopes` property is documented as one that cannot be set. Use the value the API you call documents, and confirm it.

**Cache the token, and re-acquire it on expiry — do not refresh.** Keep it in a script-scoped variable for its lifetime (part 3 recommends 60 minutes, Relativity's own wording), not on disk, and request a new one with the client secret when it expires or a call is rejected for authentication. Client Credentials offers no refresh tokens: **verified by NSerio against a live instance; not documented by Relativity** — the Allow Refresh Tokens control does not appear for this grant in the RelativityOne user interface.

**The client secret.** It is displayed once, at creation or reset. Store it as Secret Management (Part B of the Migration Documentation) describes; never in the script, never in tracked configuration. A credential saved with `Export-Clixml` is bound to the user and machine that saved it on Windows and is not protected on Linux or macOS (documented by Microsoft). A reset invalidates the previous secret immediately, so plan the cut-over before resetting it.

**Before deploying, confirm the calling identity.** Every call now runs as the OAuth2 Client's **Context User**, not as the user in the replaced credential. If that user has different workspace permissions or item-level security, the script returns fewer rows or `Unauthorized` with no change to its logic, and Relativity's audit attributes every call to that account. Confirm the Context User's permissions match those of the previous identity.

**When the integration acts for a signed-in user.** Client Credentials is one service identity by construction. That is the Authorization Code case, and moving to it means a new OAuth2 Client, because the flow is fixed when a client is created. The finding's flow note says when to make that choice.

Verification: **Verification note — four things, and the first two can break working code.** **Verify equivalence before rewriting:** not every legacy operation has an Object Manager counterpart; where one does not, this is a redesign. **A token has a finite lifetime**: rebuild the factory with a current token. **Other uses of the legacy client may not appear as findings:** this tool reports it only where it is constructed with a user-name-and-password credential. **Confirm the SDK version provides `BearerTokenCredentials` and the Object Manager service.**

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
<!-- generated:end PS-002 client-credentials -->

#### Authorization Code

<!-- generated:begin PS-002 authorization-code -->
**Confirming the flow.** An interactive-user signal in a file assigns Authorization Code, with this note: "An interactive-user signal in this file points to Authorization Code. Confirm the flow: use Client Credentials instead if the integration runs with credentials fixed for the integration and no signed-in user." Your user profile can also make Authorization Code the default; see Changing the default in [Confirming the flow](#confirming-the-flow). See [Confirming the flow](#confirming-the-flow) and the "Which flow" paragraph of PS-002 in [Part A](part-a-powershell.md#ps-002).

Source form: **Editions:** Windows PowerShell 5.1 in practice (the legacy client is a .NET Framework library); 7.x if the assembly loads.

```powershell
$proxy = New-Object <SdkNamespace>.RSAPIClient ((New-Object System.Uri -ArgumentList $serviceAddress), (New-Object <SdkNamespace>.UsernamePasswordCredentials($loginName, $loginSecret)))
```

Under token authentication: **First, the destination:** if this Relativity instance is not RelativityOne — including a Relativity Server instance, whose own documentation governs — this finding is informational and the rest of this template does not apply.

**Two steps, not one, and in this order.** First, **move the call to Object Manager**: the legacy client is retired, so swapping the credential inside its construction migrates the scheme of a call that should not survive. Rewrite the operation as the equivalent Object Manager call. Second, apply the bearer token to that rewritten call:

```powershell
$bearer = New-Object <SdkNamespace>.BearerTokenCredentials($accessToken)
$settings = New-Object <SdkNamespace>.ServiceFactorySettings($restEndpointUri, $bearer)
```

Credentials: **Where the credentials come from.** As for the SDK credential type: the type establishes Relativity, not RelativityOne; if it is not RelativityOne the finding is informational for this tool and the reader stops here. If it is RelativityOne, the token comes from the token-acquisition part that follows.

**Common to both variants**

**Sign in.** Replace the code that collected the user's credentials with a redirect: send the user's browser to the authorize endpoint `<host>/Relativity/Identity/connect/authorize`, which leads to the RelativityOne login page. The authorize endpoint is **verified by NSerio against a live instance; not documented by Relativity**. The request carries the standard authorization-code parameters — `client_id`, `response_type=code`, the registered `redirect_uri`, a `scope`, and a `state` value the script checks on return; these are standard OAuth, and Relativity documents none of them.

**Receive the code.** The browser returns to the Redirect URL registered on the OAuth2 Client, with the authorization code and the `state` value. Check the `state` before using the code.

**Exchange the code.** Send a form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=authorization_code`, the `code`, the same `redirect_uri`, the `client_id` and the `client_secret`, plus the `code_verifier` when PKCE is used. The exchange is **verified by NSerio against a live instance; not documented by Relativity**.

**Use the token.** Send `Authorization: Bearer <access token>` — the change at the use site in part 1 — with `X-CSRF-Header: -` on REST and Kepler calls.

**On expiry.** With Allow Refresh Tokens enabled on the OAuth2 Client and PKCE used, the script holds a refresh token and, when the access token expires, sends a `grant_type=refresh_token` request with it in place of the code exchange — *inferred* from the standard OAuth shape, not tested. Without a refresh token the user signs in again. Relativity does not document a refresh token's lifetime, whether it rotates, or how it is revoked. A refresh token is a secret: store it as Secret Management (Part B of the Migration Documentation) describes, not in a plain file.

**PKCE.** PKCE is required by Relativity's own documentation to obtain a refresh token, and not otherwise: a Code request that does not ask for a refresh token works without it, **verified by NSerio against a live instance; not documented by Relativity**. NSerio recommends PKCE with the `S256` method (NSerio recommendation), and does not claim that Relativity accepts any particular method, which it does not document.

**Scope.** No scope set is prescribed for Code. The set NSerio used works, **verified by NSerio against a live instance; not documented by Relativity**, verified as working and not as minimal.

**The script stops collecting the password.** After this change the script never receives, stores or forwards the user's user name and password: remove the prompt (`Get-Credential`, `Read-Host`) and any storage of the password.

**Single sign-on.** Sign-in happens on RelativityOne's own login page, so the instance's single sign-on and two-factor rules apply unchanged — *inferred* from where the sign-in happens, and not documented for clients that customers create.

**The client secret is required.** Code does not work without a client secret: tested and rejected, **verified by NSerio against a live instance; not documented by Relativity**, which confirms Relativity's own statement that the grant requires one. It is displayed once and is stored as Secret Management (Part B of the Migration Documentation) describes.

**Server-side variant.** For a script that runs on a server and receives the callback on a route of an application: one exact HTTPS Redirect URL per environment, no wildcards, registered by the Relativity administrator (NSerio recommendation; Relativity's matching rules are not documented); the secret and the user's tokens stay on the server. PowerShell is rarely the host of such a route; the server-side variant of the other languages applies unchanged.

**Desktop variant.** For a script a person runs on their own machine:

- **The system browser.** Open the authorize URL in the default browser, not in a web view the script hosts, so the script never sees the password (NSerio recommendation).
- **A loopback redirect on a fixed port.** Listen on the loopback address only, on the registered port, for one request. The Redirect URL is `http://127.0.0.1:<port>/<path>`: a plain-HTTP loopback URL is accepted, **verified by NSerio against a live instance; not documented by Relativity**. The Relativity administrator registers that exact URL, so the port is fixed and not chosen at runtime. **`HttpListener` accepts only a prefix that ends in `/`**, so the script appends one to the prefix it listens on and leaves the registered Redirect URL as registered: a request for `<path>?code=…` is received by the prefix `<path>/` (*checked by NSerio locally*, Windows PowerShell 5.1 and PowerShell 7.6.6: `Prefixes.Add` rejects a prefix without the slash, and the prefix with it answered both `/<path>?code=…` and `/<path>/?code=…`). `HttpListener` starts on the loopback address from a non-elevated process on both editions (*checked by NSerio locally*; a true standard account is not yet tested).
- **The secret is extractable.** Code requires a client secret, so a distributed script has to embed one, and **a script is text: anyone who can read the file has the secret.** It protects nothing against the person who runs the script.
- **Compensating guidance** (NSerio recommendation, subject to your own security policy): set Trusted IPs where outbound addresses are known and stable — they restrict where a token can be *requested* — use the recommended short Access Token Lifetime of 60 minutes, enable refresh tokens only when needed, and create one OAuth2 Client per distribution.
- **A known issue.** Relativity documents no recommendation for a distributed desktop application; this design is what NSerio verified works.

```powershell
# Windows PowerShell 5.1 and PowerShell 7.x. Executed against a local mock server on both, not against a live instance.
# $redirectUri is the exact URL registered on the OAuth2 Client, with a fixed port.
$state = [guid]::NewGuid().ToString('N')
$generator = [System.Security.Cryptography.RandomNumberGenerator]::Create()
$verifierBytes = New-Object byte[] 32
$generator.GetBytes($verifierBytes)
$verifier = [Convert]::ToBase64String($verifierBytes).TrimEnd('=').Replace('+', '-').Replace('/', '_')
$hasher = [System.Security.Cryptography.SHA256]::Create()
$challengeBytes = $hasher.ComputeHash([Text.Encoding]::ASCII.GetBytes($verifier))
$challenge = [Convert]::ToBase64String($challengeBytes).TrimEnd('=').Replace('+', '-').Replace('/', '_')

$listener = New-Object System.Net.HttpListener
$listener.Prefixes.Add($redirectUri.TrimEnd('/') + '/')   # a prefix must end in '/'; the registered URL is unchanged
$listener.Start()
$authorizeUrl = "$instanceUrl/Relativity/Identity/connect/authorize?client_id=$clientId&response_type=code" +
	"&redirect_uri=$([uri]::EscapeDataString($redirectUri))&scope=$([uri]::EscapeDataString($scope))" +
	"&state=$state&code_challenge=$challenge&code_challenge_method=S256"
Start-Process $authorizeUrl   # on Linux or macOS under 7.x use the platform opener instead (inferred)
$context = $listener.GetContext()
$query = $context.Request.QueryString
$page = [Text.Encoding]::UTF8.GetBytes('You can close this window.')
$context.Response.Close($page, $true)   # sends the page and waits until it is written, before the listener stops
$listener.Stop()
if ($query['state'] -ne $state) { throw 'The state value does not match.' }
$tokenResponse = Invoke-RestMethod -Method Post -Uri "$instanceUrl/Relativity/Identity/connect/token" -Body @{
	grant_type    = 'authorization_code'
	code          = $query['code']
	redirect_uri  = $redirectUri
	client_id     = $clientId
	client_secret = $clientSecret
	code_verifier = $verifier
}
$accessToken = $tokenResponse.access_token
```

**Why this form.** The two short .NET helpers for the hash and for random bytes exist on 7.x only, so the script creates the objects and calls their instance methods, which run on both editions (documented .NET API).

Verification: **Verification note — four things, and the first two can break working code.** **Verify equivalence before rewriting:** not every legacy operation has an Object Manager counterpart; where one does not, this is a redesign. **A token has a finite lifetime**: rebuild the factory with a current token. **Other uses of the legacy client may not appear as findings:** this tool reports it only where it is constructed with a user-name-and-password credential. **Confirm the SDK version provides `BearerTokenCredentials` and the Object Manager service.**

OAuth2 Client: Create or confirm this OAuth2 Client in RelativityOne (NSerio's observation: the menu path is Home > Authentication > OAuth2 Client Manager, as Relativity's REST API authentication page names it). This tool never connects to RelativityOne and never creates or changes the OAuth2 Client; the settings below are guidance.
- Name: a descriptive name identifying the application it serves.
- Flow Grant Type: Code
- Redirect URLs: Relativity documentation: each URL includes the http or https protocol prefix. The exact callback URL is registered by the Relativity administrator. Server-side web applications, NSerio's recommendation: one exact HTTPS URL per environment, with no wildcards. Desktop and CLI applications: a plain-HTTP loopback URL, `http://127.0.0.1:<port>/<path>`, on a fixed port because the URL is registered in advance and the port cannot be chosen at runtime (verified by NSerio against a live instance; not documented by Relativity).
- Access Token Lifetime: 60 minutes, stated in minutes because the field is in minutes. Relativity documentation: tokens for this flow must have a short lifetime, and the recommended value matches the Identity Server default.
- Allow refresh tokens: NSerio's recommendation: leave it off unless the application must keep a user session active beyond the access token lifetime without asking the user to sign in again. If it is on, the refresh tokens are stored as secrets, per Secret Management (Part B of the Migration Documentation).
- Enabled: on.
<!-- generated:end PS-002 authorization-code -->

### PS-003 · ExplicitBasicAuthenticationParameter

<a id="ps-003-migration"></a>

#### Client Credentials

<!-- generated:begin PS-003 client-credentials -->
**Confirming the flow.** When no signal in a file decides the flow, the tool assigns Client Credentials, the default, with this note: "Who supplies the credential at run time? If a person types it or signs in, the integration acts for that person: use Authorization Code. If it comes from configuration, settings, an environment variable or a secret store: use Client Credentials. No signal in this file showed which applies, so Client Credentials, the default, is recommended. Use Authorization Code instead if the integration acts on behalf of a signed-in user, or if your RelativityOne instance already has a Code-flow OAuth2 Client for this integration." A fixed-credential signal assigns it with this note: "A fixed-credential signal in this file points to Client Credentials, but how often that kind of signal is right has not been measured. Use Authorization Code instead if the integration acts on behalf of a signed-in user, or if your RelativityOne instance already has a Code-flow OAuth2 Client for this integration." See [Confirming the flow](#confirming-the-flow) and the "Which flow" paragraph of PS-003 in [Part A](part-a-powershell.md#ps-003).

Source form: **Editions:** PowerShell 7.x for `Invoke-RestMethod` and `Invoke-WebRequest`, where `-Authentication` exists; Windows PowerShell 5.1 and 7.x on Windows for `Start-BitsTransfer`. The 5.1 target for the web cmdlets is the header form (5.1 has no `-Authentication`).

```powershell
Invoke-RestMethod -Uri $uri -Credential $credential -Authentication Basic
```

Under token authentication: **First, the destination:** if this is not RelativityOne — including a Relativity Server instance, whose own documentation governs — this finding is informational and the rest of this template does not apply.

```powershell
# PowerShell 7.x. $secureToken is the access token as a SecureString, as in the token-acquisition part.
Invoke-RestMethod -Uri $uri -Authentication Bearer -Token $secureToken -Headers @{ 'X-CSRF-Header' = '-' }
```

```powershell
# Windows PowerShell 5.1, which has no -Authentication, and any edition that builds the header itself
Invoke-RestMethod -Uri $uri -Headers @{ Authorization = "Bearer $accessToken"; 'X-CSRF-Header' = '-' }
```

Credentials: **Where the credentials come from.** This is the explicit form: `-Authentication Basic` sends Basic pre-emptively even to a server that challenges Bearer (*checked by NSerio locally*, 7.6.6), so on RelativityOne the credential is sent where RelativityOne documents bearer authentication only. If the destination is not RelativityOne, the migration is that service's own. If it is, the token comes from the token-acquisition part that follows.

**Where the token comes from.** Create an OAuth2 Client with Flow Grant Type set to Client Credentials (part 3 lists its settings), then send a form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=client_credentials`, `client_id`, `client_secret` and a `scope`. The token endpoint is **documented** for this grant. The token is then sent as `Authorization: Bearer <token>`, with `X-CSRF-Header: -` on REST and Kepler calls — the header is documented as required, and a Kepler call without it returns 404.

```powershell
# Windows PowerShell 5.1 and PowerShell 7.x. $clientId and $clientSecret come from the process
# environment or a secret store (Secret Management (Part B of the Migration Documentation)), never from this script.
$tokenBody = @{
	grant_type    = 'client_credentials'
	client_id     = $clientId
	client_secret = $clientSecret
	scope         = $scope
}
$tokenResponse = Invoke-RestMethod -Method Post -Uri "$instanceUrl/Relativity/Identity/connect/token" -Body $tokenBody -ContentType 'application/x-www-form-urlencoded'
$accessToken = $tokenResponse.access_token
$headerTable = @{ Authorization = "Bearer $accessToken"; 'X-CSRF-Header' = '-' }
```

```powershell
# Windows PowerShell 5.1 only, before the first request, if the host does not already negotiate TLS 1.2
# (inferred from the .NET Framework default; the local mock-server run is plain http, so it does not exercise TLS).
[Net.ServicePointManager]::SecurityProtocol = [Net.ServicePointManager]::SecurityProtocol -bor [Net.SecurityProtocolType]::Tls12
```

```powershell
# PowerShell 7.x alternative to the header: the cmdlet sends the bearer token itself
# (checked by NSerio locally, 7.6.6: Authorization: Bearer is sent pre-emptively).
$secureToken = ConvertTo-SecureString $accessToken -AsPlainText -Force
Invoke-RestMethod -Uri $uri -Authentication Bearer -Token $secureToken -Headers @{ 'X-CSRF-Header' = '-' }
```

**The scope depends on what the integration needs, and no single scope is required.** Relativity's documentation shows two values for this grant: `SystemUserInfo` on the REST authentication page, the Usage Reports page and in the official Transfer SDK sample, and `UserInfoAccess` in the Kepler .NET samples. No page says which is correct, whether both work, or what either grants, and the OAuth2 Client's `Scopes` property is documented as one that cannot be set. Use the value the API you call documents, and confirm it.

**Cache the token, and re-acquire it on expiry — do not refresh.** Keep it in a script-scoped variable for its lifetime (part 3 recommends 60 minutes, Relativity's own wording), not on disk, and request a new one with the client secret when it expires or a call is rejected for authentication. Client Credentials offers no refresh tokens: **verified by NSerio against a live instance; not documented by Relativity** — the Allow Refresh Tokens control does not appear for this grant in the RelativityOne user interface.

**The client secret.** It is displayed once, at creation or reset. Store it as Secret Management (Part B of the Migration Documentation) describes; never in the script, never in tracked configuration. A credential saved with `Export-Clixml` is bound to the user and machine that saved it on Windows and is not protected on Linux or macOS (documented by Microsoft). A reset invalidates the previous secret immediately, so plan the cut-over before resetting it.

**Before deploying, confirm the calling identity.** Every call now runs as the OAuth2 Client's **Context User**, not as the user in the replaced credential. If that user has different workspace permissions or item-level security, the script returns fewer rows or `Unauthorized` with no change to its logic, and Relativity's audit attributes every call to that account. Confirm the Context User's permissions match those of the previous identity.

**When the integration acts for a signed-in user.** Client Credentials is one service identity by construction. That is the Authorization Code case, and moving to it means a new OAuth2 Client, because the flow is fixed when a client is created. The finding's flow note says when to make that choice.

Verification: **Verification note — three things.** **`-Token` takes a `SecureString`** (documented by Microsoft), so the access token is converted before the call. **A script that sent Basic over `http://` needed `-AllowUnencryptedAuthentication`** (7.x refuses the credential otherwise, *checked by NSerio locally*): remove it with the Basic credential, and use HTTPS for the bearer token. **For `Start-BitsTransfer`**, whose `-Authentication` offers no Bearer value, the header route through `-CustomHeaders` is the expected one (*inferred*; not tested).

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
<!-- generated:end PS-003 client-credentials -->

#### Authorization Code

<!-- generated:begin PS-003 authorization-code -->
**Confirming the flow.** An interactive-user signal in a file assigns Authorization Code, with this note: "An interactive-user signal in this file points to Authorization Code. Confirm the flow: use Client Credentials instead if the integration runs with credentials fixed for the integration and no signed-in user." Your user profile can also make Authorization Code the default; see Changing the default in [Confirming the flow](#confirming-the-flow). See [Confirming the flow](#confirming-the-flow) and the "Which flow" paragraph of PS-003 in [Part A](part-a-powershell.md#ps-003).

Source form: **Editions:** PowerShell 7.x for `Invoke-RestMethod` and `Invoke-WebRequest`, where `-Authentication` exists; Windows PowerShell 5.1 and 7.x on Windows for `Start-BitsTransfer`. The 5.1 target for the web cmdlets is the header form (5.1 has no `-Authentication`).

```powershell
Invoke-RestMethod -Uri $uri -Credential $credential -Authentication Basic
```

Under token authentication: **First, the destination:** if this is not RelativityOne — including a Relativity Server instance, whose own documentation governs — this finding is informational and the rest of this template does not apply.

```powershell
# PowerShell 7.x. $secureToken is the access token as a SecureString, as in the token-acquisition part.
Invoke-RestMethod -Uri $uri -Authentication Bearer -Token $secureToken -Headers @{ 'X-CSRF-Header' = '-' }
```

```powershell
# Windows PowerShell 5.1, which has no -Authentication, and any edition that builds the header itself
Invoke-RestMethod -Uri $uri -Headers @{ Authorization = "Bearer $accessToken"; 'X-CSRF-Header' = '-' }
```

Credentials: **Where the credentials come from.** This is the explicit form: `-Authentication Basic` sends Basic pre-emptively even to a server that challenges Bearer (*checked by NSerio locally*, 7.6.6), so on RelativityOne the credential is sent where RelativityOne documents bearer authentication only. If the destination is not RelativityOne, the migration is that service's own. If it is, the token comes from the token-acquisition part that follows.

**Common to both variants**

**Sign in.** Replace the code that collected the user's credentials with a redirect: send the user's browser to the authorize endpoint `<host>/Relativity/Identity/connect/authorize`, which leads to the RelativityOne login page. The authorize endpoint is **verified by NSerio against a live instance; not documented by Relativity**. The request carries the standard authorization-code parameters — `client_id`, `response_type=code`, the registered `redirect_uri`, a `scope`, and a `state` value the script checks on return; these are standard OAuth, and Relativity documents none of them.

**Receive the code.** The browser returns to the Redirect URL registered on the OAuth2 Client, with the authorization code and the `state` value. Check the `state` before using the code.

**Exchange the code.** Send a form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=authorization_code`, the `code`, the same `redirect_uri`, the `client_id` and the `client_secret`, plus the `code_verifier` when PKCE is used. The exchange is **verified by NSerio against a live instance; not documented by Relativity**.

**Use the token.** Send `Authorization: Bearer <access token>` — the change at the use site in part 1 — with `X-CSRF-Header: -` on REST and Kepler calls.

**On expiry.** With Allow Refresh Tokens enabled on the OAuth2 Client and PKCE used, the script holds a refresh token and, when the access token expires, sends a `grant_type=refresh_token` request with it in place of the code exchange — *inferred* from the standard OAuth shape, not tested. Without a refresh token the user signs in again. Relativity does not document a refresh token's lifetime, whether it rotates, or how it is revoked. A refresh token is a secret: store it as Secret Management (Part B of the Migration Documentation) describes, not in a plain file.

**PKCE.** PKCE is required by Relativity's own documentation to obtain a refresh token, and not otherwise: a Code request that does not ask for a refresh token works without it, **verified by NSerio against a live instance; not documented by Relativity**. NSerio recommends PKCE with the `S256` method (NSerio recommendation), and does not claim that Relativity accepts any particular method, which it does not document.

**Scope.** No scope set is prescribed for Code. The set NSerio used works, **verified by NSerio against a live instance; not documented by Relativity**, verified as working and not as minimal.

**The script stops collecting the password.** After this change the script never receives, stores or forwards the user's user name and password: remove the prompt (`Get-Credential`, `Read-Host`) and any storage of the password.

**Single sign-on.** Sign-in happens on RelativityOne's own login page, so the instance's single sign-on and two-factor rules apply unchanged — *inferred* from where the sign-in happens, and not documented for clients that customers create.

**The client secret is required.** Code does not work without a client secret: tested and rejected, **verified by NSerio against a live instance; not documented by Relativity**, which confirms Relativity's own statement that the grant requires one. It is displayed once and is stored as Secret Management (Part B of the Migration Documentation) describes.

**Server-side variant.** For a script that runs on a server and receives the callback on a route of an application: one exact HTTPS Redirect URL per environment, no wildcards, registered by the Relativity administrator (NSerio recommendation; Relativity's matching rules are not documented); the secret and the user's tokens stay on the server. PowerShell is rarely the host of such a route; the server-side variant of the other languages applies unchanged.

**Desktop variant.** For a script a person runs on their own machine:

- **The system browser.** Open the authorize URL in the default browser, not in a web view the script hosts, so the script never sees the password (NSerio recommendation).
- **A loopback redirect on a fixed port.** Listen on the loopback address only, on the registered port, for one request. The Redirect URL is `http://127.0.0.1:<port>/<path>`: a plain-HTTP loopback URL is accepted, **verified by NSerio against a live instance; not documented by Relativity**. The Relativity administrator registers that exact URL, so the port is fixed and not chosen at runtime. **`HttpListener` accepts only a prefix that ends in `/`**, so the script appends one to the prefix it listens on and leaves the registered Redirect URL as registered: a request for `<path>?code=…` is received by the prefix `<path>/` (*checked by NSerio locally*, Windows PowerShell 5.1 and PowerShell 7.6.6: `Prefixes.Add` rejects a prefix without the slash, and the prefix with it answered both `/<path>?code=…` and `/<path>/?code=…`). `HttpListener` starts on the loopback address from a non-elevated process on both editions (*checked by NSerio locally*; a true standard account is not yet tested).
- **The secret is extractable.** Code requires a client secret, so a distributed script has to embed one, and **a script is text: anyone who can read the file has the secret.** It protects nothing against the person who runs the script.
- **Compensating guidance** (NSerio recommendation, subject to your own security policy): set Trusted IPs where outbound addresses are known and stable — they restrict where a token can be *requested* — use the recommended short Access Token Lifetime of 60 minutes, enable refresh tokens only when needed, and create one OAuth2 Client per distribution.
- **A known issue.** Relativity documents no recommendation for a distributed desktop application; this design is what NSerio verified works.

```powershell
# Windows PowerShell 5.1 and PowerShell 7.x. Executed against a local mock server on both, not against a live instance.
# $redirectUri is the exact URL registered on the OAuth2 Client, with a fixed port.
$state = [guid]::NewGuid().ToString('N')
$generator = [System.Security.Cryptography.RandomNumberGenerator]::Create()
$verifierBytes = New-Object byte[] 32
$generator.GetBytes($verifierBytes)
$verifier = [Convert]::ToBase64String($verifierBytes).TrimEnd('=').Replace('+', '-').Replace('/', '_')
$hasher = [System.Security.Cryptography.SHA256]::Create()
$challengeBytes = $hasher.ComputeHash([Text.Encoding]::ASCII.GetBytes($verifier))
$challenge = [Convert]::ToBase64String($challengeBytes).TrimEnd('=').Replace('+', '-').Replace('/', '_')

$listener = New-Object System.Net.HttpListener
$listener.Prefixes.Add($redirectUri.TrimEnd('/') + '/')   # a prefix must end in '/'; the registered URL is unchanged
$listener.Start()
$authorizeUrl = "$instanceUrl/Relativity/Identity/connect/authorize?client_id=$clientId&response_type=code" +
	"&redirect_uri=$([uri]::EscapeDataString($redirectUri))&scope=$([uri]::EscapeDataString($scope))" +
	"&state=$state&code_challenge=$challenge&code_challenge_method=S256"
Start-Process $authorizeUrl   # on Linux or macOS under 7.x use the platform opener instead (inferred)
$context = $listener.GetContext()
$query = $context.Request.QueryString
$page = [Text.Encoding]::UTF8.GetBytes('You can close this window.')
$context.Response.Close($page, $true)   # sends the page and waits until it is written, before the listener stops
$listener.Stop()
if ($query['state'] -ne $state) { throw 'The state value does not match.' }
$tokenResponse = Invoke-RestMethod -Method Post -Uri "$instanceUrl/Relativity/Identity/connect/token" -Body @{
	grant_type    = 'authorization_code'
	code          = $query['code']
	redirect_uri  = $redirectUri
	client_id     = $clientId
	client_secret = $clientSecret
	code_verifier = $verifier
}
$accessToken = $tokenResponse.access_token
```

**Why this form.** The two short .NET helpers for the hash and for random bytes exist on 7.x only, so the script creates the objects and calls their instance methods, which run on both editions (documented .NET API).

Verification: **Verification note — three things.** **`-Token` takes a `SecureString`** (documented by Microsoft), so the access token is converted before the call. **A script that sent Basic over `http://` needed `-AllowUnencryptedAuthentication`** (7.x refuses the credential otherwise, *checked by NSerio locally*): remove it with the Basic credential, and use HTTPS for the bearer token. **For `Start-BitsTransfer`**, whose `-Authentication` offers no Bearer value, the header route through `-CustomHeaders` is the expected one (*inferred*; not tested).

OAuth2 Client: Create or confirm this OAuth2 Client in RelativityOne (NSerio's observation: the menu path is Home > Authentication > OAuth2 Client Manager, as Relativity's REST API authentication page names it). This tool never connects to RelativityOne and never creates or changes the OAuth2 Client; the settings below are guidance.
- Name: a descriptive name identifying the application it serves.
- Flow Grant Type: Code
- Redirect URLs: Relativity documentation: each URL includes the http or https protocol prefix. The exact callback URL is registered by the Relativity administrator. Server-side web applications, NSerio's recommendation: one exact HTTPS URL per environment, with no wildcards. Desktop and CLI applications: a plain-HTTP loopback URL, `http://127.0.0.1:<port>/<path>`, on a fixed port because the URL is registered in advance and the port cannot be chosen at runtime (verified by NSerio against a live instance; not documented by Relativity).
- Access Token Lifetime: 60 minutes, stated in minutes because the field is in minutes. Relativity documentation: tokens for this flow must have a short lifetime, and the recommended value matches the Identity Server default.
- Allow refresh tokens: NSerio's recommendation: leave it off unless the application must keep a user session active beyond the access token lifetime without asking the user to sign in again. If it is on, the refresh tokens are stored as secrets, per Secret Management (Part B of the Migration Documentation).
- Enabled: on.
<!-- generated:end PS-003 authorization-code -->

### PS-004 · AuthorizationHeaderHashtableLiteral

<a id="ps-004-migration"></a>

#### Client Credentials

<!-- generated:begin PS-004 client-credentials -->
**Confirming the flow.** When no signal in a file decides the flow, the tool assigns Client Credentials, the default, with this note: "Who supplies the credential at run time? If a person types it or signs in, the integration acts for that person: use Authorization Code. If it comes from configuration, settings, an environment variable or a secret store: use Client Credentials. No signal in this file showed which applies, so Client Credentials, the default, is recommended. Use Authorization Code instead if the integration acts on behalf of a signed-in user, or if your RelativityOne instance already has a Code-flow OAuth2 Client for this integration." A fixed-credential signal assigns it with this note: "A fixed-credential signal in this file points to Client Credentials, but how often that kind of signal is right has not been measured. Use Authorization Code instead if the integration acts on behalf of a signed-in user, or if your RelativityOne instance already has a Code-flow OAuth2 Client for this integration." See [Confirming the flow](#confirming-the-flow) and the "Which flow" paragraph of PS-004 in [Part A](part-a-powershell.md#ps-004).

Source form: **Editions:** Windows PowerShell 5.1 and PowerShell 7.x.

```powershell
Invoke-RestMethod -Uri $uri -Headers @{ Authorization = "Basic $encoded" }
```

Under token authentication: **First, the destination:** if this is not RelativityOne — including a Relativity Server instance, whose own documentation governs — this finding is informational and the rest of this template does not apply.

```powershell
Invoke-RestMethod -Uri $uri -Headers @{ Authorization = "Bearer $accessToken"; 'X-CSRF-Header' = '-' }
```

Credentials: **Where the credentials come from.** A hand-built header establishes nothing about the destination or about where the Base64 value came from. If the destination is RelativityOne, delete the encoding step that produced `$encoded`, and obtain the token as the token-acquisition part describes.

**Where the token comes from.** Create an OAuth2 Client with Flow Grant Type set to Client Credentials (part 3 lists its settings), then send a form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=client_credentials`, `client_id`, `client_secret` and a `scope`. The token endpoint is **documented** for this grant. The token is then sent as `Authorization: Bearer <token>`, with `X-CSRF-Header: -` on REST and Kepler calls — the header is documented as required, and a Kepler call without it returns 404.

```powershell
# Windows PowerShell 5.1 and PowerShell 7.x. $clientId and $clientSecret come from the process
# environment or a secret store (Secret Management (Part B of the Migration Documentation)), never from this script.
$tokenBody = @{
	grant_type    = 'client_credentials'
	client_id     = $clientId
	client_secret = $clientSecret
	scope         = $scope
}
$tokenResponse = Invoke-RestMethod -Method Post -Uri "$instanceUrl/Relativity/Identity/connect/token" -Body $tokenBody -ContentType 'application/x-www-form-urlencoded'
$accessToken = $tokenResponse.access_token
$headerTable = @{ Authorization = "Bearer $accessToken"; 'X-CSRF-Header' = '-' }
```

```powershell
# Windows PowerShell 5.1 only, before the first request, if the host does not already negotiate TLS 1.2
# (inferred from the .NET Framework default; the local mock-server run is plain http, so it does not exercise TLS).
[Net.ServicePointManager]::SecurityProtocol = [Net.ServicePointManager]::SecurityProtocol -bor [Net.SecurityProtocolType]::Tls12
```

```powershell
# PowerShell 7.x alternative to the header: the cmdlet sends the bearer token itself
# (checked by NSerio locally, 7.6.6: Authorization: Bearer is sent pre-emptively).
$secureToken = ConvertTo-SecureString $accessToken -AsPlainText -Force
Invoke-RestMethod -Uri $uri -Authentication Bearer -Token $secureToken -Headers @{ 'X-CSRF-Header' = '-' }
```

**The scope depends on what the integration needs, and no single scope is required.** Relativity's documentation shows two values for this grant: `SystemUserInfo` on the REST authentication page, the Usage Reports page and in the official Transfer SDK sample, and `UserInfoAccess` in the Kepler .NET samples. No page says which is correct, whether both work, or what either grants, and the OAuth2 Client's `Scopes` property is documented as one that cannot be set. Use the value the API you call documents, and confirm it.

**Cache the token, and re-acquire it on expiry — do not refresh.** Keep it in a script-scoped variable for its lifetime (part 3 recommends 60 minutes, Relativity's own wording), not on disk, and request a new one with the client secret when it expires or a call is rejected for authentication. Client Credentials offers no refresh tokens: **verified by NSerio against a live instance; not documented by Relativity** — the Allow Refresh Tokens control does not appear for this grant in the RelativityOne user interface.

**The client secret.** It is displayed once, at creation or reset. Store it as Secret Management (Part B of the Migration Documentation) describes; never in the script, never in tracked configuration. A credential saved with `Export-Clixml` is bound to the user and machine that saved it on Windows and is not protected on Linux or macOS (documented by Microsoft). A reset invalidates the previous secret immediately, so plan the cut-over before resetting it.

**Before deploying, confirm the calling identity.** Every call now runs as the OAuth2 Client's **Context User**, not as the user in the replaced credential. If that user has different workspace permissions or item-level security, the script returns fewer rows or `Unauthorized` with no change to its logic, and Relativity's audit attributes every call to that account. Confirm the Context User's permissions match those of the previous identity.

**When the integration acts for a signed-in user.** Client Credentials is one service identity by construction. That is the Authorization Code case, and moving to it means a new OAuth2 Client, because the flow is fixed when a client is created. The finding's flow note says when to make that choice.

Verification: **Verification note — two things.** **`X-CSRF-Header` is required on REST and Kepler calls** and a Kepler call without it returns 404: a Basic header that worked without it will fail with it missing. **A token has a finite lifetime**: rebuild the header with a current token.

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
<!-- generated:end PS-004 client-credentials -->

#### Authorization Code

<!-- generated:begin PS-004 authorization-code -->
**Confirming the flow.** An interactive-user signal in a file assigns Authorization Code, with this note: "An interactive-user signal in this file points to Authorization Code. Confirm the flow: use Client Credentials instead if the integration runs with credentials fixed for the integration and no signed-in user." Your user profile can also make Authorization Code the default; see Changing the default in [Confirming the flow](#confirming-the-flow). See [Confirming the flow](#confirming-the-flow) and the "Which flow" paragraph of PS-004 in [Part A](part-a-powershell.md#ps-004).

Source form: **Editions:** Windows PowerShell 5.1 and PowerShell 7.x.

```powershell
Invoke-RestMethod -Uri $uri -Headers @{ Authorization = "Basic $encoded" }
```

Under token authentication: **First, the destination:** if this is not RelativityOne — including a Relativity Server instance, whose own documentation governs — this finding is informational and the rest of this template does not apply.

```powershell
Invoke-RestMethod -Uri $uri -Headers @{ Authorization = "Bearer $accessToken"; 'X-CSRF-Header' = '-' }
```

Credentials: **Where the credentials come from.** A hand-built header establishes nothing about the destination or about where the Base64 value came from. If the destination is RelativityOne, delete the encoding step that produced `$encoded`, and obtain the token as the token-acquisition part describes.

**Common to both variants**

**Sign in.** Replace the code that collected the user's credentials with a redirect: send the user's browser to the authorize endpoint `<host>/Relativity/Identity/connect/authorize`, which leads to the RelativityOne login page. The authorize endpoint is **verified by NSerio against a live instance; not documented by Relativity**. The request carries the standard authorization-code parameters — `client_id`, `response_type=code`, the registered `redirect_uri`, a `scope`, and a `state` value the script checks on return; these are standard OAuth, and Relativity documents none of them.

**Receive the code.** The browser returns to the Redirect URL registered on the OAuth2 Client, with the authorization code and the `state` value. Check the `state` before using the code.

**Exchange the code.** Send a form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=authorization_code`, the `code`, the same `redirect_uri`, the `client_id` and the `client_secret`, plus the `code_verifier` when PKCE is used. The exchange is **verified by NSerio against a live instance; not documented by Relativity**.

**Use the token.** Send `Authorization: Bearer <access token>` — the change at the use site in part 1 — with `X-CSRF-Header: -` on REST and Kepler calls.

**On expiry.** With Allow Refresh Tokens enabled on the OAuth2 Client and PKCE used, the script holds a refresh token and, when the access token expires, sends a `grant_type=refresh_token` request with it in place of the code exchange — *inferred* from the standard OAuth shape, not tested. Without a refresh token the user signs in again. Relativity does not document a refresh token's lifetime, whether it rotates, or how it is revoked. A refresh token is a secret: store it as Secret Management (Part B of the Migration Documentation) describes, not in a plain file.

**PKCE.** PKCE is required by Relativity's own documentation to obtain a refresh token, and not otherwise: a Code request that does not ask for a refresh token works without it, **verified by NSerio against a live instance; not documented by Relativity**. NSerio recommends PKCE with the `S256` method (NSerio recommendation), and does not claim that Relativity accepts any particular method, which it does not document.

**Scope.** No scope set is prescribed for Code. The set NSerio used works, **verified by NSerio against a live instance; not documented by Relativity**, verified as working and not as minimal.

**The script stops collecting the password.** After this change the script never receives, stores or forwards the user's user name and password: remove the prompt (`Get-Credential`, `Read-Host`) and any storage of the password.

**Single sign-on.** Sign-in happens on RelativityOne's own login page, so the instance's single sign-on and two-factor rules apply unchanged — *inferred* from where the sign-in happens, and not documented for clients that customers create.

**The client secret is required.** Code does not work without a client secret: tested and rejected, **verified by NSerio against a live instance; not documented by Relativity**, which confirms Relativity's own statement that the grant requires one. It is displayed once and is stored as Secret Management (Part B of the Migration Documentation) describes.

**Server-side variant.** For a script that runs on a server and receives the callback on a route of an application: one exact HTTPS Redirect URL per environment, no wildcards, registered by the Relativity administrator (NSerio recommendation; Relativity's matching rules are not documented); the secret and the user's tokens stay on the server. PowerShell is rarely the host of such a route; the server-side variant of the other languages applies unchanged.

**Desktop variant.** For a script a person runs on their own machine:

- **The system browser.** Open the authorize URL in the default browser, not in a web view the script hosts, so the script never sees the password (NSerio recommendation).
- **A loopback redirect on a fixed port.** Listen on the loopback address only, on the registered port, for one request. The Redirect URL is `http://127.0.0.1:<port>/<path>`: a plain-HTTP loopback URL is accepted, **verified by NSerio against a live instance; not documented by Relativity**. The Relativity administrator registers that exact URL, so the port is fixed and not chosen at runtime. **`HttpListener` accepts only a prefix that ends in `/`**, so the script appends one to the prefix it listens on and leaves the registered Redirect URL as registered: a request for `<path>?code=…` is received by the prefix `<path>/` (*checked by NSerio locally*, Windows PowerShell 5.1 and PowerShell 7.6.6: `Prefixes.Add` rejects a prefix without the slash, and the prefix with it answered both `/<path>?code=…` and `/<path>/?code=…`). `HttpListener` starts on the loopback address from a non-elevated process on both editions (*checked by NSerio locally*; a true standard account is not yet tested).
- **The secret is extractable.** Code requires a client secret, so a distributed script has to embed one, and **a script is text: anyone who can read the file has the secret.** It protects nothing against the person who runs the script.
- **Compensating guidance** (NSerio recommendation, subject to your own security policy): set Trusted IPs where outbound addresses are known and stable — they restrict where a token can be *requested* — use the recommended short Access Token Lifetime of 60 minutes, enable refresh tokens only when needed, and create one OAuth2 Client per distribution.
- **A known issue.** Relativity documents no recommendation for a distributed desktop application; this design is what NSerio verified works.

```powershell
# Windows PowerShell 5.1 and PowerShell 7.x. Executed against a local mock server on both, not against a live instance.
# $redirectUri is the exact URL registered on the OAuth2 Client, with a fixed port.
$state = [guid]::NewGuid().ToString('N')
$generator = [System.Security.Cryptography.RandomNumberGenerator]::Create()
$verifierBytes = New-Object byte[] 32
$generator.GetBytes($verifierBytes)
$verifier = [Convert]::ToBase64String($verifierBytes).TrimEnd('=').Replace('+', '-').Replace('/', '_')
$hasher = [System.Security.Cryptography.SHA256]::Create()
$challengeBytes = $hasher.ComputeHash([Text.Encoding]::ASCII.GetBytes($verifier))
$challenge = [Convert]::ToBase64String($challengeBytes).TrimEnd('=').Replace('+', '-').Replace('/', '_')

$listener = New-Object System.Net.HttpListener
$listener.Prefixes.Add($redirectUri.TrimEnd('/') + '/')   # a prefix must end in '/'; the registered URL is unchanged
$listener.Start()
$authorizeUrl = "$instanceUrl/Relativity/Identity/connect/authorize?client_id=$clientId&response_type=code" +
	"&redirect_uri=$([uri]::EscapeDataString($redirectUri))&scope=$([uri]::EscapeDataString($scope))" +
	"&state=$state&code_challenge=$challenge&code_challenge_method=S256"
Start-Process $authorizeUrl   # on Linux or macOS under 7.x use the platform opener instead (inferred)
$context = $listener.GetContext()
$query = $context.Request.QueryString
$page = [Text.Encoding]::UTF8.GetBytes('You can close this window.')
$context.Response.Close($page, $true)   # sends the page and waits until it is written, before the listener stops
$listener.Stop()
if ($query['state'] -ne $state) { throw 'The state value does not match.' }
$tokenResponse = Invoke-RestMethod -Method Post -Uri "$instanceUrl/Relativity/Identity/connect/token" -Body @{
	grant_type    = 'authorization_code'
	code          = $query['code']
	redirect_uri  = $redirectUri
	client_id     = $clientId
	client_secret = $clientSecret
	code_verifier = $verifier
}
$accessToken = $tokenResponse.access_token
```

**Why this form.** The two short .NET helpers for the hash and for random bytes exist on 7.x only, so the script creates the objects and calls their instance methods, which run on both editions (documented .NET API).

Verification: **Verification note — two things.** **`X-CSRF-Header` is required on REST and Kepler calls** and a Kepler call without it returns 404: a Basic header that worked without it will fail with it missing. **A token has a finite lifetime**: rebuild the header with a current token.

OAuth2 Client: Create or confirm this OAuth2 Client in RelativityOne (NSerio's observation: the menu path is Home > Authentication > OAuth2 Client Manager, as Relativity's REST API authentication page names it). This tool never connects to RelativityOne and never creates or changes the OAuth2 Client; the settings below are guidance.
- Name: a descriptive name identifying the application it serves.
- Flow Grant Type: Code
- Redirect URLs: Relativity documentation: each URL includes the http or https protocol prefix. The exact callback URL is registered by the Relativity administrator. Server-side web applications, NSerio's recommendation: one exact HTTPS URL per environment, with no wildcards. Desktop and CLI applications: a plain-HTTP loopback URL, `http://127.0.0.1:<port>/<path>`, on a fixed port because the URL is registered in advance and the port cannot be chosen at runtime (verified by NSerio against a live instance; not documented by Relativity).
- Access Token Lifetime: 60 minutes, stated in minutes because the field is in minutes. Relativity documentation: tokens for this flow must have a short lifetime, and the recommended value matches the Identity Server default.
- Allow refresh tokens: NSerio's recommendation: leave it off unless the application must keep a user session active beyond the access token lifetime without asking the user to sign in again. If it is on, the refresh tokens are stored as secrets, per Secret Management (Part B of the Migration Documentation).
- Enabled: on.
<!-- generated:end PS-004 authorization-code -->

### PS-005 · AuthorizationHeaderStringKeyWrite

<a id="ps-005-migration"></a>

#### Client Credentials

<!-- generated:begin PS-005 client-credentials -->
**Confirming the flow.** When no signal in a file decides the flow, the tool assigns Client Credentials, the default, with this note: "Who supplies the credential at run time? If a person types it or signs in, the integration acts for that person: use Authorization Code. If it comes from configuration, settings, an environment variable or a secret store: use Client Credentials. No signal in this file showed which applies, so Client Credentials, the default, is recommended. Use Authorization Code instead if the integration acts on behalf of a signed-in user, or if your RelativityOne instance already has a Code-flow OAuth2 Client for this integration." A fixed-credential signal assigns it with this note: "A fixed-credential signal in this file points to Client Credentials, but how often that kind of signal is right has not been measured. Use Authorization Code instead if the integration acts on behalf of a signed-in user, or if your RelativityOne instance already has a Code-flow OAuth2 Client for this integration." See [Confirming the flow](#confirming-the-flow) and the "Which flow" paragraph of PS-005 in [Part A](part-a-powershell.md#ps-005).

Source form: **Editions:** Windows PowerShell 5.1 and PowerShell 7.x.

```powershell
$headerTable.Add('Authorization', 'Basic ' + $encoded)
```

Under token authentication: **First, the destination:** if this is not RelativityOne — including a Relativity Server instance, whose own documentation governs — this finding is informational and the rest of this template does not apply.

```powershell
$headerTable.Add('Authorization', "Bearer $accessToken")
$headerTable['X-CSRF-Header'] = '-'
```

Credentials: **Where the credentials come from.** As for a hand-built header table: the write establishes nothing about the destination.

**Where the token comes from.** Create an OAuth2 Client with Flow Grant Type set to Client Credentials (part 3 lists its settings), then send a form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=client_credentials`, `client_id`, `client_secret` and a `scope`. The token endpoint is **documented** for this grant. The token is then sent as `Authorization: Bearer <token>`, with `X-CSRF-Header: -` on REST and Kepler calls — the header is documented as required, and a Kepler call without it returns 404.

```powershell
# Windows PowerShell 5.1 and PowerShell 7.x. $clientId and $clientSecret come from the process
# environment or a secret store (Secret Management (Part B of the Migration Documentation)), never from this script.
$tokenBody = @{
	grant_type    = 'client_credentials'
	client_id     = $clientId
	client_secret = $clientSecret
	scope         = $scope
}
$tokenResponse = Invoke-RestMethod -Method Post -Uri "$instanceUrl/Relativity/Identity/connect/token" -Body $tokenBody -ContentType 'application/x-www-form-urlencoded'
$accessToken = $tokenResponse.access_token
$headerTable = @{ Authorization = "Bearer $accessToken"; 'X-CSRF-Header' = '-' }
```

```powershell
# Windows PowerShell 5.1 only, before the first request, if the host does not already negotiate TLS 1.2
# (inferred from the .NET Framework default; the local mock-server run is plain http, so it does not exercise TLS).
[Net.ServicePointManager]::SecurityProtocol = [Net.ServicePointManager]::SecurityProtocol -bor [Net.SecurityProtocolType]::Tls12
```

```powershell
# PowerShell 7.x alternative to the header: the cmdlet sends the bearer token itself
# (checked by NSerio locally, 7.6.6: Authorization: Bearer is sent pre-emptively).
$secureToken = ConvertTo-SecureString $accessToken -AsPlainText -Force
Invoke-RestMethod -Uri $uri -Authentication Bearer -Token $secureToken -Headers @{ 'X-CSRF-Header' = '-' }
```

**The scope depends on what the integration needs, and no single scope is required.** Relativity's documentation shows two values for this grant: `SystemUserInfo` on the REST authentication page, the Usage Reports page and in the official Transfer SDK sample, and `UserInfoAccess` in the Kepler .NET samples. No page says which is correct, whether both work, or what either grants, and the OAuth2 Client's `Scopes` property is documented as one that cannot be set. Use the value the API you call documents, and confirm it.

**Cache the token, and re-acquire it on expiry — do not refresh.** Keep it in a script-scoped variable for its lifetime (part 3 recommends 60 minutes, Relativity's own wording), not on disk, and request a new one with the client secret when it expires or a call is rejected for authentication. Client Credentials offers no refresh tokens: **verified by NSerio against a live instance; not documented by Relativity** — the Allow Refresh Tokens control does not appear for this grant in the RelativityOne user interface.

**The client secret.** It is displayed once, at creation or reset. Store it as Secret Management (Part B of the Migration Documentation) describes; never in the script, never in tracked configuration. A credential saved with `Export-Clixml` is bound to the user and machine that saved it on Windows and is not protected on Linux or macOS (documented by Microsoft). A reset invalidates the previous secret immediately, so plan the cut-over before resetting it.

**Before deploying, confirm the calling identity.** Every call now runs as the OAuth2 Client's **Context User**, not as the user in the replaced credential. If that user has different workspace permissions or item-level security, the script returns fewer rows or `Unauthorized` with no change to its logic, and Relativity's audit attributes every call to that account. Confirm the Context User's permissions match those of the previous identity.

**When the integration acts for a signed-in user.** Client Credentials is one service identity by construction. That is the Authorization Code case, and moving to it means a new OAuth2 Client, because the flow is fixed when a client is created. The finding's flow note says when to make that choice.

Verification: **Verification note.** The same two points: `X-CSRF-Header` is required, and a token expires. If the headers collection is built once and reused for the life of the script, rebuild it with the new token.

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
<!-- generated:end PS-005 client-credentials -->

#### Authorization Code

<!-- generated:begin PS-005 authorization-code -->
**Confirming the flow.** An interactive-user signal in a file assigns Authorization Code, with this note: "An interactive-user signal in this file points to Authorization Code. Confirm the flow: use Client Credentials instead if the integration runs with credentials fixed for the integration and no signed-in user." Your user profile can also make Authorization Code the default; see Changing the default in [Confirming the flow](#confirming-the-flow). See [Confirming the flow](#confirming-the-flow) and the "Which flow" paragraph of PS-005 in [Part A](part-a-powershell.md#ps-005).

Source form: **Editions:** Windows PowerShell 5.1 and PowerShell 7.x.

```powershell
$headerTable.Add('Authorization', 'Basic ' + $encoded)
```

Under token authentication: **First, the destination:** if this is not RelativityOne — including a Relativity Server instance, whose own documentation governs — this finding is informational and the rest of this template does not apply.

```powershell
$headerTable.Add('Authorization', "Bearer $accessToken")
$headerTable['X-CSRF-Header'] = '-'
```

Credentials: **Where the credentials come from.** As for a hand-built header table: the write establishes nothing about the destination.

**Common to both variants**

**Sign in.** Replace the code that collected the user's credentials with a redirect: send the user's browser to the authorize endpoint `<host>/Relativity/Identity/connect/authorize`, which leads to the RelativityOne login page. The authorize endpoint is **verified by NSerio against a live instance; not documented by Relativity**. The request carries the standard authorization-code parameters — `client_id`, `response_type=code`, the registered `redirect_uri`, a `scope`, and a `state` value the script checks on return; these are standard OAuth, and Relativity documents none of them.

**Receive the code.** The browser returns to the Redirect URL registered on the OAuth2 Client, with the authorization code and the `state` value. Check the `state` before using the code.

**Exchange the code.** Send a form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=authorization_code`, the `code`, the same `redirect_uri`, the `client_id` and the `client_secret`, plus the `code_verifier` when PKCE is used. The exchange is **verified by NSerio against a live instance; not documented by Relativity**.

**Use the token.** Send `Authorization: Bearer <access token>` — the change at the use site in part 1 — with `X-CSRF-Header: -` on REST and Kepler calls.

**On expiry.** With Allow Refresh Tokens enabled on the OAuth2 Client and PKCE used, the script holds a refresh token and, when the access token expires, sends a `grant_type=refresh_token` request with it in place of the code exchange — *inferred* from the standard OAuth shape, not tested. Without a refresh token the user signs in again. Relativity does not document a refresh token's lifetime, whether it rotates, or how it is revoked. A refresh token is a secret: store it as Secret Management (Part B of the Migration Documentation) describes, not in a plain file.

**PKCE.** PKCE is required by Relativity's own documentation to obtain a refresh token, and not otherwise: a Code request that does not ask for a refresh token works without it, **verified by NSerio against a live instance; not documented by Relativity**. NSerio recommends PKCE with the `S256` method (NSerio recommendation), and does not claim that Relativity accepts any particular method, which it does not document.

**Scope.** No scope set is prescribed for Code. The set NSerio used works, **verified by NSerio against a live instance; not documented by Relativity**, verified as working and not as minimal.

**The script stops collecting the password.** After this change the script never receives, stores or forwards the user's user name and password: remove the prompt (`Get-Credential`, `Read-Host`) and any storage of the password.

**Single sign-on.** Sign-in happens on RelativityOne's own login page, so the instance's single sign-on and two-factor rules apply unchanged — *inferred* from where the sign-in happens, and not documented for clients that customers create.

**The client secret is required.** Code does not work without a client secret: tested and rejected, **verified by NSerio against a live instance; not documented by Relativity**, which confirms Relativity's own statement that the grant requires one. It is displayed once and is stored as Secret Management (Part B of the Migration Documentation) describes.

**Server-side variant.** For a script that runs on a server and receives the callback on a route of an application: one exact HTTPS Redirect URL per environment, no wildcards, registered by the Relativity administrator (NSerio recommendation; Relativity's matching rules are not documented); the secret and the user's tokens stay on the server. PowerShell is rarely the host of such a route; the server-side variant of the other languages applies unchanged.

**Desktop variant.** For a script a person runs on their own machine:

- **The system browser.** Open the authorize URL in the default browser, not in a web view the script hosts, so the script never sees the password (NSerio recommendation).
- **A loopback redirect on a fixed port.** Listen on the loopback address only, on the registered port, for one request. The Redirect URL is `http://127.0.0.1:<port>/<path>`: a plain-HTTP loopback URL is accepted, **verified by NSerio against a live instance; not documented by Relativity**. The Relativity administrator registers that exact URL, so the port is fixed and not chosen at runtime. **`HttpListener` accepts only a prefix that ends in `/`**, so the script appends one to the prefix it listens on and leaves the registered Redirect URL as registered: a request for `<path>?code=…` is received by the prefix `<path>/` (*checked by NSerio locally*, Windows PowerShell 5.1 and PowerShell 7.6.6: `Prefixes.Add` rejects a prefix without the slash, and the prefix with it answered both `/<path>?code=…` and `/<path>/?code=…`). `HttpListener` starts on the loopback address from a non-elevated process on both editions (*checked by NSerio locally*; a true standard account is not yet tested).
- **The secret is extractable.** Code requires a client secret, so a distributed script has to embed one, and **a script is text: anyone who can read the file has the secret.** It protects nothing against the person who runs the script.
- **Compensating guidance** (NSerio recommendation, subject to your own security policy): set Trusted IPs where outbound addresses are known and stable — they restrict where a token can be *requested* — use the recommended short Access Token Lifetime of 60 minutes, enable refresh tokens only when needed, and create one OAuth2 Client per distribution.
- **A known issue.** Relativity documents no recommendation for a distributed desktop application; this design is what NSerio verified works.

```powershell
# Windows PowerShell 5.1 and PowerShell 7.x. Executed against a local mock server on both, not against a live instance.
# $redirectUri is the exact URL registered on the OAuth2 Client, with a fixed port.
$state = [guid]::NewGuid().ToString('N')
$generator = [System.Security.Cryptography.RandomNumberGenerator]::Create()
$verifierBytes = New-Object byte[] 32
$generator.GetBytes($verifierBytes)
$verifier = [Convert]::ToBase64String($verifierBytes).TrimEnd('=').Replace('+', '-').Replace('/', '_')
$hasher = [System.Security.Cryptography.SHA256]::Create()
$challengeBytes = $hasher.ComputeHash([Text.Encoding]::ASCII.GetBytes($verifier))
$challenge = [Convert]::ToBase64String($challengeBytes).TrimEnd('=').Replace('+', '-').Replace('/', '_')

$listener = New-Object System.Net.HttpListener
$listener.Prefixes.Add($redirectUri.TrimEnd('/') + '/')   # a prefix must end in '/'; the registered URL is unchanged
$listener.Start()
$authorizeUrl = "$instanceUrl/Relativity/Identity/connect/authorize?client_id=$clientId&response_type=code" +
	"&redirect_uri=$([uri]::EscapeDataString($redirectUri))&scope=$([uri]::EscapeDataString($scope))" +
	"&state=$state&code_challenge=$challenge&code_challenge_method=S256"
Start-Process $authorizeUrl   # on Linux or macOS under 7.x use the platform opener instead (inferred)
$context = $listener.GetContext()
$query = $context.Request.QueryString
$page = [Text.Encoding]::UTF8.GetBytes('You can close this window.')
$context.Response.Close($page, $true)   # sends the page and waits until it is written, before the listener stops
$listener.Stop()
if ($query['state'] -ne $state) { throw 'The state value does not match.' }
$tokenResponse = Invoke-RestMethod -Method Post -Uri "$instanceUrl/Relativity/Identity/connect/token" -Body @{
	grant_type    = 'authorization_code'
	code          = $query['code']
	redirect_uri  = $redirectUri
	client_id     = $clientId
	client_secret = $clientSecret
	code_verifier = $verifier
}
$accessToken = $tokenResponse.access_token
```

**Why this form.** The two short .NET helpers for the hash and for random bytes exist on 7.x only, so the script creates the objects and calls their instance methods, which run on both editions (documented .NET API).

Verification: **Verification note.** The same two points: `X-CSRF-Header` is required, and a token expires. If the headers collection is built once and reused for the life of the script, rebuild it with the new token.

OAuth2 Client: Create or confirm this OAuth2 Client in RelativityOne (NSerio's observation: the menu path is Home > Authentication > OAuth2 Client Manager, as Relativity's REST API authentication page names it). This tool never connects to RelativityOne and never creates or changes the OAuth2 Client; the settings below are guidance.
- Name: a descriptive name identifying the application it serves.
- Flow Grant Type: Code
- Redirect URLs: Relativity documentation: each URL includes the http or https protocol prefix. The exact callback URL is registered by the Relativity administrator. Server-side web applications, NSerio's recommendation: one exact HTTPS URL per environment, with no wildcards. Desktop and CLI applications: a plain-HTTP loopback URL, `http://127.0.0.1:<port>/<path>`, on a fixed port because the URL is registered in advance and the port cannot be chosen at runtime (verified by NSerio against a live instance; not documented by Relativity).
- Access Token Lifetime: 60 minutes, stated in minutes because the field is in minutes. Relativity documentation: tokens for this flow must have a short lifetime, and the recommended value matches the Identity Server default.
- Allow refresh tokens: NSerio's recommendation: leave it off unless the application must keep a user session active beyond the access token lifetime without asking the user to sign in again. If it is on, the refresh tokens are stored as secrets, per Secret Management (Part B of the Migration Documentation).
- Enabled: on.
<!-- generated:end PS-005 authorization-code -->

### PS-006 · AuthorizationHeaderEnumWrite

<a id="ps-006-migration"></a>

#### Client Credentials

<!-- generated:begin PS-006 client-credentials -->
**Confirming the flow.** When no signal in a file decides the flow, the tool assigns Client Credentials, the default, with this note: "Who supplies the credential at run time? If a person types it or signs in, the integration acts for that person: use Authorization Code. If it comes from configuration, settings, an environment variable or a secret store: use Client Credentials. No signal in this file showed which applies, so Client Credentials, the default, is recommended. Use Authorization Code instead if the integration acts on behalf of a signed-in user, or if your RelativityOne instance already has a Code-flow OAuth2 Client for this integration." A fixed-credential signal assigns it with this note: "A fixed-credential signal in this file points to Client Credentials, but how often that kind of signal is right has not been measured. Use Authorization Code instead if the integration acts on behalf of a signed-in user, or if your RelativityOne instance already has a Code-flow OAuth2 Client for this integration." See [Confirming the flow](#confirming-the-flow) and the "Which flow" paragraph of PS-006 in [Part A](part-a-powershell.md#ps-006).

Source form: **Editions:** Windows PowerShell 5.1 and PowerShell 7.x.

```powershell
$request.Headers.Add([System.Net.HttpRequestHeader]::Authorization, "Basic $encoded")
```

Under token authentication: **First, the destination:** if this is not RelativityOne — including a Relativity Server instance, whose own documentation governs — this finding is informational and the rest of this template does not apply.

```powershell
$request.Headers.Add([System.Net.HttpRequestHeader]::Authorization, "Bearer $accessToken")
$request.Headers.Add('X-CSRF-Header', '-')
```

Credentials: **Where the credentials come from.** As for a hand-built header table: the write establishes nothing about the destination.

**Where the token comes from.** Create an OAuth2 Client with Flow Grant Type set to Client Credentials (part 3 lists its settings), then send a form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=client_credentials`, `client_id`, `client_secret` and a `scope`. The token endpoint is **documented** for this grant. The token is then sent as `Authorization: Bearer <token>`, with `X-CSRF-Header: -` on REST and Kepler calls — the header is documented as required, and a Kepler call without it returns 404.

```powershell
# Windows PowerShell 5.1 and PowerShell 7.x. $clientId and $clientSecret come from the process
# environment or a secret store (Secret Management (Part B of the Migration Documentation)), never from this script.
$tokenBody = @{
	grant_type    = 'client_credentials'
	client_id     = $clientId
	client_secret = $clientSecret
	scope         = $scope
}
$tokenResponse = Invoke-RestMethod -Method Post -Uri "$instanceUrl/Relativity/Identity/connect/token" -Body $tokenBody -ContentType 'application/x-www-form-urlencoded'
$accessToken = $tokenResponse.access_token
$headerTable = @{ Authorization = "Bearer $accessToken"; 'X-CSRF-Header' = '-' }
```

```powershell
# Windows PowerShell 5.1 only, before the first request, if the host does not already negotiate TLS 1.2
# (inferred from the .NET Framework default; the local mock-server run is plain http, so it does not exercise TLS).
[Net.ServicePointManager]::SecurityProtocol = [Net.ServicePointManager]::SecurityProtocol -bor [Net.SecurityProtocolType]::Tls12
```

```powershell
# PowerShell 7.x alternative to the header: the cmdlet sends the bearer token itself
# (checked by NSerio locally, 7.6.6: Authorization: Bearer is sent pre-emptively).
$secureToken = ConvertTo-SecureString $accessToken -AsPlainText -Force
Invoke-RestMethod -Uri $uri -Authentication Bearer -Token $secureToken -Headers @{ 'X-CSRF-Header' = '-' }
```

**The scope depends on what the integration needs, and no single scope is required.** Relativity's documentation shows two values for this grant: `SystemUserInfo` on the REST authentication page, the Usage Reports page and in the official Transfer SDK sample, and `UserInfoAccess` in the Kepler .NET samples. No page says which is correct, whether both work, or what either grants, and the OAuth2 Client's `Scopes` property is documented as one that cannot be set. Use the value the API you call documents, and confirm it.

**Cache the token, and re-acquire it on expiry — do not refresh.** Keep it in a script-scoped variable for its lifetime (part 3 recommends 60 minutes, Relativity's own wording), not on disk, and request a new one with the client secret when it expires or a call is rejected for authentication. Client Credentials offers no refresh tokens: **verified by NSerio against a live instance; not documented by Relativity** — the Allow Refresh Tokens control does not appear for this grant in the RelativityOne user interface.

**The client secret.** It is displayed once, at creation or reset. Store it as Secret Management (Part B of the Migration Documentation) describes; never in the script, never in tracked configuration. A credential saved with `Export-Clixml` is bound to the user and machine that saved it on Windows and is not protected on Linux or macOS (documented by Microsoft). A reset invalidates the previous secret immediately, so plan the cut-over before resetting it.

**Before deploying, confirm the calling identity.** Every call now runs as the OAuth2 Client's **Context User**, not as the user in the replaced credential. If that user has different workspace permissions or item-level security, the script returns fewer rows or `Unauthorized` with no change to its logic, and Relativity's audit attributes every call to that account. Confirm the Context User's permissions match those of the previous identity.

**When the integration acts for a signed-in user.** Client Credentials is one service identity by construction. That is the Authorization Code case, and moving to it means a new OAuth2 Client, because the flow is fixed when a client is created. The finding's flow note says when to make that choice.

Verification: **Verification note.** `X-CSRF-Header` is required and a token expires. The request object is usually single-use, so the token is applied to each new request.

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
<!-- generated:end PS-006 client-credentials -->

#### Authorization Code

<!-- generated:begin PS-006 authorization-code -->
**Confirming the flow.** An interactive-user signal in a file assigns Authorization Code, with this note: "An interactive-user signal in this file points to Authorization Code. Confirm the flow: use Client Credentials instead if the integration runs with credentials fixed for the integration and no signed-in user." Your user profile can also make Authorization Code the default; see Changing the default in [Confirming the flow](#confirming-the-flow). See [Confirming the flow](#confirming-the-flow) and the "Which flow" paragraph of PS-006 in [Part A](part-a-powershell.md#ps-006).

Source form: **Editions:** Windows PowerShell 5.1 and PowerShell 7.x.

```powershell
$request.Headers.Add([System.Net.HttpRequestHeader]::Authorization, "Basic $encoded")
```

Under token authentication: **First, the destination:** if this is not RelativityOne — including a Relativity Server instance, whose own documentation governs — this finding is informational and the rest of this template does not apply.

```powershell
$request.Headers.Add([System.Net.HttpRequestHeader]::Authorization, "Bearer $accessToken")
$request.Headers.Add('X-CSRF-Header', '-')
```

Credentials: **Where the credentials come from.** As for a hand-built header table: the write establishes nothing about the destination.

**Common to both variants**

**Sign in.** Replace the code that collected the user's credentials with a redirect: send the user's browser to the authorize endpoint `<host>/Relativity/Identity/connect/authorize`, which leads to the RelativityOne login page. The authorize endpoint is **verified by NSerio against a live instance; not documented by Relativity**. The request carries the standard authorization-code parameters — `client_id`, `response_type=code`, the registered `redirect_uri`, a `scope`, and a `state` value the script checks on return; these are standard OAuth, and Relativity documents none of them.

**Receive the code.** The browser returns to the Redirect URL registered on the OAuth2 Client, with the authorization code and the `state` value. Check the `state` before using the code.

**Exchange the code.** Send a form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=authorization_code`, the `code`, the same `redirect_uri`, the `client_id` and the `client_secret`, plus the `code_verifier` when PKCE is used. The exchange is **verified by NSerio against a live instance; not documented by Relativity**.

**Use the token.** Send `Authorization: Bearer <access token>` — the change at the use site in part 1 — with `X-CSRF-Header: -` on REST and Kepler calls.

**On expiry.** With Allow Refresh Tokens enabled on the OAuth2 Client and PKCE used, the script holds a refresh token and, when the access token expires, sends a `grant_type=refresh_token` request with it in place of the code exchange — *inferred* from the standard OAuth shape, not tested. Without a refresh token the user signs in again. Relativity does not document a refresh token's lifetime, whether it rotates, or how it is revoked. A refresh token is a secret: store it as Secret Management (Part B of the Migration Documentation) describes, not in a plain file.

**PKCE.** PKCE is required by Relativity's own documentation to obtain a refresh token, and not otherwise: a Code request that does not ask for a refresh token works without it, **verified by NSerio against a live instance; not documented by Relativity**. NSerio recommends PKCE with the `S256` method (NSerio recommendation), and does not claim that Relativity accepts any particular method, which it does not document.

**Scope.** No scope set is prescribed for Code. The set NSerio used works, **verified by NSerio against a live instance; not documented by Relativity**, verified as working and not as minimal.

**The script stops collecting the password.** After this change the script never receives, stores or forwards the user's user name and password: remove the prompt (`Get-Credential`, `Read-Host`) and any storage of the password.

**Single sign-on.** Sign-in happens on RelativityOne's own login page, so the instance's single sign-on and two-factor rules apply unchanged — *inferred* from where the sign-in happens, and not documented for clients that customers create.

**The client secret is required.** Code does not work without a client secret: tested and rejected, **verified by NSerio against a live instance; not documented by Relativity**, which confirms Relativity's own statement that the grant requires one. It is displayed once and is stored as Secret Management (Part B of the Migration Documentation) describes.

**Server-side variant.** For a script that runs on a server and receives the callback on a route of an application: one exact HTTPS Redirect URL per environment, no wildcards, registered by the Relativity administrator (NSerio recommendation; Relativity's matching rules are not documented); the secret and the user's tokens stay on the server. PowerShell is rarely the host of such a route; the server-side variant of the other languages applies unchanged.

**Desktop variant.** For a script a person runs on their own machine:

- **The system browser.** Open the authorize URL in the default browser, not in a web view the script hosts, so the script never sees the password (NSerio recommendation).
- **A loopback redirect on a fixed port.** Listen on the loopback address only, on the registered port, for one request. The Redirect URL is `http://127.0.0.1:<port>/<path>`: a plain-HTTP loopback URL is accepted, **verified by NSerio against a live instance; not documented by Relativity**. The Relativity administrator registers that exact URL, so the port is fixed and not chosen at runtime. **`HttpListener` accepts only a prefix that ends in `/`**, so the script appends one to the prefix it listens on and leaves the registered Redirect URL as registered: a request for `<path>?code=…` is received by the prefix `<path>/` (*checked by NSerio locally*, Windows PowerShell 5.1 and PowerShell 7.6.6: `Prefixes.Add` rejects a prefix without the slash, and the prefix with it answered both `/<path>?code=…` and `/<path>/?code=…`). `HttpListener` starts on the loopback address from a non-elevated process on both editions (*checked by NSerio locally*; a true standard account is not yet tested).
- **The secret is extractable.** Code requires a client secret, so a distributed script has to embed one, and **a script is text: anyone who can read the file has the secret.** It protects nothing against the person who runs the script.
- **Compensating guidance** (NSerio recommendation, subject to your own security policy): set Trusted IPs where outbound addresses are known and stable — they restrict where a token can be *requested* — use the recommended short Access Token Lifetime of 60 minutes, enable refresh tokens only when needed, and create one OAuth2 Client per distribution.
- **A known issue.** Relativity documents no recommendation for a distributed desktop application; this design is what NSerio verified works.

```powershell
# Windows PowerShell 5.1 and PowerShell 7.x. Executed against a local mock server on both, not against a live instance.
# $redirectUri is the exact URL registered on the OAuth2 Client, with a fixed port.
$state = [guid]::NewGuid().ToString('N')
$generator = [System.Security.Cryptography.RandomNumberGenerator]::Create()
$verifierBytes = New-Object byte[] 32
$generator.GetBytes($verifierBytes)
$verifier = [Convert]::ToBase64String($verifierBytes).TrimEnd('=').Replace('+', '-').Replace('/', '_')
$hasher = [System.Security.Cryptography.SHA256]::Create()
$challengeBytes = $hasher.ComputeHash([Text.Encoding]::ASCII.GetBytes($verifier))
$challenge = [Convert]::ToBase64String($challengeBytes).TrimEnd('=').Replace('+', '-').Replace('/', '_')

$listener = New-Object System.Net.HttpListener
$listener.Prefixes.Add($redirectUri.TrimEnd('/') + '/')   # a prefix must end in '/'; the registered URL is unchanged
$listener.Start()
$authorizeUrl = "$instanceUrl/Relativity/Identity/connect/authorize?client_id=$clientId&response_type=code" +
	"&redirect_uri=$([uri]::EscapeDataString($redirectUri))&scope=$([uri]::EscapeDataString($scope))" +
	"&state=$state&code_challenge=$challenge&code_challenge_method=S256"
Start-Process $authorizeUrl   # on Linux or macOS under 7.x use the platform opener instead (inferred)
$context = $listener.GetContext()
$query = $context.Request.QueryString
$page = [Text.Encoding]::UTF8.GetBytes('You can close this window.')
$context.Response.Close($page, $true)   # sends the page and waits until it is written, before the listener stops
$listener.Stop()
if ($query['state'] -ne $state) { throw 'The state value does not match.' }
$tokenResponse = Invoke-RestMethod -Method Post -Uri "$instanceUrl/Relativity/Identity/connect/token" -Body @{
	grant_type    = 'authorization_code'
	code          = $query['code']
	redirect_uri  = $redirectUri
	client_id     = $clientId
	client_secret = $clientSecret
	code_verifier = $verifier
}
$accessToken = $tokenResponse.access_token
```

**Why this form.** The two short .NET helpers for the hash and for random bytes exist on 7.x only, so the script creates the objects and calls their instance methods, which run on both editions (documented .NET API).

Verification: **Verification note.** `X-CSRF-Header` is required and a token expires. The request object is usually single-use, so the token is applied to each new request.

OAuth2 Client: Create or confirm this OAuth2 Client in RelativityOne (NSerio's observation: the menu path is Home > Authentication > OAuth2 Client Manager, as Relativity's REST API authentication page names it). This tool never connects to RelativityOne and never creates or changes the OAuth2 Client; the settings below are guidance.
- Name: a descriptive name identifying the application it serves.
- Flow Grant Type: Code
- Redirect URLs: Relativity documentation: each URL includes the http or https protocol prefix. The exact callback URL is registered by the Relativity administrator. Server-side web applications, NSerio's recommendation: one exact HTTPS URL per environment, with no wildcards. Desktop and CLI applications: a plain-HTTP loopback URL, `http://127.0.0.1:<port>/<path>`, on a fixed port because the URL is registered in advance and the port cannot be chosen at runtime (verified by NSerio against a live instance; not documented by Relativity).
- Access Token Lifetime: 60 minutes, stated in minutes because the field is in minutes. Relativity documentation: tokens for this flow must have a short lifetime, and the recommended value matches the Identity Server default.
- Allow refresh tokens: NSerio's recommendation: leave it off unless the application must keep a user session active beyond the access token lifetime without asking the user to sign in again. If it is on, the refresh tokens are stored as secrets, per Secret Management (Part B of the Migration Documentation).
- Enabled: on.
<!-- generated:end PS-006 authorization-code -->

### PS-007 · AuthenticationHeaderValueScheme

<a id="ps-007-migration"></a>

#### Client Credentials

<!-- generated:begin PS-007 client-credentials -->
**Confirming the flow.** When no signal in a file decides the flow, the tool assigns Client Credentials, the default, with this note: "Who supplies the credential at run time? If a person types it or signs in, the integration acts for that person: use Authorization Code. If it comes from configuration, settings, an environment variable or a secret store: use Client Credentials. No signal in this file showed which applies, so Client Credentials, the default, is recommended. Use Authorization Code instead if the integration acts on behalf of a signed-in user, or if your RelativityOne instance already has a Code-flow OAuth2 Client for this integration." A fixed-credential signal assigns it with this note: "A fixed-credential signal in this file points to Client Credentials, but how often that kind of signal is right has not been measured. Use Authorization Code instead if the integration acts on behalf of a signed-in user, or if your RelativityOne instance already has a Code-flow OAuth2 Client for this integration." See [Confirming the flow](#confirming-the-flow) and the "Which flow" paragraph of PS-007 in [Part A](part-a-powershell.md#ps-007).

Source form: **Editions:** Windows PowerShell 5.1 (after `Add-Type -AssemblyName System.Net.Http` where the type is not loaded) and PowerShell 7.x.

```powershell
$httpClient.DefaultRequestHeaders.Authorization = [System.Net.Http.Headers.AuthenticationHeaderValue]::new('Basic', $encoded)
```

Under token authentication: **First, the destination:** if this is not RelativityOne — including a Relativity Server instance, whose own documentation governs — this finding is informational and the rest of this template does not apply.

```powershell
$httpClient.DefaultRequestHeaders.Authorization = [System.Net.Http.Headers.AuthenticationHeaderValue]::new('Bearer', $accessToken)
$httpClient.DefaultRequestHeaders.Add('X-CSRF-Header', '-')
```

Credentials: **Where the credentials come from.** As for a hand-built header table: the construction establishes nothing about the destination.

**Where the token comes from.** Create an OAuth2 Client with Flow Grant Type set to Client Credentials (part 3 lists its settings), then send a form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=client_credentials`, `client_id`, `client_secret` and a `scope`. The token endpoint is **documented** for this grant. The token is then sent as `Authorization: Bearer <token>`, with `X-CSRF-Header: -` on REST and Kepler calls — the header is documented as required, and a Kepler call without it returns 404.

```powershell
# Windows PowerShell 5.1 and PowerShell 7.x. $clientId and $clientSecret come from the process
# environment or a secret store (Secret Management (Part B of the Migration Documentation)), never from this script.
$tokenBody = @{
	grant_type    = 'client_credentials'
	client_id     = $clientId
	client_secret = $clientSecret
	scope         = $scope
}
$tokenResponse = Invoke-RestMethod -Method Post -Uri "$instanceUrl/Relativity/Identity/connect/token" -Body $tokenBody -ContentType 'application/x-www-form-urlencoded'
$accessToken = $tokenResponse.access_token
$headerTable = @{ Authorization = "Bearer $accessToken"; 'X-CSRF-Header' = '-' }
```

```powershell
# Windows PowerShell 5.1 only, before the first request, if the host does not already negotiate TLS 1.2
# (inferred from the .NET Framework default; the local mock-server run is plain http, so it does not exercise TLS).
[Net.ServicePointManager]::SecurityProtocol = [Net.ServicePointManager]::SecurityProtocol -bor [Net.SecurityProtocolType]::Tls12
```

```powershell
# PowerShell 7.x alternative to the header: the cmdlet sends the bearer token itself
# (checked by NSerio locally, 7.6.6: Authorization: Bearer is sent pre-emptively).
$secureToken = ConvertTo-SecureString $accessToken -AsPlainText -Force
Invoke-RestMethod -Uri $uri -Authentication Bearer -Token $secureToken -Headers @{ 'X-CSRF-Header' = '-' }
```

**The scope depends on what the integration needs, and no single scope is required.** Relativity's documentation shows two values for this grant: `SystemUserInfo` on the REST authentication page, the Usage Reports page and in the official Transfer SDK sample, and `UserInfoAccess` in the Kepler .NET samples. No page says which is correct, whether both work, or what either grants, and the OAuth2 Client's `Scopes` property is documented as one that cannot be set. Use the value the API you call documents, and confirm it.

**Cache the token, and re-acquire it on expiry — do not refresh.** Keep it in a script-scoped variable for its lifetime (part 3 recommends 60 minutes, Relativity's own wording), not on disk, and request a new one with the client secret when it expires or a call is rejected for authentication. Client Credentials offers no refresh tokens: **verified by NSerio against a live instance; not documented by Relativity** — the Allow Refresh Tokens control does not appear for this grant in the RelativityOne user interface.

**The client secret.** It is displayed once, at creation or reset. Store it as Secret Management (Part B of the Migration Documentation) describes; never in the script, never in tracked configuration. A credential saved with `Export-Clixml` is bound to the user and machine that saved it on Windows and is not protected on Linux or macOS (documented by Microsoft). A reset invalidates the previous secret immediately, so plan the cut-over before resetting it.

**Before deploying, confirm the calling identity.** Every call now runs as the OAuth2 Client's **Context User**, not as the user in the replaced credential. If that user has different workspace permissions or item-level security, the script returns fewer rows or `Unauthorized` with no change to its logic, and Relativity's audit attributes every call to that account. Confirm the Context User's permissions match those of the previous identity.

**When the integration acts for a signed-in user.** Client Credentials is one service identity by construction. That is the Authorization Code case, and moving to it means a new OAuth2 Client, because the flow is fixed when a client is created. The finding's flow note says when to make that choice.

Verification: **Verification note — two things, and the first can break working code.** **A default header on a client lives as long as the client:** a client built once keeps the expired token, so rebuild the header with a current token within its lifetime. `X-CSRF-Header` is required. On Windows PowerShell 5.1 the type is loaded with `Add-Type -AssemblyName System.Net.Http` where it is not already.

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
<!-- generated:end PS-007 client-credentials -->

#### Authorization Code

<!-- generated:begin PS-007 authorization-code -->
**Confirming the flow.** An interactive-user signal in a file assigns Authorization Code, with this note: "An interactive-user signal in this file points to Authorization Code. Confirm the flow: use Client Credentials instead if the integration runs with credentials fixed for the integration and no signed-in user." Your user profile can also make Authorization Code the default; see Changing the default in [Confirming the flow](#confirming-the-flow). See [Confirming the flow](#confirming-the-flow) and the "Which flow" paragraph of PS-007 in [Part A](part-a-powershell.md#ps-007).

Source form: **Editions:** Windows PowerShell 5.1 (after `Add-Type -AssemblyName System.Net.Http` where the type is not loaded) and PowerShell 7.x.

```powershell
$httpClient.DefaultRequestHeaders.Authorization = [System.Net.Http.Headers.AuthenticationHeaderValue]::new('Basic', $encoded)
```

Under token authentication: **First, the destination:** if this is not RelativityOne — including a Relativity Server instance, whose own documentation governs — this finding is informational and the rest of this template does not apply.

```powershell
$httpClient.DefaultRequestHeaders.Authorization = [System.Net.Http.Headers.AuthenticationHeaderValue]::new('Bearer', $accessToken)
$httpClient.DefaultRequestHeaders.Add('X-CSRF-Header', '-')
```

Credentials: **Where the credentials come from.** As for a hand-built header table: the construction establishes nothing about the destination.

**Common to both variants**

**Sign in.** Replace the code that collected the user's credentials with a redirect: send the user's browser to the authorize endpoint `<host>/Relativity/Identity/connect/authorize`, which leads to the RelativityOne login page. The authorize endpoint is **verified by NSerio against a live instance; not documented by Relativity**. The request carries the standard authorization-code parameters — `client_id`, `response_type=code`, the registered `redirect_uri`, a `scope`, and a `state` value the script checks on return; these are standard OAuth, and Relativity documents none of them.

**Receive the code.** The browser returns to the Redirect URL registered on the OAuth2 Client, with the authorization code and the `state` value. Check the `state` before using the code.

**Exchange the code.** Send a form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=authorization_code`, the `code`, the same `redirect_uri`, the `client_id` and the `client_secret`, plus the `code_verifier` when PKCE is used. The exchange is **verified by NSerio against a live instance; not documented by Relativity**.

**Use the token.** Send `Authorization: Bearer <access token>` — the change at the use site in part 1 — with `X-CSRF-Header: -` on REST and Kepler calls.

**On expiry.** With Allow Refresh Tokens enabled on the OAuth2 Client and PKCE used, the script holds a refresh token and, when the access token expires, sends a `grant_type=refresh_token` request with it in place of the code exchange — *inferred* from the standard OAuth shape, not tested. Without a refresh token the user signs in again. Relativity does not document a refresh token's lifetime, whether it rotates, or how it is revoked. A refresh token is a secret: store it as Secret Management (Part B of the Migration Documentation) describes, not in a plain file.

**PKCE.** PKCE is required by Relativity's own documentation to obtain a refresh token, and not otherwise: a Code request that does not ask for a refresh token works without it, **verified by NSerio against a live instance; not documented by Relativity**. NSerio recommends PKCE with the `S256` method (NSerio recommendation), and does not claim that Relativity accepts any particular method, which it does not document.

**Scope.** No scope set is prescribed for Code. The set NSerio used works, **verified by NSerio against a live instance; not documented by Relativity**, verified as working and not as minimal.

**The script stops collecting the password.** After this change the script never receives, stores or forwards the user's user name and password: remove the prompt (`Get-Credential`, `Read-Host`) and any storage of the password.

**Single sign-on.** Sign-in happens on RelativityOne's own login page, so the instance's single sign-on and two-factor rules apply unchanged — *inferred* from where the sign-in happens, and not documented for clients that customers create.

**The client secret is required.** Code does not work without a client secret: tested and rejected, **verified by NSerio against a live instance; not documented by Relativity**, which confirms Relativity's own statement that the grant requires one. It is displayed once and is stored as Secret Management (Part B of the Migration Documentation) describes.

**Server-side variant.** For a script that runs on a server and receives the callback on a route of an application: one exact HTTPS Redirect URL per environment, no wildcards, registered by the Relativity administrator (NSerio recommendation; Relativity's matching rules are not documented); the secret and the user's tokens stay on the server. PowerShell is rarely the host of such a route; the server-side variant of the other languages applies unchanged.

**Desktop variant.** For a script a person runs on their own machine:

- **The system browser.** Open the authorize URL in the default browser, not in a web view the script hosts, so the script never sees the password (NSerio recommendation).
- **A loopback redirect on a fixed port.** Listen on the loopback address only, on the registered port, for one request. The Redirect URL is `http://127.0.0.1:<port>/<path>`: a plain-HTTP loopback URL is accepted, **verified by NSerio against a live instance; not documented by Relativity**. The Relativity administrator registers that exact URL, so the port is fixed and not chosen at runtime. **`HttpListener` accepts only a prefix that ends in `/`**, so the script appends one to the prefix it listens on and leaves the registered Redirect URL as registered: a request for `<path>?code=…` is received by the prefix `<path>/` (*checked by NSerio locally*, Windows PowerShell 5.1 and PowerShell 7.6.6: `Prefixes.Add` rejects a prefix without the slash, and the prefix with it answered both `/<path>?code=…` and `/<path>/?code=…`). `HttpListener` starts on the loopback address from a non-elevated process on both editions (*checked by NSerio locally*; a true standard account is not yet tested).
- **The secret is extractable.** Code requires a client secret, so a distributed script has to embed one, and **a script is text: anyone who can read the file has the secret.** It protects nothing against the person who runs the script.
- **Compensating guidance** (NSerio recommendation, subject to your own security policy): set Trusted IPs where outbound addresses are known and stable — they restrict where a token can be *requested* — use the recommended short Access Token Lifetime of 60 minutes, enable refresh tokens only when needed, and create one OAuth2 Client per distribution.
- **A known issue.** Relativity documents no recommendation for a distributed desktop application; this design is what NSerio verified works.

```powershell
# Windows PowerShell 5.1 and PowerShell 7.x. Executed against a local mock server on both, not against a live instance.
# $redirectUri is the exact URL registered on the OAuth2 Client, with a fixed port.
$state = [guid]::NewGuid().ToString('N')
$generator = [System.Security.Cryptography.RandomNumberGenerator]::Create()
$verifierBytes = New-Object byte[] 32
$generator.GetBytes($verifierBytes)
$verifier = [Convert]::ToBase64String($verifierBytes).TrimEnd('=').Replace('+', '-').Replace('/', '_')
$hasher = [System.Security.Cryptography.SHA256]::Create()
$challengeBytes = $hasher.ComputeHash([Text.Encoding]::ASCII.GetBytes($verifier))
$challenge = [Convert]::ToBase64String($challengeBytes).TrimEnd('=').Replace('+', '-').Replace('/', '_')

$listener = New-Object System.Net.HttpListener
$listener.Prefixes.Add($redirectUri.TrimEnd('/') + '/')   # a prefix must end in '/'; the registered URL is unchanged
$listener.Start()
$authorizeUrl = "$instanceUrl/Relativity/Identity/connect/authorize?client_id=$clientId&response_type=code" +
	"&redirect_uri=$([uri]::EscapeDataString($redirectUri))&scope=$([uri]::EscapeDataString($scope))" +
	"&state=$state&code_challenge=$challenge&code_challenge_method=S256"
Start-Process $authorizeUrl   # on Linux or macOS under 7.x use the platform opener instead (inferred)
$context = $listener.GetContext()
$query = $context.Request.QueryString
$page = [Text.Encoding]::UTF8.GetBytes('You can close this window.')
$context.Response.Close($page, $true)   # sends the page and waits until it is written, before the listener stops
$listener.Stop()
if ($query['state'] -ne $state) { throw 'The state value does not match.' }
$tokenResponse = Invoke-RestMethod -Method Post -Uri "$instanceUrl/Relativity/Identity/connect/token" -Body @{
	grant_type    = 'authorization_code'
	code          = $query['code']
	redirect_uri  = $redirectUri
	client_id     = $clientId
	client_secret = $clientSecret
	code_verifier = $verifier
}
$accessToken = $tokenResponse.access_token
```

**Why this form.** The two short .NET helpers for the hash and for random bytes exist on 7.x only, so the script creates the objects and calls their instance methods, which run on both editions (documented .NET API).

Verification: **Verification note — two things, and the first can break working code.** **A default header on a client lives as long as the client:** a client built once keeps the expired token, so rebuild the header with a current token within its lifetime. `X-CSRF-Header` is required. On Windows PowerShell 5.1 the type is loaded with `Add-Type -AssemblyName System.Net.Http` where it is not already.

OAuth2 Client: Create or confirm this OAuth2 Client in RelativityOne (NSerio's observation: the menu path is Home > Authentication > OAuth2 Client Manager, as Relativity's REST API authentication page names it). This tool never connects to RelativityOne and never creates or changes the OAuth2 Client; the settings below are guidance.
- Name: a descriptive name identifying the application it serves.
- Flow Grant Type: Code
- Redirect URLs: Relativity documentation: each URL includes the http or https protocol prefix. The exact callback URL is registered by the Relativity administrator. Server-side web applications, NSerio's recommendation: one exact HTTPS URL per environment, with no wildcards. Desktop and CLI applications: a plain-HTTP loopback URL, `http://127.0.0.1:<port>/<path>`, on a fixed port because the URL is registered in advance and the port cannot be chosen at runtime (verified by NSerio against a live instance; not documented by Relativity).
- Access Token Lifetime: 60 minutes, stated in minutes because the field is in minutes. Relativity documentation: tokens for this flow must have a short lifetime, and the recommended value matches the Identity Server default.
- Allow refresh tokens: NSerio's recommendation: leave it off unless the application must keep a user session active beyond the access token lifetime without asking the user to sign in again. If it is on, the refresh tokens are stored as secrets, per Secret Management (Part B of the Migration Documentation).
- Enabled: on.
<!-- generated:end PS-007 authorization-code -->

### PS-008 · CredentialCacheBasicScheme

<a id="ps-008-migration"></a>

#### Client Credentials

<!-- generated:begin PS-008 client-credentials -->
**Confirming the flow.** When no signal in a file decides the flow, the tool assigns Client Credentials, the default, with this note: "Who supplies the credential at run time? If a person types it or signs in, the integration acts for that person: use Authorization Code. If it comes from configuration, settings, an environment variable or a secret store: use Client Credentials. No signal in this file showed which applies, so Client Credentials, the default, is recommended. Use Authorization Code instead if the integration acts on behalf of a signed-in user, or if your RelativityOne instance already has a Code-flow OAuth2 Client for this integration." A fixed-credential signal assigns it with this note: "A fixed-credential signal in this file points to Client Credentials, but how often that kind of signal is right has not been measured. Use Authorization Code instead if the integration acts on behalf of a signed-in user, or if your RelativityOne instance already has a Code-flow OAuth2 Client for this integration." See [Confirming the flow](#confirming-the-flow) and the "Which flow" paragraph of PS-008 in [Part A](part-a-powershell.md#ps-008).

Source form: **Editions:** Windows PowerShell 5.1 and PowerShell 7.x.

```powershell
$cache.Add([Uri]$uri, 'Basic', $networkCredential)
```

Under token authentication: **First, the destination:** if this is not RelativityOne — including a Relativity Server instance, whose own documentation governs — this finding is informational and the rest of this template does not apply.

Remove the cache entry, and send the bearer header on the request instead:

```powershell
Invoke-RestMethod -Uri $uri -Headers @{ Authorization = "Bearer $accessToken"; 'X-CSRF-Header' = '-' }
```

Credentials: **Where the credentials come from.** The call names the scheme but not the destination. If it is RelativityOne, the token comes from the token-acquisition part that follows, and the user name and password that built the `NetworkCredential` are no longer used.

**Where the token comes from.** Create an OAuth2 Client with Flow Grant Type set to Client Credentials (part 3 lists its settings), then send a form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=client_credentials`, `client_id`, `client_secret` and a `scope`. The token endpoint is **documented** for this grant. The token is then sent as `Authorization: Bearer <token>`, with `X-CSRF-Header: -` on REST and Kepler calls — the header is documented as required, and a Kepler call without it returns 404.

```powershell
# Windows PowerShell 5.1 and PowerShell 7.x. $clientId and $clientSecret come from the process
# environment or a secret store (Secret Management (Part B of the Migration Documentation)), never from this script.
$tokenBody = @{
	grant_type    = 'client_credentials'
	client_id     = $clientId
	client_secret = $clientSecret
	scope         = $scope
}
$tokenResponse = Invoke-RestMethod -Method Post -Uri "$instanceUrl/Relativity/Identity/connect/token" -Body $tokenBody -ContentType 'application/x-www-form-urlencoded'
$accessToken = $tokenResponse.access_token
$headerTable = @{ Authorization = "Bearer $accessToken"; 'X-CSRF-Header' = '-' }
```

```powershell
# Windows PowerShell 5.1 only, before the first request, if the host does not already negotiate TLS 1.2
# (inferred from the .NET Framework default; the local mock-server run is plain http, so it does not exercise TLS).
[Net.ServicePointManager]::SecurityProtocol = [Net.ServicePointManager]::SecurityProtocol -bor [Net.SecurityProtocolType]::Tls12
```

```powershell
# PowerShell 7.x alternative to the header: the cmdlet sends the bearer token itself
# (checked by NSerio locally, 7.6.6: Authorization: Bearer is sent pre-emptively).
$secureToken = ConvertTo-SecureString $accessToken -AsPlainText -Force
Invoke-RestMethod -Uri $uri -Authentication Bearer -Token $secureToken -Headers @{ 'X-CSRF-Header' = '-' }
```

**The scope depends on what the integration needs, and no single scope is required.** Relativity's documentation shows two values for this grant: `SystemUserInfo` on the REST authentication page, the Usage Reports page and in the official Transfer SDK sample, and `UserInfoAccess` in the Kepler .NET samples. No page says which is correct, whether both work, or what either grants, and the OAuth2 Client's `Scopes` property is documented as one that cannot be set. Use the value the API you call documents, and confirm it.

**Cache the token, and re-acquire it on expiry — do not refresh.** Keep it in a script-scoped variable for its lifetime (part 3 recommends 60 minutes, Relativity's own wording), not on disk, and request a new one with the client secret when it expires or a call is rejected for authentication. Client Credentials offers no refresh tokens: **verified by NSerio against a live instance; not documented by Relativity** — the Allow Refresh Tokens control does not appear for this grant in the RelativityOne user interface.

**The client secret.** It is displayed once, at creation or reset. Store it as Secret Management (Part B of the Migration Documentation) describes; never in the script, never in tracked configuration. A credential saved with `Export-Clixml` is bound to the user and machine that saved it on Windows and is not protected on Linux or macOS (documented by Microsoft). A reset invalidates the previous secret immediately, so plan the cut-over before resetting it.

**Before deploying, confirm the calling identity.** Every call now runs as the OAuth2 Client's **Context User**, not as the user in the replaced credential. If that user has different workspace permissions or item-level security, the script returns fewer rows or `Unauthorized` with no change to its logic, and Relativity's audit attributes every call to that account. Confirm the Context User's permissions match those of the previous identity.

**When the integration acts for a signed-in user.** Client Credentials is one service identity by construction. That is the Authorization Code case, and moving to it means a new OAuth2 Client, because the flow is fixed when a client is created. The finding's flow note says when to make that choice.

Verification: **Verification note.** A credential cache can serve several requests and addresses; **check every request that used the cache** before removing it, because the cache is shared state. `X-CSRF-Header` is required and a token expires.

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
<!-- generated:end PS-008 client-credentials -->

#### Authorization Code

<!-- generated:begin PS-008 authorization-code -->
**Confirming the flow.** An interactive-user signal in a file assigns Authorization Code, with this note: "An interactive-user signal in this file points to Authorization Code. Confirm the flow: use Client Credentials instead if the integration runs with credentials fixed for the integration and no signed-in user." Your user profile can also make Authorization Code the default; see Changing the default in [Confirming the flow](#confirming-the-flow). See [Confirming the flow](#confirming-the-flow) and the "Which flow" paragraph of PS-008 in [Part A](part-a-powershell.md#ps-008).

Source form: **Editions:** Windows PowerShell 5.1 and PowerShell 7.x.

```powershell
$cache.Add([Uri]$uri, 'Basic', $networkCredential)
```

Under token authentication: **First, the destination:** if this is not RelativityOne — including a Relativity Server instance, whose own documentation governs — this finding is informational and the rest of this template does not apply.

Remove the cache entry, and send the bearer header on the request instead:

```powershell
Invoke-RestMethod -Uri $uri -Headers @{ Authorization = "Bearer $accessToken"; 'X-CSRF-Header' = '-' }
```

Credentials: **Where the credentials come from.** The call names the scheme but not the destination. If it is RelativityOne, the token comes from the token-acquisition part that follows, and the user name and password that built the `NetworkCredential` are no longer used.

**Common to both variants**

**Sign in.** Replace the code that collected the user's credentials with a redirect: send the user's browser to the authorize endpoint `<host>/Relativity/Identity/connect/authorize`, which leads to the RelativityOne login page. The authorize endpoint is **verified by NSerio against a live instance; not documented by Relativity**. The request carries the standard authorization-code parameters — `client_id`, `response_type=code`, the registered `redirect_uri`, a `scope`, and a `state` value the script checks on return; these are standard OAuth, and Relativity documents none of them.

**Receive the code.** The browser returns to the Redirect URL registered on the OAuth2 Client, with the authorization code and the `state` value. Check the `state` before using the code.

**Exchange the code.** Send a form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=authorization_code`, the `code`, the same `redirect_uri`, the `client_id` and the `client_secret`, plus the `code_verifier` when PKCE is used. The exchange is **verified by NSerio against a live instance; not documented by Relativity**.

**Use the token.** Send `Authorization: Bearer <access token>` — the change at the use site in part 1 — with `X-CSRF-Header: -` on REST and Kepler calls.

**On expiry.** With Allow Refresh Tokens enabled on the OAuth2 Client and PKCE used, the script holds a refresh token and, when the access token expires, sends a `grant_type=refresh_token` request with it in place of the code exchange — *inferred* from the standard OAuth shape, not tested. Without a refresh token the user signs in again. Relativity does not document a refresh token's lifetime, whether it rotates, or how it is revoked. A refresh token is a secret: store it as Secret Management (Part B of the Migration Documentation) describes, not in a plain file.

**PKCE.** PKCE is required by Relativity's own documentation to obtain a refresh token, and not otherwise: a Code request that does not ask for a refresh token works without it, **verified by NSerio against a live instance; not documented by Relativity**. NSerio recommends PKCE with the `S256` method (NSerio recommendation), and does not claim that Relativity accepts any particular method, which it does not document.

**Scope.** No scope set is prescribed for Code. The set NSerio used works, **verified by NSerio against a live instance; not documented by Relativity**, verified as working and not as minimal.

**The script stops collecting the password.** After this change the script never receives, stores or forwards the user's user name and password: remove the prompt (`Get-Credential`, `Read-Host`) and any storage of the password.

**Single sign-on.** Sign-in happens on RelativityOne's own login page, so the instance's single sign-on and two-factor rules apply unchanged — *inferred* from where the sign-in happens, and not documented for clients that customers create.

**The client secret is required.** Code does not work without a client secret: tested and rejected, **verified by NSerio against a live instance; not documented by Relativity**, which confirms Relativity's own statement that the grant requires one. It is displayed once and is stored as Secret Management (Part B of the Migration Documentation) describes.

**Server-side variant.** For a script that runs on a server and receives the callback on a route of an application: one exact HTTPS Redirect URL per environment, no wildcards, registered by the Relativity administrator (NSerio recommendation; Relativity's matching rules are not documented); the secret and the user's tokens stay on the server. PowerShell is rarely the host of such a route; the server-side variant of the other languages applies unchanged.

**Desktop variant.** For a script a person runs on their own machine:

- **The system browser.** Open the authorize URL in the default browser, not in a web view the script hosts, so the script never sees the password (NSerio recommendation).
- **A loopback redirect on a fixed port.** Listen on the loopback address only, on the registered port, for one request. The Redirect URL is `http://127.0.0.1:<port>/<path>`: a plain-HTTP loopback URL is accepted, **verified by NSerio against a live instance; not documented by Relativity**. The Relativity administrator registers that exact URL, so the port is fixed and not chosen at runtime. **`HttpListener` accepts only a prefix that ends in `/`**, so the script appends one to the prefix it listens on and leaves the registered Redirect URL as registered: a request for `<path>?code=…` is received by the prefix `<path>/` (*checked by NSerio locally*, Windows PowerShell 5.1 and PowerShell 7.6.6: `Prefixes.Add` rejects a prefix without the slash, and the prefix with it answered both `/<path>?code=…` and `/<path>/?code=…`). `HttpListener` starts on the loopback address from a non-elevated process on both editions (*checked by NSerio locally*; a true standard account is not yet tested).
- **The secret is extractable.** Code requires a client secret, so a distributed script has to embed one, and **a script is text: anyone who can read the file has the secret.** It protects nothing against the person who runs the script.
- **Compensating guidance** (NSerio recommendation, subject to your own security policy): set Trusted IPs where outbound addresses are known and stable — they restrict where a token can be *requested* — use the recommended short Access Token Lifetime of 60 minutes, enable refresh tokens only when needed, and create one OAuth2 Client per distribution.
- **A known issue.** Relativity documents no recommendation for a distributed desktop application; this design is what NSerio verified works.

```powershell
# Windows PowerShell 5.1 and PowerShell 7.x. Executed against a local mock server on both, not against a live instance.
# $redirectUri is the exact URL registered on the OAuth2 Client, with a fixed port.
$state = [guid]::NewGuid().ToString('N')
$generator = [System.Security.Cryptography.RandomNumberGenerator]::Create()
$verifierBytes = New-Object byte[] 32
$generator.GetBytes($verifierBytes)
$verifier = [Convert]::ToBase64String($verifierBytes).TrimEnd('=').Replace('+', '-').Replace('/', '_')
$hasher = [System.Security.Cryptography.SHA256]::Create()
$challengeBytes = $hasher.ComputeHash([Text.Encoding]::ASCII.GetBytes($verifier))
$challenge = [Convert]::ToBase64String($challengeBytes).TrimEnd('=').Replace('+', '-').Replace('/', '_')

$listener = New-Object System.Net.HttpListener
$listener.Prefixes.Add($redirectUri.TrimEnd('/') + '/')   # a prefix must end in '/'; the registered URL is unchanged
$listener.Start()
$authorizeUrl = "$instanceUrl/Relativity/Identity/connect/authorize?client_id=$clientId&response_type=code" +
	"&redirect_uri=$([uri]::EscapeDataString($redirectUri))&scope=$([uri]::EscapeDataString($scope))" +
	"&state=$state&code_challenge=$challenge&code_challenge_method=S256"
Start-Process $authorizeUrl   # on Linux or macOS under 7.x use the platform opener instead (inferred)
$context = $listener.GetContext()
$query = $context.Request.QueryString
$page = [Text.Encoding]::UTF8.GetBytes('You can close this window.')
$context.Response.Close($page, $true)   # sends the page and waits until it is written, before the listener stops
$listener.Stop()
if ($query['state'] -ne $state) { throw 'The state value does not match.' }
$tokenResponse = Invoke-RestMethod -Method Post -Uri "$instanceUrl/Relativity/Identity/connect/token" -Body @{
	grant_type    = 'authorization_code'
	code          = $query['code']
	redirect_uri  = $redirectUri
	client_id     = $clientId
	client_secret = $clientSecret
	code_verifier = $verifier
}
$accessToken = $tokenResponse.access_token
```

**Why this form.** The two short .NET helpers for the hash and for random bytes exist on 7.x only, so the script creates the objects and calls their instance methods, which run on both editions (documented .NET API).

Verification: **Verification note.** A credential cache can serve several requests and addresses; **check every request that used the cache** before removing it, because the cache is shared state. `X-CSRF-Header` is required and a token expires.

OAuth2 Client: Create or confirm this OAuth2 Client in RelativityOne (NSerio's observation: the menu path is Home > Authentication > OAuth2 Client Manager, as Relativity's REST API authentication page names it). This tool never connects to RelativityOne and never creates or changes the OAuth2 Client; the settings below are guidance.
- Name: a descriptive name identifying the application it serves.
- Flow Grant Type: Code
- Redirect URLs: Relativity documentation: each URL includes the http or https protocol prefix. The exact callback URL is registered by the Relativity administrator. Server-side web applications, NSerio's recommendation: one exact HTTPS URL per environment, with no wildcards. Desktop and CLI applications: a plain-HTTP loopback URL, `http://127.0.0.1:<port>/<path>`, on a fixed port because the URL is registered in advance and the port cannot be chosen at runtime (verified by NSerio against a live instance; not documented by Relativity).
- Access Token Lifetime: 60 minutes, stated in minutes because the field is in minutes. Relativity documentation: tokens for this flow must have a short lifetime, and the recommended value matches the Identity Server default.
- Allow refresh tokens: NSerio's recommendation: leave it off unless the application must keep a user session active beyond the access token lifetime without asking the user to sign in again. If it is on, the refresh tokens are stored as secrets, per Secret Management (Part B of the Migration Documentation).
- Enabled: on.
<!-- generated:end PS-008 authorization-code -->

### PS-009 · NetworkCredentialOnHttpType

<a id="ps-009-migration"></a>

#### Client Credentials

<!-- generated:begin PS-009 client-credentials -->
**Confirming the flow.** When no signal in a file decides the flow, the tool assigns Client Credentials, the default, with this note: "Who supplies the credential at run time? If a person types it or signs in, the integration acts for that person: use Authorization Code. If it comes from configuration, settings, an environment variable or a secret store: use Client Credentials. No signal in this file showed which applies, so Client Credentials, the default, is recommended. Use Authorization Code instead if the integration acts on behalf of a signed-in user, or if your RelativityOne instance already has a Code-flow OAuth2 Client for this integration." A fixed-credential signal assigns it with this note: "A fixed-credential signal in this file points to Client Credentials, but how often that kind of signal is right has not been measured. Use Authorization Code instead if the integration acts on behalf of a signed-in user, or if your RelativityOne instance already has a Code-flow OAuth2 Client for this integration." See [Confirming the flow](#confirming-the-flow) and the "Which flow" paragraph of PS-009 in [Part A](part-a-powershell.md#ps-009).

Source form: **Editions:** Windows PowerShell 5.1 and PowerShell 7.x.

```powershell
$handler.Credentials = New-Object System.Net.NetworkCredential($loginName, $loginSecret)
```

Under token authentication: **First, the destination:** if this is not RelativityOne — including a Relativity Server instance, whose own documentation governs — this finding is informational and the rest of this template does not apply.

Remove the credential assignment and send the bearer header with the requests the client makes:

```powershell
$httpClient.DefaultRequestHeaders.Authorization = [System.Net.Http.Headers.AuthenticationHeaderValue]::new('Bearer', $accessToken)
$httpClient.DefaultRequestHeaders.Add('X-CSRF-Header', '-')
```

Credentials: **Where the credentials come from.** `NetworkCredential` declares no scheme: the type serves integrated authentication, mail, file transfer and proxies. This finding is reported because the credential is bound to an HTTP client or handler; **it may not be Basic at all.** Establish that, and the destination, before changing it.

**Where the token comes from.** Create an OAuth2 Client with Flow Grant Type set to Client Credentials (part 3 lists its settings), then send a form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=client_credentials`, `client_id`, `client_secret` and a `scope`. The token endpoint is **documented** for this grant. The token is then sent as `Authorization: Bearer <token>`, with `X-CSRF-Header: -` on REST and Kepler calls — the header is documented as required, and a Kepler call without it returns 404.

```powershell
# Windows PowerShell 5.1 and PowerShell 7.x. $clientId and $clientSecret come from the process
# environment or a secret store (Secret Management (Part B of the Migration Documentation)), never from this script.
$tokenBody = @{
	grant_type    = 'client_credentials'
	client_id     = $clientId
	client_secret = $clientSecret
	scope         = $scope
}
$tokenResponse = Invoke-RestMethod -Method Post -Uri "$instanceUrl/Relativity/Identity/connect/token" -Body $tokenBody -ContentType 'application/x-www-form-urlencoded'
$accessToken = $tokenResponse.access_token
$headerTable = @{ Authorization = "Bearer $accessToken"; 'X-CSRF-Header' = '-' }
```

```powershell
# Windows PowerShell 5.1 only, before the first request, if the host does not already negotiate TLS 1.2
# (inferred from the .NET Framework default; the local mock-server run is plain http, so it does not exercise TLS).
[Net.ServicePointManager]::SecurityProtocol = [Net.ServicePointManager]::SecurityProtocol -bor [Net.SecurityProtocolType]::Tls12
```

```powershell
# PowerShell 7.x alternative to the header: the cmdlet sends the bearer token itself
# (checked by NSerio locally, 7.6.6: Authorization: Bearer is sent pre-emptively).
$secureToken = ConvertTo-SecureString $accessToken -AsPlainText -Force
Invoke-RestMethod -Uri $uri -Authentication Bearer -Token $secureToken -Headers @{ 'X-CSRF-Header' = '-' }
```

**The scope depends on what the integration needs, and no single scope is required.** Relativity's documentation shows two values for this grant: `SystemUserInfo` on the REST authentication page, the Usage Reports page and in the official Transfer SDK sample, and `UserInfoAccess` in the Kepler .NET samples. No page says which is correct, whether both work, or what either grants, and the OAuth2 Client's `Scopes` property is documented as one that cannot be set. Use the value the API you call documents, and confirm it.

**Cache the token, and re-acquire it on expiry — do not refresh.** Keep it in a script-scoped variable for its lifetime (part 3 recommends 60 minutes, Relativity's own wording), not on disk, and request a new one with the client secret when it expires or a call is rejected for authentication. Client Credentials offers no refresh tokens: **verified by NSerio against a live instance; not documented by Relativity** — the Allow Refresh Tokens control does not appear for this grant in the RelativityOne user interface.

**The client secret.** It is displayed once, at creation or reset. Store it as Secret Management (Part B of the Migration Documentation) describes; never in the script, never in tracked configuration. A credential saved with `Export-Clixml` is bound to the user and machine that saved it on Windows and is not protected on Linux or macOS (documented by Microsoft). A reset invalidates the previous secret immediately, so plan the cut-over before resetting it.

**Before deploying, confirm the calling identity.** Every call now runs as the OAuth2 Client's **Context User**, not as the user in the replaced credential. If that user has different workspace permissions or item-level security, the script returns fewer rows or `Unauthorized` with no change to its logic, and Relativity's audit attributes every call to that account. Confirm the Context User's permissions match those of the previous identity.

**When the integration acts for a signed-in user.** Client Credentials is one service identity by construction. That is the Authorization Code case, and moving to it means a new OAuth2 Client, because the flow is fixed when a client is created. The finding's flow note says when to make that choice.

Verification: **Verification note.** **Confirm that this client really sends Basic.** A handler holding a credential is documented to answer an authentication challenge (*inferred* for this construct); against a server that challenges with Bearer only, as RelativityOne does (probed for the web cmdlets, not for this type), it may send nothing. If it sends nothing, the script already authenticates by other means and this change is not needed. `X-CSRF-Header` is required; a token expires.

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
<!-- generated:end PS-009 client-credentials -->

#### Authorization Code

<!-- generated:begin PS-009 authorization-code -->
**Confirming the flow.** An interactive-user signal in a file assigns Authorization Code, with this note: "An interactive-user signal in this file points to Authorization Code. Confirm the flow: use Client Credentials instead if the integration runs with credentials fixed for the integration and no signed-in user." Your user profile can also make Authorization Code the default; see Changing the default in [Confirming the flow](#confirming-the-flow). See [Confirming the flow](#confirming-the-flow) and the "Which flow" paragraph of PS-009 in [Part A](part-a-powershell.md#ps-009).

Source form: **Editions:** Windows PowerShell 5.1 and PowerShell 7.x.

```powershell
$handler.Credentials = New-Object System.Net.NetworkCredential($loginName, $loginSecret)
```

Under token authentication: **First, the destination:** if this is not RelativityOne — including a Relativity Server instance, whose own documentation governs — this finding is informational and the rest of this template does not apply.

Remove the credential assignment and send the bearer header with the requests the client makes:

```powershell
$httpClient.DefaultRequestHeaders.Authorization = [System.Net.Http.Headers.AuthenticationHeaderValue]::new('Bearer', $accessToken)
$httpClient.DefaultRequestHeaders.Add('X-CSRF-Header', '-')
```

Credentials: **Where the credentials come from.** `NetworkCredential` declares no scheme: the type serves integrated authentication, mail, file transfer and proxies. This finding is reported because the credential is bound to an HTTP client or handler; **it may not be Basic at all.** Establish that, and the destination, before changing it.

**Common to both variants**

**Sign in.** Replace the code that collected the user's credentials with a redirect: send the user's browser to the authorize endpoint `<host>/Relativity/Identity/connect/authorize`, which leads to the RelativityOne login page. The authorize endpoint is **verified by NSerio against a live instance; not documented by Relativity**. The request carries the standard authorization-code parameters — `client_id`, `response_type=code`, the registered `redirect_uri`, a `scope`, and a `state` value the script checks on return; these are standard OAuth, and Relativity documents none of them.

**Receive the code.** The browser returns to the Redirect URL registered on the OAuth2 Client, with the authorization code and the `state` value. Check the `state` before using the code.

**Exchange the code.** Send a form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=authorization_code`, the `code`, the same `redirect_uri`, the `client_id` and the `client_secret`, plus the `code_verifier` when PKCE is used. The exchange is **verified by NSerio against a live instance; not documented by Relativity**.

**Use the token.** Send `Authorization: Bearer <access token>` — the change at the use site in part 1 — with `X-CSRF-Header: -` on REST and Kepler calls.

**On expiry.** With Allow Refresh Tokens enabled on the OAuth2 Client and PKCE used, the script holds a refresh token and, when the access token expires, sends a `grant_type=refresh_token` request with it in place of the code exchange — *inferred* from the standard OAuth shape, not tested. Without a refresh token the user signs in again. Relativity does not document a refresh token's lifetime, whether it rotates, or how it is revoked. A refresh token is a secret: store it as Secret Management (Part B of the Migration Documentation) describes, not in a plain file.

**PKCE.** PKCE is required by Relativity's own documentation to obtain a refresh token, and not otherwise: a Code request that does not ask for a refresh token works without it, **verified by NSerio against a live instance; not documented by Relativity**. NSerio recommends PKCE with the `S256` method (NSerio recommendation), and does not claim that Relativity accepts any particular method, which it does not document.

**Scope.** No scope set is prescribed for Code. The set NSerio used works, **verified by NSerio against a live instance; not documented by Relativity**, verified as working and not as minimal.

**The script stops collecting the password.** After this change the script never receives, stores or forwards the user's user name and password: remove the prompt (`Get-Credential`, `Read-Host`) and any storage of the password.

**Single sign-on.** Sign-in happens on RelativityOne's own login page, so the instance's single sign-on and two-factor rules apply unchanged — *inferred* from where the sign-in happens, and not documented for clients that customers create.

**The client secret is required.** Code does not work without a client secret: tested and rejected, **verified by NSerio against a live instance; not documented by Relativity**, which confirms Relativity's own statement that the grant requires one. It is displayed once and is stored as Secret Management (Part B of the Migration Documentation) describes.

**Server-side variant.** For a script that runs on a server and receives the callback on a route of an application: one exact HTTPS Redirect URL per environment, no wildcards, registered by the Relativity administrator (NSerio recommendation; Relativity's matching rules are not documented); the secret and the user's tokens stay on the server. PowerShell is rarely the host of such a route; the server-side variant of the other languages applies unchanged.

**Desktop variant.** For a script a person runs on their own machine:

- **The system browser.** Open the authorize URL in the default browser, not in a web view the script hosts, so the script never sees the password (NSerio recommendation).
- **A loopback redirect on a fixed port.** Listen on the loopback address only, on the registered port, for one request. The Redirect URL is `http://127.0.0.1:<port>/<path>`: a plain-HTTP loopback URL is accepted, **verified by NSerio against a live instance; not documented by Relativity**. The Relativity administrator registers that exact URL, so the port is fixed and not chosen at runtime. **`HttpListener` accepts only a prefix that ends in `/`**, so the script appends one to the prefix it listens on and leaves the registered Redirect URL as registered: a request for `<path>?code=…` is received by the prefix `<path>/` (*checked by NSerio locally*, Windows PowerShell 5.1 and PowerShell 7.6.6: `Prefixes.Add` rejects a prefix without the slash, and the prefix with it answered both `/<path>?code=…` and `/<path>/?code=…`). `HttpListener` starts on the loopback address from a non-elevated process on both editions (*checked by NSerio locally*; a true standard account is not yet tested).
- **The secret is extractable.** Code requires a client secret, so a distributed script has to embed one, and **a script is text: anyone who can read the file has the secret.** It protects nothing against the person who runs the script.
- **Compensating guidance** (NSerio recommendation, subject to your own security policy): set Trusted IPs where outbound addresses are known and stable — they restrict where a token can be *requested* — use the recommended short Access Token Lifetime of 60 minutes, enable refresh tokens only when needed, and create one OAuth2 Client per distribution.
- **A known issue.** Relativity documents no recommendation for a distributed desktop application; this design is what NSerio verified works.

```powershell
# Windows PowerShell 5.1 and PowerShell 7.x. Executed against a local mock server on both, not against a live instance.
# $redirectUri is the exact URL registered on the OAuth2 Client, with a fixed port.
$state = [guid]::NewGuid().ToString('N')
$generator = [System.Security.Cryptography.RandomNumberGenerator]::Create()
$verifierBytes = New-Object byte[] 32
$generator.GetBytes($verifierBytes)
$verifier = [Convert]::ToBase64String($verifierBytes).TrimEnd('=').Replace('+', '-').Replace('/', '_')
$hasher = [System.Security.Cryptography.SHA256]::Create()
$challengeBytes = $hasher.ComputeHash([Text.Encoding]::ASCII.GetBytes($verifier))
$challenge = [Convert]::ToBase64String($challengeBytes).TrimEnd('=').Replace('+', '-').Replace('/', '_')

$listener = New-Object System.Net.HttpListener
$listener.Prefixes.Add($redirectUri.TrimEnd('/') + '/')   # a prefix must end in '/'; the registered URL is unchanged
$listener.Start()
$authorizeUrl = "$instanceUrl/Relativity/Identity/connect/authorize?client_id=$clientId&response_type=code" +
	"&redirect_uri=$([uri]::EscapeDataString($redirectUri))&scope=$([uri]::EscapeDataString($scope))" +
	"&state=$state&code_challenge=$challenge&code_challenge_method=S256"
Start-Process $authorizeUrl   # on Linux or macOS under 7.x use the platform opener instead (inferred)
$context = $listener.GetContext()
$query = $context.Request.QueryString
$page = [Text.Encoding]::UTF8.GetBytes('You can close this window.')
$context.Response.Close($page, $true)   # sends the page and waits until it is written, before the listener stops
$listener.Stop()
if ($query['state'] -ne $state) { throw 'The state value does not match.' }
$tokenResponse = Invoke-RestMethod -Method Post -Uri "$instanceUrl/Relativity/Identity/connect/token" -Body @{
	grant_type    = 'authorization_code'
	code          = $query['code']
	redirect_uri  = $redirectUri
	client_id     = $clientId
	client_secret = $clientSecret
	code_verifier = $verifier
}
$accessToken = $tokenResponse.access_token
```

**Why this form.** The two short .NET helpers for the hash and for random bytes exist on 7.x only, so the script creates the objects and calls their instance methods, which run on both editions (documented .NET API).

Verification: **Verification note.** **Confirm that this client really sends Basic.** A handler holding a credential is documented to answer an authentication challenge (*inferred* for this construct); against a server that challenges with Bearer only, as RelativityOne does (probed for the web cmdlets, not for this type), it may send nothing. If it sends nothing, the script already authenticates by other means and this change is not needed. `X-CSRF-Header` is required; a token expires.

OAuth2 Client: Create or confirm this OAuth2 Client in RelativityOne (NSerio's observation: the menu path is Home > Authentication > OAuth2 Client Manager, as Relativity's REST API authentication page names it). This tool never connects to RelativityOne and never creates or changes the OAuth2 Client; the settings below are guidance.
- Name: a descriptive name identifying the application it serves.
- Flow Grant Type: Code
- Redirect URLs: Relativity documentation: each URL includes the http or https protocol prefix. The exact callback URL is registered by the Relativity administrator. Server-side web applications, NSerio's recommendation: one exact HTTPS URL per environment, with no wildcards. Desktop and CLI applications: a plain-HTTP loopback URL, `http://127.0.0.1:<port>/<path>`, on a fixed port because the URL is registered in advance and the port cannot be chosen at runtime (verified by NSerio against a live instance; not documented by Relativity).
- Access Token Lifetime: 60 minutes, stated in minutes because the field is in minutes. Relativity documentation: tokens for this flow must have a short lifetime, and the recommended value matches the Identity Server default.
- Allow refresh tokens: NSerio's recommendation: leave it off unless the application must keep a user session active beyond the access token lifetime without asking the user to sign in again. If it is on, the refresh tokens are stored as secrets, per Secret Management (Part B of the Migration Documentation).
- Enabled: on.
<!-- generated:end PS-009 authorization-code -->

### PS-010 · Base64UserPasswordPair

<a id="ps-010-migration"></a>

#### Client Credentials

<!-- generated:begin PS-010 client-credentials -->
**Confirming the flow.** When no signal in a file decides the flow, the tool assigns Client Credentials, the default, with this note: "Who supplies the credential at run time? If a person types it or signs in, the integration acts for that person: use Authorization Code. If it comes from configuration, settings, an environment variable or a secret store: use Client Credentials. No signal in this file showed which applies, so Client Credentials, the default, is recommended. Use Authorization Code instead if the integration acts on behalf of a signed-in user, or if your RelativityOne instance already has a Code-flow OAuth2 Client for this integration." A fixed-credential signal assigns it with this note: "A fixed-credential signal in this file points to Client Credentials, but how often that kind of signal is right has not been measured. Use Authorization Code instead if the integration acts on behalf of a signed-in user, or if your RelativityOne instance already has a Code-flow OAuth2 Client for this integration." See [Confirming the flow](#confirming-the-flow) and the "Which flow" paragraph of PS-010 in [Part A](part-a-powershell.md#ps-010).

Source form: **Editions:** Windows PowerShell 5.1 and PowerShell 7.x.

```powershell
$encoded = [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes("$($loginName):$($loginSecret)"))
```

Under token authentication: **First, the destination:** if this is not RelativityOne — including a Relativity Server instance, whose own documentation governs — this finding is informational and the rest of this template does not apply.

Delete the encoding. A user name and password are not exchanged on each call: obtain a token and send it.

```powershell
$headerTable = @{ Authorization = "Bearer $accessToken"; 'X-CSRF-Header' = '-' }
```

Credentials: **Where the credentials come from.** A colon-joined pair that is Base64-encoded is the Basic credential. If it is for RelativityOne, the OAuth2 Client replaces it and its secret is displayed once; the user name and password stop being used by this script. If the encoded pair is not a credential (a host and a port, a key and a value), this finding does not apply.

**Where the token comes from.** Create an OAuth2 Client with Flow Grant Type set to Client Credentials (part 3 lists its settings), then send a form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=client_credentials`, `client_id`, `client_secret` and a `scope`. The token endpoint is **documented** for this grant. The token is then sent as `Authorization: Bearer <token>`, with `X-CSRF-Header: -` on REST and Kepler calls — the header is documented as required, and a Kepler call without it returns 404.

```powershell
# Windows PowerShell 5.1 and PowerShell 7.x. $clientId and $clientSecret come from the process
# environment or a secret store (Secret Management (Part B of the Migration Documentation)), never from this script.
$tokenBody = @{
	grant_type    = 'client_credentials'
	client_id     = $clientId
	client_secret = $clientSecret
	scope         = $scope
}
$tokenResponse = Invoke-RestMethod -Method Post -Uri "$instanceUrl/Relativity/Identity/connect/token" -Body $tokenBody -ContentType 'application/x-www-form-urlencoded'
$accessToken = $tokenResponse.access_token
$headerTable = @{ Authorization = "Bearer $accessToken"; 'X-CSRF-Header' = '-' }
```

```powershell
# Windows PowerShell 5.1 only, before the first request, if the host does not already negotiate TLS 1.2
# (inferred from the .NET Framework default; the local mock-server run is plain http, so it does not exercise TLS).
[Net.ServicePointManager]::SecurityProtocol = [Net.ServicePointManager]::SecurityProtocol -bor [Net.SecurityProtocolType]::Tls12
```

```powershell
# PowerShell 7.x alternative to the header: the cmdlet sends the bearer token itself
# (checked by NSerio locally, 7.6.6: Authorization: Bearer is sent pre-emptively).
$secureToken = ConvertTo-SecureString $accessToken -AsPlainText -Force
Invoke-RestMethod -Uri $uri -Authentication Bearer -Token $secureToken -Headers @{ 'X-CSRF-Header' = '-' }
```

**The scope depends on what the integration needs, and no single scope is required.** Relativity's documentation shows two values for this grant: `SystemUserInfo` on the REST authentication page, the Usage Reports page and in the official Transfer SDK sample, and `UserInfoAccess` in the Kepler .NET samples. No page says which is correct, whether both work, or what either grants, and the OAuth2 Client's `Scopes` property is documented as one that cannot be set. Use the value the API you call documents, and confirm it.

**Cache the token, and re-acquire it on expiry — do not refresh.** Keep it in a script-scoped variable for its lifetime (part 3 recommends 60 minutes, Relativity's own wording), not on disk, and request a new one with the client secret when it expires or a call is rejected for authentication. Client Credentials offers no refresh tokens: **verified by NSerio against a live instance; not documented by Relativity** — the Allow Refresh Tokens control does not appear for this grant in the RelativityOne user interface.

**The client secret.** It is displayed once, at creation or reset. Store it as Secret Management (Part B of the Migration Documentation) describes; never in the script, never in tracked configuration. A credential saved with `Export-Clixml` is bound to the user and machine that saved it on Windows and is not protected on Linux or macOS (documented by Microsoft). A reset invalidates the previous secret immediately, so plan the cut-over before resetting it.

**Before deploying, confirm the calling identity.** Every call now runs as the OAuth2 Client's **Context User**, not as the user in the replaced credential. If that user has different workspace permissions or item-level security, the script returns fewer rows or `Unauthorized` with no change to its logic, and Relativity's audit attributes every call to that account. Confirm the Context User's permissions match those of the previous identity.

**When the integration acts for a signed-in user.** Client Credentials is one service identity by construction. That is the Authorization Code case, and moving to it means a new OAuth2 Client, because the flow is fixed when a client is created. The finding's flow note says when to make that choice.

Verification: **Verification note — two things.** **The pair may not be a credential**: the encoded value could be any colon-joined pair, a cost this tool declares. **The calling identity changes** with the flow chosen, as the token-acquisition part states.

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
<!-- generated:end PS-010 client-credentials -->

#### Authorization Code

<!-- generated:begin PS-010 authorization-code -->
**Confirming the flow.** An interactive-user signal in a file assigns Authorization Code, with this note: "An interactive-user signal in this file points to Authorization Code. Confirm the flow: use Client Credentials instead if the integration runs with credentials fixed for the integration and no signed-in user." Your user profile can also make Authorization Code the default; see Changing the default in [Confirming the flow](#confirming-the-flow). See [Confirming the flow](#confirming-the-flow) and the "Which flow" paragraph of PS-010 in [Part A](part-a-powershell.md#ps-010).

Source form: **Editions:** Windows PowerShell 5.1 and PowerShell 7.x.

```powershell
$encoded = [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes("$($loginName):$($loginSecret)"))
```

Under token authentication: **First, the destination:** if this is not RelativityOne — including a Relativity Server instance, whose own documentation governs — this finding is informational and the rest of this template does not apply.

Delete the encoding. A user name and password are not exchanged on each call: obtain a token and send it.

```powershell
$headerTable = @{ Authorization = "Bearer $accessToken"; 'X-CSRF-Header' = '-' }
```

Credentials: **Where the credentials come from.** A colon-joined pair that is Base64-encoded is the Basic credential. If it is for RelativityOne, the OAuth2 Client replaces it and its secret is displayed once; the user name and password stop being used by this script. If the encoded pair is not a credential (a host and a port, a key and a value), this finding does not apply.

**Common to both variants**

**Sign in.** Replace the code that collected the user's credentials with a redirect: send the user's browser to the authorize endpoint `<host>/Relativity/Identity/connect/authorize`, which leads to the RelativityOne login page. The authorize endpoint is **verified by NSerio against a live instance; not documented by Relativity**. The request carries the standard authorization-code parameters — `client_id`, `response_type=code`, the registered `redirect_uri`, a `scope`, and a `state` value the script checks on return; these are standard OAuth, and Relativity documents none of them.

**Receive the code.** The browser returns to the Redirect URL registered on the OAuth2 Client, with the authorization code and the `state` value. Check the `state` before using the code.

**Exchange the code.** Send a form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=authorization_code`, the `code`, the same `redirect_uri`, the `client_id` and the `client_secret`, plus the `code_verifier` when PKCE is used. The exchange is **verified by NSerio against a live instance; not documented by Relativity**.

**Use the token.** Send `Authorization: Bearer <access token>` — the change at the use site in part 1 — with `X-CSRF-Header: -` on REST and Kepler calls.

**On expiry.** With Allow Refresh Tokens enabled on the OAuth2 Client and PKCE used, the script holds a refresh token and, when the access token expires, sends a `grant_type=refresh_token` request with it in place of the code exchange — *inferred* from the standard OAuth shape, not tested. Without a refresh token the user signs in again. Relativity does not document a refresh token's lifetime, whether it rotates, or how it is revoked. A refresh token is a secret: store it as Secret Management (Part B of the Migration Documentation) describes, not in a plain file.

**PKCE.** PKCE is required by Relativity's own documentation to obtain a refresh token, and not otherwise: a Code request that does not ask for a refresh token works without it, **verified by NSerio against a live instance; not documented by Relativity**. NSerio recommends PKCE with the `S256` method (NSerio recommendation), and does not claim that Relativity accepts any particular method, which it does not document.

**Scope.** No scope set is prescribed for Code. The set NSerio used works, **verified by NSerio against a live instance; not documented by Relativity**, verified as working and not as minimal.

**The script stops collecting the password.** After this change the script never receives, stores or forwards the user's user name and password: remove the prompt (`Get-Credential`, `Read-Host`) and any storage of the password.

**Single sign-on.** Sign-in happens on RelativityOne's own login page, so the instance's single sign-on and two-factor rules apply unchanged — *inferred* from where the sign-in happens, and not documented for clients that customers create.

**The client secret is required.** Code does not work without a client secret: tested and rejected, **verified by NSerio against a live instance; not documented by Relativity**, which confirms Relativity's own statement that the grant requires one. It is displayed once and is stored as Secret Management (Part B of the Migration Documentation) describes.

**Server-side variant.** For a script that runs on a server and receives the callback on a route of an application: one exact HTTPS Redirect URL per environment, no wildcards, registered by the Relativity administrator (NSerio recommendation; Relativity's matching rules are not documented); the secret and the user's tokens stay on the server. PowerShell is rarely the host of such a route; the server-side variant of the other languages applies unchanged.

**Desktop variant.** For a script a person runs on their own machine:

- **The system browser.** Open the authorize URL in the default browser, not in a web view the script hosts, so the script never sees the password (NSerio recommendation).
- **A loopback redirect on a fixed port.** Listen on the loopback address only, on the registered port, for one request. The Redirect URL is `http://127.0.0.1:<port>/<path>`: a plain-HTTP loopback URL is accepted, **verified by NSerio against a live instance; not documented by Relativity**. The Relativity administrator registers that exact URL, so the port is fixed and not chosen at runtime. **`HttpListener` accepts only a prefix that ends in `/`**, so the script appends one to the prefix it listens on and leaves the registered Redirect URL as registered: a request for `<path>?code=…` is received by the prefix `<path>/` (*checked by NSerio locally*, Windows PowerShell 5.1 and PowerShell 7.6.6: `Prefixes.Add` rejects a prefix without the slash, and the prefix with it answered both `/<path>?code=…` and `/<path>/?code=…`). `HttpListener` starts on the loopback address from a non-elevated process on both editions (*checked by NSerio locally*; a true standard account is not yet tested).
- **The secret is extractable.** Code requires a client secret, so a distributed script has to embed one, and **a script is text: anyone who can read the file has the secret.** It protects nothing against the person who runs the script.
- **Compensating guidance** (NSerio recommendation, subject to your own security policy): set Trusted IPs where outbound addresses are known and stable — they restrict where a token can be *requested* — use the recommended short Access Token Lifetime of 60 minutes, enable refresh tokens only when needed, and create one OAuth2 Client per distribution.
- **A known issue.** Relativity documents no recommendation for a distributed desktop application; this design is what NSerio verified works.

```powershell
# Windows PowerShell 5.1 and PowerShell 7.x. Executed against a local mock server on both, not against a live instance.
# $redirectUri is the exact URL registered on the OAuth2 Client, with a fixed port.
$state = [guid]::NewGuid().ToString('N')
$generator = [System.Security.Cryptography.RandomNumberGenerator]::Create()
$verifierBytes = New-Object byte[] 32
$generator.GetBytes($verifierBytes)
$verifier = [Convert]::ToBase64String($verifierBytes).TrimEnd('=').Replace('+', '-').Replace('/', '_')
$hasher = [System.Security.Cryptography.SHA256]::Create()
$challengeBytes = $hasher.ComputeHash([Text.Encoding]::ASCII.GetBytes($verifier))
$challenge = [Convert]::ToBase64String($challengeBytes).TrimEnd('=').Replace('+', '-').Replace('/', '_')

$listener = New-Object System.Net.HttpListener
$listener.Prefixes.Add($redirectUri.TrimEnd('/') + '/')   # a prefix must end in '/'; the registered URL is unchanged
$listener.Start()
$authorizeUrl = "$instanceUrl/Relativity/Identity/connect/authorize?client_id=$clientId&response_type=code" +
	"&redirect_uri=$([uri]::EscapeDataString($redirectUri))&scope=$([uri]::EscapeDataString($scope))" +
	"&state=$state&code_challenge=$challenge&code_challenge_method=S256"
Start-Process $authorizeUrl   # on Linux or macOS under 7.x use the platform opener instead (inferred)
$context = $listener.GetContext()
$query = $context.Request.QueryString
$page = [Text.Encoding]::UTF8.GetBytes('You can close this window.')
$context.Response.Close($page, $true)   # sends the page and waits until it is written, before the listener stops
$listener.Stop()
if ($query['state'] -ne $state) { throw 'The state value does not match.' }
$tokenResponse = Invoke-RestMethod -Method Post -Uri "$instanceUrl/Relativity/Identity/connect/token" -Body @{
	grant_type    = 'authorization_code'
	code          = $query['code']
	redirect_uri  = $redirectUri
	client_id     = $clientId
	client_secret = $clientSecret
	code_verifier = $verifier
}
$accessToken = $tokenResponse.access_token
```

**Why this form.** The two short .NET helpers for the hash and for random bytes exist on 7.x only, so the script creates the objects and calls their instance methods, which run on both editions (documented .NET API).

Verification: **Verification note — two things.** **The pair may not be a credential**: the encoded value could be any colon-joined pair, a cost this tool declares. **The calling identity changes** with the flow chosen, as the token-acquisition part states.

OAuth2 Client: Create or confirm this OAuth2 Client in RelativityOne (NSerio's observation: the menu path is Home > Authentication > OAuth2 Client Manager, as Relativity's REST API authentication page names it). This tool never connects to RelativityOne and never creates or changes the OAuth2 Client; the settings below are guidance.
- Name: a descriptive name identifying the application it serves.
- Flow Grant Type: Code
- Redirect URLs: Relativity documentation: each URL includes the http or https protocol prefix. The exact callback URL is registered by the Relativity administrator. Server-side web applications, NSerio's recommendation: one exact HTTPS URL per environment, with no wildcards. Desktop and CLI applications: a plain-HTTP loopback URL, `http://127.0.0.1:<port>/<path>`, on a fixed port because the URL is registered in advance and the port cannot be chosen at runtime (verified by NSerio against a live instance; not documented by Relativity).
- Access Token Lifetime: 60 minutes, stated in minutes because the field is in minutes. Relativity documentation: tokens for this flow must have a short lifetime, and the recommended value matches the Identity Server default.
- Allow refresh tokens: NSerio's recommendation: leave it off unless the application must keep a user session active beyond the access token lifetime without asking the user to sign in again. If it is on, the refresh tokens are stored as secrets, per Secret Management (Part B of the Migration Documentation).
- Enabled: on.
<!-- generated:end PS-010 authorization-code -->

### PS-011 · TokenCarriedUnderBasicScheme

<a id="ps-011-migration"></a>

#### Client Credentials

<!-- generated:begin PS-011 client-credentials -->
**Confirming the flow.** When no signal in a file decides the flow, the tool assigns Client Credentials, the default, with this note: "Who supplies the credential at run time? If a person types it or signs in, the integration acts for that person: use Authorization Code. If it comes from configuration, settings, an environment variable or a secret store: use Client Credentials. No signal in this file showed which applies, so Client Credentials, the default, is recommended. Use Authorization Code instead if the integration acts on behalf of a signed-in user, or if your RelativityOne instance already has a Code-flow OAuth2 Client for this integration." A fixed-credential signal assigns it with this note: "A fixed-credential signal in this file points to Client Credentials, but how often that kind of signal is right has not been measured. Use Authorization Code instead if the integration acts on behalf of a signed-in user, or if your RelativityOne instance already has a Code-flow OAuth2 Client for this integration." See [Confirming the flow](#confirming-the-flow) and the "Which flow" paragraph of PS-011 in [Part A](part-a-powershell.md#ps-011).

Source form: **Editions:** Windows PowerShell 5.1 and PowerShell 7.x.

```powershell
$pair = ":" + $accessToken
$encoded = [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes($pair))
```

Under token authentication: A token is being sent under the `Basic` prefix. The migration exchanges no credential and does not depend on the destination being RelativityOne: **delete the Base64 step and change the prefix to `Bearer`**.

```powershell
$headerTable = @{ Authorization = "Bearer $accessToken" }
```

Credentials: **Where the credentials come from.** The token's provenance depends on the destination. If the destination is RelativityOne, the token must be one that RelativityOne issued, from the token-acquisition part that follows; a token issued by another service is that service's own concern, and the prefix change applies to any destination that accepts `Bearer`.

**Where the token comes from.** Create an OAuth2 Client with Flow Grant Type set to Client Credentials (part 3 lists its settings), then send a form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=client_credentials`, `client_id`, `client_secret` and a `scope`. The token endpoint is **documented** for this grant. The token is then sent as `Authorization: Bearer <token>`, with `X-CSRF-Header: -` on REST and Kepler calls — the header is documented as required, and a Kepler call without it returns 404.

```powershell
# Windows PowerShell 5.1 and PowerShell 7.x. $clientId and $clientSecret come from the process
# environment or a secret store (Secret Management (Part B of the Migration Documentation)), never from this script.
$tokenBody = @{
	grant_type    = 'client_credentials'
	client_id     = $clientId
	client_secret = $clientSecret
	scope         = $scope
}
$tokenResponse = Invoke-RestMethod -Method Post -Uri "$instanceUrl/Relativity/Identity/connect/token" -Body $tokenBody -ContentType 'application/x-www-form-urlencoded'
$accessToken = $tokenResponse.access_token
$headerTable = @{ Authorization = "Bearer $accessToken"; 'X-CSRF-Header' = '-' }
```

```powershell
# Windows PowerShell 5.1 only, before the first request, if the host does not already negotiate TLS 1.2
# (inferred from the .NET Framework default; the local mock-server run is plain http, so it does not exercise TLS).
[Net.ServicePointManager]::SecurityProtocol = [Net.ServicePointManager]::SecurityProtocol -bor [Net.SecurityProtocolType]::Tls12
```

```powershell
# PowerShell 7.x alternative to the header: the cmdlet sends the bearer token itself
# (checked by NSerio locally, 7.6.6: Authorization: Bearer is sent pre-emptively).
$secureToken = ConvertTo-SecureString $accessToken -AsPlainText -Force
Invoke-RestMethod -Uri $uri -Authentication Bearer -Token $secureToken -Headers @{ 'X-CSRF-Header' = '-' }
```

**The scope depends on what the integration needs, and no single scope is required.** Relativity's documentation shows two values for this grant: `SystemUserInfo` on the REST authentication page, the Usage Reports page and in the official Transfer SDK sample, and `UserInfoAccess` in the Kepler .NET samples. No page says which is correct, whether both work, or what either grants, and the OAuth2 Client's `Scopes` property is documented as one that cannot be set. Use the value the API you call documents, and confirm it.

**Cache the token, and re-acquire it on expiry — do not refresh.** Keep it in a script-scoped variable for its lifetime (part 3 recommends 60 minutes, Relativity's own wording), not on disk, and request a new one with the client secret when it expires or a call is rejected for authentication. Client Credentials offers no refresh tokens: **verified by NSerio against a live instance; not documented by Relativity** — the Allow Refresh Tokens control does not appear for this grant in the RelativityOne user interface.

**The client secret.** It is displayed once, at creation or reset. Store it as Secret Management (Part B of the Migration Documentation) describes; never in the script, never in tracked configuration. A credential saved with `Export-Clixml` is bound to the user and machine that saved it on Windows and is not protected on Linux or macOS (documented by Microsoft). A reset invalidates the previous secret immediately, so plan the cut-over before resetting it.

**Before deploying, confirm the calling identity.** Every call now runs as the OAuth2 Client's **Context User**, not as the user in the replaced credential. If that user has different workspace permissions or item-level security, the script returns fewer rows or `Unauthorized` with no change to its logic, and Relativity's audit attributes every call to that account. Confirm the Context User's permissions match those of the previous identity.

**When the integration acts for a signed-in user.** Client Credentials is one service identity by construction. That is the Authorization Code case, and moving to it means a new OAuth2 Client, because the flow is fixed when a client is created. The finding's flow note says when to make that choice.

Verification: **Verification note.** **Confirm the destination accepts `Bearer`** with that token. On RelativityOne, add `X-CSRF-Header: -` on REST and Kepler calls and expect the token to expire.

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
<!-- generated:end PS-011 client-credentials -->

#### Authorization Code

<!-- generated:begin PS-011 authorization-code -->
**Confirming the flow.** An interactive-user signal in a file assigns Authorization Code, with this note: "An interactive-user signal in this file points to Authorization Code. Confirm the flow: use Client Credentials instead if the integration runs with credentials fixed for the integration and no signed-in user." Your user profile can also make Authorization Code the default; see Changing the default in [Confirming the flow](#confirming-the-flow). See [Confirming the flow](#confirming-the-flow) and the "Which flow" paragraph of PS-011 in [Part A](part-a-powershell.md#ps-011).

Source form: **Editions:** Windows PowerShell 5.1 and PowerShell 7.x.

```powershell
$pair = ":" + $accessToken
$encoded = [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes($pair))
```

Under token authentication: A token is being sent under the `Basic` prefix. The migration exchanges no credential and does not depend on the destination being RelativityOne: **delete the Base64 step and change the prefix to `Bearer`**.

```powershell
$headerTable = @{ Authorization = "Bearer $accessToken" }
```

Credentials: **Where the credentials come from.** The token's provenance depends on the destination. If the destination is RelativityOne, the token must be one that RelativityOne issued, from the token-acquisition part that follows; a token issued by another service is that service's own concern, and the prefix change applies to any destination that accepts `Bearer`.

**Common to both variants**

**Sign in.** Replace the code that collected the user's credentials with a redirect: send the user's browser to the authorize endpoint `<host>/Relativity/Identity/connect/authorize`, which leads to the RelativityOne login page. The authorize endpoint is **verified by NSerio against a live instance; not documented by Relativity**. The request carries the standard authorization-code parameters — `client_id`, `response_type=code`, the registered `redirect_uri`, a `scope`, and a `state` value the script checks on return; these are standard OAuth, and Relativity documents none of them.

**Receive the code.** The browser returns to the Redirect URL registered on the OAuth2 Client, with the authorization code and the `state` value. Check the `state` before using the code.

**Exchange the code.** Send a form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=authorization_code`, the `code`, the same `redirect_uri`, the `client_id` and the `client_secret`, plus the `code_verifier` when PKCE is used. The exchange is **verified by NSerio against a live instance; not documented by Relativity**.

**Use the token.** Send `Authorization: Bearer <access token>` — the change at the use site in part 1 — with `X-CSRF-Header: -` on REST and Kepler calls.

**On expiry.** With Allow Refresh Tokens enabled on the OAuth2 Client and PKCE used, the script holds a refresh token and, when the access token expires, sends a `grant_type=refresh_token` request with it in place of the code exchange — *inferred* from the standard OAuth shape, not tested. Without a refresh token the user signs in again. Relativity does not document a refresh token's lifetime, whether it rotates, or how it is revoked. A refresh token is a secret: store it as Secret Management (Part B of the Migration Documentation) describes, not in a plain file.

**PKCE.** PKCE is required by Relativity's own documentation to obtain a refresh token, and not otherwise: a Code request that does not ask for a refresh token works without it, **verified by NSerio against a live instance; not documented by Relativity**. NSerio recommends PKCE with the `S256` method (NSerio recommendation), and does not claim that Relativity accepts any particular method, which it does not document.

**Scope.** No scope set is prescribed for Code. The set NSerio used works, **verified by NSerio against a live instance; not documented by Relativity**, verified as working and not as minimal.

**The script stops collecting the password.** After this change the script never receives, stores or forwards the user's user name and password: remove the prompt (`Get-Credential`, `Read-Host`) and any storage of the password.

**Single sign-on.** Sign-in happens on RelativityOne's own login page, so the instance's single sign-on and two-factor rules apply unchanged — *inferred* from where the sign-in happens, and not documented for clients that customers create.

**The client secret is required.** Code does not work without a client secret: tested and rejected, **verified by NSerio against a live instance; not documented by Relativity**, which confirms Relativity's own statement that the grant requires one. It is displayed once and is stored as Secret Management (Part B of the Migration Documentation) describes.

**Server-side variant.** For a script that runs on a server and receives the callback on a route of an application: one exact HTTPS Redirect URL per environment, no wildcards, registered by the Relativity administrator (NSerio recommendation; Relativity's matching rules are not documented); the secret and the user's tokens stay on the server. PowerShell is rarely the host of such a route; the server-side variant of the other languages applies unchanged.

**Desktop variant.** For a script a person runs on their own machine:

- **The system browser.** Open the authorize URL in the default browser, not in a web view the script hosts, so the script never sees the password (NSerio recommendation).
- **A loopback redirect on a fixed port.** Listen on the loopback address only, on the registered port, for one request. The Redirect URL is `http://127.0.0.1:<port>/<path>`: a plain-HTTP loopback URL is accepted, **verified by NSerio against a live instance; not documented by Relativity**. The Relativity administrator registers that exact URL, so the port is fixed and not chosen at runtime. **`HttpListener` accepts only a prefix that ends in `/`**, so the script appends one to the prefix it listens on and leaves the registered Redirect URL as registered: a request for `<path>?code=…` is received by the prefix `<path>/` (*checked by NSerio locally*, Windows PowerShell 5.1 and PowerShell 7.6.6: `Prefixes.Add` rejects a prefix without the slash, and the prefix with it answered both `/<path>?code=…` and `/<path>/?code=…`). `HttpListener` starts on the loopback address from a non-elevated process on both editions (*checked by NSerio locally*; a true standard account is not yet tested).
- **The secret is extractable.** Code requires a client secret, so a distributed script has to embed one, and **a script is text: anyone who can read the file has the secret.** It protects nothing against the person who runs the script.
- **Compensating guidance** (NSerio recommendation, subject to your own security policy): set Trusted IPs where outbound addresses are known and stable — they restrict where a token can be *requested* — use the recommended short Access Token Lifetime of 60 minutes, enable refresh tokens only when needed, and create one OAuth2 Client per distribution.
- **A known issue.** Relativity documents no recommendation for a distributed desktop application; this design is what NSerio verified works.

```powershell
# Windows PowerShell 5.1 and PowerShell 7.x. Executed against a local mock server on both, not against a live instance.
# $redirectUri is the exact URL registered on the OAuth2 Client, with a fixed port.
$state = [guid]::NewGuid().ToString('N')
$generator = [System.Security.Cryptography.RandomNumberGenerator]::Create()
$verifierBytes = New-Object byte[] 32
$generator.GetBytes($verifierBytes)
$verifier = [Convert]::ToBase64String($verifierBytes).TrimEnd('=').Replace('+', '-').Replace('/', '_')
$hasher = [System.Security.Cryptography.SHA256]::Create()
$challengeBytes = $hasher.ComputeHash([Text.Encoding]::ASCII.GetBytes($verifier))
$challenge = [Convert]::ToBase64String($challengeBytes).TrimEnd('=').Replace('+', '-').Replace('/', '_')

$listener = New-Object System.Net.HttpListener
$listener.Prefixes.Add($redirectUri.TrimEnd('/') + '/')   # a prefix must end in '/'; the registered URL is unchanged
$listener.Start()
$authorizeUrl = "$instanceUrl/Relativity/Identity/connect/authorize?client_id=$clientId&response_type=code" +
	"&redirect_uri=$([uri]::EscapeDataString($redirectUri))&scope=$([uri]::EscapeDataString($scope))" +
	"&state=$state&code_challenge=$challenge&code_challenge_method=S256"
Start-Process $authorizeUrl   # on Linux or macOS under 7.x use the platform opener instead (inferred)
$context = $listener.GetContext()
$query = $context.Request.QueryString
$page = [Text.Encoding]::UTF8.GetBytes('You can close this window.')
$context.Response.Close($page, $true)   # sends the page and waits until it is written, before the listener stops
$listener.Stop()
if ($query['state'] -ne $state) { throw 'The state value does not match.' }
$tokenResponse = Invoke-RestMethod -Method Post -Uri "$instanceUrl/Relativity/Identity/connect/token" -Body @{
	grant_type    = 'authorization_code'
	code          = $query['code']
	redirect_uri  = $redirectUri
	client_id     = $clientId
	client_secret = $clientSecret
	code_verifier = $verifier
}
$accessToken = $tokenResponse.access_token
```

**Why this form.** The two short .NET helpers for the hash and for random bytes exist on 7.x only, so the script creates the objects and calls their instance methods, which run on both editions (documented .NET API).

Verification: **Verification note.** **Confirm the destination accepts `Bearer`** with that token. On RelativityOne, add `X-CSRF-Header: -` on REST and Kepler calls and expect the token to expire.

OAuth2 Client: Create or confirm this OAuth2 Client in RelativityOne (NSerio's observation: the menu path is Home > Authentication > OAuth2 Client Manager, as Relativity's REST API authentication page names it). This tool never connects to RelativityOne and never creates or changes the OAuth2 Client; the settings below are guidance.
- Name: a descriptive name identifying the application it serves.
- Flow Grant Type: Code
- Redirect URLs: Relativity documentation: each URL includes the http or https protocol prefix. The exact callback URL is registered by the Relativity administrator. Server-side web applications, NSerio's recommendation: one exact HTTPS URL per environment, with no wildcards. Desktop and CLI applications: a plain-HTTP loopback URL, `http://127.0.0.1:<port>/<path>`, on a fixed port because the URL is registered in advance and the port cannot be chosen at runtime (verified by NSerio against a live instance; not documented by Relativity).
- Access Token Lifetime: 60 minutes, stated in minutes because the field is in minutes. Relativity documentation: tokens for this flow must have a short lifetime, and the recommended value matches the Identity Server default.
- Allow refresh tokens: NSerio's recommendation: leave it off unless the application must keep a user session active beyond the access token lifetime without asking the user to sign in again. If it is on, the refresh tokens are stored as secrets, per Secret Management (Part B of the Migration Documentation).
- Enabled: on.
<!-- generated:end PS-011 authorization-code -->

## Generic suggestion

The tool gives a generic recommendation when a match is reported at High or Medium confidence but no pattern-specific entry applies. It is shorter than a class entry: it does not show a before-and-after form, because the construct was not characterized. Use it as a starting point, and read the Part A description of the pattern class if you can identify the construct.

### Client Credentials

<!-- generated:begin generic client-credentials -->
**Confirming the flow.** When no signal in a file decides the flow, the tool assigns Client Credentials, the default, with this note: "Who supplies the credential at run time? If a person types it or signs in, the integration acts for that person: use Authorization Code. If it comes from configuration, settings, an environment variable or a secret store: use Client Credentials. No signal in this file showed which applies, so Client Credentials, the default, is recommended. Use Authorization Code instead if the integration acts on behalf of a signed-in user, or if your RelativityOne instance already has a Code-flow OAuth2 Client for this integration." A fixed-credential signal assigns it with this note: "A fixed-credential signal in this file points to Client Credentials, but how often that kind of signal is right has not been measured. Use Authorization Code instead if the integration acts on behalf of a signed-in user, or if your RelativityOne instance already has a Code-flow OAuth2 Client for this integration." See [Confirming the flow](#confirming-the-flow).

The tool reports Basic Authentication at this location but did not recognize the construct, so the change has not been characterized and no before-and-after form is offered here. If the destination is not RelativityOne, Relativity's identity endpoint does not issue tokens for it: the migration is that service's own, and this finding is informational. If the destination is RelativityOne, Basic Authentication is no longer documented for its REST API. The documented destination in PowerShell is a bearer `Authorization` header on the request, or, where the script loads a Relativity .NET SDK assembly, the SDK's bearer-token credential type; either carries a token obtained from the platform's identity endpoint with the client-credentials grant. The client ID and secret that obtain that token belong in the process environment or a secret store, per Secret Management (Part B of the Migration Documentation), and never in the script or in tracked configuration. Beyond those facts this is a pointer and not an instruction: the migration must be designed from the code by whoever owns it, rather than applied from this text.

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

The tool reports Basic Authentication at this location but did not recognize the construct, so the change has not been characterized and no before-and-after form is offered here. If the destination is not RelativityOne, Relativity's identity endpoint does not issue tokens for it: the migration is that service's own, and this finding is informational. If the destination is RelativityOne, Basic Authentication is no longer documented for its REST API, and this finding is assigned the Authorization Code flow, in which the script sends the user to RelativityOne to sign in, receives an authorization code at a Redirect URL registered on its OAuth2 Client, and exchanges the code for an access token that it sends as a Bearer token. The authorize endpoint is `<host>/Relativity/Identity/connect/authorize` and the code is exchanged at `<host>/Relativity/Identity/connect/token`; both are verified by NSerio against a live instance; not documented by Relativity. PKCE is required by Relativity's own documentation to obtain a refresh token, and not otherwise. The client ID and secret belong in the process environment or a secret store, per Secret Management (Part B of the Migration Documentation), and never in the script or in tracked configuration; Code requires a client secret, and a secret in a distributed script can be read by anyone who holds it. After the change the script no longer collects or forwards the user's password. Beyond those facts this is a pointer and not an instruction: the migration must be designed from the code by whoever owns it, rather than applied from this text.

OAuth2 Client: Create or confirm this OAuth2 Client in RelativityOne (NSerio's observation: the menu path is Home > Authentication > OAuth2 Client Manager, as Relativity's REST API authentication page names it). This tool never connects to RelativityOne and never creates or changes the OAuth2 Client; the settings below are guidance.
- Name: a descriptive name identifying the application it serves.
- Flow Grant Type: Code
- Redirect URLs: Relativity documentation: each URL includes the http or https protocol prefix. The exact callback URL is registered by the Relativity administrator. Server-side web applications, NSerio's recommendation: one exact HTTPS URL per environment, with no wildcards. Desktop and CLI applications: a plain-HTTP loopback URL, `http://127.0.0.1:<port>/<path>`, on a fixed port because the URL is registered in advance and the port cannot be chosen at runtime (verified by NSerio against a live instance; not documented by Relativity).
- Access Token Lifetime: 60 minutes, stated in minutes because the field is in minutes. Relativity documentation: tokens for this flow must have a short lifetime, and the recommended value matches the Identity Server default.
- Allow refresh tokens: NSerio's recommendation: leave it off unless the application must keep a user session active beyond the access token lifetime without asking the user to sign in again. If it is on, the refresh tokens are stored as secrets, per Secret Management (Part B of the Migration Documentation).
- Enabled: on.
<!-- generated:end generic authorization-code -->
