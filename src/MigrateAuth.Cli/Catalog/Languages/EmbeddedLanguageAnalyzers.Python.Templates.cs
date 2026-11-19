using System.Collections.Frozen;

using MigrateAuth.Cli.Findings;
using MigrateAuth.Cli.Reporting;

namespace MigrateAuth.Cli.Catalog.Languages;

/// <summary>
/// The recommendation text for `Python`: thirteen classes, both flows and both generic fallback texts. The one class that is Low by definition (a header mapping that is not shown to reach a request) carries no
/// entry: it is routed to manual review, and its reason sentence is the shared Low vocabulary's.
///
/// HOW A RECOMMENDATION IS COMPOSED. Part 1 is the class's four fields and is flow-neutral, so the source form, the target form and the verification note are the same text for both flows. The credential provenance is
/// one sentence shared by every class followed by part 2 for the finding's flow, held once here as <see cref="CLIENT_CREDENTIALS_TOKEN_ACQUISITION"/> and <see cref="AUTHORIZATION_CODE_TOKEN_ACQUISITION"/>. Part 3, the
/// OAuth2 Client block, is appended by the shared engine's composer and is not repeated here.
///
/// ENCODING CHOICES. Design-record passages are not encoded. The source and analysis identifiers that the design text cites in parentheses are dropped, because the reader of a report has no document to resolve them
/// against: a fact is labelled in words. Every secret-handling sentence names <see cref="SecretManagementPointer.PART_B_NAME"/>. The label of a fact NSerio verified but Relativity does not document is
/// <see cref="OAuth2ClientConfigurationBlocks.NSERIO_VERIFIED_LABEL"/>. Every block is fenced <c>python</c> and is indented with tabs only. The text uses only the standard library and the four HTTP clients `requests`,
/// `httpx`, `aiohttp` and `urllib3`: no framework is installed and none is named in code, and the web-server variant uses the standard library's <c>http.server</c> for its callback handler. A template never names a
/// Relativity fact it cannot state in words; the destination is always asked first, because the engine cannot see which host a script calls.
///
/// A SEPARATE FILE BECAUSE <c>PythonAnalyzer</c> IS <c>file</c>-SCOPED: its type cannot be split across files, so the text lives in this <c>internal</c> holder and the analyzer's template provider reads it by the same
/// identifiers.
/// </summary>
internal static class PythonRecommendationTemplates
{
	private const string _FENCE_PYTHON_OPEN = "```python\n";
	private const string _FENCE_CLOSE = "\n```";

	/// <summary>The question every template opens its target form with: the engine cannot see which host the code calls.</summary>
	private const string _DESTINATION_QUESTION =
		"**First, the destination:** if this Relativity instance is not RelativityOne — including a Relativity Server instance, whose own documentation governs — no migration to RelativityOne applies and the rest of this entry does not apply.";

	/// <summary>The rotation statement of the classes that expose a credential.</summary>
	public const string ROTATION_STATEMENT =
		"> Treat this credential as compromised. Rotate it, and assume the repository history has published it. Completing the migration does not close this exposure; rotation does.";

	/// <summary>The class's share of the credential provenance, the same for every class: the destination question again, then where the token comes from.</summary>
	public const string CREDENTIAL_PROVENANCE =
		"**Where the credentials come from.** The source form establishes that a client sends Basic, not that the destination is RelativityOne; if it is not RelativityOne the migration is that service's own token mechanism, no migration to RelativityOne applies, and the reader stops here. If it is RelativityOne, the token comes from the token-acquisition part that follows, and the user name and password are replaced by the OAuth2 Client's client ID and secret, kept as " + SecretManagementPointer.PART_B_NAME + " describes, never in source.";

	/// <summary>The Client Credentials generic fallback prose for a `Python` finding, with the secret-handling sentence naming the Part B pointer.</summary>
	public const string PYTHON_LANGUAGE_FALLBACK_TEXT =
		"Basic Authentication is found at this location but the construct is not one that this text describes, so the change has not been characterized and no before-and-after form is offered here. If the destination is not RelativityOne, the migration is that service's own and no migration to RelativityOne applies (that Relativity's identity endpoint issues no tokens for another service is unverified). If the destination is RelativityOne, Relativity's documentation recommends bearer tokens and states that Basic Authentication is no longer supported for named APIs, with no date for its removal asserted (the platform's direction beyond that is NSerio's reading, unverified). The documented destination in Python is a bearer Authorization header sent by an HTTP client such as requests, httpx, aiohttp, urllib3 or the standard library's urllib.request, carrying a token obtained from the platform's identity endpoint with the client-credentials grant, from a Python job, service or script. The client ID and secret that obtain that token belong in the process environment or a secret store, per " + SecretManagementPointer.PART_B_NAME + ", and never in source. Beyond those facts this is a pointer and not an instruction: the migration must be designed from the code by whoever owns it, rather than applied from this text.";

	/// <summary>The Authorization Code generic fallback prose for a `Python` finding.</summary>
	public const string PYTHON_CODE_LANGUAGE_FALLBACK_TEXT =
		"Basic Authentication is found at this location but the construct is not one that this text describes, so the change has not been characterized and no before-and-after form is offered here. If the destination is not RelativityOne, the migration is that service's own and no migration to RelativityOne applies (that Relativity's identity endpoint issues no tokens for another service is unverified). If the destination is RelativityOne, Relativity's documentation recommends bearer tokens and states that Basic Authentication is no longer supported for named APIs, with no date for its removal asserted (the platform's direction beyond that is NSerio's reading, unverified), and this match is assigned the Authorization Code flow, in which the code sends the person to RelativityOne to sign in, receives an authorization code at a Redirect URL registered on its OAuth2 Client, and exchanges the code for an access token that it sends as a Bearer token. Code requires a client secret, so the exchange runs where a secret can be kept: for a Python web server the callback route performs it and keeps the tokens on the server; for a command-line tool or a desktop program a loopback listener on the registered fixed port receives the code. The authorize endpoint is <host>/Relativity/Identity/connect/authorize and the code is exchanged at <host>/Relativity/Identity/connect/token; both are " + OAuth2ClientConfigurationBlocks.NSERIO_VERIFIED_LABEL + ". PKCE is required by Relativity's own documentation to obtain a refresh token, and not otherwise, which is also " + OAuth2ClientConfigurationBlocks.NSERIO_VERIFIED_LABEL + ". The client ID and secret belong in the process environment or a secret store, per " + SecretManagementPointer.PART_B_NAME + ", never in source. After the change the code no longer collects or forwards the person's password. Beyond those facts this is a pointer and not an instruction: the migration must be designed from the code by whoever owns it, rather than applied from this text.";

