using System.Collections.Frozen;

using MigrateAuth.Cli.Findings;
using MigrateAuth.Cli.Reporting;

namespace MigrateAuth.Cli.Catalog.Languages;

/// <summary>
/// The recommendation text for `Shell`: eighteen classes, both flows and both generic fallback texts. The seven classes that are Low by definition (the
/// calls that send Basic only after the server challenges for it, and the helper definition) carry no entry: they are routed to manual review, and their
/// reason sentences are the shared Low vocabulary's.
///
/// HOW A RECOMMENDATION IS COMPOSED. Part 1 is the class's four fields and is flow-neutral, so the source form, the target form and the verification note are
/// the same text for both flows. The credential provenance is the class's own share followed by part 2 for the finding's flow, held once here as
/// <see cref="CLIENT_CREDENTIALS_TOKEN_ACQUISITION"/> and <see cref="AUTHORIZATION_CODE_TOKEN_ACQUISITION"/>. Part 3, the OAuth2 Client block, is appended by the
/// shared engine's composer and is not repeated here.
///
/// ENCODING CHOICES. A fact is labelled in words. Every secret-handling sentence names
/// <see cref="SecretManagementPointer.PART_B_NAME"/>, and the label of a fact NSerio verified but Relativity does not document is
/// <see cref="OAuth2ClientConfigurationBlocks.NSERIO_VERIFIED_LABEL"/>, both through the constants the shared engine already owns. Every block is fenced
/// <c>sh</c> where it is POSIX and <c>bash</c> where it needs <c>bash</c>, and is indented with tabs only. A template never names a Relativity fact it cannot
/// state in words; the destination is always asked first, because the engine cannot see which host a script calls.
///
/// A SEPARATE FILE BECAUSE <c>ShellAnalyzer</c> IS <c>file</c>-SCOPED: its type cannot be split across files, so the text lives in this <c>internal</c> holder and
/// the analyzer's template provider reads it by the same identifiers.
/// </summary>
internal static class ShellRecommendationTemplates
{
	private const string _APPLIES_TO_ALL_SHELLS = "**Applies to:** `bash`, `sh`, `zsh` and `ksh`";

	private const string _DESTINATION_QUESTION =
		"**First, the destination:** if this Relativity instance is not RelativityOne — including a Relativity Server instance, whose own documentation governs — this finding is informational and the rest of this template does not apply.";

	private const string _TOKEN_LIFETIME = "**A token has a finite lifetime.**";

	private const string _ROUTE_BY_DESTINATION =
		"**Where the credentials come from.** The text establishes that a credential is sent as Basic, not that the destination is RelativityOne. If it is not RelativityOne, the migration is that service's own token mechanism, this finding is informational for this tool, and the reader stops here. If it is RelativityOne, the token comes from the token-acquisition part that follows, and the user name and password are replaced by the OAuth2 Client's client ID and secret.";

	private const string _BEARER_FROM_HEADER_FILE_CALL =
		"curl --silent --show-error --fail -H @\"${HEADER_FILE}\" \"${HOST}/Relativity.REST/api/...\"";

	private const string _BEARER_FROM_VARIABLE_CALL =
		"curl --silent --show-error --fail -H \"Authorization: Bearer ${ACCESS_TOKEN}\" -H \"X-CSRF-Header: -\" \"${HOST}/Relativity.REST/api/...\"";

	private const string _FENCE_SH_OPEN = "```sh\n";
	private const string _FENCE_BASH_OPEN = "```bash\n";
	private const string _FENCE_CLOSE = "\n```";

	/// <summary>The Client Credentials generic fallback prose for a `Shell` finding, with the secret-handling sentence naming the Part B pointer.</summary>
	public const string SHELL_LANGUAGE_FALLBACK_TEXT =
		"The tool reports Basic Authentication at this location but did not recognize the construct, so the change has not been characterized and no before-and-after form is offered here. If the destination is not RelativityOne, Relativity's identity endpoint does not issue tokens for it: the migration is that service's own, and this finding is informational. If the destination is RelativityOne, Basic Authentication is no longer documented for its REST API. The documented destination in a shell script is a bearer `Authorization` header sent by an HTTP client such as `curl`, carrying a token obtained from the platform's identity endpoint with the client-credentials grant. The client ID and secret that obtain that token belong in a file created under `umask 077` or in the operating system's keychain, per " + SecretManagementPointer.PART_B_NAME + ", and never in the script, in a tracked `.env` file or on a command line, which in a shell estate includes `.netrc` and `.wgetrc`. Beyond those facts this is a pointer and not an instruction: the migration must be designed from the code by whoever owns it, rather than applied from this text.";

	/// <summary>The Authorization Code generic fallback prose for a `Shell` finding.</summary>
	public const string SHELL_CODE_LANGUAGE_FALLBACK_TEXT =
		"The tool reports Basic Authentication at this location but did not recognize the construct, so the change has not been characterized and no before-and-after form is offered here. If the destination is not RelativityOne, Relativity's identity endpoint does not issue tokens for it: the migration is that service's own, and this finding is informational. If the destination is RelativityOne, Basic Authentication is no longer documented for its REST API, and this finding is assigned the Authorization Code flow, in which the script sends the person to RelativityOne to sign in, receives an authorization code at a Redirect URL registered on its OAuth2 Client, and exchanges the code for an access token that it sends as a Bearer token. A shell has no listener of its own, so the simplest way to receive the code is for the person to copy the address the browser ends on after signing in and paste it to the script. The authorize endpoint is `<host>/Relativity/Identity/connect/authorize` and the code is exchanged at `<host>/Relativity/Identity/connect/token`; both are " + OAuth2ClientConfigurationBlocks.NSERIO_VERIFIED_LABEL + ". PKCE is required by Relativity's own documentation to obtain a refresh token, and not otherwise. The client ID and secret belong in a file created under `umask 077` or in the keychain, per " + SecretManagementPointer.PART_B_NAME + ", and never in the script or on a command line; Code requires a client secret. After the change the script no longer collects or forwards the person's password. Beyond those facts this is a pointer and not an instruction: the migration must be designed from the code by whoever owns it, rather than applied from this text.";

	/// <summary>The block that acquires a Client Credentials token: a function and one call of it.</summary>
	public const string CLIENT_CREDENTIALS_ACQUIRE_BLOCK =
		"# HOST, CLIENT_ID, TOKEN_SCOPE and CLIENT_SECRET_FILE come from the caller's environment; the secret file is created as " + SecretManagementPointer.PART_B_NAME + " describes.\n"
		+ "acquire_access_token() {\n"
		+ "\tcurl --silent --show-error --fail \\\n"
		+ "\t\t--data-urlencode \"grant_type=client_credentials\" \\\n"
		+ "\t\t--data-urlencode \"client_id=${CLIENT_ID}\" \\\n"
		+ "\t\t--data-urlencode \"client_secret@${CLIENT_SECRET_FILE}\" \\\n"
		+ "\t\t--data-urlencode \"scope=${TOKEN_SCOPE}\" \\\n"
		+ "\t\t\"${HOST}/Relativity/Identity/connect/token\" \\\n"
		+ "\t\t| jq -r '.access_token'\n"
		+ "}\n"
		+ "ACCESS_TOKEN=$(acquire_access_token)";

