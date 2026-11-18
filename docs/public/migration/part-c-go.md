# Migration Suggestions for Go (Part C of the Migration Documentation)

Part C tells you what to do when a Part A pattern is found in your Go code. It gives the same recommendation the tool gives for that finding, because the recommendations below are produced by the same recommendation catalog the tool reports from, and are not written separately.

This document has three kinds of text:

- **Hand-written guidance.** How to confirm which OAuth 2.0 flow applies, which variant of a recommendation fits your application, and how to judge a match the tool marked Low confidence.
- **Recommendations.** One entry for each pattern class and each flow, and one generic recommendation for each flow. Each sits between a pair of `generated` markers, which are invisible when the document is rendered.
- **Pointers.** Part A ([`part-a-go.md`](part-a-go.md)) describes each pattern and how to search for it. Secret Management (Part B of the Migration Documentation, [`part-b-secret-management.md`](part-b-secret-management.md)) says where the new secrets go.

Every example uses placeholders such as `HOST`, `userName`, `secret` and `accessToken`. Replace them with your own values, and keep real secrets out of source code and out of a tracked configuration file.

The two flows are the only ones the tool recommends:

- **Client Credentials** is the default. The code sends its client ID and client secret to the token endpoint and receives an access token. No user is involved, and every call runs as the OAuth2 Client's Context User.
- **Authorization Code** is the flow that the RelativityOne OAuth2 Client form names Code. The code sends a signed-in user to RelativityOne, receives an authorization code at a registered Redirect URL, and exchanges it for a token that carries that user's permissions.

These entries assume that Go reaches RelativityOne through its REST endpoints with the standard library's HTTP client: no Go client or package for RelativityOne was found. The text uses the standard library only (`net/http`, `net/url` and the packages it needs, with go1.13 or later) and names no third-party package in code. Where the entries show code, the blocks are fenced `go`, and a token is sent as `Authorization: Bearer`, with `X-CSRF-Header: -` on REST and Kepler calls. **Every entry asks first whether the destination is RelativityOne**, because the tool cannot see which host a call goes to: a Basic credential sent to another service is a real exposure, and the correct migration for that service is its own.

**What was run and what was not.** The code blocks of the entries were compiled with the Go toolchain and the standard library alone, with no module fetched, and the token-acquisition blocks were run on Linux against a local test server that stands in for the token endpoint, never against RelativityOne. What the standard library does is described from its documentation at the version read and is unchecked for any other version. The token request's field placement and the token response's field names are the standard OAuth shape and are not verified against RelativityOne.

## Confirming the flow

Ask one question of the file that holds the match: **who supplies the credential at run time?**

- If a person types it or signs in, the integration acts for that person. Use **Authorization Code**.
- If it comes from an environment variable, a configuration setting, a secret store or a constant, and no person is present, use **Client Credentials**.