	/// <summary>The block that acquires and caches a Client Credentials token in a Python process that acts as itself, with `requests`.</summary>
	public const string CLIENT_CREDENTIALS_REQUESTS_BLOCK =
		"import os\n"
		+ "import time\n"
		+ "\n"
		+ "import requests\n"
		+ "\n"
		+ "IDENTITY_TOKEN_URL = \"https://HOST/Relativity/Identity/connect/token\"\n"
		+ "EXPIRY_MARGIN_SECONDS = 60\n"
		+ "REQUEST_TIMEOUT_SECONDS = 30\n"
		+ "\n"
		+ "_cached_token = {\"value\": None, \"expires_at\": 0.0}\n"
		+ "\n"
		+ "\n"
		+ "def get_access_token():\n"
		+ "\tnow = time.monotonic()\n"
		+ "\tif _cached_token[\"value\"] is None or _cached_token[\"expires_at\"] - now < EXPIRY_MARGIN_SECONDS:\n"
		+ "\t\tresponse = requests.post(\n"
		+ "\t\t\tIDENTITY_TOKEN_URL,\n"
		+ "\t\t\tdata={\n"
		+ "\t\t\t\t\"grant_type\": \"client_credentials\",\n"
		+ "\t\t\t\t\"client_id\": os.environ[\"RELATIVITY_CLIENT_ID\"],\n"
		+ "\t\t\t\t\"client_secret\": os.environ[\"RELATIVITY_CLIENT_SECRET\"],\n"
		+ "\t\t\t},\n"
		+ "\t\t\ttimeout=REQUEST_TIMEOUT_SECONDS,\n"
		+ "\t\t)\n"
		+ "\t\tresponse.raise_for_status()\n"
		+ "\t\tpayload = response.json()\n"
		+ "\t\t_cached_token[\"value\"] = payload[\"access_token\"]\n"
		+ "\t\t_cached_token[\"expires_at\"] = now + payload[\"expires_in\"]\n"
		+ "\treturn _cached_token[\"value\"]";

	/// <summary>The same token request in the other three authorized clients, as fragments that share the cache and the credentials of the block above.</summary>
	public const string CLIENT_CREDENTIALS_OTHER_CLIENTS_BLOCK =
		"HTTP_OK = 200\n"
		+ "token_request_fields = {\n"
		+ "\t\"grant_type\": \"client_credentials\",\n"
		+ "\t\"client_id\": os.environ[\"RELATIVITY_CLIENT_ID\"],\n"
		+ "\t\"client_secret\": os.environ[\"RELATIVITY_CLIENT_SECRET\"],\n"
		+ "}\n"
		+ "\n"
		+ "# httpx: a dictionary passed as data is form-encoded\n"
		+ "response = httpx.post(IDENTITY_TOKEN_URL, data=token_request_fields, timeout=REQUEST_TIMEOUT_SECONDS)\n"
		+ "response.raise_for_status()\n"
		+ "payload = response.json()\n"
		+ "\n"
		+ "# aiohttp: data= is form-encoded\n"
		+ "async with session.post(IDENTITY_TOKEN_URL, data=token_request_fields) as response:\n"
		+ "\tresponse.raise_for_status()\n"
		+ "\tpayload = await response.json()\n"
		+ "\n"
		+ "# urllib3: PoolManager.request form-encodes the fields argument for POST only with encode_multipart=False\n"
		+ "response = urllib3.PoolManager().request(\"POST\", IDENTITY_TOKEN_URL, fields=token_request_fields, encode_multipart=False)\n"
		+ "if response.status != HTTP_OK:\n"
		+ "\traise RuntimeError(f\"The token request failed with status {response.status}\")\n"
		+ "payload = response.json()";

	/// <summary>Part 2 for Client Credentials: how the token is obtained and kept, in one variant that states its precondition. Appended to every class's credential-provenance share for a Client Credentials finding.</summary>
	public const string CLIENT_CREDENTIALS_TOKEN_ACQUISITION =
		"The text a match assigned Client Credentials receives after part 1. Create an OAuth2 Client with Flow Grant Type set to Client Credentials (part 3 lists its settings), then send a form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=client_credentials`, `client_id` and `client_secret`. The token is sent as `Authorization: Bearer <token>`, with `X-CSRF-Header: -` on REST and Kepler calls. **The endpoint is documented for this grant.** The scope depends on what the integration needs and **no single scope is required**: Relativity's documentation shows `SystemUserInfo` and `UserInfoAccess` and no page says which is correct or what either grants; use the value the API you call documents, and confirm it.\n\n**Variant: a Python process acting as itself**\n\n**Precondition: this variant is for a Python job, service or script that does not act for a signed-in person. If a person signs in, use Authorization Code.**\n\n"
		+ _FENCE_PYTHON_OPEN + CLIENT_CREDENTIALS_REQUESTS_BLOCK + _FENCE_CLOSE
		+ "\n\n`requests` form-encodes a dictionary passed as `data`, and `raise_for_status` raises on a 4XX or 5XX response. **The same token request in the other three authorized clients** (the cache and the credentials are as above):\n\n"
		+ _FENCE_PYTHON_OPEN + CLIENT_CREDENTIALS_OTHER_CLIENTS_BLOCK + _FENCE_CLOSE
		+ "\n\n**Cache the token and re-acquire it on expiry — do not refresh.** Request a new token with the client secret shortly before the current one expires (the block uses a margin of 60 seconds), and once when a call is rejected for authentication. Client Credentials does not offer refresh tokens: **" + OAuth2ClientConfigurationBlocks.NSERIO_VERIFIED_LABEL + "** — the Allow Refresh Tokens control does not appear for that grant in the RelativityOne user interface. The recommended Access Token Lifetime is 60 minutes. **The client secret** belongs in the process environment or a secret store, as " + SecretManagementPointer.PART_B_NAME + " describes, **never in source or in a tracked file**. A reset invalidates the previous secret immediately, so plan the cut-over (the two-client pattern is inferred). **Before deploying, confirm the calling identity:** every call now runs as the OAuth2 Client's Context User, not as the user in the replaced credential, and the non-administrator Context User is NSerio's recommendation. Trusted IPs restrict where a token can be requested, not a token already issued. **Whether `httpx` and `urllib3` accept a dictionary the way the pages read say is documented for the page's version only** (`httpx` shows no version): unchecked for other versions.";