	/// <summary>The block that sends the Client Credentials token without putting it on the command line.</summary>
	public const string CLIENT_CREDENTIALS_USE_BLOCK =
		"( umask 077; printf 'Authorization: Bearer %s\\nX-CSRF-Header: -\\n' \"${ACCESS_TOKEN}\" > \"${HEADER_FILE}\" )\n"
		+ _BEARER_FROM_HEADER_FILE_CALL;

	/// <summary>Part 2 for Client Credentials: how the token is obtained and kept. Appended to every class's credential-provenance share for a Client Credentials finding.</summary>
	public const string CLIENT_CREDENTIALS_TOKEN_ACQUISITION =
		"**Where the token comes from.** Create an OAuth2 Client with Flow Grant Type set to Client Credentials (part 3 lists its settings), then send a form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=client_credentials`, `client_id`, `client_secret` and a `scope`. The token endpoint is **documented** for this grant. The token is then sent as `Authorization: Bearer <token>`, with `X-CSRF-Header: -` on REST and Kepler calls — the header is documented as required, and a Kepler call without it returns 404. " + _APPLIES_TO_ALL_SHELLS + "; the blocks need `curl` and `jq`, which is a stated prerequisite: no fallback that parses the JSON with a regular expression is recommended.\n\n"
		+ _FENCE_SH_OPEN + CLIENT_CREDENTIALS_ACQUIRE_BLOCK + _FENCE_CLOSE + "\n\n"
		+ "`--data-urlencode \"client_secret@FILE\"` reads the secret from a file and keeps it out of the command line (*checked by NSerio locally*: the file is read whole, a trailing line feed included, so create it with `printf '%s'`); `curl -K -` with the same setting on standard input also works (*checked by NSerio locally*). **The scope depends on what the integration needs, and no single scope is required.** Relativity's documentation shows two values for this grant: `SystemUserInfo` on the REST authentication page, the Usage Reports page and in the official Transfer SDK sample, and `UserInfoAccess` in the Kepler .NET samples. No page says which is correct, whether both work, or what either grants, and the OAuth2 Client's `Scopes` property is documented as one that cannot be set. Use the value the API you call documents, and confirm it.\n\n"
		+ "**Use the token without putting it on the command line.**\n\n"
		+ _FENCE_SH_OPEN + CLIENT_CREDENTIALS_USE_BLOCK + _FENCE_CLOSE + "\n\n"
		+ "`curl -H @FILE` sends the header lines of a file (*checked by NSerio locally* on `curl` 8.17.0 and 7.81.0; documented from `curl` 7.55). A token in `-H \"Authorization: Bearer $TOKEN\"` is visible to every user of the machine in `ps` while the command runs, so the file form is the recommended one for a shared host.\n\n"
		+ "**Cache the token, and re-acquire it on expiry — do not refresh.** Keep the token for its lifetime (part 3 recommends 60 minutes, Relativity's own wording) and request a new one with the client secret when it expires or a call is rejected for authentication. Client Credentials offers no refresh tokens: **" + OAuth2ClientConfigurationBlocks.NSERIO_VERIFIED_LABEL + "** — the Allow Refresh Tokens control does not appear for this grant in the RelativityOne user interface. Relativity's own tables and text on refresh tokens conflict with each other, so re-acquisition with the secret is the advice that holds whichever way they are read. A script that runs for longer than the token's lifetime must acquire again, not hold the first token.\n\n"
		+ "**The client secret.** It is displayed once, at creation or reset. Store it as " + SecretManagementPointer.PART_B_NAME + " describes; in a shell estate that means **a file created under `umask 077`** or the operating system's keychain, **never in the script, never in a tracked `.env` file, and never exported to child processes**. A reset invalidates the previous secret immediately, so plan the cut-over before resetting it.\n\n"
		+ "**Before deploying, confirm the calling identity.** Every call now runs as the OAuth2 Client's **Context User**, not as the user in the replaced credential. If that user has different workspace permissions or item-level security, the script returns fewer rows or `Unauthorized` in production with no change to its logic, and Relativity's audit attributes every call to that one account. Confirm the Context User's permissions match those of the previous identity.\n\n"
		+ "**When the script acts for a signed-in user.** Client Credentials is one service identity by construction. A script that prompts a person for a password and sends it is the Authorization Code case, and moving to it means a new OAuth2 Client, because the flow is fixed when a client is created. The finding's flow note says when to make that choice.";

	/// <summary>The block that makes the code verifier, its challenge and the state value.</summary>
	public const string AUTHORIZATION_CODE_PKCE_BLOCK =
		"code_verifier=$(openssl rand -base64 48 | tr -d '\\r\\n' | tr '+/' '-_' | tr -d '=' | cut -c1-64)\n"
		+ "code_challenge=$(printf '%s' \"${code_verifier}\" | openssl dgst -sha256 -binary | openssl base64 -A | tr '+/' '-_' | tr -d '=')\n"
		+ "state=$(openssl rand -hex 16 | tr -d '\\r')";

	/// <summary>The block that sends the person to the authorize address and reads back the address the browser ends on.</summary>
	public const string AUTHORIZATION_CODE_AUTHORIZE_BLOCK =
		"urlencode() (\n"
		+ "\tLC_ALL=C\n"
		+ "\tremaining=$1\n"
		+ "\twhile [ -n \"${remaining}\" ]; do\n"
		+ "\t\trest=${remaining#?}\n"
		+ "\t\tcharacter=${remaining%\"${rest}\"}\n"
		+ "\t\tcase ${character} in\n"
		+ "\t\t\t[A-Za-z0-9._~-]) printf '%s' \"${character}\" ;;\n"
		+ "\t\t\t*) printf '%%%02X' \"'${character}\" ;;\n"
		+ "\t\tesac\n"
		+ "\t\tremaining=${rest}\n"
		+ "\tdone\n"
		+ ")\n"
		+ "\n"
		+ "authorize_url=\"${HOST}/Relativity/Identity/connect/authorize?client_id=$(urlencode \"${CLIENT_ID}\")&response_type=code&redirect_uri=$(urlencode \"${REDIRECT_URI}\")&scope=$(urlencode \"${TOKEN_SCOPE}\")&state=${state}&code_challenge=${code_challenge}&code_challenge_method=S256\"\n"
		+ "printf 'Open this address in a browser and sign in:\\n%s\\n' \"${authorize_url}\"\n"
		+ "printf 'Then copy the whole address the browser ends on and paste it here: '\n"
		+ "IFS= read -r returned_url";

	/// <summary>The block that takes the code and the state out of the pasted address and checks the state, in a form every shell reads the same way.</summary>
	public const string AUTHORIZATION_CODE_PARSE_BLOCK =
		"query=${returned_url#*\\?}\n"
		+ "query=${query%%#*}\n"
		+ "returned_code=''\n"
		+ "returned_state=''\n"
		+ "remaining=${query}\n"
		+ "while [ -n \"${remaining}\" ]; do\n"
		+ "\tpair=${remaining%%'&'*}\n"
		+ "\tcase ${remaining} in\n"
		+ "\t\t*'&'*) remaining=${remaining#*'&'} ;;\n"
		+ "\t\t*) remaining='' ;;\n"
		+ "\tesac\n"
		+ "\tcase ${pair} in\n"
		+ "\t\tcode=*) returned_code=${pair#code=} ;;\n"
		+ "\t\tstate=*) returned_state=${pair#state=} ;;\n"
		+ "\tesac\n"
		+ "done\n"
		+ "[ \"${returned_state}\" = \"${state}\" ] || { printf 'The state does not match; stop.\\n' >&2; exit 1; }";

