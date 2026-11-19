using System.Collections.Frozen;

using MigrateAuth.Cli.Findings;
using MigrateAuth.Cli.Reporting;

namespace MigrateAuth.Cli.Catalog.Languages;

/// <summary>
/// The recommendation text for `Go`: ten classes, both flows and both generic fallback texts. The three classes that are Low by definition (the transport that writes Basic into the requests it
/// forwards, the helper that builds a header or a pair, and the header that takes its whole value from the environment) carry no entry: they are routed to manual review, and their reason sentences are
/// the shared Low vocabulary's.
///
/// HOW A RECOMMENDATION IS COMPOSED. Part 1 is the class's four fields and is flow-neutral, so the source form, the target form and the verification note are the same text for both flows. The credential
/// provenance is the class's own share followed by part 2 for the finding's flow, held once here as <see cref="CLIENT_CREDENTIALS_TOKEN_ACQUISITION"/> and
/// <see cref="AUTHORIZATION_CODE_TOKEN_ACQUISITION"/>. Part 3, the OAuth2 Client block, is appended by the shared engine's composer and is not repeated here.
///
/// STANDARD LIBRARY ONLY. Every block uses the packages <c>net/http</c>, <c>net/url</c>, <c>encoding/json</c>, <c>crypto/rand</c> and <c>crypto/sha256</c>, with <c>context</c>, <c>crypto/subtle</c>,
/// <c>encoding/base64</c>, <c>errors</c>, <c>fmt</c>, <c>net</c>, <c>os</c>, <c>strings</c>, <c>sync</c> and <c>time</c>; no module is added. The web-server variant of Authorization Code serves the redirect and the
/// callback with <c>net/http</c> and the command-line variant listens on the loopback address with <c>net</c>.
///
/// ENCODING CHOICES. Every code block is written once, with its one level of block indentation removed and each line indented with tabs only. The source and analysis identifiers that a
/// research text cites in parentheses are dropped, because the reader of a report has no document to resolve them against: a fact is labelled in words. Every secret-handling sentence names
/// <see cref="SecretManagementPointer.PART_B_NAME"/>, and the label of a fact NSerio verified but Relativity does not document is
/// <see cref="OAuth2ClientConfigurationBlocks.NSERIO_VERIFIED_LABEL"/>, both through the constants the shared engine already owns. A template never names a Relativity fact it cannot state in words; the
/// destination is always asked first, because the engine cannot see which host a program calls. Neither Device Code nor the Resource Owner grant is named anywhere.
///
/// A SEPARATE FILE BECAUSE <c>GoAnalyzer</c> IS <c>file</c>-SCOPED: its type cannot be split across files, so the text lives in this <c>internal</c> holder and the analyzer's template provider reads it by
/// the same identifiers.
/// </summary>
internal static class GoRecommendationTemplates
{
	private const string _GO_VERSIONS = "every Go version";

	private const string _DESTINATION_QUESTION =
		"**First, the destination:** if this Relativity instance is not RelativityOne — including a Relativity Server instance, whose own documentation governs — this finding is informational and the rest of this template does not apply.";

	private const string _TOKEN_LIFETIME = "**A token has a finite lifetime.**";

	private const string _CALLING_IDENTITY = "**The calling identity changes.**";

	private const string _ROUTE_BY_DESTINATION =
		"**Where the credentials come from.** The source form establishes that the code sends Basic, not that the destination is RelativityOne. If it is not RelativityOne, the migration is that service's own token mechanism, this finding is informational for this tool, and the reader stops here. If it is RelativityOne, the token comes from the token-acquisition part that follows, and the user name and password are replaced by the OAuth2 Client's client ID and secret, kept as " + SecretManagementPointer.PART_B_NAME + " describes and never in source.";

	private const string _ROTATION_STATEMENT =
		"**Treat this credential as compromised. Rotate it, and assume the repository history has published it. Completing the migration does not close this exposure; rotation does.**";

	private const string _FENCE_GO_OPEN = "```go\n";
	private const string _FENCE_CLOSE = "\n```";
	private const string _PARAGRAPH_SEPARATOR = "\n\n";

	internal const string SHARED_DECLARATIONS_BLOCK =
		"const (\n"
		+ "\tidentityTokenURL            = \"https://HOST/Relativity/Identity/connect/token\"\n"
		+ "\tidentityAuthorizeURL        = \"https://HOST/Relativity/Identity/connect/authorize\"\n"
		+ "\tclientIDEnvironmentName     = \"RELATIVITY_CLIENT_ID\"\n"
		+ "\tclientSecretEnvironmentName = \"RELATIVITY_CLIENT_SECRET\"\n"
		+ "\texpiryMarginSeconds         = 60\n"
		+ "\tcsrfHeaderName              = \"X-CSRF-Header\"\n"
		+ "\tcsrfHeaderValue             = \"-\"\n"
		+ "\tstateByteCount              = 16\n"
		+ "\tverifierByteCount           = 32\n"
		+ "\tserverHeaderTimeoutSeconds  = 10\n"
		+ "\tsignInCookieName            = \"relativity_sign_in\"\n"
		+ "\tsignInCookieMaxAgeSeconds   = 600\n"
		+ "\tsignInTimeoutSeconds        = 300\n"
		+ ")\n"
		+ "\n"
		+ "type tokenResponse struct {\n"
		+ "\tAccessToken  string `json:\"access_token\"`\n"
		+ "\tRefreshToken string `json:\"refresh_token\"`\n"
		+ "\tExpiresIn    int    `json:\"expires_in\"`\n"
		+ "}\n"
		+ "\n"
		+ "func requestToken(ctx context.Context, client *http.Client, form url.Values) (tokenResponse, error) {\n"
		+ "\tvar payload tokenResponse\n"
		+ "\trequest, err := http.NewRequestWithContext(ctx, http.MethodPost, identityTokenURL, strings.NewReader(form.Encode()))\n"
		+ "\tif err != nil {\n"
		+ "\t\treturn payload, err\n"
		+ "\t}\n"
		+ "\trequest.Header.Set(\"Content-Type\", \"application/x-www-form-urlencoded\")\n"
		+ "\tresponse, err := client.Do(request)\n"
		+ "\tif err != nil {\n"
		+ "\t\treturn payload, err\n"
		+ "\t}\n"
		+ "\tdefer response.Body.Close()\n"
		+ "\tif response.StatusCode != http.StatusOK {\n"
		+ "\t\treturn payload, fmt.Errorf(\"token endpoint answered %d\", response.StatusCode)\n"
		+ "\t}\n"
		+ "\terr = json.NewDecoder(response.Body).Decode(&payload)\n"
		+ "\treturn payload, err\n"
		+ "}";

	internal const string GO_001_SOURCE_BLOCK =
		"request.SetBasicAuth(userName, secret)";

	internal const string GO_001_TARGET_BLOCK =
		"request.Header.Set(\"Authorization\", \"Bearer \"+accessToken)\n"
		+ "request.Header.Set(csrfHeaderName, csrfHeaderValue)";