	/// <summary>The server-side web variant of Authorization Code: the pending-login store, the authorize address, the code exchange and a callback handler of the standard library's `http.server`.</summary>
	public const string AUTHORIZATION_CODE_WEB_SERVER_BLOCK =
		"import base64\n"
		+ "import hashlib\n"
		+ "import http.server\n"
		+ "import secrets\n"
		+ "from urllib.parse import parse_qs, urlencode, urlparse\n"
		+ "\n"
		+ "import requests\n"
		+ "\n"
		+ "IDENTITY_BASE = \"https://HOST/Relativity/Identity/connect\"\n"
		+ "REDIRECT_URI = \"https://app.example.test/auth/callback\"\n"
		+ "CALLBACK_PATH = \"/auth/callback\"\n"
		+ "RANDOM_BYTES = 32\n"
		+ "REQUEST_TIMEOUT_SECONDS = 30\n"
		+ "HTTP_OK = 200\n"
		+ "HTTP_NOT_FOUND = 404\n"
		+ "_pending_by_session = {}\n"
		+ "\n"
		+ "\n"
		+ "def base64_url(raw_bytes):\n"
		+ "\treturn base64.urlsafe_b64encode(raw_bytes).rstrip(b\"=\").decode(\"ascii\")\n"
		+ "\n"
		+ "\n"
		+ "def begin_login(session_id, client_id, want_refresh_token):\n"
		+ "\tstate = secrets.token_urlsafe(RANDOM_BYTES)\n"
		+ "\tpending = {\"state\": state, \"verifier\": None}\n"
		+ "\tquery = {\n"
		+ "\t\t\"response_type\": \"code\",\n"
		+ "\t\t\"client_id\": client_id,\n"
		+ "\t\t\"redirect_uri\": REDIRECT_URI,\n"
		+ "\t\t\"state\": state,\n"
		+ "\t}\n"
		+ "\tif want_refresh_token:\n"
		+ "\t\tverifier = secrets.token_urlsafe(RANDOM_BYTES)\n"
		+ "\t\tpending[\"verifier\"] = verifier\n"
		+ "\t\tquery[\"code_challenge\"] = base64_url(hashlib.sha256(verifier.encode(\"ascii\")).digest())\n"
		+ "\t\tquery[\"code_challenge_method\"] = \"S256\"\n"
		+ "\t_pending_by_session[session_id] = pending\n"
		+ "\treturn f\"{IDENTITY_BASE}/authorize?{urlencode(query)}\"\n"
		+ "\n"
		+ "\n"
		+ "def complete_login(session_id, client_id, client_secret, returned_code, returned_state):\n"
		+ "\tpending = _pending_by_session.pop(session_id, None)\n"
		+ "\ttokens = None\n"
		+ "\tstate_matches = (\n"
		+ "\t\tpending is not None\n"
		+ "\t\tand returned_state is not None\n"
		+ "\t\tand secrets.compare_digest(returned_state.encode(\"utf-8\"), pending[\"state\"].encode(\"utf-8\"))\n"
		+ "\t)\n"
		+ "\tif state_matches:\n"
		+ "\t\tfields = {\n"
		+ "\t\t\t\"grant_type\": \"authorization_code\",\n"
		+ "\t\t\t\"code\": returned_code,\n"
		+ "\t\t\t\"redirect_uri\": REDIRECT_URI,\n"
		+ "\t\t\t\"client_id\": client_id,\n"
		+ "\t\t\t\"client_secret\": client_secret,\n"
		+ "\t\t}\n"
		+ "\t\tif pending[\"verifier\"] is not None:\n"
		+ "\t\t\tfields[\"code_verifier\"] = pending[\"verifier\"]\n"
		+ "\t\tresponse = requests.post(f\"{IDENTITY_BASE}/token\", data=fields, timeout=REQUEST_TIMEOUT_SECONDS)\n"
		+ "\t\ttokens = response.json() if response.status_code == HTTP_OK else None\n"
		+ "\treturn tokens\n"
		+ "\n"
		+ "\n"
		+ "class CallbackHandler(http.server.BaseHTTPRequestHandler):\n"
		+ "\tdef do_GET(self):\n"
		+ "\t\taddress = urlparse(self.path)\n"
		+ "\t\tis_callback = address.path == CALLBACK_PATH\n"
		+ "\t\tself.send_response(HTTP_OK if is_callback else HTTP_NOT_FOUND)\n"
		+ "\t\tself.end_headers()\n"
		+ "\t\tif is_callback:\n"
		+ "\t\t\tquery = parse_qs(address.query)\n"
		+ "\t\t\tself.wfile.write(b\"Signed in.\")\n"
		+ "\t\t\tself.server.returned_code = query.get(\"code\", [None])[0]\n"
		+ "\t\t\tself.server.returned_state = query.get(\"state\", [None])[0]";

	/// <summary>The command-line variant of Authorization Code: a loopback listener of the standard library on the registered fixed port that receives the code and the state of one callback request.</summary>
	public const string AUTHORIZATION_CODE_COMMAND_LINE_BLOCK =
		"import http.server\n"
		+ "import time\n"
		+ "import webbrowser\n"
		+ "from urllib.parse import parse_qs, urlparse\n"
		+ "\n"
		+ "LOOPBACK_HOST = \"127.0.0.1\"\n"
		+ "REGISTERED_PORT = 8123\n"
		+ "CALLBACK_PATH = \"/callback\"\n"
		+ "WAIT_SECONDS = 120\n"
		+ "POLL_SECONDS = 1.0\n"
		+ "HTTP_OK = 200\n"
		+ "HTTP_NOT_FOUND = 404\n"
		+ "\n"
		+ "\n"
		+ "class _RedirectHandler(http.server.BaseHTTPRequestHandler):\n"
		+ "\treceived = None\n"
		+ "\n"
		+ "\tdef do_GET(self):\n"
		+ "\t\taddress = urlparse(self.path)\n"
		+ "\t\tis_callback = address.path == CALLBACK_PATH\n"
		+ "\t\tself.send_response(HTTP_OK if is_callback else HTTP_NOT_FOUND)\n"
		+ "\t\tself.end_headers()\n"
		+ "\t\tself.wfile.write(b\"You can close this window.\" if is_callback else b\"Not found\")\n"
		+ "\t\tif is_callback:\n"
		+ "\t\t\tquery = parse_qs(address.query)\n"
		+ "\t\t\ttype(self).received = {\n"
		+ "\t\t\t\t\"code\": query.get(\"code\", [None])[0],\n"
		+ "\t\t\t\t\"state\": query.get(\"state\", [None])[0],\n"
		+ "\t\t\t}\n"
		+ "\n"
		+ "\tdef log_message(self, message_format, *arguments):\n"
		+ "\t\treturn None\n"
		+ "\n"
		+ "\n"
		+ "class _LoopbackServer(http.server.HTTPServer):\n"
		+ "\tallow_reuse_address = False\n"
		+ "\n"
		+ "\n"
		+ "def wait_for_redirect(authorize_address):\n"
		+ "\t_RedirectHandler.received = None\n"
		+ "\tprint(f\"Sign in at: {authorize_address}\")\n"
		+ "\ttry:\n"
		+ "\t\tserver = _LoopbackServer((LOOPBACK_HOST, REGISTERED_PORT), _RedirectHandler)\n"
		+ "\texcept OSError:\n"
		+ "\t\tprint(f\"Port {REGISTERED_PORT} is in use. Close the program that holds it and sign in again.\")\n"
		+ "\telse:\n"
		+ "\t\tserver.timeout = POLL_SECONDS\n"
		+ "\t\twebbrowser.open(authorize_address)\n"
		+ "\t\tdeadline = time.monotonic() + WAIT_SECONDS\n"
		+ "\t\twhile _RedirectHandler.received is None and time.monotonic() < deadline:\n"
		+ "\t\t\tserver.handle_request()\n"
		+ "\t\tserver.server_close()\n"
		+ "\treturn _RedirectHandler.received";