	/// <summary>The block that exchanges the code for an access token.</summary>
	public const string AUTHORIZATION_CODE_EXCHANGE_BLOCK =
		"token_response=$(curl --silent --show-error --fail \\\n"
		+ "\t--data \"grant_type=authorization_code\" \\\n"
		+ "\t--data \"code=${returned_code}\" \\\n"
		+ "\t--data-urlencode \"redirect_uri=${REDIRECT_URI}\" \\\n"
		+ "\t--data-urlencode \"client_id=${CLIENT_ID}\" \\\n"
		+ "\t--data-urlencode \"code_verifier=${code_verifier}\" \\\n"
		+ "\t--data-urlencode \"client_secret@${CLIENT_SECRET_FILE}\" \\\n"
		+ "\t\"${HOST}/Relativity/Identity/connect/token\")\n"
		+ "ACCESS_TOKEN=$(printf '%s' \"${token_response}\" | jq -r '.access_token')";

	/// <summary>Part 2 for Authorization Code: the common part and both variants. Appended to every class's credential-provenance share for an Authorization Code finding.</summary>
	public const string AUTHORIZATION_CODE_TOKEN_ACQUISITION =
		"**Common to both variants.** " + _APPLIES_TO_ALL_SHELLS + "; the blocks need `curl`, `openssl` and `jq`. The text has a common part and two variants of how the script receives the code: the **manual copy** (primary) and a **loopback listener** (optional).\n\n"
		+ "**Sign in.** Replace the code that collected the user's credentials with a redirect: send the user's browser to the authorize endpoint `<host>/Relativity/Identity/connect/authorize`, which leads to the RelativityOne login page. The authorize endpoint is **" + OAuth2ClientConfigurationBlocks.NSERIO_VERIFIED_LABEL + "**. The request carries the standard authorization-code parameters — `client_id`, `response_type=code`, the registered `redirect_uri`, a `scope`, and a `state` value the script checks on return, plus `code_challenge` and `code_challenge_method=S256` when PKCE is used; these are standard OAuth, and Relativity documents none of them. No discovery document is relied on.\n\n"
		+ "**PKCE.** PKCE is required by Relativity's own documentation to obtain a refresh token, and not otherwise: a Code request that does not ask for a refresh token works without it, **" + OAuth2ClientConfigurationBlocks.NSERIO_VERIFIED_LABEL + "**. It is never a blanket requirement. NSerio recommends PKCE with the `S256` method (NSerio recommendation), and does not claim that Relativity accepts any particular method, which it does not document. With `openssl`:\n\n"
		+ _FENCE_SH_OPEN + AUTHORIZATION_CODE_PKCE_BLOCK + _FENCE_CLOSE + "\n\n"
		+ "The pipeline gives a 64-character verifier and a 43-character challenge (*checked by NSerio locally* with OpenSSL 3.5.4 on Windows and OpenSSL 3.0.2 on Linux; the `openssl` that ships with macOS has not been checked).\n\n"
		+ "**Receive the code, by the manual copy (primary).** The browser returns to the Redirect URL registered on the OAuth2 Client with the authorization code and the `state` value. **The script prints the authorize address and the person opens it in any browser; when nothing listens at the registered loopback address the browser shows a connection error and leaves the full address, with `code` and `state`, in its address bar; the person copies that address and pastes it to the script.** This works headless, over SSH, on Linux and macOS, and needs no listener (NSerio recommendation; the browser's behaviour on an unreachable loopback address is *inferred* and has not been checked). The Redirect URL must still be exactly the one registered.\n\n"
		+ _FENCE_SH_OPEN + AUTHORIZATION_CODE_AUTHORIZE_BLOCK + _FENCE_CLOSE + "\n\n"
		+ _FENCE_SH_OPEN + AUTHORIZATION_CODE_PARSE_BLOCK + _FENCE_CLOSE + "\n\n"
		+ "The percent-encoding function and the address parsing give the same output in `bash`, `dash`, `zsh`, `ksh93` and `mksh` (*checked by NSerio locally*). **Check the `state` before using the code.** The code is already percent-encoded in the pasted address, so it is sent with `--data` below and not encoded again.\n\n"
		+ "**Exchange the code.** Send a form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=authorization_code`, the `code`, the same `redirect_uri`, the `client_id` and the `client_secret`, plus the `code_verifier` when PKCE is used. The code is exchanged at that endpoint, which Relativity documents for the Client Credentials grant only; the exchange is **" + OAuth2ClientConfigurationBlocks.NSERIO_VERIFIED_LABEL + "**.\n\n"
		+ _FENCE_SH_OPEN + AUTHORIZATION_CODE_EXCHANGE_BLOCK + _FENCE_CLOSE + "\n\n"
		+ "**Use the token.** Send `Authorization: Bearer <access token>` — the change at the use site above — with `X-CSRF-Header: -` on REST and Kepler calls, from a header file as for Client Credentials: `( umask 077; printf 'Authorization: Bearer %s\\nX-CSRF-Header: -\\n' \"${ACCESS_TOKEN}\" > \"${HEADER_FILE}\" )`, then `curl -H @\"${HEADER_FILE}\"`.\n\n"
		+ "**On expiry.** With Allow Refresh Tokens enabled on the OAuth2 Client and PKCE used, the script holds a refresh token and, when the access token expires, sends a `grant_type=refresh_token` request with it and the client credentials in place of the code exchange — *inferred* from the standard OAuth shape, not tested. Without a refresh token the person signs in again. Relativity does not document a refresh token's lifetime, whether it rotates, how it is revoked, or whether a revocation endpoint exists. **A refresh token is a secret:** keep it in a file created under `umask 077` or in the keychain, never on a command line.\n\n"
		+ "**Scope.** No scope set is prescribed for Code. The scope `openid RelativityRequestOrigin UserInfo UserInfoAccess AuthenticationID RelativityWeb` works, **" + OAuth2ClientConfigurationBlocks.NSERIO_VERIFIED_LABEL + "**, verified as working and not as minimal. `state` is sent as standard OAuth practice and `nonce` is not used; Relativity does not document whether it enforces either.\n\n"
		+ "**The script stops collecting the password.** After this change the script never receives, stores or forwards the user's user name and password: the user signs in on RelativityOne's own page, and the script holds only tokens. Remove the `read` that prompts for the password, the `stty -echo` around it and any storage of the password the replaced code needed.\n\n"
		+ "**Single sign-on.** Sign-in happens on RelativityOne's own login page, so the instance's single sign-on and two-factor rules apply unchanged — *inferred* from where the sign-in happens, and not documented for clients that customers create.\n\n"
		+ "**The client secret is required.** Code does not work without a client secret: tested and rejected, **" + OAuth2ClientConfigurationBlocks.NSERIO_VERIFIED_LABEL + "**, which confirms Relativity's own statement that the grant requires one. **A script is plain text, so a secret shipped inside a distributed script can be read by anyone who holds the script**; it protects nothing against the person running it. Store it as " + SecretManagementPointer.PART_B_NAME + " describes, in a file with the person's own permissions (`umask 077`) or in the keychain, and apply the compensating guidance below.\n\n"
		+ "**Loopback listener variant (optional, per system).** A shell has no HTTP listener of its own. Where a script runs on a machine with a browser and a registered fixed-port loopback Redirect URL `http://127.0.0.1:<port>/<path>` — a plain-HTTP loopback URL is accepted, **" + OAuth2ClientConfigurationBlocks.NSERIO_VERIFIED_LABEL + "** — it can start `nc` or `socat` bound to `127.0.0.1` only, for one request, to read the request line `GET /<path>?code=…&state=… HTTP/1.1` and answer with a short page: on Linux the OpenBSD `nc` does it with `nc -l 127.0.0.1 PORT` (*checked by NSerio locally*); the options differ between OpenBSD, BSD and macOS, GNU and traditional `nc` and `ncat`, and `socat`, and those have not been checked. Bind to loopback only, check the `state` (another local process can connect to the port), and fall back to the manual copy when the tool is missing or the machine is headless.\n\n"
		+ "**Compensating guidance** (NSerio recommendation, subject to your own security policy): set Trusted IPs where the users' outbound addresses are known and stable — they restrict where a token can be *requested* — use the recommended short Access Token Lifetime of 60 minutes, enable refresh tokens only when needed, and create one OAuth2 Client per distribution, so a leaked secret can be disabled without affecting the others. **A known issue.** Relativity documents no recommendation for a distributed script or desktop application; this design is what NSerio verified works.";