	internal const string BEARER_TRANSPORT_BLOCK =
		"type bearerTransport struct {\n"
		+ "\tsource *TokenSource\n"
		+ "\tbase   http.RoundTripper\n"
		+ "}\n"
		+ "\n"
		+ "func (transport *bearerTransport) RoundTrip(request *http.Request) (*http.Response, error) {\n"
		+ "\ttoken, err := transport.source.Token(request.Context())\n"
		+ "\tif err != nil {\n"
		+ "\t\treturn nil, err\n"
		+ "\t}\n"
		+ "\tclone := request.Clone(request.Context())\n"
		+ "\tclone.Header.Set(\"Authorization\", \"Bearer \"+token)\n"
		+ "\tclone.Header.Set(csrfHeaderName, csrfHeaderValue)\n"
		+ "\treturn transport.base.RoundTrip(clone)\n"
		+ "}\n"
		+ "\n"
		+ "func targetClientWithBearerTransport(source *TokenSource) *http.Client {\n"
		+ "\treturn &http.Client{Transport: &bearerTransport{source: source, base: http.DefaultTransport}}\n"
		+ "}";

	internal const string GO_002_SOURCE_BLOCK =
		"encoded := base64.StdEncoding.EncodeToString([]byte(userName + \":\" + secret))\n"
		+ "request.Header.Set(\"Authorization\", \"Basic \"+encoded)";

	internal const string GO_002_TARGET_BLOCK =
		"request.Header.Set(\"Authorization\", \"Bearer \"+accessToken)\n"
		+ "request.Header.Set(csrfHeaderName, csrfHeaderValue)";

	internal const string GO_003_SOURCE_BLOCK =
		"encoded := base64.StdEncoding.EncodeToString([]byte(userName + \":\" + secret))\n"
		+ "return http.Header{\"Authorization\": {\"Basic \" + encoded}}";

	internal const string GO_003_TARGET_BLOCK =
		"return http.Header{\n"
		+ "\t\"Authorization\": {\"Bearer \" + accessToken},\n"
		+ "\tcsrfHeaderName:  {csrfHeaderValue},\n"
		+ "}";

	internal const string GO_004_SOURCE_BLOCK =
		"encoded := base64.StdEncoding.EncodeToString([]byte(userName + \":\" + secret))\n"
		+ "return map[string]string{\"Authorization\": \"Basic \" + encoded}";

	internal const string GO_004_TARGET_BLOCK =
		"return map[string]string{\n"
		+ "\t\"Authorization\": \"Bearer \" + accessToken,\n"
		+ "\tcsrfHeaderName:  csrfHeaderValue,\n"
		+ "}";

	internal const string GO_005_SOURCE_BLOCK =
		"return http.Get(\"https://user:secret@host.example/path\")";

	internal const string GO_005_TARGET_BLOCK =
		"request, err := http.NewRequestWithContext(ctx, http.MethodGet, \"https://host.example/path\", nil)\n"
		+ "if err != nil {\n"
		+ "\treturn nil, err\n"
		+ "}\n"
		+ "request.Header.Set(\"Authorization\", \"Bearer \"+accessToken)\n"
		+ "request.Header.Set(csrfHeaderName, csrfHeaderValue)\n"
		+ "return client.Do(request)";

	internal const string GO_006_SOURCE_BLOCK =
		"encoded := base64.StdEncoding.EncodeToString([]byte(userName + \":\" + secret))\n"
		+ "request.Header.Set(\"Authorization\", \"Basic \"+encoded)";

	internal const string GO_006_TARGET_BLOCK =
		"request.Header.Set(\"Authorization\", \"Bearer \"+accessToken)\n"
		+ "request.Header.Set(csrfHeaderName, csrfHeaderValue)";

	internal const string GO_007_SOURCE_BLOCK =
		"address.User = url.UserPassword(userName, secret)";

	internal const string GO_007_TARGET_BLOCK =
		"address.User = nil\n"
		+ "request.Header.Set(\"Authorization\", \"Bearer \"+accessToken)\n"
		+ "request.Header.Set(csrfHeaderName, csrfHeaderValue)";

	internal const string GO_008_SOURCE_BLOCK =
		"return url.Parse(\"https://user:secret@host.example/path\")";

	internal const string GO_008_TARGET_BLOCK =
		"return url.Parse(\"https://host.example/path\")";

	internal const string GO_009_SOURCE_BLOCK =
		"request.Header.Set(\"Authorization\", header)";

	internal const string GO_009_TARGET_BLOCK =
		"request.Header.Set(\"Authorization\", \"Bearer \"+accessToken)\n"
		+ "request.Header.Set(csrfHeaderName, csrfHeaderValue)";

	internal const string GO_013_SOURCE_BLOCK =
		"request.Header.Set(\"Authorization\", basicHeader(userName, secret))";

	internal const string GO_013_TARGET_BLOCK =
		"request.Header.Set(\"Authorization\", \"Bearer \"+accessToken)\n"
		+ "request.Header.Set(csrfHeaderName, csrfHeaderValue)";

	internal const string CLIENT_CREDENTIALS_TOKEN_SOURCE_BLOCK =
		"type TokenSource struct {\n"
		+ "\tmutex     sync.Mutex\n"
		+ "\tclient    *http.Client\n"
		+ "\ttoken     string\n"
		+ "\texpiresAt time.Time\n"
		+ "}\n"
		+ "\n"
		+ "func (source *TokenSource) Token(ctx context.Context) (string, error) {\n"
		+ "\tsource.mutex.Lock()\n"
		+ "\tdefer source.mutex.Unlock()\n"
		+ "\tif source.token == \"\" || time.Until(source.expiresAt) < expiryMarginSeconds*time.Second {\n"
		+ "\t\tform := url.Values{\n"
		+ "\t\t\t\"grant_type\":    {\"client_credentials\"},\n"
		+ "\t\t\t\"client_id\":     {os.Getenv(clientIDEnvironmentName)},\n"
		+ "\t\t\t\"client_secret\": {os.Getenv(clientSecretEnvironmentName)},\n"
		+ "\t\t}\n"
		+ "\t\tpayload, err := requestToken(ctx, source.client, form)\n"
		+ "\t\tif err != nil {\n"
		+ "\t\t\treturn \"\", err\n"
		+ "\t\t}\n"
		+ "\t\tsource.token = payload.AccessToken\n"
		+ "\t\tsource.expiresAt = time.Now().Add(time.Duration(payload.ExpiresIn) * time.Second)\n"
		+ "\t}\n"
		+ "\treturn source.token, nil\n"
		+ "}";

