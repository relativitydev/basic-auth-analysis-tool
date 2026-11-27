# Migration Suggestions for Python (Part C of the Migration Documentation)

[Documentation index](../README.md#documentation)

**Contents**

<!-- generated:begin table-of-contents -->
- [Choosing the variant for your application](#choosing-the-variant-for-your-application)
	- [Client Credentials: one variant](#client-credentials-one-variant)
	- [Authorization Code: two variants](#authorization-code-two-variants)
- [Judging a Low-confidence match](#judging-a-low-confidence-match)
	- [Comments](#comments)
	- [Documentation strings](#documentation-strings)
	- [Test fixtures and mocks](#test-fixtures-and-mocks)
	- [Sample and example code](#sample-and-example-code)
	- [Vendored and generated files](#vendored-and-generated-files)
	- [Deployment folders](#deployment-folders)
	- [PY-014 · BasicLiteralInAuthorizationMapping](#py-014--basicliteralinauthorizationmapping)
- [Migration suggestions by pattern class](#migration-suggestions-by-pattern-class)
	- [PY-001 · BasicAuthHelperConstruction](#py-001--basicauthhelperconstruction)
	- [PY-002 · NamedClientAuthTuple](#py-002--namedclientauthtuple)
	- [PY-003 · AuthorizationHeaderBasicScheme](#py-003--authorizationheaderbasicscheme)
	- [PY-004 · UrllibBasicAuthHandlerConstruction](#py-004--urllibbasicauthhandlerconstruction)
	- [PY-005 · Urllib3MakeHeadersBasicAuth](#py-005--urllib3makeheadersbasicauth)
	- [PY-006 · UnnamedAuthTuple](#py-006--unnamedauthtuple)
	- [PY-007 · Base64UserPasswordPair](#py-007--base64userpasswordpair)
	- [PY-008 · AiohttpEncodeBasicAuth](#py-008--aiohttpencodebasicauth)
	- [PY-009 · PasswordManagerAddPassword](#py-009--passwordmanageraddpassword)
	- [PY-010 · HttpxNetrcAuth](#py-010--httpxnetrcauth)
	- [PY-011 · CredentialsInRequestUrl](#py-011--credentialsinrequesturl)
	- [PY-012 · UrlUserinfoHeldLiteral](#py-012--urluserinfoheldliteral)
	- [PY-013 · AuthorizationHeaderValueHeldInVariable](#py-013--authorizationheadervalueheldinvariable)
- [Generic suggestion](#generic-suggestion)
	- [Client Credentials](#client-credentials-13)
	- [Authorization Code](#authorization-code-13)
- [OAuth2 Client for Client Credentials](#oauth2-client-for-client-credentials)
- [OAuth2 Client for Authorization Code](#oauth2-client-for-authorization-code)
<!-- generated:end table-of-contents -->

This is Part C of the Migration Documentation, for developers who have found Basic Authentication in their Python code with [Part A](part-a-python.md). You use it without running anything: for each pattern class and each OAuth 2.0 flow it shows the code to replace, the code that replaces it, how to obtain and cache the token, and the OAuth2 Client to create. Store the new secrets as [Part B](part-b-secret-management.md) describes. New here? Start with the [Migration Documentation overview](README.md).

This document has three kinds of text:

- **Guidance.** Which variant of a recommendation fits your application, and how to judge a match that Part A rates Low confidence.
- **Recommendations.** Each of the 13 pattern classes that produce a recommendation (13 of the 14) has two entries, one for each flow, and each flow has one generic recommendation. The OAuth2 Client settings are in the two "OAuth2 Client for ..." sections at the end of this document, and each entry links to the section of its flow.
- **Pointers.** Part A ([`part-a-python.md`](part-a-python.md)) describes each pattern and how to search for it. Secret Management (Part B of the Migration Documentation, [`part-b-secret-management.md`](part-b-secret-management.md)) says where the new secrets go.

Every example uses placeholders such as `HOST`, `user_name`, `secret` and `access_token`. Replace them with your own values, and keep real secrets out of source code and out of a tracked configuration file.

The two flows are the only ones recommended here:

- **Client Credentials** is the flow when no person signs in. The code sends its client ID and client secret to the token endpoint and receives an access token. No user is involved, and every call runs as the OAuth2 Client's Context User.
- **Authorization Code** is the flow that the RelativityOne OAuth2 Client form names Code. The code sends a signed-in user to RelativityOne, receives an authorization code at a registered Redirect URL, and exchanges it for a token that carries that user's permissions.

These entries assume that Python reaches RelativityOne through its REST endpoints with an HTTP client, because they rely on no Relativity-published Python client or package. The text uses the standard library and the four HTTP clients `requests`, `httpx`, `aiohttp` and `urllib3`, and names no web framework in code. Where the entries show code, the blocks are fenced `python`, and a token is sent as `Authorization: Bearer`, with `X-CSRF-Header: -` on REST and Kepler calls. **Every entry asks first whether the destination is RelativityOne**, because the code does not show which host a call goes to: a Basic credential sent to another service is a real exposure, and the correct migration for that service is its own.

**Status of the examples.** The code blocks of the entries are examples and are unchecked: unless an entry marks a behaviour as checked, they have not been run, and none has been run against RelativityOne. What each client does is described from its documentation and is unchecked for versions other than the one documented. The framework text (Flask, Django and FastAPI) is not code in the entries and is not verified.

**Choosing the flow.** The "Which flow" paragraph of each class in Part A says when a match belongs to each flow. If your RelativityOne instance already has an OAuth2 Client for this integration, use the flow of that client. The flow is fixed when an OAuth2 Client is created and cannot be changed when the client is edited ([Relativity's OAuth2 clients help page](https://help.relativity.com/RelativityOne/Content/Relativity/Authentication/OAuth2_clients.htm)), so moving an integration from one flow to the other means creating a new OAuth2 Client.

## Choosing the variant for your application

A file alone does not show whether it runs in a web server, in a command-line tool or as a job, so the token-acquisition text of a recommendation has **variants by application type**, and each variant opens with a **precondition** sentence. Read the precondition of each variant and discard the ones that do not describe your code. The same variants appear in every class entry of the flow.

### Client Credentials: one variant

- **A Python process acting as itself.** For a Python job, service or script that does not act for a signed-in person. The process sends the form-encoded `POST` to the token endpoint with its client ID and client secret, caches the token, and acquires a new one on expiry. Client Credentials offers no refresh tokens, so you re-acquire and do not refresh. The token endpoint is documented for this grant. The client ID and client secret come from the process environment or a secret store, never from source. If a person signs in, use Authorization Code.

### Authorization Code: two variants

- **Variant 1: a server-side web application.** For code that runs in Python as a web server and acts for people who sign in to it. A login route builds the authorize address with `state` (and the PKCE verifier where a refresh token is wanted), stores them against the visitor's session and redirects; the callback route, registered as one exact HTTPS URL, checks `state` and exchanges the code with the secret. The logic is framework-neutral and the framework supplies the routes and the session: the entry shows the callback as a handler of the standard library's `http.server` so that it has no dependency, and that handler is a shape and not a deployment, because the standard library's page says the module is not recommended for production. In a Flask, Django or FastAPI application the same functions are called from the login and callback views. The tokens stay on the server, in a session store that is not exposed to a request.
- **Variant 2: a command-line or desktop tool.** For code that runs on a person's own machine and signs that person in. The process starts a listener from the standard library, bound to `127.0.0.1` only, on the fixed registered port, for the first request to the callback path (a stray request is answered `404` and does not end the wait); it opens the system browser on the authorize address **and always prints the address too**; it reads `code` and `state` from that request, closes the listener, checks `state`, and exchanges the code. A client secret shipped inside a distributed command-line tool can be extracted by anyone who holds the distribution, so the recommendation adds compensating guidance. Where tokens are kept, a file with restricted permissions or the operating system's keychain, is unchecked on every system.

**The endpoints and PKCE, and what is documented.** The authorize endpoint `<host>/Relativity/Identity/connect/authorize` and the code exchange at `<host>/Relativity/Identity/connect/token` are verified by NSerio; not documented by Relativity. That a Code request that does not ask for a refresh token works without PKCE is verified by NSerio; not documented by Relativity. PKCE is required by Relativity's own documentation to obtain a refresh token. That a plain-HTTP loopback Redirect URL `http://127.0.0.1:<port>/<path>` on a fixed registered port is accepted is verified by NSerio; not documented by Relativity, and the loopback rule of the general standard that lets a server accept any port is not claimed for RelativityOne. The token endpoint of the Client Credentials grant is the one the Relativity documentation shows.

**Common to both.** Neither variant stores a client secret or a RelativityOne token in source or in a tracked file. A credential that was written into source has been public for as long as the repository history has held it. The recommendation states the exposure and the rotation:

> Treat this credential as compromised. Rotate it, and assume the repository history has published it. Completing the migration does not close this exposure; rotation does.

Device Code and Resource Owner are never recommended.

## Judging a Low-confidence match

A match is **Low** confidence when it is present in the text but unlikely to be live authenticating code. A Low match has no flow and no recommendation, and is left for manual review. **A Low level is not evidence that the code does not authenticate.** It means a person has to look. The paragraphs below say how to judge each cause and when the match still needs a migration. When it does, find the live construct it belongs to, identify its class in Part A, and follow that class's entry in [Migration suggestions by pattern class](#migration-suggestions-by-pattern-class). Whatever the cause, a literal secret inside a documentation string, a comment or a test is still a secret to remove.

### Comments

The match is inside a `#` comment, so no code runs. Read the comment. If it is commented-out code, nothing needs migrating today, but delete it or migrate it before anyone restores it, because a restored line brings Basic Authentication back. If it describes code that exists elsewhere, search for that code with Part A and migrate the live code. If it only illustrates a pattern with placeholders, no migration is needed. A comment written with no space after a colon or an operator character (`if ready:# note`) can be read as code, so the words of the comment can produce a finding that is not live; read the line before you act.

### Documentation strings

The match is inside a docstring or another string that documents or explains, such as a help message, an error message, a template or a label. The body of every string is read as text, so a construct spelled in a docstring is Low whatever the class, and a docstring with a `>>>` doctest line is Low too. The text does not authenticate anything. Check that the behaviour it describes has been migrated, and update the wording afterwards so that the documentation stops describing Basic Authentication. The text itself needs no migration. One case is different: a command inside a string that a program runs, such as a `curl` line given to a function that starts a process, is not read as code and may be live. Read such a match as a command before you dismiss it.

### Test fixtures and mocks

**Test code is not Low.** A match in test code is rated **Medium** at most, with the recommendation of its class. Test code is recognized by a folder named `test`, `tests`, `testing`, `fixture`, `fixtures` or `e2e`, by a file name that begins with `test_` or ends in `_test.py` or `_test.pyw`, by the files `conftest.py` and `tests.py`, and by a class whose base is a test-case class. A test that uses a mock library is test code because of its folder, its name or its test class, and not because of the library. A test that expects a request and reads its `Authorization` header is inbound authentication and is not a finding; a test whose expected value holds an encoder is rated Medium as test code, because the encoder is built and never sent, and needs no migration when it only feeds a mock. Judge by what the test talks to. If it feeds a mock and never reaches RelativityOne, no migration is needed, though you may want the mock to model the token flow so the test still represents the code it covers. If it is an integration test that authenticates to a real RelativityOne instance, it needs migrating with everything else, because it stops working when Basic Authentication is switched off: follow the entry of its class. A credential written literally into a test is still a secret to remove.

**A mock is Low.** A match in a mock, fake or stub file is rated Low, with no recommendation: a folder named `mock`, `mocks`, `stub`, `stubs`, `fake` or `fakes`, or a file name that begins with `mock_`, `stub_` or `fake_`, ends in `_mock.py`, `_stub.py` or `_fake.py`, or is `mock.py`, `mocks.py` or `fakes.py`. A mock is recognized by its folder or file name and by nothing else, so a mock that has a neutral name is rated at the level of its class, or at Medium when it also sits in a test folder; review it as you would a test. A file that is test code and also a mock, a sample, vendored or generated is Low. For a Low mock, judge by what it stands in for. If nothing runs it against RelativityOne, no migration is needed.

### Sample and example code

The match sits in a folder named `sample`, `samples`, `example`, `examples`, `demo` or `demos`, in any letter case. Those are the only names that count, and only as folders: a sample kept in a file whose name merely begins with the word is not rated Low and keeps the level of its class, so review it as live code. Judge by who uses it. If nobody runs the sample, it needs no migration. If people copy it as a starting point, migrate it: an example that teaches Basic Authentication spreads it to new code. Use the recommendation for the class the sample matches, and prefer the flow that fits the scenario the sample shows.

### Vendored and generated files

The match sits in a folder named `vendor`, `site-packages`, `venv`, `.venv`, `dist-packages`, `__pypackages__` or `.tox`, which holds third-party code or an installed environment, or in a file produced by a tool: a name ending in `_pb2.py` or `_pb2_grpc.py`, or a comment line within the first three lines that says the file is generated, or a comment line within the first ten lines that says it must not be edited. **Do not edit a generated file by hand:** the next build overwrites the edit. Fix the source it is built from (the definition, the template or the generator settings) and rebuild. For vendored code, upgrade the library to a version that supports token authentication, or replace it. A migration is needed when the file runs against RelativityOne in your environment. It is not needed when the file is never called, or when it is only reference material. **A large vendored or environment folder can bury a real finding in third-party code**; keep such folders out of the search where you can, and search them separately.

### Deployment folders

A folder named `Deployment` does not lower a Python finding: in Python that folder holds code that authenticates, so a file under it keeps its ordinary level and this cause does not arise for it. If a match in a deployment folder is Low all the same, read the file before you decide. A migration is needed when the file is the one that runs against RelativityOne in your application or in the deployment itself. It is not needed when the file is never run, or when it only illustrates a pattern. A credential written literally into a deployment file is still a secret to remove.

### PY-014 · BasicLiteralInAuthorizationMapping

<a id="py-014-judging"></a>

A mapping with a quoted `Authorization` key and a value that begins with the literal `Basic`, that is not seen handed to a request: a mapping built in a function and returned, held under another name, or merged into the headers later. It is rated Low because every key of a Python mapping is a string, so the match has nothing in code to anchor on, and the text does not show that the mapping ever reaches a client. **A literal `Basic` header held in a mapping is the commonest way a fixed credential is written, so do not dismiss it.** Find where the mapping goes. If it is given to a client that calls RelativityOne, the construct is a header write: identify the class that matches the call site in Part A (usually `PY-003`) and follow its entry, and decide the flow from who supplies the credential. If the mapping goes to another service, the migration is that service's own and needs no change for RelativityOne. A token written after `Basic` in the mapping is a credential in the source, a secret to remove and rotate even when the mapping is dead code.

## Migration suggestions by pattern class

Each of the 13 pattern classes that produce a recommendation (13 of the 14) has two entries, one for each flow. Choose the entry for the flow that fits your integration; the "Which flow" paragraph of the class in Part A says when a match belongs to each. Every entry contains the code being replaced, the migrated code, the question of whether the destination is RelativityOne, a description of token acquisition, caching and expiry handling in the variants of [Choosing the variant for your application](#choosing-the-variant-for-your-application), the flow the entry is for, the OAuth2 Client to create or confirm (a link to its settings in [OAuth2 Client for Client Credentials](#oauth2-client-for-client-credentials) or [OAuth2 Client for Authorization Code](#oauth2-client-for-authorization-code)), and the pointer to Secret Management (Part B of the Migration Documentation). Every Authorization Code entry adds a description of refresh-token handling, of PKCE, and of its two variants.

**Part 1, part 2 and part 3.** The text of an entry refers to its own pieces by number. Each piece has a label in the entry:

- **Part 1** is the change at the point of use: the text under the label *Under token authentication*, with its migrated code.
- **Part 2** is the token acquisition: the part of the text under the label *Credentials* that follows the paragraph *Where the credentials come from*. It describes the request to the token endpoint, the caching of the token and what to do when it expires. A Client Credentials entry starts it with the text that follows the question of the destination, and an Authorization Code entry with *Common to both variants*.
- **Part 3** is the OAuth2 Client settings, under the label *OAuth2 Client* at the end of the entry.

The labels *Source form* (the construct being replaced) and *Verification* (what to check before you deploy) are not numbered.

The Low class, `PY-014`, has no entry because it produces no recommendation. It is covered under [Judging a Low-confidence match](#py-014-judging).

### PY-001 · BasicAuthHelperConstruction

<a id="py-001-migration"></a>

Remove the `HTTPBasicAuth` or `BasicAuth` helper object and the `auth` argument that holds it, and send a Bearer token in an `Authorization` header instead. A client or session created once with a Bearer header holds a token that expires, so set the header on each request, or create the client again when the token is acquired again.

#### Client Credentials

<!-- generated:begin PY-001 client-credentials -->
**Flow.** This entry is for Client Credentials. The "Which flow" paragraph of PY-001 in [Part A](part-a-python.md#py-001) says when a match belongs to this flow.

Source form: **Applies to:** a Python script, service or tool; `requests`, `httpx`, `aiohttp`.

```python
response = requests.get(url, auth=HTTPBasicAuth(user_name, secret))
client = httpx.Client(auth=httpx.BasicAuth(username=user_name, password=secret))
session = aiohttp.ClientSession(auth=aiohttp.BasicAuth(user_name, secret))
```

Under token authentication: **First, the destination:** if this Relativity instance is not RelativityOne — including a Relativity Server instance, whose own documentation governs — no migration to RelativityOne applies and the rest of this entry does not apply.

```python
CSRF_HEADER_VALUE = "-"
response = requests.get(url, headers={"Authorization": f"Bearer {access_token}", "X-CSRF-Header": CSRF_HEADER_VALUE})
client = httpx.Client(headers={"Authorization": f"Bearer {access_token}", "X-CSRF-Header": CSRF_HEADER_VALUE})
session = aiohttp.ClientSession(headers={"Authorization": f"Bearer {access_token}", "X-CSRF-Header": CSRF_HEADER_VALUE})
```

Remove the helper object and the `auth` keyword. A client or session created once with a `Bearer` header holds a token that expires: set the header per request, or create the client again when the token is acquired again.

Credentials: **Where the credentials come from.** The source form establishes that a client sends Basic, not that the destination is RelativityOne; if it is not RelativityOne the migration is that service's own token mechanism, no migration to RelativityOne applies, and the reader stops here. If it is RelativityOne, the token comes from the token-acquisition part that follows, and the user name and password are replaced by the OAuth2 Client's client ID and secret, kept as Secret Management (Part B of the Migration Documentation) describes, never in source.

The text a match assigned Client Credentials receives after part 1. Create an OAuth2 Client with Flow Grant Type set to Client Credentials (part 3 lists its settings), then send a form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=client_credentials`, `client_id` and `client_secret`. The token is sent as `Authorization: Bearer <token>`, with `X-CSRF-Header: -` on REST and Kepler calls. **The endpoint is documented for this grant.** The scope depends on what the integration needs and **no single scope is required**: Relativity's documentation shows `SystemUserInfo` and `UserInfoAccess` and no page says which is correct or what either grants; use the value the API you call documents, and confirm it.

**Variant: a Python process acting as itself**

**Precondition: this variant is for a Python job, service or script that does not act for a signed-in person. If a person signs in, use Authorization Code.**

```python
import os
import time

import requests

IDENTITY_TOKEN_URL = "https://HOST/Relativity/Identity/connect/token"
EXPIRY_MARGIN_SECONDS = 60
REQUEST_TIMEOUT_SECONDS = 30

_cached_token = {"value": None, "expires_at": 0.0}


def get_access_token():
	now = time.monotonic()
	if _cached_token["value"] is None or _cached_token["expires_at"] - now < EXPIRY_MARGIN_SECONDS:
		response = requests.post(
			IDENTITY_TOKEN_URL,
			data={
				"grant_type": "client_credentials",
				"client_id": os.environ["RELATIVITY_CLIENT_ID"],
				"client_secret": os.environ["RELATIVITY_CLIENT_SECRET"],
			},
			timeout=REQUEST_TIMEOUT_SECONDS,
		)
		response.raise_for_status()
		payload = response.json()
		_cached_token["value"] = payload["access_token"]
		_cached_token["expires_at"] = now + payload["expires_in"]
	return _cached_token["value"]
```

`requests` form-encodes a dictionary passed as `data`, and `raise_for_status` raises on a 4XX or 5XX response. **The same token request in the other three authorized clients** (the cache and the credentials are as above):

```python
HTTP_OK = 200
token_request_fields = {
	"grant_type": "client_credentials",
	"client_id": os.environ["RELATIVITY_CLIENT_ID"],
	"client_secret": os.environ["RELATIVITY_CLIENT_SECRET"],
}

# httpx: a dictionary passed as data is form-encoded
response = httpx.post(IDENTITY_TOKEN_URL, data=token_request_fields, timeout=REQUEST_TIMEOUT_SECONDS)
response.raise_for_status()
payload = response.json()

# aiohttp: data= is form-encoded
async with session.post(IDENTITY_TOKEN_URL, data=token_request_fields) as response:
	response.raise_for_status()
	payload = await response.json()

# urllib3: PoolManager.request form-encodes the fields argument for POST only with encode_multipart=False
response = urllib3.PoolManager().request("POST", IDENTITY_TOKEN_URL, fields=token_request_fields, encode_multipart=False)
if response.status != HTTP_OK:
	raise RuntimeError(f"The token request failed with status {response.status}")
payload = response.json()
```

**Cache the token and re-acquire it on expiry — do not refresh.** Request a new token with the client secret shortly before the current one expires (the block uses a margin of 60 seconds), and once when a call is rejected for authentication. Client Credentials does not offer refresh tokens: **verified by NSerio against a live instance; not documented by Relativity** — the Allow Refresh Tokens control does not appear for that grant in the RelativityOne user interface. The recommended Access Token Lifetime is 60 minutes. **The client secret** belongs in the process environment or a secret store, as Secret Management (Part B of the Migration Documentation) describes, **never in source or in a tracked file**. A reset invalidates the previous secret immediately, so plan the cut-over (the two-client pattern is inferred). **Before deploying, confirm the calling identity:** every call now runs as the OAuth2 Client's Context User, not as the user in the replaced credential, and the non-administrator Context User is NSerio's recommendation. Trusted IPs restrict where a token can be requested, not a token already issued. **Whether `httpx` and `urllib3` accept a dictionary the way the pages read say is documented for the page's version only** (`httpx` shows no version): unchecked for other versions.

Verification: **Verification note.** **A token has a finite lifetime**: acquire again within it. **The calling identity changes**; the token-acquisition part says to whom. **Other uses may not appear as matches**: a helper held in a variable and handed to a client elsewhere, and a helper renamed at import, are not read. The helper classes are documented for `requests` 2.34.2, `httpx` (no version shown) and `aiohttp` 3.14.5 (where `BasicAuth` is **deprecated since 3.14**); other versions are unchecked.

OAuth2 Client: See [OAuth2 Client for Client Credentials](#oauth2-client-for-client-credentials).
<!-- generated:end PY-001 client-credentials -->

#### Authorization Code

<!-- generated:begin PY-001 authorization-code -->
**Flow.** This entry is for Authorization Code. The "Which flow" paragraph of PY-001 in [Part A](part-a-python.md#py-001) says when a match belongs to this flow.

Source form: **Applies to:** a Python script, service or tool; `requests`, `httpx`, `aiohttp`.

```python
response = requests.get(url, auth=HTTPBasicAuth(user_name, secret))
client = httpx.Client(auth=httpx.BasicAuth(username=user_name, password=secret))
session = aiohttp.ClientSession(auth=aiohttp.BasicAuth(user_name, secret))
```

Under token authentication: **First, the destination:** if this Relativity instance is not RelativityOne — including a Relativity Server instance, whose own documentation governs — no migration to RelativityOne applies and the rest of this entry does not apply.

```python
CSRF_HEADER_VALUE = "-"
response = requests.get(url, headers={"Authorization": f"Bearer {access_token}", "X-CSRF-Header": CSRF_HEADER_VALUE})
client = httpx.Client(headers={"Authorization": f"Bearer {access_token}", "X-CSRF-Header": CSRF_HEADER_VALUE})
session = aiohttp.ClientSession(headers={"Authorization": f"Bearer {access_token}", "X-CSRF-Header": CSRF_HEADER_VALUE})
```

Remove the helper object and the `auth` keyword. A client or session created once with a `Bearer` header holds a token that expires: set the header per request, or create the client again when the token is acquired again.

Credentials: **Where the credentials come from.** The source form establishes that a client sends Basic, not that the destination is RelativityOne; if it is not RelativityOne the migration is that service's own token mechanism, no migration to RelativityOne applies, and the reader stops here. If it is RelativityOne, the token comes from the token-acquisition part that follows, and the user name and password are replaced by the OAuth2 Client's client ID and secret, kept as Secret Management (Part B of the Migration Documentation) describes, never in source.

**Common to both variants.** The person signs in on RelativityOne's own page and the code never holds their password. The text has a common part and **two variants by application type**, each opening with the precondition it is for: a server-side web application and a command-line or desktop tool. Read the precondition of each and discard the variant that does not apply.

**Sign in.** Replace the code that collected the person's credentials with a redirect to the authorize endpoint `<host>/Relativity/Identity/connect/authorize` (**verified by NSerio against a live instance; not documented by Relativity**), which leads to RelativityOne's login page. The request carries the standard authorization-code parameters — `client_id`, `response_type=code`, the registered `redirect_uri`, `state` checked on return, and `code_challenge` with `code_challenge_method=S256` when PKCE is used; these are standard OAuth (RFC 6749 and RFC 7636) and Relativity documents none of them.

**PKCE.** PKCE is required by Relativity's own documentation to obtain a refresh token and not otherwise: a Code request that does not ask for a refresh token works without it, **verified by NSerio against a live instance; not documented by Relativity**. NSerio recommends `S256` (NSerio recommendation) and does not claim Relativity accepts any particular method. The verifier is 43 to 128 characters of the unreserved set and the `S256` challenge is the base64url of the SHA-256 of the verifier; `secrets.token_urlsafe(32)` gives 43 characters from that set (that the two match is arithmetic, not a tested fact) and `hashlib.sha256` and `base64.urlsafe_b64encode` compute the challenge. **`state`** is generated, stored against the session and compared on return; `nonce` is not used, and Relativity does not document whether it enforces either.

**The client secret is required.** Code does not work without a client secret: tested and rejected, **verified by NSerio against a live instance; not documented by Relativity**, which confirms Relativity's own statement. **A secret kept in a program that is distributed is extractable**, so the exchange runs where a secret can be kept.

**Exchange the code.** A form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=authorization_code`, the code, the same `redirect_uri`, the client ID and the secret, plus the `code_verifier` when PKCE is used (**verified by NSerio against a live instance; not documented by Relativity**).

**Redirect URL.** A server-side application registers **one exact HTTPS URL per environment, no wildcards** (the matching rules are not documented and no claim is made about them). A plain-HTTP loopback Redirect URL `http://127.0.0.1:<port>/<path>` on a **fixed registered port** is accepted, **verified by NSerio against a live instance; not documented by Relativity**, and is the command-line shape. RFC 8252 requires an authorization server to allow any port for a loopback redirect; **Relativity's behaviour is the fixed registered port, and the RFC's rule is not claimed for it.**

**On expiry.** With Allow Refresh Tokens enabled and PKCE used, the code holds a refresh token and sends `grant_type=refresh_token` in place of the code exchange: *inferred* from the standard OAuth shape, not tested; Relativity does not document a refresh token's lifetime, rotation, revocation or revocation endpoint, nor what disabling the client does to tokens already issued. **A refresh token is a secret.** **Single sign-on:** sign-in happens on RelativityOne's own page, so the instance's single sign-on and two-factor rules apply unchanged (*inferred*). **The code stops collecting the password:** after this change nothing receives, stores or forwards the person's user name and password, and the code holds only tokens. Every call now runs as the person who signed in, so **the calling identity changes**. **Scope:** no scope set is prescribed for Code; `openid RelativityRequestOrigin UserInfo UserInfoAccess AuthenticationID RelativityWeb` works, **verified by NSerio against a live instance; not documented by Relativity**, as working and not as minimal.

**Variant 1 — a server-side web application (Flask, Django, FastAPI or similar)**

**Precondition: this variant is for code that runs in Python as a web server and acts for people who sign in to it. It is not for a command-line tool (variant 2).**

Standard server-side Code flow. A login route builds the authorize address with `state` (and the PKCE verifier where a refresh token is wanted), stores them against the visitor's session and redirects; RelativityOne redirects to the **callback route**, registered as an exact HTTPS URL; the callback checks `state` and exchanges the code with the secret. **The logic is framework-neutral; the framework supplies the routes and the session**, and the sketch shows the callback as a handler of the standard library's `http.server` so that it has no dependency: in a Flask, Django or FastAPI application the same two functions are called from the login and callback views, and the dictionary of pending logins is the framework's session (Flask's session holds data in a signed cookie that the user can read but not modify, so **NSerio recommends that it hold neither the verifier nor a token**; Django's session framework "stores data on the server side" with the cookie holding only a session ID, "unless you're using the cookie based backend" (`SESSION_ENGINE` set to `django.contrib.sessions.backends.signed_cookies`), whose data "can be read by the client"; the pages of FastAPI read name no session store). **No framework code is shown: none is installed and none is verified.**

```python
import base64
import hashlib
import http.server
import secrets
from urllib.parse import parse_qs, urlencode, urlparse

import requests

IDENTITY_BASE = "https://HOST/Relativity/Identity/connect"
REDIRECT_URI = "https://app.example.test/auth/callback"
CALLBACK_PATH = "/auth/callback"
RANDOM_BYTES = 32
REQUEST_TIMEOUT_SECONDS = 30
HTTP_OK = 200
HTTP_NOT_FOUND = 404
_pending_by_session = {}


def base64_url(raw_bytes):
	return base64.urlsafe_b64encode(raw_bytes).rstrip(b"=").decode("ascii")


def begin_login(session_id, client_id, want_refresh_token):
	state = secrets.token_urlsafe(RANDOM_BYTES)
	pending = {"state": state, "verifier": None}
	query = {
		"response_type": "code",
		"client_id": client_id,
		"redirect_uri": REDIRECT_URI,
		"state": state,
	}
	if want_refresh_token:
		verifier = secrets.token_urlsafe(RANDOM_BYTES)
		pending["verifier"] = verifier
		query["code_challenge"] = base64_url(hashlib.sha256(verifier.encode("ascii")).digest())
		query["code_challenge_method"] = "S256"
	_pending_by_session[session_id] = pending
	return f"{IDENTITY_BASE}/authorize?{urlencode(query)}"


def complete_login(session_id, client_id, client_secret, returned_code, returned_state):
	pending = _pending_by_session.pop(session_id, None)
	tokens = None
	state_matches = (
		pending is not None
		and returned_state is not None
		and secrets.compare_digest(returned_state.encode("utf-8"), pending["state"].encode("utf-8"))
	)
	if state_matches:
		fields = {
			"grant_type": "authorization_code",
			"code": returned_code,
			"redirect_uri": REDIRECT_URI,
			"client_id": client_id,
			"client_secret": client_secret,
		}
		if pending["verifier"] is not None:
			fields["code_verifier"] = pending["verifier"]
		response = requests.post(f"{IDENTITY_BASE}/token", data=fields, timeout=REQUEST_TIMEOUT_SECONDS)
		tokens = response.json() if response.status_code == HTTP_OK else None
	return tokens


class CallbackHandler(http.server.BaseHTTPRequestHandler):
	def do_GET(self):
		address = urlparse(self.path)
		is_callback = address.path == CALLBACK_PATH
		self.send_response(HTTP_OK if is_callback else HTTP_NOT_FOUND)
		self.end_headers()
		if is_callback:
			query = parse_qs(address.query)
			self.wfile.write(b"Signed in.")
			self.server.returned_code = query.get("code", [None])[0]
			self.server.returned_state = query.get("state", [None])[0]
```

`state` is consumed once and compared on return in constant time (`secrets.compare_digest`); a request with no pending entry or a different `state` gets no exchange. **The scope is not prescribed** (the sketch sends none). **Where the secret travels in the token request** (the body, as sketched, or HTTP Basic client authentication) is **not verified**; RFC 6749 allows either form for a client password. The sketch does not show the HTTP server's TLS, the session cookie or a session store that survives more than one process (the module dictionary does not). **The handler only records the returned `code` and `state`; in a real application the callback view calls `complete_login` with the visitor's own session identifier**, so that the pending entry of that visitor, and no other, is consumed. **Tokens stay on the server**, in a session store that is not exposed to a request.

**The `http.server` handler is a shape, not a deployment.** The `http.server` page states that the module "is not recommended for production. It only implements basic security checks" (Python 3.15.2). The handler above shows what the callback reads and answers, so that the sketch has no dependency; **an application that serves people runs the callback as a view of the framework it already uses** (Flask, Django or FastAPI), behind the TLS and the session store that framework and its server provide.

**Variant 2 — a command-line or desktop tool**

**Precondition: this variant is for code that runs on a person's own machine, as a command-line tool or a desktop shell, and signs that person in. It is not for a web server (variant 1).**

The process starts a listener from the standard library, bound to **`127.0.0.1` only**, on the **fixed registered port**, for **one callback request**; opens the system browser on the authorize address **and always prints the address too**, so a machine with no browser, an SSH session or a failed launch has a manual route; reads `code` and `state` from the callback request, closes the listener, checks `state`, and exchanges the code as variant 1's `complete_login` does (with the redirect URI `http://127.0.0.1:8123/callback`).

```python
import http.server
import time
import webbrowser
from urllib.parse import parse_qs, urlparse

LOOPBACK_HOST = "127.0.0.1"
REGISTERED_PORT = 8123
CALLBACK_PATH = "/callback"
WAIT_SECONDS = 120
POLL_SECONDS = 1.0
HTTP_OK = 200
HTTP_NOT_FOUND = 404


class _RedirectHandler(http.server.BaseHTTPRequestHandler):
	received = None

	def do_GET(self):
		address = urlparse(self.path)
		is_callback = address.path == CALLBACK_PATH
		self.send_response(HTTP_OK if is_callback else HTTP_NOT_FOUND)
		self.end_headers()
		self.wfile.write(b"You can close this window." if is_callback else b"Not found")
		if is_callback:
			query = parse_qs(address.query)
			type(self).received = {
				"code": query.get("code", [None])[0],
				"state": query.get("state", [None])[0],
			}

	def log_message(self, message_format, *arguments):
		return None


class _LoopbackServer(http.server.HTTPServer):
	allow_reuse_address = False


def wait_for_redirect(authorize_address):
	_RedirectHandler.received = None
	print(f"Sign in at: {authorize_address}")
	try:
		server = _LoopbackServer((LOOPBACK_HOST, REGISTERED_PORT), _RedirectHandler)
	except OSError:
		print(f"Port {REGISTERED_PORT} is in use. Close the program that holds it and sign in again.")
	else:
		server.timeout = POLL_SECONDS
		webbrowser.open(authorize_address)
		deadline = time.monotonic() + WAIT_SECONDS
		while _RedirectHandler.received is None and time.monotonic() < deadline:
			server.handle_request()
		server.server_close()
	return _RedirectHandler.received
```

**Rules for the listener:** bind to loopback only; **check `state`** (another local process can connect to a fixed, advertised port); close the listener after the callback request; and handle a busy port. **A busy port raises `OSError` only if the listener does not reuse the address**: `socketserver` documents the class variable `allow_reuse_address` ("This defaults to False, and can be set in subclasses to change the policy"), and `http.server.HTTPServer` sets it to a true value, with which a second bind to the same address and port raised no error on Windows (checked on Windows; the behaviour on Linux and macOS is unchecked). The sketch therefore binds a subclass with `allow_reuse_address = False`, catches the `OSError`, and prints the address and a clear message instead of waiting. `handle_request` processes one request and returns after `timeout` seconds if none arrives, which is why the sketch loops until the callback or the **overall deadline** `WAIT_SECONDS` (`timeout` bounds each wait only, and the deadline bounds the whole wait): a stray request, such as a browser asking for an icon, is answered `404` and does not end the wait. `webbrowser.open` shows the address "using the default browser" and returns whether one was launched. **Where tokens are kept:** a file with restricted permissions or the operating system's keychain (unchecked on every system; no keychain package is named here). **The extractable-secret statement:** a command-line tool or desktop program shipped to people carries its client secret in a form anyone who holds the distribution can extract, and it protects nothing against the person running it (a documentation gap: Relativity documents no recommendation for a distributed client). **Compensating guidance** (NSerio recommendation): Trusted IPs where the users' outbound addresses are known and stable (checked at token issuance; applicability to Code *inferred*); the 60-minute Access Token Lifetime; refresh tokens only when needed; and **one OAuth2 Client per distribution**, so a leaked secret can be disabled and reset alone.

Verification: **Verification note.** **A token has a finite lifetime**: acquire again within it. **The calling identity changes**; the token-acquisition part says to whom. **Other uses may not appear as matches**: a helper held in a variable and handed to a client elsewhere, and a helper renamed at import, are not read. The helper classes are documented for `requests` 2.34.2, `httpx` (no version shown) and `aiohttp` 3.14.5 (where `BasicAuth` is **deprecated since 3.14**); other versions are unchecked.

OAuth2 Client: See [OAuth2 Client for Authorization Code](#oauth2-client-for-authorization-code).
<!-- generated:end PY-001 authorization-code -->

### PY-002 · NamedClientAuthTuple

<a id="py-002-migration"></a>

Remove the `auth` pair from the call, from a client's constructor and from a `Session`, and send a Bearer token in an `Authorization` header instead. The pair was given to a named `requests` or `httpx` call, so the replacement applies to that client.

#### Client Credentials

<!-- generated:begin PY-002 client-credentials -->
**Flow.** This entry is for Client Credentials. The "Which flow" paragraph of PY-002 in [Part A](part-a-python.md#py-002) says when a match belongs to this flow.

Source form: **Applies to:** a Python script, service or tool; `requests`, `httpx`.

```python
response = requests.get(url, auth=(user_name, secret))
client = httpx.AsyncClient(auth=(user_name, secret))
```

Under token authentication: **First, the destination:** if this Relativity instance is not RelativityOne — including a Relativity Server instance, whose own documentation governs — no migration to RelativityOne applies and the rest of this entry does not apply.

```python
CSRF_HEADER_VALUE = "-"
response = requests.get(url, headers={"Authorization": f"Bearer {access_token}", "X-CSRF-Header": CSRF_HEADER_VALUE})
client = httpx.AsyncClient(headers={"Authorization": f"Bearer {access_token}", "X-CSRF-Header": CSRF_HEADER_VALUE})
```

Remove the `auth` tuple, also from a client's constructor and from a `Session`.

Credentials: **Where the credentials come from.** The source form establishes that a client sends Basic, not that the destination is RelativityOne; if it is not RelativityOne the migration is that service's own token mechanism, no migration to RelativityOne applies, and the reader stops here. If it is RelativityOne, the token comes from the token-acquisition part that follows, and the user name and password are replaced by the OAuth2 Client's client ID and secret, kept as Secret Management (Part B of the Migration Documentation) describes, never in source.

The text a match assigned Client Credentials receives after part 1. Create an OAuth2 Client with Flow Grant Type set to Client Credentials (part 3 lists its settings), then send a form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=client_credentials`, `client_id` and `client_secret`. The token is sent as `Authorization: Bearer <token>`, with `X-CSRF-Header: -` on REST and Kepler calls. **The endpoint is documented for this grant.** The scope depends on what the integration needs and **no single scope is required**: Relativity's documentation shows `SystemUserInfo` and `UserInfoAccess` and no page says which is correct or what either grants; use the value the API you call documents, and confirm it.

**Variant: a Python process acting as itself**

**Precondition: this variant is for a Python job, service or script that does not act for a signed-in person. If a person signs in, use Authorization Code.**

```python
import os
import time

import requests

IDENTITY_TOKEN_URL = "https://HOST/Relativity/Identity/connect/token"
EXPIRY_MARGIN_SECONDS = 60
REQUEST_TIMEOUT_SECONDS = 30

_cached_token = {"value": None, "expires_at": 0.0}


def get_access_token():
	now = time.monotonic()
	if _cached_token["value"] is None or _cached_token["expires_at"] - now < EXPIRY_MARGIN_SECONDS:
		response = requests.post(
			IDENTITY_TOKEN_URL,
			data={
				"grant_type": "client_credentials",
				"client_id": os.environ["RELATIVITY_CLIENT_ID"],
				"client_secret": os.environ["RELATIVITY_CLIENT_SECRET"],
			},
			timeout=REQUEST_TIMEOUT_SECONDS,
		)
		response.raise_for_status()
		payload = response.json()
		_cached_token["value"] = payload["access_token"]
		_cached_token["expires_at"] = now + payload["expires_in"]
	return _cached_token["value"]
```

`requests` form-encodes a dictionary passed as `data`, and `raise_for_status` raises on a 4XX or 5XX response. **The same token request in the other three authorized clients** (the cache and the credentials are as above):

```python
HTTP_OK = 200
token_request_fields = {
	"grant_type": "client_credentials",
	"client_id": os.environ["RELATIVITY_CLIENT_ID"],
	"client_secret": os.environ["RELATIVITY_CLIENT_SECRET"],
}

# httpx: a dictionary passed as data is form-encoded
response = httpx.post(IDENTITY_TOKEN_URL, data=token_request_fields, timeout=REQUEST_TIMEOUT_SECONDS)
response.raise_for_status()
payload = response.json()

# aiohttp: data= is form-encoded
async with session.post(IDENTITY_TOKEN_URL, data=token_request_fields) as response:
	response.raise_for_status()
	payload = await response.json()

# urllib3: PoolManager.request form-encodes the fields argument for POST only with encode_multipart=False
response = urllib3.PoolManager().request("POST", IDENTITY_TOKEN_URL, fields=token_request_fields, encode_multipart=False)
if response.status != HTTP_OK:
	raise RuntimeError(f"The token request failed with status {response.status}")
payload = response.json()
```

**Cache the token and re-acquire it on expiry — do not refresh.** Request a new token with the client secret shortly before the current one expires (the block uses a margin of 60 seconds), and once when a call is rejected for authentication. Client Credentials does not offer refresh tokens: **verified by NSerio against a live instance; not documented by Relativity** — the Allow Refresh Tokens control does not appear for that grant in the RelativityOne user interface. The recommended Access Token Lifetime is 60 minutes. **The client secret** belongs in the process environment or a secret store, as Secret Management (Part B of the Migration Documentation) describes, **never in source or in a tracked file**. A reset invalidates the previous secret immediately, so plan the cut-over (the two-client pattern is inferred). **Before deploying, confirm the calling identity:** every call now runs as the OAuth2 Client's Context User, not as the user in the replaced credential, and the non-administrator Context User is NSerio's recommendation. Trusted IPs restrict where a token can be requested, not a token already issued. **Whether `httpx` and `urllib3` accept a dictionary the way the pages read say is documented for the page's version only** (`httpx` shows no version): unchecked for other versions.

Verification: **Verification note.** **A token has a finite lifetime.** **The calling identity changes.** **The scheme changes from `Basic` to `Bearer`**: a `Bearer` header is not read by this rule again. The tuple form is documented for `requests` 2.34.2 and `httpx` (no version shown); other versions are unchecked.

OAuth2 Client: See [OAuth2 Client for Client Credentials](#oauth2-client-for-client-credentials).
<!-- generated:end PY-002 client-credentials -->

#### Authorization Code

<!-- generated:begin PY-002 authorization-code -->
**Flow.** This entry is for Authorization Code. The "Which flow" paragraph of PY-002 in [Part A](part-a-python.md#py-002) says when a match belongs to this flow.

Source form: **Applies to:** a Python script, service or tool; `requests`, `httpx`.

```python
response = requests.get(url, auth=(user_name, secret))
client = httpx.AsyncClient(auth=(user_name, secret))
```

Under token authentication: **First, the destination:** if this Relativity instance is not RelativityOne — including a Relativity Server instance, whose own documentation governs — no migration to RelativityOne applies and the rest of this entry does not apply.

```python
CSRF_HEADER_VALUE = "-"
response = requests.get(url, headers={"Authorization": f"Bearer {access_token}", "X-CSRF-Header": CSRF_HEADER_VALUE})
client = httpx.AsyncClient(headers={"Authorization": f"Bearer {access_token}", "X-CSRF-Header": CSRF_HEADER_VALUE})
```

Remove the `auth` tuple, also from a client's constructor and from a `Session`.

Credentials: **Where the credentials come from.** The source form establishes that a client sends Basic, not that the destination is RelativityOne; if it is not RelativityOne the migration is that service's own token mechanism, no migration to RelativityOne applies, and the reader stops here. If it is RelativityOne, the token comes from the token-acquisition part that follows, and the user name and password are replaced by the OAuth2 Client's client ID and secret, kept as Secret Management (Part B of the Migration Documentation) describes, never in source.

**Common to both variants.** The person signs in on RelativityOne's own page and the code never holds their password. The text has a common part and **two variants by application type**, each opening with the precondition it is for: a server-side web application and a command-line or desktop tool. Read the precondition of each and discard the variant that does not apply.

**Sign in.** Replace the code that collected the person's credentials with a redirect to the authorize endpoint `<host>/Relativity/Identity/connect/authorize` (**verified by NSerio against a live instance; not documented by Relativity**), which leads to RelativityOne's login page. The request carries the standard authorization-code parameters — `client_id`, `response_type=code`, the registered `redirect_uri`, `state` checked on return, and `code_challenge` with `code_challenge_method=S256` when PKCE is used; these are standard OAuth (RFC 6749 and RFC 7636) and Relativity documents none of them.

**PKCE.** PKCE is required by Relativity's own documentation to obtain a refresh token and not otherwise: a Code request that does not ask for a refresh token works without it, **verified by NSerio against a live instance; not documented by Relativity**. NSerio recommends `S256` (NSerio recommendation) and does not claim Relativity accepts any particular method. The verifier is 43 to 128 characters of the unreserved set and the `S256` challenge is the base64url of the SHA-256 of the verifier; `secrets.token_urlsafe(32)` gives 43 characters from that set (that the two match is arithmetic, not a tested fact) and `hashlib.sha256` and `base64.urlsafe_b64encode` compute the challenge. **`state`** is generated, stored against the session and compared on return; `nonce` is not used, and Relativity does not document whether it enforces either.

**The client secret is required.** Code does not work without a client secret: tested and rejected, **verified by NSerio against a live instance; not documented by Relativity**, which confirms Relativity's own statement. **A secret kept in a program that is distributed is extractable**, so the exchange runs where a secret can be kept.

**Exchange the code.** A form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=authorization_code`, the code, the same `redirect_uri`, the client ID and the secret, plus the `code_verifier` when PKCE is used (**verified by NSerio against a live instance; not documented by Relativity**).

**Redirect URL.** A server-side application registers **one exact HTTPS URL per environment, no wildcards** (the matching rules are not documented and no claim is made about them). A plain-HTTP loopback Redirect URL `http://127.0.0.1:<port>/<path>` on a **fixed registered port** is accepted, **verified by NSerio against a live instance; not documented by Relativity**, and is the command-line shape. RFC 8252 requires an authorization server to allow any port for a loopback redirect; **Relativity's behaviour is the fixed registered port, and the RFC's rule is not claimed for it.**

**On expiry.** With Allow Refresh Tokens enabled and PKCE used, the code holds a refresh token and sends `grant_type=refresh_token` in place of the code exchange: *inferred* from the standard OAuth shape, not tested; Relativity does not document a refresh token's lifetime, rotation, revocation or revocation endpoint, nor what disabling the client does to tokens already issued. **A refresh token is a secret.** **Single sign-on:** sign-in happens on RelativityOne's own page, so the instance's single sign-on and two-factor rules apply unchanged (*inferred*). **The code stops collecting the password:** after this change nothing receives, stores or forwards the person's user name and password, and the code holds only tokens. Every call now runs as the person who signed in, so **the calling identity changes**. **Scope:** no scope set is prescribed for Code; `openid RelativityRequestOrigin UserInfo UserInfoAccess AuthenticationID RelativityWeb` works, **verified by NSerio against a live instance; not documented by Relativity**, as working and not as minimal.

**Variant 1 — a server-side web application (Flask, Django, FastAPI or similar)**

**Precondition: this variant is for code that runs in Python as a web server and acts for people who sign in to it. It is not for a command-line tool (variant 2).**

Standard server-side Code flow. A login route builds the authorize address with `state` (and the PKCE verifier where a refresh token is wanted), stores them against the visitor's session and redirects; RelativityOne redirects to the **callback route**, registered as an exact HTTPS URL; the callback checks `state` and exchanges the code with the secret. **The logic is framework-neutral; the framework supplies the routes and the session**, and the sketch shows the callback as a handler of the standard library's `http.server` so that it has no dependency: in a Flask, Django or FastAPI application the same two functions are called from the login and callback views, and the dictionary of pending logins is the framework's session (Flask's session holds data in a signed cookie that the user can read but not modify, so **NSerio recommends that it hold neither the verifier nor a token**; Django's session framework "stores data on the server side" with the cookie holding only a session ID, "unless you're using the cookie based backend" (`SESSION_ENGINE` set to `django.contrib.sessions.backends.signed_cookies`), whose data "can be read by the client"; the pages of FastAPI read name no session store). **No framework code is shown: none is installed and none is verified.**

```python
import base64
import hashlib
import http.server
import secrets
from urllib.parse import parse_qs, urlencode, urlparse

import requests

IDENTITY_BASE = "https://HOST/Relativity/Identity/connect"
REDIRECT_URI = "https://app.example.test/auth/callback"
CALLBACK_PATH = "/auth/callback"
RANDOM_BYTES = 32
REQUEST_TIMEOUT_SECONDS = 30
HTTP_OK = 200
HTTP_NOT_FOUND = 404
_pending_by_session = {}


def base64_url(raw_bytes):
	return base64.urlsafe_b64encode(raw_bytes).rstrip(b"=").decode("ascii")


def begin_login(session_id, client_id, want_refresh_token):
	state = secrets.token_urlsafe(RANDOM_BYTES)
	pending = {"state": state, "verifier": None}
	query = {
		"response_type": "code",
		"client_id": client_id,
		"redirect_uri": REDIRECT_URI,
		"state": state,
	}
	if want_refresh_token:
		verifier = secrets.token_urlsafe(RANDOM_BYTES)
		pending["verifier"] = verifier
		query["code_challenge"] = base64_url(hashlib.sha256(verifier.encode("ascii")).digest())
		query["code_challenge_method"] = "S256"
	_pending_by_session[session_id] = pending
	return f"{IDENTITY_BASE}/authorize?{urlencode(query)}"


def complete_login(session_id, client_id, client_secret, returned_code, returned_state):
	pending = _pending_by_session.pop(session_id, None)
	tokens = None
	state_matches = (
		pending is not None
		and returned_state is not None
		and secrets.compare_digest(returned_state.encode("utf-8"), pending["state"].encode("utf-8"))
	)
	if state_matches:
		fields = {
			"grant_type": "authorization_code",
			"code": returned_code,
			"redirect_uri": REDIRECT_URI,
			"client_id": client_id,
			"client_secret": client_secret,
		}
		if pending["verifier"] is not None:
			fields["code_verifier"] = pending["verifier"]
		response = requests.post(f"{IDENTITY_BASE}/token", data=fields, timeout=REQUEST_TIMEOUT_SECONDS)
		tokens = response.json() if response.status_code == HTTP_OK else None
	return tokens


class CallbackHandler(http.server.BaseHTTPRequestHandler):
	def do_GET(self):
		address = urlparse(self.path)
		is_callback = address.path == CALLBACK_PATH
		self.send_response(HTTP_OK if is_callback else HTTP_NOT_FOUND)
		self.end_headers()
		if is_callback:
			query = parse_qs(address.query)
			self.wfile.write(b"Signed in.")
			self.server.returned_code = query.get("code", [None])[0]
			self.server.returned_state = query.get("state", [None])[0]
```

`state` is consumed once and compared on return in constant time (`secrets.compare_digest`); a request with no pending entry or a different `state` gets no exchange. **The scope is not prescribed** (the sketch sends none). **Where the secret travels in the token request** (the body, as sketched, or HTTP Basic client authentication) is **not verified**; RFC 6749 allows either form for a client password. The sketch does not show the HTTP server's TLS, the session cookie or a session store that survives more than one process (the module dictionary does not). **The handler only records the returned `code` and `state`; in a real application the callback view calls `complete_login` with the visitor's own session identifier**, so that the pending entry of that visitor, and no other, is consumed. **Tokens stay on the server**, in a session store that is not exposed to a request.

**The `http.server` handler is a shape, not a deployment.** The `http.server` page states that the module "is not recommended for production. It only implements basic security checks" (Python 3.15.2). The handler above shows what the callback reads and answers, so that the sketch has no dependency; **an application that serves people runs the callback as a view of the framework it already uses** (Flask, Django or FastAPI), behind the TLS and the session store that framework and its server provide.

**Variant 2 — a command-line or desktop tool**

**Precondition: this variant is for code that runs on a person's own machine, as a command-line tool or a desktop shell, and signs that person in. It is not for a web server (variant 1).**

The process starts a listener from the standard library, bound to **`127.0.0.1` only**, on the **fixed registered port**, for **one callback request**; opens the system browser on the authorize address **and always prints the address too**, so a machine with no browser, an SSH session or a failed launch has a manual route; reads `code` and `state` from the callback request, closes the listener, checks `state`, and exchanges the code as variant 1's `complete_login` does (with the redirect URI `http://127.0.0.1:8123/callback`).

```python
import http.server
import time
import webbrowser
from urllib.parse import parse_qs, urlparse

LOOPBACK_HOST = "127.0.0.1"
REGISTERED_PORT = 8123
CALLBACK_PATH = "/callback"
WAIT_SECONDS = 120
POLL_SECONDS = 1.0
HTTP_OK = 200
HTTP_NOT_FOUND = 404


class _RedirectHandler(http.server.BaseHTTPRequestHandler):
	received = None

	def do_GET(self):
		address = urlparse(self.path)
		is_callback = address.path == CALLBACK_PATH
		self.send_response(HTTP_OK if is_callback else HTTP_NOT_FOUND)
		self.end_headers()
		self.wfile.write(b"You can close this window." if is_callback else b"Not found")
		if is_callback:
			query = parse_qs(address.query)
			type(self).received = {
				"code": query.get("code", [None])[0],
				"state": query.get("state", [None])[0],
			}

	def log_message(self, message_format, *arguments):
		return None


class _LoopbackServer(http.server.HTTPServer):
	allow_reuse_address = False


def wait_for_redirect(authorize_address):
	_RedirectHandler.received = None
	print(f"Sign in at: {authorize_address}")
	try:
		server = _LoopbackServer((LOOPBACK_HOST, REGISTERED_PORT), _RedirectHandler)
	except OSError:
		print(f"Port {REGISTERED_PORT} is in use. Close the program that holds it and sign in again.")
	else:
		server.timeout = POLL_SECONDS
		webbrowser.open(authorize_address)
		deadline = time.monotonic() + WAIT_SECONDS
		while _RedirectHandler.received is None and time.monotonic() < deadline:
			server.handle_request()
		server.server_close()
	return _RedirectHandler.received
```

**Rules for the listener:** bind to loopback only; **check `state`** (another local process can connect to a fixed, advertised port); close the listener after the callback request; and handle a busy port. **A busy port raises `OSError` only if the listener does not reuse the address**: `socketserver` documents the class variable `allow_reuse_address` ("This defaults to False, and can be set in subclasses to change the policy"), and `http.server.HTTPServer` sets it to a true value, with which a second bind to the same address and port raised no error on Windows (checked on Windows; the behaviour on Linux and macOS is unchecked). The sketch therefore binds a subclass with `allow_reuse_address = False`, catches the `OSError`, and prints the address and a clear message instead of waiting. `handle_request` processes one request and returns after `timeout` seconds if none arrives, which is why the sketch loops until the callback or the **overall deadline** `WAIT_SECONDS` (`timeout` bounds each wait only, and the deadline bounds the whole wait): a stray request, such as a browser asking for an icon, is answered `404` and does not end the wait. `webbrowser.open` shows the address "using the default browser" and returns whether one was launched. **Where tokens are kept:** a file with restricted permissions or the operating system's keychain (unchecked on every system; no keychain package is named here). **The extractable-secret statement:** a command-line tool or desktop program shipped to people carries its client secret in a form anyone who holds the distribution can extract, and it protects nothing against the person running it (a documentation gap: Relativity documents no recommendation for a distributed client). **Compensating guidance** (NSerio recommendation): Trusted IPs where the users' outbound addresses are known and stable (checked at token issuance; applicability to Code *inferred*); the 60-minute Access Token Lifetime; refresh tokens only when needed; and **one OAuth2 Client per distribution**, so a leaked secret can be disabled and reset alone.

Verification: **Verification note.** **A token has a finite lifetime.** **The calling identity changes.** **The scheme changes from `Basic` to `Bearer`**: a `Bearer` header is not read by this rule again. The tuple form is documented for `requests` 2.34.2 and `httpx` (no version shown); other versions are unchecked.

OAuth2 Client: See [OAuth2 Client for Authorization Code](#oauth2-client-for-authorization-code).
<!-- generated:end PY-002 authorization-code -->

### PY-003 · AuthorizationHeaderBasicScheme

<a id="py-003-migration"></a>

Change the value of the `Authorization` header from `Basic ...` to `Bearer ...` at the place the header is written, and add the `X-CSRF-Header` header the entry shows. The same replacement applies to a header set with `add_header(`, with `putheader(`, with `PoolManager(headers=...)` and with `ClientSession(headers=...)`. Remove the code that built the encoded pair.

#### Client Credentials

<!-- generated:begin PY-003 client-credentials -->
**Flow.** This entry is for Client Credentials. The "Which flow" paragraph of PY-003 in [Part A](part-a-python.md#py-003) says when a match belongs to this flow.

Source form: **Applies to:** a Python script, service or tool; `requests`, `httpx`, `aiohttp`, `urllib3`, `urllib.request`, `http.client`.

```python
response = requests.get(url, headers={"Authorization": "Basic " + encoded_pair})
session.headers["Authorization"] = f"Basic {encoded_pair}"
request.add_header("Authorization", "Basic %s" % encoded_pair)
```

Under token authentication: **First, the destination:** if this Relativity instance is not RelativityOne — including a Relativity Server instance, whose own documentation governs — no migration to RelativityOne applies and the rest of this entry does not apply.

```python
CSRF_HEADER_VALUE = "-"
response = requests.get(url, headers={"Authorization": f"Bearer {access_token}", "X-CSRF-Header": CSRF_HEADER_VALUE})
session.headers["Authorization"] = f"Bearer {access_token}"
session.headers["X-CSRF-Header"] = CSRF_HEADER_VALUE
request.add_header("Authorization", f"Bearer {access_token}")
request.add_header("X-CSRF-Header", CSRF_HEADER_VALUE)
```

Remove the code that built the encoded pair. The same replacement applies to a header set with `putheader(`, to `PoolManager(headers=…)` and to `ClientSession(headers=…)`: the value changes from `Basic …` to `Bearer …`.

Credentials: **Where the credentials come from.** The source form establishes that a client sends Basic, not that the destination is RelativityOne; if it is not RelativityOne the migration is that service's own token mechanism, no migration to RelativityOne applies, and the reader stops here. If it is RelativityOne, the token comes from the token-acquisition part that follows, and the user name and password are replaced by the OAuth2 Client's client ID and secret, kept as Secret Management (Part B of the Migration Documentation) describes, never in source.

The text a match assigned Client Credentials receives after part 1. Create an OAuth2 Client with Flow Grant Type set to Client Credentials (part 3 lists its settings), then send a form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=client_credentials`, `client_id` and `client_secret`. The token is sent as `Authorization: Bearer <token>`, with `X-CSRF-Header: -` on REST and Kepler calls. **The endpoint is documented for this grant.** The scope depends on what the integration needs and **no single scope is required**: Relativity's documentation shows `SystemUserInfo` and `UserInfoAccess` and no page says which is correct or what either grants; use the value the API you call documents, and confirm it.

**Variant: a Python process acting as itself**

**Precondition: this variant is for a Python job, service or script that does not act for a signed-in person. If a person signs in, use Authorization Code.**

```python
import os
import time

import requests

IDENTITY_TOKEN_URL = "https://HOST/Relativity/Identity/connect/token"
EXPIRY_MARGIN_SECONDS = 60
REQUEST_TIMEOUT_SECONDS = 30

_cached_token = {"value": None, "expires_at": 0.0}


def get_access_token():
	now = time.monotonic()
	if _cached_token["value"] is None or _cached_token["expires_at"] - now < EXPIRY_MARGIN_SECONDS:
		response = requests.post(
			IDENTITY_TOKEN_URL,
			data={
				"grant_type": "client_credentials",
				"client_id": os.environ["RELATIVITY_CLIENT_ID"],
				"client_secret": os.environ["RELATIVITY_CLIENT_SECRET"],
			},
			timeout=REQUEST_TIMEOUT_SECONDS,
		)
		response.raise_for_status()
		payload = response.json()
		_cached_token["value"] = payload["access_token"]
		_cached_token["expires_at"] = now + payload["expires_in"]
	return _cached_token["value"]
```

`requests` form-encodes a dictionary passed as `data`, and `raise_for_status` raises on a 4XX or 5XX response. **The same token request in the other three authorized clients** (the cache and the credentials are as above):

```python
HTTP_OK = 200
token_request_fields = {
	"grant_type": "client_credentials",
	"client_id": os.environ["RELATIVITY_CLIENT_ID"],
	"client_secret": os.environ["RELATIVITY_CLIENT_SECRET"],
}

# httpx: a dictionary passed as data is form-encoded
response = httpx.post(IDENTITY_TOKEN_URL, data=token_request_fields, timeout=REQUEST_TIMEOUT_SECONDS)
response.raise_for_status()
payload = response.json()

# aiohttp: data= is form-encoded
async with session.post(IDENTITY_TOKEN_URL, data=token_request_fields) as response:
	response.raise_for_status()
	payload = await response.json()

# urllib3: PoolManager.request form-encodes the fields argument for POST only with encode_multipart=False
response = urllib3.PoolManager().request("POST", IDENTITY_TOKEN_URL, fields=token_request_fields, encode_multipart=False)
if response.status != HTTP_OK:
	raise RuntimeError(f"The token request failed with status {response.status}")
payload = response.json()
```

**Cache the token and re-acquire it on expiry — do not refresh.** Request a new token with the client secret shortly before the current one expires (the block uses a margin of 60 seconds), and once when a call is rejected for authentication. Client Credentials does not offer refresh tokens: **verified by NSerio against a live instance; not documented by Relativity** — the Allow Refresh Tokens control does not appear for that grant in the RelativityOne user interface. The recommended Access Token Lifetime is 60 minutes. **The client secret** belongs in the process environment or a secret store, as Secret Management (Part B of the Migration Documentation) describes, **never in source or in a tracked file**. A reset invalidates the previous secret immediately, so plan the cut-over (the two-client pattern is inferred). **Before deploying, confirm the calling identity:** every call now runs as the OAuth2 Client's Context User, not as the user in the replaced credential, and the non-administrator Context User is NSerio's recommendation. Trusted IPs restrict where a token can be requested, not a token already issued. **Whether `httpx` and `urllib3` accept a dictionary the way the pages read say is documented for the page's version only** (`httpx` shows no version): unchecked for other versions.

Verification: **Verification note.** **A token has a finite lifetime.** **The calling identity changes.** **The scheme changes from `Basic` to `Bearer`.** **A header set by a function the rule did not read, a merged mapping or a hook is not read**. The header carriers are documented for the pages read and their versions; other versions are unchecked.

OAuth2 Client: See [OAuth2 Client for Client Credentials](#oauth2-client-for-client-credentials).
<!-- generated:end PY-003 client-credentials -->

#### Authorization Code

<!-- generated:begin PY-003 authorization-code -->
**Flow.** This entry is for Authorization Code. The "Which flow" paragraph of PY-003 in [Part A](part-a-python.md#py-003) says when a match belongs to this flow.

Source form: **Applies to:** a Python script, service or tool; `requests`, `httpx`, `aiohttp`, `urllib3`, `urllib.request`, `http.client`.

```python
response = requests.get(url, headers={"Authorization": "Basic " + encoded_pair})
session.headers["Authorization"] = f"Basic {encoded_pair}"
request.add_header("Authorization", "Basic %s" % encoded_pair)
```

Under token authentication: **First, the destination:** if this Relativity instance is not RelativityOne — including a Relativity Server instance, whose own documentation governs — no migration to RelativityOne applies and the rest of this entry does not apply.

```python
CSRF_HEADER_VALUE = "-"
response = requests.get(url, headers={"Authorization": f"Bearer {access_token}", "X-CSRF-Header": CSRF_HEADER_VALUE})
session.headers["Authorization"] = f"Bearer {access_token}"
session.headers["X-CSRF-Header"] = CSRF_HEADER_VALUE
request.add_header("Authorization", f"Bearer {access_token}")
request.add_header("X-CSRF-Header", CSRF_HEADER_VALUE)
```

Remove the code that built the encoded pair. The same replacement applies to a header set with `putheader(`, to `PoolManager(headers=…)` and to `ClientSession(headers=…)`: the value changes from `Basic …` to `Bearer …`.

Credentials: **Where the credentials come from.** The source form establishes that a client sends Basic, not that the destination is RelativityOne; if it is not RelativityOne the migration is that service's own token mechanism, no migration to RelativityOne applies, and the reader stops here. If it is RelativityOne, the token comes from the token-acquisition part that follows, and the user name and password are replaced by the OAuth2 Client's client ID and secret, kept as Secret Management (Part B of the Migration Documentation) describes, never in source.

**Common to both variants.** The person signs in on RelativityOne's own page and the code never holds their password. The text has a common part and **two variants by application type**, each opening with the precondition it is for: a server-side web application and a command-line or desktop tool. Read the precondition of each and discard the variant that does not apply.

**Sign in.** Replace the code that collected the person's credentials with a redirect to the authorize endpoint `<host>/Relativity/Identity/connect/authorize` (**verified by NSerio against a live instance; not documented by Relativity**), which leads to RelativityOne's login page. The request carries the standard authorization-code parameters — `client_id`, `response_type=code`, the registered `redirect_uri`, `state` checked on return, and `code_challenge` with `code_challenge_method=S256` when PKCE is used; these are standard OAuth (RFC 6749 and RFC 7636) and Relativity documents none of them.

**PKCE.** PKCE is required by Relativity's own documentation to obtain a refresh token and not otherwise: a Code request that does not ask for a refresh token works without it, **verified by NSerio against a live instance; not documented by Relativity**. NSerio recommends `S256` (NSerio recommendation) and does not claim Relativity accepts any particular method. The verifier is 43 to 128 characters of the unreserved set and the `S256` challenge is the base64url of the SHA-256 of the verifier; `secrets.token_urlsafe(32)` gives 43 characters from that set (that the two match is arithmetic, not a tested fact) and `hashlib.sha256` and `base64.urlsafe_b64encode` compute the challenge. **`state`** is generated, stored against the session and compared on return; `nonce` is not used, and Relativity does not document whether it enforces either.

**The client secret is required.** Code does not work without a client secret: tested and rejected, **verified by NSerio against a live instance; not documented by Relativity**, which confirms Relativity's own statement. **A secret kept in a program that is distributed is extractable**, so the exchange runs where a secret can be kept.

**Exchange the code.** A form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=authorization_code`, the code, the same `redirect_uri`, the client ID and the secret, plus the `code_verifier` when PKCE is used (**verified by NSerio against a live instance; not documented by Relativity**).

**Redirect URL.** A server-side application registers **one exact HTTPS URL per environment, no wildcards** (the matching rules are not documented and no claim is made about them). A plain-HTTP loopback Redirect URL `http://127.0.0.1:<port>/<path>` on a **fixed registered port** is accepted, **verified by NSerio against a live instance; not documented by Relativity**, and is the command-line shape. RFC 8252 requires an authorization server to allow any port for a loopback redirect; **Relativity's behaviour is the fixed registered port, and the RFC's rule is not claimed for it.**

**On expiry.** With Allow Refresh Tokens enabled and PKCE used, the code holds a refresh token and sends `grant_type=refresh_token` in place of the code exchange: *inferred* from the standard OAuth shape, not tested; Relativity does not document a refresh token's lifetime, rotation, revocation or revocation endpoint, nor what disabling the client does to tokens already issued. **A refresh token is a secret.** **Single sign-on:** sign-in happens on RelativityOne's own page, so the instance's single sign-on and two-factor rules apply unchanged (*inferred*). **The code stops collecting the password:** after this change nothing receives, stores or forwards the person's user name and password, and the code holds only tokens. Every call now runs as the person who signed in, so **the calling identity changes**. **Scope:** no scope set is prescribed for Code; `openid RelativityRequestOrigin UserInfo UserInfoAccess AuthenticationID RelativityWeb` works, **verified by NSerio against a live instance; not documented by Relativity**, as working and not as minimal.

**Variant 1 — a server-side web application (Flask, Django, FastAPI or similar)**

**Precondition: this variant is for code that runs in Python as a web server and acts for people who sign in to it. It is not for a command-line tool (variant 2).**

Standard server-side Code flow. A login route builds the authorize address with `state` (and the PKCE verifier where a refresh token is wanted), stores them against the visitor's session and redirects; RelativityOne redirects to the **callback route**, registered as an exact HTTPS URL; the callback checks `state` and exchanges the code with the secret. **The logic is framework-neutral; the framework supplies the routes and the session**, and the sketch shows the callback as a handler of the standard library's `http.server` so that it has no dependency: in a Flask, Django or FastAPI application the same two functions are called from the login and callback views, and the dictionary of pending logins is the framework's session (Flask's session holds data in a signed cookie that the user can read but not modify, so **NSerio recommends that it hold neither the verifier nor a token**; Django's session framework "stores data on the server side" with the cookie holding only a session ID, "unless you're using the cookie based backend" (`SESSION_ENGINE` set to `django.contrib.sessions.backends.signed_cookies`), whose data "can be read by the client"; the pages of FastAPI read name no session store). **No framework code is shown: none is installed and none is verified.**

```python
import base64
import hashlib
import http.server
import secrets
from urllib.parse import parse_qs, urlencode, urlparse

import requests

IDENTITY_BASE = "https://HOST/Relativity/Identity/connect"
REDIRECT_URI = "https://app.example.test/auth/callback"
CALLBACK_PATH = "/auth/callback"
RANDOM_BYTES = 32
REQUEST_TIMEOUT_SECONDS = 30
HTTP_OK = 200
HTTP_NOT_FOUND = 404
_pending_by_session = {}


def base64_url(raw_bytes):
	return base64.urlsafe_b64encode(raw_bytes).rstrip(b"=").decode("ascii")


def begin_login(session_id, client_id, want_refresh_token):
	state = secrets.token_urlsafe(RANDOM_BYTES)
	pending = {"state": state, "verifier": None}
	query = {
		"response_type": "code",
		"client_id": client_id,
		"redirect_uri": REDIRECT_URI,
		"state": state,
	}
	if want_refresh_token:
		verifier = secrets.token_urlsafe(RANDOM_BYTES)
		pending["verifier"] = verifier
		query["code_challenge"] = base64_url(hashlib.sha256(verifier.encode("ascii")).digest())
		query["code_challenge_method"] = "S256"
	_pending_by_session[session_id] = pending
	return f"{IDENTITY_BASE}/authorize?{urlencode(query)}"


def complete_login(session_id, client_id, client_secret, returned_code, returned_state):
	pending = _pending_by_session.pop(session_id, None)
	tokens = None
	state_matches = (
		pending is not None
		and returned_state is not None
		and secrets.compare_digest(returned_state.encode("utf-8"), pending["state"].encode("utf-8"))
	)
	if state_matches:
		fields = {
			"grant_type": "authorization_code",
			"code": returned_code,
			"redirect_uri": REDIRECT_URI,
			"client_id": client_id,
			"client_secret": client_secret,
		}
		if pending["verifier"] is not None:
			fields["code_verifier"] = pending["verifier"]
		response = requests.post(f"{IDENTITY_BASE}/token", data=fields, timeout=REQUEST_TIMEOUT_SECONDS)
		tokens = response.json() if response.status_code == HTTP_OK else None
	return tokens


class CallbackHandler(http.server.BaseHTTPRequestHandler):
	def do_GET(self):
		address = urlparse(self.path)
		is_callback = address.path == CALLBACK_PATH
		self.send_response(HTTP_OK if is_callback else HTTP_NOT_FOUND)
		self.end_headers()
		if is_callback:
			query = parse_qs(address.query)
			self.wfile.write(b"Signed in.")
			self.server.returned_code = query.get("code", [None])[0]
			self.server.returned_state = query.get("state", [None])[0]
```

`state` is consumed once and compared on return in constant time (`secrets.compare_digest`); a request with no pending entry or a different `state` gets no exchange. **The scope is not prescribed** (the sketch sends none). **Where the secret travels in the token request** (the body, as sketched, or HTTP Basic client authentication) is **not verified**; RFC 6749 allows either form for a client password. The sketch does not show the HTTP server's TLS, the session cookie or a session store that survives more than one process (the module dictionary does not). **The handler only records the returned `code` and `state`; in a real application the callback view calls `complete_login` with the visitor's own session identifier**, so that the pending entry of that visitor, and no other, is consumed. **Tokens stay on the server**, in a session store that is not exposed to a request.

**The `http.server` handler is a shape, not a deployment.** The `http.server` page states that the module "is not recommended for production. It only implements basic security checks" (Python 3.15.2). The handler above shows what the callback reads and answers, so that the sketch has no dependency; **an application that serves people runs the callback as a view of the framework it already uses** (Flask, Django or FastAPI), behind the TLS and the session store that framework and its server provide.

**Variant 2 — a command-line or desktop tool**

**Precondition: this variant is for code that runs on a person's own machine, as a command-line tool or a desktop shell, and signs that person in. It is not for a web server (variant 1).**

The process starts a listener from the standard library, bound to **`127.0.0.1` only**, on the **fixed registered port**, for **one callback request**; opens the system browser on the authorize address **and always prints the address too**, so a machine with no browser, an SSH session or a failed launch has a manual route; reads `code` and `state` from the callback request, closes the listener, checks `state`, and exchanges the code as variant 1's `complete_login` does (with the redirect URI `http://127.0.0.1:8123/callback`).

```python
import http.server
import time
import webbrowser
from urllib.parse import parse_qs, urlparse

LOOPBACK_HOST = "127.0.0.1"
REGISTERED_PORT = 8123
CALLBACK_PATH = "/callback"
WAIT_SECONDS = 120
POLL_SECONDS = 1.0
HTTP_OK = 200
HTTP_NOT_FOUND = 404


class _RedirectHandler(http.server.BaseHTTPRequestHandler):
	received = None

	def do_GET(self):
		address = urlparse(self.path)
		is_callback = address.path == CALLBACK_PATH
		self.send_response(HTTP_OK if is_callback else HTTP_NOT_FOUND)
		self.end_headers()
		self.wfile.write(b"You can close this window." if is_callback else b"Not found")
		if is_callback:
			query = parse_qs(address.query)
			type(self).received = {
				"code": query.get("code", [None])[0],
				"state": query.get("state", [None])[0],
			}

	def log_message(self, message_format, *arguments):
		return None


class _LoopbackServer(http.server.HTTPServer):
	allow_reuse_address = False


def wait_for_redirect(authorize_address):
	_RedirectHandler.received = None
	print(f"Sign in at: {authorize_address}")
	try:
		server = _LoopbackServer((LOOPBACK_HOST, REGISTERED_PORT), _RedirectHandler)
	except OSError:
		print(f"Port {REGISTERED_PORT} is in use. Close the program that holds it and sign in again.")
	else:
		server.timeout = POLL_SECONDS
		webbrowser.open(authorize_address)
		deadline = time.monotonic() + WAIT_SECONDS
		while _RedirectHandler.received is None and time.monotonic() < deadline:
			server.handle_request()
		server.server_close()
	return _RedirectHandler.received
```

**Rules for the listener:** bind to loopback only; **check `state`** (another local process can connect to a fixed, advertised port); close the listener after the callback request; and handle a busy port. **A busy port raises `OSError` only if the listener does not reuse the address**: `socketserver` documents the class variable `allow_reuse_address` ("This defaults to False, and can be set in subclasses to change the policy"), and `http.server.HTTPServer` sets it to a true value, with which a second bind to the same address and port raised no error on Windows (checked on Windows; the behaviour on Linux and macOS is unchecked). The sketch therefore binds a subclass with `allow_reuse_address = False`, catches the `OSError`, and prints the address and a clear message instead of waiting. `handle_request` processes one request and returns after `timeout` seconds if none arrives, which is why the sketch loops until the callback or the **overall deadline** `WAIT_SECONDS` (`timeout` bounds each wait only, and the deadline bounds the whole wait): a stray request, such as a browser asking for an icon, is answered `404` and does not end the wait. `webbrowser.open` shows the address "using the default browser" and returns whether one was launched. **Where tokens are kept:** a file with restricted permissions or the operating system's keychain (unchecked on every system; no keychain package is named here). **The extractable-secret statement:** a command-line tool or desktop program shipped to people carries its client secret in a form anyone who holds the distribution can extract, and it protects nothing against the person running it (a documentation gap: Relativity documents no recommendation for a distributed client). **Compensating guidance** (NSerio recommendation): Trusted IPs where the users' outbound addresses are known and stable (checked at token issuance; applicability to Code *inferred*); the 60-minute Access Token Lifetime; refresh tokens only when needed; and **one OAuth2 Client per distribution**, so a leaked secret can be disabled and reset alone.

Verification: **Verification note.** **A token has a finite lifetime.** **The calling identity changes.** **The scheme changes from `Basic` to `Bearer`.** **A header set by a function the rule did not read, a merged mapping or a hook is not read**. The header carriers are documented for the pages read and their versions; other versions are unchecked.

OAuth2 Client: See [OAuth2 Client for Authorization Code](#oauth2-client-for-authorization-code).
<!-- generated:end PY-003 authorization-code -->

### PY-004 · UrllibBasicAuthHandlerConstruction

<a id="py-004-migration"></a>

Remove the Basic handler, the password manager and the opener built for them, and give `urllib.request.Request` a Bearer header instead. The handler is one construct and the password manager that holds the credential is another, which is a finding of its own as `PY-009`.

#### Client Credentials

<!-- generated:begin PY-004 client-credentials -->
**Flow.** This entry is for Client Credentials. The "Which flow" paragraph of PY-004 in [Part A](part-a-python.md#py-004) says when a match belongs to this flow.

Source form: **Applies to:** a Python script or service using the standard library's `urllib.request`.

```python
handler = urllib.request.HTTPBasicAuthHandler(password_manager)
opener = urllib.request.build_opener(handler)
response = opener.open(url)
```

Under token authentication: **First, the destination:** if this Relativity instance is not RelativityOne — including a Relativity Server instance, whose own documentation governs — no migration to RelativityOne applies and the rest of this entry does not apply.

```python
CSRF_HEADER_VALUE = "-"
request = urllib.request.Request(url, headers={"Authorization": f"Bearer {access_token}", "X-CSRF-Header": CSRF_HEADER_VALUE})
response = urllib.request.urlopen(request)
```

Remove the handler, the password manager and the opener built for them. `Request` accepts `headers`.

Credentials: **Where the credentials come from.** The source form establishes that a client sends Basic, not that the destination is RelativityOne; if it is not RelativityOne the migration is that service's own token mechanism, no migration to RelativityOne applies, and the reader stops here. If it is RelativityOne, the token comes from the token-acquisition part that follows, and the user name and password are replaced by the OAuth2 Client's client ID and secret, kept as Secret Management (Part B of the Migration Documentation) describes, never in source.

The text a match assigned Client Credentials receives after part 1. Create an OAuth2 Client with Flow Grant Type set to Client Credentials (part 3 lists its settings), then send a form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=client_credentials`, `client_id` and `client_secret`. The token is sent as `Authorization: Bearer <token>`, with `X-CSRF-Header: -` on REST and Kepler calls. **The endpoint is documented for this grant.** The scope depends on what the integration needs and **no single scope is required**: Relativity's documentation shows `SystemUserInfo` and `UserInfoAccess` and no page says which is correct or what either grants; use the value the API you call documents, and confirm it.

**Variant: a Python process acting as itself**

**Precondition: this variant is for a Python job, service or script that does not act for a signed-in person. If a person signs in, use Authorization Code.**

```python
import os
import time

import requests

IDENTITY_TOKEN_URL = "https://HOST/Relativity/Identity/connect/token"
EXPIRY_MARGIN_SECONDS = 60
REQUEST_TIMEOUT_SECONDS = 30

_cached_token = {"value": None, "expires_at": 0.0}


def get_access_token():
	now = time.monotonic()
	if _cached_token["value"] is None or _cached_token["expires_at"] - now < EXPIRY_MARGIN_SECONDS:
		response = requests.post(
			IDENTITY_TOKEN_URL,
			data={
				"grant_type": "client_credentials",
				"client_id": os.environ["RELATIVITY_CLIENT_ID"],
				"client_secret": os.environ["RELATIVITY_CLIENT_SECRET"],
			},
			timeout=REQUEST_TIMEOUT_SECONDS,
		)
		response.raise_for_status()
		payload = response.json()
		_cached_token["value"] = payload["access_token"]
		_cached_token["expires_at"] = now + payload["expires_in"]
	return _cached_token["value"]
```

`requests` form-encodes a dictionary passed as `data`, and `raise_for_status` raises on a 4XX or 5XX response. **The same token request in the other three authorized clients** (the cache and the credentials are as above):

```python
HTTP_OK = 200
token_request_fields = {
	"grant_type": "client_credentials",
	"client_id": os.environ["RELATIVITY_CLIENT_ID"],
	"client_secret": os.environ["RELATIVITY_CLIENT_SECRET"],
}

# httpx: a dictionary passed as data is form-encoded
response = httpx.post(IDENTITY_TOKEN_URL, data=token_request_fields, timeout=REQUEST_TIMEOUT_SECONDS)
response.raise_for_status()
payload = response.json()

# aiohttp: data= is form-encoded
async with session.post(IDENTITY_TOKEN_URL, data=token_request_fields) as response:
	response.raise_for_status()
	payload = await response.json()

# urllib3: PoolManager.request form-encodes the fields argument for POST only with encode_multipart=False
response = urllib3.PoolManager().request("POST", IDENTITY_TOKEN_URL, fields=token_request_fields, encode_multipart=False)
if response.status != HTTP_OK:
	raise RuntimeError(f"The token request failed with status {response.status}")
payload = response.json()
```

**Cache the token and re-acquire it on expiry — do not refresh.** Request a new token with the client secret shortly before the current one expires (the block uses a margin of 60 seconds), and once when a call is rejected for authentication. Client Credentials does not offer refresh tokens: **verified by NSerio against a live instance; not documented by Relativity** — the Allow Refresh Tokens control does not appear for that grant in the RelativityOne user interface. The recommended Access Token Lifetime is 60 minutes. **The client secret** belongs in the process environment or a secret store, as Secret Management (Part B of the Migration Documentation) describes, **never in source or in a tracked file**. A reset invalidates the previous secret immediately, so plan the cut-over (the two-client pattern is inferred). **Before deploying, confirm the calling identity:** every call now runs as the OAuth2 Client's Context User, not as the user in the replaced credential, and the non-administrator Context User is NSerio's recommendation. Trusted IPs restrict where a token can be requested, not a token already issued. **Whether `httpx` and `urllib3` accept a dictionary the way the pages read say is documented for the page's version only** (`httpx` shows no version): unchecked for other versions.

Verification: **Verification note.** **A token has a finite lifetime.** **The calling identity changes.** **This match records the handler; the credential given to the password manager is its own match at `PY-009`**, and both are removed by this change. The handler answers a Basic challenge, and **RelativityOne does not challenge** (**verified by NSerio against a live instance; not documented by Relativity**, on one instance): the handler was either never exercised against RelativityOne or the code sends Basic by another route; confirm before deleting. Behaviour before Python 3.15 is unchecked.

OAuth2 Client: See [OAuth2 Client for Client Credentials](#oauth2-client-for-client-credentials).
<!-- generated:end PY-004 client-credentials -->

#### Authorization Code

<!-- generated:begin PY-004 authorization-code -->
**Flow.** This entry is for Authorization Code. The "Which flow" paragraph of PY-004 in [Part A](part-a-python.md#py-004) says when a match belongs to this flow.

Source form: **Applies to:** a Python script or service using the standard library's `urllib.request`.

```python
handler = urllib.request.HTTPBasicAuthHandler(password_manager)
opener = urllib.request.build_opener(handler)
response = opener.open(url)
```

Under token authentication: **First, the destination:** if this Relativity instance is not RelativityOne — including a Relativity Server instance, whose own documentation governs — no migration to RelativityOne applies and the rest of this entry does not apply.

```python
CSRF_HEADER_VALUE = "-"
request = urllib.request.Request(url, headers={"Authorization": f"Bearer {access_token}", "X-CSRF-Header": CSRF_HEADER_VALUE})
response = urllib.request.urlopen(request)
```

Remove the handler, the password manager and the opener built for them. `Request` accepts `headers`.

Credentials: **Where the credentials come from.** The source form establishes that a client sends Basic, not that the destination is RelativityOne; if it is not RelativityOne the migration is that service's own token mechanism, no migration to RelativityOne applies, and the reader stops here. If it is RelativityOne, the token comes from the token-acquisition part that follows, and the user name and password are replaced by the OAuth2 Client's client ID and secret, kept as Secret Management (Part B of the Migration Documentation) describes, never in source.

**Common to both variants.** The person signs in on RelativityOne's own page and the code never holds their password. The text has a common part and **two variants by application type**, each opening with the precondition it is for: a server-side web application and a command-line or desktop tool. Read the precondition of each and discard the variant that does not apply.

**Sign in.** Replace the code that collected the person's credentials with a redirect to the authorize endpoint `<host>/Relativity/Identity/connect/authorize` (**verified by NSerio against a live instance; not documented by Relativity**), which leads to RelativityOne's login page. The request carries the standard authorization-code parameters — `client_id`, `response_type=code`, the registered `redirect_uri`, `state` checked on return, and `code_challenge` with `code_challenge_method=S256` when PKCE is used; these are standard OAuth (RFC 6749 and RFC 7636) and Relativity documents none of them.

**PKCE.** PKCE is required by Relativity's own documentation to obtain a refresh token and not otherwise: a Code request that does not ask for a refresh token works without it, **verified by NSerio against a live instance; not documented by Relativity**. NSerio recommends `S256` (NSerio recommendation) and does not claim Relativity accepts any particular method. The verifier is 43 to 128 characters of the unreserved set and the `S256` challenge is the base64url of the SHA-256 of the verifier; `secrets.token_urlsafe(32)` gives 43 characters from that set (that the two match is arithmetic, not a tested fact) and `hashlib.sha256` and `base64.urlsafe_b64encode` compute the challenge. **`state`** is generated, stored against the session and compared on return; `nonce` is not used, and Relativity does not document whether it enforces either.

**The client secret is required.** Code does not work without a client secret: tested and rejected, **verified by NSerio against a live instance; not documented by Relativity**, which confirms Relativity's own statement. **A secret kept in a program that is distributed is extractable**, so the exchange runs where a secret can be kept.

**Exchange the code.** A form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=authorization_code`, the code, the same `redirect_uri`, the client ID and the secret, plus the `code_verifier` when PKCE is used (**verified by NSerio against a live instance; not documented by Relativity**).

**Redirect URL.** A server-side application registers **one exact HTTPS URL per environment, no wildcards** (the matching rules are not documented and no claim is made about them). A plain-HTTP loopback Redirect URL `http://127.0.0.1:<port>/<path>` on a **fixed registered port** is accepted, **verified by NSerio against a live instance; not documented by Relativity**, and is the command-line shape. RFC 8252 requires an authorization server to allow any port for a loopback redirect; **Relativity's behaviour is the fixed registered port, and the RFC's rule is not claimed for it.**

**On expiry.** With Allow Refresh Tokens enabled and PKCE used, the code holds a refresh token and sends `grant_type=refresh_token` in place of the code exchange: *inferred* from the standard OAuth shape, not tested; Relativity does not document a refresh token's lifetime, rotation, revocation or revocation endpoint, nor what disabling the client does to tokens already issued. **A refresh token is a secret.** **Single sign-on:** sign-in happens on RelativityOne's own page, so the instance's single sign-on and two-factor rules apply unchanged (*inferred*). **The code stops collecting the password:** after this change nothing receives, stores or forwards the person's user name and password, and the code holds only tokens. Every call now runs as the person who signed in, so **the calling identity changes**. **Scope:** no scope set is prescribed for Code; `openid RelativityRequestOrigin UserInfo UserInfoAccess AuthenticationID RelativityWeb` works, **verified by NSerio against a live instance; not documented by Relativity**, as working and not as minimal.

**Variant 1 — a server-side web application (Flask, Django, FastAPI or similar)**

**Precondition: this variant is for code that runs in Python as a web server and acts for people who sign in to it. It is not for a command-line tool (variant 2).**

Standard server-side Code flow. A login route builds the authorize address with `state` (and the PKCE verifier where a refresh token is wanted), stores them against the visitor's session and redirects; RelativityOne redirects to the **callback route**, registered as an exact HTTPS URL; the callback checks `state` and exchanges the code with the secret. **The logic is framework-neutral; the framework supplies the routes and the session**, and the sketch shows the callback as a handler of the standard library's `http.server` so that it has no dependency: in a Flask, Django or FastAPI application the same two functions are called from the login and callback views, and the dictionary of pending logins is the framework's session (Flask's session holds data in a signed cookie that the user can read but not modify, so **NSerio recommends that it hold neither the verifier nor a token**; Django's session framework "stores data on the server side" with the cookie holding only a session ID, "unless you're using the cookie based backend" (`SESSION_ENGINE` set to `django.contrib.sessions.backends.signed_cookies`), whose data "can be read by the client"; the pages of FastAPI read name no session store). **No framework code is shown: none is installed and none is verified.**

```python
import base64
import hashlib
import http.server
import secrets
from urllib.parse import parse_qs, urlencode, urlparse

import requests

IDENTITY_BASE = "https://HOST/Relativity/Identity/connect"
REDIRECT_URI = "https://app.example.test/auth/callback"
CALLBACK_PATH = "/auth/callback"
RANDOM_BYTES = 32
REQUEST_TIMEOUT_SECONDS = 30
HTTP_OK = 200
HTTP_NOT_FOUND = 404
_pending_by_session = {}


def base64_url(raw_bytes):
	return base64.urlsafe_b64encode(raw_bytes).rstrip(b"=").decode("ascii")


def begin_login(session_id, client_id, want_refresh_token):
	state = secrets.token_urlsafe(RANDOM_BYTES)
	pending = {"state": state, "verifier": None}
	query = {
		"response_type": "code",
		"client_id": client_id,
		"redirect_uri": REDIRECT_URI,
		"state": state,
	}
	if want_refresh_token:
		verifier = secrets.token_urlsafe(RANDOM_BYTES)
		pending["verifier"] = verifier
		query["code_challenge"] = base64_url(hashlib.sha256(verifier.encode("ascii")).digest())
		query["code_challenge_method"] = "S256"
	_pending_by_session[session_id] = pending
	return f"{IDENTITY_BASE}/authorize?{urlencode(query)}"


def complete_login(session_id, client_id, client_secret, returned_code, returned_state):
	pending = _pending_by_session.pop(session_id, None)
	tokens = None
	state_matches = (
		pending is not None
		and returned_state is not None
		and secrets.compare_digest(returned_state.encode("utf-8"), pending["state"].encode("utf-8"))
	)
	if state_matches:
		fields = {
			"grant_type": "authorization_code",
			"code": returned_code,
			"redirect_uri": REDIRECT_URI,
			"client_id": client_id,
			"client_secret": client_secret,
		}
		if pending["verifier"] is not None:
			fields["code_verifier"] = pending["verifier"]
		response = requests.post(f"{IDENTITY_BASE}/token", data=fields, timeout=REQUEST_TIMEOUT_SECONDS)
		tokens = response.json() if response.status_code == HTTP_OK else None
	return tokens


class CallbackHandler(http.server.BaseHTTPRequestHandler):
	def do_GET(self):
		address = urlparse(self.path)
		is_callback = address.path == CALLBACK_PATH
		self.send_response(HTTP_OK if is_callback else HTTP_NOT_FOUND)
		self.end_headers()
		if is_callback:
			query = parse_qs(address.query)
			self.wfile.write(b"Signed in.")
			self.server.returned_code = query.get("code", [None])[0]
			self.server.returned_state = query.get("state", [None])[0]
```

`state` is consumed once and compared on return in constant time (`secrets.compare_digest`); a request with no pending entry or a different `state` gets no exchange. **The scope is not prescribed** (the sketch sends none). **Where the secret travels in the token request** (the body, as sketched, or HTTP Basic client authentication) is **not verified**; RFC 6749 allows either form for a client password. The sketch does not show the HTTP server's TLS, the session cookie or a session store that survives more than one process (the module dictionary does not). **The handler only records the returned `code` and `state`; in a real application the callback view calls `complete_login` with the visitor's own session identifier**, so that the pending entry of that visitor, and no other, is consumed. **Tokens stay on the server**, in a session store that is not exposed to a request.

**The `http.server` handler is a shape, not a deployment.** The `http.server` page states that the module "is not recommended for production. It only implements basic security checks" (Python 3.15.2). The handler above shows what the callback reads and answers, so that the sketch has no dependency; **an application that serves people runs the callback as a view of the framework it already uses** (Flask, Django or FastAPI), behind the TLS and the session store that framework and its server provide.

**Variant 2 — a command-line or desktop tool**

**Precondition: this variant is for code that runs on a person's own machine, as a command-line tool or a desktop shell, and signs that person in. It is not for a web server (variant 1).**

The process starts a listener from the standard library, bound to **`127.0.0.1` only**, on the **fixed registered port**, for **one callback request**; opens the system browser on the authorize address **and always prints the address too**, so a machine with no browser, an SSH session or a failed launch has a manual route; reads `code` and `state` from the callback request, closes the listener, checks `state`, and exchanges the code as variant 1's `complete_login` does (with the redirect URI `http://127.0.0.1:8123/callback`).

```python
import http.server
import time
import webbrowser
from urllib.parse import parse_qs, urlparse

LOOPBACK_HOST = "127.0.0.1"
REGISTERED_PORT = 8123
CALLBACK_PATH = "/callback"
WAIT_SECONDS = 120
POLL_SECONDS = 1.0
HTTP_OK = 200
HTTP_NOT_FOUND = 404


class _RedirectHandler(http.server.BaseHTTPRequestHandler):
	received = None

	def do_GET(self):
		address = urlparse(self.path)
		is_callback = address.path == CALLBACK_PATH
		self.send_response(HTTP_OK if is_callback else HTTP_NOT_FOUND)
		self.end_headers()
		self.wfile.write(b"You can close this window." if is_callback else b"Not found")
		if is_callback:
			query = parse_qs(address.query)
			type(self).received = {
				"code": query.get("code", [None])[0],
				"state": query.get("state", [None])[0],
			}

	def log_message(self, message_format, *arguments):
		return None


class _LoopbackServer(http.server.HTTPServer):
	allow_reuse_address = False


def wait_for_redirect(authorize_address):
	_RedirectHandler.received = None
	print(f"Sign in at: {authorize_address}")
	try:
		server = _LoopbackServer((LOOPBACK_HOST, REGISTERED_PORT), _RedirectHandler)
	except OSError:
		print(f"Port {REGISTERED_PORT} is in use. Close the program that holds it and sign in again.")
	else:
		server.timeout = POLL_SECONDS
		webbrowser.open(authorize_address)
		deadline = time.monotonic() + WAIT_SECONDS
		while _RedirectHandler.received is None and time.monotonic() < deadline:
			server.handle_request()
		server.server_close()
	return _RedirectHandler.received
```

**Rules for the listener:** bind to loopback only; **check `state`** (another local process can connect to a fixed, advertised port); close the listener after the callback request; and handle a busy port. **A busy port raises `OSError` only if the listener does not reuse the address**: `socketserver` documents the class variable `allow_reuse_address` ("This defaults to False, and can be set in subclasses to change the policy"), and `http.server.HTTPServer` sets it to a true value, with which a second bind to the same address and port raised no error on Windows (checked on Windows; the behaviour on Linux and macOS is unchecked). The sketch therefore binds a subclass with `allow_reuse_address = False`, catches the `OSError`, and prints the address and a clear message instead of waiting. `handle_request` processes one request and returns after `timeout` seconds if none arrives, which is why the sketch loops until the callback or the **overall deadline** `WAIT_SECONDS` (`timeout` bounds each wait only, and the deadline bounds the whole wait): a stray request, such as a browser asking for an icon, is answered `404` and does not end the wait. `webbrowser.open` shows the address "using the default browser" and returns whether one was launched. **Where tokens are kept:** a file with restricted permissions or the operating system's keychain (unchecked on every system; no keychain package is named here). **The extractable-secret statement:** a command-line tool or desktop program shipped to people carries its client secret in a form anyone who holds the distribution can extract, and it protects nothing against the person running it (a documentation gap: Relativity documents no recommendation for a distributed client). **Compensating guidance** (NSerio recommendation): Trusted IPs where the users' outbound addresses are known and stable (checked at token issuance; applicability to Code *inferred*); the 60-minute Access Token Lifetime; refresh tokens only when needed; and **one OAuth2 Client per distribution**, so a leaked secret can be disabled and reset alone.

Verification: **Verification note.** **A token has a finite lifetime.** **The calling identity changes.** **This match records the handler; the credential given to the password manager is its own match at `PY-009`**, and both are removed by this change. The handler answers a Basic challenge, and **RelativityOne does not challenge** (**verified by NSerio against a live instance; not documented by Relativity**, on one instance): the handler was either never exercised against RelativityOne or the code sends Basic by another route; confirm before deleting. Behaviour before Python 3.15 is unchecked.

OAuth2 Client: See [OAuth2 Client for Authorization Code](#oauth2-client-for-authorization-code).
<!-- generated:end PY-004 authorization-code -->

### PY-005 · Urllib3MakeHeadersBasicAuth

<a id="py-005-migration"></a>

Remove the `make_headers(basic_auth=...)` call. The documentation read for `make_headers` has a `basic_auth` option and a `proxy_basic_auth` option and no bearer option, so the Bearer header is written as a mapping.

#### Client Credentials

<!-- generated:begin PY-005 client-credentials -->
**Flow.** This entry is for Client Credentials. The "Which flow" paragraph of PY-005 in [Part A](part-a-python.md#py-005) says when a match belongs to this flow.

Source form: **Applies to:** a Python script or service; `urllib3`.

```python
headers = urllib3.util.make_headers(basic_auth=f"{user_name}:{secret}")
http = urllib3.PoolManager(headers=headers)
```

Under token authentication: **First, the destination:** if this Relativity instance is not RelativityOne — including a Relativity Server instance, whose own documentation governs — no migration to RelativityOne applies and the rest of this entry does not apply.

```python
CSRF_HEADER_VALUE = "-"
http = urllib3.PoolManager(headers={"Authorization": f"Bearer {access_token}", "X-CSRF-Header": CSRF_HEADER_VALUE})
response = http.request("GET", url)
```

Remove the `make_headers(basic_auth=…)` call. The page read for `make_headers` documents `basic_auth` and `proxy_basic_auth` and **no bearer option**, so the header is written as a mapping.

Credentials: **Where the credentials come from.** The source form establishes that a client sends Basic, not that the destination is RelativityOne; if it is not RelativityOne the migration is that service's own token mechanism, no migration to RelativityOne applies, and the reader stops here. If it is RelativityOne, the token comes from the token-acquisition part that follows, and the user name and password are replaced by the OAuth2 Client's client ID and secret, kept as Secret Management (Part B of the Migration Documentation) describes, never in source.

The text a match assigned Client Credentials receives after part 1. Create an OAuth2 Client with Flow Grant Type set to Client Credentials (part 3 lists its settings), then send a form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=client_credentials`, `client_id` and `client_secret`. The token is sent as `Authorization: Bearer <token>`, with `X-CSRF-Header: -` on REST and Kepler calls. **The endpoint is documented for this grant.** The scope depends on what the integration needs and **no single scope is required**: Relativity's documentation shows `SystemUserInfo` and `UserInfoAccess` and no page says which is correct or what either grants; use the value the API you call documents, and confirm it.

**Variant: a Python process acting as itself**

**Precondition: this variant is for a Python job, service or script that does not act for a signed-in person. If a person signs in, use Authorization Code.**

```python
import os
import time

import requests

IDENTITY_TOKEN_URL = "https://HOST/Relativity/Identity/connect/token"
EXPIRY_MARGIN_SECONDS = 60
REQUEST_TIMEOUT_SECONDS = 30

_cached_token = {"value": None, "expires_at": 0.0}


def get_access_token():
	now = time.monotonic()
	if _cached_token["value"] is None or _cached_token["expires_at"] - now < EXPIRY_MARGIN_SECONDS:
		response = requests.post(
			IDENTITY_TOKEN_URL,
			data={
				"grant_type": "client_credentials",
				"client_id": os.environ["RELATIVITY_CLIENT_ID"],
				"client_secret": os.environ["RELATIVITY_CLIENT_SECRET"],
			},
			timeout=REQUEST_TIMEOUT_SECONDS,
		)
		response.raise_for_status()
		payload = response.json()
		_cached_token["value"] = payload["access_token"]
		_cached_token["expires_at"] = now + payload["expires_in"]
	return _cached_token["value"]
```

`requests` form-encodes a dictionary passed as `data`, and `raise_for_status` raises on a 4XX or 5XX response. **The same token request in the other three authorized clients** (the cache and the credentials are as above):

```python
HTTP_OK = 200
token_request_fields = {
	"grant_type": "client_credentials",
	"client_id": os.environ["RELATIVITY_CLIENT_ID"],
	"client_secret": os.environ["RELATIVITY_CLIENT_SECRET"],
}

# httpx: a dictionary passed as data is form-encoded
response = httpx.post(IDENTITY_TOKEN_URL, data=token_request_fields, timeout=REQUEST_TIMEOUT_SECONDS)
response.raise_for_status()
payload = response.json()

# aiohttp: data= is form-encoded
async with session.post(IDENTITY_TOKEN_URL, data=token_request_fields) as response:
	response.raise_for_status()
	payload = await response.json()

# urllib3: PoolManager.request form-encodes the fields argument for POST only with encode_multipart=False
response = urllib3.PoolManager().request("POST", IDENTITY_TOKEN_URL, fields=token_request_fields, encode_multipart=False)
if response.status != HTTP_OK:
	raise RuntimeError(f"The token request failed with status {response.status}")
payload = response.json()
```

**Cache the token and re-acquire it on expiry — do not refresh.** Request a new token with the client secret shortly before the current one expires (the block uses a margin of 60 seconds), and once when a call is rejected for authentication. Client Credentials does not offer refresh tokens: **verified by NSerio against a live instance; not documented by Relativity** — the Allow Refresh Tokens control does not appear for that grant in the RelativityOne user interface. The recommended Access Token Lifetime is 60 minutes. **The client secret** belongs in the process environment or a secret store, as Secret Management (Part B of the Migration Documentation) describes, **never in source or in a tracked file**. A reset invalidates the previous secret immediately, so plan the cut-over (the two-client pattern is inferred). **Before deploying, confirm the calling identity:** every call now runs as the OAuth2 Client's Context User, not as the user in the replaced credential, and the non-administrator Context User is NSerio's recommendation. Trusted IPs restrict where a token can be requested, not a token already issued. **Whether `httpx` and `urllib3` accept a dictionary the way the pages read say is documented for the page's version only** (`httpx` shows no version): unchecked for other versions.

Verification: **Verification note.** **A token has a finite lifetime**: a `PoolManager` created once holds the header it was given; pass `headers` per `request` call. **The calling identity changes.** Documented for urllib3 2.8.0; earlier majors are unchecked.

OAuth2 Client: See [OAuth2 Client for Client Credentials](#oauth2-client-for-client-credentials).
<!-- generated:end PY-005 client-credentials -->

#### Authorization Code

<!-- generated:begin PY-005 authorization-code -->
**Flow.** This entry is for Authorization Code. The "Which flow" paragraph of PY-005 in [Part A](part-a-python.md#py-005) says when a match belongs to this flow.

Source form: **Applies to:** a Python script or service; `urllib3`.

```python
headers = urllib3.util.make_headers(basic_auth=f"{user_name}:{secret}")
http = urllib3.PoolManager(headers=headers)
```

Under token authentication: **First, the destination:** if this Relativity instance is not RelativityOne — including a Relativity Server instance, whose own documentation governs — no migration to RelativityOne applies and the rest of this entry does not apply.

```python
CSRF_HEADER_VALUE = "-"
http = urllib3.PoolManager(headers={"Authorization": f"Bearer {access_token}", "X-CSRF-Header": CSRF_HEADER_VALUE})
response = http.request("GET", url)
```

Remove the `make_headers(basic_auth=…)` call. The page read for `make_headers` documents `basic_auth` and `proxy_basic_auth` and **no bearer option**, so the header is written as a mapping.

Credentials: **Where the credentials come from.** The source form establishes that a client sends Basic, not that the destination is RelativityOne; if it is not RelativityOne the migration is that service's own token mechanism, no migration to RelativityOne applies, and the reader stops here. If it is RelativityOne, the token comes from the token-acquisition part that follows, and the user name and password are replaced by the OAuth2 Client's client ID and secret, kept as Secret Management (Part B of the Migration Documentation) describes, never in source.

**Common to both variants.** The person signs in on RelativityOne's own page and the code never holds their password. The text has a common part and **two variants by application type**, each opening with the precondition it is for: a server-side web application and a command-line or desktop tool. Read the precondition of each and discard the variant that does not apply.

**Sign in.** Replace the code that collected the person's credentials with a redirect to the authorize endpoint `<host>/Relativity/Identity/connect/authorize` (**verified by NSerio against a live instance; not documented by Relativity**), which leads to RelativityOne's login page. The request carries the standard authorization-code parameters — `client_id`, `response_type=code`, the registered `redirect_uri`, `state` checked on return, and `code_challenge` with `code_challenge_method=S256` when PKCE is used; these are standard OAuth (RFC 6749 and RFC 7636) and Relativity documents none of them.

**PKCE.** PKCE is required by Relativity's own documentation to obtain a refresh token and not otherwise: a Code request that does not ask for a refresh token works without it, **verified by NSerio against a live instance; not documented by Relativity**. NSerio recommends `S256` (NSerio recommendation) and does not claim Relativity accepts any particular method. The verifier is 43 to 128 characters of the unreserved set and the `S256` challenge is the base64url of the SHA-256 of the verifier; `secrets.token_urlsafe(32)` gives 43 characters from that set (that the two match is arithmetic, not a tested fact) and `hashlib.sha256` and `base64.urlsafe_b64encode` compute the challenge. **`state`** is generated, stored against the session and compared on return; `nonce` is not used, and Relativity does not document whether it enforces either.

**The client secret is required.** Code does not work without a client secret: tested and rejected, **verified by NSerio against a live instance; not documented by Relativity**, which confirms Relativity's own statement. **A secret kept in a program that is distributed is extractable**, so the exchange runs where a secret can be kept.

**Exchange the code.** A form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=authorization_code`, the code, the same `redirect_uri`, the client ID and the secret, plus the `code_verifier` when PKCE is used (**verified by NSerio against a live instance; not documented by Relativity**).

**Redirect URL.** A server-side application registers **one exact HTTPS URL per environment, no wildcards** (the matching rules are not documented and no claim is made about them). A plain-HTTP loopback Redirect URL `http://127.0.0.1:<port>/<path>` on a **fixed registered port** is accepted, **verified by NSerio against a live instance; not documented by Relativity**, and is the command-line shape. RFC 8252 requires an authorization server to allow any port for a loopback redirect; **Relativity's behaviour is the fixed registered port, and the RFC's rule is not claimed for it.**

**On expiry.** With Allow Refresh Tokens enabled and PKCE used, the code holds a refresh token and sends `grant_type=refresh_token` in place of the code exchange: *inferred* from the standard OAuth shape, not tested; Relativity does not document a refresh token's lifetime, rotation, revocation or revocation endpoint, nor what disabling the client does to tokens already issued. **A refresh token is a secret.** **Single sign-on:** sign-in happens on RelativityOne's own page, so the instance's single sign-on and two-factor rules apply unchanged (*inferred*). **The code stops collecting the password:** after this change nothing receives, stores or forwards the person's user name and password, and the code holds only tokens. Every call now runs as the person who signed in, so **the calling identity changes**. **Scope:** no scope set is prescribed for Code; `openid RelativityRequestOrigin UserInfo UserInfoAccess AuthenticationID RelativityWeb` works, **verified by NSerio against a live instance; not documented by Relativity**, as working and not as minimal.

**Variant 1 — a server-side web application (Flask, Django, FastAPI or similar)**

**Precondition: this variant is for code that runs in Python as a web server and acts for people who sign in to it. It is not for a command-line tool (variant 2).**

Standard server-side Code flow. A login route builds the authorize address with `state` (and the PKCE verifier where a refresh token is wanted), stores them against the visitor's session and redirects; RelativityOne redirects to the **callback route**, registered as an exact HTTPS URL; the callback checks `state` and exchanges the code with the secret. **The logic is framework-neutral; the framework supplies the routes and the session**, and the sketch shows the callback as a handler of the standard library's `http.server` so that it has no dependency: in a Flask, Django or FastAPI application the same two functions are called from the login and callback views, and the dictionary of pending logins is the framework's session (Flask's session holds data in a signed cookie that the user can read but not modify, so **NSerio recommends that it hold neither the verifier nor a token**; Django's session framework "stores data on the server side" with the cookie holding only a session ID, "unless you're using the cookie based backend" (`SESSION_ENGINE` set to `django.contrib.sessions.backends.signed_cookies`), whose data "can be read by the client"; the pages of FastAPI read name no session store). **No framework code is shown: none is installed and none is verified.**

```python
import base64
import hashlib
import http.server
import secrets
from urllib.parse import parse_qs, urlencode, urlparse

import requests

IDENTITY_BASE = "https://HOST/Relativity/Identity/connect"
REDIRECT_URI = "https://app.example.test/auth/callback"
CALLBACK_PATH = "/auth/callback"
RANDOM_BYTES = 32
REQUEST_TIMEOUT_SECONDS = 30
HTTP_OK = 200
HTTP_NOT_FOUND = 404
_pending_by_session = {}


def base64_url(raw_bytes):
	return base64.urlsafe_b64encode(raw_bytes).rstrip(b"=").decode("ascii")


def begin_login(session_id, client_id, want_refresh_token):
	state = secrets.token_urlsafe(RANDOM_BYTES)
	pending = {"state": state, "verifier": None}
	query = {
		"response_type": "code",
		"client_id": client_id,
		"redirect_uri": REDIRECT_URI,
		"state": state,
	}
	if want_refresh_token:
		verifier = secrets.token_urlsafe(RANDOM_BYTES)
		pending["verifier"] = verifier
		query["code_challenge"] = base64_url(hashlib.sha256(verifier.encode("ascii")).digest())
		query["code_challenge_method"] = "S256"
	_pending_by_session[session_id] = pending
	return f"{IDENTITY_BASE}/authorize?{urlencode(query)}"


def complete_login(session_id, client_id, client_secret, returned_code, returned_state):
	pending = _pending_by_session.pop(session_id, None)
	tokens = None
	state_matches = (
		pending is not None
		and returned_state is not None
		and secrets.compare_digest(returned_state.encode("utf-8"), pending["state"].encode("utf-8"))
	)
	if state_matches:
		fields = {
			"grant_type": "authorization_code",
			"code": returned_code,
			"redirect_uri": REDIRECT_URI,
			"client_id": client_id,
			"client_secret": client_secret,
		}
		if pending["verifier"] is not None:
			fields["code_verifier"] = pending["verifier"]
		response = requests.post(f"{IDENTITY_BASE}/token", data=fields, timeout=REQUEST_TIMEOUT_SECONDS)
		tokens = response.json() if response.status_code == HTTP_OK else None
	return tokens


class CallbackHandler(http.server.BaseHTTPRequestHandler):
	def do_GET(self):
		address = urlparse(self.path)
		is_callback = address.path == CALLBACK_PATH
		self.send_response(HTTP_OK if is_callback else HTTP_NOT_FOUND)
		self.end_headers()
		if is_callback:
			query = parse_qs(address.query)
			self.wfile.write(b"Signed in.")
			self.server.returned_code = query.get("code", [None])[0]
			self.server.returned_state = query.get("state", [None])[0]
```

`state` is consumed once and compared on return in constant time (`secrets.compare_digest`); a request with no pending entry or a different `state` gets no exchange. **The scope is not prescribed** (the sketch sends none). **Where the secret travels in the token request** (the body, as sketched, or HTTP Basic client authentication) is **not verified**; RFC 6749 allows either form for a client password. The sketch does not show the HTTP server's TLS, the session cookie or a session store that survives more than one process (the module dictionary does not). **The handler only records the returned `code` and `state`; in a real application the callback view calls `complete_login` with the visitor's own session identifier**, so that the pending entry of that visitor, and no other, is consumed. **Tokens stay on the server**, in a session store that is not exposed to a request.

**The `http.server` handler is a shape, not a deployment.** The `http.server` page states that the module "is not recommended for production. It only implements basic security checks" (Python 3.15.2). The handler above shows what the callback reads and answers, so that the sketch has no dependency; **an application that serves people runs the callback as a view of the framework it already uses** (Flask, Django or FastAPI), behind the TLS and the session store that framework and its server provide.

**Variant 2 — a command-line or desktop tool**

**Precondition: this variant is for code that runs on a person's own machine, as a command-line tool or a desktop shell, and signs that person in. It is not for a web server (variant 1).**

The process starts a listener from the standard library, bound to **`127.0.0.1` only**, on the **fixed registered port**, for **one callback request**; opens the system browser on the authorize address **and always prints the address too**, so a machine with no browser, an SSH session or a failed launch has a manual route; reads `code` and `state` from the callback request, closes the listener, checks `state`, and exchanges the code as variant 1's `complete_login` does (with the redirect URI `http://127.0.0.1:8123/callback`).

```python
import http.server
import time
import webbrowser
from urllib.parse import parse_qs, urlparse

LOOPBACK_HOST = "127.0.0.1"
REGISTERED_PORT = 8123
CALLBACK_PATH = "/callback"
WAIT_SECONDS = 120
POLL_SECONDS = 1.0
HTTP_OK = 200
HTTP_NOT_FOUND = 404


class _RedirectHandler(http.server.BaseHTTPRequestHandler):
	received = None

	def do_GET(self):
		address = urlparse(self.path)
		is_callback = address.path == CALLBACK_PATH
		self.send_response(HTTP_OK if is_callback else HTTP_NOT_FOUND)
		self.end_headers()
		self.wfile.write(b"You can close this window." if is_callback else b"Not found")
		if is_callback:
			query = parse_qs(address.query)
			type(self).received = {
				"code": query.get("code", [None])[0],
				"state": query.get("state", [None])[0],
			}

	def log_message(self, message_format, *arguments):
		return None


class _LoopbackServer(http.server.HTTPServer):
	allow_reuse_address = False


def wait_for_redirect(authorize_address):
	_RedirectHandler.received = None
	print(f"Sign in at: {authorize_address}")
	try:
		server = _LoopbackServer((LOOPBACK_HOST, REGISTERED_PORT), _RedirectHandler)
	except OSError:
		print(f"Port {REGISTERED_PORT} is in use. Close the program that holds it and sign in again.")
	else:
		server.timeout = POLL_SECONDS
		webbrowser.open(authorize_address)
		deadline = time.monotonic() + WAIT_SECONDS
		while _RedirectHandler.received is None and time.monotonic() < deadline:
			server.handle_request()
		server.server_close()
	return _RedirectHandler.received
```

**Rules for the listener:** bind to loopback only; **check `state`** (another local process can connect to a fixed, advertised port); close the listener after the callback request; and handle a busy port. **A busy port raises `OSError` only if the listener does not reuse the address**: `socketserver` documents the class variable `allow_reuse_address` ("This defaults to False, and can be set in subclasses to change the policy"), and `http.server.HTTPServer` sets it to a true value, with which a second bind to the same address and port raised no error on Windows (checked on Windows; the behaviour on Linux and macOS is unchecked). The sketch therefore binds a subclass with `allow_reuse_address = False`, catches the `OSError`, and prints the address and a clear message instead of waiting. `handle_request` processes one request and returns after `timeout` seconds if none arrives, which is why the sketch loops until the callback or the **overall deadline** `WAIT_SECONDS` (`timeout` bounds each wait only, and the deadline bounds the whole wait): a stray request, such as a browser asking for an icon, is answered `404` and does not end the wait. `webbrowser.open` shows the address "using the default browser" and returns whether one was launched. **Where tokens are kept:** a file with restricted permissions or the operating system's keychain (unchecked on every system; no keychain package is named here). **The extractable-secret statement:** a command-line tool or desktop program shipped to people carries its client secret in a form anyone who holds the distribution can extract, and it protects nothing against the person running it (a documentation gap: Relativity documents no recommendation for a distributed client). **Compensating guidance** (NSerio recommendation): Trusted IPs where the users' outbound addresses are known and stable (checked at token issuance; applicability to Code *inferred*); the 60-minute Access Token Lifetime; refresh tokens only when needed; and **one OAuth2 Client per distribution**, so a leaked secret can be disabled and reset alone.

Verification: **Verification note.** **A token has a finite lifetime**: a `PoolManager` created once holds the header it was given; pass `headers` per `request` call. **The calling identity changes.** Documented for urllib3 2.8.0; earlier majors are unchecked.

OAuth2 Client: See [OAuth2 Client for Authorization Code](#oauth2-client-for-authorization-code).
<!-- generated:end PY-005 authorization-code -->

### PY-006 · UnnamedAuthTuple

<a id="py-006-migration"></a>

Remove the `auth` pair and the `auth` attribute assignment, and send a Bearer token in an `Authorization` header instead. **The receiver of the pair is not visible.** If it is not an HTTP client (a database driver, a graph database or a message broker takes the same keyword for its own login), the finding is not Basic Authentication to RelativityOne and the migration is that service's own: the code alone cannot tell, so check what receives the pair.

#### Client Credentials

<!-- generated:begin PY-006 client-credentials -->
**Flow.** This entry is for Client Credentials. The "Which flow" paragraph of PY-006 in [Part A](part-a-python.md#py-006) says when a match belongs to this flow.

Source form: **Applies to:** a Python script, service or tool; the receiver of the tuple is not named (a `Session`, a client held in a variable, an aliased module).

```python
response = session.get(url, auth=(user_name, secret))
session.auth = (user_name, secret)
```

Under token authentication: **First, the destination:** if this Relativity instance is not RelativityOne — including a Relativity Server instance, whose own documentation governs — no migration to RelativityOne applies and the rest of this entry does not apply.

```python
CSRF_HEADER_VALUE = "-"
response = session.get(url, headers={"Authorization": f"Bearer {access_token}", "X-CSRF-Header": CSRF_HEADER_VALUE})
session.headers.update({"Authorization": f"Bearer {access_token}", "X-CSRF-Header": CSRF_HEADER_VALUE})
```

Remove the `auth` tuple and the `auth` attribute assignment. **If the receiver is not an HTTP client** (a database driver, a graph database or a message broker takes the same keyword for its own login), this match is not Basic Authentication to Relativity and the migration is that service's own: **the rule cannot tell**.

Credentials: **Where the credentials come from.** The source form establishes that a client sends Basic, not that the destination is RelativityOne; if it is not RelativityOne the migration is that service's own token mechanism, no migration to RelativityOne applies, and the reader stops here. If it is RelativityOne, the token comes from the token-acquisition part that follows, and the user name and password are replaced by the OAuth2 Client's client ID and secret, kept as Secret Management (Part B of the Migration Documentation) describes, never in source.

The text a match assigned Client Credentials receives after part 1. Create an OAuth2 Client with Flow Grant Type set to Client Credentials (part 3 lists its settings), then send a form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=client_credentials`, `client_id` and `client_secret`. The token is sent as `Authorization: Bearer <token>`, with `X-CSRF-Header: -` on REST and Kepler calls. **The endpoint is documented for this grant.** The scope depends on what the integration needs and **no single scope is required**: Relativity's documentation shows `SystemUserInfo` and `UserInfoAccess` and no page says which is correct or what either grants; use the value the API you call documents, and confirm it.

**Variant: a Python process acting as itself**

**Precondition: this variant is for a Python job, service or script that does not act for a signed-in person. If a person signs in, use Authorization Code.**

```python
import os
import time

import requests

IDENTITY_TOKEN_URL = "https://HOST/Relativity/Identity/connect/token"
EXPIRY_MARGIN_SECONDS = 60
REQUEST_TIMEOUT_SECONDS = 30

_cached_token = {"value": None, "expires_at": 0.0}


def get_access_token():
	now = time.monotonic()
	if _cached_token["value"] is None or _cached_token["expires_at"] - now < EXPIRY_MARGIN_SECONDS:
		response = requests.post(
			IDENTITY_TOKEN_URL,
			data={
				"grant_type": "client_credentials",
				"client_id": os.environ["RELATIVITY_CLIENT_ID"],
				"client_secret": os.environ["RELATIVITY_CLIENT_SECRET"],
			},
			timeout=REQUEST_TIMEOUT_SECONDS,
		)
		response.raise_for_status()
		payload = response.json()
		_cached_token["value"] = payload["access_token"]
		_cached_token["expires_at"] = now + payload["expires_in"]
	return _cached_token["value"]
```

`requests` form-encodes a dictionary passed as `data`, and `raise_for_status` raises on a 4XX or 5XX response. **The same token request in the other three authorized clients** (the cache and the credentials are as above):

```python
HTTP_OK = 200
token_request_fields = {
	"grant_type": "client_credentials",
	"client_id": os.environ["RELATIVITY_CLIENT_ID"],
	"client_secret": os.environ["RELATIVITY_CLIENT_SECRET"],
}

# httpx: a dictionary passed as data is form-encoded
response = httpx.post(IDENTITY_TOKEN_URL, data=token_request_fields, timeout=REQUEST_TIMEOUT_SECONDS)
response.raise_for_status()
payload = response.json()

# aiohttp: data= is form-encoded
async with session.post(IDENTITY_TOKEN_URL, data=token_request_fields) as response:
	response.raise_for_status()
	payload = await response.json()

# urllib3: PoolManager.request form-encodes the fields argument for POST only with encode_multipart=False
response = urllib3.PoolManager().request("POST", IDENTITY_TOKEN_URL, fields=token_request_fields, encode_multipart=False)
if response.status != HTTP_OK:
	raise RuntimeError(f"The token request failed with status {response.status}")
payload = response.json()
```

**Cache the token and re-acquire it on expiry — do not refresh.** Request a new token with the client secret shortly before the current one expires (the block uses a margin of 60 seconds), and once when a call is rejected for authentication. Client Credentials does not offer refresh tokens: **verified by NSerio against a live instance; not documented by Relativity** — the Allow Refresh Tokens control does not appear for that grant in the RelativityOne user interface. The recommended Access Token Lifetime is 60 minutes. **The client secret** belongs in the process environment or a secret store, as Secret Management (Part B of the Migration Documentation) describes, **never in source or in a tracked file**. A reset invalidates the previous secret immediately, so plan the cut-over (the two-client pattern is inferred). **Before deploying, confirm the calling identity:** every call now runs as the OAuth2 Client's Context User, not as the user in the replaced credential, and the non-administrator Context User is NSerio's recommendation. Trusted IPs restrict where a token can be requested, not a token already issued. **Whether `httpx` and `urllib3` accept a dictionary the way the pages read say is documented for the page's version only** (`httpx` shows no version): unchecked for other versions.

Verification: **Verification note.** **This match is Medium because the receiver is not named**: the tuple is documented as Basic for a named `requests` or `httpx` call and not for another receiver. `Session.headers.update` and the session defaults are documented for `requests` 2.34.2. **A token has a finite lifetime.** **The calling identity changes.**

OAuth2 Client: See [OAuth2 Client for Client Credentials](#oauth2-client-for-client-credentials).
<!-- generated:end PY-006 client-credentials -->

#### Authorization Code

<!-- generated:begin PY-006 authorization-code -->
**Flow.** This entry is for Authorization Code. The "Which flow" paragraph of PY-006 in [Part A](part-a-python.md#py-006) says when a match belongs to this flow.

Source form: **Applies to:** a Python script, service or tool; the receiver of the tuple is not named (a `Session`, a client held in a variable, an aliased module).

```python
response = session.get(url, auth=(user_name, secret))
session.auth = (user_name, secret)
```

Under token authentication: **First, the destination:** if this Relativity instance is not RelativityOne — including a Relativity Server instance, whose own documentation governs — no migration to RelativityOne applies and the rest of this entry does not apply.

```python
CSRF_HEADER_VALUE = "-"
response = session.get(url, headers={"Authorization": f"Bearer {access_token}", "X-CSRF-Header": CSRF_HEADER_VALUE})
session.headers.update({"Authorization": f"Bearer {access_token}", "X-CSRF-Header": CSRF_HEADER_VALUE})
```

Remove the `auth` tuple and the `auth` attribute assignment. **If the receiver is not an HTTP client** (a database driver, a graph database or a message broker takes the same keyword for its own login), this match is not Basic Authentication to Relativity and the migration is that service's own: **the rule cannot tell**.

Credentials: **Where the credentials come from.** The source form establishes that a client sends Basic, not that the destination is RelativityOne; if it is not RelativityOne the migration is that service's own token mechanism, no migration to RelativityOne applies, and the reader stops here. If it is RelativityOne, the token comes from the token-acquisition part that follows, and the user name and password are replaced by the OAuth2 Client's client ID and secret, kept as Secret Management (Part B of the Migration Documentation) describes, never in source.

**Common to both variants.** The person signs in on RelativityOne's own page and the code never holds their password. The text has a common part and **two variants by application type**, each opening with the precondition it is for: a server-side web application and a command-line or desktop tool. Read the precondition of each and discard the variant that does not apply.

**Sign in.** Replace the code that collected the person's credentials with a redirect to the authorize endpoint `<host>/Relativity/Identity/connect/authorize` (**verified by NSerio against a live instance; not documented by Relativity**), which leads to RelativityOne's login page. The request carries the standard authorization-code parameters — `client_id`, `response_type=code`, the registered `redirect_uri`, `state` checked on return, and `code_challenge` with `code_challenge_method=S256` when PKCE is used; these are standard OAuth (RFC 6749 and RFC 7636) and Relativity documents none of them.

**PKCE.** PKCE is required by Relativity's own documentation to obtain a refresh token and not otherwise: a Code request that does not ask for a refresh token works without it, **verified by NSerio against a live instance; not documented by Relativity**. NSerio recommends `S256` (NSerio recommendation) and does not claim Relativity accepts any particular method. The verifier is 43 to 128 characters of the unreserved set and the `S256` challenge is the base64url of the SHA-256 of the verifier; `secrets.token_urlsafe(32)` gives 43 characters from that set (that the two match is arithmetic, not a tested fact) and `hashlib.sha256` and `base64.urlsafe_b64encode` compute the challenge. **`state`** is generated, stored against the session and compared on return; `nonce` is not used, and Relativity does not document whether it enforces either.

**The client secret is required.** Code does not work without a client secret: tested and rejected, **verified by NSerio against a live instance; not documented by Relativity**, which confirms Relativity's own statement. **A secret kept in a program that is distributed is extractable**, so the exchange runs where a secret can be kept.

**Exchange the code.** A form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=authorization_code`, the code, the same `redirect_uri`, the client ID and the secret, plus the `code_verifier` when PKCE is used (**verified by NSerio against a live instance; not documented by Relativity**).

**Redirect URL.** A server-side application registers **one exact HTTPS URL per environment, no wildcards** (the matching rules are not documented and no claim is made about them). A plain-HTTP loopback Redirect URL `http://127.0.0.1:<port>/<path>` on a **fixed registered port** is accepted, **verified by NSerio against a live instance; not documented by Relativity**, and is the command-line shape. RFC 8252 requires an authorization server to allow any port for a loopback redirect; **Relativity's behaviour is the fixed registered port, and the RFC's rule is not claimed for it.**

**On expiry.** With Allow Refresh Tokens enabled and PKCE used, the code holds a refresh token and sends `grant_type=refresh_token` in place of the code exchange: *inferred* from the standard OAuth shape, not tested; Relativity does not document a refresh token's lifetime, rotation, revocation or revocation endpoint, nor what disabling the client does to tokens already issued. **A refresh token is a secret.** **Single sign-on:** sign-in happens on RelativityOne's own page, so the instance's single sign-on and two-factor rules apply unchanged (*inferred*). **The code stops collecting the password:** after this change nothing receives, stores or forwards the person's user name and password, and the code holds only tokens. Every call now runs as the person who signed in, so **the calling identity changes**. **Scope:** no scope set is prescribed for Code; `openid RelativityRequestOrigin UserInfo UserInfoAccess AuthenticationID RelativityWeb` works, **verified by NSerio against a live instance; not documented by Relativity**, as working and not as minimal.

**Variant 1 — a server-side web application (Flask, Django, FastAPI or similar)**

**Precondition: this variant is for code that runs in Python as a web server and acts for people who sign in to it. It is not for a command-line tool (variant 2).**

Standard server-side Code flow. A login route builds the authorize address with `state` (and the PKCE verifier where a refresh token is wanted), stores them against the visitor's session and redirects; RelativityOne redirects to the **callback route**, registered as an exact HTTPS URL; the callback checks `state` and exchanges the code with the secret. **The logic is framework-neutral; the framework supplies the routes and the session**, and the sketch shows the callback as a handler of the standard library's `http.server` so that it has no dependency: in a Flask, Django or FastAPI application the same two functions are called from the login and callback views, and the dictionary of pending logins is the framework's session (Flask's session holds data in a signed cookie that the user can read but not modify, so **NSerio recommends that it hold neither the verifier nor a token**; Django's session framework "stores data on the server side" with the cookie holding only a session ID, "unless you're using the cookie based backend" (`SESSION_ENGINE` set to `django.contrib.sessions.backends.signed_cookies`), whose data "can be read by the client"; the pages of FastAPI read name no session store). **No framework code is shown: none is installed and none is verified.**

```python
import base64
import hashlib
import http.server
import secrets
from urllib.parse import parse_qs, urlencode, urlparse

import requests

IDENTITY_BASE = "https://HOST/Relativity/Identity/connect"
REDIRECT_URI = "https://app.example.test/auth/callback"
CALLBACK_PATH = "/auth/callback"
RANDOM_BYTES = 32
REQUEST_TIMEOUT_SECONDS = 30
HTTP_OK = 200
HTTP_NOT_FOUND = 404
_pending_by_session = {}


def base64_url(raw_bytes):
	return base64.urlsafe_b64encode(raw_bytes).rstrip(b"=").decode("ascii")


def begin_login(session_id, client_id, want_refresh_token):
	state = secrets.token_urlsafe(RANDOM_BYTES)
	pending = {"state": state, "verifier": None}
	query = {
		"response_type": "code",
		"client_id": client_id,
		"redirect_uri": REDIRECT_URI,
		"state": state,
	}
	if want_refresh_token:
		verifier = secrets.token_urlsafe(RANDOM_BYTES)
		pending["verifier"] = verifier
		query["code_challenge"] = base64_url(hashlib.sha256(verifier.encode("ascii")).digest())
		query["code_challenge_method"] = "S256"
	_pending_by_session[session_id] = pending
	return f"{IDENTITY_BASE}/authorize?{urlencode(query)}"


def complete_login(session_id, client_id, client_secret, returned_code, returned_state):
	pending = _pending_by_session.pop(session_id, None)
	tokens = None
	state_matches = (
		pending is not None
		and returned_state is not None
		and secrets.compare_digest(returned_state.encode("utf-8"), pending["state"].encode("utf-8"))
	)
	if state_matches:
		fields = {
			"grant_type": "authorization_code",
			"code": returned_code,
			"redirect_uri": REDIRECT_URI,
			"client_id": client_id,
			"client_secret": client_secret,
		}
		if pending["verifier"] is not None:
			fields["code_verifier"] = pending["verifier"]
		response = requests.post(f"{IDENTITY_BASE}/token", data=fields, timeout=REQUEST_TIMEOUT_SECONDS)
		tokens = response.json() if response.status_code == HTTP_OK else None
	return tokens


class CallbackHandler(http.server.BaseHTTPRequestHandler):
	def do_GET(self):
		address = urlparse(self.path)
		is_callback = address.path == CALLBACK_PATH
		self.send_response(HTTP_OK if is_callback else HTTP_NOT_FOUND)
		self.end_headers()
		if is_callback:
			query = parse_qs(address.query)
			self.wfile.write(b"Signed in.")
			self.server.returned_code = query.get("code", [None])[0]
			self.server.returned_state = query.get("state", [None])[0]
```

`state` is consumed once and compared on return in constant time (`secrets.compare_digest`); a request with no pending entry or a different `state` gets no exchange. **The scope is not prescribed** (the sketch sends none). **Where the secret travels in the token request** (the body, as sketched, or HTTP Basic client authentication) is **not verified**; RFC 6749 allows either form for a client password. The sketch does not show the HTTP server's TLS, the session cookie or a session store that survives more than one process (the module dictionary does not). **The handler only records the returned `code` and `state`; in a real application the callback view calls `complete_login` with the visitor's own session identifier**, so that the pending entry of that visitor, and no other, is consumed. **Tokens stay on the server**, in a session store that is not exposed to a request.

**The `http.server` handler is a shape, not a deployment.** The `http.server` page states that the module "is not recommended for production. It only implements basic security checks" (Python 3.15.2). The handler above shows what the callback reads and answers, so that the sketch has no dependency; **an application that serves people runs the callback as a view of the framework it already uses** (Flask, Django or FastAPI), behind the TLS and the session store that framework and its server provide.

**Variant 2 — a command-line or desktop tool**

**Precondition: this variant is for code that runs on a person's own machine, as a command-line tool or a desktop shell, and signs that person in. It is not for a web server (variant 1).**

The process starts a listener from the standard library, bound to **`127.0.0.1` only**, on the **fixed registered port**, for **one callback request**; opens the system browser on the authorize address **and always prints the address too**, so a machine with no browser, an SSH session or a failed launch has a manual route; reads `code` and `state` from the callback request, closes the listener, checks `state`, and exchanges the code as variant 1's `complete_login` does (with the redirect URI `http://127.0.0.1:8123/callback`).

```python
import http.server
import time
import webbrowser
from urllib.parse import parse_qs, urlparse

LOOPBACK_HOST = "127.0.0.1"
REGISTERED_PORT = 8123
CALLBACK_PATH = "/callback"
WAIT_SECONDS = 120
POLL_SECONDS = 1.0
HTTP_OK = 200
HTTP_NOT_FOUND = 404


class _RedirectHandler(http.server.BaseHTTPRequestHandler):
	received = None

	def do_GET(self):
		address = urlparse(self.path)
		is_callback = address.path == CALLBACK_PATH
		self.send_response(HTTP_OK if is_callback else HTTP_NOT_FOUND)
		self.end_headers()
		self.wfile.write(b"You can close this window." if is_callback else b"Not found")
		if is_callback:
			query = parse_qs(address.query)
			type(self).received = {
				"code": query.get("code", [None])[0],
				"state": query.get("state", [None])[0],
			}

	def log_message(self, message_format, *arguments):
		return None


class _LoopbackServer(http.server.HTTPServer):
	allow_reuse_address = False


def wait_for_redirect(authorize_address):
	_RedirectHandler.received = None
	print(f"Sign in at: {authorize_address}")
	try:
		server = _LoopbackServer((LOOPBACK_HOST, REGISTERED_PORT), _RedirectHandler)
	except OSError:
		print(f"Port {REGISTERED_PORT} is in use. Close the program that holds it and sign in again.")
	else:
		server.timeout = POLL_SECONDS
		webbrowser.open(authorize_address)
		deadline = time.monotonic() + WAIT_SECONDS
		while _RedirectHandler.received is None and time.monotonic() < deadline:
			server.handle_request()
		server.server_close()
	return _RedirectHandler.received
```

**Rules for the listener:** bind to loopback only; **check `state`** (another local process can connect to a fixed, advertised port); close the listener after the callback request; and handle a busy port. **A busy port raises `OSError` only if the listener does not reuse the address**: `socketserver` documents the class variable `allow_reuse_address` ("This defaults to False, and can be set in subclasses to change the policy"), and `http.server.HTTPServer` sets it to a true value, with which a second bind to the same address and port raised no error on Windows (checked on Windows; the behaviour on Linux and macOS is unchecked). The sketch therefore binds a subclass with `allow_reuse_address = False`, catches the `OSError`, and prints the address and a clear message instead of waiting. `handle_request` processes one request and returns after `timeout` seconds if none arrives, which is why the sketch loops until the callback or the **overall deadline** `WAIT_SECONDS` (`timeout` bounds each wait only, and the deadline bounds the whole wait): a stray request, such as a browser asking for an icon, is answered `404` and does not end the wait. `webbrowser.open` shows the address "using the default browser" and returns whether one was launched. **Where tokens are kept:** a file with restricted permissions or the operating system's keychain (unchecked on every system; no keychain package is named here). **The extractable-secret statement:** a command-line tool or desktop program shipped to people carries its client secret in a form anyone who holds the distribution can extract, and it protects nothing against the person running it (a documentation gap: Relativity documents no recommendation for a distributed client). **Compensating guidance** (NSerio recommendation): Trusted IPs where the users' outbound addresses are known and stable (checked at token issuance; applicability to Code *inferred*); the 60-minute Access Token Lifetime; refresh tokens only when needed; and **one OAuth2 Client per distribution**, so a leaked secret can be disabled and reset alone.

Verification: **Verification note.** **This match is Medium because the receiver is not named**: the tuple is documented as Basic for a named `requests` or `httpx` call and not for another receiver. `Session.headers.update` and the session defaults are documented for `requests` 2.34.2. **A token has a finite lifetime.** **The calling identity changes.**

OAuth2 Client: See [OAuth2 Client for Authorization Code](#oauth2-client-for-authorization-code).
<!-- generated:end PY-006 authorization-code -->

### PY-007 · Base64UserPasswordPair

<a id="py-007-migration"></a>

Delete the encoder call and the pair it builds, and send a Bearer token in the `Authorization` header instead. Nothing about the user name and the password survives in the code. If the encoder is held in a name and used in another statement, follow the name to the header that carries it.

#### Client Credentials

<!-- generated:begin PY-007 client-credentials -->
**Flow.** This entry is for Client Credentials. The "Which flow" paragraph of PY-007 in [Part A](part-a-python.md#py-007) says when a match belongs to this flow.

Source form: **Applies to:** a Python script or service that builds a Basic credential by hand.

```python
token = base64.b64encode(f"{user_name}:{secret}".encode()).decode()
headers = {"Authorization": "Basic " + token}
```

Under token authentication: **First, the destination:** if this Relativity instance is not RelativityOne — including a Relativity Server instance, whose own documentation governs — no migration to RelativityOne applies and the rest of this entry does not apply.

```python
CSRF_HEADER_VALUE = "-"
headers = {"Authorization": f"Bearer {access_token}", "X-CSRF-Header": CSRF_HEADER_VALUE}
```

Delete the encoder call and the pair it builds; nothing about the user name and password survives in the code.

Credentials: **Where the credentials come from.** The source form establishes that a client sends Basic, not that the destination is RelativityOne; if it is not RelativityOne the migration is that service's own token mechanism, no migration to RelativityOne applies, and the reader stops here. If it is RelativityOne, the token comes from the token-acquisition part that follows, and the user name and password are replaced by the OAuth2 Client's client ID and secret, kept as Secret Management (Part B of the Migration Documentation) describes, never in source.

The text a match assigned Client Credentials receives after part 1. Create an OAuth2 Client with Flow Grant Type set to Client Credentials (part 3 lists its settings), then send a form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=client_credentials`, `client_id` and `client_secret`. The token is sent as `Authorization: Bearer <token>`, with `X-CSRF-Header: -` on REST and Kepler calls. **The endpoint is documented for this grant.** The scope depends on what the integration needs and **no single scope is required**: Relativity's documentation shows `SystemUserInfo` and `UserInfoAccess` and no page says which is correct or what either grants; use the value the API you call documents, and confirm it.

**Variant: a Python process acting as itself**

**Precondition: this variant is for a Python job, service or script that does not act for a signed-in person. If a person signs in, use Authorization Code.**

```python
import os
import time

import requests

IDENTITY_TOKEN_URL = "https://HOST/Relativity/Identity/connect/token"
EXPIRY_MARGIN_SECONDS = 60
REQUEST_TIMEOUT_SECONDS = 30

_cached_token = {"value": None, "expires_at": 0.0}


def get_access_token():
	now = time.monotonic()
	if _cached_token["value"] is None or _cached_token["expires_at"] - now < EXPIRY_MARGIN_SECONDS:
		response = requests.post(
			IDENTITY_TOKEN_URL,
			data={
				"grant_type": "client_credentials",
				"client_id": os.environ["RELATIVITY_CLIENT_ID"],
				"client_secret": os.environ["RELATIVITY_CLIENT_SECRET"],
			},
			timeout=REQUEST_TIMEOUT_SECONDS,
		)
		response.raise_for_status()
		payload = response.json()
		_cached_token["value"] = payload["access_token"]
		_cached_token["expires_at"] = now + payload["expires_in"]
	return _cached_token["value"]
```

`requests` form-encodes a dictionary passed as `data`, and `raise_for_status` raises on a 4XX or 5XX response. **The same token request in the other three authorized clients** (the cache and the credentials are as above):

```python
HTTP_OK = 200
token_request_fields = {
	"grant_type": "client_credentials",
	"client_id": os.environ["RELATIVITY_CLIENT_ID"],
	"client_secret": os.environ["RELATIVITY_CLIENT_SECRET"],
}

# httpx: a dictionary passed as data is form-encoded
response = httpx.post(IDENTITY_TOKEN_URL, data=token_request_fields, timeout=REQUEST_TIMEOUT_SECONDS)
response.raise_for_status()
payload = response.json()

# aiohttp: data= is form-encoded
async with session.post(IDENTITY_TOKEN_URL, data=token_request_fields) as response:
	response.raise_for_status()
	payload = await response.json()

# urllib3: PoolManager.request form-encodes the fields argument for POST only with encode_multipart=False
response = urllib3.PoolManager().request("POST", IDENTITY_TOKEN_URL, fields=token_request_fields, encode_multipart=False)
if response.status != HTTP_OK:
	raise RuntimeError(f"The token request failed with status {response.status}")
payload = response.json()
```

**Cache the token and re-acquire it on expiry — do not refresh.** Request a new token with the client secret shortly before the current one expires (the block uses a margin of 60 seconds), and once when a call is rejected for authentication. Client Credentials does not offer refresh tokens: **verified by NSerio against a live instance; not documented by Relativity** — the Allow Refresh Tokens control does not appear for that grant in the RelativityOne user interface. The recommended Access Token Lifetime is 60 minutes. **The client secret** belongs in the process environment or a secret store, as Secret Management (Part B of the Migration Documentation) describes, **never in source or in a tracked file**. A reset invalidates the previous secret immediately, so plan the cut-over (the two-client pattern is inferred). **Before deploying, confirm the calling identity:** every call now runs as the OAuth2 Client's Context User, not as the user in the replaced credential, and the non-administrator Context User is NSerio's recommendation. Trusted IPs restrict where a token can be requested, not a token already issued. **Whether `httpx` and `urllib3` accept a dictionary the way the pages read say is documented for the page's version only** (`httpx` shows no version): unchecked for other versions.

Verification: **Verification note.** **This match is Medium: the call encodes a `user:password` pair and the rule saw `Basic` or `Authorization` nearby, not that it is sent to RelativityOne.** **A split construction** (the encoder in one statement, the header in another) is a second match that nothing merges. **A token has a finite lifetime.** **The calling identity changes.**

OAuth2 Client: See [OAuth2 Client for Client Credentials](#oauth2-client-for-client-credentials).
<!-- generated:end PY-007 client-credentials -->

#### Authorization Code

<!-- generated:begin PY-007 authorization-code -->
**Flow.** This entry is for Authorization Code. The "Which flow" paragraph of PY-007 in [Part A](part-a-python.md#py-007) says when a match belongs to this flow.

Source form: **Applies to:** a Python script or service that builds a Basic credential by hand.

```python
token = base64.b64encode(f"{user_name}:{secret}".encode()).decode()
headers = {"Authorization": "Basic " + token}
```

Under token authentication: **First, the destination:** if this Relativity instance is not RelativityOne — including a Relativity Server instance, whose own documentation governs — no migration to RelativityOne applies and the rest of this entry does not apply.

```python
CSRF_HEADER_VALUE = "-"
headers = {"Authorization": f"Bearer {access_token}", "X-CSRF-Header": CSRF_HEADER_VALUE}
```

Delete the encoder call and the pair it builds; nothing about the user name and password survives in the code.

Credentials: **Where the credentials come from.** The source form establishes that a client sends Basic, not that the destination is RelativityOne; if it is not RelativityOne the migration is that service's own token mechanism, no migration to RelativityOne applies, and the reader stops here. If it is RelativityOne, the token comes from the token-acquisition part that follows, and the user name and password are replaced by the OAuth2 Client's client ID and secret, kept as Secret Management (Part B of the Migration Documentation) describes, never in source.

**Common to both variants.** The person signs in on RelativityOne's own page and the code never holds their password. The text has a common part and **two variants by application type**, each opening with the precondition it is for: a server-side web application and a command-line or desktop tool. Read the precondition of each and discard the variant that does not apply.

**Sign in.** Replace the code that collected the person's credentials with a redirect to the authorize endpoint `<host>/Relativity/Identity/connect/authorize` (**verified by NSerio against a live instance; not documented by Relativity**), which leads to RelativityOne's login page. The request carries the standard authorization-code parameters — `client_id`, `response_type=code`, the registered `redirect_uri`, `state` checked on return, and `code_challenge` with `code_challenge_method=S256` when PKCE is used; these are standard OAuth (RFC 6749 and RFC 7636) and Relativity documents none of them.

**PKCE.** PKCE is required by Relativity's own documentation to obtain a refresh token and not otherwise: a Code request that does not ask for a refresh token works without it, **verified by NSerio against a live instance; not documented by Relativity**. NSerio recommends `S256` (NSerio recommendation) and does not claim Relativity accepts any particular method. The verifier is 43 to 128 characters of the unreserved set and the `S256` challenge is the base64url of the SHA-256 of the verifier; `secrets.token_urlsafe(32)` gives 43 characters from that set (that the two match is arithmetic, not a tested fact) and `hashlib.sha256` and `base64.urlsafe_b64encode` compute the challenge. **`state`** is generated, stored against the session and compared on return; `nonce` is not used, and Relativity does not document whether it enforces either.

**The client secret is required.** Code does not work without a client secret: tested and rejected, **verified by NSerio against a live instance; not documented by Relativity**, which confirms Relativity's own statement. **A secret kept in a program that is distributed is extractable**, so the exchange runs where a secret can be kept.

**Exchange the code.** A form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=authorization_code`, the code, the same `redirect_uri`, the client ID and the secret, plus the `code_verifier` when PKCE is used (**verified by NSerio against a live instance; not documented by Relativity**).

**Redirect URL.** A server-side application registers **one exact HTTPS URL per environment, no wildcards** (the matching rules are not documented and no claim is made about them). A plain-HTTP loopback Redirect URL `http://127.0.0.1:<port>/<path>` on a **fixed registered port** is accepted, **verified by NSerio against a live instance; not documented by Relativity**, and is the command-line shape. RFC 8252 requires an authorization server to allow any port for a loopback redirect; **Relativity's behaviour is the fixed registered port, and the RFC's rule is not claimed for it.**

**On expiry.** With Allow Refresh Tokens enabled and PKCE used, the code holds a refresh token and sends `grant_type=refresh_token` in place of the code exchange: *inferred* from the standard OAuth shape, not tested; Relativity does not document a refresh token's lifetime, rotation, revocation or revocation endpoint, nor what disabling the client does to tokens already issued. **A refresh token is a secret.** **Single sign-on:** sign-in happens on RelativityOne's own page, so the instance's single sign-on and two-factor rules apply unchanged (*inferred*). **The code stops collecting the password:** after this change nothing receives, stores or forwards the person's user name and password, and the code holds only tokens. Every call now runs as the person who signed in, so **the calling identity changes**. **Scope:** no scope set is prescribed for Code; `openid RelativityRequestOrigin UserInfo UserInfoAccess AuthenticationID RelativityWeb` works, **verified by NSerio against a live instance; not documented by Relativity**, as working and not as minimal.

**Variant 1 — a server-side web application (Flask, Django, FastAPI or similar)**

**Precondition: this variant is for code that runs in Python as a web server and acts for people who sign in to it. It is not for a command-line tool (variant 2).**

Standard server-side Code flow. A login route builds the authorize address with `state` (and the PKCE verifier where a refresh token is wanted), stores them against the visitor's session and redirects; RelativityOne redirects to the **callback route**, registered as an exact HTTPS URL; the callback checks `state` and exchanges the code with the secret. **The logic is framework-neutral; the framework supplies the routes and the session**, and the sketch shows the callback as a handler of the standard library's `http.server` so that it has no dependency: in a Flask, Django or FastAPI application the same two functions are called from the login and callback views, and the dictionary of pending logins is the framework's session (Flask's session holds data in a signed cookie that the user can read but not modify, so **NSerio recommends that it hold neither the verifier nor a token**; Django's session framework "stores data on the server side" with the cookie holding only a session ID, "unless you're using the cookie based backend" (`SESSION_ENGINE` set to `django.contrib.sessions.backends.signed_cookies`), whose data "can be read by the client"; the pages of FastAPI read name no session store). **No framework code is shown: none is installed and none is verified.**

```python
import base64
import hashlib
import http.server
import secrets
from urllib.parse import parse_qs, urlencode, urlparse

import requests

IDENTITY_BASE = "https://HOST/Relativity/Identity/connect"
REDIRECT_URI = "https://app.example.test/auth/callback"
CALLBACK_PATH = "/auth/callback"
RANDOM_BYTES = 32
REQUEST_TIMEOUT_SECONDS = 30
HTTP_OK = 200
HTTP_NOT_FOUND = 404
_pending_by_session = {}


def base64_url(raw_bytes):
	return base64.urlsafe_b64encode(raw_bytes).rstrip(b"=").decode("ascii")


def begin_login(session_id, client_id, want_refresh_token):
	state = secrets.token_urlsafe(RANDOM_BYTES)
	pending = {"state": state, "verifier": None}
	query = {
		"response_type": "code",
		"client_id": client_id,
		"redirect_uri": REDIRECT_URI,
		"state": state,
	}
	if want_refresh_token:
		verifier = secrets.token_urlsafe(RANDOM_BYTES)
		pending["verifier"] = verifier
		query["code_challenge"] = base64_url(hashlib.sha256(verifier.encode("ascii")).digest())
		query["code_challenge_method"] = "S256"
	_pending_by_session[session_id] = pending
	return f"{IDENTITY_BASE}/authorize?{urlencode(query)}"


def complete_login(session_id, client_id, client_secret, returned_code, returned_state):
	pending = _pending_by_session.pop(session_id, None)
	tokens = None
	state_matches = (
		pending is not None
		and returned_state is not None
		and secrets.compare_digest(returned_state.encode("utf-8"), pending["state"].encode("utf-8"))
	)
	if state_matches:
		fields = {
			"grant_type": "authorization_code",
			"code": returned_code,
			"redirect_uri": REDIRECT_URI,
			"client_id": client_id,
			"client_secret": client_secret,
		}
		if pending["verifier"] is not None:
			fields["code_verifier"] = pending["verifier"]
		response = requests.post(f"{IDENTITY_BASE}/token", data=fields, timeout=REQUEST_TIMEOUT_SECONDS)
		tokens = response.json() if response.status_code == HTTP_OK else None
	return tokens


class CallbackHandler(http.server.BaseHTTPRequestHandler):
	def do_GET(self):
		address = urlparse(self.path)
		is_callback = address.path == CALLBACK_PATH
		self.send_response(HTTP_OK if is_callback else HTTP_NOT_FOUND)
		self.end_headers()
		if is_callback:
			query = parse_qs(address.query)
			self.wfile.write(b"Signed in.")
			self.server.returned_code = query.get("code", [None])[0]
			self.server.returned_state = query.get("state", [None])[0]
```

`state` is consumed once and compared on return in constant time (`secrets.compare_digest`); a request with no pending entry or a different `state` gets no exchange. **The scope is not prescribed** (the sketch sends none). **Where the secret travels in the token request** (the body, as sketched, or HTTP Basic client authentication) is **not verified**; RFC 6749 allows either form for a client password. The sketch does not show the HTTP server's TLS, the session cookie or a session store that survives more than one process (the module dictionary does not). **The handler only records the returned `code` and `state`; in a real application the callback view calls `complete_login` with the visitor's own session identifier**, so that the pending entry of that visitor, and no other, is consumed. **Tokens stay on the server**, in a session store that is not exposed to a request.

**The `http.server` handler is a shape, not a deployment.** The `http.server` page states that the module "is not recommended for production. It only implements basic security checks" (Python 3.15.2). The handler above shows what the callback reads and answers, so that the sketch has no dependency; **an application that serves people runs the callback as a view of the framework it already uses** (Flask, Django or FastAPI), behind the TLS and the session store that framework and its server provide.

**Variant 2 — a command-line or desktop tool**

**Precondition: this variant is for code that runs on a person's own machine, as a command-line tool or a desktop shell, and signs that person in. It is not for a web server (variant 1).**

The process starts a listener from the standard library, bound to **`127.0.0.1` only**, on the **fixed registered port**, for **one callback request**; opens the system browser on the authorize address **and always prints the address too**, so a machine with no browser, an SSH session or a failed launch has a manual route; reads `code` and `state` from the callback request, closes the listener, checks `state`, and exchanges the code as variant 1's `complete_login` does (with the redirect URI `http://127.0.0.1:8123/callback`).

```python
import http.server
import time
import webbrowser
from urllib.parse import parse_qs, urlparse

LOOPBACK_HOST = "127.0.0.1"
REGISTERED_PORT = 8123
CALLBACK_PATH = "/callback"
WAIT_SECONDS = 120
POLL_SECONDS = 1.0
HTTP_OK = 200
HTTP_NOT_FOUND = 404


class _RedirectHandler(http.server.BaseHTTPRequestHandler):
	received = None

	def do_GET(self):
		address = urlparse(self.path)
		is_callback = address.path == CALLBACK_PATH
		self.send_response(HTTP_OK if is_callback else HTTP_NOT_FOUND)
		self.end_headers()
		self.wfile.write(b"You can close this window." if is_callback else b"Not found")
		if is_callback:
			query = parse_qs(address.query)
			type(self).received = {
				"code": query.get("code", [None])[0],
				"state": query.get("state", [None])[0],
			}

	def log_message(self, message_format, *arguments):
		return None


class _LoopbackServer(http.server.HTTPServer):
	allow_reuse_address = False


def wait_for_redirect(authorize_address):
	_RedirectHandler.received = None
	print(f"Sign in at: {authorize_address}")
	try:
		server = _LoopbackServer((LOOPBACK_HOST, REGISTERED_PORT), _RedirectHandler)
	except OSError:
		print(f"Port {REGISTERED_PORT} is in use. Close the program that holds it and sign in again.")
	else:
		server.timeout = POLL_SECONDS
		webbrowser.open(authorize_address)
		deadline = time.monotonic() + WAIT_SECONDS
		while _RedirectHandler.received is None and time.monotonic() < deadline:
			server.handle_request()
		server.server_close()
	return _RedirectHandler.received
```

**Rules for the listener:** bind to loopback only; **check `state`** (another local process can connect to a fixed, advertised port); close the listener after the callback request; and handle a busy port. **A busy port raises `OSError` only if the listener does not reuse the address**: `socketserver` documents the class variable `allow_reuse_address` ("This defaults to False, and can be set in subclasses to change the policy"), and `http.server.HTTPServer` sets it to a true value, with which a second bind to the same address and port raised no error on Windows (checked on Windows; the behaviour on Linux and macOS is unchecked). The sketch therefore binds a subclass with `allow_reuse_address = False`, catches the `OSError`, and prints the address and a clear message instead of waiting. `handle_request` processes one request and returns after `timeout` seconds if none arrives, which is why the sketch loops until the callback or the **overall deadline** `WAIT_SECONDS` (`timeout` bounds each wait only, and the deadline bounds the whole wait): a stray request, such as a browser asking for an icon, is answered `404` and does not end the wait. `webbrowser.open` shows the address "using the default browser" and returns whether one was launched. **Where tokens are kept:** a file with restricted permissions or the operating system's keychain (unchecked on every system; no keychain package is named here). **The extractable-secret statement:** a command-line tool or desktop program shipped to people carries its client secret in a form anyone who holds the distribution can extract, and it protects nothing against the person running it (a documentation gap: Relativity documents no recommendation for a distributed client). **Compensating guidance** (NSerio recommendation): Trusted IPs where the users' outbound addresses are known and stable (checked at token issuance; applicability to Code *inferred*); the 60-minute Access Token Lifetime; refresh tokens only when needed; and **one OAuth2 Client per distribution**, so a leaked secret can be disabled and reset alone.

Verification: **Verification note.** **This match is Medium: the call encodes a `user:password` pair and the rule saw `Basic` or `Authorization` nearby, not that it is sent to RelativityOne.** **A split construction** (the encoder in one statement, the header in another) is a second match that nothing merges. **A token has a finite lifetime.** **The calling identity changes.**

OAuth2 Client: See [OAuth2 Client for Authorization Code](#oauth2-client-for-authorization-code).
<!-- generated:end PY-007 authorization-code -->

### PY-008 · AiohttpEncodeBasicAuth

<a id="py-008-migration"></a>

Remove the `encode_basic_auth` call and send a Bearer token in the `Authorization` header instead. The same call builds a `Proxy-Authorization` header too: if this one does, the credential is for a proxy and the migration is the proxy's own.

#### Client Credentials

<!-- generated:begin PY-008 client-credentials -->
**Flow.** This entry is for Client Credentials. The "Which flow" paragraph of PY-008 in [Part A](part-a-python.md#py-008) says when a match belongs to this flow.

Source form: **Applies to:** a Python script or service using `aiohttp` 3.14 or later.

```python
headers = {"Authorization": aiohttp.encode_basic_auth(user_name, secret)}
```

Under token authentication: **First, the destination:** if this Relativity instance is not RelativityOne — including a Relativity Server instance, whose own documentation governs — no migration to RelativityOne applies and the rest of this entry does not apply.

```python
CSRF_HEADER_VALUE = "-"
headers = {"Authorization": f"Bearer {access_token}", "X-CSRF-Header": CSRF_HEADER_VALUE}
```

Remove the `encode_basic_auth` call.

Credentials: **Where the credentials come from.** The source form establishes that a client sends Basic, not that the destination is RelativityOne; if it is not RelativityOne the migration is that service's own token mechanism, no migration to RelativityOne applies, and the reader stops here. If it is RelativityOne, the token comes from the token-acquisition part that follows, and the user name and password are replaced by the OAuth2 Client's client ID and secret, kept as Secret Management (Part B of the Migration Documentation) describes, never in source.

The text a match assigned Client Credentials receives after part 1. Create an OAuth2 Client with Flow Grant Type set to Client Credentials (part 3 lists its settings), then send a form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=client_credentials`, `client_id` and `client_secret`. The token is sent as `Authorization: Bearer <token>`, with `X-CSRF-Header: -` on REST and Kepler calls. **The endpoint is documented for this grant.** The scope depends on what the integration needs and **no single scope is required**: Relativity's documentation shows `SystemUserInfo` and `UserInfoAccess` and no page says which is correct or what either grants; use the value the API you call documents, and confirm it.

**Variant: a Python process acting as itself**

**Precondition: this variant is for a Python job, service or script that does not act for a signed-in person. If a person signs in, use Authorization Code.**

```python
import os
import time

import requests

IDENTITY_TOKEN_URL = "https://HOST/Relativity/Identity/connect/token"
EXPIRY_MARGIN_SECONDS = 60
REQUEST_TIMEOUT_SECONDS = 30

_cached_token = {"value": None, "expires_at": 0.0}


def get_access_token():
	now = time.monotonic()
	if _cached_token["value"] is None or _cached_token["expires_at"] - now < EXPIRY_MARGIN_SECONDS:
		response = requests.post(
			IDENTITY_TOKEN_URL,
			data={
				"grant_type": "client_credentials",
				"client_id": os.environ["RELATIVITY_CLIENT_ID"],
				"client_secret": os.environ["RELATIVITY_CLIENT_SECRET"],
			},
			timeout=REQUEST_TIMEOUT_SECONDS,
		)
		response.raise_for_status()
		payload = response.json()
		_cached_token["value"] = payload["access_token"]
		_cached_token["expires_at"] = now + payload["expires_in"]
	return _cached_token["value"]
```

`requests` form-encodes a dictionary passed as `data`, and `raise_for_status` raises on a 4XX or 5XX response. **The same token request in the other three authorized clients** (the cache and the credentials are as above):

```python
HTTP_OK = 200
token_request_fields = {
	"grant_type": "client_credentials",
	"client_id": os.environ["RELATIVITY_CLIENT_ID"],
	"client_secret": os.environ["RELATIVITY_CLIENT_SECRET"],
}

# httpx: a dictionary passed as data is form-encoded
response = httpx.post(IDENTITY_TOKEN_URL, data=token_request_fields, timeout=REQUEST_TIMEOUT_SECONDS)
response.raise_for_status()
payload = response.json()

# aiohttp: data= is form-encoded
async with session.post(IDENTITY_TOKEN_URL, data=token_request_fields) as response:
	response.raise_for_status()
	payload = await response.json()

# urllib3: PoolManager.request form-encodes the fields argument for POST only with encode_multipart=False
response = urllib3.PoolManager().request("POST", IDENTITY_TOKEN_URL, fields=token_request_fields, encode_multipart=False)
if response.status != HTTP_OK:
	raise RuntimeError(f"The token request failed with status {response.status}")
payload = response.json()
```

**Cache the token and re-acquire it on expiry — do not refresh.** Request a new token with the client secret shortly before the current one expires (the block uses a margin of 60 seconds), and once when a call is rejected for authentication. Client Credentials does not offer refresh tokens: **verified by NSerio against a live instance; not documented by Relativity** — the Allow Refresh Tokens control does not appear for that grant in the RelativityOne user interface. The recommended Access Token Lifetime is 60 minutes. **The client secret** belongs in the process environment or a secret store, as Secret Management (Part B of the Migration Documentation) describes, **never in source or in a tracked file**. A reset invalidates the previous secret immediately, so plan the cut-over (the two-client pattern is inferred). **Before deploying, confirm the calling identity:** every call now runs as the OAuth2 Client's Context User, not as the user in the replaced credential, and the non-administrator Context User is NSerio's recommendation. Trusted IPs restrict where a token can be requested, not a token already issued. **Whether `httpx` and `urllib3` accept a dictionary the way the pages read say is documented for the page's version only** (`httpx` shows no version): unchecked for other versions.

Verification: **Verification note.** **`encode_basic_auth` also builds a `Proxy-Authorization` value**: **if the value is a proxy header, it is a proxy credential and not a Relativity credential, and no migration to RelativityOne applies** (the class cannot see the header name). The function was **added in aiohttp 3.14**. **A token has a finite lifetime.** **The calling identity changes.**

OAuth2 Client: See [OAuth2 Client for Client Credentials](#oauth2-client-for-client-credentials).
<!-- generated:end PY-008 client-credentials -->

#### Authorization Code

<!-- generated:begin PY-008 authorization-code -->
**Flow.** This entry is for Authorization Code. The "Which flow" paragraph of PY-008 in [Part A](part-a-python.md#py-008) says when a match belongs to this flow.

Source form: **Applies to:** a Python script or service using `aiohttp` 3.14 or later.

```python
headers = {"Authorization": aiohttp.encode_basic_auth(user_name, secret)}
```

Under token authentication: **First, the destination:** if this Relativity instance is not RelativityOne — including a Relativity Server instance, whose own documentation governs — no migration to RelativityOne applies and the rest of this entry does not apply.

```python
CSRF_HEADER_VALUE = "-"
headers = {"Authorization": f"Bearer {access_token}", "X-CSRF-Header": CSRF_HEADER_VALUE}
```

Remove the `encode_basic_auth` call.

Credentials: **Where the credentials come from.** The source form establishes that a client sends Basic, not that the destination is RelativityOne; if it is not RelativityOne the migration is that service's own token mechanism, no migration to RelativityOne applies, and the reader stops here. If it is RelativityOne, the token comes from the token-acquisition part that follows, and the user name and password are replaced by the OAuth2 Client's client ID and secret, kept as Secret Management (Part B of the Migration Documentation) describes, never in source.

**Common to both variants.** The person signs in on RelativityOne's own page and the code never holds their password. The text has a common part and **two variants by application type**, each opening with the precondition it is for: a server-side web application and a command-line or desktop tool. Read the precondition of each and discard the variant that does not apply.

**Sign in.** Replace the code that collected the person's credentials with a redirect to the authorize endpoint `<host>/Relativity/Identity/connect/authorize` (**verified by NSerio against a live instance; not documented by Relativity**), which leads to RelativityOne's login page. The request carries the standard authorization-code parameters — `client_id`, `response_type=code`, the registered `redirect_uri`, `state` checked on return, and `code_challenge` with `code_challenge_method=S256` when PKCE is used; these are standard OAuth (RFC 6749 and RFC 7636) and Relativity documents none of them.

**PKCE.** PKCE is required by Relativity's own documentation to obtain a refresh token and not otherwise: a Code request that does not ask for a refresh token works without it, **verified by NSerio against a live instance; not documented by Relativity**. NSerio recommends `S256` (NSerio recommendation) and does not claim Relativity accepts any particular method. The verifier is 43 to 128 characters of the unreserved set and the `S256` challenge is the base64url of the SHA-256 of the verifier; `secrets.token_urlsafe(32)` gives 43 characters from that set (that the two match is arithmetic, not a tested fact) and `hashlib.sha256` and `base64.urlsafe_b64encode` compute the challenge. **`state`** is generated, stored against the session and compared on return; `nonce` is not used, and Relativity does not document whether it enforces either.

**The client secret is required.** Code does not work without a client secret: tested and rejected, **verified by NSerio against a live instance; not documented by Relativity**, which confirms Relativity's own statement. **A secret kept in a program that is distributed is extractable**, so the exchange runs where a secret can be kept.

**Exchange the code.** A form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=authorization_code`, the code, the same `redirect_uri`, the client ID and the secret, plus the `code_verifier` when PKCE is used (**verified by NSerio against a live instance; not documented by Relativity**).

**Redirect URL.** A server-side application registers **one exact HTTPS URL per environment, no wildcards** (the matching rules are not documented and no claim is made about them). A plain-HTTP loopback Redirect URL `http://127.0.0.1:<port>/<path>` on a **fixed registered port** is accepted, **verified by NSerio against a live instance; not documented by Relativity**, and is the command-line shape. RFC 8252 requires an authorization server to allow any port for a loopback redirect; **Relativity's behaviour is the fixed registered port, and the RFC's rule is not claimed for it.**

**On expiry.** With Allow Refresh Tokens enabled and PKCE used, the code holds a refresh token and sends `grant_type=refresh_token` in place of the code exchange: *inferred* from the standard OAuth shape, not tested; Relativity does not document a refresh token's lifetime, rotation, revocation or revocation endpoint, nor what disabling the client does to tokens already issued. **A refresh token is a secret.** **Single sign-on:** sign-in happens on RelativityOne's own page, so the instance's single sign-on and two-factor rules apply unchanged (*inferred*). **The code stops collecting the password:** after this change nothing receives, stores or forwards the person's user name and password, and the code holds only tokens. Every call now runs as the person who signed in, so **the calling identity changes**. **Scope:** no scope set is prescribed for Code; `openid RelativityRequestOrigin UserInfo UserInfoAccess AuthenticationID RelativityWeb` works, **verified by NSerio against a live instance; not documented by Relativity**, as working and not as minimal.

**Variant 1 — a server-side web application (Flask, Django, FastAPI or similar)**

**Precondition: this variant is for code that runs in Python as a web server and acts for people who sign in to it. It is not for a command-line tool (variant 2).**

Standard server-side Code flow. A login route builds the authorize address with `state` (and the PKCE verifier where a refresh token is wanted), stores them against the visitor's session and redirects; RelativityOne redirects to the **callback route**, registered as an exact HTTPS URL; the callback checks `state` and exchanges the code with the secret. **The logic is framework-neutral; the framework supplies the routes and the session**, and the sketch shows the callback as a handler of the standard library's `http.server` so that it has no dependency: in a Flask, Django or FastAPI application the same two functions are called from the login and callback views, and the dictionary of pending logins is the framework's session (Flask's session holds data in a signed cookie that the user can read but not modify, so **NSerio recommends that it hold neither the verifier nor a token**; Django's session framework "stores data on the server side" with the cookie holding only a session ID, "unless you're using the cookie based backend" (`SESSION_ENGINE` set to `django.contrib.sessions.backends.signed_cookies`), whose data "can be read by the client"; the pages of FastAPI read name no session store). **No framework code is shown: none is installed and none is verified.**

```python
import base64
import hashlib
import http.server
import secrets
from urllib.parse import parse_qs, urlencode, urlparse

import requests

IDENTITY_BASE = "https://HOST/Relativity/Identity/connect"
REDIRECT_URI = "https://app.example.test/auth/callback"
CALLBACK_PATH = "/auth/callback"
RANDOM_BYTES = 32
REQUEST_TIMEOUT_SECONDS = 30
HTTP_OK = 200
HTTP_NOT_FOUND = 404
_pending_by_session = {}


def base64_url(raw_bytes):
	return base64.urlsafe_b64encode(raw_bytes).rstrip(b"=").decode("ascii")


def begin_login(session_id, client_id, want_refresh_token):
	state = secrets.token_urlsafe(RANDOM_BYTES)
	pending = {"state": state, "verifier": None}
	query = {
		"response_type": "code",
		"client_id": client_id,
		"redirect_uri": REDIRECT_URI,
		"state": state,
	}
	if want_refresh_token:
		verifier = secrets.token_urlsafe(RANDOM_BYTES)
		pending["verifier"] = verifier
		query["code_challenge"] = base64_url(hashlib.sha256(verifier.encode("ascii")).digest())
		query["code_challenge_method"] = "S256"
	_pending_by_session[session_id] = pending
	return f"{IDENTITY_BASE}/authorize?{urlencode(query)}"


def complete_login(session_id, client_id, client_secret, returned_code, returned_state):
	pending = _pending_by_session.pop(session_id, None)
	tokens = None
	state_matches = (
		pending is not None
		and returned_state is not None
		and secrets.compare_digest(returned_state.encode("utf-8"), pending["state"].encode("utf-8"))
	)
	if state_matches:
		fields = {
			"grant_type": "authorization_code",
			"code": returned_code,
			"redirect_uri": REDIRECT_URI,
			"client_id": client_id,
			"client_secret": client_secret,
		}
		if pending["verifier"] is not None:
			fields["code_verifier"] = pending["verifier"]
		response = requests.post(f"{IDENTITY_BASE}/token", data=fields, timeout=REQUEST_TIMEOUT_SECONDS)
		tokens = response.json() if response.status_code == HTTP_OK else None
	return tokens


class CallbackHandler(http.server.BaseHTTPRequestHandler):
	def do_GET(self):
		address = urlparse(self.path)
		is_callback = address.path == CALLBACK_PATH
		self.send_response(HTTP_OK if is_callback else HTTP_NOT_FOUND)
		self.end_headers()
		if is_callback:
			query = parse_qs(address.query)
			self.wfile.write(b"Signed in.")
			self.server.returned_code = query.get("code", [None])[0]
			self.server.returned_state = query.get("state", [None])[0]
```

`state` is consumed once and compared on return in constant time (`secrets.compare_digest`); a request with no pending entry or a different `state` gets no exchange. **The scope is not prescribed** (the sketch sends none). **Where the secret travels in the token request** (the body, as sketched, or HTTP Basic client authentication) is **not verified**; RFC 6749 allows either form for a client password. The sketch does not show the HTTP server's TLS, the session cookie or a session store that survives more than one process (the module dictionary does not). **The handler only records the returned `code` and `state`; in a real application the callback view calls `complete_login` with the visitor's own session identifier**, so that the pending entry of that visitor, and no other, is consumed. **Tokens stay on the server**, in a session store that is not exposed to a request.

**The `http.server` handler is a shape, not a deployment.** The `http.server` page states that the module "is not recommended for production. It only implements basic security checks" (Python 3.15.2). The handler above shows what the callback reads and answers, so that the sketch has no dependency; **an application that serves people runs the callback as a view of the framework it already uses** (Flask, Django or FastAPI), behind the TLS and the session store that framework and its server provide.

**Variant 2 — a command-line or desktop tool**

**Precondition: this variant is for code that runs on a person's own machine, as a command-line tool or a desktop shell, and signs that person in. It is not for a web server (variant 1).**

The process starts a listener from the standard library, bound to **`127.0.0.1` only**, on the **fixed registered port**, for **one callback request**; opens the system browser on the authorize address **and always prints the address too**, so a machine with no browser, an SSH session or a failed launch has a manual route; reads `code` and `state` from the callback request, closes the listener, checks `state`, and exchanges the code as variant 1's `complete_login` does (with the redirect URI `http://127.0.0.1:8123/callback`).

```python
import http.server
import time
import webbrowser
from urllib.parse import parse_qs, urlparse

LOOPBACK_HOST = "127.0.0.1"
REGISTERED_PORT = 8123
CALLBACK_PATH = "/callback"
WAIT_SECONDS = 120
POLL_SECONDS = 1.0
HTTP_OK = 200
HTTP_NOT_FOUND = 404


class _RedirectHandler(http.server.BaseHTTPRequestHandler):
	received = None

	def do_GET(self):
		address = urlparse(self.path)
		is_callback = address.path == CALLBACK_PATH
		self.send_response(HTTP_OK if is_callback else HTTP_NOT_FOUND)
		self.end_headers()
		self.wfile.write(b"You can close this window." if is_callback else b"Not found")
		if is_callback:
			query = parse_qs(address.query)
			type(self).received = {
				"code": query.get("code", [None])[0],
				"state": query.get("state", [None])[0],
			}

	def log_message(self, message_format, *arguments):
		return None


class _LoopbackServer(http.server.HTTPServer):
	allow_reuse_address = False


def wait_for_redirect(authorize_address):
	_RedirectHandler.received = None
	print(f"Sign in at: {authorize_address}")
	try:
		server = _LoopbackServer((LOOPBACK_HOST, REGISTERED_PORT), _RedirectHandler)
	except OSError:
		print(f"Port {REGISTERED_PORT} is in use. Close the program that holds it and sign in again.")
	else:
		server.timeout = POLL_SECONDS
		webbrowser.open(authorize_address)
		deadline = time.monotonic() + WAIT_SECONDS
		while _RedirectHandler.received is None and time.monotonic() < deadline:
			server.handle_request()
		server.server_close()
	return _RedirectHandler.received
```

**Rules for the listener:** bind to loopback only; **check `state`** (another local process can connect to a fixed, advertised port); close the listener after the callback request; and handle a busy port. **A busy port raises `OSError` only if the listener does not reuse the address**: `socketserver` documents the class variable `allow_reuse_address` ("This defaults to False, and can be set in subclasses to change the policy"), and `http.server.HTTPServer` sets it to a true value, with which a second bind to the same address and port raised no error on Windows (checked on Windows; the behaviour on Linux and macOS is unchecked). The sketch therefore binds a subclass with `allow_reuse_address = False`, catches the `OSError`, and prints the address and a clear message instead of waiting. `handle_request` processes one request and returns after `timeout` seconds if none arrives, which is why the sketch loops until the callback or the **overall deadline** `WAIT_SECONDS` (`timeout` bounds each wait only, and the deadline bounds the whole wait): a stray request, such as a browser asking for an icon, is answered `404` and does not end the wait. `webbrowser.open` shows the address "using the default browser" and returns whether one was launched. **Where tokens are kept:** a file with restricted permissions or the operating system's keychain (unchecked on every system; no keychain package is named here). **The extractable-secret statement:** a command-line tool or desktop program shipped to people carries its client secret in a form anyone who holds the distribution can extract, and it protects nothing against the person running it (a documentation gap: Relativity documents no recommendation for a distributed client). **Compensating guidance** (NSerio recommendation): Trusted IPs where the users' outbound addresses are known and stable (checked at token issuance; applicability to Code *inferred*); the 60-minute Access Token Lifetime; refresh tokens only when needed; and **one OAuth2 Client per distribution**, so a leaked secret can be disabled and reset alone.

Verification: **Verification note.** **`encode_basic_auth` also builds a `Proxy-Authorization` value**: **if the value is a proxy header, it is a proxy credential and not a Relativity credential, and no migration to RelativityOne applies** (the class cannot see the header name). The function was **added in aiohttp 3.14**. **A token has a finite lifetime.** **The calling identity changes.**

OAuth2 Client: See [OAuth2 Client for Authorization Code](#oauth2-client-for-authorization-code).
<!-- generated:end PY-008 authorization-code -->

### PY-009 · PasswordManagerAddPassword

<a id="py-009-migration"></a>

Remove the password manager, its `add_password` calls and the handler that receives it (`PY-004`), and give `urllib.request.Request` a Bearer header instead. A manager that is given to a Digest handler or to a proxy handler is not Basic Authentication to RelativityOne: check the handler first.

#### Client Credentials

<!-- generated:begin PY-009 client-credentials -->
**Flow.** This entry is for Client Credentials. The "Which flow" paragraph of PY-009 in [Part A](part-a-python.md#py-009) says when a match belongs to this flow.

Source form: **Applies to:** a Python script or service using the standard library's `urllib.request`.

```python
manager = urllib.request.HTTPPasswordMgrWithDefaultRealm()
manager.add_password(None, "https://example.test/", user_name, secret)
```

Under token authentication: **First, the destination:** if this Relativity instance is not RelativityOne — including a Relativity Server instance, whose own documentation governs — no migration to RelativityOne applies and the rest of this entry does not apply.

```python
CSRF_HEADER_VALUE = "-"
request = urllib.request.Request(url, headers={"Authorization": f"Bearer {access_token}", "X-CSRF-Header": CSRF_HEADER_VALUE})
response = urllib.request.urlopen(request)
```

Remove the password manager, its `add_password` calls and the handler that receives it (`PY-004`).

Credentials: **Where the credentials come from.** The source form establishes that a client sends Basic, not that the destination is RelativityOne; if it is not RelativityOne the migration is that service's own token mechanism, no migration to RelativityOne applies, and the reader stops here. If it is RelativityOne, the token comes from the token-acquisition part that follows, and the user name and password are replaced by the OAuth2 Client's client ID and secret, kept as Secret Management (Part B of the Migration Documentation) describes, never in source.

The text a match assigned Client Credentials receives after part 1. Create an OAuth2 Client with Flow Grant Type set to Client Credentials (part 3 lists its settings), then send a form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=client_credentials`, `client_id` and `client_secret`. The token is sent as `Authorization: Bearer <token>`, with `X-CSRF-Header: -` on REST and Kepler calls. **The endpoint is documented for this grant.** The scope depends on what the integration needs and **no single scope is required**: Relativity's documentation shows `SystemUserInfo` and `UserInfoAccess` and no page says which is correct or what either grants; use the value the API you call documents, and confirm it.

**Variant: a Python process acting as itself**

**Precondition: this variant is for a Python job, service or script that does not act for a signed-in person. If a person signs in, use Authorization Code.**

```python
import os
import time

import requests

IDENTITY_TOKEN_URL = "https://HOST/Relativity/Identity/connect/token"
EXPIRY_MARGIN_SECONDS = 60
REQUEST_TIMEOUT_SECONDS = 30

_cached_token = {"value": None, "expires_at": 0.0}


def get_access_token():
	now = time.monotonic()
	if _cached_token["value"] is None or _cached_token["expires_at"] - now < EXPIRY_MARGIN_SECONDS:
		response = requests.post(
			IDENTITY_TOKEN_URL,
			data={
				"grant_type": "client_credentials",
				"client_id": os.environ["RELATIVITY_CLIENT_ID"],
				"client_secret": os.environ["RELATIVITY_CLIENT_SECRET"],
			},
			timeout=REQUEST_TIMEOUT_SECONDS,
		)
		response.raise_for_status()
		payload = response.json()
		_cached_token["value"] = payload["access_token"]
		_cached_token["expires_at"] = now + payload["expires_in"]
	return _cached_token["value"]
```

`requests` form-encodes a dictionary passed as `data`, and `raise_for_status` raises on a 4XX or 5XX response. **The same token request in the other three authorized clients** (the cache and the credentials are as above):

```python
HTTP_OK = 200
token_request_fields = {
	"grant_type": "client_credentials",
	"client_id": os.environ["RELATIVITY_CLIENT_ID"],
	"client_secret": os.environ["RELATIVITY_CLIENT_SECRET"],
}

# httpx: a dictionary passed as data is form-encoded
response = httpx.post(IDENTITY_TOKEN_URL, data=token_request_fields, timeout=REQUEST_TIMEOUT_SECONDS)
response.raise_for_status()
payload = response.json()

# aiohttp: data= is form-encoded
async with session.post(IDENTITY_TOKEN_URL, data=token_request_fields) as response:
	response.raise_for_status()
	payload = await response.json()

# urllib3: PoolManager.request form-encodes the fields argument for POST only with encode_multipart=False
response = urllib3.PoolManager().request("POST", IDENTITY_TOKEN_URL, fields=token_request_fields, encode_multipart=False)
if response.status != HTTP_OK:
	raise RuntimeError(f"The token request failed with status {response.status}")
payload = response.json()
```

**Cache the token and re-acquire it on expiry — do not refresh.** Request a new token with the client secret shortly before the current one expires (the block uses a margin of 60 seconds), and once when a call is rejected for authentication. Client Credentials does not offer refresh tokens: **verified by NSerio against a live instance; not documented by Relativity** — the Allow Refresh Tokens control does not appear for that grant in the RelativityOne user interface. The recommended Access Token Lifetime is 60 minutes. **The client secret** belongs in the process environment or a secret store, as Secret Management (Part B of the Migration Documentation) describes, **never in source or in a tracked file**. A reset invalidates the previous secret immediately, so plan the cut-over (the two-client pattern is inferred). **Before deploying, confirm the calling identity:** every call now runs as the OAuth2 Client's Context User, not as the user in the replaced credential, and the non-administrator Context User is NSerio's recommendation. Trusted IPs restrict where a token can be requested, not a token already issued. **Whether `httpx` and `urllib3` accept a dictionary the way the pages read say is documented for the page's version only** (`httpx` shows no version): unchecked for other versions.

Verification: **Verification note.** **A password manager is scheme-agnostic**: **the rule cannot tell whether the handler that receives it is Basic, Digest or a proxy handler**; if it is not a Basic handler for RelativityOne no migration to RelativityOne applies. **A token has a finite lifetime.** **The calling identity changes.** The scheme scoping of `add_password` changed in Python 3.15: behaviour before it is unchecked.

OAuth2 Client: See [OAuth2 Client for Client Credentials](#oauth2-client-for-client-credentials).
<!-- generated:end PY-009 client-credentials -->

#### Authorization Code

<!-- generated:begin PY-009 authorization-code -->
**Flow.** This entry is for Authorization Code. The "Which flow" paragraph of PY-009 in [Part A](part-a-python.md#py-009) says when a match belongs to this flow.

Source form: **Applies to:** a Python script or service using the standard library's `urllib.request`.

```python
manager = urllib.request.HTTPPasswordMgrWithDefaultRealm()
manager.add_password(None, "https://example.test/", user_name, secret)
```

Under token authentication: **First, the destination:** if this Relativity instance is not RelativityOne — including a Relativity Server instance, whose own documentation governs — no migration to RelativityOne applies and the rest of this entry does not apply.

```python
CSRF_HEADER_VALUE = "-"
request = urllib.request.Request(url, headers={"Authorization": f"Bearer {access_token}", "X-CSRF-Header": CSRF_HEADER_VALUE})
response = urllib.request.urlopen(request)
```

Remove the password manager, its `add_password` calls and the handler that receives it (`PY-004`).

Credentials: **Where the credentials come from.** The source form establishes that a client sends Basic, not that the destination is RelativityOne; if it is not RelativityOne the migration is that service's own token mechanism, no migration to RelativityOne applies, and the reader stops here. If it is RelativityOne, the token comes from the token-acquisition part that follows, and the user name and password are replaced by the OAuth2 Client's client ID and secret, kept as Secret Management (Part B of the Migration Documentation) describes, never in source.

**Common to both variants.** The person signs in on RelativityOne's own page and the code never holds their password. The text has a common part and **two variants by application type**, each opening with the precondition it is for: a server-side web application and a command-line or desktop tool. Read the precondition of each and discard the variant that does not apply.

**Sign in.** Replace the code that collected the person's credentials with a redirect to the authorize endpoint `<host>/Relativity/Identity/connect/authorize` (**verified by NSerio against a live instance; not documented by Relativity**), which leads to RelativityOne's login page. The request carries the standard authorization-code parameters — `client_id`, `response_type=code`, the registered `redirect_uri`, `state` checked on return, and `code_challenge` with `code_challenge_method=S256` when PKCE is used; these are standard OAuth (RFC 6749 and RFC 7636) and Relativity documents none of them.

**PKCE.** PKCE is required by Relativity's own documentation to obtain a refresh token and not otherwise: a Code request that does not ask for a refresh token works without it, **verified by NSerio against a live instance; not documented by Relativity**. NSerio recommends `S256` (NSerio recommendation) and does not claim Relativity accepts any particular method. The verifier is 43 to 128 characters of the unreserved set and the `S256` challenge is the base64url of the SHA-256 of the verifier; `secrets.token_urlsafe(32)` gives 43 characters from that set (that the two match is arithmetic, not a tested fact) and `hashlib.sha256` and `base64.urlsafe_b64encode` compute the challenge. **`state`** is generated, stored against the session and compared on return; `nonce` is not used, and Relativity does not document whether it enforces either.

**The client secret is required.** Code does not work without a client secret: tested and rejected, **verified by NSerio against a live instance; not documented by Relativity**, which confirms Relativity's own statement. **A secret kept in a program that is distributed is extractable**, so the exchange runs where a secret can be kept.

**Exchange the code.** A form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=authorization_code`, the code, the same `redirect_uri`, the client ID and the secret, plus the `code_verifier` when PKCE is used (**verified by NSerio against a live instance; not documented by Relativity**).

**Redirect URL.** A server-side application registers **one exact HTTPS URL per environment, no wildcards** (the matching rules are not documented and no claim is made about them). A plain-HTTP loopback Redirect URL `http://127.0.0.1:<port>/<path>` on a **fixed registered port** is accepted, **verified by NSerio against a live instance; not documented by Relativity**, and is the command-line shape. RFC 8252 requires an authorization server to allow any port for a loopback redirect; **Relativity's behaviour is the fixed registered port, and the RFC's rule is not claimed for it.**

**On expiry.** With Allow Refresh Tokens enabled and PKCE used, the code holds a refresh token and sends `grant_type=refresh_token` in place of the code exchange: *inferred* from the standard OAuth shape, not tested; Relativity does not document a refresh token's lifetime, rotation, revocation or revocation endpoint, nor what disabling the client does to tokens already issued. **A refresh token is a secret.** **Single sign-on:** sign-in happens on RelativityOne's own page, so the instance's single sign-on and two-factor rules apply unchanged (*inferred*). **The code stops collecting the password:** after this change nothing receives, stores or forwards the person's user name and password, and the code holds only tokens. Every call now runs as the person who signed in, so **the calling identity changes**. **Scope:** no scope set is prescribed for Code; `openid RelativityRequestOrigin UserInfo UserInfoAccess AuthenticationID RelativityWeb` works, **verified by NSerio against a live instance; not documented by Relativity**, as working and not as minimal.

**Variant 1 — a server-side web application (Flask, Django, FastAPI or similar)**

**Precondition: this variant is for code that runs in Python as a web server and acts for people who sign in to it. It is not for a command-line tool (variant 2).**

Standard server-side Code flow. A login route builds the authorize address with `state` (and the PKCE verifier where a refresh token is wanted), stores them against the visitor's session and redirects; RelativityOne redirects to the **callback route**, registered as an exact HTTPS URL; the callback checks `state` and exchanges the code with the secret. **The logic is framework-neutral; the framework supplies the routes and the session**, and the sketch shows the callback as a handler of the standard library's `http.server` so that it has no dependency: in a Flask, Django or FastAPI application the same two functions are called from the login and callback views, and the dictionary of pending logins is the framework's session (Flask's session holds data in a signed cookie that the user can read but not modify, so **NSerio recommends that it hold neither the verifier nor a token**; Django's session framework "stores data on the server side" with the cookie holding only a session ID, "unless you're using the cookie based backend" (`SESSION_ENGINE` set to `django.contrib.sessions.backends.signed_cookies`), whose data "can be read by the client"; the pages of FastAPI read name no session store). **No framework code is shown: none is installed and none is verified.**

```python
import base64
import hashlib
import http.server
import secrets
from urllib.parse import parse_qs, urlencode, urlparse

import requests

IDENTITY_BASE = "https://HOST/Relativity/Identity/connect"
REDIRECT_URI = "https://app.example.test/auth/callback"
CALLBACK_PATH = "/auth/callback"
RANDOM_BYTES = 32
REQUEST_TIMEOUT_SECONDS = 30
HTTP_OK = 200
HTTP_NOT_FOUND = 404
_pending_by_session = {}


def base64_url(raw_bytes):
	return base64.urlsafe_b64encode(raw_bytes).rstrip(b"=").decode("ascii")


def begin_login(session_id, client_id, want_refresh_token):
	state = secrets.token_urlsafe(RANDOM_BYTES)
	pending = {"state": state, "verifier": None}
	query = {
		"response_type": "code",
		"client_id": client_id,
		"redirect_uri": REDIRECT_URI,
		"state": state,
	}
	if want_refresh_token:
		verifier = secrets.token_urlsafe(RANDOM_BYTES)
		pending["verifier"] = verifier
		query["code_challenge"] = base64_url(hashlib.sha256(verifier.encode("ascii")).digest())
		query["code_challenge_method"] = "S256"
	_pending_by_session[session_id] = pending
	return f"{IDENTITY_BASE}/authorize?{urlencode(query)}"


def complete_login(session_id, client_id, client_secret, returned_code, returned_state):
	pending = _pending_by_session.pop(session_id, None)
	tokens = None
	state_matches = (
		pending is not None
		and returned_state is not None
		and secrets.compare_digest(returned_state.encode("utf-8"), pending["state"].encode("utf-8"))
	)
	if state_matches:
		fields = {
			"grant_type": "authorization_code",
			"code": returned_code,
			"redirect_uri": REDIRECT_URI,
			"client_id": client_id,
			"client_secret": client_secret,
		}
		if pending["verifier"] is not None:
			fields["code_verifier"] = pending["verifier"]
		response = requests.post(f"{IDENTITY_BASE}/token", data=fields, timeout=REQUEST_TIMEOUT_SECONDS)
		tokens = response.json() if response.status_code == HTTP_OK else None
	return tokens


class CallbackHandler(http.server.BaseHTTPRequestHandler):
	def do_GET(self):
		address = urlparse(self.path)
		is_callback = address.path == CALLBACK_PATH
		self.send_response(HTTP_OK if is_callback else HTTP_NOT_FOUND)
		self.end_headers()
		if is_callback:
			query = parse_qs(address.query)
			self.wfile.write(b"Signed in.")
			self.server.returned_code = query.get("code", [None])[0]
			self.server.returned_state = query.get("state", [None])[0]
```

`state` is consumed once and compared on return in constant time (`secrets.compare_digest`); a request with no pending entry or a different `state` gets no exchange. **The scope is not prescribed** (the sketch sends none). **Where the secret travels in the token request** (the body, as sketched, or HTTP Basic client authentication) is **not verified**; RFC 6749 allows either form for a client password. The sketch does not show the HTTP server's TLS, the session cookie or a session store that survives more than one process (the module dictionary does not). **The handler only records the returned `code` and `state`; in a real application the callback view calls `complete_login` with the visitor's own session identifier**, so that the pending entry of that visitor, and no other, is consumed. **Tokens stay on the server**, in a session store that is not exposed to a request.

**The `http.server` handler is a shape, not a deployment.** The `http.server` page states that the module "is not recommended for production. It only implements basic security checks" (Python 3.15.2). The handler above shows what the callback reads and answers, so that the sketch has no dependency; **an application that serves people runs the callback as a view of the framework it already uses** (Flask, Django or FastAPI), behind the TLS and the session store that framework and its server provide.

**Variant 2 — a command-line or desktop tool**

**Precondition: this variant is for code that runs on a person's own machine, as a command-line tool or a desktop shell, and signs that person in. It is not for a web server (variant 1).**

The process starts a listener from the standard library, bound to **`127.0.0.1` only**, on the **fixed registered port**, for **one callback request**; opens the system browser on the authorize address **and always prints the address too**, so a machine with no browser, an SSH session or a failed launch has a manual route; reads `code` and `state` from the callback request, closes the listener, checks `state`, and exchanges the code as variant 1's `complete_login` does (with the redirect URI `http://127.0.0.1:8123/callback`).

```python
import http.server
import time
import webbrowser
from urllib.parse import parse_qs, urlparse

LOOPBACK_HOST = "127.0.0.1"
REGISTERED_PORT = 8123
CALLBACK_PATH = "/callback"
WAIT_SECONDS = 120
POLL_SECONDS = 1.0
HTTP_OK = 200
HTTP_NOT_FOUND = 404


class _RedirectHandler(http.server.BaseHTTPRequestHandler):
	received = None

	def do_GET(self):
		address = urlparse(self.path)
		is_callback = address.path == CALLBACK_PATH
		self.send_response(HTTP_OK if is_callback else HTTP_NOT_FOUND)
		self.end_headers()
		self.wfile.write(b"You can close this window." if is_callback else b"Not found")
		if is_callback:
			query = parse_qs(address.query)
			type(self).received = {
				"code": query.get("code", [None])[0],
				"state": query.get("state", [None])[0],
			}

	def log_message(self, message_format, *arguments):
		return None


class _LoopbackServer(http.server.HTTPServer):
	allow_reuse_address = False


def wait_for_redirect(authorize_address):
	_RedirectHandler.received = None
	print(f"Sign in at: {authorize_address}")
	try:
		server = _LoopbackServer((LOOPBACK_HOST, REGISTERED_PORT), _RedirectHandler)
	except OSError:
		print(f"Port {REGISTERED_PORT} is in use. Close the program that holds it and sign in again.")
	else:
		server.timeout = POLL_SECONDS
		webbrowser.open(authorize_address)
		deadline = time.monotonic() + WAIT_SECONDS
		while _RedirectHandler.received is None and time.monotonic() < deadline:
			server.handle_request()
		server.server_close()
	return _RedirectHandler.received
```

**Rules for the listener:** bind to loopback only; **check `state`** (another local process can connect to a fixed, advertised port); close the listener after the callback request; and handle a busy port. **A busy port raises `OSError` only if the listener does not reuse the address**: `socketserver` documents the class variable `allow_reuse_address` ("This defaults to False, and can be set in subclasses to change the policy"), and `http.server.HTTPServer` sets it to a true value, with which a second bind to the same address and port raised no error on Windows (checked on Windows; the behaviour on Linux and macOS is unchecked). The sketch therefore binds a subclass with `allow_reuse_address = False`, catches the `OSError`, and prints the address and a clear message instead of waiting. `handle_request` processes one request and returns after `timeout` seconds if none arrives, which is why the sketch loops until the callback or the **overall deadline** `WAIT_SECONDS` (`timeout` bounds each wait only, and the deadline bounds the whole wait): a stray request, such as a browser asking for an icon, is answered `404` and does not end the wait. `webbrowser.open` shows the address "using the default browser" and returns whether one was launched. **Where tokens are kept:** a file with restricted permissions or the operating system's keychain (unchecked on every system; no keychain package is named here). **The extractable-secret statement:** a command-line tool or desktop program shipped to people carries its client secret in a form anyone who holds the distribution can extract, and it protects nothing against the person running it (a documentation gap: Relativity documents no recommendation for a distributed client). **Compensating guidance** (NSerio recommendation): Trusted IPs where the users' outbound addresses are known and stable (checked at token issuance; applicability to Code *inferred*); the 60-minute Access Token Lifetime; refresh tokens only when needed; and **one OAuth2 Client per distribution**, so a leaked secret can be disabled and reset alone.

Verification: **Verification note.** **A password manager is scheme-agnostic**: **the rule cannot tell whether the handler that receives it is Basic, Digest or a proxy handler**; if it is not a Basic handler for RelativityOne no migration to RelativityOne applies. **A token has a finite lifetime.** **The calling identity changes.** The scheme scoping of `add_password` changed in Python 3.15: behaviour before it is unchecked.

OAuth2 Client: See [OAuth2 Client for Authorization Code](#oauth2-client-for-authorization-code).
<!-- generated:end PY-009 authorization-code -->

### PY-010 · HttpxNetrcAuth

<a id="py-010-migration"></a>

Remove `NetRCAuth` and the entry for the host from the `.netrc` file, and send a Bearer token in the `Authorization` header instead. The credential is not in the code: it is in the file, so the file is where it is rotated and removed.

#### Client Credentials

<!-- generated:begin PY-010 client-credentials -->
**Flow.** This entry is for Client Credentials. The "Which flow" paragraph of PY-010 in [Part A](part-a-python.md#py-010) says when a match belongs to this flow.

Source form: **Applies to:** a Python script or service; `httpx`.

```python
client = httpx.Client(auth=httpx.NetRCAuth())
```

Under token authentication: **First, the destination:** if this Relativity instance is not RelativityOne — including a Relativity Server instance, whose own documentation governs — no migration to RelativityOne applies and the rest of this entry does not apply.

```python
CSRF_HEADER_VALUE = "-"
client = httpx.Client(headers={"Authorization": f"Bearer {access_token}", "X-CSRF-Header": CSRF_HEADER_VALUE})
```

Remove `NetRCAuth` and the entry for the host from the `.netrc` file.

Credentials: **Where the credentials come from.** The source form establishes that a client sends Basic, not that the destination is RelativityOne; if it is not RelativityOne the migration is that service's own token mechanism, no migration to RelativityOne applies, and the reader stops here. If it is RelativityOne, the token comes from the token-acquisition part that follows, and the user name and password are replaced by the OAuth2 Client's client ID and secret, kept as Secret Management (Part B of the Migration Documentation) describes, never in source.

The text a match assigned Client Credentials receives after part 1. Create an OAuth2 Client with Flow Grant Type set to Client Credentials (part 3 lists its settings), then send a form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=client_credentials`, `client_id` and `client_secret`. The token is sent as `Authorization: Bearer <token>`, with `X-CSRF-Header: -` on REST and Kepler calls. **The endpoint is documented for this grant.** The scope depends on what the integration needs and **no single scope is required**: Relativity's documentation shows `SystemUserInfo` and `UserInfoAccess` and no page says which is correct or what either grants; use the value the API you call documents, and confirm it.

**Variant: a Python process acting as itself**

**Precondition: this variant is for a Python job, service or script that does not act for a signed-in person. If a person signs in, use Authorization Code.**

```python
import os
import time

import requests

IDENTITY_TOKEN_URL = "https://HOST/Relativity/Identity/connect/token"
EXPIRY_MARGIN_SECONDS = 60
REQUEST_TIMEOUT_SECONDS = 30

_cached_token = {"value": None, "expires_at": 0.0}


def get_access_token():
	now = time.monotonic()
	if _cached_token["value"] is None or _cached_token["expires_at"] - now < EXPIRY_MARGIN_SECONDS:
		response = requests.post(
			IDENTITY_TOKEN_URL,
			data={
				"grant_type": "client_credentials",
				"client_id": os.environ["RELATIVITY_CLIENT_ID"],
				"client_secret": os.environ["RELATIVITY_CLIENT_SECRET"],
			},
			timeout=REQUEST_TIMEOUT_SECONDS,
		)
		response.raise_for_status()
		payload = response.json()
		_cached_token["value"] = payload["access_token"]
		_cached_token["expires_at"] = now + payload["expires_in"]
	return _cached_token["value"]
```

`requests` form-encodes a dictionary passed as `data`, and `raise_for_status` raises on a 4XX or 5XX response. **The same token request in the other three authorized clients** (the cache and the credentials are as above):

```python
HTTP_OK = 200
token_request_fields = {
	"grant_type": "client_credentials",
	"client_id": os.environ["RELATIVITY_CLIENT_ID"],
	"client_secret": os.environ["RELATIVITY_CLIENT_SECRET"],
}

# httpx: a dictionary passed as data is form-encoded
response = httpx.post(IDENTITY_TOKEN_URL, data=token_request_fields, timeout=REQUEST_TIMEOUT_SECONDS)
response.raise_for_status()
payload = response.json()

# aiohttp: data= is form-encoded
async with session.post(IDENTITY_TOKEN_URL, data=token_request_fields) as response:
	response.raise_for_status()
	payload = await response.json()

# urllib3: PoolManager.request form-encodes the fields argument for POST only with encode_multipart=False
response = urllib3.PoolManager().request("POST", IDENTITY_TOKEN_URL, fields=token_request_fields, encode_multipart=False)
if response.status != HTTP_OK:
	raise RuntimeError(f"The token request failed with status {response.status}")
payload = response.json()
```

**Cache the token and re-acquire it on expiry — do not refresh.** Request a new token with the client secret shortly before the current one expires (the block uses a margin of 60 seconds), and once when a call is rejected for authentication. Client Credentials does not offer refresh tokens: **verified by NSerio against a live instance; not documented by Relativity** — the Allow Refresh Tokens control does not appear for that grant in the RelativityOne user interface. The recommended Access Token Lifetime is 60 minutes. **The client secret** belongs in the process environment or a secret store, as Secret Management (Part B of the Migration Documentation) describes, **never in source or in a tracked file**. A reset invalidates the previous secret immediately, so plan the cut-over (the two-client pattern is inferred). **Before deploying, confirm the calling identity:** every call now runs as the OAuth2 Client's Context User, not as the user in the replaced credential, and the non-administrator Context User is NSerio's recommendation. Trusted IPs restrict where a token can be requested, not a token already issued. **Whether `httpx` and `urllib3` accept a dictionary the way the pages read say is documented for the page's version only** (`httpx` shows no version): unchecked for other versions.

Verification: **Verification note.** **No credential is in the code: it is in a file outside the repository**, which is not read; **that file still holds a password at rest and the entry must be removed or rotated** (the rotation statement below). The netrc behaviour is documented for `httpx` (no version shown); the implicit netrc forms that use no construct are not matches. **A token has a finite lifetime.** **The calling identity changes.**

> Treat this credential as compromised. Rotate it, and assume the repository history has published it. Completing the migration does not close this exposure; rotation does.

OAuth2 Client: See [OAuth2 Client for Client Credentials](#oauth2-client-for-client-credentials).
<!-- generated:end PY-010 client-credentials -->

#### Authorization Code

<!-- generated:begin PY-010 authorization-code -->
**Flow.** This entry is for Authorization Code. The "Which flow" paragraph of PY-010 in [Part A](part-a-python.md#py-010) says when a match belongs to this flow.

Source form: **Applies to:** a Python script or service; `httpx`.

```python
client = httpx.Client(auth=httpx.NetRCAuth())
```

Under token authentication: **First, the destination:** if this Relativity instance is not RelativityOne — including a Relativity Server instance, whose own documentation governs — no migration to RelativityOne applies and the rest of this entry does not apply.

```python
CSRF_HEADER_VALUE = "-"
client = httpx.Client(headers={"Authorization": f"Bearer {access_token}", "X-CSRF-Header": CSRF_HEADER_VALUE})
```

Remove `NetRCAuth` and the entry for the host from the `.netrc` file.

Credentials: **Where the credentials come from.** The source form establishes that a client sends Basic, not that the destination is RelativityOne; if it is not RelativityOne the migration is that service's own token mechanism, no migration to RelativityOne applies, and the reader stops here. If it is RelativityOne, the token comes from the token-acquisition part that follows, and the user name and password are replaced by the OAuth2 Client's client ID and secret, kept as Secret Management (Part B of the Migration Documentation) describes, never in source.

**Common to both variants.** The person signs in on RelativityOne's own page and the code never holds their password. The text has a common part and **two variants by application type**, each opening with the precondition it is for: a server-side web application and a command-line or desktop tool. Read the precondition of each and discard the variant that does not apply.

**Sign in.** Replace the code that collected the person's credentials with a redirect to the authorize endpoint `<host>/Relativity/Identity/connect/authorize` (**verified by NSerio against a live instance; not documented by Relativity**), which leads to RelativityOne's login page. The request carries the standard authorization-code parameters — `client_id`, `response_type=code`, the registered `redirect_uri`, `state` checked on return, and `code_challenge` with `code_challenge_method=S256` when PKCE is used; these are standard OAuth (RFC 6749 and RFC 7636) and Relativity documents none of them.

**PKCE.** PKCE is required by Relativity's own documentation to obtain a refresh token and not otherwise: a Code request that does not ask for a refresh token works without it, **verified by NSerio against a live instance; not documented by Relativity**. NSerio recommends `S256` (NSerio recommendation) and does not claim Relativity accepts any particular method. The verifier is 43 to 128 characters of the unreserved set and the `S256` challenge is the base64url of the SHA-256 of the verifier; `secrets.token_urlsafe(32)` gives 43 characters from that set (that the two match is arithmetic, not a tested fact) and `hashlib.sha256` and `base64.urlsafe_b64encode` compute the challenge. **`state`** is generated, stored against the session and compared on return; `nonce` is not used, and Relativity does not document whether it enforces either.

**The client secret is required.** Code does not work without a client secret: tested and rejected, **verified by NSerio against a live instance; not documented by Relativity**, which confirms Relativity's own statement. **A secret kept in a program that is distributed is extractable**, so the exchange runs where a secret can be kept.

**Exchange the code.** A form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=authorization_code`, the code, the same `redirect_uri`, the client ID and the secret, plus the `code_verifier` when PKCE is used (**verified by NSerio against a live instance; not documented by Relativity**).

**Redirect URL.** A server-side application registers **one exact HTTPS URL per environment, no wildcards** (the matching rules are not documented and no claim is made about them). A plain-HTTP loopback Redirect URL `http://127.0.0.1:<port>/<path>` on a **fixed registered port** is accepted, **verified by NSerio against a live instance; not documented by Relativity**, and is the command-line shape. RFC 8252 requires an authorization server to allow any port for a loopback redirect; **Relativity's behaviour is the fixed registered port, and the RFC's rule is not claimed for it.**

**On expiry.** With Allow Refresh Tokens enabled and PKCE used, the code holds a refresh token and sends `grant_type=refresh_token` in place of the code exchange: *inferred* from the standard OAuth shape, not tested; Relativity does not document a refresh token's lifetime, rotation, revocation or revocation endpoint, nor what disabling the client does to tokens already issued. **A refresh token is a secret.** **Single sign-on:** sign-in happens on RelativityOne's own page, so the instance's single sign-on and two-factor rules apply unchanged (*inferred*). **The code stops collecting the password:** after this change nothing receives, stores or forwards the person's user name and password, and the code holds only tokens. Every call now runs as the person who signed in, so **the calling identity changes**. **Scope:** no scope set is prescribed for Code; `openid RelativityRequestOrigin UserInfo UserInfoAccess AuthenticationID RelativityWeb` works, **verified by NSerio against a live instance; not documented by Relativity**, as working and not as minimal.

**Variant 1 — a server-side web application (Flask, Django, FastAPI or similar)**

**Precondition: this variant is for code that runs in Python as a web server and acts for people who sign in to it. It is not for a command-line tool (variant 2).**

Standard server-side Code flow. A login route builds the authorize address with `state` (and the PKCE verifier where a refresh token is wanted), stores them against the visitor's session and redirects; RelativityOne redirects to the **callback route**, registered as an exact HTTPS URL; the callback checks `state` and exchanges the code with the secret. **The logic is framework-neutral; the framework supplies the routes and the session**, and the sketch shows the callback as a handler of the standard library's `http.server` so that it has no dependency: in a Flask, Django or FastAPI application the same two functions are called from the login and callback views, and the dictionary of pending logins is the framework's session (Flask's session holds data in a signed cookie that the user can read but not modify, so **NSerio recommends that it hold neither the verifier nor a token**; Django's session framework "stores data on the server side" with the cookie holding only a session ID, "unless you're using the cookie based backend" (`SESSION_ENGINE` set to `django.contrib.sessions.backends.signed_cookies`), whose data "can be read by the client"; the pages of FastAPI read name no session store). **No framework code is shown: none is installed and none is verified.**

```python
import base64
import hashlib
import http.server
import secrets
from urllib.parse import parse_qs, urlencode, urlparse

import requests

IDENTITY_BASE = "https://HOST/Relativity/Identity/connect"
REDIRECT_URI = "https://app.example.test/auth/callback"
CALLBACK_PATH = "/auth/callback"
RANDOM_BYTES = 32
REQUEST_TIMEOUT_SECONDS = 30
HTTP_OK = 200
HTTP_NOT_FOUND = 404
_pending_by_session = {}


def base64_url(raw_bytes):
	return base64.urlsafe_b64encode(raw_bytes).rstrip(b"=").decode("ascii")


def begin_login(session_id, client_id, want_refresh_token):
	state = secrets.token_urlsafe(RANDOM_BYTES)
	pending = {"state": state, "verifier": None}
	query = {
		"response_type": "code",
		"client_id": client_id,
		"redirect_uri": REDIRECT_URI,
		"state": state,
	}
	if want_refresh_token:
		verifier = secrets.token_urlsafe(RANDOM_BYTES)
		pending["verifier"] = verifier
		query["code_challenge"] = base64_url(hashlib.sha256(verifier.encode("ascii")).digest())
		query["code_challenge_method"] = "S256"
	_pending_by_session[session_id] = pending
	return f"{IDENTITY_BASE}/authorize?{urlencode(query)}"


def complete_login(session_id, client_id, client_secret, returned_code, returned_state):
	pending = _pending_by_session.pop(session_id, None)
	tokens = None
	state_matches = (
		pending is not None
		and returned_state is not None
		and secrets.compare_digest(returned_state.encode("utf-8"), pending["state"].encode("utf-8"))
	)
	if state_matches:
		fields = {
			"grant_type": "authorization_code",
			"code": returned_code,
			"redirect_uri": REDIRECT_URI,
			"client_id": client_id,
			"client_secret": client_secret,
		}
		if pending["verifier"] is not None:
			fields["code_verifier"] = pending["verifier"]
		response = requests.post(f"{IDENTITY_BASE}/token", data=fields, timeout=REQUEST_TIMEOUT_SECONDS)
		tokens = response.json() if response.status_code == HTTP_OK else None
	return tokens


class CallbackHandler(http.server.BaseHTTPRequestHandler):
	def do_GET(self):
		address = urlparse(self.path)
		is_callback = address.path == CALLBACK_PATH
		self.send_response(HTTP_OK if is_callback else HTTP_NOT_FOUND)
		self.end_headers()
		if is_callback:
			query = parse_qs(address.query)
			self.wfile.write(b"Signed in.")
			self.server.returned_code = query.get("code", [None])[0]
			self.server.returned_state = query.get("state", [None])[0]
```

`state` is consumed once and compared on return in constant time (`secrets.compare_digest`); a request with no pending entry or a different `state` gets no exchange. **The scope is not prescribed** (the sketch sends none). **Where the secret travels in the token request** (the body, as sketched, or HTTP Basic client authentication) is **not verified**; RFC 6749 allows either form for a client password. The sketch does not show the HTTP server's TLS, the session cookie or a session store that survives more than one process (the module dictionary does not). **The handler only records the returned `code` and `state`; in a real application the callback view calls `complete_login` with the visitor's own session identifier**, so that the pending entry of that visitor, and no other, is consumed. **Tokens stay on the server**, in a session store that is not exposed to a request.

**The `http.server` handler is a shape, not a deployment.** The `http.server` page states that the module "is not recommended for production. It only implements basic security checks" (Python 3.15.2). The handler above shows what the callback reads and answers, so that the sketch has no dependency; **an application that serves people runs the callback as a view of the framework it already uses** (Flask, Django or FastAPI), behind the TLS and the session store that framework and its server provide.

**Variant 2 — a command-line or desktop tool**

**Precondition: this variant is for code that runs on a person's own machine, as a command-line tool or a desktop shell, and signs that person in. It is not for a web server (variant 1).**

The process starts a listener from the standard library, bound to **`127.0.0.1` only**, on the **fixed registered port**, for **one callback request**; opens the system browser on the authorize address **and always prints the address too**, so a machine with no browser, an SSH session or a failed launch has a manual route; reads `code` and `state` from the callback request, closes the listener, checks `state`, and exchanges the code as variant 1's `complete_login` does (with the redirect URI `http://127.0.0.1:8123/callback`).

```python
import http.server
import time
import webbrowser
from urllib.parse import parse_qs, urlparse

LOOPBACK_HOST = "127.0.0.1"
REGISTERED_PORT = 8123
CALLBACK_PATH = "/callback"
WAIT_SECONDS = 120
POLL_SECONDS = 1.0
HTTP_OK = 200
HTTP_NOT_FOUND = 404


class _RedirectHandler(http.server.BaseHTTPRequestHandler):
	received = None

	def do_GET(self):
		address = urlparse(self.path)
		is_callback = address.path == CALLBACK_PATH
		self.send_response(HTTP_OK if is_callback else HTTP_NOT_FOUND)
		self.end_headers()
		self.wfile.write(b"You can close this window." if is_callback else b"Not found")
		if is_callback:
			query = parse_qs(address.query)
			type(self).received = {
				"code": query.get("code", [None])[0],
				"state": query.get("state", [None])[0],
			}

	def log_message(self, message_format, *arguments):
		return None


class _LoopbackServer(http.server.HTTPServer):
	allow_reuse_address = False


def wait_for_redirect(authorize_address):
	_RedirectHandler.received = None
	print(f"Sign in at: {authorize_address}")
	try:
		server = _LoopbackServer((LOOPBACK_HOST, REGISTERED_PORT), _RedirectHandler)
	except OSError:
		print(f"Port {REGISTERED_PORT} is in use. Close the program that holds it and sign in again.")
	else:
		server.timeout = POLL_SECONDS
		webbrowser.open(authorize_address)
		deadline = time.monotonic() + WAIT_SECONDS
		while _RedirectHandler.received is None and time.monotonic() < deadline:
			server.handle_request()
		server.server_close()
	return _RedirectHandler.received
```

**Rules for the listener:** bind to loopback only; **check `state`** (another local process can connect to a fixed, advertised port); close the listener after the callback request; and handle a busy port. **A busy port raises `OSError` only if the listener does not reuse the address**: `socketserver` documents the class variable `allow_reuse_address` ("This defaults to False, and can be set in subclasses to change the policy"), and `http.server.HTTPServer` sets it to a true value, with which a second bind to the same address and port raised no error on Windows (checked on Windows; the behaviour on Linux and macOS is unchecked). The sketch therefore binds a subclass with `allow_reuse_address = False`, catches the `OSError`, and prints the address and a clear message instead of waiting. `handle_request` processes one request and returns after `timeout` seconds if none arrives, which is why the sketch loops until the callback or the **overall deadline** `WAIT_SECONDS` (`timeout` bounds each wait only, and the deadline bounds the whole wait): a stray request, such as a browser asking for an icon, is answered `404` and does not end the wait. `webbrowser.open` shows the address "using the default browser" and returns whether one was launched. **Where tokens are kept:** a file with restricted permissions or the operating system's keychain (unchecked on every system; no keychain package is named here). **The extractable-secret statement:** a command-line tool or desktop program shipped to people carries its client secret in a form anyone who holds the distribution can extract, and it protects nothing against the person running it (a documentation gap: Relativity documents no recommendation for a distributed client). **Compensating guidance** (NSerio recommendation): Trusted IPs where the users' outbound addresses are known and stable (checked at token issuance; applicability to Code *inferred*); the 60-minute Access Token Lifetime; refresh tokens only when needed; and **one OAuth2 Client per distribution**, so a leaked secret can be disabled and reset alone.

Verification: **Verification note.** **No credential is in the code: it is in a file outside the repository**, which is not read; **that file still holds a password at rest and the entry must be removed or rotated** (the rotation statement below). The netrc behaviour is documented for `httpx` (no version shown); the implicit netrc forms that use no construct are not matches. **A token has a finite lifetime.** **The calling identity changes.**

> Treat this credential as compromised. Rotate it, and assume the repository history has published it. Completing the migration does not close this exposure; rotation does.

OAuth2 Client: See [OAuth2 Client for Authorization Code](#oauth2-client-for-authorization-code).
<!-- generated:end PY-010 authorization-code -->

### PY-011 · CredentialsInRequestUrl

<a id="py-011-migration"></a>

Remove the user name and the password from the address and send a Bearer token in the `Authorization` header instead. **A credential in an address is also in logs, in a process list and in the repository history**: apply the rotation statement below.

#### Client Credentials

<!-- generated:begin PY-011 client-credentials -->
**Flow.** This entry is for Client Credentials. The "Which flow" paragraph of PY-011 in [Part A](part-a-python.md#py-011) says when a match belongs to this flow.

Source form: **Applies to:** a Python script or service; `requests`, `httpx`, `aiohttp`, `urllib3`, `urllib.request`.

```python
response = requests.get("https://user_name:secret@example.test/items")
```

Under token authentication: **First, the destination:** if this Relativity instance is not RelativityOne — including a Relativity Server instance, whose own documentation governs — no migration to RelativityOne applies and the rest of this entry does not apply.

```python
CSRF_HEADER_VALUE = "-"
response = requests.get("https://example.test/items", headers={"Authorization": f"Bearer {access_token}", "X-CSRF-Header": CSRF_HEADER_VALUE})
```

Remove the user name and password from the address. **A credential in an address is also in logs, in a process list and in the repository history**: apply the rotation statement below.

Credentials: **Where the credentials come from.** The source form establishes that a client sends Basic, not that the destination is RelativityOne; if it is not RelativityOne the migration is that service's own token mechanism, no migration to RelativityOne applies, and the reader stops here. If it is RelativityOne, the token comes from the token-acquisition part that follows, and the user name and password are replaced by the OAuth2 Client's client ID and secret, kept as Secret Management (Part B of the Migration Documentation) describes, never in source.

The text a match assigned Client Credentials receives after part 1. Create an OAuth2 Client with Flow Grant Type set to Client Credentials (part 3 lists its settings), then send a form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=client_credentials`, `client_id` and `client_secret`. The token is sent as `Authorization: Bearer <token>`, with `X-CSRF-Header: -` on REST and Kepler calls. **The endpoint is documented for this grant.** The scope depends on what the integration needs and **no single scope is required**: Relativity's documentation shows `SystemUserInfo` and `UserInfoAccess` and no page says which is correct or what either grants; use the value the API you call documents, and confirm it.

**Variant: a Python process acting as itself**

**Precondition: this variant is for a Python job, service or script that does not act for a signed-in person. If a person signs in, use Authorization Code.**

```python
import os
import time

import requests

IDENTITY_TOKEN_URL = "https://HOST/Relativity/Identity/connect/token"
EXPIRY_MARGIN_SECONDS = 60
REQUEST_TIMEOUT_SECONDS = 30

_cached_token = {"value": None, "expires_at": 0.0}


def get_access_token():
	now = time.monotonic()
	if _cached_token["value"] is None or _cached_token["expires_at"] - now < EXPIRY_MARGIN_SECONDS:
		response = requests.post(
			IDENTITY_TOKEN_URL,
			data={
				"grant_type": "client_credentials",
				"client_id": os.environ["RELATIVITY_CLIENT_ID"],
				"client_secret": os.environ["RELATIVITY_CLIENT_SECRET"],
			},
			timeout=REQUEST_TIMEOUT_SECONDS,
		)
		response.raise_for_status()
		payload = response.json()
		_cached_token["value"] = payload["access_token"]
		_cached_token["expires_at"] = now + payload["expires_in"]
	return _cached_token["value"]
```

`requests` form-encodes a dictionary passed as `data`, and `raise_for_status` raises on a 4XX or 5XX response. **The same token request in the other three authorized clients** (the cache and the credentials are as above):

```python
HTTP_OK = 200
token_request_fields = {
	"grant_type": "client_credentials",
	"client_id": os.environ["RELATIVITY_CLIENT_ID"],
	"client_secret": os.environ["RELATIVITY_CLIENT_SECRET"],
}

# httpx: a dictionary passed as data is form-encoded
response = httpx.post(IDENTITY_TOKEN_URL, data=token_request_fields, timeout=REQUEST_TIMEOUT_SECONDS)
response.raise_for_status()
payload = response.json()

# aiohttp: data= is form-encoded
async with session.post(IDENTITY_TOKEN_URL, data=token_request_fields) as response:
	response.raise_for_status()
	payload = await response.json()

# urllib3: PoolManager.request form-encodes the fields argument for POST only with encode_multipart=False
response = urllib3.PoolManager().request("POST", IDENTITY_TOKEN_URL, fields=token_request_fields, encode_multipart=False)
if response.status != HTTP_OK:
	raise RuntimeError(f"The token request failed with status {response.status}")
payload = response.json()
```

**Cache the token and re-acquire it on expiry — do not refresh.** Request a new token with the client secret shortly before the current one expires (the block uses a margin of 60 seconds), and once when a call is rejected for authentication. Client Credentials does not offer refresh tokens: **verified by NSerio against a live instance; not documented by Relativity** — the Allow Refresh Tokens control does not appear for that grant in the RelativityOne user interface. The recommended Access Token Lifetime is 60 minutes. **The client secret** belongs in the process environment or a secret store, as Secret Management (Part B of the Migration Documentation) describes, **never in source or in a tracked file**. A reset invalidates the previous secret immediately, so plan the cut-over (the two-client pattern is inferred). **Before deploying, confirm the calling identity:** every call now runs as the OAuth2 Client's Context User, not as the user in the replaced credential, and the non-administrator Context User is NSerio's recommendation. Trusted IPs restrict where a token can be requested, not a token already issued. **Whether `httpx` and `urllib3` accept a dictionary the way the pages read say is documented for the page's version only** (`httpx` shows no version): unchecked for other versions.

Verification: **Verification note.** **This match is Medium**: the pages read state that a client turns the userinfo into a Basic header for **`aiohttp` only**; for `requests`, `httpx`, `urllib3` and `urllib.request` the behaviour is **unchecked** and this text does not claim it. A URL assembled from parts is not read. **A token has a finite lifetime.** **The calling identity changes.**

> Treat this credential as compromised. Rotate it, and assume the repository history has published it. Completing the migration does not close this exposure; rotation does.

OAuth2 Client: See [OAuth2 Client for Client Credentials](#oauth2-client-for-client-credentials).
<!-- generated:end PY-011 client-credentials -->

#### Authorization Code

<!-- generated:begin PY-011 authorization-code -->
**Flow.** This entry is for Authorization Code. The "Which flow" paragraph of PY-011 in [Part A](part-a-python.md#py-011) says when a match belongs to this flow.

Source form: **Applies to:** a Python script or service; `requests`, `httpx`, `aiohttp`, `urllib3`, `urllib.request`.

```python
response = requests.get("https://user_name:secret@example.test/items")
```

Under token authentication: **First, the destination:** if this Relativity instance is not RelativityOne — including a Relativity Server instance, whose own documentation governs — no migration to RelativityOne applies and the rest of this entry does not apply.

```python
CSRF_HEADER_VALUE = "-"
response = requests.get("https://example.test/items", headers={"Authorization": f"Bearer {access_token}", "X-CSRF-Header": CSRF_HEADER_VALUE})
```

Remove the user name and password from the address. **A credential in an address is also in logs, in a process list and in the repository history**: apply the rotation statement below.

Credentials: **Where the credentials come from.** The source form establishes that a client sends Basic, not that the destination is RelativityOne; if it is not RelativityOne the migration is that service's own token mechanism, no migration to RelativityOne applies, and the reader stops here. If it is RelativityOne, the token comes from the token-acquisition part that follows, and the user name and password are replaced by the OAuth2 Client's client ID and secret, kept as Secret Management (Part B of the Migration Documentation) describes, never in source.

**Common to both variants.** The person signs in on RelativityOne's own page and the code never holds their password. The text has a common part and **two variants by application type**, each opening with the precondition it is for: a server-side web application and a command-line or desktop tool. Read the precondition of each and discard the variant that does not apply.

**Sign in.** Replace the code that collected the person's credentials with a redirect to the authorize endpoint `<host>/Relativity/Identity/connect/authorize` (**verified by NSerio against a live instance; not documented by Relativity**), which leads to RelativityOne's login page. The request carries the standard authorization-code parameters — `client_id`, `response_type=code`, the registered `redirect_uri`, `state` checked on return, and `code_challenge` with `code_challenge_method=S256` when PKCE is used; these are standard OAuth (RFC 6749 and RFC 7636) and Relativity documents none of them.

**PKCE.** PKCE is required by Relativity's own documentation to obtain a refresh token and not otherwise: a Code request that does not ask for a refresh token works without it, **verified by NSerio against a live instance; not documented by Relativity**. NSerio recommends `S256` (NSerio recommendation) and does not claim Relativity accepts any particular method. The verifier is 43 to 128 characters of the unreserved set and the `S256` challenge is the base64url of the SHA-256 of the verifier; `secrets.token_urlsafe(32)` gives 43 characters from that set (that the two match is arithmetic, not a tested fact) and `hashlib.sha256` and `base64.urlsafe_b64encode` compute the challenge. **`state`** is generated, stored against the session and compared on return; `nonce` is not used, and Relativity does not document whether it enforces either.

**The client secret is required.** Code does not work without a client secret: tested and rejected, **verified by NSerio against a live instance; not documented by Relativity**, which confirms Relativity's own statement. **A secret kept in a program that is distributed is extractable**, so the exchange runs where a secret can be kept.

**Exchange the code.** A form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=authorization_code`, the code, the same `redirect_uri`, the client ID and the secret, plus the `code_verifier` when PKCE is used (**verified by NSerio against a live instance; not documented by Relativity**).

**Redirect URL.** A server-side application registers **one exact HTTPS URL per environment, no wildcards** (the matching rules are not documented and no claim is made about them). A plain-HTTP loopback Redirect URL `http://127.0.0.1:<port>/<path>` on a **fixed registered port** is accepted, **verified by NSerio against a live instance; not documented by Relativity**, and is the command-line shape. RFC 8252 requires an authorization server to allow any port for a loopback redirect; **Relativity's behaviour is the fixed registered port, and the RFC's rule is not claimed for it.**

**On expiry.** With Allow Refresh Tokens enabled and PKCE used, the code holds a refresh token and sends `grant_type=refresh_token` in place of the code exchange: *inferred* from the standard OAuth shape, not tested; Relativity does not document a refresh token's lifetime, rotation, revocation or revocation endpoint, nor what disabling the client does to tokens already issued. **A refresh token is a secret.** **Single sign-on:** sign-in happens on RelativityOne's own page, so the instance's single sign-on and two-factor rules apply unchanged (*inferred*). **The code stops collecting the password:** after this change nothing receives, stores or forwards the person's user name and password, and the code holds only tokens. Every call now runs as the person who signed in, so **the calling identity changes**. **Scope:** no scope set is prescribed for Code; `openid RelativityRequestOrigin UserInfo UserInfoAccess AuthenticationID RelativityWeb` works, **verified by NSerio against a live instance; not documented by Relativity**, as working and not as minimal.

**Variant 1 — a server-side web application (Flask, Django, FastAPI or similar)**

**Precondition: this variant is for code that runs in Python as a web server and acts for people who sign in to it. It is not for a command-line tool (variant 2).**

Standard server-side Code flow. A login route builds the authorize address with `state` (and the PKCE verifier where a refresh token is wanted), stores them against the visitor's session and redirects; RelativityOne redirects to the **callback route**, registered as an exact HTTPS URL; the callback checks `state` and exchanges the code with the secret. **The logic is framework-neutral; the framework supplies the routes and the session**, and the sketch shows the callback as a handler of the standard library's `http.server` so that it has no dependency: in a Flask, Django or FastAPI application the same two functions are called from the login and callback views, and the dictionary of pending logins is the framework's session (Flask's session holds data in a signed cookie that the user can read but not modify, so **NSerio recommends that it hold neither the verifier nor a token**; Django's session framework "stores data on the server side" with the cookie holding only a session ID, "unless you're using the cookie based backend" (`SESSION_ENGINE` set to `django.contrib.sessions.backends.signed_cookies`), whose data "can be read by the client"; the pages of FastAPI read name no session store). **No framework code is shown: none is installed and none is verified.**

```python
import base64
import hashlib
import http.server
import secrets
from urllib.parse import parse_qs, urlencode, urlparse

import requests

IDENTITY_BASE = "https://HOST/Relativity/Identity/connect"
REDIRECT_URI = "https://app.example.test/auth/callback"
CALLBACK_PATH = "/auth/callback"
RANDOM_BYTES = 32
REQUEST_TIMEOUT_SECONDS = 30
HTTP_OK = 200
HTTP_NOT_FOUND = 404
_pending_by_session = {}


def base64_url(raw_bytes):
	return base64.urlsafe_b64encode(raw_bytes).rstrip(b"=").decode("ascii")


def begin_login(session_id, client_id, want_refresh_token):
	state = secrets.token_urlsafe(RANDOM_BYTES)
	pending = {"state": state, "verifier": None}
	query = {
		"response_type": "code",
		"client_id": client_id,
		"redirect_uri": REDIRECT_URI,
		"state": state,
	}
	if want_refresh_token:
		verifier = secrets.token_urlsafe(RANDOM_BYTES)
		pending["verifier"] = verifier
		query["code_challenge"] = base64_url(hashlib.sha256(verifier.encode("ascii")).digest())
		query["code_challenge_method"] = "S256"
	_pending_by_session[session_id] = pending
	return f"{IDENTITY_BASE}/authorize?{urlencode(query)}"


def complete_login(session_id, client_id, client_secret, returned_code, returned_state):
	pending = _pending_by_session.pop(session_id, None)
	tokens = None
	state_matches = (
		pending is not None
		and returned_state is not None
		and secrets.compare_digest(returned_state.encode("utf-8"), pending["state"].encode("utf-8"))
	)
	if state_matches:
		fields = {
			"grant_type": "authorization_code",
			"code": returned_code,
			"redirect_uri": REDIRECT_URI,
			"client_id": client_id,
			"client_secret": client_secret,
		}
		if pending["verifier"] is not None:
			fields["code_verifier"] = pending["verifier"]
		response = requests.post(f"{IDENTITY_BASE}/token", data=fields, timeout=REQUEST_TIMEOUT_SECONDS)
		tokens = response.json() if response.status_code == HTTP_OK else None
	return tokens


class CallbackHandler(http.server.BaseHTTPRequestHandler):
	def do_GET(self):
		address = urlparse(self.path)
		is_callback = address.path == CALLBACK_PATH
		self.send_response(HTTP_OK if is_callback else HTTP_NOT_FOUND)
		self.end_headers()
		if is_callback:
			query = parse_qs(address.query)
			self.wfile.write(b"Signed in.")
			self.server.returned_code = query.get("code", [None])[0]
			self.server.returned_state = query.get("state", [None])[0]
```

`state` is consumed once and compared on return in constant time (`secrets.compare_digest`); a request with no pending entry or a different `state` gets no exchange. **The scope is not prescribed** (the sketch sends none). **Where the secret travels in the token request** (the body, as sketched, or HTTP Basic client authentication) is **not verified**; RFC 6749 allows either form for a client password. The sketch does not show the HTTP server's TLS, the session cookie or a session store that survives more than one process (the module dictionary does not). **The handler only records the returned `code` and `state`; in a real application the callback view calls `complete_login` with the visitor's own session identifier**, so that the pending entry of that visitor, and no other, is consumed. **Tokens stay on the server**, in a session store that is not exposed to a request.

**The `http.server` handler is a shape, not a deployment.** The `http.server` page states that the module "is not recommended for production. It only implements basic security checks" (Python 3.15.2). The handler above shows what the callback reads and answers, so that the sketch has no dependency; **an application that serves people runs the callback as a view of the framework it already uses** (Flask, Django or FastAPI), behind the TLS and the session store that framework and its server provide.

**Variant 2 — a command-line or desktop tool**

**Precondition: this variant is for code that runs on a person's own machine, as a command-line tool or a desktop shell, and signs that person in. It is not for a web server (variant 1).**

The process starts a listener from the standard library, bound to **`127.0.0.1` only**, on the **fixed registered port**, for **one callback request**; opens the system browser on the authorize address **and always prints the address too**, so a machine with no browser, an SSH session or a failed launch has a manual route; reads `code` and `state` from the callback request, closes the listener, checks `state`, and exchanges the code as variant 1's `complete_login` does (with the redirect URI `http://127.0.0.1:8123/callback`).

```python
import http.server
import time
import webbrowser
from urllib.parse import parse_qs, urlparse

LOOPBACK_HOST = "127.0.0.1"
REGISTERED_PORT = 8123
CALLBACK_PATH = "/callback"
WAIT_SECONDS = 120
POLL_SECONDS = 1.0
HTTP_OK = 200
HTTP_NOT_FOUND = 404


class _RedirectHandler(http.server.BaseHTTPRequestHandler):
	received = None

	def do_GET(self):
		address = urlparse(self.path)
		is_callback = address.path == CALLBACK_PATH
		self.send_response(HTTP_OK if is_callback else HTTP_NOT_FOUND)
		self.end_headers()
		self.wfile.write(b"You can close this window." if is_callback else b"Not found")
		if is_callback:
			query = parse_qs(address.query)
			type(self).received = {
				"code": query.get("code", [None])[0],
				"state": query.get("state", [None])[0],
			}

	def log_message(self, message_format, *arguments):
		return None


class _LoopbackServer(http.server.HTTPServer):
	allow_reuse_address = False


def wait_for_redirect(authorize_address):
	_RedirectHandler.received = None
	print(f"Sign in at: {authorize_address}")
	try:
		server = _LoopbackServer((LOOPBACK_HOST, REGISTERED_PORT), _RedirectHandler)
	except OSError:
		print(f"Port {REGISTERED_PORT} is in use. Close the program that holds it and sign in again.")
	else:
		server.timeout = POLL_SECONDS
		webbrowser.open(authorize_address)
		deadline = time.monotonic() + WAIT_SECONDS
		while _RedirectHandler.received is None and time.monotonic() < deadline:
			server.handle_request()
		server.server_close()
	return _RedirectHandler.received
```

**Rules for the listener:** bind to loopback only; **check `state`** (another local process can connect to a fixed, advertised port); close the listener after the callback request; and handle a busy port. **A busy port raises `OSError` only if the listener does not reuse the address**: `socketserver` documents the class variable `allow_reuse_address` ("This defaults to False, and can be set in subclasses to change the policy"), and `http.server.HTTPServer` sets it to a true value, with which a second bind to the same address and port raised no error on Windows (checked on Windows; the behaviour on Linux and macOS is unchecked). The sketch therefore binds a subclass with `allow_reuse_address = False`, catches the `OSError`, and prints the address and a clear message instead of waiting. `handle_request` processes one request and returns after `timeout` seconds if none arrives, which is why the sketch loops until the callback or the **overall deadline** `WAIT_SECONDS` (`timeout` bounds each wait only, and the deadline bounds the whole wait): a stray request, such as a browser asking for an icon, is answered `404` and does not end the wait. `webbrowser.open` shows the address "using the default browser" and returns whether one was launched. **Where tokens are kept:** a file with restricted permissions or the operating system's keychain (unchecked on every system; no keychain package is named here). **The extractable-secret statement:** a command-line tool or desktop program shipped to people carries its client secret in a form anyone who holds the distribution can extract, and it protects nothing against the person running it (a documentation gap: Relativity documents no recommendation for a distributed client). **Compensating guidance** (NSerio recommendation): Trusted IPs where the users' outbound addresses are known and stable (checked at token issuance; applicability to Code *inferred*); the 60-minute Access Token Lifetime; refresh tokens only when needed; and **one OAuth2 Client per distribution**, so a leaked secret can be disabled and reset alone.

Verification: **Verification note.** **This match is Medium**: the pages read state that a client turns the userinfo into a Basic header for **`aiohttp` only**; for `requests`, `httpx`, `urllib3` and `urllib.request` the behaviour is **unchecked** and this text does not claim it. A URL assembled from parts is not read. **A token has a finite lifetime.** **The calling identity changes.**

> Treat this credential as compromised. Rotate it, and assume the repository history has published it. Completing the migration does not close this exposure; rotation does.

OAuth2 Client: See [OAuth2 Client for Authorization Code](#oauth2-client-for-authorization-code).
<!-- generated:end PY-011 authorization-code -->

### PY-012 · UrlUserinfoHeldLiteral

<a id="py-012-migration"></a>

Remove the user information from the held address and send a Bearer token in the `Authorization` header instead. The address is also held in the repository history: apply the rotation statement below.

#### Client Credentials

<!-- generated:begin PY-012 client-credentials -->
**Flow.** This entry is for Client Credentials. The "Which flow" paragraph of PY-012 in [Part A](part-a-python.md#py-012) says when a match belongs to this flow.

Source form: **Applies to:** a Python script or service.

```python
BASE_URL = "https://user_name:secret@example.test/items"
```

Under token authentication: **First, the destination:** if this Relativity instance is not RelativityOne — including a Relativity Server instance, whose own documentation governs — no migration to RelativityOne applies and the rest of this entry does not apply.

```python
BASE_URL = "https://example.test/items"
CSRF_HEADER_VALUE = "-"
response = requests.get(BASE_URL, headers={"Authorization": f"Bearer {access_token}", "X-CSRF-Header": CSRF_HEADER_VALUE})
```

Remove the userinfo from the held address and apply the rotation statement below.

Credentials: **Where the credentials come from.** The source form establishes that a client sends Basic, not that the destination is RelativityOne; if it is not RelativityOne the migration is that service's own token mechanism, no migration to RelativityOne applies, and the reader stops here. If it is RelativityOne, the token comes from the token-acquisition part that follows, and the user name and password are replaced by the OAuth2 Client's client ID and secret, kept as Secret Management (Part B of the Migration Documentation) describes, never in source.

The text a match assigned Client Credentials receives after part 1. Create an OAuth2 Client with Flow Grant Type set to Client Credentials (part 3 lists its settings), then send a form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=client_credentials`, `client_id` and `client_secret`. The token is sent as `Authorization: Bearer <token>`, with `X-CSRF-Header: -` on REST and Kepler calls. **The endpoint is documented for this grant.** The scope depends on what the integration needs and **no single scope is required**: Relativity's documentation shows `SystemUserInfo` and `UserInfoAccess` and no page says which is correct or what either grants; use the value the API you call documents, and confirm it.

**Variant: a Python process acting as itself**

**Precondition: this variant is for a Python job, service or script that does not act for a signed-in person. If a person signs in, use Authorization Code.**

```python
import os
import time

import requests

IDENTITY_TOKEN_URL = "https://HOST/Relativity/Identity/connect/token"
EXPIRY_MARGIN_SECONDS = 60
REQUEST_TIMEOUT_SECONDS = 30

_cached_token = {"value": None, "expires_at": 0.0}


def get_access_token():
	now = time.monotonic()
	if _cached_token["value"] is None or _cached_token["expires_at"] - now < EXPIRY_MARGIN_SECONDS:
		response = requests.post(
			IDENTITY_TOKEN_URL,
			data={
				"grant_type": "client_credentials",
				"client_id": os.environ["RELATIVITY_CLIENT_ID"],
				"client_secret": os.environ["RELATIVITY_CLIENT_SECRET"],
			},
			timeout=REQUEST_TIMEOUT_SECONDS,
		)
		response.raise_for_status()
		payload = response.json()
		_cached_token["value"] = payload["access_token"]
		_cached_token["expires_at"] = now + payload["expires_in"]
	return _cached_token["value"]
```

`requests` form-encodes a dictionary passed as `data`, and `raise_for_status` raises on a 4XX or 5XX response. **The same token request in the other three authorized clients** (the cache and the credentials are as above):

```python
HTTP_OK = 200
token_request_fields = {
	"grant_type": "client_credentials",
	"client_id": os.environ["RELATIVITY_CLIENT_ID"],
	"client_secret": os.environ["RELATIVITY_CLIENT_SECRET"],
}

# httpx: a dictionary passed as data is form-encoded
response = httpx.post(IDENTITY_TOKEN_URL, data=token_request_fields, timeout=REQUEST_TIMEOUT_SECONDS)
response.raise_for_status()
payload = response.json()

# aiohttp: data= is form-encoded
async with session.post(IDENTITY_TOKEN_URL, data=token_request_fields) as response:
	response.raise_for_status()
	payload = await response.json()

# urllib3: PoolManager.request form-encodes the fields argument for POST only with encode_multipart=False
response = urllib3.PoolManager().request("POST", IDENTITY_TOKEN_URL, fields=token_request_fields, encode_multipart=False)
if response.status != HTTP_OK:
	raise RuntimeError(f"The token request failed with status {response.status}")
payload = response.json()
```

**Cache the token and re-acquire it on expiry — do not refresh.** Request a new token with the client secret shortly before the current one expires (the block uses a margin of 60 seconds), and once when a call is rejected for authentication. Client Credentials does not offer refresh tokens: **verified by NSerio against a live instance; not documented by Relativity** — the Allow Refresh Tokens control does not appear for that grant in the RelativityOne user interface. The recommended Access Token Lifetime is 60 minutes. **The client secret** belongs in the process environment or a secret store, as Secret Management (Part B of the Migration Documentation) describes, **never in source or in a tracked file**. A reset invalidates the previous secret immediately, so plan the cut-over (the two-client pattern is inferred). **Before deploying, confirm the calling identity:** every call now runs as the OAuth2 Client's Context User, not as the user in the replaced credential, and the non-administrator Context User is NSerio's recommendation. Trusted IPs restrict where a token can be requested, not a token already issued. **Whether `httpx` and `urllib3` accept a dictionary the way the pages read say is documented for the page's version only** (`httpx` shows no version): unchecked for other versions.

Verification: **Verification note.** **This match is Medium: the consumer of the address is not visible**, so the rule cannot say how it is sent. **A token has a finite lifetime.** **The calling identity changes.**

> Treat this credential as compromised. Rotate it, and assume the repository history has published it. Completing the migration does not close this exposure; rotation does.

OAuth2 Client: See [OAuth2 Client for Client Credentials](#oauth2-client-for-client-credentials).
<!-- generated:end PY-012 client-credentials -->

#### Authorization Code

<!-- generated:begin PY-012 authorization-code -->
**Flow.** This entry is for Authorization Code. The "Which flow" paragraph of PY-012 in [Part A](part-a-python.md#py-012) says when a match belongs to this flow.

Source form: **Applies to:** a Python script or service.

```python
BASE_URL = "https://user_name:secret@example.test/items"
```

Under token authentication: **First, the destination:** if this Relativity instance is not RelativityOne — including a Relativity Server instance, whose own documentation governs — no migration to RelativityOne applies and the rest of this entry does not apply.

```python
BASE_URL = "https://example.test/items"
CSRF_HEADER_VALUE = "-"
response = requests.get(BASE_URL, headers={"Authorization": f"Bearer {access_token}", "X-CSRF-Header": CSRF_HEADER_VALUE})
```

Remove the userinfo from the held address and apply the rotation statement below.

Credentials: **Where the credentials come from.** The source form establishes that a client sends Basic, not that the destination is RelativityOne; if it is not RelativityOne the migration is that service's own token mechanism, no migration to RelativityOne applies, and the reader stops here. If it is RelativityOne, the token comes from the token-acquisition part that follows, and the user name and password are replaced by the OAuth2 Client's client ID and secret, kept as Secret Management (Part B of the Migration Documentation) describes, never in source.

**Common to both variants.** The person signs in on RelativityOne's own page and the code never holds their password. The text has a common part and **two variants by application type**, each opening with the precondition it is for: a server-side web application and a command-line or desktop tool. Read the precondition of each and discard the variant that does not apply.

**Sign in.** Replace the code that collected the person's credentials with a redirect to the authorize endpoint `<host>/Relativity/Identity/connect/authorize` (**verified by NSerio against a live instance; not documented by Relativity**), which leads to RelativityOne's login page. The request carries the standard authorization-code parameters — `client_id`, `response_type=code`, the registered `redirect_uri`, `state` checked on return, and `code_challenge` with `code_challenge_method=S256` when PKCE is used; these are standard OAuth (RFC 6749 and RFC 7636) and Relativity documents none of them.

**PKCE.** PKCE is required by Relativity's own documentation to obtain a refresh token and not otherwise: a Code request that does not ask for a refresh token works without it, **verified by NSerio against a live instance; not documented by Relativity**. NSerio recommends `S256` (NSerio recommendation) and does not claim Relativity accepts any particular method. The verifier is 43 to 128 characters of the unreserved set and the `S256` challenge is the base64url of the SHA-256 of the verifier; `secrets.token_urlsafe(32)` gives 43 characters from that set (that the two match is arithmetic, not a tested fact) and `hashlib.sha256` and `base64.urlsafe_b64encode` compute the challenge. **`state`** is generated, stored against the session and compared on return; `nonce` is not used, and Relativity does not document whether it enforces either.

**The client secret is required.** Code does not work without a client secret: tested and rejected, **verified by NSerio against a live instance; not documented by Relativity**, which confirms Relativity's own statement. **A secret kept in a program that is distributed is extractable**, so the exchange runs where a secret can be kept.

**Exchange the code.** A form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=authorization_code`, the code, the same `redirect_uri`, the client ID and the secret, plus the `code_verifier` when PKCE is used (**verified by NSerio against a live instance; not documented by Relativity**).

**Redirect URL.** A server-side application registers **one exact HTTPS URL per environment, no wildcards** (the matching rules are not documented and no claim is made about them). A plain-HTTP loopback Redirect URL `http://127.0.0.1:<port>/<path>` on a **fixed registered port** is accepted, **verified by NSerio against a live instance; not documented by Relativity**, and is the command-line shape. RFC 8252 requires an authorization server to allow any port for a loopback redirect; **Relativity's behaviour is the fixed registered port, and the RFC's rule is not claimed for it.**

**On expiry.** With Allow Refresh Tokens enabled and PKCE used, the code holds a refresh token and sends `grant_type=refresh_token` in place of the code exchange: *inferred* from the standard OAuth shape, not tested; Relativity does not document a refresh token's lifetime, rotation, revocation or revocation endpoint, nor what disabling the client does to tokens already issued. **A refresh token is a secret.** **Single sign-on:** sign-in happens on RelativityOne's own page, so the instance's single sign-on and two-factor rules apply unchanged (*inferred*). **The code stops collecting the password:** after this change nothing receives, stores or forwards the person's user name and password, and the code holds only tokens. Every call now runs as the person who signed in, so **the calling identity changes**. **Scope:** no scope set is prescribed for Code; `openid RelativityRequestOrigin UserInfo UserInfoAccess AuthenticationID RelativityWeb` works, **verified by NSerio against a live instance; not documented by Relativity**, as working and not as minimal.

**Variant 1 — a server-side web application (Flask, Django, FastAPI or similar)**

**Precondition: this variant is for code that runs in Python as a web server and acts for people who sign in to it. It is not for a command-line tool (variant 2).**

Standard server-side Code flow. A login route builds the authorize address with `state` (and the PKCE verifier where a refresh token is wanted), stores them against the visitor's session and redirects; RelativityOne redirects to the **callback route**, registered as an exact HTTPS URL; the callback checks `state` and exchanges the code with the secret. **The logic is framework-neutral; the framework supplies the routes and the session**, and the sketch shows the callback as a handler of the standard library's `http.server` so that it has no dependency: in a Flask, Django or FastAPI application the same two functions are called from the login and callback views, and the dictionary of pending logins is the framework's session (Flask's session holds data in a signed cookie that the user can read but not modify, so **NSerio recommends that it hold neither the verifier nor a token**; Django's session framework "stores data on the server side" with the cookie holding only a session ID, "unless you're using the cookie based backend" (`SESSION_ENGINE` set to `django.contrib.sessions.backends.signed_cookies`), whose data "can be read by the client"; the pages of FastAPI read name no session store). **No framework code is shown: none is installed and none is verified.**

```python
import base64
import hashlib
import http.server
import secrets
from urllib.parse import parse_qs, urlencode, urlparse

import requests

IDENTITY_BASE = "https://HOST/Relativity/Identity/connect"
REDIRECT_URI = "https://app.example.test/auth/callback"
CALLBACK_PATH = "/auth/callback"
RANDOM_BYTES = 32
REQUEST_TIMEOUT_SECONDS = 30
HTTP_OK = 200
HTTP_NOT_FOUND = 404
_pending_by_session = {}


def base64_url(raw_bytes):
	return base64.urlsafe_b64encode(raw_bytes).rstrip(b"=").decode("ascii")


def begin_login(session_id, client_id, want_refresh_token):
	state = secrets.token_urlsafe(RANDOM_BYTES)
	pending = {"state": state, "verifier": None}
	query = {
		"response_type": "code",
		"client_id": client_id,
		"redirect_uri": REDIRECT_URI,
		"state": state,
	}
	if want_refresh_token:
		verifier = secrets.token_urlsafe(RANDOM_BYTES)
		pending["verifier"] = verifier
		query["code_challenge"] = base64_url(hashlib.sha256(verifier.encode("ascii")).digest())
		query["code_challenge_method"] = "S256"
	_pending_by_session[session_id] = pending
	return f"{IDENTITY_BASE}/authorize?{urlencode(query)}"


def complete_login(session_id, client_id, client_secret, returned_code, returned_state):
	pending = _pending_by_session.pop(session_id, None)
	tokens = None
	state_matches = (
		pending is not None
		and returned_state is not None
		and secrets.compare_digest(returned_state.encode("utf-8"), pending["state"].encode("utf-8"))
	)
	if state_matches:
		fields = {
			"grant_type": "authorization_code",
			"code": returned_code,
			"redirect_uri": REDIRECT_URI,
			"client_id": client_id,
			"client_secret": client_secret,
		}
		if pending["verifier"] is not None:
			fields["code_verifier"] = pending["verifier"]
		response = requests.post(f"{IDENTITY_BASE}/token", data=fields, timeout=REQUEST_TIMEOUT_SECONDS)
		tokens = response.json() if response.status_code == HTTP_OK else None
	return tokens


class CallbackHandler(http.server.BaseHTTPRequestHandler):
	def do_GET(self):
		address = urlparse(self.path)
		is_callback = address.path == CALLBACK_PATH
		self.send_response(HTTP_OK if is_callback else HTTP_NOT_FOUND)
		self.end_headers()
		if is_callback:
			query = parse_qs(address.query)
			self.wfile.write(b"Signed in.")
			self.server.returned_code = query.get("code", [None])[0]
			self.server.returned_state = query.get("state", [None])[0]
```

`state` is consumed once and compared on return in constant time (`secrets.compare_digest`); a request with no pending entry or a different `state` gets no exchange. **The scope is not prescribed** (the sketch sends none). **Where the secret travels in the token request** (the body, as sketched, or HTTP Basic client authentication) is **not verified**; RFC 6749 allows either form for a client password. The sketch does not show the HTTP server's TLS, the session cookie or a session store that survives more than one process (the module dictionary does not). **The handler only records the returned `code` and `state`; in a real application the callback view calls `complete_login` with the visitor's own session identifier**, so that the pending entry of that visitor, and no other, is consumed. **Tokens stay on the server**, in a session store that is not exposed to a request.

**The `http.server` handler is a shape, not a deployment.** The `http.server` page states that the module "is not recommended for production. It only implements basic security checks" (Python 3.15.2). The handler above shows what the callback reads and answers, so that the sketch has no dependency; **an application that serves people runs the callback as a view of the framework it already uses** (Flask, Django or FastAPI), behind the TLS and the session store that framework and its server provide.

**Variant 2 — a command-line or desktop tool**

**Precondition: this variant is for code that runs on a person's own machine, as a command-line tool or a desktop shell, and signs that person in. It is not for a web server (variant 1).**

The process starts a listener from the standard library, bound to **`127.0.0.1` only**, on the **fixed registered port**, for **one callback request**; opens the system browser on the authorize address **and always prints the address too**, so a machine with no browser, an SSH session or a failed launch has a manual route; reads `code` and `state` from the callback request, closes the listener, checks `state`, and exchanges the code as variant 1's `complete_login` does (with the redirect URI `http://127.0.0.1:8123/callback`).

```python
import http.server
import time
import webbrowser
from urllib.parse import parse_qs, urlparse

LOOPBACK_HOST = "127.0.0.1"
REGISTERED_PORT = 8123
CALLBACK_PATH = "/callback"
WAIT_SECONDS = 120
POLL_SECONDS = 1.0
HTTP_OK = 200
HTTP_NOT_FOUND = 404


class _RedirectHandler(http.server.BaseHTTPRequestHandler):
	received = None

	def do_GET(self):
		address = urlparse(self.path)
		is_callback = address.path == CALLBACK_PATH
		self.send_response(HTTP_OK if is_callback else HTTP_NOT_FOUND)
		self.end_headers()
		self.wfile.write(b"You can close this window." if is_callback else b"Not found")
		if is_callback:
			query = parse_qs(address.query)
			type(self).received = {
				"code": query.get("code", [None])[0],
				"state": query.get("state", [None])[0],
			}

	def log_message(self, message_format, *arguments):
		return None


class _LoopbackServer(http.server.HTTPServer):
	allow_reuse_address = False


def wait_for_redirect(authorize_address):
	_RedirectHandler.received = None
	print(f"Sign in at: {authorize_address}")
	try:
		server = _LoopbackServer((LOOPBACK_HOST, REGISTERED_PORT), _RedirectHandler)
	except OSError:
		print(f"Port {REGISTERED_PORT} is in use. Close the program that holds it and sign in again.")
	else:
		server.timeout = POLL_SECONDS
		webbrowser.open(authorize_address)
		deadline = time.monotonic() + WAIT_SECONDS
		while _RedirectHandler.received is None and time.monotonic() < deadline:
			server.handle_request()
		server.server_close()
	return _RedirectHandler.received
```

**Rules for the listener:** bind to loopback only; **check `state`** (another local process can connect to a fixed, advertised port); close the listener after the callback request; and handle a busy port. **A busy port raises `OSError` only if the listener does not reuse the address**: `socketserver` documents the class variable `allow_reuse_address` ("This defaults to False, and can be set in subclasses to change the policy"), and `http.server.HTTPServer` sets it to a true value, with which a second bind to the same address and port raised no error on Windows (checked on Windows; the behaviour on Linux and macOS is unchecked). The sketch therefore binds a subclass with `allow_reuse_address = False`, catches the `OSError`, and prints the address and a clear message instead of waiting. `handle_request` processes one request and returns after `timeout` seconds if none arrives, which is why the sketch loops until the callback or the **overall deadline** `WAIT_SECONDS` (`timeout` bounds each wait only, and the deadline bounds the whole wait): a stray request, such as a browser asking for an icon, is answered `404` and does not end the wait. `webbrowser.open` shows the address "using the default browser" and returns whether one was launched. **Where tokens are kept:** a file with restricted permissions or the operating system's keychain (unchecked on every system; no keychain package is named here). **The extractable-secret statement:** a command-line tool or desktop program shipped to people carries its client secret in a form anyone who holds the distribution can extract, and it protects nothing against the person running it (a documentation gap: Relativity documents no recommendation for a distributed client). **Compensating guidance** (NSerio recommendation): Trusted IPs where the users' outbound addresses are known and stable (checked at token issuance; applicability to Code *inferred*); the 60-minute Access Token Lifetime; refresh tokens only when needed; and **one OAuth2 Client per distribution**, so a leaked secret can be disabled and reset alone.

Verification: **Verification note.** **This match is Medium: the consumer of the address is not visible**, so the rule cannot say how it is sent. **A token has a finite lifetime.** **The calling identity changes.**

> Treat this credential as compromised. Rotate it, and assume the repository history has published it. Completing the migration does not close this exposure; rotation does.

OAuth2 Client: See [OAuth2 Client for Authorization Code](#oauth2-client-for-authorization-code).
<!-- generated:end PY-012 authorization-code -->

### PY-013 · AuthorizationHeaderValueHeldInVariable

<a id="py-013-migration"></a>

Remove the variable that holds the `Basic` value and the code that built the pair, and send a Bearer token in the `Authorization` header instead. The name resolves to a value that begins with `Basic`; a token written after the word in the declaration is a credential in code and is a secret to remove.

#### Client Credentials

<!-- generated:begin PY-013 client-credentials -->
**Flow.** This entry is for Client Credentials. The "Which flow" paragraph of PY-013 in [Part A](part-a-python.md#py-013) says when a match belongs to this flow.

Source form: **Applies to:** a Python script, service or tool.

```python
scheme_value = "Basic " + encoded_pair
response = requests.get(url, headers={"Authorization": scheme_value})
```

Under token authentication: **First, the destination:** if this Relativity instance is not RelativityOne — including a Relativity Server instance, whose own documentation governs — no migration to RelativityOne applies and the rest of this entry does not apply.

```python
CSRF_HEADER_VALUE = "-"
response = requests.get(url, headers={"Authorization": f"Bearer {access_token}", "X-CSRF-Header": CSRF_HEADER_VALUE})
```

Remove the variable and the code that built the pair.

Credentials: **Where the credentials come from.** The source form establishes that a client sends Basic, not that the destination is RelativityOne; if it is not RelativityOne the migration is that service's own token mechanism, no migration to RelativityOne applies, and the reader stops here. If it is RelativityOne, the token comes from the token-acquisition part that follows, and the user name and password are replaced by the OAuth2 Client's client ID and secret, kept as Secret Management (Part B of the Migration Documentation) describes, never in source.

The text a match assigned Client Credentials receives after part 1. Create an OAuth2 Client with Flow Grant Type set to Client Credentials (part 3 lists its settings), then send a form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=client_credentials`, `client_id` and `client_secret`. The token is sent as `Authorization: Bearer <token>`, with `X-CSRF-Header: -` on REST and Kepler calls. **The endpoint is documented for this grant.** The scope depends on what the integration needs and **no single scope is required**: Relativity's documentation shows `SystemUserInfo` and `UserInfoAccess` and no page says which is correct or what either grants; use the value the API you call documents, and confirm it.

**Variant: a Python process acting as itself**

**Precondition: this variant is for a Python job, service or script that does not act for a signed-in person. If a person signs in, use Authorization Code.**

```python
import os
import time

import requests

IDENTITY_TOKEN_URL = "https://HOST/Relativity/Identity/connect/token"
EXPIRY_MARGIN_SECONDS = 60
REQUEST_TIMEOUT_SECONDS = 30

_cached_token = {"value": None, "expires_at": 0.0}


def get_access_token():
	now = time.monotonic()
	if _cached_token["value"] is None or _cached_token["expires_at"] - now < EXPIRY_MARGIN_SECONDS:
		response = requests.post(
			IDENTITY_TOKEN_URL,
			data={
				"grant_type": "client_credentials",
				"client_id": os.environ["RELATIVITY_CLIENT_ID"],
				"client_secret": os.environ["RELATIVITY_CLIENT_SECRET"],
			},
			timeout=REQUEST_TIMEOUT_SECONDS,
		)
		response.raise_for_status()
		payload = response.json()
		_cached_token["value"] = payload["access_token"]
		_cached_token["expires_at"] = now + payload["expires_in"]
	return _cached_token["value"]
```

`requests` form-encodes a dictionary passed as `data`, and `raise_for_status` raises on a 4XX or 5XX response. **The same token request in the other three authorized clients** (the cache and the credentials are as above):

```python
HTTP_OK = 200
token_request_fields = {
	"grant_type": "client_credentials",
	"client_id": os.environ["RELATIVITY_CLIENT_ID"],
	"client_secret": os.environ["RELATIVITY_CLIENT_SECRET"],
}

# httpx: a dictionary passed as data is form-encoded
response = httpx.post(IDENTITY_TOKEN_URL, data=token_request_fields, timeout=REQUEST_TIMEOUT_SECONDS)
response.raise_for_status()
payload = response.json()

# aiohttp: data= is form-encoded
async with session.post(IDENTITY_TOKEN_URL, data=token_request_fields) as response:
	response.raise_for_status()
	payload = await response.json()

# urllib3: PoolManager.request form-encodes the fields argument for POST only with encode_multipart=False
response = urllib3.PoolManager().request("POST", IDENTITY_TOKEN_URL, fields=token_request_fields, encode_multipart=False)
if response.status != HTTP_OK:
	raise RuntimeError(f"The token request failed with status {response.status}")
payload = response.json()
```

**Cache the token and re-acquire it on expiry — do not refresh.** Request a new token with the client secret shortly before the current one expires (the block uses a margin of 60 seconds), and once when a call is rejected for authentication. Client Credentials does not offer refresh tokens: **verified by NSerio against a live instance; not documented by Relativity** — the Allow Refresh Tokens control does not appear for that grant in the RelativityOne user interface. The recommended Access Token Lifetime is 60 minutes. **The client secret** belongs in the process environment or a secret store, as Secret Management (Part B of the Migration Documentation) describes, **never in source or in a tracked file**. A reset invalidates the previous secret immediately, so plan the cut-over (the two-client pattern is inferred). **Before deploying, confirm the calling identity:** every call now runs as the OAuth2 Client's Context User, not as the user in the replaced credential, and the non-administrator Context User is NSerio's recommendation. Trusted IPs restrict where a token can be requested, not a token already issued. **Whether `httpx` and `urllib3` accept a dictionary the way the pages read say is documented for the page's version only** (`httpx` shows no version): unchecked for other versions.

Verification: **Verification note.** **The rule resolved the header's variable to a scheme of `Basic` in this file** (one declaration, before the use); a variable it cannot resolve is not read. **A token has a finite lifetime.** **The calling identity changes.**

OAuth2 Client: See [OAuth2 Client for Client Credentials](#oauth2-client-for-client-credentials).
<!-- generated:end PY-013 client-credentials -->

#### Authorization Code

<!-- generated:begin PY-013 authorization-code -->
**Flow.** This entry is for Authorization Code. The "Which flow" paragraph of PY-013 in [Part A](part-a-python.md#py-013) says when a match belongs to this flow.

Source form: **Applies to:** a Python script, service or tool.

```python
scheme_value = "Basic " + encoded_pair
response = requests.get(url, headers={"Authorization": scheme_value})
```

Under token authentication: **First, the destination:** if this Relativity instance is not RelativityOne — including a Relativity Server instance, whose own documentation governs — no migration to RelativityOne applies and the rest of this entry does not apply.

```python
CSRF_HEADER_VALUE = "-"
response = requests.get(url, headers={"Authorization": f"Bearer {access_token}", "X-CSRF-Header": CSRF_HEADER_VALUE})
```

Remove the variable and the code that built the pair.

Credentials: **Where the credentials come from.** The source form establishes that a client sends Basic, not that the destination is RelativityOne; if it is not RelativityOne the migration is that service's own token mechanism, no migration to RelativityOne applies, and the reader stops here. If it is RelativityOne, the token comes from the token-acquisition part that follows, and the user name and password are replaced by the OAuth2 Client's client ID and secret, kept as Secret Management (Part B of the Migration Documentation) describes, never in source.

**Common to both variants.** The person signs in on RelativityOne's own page and the code never holds their password. The text has a common part and **two variants by application type**, each opening with the precondition it is for: a server-side web application and a command-line or desktop tool. Read the precondition of each and discard the variant that does not apply.

**Sign in.** Replace the code that collected the person's credentials with a redirect to the authorize endpoint `<host>/Relativity/Identity/connect/authorize` (**verified by NSerio against a live instance; not documented by Relativity**), which leads to RelativityOne's login page. The request carries the standard authorization-code parameters — `client_id`, `response_type=code`, the registered `redirect_uri`, `state` checked on return, and `code_challenge` with `code_challenge_method=S256` when PKCE is used; these are standard OAuth (RFC 6749 and RFC 7636) and Relativity documents none of them.

**PKCE.** PKCE is required by Relativity's own documentation to obtain a refresh token and not otherwise: a Code request that does not ask for a refresh token works without it, **verified by NSerio against a live instance; not documented by Relativity**. NSerio recommends `S256` (NSerio recommendation) and does not claim Relativity accepts any particular method. The verifier is 43 to 128 characters of the unreserved set and the `S256` challenge is the base64url of the SHA-256 of the verifier; `secrets.token_urlsafe(32)` gives 43 characters from that set (that the two match is arithmetic, not a tested fact) and `hashlib.sha256` and `base64.urlsafe_b64encode` compute the challenge. **`state`** is generated, stored against the session and compared on return; `nonce` is not used, and Relativity does not document whether it enforces either.

**The client secret is required.** Code does not work without a client secret: tested and rejected, **verified by NSerio against a live instance; not documented by Relativity**, which confirms Relativity's own statement. **A secret kept in a program that is distributed is extractable**, so the exchange runs where a secret can be kept.

**Exchange the code.** A form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=authorization_code`, the code, the same `redirect_uri`, the client ID and the secret, plus the `code_verifier` when PKCE is used (**verified by NSerio against a live instance; not documented by Relativity**).

**Redirect URL.** A server-side application registers **one exact HTTPS URL per environment, no wildcards** (the matching rules are not documented and no claim is made about them). A plain-HTTP loopback Redirect URL `http://127.0.0.1:<port>/<path>` on a **fixed registered port** is accepted, **verified by NSerio against a live instance; not documented by Relativity**, and is the command-line shape. RFC 8252 requires an authorization server to allow any port for a loopback redirect; **Relativity's behaviour is the fixed registered port, and the RFC's rule is not claimed for it.**

**On expiry.** With Allow Refresh Tokens enabled and PKCE used, the code holds a refresh token and sends `grant_type=refresh_token` in place of the code exchange: *inferred* from the standard OAuth shape, not tested; Relativity does not document a refresh token's lifetime, rotation, revocation or revocation endpoint, nor what disabling the client does to tokens already issued. **A refresh token is a secret.** **Single sign-on:** sign-in happens on RelativityOne's own page, so the instance's single sign-on and two-factor rules apply unchanged (*inferred*). **The code stops collecting the password:** after this change nothing receives, stores or forwards the person's user name and password, and the code holds only tokens. Every call now runs as the person who signed in, so **the calling identity changes**. **Scope:** no scope set is prescribed for Code; `openid RelativityRequestOrigin UserInfo UserInfoAccess AuthenticationID RelativityWeb` works, **verified by NSerio against a live instance; not documented by Relativity**, as working and not as minimal.

**Variant 1 — a server-side web application (Flask, Django, FastAPI or similar)**

**Precondition: this variant is for code that runs in Python as a web server and acts for people who sign in to it. It is not for a command-line tool (variant 2).**

Standard server-side Code flow. A login route builds the authorize address with `state` (and the PKCE verifier where a refresh token is wanted), stores them against the visitor's session and redirects; RelativityOne redirects to the **callback route**, registered as an exact HTTPS URL; the callback checks `state` and exchanges the code with the secret. **The logic is framework-neutral; the framework supplies the routes and the session**, and the sketch shows the callback as a handler of the standard library's `http.server` so that it has no dependency: in a Flask, Django or FastAPI application the same two functions are called from the login and callback views, and the dictionary of pending logins is the framework's session (Flask's session holds data in a signed cookie that the user can read but not modify, so **NSerio recommends that it hold neither the verifier nor a token**; Django's session framework "stores data on the server side" with the cookie holding only a session ID, "unless you're using the cookie based backend" (`SESSION_ENGINE` set to `django.contrib.sessions.backends.signed_cookies`), whose data "can be read by the client"; the pages of FastAPI read name no session store). **No framework code is shown: none is installed and none is verified.**

```python
import base64
import hashlib
import http.server
import secrets
from urllib.parse import parse_qs, urlencode, urlparse

import requests

IDENTITY_BASE = "https://HOST/Relativity/Identity/connect"
REDIRECT_URI = "https://app.example.test/auth/callback"
CALLBACK_PATH = "/auth/callback"
RANDOM_BYTES = 32
REQUEST_TIMEOUT_SECONDS = 30
HTTP_OK = 200
HTTP_NOT_FOUND = 404
_pending_by_session = {}


def base64_url(raw_bytes):
	return base64.urlsafe_b64encode(raw_bytes).rstrip(b"=").decode("ascii")


def begin_login(session_id, client_id, want_refresh_token):
	state = secrets.token_urlsafe(RANDOM_BYTES)
	pending = {"state": state, "verifier": None}
	query = {
		"response_type": "code",
		"client_id": client_id,
		"redirect_uri": REDIRECT_URI,
		"state": state,
	}
	if want_refresh_token:
		verifier = secrets.token_urlsafe(RANDOM_BYTES)
		pending["verifier"] = verifier
		query["code_challenge"] = base64_url(hashlib.sha256(verifier.encode("ascii")).digest())
		query["code_challenge_method"] = "S256"
	_pending_by_session[session_id] = pending
	return f"{IDENTITY_BASE}/authorize?{urlencode(query)}"


def complete_login(session_id, client_id, client_secret, returned_code, returned_state):
	pending = _pending_by_session.pop(session_id, None)
	tokens = None
	state_matches = (
		pending is not None
		and returned_state is not None
		and secrets.compare_digest(returned_state.encode("utf-8"), pending["state"].encode("utf-8"))
	)
	if state_matches:
		fields = {
			"grant_type": "authorization_code",
			"code": returned_code,
			"redirect_uri": REDIRECT_URI,
			"client_id": client_id,
			"client_secret": client_secret,
		}
		if pending["verifier"] is not None:
			fields["code_verifier"] = pending["verifier"]
		response = requests.post(f"{IDENTITY_BASE}/token", data=fields, timeout=REQUEST_TIMEOUT_SECONDS)
		tokens = response.json() if response.status_code == HTTP_OK else None
	return tokens


class CallbackHandler(http.server.BaseHTTPRequestHandler):
	def do_GET(self):
		address = urlparse(self.path)
		is_callback = address.path == CALLBACK_PATH
		self.send_response(HTTP_OK if is_callback else HTTP_NOT_FOUND)
		self.end_headers()
		if is_callback:
			query = parse_qs(address.query)
			self.wfile.write(b"Signed in.")
			self.server.returned_code = query.get("code", [None])[0]
			self.server.returned_state = query.get("state", [None])[0]
```

`state` is consumed once and compared on return in constant time (`secrets.compare_digest`); a request with no pending entry or a different `state` gets no exchange. **The scope is not prescribed** (the sketch sends none). **Where the secret travels in the token request** (the body, as sketched, or HTTP Basic client authentication) is **not verified**; RFC 6749 allows either form for a client password. The sketch does not show the HTTP server's TLS, the session cookie or a session store that survives more than one process (the module dictionary does not). **The handler only records the returned `code` and `state`; in a real application the callback view calls `complete_login` with the visitor's own session identifier**, so that the pending entry of that visitor, and no other, is consumed. **Tokens stay on the server**, in a session store that is not exposed to a request.

**The `http.server` handler is a shape, not a deployment.** The `http.server` page states that the module "is not recommended for production. It only implements basic security checks" (Python 3.15.2). The handler above shows what the callback reads and answers, so that the sketch has no dependency; **an application that serves people runs the callback as a view of the framework it already uses** (Flask, Django or FastAPI), behind the TLS and the session store that framework and its server provide.

**Variant 2 — a command-line or desktop tool**

**Precondition: this variant is for code that runs on a person's own machine, as a command-line tool or a desktop shell, and signs that person in. It is not for a web server (variant 1).**

The process starts a listener from the standard library, bound to **`127.0.0.1` only**, on the **fixed registered port**, for **one callback request**; opens the system browser on the authorize address **and always prints the address too**, so a machine with no browser, an SSH session or a failed launch has a manual route; reads `code` and `state` from the callback request, closes the listener, checks `state`, and exchanges the code as variant 1's `complete_login` does (with the redirect URI `http://127.0.0.1:8123/callback`).

```python
import http.server
import time
import webbrowser
from urllib.parse import parse_qs, urlparse

LOOPBACK_HOST = "127.0.0.1"
REGISTERED_PORT = 8123
CALLBACK_PATH = "/callback"
WAIT_SECONDS = 120
POLL_SECONDS = 1.0
HTTP_OK = 200
HTTP_NOT_FOUND = 404


class _RedirectHandler(http.server.BaseHTTPRequestHandler):
	received = None

	def do_GET(self):
		address = urlparse(self.path)
		is_callback = address.path == CALLBACK_PATH
		self.send_response(HTTP_OK if is_callback else HTTP_NOT_FOUND)
		self.end_headers()
		self.wfile.write(b"You can close this window." if is_callback else b"Not found")
		if is_callback:
			query = parse_qs(address.query)
			type(self).received = {
				"code": query.get("code", [None])[0],
				"state": query.get("state", [None])[0],
			}

	def log_message(self, message_format, *arguments):
		return None


class _LoopbackServer(http.server.HTTPServer):
	allow_reuse_address = False


def wait_for_redirect(authorize_address):
	_RedirectHandler.received = None
	print(f"Sign in at: {authorize_address}")
	try:
		server = _LoopbackServer((LOOPBACK_HOST, REGISTERED_PORT), _RedirectHandler)
	except OSError:
		print(f"Port {REGISTERED_PORT} is in use. Close the program that holds it and sign in again.")
	else:
		server.timeout = POLL_SECONDS
		webbrowser.open(authorize_address)
		deadline = time.monotonic() + WAIT_SECONDS
		while _RedirectHandler.received is None and time.monotonic() < deadline:
			server.handle_request()
		server.server_close()
	return _RedirectHandler.received
```

**Rules for the listener:** bind to loopback only; **check `state`** (another local process can connect to a fixed, advertised port); close the listener after the callback request; and handle a busy port. **A busy port raises `OSError` only if the listener does not reuse the address**: `socketserver` documents the class variable `allow_reuse_address` ("This defaults to False, and can be set in subclasses to change the policy"), and `http.server.HTTPServer` sets it to a true value, with which a second bind to the same address and port raised no error on Windows (checked on Windows; the behaviour on Linux and macOS is unchecked). The sketch therefore binds a subclass with `allow_reuse_address = False`, catches the `OSError`, and prints the address and a clear message instead of waiting. `handle_request` processes one request and returns after `timeout` seconds if none arrives, which is why the sketch loops until the callback or the **overall deadline** `WAIT_SECONDS` (`timeout` bounds each wait only, and the deadline bounds the whole wait): a stray request, such as a browser asking for an icon, is answered `404` and does not end the wait. `webbrowser.open` shows the address "using the default browser" and returns whether one was launched. **Where tokens are kept:** a file with restricted permissions or the operating system's keychain (unchecked on every system; no keychain package is named here). **The extractable-secret statement:** a command-line tool or desktop program shipped to people carries its client secret in a form anyone who holds the distribution can extract, and it protects nothing against the person running it (a documentation gap: Relativity documents no recommendation for a distributed client). **Compensating guidance** (NSerio recommendation): Trusted IPs where the users' outbound addresses are known and stable (checked at token issuance; applicability to Code *inferred*); the 60-minute Access Token Lifetime; refresh tokens only when needed; and **one OAuth2 Client per distribution**, so a leaked secret can be disabled and reset alone.

Verification: **Verification note.** **The rule resolved the header's variable to a scheme of `Basic` in this file** (one declaration, before the use); a variable it cannot resolve is not read. **A token has a finite lifetime.** **The calling identity changes.**

OAuth2 Client: See [OAuth2 Client for Authorization Code](#oauth2-client-for-authorization-code).
<!-- generated:end PY-013 authorization-code -->

## Generic suggestion

The generic suggestion is for a High or Medium finding of a construct that has no entry of its own. In Python every class that can reach High or Medium has its own entries above, so the generic suggestion is a fallback only. It is a pointer and not an instruction: the migration must be designed from the code by whoever owns it.

### Client Credentials

<!-- generated:begin generic client-credentials -->
**Flow.** This entry is for Client Credentials.

Basic Authentication is found at this location but the construct is not one that this text describes, so the change has not been characterized and no before-and-after form is offered here. If the destination is not RelativityOne, the migration is that service's own and no migration to RelativityOne applies (that Relativity's identity endpoint issues no tokens for another service is unverified). If the destination is RelativityOne, Relativity's documentation recommends bearer tokens and states that Basic Authentication is no longer supported for named APIs, with no date for its removal asserted (the platform's direction beyond that is NSerio's reading, unverified). The documented destination in Python is a bearer Authorization header sent by an HTTP client such as requests, httpx, aiohttp, urllib3 or the standard library's urllib.request, carrying a token obtained from the platform's identity endpoint with the client-credentials grant, from a Python job, service or script. The client ID and secret that obtain that token belong in the process environment or a secret store, per Secret Management (Part B of the Migration Documentation), and never in source. Beyond those facts this is a pointer and not an instruction: the migration must be designed from the code by whoever owns it, rather than applied from this text.

OAuth2 Client: See [OAuth2 Client for Client Credentials](#oauth2-client-for-client-credentials).
<!-- generated:end generic client-credentials -->

### Authorization Code

<!-- generated:begin generic authorization-code -->
**Flow.** This entry is for Authorization Code.

Basic Authentication is found at this location but the construct is not one that this text describes, so the change has not been characterized and no before-and-after form is offered here. If the destination is not RelativityOne, the migration is that service's own and no migration to RelativityOne applies (that Relativity's identity endpoint issues no tokens for another service is unverified). If the destination is RelativityOne, Relativity's documentation recommends bearer tokens and states that Basic Authentication is no longer supported for named APIs, with no date for its removal asserted (the platform's direction beyond that is NSerio's reading, unverified), and this match is assigned the Authorization Code flow, in which the code sends the person to RelativityOne to sign in, receives an authorization code at a Redirect URL registered on its OAuth2 Client, and exchanges the code for an access token that it sends as a Bearer token. Code requires a client secret, so the exchange runs where a secret can be kept: for a Python web server the callback route performs it and keeps the tokens on the server; for a command-line tool or a desktop program a loopback listener on the registered fixed port receives the code. The authorize endpoint is <host>/Relativity/Identity/connect/authorize and the code is exchanged at <host>/Relativity/Identity/connect/token; both are verified by NSerio against a live instance; not documented by Relativity. PKCE is required by Relativity's own documentation to obtain a refresh token, and not otherwise, which is also verified by NSerio against a live instance; not documented by Relativity. The client ID and secret belong in the process environment or a secret store, per Secret Management (Part B of the Migration Documentation), never in source. After the change the code no longer collects or forwards the person's password. Beyond those facts this is a pointer and not an instruction: the migration must be designed from the code by whoever owns it, rather than applied from this text.

OAuth2 Client: See [OAuth2 Client for Authorization Code](#oauth2-client-for-authorization-code).
<!-- generated:end generic authorization-code -->

## OAuth2 Client for Client Credentials

Create or confirm this OAuth2 Client in RelativityOne for every Client Credentials entry above. Each of those entries links here instead of repeating the settings.

<!-- generated:begin oauth2-client client-credentials -->
Create or confirm this OAuth2 Client in RelativityOne (NSerio's observation: the menu path is Home > Authentication > OAuth2 Client Manager, as Relativity's REST API authentication page names it). These settings are guidance; create or change the OAuth2 Client in RelativityOne yourself.
- Name: a descriptive name identifying the integration it serves. Use one OAuth2 Client per integration and do not share it across integrations.
- Flow Grant Type: Client Credentials
- Context User: NSerio's recommendation: a dedicated integration user with the minimum permissions the integration needs, and not a personal account and not an administrator unless a specific API the integration calls requires it, because every call runs with that user's permissions and an administrator Context User gives the integration an administrator's reach. Relativity documentation (REST API authentication page): describes the Context User as usually a member of the Relativity Administrators group, though it could differ depending on the API or operation invoked; NSerio recommends against that default for an integration.
- Access Token Lifetime: 60 minutes, stated in minutes because the field is in minutes. Relativity documentation: tokens for this flow must have a short lifetime, and the recommended value matches the Identity Server default.
- Trusted IPs: Relativity documentation: the address list is checked whenever a token is requested with the OAuth2 Client's ID and secret. NSerio's recommendation: populate it with the integration's outbound IP addresses, one per line, wherever they are known and stable. It restricts where a token can be requested, checked when the token is issued; it does not restrict a token already issued, which travels in the request header and is valid from anywhere until it expires.
- Allow refresh tokens: not used by this flow. The integration requests a new access token when the current one expires.
- Enabled: on.
- Every call made with this OAuth2 Client runs with the Context User's permissions.
- Relativity documentation: the flow is fixed when the OAuth2 Client is created and cannot be changed when editing it, so moving this integration to Authorization Code later means creating a new OAuth2 Client.
<!-- generated:end oauth2-client client-credentials -->

## OAuth2 Client for Authorization Code

Create or confirm this OAuth2 Client in RelativityOne for every Authorization Code entry above. Each of those entries links here instead of repeating the settings.

<!-- generated:begin oauth2-client authorization-code -->
Create or confirm this OAuth2 Client in RelativityOne (NSerio's observation: the menu path is Home > Authentication > OAuth2 Client Manager, as Relativity's REST API authentication page names it). These settings are guidance; create or change the OAuth2 Client in RelativityOne yourself.
- Name: a descriptive name identifying the application it serves.
- Flow Grant Type: Code
- Redirect URLs: Relativity documentation: each URL includes the http or https protocol prefix. The exact callback URL is registered by the Relativity administrator. Server-side web applications, NSerio's recommendation: one exact HTTPS URL per environment, with no wildcards. Desktop and CLI applications: a plain-HTTP loopback URL, `http://127.0.0.1:<port>/<path>`, on a fixed port because the URL is registered in advance and the port cannot be chosen at runtime (verified by NSerio against a live instance; not documented by Relativity).
- Access Token Lifetime: 60 minutes, stated in minutes because the field is in minutes. Relativity documentation: tokens for this flow must have a short lifetime, and the recommended value matches the Identity Server default.
- Allow refresh tokens: NSerio's recommendation: leave it off unless the application must keep a user session active beyond the access token lifetime without asking the user to sign in again. If it is on, the refresh tokens are stored as secrets, per Secret Management (Part B of the Migration Documentation).
- Enabled: on.
<!-- generated:end oauth2-client authorization-code -->