	/// <summary>SH-001 generic source form.</summary>
	public const string SH_001_SOURCE_FORM =
		_APPLIES_TO_ALL_SHELLS + "; `curl` (a `--user=` form needs `curl` 8.x).\n\n"
		+ _FENCE_SH_OPEN + "curl --silent --show-error -u \"${API_USER}:${API_PASSWORD}\" \"${HOST}/Relativity.REST/api/...\"" + _FENCE_CLOSE;

	/// <summary>SH-001 target form under token authentication.</summary>
	public const string SH_001_TARGET_FORM =
		_DESTINATION_QUESTION + "\n\n" + _FENCE_SH_OPEN + _BEARER_FROM_HEADER_FILE_CALL + _FENCE_CLOSE + "\n\nwhere `HEADER_FILE` holds `Authorization: Bearer <access token>` and `X-CSRF-Header: -` (see the token acquisition below). Remove `-u`, `--user` and `--basic`.";

	/// <summary>SH-001 class share of the credential provenance.</summary>
	public const string SH_001_CREDENTIAL_PROVENANCE =
		"**Where the credentials come from.** The command line establishes that a client sends Basic, not that the destination is RelativityOne. If it is not RelativityOne, the migration is that service's own token mechanism, this finding is informational for this tool, and the reader stops here. If it is RelativityOne, the token comes from the token-acquisition part that follows, and `API_USER` and `API_PASSWORD` are replaced by the OAuth2 Client's client ID and secret.";

	/// <summary>SH-001 verification note.</summary>
	public const string SH_001_VERIFICATION_NOTE =
		"**Verification note — three things, and the first can break working code.** " + _TOKEN_LIFETIME + " A script that obtains the token once and keeps it fails authentication after it expires; acquire again within the lifetime. **The identity changes**: calls run as the OAuth2 Client's Context User. **A value that is `-u \"$CREDS\"` or built from several variables** may hold a user name and a password, or a user name only (`curl` then prompts): after the change the prompt, if any, is gone. **Other uses may not appear as findings**: a `curl` called through a wrapper function with arguments, or through a name reached by indirection, is not read; one called through a same-file alias or command variable is.";

	/// <summary>SH-002 generic source form.</summary>
	public const string SH_002_SOURCE_FORM =
		_APPLIES_TO_ALL_SHELLS + "; `curl`.\n\n"
		+ _FENCE_SH_OPEN + "curl --silent --show-error -u \"${API_USER}\" \"${HOST}/Relativity.REST/api/...\"" + _FENCE_CLOSE;

	/// <summary>SH-002 target form under token authentication.</summary>
	public const string SH_002_TARGET_FORM =
		_DESTINATION_QUESTION + "\n\n" + _FENCE_SH_OPEN + _BEARER_FROM_HEADER_FILE_CALL + _FENCE_CLOSE + "\n\nThe source form makes `curl` prompt a person for the password and then send Basic; the target form sends a token and prompts nothing.";

	/// <summary>SH-002 class share of the credential provenance.</summary>
	public const string SH_002_CREDENTIAL_PROVENANCE =
		_ROUTE_BY_DESTINATION + " **Because a person types the password, this construct is also interactive flow evidence**: the finding is assigned the Authorization Code flow, in which the person signs in on RelativityOne's page.";

	/// <summary>SH-002 verification note.</summary>
	public const string SH_002_VERIFICATION_NOTE =
		"**Verification note.** " + _TOKEN_LIFETIME + " Acquire again within it. **If the person's own identity matters** — the calls must run as the person who typed the password — use the Authorization Code flow; Client Credentials runs every call as the OAuth2 Client's Context User.";

	/// <summary>SH-003 generic source form.</summary>
	public const string SH_003_SOURCE_FORM =
		_APPLIES_TO_ALL_SHELLS + "; `curl` (`-H`, `--header`) and `wget` (`--header`).\n\n"
		+ _FENCE_SH_OPEN + "curl --silent --show-error -H \"Authorization: Basic ${ENCODED}\" \"${HOST}/Relativity.REST/api/...\"" + _FENCE_CLOSE;

	/// <summary>SH-003 target form under token authentication.</summary>
	public const string SH_003_TARGET_FORM =
		_DESTINATION_QUESTION + "\n\n" + _FENCE_SH_OPEN + _BEARER_FROM_VARIABLE_CALL + _FENCE_CLOSE + "\n\nFor `wget`: `--header=\"Authorization: Bearer ${ACCESS_TOKEN}\" --header=\"X-CSRF-Header: -\"`. Remove the code that encoded the user name and password.";

	/// <summary>SH-003 class share of the credential provenance.</summary>
	public const string SH_003_CREDENTIAL_PROVENANCE =
		"**Where the credentials come from.** The header establishes that a Basic credential is sent, not that the destination is RelativityOne. If the destination is RelativityOne, the pair that was encoded is replaced by the token from the token-acquisition part that follows.";

	/// <summary>SH-003 verification note.</summary>
	public const string SH_003_VERIFICATION_NOTE =
		"**Verification note.** " + _TOKEN_LIFETIME + " **The scheme word changes from `Basic` to `Bearer`**: a `Bearer` header is not read by this tool again. On a shared host prefer `-H @FILE` to a token on the command line, because a token on the command line is visible in `ps` while the command runs.";

	/// <summary>SH-004 generic source form.</summary>
	public const string SH_004_SOURCE_FORM =
		_APPLIES_TO_ALL_SHELLS + "; `wget`.\n\n"
		+ _FENCE_SH_OPEN + "wget --auth-no-challenge --user=\"${API_USER}\" --password=\"${API_PASSWORD}\" \"${HOST}/Relativity.REST/api/...\"" + _FENCE_CLOSE;

	/// <summary>SH-004 target form under token authentication.</summary>
	public const string SH_004_TARGET_FORM =
		_DESTINATION_QUESTION + "\n\n" + _FENCE_SH_OPEN + "wget --header=\"Authorization: Bearer ${ACCESS_TOKEN}\" --header=\"X-CSRF-Header: -\" \"${HOST}/Relativity.REST/api/...\"" + _FENCE_CLOSE + "\n\nRemove `--auth-no-challenge`, `--user`, `--password` and any userinfo in the URL.";

	/// <summary>SH-004 class share of the credential provenance.</summary>
	public const string SH_004_CREDENTIAL_PROVENANCE = _ROUTE_BY_DESTINATION;