	internal const string AUTHORIZATION_CODE_HELPERS_BLOCK =
		"func randomToken(byteCount int) (string, error) {\n"
		+ "\tbuffer := make([]byte, byteCount)\n"
		+ "\tif _, err := rand.Read(buffer); err != nil {\n"
		+ "\t\treturn \"\", err\n"
		+ "\t}\n"
		+ "\treturn base64.RawURLEncoding.EncodeToString(buffer), nil\n"
		+ "}\n"
		+ "\n"
		+ "func challengeOf(verifier string) string {\n"
		+ "\tdigest := sha256.Sum256([]byte(verifier))\n"
		+ "\treturn base64.RawURLEncoding.EncodeToString(digest[:])\n"
		+ "}\n"
		+ "\n"
		+ "func authorizeAddress(redirectURI string, state string, challenge string) string {\n"
		+ "\tquery := url.Values{\n"
		+ "\t\t\"client_id\":     {os.Getenv(clientIDEnvironmentName)},\n"
		+ "\t\t\"response_type\": {\"code\"},\n"
		+ "\t\t\"redirect_uri\":  {redirectURI},\n"
		+ "\t\t\"state\":         {state},\n"
		+ "\t}\n"
		+ "\tif challenge != \"\" {\n"
		+ "\t\tquery.Set(\"code_challenge\", challenge)\n"
		+ "\t\tquery.Set(\"code_challenge_method\", \"S256\")\n"
		+ "\t}\n"
		+ "\treturn identityAuthorizeURL + \"?\" + query.Encode()\n"
		+ "}\n"
		+ "\n"
		+ "func exchangeCode(ctx context.Context, client *http.Client, code string, redirectURI string, verifier string) (tokenResponse, error) {\n"
		+ "\tform := url.Values{\n"
		+ "\t\t\"grant_type\":    {\"authorization_code\"},\n"
		+ "\t\t\"code\":          {code},\n"
		+ "\t\t\"redirect_uri\":  {redirectURI},\n"
		+ "\t\t\"client_id\":     {os.Getenv(clientIDEnvironmentName)},\n"
		+ "\t\t\"client_secret\": {os.Getenv(clientSecretEnvironmentName)},\n"
		+ "\t}\n"
		+ "\tif verifier != \"\" {\n"
		+ "\t\tform.Set(\"code_verifier\", verifier)\n"
		+ "\t}\n"
		+ "\treturn requestToken(ctx, client, form)\n"
		+ "}";

	internal const string AUTHORIZATION_CODE_WEB_SERVER_BLOCK =
		"type WebSignIn struct {\n"
		+ "\tmutex       sync.Mutex\n"
		+ "\tpending     map[string]string\n"
		+ "\tclient      *http.Client\n"
		+ "\tredirectURI string\n"
		+ "\tkeepTokens  func(w http.ResponseWriter, r *http.Request, tokens tokenResponse)\n"
		+ "}\n"
		+ "\n"
		+ "func (signIn *WebSignIn) Login(w http.ResponseWriter, r *http.Request) {\n"
		+ "\tstate, err := randomToken(stateByteCount)\n"
		+ "\tif err != nil {\n"
		+ "\t\thttp.Error(w, \"sign-in unavailable\", http.StatusInternalServerError)\n"
		+ "\t\treturn\n"
		+ "\t}\n"
		+ "\tverifier, err := randomToken(verifierByteCount)\n"
		+ "\tif err != nil {\n"
		+ "\t\thttp.Error(w, \"sign-in unavailable\", http.StatusInternalServerError)\n"
		+ "\t\treturn\n"
		+ "\t}\n"
		+ "\tsignIn.mutex.Lock()\n"
		+ "\tsignIn.pending[state] = verifier\n"
		+ "\tsignIn.mutex.Unlock()\n"
		+ "\thttp.SetCookie(w, &http.Cookie{\n"
		+ "\t\tName:     signInCookieName,\n"
		+ "\t\tValue:    state,\n"
		+ "\t\tPath:     \"/\",\n"
		+ "\t\tMaxAge:   signInCookieMaxAgeSeconds,\n"
		+ "\t\tHttpOnly: true,\n"
		+ "\t\tSecure:   true,\n"
		+ "\t\tSameSite: http.SameSiteLaxMode,\n"
		+ "\t})\n"
		+ "\thttp.Redirect(w, r, authorizeAddress(signIn.redirectURI, state, challengeOf(verifier)), http.StatusFound)\n"
		+ "}\n"
		+ "\n"
		+ "func (signIn *WebSignIn) Callback(w http.ResponseWriter, r *http.Request) {\n"
		+ "\tquery := r.URL.Query()\n"
		+ "\tif query.Get(\"error\") != \"\" {\n"
		+ "\t\thttp.Error(w, \"sign-in was not completed\", http.StatusUnauthorized)\n"
		+ "\t\treturn\n"
		+ "\t}\n"
		+ "\tstate := query.Get(\"state\")\n"
		+ "\tcookie, err := r.Cookie(signInCookieName)\n"
		+ "\tif err != nil || query.Get(\"code\") == \"\" || subtle.ConstantTimeCompare([]byte(cookie.Value), []byte(state)) != 1 {\n"
		+ "\t\thttp.Error(w, \"unknown sign-in\", http.StatusBadRequest)\n"
		+ "\t\treturn\n"
		+ "\t}\n"
		+ "\tsignIn.mutex.Lock()\n"
		+ "\tverifier, known := signIn.pending[state]\n"
		+ "\tdelete(signIn.pending, state)\n"
		+ "\tsignIn.mutex.Unlock()\n"
		+ "\tif !known {\n"
		+ "\t\thttp.Error(w, \"unknown sign-in\", http.StatusBadRequest)\n"
		+ "\t\treturn\n"
		+ "\t}\n"
		+ "\thttp.SetCookie(w, &http.Cookie{Name: signInCookieName, Path: \"/\", MaxAge: -1})\n"
		+ "\ttokens, err := exchangeCode(r.Context(), signIn.client, query.Get(\"code\"), signIn.redirectURI, verifier)\n"
		+ "\tif err != nil {\n"
		+ "\t\thttp.Error(w, \"sign-in failed\", http.StatusBadGateway)\n"
		+ "\t\treturn\n"
		+ "\t}\n"
		+ "\tsignIn.keepTokens(w, r, tokens)\n"
		+ "}";