The tool answers that question by looking for signals in the **same file** as the match. Part A lists every signal by name under [Flow signals](part-a-go.md#flow-signals). The rules it applies are these:

1. **An interactive-user signal decides Authorization Code.** There are six interactive-user signals. Three describe a command-line tool: a password read at the terminal with `ReadPassword`, a prompt that asks for a password and reads the standard input, and a scan of the standard input into a credential-named variable. Three describe a web server: a password read from a form, a password input in markup written into a literal, and the registration of a login route. If an interactive-user signal and a fixed-credential signal are both present, the interactive one wins.
2. **A fixed-credential signal decides Client Credentials.** There are five fixed-credential signals: a credential-named variable read from the environment with `Getenv` or `LookupEnv`, a credential-named setting read from a configuration receiver, a call of a secret-store client, a credential-named name assigned a literal, and a read of a file whose path names a secret. The recommendation carries a note asking you to check, because nothing yet measures how often such a signal points the wrong way.
3. **No signal means the default.** With no signal in the file, the tool recommends Client Credentials and carries the same note. This is the expected result for most real files and not a failure of the tool, for the reason below.

**The tool reads one file at a time.** A password typed in another package, a credential read in another file, and a credential that a caller passes in are not seen. Such a match is decided by the default even when the code acts for a signed-in user. When you review a finding, look beyond its file: follow the credential back to where it comes from, and if a person supplies it, use Authorization Code even though the tool recommended Client Credentials. **In a web application the usual split is a handler that holds the form and a package that makes the call**, so the file with the call has no interactive signal and is assigned the default, although a person typed the password.

**A file that does both is read as interactive.** A file that uses an environment credential when there is one and falls back to a prompt at the terminal when there is not holds both kinds of signal, so the interactive one wins and the tool recommends Authorization Code. If that file is in practice an unattended integration, the credential is the environment one and Client Credentials is the right flow. Decide by how the code runs, not by the fallback. A program run with piped input and no terminal that still prompts for a password on the standard input is read the same way.

**The tool does not know what kind of application it is reading.** A Go web server may act for people who sign in to it, a command-line tool may ask a person at a terminal, and a job or a service acts as itself; the tool sees only the signals in the file. That is why each Authorization Code recommendation carries both variants, each opening with the precondition it is for, and you discard the variant that does not describe your application. See [Choosing the variant for your application](#choosing-the-variant-for-your-application). A Go program does not run in a browser page, so there is no browser variant.

**Things that are not signals.** A server marker on its own (`http.ListenAndServe`, a router, `os.Args`) is not a signal, because a Go server may act as itself. A reader of the standard input with no prompt wording and no credential name is a filter or a batch job and not a person typing. A password read from the query string directly is not a posted form. A login route lookalike such as `/logout` or `/api/login-events` is not a login route. An inbound read of a Basic header (`request.BasicAuth()`) is a server checking a caller, not a person typing a password. A literal credential written into the code is **not** evidence of either flow: it is a secret to remove, and Part B explains how.

Two more rules the tool applies, so that you can reproduce its choice by hand:

- A file whose only credential is a literal is decided by the default, unless a credential-named name holds that literal, which is a fixed-credential signal.
- The tool works from the scanned code and local configuration only. It never contacts RelativityOne, so it cannot tell you what OAuth2 Clients your instance already has.

**Confirm against your instance.** If your RelativityOne instance already has an OAuth2 Client with the Code flow for this integration, use Authorization Code, whatever the tool recommended. If it has one with Client Credentials for this integration, use Client Credentials. Whether an existing OAuth2 Client can be moved from one flow to the other is not verified here; check it in your instance before assuming it can.

**Changing the default.** If most of your integrations are user-facing, you can make Authorization Code the default. Set `defaultFlow` to `AuthorizationCode` in the `.migrate-auth.json` file in your user profile folder; the accepted values are `ClientCredentials` and `AuthorizationCode`. The tool reads this setting from the user profile file only. A `defaultFlow` in the scanned repository's own file is ignored and recorded as ignored, so a repository cannot change the recommendation for the findings it holds. The setting changes only the default; a signal in the file still decides the flow.

## Choosing the variant for your application

The tool does not know whether a file runs in a web server, in a command-line tool or as a job, so the token-acquisition text of a recommendation has **variants by application type**, and each variant opens with a **precondition** sentence. Read the precondition of each variant and discard the ones that do not describe your code. The same variants appear in every class entry of the flow.

### Client Credentials: two variants

- **A Go process acting as itself.** For a Go job, daemon or service that does not act for a signed-in person. The process sends the form-encoded `POST` to the token endpoint with its client ID and client secret, caches the token, and acquires a new one on expiry. Client Credentials offers no refresh tokens, so you re-acquire and do not refresh. The token endpoint is documented for this grant. The client ID and client secret come from the process environment or a secret store, never from source. If a person signs in, use Authorization Code.
- **A Go web service that does not act for a signed-in user.** For a Go server that calls RelativityOne with no person signed in to it, so no person's identity exists to preserve. It uses the same token source in the server process. **The server must authenticate and authorize its own callers, and the OAuth2 Client's Context User must hold the minimum permissions the operation needs**: without both, the result is an unauthenticated relay into RelativityOne, which is worse than the starting point. The handler returns results to its caller, never the token and never the secret.

### Authorization Code: two variants

- **Variant 1: a Go web server.** For a Go server that signs a person in to RelativityOne. A login handler builds the authorize address with `state` and the PKCE verifier, stores them against the browser session in a short-lived `HttpOnly`, `Secure` cookie and redirects with `http.Redirect`; the callback handler, registered as one exact HTTPS URL, checks that the cookie is present and equals the `state` on the query, handles the `error` parameter and an empty `code`, consumes the `state` once, and exchanges the code with the secret. The tokens stay on the server, and the browser gets only its own session; it never holds anything from RelativityOne.
- **Variant 2: a command-line tool or a desktop process.** For a Go command-line tool or a desktop process that signs a person in. The process starts a listener from the standard library bound to `127.0.0.1` only, on the fixed port the administrator registered, for one request; it prints the authorize address so that a machine with no browser has the manual route, and opens the system browser if you want; it reads `code` and `state` from the request, closes the listener, checks `state`, and exchanges the code. The listener refuses a host that is not loopback, answers 404 to every path other than the registered one, has its own deadline, and reports a busy port as an error and does not retry on another port, because a different port is a different Redirect URL. A client secret shipped inside a distributed command-line tool can be extracted by anyone who holds the distribution, so the recommendation adds compensating guidance. Where tokens are kept is the program's decision, and is unchecked on every system.

**The endpoints and PKCE, and what is documented.** The authorize endpoint `<host>/Relativity/Identity/connect/authorize` and the code exchange at `<host>/Relativity/Identity/connect/token` are verified by NSerio; not documented by Relativity. That a Code request that does not ask for a refresh token works without PKCE is verified by NSerio; not documented by Relativity. PKCE is required by Relativity's own documentation to obtain a refresh token, and NSerio recommends `S256` for every Authorization Code client. That a plain-HTTP loopback Redirect URL `http://127.0.0.1:<port>/<path>` on a fixed registered port is accepted is verified by NSerio; not documented by Relativity, and the loopback rule of the general standard that lets a server accept any port is not claimed for RelativityOne. The token endpoint of the Client Credentials grant is the one the Relativity documentation shows.

**Common to both.** Neither variant stores a client secret or a RelativityOne token in source or in a tracked file. A credential that was written into source has been public for as long as the repository history has held it. The tool does not escalate it; the recommendation states the exposure and the rotation, and whoever leads the onboarding should raise it with the client:

> Treat this credential as compromised. Rotate it, and assume the repository history has published it. Completing the migration does not close this exposure; rotation does.

Device Code and Resource Owner are never recommended.

## Judging a Low-confidence match

A match is **Low** confidence when it is present in the text but unlikely to be live authenticating code. The tool gives a Low match no flow and no recommendation, and lists it for manual review. **A Low level is not evidence that the code does not authenticate.** It means a person has to look. The paragraphs below say how to judge each cause and when the match still needs a migration. When it does, find the live construct it belongs to, identify its class in Part A, and follow that class's entry in [Migration suggestions by pattern class](#migration-suggestions-by-pattern-class).

### Comments

The match is inside a `//` comment or a `/* */` block comment, so no code runs. Read the comment. If it is commented-out code, nothing needs migrating today, but delete it or migrate it before anyone restores it, because a restored line brings Basic Authentication back. If it describes code that exists elsewhere, search for that code with Part A and migrate the live code. If it only illustrates a pattern with placeholders, no migration is needed. The C code of a cgo preamble is also a comment to the tool and is not analyzed, so a credential written in it is not found; search such a file by hand. A block comment that nothing closes makes the tool give up on the file from its opening, so the constructs after it are reported at Low; if a file reports only Low findings after some point, look above that point for the opening of a comment.

### Documentation strings

The match is inside a string that documents or explains, such as a help message, an error message, a log line, a template, a label or a struct tag, or inside a raw string that holds another language. The tool reads the body of every string as text, so a construct spelled in a string is Low whatever the class. The text does not authenticate anything. Check that the behaviour it describes has been migrated, and update the wording afterwards so that the documentation stops describing Basic Authentication. The text itself needs no migration. One case is different: a command inside a string that a program runs, such as a `curl` line given to a function that starts a process, is not read as code and may be live. Read such a match as a command before you dismiss it. The same holds for SQL or markup inside a string.

### Test fixtures and mocks

**Test code is not Low.** A match in a file the tool recognizes as test code is reported at **Medium** at most, with the recommendation of its class, and the report marks the finding `Test code: yes` (the JSON data file sets `testCode` to `true`). The tool recognizes test code by a file name that ends in `_test.go` and by a folder named `testdata`, `test`, `tests`, `e2e`, `fixture`, `fixtures`, `testutil` or `testutils`. **A file under `testdata/` is test code and is Medium at most**, not lower. A test that uses `httptest`, a mock-server package or a stub transport is test code because of its name or its folder, and not because of the package. A test that starts a local server and reads `request.BasicAuth()` is inbound authentication and is not a finding; a test whose expected value holds an encoder over a colon-joined pair is reported at Medium as test code, because the encoder is built and never sent, and needs no migration when it only feeds a local server. Judge by what the test talks to. If it feeds a mock and never reaches RelativityOne, no migration is needed, though you may want the mock to model the token flow so the test still represents the code it covers. If it is an integration test that authenticates to a real RelativityOne instance, it needs migrating with everything else, because it stops working when Basic Authentication is switched off: follow the entry of its class. A credential written literally into a test is still a secret to remove.

**A mock is Low.** A match in a file the tool recognizes as a mock, fake or stub is reported at Low, with no recommendation: a folder named `mock`, `mocks`, `fake`, `fakes`, `stub` or `stubs`, or a file name that begins with `mock_`, `mocks_`, `fake_` or `stub_`, ends in `_mock.go`, `_mocks.go`, `_fake.go` or `_stub.go`, or is `mock.go`, `mocks.go`, `fake.go`, `fakes.go`, `stub.go` or `stubs.go`. The tool recognizes a mock by its folder or file name and by nothing else, so a mock that has a neutral name is reported at the level of its class, or at Medium when it also sits in a test folder; review it as you would a test. A file that is test code and also a mock, a sample, vendored or generated, such as `testdata/mocks/client.go` or `mock_server_test.go`, is Low, and the report still says it is test code. For a Low mock, judge by what it stands in for. If nothing runs it against RelativityOne, no migration is needed.

### Sample and example code

The match sits in a folder named `example`, `examples`, `_example`, `_examples`, `sample`, `samples`, `demo` or `demos`, in any letter case, in a file whose name ends in `example_test.go`, or in a function named `Example` or beginning `Example` that documents a package. Folders are the only names the tool recognizes, and a sample kept in a file whose name merely begins with the word is not demoted and is reported at its ordinary level, so review it as live code. Judge by who uses it. If nobody runs the sample, it needs no migration. If people copy it as a starting point, migrate it: an example that teaches Basic Authentication spreads it to new code. Use the recommendation for the class the sample matches, and prefer the flow that fits the scenario the sample shows.

### Vendored and generated files

The match sits in a folder named `vendor`, `third_party`, `third-party`, `thirdparty` or `Godeps`, which holds third-party code, or in a file produced by a tool: a name ending in `.pb.go`, `.pb.gw.go`, `_generated.go`, `.generated.go`, `_gen.go` or `.gen.go`, a name beginning with `zz_generated.`, or a file that opens with the header line `// Code generated ... DO NOT EDIT.`. **Do not edit a generated file by hand:** the next build overwrites the edit. Fix the source it is built from (the definition, the template or the generator settings) and rebuild. For vendored code, upgrade the module to a version that supports token authentication, or replace it. A migration is needed when the file runs against RelativityOne in your environment. It is not needed when the file is never called, or when it is only reference material. **A large vendored folder can push a real finding out of the findings cap**, because the cap keeps findings by path and not by confidence (see Part A, [Limits of the reading](part-a-go.md#limits-of-the-reading)); keep such folders out of the scanned path where you can.

### Deployment folders

A folder named `Deployment` does not lower a Go finding: in Go that folder holds code that authenticates, so a file under it is reported at its ordinary level and this cause does not arise for it. If a Low match is reported for a deployment folder all the same, read the file before you decide. A migration is needed when the file is the one that runs against RelativityOne in your application or in the deployment itself. It is not needed when the file is never run, or when it only illustrates a pattern. A credential written literally into a deployment file is still a secret to remove.

### GO-010 · RoundTripperBasicAuthDefinition

<a id="go-010-judging"></a>

A method named `RoundTrip` whose body holds a Basic construct: the transport that adds Basic Authentication to every request that passes through it. The tool marks the definition Low because it is a definition and not a use, and the construct inside the body is reported at its own anchor by its own class, usually [GO-001](part-a-go.md#go-001) for a `SetBasicAuth` call. **Do not dismiss it.** A transport that writes the header affects every call of the client that holds it. Find the inner finding in the same file, follow the entry of its class, and move the header write into a transport that adds the Bearer token to a clone of the request, because `RoundTrip` should not modify the request it is given; the entry of GO-001 shows that transport. A transport that writes a Basic header for a proxy is the proxy's own migration. A credential held as a field of the transport is a secret to remove and rotate even when the transport is dead code.

### GO-011 · AuthenticationHelperDefinition

<a id="go-011-judging"></a>

A function or a method, other than `RoundTrip`, that builds a Basic header from its parameters, or an encoder and a `"Basic "` literal that stand in the same function. The tool marks the definition Low because it is a definition, and the constructs inside it are reported at their own anchors. **A helper that builds a Basic value is the commonest way a Go program centralizes the header, so do not dismiss it.** When the same file calls the helper as the value of an `Authorization` header, [GO-013](part-a-go.md#go-013) reports the call at Medium and its entry says how to migrate both the call and the helper; when a call is in another file, a method call or a call through a variable, the tool does not follow it, so search for the callers by the helper's name and migrate each one. A function that holds an encoder and a `"Basic "` literal for another purpose, such as a log line or a cache key, needs no migration. A challenge builder that a server writes (`"Basic realm=..."`) is not a helper and is not reported.

### GO-012 · AuthorizationHeaderValueFromEnvironment

<a id="go-012-judging"></a>

An `Authorization` header whose whole value is an environment read, written directly or through a name that holds the read. The tool marks it Low because the scheme is not visible: the variable may hold `Basic ...`, `Bearer ...` or a bare token, and the file does not say which. **Do not dismiss it.** Read the deployment that sets the variable. If it holds a `Basic` value, the construct is a Basic header written from the environment: follow the entry of the header class [GO-002](part-a-go.md#go-002) and decide the flow from who supplies the credential, and remember that the credential then lives in the environment and in the place that sets it, which is where it is rotated. If it holds a Bearer token or an API key for another service, no migration is needed for RelativityOne. A name held in a constant, as in `os.Getenv(headerName)`, is not read at all, so search for such constants as well.

## Migration suggestions by pattern class

Each pattern class has two entries, one for each flow. Choose the entry for the flow you confirmed above. Every entry contains the code being replaced, the migrated code, the question of whether the destination is RelativityOne, a description of token acquisition, caching and expiry handling in the variants of [Choosing the variant for your application](#choosing-the-variant-for-your-application), how to confirm the flow and the default, the OAuth2 Client to create or confirm with its settings, and the pointer to Secret Management (Part B of the Migration Documentation). Every Authorization Code entry adds a description of refresh-token handling, of PKCE, and of its two variants.

**Part 1, part 2 and part 3.** The text of an entry refers to its own pieces by number. The same names appear in the report's recommendations. Each piece has a label in the entry:

- **Part 1** is the change at the point of use: the text under the label *Under token authentication*, with its migrated code.
- **Part 2** is the token acquisition: the part of the text under the label *Credentials* that follows the paragraph *Where the credentials come from*. It describes the request to the token endpoint, the caching of the token and what to do when it expires. A Client Credentials entry starts it with the text that follows the question of the destination, and an Authorization Code entry with *Common to both variants*.
- **Part 3** is the OAuth2 Client settings, under the label *OAuth2 Client* at the end of the entry.

The labels *Source form* (the construct being replaced) and *Verification* (what to check before you deploy) are not numbered.

The three Low classes, `GO-010`, `GO-011` and `GO-012`, have no entry because they produce no recommendation. They are covered under [Judging a Low-confidence match](#judging-a-low-confidence-match).

### GO-001 · RequestSetBasicAuth

<a id="go-001-migration"></a>

Remove the `SetBasicAuth` call and send a Bearer token in an `Authorization` header instead, with the `X-CSRF-Header` header the entry shows. If the call sits in the `RoundTrip` of a custom transport, move the header write into a transport that adds the token to a clone of the request. **If `SetBasicAuth` here authenticates the client at an OAuth 2.0 token endpoint**, this is not the Relativity migration: the endpoint's own documentation governs.

#### Client Credentials

<!-- generated:begin GO-001 client-credentials -->
**Confirming the flow.** When no signal in a file decides the flow, the tool assigns Client Credentials, the default, with this note: "Who supplies the credential at run time? If a person types it or signs in, the integration acts for that person: use Authorization Code. If it comes from configuration, settings, an environment variable or a secret store: use Client Credentials. No signal in this file showed which applies, so Client Credentials, the default, is recommended. Use Authorization Code instead if the integration acts on behalf of a signed-in user, or if your RelativityOne instance already has a Code-flow OAuth2 Client for this integration." A fixed-credential signal assigns it with this note: "A fixed-credential signal in this file points to Client Credentials, but how often that kind of signal is right has not been measured. Use Authorization Code instead if the integration acts on behalf of a signed-in user, or if your RelativityOne instance already has a Code-flow OAuth2 Client for this integration." See [Confirming the flow](#confirming-the-flow) and the "Which flow" paragraph of GO-001 in [Part A](part-a-go.md#go-001).

Source form: **Applies to:** every Go version; any program that calls `SetBasicAuth` on a request.

```go
request.SetBasicAuth(userName, secret)
```

Under token authentication: **First, the destination:** if this Relativity instance is not RelativityOne — including a Relativity Server instance, whose own documentation governs — this finding is informational and the rest of this template does not apply.

```go
request.Header.Set("Authorization", "Bearer "+accessToken)
request.Header.Set(csrfHeaderName, csrfHeaderValue)
```

Remove the `SetBasicAuth` call. **`accessToken` comes from the token-acquisition part that follows** (`TokenSource.Token`). **If the call is in the `RoundTrip` of a custom transport**, move the header write into a transport that adds the bearer token to **a clone** of the request, because `RoundTrip` "should not modify the request" in the standard library's documentation, and give the client that transport:

```go
type bearerTransport struct {
	source *TokenSource
	base   http.RoundTripper
}

func (transport *bearerTransport) RoundTrip(request *http.Request) (*http.Response, error) {
	token, err := transport.source.Token(request.Context())
	if err != nil {
		return nil, err
	}
	clone := request.Clone(request.Context())
	clone.Header.Set("Authorization", "Bearer "+token)
	clone.Header.Set(csrfHeaderName, csrfHeaderValue)
	return transport.base.RoundTrip(clone)
}

func targetClientWithBearerTransport(source *TokenSource) *http.Client {
	return &http.Client{Transport: &bearerTransport{source: source, base: http.DefaultTransport}}
}
```

Credentials: **Where the credentials come from.** The source form establishes that the code sends Basic, not that the destination is RelativityOne. If it is not RelativityOne, the migration is that service's own token mechanism, this finding is informational for this tool, and the reader stops here. If it is RelativityOne, the token comes from the token-acquisition part that follows, and the user name and password are replaced by the OAuth2 Client's client ID and secret, kept as Secret Management (Part B of the Migration Documentation) describes and never in source. **If `SetBasicAuth` here authenticates the client at an OAuth 2.0 token endpoint**, this is not the Relativity migration: the endpoint's own documentation governs.

**Where the token comes from.** Create an OAuth2 Client with Flow Grant Type set to Client Credentials (part 3 lists its settings), then send a form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=client_credentials`, `client_id` and `client_secret`. The token endpoint is **documented** for this grant. The token is then sent as `Authorization: Bearer <token>`, with `X-CSRF-Header: -` on REST and Kepler calls. The scope depends on what the integration needs and **no single scope is required**: Relativity's documentation shows `SystemUserInfo` and `UserInfoAccess` and no page says which is correct or what either grants; use the value the API you call documents, and confirm it. Every block below uses only the standard library and needs go1.13 or later.

**Shared declarations.** Every block of this text, and of the use-site forms above, uses these constants and helpers; `HOST` is the RelativityOne host.

```go
const (
	identityTokenURL            = "https://HOST/Relativity/Identity/connect/token"
	identityAuthorizeURL        = "https://HOST/Relativity/Identity/connect/authorize"
	clientIDEnvironmentName     = "RELATIVITY_CLIENT_ID"
	clientSecretEnvironmentName = "RELATIVITY_CLIENT_SECRET"
	expiryMarginSeconds         = 60
	csrfHeaderName              = "X-CSRF-Header"
	csrfHeaderValue             = "-"
	stateByteCount              = 16
	verifierByteCount           = 32
	serverHeaderTimeoutSeconds  = 10
	signInCookieName            = "relativity_sign_in"
	signInCookieMaxAgeSeconds   = 600
	signInTimeoutSeconds        = 300
)

type tokenResponse struct {
	AccessToken  string `json:"access_token"`
	RefreshToken string `json:"refresh_token"`
	ExpiresIn    int    `json:"expires_in"`
}

func requestToken(ctx context.Context, client *http.Client, form url.Values) (tokenResponse, error) {
	var payload tokenResponse
	request, err := http.NewRequestWithContext(ctx, http.MethodPost, identityTokenURL, strings.NewReader(form.Encode()))
	if err != nil {
		return payload, err
	}
	request.Header.Set("Content-Type", "application/x-www-form-urlencoded")
	response, err := client.Do(request)
	if err != nil {
		return payload, err
	}
	defer response.Body.Close()
	if response.StatusCode != http.StatusOK {
		return payload, fmt.Errorf("token endpoint answered %d", response.StatusCode)
	}
	err = json.NewDecoder(response.Body).Decode(&payload)
	return payload, err
}
```

The token request is a form-encoded `POST`. **Where the client secret travels in the token request** (the form body, as here, or HTTP Basic client authentication) **and the field names of the token response are not documented by Relativity**: the form body and `tokenResponse` are the standard OAuth shape and an NSerio recommendation, **not verified** against RelativityOne. **Do not** send the client secret with `SetBasicAuth` and **do not** put it in the URL.

**Variant: a Go process acting as itself.** **Precondition: this variant is for a Go job, daemon or service that does not act for a signed-in person.**

```go
type TokenSource struct {
	mutex     sync.Mutex
	client    *http.Client
	token     string
	expiresAt time.Time
}

func (source *TokenSource) Token(ctx context.Context) (string, error) {
	source.mutex.Lock()
	defer source.mutex.Unlock()
	if source.token == "" || time.Until(source.expiresAt) < expiryMarginSeconds*time.Second {
		form := url.Values{
			"grant_type":    {"client_credentials"},
			"client_id":     {os.Getenv(clientIDEnvironmentName)},
			"client_secret": {os.Getenv(clientSecretEnvironmentName)},
		}
		payload, err := requestToken(ctx, source.client, form)
		if err != nil {
			return "", err
		}
		source.token = payload.AccessToken
		source.expiresAt = time.Now().Add(time.Duration(payload.ExpiresIn) * time.Second)
	}
	return source.token, nil
}
```

**Cache the token and re-acquire it on expiry — do not refresh.** Client Credentials does not offer refresh tokens: **verified by NSerio against a live instance; not documented by Relativity** — the Allow Refresh Tokens control does not appear for this grant in the RelativityOne user interface. The recommended Access Token Lifetime is 60 minutes. **The client secret** belongs in the process environment or a secret store, as Secret Management (Part B of the Migration Documentation) describes, **never in source, in a tracked `.env` file or in a log**: the block reads it with `os.Getenv`, which returns an empty string when the variable is not present, so an unset variable is an empty secret and the token request fails, which the code reports. A reset invalidates the previous secret immediately, so plan the cut-over; a second client created before the reset is an inferred way to do it without a gap. **Before deploying, confirm the calling identity:** every call now runs as the OAuth2 Client's Context User, not as the user in the replaced credential, and a non-administrator Context User is NSerio's recommendation. Trusted IPs restrict where a token can be requested, not a token already issued. The statements that Client Credentials needs a client secret and that the secret is displayed once are carried from the other languages' guidance and are not verified for this text.

**Variant: a Go web service that does not act for a signed-in user.** **Precondition: this variant is for a Go server that calls RelativityOne with no person signed in to it, so no person's identity exists to preserve. If a person signs in, use Authorization Code, variant 1.**

Use the block above in the server process; **the server must authenticate and authorize its own callers, and the OAuth2 Client's Context User must hold the minimum permissions the operation needs. Without both, the result is an unauthenticated relay into RelativityOne**: every caller reaching it acts with the Context User's full rights, which is strictly worse than the starting point. The handler returns results to its caller, **never the token** and never the secret.

Verification: **Verification note.** **A token has a finite lifetime.** Acquire again within it (the token source does). **The calling identity changes.** The token-acquisition part says to whom. **Other uses may not appear as findings**: a request or a header held in a struct and handed to a helper elsewhere is not read.

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
<!-- generated:end GO-001 client-credentials -->

#### Authorization Code

<!-- generated:begin GO-001 authorization-code -->
**Confirming the flow.** An interactive-user signal in a file assigns Authorization Code, with this note: "An interactive-user signal in this file points to Authorization Code. Confirm the flow: use Client Credentials instead if the integration runs with credentials fixed for the integration and no signed-in user." Your user profile can also make Authorization Code the default; see Changing the default in [Confirming the flow](#confirming-the-flow). See [Confirming the flow](#confirming-the-flow) and the "Which flow" paragraph of GO-001 in [Part A](part-a-go.md#go-001).

Source form: **Applies to:** every Go version; any program that calls `SetBasicAuth` on a request.

```go
request.SetBasicAuth(userName, secret)
```

Under token authentication: **First, the destination:** if this Relativity instance is not RelativityOne — including a Relativity Server instance, whose own documentation governs — this finding is informational and the rest of this template does not apply.

```go
request.Header.Set("Authorization", "Bearer "+accessToken)
request.Header.Set(csrfHeaderName, csrfHeaderValue)
```

Remove the `SetBasicAuth` call. **`accessToken` comes from the token-acquisition part that follows** (`TokenSource.Token`). **If the call is in the `RoundTrip` of a custom transport**, move the header write into a transport that adds the bearer token to **a clone** of the request, because `RoundTrip` "should not modify the request" in the standard library's documentation, and give the client that transport:

```go
type bearerTransport struct {
	source *TokenSource
	base   http.RoundTripper
}

func (transport *bearerTransport) RoundTrip(request *http.Request) (*http.Response, error) {
	token, err := transport.source.Token(request.Context())
	if err != nil {
		return nil, err
	}
	clone := request.Clone(request.Context())
	clone.Header.Set("Authorization", "Bearer "+token)
	clone.Header.Set(csrfHeaderName, csrfHeaderValue)
	return transport.base.RoundTrip(clone)
}

func targetClientWithBearerTransport(source *TokenSource) *http.Client {
	return &http.Client{Transport: &bearerTransport{source: source, base: http.DefaultTransport}}
}
```

Credentials: **Where the credentials come from.** The source form establishes that the code sends Basic, not that the destination is RelativityOne. If it is not RelativityOne, the migration is that service's own token mechanism, this finding is informational for this tool, and the reader stops here. If it is RelativityOne, the token comes from the token-acquisition part that follows, and the user name and password are replaced by the OAuth2 Client's client ID and secret, kept as Secret Management (Part B of the Migration Documentation) describes and never in source. **If `SetBasicAuth` here authenticates the client at an OAuth 2.0 token endpoint**, this is not the Relativity migration: the endpoint's own documentation governs.

**Common to both variants.** The person signs in on RelativityOne's own page and the code never holds their password. The text has a common part and two variants by application type, and each variant opens with its own precondition, so that the reader can discard the others by reading one sentence each. Every block uses only the standard library and needs go1.13 or later; the shared declarations are those of the Client Credentials text and are repeated here.

```go
const (
	identityTokenURL            = "https://HOST/Relativity/Identity/connect/token"
	identityAuthorizeURL        = "https://HOST/Relativity/Identity/connect/authorize"
	clientIDEnvironmentName     = "RELATIVITY_CLIENT_ID"
	clientSecretEnvironmentName = "RELATIVITY_CLIENT_SECRET"
	expiryMarginSeconds         = 60
	csrfHeaderName              = "X-CSRF-Header"
	csrfHeaderValue             = "-"
	stateByteCount              = 16
	verifierByteCount           = 32
	serverHeaderTimeoutSeconds  = 10
	signInCookieName            = "relativity_sign_in"
	signInCookieMaxAgeSeconds   = 600
	signInTimeoutSeconds        = 300
)

type tokenResponse struct {
	AccessToken  string `json:"access_token"`
	RefreshToken string `json:"refresh_token"`
	ExpiresIn    int    `json:"expires_in"`
}

func requestToken(ctx context.Context, client *http.Client, form url.Values) (tokenResponse, error) {
	var payload tokenResponse
	request, err := http.NewRequestWithContext(ctx, http.MethodPost, identityTokenURL, strings.NewReader(form.Encode()))
	if err != nil {
		return payload, err
	}
	request.Header.Set("Content-Type", "application/x-www-form-urlencoded")
	response, err := client.Do(request)
	if err != nil {
		return payload, err
	}
	defer response.Body.Close()
	if response.StatusCode != http.StatusOK {
		return payload, fmt.Errorf("token endpoint answered %d", response.StatusCode)
	}
	err = json.NewDecoder(response.Body).Decode(&payload)
	return payload, err
}
```

**Sign in.** Replace the code that collected the person's credentials with a redirect to the authorize endpoint `<host>/Relativity/Identity/connect/authorize`, which is **verified by NSerio against a live instance; not documented by Relativity** and leads to RelativityOne's login page. The request carries the standard authorization-code parameters — `client_id`, `response_type=code`, the registered `redirect_uri`, `state` checked on return, and `code_challenge` with `code_challenge_method=S256` when PKCE is used; these are standard OAuth and Relativity documents none of them.

**PKCE.** Relativity's own documentation requires PKCE to obtain a refresh token; that a Code request that does not ask for a refresh token works without PKCE is **verified by NSerio against a live instance; not documented by Relativity**. **PKCE is not limited to clients that use refresh tokens**: NSerio recommends `S256` for every Authorization Code client (NSerio recommendation) and does not claim Relativity accepts any particular method. **Whether a client with refresh tokens disabled accepts a PKCE request (a `code_challenge`) is unverified**: test it before the block is relied on for such a client. **`state`** is generated, stored against the session and compared on return; `nonce` is not used, and Relativity does not document whether it enforces either. The authorize and token endpoints and the PKCE and `state` behaviour above are **verified by NSerio against a live instance; not documented by Relativity**. The block draws the verifier and `state` from `crypto/rand`, whose `Read` fills the buffer with cryptographically secure random bytes (the signature still returns an error, which the block checks), and computes the challenge with `crypto/sha256` and `base64.RawURLEncoding`, the unpadded URL-safe encoding; a 32-byte value encodes to a 43-character verifier.

**The client secret is required.** Code does not work without a client secret: tested and rejected, **verified by NSerio against a live instance; not documented by Relativity**, which confirms Relativity's own statement. **Any secret placed in a distributed binary is extractable by whoever holds the binary**: a Go program compiled for people other than its owner carries its client secret in a form anyone who holds the file can extract, and Relativity documents no recommendation for that case; use a short Access Token Lifetime, Trusted IPs where the outbound addresses are stable and one OAuth2 Client per distribution as compensating guidance (NSerio recommendation). Keep the secret as Secret Management (Part B of the Migration Documentation) describes, never in source or in a log.

**Exchange the code.** A form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=authorization_code`, the code, the same `redirect_uri`, the client ID and the secret, plus the `code_verifier` when PKCE is used.

```go
func randomToken(byteCount int) (string, error) {
	buffer := make([]byte, byteCount)
	if _, err := rand.Read(buffer); err != nil {
		return "", err
	}
	return base64.RawURLEncoding.EncodeToString(buffer), nil
}

func challengeOf(verifier string) string {
	digest := sha256.Sum256([]byte(verifier))
	return base64.RawURLEncoding.EncodeToString(digest[:])
}

func authorizeAddress(redirectURI string, state string, challenge string) string {
	query := url.Values{
		"client_id":     {os.Getenv(clientIDEnvironmentName)},
		"response_type": {"code"},
		"redirect_uri":  {redirectURI},
		"state":         {state},
	}
	if challenge != "" {
		query.Set("code_challenge", challenge)
		query.Set("code_challenge_method", "S256")
	}
	return identityAuthorizeURL + "?" + query.Encode()
}

func exchangeCode(ctx context.Context, client *http.Client, code string, redirectURI string, verifier string) (tokenResponse, error) {
	form := url.Values{
		"grant_type":    {"authorization_code"},
		"code":          {code},
		"redirect_uri":  {redirectURI},
		"client_id":     {os.Getenv(clientIDEnvironmentName)},
		"client_secret": {os.Getenv(clientSecretEnvironmentName)},
	}
	if verifier != "" {
		form.Set("code_verifier", verifier)
	}
	return requestToken(ctx, client, form)
}
```

**Redirect URL.** A server-side application registers **one exact HTTPS URL per environment, no wildcards**; the matching rules are not documented and no claim is made about them. A plain-HTTP loopback Redirect URL `http://127.0.0.1:<port>/<path>` on a **fixed registered port** is accepted, **verified by NSerio against a live instance; not documented by Relativity**, and is the command-line and desktop shape.

**On expiry.** With Allow Refresh Tokens enabled and PKCE used, the code holds a refresh token and sends `grant_type=refresh_token` in place of the code exchange: *inferred* from the standard OAuth shape, not tested; Relativity does not document a refresh token's lifetime, rotation or revocation, nor what disabling the client does to tokens already issued. **A refresh token is a secret**: hold it where the secret is held, never in a log. **Single sign-on:** sign-in happens on RelativityOne's own page, so the instance's single sign-on and two-factor rules apply unchanged (*inferred*). **The code stops collecting the password:** after this change nothing receives, stores or forwards the person's user name and password, and the code holds only tokens. Every call now runs as the person who signed in, so **the calling identity changes**. **Scope:** no scope set is prescribed for Code; `openid RelativityRequestOrigin UserInfo UserInfoAccess AuthenticationID RelativityWeb` works, **verified by NSerio against a live instance; not documented by Relativity**, as working and not as minimal.

**Variant 1 — a Go web server.** **Precondition: this variant is for a Go server that signs a person in to RelativityOne. If the code is a command-line tool or a desktop process, use variant 2.**

A server-side component creates `state` and the PKCE verifier, redirects to the authorize endpoint, receives the code at its registered HTTPS Redirect URL, exchanges it, keeps the tokens on the server and gives the browser **only its own session**; **the browser never holds anything from RelativityOne**. The two handlers use the standard library's `net/http`: `http.Redirect` replies to the request with a redirect to the authorize address.

```go
type WebSignIn struct {
	mutex       sync.Mutex
	pending     map[string]string
	client      *http.Client
	redirectURI string
	keepTokens  func(w http.ResponseWriter, r *http.Request, tokens tokenResponse)
}

func (signIn *WebSignIn) Login(w http.ResponseWriter, r *http.Request) {
	state, err := randomToken(stateByteCount)
	if err != nil {
		http.Error(w, "sign-in unavailable", http.StatusInternalServerError)
		return
	}
	verifier, err := randomToken(verifierByteCount)
	if err != nil {
		http.Error(w, "sign-in unavailable", http.StatusInternalServerError)
		return
	}
	signIn.mutex.Lock()
	signIn.pending[state] = verifier
	signIn.mutex.Unlock()
	http.SetCookie(w, &http.Cookie{
		Name:     signInCookieName,
		Value:    state,
		Path:     "/",
		MaxAge:   signInCookieMaxAgeSeconds,
		HttpOnly: true,
		Secure:   true,
		SameSite: http.SameSiteLaxMode,
	})
	http.Redirect(w, r, authorizeAddress(signIn.redirectURI, state, challengeOf(verifier)), http.StatusFound)
}

func (signIn *WebSignIn) Callback(w http.ResponseWriter, r *http.Request) {
	query := r.URL.Query()
	if query.Get("error") != "" {
		http.Error(w, "sign-in was not completed", http.StatusUnauthorized)
		return
	}
	state := query.Get("state")
	cookie, err := r.Cookie(signInCookieName)
	if err != nil || query.Get("code") == "" || subtle.ConstantTimeCompare([]byte(cookie.Value), []byte(state)) != 1 {
		http.Error(w, "unknown sign-in", http.StatusBadRequest)
		return
	}
	signIn.mutex.Lock()
	verifier, known := signIn.pending[state]
	delete(signIn.pending, state)
	signIn.mutex.Unlock()
	if !known {
		http.Error(w, "unknown sign-in", http.StatusBadRequest)
		return
	}
	http.SetCookie(w, &http.Cookie{Name: signInCookieName, Path: "/", MaxAge: -1})
	tokens, err := exchangeCode(r.Context(), signIn.client, query.Get("code"), signIn.redirectURI, verifier)
	if err != nil {
		http.Error(w, "sign-in failed", http.StatusBadGateway)
		return
	}
	signIn.keepTokens(w, r, tokens)
}
```

**`state` is bound to the browser session**: `Login` stores it in a short-lived, `HttpOnly`, `Secure`, `SameSite=Lax` cookie as well as in the server's pending table, and `Callback` fails (400) unless the cookie is present **and** equals the `state` on the query, so a callback that arrives without the matching cookie gets no exchange. `Callback` also **handles the `error` parameter** (401, no exchange) and **an empty `code`** (400). `state` is consumed once: the entry is deleted before the exchange and the cookie is cleared, so a replayed or unknown `state` gets no exchange. `keepTokens` is yours: store the tokens against the visitor's session on the server and set a session cookie that holds **only a session identifier**. The refresh call, if used, goes to the same token endpoint with `grant_type=refresh_token` (*inferred*). **The calling identity changes** to the person who signed in.

**Variant 2 — a command-line tool or a desktop process.** **Precondition: this variant is for a Go command-line tool or a desktop process that signs a person in to RelativityOne. If the code is a server, use variant 1.**

The process starts a listener from the standard library bound to `127.0.0.1` only, on the **fixed port the administrator registered** (the exact URL is registered, so the port is fixed, chosen by the administrator and not at runtime), for **one request**; it **prints the authorize address** (so a machine with no browser, an SSH session or a failed launch has the manual route) and, if you want, opens the system browser yourself; reads `code` and `state` from the one request, answers with a short page, closes the listener, checks `state` and exchanges the code.

```go
func SignInFromCommandLine(ctx context.Context, client *http.Client, listenAddress string, redirectURI string) (tokenResponse, error) {
	var tokens tokenResponse
	redirect, err := url.Parse(redirectURI)
	if err != nil {
		return tokens, err
	}
	host, _, err := net.SplitHostPort(listenAddress)
	if err != nil {
		return tokens, err
	}
	if host != "127.0.0.1" && host != "::1" {
		return tokens, errors.New("the sign-in listener must be bound to the loopback address")
	}
	ctx, cancel := context.WithTimeout(ctx, signInTimeoutSeconds*time.Second)
	defer cancel()
	state, err := randomToken(stateByteCount)
	if err != nil {
		return tokens, err
	}
	verifier, err := randomToken(verifierByteCount)
	if err != nil {
		return tokens, err
	}
	listener, err := net.Listen("tcp", listenAddress)
	if err != nil {
		return tokens, fmt.Errorf("the registered port is not available: %w", err)
	}
	type callback struct{ code, state, failure string }
	received := make(chan callback, 1)
	server := &http.Server{
		ReadHeaderTimeout: serverHeaderTimeoutSeconds * time.Second,
		Handler: http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
			if r.URL.Path != redirect.Path {
				http.NotFound(w, r)
				return
			}
			query := r.URL.Query()
			select {
			case received <- callback{code: query.Get("code"), state: query.Get("state"), failure: query.Get("error")}:
			default:
			}
			fmt.Fprintln(w, "Sign-in received. You can close this window.")
		}),
	}
	go func() { _ = server.Serve(listener) }()
	defer server.Shutdown(context.Background())
	fmt.Println("Open this address in a browser to sign in:", authorizeAddress(redirectURI, state, challengeOf(verifier)))
	select {
	case result := <-received:
		if result.failure != "" || result.code == "" {
			return tokens, errors.New("sign-in was not completed")
		}
		if subtle.ConstantTimeCompare([]byte(result.state), []byte(state)) != 1 {
			return tokens, errors.New("the sign-in answer does not match the request")
		}
		return exchangeCode(ctx, client, result.code, redirectURI, verifier)
	case <-ctx.Done():
		return tokens, ctx.Err()
	}
}
```

**The listener is checked and bounded in the code**: it **refuses a `listenAddress` whose host is not `127.0.0.1` or `::1`** (loopback only); its handler **answers 404 to every path other than the registered redirect path** (so a stray request cannot consume the one-shot channel); the whole sign-in has **its own deadline** (`signInTimeoutSeconds`, 300 seconds, on top of the caller's context), after which it returns the context error; a callback with an `error` parameter or an empty `code` is an error, not an exchange. **Busy port:** the port is the registered one and is **not** replaced by another (a different port is a different Redirect URL, which Relativity would not match); if `net.Listen` fails because the port is taken, the function returns the error `the registered port is not available` and the tool must report it, not retry on another port. Call it with `listenAddress` `127.0.0.1:<the registered port>` and `redirectURI` `http://127.0.0.1:<the registered port>/<path>`. **Where the tokens are kept** is the program's decision: in memory for the run, or, for a refresh token, in a file whose permissions you restrict (**a distributed command-line tool carries its client secret in a form anyone who holds the distribution can extract**). Every call now runs as the person who signed in.

Verification: **Verification note.** **A token has a finite lifetime.** Acquire again within it (the token source does). **The calling identity changes.** The token-acquisition part says to whom. **Other uses may not appear as findings**: a request or a header held in a struct and handed to a helper elsewhere is not read.

OAuth2 Client: Create or confirm this OAuth2 Client in RelativityOne (NSerio's observation: the menu path is Home > Authentication > OAuth2 Client Manager, as Relativity's REST API authentication page names it). This tool never connects to RelativityOne and never creates or changes the OAuth2 Client; the settings below are guidance.
- Name: a descriptive name identifying the application it serves.
- Flow Grant Type: Code
- Redirect URLs: Relativity documentation: each URL includes the http or https protocol prefix. The exact callback URL is registered by the Relativity administrator. Server-side web applications, NSerio's recommendation: one exact HTTPS URL per environment, with no wildcards. Desktop and CLI applications: a plain-HTTP loopback URL, `http://127.0.0.1:<port>/<path>`, on a fixed port because the URL is registered in advance and the port cannot be chosen at runtime (verified by NSerio against a live instance; not documented by Relativity).
- Access Token Lifetime: 60 minutes, stated in minutes because the field is in minutes. Relativity documentation: tokens for this flow must have a short lifetime, and the recommended value matches the Identity Server default.
- Allow refresh tokens: NSerio's recommendation: leave it off unless the application must keep a user session active beyond the access token lifetime without asking the user to sign in again. If it is on, the refresh tokens are stored as secrets, per Secret Management (Part B of the Migration Documentation).
- Enabled: on.
<!-- generated:end GO-001 authorization-code -->

### GO-002 · AuthorizationHeaderBasicScheme

<a id="go-002-migration"></a>

Change the value of the `Authorization` header from `Basic ...` to `Bearer ...` at the place the header is written, and add the `X-CSRF-Header` header the entry shows. The same replacement applies to a header written with `Add` and with an index assignment; for an index assignment, use `Set`, which canonicalizes the key. Delete the encoder and the pair, and keep no variable that holds the scheme.

#### Client Credentials

<!-- generated:begin GO-002 client-credentials -->
**Confirming the flow.** When no signal in a file decides the flow, the tool assigns Client Credentials, the default, with this note: "Who supplies the credential at run time? If a person types it or signs in, the integration acts for that person: use Authorization Code. If it comes from configuration, settings, an environment variable or a secret store: use Client Credentials. No signal in this file showed which applies, so Client Credentials, the default, is recommended. Use Authorization Code instead if the integration acts on behalf of a signed-in user, or if your RelativityOne instance already has a Code-flow OAuth2 Client for this integration." A fixed-credential signal assigns it with this note: "A fixed-credential signal in this file points to Client Credentials, but how often that kind of signal is right has not been measured. Use Authorization Code instead if the integration acts on behalf of a signed-in user, or if your RelativityOne instance already has a Code-flow OAuth2 Client for this integration." See [Confirming the flow](#confirming-the-flow) and the "Which flow" paragraph of GO-002 in [Part A](part-a-go.md#go-002).

Source form: **Applies to:** every Go version; a header written by `Set`, `Add` or an index assignment.

```go
encoded := base64.StdEncoding.EncodeToString([]byte(userName + ":" + secret))
request.Header.Set("Authorization", "Basic "+encoded)
```

Under token authentication: **First, the destination:** if this Relativity instance is not RelativityOne — including a Relativity Server instance, whose own documentation governs — this finding is informational and the rest of this template does not apply.

```go
request.Header.Set("Authorization", "Bearer "+accessToken)
request.Header.Set(csrfHeaderName, csrfHeaderValue)
```

Delete the encoder and the pair; keep **no** variable that holds the scheme. For an index assignment (`request.Header["Authorization"] = []string{…}`) use `Set`, which canonicalizes the key.

Credentials: **Where the credentials come from.** The source form establishes that the code sends Basic, not that the destination is RelativityOne. If it is not RelativityOne, the migration is that service's own token mechanism, this finding is informational for this tool, and the reader stops here. If it is RelativityOne, the token comes from the token-acquisition part that follows, and the user name and password are replaced by the OAuth2 Client's client ID and secret, kept as Secret Management (Part B of the Migration Documentation) describes and never in source.

**Where the token comes from.** Create an OAuth2 Client with Flow Grant Type set to Client Credentials (part 3 lists its settings), then send a form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=client_credentials`, `client_id` and `client_secret`. The token endpoint is **documented** for this grant. The token is then sent as `Authorization: Bearer <token>`, with `X-CSRF-Header: -` on REST and Kepler calls. The scope depends on what the integration needs and **no single scope is required**: Relativity's documentation shows `SystemUserInfo` and `UserInfoAccess` and no page says which is correct or what either grants; use the value the API you call documents, and confirm it. Every block below uses only the standard library and needs go1.13 or later.

**Shared declarations.** Every block of this text, and of the use-site forms above, uses these constants and helpers; `HOST` is the RelativityOne host.

```go
const (
	identityTokenURL            = "https://HOST/Relativity/Identity/connect/token"
	identityAuthorizeURL        = "https://HOST/Relativity/Identity/connect/authorize"
	clientIDEnvironmentName     = "RELATIVITY_CLIENT_ID"
	clientSecretEnvironmentName = "RELATIVITY_CLIENT_SECRET"
	expiryMarginSeconds         = 60
	csrfHeaderName              = "X-CSRF-Header"
	csrfHeaderValue             = "-"
	stateByteCount              = 16
	verifierByteCount           = 32
	serverHeaderTimeoutSeconds  = 10
	signInCookieName            = "relativity_sign_in"
	signInCookieMaxAgeSeconds   = 600
	signInTimeoutSeconds        = 300
)

type tokenResponse struct {
	AccessToken  string `json:"access_token"`
	RefreshToken string `json:"refresh_token"`
	ExpiresIn    int    `json:"expires_in"`
}

func requestToken(ctx context.Context, client *http.Client, form url.Values) (tokenResponse, error) {
	var payload tokenResponse
	request, err := http.NewRequestWithContext(ctx, http.MethodPost, identityTokenURL, strings.NewReader(form.Encode()))
	if err != nil {
		return payload, err
	}
	request.Header.Set("Content-Type", "application/x-www-form-urlencoded")
	response, err := client.Do(request)
	if err != nil {
		return payload, err
	}
	defer response.Body.Close()
	if response.StatusCode != http.StatusOK {
		return payload, fmt.Errorf("token endpoint answered %d", response.StatusCode)
	}
	err = json.NewDecoder(response.Body).Decode(&payload)
	return payload, err
}
```

The token request is a form-encoded `POST`. **Where the client secret travels in the token request** (the form body, as here, or HTTP Basic client authentication) **and the field names of the token response are not documented by Relativity**: the form body and `tokenResponse` are the standard OAuth shape and an NSerio recommendation, **not verified** against RelativityOne. **Do not** send the client secret with `SetBasicAuth` and **do not** put it in the URL.

**Variant: a Go process acting as itself.** **Precondition: this variant is for a Go job, daemon or service that does not act for a signed-in person.**

```go
type TokenSource struct {
	mutex     sync.Mutex
	client    *http.Client
	token     string
	expiresAt time.Time
}

func (source *TokenSource) Token(ctx context.Context) (string, error) {
	source.mutex.Lock()
	defer source.mutex.Unlock()
	if source.token == "" || time.Until(source.expiresAt) < expiryMarginSeconds*time.Second {
		form := url.Values{
			"grant_type":    {"client_credentials"},
			"client_id":     {os.Getenv(clientIDEnvironmentName)},
			"client_secret": {os.Getenv(clientSecretEnvironmentName)},
		}
		payload, err := requestToken(ctx, source.client, form)
		if err != nil {
			return "", err
		}
		source.token = payload.AccessToken
		source.expiresAt = time.Now().Add(time.Duration(payload.ExpiresIn) * time.Second)
	}
	return source.token, nil
}
```

**Cache the token and re-acquire it on expiry — do not refresh.** Client Credentials does not offer refresh tokens: **verified by NSerio against a live instance; not documented by Relativity** — the Allow Refresh Tokens control does not appear for this grant in the RelativityOne user interface. The recommended Access Token Lifetime is 60 minutes. **The client secret** belongs in the process environment or a secret store, as Secret Management (Part B of the Migration Documentation) describes, **never in source, in a tracked `.env` file or in a log**: the block reads it with `os.Getenv`, which returns an empty string when the variable is not present, so an unset variable is an empty secret and the token request fails, which the code reports. A reset invalidates the previous secret immediately, so plan the cut-over; a second client created before the reset is an inferred way to do it without a gap. **Before deploying, confirm the calling identity:** every call now runs as the OAuth2 Client's Context User, not as the user in the replaced credential, and a non-administrator Context User is NSerio's recommendation. Trusted IPs restrict where a token can be requested, not a token already issued. The statements that Client Credentials needs a client secret and that the secret is displayed once are carried from the other languages' guidance and are not verified for this text.

**Variant: a Go web service that does not act for a signed-in user.** **Precondition: this variant is for a Go server that calls RelativityOne with no person signed in to it, so no person's identity exists to preserve. If a person signs in, use Authorization Code, variant 1.**

Use the block above in the server process; **the server must authenticate and authorize its own callers, and the OAuth2 Client's Context User must hold the minimum permissions the operation needs. Without both, the result is an unauthenticated relay into RelativityOne**: every caller reaching it acts with the Context User's full rights, which is strictly worse than the starting point. The handler returns results to its caller, **never the token** and never the secret.

Verification: **Verification note.** **A token has a finite lifetime.** **The calling identity changes.** A `Basic` value built in another function and handed here is read at its own site, not at this one.

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
<!-- generated:end GO-002 client-credentials -->

#### Authorization Code

<!-- generated:begin GO-002 authorization-code -->
**Confirming the flow.** An interactive-user signal in a file assigns Authorization Code, with this note: "An interactive-user signal in this file points to Authorization Code. Confirm the flow: use Client Credentials instead if the integration runs with credentials fixed for the integration and no signed-in user." Your user profile can also make Authorization Code the default; see Changing the default in [Confirming the flow](#confirming-the-flow). See [Confirming the flow](#confirming-the-flow) and the "Which flow" paragraph of GO-002 in [Part A](part-a-go.md#go-002).

Source form: **Applies to:** every Go version; a header written by `Set`, `Add` or an index assignment.

```go
encoded := base64.StdEncoding.EncodeToString([]byte(userName + ":" + secret))
request.Header.Set("Authorization", "Basic "+encoded)
```

Under token authentication: **First, the destination:** if this Relativity instance is not RelativityOne — including a Relativity Server instance, whose own documentation governs — this finding is informational and the rest of this template does not apply.

```go
request.Header.Set("Authorization", "Bearer "+accessToken)
request.Header.Set(csrfHeaderName, csrfHeaderValue)
```

Delete the encoder and the pair; keep **no** variable that holds the scheme. For an index assignment (`request.Header["Authorization"] = []string{…}`) use `Set`, which canonicalizes the key.

Credentials: **Where the credentials come from.** The source form establishes that the code sends Basic, not that the destination is RelativityOne. If it is not RelativityOne, the migration is that service's own token mechanism, this finding is informational for this tool, and the reader stops here. If it is RelativityOne, the token comes from the token-acquisition part that follows, and the user name and password are replaced by the OAuth2 Client's client ID and secret, kept as Secret Management (Part B of the Migration Documentation) describes and never in source.

**Common to both variants.** The person signs in on RelativityOne's own page and the code never holds their password. The text has a common part and two variants by application type, and each variant opens with its own precondition, so that the reader can discard the others by reading one sentence each. Every block uses only the standard library and needs go1.13 or later; the shared declarations are those of the Client Credentials text and are repeated here.

```go
const (
	identityTokenURL            = "https://HOST/Relativity/Identity/connect/token"
	identityAuthorizeURL        = "https://HOST/Relativity/Identity/connect/authorize"
	clientIDEnvironmentName     = "RELATIVITY_CLIENT_ID"
	clientSecretEnvironmentName = "RELATIVITY_CLIENT_SECRET"
	expiryMarginSeconds         = 60
	csrfHeaderName              = "X-CSRF-Header"
	csrfHeaderValue             = "-"
	stateByteCount              = 16
	verifierByteCount           = 32
	serverHeaderTimeoutSeconds  = 10
	signInCookieName            = "relativity_sign_in"
	signInCookieMaxAgeSeconds   = 600
	signInTimeoutSeconds        = 300
)

type tokenResponse struct {
	AccessToken  string `json:"access_token"`
	RefreshToken string `json:"refresh_token"`
	ExpiresIn    int    `json:"expires_in"`
}

func requestToken(ctx context.Context, client *http.Client, form url.Values) (tokenResponse, error) {
	var payload tokenResponse
	request, err := http.NewRequestWithContext(ctx, http.MethodPost, identityTokenURL, strings.NewReader(form.Encode()))
	if err != nil {
		return payload, err
	}
	request.Header.Set("Content-Type", "application/x-www-form-urlencoded")
	response, err := client.Do(request)
	if err != nil {
		return payload, err
	}
	defer response.Body.Close()
	if response.StatusCode != http.StatusOK {
		return payload, fmt.Errorf("token endpoint answered %d", response.StatusCode)
	}
	err = json.NewDecoder(response.Body).Decode(&payload)
	return payload, err
}
```

**Sign in.** Replace the code that collected the person's credentials with a redirect to the authorize endpoint `<host>/Relativity/Identity/connect/authorize`, which is **verified by NSerio against a live instance; not documented by Relativity** and leads to RelativityOne's login page. The request carries the standard authorization-code parameters — `client_id`, `response_type=code`, the registered `redirect_uri`, `state` checked on return, and `code_challenge` with `code_challenge_method=S256` when PKCE is used; these are standard OAuth and Relativity documents none of them.

**PKCE.** Relativity's own documentation requires PKCE to obtain a refresh token; that a Code request that does not ask for a refresh token works without PKCE is **verified by NSerio against a live instance; not documented by Relativity**. **PKCE is not limited to clients that use refresh tokens**: NSerio recommends `S256` for every Authorization Code client (NSerio recommendation) and does not claim Relativity accepts any particular method. **Whether a client with refresh tokens disabled accepts a PKCE request (a `code_challenge`) is unverified**: test it before the block is relied on for such a client. **`state`** is generated, stored against the session and compared on return; `nonce` is not used, and Relativity does not document whether it enforces either. The authorize and token endpoints and the PKCE and `state` behaviour above are **verified by NSerio against a live instance; not documented by Relativity**. The block draws the verifier and `state` from `crypto/rand`, whose `Read` fills the buffer with cryptographically secure random bytes (the signature still returns an error, which the block checks), and computes the challenge with `crypto/sha256` and `base64.RawURLEncoding`, the unpadded URL-safe encoding; a 32-byte value encodes to a 43-character verifier.

**The client secret is required.** Code does not work without a client secret: tested and rejected, **verified by NSerio against a live instance; not documented by Relativity**, which confirms Relativity's own statement. **Any secret placed in a distributed binary is extractable by whoever holds the binary**: a Go program compiled for people other than its owner carries its client secret in a form anyone who holds the file can extract, and Relativity documents no recommendation for that case; use a short Access Token Lifetime, Trusted IPs where the outbound addresses are stable and one OAuth2 Client per distribution as compensating guidance (NSerio recommendation). Keep the secret as Secret Management (Part B of the Migration Documentation) describes, never in source or in a log.

**Exchange the code.** A form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=authorization_code`, the code, the same `redirect_uri`, the client ID and the secret, plus the `code_verifier` when PKCE is used.

```go
func randomToken(byteCount int) (string, error) {
	buffer := make([]byte, byteCount)
	if _, err := rand.Read(buffer); err != nil {
		return "", err
	}
	return base64.RawURLEncoding.EncodeToString(buffer), nil
}

func challengeOf(verifier string) string {
	digest := sha256.Sum256([]byte(verifier))
	return base64.RawURLEncoding.EncodeToString(digest[:])
}

func authorizeAddress(redirectURI string, state string, challenge string) string {
	query := url.Values{
		"client_id":     {os.Getenv(clientIDEnvironmentName)},
		"response_type": {"code"},
		"redirect_uri":  {redirectURI},
		"state":         {state},
	}
	if challenge != "" {
		query.Set("code_challenge", challenge)
		query.Set("code_challenge_method", "S256")
	}
	return identityAuthorizeURL + "?" + query.Encode()
}

func exchangeCode(ctx context.Context, client *http.Client, code string, redirectURI string, verifier string) (tokenResponse, error) {
	form := url.Values{
		"grant_type":    {"authorization_code"},
		"code":          {code},
		"redirect_uri":  {redirectURI},
		"client_id":     {os.Getenv(clientIDEnvironmentName)},
		"client_secret": {os.Getenv(clientSecretEnvironmentName)},
	}
	if verifier != "" {
		form.Set("code_verifier", verifier)
	}
	return requestToken(ctx, client, form)
}
```

**Redirect URL.** A server-side application registers **one exact HTTPS URL per environment, no wildcards**; the matching rules are not documented and no claim is made about them. A plain-HTTP loopback Redirect URL `http://127.0.0.1:<port>/<path>` on a **fixed registered port** is accepted, **verified by NSerio against a live instance; not documented by Relativity**, and is the command-line and desktop shape.

**On expiry.** With Allow Refresh Tokens enabled and PKCE used, the code holds a refresh token and sends `grant_type=refresh_token` in place of the code exchange: *inferred* from the standard OAuth shape, not tested; Relativity does not document a refresh token's lifetime, rotation or revocation, nor what disabling the client does to tokens already issued. **A refresh token is a secret**: hold it where the secret is held, never in a log. **Single sign-on:** sign-in happens on RelativityOne's own page, so the instance's single sign-on and two-factor rules apply unchanged (*inferred*). **The code stops collecting the password:** after this change nothing receives, stores or forwards the person's user name and password, and the code holds only tokens. Every call now runs as the person who signed in, so **the calling identity changes**. **Scope:** no scope set is prescribed for Code; `openid RelativityRequestOrigin UserInfo UserInfoAccess AuthenticationID RelativityWeb` works, **verified by NSerio against a live instance; not documented by Relativity**, as working and not as minimal.

**Variant 1 — a Go web server.** **Precondition: this variant is for a Go server that signs a person in to RelativityOne. If the code is a command-line tool or a desktop process, use variant 2.**

A server-side component creates `state` and the PKCE verifier, redirects to the authorize endpoint, receives the code at its registered HTTPS Redirect URL, exchanges it, keeps the tokens on the server and gives the browser **only its own session**; **the browser never holds anything from RelativityOne**. The two handlers use the standard library's `net/http`: `http.Redirect` replies to the request with a redirect to the authorize address.

```go
type WebSignIn struct {
	mutex       sync.Mutex
	pending     map[string]string
	client      *http.Client
	redirectURI string
	keepTokens  func(w http.ResponseWriter, r *http.Request, tokens tokenResponse)
}

func (signIn *WebSignIn) Login(w http.ResponseWriter, r *http.Request) {
	state, err := randomToken(stateByteCount)
	if err != nil {
		http.Error(w, "sign-in unavailable", http.StatusInternalServerError)
		return
	}
	verifier, err := randomToken(verifierByteCount)
	if err != nil {
		http.Error(w, "sign-in unavailable", http.StatusInternalServerError)
		return
	}
	signIn.mutex.Lock()
	signIn.pending[state] = verifier
	signIn.mutex.Unlock()
	http.SetCookie(w, &http.Cookie{
		Name:     signInCookieName,
		Value:    state,
		Path:     "/",
		MaxAge:   signInCookieMaxAgeSeconds,
		HttpOnly: true,
		Secure:   true,
		SameSite: http.SameSiteLaxMode,
	})
	http.Redirect(w, r, authorizeAddress(signIn.redirectURI, state, challengeOf(verifier)), http.StatusFound)
}

func (signIn *WebSignIn) Callback(w http.ResponseWriter, r *http.Request) {
	query := r.URL.Query()
	if query.Get("error") != "" {
		http.Error(w, "sign-in was not completed", http.StatusUnauthorized)
		return
	}
	state := query.Get("state")
	cookie, err := r.Cookie(signInCookieName)
	if err != nil || query.Get("code") == "" || subtle.ConstantTimeCompare([]byte(cookie.Value), []byte(state)) != 1 {
		http.Error(w, "unknown sign-in", http.StatusBadRequest)
		return
	}
	signIn.mutex.Lock()
	verifier, known := signIn.pending[state]
	delete(signIn.pending, state)
	signIn.mutex.Unlock()
	if !known {
		http.Error(w, "unknown sign-in", http.StatusBadRequest)
		return
	}
	http.SetCookie(w, &http.Cookie{Name: signInCookieName, Path: "/", MaxAge: -1})
	tokens, err := exchangeCode(r.Context(), signIn.client, query.Get("code"), signIn.redirectURI, verifier)
	if err != nil {
		http.Error(w, "sign-in failed", http.StatusBadGateway)
		return
	}
	signIn.keepTokens(w, r, tokens)
}
```

**`state` is bound to the browser session**: `Login` stores it in a short-lived, `HttpOnly`, `Secure`, `SameSite=Lax` cookie as well as in the server's pending table, and `Callback` fails (400) unless the cookie is present **and** equals the `state` on the query, so a callback that arrives without the matching cookie gets no exchange. `Callback` also **handles the `error` parameter** (401, no exchange) and **an empty `code`** (400). `state` is consumed once: the entry is deleted before the exchange and the cookie is cleared, so a replayed or unknown `state` gets no exchange. `keepTokens` is yours: store the tokens against the visitor's session on the server and set a session cookie that holds **only a session identifier**. The refresh call, if used, goes to the same token endpoint with `grant_type=refresh_token` (*inferred*). **The calling identity changes** to the person who signed in.

**Variant 2 — a command-line tool or a desktop process.** **Precondition: this variant is for a Go command-line tool or a desktop process that signs a person in to RelativityOne. If the code is a server, use variant 1.**

The process starts a listener from the standard library bound to `127.0.0.1` only, on the **fixed port the administrator registered** (the exact URL is registered, so the port is fixed, chosen by the administrator and not at runtime), for **one request**; it **prints the authorize address** (so a machine with no browser, an SSH session or a failed launch has the manual route) and, if you want, opens the system browser yourself; reads `code` and `state` from the one request, answers with a short page, closes the listener, checks `state` and exchanges the code.

```go
func SignInFromCommandLine(ctx context.Context, client *http.Client, listenAddress string, redirectURI string) (tokenResponse, error) {
	var tokens tokenResponse
	redirect, err := url.Parse(redirectURI)
	if err != nil {
		return tokens, err
	}
	host, _, err := net.SplitHostPort(listenAddress)
	if err != nil {
		return tokens, err
	}
	if host != "127.0.0.1" && host != "::1" {
		return tokens, errors.New("the sign-in listener must be bound to the loopback address")
	}
	ctx, cancel := context.WithTimeout(ctx, signInTimeoutSeconds*time.Second)
	defer cancel()
	state, err := randomToken(stateByteCount)
	if err != nil {
		return tokens, err
	}
	verifier, err := randomToken(verifierByteCount)
	if err != nil {
		return tokens, err
	}
	listener, err := net.Listen("tcp", listenAddress)
	if err != nil {
		return tokens, fmt.Errorf("the registered port is not available: %w", err)
	}
	type callback struct{ code, state, failure string }
	received := make(chan callback, 1)
	server := &http.Server{
		ReadHeaderTimeout: serverHeaderTimeoutSeconds * time.Second,
		Handler: http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
			if r.URL.Path != redirect.Path {
				http.NotFound(w, r)
				return
			}
			query := r.URL.Query()
			select {
			case received <- callback{code: query.Get("code"), state: query.Get("state"), failure: query.Get("error")}:
			default:
			}
			fmt.Fprintln(w, "Sign-in received. You can close this window.")
		}),
	}
	go func() { _ = server.Serve(listener) }()
	defer server.Shutdown(context.Background())
	fmt.Println("Open this address in a browser to sign in:", authorizeAddress(redirectURI, state, challengeOf(verifier)))
	select {
	case result := <-received:
		if result.failure != "" || result.code == "" {
			return tokens, errors.New("sign-in was not completed")
		}
		if subtle.ConstantTimeCompare([]byte(result.state), []byte(state)) != 1 {
			return tokens, errors.New("the sign-in answer does not match the request")
		}
		return exchangeCode(ctx, client, result.code, redirectURI, verifier)
	case <-ctx.Done():
		return tokens, ctx.Err()
	}
}
```

**The listener is checked and bounded in the code**: it **refuses a `listenAddress` whose host is not `127.0.0.1` or `::1`** (loopback only); its handler **answers 404 to every path other than the registered redirect path** (so a stray request cannot consume the one-shot channel); the whole sign-in has **its own deadline** (`signInTimeoutSeconds`, 300 seconds, on top of the caller's context), after which it returns the context error; a callback with an `error` parameter or an empty `code` is an error, not an exchange. **Busy port:** the port is the registered one and is **not** replaced by another (a different port is a different Redirect URL, which Relativity would not match); if `net.Listen` fails because the port is taken, the function returns the error `the registered port is not available` and the tool must report it, not retry on another port. Call it with `listenAddress` `127.0.0.1:<the registered port>` and `redirectURI` `http://127.0.0.1:<the registered port>/<path>`. **Where the tokens are kept** is the program's decision: in memory for the run, or, for a refresh token, in a file whose permissions you restrict (**a distributed command-line tool carries its client secret in a form anyone who holds the distribution can extract**). Every call now runs as the person who signed in.

Verification: **Verification note.** **A token has a finite lifetime.** **The calling identity changes.** A `Basic` value built in another function and handed here is read at its own site, not at this one.

OAuth2 Client: Create or confirm this OAuth2 Client in RelativityOne (NSerio's observation: the menu path is Home > Authentication > OAuth2 Client Manager, as Relativity's REST API authentication page names it). This tool never connects to RelativityOne and never creates or changes the OAuth2 Client; the settings below are guidance.
- Name: a descriptive name identifying the application it serves.
- Flow Grant Type: Code
- Redirect URLs: Relativity documentation: each URL includes the http or https protocol prefix. The exact callback URL is registered by the Relativity administrator. Server-side web applications, NSerio's recommendation: one exact HTTPS URL per environment, with no wildcards. Desktop and CLI applications: a plain-HTTP loopback URL, `http://127.0.0.1:<port>/<path>`, on a fixed port because the URL is registered in advance and the port cannot be chosen at runtime (verified by NSerio against a live instance; not documented by Relativity).
- Access Token Lifetime: 60 minutes, stated in minutes because the field is in minutes. Relativity documentation: tokens for this flow must have a short lifetime, and the recommended value matches the Identity Server default.
- Allow refresh tokens: NSerio's recommendation: leave it off unless the application must keep a user session active beyond the access token lifetime without asking the user to sign in again. If it is on, the refresh tokens are stored as secrets, per Secret Management (Part B of the Migration Documentation).
- Enabled: on.
<!-- generated:end GO-002 authorization-code -->

### GO-003 · HeaderLiteralBasicScheme

<a id="go-003-migration"></a>

Change the `Basic` value in the `http.Header` literal to a `Bearer` value, and use the canonical key `Authorization`: a literal key is not canonicalized, so a lower-case key is not found by `Header.Get`. A header built once at start-up holds a token that expires, so build it for each request or use a transport that sets the header on a clone of each request.

#### Client Credentials

<!-- generated:begin GO-003 client-credentials -->
**Confirming the flow.** When no signal in a file decides the flow, the tool assigns Client Credentials, the default, with this note: "Who supplies the credential at run time? If a person types it or signs in, the integration acts for that person: use Authorization Code. If it comes from configuration, settings, an environment variable or a secret store: use Client Credentials. No signal in this file showed which applies, so Client Credentials, the default, is recommended. Use Authorization Code instead if the integration acts on behalf of a signed-in user, or if your RelativityOne instance already has a Code-flow OAuth2 Client for this integration." A fixed-credential signal assigns it with this note: "A fixed-credential signal in this file points to Client Credentials, but how often that kind of signal is right has not been measured. Use Authorization Code instead if the integration acts on behalf of a signed-in user, or if your RelativityOne instance already has a Code-flow OAuth2 Client for this integration." See [Confirming the flow](#confirming-the-flow) and the "Which flow" paragraph of GO-003 in [Part A](part-a-go.md#go-003).

Source form: **Applies to:** every Go version; an `http.Header` literal.

```go
encoded := base64.StdEncoding.EncodeToString([]byte(userName + ":" + secret))
return http.Header{"Authorization": {"Basic " + encoded}}
```

Under token authentication: **First, the destination:** if this Relativity instance is not RelativityOne — including a Relativity Server instance, whose own documentation governs — this finding is informational and the rest of this template does not apply.

```go
return http.Header{
	"Authorization": {"Bearer " + accessToken},
	csrfHeaderName:  {csrfHeaderValue},
}
```

Use the canonical key `Authorization`: a literal key is **not** canonicalized, so a lower-case key is not found by `Header.Get`.

Credentials: **Where the credentials come from.** The source form establishes that the code sends Basic, not that the destination is RelativityOne. If it is not RelativityOne, the migration is that service's own token mechanism, this finding is informational for this tool, and the reader stops here. If it is RelativityOne, the token comes from the token-acquisition part that follows, and the user name and password are replaced by the OAuth2 Client's client ID and secret, kept as Secret Management (Part B of the Migration Documentation) describes and never in source.

**Where the token comes from.** Create an OAuth2 Client with Flow Grant Type set to Client Credentials (part 3 lists its settings), then send a form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=client_credentials`, `client_id` and `client_secret`. The token endpoint is **documented** for this grant. The token is then sent as `Authorization: Bearer <token>`, with `X-CSRF-Header: -` on REST and Kepler calls. The scope depends on what the integration needs and **no single scope is required**: Relativity's documentation shows `SystemUserInfo` and `UserInfoAccess` and no page says which is correct or what either grants; use the value the API you call documents, and confirm it. Every block below uses only the standard library and needs go1.13 or later.

**Shared declarations.** Every block of this text, and of the use-site forms above, uses these constants and helpers; `HOST` is the RelativityOne host.

```go
const (
	identityTokenURL            = "https://HOST/Relativity/Identity/connect/token"
	identityAuthorizeURL        = "https://HOST/Relativity/Identity/connect/authorize"
	clientIDEnvironmentName     = "RELATIVITY_CLIENT_ID"
	clientSecretEnvironmentName = "RELATIVITY_CLIENT_SECRET"
	expiryMarginSeconds         = 60
	csrfHeaderName              = "X-CSRF-Header"
	csrfHeaderValue             = "-"
	stateByteCount              = 16
	verifierByteCount           = 32
	serverHeaderTimeoutSeconds  = 10
	signInCookieName            = "relativity_sign_in"
	signInCookieMaxAgeSeconds   = 600
	signInTimeoutSeconds        = 300
)

type tokenResponse struct {
	AccessToken  string `json:"access_token"`
	RefreshToken string `json:"refresh_token"`
	ExpiresIn    int    `json:"expires_in"`
}

func requestToken(ctx context.Context, client *http.Client, form url.Values) (tokenResponse, error) {
	var payload tokenResponse
	request, err := http.NewRequestWithContext(ctx, http.MethodPost, identityTokenURL, strings.NewReader(form.Encode()))
	if err != nil {
		return payload, err
	}
	request.Header.Set("Content-Type", "application/x-www-form-urlencoded")
	response, err := client.Do(request)
	if err != nil {
		return payload, err
	}
	defer response.Body.Close()
	if response.StatusCode != http.StatusOK {
		return payload, fmt.Errorf("token endpoint answered %d", response.StatusCode)
	}
	err = json.NewDecoder(response.Body).Decode(&payload)
	return payload, err
}
```

The token request is a form-encoded `POST`. **Where the client secret travels in the token request** (the form body, as here, or HTTP Basic client authentication) **and the field names of the token response are not documented by Relativity**: the form body and `tokenResponse` are the standard OAuth shape and an NSerio recommendation, **not verified** against RelativityOne. **Do not** send the client secret with `SetBasicAuth` and **do not** put it in the URL.

**Variant: a Go process acting as itself.** **Precondition: this variant is for a Go job, daemon or service that does not act for a signed-in person.**

```go
type TokenSource struct {
	mutex     sync.Mutex
	client    *http.Client
	token     string
	expiresAt time.Time
}

func (source *TokenSource) Token(ctx context.Context) (string, error) {
	source.mutex.Lock()
	defer source.mutex.Unlock()
	if source.token == "" || time.Until(source.expiresAt) < expiryMarginSeconds*time.Second {
		form := url.Values{
			"grant_type":    {"client_credentials"},
			"client_id":     {os.Getenv(clientIDEnvironmentName)},
			"client_secret": {os.Getenv(clientSecretEnvironmentName)},
		}
		payload, err := requestToken(ctx, source.client, form)
		if err != nil {
			return "", err
		}
		source.token = payload.AccessToken
		source.expiresAt = time.Now().Add(time.Duration(payload.ExpiresIn) * time.Second)
	}
	return source.token, nil
}
```

**Cache the token and re-acquire it on expiry — do not refresh.** Client Credentials does not offer refresh tokens: **verified by NSerio against a live instance; not documented by Relativity** — the Allow Refresh Tokens control does not appear for this grant in the RelativityOne user interface. The recommended Access Token Lifetime is 60 minutes. **The client secret** belongs in the process environment or a secret store, as Secret Management (Part B of the Migration Documentation) describes, **never in source, in a tracked `.env` file or in a log**: the block reads it with `os.Getenv`, which returns an empty string when the variable is not present, so an unset variable is an empty secret and the token request fails, which the code reports. A reset invalidates the previous secret immediately, so plan the cut-over; a second client created before the reset is an inferred way to do it without a gap. **Before deploying, confirm the calling identity:** every call now runs as the OAuth2 Client's Context User, not as the user in the replaced credential, and a non-administrator Context User is NSerio's recommendation. Trusted IPs restrict where a token can be requested, not a token already issued. The statements that Client Credentials needs a client secret and that the secret is displayed once are carried from the other languages' guidance and are not verified for this text.

**Variant: a Go web service that does not act for a signed-in user.** **Precondition: this variant is for a Go server that calls RelativityOne with no person signed in to it, so no person's identity exists to preserve. If a person signs in, use Authorization Code, variant 1.**

Use the block above in the server process; **the server must authenticate and authorize its own callers, and the OAuth2 Client's Context User must hold the minimum permissions the operation needs. Without both, the result is an unauthenticated relay into RelativityOne**: every caller reaching it acts with the Context User's full rights, which is strictly worse than the starting point. The handler returns results to its caller, **never the token** and never the secret.

Verification: **Verification note.** **A token has a finite lifetime.** A header built once at start-up expires, so build it per request or use a transport that sets the header on a clone of each request. **The calling identity changes.**

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
<!-- generated:end GO-003 client-credentials -->

#### Authorization Code

<!-- generated:begin GO-003 authorization-code -->
**Confirming the flow.** An interactive-user signal in a file assigns Authorization Code, with this note: "An interactive-user signal in this file points to Authorization Code. Confirm the flow: use Client Credentials instead if the integration runs with credentials fixed for the integration and no signed-in user." Your user profile can also make Authorization Code the default; see Changing the default in [Confirming the flow](#confirming-the-flow). See [Confirming the flow](#confirming-the-flow) and the "Which flow" paragraph of GO-003 in [Part A](part-a-go.md#go-003).

Source form: **Applies to:** every Go version; an `http.Header` literal.

```go
encoded := base64.StdEncoding.EncodeToString([]byte(userName + ":" + secret))
return http.Header{"Authorization": {"Basic " + encoded}}
```

Under token authentication: **First, the destination:** if this Relativity instance is not RelativityOne — including a Relativity Server instance, whose own documentation governs — this finding is informational and the rest of this template does not apply.

```go
return http.Header{
	"Authorization": {"Bearer " + accessToken},
	csrfHeaderName:  {csrfHeaderValue},
}
```

Use the canonical key `Authorization`: a literal key is **not** canonicalized, so a lower-case key is not found by `Header.Get`.

Credentials: **Where the credentials come from.** The source form establishes that the code sends Basic, not that the destination is RelativityOne. If it is not RelativityOne, the migration is that service's own token mechanism, this finding is informational for this tool, and the reader stops here. If it is RelativityOne, the token comes from the token-acquisition part that follows, and the user name and password are replaced by the OAuth2 Client's client ID and secret, kept as Secret Management (Part B of the Migration Documentation) describes and never in source.

**Common to both variants.** The person signs in on RelativityOne's own page and the code never holds their password. The text has a common part and two variants by application type, and each variant opens with its own precondition, so that the reader can discard the others by reading one sentence each. Every block uses only the standard library and needs go1.13 or later; the shared declarations are those of the Client Credentials text and are repeated here.

```go
const (
	identityTokenURL            = "https://HOST/Relativity/Identity/connect/token"
	identityAuthorizeURL        = "https://HOST/Relativity/Identity/connect/authorize"
	clientIDEnvironmentName     = "RELATIVITY_CLIENT_ID"
	clientSecretEnvironmentName = "RELATIVITY_CLIENT_SECRET"
	expiryMarginSeconds         = 60
	csrfHeaderName              = "X-CSRF-Header"
	csrfHeaderValue             = "-"
	stateByteCount              = 16
	verifierByteCount           = 32
	serverHeaderTimeoutSeconds  = 10
	signInCookieName            = "relativity_sign_in"
	signInCookieMaxAgeSeconds   = 600
	signInTimeoutSeconds        = 300
)

type tokenResponse struct {
	AccessToken  string `json:"access_token"`
	RefreshToken string `json:"refresh_token"`
	ExpiresIn    int    `json:"expires_in"`
}

func requestToken(ctx context.Context, client *http.Client, form url.Values) (tokenResponse, error) {
	var payload tokenResponse
	request, err := http.NewRequestWithContext(ctx, http.MethodPost, identityTokenURL, strings.NewReader(form.Encode()))
	if err != nil {
		return payload, err
	}
	request.Header.Set("Content-Type", "application/x-www-form-urlencoded")
	response, err := client.Do(request)
	if err != nil {
		return payload, err
	}
	defer response.Body.Close()
	if response.StatusCode != http.StatusOK {
		return payload, fmt.Errorf("token endpoint answered %d", response.StatusCode)
	}
	err = json.NewDecoder(response.Body).Decode(&payload)
	return payload, err
}
```

**Sign in.** Replace the code that collected the person's credentials with a redirect to the authorize endpoint `<host>/Relativity/Identity/connect/authorize`, which is **verified by NSerio against a live instance; not documented by Relativity** and leads to RelativityOne's login page. The request carries the standard authorization-code parameters — `client_id`, `response_type=code`, the registered `redirect_uri`, `state` checked on return, and `code_challenge` with `code_challenge_method=S256` when PKCE is used; these are standard OAuth and Relativity documents none of them.

**PKCE.** Relativity's own documentation requires PKCE to obtain a refresh token; that a Code request that does not ask for a refresh token works without PKCE is **verified by NSerio against a live instance; not documented by Relativity**. **PKCE is not limited to clients that use refresh tokens**: NSerio recommends `S256` for every Authorization Code client (NSerio recommendation) and does not claim Relativity accepts any particular method. **Whether a client with refresh tokens disabled accepts a PKCE request (a `code_challenge`) is unverified**: test it before the block is relied on for such a client. **`state`** is generated, stored against the session and compared on return; `nonce` is not used, and Relativity does not document whether it enforces either. The authorize and token endpoints and the PKCE and `state` behaviour above are **verified by NSerio against a live instance; not documented by Relativity**. The block draws the verifier and `state` from `crypto/rand`, whose `Read` fills the buffer with cryptographically secure random bytes (the signature still returns an error, which the block checks), and computes the challenge with `crypto/sha256` and `base64.RawURLEncoding`, the unpadded URL-safe encoding; a 32-byte value encodes to a 43-character verifier.

**The client secret is required.** Code does not work without a client secret: tested and rejected, **verified by NSerio against a live instance; not documented by Relativity**, which confirms Relativity's own statement. **Any secret placed in a distributed binary is extractable by whoever holds the binary**: a Go program compiled for people other than its owner carries its client secret in a form anyone who holds the file can extract, and Relativity documents no recommendation for that case; use a short Access Token Lifetime, Trusted IPs where the outbound addresses are stable and one OAuth2 Client per distribution as compensating guidance (NSerio recommendation). Keep the secret as Secret Management (Part B of the Migration Documentation) describes, never in source or in a log.

**Exchange the code.** A form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=authorization_code`, the code, the same `redirect_uri`, the client ID and the secret, plus the `code_verifier` when PKCE is used.

```go
func randomToken(byteCount int) (string, error) {
	buffer := make([]byte, byteCount)
	if _, err := rand.Read(buffer); err != nil {
		return "", err
	}
	return base64.RawURLEncoding.EncodeToString(buffer), nil
}

func challengeOf(verifier string) string {
	digest := sha256.Sum256([]byte(verifier))
	return base64.RawURLEncoding.EncodeToString(digest[:])
}

func authorizeAddress(redirectURI string, state string, challenge string) string {
	query := url.Values{
		"client_id":     {os.Getenv(clientIDEnvironmentName)},
		"response_type": {"code"},
		"redirect_uri":  {redirectURI},
		"state":         {state},
	}
	if challenge != "" {
		query.Set("code_challenge", challenge)
		query.Set("code_challenge_method", "S256")
	}
	return identityAuthorizeURL + "?" + query.Encode()
}

func exchangeCode(ctx context.Context, client *http.Client, code string, redirectURI string, verifier string) (tokenResponse, error) {
	form := url.Values{
		"grant_type":    {"authorization_code"},
		"code":          {code},
		"redirect_uri":  {redirectURI},
		"client_id":     {os.Getenv(clientIDEnvironmentName)},
		"client_secret": {os.Getenv(clientSecretEnvironmentName)},
	}
	if verifier != "" {
		form.Set("code_verifier", verifier)
	}
	return requestToken(ctx, client, form)
}
```

**Redirect URL.** A server-side application registers **one exact HTTPS URL per environment, no wildcards**; the matching rules are not documented and no claim is made about them. A plain-HTTP loopback Redirect URL `http://127.0.0.1:<port>/<path>` on a **fixed registered port** is accepted, **verified by NSerio against a live instance; not documented by Relativity**, and is the command-line and desktop shape.

**On expiry.** With Allow Refresh Tokens enabled and PKCE used, the code holds a refresh token and sends `grant_type=refresh_token` in place of the code exchange: *inferred* from the standard OAuth shape, not tested; Relativity does not document a refresh token's lifetime, rotation or revocation, nor what disabling the client does to tokens already issued. **A refresh token is a secret**: hold it where the secret is held, never in a log. **Single sign-on:** sign-in happens on RelativityOne's own page, so the instance's single sign-on and two-factor rules apply unchanged (*inferred*). **The code stops collecting the password:** after this change nothing receives, stores or forwards the person's user name and password, and the code holds only tokens. Every call now runs as the person who signed in, so **the calling identity changes**. **Scope:** no scope set is prescribed for Code; `openid RelativityRequestOrigin UserInfo UserInfoAccess AuthenticationID RelativityWeb` works, **verified by NSerio against a live instance; not documented by Relativity**, as working and not as minimal.

**Variant 1 — a Go web server.** **Precondition: this variant is for a Go server that signs a person in to RelativityOne. If the code is a command-line tool or a desktop process, use variant 2.**

A server-side component creates `state` and the PKCE verifier, redirects to the authorize endpoint, receives the code at its registered HTTPS Redirect URL, exchanges it, keeps the tokens on the server and gives the browser **only its own session**; **the browser never holds anything from RelativityOne**. The two handlers use the standard library's `net/http`: `http.Redirect` replies to the request with a redirect to the authorize address.

```go
type WebSignIn struct {
	mutex       sync.Mutex
	pending     map[string]string
	client      *http.Client
	redirectURI string
	keepTokens  func(w http.ResponseWriter, r *http.Request, tokens tokenResponse)
}

func (signIn *WebSignIn) Login(w http.ResponseWriter, r *http.Request) {
	state, err := randomToken(stateByteCount)
	if err != nil {
		http.Error(w, "sign-in unavailable", http.StatusInternalServerError)
		return
	}
	verifier, err := randomToken(verifierByteCount)
	if err != nil {
		http.Error(w, "sign-in unavailable", http.StatusInternalServerError)
		return
	}
	signIn.mutex.Lock()
	signIn.pending[state] = verifier
	signIn.mutex.Unlock()
	http.SetCookie(w, &http.Cookie{
		Name:     signInCookieName,
		Value:    state,
		Path:     "/",
		MaxAge:   signInCookieMaxAgeSeconds,
		HttpOnly: true,
		Secure:   true,
		SameSite: http.SameSiteLaxMode,
	})
	http.Redirect(w, r, authorizeAddress(signIn.redirectURI, state, challengeOf(verifier)), http.StatusFound)
}

func (signIn *WebSignIn) Callback(w http.ResponseWriter, r *http.Request) {
	query := r.URL.Query()
	if query.Get("error") != "" {
		http.Error(w, "sign-in was not completed", http.StatusUnauthorized)
		return
	}
	state := query.Get("state")
	cookie, err := r.Cookie(signInCookieName)
	if err != nil || query.Get("code") == "" || subtle.ConstantTimeCompare([]byte(cookie.Value), []byte(state)) != 1 {
		http.Error(w, "unknown sign-in", http.StatusBadRequest)
		return
	}
	signIn.mutex.Lock()
	verifier, known := signIn.pending[state]
	delete(signIn.pending, state)
	signIn.mutex.Unlock()
	if !known {
		http.Error(w, "unknown sign-in", http.StatusBadRequest)
		return
	}
	http.SetCookie(w, &http.Cookie{Name: signInCookieName, Path: "/", MaxAge: -1})
	tokens, err := exchangeCode(r.Context(), signIn.client, query.Get("code"), signIn.redirectURI, verifier)
	if err != nil {
		http.Error(w, "sign-in failed", http.StatusBadGateway)
		return
	}
	signIn.keepTokens(w, r, tokens)
}
```

**`state` is bound to the browser session**: `Login` stores it in a short-lived, `HttpOnly`, `Secure`, `SameSite=Lax` cookie as well as in the server's pending table, and `Callback` fails (400) unless the cookie is present **and** equals the `state` on the query, so a callback that arrives without the matching cookie gets no exchange. `Callback` also **handles the `error` parameter** (401, no exchange) and **an empty `code`** (400). `state` is consumed once: the entry is deleted before the exchange and the cookie is cleared, so a replayed or unknown `state` gets no exchange. `keepTokens` is yours: store the tokens against the visitor's session on the server and set a session cookie that holds **only a session identifier**. The refresh call, if used, goes to the same token endpoint with `grant_type=refresh_token` (*inferred*). **The calling identity changes** to the person who signed in.

**Variant 2 — a command-line tool or a desktop process.** **Precondition: this variant is for a Go command-line tool or a desktop process that signs a person in to RelativityOne. If the code is a server, use variant 1.**

The process starts a listener from the standard library bound to `127.0.0.1` only, on the **fixed port the administrator registered** (the exact URL is registered, so the port is fixed, chosen by the administrator and not at runtime), for **one request**; it **prints the authorize address** (so a machine with no browser, an SSH session or a failed launch has the manual route) and, if you want, opens the system browser yourself; reads `code` and `state` from the one request, answers with a short page, closes the listener, checks `state` and exchanges the code.

```go
func SignInFromCommandLine(ctx context.Context, client *http.Client, listenAddress string, redirectURI string) (tokenResponse, error) {
	var tokens tokenResponse
	redirect, err := url.Parse(redirectURI)
	if err != nil {
		return tokens, err
	}
	host, _, err := net.SplitHostPort(listenAddress)
	if err != nil {
		return tokens, err
	}
	if host != "127.0.0.1" && host != "::1" {
		return tokens, errors.New("the sign-in listener must be bound to the loopback address")
	}
	ctx, cancel := context.WithTimeout(ctx, signInTimeoutSeconds*time.Second)
	defer cancel()
	state, err := randomToken(stateByteCount)
	if err != nil {
		return tokens, err
	}
	verifier, err := randomToken(verifierByteCount)
	if err != nil {
		return tokens, err
	}
	listener, err := net.Listen("tcp", listenAddress)
	if err != nil {
		return tokens, fmt.Errorf("the registered port is not available: %w", err)
	}
	type callback struct{ code, state, failure string }
	received := make(chan callback, 1)
	server := &http.Server{
		ReadHeaderTimeout: serverHeaderTimeoutSeconds * time.Second,
		Handler: http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
			if r.URL.Path != redirect.Path {
				http.NotFound(w, r)
				return
			}
			query := r.URL.Query()
			select {
			case received <- callback{code: query.Get("code"), state: query.Get("state"), failure: query.Get("error")}:
			default:
			}
			fmt.Fprintln(w, "Sign-in received. You can close this window.")
		}),
	}
	go func() { _ = server.Serve(listener) }()
	defer server.Shutdown(context.Background())
	fmt.Println("Open this address in a browser to sign in:", authorizeAddress(redirectURI, state, challengeOf(verifier)))
	select {
	case result := <-received:
		if result.failure != "" || result.code == "" {
			return tokens, errors.New("sign-in was not completed")
		}
		if subtle.ConstantTimeCompare([]byte(result.state), []byte(state)) != 1 {
			return tokens, errors.New("the sign-in answer does not match the request")
		}
		return exchangeCode(ctx, client, result.code, redirectURI, verifier)
	case <-ctx.Done():
		return tokens, ctx.Err()
	}
}
```

**The listener is checked and bounded in the code**: it **refuses a `listenAddress` whose host is not `127.0.0.1` or `::1`** (loopback only); its handler **answers 404 to every path other than the registered redirect path** (so a stray request cannot consume the one-shot channel); the whole sign-in has **its own deadline** (`signInTimeoutSeconds`, 300 seconds, on top of the caller's context), after which it returns the context error; a callback with an `error` parameter or an empty `code` is an error, not an exchange. **Busy port:** the port is the registered one and is **not** replaced by another (a different port is a different Redirect URL, which Relativity would not match); if `net.Listen` fails because the port is taken, the function returns the error `the registered port is not available` and the tool must report it, not retry on another port. Call it with `listenAddress` `127.0.0.1:<the registered port>` and `redirectURI` `http://127.0.0.1:<the registered port>/<path>`. **Where the tokens are kept** is the program's decision: in memory for the run, or, for a refresh token, in a file whose permissions you restrict (**a distributed command-line tool carries its client secret in a form anyone who holds the distribution can extract**). Every call now runs as the person who signed in.

Verification: **Verification note.** **A token has a finite lifetime.** A header built once at start-up expires, so build it per request or use a transport that sets the header on a clone of each request. **The calling identity changes.**

OAuth2 Client: Create or confirm this OAuth2 Client in RelativityOne (NSerio's observation: the menu path is Home > Authentication > OAuth2 Client Manager, as Relativity's REST API authentication page names it). This tool never connects to RelativityOne and never creates or changes the OAuth2 Client; the settings below are guidance.
- Name: a descriptive name identifying the application it serves.
- Flow Grant Type: Code
- Redirect URLs: Relativity documentation: each URL includes the http or https protocol prefix. The exact callback URL is registered by the Relativity administrator. Server-side web applications, NSerio's recommendation: one exact HTTPS URL per environment, with no wildcards. Desktop and CLI applications: a plain-HTTP loopback URL, `http://127.0.0.1:<port>/<path>`, on a fixed port because the URL is registered in advance and the port cannot be chosen at runtime (verified by NSerio against a live instance; not documented by Relativity).
- Access Token Lifetime: 60 minutes, stated in minutes because the field is in minutes. Relativity documentation: tokens for this flow must have a short lifetime, and the recommended value matches the Identity Server default.
- Allow refresh tokens: NSerio's recommendation: leave it off unless the application must keep a user session active beyond the access token lifetime without asking the user to sign in again. If it is on, the refresh tokens are stored as secrets, per Secret Management (Part B of the Migration Documentation).
- Enabled: on.
<!-- generated:end GO-003 authorization-code -->

### GO-004 · MapLiteralBasicScheme

<a id="go-004-migration"></a>

Keep the key `Authorization` and write the scheme as the literal `Bearer`. **The tool did not establish that this map is an HTTP header object**: confirm where the map is consumed; if it is not a header for a request to RelativityOne, the finding is informational and the migration is that consumer's own.

#### Client Credentials

<!-- generated:begin GO-004 client-credentials -->
**Confirming the flow.** When no signal in a file decides the flow, the tool assigns Client Credentials, the default, with this note: "Who supplies the credential at run time? If a person types it or signs in, the integration acts for that person: use Authorization Code. If it comes from configuration, settings, an environment variable or a secret store: use Client Credentials. No signal in this file showed which applies, so Client Credentials, the default, is recommended. Use Authorization Code instead if the integration acts on behalf of a signed-in user, or if your RelativityOne instance already has a Code-flow OAuth2 Client for this integration." A fixed-credential signal assigns it with this note: "A fixed-credential signal in this file points to Client Credentials, but how often that kind of signal is right has not been measured. Use Authorization Code instead if the integration acts on behalf of a signed-in user, or if your RelativityOne instance already has a Code-flow OAuth2 Client for this integration." See [Confirming the flow](#confirming-the-flow) and the "Which flow" paragraph of GO-004 in [Part A](part-a-go.md#go-004).

Source form: **Applies to:** every Go version; an unnamed map literal that carries the header.

```go
encoded := base64.StdEncoding.EncodeToString([]byte(userName + ":" + secret))
return map[string]string{"Authorization": "Basic " + encoded}
```

Under token authentication: **First, the destination:** if this Relativity instance is not RelativityOne — including a Relativity Server instance, whose own documentation governs — this finding is informational and the rest of this template does not apply.

```go
return map[string]string{
	"Authorization": "Bearer " + accessToken,
	csrfHeaderName:  csrfHeaderValue,
}
```

Keep the key `Authorization` and write the scheme as the literal `Bearer`.

Credentials: **Where the credentials come from.** The source form establishes that the code sends Basic, not that the destination is RelativityOne. If it is not RelativityOne, the migration is that service's own token mechanism, this finding is informational for this tool, and the reader stops here. If it is RelativityOne, the token comes from the token-acquisition part that follows, and the user name and password are replaced by the OAuth2 Client's client ID and secret, kept as Secret Management (Part B of the Migration Documentation) describes and never in source.

**Where the token comes from.** Create an OAuth2 Client with Flow Grant Type set to Client Credentials (part 3 lists its settings), then send a form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=client_credentials`, `client_id` and `client_secret`. The token endpoint is **documented** for this grant. The token is then sent as `Authorization: Bearer <token>`, with `X-CSRF-Header: -` on REST and Kepler calls. The scope depends on what the integration needs and **no single scope is required**: Relativity's documentation shows `SystemUserInfo` and `UserInfoAccess` and no page says which is correct or what either grants; use the value the API you call documents, and confirm it. Every block below uses only the standard library and needs go1.13 or later.

**Shared declarations.** Every block of this text, and of the use-site forms above, uses these constants and helpers; `HOST` is the RelativityOne host.

```go
const (
	identityTokenURL            = "https://HOST/Relativity/Identity/connect/token"
	identityAuthorizeURL        = "https://HOST/Relativity/Identity/connect/authorize"
	clientIDEnvironmentName     = "RELATIVITY_CLIENT_ID"
	clientSecretEnvironmentName = "RELATIVITY_CLIENT_SECRET"
	expiryMarginSeconds         = 60
	csrfHeaderName              = "X-CSRF-Header"
	csrfHeaderValue             = "-"
	stateByteCount              = 16
	verifierByteCount           = 32
	serverHeaderTimeoutSeconds  = 10
	signInCookieName            = "relativity_sign_in"
	signInCookieMaxAgeSeconds   = 600
	signInTimeoutSeconds        = 300
)

type tokenResponse struct {
	AccessToken  string `json:"access_token"`
	RefreshToken string `json:"refresh_token"`
	ExpiresIn    int    `json:"expires_in"`
}

func requestToken(ctx context.Context, client *http.Client, form url.Values) (tokenResponse, error) {
	var payload tokenResponse
	request, err := http.NewRequestWithContext(ctx, http.MethodPost, identityTokenURL, strings.NewReader(form.Encode()))
	if err != nil {
		return payload, err
	}
	request.Header.Set("Content-Type", "application/x-www-form-urlencoded")
	response, err := client.Do(request)
	if err != nil {
		return payload, err
	}
	defer response.Body.Close()
	if response.StatusCode != http.StatusOK {
		return payload, fmt.Errorf("token endpoint answered %d", response.StatusCode)
	}
	err = json.NewDecoder(response.Body).Decode(&payload)
	return payload, err
}
```

The token request is a form-encoded `POST`. **Where the client secret travels in the token request** (the form body, as here, or HTTP Basic client authentication) **and the field names of the token response are not documented by Relativity**: the form body and `tokenResponse` are the standard OAuth shape and an NSerio recommendation, **not verified** against RelativityOne. **Do not** send the client secret with `SetBasicAuth` and **do not** put it in the URL.

**Variant: a Go process acting as itself.** **Precondition: this variant is for a Go job, daemon or service that does not act for a signed-in person.**

```go
type TokenSource struct {
	mutex     sync.Mutex
	client    *http.Client
	token     string
	expiresAt time.Time
}

func (source *TokenSource) Token(ctx context.Context) (string, error) {
	source.mutex.Lock()
	defer source.mutex.Unlock()
	if source.token == "" || time.Until(source.expiresAt) < expiryMarginSeconds*time.Second {
		form := url.Values{
			"grant_type":    {"client_credentials"},
			"client_id":     {os.Getenv(clientIDEnvironmentName)},
			"client_secret": {os.Getenv(clientSecretEnvironmentName)},
		}
		payload, err := requestToken(ctx, source.client, form)
		if err != nil {
			return "", err
		}
		source.token = payload.AccessToken
		source.expiresAt = time.Now().Add(time.Duration(payload.ExpiresIn) * time.Second)
	}
	return source.token, nil
}
```

**Cache the token and re-acquire it on expiry — do not refresh.** Client Credentials does not offer refresh tokens: **verified by NSerio against a live instance; not documented by Relativity** — the Allow Refresh Tokens control does not appear for this grant in the RelativityOne user interface. The recommended Access Token Lifetime is 60 minutes. **The client secret** belongs in the process environment or a secret store, as Secret Management (Part B of the Migration Documentation) describes, **never in source, in a tracked `.env` file or in a log**: the block reads it with `os.Getenv`, which returns an empty string when the variable is not present, so an unset variable is an empty secret and the token request fails, which the code reports. A reset invalidates the previous secret immediately, so plan the cut-over; a second client created before the reset is an inferred way to do it without a gap. **Before deploying, confirm the calling identity:** every call now runs as the OAuth2 Client's Context User, not as the user in the replaced credential, and a non-administrator Context User is NSerio's recommendation. Trusted IPs restrict where a token can be requested, not a token already issued. The statements that Client Credentials needs a client secret and that the secret is displayed once are carried from the other languages' guidance and are not verified for this text.

**Variant: a Go web service that does not act for a signed-in user.** **Precondition: this variant is for a Go server that calls RelativityOne with no person signed in to it, so no person's identity exists to preserve. If a person signs in, use Authorization Code, variant 1.**

Use the block above in the server process; **the server must authenticate and authorize its own callers, and the OAuth2 Client's Context User must hold the minimum permissions the operation needs. Without both, the result is an unauthenticated relay into RelativityOne**: every caller reaching it acts with the Context User's full rights, which is strictly worse than the starting point. The handler returns results to its caller, **never the token** and never the secret.

Verification: **Verification note.** **The tool did not establish that this map is an HTTP header object** (the finding is Medium for that reason): confirm where the map is consumed; if it is not a header for a request to RelativityOne, this finding is informational. **A token has a finite lifetime.** **The calling identity changes.**

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
<!-- generated:end GO-004 client-credentials -->

#### Authorization Code

<!-- generated:begin GO-004 authorization-code -->
**Confirming the flow.** An interactive-user signal in a file assigns Authorization Code, with this note: "An interactive-user signal in this file points to Authorization Code. Confirm the flow: use Client Credentials instead if the integration runs with credentials fixed for the integration and no signed-in user." Your user profile can also make Authorization Code the default; see Changing the default in [Confirming the flow](#confirming-the-flow). See [Confirming the flow](#confirming-the-flow) and the "Which flow" paragraph of GO-004 in [Part A](part-a-go.md#go-004).

Source form: **Applies to:** every Go version; an unnamed map literal that carries the header.

```go
encoded := base64.StdEncoding.EncodeToString([]byte(userName + ":" + secret))
return map[string]string{"Authorization": "Basic " + encoded}
```

Under token authentication: **First, the destination:** if this Relativity instance is not RelativityOne — including a Relativity Server instance, whose own documentation governs — this finding is informational and the rest of this template does not apply.

```go
return map[string]string{
	"Authorization": "Bearer " + accessToken,
	csrfHeaderName:  csrfHeaderValue,
}
```

Keep the key `Authorization` and write the scheme as the literal `Bearer`.

Credentials: **Where the credentials come from.** The source form establishes that the code sends Basic, not that the destination is RelativityOne. If it is not RelativityOne, the migration is that service's own token mechanism, this finding is informational for this tool, and the reader stops here. If it is RelativityOne, the token comes from the token-acquisition part that follows, and the user name and password are replaced by the OAuth2 Client's client ID and secret, kept as Secret Management (Part B of the Migration Documentation) describes and never in source.

**Common to both variants.** The person signs in on RelativityOne's own page and the code never holds their password. The text has a common part and two variants by application type, and each variant opens with its own precondition, so that the reader can discard the others by reading one sentence each. Every block uses only the standard library and needs go1.13 or later; the shared declarations are those of the Client Credentials text and are repeated here.

```go
const (
	identityTokenURL            = "https://HOST/Relativity/Identity/connect/token"
	identityAuthorizeURL        = "https://HOST/Relativity/Identity/connect/authorize"
	clientIDEnvironmentName     = "RELATIVITY_CLIENT_ID"
	clientSecretEnvironmentName = "RELATIVITY_CLIENT_SECRET"
	expiryMarginSeconds         = 60
	csrfHeaderName              = "X-CSRF-Header"
	csrfHeaderValue             = "-"
	stateByteCount              = 16
	verifierByteCount           = 32
	serverHeaderTimeoutSeconds  = 10
	signInCookieName            = "relativity_sign_in"
	signInCookieMaxAgeSeconds   = 600
	signInTimeoutSeconds        = 300
)

type tokenResponse struct {
	AccessToken  string `json:"access_token"`
	RefreshToken string `json:"refresh_token"`
	ExpiresIn    int    `json:"expires_in"`
}

func requestToken(ctx context.Context, client *http.Client, form url.Values) (tokenResponse, error) {
	var payload tokenResponse
	request, err := http.NewRequestWithContext(ctx, http.MethodPost, identityTokenURL, strings.NewReader(form.Encode()))
	if err != nil {
		return payload, err
	}
	request.Header.Set("Content-Type", "application/x-www-form-urlencoded")
	response, err := client.Do(request)
	if err != nil {
		return payload, err
	}
	defer response.Body.Close()
	if response.StatusCode != http.StatusOK {
		return payload, fmt.Errorf("token endpoint answered %d", response.StatusCode)
	}
	err = json.NewDecoder(response.Body).Decode(&payload)
	return payload, err
}
```

**Sign in.** Replace the code that collected the person's credentials with a redirect to the authorize endpoint `<host>/Relativity/Identity/connect/authorize`, which is **verified by NSerio against a live instance; not documented by Relativity** and leads to RelativityOne's login page. The request carries the standard authorization-code parameters — `client_id`, `response_type=code`, the registered `redirect_uri`, `state` checked on return, and `code_challenge` with `code_challenge_method=S256` when PKCE is used; these are standard OAuth and Relativity documents none of them.

**PKCE.** Relativity's own documentation requires PKCE to obtain a refresh token; that a Code request that does not ask for a refresh token works without PKCE is **verified by NSerio against a live instance; not documented by Relativity**. **PKCE is not limited to clients that use refresh tokens**: NSerio recommends `S256` for every Authorization Code client (NSerio recommendation) and does not claim Relativity accepts any particular method. **Whether a client with refresh tokens disabled accepts a PKCE request (a `code_challenge`) is unverified**: test it before the block is relied on for such a client. **`state`** is generated, stored against the session and compared on return; `nonce` is not used, and Relativity does not document whether it enforces either. The authorize and token endpoints and the PKCE and `state` behaviour above are **verified by NSerio against a live instance; not documented by Relativity**. The block draws the verifier and `state` from `crypto/rand`, whose `Read` fills the buffer with cryptographically secure random bytes (the signature still returns an error, which the block checks), and computes the challenge with `crypto/sha256` and `base64.RawURLEncoding`, the unpadded URL-safe encoding; a 32-byte value encodes to a 43-character verifier.

**The client secret is required.** Code does not work without a client secret: tested and rejected, **verified by NSerio against a live instance; not documented by Relativity**, which confirms Relativity's own statement. **Any secret placed in a distributed binary is extractable by whoever holds the binary**: a Go program compiled for people other than its owner carries its client secret in a form anyone who holds the file can extract, and Relativity documents no recommendation for that case; use a short Access Token Lifetime, Trusted IPs where the outbound addresses are stable and one OAuth2 Client per distribution as compensating guidance (NSerio recommendation). Keep the secret as Secret Management (Part B of the Migration Documentation) describes, never in source or in a log.

**Exchange the code.** A form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=authorization_code`, the code, the same `redirect_uri`, the client ID and the secret, plus the `code_verifier` when PKCE is used.

```go
func randomToken(byteCount int) (string, error) {
	buffer := make([]byte, byteCount)
	if _, err := rand.Read(buffer); err != nil {
		return "", err
	}
	return base64.RawURLEncoding.EncodeToString(buffer), nil
}

func challengeOf(verifier string) string {
	digest := sha256.Sum256([]byte(verifier))
	return base64.RawURLEncoding.EncodeToString(digest[:])
}

func authorizeAddress(redirectURI string, state string, challenge string) string {
	query := url.Values{
		"client_id":     {os.Getenv(clientIDEnvironmentName)},
		"response_type": {"code"},
		"redirect_uri":  {redirectURI},
		"state":         {state},
	}
	if challenge != "" {
		query.Set("code_challenge", challenge)
		query.Set("code_challenge_method", "S256")
	}
	return identityAuthorizeURL + "?" + query.Encode()
}

func exchangeCode(ctx context.Context, client *http.Client, code string, redirectURI string, verifier string) (tokenResponse, error) {
	form := url.Values{
		"grant_type":    {"authorization_code"},
		"code":          {code},
		"redirect_uri":  {redirectURI},
		"client_id":     {os.Getenv(clientIDEnvironmentName)},
		"client_secret": {os.Getenv(clientSecretEnvironmentName)},
	}
	if verifier != "" {
		form.Set("code_verifier", verifier)
	}
	return requestToken(ctx, client, form)
}
```

**Redirect URL.** A server-side application registers **one exact HTTPS URL per environment, no wildcards**; the matching rules are not documented and no claim is made about them. A plain-HTTP loopback Redirect URL `http://127.0.0.1:<port>/<path>` on a **fixed registered port** is accepted, **verified by NSerio against a live instance; not documented by Relativity**, and is the command-line and desktop shape.

**On expiry.** With Allow Refresh Tokens enabled and PKCE used, the code holds a refresh token and sends `grant_type=refresh_token` in place of the code exchange: *inferred* from the standard OAuth shape, not tested; Relativity does not document a refresh token's lifetime, rotation or revocation, nor what disabling the client does to tokens already issued. **A refresh token is a secret**: hold it where the secret is held, never in a log. **Single sign-on:** sign-in happens on RelativityOne's own page, so the instance's single sign-on and two-factor rules apply unchanged (*inferred*). **The code stops collecting the password:** after this change nothing receives, stores or forwards the person's user name and password, and the code holds only tokens. Every call now runs as the person who signed in, so **the calling identity changes**. **Scope:** no scope set is prescribed for Code; `openid RelativityRequestOrigin UserInfo UserInfoAccess AuthenticationID RelativityWeb` works, **verified by NSerio against a live instance; not documented by Relativity**, as working and not as minimal.

**Variant 1 — a Go web server.** **Precondition: this variant is for a Go server that signs a person in to RelativityOne. If the code is a command-line tool or a desktop process, use variant 2.**

A server-side component creates `state` and the PKCE verifier, redirects to the authorize endpoint, receives the code at its registered HTTPS Redirect URL, exchanges it, keeps the tokens on the server and gives the browser **only its own session**; **the browser never holds anything from RelativityOne**. The two handlers use the standard library's `net/http`: `http.Redirect` replies to the request with a redirect to the authorize address.

```go
type WebSignIn struct {
	mutex       sync.Mutex
	pending     map[string]string
	client      *http.Client
	redirectURI string
	keepTokens  func(w http.ResponseWriter, r *http.Request, tokens tokenResponse)
}

func (signIn *WebSignIn) Login(w http.ResponseWriter, r *http.Request) {
	state, err := randomToken(stateByteCount)
	if err != nil {
		http.Error(w, "sign-in unavailable", http.StatusInternalServerError)
		return
	}
	verifier, err := randomToken(verifierByteCount)
	if err != nil {
		http.Error(w, "sign-in unavailable", http.StatusInternalServerError)
		return
	}
	signIn.mutex.Lock()
	signIn.pending[state] = verifier
	signIn.mutex.Unlock()
	http.SetCookie(w, &http.Cookie{
		Name:     signInCookieName,
		Value:    state,
		Path:     "/",
		MaxAge:   signInCookieMaxAgeSeconds,
		HttpOnly: true,
		Secure:   true,
		SameSite: http.SameSiteLaxMode,
	})
	http.Redirect(w, r, authorizeAddress(signIn.redirectURI, state, challengeOf(verifier)), http.StatusFound)
}

func (signIn *WebSignIn) Callback(w http.ResponseWriter, r *http.Request) {
	query := r.URL.Query()
	if query.Get("error") != "" {
		http.Error(w, "sign-in was not completed", http.StatusUnauthorized)
		return
	}
	state := query.Get("state")
	cookie, err := r.Cookie(signInCookieName)
	if err != nil || query.Get("code") == "" || subtle.ConstantTimeCompare([]byte(cookie.Value), []byte(state)) != 1 {
		http.Error(w, "unknown sign-in", http.StatusBadRequest)
		return
	}
	signIn.mutex.Lock()
	verifier, known := signIn.pending[state]
	delete(signIn.pending, state)
	signIn.mutex.Unlock()
	if !known {
		http.Error(w, "unknown sign-in", http.StatusBadRequest)
		return
	}
	http.SetCookie(w, &http.Cookie{Name: signInCookieName, Path: "/", MaxAge: -1})
	tokens, err := exchangeCode(r.Context(), signIn.client, query.Get("code"), signIn.redirectURI, verifier)
	if err != nil {
		http.Error(w, "sign-in failed", http.StatusBadGateway)
		return
	}
	signIn.keepTokens(w, r, tokens)
}
```

**`state` is bound to the browser session**: `Login` stores it in a short-lived, `HttpOnly`, `Secure`, `SameSite=Lax` cookie as well as in the server's pending table, and `Callback` fails (400) unless the cookie is present **and** equals the `state` on the query, so a callback that arrives without the matching cookie gets no exchange. `Callback` also **handles the `error` parameter** (401, no exchange) and **an empty `code`** (400). `state` is consumed once: the entry is deleted before the exchange and the cookie is cleared, so a replayed or unknown `state` gets no exchange. `keepTokens` is yours: store the tokens against the visitor's session on the server and set a session cookie that holds **only a session identifier**. The refresh call, if used, goes to the same token endpoint with `grant_type=refresh_token` (*inferred*). **The calling identity changes** to the person who signed in.

**Variant 2 — a command-line tool or a desktop process.** **Precondition: this variant is for a Go command-line tool or a desktop process that signs a person in to RelativityOne. If the code is a server, use variant 1.**

The process starts a listener from the standard library bound to `127.0.0.1` only, on the **fixed port the administrator registered** (the exact URL is registered, so the port is fixed, chosen by the administrator and not at runtime), for **one request**; it **prints the authorize address** (so a machine with no browser, an SSH session or a failed launch has the manual route) and, if you want, opens the system browser yourself; reads `code` and `state` from the one request, answers with a short page, closes the listener, checks `state` and exchanges the code.

```go
func SignInFromCommandLine(ctx context.Context, client *http.Client, listenAddress string, redirectURI string) (tokenResponse, error) {
	var tokens tokenResponse
	redirect, err := url.Parse(redirectURI)
	if err != nil {
		return tokens, err
	}
	host, _, err := net.SplitHostPort(listenAddress)
	if err != nil {
		return tokens, err
	}
	if host != "127.0.0.1" && host != "::1" {
		return tokens, errors.New("the sign-in listener must be bound to the loopback address")
	}
	ctx, cancel := context.WithTimeout(ctx, signInTimeoutSeconds*time.Second)
	defer cancel()
	state, err := randomToken(stateByteCount)
	if err != nil {
		return tokens, err
	}
	verifier, err := randomToken(verifierByteCount)
	if err != nil {
		return tokens, err
	}
	listener, err := net.Listen("tcp", listenAddress)
	if err != nil {
		return tokens, fmt.Errorf("the registered port is not available: %w", err)
	}
	type callback struct{ code, state, failure string }
	received := make(chan callback, 1)
	server := &http.Server{
		ReadHeaderTimeout: serverHeaderTimeoutSeconds * time.Second,
		Handler: http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
			if r.URL.Path != redirect.Path {
				http.NotFound(w, r)
				return
			}
			query := r.URL.Query()
			select {
			case received <- callback{code: query.Get("code"), state: query.Get("state"), failure: query.Get("error")}:
			default:
			}
			fmt.Fprintln(w, "Sign-in received. You can close this window.")
		}),
	}
	go func() { _ = server.Serve(listener) }()
	defer server.Shutdown(context.Background())
	fmt.Println("Open this address in a browser to sign in:", authorizeAddress(redirectURI, state, challengeOf(verifier)))
	select {
	case result := <-received:
		if result.failure != "" || result.code == "" {
			return tokens, errors.New("sign-in was not completed")
		}
		if subtle.ConstantTimeCompare([]byte(result.state), []byte(state)) != 1 {
			return tokens, errors.New("the sign-in answer does not match the request")
		}
		return exchangeCode(ctx, client, result.code, redirectURI, verifier)
	case <-ctx.Done():
		return tokens, ctx.Err()
	}
}
```

**The listener is checked and bounded in the code**: it **refuses a `listenAddress` whose host is not `127.0.0.1` or `::1`** (loopback only); its handler **answers 404 to every path other than the registered redirect path** (so a stray request cannot consume the one-shot channel); the whole sign-in has **its own deadline** (`signInTimeoutSeconds`, 300 seconds, on top of the caller's context), after which it returns the context error; a callback with an `error` parameter or an empty `code` is an error, not an exchange. **Busy port:** the port is the registered one and is **not** replaced by another (a different port is a different Redirect URL, which Relativity would not match); if `net.Listen` fails because the port is taken, the function returns the error `the registered port is not available` and the tool must report it, not retry on another port. Call it with `listenAddress` `127.0.0.1:<the registered port>` and `redirectURI` `http://127.0.0.1:<the registered port>/<path>`. **Where the tokens are kept** is the program's decision: in memory for the run, or, for a refresh token, in a file whose permissions you restrict (**a distributed command-line tool carries its client secret in a form anyone who holds the distribution can extract**). Every call now runs as the person who signed in.

Verification: **Verification note.** **The tool did not establish that this map is an HTTP header object** (the finding is Medium for that reason): confirm where the map is consumed; if it is not a header for a request to RelativityOne, this finding is informational. **A token has a finite lifetime.** **The calling identity changes.**

OAuth2 Client: Create or confirm this OAuth2 Client in RelativityOne (NSerio's observation: the menu path is Home > Authentication > OAuth2 Client Manager, as Relativity's REST API authentication page names it). This tool never connects to RelativityOne and never creates or changes the OAuth2 Client; the settings below are guidance.
- Name: a descriptive name identifying the application it serves.
- Flow Grant Type: Code
- Redirect URLs: Relativity documentation: each URL includes the http or https protocol prefix. The exact callback URL is registered by the Relativity administrator. Server-side web applications, NSerio's recommendation: one exact HTTPS URL per environment, with no wildcards. Desktop and CLI applications: a plain-HTTP loopback URL, `http://127.0.0.1:<port>/<path>`, on a fixed port because the URL is registered in advance and the port cannot be chosen at runtime (verified by NSerio against a live instance; not documented by Relativity).
- Access Token Lifetime: 60 minutes, stated in minutes because the field is in minutes. Relativity documentation: tokens for this flow must have a short lifetime, and the recommended value matches the Identity Server default.
- Allow refresh tokens: NSerio's recommendation: leave it off unless the application must keep a user session active beyond the access token lifetime without asking the user to sign in again. If it is on, the refresh tokens are stored as secrets, per Secret Management (Part B of the Migration Documentation).
- Enabled: on.
<!-- generated:end GO-004 authorization-code -->

### GO-005 · URLUserinfoInRequestCall

<a id="go-005-migration"></a>

Remove the user name and the password from the address and send a Bearer token in the `Authorization` header instead. The standard library turns a URL's user information into a Basic header only when the request has none, so the Bearer header the entry shows is what is sent. **A credential in an address is also in logs, in a process list and in the repository history**: apply the rotation statement below.

#### Client Credentials

<!-- generated:begin GO-005 client-credentials -->
**Confirming the flow.** When no signal in a file decides the flow, the tool assigns Client Credentials, the default, with this note: "Who supplies the credential at run time? If a person types it or signs in, the integration acts for that person: use Authorization Code. If it comes from configuration, settings, an environment variable or a secret store: use Client Credentials. No signal in this file showed which applies, so Client Credentials, the default, is recommended. Use Authorization Code instead if the integration acts on behalf of a signed-in user, or if your RelativityOne instance already has a Code-flow OAuth2 Client for this integration." A fixed-credential signal assigns it with this note: "A fixed-credential signal in this file points to Client Credentials, but how often that kind of signal is right has not been measured. Use Authorization Code instead if the integration acts on behalf of a signed-in user, or if your RelativityOne instance already has a Code-flow OAuth2 Client for this integration." See [Confirming the flow](#confirming-the-flow) and the "Which flow" paragraph of GO-005 in [Part A](part-a-go.md#go-005).

Source form: **Applies to:** every Go version (`NewRequestWithContext` needs go1.13); an `http` or `https` URL literal with a user and a password given to a request call.

```go
return http.Get("https://user:secret@host.example/path")
```

Under token authentication: **First, the destination:** if this Relativity instance is not RelativityOne — including a Relativity Server instance, whose own documentation governs — this finding is informational and the rest of this template does not apply.

```go
request, err := http.NewRequestWithContext(ctx, http.MethodGet, "https://host.example/path", nil)
if err != nil {
	return nil, err
}
request.Header.Set("Authorization", "Bearer "+accessToken)
request.Header.Set(csrfHeaderName, csrfHeaderValue)
return client.Do(request)
```

Remove the user and the password from the URL. **The standard library turns a URL's userinfo into a Basic `Authorization` header only when the request has none**, so the Bearer header above is what is sent.

Credentials: **Where the credentials come from.** The source form establishes that the code sends Basic, not that the destination is RelativityOne. If it is not RelativityOne, the migration is that service's own token mechanism, this finding is informational for this tool, and the reader stops here. If it is RelativityOne, the token comes from the token-acquisition part that follows, and the user name and password are replaced by the OAuth2 Client's client ID and secret, kept as Secret Management (Part B of the Migration Documentation) describes and never in source. **A URL that carried a password is an exposure of its own**: it is in source, and the Go program may have logged it. **Treat this credential as compromised. Rotate it, and assume the repository history has published it. Completing the migration does not close this exposure; rotation does.**

**Where the token comes from.** Create an OAuth2 Client with Flow Grant Type set to Client Credentials (part 3 lists its settings), then send a form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=client_credentials`, `client_id` and `client_secret`. The token endpoint is **documented** for this grant. The token is then sent as `Authorization: Bearer <token>`, with `X-CSRF-Header: -` on REST and Kepler calls. The scope depends on what the integration needs and **no single scope is required**: Relativity's documentation shows `SystemUserInfo` and `UserInfoAccess` and no page says which is correct or what either grants; use the value the API you call documents, and confirm it. Every block below uses only the standard library and needs go1.13 or later.

**Shared declarations.** Every block of this text, and of the use-site forms above, uses these constants and helpers; `HOST` is the RelativityOne host.

```go
const (
	identityTokenURL            = "https://HOST/Relativity/Identity/connect/token"
	identityAuthorizeURL        = "https://HOST/Relativity/Identity/connect/authorize"
	clientIDEnvironmentName     = "RELATIVITY_CLIENT_ID"
	clientSecretEnvironmentName = "RELATIVITY_CLIENT_SECRET"
	expiryMarginSeconds         = 60
	csrfHeaderName              = "X-CSRF-Header"
	csrfHeaderValue             = "-"
	stateByteCount              = 16
	verifierByteCount           = 32
	serverHeaderTimeoutSeconds  = 10
	signInCookieName            = "relativity_sign_in"
	signInCookieMaxAgeSeconds   = 600
	signInTimeoutSeconds        = 300
)

type tokenResponse struct {
	AccessToken  string `json:"access_token"`
	RefreshToken string `json:"refresh_token"`
	ExpiresIn    int    `json:"expires_in"`
}

func requestToken(ctx context.Context, client *http.Client, form url.Values) (tokenResponse, error) {
	var payload tokenResponse
	request, err := http.NewRequestWithContext(ctx, http.MethodPost, identityTokenURL, strings.NewReader(form.Encode()))
	if err != nil {
		return payload, err
	}
	request.Header.Set("Content-Type", "application/x-www-form-urlencoded")
	response, err := client.Do(request)
	if err != nil {
		return payload, err
	}
	defer response.Body.Close()
	if response.StatusCode != http.StatusOK {
		return payload, fmt.Errorf("token endpoint answered %d", response.StatusCode)
	}
	err = json.NewDecoder(response.Body).Decode(&payload)
	return payload, err
}
```

The token request is a form-encoded `POST`. **Where the client secret travels in the token request** (the form body, as here, or HTTP Basic client authentication) **and the field names of the token response are not documented by Relativity**: the form body and `tokenResponse` are the standard OAuth shape and an NSerio recommendation, **not verified** against RelativityOne. **Do not** send the client secret with `SetBasicAuth` and **do not** put it in the URL.

**Variant: a Go process acting as itself.** **Precondition: this variant is for a Go job, daemon or service that does not act for a signed-in person.**

```go
type TokenSource struct {
	mutex     sync.Mutex
	client    *http.Client
	token     string
	expiresAt time.Time
}

func (source *TokenSource) Token(ctx context.Context) (string, error) {
	source.mutex.Lock()
	defer source.mutex.Unlock()
	if source.token == "" || time.Until(source.expiresAt) < expiryMarginSeconds*time.Second {
		form := url.Values{
			"grant_type":    {"client_credentials"},
			"client_id":     {os.Getenv(clientIDEnvironmentName)},
			"client_secret": {os.Getenv(clientSecretEnvironmentName)},
		}
		payload, err := requestToken(ctx, source.client, form)
		if err != nil {
			return "", err
		}
		source.token = payload.AccessToken
		source.expiresAt = time.Now().Add(time.Duration(payload.ExpiresIn) * time.Second)
	}
	return source.token, nil
}
```

**Cache the token and re-acquire it on expiry — do not refresh.** Client Credentials does not offer refresh tokens: **verified by NSerio against a live instance; not documented by Relativity** — the Allow Refresh Tokens control does not appear for this grant in the RelativityOne user interface. The recommended Access Token Lifetime is 60 minutes. **The client secret** belongs in the process environment or a secret store, as Secret Management (Part B of the Migration Documentation) describes, **never in source, in a tracked `.env` file or in a log**: the block reads it with `os.Getenv`, which returns an empty string when the variable is not present, so an unset variable is an empty secret and the token request fails, which the code reports. A reset invalidates the previous secret immediately, so plan the cut-over; a second client created before the reset is an inferred way to do it without a gap. **Before deploying, confirm the calling identity:** every call now runs as the OAuth2 Client's Context User, not as the user in the replaced credential, and a non-administrator Context User is NSerio's recommendation. Trusted IPs restrict where a token can be requested, not a token already issued. The statements that Client Credentials needs a client secret and that the secret is displayed once are carried from the other languages' guidance and are not verified for this text.

**Variant: a Go web service that does not act for a signed-in user.** **Precondition: this variant is for a Go server that calls RelativityOne with no person signed in to it, so no person's identity exists to preserve. If a person signs in, use Authorization Code, variant 1.**

Use the block above in the server process; **the server must authenticate and authorize its own callers, and the OAuth2 Client's Context User must hold the minimum permissions the operation needs. Without both, the result is an unauthenticated relay into RelativityOne**: every caller reaching it acts with the Context User's full rights, which is strictly worse than the starting point. The handler returns results to its caller, **never the token** and never the secret.

Verification: **Verification note.** **A token has a finite lifetime.** **The calling identity changes.** A URL assembled from parts is not read.

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
<!-- generated:end GO-005 client-credentials -->

#### Authorization Code

<!-- generated:begin GO-005 authorization-code -->
**Confirming the flow.** An interactive-user signal in a file assigns Authorization Code, with this note: "An interactive-user signal in this file points to Authorization Code. Confirm the flow: use Client Credentials instead if the integration runs with credentials fixed for the integration and no signed-in user." Your user profile can also make Authorization Code the default; see Changing the default in [Confirming the flow](#confirming-the-flow). See [Confirming the flow](#confirming-the-flow) and the "Which flow" paragraph of GO-005 in [Part A](part-a-go.md#go-005).

Source form: **Applies to:** every Go version (`NewRequestWithContext` needs go1.13); an `http` or `https` URL literal with a user and a password given to a request call.

```go
return http.Get("https://user:secret@host.example/path")
```

Under token authentication: **First, the destination:** if this Relativity instance is not RelativityOne — including a Relativity Server instance, whose own documentation governs — this finding is informational and the rest of this template does not apply.

```go
request, err := http.NewRequestWithContext(ctx, http.MethodGet, "https://host.example/path", nil)
if err != nil {
	return nil, err
}
request.Header.Set("Authorization", "Bearer "+accessToken)
request.Header.Set(csrfHeaderName, csrfHeaderValue)
return client.Do(request)
```

Remove the user and the password from the URL. **The standard library turns a URL's userinfo into a Basic `Authorization` header only when the request has none**, so the Bearer header above is what is sent.

Credentials: **Where the credentials come from.** The source form establishes that the code sends Basic, not that the destination is RelativityOne. If it is not RelativityOne, the migration is that service's own token mechanism, this finding is informational for this tool, and the reader stops here. If it is RelativityOne, the token comes from the token-acquisition part that follows, and the user name and password are replaced by the OAuth2 Client's client ID and secret, kept as Secret Management (Part B of the Migration Documentation) describes and never in source. **A URL that carried a password is an exposure of its own**: it is in source, and the Go program may have logged it. **Treat this credential as compromised. Rotate it, and assume the repository history has published it. Completing the migration does not close this exposure; rotation does.**

**Common to both variants.** The person signs in on RelativityOne's own page and the code never holds their password. The text has a common part and two variants by application type, and each variant opens with its own precondition, so that the reader can discard the others by reading one sentence each. Every block uses only the standard library and needs go1.13 or later; the shared declarations are those of the Client Credentials text and are repeated here.

```go
const (
	identityTokenURL            = "https://HOST/Relativity/Identity/connect/token"
	identityAuthorizeURL        = "https://HOST/Relativity/Identity/connect/authorize"
	clientIDEnvironmentName     = "RELATIVITY_CLIENT_ID"
	clientSecretEnvironmentName = "RELATIVITY_CLIENT_SECRET"
	expiryMarginSeconds         = 60
	csrfHeaderName              = "X-CSRF-Header"
	csrfHeaderValue             = "-"
	stateByteCount              = 16
	verifierByteCount           = 32
	serverHeaderTimeoutSeconds  = 10
	signInCookieName            = "relativity_sign_in"
	signInCookieMaxAgeSeconds   = 600
	signInTimeoutSeconds        = 300
)

type tokenResponse struct {
	AccessToken  string `json:"access_token"`
	RefreshToken string `json:"refresh_token"`
	ExpiresIn    int    `json:"expires_in"`
}

func requestToken(ctx context.Context, client *http.Client, form url.Values) (tokenResponse, error) {
	var payload tokenResponse
	request, err := http.NewRequestWithContext(ctx, http.MethodPost, identityTokenURL, strings.NewReader(form.Encode()))
	if err != nil {
		return payload, err
	}
	request.Header.Set("Content-Type", "application/x-www-form-urlencoded")
	response, err := client.Do(request)
	if err != nil {
		return payload, err
	}
	defer response.Body.Close()
	if response.StatusCode != http.StatusOK {
		return payload, fmt.Errorf("token endpoint answered %d", response.StatusCode)
	}
	err = json.NewDecoder(response.Body).Decode(&payload)
	return payload, err
}
```

**Sign in.** Replace the code that collected the person's credentials with a redirect to the authorize endpoint `<host>/Relativity/Identity/connect/authorize`, which is **verified by NSerio against a live instance; not documented by Relativity** and leads to RelativityOne's login page. The request carries the standard authorization-code parameters — `client_id`, `response_type=code`, the registered `redirect_uri`, `state` checked on return, and `code_challenge` with `code_challenge_method=S256` when PKCE is used; these are standard OAuth and Relativity documents none of them.

**PKCE.** Relativity's own documentation requires PKCE to obtain a refresh token; that a Code request that does not ask for a refresh token works without PKCE is **verified by NSerio against a live instance; not documented by Relativity**. **PKCE is not limited to clients that use refresh tokens**: NSerio recommends `S256` for every Authorization Code client (NSerio recommendation) and does not claim Relativity accepts any particular method. **Whether a client with refresh tokens disabled accepts a PKCE request (a `code_challenge`) is unverified**: test it before the block is relied on for such a client. **`state`** is generated, stored against the session and compared on return; `nonce` is not used, and Relativity does not document whether it enforces either. The authorize and token endpoints and the PKCE and `state` behaviour above are **verified by NSerio against a live instance; not documented by Relativity**. The block draws the verifier and `state` from `crypto/rand`, whose `Read` fills the buffer with cryptographically secure random bytes (the signature still returns an error, which the block checks), and computes the challenge with `crypto/sha256` and `base64.RawURLEncoding`, the unpadded URL-safe encoding; a 32-byte value encodes to a 43-character verifier.

**The client secret is required.** Code does not work without a client secret: tested and rejected, **verified by NSerio against a live instance; not documented by Relativity**, which confirms Relativity's own statement. **Any secret placed in a distributed binary is extractable by whoever holds the binary**: a Go program compiled for people other than its owner carries its client secret in a form anyone who holds the file can extract, and Relativity documents no recommendation for that case; use a short Access Token Lifetime, Trusted IPs where the outbound addresses are stable and one OAuth2 Client per distribution as compensating guidance (NSerio recommendation). Keep the secret as Secret Management (Part B of the Migration Documentation) describes, never in source or in a log.

**Exchange the code.** A form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=authorization_code`, the code, the same `redirect_uri`, the client ID and the secret, plus the `code_verifier` when PKCE is used.

```go
func randomToken(byteCount int) (string, error) {
	buffer := make([]byte, byteCount)
	if _, err := rand.Read(buffer); err != nil {
		return "", err
	}
	return base64.RawURLEncoding.EncodeToString(buffer), nil
}

func challengeOf(verifier string) string {
	digest := sha256.Sum256([]byte(verifier))
	return base64.RawURLEncoding.EncodeToString(digest[:])
}

func authorizeAddress(redirectURI string, state string, challenge string) string {
	query := url.Values{
		"client_id":     {os.Getenv(clientIDEnvironmentName)},
		"response_type": {"code"},
		"redirect_uri":  {redirectURI},
		"state":         {state},
	}
	if challenge != "" {
		query.Set("code_challenge", challenge)
		query.Set("code_challenge_method", "S256")
	}
	return identityAuthorizeURL + "?" + query.Encode()
}

func exchangeCode(ctx context.Context, client *http.Client, code string, redirectURI string, verifier string) (tokenResponse, error) {
	form := url.Values{
		"grant_type":    {"authorization_code"},
		"code":          {code},
		"redirect_uri":  {redirectURI},
		"client_id":     {os.Getenv(clientIDEnvironmentName)},
		"client_secret": {os.Getenv(clientSecretEnvironmentName)},
	}
	if verifier != "" {
		form.Set("code_verifier", verifier)
	}
	return requestToken(ctx, client, form)
}
```

**Redirect URL.** A server-side application registers **one exact HTTPS URL per environment, no wildcards**; the matching rules are not documented and no claim is made about them. A plain-HTTP loopback Redirect URL `http://127.0.0.1:<port>/<path>` on a **fixed registered port** is accepted, **verified by NSerio against a live instance; not documented by Relativity**, and is the command-line and desktop shape.

**On expiry.** With Allow Refresh Tokens enabled and PKCE used, the code holds a refresh token and sends `grant_type=refresh_token` in place of the code exchange: *inferred* from the standard OAuth shape, not tested; Relativity does not document a refresh token's lifetime, rotation or revocation, nor what disabling the client does to tokens already issued. **A refresh token is a secret**: hold it where the secret is held, never in a log. **Single sign-on:** sign-in happens on RelativityOne's own page, so the instance's single sign-on and two-factor rules apply unchanged (*inferred*). **The code stops collecting the password:** after this change nothing receives, stores or forwards the person's user name and password, and the code holds only tokens. Every call now runs as the person who signed in, so **the calling identity changes**. **Scope:** no scope set is prescribed for Code; `openid RelativityRequestOrigin UserInfo UserInfoAccess AuthenticationID RelativityWeb` works, **verified by NSerio against a live instance; not documented by Relativity**, as working and not as minimal.

**Variant 1 — a Go web server.** **Precondition: this variant is for a Go server that signs a person in to RelativityOne. If the code is a command-line tool or a desktop process, use variant 2.**

A server-side component creates `state` and the PKCE verifier, redirects to the authorize endpoint, receives the code at its registered HTTPS Redirect URL, exchanges it, keeps the tokens on the server and gives the browser **only its own session**; **the browser never holds anything from RelativityOne**. The two handlers use the standard library's `net/http`: `http.Redirect` replies to the request with a redirect to the authorize address.

```go
type WebSignIn struct {
	mutex       sync.Mutex
	pending     map[string]string
	client      *http.Client
	redirectURI string
	keepTokens  func(w http.ResponseWriter, r *http.Request, tokens tokenResponse)
}

func (signIn *WebSignIn) Login(w http.ResponseWriter, r *http.Request) {
	state, err := randomToken(stateByteCount)
	if err != nil {
		http.Error(w, "sign-in unavailable", http.StatusInternalServerError)
		return
	}
	verifier, err := randomToken(verifierByteCount)
	if err != nil {
		http.Error(w, "sign-in unavailable", http.StatusInternalServerError)
		return
	}
	signIn.mutex.Lock()
	signIn.pending[state] = verifier
	signIn.mutex.Unlock()
	http.SetCookie(w, &http.Cookie{
		Name:     signInCookieName,
		Value:    state,
		Path:     "/",
		MaxAge:   signInCookieMaxAgeSeconds,
		HttpOnly: true,
		Secure:   true,
		SameSite: http.SameSiteLaxMode,
	})
	http.Redirect(w, r, authorizeAddress(signIn.redirectURI, state, challengeOf(verifier)), http.StatusFound)
}

func (signIn *WebSignIn) Callback(w http.ResponseWriter, r *http.Request) {
	query := r.URL.Query()
	if query.Get("error") != "" {
		http.Error(w, "sign-in was not completed", http.StatusUnauthorized)
		return
	}
	state := query.Get("state")
	cookie, err := r.Cookie(signInCookieName)
	if err != nil || query.Get("code") == "" || subtle.ConstantTimeCompare([]byte(cookie.Value), []byte(state)) != 1 {
		http.Error(w, "unknown sign-in", http.StatusBadRequest)
		return
	}
	signIn.mutex.Lock()
	verifier, known := signIn.pending[state]
	delete(signIn.pending, state)
	signIn.mutex.Unlock()
	if !known {
		http.Error(w, "unknown sign-in", http.StatusBadRequest)
		return
	}
	http.SetCookie(w, &http.Cookie{Name: signInCookieName, Path: "/", MaxAge: -1})
	tokens, err := exchangeCode(r.Context(), signIn.client, query.Get("code"), signIn.redirectURI, verifier)
	if err != nil {
		http.Error(w, "sign-in failed", http.StatusBadGateway)
		return
	}
	signIn.keepTokens(w, r, tokens)
}
```

**`state` is bound to the browser session**: `Login` stores it in a short-lived, `HttpOnly`, `Secure`, `SameSite=Lax` cookie as well as in the server's pending table, and `Callback` fails (400) unless the cookie is present **and** equals the `state` on the query, so a callback that arrives without the matching cookie gets no exchange. `Callback` also **handles the `error` parameter** (401, no exchange) and **an empty `code`** (400). `state` is consumed once: the entry is deleted before the exchange and the cookie is cleared, so a replayed or unknown `state` gets no exchange. `keepTokens` is yours: store the tokens against the visitor's session on the server and set a session cookie that holds **only a session identifier**. The refresh call, if used, goes to the same token endpoint with `grant_type=refresh_token` (*inferred*). **The calling identity changes** to the person who signed in.

**Variant 2 — a command-line tool or a desktop process.** **Precondition: this variant is for a Go command-line tool or a desktop process that signs a person in to RelativityOne. If the code is a server, use variant 1.**

The process starts a listener from the standard library bound to `127.0.0.1` only, on the **fixed port the administrator registered** (the exact URL is registered, so the port is fixed, chosen by the administrator and not at runtime), for **one request**; it **prints the authorize address** (so a machine with no browser, an SSH session or a failed launch has the manual route) and, if you want, opens the system browser yourself; reads `code` and `state` from the one request, answers with a short page, closes the listener, checks `state` and exchanges the code.

```go
func SignInFromCommandLine(ctx context.Context, client *http.Client, listenAddress string, redirectURI string) (tokenResponse, error) {
	var tokens tokenResponse
	redirect, err := url.Parse(redirectURI)
	if err != nil {
		return tokens, err
	}
	host, _, err := net.SplitHostPort(listenAddress)
	if err != nil {
		return tokens, err
	}
	if host != "127.0.0.1" && host != "::1" {
		return tokens, errors.New("the sign-in listener must be bound to the loopback address")
	}
	ctx, cancel := context.WithTimeout(ctx, signInTimeoutSeconds*time.Second)
	defer cancel()
	state, err := randomToken(stateByteCount)
	if err != nil {
		return tokens, err
	}
	verifier, err := randomToken(verifierByteCount)
	if err != nil {
		return tokens, err
	}
	listener, err := net.Listen("tcp", listenAddress)
	if err != nil {
		return tokens, fmt.Errorf("the registered port is not available: %w", err)
	}
	type callback struct{ code, state, failure string }
	received := make(chan callback, 1)
	server := &http.Server{
		ReadHeaderTimeout: serverHeaderTimeoutSeconds * time.Second,
		Handler: http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
			if r.URL.Path != redirect.Path {
				http.NotFound(w, r)
				return
			}
			query := r.URL.Query()
			select {
			case received <- callback{code: query.Get("code"), state: query.Get("state"), failure: query.Get("error")}:
			default:
			}
			fmt.Fprintln(w, "Sign-in received. You can close this window.")
		}),
	}
	go func() { _ = server.Serve(listener) }()
	defer server.Shutdown(context.Background())
	fmt.Println("Open this address in a browser to sign in:", authorizeAddress(redirectURI, state, challengeOf(verifier)))
	select {
	case result := <-received:
		if result.failure != "" || result.code == "" {
			return tokens, errors.New("sign-in was not completed")
		}
		if subtle.ConstantTimeCompare([]byte(result.state), []byte(state)) != 1 {
			return tokens, errors.New("the sign-in answer does not match the request")
		}
		return exchangeCode(ctx, client, result.code, redirectURI, verifier)
	case <-ctx.Done():
		return tokens, ctx.Err()
	}
}
```

**The listener is checked and bounded in the code**: it **refuses a `listenAddress` whose host is not `127.0.0.1` or `::1`** (loopback only); its handler **answers 404 to every path other than the registered redirect path** (so a stray request cannot consume the one-shot channel); the whole sign-in has **its own deadline** (`signInTimeoutSeconds`, 300 seconds, on top of the caller's context), after which it returns the context error; a callback with an `error` parameter or an empty `code` is an error, not an exchange. **Busy port:** the port is the registered one and is **not** replaced by another (a different port is a different Redirect URL, which Relativity would not match); if `net.Listen` fails because the port is taken, the function returns the error `the registered port is not available` and the tool must report it, not retry on another port. Call it with `listenAddress` `127.0.0.1:<the registered port>` and `redirectURI` `http://127.0.0.1:<the registered port>/<path>`. **Where the tokens are kept** is the program's decision: in memory for the run, or, for a refresh token, in a file whose permissions you restrict (**a distributed command-line tool carries its client secret in a form anyone who holds the distribution can extract**). Every call now runs as the person who signed in.

Verification: **Verification note.** **A token has a finite lifetime.** **The calling identity changes.** A URL assembled from parts is not read.

OAuth2 Client: Create or confirm this OAuth2 Client in RelativityOne (NSerio's observation: the menu path is Home > Authentication > OAuth2 Client Manager, as Relativity's REST API authentication page names it). This tool never connects to RelativityOne and never creates or changes the OAuth2 Client; the settings below are guidance.
- Name: a descriptive name identifying the application it serves.
- Flow Grant Type: Code
- Redirect URLs: Relativity documentation: each URL includes the http or https protocol prefix. The exact callback URL is registered by the Relativity administrator. Server-side web applications, NSerio's recommendation: one exact HTTPS URL per environment, with no wildcards. Desktop and CLI applications: a plain-HTTP loopback URL, `http://127.0.0.1:<port>/<path>`, on a fixed port because the URL is registered in advance and the port cannot be chosen at runtime (verified by NSerio against a live instance; not documented by Relativity).
- Access Token Lifetime: 60 minutes, stated in minutes because the field is in minutes. Relativity documentation: tokens for this flow must have a short lifetime, and the recommended value matches the Identity Server default.
- Allow refresh tokens: NSerio's recommendation: leave it off unless the application must keep a user session active beyond the access token lifetime without asking the user to sign in again. If it is on, the refresh tokens are stored as secrets, per Secret Management (Part B of the Migration Documentation).
- Enabled: on.
<!-- generated:end GO-005 authorization-code -->

### GO-006 · Base64UserPasswordPair

<a id="go-006-migration"></a>

Delete the encoder call and the pair it builds, and send a Bearer token in the `Authorization` header instead. Nothing about the user name and the password survives in the code. If the encoded value is held in a name and used in another statement, follow the name to the header that carries it. The tool read the encoder and not where the encoded value goes: confirm that it is a request to RelativityOne.

#### Client Credentials

<!-- generated:begin GO-006 client-credentials -->
**Confirming the flow.** When no signal in a file decides the flow, the tool assigns Client Credentials, the default, with this note: "Who supplies the credential at run time? If a person types it or signs in, the integration acts for that person: use Authorization Code. If it comes from configuration, settings, an environment variable or a secret store: use Client Credentials. No signal in this file showed which applies, so Client Credentials, the default, is recommended. Use Authorization Code instead if the integration acts on behalf of a signed-in user, or if your RelativityOne instance already has a Code-flow OAuth2 Client for this integration." A fixed-credential signal assigns it with this note: "A fixed-credential signal in this file points to Client Credentials, but how often that kind of signal is right has not been measured. Use Authorization Code instead if the integration acts on behalf of a signed-in user, or if your RelativityOne instance already has a Code-flow OAuth2 Client for this integration." See [Confirming the flow](#confirming-the-flow) and the "Which flow" paragraph of GO-006 in [Part A](part-a-go.md#go-006).

Source form: **Applies to:** every Go version (`AppendEncode` needs go1.22); an encoder over a user-and-password pair near a header.

```go
encoded := base64.StdEncoding.EncodeToString([]byte(userName + ":" + secret))
request.Header.Set("Authorization", "Basic "+encoded)
```

Under token authentication: **First, the destination:** if this Relativity instance is not RelativityOne — including a Relativity Server instance, whose own documentation governs — this finding is informational and the rest of this template does not apply.

```go
request.Header.Set("Authorization", "Bearer "+accessToken)
request.Header.Set(csrfHeaderName, csrfHeaderValue)
```

Delete the encoder.

Credentials: **Where the credentials come from.** The source form establishes that the code sends Basic, not that the destination is RelativityOne. If it is not RelativityOne, the migration is that service's own token mechanism, this finding is informational for this tool, and the reader stops here. If it is RelativityOne, the token comes from the token-acquisition part that follows, and the user name and password are replaced by the OAuth2 Client's client ID and secret, kept as Secret Management (Part B of the Migration Documentation) describes and never in source.

**Where the token comes from.** Create an OAuth2 Client with Flow Grant Type set to Client Credentials (part 3 lists its settings), then send a form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=client_credentials`, `client_id` and `client_secret`. The token endpoint is **documented** for this grant. The token is then sent as `Authorization: Bearer <token>`, with `X-CSRF-Header: -` on REST and Kepler calls. The scope depends on what the integration needs and **no single scope is required**: Relativity's documentation shows `SystemUserInfo` and `UserInfoAccess` and no page says which is correct or what either grants; use the value the API you call documents, and confirm it. Every block below uses only the standard library and needs go1.13 or later.

**Shared declarations.** Every block of this text, and of the use-site forms above, uses these constants and helpers; `HOST` is the RelativityOne host.

```go
const (
	identityTokenURL            = "https://HOST/Relativity/Identity/connect/token"
	identityAuthorizeURL        = "https://HOST/Relativity/Identity/connect/authorize"
	clientIDEnvironmentName     = "RELATIVITY_CLIENT_ID"
	clientSecretEnvironmentName = "RELATIVITY_CLIENT_SECRET"
	expiryMarginSeconds         = 60
	csrfHeaderName              = "X-CSRF-Header"
	csrfHeaderValue             = "-"
	stateByteCount              = 16
	verifierByteCount           = 32
	serverHeaderTimeoutSeconds  = 10
	signInCookieName            = "relativity_sign_in"
	signInCookieMaxAgeSeconds   = 600
	signInTimeoutSeconds        = 300
)

type tokenResponse struct {
	AccessToken  string `json:"access_token"`
	RefreshToken string `json:"refresh_token"`
	ExpiresIn    int    `json:"expires_in"`
}

func requestToken(ctx context.Context, client *http.Client, form url.Values) (tokenResponse, error) {
	var payload tokenResponse
	request, err := http.NewRequestWithContext(ctx, http.MethodPost, identityTokenURL, strings.NewReader(form.Encode()))
	if err != nil {
		return payload, err
	}
	request.Header.Set("Content-Type", "application/x-www-form-urlencoded")
	response, err := client.Do(request)
	if err != nil {
		return payload, err
	}
	defer response.Body.Close()
	if response.StatusCode != http.StatusOK {
		return payload, fmt.Errorf("token endpoint answered %d", response.StatusCode)
	}
	err = json.NewDecoder(response.Body).Decode(&payload)
	return payload, err
}
```

The token request is a form-encoded `POST`. **Where the client secret travels in the token request** (the form body, as here, or HTTP Basic client authentication) **and the field names of the token response are not documented by Relativity**: the form body and `tokenResponse` are the standard OAuth shape and an NSerio recommendation, **not verified** against RelativityOne. **Do not** send the client secret with `SetBasicAuth` and **do not** put it in the URL.

**Variant: a Go process acting as itself.** **Precondition: this variant is for a Go job, daemon or service that does not act for a signed-in person.**

```go
type TokenSource struct {
	mutex     sync.Mutex
	client    *http.Client
	token     string
	expiresAt time.Time
}

func (source *TokenSource) Token(ctx context.Context) (string, error) {
	source.mutex.Lock()
	defer source.mutex.Unlock()
	if source.token == "" || time.Until(source.expiresAt) < expiryMarginSeconds*time.Second {
		form := url.Values{
			"grant_type":    {"client_credentials"},
			"client_id":     {os.Getenv(clientIDEnvironmentName)},
			"client_secret": {os.Getenv(clientSecretEnvironmentName)},
		}
		payload, err := requestToken(ctx, source.client, form)
		if err != nil {
			return "", err
		}
		source.token = payload.AccessToken
		source.expiresAt = time.Now().Add(time.Duration(payload.ExpiresIn) * time.Second)
	}
	return source.token, nil
}
```

**Cache the token and re-acquire it on expiry — do not refresh.** Client Credentials does not offer refresh tokens: **verified by NSerio against a live instance; not documented by Relativity** — the Allow Refresh Tokens control does not appear for this grant in the RelativityOne user interface. The recommended Access Token Lifetime is 60 minutes. **The client secret** belongs in the process environment or a secret store, as Secret Management (Part B of the Migration Documentation) describes, **never in source, in a tracked `.env` file or in a log**: the block reads it with `os.Getenv`, which returns an empty string when the variable is not present, so an unset variable is an empty secret and the token request fails, which the code reports. A reset invalidates the previous secret immediately, so plan the cut-over; a second client created before the reset is an inferred way to do it without a gap. **Before deploying, confirm the calling identity:** every call now runs as the OAuth2 Client's Context User, not as the user in the replaced credential, and a non-administrator Context User is NSerio's recommendation. Trusted IPs restrict where a token can be requested, not a token already issued. The statements that Client Credentials needs a client secret and that the secret is displayed once are carried from the other languages' guidance and are not verified for this text.

**Variant: a Go web service that does not act for a signed-in user.** **Precondition: this variant is for a Go server that calls RelativityOne with no person signed in to it, so no person's identity exists to preserve. If a person signs in, use Authorization Code, variant 1.**

Use the block above in the server process; **the server must authenticate and authorize its own callers, and the OAuth2 Client's Context User must hold the minimum permissions the operation needs. Without both, the result is an unauthenticated relay into RelativityOne**: every caller reaching it acts with the Context User's full rights, which is strictly worse than the starting point. The handler returns results to its caller, **never the token** and never the secret.

Verification: **Verification note.** The tool read the encoder, **not where the encoded value goes**: confirm that it is a request to RelativityOne. **Other uses may not appear as findings**: a pair built in another statement is not read. **A token has a finite lifetime.** **The calling identity changes.**

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
<!-- generated:end GO-006 client-credentials -->

#### Authorization Code

<!-- generated:begin GO-006 authorization-code -->
**Confirming the flow.** An interactive-user signal in a file assigns Authorization Code, with this note: "An interactive-user signal in this file points to Authorization Code. Confirm the flow: use Client Credentials instead if the integration runs with credentials fixed for the integration and no signed-in user." Your user profile can also make Authorization Code the default; see Changing the default in [Confirming the flow](#confirming-the-flow). See [Confirming the flow](#confirming-the-flow) and the "Which flow" paragraph of GO-006 in [Part A](part-a-go.md#go-006).

Source form: **Applies to:** every Go version (`AppendEncode` needs go1.22); an encoder over a user-and-password pair near a header.

```go
encoded := base64.StdEncoding.EncodeToString([]byte(userName + ":" + secret))
request.Header.Set("Authorization", "Basic "+encoded)
```

Under token authentication: **First, the destination:** if this Relativity instance is not RelativityOne — including a Relativity Server instance, whose own documentation governs — this finding is informational and the rest of this template does not apply.

```go
request.Header.Set("Authorization", "Bearer "+accessToken)
request.Header.Set(csrfHeaderName, csrfHeaderValue)
```

Delete the encoder.

Credentials: **Where the credentials come from.** The source form establishes that the code sends Basic, not that the destination is RelativityOne. If it is not RelativityOne, the migration is that service's own token mechanism, this finding is informational for this tool, and the reader stops here. If it is RelativityOne, the token comes from the token-acquisition part that follows, and the user name and password are replaced by the OAuth2 Client's client ID and secret, kept as Secret Management (Part B of the Migration Documentation) describes and never in source.

**Common to both variants.** The person signs in on RelativityOne's own page and the code never holds their password. The text has a common part and two variants by application type, and each variant opens with its own precondition, so that the reader can discard the others by reading one sentence each. Every block uses only the standard library and needs go1.13 or later; the shared declarations are those of the Client Credentials text and are repeated here.

```go
const (
	identityTokenURL            = "https://HOST/Relativity/Identity/connect/token"
	identityAuthorizeURL        = "https://HOST/Relativity/Identity/connect/authorize"
	clientIDEnvironmentName     = "RELATIVITY_CLIENT_ID"
	clientSecretEnvironmentName = "RELATIVITY_CLIENT_SECRET"
	expiryMarginSeconds         = 60
	csrfHeaderName              = "X-CSRF-Header"
	csrfHeaderValue             = "-"
	stateByteCount              = 16
	verifierByteCount           = 32
	serverHeaderTimeoutSeconds  = 10
	signInCookieName            = "relativity_sign_in"
	signInCookieMaxAgeSeconds   = 600
	signInTimeoutSeconds        = 300
)

type tokenResponse struct {
	AccessToken  string `json:"access_token"`
	RefreshToken string `json:"refresh_token"`
	ExpiresIn    int    `json:"expires_in"`
}

func requestToken(ctx context.Context, client *http.Client, form url.Values) (tokenResponse, error) {
	var payload tokenResponse
	request, err := http.NewRequestWithContext(ctx, http.MethodPost, identityTokenURL, strings.NewReader(form.Encode()))
	if err != nil {
		return payload, err
	}
	request.Header.Set("Content-Type", "application/x-www-form-urlencoded")
	response, err := client.Do(request)
	if err != nil {
		return payload, err
	}
	defer response.Body.Close()
	if response.StatusCode != http.StatusOK {
		return payload, fmt.Errorf("token endpoint answered %d", response.StatusCode)
	}
	err = json.NewDecoder(response.Body).Decode(&payload)
	return payload, err
}
```

**Sign in.** Replace the code that collected the person's credentials with a redirect to the authorize endpoint `<host>/Relativity/Identity/connect/authorize`, which is **verified by NSerio against a live instance; not documented by Relativity** and leads to RelativityOne's login page. The request carries the standard authorization-code parameters — `client_id`, `response_type=code`, the registered `redirect_uri`, `state` checked on return, and `code_challenge` with `code_challenge_method=S256` when PKCE is used; these are standard OAuth and Relativity documents none of them.

**PKCE.** Relativity's own documentation requires PKCE to obtain a refresh token; that a Code request that does not ask for a refresh token works without PKCE is **verified by NSerio against a live instance; not documented by Relativity**. **PKCE is not limited to clients that use refresh tokens**: NSerio recommends `S256` for every Authorization Code client (NSerio recommendation) and does not claim Relativity accepts any particular method. **Whether a client with refresh tokens disabled accepts a PKCE request (a `code_challenge`) is unverified**: test it before the block is relied on for such a client. **`state`** is generated, stored against the session and compared on return; `nonce` is not used, and Relativity does not document whether it enforces either. The authorize and token endpoints and the PKCE and `state` behaviour above are **verified by NSerio against a live instance; not documented by Relativity**. The block draws the verifier and `state` from `crypto/rand`, whose `Read` fills the buffer with cryptographically secure random bytes (the signature still returns an error, which the block checks), and computes the challenge with `crypto/sha256` and `base64.RawURLEncoding`, the unpadded URL-safe encoding; a 32-byte value encodes to a 43-character verifier.

**The client secret is required.** Code does not work without a client secret: tested and rejected, **verified by NSerio against a live instance; not documented by Relativity**, which confirms Relativity's own statement. **Any secret placed in a distributed binary is extractable by whoever holds the binary**: a Go program compiled for people other than its owner carries its client secret in a form anyone who holds the file can extract, and Relativity documents no recommendation for that case; use a short Access Token Lifetime, Trusted IPs where the outbound addresses are stable and one OAuth2 Client per distribution as compensating guidance (NSerio recommendation). Keep the secret as Secret Management (Part B of the Migration Documentation) describes, never in source or in a log.

**Exchange the code.** A form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=authorization_code`, the code, the same `redirect_uri`, the client ID and the secret, plus the `code_verifier` when PKCE is used.

```go
func randomToken(byteCount int) (string, error) {
	buffer := make([]byte, byteCount)
	if _, err := rand.Read(buffer); err != nil {
		return "", err
	}
	return base64.RawURLEncoding.EncodeToString(buffer), nil
}

func challengeOf(verifier string) string {
	digest := sha256.Sum256([]byte(verifier))
	return base64.RawURLEncoding.EncodeToString(digest[:])
}

func authorizeAddress(redirectURI string, state string, challenge string) string {
	query := url.Values{
		"client_id":     {os.Getenv(clientIDEnvironmentName)},
		"response_type": {"code"},
		"redirect_uri":  {redirectURI},
		"state":         {state},
	}
	if challenge != "" {
		query.Set("code_challenge", challenge)
		query.Set("code_challenge_method", "S256")
	}
	return identityAuthorizeURL + "?" + query.Encode()
}

func exchangeCode(ctx context.Context, client *http.Client, code string, redirectURI string, verifier string) (tokenResponse, error) {
	form := url.Values{
		"grant_type":    {"authorization_code"},
		"code":          {code},
		"redirect_uri":  {redirectURI},
		"client_id":     {os.Getenv(clientIDEnvironmentName)},
		"client_secret": {os.Getenv(clientSecretEnvironmentName)},
	}
	if verifier != "" {
		form.Set("code_verifier", verifier)
	}
	return requestToken(ctx, client, form)
}
```

**Redirect URL.** A server-side application registers **one exact HTTPS URL per environment, no wildcards**; the matching rules are not documented and no claim is made about them. A plain-HTTP loopback Redirect URL `http://127.0.0.1:<port>/<path>` on a **fixed registered port** is accepted, **verified by NSerio against a live instance; not documented by Relativity**, and is the command-line and desktop shape.

**On expiry.** With Allow Refresh Tokens enabled and PKCE used, the code holds a refresh token and sends `grant_type=refresh_token` in place of the code exchange: *inferred* from the standard OAuth shape, not tested; Relativity does not document a refresh token's lifetime, rotation or revocation, nor what disabling the client does to tokens already issued. **A refresh token is a secret**: hold it where the secret is held, never in a log. **Single sign-on:** sign-in happens on RelativityOne's own page, so the instance's single sign-on and two-factor rules apply unchanged (*inferred*). **The code stops collecting the password:** after this change nothing receives, stores or forwards the person's user name and password, and the code holds only tokens. Every call now runs as the person who signed in, so **the calling identity changes**. **Scope:** no scope set is prescribed for Code; `openid RelativityRequestOrigin UserInfo UserInfoAccess AuthenticationID RelativityWeb` works, **verified by NSerio against a live instance; not documented by Relativity**, as working and not as minimal.

**Variant 1 — a Go web server.** **Precondition: this variant is for a Go server that signs a person in to RelativityOne. If the code is a command-line tool or a desktop process, use variant 2.**

A server-side component creates `state` and the PKCE verifier, redirects to the authorize endpoint, receives the code at its registered HTTPS Redirect URL, exchanges it, keeps the tokens on the server and gives the browser **only its own session**; **the browser never holds anything from RelativityOne**. The two handlers use the standard library's `net/http`: `http.Redirect` replies to the request with a redirect to the authorize address.

```go
type WebSignIn struct {
	mutex       sync.Mutex
	pending     map[string]string
	client      *http.Client
	redirectURI string
	keepTokens  func(w http.ResponseWriter, r *http.Request, tokens tokenResponse)
}

func (signIn *WebSignIn) Login(w http.ResponseWriter, r *http.Request) {
	state, err := randomToken(stateByteCount)
	if err != nil {
		http.Error(w, "sign-in unavailable", http.StatusInternalServerError)
		return
	}
	verifier, err := randomToken(verifierByteCount)
	if err != nil {
		http.Error(w, "sign-in unavailable", http.StatusInternalServerError)
		return
	}
	signIn.mutex.Lock()
	signIn.pending[state] = verifier
	signIn.mutex.Unlock()
	http.SetCookie(w, &http.Cookie{
		Name:     signInCookieName,
		Value:    state,
		Path:     "/",
		MaxAge:   signInCookieMaxAgeSeconds,
		HttpOnly: true,
		Secure:   true,
		SameSite: http.SameSiteLaxMode,
	})
	http.Redirect(w, r, authorizeAddress(signIn.redirectURI, state, challengeOf(verifier)), http.StatusFound)
}

func (signIn *WebSignIn) Callback(w http.ResponseWriter, r *http.Request) {
	query := r.URL.Query()
	if query.Get("error") != "" {
		http.Error(w, "sign-in was not completed", http.StatusUnauthorized)
		return
	}
	state := query.Get("state")
	cookie, err := r.Cookie(signInCookieName)
	if err != nil || query.Get("code") == "" || subtle.ConstantTimeCompare([]byte(cookie.Value), []byte(state)) != 1 {
		http.Error(w, "unknown sign-in", http.StatusBadRequest)
		return
	}
	signIn.mutex.Lock()
	verifier, known := signIn.pending[state]
	delete(signIn.pending, state)
	signIn.mutex.Unlock()
	if !known {
		http.Error(w, "unknown sign-in", http.StatusBadRequest)
		return
	}
	http.SetCookie(w, &http.Cookie{Name: signInCookieName, Path: "/", MaxAge: -1})
	tokens, err := exchangeCode(r.Context(), signIn.client, query.Get("code"), signIn.redirectURI, verifier)
	if err != nil {
		http.Error(w, "sign-in failed", http.StatusBadGateway)
		return
	}
	signIn.keepTokens(w, r, tokens)
}
```

**`state` is bound to the browser session**: `Login` stores it in a short-lived, `HttpOnly`, `Secure`, `SameSite=Lax` cookie as well as in the server's pending table, and `Callback` fails (400) unless the cookie is present **and** equals the `state` on the query, so a callback that arrives without the matching cookie gets no exchange. `Callback` also **handles the `error` parameter** (401, no exchange) and **an empty `code`** (400). `state` is consumed once: the entry is deleted before the exchange and the cookie is cleared, so a replayed or unknown `state` gets no exchange. `keepTokens` is yours: store the tokens against the visitor's session on the server and set a session cookie that holds **only a session identifier**. The refresh call, if used, goes to the same token endpoint with `grant_type=refresh_token` (*inferred*). **The calling identity changes** to the person who signed in.

**Variant 2 — a command-line tool or a desktop process.** **Precondition: this variant is for a Go command-line tool or a desktop process that signs a person in to RelativityOne. If the code is a server, use variant 1.**

The process starts a listener from the standard library bound to `127.0.0.1` only, on the **fixed port the administrator registered** (the exact URL is registered, so the port is fixed, chosen by the administrator and not at runtime), for **one request**; it **prints the authorize address** (so a machine with no browser, an SSH session or a failed launch has the manual route) and, if you want, opens the system browser yourself; reads `code` and `state` from the one request, answers with a short page, closes the listener, checks `state` and exchanges the code.

```go
func SignInFromCommandLine(ctx context.Context, client *http.Client, listenAddress string, redirectURI string) (tokenResponse, error) {
	var tokens tokenResponse
	redirect, err := url.Parse(redirectURI)
	if err != nil {
		return tokens, err
	}
	host, _, err := net.SplitHostPort(listenAddress)
	if err != nil {
		return tokens, err
	}
	if host != "127.0.0.1" && host != "::1" {
		return tokens, errors.New("the sign-in listener must be bound to the loopback address")
	}
	ctx, cancel := context.WithTimeout(ctx, signInTimeoutSeconds*time.Second)
	defer cancel()
	state, err := randomToken(stateByteCount)
	if err != nil {
		return tokens, err
	}
	verifier, err := randomToken(verifierByteCount)
	if err != nil {
		return tokens, err
	}
	listener, err := net.Listen("tcp", listenAddress)
	if err != nil {
		return tokens, fmt.Errorf("the registered port is not available: %w", err)
	}
	type callback struct{ code, state, failure string }
	received := make(chan callback, 1)
	server := &http.Server{
		ReadHeaderTimeout: serverHeaderTimeoutSeconds * time.Second,
		Handler: http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
			if r.URL.Path != redirect.Path {
				http.NotFound(w, r)
				return
			}
			query := r.URL.Query()
			select {
			case received <- callback{code: query.Get("code"), state: query.Get("state"), failure: query.Get("error")}:
			default:
			}
			fmt.Fprintln(w, "Sign-in received. You can close this window.")
		}),
	}
	go func() { _ = server.Serve(listener) }()
	defer server.Shutdown(context.Background())
	fmt.Println("Open this address in a browser to sign in:", authorizeAddress(redirectURI, state, challengeOf(verifier)))
	select {
	case result := <-received:
		if result.failure != "" || result.code == "" {
			return tokens, errors.New("sign-in was not completed")
		}
		if subtle.ConstantTimeCompare([]byte(result.state), []byte(state)) != 1 {
			return tokens, errors.New("the sign-in answer does not match the request")
		}
		return exchangeCode(ctx, client, result.code, redirectURI, verifier)
	case <-ctx.Done():
		return tokens, ctx.Err()
	}
}
```

**The listener is checked and bounded in the code**: it **refuses a `listenAddress` whose host is not `127.0.0.1` or `::1`** (loopback only); its handler **answers 404 to every path other than the registered redirect path** (so a stray request cannot consume the one-shot channel); the whole sign-in has **its own deadline** (`signInTimeoutSeconds`, 300 seconds, on top of the caller's context), after which it returns the context error; a callback with an `error` parameter or an empty `code` is an error, not an exchange. **Busy port:** the port is the registered one and is **not** replaced by another (a different port is a different Redirect URL, which Relativity would not match); if `net.Listen` fails because the port is taken, the function returns the error `the registered port is not available` and the tool must report it, not retry on another port. Call it with `listenAddress` `127.0.0.1:<the registered port>` and `redirectURI` `http://127.0.0.1:<the registered port>/<path>`. **Where the tokens are kept** is the program's decision: in memory for the run, or, for a refresh token, in a file whose permissions you restrict (**a distributed command-line tool carries its client secret in a form anyone who holds the distribution can extract**). Every call now runs as the person who signed in.

Verification: **Verification note.** The tool read the encoder, **not where the encoded value goes**: confirm that it is a request to RelativityOne. **Other uses may not appear as findings**: a pair built in another statement is not read. **A token has a finite lifetime.** **The calling identity changes.**

OAuth2 Client: Create or confirm this OAuth2 Client in RelativityOne (NSerio's observation: the menu path is Home > Authentication > OAuth2 Client Manager, as Relativity's REST API authentication page names it). This tool never connects to RelativityOne and never creates or changes the OAuth2 Client; the settings below are guidance.
- Name: a descriptive name identifying the application it serves.
- Flow Grant Type: Code
- Redirect URLs: Relativity documentation: each URL includes the http or https protocol prefix. The exact callback URL is registered by the Relativity administrator. Server-side web applications, NSerio's recommendation: one exact HTTPS URL per environment, with no wildcards. Desktop and CLI applications: a plain-HTTP loopback URL, `http://127.0.0.1:<port>/<path>`, on a fixed port because the URL is registered in advance and the port cannot be chosen at runtime (verified by NSerio against a live instance; not documented by Relativity).
- Access Token Lifetime: 60 minutes, stated in minutes because the field is in minutes. Relativity documentation: tokens for this flow must have a short lifetime, and the recommended value matches the Identity Server default.
- Allow refresh tokens: NSerio's recommendation: leave it off unless the application must keep a user session active beyond the access token lifetime without asking the user to sign in again. If it is on, the refresh tokens are stored as secrets, per Secret Management (Part B of the Migration Documentation).
- Enabled: on.
<!-- generated:end GO-006 authorization-code -->

### GO-007 · UserinfoUserPassword

<a id="go-007-migration"></a>

Remove the `Userinfo` and send the token in the `Authorization` header instead. The tool did not establish where the `Userinfo` goes: if the address reaches a request, the client turns it into a Basic header; if it builds a database or broker address, the finding is informational. The standard library's documentation says that `UserPassword` should only be used with legacy web sites.

#### Client Credentials

<!-- generated:begin GO-007 client-credentials -->
**Confirming the flow.** When no signal in a file decides the flow, the tool assigns Client Credentials, the default, with this note: "Who supplies the credential at run time? If a person types it or signs in, the integration acts for that person: use Authorization Code. If it comes from configuration, settings, an environment variable or a secret store: use Client Credentials. No signal in this file showed which applies, so Client Credentials, the default, is recommended. Use Authorization Code instead if the integration acts on behalf of a signed-in user, or if your RelativityOne instance already has a Code-flow OAuth2 Client for this integration." A fixed-credential signal assigns it with this note: "A fixed-credential signal in this file points to Client Credentials, but how often that kind of signal is right has not been measured. Use Authorization Code instead if the integration acts on behalf of a signed-in user, or if your RelativityOne instance already has a Code-flow OAuth2 Client for this integration." See [Confirming the flow](#confirming-the-flow) and the "Which flow" paragraph of GO-007 in [Part A](part-a-go.md#go-007).

Source form: **Applies to:** every Go version; a `Userinfo` built with `url.UserPassword`.

```go
address.User = url.UserPassword(userName, secret)
```

Under token authentication: **First, the destination:** if this Relativity instance is not RelativityOne — including a Relativity Server instance, whose own documentation governs — this finding is informational and the rest of this template does not apply.

```go
address.User = nil
request.Header.Set("Authorization", "Bearer "+accessToken)
request.Header.Set(csrfHeaderName, csrfHeaderValue)
```

Remove the `Userinfo` and send the token in the header.

Credentials: **Where the credentials come from.** The source form establishes that the code sends Basic, not that the destination is RelativityOne. If it is not RelativityOne, the migration is that service's own token mechanism, this finding is informational for this tool, and the reader stops here. If it is RelativityOne, the token comes from the token-acquisition part that follows, and the user name and password are replaced by the OAuth2 Client's client ID and secret, kept as Secret Management (Part B of the Migration Documentation) describes and never in source.

**Where the token comes from.** Create an OAuth2 Client with Flow Grant Type set to Client Credentials (part 3 lists its settings), then send a form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=client_credentials`, `client_id` and `client_secret`. The token endpoint is **documented** for this grant. The token is then sent as `Authorization: Bearer <token>`, with `X-CSRF-Header: -` on REST and Kepler calls. The scope depends on what the integration needs and **no single scope is required**: Relativity's documentation shows `SystemUserInfo` and `UserInfoAccess` and no page says which is correct or what either grants; use the value the API you call documents, and confirm it. Every block below uses only the standard library and needs go1.13 or later.

**Shared declarations.** Every block of this text, and of the use-site forms above, uses these constants and helpers; `HOST` is the RelativityOne host.

```go
const (
	identityTokenURL            = "https://HOST/Relativity/Identity/connect/token"
	identityAuthorizeURL        = "https://HOST/Relativity/Identity/connect/authorize"
	clientIDEnvironmentName     = "RELATIVITY_CLIENT_ID"
	clientSecretEnvironmentName = "RELATIVITY_CLIENT_SECRET"
	expiryMarginSeconds         = 60
	csrfHeaderName              = "X-CSRF-Header"
	csrfHeaderValue             = "-"
	stateByteCount              = 16
	verifierByteCount           = 32
	serverHeaderTimeoutSeconds  = 10
	signInCookieName            = "relativity_sign_in"
	signInCookieMaxAgeSeconds   = 600
	signInTimeoutSeconds        = 300
)

type tokenResponse struct {
	AccessToken  string `json:"access_token"`
	RefreshToken string `json:"refresh_token"`
	ExpiresIn    int    `json:"expires_in"`
}

func requestToken(ctx context.Context, client *http.Client, form url.Values) (tokenResponse, error) {
	var payload tokenResponse
	request, err := http.NewRequestWithContext(ctx, http.MethodPost, identityTokenURL, strings.NewReader(form.Encode()))
	if err != nil {
		return payload, err
	}
	request.Header.Set("Content-Type", "application/x-www-form-urlencoded")
	response, err := client.Do(request)
	if err != nil {
		return payload, err
	}
	defer response.Body.Close()
	if response.StatusCode != http.StatusOK {
		return payload, fmt.Errorf("token endpoint answered %d", response.StatusCode)
	}
	err = json.NewDecoder(response.Body).Decode(&payload)
	return payload, err
}
```

The token request is a form-encoded `POST`. **Where the client secret travels in the token request** (the form body, as here, or HTTP Basic client authentication) **and the field names of the token response are not documented by Relativity**: the form body and `tokenResponse` are the standard OAuth shape and an NSerio recommendation, **not verified** against RelativityOne. **Do not** send the client secret with `SetBasicAuth` and **do not** put it in the URL.

**Variant: a Go process acting as itself.** **Precondition: this variant is for a Go job, daemon or service that does not act for a signed-in person.**

```go
type TokenSource struct {
	mutex     sync.Mutex
	client    *http.Client
	token     string
	expiresAt time.Time
}

func (source *TokenSource) Token(ctx context.Context) (string, error) {
	source.mutex.Lock()
	defer source.mutex.Unlock()
	if source.token == "" || time.Until(source.expiresAt) < expiryMarginSeconds*time.Second {
		form := url.Values{
			"grant_type":    {"client_credentials"},
			"client_id":     {os.Getenv(clientIDEnvironmentName)},
			"client_secret": {os.Getenv(clientSecretEnvironmentName)},
		}
		payload, err := requestToken(ctx, source.client, form)
		if err != nil {
			return "", err
		}
		source.token = payload.AccessToken
		source.expiresAt = time.Now().Add(time.Duration(payload.ExpiresIn) * time.Second)
	}
	return source.token, nil
}
```

**Cache the token and re-acquire it on expiry — do not refresh.** Client Credentials does not offer refresh tokens: **verified by NSerio against a live instance; not documented by Relativity** — the Allow Refresh Tokens control does not appear for this grant in the RelativityOne user interface. The recommended Access Token Lifetime is 60 minutes. **The client secret** belongs in the process environment or a secret store, as Secret Management (Part B of the Migration Documentation) describes, **never in source, in a tracked `.env` file or in a log**: the block reads it with `os.Getenv`, which returns an empty string when the variable is not present, so an unset variable is an empty secret and the token request fails, which the code reports. A reset invalidates the previous secret immediately, so plan the cut-over; a second client created before the reset is an inferred way to do it without a gap. **Before deploying, confirm the calling identity:** every call now runs as the OAuth2 Client's Context User, not as the user in the replaced credential, and a non-administrator Context User is NSerio's recommendation. Trusted IPs restrict where a token can be requested, not a token already issued. The statements that Client Credentials needs a client secret and that the secret is displayed once are carried from the other languages' guidance and are not verified for this text.

**Variant: a Go web service that does not act for a signed-in user.** **Precondition: this variant is for a Go server that calls RelativityOne with no person signed in to it, so no person's identity exists to preserve. If a person signs in, use Authorization Code, variant 1.**

Use the block above in the server process; **the server must authenticate and authorize its own callers, and the OAuth2 Client's Context User must hold the minimum permissions the operation needs. Without both, the result is an unauthenticated relay into RelativityOne**: every caller reaching it acts with the Context User's full rights, which is strictly worse than the starting point. The handler returns results to its caller, **never the token** and never the secret.

Verification: **Verification note.** The tool did not establish where the `Userinfo` goes: if the `URL` reaches a request, the client turns it into a Basic header; if it builds a database or broker address, this finding is informational. The standard library's documentation says `UserPassword` "should only be used with legacy web sites". **A token has a finite lifetime.** **The calling identity changes.**

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
<!-- generated:end GO-007 client-credentials -->

#### Authorization Code

<!-- generated:begin GO-007 authorization-code -->
**Confirming the flow.** An interactive-user signal in a file assigns Authorization Code, with this note: "An interactive-user signal in this file points to Authorization Code. Confirm the flow: use Client Credentials instead if the integration runs with credentials fixed for the integration and no signed-in user." Your user profile can also make Authorization Code the default; see Changing the default in [Confirming the flow](#confirming-the-flow). See [Confirming the flow](#confirming-the-flow) and the "Which flow" paragraph of GO-007 in [Part A](part-a-go.md#go-007).

Source form: **Applies to:** every Go version; a `Userinfo` built with `url.UserPassword`.

```go
address.User = url.UserPassword(userName, secret)
```

Under token authentication: **First, the destination:** if this Relativity instance is not RelativityOne — including a Relativity Server instance, whose own documentation governs — this finding is informational and the rest of this template does not apply.

```go
address.User = nil
request.Header.Set("Authorization", "Bearer "+accessToken)
request.Header.Set(csrfHeaderName, csrfHeaderValue)
```

Remove the `Userinfo` and send the token in the header.

Credentials: **Where the credentials come from.** The source form establishes that the code sends Basic, not that the destination is RelativityOne. If it is not RelativityOne, the migration is that service's own token mechanism, this finding is informational for this tool, and the reader stops here. If it is RelativityOne, the token comes from the token-acquisition part that follows, and the user name and password are replaced by the OAuth2 Client's client ID and secret, kept as Secret Management (Part B of the Migration Documentation) describes and never in source.

**Common to both variants.** The person signs in on RelativityOne's own page and the code never holds their password. The text has a common part and two variants by application type, and each variant opens with its own precondition, so that the reader can discard the others by reading one sentence each. Every block uses only the standard library and needs go1.13 or later; the shared declarations are those of the Client Credentials text and are repeated here.

```go
const (
	identityTokenURL            = "https://HOST/Relativity/Identity/connect/token"
	identityAuthorizeURL        = "https://HOST/Relativity/Identity/connect/authorize"
	clientIDEnvironmentName     = "RELATIVITY_CLIENT_ID"
	clientSecretEnvironmentName = "RELATIVITY_CLIENT_SECRET"
	expiryMarginSeconds         = 60
	csrfHeaderName              = "X-CSRF-Header"
	csrfHeaderValue             = "-"
	stateByteCount              = 16
	verifierByteCount           = 32
	serverHeaderTimeoutSeconds  = 10
	signInCookieName            = "relativity_sign_in"
	signInCookieMaxAgeSeconds   = 600
	signInTimeoutSeconds        = 300
)

type tokenResponse struct {
	AccessToken  string `json:"access_token"`
	RefreshToken string `json:"refresh_token"`
	ExpiresIn    int    `json:"expires_in"`
}

func requestToken(ctx context.Context, client *http.Client, form url.Values) (tokenResponse, error) {
	var payload tokenResponse
	request, err := http.NewRequestWithContext(ctx, http.MethodPost, identityTokenURL, strings.NewReader(form.Encode()))
	if err != nil {
		return payload, err
	}
	request.Header.Set("Content-Type", "application/x-www-form-urlencoded")
	response, err := client.Do(request)
	if err != nil {
		return payload, err
	}
	defer response.Body.Close()
	if response.StatusCode != http.StatusOK {
		return payload, fmt.Errorf("token endpoint answered %d", response.StatusCode)
	}
	err = json.NewDecoder(response.Body).Decode(&payload)
	return payload, err
}
```

**Sign in.** Replace the code that collected the person's credentials with a redirect to the authorize endpoint `<host>/Relativity/Identity/connect/authorize`, which is **verified by NSerio against a live instance; not documented by Relativity** and leads to RelativityOne's login page. The request carries the standard authorization-code parameters — `client_id`, `response_type=code`, the registered `redirect_uri`, `state` checked on return, and `code_challenge` with `code_challenge_method=S256` when PKCE is used; these are standard OAuth and Relativity documents none of them.

**PKCE.** Relativity's own documentation requires PKCE to obtain a refresh token; that a Code request that does not ask for a refresh token works without PKCE is **verified by NSerio against a live instance; not documented by Relativity**. **PKCE is not limited to clients that use refresh tokens**: NSerio recommends `S256` for every Authorization Code client (NSerio recommendation) and does not claim Relativity accepts any particular method. **Whether a client with refresh tokens disabled accepts a PKCE request (a `code_challenge`) is unverified**: test it before the block is relied on for such a client. **`state`** is generated, stored against the session and compared on return; `nonce` is not used, and Relativity does not document whether it enforces either. The authorize and token endpoints and the PKCE and `state` behaviour above are **verified by NSerio against a live instance; not documented by Relativity**. The block draws the verifier and `state` from `crypto/rand`, whose `Read` fills the buffer with cryptographically secure random bytes (the signature still returns an error, which the block checks), and computes the challenge with `crypto/sha256` and `base64.RawURLEncoding`, the unpadded URL-safe encoding; a 32-byte value encodes to a 43-character verifier.

**The client secret is required.** Code does not work without a client secret: tested and rejected, **verified by NSerio against a live instance; not documented by Relativity**, which confirms Relativity's own statement. **Any secret placed in a distributed binary is extractable by whoever holds the binary**: a Go program compiled for people other than its owner carries its client secret in a form anyone who holds the file can extract, and Relativity documents no recommendation for that case; use a short Access Token Lifetime, Trusted IPs where the outbound addresses are stable and one OAuth2 Client per distribution as compensating guidance (NSerio recommendation). Keep the secret as Secret Management (Part B of the Migration Documentation) describes, never in source or in a log.

**Exchange the code.** A form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=authorization_code`, the code, the same `redirect_uri`, the client ID and the secret, plus the `code_verifier` when PKCE is used.

```go
func randomToken(byteCount int) (string, error) {
	buffer := make([]byte, byteCount)
	if _, err := rand.Read(buffer); err != nil {
		return "", err
	}
	return base64.RawURLEncoding.EncodeToString(buffer), nil
}

func challengeOf(verifier string) string {
	digest := sha256.Sum256([]byte(verifier))
	return base64.RawURLEncoding.EncodeToString(digest[:])
}

func authorizeAddress(redirectURI string, state string, challenge string) string {
	query := url.Values{
		"client_id":     {os.Getenv(clientIDEnvironmentName)},
		"response_type": {"code"},
		"redirect_uri":  {redirectURI},
		"state":         {state},
	}
	if challenge != "" {
		query.Set("code_challenge", challenge)
		query.Set("code_challenge_method", "S256")
	}
	return identityAuthorizeURL + "?" + query.Encode()
}

func exchangeCode(ctx context.Context, client *http.Client, code string, redirectURI string, verifier string) (tokenResponse, error) {
	form := url.Values{
		"grant_type":    {"authorization_code"},
		"code":          {code},
		"redirect_uri":  {redirectURI},
		"client_id":     {os.Getenv(clientIDEnvironmentName)},
		"client_secret": {os.Getenv(clientSecretEnvironmentName)},
	}
	if verifier != "" {
		form.Set("code_verifier", verifier)
	}
	return requestToken(ctx, client, form)
}
```

**Redirect URL.** A server-side application registers **one exact HTTPS URL per environment, no wildcards**; the matching rules are not documented and no claim is made about them. A plain-HTTP loopback Redirect URL `http://127.0.0.1:<port>/<path>` on a **fixed registered port** is accepted, **verified by NSerio against a live instance; not documented by Relativity**, and is the command-line and desktop shape.

**On expiry.** With Allow Refresh Tokens enabled and PKCE used, the code holds a refresh token and sends `grant_type=refresh_token` in place of the code exchange: *inferred* from the standard OAuth shape, not tested; Relativity does not document a refresh token's lifetime, rotation or revocation, nor what disabling the client does to tokens already issued. **A refresh token is a secret**: hold it where the secret is held, never in a log. **Single sign-on:** sign-in happens on RelativityOne's own page, so the instance's single sign-on and two-factor rules apply unchanged (*inferred*). **The code stops collecting the password:** after this change nothing receives, stores or forwards the person's user name and password, and the code holds only tokens. Every call now runs as the person who signed in, so **the calling identity changes**. **Scope:** no scope set is prescribed for Code; `openid RelativityRequestOrigin UserInfo UserInfoAccess AuthenticationID RelativityWeb` works, **verified by NSerio against a live instance; not documented by Relativity**, as working and not as minimal.

**Variant 1 — a Go web server.** **Precondition: this variant is for a Go server that signs a person in to RelativityOne. If the code is a command-line tool or a desktop process, use variant 2.**

A server-side component creates `state` and the PKCE verifier, redirects to the authorize endpoint, receives the code at its registered HTTPS Redirect URL, exchanges it, keeps the tokens on the server and gives the browser **only its own session**; **the browser never holds anything from RelativityOne**. The two handlers use the standard library's `net/http`: `http.Redirect` replies to the request with a redirect to the authorize address.

```go
type WebSignIn struct {
	mutex       sync.Mutex
	pending     map[string]string
	client      *http.Client
	redirectURI string
	keepTokens  func(w http.ResponseWriter, r *http.Request, tokens tokenResponse)
}

func (signIn *WebSignIn) Login(w http.ResponseWriter, r *http.Request) {
	state, err := randomToken(stateByteCount)
	if err != nil {
		http.Error(w, "sign-in unavailable", http.StatusInternalServerError)
		return
	}
	verifier, err := randomToken(verifierByteCount)
	if err != nil {
		http.Error(w, "sign-in unavailable", http.StatusInternalServerError)
		return
	}
	signIn.mutex.Lock()
	signIn.pending[state] = verifier
	signIn.mutex.Unlock()
	http.SetCookie(w, &http.Cookie{
		Name:     signInCookieName,
		Value:    state,
		Path:     "/",
		MaxAge:   signInCookieMaxAgeSeconds,
		HttpOnly: true,
		Secure:   true,
		SameSite: http.SameSiteLaxMode,
	})
	http.Redirect(w, r, authorizeAddress(signIn.redirectURI, state, challengeOf(verifier)), http.StatusFound)
}

func (signIn *WebSignIn) Callback(w http.ResponseWriter, r *http.Request) {
	query := r.URL.Query()
	if query.Get("error") != "" {
		http.Error(w, "sign-in was not completed", http.StatusUnauthorized)
		return
	}
	state := query.Get("state")
	cookie, err := r.Cookie(signInCookieName)
	if err != nil || query.Get("code") == "" || subtle.ConstantTimeCompare([]byte(cookie.Value), []byte(state)) != 1 {
		http.Error(w, "unknown sign-in", http.StatusBadRequest)
		return
	}
	signIn.mutex.Lock()
	verifier, known := signIn.pending[state]
	delete(signIn.pending, state)
	signIn.mutex.Unlock()
	if !known {
		http.Error(w, "unknown sign-in", http.StatusBadRequest)
		return
	}
	http.SetCookie(w, &http.Cookie{Name: signInCookieName, Path: "/", MaxAge: -1})
	tokens, err := exchangeCode(r.Context(), signIn.client, query.Get("code"), signIn.redirectURI, verifier)
	if err != nil {
		http.Error(w, "sign-in failed", http.StatusBadGateway)
		return
	}
	signIn.keepTokens(w, r, tokens)
}
```

**`state` is bound to the browser session**: `Login` stores it in a short-lived, `HttpOnly`, `Secure`, `SameSite=Lax` cookie as well as in the server's pending table, and `Callback` fails (400) unless the cookie is present **and** equals the `state` on the query, so a callback that arrives without the matching cookie gets no exchange. `Callback` also **handles the `error` parameter** (401, no exchange) and **an empty `code`** (400). `state` is consumed once: the entry is deleted before the exchange and the cookie is cleared, so a replayed or unknown `state` gets no exchange. `keepTokens` is yours: store the tokens against the visitor's session on the server and set a session cookie that holds **only a session identifier**. The refresh call, if used, goes to the same token endpoint with `grant_type=refresh_token` (*inferred*). **The calling identity changes** to the person who signed in.

**Variant 2 — a command-line tool or a desktop process.** **Precondition: this variant is for a Go command-line tool or a desktop process that signs a person in to RelativityOne. If the code is a server, use variant 1.**

The process starts a listener from the standard library bound to `127.0.0.1` only, on the **fixed port the administrator registered** (the exact URL is registered, so the port is fixed, chosen by the administrator and not at runtime), for **one request**; it **prints the authorize address** (so a machine with no browser, an SSH session or a failed launch has the manual route) and, if you want, opens the system browser yourself; reads `code` and `state` from the one request, answers with a short page, closes the listener, checks `state` and exchanges the code.

```go
func SignInFromCommandLine(ctx context.Context, client *http.Client, listenAddress string, redirectURI string) (tokenResponse, error) {
	var tokens tokenResponse
	redirect, err := url.Parse(redirectURI)
	if err != nil {
		return tokens, err
	}
	host, _, err := net.SplitHostPort(listenAddress)
	if err != nil {
		return tokens, err
	}
	if host != "127.0.0.1" && host != "::1" {
		return tokens, errors.New("the sign-in listener must be bound to the loopback address")
	}
	ctx, cancel := context.WithTimeout(ctx, signInTimeoutSeconds*time.Second)
	defer cancel()
	state, err := randomToken(stateByteCount)
	if err != nil {
		return tokens, err
	}
	verifier, err := randomToken(verifierByteCount)
	if err != nil {
		return tokens, err
	}
	listener, err := net.Listen("tcp", listenAddress)
	if err != nil {
		return tokens, fmt.Errorf("the registered port is not available: %w", err)
	}
	type callback struct{ code, state, failure string }
	received := make(chan callback, 1)
	server := &http.Server{
		ReadHeaderTimeout: serverHeaderTimeoutSeconds * time.Second,
		Handler: http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
			if r.URL.Path != redirect.Path {
				http.NotFound(w, r)
				return
			}
			query := r.URL.Query()
			select {
			case received <- callback{code: query.Get("code"), state: query.Get("state"), failure: query.Get("error")}:
			default:
			}
			fmt.Fprintln(w, "Sign-in received. You can close this window.")
		}),
	}
	go func() { _ = server.Serve(listener) }()
	defer server.Shutdown(context.Background())
	fmt.Println("Open this address in a browser to sign in:", authorizeAddress(redirectURI, state, challengeOf(verifier)))
	select {
	case result := <-received:
		if result.failure != "" || result.code == "" {
			return tokens, errors.New("sign-in was not completed")
		}
		if subtle.ConstantTimeCompare([]byte(result.state), []byte(state)) != 1 {
			return tokens, errors.New("the sign-in answer does not match the request")
		}
		return exchangeCode(ctx, client, result.code, redirectURI, verifier)
	case <-ctx.Done():
		return tokens, ctx.Err()
	}
}
```

**The listener is checked and bounded in the code**: it **refuses a `listenAddress` whose host is not `127.0.0.1` or `::1`** (loopback only); its handler **answers 404 to every path other than the registered redirect path** (so a stray request cannot consume the one-shot channel); the whole sign-in has **its own deadline** (`signInTimeoutSeconds`, 300 seconds, on top of the caller's context), after which it returns the context error; a callback with an `error` parameter or an empty `code` is an error, not an exchange. **Busy port:** the port is the registered one and is **not** replaced by another (a different port is a different Redirect URL, which Relativity would not match); if `net.Listen` fails because the port is taken, the function returns the error `the registered port is not available` and the tool must report it, not retry on another port. Call it with `listenAddress` `127.0.0.1:<the registered port>` and `redirectURI` `http://127.0.0.1:<the registered port>/<path>`. **Where the tokens are kept** is the program's decision: in memory for the run, or, for a refresh token, in a file whose permissions you restrict (**a distributed command-line tool carries its client secret in a form anyone who holds the distribution can extract**). Every call now runs as the person who signed in.

Verification: **Verification note.** The tool did not establish where the `Userinfo` goes: if the `URL` reaches a request, the client turns it into a Basic header; if it builds a database or broker address, this finding is informational. The standard library's documentation says `UserPassword` "should only be used with legacy web sites". **A token has a finite lifetime.** **The calling identity changes.**

OAuth2 Client: Create or confirm this OAuth2 Client in RelativityOne (NSerio's observation: the menu path is Home > Authentication > OAuth2 Client Manager, as Relativity's REST API authentication page names it). This tool never connects to RelativityOne and never creates or changes the OAuth2 Client; the settings below are guidance.
- Name: a descriptive name identifying the application it serves.
- Flow Grant Type: Code
- Redirect URLs: Relativity documentation: each URL includes the http or https protocol prefix. The exact callback URL is registered by the Relativity administrator. Server-side web applications, NSerio's recommendation: one exact HTTPS URL per environment, with no wildcards. Desktop and CLI applications: a plain-HTTP loopback URL, `http://127.0.0.1:<port>/<path>`, on a fixed port because the URL is registered in advance and the port cannot be chosen at runtime (verified by NSerio against a live instance; not documented by Relativity).
- Access Token Lifetime: 60 minutes, stated in minutes because the field is in minutes. Relativity documentation: tokens for this flow must have a short lifetime, and the recommended value matches the Identity Server default.
- Allow refresh tokens: NSerio's recommendation: leave it off unless the application must keep a user session active beyond the access token lifetime without asking the user to sign in again. If it is on, the refresh tokens are stored as secrets, per Secret Management (Part B of the Migration Documentation).
- Enabled: on.
<!-- generated:end GO-007 authorization-code -->

### GO-008 · URLUserinfoHeldLiteral

<a id="go-008-migration"></a>

Remove the credentials from the held address and add the Bearer header to the request that uses the parsed address: build the request with `http.NewRequestWithContext` and set `Authorization` and `X-CSRF-Header` on it. The address is also held in the repository history: apply the rotation statement below.

#### Client Credentials

<!-- generated:begin GO-008 client-credentials -->
**Confirming the flow.** When no signal in a file decides the flow, the tool assigns Client Credentials, the default, with this note: "Who supplies the credential at run time? If a person types it or signs in, the integration acts for that person: use Authorization Code. If it comes from configuration, settings, an environment variable or a secret store: use Client Credentials. No signal in this file showed which applies, so Client Credentials, the default, is recommended. Use Authorization Code instead if the integration acts on behalf of a signed-in user, or if your RelativityOne instance already has a Code-flow OAuth2 Client for this integration." A fixed-credential signal assigns it with this note: "A fixed-credential signal in this file points to Client Credentials, but how often that kind of signal is right has not been measured. Use Authorization Code instead if the integration acts on behalf of a signed-in user, or if your RelativityOne instance already has a Code-flow OAuth2 Client for this integration." See [Confirming the flow](#confirming-the-flow) and the "Which flow" paragraph of GO-008 in [Part A](part-a-go.md#go-008).

Source form: **Applies to:** every Go version; a held URL literal that carries a user and a password.

```go
return url.Parse("https://user:secret@host.example/path")
```

Under token authentication: **First, the destination:** if this Relativity instance is not RelativityOne — including a Relativity Server instance, whose own documentation governs — this finding is informational and the rest of this template does not apply.

```go
return url.Parse("https://host.example/path")
```

Remove the credentials from the literal and add the Bearer header to the request that uses the parsed address: build the request with `http.NewRequestWithContext` and set `Authorization` and `X-CSRF-Header` on it.

Credentials: **Where the credentials come from.** The source form establishes that the code sends Basic, not that the destination is RelativityOne. If it is not RelativityOne, the migration is that service's own token mechanism, this finding is informational for this tool, and the reader stops here. If it is RelativityOne, the token comes from the token-acquisition part that follows, and the user name and password are replaced by the OAuth2 Client's client ID and secret, kept as Secret Management (Part B of the Migration Documentation) describes and never in source. **Treat this credential as compromised. Rotate it, and assume the repository history has published it. Completing the migration does not close this exposure; rotation does.**

**Where the token comes from.** Create an OAuth2 Client with Flow Grant Type set to Client Credentials (part 3 lists its settings), then send a form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=client_credentials`, `client_id` and `client_secret`. The token endpoint is **documented** for this grant. The token is then sent as `Authorization: Bearer <token>`, with `X-CSRF-Header: -` on REST and Kepler calls. The scope depends on what the integration needs and **no single scope is required**: Relativity's documentation shows `SystemUserInfo` and `UserInfoAccess` and no page says which is correct or what either grants; use the value the API you call documents, and confirm it. Every block below uses only the standard library and needs go1.13 or later.

**Shared declarations.** Every block of this text, and of the use-site forms above, uses these constants and helpers; `HOST` is the RelativityOne host.

```go
const (
	identityTokenURL            = "https://HOST/Relativity/Identity/connect/token"
	identityAuthorizeURL        = "https://HOST/Relativity/Identity/connect/authorize"
	clientIDEnvironmentName     = "RELATIVITY_CLIENT_ID"
	clientSecretEnvironmentName = "RELATIVITY_CLIENT_SECRET"
	expiryMarginSeconds         = 60
	csrfHeaderName              = "X-CSRF-Header"
	csrfHeaderValue             = "-"
	stateByteCount              = 16
	verifierByteCount           = 32
	serverHeaderTimeoutSeconds  = 10
	signInCookieName            = "relativity_sign_in"
	signInCookieMaxAgeSeconds   = 600
	signInTimeoutSeconds        = 300
)

type tokenResponse struct {
	AccessToken  string `json:"access_token"`
	RefreshToken string `json:"refresh_token"`
	ExpiresIn    int    `json:"expires_in"`
}

func requestToken(ctx context.Context, client *http.Client, form url.Values) (tokenResponse, error) {
	var payload tokenResponse
	request, err := http.NewRequestWithContext(ctx, http.MethodPost, identityTokenURL, strings.NewReader(form.Encode()))
	if err != nil {
		return payload, err
	}
	request.Header.Set("Content-Type", "application/x-www-form-urlencoded")
	response, err := client.Do(request)
	if err != nil {
		return payload, err
	}
	defer response.Body.Close()
	if response.StatusCode != http.StatusOK {
		return payload, fmt.Errorf("token endpoint answered %d", response.StatusCode)
	}
	err = json.NewDecoder(response.Body).Decode(&payload)
	return payload, err
}
```

The token request is a form-encoded `POST`. **Where the client secret travels in the token request** (the form body, as here, or HTTP Basic client authentication) **and the field names of the token response are not documented by Relativity**: the form body and `tokenResponse` are the standard OAuth shape and an NSerio recommendation, **not verified** against RelativityOne. **Do not** send the client secret with `SetBasicAuth` and **do not** put it in the URL.

**Variant: a Go process acting as itself.** **Precondition: this variant is for a Go job, daemon or service that does not act for a signed-in person.**

```go
type TokenSource struct {
	mutex     sync.Mutex
	client    *http.Client
	token     string
	expiresAt time.Time
}

func (source *TokenSource) Token(ctx context.Context) (string, error) {
	source.mutex.Lock()
	defer source.mutex.Unlock()
	if source.token == "" || time.Until(source.expiresAt) < expiryMarginSeconds*time.Second {
		form := url.Values{
			"grant_type":    {"client_credentials"},
			"client_id":     {os.Getenv(clientIDEnvironmentName)},
			"client_secret": {os.Getenv(clientSecretEnvironmentName)},
		}
		payload, err := requestToken(ctx, source.client, form)
		if err != nil {
			return "", err
		}
		source.token = payload.AccessToken
		source.expiresAt = time.Now().Add(time.Duration(payload.ExpiresIn) * time.Second)
	}
	return source.token, nil
}
```

**Cache the token and re-acquire it on expiry — do not refresh.** Client Credentials does not offer refresh tokens: **verified by NSerio against a live instance; not documented by Relativity** — the Allow Refresh Tokens control does not appear for this grant in the RelativityOne user interface. The recommended Access Token Lifetime is 60 minutes. **The client secret** belongs in the process environment or a secret store, as Secret Management (Part B of the Migration Documentation) describes, **never in source, in a tracked `.env` file or in a log**: the block reads it with `os.Getenv`, which returns an empty string when the variable is not present, so an unset variable is an empty secret and the token request fails, which the code reports. A reset invalidates the previous secret immediately, so plan the cut-over; a second client created before the reset is an inferred way to do it without a gap. **Before deploying, confirm the calling identity:** every call now runs as the OAuth2 Client's Context User, not as the user in the replaced credential, and a non-administrator Context User is NSerio's recommendation. Trusted IPs restrict where a token can be requested, not a token already issued. The statements that Client Credentials needs a client secret and that the secret is displayed once are carried from the other languages' guidance and are not verified for this text.

**Variant: a Go web service that does not act for a signed-in user.** **Precondition: this variant is for a Go server that calls RelativityOne with no person signed in to it, so no person's identity exists to preserve. If a person signs in, use Authorization Code, variant 1.**

Use the block above in the server process; **the server must authenticate and authorize its own callers, and the OAuth2 Client's Context User must hold the minimum permissions the operation needs. Without both, the result is an unauthenticated relay into RelativityOne**: every caller reaching it acts with the Context User's full rights, which is strictly worse than the starting point. The handler returns results to its caller, **never the token** and never the secret.

Verification: **Verification note.** The tool did not establish what consumes the parsed address. **A token has a finite lifetime.** **The calling identity changes.**

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
<!-- generated:end GO-008 client-credentials -->

#### Authorization Code

<!-- generated:begin GO-008 authorization-code -->
**Confirming the flow.** An interactive-user signal in a file assigns Authorization Code, with this note: "An interactive-user signal in this file points to Authorization Code. Confirm the flow: use Client Credentials instead if the integration runs with credentials fixed for the integration and no signed-in user." Your user profile can also make Authorization Code the default; see Changing the default in [Confirming the flow](#confirming-the-flow). See [Confirming the flow](#confirming-the-flow) and the "Which flow" paragraph of GO-008 in [Part A](part-a-go.md#go-008).

Source form: **Applies to:** every Go version; a held URL literal that carries a user and a password.

```go
return url.Parse("https://user:secret@host.example/path")
```

Under token authentication: **First, the destination:** if this Relativity instance is not RelativityOne — including a Relativity Server instance, whose own documentation governs — this finding is informational and the rest of this template does not apply.

```go
return url.Parse("https://host.example/path")
```

Remove the credentials from the literal and add the Bearer header to the request that uses the parsed address: build the request with `http.NewRequestWithContext` and set `Authorization` and `X-CSRF-Header` on it.

Credentials: **Where the credentials come from.** The source form establishes that the code sends Basic, not that the destination is RelativityOne. If it is not RelativityOne, the migration is that service's own token mechanism, this finding is informational for this tool, and the reader stops here. If it is RelativityOne, the token comes from the token-acquisition part that follows, and the user name and password are replaced by the OAuth2 Client's client ID and secret, kept as Secret Management (Part B of the Migration Documentation) describes and never in source. **Treat this credential as compromised. Rotate it, and assume the repository history has published it. Completing the migration does not close this exposure; rotation does.**

**Common to both variants.** The person signs in on RelativityOne's own page and the code never holds their password. The text has a common part and two variants by application type, and each variant opens with its own precondition, so that the reader can discard the others by reading one sentence each. Every block uses only the standard library and needs go1.13 or later; the shared declarations are those of the Client Credentials text and are repeated here.

```go
const (
	identityTokenURL            = "https://HOST/Relativity/Identity/connect/token"
	identityAuthorizeURL        = "https://HOST/Relativity/Identity/connect/authorize"
	clientIDEnvironmentName     = "RELATIVITY_CLIENT_ID"
	clientSecretEnvironmentName = "RELATIVITY_CLIENT_SECRET"
	expiryMarginSeconds         = 60
	csrfHeaderName              = "X-CSRF-Header"
	csrfHeaderValue             = "-"
	stateByteCount              = 16
	verifierByteCount           = 32
	serverHeaderTimeoutSeconds  = 10
	signInCookieName            = "relativity_sign_in"
	signInCookieMaxAgeSeconds   = 600
	signInTimeoutSeconds        = 300
)

type tokenResponse struct {
	AccessToken  string `json:"access_token"`
	RefreshToken string `json:"refresh_token"`
	ExpiresIn    int    `json:"expires_in"`
}

func requestToken(ctx context.Context, client *http.Client, form url.Values) (tokenResponse, error) {
	var payload tokenResponse
	request, err := http.NewRequestWithContext(ctx, http.MethodPost, identityTokenURL, strings.NewReader(form.Encode()))
	if err != nil {
		return payload, err
	}
	request.Header.Set("Content-Type", "application/x-www-form-urlencoded")
	response, err := client.Do(request)
	if err != nil {
		return payload, err
	}
	defer response.Body.Close()
	if response.StatusCode != http.StatusOK {
		return payload, fmt.Errorf("token endpoint answered %d", response.StatusCode)
	}
	err = json.NewDecoder(response.Body).Decode(&payload)
	return payload, err
}
```

**Sign in.** Replace the code that collected the person's credentials with a redirect to the authorize endpoint `<host>/Relativity/Identity/connect/authorize`, which is **verified by NSerio against a live instance; not documented by Relativity** and leads to RelativityOne's login page. The request carries the standard authorization-code parameters — `client_id`, `response_type=code`, the registered `redirect_uri`, `state` checked on return, and `code_challenge` with `code_challenge_method=S256` when PKCE is used; these are standard OAuth and Relativity documents none of them.

**PKCE.** Relativity's own documentation requires PKCE to obtain a refresh token; that a Code request that does not ask for a refresh token works without PKCE is **verified by NSerio against a live instance; not documented by Relativity**. **PKCE is not limited to clients that use refresh tokens**: NSerio recommends `S256` for every Authorization Code client (NSerio recommendation) and does not claim Relativity accepts any particular method. **Whether a client with refresh tokens disabled accepts a PKCE request (a `code_challenge`) is unverified**: test it before the block is relied on for such a client. **`state`** is generated, stored against the session and compared on return; `nonce` is not used, and Relativity does not document whether it enforces either. The authorize and token endpoints and the PKCE and `state` behaviour above are **verified by NSerio against a live instance; not documented by Relativity**. The block draws the verifier and `state` from `crypto/rand`, whose `Read` fills the buffer with cryptographically secure random bytes (the signature still returns an error, which the block checks), and computes the challenge with `crypto/sha256` and `base64.RawURLEncoding`, the unpadded URL-safe encoding; a 32-byte value encodes to a 43-character verifier.

**The client secret is required.** Code does not work without a client secret: tested and rejected, **verified by NSerio against a live instance; not documented by Relativity**, which confirms Relativity's own statement. **Any secret placed in a distributed binary is extractable by whoever holds the binary**: a Go program compiled for people other than its owner carries its client secret in a form anyone who holds the file can extract, and Relativity documents no recommendation for that case; use a short Access Token Lifetime, Trusted IPs where the outbound addresses are stable and one OAuth2 Client per distribution as compensating guidance (NSerio recommendation). Keep the secret as Secret Management (Part B of the Migration Documentation) describes, never in source or in a log.

**Exchange the code.** A form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=authorization_code`, the code, the same `redirect_uri`, the client ID and the secret, plus the `code_verifier` when PKCE is used.

```go
func randomToken(byteCount int) (string, error) {
	buffer := make([]byte, byteCount)
	if _, err := rand.Read(buffer); err != nil {
		return "", err
	}
	return base64.RawURLEncoding.EncodeToString(buffer), nil
}

func challengeOf(verifier string) string {
	digest := sha256.Sum256([]byte(verifier))
	return base64.RawURLEncoding.EncodeToString(digest[:])
}

func authorizeAddress(redirectURI string, state string, challenge string) string {
	query := url.Values{
		"client_id":     {os.Getenv(clientIDEnvironmentName)},
		"response_type": {"code"},
		"redirect_uri":  {redirectURI},
		"state":         {state},
	}
	if challenge != "" {
		query.Set("code_challenge", challenge)
		query.Set("code_challenge_method", "S256")
	}
	return identityAuthorizeURL + "?" + query.Encode()
}

func exchangeCode(ctx context.Context, client *http.Client, code string, redirectURI string, verifier string) (tokenResponse, error) {
	form := url.Values{
		"grant_type":    {"authorization_code"},
		"code":          {code},
		"redirect_uri":  {redirectURI},
		"client_id":     {os.Getenv(clientIDEnvironmentName)},
		"client_secret": {os.Getenv(clientSecretEnvironmentName)},
	}
	if verifier != "" {
		form.Set("code_verifier", verifier)
	}
	return requestToken(ctx, client, form)
}
```

**Redirect URL.** A server-side application registers **one exact HTTPS URL per environment, no wildcards**; the matching rules are not documented and no claim is made about them. A plain-HTTP loopback Redirect URL `http://127.0.0.1:<port>/<path>` on a **fixed registered port** is accepted, **verified by NSerio against a live instance; not documented by Relativity**, and is the command-line and desktop shape.

**On expiry.** With Allow Refresh Tokens enabled and PKCE used, the code holds a refresh token and sends `grant_type=refresh_token` in place of the code exchange: *inferred* from the standard OAuth shape, not tested; Relativity does not document a refresh token's lifetime, rotation or revocation, nor what disabling the client does to tokens already issued. **A refresh token is a secret**: hold it where the secret is held, never in a log. **Single sign-on:** sign-in happens on RelativityOne's own page, so the instance's single sign-on and two-factor rules apply unchanged (*inferred*). **The code stops collecting the password:** after this change nothing receives, stores or forwards the person's user name and password, and the code holds only tokens. Every call now runs as the person who signed in, so **the calling identity changes**. **Scope:** no scope set is prescribed for Code; `openid RelativityRequestOrigin UserInfo UserInfoAccess AuthenticationID RelativityWeb` works, **verified by NSerio against a live instance; not documented by Relativity**, as working and not as minimal.

**Variant 1 — a Go web server.** **Precondition: this variant is for a Go server that signs a person in to RelativityOne. If the code is a command-line tool or a desktop process, use variant 2.**

A server-side component creates `state` and the PKCE verifier, redirects to the authorize endpoint, receives the code at its registered HTTPS Redirect URL, exchanges it, keeps the tokens on the server and gives the browser **only its own session**; **the browser never holds anything from RelativityOne**. The two handlers use the standard library's `net/http`: `http.Redirect` replies to the request with a redirect to the authorize address.

```go
type WebSignIn struct {
	mutex       sync.Mutex
	pending     map[string]string
	client      *http.Client
	redirectURI string
	keepTokens  func(w http.ResponseWriter, r *http.Request, tokens tokenResponse)
}

func (signIn *WebSignIn) Login(w http.ResponseWriter, r *http.Request) {
	state, err := randomToken(stateByteCount)
	if err != nil {
		http.Error(w, "sign-in unavailable", http.StatusInternalServerError)
		return
	}
	verifier, err := randomToken(verifierByteCount)
	if err != nil {
		http.Error(w, "sign-in unavailable", http.StatusInternalServerError)
		return
	}
	signIn.mutex.Lock()
	signIn.pending[state] = verifier
	signIn.mutex.Unlock()
	http.SetCookie(w, &http.Cookie{
		Name:     signInCookieName,
		Value:    state,
		Path:     "/",
		MaxAge:   signInCookieMaxAgeSeconds,
		HttpOnly: true,
		Secure:   true,
		SameSite: http.SameSiteLaxMode,
	})
	http.Redirect(w, r, authorizeAddress(signIn.redirectURI, state, challengeOf(verifier)), http.StatusFound)
}

func (signIn *WebSignIn) Callback(w http.ResponseWriter, r *http.Request) {
	query := r.URL.Query()
	if query.Get("error") != "" {
		http.Error(w, "sign-in was not completed", http.StatusUnauthorized)
		return
	}
	state := query.Get("state")
	cookie, err := r.Cookie(signInCookieName)
	if err != nil || query.Get("code") == "" || subtle.ConstantTimeCompare([]byte(cookie.Value), []byte(state)) != 1 {
		http.Error(w, "unknown sign-in", http.StatusBadRequest)
		return
	}
	signIn.mutex.Lock()
	verifier, known := signIn.pending[state]
	delete(signIn.pending, state)
	signIn.mutex.Unlock()
	if !known {
		http.Error(w, "unknown sign-in", http.StatusBadRequest)
		return
	}
	http.SetCookie(w, &http.Cookie{Name: signInCookieName, Path: "/", MaxAge: -1})
	tokens, err := exchangeCode(r.Context(), signIn.client, query.Get("code"), signIn.redirectURI, verifier)
	if err != nil {
		http.Error(w, "sign-in failed", http.StatusBadGateway)
		return
	}
	signIn.keepTokens(w, r, tokens)
}
```

**`state` is bound to the browser session**: `Login` stores it in a short-lived, `HttpOnly`, `Secure`, `SameSite=Lax` cookie as well as in the server's pending table, and `Callback` fails (400) unless the cookie is present **and** equals the `state` on the query, so a callback that arrives without the matching cookie gets no exchange. `Callback` also **handles the `error` parameter** (401, no exchange) and **an empty `code`** (400). `state` is consumed once: the entry is deleted before the exchange and the cookie is cleared, so a replayed or unknown `state` gets no exchange. `keepTokens` is yours: store the tokens against the visitor's session on the server and set a session cookie that holds **only a session identifier**. The refresh call, if used, goes to the same token endpoint with `grant_type=refresh_token` (*inferred*). **The calling identity changes** to the person who signed in.

**Variant 2 — a command-line tool or a desktop process.** **Precondition: this variant is for a Go command-line tool or a desktop process that signs a person in to RelativityOne. If the code is a server, use variant 1.**

The process starts a listener from the standard library bound to `127.0.0.1` only, on the **fixed port the administrator registered** (the exact URL is registered, so the port is fixed, chosen by the administrator and not at runtime), for **one request**; it **prints the authorize address** (so a machine with no browser, an SSH session or a failed launch has the manual route) and, if you want, opens the system browser yourself; reads `code` and `state` from the one request, answers with a short page, closes the listener, checks `state` and exchanges the code.

```go
func SignInFromCommandLine(ctx context.Context, client *http.Client, listenAddress string, redirectURI string) (tokenResponse, error) {
	var tokens tokenResponse
	redirect, err := url.Parse(redirectURI)
	if err != nil {
		return tokens, err
	}
	host, _, err := net.SplitHostPort(listenAddress)
	if err != nil {
		return tokens, err
	}
	if host != "127.0.0.1" && host != "::1" {
		return tokens, errors.New("the sign-in listener must be bound to the loopback address")
	}
	ctx, cancel := context.WithTimeout(ctx, signInTimeoutSeconds*time.Second)
	defer cancel()
	state, err := randomToken(stateByteCount)
	if err != nil {
		return tokens, err
	}
	verifier, err := randomToken(verifierByteCount)
	if err != nil {
		return tokens, err
	}
	listener, err := net.Listen("tcp", listenAddress)
	if err != nil {
		return tokens, fmt.Errorf("the registered port is not available: %w", err)
	}
	type callback struct{ code, state, failure string }
	received := make(chan callback, 1)
	server := &http.Server{
		ReadHeaderTimeout: serverHeaderTimeoutSeconds * time.Second,
		Handler: http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
			if r.URL.Path != redirect.Path {
				http.NotFound(w, r)
				return
			}
			query := r.URL.Query()
			select {
			case received <- callback{code: query.Get("code"), state: query.Get("state"), failure: query.Get("error")}:
			default:
			}
			fmt.Fprintln(w, "Sign-in received. You can close this window.")
		}),
	}
	go func() { _ = server.Serve(listener) }()
	defer server.Shutdown(context.Background())
	fmt.Println("Open this address in a browser to sign in:", authorizeAddress(redirectURI, state, challengeOf(verifier)))
	select {
	case result := <-received:
		if result.failure != "" || result.code == "" {
			return tokens, errors.New("sign-in was not completed")
		}
		if subtle.ConstantTimeCompare([]byte(result.state), []byte(state)) != 1 {
			return tokens, errors.New("the sign-in answer does not match the request")
		}
		return exchangeCode(ctx, client, result.code, redirectURI, verifier)
	case <-ctx.Done():
		return tokens, ctx.Err()
	}
}
```

**The listener is checked and bounded in the code**: it **refuses a `listenAddress` whose host is not `127.0.0.1` or `::1`** (loopback only); its handler **answers 404 to every path other than the registered redirect path** (so a stray request cannot consume the one-shot channel); the whole sign-in has **its own deadline** (`signInTimeoutSeconds`, 300 seconds, on top of the caller's context), after which it returns the context error; a callback with an `error` parameter or an empty `code` is an error, not an exchange. **Busy port:** the port is the registered one and is **not** replaced by another (a different port is a different Redirect URL, which Relativity would not match); if `net.Listen` fails because the port is taken, the function returns the error `the registered port is not available` and the tool must report it, not retry on another port. Call it with `listenAddress` `127.0.0.1:<the registered port>` and `redirectURI` `http://127.0.0.1:<the registered port>/<path>`. **Where the tokens are kept** is the program's decision: in memory for the run, or, for a refresh token, in a file whose permissions you restrict (**a distributed command-line tool carries its client secret in a form anyone who holds the distribution can extract**). Every call now runs as the person who signed in.

Verification: **Verification note.** The tool did not establish what consumes the parsed address. **A token has a finite lifetime.** **The calling identity changes.**

OAuth2 Client: Create or confirm this OAuth2 Client in RelativityOne (NSerio's observation: the menu path is Home > Authentication > OAuth2 Client Manager, as Relativity's REST API authentication page names it). This tool never connects to RelativityOne and never creates or changes the OAuth2 Client; the settings below are guidance.
- Name: a descriptive name identifying the application it serves.
- Flow Grant Type: Code
- Redirect URLs: Relativity documentation: each URL includes the http or https protocol prefix. The exact callback URL is registered by the Relativity administrator. Server-side web applications, NSerio's recommendation: one exact HTTPS URL per environment, with no wildcards. Desktop and CLI applications: a plain-HTTP loopback URL, `http://127.0.0.1:<port>/<path>`, on a fixed port because the URL is registered in advance and the port cannot be chosen at runtime (verified by NSerio against a live instance; not documented by Relativity).
- Access Token Lifetime: 60 minutes, stated in minutes because the field is in minutes. Relativity documentation: tokens for this flow must have a short lifetime, and the recommended value matches the Identity Server default.
- Allow refresh tokens: NSerio's recommendation: leave it off unless the application must keep a user session active beyond the access token lifetime without asking the user to sign in again. If it is on, the refresh tokens are stored as secrets, per Secret Management (Part B of the Migration Documentation).
- Enabled: on.
<!-- generated:end GO-008 authorization-code -->

### GO-009 · AuthorizationHeaderValueHeldInVariable

<a id="go-009-migration"></a>

Delete the declaration that builds the `Basic` value and write the Bearer value at the header write instead. The tool resolved the header's variable to a scheme of `Basic` in this file, from one declaration before the use; a variable it cannot resolve is not read, so search for other headers that take their value from a name.

#### Client Credentials

<!-- generated:begin GO-009 client-credentials -->
**Confirming the flow.** When no signal in a file decides the flow, the tool assigns Client Credentials, the default, with this note: "Who supplies the credential at run time? If a person types it or signs in, the integration acts for that person: use Authorization Code. If it comes from configuration, settings, an environment variable or a secret store: use Client Credentials. No signal in this file showed which applies, so Client Credentials, the default, is recommended. Use Authorization Code instead if the integration acts on behalf of a signed-in user, or if your RelativityOne instance already has a Code-flow OAuth2 Client for this integration." A fixed-credential signal assigns it with this note: "A fixed-credential signal in this file points to Client Credentials, but how often that kind of signal is right has not been measured. Use Authorization Code instead if the integration acts on behalf of a signed-in user, or if your RelativityOne instance already has a Code-flow OAuth2 Client for this integration." See [Confirming the flow](#confirming-the-flow) and the "Which flow" paragraph of GO-009 in [Part A](part-a-go.md#go-009).

Source form: **Applies to:** every Go version; a header whose value is an identifier that the tool resolved to a `Basic` scheme.

```go
request.Header.Set("Authorization", header)
```

Under token authentication: **First, the destination:** if this Relativity instance is not RelativityOne — including a Relativity Server instance, whose own documentation governs — this finding is informational and the rest of this template does not apply.

```go
request.Header.Set("Authorization", "Bearer "+accessToken)
request.Header.Set(csrfHeaderName, csrfHeaderValue)
```

Delete the declaration that builds `header` as well.

Credentials: **Where the credentials come from.** The source form establishes that the code sends Basic, not that the destination is RelativityOne. If it is not RelativityOne, the migration is that service's own token mechanism, this finding is informational for this tool, and the reader stops here. If it is RelativityOne, the token comes from the token-acquisition part that follows, and the user name and password are replaced by the OAuth2 Client's client ID and secret, kept as Secret Management (Part B of the Migration Documentation) describes and never in source.

**Where the token comes from.** Create an OAuth2 Client with Flow Grant Type set to Client Credentials (part 3 lists its settings), then send a form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=client_credentials`, `client_id` and `client_secret`. The token endpoint is **documented** for this grant. The token is then sent as `Authorization: Bearer <token>`, with `X-CSRF-Header: -` on REST and Kepler calls. The scope depends on what the integration needs and **no single scope is required**: Relativity's documentation shows `SystemUserInfo` and `UserInfoAccess` and no page says which is correct or what either grants; use the value the API you call documents, and confirm it. Every block below uses only the standard library and needs go1.13 or later.

**Shared declarations.** Every block of this text, and of the use-site forms above, uses these constants and helpers; `HOST` is the RelativityOne host.

```go
const (
	identityTokenURL            = "https://HOST/Relativity/Identity/connect/token"
	identityAuthorizeURL        = "https://HOST/Relativity/Identity/connect/authorize"
	clientIDEnvironmentName     = "RELATIVITY_CLIENT_ID"
	clientSecretEnvironmentName = "RELATIVITY_CLIENT_SECRET"
	expiryMarginSeconds         = 60
	csrfHeaderName              = "X-CSRF-Header"
	csrfHeaderValue             = "-"
	stateByteCount              = 16
	verifierByteCount           = 32
	serverHeaderTimeoutSeconds  = 10
	signInCookieName            = "relativity_sign_in"
	signInCookieMaxAgeSeconds   = 600
	signInTimeoutSeconds        = 300
)

type tokenResponse struct {
	AccessToken  string `json:"access_token"`
	RefreshToken string `json:"refresh_token"`
	ExpiresIn    int    `json:"expires_in"`
}

func requestToken(ctx context.Context, client *http.Client, form url.Values) (tokenResponse, error) {
	var payload tokenResponse
	request, err := http.NewRequestWithContext(ctx, http.MethodPost, identityTokenURL, strings.NewReader(form.Encode()))
	if err != nil {
		return payload, err
	}
	request.Header.Set("Content-Type", "application/x-www-form-urlencoded")
	response, err := client.Do(request)
	if err != nil {
		return payload, err
	}
	defer response.Body.Close()
	if response.StatusCode != http.StatusOK {
		return payload, fmt.Errorf("token endpoint answered %d", response.StatusCode)
	}
	err = json.NewDecoder(response.Body).Decode(&payload)
	return payload, err
}
```

The token request is a form-encoded `POST`. **Where the client secret travels in the token request** (the form body, as here, or HTTP Basic client authentication) **and the field names of the token response are not documented by Relativity**: the form body and `tokenResponse` are the standard OAuth shape and an NSerio recommendation, **not verified** against RelativityOne. **Do not** send the client secret with `SetBasicAuth` and **do not** put it in the URL.

**Variant: a Go process acting as itself.** **Precondition: this variant is for a Go job, daemon or service that does not act for a signed-in person.**

```go
type TokenSource struct {
	mutex     sync.Mutex
	client    *http.Client
	token     string
	expiresAt time.Time
}

func (source *TokenSource) Token(ctx context.Context) (string, error) {
	source.mutex.Lock()
	defer source.mutex.Unlock()
	if source.token == "" || time.Until(source.expiresAt) < expiryMarginSeconds*time.Second {
		form := url.Values{
			"grant_type":    {"client_credentials"},
			"client_id":     {os.Getenv(clientIDEnvironmentName)},
			"client_secret": {os.Getenv(clientSecretEnvironmentName)},
		}
		payload, err := requestToken(ctx, source.client, form)
		if err != nil {
			return "", err
		}
		source.token = payload.AccessToken
		source.expiresAt = time.Now().Add(time.Duration(payload.ExpiresIn) * time.Second)
	}
	return source.token, nil
}
```

**Cache the token and re-acquire it on expiry — do not refresh.** Client Credentials does not offer refresh tokens: **verified by NSerio against a live instance; not documented by Relativity** — the Allow Refresh Tokens control does not appear for this grant in the RelativityOne user interface. The recommended Access Token Lifetime is 60 minutes. **The client secret** belongs in the process environment or a secret store, as Secret Management (Part B of the Migration Documentation) describes, **never in source, in a tracked `.env` file or in a log**: the block reads it with `os.Getenv`, which returns an empty string when the variable is not present, so an unset variable is an empty secret and the token request fails, which the code reports. A reset invalidates the previous secret immediately, so plan the cut-over; a second client created before the reset is an inferred way to do it without a gap. **Before deploying, confirm the calling identity:** every call now runs as the OAuth2 Client's Context User, not as the user in the replaced credential, and a non-administrator Context User is NSerio's recommendation. Trusted IPs restrict where a token can be requested, not a token already issued. The statements that Client Credentials needs a client secret and that the secret is displayed once are carried from the other languages' guidance and are not verified for this text.

**Variant: a Go web service that does not act for a signed-in user.** **Precondition: this variant is for a Go server that calls RelativityOne with no person signed in to it, so no person's identity exists to preserve. If a person signs in, use Authorization Code, variant 1.**

Use the block above in the server process; **the server must authenticate and authorize its own callers, and the OAuth2 Client's Context User must hold the minimum permissions the operation needs. Without both, the result is an unauthenticated relay into RelativityOne**: every caller reaching it acts with the Context User's full rights, which is strictly worse than the starting point. The handler returns results to its caller, **never the token** and never the secret.

Verification: **Verification note.** **The tool resolved the header's variable to a scheme of `Basic` in this file** (one declaration, before the use); a variable it cannot resolve is not read. **A token has a finite lifetime.** **The calling identity changes.**

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
<!-- generated:end GO-009 client-credentials -->

#### Authorization Code

<!-- generated:begin GO-009 authorization-code -->
**Confirming the flow.** An interactive-user signal in a file assigns Authorization Code, with this note: "An interactive-user signal in this file points to Authorization Code. Confirm the flow: use Client Credentials instead if the integration runs with credentials fixed for the integration and no signed-in user." Your user profile can also make Authorization Code the default; see Changing the default in [Confirming the flow](#confirming-the-flow). See [Confirming the flow](#confirming-the-flow) and the "Which flow" paragraph of GO-009 in [Part A](part-a-go.md#go-009).

Source form: **Applies to:** every Go version; a header whose value is an identifier that the tool resolved to a `Basic` scheme.

```go
request.Header.Set("Authorization", header)
```

Under token authentication: **First, the destination:** if this Relativity instance is not RelativityOne — including a Relativity Server instance, whose own documentation governs — this finding is informational and the rest of this template does not apply.

```go
request.Header.Set("Authorization", "Bearer "+accessToken)
request.Header.Set(csrfHeaderName, csrfHeaderValue)
```

Delete the declaration that builds `header` as well.

Credentials: **Where the credentials come from.** The source form establishes that the code sends Basic, not that the destination is RelativityOne. If it is not RelativityOne, the migration is that service's own token mechanism, this finding is informational for this tool, and the reader stops here. If it is RelativityOne, the token comes from the token-acquisition part that follows, and the user name and password are replaced by the OAuth2 Client's client ID and secret, kept as Secret Management (Part B of the Migration Documentation) describes and never in source.

**Common to both variants.** The person signs in on RelativityOne's own page and the code never holds their password. The text has a common part and two variants by application type, and each variant opens with its own precondition, so that the reader can discard the others by reading one sentence each. Every block uses only the standard library and needs go1.13 or later; the shared declarations are those of the Client Credentials text and are repeated here.

```go
const (
	identityTokenURL            = "https://HOST/Relativity/Identity/connect/token"
	identityAuthorizeURL        = "https://HOST/Relativity/Identity/connect/authorize"
	clientIDEnvironmentName     = "RELATIVITY_CLIENT_ID"
	clientSecretEnvironmentName = "RELATIVITY_CLIENT_SECRET"
	expiryMarginSeconds         = 60
	csrfHeaderName              = "X-CSRF-Header"
	csrfHeaderValue             = "-"
	stateByteCount              = 16
	verifierByteCount           = 32
	serverHeaderTimeoutSeconds  = 10
	signInCookieName            = "relativity_sign_in"
	signInCookieMaxAgeSeconds   = 600
	signInTimeoutSeconds        = 300
)

type tokenResponse struct {
	AccessToken  string `json:"access_token"`
	RefreshToken string `json:"refresh_token"`
	ExpiresIn    int    `json:"expires_in"`
}

func requestToken(ctx context.Context, client *http.Client, form url.Values) (tokenResponse, error) {
	var payload tokenResponse
	request, err := http.NewRequestWithContext(ctx, http.MethodPost, identityTokenURL, strings.NewReader(form.Encode()))
	if err != nil {
		return payload, err
	}
	request.Header.Set("Content-Type", "application/x-www-form-urlencoded")
	response, err := client.Do(request)
	if err != nil {
		return payload, err
	}
	defer response.Body.Close()
	if response.StatusCode != http.StatusOK {
		return payload, fmt.Errorf("token endpoint answered %d", response.StatusCode)
	}
	err = json.NewDecoder(response.Body).Decode(&payload)
	return payload, err
}
```

**Sign in.** Replace the code that collected the person's credentials with a redirect to the authorize endpoint `<host>/Relativity/Identity/connect/authorize`, which is **verified by NSerio against a live instance; not documented by Relativity** and leads to RelativityOne's login page. The request carries the standard authorization-code parameters — `client_id`, `response_type=code`, the registered `redirect_uri`, `state` checked on return, and `code_challenge` with `code_challenge_method=S256` when PKCE is used; these are standard OAuth and Relativity documents none of them.

**PKCE.** Relativity's own documentation requires PKCE to obtain a refresh token; that a Code request that does not ask for a refresh token works without PKCE is **verified by NSerio against a live instance; not documented by Relativity**. **PKCE is not limited to clients that use refresh tokens**: NSerio recommends `S256` for every Authorization Code client (NSerio recommendation) and does not claim Relativity accepts any particular method. **Whether a client with refresh tokens disabled accepts a PKCE request (a `code_challenge`) is unverified**: test it before the block is relied on for such a client. **`state`** is generated, stored against the session and compared on return; `nonce` is not used, and Relativity does not document whether it enforces either. The authorize and token endpoints and the PKCE and `state` behaviour above are **verified by NSerio against a live instance; not documented by Relativity**. The block draws the verifier and `state` from `crypto/rand`, whose `Read` fills the buffer with cryptographically secure random bytes (the signature still returns an error, which the block checks), and computes the challenge with `crypto/sha256` and `base64.RawURLEncoding`, the unpadded URL-safe encoding; a 32-byte value encodes to a 43-character verifier.

**The client secret is required.** Code does not work without a client secret: tested and rejected, **verified by NSerio against a live instance; not documented by Relativity**, which confirms Relativity's own statement. **Any secret placed in a distributed binary is extractable by whoever holds the binary**: a Go program compiled for people other than its owner carries its client secret in a form anyone who holds the file can extract, and Relativity documents no recommendation for that case; use a short Access Token Lifetime, Trusted IPs where the outbound addresses are stable and one OAuth2 Client per distribution as compensating guidance (NSerio recommendation). Keep the secret as Secret Management (Part B of the Migration Documentation) describes, never in source or in a log.

**Exchange the code.** A form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=authorization_code`, the code, the same `redirect_uri`, the client ID and the secret, plus the `code_verifier` when PKCE is used.

```go
func randomToken(byteCount int) (string, error) {
	buffer := make([]byte, byteCount)
	if _, err := rand.Read(buffer); err != nil {
		return "", err
	}
	return base64.RawURLEncoding.EncodeToString(buffer), nil
}

func challengeOf(verifier string) string {
	digest := sha256.Sum256([]byte(verifier))
	return base64.RawURLEncoding.EncodeToString(digest[:])
}

func authorizeAddress(redirectURI string, state string, challenge string) string {
	query := url.Values{
		"client_id":     {os.Getenv(clientIDEnvironmentName)},
		"response_type": {"code"},
		"redirect_uri":  {redirectURI},
		"state":         {state},
	}
	if challenge != "" {
		query.Set("code_challenge", challenge)
		query.Set("code_challenge_method", "S256")
	}
	return identityAuthorizeURL + "?" + query.Encode()
}

func exchangeCode(ctx context.Context, client *http.Client, code string, redirectURI string, verifier string) (tokenResponse, error) {
	form := url.Values{
		"grant_type":    {"authorization_code"},
		"code":          {code},
		"redirect_uri":  {redirectURI},
		"client_id":     {os.Getenv(clientIDEnvironmentName)},
		"client_secret": {os.Getenv(clientSecretEnvironmentName)},
	}
	if verifier != "" {
		form.Set("code_verifier", verifier)
	}
	return requestToken(ctx, client, form)
}
```

**Redirect URL.** A server-side application registers **one exact HTTPS URL per environment, no wildcards**; the matching rules are not documented and no claim is made about them. A plain-HTTP loopback Redirect URL `http://127.0.0.1:<port>/<path>` on a **fixed registered port** is accepted, **verified by NSerio against a live instance; not documented by Relativity**, and is the command-line and desktop shape.

**On expiry.** With Allow Refresh Tokens enabled and PKCE used, the code holds a refresh token and sends `grant_type=refresh_token` in place of the code exchange: *inferred* from the standard OAuth shape, not tested; Relativity does not document a refresh token's lifetime, rotation or revocation, nor what disabling the client does to tokens already issued. **A refresh token is a secret**: hold it where the secret is held, never in a log. **Single sign-on:** sign-in happens on RelativityOne's own page, so the instance's single sign-on and two-factor rules apply unchanged (*inferred*). **The code stops collecting the password:** after this change nothing receives, stores or forwards the person's user name and password, and the code holds only tokens. Every call now runs as the person who signed in, so **the calling identity changes**. **Scope:** no scope set is prescribed for Code; `openid RelativityRequestOrigin UserInfo UserInfoAccess AuthenticationID RelativityWeb` works, **verified by NSerio against a live instance; not documented by Relativity**, as working and not as minimal.

**Variant 1 — a Go web server.** **Precondition: this variant is for a Go server that signs a person in to RelativityOne. If the code is a command-line tool or a desktop process, use variant 2.**

A server-side component creates `state` and the PKCE verifier, redirects to the authorize endpoint, receives the code at its registered HTTPS Redirect URL, exchanges it, keeps the tokens on the server and gives the browser **only its own session**; **the browser never holds anything from RelativityOne**. The two handlers use the standard library's `net/http`: `http.Redirect` replies to the request with a redirect to the authorize address.

```go
type WebSignIn struct {
	mutex       sync.Mutex
	pending     map[string]string
	client      *http.Client
	redirectURI string
	keepTokens  func(w http.ResponseWriter, r *http.Request, tokens tokenResponse)
}

func (signIn *WebSignIn) Login(w http.ResponseWriter, r *http.Request) {
	state, err := randomToken(stateByteCount)
	if err != nil {
		http.Error(w, "sign-in unavailable", http.StatusInternalServerError)
		return
	}
	verifier, err := randomToken(verifierByteCount)
	if err != nil {
		http.Error(w, "sign-in unavailable", http.StatusInternalServerError)
		return
	}
	signIn.mutex.Lock()
	signIn.pending[state] = verifier
	signIn.mutex.Unlock()
	http.SetCookie(w, &http.Cookie{
		Name:     signInCookieName,
		Value:    state,
		Path:     "/",
		MaxAge:   signInCookieMaxAgeSeconds,
		HttpOnly: true,
		Secure:   true,
		SameSite: http.SameSiteLaxMode,
	})
	http.Redirect(w, r, authorizeAddress(signIn.redirectURI, state, challengeOf(verifier)), http.StatusFound)
}

func (signIn *WebSignIn) Callback(w http.ResponseWriter, r *http.Request) {
	query := r.URL.Query()
	if query.Get("error") != "" {
		http.Error(w, "sign-in was not completed", http.StatusUnauthorized)
		return
	}
	state := query.Get("state")
	cookie, err := r.Cookie(signInCookieName)
	if err != nil || query.Get("code") == "" || subtle.ConstantTimeCompare([]byte(cookie.Value), []byte(state)) != 1 {
		http.Error(w, "unknown sign-in", http.StatusBadRequest)
		return
	}
	signIn.mutex.Lock()
	verifier, known := signIn.pending[state]
	delete(signIn.pending, state)
	signIn.mutex.Unlock()
	if !known {
		http.Error(w, "unknown sign-in", http.StatusBadRequest)
		return
	}
	http.SetCookie(w, &http.Cookie{Name: signInCookieName, Path: "/", MaxAge: -1})
	tokens, err := exchangeCode(r.Context(), signIn.client, query.Get("code"), signIn.redirectURI, verifier)
	if err != nil {
		http.Error(w, "sign-in failed", http.StatusBadGateway)
		return
	}
	signIn.keepTokens(w, r, tokens)
}
```

**`state` is bound to the browser session**: `Login` stores it in a short-lived, `HttpOnly`, `Secure`, `SameSite=Lax` cookie as well as in the server's pending table, and `Callback` fails (400) unless the cookie is present **and** equals the `state` on the query, so a callback that arrives without the matching cookie gets no exchange. `Callback` also **handles the `error` parameter** (401, no exchange) and **an empty `code`** (400). `state` is consumed once: the entry is deleted before the exchange and the cookie is cleared, so a replayed or unknown `state` gets no exchange. `keepTokens` is yours: store the tokens against the visitor's session on the server and set a session cookie that holds **only a session identifier**. The refresh call, if used, goes to the same token endpoint with `grant_type=refresh_token` (*inferred*). **The calling identity changes** to the person who signed in.

**Variant 2 — a command-line tool or a desktop process.** **Precondition: this variant is for a Go command-line tool or a desktop process that signs a person in to RelativityOne. If the code is a server, use variant 1.**

The process starts a listener from the standard library bound to `127.0.0.1` only, on the **fixed port the administrator registered** (the exact URL is registered, so the port is fixed, chosen by the administrator and not at runtime), for **one request**; it **prints the authorize address** (so a machine with no browser, an SSH session or a failed launch has the manual route) and, if you want, opens the system browser yourself; reads `code` and `state` from the one request, answers with a short page, closes the listener, checks `state` and exchanges the code.

```go
func SignInFromCommandLine(ctx context.Context, client *http.Client, listenAddress string, redirectURI string) (tokenResponse, error) {
	var tokens tokenResponse
	redirect, err := url.Parse(redirectURI)
	if err != nil {
		return tokens, err
	}
	host, _, err := net.SplitHostPort(listenAddress)
	if err != nil {
		return tokens, err
	}
	if host != "127.0.0.1" && host != "::1" {
		return tokens, errors.New("the sign-in listener must be bound to the loopback address")
	}
	ctx, cancel := context.WithTimeout(ctx, signInTimeoutSeconds*time.Second)
	defer cancel()
	state, err := randomToken(stateByteCount)
	if err != nil {
		return tokens, err
	}
	verifier, err := randomToken(verifierByteCount)
	if err != nil {
		return tokens, err
	}
	listener, err := net.Listen("tcp", listenAddress)
	if err != nil {
		return tokens, fmt.Errorf("the registered port is not available: %w", err)
	}
	type callback struct{ code, state, failure string }
	received := make(chan callback, 1)
	server := &http.Server{
		ReadHeaderTimeout: serverHeaderTimeoutSeconds * time.Second,
		Handler: http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
			if r.URL.Path != redirect.Path {
				http.NotFound(w, r)
				return
			}
			query := r.URL.Query()
			select {
			case received <- callback{code: query.Get("code"), state: query.Get("state"), failure: query.Get("error")}:
			default:
			}
			fmt.Fprintln(w, "Sign-in received. You can close this window.")
		}),
	}
	go func() { _ = server.Serve(listener) }()
	defer server.Shutdown(context.Background())
	fmt.Println("Open this address in a browser to sign in:", authorizeAddress(redirectURI, state, challengeOf(verifier)))
	select {
	case result := <-received:
		if result.failure != "" || result.code == "" {
			return tokens, errors.New("sign-in was not completed")
		}
		if subtle.ConstantTimeCompare([]byte(result.state), []byte(state)) != 1 {
			return tokens, errors.New("the sign-in answer does not match the request")
		}
		return exchangeCode(ctx, client, result.code, redirectURI, verifier)
	case <-ctx.Done():
		return tokens, ctx.Err()
	}
}
```

**The listener is checked and bounded in the code**: it **refuses a `listenAddress` whose host is not `127.0.0.1` or `::1`** (loopback only); its handler **answers 404 to every path other than the registered redirect path** (so a stray request cannot consume the one-shot channel); the whole sign-in has **its own deadline** (`signInTimeoutSeconds`, 300 seconds, on top of the caller's context), after which it returns the context error; a callback with an `error` parameter or an empty `code` is an error, not an exchange. **Busy port:** the port is the registered one and is **not** replaced by another (a different port is a different Redirect URL, which Relativity would not match); if `net.Listen` fails because the port is taken, the function returns the error `the registered port is not available` and the tool must report it, not retry on another port. Call it with `listenAddress` `127.0.0.1:<the registered port>` and `redirectURI` `http://127.0.0.1:<the registered port>/<path>`. **Where the tokens are kept** is the program's decision: in memory for the run, or, for a refresh token, in a file whose permissions you restrict (**a distributed command-line tool carries its client secret in a form anyone who holds the distribution can extract**). Every call now runs as the person who signed in.

Verification: **Verification note.** **The tool resolved the header's variable to a scheme of `Basic` in this file** (one declaration, before the use); a variable it cannot resolve is not read. **A token has a finite lifetime.** **The calling identity changes.**

OAuth2 Client: Create or confirm this OAuth2 Client in RelativityOne (NSerio's observation: the menu path is Home > Authentication > OAuth2 Client Manager, as Relativity's REST API authentication page names it). This tool never connects to RelativityOne and never creates or changes the OAuth2 Client; the settings below are guidance.
- Name: a descriptive name identifying the application it serves.
- Flow Grant Type: Code
- Redirect URLs: Relativity documentation: each URL includes the http or https protocol prefix. The exact callback URL is registered by the Relativity administrator. Server-side web applications, NSerio's recommendation: one exact HTTPS URL per environment, with no wildcards. Desktop and CLI applications: a plain-HTTP loopback URL, `http://127.0.0.1:<port>/<path>`, on a fixed port because the URL is registered in advance and the port cannot be chosen at runtime (verified by NSerio against a live instance; not documented by Relativity).
- Access Token Lifetime: 60 minutes, stated in minutes because the field is in minutes. Relativity documentation: tokens for this flow must have a short lifetime, and the recommended value matches the Identity Server default.
- Allow refresh tokens: NSerio's recommendation: leave it off unless the application must keep a user session active beyond the access token lifetime without asking the user to sign in again. If it is on, the refresh tokens are stored as secrets, per Secret Management (Part B of the Migration Documentation).
- Enabled: on.
<!-- generated:end GO-009 authorization-code -->

### GO-013 · AuthorizationHeaderValueFromLocalHelper

<a id="go-013-migration"></a>

Change the call as the entry shows, and **change or delete the helper the call names**: the tool reports the helper at its definition, where the encoder and the `"Basic "` literal are, so the helper's own findings (its header write, if it has one, and its encoder) are the places to edit. Keep no function that builds a `Basic` value. The tool resolved the called name to one function in this file and did not establish where the header goes: confirm that it is a request to RelativityOne.

#### Client Credentials

<!-- generated:begin GO-013 client-credentials -->
**Confirming the flow.** When no signal in a file decides the flow, the tool assigns Client Credentials, the default, with this note: "Who supplies the credential at run time? If a person types it or signs in, the integration acts for that person: use Authorization Code. If it comes from configuration, settings, an environment variable or a secret store: use Client Credentials. No signal in this file showed which applies, so Client Credentials, the default, is recommended. Use Authorization Code instead if the integration acts on behalf of a signed-in user, or if your RelativityOne instance already has a Code-flow OAuth2 Client for this integration." A fixed-credential signal assigns it with this note: "A fixed-credential signal in this file points to Client Credentials, but how often that kind of signal is right has not been measured. Use Authorization Code instead if the integration acts on behalf of a signed-in user, or if your RelativityOne instance already has a Code-flow OAuth2 Client for this integration." See [Confirming the flow](#confirming-the-flow) and the "Which flow" paragraph of GO-013 in [Part A](part-a-go.md#go-013).

Source form: **Applies to:** every Go version; a header written by `Set`, `Add` or an index assignment whose value is a call to a function declared once in the same file that builds a `Basic` value.

```go
request.Header.Set("Authorization", basicHeader(userName, secret))
```

Under token authentication: **First, the destination:** if this Relativity instance is not RelativityOne — including a Relativity Server instance, whose own documentation governs — this finding is informational and the rest of this template does not apply.

```go
request.Header.Set("Authorization", "Bearer "+accessToken)
request.Header.Set(csrfHeaderName, csrfHeaderValue)
```

Change the call as above and **change or delete the helper the call names**: the tool reports the helper at its definition, where the encoder and the `"Basic "` literal are, so the helper's own findings (its header write, if it has one, and its encoder) are the places to edit; keep **no** function that builds a `Basic` value.

Credentials: **Where the credentials come from.** The source form establishes that the code sends Basic, not that the destination is RelativityOne. If it is not RelativityOne, the migration is that service's own token mechanism, this finding is informational for this tool, and the reader stops here. If it is RelativityOne, the token comes from the token-acquisition part that follows, and the user name and password are replaced by the OAuth2 Client's client ID and secret, kept as Secret Management (Part B of the Migration Documentation) describes and never in source.

**Where the token comes from.** Create an OAuth2 Client with Flow Grant Type set to Client Credentials (part 3 lists its settings), then send a form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=client_credentials`, `client_id` and `client_secret`. The token endpoint is **documented** for this grant. The token is then sent as `Authorization: Bearer <token>`, with `X-CSRF-Header: -` on REST and Kepler calls. The scope depends on what the integration needs and **no single scope is required**: Relativity's documentation shows `SystemUserInfo` and `UserInfoAccess` and no page says which is correct or what either grants; use the value the API you call documents, and confirm it. Every block below uses only the standard library and needs go1.13 or later.

**Shared declarations.** Every block of this text, and of the use-site forms above, uses these constants and helpers; `HOST` is the RelativityOne host.

```go
const (
	identityTokenURL            = "https://HOST/Relativity/Identity/connect/token"
	identityAuthorizeURL        = "https://HOST/Relativity/Identity/connect/authorize"
	clientIDEnvironmentName     = "RELATIVITY_CLIENT_ID"
	clientSecretEnvironmentName = "RELATIVITY_CLIENT_SECRET"
	expiryMarginSeconds         = 60
	csrfHeaderName              = "X-CSRF-Header"
	csrfHeaderValue             = "-"
	stateByteCount              = 16
	verifierByteCount           = 32
	serverHeaderTimeoutSeconds  = 10
	signInCookieName            = "relativity_sign_in"
	signInCookieMaxAgeSeconds   = 600
	signInTimeoutSeconds        = 300
)

type tokenResponse struct {
	AccessToken  string `json:"access_token"`
	RefreshToken string `json:"refresh_token"`
	ExpiresIn    int    `json:"expires_in"`
}

func requestToken(ctx context.Context, client *http.Client, form url.Values) (tokenResponse, error) {
	var payload tokenResponse
	request, err := http.NewRequestWithContext(ctx, http.MethodPost, identityTokenURL, strings.NewReader(form.Encode()))
	if err != nil {
		return payload, err
	}
	request.Header.Set("Content-Type", "application/x-www-form-urlencoded")
	response, err := client.Do(request)
	if err != nil {
		return payload, err
	}
	defer response.Body.Close()
	if response.StatusCode != http.StatusOK {
		return payload, fmt.Errorf("token endpoint answered %d", response.StatusCode)
	}
	err = json.NewDecoder(response.Body).Decode(&payload)
	return payload, err
}
```

The token request is a form-encoded `POST`. **Where the client secret travels in the token request** (the form body, as here, or HTTP Basic client authentication) **and the field names of the token response are not documented by Relativity**: the form body and `tokenResponse` are the standard OAuth shape and an NSerio recommendation, **not verified** against RelativityOne. **Do not** send the client secret with `SetBasicAuth` and **do not** put it in the URL.

**Variant: a Go process acting as itself.** **Precondition: this variant is for a Go job, daemon or service that does not act for a signed-in person.**

```go
type TokenSource struct {
	mutex     sync.Mutex
	client    *http.Client
	token     string
	expiresAt time.Time
}

func (source *TokenSource) Token(ctx context.Context) (string, error) {
	source.mutex.Lock()
	defer source.mutex.Unlock()
	if source.token == "" || time.Until(source.expiresAt) < expiryMarginSeconds*time.Second {
		form := url.Values{
			"grant_type":    {"client_credentials"},
			"client_id":     {os.Getenv(clientIDEnvironmentName)},
			"client_secret": {os.Getenv(clientSecretEnvironmentName)},
		}
		payload, err := requestToken(ctx, source.client, form)
		if err != nil {
			return "", err
		}
		source.token = payload.AccessToken
		source.expiresAt = time.Now().Add(time.Duration(payload.ExpiresIn) * time.Second)
	}
	return source.token, nil
}
```

**Cache the token and re-acquire it on expiry — do not refresh.** Client Credentials does not offer refresh tokens: **verified by NSerio against a live instance; not documented by Relativity** — the Allow Refresh Tokens control does not appear for this grant in the RelativityOne user interface. The recommended Access Token Lifetime is 60 minutes. **The client secret** belongs in the process environment or a secret store, as Secret Management (Part B of the Migration Documentation) describes, **never in source, in a tracked `.env` file or in a log**: the block reads it with `os.Getenv`, which returns an empty string when the variable is not present, so an unset variable is an empty secret and the token request fails, which the code reports. A reset invalidates the previous secret immediately, so plan the cut-over; a second client created before the reset is an inferred way to do it without a gap. **Before deploying, confirm the calling identity:** every call now runs as the OAuth2 Client's Context User, not as the user in the replaced credential, and a non-administrator Context User is NSerio's recommendation. Trusted IPs restrict where a token can be requested, not a token already issued. The statements that Client Credentials needs a client secret and that the secret is displayed once are carried from the other languages' guidance and are not verified for this text.

**Variant: a Go web service that does not act for a signed-in user.** **Precondition: this variant is for a Go server that calls RelativityOne with no person signed in to it, so no person's identity exists to preserve. If a person signs in, use Authorization Code, variant 1.**

Use the block above in the server process; **the server must authenticate and authorize its own callers, and the OAuth2 Client's Context User must hold the minimum permissions the operation needs. Without both, the result is an unauthenticated relay into RelativityOne**: every caller reaching it acts with the Context User's full rights, which is strictly worse than the starting point. The handler returns results to its caller, **never the token** and never the secret.

Verification: **Verification note.** **The tool resolved the called name to one function in this file that builds a `Basic` value, and did not establish where the header goes** (the finding is Medium for that reason): confirm that it is a request to RelativityOne; if it is not, this finding is informational. **Other callers may not appear as findings**: a call to a helper in another file or package, a method call and a name declared twice are not read, and a value held in a variable is not followed. **A token has a finite lifetime.** **The calling identity changes.**

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
<!-- generated:end GO-013 client-credentials -->

#### Authorization Code

<!-- generated:begin GO-013 authorization-code -->
**Confirming the flow.** An interactive-user signal in a file assigns Authorization Code, with this note: "An interactive-user signal in this file points to Authorization Code. Confirm the flow: use Client Credentials instead if the integration runs with credentials fixed for the integration and no signed-in user." Your user profile can also make Authorization Code the default; see Changing the default in [Confirming the flow](#confirming-the-flow). See [Confirming the flow](#confirming-the-flow) and the "Which flow" paragraph of GO-013 in [Part A](part-a-go.md#go-013).

Source form: **Applies to:** every Go version; a header written by `Set`, `Add` or an index assignment whose value is a call to a function declared once in the same file that builds a `Basic` value.

```go
request.Header.Set("Authorization", basicHeader(userName, secret))
```

Under token authentication: **First, the destination:** if this Relativity instance is not RelativityOne — including a Relativity Server instance, whose own documentation governs — this finding is informational and the rest of this template does not apply.

```go
request.Header.Set("Authorization", "Bearer "+accessToken)
request.Header.Set(csrfHeaderName, csrfHeaderValue)
```

Change the call as above and **change or delete the helper the call names**: the tool reports the helper at its definition, where the encoder and the `"Basic "` literal are, so the helper's own findings (its header write, if it has one, and its encoder) are the places to edit; keep **no** function that builds a `Basic` value.

Credentials: **Where the credentials come from.** The source form establishes that the code sends Basic, not that the destination is RelativityOne. If it is not RelativityOne, the migration is that service's own token mechanism, this finding is informational for this tool, and the reader stops here. If it is RelativityOne, the token comes from the token-acquisition part that follows, and the user name and password are replaced by the OAuth2 Client's client ID and secret, kept as Secret Management (Part B of the Migration Documentation) describes and never in source.

**Common to both variants.** The person signs in on RelativityOne's own page and the code never holds their password. The text has a common part and two variants by application type, and each variant opens with its own precondition, so that the reader can discard the others by reading one sentence each. Every block uses only the standard library and needs go1.13 or later; the shared declarations are those of the Client Credentials text and are repeated here.

```go
const (
	identityTokenURL            = "https://HOST/Relativity/Identity/connect/token"
	identityAuthorizeURL        = "https://HOST/Relativity/Identity/connect/authorize"
	clientIDEnvironmentName     = "RELATIVITY_CLIENT_ID"
	clientSecretEnvironmentName = "RELATIVITY_CLIENT_SECRET"
	expiryMarginSeconds         = 60
	csrfHeaderName              = "X-CSRF-Header"
	csrfHeaderValue             = "-"
	stateByteCount              = 16
	verifierByteCount           = 32
	serverHeaderTimeoutSeconds  = 10
	signInCookieName            = "relativity_sign_in"
	signInCookieMaxAgeSeconds   = 600
	signInTimeoutSeconds        = 300
)

type tokenResponse struct {
	AccessToken  string `json:"access_token"`
	RefreshToken string `json:"refresh_token"`
	ExpiresIn    int    `json:"expires_in"`
}

func requestToken(ctx context.Context, client *http.Client, form url.Values) (tokenResponse, error) {
	var payload tokenResponse
	request, err := http.NewRequestWithContext(ctx, http.MethodPost, identityTokenURL, strings.NewReader(form.Encode()))
	if err != nil {
		return payload, err
	}
	request.Header.Set("Content-Type", "application/x-www-form-urlencoded")
	response, err := client.Do(request)
	if err != nil {
		return payload, err
	}
	defer response.Body.Close()
	if response.StatusCode != http.StatusOK {
		return payload, fmt.Errorf("token endpoint answered %d", response.StatusCode)
	}
	err = json.NewDecoder(response.Body).Decode(&payload)
	return payload, err
}
```

**Sign in.** Replace the code that collected the person's credentials with a redirect to the authorize endpoint `<host>/Relativity/Identity/connect/authorize`, which is **verified by NSerio against a live instance; not documented by Relativity** and leads to RelativityOne's login page. The request carries the standard authorization-code parameters — `client_id`, `response_type=code`, the registered `redirect_uri`, `state` checked on return, and `code_challenge` with `code_challenge_method=S256` when PKCE is used; these are standard OAuth and Relativity documents none of them.

**PKCE.** Relativity's own documentation requires PKCE to obtain a refresh token; that a Code request that does not ask for a refresh token works without PKCE is **verified by NSerio against a live instance; not documented by Relativity**. **PKCE is not limited to clients that use refresh tokens**: NSerio recommends `S256` for every Authorization Code client (NSerio recommendation) and does not claim Relativity accepts any particular method. **Whether a client with refresh tokens disabled accepts a PKCE request (a `code_challenge`) is unverified**: test it before the block is relied on for such a client. **`state`** is generated, stored against the session and compared on return; `nonce` is not used, and Relativity does not document whether it enforces either. The authorize and token endpoints and the PKCE and `state` behaviour above are **verified by NSerio against a live instance; not documented by Relativity**. The block draws the verifier and `state` from `crypto/rand`, whose `Read` fills the buffer with cryptographically secure random bytes (the signature still returns an error, which the block checks), and computes the challenge with `crypto/sha256` and `base64.RawURLEncoding`, the unpadded URL-safe encoding; a 32-byte value encodes to a 43-character verifier.

**The client secret is required.** Code does not work without a client secret: tested and rejected, **verified by NSerio against a live instance; not documented by Relativity**, which confirms Relativity's own statement. **Any secret placed in a distributed binary is extractable by whoever holds the binary**: a Go program compiled for people other than its owner carries its client secret in a form anyone who holds the file can extract, and Relativity documents no recommendation for that case; use a short Access Token Lifetime, Trusted IPs where the outbound addresses are stable and one OAuth2 Client per distribution as compensating guidance (NSerio recommendation). Keep the secret as Secret Management (Part B of the Migration Documentation) describes, never in source or in a log.

**Exchange the code.** A form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=authorization_code`, the code, the same `redirect_uri`, the client ID and the secret, plus the `code_verifier` when PKCE is used.

```go
func randomToken(byteCount int) (string, error) {
	buffer := make([]byte, byteCount)
	if _, err := rand.Read(buffer); err != nil {
		return "", err
	}
	return base64.RawURLEncoding.EncodeToString(buffer), nil
}

func challengeOf(verifier string) string {
	digest := sha256.Sum256([]byte(verifier))
	return base64.RawURLEncoding.EncodeToString(digest[:])
}

func authorizeAddress(redirectURI string, state string, challenge string) string {
	query := url.Values{
		"client_id":     {os.Getenv(clientIDEnvironmentName)},
		"response_type": {"code"},
		"redirect_uri":  {redirectURI},
		"state":         {state},
	}
	if challenge != "" {
		query.Set("code_challenge", challenge)
		query.Set("code_challenge_method", "S256")
	}
	return identityAuthorizeURL + "?" + query.Encode()
}

func exchangeCode(ctx context.Context, client *http.Client, code string, redirectURI string, verifier string) (tokenResponse, error) {
	form := url.Values{
		"grant_type":    {"authorization_code"},
		"code":          {code},
		"redirect_uri":  {redirectURI},
		"client_id":     {os.Getenv(clientIDEnvironmentName)},
		"client_secret": {os.Getenv(clientSecretEnvironmentName)},
	}
	if verifier != "" {
		form.Set("code_verifier", verifier)
	}
	return requestToken(ctx, client, form)
}
```

**Redirect URL.** A server-side application registers **one exact HTTPS URL per environment, no wildcards**; the matching rules are not documented and no claim is made about them. A plain-HTTP loopback Redirect URL `http://127.0.0.1:<port>/<path>` on a **fixed registered port** is accepted, **verified by NSerio against a live instance; not documented by Relativity**, and is the command-line and desktop shape.

**On expiry.** With Allow Refresh Tokens enabled and PKCE used, the code holds a refresh token and sends `grant_type=refresh_token` in place of the code exchange: *inferred* from the standard OAuth shape, not tested; Relativity does not document a refresh token's lifetime, rotation or revocation, nor what disabling the client does to tokens already issued. **A refresh token is a secret**: hold it where the secret is held, never in a log. **Single sign-on:** sign-in happens on RelativityOne's own page, so the instance's single sign-on and two-factor rules apply unchanged (*inferred*). **The code stops collecting the password:** after this change nothing receives, stores or forwards the person's user name and password, and the code holds only tokens. Every call now runs as the person who signed in, so **the calling identity changes**. **Scope:** no scope set is prescribed for Code; `openid RelativityRequestOrigin UserInfo UserInfoAccess AuthenticationID RelativityWeb` works, **verified by NSerio against a live instance; not documented by Relativity**, as working and not as minimal.

**Variant 1 — a Go web server.** **Precondition: this variant is for a Go server that signs a person in to RelativityOne. If the code is a command-line tool or a desktop process, use variant 2.**

A server-side component creates `state` and the PKCE verifier, redirects to the authorize endpoint, receives the code at its registered HTTPS Redirect URL, exchanges it, keeps the tokens on the server and gives the browser **only its own session**; **the browser never holds anything from RelativityOne**. The two handlers use the standard library's `net/http`: `http.Redirect` replies to the request with a redirect to the authorize address.

```go
type WebSignIn struct {
	mutex       sync.Mutex
	pending     map[string]string
	client      *http.Client
	redirectURI string
	keepTokens  func(w http.ResponseWriter, r *http.Request, tokens tokenResponse)
}

func (signIn *WebSignIn) Login(w http.ResponseWriter, r *http.Request) {
	state, err := randomToken(stateByteCount)
	if err != nil {
		http.Error(w, "sign-in unavailable", http.StatusInternalServerError)
		return
	}
	verifier, err := randomToken(verifierByteCount)
	if err != nil {
		http.Error(w, "sign-in unavailable", http.StatusInternalServerError)
		return
	}
	signIn.mutex.Lock()
	signIn.pending[state] = verifier
	signIn.mutex.Unlock()
	http.SetCookie(w, &http.Cookie{
		Name:     signInCookieName,
		Value:    state,
		Path:     "/",
		MaxAge:   signInCookieMaxAgeSeconds,
		HttpOnly: true,
		Secure:   true,
		SameSite: http.SameSiteLaxMode,
	})
	http.Redirect(w, r, authorizeAddress(signIn.redirectURI, state, challengeOf(verifier)), http.StatusFound)
}

func (signIn *WebSignIn) Callback(w http.ResponseWriter, r *http.Request) {
	query := r.URL.Query()
	if query.Get("error") != "" {
		http.Error(w, "sign-in was not completed", http.StatusUnauthorized)
		return
	}
	state := query.Get("state")
	cookie, err := r.Cookie(signInCookieName)
	if err != nil || query.Get("code") == "" || subtle.ConstantTimeCompare([]byte(cookie.Value), []byte(state)) != 1 {
		http.Error(w, "unknown sign-in", http.StatusBadRequest)
		return
	}
	signIn.mutex.Lock()
	verifier, known := signIn.pending[state]
	delete(signIn.pending, state)
	signIn.mutex.Unlock()
	if !known {
		http.Error(w, "unknown sign-in", http.StatusBadRequest)
		return
	}
	http.SetCookie(w, &http.Cookie{Name: signInCookieName, Path: "/", MaxAge: -1})
	tokens, err := exchangeCode(r.Context(), signIn.client, query.Get("code"), signIn.redirectURI, verifier)
	if err != nil {
		http.Error(w, "sign-in failed", http.StatusBadGateway)
		return
	}
	signIn.keepTokens(w, r, tokens)
}
```

**`state` is bound to the browser session**: `Login` stores it in a short-lived, `HttpOnly`, `Secure`, `SameSite=Lax` cookie as well as in the server's pending table, and `Callback` fails (400) unless the cookie is present **and** equals the `state` on the query, so a callback that arrives without the matching cookie gets no exchange. `Callback` also **handles the `error` parameter** (401, no exchange) and **an empty `code`** (400). `state` is consumed once: the entry is deleted before the exchange and the cookie is cleared, so a replayed or unknown `state` gets no exchange. `keepTokens` is yours: store the tokens against the visitor's session on the server and set a session cookie that holds **only a session identifier**. The refresh call, if used, goes to the same token endpoint with `grant_type=refresh_token` (*inferred*). **The calling identity changes** to the person who signed in.

**Variant 2 — a command-line tool or a desktop process.** **Precondition: this variant is for a Go command-line tool or a desktop process that signs a person in to RelativityOne. If the code is a server, use variant 1.**

The process starts a listener from the standard library bound to `127.0.0.1` only, on the **fixed port the administrator registered** (the exact URL is registered, so the port is fixed, chosen by the administrator and not at runtime), for **one request**; it **prints the authorize address** (so a machine with no browser, an SSH session or a failed launch has the manual route) and, if you want, opens the system browser yourself; reads `code` and `state` from the one request, answers with a short page, closes the listener, checks `state` and exchanges the code.

```go
func SignInFromCommandLine(ctx context.Context, client *http.Client, listenAddress string, redirectURI string) (tokenResponse, error) {
	var tokens tokenResponse
	redirect, err := url.Parse(redirectURI)
	if err != nil {
		return tokens, err
	}
	host, _, err := net.SplitHostPort(listenAddress)
	if err != nil {
		return tokens, err
	}
	if host != "127.0.0.1" && host != "::1" {
		return tokens, errors.New("the sign-in listener must be bound to the loopback address")
	}
	ctx, cancel := context.WithTimeout(ctx, signInTimeoutSeconds*time.Second)
	defer cancel()
	state, err := randomToken(stateByteCount)
	if err != nil {
		return tokens, err
	}
	verifier, err := randomToken(verifierByteCount)
	if err != nil {
		return tokens, err
	}
	listener, err := net.Listen("tcp", listenAddress)
	if err != nil {
		return tokens, fmt.Errorf("the registered port is not available: %w", err)
	}
	type callback struct{ code, state, failure string }
	received := make(chan callback, 1)
	server := &http.Server{
		ReadHeaderTimeout: serverHeaderTimeoutSeconds * time.Second,
		Handler: http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
			if r.URL.Path != redirect.Path {
				http.NotFound(w, r)
				return
			}
			query := r.URL.Query()
			select {
			case received <- callback{code: query.Get("code"), state: query.Get("state"), failure: query.Get("error")}:
			default:
			}
			fmt.Fprintln(w, "Sign-in received. You can close this window.")
		}),
	}
	go func() { _ = server.Serve(listener) }()
	defer server.Shutdown(context.Background())
	fmt.Println("Open this address in a browser to sign in:", authorizeAddress(redirectURI, state, challengeOf(verifier)))
	select {
	case result := <-received:
		if result.failure != "" || result.code == "" {
			return tokens, errors.New("sign-in was not completed")
		}
		if subtle.ConstantTimeCompare([]byte(result.state), []byte(state)) != 1 {
			return tokens, errors.New("the sign-in answer does not match the request")
		}
		return exchangeCode(ctx, client, result.code, redirectURI, verifier)
	case <-ctx.Done():
		return tokens, ctx.Err()
	}
}
```

**The listener is checked and bounded in the code**: it **refuses a `listenAddress` whose host is not `127.0.0.1` or `::1`** (loopback only); its handler **answers 404 to every path other than the registered redirect path** (so a stray request cannot consume the one-shot channel); the whole sign-in has **its own deadline** (`signInTimeoutSeconds`, 300 seconds, on top of the caller's context), after which it returns the context error; a callback with an `error` parameter or an empty `code` is an error, not an exchange. **Busy port:** the port is the registered one and is **not** replaced by another (a different port is a different Redirect URL, which Relativity would not match); if `net.Listen` fails because the port is taken, the function returns the error `the registered port is not available` and the tool must report it, not retry on another port. Call it with `listenAddress` `127.0.0.1:<the registered port>` and `redirectURI` `http://127.0.0.1:<the registered port>/<path>`. **Where the tokens are kept** is the program's decision: in memory for the run, or, for a refresh token, in a file whose permissions you restrict (**a distributed command-line tool carries its client secret in a form anyone who holds the distribution can extract**). Every call now runs as the person who signed in.

Verification: **Verification note.** **The tool resolved the called name to one function in this file that builds a `Basic` value, and did not establish where the header goes** (the finding is Medium for that reason): confirm that it is a request to RelativityOne; if it is not, this finding is informational. **Other callers may not appear as findings**: a call to a helper in another file or package, a method call and a name declared twice are not read, and a value held in a variable is not followed. **A token has a finite lifetime.** **The calling identity changes.**

OAuth2 Client: Create or confirm this OAuth2 Client in RelativityOne (NSerio's observation: the menu path is Home > Authentication > OAuth2 Client Manager, as Relativity's REST API authentication page names it). This tool never connects to RelativityOne and never creates or changes the OAuth2 Client; the settings below are guidance.
- Name: a descriptive name identifying the application it serves.
- Flow Grant Type: Code
- Redirect URLs: Relativity documentation: each URL includes the http or https protocol prefix. The exact callback URL is registered by the Relativity administrator. Server-side web applications, NSerio's recommendation: one exact HTTPS URL per environment, with no wildcards. Desktop and CLI applications: a plain-HTTP loopback URL, `http://127.0.0.1:<port>/<path>`, on a fixed port because the URL is registered in advance and the port cannot be chosen at runtime (verified by NSerio against a live instance; not documented by Relativity).
- Access Token Lifetime: 60 minutes, stated in minutes because the field is in minutes. Relativity documentation: tokens for this flow must have a short lifetime, and the recommended value matches the Identity Server default.
- Allow refresh tokens: NSerio's recommendation: leave it off unless the application must keep a user session active beyond the access token lifetime without asking the user to sign in again. If it is on, the refresh tokens are stored as secrets, per Secret Management (Part B of the Migration Documentation).
- Enabled: on.
<!-- generated:end GO-013 authorization-code -->

## Generic suggestion

The generic suggestion is what the tool gives a High or Medium finding of a construct its catalog has no entry for. In Go every class that can reach High or Medium has its own entries above, so the generic suggestion covers a class that a later release adds without one. It is a pointer and not an instruction: the migration must be designed from the code by whoever owns it.

### Client Credentials

<!-- generated:begin generic client-credentials -->
**Confirming the flow.** When no signal in a file decides the flow, the tool assigns Client Credentials, the default, with this note: "Who supplies the credential at run time? If a person types it or signs in, the integration acts for that person: use Authorization Code. If it comes from configuration, settings, an environment variable or a secret store: use Client Credentials. No signal in this file showed which applies, so Client Credentials, the default, is recommended. Use Authorization Code instead if the integration acts on behalf of a signed-in user, or if your RelativityOne instance already has a Code-flow OAuth2 Client for this integration." A fixed-credential signal assigns it with this note: "A fixed-credential signal in this file points to Client Credentials, but how often that kind of signal is right has not been measured. Use Authorization Code instead if the integration acts on behalf of a signed-in user, or if your RelativityOne instance already has a Code-flow OAuth2 Client for this integration." See [Confirming the flow](#confirming-the-flow).

The tool reports Basic Authentication at this location but did not recognize the construct, so the change has not been characterized and no before-and-after form is offered here. If the destination is not RelativityOne, the migration is that service's own and this finding is informational (that Relativity's identity endpoint issues no tokens for another service is unverified). If the destination is RelativityOne, Relativity's documentation recommends bearer tokens and states that Basic Authentication is no longer supported for named APIs, with no date for its removal asserted (the platform's direction beyond that is NSerio's reading, unverified). The documented destination in Go is a bearer `Authorization` header sent by the standard library's `net/http` client, carrying a token obtained from the platform's identity endpoint with the client-credentials grant, from a Go process or service that holds the credential. The client ID and secret that obtain that token belong in the process environment or a secret store, per Secret Management (Part B of the Migration Documentation), and never in source or in a log. Beyond those facts this is a pointer and not an instruction: the migration must be designed from the code by whoever owns it, rather than applied from this text.

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

The tool reports Basic Authentication at this location but did not recognize the construct, so the change has not been characterized and no before-and-after form is offered here. If the destination is not RelativityOne, the migration is that service's own and this finding is informational (that Relativity's identity endpoint issues no tokens for another service is unverified). If the destination is RelativityOne, Relativity's documentation recommends bearer tokens and states that Basic Authentication is no longer supported for named APIs, with no date for its removal asserted (the platform's direction beyond that is NSerio's reading, unverified), and this finding is assigned the Authorization Code flow, in which the code sends the person to RelativityOne to sign in, receives an authorization code at a Redirect URL registered on its OAuth2 Client, and exchanges the code for an access token that it sends as a Bearer token. Code requires a client secret, so the exchange runs in a Go server process or in a command-line tool: for a Go web server the callback handler performs it; for a command-line tool or a desktop process a loopback listener on the registered fixed port receives the code. The authorize endpoint is `<host>/Relativity/Identity/connect/authorize` and the code is exchanged at `<host>/Relativity/Identity/connect/token`; both are verified by NSerio against a live instance; not documented by Relativity. PKCE is required by Relativity's own documentation to obtain a refresh token; NSerio recommends it for every Authorization Code client, and whether a client with refresh tokens disabled accepts it is unverified. The client ID and secret belong in the process environment or a secret store, per Secret Management (Part B of the Migration Documentation), never in source or in a log. After the change the code no longer collects or forwards the person's password. Beyond those facts this is a pointer and not an instruction: the migration must be designed from the code by whoever owns it, rather than applied from this text.

OAuth2 Client: Create or confirm this OAuth2 Client in RelativityOne (NSerio's observation: the menu path is Home > Authentication > OAuth2 Client Manager, as Relativity's REST API authentication page names it). This tool never connects to RelativityOne and never creates or changes the OAuth2 Client; the settings below are guidance.
- Name: a descriptive name identifying the application it serves.
- Flow Grant Type: Code
- Redirect URLs: Relativity documentation: each URL includes the http or https protocol prefix. The exact callback URL is registered by the Relativity administrator. Server-side web applications, NSerio's recommendation: one exact HTTPS URL per environment, with no wildcards. Desktop and CLI applications: a plain-HTTP loopback URL, `http://127.0.0.1:<port>/<path>`, on a fixed port because the URL is registered in advance and the port cannot be chosen at runtime (verified by NSerio against a live instance; not documented by Relativity).
- Access Token Lifetime: 60 minutes, stated in minutes because the field is in minutes. Relativity documentation: tokens for this flow must have a short lifetime, and the recommended value matches the Identity Server default.
- Allow refresh tokens: NSerio's recommendation: leave it off unless the application must keep a user session active beyond the access token lifetime without asking the user to sign in again. If it is on, the refresh tokens are stored as secrets, per Secret Management (Part B of the Migration Documentation).
- Enabled: on.
<!-- generated:end generic authorization-code -->