	/// <summary>SH-004 verification note.</summary>
	public const string SH_004_VERIFICATION_NOTE =
		"**Verification note.** " + _TOKEN_LIFETIME + " `wget` has no `-H @FILE` form, so the token is on the command line while `wget` runs, visible in `ps`: on a shared host use `curl` for this call, or a `wget` configuration file created under `umask 077` that holds the header (documented in the GNU Wget manual; not checked here).";

	/// <summary>SH-005 generic source form.</summary>
	public const string SH_005_SOURCE_FORM =
		_APPLIES_TO_ALL_SHELLS + "; `curl`.\n\n"
		+ _FENCE_SH_OPEN + "curl --silent --show-error \"https://${API_USER}:${API_PASSWORD}@host.example/Relativity.REST/api/...\"" + _FENCE_CLOSE;

	/// <summary>SH-005 target form under token authentication.</summary>
	public const string SH_005_TARGET_FORM =
		_DESTINATION_QUESTION + "\n\n" + _FENCE_SH_OPEN + "curl --silent --show-error --fail -H @\"${HEADER_FILE}\" \"https://host.example/Relativity.REST/api/...\"" + _FENCE_CLOSE + "\n\nRemove the userinfo from the URL: it is also written to logs and to the process list.";

	/// <summary>SH-005 class share of the credential provenance.</summary>
	public const string SH_005_CREDENTIAL_PROVENANCE = _ROUTE_BY_DESTINATION;

	/// <summary>SH-005 verification note.</summary>
	public const string SH_005_VERIFICATION_NOTE =
		"**Verification note.** " + _TOKEN_LIFETIME + " **A URL with userinfo is read from other places too**: a proxy or a log that recorded it holds the credential, and the credential must be rotated, not only removed.";

	/// <summary>SH-006 generic source form.</summary>
	public const string SH_006_SOURCE_FORM =
		_APPLIES_TO_ALL_SHELLS + "; `curl`.\n\n"
		+ _FENCE_SH_OPEN + "curl --silent --show-error --netrc \"${HOST}/Relativity.REST/api/...\"" + _FENCE_CLOSE;

	/// <summary>SH-006 target form under token authentication.</summary>
	public const string SH_006_TARGET_FORM =
		_DESTINATION_QUESTION + "\n\n" + _FENCE_SH_OPEN + _BEARER_FROM_HEADER_FILE_CALL + _FENCE_CLOSE + "\n\nRemove `-n`, `--netrc`, `--netrc-file` and `--netrc-optional`.";

	/// <summary>SH-006 class share of the credential provenance.</summary>
	public const string SH_006_CREDENTIAL_PROVENANCE =
		"**Where the credentials come from.** The `.netrc` entry holds the Basic credential this finding cannot see. If the destination is RelativityOne, replace it by the token of the token-acquisition part that follows, and delete the entry.";

	/// <summary>SH-006 verification note.</summary>
	public const string SH_006_VERIFICATION_NOTE =
		"**Verification note.** **The tool did not read the `.netrc` file**, so whether it holds a credential for RelativityOne is for the reader to check; `--netrc-optional` may find none. " + _TOKEN_LIFETIME;

	/// <summary>SH-007 generic source form.</summary>
	public const string SH_007_SOURCE_FORM =
		_APPLIES_TO_ALL_SHELLS + "; `base64` and `openssl` (their options differ by system).\n\n"
		+ _FENCE_SH_OPEN + "ENCODED=$(printf '%s:%s' \"${API_USER}\" \"${API_PASSWORD}\" | base64 | tr -d '\\n')" + _FENCE_CLOSE;

	/// <summary>SH-007 target form under token authentication.</summary>
	public const string SH_007_TARGET_FORM =
		_DESTINATION_QUESTION + " **If the encoded pair is not a credential** (an identifier and a version, for example; a host and a port is not read), this finding is a false positive and nothing changes.\n\nRemove the encoding and send the token: `-H \"Authorization: Bearer ${ACCESS_TOKEN}\"`, as for a header that names the scheme.";

	/// <summary>SH-007 class share of the credential provenance.</summary>
	public const string SH_007_CREDENTIAL_PROVENANCE = _ROUTE_BY_DESTINATION;

	/// <summary>SH-007 verification note.</summary>
	public const string SH_007_VERIFICATION_NOTE =
		"**Verification note.** **The tool saw an encoded colon pair that a client command uses, not the header's scheme**: follow `ENCODED` to where it is sent and change that use too; a header that names the scheme is reported separately as a Basic header. " + _TOKEN_LIFETIME;

	/// <summary>SH-008 generic source form.</summary>
	public const string SH_008_SOURCE_FORM =
		_APPLIES_TO_ALL_SHELLS + "; `curl`.\n\n"
		+ _FENCE_SH_OPEN + "AUTH=\"Basic ${ENCODED}\"\ncurl --silent --show-error -H \"Authorization: ${AUTH}\" \"${HOST}/Relativity.REST/api/...\"" + _FENCE_CLOSE;

	/// <summary>SH-008 target form under token authentication.</summary>
	public const string SH_008_TARGET_FORM =
		_DESTINATION_QUESTION + "\n\n" + _FENCE_SH_OPEN + "AUTH=\"Bearer ${ACCESS_TOKEN}\"\ncurl --silent --show-error --fail -H \"Authorization: ${AUTH}\" -H \"X-CSRF-Header: -\" \"${HOST}/Relativity.REST/api/...\"" + _FENCE_CLOSE;

	/// <summary>SH-008 class share of the credential provenance.</summary>
	public const string SH_008_CREDENTIAL_PROVENANCE = _ROUTE_BY_DESTINATION;

	/// <summary>SH-008 verification note.</summary>
	public const string SH_008_VERIFICATION_NOTE =
		"**Verification note.** **Other uses of the variable beyond thirty lines, or in another file, are not tracked**: change every place that sends it. " + _TOKEN_LIFETIME;

	/// <summary>SH-009 generic source form.</summary>
	public const string SH_009_SOURCE_FORM =
		"**Applies to:** **`bash`, `zsh` and `ksh` only** (POSIX `sh` has no arrays); `curl`, `wget`.\n\n"
		+ _FENCE_BASH_OPEN + "options=(-sS -u \"${API_USER}:${API_PASSWORD}\")\ncurl \"${options[@]}\" \"${HOST}/Relativity.REST/api/...\"" + _FENCE_CLOSE;

	/// <summary>SH-009 target form under token authentication.</summary>
	public const string SH_009_TARGET_FORM =
		_DESTINATION_QUESTION + "\n\n" + _FENCE_BASH_OPEN + "options=(-sS --fail -H \"Authorization: Bearer ${ACCESS_TOKEN}\" -H \"X-CSRF-Header: -\")\ncurl \"${options[@]}\" \"${HOST}/Relativity.REST/api/...\"" + _FENCE_CLOSE;

	/// <summary>SH-009 class share of the credential provenance.</summary>
	public const string SH_009_CREDENTIAL_PROVENANCE = _ROUTE_BY_DESTINATION;

	/// <summary>SH-009 verification note.</summary>
	public const string SH_009_VERIFICATION_NOTE =
		"**Verification note.** **The tool read the array (appends and associative arrays included) and a later use within thirty lines**; an array built in another file, reached by a name reference or used beyond thirty lines is not read, so look for the other places the same options are used. " + _TOKEN_LIFETIME;