	internal const string AUTHORIZATION_CODE_COMMAND_LINE_BLOCK =
		"func SignInFromCommandLine(ctx context.Context, client *http.Client, listenAddress string, redirectURI string) (tokenResponse, error) {\n"
		+ "\tvar tokens tokenResponse\n"
		+ "\tredirect, err := url.Parse(redirectURI)\n"
		+ "\tif err != nil {\n"
		+ "\t\treturn tokens, err\n"
		+ "\t}\n"
		+ "\thost, _, err := net.SplitHostPort(listenAddress)\n"
		+ "\tif err != nil {\n"
		+ "\t\treturn tokens, err\n"
		+ "\t}\n"
		+ "\tif host != \"127.0.0.1\" && host != \"::1\" {\n"
		+ "\t\treturn tokens, errors.New(\"the sign-in listener must be bound to the loopback address\")\n"
		+ "\t}\n"
		+ "\tctx, cancel := context.WithTimeout(ctx, signInTimeoutSeconds*time.Second)\n"
		+ "\tdefer cancel()\n"
		+ "\tstate, err := randomToken(stateByteCount)\n"
		+ "\tif err != nil {\n"
		+ "\t\treturn tokens, err\n"
		+ "\t}\n"
		+ "\tverifier, err := randomToken(verifierByteCount)\n"
		+ "\tif err != nil {\n"
		+ "\t\treturn tokens, err\n"
		+ "\t}\n"
		+ "\tlistener, err := net.Listen(\"tcp\", listenAddress)\n"
		+ "\tif err != nil {\n"
		+ "\t\treturn tokens, fmt.Errorf(\"the registered port is not available: %w\", err)\n"
		+ "\t}\n"
		+ "\ttype callback struct{ code, state, failure string }\n"
		+ "\treceived := make(chan callback, 1)\n"
		+ "\tserver := &http.Server{\n"
		+ "\t\tReadHeaderTimeout: serverHeaderTimeoutSeconds * time.Second,\n"
		+ "\t\tHandler: http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {\n"
		+ "\t\t\tif r.URL.Path != redirect.Path {\n"
		+ "\t\t\t\thttp.NotFound(w, r)\n"
		+ "\t\t\t\treturn\n"
		+ "\t\t\t}\n"
		+ "\t\t\tquery := r.URL.Query()\n"
		+ "\t\t\tselect {\n"
		+ "\t\t\tcase received <- callback{code: query.Get(\"code\"), state: query.Get(\"state\"), failure: query.Get(\"error\")}:\n"
		+ "\t\t\tdefault:\n"
		+ "\t\t\t}\n"
		+ "\t\t\tfmt.Fprintln(w, \"Sign-in received. You can close this window.\")\n"
		+ "\t\t}),\n"
		+ "\t}\n"
		+ "\tgo func() { _ = server.Serve(listener) }()\n"
		+ "\tdefer server.Shutdown(context.Background())\n"
		+ "\tfmt.Println(\"Open this address in a browser to sign in:\", authorizeAddress(redirectURI, state, challengeOf(verifier)))\n"
		+ "\tselect {\n"
		+ "\tcase result := <-received:\n"
		+ "\t\tif result.failure != \"\" || result.code == \"\" {\n"
		+ "\t\t\treturn tokens, errors.New(\"sign-in was not completed\")\n"
		+ "\t\t}\n"
		+ "\t\tif subtle.ConstantTimeCompare([]byte(result.state), []byte(state)) != 1 {\n"
		+ "\t\t\treturn tokens, errors.New(\"the sign-in answer does not match the request\")\n"
		+ "\t\t}\n"
		+ "\t\treturn exchangeCode(ctx, client, result.code, redirectURI, verifier)\n"
		+ "\tcase <-ctx.Done():\n"
		+ "\t\treturn tokens, ctx.Err()\n"
		+ "\t}\n"
		+ "}";

	/// <summary>The Client Credentials generic fallback prose for a `Go` finding, with the secret-handling sentence naming the Part B pointer.</summary>
	public const string GO_LANGUAGE_FALLBACK_TEXT =
		"The tool reports Basic Authentication at this location but did not recognize the construct, so the change has not been characterized and no before-and-after form is offered here. If the destination is not RelativityOne, the migration is that service's own and this finding is informational (that Relativity's identity endpoint issues no tokens for another service is unverified). If the destination is RelativityOne, Relativity's documentation recommends bearer tokens and states that Basic Authentication is no longer supported for named APIs, with no date for its removal asserted (the platform's direction beyond that is NSerio's reading, unverified). The documented destination in Go is a bearer `Authorization` header sent by the standard library's `net/http` client, carrying a token obtained from the platform's identity endpoint with the client-credentials grant, from a Go process or service that holds the credential. The client ID and secret that obtain that token belong in the process environment or a secret store, per " + SecretManagementPointer.PART_B_NAME + ", and never in source or in a log. Beyond those facts this is a pointer and not an instruction: the migration must be designed from the code by whoever owns it, rather than applied from this text.";

	/// <summary>The Authorization Code generic fallback prose for a `Go` finding.</summary>
	public const string GO_CODE_LANGUAGE_FALLBACK_TEXT =
		"The tool reports Basic Authentication at this location but did not recognize the construct, so the change has not been characterized and no before-and-after form is offered here. If the destination is not RelativityOne, the migration is that service's own and this finding is informational (that Relativity's identity endpoint issues no tokens for another service is unverified). If the destination is RelativityOne, Relativity's documentation recommends bearer tokens and states that Basic Authentication is no longer supported for named APIs, with no date for its removal asserted (the platform's direction beyond that is NSerio's reading, unverified), and this finding is assigned the Authorization Code flow, in which the code sends the person to RelativityOne to sign in, receives an authorization code at a Redirect URL registered on its OAuth2 Client, and exchanges the code for an access token that it sends as a Bearer token. Code requires a client secret, so the exchange runs in a Go server process or in a command-line tool: for a Go web server the callback handler performs it; for a command-line tool or a desktop process a loopback listener on the registered fixed port receives the code. The authorize endpoint is `<host>/Relativity/Identity/connect/authorize` and the code is exchanged at `<host>/Relativity/Identity/connect/token`; both are " + OAuth2ClientConfigurationBlocks.NSERIO_VERIFIED_LABEL + ". PKCE is required by Relativity's own documentation to obtain a refresh token; NSerio recommends it for every Authorization Code client, and whether a client with refresh tokens disabled accepts it is unverified. The client ID and secret belong in the process environment or a secret store, per " + SecretManagementPointer.PART_B_NAME + ", never in source or in a log. After the change the code no longer collects or forwards the person's password. Beyond those facts this is a pointer and not an instruction: the migration must be designed from the code by whoever owns it, rather than applied from this text.";

