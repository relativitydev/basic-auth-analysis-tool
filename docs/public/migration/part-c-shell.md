# Migration Suggestions for Shell (Part C of the Migration Documentation)

This is Part C of the Migration Documentation, for developers who have found Basic Authentication in their Shell scripts with [Part A](part-a-shell.md). You use it without running anything: for each pattern class and each OAuth 2.0 flow it shows the code to replace, the code that replaces it, how to obtain and cache the token, and the OAuth2 Client to create. Store the new secrets as [Part B](part-b-secret-management.md) describes. New here? Start with the [Migration Documentation overview](README.md).

This document has three kinds of text:

- **Judging guidance.** How to judge a match that Part A rates Low confidence.
- **Recommendations.** Each of the 18 pattern classes that produce a recommendation (18 of the 25) has two entries, one for each flow, and each flow has one generic recommendation. The OAuth2 Client settings are in the two "OAuth2 Client for ..." sections at the end of this document, and each entry links to the section of its flow.
- **Pointers.** Part A ([`part-a-shell.md`](part-a-shell.md)) describes each pattern and how to search for it. Secret Management (Part B of the Migration Documentation, [`part-b-secret-management.md`](part-b-secret-management.md)) says where the new secrets go.

Every example uses placeholders such as `${HOST}`, `${CLIENT_ID}` and `${CLIENT_SECRET_FILE}`. Replace them with your own values, and keep real secrets out of scripts.

The two flows are the only ones recommended here:

- **Client Credentials** is the flow when no person signs in. The script sends its client ID and client secret to the token endpoint and receives an access token. No user is involved, and every call runs as the OAuth2 Client's Context User.
- **Authorization Code** is called "Code" in RelativityOne's OAuth2 Client form. The script sends a signed-in user to RelativityOne, receives an authorization code at a registered Redirect URL, and exchanges it for a token that carries that user's permissions. A shell script has no listener of its own, so the simplest way to receive the code is for the person to copy the address the browser ends on after signing in and paste it to the script.

A shell script has no SDK, and Relativity publishes no shell documentation: a script reaches RelativityOne through its REST endpoints with an HTTP client such as `curl`. Where the entries show code, a block is fenced `sh` when it runs in every dialect covered here (`bash`, `zsh`, `ksh93` and `mksh`; a script that names `sh` is read as `bash`) and `bash` when it needs `bash`, as arrays and `read -s` do, and each entry says which dialects it applies to. A token is sent as `Authorization: Bearer`, with `X-CSRF-Header: -` on REST and Kepler calls.

**Choosing the flow.** The "Which flow" paragraph of each class in Part A says when a match belongs to each flow. If your RelativityOne instance already has an OAuth2 Client for this integration, use the flow of that client. The flow is fixed when an OAuth2 Client is created and cannot be changed when the client is edited ([Relativity's OAuth2 clients help page](https://help.relativity.com/RelativityOne/Content/Relativity/Authentication/OAuth2_clients.htm)), so moving an integration from one flow to the other means creating a new OAuth2 Client.

## Judging a Low-confidence match

A match is **Low** confidence when it is present in the text but unlikely to be live authenticating code. A Low match has no flow and no recommendation, and is left for manual review. **A Low level is not evidence that the code does not authenticate.** It means a person has to look. The paragraphs below say how to judge each cause and when the match still needs a migration. When it does, find the live construct it belongs to, identify its class in Part A, and follow that class's entry in [Migration suggestions by pattern class](#migration-suggestions-by-pattern-class). Whatever the cause, a literal secret inside a documentation string, a comment or a test is still a secret to remove.

### Comments

The match is inside a `#` comment, so no code runs. Read the comment. If it is commented-out code, nothing needs migrating today, but delete it or migrate it before anyone restores it, because a restored line brings Basic Authentication back. If it describes code that exists elsewhere, search for that code with Part A and migrate the live code. If it only illustrates a pattern with placeholders, no migration is needed.

### Documentation strings

The match is inside text that documents or explains, such as a usage message, an error message, a here-document that holds a sample or a help text, or an `echo` that shows an example command. The text does not authenticate anything. Check that the behaviour it describes has been migrated, and update the wording afterwards so that the documentation stops describing Basic Authentication. The text itself needs no migration. One case is different: a command inside a quoted string or a script the shell runs, in a form that is not read as code (a string given to a prefix wrapper such as `nohup`, the earlier stage of a pipeline, a quoted command word; Part A lists them under Shell run from a string), is also rated Low because it counts as text, and it may be live code. Read such a match as a command before you dismiss it.

### Test fixtures and mocks

**Test code is not Low.** A match in test code is rated **Medium** at most, with the recommendation of its class. Test code is recognized by a folder named `test`, `tests`, `spec`, `fixture` or `fixtures`, by a file name that begins with `test_` or `test-` or ends in `_test`, `-test`, `.test` or `_spec`, and by a script that sources a shell unit-test library or holds a `Describe` block with an `It` block. Judge by what the test talks to. If it feeds a mock and never reaches RelativityOne, no migration is needed, though you may want the mock to model the token flow so the test still represents the script it covers. If it is an integration test that authenticates to a real RelativityOne instance, it needs migrating with everything else, because it stops working when Basic Authentication is switched off: follow the entry of its class. A credential written literally into a test is still a secret to remove.

**A mock is Low.** A match in a mock, fake or stub file is rated Low, with no recommendation: a folder named `mock`, `mocks`, `stub`, `stubs`, `fake` or `fakes`, or a file name that begins with `mock_`, `fake_` or `stub_` or ends in `_mock`, `_fake` or `_stub`. A mock is recognized by its folder or file name and by nothing else, so a mock that has a neutral name is rated at the level of its class, or at Medium when it also sits in a test folder; review it as you would a test. A file that is test code and also a mock, a sample, vendored or generated is Low. For a Low mock, judge by what it stands in for. If nothing runs it against RelativityOne, no migration is needed.

### Sample and example code

The match sits in a folder named `sample`, `samples`, `example` or `examples`, in any letter case, or in a file whose name begins with `sample` or `example`. Those are the only names that count: a sample kept in a folder called `demo`, or in a file whose name merely contains the word, is not rated Low and keeps the level of its class, so review it as live code. Judge by who uses it. If nobody runs the sample, it needs no migration. If people copy it as a starting point, migrate it: an example that teaches Basic Authentication spreads it to new scripts. Use the recommendation for the class the sample matches, and prefer the flow that fits the scenario the sample shows.

### Vendored and generated files

The match sits in a file produced by a tool (a first or second line, or a line within the first ten, that says the file is generated or must not be edited) or in a folder named `vendor`, `third-party` or `third_party`, which holds third-party code. **Do not edit a generated file by hand:** the next generation overwrites the edit. Fix the source it is generated from (the template, the schema or the generator settings) and regenerate. For vendored code, upgrade the script or the library to a version that supports token authentication, or replace it. A migration is needed when the file runs against RelativityOne in your environment. It is not needed when the file is never called, or when it is only reference material.

### Deployment folders

A folder named `Deployment` does not lower a Shell finding: in Shell that folder holds deployment scripts that need migrating as much as any other, so a script under it keeps the level of its class and this cause does not arise for it. If a match in a deployment folder is Low all the same, read the file before you decide, because a deployment folder is also where an installer or a release script that talks to RelativityOne is kept. A migration is needed when the file is the one that runs against RelativityOne in your application or in the deployment itself. It is not needed when the file is never run, or when it only illustrates a pattern. Judge by who runs it and against which server: a deployment script that signs in to RelativityOne with a user name and a password stops working when Basic Authentication is switched off, and it is a script people copy from one environment to the next, so migrate it with everything else. A credential written literally into a deployment script is still a secret to remove.

### SH-011 · WgetChallengeDependentCredentials

<a id="sh-011-judging"></a>

A `wget` call that is given a user name and a password and no `--auth-no-challenge`. It is rated Low because, as written, `wget` sends no credential until the server answers with a Basic challenge: when the server answers with a bearer challenge, as RelativityOne does, `wget` sends nothing (RelativityOne does not challenge with Basic; this rests on a single RelativityOne instance, is not documented by Relativity, and was not probed on Relativity Server), and when it answers with a Basic challenge, it sends Basic. Confirm where the call points. If it points at RelativityOne, the call already fails to authenticate with that credential, and you should look for the `--auth-no-challenge` switch or the hand-built header the script also uses, which matches its own class, and migrate that. If it points at another server that asks for Basic, the call is that service's own authentication and needs no migration to RelativityOne.

### SH-012 · AuthenticationHelperDefinition

<a id="sh-012-judging"></a>

A function that builds the credential or the authentication header for the script is a helper. Its definition is rated Low because the advice for a helper is not a mechanical code change. **The helper is the place to migrate, and it hides its callers:** the definition is a finding once, and its call sites are not findings because they contain no visible credential. Search for the helper's name to find every caller. If the helper builds Basic Authentication for calls to RelativityOne, migrate it once, so that it obtains and caches an access token and sends a Bearer header. Decide the flow from who supplies the credential to its callers: a person means Authorization Code, and a fixed credential means Client Credentials. Then follow the entry for the construct the helper contains, if it has one. If the helper builds a credential for another service, it needs no migration.

### SH-015 · ChallengeDependentClientCredentials

<a id="sh-015-judging"></a>

A call of `lwp-request` that gives a credential, or of `aria2c` with `--http-auth-challenge` set, with a user name and a password. It is rated Low because the `-C` form of `lwp-request` and the challenge switch of `aria2c` send Basic only after the server challenges with Basic, and RelativityOne does not (this rests on a single RelativityOne instance, is not documented by Relativity, and was not probed on Relativity Server), so as written those calls do not send Basic to it. Confirm where the call points. If it points at RelativityOne, a call that gives the credential with `-C` or with the switch does not authenticate with that credential, and you should look for the hand-built header or the preemptive form the script also uses, which matches its own class, and migrate that. The other two forms of `lwp-request` are different: a `-H` header with a literal `Authorization: Basic` value and a credential in the address (userinfo) are sent on the first request, without waiting for a challenge, so they do authenticate with the credential as written, whatever the level of this class: migrate them like SH-003 and SH-005. If it points at another server that asks for Basic, the call is that service's own authentication and needs no migration to RelativityOne. A credential written literally into the call is still a secret to remove.

### SH-017 · WgetExecuteCredentialsChallengeDependent

<a id="sh-017-judging"></a>

A `wget` call that gives the user name and the password as settings with `-e`, and no preemptive switch. The reason is the one of SH-011: `wget` sends Basic only after a Basic challenge, so as written the call does not send Basic to RelativityOne, which does not challenge with Basic (this rests on a single RelativityOne instance, is not documented by Relativity, and was not probed on Relativity Server). Confirm where the call points. If it points at RelativityOne, the call does not authenticate with that credential; look for the `--auth-no-challenge` switch or the hand-built header the script also uses, and migrate that. If it points at another server that asks for Basic, it needs no migration to RelativityOne. The password written in the setting is still a secret to remove, and it is visible in the process list while the command runs.

### SH-020 · WgetConfigurationWrittenByScript

<a id="sh-020-judging"></a>

A script that writes a start-up file for `wget` with the credential in it, and points `wget` at it with `WGETRC` or `--config`. It is rated Low because `wget` sends Basic only after a Basic challenge unless the file also turns the preemptive setting on, and the weaker fact decides the level. Confirm where the `wget` call points and whether the file sets `auth_no_challenge`. If the call points at RelativityOne, it does not send Basic as written (RelativityOne does not challenge with Basic; this rests on a single RelativityOne instance, is not documented by Relativity, and was not probed on Relativity Server), and the migration is the one of the call that does: follow the entry of its class. If it points at another server that asks for Basic, the call is that service's own authentication and needs no migration to RelativityOne. The file holds a secret: create it under `umask 077`, delete it when the call is done, and never leave it in a repository.

### SH-022 · WgetViaSameFileAliasOrVariable

<a id="sh-022-judging"></a>

A `wget` call made through a name that the same file defines, with a user name and a password. It is rated Low for the reason of SH-011, and because behind a name the join between the command and its options is by text. Confirm where the call points. If it points at RelativityOne, the call does not send Basic as written (RelativityOne does not challenge with Basic; this rests on a single RelativityOne instance, is not documented by Relativity, and was not probed on Relativity Server), and you should migrate the script's real path to RelativityOne, which matches its own class if there is one. If it points at another server that asks for Basic, it needs no migration to RelativityOne. Search the file for every use of the name: a credential given through the name does not match as a literal, so a password written there is a secret that is easy to miss.

### SH-023 · CurlAnyauthWithUser

<a id="sh-023-judging"></a>

A `curl` call with `--anyauth` and a user name and a password. `--anyauth` makes `curl` wait for the server's challenge and choose the method itself, so it sends nothing on the first request to a server that does not challenge. The call is rated Low because, as written, it does not send Basic to RelativityOne, which does not challenge with Basic (this rests on a single RelativityOne instance, is not documented by Relativity, and was not probed on Relativity Server). Confirm where the call points. If it points at RelativityOne, the call does not authenticate with that credential; look for the hand-built header or the call without `--anyauth` that the script also uses, and migrate that. If it points at another server that offers Basic, the call is that service's own authentication and needs no migration to RelativityOne. A credential written literally into the call is still a secret to remove.

## Migration suggestions by pattern class

Each of the 18 pattern classes that produce a recommendation (18 of the 25) has two entries, one for each flow. Choose the entry for the flow that fits your integration; the "Which flow" paragraph of the class in Part A says when a match belongs to each. Every entry contains the code being replaced, the migrated code, a description of token acquisition, caching and expiry handling, the flow the entry is for, the OAuth2 Client to create or confirm (a link to its settings in [OAuth2 Client for Client Credentials](#oauth2-client-for-client-credentials) or [OAuth2 Client for Authorization Code](#oauth2-client-for-authorization-code)), and the pointer to Secret Management (Part B of the Migration Documentation). Every Authorization Code entry adds a description of refresh-token handling, of PKCE, and of the two ways the script receives the code: the manual copy of the address the browser ends on, and the loopback listener.

**Part 1, part 2 and part 3.** The text of an entry refers to its own pieces by number. Each piece has a label in the entry:

- **Part 1** is the change at the point of use: the text under the label *Under token authentication*, with its migrated code.
- **Part 2** is the token acquisition: the part of the text under the label *Credentials* that follows the paragraph *Where the credentials come from*. It describes the request to the token endpoint, the caching of the token and what to do when it expires. A Client Credentials entry starts it with *Where the token comes from*, and an Authorization Code entry with *Common to both variants*.
- **Part 3** is the OAuth2 Client settings, under the label *OAuth2 Client* at the end of the entry.

The labels *Source form* (the construct being replaced, shown as an excerpt; Part A gives the complete form) and *Verification* (what to check before you deploy) are not numbered.

The seven Low classes, SH-011, SH-012, SH-015, SH-017, SH-020, SH-022 and SH-023, have no entry because they produce no recommendation. They are covered under [Judging a Low-confidence match](#judging-a-low-confidence-match).

### SH-001 · CurlUserPassword

<a id="sh-001-migration"></a>

#### Client Credentials

<!-- generated:begin SH-001 client-credentials -->
**Flow.** This entry is for Client Credentials. The "Which flow" paragraph of SH-001 in [Part A](part-a-shell.md#sh-001) says when a match belongs to this flow.

Source form: **Applies to:** `bash`, `zsh`, `ksh93` and `mksh` (and `sh`, read as `bash`); `curl` (a `--user=` form needs `curl` 8.x).

```sh
curl --silent --show-error -u "${API_USER}:${API_PASSWORD}" "${HOST}/Relativity.REST/api/..."
```

Under token authentication: **First, the destination:** if this Relativity instance is not RelativityOne — including a Relativity Server instance, whose own documentation governs — no migration to RelativityOne applies and the rest of this entry does not apply.

```sh
curl --silent --show-error --fail -H @"${HEADER_FILE}" "${HOST}/Relativity.REST/api/..."
```

where `HEADER_FILE` holds `Authorization: Bearer <access token>` and `X-CSRF-Header: -` (see the token acquisition below). Remove `-u`, `--user` and `--basic`.

Credentials: **Where the credentials come from.** The command line establishes that a client sends Basic, not that the destination is RelativityOne. If it is not RelativityOne, the migration is that service's own token mechanism, no migration to RelativityOne applies, and the reader stops here. If it is RelativityOne, the token comes from the token-acquisition part that follows, and `API_USER` and `API_PASSWORD` are replaced by the OAuth2 Client's client ID and secret.

**Where the token comes from.** Create an OAuth2 Client with Flow Grant Type set to Client Credentials (part 3 lists its settings), then send a form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=client_credentials`, `client_id`, `client_secret` and a `scope`. The token endpoint is **documented** for this grant. The token is then sent as `Authorization: Bearer <token>`, with `X-CSRF-Header: -` on REST and Kepler calls — the header is documented as required, and a Kepler call without it returns 404. **Applies to:** `bash`, `zsh`, `ksh93` and `mksh` (and `sh`, read as `bash`); the blocks need `curl` and `jq`, which is a stated prerequisite: no fallback that parses the JSON with a regular expression is recommended.

```sh
# HOST, CLIENT_ID, TOKEN_SCOPE and CLIENT_SECRET_FILE come from the caller's environment; the secret file is created as Secret Management (Part B of the Migration Documentation) describes.
acquire_access_token() {
	curl --silent --show-error --fail \
		--data-urlencode "grant_type=client_credentials" \
		--data-urlencode "client_id=${CLIENT_ID}" \
		--data-urlencode "client_secret@${CLIENT_SECRET_FILE}" \
		--data-urlencode "scope=${TOKEN_SCOPE}" \
		"${HOST}/Relativity/Identity/connect/token" \
		| jq -r '.access_token'
}
ACCESS_TOKEN=$(acquire_access_token)
```

`--data-urlencode "client_secret@FILE"` reads the secret from a file and keeps it out of the command line (*checked by NSerio locally*: the file is read whole, a trailing line feed included, so create it with `printf '%s'`); `curl -K -` with the same setting on standard input also works (*checked by NSerio locally*). **The scope depends on what the integration needs, and no single scope is required.** Relativity's documentation shows two values for this grant: `SystemUserInfo` on the REST authentication page, the Usage Reports page and in the official Transfer SDK sample, and `UserInfoAccess` in the Kepler .NET samples. No page says which is correct, whether both work, or what either grants, and the OAuth2 Client's `Scopes` property is documented as one that cannot be set. Use the value the API you call documents, and confirm it.

**Use the token without putting it on the command line.**

```sh
( umask 077; printf 'Authorization: Bearer %s\nX-CSRF-Header: -\n' "${ACCESS_TOKEN}" > "${HEADER_FILE}" )
curl --silent --show-error --fail -H @"${HEADER_FILE}" "${HOST}/Relativity.REST/api/..."
```

`curl -H @FILE` sends the header lines of a file (*checked by NSerio locally* on `curl` 8.17.0 and 7.81.0; documented from `curl` 7.55). A token in `-H "Authorization: Bearer $TOKEN"` is visible to every user of the machine in `ps` while the command runs, so the file form is the recommended one for a shared host.

**Cache the token, and re-acquire it on expiry — do not refresh.** Keep the token for its lifetime (part 3 recommends 60 minutes, Relativity's own wording) and request a new one with the client secret shortly before it expires (a margin of 60 seconds is reasonable) and once when a call is rejected for authentication. Client Credentials offers no refresh tokens: **verified by NSerio against a live instance; not documented by Relativity** — the Allow Refresh Tokens control does not appear for this grant in the RelativityOne user interface. Relativity's own tables and text on refresh tokens conflict with each other, so re-acquisition with the secret is the advice that holds whichever way they are read. A script that runs for longer than the token's lifetime must acquire again, not hold the first token.

**The client secret.** It is displayed once, at creation or reset. Store it as Secret Management (Part B of the Migration Documentation) describes; in a shell estate that means **a file created under `umask 077`** or the operating system's keychain, **never in the script, never in a tracked `.env` file, and never exported to child processes**. A reset invalidates the previous secret immediately, so plan the cut-over before resetting it.

**Before deploying, confirm the calling identity.** Every call now runs as the OAuth2 Client's **Context User**, not as the user in the replaced credential. If that user has different workspace permissions or item-level security, the script returns fewer rows or `Unauthorized` in production with no change to its logic, and Relativity's audit attributes every call to that one account. Confirm the Context User's permissions match those of the previous identity.

**When the script acts for a signed-in user.** Client Credentials is one service identity by construction. A script that prompts a person for a password and sends it is the Authorization Code case, and moving to it means a new OAuth2 Client, because the flow is fixed when a client is created. Which of the two flows applies depends on who supplies the credential at run time.

Verification: **Verification note — three things, and the first can break working code.** **A token has a finite lifetime.** A script that obtains the token once and keeps it fails authentication after it expires; acquire again within the lifetime. **The identity changes**: calls run as the OAuth2 Client's Context User. **A value that is `-u "$CREDS"` or built from several variables** may hold a user name and a password, or a user name only (`curl` then prompts): after the change the prompt, if any, is gone. **Other uses may not appear as matches**: a `curl` inside a wrapper function is read only at a call that passes a literal credential, and a call that passes variables yields no match; a `curl` called through a name reached by indirection is not read; one called through a same-file alias or command variable is.

OAuth2 Client: See [OAuth2 Client for Client Credentials](#oauth2-client-for-client-credentials).
<!-- generated:end SH-001 client-credentials -->

#### Authorization Code

<!-- generated:begin SH-001 authorization-code -->
**Flow.** This entry is for Authorization Code. The "Which flow" paragraph of SH-001 in [Part A](part-a-shell.md#sh-001) says when a match belongs to this flow.

Source form: **Applies to:** `bash`, `zsh`, `ksh93` and `mksh` (and `sh`, read as `bash`); `curl` (a `--user=` form needs `curl` 8.x).

```sh
curl --silent --show-error -u "${API_USER}:${API_PASSWORD}" "${HOST}/Relativity.REST/api/..."
```

Under token authentication: **First, the destination:** if this Relativity instance is not RelativityOne — including a Relativity Server instance, whose own documentation governs — no migration to RelativityOne applies and the rest of this entry does not apply.

```sh
curl --silent --show-error --fail -H @"${HEADER_FILE}" "${HOST}/Relativity.REST/api/..."
```

where `HEADER_FILE` holds `Authorization: Bearer <access token>` and `X-CSRF-Header: -` (see the token acquisition below). Remove `-u`, `--user` and `--basic`.

Credentials: **Where the credentials come from.** The command line establishes that a client sends Basic, not that the destination is RelativityOne. If it is not RelativityOne, the migration is that service's own token mechanism, no migration to RelativityOne applies, and the reader stops here. If it is RelativityOne, the token comes from the token-acquisition part that follows, and `API_USER` and `API_PASSWORD` are replaced by the OAuth2 Client's client ID and secret.

**Common to both variants.** **Applies to:** `bash`, `zsh`, `ksh93` and `mksh` (and `sh`, read as `bash`); the blocks need `curl`, `openssl` and `jq`. The text has a common part and two variants of how the script receives the code: the **manual copy** (primary) and a **loopback listener** (optional).

**Sign in.** Replace the code that collected the user's credentials with a redirect: send the user's browser to the authorize endpoint `<host>/Relativity/Identity/connect/authorize`, which leads to the RelativityOne login page. The authorize endpoint is **verified by NSerio against a live instance; not documented by Relativity**. The request carries the standard authorization-code parameters — `client_id`, `response_type=code`, the registered `redirect_uri`, a `scope`, and a `state` value the script checks on return, plus `code_challenge` and `code_challenge_method=S256` when PKCE is used; these are standard OAuth, and Relativity documents none of them. No discovery document is relied on.

**PKCE.** PKCE is required by Relativity's own documentation to obtain a refresh token, and not otherwise: a Code request that does not ask for a refresh token works without it, **verified by NSerio against a live instance; not documented by Relativity**. It is never a blanket requirement. NSerio recommends PKCE with the `S256` method (NSerio recommendation), and does not claim that Relativity accepts any particular method, which it does not document. With `openssl`:

```sh
code_verifier=$(openssl rand -base64 48 | tr -d '\r\n' | tr '+/' '-_' | tr -d '=' | cut -c1-64)
code_challenge=$(printf '%s' "${code_verifier}" | openssl dgst -sha256 -binary | openssl base64 -A | tr '+/' '-_' | tr -d '=')
state=$(openssl rand -hex 16 | tr -d '\r')
```

The pipeline gives a 64-character verifier and a 43-character challenge (*checked by NSerio locally* with OpenSSL 3.5.4 on Windows and OpenSSL 3.0.2 on Linux; the `openssl` that ships with macOS has not been checked).

**Receive the code, by the manual copy (primary).** The browser returns to the Redirect URL registered on the OAuth2 Client with the authorization code and the `state` value. **The script prints the authorize address and the person opens it in any browser; when nothing listens at the registered loopback address the browser shows a connection error and leaves the full address, with `code` and `state`, in its address bar; the person copies that address and pastes it to the script.** This works headless, over SSH, on Linux and macOS, and needs no listener (NSerio recommendation; the browser's behaviour on an unreachable loopback address is *inferred* and has not been checked). The Redirect URL must still be exactly the one registered.

```sh
urlencode() (
	LC_ALL=C
	remaining=$1
	while [ -n "${remaining}" ]; do
		rest=${remaining#?}
		character=${remaining%"${rest}"}
		case ${character} in
			[A-Za-z0-9._~-]) printf '%s' "${character}" ;;
			*) printf '%%%02X' "'${character}" ;;
		esac
		remaining=${rest}
	done
)

authorize_url="${HOST}/Relativity/Identity/connect/authorize?client_id=$(urlencode "${CLIENT_ID}")&response_type=code&redirect_uri=$(urlencode "${REDIRECT_URI}")&scope=$(urlencode "${TOKEN_SCOPE}")&state=${state}&code_challenge=${code_challenge}&code_challenge_method=S256"
printf 'Open this address in a browser and sign in:\n%s\n' "${authorize_url}"
printf 'Then copy the whole address the browser ends on and paste it here: '
IFS= read -r returned_url
```

```sh
query=${returned_url#*\?}
query=${query%%#*}
returned_code=''
returned_state=''
remaining=${query}
while [ -n "${remaining}" ]; do
	pair=${remaining%%'&'*}
	case ${remaining} in
		*'&'*) remaining=${remaining#*'&'} ;;
		*) remaining='' ;;
	esac
	case ${pair} in
		code=*) returned_code=${pair#code=} ;;
		state=*) returned_state=${pair#state=} ;;
	esac
done
[ "${returned_state}" = "${state}" ] || { printf 'The state does not match; stop.\n' >&2; exit 1; }
```

The percent-encoding function and the address parsing give the same output in `bash`, `dash`, `zsh`, `ksh93` and `mksh` (*checked by NSerio locally*). **Check the `state` before using the code.** The code is already percent-encoded in the pasted address, so it is sent with `data` (not `data-urlencode`) below and not encoded again. The block passes the code and the verifier to `curl` as a configuration on standard input (`-K -`), so neither appears on the command line of `curl`; `printf` is a shell built-in and its arguments do not appear in `ps` either.

**Exchange the code.** Send a form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=authorization_code`, the `code`, the same `redirect_uri`, the `client_id` and the `client_secret`, plus the `code_verifier` when PKCE is used. The code is exchanged at that endpoint, which Relativity documents for the Client Credentials grant only; the exchange is **verified by NSerio against a live instance; not documented by Relativity**.

```sh
# The code and the verifier travel on standard input, never on a command line; printf is a built-in and does not appear in ps.
token_response=$(printf '%s\n' \
		"data = \"grant_type=authorization_code\"" \
		"data = \"code=${returned_code}\"" \
		"data-urlencode = \"redirect_uri=${REDIRECT_URI}\"" \
		"data-urlencode = \"client_id=${CLIENT_ID}\"" \
		"data-urlencode = \"code_verifier=${code_verifier}\"" \
		"data-urlencode = \"client_secret@${CLIENT_SECRET_FILE}\"" \
	| curl --silent --show-error --fail -K - \
		"${HOST}/Relativity/Identity/connect/token")
ACCESS_TOKEN=$(printf '%s' "${token_response}" | jq -r '.access_token')
```

**Use the token.** Send `Authorization: Bearer <access token>` — the change at the use site above — with `X-CSRF-Header: -` on REST and Kepler calls, from a header file as for Client Credentials: `( umask 077; printf 'Authorization: Bearer %s\nX-CSRF-Header: -\n' "${ACCESS_TOKEN}" > "${HEADER_FILE}" )`, then `curl -H @"${HEADER_FILE}"`.

**On expiry.** With Allow Refresh Tokens enabled on the OAuth2 Client and PKCE used, the script holds a refresh token and, when the access token expires, sends a `grant_type=refresh_token` request with it and the client credentials in place of the code exchange — *inferred* from the standard OAuth shape, not tested. Without a refresh token the person signs in again. Relativity does not document a refresh token's lifetime, whether it rotates, how it is revoked, whether a revocation endpoint exists, or what revoking it does to access tokens already issued. **A refresh token is a secret:** keep it in a file created under `umask 077` or in the keychain, never on a command line.

**Scope.** No scope set is prescribed for Code. The scope `openid RelativityRequestOrigin UserInfo UserInfoAccess AuthenticationID RelativityWeb` works, **verified by NSerio against a live instance; not documented by Relativity**, verified as working and not as minimal. `state` is sent as standard OAuth practice and `nonce` is not used; Relativity does not document whether it enforces either.

**The script stops collecting the password.** After this change the script never receives, stores or forwards the user's user name and password: the user signs in on RelativityOne's own page, and the script holds only tokens. Remove the `read` that prompts for the password, the `stty -echo` around it and any storage of the password the replaced code needed.

**Single sign-on.** Sign-in happens on RelativityOne's own login page, so the instance's single sign-on and two-factor rules apply unchanged — *inferred* from where the sign-in happens, and not documented for clients that customers create.

**The client secret is required.** Code does not work without a client secret: tested and rejected, **verified by NSerio against a live instance; not documented by Relativity**, which confirms Relativity's own statement that the grant requires one. **A script is plain text, so a secret shipped inside a distributed script can be read by anyone who holds the script**; it protects nothing against the person running it. Store it as Secret Management (Part B of the Migration Documentation) describes, in a file with the person's own permissions (`umask 077`) or in the keychain, and apply the compensating guidance below.

**Loopback listener variant (optional, per system).** A shell has no HTTP listener of its own. Where a script runs on a machine with a browser and a registered fixed-port loopback Redirect URL `http://127.0.0.1:<port>/<path>` — a plain-HTTP loopback URL is accepted, **verified by NSerio against a live instance; not documented by Relativity** — it can start `nc` or `socat` bound to `127.0.0.1` only, for one request, to read the request line `GET /<path>?code=…&state=… HTTP/1.1` and answer with a short page: on Linux the OpenBSD `nc` does it with `nc -l 127.0.0.1 PORT` (*checked by NSerio locally*); the options differ between OpenBSD, BSD and macOS, GNU and traditional `nc` and `ncat`, and `socat`, and those have not been checked. Bind to loopback only, check the `state` (another local process can connect to the port), and fall back to the manual copy when the tool is missing or the machine is headless.

**Compensating guidance** (NSerio recommendation, subject to your own security policy): set Trusted IPs where the users' outbound addresses are known and stable — they restrict where a token can be *requested* — use the recommended short Access Token Lifetime of 60 minutes, enable refresh tokens only when needed, and create one OAuth2 Client per distribution, so a leaked secret can be disabled without affecting the others. **A documentation gap.** Relativity documents no recommendation for a distributed script or desktop application; this design is what NSerio verified works.

Verification: **Verification note — three things, and the first can break working code.** **A token has a finite lifetime.** A script that obtains the token once and keeps it fails authentication after it expires; acquire again within the lifetime. **The identity changes**: calls run as the OAuth2 Client's Context User. **A value that is `-u "$CREDS"` or built from several variables** may hold a user name and a password, or a user name only (`curl` then prompts): after the change the prompt, if any, is gone. **Other uses may not appear as matches**: a `curl` inside a wrapper function is read only at a call that passes a literal credential, and a call that passes variables yields no match; a `curl` called through a name reached by indirection is not read; one called through a same-file alias or command variable is.

OAuth2 Client: See [OAuth2 Client for Authorization Code](#oauth2-client-for-authorization-code).
<!-- generated:end SH-001 authorization-code -->

### SH-002 · CurlUserWithoutPassword

<a id="sh-002-migration"></a>

#### Client Credentials

<!-- generated:begin SH-002 client-credentials -->
**Flow.** This entry is for Client Credentials. The "Which flow" paragraph of SH-002 in [Part A](part-a-shell.md#sh-002) says when a match belongs to this flow.

Source form: **Applies to:** `bash`, `zsh`, `ksh93` and `mksh` (and `sh`, read as `bash`); `curl`.

```sh
curl --silent --show-error -u user "${HOST}/Relativity.REST/api/..."
```

Under token authentication: **First, the destination:** if this Relativity instance is not RelativityOne — including a Relativity Server instance, whose own documentation governs — no migration to RelativityOne applies and the rest of this entry does not apply.

```sh
curl --silent --show-error --fail -H @"${HEADER_FILE}" "${HOST}/Relativity.REST/api/..."
```

The source form makes `curl` prompt a person for the password and then send Basic; the target form sends a token and prompts nothing.

Credentials: **Where the credentials come from.** The text establishes that a credential is sent as Basic, not that the destination is RelativityOne. If it is not RelativityOne, the migration is that service's own token mechanism, no migration to RelativityOne applies, and the reader stops here. If it is RelativityOne, the token comes from the token-acquisition part that follows, and the user name and password are replaced by the OAuth2 Client's client ID and secret. **Because a person types the password, this construct is also interactive flow evidence**: the match is assigned the Authorization Code flow, in which the person signs in on RelativityOne's page.

**Where the token comes from.** Create an OAuth2 Client with Flow Grant Type set to Client Credentials (part 3 lists its settings), then send a form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=client_credentials`, `client_id`, `client_secret` and a `scope`. The token endpoint is **documented** for this grant. The token is then sent as `Authorization: Bearer <token>`, with `X-CSRF-Header: -` on REST and Kepler calls — the header is documented as required, and a Kepler call without it returns 404. **Applies to:** `bash`, `zsh`, `ksh93` and `mksh` (and `sh`, read as `bash`); the blocks need `curl` and `jq`, which is a stated prerequisite: no fallback that parses the JSON with a regular expression is recommended.

```sh
# HOST, CLIENT_ID, TOKEN_SCOPE and CLIENT_SECRET_FILE come from the caller's environment; the secret file is created as Secret Management (Part B of the Migration Documentation) describes.
acquire_access_token() {
	curl --silent --show-error --fail \
		--data-urlencode "grant_type=client_credentials" \
		--data-urlencode "client_id=${CLIENT_ID}" \
		--data-urlencode "client_secret@${CLIENT_SECRET_FILE}" \
		--data-urlencode "scope=${TOKEN_SCOPE}" \
		"${HOST}/Relativity/Identity/connect/token" \
		| jq -r '.access_token'
}
ACCESS_TOKEN=$(acquire_access_token)
```

`--data-urlencode "client_secret@FILE"` reads the secret from a file and keeps it out of the command line (*checked by NSerio locally*: the file is read whole, a trailing line feed included, so create it with `printf '%s'`); `curl -K -` with the same setting on standard input also works (*checked by NSerio locally*). **The scope depends on what the integration needs, and no single scope is required.** Relativity's documentation shows two values for this grant: `SystemUserInfo` on the REST authentication page, the Usage Reports page and in the official Transfer SDK sample, and `UserInfoAccess` in the Kepler .NET samples. No page says which is correct, whether both work, or what either grants, and the OAuth2 Client's `Scopes` property is documented as one that cannot be set. Use the value the API you call documents, and confirm it.

**Use the token without putting it on the command line.**

```sh
( umask 077; printf 'Authorization: Bearer %s\nX-CSRF-Header: -\n' "${ACCESS_TOKEN}" > "${HEADER_FILE}" )
curl --silent --show-error --fail -H @"${HEADER_FILE}" "${HOST}/Relativity.REST/api/..."
```

`curl -H @FILE` sends the header lines of a file (*checked by NSerio locally* on `curl` 8.17.0 and 7.81.0; documented from `curl` 7.55). A token in `-H "Authorization: Bearer $TOKEN"` is visible to every user of the machine in `ps` while the command runs, so the file form is the recommended one for a shared host.

**Cache the token, and re-acquire it on expiry — do not refresh.** Keep the token for its lifetime (part 3 recommends 60 minutes, Relativity's own wording) and request a new one with the client secret shortly before it expires (a margin of 60 seconds is reasonable) and once when a call is rejected for authentication. Client Credentials offers no refresh tokens: **verified by NSerio against a live instance; not documented by Relativity** — the Allow Refresh Tokens control does not appear for this grant in the RelativityOne user interface. Relativity's own tables and text on refresh tokens conflict with each other, so re-acquisition with the secret is the advice that holds whichever way they are read. A script that runs for longer than the token's lifetime must acquire again, not hold the first token.

**The client secret.** It is displayed once, at creation or reset. Store it as Secret Management (Part B of the Migration Documentation) describes; in a shell estate that means **a file created under `umask 077`** or the operating system's keychain, **never in the script, never in a tracked `.env` file, and never exported to child processes**. A reset invalidates the previous secret immediately, so plan the cut-over before resetting it.

**Before deploying, confirm the calling identity.** Every call now runs as the OAuth2 Client's **Context User**, not as the user in the replaced credential. If that user has different workspace permissions or item-level security, the script returns fewer rows or `Unauthorized` in production with no change to its logic, and Relativity's audit attributes every call to that one account. Confirm the Context User's permissions match those of the previous identity.

**When the script acts for a signed-in user.** Client Credentials is one service identity by construction. A script that prompts a person for a password and sends it is the Authorization Code case, and moving to it means a new OAuth2 Client, because the flow is fixed when a client is created. Which of the two flows applies depends on who supplies the credential at run time.

Verification: **Verification note.** **A token has a finite lifetime.** Acquire again within it. **If the person's own identity matters** — the calls must run as the person who typed the password — use the Authorization Code flow; Client Credentials runs every call as the OAuth2 Client's Context User.

OAuth2 Client: See [OAuth2 Client for Client Credentials](#oauth2-client-for-client-credentials).
<!-- generated:end SH-002 client-credentials -->

#### Authorization Code

<!-- generated:begin SH-002 authorization-code -->
**Flow.** This entry is for Authorization Code. The "Which flow" paragraph of SH-002 in [Part A](part-a-shell.md#sh-002) says when a match belongs to this flow.

Source form: **Applies to:** `bash`, `zsh`, `ksh93` and `mksh` (and `sh`, read as `bash`); `curl`.

```sh
curl --silent --show-error -u user "${HOST}/Relativity.REST/api/..."
```

Under token authentication: **First, the destination:** if this Relativity instance is not RelativityOne — including a Relativity Server instance, whose own documentation governs — no migration to RelativityOne applies and the rest of this entry does not apply.

```sh
curl --silent --show-error --fail -H @"${HEADER_FILE}" "${HOST}/Relativity.REST/api/..."
```

The source form makes `curl` prompt a person for the password and then send Basic; the target form sends a token and prompts nothing.

Credentials: **Where the credentials come from.** The text establishes that a credential is sent as Basic, not that the destination is RelativityOne. If it is not RelativityOne, the migration is that service's own token mechanism, no migration to RelativityOne applies, and the reader stops here. If it is RelativityOne, the token comes from the token-acquisition part that follows, and the user name and password are replaced by the OAuth2 Client's client ID and secret. **Because a person types the password, this construct is also interactive flow evidence**: the match is assigned the Authorization Code flow, in which the person signs in on RelativityOne's page.

**Common to both variants.** **Applies to:** `bash`, `zsh`, `ksh93` and `mksh` (and `sh`, read as `bash`); the blocks need `curl`, `openssl` and `jq`. The text has a common part and two variants of how the script receives the code: the **manual copy** (primary) and a **loopback listener** (optional).

**Sign in.** Replace the code that collected the user's credentials with a redirect: send the user's browser to the authorize endpoint `<host>/Relativity/Identity/connect/authorize`, which leads to the RelativityOne login page. The authorize endpoint is **verified by NSerio against a live instance; not documented by Relativity**. The request carries the standard authorization-code parameters — `client_id`, `response_type=code`, the registered `redirect_uri`, a `scope`, and a `state` value the script checks on return, plus `code_challenge` and `code_challenge_method=S256` when PKCE is used; these are standard OAuth, and Relativity documents none of them. No discovery document is relied on.

**PKCE.** PKCE is required by Relativity's own documentation to obtain a refresh token, and not otherwise: a Code request that does not ask for a refresh token works without it, **verified by NSerio against a live instance; not documented by Relativity**. It is never a blanket requirement. NSerio recommends PKCE with the `S256` method (NSerio recommendation), and does not claim that Relativity accepts any particular method, which it does not document. With `openssl`:

```sh
code_verifier=$(openssl rand -base64 48 | tr -d '\r\n' | tr '+/' '-_' | tr -d '=' | cut -c1-64)
code_challenge=$(printf '%s' "${code_verifier}" | openssl dgst -sha256 -binary | openssl base64 -A | tr '+/' '-_' | tr -d '=')
state=$(openssl rand -hex 16 | tr -d '\r')
```

The pipeline gives a 64-character verifier and a 43-character challenge (*checked by NSerio locally* with OpenSSL 3.5.4 on Windows and OpenSSL 3.0.2 on Linux; the `openssl` that ships with macOS has not been checked).

**Receive the code, by the manual copy (primary).** The browser returns to the Redirect URL registered on the OAuth2 Client with the authorization code and the `state` value. **The script prints the authorize address and the person opens it in any browser; when nothing listens at the registered loopback address the browser shows a connection error and leaves the full address, with `code` and `state`, in its address bar; the person copies that address and pastes it to the script.** This works headless, over SSH, on Linux and macOS, and needs no listener (NSerio recommendation; the browser's behaviour on an unreachable loopback address is *inferred* and has not been checked). The Redirect URL must still be exactly the one registered.

```sh
urlencode() (
	LC_ALL=C
	remaining=$1
	while [ -n "${remaining}" ]; do
		rest=${remaining#?}
		character=${remaining%"${rest}"}
		case ${character} in
			[A-Za-z0-9._~-]) printf '%s' "${character}" ;;
			*) printf '%%%02X' "'${character}" ;;
		esac
		remaining=${rest}
	done
)

authorize_url="${HOST}/Relativity/Identity/connect/authorize?client_id=$(urlencode "${CLIENT_ID}")&response_type=code&redirect_uri=$(urlencode "${REDIRECT_URI}")&scope=$(urlencode "${TOKEN_SCOPE}")&state=${state}&code_challenge=${code_challenge}&code_challenge_method=S256"
printf 'Open this address in a browser and sign in:\n%s\n' "${authorize_url}"
printf 'Then copy the whole address the browser ends on and paste it here: '
IFS= read -r returned_url
```

```sh
query=${returned_url#*\?}
query=${query%%#*}
returned_code=''
returned_state=''
remaining=${query}
while [ -n "${remaining}" ]; do
	pair=${remaining%%'&'*}
	case ${remaining} in
		*'&'*) remaining=${remaining#*'&'} ;;
		*) remaining='' ;;
	esac
	case ${pair} in
		code=*) returned_code=${pair#code=} ;;
		state=*) returned_state=${pair#state=} ;;
	esac
done
[ "${returned_state}" = "${state}" ] || { printf 'The state does not match; stop.\n' >&2; exit 1; }
```

The percent-encoding function and the address parsing give the same output in `bash`, `dash`, `zsh`, `ksh93` and `mksh` (*checked by NSerio locally*). **Check the `state` before using the code.** The code is already percent-encoded in the pasted address, so it is sent with `data` (not `data-urlencode`) below and not encoded again. The block passes the code and the verifier to `curl` as a configuration on standard input (`-K -`), so neither appears on the command line of `curl`; `printf` is a shell built-in and its arguments do not appear in `ps` either.

**Exchange the code.** Send a form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=authorization_code`, the `code`, the same `redirect_uri`, the `client_id` and the `client_secret`, plus the `code_verifier` when PKCE is used. The code is exchanged at that endpoint, which Relativity documents for the Client Credentials grant only; the exchange is **verified by NSerio against a live instance; not documented by Relativity**.

```sh
# The code and the verifier travel on standard input, never on a command line; printf is a built-in and does not appear in ps.
token_response=$(printf '%s\n' \
		"data = \"grant_type=authorization_code\"" \
		"data = \"code=${returned_code}\"" \
		"data-urlencode = \"redirect_uri=${REDIRECT_URI}\"" \
		"data-urlencode = \"client_id=${CLIENT_ID}\"" \
		"data-urlencode = \"code_verifier=${code_verifier}\"" \
		"data-urlencode = \"client_secret@${CLIENT_SECRET_FILE}\"" \
	| curl --silent --show-error --fail -K - \
		"${HOST}/Relativity/Identity/connect/token")
ACCESS_TOKEN=$(printf '%s' "${token_response}" | jq -r '.access_token')
```

**Use the token.** Send `Authorization: Bearer <access token>` — the change at the use site above — with `X-CSRF-Header: -` on REST and Kepler calls, from a header file as for Client Credentials: `( umask 077; printf 'Authorization: Bearer %s\nX-CSRF-Header: -\n' "${ACCESS_TOKEN}" > "${HEADER_FILE}" )`, then `curl -H @"${HEADER_FILE}"`.

**On expiry.** With Allow Refresh Tokens enabled on the OAuth2 Client and PKCE used, the script holds a refresh token and, when the access token expires, sends a `grant_type=refresh_token` request with it and the client credentials in place of the code exchange — *inferred* from the standard OAuth shape, not tested. Without a refresh token the person signs in again. Relativity does not document a refresh token's lifetime, whether it rotates, how it is revoked, whether a revocation endpoint exists, or what revoking it does to access tokens already issued. **A refresh token is a secret:** keep it in a file created under `umask 077` or in the keychain, never on a command line.

**Scope.** No scope set is prescribed for Code. The scope `openid RelativityRequestOrigin UserInfo UserInfoAccess AuthenticationID RelativityWeb` works, **verified by NSerio against a live instance; not documented by Relativity**, verified as working and not as minimal. `state` is sent as standard OAuth practice and `nonce` is not used; Relativity does not document whether it enforces either.

**The script stops collecting the password.** After this change the script never receives, stores or forwards the user's user name and password: the user signs in on RelativityOne's own page, and the script holds only tokens. Remove the `read` that prompts for the password, the `stty -echo` around it and any storage of the password the replaced code needed.

**Single sign-on.** Sign-in happens on RelativityOne's own login page, so the instance's single sign-on and two-factor rules apply unchanged — *inferred* from where the sign-in happens, and not documented for clients that customers create.

**The client secret is required.** Code does not work without a client secret: tested and rejected, **verified by NSerio against a live instance; not documented by Relativity**, which confirms Relativity's own statement that the grant requires one. **A script is plain text, so a secret shipped inside a distributed script can be read by anyone who holds the script**; it protects nothing against the person running it. Store it as Secret Management (Part B of the Migration Documentation) describes, in a file with the person's own permissions (`umask 077`) or in the keychain, and apply the compensating guidance below.

**Loopback listener variant (optional, per system).** A shell has no HTTP listener of its own. Where a script runs on a machine with a browser and a registered fixed-port loopback Redirect URL `http://127.0.0.1:<port>/<path>` — a plain-HTTP loopback URL is accepted, **verified by NSerio against a live instance; not documented by Relativity** — it can start `nc` or `socat` bound to `127.0.0.1` only, for one request, to read the request line `GET /<path>?code=…&state=… HTTP/1.1` and answer with a short page: on Linux the OpenBSD `nc` does it with `nc -l 127.0.0.1 PORT` (*checked by NSerio locally*); the options differ between OpenBSD, BSD and macOS, GNU and traditional `nc` and `ncat`, and `socat`, and those have not been checked. Bind to loopback only, check the `state` (another local process can connect to the port), and fall back to the manual copy when the tool is missing or the machine is headless.

**Compensating guidance** (NSerio recommendation, subject to your own security policy): set Trusted IPs where the users' outbound addresses are known and stable — they restrict where a token can be *requested* — use the recommended short Access Token Lifetime of 60 minutes, enable refresh tokens only when needed, and create one OAuth2 Client per distribution, so a leaked secret can be disabled without affecting the others. **A documentation gap.** Relativity documents no recommendation for a distributed script or desktop application; this design is what NSerio verified works.

Verification: **Verification note.** **A token has a finite lifetime.** Acquire again within it. **If the person's own identity matters** — the calls must run as the person who typed the password — use the Authorization Code flow; Client Credentials runs every call as the OAuth2 Client's Context User.

OAuth2 Client: See [OAuth2 Client for Authorization Code](#oauth2-client-for-authorization-code).
<!-- generated:end SH-002 authorization-code -->

### SH-003 · AuthorizationHeaderBasic

<a id="sh-003-migration"></a>

#### Client Credentials

<!-- generated:begin SH-003 client-credentials -->
**Flow.** This entry is for Client Credentials. The "Which flow" paragraph of SH-003 in [Part A](part-a-shell.md#sh-003) says when a match belongs to this flow.

Source form: **Applies to:** `bash`, `zsh`, `ksh93` and `mksh` (and `sh`, read as `bash`); `curl` (`-H`, `--header`) and `wget` (`--header`).

```sh
curl --silent --show-error -H "Authorization: Basic ${ENCODED}" "${HOST}/Relativity.REST/api/..."
```

Under token authentication: **First, the destination:** if this Relativity instance is not RelativityOne — including a Relativity Server instance, whose own documentation governs — no migration to RelativityOne applies and the rest of this entry does not apply.

```sh
( umask 077; printf 'Authorization: Bearer %s\nX-CSRF-Header: -\n' "${ACCESS_TOKEN}" > "${HEADER_FILE}" )
curl --silent --show-error --fail -H @"${HEADER_FILE}" "${HOST}/Relativity.REST/api/..."
```

where `HEADER_FILE` is created under `umask 077` and holds `Authorization: Bearer <access token>` and `X-CSRF-Header: -`, so the token is not on the command line (see the token acquisition below). `wget` has no `-H @FILE` form: `--header="Authorization: Bearer ${ACCESS_TOKEN}"` puts the token on the command line, so use `curl` for this call. Remove the code that encoded the user name and password.

Credentials: **Where the credentials come from.** The header establishes that a Basic credential is sent, not that the destination is RelativityOne. If the destination is RelativityOne, the pair that was encoded is replaced by the token from the token-acquisition part that follows.

**Where the token comes from.** Create an OAuth2 Client with Flow Grant Type set to Client Credentials (part 3 lists its settings), then send a form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=client_credentials`, `client_id`, `client_secret` and a `scope`. The token endpoint is **documented** for this grant. The token is then sent as `Authorization: Bearer <token>`, with `X-CSRF-Header: -` on REST and Kepler calls — the header is documented as required, and a Kepler call without it returns 404. **Applies to:** `bash`, `zsh`, `ksh93` and `mksh` (and `sh`, read as `bash`); the blocks need `curl` and `jq`, which is a stated prerequisite: no fallback that parses the JSON with a regular expression is recommended.

```sh
# HOST, CLIENT_ID, TOKEN_SCOPE and CLIENT_SECRET_FILE come from the caller's environment; the secret file is created as Secret Management (Part B of the Migration Documentation) describes.
acquire_access_token() {
	curl --silent --show-error --fail \
		--data-urlencode "grant_type=client_credentials" \
		--data-urlencode "client_id=${CLIENT_ID}" \
		--data-urlencode "client_secret@${CLIENT_SECRET_FILE}" \
		--data-urlencode "scope=${TOKEN_SCOPE}" \
		"${HOST}/Relativity/Identity/connect/token" \
		| jq -r '.access_token'
}
ACCESS_TOKEN=$(acquire_access_token)
```

`--data-urlencode "client_secret@FILE"` reads the secret from a file and keeps it out of the command line (*checked by NSerio locally*: the file is read whole, a trailing line feed included, so create it with `printf '%s'`); `curl -K -` with the same setting on standard input also works (*checked by NSerio locally*). **The scope depends on what the integration needs, and no single scope is required.** Relativity's documentation shows two values for this grant: `SystemUserInfo` on the REST authentication page, the Usage Reports page and in the official Transfer SDK sample, and `UserInfoAccess` in the Kepler .NET samples. No page says which is correct, whether both work, or what either grants, and the OAuth2 Client's `Scopes` property is documented as one that cannot be set. Use the value the API you call documents, and confirm it.

**Use the token without putting it on the command line.**

```sh
( umask 077; printf 'Authorization: Bearer %s\nX-CSRF-Header: -\n' "${ACCESS_TOKEN}" > "${HEADER_FILE}" )
curl --silent --show-error --fail -H @"${HEADER_FILE}" "${HOST}/Relativity.REST/api/..."
```

`curl -H @FILE` sends the header lines of a file (*checked by NSerio locally* on `curl` 8.17.0 and 7.81.0; documented from `curl` 7.55). A token in `-H "Authorization: Bearer $TOKEN"` is visible to every user of the machine in `ps` while the command runs, so the file form is the recommended one for a shared host.

**Cache the token, and re-acquire it on expiry — do not refresh.** Keep the token for its lifetime (part 3 recommends 60 minutes, Relativity's own wording) and request a new one with the client secret shortly before it expires (a margin of 60 seconds is reasonable) and once when a call is rejected for authentication. Client Credentials offers no refresh tokens: **verified by NSerio against a live instance; not documented by Relativity** — the Allow Refresh Tokens control does not appear for this grant in the RelativityOne user interface. Relativity's own tables and text on refresh tokens conflict with each other, so re-acquisition with the secret is the advice that holds whichever way they are read. A script that runs for longer than the token's lifetime must acquire again, not hold the first token.

**The client secret.** It is displayed once, at creation or reset. Store it as Secret Management (Part B of the Migration Documentation) describes; in a shell estate that means **a file created under `umask 077`** or the operating system's keychain, **never in the script, never in a tracked `.env` file, and never exported to child processes**. A reset invalidates the previous secret immediately, so plan the cut-over before resetting it.

**Before deploying, confirm the calling identity.** Every call now runs as the OAuth2 Client's **Context User**, not as the user in the replaced credential. If that user has different workspace permissions or item-level security, the script returns fewer rows or `Unauthorized` in production with no change to its logic, and Relativity's audit attributes every call to that one account. Confirm the Context User's permissions match those of the previous identity.

**When the script acts for a signed-in user.** Client Credentials is one service identity by construction. A script that prompts a person for a password and sends it is the Authorization Code case, and moving to it means a new OAuth2 Client, because the flow is fixed when a client is created. Which of the two flows applies depends on who supplies the credential at run time.

Verification: **Verification note.** **A token has a finite lifetime.** **The scheme word changes from `Basic` to `Bearer`**: a `Bearer` header is not matched again. The header file keeps the token off the command line, where it would be visible in `ps` while the command runs.

OAuth2 Client: See [OAuth2 Client for Client Credentials](#oauth2-client-for-client-credentials).
<!-- generated:end SH-003 client-credentials -->

#### Authorization Code

<!-- generated:begin SH-003 authorization-code -->
**Flow.** This entry is for Authorization Code. The "Which flow" paragraph of SH-003 in [Part A](part-a-shell.md#sh-003) says when a match belongs to this flow.

Source form: **Applies to:** `bash`, `zsh`, `ksh93` and `mksh` (and `sh`, read as `bash`); `curl` (`-H`, `--header`) and `wget` (`--header`).

```sh
curl --silent --show-error -H "Authorization: Basic ${ENCODED}" "${HOST}/Relativity.REST/api/..."
```

Under token authentication: **First, the destination:** if this Relativity instance is not RelativityOne — including a Relativity Server instance, whose own documentation governs — no migration to RelativityOne applies and the rest of this entry does not apply.

```sh
( umask 077; printf 'Authorization: Bearer %s\nX-CSRF-Header: -\n' "${ACCESS_TOKEN}" > "${HEADER_FILE}" )
curl --silent --show-error --fail -H @"${HEADER_FILE}" "${HOST}/Relativity.REST/api/..."
```

where `HEADER_FILE` is created under `umask 077` and holds `Authorization: Bearer <access token>` and `X-CSRF-Header: -`, so the token is not on the command line (see the token acquisition below). `wget` has no `-H @FILE` form: `--header="Authorization: Bearer ${ACCESS_TOKEN}"` puts the token on the command line, so use `curl` for this call. Remove the code that encoded the user name and password.

Credentials: **Where the credentials come from.** The header establishes that a Basic credential is sent, not that the destination is RelativityOne. If the destination is RelativityOne, the pair that was encoded is replaced by the token from the token-acquisition part that follows.

**Common to both variants.** **Applies to:** `bash`, `zsh`, `ksh93` and `mksh` (and `sh`, read as `bash`); the blocks need `curl`, `openssl` and `jq`. The text has a common part and two variants of how the script receives the code: the **manual copy** (primary) and a **loopback listener** (optional).

**Sign in.** Replace the code that collected the user's credentials with a redirect: send the user's browser to the authorize endpoint `<host>/Relativity/Identity/connect/authorize`, which leads to the RelativityOne login page. The authorize endpoint is **verified by NSerio against a live instance; not documented by Relativity**. The request carries the standard authorization-code parameters — `client_id`, `response_type=code`, the registered `redirect_uri`, a `scope`, and a `state` value the script checks on return, plus `code_challenge` and `code_challenge_method=S256` when PKCE is used; these are standard OAuth, and Relativity documents none of them. No discovery document is relied on.

**PKCE.** PKCE is required by Relativity's own documentation to obtain a refresh token, and not otherwise: a Code request that does not ask for a refresh token works without it, **verified by NSerio against a live instance; not documented by Relativity**. It is never a blanket requirement. NSerio recommends PKCE with the `S256` method (NSerio recommendation), and does not claim that Relativity accepts any particular method, which it does not document. With `openssl`:

```sh
code_verifier=$(openssl rand -base64 48 | tr -d '\r\n' | tr '+/' '-_' | tr -d '=' | cut -c1-64)
code_challenge=$(printf '%s' "${code_verifier}" | openssl dgst -sha256 -binary | openssl base64 -A | tr '+/' '-_' | tr -d '=')
state=$(openssl rand -hex 16 | tr -d '\r')
```

The pipeline gives a 64-character verifier and a 43-character challenge (*checked by NSerio locally* with OpenSSL 3.5.4 on Windows and OpenSSL 3.0.2 on Linux; the `openssl` that ships with macOS has not been checked).

**Receive the code, by the manual copy (primary).** The browser returns to the Redirect URL registered on the OAuth2 Client with the authorization code and the `state` value. **The script prints the authorize address and the person opens it in any browser; when nothing listens at the registered loopback address the browser shows a connection error and leaves the full address, with `code` and `state`, in its address bar; the person copies that address and pastes it to the script.** This works headless, over SSH, on Linux and macOS, and needs no listener (NSerio recommendation; the browser's behaviour on an unreachable loopback address is *inferred* and has not been checked). The Redirect URL must still be exactly the one registered.

```sh
urlencode() (
	LC_ALL=C
	remaining=$1
	while [ -n "${remaining}" ]; do
		rest=${remaining#?}
		character=${remaining%"${rest}"}
		case ${character} in
			[A-Za-z0-9._~-]) printf '%s' "${character}" ;;
			*) printf '%%%02X' "'${character}" ;;
		esac
		remaining=${rest}
	done
)

authorize_url="${HOST}/Relativity/Identity/connect/authorize?client_id=$(urlencode "${CLIENT_ID}")&response_type=code&redirect_uri=$(urlencode "${REDIRECT_URI}")&scope=$(urlencode "${TOKEN_SCOPE}")&state=${state}&code_challenge=${code_challenge}&code_challenge_method=S256"
printf 'Open this address in a browser and sign in:\n%s\n' "${authorize_url}"
printf 'Then copy the whole address the browser ends on and paste it here: '
IFS= read -r returned_url
```

```sh
query=${returned_url#*\?}
query=${query%%#*}
returned_code=''
returned_state=''
remaining=${query}
while [ -n "${remaining}" ]; do
	pair=${remaining%%'&'*}
	case ${remaining} in
		*'&'*) remaining=${remaining#*'&'} ;;
		*) remaining='' ;;
	esac
	case ${pair} in
		code=*) returned_code=${pair#code=} ;;
		state=*) returned_state=${pair#state=} ;;
	esac
done
[ "${returned_state}" = "${state}" ] || { printf 'The state does not match; stop.\n' >&2; exit 1; }
```

The percent-encoding function and the address parsing give the same output in `bash`, `dash`, `zsh`, `ksh93` and `mksh` (*checked by NSerio locally*). **Check the `state` before using the code.** The code is already percent-encoded in the pasted address, so it is sent with `data` (not `data-urlencode`) below and not encoded again. The block passes the code and the verifier to `curl` as a configuration on standard input (`-K -`), so neither appears on the command line of `curl`; `printf` is a shell built-in and its arguments do not appear in `ps` either.

**Exchange the code.** Send a form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=authorization_code`, the `code`, the same `redirect_uri`, the `client_id` and the `client_secret`, plus the `code_verifier` when PKCE is used. The code is exchanged at that endpoint, which Relativity documents for the Client Credentials grant only; the exchange is **verified by NSerio against a live instance; not documented by Relativity**.

```sh
# The code and the verifier travel on standard input, never on a command line; printf is a built-in and does not appear in ps.
token_response=$(printf '%s\n' \
		"data = \"grant_type=authorization_code\"" \
		"data = \"code=${returned_code}\"" \
		"data-urlencode = \"redirect_uri=${REDIRECT_URI}\"" \
		"data-urlencode = \"client_id=${CLIENT_ID}\"" \
		"data-urlencode = \"code_verifier=${code_verifier}\"" \
		"data-urlencode = \"client_secret@${CLIENT_SECRET_FILE}\"" \
	| curl --silent --show-error --fail -K - \
		"${HOST}/Relativity/Identity/connect/token")
ACCESS_TOKEN=$(printf '%s' "${token_response}" | jq -r '.access_token')
```

**Use the token.** Send `Authorization: Bearer <access token>` — the change at the use site above — with `X-CSRF-Header: -` on REST and Kepler calls, from a header file as for Client Credentials: `( umask 077; printf 'Authorization: Bearer %s\nX-CSRF-Header: -\n' "${ACCESS_TOKEN}" > "${HEADER_FILE}" )`, then `curl -H @"${HEADER_FILE}"`.

**On expiry.** With Allow Refresh Tokens enabled on the OAuth2 Client and PKCE used, the script holds a refresh token and, when the access token expires, sends a `grant_type=refresh_token` request with it and the client credentials in place of the code exchange — *inferred* from the standard OAuth shape, not tested. Without a refresh token the person signs in again. Relativity does not document a refresh token's lifetime, whether it rotates, how it is revoked, whether a revocation endpoint exists, or what revoking it does to access tokens already issued. **A refresh token is a secret:** keep it in a file created under `umask 077` or in the keychain, never on a command line.

**Scope.** No scope set is prescribed for Code. The scope `openid RelativityRequestOrigin UserInfo UserInfoAccess AuthenticationID RelativityWeb` works, **verified by NSerio against a live instance; not documented by Relativity**, verified as working and not as minimal. `state` is sent as standard OAuth practice and `nonce` is not used; Relativity does not document whether it enforces either.

**The script stops collecting the password.** After this change the script never receives, stores or forwards the user's user name and password: the user signs in on RelativityOne's own page, and the script holds only tokens. Remove the `read` that prompts for the password, the `stty -echo` around it and any storage of the password the replaced code needed.

**Single sign-on.** Sign-in happens on RelativityOne's own login page, so the instance's single sign-on and two-factor rules apply unchanged — *inferred* from where the sign-in happens, and not documented for clients that customers create.

**The client secret is required.** Code does not work without a client secret: tested and rejected, **verified by NSerio against a live instance; not documented by Relativity**, which confirms Relativity's own statement that the grant requires one. **A script is plain text, so a secret shipped inside a distributed script can be read by anyone who holds the script**; it protects nothing against the person running it. Store it as Secret Management (Part B of the Migration Documentation) describes, in a file with the person's own permissions (`umask 077`) or in the keychain, and apply the compensating guidance below.

**Loopback listener variant (optional, per system).** A shell has no HTTP listener of its own. Where a script runs on a machine with a browser and a registered fixed-port loopback Redirect URL `http://127.0.0.1:<port>/<path>` — a plain-HTTP loopback URL is accepted, **verified by NSerio against a live instance; not documented by Relativity** — it can start `nc` or `socat` bound to `127.0.0.1` only, for one request, to read the request line `GET /<path>?code=…&state=… HTTP/1.1` and answer with a short page: on Linux the OpenBSD `nc` does it with `nc -l 127.0.0.1 PORT` (*checked by NSerio locally*); the options differ between OpenBSD, BSD and macOS, GNU and traditional `nc` and `ncat`, and `socat`, and those have not been checked. Bind to loopback only, check the `state` (another local process can connect to the port), and fall back to the manual copy when the tool is missing or the machine is headless.

**Compensating guidance** (NSerio recommendation, subject to your own security policy): set Trusted IPs where the users' outbound addresses are known and stable — they restrict where a token can be *requested* — use the recommended short Access Token Lifetime of 60 minutes, enable refresh tokens only when needed, and create one OAuth2 Client per distribution, so a leaked secret can be disabled without affecting the others. **A documentation gap.** Relativity documents no recommendation for a distributed script or desktop application; this design is what NSerio verified works.

Verification: **Verification note.** **A token has a finite lifetime.** **The scheme word changes from `Basic` to `Bearer`**: a `Bearer` header is not matched again. The header file keeps the token off the command line, where it would be visible in `ps` while the command runs.

OAuth2 Client: See [OAuth2 Client for Authorization Code](#oauth2-client-for-authorization-code).
<!-- generated:end SH-003 authorization-code -->

### SH-004 · WgetPreemptiveCredentials

<a id="sh-004-migration"></a>

#### Client Credentials

<!-- generated:begin SH-004 client-credentials -->
**Flow.** This entry is for Client Credentials. The "Which flow" paragraph of SH-004 in [Part A](part-a-shell.md#sh-004) says when a match belongs to this flow.

Source form: **Applies to:** `bash`, `zsh`, `ksh93` and `mksh` (and `sh`, read as `bash`); `wget`.

```sh
wget --auth-no-challenge --user="${API_USER}" --password="${API_PASSWORD}" "${HOST}/Relativity.REST/api/..."
```

Under token authentication: **First, the destination:** if this Relativity instance is not RelativityOne — including a Relativity Server instance, whose own documentation governs — no migration to RelativityOne applies and the rest of this entry does not apply.

```sh
wget --header="Authorization: Bearer ${ACCESS_TOKEN}" --header="X-CSRF-Header: -" "${HOST}/Relativity.REST/api/..."
```

Remove `--auth-no-challenge`, `--user`, `--password` and any userinfo in the URL.

Credentials: **Where the credentials come from.** The text establishes that a credential is sent as Basic, not that the destination is RelativityOne. If it is not RelativityOne, the migration is that service's own token mechanism, no migration to RelativityOne applies, and the reader stops here. If it is RelativityOne, the token comes from the token-acquisition part that follows, and the user name and password are replaced by the OAuth2 Client's client ID and secret.

**Where the token comes from.** Create an OAuth2 Client with Flow Grant Type set to Client Credentials (part 3 lists its settings), then send a form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=client_credentials`, `client_id`, `client_secret` and a `scope`. The token endpoint is **documented** for this grant. The token is then sent as `Authorization: Bearer <token>`, with `X-CSRF-Header: -` on REST and Kepler calls — the header is documented as required, and a Kepler call without it returns 404. **Applies to:** `bash`, `zsh`, `ksh93` and `mksh` (and `sh`, read as `bash`); the blocks need `curl` and `jq`, which is a stated prerequisite: no fallback that parses the JSON with a regular expression is recommended.

```sh
# HOST, CLIENT_ID, TOKEN_SCOPE and CLIENT_SECRET_FILE come from the caller's environment; the secret file is created as Secret Management (Part B of the Migration Documentation) describes.
acquire_access_token() {
	curl --silent --show-error --fail \
		--data-urlencode "grant_type=client_credentials" \
		--data-urlencode "client_id=${CLIENT_ID}" \
		--data-urlencode "client_secret@${CLIENT_SECRET_FILE}" \
		--data-urlencode "scope=${TOKEN_SCOPE}" \
		"${HOST}/Relativity/Identity/connect/token" \
		| jq -r '.access_token'
}
ACCESS_TOKEN=$(acquire_access_token)
```

`--data-urlencode "client_secret@FILE"` reads the secret from a file and keeps it out of the command line (*checked by NSerio locally*: the file is read whole, a trailing line feed included, so create it with `printf '%s'`); `curl -K -` with the same setting on standard input also works (*checked by NSerio locally*). **The scope depends on what the integration needs, and no single scope is required.** Relativity's documentation shows two values for this grant: `SystemUserInfo` on the REST authentication page, the Usage Reports page and in the official Transfer SDK sample, and `UserInfoAccess` in the Kepler .NET samples. No page says which is correct, whether both work, or what either grants, and the OAuth2 Client's `Scopes` property is documented as one that cannot be set. Use the value the API you call documents, and confirm it.

**Use the token without putting it on the command line.**

```sh
( umask 077; printf 'Authorization: Bearer %s\nX-CSRF-Header: -\n' "${ACCESS_TOKEN}" > "${HEADER_FILE}" )
curl --silent --show-error --fail -H @"${HEADER_FILE}" "${HOST}/Relativity.REST/api/..."
```

`curl -H @FILE` sends the header lines of a file (*checked by NSerio locally* on `curl` 8.17.0 and 7.81.0; documented from `curl` 7.55). A token in `-H "Authorization: Bearer $TOKEN"` is visible to every user of the machine in `ps` while the command runs, so the file form is the recommended one for a shared host.

**Cache the token, and re-acquire it on expiry — do not refresh.** Keep the token for its lifetime (part 3 recommends 60 minutes, Relativity's own wording) and request a new one with the client secret shortly before it expires (a margin of 60 seconds is reasonable) and once when a call is rejected for authentication. Client Credentials offers no refresh tokens: **verified by NSerio against a live instance; not documented by Relativity** — the Allow Refresh Tokens control does not appear for this grant in the RelativityOne user interface. Relativity's own tables and text on refresh tokens conflict with each other, so re-acquisition with the secret is the advice that holds whichever way they are read. A script that runs for longer than the token's lifetime must acquire again, not hold the first token.

**The client secret.** It is displayed once, at creation or reset. Store it as Secret Management (Part B of the Migration Documentation) describes; in a shell estate that means **a file created under `umask 077`** or the operating system's keychain, **never in the script, never in a tracked `.env` file, and never exported to child processes**. A reset invalidates the previous secret immediately, so plan the cut-over before resetting it.

**Before deploying, confirm the calling identity.** Every call now runs as the OAuth2 Client's **Context User**, not as the user in the replaced credential. If that user has different workspace permissions or item-level security, the script returns fewer rows or `Unauthorized` in production with no change to its logic, and Relativity's audit attributes every call to that one account. Confirm the Context User's permissions match those of the previous identity.

**When the script acts for a signed-in user.** Client Credentials is one service identity by construction. A script that prompts a person for a password and sends it is the Authorization Code case, and moving to it means a new OAuth2 Client, because the flow is fixed when a client is created. Which of the two flows applies depends on who supplies the credential at run time.

Verification: **Verification note.** **A token has a finite lifetime.** `wget` has no `-H @FILE` form, so the token is on the command line while `wget` runs, visible in `ps`: on a shared host use `curl` for this call, or a `wget` configuration file created under `umask 077` that holds the header (documented in the GNU Wget manual; not checked here).

OAuth2 Client: See [OAuth2 Client for Client Credentials](#oauth2-client-for-client-credentials).
<!-- generated:end SH-004 client-credentials -->

#### Authorization Code

<!-- generated:begin SH-004 authorization-code -->
**Flow.** This entry is for Authorization Code. The "Which flow" paragraph of SH-004 in [Part A](part-a-shell.md#sh-004) says when a match belongs to this flow.

Source form: **Applies to:** `bash`, `zsh`, `ksh93` and `mksh` (and `sh`, read as `bash`); `wget`.

```sh
wget --auth-no-challenge --user="${API_USER}" --password="${API_PASSWORD}" "${HOST}/Relativity.REST/api/..."
```

Under token authentication: **First, the destination:** if this Relativity instance is not RelativityOne — including a Relativity Server instance, whose own documentation governs — no migration to RelativityOne applies and the rest of this entry does not apply.

```sh
wget --header="Authorization: Bearer ${ACCESS_TOKEN}" --header="X-CSRF-Header: -" "${HOST}/Relativity.REST/api/..."
```

Remove `--auth-no-challenge`, `--user`, `--password` and any userinfo in the URL.

Credentials: **Where the credentials come from.** The text establishes that a credential is sent as Basic, not that the destination is RelativityOne. If it is not RelativityOne, the migration is that service's own token mechanism, no migration to RelativityOne applies, and the reader stops here. If it is RelativityOne, the token comes from the token-acquisition part that follows, and the user name and password are replaced by the OAuth2 Client's client ID and secret.

**Common to both variants.** **Applies to:** `bash`, `zsh`, `ksh93` and `mksh` (and `sh`, read as `bash`); the blocks need `curl`, `openssl` and `jq`. The text has a common part and two variants of how the script receives the code: the **manual copy** (primary) and a **loopback listener** (optional).

**Sign in.** Replace the code that collected the user's credentials with a redirect: send the user's browser to the authorize endpoint `<host>/Relativity/Identity/connect/authorize`, which leads to the RelativityOne login page. The authorize endpoint is **verified by NSerio against a live instance; not documented by Relativity**. The request carries the standard authorization-code parameters — `client_id`, `response_type=code`, the registered `redirect_uri`, a `scope`, and a `state` value the script checks on return, plus `code_challenge` and `code_challenge_method=S256` when PKCE is used; these are standard OAuth, and Relativity documents none of them. No discovery document is relied on.

**PKCE.** PKCE is required by Relativity's own documentation to obtain a refresh token, and not otherwise: a Code request that does not ask for a refresh token works without it, **verified by NSerio against a live instance; not documented by Relativity**. It is never a blanket requirement. NSerio recommends PKCE with the `S256` method (NSerio recommendation), and does not claim that Relativity accepts any particular method, which it does not document. With `openssl`:

```sh
code_verifier=$(openssl rand -base64 48 | tr -d '\r\n' | tr '+/' '-_' | tr -d '=' | cut -c1-64)
code_challenge=$(printf '%s' "${code_verifier}" | openssl dgst -sha256 -binary | openssl base64 -A | tr '+/' '-_' | tr -d '=')
state=$(openssl rand -hex 16 | tr -d '\r')
```

The pipeline gives a 64-character verifier and a 43-character challenge (*checked by NSerio locally* with OpenSSL 3.5.4 on Windows and OpenSSL 3.0.2 on Linux; the `openssl` that ships with macOS has not been checked).

**Receive the code, by the manual copy (primary).** The browser returns to the Redirect URL registered on the OAuth2 Client with the authorization code and the `state` value. **The script prints the authorize address and the person opens it in any browser; when nothing listens at the registered loopback address the browser shows a connection error and leaves the full address, with `code` and `state`, in its address bar; the person copies that address and pastes it to the script.** This works headless, over SSH, on Linux and macOS, and needs no listener (NSerio recommendation; the browser's behaviour on an unreachable loopback address is *inferred* and has not been checked). The Redirect URL must still be exactly the one registered.

```sh
urlencode() (
	LC_ALL=C
	remaining=$1
	while [ -n "${remaining}" ]; do
		rest=${remaining#?}
		character=${remaining%"${rest}"}
		case ${character} in
			[A-Za-z0-9._~-]) printf '%s' "${character}" ;;
			*) printf '%%%02X' "'${character}" ;;
		esac
		remaining=${rest}
	done
)

authorize_url="${HOST}/Relativity/Identity/connect/authorize?client_id=$(urlencode "${CLIENT_ID}")&response_type=code&redirect_uri=$(urlencode "${REDIRECT_URI}")&scope=$(urlencode "${TOKEN_SCOPE}")&state=${state}&code_challenge=${code_challenge}&code_challenge_method=S256"
printf 'Open this address in a browser and sign in:\n%s\n' "${authorize_url}"
printf 'Then copy the whole address the browser ends on and paste it here: '
IFS= read -r returned_url
```

```sh
query=${returned_url#*\?}
query=${query%%#*}
returned_code=''
returned_state=''
remaining=${query}
while [ -n "${remaining}" ]; do
	pair=${remaining%%'&'*}
	case ${remaining} in
		*'&'*) remaining=${remaining#*'&'} ;;
		*) remaining='' ;;
	esac
	case ${pair} in
		code=*) returned_code=${pair#code=} ;;
		state=*) returned_state=${pair#state=} ;;
	esac
done
[ "${returned_state}" = "${state}" ] || { printf 'The state does not match; stop.\n' >&2; exit 1; }
```

The percent-encoding function and the address parsing give the same output in `bash`, `dash`, `zsh`, `ksh93` and `mksh` (*checked by NSerio locally*). **Check the `state` before using the code.** The code is already percent-encoded in the pasted address, so it is sent with `data` (not `data-urlencode`) below and not encoded again. The block passes the code and the verifier to `curl` as a configuration on standard input (`-K -`), so neither appears on the command line of `curl`; `printf` is a shell built-in and its arguments do not appear in `ps` either.

**Exchange the code.** Send a form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=authorization_code`, the `code`, the same `redirect_uri`, the `client_id` and the `client_secret`, plus the `code_verifier` when PKCE is used. The code is exchanged at that endpoint, which Relativity documents for the Client Credentials grant only; the exchange is **verified by NSerio against a live instance; not documented by Relativity**.

```sh
# The code and the verifier travel on standard input, never on a command line; printf is a built-in and does not appear in ps.
token_response=$(printf '%s\n' \
		"data = \"grant_type=authorization_code\"" \
		"data = \"code=${returned_code}\"" \
		"data-urlencode = \"redirect_uri=${REDIRECT_URI}\"" \
		"data-urlencode = \"client_id=${CLIENT_ID}\"" \
		"data-urlencode = \"code_verifier=${code_verifier}\"" \
		"data-urlencode = \"client_secret@${CLIENT_SECRET_FILE}\"" \
	| curl --silent --show-error --fail -K - \
		"${HOST}/Relativity/Identity/connect/token")
ACCESS_TOKEN=$(printf '%s' "${token_response}" | jq -r '.access_token')
```

**Use the token.** Send `Authorization: Bearer <access token>` — the change at the use site above — with `X-CSRF-Header: -` on REST and Kepler calls, from a header file as for Client Credentials: `( umask 077; printf 'Authorization: Bearer %s\nX-CSRF-Header: -\n' "${ACCESS_TOKEN}" > "${HEADER_FILE}" )`, then `curl -H @"${HEADER_FILE}"`.

**On expiry.** With Allow Refresh Tokens enabled on the OAuth2 Client and PKCE used, the script holds a refresh token and, when the access token expires, sends a `grant_type=refresh_token` request with it and the client credentials in place of the code exchange — *inferred* from the standard OAuth shape, not tested. Without a refresh token the person signs in again. Relativity does not document a refresh token's lifetime, whether it rotates, how it is revoked, whether a revocation endpoint exists, or what revoking it does to access tokens already issued. **A refresh token is a secret:** keep it in a file created under `umask 077` or in the keychain, never on a command line.

**Scope.** No scope set is prescribed for Code. The scope `openid RelativityRequestOrigin UserInfo UserInfoAccess AuthenticationID RelativityWeb` works, **verified by NSerio against a live instance; not documented by Relativity**, verified as working and not as minimal. `state` is sent as standard OAuth practice and `nonce` is not used; Relativity does not document whether it enforces either.

**The script stops collecting the password.** After this change the script never receives, stores or forwards the user's user name and password: the user signs in on RelativityOne's own page, and the script holds only tokens. Remove the `read` that prompts for the password, the `stty -echo` around it and any storage of the password the replaced code needed.

**Single sign-on.** Sign-in happens on RelativityOne's own login page, so the instance's single sign-on and two-factor rules apply unchanged — *inferred* from where the sign-in happens, and not documented for clients that customers create.

**The client secret is required.** Code does not work without a client secret: tested and rejected, **verified by NSerio against a live instance; not documented by Relativity**, which confirms Relativity's own statement that the grant requires one. **A script is plain text, so a secret shipped inside a distributed script can be read by anyone who holds the script**; it protects nothing against the person running it. Store it as Secret Management (Part B of the Migration Documentation) describes, in a file with the person's own permissions (`umask 077`) or in the keychain, and apply the compensating guidance below.

**Loopback listener variant (optional, per system).** A shell has no HTTP listener of its own. Where a script runs on a machine with a browser and a registered fixed-port loopback Redirect URL `http://127.0.0.1:<port>/<path>` — a plain-HTTP loopback URL is accepted, **verified by NSerio against a live instance; not documented by Relativity** — it can start `nc` or `socat` bound to `127.0.0.1` only, for one request, to read the request line `GET /<path>?code=…&state=… HTTP/1.1` and answer with a short page: on Linux the OpenBSD `nc` does it with `nc -l 127.0.0.1 PORT` (*checked by NSerio locally*); the options differ between OpenBSD, BSD and macOS, GNU and traditional `nc` and `ncat`, and `socat`, and those have not been checked. Bind to loopback only, check the `state` (another local process can connect to the port), and fall back to the manual copy when the tool is missing or the machine is headless.

**Compensating guidance** (NSerio recommendation, subject to your own security policy): set Trusted IPs where the users' outbound addresses are known and stable — they restrict where a token can be *requested* — use the recommended short Access Token Lifetime of 60 minutes, enable refresh tokens only when needed, and create one OAuth2 Client per distribution, so a leaked secret can be disabled without affecting the others. **A documentation gap.** Relativity documents no recommendation for a distributed script or desktop application; this design is what NSerio verified works.

Verification: **Verification note.** **A token has a finite lifetime.** `wget` has no `-H @FILE` form, so the token is on the command line while `wget` runs, visible in `ps`: on a shared host use `curl` for this call, or a `wget` configuration file created under `umask 077` that holds the header (documented in the GNU Wget manual; not checked here).

OAuth2 Client: See [OAuth2 Client for Authorization Code](#oauth2-client-for-authorization-code).
<!-- generated:end SH-004 authorization-code -->

### SH-005 · CurlUrlUserinfo

<a id="sh-005-migration"></a>

#### Client Credentials

<!-- generated:begin SH-005 client-credentials -->
**Flow.** This entry is for Client Credentials. The "Which flow" paragraph of SH-005 in [Part A](part-a-shell.md#sh-005) says when a match belongs to this flow.

Source form: **Applies to:** `bash`, `zsh`, `ksh93` and `mksh` (and `sh`, read as `bash`); `curl`.

```sh
curl --silent --show-error "https://${API_USER}:${API_PASSWORD}@host.example/Relativity.REST/api/..."
```

Under token authentication: **First, the destination:** if this Relativity instance is not RelativityOne — including a Relativity Server instance, whose own documentation governs — no migration to RelativityOne applies and the rest of this entry does not apply.

```sh
curl --silent --show-error --fail -H @"${HEADER_FILE}" "https://host.example/Relativity.REST/api/..."
```

Remove the userinfo from the URL: it is also written to logs and to the process list.

Credentials: **Where the credentials come from.** The text establishes that a credential is sent as Basic, not that the destination is RelativityOne. If it is not RelativityOne, the migration is that service's own token mechanism, no migration to RelativityOne applies, and the reader stops here. If it is RelativityOne, the token comes from the token-acquisition part that follows, and the user name and password are replaced by the OAuth2 Client's client ID and secret.

**Where the token comes from.** Create an OAuth2 Client with Flow Grant Type set to Client Credentials (part 3 lists its settings), then send a form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=client_credentials`, `client_id`, `client_secret` and a `scope`. The token endpoint is **documented** for this grant. The token is then sent as `Authorization: Bearer <token>`, with `X-CSRF-Header: -` on REST and Kepler calls — the header is documented as required, and a Kepler call without it returns 404. **Applies to:** `bash`, `zsh`, `ksh93` and `mksh` (and `sh`, read as `bash`); the blocks need `curl` and `jq`, which is a stated prerequisite: no fallback that parses the JSON with a regular expression is recommended.

```sh
# HOST, CLIENT_ID, TOKEN_SCOPE and CLIENT_SECRET_FILE come from the caller's environment; the secret file is created as Secret Management (Part B of the Migration Documentation) describes.
acquire_access_token() {
	curl --silent --show-error --fail \
		--data-urlencode "grant_type=client_credentials" \
		--data-urlencode "client_id=${CLIENT_ID}" \
		--data-urlencode "client_secret@${CLIENT_SECRET_FILE}" \
		--data-urlencode "scope=${TOKEN_SCOPE}" \
		"${HOST}/Relativity/Identity/connect/token" \
		| jq -r '.access_token'
}
ACCESS_TOKEN=$(acquire_access_token)
```

`--data-urlencode "client_secret@FILE"` reads the secret from a file and keeps it out of the command line (*checked by NSerio locally*: the file is read whole, a trailing line feed included, so create it with `printf '%s'`); `curl -K -` with the same setting on standard input also works (*checked by NSerio locally*). **The scope depends on what the integration needs, and no single scope is required.** Relativity's documentation shows two values for this grant: `SystemUserInfo` on the REST authentication page, the Usage Reports page and in the official Transfer SDK sample, and `UserInfoAccess` in the Kepler .NET samples. No page says which is correct, whether both work, or what either grants, and the OAuth2 Client's `Scopes` property is documented as one that cannot be set. Use the value the API you call documents, and confirm it.

**Use the token without putting it on the command line.**

```sh
( umask 077; printf 'Authorization: Bearer %s\nX-CSRF-Header: -\n' "${ACCESS_TOKEN}" > "${HEADER_FILE}" )
curl --silent --show-error --fail -H @"${HEADER_FILE}" "${HOST}/Relativity.REST/api/..."
```

`curl -H @FILE` sends the header lines of a file (*checked by NSerio locally* on `curl` 8.17.0 and 7.81.0; documented from `curl` 7.55). A token in `-H "Authorization: Bearer $TOKEN"` is visible to every user of the machine in `ps` while the command runs, so the file form is the recommended one for a shared host.

**Cache the token, and re-acquire it on expiry — do not refresh.** Keep the token for its lifetime (part 3 recommends 60 minutes, Relativity's own wording) and request a new one with the client secret shortly before it expires (a margin of 60 seconds is reasonable) and once when a call is rejected for authentication. Client Credentials offers no refresh tokens: **verified by NSerio against a live instance; not documented by Relativity** — the Allow Refresh Tokens control does not appear for this grant in the RelativityOne user interface. Relativity's own tables and text on refresh tokens conflict with each other, so re-acquisition with the secret is the advice that holds whichever way they are read. A script that runs for longer than the token's lifetime must acquire again, not hold the first token.

**The client secret.** It is displayed once, at creation or reset. Store it as Secret Management (Part B of the Migration Documentation) describes; in a shell estate that means **a file created under `umask 077`** or the operating system's keychain, **never in the script, never in a tracked `.env` file, and never exported to child processes**. A reset invalidates the previous secret immediately, so plan the cut-over before resetting it.

**Before deploying, confirm the calling identity.** Every call now runs as the OAuth2 Client's **Context User**, not as the user in the replaced credential. If that user has different workspace permissions or item-level security, the script returns fewer rows or `Unauthorized` in production with no change to its logic, and Relativity's audit attributes every call to that one account. Confirm the Context User's permissions match those of the previous identity.

**When the script acts for a signed-in user.** Client Credentials is one service identity by construction. A script that prompts a person for a password and sends it is the Authorization Code case, and moving to it means a new OAuth2 Client, because the flow is fixed when a client is created. Which of the two flows applies depends on who supplies the credential at run time.

Verification: **Verification note.** **A token has a finite lifetime.** **A URL with userinfo is read from other places too**: a proxy or a log that recorded it holds the credential, and the credential must be rotated, not only removed.

OAuth2 Client: See [OAuth2 Client for Client Credentials](#oauth2-client-for-client-credentials).
<!-- generated:end SH-005 client-credentials -->

#### Authorization Code

<!-- generated:begin SH-005 authorization-code -->
**Flow.** This entry is for Authorization Code. The "Which flow" paragraph of SH-005 in [Part A](part-a-shell.md#sh-005) says when a match belongs to this flow.

Source form: **Applies to:** `bash`, `zsh`, `ksh93` and `mksh` (and `sh`, read as `bash`); `curl`.

```sh
curl --silent --show-error "https://${API_USER}:${API_PASSWORD}@host.example/Relativity.REST/api/..."
```

Under token authentication: **First, the destination:** if this Relativity instance is not RelativityOne — including a Relativity Server instance, whose own documentation governs — no migration to RelativityOne applies and the rest of this entry does not apply.

```sh
curl --silent --show-error --fail -H @"${HEADER_FILE}" "https://host.example/Relativity.REST/api/..."
```

Remove the userinfo from the URL: it is also written to logs and to the process list.

Credentials: **Where the credentials come from.** The text establishes that a credential is sent as Basic, not that the destination is RelativityOne. If it is not RelativityOne, the migration is that service's own token mechanism, no migration to RelativityOne applies, and the reader stops here. If it is RelativityOne, the token comes from the token-acquisition part that follows, and the user name and password are replaced by the OAuth2 Client's client ID and secret.

**Common to both variants.** **Applies to:** `bash`, `zsh`, `ksh93` and `mksh` (and `sh`, read as `bash`); the blocks need `curl`, `openssl` and `jq`. The text has a common part and two variants of how the script receives the code: the **manual copy** (primary) and a **loopback listener** (optional).

**Sign in.** Replace the code that collected the user's credentials with a redirect: send the user's browser to the authorize endpoint `<host>/Relativity/Identity/connect/authorize`, which leads to the RelativityOne login page. The authorize endpoint is **verified by NSerio against a live instance; not documented by Relativity**. The request carries the standard authorization-code parameters — `client_id`, `response_type=code`, the registered `redirect_uri`, a `scope`, and a `state` value the script checks on return, plus `code_challenge` and `code_challenge_method=S256` when PKCE is used; these are standard OAuth, and Relativity documents none of them. No discovery document is relied on.

**PKCE.** PKCE is required by Relativity's own documentation to obtain a refresh token, and not otherwise: a Code request that does not ask for a refresh token works without it, **verified by NSerio against a live instance; not documented by Relativity**. It is never a blanket requirement. NSerio recommends PKCE with the `S256` method (NSerio recommendation), and does not claim that Relativity accepts any particular method, which it does not document. With `openssl`:

```sh
code_verifier=$(openssl rand -base64 48 | tr -d '\r\n' | tr '+/' '-_' | tr -d '=' | cut -c1-64)
code_challenge=$(printf '%s' "${code_verifier}" | openssl dgst -sha256 -binary | openssl base64 -A | tr '+/' '-_' | tr -d '=')
state=$(openssl rand -hex 16 | tr -d '\r')
```

The pipeline gives a 64-character verifier and a 43-character challenge (*checked by NSerio locally* with OpenSSL 3.5.4 on Windows and OpenSSL 3.0.2 on Linux; the `openssl` that ships with macOS has not been checked).

**Receive the code, by the manual copy (primary).** The browser returns to the Redirect URL registered on the OAuth2 Client with the authorization code and the `state` value. **The script prints the authorize address and the person opens it in any browser; when nothing listens at the registered loopback address the browser shows a connection error and leaves the full address, with `code` and `state`, in its address bar; the person copies that address and pastes it to the script.** This works headless, over SSH, on Linux and macOS, and needs no listener (NSerio recommendation; the browser's behaviour on an unreachable loopback address is *inferred* and has not been checked). The Redirect URL must still be exactly the one registered.

```sh
urlencode() (
	LC_ALL=C
	remaining=$1
	while [ -n "${remaining}" ]; do
		rest=${remaining#?}
		character=${remaining%"${rest}"}
		case ${character} in
			[A-Za-z0-9._~-]) printf '%s' "${character}" ;;
			*) printf '%%%02X' "'${character}" ;;
		esac
		remaining=${rest}
	done
)

authorize_url="${HOST}/Relativity/Identity/connect/authorize?client_id=$(urlencode "${CLIENT_ID}")&response_type=code&redirect_uri=$(urlencode "${REDIRECT_URI}")&scope=$(urlencode "${TOKEN_SCOPE}")&state=${state}&code_challenge=${code_challenge}&code_challenge_method=S256"
printf 'Open this address in a browser and sign in:\n%s\n' "${authorize_url}"
printf 'Then copy the whole address the browser ends on and paste it here: '
IFS= read -r returned_url
```

```sh
query=${returned_url#*\?}
query=${query%%#*}
returned_code=''
returned_state=''
remaining=${query}
while [ -n "${remaining}" ]; do
	pair=${remaining%%'&'*}
	case ${remaining} in
		*'&'*) remaining=${remaining#*'&'} ;;
		*) remaining='' ;;
	esac
	case ${pair} in
		code=*) returned_code=${pair#code=} ;;
		state=*) returned_state=${pair#state=} ;;
	esac
done
[ "${returned_state}" = "${state}" ] || { printf 'The state does not match; stop.\n' >&2; exit 1; }
```

The percent-encoding function and the address parsing give the same output in `bash`, `dash`, `zsh`, `ksh93` and `mksh` (*checked by NSerio locally*). **Check the `state` before using the code.** The code is already percent-encoded in the pasted address, so it is sent with `data` (not `data-urlencode`) below and not encoded again. The block passes the code and the verifier to `curl` as a configuration on standard input (`-K -`), so neither appears on the command line of `curl`; `printf` is a shell built-in and its arguments do not appear in `ps` either.

**Exchange the code.** Send a form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=authorization_code`, the `code`, the same `redirect_uri`, the `client_id` and the `client_secret`, plus the `code_verifier` when PKCE is used. The code is exchanged at that endpoint, which Relativity documents for the Client Credentials grant only; the exchange is **verified by NSerio against a live instance; not documented by Relativity**.

```sh
# The code and the verifier travel on standard input, never on a command line; printf is a built-in and does not appear in ps.
token_response=$(printf '%s\n' \
		"data = \"grant_type=authorization_code\"" \
		"data = \"code=${returned_code}\"" \
		"data-urlencode = \"redirect_uri=${REDIRECT_URI}\"" \
		"data-urlencode = \"client_id=${CLIENT_ID}\"" \
		"data-urlencode = \"code_verifier=${code_verifier}\"" \
		"data-urlencode = \"client_secret@${CLIENT_SECRET_FILE}\"" \
	| curl --silent --show-error --fail -K - \
		"${HOST}/Relativity/Identity/connect/token")
ACCESS_TOKEN=$(printf '%s' "${token_response}" | jq -r '.access_token')
```

**Use the token.** Send `Authorization: Bearer <access token>` — the change at the use site above — with `X-CSRF-Header: -` on REST and Kepler calls, from a header file as for Client Credentials: `( umask 077; printf 'Authorization: Bearer %s\nX-CSRF-Header: -\n' "${ACCESS_TOKEN}" > "${HEADER_FILE}" )`, then `curl -H @"${HEADER_FILE}"`.

**On expiry.** With Allow Refresh Tokens enabled on the OAuth2 Client and PKCE used, the script holds a refresh token and, when the access token expires, sends a `grant_type=refresh_token` request with it and the client credentials in place of the code exchange — *inferred* from the standard OAuth shape, not tested. Without a refresh token the person signs in again. Relativity does not document a refresh token's lifetime, whether it rotates, how it is revoked, whether a revocation endpoint exists, or what revoking it does to access tokens already issued. **A refresh token is a secret:** keep it in a file created under `umask 077` or in the keychain, never on a command line.

**Scope.** No scope set is prescribed for Code. The scope `openid RelativityRequestOrigin UserInfo UserInfoAccess AuthenticationID RelativityWeb` works, **verified by NSerio against a live instance; not documented by Relativity**, verified as working and not as minimal. `state` is sent as standard OAuth practice and `nonce` is not used; Relativity does not document whether it enforces either.

**The script stops collecting the password.** After this change the script never receives, stores or forwards the user's user name and password: the user signs in on RelativityOne's own page, and the script holds only tokens. Remove the `read` that prompts for the password, the `stty -echo` around it and any storage of the password the replaced code needed.

**Single sign-on.** Sign-in happens on RelativityOne's own login page, so the instance's single sign-on and two-factor rules apply unchanged — *inferred* from where the sign-in happens, and not documented for clients that customers create.

**The client secret is required.** Code does not work without a client secret: tested and rejected, **verified by NSerio against a live instance; not documented by Relativity**, which confirms Relativity's own statement that the grant requires one. **A script is plain text, so a secret shipped inside a distributed script can be read by anyone who holds the script**; it protects nothing against the person running it. Store it as Secret Management (Part B of the Migration Documentation) describes, in a file with the person's own permissions (`umask 077`) or in the keychain, and apply the compensating guidance below.

**Loopback listener variant (optional, per system).** A shell has no HTTP listener of its own. Where a script runs on a machine with a browser and a registered fixed-port loopback Redirect URL `http://127.0.0.1:<port>/<path>` — a plain-HTTP loopback URL is accepted, **verified by NSerio against a live instance; not documented by Relativity** — it can start `nc` or `socat` bound to `127.0.0.1` only, for one request, to read the request line `GET /<path>?code=…&state=… HTTP/1.1` and answer with a short page: on Linux the OpenBSD `nc` does it with `nc -l 127.0.0.1 PORT` (*checked by NSerio locally*); the options differ between OpenBSD, BSD and macOS, GNU and traditional `nc` and `ncat`, and `socat`, and those have not been checked. Bind to loopback only, check the `state` (another local process can connect to the port), and fall back to the manual copy when the tool is missing or the machine is headless.

**Compensating guidance** (NSerio recommendation, subject to your own security policy): set Trusted IPs where the users' outbound addresses are known and stable — they restrict where a token can be *requested* — use the recommended short Access Token Lifetime of 60 minutes, enable refresh tokens only when needed, and create one OAuth2 Client per distribution, so a leaked secret can be disabled without affecting the others. **A documentation gap.** Relativity documents no recommendation for a distributed script or desktop application; this design is what NSerio verified works.

Verification: **Verification note.** **A token has a finite lifetime.** **A URL with userinfo is read from other places too**: a proxy or a log that recorded it holds the credential, and the credential must be rotated, not only removed.

OAuth2 Client: See [OAuth2 Client for Authorization Code](#oauth2-client-for-authorization-code).
<!-- generated:end SH-005 authorization-code -->

### SH-006 · CurlNetrcOption

<a id="sh-006-migration"></a>

#### Client Credentials

<!-- generated:begin SH-006 client-credentials -->
**Flow.** This entry is for Client Credentials. The "Which flow" paragraph of SH-006 in [Part A](part-a-shell.md#sh-006) says when a match belongs to this flow.

Source form: **Applies to:** `bash`, `zsh`, `ksh93` and `mksh` (and `sh`, read as `bash`); `curl`.

```sh
curl --silent --show-error --netrc "${HOST}/Relativity.REST/api/..."
```

Under token authentication: **First, the destination:** if this Relativity instance is not RelativityOne — including a Relativity Server instance, whose own documentation governs — no migration to RelativityOne applies and the rest of this entry does not apply.

```sh
curl --silent --show-error --fail -H @"${HEADER_FILE}" "${HOST}/Relativity.REST/api/..."
```

Remove `-n`, `--netrc`, `--netrc-file` and `--netrc-optional`.

Credentials: **Where the credentials come from.** The `.netrc` entry holds the Basic credential this match cannot see. If the destination is RelativityOne, replace it by the token of the token-acquisition part that follows, and delete the entry.

**Where the token comes from.** Create an OAuth2 Client with Flow Grant Type set to Client Credentials (part 3 lists its settings), then send a form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=client_credentials`, `client_id`, `client_secret` and a `scope`. The token endpoint is **documented** for this grant. The token is then sent as `Authorization: Bearer <token>`, with `X-CSRF-Header: -` on REST and Kepler calls — the header is documented as required, and a Kepler call without it returns 404. **Applies to:** `bash`, `zsh`, `ksh93` and `mksh` (and `sh`, read as `bash`); the blocks need `curl` and `jq`, which is a stated prerequisite: no fallback that parses the JSON with a regular expression is recommended.

```sh
# HOST, CLIENT_ID, TOKEN_SCOPE and CLIENT_SECRET_FILE come from the caller's environment; the secret file is created as Secret Management (Part B of the Migration Documentation) describes.
acquire_access_token() {
	curl --silent --show-error --fail \
		--data-urlencode "grant_type=client_credentials" \
		--data-urlencode "client_id=${CLIENT_ID}" \
		--data-urlencode "client_secret@${CLIENT_SECRET_FILE}" \
		--data-urlencode "scope=${TOKEN_SCOPE}" \
		"${HOST}/Relativity/Identity/connect/token" \
		| jq -r '.access_token'
}
ACCESS_TOKEN=$(acquire_access_token)
```

`--data-urlencode "client_secret@FILE"` reads the secret from a file and keeps it out of the command line (*checked by NSerio locally*: the file is read whole, a trailing line feed included, so create it with `printf '%s'`); `curl -K -` with the same setting on standard input also works (*checked by NSerio locally*). **The scope depends on what the integration needs, and no single scope is required.** Relativity's documentation shows two values for this grant: `SystemUserInfo` on the REST authentication page, the Usage Reports page and in the official Transfer SDK sample, and `UserInfoAccess` in the Kepler .NET samples. No page says which is correct, whether both work, or what either grants, and the OAuth2 Client's `Scopes` property is documented as one that cannot be set. Use the value the API you call documents, and confirm it.

**Use the token without putting it on the command line.**

```sh
( umask 077; printf 'Authorization: Bearer %s\nX-CSRF-Header: -\n' "${ACCESS_TOKEN}" > "${HEADER_FILE}" )
curl --silent --show-error --fail -H @"${HEADER_FILE}" "${HOST}/Relativity.REST/api/..."
```

`curl -H @FILE` sends the header lines of a file (*checked by NSerio locally* on `curl` 8.17.0 and 7.81.0; documented from `curl` 7.55). A token in `-H "Authorization: Bearer $TOKEN"` is visible to every user of the machine in `ps` while the command runs, so the file form is the recommended one for a shared host.

**Cache the token, and re-acquire it on expiry — do not refresh.** Keep the token for its lifetime (part 3 recommends 60 minutes, Relativity's own wording) and request a new one with the client secret shortly before it expires (a margin of 60 seconds is reasonable) and once when a call is rejected for authentication. Client Credentials offers no refresh tokens: **verified by NSerio against a live instance; not documented by Relativity** — the Allow Refresh Tokens control does not appear for this grant in the RelativityOne user interface. Relativity's own tables and text on refresh tokens conflict with each other, so re-acquisition with the secret is the advice that holds whichever way they are read. A script that runs for longer than the token's lifetime must acquire again, not hold the first token.

**The client secret.** It is displayed once, at creation or reset. Store it as Secret Management (Part B of the Migration Documentation) describes; in a shell estate that means **a file created under `umask 077`** or the operating system's keychain, **never in the script, never in a tracked `.env` file, and never exported to child processes**. A reset invalidates the previous secret immediately, so plan the cut-over before resetting it.

**Before deploying, confirm the calling identity.** Every call now runs as the OAuth2 Client's **Context User**, not as the user in the replaced credential. If that user has different workspace permissions or item-level security, the script returns fewer rows or `Unauthorized` in production with no change to its logic, and Relativity's audit attributes every call to that one account. Confirm the Context User's permissions match those of the previous identity.

**When the script acts for a signed-in user.** Client Credentials is one service identity by construction. A script that prompts a person for a password and sends it is the Authorization Code case, and moving to it means a new OAuth2 Client, because the flow is fixed when a client is created. Which of the two flows applies depends on who supplies the credential at run time.

Verification: **Verification note.** **The `.netrc` file is not read**, so whether it holds a credential for RelativityOne is for the reader to check; `--netrc-optional` may find none. **A token has a finite lifetime.**

OAuth2 Client: See [OAuth2 Client for Client Credentials](#oauth2-client-for-client-credentials).
<!-- generated:end SH-006 client-credentials -->

#### Authorization Code

<!-- generated:begin SH-006 authorization-code -->
**Flow.** This entry is for Authorization Code. The "Which flow" paragraph of SH-006 in [Part A](part-a-shell.md#sh-006) says when a match belongs to this flow.

Source form: **Applies to:** `bash`, `zsh`, `ksh93` and `mksh` (and `sh`, read as `bash`); `curl`.

```sh
curl --silent --show-error --netrc "${HOST}/Relativity.REST/api/..."
```

Under token authentication: **First, the destination:** if this Relativity instance is not RelativityOne — including a Relativity Server instance, whose own documentation governs — no migration to RelativityOne applies and the rest of this entry does not apply.

```sh
curl --silent --show-error --fail -H @"${HEADER_FILE}" "${HOST}/Relativity.REST/api/..."
```

Remove `-n`, `--netrc`, `--netrc-file` and `--netrc-optional`.

Credentials: **Where the credentials come from.** The `.netrc` entry holds the Basic credential this match cannot see. If the destination is RelativityOne, replace it by the token of the token-acquisition part that follows, and delete the entry.

**Common to both variants.** **Applies to:** `bash`, `zsh`, `ksh93` and `mksh` (and `sh`, read as `bash`); the blocks need `curl`, `openssl` and `jq`. The text has a common part and two variants of how the script receives the code: the **manual copy** (primary) and a **loopback listener** (optional).

**Sign in.** Replace the code that collected the user's credentials with a redirect: send the user's browser to the authorize endpoint `<host>/Relativity/Identity/connect/authorize`, which leads to the RelativityOne login page. The authorize endpoint is **verified by NSerio against a live instance; not documented by Relativity**. The request carries the standard authorization-code parameters — `client_id`, `response_type=code`, the registered `redirect_uri`, a `scope`, and a `state` value the script checks on return, plus `code_challenge` and `code_challenge_method=S256` when PKCE is used; these are standard OAuth, and Relativity documents none of them. No discovery document is relied on.

**PKCE.** PKCE is required by Relativity's own documentation to obtain a refresh token, and not otherwise: a Code request that does not ask for a refresh token works without it, **verified by NSerio against a live instance; not documented by Relativity**. It is never a blanket requirement. NSerio recommends PKCE with the `S256` method (NSerio recommendation), and does not claim that Relativity accepts any particular method, which it does not document. With `openssl`:

```sh
code_verifier=$(openssl rand -base64 48 | tr -d '\r\n' | tr '+/' '-_' | tr -d '=' | cut -c1-64)
code_challenge=$(printf '%s' "${code_verifier}" | openssl dgst -sha256 -binary | openssl base64 -A | tr '+/' '-_' | tr -d '=')
state=$(openssl rand -hex 16 | tr -d '\r')
```

The pipeline gives a 64-character verifier and a 43-character challenge (*checked by NSerio locally* with OpenSSL 3.5.4 on Windows and OpenSSL 3.0.2 on Linux; the `openssl` that ships with macOS has not been checked).

**Receive the code, by the manual copy (primary).** The browser returns to the Redirect URL registered on the OAuth2 Client with the authorization code and the `state` value. **The script prints the authorize address and the person opens it in any browser; when nothing listens at the registered loopback address the browser shows a connection error and leaves the full address, with `code` and `state`, in its address bar; the person copies that address and pastes it to the script.** This works headless, over SSH, on Linux and macOS, and needs no listener (NSerio recommendation; the browser's behaviour on an unreachable loopback address is *inferred* and has not been checked). The Redirect URL must still be exactly the one registered.

```sh
urlencode() (
	LC_ALL=C
	remaining=$1
	while [ -n "${remaining}" ]; do
		rest=${remaining#?}
		character=${remaining%"${rest}"}
		case ${character} in
			[A-Za-z0-9._~-]) printf '%s' "${character}" ;;
			*) printf '%%%02X' "'${character}" ;;
		esac
		remaining=${rest}
	done
)

authorize_url="${HOST}/Relativity/Identity/connect/authorize?client_id=$(urlencode "${CLIENT_ID}")&response_type=code&redirect_uri=$(urlencode "${REDIRECT_URI}")&scope=$(urlencode "${TOKEN_SCOPE}")&state=${state}&code_challenge=${code_challenge}&code_challenge_method=S256"
printf 'Open this address in a browser and sign in:\n%s\n' "${authorize_url}"
printf 'Then copy the whole address the browser ends on and paste it here: '
IFS= read -r returned_url
```

```sh
query=${returned_url#*\?}
query=${query%%#*}
returned_code=''
returned_state=''
remaining=${query}
while [ -n "${remaining}" ]; do
	pair=${remaining%%'&'*}
	case ${remaining} in
		*'&'*) remaining=${remaining#*'&'} ;;
		*) remaining='' ;;
	esac
	case ${pair} in
		code=*) returned_code=${pair#code=} ;;
		state=*) returned_state=${pair#state=} ;;
	esac
done
[ "${returned_state}" = "${state}" ] || { printf 'The state does not match; stop.\n' >&2; exit 1; }
```

The percent-encoding function and the address parsing give the same output in `bash`, `dash`, `zsh`, `ksh93` and `mksh` (*checked by NSerio locally*). **Check the `state` before using the code.** The code is already percent-encoded in the pasted address, so it is sent with `data` (not `data-urlencode`) below and not encoded again. The block passes the code and the verifier to `curl` as a configuration on standard input (`-K -`), so neither appears on the command line of `curl`; `printf` is a shell built-in and its arguments do not appear in `ps` either.

**Exchange the code.** Send a form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=authorization_code`, the `code`, the same `redirect_uri`, the `client_id` and the `client_secret`, plus the `code_verifier` when PKCE is used. The code is exchanged at that endpoint, which Relativity documents for the Client Credentials grant only; the exchange is **verified by NSerio against a live instance; not documented by Relativity**.

```sh
# The code and the verifier travel on standard input, never on a command line; printf is a built-in and does not appear in ps.
token_response=$(printf '%s\n' \
		"data = \"grant_type=authorization_code\"" \
		"data = \"code=${returned_code}\"" \
		"data-urlencode = \"redirect_uri=${REDIRECT_URI}\"" \
		"data-urlencode = \"client_id=${CLIENT_ID}\"" \
		"data-urlencode = \"code_verifier=${code_verifier}\"" \
		"data-urlencode = \"client_secret@${CLIENT_SECRET_FILE}\"" \
	| curl --silent --show-error --fail -K - \
		"${HOST}/Relativity/Identity/connect/token")
ACCESS_TOKEN=$(printf '%s' "${token_response}" | jq -r '.access_token')
```

**Use the token.** Send `Authorization: Bearer <access token>` — the change at the use site above — with `X-CSRF-Header: -` on REST and Kepler calls, from a header file as for Client Credentials: `( umask 077; printf 'Authorization: Bearer %s\nX-CSRF-Header: -\n' "${ACCESS_TOKEN}" > "${HEADER_FILE}" )`, then `curl -H @"${HEADER_FILE}"`.

**On expiry.** With Allow Refresh Tokens enabled on the OAuth2 Client and PKCE used, the script holds a refresh token and, when the access token expires, sends a `grant_type=refresh_token` request with it and the client credentials in place of the code exchange — *inferred* from the standard OAuth shape, not tested. Without a refresh token the person signs in again. Relativity does not document a refresh token's lifetime, whether it rotates, how it is revoked, whether a revocation endpoint exists, or what revoking it does to access tokens already issued. **A refresh token is a secret:** keep it in a file created under `umask 077` or in the keychain, never on a command line.

**Scope.** No scope set is prescribed for Code. The scope `openid RelativityRequestOrigin UserInfo UserInfoAccess AuthenticationID RelativityWeb` works, **verified by NSerio against a live instance; not documented by Relativity**, verified as working and not as minimal. `state` is sent as standard OAuth practice and `nonce` is not used; Relativity does not document whether it enforces either.

**The script stops collecting the password.** After this change the script never receives, stores or forwards the user's user name and password: the user signs in on RelativityOne's own page, and the script holds only tokens. Remove the `read` that prompts for the password, the `stty -echo` around it and any storage of the password the replaced code needed.

**Single sign-on.** Sign-in happens on RelativityOne's own login page, so the instance's single sign-on and two-factor rules apply unchanged — *inferred* from where the sign-in happens, and not documented for clients that customers create.

**The client secret is required.** Code does not work without a client secret: tested and rejected, **verified by NSerio against a live instance; not documented by Relativity**, which confirms Relativity's own statement that the grant requires one. **A script is plain text, so a secret shipped inside a distributed script can be read by anyone who holds the script**; it protects nothing against the person running it. Store it as Secret Management (Part B of the Migration Documentation) describes, in a file with the person's own permissions (`umask 077`) or in the keychain, and apply the compensating guidance below.

**Loopback listener variant (optional, per system).** A shell has no HTTP listener of its own. Where a script runs on a machine with a browser and a registered fixed-port loopback Redirect URL `http://127.0.0.1:<port>/<path>` — a plain-HTTP loopback URL is accepted, **verified by NSerio against a live instance; not documented by Relativity** — it can start `nc` or `socat` bound to `127.0.0.1` only, for one request, to read the request line `GET /<path>?code=…&state=… HTTP/1.1` and answer with a short page: on Linux the OpenBSD `nc` does it with `nc -l 127.0.0.1 PORT` (*checked by NSerio locally*); the options differ between OpenBSD, BSD and macOS, GNU and traditional `nc` and `ncat`, and `socat`, and those have not been checked. Bind to loopback only, check the `state` (another local process can connect to the port), and fall back to the manual copy when the tool is missing or the machine is headless.

**Compensating guidance** (NSerio recommendation, subject to your own security policy): set Trusted IPs where the users' outbound addresses are known and stable — they restrict where a token can be *requested* — use the recommended short Access Token Lifetime of 60 minutes, enable refresh tokens only when needed, and create one OAuth2 Client per distribution, so a leaked secret can be disabled without affecting the others. **A documentation gap.** Relativity documents no recommendation for a distributed script or desktop application; this design is what NSerio verified works.

Verification: **Verification note.** **The `.netrc` file is not read**, so whether it holds a credential for RelativityOne is for the reader to check; `--netrc-optional` may find none. **A token has a finite lifetime.**

OAuth2 Client: See [OAuth2 Client for Authorization Code](#oauth2-client-for-authorization-code).
<!-- generated:end SH-006 authorization-code -->

### SH-007 · EncodedCredentialPair

<a id="sh-007-migration"></a>

#### Client Credentials

<!-- generated:begin SH-007 client-credentials -->
**Flow.** This entry is for Client Credentials. The "Which flow" paragraph of SH-007 in [Part A](part-a-shell.md#sh-007) says when a match belongs to this flow.

Source form: **Applies to:** `bash`, `zsh`, `ksh93` and `mksh` (and `sh`, read as `bash`); `base64` and `openssl` (their options differ by system).

```sh
ENCODED=$(printf '%s:%s' "${API_USER}" "${API_PASSWORD}" | base64 | tr -d '\n')
```

Under token authentication: **First, the destination:** if this Relativity instance is not RelativityOne — including a Relativity Server instance, whose own documentation governs — no migration to RelativityOne applies and the rest of this entry does not apply. **If the encoded pair is not a credential** (an identifier and a version, for example; a host and a port is not read), this match is a false positive and nothing changes.

Remove the encoding and send the token from a header file: `-H @"${HEADER_FILE}"`, as in the target form for a header that names the scheme. The here-string form (`<<<`) that some scripts use to feed the pair to the encoder is supported by `bash`, `zsh`, `ksh93` and `mksh` (and by `sh`, read as `bash`) and not by POSIX `sh`; a script that must run under POSIX `sh` pipes the text instead. A token written as `-H "Authorization: Bearer ..."` is visible in `ps` while the command runs.

Credentials: **Where the credentials come from.** The text establishes that a credential is sent as Basic, not that the destination is RelativityOne. If it is not RelativityOne, the migration is that service's own token mechanism, no migration to RelativityOne applies, and the reader stops here. If it is RelativityOne, the token comes from the token-acquisition part that follows, and the user name and password are replaced by the OAuth2 Client's client ID and secret.

**Where the token comes from.** Create an OAuth2 Client with Flow Grant Type set to Client Credentials (part 3 lists its settings), then send a form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=client_credentials`, `client_id`, `client_secret` and a `scope`. The token endpoint is **documented** for this grant. The token is then sent as `Authorization: Bearer <token>`, with `X-CSRF-Header: -` on REST and Kepler calls — the header is documented as required, and a Kepler call without it returns 404. **Applies to:** `bash`, `zsh`, `ksh93` and `mksh` (and `sh`, read as `bash`); the blocks need `curl` and `jq`, which is a stated prerequisite: no fallback that parses the JSON with a regular expression is recommended.

```sh
# HOST, CLIENT_ID, TOKEN_SCOPE and CLIENT_SECRET_FILE come from the caller's environment; the secret file is created as Secret Management (Part B of the Migration Documentation) describes.
acquire_access_token() {
	curl --silent --show-error --fail \
		--data-urlencode "grant_type=client_credentials" \
		--data-urlencode "client_id=${CLIENT_ID}" \
		--data-urlencode "client_secret@${CLIENT_SECRET_FILE}" \
		--data-urlencode "scope=${TOKEN_SCOPE}" \
		"${HOST}/Relativity/Identity/connect/token" \
		| jq -r '.access_token'
}
ACCESS_TOKEN=$(acquire_access_token)
```

`--data-urlencode "client_secret@FILE"` reads the secret from a file and keeps it out of the command line (*checked by NSerio locally*: the file is read whole, a trailing line feed included, so create it with `printf '%s'`); `curl -K -` with the same setting on standard input also works (*checked by NSerio locally*). **The scope depends on what the integration needs, and no single scope is required.** Relativity's documentation shows two values for this grant: `SystemUserInfo` on the REST authentication page, the Usage Reports page and in the official Transfer SDK sample, and `UserInfoAccess` in the Kepler .NET samples. No page says which is correct, whether both work, or what either grants, and the OAuth2 Client's `Scopes` property is documented as one that cannot be set. Use the value the API you call documents, and confirm it.

**Use the token without putting it on the command line.**

```sh
( umask 077; printf 'Authorization: Bearer %s\nX-CSRF-Header: -\n' "${ACCESS_TOKEN}" > "${HEADER_FILE}" )
curl --silent --show-error --fail -H @"${HEADER_FILE}" "${HOST}/Relativity.REST/api/..."
```

`curl -H @FILE` sends the header lines of a file (*checked by NSerio locally* on `curl` 8.17.0 and 7.81.0; documented from `curl` 7.55). A token in `-H "Authorization: Bearer $TOKEN"` is visible to every user of the machine in `ps` while the command runs, so the file form is the recommended one for a shared host.

**Cache the token, and re-acquire it on expiry — do not refresh.** Keep the token for its lifetime (part 3 recommends 60 minutes, Relativity's own wording) and request a new one with the client secret shortly before it expires (a margin of 60 seconds is reasonable) and once when a call is rejected for authentication. Client Credentials offers no refresh tokens: **verified by NSerio against a live instance; not documented by Relativity** — the Allow Refresh Tokens control does not appear for this grant in the RelativityOne user interface. Relativity's own tables and text on refresh tokens conflict with each other, so re-acquisition with the secret is the advice that holds whichever way they are read. A script that runs for longer than the token's lifetime must acquire again, not hold the first token.

**The client secret.** It is displayed once, at creation or reset. Store it as Secret Management (Part B of the Migration Documentation) describes; in a shell estate that means **a file created under `umask 077`** or the operating system's keychain, **never in the script, never in a tracked `.env` file, and never exported to child processes**. A reset invalidates the previous secret immediately, so plan the cut-over before resetting it.

**Before deploying, confirm the calling identity.** Every call now runs as the OAuth2 Client's **Context User**, not as the user in the replaced credential. If that user has different workspace permissions or item-level security, the script returns fewer rows or `Unauthorized` in production with no change to its logic, and Relativity's audit attributes every call to that one account. Confirm the Context User's permissions match those of the previous identity.

**When the script acts for a signed-in user.** Client Credentials is one service identity by construction. A script that prompts a person for a password and sends it is the Authorization Code case, and moving to it means a new OAuth2 Client, because the flow is fixed when a client is created. Which of the two flows applies depends on who supplies the credential at run time.

Verification: **Verification note.** **What is matched is an encoded colon pair that a client command uses, not the header's scheme**: follow `ENCODED` to where it is sent and change that use too; a header that names the scheme is a different case, and is matched as a Basic header of its own. **A token has a finite lifetime.**

OAuth2 Client: See [OAuth2 Client for Client Credentials](#oauth2-client-for-client-credentials).
<!-- generated:end SH-007 client-credentials -->

#### Authorization Code

<!-- generated:begin SH-007 authorization-code -->
**Flow.** This entry is for Authorization Code. The "Which flow" paragraph of SH-007 in [Part A](part-a-shell.md#sh-007) says when a match belongs to this flow.

Source form: **Applies to:** `bash`, `zsh`, `ksh93` and `mksh` (and `sh`, read as `bash`); `base64` and `openssl` (their options differ by system).

```sh
ENCODED=$(printf '%s:%s' "${API_USER}" "${API_PASSWORD}" | base64 | tr -d '\n')
```

Under token authentication: **First, the destination:** if this Relativity instance is not RelativityOne — including a Relativity Server instance, whose own documentation governs — no migration to RelativityOne applies and the rest of this entry does not apply. **If the encoded pair is not a credential** (an identifier and a version, for example; a host and a port is not read), this match is a false positive and nothing changes.

Remove the encoding and send the token from a header file: `-H @"${HEADER_FILE}"`, as in the target form for a header that names the scheme. The here-string form (`<<<`) that some scripts use to feed the pair to the encoder is supported by `bash`, `zsh`, `ksh93` and `mksh` (and by `sh`, read as `bash`) and not by POSIX `sh`; a script that must run under POSIX `sh` pipes the text instead. A token written as `-H "Authorization: Bearer ..."` is visible in `ps` while the command runs.

Credentials: **Where the credentials come from.** The text establishes that a credential is sent as Basic, not that the destination is RelativityOne. If it is not RelativityOne, the migration is that service's own token mechanism, no migration to RelativityOne applies, and the reader stops here. If it is RelativityOne, the token comes from the token-acquisition part that follows, and the user name and password are replaced by the OAuth2 Client's client ID and secret.

**Common to both variants.** **Applies to:** `bash`, `zsh`, `ksh93` and `mksh` (and `sh`, read as `bash`); the blocks need `curl`, `openssl` and `jq`. The text has a common part and two variants of how the script receives the code: the **manual copy** (primary) and a **loopback listener** (optional).

**Sign in.** Replace the code that collected the user's credentials with a redirect: send the user's browser to the authorize endpoint `<host>/Relativity/Identity/connect/authorize`, which leads to the RelativityOne login page. The authorize endpoint is **verified by NSerio against a live instance; not documented by Relativity**. The request carries the standard authorization-code parameters — `client_id`, `response_type=code`, the registered `redirect_uri`, a `scope`, and a `state` value the script checks on return, plus `code_challenge` and `code_challenge_method=S256` when PKCE is used; these are standard OAuth, and Relativity documents none of them. No discovery document is relied on.

**PKCE.** PKCE is required by Relativity's own documentation to obtain a refresh token, and not otherwise: a Code request that does not ask for a refresh token works without it, **verified by NSerio against a live instance; not documented by Relativity**. It is never a blanket requirement. NSerio recommends PKCE with the `S256` method (NSerio recommendation), and does not claim that Relativity accepts any particular method, which it does not document. With `openssl`:

```sh
code_verifier=$(openssl rand -base64 48 | tr -d '\r\n' | tr '+/' '-_' | tr -d '=' | cut -c1-64)
code_challenge=$(printf '%s' "${code_verifier}" | openssl dgst -sha256 -binary | openssl base64 -A | tr '+/' '-_' | tr -d '=')
state=$(openssl rand -hex 16 | tr -d '\r')
```

The pipeline gives a 64-character verifier and a 43-character challenge (*checked by NSerio locally* with OpenSSL 3.5.4 on Windows and OpenSSL 3.0.2 on Linux; the `openssl` that ships with macOS has not been checked).

**Receive the code, by the manual copy (primary).** The browser returns to the Redirect URL registered on the OAuth2 Client with the authorization code and the `state` value. **The script prints the authorize address and the person opens it in any browser; when nothing listens at the registered loopback address the browser shows a connection error and leaves the full address, with `code` and `state`, in its address bar; the person copies that address and pastes it to the script.** This works headless, over SSH, on Linux and macOS, and needs no listener (NSerio recommendation; the browser's behaviour on an unreachable loopback address is *inferred* and has not been checked). The Redirect URL must still be exactly the one registered.

```sh
urlencode() (
	LC_ALL=C
	remaining=$1
	while [ -n "${remaining}" ]; do
		rest=${remaining#?}
		character=${remaining%"${rest}"}
		case ${character} in
			[A-Za-z0-9._~-]) printf '%s' "${character}" ;;
			*) printf '%%%02X' "'${character}" ;;
		esac
		remaining=${rest}
	done
)

authorize_url="${HOST}/Relativity/Identity/connect/authorize?client_id=$(urlencode "${CLIENT_ID}")&response_type=code&redirect_uri=$(urlencode "${REDIRECT_URI}")&scope=$(urlencode "${TOKEN_SCOPE}")&state=${state}&code_challenge=${code_challenge}&code_challenge_method=S256"
printf 'Open this address in a browser and sign in:\n%s\n' "${authorize_url}"
printf 'Then copy the whole address the browser ends on and paste it here: '
IFS= read -r returned_url
```

```sh
query=${returned_url#*\?}
query=${query%%#*}
returned_code=''
returned_state=''
remaining=${query}
while [ -n "${remaining}" ]; do
	pair=${remaining%%'&'*}
	case ${remaining} in
		*'&'*) remaining=${remaining#*'&'} ;;
		*) remaining='' ;;
	esac
	case ${pair} in
		code=*) returned_code=${pair#code=} ;;
		state=*) returned_state=${pair#state=} ;;
	esac
done
[ "${returned_state}" = "${state}" ] || { printf 'The state does not match; stop.\n' >&2; exit 1; }
```

The percent-encoding function and the address parsing give the same output in `bash`, `dash`, `zsh`, `ksh93` and `mksh` (*checked by NSerio locally*). **Check the `state` before using the code.** The code is already percent-encoded in the pasted address, so it is sent with `data` (not `data-urlencode`) below and not encoded again. The block passes the code and the verifier to `curl` as a configuration on standard input (`-K -`), so neither appears on the command line of `curl`; `printf` is a shell built-in and its arguments do not appear in `ps` either.

**Exchange the code.** Send a form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=authorization_code`, the `code`, the same `redirect_uri`, the `client_id` and the `client_secret`, plus the `code_verifier` when PKCE is used. The code is exchanged at that endpoint, which Relativity documents for the Client Credentials grant only; the exchange is **verified by NSerio against a live instance; not documented by Relativity**.

```sh
# The code and the verifier travel on standard input, never on a command line; printf is a built-in and does not appear in ps.
token_response=$(printf '%s\n' \
		"data = \"grant_type=authorization_code\"" \
		"data = \"code=${returned_code}\"" \
		"data-urlencode = \"redirect_uri=${REDIRECT_URI}\"" \
		"data-urlencode = \"client_id=${CLIENT_ID}\"" \
		"data-urlencode = \"code_verifier=${code_verifier}\"" \
		"data-urlencode = \"client_secret@${CLIENT_SECRET_FILE}\"" \
	| curl --silent --show-error --fail -K - \
		"${HOST}/Relativity/Identity/connect/token")
ACCESS_TOKEN=$(printf '%s' "${token_response}" | jq -r '.access_token')
```

**Use the token.** Send `Authorization: Bearer <access token>` — the change at the use site above — with `X-CSRF-Header: -` on REST and Kepler calls, from a header file as for Client Credentials: `( umask 077; printf 'Authorization: Bearer %s\nX-CSRF-Header: -\n' "${ACCESS_TOKEN}" > "${HEADER_FILE}" )`, then `curl -H @"${HEADER_FILE}"`.

**On expiry.** With Allow Refresh Tokens enabled on the OAuth2 Client and PKCE used, the script holds a refresh token and, when the access token expires, sends a `grant_type=refresh_token` request with it and the client credentials in place of the code exchange — *inferred* from the standard OAuth shape, not tested. Without a refresh token the person signs in again. Relativity does not document a refresh token's lifetime, whether it rotates, how it is revoked, whether a revocation endpoint exists, or what revoking it does to access tokens already issued. **A refresh token is a secret:** keep it in a file created under `umask 077` or in the keychain, never on a command line.

**Scope.** No scope set is prescribed for Code. The scope `openid RelativityRequestOrigin UserInfo UserInfoAccess AuthenticationID RelativityWeb` works, **verified by NSerio against a live instance; not documented by Relativity**, verified as working and not as minimal. `state` is sent as standard OAuth practice and `nonce` is not used; Relativity does not document whether it enforces either.

**The script stops collecting the password.** After this change the script never receives, stores or forwards the user's user name and password: the user signs in on RelativityOne's own page, and the script holds only tokens. Remove the `read` that prompts for the password, the `stty -echo` around it and any storage of the password the replaced code needed.

**Single sign-on.** Sign-in happens on RelativityOne's own login page, so the instance's single sign-on and two-factor rules apply unchanged — *inferred* from where the sign-in happens, and not documented for clients that customers create.

**The client secret is required.** Code does not work without a client secret: tested and rejected, **verified by NSerio against a live instance; not documented by Relativity**, which confirms Relativity's own statement that the grant requires one. **A script is plain text, so a secret shipped inside a distributed script can be read by anyone who holds the script**; it protects nothing against the person running it. Store it as Secret Management (Part B of the Migration Documentation) describes, in a file with the person's own permissions (`umask 077`) or in the keychain, and apply the compensating guidance below.

**Loopback listener variant (optional, per system).** A shell has no HTTP listener of its own. Where a script runs on a machine with a browser and a registered fixed-port loopback Redirect URL `http://127.0.0.1:<port>/<path>` — a plain-HTTP loopback URL is accepted, **verified by NSerio against a live instance; not documented by Relativity** — it can start `nc` or `socat` bound to `127.0.0.1` only, for one request, to read the request line `GET /<path>?code=…&state=… HTTP/1.1` and answer with a short page: on Linux the OpenBSD `nc` does it with `nc -l 127.0.0.1 PORT` (*checked by NSerio locally*); the options differ between OpenBSD, BSD and macOS, GNU and traditional `nc` and `ncat`, and `socat`, and those have not been checked. Bind to loopback only, check the `state` (another local process can connect to the port), and fall back to the manual copy when the tool is missing or the machine is headless.

**Compensating guidance** (NSerio recommendation, subject to your own security policy): set Trusted IPs where the users' outbound addresses are known and stable — they restrict where a token can be *requested* — use the recommended short Access Token Lifetime of 60 minutes, enable refresh tokens only when needed, and create one OAuth2 Client per distribution, so a leaked secret can be disabled without affecting the others. **A documentation gap.** Relativity documents no recommendation for a distributed script or desktop application; this design is what NSerio verified works.

Verification: **Verification note.** **What is matched is an encoded colon pair that a client command uses, not the header's scheme**: follow `ENCODED` to where it is sent and change that use too; a header that names the scheme is a different case, and is matched as a Basic header of its own. **A token has a finite lifetime.**

OAuth2 Client: See [OAuth2 Client for Authorization Code](#oauth2-client-for-authorization-code).
<!-- generated:end SH-007 authorization-code -->

### SH-008 · BasicTokenInVariable

<a id="sh-008-migration"></a>

#### Client Credentials

<!-- generated:begin SH-008 client-credentials -->
**Flow.** This entry is for Client Credentials. The "Which flow" paragraph of SH-008 in [Part A](part-a-shell.md#sh-008) says when a match belongs to this flow.

Source form: **Applies to:** `bash`, `zsh`, `ksh93` and `mksh` (and `sh`, read as `bash`); `curl`.

```sh
AUTH="Basic ${ENCODED}"
curl --silent --show-error -H "Authorization: ${AUTH}" "${HOST}/Relativity.REST/api/..."
```

Under token authentication: **First, the destination:** if this Relativity instance is not RelativityOne — including a Relativity Server instance, whose own documentation governs — no migration to RelativityOne applies and the rest of this entry does not apply.

```sh
AUTH="Bearer ${ACCESS_TOKEN}"
curl --silent --show-error --fail -H "Authorization: ${AUTH}" -H "X-CSRF-Header: -" "${HOST}/Relativity.REST/api/..."
```

Credentials: **Where the credentials come from.** The text establishes that a credential is sent as Basic, not that the destination is RelativityOne. If it is not RelativityOne, the migration is that service's own token mechanism, no migration to RelativityOne applies, and the reader stops here. If it is RelativityOne, the token comes from the token-acquisition part that follows, and the user name and password are replaced by the OAuth2 Client's client ID and secret.

**Where the token comes from.** Create an OAuth2 Client with Flow Grant Type set to Client Credentials (part 3 lists its settings), then send a form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=client_credentials`, `client_id`, `client_secret` and a `scope`. The token endpoint is **documented** for this grant. The token is then sent as `Authorization: Bearer <token>`, with `X-CSRF-Header: -` on REST and Kepler calls — the header is documented as required, and a Kepler call without it returns 404. **Applies to:** `bash`, `zsh`, `ksh93` and `mksh` (and `sh`, read as `bash`); the blocks need `curl` and `jq`, which is a stated prerequisite: no fallback that parses the JSON with a regular expression is recommended.

```sh
# HOST, CLIENT_ID, TOKEN_SCOPE and CLIENT_SECRET_FILE come from the caller's environment; the secret file is created as Secret Management (Part B of the Migration Documentation) describes.
acquire_access_token() {
	curl --silent --show-error --fail \
		--data-urlencode "grant_type=client_credentials" \
		--data-urlencode "client_id=${CLIENT_ID}" \
		--data-urlencode "client_secret@${CLIENT_SECRET_FILE}" \
		--data-urlencode "scope=${TOKEN_SCOPE}" \
		"${HOST}/Relativity/Identity/connect/token" \
		| jq -r '.access_token'
}
ACCESS_TOKEN=$(acquire_access_token)
```

`--data-urlencode "client_secret@FILE"` reads the secret from a file and keeps it out of the command line (*checked by NSerio locally*: the file is read whole, a trailing line feed included, so create it with `printf '%s'`); `curl -K -` with the same setting on standard input also works (*checked by NSerio locally*). **The scope depends on what the integration needs, and no single scope is required.** Relativity's documentation shows two values for this grant: `SystemUserInfo` on the REST authentication page, the Usage Reports page and in the official Transfer SDK sample, and `UserInfoAccess` in the Kepler .NET samples. No page says which is correct, whether both work, or what either grants, and the OAuth2 Client's `Scopes` property is documented as one that cannot be set. Use the value the API you call documents, and confirm it.

**Use the token without putting it on the command line.**

```sh
( umask 077; printf 'Authorization: Bearer %s\nX-CSRF-Header: -\n' "${ACCESS_TOKEN}" > "${HEADER_FILE}" )
curl --silent --show-error --fail -H @"${HEADER_FILE}" "${HOST}/Relativity.REST/api/..."
```

`curl -H @FILE` sends the header lines of a file (*checked by NSerio locally* on `curl` 8.17.0 and 7.81.0; documented from `curl` 7.55). A token in `-H "Authorization: Bearer $TOKEN"` is visible to every user of the machine in `ps` while the command runs, so the file form is the recommended one for a shared host.

**Cache the token, and re-acquire it on expiry — do not refresh.** Keep the token for its lifetime (part 3 recommends 60 minutes, Relativity's own wording) and request a new one with the client secret shortly before it expires (a margin of 60 seconds is reasonable) and once when a call is rejected for authentication. Client Credentials offers no refresh tokens: **verified by NSerio against a live instance; not documented by Relativity** — the Allow Refresh Tokens control does not appear for this grant in the RelativityOne user interface. Relativity's own tables and text on refresh tokens conflict with each other, so re-acquisition with the secret is the advice that holds whichever way they are read. A script that runs for longer than the token's lifetime must acquire again, not hold the first token.

**The client secret.** It is displayed once, at creation or reset. Store it as Secret Management (Part B of the Migration Documentation) describes; in a shell estate that means **a file created under `umask 077`** or the operating system's keychain, **never in the script, never in a tracked `.env` file, and never exported to child processes**. A reset invalidates the previous secret immediately, so plan the cut-over before resetting it.

**Before deploying, confirm the calling identity.** Every call now runs as the OAuth2 Client's **Context User**, not as the user in the replaced credential. If that user has different workspace permissions or item-level security, the script returns fewer rows or `Unauthorized` in production with no change to its logic, and Relativity's audit attributes every call to that one account. Confirm the Context User's permissions match those of the previous identity.

**When the script acts for a signed-in user.** Client Credentials is one service identity by construction. A script that prompts a person for a password and sends it is the Authorization Code case, and moving to it means a new OAuth2 Client, because the flow is fixed when a client is created. Which of the two flows applies depends on who supplies the credential at run time.

Verification: **Verification note.** **Other uses of the variable beyond thirty lines, or in another file, are not tracked**: change every place that sends it. A token in `-H "Authorization: Bearer ..."` is visible in `ps` while the command runs, so on a shared host send it with `-H @FILE` (see the token acquisition below). **A token has a finite lifetime.**

OAuth2 Client: See [OAuth2 Client for Client Credentials](#oauth2-client-for-client-credentials).
<!-- generated:end SH-008 client-credentials -->

#### Authorization Code

<!-- generated:begin SH-008 authorization-code -->
**Flow.** This entry is for Authorization Code. The "Which flow" paragraph of SH-008 in [Part A](part-a-shell.md#sh-008) says when a match belongs to this flow.

Source form: **Applies to:** `bash`, `zsh`, `ksh93` and `mksh` (and `sh`, read as `bash`); `curl`.

```sh
AUTH="Basic ${ENCODED}"
curl --silent --show-error -H "Authorization: ${AUTH}" "${HOST}/Relativity.REST/api/..."
```

Under token authentication: **First, the destination:** if this Relativity instance is not RelativityOne — including a Relativity Server instance, whose own documentation governs — no migration to RelativityOne applies and the rest of this entry does not apply.

```sh
AUTH="Bearer ${ACCESS_TOKEN}"
curl --silent --show-error --fail -H "Authorization: ${AUTH}" -H "X-CSRF-Header: -" "${HOST}/Relativity.REST/api/..."
```

Credentials: **Where the credentials come from.** The text establishes that a credential is sent as Basic, not that the destination is RelativityOne. If it is not RelativityOne, the migration is that service's own token mechanism, no migration to RelativityOne applies, and the reader stops here. If it is RelativityOne, the token comes from the token-acquisition part that follows, and the user name and password are replaced by the OAuth2 Client's client ID and secret.

**Common to both variants.** **Applies to:** `bash`, `zsh`, `ksh93` and `mksh` (and `sh`, read as `bash`); the blocks need `curl`, `openssl` and `jq`. The text has a common part and two variants of how the script receives the code: the **manual copy** (primary) and a **loopback listener** (optional).

**Sign in.** Replace the code that collected the user's credentials with a redirect: send the user's browser to the authorize endpoint `<host>/Relativity/Identity/connect/authorize`, which leads to the RelativityOne login page. The authorize endpoint is **verified by NSerio against a live instance; not documented by Relativity**. The request carries the standard authorization-code parameters — `client_id`, `response_type=code`, the registered `redirect_uri`, a `scope`, and a `state` value the script checks on return, plus `code_challenge` and `code_challenge_method=S256` when PKCE is used; these are standard OAuth, and Relativity documents none of them. No discovery document is relied on.

**PKCE.** PKCE is required by Relativity's own documentation to obtain a refresh token, and not otherwise: a Code request that does not ask for a refresh token works without it, **verified by NSerio against a live instance; not documented by Relativity**. It is never a blanket requirement. NSerio recommends PKCE with the `S256` method (NSerio recommendation), and does not claim that Relativity accepts any particular method, which it does not document. With `openssl`:

```sh
code_verifier=$(openssl rand -base64 48 | tr -d '\r\n' | tr '+/' '-_' | tr -d '=' | cut -c1-64)
code_challenge=$(printf '%s' "${code_verifier}" | openssl dgst -sha256 -binary | openssl base64 -A | tr '+/' '-_' | tr -d '=')
state=$(openssl rand -hex 16 | tr -d '\r')
```

The pipeline gives a 64-character verifier and a 43-character challenge (*checked by NSerio locally* with OpenSSL 3.5.4 on Windows and OpenSSL 3.0.2 on Linux; the `openssl` that ships with macOS has not been checked).

**Receive the code, by the manual copy (primary).** The browser returns to the Redirect URL registered on the OAuth2 Client with the authorization code and the `state` value. **The script prints the authorize address and the person opens it in any browser; when nothing listens at the registered loopback address the browser shows a connection error and leaves the full address, with `code` and `state`, in its address bar; the person copies that address and pastes it to the script.** This works headless, over SSH, on Linux and macOS, and needs no listener (NSerio recommendation; the browser's behaviour on an unreachable loopback address is *inferred* and has not been checked). The Redirect URL must still be exactly the one registered.

```sh
urlencode() (
	LC_ALL=C
	remaining=$1
	while [ -n "${remaining}" ]; do
		rest=${remaining#?}
		character=${remaining%"${rest}"}
		case ${character} in
			[A-Za-z0-9._~-]) printf '%s' "${character}" ;;
			*) printf '%%%02X' "'${character}" ;;
		esac
		remaining=${rest}
	done
)

authorize_url="${HOST}/Relativity/Identity/connect/authorize?client_id=$(urlencode "${CLIENT_ID}")&response_type=code&redirect_uri=$(urlencode "${REDIRECT_URI}")&scope=$(urlencode "${TOKEN_SCOPE}")&state=${state}&code_challenge=${code_challenge}&code_challenge_method=S256"
printf 'Open this address in a browser and sign in:\n%s\n' "${authorize_url}"
printf 'Then copy the whole address the browser ends on and paste it here: '
IFS= read -r returned_url
```

```sh
query=${returned_url#*\?}
query=${query%%#*}
returned_code=''
returned_state=''
remaining=${query}
while [ -n "${remaining}" ]; do
	pair=${remaining%%'&'*}
	case ${remaining} in
		*'&'*) remaining=${remaining#*'&'} ;;
		*) remaining='' ;;
	esac
	case ${pair} in
		code=*) returned_code=${pair#code=} ;;
		state=*) returned_state=${pair#state=} ;;
	esac
done
[ "${returned_state}" = "${state}" ] || { printf 'The state does not match; stop.\n' >&2; exit 1; }
```

The percent-encoding function and the address parsing give the same output in `bash`, `dash`, `zsh`, `ksh93` and `mksh` (*checked by NSerio locally*). **Check the `state` before using the code.** The code is already percent-encoded in the pasted address, so it is sent with `data` (not `data-urlencode`) below and not encoded again. The block passes the code and the verifier to `curl` as a configuration on standard input (`-K -`), so neither appears on the command line of `curl`; `printf` is a shell built-in and its arguments do not appear in `ps` either.

**Exchange the code.** Send a form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=authorization_code`, the `code`, the same `redirect_uri`, the `client_id` and the `client_secret`, plus the `code_verifier` when PKCE is used. The code is exchanged at that endpoint, which Relativity documents for the Client Credentials grant only; the exchange is **verified by NSerio against a live instance; not documented by Relativity**.

```sh
# The code and the verifier travel on standard input, never on a command line; printf is a built-in and does not appear in ps.
token_response=$(printf '%s\n' \
		"data = \"grant_type=authorization_code\"" \
		"data = \"code=${returned_code}\"" \
		"data-urlencode = \"redirect_uri=${REDIRECT_URI}\"" \
		"data-urlencode = \"client_id=${CLIENT_ID}\"" \
		"data-urlencode = \"code_verifier=${code_verifier}\"" \
		"data-urlencode = \"client_secret@${CLIENT_SECRET_FILE}\"" \
	| curl --silent --show-error --fail -K - \
		"${HOST}/Relativity/Identity/connect/token")
ACCESS_TOKEN=$(printf '%s' "${token_response}" | jq -r '.access_token')
```

**Use the token.** Send `Authorization: Bearer <access token>` — the change at the use site above — with `X-CSRF-Header: -` on REST and Kepler calls, from a header file as for Client Credentials: `( umask 077; printf 'Authorization: Bearer %s\nX-CSRF-Header: -\n' "${ACCESS_TOKEN}" > "${HEADER_FILE}" )`, then `curl -H @"${HEADER_FILE}"`.

**On expiry.** With Allow Refresh Tokens enabled on the OAuth2 Client and PKCE used, the script holds a refresh token and, when the access token expires, sends a `grant_type=refresh_token` request with it and the client credentials in place of the code exchange — *inferred* from the standard OAuth shape, not tested. Without a refresh token the person signs in again. Relativity does not document a refresh token's lifetime, whether it rotates, how it is revoked, whether a revocation endpoint exists, or what revoking it does to access tokens already issued. **A refresh token is a secret:** keep it in a file created under `umask 077` or in the keychain, never on a command line.

**Scope.** No scope set is prescribed for Code. The scope `openid RelativityRequestOrigin UserInfo UserInfoAccess AuthenticationID RelativityWeb` works, **verified by NSerio against a live instance; not documented by Relativity**, verified as working and not as minimal. `state` is sent as standard OAuth practice and `nonce` is not used; Relativity does not document whether it enforces either.

**The script stops collecting the password.** After this change the script never receives, stores or forwards the user's user name and password: the user signs in on RelativityOne's own page, and the script holds only tokens. Remove the `read` that prompts for the password, the `stty -echo` around it and any storage of the password the replaced code needed.

**Single sign-on.** Sign-in happens on RelativityOne's own login page, so the instance's single sign-on and two-factor rules apply unchanged — *inferred* from where the sign-in happens, and not documented for clients that customers create.

**The client secret is required.** Code does not work without a client secret: tested and rejected, **verified by NSerio against a live instance; not documented by Relativity**, which confirms Relativity's own statement that the grant requires one. **A script is plain text, so a secret shipped inside a distributed script can be read by anyone who holds the script**; it protects nothing against the person running it. Store it as Secret Management (Part B of the Migration Documentation) describes, in a file with the person's own permissions (`umask 077`) or in the keychain, and apply the compensating guidance below.

**Loopback listener variant (optional, per system).** A shell has no HTTP listener of its own. Where a script runs on a machine with a browser and a registered fixed-port loopback Redirect URL `http://127.0.0.1:<port>/<path>` — a plain-HTTP loopback URL is accepted, **verified by NSerio against a live instance; not documented by Relativity** — it can start `nc` or `socat` bound to `127.0.0.1` only, for one request, to read the request line `GET /<path>?code=…&state=… HTTP/1.1` and answer with a short page: on Linux the OpenBSD `nc` does it with `nc -l 127.0.0.1 PORT` (*checked by NSerio locally*); the options differ between OpenBSD, BSD and macOS, GNU and traditional `nc` and `ncat`, and `socat`, and those have not been checked. Bind to loopback only, check the `state` (another local process can connect to the port), and fall back to the manual copy when the tool is missing or the machine is headless.

**Compensating guidance** (NSerio recommendation, subject to your own security policy): set Trusted IPs where the users' outbound addresses are known and stable — they restrict where a token can be *requested* — use the recommended short Access Token Lifetime of 60 minutes, enable refresh tokens only when needed, and create one OAuth2 Client per distribution, so a leaked secret can be disabled without affecting the others. **A documentation gap.** Relativity documents no recommendation for a distributed script or desktop application; this design is what NSerio verified works.

Verification: **Verification note.** **Other uses of the variable beyond thirty lines, or in another file, are not tracked**: change every place that sends it. A token in `-H "Authorization: Bearer ..."` is visible in `ps` while the command runs, so on a shared host send it with `-H @FILE` (see the token acquisition below). **A token has a finite lifetime.**

OAuth2 Client: See [OAuth2 Client for Authorization Code](#oauth2-client-for-authorization-code).
<!-- generated:end SH-008 authorization-code -->

### SH-009 · ArgumentArrayCredentials

<a id="sh-009-migration"></a>

#### Client Credentials

<!-- generated:begin SH-009 client-credentials -->
**Flow.** This entry is for Client Credentials. The "Which flow" paragraph of SH-009 in [Part A](part-a-shell.md#sh-009) says when a match belongs to this flow.

Source form: **Applies to:** **`bash`, `zsh`, `ksh93` and `mksh` only** (POSIX `sh` has no arrays); `curl`, `wget`.

```bash
options=(-sS -u "${API_USER}:${API_PASSWORD}")
curl "${options[@]}" "${HOST}/Relativity.REST/api/..."
```

Under token authentication: **First, the destination:** if this Relativity instance is not RelativityOne — including a Relativity Server instance, whose own documentation governs — no migration to RelativityOne applies and the rest of this entry does not apply.

```bash
( umask 077; printf 'Authorization: Bearer %s\nX-CSRF-Header: -\n' "${ACCESS_TOKEN}" > "${HEADER_FILE}" )
options=(-sS --fail -H @"${HEADER_FILE}")
curl "${options[@]}" "${HOST}/Relativity.REST/api/..."
```

where `HEADER_FILE` holds `Authorization: Bearer <access token>` and `X-CSRF-Header: -` (see the token acquisition below). Remove the credential option from the array.

Credentials: **Where the credentials come from.** The text establishes that a credential is sent as Basic, not that the destination is RelativityOne. If it is not RelativityOne, the migration is that service's own token mechanism, no migration to RelativityOne applies, and the reader stops here. If it is RelativityOne, the token comes from the token-acquisition part that follows, and the user name and password are replaced by the OAuth2 Client's client ID and secret.

**Where the token comes from.** Create an OAuth2 Client with Flow Grant Type set to Client Credentials (part 3 lists its settings), then send a form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=client_credentials`, `client_id`, `client_secret` and a `scope`. The token endpoint is **documented** for this grant. The token is then sent as `Authorization: Bearer <token>`, with `X-CSRF-Header: -` on REST and Kepler calls — the header is documented as required, and a Kepler call without it returns 404. **Applies to:** `bash`, `zsh`, `ksh93` and `mksh` (and `sh`, read as `bash`); the blocks need `curl` and `jq`, which is a stated prerequisite: no fallback that parses the JSON with a regular expression is recommended.

```sh
# HOST, CLIENT_ID, TOKEN_SCOPE and CLIENT_SECRET_FILE come from the caller's environment; the secret file is created as Secret Management (Part B of the Migration Documentation) describes.
acquire_access_token() {
	curl --silent --show-error --fail \
		--data-urlencode "grant_type=client_credentials" \
		--data-urlencode "client_id=${CLIENT_ID}" \
		--data-urlencode "client_secret@${CLIENT_SECRET_FILE}" \
		--data-urlencode "scope=${TOKEN_SCOPE}" \
		"${HOST}/Relativity/Identity/connect/token" \
		| jq -r '.access_token'
}
ACCESS_TOKEN=$(acquire_access_token)
```

`--data-urlencode "client_secret@FILE"` reads the secret from a file and keeps it out of the command line (*checked by NSerio locally*: the file is read whole, a trailing line feed included, so create it with `printf '%s'`); `curl -K -` with the same setting on standard input also works (*checked by NSerio locally*). **The scope depends on what the integration needs, and no single scope is required.** Relativity's documentation shows two values for this grant: `SystemUserInfo` on the REST authentication page, the Usage Reports page and in the official Transfer SDK sample, and `UserInfoAccess` in the Kepler .NET samples. No page says which is correct, whether both work, or what either grants, and the OAuth2 Client's `Scopes` property is documented as one that cannot be set. Use the value the API you call documents, and confirm it.

**Use the token without putting it on the command line.**

```sh
( umask 077; printf 'Authorization: Bearer %s\nX-CSRF-Header: -\n' "${ACCESS_TOKEN}" > "${HEADER_FILE}" )
curl --silent --show-error --fail -H @"${HEADER_FILE}" "${HOST}/Relativity.REST/api/..."
```

`curl -H @FILE` sends the header lines of a file (*checked by NSerio locally* on `curl` 8.17.0 and 7.81.0; documented from `curl` 7.55). A token in `-H "Authorization: Bearer $TOKEN"` is visible to every user of the machine in `ps` while the command runs, so the file form is the recommended one for a shared host.

**Cache the token, and re-acquire it on expiry — do not refresh.** Keep the token for its lifetime (part 3 recommends 60 minutes, Relativity's own wording) and request a new one with the client secret shortly before it expires (a margin of 60 seconds is reasonable) and once when a call is rejected for authentication. Client Credentials offers no refresh tokens: **verified by NSerio against a live instance; not documented by Relativity** — the Allow Refresh Tokens control does not appear for this grant in the RelativityOne user interface. Relativity's own tables and text on refresh tokens conflict with each other, so re-acquisition with the secret is the advice that holds whichever way they are read. A script that runs for longer than the token's lifetime must acquire again, not hold the first token.

**The client secret.** It is displayed once, at creation or reset. Store it as Secret Management (Part B of the Migration Documentation) describes; in a shell estate that means **a file created under `umask 077`** or the operating system's keychain, **never in the script, never in a tracked `.env` file, and never exported to child processes**. A reset invalidates the previous secret immediately, so plan the cut-over before resetting it.

**Before deploying, confirm the calling identity.** Every call now runs as the OAuth2 Client's **Context User**, not as the user in the replaced credential. If that user has different workspace permissions or item-level security, the script returns fewer rows or `Unauthorized` in production with no change to its logic, and Relativity's audit attributes every call to that one account. Confirm the Context User's permissions match those of the previous identity.

**When the script acts for a signed-in user.** Client Credentials is one service identity by construction. A script that prompts a person for a password and sends it is the Authorization Code case, and moving to it means a new OAuth2 Client, because the flow is fixed when a client is created. Which of the two flows applies depends on who supplies the credential at run time.

Verification: **Verification note.** **The array (appends and associative arrays included) and a later use within thirty lines are read**; an array built in another file, reached by a name reference or used beyond thirty lines is not read, so look for the other places the same options are used. A token in `-H "Authorization: Bearer ..."` is visible in `ps` while the command runs, so on a shared host send it with `-H @FILE` (see the token acquisition below). **A token has a finite lifetime.**

OAuth2 Client: See [OAuth2 Client for Client Credentials](#oauth2-client-for-client-credentials).
<!-- generated:end SH-009 client-credentials -->

#### Authorization Code

<!-- generated:begin SH-009 authorization-code -->
**Flow.** This entry is for Authorization Code. The "Which flow" paragraph of SH-009 in [Part A](part-a-shell.md#sh-009) says when a match belongs to this flow.

Source form: **Applies to:** **`bash`, `zsh`, `ksh93` and `mksh` only** (POSIX `sh` has no arrays); `curl`, `wget`.

```bash
options=(-sS -u "${API_USER}:${API_PASSWORD}")
curl "${options[@]}" "${HOST}/Relativity.REST/api/..."
```

Under token authentication: **First, the destination:** if this Relativity instance is not RelativityOne — including a Relativity Server instance, whose own documentation governs — no migration to RelativityOne applies and the rest of this entry does not apply.

```bash
( umask 077; printf 'Authorization: Bearer %s\nX-CSRF-Header: -\n' "${ACCESS_TOKEN}" > "${HEADER_FILE}" )
options=(-sS --fail -H @"${HEADER_FILE}")
curl "${options[@]}" "${HOST}/Relativity.REST/api/..."
```

where `HEADER_FILE` holds `Authorization: Bearer <access token>` and `X-CSRF-Header: -` (see the token acquisition below). Remove the credential option from the array.

Credentials: **Where the credentials come from.** The text establishes that a credential is sent as Basic, not that the destination is RelativityOne. If it is not RelativityOne, the migration is that service's own token mechanism, no migration to RelativityOne applies, and the reader stops here. If it is RelativityOne, the token comes from the token-acquisition part that follows, and the user name and password are replaced by the OAuth2 Client's client ID and secret.

**Common to both variants.** **Applies to:** `bash`, `zsh`, `ksh93` and `mksh` (and `sh`, read as `bash`); the blocks need `curl`, `openssl` and `jq`. The text has a common part and two variants of how the script receives the code: the **manual copy** (primary) and a **loopback listener** (optional).

**Sign in.** Replace the code that collected the user's credentials with a redirect: send the user's browser to the authorize endpoint `<host>/Relativity/Identity/connect/authorize`, which leads to the RelativityOne login page. The authorize endpoint is **verified by NSerio against a live instance; not documented by Relativity**. The request carries the standard authorization-code parameters — `client_id`, `response_type=code`, the registered `redirect_uri`, a `scope`, and a `state` value the script checks on return, plus `code_challenge` and `code_challenge_method=S256` when PKCE is used; these are standard OAuth, and Relativity documents none of them. No discovery document is relied on.

**PKCE.** PKCE is required by Relativity's own documentation to obtain a refresh token, and not otherwise: a Code request that does not ask for a refresh token works without it, **verified by NSerio against a live instance; not documented by Relativity**. It is never a blanket requirement. NSerio recommends PKCE with the `S256` method (NSerio recommendation), and does not claim that Relativity accepts any particular method, which it does not document. With `openssl`:

```sh
code_verifier=$(openssl rand -base64 48 | tr -d '\r\n' | tr '+/' '-_' | tr -d '=' | cut -c1-64)
code_challenge=$(printf '%s' "${code_verifier}" | openssl dgst -sha256 -binary | openssl base64 -A | tr '+/' '-_' | tr -d '=')
state=$(openssl rand -hex 16 | tr -d '\r')
```

The pipeline gives a 64-character verifier and a 43-character challenge (*checked by NSerio locally* with OpenSSL 3.5.4 on Windows and OpenSSL 3.0.2 on Linux; the `openssl` that ships with macOS has not been checked).

**Receive the code, by the manual copy (primary).** The browser returns to the Redirect URL registered on the OAuth2 Client with the authorization code and the `state` value. **The script prints the authorize address and the person opens it in any browser; when nothing listens at the registered loopback address the browser shows a connection error and leaves the full address, with `code` and `state`, in its address bar; the person copies that address and pastes it to the script.** This works headless, over SSH, on Linux and macOS, and needs no listener (NSerio recommendation; the browser's behaviour on an unreachable loopback address is *inferred* and has not been checked). The Redirect URL must still be exactly the one registered.

```sh
urlencode() (
	LC_ALL=C
	remaining=$1
	while [ -n "${remaining}" ]; do
		rest=${remaining#?}
		character=${remaining%"${rest}"}
		case ${character} in
			[A-Za-z0-9._~-]) printf '%s' "${character}" ;;
			*) printf '%%%02X' "'${character}" ;;
		esac
		remaining=${rest}
	done
)

authorize_url="${HOST}/Relativity/Identity/connect/authorize?client_id=$(urlencode "${CLIENT_ID}")&response_type=code&redirect_uri=$(urlencode "${REDIRECT_URI}")&scope=$(urlencode "${TOKEN_SCOPE}")&state=${state}&code_challenge=${code_challenge}&code_challenge_method=S256"
printf 'Open this address in a browser and sign in:\n%s\n' "${authorize_url}"
printf 'Then copy the whole address the browser ends on and paste it here: '
IFS= read -r returned_url
```

```sh
query=${returned_url#*\?}
query=${query%%#*}
returned_code=''
returned_state=''
remaining=${query}
while [ -n "${remaining}" ]; do
	pair=${remaining%%'&'*}
	case ${remaining} in
		*'&'*) remaining=${remaining#*'&'} ;;
		*) remaining='' ;;
	esac
	case ${pair} in
		code=*) returned_code=${pair#code=} ;;
		state=*) returned_state=${pair#state=} ;;
	esac
done
[ "${returned_state}" = "${state}" ] || { printf 'The state does not match; stop.\n' >&2; exit 1; }
```

The percent-encoding function and the address parsing give the same output in `bash`, `dash`, `zsh`, `ksh93` and `mksh` (*checked by NSerio locally*). **Check the `state` before using the code.** The code is already percent-encoded in the pasted address, so it is sent with `data` (not `data-urlencode`) below and not encoded again. The block passes the code and the verifier to `curl` as a configuration on standard input (`-K -`), so neither appears on the command line of `curl`; `printf` is a shell built-in and its arguments do not appear in `ps` either.

**Exchange the code.** Send a form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=authorization_code`, the `code`, the same `redirect_uri`, the `client_id` and the `client_secret`, plus the `code_verifier` when PKCE is used. The code is exchanged at that endpoint, which Relativity documents for the Client Credentials grant only; the exchange is **verified by NSerio against a live instance; not documented by Relativity**.

```sh
# The code and the verifier travel on standard input, never on a command line; printf is a built-in and does not appear in ps.
token_response=$(printf '%s\n' \
		"data = \"grant_type=authorization_code\"" \
		"data = \"code=${returned_code}\"" \
		"data-urlencode = \"redirect_uri=${REDIRECT_URI}\"" \
		"data-urlencode = \"client_id=${CLIENT_ID}\"" \
		"data-urlencode = \"code_verifier=${code_verifier}\"" \
		"data-urlencode = \"client_secret@${CLIENT_SECRET_FILE}\"" \
	| curl --silent --show-error --fail -K - \
		"${HOST}/Relativity/Identity/connect/token")
ACCESS_TOKEN=$(printf '%s' "${token_response}" | jq -r '.access_token')
```

**Use the token.** Send `Authorization: Bearer <access token>` — the change at the use site above — with `X-CSRF-Header: -` on REST and Kepler calls, from a header file as for Client Credentials: `( umask 077; printf 'Authorization: Bearer %s\nX-CSRF-Header: -\n' "${ACCESS_TOKEN}" > "${HEADER_FILE}" )`, then `curl -H @"${HEADER_FILE}"`.

**On expiry.** With Allow Refresh Tokens enabled on the OAuth2 Client and PKCE used, the script holds a refresh token and, when the access token expires, sends a `grant_type=refresh_token` request with it and the client credentials in place of the code exchange — *inferred* from the standard OAuth shape, not tested. Without a refresh token the person signs in again. Relativity does not document a refresh token's lifetime, whether it rotates, how it is revoked, whether a revocation endpoint exists, or what revoking it does to access tokens already issued. **A refresh token is a secret:** keep it in a file created under `umask 077` or in the keychain, never on a command line.

**Scope.** No scope set is prescribed for Code. The scope `openid RelativityRequestOrigin UserInfo UserInfoAccess AuthenticationID RelativityWeb` works, **verified by NSerio against a live instance; not documented by Relativity**, verified as working and not as minimal. `state` is sent as standard OAuth practice and `nonce` is not used; Relativity does not document whether it enforces either.

**The script stops collecting the password.** After this change the script never receives, stores or forwards the user's user name and password: the user signs in on RelativityOne's own page, and the script holds only tokens. Remove the `read` that prompts for the password, the `stty -echo` around it and any storage of the password the replaced code needed.

**Single sign-on.** Sign-in happens on RelativityOne's own login page, so the instance's single sign-on and two-factor rules apply unchanged — *inferred* from where the sign-in happens, and not documented for clients that customers create.

**The client secret is required.** Code does not work without a client secret: tested and rejected, **verified by NSerio against a live instance; not documented by Relativity**, which confirms Relativity's own statement that the grant requires one. **A script is plain text, so a secret shipped inside a distributed script can be read by anyone who holds the script**; it protects nothing against the person running it. Store it as Secret Management (Part B of the Migration Documentation) describes, in a file with the person's own permissions (`umask 077`) or in the keychain, and apply the compensating guidance below.

**Loopback listener variant (optional, per system).** A shell has no HTTP listener of its own. Where a script runs on a machine with a browser and a registered fixed-port loopback Redirect URL `http://127.0.0.1:<port>/<path>` — a plain-HTTP loopback URL is accepted, **verified by NSerio against a live instance; not documented by Relativity** — it can start `nc` or `socat` bound to `127.0.0.1` only, for one request, to read the request line `GET /<path>?code=…&state=… HTTP/1.1` and answer with a short page: on Linux the OpenBSD `nc` does it with `nc -l 127.0.0.1 PORT` (*checked by NSerio locally*); the options differ between OpenBSD, BSD and macOS, GNU and traditional `nc` and `ncat`, and `socat`, and those have not been checked. Bind to loopback only, check the `state` (another local process can connect to the port), and fall back to the manual copy when the tool is missing or the machine is headless.

**Compensating guidance** (NSerio recommendation, subject to your own security policy): set Trusted IPs where the users' outbound addresses are known and stable — they restrict where a token can be *requested* — use the recommended short Access Token Lifetime of 60 minutes, enable refresh tokens only when needed, and create one OAuth2 Client per distribution, so a leaked secret can be disabled without affecting the others. **A documentation gap.** Relativity documents no recommendation for a distributed script or desktop application; this design is what NSerio verified works.

Verification: **Verification note.** **The array (appends and associative arrays included) and a later use within thirty lines are read**; an array built in another file, reached by a name reference or used beyond thirty lines is not read, so look for the other places the same options are used. A token in `-H "Authorization: Bearer ..."` is visible in `ps` while the command runs, so on a shared host send it with `-H @FILE` (see the token acquisition below). **A token has a finite lifetime.**

OAuth2 Client: See [OAuth2 Client for Authorization Code](#oauth2-client-for-authorization-code).
<!-- generated:end SH-009 authorization-code -->

### SH-010 · UrlUserinfoAssignment

<a id="sh-010-migration"></a>

#### Client Credentials

<!-- generated:begin SH-010 client-credentials -->
**Flow.** This entry is for Client Credentials. The "Which flow" paragraph of SH-010 in [Part A](part-a-shell.md#sh-010) says when a match belongs to this flow.

Source form: **Applies to:** `bash`, `zsh`, `ksh93` and `mksh` (and `sh`, read as `bash`).

```sh
API_URL="https://${API_USER}:${API_PASSWORD}@host.example/Relativity.REST/api"
```

Under token authentication: **First, the destination:** if this Relativity instance is not RelativityOne — including a Relativity Server instance, whose own documentation governs — no migration to RelativityOne applies and the rest of this entry does not apply. **If the URL is another service's, this match is a false positive** (a git remote used by `git` is not read).

```sh
API_URL="https://host.example/Relativity.REST/api"
```

and send the token with `-H @"${HEADER_FILE}"`, as in the target form for a header that names the scheme.

Credentials: **Where the credentials come from.** The text establishes that a credential is sent as Basic, not that the destination is RelativityOne. If it is not RelativityOne, the migration is that service's own token mechanism, no migration to RelativityOne applies, and the reader stops here. If it is RelativityOne, the token comes from the token-acquisition part that follows, and the user name and password are replaced by the OAuth2 Client's client ID and secret.

**Where the token comes from.** Create an OAuth2 Client with Flow Grant Type set to Client Credentials (part 3 lists its settings), then send a form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=client_credentials`, `client_id`, `client_secret` and a `scope`. The token endpoint is **documented** for this grant. The token is then sent as `Authorization: Bearer <token>`, with `X-CSRF-Header: -` on REST and Kepler calls — the header is documented as required, and a Kepler call without it returns 404. **Applies to:** `bash`, `zsh`, `ksh93` and `mksh` (and `sh`, read as `bash`); the blocks need `curl` and `jq`, which is a stated prerequisite: no fallback that parses the JSON with a regular expression is recommended.

```sh
# HOST, CLIENT_ID, TOKEN_SCOPE and CLIENT_SECRET_FILE come from the caller's environment; the secret file is created as Secret Management (Part B of the Migration Documentation) describes.
acquire_access_token() {
	curl --silent --show-error --fail \
		--data-urlencode "grant_type=client_credentials" \
		--data-urlencode "client_id=${CLIENT_ID}" \
		--data-urlencode "client_secret@${CLIENT_SECRET_FILE}" \
		--data-urlencode "scope=${TOKEN_SCOPE}" \
		"${HOST}/Relativity/Identity/connect/token" \
		| jq -r '.access_token'
}
ACCESS_TOKEN=$(acquire_access_token)
```

`--data-urlencode "client_secret@FILE"` reads the secret from a file and keeps it out of the command line (*checked by NSerio locally*: the file is read whole, a trailing line feed included, so create it with `printf '%s'`); `curl -K -` with the same setting on standard input also works (*checked by NSerio locally*). **The scope depends on what the integration needs, and no single scope is required.** Relativity's documentation shows two values for this grant: `SystemUserInfo` on the REST authentication page, the Usage Reports page and in the official Transfer SDK sample, and `UserInfoAccess` in the Kepler .NET samples. No page says which is correct, whether both work, or what either grants, and the OAuth2 Client's `Scopes` property is documented as one that cannot be set. Use the value the API you call documents, and confirm it.

**Use the token without putting it on the command line.**

```sh
( umask 077; printf 'Authorization: Bearer %s\nX-CSRF-Header: -\n' "${ACCESS_TOKEN}" > "${HEADER_FILE}" )
curl --silent --show-error --fail -H @"${HEADER_FILE}" "${HOST}/Relativity.REST/api/..."
```

`curl -H @FILE` sends the header lines of a file (*checked by NSerio locally* on `curl` 8.17.0 and 7.81.0; documented from `curl` 7.55). A token in `-H "Authorization: Bearer $TOKEN"` is visible to every user of the machine in `ps` while the command runs, so the file form is the recommended one for a shared host.

**Cache the token, and re-acquire it on expiry — do not refresh.** Keep the token for its lifetime (part 3 recommends 60 minutes, Relativity's own wording) and request a new one with the client secret shortly before it expires (a margin of 60 seconds is reasonable) and once when a call is rejected for authentication. Client Credentials offers no refresh tokens: **verified by NSerio against a live instance; not documented by Relativity** — the Allow Refresh Tokens control does not appear for this grant in the RelativityOne user interface. Relativity's own tables and text on refresh tokens conflict with each other, so re-acquisition with the secret is the advice that holds whichever way they are read. A script that runs for longer than the token's lifetime must acquire again, not hold the first token.

**The client secret.** It is displayed once, at creation or reset. Store it as Secret Management (Part B of the Migration Documentation) describes; in a shell estate that means **a file created under `umask 077`** or the operating system's keychain, **never in the script, never in a tracked `.env` file, and never exported to child processes**. A reset invalidates the previous secret immediately, so plan the cut-over before resetting it.

**Before deploying, confirm the calling identity.** Every call now runs as the OAuth2 Client's **Context User**, not as the user in the replaced credential. If that user has different workspace permissions or item-level security, the script returns fewer rows or `Unauthorized` in production with no change to its logic, and Relativity's audit attributes every call to that one account. Confirm the Context User's permissions match those of the previous identity.

**When the script acts for a signed-in user.** Client Credentials is one service identity by construction. A script that prompts a person for a password and sends it is the Authorization Code case, and moving to it means a new OAuth2 Client, because the flow is fixed when a client is created. Which of the two flows applies depends on who supplies the credential at run time.

Verification: **Verification note.** **A covered client use of the variable within thirty lines is read**; change that call and any other that uses the URL. **A URL with userinfo may already be in logs**: rotate the credential. **A token has a finite lifetime.**

OAuth2 Client: See [OAuth2 Client for Client Credentials](#oauth2-client-for-client-credentials).
<!-- generated:end SH-010 client-credentials -->

#### Authorization Code

<!-- generated:begin SH-010 authorization-code -->
**Flow.** This entry is for Authorization Code. The "Which flow" paragraph of SH-010 in [Part A](part-a-shell.md#sh-010) says when a match belongs to this flow.

Source form: **Applies to:** `bash`, `zsh`, `ksh93` and `mksh` (and `sh`, read as `bash`).

```sh
API_URL="https://${API_USER}:${API_PASSWORD}@host.example/Relativity.REST/api"
```

Under token authentication: **First, the destination:** if this Relativity instance is not RelativityOne — including a Relativity Server instance, whose own documentation governs — no migration to RelativityOne applies and the rest of this entry does not apply. **If the URL is another service's, this match is a false positive** (a git remote used by `git` is not read).

```sh
API_URL="https://host.example/Relativity.REST/api"
```

and send the token with `-H @"${HEADER_FILE}"`, as in the target form for a header that names the scheme.

Credentials: **Where the credentials come from.** The text establishes that a credential is sent as Basic, not that the destination is RelativityOne. If it is not RelativityOne, the migration is that service's own token mechanism, no migration to RelativityOne applies, and the reader stops here. If it is RelativityOne, the token comes from the token-acquisition part that follows, and the user name and password are replaced by the OAuth2 Client's client ID and secret.

**Common to both variants.** **Applies to:** `bash`, `zsh`, `ksh93` and `mksh` (and `sh`, read as `bash`); the blocks need `curl`, `openssl` and `jq`. The text has a common part and two variants of how the script receives the code: the **manual copy** (primary) and a **loopback listener** (optional).

**Sign in.** Replace the code that collected the user's credentials with a redirect: send the user's browser to the authorize endpoint `<host>/Relativity/Identity/connect/authorize`, which leads to the RelativityOne login page. The authorize endpoint is **verified by NSerio against a live instance; not documented by Relativity**. The request carries the standard authorization-code parameters — `client_id`, `response_type=code`, the registered `redirect_uri`, a `scope`, and a `state` value the script checks on return, plus `code_challenge` and `code_challenge_method=S256` when PKCE is used; these are standard OAuth, and Relativity documents none of them. No discovery document is relied on.

**PKCE.** PKCE is required by Relativity's own documentation to obtain a refresh token, and not otherwise: a Code request that does not ask for a refresh token works without it, **verified by NSerio against a live instance; not documented by Relativity**. It is never a blanket requirement. NSerio recommends PKCE with the `S256` method (NSerio recommendation), and does not claim that Relativity accepts any particular method, which it does not document. With `openssl`:

```sh
code_verifier=$(openssl rand -base64 48 | tr -d '\r\n' | tr '+/' '-_' | tr -d '=' | cut -c1-64)
code_challenge=$(printf '%s' "${code_verifier}" | openssl dgst -sha256 -binary | openssl base64 -A | tr '+/' '-_' | tr -d '=')
state=$(openssl rand -hex 16 | tr -d '\r')
```

The pipeline gives a 64-character verifier and a 43-character challenge (*checked by NSerio locally* with OpenSSL 3.5.4 on Windows and OpenSSL 3.0.2 on Linux; the `openssl` that ships with macOS has not been checked).

**Receive the code, by the manual copy (primary).** The browser returns to the Redirect URL registered on the OAuth2 Client with the authorization code and the `state` value. **The script prints the authorize address and the person opens it in any browser; when nothing listens at the registered loopback address the browser shows a connection error and leaves the full address, with `code` and `state`, in its address bar; the person copies that address and pastes it to the script.** This works headless, over SSH, on Linux and macOS, and needs no listener (NSerio recommendation; the browser's behaviour on an unreachable loopback address is *inferred* and has not been checked). The Redirect URL must still be exactly the one registered.

```sh
urlencode() (
	LC_ALL=C
	remaining=$1
	while [ -n "${remaining}" ]; do
		rest=${remaining#?}
		character=${remaining%"${rest}"}
		case ${character} in
			[A-Za-z0-9._~-]) printf '%s' "${character}" ;;
			*) printf '%%%02X' "'${character}" ;;
		esac
		remaining=${rest}
	done
)

authorize_url="${HOST}/Relativity/Identity/connect/authorize?client_id=$(urlencode "${CLIENT_ID}")&response_type=code&redirect_uri=$(urlencode "${REDIRECT_URI}")&scope=$(urlencode "${TOKEN_SCOPE}")&state=${state}&code_challenge=${code_challenge}&code_challenge_method=S256"
printf 'Open this address in a browser and sign in:\n%s\n' "${authorize_url}"
printf 'Then copy the whole address the browser ends on and paste it here: '
IFS= read -r returned_url
```

```sh
query=${returned_url#*\?}
query=${query%%#*}
returned_code=''
returned_state=''
remaining=${query}
while [ -n "${remaining}" ]; do
	pair=${remaining%%'&'*}
	case ${remaining} in
		*'&'*) remaining=${remaining#*'&'} ;;
		*) remaining='' ;;
	esac
	case ${pair} in
		code=*) returned_code=${pair#code=} ;;
		state=*) returned_state=${pair#state=} ;;
	esac
done
[ "${returned_state}" = "${state}" ] || { printf 'The state does not match; stop.\n' >&2; exit 1; }
```

The percent-encoding function and the address parsing give the same output in `bash`, `dash`, `zsh`, `ksh93` and `mksh` (*checked by NSerio locally*). **Check the `state` before using the code.** The code is already percent-encoded in the pasted address, so it is sent with `data` (not `data-urlencode`) below and not encoded again. The block passes the code and the verifier to `curl` as a configuration on standard input (`-K -`), so neither appears on the command line of `curl`; `printf` is a shell built-in and its arguments do not appear in `ps` either.

**Exchange the code.** Send a form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=authorization_code`, the `code`, the same `redirect_uri`, the `client_id` and the `client_secret`, plus the `code_verifier` when PKCE is used. The code is exchanged at that endpoint, which Relativity documents for the Client Credentials grant only; the exchange is **verified by NSerio against a live instance; not documented by Relativity**.

```sh
# The code and the verifier travel on standard input, never on a command line; printf is a built-in and does not appear in ps.
token_response=$(printf '%s\n' \
		"data = \"grant_type=authorization_code\"" \
		"data = \"code=${returned_code}\"" \
		"data-urlencode = \"redirect_uri=${REDIRECT_URI}\"" \
		"data-urlencode = \"client_id=${CLIENT_ID}\"" \
		"data-urlencode = \"code_verifier=${code_verifier}\"" \
		"data-urlencode = \"client_secret@${CLIENT_SECRET_FILE}\"" \
	| curl --silent --show-error --fail -K - \
		"${HOST}/Relativity/Identity/connect/token")
ACCESS_TOKEN=$(printf '%s' "${token_response}" | jq -r '.access_token')
```

**Use the token.** Send `Authorization: Bearer <access token>` — the change at the use site above — with `X-CSRF-Header: -` on REST and Kepler calls, from a header file as for Client Credentials: `( umask 077; printf 'Authorization: Bearer %s\nX-CSRF-Header: -\n' "${ACCESS_TOKEN}" > "${HEADER_FILE}" )`, then `curl -H @"${HEADER_FILE}"`.

**On expiry.** With Allow Refresh Tokens enabled on the OAuth2 Client and PKCE used, the script holds a refresh token and, when the access token expires, sends a `grant_type=refresh_token` request with it and the client credentials in place of the code exchange — *inferred* from the standard OAuth shape, not tested. Without a refresh token the person signs in again. Relativity does not document a refresh token's lifetime, whether it rotates, how it is revoked, whether a revocation endpoint exists, or what revoking it does to access tokens already issued. **A refresh token is a secret:** keep it in a file created under `umask 077` or in the keychain, never on a command line.

**Scope.** No scope set is prescribed for Code. The scope `openid RelativityRequestOrigin UserInfo UserInfoAccess AuthenticationID RelativityWeb` works, **verified by NSerio against a live instance; not documented by Relativity**, verified as working and not as minimal. `state` is sent as standard OAuth practice and `nonce` is not used; Relativity does not document whether it enforces either.

**The script stops collecting the password.** After this change the script never receives, stores or forwards the user's user name and password: the user signs in on RelativityOne's own page, and the script holds only tokens. Remove the `read` that prompts for the password, the `stty -echo` around it and any storage of the password the replaced code needed.

**Single sign-on.** Sign-in happens on RelativityOne's own login page, so the instance's single sign-on and two-factor rules apply unchanged — *inferred* from where the sign-in happens, and not documented for clients that customers create.

**The client secret is required.** Code does not work without a client secret: tested and rejected, **verified by NSerio against a live instance; not documented by Relativity**, which confirms Relativity's own statement that the grant requires one. **A script is plain text, so a secret shipped inside a distributed script can be read by anyone who holds the script**; it protects nothing against the person running it. Store it as Secret Management (Part B of the Migration Documentation) describes, in a file with the person's own permissions (`umask 077`) or in the keychain, and apply the compensating guidance below.

**Loopback listener variant (optional, per system).** A shell has no HTTP listener of its own. Where a script runs on a machine with a browser and a registered fixed-port loopback Redirect URL `http://127.0.0.1:<port>/<path>` — a plain-HTTP loopback URL is accepted, **verified by NSerio against a live instance; not documented by Relativity** — it can start `nc` or `socat` bound to `127.0.0.1` only, for one request, to read the request line `GET /<path>?code=…&state=… HTTP/1.1` and answer with a short page: on Linux the OpenBSD `nc` does it with `nc -l 127.0.0.1 PORT` (*checked by NSerio locally*); the options differ between OpenBSD, BSD and macOS, GNU and traditional `nc` and `ncat`, and `socat`, and those have not been checked. Bind to loopback only, check the `state` (another local process can connect to the port), and fall back to the manual copy when the tool is missing or the machine is headless.

**Compensating guidance** (NSerio recommendation, subject to your own security policy): set Trusted IPs where the users' outbound addresses are known and stable — they restrict where a token can be *requested* — use the recommended short Access Token Lifetime of 60 minutes, enable refresh tokens only when needed, and create one OAuth2 Client per distribution, so a leaked secret can be disabled without affecting the others. **A documentation gap.** Relativity documents no recommendation for a distributed script or desktop application; this design is what NSerio verified works.

Verification: **Verification note.** **A covered client use of the variable within thirty lines is read**; change that call and any other that uses the URL. **A URL with userinfo may already be in logs**: rotate the credential. **A token has a finite lifetime.**

OAuth2 Client: See [OAuth2 Client for Authorization Code](#oauth2-client-for-authorization-code).
<!-- generated:end SH-010 authorization-code -->

### SH-013 · HttpieAuthentication

<a id="sh-013-migration"></a>

#### Client Credentials

<!-- generated:begin SH-013 client-credentials -->
**Flow.** This entry is for Client Credentials. The "Which flow" paragraph of SH-013 in [Part A](part-a-shell.md#sh-013) says when a match belongs to this flow.

Source form: **Applies to:** `bash`, `zsh`, `ksh93` and `mksh` (and `sh`, read as `bash`); `http` and `https` (HTTPie).

```sh
http --auth "${API_USER}:${API_PASSWORD}" GET "${HOST}/Relativity.REST/api/..."
```

Under token authentication: **First, the destination:** if this Relativity instance is not RelativityOne — including a Relativity Server instance, whose own documentation governs — no migration to RelativityOne applies and the rest of this entry does not apply.

```sh
http --check-status GET "${HOST}/Relativity.REST/api/..." "Authorization:Bearer ${ACCESS_TOKEN}" "X-CSRF-Header:-"
```

Remove `-a`, `--auth`, `-A` and any userinfo in the URL. This form puts the token on the command line, where it is visible in `ps` while the command runs; on a shared host use `curl` with `-H @FILE` for this call.

Credentials: **Where the credentials come from.** The text establishes that a credential is sent as Basic, not that the destination is RelativityOne. If it is not RelativityOne, the migration is that service's own token mechanism, no migration to RelativityOne applies, and the reader stops here. If it is RelativityOne, the token comes from the token-acquisition part that follows, and the user name and password are replaced by the OAuth2 Client's client ID and secret.

**Where the token comes from.** Create an OAuth2 Client with Flow Grant Type set to Client Credentials (part 3 lists its settings), then send a form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=client_credentials`, `client_id`, `client_secret` and a `scope`. The token endpoint is **documented** for this grant. The token is then sent as `Authorization: Bearer <token>`, with `X-CSRF-Header: -` on REST and Kepler calls — the header is documented as required, and a Kepler call without it returns 404. **Applies to:** `bash`, `zsh`, `ksh93` and `mksh` (and `sh`, read as `bash`); the blocks need `curl` and `jq`, which is a stated prerequisite: no fallback that parses the JSON with a regular expression is recommended.

```sh
# HOST, CLIENT_ID, TOKEN_SCOPE and CLIENT_SECRET_FILE come from the caller's environment; the secret file is created as Secret Management (Part B of the Migration Documentation) describes.
acquire_access_token() {
	curl --silent --show-error --fail \
		--data-urlencode "grant_type=client_credentials" \
		--data-urlencode "client_id=${CLIENT_ID}" \
		--data-urlencode "client_secret@${CLIENT_SECRET_FILE}" \
		--data-urlencode "scope=${TOKEN_SCOPE}" \
		"${HOST}/Relativity/Identity/connect/token" \
		| jq -r '.access_token'
}
ACCESS_TOKEN=$(acquire_access_token)
```

`--data-urlencode "client_secret@FILE"` reads the secret from a file and keeps it out of the command line (*checked by NSerio locally*: the file is read whole, a trailing line feed included, so create it with `printf '%s'`); `curl -K -` with the same setting on standard input also works (*checked by NSerio locally*). **The scope depends on what the integration needs, and no single scope is required.** Relativity's documentation shows two values for this grant: `SystemUserInfo` on the REST authentication page, the Usage Reports page and in the official Transfer SDK sample, and `UserInfoAccess` in the Kepler .NET samples. No page says which is correct, whether both work, or what either grants, and the OAuth2 Client's `Scopes` property is documented as one that cannot be set. Use the value the API you call documents, and confirm it.

**Use the token without putting it on the command line.**

```sh
( umask 077; printf 'Authorization: Bearer %s\nX-CSRF-Header: -\n' "${ACCESS_TOKEN}" > "${HEADER_FILE}" )
curl --silent --show-error --fail -H @"${HEADER_FILE}" "${HOST}/Relativity.REST/api/..."
```

`curl -H @FILE` sends the header lines of a file (*checked by NSerio locally* on `curl` 8.17.0 and 7.81.0; documented from `curl` 7.55). A token in `-H "Authorization: Bearer $TOKEN"` is visible to every user of the machine in `ps` while the command runs, so the file form is the recommended one for a shared host.

**Cache the token, and re-acquire it on expiry — do not refresh.** Keep the token for its lifetime (part 3 recommends 60 minutes, Relativity's own wording) and request a new one with the client secret shortly before it expires (a margin of 60 seconds is reasonable) and once when a call is rejected for authentication. Client Credentials offers no refresh tokens: **verified by NSerio against a live instance; not documented by Relativity** — the Allow Refresh Tokens control does not appear for this grant in the RelativityOne user interface. Relativity's own tables and text on refresh tokens conflict with each other, so re-acquisition with the secret is the advice that holds whichever way they are read. A script that runs for longer than the token's lifetime must acquire again, not hold the first token.

**The client secret.** It is displayed once, at creation or reset. Store it as Secret Management (Part B of the Migration Documentation) describes; in a shell estate that means **a file created under `umask 077`** or the operating system's keychain, **never in the script, never in a tracked `.env` file, and never exported to child processes**. A reset invalidates the previous secret immediately, so plan the cut-over before resetting it.

**Before deploying, confirm the calling identity.** Every call now runs as the OAuth2 Client's **Context User**, not as the user in the replaced credential. If that user has different workspace permissions or item-level security, the script returns fewer rows or `Unauthorized` in production with no change to its logic, and Relativity's audit attributes every call to that one account. Confirm the Context User's permissions match those of the previous identity.

**When the script acts for a signed-in user.** Client Credentials is one service identity by construction. A script that prompts a person for a password and sends it is the Authorization Code case, and moving to it means a new OAuth2 Client, because the flow is fixed when a client is created. Which of the two flows applies depends on who supplies the credential at run time.

Verification: **Verification note.** **A token has a finite lifetime.** The HTTPie form given sends the token as an argument, and an argument is visible in `ps` while the command runs; this text does not give an HTTPie form that reads the header from a file, and does not claim that none exists. **A prompt** (`--auth USER` with no password) is interactive flow evidence, and the match is then assigned the Authorization Code flow, in which the person signs in on RelativityOne's page. **The target form was syntax-checked and not run: HTTPie has not been installed where this text was checked.**

OAuth2 Client: See [OAuth2 Client for Client Credentials](#oauth2-client-for-client-credentials).
<!-- generated:end SH-013 client-credentials -->

#### Authorization Code

<!-- generated:begin SH-013 authorization-code -->
**Flow.** This entry is for Authorization Code. The "Which flow" paragraph of SH-013 in [Part A](part-a-shell.md#sh-013) says when a match belongs to this flow.

Source form: **Applies to:** `bash`, `zsh`, `ksh93` and `mksh` (and `sh`, read as `bash`); `http` and `https` (HTTPie).

```sh
http --auth "${API_USER}:${API_PASSWORD}" GET "${HOST}/Relativity.REST/api/..."
```

Under token authentication: **First, the destination:** if this Relativity instance is not RelativityOne — including a Relativity Server instance, whose own documentation governs — no migration to RelativityOne applies and the rest of this entry does not apply.

```sh
http --check-status GET "${HOST}/Relativity.REST/api/..." "Authorization:Bearer ${ACCESS_TOKEN}" "X-CSRF-Header:-"
```

Remove `-a`, `--auth`, `-A` and any userinfo in the URL. This form puts the token on the command line, where it is visible in `ps` while the command runs; on a shared host use `curl` with `-H @FILE` for this call.

Credentials: **Where the credentials come from.** The text establishes that a credential is sent as Basic, not that the destination is RelativityOne. If it is not RelativityOne, the migration is that service's own token mechanism, no migration to RelativityOne applies, and the reader stops here. If it is RelativityOne, the token comes from the token-acquisition part that follows, and the user name and password are replaced by the OAuth2 Client's client ID and secret.

**Common to both variants.** **Applies to:** `bash`, `zsh`, `ksh93` and `mksh` (and `sh`, read as `bash`); the blocks need `curl`, `openssl` and `jq`. The text has a common part and two variants of how the script receives the code: the **manual copy** (primary) and a **loopback listener** (optional).

**Sign in.** Replace the code that collected the user's credentials with a redirect: send the user's browser to the authorize endpoint `<host>/Relativity/Identity/connect/authorize`, which leads to the RelativityOne login page. The authorize endpoint is **verified by NSerio against a live instance; not documented by Relativity**. The request carries the standard authorization-code parameters — `client_id`, `response_type=code`, the registered `redirect_uri`, a `scope`, and a `state` value the script checks on return, plus `code_challenge` and `code_challenge_method=S256` when PKCE is used; these are standard OAuth, and Relativity documents none of them. No discovery document is relied on.

**PKCE.** PKCE is required by Relativity's own documentation to obtain a refresh token, and not otherwise: a Code request that does not ask for a refresh token works without it, **verified by NSerio against a live instance; not documented by Relativity**. It is never a blanket requirement. NSerio recommends PKCE with the `S256` method (NSerio recommendation), and does not claim that Relativity accepts any particular method, which it does not document. With `openssl`:

```sh
code_verifier=$(openssl rand -base64 48 | tr -d '\r\n' | tr '+/' '-_' | tr -d '=' | cut -c1-64)
code_challenge=$(printf '%s' "${code_verifier}" | openssl dgst -sha256 -binary | openssl base64 -A | tr '+/' '-_' | tr -d '=')
state=$(openssl rand -hex 16 | tr -d '\r')
```

The pipeline gives a 64-character verifier and a 43-character challenge (*checked by NSerio locally* with OpenSSL 3.5.4 on Windows and OpenSSL 3.0.2 on Linux; the `openssl` that ships with macOS has not been checked).

**Receive the code, by the manual copy (primary).** The browser returns to the Redirect URL registered on the OAuth2 Client with the authorization code and the `state` value. **The script prints the authorize address and the person opens it in any browser; when nothing listens at the registered loopback address the browser shows a connection error and leaves the full address, with `code` and `state`, in its address bar; the person copies that address and pastes it to the script.** This works headless, over SSH, on Linux and macOS, and needs no listener (NSerio recommendation; the browser's behaviour on an unreachable loopback address is *inferred* and has not been checked). The Redirect URL must still be exactly the one registered.

```sh
urlencode() (
	LC_ALL=C
	remaining=$1
	while [ -n "${remaining}" ]; do
		rest=${remaining#?}
		character=${remaining%"${rest}"}
		case ${character} in
			[A-Za-z0-9._~-]) printf '%s' "${character}" ;;
			*) printf '%%%02X' "'${character}" ;;
		esac
		remaining=${rest}
	done
)

authorize_url="${HOST}/Relativity/Identity/connect/authorize?client_id=$(urlencode "${CLIENT_ID}")&response_type=code&redirect_uri=$(urlencode "${REDIRECT_URI}")&scope=$(urlencode "${TOKEN_SCOPE}")&state=${state}&code_challenge=${code_challenge}&code_challenge_method=S256"
printf 'Open this address in a browser and sign in:\n%s\n' "${authorize_url}"
printf 'Then copy the whole address the browser ends on and paste it here: '
IFS= read -r returned_url
```

```sh
query=${returned_url#*\?}
query=${query%%#*}
returned_code=''
returned_state=''
remaining=${query}
while [ -n "${remaining}" ]; do
	pair=${remaining%%'&'*}
	case ${remaining} in
		*'&'*) remaining=${remaining#*'&'} ;;
		*) remaining='' ;;
	esac
	case ${pair} in
		code=*) returned_code=${pair#code=} ;;
		state=*) returned_state=${pair#state=} ;;
	esac
done
[ "${returned_state}" = "${state}" ] || { printf 'The state does not match; stop.\n' >&2; exit 1; }
```

The percent-encoding function and the address parsing give the same output in `bash`, `dash`, `zsh`, `ksh93` and `mksh` (*checked by NSerio locally*). **Check the `state` before using the code.** The code is already percent-encoded in the pasted address, so it is sent with `data` (not `data-urlencode`) below and not encoded again. The block passes the code and the verifier to `curl` as a configuration on standard input (`-K -`), so neither appears on the command line of `curl`; `printf` is a shell built-in and its arguments do not appear in `ps` either.

**Exchange the code.** Send a form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=authorization_code`, the `code`, the same `redirect_uri`, the `client_id` and the `client_secret`, plus the `code_verifier` when PKCE is used. The code is exchanged at that endpoint, which Relativity documents for the Client Credentials grant only; the exchange is **verified by NSerio against a live instance; not documented by Relativity**.

```sh
# The code and the verifier travel on standard input, never on a command line; printf is a built-in and does not appear in ps.
token_response=$(printf '%s\n' \
		"data = \"grant_type=authorization_code\"" \
		"data = \"code=${returned_code}\"" \
		"data-urlencode = \"redirect_uri=${REDIRECT_URI}\"" \
		"data-urlencode = \"client_id=${CLIENT_ID}\"" \
		"data-urlencode = \"code_verifier=${code_verifier}\"" \
		"data-urlencode = \"client_secret@${CLIENT_SECRET_FILE}\"" \
	| curl --silent --show-error --fail -K - \
		"${HOST}/Relativity/Identity/connect/token")
ACCESS_TOKEN=$(printf '%s' "${token_response}" | jq -r '.access_token')
```

**Use the token.** Send `Authorization: Bearer <access token>` — the change at the use site above — with `X-CSRF-Header: -` on REST and Kepler calls, from a header file as for Client Credentials: `( umask 077; printf 'Authorization: Bearer %s\nX-CSRF-Header: -\n' "${ACCESS_TOKEN}" > "${HEADER_FILE}" )`, then `curl -H @"${HEADER_FILE}"`.

**On expiry.** With Allow Refresh Tokens enabled on the OAuth2 Client and PKCE used, the script holds a refresh token and, when the access token expires, sends a `grant_type=refresh_token` request with it and the client credentials in place of the code exchange — *inferred* from the standard OAuth shape, not tested. Without a refresh token the person signs in again. Relativity does not document a refresh token's lifetime, whether it rotates, how it is revoked, whether a revocation endpoint exists, or what revoking it does to access tokens already issued. **A refresh token is a secret:** keep it in a file created under `umask 077` or in the keychain, never on a command line.

**Scope.** No scope set is prescribed for Code. The scope `openid RelativityRequestOrigin UserInfo UserInfoAccess AuthenticationID RelativityWeb` works, **verified by NSerio against a live instance; not documented by Relativity**, verified as working and not as minimal. `state` is sent as standard OAuth practice and `nonce` is not used; Relativity does not document whether it enforces either.

**The script stops collecting the password.** After this change the script never receives, stores or forwards the user's user name and password: the user signs in on RelativityOne's own page, and the script holds only tokens. Remove the `read` that prompts for the password, the `stty -echo` around it and any storage of the password the replaced code needed.

**Single sign-on.** Sign-in happens on RelativityOne's own login page, so the instance's single sign-on and two-factor rules apply unchanged — *inferred* from where the sign-in happens, and not documented for clients that customers create.

**The client secret is required.** Code does not work without a client secret: tested and rejected, **verified by NSerio against a live instance; not documented by Relativity**, which confirms Relativity's own statement that the grant requires one. **A script is plain text, so a secret shipped inside a distributed script can be read by anyone who holds the script**; it protects nothing against the person running it. Store it as Secret Management (Part B of the Migration Documentation) describes, in a file with the person's own permissions (`umask 077`) or in the keychain, and apply the compensating guidance below.

**Loopback listener variant (optional, per system).** A shell has no HTTP listener of its own. Where a script runs on a machine with a browser and a registered fixed-port loopback Redirect URL `http://127.0.0.1:<port>/<path>` — a plain-HTTP loopback URL is accepted, **verified by NSerio against a live instance; not documented by Relativity** — it can start `nc` or `socat` bound to `127.0.0.1` only, for one request, to read the request line `GET /<path>?code=…&state=… HTTP/1.1` and answer with a short page: on Linux the OpenBSD `nc` does it with `nc -l 127.0.0.1 PORT` (*checked by NSerio locally*); the options differ between OpenBSD, BSD and macOS, GNU and traditional `nc` and `ncat`, and `socat`, and those have not been checked. Bind to loopback only, check the `state` (another local process can connect to the port), and fall back to the manual copy when the tool is missing or the machine is headless.

**Compensating guidance** (NSerio recommendation, subject to your own security policy): set Trusted IPs where the users' outbound addresses are known and stable — they restrict where a token can be *requested* — use the recommended short Access Token Lifetime of 60 minutes, enable refresh tokens only when needed, and create one OAuth2 Client per distribution, so a leaked secret can be disabled without affecting the others. **A documentation gap.** Relativity documents no recommendation for a distributed script or desktop application; this design is what NSerio verified works.

Verification: **Verification note.** **A token has a finite lifetime.** The HTTPie form given sends the token as an argument, and an argument is visible in `ps` while the command runs; this text does not give an HTTPie form that reads the header from a file, and does not claim that none exists. **A prompt** (`--auth USER` with no password) is interactive flow evidence, and the match is then assigned the Authorization Code flow, in which the person signs in on RelativityOne's page. **The target form was syntax-checked and not run: HTTPie has not been installed where this text was checked.**

OAuth2 Client: See [OAuth2 Client for Authorization Code](#oauth2-client-for-authorization-code).
<!-- generated:end SH-013 authorization-code -->

### SH-014 · Aria2cCredentials

<a id="sh-014-migration"></a>

#### Client Credentials

<!-- generated:begin SH-014 client-credentials -->
**Flow.** This entry is for Client Credentials. The "Which flow" paragraph of SH-014 in [Part A](part-a-shell.md#sh-014) says when a match belongs to this flow.

Source form: **Applies to:** `bash`, `zsh`, `ksh93` and `mksh` (and `sh`, read as `bash`); `aria2c`.

```sh
aria2c --http-user="${API_USER}" --http-passwd="${API_PASSWORD}" "${HOST}/Relativity.REST/api/..."
```

Under token authentication: **First, the destination:** if this Relativity instance is not RelativityOne — including a Relativity Server instance, whose own documentation governs — no migration to RelativityOne applies and the rest of this entry does not apply.

```sh
aria2c --header="Authorization: Bearer ${ACCESS_TOKEN}" --header="X-CSRF-Header: -" "${HOST}/Relativity.REST/api/..."
```

Remove `--http-user`, `--http-passwd` and any userinfo in the URL.

Credentials: **Where the credentials come from.** The text establishes that a credential is sent as Basic, not that the destination is RelativityOne. If it is not RelativityOne, the migration is that service's own token mechanism, no migration to RelativityOne applies, and the reader stops here. If it is RelativityOne, the token comes from the token-acquisition part that follows, and the user name and password are replaced by the OAuth2 Client's client ID and secret.

**Where the token comes from.** Create an OAuth2 Client with Flow Grant Type set to Client Credentials (part 3 lists its settings), then send a form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=client_credentials`, `client_id`, `client_secret` and a `scope`. The token endpoint is **documented** for this grant. The token is then sent as `Authorization: Bearer <token>`, with `X-CSRF-Header: -` on REST and Kepler calls — the header is documented as required, and a Kepler call without it returns 404. **Applies to:** `bash`, `zsh`, `ksh93` and `mksh` (and `sh`, read as `bash`); the blocks need `curl` and `jq`, which is a stated prerequisite: no fallback that parses the JSON with a regular expression is recommended.

```sh
# HOST, CLIENT_ID, TOKEN_SCOPE and CLIENT_SECRET_FILE come from the caller's environment; the secret file is created as Secret Management (Part B of the Migration Documentation) describes.
acquire_access_token() {
	curl --silent --show-error --fail \
		--data-urlencode "grant_type=client_credentials" \
		--data-urlencode "client_id=${CLIENT_ID}" \
		--data-urlencode "client_secret@${CLIENT_SECRET_FILE}" \
		--data-urlencode "scope=${TOKEN_SCOPE}" \
		"${HOST}/Relativity/Identity/connect/token" \
		| jq -r '.access_token'
}
ACCESS_TOKEN=$(acquire_access_token)
```

`--data-urlencode "client_secret@FILE"` reads the secret from a file and keeps it out of the command line (*checked by NSerio locally*: the file is read whole, a trailing line feed included, so create it with `printf '%s'`); `curl -K -` with the same setting on standard input also works (*checked by NSerio locally*). **The scope depends on what the integration needs, and no single scope is required.** Relativity's documentation shows two values for this grant: `SystemUserInfo` on the REST authentication page, the Usage Reports page and in the official Transfer SDK sample, and `UserInfoAccess` in the Kepler .NET samples. No page says which is correct, whether both work, or what either grants, and the OAuth2 Client's `Scopes` property is documented as one that cannot be set. Use the value the API you call documents, and confirm it.

**Use the token without putting it on the command line.**

```sh
( umask 077; printf 'Authorization: Bearer %s\nX-CSRF-Header: -\n' "${ACCESS_TOKEN}" > "${HEADER_FILE}" )
curl --silent --show-error --fail -H @"${HEADER_FILE}" "${HOST}/Relativity.REST/api/..."
```

`curl -H @FILE` sends the header lines of a file (*checked by NSerio locally* on `curl` 8.17.0 and 7.81.0; documented from `curl` 7.55). A token in `-H "Authorization: Bearer $TOKEN"` is visible to every user of the machine in `ps` while the command runs, so the file form is the recommended one for a shared host.

**Cache the token, and re-acquire it on expiry — do not refresh.** Keep the token for its lifetime (part 3 recommends 60 minutes, Relativity's own wording) and request a new one with the client secret shortly before it expires (a margin of 60 seconds is reasonable) and once when a call is rejected for authentication. Client Credentials offers no refresh tokens: **verified by NSerio against a live instance; not documented by Relativity** — the Allow Refresh Tokens control does not appear for this grant in the RelativityOne user interface. Relativity's own tables and text on refresh tokens conflict with each other, so re-acquisition with the secret is the advice that holds whichever way they are read. A script that runs for longer than the token's lifetime must acquire again, not hold the first token.

**The client secret.** It is displayed once, at creation or reset. Store it as Secret Management (Part B of the Migration Documentation) describes; in a shell estate that means **a file created under `umask 077`** or the operating system's keychain, **never in the script, never in a tracked `.env` file, and never exported to child processes**. A reset invalidates the previous secret immediately, so plan the cut-over before resetting it.

**Before deploying, confirm the calling identity.** Every call now runs as the OAuth2 Client's **Context User**, not as the user in the replaced credential. If that user has different workspace permissions or item-level security, the script returns fewer rows or `Unauthorized` in production with no change to its logic, and Relativity's audit attributes every call to that one account. Confirm the Context User's permissions match those of the previous identity.

**When the script acts for a signed-in user.** Client Credentials is one service identity by construction. A script that prompts a person for a password and sends it is the Authorization Code case, and moving to it means a new OAuth2 Client, because the flow is fixed when a client is created. Which of the two flows applies depends on who supplies the credential at run time.

Verification: **Verification note.** **A token has a finite lifetime.** `aria2c` has no `-H @FILE` form, so the token is on the command line while it runs: on a shared host use `curl` for this call. **The target form was syntax-checked and not run: `aria2c` has not been installed where this text was checked.**

OAuth2 Client: See [OAuth2 Client for Client Credentials](#oauth2-client-for-client-credentials).
<!-- generated:end SH-014 client-credentials -->

#### Authorization Code

<!-- generated:begin SH-014 authorization-code -->
**Flow.** This entry is for Authorization Code. The "Which flow" paragraph of SH-014 in [Part A](part-a-shell.md#sh-014) says when a match belongs to this flow.

Source form: **Applies to:** `bash`, `zsh`, `ksh93` and `mksh` (and `sh`, read as `bash`); `aria2c`.

```sh
aria2c --http-user="${API_USER}" --http-passwd="${API_PASSWORD}" "${HOST}/Relativity.REST/api/..."
```

Under token authentication: **First, the destination:** if this Relativity instance is not RelativityOne — including a Relativity Server instance, whose own documentation governs — no migration to RelativityOne applies and the rest of this entry does not apply.

```sh
aria2c --header="Authorization: Bearer ${ACCESS_TOKEN}" --header="X-CSRF-Header: -" "${HOST}/Relativity.REST/api/..."
```

Remove `--http-user`, `--http-passwd` and any userinfo in the URL.

Credentials: **Where the credentials come from.** The text establishes that a credential is sent as Basic, not that the destination is RelativityOne. If it is not RelativityOne, the migration is that service's own token mechanism, no migration to RelativityOne applies, and the reader stops here. If it is RelativityOne, the token comes from the token-acquisition part that follows, and the user name and password are replaced by the OAuth2 Client's client ID and secret.

**Common to both variants.** **Applies to:** `bash`, `zsh`, `ksh93` and `mksh` (and `sh`, read as `bash`); the blocks need `curl`, `openssl` and `jq`. The text has a common part and two variants of how the script receives the code: the **manual copy** (primary) and a **loopback listener** (optional).

**Sign in.** Replace the code that collected the user's credentials with a redirect: send the user's browser to the authorize endpoint `<host>/Relativity/Identity/connect/authorize`, which leads to the RelativityOne login page. The authorize endpoint is **verified by NSerio against a live instance; not documented by Relativity**. The request carries the standard authorization-code parameters — `client_id`, `response_type=code`, the registered `redirect_uri`, a `scope`, and a `state` value the script checks on return, plus `code_challenge` and `code_challenge_method=S256` when PKCE is used; these are standard OAuth, and Relativity documents none of them. No discovery document is relied on.

**PKCE.** PKCE is required by Relativity's own documentation to obtain a refresh token, and not otherwise: a Code request that does not ask for a refresh token works without it, **verified by NSerio against a live instance; not documented by Relativity**. It is never a blanket requirement. NSerio recommends PKCE with the `S256` method (NSerio recommendation), and does not claim that Relativity accepts any particular method, which it does not document. With `openssl`:

```sh
code_verifier=$(openssl rand -base64 48 | tr -d '\r\n' | tr '+/' '-_' | tr -d '=' | cut -c1-64)
code_challenge=$(printf '%s' "${code_verifier}" | openssl dgst -sha256 -binary | openssl base64 -A | tr '+/' '-_' | tr -d '=')
state=$(openssl rand -hex 16 | tr -d '\r')
```

The pipeline gives a 64-character verifier and a 43-character challenge (*checked by NSerio locally* with OpenSSL 3.5.4 on Windows and OpenSSL 3.0.2 on Linux; the `openssl` that ships with macOS has not been checked).

**Receive the code, by the manual copy (primary).** The browser returns to the Redirect URL registered on the OAuth2 Client with the authorization code and the `state` value. **The script prints the authorize address and the person opens it in any browser; when nothing listens at the registered loopback address the browser shows a connection error and leaves the full address, with `code` and `state`, in its address bar; the person copies that address and pastes it to the script.** This works headless, over SSH, on Linux and macOS, and needs no listener (NSerio recommendation; the browser's behaviour on an unreachable loopback address is *inferred* and has not been checked). The Redirect URL must still be exactly the one registered.

```sh
urlencode() (
	LC_ALL=C
	remaining=$1
	while [ -n "${remaining}" ]; do
		rest=${remaining#?}
		character=${remaining%"${rest}"}
		case ${character} in
			[A-Za-z0-9._~-]) printf '%s' "${character}" ;;
			*) printf '%%%02X' "'${character}" ;;
		esac
		remaining=${rest}
	done
)

authorize_url="${HOST}/Relativity/Identity/connect/authorize?client_id=$(urlencode "${CLIENT_ID}")&response_type=code&redirect_uri=$(urlencode "${REDIRECT_URI}")&scope=$(urlencode "${TOKEN_SCOPE}")&state=${state}&code_challenge=${code_challenge}&code_challenge_method=S256"
printf 'Open this address in a browser and sign in:\n%s\n' "${authorize_url}"
printf 'Then copy the whole address the browser ends on and paste it here: '
IFS= read -r returned_url
```

```sh
query=${returned_url#*\?}
query=${query%%#*}
returned_code=''
returned_state=''
remaining=${query}
while [ -n "${remaining}" ]; do
	pair=${remaining%%'&'*}
	case ${remaining} in
		*'&'*) remaining=${remaining#*'&'} ;;
		*) remaining='' ;;
	esac
	case ${pair} in
		code=*) returned_code=${pair#code=} ;;
		state=*) returned_state=${pair#state=} ;;
	esac
done
[ "${returned_state}" = "${state}" ] || { printf 'The state does not match; stop.\n' >&2; exit 1; }
```

The percent-encoding function and the address parsing give the same output in `bash`, `dash`, `zsh`, `ksh93` and `mksh` (*checked by NSerio locally*). **Check the `state` before using the code.** The code is already percent-encoded in the pasted address, so it is sent with `data` (not `data-urlencode`) below and not encoded again. The block passes the code and the verifier to `curl` as a configuration on standard input (`-K -`), so neither appears on the command line of `curl`; `printf` is a shell built-in and its arguments do not appear in `ps` either.

**Exchange the code.** Send a form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=authorization_code`, the `code`, the same `redirect_uri`, the `client_id` and the `client_secret`, plus the `code_verifier` when PKCE is used. The code is exchanged at that endpoint, which Relativity documents for the Client Credentials grant only; the exchange is **verified by NSerio against a live instance; not documented by Relativity**.

```sh
# The code and the verifier travel on standard input, never on a command line; printf is a built-in and does not appear in ps.
token_response=$(printf '%s\n' \
		"data = \"grant_type=authorization_code\"" \
		"data = \"code=${returned_code}\"" \
		"data-urlencode = \"redirect_uri=${REDIRECT_URI}\"" \
		"data-urlencode = \"client_id=${CLIENT_ID}\"" \
		"data-urlencode = \"code_verifier=${code_verifier}\"" \
		"data-urlencode = \"client_secret@${CLIENT_SECRET_FILE}\"" \
	| curl --silent --show-error --fail -K - \
		"${HOST}/Relativity/Identity/connect/token")
ACCESS_TOKEN=$(printf '%s' "${token_response}" | jq -r '.access_token')
```

**Use the token.** Send `Authorization: Bearer <access token>` — the change at the use site above — with `X-CSRF-Header: -` on REST and Kepler calls, from a header file as for Client Credentials: `( umask 077; printf 'Authorization: Bearer %s\nX-CSRF-Header: -\n' "${ACCESS_TOKEN}" > "${HEADER_FILE}" )`, then `curl -H @"${HEADER_FILE}"`.

**On expiry.** With Allow Refresh Tokens enabled on the OAuth2 Client and PKCE used, the script holds a refresh token and, when the access token expires, sends a `grant_type=refresh_token` request with it and the client credentials in place of the code exchange — *inferred* from the standard OAuth shape, not tested. Without a refresh token the person signs in again. Relativity does not document a refresh token's lifetime, whether it rotates, how it is revoked, whether a revocation endpoint exists, or what revoking it does to access tokens already issued. **A refresh token is a secret:** keep it in a file created under `umask 077` or in the keychain, never on a command line.

**Scope.** No scope set is prescribed for Code. The scope `openid RelativityRequestOrigin UserInfo UserInfoAccess AuthenticationID RelativityWeb` works, **verified by NSerio against a live instance; not documented by Relativity**, verified as working and not as minimal. `state` is sent as standard OAuth practice and `nonce` is not used; Relativity does not document whether it enforces either.

**The script stops collecting the password.** After this change the script never receives, stores or forwards the user's user name and password: the user signs in on RelativityOne's own page, and the script holds only tokens. Remove the `read` that prompts for the password, the `stty -echo` around it and any storage of the password the replaced code needed.

**Single sign-on.** Sign-in happens on RelativityOne's own login page, so the instance's single sign-on and two-factor rules apply unchanged — *inferred* from where the sign-in happens, and not documented for clients that customers create.

**The client secret is required.** Code does not work without a client secret: tested and rejected, **verified by NSerio against a live instance; not documented by Relativity**, which confirms Relativity's own statement that the grant requires one. **A script is plain text, so a secret shipped inside a distributed script can be read by anyone who holds the script**; it protects nothing against the person running it. Store it as Secret Management (Part B of the Migration Documentation) describes, in a file with the person's own permissions (`umask 077`) or in the keychain, and apply the compensating guidance below.

**Loopback listener variant (optional, per system).** A shell has no HTTP listener of its own. Where a script runs on a machine with a browser and a registered fixed-port loopback Redirect URL `http://127.0.0.1:<port>/<path>` — a plain-HTTP loopback URL is accepted, **verified by NSerio against a live instance; not documented by Relativity** — it can start `nc` or `socat` bound to `127.0.0.1` only, for one request, to read the request line `GET /<path>?code=…&state=… HTTP/1.1` and answer with a short page: on Linux the OpenBSD `nc` does it with `nc -l 127.0.0.1 PORT` (*checked by NSerio locally*); the options differ between OpenBSD, BSD and macOS, GNU and traditional `nc` and `ncat`, and `socat`, and those have not been checked. Bind to loopback only, check the `state` (another local process can connect to the port), and fall back to the manual copy when the tool is missing or the machine is headless.

**Compensating guidance** (NSerio recommendation, subject to your own security policy): set Trusted IPs where the users' outbound addresses are known and stable — they restrict where a token can be *requested* — use the recommended short Access Token Lifetime of 60 minutes, enable refresh tokens only when needed, and create one OAuth2 Client per distribution, so a leaked secret can be disabled without affecting the others. **A documentation gap.** Relativity documents no recommendation for a distributed script or desktop application; this design is what NSerio verified works.

Verification: **Verification note.** **A token has a finite lifetime.** `aria2c` has no `-H @FILE` form, so the token is on the command line while it runs: on a shared host use `curl` for this call. **The target form was syntax-checked and not run: `aria2c` has not been installed where this text was checked.**

OAuth2 Client: See [OAuth2 Client for Authorization Code](#oauth2-client-for-authorization-code).
<!-- generated:end SH-014 authorization-code -->

### SH-016 · WgetExecuteCredentialsPreemptive

<a id="sh-016-migration"></a>

#### Client Credentials

<!-- generated:begin SH-016 client-credentials -->
**Flow.** This entry is for Client Credentials. The "Which flow" paragraph of SH-016 in [Part A](part-a-shell.md#sh-016) says when a match belongs to this flow.

Source form: **Applies to:** `bash`, `zsh`, `ksh93` and `mksh` (and `sh`, read as `bash`); `wget`.

```sh
wget --auth-no-challenge -e "http_user=${API_USER}" -e "http_password=${API_PASSWORD}" "${HOST}/Relativity.REST/api/..."
```

Under token authentication: **First, the destination:** if this Relativity instance is not RelativityOne — including a Relativity Server instance, whose own documentation governs — no migration to RelativityOne applies and the rest of this entry does not apply.

```sh
wget --header="Authorization: Bearer ${ACCESS_TOKEN}" --header="X-CSRF-Header: -" "${HOST}/Relativity.REST/api/..."
```

Remove `--auth-no-challenge`, every `-e` or `--execute` that sets `http_user`, `http_password`, `user` or `password`, and any userinfo in the URL.

Credentials: **Where the credentials come from.** The text establishes that a credential is sent as Basic, not that the destination is RelativityOne. If it is not RelativityOne, the migration is that service's own token mechanism, no migration to RelativityOne applies, and the reader stops here. If it is RelativityOne, the token comes from the token-acquisition part that follows, and the user name and password are replaced by the OAuth2 Client's client ID and secret.

**Where the token comes from.** Create an OAuth2 Client with Flow Grant Type set to Client Credentials (part 3 lists its settings), then send a form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=client_credentials`, `client_id`, `client_secret` and a `scope`. The token endpoint is **documented** for this grant. The token is then sent as `Authorization: Bearer <token>`, with `X-CSRF-Header: -` on REST and Kepler calls — the header is documented as required, and a Kepler call without it returns 404. **Applies to:** `bash`, `zsh`, `ksh93` and `mksh` (and `sh`, read as `bash`); the blocks need `curl` and `jq`, which is a stated prerequisite: no fallback that parses the JSON with a regular expression is recommended.

```sh
# HOST, CLIENT_ID, TOKEN_SCOPE and CLIENT_SECRET_FILE come from the caller's environment; the secret file is created as Secret Management (Part B of the Migration Documentation) describes.
acquire_access_token() {
	curl --silent --show-error --fail \
		--data-urlencode "grant_type=client_credentials" \
		--data-urlencode "client_id=${CLIENT_ID}" \
		--data-urlencode "client_secret@${CLIENT_SECRET_FILE}" \
		--data-urlencode "scope=${TOKEN_SCOPE}" \
		"${HOST}/Relativity/Identity/connect/token" \
		| jq -r '.access_token'
}
ACCESS_TOKEN=$(acquire_access_token)
```

`--data-urlencode "client_secret@FILE"` reads the secret from a file and keeps it out of the command line (*checked by NSerio locally*: the file is read whole, a trailing line feed included, so create it with `printf '%s'`); `curl -K -` with the same setting on standard input also works (*checked by NSerio locally*). **The scope depends on what the integration needs, and no single scope is required.** Relativity's documentation shows two values for this grant: `SystemUserInfo` on the REST authentication page, the Usage Reports page and in the official Transfer SDK sample, and `UserInfoAccess` in the Kepler .NET samples. No page says which is correct, whether both work, or what either grants, and the OAuth2 Client's `Scopes` property is documented as one that cannot be set. Use the value the API you call documents, and confirm it.

**Use the token without putting it on the command line.**

```sh
( umask 077; printf 'Authorization: Bearer %s\nX-CSRF-Header: -\n' "${ACCESS_TOKEN}" > "${HEADER_FILE}" )
curl --silent --show-error --fail -H @"${HEADER_FILE}" "${HOST}/Relativity.REST/api/..."
```

`curl -H @FILE` sends the header lines of a file (*checked by NSerio locally* on `curl` 8.17.0 and 7.81.0; documented from `curl` 7.55). A token in `-H "Authorization: Bearer $TOKEN"` is visible to every user of the machine in `ps` while the command runs, so the file form is the recommended one for a shared host.

**Cache the token, and re-acquire it on expiry — do not refresh.** Keep the token for its lifetime (part 3 recommends 60 minutes, Relativity's own wording) and request a new one with the client secret shortly before it expires (a margin of 60 seconds is reasonable) and once when a call is rejected for authentication. Client Credentials offers no refresh tokens: **verified by NSerio against a live instance; not documented by Relativity** — the Allow Refresh Tokens control does not appear for this grant in the RelativityOne user interface. Relativity's own tables and text on refresh tokens conflict with each other, so re-acquisition with the secret is the advice that holds whichever way they are read. A script that runs for longer than the token's lifetime must acquire again, not hold the first token.

**The client secret.** It is displayed once, at creation or reset. Store it as Secret Management (Part B of the Migration Documentation) describes; in a shell estate that means **a file created under `umask 077`** or the operating system's keychain, **never in the script, never in a tracked `.env` file, and never exported to child processes**. A reset invalidates the previous secret immediately, so plan the cut-over before resetting it.

**Before deploying, confirm the calling identity.** Every call now runs as the OAuth2 Client's **Context User**, not as the user in the replaced credential. If that user has different workspace permissions or item-level security, the script returns fewer rows or `Unauthorized` in production with no change to its logic, and Relativity's audit attributes every call to that one account. Confirm the Context User's permissions match those of the previous identity.

**When the script acts for a signed-in user.** Client Credentials is one service identity by construction. A script that prompts a person for a password and sends it is the Authorization Code case, and moving to it means a new OAuth2 Client, because the flow is fixed when a client is created. Which of the two flows applies depends on who supplies the credential at run time.

Verification: **Verification note.** **A token has a finite lifetime.** `wget` has no `-H @FILE` form, so on a shared host use `curl` for this call.

OAuth2 Client: See [OAuth2 Client for Client Credentials](#oauth2-client-for-client-credentials).
<!-- generated:end SH-016 client-credentials -->

#### Authorization Code

<!-- generated:begin SH-016 authorization-code -->
**Flow.** This entry is for Authorization Code. The "Which flow" paragraph of SH-016 in [Part A](part-a-shell.md#sh-016) says when a match belongs to this flow.

Source form: **Applies to:** `bash`, `zsh`, `ksh93` and `mksh` (and `sh`, read as `bash`); `wget`.

```sh
wget --auth-no-challenge -e "http_user=${API_USER}" -e "http_password=${API_PASSWORD}" "${HOST}/Relativity.REST/api/..."
```

Under token authentication: **First, the destination:** if this Relativity instance is not RelativityOne — including a Relativity Server instance, whose own documentation governs — no migration to RelativityOne applies and the rest of this entry does not apply.

```sh
wget --header="Authorization: Bearer ${ACCESS_TOKEN}" --header="X-CSRF-Header: -" "${HOST}/Relativity.REST/api/..."
```

Remove `--auth-no-challenge`, every `-e` or `--execute` that sets `http_user`, `http_password`, `user` or `password`, and any userinfo in the URL.

Credentials: **Where the credentials come from.** The text establishes that a credential is sent as Basic, not that the destination is RelativityOne. If it is not RelativityOne, the migration is that service's own token mechanism, no migration to RelativityOne applies, and the reader stops here. If it is RelativityOne, the token comes from the token-acquisition part that follows, and the user name and password are replaced by the OAuth2 Client's client ID and secret.

**Common to both variants.** **Applies to:** `bash`, `zsh`, `ksh93` and `mksh` (and `sh`, read as `bash`); the blocks need `curl`, `openssl` and `jq`. The text has a common part and two variants of how the script receives the code: the **manual copy** (primary) and a **loopback listener** (optional).

**Sign in.** Replace the code that collected the user's credentials with a redirect: send the user's browser to the authorize endpoint `<host>/Relativity/Identity/connect/authorize`, which leads to the RelativityOne login page. The authorize endpoint is **verified by NSerio against a live instance; not documented by Relativity**. The request carries the standard authorization-code parameters — `client_id`, `response_type=code`, the registered `redirect_uri`, a `scope`, and a `state` value the script checks on return, plus `code_challenge` and `code_challenge_method=S256` when PKCE is used; these are standard OAuth, and Relativity documents none of them. No discovery document is relied on.

**PKCE.** PKCE is required by Relativity's own documentation to obtain a refresh token, and not otherwise: a Code request that does not ask for a refresh token works without it, **verified by NSerio against a live instance; not documented by Relativity**. It is never a blanket requirement. NSerio recommends PKCE with the `S256` method (NSerio recommendation), and does not claim that Relativity accepts any particular method, which it does not document. With `openssl`:

```sh
code_verifier=$(openssl rand -base64 48 | tr -d '\r\n' | tr '+/' '-_' | tr -d '=' | cut -c1-64)
code_challenge=$(printf '%s' "${code_verifier}" | openssl dgst -sha256 -binary | openssl base64 -A | tr '+/' '-_' | tr -d '=')
state=$(openssl rand -hex 16 | tr -d '\r')
```

The pipeline gives a 64-character verifier and a 43-character challenge (*checked by NSerio locally* with OpenSSL 3.5.4 on Windows and OpenSSL 3.0.2 on Linux; the `openssl` that ships with macOS has not been checked).

**Receive the code, by the manual copy (primary).** The browser returns to the Redirect URL registered on the OAuth2 Client with the authorization code and the `state` value. **The script prints the authorize address and the person opens it in any browser; when nothing listens at the registered loopback address the browser shows a connection error and leaves the full address, with `code` and `state`, in its address bar; the person copies that address and pastes it to the script.** This works headless, over SSH, on Linux and macOS, and needs no listener (NSerio recommendation; the browser's behaviour on an unreachable loopback address is *inferred* and has not been checked). The Redirect URL must still be exactly the one registered.

```sh
urlencode() (
	LC_ALL=C
	remaining=$1
	while [ -n "${remaining}" ]; do
		rest=${remaining#?}
		character=${remaining%"${rest}"}
		case ${character} in
			[A-Za-z0-9._~-]) printf '%s' "${character}" ;;
			*) printf '%%%02X' "'${character}" ;;
		esac
		remaining=${rest}
	done
)

authorize_url="${HOST}/Relativity/Identity/connect/authorize?client_id=$(urlencode "${CLIENT_ID}")&response_type=code&redirect_uri=$(urlencode "${REDIRECT_URI}")&scope=$(urlencode "${TOKEN_SCOPE}")&state=${state}&code_challenge=${code_challenge}&code_challenge_method=S256"
printf 'Open this address in a browser and sign in:\n%s\n' "${authorize_url}"
printf 'Then copy the whole address the browser ends on and paste it here: '
IFS= read -r returned_url
```

```sh
query=${returned_url#*\?}
query=${query%%#*}
returned_code=''
returned_state=''
remaining=${query}
while [ -n "${remaining}" ]; do
	pair=${remaining%%'&'*}
	case ${remaining} in
		*'&'*) remaining=${remaining#*'&'} ;;
		*) remaining='' ;;
	esac
	case ${pair} in
		code=*) returned_code=${pair#code=} ;;
		state=*) returned_state=${pair#state=} ;;
	esac
done
[ "${returned_state}" = "${state}" ] || { printf 'The state does not match; stop.\n' >&2; exit 1; }
```

The percent-encoding function and the address parsing give the same output in `bash`, `dash`, `zsh`, `ksh93` and `mksh` (*checked by NSerio locally*). **Check the `state` before using the code.** The code is already percent-encoded in the pasted address, so it is sent with `data` (not `data-urlencode`) below and not encoded again. The block passes the code and the verifier to `curl` as a configuration on standard input (`-K -`), so neither appears on the command line of `curl`; `printf` is a shell built-in and its arguments do not appear in `ps` either.

**Exchange the code.** Send a form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=authorization_code`, the `code`, the same `redirect_uri`, the `client_id` and the `client_secret`, plus the `code_verifier` when PKCE is used. The code is exchanged at that endpoint, which Relativity documents for the Client Credentials grant only; the exchange is **verified by NSerio against a live instance; not documented by Relativity**.

```sh
# The code and the verifier travel on standard input, never on a command line; printf is a built-in and does not appear in ps.
token_response=$(printf '%s\n' \
		"data = \"grant_type=authorization_code\"" \
		"data = \"code=${returned_code}\"" \
		"data-urlencode = \"redirect_uri=${REDIRECT_URI}\"" \
		"data-urlencode = \"client_id=${CLIENT_ID}\"" \
		"data-urlencode = \"code_verifier=${code_verifier}\"" \
		"data-urlencode = \"client_secret@${CLIENT_SECRET_FILE}\"" \
	| curl --silent --show-error --fail -K - \
		"${HOST}/Relativity/Identity/connect/token")
ACCESS_TOKEN=$(printf '%s' "${token_response}" | jq -r '.access_token')
```

**Use the token.** Send `Authorization: Bearer <access token>` — the change at the use site above — with `X-CSRF-Header: -` on REST and Kepler calls, from a header file as for Client Credentials: `( umask 077; printf 'Authorization: Bearer %s\nX-CSRF-Header: -\n' "${ACCESS_TOKEN}" > "${HEADER_FILE}" )`, then `curl -H @"${HEADER_FILE}"`.

**On expiry.** With Allow Refresh Tokens enabled on the OAuth2 Client and PKCE used, the script holds a refresh token and, when the access token expires, sends a `grant_type=refresh_token` request with it and the client credentials in place of the code exchange — *inferred* from the standard OAuth shape, not tested. Without a refresh token the person signs in again. Relativity does not document a refresh token's lifetime, whether it rotates, how it is revoked, whether a revocation endpoint exists, or what revoking it does to access tokens already issued. **A refresh token is a secret:** keep it in a file created under `umask 077` or in the keychain, never on a command line.

**Scope.** No scope set is prescribed for Code. The scope `openid RelativityRequestOrigin UserInfo UserInfoAccess AuthenticationID RelativityWeb` works, **verified by NSerio against a live instance; not documented by Relativity**, verified as working and not as minimal. `state` is sent as standard OAuth practice and `nonce` is not used; Relativity does not document whether it enforces either.

**The script stops collecting the password.** After this change the script never receives, stores or forwards the user's user name and password: the user signs in on RelativityOne's own page, and the script holds only tokens. Remove the `read` that prompts for the password, the `stty -echo` around it and any storage of the password the replaced code needed.

**Single sign-on.** Sign-in happens on RelativityOne's own login page, so the instance's single sign-on and two-factor rules apply unchanged — *inferred* from where the sign-in happens, and not documented for clients that customers create.

**The client secret is required.** Code does not work without a client secret: tested and rejected, **verified by NSerio against a live instance; not documented by Relativity**, which confirms Relativity's own statement that the grant requires one. **A script is plain text, so a secret shipped inside a distributed script can be read by anyone who holds the script**; it protects nothing against the person running it. Store it as Secret Management (Part B of the Migration Documentation) describes, in a file with the person's own permissions (`umask 077`) or in the keychain, and apply the compensating guidance below.

**Loopback listener variant (optional, per system).** A shell has no HTTP listener of its own. Where a script runs on a machine with a browser and a registered fixed-port loopback Redirect URL `http://127.0.0.1:<port>/<path>` — a plain-HTTP loopback URL is accepted, **verified by NSerio against a live instance; not documented by Relativity** — it can start `nc` or `socat` bound to `127.0.0.1` only, for one request, to read the request line `GET /<path>?code=…&state=… HTTP/1.1` and answer with a short page: on Linux the OpenBSD `nc` does it with `nc -l 127.0.0.1 PORT` (*checked by NSerio locally*); the options differ between OpenBSD, BSD and macOS, GNU and traditional `nc` and `ncat`, and `socat`, and those have not been checked. Bind to loopback only, check the `state` (another local process can connect to the port), and fall back to the manual copy when the tool is missing or the machine is headless.

**Compensating guidance** (NSerio recommendation, subject to your own security policy): set Trusted IPs where the users' outbound addresses are known and stable — they restrict where a token can be *requested* — use the recommended short Access Token Lifetime of 60 minutes, enable refresh tokens only when needed, and create one OAuth2 Client per distribution, so a leaked secret can be disabled without affecting the others. **A documentation gap.** Relativity documents no recommendation for a distributed script or desktop application; this design is what NSerio verified works.

Verification: **Verification note.** **A token has a finite lifetime.** `wget` has no `-H @FILE` form, so on a shared host use `curl` for this call.

OAuth2 Client: See [OAuth2 Client for Authorization Code](#oauth2-client-for-authorization-code).
<!-- generated:end SH-016 authorization-code -->

### SH-018 · HandBuiltBasicRequest

<a id="sh-018-migration"></a>

#### Client Credentials

<!-- generated:begin SH-018 client-credentials -->
**Flow.** This entry is for Client Credentials. The "Which flow" paragraph of SH-018 in [Part A](part-a-shell.md#sh-018) says when a match belongs to this flow.

Source form: **Applies to:** `bash` and `ksh93` (the `/dev/tcp` form); `bash`, `zsh`, `ksh93` and `mksh`, and `sh` read as `bash` (the `nc`, `socat` and `openssl s_client` forms).

```bash
exec 3<>/dev/tcp/host.example/80
printf 'GET /api/ HTTP/1.1\r\nHost: host.example\r\nAuthorization: Basic %s\r\n\r\n' "${ENCODED}" >&3
```

Under token authentication: **First, the destination:** if this Relativity instance is not RelativityOne — including a Relativity Server instance, whose own documentation governs — no migration to RelativityOne applies and the rest of this entry does not apply.

```sh
curl --silent --show-error --fail -H @"${HEADER_FILE}" "${HOST}/Relativity.REST/api/..."
```

Replace the hand-built request with a client that speaks HTTPS and send the token with `-H @"${HEADER_FILE}"`, as in the target form for a header that names the scheme.

Credentials: **Where the credentials come from.** The text establishes that a credential is sent as Basic, not that the destination is RelativityOne. If it is not RelativityOne, the migration is that service's own token mechanism, no migration to RelativityOne applies, and the reader stops here. If it is RelativityOne, the token comes from the token-acquisition part that follows, and the user name and password are replaced by the OAuth2 Client's client ID and secret.

**Where the token comes from.** Create an OAuth2 Client with Flow Grant Type set to Client Credentials (part 3 lists its settings), then send a form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=client_credentials`, `client_id`, `client_secret` and a `scope`. The token endpoint is **documented** for this grant. The token is then sent as `Authorization: Bearer <token>`, with `X-CSRF-Header: -` on REST and Kepler calls — the header is documented as required, and a Kepler call without it returns 404. **Applies to:** `bash`, `zsh`, `ksh93` and `mksh` (and `sh`, read as `bash`); the blocks need `curl` and `jq`, which is a stated prerequisite: no fallback that parses the JSON with a regular expression is recommended.

```sh
# HOST, CLIENT_ID, TOKEN_SCOPE and CLIENT_SECRET_FILE come from the caller's environment; the secret file is created as Secret Management (Part B of the Migration Documentation) describes.
acquire_access_token() {
	curl --silent --show-error --fail \
		--data-urlencode "grant_type=client_credentials" \
		--data-urlencode "client_id=${CLIENT_ID}" \
		--data-urlencode "client_secret@${CLIENT_SECRET_FILE}" \
		--data-urlencode "scope=${TOKEN_SCOPE}" \
		"${HOST}/Relativity/Identity/connect/token" \
		| jq -r '.access_token'
}
ACCESS_TOKEN=$(acquire_access_token)
```

`--data-urlencode "client_secret@FILE"` reads the secret from a file and keeps it out of the command line (*checked by NSerio locally*: the file is read whole, a trailing line feed included, so create it with `printf '%s'`); `curl -K -` with the same setting on standard input also works (*checked by NSerio locally*). **The scope depends on what the integration needs, and no single scope is required.** Relativity's documentation shows two values for this grant: `SystemUserInfo` on the REST authentication page, the Usage Reports page and in the official Transfer SDK sample, and `UserInfoAccess` in the Kepler .NET samples. No page says which is correct, whether both work, or what either grants, and the OAuth2 Client's `Scopes` property is documented as one that cannot be set. Use the value the API you call documents, and confirm it.

**Use the token without putting it on the command line.**

```sh
( umask 077; printf 'Authorization: Bearer %s\nX-CSRF-Header: -\n' "${ACCESS_TOKEN}" > "${HEADER_FILE}" )
curl --silent --show-error --fail -H @"${HEADER_FILE}" "${HOST}/Relativity.REST/api/..."
```

`curl -H @FILE` sends the header lines of a file (*checked by NSerio locally* on `curl` 8.17.0 and 7.81.0; documented from `curl` 7.55). A token in `-H "Authorization: Bearer $TOKEN"` is visible to every user of the machine in `ps` while the command runs, so the file form is the recommended one for a shared host.

**Cache the token, and re-acquire it on expiry — do not refresh.** Keep the token for its lifetime (part 3 recommends 60 minutes, Relativity's own wording) and request a new one with the client secret shortly before it expires (a margin of 60 seconds is reasonable) and once when a call is rejected for authentication. Client Credentials offers no refresh tokens: **verified by NSerio against a live instance; not documented by Relativity** — the Allow Refresh Tokens control does not appear for this grant in the RelativityOne user interface. Relativity's own tables and text on refresh tokens conflict with each other, so re-acquisition with the secret is the advice that holds whichever way they are read. A script that runs for longer than the token's lifetime must acquire again, not hold the first token.

**The client secret.** It is displayed once, at creation or reset. Store it as Secret Management (Part B of the Migration Documentation) describes; in a shell estate that means **a file created under `umask 077`** or the operating system's keychain, **never in the script, never in a tracked `.env` file, and never exported to child processes**. A reset invalidates the previous secret immediately, so plan the cut-over before resetting it.

**Before deploying, confirm the calling identity.** Every call now runs as the OAuth2 Client's **Context User**, not as the user in the replaced credential. If that user has different workspace permissions or item-level security, the script returns fewer rows or `Unauthorized` in production with no change to its logic, and Relativity's audit attributes every call to that one account. Confirm the Context User's permissions match those of the previous identity.

**When the script acts for a signed-in user.** Client Credentials is one service identity by construction. A script that prompts a person for a password and sends it is the Authorization Code case, and moving to it means a new OAuth2 Client, because the flow is fixed when a client is created. Which of the two flows applies depends on who supplies the credential at run time.

Verification: **Verification note.** **A token has a finite lifetime.** **A request written to a plain TCP socket carries no TLS**: the credential crossed the network in clear text, so treat it as exposed and rotate it. **What is read is a literal Basic header written to a socket, not where the socket leads.**

OAuth2 Client: See [OAuth2 Client for Client Credentials](#oauth2-client-for-client-credentials).
<!-- generated:end SH-018 client-credentials -->

#### Authorization Code

<!-- generated:begin SH-018 authorization-code -->
**Flow.** This entry is for Authorization Code. The "Which flow" paragraph of SH-018 in [Part A](part-a-shell.md#sh-018) says when a match belongs to this flow.

Source form: **Applies to:** `bash` and `ksh93` (the `/dev/tcp` form); `bash`, `zsh`, `ksh93` and `mksh`, and `sh` read as `bash` (the `nc`, `socat` and `openssl s_client` forms).

```bash
exec 3<>/dev/tcp/host.example/80
printf 'GET /api/ HTTP/1.1\r\nHost: host.example\r\nAuthorization: Basic %s\r\n\r\n' "${ENCODED}" >&3
```

Under token authentication: **First, the destination:** if this Relativity instance is not RelativityOne — including a Relativity Server instance, whose own documentation governs — no migration to RelativityOne applies and the rest of this entry does not apply.

```sh
curl --silent --show-error --fail -H @"${HEADER_FILE}" "${HOST}/Relativity.REST/api/..."
```

Replace the hand-built request with a client that speaks HTTPS and send the token with `-H @"${HEADER_FILE}"`, as in the target form for a header that names the scheme.

Credentials: **Where the credentials come from.** The text establishes that a credential is sent as Basic, not that the destination is RelativityOne. If it is not RelativityOne, the migration is that service's own token mechanism, no migration to RelativityOne applies, and the reader stops here. If it is RelativityOne, the token comes from the token-acquisition part that follows, and the user name and password are replaced by the OAuth2 Client's client ID and secret.

**Common to both variants.** **Applies to:** `bash`, `zsh`, `ksh93` and `mksh` (and `sh`, read as `bash`); the blocks need `curl`, `openssl` and `jq`. The text has a common part and two variants of how the script receives the code: the **manual copy** (primary) and a **loopback listener** (optional).

**Sign in.** Replace the code that collected the user's credentials with a redirect: send the user's browser to the authorize endpoint `<host>/Relativity/Identity/connect/authorize`, which leads to the RelativityOne login page. The authorize endpoint is **verified by NSerio against a live instance; not documented by Relativity**. The request carries the standard authorization-code parameters — `client_id`, `response_type=code`, the registered `redirect_uri`, a `scope`, and a `state` value the script checks on return, plus `code_challenge` and `code_challenge_method=S256` when PKCE is used; these are standard OAuth, and Relativity documents none of them. No discovery document is relied on.

**PKCE.** PKCE is required by Relativity's own documentation to obtain a refresh token, and not otherwise: a Code request that does not ask for a refresh token works without it, **verified by NSerio against a live instance; not documented by Relativity**. It is never a blanket requirement. NSerio recommends PKCE with the `S256` method (NSerio recommendation), and does not claim that Relativity accepts any particular method, which it does not document. With `openssl`:

```sh
code_verifier=$(openssl rand -base64 48 | tr -d '\r\n' | tr '+/' '-_' | tr -d '=' | cut -c1-64)
code_challenge=$(printf '%s' "${code_verifier}" | openssl dgst -sha256 -binary | openssl base64 -A | tr '+/' '-_' | tr -d '=')
state=$(openssl rand -hex 16 | tr -d '\r')
```

The pipeline gives a 64-character verifier and a 43-character challenge (*checked by NSerio locally* with OpenSSL 3.5.4 on Windows and OpenSSL 3.0.2 on Linux; the `openssl` that ships with macOS has not been checked).

**Receive the code, by the manual copy (primary).** The browser returns to the Redirect URL registered on the OAuth2 Client with the authorization code and the `state` value. **The script prints the authorize address and the person opens it in any browser; when nothing listens at the registered loopback address the browser shows a connection error and leaves the full address, with `code` and `state`, in its address bar; the person copies that address and pastes it to the script.** This works headless, over SSH, on Linux and macOS, and needs no listener (NSerio recommendation; the browser's behaviour on an unreachable loopback address is *inferred* and has not been checked). The Redirect URL must still be exactly the one registered.

```sh
urlencode() (
	LC_ALL=C
	remaining=$1
	while [ -n "${remaining}" ]; do
		rest=${remaining#?}
		character=${remaining%"${rest}"}
		case ${character} in
			[A-Za-z0-9._~-]) printf '%s' "${character}" ;;
			*) printf '%%%02X' "'${character}" ;;
		esac
		remaining=${rest}
	done
)

authorize_url="${HOST}/Relativity/Identity/connect/authorize?client_id=$(urlencode "${CLIENT_ID}")&response_type=code&redirect_uri=$(urlencode "${REDIRECT_URI}")&scope=$(urlencode "${TOKEN_SCOPE}")&state=${state}&code_challenge=${code_challenge}&code_challenge_method=S256"
printf 'Open this address in a browser and sign in:\n%s\n' "${authorize_url}"
printf 'Then copy the whole address the browser ends on and paste it here: '
IFS= read -r returned_url
```

```sh
query=${returned_url#*\?}
query=${query%%#*}
returned_code=''
returned_state=''
remaining=${query}
while [ -n "${remaining}" ]; do
	pair=${remaining%%'&'*}
	case ${remaining} in
		*'&'*) remaining=${remaining#*'&'} ;;
		*) remaining='' ;;
	esac
	case ${pair} in
		code=*) returned_code=${pair#code=} ;;
		state=*) returned_state=${pair#state=} ;;
	esac
done
[ "${returned_state}" = "${state}" ] || { printf 'The state does not match; stop.\n' >&2; exit 1; }
```

The percent-encoding function and the address parsing give the same output in `bash`, `dash`, `zsh`, `ksh93` and `mksh` (*checked by NSerio locally*). **Check the `state` before using the code.** The code is already percent-encoded in the pasted address, so it is sent with `data` (not `data-urlencode`) below and not encoded again. The block passes the code and the verifier to `curl` as a configuration on standard input (`-K -`), so neither appears on the command line of `curl`; `printf` is a shell built-in and its arguments do not appear in `ps` either.

**Exchange the code.** Send a form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=authorization_code`, the `code`, the same `redirect_uri`, the `client_id` and the `client_secret`, plus the `code_verifier` when PKCE is used. The code is exchanged at that endpoint, which Relativity documents for the Client Credentials grant only; the exchange is **verified by NSerio against a live instance; not documented by Relativity**.

```sh
# The code and the verifier travel on standard input, never on a command line; printf is a built-in and does not appear in ps.
token_response=$(printf '%s\n' \
		"data = \"grant_type=authorization_code\"" \
		"data = \"code=${returned_code}\"" \
		"data-urlencode = \"redirect_uri=${REDIRECT_URI}\"" \
		"data-urlencode = \"client_id=${CLIENT_ID}\"" \
		"data-urlencode = \"code_verifier=${code_verifier}\"" \
		"data-urlencode = \"client_secret@${CLIENT_SECRET_FILE}\"" \
	| curl --silent --show-error --fail -K - \
		"${HOST}/Relativity/Identity/connect/token")
ACCESS_TOKEN=$(printf '%s' "${token_response}" | jq -r '.access_token')
```

**Use the token.** Send `Authorization: Bearer <access token>` — the change at the use site above — with `X-CSRF-Header: -` on REST and Kepler calls, from a header file as for Client Credentials: `( umask 077; printf 'Authorization: Bearer %s\nX-CSRF-Header: -\n' "${ACCESS_TOKEN}" > "${HEADER_FILE}" )`, then `curl -H @"${HEADER_FILE}"`.

**On expiry.** With Allow Refresh Tokens enabled on the OAuth2 Client and PKCE used, the script holds a refresh token and, when the access token expires, sends a `grant_type=refresh_token` request with it and the client credentials in place of the code exchange — *inferred* from the standard OAuth shape, not tested. Without a refresh token the person signs in again. Relativity does not document a refresh token's lifetime, whether it rotates, how it is revoked, whether a revocation endpoint exists, or what revoking it does to access tokens already issued. **A refresh token is a secret:** keep it in a file created under `umask 077` or in the keychain, never on a command line.

**Scope.** No scope set is prescribed for Code. The scope `openid RelativityRequestOrigin UserInfo UserInfoAccess AuthenticationID RelativityWeb` works, **verified by NSerio against a live instance; not documented by Relativity**, verified as working and not as minimal. `state` is sent as standard OAuth practice and `nonce` is not used; Relativity does not document whether it enforces either.

**The script stops collecting the password.** After this change the script never receives, stores or forwards the user's user name and password: the user signs in on RelativityOne's own page, and the script holds only tokens. Remove the `read` that prompts for the password, the `stty -echo` around it and any storage of the password the replaced code needed.

**Single sign-on.** Sign-in happens on RelativityOne's own login page, so the instance's single sign-on and two-factor rules apply unchanged — *inferred* from where the sign-in happens, and not documented for clients that customers create.

**The client secret is required.** Code does not work without a client secret: tested and rejected, **verified by NSerio against a live instance; not documented by Relativity**, which confirms Relativity's own statement that the grant requires one. **A script is plain text, so a secret shipped inside a distributed script can be read by anyone who holds the script**; it protects nothing against the person running it. Store it as Secret Management (Part B of the Migration Documentation) describes, in a file with the person's own permissions (`umask 077`) or in the keychain, and apply the compensating guidance below.

**Loopback listener variant (optional, per system).** A shell has no HTTP listener of its own. Where a script runs on a machine with a browser and a registered fixed-port loopback Redirect URL `http://127.0.0.1:<port>/<path>` — a plain-HTTP loopback URL is accepted, **verified by NSerio against a live instance; not documented by Relativity** — it can start `nc` or `socat` bound to `127.0.0.1` only, for one request, to read the request line `GET /<path>?code=…&state=… HTTP/1.1` and answer with a short page: on Linux the OpenBSD `nc` does it with `nc -l 127.0.0.1 PORT` (*checked by NSerio locally*); the options differ between OpenBSD, BSD and macOS, GNU and traditional `nc` and `ncat`, and `socat`, and those have not been checked. Bind to loopback only, check the `state` (another local process can connect to the port), and fall back to the manual copy when the tool is missing or the machine is headless.

**Compensating guidance** (NSerio recommendation, subject to your own security policy): set Trusted IPs where the users' outbound addresses are known and stable — they restrict where a token can be *requested* — use the recommended short Access Token Lifetime of 60 minutes, enable refresh tokens only when needed, and create one OAuth2 Client per distribution, so a leaked secret can be disabled without affecting the others. **A documentation gap.** Relativity documents no recommendation for a distributed script or desktop application; this design is what NSerio verified works.

Verification: **Verification note.** **A token has a finite lifetime.** **A request written to a plain TCP socket carries no TLS**: the credential crossed the network in clear text, so treat it as exposed and rotate it. **What is read is a literal Basic header written to a socket, not where the socket leads.**

OAuth2 Client: See [OAuth2 Client for Authorization Code](#oauth2-client-for-authorization-code).
<!-- generated:end SH-018 authorization-code -->

### SH-019 · CurlConfigurationWrittenByScript

<a id="sh-019-migration"></a>

#### Client Credentials

<!-- generated:begin SH-019 client-credentials -->
**Flow.** This entry is for Client Credentials. The "Which flow" paragraph of SH-019 in [Part A](part-a-shell.md#sh-019) says when a match belongs to this flow.

Source form: **Applies to:** `bash`, `zsh`, `ksh93` and `mksh` (and `sh`, read as `bash`); `curl`.

```sh
cat > "${CONFIG_FILE}" <<-EOF
	user = "${API_USER}:${API_PASSWORD}"
EOF
curl --silent --show-error --config "${CONFIG_FILE}" "${HOST}/Relativity.REST/api/..."
```

Under token authentication: **First, the destination:** if this Relativity instance is not RelativityOne — including a Relativity Server instance, whose own documentation governs — no migration to RelativityOne applies and the rest of this entry does not apply.

```sh
( umask 077; printf 'header = "Authorization: Bearer %s"\nheader = "X-CSRF-Header: -"\n' "${ACCESS_TOKEN}" > "${CONFIG_FILE}" )
curl --silent --show-error --fail --config "${CONFIG_FILE}" "${HOST}/Relativity.REST/api/..."
```

Remove the `user` line and the code that wrote it.

Credentials: **Where the credentials come from.** The text establishes that a credential is sent as Basic, not that the destination is RelativityOne. If it is not RelativityOne, the migration is that service's own token mechanism, no migration to RelativityOne applies, and the reader stops here. If it is RelativityOne, the token comes from the token-acquisition part that follows, and the user name and password are replaced by the OAuth2 Client's client ID and secret.

**Where the token comes from.** Create an OAuth2 Client with Flow Grant Type set to Client Credentials (part 3 lists its settings), then send a form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=client_credentials`, `client_id`, `client_secret` and a `scope`. The token endpoint is **documented** for this grant. The token is then sent as `Authorization: Bearer <token>`, with `X-CSRF-Header: -` on REST and Kepler calls — the header is documented as required, and a Kepler call without it returns 404. **Applies to:** `bash`, `zsh`, `ksh93` and `mksh` (and `sh`, read as `bash`); the blocks need `curl` and `jq`, which is a stated prerequisite: no fallback that parses the JSON with a regular expression is recommended.

```sh
# HOST, CLIENT_ID, TOKEN_SCOPE and CLIENT_SECRET_FILE come from the caller's environment; the secret file is created as Secret Management (Part B of the Migration Documentation) describes.
acquire_access_token() {
	curl --silent --show-error --fail \
		--data-urlencode "grant_type=client_credentials" \
		--data-urlencode "client_id=${CLIENT_ID}" \
		--data-urlencode "client_secret@${CLIENT_SECRET_FILE}" \
		--data-urlencode "scope=${TOKEN_SCOPE}" \
		"${HOST}/Relativity/Identity/connect/token" \
		| jq -r '.access_token'
}
ACCESS_TOKEN=$(acquire_access_token)
```

`--data-urlencode "client_secret@FILE"` reads the secret from a file and keeps it out of the command line (*checked by NSerio locally*: the file is read whole, a trailing line feed included, so create it with `printf '%s'`); `curl -K -` with the same setting on standard input also works (*checked by NSerio locally*). **The scope depends on what the integration needs, and no single scope is required.** Relativity's documentation shows two values for this grant: `SystemUserInfo` on the REST authentication page, the Usage Reports page and in the official Transfer SDK sample, and `UserInfoAccess` in the Kepler .NET samples. No page says which is correct, whether both work, or what either grants, and the OAuth2 Client's `Scopes` property is documented as one that cannot be set. Use the value the API you call documents, and confirm it.

**Use the token without putting it on the command line.**

```sh
( umask 077; printf 'Authorization: Bearer %s\nX-CSRF-Header: -\n' "${ACCESS_TOKEN}" > "${HEADER_FILE}" )
curl --silent --show-error --fail -H @"${HEADER_FILE}" "${HOST}/Relativity.REST/api/..."
```

`curl -H @FILE` sends the header lines of a file (*checked by NSerio locally* on `curl` 8.17.0 and 7.81.0; documented from `curl` 7.55). A token in `-H "Authorization: Bearer $TOKEN"` is visible to every user of the machine in `ps` while the command runs, so the file form is the recommended one for a shared host.

**Cache the token, and re-acquire it on expiry — do not refresh.** Keep the token for its lifetime (part 3 recommends 60 minutes, Relativity's own wording) and request a new one with the client secret shortly before it expires (a margin of 60 seconds is reasonable) and once when a call is rejected for authentication. Client Credentials offers no refresh tokens: **verified by NSerio against a live instance; not documented by Relativity** — the Allow Refresh Tokens control does not appear for this grant in the RelativityOne user interface. Relativity's own tables and text on refresh tokens conflict with each other, so re-acquisition with the secret is the advice that holds whichever way they are read. A script that runs for longer than the token's lifetime must acquire again, not hold the first token.

**The client secret.** It is displayed once, at creation or reset. Store it as Secret Management (Part B of the Migration Documentation) describes; in a shell estate that means **a file created under `umask 077`** or the operating system's keychain, **never in the script, never in a tracked `.env` file, and never exported to child processes**. A reset invalidates the previous secret immediately, so plan the cut-over before resetting it.

**Before deploying, confirm the calling identity.** Every call now runs as the OAuth2 Client's **Context User**, not as the user in the replaced credential. If that user has different workspace permissions or item-level security, the script returns fewer rows or `Unauthorized` in production with no change to its logic, and Relativity's audit attributes every call to that one account. Confirm the Context User's permissions match those of the previous identity.

**When the script acts for a signed-in user.** Client Credentials is one service identity by construction. A script that prompts a person for a password and sends it is the Authorization Code case, and moving to it means a new OAuth2 Client, because the flow is fixed when a client is created. Which of the two flows applies depends on who supplies the credential at run time.

Verification: **Verification note.** **A token has a finite lifetime.** **The configuration the script writes and the call that reads it are read, joined by the text of the path**: another script that reads the same file, or a copy of the file in a backup or a log, still holds the credential, which must be rotated and not only removed. `curl` reads the `header` line from the file as it reads `-H` (documented in the `curl` manual; the `user` line was *checked by NSerio locally*).

OAuth2 Client: See [OAuth2 Client for Client Credentials](#oauth2-client-for-client-credentials).
<!-- generated:end SH-019 client-credentials -->

#### Authorization Code

<!-- generated:begin SH-019 authorization-code -->
**Flow.** This entry is for Authorization Code. The "Which flow" paragraph of SH-019 in [Part A](part-a-shell.md#sh-019) says when a match belongs to this flow.

Source form: **Applies to:** `bash`, `zsh`, `ksh93` and `mksh` (and `sh`, read as `bash`); `curl`.

```sh
cat > "${CONFIG_FILE}" <<-EOF
	user = "${API_USER}:${API_PASSWORD}"
EOF
curl --silent --show-error --config "${CONFIG_FILE}" "${HOST}/Relativity.REST/api/..."
```

Under token authentication: **First, the destination:** if this Relativity instance is not RelativityOne — including a Relativity Server instance, whose own documentation governs — no migration to RelativityOne applies and the rest of this entry does not apply.

```sh
( umask 077; printf 'header = "Authorization: Bearer %s"\nheader = "X-CSRF-Header: -"\n' "${ACCESS_TOKEN}" > "${CONFIG_FILE}" )
curl --silent --show-error --fail --config "${CONFIG_FILE}" "${HOST}/Relativity.REST/api/..."
```

Remove the `user` line and the code that wrote it.

Credentials: **Where the credentials come from.** The text establishes that a credential is sent as Basic, not that the destination is RelativityOne. If it is not RelativityOne, the migration is that service's own token mechanism, no migration to RelativityOne applies, and the reader stops here. If it is RelativityOne, the token comes from the token-acquisition part that follows, and the user name and password are replaced by the OAuth2 Client's client ID and secret.

**Common to both variants.** **Applies to:** `bash`, `zsh`, `ksh93` and `mksh` (and `sh`, read as `bash`); the blocks need `curl`, `openssl` and `jq`. The text has a common part and two variants of how the script receives the code: the **manual copy** (primary) and a **loopback listener** (optional).

**Sign in.** Replace the code that collected the user's credentials with a redirect: send the user's browser to the authorize endpoint `<host>/Relativity/Identity/connect/authorize`, which leads to the RelativityOne login page. The authorize endpoint is **verified by NSerio against a live instance; not documented by Relativity**. The request carries the standard authorization-code parameters — `client_id`, `response_type=code`, the registered `redirect_uri`, a `scope`, and a `state` value the script checks on return, plus `code_challenge` and `code_challenge_method=S256` when PKCE is used; these are standard OAuth, and Relativity documents none of them. No discovery document is relied on.

**PKCE.** PKCE is required by Relativity's own documentation to obtain a refresh token, and not otherwise: a Code request that does not ask for a refresh token works without it, **verified by NSerio against a live instance; not documented by Relativity**. It is never a blanket requirement. NSerio recommends PKCE with the `S256` method (NSerio recommendation), and does not claim that Relativity accepts any particular method, which it does not document. With `openssl`:

```sh
code_verifier=$(openssl rand -base64 48 | tr -d '\r\n' | tr '+/' '-_' | tr -d '=' | cut -c1-64)
code_challenge=$(printf '%s' "${code_verifier}" | openssl dgst -sha256 -binary | openssl base64 -A | tr '+/' '-_' | tr -d '=')
state=$(openssl rand -hex 16 | tr -d '\r')
```

The pipeline gives a 64-character verifier and a 43-character challenge (*checked by NSerio locally* with OpenSSL 3.5.4 on Windows and OpenSSL 3.0.2 on Linux; the `openssl` that ships with macOS has not been checked).

**Receive the code, by the manual copy (primary).** The browser returns to the Redirect URL registered on the OAuth2 Client with the authorization code and the `state` value. **The script prints the authorize address and the person opens it in any browser; when nothing listens at the registered loopback address the browser shows a connection error and leaves the full address, with `code` and `state`, in its address bar; the person copies that address and pastes it to the script.** This works headless, over SSH, on Linux and macOS, and needs no listener (NSerio recommendation; the browser's behaviour on an unreachable loopback address is *inferred* and has not been checked). The Redirect URL must still be exactly the one registered.

```sh
urlencode() (
	LC_ALL=C
	remaining=$1
	while [ -n "${remaining}" ]; do
		rest=${remaining#?}
		character=${remaining%"${rest}"}
		case ${character} in
			[A-Za-z0-9._~-]) printf '%s' "${character}" ;;
			*) printf '%%%02X' "'${character}" ;;
		esac
		remaining=${rest}
	done
)

authorize_url="${HOST}/Relativity/Identity/connect/authorize?client_id=$(urlencode "${CLIENT_ID}")&response_type=code&redirect_uri=$(urlencode "${REDIRECT_URI}")&scope=$(urlencode "${TOKEN_SCOPE}")&state=${state}&code_challenge=${code_challenge}&code_challenge_method=S256"
printf 'Open this address in a browser and sign in:\n%s\n' "${authorize_url}"
printf 'Then copy the whole address the browser ends on and paste it here: '
IFS= read -r returned_url
```

```sh
query=${returned_url#*\?}
query=${query%%#*}
returned_code=''
returned_state=''
remaining=${query}
while [ -n "${remaining}" ]; do
	pair=${remaining%%'&'*}
	case ${remaining} in
		*'&'*) remaining=${remaining#*'&'} ;;
		*) remaining='' ;;
	esac
	case ${pair} in
		code=*) returned_code=${pair#code=} ;;
		state=*) returned_state=${pair#state=} ;;
	esac
done
[ "${returned_state}" = "${state}" ] || { printf 'The state does not match; stop.\n' >&2; exit 1; }
```

The percent-encoding function and the address parsing give the same output in `bash`, `dash`, `zsh`, `ksh93` and `mksh` (*checked by NSerio locally*). **Check the `state` before using the code.** The code is already percent-encoded in the pasted address, so it is sent with `data` (not `data-urlencode`) below and not encoded again. The block passes the code and the verifier to `curl` as a configuration on standard input (`-K -`), so neither appears on the command line of `curl`; `printf` is a shell built-in and its arguments do not appear in `ps` either.

**Exchange the code.** Send a form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=authorization_code`, the `code`, the same `redirect_uri`, the `client_id` and the `client_secret`, plus the `code_verifier` when PKCE is used. The code is exchanged at that endpoint, which Relativity documents for the Client Credentials grant only; the exchange is **verified by NSerio against a live instance; not documented by Relativity**.

```sh
# The code and the verifier travel on standard input, never on a command line; printf is a built-in and does not appear in ps.
token_response=$(printf '%s\n' \
		"data = \"grant_type=authorization_code\"" \
		"data = \"code=${returned_code}\"" \
		"data-urlencode = \"redirect_uri=${REDIRECT_URI}\"" \
		"data-urlencode = \"client_id=${CLIENT_ID}\"" \
		"data-urlencode = \"code_verifier=${code_verifier}\"" \
		"data-urlencode = \"client_secret@${CLIENT_SECRET_FILE}\"" \
	| curl --silent --show-error --fail -K - \
		"${HOST}/Relativity/Identity/connect/token")
ACCESS_TOKEN=$(printf '%s' "${token_response}" | jq -r '.access_token')
```

**Use the token.** Send `Authorization: Bearer <access token>` — the change at the use site above — with `X-CSRF-Header: -` on REST and Kepler calls, from a header file as for Client Credentials: `( umask 077; printf 'Authorization: Bearer %s\nX-CSRF-Header: -\n' "${ACCESS_TOKEN}" > "${HEADER_FILE}" )`, then `curl -H @"${HEADER_FILE}"`.

**On expiry.** With Allow Refresh Tokens enabled on the OAuth2 Client and PKCE used, the script holds a refresh token and, when the access token expires, sends a `grant_type=refresh_token` request with it and the client credentials in place of the code exchange — *inferred* from the standard OAuth shape, not tested. Without a refresh token the person signs in again. Relativity does not document a refresh token's lifetime, whether it rotates, how it is revoked, whether a revocation endpoint exists, or what revoking it does to access tokens already issued. **A refresh token is a secret:** keep it in a file created under `umask 077` or in the keychain, never on a command line.

**Scope.** No scope set is prescribed for Code. The scope `openid RelativityRequestOrigin UserInfo UserInfoAccess AuthenticationID RelativityWeb` works, **verified by NSerio against a live instance; not documented by Relativity**, verified as working and not as minimal. `state` is sent as standard OAuth practice and `nonce` is not used; Relativity does not document whether it enforces either.

**The script stops collecting the password.** After this change the script never receives, stores or forwards the user's user name and password: the user signs in on RelativityOne's own page, and the script holds only tokens. Remove the `read` that prompts for the password, the `stty -echo` around it and any storage of the password the replaced code needed.

**Single sign-on.** Sign-in happens on RelativityOne's own login page, so the instance's single sign-on and two-factor rules apply unchanged — *inferred* from where the sign-in happens, and not documented for clients that customers create.

**The client secret is required.** Code does not work without a client secret: tested and rejected, **verified by NSerio against a live instance; not documented by Relativity**, which confirms Relativity's own statement that the grant requires one. **A script is plain text, so a secret shipped inside a distributed script can be read by anyone who holds the script**; it protects nothing against the person running it. Store it as Secret Management (Part B of the Migration Documentation) describes, in a file with the person's own permissions (`umask 077`) or in the keychain, and apply the compensating guidance below.

**Loopback listener variant (optional, per system).** A shell has no HTTP listener of its own. Where a script runs on a machine with a browser and a registered fixed-port loopback Redirect URL `http://127.0.0.1:<port>/<path>` — a plain-HTTP loopback URL is accepted, **verified by NSerio against a live instance; not documented by Relativity** — it can start `nc` or `socat` bound to `127.0.0.1` only, for one request, to read the request line `GET /<path>?code=…&state=… HTTP/1.1` and answer with a short page: on Linux the OpenBSD `nc` does it with `nc -l 127.0.0.1 PORT` (*checked by NSerio locally*); the options differ between OpenBSD, BSD and macOS, GNU and traditional `nc` and `ncat`, and `socat`, and those have not been checked. Bind to loopback only, check the `state` (another local process can connect to the port), and fall back to the manual copy when the tool is missing or the machine is headless.

**Compensating guidance** (NSerio recommendation, subject to your own security policy): set Trusted IPs where the users' outbound addresses are known and stable — they restrict where a token can be *requested* — use the recommended short Access Token Lifetime of 60 minutes, enable refresh tokens only when needed, and create one OAuth2 Client per distribution, so a leaked secret can be disabled without affecting the others. **A documentation gap.** Relativity documents no recommendation for a distributed script or desktop application; this design is what NSerio verified works.

Verification: **Verification note.** **A token has a finite lifetime.** **The configuration the script writes and the call that reads it are read, joined by the text of the path**: another script that reads the same file, or a copy of the file in a backup or a log, still holds the credential, which must be rotated and not only removed. `curl` reads the `header` line from the file as it reads `-H` (documented in the `curl` manual; the `user` line was *checked by NSerio locally*).

OAuth2 Client: See [OAuth2 Client for Authorization Code](#oauth2-client-for-authorization-code).
<!-- generated:end SH-019 authorization-code -->

### SH-021 · CurlViaSameFileAliasOrVariable

<a id="sh-021-migration"></a>

#### Client Credentials

<!-- generated:begin SH-021 client-credentials -->
**Flow.** This entry is for Client Credentials. The "Which flow" paragraph of SH-021 in [Part A](part-a-shell.md#sh-021) says when a match belongs to this flow.

Source form: **Applies to:** `bash`, `zsh`, `ksh93` and `mksh` (and `sh`, read as `bash`); `curl` through a same-file command variable (the alias form needs `expand_aliases` on, or POSIX mode, in `bash`).

```sh
CURL=curl
${CURL} --silent --show-error -u "${API_USER}:${API_PASSWORD}" "${HOST}/Relativity.REST/api/..."
```

Under token authentication: **First, the destination:** if this Relativity instance is not RelativityOne — including a Relativity Server instance, whose own documentation governs — no migration to RelativityOne applies and the rest of this entry does not apply.

```sh
CURL=curl
${CURL} --silent --show-error --fail -H @"${HEADER_FILE}" "${HOST}/Relativity.REST/api/..."
```

Remove `-u`, `--user` and `--basic`.

Credentials: **Where the credentials come from.** The text establishes that a credential is sent as Basic, not that the destination is RelativityOne. If it is not RelativityOne, the migration is that service's own token mechanism, no migration to RelativityOne applies, and the reader stops here. If it is RelativityOne, the token comes from the token-acquisition part that follows, and the user name and password are replaced by the OAuth2 Client's client ID and secret.

**Where the token comes from.** Create an OAuth2 Client with Flow Grant Type set to Client Credentials (part 3 lists its settings), then send a form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=client_credentials`, `client_id`, `client_secret` and a `scope`. The token endpoint is **documented** for this grant. The token is then sent as `Authorization: Bearer <token>`, with `X-CSRF-Header: -` on REST and Kepler calls — the header is documented as required, and a Kepler call without it returns 404. **Applies to:** `bash`, `zsh`, `ksh93` and `mksh` (and `sh`, read as `bash`); the blocks need `curl` and `jq`, which is a stated prerequisite: no fallback that parses the JSON with a regular expression is recommended.

```sh
# HOST, CLIENT_ID, TOKEN_SCOPE and CLIENT_SECRET_FILE come from the caller's environment; the secret file is created as Secret Management (Part B of the Migration Documentation) describes.
acquire_access_token() {
	curl --silent --show-error --fail \
		--data-urlencode "grant_type=client_credentials" \
		--data-urlencode "client_id=${CLIENT_ID}" \
		--data-urlencode "client_secret@${CLIENT_SECRET_FILE}" \
		--data-urlencode "scope=${TOKEN_SCOPE}" \
		"${HOST}/Relativity/Identity/connect/token" \
		| jq -r '.access_token'
}
ACCESS_TOKEN=$(acquire_access_token)
```

`--data-urlencode "client_secret@FILE"` reads the secret from a file and keeps it out of the command line (*checked by NSerio locally*: the file is read whole, a trailing line feed included, so create it with `printf '%s'`); `curl -K -` with the same setting on standard input also works (*checked by NSerio locally*). **The scope depends on what the integration needs, and no single scope is required.** Relativity's documentation shows two values for this grant: `SystemUserInfo` on the REST authentication page, the Usage Reports page and in the official Transfer SDK sample, and `UserInfoAccess` in the Kepler .NET samples. No page says which is correct, whether both work, or what either grants, and the OAuth2 Client's `Scopes` property is documented as one that cannot be set. Use the value the API you call documents, and confirm it.

**Use the token without putting it on the command line.**

```sh
( umask 077; printf 'Authorization: Bearer %s\nX-CSRF-Header: -\n' "${ACCESS_TOKEN}" > "${HEADER_FILE}" )
curl --silent --show-error --fail -H @"${HEADER_FILE}" "${HOST}/Relativity.REST/api/..."
```

`curl -H @FILE` sends the header lines of a file (*checked by NSerio locally* on `curl` 8.17.0 and 7.81.0; documented from `curl` 7.55). A token in `-H "Authorization: Bearer $TOKEN"` is visible to every user of the machine in `ps` while the command runs, so the file form is the recommended one for a shared host.

**Cache the token, and re-acquire it on expiry — do not refresh.** Keep the token for its lifetime (part 3 recommends 60 minutes, Relativity's own wording) and request a new one with the client secret shortly before it expires (a margin of 60 seconds is reasonable) and once when a call is rejected for authentication. Client Credentials offers no refresh tokens: **verified by NSerio against a live instance; not documented by Relativity** — the Allow Refresh Tokens control does not appear for this grant in the RelativityOne user interface. Relativity's own tables and text on refresh tokens conflict with each other, so re-acquisition with the secret is the advice that holds whichever way they are read. A script that runs for longer than the token's lifetime must acquire again, not hold the first token.

**The client secret.** It is displayed once, at creation or reset. Store it as Secret Management (Part B of the Migration Documentation) describes; in a shell estate that means **a file created under `umask 077`** or the operating system's keychain, **never in the script, never in a tracked `.env` file, and never exported to child processes**. A reset invalidates the previous secret immediately, so plan the cut-over before resetting it.

**Before deploying, confirm the calling identity.** Every call now runs as the OAuth2 Client's **Context User**, not as the user in the replaced credential. If that user has different workspace permissions or item-level security, the script returns fewer rows or `Unauthorized` in production with no change to its logic, and Relativity's audit attributes every call to that one account. Confirm the Context User's permissions match those of the previous identity.

**When the script acts for a signed-in user.** Client Credentials is one service identity by construction. A script that prompts a person for a password and sends it is the Authorization Code case, and moving to it means a new OAuth2 Client, because the flow is fixed when a client is created. Which of the two flows applies depends on who supplies the credential at run time.

Verification: **Verification note.** **A token has a finite lifetime.** **A definition of the command within thirty lines is followed**: other calls through the same name that sit farther away, or in other files, are not read, so change every place that uses it.

OAuth2 Client: See [OAuth2 Client for Client Credentials](#oauth2-client-for-client-credentials).
<!-- generated:end SH-021 client-credentials -->

#### Authorization Code

<!-- generated:begin SH-021 authorization-code -->
**Flow.** This entry is for Authorization Code. The "Which flow" paragraph of SH-021 in [Part A](part-a-shell.md#sh-021) says when a match belongs to this flow.

Source form: **Applies to:** `bash`, `zsh`, `ksh93` and `mksh` (and `sh`, read as `bash`); `curl` through a same-file command variable (the alias form needs `expand_aliases` on, or POSIX mode, in `bash`).

```sh
CURL=curl
${CURL} --silent --show-error -u "${API_USER}:${API_PASSWORD}" "${HOST}/Relativity.REST/api/..."
```

Under token authentication: **First, the destination:** if this Relativity instance is not RelativityOne — including a Relativity Server instance, whose own documentation governs — no migration to RelativityOne applies and the rest of this entry does not apply.

```sh
CURL=curl
${CURL} --silent --show-error --fail -H @"${HEADER_FILE}" "${HOST}/Relativity.REST/api/..."
```

Remove `-u`, `--user` and `--basic`.

Credentials: **Where the credentials come from.** The text establishes that a credential is sent as Basic, not that the destination is RelativityOne. If it is not RelativityOne, the migration is that service's own token mechanism, no migration to RelativityOne applies, and the reader stops here. If it is RelativityOne, the token comes from the token-acquisition part that follows, and the user name and password are replaced by the OAuth2 Client's client ID and secret.

**Common to both variants.** **Applies to:** `bash`, `zsh`, `ksh93` and `mksh` (and `sh`, read as `bash`); the blocks need `curl`, `openssl` and `jq`. The text has a common part and two variants of how the script receives the code: the **manual copy** (primary) and a **loopback listener** (optional).

**Sign in.** Replace the code that collected the user's credentials with a redirect: send the user's browser to the authorize endpoint `<host>/Relativity/Identity/connect/authorize`, which leads to the RelativityOne login page. The authorize endpoint is **verified by NSerio against a live instance; not documented by Relativity**. The request carries the standard authorization-code parameters — `client_id`, `response_type=code`, the registered `redirect_uri`, a `scope`, and a `state` value the script checks on return, plus `code_challenge` and `code_challenge_method=S256` when PKCE is used; these are standard OAuth, and Relativity documents none of them. No discovery document is relied on.

**PKCE.** PKCE is required by Relativity's own documentation to obtain a refresh token, and not otherwise: a Code request that does not ask for a refresh token works without it, **verified by NSerio against a live instance; not documented by Relativity**. It is never a blanket requirement. NSerio recommends PKCE with the `S256` method (NSerio recommendation), and does not claim that Relativity accepts any particular method, which it does not document. With `openssl`:

```sh
code_verifier=$(openssl rand -base64 48 | tr -d '\r\n' | tr '+/' '-_' | tr -d '=' | cut -c1-64)
code_challenge=$(printf '%s' "${code_verifier}" | openssl dgst -sha256 -binary | openssl base64 -A | tr '+/' '-_' | tr -d '=')
state=$(openssl rand -hex 16 | tr -d '\r')
```

The pipeline gives a 64-character verifier and a 43-character challenge (*checked by NSerio locally* with OpenSSL 3.5.4 on Windows and OpenSSL 3.0.2 on Linux; the `openssl` that ships with macOS has not been checked).

**Receive the code, by the manual copy (primary).** The browser returns to the Redirect URL registered on the OAuth2 Client with the authorization code and the `state` value. **The script prints the authorize address and the person opens it in any browser; when nothing listens at the registered loopback address the browser shows a connection error and leaves the full address, with `code` and `state`, in its address bar; the person copies that address and pastes it to the script.** This works headless, over SSH, on Linux and macOS, and needs no listener (NSerio recommendation; the browser's behaviour on an unreachable loopback address is *inferred* and has not been checked). The Redirect URL must still be exactly the one registered.

```sh
urlencode() (
	LC_ALL=C
	remaining=$1
	while [ -n "${remaining}" ]; do
		rest=${remaining#?}
		character=${remaining%"${rest}"}
		case ${character} in
			[A-Za-z0-9._~-]) printf '%s' "${character}" ;;
			*) printf '%%%02X' "'${character}" ;;
		esac
		remaining=${rest}
	done
)

authorize_url="${HOST}/Relativity/Identity/connect/authorize?client_id=$(urlencode "${CLIENT_ID}")&response_type=code&redirect_uri=$(urlencode "${REDIRECT_URI}")&scope=$(urlencode "${TOKEN_SCOPE}")&state=${state}&code_challenge=${code_challenge}&code_challenge_method=S256"
printf 'Open this address in a browser and sign in:\n%s\n' "${authorize_url}"
printf 'Then copy the whole address the browser ends on and paste it here: '
IFS= read -r returned_url
```

```sh
query=${returned_url#*\?}
query=${query%%#*}
returned_code=''
returned_state=''
remaining=${query}
while [ -n "${remaining}" ]; do
	pair=${remaining%%'&'*}
	case ${remaining} in
		*'&'*) remaining=${remaining#*'&'} ;;
		*) remaining='' ;;
	esac
	case ${pair} in
		code=*) returned_code=${pair#code=} ;;
		state=*) returned_state=${pair#state=} ;;
	esac
done
[ "${returned_state}" = "${state}" ] || { printf 'The state does not match; stop.\n' >&2; exit 1; }
```

The percent-encoding function and the address parsing give the same output in `bash`, `dash`, `zsh`, `ksh93` and `mksh` (*checked by NSerio locally*). **Check the `state` before using the code.** The code is already percent-encoded in the pasted address, so it is sent with `data` (not `data-urlencode`) below and not encoded again. The block passes the code and the verifier to `curl` as a configuration on standard input (`-K -`), so neither appears on the command line of `curl`; `printf` is a shell built-in and its arguments do not appear in `ps` either.

**Exchange the code.** Send a form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=authorization_code`, the `code`, the same `redirect_uri`, the `client_id` and the `client_secret`, plus the `code_verifier` when PKCE is used. The code is exchanged at that endpoint, which Relativity documents for the Client Credentials grant only; the exchange is **verified by NSerio against a live instance; not documented by Relativity**.

```sh
# The code and the verifier travel on standard input, never on a command line; printf is a built-in and does not appear in ps.
token_response=$(printf '%s\n' \
		"data = \"grant_type=authorization_code\"" \
		"data = \"code=${returned_code}\"" \
		"data-urlencode = \"redirect_uri=${REDIRECT_URI}\"" \
		"data-urlencode = \"client_id=${CLIENT_ID}\"" \
		"data-urlencode = \"code_verifier=${code_verifier}\"" \
		"data-urlencode = \"client_secret@${CLIENT_SECRET_FILE}\"" \
	| curl --silent --show-error --fail -K - \
		"${HOST}/Relativity/Identity/connect/token")
ACCESS_TOKEN=$(printf '%s' "${token_response}" | jq -r '.access_token')
```

**Use the token.** Send `Authorization: Bearer <access token>` — the change at the use site above — with `X-CSRF-Header: -` on REST and Kepler calls, from a header file as for Client Credentials: `( umask 077; printf 'Authorization: Bearer %s\nX-CSRF-Header: -\n' "${ACCESS_TOKEN}" > "${HEADER_FILE}" )`, then `curl -H @"${HEADER_FILE}"`.

**On expiry.** With Allow Refresh Tokens enabled on the OAuth2 Client and PKCE used, the script holds a refresh token and, when the access token expires, sends a `grant_type=refresh_token` request with it and the client credentials in place of the code exchange — *inferred* from the standard OAuth shape, not tested. Without a refresh token the person signs in again. Relativity does not document a refresh token's lifetime, whether it rotates, how it is revoked, whether a revocation endpoint exists, or what revoking it does to access tokens already issued. **A refresh token is a secret:** keep it in a file created under `umask 077` or in the keychain, never on a command line.

**Scope.** No scope set is prescribed for Code. The scope `openid RelativityRequestOrigin UserInfo UserInfoAccess AuthenticationID RelativityWeb` works, **verified by NSerio against a live instance; not documented by Relativity**, verified as working and not as minimal. `state` is sent as standard OAuth practice and `nonce` is not used; Relativity does not document whether it enforces either.

**The script stops collecting the password.** After this change the script never receives, stores or forwards the user's user name and password: the user signs in on RelativityOne's own page, and the script holds only tokens. Remove the `read` that prompts for the password, the `stty -echo` around it and any storage of the password the replaced code needed.

**Single sign-on.** Sign-in happens on RelativityOne's own login page, so the instance's single sign-on and two-factor rules apply unchanged — *inferred* from where the sign-in happens, and not documented for clients that customers create.

**The client secret is required.** Code does not work without a client secret: tested and rejected, **verified by NSerio against a live instance; not documented by Relativity**, which confirms Relativity's own statement that the grant requires one. **A script is plain text, so a secret shipped inside a distributed script can be read by anyone who holds the script**; it protects nothing against the person running it. Store it as Secret Management (Part B of the Migration Documentation) describes, in a file with the person's own permissions (`umask 077`) or in the keychain, and apply the compensating guidance below.

**Loopback listener variant (optional, per system).** A shell has no HTTP listener of its own. Where a script runs on a machine with a browser and a registered fixed-port loopback Redirect URL `http://127.0.0.1:<port>/<path>` — a plain-HTTP loopback URL is accepted, **verified by NSerio against a live instance; not documented by Relativity** — it can start `nc` or `socat` bound to `127.0.0.1` only, for one request, to read the request line `GET /<path>?code=…&state=… HTTP/1.1` and answer with a short page: on Linux the OpenBSD `nc` does it with `nc -l 127.0.0.1 PORT` (*checked by NSerio locally*); the options differ between OpenBSD, BSD and macOS, GNU and traditional `nc` and `ncat`, and `socat`, and those have not been checked. Bind to loopback only, check the `state` (another local process can connect to the port), and fall back to the manual copy when the tool is missing or the machine is headless.

**Compensating guidance** (NSerio recommendation, subject to your own security policy): set Trusted IPs where the users' outbound addresses are known and stable — they restrict where a token can be *requested* — use the recommended short Access Token Lifetime of 60 minutes, enable refresh tokens only when needed, and create one OAuth2 Client per distribution, so a leaked secret can be disabled without affecting the others. **A documentation gap.** Relativity documents no recommendation for a distributed script or desktop application; this design is what NSerio verified works.

Verification: **Verification note.** **A token has a finite lifetime.** **A definition of the command within thirty lines is followed**: other calls through the same name that sit farther away, or in other files, are not read, so change every place that uses it.

OAuth2 Client: See [OAuth2 Client for Authorization Code](#oauth2-client-for-authorization-code).
<!-- generated:end SH-021 authorization-code -->

### SH-024 · EncodedPairHeldInVariable

<a id="sh-024-migration"></a>

#### Client Credentials

<!-- generated:begin SH-024 client-credentials -->
**Flow.** This entry is for Client Credentials. The "Which flow" paragraph of SH-024 in [Part A](part-a-shell.md#sh-024) says when a match belongs to this flow.

Source form: **Applies to:** `bash`, `zsh`, `ksh93` and `mksh` (and `sh`, read as `bash`); `base64` and `openssl`.

```sh
pair="${API_USER}:${API_PASSWORD}"
ENCODED=$(printf '%s' "${pair}" | base64 | tr -d '\n')
```

Under token authentication: **First, the destination:** if this Relativity instance is not RelativityOne — including a Relativity Server instance, whose own documentation governs — no migration to RelativityOne applies and the rest of this entry does not apply. **If the encoded pair is not a credential**, this match is a false positive and nothing changes.

Remove the pair, the encoding and the variable, and send the token from a header file: `-H @"${HEADER_FILE}"`, as in the target form for a header that names the scheme; a token written as `-H "Authorization: Bearer ..."` is visible in `ps` while the command runs.

Credentials: **Where the credentials come from.** The text establishes that a credential is sent as Basic, not that the destination is RelativityOne. If it is not RelativityOne, the migration is that service's own token mechanism, no migration to RelativityOne applies, and the reader stops here. If it is RelativityOne, the token comes from the token-acquisition part that follows, and the user name and password are replaced by the OAuth2 Client's client ID and secret.

**Where the token comes from.** Create an OAuth2 Client with Flow Grant Type set to Client Credentials (part 3 lists its settings), then send a form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=client_credentials`, `client_id`, `client_secret` and a `scope`. The token endpoint is **documented** for this grant. The token is then sent as `Authorization: Bearer <token>`, with `X-CSRF-Header: -` on REST and Kepler calls — the header is documented as required, and a Kepler call without it returns 404. **Applies to:** `bash`, `zsh`, `ksh93` and `mksh` (and `sh`, read as `bash`); the blocks need `curl` and `jq`, which is a stated prerequisite: no fallback that parses the JSON with a regular expression is recommended.

```sh
# HOST, CLIENT_ID, TOKEN_SCOPE and CLIENT_SECRET_FILE come from the caller's environment; the secret file is created as Secret Management (Part B of the Migration Documentation) describes.
acquire_access_token() {
	curl --silent --show-error --fail \
		--data-urlencode "grant_type=client_credentials" \
		--data-urlencode "client_id=${CLIENT_ID}" \
		--data-urlencode "client_secret@${CLIENT_SECRET_FILE}" \
		--data-urlencode "scope=${TOKEN_SCOPE}" \
		"${HOST}/Relativity/Identity/connect/token" \
		| jq -r '.access_token'
}
ACCESS_TOKEN=$(acquire_access_token)
```

`--data-urlencode "client_secret@FILE"` reads the secret from a file and keeps it out of the command line (*checked by NSerio locally*: the file is read whole, a trailing line feed included, so create it with `printf '%s'`); `curl -K -` with the same setting on standard input also works (*checked by NSerio locally*). **The scope depends on what the integration needs, and no single scope is required.** Relativity's documentation shows two values for this grant: `SystemUserInfo` on the REST authentication page, the Usage Reports page and in the official Transfer SDK sample, and `UserInfoAccess` in the Kepler .NET samples. No page says which is correct, whether both work, or what either grants, and the OAuth2 Client's `Scopes` property is documented as one that cannot be set. Use the value the API you call documents, and confirm it.

**Use the token without putting it on the command line.**

```sh
( umask 077; printf 'Authorization: Bearer %s\nX-CSRF-Header: -\n' "${ACCESS_TOKEN}" > "${HEADER_FILE}" )
curl --silent --show-error --fail -H @"${HEADER_FILE}" "${HOST}/Relativity.REST/api/..."
```

`curl -H @FILE` sends the header lines of a file (*checked by NSerio locally* on `curl` 8.17.0 and 7.81.0; documented from `curl` 7.55). A token in `-H "Authorization: Bearer $TOKEN"` is visible to every user of the machine in `ps` while the command runs, so the file form is the recommended one for a shared host.

**Cache the token, and re-acquire it on expiry — do not refresh.** Keep the token for its lifetime (part 3 recommends 60 minutes, Relativity's own wording) and request a new one with the client secret shortly before it expires (a margin of 60 seconds is reasonable) and once when a call is rejected for authentication. Client Credentials offers no refresh tokens: **verified by NSerio against a live instance; not documented by Relativity** — the Allow Refresh Tokens control does not appear for this grant in the RelativityOne user interface. Relativity's own tables and text on refresh tokens conflict with each other, so re-acquisition with the secret is the advice that holds whichever way they are read. A script that runs for longer than the token's lifetime must acquire again, not hold the first token.

**The client secret.** It is displayed once, at creation or reset. Store it as Secret Management (Part B of the Migration Documentation) describes; in a shell estate that means **a file created under `umask 077`** or the operating system's keychain, **never in the script, never in a tracked `.env` file, and never exported to child processes**. A reset invalidates the previous secret immediately, so plan the cut-over before resetting it.

**Before deploying, confirm the calling identity.** Every call now runs as the OAuth2 Client's **Context User**, not as the user in the replaced credential. If that user has different workspace permissions or item-level security, the script returns fewer rows or `Unauthorized` in production with no change to its logic, and Relativity's audit attributes every call to that one account. Confirm the Context User's permissions match those of the previous identity.

**When the script acts for a signed-in user.** Client Credentials is one service identity by construction. A script that prompts a person for a password and sends it is the Authorization Code case, and moving to it means a new OAuth2 Client, because the flow is fixed when a client is created. Which of the two flows applies depends on who supplies the credential at run time.

Verification: **Verification note.** **What is read is a colon pair held in a variable, encoded, with the encoded value used by a client command within thirty lines**: follow `ENCODED` to every place it is sent and change those uses too. **A token has a finite lifetime.**

OAuth2 Client: See [OAuth2 Client for Client Credentials](#oauth2-client-for-client-credentials).
<!-- generated:end SH-024 client-credentials -->

#### Authorization Code

<!-- generated:begin SH-024 authorization-code -->
**Flow.** This entry is for Authorization Code. The "Which flow" paragraph of SH-024 in [Part A](part-a-shell.md#sh-024) says when a match belongs to this flow.

Source form: **Applies to:** `bash`, `zsh`, `ksh93` and `mksh` (and `sh`, read as `bash`); `base64` and `openssl`.

```sh
pair="${API_USER}:${API_PASSWORD}"
ENCODED=$(printf '%s' "${pair}" | base64 | tr -d '\n')
```

Under token authentication: **First, the destination:** if this Relativity instance is not RelativityOne — including a Relativity Server instance, whose own documentation governs — no migration to RelativityOne applies and the rest of this entry does not apply. **If the encoded pair is not a credential**, this match is a false positive and nothing changes.

Remove the pair, the encoding and the variable, and send the token from a header file: `-H @"${HEADER_FILE}"`, as in the target form for a header that names the scheme; a token written as `-H "Authorization: Bearer ..."` is visible in `ps` while the command runs.

Credentials: **Where the credentials come from.** The text establishes that a credential is sent as Basic, not that the destination is RelativityOne. If it is not RelativityOne, the migration is that service's own token mechanism, no migration to RelativityOne applies, and the reader stops here. If it is RelativityOne, the token comes from the token-acquisition part that follows, and the user name and password are replaced by the OAuth2 Client's client ID and secret.

**Common to both variants.** **Applies to:** `bash`, `zsh`, `ksh93` and `mksh` (and `sh`, read as `bash`); the blocks need `curl`, `openssl` and `jq`. The text has a common part and two variants of how the script receives the code: the **manual copy** (primary) and a **loopback listener** (optional).

**Sign in.** Replace the code that collected the user's credentials with a redirect: send the user's browser to the authorize endpoint `<host>/Relativity/Identity/connect/authorize`, which leads to the RelativityOne login page. The authorize endpoint is **verified by NSerio against a live instance; not documented by Relativity**. The request carries the standard authorization-code parameters — `client_id`, `response_type=code`, the registered `redirect_uri`, a `scope`, and a `state` value the script checks on return, plus `code_challenge` and `code_challenge_method=S256` when PKCE is used; these are standard OAuth, and Relativity documents none of them. No discovery document is relied on.

**PKCE.** PKCE is required by Relativity's own documentation to obtain a refresh token, and not otherwise: a Code request that does not ask for a refresh token works without it, **verified by NSerio against a live instance; not documented by Relativity**. It is never a blanket requirement. NSerio recommends PKCE with the `S256` method (NSerio recommendation), and does not claim that Relativity accepts any particular method, which it does not document. With `openssl`:

```sh
code_verifier=$(openssl rand -base64 48 | tr -d '\r\n' | tr '+/' '-_' | tr -d '=' | cut -c1-64)
code_challenge=$(printf '%s' "${code_verifier}" | openssl dgst -sha256 -binary | openssl base64 -A | tr '+/' '-_' | tr -d '=')
state=$(openssl rand -hex 16 | tr -d '\r')
```

The pipeline gives a 64-character verifier and a 43-character challenge (*checked by NSerio locally* with OpenSSL 3.5.4 on Windows and OpenSSL 3.0.2 on Linux; the `openssl` that ships with macOS has not been checked).

**Receive the code, by the manual copy (primary).** The browser returns to the Redirect URL registered on the OAuth2 Client with the authorization code and the `state` value. **The script prints the authorize address and the person opens it in any browser; when nothing listens at the registered loopback address the browser shows a connection error and leaves the full address, with `code` and `state`, in its address bar; the person copies that address and pastes it to the script.** This works headless, over SSH, on Linux and macOS, and needs no listener (NSerio recommendation; the browser's behaviour on an unreachable loopback address is *inferred* and has not been checked). The Redirect URL must still be exactly the one registered.

```sh
urlencode() (
	LC_ALL=C
	remaining=$1
	while [ -n "${remaining}" ]; do
		rest=${remaining#?}
		character=${remaining%"${rest}"}
		case ${character} in
			[A-Za-z0-9._~-]) printf '%s' "${character}" ;;
			*) printf '%%%02X' "'${character}" ;;
		esac
		remaining=${rest}
	done
)

authorize_url="${HOST}/Relativity/Identity/connect/authorize?client_id=$(urlencode "${CLIENT_ID}")&response_type=code&redirect_uri=$(urlencode "${REDIRECT_URI}")&scope=$(urlencode "${TOKEN_SCOPE}")&state=${state}&code_challenge=${code_challenge}&code_challenge_method=S256"
printf 'Open this address in a browser and sign in:\n%s\n' "${authorize_url}"
printf 'Then copy the whole address the browser ends on and paste it here: '
IFS= read -r returned_url
```

```sh
query=${returned_url#*\?}
query=${query%%#*}
returned_code=''
returned_state=''
remaining=${query}
while [ -n "${remaining}" ]; do
	pair=${remaining%%'&'*}
	case ${remaining} in
		*'&'*) remaining=${remaining#*'&'} ;;
		*) remaining='' ;;
	esac
	case ${pair} in
		code=*) returned_code=${pair#code=} ;;
		state=*) returned_state=${pair#state=} ;;
	esac
done
[ "${returned_state}" = "${state}" ] || { printf 'The state does not match; stop.\n' >&2; exit 1; }
```

The percent-encoding function and the address parsing give the same output in `bash`, `dash`, `zsh`, `ksh93` and `mksh` (*checked by NSerio locally*). **Check the `state` before using the code.** The code is already percent-encoded in the pasted address, so it is sent with `data` (not `data-urlencode`) below and not encoded again. The block passes the code and the verifier to `curl` as a configuration on standard input (`-K -`), so neither appears on the command line of `curl`; `printf` is a shell built-in and its arguments do not appear in `ps` either.

**Exchange the code.** Send a form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=authorization_code`, the `code`, the same `redirect_uri`, the `client_id` and the `client_secret`, plus the `code_verifier` when PKCE is used. The code is exchanged at that endpoint, which Relativity documents for the Client Credentials grant only; the exchange is **verified by NSerio against a live instance; not documented by Relativity**.

```sh
# The code and the verifier travel on standard input, never on a command line; printf is a built-in and does not appear in ps.
token_response=$(printf '%s\n' \
		"data = \"grant_type=authorization_code\"" \
		"data = \"code=${returned_code}\"" \
		"data-urlencode = \"redirect_uri=${REDIRECT_URI}\"" \
		"data-urlencode = \"client_id=${CLIENT_ID}\"" \
		"data-urlencode = \"code_verifier=${code_verifier}\"" \
		"data-urlencode = \"client_secret@${CLIENT_SECRET_FILE}\"" \
	| curl --silent --show-error --fail -K - \
		"${HOST}/Relativity/Identity/connect/token")
ACCESS_TOKEN=$(printf '%s' "${token_response}" | jq -r '.access_token')
```

**Use the token.** Send `Authorization: Bearer <access token>` — the change at the use site above — with `X-CSRF-Header: -` on REST and Kepler calls, from a header file as for Client Credentials: `( umask 077; printf 'Authorization: Bearer %s\nX-CSRF-Header: -\n' "${ACCESS_TOKEN}" > "${HEADER_FILE}" )`, then `curl -H @"${HEADER_FILE}"`.

**On expiry.** With Allow Refresh Tokens enabled on the OAuth2 Client and PKCE used, the script holds a refresh token and, when the access token expires, sends a `grant_type=refresh_token` request with it and the client credentials in place of the code exchange — *inferred* from the standard OAuth shape, not tested. Without a refresh token the person signs in again. Relativity does not document a refresh token's lifetime, whether it rotates, how it is revoked, whether a revocation endpoint exists, or what revoking it does to access tokens already issued. **A refresh token is a secret:** keep it in a file created under `umask 077` or in the keychain, never on a command line.

**Scope.** No scope set is prescribed for Code. The scope `openid RelativityRequestOrigin UserInfo UserInfoAccess AuthenticationID RelativityWeb` works, **verified by NSerio against a live instance; not documented by Relativity**, verified as working and not as minimal. `state` is sent as standard OAuth practice and `nonce` is not used; Relativity does not document whether it enforces either.

**The script stops collecting the password.** After this change the script never receives, stores or forwards the user's user name and password: the user signs in on RelativityOne's own page, and the script holds only tokens. Remove the `read` that prompts for the password, the `stty -echo` around it and any storage of the password the replaced code needed.

**Single sign-on.** Sign-in happens on RelativityOne's own login page, so the instance's single sign-on and two-factor rules apply unchanged — *inferred* from where the sign-in happens, and not documented for clients that customers create.

**The client secret is required.** Code does not work without a client secret: tested and rejected, **verified by NSerio against a live instance; not documented by Relativity**, which confirms Relativity's own statement that the grant requires one. **A script is plain text, so a secret shipped inside a distributed script can be read by anyone who holds the script**; it protects nothing against the person running it. Store it as Secret Management (Part B of the Migration Documentation) describes, in a file with the person's own permissions (`umask 077`) or in the keychain, and apply the compensating guidance below.

**Loopback listener variant (optional, per system).** A shell has no HTTP listener of its own. Where a script runs on a machine with a browser and a registered fixed-port loopback Redirect URL `http://127.0.0.1:<port>/<path>` — a plain-HTTP loopback URL is accepted, **verified by NSerio against a live instance; not documented by Relativity** — it can start `nc` or `socat` bound to `127.0.0.1` only, for one request, to read the request line `GET /<path>?code=…&state=… HTTP/1.1` and answer with a short page: on Linux the OpenBSD `nc` does it with `nc -l 127.0.0.1 PORT` (*checked by NSerio locally*); the options differ between OpenBSD, BSD and macOS, GNU and traditional `nc` and `ncat`, and `socat`, and those have not been checked. Bind to loopback only, check the `state` (another local process can connect to the port), and fall back to the manual copy when the tool is missing or the machine is headless.

**Compensating guidance** (NSerio recommendation, subject to your own security policy): set Trusted IPs where the users' outbound addresses are known and stable — they restrict where a token can be *requested* — use the recommended short Access Token Lifetime of 60 minutes, enable refresh tokens only when needed, and create one OAuth2 Client per distribution, so a leaked secret can be disabled without affecting the others. **A documentation gap.** Relativity documents no recommendation for a distributed script or desktop application; this design is what NSerio verified works.

Verification: **Verification note.** **What is read is a colon pair held in a variable, encoded, with the encoded value used by a client command within thirty lines**: follow `ENCODED` to every place it is sent and change those uses too. **A token has a finite lifetime.**

OAuth2 Client: See [OAuth2 Client for Authorization Code](#oauth2-client-for-authorization-code).
<!-- generated:end SH-024 authorization-code -->

### SH-025 · OptionsHeldInStringVariable

<a id="sh-025-migration"></a>

#### Client Credentials

<!-- generated:begin SH-025 client-credentials -->
**Flow.** This entry is for Client Credentials. The "Which flow" paragraph of SH-025 in [Part A](part-a-shell.md#sh-025) says when a match belongs to this flow.

Source form: **Applies to:** `bash`, `zsh`, `ksh93` and `mksh` (and `sh`, read as `bash`); `curl`, `wget`, HTTPie, `aria2c`.

```sh
CURL_OPTIONS="-sS -u ${API_USER}:${API_PASSWORD}"
curl ${CURL_OPTIONS} "${HOST}/Relativity.REST/api/..."
```

Under token authentication: **First, the destination:** if this Relativity instance is not RelativityOne — including a Relativity Server instance, whose own documentation governs — no migration to RelativityOne applies and the rest of this entry does not apply.

```sh
CURL_OPTIONS="-sS --fail"
curl ${CURL_OPTIONS} -H @"${HEADER_FILE}" "${HOST}/Relativity.REST/api/..."
```

Remove the credential option from the string.

Credentials: **Where the credentials come from.** The text establishes that a credential is sent as Basic, not that the destination is RelativityOne. If it is not RelativityOne, the migration is that service's own token mechanism, no migration to RelativityOne applies, and the reader stops here. If it is RelativityOne, the token comes from the token-acquisition part that follows, and the user name and password are replaced by the OAuth2 Client's client ID and secret.

**Where the token comes from.** Create an OAuth2 Client with Flow Grant Type set to Client Credentials (part 3 lists its settings), then send a form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=client_credentials`, `client_id`, `client_secret` and a `scope`. The token endpoint is **documented** for this grant. The token is then sent as `Authorization: Bearer <token>`, with `X-CSRF-Header: -` on REST and Kepler calls — the header is documented as required, and a Kepler call without it returns 404. **Applies to:** `bash`, `zsh`, `ksh93` and `mksh` (and `sh`, read as `bash`); the blocks need `curl` and `jq`, which is a stated prerequisite: no fallback that parses the JSON with a regular expression is recommended.

```sh
# HOST, CLIENT_ID, TOKEN_SCOPE and CLIENT_SECRET_FILE come from the caller's environment; the secret file is created as Secret Management (Part B of the Migration Documentation) describes.
acquire_access_token() {
	curl --silent --show-error --fail \
		--data-urlencode "grant_type=client_credentials" \
		--data-urlencode "client_id=${CLIENT_ID}" \
		--data-urlencode "client_secret@${CLIENT_SECRET_FILE}" \
		--data-urlencode "scope=${TOKEN_SCOPE}" \
		"${HOST}/Relativity/Identity/connect/token" \
		| jq -r '.access_token'
}
ACCESS_TOKEN=$(acquire_access_token)
```

`--data-urlencode "client_secret@FILE"` reads the secret from a file and keeps it out of the command line (*checked by NSerio locally*: the file is read whole, a trailing line feed included, so create it with `printf '%s'`); `curl -K -` with the same setting on standard input also works (*checked by NSerio locally*). **The scope depends on what the integration needs, and no single scope is required.** Relativity's documentation shows two values for this grant: `SystemUserInfo` on the REST authentication page, the Usage Reports page and in the official Transfer SDK sample, and `UserInfoAccess` in the Kepler .NET samples. No page says which is correct, whether both work, or what either grants, and the OAuth2 Client's `Scopes` property is documented as one that cannot be set. Use the value the API you call documents, and confirm it.

**Use the token without putting it on the command line.**

```sh
( umask 077; printf 'Authorization: Bearer %s\nX-CSRF-Header: -\n' "${ACCESS_TOKEN}" > "${HEADER_FILE}" )
curl --silent --show-error --fail -H @"${HEADER_FILE}" "${HOST}/Relativity.REST/api/..."
```

`curl -H @FILE` sends the header lines of a file (*checked by NSerio locally* on `curl` 8.17.0 and 7.81.0; documented from `curl` 7.55). A token in `-H "Authorization: Bearer $TOKEN"` is visible to every user of the machine in `ps` while the command runs, so the file form is the recommended one for a shared host.

**Cache the token, and re-acquire it on expiry — do not refresh.** Keep the token for its lifetime (part 3 recommends 60 minutes, Relativity's own wording) and request a new one with the client secret shortly before it expires (a margin of 60 seconds is reasonable) and once when a call is rejected for authentication. Client Credentials offers no refresh tokens: **verified by NSerio against a live instance; not documented by Relativity** — the Allow Refresh Tokens control does not appear for this grant in the RelativityOne user interface. Relativity's own tables and text on refresh tokens conflict with each other, so re-acquisition with the secret is the advice that holds whichever way they are read. A script that runs for longer than the token's lifetime must acquire again, not hold the first token.

**The client secret.** It is displayed once, at creation or reset. Store it as Secret Management (Part B of the Migration Documentation) describes; in a shell estate that means **a file created under `umask 077`** or the operating system's keychain, **never in the script, never in a tracked `.env` file, and never exported to child processes**. A reset invalidates the previous secret immediately, so plan the cut-over before resetting it.

**Before deploying, confirm the calling identity.** Every call now runs as the OAuth2 Client's **Context User**, not as the user in the replaced credential. If that user has different workspace permissions or item-level security, the script returns fewer rows or `Unauthorized` in production with no change to its logic, and Relativity's audit attributes every call to that one account. Confirm the Context User's permissions match those of the previous identity.

**When the script acts for a signed-in user.** Client Credentials is one service identity by construction. A script that prompts a person for a password and sends it is the Authorization Code case, and moving to it means a new OAuth2 Client, because the flow is fixed when a client is created. Which of the two flows applies depends on who supplies the credential at run time.

Verification: **Verification note.** **The string and an unquoted use within thirty lines are read**; an option string used elsewhere is not read, so look for the other places it is used. A string that holds a credential is split into words by the shell, so it also appears in `ps` while the command runs. **A token has a finite lifetime.**

OAuth2 Client: See [OAuth2 Client for Client Credentials](#oauth2-client-for-client-credentials).
<!-- generated:end SH-025 client-credentials -->

#### Authorization Code

<!-- generated:begin SH-025 authorization-code -->
**Flow.** This entry is for Authorization Code. The "Which flow" paragraph of SH-025 in [Part A](part-a-shell.md#sh-025) says when a match belongs to this flow.

Source form: **Applies to:** `bash`, `zsh`, `ksh93` and `mksh` (and `sh`, read as `bash`); `curl`, `wget`, HTTPie, `aria2c`.

```sh
CURL_OPTIONS="-sS -u ${API_USER}:${API_PASSWORD}"
curl ${CURL_OPTIONS} "${HOST}/Relativity.REST/api/..."
```

Under token authentication: **First, the destination:** if this Relativity instance is not RelativityOne — including a Relativity Server instance, whose own documentation governs — no migration to RelativityOne applies and the rest of this entry does not apply.

```sh
CURL_OPTIONS="-sS --fail"
curl ${CURL_OPTIONS} -H @"${HEADER_FILE}" "${HOST}/Relativity.REST/api/..."
```

Remove the credential option from the string.

Credentials: **Where the credentials come from.** The text establishes that a credential is sent as Basic, not that the destination is RelativityOne. If it is not RelativityOne, the migration is that service's own token mechanism, no migration to RelativityOne applies, and the reader stops here. If it is RelativityOne, the token comes from the token-acquisition part that follows, and the user name and password are replaced by the OAuth2 Client's client ID and secret.

**Common to both variants.** **Applies to:** `bash`, `zsh`, `ksh93` and `mksh` (and `sh`, read as `bash`); the blocks need `curl`, `openssl` and `jq`. The text has a common part and two variants of how the script receives the code: the **manual copy** (primary) and a **loopback listener** (optional).

**Sign in.** Replace the code that collected the user's credentials with a redirect: send the user's browser to the authorize endpoint `<host>/Relativity/Identity/connect/authorize`, which leads to the RelativityOne login page. The authorize endpoint is **verified by NSerio against a live instance; not documented by Relativity**. The request carries the standard authorization-code parameters — `client_id`, `response_type=code`, the registered `redirect_uri`, a `scope`, and a `state` value the script checks on return, plus `code_challenge` and `code_challenge_method=S256` when PKCE is used; these are standard OAuth, and Relativity documents none of them. No discovery document is relied on.

**PKCE.** PKCE is required by Relativity's own documentation to obtain a refresh token, and not otherwise: a Code request that does not ask for a refresh token works without it, **verified by NSerio against a live instance; not documented by Relativity**. It is never a blanket requirement. NSerio recommends PKCE with the `S256` method (NSerio recommendation), and does not claim that Relativity accepts any particular method, which it does not document. With `openssl`:

```sh
code_verifier=$(openssl rand -base64 48 | tr -d '\r\n' | tr '+/' '-_' | tr -d '=' | cut -c1-64)
code_challenge=$(printf '%s' "${code_verifier}" | openssl dgst -sha256 -binary | openssl base64 -A | tr '+/' '-_' | tr -d '=')
state=$(openssl rand -hex 16 | tr -d '\r')
```

The pipeline gives a 64-character verifier and a 43-character challenge (*checked by NSerio locally* with OpenSSL 3.5.4 on Windows and OpenSSL 3.0.2 on Linux; the `openssl` that ships with macOS has not been checked).

**Receive the code, by the manual copy (primary).** The browser returns to the Redirect URL registered on the OAuth2 Client with the authorization code and the `state` value. **The script prints the authorize address and the person opens it in any browser; when nothing listens at the registered loopback address the browser shows a connection error and leaves the full address, with `code` and `state`, in its address bar; the person copies that address and pastes it to the script.** This works headless, over SSH, on Linux and macOS, and needs no listener (NSerio recommendation; the browser's behaviour on an unreachable loopback address is *inferred* and has not been checked). The Redirect URL must still be exactly the one registered.

```sh
urlencode() (
	LC_ALL=C
	remaining=$1
	while [ -n "${remaining}" ]; do
		rest=${remaining#?}
		character=${remaining%"${rest}"}
		case ${character} in
			[A-Za-z0-9._~-]) printf '%s' "${character}" ;;
			*) printf '%%%02X' "'${character}" ;;
		esac
		remaining=${rest}
	done
)

authorize_url="${HOST}/Relativity/Identity/connect/authorize?client_id=$(urlencode "${CLIENT_ID}")&response_type=code&redirect_uri=$(urlencode "${REDIRECT_URI}")&scope=$(urlencode "${TOKEN_SCOPE}")&state=${state}&code_challenge=${code_challenge}&code_challenge_method=S256"
printf 'Open this address in a browser and sign in:\n%s\n' "${authorize_url}"
printf 'Then copy the whole address the browser ends on and paste it here: '
IFS= read -r returned_url
```

```sh
query=${returned_url#*\?}
query=${query%%#*}
returned_code=''
returned_state=''
remaining=${query}
while [ -n "${remaining}" ]; do
	pair=${remaining%%'&'*}
	case ${remaining} in
		*'&'*) remaining=${remaining#*'&'} ;;
		*) remaining='' ;;
	esac
	case ${pair} in
		code=*) returned_code=${pair#code=} ;;
		state=*) returned_state=${pair#state=} ;;
	esac
done
[ "${returned_state}" = "${state}" ] || { printf 'The state does not match; stop.\n' >&2; exit 1; }
```

The percent-encoding function and the address parsing give the same output in `bash`, `dash`, `zsh`, `ksh93` and `mksh` (*checked by NSerio locally*). **Check the `state` before using the code.** The code is already percent-encoded in the pasted address, so it is sent with `data` (not `data-urlencode`) below and not encoded again. The block passes the code and the verifier to `curl` as a configuration on standard input (`-K -`), so neither appears on the command line of `curl`; `printf` is a shell built-in and its arguments do not appear in `ps` either.

**Exchange the code.** Send a form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=authorization_code`, the `code`, the same `redirect_uri`, the `client_id` and the `client_secret`, plus the `code_verifier` when PKCE is used. The code is exchanged at that endpoint, which Relativity documents for the Client Credentials grant only; the exchange is **verified by NSerio against a live instance; not documented by Relativity**.

```sh
# The code and the verifier travel on standard input, never on a command line; printf is a built-in and does not appear in ps.
token_response=$(printf '%s\n' \
		"data = \"grant_type=authorization_code\"" \
		"data = \"code=${returned_code}\"" \
		"data-urlencode = \"redirect_uri=${REDIRECT_URI}\"" \
		"data-urlencode = \"client_id=${CLIENT_ID}\"" \
		"data-urlencode = \"code_verifier=${code_verifier}\"" \
		"data-urlencode = \"client_secret@${CLIENT_SECRET_FILE}\"" \
	| curl --silent --show-error --fail -K - \
		"${HOST}/Relativity/Identity/connect/token")
ACCESS_TOKEN=$(printf '%s' "${token_response}" | jq -r '.access_token')
```

**Use the token.** Send `Authorization: Bearer <access token>` — the change at the use site above — with `X-CSRF-Header: -` on REST and Kepler calls, from a header file as for Client Credentials: `( umask 077; printf 'Authorization: Bearer %s\nX-CSRF-Header: -\n' "${ACCESS_TOKEN}" > "${HEADER_FILE}" )`, then `curl -H @"${HEADER_FILE}"`.

**On expiry.** With Allow Refresh Tokens enabled on the OAuth2 Client and PKCE used, the script holds a refresh token and, when the access token expires, sends a `grant_type=refresh_token` request with it and the client credentials in place of the code exchange — *inferred* from the standard OAuth shape, not tested. Without a refresh token the person signs in again. Relativity does not document a refresh token's lifetime, whether it rotates, how it is revoked, whether a revocation endpoint exists, or what revoking it does to access tokens already issued. **A refresh token is a secret:** keep it in a file created under `umask 077` or in the keychain, never on a command line.

**Scope.** No scope set is prescribed for Code. The scope `openid RelativityRequestOrigin UserInfo UserInfoAccess AuthenticationID RelativityWeb` works, **verified by NSerio against a live instance; not documented by Relativity**, verified as working and not as minimal. `state` is sent as standard OAuth practice and `nonce` is not used; Relativity does not document whether it enforces either.

**The script stops collecting the password.** After this change the script never receives, stores or forwards the user's user name and password: the user signs in on RelativityOne's own page, and the script holds only tokens. Remove the `read` that prompts for the password, the `stty -echo` around it and any storage of the password the replaced code needed.

**Single sign-on.** Sign-in happens on RelativityOne's own login page, so the instance's single sign-on and two-factor rules apply unchanged — *inferred* from where the sign-in happens, and not documented for clients that customers create.

**The client secret is required.** Code does not work without a client secret: tested and rejected, **verified by NSerio against a live instance; not documented by Relativity**, which confirms Relativity's own statement that the grant requires one. **A script is plain text, so a secret shipped inside a distributed script can be read by anyone who holds the script**; it protects nothing against the person running it. Store it as Secret Management (Part B of the Migration Documentation) describes, in a file with the person's own permissions (`umask 077`) or in the keychain, and apply the compensating guidance below.

**Loopback listener variant (optional, per system).** A shell has no HTTP listener of its own. Where a script runs on a machine with a browser and a registered fixed-port loopback Redirect URL `http://127.0.0.1:<port>/<path>` — a plain-HTTP loopback URL is accepted, **verified by NSerio against a live instance; not documented by Relativity** — it can start `nc` or `socat` bound to `127.0.0.1` only, for one request, to read the request line `GET /<path>?code=…&state=… HTTP/1.1` and answer with a short page: on Linux the OpenBSD `nc` does it with `nc -l 127.0.0.1 PORT` (*checked by NSerio locally*); the options differ between OpenBSD, BSD and macOS, GNU and traditional `nc` and `ncat`, and `socat`, and those have not been checked. Bind to loopback only, check the `state` (another local process can connect to the port), and fall back to the manual copy when the tool is missing or the machine is headless.

**Compensating guidance** (NSerio recommendation, subject to your own security policy): set Trusted IPs where the users' outbound addresses are known and stable — they restrict where a token can be *requested* — use the recommended short Access Token Lifetime of 60 minutes, enable refresh tokens only when needed, and create one OAuth2 Client per distribution, so a leaked secret can be disabled without affecting the others. **A documentation gap.** Relativity documents no recommendation for a distributed script or desktop application; this design is what NSerio verified works.

Verification: **Verification note.** **The string and an unquoted use within thirty lines are read**; an option string used elsewhere is not read, so look for the other places it is used. A string that holds a credential is split into words by the shell, so it also appears in `ps` while the command runs. **A token has a finite lifetime.**

OAuth2 Client: See [OAuth2 Client for Authorization Code](#oauth2-client-for-authorization-code).
<!-- generated:end SH-025 authorization-code -->

## Generic suggestion

The generic recommendation applies when a match has High or Medium confidence but no pattern-specific entry fits it. It is shorter than a class entry: it does not show a before-and-after form, because the construct was not characterized. Use it as a starting point, and read the Part A description of the pattern class if you can identify the construct.

### Client Credentials

<!-- generated:begin generic client-credentials -->
**Flow.** This entry is for Client Credentials.

Basic Authentication is found at this location but the construct is not one that this text describes, so the change has not been characterized and no before-and-after form is offered here. If the destination is not RelativityOne, Relativity's identity endpoint does not issue tokens for it: the migration is that service's own, and no migration to RelativityOne applies. If the destination is RelativityOne, Basic Authentication is no longer documented for its REST API. The documented destination in a shell script is a bearer `Authorization` header sent by an HTTP client such as `curl`, carrying a token obtained from the platform's identity endpoint with the client-credentials grant. The client ID and secret that obtain that token belong in a file created under `umask 077` or in the operating system's keychain, per Secret Management (Part B of the Migration Documentation), and never in the script, in a tracked `.env` file, in `.netrc` or `.wgetrc`, or on a command line. Beyond those facts this is a pointer and not an instruction: the migration must be designed from the code by whoever owns it, rather than applied from this text.

OAuth2 Client: See [OAuth2 Client for Client Credentials](#oauth2-client-for-client-credentials).
<!-- generated:end generic client-credentials -->

### Authorization Code

<!-- generated:begin generic authorization-code -->
**Flow.** This entry is for Authorization Code.

Basic Authentication is found at this location but the construct is not one that this text describes, so the change has not been characterized and no before-and-after form is offered here. If the destination is not RelativityOne, Relativity's identity endpoint does not issue tokens for it: the migration is that service's own, and no migration to RelativityOne applies. If the destination is RelativityOne, Basic Authentication is no longer documented for its REST API, and this match is assigned the Authorization Code flow, in which the script sends the person to RelativityOne to sign in, receives an authorization code at a Redirect URL registered on its OAuth2 Client, and exchanges the code for an access token that it sends as a Bearer token. A shell has no listener of its own, so the simplest way to receive the code is for the person to copy the address the browser ends on after signing in and paste it to the script. The authorize endpoint is `<host>/Relativity/Identity/connect/authorize` and the code is exchanged at `<host>/Relativity/Identity/connect/token`; both are verified by NSerio against a live instance; not documented by Relativity. PKCE is required by Relativity's own documentation to obtain a refresh token, and not otherwise. The client ID and secret belong in a file created under `umask 077` or in the keychain, per Secret Management (Part B of the Migration Documentation), and never in the script or on a command line; Code requires a client secret. After the change the script no longer collects or forwards the person's password. Beyond those facts this is a pointer and not an instruction: the migration must be designed from the code by whoever owns it, rather than applied from this text.

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