	/// <summary>SH-010 generic source form.</summary>
	public const string SH_010_SOURCE_FORM =
		_APPLIES_TO_ALL_SHELLS + ".\n\n"
		+ _FENCE_SH_OPEN + "API_URL=\"https://${API_USER}:${API_PASSWORD}@host.example/Relativity.REST/api\"" + _FENCE_CLOSE;

	/// <summary>SH-010 target form under token authentication.</summary>
	public const string SH_010_TARGET_FORM =
		_DESTINATION_QUESTION + " **If the URL is another service's, this finding is a false positive** (a git remote used by `git` is not read).\n\n" + _FENCE_SH_OPEN + "API_URL=\"https://host.example/Relativity.REST/api\"" + _FENCE_CLOSE + "\n\nand send the token as for a header that names the scheme.";

	/// <summary>SH-010 class share of the credential provenance.</summary>
	public const string SH_010_CREDENTIAL_PROVENANCE = _ROUTE_BY_DESTINATION;

	/// <summary>SH-010 verification note.</summary>
	public const string SH_010_VERIFICATION_NOTE =
		"**Verification note.** **The tool saw a covered client use the variable within thirty lines**; change that call and any other that uses the URL. **A URL with userinfo may already be in logs**: rotate the credential. " + _TOKEN_LIFETIME;

	/// <summary>SH-013 generic source form.</summary>
	public const string SH_013_SOURCE_FORM =
		_APPLIES_TO_ALL_SHELLS + "; `http` and `https` (HTTPie).\n\n"
		+ _FENCE_SH_OPEN + "http --auth \"${API_USER}:${API_PASSWORD}\" GET \"${HOST}/Relativity.REST/api/...\"" + _FENCE_CLOSE;

	/// <summary>SH-013 target form under token authentication.</summary>
	public const string SH_013_TARGET_FORM =
		_DESTINATION_QUESTION + "\n\n" + _FENCE_SH_OPEN + "http --check-status GET \"${HOST}/Relativity.REST/api/...\" \"Authorization:Bearer ${ACCESS_TOKEN}\" \"X-CSRF-Header:-\"" + _FENCE_CLOSE + "\n\nRemove `-a`, `--auth`, `-A` and any userinfo in the URL.";

	/// <summary>SH-013 class share of the credential provenance.</summary>
	public const string SH_013_CREDENTIAL_PROVENANCE = _ROUTE_BY_DESTINATION;

	/// <summary>SH-013 verification note.</summary>
	public const string SH_013_VERIFICATION_NOTE =
		"**Verification note.** " + _TOKEN_LIFETIME + " HTTPie has no `-H @FILE` form, so a token on the command line is visible in `ps` while the command runs: on a shared host use `curl` with `-H @FILE` for this call. **A prompt** (`--auth USER` with no password) is interactive flow evidence, and the finding is then assigned the Authorization Code flow, in which the person signs in on RelativityOne's page. **The target form was syntax-checked and not run: HTTPie has not been installed where this text was checked.**";

	/// <summary>SH-014 generic source form.</summary>
	public const string SH_014_SOURCE_FORM =
		_APPLIES_TO_ALL_SHELLS + "; `aria2c`.\n\n"
		+ _FENCE_SH_OPEN + "aria2c --http-user=\"${API_USER}\" --http-passwd=\"${API_PASSWORD}\" \"${HOST}/Relativity.REST/api/...\"" + _FENCE_CLOSE;

	/// <summary>SH-014 target form under token authentication.</summary>
	public const string SH_014_TARGET_FORM =
		_DESTINATION_QUESTION + "\n\n" + _FENCE_SH_OPEN + "aria2c --header=\"Authorization: Bearer ${ACCESS_TOKEN}\" --header=\"X-CSRF-Header: -\" \"${HOST}/Relativity.REST/api/...\"" + _FENCE_CLOSE + "\n\nRemove `--http-user`, `--http-passwd` and any userinfo in the URL.";

	/// <summary>SH-014 class share of the credential provenance.</summary>
	public const string SH_014_CREDENTIAL_PROVENANCE = _ROUTE_BY_DESTINATION;

	/// <summary>SH-014 verification note.</summary>
	public const string SH_014_VERIFICATION_NOTE =
		"**Verification note.** " + _TOKEN_LIFETIME + " `aria2c` has no `-H @FILE` form, so the token is on the command line while it runs: on a shared host use `curl` for this call. **The target form was syntax-checked and not run: `aria2c` has not been installed where this text was checked.**";

	/// <summary>SH-016 generic source form.</summary>
	public const string SH_016_SOURCE_FORM =
		_APPLIES_TO_ALL_SHELLS + "; `wget`.\n\n"
		+ _FENCE_SH_OPEN + "wget --auth-no-challenge -e \"http_user=${API_USER}\" -e \"http_password=${API_PASSWORD}\" \"${HOST}/Relativity.REST/api/...\"" + _FENCE_CLOSE;

	/// <summary>SH-016 target form under token authentication.</summary>
	public const string SH_016_TARGET_FORM =
		_DESTINATION_QUESTION + "\n\n" + _FENCE_SH_OPEN + "wget --header=\"Authorization: Bearer ${ACCESS_TOKEN}\" --header=\"X-CSRF-Header: -\" \"${HOST}/Relativity.REST/api/...\"" + _FENCE_CLOSE + "\n\nRemove `--auth-no-challenge`, every `-e` or `--execute` that sets `http_user`, `http_password`, `user` or `password`, and any userinfo in the URL.";

	/// <summary>SH-016 class share of the credential provenance.</summary>
	public const string SH_016_CREDENTIAL_PROVENANCE = _ROUTE_BY_DESTINATION;

	/// <summary>SH-016 verification note.</summary>
	public const string SH_016_VERIFICATION_NOTE =
		"**Verification note.** " + _TOKEN_LIFETIME + " `wget` has no `-H @FILE` form, so on a shared host use `curl` for this call.";

	/// <summary>SH-018 generic source form.</summary>
	public const string SH_018_SOURCE_FORM =
		"**Applies to:** `bash` (the `/dev/tcp` form), `sh`, `zsh` and `ksh` (the `nc`, `socat` and `openssl s_client` forms).\n\n"
		+ _FENCE_BASH_OPEN + "exec 3<>/dev/tcp/host.example/80\nprintf 'GET /api/ HTTP/1.1\\r\\nHost: host.example\\r\\nAuthorization: Basic %s\\r\\n\\r\\n' \"${ENCODED}\" >&3" + _FENCE_CLOSE;

	/// <summary>SH-018 target form under token authentication.</summary>
	public const string SH_018_TARGET_FORM =
		_DESTINATION_QUESTION + "\n\n" + _FENCE_SH_OPEN + _BEARER_FROM_HEADER_FILE_CALL + _FENCE_CLOSE + "\n\nReplace the hand-built request with a client that speaks HTTPS and send the token as for a header that names the scheme.";

	/// <summary>SH-018 class share of the credential provenance.</summary>
	public const string SH_018_CREDENTIAL_PROVENANCE = _ROUTE_BY_DESTINATION;

	/// <summary>SH-018 verification note.</summary>
	public const string SH_018_VERIFICATION_NOTE =
		"**Verification note.** " + _TOKEN_LIFETIME + " **A request written to a plain TCP socket carries no TLS**: the credential crossed the network in clear text, so treat it as exposed and rotate it. **The tool read a literal Basic header written to a socket, not where the socket leads.**";