	/// <summary>Part 2 for Client Credentials: how the token is obtained and kept. Appended to every class's credential-provenance share for a Client Credentials finding.</summary>
	public const string CLIENT_CREDENTIALS_TOKEN_ACQUISITION =
		"**Where the token comes from.** Create an OAuth2 Client with Flow Grant Type set to Client Credentials (part 3 lists its settings), then send a form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=client_credentials`, `client_id` and `client_secret`. The token endpoint is **documented** for this grant. The token is then sent as `Authorization: Bearer <token>`, with `X-CSRF-Header: -` on REST and Kepler calls. The scope depends on what the integration needs and **no single scope is required**: Relativity's documentation shows `SystemUserInfo` and `UserInfoAccess` and no page says which is correct or what either grants; use the value the API you call documents, and confirm it. Every block below uses only the standard library and needs go1.13 or later.\n\n"
		+ "**Shared declarations.** Every block of this text, and of the use-site forms above, uses these constants and helpers; `HOST` is the RelativityOne host.\n\n"
		+ _FENCE_GO_OPEN + SHARED_DECLARATIONS_BLOCK + _FENCE_CLOSE + "\n\n"
		+ "The token request is a form-encoded `POST`. **Where the client secret travels in the token request** (the form body, as here, or HTTP Basic client authentication) **and the field names of the token response are not documented by Relativity**: the form body and `tokenResponse` are the standard OAuth shape and an NSerio recommendation, **not verified** against RelativityOne. **Do not** send the client secret with `SetBasicAuth` and **do not** put it in the URL.\n\n"
		+ "**Variant: a Go process acting as itself.** **Precondition: this variant is for a Go job, daemon or service that does not act for a signed-in person.**\n\n"
		+ _FENCE_GO_OPEN + CLIENT_CREDENTIALS_TOKEN_SOURCE_BLOCK + _FENCE_CLOSE + "\n\n"
		+ "**Cache the token and re-acquire it on expiry — do not refresh.** Client Credentials does not offer refresh tokens: **" + OAuth2ClientConfigurationBlocks.NSERIO_VERIFIED_LABEL + "** — the Allow Refresh Tokens control does not appear for this grant in the RelativityOne user interface. The recommended Access Token Lifetime is 60 minutes. **The client secret** belongs in the process environment or a secret store, as " + SecretManagementPointer.PART_B_NAME + " describes, **never in source, in a tracked `.env` file or in a log**: the block reads it with `os.Getenv`, which returns an empty string when the variable is not present, so an unset variable is an empty secret and the token request fails, which the code reports. A reset invalidates the previous secret immediately, so plan the cut-over; a second client created before the reset is an inferred way to do it without a gap. **Before deploying, confirm the calling identity:** every call now runs as the OAuth2 Client's Context User, not as the user in the replaced credential, and a non-administrator Context User is NSerio's recommendation. Trusted IPs restrict where a token can be requested, not a token already issued. The statements that Client Credentials needs a client secret and that the secret is displayed once are carried from the other languages' guidance and are not verified for this text.\n\n"
		+ "**Variant: a Go web service that does not act for a signed-in user.** **Precondition: this variant is for a Go server that calls RelativityOne with no person signed in to it, so no person's identity exists to preserve. If a person signs in, use Authorization Code, variant 1.**\n\n"
		+ "Use the block above in the server process; **the server must authenticate and authorize its own callers, and the OAuth2 Client's Context User must hold the minimum permissions the operation needs. Without both, the result is an unauthenticated relay into RelativityOne**: every caller reaching it acts with the Context User's full rights, which is strictly worse than the starting point. The handler returns results to its caller, **never the token** and never the secret.";

	/// <summary>Part 2 for Authorization Code: the common part and both variants, each with its precondition. Appended to every class's credential-provenance share for an Authorization Code finding.</summary>
	public const string AUTHORIZATION_CODE_TOKEN_ACQUISITION =
		"**Common to both variants.** The person signs in on RelativityOne's own page and the code never holds their password. The text has a common part and two variants by application type, and each variant opens with its own precondition, so that the reader can discard the others by reading one sentence each. Every block uses only the standard library and needs go1.13 or later; the shared declarations are those of the Client Credentials text and are repeated here.\n\n"
		+ _FENCE_GO_OPEN + SHARED_DECLARATIONS_BLOCK + _FENCE_CLOSE + "\n\n"
		+ "**Sign in.** Replace the code that collected the person's credentials with a redirect to the authorize endpoint `<host>/Relativity/Identity/connect/authorize`, which is **" + OAuth2ClientConfigurationBlocks.NSERIO_VERIFIED_LABEL + "** and leads to RelativityOne's login page. The request carries the standard authorization-code parameters — `client_id`, `response_type=code`, the registered `redirect_uri`, `state` checked on return, and `code_challenge` with `code_challenge_method=S256` when PKCE is used; these are standard OAuth and Relativity documents none of them.\n\n"
		+ "**PKCE.** Relativity's own documentation requires PKCE to obtain a refresh token; that a Code request that does not ask for a refresh token works without PKCE is **" + OAuth2ClientConfigurationBlocks.NSERIO_VERIFIED_LABEL + "**. **PKCE is not limited to clients that use refresh tokens**: NSerio recommends `S256` for every Authorization Code client (NSerio recommendation) and does not claim Relativity accepts any particular method. **Whether a client with refresh tokens disabled accepts a PKCE request (a `code_challenge`) is unverified**: test it before the block is relied on for such a client. **`state`** is generated, stored against the session and compared on return; `nonce` is not used, and Relativity does not document whether it enforces either. The authorize and token endpoints and the PKCE and `state` behaviour above are **" + OAuth2ClientConfigurationBlocks.NSERIO_VERIFIED_LABEL + "**. The block draws the verifier and `state` from `crypto/rand`, whose `Read` fills the buffer with cryptographically secure random bytes (the signature still returns an error, which the block checks), and computes the challenge with `crypto/sha256` and `base64.RawURLEncoding`, the unpadded URL-safe encoding; a 32-byte value encodes to a 43-character verifier.\n\n"
		+ "**The client secret is required.** Code does not work without a client secret: tested and rejected, **" + OAuth2ClientConfigurationBlocks.NSERIO_VERIFIED_LABEL + "**, which confirms Relativity's own statement. **Any secret placed in a distributed binary is extractable by whoever holds the binary**: a Go program compiled for people other than its owner carries its client secret in a form anyone who holds the file can extract, and Relativity documents no recommendation for that case; use a short Access Token Lifetime, Trusted IPs where the outbound addresses are stable and one OAuth2 Client per distribution as compensating guidance (NSerio recommendation). Keep the secret as " + SecretManagementPointer.PART_B_NAME + " describes, never in source or in a log.\n\n"
		+ "**Exchange the code.** A form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=authorization_code`, the code, the same `redirect_uri`, the client ID and the secret, plus the `code_verifier` when PKCE is used.\n\n"
		+ _FENCE_GO_OPEN + AUTHORIZATION_CODE_HELPERS_BLOCK + _FENCE_CLOSE + "\n\n"
		+ "**Redirect URL.** A server-side application registers **one exact HTTPS URL per environment, no wildcards**; the matching rules are not documented and no claim is made about them. A plain-HTTP loopback Redirect URL `http://127.0.0.1:<port>/<path>` on a **fixed registered port** is accepted, **" + OAuth2ClientConfigurationBlocks.NSERIO_VERIFIED_LABEL + "**, and is the command-line and desktop shape.\n\n"
		+ "**On expiry.** With Allow Refresh Tokens enabled and PKCE used, the code holds a refresh token and sends `grant_type=refresh_token` in place of the code exchange: *inferred* from the standard OAuth shape, not tested; Relativity does not document a refresh token's lifetime, rotation or revocation, nor what disabling the client does to tokens already issued. **A refresh token is a secret**: hold it where the secret is held, never in a log. **Single sign-on:** sign-in happens on RelativityOne's own page, so the instance's single sign-on and two-factor rules apply unchanged (*inferred*). **The code stops collecting the password:** after this change nothing receives, stores or forwards the person's user name and password, and the code holds only tokens. Every call now runs as the person who signed in, so **the calling identity changes**. **Scope:** no scope set is prescribed for Code; `openid RelativityRequestOrigin UserInfo UserInfoAccess AuthenticationID RelativityWeb` works, **" + OAuth2ClientConfigurationBlocks.NSERIO_VERIFIED_LABEL + "**, as working and not as minimal.\n\n"
		+ "**Variant 1 — a Go web server.** **Precondition: this variant is for a Go server that signs a person in to RelativityOne. If the code is a command-line tool or a desktop process, use variant 2.**\n\n"
		+ "A server-side component creates `state` and the PKCE verifier, redirects to the authorize endpoint, receives the code at its registered HTTPS Redirect URL, exchanges it, keeps the tokens on the server and gives the browser **only its own session**; **the browser never holds anything from RelativityOne**. The two handlers use the standard library's `net/http`: `http.Redirect` replies to the request with a redirect to the authorize address.\n\n"
		+ _FENCE_GO_OPEN + AUTHORIZATION_CODE_WEB_SERVER_BLOCK + _FENCE_CLOSE + "\n\n"
		+ "**`state` is bound to the browser session**: `Login` stores it in a short-lived, `HttpOnly`, `Secure`, `SameSite=Lax` cookie as well as in the server's pending table, and `Callback` fails (400) unless the cookie is present **and** equals the `state` on the query, so a callback that arrives without the matching cookie gets no exchange. `Callback` also **handles the `error` parameter** (401, no exchange) and **an empty `code`** (400). `state` is consumed once: the entry is deleted before the exchange and the cookie is cleared, so a replayed or unknown `state` gets no exchange. `keepTokens` is yours: store the tokens against the visitor's session on the server and set a session cookie that holds **only a session identifier**. The refresh call, if used, goes to the same token endpoint with `grant_type=refresh_token` (*inferred*). **The calling identity changes** to the person who signed in.\n\n"
		+ "**Variant 2 — a command-line tool or a desktop process.** **Precondition: this variant is for a Go command-line tool or a desktop process that signs a person in to RelativityOne. If the code is a server, use variant 1.**\n\n"
		+ "The process starts a listener from the standard library bound to `127.0.0.1` only, on the **fixed port the administrator registered** (the exact URL is registered, so the port is fixed, chosen by the administrator and not at runtime), for **one request**; it **prints the authorize address** (so a machine with no browser, an SSH session or a failed launch has the manual route) and, if you want, opens the system browser yourself; reads `code` and `state` from the one request, answers with a short page, closes the listener, checks `state` and exchanges the code.\n\n"
		+ _FENCE_GO_OPEN + AUTHORIZATION_CODE_COMMAND_LINE_BLOCK + _FENCE_CLOSE + "\n\n"
		+ "**The listener is checked and bounded in the code**: it **refuses a `listenAddress` whose host is not `127.0.0.1` or `::1`** (loopback only); its handler **answers 404 to every path other than the registered redirect path** (so a stray request cannot consume the one-shot channel); the whole sign-in has **its own deadline** (`signInTimeoutSeconds`, 300 seconds, on top of the caller's context), after which it returns the context error; a callback with an `error` parameter or an empty `code` is an error, not an exchange. **Busy port:** the port is the registered one and is **not** replaced by another (a different port is a different Redirect URL, which Relativity would not match); if `net.Listen` fails because the port is taken, the function returns the error `the registered port is not available` and the tool must report it, not retry on another port. Call it with `listenAddress` `127.0.0.1:<the registered port>` and `redirectURI` `http://127.0.0.1:<the registered port>/<path>`. **Where the tokens are kept** is the program's decision: in memory for the run, or, for a refresh token, in a file whose permissions you restrict (**a distributed command-line tool carries its client secret in a form anyone who holds the distribution can extract**). Every call now runs as the person who signed in.";