	/// <summary>Part 2 for Authorization Code: the common part and the two variants, each with its precondition. Appended to every class's credential-provenance share for an Authorization Code finding.</summary>
	public const string AUTHORIZATION_CODE_TOKEN_ACQUISITION =
		"**Common to both variants.** The person signs in on RelativityOne's own page and the code never holds their password. The text has a common part and **two variants by application type**, each opening with the precondition it is for: a server-side web application and a command-line or desktop tool. Read the precondition of each and discard the variant that does not apply.\n\n**Sign in.** Replace the code that collected the person's credentials with a redirect to the authorize endpoint `<host>/Relativity/Identity/connect/authorize` (**" + OAuth2ClientConfigurationBlocks.NSERIO_VERIFIED_LABEL + "**), which leads to RelativityOne's login page. The request carries the standard authorization-code parameters — `client_id`, `response_type=code`, the registered `redirect_uri`, `state` checked on return, and `code_challenge` with `code_challenge_method=S256` when PKCE is used; these are standard OAuth (RFC 6749 and RFC 7636) and Relativity documents none of them.\n\n**PKCE.** PKCE is required by Relativity's own documentation to obtain a refresh token and not otherwise: a Code request that does not ask for a refresh token works without it, **" + OAuth2ClientConfigurationBlocks.NSERIO_VERIFIED_LABEL + "**. NSerio recommends `S256` (NSerio recommendation) and does not claim Relativity accepts any particular method. The verifier is 43 to 128 characters of the unreserved set and the `S256` challenge is the base64url of the SHA-256 of the verifier; `secrets.token_urlsafe(32)` gives 43 characters from that set (that the two match is arithmetic, not a tested fact) and `hashlib.sha256` and `base64.urlsafe_b64encode` compute the challenge. **`state`** is generated, stored against the session and compared on return; `nonce` is not used, and Relativity does not document whether it enforces either.\n\n**The client secret is required.** Code does not work without a client secret: tested and rejected, **" + OAuth2ClientConfigurationBlocks.NSERIO_VERIFIED_LABEL + "**, which confirms Relativity's own statement. **A secret kept in a program that is distributed is extractable**, so the exchange runs where a secret can be kept.\n\n**Exchange the code.** A form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=authorization_code`, the code, the same `redirect_uri`, the client ID and the secret, plus the `code_verifier` when PKCE is used (**" + OAuth2ClientConfigurationBlocks.NSERIO_VERIFIED_LABEL + "**).\n\n**Redirect URL.** A server-side application registers **one exact HTTPS URL per environment, no wildcards** (the matching rules are not documented and no claim is made about them). A plain-HTTP loopback Redirect URL `http://127.0.0.1:<port>/<path>` on a **fixed registered port** is accepted, **" + OAuth2ClientConfigurationBlocks.NSERIO_VERIFIED_LABEL + "**, and is the command-line shape. RFC 8252 requires an authorization server to allow any port for a loopback redirect; **Relativity's behaviour is the fixed registered port, and the RFC's rule is not claimed for it.**\n\n**On expiry.** With Allow Refresh Tokens enabled and PKCE used, the code holds a refresh token and sends `grant_type=refresh_token` in place of the code exchange: *inferred* from the standard OAuth shape, not tested; Relativity does not document a refresh token's lifetime, rotation, revocation or revocation endpoint, nor what disabling the client does to tokens already issued. **A refresh token is a secret.** **Single sign-on:** sign-in happens on RelativityOne's own page, so the instance's single sign-on and two-factor rules apply unchanged (*inferred*). **The code stops collecting the password:** after this change nothing receives, stores or forwards the person's user name and password, and the code holds only tokens. Every call now runs as the person who signed in, so **the calling identity changes**. **Scope:** no scope set is prescribed for Code; `openid RelativityRequestOrigin UserInfo UserInfoAccess AuthenticationID RelativityWeb` works, **" + OAuth2ClientConfigurationBlocks.NSERIO_VERIFIED_LABEL + "**, as working and not as minimal.\n\n**Variant 1 — a server-side web application (Flask, Django, FastAPI or similar)**\n\n**Precondition: this variant is for code that runs in Python as a web server and acts for people who sign in to it. It is not for a command-line tool (variant 2).**\n\nStandard server-side Code flow. A login route builds the authorize address with `state` (and the PKCE verifier where a refresh token is wanted), stores them against the visitor's session and redirects; RelativityOne redirects to the **callback route**, registered as an exact HTTPS URL; the callback checks `state` and exchanges the code with the secret. **The logic is framework-neutral; the framework supplies the routes and the session**, and the sketch shows the callback as a handler of the standard library's `http.server` so that it has no dependency: in a Flask, Django or FastAPI application the same two functions are called from the login and callback views, and the dictionary of pending logins is the framework's session (Flask's session holds data in a signed cookie that the user can read but not modify, so **NSerio recommends that it hold neither the verifier nor a token**; Django's session framework \"stores data on the server side\" with the cookie holding only a session ID, \"unless you're using the cookie based backend\" (`SESSION_ENGINE` set to `django.contrib.sessions.backends.signed_cookies`), whose data \"can be read by the client\"; the pages of FastAPI read name no session store). **No framework code is shown: none is installed and none is verified.**\n\n"
		+ _FENCE_PYTHON_OPEN + AUTHORIZATION_CODE_WEB_SERVER_BLOCK + _FENCE_CLOSE
		+ "\n\n`state` is consumed once and compared on return in constant time (`secrets.compare_digest`); a request with no pending entry or a different `state` gets no exchange. **The scope is not prescribed** (the sketch sends none). **Where the secret travels in the token request** (the body, as sketched, or HTTP Basic client authentication) is **not verified**; RFC 6749 allows either form for a client password. The sketch does not show the HTTP server's TLS, the session cookie or a session store that survives more than one process (the module dictionary does not). **The handler only records the returned `code` and `state`; in a real application the callback view calls `complete_login` with the visitor's own session identifier**, so that the pending entry of that visitor, and no other, is consumed. **Tokens stay on the server**, in a session store that is not exposed to a request.\n\n**The `http.server` handler is a shape, not a deployment.** The `http.server` page states that the module \"is not recommended for production. It only implements basic security checks\" (Python 3.15.2). The handler above shows what the callback reads and answers, so that the sketch has no dependency; **an application that serves people runs the callback as a view of the framework it already uses** (Flask, Django or FastAPI), behind the TLS and the session store that framework and its server provide.\n\n**Variant 2 — a command-line or desktop tool**\n\n**Precondition: this variant is for code that runs on a person's own machine, as a command-line tool or a desktop shell, and signs that person in. It is not for a web server (variant 1).**\n\nThe process starts a listener from the standard library, bound to **`127.0.0.1` only**, on the **fixed registered port**, for **one callback request**; opens the system browser on the authorize address **and always prints the address too**, so a machine with no browser, an SSH session or a failed launch has a manual route; reads `code` and `state` from the callback request, closes the listener, checks `state`, and exchanges the code as variant 1's `complete_login` does (with the redirect URI `http://127.0.0.1:8123/callback`).\n\n"
		+ _FENCE_PYTHON_OPEN + AUTHORIZATION_CODE_COMMAND_LINE_BLOCK + _FENCE_CLOSE
		+ "\n\n**Rules for the listener:** bind to loopback only; **check `state`** (another local process can connect to a fixed, advertised port); close the listener after the callback request; and handle a busy port. **A busy port raises `OSError` only if the listener does not reuse the address**: `socketserver` documents the class variable `allow_reuse_address` (\"This defaults to False, and can be set in subclasses to change the policy\"), and `http.server.HTTPServer` sets it to a true value, with which a second bind to the same address and port raised no error on Windows (checked on Windows; the behaviour on Linux and macOS is unchecked). The sketch therefore binds a subclass with `allow_reuse_address = False`, catches the `OSError`, and prints the address and a clear message instead of waiting. `handle_request` processes one request and returns after `timeout` seconds if none arrives, which is why the sketch loops until the callback or the **overall deadline** `WAIT_SECONDS` (`timeout` bounds each wait only, and the deadline bounds the whole wait): a stray request, such as a browser asking for an icon, is answered `404` and does not end the wait. `webbrowser.open` shows the address \"using the default browser\" and returns whether one was launched. **Where tokens are kept:** a file with restricted permissions or the operating system's keychain (unchecked on every system; no keychain package is named here). **The extractable-secret statement:** a command-line tool or desktop program shipped to people carries its client secret in a form anyone who holds the distribution can extract, and it protects nothing against the person running it (a documentation gap: Relativity documents no recommendation for a distributed client). **Compensating guidance** (NSerio recommendation): Trusted IPs where the users' outbound addresses are known and stable (checked at token issuance; applicability to Code *inferred*); the 60-minute Access Token Lifetime; refresh tokens only when needed; and **one OAuth2 Client per distribution**, so a leaked secret can be disabled and reset alone.";