	/// <summary>SH-019 generic source form.</summary>
	public const string SH_019_SOURCE_FORM =
		_APPLIES_TO_ALL_SHELLS + "; `curl`.\n\n"
		+ _FENCE_SH_OPEN + "cat > \"${CONFIG_FILE}\" <<-EOF\n\tuser = \"${API_USER}:${API_PASSWORD}\"\nEOF\ncurl --silent --show-error --config \"${CONFIG_FILE}\" \"${HOST}/Relativity.REST/api/...\"" + _FENCE_CLOSE;

	/// <summary>SH-019 target form under token authentication.</summary>
	public const string SH_019_TARGET_FORM =
		_DESTINATION_QUESTION + "\n\n" + _FENCE_SH_OPEN + "( umask 077; printf 'header = \"Authorization: Bearer %s\"\\nheader = \"X-CSRF-Header: -\"\\n' \"${ACCESS_TOKEN}\" > \"${CONFIG_FILE}\" )\ncurl --silent --show-error --fail --config \"${CONFIG_FILE}\" \"${HOST}/Relativity.REST/api/...\"" + _FENCE_CLOSE + "\n\nRemove the `user` line and the code that wrote it.";

	/// <summary>SH-019 class share of the credential provenance.</summary>
	public const string SH_019_CREDENTIAL_PROVENANCE = _ROUTE_BY_DESTINATION;

	/// <summary>SH-019 verification note.</summary>
	public const string SH_019_VERIFICATION_NOTE =
		"**Verification note.** " + _TOKEN_LIFETIME + " **The tool read the configuration the script writes and the call that reads it, joined by the text of the path**: another script that reads the same file, or a copy of the file in a backup or a log, still holds the credential, which must be rotated and not only removed. `curl` reads the `header` line from the file as it reads `-H` (documented in the `curl` manual; the `user` line was *checked by NSerio locally*).";

	/// <summary>SH-021 generic source form.</summary>
	public const string SH_021_SOURCE_FORM =
		_APPLIES_TO_ALL_SHELLS + "; `curl` through a same-file command variable (the alias form needs `expand_aliases` on, or POSIX mode, in `bash`).\n\n"
		+ _FENCE_SH_OPEN + "CURL=curl\n${CURL} --silent --show-error -u \"${API_USER}:${API_PASSWORD}\" \"${HOST}/Relativity.REST/api/...\"" + _FENCE_CLOSE;

	/// <summary>SH-021 target form under token authentication.</summary>
	public const string SH_021_TARGET_FORM =
		_DESTINATION_QUESTION + "\n\n" + _FENCE_SH_OPEN + "CURL=curl\n${CURL} --silent --show-error --fail -H @\"${HEADER_FILE}\" \"${HOST}/Relativity.REST/api/...\"" + _FENCE_CLOSE + "\n\nRemove `-u`, `--user` and `--basic`.";

	/// <summary>SH-021 class share of the credential provenance.</summary>
	public const string SH_021_CREDENTIAL_PROVENANCE = _ROUTE_BY_DESTINATION;

	/// <summary>SH-021 verification note.</summary>
	public const string SH_021_VERIFICATION_NOTE =
		"**Verification note.** " + _TOKEN_LIFETIME + " **The tool followed a definition of the command within thirty lines**: other calls through the same name that sit farther away, or in other files, are not read, so change every place that uses it.";

	/// <summary>SH-024 generic source form.</summary>
	public const string SH_024_SOURCE_FORM =
		_APPLIES_TO_ALL_SHELLS + " (the here-string form: `bash`, `zsh` and `ksh`); `base64` and `openssl`.\n\n"
		+ _FENCE_SH_OPEN + "pair=\"${API_USER}:${API_PASSWORD}\"\nENCODED=$(printf '%s' \"${pair}\" | base64 | tr -d '\\n')" + _FENCE_CLOSE;

	/// <summary>SH-024 target form under token authentication.</summary>
	public const string SH_024_TARGET_FORM =
		_DESTINATION_QUESTION + " **If the encoded pair is not a credential**, this finding is a false positive and nothing changes.\n\nRemove the pair, the encoding and the variable, and send the token: `-H \"Authorization: Bearer ${ACCESS_TOKEN}\"`, as for a header that names the scheme.";

	/// <summary>SH-024 class share of the credential provenance.</summary>
	public const string SH_024_CREDENTIAL_PROVENANCE = _ROUTE_BY_DESTINATION;

	/// <summary>SH-024 verification note.</summary>
	public const string SH_024_VERIFICATION_NOTE =
		"**Verification note.** **The tool saw a colon pair held in a variable, encoded, and the encoded value used by a client command within thirty lines**: follow `ENCODED` to every place it is sent and change those uses too. " + _TOKEN_LIFETIME;

	/// <summary>SH-025 generic source form.</summary>
	public const string SH_025_SOURCE_FORM =
		_APPLIES_TO_ALL_SHELLS + "; `curl`, `wget`, HTTPie, `aria2c`.\n\n"
		+ _FENCE_SH_OPEN + "CURL_OPTIONS=\"-sS -u ${API_USER}:${API_PASSWORD}\"\ncurl ${CURL_OPTIONS} \"${HOST}/Relativity.REST/api/...\"" + _FENCE_CLOSE;

	/// <summary>SH-025 target form under token authentication.</summary>
	public const string SH_025_TARGET_FORM =
		_DESTINATION_QUESTION + "\n\n" + _FENCE_SH_OPEN + "CURL_OPTIONS=\"-sS --fail\"\ncurl ${CURL_OPTIONS} -H @\"${HEADER_FILE}\" \"${HOST}/Relativity.REST/api/...\"" + _FENCE_CLOSE + "\n\nRemove the credential option from the string.";

	/// <summary>SH-025 class share of the credential provenance.</summary>
	public const string SH_025_CREDENTIAL_PROVENANCE = _ROUTE_BY_DESTINATION;

	/// <summary>SH-025 verification note.</summary>
	public const string SH_025_VERIFICATION_NOTE =
		"**Verification note.** **The tool read the string and an unquoted use within thirty lines**; an option string used elsewhere is not read, so look for the other places it is used. A string that holds a credential is split into words by the shell, so it also appears in `ps` while the command runs. " + _TOKEN_LIFETIME;
}

/// <summary>
/// The `Shell` analyzer's template provider and fallback texts: eighteen classes resolve to their entry under either flow; the seven Low classes, and any
/// identifier the analyzer does not declare, resolve to null, which routes a Low finding to manual review and sends a High or Medium finding of an undeclared
/// class to the language's fallback text.
/// </summary>
internal static class ShellTemplateDeclarations
{
	private const string _PARAGRAPH_SEPARATOR = "\n\n";

	private const string _SH_001 = "SH-001";
	private const string _SH_002 = "SH-002";
	private const string _SH_003 = "SH-003";
	private const string _SH_004 = "SH-004";
	private const string _SH_005 = "SH-005";
	private const string _SH_006 = "SH-006";
	private const string _SH_007 = "SH-007";
	private const string _SH_008 = "SH-008";
	private const string _SH_009 = "SH-009";
	private const string _SH_010 = "SH-010";
	private const string _SH_013 = "SH-013";
	private const string _SH_014 = "SH-014";
	private const string _SH_016 = "SH-016";
	private const string _SH_018 = "SH-018";
	private const string _SH_019 = "SH-019";
	private const string _SH_021 = "SH-021";
	private const string _SH_024 = "SH-024";
	private const string _SH_025 = "SH-025";