	private static string AppliesTo(string detail, string versionNote = "")
	{
		return "**Applies to:** " + _GO_VERSIONS + versionNote + "; " + detail;
	}

	private static string Fenced(string block)
	{
		return _FENCE_GO_OPEN + block + _FENCE_CLOSE;
	}

	private static string TargetForm(string block, string afterTheBlock)
	{
		return _DESTINATION_QUESTION + _PARAGRAPH_SEPARATOR + Fenced(block) + _PARAGRAPH_SEPARATOR + afterTheBlock;
	}

	/// <summary>GO-001 generic source form.</summary>
	public static readonly string GO_001_SOURCE_FORM =
		AppliesTo("any program that calls `SetBasicAuth` on a request.") + _PARAGRAPH_SEPARATOR + Fenced(GO_001_SOURCE_BLOCK);

	/// <summary>GO-001 target form under token authentication, with the transport for the call that sits in a custom transport.</summary>
	public static readonly string GO_001_TARGET_FORM =
		TargetForm(GO_001_TARGET_BLOCK, "Remove the `SetBasicAuth` call. **`accessToken` comes from the token-acquisition part that follows** (`TokenSource.Token`). **If the call is in the `RoundTrip` of a custom transport**, move the header write into a transport that adds the bearer token to **a clone** of the request, because `RoundTrip` \"should not modify the request\" in the standard library's documentation, and give the client that transport:" + _PARAGRAPH_SEPARATOR + Fenced(BEARER_TRANSPORT_BLOCK));

	/// <summary>GO-001 class share of the credential provenance.</summary>
	public const string GO_001_CREDENTIAL_PROVENANCE =
		_ROUTE_BY_DESTINATION + " **If `SetBasicAuth` here authenticates the client at an OAuth 2.0 token endpoint**, this is not the Relativity migration: the endpoint's own documentation governs.";

	/// <summary>GO-001 verification note.</summary>
	public const string GO_001_VERIFICATION_NOTE =
		"**Verification note.** " + _TOKEN_LIFETIME + " Acquire again within it (the token source does). " + _CALLING_IDENTITY + " The token-acquisition part says to whom. **Other uses may not appear as findings**: a request or a header held in a struct and handed to a helper elsewhere is not read.";

	/// <summary>GO-002 generic source form.</summary>
	public static readonly string GO_002_SOURCE_FORM =
		AppliesTo("a header written by `Set`, `Add` or an index assignment.") + _PARAGRAPH_SEPARATOR + Fenced(GO_002_SOURCE_BLOCK);

	/// <summary>GO-002 target form under token authentication.</summary>
	public static readonly string GO_002_TARGET_FORM =
		TargetForm(GO_002_TARGET_BLOCK, "Delete the encoder and the pair; keep **no** variable that holds the scheme. For an index assignment (`request.Header[\"Authorization\"] = []string{…}`) use `Set`, which canonicalizes the key.");

	/// <summary>GO-002 verification note.</summary>
	public const string GO_002_VERIFICATION_NOTE =
		"**Verification note.** " + _TOKEN_LIFETIME + " " + _CALLING_IDENTITY + " A `Basic` value built in another function and handed here is read at its own site, not at this one.";