	/// <summary>PY-001 generic source form.</summary>
	public const string PY_001_SOURCE_FORM =
		"**Applies to:** a Python script, service or tool; `requests`, `httpx`, `aiohttp`.\n\n"
		+ _FENCE_PYTHON_OPEN
		+ "response = requests.get(url, auth=HTTPBasicAuth(user_name, secret))\n"
		+ "client = httpx.Client(auth=httpx.BasicAuth(username=user_name, password=secret))\n"
		+ "session = aiohttp.ClientSession(auth=aiohttp.BasicAuth(user_name, secret))"
		+ _FENCE_CLOSE;

	/// <summary>PY-001 target form under token authentication.</summary>
	public const string PY_001_TARGET_FORM =
		_DESTINATION_QUESTION
		+ "\n\n"
		+ _FENCE_PYTHON_OPEN
		+ "CSRF_HEADER_VALUE = \"-\"\n"
		+ "response = requests.get(url, headers={\"Authorization\": f\"Bearer {access_token}\", \"X-CSRF-Header\": CSRF_HEADER_VALUE})\n"
		+ "client = httpx.Client(headers={\"Authorization\": f\"Bearer {access_token}\", \"X-CSRF-Header\": CSRF_HEADER_VALUE})\n"
		+ "session = aiohttp.ClientSession(headers={\"Authorization\": f\"Bearer {access_token}\", \"X-CSRF-Header\": CSRF_HEADER_VALUE})"
		+ _FENCE_CLOSE
		+ "\n\nRemove the helper object and the `auth` keyword. A client or session created once with a `Bearer` header holds a token that expires: set the header per request, or create the client again when the token is acquired again.";

	/// <summary>PY-001 verification note.</summary>
	public const string PY_001_VERIFICATION_NOTE =
		"**Verification note.** **A token has a finite lifetime**: acquire again within it. **The calling identity changes**; the token-acquisition part says to whom. **Other uses may not appear as matches**: a helper held in a variable and handed to a client elsewhere, and a helper renamed at import, are not read. The helper classes are documented for `requests` 2.34.2, `httpx` (no version shown) and `aiohttp` 3.14.5 (where `BasicAuth` is **deprecated since 3.14**); other versions are unchecked.";

	/// <summary>PY-002 generic source form.</summary>
	public const string PY_002_SOURCE_FORM =
		"**Applies to:** a Python script, service or tool; `requests`, `httpx`.\n\n"
		+ _FENCE_PYTHON_OPEN
		+ "response = requests.get(url, auth=(user_name, secret))\n"
		+ "client = httpx.AsyncClient(auth=(user_name, secret))"
		+ _FENCE_CLOSE;

	/// <summary>PY-002 target form under token authentication.</summary>
	public const string PY_002_TARGET_FORM =
		_DESTINATION_QUESTION
		+ "\n\n"
		+ _FENCE_PYTHON_OPEN
		+ "CSRF_HEADER_VALUE = \"-\"\n"
		+ "response = requests.get(url, headers={\"Authorization\": f\"Bearer {access_token}\", \"X-CSRF-Header\": CSRF_HEADER_VALUE})\n"
		+ "client = httpx.AsyncClient(headers={\"Authorization\": f\"Bearer {access_token}\", \"X-CSRF-Header\": CSRF_HEADER_VALUE})"
		+ _FENCE_CLOSE
		+ "\n\nRemove the `auth` tuple, also from a client's constructor and from a `Session`.";

	/// <summary>PY-002 verification note.</summary>
	public const string PY_002_VERIFICATION_NOTE =
		"**Verification note.** **A token has a finite lifetime.** **The calling identity changes.** **The scheme changes from `Basic` to `Bearer`**: a `Bearer` header is not read by this rule again. The tuple form is documented for `requests` 2.34.2 and `httpx` (no version shown); other versions are unchecked.";

	/// <summary>PY-003 generic source form.</summary>
	public const string PY_003_SOURCE_FORM =
		"**Applies to:** a Python script, service or tool; `requests`, `httpx`, `aiohttp`, `urllib3`, `urllib.request`, `http.client`.\n\n"
		+ _FENCE_PYTHON_OPEN
		+ "response = requests.get(url, headers={\"Authorization\": \"Basic \" + encoded_pair})\n"
		+ "session.headers[\"Authorization\"] = f\"Basic {encoded_pair}\"\n"
		+ "request.add_header(\"Authorization\", \"Basic %s\" % encoded_pair)"
		+ _FENCE_CLOSE;

	/// <summary>PY-003 target form under token authentication.</summary>
	public const string PY_003_TARGET_FORM =
		_DESTINATION_QUESTION
		+ "\n\n"
		+ _FENCE_PYTHON_OPEN
		+ "CSRF_HEADER_VALUE = \"-\"\n"
		+ "response = requests.get(url, headers={\"Authorization\": f\"Bearer {access_token}\", \"X-CSRF-Header\": CSRF_HEADER_VALUE})\n"
		+ "session.headers[\"Authorization\"] = f\"Bearer {access_token}\"\n"
		+ "session.headers[\"X-CSRF-Header\"] = CSRF_HEADER_VALUE\n"
		+ "request.add_header(\"Authorization\", f\"Bearer {access_token}\")\n"
		+ "request.add_header(\"X-CSRF-Header\", CSRF_HEADER_VALUE)"
		+ _FENCE_CLOSE
		+ "\n\nRemove the code that built the encoded pair. The same replacement applies to a header set with `putheader(`, to `PoolManager(headers=…)` and to `ClientSession(headers=…)`: the value changes from `Basic …` to `Bearer …`.";

	/// <summary>PY-003 verification note.</summary>
	public const string PY_003_VERIFICATION_NOTE =
		"**Verification note.** **A token has a finite lifetime.** **The calling identity changes.** **The scheme changes from `Basic` to `Bearer`.** **A header set by a function the rule did not read, a merged mapping or a hook is not read**. The header carriers are documented for the pages read and their versions; other versions are unchecked.";

	/// <summary>PY-004 generic source form.</summary>
	public const string PY_004_SOURCE_FORM =
		"**Applies to:** a Python script or service using the standard library's `urllib.request`.\n\n"
		+ _FENCE_PYTHON_OPEN
		+ "handler = urllib.request.HTTPBasicAuthHandler(password_manager)\n"
		+ "opener = urllib.request.build_opener(handler)\n"
		+ "response = opener.open(url)"
		+ _FENCE_CLOSE;