	private static readonly FrozenDictionary<string, RecommendationTemplate> _clientCredentialsTemplatesByPatternClass =
		BuildTemplates(ShellRecommendationTemplates.CLIENT_CREDENTIALS_TOKEN_ACQUISITION);

	private static readonly FrozenDictionary<string, RecommendationTemplate> _authorizationCodeTemplatesByPatternClass =
		BuildTemplates(ShellRecommendationTemplates.AUTHORIZATION_CODE_TOKEN_ACQUISITION);

	/// <summary>The language's own text for the generic recommendation route, per flow.</summary>
	internal static FrozenDictionary<CredentialFlow, string> FallbackTextByFlow { get; } = new Dictionary<CredentialFlow, string>
	{
		[CredentialFlow.ClientCredentials] = ShellRecommendationTemplates.SHELL_LANGUAGE_FALLBACK_TEXT,
		[CredentialFlow.AuthorizationCode] = ShellRecommendationTemplates.SHELL_CODE_LANGUAGE_FALLBACK_TEXT
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
			[_SH_001] = Template(ShellRecommendationTemplates.SH_001_SOURCE_FORM, ShellRecommendationTemplates.SH_001_TARGET_FORM, ShellRecommendationTemplates.SH_001_CREDENTIAL_PROVENANCE, ShellRecommendationTemplates.SH_001_VERIFICATION_NOTE, tokenAcquisition),
			[_SH_002] = Template(ShellRecommendationTemplates.SH_002_SOURCE_FORM, ShellRecommendationTemplates.SH_002_TARGET_FORM, ShellRecommendationTemplates.SH_002_CREDENTIAL_PROVENANCE, ShellRecommendationTemplates.SH_002_VERIFICATION_NOTE, tokenAcquisition),
			[_SH_003] = Template(ShellRecommendationTemplates.SH_003_SOURCE_FORM, ShellRecommendationTemplates.SH_003_TARGET_FORM, ShellRecommendationTemplates.SH_003_CREDENTIAL_PROVENANCE, ShellRecommendationTemplates.SH_003_VERIFICATION_NOTE, tokenAcquisition),
			[_SH_004] = Template(ShellRecommendationTemplates.SH_004_SOURCE_FORM, ShellRecommendationTemplates.SH_004_TARGET_FORM, ShellRecommendationTemplates.SH_004_CREDENTIAL_PROVENANCE, ShellRecommendationTemplates.SH_004_VERIFICATION_NOTE, tokenAcquisition),
			[_SH_005] = Template(ShellRecommendationTemplates.SH_005_SOURCE_FORM, ShellRecommendationTemplates.SH_005_TARGET_FORM, ShellRecommendationTemplates.SH_005_CREDENTIAL_PROVENANCE, ShellRecommendationTemplates.SH_005_VERIFICATION_NOTE, tokenAcquisition),
			[_SH_006] = Template(ShellRecommendationTemplates.SH_006_SOURCE_FORM, ShellRecommendationTemplates.SH_006_TARGET_FORM, ShellRecommendationTemplates.SH_006_CREDENTIAL_PROVENANCE, ShellRecommendationTemplates.SH_006_VERIFICATION_NOTE, tokenAcquisition),
			[_SH_007] = Template(ShellRecommendationTemplates.SH_007_SOURCE_FORM, ShellRecommendationTemplates.SH_007_TARGET_FORM, ShellRecommendationTemplates.SH_007_CREDENTIAL_PROVENANCE, ShellRecommendationTemplates.SH_007_VERIFICATION_NOTE, tokenAcquisition),
			[_SH_008] = Template(ShellRecommendationTemplates.SH_008_SOURCE_FORM, ShellRecommendationTemplates.SH_008_TARGET_FORM, ShellRecommendationTemplates.SH_008_CREDENTIAL_PROVENANCE, ShellRecommendationTemplates.SH_008_VERIFICATION_NOTE, tokenAcquisition),
			[_SH_009] = Template(ShellRecommendationTemplates.SH_009_SOURCE_FORM, ShellRecommendationTemplates.SH_009_TARGET_FORM, ShellRecommendationTemplates.SH_009_CREDENTIAL_PROVENANCE, ShellRecommendationTemplates.SH_009_VERIFICATION_NOTE, tokenAcquisition),
			[_SH_010] = Template(ShellRecommendationTemplates.SH_010_SOURCE_FORM, ShellRecommendationTemplates.SH_010_TARGET_FORM, ShellRecommendationTemplates.SH_010_CREDENTIAL_PROVENANCE, ShellRecommendationTemplates.SH_010_VERIFICATION_NOTE, tokenAcquisition),
			[_SH_013] = Template(ShellRecommendationTemplates.SH_013_SOURCE_FORM, ShellRecommendationTemplates.SH_013_TARGET_FORM, ShellRecommendationTemplates.SH_013_CREDENTIAL_PROVENANCE, ShellRecommendationTemplates.SH_013_VERIFICATION_NOTE, tokenAcquisition),
			[_SH_014] = Template(ShellRecommendationTemplates.SH_014_SOURCE_FORM, ShellRecommendationTemplates.SH_014_TARGET_FORM, ShellRecommendationTemplates.SH_014_CREDENTIAL_PROVENANCE, ShellRecommendationTemplates.SH_014_VERIFICATION_NOTE, tokenAcquisition),
			[_SH_016] = Template(ShellRecommendationTemplates.SH_016_SOURCE_FORM, ShellRecommendationTemplates.SH_016_TARGET_FORM, ShellRecommendationTemplates.SH_016_CREDENTIAL_PROVENANCE, ShellRecommendationTemplates.SH_016_VERIFICATION_NOTE, tokenAcquisition),
			[_SH_018] = Template(ShellRecommendationTemplates.SH_018_SOURCE_FORM, ShellRecommendationTemplates.SH_018_TARGET_FORM, ShellRecommendationTemplates.SH_018_CREDENTIAL_PROVENANCE, ShellRecommendationTemplates.SH_018_VERIFICATION_NOTE, tokenAcquisition),
			[_SH_019] = Template(ShellRecommendationTemplates.SH_019_SOURCE_FORM, ShellRecommendationTemplates.SH_019_TARGET_FORM, ShellRecommendationTemplates.SH_019_CREDENTIAL_PROVENANCE, ShellRecommendationTemplates.SH_019_VERIFICATION_NOTE, tokenAcquisition),
			[_SH_021] = Template(ShellRecommendationTemplates.SH_021_SOURCE_FORM, ShellRecommendationTemplates.SH_021_TARGET_FORM, ShellRecommendationTemplates.SH_021_CREDENTIAL_PROVENANCE, ShellRecommendationTemplates.SH_021_VERIFICATION_NOTE, tokenAcquisition),
			[_SH_024] = Template(ShellRecommendationTemplates.SH_024_SOURCE_FORM, ShellRecommendationTemplates.SH_024_TARGET_FORM, ShellRecommendationTemplates.SH_024_CREDENTIAL_PROVENANCE, ShellRecommendationTemplates.SH_024_VERIFICATION_NOTE, tokenAcquisition),
			[_SH_025] = Template(ShellRecommendationTemplates.SH_025_SOURCE_FORM, ShellRecommendationTemplates.SH_025_TARGET_FORM, ShellRecommendationTemplates.SH_025_CREDENTIAL_PROVENANCE, ShellRecommendationTemplates.SH_025_VERIFICATION_NOTE, tokenAcquisition)
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