	/// <summary>GO-003 generic source form.</summary>
	public static readonly string GO_003_SOURCE_FORM =
		AppliesTo("an `http.Header` literal.") + _PARAGRAPH_SEPARATOR + Fenced(GO_003_SOURCE_BLOCK);

	/// <summary>GO-003 target form under token authentication.</summary>
	public static readonly string GO_003_TARGET_FORM =
		TargetForm(GO_003_TARGET_BLOCK, "Use the canonical key `Authorization`: a literal key is **not** canonicalized, so a lower-case key is not found by `Header.Get`.");

	/// <summary>GO-003 verification note.</summary>
	public const string GO_003_VERIFICATION_NOTE =
		"**Verification note.** " + _TOKEN_LIFETIME + " A header built once at start-up expires, so build it per request or use a transport that sets the header on a clone of each request. " + _CALLING_IDENTITY;

	/// <summary>GO-004 generic source form.</summary>
	public static readonly string GO_004_SOURCE_FORM =
		AppliesTo("an unnamed map literal that carries the header.") + _PARAGRAPH_SEPARATOR + Fenced(GO_004_SOURCE_BLOCK);

	/// <summary>GO-004 target form under token authentication.</summary>
	public static readonly string GO_004_TARGET_FORM =
		TargetForm(GO_004_TARGET_BLOCK, "Keep the key `Authorization` and write the scheme as the literal `Bearer`.");

	/// <summary>GO-004 verification note.</summary>
	public const string GO_004_VERIFICATION_NOTE =
		"**Verification note.** **The tool did not establish that this map is an HTTP header object** (the finding is Medium for that reason): confirm where the map is consumed; if it is not a header for a request to RelativityOne, this finding is informational. " + _TOKEN_LIFETIME + " " + _CALLING_IDENTITY;

	/// <summary>GO-005 generic source form.</summary>
	public static readonly string GO_005_SOURCE_FORM =
		AppliesTo("an `http` or `https` URL literal with a user and a password given to a request call.", " (`NewRequestWithContext` needs go1.13)") + _PARAGRAPH_SEPARATOR + Fenced(GO_005_SOURCE_BLOCK);

	/// <summary>GO-005 target form under token authentication.</summary>
	public static readonly string GO_005_TARGET_FORM =
		TargetForm(GO_005_TARGET_BLOCK, "Remove the user and the password from the URL. **The standard library turns a URL's userinfo into a Basic `Authorization` header only when the request has none**, so the Bearer header above is what is sent.");

	/// <summary>GO-005 class share of the credential provenance.</summary>
	public const string GO_005_CREDENTIAL_PROVENANCE =
		_ROUTE_BY_DESTINATION + " **A URL that carried a password is an exposure of its own**: it is in source, and the Go program may have logged it. " + _ROTATION_STATEMENT;

	/// <summary>GO-005 verification note.</summary>
	public const string GO_005_VERIFICATION_NOTE =
		"**Verification note.** " + _TOKEN_LIFETIME + " " + _CALLING_IDENTITY + " A URL assembled from parts is not read.";

	/// <summary>GO-006 generic source form.</summary>
	public static readonly string GO_006_SOURCE_FORM =
		AppliesTo("an encoder over a user-and-password pair near a header.", " (`AppendEncode` needs go1.22)") + _PARAGRAPH_SEPARATOR + Fenced(GO_006_SOURCE_BLOCK);

	/// <summary>GO-006 target form under token authentication.</summary>
	public static readonly string GO_006_TARGET_FORM =
		TargetForm(GO_006_TARGET_BLOCK, "Delete the encoder.");

	/// <summary>GO-006 verification note.</summary>
	public const string GO_006_VERIFICATION_NOTE =
		"**Verification note.** The tool read the encoder, **not where the encoded value goes**: confirm that it is a request to RelativityOne. **Other uses may not appear as findings**: a pair built in another statement is not read. " + _TOKEN_LIFETIME + " " + _CALLING_IDENTITY;

	/// <summary>GO-007 generic source form.</summary>
	public static readonly string GO_007_SOURCE_FORM =
		AppliesTo("a `Userinfo` built with `url.UserPassword`.") + _PARAGRAPH_SEPARATOR + Fenced(GO_007_SOURCE_BLOCK);

	/// <summary>GO-007 target form under token authentication.</summary>
	public static readonly string GO_007_TARGET_FORM =
		TargetForm(GO_007_TARGET_BLOCK, "Remove the `Userinfo` and send the token in the header.");

	/// <summary>GO-007 verification note.</summary>
	public const string GO_007_VERIFICATION_NOTE =
		"**Verification note.** The tool did not establish where the `Userinfo` goes: if the `URL` reaches a request, the client turns it into a Basic header; if it builds a database or broker address, this finding is informational. The standard library's documentation says `UserPassword` \"should only be used with legacy web sites\". " + _TOKEN_LIFETIME + " " + _CALLING_IDENTITY;

	/// <summary>GO-008 generic source form.</summary>
	public static readonly string GO_008_SOURCE_FORM =
		AppliesTo("a held URL literal that carries a user and a password.") + _PARAGRAPH_SEPARATOR + Fenced(GO_008_SOURCE_BLOCK);

	/// <summary>GO-008 target form under token authentication.</summary>
	public static readonly string GO_008_TARGET_FORM =
		TargetForm(GO_008_TARGET_BLOCK, "Remove the credentials from the literal and add the Bearer header to the request that uses the parsed address: build the request with `http.NewRequestWithContext` and set `Authorization` and `X-CSRF-Header` on it.");

	/// <summary>GO-008 class share of the credential provenance.</summary>
	public const string GO_008_CREDENTIAL_PROVENANCE =
		_ROUTE_BY_DESTINATION + " " + _ROTATION_STATEMENT;

	/// <summary>GO-008 verification note.</summary>
	public const string GO_008_VERIFICATION_NOTE =
		"**Verification note.** The tool did not establish what consumes the parsed address. " + _TOKEN_LIFETIME + " " + _CALLING_IDENTITY;

	/// <summary>GO-009 generic source form.</summary>
	public static readonly string GO_009_SOURCE_FORM =
		AppliesTo("a header whose value is an identifier that the tool resolved to a `Basic` scheme.") + _PARAGRAPH_SEPARATOR + Fenced(GO_009_SOURCE_BLOCK);

	/// <summary>GO-009 target form under token authentication.</summary>
	public static readonly string GO_009_TARGET_FORM =
		TargetForm(GO_009_TARGET_BLOCK, "Delete the declaration that builds `header` as well.");

	/// <summary>GO-009 verification note.</summary>
	public const string GO_009_VERIFICATION_NOTE =
		"**Verification note.** **The tool resolved the header's variable to a scheme of `Basic` in this file** (one declaration, before the use); a variable it cannot resolve is not read. " + _TOKEN_LIFETIME + " " + _CALLING_IDENTITY;