	/// <summary>PY-004 target form under token authentication.</summary>
	public const string PY_004_TARGET_FORM =
		_DESTINATION_QUESTION
		+ "\n\n"
		+ _FENCE_PYTHON_OPEN
		+ "CSRF_HEADER_VALUE = \"-\"\n"
		+ "request = urllib.request.Request(url, headers={\"Authorization\": f\"Bearer {access_token}\", \"X-CSRF-Header\": CSRF_HEADER_VALUE})\n"
		+ "response = urllib.request.urlopen(request)"
		+ _FENCE_CLOSE
		+ "\n\nRemove the handler, the password manager and the opener built for them. `Request` accepts `headers`.";

	/// <summary>PY-004 verification note.</summary>
	public const string PY_004_VERIFICATION_NOTE =
		"**Verification note.** **A token has a finite lifetime.** **The calling identity changes.** **This match records the handler; the credential given to the password manager is its own match at `PY-009`**, and both are removed by this change. The handler answers a Basic challenge, and **RelativityOne does not challenge** (**" + OAuth2ClientConfigurationBlocks.NSERIO_VERIFIED_LABEL + "**, on one instance): the handler was either never exercised against RelativityOne or the code sends Basic by another route; confirm before deleting. Behaviour before Python 3.15 is unchecked.";

	/// <summary>PY-005 generic source form.</summary>
	public const string PY_005_SOURCE_FORM =
		"**Applies to:** a Python script or service; `urllib3`.\n\n"
		+ _FENCE_PYTHON_OPEN
		+ "headers = urllib3.util.make_headers(basic_auth=f\"{user_name}:{secret}\")\n"
		+ "http = urllib3.PoolManager(headers=headers)"
		+ _FENCE_CLOSE;

	/// <summary>PY-005 target form under token authentication.</summary>
	public const string PY_005_TARGET_FORM =
		_DESTINATION_QUESTION
		+ "\n\n"
		+ _FENCE_PYTHON_OPEN
		+ "CSRF_HEADER_VALUE = \"-\"\n"
		+ "http = urllib3.PoolManager(headers={\"Authorization\": f\"Bearer {access_token}\", \"X-CSRF-Header\": CSRF_HEADER_VALUE})\n"
		+ "response = http.request(\"GET\", url)"
		+ _FENCE_CLOSE
		+ "\n\nRemove the `make_headers(basic_auth=…)` call. The page read for `make_headers` documents `basic_auth` and `proxy_basic_auth` and **no bearer option**, so the header is written as a mapping.";

	/// <summary>PY-005 verification note.</summary>
	public const string PY_005_VERIFICATION_NOTE =
		"**Verification note.** **A token has a finite lifetime**: a `PoolManager` created once holds the header it was given; pass `headers` per `request` call. **The calling identity changes.** Documented for urllib3 2.8.0; earlier majors are unchecked.";

	/// <summary>PY-006 generic source form.</summary>
	public const string PY_006_SOURCE_FORM =
		"**Applies to:** a Python script, service or tool; the receiver of the tuple is not named (a `Session`, a client held in a variable, an aliased module).\n\n"
		+ _FENCE_PYTHON_OPEN
		+ "response = session.get(url, auth=(user_name, secret))\n"
		+ "session.auth = (user_name, secret)"
		+ _FENCE_CLOSE;

	/// <summary>PY-006 target form under token authentication.</summary>
	public const string PY_006_TARGET_FORM =
		_DESTINATION_QUESTION
		+ "\n\n"
		+ _FENCE_PYTHON_OPEN
		+ "CSRF_HEADER_VALUE = \"-\"\n"
		+ "response = session.get(url, headers={\"Authorization\": f\"Bearer {access_token}\", \"X-CSRF-Header\": CSRF_HEADER_VALUE})\n"
		+ "session.headers.update({\"Authorization\": f\"Bearer {access_token}\", \"X-CSRF-Header\": CSRF_HEADER_VALUE})"
		+ _FENCE_CLOSE
		+ "\n\nRemove the `auth` tuple and the `auth` attribute assignment. **If the receiver is not an HTTP client** (a database driver, a graph database or a message broker takes the same keyword for its own login), this match is not Basic Authentication to Relativity and the migration is that service's own: **the rule cannot tell**.";

	/// <summary>PY-006 verification note.</summary>
	public const string PY_006_VERIFICATION_NOTE =
		"**Verification note.** **This match is Medium because the receiver is not named**: the tuple is documented as Basic for a named `requests` or `httpx` call and not for another receiver. `Session.headers.update` and the session defaults are documented for `requests` 2.34.2. **A token has a finite lifetime.** **The calling identity changes.**";

	/// <summary>PY-007 generic source form.</summary>
	public const string PY_007_SOURCE_FORM =
		"**Applies to:** a Python script or service that builds a Basic credential by hand.\n\n"
		+ _FENCE_PYTHON_OPEN
		+ "token = base64.b64encode(f\"{user_name}:{secret}\".encode()).decode()\n"
		+ "headers = {\"Authorization\": \"Basic \" + token}"
		+ _FENCE_CLOSE;

	/// <summary>PY-007 target form under token authentication.</summary>
	public const string PY_007_TARGET_FORM =
		_DESTINATION_QUESTION
		+ "\n\n"
		+ _FENCE_PYTHON_OPEN
		+ "CSRF_HEADER_VALUE = \"-\"\n"
		+ "headers = {\"Authorization\": f\"Bearer {access_token}\", \"X-CSRF-Header\": CSRF_HEADER_VALUE}"
		+ _FENCE_CLOSE
		+ "\n\nDelete the encoder call and the pair it builds; nothing about the user name and password survives in the code.";

	/// <summary>PY-007 verification note.</summary>
	public const string PY_007_VERIFICATION_NOTE =
		"**Verification note.** **This match is Medium: the call encodes a `user:password` pair and the rule saw `Basic` or `Authorization` nearby, not that it is sent to RelativityOne.** **A split construction** (the encoder in one statement, the header in another) is a second match that nothing merges. **A token has a finite lifetime.** **The calling identity changes.**";

	/// <summary>PY-008 generic source form.</summary>
	public const string PY_008_SOURCE_FORM =
		"**Applies to:** a Python script or service using `aiohttp` 3.14 or later.\n\n"
		+ _FENCE_PYTHON_OPEN
		+ "headers = {\"Authorization\": aiohttp.encode_basic_auth(user_name, secret)}"
		+ _FENCE_CLOSE;

	/// <summary>PY-008 target form under token authentication.</summary>
	public const string PY_008_TARGET_FORM =
		_DESTINATION_QUESTION
		+ "\n\n"
		+ _FENCE_PYTHON_OPEN
		+ "CSRF_HEADER_VALUE = \"-\"\n"
		+ "headers = {\"Authorization\": f\"Bearer {access_token}\", \"X-CSRF-Header\": CSRF_HEADER_VALUE}"
		+ _FENCE_CLOSE
		+ "\n\nRemove the `encode_basic_auth` call.";