	/// <summary>GO-013 generic source form.</summary>
	public static readonly string GO_013_SOURCE_FORM =
		AppliesTo("a header written by `Set`, `Add` or an index assignment whose value is a call to a function declared once in the same file that builds a `Basic` value.") + _PARAGRAPH_SEPARATOR + Fenced(GO_013_SOURCE_BLOCK);

	/// <summary>GO-013 target form under token authentication.</summary>
	public static readonly string GO_013_TARGET_FORM =
		TargetForm(GO_013_TARGET_BLOCK, "Change the call as above and **change or delete the helper the call names**: the tool reports the helper at its definition, where the encoder and the `\"Basic \"` literal are, so the helper's own findings (its header write, if it has one, and its encoder) are the places to edit; keep **no** function that builds a `Basic` value.");

	/// <summary>GO-013 verification note.</summary>
	public const string GO_013_VERIFICATION_NOTE =
		"**Verification note.** **The tool resolved the called name to one function in this file that builds a `Basic` value, and did not establish where the header goes** (the finding is Medium for that reason): confirm that it is a request to RelativityOne; if it is not, this finding is informational. **Other callers may not appear as findings**: a call to a helper in another file or package, a method call and a name declared twice are not read, and a value held in a variable is not followed. " + _TOKEN_LIFETIME + " " + _CALLING_IDENTITY;

	/// <summary>The class share of the credential provenance for every class that has no text of its own.</summary>
	public const string DEFAULT_CREDENTIAL_PROVENANCE = _ROUTE_BY_DESTINATION;
}

/// <summary>
/// The `Go` analyzer's template provider and fallback texts: ten classes resolve to their entry under either flow; the three Low classes, and any identifier the analyzer does not declare, resolve to null,
/// which routes a Low finding to manual review and sends a High or Medium finding of an undeclared class to the language's fallback text.
/// </summary>
internal static class GoTemplateDeclarations
{
	private const string _PARAGRAPH_SEPARATOR = "\n\n";

	private const string _GO_001 = "GO-001";
	private const string _GO_002 = "GO-002";
	private const string _GO_003 = "GO-003";
	private const string _GO_004 = "GO-004";
	private const string _GO_005 = "GO-005";
	private const string _GO_006 = "GO-006";
	private const string _GO_007 = "GO-007";
	private const string _GO_008 = "GO-008";
	private const string _GO_009 = "GO-009";
	private const string _GO_013 = "GO-013";

	private static readonly FrozenDictionary<string, RecommendationTemplate> _clientCredentialsTemplatesByPatternClass =
		BuildTemplates(GoRecommendationTemplates.CLIENT_CREDENTIALS_TOKEN_ACQUISITION);

	private static readonly FrozenDictionary<string, RecommendationTemplate> _authorizationCodeTemplatesByPatternClass =
		BuildTemplates(GoRecommendationTemplates.AUTHORIZATION_CODE_TOKEN_ACQUISITION);

	/// <summary>The language's own text for the generic recommendation route, per flow.</summary>
	internal static FrozenDictionary<CredentialFlow, string> FallbackTextByFlow { get; } = new Dictionary<CredentialFlow, string>
	{
		[CredentialFlow.ClientCredentials] = GoRecommendationTemplates.GO_LANGUAGE_FALLBACK_TEXT,
		[CredentialFlow.AuthorizationCode] = GoRecommendationTemplates.GO_CODE_LANGUAGE_FALLBACK_TEXT
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
			[_GO_001] = Template(GoRecommendationTemplates.GO_001_SOURCE_FORM, GoRecommendationTemplates.GO_001_TARGET_FORM, GoRecommendationTemplates.GO_001_CREDENTIAL_PROVENANCE, GoRecommendationTemplates.GO_001_VERIFICATION_NOTE, tokenAcquisition),
			[_GO_002] = Template(GoRecommendationTemplates.GO_002_SOURCE_FORM, GoRecommendationTemplates.GO_002_TARGET_FORM, GoRecommendationTemplates.DEFAULT_CREDENTIAL_PROVENANCE, GoRecommendationTemplates.GO_002_VERIFICATION_NOTE, tokenAcquisition),
			[_GO_003] = Template(GoRecommendationTemplates.GO_003_SOURCE_FORM, GoRecommendationTemplates.GO_003_TARGET_FORM, GoRecommendationTemplates.DEFAULT_CREDENTIAL_PROVENANCE, GoRecommendationTemplates.GO_003_VERIFICATION_NOTE, tokenAcquisition),
			[_GO_004] = Template(GoRecommendationTemplates.GO_004_SOURCE_FORM, GoRecommendationTemplates.GO_004_TARGET_FORM, GoRecommendationTemplates.DEFAULT_CREDENTIAL_PROVENANCE, GoRecommendationTemplates.GO_004_VERIFICATION_NOTE, tokenAcquisition),
			[_GO_005] = Template(GoRecommendationTemplates.GO_005_SOURCE_FORM, GoRecommendationTemplates.GO_005_TARGET_FORM, GoRecommendationTemplates.GO_005_CREDENTIAL_PROVENANCE, GoRecommendationTemplates.GO_005_VERIFICATION_NOTE, tokenAcquisition),
			[_GO_006] = Template(GoRecommendationTemplates.GO_006_SOURCE_FORM, GoRecommendationTemplates.GO_006_TARGET_FORM, GoRecommendationTemplates.DEFAULT_CREDENTIAL_PROVENANCE, GoRecommendationTemplates.GO_006_VERIFICATION_NOTE, tokenAcquisition),
			[_GO_007] = Template(GoRecommendationTemplates.GO_007_SOURCE_FORM, GoRecommendationTemplates.GO_007_TARGET_FORM, GoRecommendationTemplates.DEFAULT_CREDENTIAL_PROVENANCE, GoRecommendationTemplates.GO_007_VERIFICATION_NOTE, tokenAcquisition),
			[_GO_008] = Template(GoRecommendationTemplates.GO_008_SOURCE_FORM, GoRecommendationTemplates.GO_008_TARGET_FORM, GoRecommendationTemplates.GO_008_CREDENTIAL_PROVENANCE, GoRecommendationTemplates.GO_008_VERIFICATION_NOTE, tokenAcquisition),
			[_GO_009] = Template(GoRecommendationTemplates.GO_009_SOURCE_FORM, GoRecommendationTemplates.GO_009_TARGET_FORM, GoRecommendationTemplates.DEFAULT_CREDENTIAL_PROVENANCE, GoRecommendationTemplates.GO_009_VERIFICATION_NOTE, tokenAcquisition),
			[_GO_013] = Template(GoRecommendationTemplates.GO_013_SOURCE_FORM, GoRecommendationTemplates.GO_013_TARGET_FORM, GoRecommendationTemplates.DEFAULT_CREDENTIAL_PROVENANCE, GoRecommendationTemplates.GO_013_VERIFICATION_NOTE, tokenAcquisition)
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