	/// <summary>PY-008 verification note.</summary>
	public const string PY_008_VERIFICATION_NOTE =
		"**Verification note.** **`encode_basic_auth` also builds a `Proxy-Authorization` value**: **if the value is a proxy header, it is a proxy credential and not a Relativity credential, and no migration to RelativityOne applies** (the class cannot see the header name). The function was **added in aiohttp 3.14**. **A token has a finite lifetime.** **The calling identity changes.**";

	/// <summary>PY-009 generic source form.</summary>
	public const string PY_009_SOURCE_FORM =
		"**Applies to:** a Python script or service using the standard library's `urllib.request`.\n\n"
		+ _FENCE_PYTHON_OPEN
		+ "manager = urllib.request.HTTPPasswordMgrWithDefaultRealm()\n"
		+ "manager.add_password(None, \"https://example.test/\", user_name, secret)"
		+ _FENCE_CLOSE;

	/// <summary>PY-009 target form under token authentication.</summary>
	public const string PY_009_TARGET_FORM =
		_DESTINATION_QUESTION
		+ "\n\n"
		+ _FENCE_PYTHON_OPEN
		+ "CSRF_HEADER_VALUE = \"-\"\n"
		+ "request = urllib.request.Request(url, headers={\"Authorization\": f\"Bearer {access_token}\", \"X-CSRF-Header\": CSRF_HEADER_VALUE})\n"
		+ "response = urllib.request.urlopen(request)"
		+ _FENCE_CLOSE
		+ "\n\nRemove the password manager, its `add_password` calls and the handler that receives it (`PY-004`).";

	/// <summary>PY-009 verification note.</summary>
	public const string PY_009_VERIFICATION_NOTE =
		"**Verification note.** **A password manager is scheme-agnostic**: **the rule cannot tell whether the handler that receives it is Basic, Digest or a proxy handler**; if it is not a Basic handler for RelativityOne no migration to RelativityOne applies. **A token has a finite lifetime.** **The calling identity changes.** The scheme scoping of `add_password` changed in Python 3.15: behaviour before it is unchecked.";

	/// <summary>PY-010 generic source form.</summary>
	public const string PY_010_SOURCE_FORM =
		"**Applies to:** a Python script or service; `httpx`.\n\n"
		+ _FENCE_PYTHON_OPEN
		+ "client = httpx.Client(auth=httpx.NetRCAuth())"
		+ _FENCE_CLOSE;

	/// <summary>PY-010 target form under token authentication.</summary>
	public const string PY_010_TARGET_FORM =
		_DESTINATION_QUESTION
		+ "\n\n"
		+ _FENCE_PYTHON_OPEN
		+ "CSRF_HEADER_VALUE = \"-\"\n"
		+ "client = httpx.Client(headers={\"Authorization\": f\"Bearer {access_token}\", \"X-CSRF-Header\": CSRF_HEADER_VALUE})"
		+ _FENCE_CLOSE
		+ "\n\nRemove `NetRCAuth` and the entry for the host from the `.netrc` file.";

	/// <summary>PY-010 verification note.</summary>
	public const string PY_010_VERIFICATION_NOTE =
		"**Verification note.** **No credential is in the code: it is in a file outside the repository**, which is not read; **that file still holds a password at rest and the entry must be removed or rotated** (the rotation statement below). The netrc behaviour is documented for `httpx` (no version shown); the implicit netrc forms that use no construct are not matches. **A token has a finite lifetime.** **The calling identity changes.**\n\n"
		+ ROTATION_STATEMENT;

	/// <summary>PY-011 generic source form.</summary>
	public const string PY_011_SOURCE_FORM =
		"**Applies to:** a Python script or service; `requests`, `httpx`, `aiohttp`, `urllib3`, `urllib.request`.\n\n"
		+ _FENCE_PYTHON_OPEN
		+ "response = requests.get(\"https://user_name:secret@example.test/items\")"
		+ _FENCE_CLOSE;

	/// <summary>PY-011 target form under token authentication.</summary>
	public const string PY_011_TARGET_FORM =
		_DESTINATION_QUESTION
		+ "\n\n"
		+ _FENCE_PYTHON_OPEN
		+ "CSRF_HEADER_VALUE = \"-\"\n"
		+ "response = requests.get(\"https://example.test/items\", headers={\"Authorization\": f\"Bearer {access_token}\", \"X-CSRF-Header\": CSRF_HEADER_VALUE})"
		+ _FENCE_CLOSE
		+ "\n\nRemove the user name and password from the address. **A credential in an address is also in logs, in a process list and in the repository history**: apply the rotation statement below.";

	/// <summary>PY-011 verification note.</summary>
	public const string PY_011_VERIFICATION_NOTE =
		"**Verification note.** **This match is Medium**: the pages read state that a client turns the userinfo into a Basic header for **`aiohttp` only**; for `requests`, `httpx`, `urllib3` and `urllib.request` the behaviour is **unchecked** and this text does not claim it. A URL assembled from parts is not read. **A token has a finite lifetime.** **The calling identity changes.**\n\n"
		+ ROTATION_STATEMENT;

	/// <summary>PY-012 generic source form.</summary>
	public const string PY_012_SOURCE_FORM =
		"**Applies to:** a Python script or service.\n\n"
		+ _FENCE_PYTHON_OPEN
		+ "BASE_URL = \"https://user_name:secret@example.test/items\""
		+ _FENCE_CLOSE;

	/// <summary>PY-012 target form under token authentication.</summary>
	public const string PY_012_TARGET_FORM =
		_DESTINATION_QUESTION
		+ "\n\n"
		+ _FENCE_PYTHON_OPEN
		+ "BASE_URL = \"https://example.test/items\"\n"
		+ "CSRF_HEADER_VALUE = \"-\"\n"
		+ "response = requests.get(BASE_URL, headers={\"Authorization\": f\"Bearer {access_token}\", \"X-CSRF-Header\": CSRF_HEADER_VALUE})"
		+ _FENCE_CLOSE
		+ "\n\nRemove the userinfo from the held address and apply the rotation statement below.";

	/// <summary>PY-012 verification note.</summary>
	public const string PY_012_VERIFICATION_NOTE =
		"**Verification note.** **This match is Medium: the consumer of the address is not visible**, so the rule cannot say how it is sent. **A token has a finite lifetime.** **The calling identity changes.**\n\n"
		+ ROTATION_STATEMENT;

	/// <summary>PY-013 generic source form.</summary>
	public const string PY_013_SOURCE_FORM =
		"**Applies to:** a Python script, service or tool.\n\n"
		+ _FENCE_PYTHON_OPEN
		+ "scheme_value = \"Basic \" + encoded_pair\n"
		+ "response = requests.get(url, headers={\"Authorization\": scheme_value})"
		+ _FENCE_CLOSE;

	/// <summary>PY-013 target form under token authentication.</summary>
	public const string PY_013_TARGET_FORM =
		_DESTINATION_QUESTION
		+ "\n\n"
		+ _FENCE_PYTHON_OPEN
		+ "CSRF_HEADER_VALUE = \"-\"\n"
		+ "response = requests.get(url, headers={\"Authorization\": f\"Bearer {access_token}\", \"X-CSRF-Header\": CSRF_HEADER_VALUE})"
		+ _FENCE_CLOSE
		+ "\n\nRemove the variable and the code that built the pair.";

	/// <summary>PY-013 verification note.</summary>
	public const string PY_013_VERIFICATION_NOTE =
		"**Verification note.** **The rule resolved the header's variable to a scheme of `Basic` in this file** (one declaration, before the use); a variable it cannot resolve is not read. **A token has a finite lifetime.** **The calling identity changes.**";
}

/// <summary>
/// The `Python` analyzer's template provider and fallback texts: thirteen classes resolve to their entry under either flow; the Low class, and any identifier the analyzer does not declare, resolve to null, which routes a
/// Low finding to manual review and sends a High or Medium finding of an undeclared class to the language's fallback text.
/// </summary>
internal static class PythonTemplateDeclarations
{
	private const string _PARAGRAPH_SEPARATOR = "\n\n";

	private const string _PY_001 = "PY-001";
	private const string _PY_002 = "PY-002";
	private const string _PY_003 = "PY-003";
	private const string _PY_004 = "PY-004";
	private const string _PY_005 = "PY-005";
	private const string _PY_006 = "PY-006";
	private const string _PY_007 = "PY-007";
	private const string _PY_008 = "PY-008";
	private const string _PY_009 = "PY-009";
	private const string _PY_010 = "PY-010";
	private const string _PY_011 = "PY-011";
	private const string _PY_012 = "PY-012";
	private const string _PY_013 = "PY-013";

	private static readonly FrozenDictionary<string, RecommendationTemplate> _clientCredentialsTemplatesByPatternClass =
		BuildTemplates(PythonRecommendationTemplates.CLIENT_CREDENTIALS_TOKEN_ACQUISITION);

	private static readonly FrozenDictionary<string, RecommendationTemplate> _authorizationCodeTemplatesByPatternClass =
		BuildTemplates(PythonRecommendationTemplates.AUTHORIZATION_CODE_TOKEN_ACQUISITION);

	/// <summary>The language's own text for the generic recommendation route, per flow.</summary>
	internal static FrozenDictionary<CredentialFlow, string> FallbackTextByFlow { get; } = new Dictionary<CredentialFlow, string>
	{
		[CredentialFlow.ClientCredentials] = PythonRecommendationTemplates.PYTHON_LANGUAGE_FALLBACK_TEXT,
		[CredentialFlow.AuthorizationCode] = PythonRecommendationTemplates.PYTHON_CODE_LANGUAGE_FALLBACK_TEXT
	}.ToFrozenDictionary();

	/// <summary>The template of a class for a flow, or null for the Low class and for a class this analyzer does not declare.</summary>
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
			[_PY_001] = Template(PythonRecommendationTemplates.PY_001_SOURCE_FORM, PythonRecommendationTemplates.PY_001_TARGET_FORM, PythonRecommendationTemplates.CREDENTIAL_PROVENANCE, PythonRecommendationTemplates.PY_001_VERIFICATION_NOTE, tokenAcquisition),
			[_PY_002] = Template(PythonRecommendationTemplates.PY_002_SOURCE_FORM, PythonRecommendationTemplates.PY_002_TARGET_FORM, PythonRecommendationTemplates.CREDENTIAL_PROVENANCE, PythonRecommendationTemplates.PY_002_VERIFICATION_NOTE, tokenAcquisition),
			[_PY_003] = Template(PythonRecommendationTemplates.PY_003_SOURCE_FORM, PythonRecommendationTemplates.PY_003_TARGET_FORM, PythonRecommendationTemplates.CREDENTIAL_PROVENANCE, PythonRecommendationTemplates.PY_003_VERIFICATION_NOTE, tokenAcquisition),
			[_PY_004] = Template(PythonRecommendationTemplates.PY_004_SOURCE_FORM, PythonRecommendationTemplates.PY_004_TARGET_FORM, PythonRecommendationTemplates.CREDENTIAL_PROVENANCE, PythonRecommendationTemplates.PY_004_VERIFICATION_NOTE, tokenAcquisition),
			[_PY_005] = Template(PythonRecommendationTemplates.PY_005_SOURCE_FORM, PythonRecommendationTemplates.PY_005_TARGET_FORM, PythonRecommendationTemplates.CREDENTIAL_PROVENANCE, PythonRecommendationTemplates.PY_005_VERIFICATION_NOTE, tokenAcquisition),
			[_PY_006] = Template(PythonRecommendationTemplates.PY_006_SOURCE_FORM, PythonRecommendationTemplates.PY_006_TARGET_FORM, PythonRecommendationTemplates.CREDENTIAL_PROVENANCE, PythonRecommendationTemplates.PY_006_VERIFICATION_NOTE, tokenAcquisition),
			[_PY_007] = Template(PythonRecommendationTemplates.PY_007_SOURCE_FORM, PythonRecommendationTemplates.PY_007_TARGET_FORM, PythonRecommendationTemplates.CREDENTIAL_PROVENANCE, PythonRecommendationTemplates.PY_007_VERIFICATION_NOTE, tokenAcquisition),
			[_PY_008] = Template(PythonRecommendationTemplates.PY_008_SOURCE_FORM, PythonRecommendationTemplates.PY_008_TARGET_FORM, PythonRecommendationTemplates.CREDENTIAL_PROVENANCE, PythonRecommendationTemplates.PY_008_VERIFICATION_NOTE, tokenAcquisition),
			[_PY_009] = Template(PythonRecommendationTemplates.PY_009_SOURCE_FORM, PythonRecommendationTemplates.PY_009_TARGET_FORM, PythonRecommendationTemplates.CREDENTIAL_PROVENANCE, PythonRecommendationTemplates.PY_009_VERIFICATION_NOTE, tokenAcquisition),
			[_PY_010] = Template(PythonRecommendationTemplates.PY_010_SOURCE_FORM, PythonRecommendationTemplates.PY_010_TARGET_FORM, PythonRecommendationTemplates.CREDENTIAL_PROVENANCE, PythonRecommendationTemplates.PY_010_VERIFICATION_NOTE, tokenAcquisition),
			[_PY_011] = Template(PythonRecommendationTemplates.PY_011_SOURCE_FORM, PythonRecommendationTemplates.PY_011_TARGET_FORM, PythonRecommendationTemplates.CREDENTIAL_PROVENANCE, PythonRecommendationTemplates.PY_011_VERIFICATION_NOTE, tokenAcquisition),
			[_PY_012] = Template(PythonRecommendationTemplates.PY_012_SOURCE_FORM, PythonRecommendationTemplates.PY_012_TARGET_FORM, PythonRecommendationTemplates.CREDENTIAL_PROVENANCE, PythonRecommendationTemplates.PY_012_VERIFICATION_NOTE, tokenAcquisition),
			[_PY_013] = Template(PythonRecommendationTemplates.PY_013_SOURCE_FORM, PythonRecommendationTemplates.PY_013_TARGET_FORM, PythonRecommendationTemplates.CREDENTIAL_PROVENANCE, PythonRecommendationTemplates.PY_013_VERIFICATION_NOTE, tokenAcquisition)
		};

		return templates.ToFrozenDictionary(StringComparer.Ordinal);
	}

	/// <summary>The credential provenance is the shared share followed by part 2 for the flow, as two paragraphs.</summary>
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
