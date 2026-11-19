# Migration Suggestions for C# (Part C of the Migration Documentation)

This is Part C of the Migration Documentation, for developers who have found Basic Authentication in their C# code with [Part A](part-a-csharp.md). You use it without running anything: for each pattern class and each OAuth 2.0 flow it shows the code to replace, the code that replaces it, how to obtain and cache the token, and the OAuth2 Client to create. Store the new secrets as [Part B](part-b-secret-management.md) describes. New here? Start with the [Migration Documentation overview](README.md).

This document has three kinds of text:

- **Judging guidance.** How to judge a match that Part A rates Low confidence.
- **Recommendations.** Each of the 17 pattern classes that produce a recommendation has two entries, one for each flow, and each flow has one generic recommendation. The OAuth2 Client settings are in the two "OAuth2 Client for ..." sections at the end of this document, and each entry links to the section of its flow.
- **Pointers.** Part A ([`part-a-csharp.md`](part-a-csharp.md)) describes each pattern and how to search for it. Secret Management (Part B of the Migration Documentation, [`part-b-secret-management.md`](part-b-secret-management.md)) says where the new secrets go.

Every example uses placeholders such as `<host>`, `<client id>` and `<client secret>`. Replace them with your own values, and keep real secrets out of source code.

The two flows are the only ones recommended here:

- **Client Credentials** is the flow when no person signs in. The application sends its client ID and client secret to the token endpoint and receives an access token. No user is involved, and every call runs as the OAuth2 Client's Context User.
- **Authorization Code** is called "Code" in RelativityOne's OAuth2 Client form. The application redirects a signed-in user to RelativityOne, receives an authorization code at a registered Redirect URL, and exchanges it for a token that carries that user's permissions.

**Choosing the flow.** The "Which flow" paragraph of each class in Part A says when a match belongs to each flow. If your RelativityOne instance already has an OAuth2 Client for this integration, use the flow of that client. The flow is fixed when an OAuth2 Client is created and cannot be changed when the client is edited ([Relativity's OAuth2 clients help page](https://help.relativity.com/RelativityOne/Content/Relativity/Authentication/OAuth2_clients.htm)), so moving an integration from one flow to the other means creating a new OAuth2 Client.

## Judging a Low-confidence match

A match is **Low** confidence when it is present in the text but unlikely to be live authenticating code. A Low match has no flow and no recommendation, and is left for manual review. **A Low level is not evidence that the code does not authenticate.** It means a person has to look. The paragraphs below say how to judge each cause and when the match still needs a migration. When it does, find the live construct it belongs to, identify its class in Part A, and follow that class's entry in [Migration suggestions by pattern class](#migration-suggestions-by-pattern-class). Whatever the cause, a literal secret inside a documentation string, a comment or a test is still a secret to remove.

### Comments

The match is inside a `//` or `/* */` comment, so no code runs. Read the comment. If it is commented-out code, nothing needs migrating today, but delete it or migrate it before anyone restores it, because a restored line brings Basic Authentication back. If it describes code that exists elsewhere, search for that code with Part A and migrate the live code. If it only illustrates a pattern with placeholders, no migration is needed.

### Documentation strings

The match is inside text that documents or explains, such as an XML documentation comment, a help message, an error message or a sample shown to the user. The string does not authenticate anything. Check that the behaviour it describes has been migrated, and update the wording afterwards so that the documentation stops describing Basic Authentication. The string itself needs no migration.

### Test fixtures and mocks

**Test code is not Low.** A match in test code is rated **Medium** at most, with the recommendation of its class. A file is test code when it sits in a folder named `test`, `tests`, `spec` or `fixtures`, or in a folder whose name ends in `.Test` or `.Tests`, when its name ends in `.Tests.cs`, `.Test.cs` or `.Spec.cs`, or when its code carries a test attribute. Judge by what the test talks to. If it feeds a mock and never reaches RelativityOne, no migration is needed, though you may want the mock to model the token flow so the test still represents the code it covers. If it is an integration test that authenticates to a real RelativityOne instance, it needs migrating with everything else, because it stops working when Basic Authentication is switched off: follow the entry of its class. A credential written literally into a test is still a secret to remove.

**A mock is Low.** A match in a mock, fake or stub file is rated Low, with no recommendation: a file name that begins or ends with `Fake`, `Mock` or `Stub`. A mock is recognized by its file name and by nothing else, so a mock that has a neutral name is rated at the level of its class, or at Medium when it also sits in test code; review it as you would a test. A file that is test code and also a mock, a sample, vendored or generated is Low. For a Low mock, judge by what it stands in for: if nothing runs it against RelativityOne, no migration is needed. A credential written literally into a mock is still a secret to remove.

### Sample and example code

The match sits in a folder named `sample`, `samples`, `example` or `examples`, in any letter case, or in a file whose name begins with `Sample` or `Example`. Those are the only names that count: a sample kept in a folder called `demo`, or in a file whose name merely contains the word, is not rated Low and keeps the level of its class, so review it as live code. Judge by who uses it. If nobody runs the sample, it needs no migration. If people copy it as a starting point, migrate it: an example that teaches Basic Authentication spreads it to new code. Use the recommendation for the class the sample matches, and prefer the flow that fits the scenario the sample shows. A credential written literally into a sample is still a secret to remove.

### Vendored and generated files

The match sits in a file produced by a tool (a generated-code header, a generated-code attribute, or a generated or designer file name) or in a folder named `vendor` or `third-party`, which holds third-party code. **Do not edit a generated file by hand:** the next generation overwrites the edit. Fix the source it is generated from (the template, the schema or the generator settings) and regenerate. For vendored code, upgrade the library to a version that supports token authentication, or replace it. A migration is needed when the file runs against RelativityOne in your application. It is not needed when the file is never called, or when it is only reference material. A credential written literally into a vendored or generated file is still a secret to remove.

### Deployment folders

The match sits in a folder named `Deployment`, in any letter case, which is treated as installation and release tooling rather than the application's own code. Read the file before you decide, because a deployment folder is also where an installer or a release script that talks to RelativityOne is kept. A migration is needed when the file is the one that runs against RelativityOne in your application or in the deployment itself. It is not needed when the file is never run, or when it only illustrates a pattern. Judge by who runs it and against which server: a deployment script that signs in to RelativityOne with a user name and a password stops working when Basic Authentication is switched off, and it is a script people copy from one environment to the next, so migrate it with everything else. A credential written literally into a deployment script is still a secret to remove.

### Conditional-compilation blocks

The match sits in a block of code that the file's own conditional-compilation symbols switch off, for example the body of an `#if` whose symbol the file never defines. In the build the file describes, that block does not run, but the file's symbols are not the whole story: a project setting or a build command can define a symbol that the file does not, and then the block is live. Check which symbols the project and its builds define. If any build includes the block, treat the match as live code: find its class in Part A and follow that class's entry. If no build includes it, treat it like commented-out code: delete it or migrate it before anyone switches it on, because a restored block brings Basic Authentication back. A credential written literally into the block is still a secret to remove.

### Colon-joined pairs without a credential name

The match is a user name and a password joined with a colon and passed to a Base64 encoder ([CS-009](#cs-009-migration)), but neither half of the pair is named like a user name or a secret: a size, a host and a port, a range or a composite key of identifiers, for example. **A Low pair is not evidence that it is harmless.** It may be genuine: a pair whose two halves are both named neutrally, a pair whose halves use a credential word outside the lists in Part A, and a pair whose only named halves end in an identifier word. Check where the encoded value goes. If it is sent in an `Authorization` header, or to anything that authenticates to RelativityOne, it is Basic Authentication under other names: follow the CS-009 entry for its flow. If it only labels, keys or formats data, no migration is needed. A literal secret inside the pair is still a secret to remove.

### CS-011 · AuthenticationHelperDefinition

<a id="cs-011-judging"></a>

A method that builds the credential or the authentication header for the project is a helper. Its definition is rated Low because the advice for a helper is not a mechanical code change. **The helper is the place to migrate, and it hides its callers:** the definition is a finding once, and its call sites are not findings because they contain no visible credential. Search for the helper's name to find every caller. If the helper builds Basic Authentication for calls to RelativityOne, migrate it once, so that it obtains and caches an access token and sends a Bearer header. Decide the flow from who supplies the credential to its callers: a person means Authorization Code, and a fixed credential means Client Credentials. Then follow the entry for the construct the helper contains, if it has one. If the helper builds a credential for another service, it needs no migration.

### CS-019 · NonHttpCredentialSink

<a id="cs-019-judging"></a>

A credential object that is handed to a sink that is not an HTTP client, such as a mail client, a file-transfer request, a directory entry or searcher, or a network proxy. It is rated Low because it is not believed to be Basic Authentication towards RelativityOne, and the class exists so that mail, file-transfer, directory and proxy credentials are not listed as migration targets. Confirm the sink. If the credential really goes to a mail server, a file server, a directory or a proxy, no migration to RelativityOne is needed, and the credential's secret handling is still governed by your own policy. If the same credential is also used to call RelativityOne, that use needs migrating too. It matches its own class, with one exception: when the credential is stored in a member that is also bound to an HTTP client anywhere in the file, the HTTP use does not match at its own place, so find it by hand.

## Migration suggestions by pattern class

Each of the 17 pattern classes that produce a recommendation has two entries, one for each flow. Choose the entry for the flow that fits your integration; the "Which flow" paragraph of the class in Part A says when a match belongs to each. Every entry contains the code being replaced, the migrated code, a description of token acquisition, caching and expiry handling, the flow the entry is for, the OAuth2 Client to create or confirm (a link to its settings in [OAuth2 Client for Client Credentials](#oauth2-client-for-client-credentials) or [OAuth2 Client for Authorization Code](#oauth2-client-for-authorization-code)), and the pointer to Secret Management (Part B of the Migration Documentation). Every Authorization Code entry adds a description of refresh-token handling, of PKCE, and of a server-side variant and a desktop variant.

**An entry carries the code that obtains the token and the change at the point of use.** For Client Credentials, a class requests the token, keeps it and renews it before it expires; for Authorization Code, the code is exchanged for a token and, for a desktop application, a local receiver takes the redirect. How to renew a token with a refresh token is described in words.

**Part 1, part 2 and part 3.** The text of an entry refers to its own pieces by number. Each piece has a label in the entry:

- **Part 1** is the change at the point of use: the text under the label *Under token authentication*, with its migrated code.
- **Part 2** is the token acquisition: the part of the text under the label *Credentials* that follows the paragraph *Where the credentials come from*. It describes the request to the token endpoint, the caching of the token and what to do when it expires. A Client Credentials entry starts it with *Where the token comes from*, and an Authorization Code entry with *Common to both variants*.
- **Part 3** is the OAuth2 Client settings, under the label *OAuth2 Client* at the end of the entry.

The labels *Source form* (the construct being replaced) and *Verification* (what to check before you deploy) are not numbered.

The two Low classes, CS-011 and CS-019, have no entry because they produce no recommendation. They are covered under [Judging a Low-confidence match](#judging-a-low-confidence-match).

### CS-001 · SdkUserNamePasswordCredentials

<a id="cs-001-migration"></a>

#### Client Credentials

<!-- generated:begin CS-001 client-credentials -->
**Flow.** This entry is for Client Credentials. The "Which flow" paragraph of CS-001 in [Part A](part-a-csharp.md#cs-001) says when a match belongs to this flow.

Source form:
```csharp
Credentials credentials = new UsernamePasswordCredentials(userName, password);
ServiceFactorySettings settings = new ServiceFactorySettings(restEndpointUri, credentials);
```

Under token authentication: **First, the destination:** if this Relativity instance is not RelativityOne — including a Relativity Server instance, whose own documentation governs — no migration to RelativityOne applies and the rest of this entry does not apply.

```csharp
ServiceFactorySettings settings = new ServiceFactorySettings(
	restEndpointUri, new BearerTokenCredentials(accessToken));
```

The REST URI is unchanged: `<scheme>://<machine name>/Relativity.Rest/api`.

Credentials: **Where the credentials come from.** The SDK type establishes that the destination is Relativity, not that it is RelativityOne; if it is not RelativityOne — including a Relativity Server instance, whose own documentation governs — the migration is that service's own token mechanism, and no migration to RelativityOne applies. If it is RelativityOne, the token comes from the token-acquisition part that follows.

**Where the token comes from.** Create an OAuth2 Client with Flow Grant Type set to Client Credentials (part 3 lists its settings), then send a form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=client_credentials`, `client_id`, `client_secret` and a `scope`. The token endpoint is **documented** for this grant. The token is then sent as `Authorization: Bearer <token>`, with `X-CSRF-Header: -` on REST and Kepler calls — the header is documented as required, and a Kepler call without it returns 404.

**.NET** (`HttpClient` and `System.Text.Json`). The class requests the token with the client ID and the client secret, caches it, requests a new one 60 seconds before it expires, and, when a call is rejected for authentication, requests a new one and sends the call once more. Build one instance for the life of the process and send every call through `SendAsync`; the request is created by the delegate on each attempt, because a request message cannot be sent twice. The client ID and the client secret come from the process environment or a secret store, never from source. Where the code uses the Relativity SDK, `GetAccessTokenAsync(false)` gives the token for `BearerTokenCredentials` each time a `ServiceFactory` is built.

```csharp
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

public sealed class AccessTokenProvider : IDisposable
{
	private const string _TOKEN_PATH = "/Relativity/Identity/connect/token";
	private const string _BEARER_SCHEME = "Bearer";
	private const string _CSRF_HEADER_NAME = "X-CSRF-Header";
	private const string _CSRF_HEADER_VALUE = "-";
	private static readonly TimeSpan _renewalMargin = TimeSpan.FromSeconds(60);

	private readonly HttpClient _client = new HttpClient();
	private readonly SemaphoreSlim _gate = new SemaphoreSlim(1, 1);
	private readonly string _host;
	private readonly string _clientId;
	private readonly string _clientSecret;
	private readonly string _scope;
	private string _accessToken;
	private DateTimeOffset _expiresAt;

	public AccessTokenProvider(string host, string clientId, string clientSecret, string scope)
	{
		_host = host;
		_clientId = clientId;
		_clientSecret = clientSecret;
		_scope = scope;
	}

	// Cached token; renewed shortly before it expires, or on demand after a rejected call.
	public async Task<string> GetAccessTokenAsync(bool forceRenewal)
	{
		await _gate.WaitAsync();
		try
		{
			if (forceRenewal || _accessToken == null || DateTimeOffset.UtcNow >= _expiresAt - _renewalMargin)
			{
				await AcquireAccessTokenAsync();
			}

			return _accessToken;
		}
		finally
		{
			_gate.Release();
		}
	}

	// Sends the request with the bearer token; when the call is rejected for authentication, renews the token and sends it once more.
	public async Task<HttpResponseMessage> SendAsync(Func<HttpRequestMessage> createRequest)
	{
		HttpResponseMessage response = await SendOnceAsync(createRequest, false);
		if (response.StatusCode == HttpStatusCode.Unauthorized)
		{
			response.Dispose();
			response = await SendOnceAsync(createRequest, true);
		}

		return response;
	}

	public void Dispose()
	{
		_client.Dispose();
		_gate.Dispose();
	}

	private async Task AcquireAccessTokenAsync()
	{
		using (FormUrlEncodedContent form = new FormUrlEncodedContent(new Dictionary<string, string>
		{
			{ "grant_type", "client_credentials" },
			{ "client_id", _clientId },
			{ "client_secret", _clientSecret },
			{ "scope", _scope }
		}))
		using (HttpResponseMessage response = await _client.PostAsync($"{_host}{_TOKEN_PATH}", form))
		{
			response.EnsureSuccessStatusCode();
			using (JsonDocument document = JsonDocument.Parse(await response.Content.ReadAsStringAsync()))
			{
				_accessToken = document.RootElement.GetProperty("access_token").GetString();
				_expiresAt = DateTimeOffset.UtcNow.AddSeconds(document.RootElement.GetProperty("expires_in").GetInt32());
			}
		}
	}

	private async Task<HttpResponseMessage> SendOnceAsync(Func<HttpRequestMessage> createRequest, bool forceRenewal)
	{
		string token = await GetAccessTokenAsync(forceRenewal);
		HttpResponseMessage response;
		using (HttpRequestMessage request = createRequest())
		{
			request.Headers.Authorization = new AuthenticationHeaderValue(_BEARER_SCHEME, token);
			request.Headers.Add(_CSRF_HEADER_NAME, _CSRF_HEADER_VALUE);
			response = await _client.SendAsync(request);
		}

		return response;
	}
}
```

**.NET Framework 4.7.2**: the same class with the serializer the runtime ships (`System.Web.Extensions`, referenced together with `System.Net.Http`), and C# 7.3 syntax:

```csharp
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

public sealed class AccessTokenProvider : IDisposable
{
	private const string _TOKEN_PATH = "/Relativity/Identity/connect/token";
	private const string _BEARER_SCHEME = "Bearer";
	private const string _CSRF_HEADER_NAME = "X-CSRF-Header";
	private const string _CSRF_HEADER_VALUE = "-";
	private static readonly TimeSpan _renewalMargin = TimeSpan.FromSeconds(60);

	private readonly HttpClient _client = new HttpClient();
	private readonly SemaphoreSlim _gate = new SemaphoreSlim(1, 1);
	private readonly string _host;
	private readonly string _clientId;
	private readonly string _clientSecret;
	private readonly string _scope;
	private string _accessToken;
	private DateTimeOffset _expiresAt;

	public AccessTokenProvider(string host, string clientId, string clientSecret, string scope)
	{
		_host = host;
		_clientId = clientId;
		_clientSecret = clientSecret;
		_scope = scope;
	}

	// Cached token; renewed shortly before it expires, or on demand after a rejected call.
	public async Task<string> GetAccessTokenAsync(bool forceRenewal)
	{
		await _gate.WaitAsync();
		try
		{
			if (forceRenewal || _accessToken == null || DateTimeOffset.UtcNow >= _expiresAt - _renewalMargin)
			{
				await AcquireAccessTokenAsync();
			}

			return _accessToken;
		}
		finally
		{
			_gate.Release();
		}
	}

	// Sends the request with the bearer token; when the call is rejected for authentication, renews the token and sends it once more.
	public async Task<HttpResponseMessage> SendAsync(Func<HttpRequestMessage> createRequest)
	{
		HttpResponseMessage response = await SendOnceAsync(createRequest, false);
		if (response.StatusCode == HttpStatusCode.Unauthorized)
		{
			response.Dispose();
			response = await SendOnceAsync(createRequest, true);
		}

		return response;
	}

	public void Dispose()
	{
		_client.Dispose();
		_gate.Dispose();
	}

	private async Task AcquireAccessTokenAsync()
	{
		using (FormUrlEncodedContent form = new FormUrlEncodedContent(new Dictionary<string, string>
		{
			{ "grant_type", "client_credentials" },
			{ "client_id", _clientId },
			{ "client_secret", _clientSecret },
			{ "scope", _scope }
		}))
		using (HttpResponseMessage response = await _client.PostAsync($"{_host}{_TOKEN_PATH}", form))
		{
			response.EnsureSuccessStatusCode();
			string body = await response.Content.ReadAsStringAsync();
			Dictionary<string, object> fields = new JavaScriptSerializer().Deserialize<Dictionary<string, object>>(body);
			_accessToken = fields["access_token"].ToString();
			_expiresAt = DateTimeOffset.UtcNow.AddSeconds(Convert.ToInt32(fields["expires_in"]));
		}
	}

	private async Task<HttpResponseMessage> SendOnceAsync(Func<HttpRequestMessage> createRequest, bool forceRenewal)
	{
		string token = await GetAccessTokenAsync(forceRenewal);
		HttpResponseMessage response;
		using (HttpRequestMessage request = createRequest())
		{
			request.Headers.Authorization = new AuthenticationHeaderValue(_BEARER_SCHEME, token);
			request.Headers.Add(_CSRF_HEADER_NAME, _CSRF_HEADER_VALUE);
			response = await _client.SendAsync(request);
		}

		return response;
	}
}
```

**The scope depends on what the integration needs, and no single scope is required.** Relativity's documentation shows two values for this grant: `SystemUserInfo` on the REST authentication page, the Usage Reports page and in the official Transfer SDK sample, and `UserInfoAccess` in the Kepler .NET samples. No page says which is correct, whether both work, or what either grants, and the OAuth2 Client's `Scopes` property is documented as one that cannot be set. Use the value the API you call documents, and confirm it.

**Cache the token, and re-acquire it on expiry — do not refresh.** Keep the token for its lifetime (part 3 recommends 60 minutes, Relativity's own wording) and request a new one with the client secret shortly before it expires (the JavaScript, TypeScript and Python examples use 60 seconds), and once when a call is rejected for authentication. Client Credentials does not offer refresh tokens: **verified by NSerio against a live instance; not documented by Relativity** — the Allow Refresh Tokens control does not appear for this grant in the RelativityOne user interface. Relativity's own tables and text on refresh tokens conflict with each other, so re-acquisition with the secret is the advice that holds whichever way they are read.

**Long-running processes that use the Relativity SDK.** A `ServiceFactory` built once from a bearer token keeps that token after it expires. Rebuild the `ServiceFactory` and its proxies with a fresh token before expiry. This is the only advice consistent with the documentation: the package `Relativity.Kepler.Client.SDK` (documented) exposes a settable `Token` and an `HttpMessageHandler` overload but no token-provider overload, and no page documents renewing a token behind a factory.

**The client secret.** It is displayed once, at creation or reset. Store it as Secret Management (Part B of the Migration Documentation) describes; never in source, never in tracked configuration. A reset invalidates the previous secret immediately, so plan the cut-over before resetting it.

**Before deploying, confirm the calling identity.** Every call now runs as the OAuth2 Client's **Context User** — not as the user in the replaced credential, and for an import or a job as much as for a request. If that user has different workspace permissions or item-level security, the code returns fewer rows or `Unauthorized` in production with no change to its logic, and Relativity's audit attributes every call to that one account. Confirm the Context User's permissions match those of the previous identity. Relativity's REST API authentication page describes the Context User as usually a member of the Relativity Administrators group; the OAuth2 Client block (part 3) gives NSerio's recommendation, which differs.

**When the integration acts for a signed-in user.** Client Credentials is one service identity by construction. An integration that acts on behalf of a signed-in user is the Authorization Code case, and moving to it means a new OAuth2 Client, because the flow is fixed when a client is created. Which of the two flows applies depends on who supplies the credential at run time.

Verification: **Verification note — two things, and the first can break working code.**

**A token has a finite lifetime**, set per client in minutes, and this construct holds it. A `ServiceFactory` built once from `BearerTokenCredentials(accessToken)` and kept for the life of a process keeps that token after it expires, and the calls made through it then fail authentication. Rebuild the settings and the factory with a current token within the token's lifetime; the previous credential never expired, so nothing in the existing code does this.

**Confirm the SDK version in use provides `BearerTokenCredentials`.** Where it does not, this is an SDK upgrade before it is a code change.

OAuth2 Client: See [OAuth2 Client for Client Credentials](#oauth2-client-for-client-credentials).
<!-- generated:end CS-001 client-credentials -->

#### Authorization Code

<!-- generated:begin CS-001 authorization-code -->
**Flow.** This entry is for Authorization Code. The "Which flow" paragraph of CS-001 in [Part A](part-a-csharp.md#cs-001) says when a match belongs to this flow.

Source form:
```csharp
Credentials credentials = new UsernamePasswordCredentials(userName, password);
ServiceFactorySettings settings = new ServiceFactorySettings(restEndpointUri, credentials);
```

Under token authentication: **First, the destination:** if this Relativity instance is not RelativityOne — including a Relativity Server instance, whose own documentation governs — no migration to RelativityOne applies and the rest of this entry does not apply.

```csharp
ServiceFactorySettings settings = new ServiceFactorySettings(
	restEndpointUri, new BearerTokenCredentials(accessToken));
```

The REST URI is unchanged: `<scheme>://<machine name>/Relativity.Rest/api`.

Credentials: **Where the credentials come from.** The SDK type establishes that the destination is Relativity, not that it is RelativityOne; if it is not RelativityOne — including a Relativity Server instance, whose own documentation governs — the migration is that service's own token mechanism, and no migration to RelativityOne applies. If it is RelativityOne, the token comes from the token-acquisition part that follows.

**Common to both variants**

**Sign in.** Replace the code that collected the user's credentials with a redirect: send the user's browser to the authorize endpoint `<host>/Relativity/Identity/connect/authorize`, which leads to the RelativityOne login page. The authorize endpoint is **verified by NSerio against a live instance; not documented by Relativity**. The request carries the standard authorization-code parameters — `client_id`, `response_type=code`, the registered `redirect_uri`, a `scope`, and a `state` value the application checks on return; these are standard OAuth, and Relativity documents none of them. No discovery document is relied on.

**Receive the code.** The browser returns to the Redirect URL registered on the OAuth2 Client, with the authorization code and the `state` value. Check the `state` before using the code. The Redirect URL must be exactly the one registered (the variants say how).

**Exchange the code.** Send a form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=authorization_code`, the `code`, the same `redirect_uri`, the `client_id` and the `client_secret`, plus the `code_verifier` when PKCE is used. The code is exchanged at that endpoint, which Relativity documents for the Client Credentials grant only; the exchange is **verified by NSerio against a live instance; not documented by Relativity**. Send the `code_verifier` only when the authorize request carried a `code_challenge`; otherwise leave it out.

**.NET** (`HttpClient` and `System.Text.Json`):

```csharp
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

public sealed class AuthorizationCodeRequest
{
	public string Host { get; set; }
	public string AuthorizationCode { get; set; }
	public string RedirectUri { get; set; }
	public string ClientId { get; set; }
	public string ClientSecret { get; set; }

	// The PKCE verifier; null when no code_challenge was sent on the authorize request.
	public string CodeVerifier { get; set; }
}

public static class AuthorizationCodeExchange
{
	private const string _TOKEN_PATH = "/Relativity/Identity/connect/token";

	public static async Task<string> ExchangeAuthorizationCodeAsync(HttpClient client, AuthorizationCodeRequest request)
	{
		Dictionary<string, string> fields = new Dictionary<string, string>
		{
			{ "grant_type", "authorization_code" },
			{ "code", request.AuthorizationCode },
			{ "redirect_uri", request.RedirectUri },
			{ "client_id", request.ClientId },
			{ "client_secret", request.ClientSecret }
		};
		if (!string.IsNullOrEmpty(request.CodeVerifier))
		{
			fields["code_verifier"] = request.CodeVerifier;
		}

		string accessToken;
		using (FormUrlEncodedContent form = new FormUrlEncodedContent(fields))
		using (HttpResponseMessage response = await client.PostAsync($"{request.Host}{_TOKEN_PATH}", form))
		{
			response.EnsureSuccessStatusCode();
			using (JsonDocument document = JsonDocument.Parse(await response.Content.ReadAsStringAsync()))
			{
				accessToken = document.RootElement.GetProperty("access_token").GetString();
			}
		}

		return accessToken;
	}
}
```

**.NET Framework 4.7.2** (`System.Web.Extensions` and C# 7.3 syntax):

```csharp
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

public sealed class AuthorizationCodeRequest
{
	public string Host { get; set; }
	public string AuthorizationCode { get; set; }
	public string RedirectUri { get; set; }
	public string ClientId { get; set; }
	public string ClientSecret { get; set; }

	// The PKCE verifier; null when no code_challenge was sent on the authorize request.
	public string CodeVerifier { get; set; }
}

public static class AuthorizationCodeExchange
{
	private const string _TOKEN_PATH = "/Relativity/Identity/connect/token";

	public static async Task<string> ExchangeAuthorizationCodeAsync(HttpClient client, AuthorizationCodeRequest request)
	{
		Dictionary<string, string> fields = new Dictionary<string, string>
		{
			{ "grant_type", "authorization_code" },
			{ "code", request.AuthorizationCode },
			{ "redirect_uri", request.RedirectUri },
			{ "client_id", request.ClientId },
			{ "client_secret", request.ClientSecret }
		};
		if (!string.IsNullOrEmpty(request.CodeVerifier))
		{
			fields["code_verifier"] = request.CodeVerifier;
		}

		string accessToken;
		using (FormUrlEncodedContent form = new FormUrlEncodedContent(fields))
		using (HttpResponseMessage response = await client.PostAsync($"{request.Host}{_TOKEN_PATH}", form))
		{
			response.EnsureSuccessStatusCode();
			string body = await response.Content.ReadAsStringAsync();
			accessToken = new JavaScriptSerializer().Deserialize<Dictionary<string, object>>(body)["access_token"].ToString();
		}

		return accessToken;
	}
}
```

**Use the token.** Send `Authorization: Bearer <access token>` — the change at the use site in part 1 — with `X-CSRF-Header: -` on REST and Kepler calls.

**On expiry.** With Allow Refresh Tokens enabled on the OAuth2 Client and PKCE used, the application holds a refresh token and, when the access token expires, sends a `grant_type=refresh_token` request with it and the client credentials in place of the code exchange — *inferred* from the standard OAuth shape, not tested. The exchange example above reads only the `access_token` of the response; to renew with a refresh token, read the `refresh_token` from the same response as well and keep it as a secret, and otherwise the application sends the user through the sign-in again when the access token expires. Relativity does not document a refresh token's lifetime, whether it rotates, how it is revoked, or whether a revocation endpoint exists, nor what disabling or deleting the client does to tokens already issued. A refresh token is a secret: store it as Secret Management (Part B of the Migration Documentation) describes, and on compromise disable the client and replace it. Enable refresh tokens only if the application must keep a session beyond one token lifetime without asking the user to sign in again.

**PKCE.** PKCE is required by Relativity's own documentation to obtain a refresh token, and not otherwise: a Code request that does not ask for a refresh token works without it, **verified by NSerio against a live instance; not documented by Relativity**. It is never a blanket requirement. NSerio recommends PKCE with the `S256` method in both variants (NSerio recommendation), and does not claim that Relativity accepts any particular method, which it does not document.

**Scope.** No scope set is prescribed for Code. The scope below works — `openid RelativityRequestOrigin UserInfo UserInfoAccess AuthenticationID RelativityWeb` — **verified by NSerio against a live instance; not documented by Relativity**, verified as working and not as minimal. `state` is sent as standard OAuth practice and `nonce` is not used; Relativity does not document whether it enforces either.

**The application stops collecting the password.** After this change the application never receives, stores or forwards the user's username and password: the user signs in on RelativityOne's own page, and the application holds only tokens. Remove the credential prompt, the form field and any storage of the password that the replaced code needed.

**Single sign-on.** Sign-in happens on RelativityOne's own login page, so the instance's single sign-on and two-factor rules apply unchanged — *inferred* from where the sign-in happens, and not documented for clients that customers create.

**The client secret is required.** Code does not work without a client secret: tested and rejected, **verified by NSerio against a live instance; not documented by Relativity**, which confirms Relativity's own statement that the grant requires one. It is displayed once and is stored as Secret Management (Part B of the Migration Documentation) describes.

**Server-side variant.** For a web application whose code runs on a server:

- **Redirect.** The application receives the code at one exact HTTPS URL per environment, a route of the application, with no wildcards (NSerio recommendation; Relativity's matching rules are not documented). The Relativity administrator registers the exact URL on the OAuth2 Client.
- **Secret and tokens.** The client secret stays on the server and never reaches the browser. The user's tokens are held server-side, keyed to the session, and never in the browser.
- **Exchange.** The server performs the code exchange itself.

**Desktop variant.** For a desktop or command-line application run by a user on their own machine:

- **The system browser.** Open the authorize URL in the user's default browser and not in a web view the application hosts, so the application never sees the password (NSerio recommendation).
- **A loopback redirect on a fixed port.** Listen on the loopback address only, on the registered port, for one request. The Redirect URL is `http://127.0.0.1:<port>/<path>/`, ending with a slash because the listener below registers it as its prefix: a plain-HTTP loopback URL is accepted, **verified by NSerio against a live instance; not documented by Relativity**. The Relativity administrator registers that exact URL; because it is registered in advance, the port is fixed and not chosen at runtime.

```csharp
using System;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Net;
using System.Threading.Tasks;

public static class LoopbackAuthorizationReceiver
{
	private const string _CODE_PARAMETER = "code";
	private const string _STATE_PARAMETER = "state";
	private const string _ERROR_PARAMETER = "error";
	private const string _ERROR_DESCRIPTION_PARAMETER = "error_description";
	private const string _STATE_MISMATCH_MESSAGE = "The returned state is not the one that was sent; the response is rejected.";
	private const string _MISSING_CODE_MESSAGE = "The response carries no authorization code.";

	// Waits for the one request the browser sends to the registered loopback Redirect URL, and returns its code
	// only when its state is the one that was sent on the authorize request and the response carries no error.
	// redirectUrl is the exact URL registered on the OAuth2 Client, for example http://127.0.0.1:<port>/<path>/ :
	// it ends with a slash, as a listener prefix must.
	public static async Task<string> ReceiveAuthorizationCodeAsync(string authorizeUrl, string redirectUrl, string expectedState)
	{
		string authorizationCode;
		using (HttpListener listener = new HttpListener())
		{
			listener.Prefixes.Add(redirectUrl);
			listener.Start();
			Process.Start(new ProcessStartInfo(authorizeUrl) { UseShellExecute = true })?.Dispose();
			HttpListenerContext context = await listener.GetContextAsync();
			NameValueCollection query = context.Request.QueryString;
			context.Response.Close();
			if (query[_STATE_PARAMETER] != expectedState)
			{
				throw new InvalidOperationException(_STATE_MISMATCH_MESSAGE);
			}

			if (!string.IsNullOrEmpty(query[_ERROR_PARAMETER]))
			{
				throw new InvalidOperationException($"The authorization server returned an error: {query[_ERROR_PARAMETER]} {query[_ERROR_DESCRIPTION_PARAMETER]}".TrimEnd());
			}

			authorizationCode = query[_CODE_PARAMETER];
			if (string.IsNullOrEmpty(authorizationCode))
			{
				throw new InvalidOperationException(_MISSING_CODE_MESSAGE);
			}
		}

		return authorizationCode;
	}
}
```

- **The secret is extractable.** Code requires a client secret (above), so a distributed desktop application holds one, and **a secret shipped inside a distributed binary can be extracted by anyone who holds the binary**. It protects nothing against the person running the application.
- **Compensating guidance** (NSerio recommendation, subject to your own security policy): set Trusted IPs where the users' outbound addresses are known and stable — they restrict where a token can be *requested*, and are checked when a token is issued against the client's ID and secret; use the recommended short Access Token Lifetime of 60 minutes; enable refresh tokens only when needed; and create one OAuth2 Client per distribution, so a leaked secret can be disabled without affecting the others.
- **A documentation gap.** Relativity documents no recommendation for a distributed desktop application; this design is what NSerio verified works.

Verification: **Verification note — two things, and the first can break working code.**

**A token has a finite lifetime**, set per client in minutes, and this construct holds it. A `ServiceFactory` built once from `BearerTokenCredentials(accessToken)` and kept for the life of a process keeps that token after it expires, and the calls made through it then fail authentication. Rebuild the settings and the factory with a current token within the token's lifetime; the previous credential never expired, so nothing in the existing code does this.

**Confirm the SDK version in use provides `BearerTokenCredentials`.** Where it does not, this is an SDK upgrade before it is a code change.

OAuth2 Client: See [OAuth2 Client for Authorization Code](#oauth2-client-for-authorization-code).
<!-- generated:end CS-001 authorization-code -->

### CS-002 · SdkLegacyRsapiCredentials

<a id="cs-002-migration"></a>

#### Client Credentials

<!-- generated:begin CS-002 client-credentials -->
**Flow.** This entry is for Client Credentials. The "Which flow" paragraph of CS-002 in [Part A](part-a-csharp.md#cs-002) says when a match belongs to this flow.

Source form:
```csharp
var client = new RSAPIClient(rsapiUri, new UsernamePasswordCredentials(userName, password));
```

Under token authentication: **First, the destination:** if this Relativity instance is not RelativityOne — including a Relativity Server instance, whose own documentation governs — no migration to RelativityOne applies and the rest of this entry does not apply.

**Two steps, not one, and in this order.** First, **move the call to Object Manager**: the RSAPI client is retired, so swapping the credential inside the RSAPI construction migrates the scheme of a call that should not survive. Rewrite the operation as the equivalent Object Manager call, reached through a service factory. Second, apply the token credential to that rewritten call:

```csharp
ServiceFactorySettings settings = new ServiceFactorySettings(
	restEndpointUri, new BearerTokenCredentials(accessToken));
```

The REST URI is `<scheme>://<machine name>/Relativity.Rest/api`, which is not the RSAPI endpoint the source form addressed.

Credentials: **Where the credentials come from.** The SDK type establishes that the destination is Relativity, not that it is RelativityOne; if it is not RelativityOne — including a Relativity Server instance, whose own documentation governs — the migration is that service's own token mechanism, and no migration to RelativityOne applies. If it is RelativityOne, the token comes from the token-acquisition part that follows.

**Where the token comes from.** Create an OAuth2 Client with Flow Grant Type set to Client Credentials (part 3 lists its settings), then send a form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=client_credentials`, `client_id`, `client_secret` and a `scope`. The token endpoint is **documented** for this grant. The token is then sent as `Authorization: Bearer <token>`, with `X-CSRF-Header: -` on REST and Kepler calls — the header is documented as required, and a Kepler call without it returns 404.

**.NET** (`HttpClient` and `System.Text.Json`). The class requests the token with the client ID and the client secret, caches it, requests a new one 60 seconds before it expires, and, when a call is rejected for authentication, requests a new one and sends the call once more. Build one instance for the life of the process and send every call through `SendAsync`; the request is created by the delegate on each attempt, because a request message cannot be sent twice. The client ID and the client secret come from the process environment or a secret store, never from source. Where the code uses the Relativity SDK, `GetAccessTokenAsync(false)` gives the token for `BearerTokenCredentials` each time a `ServiceFactory` is built.

```csharp
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

public sealed class AccessTokenProvider : IDisposable
{
	private const string _TOKEN_PATH = "/Relativity/Identity/connect/token";
	private const string _BEARER_SCHEME = "Bearer";
	private const string _CSRF_HEADER_NAME = "X-CSRF-Header";
	private const string _CSRF_HEADER_VALUE = "-";
	private static readonly TimeSpan _renewalMargin = TimeSpan.FromSeconds(60);

	private readonly HttpClient _client = new HttpClient();
	private readonly SemaphoreSlim _gate = new SemaphoreSlim(1, 1);
	private readonly string _host;
	private readonly string _clientId;
	private readonly string _clientSecret;
	private readonly string _scope;
	private string _accessToken;
	private DateTimeOffset _expiresAt;

	public AccessTokenProvider(string host, string clientId, string clientSecret, string scope)
	{
		_host = host;
		_clientId = clientId;
		_clientSecret = clientSecret;
		_scope = scope;
	}

	// Cached token; renewed shortly before it expires, or on demand after a rejected call.
	public async Task<string> GetAccessTokenAsync(bool forceRenewal)
	{
		await _gate.WaitAsync();
		try
		{
			if (forceRenewal || _accessToken == null || DateTimeOffset.UtcNow >= _expiresAt - _renewalMargin)
			{
				await AcquireAccessTokenAsync();
			}

			return _accessToken;
		}
		finally
		{
			_gate.Release();
		}
	}

	// Sends the request with the bearer token; when the call is rejected for authentication, renews the token and sends it once more.
	public async Task<HttpResponseMessage> SendAsync(Func<HttpRequestMessage> createRequest)
	{
		HttpResponseMessage response = await SendOnceAsync(createRequest, false);
		if (response.StatusCode == HttpStatusCode.Unauthorized)
		{
			response.Dispose();
			response = await SendOnceAsync(createRequest, true);
		}

		return response;
	}

	public void Dispose()
	{
		_client.Dispose();
		_gate.Dispose();
	}

	private async Task AcquireAccessTokenAsync()
	{
		using (FormUrlEncodedContent form = new FormUrlEncodedContent(new Dictionary<string, string>
		{
			{ "grant_type", "client_credentials" },
			{ "client_id", _clientId },
			{ "client_secret", _clientSecret },
			{ "scope", _scope }
		}))
		using (HttpResponseMessage response = await _client.PostAsync($"{_host}{_TOKEN_PATH}", form))
		{
			response.EnsureSuccessStatusCode();
			using (JsonDocument document = JsonDocument.Parse(await response.Content.ReadAsStringAsync()))
			{
				_accessToken = document.RootElement.GetProperty("access_token").GetString();
				_expiresAt = DateTimeOffset.UtcNow.AddSeconds(document.RootElement.GetProperty("expires_in").GetInt32());
			}
		}
	}

	private async Task<HttpResponseMessage> SendOnceAsync(Func<HttpRequestMessage> createRequest, bool forceRenewal)
	{
		string token = await GetAccessTokenAsync(forceRenewal);
		HttpResponseMessage response;
		using (HttpRequestMessage request = createRequest())
		{
			request.Headers.Authorization = new AuthenticationHeaderValue(_BEARER_SCHEME, token);
			request.Headers.Add(_CSRF_HEADER_NAME, _CSRF_HEADER_VALUE);
			response = await _client.SendAsync(request);
		}

		return response;
	}
}
```

**.NET Framework 4.7.2**: the same class with the serializer the runtime ships (`System.Web.Extensions`, referenced together with `System.Net.Http`), and C# 7.3 syntax:

```csharp
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

public sealed class AccessTokenProvider : IDisposable
{
	private const string _TOKEN_PATH = "/Relativity/Identity/connect/token";
	private const string _BEARER_SCHEME = "Bearer";
	private const string _CSRF_HEADER_NAME = "X-CSRF-Header";
	private const string _CSRF_HEADER_VALUE = "-";
	private static readonly TimeSpan _renewalMargin = TimeSpan.FromSeconds(60);

	private readonly HttpClient _client = new HttpClient();
	private readonly SemaphoreSlim _gate = new SemaphoreSlim(1, 1);
	private readonly string _host;
	private readonly string _clientId;
	private readonly string _clientSecret;
	private readonly string _scope;
	private string _accessToken;
	private DateTimeOffset _expiresAt;

	public AccessTokenProvider(string host, string clientId, string clientSecret, string scope)
	{
		_host = host;
		_clientId = clientId;
		_clientSecret = clientSecret;
		_scope = scope;
	}

	// Cached token; renewed shortly before it expires, or on demand after a rejected call.
	public async Task<string> GetAccessTokenAsync(bool forceRenewal)
	{
		await _gate.WaitAsync();
		try
		{
			if (forceRenewal || _accessToken == null || DateTimeOffset.UtcNow >= _expiresAt - _renewalMargin)
			{
				await AcquireAccessTokenAsync();
			}

			return _accessToken;
		}
		finally
		{
			_gate.Release();
		}
	}

	// Sends the request with the bearer token; when the call is rejected for authentication, renews the token and sends it once more.
	public async Task<HttpResponseMessage> SendAsync(Func<HttpRequestMessage> createRequest)
	{
		HttpResponseMessage response = await SendOnceAsync(createRequest, false);
		if (response.StatusCode == HttpStatusCode.Unauthorized)
		{
			response.Dispose();
			response = await SendOnceAsync(createRequest, true);
		}

		return response;
	}

	public void Dispose()
	{
		_client.Dispose();
		_gate.Dispose();
	}

	private async Task AcquireAccessTokenAsync()
	{
		using (FormUrlEncodedContent form = new FormUrlEncodedContent(new Dictionary<string, string>
		{
			{ "grant_type", "client_credentials" },
			{ "client_id", _clientId },
			{ "client_secret", _clientSecret },
			{ "scope", _scope }
		}))
		using (HttpResponseMessage response = await _client.PostAsync($"{_host}{_TOKEN_PATH}", form))
		{
			response.EnsureSuccessStatusCode();
			string body = await response.Content.ReadAsStringAsync();
			Dictionary<string, object> fields = new JavaScriptSerializer().Deserialize<Dictionary<string, object>>(body);
			_accessToken = fields["access_token"].ToString();
			_expiresAt = DateTimeOffset.UtcNow.AddSeconds(Convert.ToInt32(fields["expires_in"]));
		}
	}

	private async Task<HttpResponseMessage> SendOnceAsync(Func<HttpRequestMessage> createRequest, bool forceRenewal)
	{
		string token = await GetAccessTokenAsync(forceRenewal);
		HttpResponseMessage response;
		using (HttpRequestMessage request = createRequest())
		{
			request.Headers.Authorization = new AuthenticationHeaderValue(_BEARER_SCHEME, token);
			request.Headers.Add(_CSRF_HEADER_NAME, _CSRF_HEADER_VALUE);
			response = await _client.SendAsync(request);
		}

		return response;
	}
}
```

**The scope depends on what the integration needs, and no single scope is required.** Relativity's documentation shows two values for this grant: `SystemUserInfo` on the REST authentication page, the Usage Reports page and in the official Transfer SDK sample, and `UserInfoAccess` in the Kepler .NET samples. No page says which is correct, whether both work, or what either grants, and the OAuth2 Client's `Scopes` property is documented as one that cannot be set. Use the value the API you call documents, and confirm it.

**Cache the token, and re-acquire it on expiry — do not refresh.** Keep the token for its lifetime (part 3 recommends 60 minutes, Relativity's own wording) and request a new one with the client secret shortly before it expires (the JavaScript, TypeScript and Python examples use 60 seconds), and once when a call is rejected for authentication. Client Credentials does not offer refresh tokens: **verified by NSerio against a live instance; not documented by Relativity** — the Allow Refresh Tokens control does not appear for this grant in the RelativityOne user interface. Relativity's own tables and text on refresh tokens conflict with each other, so re-acquisition with the secret is the advice that holds whichever way they are read.

**Long-running processes that use the Relativity SDK.** A `ServiceFactory` built once from a bearer token keeps that token after it expires. Rebuild the `ServiceFactory` and its proxies with a fresh token before expiry. This is the only advice consistent with the documentation: the package `Relativity.Kepler.Client.SDK` (documented) exposes a settable `Token` and an `HttpMessageHandler` overload but no token-provider overload, and no page documents renewing a token behind a factory.

**The client secret.** It is displayed once, at creation or reset. Store it as Secret Management (Part B of the Migration Documentation) describes; never in source, never in tracked configuration. A reset invalidates the previous secret immediately, so plan the cut-over before resetting it.

**Before deploying, confirm the calling identity.** Every call now runs as the OAuth2 Client's **Context User** — not as the user in the replaced credential, and for an import or a job as much as for a request. If that user has different workspace permissions or item-level security, the code returns fewer rows or `Unauthorized` in production with no change to its logic, and Relativity's audit attributes every call to that one account. Confirm the Context User's permissions match those of the previous identity. Relativity's REST API authentication page describes the Context User as usually a member of the Relativity Administrators group; the OAuth2 Client block (part 3) gives NSerio's recommendation, which differs.

**When the integration acts for a signed-in user.** Client Credentials is one service identity by construction. An integration that acts on behalf of a signed-in user is the Authorization Code case, and moving to it means a new OAuth2 Client, because the flow is fixed when a client is created. Which of the two flows applies depends on who supplies the credential at run time.

Verification: **Verification note — four things, and the first two can break working code.**

**Verify equivalence before rewriting.** Not every RSAPI operation has a direct Object Manager counterpart. Establish that each operation this client performs has one before starting; where one does not, this is a redesign of that operation, not a migration of it.

**A token has a finite lifetime**, set per client in minutes. A service factory built once from the token and kept for the life of a process keeps it after it expires; rebuild it with a current token within that lifetime, which the RSAPI credential never required.

**Other RSAPI uses may not appear as matches.** An RSAPI client is matched only where it is constructed with a username-and-password credential; an RSAPI client obtained without one is not covered. Migrating this call does not establish that the codebase has no RSAPI left.

**Confirm the SDK version in use provides `BearerTokenCredentials` and the Object Manager service.** Where it does not, this is an SDK upgrade before it is a code change.

OAuth2 Client: See [OAuth2 Client for Client Credentials](#oauth2-client-for-client-credentials).
<!-- generated:end CS-002 client-credentials -->

#### Authorization Code

<!-- generated:begin CS-002 authorization-code -->
**Flow.** This entry is for Authorization Code. The "Which flow" paragraph of CS-002 in [Part A](part-a-csharp.md#cs-002) says when a match belongs to this flow.

Source form:
```csharp
var client = new RSAPIClient(rsapiUri, new UsernamePasswordCredentials(userName, password));
```

Under token authentication: **First, the destination:** if this Relativity instance is not RelativityOne — including a Relativity Server instance, whose own documentation governs — no migration to RelativityOne applies and the rest of this entry does not apply.

**Two steps, not one, and in this order.** First, **move the call to Object Manager**: the RSAPI client is retired, so swapping the credential inside the RSAPI construction migrates the scheme of a call that should not survive. Rewrite the operation as the equivalent Object Manager call, reached through a service factory. Second, apply the token credential to that rewritten call:

```csharp
ServiceFactorySettings settings = new ServiceFactorySettings(
	restEndpointUri, new BearerTokenCredentials(accessToken));
```

The REST URI is `<scheme>://<machine name>/Relativity.Rest/api`, which is not the RSAPI endpoint the source form addressed.

Credentials: **Where the credentials come from.** The SDK type establishes that the destination is Relativity, not that it is RelativityOne; if it is not RelativityOne — including a Relativity Server instance, whose own documentation governs — the migration is that service's own token mechanism, and no migration to RelativityOne applies. If it is RelativityOne, the token comes from the token-acquisition part that follows.

**Common to both variants**

**Sign in.** Replace the code that collected the user's credentials with a redirect: send the user's browser to the authorize endpoint `<host>/Relativity/Identity/connect/authorize`, which leads to the RelativityOne login page. The authorize endpoint is **verified by NSerio against a live instance; not documented by Relativity**. The request carries the standard authorization-code parameters — `client_id`, `response_type=code`, the registered `redirect_uri`, a `scope`, and a `state` value the application checks on return; these are standard OAuth, and Relativity documents none of them. No discovery document is relied on.

**Receive the code.** The browser returns to the Redirect URL registered on the OAuth2 Client, with the authorization code and the `state` value. Check the `state` before using the code. The Redirect URL must be exactly the one registered (the variants say how).

**Exchange the code.** Send a form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=authorization_code`, the `code`, the same `redirect_uri`, the `client_id` and the `client_secret`, plus the `code_verifier` when PKCE is used. The code is exchanged at that endpoint, which Relativity documents for the Client Credentials grant only; the exchange is **verified by NSerio against a live instance; not documented by Relativity**. Send the `code_verifier` only when the authorize request carried a `code_challenge`; otherwise leave it out.

**.NET** (`HttpClient` and `System.Text.Json`):

```csharp
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

public sealed class AuthorizationCodeRequest
{
	public string Host { get; set; }
	public string AuthorizationCode { get; set; }
	public string RedirectUri { get; set; }
	public string ClientId { get; set; }
	public string ClientSecret { get; set; }

	// The PKCE verifier; null when no code_challenge was sent on the authorize request.
	public string CodeVerifier { get; set; }
}

public static class AuthorizationCodeExchange
{
	private const string _TOKEN_PATH = "/Relativity/Identity/connect/token";

	public static async Task<string> ExchangeAuthorizationCodeAsync(HttpClient client, AuthorizationCodeRequest request)
	{
		Dictionary<string, string> fields = new Dictionary<string, string>
		{
			{ "grant_type", "authorization_code" },
			{ "code", request.AuthorizationCode },
			{ "redirect_uri", request.RedirectUri },
			{ "client_id", request.ClientId },
			{ "client_secret", request.ClientSecret }
		};
		if (!string.IsNullOrEmpty(request.CodeVerifier))
		{
			fields["code_verifier"] = request.CodeVerifier;
		}

		string accessToken;
		using (FormUrlEncodedContent form = new FormUrlEncodedContent(fields))
		using (HttpResponseMessage response = await client.PostAsync($"{request.Host}{_TOKEN_PATH}", form))
		{
			response.EnsureSuccessStatusCode();
			using (JsonDocument document = JsonDocument.Parse(await response.Content.ReadAsStringAsync()))
			{
				accessToken = document.RootElement.GetProperty("access_token").GetString();
			}
		}

		return accessToken;
	}
}
```

**.NET Framework 4.7.2** (`System.Web.Extensions` and C# 7.3 syntax):

```csharp
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

public sealed class AuthorizationCodeRequest
{
	public string Host { get; set; }
	public string AuthorizationCode { get; set; }
	public string RedirectUri { get; set; }
	public string ClientId { get; set; }
	public string ClientSecret { get; set; }

	// The PKCE verifier; null when no code_challenge was sent on the authorize request.
	public string CodeVerifier { get; set; }
}

public static class AuthorizationCodeExchange
{
	private const string _TOKEN_PATH = "/Relativity/Identity/connect/token";

	public static async Task<string> ExchangeAuthorizationCodeAsync(HttpClient client, AuthorizationCodeRequest request)
	{
		Dictionary<string, string> fields = new Dictionary<string, string>
		{
			{ "grant_type", "authorization_code" },
			{ "code", request.AuthorizationCode },
			{ "redirect_uri", request.RedirectUri },
			{ "client_id", request.ClientId },
			{ "client_secret", request.ClientSecret }
		};
		if (!string.IsNullOrEmpty(request.CodeVerifier))
		{
			fields["code_verifier"] = request.CodeVerifier;
		}

		string accessToken;
		using (FormUrlEncodedContent form = new FormUrlEncodedContent(fields))
		using (HttpResponseMessage response = await client.PostAsync($"{request.Host}{_TOKEN_PATH}", form))
		{
			response.EnsureSuccessStatusCode();
			string body = await response.Content.ReadAsStringAsync();
			accessToken = new JavaScriptSerializer().Deserialize<Dictionary<string, object>>(body)["access_token"].ToString();
		}

		return accessToken;
	}
}
```

**Use the token.** Send `Authorization: Bearer <access token>` — the change at the use site in part 1 — with `X-CSRF-Header: -` on REST and Kepler calls.

**On expiry.** With Allow Refresh Tokens enabled on the OAuth2 Client and PKCE used, the application holds a refresh token and, when the access token expires, sends a `grant_type=refresh_token` request with it and the client credentials in place of the code exchange — *inferred* from the standard OAuth shape, not tested. The exchange example above reads only the `access_token` of the response; to renew with a refresh token, read the `refresh_token` from the same response as well and keep it as a secret, and otherwise the application sends the user through the sign-in again when the access token expires. Relativity does not document a refresh token's lifetime, whether it rotates, how it is revoked, or whether a revocation endpoint exists, nor what disabling or deleting the client does to tokens already issued. A refresh token is a secret: store it as Secret Management (Part B of the Migration Documentation) describes, and on compromise disable the client and replace it. Enable refresh tokens only if the application must keep a session beyond one token lifetime without asking the user to sign in again.

**PKCE.** PKCE is required by Relativity's own documentation to obtain a refresh token, and not otherwise: a Code request that does not ask for a refresh token works without it, **verified by NSerio against a live instance; not documented by Relativity**. It is never a blanket requirement. NSerio recommends PKCE with the `S256` method in both variants (NSerio recommendation), and does not claim that Relativity accepts any particular method, which it does not document.

**Scope.** No scope set is prescribed for Code. The scope below works — `openid RelativityRequestOrigin UserInfo UserInfoAccess AuthenticationID RelativityWeb` — **verified by NSerio against a live instance; not documented by Relativity**, verified as working and not as minimal. `state` is sent as standard OAuth practice and `nonce` is not used; Relativity does not document whether it enforces either.

**The application stops collecting the password.** After this change the application never receives, stores or forwards the user's username and password: the user signs in on RelativityOne's own page, and the application holds only tokens. Remove the credential prompt, the form field and any storage of the password that the replaced code needed.

**Single sign-on.** Sign-in happens on RelativityOne's own login page, so the instance's single sign-on and two-factor rules apply unchanged — *inferred* from where the sign-in happens, and not documented for clients that customers create.

**The client secret is required.** Code does not work without a client secret: tested and rejected, **verified by NSerio against a live instance; not documented by Relativity**, which confirms Relativity's own statement that the grant requires one. It is displayed once and is stored as Secret Management (Part B of the Migration Documentation) describes.

**Server-side variant.** For a web application whose code runs on a server:

- **Redirect.** The application receives the code at one exact HTTPS URL per environment, a route of the application, with no wildcards (NSerio recommendation; Relativity's matching rules are not documented). The Relativity administrator registers the exact URL on the OAuth2 Client.
- **Secret and tokens.** The client secret stays on the server and never reaches the browser. The user's tokens are held server-side, keyed to the session, and never in the browser.
- **Exchange.** The server performs the code exchange itself.

**Desktop variant.** For a desktop or command-line application run by a user on their own machine:

- **The system browser.** Open the authorize URL in the user's default browser and not in a web view the application hosts, so the application never sees the password (NSerio recommendation).
- **A loopback redirect on a fixed port.** Listen on the loopback address only, on the registered port, for one request. The Redirect URL is `http://127.0.0.1:<port>/<path>/`, ending with a slash because the listener below registers it as its prefix: a plain-HTTP loopback URL is accepted, **verified by NSerio against a live instance; not documented by Relativity**. The Relativity administrator registers that exact URL; because it is registered in advance, the port is fixed and not chosen at runtime.

```csharp
using System;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Net;
using System.Threading.Tasks;

public static class LoopbackAuthorizationReceiver
{
	private const string _CODE_PARAMETER = "code";
	private const string _STATE_PARAMETER = "state";
	private const string _ERROR_PARAMETER = "error";
	private const string _ERROR_DESCRIPTION_PARAMETER = "error_description";
	private const string _STATE_MISMATCH_MESSAGE = "The returned state is not the one that was sent; the response is rejected.";
	private const string _MISSING_CODE_MESSAGE = "The response carries no authorization code.";

	// Waits for the one request the browser sends to the registered loopback Redirect URL, and returns its code
	// only when its state is the one that was sent on the authorize request and the response carries no error.
	// redirectUrl is the exact URL registered on the OAuth2 Client, for example http://127.0.0.1:<port>/<path>/ :
	// it ends with a slash, as a listener prefix must.
	public static async Task<string> ReceiveAuthorizationCodeAsync(string authorizeUrl, string redirectUrl, string expectedState)
	{
		string authorizationCode;
		using (HttpListener listener = new HttpListener())
		{
			listener.Prefixes.Add(redirectUrl);
			listener.Start();
			Process.Start(new ProcessStartInfo(authorizeUrl) { UseShellExecute = true })?.Dispose();
			HttpListenerContext context = await listener.GetContextAsync();
			NameValueCollection query = context.Request.QueryString;
			context.Response.Close();
			if (query[_STATE_PARAMETER] != expectedState)
			{
				throw new InvalidOperationException(_STATE_MISMATCH_MESSAGE);
			}

			if (!string.IsNullOrEmpty(query[_ERROR_PARAMETER]))
			{
				throw new InvalidOperationException($"The authorization server returned an error: {query[_ERROR_PARAMETER]} {query[_ERROR_DESCRIPTION_PARAMETER]}".TrimEnd());
			}

			authorizationCode = query[_CODE_PARAMETER];
			if (string.IsNullOrEmpty(authorizationCode))
			{
				throw new InvalidOperationException(_MISSING_CODE_MESSAGE);
			}
		}

		return authorizationCode;
	}
}
```

- **The secret is extractable.** Code requires a client secret (above), so a distributed desktop application holds one, and **a secret shipped inside a distributed binary can be extracted by anyone who holds the binary**. It protects nothing against the person running the application.
- **Compensating guidance** (NSerio recommendation, subject to your own security policy): set Trusted IPs where the users' outbound addresses are known and stable — they restrict where a token can be *requested*, and are checked when a token is issued against the client's ID and secret; use the recommended short Access Token Lifetime of 60 minutes; enable refresh tokens only when needed; and create one OAuth2 Client per distribution, so a leaked secret can be disabled without affecting the others.
- **A documentation gap.** Relativity documents no recommendation for a distributed desktop application; this design is what NSerio verified works.

Verification: **Verification note — four things, and the first two can break working code.**

**Verify equivalence before rewriting.** Not every RSAPI operation has a direct Object Manager counterpart. Establish that each operation this client performs has one before starting; where one does not, this is a redesign of that operation, not a migration of it.

**A token has a finite lifetime**, set per client in minutes. A service factory built once from the token and kept for the life of a process keeps it after it expires; rebuild it with a current token within that lifetime, which the RSAPI credential never required.

**Other RSAPI uses may not appear as matches.** An RSAPI client is matched only where it is constructed with a username-and-password credential; an RSAPI client obtained without one is not covered. Migrating this call does not establish that the codebase has no RSAPI left.

**Confirm the SDK version in use provides `BearerTokenCredentials` and the Object Manager service.** Where it does not, this is an SDK upgrade before it is a code change.

OAuth2 Client: See [OAuth2 Client for Authorization Code](#oauth2-client-for-authorization-code).
<!-- generated:end CS-002 authorization-code -->

### CS-003 · CredentialCacheBasicScheme

<a id="cs-003-migration"></a>

#### Client Credentials

<!-- generated:begin CS-003 client-credentials -->
**Flow.** This entry is for Client Credentials. The "Which flow" paragraph of CS-003 in [Part A](part-a-csharp.md#cs-003) says when a match belongs to this flow.

Source form:
```csharp
cache.Add(new Uri(instanceUrl), "Basic", new NetworkCredential(userName, password));
```

Under token authentication: **First, the destination:** if it is not RelativityOne, no migration to RelativityOne applies — that service's own token mechanism is the migration — and the rest of this entry does not apply.

Remove **this cache entry only** — the `Add` for this host with the `Basic` scheme. A `CredentialCache` usually holds several entries for several hosts, and the others stay. The requests to this host then carry the token, set on each request, in the form the consumer of the cache accepts.

Where the cache serves `HttpClient`, on each `HttpRequestMessage`:

```csharp
request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
```

Where it serves `HttpWebRequest` or `WebClient`, whose headers have no typed `Authorization` property:

```csharp
request.Headers[HttpRequestHeader.Authorization] = $"Bearer {accessToken}";   // HttpWebRequest
client.Headers[HttpRequestHeader.Authorization] = $"Bearer {accessToken}";    // WebClient, before each call
```

Credentials: **Where the credentials come from.** The destination decides whether the migration applies. If the destination is not RelativityOne, the migration is that service's own token mechanism, and no migration to RelativityOne applies. If it is RelativityOne, the token comes from the token-acquisition part that follows.

**Where the token comes from.** Create an OAuth2 Client with Flow Grant Type set to Client Credentials (part 3 lists its settings), then send a form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=client_credentials`, `client_id`, `client_secret` and a `scope`. The token endpoint is **documented** for this grant. The token is then sent as `Authorization: Bearer <token>`, with `X-CSRF-Header: -` on REST and Kepler calls — the header is documented as required, and a Kepler call without it returns 404.

**.NET** (`HttpClient` and `System.Text.Json`). The class requests the token with the client ID and the client secret, caches it, requests a new one 60 seconds before it expires, and, when a call is rejected for authentication, requests a new one and sends the call once more. Build one instance for the life of the process and send every call through `SendAsync`; the request is created by the delegate on each attempt, because a request message cannot be sent twice. The client ID and the client secret come from the process environment or a secret store, never from source. Where the code uses the Relativity SDK, `GetAccessTokenAsync(false)` gives the token for `BearerTokenCredentials` each time a `ServiceFactory` is built.

```csharp
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

public sealed class AccessTokenProvider : IDisposable
{
	private const string _TOKEN_PATH = "/Relativity/Identity/connect/token";
	private const string _BEARER_SCHEME = "Bearer";
	private const string _CSRF_HEADER_NAME = "X-CSRF-Header";
	private const string _CSRF_HEADER_VALUE = "-";
	private static readonly TimeSpan _renewalMargin = TimeSpan.FromSeconds(60);

	private readonly HttpClient _client = new HttpClient();
	private readonly SemaphoreSlim _gate = new SemaphoreSlim(1, 1);
	private readonly string _host;
	private readonly string _clientId;
	private readonly string _clientSecret;
	private readonly string _scope;
	private string _accessToken;
	private DateTimeOffset _expiresAt;

	public AccessTokenProvider(string host, string clientId, string clientSecret, string scope)
	{
		_host = host;
		_clientId = clientId;
		_clientSecret = clientSecret;
		_scope = scope;
	}

	// Cached token; renewed shortly before it expires, or on demand after a rejected call.
	public async Task<string> GetAccessTokenAsync(bool forceRenewal)
	{
		await _gate.WaitAsync();
		try
		{
			if (forceRenewal || _accessToken == null || DateTimeOffset.UtcNow >= _expiresAt - _renewalMargin)
			{
				await AcquireAccessTokenAsync();
			}

			return _accessToken;
		}
		finally
		{
			_gate.Release();
		}
	}

	// Sends the request with the bearer token; when the call is rejected for authentication, renews the token and sends it once more.
	public async Task<HttpResponseMessage> SendAsync(Func<HttpRequestMessage> createRequest)
	{
		HttpResponseMessage response = await SendOnceAsync(createRequest, false);
		if (response.StatusCode == HttpStatusCode.Unauthorized)
		{
			response.Dispose();
			response = await SendOnceAsync(createRequest, true);
		}

		return response;
	}

	public void Dispose()
	{
		_client.Dispose();
		_gate.Dispose();
	}

	private async Task AcquireAccessTokenAsync()
	{
		using (FormUrlEncodedContent form = new FormUrlEncodedContent(new Dictionary<string, string>
		{
			{ "grant_type", "client_credentials" },
			{ "client_id", _clientId },
			{ "client_secret", _clientSecret },
			{ "scope", _scope }
		}))
		using (HttpResponseMessage response = await _client.PostAsync($"{_host}{_TOKEN_PATH}", form))
		{
			response.EnsureSuccessStatusCode();
			using (JsonDocument document = JsonDocument.Parse(await response.Content.ReadAsStringAsync()))
			{
				_accessToken = document.RootElement.GetProperty("access_token").GetString();
				_expiresAt = DateTimeOffset.UtcNow.AddSeconds(document.RootElement.GetProperty("expires_in").GetInt32());
			}
		}
	}

	private async Task<HttpResponseMessage> SendOnceAsync(Func<HttpRequestMessage> createRequest, bool forceRenewal)
	{
		string token = await GetAccessTokenAsync(forceRenewal);
		HttpResponseMessage response;
		using (HttpRequestMessage request = createRequest())
		{
			request.Headers.Authorization = new AuthenticationHeaderValue(_BEARER_SCHEME, token);
			request.Headers.Add(_CSRF_HEADER_NAME, _CSRF_HEADER_VALUE);
			response = await _client.SendAsync(request);
		}

		return response;
	}
}
```

**.NET Framework 4.7.2**: the same class with the serializer the runtime ships (`System.Web.Extensions`, referenced together with `System.Net.Http`), and C# 7.3 syntax:

```csharp
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

public sealed class AccessTokenProvider : IDisposable
{
	private const string _TOKEN_PATH = "/Relativity/Identity/connect/token";
	private const string _BEARER_SCHEME = "Bearer";
	private const string _CSRF_HEADER_NAME = "X-CSRF-Header";
	private const string _CSRF_HEADER_VALUE = "-";
	private static readonly TimeSpan _renewalMargin = TimeSpan.FromSeconds(60);

	private readonly HttpClient _client = new HttpClient();
	private readonly SemaphoreSlim _gate = new SemaphoreSlim(1, 1);
	private readonly string _host;
	private readonly string _clientId;
	private readonly string _clientSecret;
	private readonly string _scope;
	private string _accessToken;
	private DateTimeOffset _expiresAt;

	public AccessTokenProvider(string host, string clientId, string clientSecret, string scope)
	{
		_host = host;
		_clientId = clientId;
		_clientSecret = clientSecret;
		_scope = scope;
	}

	// Cached token; renewed shortly before it expires, or on demand after a rejected call.
	public async Task<string> GetAccessTokenAsync(bool forceRenewal)
	{
		await _gate.WaitAsync();
		try
		{
			if (forceRenewal || _accessToken == null || DateTimeOffset.UtcNow >= _expiresAt - _renewalMargin)
			{
				await AcquireAccessTokenAsync();
			}

			return _accessToken;
		}
		finally
		{
			_gate.Release();
		}
	}

	// Sends the request with the bearer token; when the call is rejected for authentication, renews the token and sends it once more.
	public async Task<HttpResponseMessage> SendAsync(Func<HttpRequestMessage> createRequest)
	{
		HttpResponseMessage response = await SendOnceAsync(createRequest, false);
		if (response.StatusCode == HttpStatusCode.Unauthorized)
		{
			response.Dispose();
			response = await SendOnceAsync(createRequest, true);
		}

		return response;
	}

	public void Dispose()
	{
		_client.Dispose();
		_gate.Dispose();
	}

	private async Task AcquireAccessTokenAsync()
	{
		using (FormUrlEncodedContent form = new FormUrlEncodedContent(new Dictionary<string, string>
		{
			{ "grant_type", "client_credentials" },
			{ "client_id", _clientId },
			{ "client_secret", _clientSecret },
			{ "scope", _scope }
		}))
		using (HttpResponseMessage response = await _client.PostAsync($"{_host}{_TOKEN_PATH}", form))
		{
			response.EnsureSuccessStatusCode();
			string body = await response.Content.ReadAsStringAsync();
			Dictionary<string, object> fields = new JavaScriptSerializer().Deserialize<Dictionary<string, object>>(body);
			_accessToken = fields["access_token"].ToString();
			_expiresAt = DateTimeOffset.UtcNow.AddSeconds(Convert.ToInt32(fields["expires_in"]));
		}
	}

	private async Task<HttpResponseMessage> SendOnceAsync(Func<HttpRequestMessage> createRequest, bool forceRenewal)
	{
		string token = await GetAccessTokenAsync(forceRenewal);
		HttpResponseMessage response;
		using (HttpRequestMessage request = createRequest())
		{
			request.Headers.Authorization = new AuthenticationHeaderValue(_BEARER_SCHEME, token);
			request.Headers.Add(_CSRF_HEADER_NAME, _CSRF_HEADER_VALUE);
			response = await _client.SendAsync(request);
		}

		return response;
	}
}
```

**The scope depends on what the integration needs, and no single scope is required.** Relativity's documentation shows two values for this grant: `SystemUserInfo` on the REST authentication page, the Usage Reports page and in the official Transfer SDK sample, and `UserInfoAccess` in the Kepler .NET samples. No page says which is correct, whether both work, or what either grants, and the OAuth2 Client's `Scopes` property is documented as one that cannot be set. Use the value the API you call documents, and confirm it.

**Cache the token, and re-acquire it on expiry — do not refresh.** Keep the token for its lifetime (part 3 recommends 60 minutes, Relativity's own wording) and request a new one with the client secret shortly before it expires (the JavaScript, TypeScript and Python examples use 60 seconds), and once when a call is rejected for authentication. Client Credentials does not offer refresh tokens: **verified by NSerio against a live instance; not documented by Relativity** — the Allow Refresh Tokens control does not appear for this grant in the RelativityOne user interface. Relativity's own tables and text on refresh tokens conflict with each other, so re-acquisition with the secret is the advice that holds whichever way they are read.

**Long-running processes that use the Relativity SDK.** A `ServiceFactory` built once from a bearer token keeps that token after it expires. Rebuild the `ServiceFactory` and its proxies with a fresh token before expiry. This is the only advice consistent with the documentation: the package `Relativity.Kepler.Client.SDK` (documented) exposes a settable `Token` and an `HttpMessageHandler` overload but no token-provider overload, and no page documents renewing a token behind a factory.

**The client secret.** It is displayed once, at creation or reset. Store it as Secret Management (Part B of the Migration Documentation) describes; never in source, never in tracked configuration. A reset invalidates the previous secret immediately, so plan the cut-over before resetting it.

**Before deploying, confirm the calling identity.** Every call now runs as the OAuth2 Client's **Context User** — not as the user in the replaced credential, and for an import or a job as much as for a request. If that user has different workspace permissions or item-level security, the code returns fewer rows or `Unauthorized` in production with no change to its logic, and Relativity's audit attributes every call to that one account. Confirm the Context User's permissions match those of the previous identity. Relativity's REST API authentication page describes the Context User as usually a member of the Relativity Administrators group; the OAuth2 Client block (part 3) gives NSerio's recommendation, which differs.

**When the integration acts for a signed-in user.** Client Credentials is one service identity by construction. An integration that acts on behalf of a signed-in user is the Authorization Code case, and moving to it means a new OAuth2 Client, because the flow is fixed when a client is created. Which of the two flows applies depends on who supplies the credential at run time.

Verification: **Verification note — five things.**

**The 401-challenge response is lost.** A `CredentialCache` entry answers the server's challenge through the handler's credential path; a bearer header is sent up front and answers nothing. Code or a server that depended on that exchange behaves differently after the change.

**Redirects stop carrying the credential.** Cache credentials are re-applied to a redirect target; a manually set `Authorization` header is not. .NET documents that `HttpWebRequest` clears the `Authorization` header when it follows a redirect automatically, and the `HttpClient` handler drops it on redirect. Code that reached this host through a redirect starts returning 401 after the migration; address the final URI, or handle the redirect explicitly.

**Removing the wrong entry breaks another host.** The cache is keyed by URI and scheme. Remove only the entry for this host and `Basic`; an entry for another host or another scheme is not part of this match.

**A token has a finite lifetime, and a header set once on a long-lived client cannot be refreshed.** Set it per request, not on a shared client's default headers, or the code works for the token's lifetime and then returns 401 until the process restarts.

**Confirm the endpoint in use is the RelativityOne REST API.** That is the API whose bearer token this guidance describes; an endpoint elsewhere reached from the same code may accept only Basic and reject the migrated call.

OAuth2 Client: See [OAuth2 Client for Client Credentials](#oauth2-client-for-client-credentials).
<!-- generated:end CS-003 client-credentials -->

#### Authorization Code

<!-- generated:begin CS-003 authorization-code -->
**Flow.** This entry is for Authorization Code. The "Which flow" paragraph of CS-003 in [Part A](part-a-csharp.md#cs-003) says when a match belongs to this flow.

Source form:
```csharp
cache.Add(new Uri(instanceUrl), "Basic", new NetworkCredential(userName, password));
```

Under token authentication: **First, the destination:** if it is not RelativityOne, no migration to RelativityOne applies — that service's own token mechanism is the migration — and the rest of this entry does not apply.

Remove **this cache entry only** — the `Add` for this host with the `Basic` scheme. A `CredentialCache` usually holds several entries for several hosts, and the others stay. The requests to this host then carry the token, set on each request, in the form the consumer of the cache accepts.

Where the cache serves `HttpClient`, on each `HttpRequestMessage`:

```csharp
request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
```

Where it serves `HttpWebRequest` or `WebClient`, whose headers have no typed `Authorization` property:

```csharp
request.Headers[HttpRequestHeader.Authorization] = $"Bearer {accessToken}";   // HttpWebRequest
client.Headers[HttpRequestHeader.Authorization] = $"Bearer {accessToken}";    // WebClient, before each call
```

Credentials: **Where the credentials come from.** The destination decides whether the migration applies. If the destination is not RelativityOne, the migration is that service's own token mechanism, and no migration to RelativityOne applies. If it is RelativityOne, the token comes from the token-acquisition part that follows.

**Common to both variants**

**Sign in.** Replace the code that collected the user's credentials with a redirect: send the user's browser to the authorize endpoint `<host>/Relativity/Identity/connect/authorize`, which leads to the RelativityOne login page. The authorize endpoint is **verified by NSerio against a live instance; not documented by Relativity**. The request carries the standard authorization-code parameters — `client_id`, `response_type=code`, the registered `redirect_uri`, a `scope`, and a `state` value the application checks on return; these are standard OAuth, and Relativity documents none of them. No discovery document is relied on.

**Receive the code.** The browser returns to the Redirect URL registered on the OAuth2 Client, with the authorization code and the `state` value. Check the `state` before using the code. The Redirect URL must be exactly the one registered (the variants say how).

**Exchange the code.** Send a form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=authorization_code`, the `code`, the same `redirect_uri`, the `client_id` and the `client_secret`, plus the `code_verifier` when PKCE is used. The code is exchanged at that endpoint, which Relativity documents for the Client Credentials grant only; the exchange is **verified by NSerio against a live instance; not documented by Relativity**. Send the `code_verifier` only when the authorize request carried a `code_challenge`; otherwise leave it out.

**.NET** (`HttpClient` and `System.Text.Json`):

```csharp
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

public sealed class AuthorizationCodeRequest
{
	public string Host { get; set; }
	public string AuthorizationCode { get; set; }
	public string RedirectUri { get; set; }
	public string ClientId { get; set; }
	public string ClientSecret { get; set; }

	// The PKCE verifier; null when no code_challenge was sent on the authorize request.
	public string CodeVerifier { get; set; }
}

public static class AuthorizationCodeExchange
{
	private const string _TOKEN_PATH = "/Relativity/Identity/connect/token";

	public static async Task<string> ExchangeAuthorizationCodeAsync(HttpClient client, AuthorizationCodeRequest request)
	{
		Dictionary<string, string> fields = new Dictionary<string, string>
		{
			{ "grant_type", "authorization_code" },
			{ "code", request.AuthorizationCode },
			{ "redirect_uri", request.RedirectUri },
			{ "client_id", request.ClientId },
			{ "client_secret", request.ClientSecret }
		};
		if (!string.IsNullOrEmpty(request.CodeVerifier))
		{
			fields["code_verifier"] = request.CodeVerifier;
		}

		string accessToken;
		using (FormUrlEncodedContent form = new FormUrlEncodedContent(fields))
		using (HttpResponseMessage response = await client.PostAsync($"{request.Host}{_TOKEN_PATH}", form))
		{
			response.EnsureSuccessStatusCode();
			using (JsonDocument document = JsonDocument.Parse(await response.Content.ReadAsStringAsync()))
			{
				accessToken = document.RootElement.GetProperty("access_token").GetString();
			}
		}

		return accessToken;
	}
}
```

**.NET Framework 4.7.2** (`System.Web.Extensions` and C# 7.3 syntax):

```csharp
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

public sealed class AuthorizationCodeRequest
{
	public string Host { get; set; }
	public string AuthorizationCode { get; set; }
	public string RedirectUri { get; set; }
	public string ClientId { get; set; }
	public string ClientSecret { get; set; }

	// The PKCE verifier; null when no code_challenge was sent on the authorize request.
	public string CodeVerifier { get; set; }
}

public static class AuthorizationCodeExchange
{
	private const string _TOKEN_PATH = "/Relativity/Identity/connect/token";

	public static async Task<string> ExchangeAuthorizationCodeAsync(HttpClient client, AuthorizationCodeRequest request)
	{
		Dictionary<string, string> fields = new Dictionary<string, string>
		{
			{ "grant_type", "authorization_code" },
			{ "code", request.AuthorizationCode },
			{ "redirect_uri", request.RedirectUri },
			{ "client_id", request.ClientId },
			{ "client_secret", request.ClientSecret }
		};
		if (!string.IsNullOrEmpty(request.CodeVerifier))
		{
			fields["code_verifier"] = request.CodeVerifier;
		}

		string accessToken;
		using (FormUrlEncodedContent form = new FormUrlEncodedContent(fields))
		using (HttpResponseMessage response = await client.PostAsync($"{request.Host}{_TOKEN_PATH}", form))
		{
			response.EnsureSuccessStatusCode();
			string body = await response.Content.ReadAsStringAsync();
			accessToken = new JavaScriptSerializer().Deserialize<Dictionary<string, object>>(body)["access_token"].ToString();
		}

		return accessToken;
	}
}
```

**Use the token.** Send `Authorization: Bearer <access token>` — the change at the use site in part 1 — with `X-CSRF-Header: -` on REST and Kepler calls.

**On expiry.** With Allow Refresh Tokens enabled on the OAuth2 Client and PKCE used, the application holds a refresh token and, when the access token expires, sends a `grant_type=refresh_token` request with it and the client credentials in place of the code exchange — *inferred* from the standard OAuth shape, not tested. The exchange example above reads only the `access_token` of the response; to renew with a refresh token, read the `refresh_token` from the same response as well and keep it as a secret, and otherwise the application sends the user through the sign-in again when the access token expires. Relativity does not document a refresh token's lifetime, whether it rotates, how it is revoked, or whether a revocation endpoint exists, nor what disabling or deleting the client does to tokens already issued. A refresh token is a secret: store it as Secret Management (Part B of the Migration Documentation) describes, and on compromise disable the client and replace it. Enable refresh tokens only if the application must keep a session beyond one token lifetime without asking the user to sign in again.

**PKCE.** PKCE is required by Relativity's own documentation to obtain a refresh token, and not otherwise: a Code request that does not ask for a refresh token works without it, **verified by NSerio against a live instance; not documented by Relativity**. It is never a blanket requirement. NSerio recommends PKCE with the `S256` method in both variants (NSerio recommendation), and does not claim that Relativity accepts any particular method, which it does not document.

**Scope.** No scope set is prescribed for Code. The scope below works — `openid RelativityRequestOrigin UserInfo UserInfoAccess AuthenticationID RelativityWeb` — **verified by NSerio against a live instance; not documented by Relativity**, verified as working and not as minimal. `state` is sent as standard OAuth practice and `nonce` is not used; Relativity does not document whether it enforces either.

**The application stops collecting the password.** After this change the application never receives, stores or forwards the user's username and password: the user signs in on RelativityOne's own page, and the application holds only tokens. Remove the credential prompt, the form field and any storage of the password that the replaced code needed.

**Single sign-on.** Sign-in happens on RelativityOne's own login page, so the instance's single sign-on and two-factor rules apply unchanged — *inferred* from where the sign-in happens, and not documented for clients that customers create.

**The client secret is required.** Code does not work without a client secret: tested and rejected, **verified by NSerio against a live instance; not documented by Relativity**, which confirms Relativity's own statement that the grant requires one. It is displayed once and is stored as Secret Management (Part B of the Migration Documentation) describes.

**Server-side variant.** For a web application whose code runs on a server:

- **Redirect.** The application receives the code at one exact HTTPS URL per environment, a route of the application, with no wildcards (NSerio recommendation; Relativity's matching rules are not documented). The Relativity administrator registers the exact URL on the OAuth2 Client.
- **Secret and tokens.** The client secret stays on the server and never reaches the browser. The user's tokens are held server-side, keyed to the session, and never in the browser.
- **Exchange.** The server performs the code exchange itself.

**Desktop variant.** For a desktop or command-line application run by a user on their own machine:

- **The system browser.** Open the authorize URL in the user's default browser and not in a web view the application hosts, so the application never sees the password (NSerio recommendation).
- **A loopback redirect on a fixed port.** Listen on the loopback address only, on the registered port, for one request. The Redirect URL is `http://127.0.0.1:<port>/<path>/`, ending with a slash because the listener below registers it as its prefix: a plain-HTTP loopback URL is accepted, **verified by NSerio against a live instance; not documented by Relativity**. The Relativity administrator registers that exact URL; because it is registered in advance, the port is fixed and not chosen at runtime.

```csharp
using System;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Net;
using System.Threading.Tasks;

public static class LoopbackAuthorizationReceiver
{
	private const string _CODE_PARAMETER = "code";
	private const string _STATE_PARAMETER = "state";
	private const string _ERROR_PARAMETER = "error";
	private const string _ERROR_DESCRIPTION_PARAMETER = "error_description";
	private const string _STATE_MISMATCH_MESSAGE = "The returned state is not the one that was sent; the response is rejected.";
	private const string _MISSING_CODE_MESSAGE = "The response carries no authorization code.";

	// Waits for the one request the browser sends to the registered loopback Redirect URL, and returns its code
	// only when its state is the one that was sent on the authorize request and the response carries no error.
	// redirectUrl is the exact URL registered on the OAuth2 Client, for example http://127.0.0.1:<port>/<path>/ :
	// it ends with a slash, as a listener prefix must.
	public static async Task<string> ReceiveAuthorizationCodeAsync(string authorizeUrl, string redirectUrl, string expectedState)
	{
		string authorizationCode;
		using (HttpListener listener = new HttpListener())
		{
			listener.Prefixes.Add(redirectUrl);
			listener.Start();
			Process.Start(new ProcessStartInfo(authorizeUrl) { UseShellExecute = true })?.Dispose();
			HttpListenerContext context = await listener.GetContextAsync();
			NameValueCollection query = context.Request.QueryString;
			context.Response.Close();
			if (query[_STATE_PARAMETER] != expectedState)
			{
				throw new InvalidOperationException(_STATE_MISMATCH_MESSAGE);
			}

			if (!string.IsNullOrEmpty(query[_ERROR_PARAMETER]))
			{
				throw new InvalidOperationException($"The authorization server returned an error: {query[_ERROR_PARAMETER]} {query[_ERROR_DESCRIPTION_PARAMETER]}".TrimEnd());
			}

			authorizationCode = query[_CODE_PARAMETER];
			if (string.IsNullOrEmpty(authorizationCode))
			{
				throw new InvalidOperationException(_MISSING_CODE_MESSAGE);
			}
		}

		return authorizationCode;
	}
}
```

- **The secret is extractable.** Code requires a client secret (above), so a distributed desktop application holds one, and **a secret shipped inside a distributed binary can be extracted by anyone who holds the binary**. It protects nothing against the person running the application.
- **Compensating guidance** (NSerio recommendation, subject to your own security policy): set Trusted IPs where the users' outbound addresses are known and stable — they restrict where a token can be *requested*, and are checked when a token is issued against the client's ID and secret; use the recommended short Access Token Lifetime of 60 minutes; enable refresh tokens only when needed; and create one OAuth2 Client per distribution, so a leaked secret can be disabled without affecting the others.
- **A documentation gap.** Relativity documents no recommendation for a distributed desktop application; this design is what NSerio verified works.

Verification: **Verification note — five things.**

**The 401-challenge response is lost.** A `CredentialCache` entry answers the server's challenge through the handler's credential path; a bearer header is sent up front and answers nothing. Code or a server that depended on that exchange behaves differently after the change.

**Redirects stop carrying the credential.** Cache credentials are re-applied to a redirect target; a manually set `Authorization` header is not. .NET documents that `HttpWebRequest` clears the `Authorization` header when it follows a redirect automatically, and the `HttpClient` handler drops it on redirect. Code that reached this host through a redirect starts returning 401 after the migration; address the final URI, or handle the redirect explicitly.

**Removing the wrong entry breaks another host.** The cache is keyed by URI and scheme. Remove only the entry for this host and `Basic`; an entry for another host or another scheme is not part of this match.

**A token has a finite lifetime, and a header set once on a long-lived client cannot be refreshed.** Set it per request, not on a shared client's default headers, or the code works for the token's lifetime and then returns 401 until the process restarts.

**Confirm the endpoint in use is the RelativityOne REST API.** That is the API whose bearer token this guidance describes; an endpoint elsewhere reached from the same code may accept only Basic and reject the migrated call.

OAuth2 Client: See [OAuth2 Client for Authorization Code](#oauth2-client-for-authorization-code).
<!-- generated:end CS-003 authorization-code -->

### CS-004 · ThirdPartyBasicAuthenticatorType

<a id="cs-004-migration"></a>

#### Client Credentials

<!-- generated:begin CS-004 client-credentials -->
**Flow.** This entry is for Client Credentials. The "Which flow" paragraph of CS-004 in [Part A](part-a-csharp.md#cs-004) says when a match belongs to this flow.

Source form:
```csharp
var authenticator = new HttpBasicAuthenticator(userName, password);
```

Under token authentication: **First, the destination:** if it is not RelativityOne, no migration to RelativityOne applies — that service's own token mechanism is the migration — and the rest of this entry does not apply.

Replace the Basic authenticator with **the same library's bearer or token authenticator, carrying the access token** in place of the credential pair. This guidance names no target type: the construct occurs in more than one library, and the token authenticator's name differs between them and between versions.

Credentials: **Where the credentials come from.** The destination decides whether the migration applies. If the destination is not RelativityOne, the migration is that service's own token mechanism, and no migration to RelativityOne applies. If it is RelativityOne, the token comes from the token-acquisition part that follows.

**Where the token comes from.** Create an OAuth2 Client with Flow Grant Type set to Client Credentials (part 3 lists its settings), then send a form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=client_credentials`, `client_id`, `client_secret` and a `scope`. The token endpoint is **documented** for this grant. The token is then sent as `Authorization: Bearer <token>`, with `X-CSRF-Header: -` on REST and Kepler calls — the header is documented as required, and a Kepler call without it returns 404.

**.NET** (`HttpClient` and `System.Text.Json`). The class requests the token with the client ID and the client secret, caches it, requests a new one 60 seconds before it expires, and, when a call is rejected for authentication, requests a new one and sends the call once more. Build one instance for the life of the process and send every call through `SendAsync`; the request is created by the delegate on each attempt, because a request message cannot be sent twice. The client ID and the client secret come from the process environment or a secret store, never from source. Where the code uses the Relativity SDK, `GetAccessTokenAsync(false)` gives the token for `BearerTokenCredentials` each time a `ServiceFactory` is built.

```csharp
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

public sealed class AccessTokenProvider : IDisposable
{
	private const string _TOKEN_PATH = "/Relativity/Identity/connect/token";
	private const string _BEARER_SCHEME = "Bearer";
	private const string _CSRF_HEADER_NAME = "X-CSRF-Header";
	private const string _CSRF_HEADER_VALUE = "-";
	private static readonly TimeSpan _renewalMargin = TimeSpan.FromSeconds(60);

	private readonly HttpClient _client = new HttpClient();
	private readonly SemaphoreSlim _gate = new SemaphoreSlim(1, 1);
	private readonly string _host;
	private readonly string _clientId;
	private readonly string _clientSecret;
	private readonly string _scope;
	private string _accessToken;
	private DateTimeOffset _expiresAt;

	public AccessTokenProvider(string host, string clientId, string clientSecret, string scope)
	{
		_host = host;
		_clientId = clientId;
		_clientSecret = clientSecret;
		_scope = scope;
	}

	// Cached token; renewed shortly before it expires, or on demand after a rejected call.
	public async Task<string> GetAccessTokenAsync(bool forceRenewal)
	{
		await _gate.WaitAsync();
		try
		{
			if (forceRenewal || _accessToken == null || DateTimeOffset.UtcNow >= _expiresAt - _renewalMargin)
			{
				await AcquireAccessTokenAsync();
			}

			return _accessToken;
		}
		finally
		{
			_gate.Release();
		}
	}

	// Sends the request with the bearer token; when the call is rejected for authentication, renews the token and sends it once more.
	public async Task<HttpResponseMessage> SendAsync(Func<HttpRequestMessage> createRequest)
	{
		HttpResponseMessage response = await SendOnceAsync(createRequest, false);
		if (response.StatusCode == HttpStatusCode.Unauthorized)
		{
			response.Dispose();
			response = await SendOnceAsync(createRequest, true);
		}

		return response;
	}

	public void Dispose()
	{
		_client.Dispose();
		_gate.Dispose();
	}

	private async Task AcquireAccessTokenAsync()
	{
		using (FormUrlEncodedContent form = new FormUrlEncodedContent(new Dictionary<string, string>
		{
			{ "grant_type", "client_credentials" },
			{ "client_id", _clientId },
			{ "client_secret", _clientSecret },
			{ "scope", _scope }
		}))
		using (HttpResponseMessage response = await _client.PostAsync($"{_host}{_TOKEN_PATH}", form))
		{
			response.EnsureSuccessStatusCode();
			using (JsonDocument document = JsonDocument.Parse(await response.Content.ReadAsStringAsync()))
			{
				_accessToken = document.RootElement.GetProperty("access_token").GetString();
				_expiresAt = DateTimeOffset.UtcNow.AddSeconds(document.RootElement.GetProperty("expires_in").GetInt32());
			}
		}
	}

	private async Task<HttpResponseMessage> SendOnceAsync(Func<HttpRequestMessage> createRequest, bool forceRenewal)
	{
		string token = await GetAccessTokenAsync(forceRenewal);
		HttpResponseMessage response;
		using (HttpRequestMessage request = createRequest())
		{
			request.Headers.Authorization = new AuthenticationHeaderValue(_BEARER_SCHEME, token);
			request.Headers.Add(_CSRF_HEADER_NAME, _CSRF_HEADER_VALUE);
			response = await _client.SendAsync(request);
		}

		return response;
	}
}
```

**.NET Framework 4.7.2**: the same class with the serializer the runtime ships (`System.Web.Extensions`, referenced together with `System.Net.Http`), and C# 7.3 syntax:

```csharp
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

public sealed class AccessTokenProvider : IDisposable
{
	private const string _TOKEN_PATH = "/Relativity/Identity/connect/token";
	private const string _BEARER_SCHEME = "Bearer";
	private const string _CSRF_HEADER_NAME = "X-CSRF-Header";
	private const string _CSRF_HEADER_VALUE = "-";
	private static readonly TimeSpan _renewalMargin = TimeSpan.FromSeconds(60);

	private readonly HttpClient _client = new HttpClient();
	private readonly SemaphoreSlim _gate = new SemaphoreSlim(1, 1);
	private readonly string _host;
	private readonly string _clientId;
	private readonly string _clientSecret;
	private readonly string _scope;
	private string _accessToken;
	private DateTimeOffset _expiresAt;

	public AccessTokenProvider(string host, string clientId, string clientSecret, string scope)
	{
		_host = host;
		_clientId = clientId;
		_clientSecret = clientSecret;
		_scope = scope;
	}

	// Cached token; renewed shortly before it expires, or on demand after a rejected call.
	public async Task<string> GetAccessTokenAsync(bool forceRenewal)
	{
		await _gate.WaitAsync();
		try
		{
			if (forceRenewal || _accessToken == null || DateTimeOffset.UtcNow >= _expiresAt - _renewalMargin)
			{
				await AcquireAccessTokenAsync();
			}

			return _accessToken;
		}
		finally
		{
			_gate.Release();
		}
	}

	// Sends the request with the bearer token; when the call is rejected for authentication, renews the token and sends it once more.
	public async Task<HttpResponseMessage> SendAsync(Func<HttpRequestMessage> createRequest)
	{
		HttpResponseMessage response = await SendOnceAsync(createRequest, false);
		if (response.StatusCode == HttpStatusCode.Unauthorized)
		{
			response.Dispose();
			response = await SendOnceAsync(createRequest, true);
		}

		return response;
	}

	public void Dispose()
	{
		_client.Dispose();
		_gate.Dispose();
	}

	private async Task AcquireAccessTokenAsync()
	{
		using (FormUrlEncodedContent form = new FormUrlEncodedContent(new Dictionary<string, string>
		{
			{ "grant_type", "client_credentials" },
			{ "client_id", _clientId },
			{ "client_secret", _clientSecret },
			{ "scope", _scope }
		}))
		using (HttpResponseMessage response = await _client.PostAsync($"{_host}{_TOKEN_PATH}", form))
		{
			response.EnsureSuccessStatusCode();
			string body = await response.Content.ReadAsStringAsync();
			Dictionary<string, object> fields = new JavaScriptSerializer().Deserialize<Dictionary<string, object>>(body);
			_accessToken = fields["access_token"].ToString();
			_expiresAt = DateTimeOffset.UtcNow.AddSeconds(Convert.ToInt32(fields["expires_in"]));
		}
	}

	private async Task<HttpResponseMessage> SendOnceAsync(Func<HttpRequestMessage> createRequest, bool forceRenewal)
	{
		string token = await GetAccessTokenAsync(forceRenewal);
		HttpResponseMessage response;
		using (HttpRequestMessage request = createRequest())
		{
			request.Headers.Authorization = new AuthenticationHeaderValue(_BEARER_SCHEME, token);
			request.Headers.Add(_CSRF_HEADER_NAME, _CSRF_HEADER_VALUE);
			response = await _client.SendAsync(request);
		}

		return response;
	}
}
```

**The scope depends on what the integration needs, and no single scope is required.** Relativity's documentation shows two values for this grant: `SystemUserInfo` on the REST authentication page, the Usage Reports page and in the official Transfer SDK sample, and `UserInfoAccess` in the Kepler .NET samples. No page says which is correct, whether both work, or what either grants, and the OAuth2 Client's `Scopes` property is documented as one that cannot be set. Use the value the API you call documents, and confirm it.

**Cache the token, and re-acquire it on expiry — do not refresh.** Keep the token for its lifetime (part 3 recommends 60 minutes, Relativity's own wording) and request a new one with the client secret shortly before it expires (the JavaScript, TypeScript and Python examples use 60 seconds), and once when a call is rejected for authentication. Client Credentials does not offer refresh tokens: **verified by NSerio against a live instance; not documented by Relativity** — the Allow Refresh Tokens control does not appear for this grant in the RelativityOne user interface. Relativity's own tables and text on refresh tokens conflict with each other, so re-acquisition with the secret is the advice that holds whichever way they are read.

**Long-running processes that use the Relativity SDK.** A `ServiceFactory` built once from a bearer token keeps that token after it expires. Rebuild the `ServiceFactory` and its proxies with a fresh token before expiry. This is the only advice consistent with the documentation: the package `Relativity.Kepler.Client.SDK` (documented) exposes a settable `Token` and an `HttpMessageHandler` overload but no token-provider overload, and no page documents renewing a token behind a factory.

**The client secret.** It is displayed once, at creation or reset. Store it as Secret Management (Part B of the Migration Documentation) describes; never in source, never in tracked configuration. A reset invalidates the previous secret immediately, so plan the cut-over before resetting it.

**Before deploying, confirm the calling identity.** Every call now runs as the OAuth2 Client's **Context User** — not as the user in the replaced credential, and for an import or a job as much as for a request. If that user has different workspace permissions or item-level security, the code returns fewer rows or `Unauthorized` in production with no change to its logic, and Relativity's audit attributes every call to that one account. Confirm the Context User's permissions match those of the previous identity. Relativity's REST API authentication page describes the Context User as usually a member of the Relativity Administrators group; the OAuth2 Client block (part 3) gives NSerio's recommendation, which differs.

**When the integration acts for a signed-in user.** Client Credentials is one service identity by construction. An integration that acts on behalf of a signed-in user is the Authorization Code case, and moving to it means a new OAuth2 Client, because the flow is fixed when a client is created. Which of the two flows applies depends on who supplies the credential at run time.

Verification: **Verification note — three things.**

**Confirm the library version in use offers a token authenticator.** Where it does not, this is a library upgrade before it is a code change, with whatever that upgrade carries.

**A token has a finite lifetime.** An authenticator constructed once with a fixed token string and attached to a long-lived client holds that token after it expires. Whether the library can refresh it is the library's behaviour and is not established here; where it cannot, construct the authenticator with a current token for each request or each short-lived client.

**Confirm the endpoint in use is the RelativityOne REST API.** The authenticator changes the scheme on the wire, and an endpoint elsewhere reached from the same code may accept only Basic and reject the migrated call.

OAuth2 Client: See [OAuth2 Client for Client Credentials](#oauth2-client-for-client-credentials).
<!-- generated:end CS-004 client-credentials -->

#### Authorization Code

<!-- generated:begin CS-004 authorization-code -->
**Flow.** This entry is for Authorization Code. The "Which flow" paragraph of CS-004 in [Part A](part-a-csharp.md#cs-004) says when a match belongs to this flow.

Source form:
```csharp
var authenticator = new HttpBasicAuthenticator(userName, password);
```

Under token authentication: **First, the destination:** if it is not RelativityOne, no migration to RelativityOne applies — that service's own token mechanism is the migration — and the rest of this entry does not apply.

Replace the Basic authenticator with **the same library's bearer or token authenticator, carrying the access token** in place of the credential pair. This guidance names no target type: the construct occurs in more than one library, and the token authenticator's name differs between them and between versions.

Credentials: **Where the credentials come from.** The destination decides whether the migration applies. If the destination is not RelativityOne, the migration is that service's own token mechanism, and no migration to RelativityOne applies. If it is RelativityOne, the token comes from the token-acquisition part that follows.

**Common to both variants**

**Sign in.** Replace the code that collected the user's credentials with a redirect: send the user's browser to the authorize endpoint `<host>/Relativity/Identity/connect/authorize`, which leads to the RelativityOne login page. The authorize endpoint is **verified by NSerio against a live instance; not documented by Relativity**. The request carries the standard authorization-code parameters — `client_id`, `response_type=code`, the registered `redirect_uri`, a `scope`, and a `state` value the application checks on return; these are standard OAuth, and Relativity documents none of them. No discovery document is relied on.

**Receive the code.** The browser returns to the Redirect URL registered on the OAuth2 Client, with the authorization code and the `state` value. Check the `state` before using the code. The Redirect URL must be exactly the one registered (the variants say how).

**Exchange the code.** Send a form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=authorization_code`, the `code`, the same `redirect_uri`, the `client_id` and the `client_secret`, plus the `code_verifier` when PKCE is used. The code is exchanged at that endpoint, which Relativity documents for the Client Credentials grant only; the exchange is **verified by NSerio against a live instance; not documented by Relativity**. Send the `code_verifier` only when the authorize request carried a `code_challenge`; otherwise leave it out.

**.NET** (`HttpClient` and `System.Text.Json`):

```csharp
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

public sealed class AuthorizationCodeRequest
{
	public string Host { get; set; }
	public string AuthorizationCode { get; set; }
	public string RedirectUri { get; set; }
	public string ClientId { get; set; }
	public string ClientSecret { get; set; }

	// The PKCE verifier; null when no code_challenge was sent on the authorize request.
	public string CodeVerifier { get; set; }
}

public static class AuthorizationCodeExchange
{
	private const string _TOKEN_PATH = "/Relativity/Identity/connect/token";

	public static async Task<string> ExchangeAuthorizationCodeAsync(HttpClient client, AuthorizationCodeRequest request)
	{
		Dictionary<string, string> fields = new Dictionary<string, string>
		{
			{ "grant_type", "authorization_code" },
			{ "code", request.AuthorizationCode },
			{ "redirect_uri", request.RedirectUri },
			{ "client_id", request.ClientId },
			{ "client_secret", request.ClientSecret }
		};
		if (!string.IsNullOrEmpty(request.CodeVerifier))
		{
			fields["code_verifier"] = request.CodeVerifier;
		}

		string accessToken;
		using (FormUrlEncodedContent form = new FormUrlEncodedContent(fields))
		using (HttpResponseMessage response = await client.PostAsync($"{request.Host}{_TOKEN_PATH}", form))
		{
			response.EnsureSuccessStatusCode();
			using (JsonDocument document = JsonDocument.Parse(await response.Content.ReadAsStringAsync()))
			{
				accessToken = document.RootElement.GetProperty("access_token").GetString();
			}
		}

		return accessToken;
	}
}
```

**.NET Framework 4.7.2** (`System.Web.Extensions` and C# 7.3 syntax):

```csharp
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

public sealed class AuthorizationCodeRequest
{
	public string Host { get; set; }
	public string AuthorizationCode { get; set; }
	public string RedirectUri { get; set; }
	public string ClientId { get; set; }
	public string ClientSecret { get; set; }

	// The PKCE verifier; null when no code_challenge was sent on the authorize request.
	public string CodeVerifier { get; set; }
}

public static class AuthorizationCodeExchange
{
	private const string _TOKEN_PATH = "/Relativity/Identity/connect/token";

	public static async Task<string> ExchangeAuthorizationCodeAsync(HttpClient client, AuthorizationCodeRequest request)
	{
		Dictionary<string, string> fields = new Dictionary<string, string>
		{
			{ "grant_type", "authorization_code" },
			{ "code", request.AuthorizationCode },
			{ "redirect_uri", request.RedirectUri },
			{ "client_id", request.ClientId },
			{ "client_secret", request.ClientSecret }
		};
		if (!string.IsNullOrEmpty(request.CodeVerifier))
		{
			fields["code_verifier"] = request.CodeVerifier;
		}

		string accessToken;
		using (FormUrlEncodedContent form = new FormUrlEncodedContent(fields))
		using (HttpResponseMessage response = await client.PostAsync($"{request.Host}{_TOKEN_PATH}", form))
		{
			response.EnsureSuccessStatusCode();
			string body = await response.Content.ReadAsStringAsync();
			accessToken = new JavaScriptSerializer().Deserialize<Dictionary<string, object>>(body)["access_token"].ToString();
		}

		return accessToken;
	}
}
```

**Use the token.** Send `Authorization: Bearer <access token>` — the change at the use site in part 1 — with `X-CSRF-Header: -` on REST and Kepler calls.

**On expiry.** With Allow Refresh Tokens enabled on the OAuth2 Client and PKCE used, the application holds a refresh token and, when the access token expires, sends a `grant_type=refresh_token` request with it and the client credentials in place of the code exchange — *inferred* from the standard OAuth shape, not tested. The exchange example above reads only the `access_token` of the response; to renew with a refresh token, read the `refresh_token` from the same response as well and keep it as a secret, and otherwise the application sends the user through the sign-in again when the access token expires. Relativity does not document a refresh token's lifetime, whether it rotates, how it is revoked, or whether a revocation endpoint exists, nor what disabling or deleting the client does to tokens already issued. A refresh token is a secret: store it as Secret Management (Part B of the Migration Documentation) describes, and on compromise disable the client and replace it. Enable refresh tokens only if the application must keep a session beyond one token lifetime without asking the user to sign in again.

**PKCE.** PKCE is required by Relativity's own documentation to obtain a refresh token, and not otherwise: a Code request that does not ask for a refresh token works without it, **verified by NSerio against a live instance; not documented by Relativity**. It is never a blanket requirement. NSerio recommends PKCE with the `S256` method in both variants (NSerio recommendation), and does not claim that Relativity accepts any particular method, which it does not document.

**Scope.** No scope set is prescribed for Code. The scope below works — `openid RelativityRequestOrigin UserInfo UserInfoAccess AuthenticationID RelativityWeb` — **verified by NSerio against a live instance; not documented by Relativity**, verified as working and not as minimal. `state` is sent as standard OAuth practice and `nonce` is not used; Relativity does not document whether it enforces either.

**The application stops collecting the password.** After this change the application never receives, stores or forwards the user's username and password: the user signs in on RelativityOne's own page, and the application holds only tokens. Remove the credential prompt, the form field and any storage of the password that the replaced code needed.

**Single sign-on.** Sign-in happens on RelativityOne's own login page, so the instance's single sign-on and two-factor rules apply unchanged — *inferred* from where the sign-in happens, and not documented for clients that customers create.

**The client secret is required.** Code does not work without a client secret: tested and rejected, **verified by NSerio against a live instance; not documented by Relativity**, which confirms Relativity's own statement that the grant requires one. It is displayed once and is stored as Secret Management (Part B of the Migration Documentation) describes.

**Server-side variant.** For a web application whose code runs on a server:

- **Redirect.** The application receives the code at one exact HTTPS URL per environment, a route of the application, with no wildcards (NSerio recommendation; Relativity's matching rules are not documented). The Relativity administrator registers the exact URL on the OAuth2 Client.
- **Secret and tokens.** The client secret stays on the server and never reaches the browser. The user's tokens are held server-side, keyed to the session, and never in the browser.
- **Exchange.** The server performs the code exchange itself.

**Desktop variant.** For a desktop or command-line application run by a user on their own machine:

- **The system browser.** Open the authorize URL in the user's default browser and not in a web view the application hosts, so the application never sees the password (NSerio recommendation).
- **A loopback redirect on a fixed port.** Listen on the loopback address only, on the registered port, for one request. The Redirect URL is `http://127.0.0.1:<port>/<path>/`, ending with a slash because the listener below registers it as its prefix: a plain-HTTP loopback URL is accepted, **verified by NSerio against a live instance; not documented by Relativity**. The Relativity administrator registers that exact URL; because it is registered in advance, the port is fixed and not chosen at runtime.

```csharp
using System;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Net;
using System.Threading.Tasks;

public static class LoopbackAuthorizationReceiver
{
	private const string _CODE_PARAMETER = "code";
	private const string _STATE_PARAMETER = "state";
	private const string _ERROR_PARAMETER = "error";
	private const string _ERROR_DESCRIPTION_PARAMETER = "error_description";
	private const string _STATE_MISMATCH_MESSAGE = "The returned state is not the one that was sent; the response is rejected.";
	private const string _MISSING_CODE_MESSAGE = "The response carries no authorization code.";

	// Waits for the one request the browser sends to the registered loopback Redirect URL, and returns its code
	// only when its state is the one that was sent on the authorize request and the response carries no error.
	// redirectUrl is the exact URL registered on the OAuth2 Client, for example http://127.0.0.1:<port>/<path>/ :
	// it ends with a slash, as a listener prefix must.
	public static async Task<string> ReceiveAuthorizationCodeAsync(string authorizeUrl, string redirectUrl, string expectedState)
	{
		string authorizationCode;
		using (HttpListener listener = new HttpListener())
		{
			listener.Prefixes.Add(redirectUrl);
			listener.Start();
			Process.Start(new ProcessStartInfo(authorizeUrl) { UseShellExecute = true })?.Dispose();
			HttpListenerContext context = await listener.GetContextAsync();
			NameValueCollection query = context.Request.QueryString;
			context.Response.Close();
			if (query[_STATE_PARAMETER] != expectedState)
			{
				throw new InvalidOperationException(_STATE_MISMATCH_MESSAGE);
			}

			if (!string.IsNullOrEmpty(query[_ERROR_PARAMETER]))
			{
				throw new InvalidOperationException($"The authorization server returned an error: {query[_ERROR_PARAMETER]} {query[_ERROR_DESCRIPTION_PARAMETER]}".TrimEnd());
			}

			authorizationCode = query[_CODE_PARAMETER];
			if (string.IsNullOrEmpty(authorizationCode))
			{
				throw new InvalidOperationException(_MISSING_CODE_MESSAGE);
			}
		}

		return authorizationCode;
	}
}
```

- **The secret is extractable.** Code requires a client secret (above), so a distributed desktop application holds one, and **a secret shipped inside a distributed binary can be extracted by anyone who holds the binary**. It protects nothing against the person running the application.
- **Compensating guidance** (NSerio recommendation, subject to your own security policy): set Trusted IPs where the users' outbound addresses are known and stable — they restrict where a token can be *requested*, and are checked when a token is issued against the client's ID and secret; use the recommended short Access Token Lifetime of 60 minutes; enable refresh tokens only when needed; and create one OAuth2 Client per distribution, so a leaked secret can be disabled without affecting the others.
- **A documentation gap.** Relativity documents no recommendation for a distributed desktop application; this design is what NSerio verified works.

Verification: **Verification note — three things.**

**Confirm the library version in use offers a token authenticator.** Where it does not, this is a library upgrade before it is a code change, with whatever that upgrade carries.

**A token has a finite lifetime.** An authenticator constructed once with a fixed token string and attached to a long-lived client holds that token after it expires. Whether the library can refresh it is the library's behaviour and is not established here; where it cannot, construct the authenticator with a current token for each request or each short-lived client.

**Confirm the endpoint in use is the RelativityOne REST API.** The authenticator changes the scheme on the wire, and an endpoint elsewhere reached from the same code may accept only Basic and reject the migrated call.

OAuth2 Client: See [OAuth2 Client for Authorization Code](#oauth2-client-for-authorization-code).
<!-- generated:end CS-004 authorization-code -->

### CS-005 · AuthorizationHeaderValueScheme

<a id="cs-005-migration"></a>

#### Client Credentials

<!-- generated:begin CS-005 client-credentials -->
**Flow.** This entry is for Client Credentials. The "Which flow" paragraph of CS-005 in [Part A](part-a-csharp.md#cs-005) says when a match belongs to this flow.

Source form:
```csharp
client.DefaultRequestHeaders.Authorization =
	new AuthenticationHeaderValue(BASIC_SCHEME, encodedCredentials);
```

Under token authentication: **First, the destination:** if it is not RelativityOne, no migration to RelativityOne applies — that service's own token mechanism is the migration — and the rest of this entry does not apply.

`"Bearer"` and the access token in place of the scheme constant and the encoded pair, **set per request** on the request message rather than on the long-lived client's default headers:

```csharp
request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
```

A per-request header needs an `HttpRequestMessage` sent with `SendAsync`; the `GetAsync` and `PostAsync` convenience calls take no request message, so calls written with them change shape. The code that produced `encodedCredentials` has no counterpart: a bearer token travels verbatim.

Credentials: **Where the credentials come from.** The destination decides whether the migration applies. If the destination is not RelativityOne, the migration is that service's own token mechanism, and no migration to RelativityOne applies. If it is RelativityOne, the token comes from the token-acquisition part that follows.

**Where the token comes from.** Create an OAuth2 Client with Flow Grant Type set to Client Credentials (part 3 lists its settings), then send a form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=client_credentials`, `client_id`, `client_secret` and a `scope`. The token endpoint is **documented** for this grant. The token is then sent as `Authorization: Bearer <token>`, with `X-CSRF-Header: -` on REST and Kepler calls — the header is documented as required, and a Kepler call without it returns 404.

**.NET** (`HttpClient` and `System.Text.Json`). The class requests the token with the client ID and the client secret, caches it, requests a new one 60 seconds before it expires, and, when a call is rejected for authentication, requests a new one and sends the call once more. Build one instance for the life of the process and send every call through `SendAsync`; the request is created by the delegate on each attempt, because a request message cannot be sent twice. The client ID and the client secret come from the process environment or a secret store, never from source. Where the code uses the Relativity SDK, `GetAccessTokenAsync(false)` gives the token for `BearerTokenCredentials` each time a `ServiceFactory` is built.

```csharp
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

public sealed class AccessTokenProvider : IDisposable
{
	private const string _TOKEN_PATH = "/Relativity/Identity/connect/token";
	private const string _BEARER_SCHEME = "Bearer";
	private const string _CSRF_HEADER_NAME = "X-CSRF-Header";
	private const string _CSRF_HEADER_VALUE = "-";
	private static readonly TimeSpan _renewalMargin = TimeSpan.FromSeconds(60);

	private readonly HttpClient _client = new HttpClient();
	private readonly SemaphoreSlim _gate = new SemaphoreSlim(1, 1);
	private readonly string _host;
	private readonly string _clientId;
	private readonly string _clientSecret;
	private readonly string _scope;
	private string _accessToken;
	private DateTimeOffset _expiresAt;

	public AccessTokenProvider(string host, string clientId, string clientSecret, string scope)
	{
		_host = host;
		_clientId = clientId;
		_clientSecret = clientSecret;
		_scope = scope;
	}

	// Cached token; renewed shortly before it expires, or on demand after a rejected call.
	public async Task<string> GetAccessTokenAsync(bool forceRenewal)
	{
		await _gate.WaitAsync();
		try
		{
			if (forceRenewal || _accessToken == null || DateTimeOffset.UtcNow >= _expiresAt - _renewalMargin)
			{
				await AcquireAccessTokenAsync();
			}

			return _accessToken;
		}
		finally
		{
			_gate.Release();
		}
	}

	// Sends the request with the bearer token; when the call is rejected for authentication, renews the token and sends it once more.
	public async Task<HttpResponseMessage> SendAsync(Func<HttpRequestMessage> createRequest)
	{
		HttpResponseMessage response = await SendOnceAsync(createRequest, false);
		if (response.StatusCode == HttpStatusCode.Unauthorized)
		{
			response.Dispose();
			response = await SendOnceAsync(createRequest, true);
		}

		return response;
	}

	public void Dispose()
	{
		_client.Dispose();
		_gate.Dispose();
	}

	private async Task AcquireAccessTokenAsync()
	{
		using (FormUrlEncodedContent form = new FormUrlEncodedContent(new Dictionary<string, string>
		{
			{ "grant_type", "client_credentials" },
			{ "client_id", _clientId },
			{ "client_secret", _clientSecret },
			{ "scope", _scope }
		}))
		using (HttpResponseMessage response = await _client.PostAsync($"{_host}{_TOKEN_PATH}", form))
		{
			response.EnsureSuccessStatusCode();
			using (JsonDocument document = JsonDocument.Parse(await response.Content.ReadAsStringAsync()))
			{
				_accessToken = document.RootElement.GetProperty("access_token").GetString();
				_expiresAt = DateTimeOffset.UtcNow.AddSeconds(document.RootElement.GetProperty("expires_in").GetInt32());
			}
		}
	}

	private async Task<HttpResponseMessage> SendOnceAsync(Func<HttpRequestMessage> createRequest, bool forceRenewal)
	{
		string token = await GetAccessTokenAsync(forceRenewal);
		HttpResponseMessage response;
		using (HttpRequestMessage request = createRequest())
		{
			request.Headers.Authorization = new AuthenticationHeaderValue(_BEARER_SCHEME, token);
			request.Headers.Add(_CSRF_HEADER_NAME, _CSRF_HEADER_VALUE);
			response = await _client.SendAsync(request);
		}

		return response;
	}
}
```

**.NET Framework 4.7.2**: the same class with the serializer the runtime ships (`System.Web.Extensions`, referenced together with `System.Net.Http`), and C# 7.3 syntax:

```csharp
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

public sealed class AccessTokenProvider : IDisposable
{
	private const string _TOKEN_PATH = "/Relativity/Identity/connect/token";
	private const string _BEARER_SCHEME = "Bearer";
	private const string _CSRF_HEADER_NAME = "X-CSRF-Header";
	private const string _CSRF_HEADER_VALUE = "-";
	private static readonly TimeSpan _renewalMargin = TimeSpan.FromSeconds(60);

	private readonly HttpClient _client = new HttpClient();
	private readonly SemaphoreSlim _gate = new SemaphoreSlim(1, 1);
	private readonly string _host;
	private readonly string _clientId;
	private readonly string _clientSecret;
	private readonly string _scope;
	private string _accessToken;
	private DateTimeOffset _expiresAt;

	public AccessTokenProvider(string host, string clientId, string clientSecret, string scope)
	{
		_host = host;
		_clientId = clientId;
		_clientSecret = clientSecret;
		_scope = scope;
	}

	// Cached token; renewed shortly before it expires, or on demand after a rejected call.
	public async Task<string> GetAccessTokenAsync(bool forceRenewal)
	{
		await _gate.WaitAsync();
		try
		{
			if (forceRenewal || _accessToken == null || DateTimeOffset.UtcNow >= _expiresAt - _renewalMargin)
			{
				await AcquireAccessTokenAsync();
			}

			return _accessToken;
		}
		finally
		{
			_gate.Release();
		}
	}

	// Sends the request with the bearer token; when the call is rejected for authentication, renews the token and sends it once more.
	public async Task<HttpResponseMessage> SendAsync(Func<HttpRequestMessage> createRequest)
	{
		HttpResponseMessage response = await SendOnceAsync(createRequest, false);
		if (response.StatusCode == HttpStatusCode.Unauthorized)
		{
			response.Dispose();
			response = await SendOnceAsync(createRequest, true);
		}

		return response;
	}

	public void Dispose()
	{
		_client.Dispose();
		_gate.Dispose();
	}

	private async Task AcquireAccessTokenAsync()
	{
		using (FormUrlEncodedContent form = new FormUrlEncodedContent(new Dictionary<string, string>
		{
			{ "grant_type", "client_credentials" },
			{ "client_id", _clientId },
			{ "client_secret", _clientSecret },
			{ "scope", _scope }
		}))
		using (HttpResponseMessage response = await _client.PostAsync($"{_host}{_TOKEN_PATH}", form))
		{
			response.EnsureSuccessStatusCode();
			string body = await response.Content.ReadAsStringAsync();
			Dictionary<string, object> fields = new JavaScriptSerializer().Deserialize<Dictionary<string, object>>(body);
			_accessToken = fields["access_token"].ToString();
			_expiresAt = DateTimeOffset.UtcNow.AddSeconds(Convert.ToInt32(fields["expires_in"]));
		}
	}

	private async Task<HttpResponseMessage> SendOnceAsync(Func<HttpRequestMessage> createRequest, bool forceRenewal)
	{
		string token = await GetAccessTokenAsync(forceRenewal);
		HttpResponseMessage response;
		using (HttpRequestMessage request = createRequest())
		{
			request.Headers.Authorization = new AuthenticationHeaderValue(_BEARER_SCHEME, token);
			request.Headers.Add(_CSRF_HEADER_NAME, _CSRF_HEADER_VALUE);
			response = await _client.SendAsync(request);
		}

		return response;
	}
}
```

**The scope depends on what the integration needs, and no single scope is required.** Relativity's documentation shows two values for this grant: `SystemUserInfo` on the REST authentication page, the Usage Reports page and in the official Transfer SDK sample, and `UserInfoAccess` in the Kepler .NET samples. No page says which is correct, whether both work, or what either grants, and the OAuth2 Client's `Scopes` property is documented as one that cannot be set. Use the value the API you call documents, and confirm it.

**Cache the token, and re-acquire it on expiry — do not refresh.** Keep the token for its lifetime (part 3 recommends 60 minutes, Relativity's own wording) and request a new one with the client secret shortly before it expires (the JavaScript, TypeScript and Python examples use 60 seconds), and once when a call is rejected for authentication. Client Credentials does not offer refresh tokens: **verified by NSerio against a live instance; not documented by Relativity** — the Allow Refresh Tokens control does not appear for this grant in the RelativityOne user interface. Relativity's own tables and text on refresh tokens conflict with each other, so re-acquisition with the secret is the advice that holds whichever way they are read.

**Long-running processes that use the Relativity SDK.** A `ServiceFactory` built once from a bearer token keeps that token after it expires. Rebuild the `ServiceFactory` and its proxies with a fresh token before expiry. This is the only advice consistent with the documentation: the package `Relativity.Kepler.Client.SDK` (documented) exposes a settable `Token` and an `HttpMessageHandler` overload but no token-provider overload, and no page documents renewing a token behind a factory.

**The client secret.** It is displayed once, at creation or reset. Store it as Secret Management (Part B of the Migration Documentation) describes; never in source, never in tracked configuration. A reset invalidates the previous secret immediately, so plan the cut-over before resetting it.

**Before deploying, confirm the calling identity.** Every call now runs as the OAuth2 Client's **Context User** — not as the user in the replaced credential, and for an import or a job as much as for a request. If that user has different workspace permissions or item-level security, the code returns fewer rows or `Unauthorized` in production with no change to its logic, and Relativity's audit attributes every call to that one account. Confirm the Context User's permissions match those of the previous identity. Relativity's REST API authentication page describes the Context User as usually a member of the Relativity Administrators group; the OAuth2 Client block (part 3) gives NSerio's recommendation, which differs.

**When the integration acts for a signed-in user.** Client Credentials is one service identity by construction. An integration that acts on behalf of a signed-in user is the Authorization Code case, and moving to it means a new OAuth2 Client, because the flow is fixed when a client is created. Which of the two flows applies depends on who supplies the credential at run time.

Verification: **Verification note — three things.**

**Moving the header from the client to the request is part of the migration, not a style choice.** A token has a finite lifetime, and a header set once on `DefaultRequestHeaders` cannot be refreshed without mutating shared state; the code works for the token's lifetime and then returns 401 until the process restarts.

**The pair's encoding may be a separate match.** Where `encodedCredentials` is built in the same file, the encoding and this header are two matches at two locations. They are one migration, and both locations change together.

**Confirm the endpoint in use is the RelativityOne REST API.** That is the API whose bearer token this guidance describes; an endpoint elsewhere reached from the same code may accept only Basic and reject the migrated call.

OAuth2 Client: See [OAuth2 Client for Client Credentials](#oauth2-client-for-client-credentials).
<!-- generated:end CS-005 client-credentials -->

#### Authorization Code

<!-- generated:begin CS-005 authorization-code -->
**Flow.** This entry is for Authorization Code. The "Which flow" paragraph of CS-005 in [Part A](part-a-csharp.md#cs-005) says when a match belongs to this flow.

Source form:
```csharp
client.DefaultRequestHeaders.Authorization =
	new AuthenticationHeaderValue(BASIC_SCHEME, encodedCredentials);
```

Under token authentication: **First, the destination:** if it is not RelativityOne, no migration to RelativityOne applies — that service's own token mechanism is the migration — and the rest of this entry does not apply.

`"Bearer"` and the access token in place of the scheme constant and the encoded pair, **set per request** on the request message rather than on the long-lived client's default headers:

```csharp
request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
```

A per-request header needs an `HttpRequestMessage` sent with `SendAsync`; the `GetAsync` and `PostAsync` convenience calls take no request message, so calls written with them change shape. The code that produced `encodedCredentials` has no counterpart: a bearer token travels verbatim.

Credentials: **Where the credentials come from.** The destination decides whether the migration applies. If the destination is not RelativityOne, the migration is that service's own token mechanism, and no migration to RelativityOne applies. If it is RelativityOne, the token comes from the token-acquisition part that follows.

**Common to both variants**

**Sign in.** Replace the code that collected the user's credentials with a redirect: send the user's browser to the authorize endpoint `<host>/Relativity/Identity/connect/authorize`, which leads to the RelativityOne login page. The authorize endpoint is **verified by NSerio against a live instance; not documented by Relativity**. The request carries the standard authorization-code parameters — `client_id`, `response_type=code`, the registered `redirect_uri`, a `scope`, and a `state` value the application checks on return; these are standard OAuth, and Relativity documents none of them. No discovery document is relied on.

**Receive the code.** The browser returns to the Redirect URL registered on the OAuth2 Client, with the authorization code and the `state` value. Check the `state` before using the code. The Redirect URL must be exactly the one registered (the variants say how).

**Exchange the code.** Send a form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=authorization_code`, the `code`, the same `redirect_uri`, the `client_id` and the `client_secret`, plus the `code_verifier` when PKCE is used. The code is exchanged at that endpoint, which Relativity documents for the Client Credentials grant only; the exchange is **verified by NSerio against a live instance; not documented by Relativity**. Send the `code_verifier` only when the authorize request carried a `code_challenge`; otherwise leave it out.

**.NET** (`HttpClient` and `System.Text.Json`):

```csharp
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

public sealed class AuthorizationCodeRequest
{
	public string Host { get; set; }
	public string AuthorizationCode { get; set; }
	public string RedirectUri { get; set; }
	public string ClientId { get; set; }
	public string ClientSecret { get; set; }

	// The PKCE verifier; null when no code_challenge was sent on the authorize request.
	public string CodeVerifier { get; set; }
}

public static class AuthorizationCodeExchange
{
	private const string _TOKEN_PATH = "/Relativity/Identity/connect/token";

	public static async Task<string> ExchangeAuthorizationCodeAsync(HttpClient client, AuthorizationCodeRequest request)
	{
		Dictionary<string, string> fields = new Dictionary<string, string>
		{
			{ "grant_type", "authorization_code" },
			{ "code", request.AuthorizationCode },
			{ "redirect_uri", request.RedirectUri },
			{ "client_id", request.ClientId },
			{ "client_secret", request.ClientSecret }
		};
		if (!string.IsNullOrEmpty(request.CodeVerifier))
		{
			fields["code_verifier"] = request.CodeVerifier;
		}

		string accessToken;
		using (FormUrlEncodedContent form = new FormUrlEncodedContent(fields))
		using (HttpResponseMessage response = await client.PostAsync($"{request.Host}{_TOKEN_PATH}", form))
		{
			response.EnsureSuccessStatusCode();
			using (JsonDocument document = JsonDocument.Parse(await response.Content.ReadAsStringAsync()))
			{
				accessToken = document.RootElement.GetProperty("access_token").GetString();
			}
		}

		return accessToken;
	}
}
```

**.NET Framework 4.7.2** (`System.Web.Extensions` and C# 7.3 syntax):

```csharp
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

public sealed class AuthorizationCodeRequest
{
	public string Host { get; set; }
	public string AuthorizationCode { get; set; }
	public string RedirectUri { get; set; }
	public string ClientId { get; set; }
	public string ClientSecret { get; set; }

	// The PKCE verifier; null when no code_challenge was sent on the authorize request.
	public string CodeVerifier { get; set; }
}

public static class AuthorizationCodeExchange
{
	private const string _TOKEN_PATH = "/Relativity/Identity/connect/token";

	public static async Task<string> ExchangeAuthorizationCodeAsync(HttpClient client, AuthorizationCodeRequest request)
	{
		Dictionary<string, string> fields = new Dictionary<string, string>
		{
			{ "grant_type", "authorization_code" },
			{ "code", request.AuthorizationCode },
			{ "redirect_uri", request.RedirectUri },
			{ "client_id", request.ClientId },
			{ "client_secret", request.ClientSecret }
		};
		if (!string.IsNullOrEmpty(request.CodeVerifier))
		{
			fields["code_verifier"] = request.CodeVerifier;
		}

		string accessToken;
		using (FormUrlEncodedContent form = new FormUrlEncodedContent(fields))
		using (HttpResponseMessage response = await client.PostAsync($"{request.Host}{_TOKEN_PATH}", form))
		{
			response.EnsureSuccessStatusCode();
			string body = await response.Content.ReadAsStringAsync();
			accessToken = new JavaScriptSerializer().Deserialize<Dictionary<string, object>>(body)["access_token"].ToString();
		}

		return accessToken;
	}
}
```

**Use the token.** Send `Authorization: Bearer <access token>` — the change at the use site in part 1 — with `X-CSRF-Header: -` on REST and Kepler calls.

**On expiry.** With Allow Refresh Tokens enabled on the OAuth2 Client and PKCE used, the application holds a refresh token and, when the access token expires, sends a `grant_type=refresh_token` request with it and the client credentials in place of the code exchange — *inferred* from the standard OAuth shape, not tested. The exchange example above reads only the `access_token` of the response; to renew with a refresh token, read the `refresh_token` from the same response as well and keep it as a secret, and otherwise the application sends the user through the sign-in again when the access token expires. Relativity does not document a refresh token's lifetime, whether it rotates, how it is revoked, or whether a revocation endpoint exists, nor what disabling or deleting the client does to tokens already issued. A refresh token is a secret: store it as Secret Management (Part B of the Migration Documentation) describes, and on compromise disable the client and replace it. Enable refresh tokens only if the application must keep a session beyond one token lifetime without asking the user to sign in again.

**PKCE.** PKCE is required by Relativity's own documentation to obtain a refresh token, and not otherwise: a Code request that does not ask for a refresh token works without it, **verified by NSerio against a live instance; not documented by Relativity**. It is never a blanket requirement. NSerio recommends PKCE with the `S256` method in both variants (NSerio recommendation), and does not claim that Relativity accepts any particular method, which it does not document.

**Scope.** No scope set is prescribed for Code. The scope below works — `openid RelativityRequestOrigin UserInfo UserInfoAccess AuthenticationID RelativityWeb` — **verified by NSerio against a live instance; not documented by Relativity**, verified as working and not as minimal. `state` is sent as standard OAuth practice and `nonce` is not used; Relativity does not document whether it enforces either.

**The application stops collecting the password.** After this change the application never receives, stores or forwards the user's username and password: the user signs in on RelativityOne's own page, and the application holds only tokens. Remove the credential prompt, the form field and any storage of the password that the replaced code needed.

**Single sign-on.** Sign-in happens on RelativityOne's own login page, so the instance's single sign-on and two-factor rules apply unchanged — *inferred* from where the sign-in happens, and not documented for clients that customers create.

**The client secret is required.** Code does not work without a client secret: tested and rejected, **verified by NSerio against a live instance; not documented by Relativity**, which confirms Relativity's own statement that the grant requires one. It is displayed once and is stored as Secret Management (Part B of the Migration Documentation) describes.

**Server-side variant.** For a web application whose code runs on a server:

- **Redirect.** The application receives the code at one exact HTTPS URL per environment, a route of the application, with no wildcards (NSerio recommendation; Relativity's matching rules are not documented). The Relativity administrator registers the exact URL on the OAuth2 Client.
- **Secret and tokens.** The client secret stays on the server and never reaches the browser. The user's tokens are held server-side, keyed to the session, and never in the browser.
- **Exchange.** The server performs the code exchange itself.

**Desktop variant.** For a desktop or command-line application run by a user on their own machine:

- **The system browser.** Open the authorize URL in the user's default browser and not in a web view the application hosts, so the application never sees the password (NSerio recommendation).
- **A loopback redirect on a fixed port.** Listen on the loopback address only, on the registered port, for one request. The Redirect URL is `http://127.0.0.1:<port>/<path>/`, ending with a slash because the listener below registers it as its prefix: a plain-HTTP loopback URL is accepted, **verified by NSerio against a live instance; not documented by Relativity**. The Relativity administrator registers that exact URL; because it is registered in advance, the port is fixed and not chosen at runtime.

```csharp
using System;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Net;
using System.Threading.Tasks;

public static class LoopbackAuthorizationReceiver
{
	private const string _CODE_PARAMETER = "code";
	private const string _STATE_PARAMETER = "state";
	private const string _ERROR_PARAMETER = "error";
	private const string _ERROR_DESCRIPTION_PARAMETER = "error_description";
	private const string _STATE_MISMATCH_MESSAGE = "The returned state is not the one that was sent; the response is rejected.";
	private const string _MISSING_CODE_MESSAGE = "The response carries no authorization code.";

	// Waits for the one request the browser sends to the registered loopback Redirect URL, and returns its code
	// only when its state is the one that was sent on the authorize request and the response carries no error.
	// redirectUrl is the exact URL registered on the OAuth2 Client, for example http://127.0.0.1:<port>/<path>/ :
	// it ends with a slash, as a listener prefix must.
	public static async Task<string> ReceiveAuthorizationCodeAsync(string authorizeUrl, string redirectUrl, string expectedState)
	{
		string authorizationCode;
		using (HttpListener listener = new HttpListener())
		{
			listener.Prefixes.Add(redirectUrl);
			listener.Start();
			Process.Start(new ProcessStartInfo(authorizeUrl) { UseShellExecute = true })?.Dispose();
			HttpListenerContext context = await listener.GetContextAsync();
			NameValueCollection query = context.Request.QueryString;
			context.Response.Close();
			if (query[_STATE_PARAMETER] != expectedState)
			{
				throw new InvalidOperationException(_STATE_MISMATCH_MESSAGE);
			}

			if (!string.IsNullOrEmpty(query[_ERROR_PARAMETER]))
			{
				throw new InvalidOperationException($"The authorization server returned an error: {query[_ERROR_PARAMETER]} {query[_ERROR_DESCRIPTION_PARAMETER]}".TrimEnd());
			}

			authorizationCode = query[_CODE_PARAMETER];
			if (string.IsNullOrEmpty(authorizationCode))
			{
				throw new InvalidOperationException(_MISSING_CODE_MESSAGE);
			}
		}

		return authorizationCode;
	}
}
```

- **The secret is extractable.** Code requires a client secret (above), so a distributed desktop application holds one, and **a secret shipped inside a distributed binary can be extracted by anyone who holds the binary**. It protects nothing against the person running the application.
- **Compensating guidance** (NSerio recommendation, subject to your own security policy): set Trusted IPs where the users' outbound addresses are known and stable — they restrict where a token can be *requested*, and are checked when a token is issued against the client's ID and secret; use the recommended short Access Token Lifetime of 60 minutes; enable refresh tokens only when needed; and create one OAuth2 Client per distribution, so a leaked secret can be disabled without affecting the others.
- **A documentation gap.** Relativity documents no recommendation for a distributed desktop application; this design is what NSerio verified works.

Verification: **Verification note — three things.**

**Moving the header from the client to the request is part of the migration, not a style choice.** A token has a finite lifetime, and a header set once on `DefaultRequestHeaders` cannot be refreshed without mutating shared state; the code works for the token's lifetime and then returns 401 until the process restarts.

**The pair's encoding may be a separate match.** Where `encodedCredentials` is built in the same file, the encoding and this header are two matches at two locations. They are one migration, and both locations change together.

**Confirm the endpoint in use is the RelativityOne REST API.** That is the API whose bearer token this guidance describes; an endpoint elsewhere reached from the same code may accept only Basic and reject the migrated call.

OAuth2 Client: See [OAuth2 Client for Authorization Code](#oauth2-client-for-authorization-code).
<!-- generated:end CS-005 authorization-code -->

### CS-006 · AuthorizationHeaderStringLiteral

<a id="cs-006-migration"></a>

#### Client Credentials

<!-- generated:begin CS-006 client-credentials -->
**Flow.** This entry is for Client Credentials. The "Which flow" paragraph of CS-006 in [Part A](part-a-csharp.md#cs-006) says when a match belongs to this flow.

Source form:
```csharp
client.DefaultRequestHeaders.Add("Authorization", $"Basic {encodedCredentials}");
```

Under token authentication: **First, the destination:** if it is not RelativityOne, no migration to RelativityOne applies — that service's own token mechanism is the migration — and the rest of this entry does not apply.

`Bearer` and the access token in place of `Basic` and the encoded pair, **set per request** rather than on the long-lived client's default headers:

```csharp
request.Headers.Add("Authorization", $"Bearer {accessToken}");
```

A per-request header needs an `HttpRequestMessage` sent with `SendAsync`; the `GetAsync` and `PostAsync` convenience calls take no request message, so calls written with them change shape. The code that produced `encodedCredentials` has no counterpart: a bearer token travels verbatim.

Credentials: **Where the credentials come from.** The destination decides whether the migration applies. If the destination is not RelativityOne, the migration is that service's own token mechanism, and no migration to RelativityOne applies. If it is RelativityOne, the token comes from the token-acquisition part that follows.

**Where the token comes from.** Create an OAuth2 Client with Flow Grant Type set to Client Credentials (part 3 lists its settings), then send a form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=client_credentials`, `client_id`, `client_secret` and a `scope`. The token endpoint is **documented** for this grant. The token is then sent as `Authorization: Bearer <token>`, with `X-CSRF-Header: -` on REST and Kepler calls — the header is documented as required, and a Kepler call without it returns 404.

**.NET** (`HttpClient` and `System.Text.Json`). The class requests the token with the client ID and the client secret, caches it, requests a new one 60 seconds before it expires, and, when a call is rejected for authentication, requests a new one and sends the call once more. Build one instance for the life of the process and send every call through `SendAsync`; the request is created by the delegate on each attempt, because a request message cannot be sent twice. The client ID and the client secret come from the process environment or a secret store, never from source. Where the code uses the Relativity SDK, `GetAccessTokenAsync(false)` gives the token for `BearerTokenCredentials` each time a `ServiceFactory` is built.

```csharp
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

public sealed class AccessTokenProvider : IDisposable
{
	private const string _TOKEN_PATH = "/Relativity/Identity/connect/token";
	private const string _BEARER_SCHEME = "Bearer";
	private const string _CSRF_HEADER_NAME = "X-CSRF-Header";
	private const string _CSRF_HEADER_VALUE = "-";
	private static readonly TimeSpan _renewalMargin = TimeSpan.FromSeconds(60);

	private readonly HttpClient _client = new HttpClient();
	private readonly SemaphoreSlim _gate = new SemaphoreSlim(1, 1);
	private readonly string _host;
	private readonly string _clientId;
	private readonly string _clientSecret;
	private readonly string _scope;
	private string _accessToken;
	private DateTimeOffset _expiresAt;

	public AccessTokenProvider(string host, string clientId, string clientSecret, string scope)
	{
		_host = host;
		_clientId = clientId;
		_clientSecret = clientSecret;
		_scope = scope;
	}

	// Cached token; renewed shortly before it expires, or on demand after a rejected call.
	public async Task<string> GetAccessTokenAsync(bool forceRenewal)
	{
		await _gate.WaitAsync();
		try
		{
			if (forceRenewal || _accessToken == null || DateTimeOffset.UtcNow >= _expiresAt - _renewalMargin)
			{
				await AcquireAccessTokenAsync();
			}

			return _accessToken;
		}
		finally
		{
			_gate.Release();
		}
	}

	// Sends the request with the bearer token; when the call is rejected for authentication, renews the token and sends it once more.
	public async Task<HttpResponseMessage> SendAsync(Func<HttpRequestMessage> createRequest)
	{
		HttpResponseMessage response = await SendOnceAsync(createRequest, false);
		if (response.StatusCode == HttpStatusCode.Unauthorized)
		{
			response.Dispose();
			response = await SendOnceAsync(createRequest, true);
		}

		return response;
	}

	public void Dispose()
	{
		_client.Dispose();
		_gate.Dispose();
	}

	private async Task AcquireAccessTokenAsync()
	{
		using (FormUrlEncodedContent form = new FormUrlEncodedContent(new Dictionary<string, string>
		{
			{ "grant_type", "client_credentials" },
			{ "client_id", _clientId },
			{ "client_secret", _clientSecret },
			{ "scope", _scope }
		}))
		using (HttpResponseMessage response = await _client.PostAsync($"{_host}{_TOKEN_PATH}", form))
		{
			response.EnsureSuccessStatusCode();
			using (JsonDocument document = JsonDocument.Parse(await response.Content.ReadAsStringAsync()))
			{
				_accessToken = document.RootElement.GetProperty("access_token").GetString();
				_expiresAt = DateTimeOffset.UtcNow.AddSeconds(document.RootElement.GetProperty("expires_in").GetInt32());
			}
		}
	}

	private async Task<HttpResponseMessage> SendOnceAsync(Func<HttpRequestMessage> createRequest, bool forceRenewal)
	{
		string token = await GetAccessTokenAsync(forceRenewal);
		HttpResponseMessage response;
		using (HttpRequestMessage request = createRequest())
		{
			request.Headers.Authorization = new AuthenticationHeaderValue(_BEARER_SCHEME, token);
			request.Headers.Add(_CSRF_HEADER_NAME, _CSRF_HEADER_VALUE);
			response = await _client.SendAsync(request);
		}

		return response;
	}
}
```

**.NET Framework 4.7.2**: the same class with the serializer the runtime ships (`System.Web.Extensions`, referenced together with `System.Net.Http`), and C# 7.3 syntax:

```csharp
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

public sealed class AccessTokenProvider : IDisposable
{
	private const string _TOKEN_PATH = "/Relativity/Identity/connect/token";
	private const string _BEARER_SCHEME = "Bearer";
	private const string _CSRF_HEADER_NAME = "X-CSRF-Header";
	private const string _CSRF_HEADER_VALUE = "-";
	private static readonly TimeSpan _renewalMargin = TimeSpan.FromSeconds(60);

	private readonly HttpClient _client = new HttpClient();
	private readonly SemaphoreSlim _gate = new SemaphoreSlim(1, 1);
	private readonly string _host;
	private readonly string _clientId;
	private readonly string _clientSecret;
	private readonly string _scope;
	private string _accessToken;
	private DateTimeOffset _expiresAt;

	public AccessTokenProvider(string host, string clientId, string clientSecret, string scope)
	{
		_host = host;
		_clientId = clientId;
		_clientSecret = clientSecret;
		_scope = scope;
	}

	// Cached token; renewed shortly before it expires, or on demand after a rejected call.
	public async Task<string> GetAccessTokenAsync(bool forceRenewal)
	{
		await _gate.WaitAsync();
		try
		{
			if (forceRenewal || _accessToken == null || DateTimeOffset.UtcNow >= _expiresAt - _renewalMargin)
			{
				await AcquireAccessTokenAsync();
			}

			return _accessToken;
		}
		finally
		{
			_gate.Release();
		}
	}

	// Sends the request with the bearer token; when the call is rejected for authentication, renews the token and sends it once more.
	public async Task<HttpResponseMessage> SendAsync(Func<HttpRequestMessage> createRequest)
	{
		HttpResponseMessage response = await SendOnceAsync(createRequest, false);
		if (response.StatusCode == HttpStatusCode.Unauthorized)
		{
			response.Dispose();
			response = await SendOnceAsync(createRequest, true);
		}

		return response;
	}

	public void Dispose()
	{
		_client.Dispose();
		_gate.Dispose();
	}

	private async Task AcquireAccessTokenAsync()
	{
		using (FormUrlEncodedContent form = new FormUrlEncodedContent(new Dictionary<string, string>
		{
			{ "grant_type", "client_credentials" },
			{ "client_id", _clientId },
			{ "client_secret", _clientSecret },
			{ "scope", _scope }
		}))
		using (HttpResponseMessage response = await _client.PostAsync($"{_host}{_TOKEN_PATH}", form))
		{
			response.EnsureSuccessStatusCode();
			string body = await response.Content.ReadAsStringAsync();
			Dictionary<string, object> fields = new JavaScriptSerializer().Deserialize<Dictionary<string, object>>(body);
			_accessToken = fields["access_token"].ToString();
			_expiresAt = DateTimeOffset.UtcNow.AddSeconds(Convert.ToInt32(fields["expires_in"]));
		}
	}

	private async Task<HttpResponseMessage> SendOnceAsync(Func<HttpRequestMessage> createRequest, bool forceRenewal)
	{
		string token = await GetAccessTokenAsync(forceRenewal);
		HttpResponseMessage response;
		using (HttpRequestMessage request = createRequest())
		{
			request.Headers.Authorization = new AuthenticationHeaderValue(_BEARER_SCHEME, token);
			request.Headers.Add(_CSRF_HEADER_NAME, _CSRF_HEADER_VALUE);
			response = await _client.SendAsync(request);
		}

		return response;
	}
}
```

**The scope depends on what the integration needs, and no single scope is required.** Relativity's documentation shows two values for this grant: `SystemUserInfo` on the REST authentication page, the Usage Reports page and in the official Transfer SDK sample, and `UserInfoAccess` in the Kepler .NET samples. No page says which is correct, whether both work, or what either grants, and the OAuth2 Client's `Scopes` property is documented as one that cannot be set. Use the value the API you call documents, and confirm it.

**Cache the token, and re-acquire it on expiry — do not refresh.** Keep the token for its lifetime (part 3 recommends 60 minutes, Relativity's own wording) and request a new one with the client secret shortly before it expires (the JavaScript, TypeScript and Python examples use 60 seconds), and once when a call is rejected for authentication. Client Credentials does not offer refresh tokens: **verified by NSerio against a live instance; not documented by Relativity** — the Allow Refresh Tokens control does not appear for this grant in the RelativityOne user interface. Relativity's own tables and text on refresh tokens conflict with each other, so re-acquisition with the secret is the advice that holds whichever way they are read.

**Long-running processes that use the Relativity SDK.** A `ServiceFactory` built once from a bearer token keeps that token after it expires. Rebuild the `ServiceFactory` and its proxies with a fresh token before expiry. This is the only advice consistent with the documentation: the package `Relativity.Kepler.Client.SDK` (documented) exposes a settable `Token` and an `HttpMessageHandler` overload but no token-provider overload, and no page documents renewing a token behind a factory.

**The client secret.** It is displayed once, at creation or reset. Store it as Secret Management (Part B of the Migration Documentation) describes; never in source, never in tracked configuration. A reset invalidates the previous secret immediately, so plan the cut-over before resetting it.

**Before deploying, confirm the calling identity.** Every call now runs as the OAuth2 Client's **Context User** — not as the user in the replaced credential, and for an import or a job as much as for a request. If that user has different workspace permissions or item-level security, the code returns fewer rows or `Unauthorized` in production with no change to its logic, and Relativity's audit attributes every call to that one account. Confirm the Context User's permissions match those of the previous identity. Relativity's REST API authentication page describes the Context User as usually a member of the Relativity Administrators group; the OAuth2 Client block (part 3) gives NSerio's recommendation, which differs.

**When the integration acts for a signed-in user.** Client Credentials is one service identity by construction. An integration that acts on behalf of a signed-in user is the Authorization Code case, and moving to it means a new OAuth2 Client, because the flow is fixed when a client is created. Which of the two flows applies depends on who supplies the credential at run time.

Verification: **Verification note — three things.**

**Moving the header from the client to the request is part of the migration, not a style choice.** A token has a finite lifetime, and a header added once to `DefaultRequestHeaders` cannot be refreshed without mutating shared state; the code works for the token's lifetime and then returns 401 until the process restarts.

**The pair's encoding may be a separate match.** Where `encodedCredentials` is built in the same file, the encoding and this header are two matches at two locations. They are one migration, and both locations change together.

**Confirm the endpoint in use is the RelativityOne REST API.** That is the API whose bearer token this guidance describes; an endpoint elsewhere reached from the same code may accept only Basic and reject the migrated call.

OAuth2 Client: See [OAuth2 Client for Client Credentials](#oauth2-client-for-client-credentials).
<!-- generated:end CS-006 client-credentials -->

#### Authorization Code

<!-- generated:begin CS-006 authorization-code -->
**Flow.** This entry is for Authorization Code. The "Which flow" paragraph of CS-006 in [Part A](part-a-csharp.md#cs-006) says when a match belongs to this flow.

Source form:
```csharp
client.DefaultRequestHeaders.Add("Authorization", $"Basic {encodedCredentials}");
```

Under token authentication: **First, the destination:** if it is not RelativityOne, no migration to RelativityOne applies — that service's own token mechanism is the migration — and the rest of this entry does not apply.

`Bearer` and the access token in place of `Basic` and the encoded pair, **set per request** rather than on the long-lived client's default headers:

```csharp
request.Headers.Add("Authorization", $"Bearer {accessToken}");
```

A per-request header needs an `HttpRequestMessage` sent with `SendAsync`; the `GetAsync` and `PostAsync` convenience calls take no request message, so calls written with them change shape. The code that produced `encodedCredentials` has no counterpart: a bearer token travels verbatim.

Credentials: **Where the credentials come from.** The destination decides whether the migration applies. If the destination is not RelativityOne, the migration is that service's own token mechanism, and no migration to RelativityOne applies. If it is RelativityOne, the token comes from the token-acquisition part that follows.

**Common to both variants**

**Sign in.** Replace the code that collected the user's credentials with a redirect: send the user's browser to the authorize endpoint `<host>/Relativity/Identity/connect/authorize`, which leads to the RelativityOne login page. The authorize endpoint is **verified by NSerio against a live instance; not documented by Relativity**. The request carries the standard authorization-code parameters — `client_id`, `response_type=code`, the registered `redirect_uri`, a `scope`, and a `state` value the application checks on return; these are standard OAuth, and Relativity documents none of them. No discovery document is relied on.

**Receive the code.** The browser returns to the Redirect URL registered on the OAuth2 Client, with the authorization code and the `state` value. Check the `state` before using the code. The Redirect URL must be exactly the one registered (the variants say how).

**Exchange the code.** Send a form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=authorization_code`, the `code`, the same `redirect_uri`, the `client_id` and the `client_secret`, plus the `code_verifier` when PKCE is used. The code is exchanged at that endpoint, which Relativity documents for the Client Credentials grant only; the exchange is **verified by NSerio against a live instance; not documented by Relativity**. Send the `code_verifier` only when the authorize request carried a `code_challenge`; otherwise leave it out.

**.NET** (`HttpClient` and `System.Text.Json`):

```csharp
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

public sealed class AuthorizationCodeRequest
{
	public string Host { get; set; }
	public string AuthorizationCode { get; set; }
	public string RedirectUri { get; set; }
	public string ClientId { get; set; }
	public string ClientSecret { get; set; }

	// The PKCE verifier; null when no code_challenge was sent on the authorize request.
	public string CodeVerifier { get; set; }
}

public static class AuthorizationCodeExchange
{
	private const string _TOKEN_PATH = "/Relativity/Identity/connect/token";

	public static async Task<string> ExchangeAuthorizationCodeAsync(HttpClient client, AuthorizationCodeRequest request)
	{
		Dictionary<string, string> fields = new Dictionary<string, string>
		{
			{ "grant_type", "authorization_code" },
			{ "code", request.AuthorizationCode },
			{ "redirect_uri", request.RedirectUri },
			{ "client_id", request.ClientId },
			{ "client_secret", request.ClientSecret }
		};
		if (!string.IsNullOrEmpty(request.CodeVerifier))
		{
			fields["code_verifier"] = request.CodeVerifier;
		}

		string accessToken;
		using (FormUrlEncodedContent form = new FormUrlEncodedContent(fields))
		using (HttpResponseMessage response = await client.PostAsync($"{request.Host}{_TOKEN_PATH}", form))
		{
			response.EnsureSuccessStatusCode();
			using (JsonDocument document = JsonDocument.Parse(await response.Content.ReadAsStringAsync()))
			{
				accessToken = document.RootElement.GetProperty("access_token").GetString();
			}
		}

		return accessToken;
	}
}
```

**.NET Framework 4.7.2** (`System.Web.Extensions` and C# 7.3 syntax):

```csharp
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

public sealed class AuthorizationCodeRequest
{
	public string Host { get; set; }
	public string AuthorizationCode { get; set; }
	public string RedirectUri { get; set; }
	public string ClientId { get; set; }
	public string ClientSecret { get; set; }

	// The PKCE verifier; null when no code_challenge was sent on the authorize request.
	public string CodeVerifier { get; set; }
}

public static class AuthorizationCodeExchange
{
	private const string _TOKEN_PATH = "/Relativity/Identity/connect/token";

	public static async Task<string> ExchangeAuthorizationCodeAsync(HttpClient client, AuthorizationCodeRequest request)
	{
		Dictionary<string, string> fields = new Dictionary<string, string>
		{
			{ "grant_type", "authorization_code" },
			{ "code", request.AuthorizationCode },
			{ "redirect_uri", request.RedirectUri },
			{ "client_id", request.ClientId },
			{ "client_secret", request.ClientSecret }
		};
		if (!string.IsNullOrEmpty(request.CodeVerifier))
		{
			fields["code_verifier"] = request.CodeVerifier;
		}

		string accessToken;
		using (FormUrlEncodedContent form = new FormUrlEncodedContent(fields))
		using (HttpResponseMessage response = await client.PostAsync($"{request.Host}{_TOKEN_PATH}", form))
		{
			response.EnsureSuccessStatusCode();
			string body = await response.Content.ReadAsStringAsync();
			accessToken = new JavaScriptSerializer().Deserialize<Dictionary<string, object>>(body)["access_token"].ToString();
		}

		return accessToken;
	}
}
```

**Use the token.** Send `Authorization: Bearer <access token>` — the change at the use site in part 1 — with `X-CSRF-Header: -` on REST and Kepler calls.

**On expiry.** With Allow Refresh Tokens enabled on the OAuth2 Client and PKCE used, the application holds a refresh token and, when the access token expires, sends a `grant_type=refresh_token` request with it and the client credentials in place of the code exchange — *inferred* from the standard OAuth shape, not tested. The exchange example above reads only the `access_token` of the response; to renew with a refresh token, read the `refresh_token` from the same response as well and keep it as a secret, and otherwise the application sends the user through the sign-in again when the access token expires. Relativity does not document a refresh token's lifetime, whether it rotates, how it is revoked, or whether a revocation endpoint exists, nor what disabling or deleting the client does to tokens already issued. A refresh token is a secret: store it as Secret Management (Part B of the Migration Documentation) describes, and on compromise disable the client and replace it. Enable refresh tokens only if the application must keep a session beyond one token lifetime without asking the user to sign in again.

**PKCE.** PKCE is required by Relativity's own documentation to obtain a refresh token, and not otherwise: a Code request that does not ask for a refresh token works without it, **verified by NSerio against a live instance; not documented by Relativity**. It is never a blanket requirement. NSerio recommends PKCE with the `S256` method in both variants (NSerio recommendation), and does not claim that Relativity accepts any particular method, which it does not document.

**Scope.** No scope set is prescribed for Code. The scope below works — `openid RelativityRequestOrigin UserInfo UserInfoAccess AuthenticationID RelativityWeb` — **verified by NSerio against a live instance; not documented by Relativity**, verified as working and not as minimal. `state` is sent as standard OAuth practice and `nonce` is not used; Relativity does not document whether it enforces either.

**The application stops collecting the password.** After this change the application never receives, stores or forwards the user's username and password: the user signs in on RelativityOne's own page, and the application holds only tokens. Remove the credential prompt, the form field and any storage of the password that the replaced code needed.

**Single sign-on.** Sign-in happens on RelativityOne's own login page, so the instance's single sign-on and two-factor rules apply unchanged — *inferred* from where the sign-in happens, and not documented for clients that customers create.

**The client secret is required.** Code does not work without a client secret: tested and rejected, **verified by NSerio against a live instance; not documented by Relativity**, which confirms Relativity's own statement that the grant requires one. It is displayed once and is stored as Secret Management (Part B of the Migration Documentation) describes.

**Server-side variant.** For a web application whose code runs on a server:

- **Redirect.** The application receives the code at one exact HTTPS URL per environment, a route of the application, with no wildcards (NSerio recommendation; Relativity's matching rules are not documented). The Relativity administrator registers the exact URL on the OAuth2 Client.
- **Secret and tokens.** The client secret stays on the server and never reaches the browser. The user's tokens are held server-side, keyed to the session, and never in the browser.
- **Exchange.** The server performs the code exchange itself.

**Desktop variant.** For a desktop or command-line application run by a user on their own machine:

- **The system browser.** Open the authorize URL in the user's default browser and not in a web view the application hosts, so the application never sees the password (NSerio recommendation).
- **A loopback redirect on a fixed port.** Listen on the loopback address only, on the registered port, for one request. The Redirect URL is `http://127.0.0.1:<port>/<path>/`, ending with a slash because the listener below registers it as its prefix: a plain-HTTP loopback URL is accepted, **verified by NSerio against a live instance; not documented by Relativity**. The Relativity administrator registers that exact URL; because it is registered in advance, the port is fixed and not chosen at runtime.

```csharp
using System;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Net;
using System.Threading.Tasks;

public static class LoopbackAuthorizationReceiver
{
	private const string _CODE_PARAMETER = "code";
	private const string _STATE_PARAMETER = "state";
	private const string _ERROR_PARAMETER = "error";
	private const string _ERROR_DESCRIPTION_PARAMETER = "error_description";
	private const string _STATE_MISMATCH_MESSAGE = "The returned state is not the one that was sent; the response is rejected.";
	private const string _MISSING_CODE_MESSAGE = "The response carries no authorization code.";

	// Waits for the one request the browser sends to the registered loopback Redirect URL, and returns its code
	// only when its state is the one that was sent on the authorize request and the response carries no error.
	// redirectUrl is the exact URL registered on the OAuth2 Client, for example http://127.0.0.1:<port>/<path>/ :
	// it ends with a slash, as a listener prefix must.
	public static async Task<string> ReceiveAuthorizationCodeAsync(string authorizeUrl, string redirectUrl, string expectedState)
	{
		string authorizationCode;
		using (HttpListener listener = new HttpListener())
		{
			listener.Prefixes.Add(redirectUrl);
			listener.Start();
			Process.Start(new ProcessStartInfo(authorizeUrl) { UseShellExecute = true })?.Dispose();
			HttpListenerContext context = await listener.GetContextAsync();
			NameValueCollection query = context.Request.QueryString;
			context.Response.Close();
			if (query[_STATE_PARAMETER] != expectedState)
			{
				throw new InvalidOperationException(_STATE_MISMATCH_MESSAGE);
			}

			if (!string.IsNullOrEmpty(query[_ERROR_PARAMETER]))
			{
				throw new InvalidOperationException($"The authorization server returned an error: {query[_ERROR_PARAMETER]} {query[_ERROR_DESCRIPTION_PARAMETER]}".TrimEnd());
			}

			authorizationCode = query[_CODE_PARAMETER];
			if (string.IsNullOrEmpty(authorizationCode))
			{
				throw new InvalidOperationException(_MISSING_CODE_MESSAGE);
			}
		}

		return authorizationCode;
	}
}
```

- **The secret is extractable.** Code requires a client secret (above), so a distributed desktop application holds one, and **a secret shipped inside a distributed binary can be extracted by anyone who holds the binary**. It protects nothing against the person running the application.
- **Compensating guidance** (NSerio recommendation, subject to your own security policy): set Trusted IPs where the users' outbound addresses are known and stable — they restrict where a token can be *requested*, and are checked when a token is issued against the client's ID and secret; use the recommended short Access Token Lifetime of 60 minutes; enable refresh tokens only when needed; and create one OAuth2 Client per distribution, so a leaked secret can be disabled without affecting the others.
- **A documentation gap.** Relativity documents no recommendation for a distributed desktop application; this design is what NSerio verified works.

Verification: **Verification note — three things.**

**Moving the header from the client to the request is part of the migration, not a style choice.** A token has a finite lifetime, and a header added once to `DefaultRequestHeaders` cannot be refreshed without mutating shared state; the code works for the token's lifetime and then returns 401 until the process restarts.

**The pair's encoding may be a separate match.** Where `encodedCredentials` is built in the same file, the encoding and this header are two matches at two locations. They are one migration, and both locations change together.

**Confirm the endpoint in use is the RelativityOne REST API.** That is the API whose bearer token this guidance describes; an endpoint elsewhere reached from the same code may accept only Basic and reject the migrated call.

OAuth2 Client: See [OAuth2 Client for Authorization Code](#oauth2-client-for-authorization-code).
<!-- generated:end CS-006 authorization-code -->

### CS-007 · NetworkCredentialOnHttpHandler

<a id="cs-007-migration"></a>

#### Client Credentials

<!-- generated:begin CS-007 client-credentials -->
**Flow.** This entry is for Client Credentials. The "Which flow" paragraph of CS-007 in [Part A](part-a-csharp.md#cs-007) says when a match belongs to this flow.

Source form:
```csharp
var handler = new HttpClientHandler { Credentials = new NetworkCredential(userName, password) };
```

Under token authentication: **First, the destination:** if it is not RelativityOne, no migration to RelativityOne applies — that service's own token mechanism is the migration — and the rest of this entry does not apply.

**Drop the `Credentials` assignment** from the handler, keeping its other settings, and **set the bearer header per request**:

```csharp
var handler = new HttpClientHandler();
request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
```

Credentials: **Where the credentials come from.** The destination decides whether the migration applies. If the destination is not RelativityOne, the migration is that service's own token mechanism, and no migration to RelativityOne applies. If it is RelativityOne, the token comes from the token-acquisition part that follows.

**Where the token comes from.** Create an OAuth2 Client with Flow Grant Type set to Client Credentials (part 3 lists its settings), then send a form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=client_credentials`, `client_id`, `client_secret` and a `scope`. The token endpoint is **documented** for this grant. The token is then sent as `Authorization: Bearer <token>`, with `X-CSRF-Header: -` on REST and Kepler calls — the header is documented as required, and a Kepler call without it returns 404.

**.NET** (`HttpClient` and `System.Text.Json`). The class requests the token with the client ID and the client secret, caches it, requests a new one 60 seconds before it expires, and, when a call is rejected for authentication, requests a new one and sends the call once more. Build one instance for the life of the process and send every call through `SendAsync`; the request is created by the delegate on each attempt, because a request message cannot be sent twice. The client ID and the client secret come from the process environment or a secret store, never from source. Where the code uses the Relativity SDK, `GetAccessTokenAsync(false)` gives the token for `BearerTokenCredentials` each time a `ServiceFactory` is built.

```csharp
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

public sealed class AccessTokenProvider : IDisposable
{
	private const string _TOKEN_PATH = "/Relativity/Identity/connect/token";
	private const string _BEARER_SCHEME = "Bearer";
	private const string _CSRF_HEADER_NAME = "X-CSRF-Header";
	private const string _CSRF_HEADER_VALUE = "-";
	private static readonly TimeSpan _renewalMargin = TimeSpan.FromSeconds(60);

	private readonly HttpClient _client = new HttpClient();
	private readonly SemaphoreSlim _gate = new SemaphoreSlim(1, 1);
	private readonly string _host;
	private readonly string _clientId;
	private readonly string _clientSecret;
	private readonly string _scope;
	private string _accessToken;
	private DateTimeOffset _expiresAt;

	public AccessTokenProvider(string host, string clientId, string clientSecret, string scope)
	{
		_host = host;
		_clientId = clientId;
		_clientSecret = clientSecret;
		_scope = scope;
	}

	// Cached token; renewed shortly before it expires, or on demand after a rejected call.
	public async Task<string> GetAccessTokenAsync(bool forceRenewal)
	{
		await _gate.WaitAsync();
		try
		{
			if (forceRenewal || _accessToken == null || DateTimeOffset.UtcNow >= _expiresAt - _renewalMargin)
			{
				await AcquireAccessTokenAsync();
			}

			return _accessToken;
		}
		finally
		{
			_gate.Release();
		}
	}

	// Sends the request with the bearer token; when the call is rejected for authentication, renews the token and sends it once more.
	public async Task<HttpResponseMessage> SendAsync(Func<HttpRequestMessage> createRequest)
	{
		HttpResponseMessage response = await SendOnceAsync(createRequest, false);
		if (response.StatusCode == HttpStatusCode.Unauthorized)
		{
			response.Dispose();
			response = await SendOnceAsync(createRequest, true);
		}

		return response;
	}

	public void Dispose()
	{
		_client.Dispose();
		_gate.Dispose();
	}

	private async Task AcquireAccessTokenAsync()
	{
		using (FormUrlEncodedContent form = new FormUrlEncodedContent(new Dictionary<string, string>
		{
			{ "grant_type", "client_credentials" },
			{ "client_id", _clientId },
			{ "client_secret", _clientSecret },
			{ "scope", _scope }
		}))
		using (HttpResponseMessage response = await _client.PostAsync($"{_host}{_TOKEN_PATH}", form))
		{
			response.EnsureSuccessStatusCode();
			using (JsonDocument document = JsonDocument.Parse(await response.Content.ReadAsStringAsync()))
			{
				_accessToken = document.RootElement.GetProperty("access_token").GetString();
				_expiresAt = DateTimeOffset.UtcNow.AddSeconds(document.RootElement.GetProperty("expires_in").GetInt32());
			}
		}
	}

	private async Task<HttpResponseMessage> SendOnceAsync(Func<HttpRequestMessage> createRequest, bool forceRenewal)
	{
		string token = await GetAccessTokenAsync(forceRenewal);
		HttpResponseMessage response;
		using (HttpRequestMessage request = createRequest())
		{
			request.Headers.Authorization = new AuthenticationHeaderValue(_BEARER_SCHEME, token);
			request.Headers.Add(_CSRF_HEADER_NAME, _CSRF_HEADER_VALUE);
			response = await _client.SendAsync(request);
		}

		return response;
	}
}
```

**.NET Framework 4.7.2**: the same class with the serializer the runtime ships (`System.Web.Extensions`, referenced together with `System.Net.Http`), and C# 7.3 syntax:

```csharp
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

public sealed class AccessTokenProvider : IDisposable
{
	private const string _TOKEN_PATH = "/Relativity/Identity/connect/token";
	private const string _BEARER_SCHEME = "Bearer";
	private const string _CSRF_HEADER_NAME = "X-CSRF-Header";
	private const string _CSRF_HEADER_VALUE = "-";
	private static readonly TimeSpan _renewalMargin = TimeSpan.FromSeconds(60);

	private readonly HttpClient _client = new HttpClient();
	private readonly SemaphoreSlim _gate = new SemaphoreSlim(1, 1);
	private readonly string _host;
	private readonly string _clientId;
	private readonly string _clientSecret;
	private readonly string _scope;
	private string _accessToken;
	private DateTimeOffset _expiresAt;

	public AccessTokenProvider(string host, string clientId, string clientSecret, string scope)
	{
		_host = host;
		_clientId = clientId;
		_clientSecret = clientSecret;
		_scope = scope;
	}

	// Cached token; renewed shortly before it expires, or on demand after a rejected call.
	public async Task<string> GetAccessTokenAsync(bool forceRenewal)
	{
		await _gate.WaitAsync();
		try
		{
			if (forceRenewal || _accessToken == null || DateTimeOffset.UtcNow >= _expiresAt - _renewalMargin)
			{
				await AcquireAccessTokenAsync();
			}

			return _accessToken;
		}
		finally
		{
			_gate.Release();
		}
	}

	// Sends the request with the bearer token; when the call is rejected for authentication, renews the token and sends it once more.
	public async Task<HttpResponseMessage> SendAsync(Func<HttpRequestMessage> createRequest)
	{
		HttpResponseMessage response = await SendOnceAsync(createRequest, false);
		if (response.StatusCode == HttpStatusCode.Unauthorized)
		{
			response.Dispose();
			response = await SendOnceAsync(createRequest, true);
		}

		return response;
	}

	public void Dispose()
	{
		_client.Dispose();
		_gate.Dispose();
	}

	private async Task AcquireAccessTokenAsync()
	{
		using (FormUrlEncodedContent form = new FormUrlEncodedContent(new Dictionary<string, string>
		{
			{ "grant_type", "client_credentials" },
			{ "client_id", _clientId },
			{ "client_secret", _clientSecret },
			{ "scope", _scope }
		}))
		using (HttpResponseMessage response = await _client.PostAsync($"{_host}{_TOKEN_PATH}", form))
		{
			response.EnsureSuccessStatusCode();
			string body = await response.Content.ReadAsStringAsync();
			Dictionary<string, object> fields = new JavaScriptSerializer().Deserialize<Dictionary<string, object>>(body);
			_accessToken = fields["access_token"].ToString();
			_expiresAt = DateTimeOffset.UtcNow.AddSeconds(Convert.ToInt32(fields["expires_in"]));
		}
	}

	private async Task<HttpResponseMessage> SendOnceAsync(Func<HttpRequestMessage> createRequest, bool forceRenewal)
	{
		string token = await GetAccessTokenAsync(forceRenewal);
		HttpResponseMessage response;
		using (HttpRequestMessage request = createRequest())
		{
			request.Headers.Authorization = new AuthenticationHeaderValue(_BEARER_SCHEME, token);
			request.Headers.Add(_CSRF_HEADER_NAME, _CSRF_HEADER_VALUE);
			response = await _client.SendAsync(request);
		}

		return response;
	}
}
```

**The scope depends on what the integration needs, and no single scope is required.** Relativity's documentation shows two values for this grant: `SystemUserInfo` on the REST authentication page, the Usage Reports page and in the official Transfer SDK sample, and `UserInfoAccess` in the Kepler .NET samples. No page says which is correct, whether both work, or what either grants, and the OAuth2 Client's `Scopes` property is documented as one that cannot be set. Use the value the API you call documents, and confirm it.

**Cache the token, and re-acquire it on expiry — do not refresh.** Keep the token for its lifetime (part 3 recommends 60 minutes, Relativity's own wording) and request a new one with the client secret shortly before it expires (the JavaScript, TypeScript and Python examples use 60 seconds), and once when a call is rejected for authentication. Client Credentials does not offer refresh tokens: **verified by NSerio against a live instance; not documented by Relativity** — the Allow Refresh Tokens control does not appear for this grant in the RelativityOne user interface. Relativity's own tables and text on refresh tokens conflict with each other, so re-acquisition with the secret is the advice that holds whichever way they are read.

**Long-running processes that use the Relativity SDK.** A `ServiceFactory` built once from a bearer token keeps that token after it expires. Rebuild the `ServiceFactory` and its proxies with a fresh token before expiry. This is the only advice consistent with the documentation: the package `Relativity.Kepler.Client.SDK` (documented) exposes a settable `Token` and an `HttpMessageHandler` overload but no token-provider overload, and no page documents renewing a token behind a factory.

**The client secret.** It is displayed once, at creation or reset. Store it as Secret Management (Part B of the Migration Documentation) describes; never in source, never in tracked configuration. A reset invalidates the previous secret immediately, so plan the cut-over before resetting it.

**Before deploying, confirm the calling identity.** Every call now runs as the OAuth2 Client's **Context User** — not as the user in the replaced credential, and for an import or a job as much as for a request. If that user has different workspace permissions or item-level security, the code returns fewer rows or `Unauthorized` in production with no change to its logic, and Relativity's audit attributes every call to that one account. Confirm the Context User's permissions match those of the previous identity. Relativity's REST API authentication page describes the Context User as usually a member of the Relativity Administrators group; the OAuth2 Client block (part 3) gives NSerio's recommendation, which differs.

**When the integration acts for a signed-in user.** Client Credentials is one service identity by construction. An integration that acts on behalf of a signed-in user is the Authorization Code case, and moving to it means a new OAuth2 Client, because the flow is fixed when a client is created. Which of the two flows applies depends on who supplies the credential at run time.

Verification: **Verification note — five things.**

**Confirm the exchange was Basic.** `NetworkCredential` declares no scheme, and a handler carrying one serves integrated Windows authentication as readily as Basic. If the server negotiated an integrated scheme, this construct is not Basic Authentication and the migration does not apply.

**The 401-challenge response is lost.** A handler's credentials answer the server's challenge; a bearer header is sent up front and answers nothing. Code or a server that depended on that exchange behaves differently after the change.

**Redirects stop carrying the credential.** Depending on the runtime and the credential type, handler credentials may be re-applied to a redirect target; a manually set `Authorization` header never is — .NET documents that the `HttpClient` handler drops it on redirect. Code that relied on a redirect can start returning 401 after the migration; address the final URI, or handle the redirect explicitly.

**A token has a finite lifetime, and a header set once on a long-lived client cannot be refreshed.** Set it per request, not on the client's default headers, or the code works for the token's lifetime and then returns 401 until the process restarts.

**Confirm the endpoint in use is the RelativityOne REST API.** That is the API whose bearer token this guidance describes; an endpoint elsewhere reached from the same code may accept only Basic and reject the migrated call.

OAuth2 Client: See [OAuth2 Client for Client Credentials](#oauth2-client-for-client-credentials).
<!-- generated:end CS-007 client-credentials -->

#### Authorization Code

<!-- generated:begin CS-007 authorization-code -->
**Flow.** This entry is for Authorization Code. The "Which flow" paragraph of CS-007 in [Part A](part-a-csharp.md#cs-007) says when a match belongs to this flow.

Source form:
```csharp
var handler = new HttpClientHandler { Credentials = new NetworkCredential(userName, password) };
```

Under token authentication: **First, the destination:** if it is not RelativityOne, no migration to RelativityOne applies — that service's own token mechanism is the migration — and the rest of this entry does not apply.

**Drop the `Credentials` assignment** from the handler, keeping its other settings, and **set the bearer header per request**:

```csharp
var handler = new HttpClientHandler();
request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
```

Credentials: **Where the credentials come from.** The destination decides whether the migration applies. If the destination is not RelativityOne, the migration is that service's own token mechanism, and no migration to RelativityOne applies. If it is RelativityOne, the token comes from the token-acquisition part that follows.

**Common to both variants**

**Sign in.** Replace the code that collected the user's credentials with a redirect: send the user's browser to the authorize endpoint `<host>/Relativity/Identity/connect/authorize`, which leads to the RelativityOne login page. The authorize endpoint is **verified by NSerio against a live instance; not documented by Relativity**. The request carries the standard authorization-code parameters — `client_id`, `response_type=code`, the registered `redirect_uri`, a `scope`, and a `state` value the application checks on return; these are standard OAuth, and Relativity documents none of them. No discovery document is relied on.

**Receive the code.** The browser returns to the Redirect URL registered on the OAuth2 Client, with the authorization code and the `state` value. Check the `state` before using the code. The Redirect URL must be exactly the one registered (the variants say how).

**Exchange the code.** Send a form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=authorization_code`, the `code`, the same `redirect_uri`, the `client_id` and the `client_secret`, plus the `code_verifier` when PKCE is used. The code is exchanged at that endpoint, which Relativity documents for the Client Credentials grant only; the exchange is **verified by NSerio against a live instance; not documented by Relativity**. Send the `code_verifier` only when the authorize request carried a `code_challenge`; otherwise leave it out.

**.NET** (`HttpClient` and `System.Text.Json`):

```csharp
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

public sealed class AuthorizationCodeRequest
{
	public string Host { get; set; }
	public string AuthorizationCode { get; set; }
	public string RedirectUri { get; set; }
	public string ClientId { get; set; }
	public string ClientSecret { get; set; }

	// The PKCE verifier; null when no code_challenge was sent on the authorize request.
	public string CodeVerifier { get; set; }
}

public static class AuthorizationCodeExchange
{
	private const string _TOKEN_PATH = "/Relativity/Identity/connect/token";

	public static async Task<string> ExchangeAuthorizationCodeAsync(HttpClient client, AuthorizationCodeRequest request)
	{
		Dictionary<string, string> fields = new Dictionary<string, string>
		{
			{ "grant_type", "authorization_code" },
			{ "code", request.AuthorizationCode },
			{ "redirect_uri", request.RedirectUri },
			{ "client_id", request.ClientId },
			{ "client_secret", request.ClientSecret }
		};
		if (!string.IsNullOrEmpty(request.CodeVerifier))
		{
			fields["code_verifier"] = request.CodeVerifier;
		}

		string accessToken;
		using (FormUrlEncodedContent form = new FormUrlEncodedContent(fields))
		using (HttpResponseMessage response = await client.PostAsync($"{request.Host}{_TOKEN_PATH}", form))
		{
			response.EnsureSuccessStatusCode();
			using (JsonDocument document = JsonDocument.Parse(await response.Content.ReadAsStringAsync()))
			{
				accessToken = document.RootElement.GetProperty("access_token").GetString();
			}
		}

		return accessToken;
	}
}
```

**.NET Framework 4.7.2** (`System.Web.Extensions` and C# 7.3 syntax):

```csharp
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

public sealed class AuthorizationCodeRequest
{
	public string Host { get; set; }
	public string AuthorizationCode { get; set; }
	public string RedirectUri { get; set; }
	public string ClientId { get; set; }
	public string ClientSecret { get; set; }

	// The PKCE verifier; null when no code_challenge was sent on the authorize request.
	public string CodeVerifier { get; set; }
}

public static class AuthorizationCodeExchange
{
	private const string _TOKEN_PATH = "/Relativity/Identity/connect/token";

	public static async Task<string> ExchangeAuthorizationCodeAsync(HttpClient client, AuthorizationCodeRequest request)
	{
		Dictionary<string, string> fields = new Dictionary<string, string>
		{
			{ "grant_type", "authorization_code" },
			{ "code", request.AuthorizationCode },
			{ "redirect_uri", request.RedirectUri },
			{ "client_id", request.ClientId },
			{ "client_secret", request.ClientSecret }
		};
		if (!string.IsNullOrEmpty(request.CodeVerifier))
		{
			fields["code_verifier"] = request.CodeVerifier;
		}

		string accessToken;
		using (FormUrlEncodedContent form = new FormUrlEncodedContent(fields))
		using (HttpResponseMessage response = await client.PostAsync($"{request.Host}{_TOKEN_PATH}", form))
		{
			response.EnsureSuccessStatusCode();
			string body = await response.Content.ReadAsStringAsync();
			accessToken = new JavaScriptSerializer().Deserialize<Dictionary<string, object>>(body)["access_token"].ToString();
		}

		return accessToken;
	}
}
```

**Use the token.** Send `Authorization: Bearer <access token>` — the change at the use site in part 1 — with `X-CSRF-Header: -` on REST and Kepler calls.

**On expiry.** With Allow Refresh Tokens enabled on the OAuth2 Client and PKCE used, the application holds a refresh token and, when the access token expires, sends a `grant_type=refresh_token` request with it and the client credentials in place of the code exchange — *inferred* from the standard OAuth shape, not tested. The exchange example above reads only the `access_token` of the response; to renew with a refresh token, read the `refresh_token` from the same response as well and keep it as a secret, and otherwise the application sends the user through the sign-in again when the access token expires. Relativity does not document a refresh token's lifetime, whether it rotates, how it is revoked, or whether a revocation endpoint exists, nor what disabling or deleting the client does to tokens already issued. A refresh token is a secret: store it as Secret Management (Part B of the Migration Documentation) describes, and on compromise disable the client and replace it. Enable refresh tokens only if the application must keep a session beyond one token lifetime without asking the user to sign in again.

**PKCE.** PKCE is required by Relativity's own documentation to obtain a refresh token, and not otherwise: a Code request that does not ask for a refresh token works without it, **verified by NSerio against a live instance; not documented by Relativity**. It is never a blanket requirement. NSerio recommends PKCE with the `S256` method in both variants (NSerio recommendation), and does not claim that Relativity accepts any particular method, which it does not document.

**Scope.** No scope set is prescribed for Code. The scope below works — `openid RelativityRequestOrigin UserInfo UserInfoAccess AuthenticationID RelativityWeb` — **verified by NSerio against a live instance; not documented by Relativity**, verified as working and not as minimal. `state` is sent as standard OAuth practice and `nonce` is not used; Relativity does not document whether it enforces either.

**The application stops collecting the password.** After this change the application never receives, stores or forwards the user's username and password: the user signs in on RelativityOne's own page, and the application holds only tokens. Remove the credential prompt, the form field and any storage of the password that the replaced code needed.

**Single sign-on.** Sign-in happens on RelativityOne's own login page, so the instance's single sign-on and two-factor rules apply unchanged — *inferred* from where the sign-in happens, and not documented for clients that customers create.

**The client secret is required.** Code does not work without a client secret: tested and rejected, **verified by NSerio against a live instance; not documented by Relativity**, which confirms Relativity's own statement that the grant requires one. It is displayed once and is stored as Secret Management (Part B of the Migration Documentation) describes.

**Server-side variant.** For a web application whose code runs on a server:

- **Redirect.** The application receives the code at one exact HTTPS URL per environment, a route of the application, with no wildcards (NSerio recommendation; Relativity's matching rules are not documented). The Relativity administrator registers the exact URL on the OAuth2 Client.
- **Secret and tokens.** The client secret stays on the server and never reaches the browser. The user's tokens are held server-side, keyed to the session, and never in the browser.
- **Exchange.** The server performs the code exchange itself.

**Desktop variant.** For a desktop or command-line application run by a user on their own machine:

- **The system browser.** Open the authorize URL in the user's default browser and not in a web view the application hosts, so the application never sees the password (NSerio recommendation).
- **A loopback redirect on a fixed port.** Listen on the loopback address only, on the registered port, for one request. The Redirect URL is `http://127.0.0.1:<port>/<path>/`, ending with a slash because the listener below registers it as its prefix: a plain-HTTP loopback URL is accepted, **verified by NSerio against a live instance; not documented by Relativity**. The Relativity administrator registers that exact URL; because it is registered in advance, the port is fixed and not chosen at runtime.

```csharp
using System;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Net;
using System.Threading.Tasks;

public static class LoopbackAuthorizationReceiver
{
	private const string _CODE_PARAMETER = "code";
	private const string _STATE_PARAMETER = "state";
	private const string _ERROR_PARAMETER = "error";
	private const string _ERROR_DESCRIPTION_PARAMETER = "error_description";
	private const string _STATE_MISMATCH_MESSAGE = "The returned state is not the one that was sent; the response is rejected.";
	private const string _MISSING_CODE_MESSAGE = "The response carries no authorization code.";

	// Waits for the one request the browser sends to the registered loopback Redirect URL, and returns its code
	// only when its state is the one that was sent on the authorize request and the response carries no error.
	// redirectUrl is the exact URL registered on the OAuth2 Client, for example http://127.0.0.1:<port>/<path>/ :
	// it ends with a slash, as a listener prefix must.
	public static async Task<string> ReceiveAuthorizationCodeAsync(string authorizeUrl, string redirectUrl, string expectedState)
	{
		string authorizationCode;
		using (HttpListener listener = new HttpListener())
		{
			listener.Prefixes.Add(redirectUrl);
			listener.Start();
			Process.Start(new ProcessStartInfo(authorizeUrl) { UseShellExecute = true })?.Dispose();
			HttpListenerContext context = await listener.GetContextAsync();
			NameValueCollection query = context.Request.QueryString;
			context.Response.Close();
			if (query[_STATE_PARAMETER] != expectedState)
			{
				throw new InvalidOperationException(_STATE_MISMATCH_MESSAGE);
			}

			if (!string.IsNullOrEmpty(query[_ERROR_PARAMETER]))
			{
				throw new InvalidOperationException($"The authorization server returned an error: {query[_ERROR_PARAMETER]} {query[_ERROR_DESCRIPTION_PARAMETER]}".TrimEnd());
			}

			authorizationCode = query[_CODE_PARAMETER];
			if (string.IsNullOrEmpty(authorizationCode))
			{
				throw new InvalidOperationException(_MISSING_CODE_MESSAGE);
			}
		}

		return authorizationCode;
	}
}
```

- **The secret is extractable.** Code requires a client secret (above), so a distributed desktop application holds one, and **a secret shipped inside a distributed binary can be extracted by anyone who holds the binary**. It protects nothing against the person running the application.
- **Compensating guidance** (NSerio recommendation, subject to your own security policy): set Trusted IPs where the users' outbound addresses are known and stable — they restrict where a token can be *requested*, and are checked when a token is issued against the client's ID and secret; use the recommended short Access Token Lifetime of 60 minutes; enable refresh tokens only when needed; and create one OAuth2 Client per distribution, so a leaked secret can be disabled without affecting the others.
- **A documentation gap.** Relativity documents no recommendation for a distributed desktop application; this design is what NSerio verified works.

Verification: **Verification note — five things.**

**Confirm the exchange was Basic.** `NetworkCredential` declares no scheme, and a handler carrying one serves integrated Windows authentication as readily as Basic. If the server negotiated an integrated scheme, this construct is not Basic Authentication and the migration does not apply.

**The 401-challenge response is lost.** A handler's credentials answer the server's challenge; a bearer header is sent up front and answers nothing. Code or a server that depended on that exchange behaves differently after the change.

**Redirects stop carrying the credential.** Depending on the runtime and the credential type, handler credentials may be re-applied to a redirect target; a manually set `Authorization` header never is — .NET documents that the `HttpClient` handler drops it on redirect. Code that relied on a redirect can start returning 401 after the migration; address the final URI, or handle the redirect explicitly.

**A token has a finite lifetime, and a header set once on a long-lived client cannot be refreshed.** Set it per request, not on the client's default headers, or the code works for the token's lifetime and then returns 401 until the process restarts.

**Confirm the endpoint in use is the RelativityOne REST API.** That is the API whose bearer token this guidance describes; an endpoint elsewhere reached from the same code may accept only Basic and reject the migrated call.

OAuth2 Client: See [OAuth2 Client for Authorization Code](#oauth2-client-for-authorization-code).
<!-- generated:end CS-007 authorization-code -->

### CS-008 · NetworkCredentialConstruction

<a id="cs-008-migration"></a>

#### Client Credentials

<!-- generated:begin CS-008 client-credentials -->
**Flow.** This entry is for Client Credentials. The "Which flow" paragraph of CS-008 in [Part A](part-a-csharp.md#cs-008) says when a match belongs to this flow.

Source form:
```csharp
var credential = new NetworkCredential(userName, password);
```

Under token authentication: **First, the destination:** if it is not RelativityOne, no migration to RelativityOne applies — that service's own token mechanism is the migration — and the rest of this entry does not apply.

**Identify the consumer first** — the fix may be outside the matched file, because the credential in this match has a consumer that was not identified in it. The form of the change depends on what consumes the credential:

- an `HttpClient` handler: remove the credential from the handler and set the `Authorization` header to `Bearer` and the token on each request message;
- `WebClient` or `HttpWebRequest`: remove the credential and set the `Authorization` header to `Bearer` and the token on each request;
- a SOAP proxy: the binding changes, and the service may not accept bearer at all;
- a mail, file-transfer, directory or proxy consumer: not a migration target.

No code form is given, because none is correct for all four.

Credentials: **Where the credentials come from.** The destination decides whether the migration applies. If the destination is not RelativityOne, the migration is that service's own token mechanism, and no migration to RelativityOne applies. If it is RelativityOne, the token comes from the token-acquisition part that follows.

**Where the token comes from.** Create an OAuth2 Client with Flow Grant Type set to Client Credentials (part 3 lists its settings), then send a form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=client_credentials`, `client_id`, `client_secret` and a `scope`. The token endpoint is **documented** for this grant. The token is then sent as `Authorization: Bearer <token>`, with `X-CSRF-Header: -` on REST and Kepler calls — the header is documented as required, and a Kepler call without it returns 404.

**.NET** (`HttpClient` and `System.Text.Json`). The class requests the token with the client ID and the client secret, caches it, requests a new one 60 seconds before it expires, and, when a call is rejected for authentication, requests a new one and sends the call once more. Build one instance for the life of the process and send every call through `SendAsync`; the request is created by the delegate on each attempt, because a request message cannot be sent twice. The client ID and the client secret come from the process environment or a secret store, never from source. Where the code uses the Relativity SDK, `GetAccessTokenAsync(false)` gives the token for `BearerTokenCredentials` each time a `ServiceFactory` is built.

```csharp
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

public sealed class AccessTokenProvider : IDisposable
{
	private const string _TOKEN_PATH = "/Relativity/Identity/connect/token";
	private const string _BEARER_SCHEME = "Bearer";
	private const string _CSRF_HEADER_NAME = "X-CSRF-Header";
	private const string _CSRF_HEADER_VALUE = "-";
	private static readonly TimeSpan _renewalMargin = TimeSpan.FromSeconds(60);

	private readonly HttpClient _client = new HttpClient();
	private readonly SemaphoreSlim _gate = new SemaphoreSlim(1, 1);
	private readonly string _host;
	private readonly string _clientId;
	private readonly string _clientSecret;
	private readonly string _scope;
	private string _accessToken;
	private DateTimeOffset _expiresAt;

	public AccessTokenProvider(string host, string clientId, string clientSecret, string scope)
	{
		_host = host;
		_clientId = clientId;
		_clientSecret = clientSecret;
		_scope = scope;
	}

	// Cached token; renewed shortly before it expires, or on demand after a rejected call.
	public async Task<string> GetAccessTokenAsync(bool forceRenewal)
	{
		await _gate.WaitAsync();
		try
		{
			if (forceRenewal || _accessToken == null || DateTimeOffset.UtcNow >= _expiresAt - _renewalMargin)
			{
				await AcquireAccessTokenAsync();
			}

			return _accessToken;
		}
		finally
		{
			_gate.Release();
		}
	}

	// Sends the request with the bearer token; when the call is rejected for authentication, renews the token and sends it once more.
	public async Task<HttpResponseMessage> SendAsync(Func<HttpRequestMessage> createRequest)
	{
		HttpResponseMessage response = await SendOnceAsync(createRequest, false);
		if (response.StatusCode == HttpStatusCode.Unauthorized)
		{
			response.Dispose();
			response = await SendOnceAsync(createRequest, true);
		}

		return response;
	}

	public void Dispose()
	{
		_client.Dispose();
		_gate.Dispose();
	}

	private async Task AcquireAccessTokenAsync()
	{
		using (FormUrlEncodedContent form = new FormUrlEncodedContent(new Dictionary<string, string>
		{
			{ "grant_type", "client_credentials" },
			{ "client_id", _clientId },
			{ "client_secret", _clientSecret },
			{ "scope", _scope }
		}))
		using (HttpResponseMessage response = await _client.PostAsync($"{_host}{_TOKEN_PATH}", form))
		{
			response.EnsureSuccessStatusCode();
			using (JsonDocument document = JsonDocument.Parse(await response.Content.ReadAsStringAsync()))
			{
				_accessToken = document.RootElement.GetProperty("access_token").GetString();
				_expiresAt = DateTimeOffset.UtcNow.AddSeconds(document.RootElement.GetProperty("expires_in").GetInt32());
			}
		}
	}

	private async Task<HttpResponseMessage> SendOnceAsync(Func<HttpRequestMessage> createRequest, bool forceRenewal)
	{
		string token = await GetAccessTokenAsync(forceRenewal);
		HttpResponseMessage response;
		using (HttpRequestMessage request = createRequest())
		{
			request.Headers.Authorization = new AuthenticationHeaderValue(_BEARER_SCHEME, token);
			request.Headers.Add(_CSRF_HEADER_NAME, _CSRF_HEADER_VALUE);
			response = await _client.SendAsync(request);
		}

		return response;
	}
}
```

**.NET Framework 4.7.2**: the same class with the serializer the runtime ships (`System.Web.Extensions`, referenced together with `System.Net.Http`), and C# 7.3 syntax:

```csharp
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

public sealed class AccessTokenProvider : IDisposable
{
	private const string _TOKEN_PATH = "/Relativity/Identity/connect/token";
	private const string _BEARER_SCHEME = "Bearer";
	private const string _CSRF_HEADER_NAME = "X-CSRF-Header";
	private const string _CSRF_HEADER_VALUE = "-";
	private static readonly TimeSpan _renewalMargin = TimeSpan.FromSeconds(60);

	private readonly HttpClient _client = new HttpClient();
	private readonly SemaphoreSlim _gate = new SemaphoreSlim(1, 1);
	private readonly string _host;
	private readonly string _clientId;
	private readonly string _clientSecret;
	private readonly string _scope;
	private string _accessToken;
	private DateTimeOffset _expiresAt;

	public AccessTokenProvider(string host, string clientId, string clientSecret, string scope)
	{
		_host = host;
		_clientId = clientId;
		_clientSecret = clientSecret;
		_scope = scope;
	}

	// Cached token; renewed shortly before it expires, or on demand after a rejected call.
	public async Task<string> GetAccessTokenAsync(bool forceRenewal)
	{
		await _gate.WaitAsync();
		try
		{
			if (forceRenewal || _accessToken == null || DateTimeOffset.UtcNow >= _expiresAt - _renewalMargin)
			{
				await AcquireAccessTokenAsync();
			}

			return _accessToken;
		}
		finally
		{
			_gate.Release();
		}
	}

	// Sends the request with the bearer token; when the call is rejected for authentication, renews the token and sends it once more.
	public async Task<HttpResponseMessage> SendAsync(Func<HttpRequestMessage> createRequest)
	{
		HttpResponseMessage response = await SendOnceAsync(createRequest, false);
		if (response.StatusCode == HttpStatusCode.Unauthorized)
		{
			response.Dispose();
			response = await SendOnceAsync(createRequest, true);
		}

		return response;
	}

	public void Dispose()
	{
		_client.Dispose();
		_gate.Dispose();
	}

	private async Task AcquireAccessTokenAsync()
	{
		using (FormUrlEncodedContent form = new FormUrlEncodedContent(new Dictionary<string, string>
		{
			{ "grant_type", "client_credentials" },
			{ "client_id", _clientId },
			{ "client_secret", _clientSecret },
			{ "scope", _scope }
		}))
		using (HttpResponseMessage response = await _client.PostAsync($"{_host}{_TOKEN_PATH}", form))
		{
			response.EnsureSuccessStatusCode();
			string body = await response.Content.ReadAsStringAsync();
			Dictionary<string, object> fields = new JavaScriptSerializer().Deserialize<Dictionary<string, object>>(body);
			_accessToken = fields["access_token"].ToString();
			_expiresAt = DateTimeOffset.UtcNow.AddSeconds(Convert.ToInt32(fields["expires_in"]));
		}
	}

	private async Task<HttpResponseMessage> SendOnceAsync(Func<HttpRequestMessage> createRequest, bool forceRenewal)
	{
		string token = await GetAccessTokenAsync(forceRenewal);
		HttpResponseMessage response;
		using (HttpRequestMessage request = createRequest())
		{
			request.Headers.Authorization = new AuthenticationHeaderValue(_BEARER_SCHEME, token);
			request.Headers.Add(_CSRF_HEADER_NAME, _CSRF_HEADER_VALUE);
			response = await _client.SendAsync(request);
		}

		return response;
	}
}
```

**The scope depends on what the integration needs, and no single scope is required.** Relativity's documentation shows two values for this grant: `SystemUserInfo` on the REST authentication page, the Usage Reports page and in the official Transfer SDK sample, and `UserInfoAccess` in the Kepler .NET samples. No page says which is correct, whether both work, or what either grants, and the OAuth2 Client's `Scopes` property is documented as one that cannot be set. Use the value the API you call documents, and confirm it.

**Cache the token, and re-acquire it on expiry — do not refresh.** Keep the token for its lifetime (part 3 recommends 60 minutes, Relativity's own wording) and request a new one with the client secret shortly before it expires (the JavaScript, TypeScript and Python examples use 60 seconds), and once when a call is rejected for authentication. Client Credentials does not offer refresh tokens: **verified by NSerio against a live instance; not documented by Relativity** — the Allow Refresh Tokens control does not appear for this grant in the RelativityOne user interface. Relativity's own tables and text on refresh tokens conflict with each other, so re-acquisition with the secret is the advice that holds whichever way they are read.

**Long-running processes that use the Relativity SDK.** A `ServiceFactory` built once from a bearer token keeps that token after it expires. Rebuild the `ServiceFactory` and its proxies with a fresh token before expiry. This is the only advice consistent with the documentation: the package `Relativity.Kepler.Client.SDK` (documented) exposes a settable `Token` and an `HttpMessageHandler` overload but no token-provider overload, and no page documents renewing a token behind a factory.

**The client secret.** It is displayed once, at creation or reset. Store it as Secret Management (Part B of the Migration Documentation) describes; never in source, never in tracked configuration. A reset invalidates the previous secret immediately, so plan the cut-over before resetting it.

**Before deploying, confirm the calling identity.** Every call now runs as the OAuth2 Client's **Context User** — not as the user in the replaced credential, and for an import or a job as much as for a request. If that user has different workspace permissions or item-level security, the code returns fewer rows or `Unauthorized` in production with no change to its logic, and Relativity's audit attributes every call to that one account. Confirm the Context User's permissions match those of the previous identity. Relativity's REST API authentication page describes the Context User as usually a member of the Relativity Administrators group; the OAuth2 Client block (part 3) gives NSerio's recommendation, which differs.

**When the integration acts for a signed-in user.** Client Credentials is one service identity by construction. An integration that acts on behalf of a signed-in user is the Authorization Code case, and moving to it means a new OAuth2 Client, because the flow is fixed when a client is created. Which of the two flows applies depends on who supplies the credential at run time.

Verification: **Verification note — seven things.**

**This match is the least characterized of any other class.** The consumer was not identified, so neither the scheme nor the destination has been established. Treat it as the location of a credential to trace, not as a construct whose migration is known.

**Confirm the exchange was Basic.** `NetworkCredential` declares no scheme; the same type serves integrated Windows authentication, and the consumers above perform the exchange themselves with no `Basic` in the source. If the exchange is integrated, the migration does not apply.

**The 401-challenge response is lost** wherever the consumer answered a challenge with this credential; a bearer header is sent up front and answers nothing.

**Redirects stop carrying the credential.** Depending on the runtime and the credential type, a consumer's credentials may be re-applied to a redirect target; a manually set `Authorization` header never is. .NET documents that `HttpWebRequest` clears the `Authorization` header when it follows a redirect automatically, and the `HttpClient` handler drops it on redirect. Code that relied on a redirect can start returning 401 after the migration; address the final URI, or handle the redirect explicitly.

**A SOAP service may not accept bearer at all.** Confirm it does before changing a binding.

**Confirm the endpoint in use is the RelativityOne REST API.** That is the API whose bearer token this guidance describes; an endpoint elsewhere reached from the same code may accept only Basic and reject the migrated call.

**A token has a finite lifetime.** A header must be set per request to be refreshable: a header set once on a long-lived consumer works for the token's lifetime and then returns 401 until the process restarts.

OAuth2 Client: See [OAuth2 Client for Client Credentials](#oauth2-client-for-client-credentials).
<!-- generated:end CS-008 client-credentials -->

#### Authorization Code

<!-- generated:begin CS-008 authorization-code -->
**Flow.** This entry is for Authorization Code. The "Which flow" paragraph of CS-008 in [Part A](part-a-csharp.md#cs-008) says when a match belongs to this flow.

Source form:
```csharp
var credential = new NetworkCredential(userName, password);
```

Under token authentication: **First, the destination:** if it is not RelativityOne, no migration to RelativityOne applies — that service's own token mechanism is the migration — and the rest of this entry does not apply.

**Identify the consumer first** — the fix may be outside the matched file, because the credential in this match has a consumer that was not identified in it. The form of the change depends on what consumes the credential:

- an `HttpClient` handler: remove the credential from the handler and set the `Authorization` header to `Bearer` and the token on each request message;
- `WebClient` or `HttpWebRequest`: remove the credential and set the `Authorization` header to `Bearer` and the token on each request;
- a SOAP proxy: the binding changes, and the service may not accept bearer at all;
- a mail, file-transfer, directory or proxy consumer: not a migration target.

No code form is given, because none is correct for all four.

Credentials: **Where the credentials come from.** The destination decides whether the migration applies. If the destination is not RelativityOne, the migration is that service's own token mechanism, and no migration to RelativityOne applies. If it is RelativityOne, the token comes from the token-acquisition part that follows.

**Common to both variants**

**Sign in.** Replace the code that collected the user's credentials with a redirect: send the user's browser to the authorize endpoint `<host>/Relativity/Identity/connect/authorize`, which leads to the RelativityOne login page. The authorize endpoint is **verified by NSerio against a live instance; not documented by Relativity**. The request carries the standard authorization-code parameters — `client_id`, `response_type=code`, the registered `redirect_uri`, a `scope`, and a `state` value the application checks on return; these are standard OAuth, and Relativity documents none of them. No discovery document is relied on.

**Receive the code.** The browser returns to the Redirect URL registered on the OAuth2 Client, with the authorization code and the `state` value. Check the `state` before using the code. The Redirect URL must be exactly the one registered (the variants say how).

**Exchange the code.** Send a form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=authorization_code`, the `code`, the same `redirect_uri`, the `client_id` and the `client_secret`, plus the `code_verifier` when PKCE is used. The code is exchanged at that endpoint, which Relativity documents for the Client Credentials grant only; the exchange is **verified by NSerio against a live instance; not documented by Relativity**. Send the `code_verifier` only when the authorize request carried a `code_challenge`; otherwise leave it out.

**.NET** (`HttpClient` and `System.Text.Json`):

```csharp
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

public sealed class AuthorizationCodeRequest
{
	public string Host { get; set; }
	public string AuthorizationCode { get; set; }
	public string RedirectUri { get; set; }
	public string ClientId { get; set; }
	public string ClientSecret { get; set; }

	// The PKCE verifier; null when no code_challenge was sent on the authorize request.
	public string CodeVerifier { get; set; }
}

public static class AuthorizationCodeExchange
{
	private const string _TOKEN_PATH = "/Relativity/Identity/connect/token";

	public static async Task<string> ExchangeAuthorizationCodeAsync(HttpClient client, AuthorizationCodeRequest request)
	{
		Dictionary<string, string> fields = new Dictionary<string, string>
		{
			{ "grant_type", "authorization_code" },
			{ "code", request.AuthorizationCode },
			{ "redirect_uri", request.RedirectUri },
			{ "client_id", request.ClientId },
			{ "client_secret", request.ClientSecret }
		};
		if (!string.IsNullOrEmpty(request.CodeVerifier))
		{
			fields["code_verifier"] = request.CodeVerifier;
		}

		string accessToken;
		using (FormUrlEncodedContent form = new FormUrlEncodedContent(fields))
		using (HttpResponseMessage response = await client.PostAsync($"{request.Host}{_TOKEN_PATH}", form))
		{
			response.EnsureSuccessStatusCode();
			using (JsonDocument document = JsonDocument.Parse(await response.Content.ReadAsStringAsync()))
			{
				accessToken = document.RootElement.GetProperty("access_token").GetString();
			}
		}

		return accessToken;
	}
}
```

**.NET Framework 4.7.2** (`System.Web.Extensions` and C# 7.3 syntax):

```csharp
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

public sealed class AuthorizationCodeRequest
{
	public string Host { get; set; }
	public string AuthorizationCode { get; set; }
	public string RedirectUri { get; set; }
	public string ClientId { get; set; }
	public string ClientSecret { get; set; }

	// The PKCE verifier; null when no code_challenge was sent on the authorize request.
	public string CodeVerifier { get; set; }
}

public static class AuthorizationCodeExchange
{
	private const string _TOKEN_PATH = "/Relativity/Identity/connect/token";

	public static async Task<string> ExchangeAuthorizationCodeAsync(HttpClient client, AuthorizationCodeRequest request)
	{
		Dictionary<string, string> fields = new Dictionary<string, string>
		{
			{ "grant_type", "authorization_code" },
			{ "code", request.AuthorizationCode },
			{ "redirect_uri", request.RedirectUri },
			{ "client_id", request.ClientId },
			{ "client_secret", request.ClientSecret }
		};
		if (!string.IsNullOrEmpty(request.CodeVerifier))
		{
			fields["code_verifier"] = request.CodeVerifier;
		}

		string accessToken;
		using (FormUrlEncodedContent form = new FormUrlEncodedContent(fields))
		using (HttpResponseMessage response = await client.PostAsync($"{request.Host}{_TOKEN_PATH}", form))
		{
			response.EnsureSuccessStatusCode();
			string body = await response.Content.ReadAsStringAsync();
			accessToken = new JavaScriptSerializer().Deserialize<Dictionary<string, object>>(body)["access_token"].ToString();
		}

		return accessToken;
	}
}
```

**Use the token.** Send `Authorization: Bearer <access token>` — the change at the use site in part 1 — with `X-CSRF-Header: -` on REST and Kepler calls.

**On expiry.** With Allow Refresh Tokens enabled on the OAuth2 Client and PKCE used, the application holds a refresh token and, when the access token expires, sends a `grant_type=refresh_token` request with it and the client credentials in place of the code exchange — *inferred* from the standard OAuth shape, not tested. The exchange example above reads only the `access_token` of the response; to renew with a refresh token, read the `refresh_token` from the same response as well and keep it as a secret, and otherwise the application sends the user through the sign-in again when the access token expires. Relativity does not document a refresh token's lifetime, whether it rotates, how it is revoked, or whether a revocation endpoint exists, nor what disabling or deleting the client does to tokens already issued. A refresh token is a secret: store it as Secret Management (Part B of the Migration Documentation) describes, and on compromise disable the client and replace it. Enable refresh tokens only if the application must keep a session beyond one token lifetime without asking the user to sign in again.

**PKCE.** PKCE is required by Relativity's own documentation to obtain a refresh token, and not otherwise: a Code request that does not ask for a refresh token works without it, **verified by NSerio against a live instance; not documented by Relativity**. It is never a blanket requirement. NSerio recommends PKCE with the `S256` method in both variants (NSerio recommendation), and does not claim that Relativity accepts any particular method, which it does not document.

**Scope.** No scope set is prescribed for Code. The scope below works — `openid RelativityRequestOrigin UserInfo UserInfoAccess AuthenticationID RelativityWeb` — **verified by NSerio against a live instance; not documented by Relativity**, verified as working and not as minimal. `state` is sent as standard OAuth practice and `nonce` is not used; Relativity does not document whether it enforces either.

**The application stops collecting the password.** After this change the application never receives, stores or forwards the user's username and password: the user signs in on RelativityOne's own page, and the application holds only tokens. Remove the credential prompt, the form field and any storage of the password that the replaced code needed.

**Single sign-on.** Sign-in happens on RelativityOne's own login page, so the instance's single sign-on and two-factor rules apply unchanged — *inferred* from where the sign-in happens, and not documented for clients that customers create.

**The client secret is required.** Code does not work without a client secret: tested and rejected, **verified by NSerio against a live instance; not documented by Relativity**, which confirms Relativity's own statement that the grant requires one. It is displayed once and is stored as Secret Management (Part B of the Migration Documentation) describes.

**Server-side variant.** For a web application whose code runs on a server:

- **Redirect.** The application receives the code at one exact HTTPS URL per environment, a route of the application, with no wildcards (NSerio recommendation; Relativity's matching rules are not documented). The Relativity administrator registers the exact URL on the OAuth2 Client.
- **Secret and tokens.** The client secret stays on the server and never reaches the browser. The user's tokens are held server-side, keyed to the session, and never in the browser.
- **Exchange.** The server performs the code exchange itself.

**Desktop variant.** For a desktop or command-line application run by a user on their own machine:

- **The system browser.** Open the authorize URL in the user's default browser and not in a web view the application hosts, so the application never sees the password (NSerio recommendation).
- **A loopback redirect on a fixed port.** Listen on the loopback address only, on the registered port, for one request. The Redirect URL is `http://127.0.0.1:<port>/<path>/`, ending with a slash because the listener below registers it as its prefix: a plain-HTTP loopback URL is accepted, **verified by NSerio against a live instance; not documented by Relativity**. The Relativity administrator registers that exact URL; because it is registered in advance, the port is fixed and not chosen at runtime.

```csharp
using System;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Net;
using System.Threading.Tasks;

public static class LoopbackAuthorizationReceiver
{
	private const string _CODE_PARAMETER = "code";
	private const string _STATE_PARAMETER = "state";
	private const string _ERROR_PARAMETER = "error";
	private const string _ERROR_DESCRIPTION_PARAMETER = "error_description";
	private const string _STATE_MISMATCH_MESSAGE = "The returned state is not the one that was sent; the response is rejected.";
	private const string _MISSING_CODE_MESSAGE = "The response carries no authorization code.";

	// Waits for the one request the browser sends to the registered loopback Redirect URL, and returns its code
	// only when its state is the one that was sent on the authorize request and the response carries no error.
	// redirectUrl is the exact URL registered on the OAuth2 Client, for example http://127.0.0.1:<port>/<path>/ :
	// it ends with a slash, as a listener prefix must.
	public static async Task<string> ReceiveAuthorizationCodeAsync(string authorizeUrl, string redirectUrl, string expectedState)
	{
		string authorizationCode;
		using (HttpListener listener = new HttpListener())
		{
			listener.Prefixes.Add(redirectUrl);
			listener.Start();
			Process.Start(new ProcessStartInfo(authorizeUrl) { UseShellExecute = true })?.Dispose();
			HttpListenerContext context = await listener.GetContextAsync();
			NameValueCollection query = context.Request.QueryString;
			context.Response.Close();
			if (query[_STATE_PARAMETER] != expectedState)
			{
				throw new InvalidOperationException(_STATE_MISMATCH_MESSAGE);
			}

			if (!string.IsNullOrEmpty(query[_ERROR_PARAMETER]))
			{
				throw new InvalidOperationException($"The authorization server returned an error: {query[_ERROR_PARAMETER]} {query[_ERROR_DESCRIPTION_PARAMETER]}".TrimEnd());
			}

			authorizationCode = query[_CODE_PARAMETER];
			if (string.IsNullOrEmpty(authorizationCode))
			{
				throw new InvalidOperationException(_MISSING_CODE_MESSAGE);
			}
		}

		return authorizationCode;
	}
}
```

- **The secret is extractable.** Code requires a client secret (above), so a distributed desktop application holds one, and **a secret shipped inside a distributed binary can be extracted by anyone who holds the binary**. It protects nothing against the person running the application.
- **Compensating guidance** (NSerio recommendation, subject to your own security policy): set Trusted IPs where the users' outbound addresses are known and stable — they restrict where a token can be *requested*, and are checked when a token is issued against the client's ID and secret; use the recommended short Access Token Lifetime of 60 minutes; enable refresh tokens only when needed; and create one OAuth2 Client per distribution, so a leaked secret can be disabled without affecting the others.
- **A documentation gap.** Relativity documents no recommendation for a distributed desktop application; this design is what NSerio verified works.

Verification: **Verification note — seven things.**

**This match is the least characterized of any other class.** The consumer was not identified, so neither the scheme nor the destination has been established. Treat it as the location of a credential to trace, not as a construct whose migration is known.

**Confirm the exchange was Basic.** `NetworkCredential` declares no scheme; the same type serves integrated Windows authentication, and the consumers above perform the exchange themselves with no `Basic` in the source. If the exchange is integrated, the migration does not apply.

**The 401-challenge response is lost** wherever the consumer answered a challenge with this credential; a bearer header is sent up front and answers nothing.

**Redirects stop carrying the credential.** Depending on the runtime and the credential type, a consumer's credentials may be re-applied to a redirect target; a manually set `Authorization` header never is. .NET documents that `HttpWebRequest` clears the `Authorization` header when it follows a redirect automatically, and the `HttpClient` handler drops it on redirect. Code that relied on a redirect can start returning 401 after the migration; address the final URI, or handle the redirect explicitly.

**A SOAP service may not accept bearer at all.** Confirm it does before changing a binding.

**Confirm the endpoint in use is the RelativityOne REST API.** That is the API whose bearer token this guidance describes; an endpoint elsewhere reached from the same code may accept only Basic and reject the migrated call.

**A token has a finite lifetime.** A header must be set per request to be refreshable: a header set once on a long-lived consumer works for the token's lifetime and then returns 401 until the process restarts.

OAuth2 Client: See [OAuth2 Client for Authorization Code](#oauth2-client-for-authorization-code).
<!-- generated:end CS-008 authorization-code -->

### CS-009 · Base64UserPasswordPair

<a id="cs-009-migration"></a>

#### Client Credentials

<!-- generated:begin CS-009 client-credentials -->
**Flow.** This entry is for Client Credentials. The "Which flow" paragraph of CS-009 in [Part A](part-a-csharp.md#cs-009) says when a match belongs to this flow.

Source form:
```csharp
string pair = $"{userName}:{password}";
string encoded = Convert.ToBase64String(Encoding.ASCII.GetBytes(pair));
```

Under token authentication: **First, the destination:** if it is not RelativityOne, no migration to RelativityOne applies — that service's own token mechanism is the migration — and the rest of this entry does not apply.

**Only if this encoding feeds an `Authorization` header.** Base64 over a colon-separated pair is not always a credential: the same shape encodes other data, and it is Basic Authentication only where the encoded value becomes the header. Where it does, **delete the encoding**: bearer carries the token verbatim, with no Base64 of a credential pair, and the header that received `encoded` receives the token, set per request:

```csharp
request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
```

Credentials: **Where the credentials come from.** The destination decides whether the migration applies. If the destination is not RelativityOne, the migration is that service's own token mechanism, and no migration to RelativityOne applies. If it is RelativityOne, the token comes from the token-acquisition part that follows.

**Where the token comes from.** Create an OAuth2 Client with Flow Grant Type set to Client Credentials (part 3 lists its settings), then send a form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=client_credentials`, `client_id`, `client_secret` and a `scope`. The token endpoint is **documented** for this grant. The token is then sent as `Authorization: Bearer <token>`, with `X-CSRF-Header: -` on REST and Kepler calls — the header is documented as required, and a Kepler call without it returns 404.

**.NET** (`HttpClient` and `System.Text.Json`). The class requests the token with the client ID and the client secret, caches it, requests a new one 60 seconds before it expires, and, when a call is rejected for authentication, requests a new one and sends the call once more. Build one instance for the life of the process and send every call through `SendAsync`; the request is created by the delegate on each attempt, because a request message cannot be sent twice. The client ID and the client secret come from the process environment or a secret store, never from source. Where the code uses the Relativity SDK, `GetAccessTokenAsync(false)` gives the token for `BearerTokenCredentials` each time a `ServiceFactory` is built.

```csharp
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

public sealed class AccessTokenProvider : IDisposable
{
	private const string _TOKEN_PATH = "/Relativity/Identity/connect/token";
	private const string _BEARER_SCHEME = "Bearer";
	private const string _CSRF_HEADER_NAME = "X-CSRF-Header";
	private const string _CSRF_HEADER_VALUE = "-";
	private static readonly TimeSpan _renewalMargin = TimeSpan.FromSeconds(60);

	private readonly HttpClient _client = new HttpClient();
	private readonly SemaphoreSlim _gate = new SemaphoreSlim(1, 1);
	private readonly string _host;
	private readonly string _clientId;
	private readonly string _clientSecret;
	private readonly string _scope;
	private string _accessToken;
	private DateTimeOffset _expiresAt;

	public AccessTokenProvider(string host, string clientId, string clientSecret, string scope)
	{
		_host = host;
		_clientId = clientId;
		_clientSecret = clientSecret;
		_scope = scope;
	}

	// Cached token; renewed shortly before it expires, or on demand after a rejected call.
	public async Task<string> GetAccessTokenAsync(bool forceRenewal)
	{
		await _gate.WaitAsync();
		try
		{
			if (forceRenewal || _accessToken == null || DateTimeOffset.UtcNow >= _expiresAt - _renewalMargin)
			{
				await AcquireAccessTokenAsync();
			}

			return _accessToken;
		}
		finally
		{
			_gate.Release();
		}
	}

	// Sends the request with the bearer token; when the call is rejected for authentication, renews the token and sends it once more.
	public async Task<HttpResponseMessage> SendAsync(Func<HttpRequestMessage> createRequest)
	{
		HttpResponseMessage response = await SendOnceAsync(createRequest, false);
		if (response.StatusCode == HttpStatusCode.Unauthorized)
		{
			response.Dispose();
			response = await SendOnceAsync(createRequest, true);
		}

		return response;
	}

	public void Dispose()
	{
		_client.Dispose();
		_gate.Dispose();
	}

	private async Task AcquireAccessTokenAsync()
	{
		using (FormUrlEncodedContent form = new FormUrlEncodedContent(new Dictionary<string, string>
		{
			{ "grant_type", "client_credentials" },
			{ "client_id", _clientId },
			{ "client_secret", _clientSecret },
			{ "scope", _scope }
		}))
		using (HttpResponseMessage response = await _client.PostAsync($"{_host}{_TOKEN_PATH}", form))
		{
			response.EnsureSuccessStatusCode();
			using (JsonDocument document = JsonDocument.Parse(await response.Content.ReadAsStringAsync()))
			{
				_accessToken = document.RootElement.GetProperty("access_token").GetString();
				_expiresAt = DateTimeOffset.UtcNow.AddSeconds(document.RootElement.GetProperty("expires_in").GetInt32());
			}
		}
	}

	private async Task<HttpResponseMessage> SendOnceAsync(Func<HttpRequestMessage> createRequest, bool forceRenewal)
	{
		string token = await GetAccessTokenAsync(forceRenewal);
		HttpResponseMessage response;
		using (HttpRequestMessage request = createRequest())
		{
			request.Headers.Authorization = new AuthenticationHeaderValue(_BEARER_SCHEME, token);
			request.Headers.Add(_CSRF_HEADER_NAME, _CSRF_HEADER_VALUE);
			response = await _client.SendAsync(request);
		}

		return response;
	}
}
```

**.NET Framework 4.7.2**: the same class with the serializer the runtime ships (`System.Web.Extensions`, referenced together with `System.Net.Http`), and C# 7.3 syntax:

```csharp
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

public sealed class AccessTokenProvider : IDisposable
{
	private const string _TOKEN_PATH = "/Relativity/Identity/connect/token";
	private const string _BEARER_SCHEME = "Bearer";
	private const string _CSRF_HEADER_NAME = "X-CSRF-Header";
	private const string _CSRF_HEADER_VALUE = "-";
	private static readonly TimeSpan _renewalMargin = TimeSpan.FromSeconds(60);

	private readonly HttpClient _client = new HttpClient();
	private readonly SemaphoreSlim _gate = new SemaphoreSlim(1, 1);
	private readonly string _host;
	private readonly string _clientId;
	private readonly string _clientSecret;
	private readonly string _scope;
	private string _accessToken;
	private DateTimeOffset _expiresAt;

	public AccessTokenProvider(string host, string clientId, string clientSecret, string scope)
	{
		_host = host;
		_clientId = clientId;
		_clientSecret = clientSecret;
		_scope = scope;
	}

	// Cached token; renewed shortly before it expires, or on demand after a rejected call.
	public async Task<string> GetAccessTokenAsync(bool forceRenewal)
	{
		await _gate.WaitAsync();
		try
		{
			if (forceRenewal || _accessToken == null || DateTimeOffset.UtcNow >= _expiresAt - _renewalMargin)
			{
				await AcquireAccessTokenAsync();
			}

			return _accessToken;
		}
		finally
		{
			_gate.Release();
		}
	}

	// Sends the request with the bearer token; when the call is rejected for authentication, renews the token and sends it once more.
	public async Task<HttpResponseMessage> SendAsync(Func<HttpRequestMessage> createRequest)
	{
		HttpResponseMessage response = await SendOnceAsync(createRequest, false);
		if (response.StatusCode == HttpStatusCode.Unauthorized)
		{
			response.Dispose();
			response = await SendOnceAsync(createRequest, true);
		}

		return response;
	}

	public void Dispose()
	{
		_client.Dispose();
		_gate.Dispose();
	}

	private async Task AcquireAccessTokenAsync()
	{
		using (FormUrlEncodedContent form = new FormUrlEncodedContent(new Dictionary<string, string>
		{
			{ "grant_type", "client_credentials" },
			{ "client_id", _clientId },
			{ "client_secret", _clientSecret },
			{ "scope", _scope }
		}))
		using (HttpResponseMessage response = await _client.PostAsync($"{_host}{_TOKEN_PATH}", form))
		{
			response.EnsureSuccessStatusCode();
			string body = await response.Content.ReadAsStringAsync();
			Dictionary<string, object> fields = new JavaScriptSerializer().Deserialize<Dictionary<string, object>>(body);
			_accessToken = fields["access_token"].ToString();
			_expiresAt = DateTimeOffset.UtcNow.AddSeconds(Convert.ToInt32(fields["expires_in"]));
		}
	}

	private async Task<HttpResponseMessage> SendOnceAsync(Func<HttpRequestMessage> createRequest, bool forceRenewal)
	{
		string token = await GetAccessTokenAsync(forceRenewal);
		HttpResponseMessage response;
		using (HttpRequestMessage request = createRequest())
		{
			request.Headers.Authorization = new AuthenticationHeaderValue(_BEARER_SCHEME, token);
			request.Headers.Add(_CSRF_HEADER_NAME, _CSRF_HEADER_VALUE);
			response = await _client.SendAsync(request);
		}

		return response;
	}
}
```

**The scope depends on what the integration needs, and no single scope is required.** Relativity's documentation shows two values for this grant: `SystemUserInfo` on the REST authentication page, the Usage Reports page and in the official Transfer SDK sample, and `UserInfoAccess` in the Kepler .NET samples. No page says which is correct, whether both work, or what either grants, and the OAuth2 Client's `Scopes` property is documented as one that cannot be set. Use the value the API you call documents, and confirm it.

**Cache the token, and re-acquire it on expiry — do not refresh.** Keep the token for its lifetime (part 3 recommends 60 minutes, Relativity's own wording) and request a new one with the client secret shortly before it expires (the JavaScript, TypeScript and Python examples use 60 seconds), and once when a call is rejected for authentication. Client Credentials does not offer refresh tokens: **verified by NSerio against a live instance; not documented by Relativity** — the Allow Refresh Tokens control does not appear for this grant in the RelativityOne user interface. Relativity's own tables and text on refresh tokens conflict with each other, so re-acquisition with the secret is the advice that holds whichever way they are read.

**Long-running processes that use the Relativity SDK.** A `ServiceFactory` built once from a bearer token keeps that token after it expires. Rebuild the `ServiceFactory` and its proxies with a fresh token before expiry. This is the only advice consistent with the documentation: the package `Relativity.Kepler.Client.SDK` (documented) exposes a settable `Token` and an `HttpMessageHandler` overload but no token-provider overload, and no page documents renewing a token behind a factory.

**The client secret.** It is displayed once, at creation or reset. Store it as Secret Management (Part B of the Migration Documentation) describes; never in source, never in tracked configuration. A reset invalidates the previous secret immediately, so plan the cut-over before resetting it.

**Before deploying, confirm the calling identity.** Every call now runs as the OAuth2 Client's **Context User** — not as the user in the replaced credential, and for an import or a job as much as for a request. If that user has different workspace permissions or item-level security, the code returns fewer rows or `Unauthorized` in production with no change to its logic, and Relativity's audit attributes every call to that one account. Confirm the Context User's permissions match those of the previous identity. Relativity's REST API authentication page describes the Context User as usually a member of the Relativity Administrators group; the OAuth2 Client block (part 3) gives NSerio's recommendation, which differs.

**When the integration acts for a signed-in user.** Client Credentials is one service identity by construction. An integration that acts on behalf of a signed-in user is the Authorization Code case, and moving to it means a new OAuth2 Client, because the flow is fixed when a client is created. Which of the two flows applies depends on who supplies the credential at run time.

Verification: **Verification note — four things.**

**Confirm the pair is a credential sent outward.** A service of the client's that *checks* Basic from its own callers is inbound authentication, which has nothing to migrate to Relativity OAuth2, and the rule does not cover it; it can still surface here, and such a match is not a migration.

**The header write may be a separate match.** Where the header is written in the same file, this encoding and that header are two matches at two locations. They are one migration, and both locations change together.

**A token has a finite lifetime, and a header set once on a long-lived client cannot be refreshed.** Set it per request, or the code works for the token's lifetime and then returns 401 until the process restarts.

**Confirm the endpoint in use is the RelativityOne REST API.** That is the API whose bearer token this guidance describes; an endpoint elsewhere reached from the same code may accept only Basic and reject the migrated call.

OAuth2 Client: See [OAuth2 Client for Client Credentials](#oauth2-client-for-client-credentials).
<!-- generated:end CS-009 client-credentials -->

#### Authorization Code

<!-- generated:begin CS-009 authorization-code -->
**Flow.** This entry is for Authorization Code. The "Which flow" paragraph of CS-009 in [Part A](part-a-csharp.md#cs-009) says when a match belongs to this flow.

Source form:
```csharp
string pair = $"{userName}:{password}";
string encoded = Convert.ToBase64String(Encoding.ASCII.GetBytes(pair));
```

Under token authentication: **First, the destination:** if it is not RelativityOne, no migration to RelativityOne applies — that service's own token mechanism is the migration — and the rest of this entry does not apply.

**Only if this encoding feeds an `Authorization` header.** Base64 over a colon-separated pair is not always a credential: the same shape encodes other data, and it is Basic Authentication only where the encoded value becomes the header. Where it does, **delete the encoding**: bearer carries the token verbatim, with no Base64 of a credential pair, and the header that received `encoded` receives the token, set per request:

```csharp
request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
```

Credentials: **Where the credentials come from.** The destination decides whether the migration applies. If the destination is not RelativityOne, the migration is that service's own token mechanism, and no migration to RelativityOne applies. If it is RelativityOne, the token comes from the token-acquisition part that follows.

**Common to both variants**

**Sign in.** Replace the code that collected the user's credentials with a redirect: send the user's browser to the authorize endpoint `<host>/Relativity/Identity/connect/authorize`, which leads to the RelativityOne login page. The authorize endpoint is **verified by NSerio against a live instance; not documented by Relativity**. The request carries the standard authorization-code parameters — `client_id`, `response_type=code`, the registered `redirect_uri`, a `scope`, and a `state` value the application checks on return; these are standard OAuth, and Relativity documents none of them. No discovery document is relied on.

**Receive the code.** The browser returns to the Redirect URL registered on the OAuth2 Client, with the authorization code and the `state` value. Check the `state` before using the code. The Redirect URL must be exactly the one registered (the variants say how).

**Exchange the code.** Send a form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=authorization_code`, the `code`, the same `redirect_uri`, the `client_id` and the `client_secret`, plus the `code_verifier` when PKCE is used. The code is exchanged at that endpoint, which Relativity documents for the Client Credentials grant only; the exchange is **verified by NSerio against a live instance; not documented by Relativity**. Send the `code_verifier` only when the authorize request carried a `code_challenge`; otherwise leave it out.

**.NET** (`HttpClient` and `System.Text.Json`):

```csharp
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

public sealed class AuthorizationCodeRequest
{
	public string Host { get; set; }
	public string AuthorizationCode { get; set; }
	public string RedirectUri { get; set; }
	public string ClientId { get; set; }
	public string ClientSecret { get; set; }

	// The PKCE verifier; null when no code_challenge was sent on the authorize request.
	public string CodeVerifier { get; set; }
}

public static class AuthorizationCodeExchange
{
	private const string _TOKEN_PATH = "/Relativity/Identity/connect/token";

	public static async Task<string> ExchangeAuthorizationCodeAsync(HttpClient client, AuthorizationCodeRequest request)
	{
		Dictionary<string, string> fields = new Dictionary<string, string>
		{
			{ "grant_type", "authorization_code" },
			{ "code", request.AuthorizationCode },
			{ "redirect_uri", request.RedirectUri },
			{ "client_id", request.ClientId },
			{ "client_secret", request.ClientSecret }
		};
		if (!string.IsNullOrEmpty(request.CodeVerifier))
		{
			fields["code_verifier"] = request.CodeVerifier;
		}

		string accessToken;
		using (FormUrlEncodedContent form = new FormUrlEncodedContent(fields))
		using (HttpResponseMessage response = await client.PostAsync($"{request.Host}{_TOKEN_PATH}", form))
		{
			response.EnsureSuccessStatusCode();
			using (JsonDocument document = JsonDocument.Parse(await response.Content.ReadAsStringAsync()))
			{
				accessToken = document.RootElement.GetProperty("access_token").GetString();
			}
		}

		return accessToken;
	}
}
```

**.NET Framework 4.7.2** (`System.Web.Extensions` and C# 7.3 syntax):

```csharp
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

public sealed class AuthorizationCodeRequest
{
	public string Host { get; set; }
	public string AuthorizationCode { get; set; }
	public string RedirectUri { get; set; }
	public string ClientId { get; set; }
	public string ClientSecret { get; set; }

	// The PKCE verifier; null when no code_challenge was sent on the authorize request.
	public string CodeVerifier { get; set; }
}

public static class AuthorizationCodeExchange
{
	private const string _TOKEN_PATH = "/Relativity/Identity/connect/token";

	public static async Task<string> ExchangeAuthorizationCodeAsync(HttpClient client, AuthorizationCodeRequest request)
	{
		Dictionary<string, string> fields = new Dictionary<string, string>
		{
			{ "grant_type", "authorization_code" },
			{ "code", request.AuthorizationCode },
			{ "redirect_uri", request.RedirectUri },
			{ "client_id", request.ClientId },
			{ "client_secret", request.ClientSecret }
		};
		if (!string.IsNullOrEmpty(request.CodeVerifier))
		{
			fields["code_verifier"] = request.CodeVerifier;
		}

		string accessToken;
		using (FormUrlEncodedContent form = new FormUrlEncodedContent(fields))
		using (HttpResponseMessage response = await client.PostAsync($"{request.Host}{_TOKEN_PATH}", form))
		{
			response.EnsureSuccessStatusCode();
			string body = await response.Content.ReadAsStringAsync();
			accessToken = new JavaScriptSerializer().Deserialize<Dictionary<string, object>>(body)["access_token"].ToString();
		}

		return accessToken;
	}
}
```

**Use the token.** Send `Authorization: Bearer <access token>` — the change at the use site in part 1 — with `X-CSRF-Header: -` on REST and Kepler calls.

**On expiry.** With Allow Refresh Tokens enabled on the OAuth2 Client and PKCE used, the application holds a refresh token and, when the access token expires, sends a `grant_type=refresh_token` request with it and the client credentials in place of the code exchange — *inferred* from the standard OAuth shape, not tested. The exchange example above reads only the `access_token` of the response; to renew with a refresh token, read the `refresh_token` from the same response as well and keep it as a secret, and otherwise the application sends the user through the sign-in again when the access token expires. Relativity does not document a refresh token's lifetime, whether it rotates, how it is revoked, or whether a revocation endpoint exists, nor what disabling or deleting the client does to tokens already issued. A refresh token is a secret: store it as Secret Management (Part B of the Migration Documentation) describes, and on compromise disable the client and replace it. Enable refresh tokens only if the application must keep a session beyond one token lifetime without asking the user to sign in again.

**PKCE.** PKCE is required by Relativity's own documentation to obtain a refresh token, and not otherwise: a Code request that does not ask for a refresh token works without it, **verified by NSerio against a live instance; not documented by Relativity**. It is never a blanket requirement. NSerio recommends PKCE with the `S256` method in both variants (NSerio recommendation), and does not claim that Relativity accepts any particular method, which it does not document.

**Scope.** No scope set is prescribed for Code. The scope below works — `openid RelativityRequestOrigin UserInfo UserInfoAccess AuthenticationID RelativityWeb` — **verified by NSerio against a live instance; not documented by Relativity**, verified as working and not as minimal. `state` is sent as standard OAuth practice and `nonce` is not used; Relativity does not document whether it enforces either.

**The application stops collecting the password.** After this change the application never receives, stores or forwards the user's username and password: the user signs in on RelativityOne's own page, and the application holds only tokens. Remove the credential prompt, the form field and any storage of the password that the replaced code needed.

**Single sign-on.** Sign-in happens on RelativityOne's own login page, so the instance's single sign-on and two-factor rules apply unchanged — *inferred* from where the sign-in happens, and not documented for clients that customers create.

**The client secret is required.** Code does not work without a client secret: tested and rejected, **verified by NSerio against a live instance; not documented by Relativity**, which confirms Relativity's own statement that the grant requires one. It is displayed once and is stored as Secret Management (Part B of the Migration Documentation) describes.

**Server-side variant.** For a web application whose code runs on a server:

- **Redirect.** The application receives the code at one exact HTTPS URL per environment, a route of the application, with no wildcards (NSerio recommendation; Relativity's matching rules are not documented). The Relativity administrator registers the exact URL on the OAuth2 Client.
- **Secret and tokens.** The client secret stays on the server and never reaches the browser. The user's tokens are held server-side, keyed to the session, and never in the browser.
- **Exchange.** The server performs the code exchange itself.

**Desktop variant.** For a desktop or command-line application run by a user on their own machine:

- **The system browser.** Open the authorize URL in the user's default browser and not in a web view the application hosts, so the application never sees the password (NSerio recommendation).
- **A loopback redirect on a fixed port.** Listen on the loopback address only, on the registered port, for one request. The Redirect URL is `http://127.0.0.1:<port>/<path>/`, ending with a slash because the listener below registers it as its prefix: a plain-HTTP loopback URL is accepted, **verified by NSerio against a live instance; not documented by Relativity**. The Relativity administrator registers that exact URL; because it is registered in advance, the port is fixed and not chosen at runtime.

```csharp
using System;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Net;
using System.Threading.Tasks;

public static class LoopbackAuthorizationReceiver
{
	private const string _CODE_PARAMETER = "code";
	private const string _STATE_PARAMETER = "state";
	private const string _ERROR_PARAMETER = "error";
	private const string _ERROR_DESCRIPTION_PARAMETER = "error_description";
	private const string _STATE_MISMATCH_MESSAGE = "The returned state is not the one that was sent; the response is rejected.";
	private const string _MISSING_CODE_MESSAGE = "The response carries no authorization code.";

	// Waits for the one request the browser sends to the registered loopback Redirect URL, and returns its code
	// only when its state is the one that was sent on the authorize request and the response carries no error.
	// redirectUrl is the exact URL registered on the OAuth2 Client, for example http://127.0.0.1:<port>/<path>/ :
	// it ends with a slash, as a listener prefix must.
	public static async Task<string> ReceiveAuthorizationCodeAsync(string authorizeUrl, string redirectUrl, string expectedState)
	{
		string authorizationCode;
		using (HttpListener listener = new HttpListener())
		{
			listener.Prefixes.Add(redirectUrl);
			listener.Start();
			Process.Start(new ProcessStartInfo(authorizeUrl) { UseShellExecute = true })?.Dispose();
			HttpListenerContext context = await listener.GetContextAsync();
			NameValueCollection query = context.Request.QueryString;
			context.Response.Close();
			if (query[_STATE_PARAMETER] != expectedState)
			{
				throw new InvalidOperationException(_STATE_MISMATCH_MESSAGE);
			}

			if (!string.IsNullOrEmpty(query[_ERROR_PARAMETER]))
			{
				throw new InvalidOperationException($"The authorization server returned an error: {query[_ERROR_PARAMETER]} {query[_ERROR_DESCRIPTION_PARAMETER]}".TrimEnd());
			}

			authorizationCode = query[_CODE_PARAMETER];
			if (string.IsNullOrEmpty(authorizationCode))
			{
				throw new InvalidOperationException(_MISSING_CODE_MESSAGE);
			}
		}

		return authorizationCode;
	}
}
```

- **The secret is extractable.** Code requires a client secret (above), so a distributed desktop application holds one, and **a secret shipped inside a distributed binary can be extracted by anyone who holds the binary**. It protects nothing against the person running the application.
- **Compensating guidance** (NSerio recommendation, subject to your own security policy): set Trusted IPs where the users' outbound addresses are known and stable — they restrict where a token can be *requested*, and are checked when a token is issued against the client's ID and secret; use the recommended short Access Token Lifetime of 60 minutes; enable refresh tokens only when needed; and create one OAuth2 Client per distribution, so a leaked secret can be disabled without affecting the others.
- **A documentation gap.** Relativity documents no recommendation for a distributed desktop application; this design is what NSerio verified works.

Verification: **Verification note — four things.**

**Confirm the pair is a credential sent outward.** A service of the client's that *checks* Basic from its own callers is inbound authentication, which has nothing to migrate to Relativity OAuth2, and the rule does not cover it; it can still surface here, and such a match is not a migration.

**The header write may be a separate match.** Where the header is written in the same file, this encoding and that header are two matches at two locations. They are one migration, and both locations change together.

**A token has a finite lifetime, and a header set once on a long-lived client cannot be refreshed.** Set it per request, or the code works for the token's lifetime and then returns 401 until the process restarts.

**Confirm the endpoint in use is the RelativityOne REST API.** That is the API whose bearer token this guidance describes; an endpoint elsewhere reached from the same code may accept only Basic and reject the migrated call.

OAuth2 Client: See [OAuth2 Client for Authorization Code](#oauth2-client-for-authorization-code).
<!-- generated:end CS-009 authorization-code -->

### CS-012 · HttpRequestHeaderEnumAuthorization

<a id="cs-012-migration"></a>

#### Client Credentials

<!-- generated:begin CS-012 client-credentials -->
**Flow.** This entry is for Client Credentials. The "Which flow" paragraph of CS-012 in [Part A](part-a-csharp.md#cs-012) says when a match belongs to this flow.

Source form:
```csharp
client.Headers[HttpRequestHeader.Authorization] = "Basic " + encoded;    // WebClient
request.Headers[HttpRequestHeader.Authorization] = "Basic " + encoded;   // HttpWebRequest
```

Under token authentication: **First, the destination:** if it is not RelativityOne, no migration to RelativityOne applies — that service's own token mechanism is the migration — and the rest of this entry does not apply.

`"Bearer "` and the access token in place of `"Basic "` and the encoded pair, **set per request**:

```csharp
client.Headers[HttpRequestHeader.Authorization] = "Bearer " + accessToken;    // WebClient, before each call
request.Headers[HttpRequestHeader.Authorization] = "Bearer " + accessToken;   // HttpWebRequest
```

The code that produced `encoded` has no counterpart: a bearer token travels verbatim.

Credentials: **Where the credentials come from.** The destination decides whether the migration applies. If the destination is not RelativityOne, the migration is that service's own token mechanism, and no migration to RelativityOne applies. If it is RelativityOne, the token comes from the token-acquisition part that follows.

**Where the token comes from.** Create an OAuth2 Client with Flow Grant Type set to Client Credentials (part 3 lists its settings), then send a form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=client_credentials`, `client_id`, `client_secret` and a `scope`. The token endpoint is **documented** for this grant. The token is then sent as `Authorization: Bearer <token>`, with `X-CSRF-Header: -` on REST and Kepler calls — the header is documented as required, and a Kepler call without it returns 404.

**.NET** (`HttpClient` and `System.Text.Json`). The class requests the token with the client ID and the client secret, caches it, requests a new one 60 seconds before it expires, and, when a call is rejected for authentication, requests a new one and sends the call once more. Build one instance for the life of the process and send every call through `SendAsync`; the request is created by the delegate on each attempt, because a request message cannot be sent twice. The client ID and the client secret come from the process environment or a secret store, never from source. Where the code uses the Relativity SDK, `GetAccessTokenAsync(false)` gives the token for `BearerTokenCredentials` each time a `ServiceFactory` is built.

```csharp
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

public sealed class AccessTokenProvider : IDisposable
{
	private const string _TOKEN_PATH = "/Relativity/Identity/connect/token";
	private const string _BEARER_SCHEME = "Bearer";
	private const string _CSRF_HEADER_NAME = "X-CSRF-Header";
	private const string _CSRF_HEADER_VALUE = "-";
	private static readonly TimeSpan _renewalMargin = TimeSpan.FromSeconds(60);

	private readonly HttpClient _client = new HttpClient();
	private readonly SemaphoreSlim _gate = new SemaphoreSlim(1, 1);
	private readonly string _host;
	private readonly string _clientId;
	private readonly string _clientSecret;
	private readonly string _scope;
	private string _accessToken;
	private DateTimeOffset _expiresAt;

	public AccessTokenProvider(string host, string clientId, string clientSecret, string scope)
	{
		_host = host;
		_clientId = clientId;
		_clientSecret = clientSecret;
		_scope = scope;
	}

	// Cached token; renewed shortly before it expires, or on demand after a rejected call.
	public async Task<string> GetAccessTokenAsync(bool forceRenewal)
	{
		await _gate.WaitAsync();
		try
		{
			if (forceRenewal || _accessToken == null || DateTimeOffset.UtcNow >= _expiresAt - _renewalMargin)
			{
				await AcquireAccessTokenAsync();
			}

			return _accessToken;
		}
		finally
		{
			_gate.Release();
		}
	}

	// Sends the request with the bearer token; when the call is rejected for authentication, renews the token and sends it once more.
	public async Task<HttpResponseMessage> SendAsync(Func<HttpRequestMessage> createRequest)
	{
		HttpResponseMessage response = await SendOnceAsync(createRequest, false);
		if (response.StatusCode == HttpStatusCode.Unauthorized)
		{
			response.Dispose();
			response = await SendOnceAsync(createRequest, true);
		}

		return response;
	}

	public void Dispose()
	{
		_client.Dispose();
		_gate.Dispose();
	}

	private async Task AcquireAccessTokenAsync()
	{
		using (FormUrlEncodedContent form = new FormUrlEncodedContent(new Dictionary<string, string>
		{
			{ "grant_type", "client_credentials" },
			{ "client_id", _clientId },
			{ "client_secret", _clientSecret },
			{ "scope", _scope }
		}))
		using (HttpResponseMessage response = await _client.PostAsync($"{_host}{_TOKEN_PATH}", form))
		{
			response.EnsureSuccessStatusCode();
			using (JsonDocument document = JsonDocument.Parse(await response.Content.ReadAsStringAsync()))
			{
				_accessToken = document.RootElement.GetProperty("access_token").GetString();
				_expiresAt = DateTimeOffset.UtcNow.AddSeconds(document.RootElement.GetProperty("expires_in").GetInt32());
			}
		}
	}

	private async Task<HttpResponseMessage> SendOnceAsync(Func<HttpRequestMessage> createRequest, bool forceRenewal)
	{
		string token = await GetAccessTokenAsync(forceRenewal);
		HttpResponseMessage response;
		using (HttpRequestMessage request = createRequest())
		{
			request.Headers.Authorization = new AuthenticationHeaderValue(_BEARER_SCHEME, token);
			request.Headers.Add(_CSRF_HEADER_NAME, _CSRF_HEADER_VALUE);
			response = await _client.SendAsync(request);
		}

		return response;
	}
}
```

**.NET Framework 4.7.2**: the same class with the serializer the runtime ships (`System.Web.Extensions`, referenced together with `System.Net.Http`), and C# 7.3 syntax:

```csharp
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

public sealed class AccessTokenProvider : IDisposable
{
	private const string _TOKEN_PATH = "/Relativity/Identity/connect/token";
	private const string _BEARER_SCHEME = "Bearer";
	private const string _CSRF_HEADER_NAME = "X-CSRF-Header";
	private const string _CSRF_HEADER_VALUE = "-";
	private static readonly TimeSpan _renewalMargin = TimeSpan.FromSeconds(60);

	private readonly HttpClient _client = new HttpClient();
	private readonly SemaphoreSlim _gate = new SemaphoreSlim(1, 1);
	private readonly string _host;
	private readonly string _clientId;
	private readonly string _clientSecret;
	private readonly string _scope;
	private string _accessToken;
	private DateTimeOffset _expiresAt;

	public AccessTokenProvider(string host, string clientId, string clientSecret, string scope)
	{
		_host = host;
		_clientId = clientId;
		_clientSecret = clientSecret;
		_scope = scope;
	}

	// Cached token; renewed shortly before it expires, or on demand after a rejected call.
	public async Task<string> GetAccessTokenAsync(bool forceRenewal)
	{
		await _gate.WaitAsync();
		try
		{
			if (forceRenewal || _accessToken == null || DateTimeOffset.UtcNow >= _expiresAt - _renewalMargin)
			{
				await AcquireAccessTokenAsync();
			}

			return _accessToken;
		}
		finally
		{
			_gate.Release();
		}
	}

	// Sends the request with the bearer token; when the call is rejected for authentication, renews the token and sends it once more.
	public async Task<HttpResponseMessage> SendAsync(Func<HttpRequestMessage> createRequest)
	{
		HttpResponseMessage response = await SendOnceAsync(createRequest, false);
		if (response.StatusCode == HttpStatusCode.Unauthorized)
		{
			response.Dispose();
			response = await SendOnceAsync(createRequest, true);
		}

		return response;
	}

	public void Dispose()
	{
		_client.Dispose();
		_gate.Dispose();
	}

	private async Task AcquireAccessTokenAsync()
	{
		using (FormUrlEncodedContent form = new FormUrlEncodedContent(new Dictionary<string, string>
		{
			{ "grant_type", "client_credentials" },
			{ "client_id", _clientId },
			{ "client_secret", _clientSecret },
			{ "scope", _scope }
		}))
		using (HttpResponseMessage response = await _client.PostAsync($"{_host}{_TOKEN_PATH}", form))
		{
			response.EnsureSuccessStatusCode();
			string body = await response.Content.ReadAsStringAsync();
			Dictionary<string, object> fields = new JavaScriptSerializer().Deserialize<Dictionary<string, object>>(body);
			_accessToken = fields["access_token"].ToString();
			_expiresAt = DateTimeOffset.UtcNow.AddSeconds(Convert.ToInt32(fields["expires_in"]));
		}
	}

	private async Task<HttpResponseMessage> SendOnceAsync(Func<HttpRequestMessage> createRequest, bool forceRenewal)
	{
		string token = await GetAccessTokenAsync(forceRenewal);
		HttpResponseMessage response;
		using (HttpRequestMessage request = createRequest())
		{
			request.Headers.Authorization = new AuthenticationHeaderValue(_BEARER_SCHEME, token);
			request.Headers.Add(_CSRF_HEADER_NAME, _CSRF_HEADER_VALUE);
			response = await _client.SendAsync(request);
		}

		return response;
	}
}
```

**The scope depends on what the integration needs, and no single scope is required.** Relativity's documentation shows two values for this grant: `SystemUserInfo` on the REST authentication page, the Usage Reports page and in the official Transfer SDK sample, and `UserInfoAccess` in the Kepler .NET samples. No page says which is correct, whether both work, or what either grants, and the OAuth2 Client's `Scopes` property is documented as one that cannot be set. Use the value the API you call documents, and confirm it.

**Cache the token, and re-acquire it on expiry — do not refresh.** Keep the token for its lifetime (part 3 recommends 60 minutes, Relativity's own wording) and request a new one with the client secret shortly before it expires (the JavaScript, TypeScript and Python examples use 60 seconds), and once when a call is rejected for authentication. Client Credentials does not offer refresh tokens: **verified by NSerio against a live instance; not documented by Relativity** — the Allow Refresh Tokens control does not appear for this grant in the RelativityOne user interface. Relativity's own tables and text on refresh tokens conflict with each other, so re-acquisition with the secret is the advice that holds whichever way they are read.

**Long-running processes that use the Relativity SDK.** A `ServiceFactory` built once from a bearer token keeps that token after it expires. Rebuild the `ServiceFactory` and its proxies with a fresh token before expiry. This is the only advice consistent with the documentation: the package `Relativity.Kepler.Client.SDK` (documented) exposes a settable `Token` and an `HttpMessageHandler` overload but no token-provider overload, and no page documents renewing a token behind a factory.

**The client secret.** It is displayed once, at creation or reset. Store it as Secret Management (Part B of the Migration Documentation) describes; never in source, never in tracked configuration. A reset invalidates the previous secret immediately, so plan the cut-over before resetting it.

**Before deploying, confirm the calling identity.** Every call now runs as the OAuth2 Client's **Context User** — not as the user in the replaced credential, and for an import or a job as much as for a request. If that user has different workspace permissions or item-level security, the code returns fewer rows or `Unauthorized` in production with no change to its logic, and Relativity's audit attributes every call to that one account. Confirm the Context User's permissions match those of the previous identity. Relativity's REST API authentication page describes the Context User as usually a member of the Relativity Administrators group; the OAuth2 Client block (part 3) gives NSerio's recommendation, which differs.

**When the integration acts for a signed-in user.** Client Credentials is one service identity by construction. An integration that acts on behalf of a signed-in user is the Authorization Code case, and moving to it means a new OAuth2 Client, because the flow is fixed when a client is created. Which of the two flows applies depends on who supplies the credential at run time.

Verification: **Verification note — three things.**

**A token has a finite lifetime, and a header set once cannot be refreshed.** An `HttpWebRequest` is one request, so its header is already per request. A `WebClient` instance reused across calls is the long-lived case: set the header before each call with a current token, or the code works for the token's lifetime and then returns 401.

**The pair's encoding may be a separate match.** Where `encoded` is built in the same file, the encoding and this header are two matches at two locations. They are one migration, and both locations change together.

**Confirm the endpoint in use is the RelativityOne REST API.** That is the API whose bearer token this guidance describes; an endpoint elsewhere reached from the same code may accept only Basic and reject the migrated call.

OAuth2 Client: See [OAuth2 Client for Client Credentials](#oauth2-client-for-client-credentials).
<!-- generated:end CS-012 client-credentials -->

#### Authorization Code

<!-- generated:begin CS-012 authorization-code -->
**Flow.** This entry is for Authorization Code. The "Which flow" paragraph of CS-012 in [Part A](part-a-csharp.md#cs-012) says when a match belongs to this flow.

Source form:
```csharp
client.Headers[HttpRequestHeader.Authorization] = "Basic " + encoded;    // WebClient
request.Headers[HttpRequestHeader.Authorization] = "Basic " + encoded;   // HttpWebRequest
```

Under token authentication: **First, the destination:** if it is not RelativityOne, no migration to RelativityOne applies — that service's own token mechanism is the migration — and the rest of this entry does not apply.

`"Bearer "` and the access token in place of `"Basic "` and the encoded pair, **set per request**:

```csharp
client.Headers[HttpRequestHeader.Authorization] = "Bearer " + accessToken;    // WebClient, before each call
request.Headers[HttpRequestHeader.Authorization] = "Bearer " + accessToken;   // HttpWebRequest
```

The code that produced `encoded` has no counterpart: a bearer token travels verbatim.

Credentials: **Where the credentials come from.** The destination decides whether the migration applies. If the destination is not RelativityOne, the migration is that service's own token mechanism, and no migration to RelativityOne applies. If it is RelativityOne, the token comes from the token-acquisition part that follows.

**Common to both variants**

**Sign in.** Replace the code that collected the user's credentials with a redirect: send the user's browser to the authorize endpoint `<host>/Relativity/Identity/connect/authorize`, which leads to the RelativityOne login page. The authorize endpoint is **verified by NSerio against a live instance; not documented by Relativity**. The request carries the standard authorization-code parameters — `client_id`, `response_type=code`, the registered `redirect_uri`, a `scope`, and a `state` value the application checks on return; these are standard OAuth, and Relativity documents none of them. No discovery document is relied on.

**Receive the code.** The browser returns to the Redirect URL registered on the OAuth2 Client, with the authorization code and the `state` value. Check the `state` before using the code. The Redirect URL must be exactly the one registered (the variants say how).

**Exchange the code.** Send a form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=authorization_code`, the `code`, the same `redirect_uri`, the `client_id` and the `client_secret`, plus the `code_verifier` when PKCE is used. The code is exchanged at that endpoint, which Relativity documents for the Client Credentials grant only; the exchange is **verified by NSerio against a live instance; not documented by Relativity**. Send the `code_verifier` only when the authorize request carried a `code_challenge`; otherwise leave it out.

**.NET** (`HttpClient` and `System.Text.Json`):

```csharp
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

public sealed class AuthorizationCodeRequest
{
	public string Host { get; set; }
	public string AuthorizationCode { get; set; }
	public string RedirectUri { get; set; }
	public string ClientId { get; set; }
	public string ClientSecret { get; set; }

	// The PKCE verifier; null when no code_challenge was sent on the authorize request.
	public string CodeVerifier { get; set; }
}

public static class AuthorizationCodeExchange
{
	private const string _TOKEN_PATH = "/Relativity/Identity/connect/token";

	public static async Task<string> ExchangeAuthorizationCodeAsync(HttpClient client, AuthorizationCodeRequest request)
	{
		Dictionary<string, string> fields = new Dictionary<string, string>
		{
			{ "grant_type", "authorization_code" },
			{ "code", request.AuthorizationCode },
			{ "redirect_uri", request.RedirectUri },
			{ "client_id", request.ClientId },
			{ "client_secret", request.ClientSecret }
		};
		if (!string.IsNullOrEmpty(request.CodeVerifier))
		{
			fields["code_verifier"] = request.CodeVerifier;
		}

		string accessToken;
		using (FormUrlEncodedContent form = new FormUrlEncodedContent(fields))
		using (HttpResponseMessage response = await client.PostAsync($"{request.Host}{_TOKEN_PATH}", form))
		{
			response.EnsureSuccessStatusCode();
			using (JsonDocument document = JsonDocument.Parse(await response.Content.ReadAsStringAsync()))
			{
				accessToken = document.RootElement.GetProperty("access_token").GetString();
			}
		}

		return accessToken;
	}
}
```

**.NET Framework 4.7.2** (`System.Web.Extensions` and C# 7.3 syntax):

```csharp
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

public sealed class AuthorizationCodeRequest
{
	public string Host { get; set; }
	public string AuthorizationCode { get; set; }
	public string RedirectUri { get; set; }
	public string ClientId { get; set; }
	public string ClientSecret { get; set; }

	// The PKCE verifier; null when no code_challenge was sent on the authorize request.
	public string CodeVerifier { get; set; }
}

public static class AuthorizationCodeExchange
{
	private const string _TOKEN_PATH = "/Relativity/Identity/connect/token";

	public static async Task<string> ExchangeAuthorizationCodeAsync(HttpClient client, AuthorizationCodeRequest request)
	{
		Dictionary<string, string> fields = new Dictionary<string, string>
		{
			{ "grant_type", "authorization_code" },
			{ "code", request.AuthorizationCode },
			{ "redirect_uri", request.RedirectUri },
			{ "client_id", request.ClientId },
			{ "client_secret", request.ClientSecret }
		};
		if (!string.IsNullOrEmpty(request.CodeVerifier))
		{
			fields["code_verifier"] = request.CodeVerifier;
		}

		string accessToken;
		using (FormUrlEncodedContent form = new FormUrlEncodedContent(fields))
		using (HttpResponseMessage response = await client.PostAsync($"{request.Host}{_TOKEN_PATH}", form))
		{
			response.EnsureSuccessStatusCode();
			string body = await response.Content.ReadAsStringAsync();
			accessToken = new JavaScriptSerializer().Deserialize<Dictionary<string, object>>(body)["access_token"].ToString();
		}

		return accessToken;
	}
}
```

**Use the token.** Send `Authorization: Bearer <access token>` — the change at the use site in part 1 — with `X-CSRF-Header: -` on REST and Kepler calls.

**On expiry.** With Allow Refresh Tokens enabled on the OAuth2 Client and PKCE used, the application holds a refresh token and, when the access token expires, sends a `grant_type=refresh_token` request with it and the client credentials in place of the code exchange — *inferred* from the standard OAuth shape, not tested. The exchange example above reads only the `access_token` of the response; to renew with a refresh token, read the `refresh_token` from the same response as well and keep it as a secret, and otherwise the application sends the user through the sign-in again when the access token expires. Relativity does not document a refresh token's lifetime, whether it rotates, how it is revoked, or whether a revocation endpoint exists, nor what disabling or deleting the client does to tokens already issued. A refresh token is a secret: store it as Secret Management (Part B of the Migration Documentation) describes, and on compromise disable the client and replace it. Enable refresh tokens only if the application must keep a session beyond one token lifetime without asking the user to sign in again.

**PKCE.** PKCE is required by Relativity's own documentation to obtain a refresh token, and not otherwise: a Code request that does not ask for a refresh token works without it, **verified by NSerio against a live instance; not documented by Relativity**. It is never a blanket requirement. NSerio recommends PKCE with the `S256` method in both variants (NSerio recommendation), and does not claim that Relativity accepts any particular method, which it does not document.

**Scope.** No scope set is prescribed for Code. The scope below works — `openid RelativityRequestOrigin UserInfo UserInfoAccess AuthenticationID RelativityWeb` — **verified by NSerio against a live instance; not documented by Relativity**, verified as working and not as minimal. `state` is sent as standard OAuth practice and `nonce` is not used; Relativity does not document whether it enforces either.

**The application stops collecting the password.** After this change the application never receives, stores or forwards the user's username and password: the user signs in on RelativityOne's own page, and the application holds only tokens. Remove the credential prompt, the form field and any storage of the password that the replaced code needed.

**Single sign-on.** Sign-in happens on RelativityOne's own login page, so the instance's single sign-on and two-factor rules apply unchanged — *inferred* from where the sign-in happens, and not documented for clients that customers create.

**The client secret is required.** Code does not work without a client secret: tested and rejected, **verified by NSerio against a live instance; not documented by Relativity**, which confirms Relativity's own statement that the grant requires one. It is displayed once and is stored as Secret Management (Part B of the Migration Documentation) describes.

**Server-side variant.** For a web application whose code runs on a server:

- **Redirect.** The application receives the code at one exact HTTPS URL per environment, a route of the application, with no wildcards (NSerio recommendation; Relativity's matching rules are not documented). The Relativity administrator registers the exact URL on the OAuth2 Client.
- **Secret and tokens.** The client secret stays on the server and never reaches the browser. The user's tokens are held server-side, keyed to the session, and never in the browser.
- **Exchange.** The server performs the code exchange itself.

**Desktop variant.** For a desktop or command-line application run by a user on their own machine:

- **The system browser.** Open the authorize URL in the user's default browser and not in a web view the application hosts, so the application never sees the password (NSerio recommendation).
- **A loopback redirect on a fixed port.** Listen on the loopback address only, on the registered port, for one request. The Redirect URL is `http://127.0.0.1:<port>/<path>/`, ending with a slash because the listener below registers it as its prefix: a plain-HTTP loopback URL is accepted, **verified by NSerio against a live instance; not documented by Relativity**. The Relativity administrator registers that exact URL; because it is registered in advance, the port is fixed and not chosen at runtime.

```csharp
using System;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Net;
using System.Threading.Tasks;

public static class LoopbackAuthorizationReceiver
{
	private const string _CODE_PARAMETER = "code";
	private const string _STATE_PARAMETER = "state";
	private const string _ERROR_PARAMETER = "error";
	private const string _ERROR_DESCRIPTION_PARAMETER = "error_description";
	private const string _STATE_MISMATCH_MESSAGE = "The returned state is not the one that was sent; the response is rejected.";
	private const string _MISSING_CODE_MESSAGE = "The response carries no authorization code.";

	// Waits for the one request the browser sends to the registered loopback Redirect URL, and returns its code
	// only when its state is the one that was sent on the authorize request and the response carries no error.
	// redirectUrl is the exact URL registered on the OAuth2 Client, for example http://127.0.0.1:<port>/<path>/ :
	// it ends with a slash, as a listener prefix must.
	public static async Task<string> ReceiveAuthorizationCodeAsync(string authorizeUrl, string redirectUrl, string expectedState)
	{
		string authorizationCode;
		using (HttpListener listener = new HttpListener())
		{
			listener.Prefixes.Add(redirectUrl);
			listener.Start();
			Process.Start(new ProcessStartInfo(authorizeUrl) { UseShellExecute = true })?.Dispose();
			HttpListenerContext context = await listener.GetContextAsync();
			NameValueCollection query = context.Request.QueryString;
			context.Response.Close();
			if (query[_STATE_PARAMETER] != expectedState)
			{
				throw new InvalidOperationException(_STATE_MISMATCH_MESSAGE);
			}

			if (!string.IsNullOrEmpty(query[_ERROR_PARAMETER]))
			{
				throw new InvalidOperationException($"The authorization server returned an error: {query[_ERROR_PARAMETER]} {query[_ERROR_DESCRIPTION_PARAMETER]}".TrimEnd());
			}

			authorizationCode = query[_CODE_PARAMETER];
			if (string.IsNullOrEmpty(authorizationCode))
			{
				throw new InvalidOperationException(_MISSING_CODE_MESSAGE);
			}
		}

		return authorizationCode;
	}
}
```

- **The secret is extractable.** Code requires a client secret (above), so a distributed desktop application holds one, and **a secret shipped inside a distributed binary can be extracted by anyone who holds the binary**. It protects nothing against the person running the application.
- **Compensating guidance** (NSerio recommendation, subject to your own security policy): set Trusted IPs where the users' outbound addresses are known and stable — they restrict where a token can be *requested*, and are checked when a token is issued against the client's ID and secret; use the recommended short Access Token Lifetime of 60 minutes; enable refresh tokens only when needed; and create one OAuth2 Client per distribution, so a leaked secret can be disabled without affecting the others.
- **A documentation gap.** Relativity documents no recommendation for a distributed desktop application; this design is what NSerio verified works.

Verification: **Verification note — three things.**

**A token has a finite lifetime, and a header set once cannot be refreshed.** An `HttpWebRequest` is one request, so its header is already per request. A `WebClient` instance reused across calls is the long-lived case: set the header before each call with a current token, or the code works for the token's lifetime and then returns 401.

**The pair's encoding may be a separate match.** Where `encoded` is built in the same file, the encoding and this header are two matches at two locations. They are one migration, and both locations change together.

**Confirm the endpoint in use is the RelativityOne REST API.** That is the API whose bearer token this guidance describes; an endpoint elsewhere reached from the same code may accept only Basic and reject the migrated call.

OAuth2 Client: See [OAuth2 Client for Authorization Code](#oauth2-client-for-authorization-code).
<!-- generated:end CS-012 authorization-code -->

### CS-013 · WcfBasicTransportCredential

<a id="cs-013-migration"></a>

#### Client Credentials

<!-- generated:begin CS-013 client-credentials -->
**Flow.** This entry is for Client Credentials. The "Which flow" paragraph of CS-013 in [Part A](part-a-csharp.md#cs-013) says when a match belongs to this flow.

Source form:
```csharp
binding.Security.Transport.ClientCredentialType = HttpClientCredentialType.Basic;
client.ClientCredentials.UserName.UserName = userName;
client.ClientCredentials.UserName.Password = password;
```

Under token authentication: **First, the destination:** if it is not RelativityOne, no migration to RelativityOne applies — that service's own token mechanism is the migration — and the rest of this entry does not apply.

The binding's **transport credential type becomes the token form that binding supports**, and the username and password assignments are removed — from code, and from configuration where the credential lives there instead. No code form is given: which token form a binding and a service accept is decided by the service, not by the client.

**If the SOAP service is Relativity's, confirm it accepts bearer at all before changing the binding.** Where it does not, there is no scheme to migrate to on that service, and the migration is the move to a service that does.

Credentials: **Where the credentials come from.** The destination decides whether the migration applies. If the destination is not RelativityOne, the migration is that service's own token mechanism, and no migration to RelativityOne applies. If it is RelativityOne, the token comes from the token-acquisition part that follows.

**Where the token comes from.** Create an OAuth2 Client with Flow Grant Type set to Client Credentials (part 3 lists its settings), then send a form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=client_credentials`, `client_id`, `client_secret` and a `scope`. The token endpoint is **documented** for this grant. The token is then sent as `Authorization: Bearer <token>`, with `X-CSRF-Header: -` on REST and Kepler calls — the header is documented as required, and a Kepler call without it returns 404.

**.NET** (`HttpClient` and `System.Text.Json`). The class requests the token with the client ID and the client secret, caches it, requests a new one 60 seconds before it expires, and, when a call is rejected for authentication, requests a new one and sends the call once more. Build one instance for the life of the process and send every call through `SendAsync`; the request is created by the delegate on each attempt, because a request message cannot be sent twice. The client ID and the client secret come from the process environment or a secret store, never from source. Where the code uses the Relativity SDK, `GetAccessTokenAsync(false)` gives the token for `BearerTokenCredentials` each time a `ServiceFactory` is built.

```csharp
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

public sealed class AccessTokenProvider : IDisposable
{
	private const string _TOKEN_PATH = "/Relativity/Identity/connect/token";
	private const string _BEARER_SCHEME = "Bearer";
	private const string _CSRF_HEADER_NAME = "X-CSRF-Header";
	private const string _CSRF_HEADER_VALUE = "-";
	private static readonly TimeSpan _renewalMargin = TimeSpan.FromSeconds(60);

	private readonly HttpClient _client = new HttpClient();
	private readonly SemaphoreSlim _gate = new SemaphoreSlim(1, 1);
	private readonly string _host;
	private readonly string _clientId;
	private readonly string _clientSecret;
	private readonly string _scope;
	private string _accessToken;
	private DateTimeOffset _expiresAt;

	public AccessTokenProvider(string host, string clientId, string clientSecret, string scope)
	{
		_host = host;
		_clientId = clientId;
		_clientSecret = clientSecret;
		_scope = scope;
	}

	// Cached token; renewed shortly before it expires, or on demand after a rejected call.
	public async Task<string> GetAccessTokenAsync(bool forceRenewal)
	{
		await _gate.WaitAsync();
		try
		{
			if (forceRenewal || _accessToken == null || DateTimeOffset.UtcNow >= _expiresAt - _renewalMargin)
			{
				await AcquireAccessTokenAsync();
			}

			return _accessToken;
		}
		finally
		{
			_gate.Release();
		}
	}

	// Sends the request with the bearer token; when the call is rejected for authentication, renews the token and sends it once more.
	public async Task<HttpResponseMessage> SendAsync(Func<HttpRequestMessage> createRequest)
	{
		HttpResponseMessage response = await SendOnceAsync(createRequest, false);
		if (response.StatusCode == HttpStatusCode.Unauthorized)
		{
			response.Dispose();
			response = await SendOnceAsync(createRequest, true);
		}

		return response;
	}

	public void Dispose()
	{
		_client.Dispose();
		_gate.Dispose();
	}

	private async Task AcquireAccessTokenAsync()
	{
		using (FormUrlEncodedContent form = new FormUrlEncodedContent(new Dictionary<string, string>
		{
			{ "grant_type", "client_credentials" },
			{ "client_id", _clientId },
			{ "client_secret", _clientSecret },
			{ "scope", _scope }
		}))
		using (HttpResponseMessage response = await _client.PostAsync($"{_host}{_TOKEN_PATH}", form))
		{
			response.EnsureSuccessStatusCode();
			using (JsonDocument document = JsonDocument.Parse(await response.Content.ReadAsStringAsync()))
			{
				_accessToken = document.RootElement.GetProperty("access_token").GetString();
				_expiresAt = DateTimeOffset.UtcNow.AddSeconds(document.RootElement.GetProperty("expires_in").GetInt32());
			}
		}
	}

	private async Task<HttpResponseMessage> SendOnceAsync(Func<HttpRequestMessage> createRequest, bool forceRenewal)
	{
		string token = await GetAccessTokenAsync(forceRenewal);
		HttpResponseMessage response;
		using (HttpRequestMessage request = createRequest())
		{
			request.Headers.Authorization = new AuthenticationHeaderValue(_BEARER_SCHEME, token);
			request.Headers.Add(_CSRF_HEADER_NAME, _CSRF_HEADER_VALUE);
			response = await _client.SendAsync(request);
		}

		return response;
	}
}
```

**.NET Framework 4.7.2**: the same class with the serializer the runtime ships (`System.Web.Extensions`, referenced together with `System.Net.Http`), and C# 7.3 syntax:

```csharp
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

public sealed class AccessTokenProvider : IDisposable
{
	private const string _TOKEN_PATH = "/Relativity/Identity/connect/token";
	private const string _BEARER_SCHEME = "Bearer";
	private const string _CSRF_HEADER_NAME = "X-CSRF-Header";
	private const string _CSRF_HEADER_VALUE = "-";
	private static readonly TimeSpan _renewalMargin = TimeSpan.FromSeconds(60);

	private readonly HttpClient _client = new HttpClient();
	private readonly SemaphoreSlim _gate = new SemaphoreSlim(1, 1);
	private readonly string _host;
	private readonly string _clientId;
	private readonly string _clientSecret;
	private readonly string _scope;
	private string _accessToken;
	private DateTimeOffset _expiresAt;

	public AccessTokenProvider(string host, string clientId, string clientSecret, string scope)
	{
		_host = host;
		_clientId = clientId;
		_clientSecret = clientSecret;
		_scope = scope;
	}

	// Cached token; renewed shortly before it expires, or on demand after a rejected call.
	public async Task<string> GetAccessTokenAsync(bool forceRenewal)
	{
		await _gate.WaitAsync();
		try
		{
			if (forceRenewal || _accessToken == null || DateTimeOffset.UtcNow >= _expiresAt - _renewalMargin)
			{
				await AcquireAccessTokenAsync();
			}

			return _accessToken;
		}
		finally
		{
			_gate.Release();
		}
	}

	// Sends the request with the bearer token; when the call is rejected for authentication, renews the token and sends it once more.
	public async Task<HttpResponseMessage> SendAsync(Func<HttpRequestMessage> createRequest)
	{
		HttpResponseMessage response = await SendOnceAsync(createRequest, false);
		if (response.StatusCode == HttpStatusCode.Unauthorized)
		{
			response.Dispose();
			response = await SendOnceAsync(createRequest, true);
		}

		return response;
	}

	public void Dispose()
	{
		_client.Dispose();
		_gate.Dispose();
	}

	private async Task AcquireAccessTokenAsync()
	{
		using (FormUrlEncodedContent form = new FormUrlEncodedContent(new Dictionary<string, string>
		{
			{ "grant_type", "client_credentials" },
			{ "client_id", _clientId },
			{ "client_secret", _clientSecret },
			{ "scope", _scope }
		}))
		using (HttpResponseMessage response = await _client.PostAsync($"{_host}{_TOKEN_PATH}", form))
		{
			response.EnsureSuccessStatusCode();
			string body = await response.Content.ReadAsStringAsync();
			Dictionary<string, object> fields = new JavaScriptSerializer().Deserialize<Dictionary<string, object>>(body);
			_accessToken = fields["access_token"].ToString();
			_expiresAt = DateTimeOffset.UtcNow.AddSeconds(Convert.ToInt32(fields["expires_in"]));
		}
	}

	private async Task<HttpResponseMessage> SendOnceAsync(Func<HttpRequestMessage> createRequest, bool forceRenewal)
	{
		string token = await GetAccessTokenAsync(forceRenewal);
		HttpResponseMessage response;
		using (HttpRequestMessage request = createRequest())
		{
			request.Headers.Authorization = new AuthenticationHeaderValue(_BEARER_SCHEME, token);
			request.Headers.Add(_CSRF_HEADER_NAME, _CSRF_HEADER_VALUE);
			response = await _client.SendAsync(request);
		}

		return response;
	}
}
```

**The scope depends on what the integration needs, and no single scope is required.** Relativity's documentation shows two values for this grant: `SystemUserInfo` on the REST authentication page, the Usage Reports page and in the official Transfer SDK sample, and `UserInfoAccess` in the Kepler .NET samples. No page says which is correct, whether both work, or what either grants, and the OAuth2 Client's `Scopes` property is documented as one that cannot be set. Use the value the API you call documents, and confirm it.

**Cache the token, and re-acquire it on expiry — do not refresh.** Keep the token for its lifetime (part 3 recommends 60 minutes, Relativity's own wording) and request a new one with the client secret shortly before it expires (the JavaScript, TypeScript and Python examples use 60 seconds), and once when a call is rejected for authentication. Client Credentials does not offer refresh tokens: **verified by NSerio against a live instance; not documented by Relativity** — the Allow Refresh Tokens control does not appear for this grant in the RelativityOne user interface. Relativity's own tables and text on refresh tokens conflict with each other, so re-acquisition with the secret is the advice that holds whichever way they are read.

**Long-running processes that use the Relativity SDK.** A `ServiceFactory` built once from a bearer token keeps that token after it expires. Rebuild the `ServiceFactory` and its proxies with a fresh token before expiry. This is the only advice consistent with the documentation: the package `Relativity.Kepler.Client.SDK` (documented) exposes a settable `Token` and an `HttpMessageHandler` overload but no token-provider overload, and no page documents renewing a token behind a factory.

**The client secret.** It is displayed once, at creation or reset. Store it as Secret Management (Part B of the Migration Documentation) describes; never in source, never in tracked configuration. A reset invalidates the previous secret immediately, so plan the cut-over before resetting it.

**Before deploying, confirm the calling identity.** Every call now runs as the OAuth2 Client's **Context User** — not as the user in the replaced credential, and for an import or a job as much as for a request. If that user has different workspace permissions or item-level security, the code returns fewer rows or `Unauthorized` in production with no change to its logic, and Relativity's audit attributes every call to that one account. Confirm the Context User's permissions match those of the previous identity. Relativity's REST API authentication page describes the Context User as usually a member of the Relativity Administrators group; the OAuth2 Client block (part 3) gives NSerio's recommendation, which differs.

**When the integration acts for a signed-in user.** Client Credentials is one service identity by construction. An integration that acts on behalf of a signed-in user is the Authorization Code case, and moving to it means a new OAuth2 Client, because the flow is fixed when a client is created. Which of the two flows applies depends on who supplies the credential at run time.

Verification: **Verification note — three things.**

**A SOAP or WCF service may not accept bearer at all.** This is the first check, not the last: changing the binding against a service that accepts only Basic breaks a working integration.

**The credential may not be in this file.** The binding is what this match is about; its username and password may live in configuration files, which were not examined here. A migration that changes the binding and leaves a configured password behind leaves the secret in place.

**A token has a finite lifetime.** A client proxy configured once and kept for the process's life holds a token that expires; within that lifetime the token on the proxy must be replaced, or the proxy recreated.

OAuth2 Client: See [OAuth2 Client for Client Credentials](#oauth2-client-for-client-credentials).
<!-- generated:end CS-013 client-credentials -->

#### Authorization Code

<!-- generated:begin CS-013 authorization-code -->
**Flow.** This entry is for Authorization Code. The "Which flow" paragraph of CS-013 in [Part A](part-a-csharp.md#cs-013) says when a match belongs to this flow.

Source form:
```csharp
binding.Security.Transport.ClientCredentialType = HttpClientCredentialType.Basic;
client.ClientCredentials.UserName.UserName = userName;
client.ClientCredentials.UserName.Password = password;
```

Under token authentication: **First, the destination:** if it is not RelativityOne, no migration to RelativityOne applies — that service's own token mechanism is the migration — and the rest of this entry does not apply.

The binding's **transport credential type becomes the token form that binding supports**, and the username and password assignments are removed — from code, and from configuration where the credential lives there instead. No code form is given: which token form a binding and a service accept is decided by the service, not by the client.

**If the SOAP service is Relativity's, confirm it accepts bearer at all before changing the binding.** Where it does not, there is no scheme to migrate to on that service, and the migration is the move to a service that does.

Credentials: **Where the credentials come from.** The destination decides whether the migration applies. If the destination is not RelativityOne, the migration is that service's own token mechanism, and no migration to RelativityOne applies. If it is RelativityOne, the token comes from the token-acquisition part that follows.

**Common to both variants**

**Sign in.** Replace the code that collected the user's credentials with a redirect: send the user's browser to the authorize endpoint `<host>/Relativity/Identity/connect/authorize`, which leads to the RelativityOne login page. The authorize endpoint is **verified by NSerio against a live instance; not documented by Relativity**. The request carries the standard authorization-code parameters — `client_id`, `response_type=code`, the registered `redirect_uri`, a `scope`, and a `state` value the application checks on return; these are standard OAuth, and Relativity documents none of them. No discovery document is relied on.

**Receive the code.** The browser returns to the Redirect URL registered on the OAuth2 Client, with the authorization code and the `state` value. Check the `state` before using the code. The Redirect URL must be exactly the one registered (the variants say how).

**Exchange the code.** Send a form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=authorization_code`, the `code`, the same `redirect_uri`, the `client_id` and the `client_secret`, plus the `code_verifier` when PKCE is used. The code is exchanged at that endpoint, which Relativity documents for the Client Credentials grant only; the exchange is **verified by NSerio against a live instance; not documented by Relativity**. Send the `code_verifier` only when the authorize request carried a `code_challenge`; otherwise leave it out.

**.NET** (`HttpClient` and `System.Text.Json`):

```csharp
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

public sealed class AuthorizationCodeRequest
{
	public string Host { get; set; }
	public string AuthorizationCode { get; set; }
	public string RedirectUri { get; set; }
	public string ClientId { get; set; }
	public string ClientSecret { get; set; }

	// The PKCE verifier; null when no code_challenge was sent on the authorize request.
	public string CodeVerifier { get; set; }
}

public static class AuthorizationCodeExchange
{
	private const string _TOKEN_PATH = "/Relativity/Identity/connect/token";

	public static async Task<string> ExchangeAuthorizationCodeAsync(HttpClient client, AuthorizationCodeRequest request)
	{
		Dictionary<string, string> fields = new Dictionary<string, string>
		{
			{ "grant_type", "authorization_code" },
			{ "code", request.AuthorizationCode },
			{ "redirect_uri", request.RedirectUri },
			{ "client_id", request.ClientId },
			{ "client_secret", request.ClientSecret }
		};
		if (!string.IsNullOrEmpty(request.CodeVerifier))
		{
			fields["code_verifier"] = request.CodeVerifier;
		}

		string accessToken;
		using (FormUrlEncodedContent form = new FormUrlEncodedContent(fields))
		using (HttpResponseMessage response = await client.PostAsync($"{request.Host}{_TOKEN_PATH}", form))
		{
			response.EnsureSuccessStatusCode();
			using (JsonDocument document = JsonDocument.Parse(await response.Content.ReadAsStringAsync()))
			{
				accessToken = document.RootElement.GetProperty("access_token").GetString();
			}
		}

		return accessToken;
	}
}
```

**.NET Framework 4.7.2** (`System.Web.Extensions` and C# 7.3 syntax):

```csharp
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

public sealed class AuthorizationCodeRequest
{
	public string Host { get; set; }
	public string AuthorizationCode { get; set; }
	public string RedirectUri { get; set; }
	public string ClientId { get; set; }
	public string ClientSecret { get; set; }

	// The PKCE verifier; null when no code_challenge was sent on the authorize request.
	public string CodeVerifier { get; set; }
}

public static class AuthorizationCodeExchange
{
	private const string _TOKEN_PATH = "/Relativity/Identity/connect/token";

	public static async Task<string> ExchangeAuthorizationCodeAsync(HttpClient client, AuthorizationCodeRequest request)
	{
		Dictionary<string, string> fields = new Dictionary<string, string>
		{
			{ "grant_type", "authorization_code" },
			{ "code", request.AuthorizationCode },
			{ "redirect_uri", request.RedirectUri },
			{ "client_id", request.ClientId },
			{ "client_secret", request.ClientSecret }
		};
		if (!string.IsNullOrEmpty(request.CodeVerifier))
		{
			fields["code_verifier"] = request.CodeVerifier;
		}

		string accessToken;
		using (FormUrlEncodedContent form = new FormUrlEncodedContent(fields))
		using (HttpResponseMessage response = await client.PostAsync($"{request.Host}{_TOKEN_PATH}", form))
		{
			response.EnsureSuccessStatusCode();
			string body = await response.Content.ReadAsStringAsync();
			accessToken = new JavaScriptSerializer().Deserialize<Dictionary<string, object>>(body)["access_token"].ToString();
		}

		return accessToken;
	}
}
```

**Use the token.** Send `Authorization: Bearer <access token>` — the change at the use site in part 1 — with `X-CSRF-Header: -` on REST and Kepler calls.

**On expiry.** With Allow Refresh Tokens enabled on the OAuth2 Client and PKCE used, the application holds a refresh token and, when the access token expires, sends a `grant_type=refresh_token` request with it and the client credentials in place of the code exchange — *inferred* from the standard OAuth shape, not tested. The exchange example above reads only the `access_token` of the response; to renew with a refresh token, read the `refresh_token` from the same response as well and keep it as a secret, and otherwise the application sends the user through the sign-in again when the access token expires. Relativity does not document a refresh token's lifetime, whether it rotates, how it is revoked, or whether a revocation endpoint exists, nor what disabling or deleting the client does to tokens already issued. A refresh token is a secret: store it as Secret Management (Part B of the Migration Documentation) describes, and on compromise disable the client and replace it. Enable refresh tokens only if the application must keep a session beyond one token lifetime without asking the user to sign in again.

**PKCE.** PKCE is required by Relativity's own documentation to obtain a refresh token, and not otherwise: a Code request that does not ask for a refresh token works without it, **verified by NSerio against a live instance; not documented by Relativity**. It is never a blanket requirement. NSerio recommends PKCE with the `S256` method in both variants (NSerio recommendation), and does not claim that Relativity accepts any particular method, which it does not document.

**Scope.** No scope set is prescribed for Code. The scope below works — `openid RelativityRequestOrigin UserInfo UserInfoAccess AuthenticationID RelativityWeb` — **verified by NSerio against a live instance; not documented by Relativity**, verified as working and not as minimal. `state` is sent as standard OAuth practice and `nonce` is not used; Relativity does not document whether it enforces either.

**The application stops collecting the password.** After this change the application never receives, stores or forwards the user's username and password: the user signs in on RelativityOne's own page, and the application holds only tokens. Remove the credential prompt, the form field and any storage of the password that the replaced code needed.

**Single sign-on.** Sign-in happens on RelativityOne's own login page, so the instance's single sign-on and two-factor rules apply unchanged — *inferred* from where the sign-in happens, and not documented for clients that customers create.

**The client secret is required.** Code does not work without a client secret: tested and rejected, **verified by NSerio against a live instance; not documented by Relativity**, which confirms Relativity's own statement that the grant requires one. It is displayed once and is stored as Secret Management (Part B of the Migration Documentation) describes.

**Server-side variant.** For a web application whose code runs on a server:

- **Redirect.** The application receives the code at one exact HTTPS URL per environment, a route of the application, with no wildcards (NSerio recommendation; Relativity's matching rules are not documented). The Relativity administrator registers the exact URL on the OAuth2 Client.
- **Secret and tokens.** The client secret stays on the server and never reaches the browser. The user's tokens are held server-side, keyed to the session, and never in the browser.
- **Exchange.** The server performs the code exchange itself.

**Desktop variant.** For a desktop or command-line application run by a user on their own machine:

- **The system browser.** Open the authorize URL in the user's default browser and not in a web view the application hosts, so the application never sees the password (NSerio recommendation).
- **A loopback redirect on a fixed port.** Listen on the loopback address only, on the registered port, for one request. The Redirect URL is `http://127.0.0.1:<port>/<path>/`, ending with a slash because the listener below registers it as its prefix: a plain-HTTP loopback URL is accepted, **verified by NSerio against a live instance; not documented by Relativity**. The Relativity administrator registers that exact URL; because it is registered in advance, the port is fixed and not chosen at runtime.

```csharp
using System;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Net;
using System.Threading.Tasks;

public static class LoopbackAuthorizationReceiver
{
	private const string _CODE_PARAMETER = "code";
	private const string _STATE_PARAMETER = "state";
	private const string _ERROR_PARAMETER = "error";
	private const string _ERROR_DESCRIPTION_PARAMETER = "error_description";
	private const string _STATE_MISMATCH_MESSAGE = "The returned state is not the one that was sent; the response is rejected.";
	private const string _MISSING_CODE_MESSAGE = "The response carries no authorization code.";

	// Waits for the one request the browser sends to the registered loopback Redirect URL, and returns its code
	// only when its state is the one that was sent on the authorize request and the response carries no error.
	// redirectUrl is the exact URL registered on the OAuth2 Client, for example http://127.0.0.1:<port>/<path>/ :
	// it ends with a slash, as a listener prefix must.
	public static async Task<string> ReceiveAuthorizationCodeAsync(string authorizeUrl, string redirectUrl, string expectedState)
	{
		string authorizationCode;
		using (HttpListener listener = new HttpListener())
		{
			listener.Prefixes.Add(redirectUrl);
			listener.Start();
			Process.Start(new ProcessStartInfo(authorizeUrl) { UseShellExecute = true })?.Dispose();
			HttpListenerContext context = await listener.GetContextAsync();
			NameValueCollection query = context.Request.QueryString;
			context.Response.Close();
			if (query[_STATE_PARAMETER] != expectedState)
			{
				throw new InvalidOperationException(_STATE_MISMATCH_MESSAGE);
			}

			if (!string.IsNullOrEmpty(query[_ERROR_PARAMETER]))
			{
				throw new InvalidOperationException($"The authorization server returned an error: {query[_ERROR_PARAMETER]} {query[_ERROR_DESCRIPTION_PARAMETER]}".TrimEnd());
			}

			authorizationCode = query[_CODE_PARAMETER];
			if (string.IsNullOrEmpty(authorizationCode))
			{
				throw new InvalidOperationException(_MISSING_CODE_MESSAGE);
			}
		}

		return authorizationCode;
	}
}
```

- **The secret is extractable.** Code requires a client secret (above), so a distributed desktop application holds one, and **a secret shipped inside a distributed binary can be extracted by anyone who holds the binary**. It protects nothing against the person running the application.
- **Compensating guidance** (NSerio recommendation, subject to your own security policy): set Trusted IPs where the users' outbound addresses are known and stable — they restrict where a token can be *requested*, and are checked when a token is issued against the client's ID and secret; use the recommended short Access Token Lifetime of 60 minutes; enable refresh tokens only when needed; and create one OAuth2 Client per distribution, so a leaked secret can be disabled without affecting the others.
- **A documentation gap.** Relativity documents no recommendation for a distributed desktop application; this design is what NSerio verified works.

Verification: **Verification note — three things.**

**A SOAP or WCF service may not accept bearer at all.** This is the first check, not the last: changing the binding against a service that accepts only Basic breaks a working integration.

**The credential may not be in this file.** The binding is what this match is about; its username and password may live in configuration files, which were not examined here. A migration that changes the binding and leaves a configured password behind leaves the secret in place.

**A token has a finite lifetime.** A client proxy configured once and kept for the process's life holds a token that expires; within that lifetime the token on the proxy must be replaced, or the proxy recreated.

OAuth2 Client: See [OAuth2 Client for Authorization Code](#oauth2-client-for-authorization-code).
<!-- generated:end CS-013 authorization-code -->

### CS-014 · SchemeNamingExtensionMethod

<a id="cs-014-migration"></a>

#### Client Credentials

<!-- generated:begin CS-014 client-credentials -->
**Flow.** This entry is for Client Credentials. The "Which flow" paragraph of CS-014 in [Part A](part-a-csharp.md#cs-014) says when a match belongs to this flow.

Source form:
```csharp
var authenticatedRequest = request.WithBasicAuth(userName, password);
```

Under token authentication: **First, the destination:** if it is not RelativityOne, no migration to RelativityOne applies — that service's own token mechanism is the migration — and the rest of this entry does not apply.

Replace the Basic method with **the same library's bearer or token method, carrying the access token** in place of the credential pair. This guidance names no target method: the construct is a method whose name names the scheme, and the token method's name differs between libraries and versions.

Credentials: **Where the credentials come from.** The destination decides whether the migration applies. If the destination is not RelativityOne, the migration is that service's own token mechanism, and no migration to RelativityOne applies. If it is RelativityOne, the token comes from the token-acquisition part that follows.

**Where the token comes from.** Create an OAuth2 Client with Flow Grant Type set to Client Credentials (part 3 lists its settings), then send a form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=client_credentials`, `client_id`, `client_secret` and a `scope`. The token endpoint is **documented** for this grant. The token is then sent as `Authorization: Bearer <token>`, with `X-CSRF-Header: -` on REST and Kepler calls — the header is documented as required, and a Kepler call without it returns 404.

**.NET** (`HttpClient` and `System.Text.Json`). The class requests the token with the client ID and the client secret, caches it, requests a new one 60 seconds before it expires, and, when a call is rejected for authentication, requests a new one and sends the call once more. Build one instance for the life of the process and send every call through `SendAsync`; the request is created by the delegate on each attempt, because a request message cannot be sent twice. The client ID and the client secret come from the process environment or a secret store, never from source. Where the code uses the Relativity SDK, `GetAccessTokenAsync(false)` gives the token for `BearerTokenCredentials` each time a `ServiceFactory` is built.

```csharp
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

public sealed class AccessTokenProvider : IDisposable
{
	private const string _TOKEN_PATH = "/Relativity/Identity/connect/token";
	private const string _BEARER_SCHEME = "Bearer";
	private const string _CSRF_HEADER_NAME = "X-CSRF-Header";
	private const string _CSRF_HEADER_VALUE = "-";
	private static readonly TimeSpan _renewalMargin = TimeSpan.FromSeconds(60);

	private readonly HttpClient _client = new HttpClient();
	private readonly SemaphoreSlim _gate = new SemaphoreSlim(1, 1);
	private readonly string _host;
	private readonly string _clientId;
	private readonly string _clientSecret;
	private readonly string _scope;
	private string _accessToken;
	private DateTimeOffset _expiresAt;

	public AccessTokenProvider(string host, string clientId, string clientSecret, string scope)
	{
		_host = host;
		_clientId = clientId;
		_clientSecret = clientSecret;
		_scope = scope;
	}

	// Cached token; renewed shortly before it expires, or on demand after a rejected call.
	public async Task<string> GetAccessTokenAsync(bool forceRenewal)
	{
		await _gate.WaitAsync();
		try
		{
			if (forceRenewal || _accessToken == null || DateTimeOffset.UtcNow >= _expiresAt - _renewalMargin)
			{
				await AcquireAccessTokenAsync();
			}

			return _accessToken;
		}
		finally
		{
			_gate.Release();
		}
	}

	// Sends the request with the bearer token; when the call is rejected for authentication, renews the token and sends it once more.
	public async Task<HttpResponseMessage> SendAsync(Func<HttpRequestMessage> createRequest)
	{
		HttpResponseMessage response = await SendOnceAsync(createRequest, false);
		if (response.StatusCode == HttpStatusCode.Unauthorized)
		{
			response.Dispose();
			response = await SendOnceAsync(createRequest, true);
		}

		return response;
	}

	public void Dispose()
	{
		_client.Dispose();
		_gate.Dispose();
	}

	private async Task AcquireAccessTokenAsync()
	{
		using (FormUrlEncodedContent form = new FormUrlEncodedContent(new Dictionary<string, string>
		{
			{ "grant_type", "client_credentials" },
			{ "client_id", _clientId },
			{ "client_secret", _clientSecret },
			{ "scope", _scope }
		}))
		using (HttpResponseMessage response = await _client.PostAsync($"{_host}{_TOKEN_PATH}", form))
		{
			response.EnsureSuccessStatusCode();
			using (JsonDocument document = JsonDocument.Parse(await response.Content.ReadAsStringAsync()))
			{
				_accessToken = document.RootElement.GetProperty("access_token").GetString();
				_expiresAt = DateTimeOffset.UtcNow.AddSeconds(document.RootElement.GetProperty("expires_in").GetInt32());
			}
		}
	}

	private async Task<HttpResponseMessage> SendOnceAsync(Func<HttpRequestMessage> createRequest, bool forceRenewal)
	{
		string token = await GetAccessTokenAsync(forceRenewal);
		HttpResponseMessage response;
		using (HttpRequestMessage request = createRequest())
		{
			request.Headers.Authorization = new AuthenticationHeaderValue(_BEARER_SCHEME, token);
			request.Headers.Add(_CSRF_HEADER_NAME, _CSRF_HEADER_VALUE);
			response = await _client.SendAsync(request);
		}

		return response;
	}
}
```

**.NET Framework 4.7.2**: the same class with the serializer the runtime ships (`System.Web.Extensions`, referenced together with `System.Net.Http`), and C# 7.3 syntax:

```csharp
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

public sealed class AccessTokenProvider : IDisposable
{
	private const string _TOKEN_PATH = "/Relativity/Identity/connect/token";
	private const string _BEARER_SCHEME = "Bearer";
	private const string _CSRF_HEADER_NAME = "X-CSRF-Header";
	private const string _CSRF_HEADER_VALUE = "-";
	private static readonly TimeSpan _renewalMargin = TimeSpan.FromSeconds(60);

	private readonly HttpClient _client = new HttpClient();
	private readonly SemaphoreSlim _gate = new SemaphoreSlim(1, 1);
	private readonly string _host;
	private readonly string _clientId;
	private readonly string _clientSecret;
	private readonly string _scope;
	private string _accessToken;
	private DateTimeOffset _expiresAt;

	public AccessTokenProvider(string host, string clientId, string clientSecret, string scope)
	{
		_host = host;
		_clientId = clientId;
		_clientSecret = clientSecret;
		_scope = scope;
	}

	// Cached token; renewed shortly before it expires, or on demand after a rejected call.
	public async Task<string> GetAccessTokenAsync(bool forceRenewal)
	{
		await _gate.WaitAsync();
		try
		{
			if (forceRenewal || _accessToken == null || DateTimeOffset.UtcNow >= _expiresAt - _renewalMargin)
			{
				await AcquireAccessTokenAsync();
			}

			return _accessToken;
		}
		finally
		{
			_gate.Release();
		}
	}

	// Sends the request with the bearer token; when the call is rejected for authentication, renews the token and sends it once more.
	public async Task<HttpResponseMessage> SendAsync(Func<HttpRequestMessage> createRequest)
	{
		HttpResponseMessage response = await SendOnceAsync(createRequest, false);
		if (response.StatusCode == HttpStatusCode.Unauthorized)
		{
			response.Dispose();
			response = await SendOnceAsync(createRequest, true);
		}

		return response;
	}

	public void Dispose()
	{
		_client.Dispose();
		_gate.Dispose();
	}

	private async Task AcquireAccessTokenAsync()
	{
		using (FormUrlEncodedContent form = new FormUrlEncodedContent(new Dictionary<string, string>
		{
			{ "grant_type", "client_credentials" },
			{ "client_id", _clientId },
			{ "client_secret", _clientSecret },
			{ "scope", _scope }
		}))
		using (HttpResponseMessage response = await _client.PostAsync($"{_host}{_TOKEN_PATH}", form))
		{
			response.EnsureSuccessStatusCode();
			string body = await response.Content.ReadAsStringAsync();
			Dictionary<string, object> fields = new JavaScriptSerializer().Deserialize<Dictionary<string, object>>(body);
			_accessToken = fields["access_token"].ToString();
			_expiresAt = DateTimeOffset.UtcNow.AddSeconds(Convert.ToInt32(fields["expires_in"]));
		}
	}

	private async Task<HttpResponseMessage> SendOnceAsync(Func<HttpRequestMessage> createRequest, bool forceRenewal)
	{
		string token = await GetAccessTokenAsync(forceRenewal);
		HttpResponseMessage response;
		using (HttpRequestMessage request = createRequest())
		{
			request.Headers.Authorization = new AuthenticationHeaderValue(_BEARER_SCHEME, token);
			request.Headers.Add(_CSRF_HEADER_NAME, _CSRF_HEADER_VALUE);
			response = await _client.SendAsync(request);
		}

		return response;
	}
}
```

**The scope depends on what the integration needs, and no single scope is required.** Relativity's documentation shows two values for this grant: `SystemUserInfo` on the REST authentication page, the Usage Reports page and in the official Transfer SDK sample, and `UserInfoAccess` in the Kepler .NET samples. No page says which is correct, whether both work, or what either grants, and the OAuth2 Client's `Scopes` property is documented as one that cannot be set. Use the value the API you call documents, and confirm it.

**Cache the token, and re-acquire it on expiry — do not refresh.** Keep the token for its lifetime (part 3 recommends 60 minutes, Relativity's own wording) and request a new one with the client secret shortly before it expires (the JavaScript, TypeScript and Python examples use 60 seconds), and once when a call is rejected for authentication. Client Credentials does not offer refresh tokens: **verified by NSerio against a live instance; not documented by Relativity** — the Allow Refresh Tokens control does not appear for this grant in the RelativityOne user interface. Relativity's own tables and text on refresh tokens conflict with each other, so re-acquisition with the secret is the advice that holds whichever way they are read.

**Long-running processes that use the Relativity SDK.** A `ServiceFactory` built once from a bearer token keeps that token after it expires. Rebuild the `ServiceFactory` and its proxies with a fresh token before expiry. This is the only advice consistent with the documentation: the package `Relativity.Kepler.Client.SDK` (documented) exposes a settable `Token` and an `HttpMessageHandler` overload but no token-provider overload, and no page documents renewing a token behind a factory.

**The client secret.** It is displayed once, at creation or reset. Store it as Secret Management (Part B of the Migration Documentation) describes; never in source, never in tracked configuration. A reset invalidates the previous secret immediately, so plan the cut-over before resetting it.

**Before deploying, confirm the calling identity.** Every call now runs as the OAuth2 Client's **Context User** — not as the user in the replaced credential, and for an import or a job as much as for a request. If that user has different workspace permissions or item-level security, the code returns fewer rows or `Unauthorized` in production with no change to its logic, and Relativity's audit attributes every call to that one account. Confirm the Context User's permissions match those of the previous identity. Relativity's REST API authentication page describes the Context User as usually a member of the Relativity Administrators group; the OAuth2 Client block (part 3) gives NSerio's recommendation, which differs.

**When the integration acts for a signed-in user.** Client Credentials is one service identity by construction. An integration that acts on behalf of a signed-in user is the Authorization Code case, and moving to it means a new OAuth2 Client, because the flow is fixed when a client is created. Which of the two flows applies depends on who supplies the credential at run time.

Verification: **Verification note — three things.**

**Confirm the library version in use offers a token method.** Where it does not, this is a library upgrade before it is a code change.

**A token has a finite lifetime.** Applied to one request, the method is per request already. Applied once to a client or configuration shared across requests, it holds a token that expires, and the code works for the token's lifetime and then returns 401; pass a current token where the request is built.

**Confirm the endpoint in use is the RelativityOne REST API.** That is the API whose bearer token this guidance describes; an endpoint elsewhere reached from the same code may accept only Basic and reject the migrated call.

OAuth2 Client: See [OAuth2 Client for Client Credentials](#oauth2-client-for-client-credentials).
<!-- generated:end CS-014 client-credentials -->

#### Authorization Code

<!-- generated:begin CS-014 authorization-code -->
**Flow.** This entry is for Authorization Code. The "Which flow" paragraph of CS-014 in [Part A](part-a-csharp.md#cs-014) says when a match belongs to this flow.

Source form:
```csharp
var authenticatedRequest = request.WithBasicAuth(userName, password);
```

Under token authentication: **First, the destination:** if it is not RelativityOne, no migration to RelativityOne applies — that service's own token mechanism is the migration — and the rest of this entry does not apply.

Replace the Basic method with **the same library's bearer or token method, carrying the access token** in place of the credential pair. This guidance names no target method: the construct is a method whose name names the scheme, and the token method's name differs between libraries and versions.

Credentials: **Where the credentials come from.** The destination decides whether the migration applies. If the destination is not RelativityOne, the migration is that service's own token mechanism, and no migration to RelativityOne applies. If it is RelativityOne, the token comes from the token-acquisition part that follows.

**Common to both variants**

**Sign in.** Replace the code that collected the user's credentials with a redirect: send the user's browser to the authorize endpoint `<host>/Relativity/Identity/connect/authorize`, which leads to the RelativityOne login page. The authorize endpoint is **verified by NSerio against a live instance; not documented by Relativity**. The request carries the standard authorization-code parameters — `client_id`, `response_type=code`, the registered `redirect_uri`, a `scope`, and a `state` value the application checks on return; these are standard OAuth, and Relativity documents none of them. No discovery document is relied on.

**Receive the code.** The browser returns to the Redirect URL registered on the OAuth2 Client, with the authorization code and the `state` value. Check the `state` before using the code. The Redirect URL must be exactly the one registered (the variants say how).

**Exchange the code.** Send a form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=authorization_code`, the `code`, the same `redirect_uri`, the `client_id` and the `client_secret`, plus the `code_verifier` when PKCE is used. The code is exchanged at that endpoint, which Relativity documents for the Client Credentials grant only; the exchange is **verified by NSerio against a live instance; not documented by Relativity**. Send the `code_verifier` only when the authorize request carried a `code_challenge`; otherwise leave it out.

**.NET** (`HttpClient` and `System.Text.Json`):

```csharp
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

public sealed class AuthorizationCodeRequest
{
	public string Host { get; set; }
	public string AuthorizationCode { get; set; }
	public string RedirectUri { get; set; }
	public string ClientId { get; set; }
	public string ClientSecret { get; set; }

	// The PKCE verifier; null when no code_challenge was sent on the authorize request.
	public string CodeVerifier { get; set; }
}

public static class AuthorizationCodeExchange
{
	private const string _TOKEN_PATH = "/Relativity/Identity/connect/token";

	public static async Task<string> ExchangeAuthorizationCodeAsync(HttpClient client, AuthorizationCodeRequest request)
	{
		Dictionary<string, string> fields = new Dictionary<string, string>
		{
			{ "grant_type", "authorization_code" },
			{ "code", request.AuthorizationCode },
			{ "redirect_uri", request.RedirectUri },
			{ "client_id", request.ClientId },
			{ "client_secret", request.ClientSecret }
		};
		if (!string.IsNullOrEmpty(request.CodeVerifier))
		{
			fields["code_verifier"] = request.CodeVerifier;
		}

		string accessToken;
		using (FormUrlEncodedContent form = new FormUrlEncodedContent(fields))
		using (HttpResponseMessage response = await client.PostAsync($"{request.Host}{_TOKEN_PATH}", form))
		{
			response.EnsureSuccessStatusCode();
			using (JsonDocument document = JsonDocument.Parse(await response.Content.ReadAsStringAsync()))
			{
				accessToken = document.RootElement.GetProperty("access_token").GetString();
			}
		}

		return accessToken;
	}
}
```

**.NET Framework 4.7.2** (`System.Web.Extensions` and C# 7.3 syntax):

```csharp
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

public sealed class AuthorizationCodeRequest
{
	public string Host { get; set; }
	public string AuthorizationCode { get; set; }
	public string RedirectUri { get; set; }
	public string ClientId { get; set; }
	public string ClientSecret { get; set; }

	// The PKCE verifier; null when no code_challenge was sent on the authorize request.
	public string CodeVerifier { get; set; }
}

public static class AuthorizationCodeExchange
{
	private const string _TOKEN_PATH = "/Relativity/Identity/connect/token";

	public static async Task<string> ExchangeAuthorizationCodeAsync(HttpClient client, AuthorizationCodeRequest request)
	{
		Dictionary<string, string> fields = new Dictionary<string, string>
		{
			{ "grant_type", "authorization_code" },
			{ "code", request.AuthorizationCode },
			{ "redirect_uri", request.RedirectUri },
			{ "client_id", request.ClientId },
			{ "client_secret", request.ClientSecret }
		};
		if (!string.IsNullOrEmpty(request.CodeVerifier))
		{
			fields["code_verifier"] = request.CodeVerifier;
		}

		string accessToken;
		using (FormUrlEncodedContent form = new FormUrlEncodedContent(fields))
		using (HttpResponseMessage response = await client.PostAsync($"{request.Host}{_TOKEN_PATH}", form))
		{
			response.EnsureSuccessStatusCode();
			string body = await response.Content.ReadAsStringAsync();
			accessToken = new JavaScriptSerializer().Deserialize<Dictionary<string, object>>(body)["access_token"].ToString();
		}

		return accessToken;
	}
}
```

**Use the token.** Send `Authorization: Bearer <access token>` — the change at the use site in part 1 — with `X-CSRF-Header: -` on REST and Kepler calls.

**On expiry.** With Allow Refresh Tokens enabled on the OAuth2 Client and PKCE used, the application holds a refresh token and, when the access token expires, sends a `grant_type=refresh_token` request with it and the client credentials in place of the code exchange — *inferred* from the standard OAuth shape, not tested. The exchange example above reads only the `access_token` of the response; to renew with a refresh token, read the `refresh_token` from the same response as well and keep it as a secret, and otherwise the application sends the user through the sign-in again when the access token expires. Relativity does not document a refresh token's lifetime, whether it rotates, how it is revoked, or whether a revocation endpoint exists, nor what disabling or deleting the client does to tokens already issued. A refresh token is a secret: store it as Secret Management (Part B of the Migration Documentation) describes, and on compromise disable the client and replace it. Enable refresh tokens only if the application must keep a session beyond one token lifetime without asking the user to sign in again.

**PKCE.** PKCE is required by Relativity's own documentation to obtain a refresh token, and not otherwise: a Code request that does not ask for a refresh token works without it, **verified by NSerio against a live instance; not documented by Relativity**. It is never a blanket requirement. NSerio recommends PKCE with the `S256` method in both variants (NSerio recommendation), and does not claim that Relativity accepts any particular method, which it does not document.

**Scope.** No scope set is prescribed for Code. The scope below works — `openid RelativityRequestOrigin UserInfo UserInfoAccess AuthenticationID RelativityWeb` — **verified by NSerio against a live instance; not documented by Relativity**, verified as working and not as minimal. `state` is sent as standard OAuth practice and `nonce` is not used; Relativity does not document whether it enforces either.

**The application stops collecting the password.** After this change the application never receives, stores or forwards the user's username and password: the user signs in on RelativityOne's own page, and the application holds only tokens. Remove the credential prompt, the form field and any storage of the password that the replaced code needed.

**Single sign-on.** Sign-in happens on RelativityOne's own login page, so the instance's single sign-on and two-factor rules apply unchanged — *inferred* from where the sign-in happens, and not documented for clients that customers create.

**The client secret is required.** Code does not work without a client secret: tested and rejected, **verified by NSerio against a live instance; not documented by Relativity**, which confirms Relativity's own statement that the grant requires one. It is displayed once and is stored as Secret Management (Part B of the Migration Documentation) describes.

**Server-side variant.** For a web application whose code runs on a server:

- **Redirect.** The application receives the code at one exact HTTPS URL per environment, a route of the application, with no wildcards (NSerio recommendation; Relativity's matching rules are not documented). The Relativity administrator registers the exact URL on the OAuth2 Client.
- **Secret and tokens.** The client secret stays on the server and never reaches the browser. The user's tokens are held server-side, keyed to the session, and never in the browser.
- **Exchange.** The server performs the code exchange itself.

**Desktop variant.** For a desktop or command-line application run by a user on their own machine:

- **The system browser.** Open the authorize URL in the user's default browser and not in a web view the application hosts, so the application never sees the password (NSerio recommendation).
- **A loopback redirect on a fixed port.** Listen on the loopback address only, on the registered port, for one request. The Redirect URL is `http://127.0.0.1:<port>/<path>/`, ending with a slash because the listener below registers it as its prefix: a plain-HTTP loopback URL is accepted, **verified by NSerio against a live instance; not documented by Relativity**. The Relativity administrator registers that exact URL; because it is registered in advance, the port is fixed and not chosen at runtime.

```csharp
using System;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Net;
using System.Threading.Tasks;

public static class LoopbackAuthorizationReceiver
{
	private const string _CODE_PARAMETER = "code";
	private const string _STATE_PARAMETER = "state";
	private const string _ERROR_PARAMETER = "error";
	private const string _ERROR_DESCRIPTION_PARAMETER = "error_description";
	private const string _STATE_MISMATCH_MESSAGE = "The returned state is not the one that was sent; the response is rejected.";
	private const string _MISSING_CODE_MESSAGE = "The response carries no authorization code.";

	// Waits for the one request the browser sends to the registered loopback Redirect URL, and returns its code
	// only when its state is the one that was sent on the authorize request and the response carries no error.
	// redirectUrl is the exact URL registered on the OAuth2 Client, for example http://127.0.0.1:<port>/<path>/ :
	// it ends with a slash, as a listener prefix must.
	public static async Task<string> ReceiveAuthorizationCodeAsync(string authorizeUrl, string redirectUrl, string expectedState)
	{
		string authorizationCode;
		using (HttpListener listener = new HttpListener())
		{
			listener.Prefixes.Add(redirectUrl);
			listener.Start();
			Process.Start(new ProcessStartInfo(authorizeUrl) { UseShellExecute = true })?.Dispose();
			HttpListenerContext context = await listener.GetContextAsync();
			NameValueCollection query = context.Request.QueryString;
			context.Response.Close();
			if (query[_STATE_PARAMETER] != expectedState)
			{
				throw new InvalidOperationException(_STATE_MISMATCH_MESSAGE);
			}

			if (!string.IsNullOrEmpty(query[_ERROR_PARAMETER]))
			{
				throw new InvalidOperationException($"The authorization server returned an error: {query[_ERROR_PARAMETER]} {query[_ERROR_DESCRIPTION_PARAMETER]}".TrimEnd());
			}

			authorizationCode = query[_CODE_PARAMETER];
			if (string.IsNullOrEmpty(authorizationCode))
			{
				throw new InvalidOperationException(_MISSING_CODE_MESSAGE);
			}
		}

		return authorizationCode;
	}
}
```

- **The secret is extractable.** Code requires a client secret (above), so a distributed desktop application holds one, and **a secret shipped inside a distributed binary can be extracted by anyone who holds the binary**. It protects nothing against the person running the application.
- **Compensating guidance** (NSerio recommendation, subject to your own security policy): set Trusted IPs where the users' outbound addresses are known and stable — they restrict where a token can be *requested*, and are checked when a token is issued against the client's ID and secret; use the recommended short Access Token Lifetime of 60 minutes; enable refresh tokens only when needed; and create one OAuth2 Client per distribution, so a leaked secret can be disabled without affecting the others.
- **A documentation gap.** Relativity documents no recommendation for a distributed desktop application; this design is what NSerio verified works.

Verification: **Verification note — three things.**

**Confirm the library version in use offers a token method.** Where it does not, this is a library upgrade before it is a code change.

**A token has a finite lifetime.** Applied to one request, the method is per request already. Applied once to a client or configuration shared across requests, it holds a token that expires, and the code works for the token's lifetime and then returns 401; pass a current token where the request is built.

**Confirm the endpoint in use is the RelativityOne REST API.** That is the API whose bearer token this guidance describes; an endpoint elsewhere reached from the same code may accept only Basic and reject the migrated call.

OAuth2 Client: See [OAuth2 Client for Authorization Code](#oauth2-client-for-authorization-code).
<!-- generated:end CS-014 authorization-code -->

### CS-015 · SdkConstructorWithUserAndPassword

<a id="cs-015-migration"></a>

#### Client Credentials

<!-- generated:begin CS-015 client-credentials -->
**Flow.** This entry is for Client Credentials. The "Which flow" paragraph of CS-015 in [Part A](part-a-csharp.md#cs-015) says when a match belongs to this flow.

Source form:
```csharp
var importApi = new ImportAPI(userName, password, webServiceUrl);
```

Under token authentication: **First, the destination:** if this Relativity instance is not RelativityOne — including a Relativity Server instance, whose own documentation governs — no migration to RelativityOne applies and the rest of this entry does not apply.

**Change what the entry point receives, not the call site.** Relativity's Import API documentation names one static factory on the type, `ImportAPI.CreateByRsaBearerToken(webServiceUrl)`. **It takes the web service URL and no token**: it uses the bearer token of the identity of the process that runs it, and the documentation says it should be used only by processes hosted by the Relativity Service Account, such as an agent. Where the code runs in such a host, the construction above is replaced by:

```csharp
var importApi = ImportAPI.CreateByRsaBearerToken(webServiceUrl);
```

**For a standalone or desktop process that would obtain an OAuth2 token itself, this guidance gives no code form.** The documentation names no `ImportAPI` factory that accepts a token acquired by the caller, and it lists bearer-token authentication as not supported for a desktop application, where user name and password is the preferred form; this text does not resolve that. Whether the code that uses `importApi` afterwards needs any change is not established here; confirm it against the SDK's documentation for the version in use. The migrated construction must not keep the three-argument form with a credential pair, which is the one this match is about.

Credentials: **Where the credentials come from.** The SDK type establishes that the destination is Relativity, not that it is RelativityOne; if it is not RelativityOne — including a Relativity Server instance, whose own documentation governs — the migration is that service's own token mechanism, and no migration to RelativityOne applies. If it is RelativityOne, the factory above carries no token of the code's own: the identity comes from the hosting process, so the token-acquisition part that follows applies only to code that obtains a token itself, and for the entry point that case has no code form, as stated above.

**Where the token comes from.** Create an OAuth2 Client with Flow Grant Type set to Client Credentials (part 3 lists its settings), then send a form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=client_credentials`, `client_id`, `client_secret` and a `scope`. The token endpoint is **documented** for this grant. The token is then sent as `Authorization: Bearer <token>`, with `X-CSRF-Header: -` on REST and Kepler calls — the header is documented as required, and a Kepler call without it returns 404.

**.NET** (`HttpClient` and `System.Text.Json`). The class requests the token with the client ID and the client secret, caches it, requests a new one 60 seconds before it expires, and, when a call is rejected for authentication, requests a new one and sends the call once more. Build one instance for the life of the process and send every call through `SendAsync`; the request is created by the delegate on each attempt, because a request message cannot be sent twice. The client ID and the client secret come from the process environment or a secret store, never from source. Where the code uses the Relativity SDK, `GetAccessTokenAsync(false)` gives the token for `BearerTokenCredentials` each time a `ServiceFactory` is built.

```csharp
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

public sealed class AccessTokenProvider : IDisposable
{
	private const string _TOKEN_PATH = "/Relativity/Identity/connect/token";
	private const string _BEARER_SCHEME = "Bearer";
	private const string _CSRF_HEADER_NAME = "X-CSRF-Header";
	private const string _CSRF_HEADER_VALUE = "-";
	private static readonly TimeSpan _renewalMargin = TimeSpan.FromSeconds(60);

	private readonly HttpClient _client = new HttpClient();
	private readonly SemaphoreSlim _gate = new SemaphoreSlim(1, 1);
	private readonly string _host;
	private readonly string _clientId;
	private readonly string _clientSecret;
	private readonly string _scope;
	private string _accessToken;
	private DateTimeOffset _expiresAt;

	public AccessTokenProvider(string host, string clientId, string clientSecret, string scope)
	{
		_host = host;
		_clientId = clientId;
		_clientSecret = clientSecret;
		_scope = scope;
	}

	// Cached token; renewed shortly before it expires, or on demand after a rejected call.
	public async Task<string> GetAccessTokenAsync(bool forceRenewal)
	{
		await _gate.WaitAsync();
		try
		{
			if (forceRenewal || _accessToken == null || DateTimeOffset.UtcNow >= _expiresAt - _renewalMargin)
			{
				await AcquireAccessTokenAsync();
			}

			return _accessToken;
		}
		finally
		{
			_gate.Release();
		}
	}

	// Sends the request with the bearer token; when the call is rejected for authentication, renews the token and sends it once more.
	public async Task<HttpResponseMessage> SendAsync(Func<HttpRequestMessage> createRequest)
	{
		HttpResponseMessage response = await SendOnceAsync(createRequest, false);
		if (response.StatusCode == HttpStatusCode.Unauthorized)
		{
			response.Dispose();
			response = await SendOnceAsync(createRequest, true);
		}

		return response;
	}

	public void Dispose()
	{
		_client.Dispose();
		_gate.Dispose();
	}

	private async Task AcquireAccessTokenAsync()
	{
		using (FormUrlEncodedContent form = new FormUrlEncodedContent(new Dictionary<string, string>
		{
			{ "grant_type", "client_credentials" },
			{ "client_id", _clientId },
			{ "client_secret", _clientSecret },
			{ "scope", _scope }
		}))
		using (HttpResponseMessage response = await _client.PostAsync($"{_host}{_TOKEN_PATH}", form))
		{
			response.EnsureSuccessStatusCode();
			using (JsonDocument document = JsonDocument.Parse(await response.Content.ReadAsStringAsync()))
			{
				_accessToken = document.RootElement.GetProperty("access_token").GetString();
				_expiresAt = DateTimeOffset.UtcNow.AddSeconds(document.RootElement.GetProperty("expires_in").GetInt32());
			}
		}
	}

	private async Task<HttpResponseMessage> SendOnceAsync(Func<HttpRequestMessage> createRequest, bool forceRenewal)
	{
		string token = await GetAccessTokenAsync(forceRenewal);
		HttpResponseMessage response;
		using (HttpRequestMessage request = createRequest())
		{
			request.Headers.Authorization = new AuthenticationHeaderValue(_BEARER_SCHEME, token);
			request.Headers.Add(_CSRF_HEADER_NAME, _CSRF_HEADER_VALUE);
			response = await _client.SendAsync(request);
		}

		return response;
	}
}
```

**.NET Framework 4.7.2**: the same class with the serializer the runtime ships (`System.Web.Extensions`, referenced together with `System.Net.Http`), and C# 7.3 syntax:

```csharp
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

public sealed class AccessTokenProvider : IDisposable
{
	private const string _TOKEN_PATH = "/Relativity/Identity/connect/token";
	private const string _BEARER_SCHEME = "Bearer";
	private const string _CSRF_HEADER_NAME = "X-CSRF-Header";
	private const string _CSRF_HEADER_VALUE = "-";
	private static readonly TimeSpan _renewalMargin = TimeSpan.FromSeconds(60);

	private readonly HttpClient _client = new HttpClient();
	private readonly SemaphoreSlim _gate = new SemaphoreSlim(1, 1);
	private readonly string _host;
	private readonly string _clientId;
	private readonly string _clientSecret;
	private readonly string _scope;
	private string _accessToken;
	private DateTimeOffset _expiresAt;

	public AccessTokenProvider(string host, string clientId, string clientSecret, string scope)
	{
		_host = host;
		_clientId = clientId;
		_clientSecret = clientSecret;
		_scope = scope;
	}

	// Cached token; renewed shortly before it expires, or on demand after a rejected call.
	public async Task<string> GetAccessTokenAsync(bool forceRenewal)
	{
		await _gate.WaitAsync();
		try
		{
			if (forceRenewal || _accessToken == null || DateTimeOffset.UtcNow >= _expiresAt - _renewalMargin)
			{
				await AcquireAccessTokenAsync();
			}

			return _accessToken;
		}
		finally
		{
			_gate.Release();
		}
	}

	// Sends the request with the bearer token; when the call is rejected for authentication, renews the token and sends it once more.
	public async Task<HttpResponseMessage> SendAsync(Func<HttpRequestMessage> createRequest)
	{
		HttpResponseMessage response = await SendOnceAsync(createRequest, false);
		if (response.StatusCode == HttpStatusCode.Unauthorized)
		{
			response.Dispose();
			response = await SendOnceAsync(createRequest, true);
		}

		return response;
	}

	public void Dispose()
	{
		_client.Dispose();
		_gate.Dispose();
	}

	private async Task AcquireAccessTokenAsync()
	{
		using (FormUrlEncodedContent form = new FormUrlEncodedContent(new Dictionary<string, string>
		{
			{ "grant_type", "client_credentials" },
			{ "client_id", _clientId },
			{ "client_secret", _clientSecret },
			{ "scope", _scope }
		}))
		using (HttpResponseMessage response = await _client.PostAsync($"{_host}{_TOKEN_PATH}", form))
		{
			response.EnsureSuccessStatusCode();
			string body = await response.Content.ReadAsStringAsync();
			Dictionary<string, object> fields = new JavaScriptSerializer().Deserialize<Dictionary<string, object>>(body);
			_accessToken = fields["access_token"].ToString();
			_expiresAt = DateTimeOffset.UtcNow.AddSeconds(Convert.ToInt32(fields["expires_in"]));
		}
	}

	private async Task<HttpResponseMessage> SendOnceAsync(Func<HttpRequestMessage> createRequest, bool forceRenewal)
	{
		string token = await GetAccessTokenAsync(forceRenewal);
		HttpResponseMessage response;
		using (HttpRequestMessage request = createRequest())
		{
			request.Headers.Authorization = new AuthenticationHeaderValue(_BEARER_SCHEME, token);
			request.Headers.Add(_CSRF_HEADER_NAME, _CSRF_HEADER_VALUE);
			response = await _client.SendAsync(request);
		}

		return response;
	}
}
```

**The scope depends on what the integration needs, and no single scope is required.** Relativity's documentation shows two values for this grant: `SystemUserInfo` on the REST authentication page, the Usage Reports page and in the official Transfer SDK sample, and `UserInfoAccess` in the Kepler .NET samples. No page says which is correct, whether both work, or what either grants, and the OAuth2 Client's `Scopes` property is documented as one that cannot be set. Use the value the API you call documents, and confirm it.

**Cache the token, and re-acquire it on expiry — do not refresh.** Keep the token for its lifetime (part 3 recommends 60 minutes, Relativity's own wording) and request a new one with the client secret shortly before it expires (the JavaScript, TypeScript and Python examples use 60 seconds), and once when a call is rejected for authentication. Client Credentials does not offer refresh tokens: **verified by NSerio against a live instance; not documented by Relativity** — the Allow Refresh Tokens control does not appear for this grant in the RelativityOne user interface. Relativity's own tables and text on refresh tokens conflict with each other, so re-acquisition with the secret is the advice that holds whichever way they are read.

**Long-running processes that use the Relativity SDK.** A `ServiceFactory` built once from a bearer token keeps that token after it expires. Rebuild the `ServiceFactory` and its proxies with a fresh token before expiry. This is the only advice consistent with the documentation: the package `Relativity.Kepler.Client.SDK` (documented) exposes a settable `Token` and an `HttpMessageHandler` overload but no token-provider overload, and no page documents renewing a token behind a factory.

**The client secret.** It is displayed once, at creation or reset. Store it as Secret Management (Part B of the Migration Documentation) describes; never in source, never in tracked configuration. A reset invalidates the previous secret immediately, so plan the cut-over before resetting it.

**Before deploying, confirm the calling identity.** Every call now runs as the OAuth2 Client's **Context User** — not as the user in the replaced credential, and for an import or a job as much as for a request. If that user has different workspace permissions or item-level security, the code returns fewer rows or `Unauthorized` in production with no change to its logic, and Relativity's audit attributes every call to that one account. Confirm the Context User's permissions match those of the previous identity. Relativity's REST API authentication page describes the Context User as usually a member of the Relativity Administrators group; the OAuth2 Client block (part 3) gives NSerio's recommendation, which differs.

**When the integration acts for a signed-in user.** Client Credentials is one service identity by construction. An integration that acts on behalf of a signed-in user is the Authorization Code case, and moving to it means a new OAuth2 Client, because the flow is fixed when a client is created. Which of the two flows applies depends on who supplies the credential at run time.

Verification: **Verification note — two things, and the first can break working code.**

**The factory is documented for hosted processes only, and for Relativity Server.** It is meant for an agent or another process hosted by the Relativity Service Account; a process that runs elsewhere has no such identity to hand it. The two documentation pages that describe it are Relativity Server pages, and Relativity's RelativityOne documentation does not describe this class, so confirm that the SDK version in use for RelativityOne exposes it before relying on it.

**Confirm the SDK version in use exposes `CreateByRsaBearerToken`.** Where it does not, this is an SDK upgrade before it is a code change. The documentation states that the factory handles authentication and token expiration itself, so the finite-lifetime concern of the other classes is the factory's to manage here, not the code's.

OAuth2 Client: See [OAuth2 Client for Client Credentials](#oauth2-client-for-client-credentials).
<!-- generated:end CS-015 client-credentials -->

#### Authorization Code

<!-- generated:begin CS-015 authorization-code -->
**Flow.** This entry is for Authorization Code. The "Which flow" paragraph of CS-015 in [Part A](part-a-csharp.md#cs-015) says when a match belongs to this flow.

Source form:
```csharp
var importApi = new ImportAPI(userName, password, webServiceUrl);
```

Under token authentication: **First, the destination:** if this Relativity instance is not RelativityOne — including a Relativity Server instance, whose own documentation governs — no migration to RelativityOne applies and the rest of this entry does not apply.

**Change what the entry point receives, not the call site.** Relativity's Import API documentation names one static factory on the type, `ImportAPI.CreateByRsaBearerToken(webServiceUrl)`. **It takes the web service URL and no token**: it uses the bearer token of the identity of the process that runs it, and the documentation says it should be used only by processes hosted by the Relativity Service Account, such as an agent. Where the code runs in such a host, the construction above is replaced by:

```csharp
var importApi = ImportAPI.CreateByRsaBearerToken(webServiceUrl);
```

**For a standalone or desktop process that would obtain an OAuth2 token itself, this guidance gives no code form.** The documentation names no `ImportAPI` factory that accepts a token acquired by the caller, and it lists bearer-token authentication as not supported for a desktop application, where user name and password is the preferred form; this text does not resolve that. Whether the code that uses `importApi` afterwards needs any change is not established here; confirm it against the SDK's documentation for the version in use. The migrated construction must not keep the three-argument form with a credential pair, which is the one this match is about.

Credentials: **Where the credentials come from.** The SDK type establishes that the destination is Relativity, not that it is RelativityOne; if it is not RelativityOne — including a Relativity Server instance, whose own documentation governs — the migration is that service's own token mechanism, and no migration to RelativityOne applies. If it is RelativityOne, the factory above carries no token of the code's own: the identity comes from the hosting process, so the token-acquisition part that follows applies only to code that obtains a token itself, and for the entry point that case has no code form, as stated above.

**Common to both variants**

**Sign in.** Replace the code that collected the user's credentials with a redirect: send the user's browser to the authorize endpoint `<host>/Relativity/Identity/connect/authorize`, which leads to the RelativityOne login page. The authorize endpoint is **verified by NSerio against a live instance; not documented by Relativity**. The request carries the standard authorization-code parameters — `client_id`, `response_type=code`, the registered `redirect_uri`, a `scope`, and a `state` value the application checks on return; these are standard OAuth, and Relativity documents none of them. No discovery document is relied on.

**Receive the code.** The browser returns to the Redirect URL registered on the OAuth2 Client, with the authorization code and the `state` value. Check the `state` before using the code. The Redirect URL must be exactly the one registered (the variants say how).

**Exchange the code.** Send a form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=authorization_code`, the `code`, the same `redirect_uri`, the `client_id` and the `client_secret`, plus the `code_verifier` when PKCE is used. The code is exchanged at that endpoint, which Relativity documents for the Client Credentials grant only; the exchange is **verified by NSerio against a live instance; not documented by Relativity**. Send the `code_verifier` only when the authorize request carried a `code_challenge`; otherwise leave it out.

**.NET** (`HttpClient` and `System.Text.Json`):

```csharp
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

public sealed class AuthorizationCodeRequest
{
	public string Host { get; set; }
	public string AuthorizationCode { get; set; }
	public string RedirectUri { get; set; }
	public string ClientId { get; set; }
	public string ClientSecret { get; set; }

	// The PKCE verifier; null when no code_challenge was sent on the authorize request.
	public string CodeVerifier { get; set; }
}

public static class AuthorizationCodeExchange
{
	private const string _TOKEN_PATH = "/Relativity/Identity/connect/token";

	public static async Task<string> ExchangeAuthorizationCodeAsync(HttpClient client, AuthorizationCodeRequest request)
	{
		Dictionary<string, string> fields = new Dictionary<string, string>
		{
			{ "grant_type", "authorization_code" },
			{ "code", request.AuthorizationCode },
			{ "redirect_uri", request.RedirectUri },
			{ "client_id", request.ClientId },
			{ "client_secret", request.ClientSecret }
		};
		if (!string.IsNullOrEmpty(request.CodeVerifier))
		{
			fields["code_verifier"] = request.CodeVerifier;
		}

		string accessToken;
		using (FormUrlEncodedContent form = new FormUrlEncodedContent(fields))
		using (HttpResponseMessage response = await client.PostAsync($"{request.Host}{_TOKEN_PATH}", form))
		{
			response.EnsureSuccessStatusCode();
			using (JsonDocument document = JsonDocument.Parse(await response.Content.ReadAsStringAsync()))
			{
				accessToken = document.RootElement.GetProperty("access_token").GetString();
			}
		}

		return accessToken;
	}
}
```

**.NET Framework 4.7.2** (`System.Web.Extensions` and C# 7.3 syntax):

```csharp
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

public sealed class AuthorizationCodeRequest
{
	public string Host { get; set; }
	public string AuthorizationCode { get; set; }
	public string RedirectUri { get; set; }
	public string ClientId { get; set; }
	public string ClientSecret { get; set; }

	// The PKCE verifier; null when no code_challenge was sent on the authorize request.
	public string CodeVerifier { get; set; }
}

public static class AuthorizationCodeExchange
{
	private const string _TOKEN_PATH = "/Relativity/Identity/connect/token";

	public static async Task<string> ExchangeAuthorizationCodeAsync(HttpClient client, AuthorizationCodeRequest request)
	{
		Dictionary<string, string> fields = new Dictionary<string, string>
		{
			{ "grant_type", "authorization_code" },
			{ "code", request.AuthorizationCode },
			{ "redirect_uri", request.RedirectUri },
			{ "client_id", request.ClientId },
			{ "client_secret", request.ClientSecret }
		};
		if (!string.IsNullOrEmpty(request.CodeVerifier))
		{
			fields["code_verifier"] = request.CodeVerifier;
		}

		string accessToken;
		using (FormUrlEncodedContent form = new FormUrlEncodedContent(fields))
		using (HttpResponseMessage response = await client.PostAsync($"{request.Host}{_TOKEN_PATH}", form))
		{
			response.EnsureSuccessStatusCode();
			string body = await response.Content.ReadAsStringAsync();
			accessToken = new JavaScriptSerializer().Deserialize<Dictionary<string, object>>(body)["access_token"].ToString();
		}

		return accessToken;
	}
}
```

**Use the token.** Send `Authorization: Bearer <access token>` — the change at the use site in part 1 — with `X-CSRF-Header: -` on REST and Kepler calls.

**On expiry.** With Allow Refresh Tokens enabled on the OAuth2 Client and PKCE used, the application holds a refresh token and, when the access token expires, sends a `grant_type=refresh_token` request with it and the client credentials in place of the code exchange — *inferred* from the standard OAuth shape, not tested. The exchange example above reads only the `access_token` of the response; to renew with a refresh token, read the `refresh_token` from the same response as well and keep it as a secret, and otherwise the application sends the user through the sign-in again when the access token expires. Relativity does not document a refresh token's lifetime, whether it rotates, how it is revoked, or whether a revocation endpoint exists, nor what disabling or deleting the client does to tokens already issued. A refresh token is a secret: store it as Secret Management (Part B of the Migration Documentation) describes, and on compromise disable the client and replace it. Enable refresh tokens only if the application must keep a session beyond one token lifetime without asking the user to sign in again.

**PKCE.** PKCE is required by Relativity's own documentation to obtain a refresh token, and not otherwise: a Code request that does not ask for a refresh token works without it, **verified by NSerio against a live instance; not documented by Relativity**. It is never a blanket requirement. NSerio recommends PKCE with the `S256` method in both variants (NSerio recommendation), and does not claim that Relativity accepts any particular method, which it does not document.

**Scope.** No scope set is prescribed for Code. The scope below works — `openid RelativityRequestOrigin UserInfo UserInfoAccess AuthenticationID RelativityWeb` — **verified by NSerio against a live instance; not documented by Relativity**, verified as working and not as minimal. `state` is sent as standard OAuth practice and `nonce` is not used; Relativity does not document whether it enforces either.

**The application stops collecting the password.** After this change the application never receives, stores or forwards the user's username and password: the user signs in on RelativityOne's own page, and the application holds only tokens. Remove the credential prompt, the form field and any storage of the password that the replaced code needed.

**Single sign-on.** Sign-in happens on RelativityOne's own login page, so the instance's single sign-on and two-factor rules apply unchanged — *inferred* from where the sign-in happens, and not documented for clients that customers create.

**The client secret is required.** Code does not work without a client secret: tested and rejected, **verified by NSerio against a live instance; not documented by Relativity**, which confirms Relativity's own statement that the grant requires one. It is displayed once and is stored as Secret Management (Part B of the Migration Documentation) describes.

**Server-side variant.** For a web application whose code runs on a server:

- **Redirect.** The application receives the code at one exact HTTPS URL per environment, a route of the application, with no wildcards (NSerio recommendation; Relativity's matching rules are not documented). The Relativity administrator registers the exact URL on the OAuth2 Client.
- **Secret and tokens.** The client secret stays on the server and never reaches the browser. The user's tokens are held server-side, keyed to the session, and never in the browser.
- **Exchange.** The server performs the code exchange itself.

**Desktop variant.** For a desktop or command-line application run by a user on their own machine:

- **The system browser.** Open the authorize URL in the user's default browser and not in a web view the application hosts, so the application never sees the password (NSerio recommendation).
- **A loopback redirect on a fixed port.** Listen on the loopback address only, on the registered port, for one request. The Redirect URL is `http://127.0.0.1:<port>/<path>/`, ending with a slash because the listener below registers it as its prefix: a plain-HTTP loopback URL is accepted, **verified by NSerio against a live instance; not documented by Relativity**. The Relativity administrator registers that exact URL; because it is registered in advance, the port is fixed and not chosen at runtime.

```csharp
using System;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Net;
using System.Threading.Tasks;

public static class LoopbackAuthorizationReceiver
{
	private const string _CODE_PARAMETER = "code";
	private const string _STATE_PARAMETER = "state";
	private const string _ERROR_PARAMETER = "error";
	private const string _ERROR_DESCRIPTION_PARAMETER = "error_description";
	private const string _STATE_MISMATCH_MESSAGE = "The returned state is not the one that was sent; the response is rejected.";
	private const string _MISSING_CODE_MESSAGE = "The response carries no authorization code.";

	// Waits for the one request the browser sends to the registered loopback Redirect URL, and returns its code
	// only when its state is the one that was sent on the authorize request and the response carries no error.
	// redirectUrl is the exact URL registered on the OAuth2 Client, for example http://127.0.0.1:<port>/<path>/ :
	// it ends with a slash, as a listener prefix must.
	public static async Task<string> ReceiveAuthorizationCodeAsync(string authorizeUrl, string redirectUrl, string expectedState)
	{
		string authorizationCode;
		using (HttpListener listener = new HttpListener())
		{
			listener.Prefixes.Add(redirectUrl);
			listener.Start();
			Process.Start(new ProcessStartInfo(authorizeUrl) { UseShellExecute = true })?.Dispose();
			HttpListenerContext context = await listener.GetContextAsync();
			NameValueCollection query = context.Request.QueryString;
			context.Response.Close();
			if (query[_STATE_PARAMETER] != expectedState)
			{
				throw new InvalidOperationException(_STATE_MISMATCH_MESSAGE);
			}

			if (!string.IsNullOrEmpty(query[_ERROR_PARAMETER]))
			{
				throw new InvalidOperationException($"The authorization server returned an error: {query[_ERROR_PARAMETER]} {query[_ERROR_DESCRIPTION_PARAMETER]}".TrimEnd());
			}

			authorizationCode = query[_CODE_PARAMETER];
			if (string.IsNullOrEmpty(authorizationCode))
			{
				throw new InvalidOperationException(_MISSING_CODE_MESSAGE);
			}
		}

		return authorizationCode;
	}
}
```

- **The secret is extractable.** Code requires a client secret (above), so a distributed desktop application holds one, and **a secret shipped inside a distributed binary can be extracted by anyone who holds the binary**. It protects nothing against the person running the application.
- **Compensating guidance** (NSerio recommendation, subject to your own security policy): set Trusted IPs where the users' outbound addresses are known and stable — they restrict where a token can be *requested*, and are checked when a token is issued against the client's ID and secret; use the recommended short Access Token Lifetime of 60 minutes; enable refresh tokens only when needed; and create one OAuth2 Client per distribution, so a leaked secret can be disabled without affecting the others.
- **A documentation gap.** Relativity documents no recommendation for a distributed desktop application; this design is what NSerio verified works.

Verification: **Verification note — two things, and the first can break working code.**

**The factory is documented for hosted processes only, and for Relativity Server.** It is meant for an agent or another process hosted by the Relativity Service Account; a process that runs elsewhere has no such identity to hand it. The two documentation pages that describe it are Relativity Server pages, and Relativity's RelativityOne documentation does not describe this class, so confirm that the SDK version in use for RelativityOne exposes it before relying on it.

**Confirm the SDK version in use exposes `CreateByRsaBearerToken`.** Where it does not, this is an SDK upgrade before it is a code change. The documentation states that the factory handles authentication and token expiration itself, so the finite-lifetime concern of the other classes is the factory's to manage here, not the code's.

OAuth2 Client: See [OAuth2 Client for Authorization Code](#oauth2-client-for-authorization-code).
<!-- generated:end CS-015 authorization-code -->

### CS-016 · CredentialAssembledAcrossStatements

<a id="cs-016-migration"></a>

#### Client Credentials

<!-- generated:begin CS-016 client-credentials -->
**Flow.** This entry is for Client Credentials. The "Which flow" paragraph of CS-016 in [Part A](part-a-csharp.md#cs-016) says when a match belongs to this flow.

Source form:
```csharp
NetworkCredential credential = new NetworkCredential { UserName = userName };
credential.Password = password;
```

Under token authentication: **First, the destination:** if it is not RelativityOne, no migration to RelativityOne applies — that service's own token mechanism is the migration — and the rest of this entry does not apply.

**Identify the destination first**, because a credential assembled across statements is usually consumed somewhere other than where it is built. Then **remove the credential from its consumer and set the bearer header on each request** that consumer sends:

```csharp
request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
```

Credentials: **Where the credentials come from.** The destination decides whether the migration applies. If the destination is not RelativityOne, the migration is that service's own token mechanism, and no migration to RelativityOne applies. If it is RelativityOne, the token comes from the token-acquisition part that follows.

**Where the token comes from.** Create an OAuth2 Client with Flow Grant Type set to Client Credentials (part 3 lists its settings), then send a form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=client_credentials`, `client_id`, `client_secret` and a `scope`. The token endpoint is **documented** for this grant. The token is then sent as `Authorization: Bearer <token>`, with `X-CSRF-Header: -` on REST and Kepler calls — the header is documented as required, and a Kepler call without it returns 404.

**.NET** (`HttpClient` and `System.Text.Json`). The class requests the token with the client ID and the client secret, caches it, requests a new one 60 seconds before it expires, and, when a call is rejected for authentication, requests a new one and sends the call once more. Build one instance for the life of the process and send every call through `SendAsync`; the request is created by the delegate on each attempt, because a request message cannot be sent twice. The client ID and the client secret come from the process environment or a secret store, never from source. Where the code uses the Relativity SDK, `GetAccessTokenAsync(false)` gives the token for `BearerTokenCredentials` each time a `ServiceFactory` is built.

```csharp
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

public sealed class AccessTokenProvider : IDisposable
{
	private const string _TOKEN_PATH = "/Relativity/Identity/connect/token";
	private const string _BEARER_SCHEME = "Bearer";
	private const string _CSRF_HEADER_NAME = "X-CSRF-Header";
	private const string _CSRF_HEADER_VALUE = "-";
	private static readonly TimeSpan _renewalMargin = TimeSpan.FromSeconds(60);

	private readonly HttpClient _client = new HttpClient();
	private readonly SemaphoreSlim _gate = new SemaphoreSlim(1, 1);
	private readonly string _host;
	private readonly string _clientId;
	private readonly string _clientSecret;
	private readonly string _scope;
	private string _accessToken;
	private DateTimeOffset _expiresAt;

	public AccessTokenProvider(string host, string clientId, string clientSecret, string scope)
	{
		_host = host;
		_clientId = clientId;
		_clientSecret = clientSecret;
		_scope = scope;
	}

	// Cached token; renewed shortly before it expires, or on demand after a rejected call.
	public async Task<string> GetAccessTokenAsync(bool forceRenewal)
	{
		await _gate.WaitAsync();
		try
		{
			if (forceRenewal || _accessToken == null || DateTimeOffset.UtcNow >= _expiresAt - _renewalMargin)
			{
				await AcquireAccessTokenAsync();
			}

			return _accessToken;
		}
		finally
		{
			_gate.Release();
		}
	}

	// Sends the request with the bearer token; when the call is rejected for authentication, renews the token and sends it once more.
	public async Task<HttpResponseMessage> SendAsync(Func<HttpRequestMessage> createRequest)
	{
		HttpResponseMessage response = await SendOnceAsync(createRequest, false);
		if (response.StatusCode == HttpStatusCode.Unauthorized)
		{
			response.Dispose();
			response = await SendOnceAsync(createRequest, true);
		}

		return response;
	}

	public void Dispose()
	{
		_client.Dispose();
		_gate.Dispose();
	}

	private async Task AcquireAccessTokenAsync()
	{
		using (FormUrlEncodedContent form = new FormUrlEncodedContent(new Dictionary<string, string>
		{
			{ "grant_type", "client_credentials" },
			{ "client_id", _clientId },
			{ "client_secret", _clientSecret },
			{ "scope", _scope }
		}))
		using (HttpResponseMessage response = await _client.PostAsync($"{_host}{_TOKEN_PATH}", form))
		{
			response.EnsureSuccessStatusCode();
			using (JsonDocument document = JsonDocument.Parse(await response.Content.ReadAsStringAsync()))
			{
				_accessToken = document.RootElement.GetProperty("access_token").GetString();
				_expiresAt = DateTimeOffset.UtcNow.AddSeconds(document.RootElement.GetProperty("expires_in").GetInt32());
			}
		}
	}

	private async Task<HttpResponseMessage> SendOnceAsync(Func<HttpRequestMessage> createRequest, bool forceRenewal)
	{
		string token = await GetAccessTokenAsync(forceRenewal);
		HttpResponseMessage response;
		using (HttpRequestMessage request = createRequest())
		{
			request.Headers.Authorization = new AuthenticationHeaderValue(_BEARER_SCHEME, token);
			request.Headers.Add(_CSRF_HEADER_NAME, _CSRF_HEADER_VALUE);
			response = await _client.SendAsync(request);
		}

		return response;
	}
}
```

**.NET Framework 4.7.2**: the same class with the serializer the runtime ships (`System.Web.Extensions`, referenced together with `System.Net.Http`), and C# 7.3 syntax:

```csharp
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

public sealed class AccessTokenProvider : IDisposable
{
	private const string _TOKEN_PATH = "/Relativity/Identity/connect/token";
	private const string _BEARER_SCHEME = "Bearer";
	private const string _CSRF_HEADER_NAME = "X-CSRF-Header";
	private const string _CSRF_HEADER_VALUE = "-";
	private static readonly TimeSpan _renewalMargin = TimeSpan.FromSeconds(60);

	private readonly HttpClient _client = new HttpClient();
	private readonly SemaphoreSlim _gate = new SemaphoreSlim(1, 1);
	private readonly string _host;
	private readonly string _clientId;
	private readonly string _clientSecret;
	private readonly string _scope;
	private string _accessToken;
	private DateTimeOffset _expiresAt;

	public AccessTokenProvider(string host, string clientId, string clientSecret, string scope)
	{
		_host = host;
		_clientId = clientId;
		_clientSecret = clientSecret;
		_scope = scope;
	}

	// Cached token; renewed shortly before it expires, or on demand after a rejected call.
	public async Task<string> GetAccessTokenAsync(bool forceRenewal)
	{
		await _gate.WaitAsync();
		try
		{
			if (forceRenewal || _accessToken == null || DateTimeOffset.UtcNow >= _expiresAt - _renewalMargin)
			{
				await AcquireAccessTokenAsync();
			}

			return _accessToken;
		}
		finally
		{
			_gate.Release();
		}
	}

	// Sends the request with the bearer token; when the call is rejected for authentication, renews the token and sends it once more.
	public async Task<HttpResponseMessage> SendAsync(Func<HttpRequestMessage> createRequest)
	{
		HttpResponseMessage response = await SendOnceAsync(createRequest, false);
		if (response.StatusCode == HttpStatusCode.Unauthorized)
		{
			response.Dispose();
			response = await SendOnceAsync(createRequest, true);
		}

		return response;
	}

	public void Dispose()
	{
		_client.Dispose();
		_gate.Dispose();
	}

	private async Task AcquireAccessTokenAsync()
	{
		using (FormUrlEncodedContent form = new FormUrlEncodedContent(new Dictionary<string, string>
		{
			{ "grant_type", "client_credentials" },
			{ "client_id", _clientId },
			{ "client_secret", _clientSecret },
			{ "scope", _scope }
		}))
		using (HttpResponseMessage response = await _client.PostAsync($"{_host}{_TOKEN_PATH}", form))
		{
			response.EnsureSuccessStatusCode();
			string body = await response.Content.ReadAsStringAsync();
			Dictionary<string, object> fields = new JavaScriptSerializer().Deserialize<Dictionary<string, object>>(body);
			_accessToken = fields["access_token"].ToString();
			_expiresAt = DateTimeOffset.UtcNow.AddSeconds(Convert.ToInt32(fields["expires_in"]));
		}
	}

	private async Task<HttpResponseMessage> SendOnceAsync(Func<HttpRequestMessage> createRequest, bool forceRenewal)
	{
		string token = await GetAccessTokenAsync(forceRenewal);
		HttpResponseMessage response;
		using (HttpRequestMessage request = createRequest())
		{
			request.Headers.Authorization = new AuthenticationHeaderValue(_BEARER_SCHEME, token);
			request.Headers.Add(_CSRF_HEADER_NAME, _CSRF_HEADER_VALUE);
			response = await _client.SendAsync(request);
		}

		return response;
	}
}
```

**The scope depends on what the integration needs, and no single scope is required.** Relativity's documentation shows two values for this grant: `SystemUserInfo` on the REST authentication page, the Usage Reports page and in the official Transfer SDK sample, and `UserInfoAccess` in the Kepler .NET samples. No page says which is correct, whether both work, or what either grants, and the OAuth2 Client's `Scopes` property is documented as one that cannot be set. Use the value the API you call documents, and confirm it.

**Cache the token, and re-acquire it on expiry — do not refresh.** Keep the token for its lifetime (part 3 recommends 60 minutes, Relativity's own wording) and request a new one with the client secret shortly before it expires (the JavaScript, TypeScript and Python examples use 60 seconds), and once when a call is rejected for authentication. Client Credentials does not offer refresh tokens: **verified by NSerio against a live instance; not documented by Relativity** — the Allow Refresh Tokens control does not appear for this grant in the RelativityOne user interface. Relativity's own tables and text on refresh tokens conflict with each other, so re-acquisition with the secret is the advice that holds whichever way they are read.

**Long-running processes that use the Relativity SDK.** A `ServiceFactory` built once from a bearer token keeps that token after it expires. Rebuild the `ServiceFactory` and its proxies with a fresh token before expiry. This is the only advice consistent with the documentation: the package `Relativity.Kepler.Client.SDK` (documented) exposes a settable `Token` and an `HttpMessageHandler` overload but no token-provider overload, and no page documents renewing a token behind a factory.

**The client secret.** It is displayed once, at creation or reset. Store it as Secret Management (Part B of the Migration Documentation) describes; never in source, never in tracked configuration. A reset invalidates the previous secret immediately, so plan the cut-over before resetting it.

**Before deploying, confirm the calling identity.** Every call now runs as the OAuth2 Client's **Context User** — not as the user in the replaced credential, and for an import or a job as much as for a request. If that user has different workspace permissions or item-level security, the code returns fewer rows or `Unauthorized` in production with no change to its logic, and Relativity's audit attributes every call to that one account. Confirm the Context User's permissions match those of the previous identity. Relativity's REST API authentication page describes the Context User as usually a member of the Relativity Administrators group; the OAuth2 Client block (part 3) gives NSerio's recommendation, which differs.

**When the integration acts for a signed-in user.** Client Credentials is one service identity by construction. An integration that acts on behalf of a signed-in user is the Authorization Code case, and moving to it means a new OAuth2 Client, because the flow is fixed when a client is created. Which of the two flows applies depends on who supplies the credential at run time.

Verification: **Verification note — seven things.**

**Confirm the consumer is an HTTP destination.** The same form bound to a mail, file-transfer or directory client is not a migration target, and the construct alone does not show which consumer it has.

**Confirm the exchange was Basic.** `NetworkCredential` declares no scheme and serves integrated Windows authentication as readily as Basic; if the exchange is integrated, the migration does not apply.

**The 401-challenge response is lost** wherever the consumer answered a challenge with this credential; a bearer header is sent up front and answers nothing.

**Redirects stop carrying the credential.** Depending on the runtime and the credential type, a consumer's credentials may be re-applied to a redirect target; a manually set `Authorization` header never is. .NET documents that `HttpWebRequest` clears the `Authorization` header when it follows a redirect automatically, and the `HttpClient` handler drops it on redirect. Code that relied on a redirect can start returning 401 after the migration; address the final URI, or handle the redirect explicitly.

**A token has a finite lifetime, and a header set once on a long-lived client cannot be refreshed.** Set it per request, or the code works for the token's lifetime and then returns 401 until the process restarts.

**A SOAP service may not accept bearer at all.** Confirm it does before changing a binding.

**Confirm the endpoint in use is the RelativityOne REST API.** That is the API whose bearer token this guidance describes; an endpoint elsewhere reached from the same code may accept only Basic and reject the migrated call.

OAuth2 Client: See [OAuth2 Client for Client Credentials](#oauth2-client-for-client-credentials).
<!-- generated:end CS-016 client-credentials -->

#### Authorization Code

<!-- generated:begin CS-016 authorization-code -->
**Flow.** This entry is for Authorization Code. The "Which flow" paragraph of CS-016 in [Part A](part-a-csharp.md#cs-016) says when a match belongs to this flow.

Source form:
```csharp
NetworkCredential credential = new NetworkCredential { UserName = userName };
credential.Password = password;
```

Under token authentication: **First, the destination:** if it is not RelativityOne, no migration to RelativityOne applies — that service's own token mechanism is the migration — and the rest of this entry does not apply.

**Identify the destination first**, because a credential assembled across statements is usually consumed somewhere other than where it is built. Then **remove the credential from its consumer and set the bearer header on each request** that consumer sends:

```csharp
request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
```

Credentials: **Where the credentials come from.** The destination decides whether the migration applies. If the destination is not RelativityOne, the migration is that service's own token mechanism, and no migration to RelativityOne applies. If it is RelativityOne, the token comes from the token-acquisition part that follows.

**Common to both variants**

**Sign in.** Replace the code that collected the user's credentials with a redirect: send the user's browser to the authorize endpoint `<host>/Relativity/Identity/connect/authorize`, which leads to the RelativityOne login page. The authorize endpoint is **verified by NSerio against a live instance; not documented by Relativity**. The request carries the standard authorization-code parameters — `client_id`, `response_type=code`, the registered `redirect_uri`, a `scope`, and a `state` value the application checks on return; these are standard OAuth, and Relativity documents none of them. No discovery document is relied on.

**Receive the code.** The browser returns to the Redirect URL registered on the OAuth2 Client, with the authorization code and the `state` value. Check the `state` before using the code. The Redirect URL must be exactly the one registered (the variants say how).

**Exchange the code.** Send a form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=authorization_code`, the `code`, the same `redirect_uri`, the `client_id` and the `client_secret`, plus the `code_verifier` when PKCE is used. The code is exchanged at that endpoint, which Relativity documents for the Client Credentials grant only; the exchange is **verified by NSerio against a live instance; not documented by Relativity**. Send the `code_verifier` only when the authorize request carried a `code_challenge`; otherwise leave it out.

**.NET** (`HttpClient` and `System.Text.Json`):

```csharp
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

public sealed class AuthorizationCodeRequest
{
	public string Host { get; set; }
	public string AuthorizationCode { get; set; }
	public string RedirectUri { get; set; }
	public string ClientId { get; set; }
	public string ClientSecret { get; set; }

	// The PKCE verifier; null when no code_challenge was sent on the authorize request.
	public string CodeVerifier { get; set; }
}

public static class AuthorizationCodeExchange
{
	private const string _TOKEN_PATH = "/Relativity/Identity/connect/token";

	public static async Task<string> ExchangeAuthorizationCodeAsync(HttpClient client, AuthorizationCodeRequest request)
	{
		Dictionary<string, string> fields = new Dictionary<string, string>
		{
			{ "grant_type", "authorization_code" },
			{ "code", request.AuthorizationCode },
			{ "redirect_uri", request.RedirectUri },
			{ "client_id", request.ClientId },
			{ "client_secret", request.ClientSecret }
		};
		if (!string.IsNullOrEmpty(request.CodeVerifier))
		{
			fields["code_verifier"] = request.CodeVerifier;
		}

		string accessToken;
		using (FormUrlEncodedContent form = new FormUrlEncodedContent(fields))
		using (HttpResponseMessage response = await client.PostAsync($"{request.Host}{_TOKEN_PATH}", form))
		{
			response.EnsureSuccessStatusCode();
			using (JsonDocument document = JsonDocument.Parse(await response.Content.ReadAsStringAsync()))
			{
				accessToken = document.RootElement.GetProperty("access_token").GetString();
			}
		}

		return accessToken;
	}
}
```

**.NET Framework 4.7.2** (`System.Web.Extensions` and C# 7.3 syntax):

```csharp
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

public sealed class AuthorizationCodeRequest
{
	public string Host { get; set; }
	public string AuthorizationCode { get; set; }
	public string RedirectUri { get; set; }
	public string ClientId { get; set; }
	public string ClientSecret { get; set; }

	// The PKCE verifier; null when no code_challenge was sent on the authorize request.
	public string CodeVerifier { get; set; }
}

public static class AuthorizationCodeExchange
{
	private const string _TOKEN_PATH = "/Relativity/Identity/connect/token";

	public static async Task<string> ExchangeAuthorizationCodeAsync(HttpClient client, AuthorizationCodeRequest request)
	{
		Dictionary<string, string> fields = new Dictionary<string, string>
		{
			{ "grant_type", "authorization_code" },
			{ "code", request.AuthorizationCode },
			{ "redirect_uri", request.RedirectUri },
			{ "client_id", request.ClientId },
			{ "client_secret", request.ClientSecret }
		};
		if (!string.IsNullOrEmpty(request.CodeVerifier))
		{
			fields["code_verifier"] = request.CodeVerifier;
		}

		string accessToken;
		using (FormUrlEncodedContent form = new FormUrlEncodedContent(fields))
		using (HttpResponseMessage response = await client.PostAsync($"{request.Host}{_TOKEN_PATH}", form))
		{
			response.EnsureSuccessStatusCode();
			string body = await response.Content.ReadAsStringAsync();
			accessToken = new JavaScriptSerializer().Deserialize<Dictionary<string, object>>(body)["access_token"].ToString();
		}

		return accessToken;
	}
}
```

**Use the token.** Send `Authorization: Bearer <access token>` — the change at the use site in part 1 — with `X-CSRF-Header: -` on REST and Kepler calls.

**On expiry.** With Allow Refresh Tokens enabled on the OAuth2 Client and PKCE used, the application holds a refresh token and, when the access token expires, sends a `grant_type=refresh_token` request with it and the client credentials in place of the code exchange — *inferred* from the standard OAuth shape, not tested. The exchange example above reads only the `access_token` of the response; to renew with a refresh token, read the `refresh_token` from the same response as well and keep it as a secret, and otherwise the application sends the user through the sign-in again when the access token expires. Relativity does not document a refresh token's lifetime, whether it rotates, how it is revoked, or whether a revocation endpoint exists, nor what disabling or deleting the client does to tokens already issued. A refresh token is a secret: store it as Secret Management (Part B of the Migration Documentation) describes, and on compromise disable the client and replace it. Enable refresh tokens only if the application must keep a session beyond one token lifetime without asking the user to sign in again.

**PKCE.** PKCE is required by Relativity's own documentation to obtain a refresh token, and not otherwise: a Code request that does not ask for a refresh token works without it, **verified by NSerio against a live instance; not documented by Relativity**. It is never a blanket requirement. NSerio recommends PKCE with the `S256` method in both variants (NSerio recommendation), and does not claim that Relativity accepts any particular method, which it does not document.

**Scope.** No scope set is prescribed for Code. The scope below works — `openid RelativityRequestOrigin UserInfo UserInfoAccess AuthenticationID RelativityWeb` — **verified by NSerio against a live instance; not documented by Relativity**, verified as working and not as minimal. `state` is sent as standard OAuth practice and `nonce` is not used; Relativity does not document whether it enforces either.

**The application stops collecting the password.** After this change the application never receives, stores or forwards the user's username and password: the user signs in on RelativityOne's own page, and the application holds only tokens. Remove the credential prompt, the form field and any storage of the password that the replaced code needed.

**Single sign-on.** Sign-in happens on RelativityOne's own login page, so the instance's single sign-on and two-factor rules apply unchanged — *inferred* from where the sign-in happens, and not documented for clients that customers create.

**The client secret is required.** Code does not work without a client secret: tested and rejected, **verified by NSerio against a live instance; not documented by Relativity**, which confirms Relativity's own statement that the grant requires one. It is displayed once and is stored as Secret Management (Part B of the Migration Documentation) describes.

**Server-side variant.** For a web application whose code runs on a server:

- **Redirect.** The application receives the code at one exact HTTPS URL per environment, a route of the application, with no wildcards (NSerio recommendation; Relativity's matching rules are not documented). The Relativity administrator registers the exact URL on the OAuth2 Client.
- **Secret and tokens.** The client secret stays on the server and never reaches the browser. The user's tokens are held server-side, keyed to the session, and never in the browser.
- **Exchange.** The server performs the code exchange itself.

**Desktop variant.** For a desktop or command-line application run by a user on their own machine:

- **The system browser.** Open the authorize URL in the user's default browser and not in a web view the application hosts, so the application never sees the password (NSerio recommendation).
- **A loopback redirect on a fixed port.** Listen on the loopback address only, on the registered port, for one request. The Redirect URL is `http://127.0.0.1:<port>/<path>/`, ending with a slash because the listener below registers it as its prefix: a plain-HTTP loopback URL is accepted, **verified by NSerio against a live instance; not documented by Relativity**. The Relativity administrator registers that exact URL; because it is registered in advance, the port is fixed and not chosen at runtime.

```csharp
using System;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Net;
using System.Threading.Tasks;

public static class LoopbackAuthorizationReceiver
{
	private const string _CODE_PARAMETER = "code";
	private const string _STATE_PARAMETER = "state";
	private const string _ERROR_PARAMETER = "error";
	private const string _ERROR_DESCRIPTION_PARAMETER = "error_description";
	private const string _STATE_MISMATCH_MESSAGE = "The returned state is not the one that was sent; the response is rejected.";
	private const string _MISSING_CODE_MESSAGE = "The response carries no authorization code.";

	// Waits for the one request the browser sends to the registered loopback Redirect URL, and returns its code
	// only when its state is the one that was sent on the authorize request and the response carries no error.
	// redirectUrl is the exact URL registered on the OAuth2 Client, for example http://127.0.0.1:<port>/<path>/ :
	// it ends with a slash, as a listener prefix must.
	public static async Task<string> ReceiveAuthorizationCodeAsync(string authorizeUrl, string redirectUrl, string expectedState)
	{
		string authorizationCode;
		using (HttpListener listener = new HttpListener())
		{
			listener.Prefixes.Add(redirectUrl);
			listener.Start();
			Process.Start(new ProcessStartInfo(authorizeUrl) { UseShellExecute = true })?.Dispose();
			HttpListenerContext context = await listener.GetContextAsync();
			NameValueCollection query = context.Request.QueryString;
			context.Response.Close();
			if (query[_STATE_PARAMETER] != expectedState)
			{
				throw new InvalidOperationException(_STATE_MISMATCH_MESSAGE);
			}

			if (!string.IsNullOrEmpty(query[_ERROR_PARAMETER]))
			{
				throw new InvalidOperationException($"The authorization server returned an error: {query[_ERROR_PARAMETER]} {query[_ERROR_DESCRIPTION_PARAMETER]}".TrimEnd());
			}

			authorizationCode = query[_CODE_PARAMETER];
			if (string.IsNullOrEmpty(authorizationCode))
			{
				throw new InvalidOperationException(_MISSING_CODE_MESSAGE);
			}
		}

		return authorizationCode;
	}
}
```

- **The secret is extractable.** Code requires a client secret (above), so a distributed desktop application holds one, and **a secret shipped inside a distributed binary can be extracted by anyone who holds the binary**. It protects nothing against the person running the application.
- **Compensating guidance** (NSerio recommendation, subject to your own security policy): set Trusted IPs where the users' outbound addresses are known and stable — they restrict where a token can be *requested*, and are checked when a token is issued against the client's ID and secret; use the recommended short Access Token Lifetime of 60 minutes; enable refresh tokens only when needed; and create one OAuth2 Client per distribution, so a leaked secret can be disabled without affecting the others.
- **A documentation gap.** Relativity documents no recommendation for a distributed desktop application; this design is what NSerio verified works.

Verification: **Verification note — seven things.**

**Confirm the consumer is an HTTP destination.** The same form bound to a mail, file-transfer or directory client is not a migration target, and the construct alone does not show which consumer it has.

**Confirm the exchange was Basic.** `NetworkCredential` declares no scheme and serves integrated Windows authentication as readily as Basic; if the exchange is integrated, the migration does not apply.

**The 401-challenge response is lost** wherever the consumer answered a challenge with this credential; a bearer header is sent up front and answers nothing.

**Redirects stop carrying the credential.** Depending on the runtime and the credential type, a consumer's credentials may be re-applied to a redirect target; a manually set `Authorization` header never is. .NET documents that `HttpWebRequest` clears the `Authorization` header when it follows a redirect automatically, and the `HttpClient` handler drops it on redirect. Code that relied on a redirect can start returning 401 after the migration; address the final URI, or handle the redirect explicitly.

**A token has a finite lifetime, and a header set once on a long-lived client cannot be refreshed.** Set it per request, or the code works for the token's lifetime and then returns 401 until the process restarts.

**A SOAP service may not accept bearer at all.** Confirm it does before changing a binding.

**Confirm the endpoint in use is the RelativityOne REST API.** That is the API whose bearer token this guidance describes; an endpoint elsewhere reached from the same code may accept only Basic and reject the migrated call.

OAuth2 Client: See [OAuth2 Client for Authorization Code](#oauth2-client-for-authorization-code).
<!-- generated:end CS-016 authorization-code -->

### CS-017 · SdkSettingsCredentialProperties

<a id="cs-017-migration"></a>

#### Client Credentials

<!-- generated:begin CS-017 client-credentials -->
**Flow.** This entry is for Client Credentials. The "Which flow" paragraph of CS-017 in [Part A](part-a-csharp.md#cs-017) says when a match belongs to this flow.

Source form:
```csharp
importJob.Settings.RelativityUsername = userName;
importJob.Settings.RelativityPassword = password;
```

Under token authentication: **First, the destination:** if this Relativity instance is not RelativityOne — including a Relativity Server instance, whose own documentation governs — no migration to RelativityOne applies and the rest of this entry does not apply.

Replace the username and password properties with **the settings object's token-bearing equivalent**, assigned the access token, and remove both credential assignments. This guidance does not name that member, because which one exists depends on the SDK version. **Confirm the SDK version in use exposes one; where it does not, this is an SDK upgrade before it is a code change.**

Credentials: **Where the credentials come from.** The property names establish that the destination is Relativity, not that it is RelativityOne; if it is not RelativityOne — including a Relativity Server instance, whose own documentation governs — the migration is that service's own token mechanism, and no migration to RelativityOne applies. If it is RelativityOne, the token comes from the token-acquisition part that follows.

**Where the token comes from.** Create an OAuth2 Client with Flow Grant Type set to Client Credentials (part 3 lists its settings), then send a form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=client_credentials`, `client_id`, `client_secret` and a `scope`. The token endpoint is **documented** for this grant. The token is then sent as `Authorization: Bearer <token>`, with `X-CSRF-Header: -` on REST and Kepler calls — the header is documented as required, and a Kepler call without it returns 404.

**.NET** (`HttpClient` and `System.Text.Json`). The class requests the token with the client ID and the client secret, caches it, requests a new one 60 seconds before it expires, and, when a call is rejected for authentication, requests a new one and sends the call once more. Build one instance for the life of the process and send every call through `SendAsync`; the request is created by the delegate on each attempt, because a request message cannot be sent twice. The client ID and the client secret come from the process environment or a secret store, never from source. Where the code uses the Relativity SDK, `GetAccessTokenAsync(false)` gives the token for `BearerTokenCredentials` each time a `ServiceFactory` is built.

```csharp
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

public sealed class AccessTokenProvider : IDisposable
{
	private const string _TOKEN_PATH = "/Relativity/Identity/connect/token";
	private const string _BEARER_SCHEME = "Bearer";
	private const string _CSRF_HEADER_NAME = "X-CSRF-Header";
	private const string _CSRF_HEADER_VALUE = "-";
	private static readonly TimeSpan _renewalMargin = TimeSpan.FromSeconds(60);

	private readonly HttpClient _client = new HttpClient();
	private readonly SemaphoreSlim _gate = new SemaphoreSlim(1, 1);
	private readonly string _host;
	private readonly string _clientId;
	private readonly string _clientSecret;
	private readonly string _scope;
	private string _accessToken;
	private DateTimeOffset _expiresAt;

	public AccessTokenProvider(string host, string clientId, string clientSecret, string scope)
	{
		_host = host;
		_clientId = clientId;
		_clientSecret = clientSecret;
		_scope = scope;
	}

	// Cached token; renewed shortly before it expires, or on demand after a rejected call.
	public async Task<string> GetAccessTokenAsync(bool forceRenewal)
	{
		await _gate.WaitAsync();
		try
		{
			if (forceRenewal || _accessToken == null || DateTimeOffset.UtcNow >= _expiresAt - _renewalMargin)
			{
				await AcquireAccessTokenAsync();
			}

			return _accessToken;
		}
		finally
		{
			_gate.Release();
		}
	}

	// Sends the request with the bearer token; when the call is rejected for authentication, renews the token and sends it once more.
	public async Task<HttpResponseMessage> SendAsync(Func<HttpRequestMessage> createRequest)
	{
		HttpResponseMessage response = await SendOnceAsync(createRequest, false);
		if (response.StatusCode == HttpStatusCode.Unauthorized)
		{
			response.Dispose();
			response = await SendOnceAsync(createRequest, true);
		}

		return response;
	}

	public void Dispose()
	{
		_client.Dispose();
		_gate.Dispose();
	}

	private async Task AcquireAccessTokenAsync()
	{
		using (FormUrlEncodedContent form = new FormUrlEncodedContent(new Dictionary<string, string>
		{
			{ "grant_type", "client_credentials" },
			{ "client_id", _clientId },
			{ "client_secret", _clientSecret },
			{ "scope", _scope }
		}))
		using (HttpResponseMessage response = await _client.PostAsync($"{_host}{_TOKEN_PATH}", form))
		{
			response.EnsureSuccessStatusCode();
			using (JsonDocument document = JsonDocument.Parse(await response.Content.ReadAsStringAsync()))
			{
				_accessToken = document.RootElement.GetProperty("access_token").GetString();
				_expiresAt = DateTimeOffset.UtcNow.AddSeconds(document.RootElement.GetProperty("expires_in").GetInt32());
			}
		}
	}

	private async Task<HttpResponseMessage> SendOnceAsync(Func<HttpRequestMessage> createRequest, bool forceRenewal)
	{
		string token = await GetAccessTokenAsync(forceRenewal);
		HttpResponseMessage response;
		using (HttpRequestMessage request = createRequest())
		{
			request.Headers.Authorization = new AuthenticationHeaderValue(_BEARER_SCHEME, token);
			request.Headers.Add(_CSRF_HEADER_NAME, _CSRF_HEADER_VALUE);
			response = await _client.SendAsync(request);
		}

		return response;
	}
}
```

**.NET Framework 4.7.2**: the same class with the serializer the runtime ships (`System.Web.Extensions`, referenced together with `System.Net.Http`), and C# 7.3 syntax:

```csharp
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

public sealed class AccessTokenProvider : IDisposable
{
	private const string _TOKEN_PATH = "/Relativity/Identity/connect/token";
	private const string _BEARER_SCHEME = "Bearer";
	private const string _CSRF_HEADER_NAME = "X-CSRF-Header";
	private const string _CSRF_HEADER_VALUE = "-";
	private static readonly TimeSpan _renewalMargin = TimeSpan.FromSeconds(60);

	private readonly HttpClient _client = new HttpClient();
	private readonly SemaphoreSlim _gate = new SemaphoreSlim(1, 1);
	private readonly string _host;
	private readonly string _clientId;
	private readonly string _clientSecret;
	private readonly string _scope;
	private string _accessToken;
	private DateTimeOffset _expiresAt;

	public AccessTokenProvider(string host, string clientId, string clientSecret, string scope)
	{
		_host = host;
		_clientId = clientId;
		_clientSecret = clientSecret;
		_scope = scope;
	}

	// Cached token; renewed shortly before it expires, or on demand after a rejected call.
	public async Task<string> GetAccessTokenAsync(bool forceRenewal)
	{
		await _gate.WaitAsync();
		try
		{
			if (forceRenewal || _accessToken == null || DateTimeOffset.UtcNow >= _expiresAt - _renewalMargin)
			{
				await AcquireAccessTokenAsync();
			}

			return _accessToken;
		}
		finally
		{
			_gate.Release();
		}
	}

	// Sends the request with the bearer token; when the call is rejected for authentication, renews the token and sends it once more.
	public async Task<HttpResponseMessage> SendAsync(Func<HttpRequestMessage> createRequest)
	{
		HttpResponseMessage response = await SendOnceAsync(createRequest, false);
		if (response.StatusCode == HttpStatusCode.Unauthorized)
		{
			response.Dispose();
			response = await SendOnceAsync(createRequest, true);
		}

		return response;
	}

	public void Dispose()
	{
		_client.Dispose();
		_gate.Dispose();
	}

	private async Task AcquireAccessTokenAsync()
	{
		using (FormUrlEncodedContent form = new FormUrlEncodedContent(new Dictionary<string, string>
		{
			{ "grant_type", "client_credentials" },
			{ "client_id", _clientId },
			{ "client_secret", _clientSecret },
			{ "scope", _scope }
		}))
		using (HttpResponseMessage response = await _client.PostAsync($"{_host}{_TOKEN_PATH}", form))
		{
			response.EnsureSuccessStatusCode();
			string body = await response.Content.ReadAsStringAsync();
			Dictionary<string, object> fields = new JavaScriptSerializer().Deserialize<Dictionary<string, object>>(body);
			_accessToken = fields["access_token"].ToString();
			_expiresAt = DateTimeOffset.UtcNow.AddSeconds(Convert.ToInt32(fields["expires_in"]));
		}
	}

	private async Task<HttpResponseMessage> SendOnceAsync(Func<HttpRequestMessage> createRequest, bool forceRenewal)
	{
		string token = await GetAccessTokenAsync(forceRenewal);
		HttpResponseMessage response;
		using (HttpRequestMessage request = createRequest())
		{
			request.Headers.Authorization = new AuthenticationHeaderValue(_BEARER_SCHEME, token);
			request.Headers.Add(_CSRF_HEADER_NAME, _CSRF_HEADER_VALUE);
			response = await _client.SendAsync(request);
		}

		return response;
	}
}
```

**The scope depends on what the integration needs, and no single scope is required.** Relativity's documentation shows two values for this grant: `SystemUserInfo` on the REST authentication page, the Usage Reports page and in the official Transfer SDK sample, and `UserInfoAccess` in the Kepler .NET samples. No page says which is correct, whether both work, or what either grants, and the OAuth2 Client's `Scopes` property is documented as one that cannot be set. Use the value the API you call documents, and confirm it.

**Cache the token, and re-acquire it on expiry — do not refresh.** Keep the token for its lifetime (part 3 recommends 60 minutes, Relativity's own wording) and request a new one with the client secret shortly before it expires (the JavaScript, TypeScript and Python examples use 60 seconds), and once when a call is rejected for authentication. Client Credentials does not offer refresh tokens: **verified by NSerio against a live instance; not documented by Relativity** — the Allow Refresh Tokens control does not appear for this grant in the RelativityOne user interface. Relativity's own tables and text on refresh tokens conflict with each other, so re-acquisition with the secret is the advice that holds whichever way they are read.

**Long-running processes that use the Relativity SDK.** A `ServiceFactory` built once from a bearer token keeps that token after it expires. Rebuild the `ServiceFactory` and its proxies with a fresh token before expiry. This is the only advice consistent with the documentation: the package `Relativity.Kepler.Client.SDK` (documented) exposes a settable `Token` and an `HttpMessageHandler` overload but no token-provider overload, and no page documents renewing a token behind a factory.

**The client secret.** It is displayed once, at creation or reset. Store it as Secret Management (Part B of the Migration Documentation) describes; never in source, never in tracked configuration. A reset invalidates the previous secret immediately, so plan the cut-over before resetting it.

**Before deploying, confirm the calling identity.** Every call now runs as the OAuth2 Client's **Context User** — not as the user in the replaced credential, and for an import or a job as much as for a request. If that user has different workspace permissions or item-level security, the code returns fewer rows or `Unauthorized` in production with no change to its logic, and Relativity's audit attributes every call to that one account. Confirm the Context User's permissions match those of the previous identity. Relativity's REST API authentication page describes the Context User as usually a member of the Relativity Administrators group; the OAuth2 Client block (part 3) gives NSerio's recommendation, which differs.

**When the integration acts for a signed-in user.** Client Credentials is one service identity by construction. An integration that acts on behalf of a signed-in user is the Authorization Code case, and moving to it means a new OAuth2 Client, because the flow is fixed when a client is created. Which of the two flows applies depends on who supplies the credential at run time.

Verification: **Verification note — two things. The first decides whether the change can be made at all.**

**SDK version support comes first.** Where the version in use has no token-bearing settings member, the change cannot be made at this call site at all, and the upgrade carries its own verification.

**A token has a finite lifetime.** A settings object populated once and reused across long or repeated jobs holds a token that expires; populate it with a current token for each job.

OAuth2 Client: See [OAuth2 Client for Client Credentials](#oauth2-client-for-client-credentials).
<!-- generated:end CS-017 client-credentials -->

#### Authorization Code

<!-- generated:begin CS-017 authorization-code -->
**Flow.** This entry is for Authorization Code. The "Which flow" paragraph of CS-017 in [Part A](part-a-csharp.md#cs-017) says when a match belongs to this flow.

Source form:
```csharp
importJob.Settings.RelativityUsername = userName;
importJob.Settings.RelativityPassword = password;
```

Under token authentication: **First, the destination:** if this Relativity instance is not RelativityOne — including a Relativity Server instance, whose own documentation governs — no migration to RelativityOne applies and the rest of this entry does not apply.

Replace the username and password properties with **the settings object's token-bearing equivalent**, assigned the access token, and remove both credential assignments. This guidance does not name that member, because which one exists depends on the SDK version. **Confirm the SDK version in use exposes one; where it does not, this is an SDK upgrade before it is a code change.**

Credentials: **Where the credentials come from.** The property names establish that the destination is Relativity, not that it is RelativityOne; if it is not RelativityOne — including a Relativity Server instance, whose own documentation governs — the migration is that service's own token mechanism, and no migration to RelativityOne applies. If it is RelativityOne, the token comes from the token-acquisition part that follows.

**Common to both variants**

**Sign in.** Replace the code that collected the user's credentials with a redirect: send the user's browser to the authorize endpoint `<host>/Relativity/Identity/connect/authorize`, which leads to the RelativityOne login page. The authorize endpoint is **verified by NSerio against a live instance; not documented by Relativity**. The request carries the standard authorization-code parameters — `client_id`, `response_type=code`, the registered `redirect_uri`, a `scope`, and a `state` value the application checks on return; these are standard OAuth, and Relativity documents none of them. No discovery document is relied on.

**Receive the code.** The browser returns to the Redirect URL registered on the OAuth2 Client, with the authorization code and the `state` value. Check the `state` before using the code. The Redirect URL must be exactly the one registered (the variants say how).

**Exchange the code.** Send a form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=authorization_code`, the `code`, the same `redirect_uri`, the `client_id` and the `client_secret`, plus the `code_verifier` when PKCE is used. The code is exchanged at that endpoint, which Relativity documents for the Client Credentials grant only; the exchange is **verified by NSerio against a live instance; not documented by Relativity**. Send the `code_verifier` only when the authorize request carried a `code_challenge`; otherwise leave it out.

**.NET** (`HttpClient` and `System.Text.Json`):

```csharp
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

public sealed class AuthorizationCodeRequest
{
	public string Host { get; set; }
	public string AuthorizationCode { get; set; }
	public string RedirectUri { get; set; }
	public string ClientId { get; set; }
	public string ClientSecret { get; set; }

	// The PKCE verifier; null when no code_challenge was sent on the authorize request.
	public string CodeVerifier { get; set; }
}

public static class AuthorizationCodeExchange
{
	private const string _TOKEN_PATH = "/Relativity/Identity/connect/token";

	public static async Task<string> ExchangeAuthorizationCodeAsync(HttpClient client, AuthorizationCodeRequest request)
	{
		Dictionary<string, string> fields = new Dictionary<string, string>
		{
			{ "grant_type", "authorization_code" },
			{ "code", request.AuthorizationCode },
			{ "redirect_uri", request.RedirectUri },
			{ "client_id", request.ClientId },
			{ "client_secret", request.ClientSecret }
		};
		if (!string.IsNullOrEmpty(request.CodeVerifier))
		{
			fields["code_verifier"] = request.CodeVerifier;
		}

		string accessToken;
		using (FormUrlEncodedContent form = new FormUrlEncodedContent(fields))
		using (HttpResponseMessage response = await client.PostAsync($"{request.Host}{_TOKEN_PATH}", form))
		{
			response.EnsureSuccessStatusCode();
			using (JsonDocument document = JsonDocument.Parse(await response.Content.ReadAsStringAsync()))
			{
				accessToken = document.RootElement.GetProperty("access_token").GetString();
			}
		}

		return accessToken;
	}
}
```

**.NET Framework 4.7.2** (`System.Web.Extensions` and C# 7.3 syntax):

```csharp
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

public sealed class AuthorizationCodeRequest
{
	public string Host { get; set; }
	public string AuthorizationCode { get; set; }
	public string RedirectUri { get; set; }
	public string ClientId { get; set; }
	public string ClientSecret { get; set; }

	// The PKCE verifier; null when no code_challenge was sent on the authorize request.
	public string CodeVerifier { get; set; }
}

public static class AuthorizationCodeExchange
{
	private const string _TOKEN_PATH = "/Relativity/Identity/connect/token";

	public static async Task<string> ExchangeAuthorizationCodeAsync(HttpClient client, AuthorizationCodeRequest request)
	{
		Dictionary<string, string> fields = new Dictionary<string, string>
		{
			{ "grant_type", "authorization_code" },
			{ "code", request.AuthorizationCode },
			{ "redirect_uri", request.RedirectUri },
			{ "client_id", request.ClientId },
			{ "client_secret", request.ClientSecret }
		};
		if (!string.IsNullOrEmpty(request.CodeVerifier))
		{
			fields["code_verifier"] = request.CodeVerifier;
		}

		string accessToken;
		using (FormUrlEncodedContent form = new FormUrlEncodedContent(fields))
		using (HttpResponseMessage response = await client.PostAsync($"{request.Host}{_TOKEN_PATH}", form))
		{
			response.EnsureSuccessStatusCode();
			string body = await response.Content.ReadAsStringAsync();
			accessToken = new JavaScriptSerializer().Deserialize<Dictionary<string, object>>(body)["access_token"].ToString();
		}

		return accessToken;
	}
}
```

**Use the token.** Send `Authorization: Bearer <access token>` — the change at the use site in part 1 — with `X-CSRF-Header: -` on REST and Kepler calls.

**On expiry.** With Allow Refresh Tokens enabled on the OAuth2 Client and PKCE used, the application holds a refresh token and, when the access token expires, sends a `grant_type=refresh_token` request with it and the client credentials in place of the code exchange — *inferred* from the standard OAuth shape, not tested. The exchange example above reads only the `access_token` of the response; to renew with a refresh token, read the `refresh_token` from the same response as well and keep it as a secret, and otherwise the application sends the user through the sign-in again when the access token expires. Relativity does not document a refresh token's lifetime, whether it rotates, how it is revoked, or whether a revocation endpoint exists, nor what disabling or deleting the client does to tokens already issued. A refresh token is a secret: store it as Secret Management (Part B of the Migration Documentation) describes, and on compromise disable the client and replace it. Enable refresh tokens only if the application must keep a session beyond one token lifetime without asking the user to sign in again.

**PKCE.** PKCE is required by Relativity's own documentation to obtain a refresh token, and not otherwise: a Code request that does not ask for a refresh token works without it, **verified by NSerio against a live instance; not documented by Relativity**. It is never a blanket requirement. NSerio recommends PKCE with the `S256` method in both variants (NSerio recommendation), and does not claim that Relativity accepts any particular method, which it does not document.

**Scope.** No scope set is prescribed for Code. The scope below works — `openid RelativityRequestOrigin UserInfo UserInfoAccess AuthenticationID RelativityWeb` — **verified by NSerio against a live instance; not documented by Relativity**, verified as working and not as minimal. `state` is sent as standard OAuth practice and `nonce` is not used; Relativity does not document whether it enforces either.

**The application stops collecting the password.** After this change the application never receives, stores or forwards the user's username and password: the user signs in on RelativityOne's own page, and the application holds only tokens. Remove the credential prompt, the form field and any storage of the password that the replaced code needed.

**Single sign-on.** Sign-in happens on RelativityOne's own login page, so the instance's single sign-on and two-factor rules apply unchanged — *inferred* from where the sign-in happens, and not documented for clients that customers create.

**The client secret is required.** Code does not work without a client secret: tested and rejected, **verified by NSerio against a live instance; not documented by Relativity**, which confirms Relativity's own statement that the grant requires one. It is displayed once and is stored as Secret Management (Part B of the Migration Documentation) describes.

**Server-side variant.** For a web application whose code runs on a server:

- **Redirect.** The application receives the code at one exact HTTPS URL per environment, a route of the application, with no wildcards (NSerio recommendation; Relativity's matching rules are not documented). The Relativity administrator registers the exact URL on the OAuth2 Client.
- **Secret and tokens.** The client secret stays on the server and never reaches the browser. The user's tokens are held server-side, keyed to the session, and never in the browser.
- **Exchange.** The server performs the code exchange itself.

**Desktop variant.** For a desktop or command-line application run by a user on their own machine:

- **The system browser.** Open the authorize URL in the user's default browser and not in a web view the application hosts, so the application never sees the password (NSerio recommendation).
- **A loopback redirect on a fixed port.** Listen on the loopback address only, on the registered port, for one request. The Redirect URL is `http://127.0.0.1:<port>/<path>/`, ending with a slash because the listener below registers it as its prefix: a plain-HTTP loopback URL is accepted, **verified by NSerio against a live instance; not documented by Relativity**. The Relativity administrator registers that exact URL; because it is registered in advance, the port is fixed and not chosen at runtime.

```csharp
using System;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Net;
using System.Threading.Tasks;

public static class LoopbackAuthorizationReceiver
{
	private const string _CODE_PARAMETER = "code";
	private const string _STATE_PARAMETER = "state";
	private const string _ERROR_PARAMETER = "error";
	private const string _ERROR_DESCRIPTION_PARAMETER = "error_description";
	private const string _STATE_MISMATCH_MESSAGE = "The returned state is not the one that was sent; the response is rejected.";
	private const string _MISSING_CODE_MESSAGE = "The response carries no authorization code.";

	// Waits for the one request the browser sends to the registered loopback Redirect URL, and returns its code
	// only when its state is the one that was sent on the authorize request and the response carries no error.
	// redirectUrl is the exact URL registered on the OAuth2 Client, for example http://127.0.0.1:<port>/<path>/ :
	// it ends with a slash, as a listener prefix must.
	public static async Task<string> ReceiveAuthorizationCodeAsync(string authorizeUrl, string redirectUrl, string expectedState)
	{
		string authorizationCode;
		using (HttpListener listener = new HttpListener())
		{
			listener.Prefixes.Add(redirectUrl);
			listener.Start();
			Process.Start(new ProcessStartInfo(authorizeUrl) { UseShellExecute = true })?.Dispose();
			HttpListenerContext context = await listener.GetContextAsync();
			NameValueCollection query = context.Request.QueryString;
			context.Response.Close();
			if (query[_STATE_PARAMETER] != expectedState)
			{
				throw new InvalidOperationException(_STATE_MISMATCH_MESSAGE);
			}

			if (!string.IsNullOrEmpty(query[_ERROR_PARAMETER]))
			{
				throw new InvalidOperationException($"The authorization server returned an error: {query[_ERROR_PARAMETER]} {query[_ERROR_DESCRIPTION_PARAMETER]}".TrimEnd());
			}

			authorizationCode = query[_CODE_PARAMETER];
			if (string.IsNullOrEmpty(authorizationCode))
			{
				throw new InvalidOperationException(_MISSING_CODE_MESSAGE);
			}
		}

		return authorizationCode;
	}
}
```

- **The secret is extractable.** Code requires a client secret (above), so a distributed desktop application holds one, and **a secret shipped inside a distributed binary can be extracted by anyone who holds the binary**. It protects nothing against the person running the application.
- **Compensating guidance** (NSerio recommendation, subject to your own security policy): set Trusted IPs where the users' outbound addresses are known and stable — they restrict where a token can be *requested*, and are checked when a token is issued against the client's ID and secret; use the recommended short Access Token Lifetime of 60 minutes; enable refresh tokens only when needed; and create one OAuth2 Client per distribution, so a leaked secret can be disabled without affecting the others.
- **A documentation gap.** Relativity documents no recommendation for a distributed desktop application; this design is what NSerio verified works.

Verification: **Verification note — two things. The first decides whether the change can be made at all.**

**SDK version support comes first.** Where the version in use has no token-bearing settings member, the change cannot be made at this call site at all, and the upgrade carries its own verification.

**A token has a finite lifetime.** A settings object populated once and reused across long or repeated jobs holds a token that expires; populate it with a current token for each job.

OAuth2 Client: See [OAuth2 Client for Authorization Code](#oauth2-client-for-authorization-code).
<!-- generated:end CS-017 authorization-code -->

### CS-018 · TokenCarriedUnderBasicScheme

<a id="cs-018-migration"></a>

#### Client Credentials

<!-- generated:begin CS-018 client-credentials -->
**Flow.** This entry is for Client Credentials. The "Which flow" paragraph of CS-018 in [Part A](part-a-csharp.md#cs-018) says when a match belongs to this flow.

Source form:
```csharp
string pair = "" + ":" + accessToken;
string encoded = Convert.ToBase64String(Encoding.ASCII.GetBytes(pair));
```

Under token authentication: **This change applies to any destination that accepts `Bearer`**, RelativityOne or not: no credential is exchanged, so only the token's provenance differs when the destination is not RelativityOne.

**Delete the encoding and change the prefix.** The token already in hand is carried verbatim:

```
Authorization: Basic <base64 of ":" + token>      ->      Authorization: Bearer <token>
```

```csharp
request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
```

The header change itself exchanges no credential; where the destination is RelativityOne, the token still comes from an OAuth2 Client, as the token-acquisition part that follows describes. This is the cheapest remediation of any other class.

Credentials: **Where the credentials come from.** The prefix change applies to any destination that accepts `Bearer`. If the destination is RelativityOne, the token that belongs in the header is obtained as the token-acquisition part that follows describes. If it is not RelativityOne, the token's provenance is that service's and does not change: the token already in hand is the one that moves to the `Bearer` prefix, issued however that service issues it.

**Where the token comes from.** Create an OAuth2 Client with Flow Grant Type set to Client Credentials (part 3 lists its settings), then send a form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=client_credentials`, `client_id`, `client_secret` and a `scope`. The token endpoint is **documented** for this grant. The token is then sent as `Authorization: Bearer <token>`, with `X-CSRF-Header: -` on REST and Kepler calls — the header is documented as required, and a Kepler call without it returns 404.

**.NET** (`HttpClient` and `System.Text.Json`). The class requests the token with the client ID and the client secret, caches it, requests a new one 60 seconds before it expires, and, when a call is rejected for authentication, requests a new one and sends the call once more. Build one instance for the life of the process and send every call through `SendAsync`; the request is created by the delegate on each attempt, because a request message cannot be sent twice. The client ID and the client secret come from the process environment or a secret store, never from source. Where the code uses the Relativity SDK, `GetAccessTokenAsync(false)` gives the token for `BearerTokenCredentials` each time a `ServiceFactory` is built.

```csharp
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

public sealed class AccessTokenProvider : IDisposable
{
	private const string _TOKEN_PATH = "/Relativity/Identity/connect/token";
	private const string _BEARER_SCHEME = "Bearer";
	private const string _CSRF_HEADER_NAME = "X-CSRF-Header";
	private const string _CSRF_HEADER_VALUE = "-";
	private static readonly TimeSpan _renewalMargin = TimeSpan.FromSeconds(60);

	private readonly HttpClient _client = new HttpClient();
	private readonly SemaphoreSlim _gate = new SemaphoreSlim(1, 1);
	private readonly string _host;
	private readonly string _clientId;
	private readonly string _clientSecret;
	private readonly string _scope;
	private string _accessToken;
	private DateTimeOffset _expiresAt;

	public AccessTokenProvider(string host, string clientId, string clientSecret, string scope)
	{
		_host = host;
		_clientId = clientId;
		_clientSecret = clientSecret;
		_scope = scope;
	}

	// Cached token; renewed shortly before it expires, or on demand after a rejected call.
	public async Task<string> GetAccessTokenAsync(bool forceRenewal)
	{
		await _gate.WaitAsync();
		try
		{
			if (forceRenewal || _accessToken == null || DateTimeOffset.UtcNow >= _expiresAt - _renewalMargin)
			{
				await AcquireAccessTokenAsync();
			}

			return _accessToken;
		}
		finally
		{
			_gate.Release();
		}
	}

	// Sends the request with the bearer token; when the call is rejected for authentication, renews the token and sends it once more.
	public async Task<HttpResponseMessage> SendAsync(Func<HttpRequestMessage> createRequest)
	{
		HttpResponseMessage response = await SendOnceAsync(createRequest, false);
		if (response.StatusCode == HttpStatusCode.Unauthorized)
		{
			response.Dispose();
			response = await SendOnceAsync(createRequest, true);
		}

		return response;
	}

	public void Dispose()
	{
		_client.Dispose();
		_gate.Dispose();
	}

	private async Task AcquireAccessTokenAsync()
	{
		using (FormUrlEncodedContent form = new FormUrlEncodedContent(new Dictionary<string, string>
		{
			{ "grant_type", "client_credentials" },
			{ "client_id", _clientId },
			{ "client_secret", _clientSecret },
			{ "scope", _scope }
		}))
		using (HttpResponseMessage response = await _client.PostAsync($"{_host}{_TOKEN_PATH}", form))
		{
			response.EnsureSuccessStatusCode();
			using (JsonDocument document = JsonDocument.Parse(await response.Content.ReadAsStringAsync()))
			{
				_accessToken = document.RootElement.GetProperty("access_token").GetString();
				_expiresAt = DateTimeOffset.UtcNow.AddSeconds(document.RootElement.GetProperty("expires_in").GetInt32());
			}
		}
	}

	private async Task<HttpResponseMessage> SendOnceAsync(Func<HttpRequestMessage> createRequest, bool forceRenewal)
	{
		string token = await GetAccessTokenAsync(forceRenewal);
		HttpResponseMessage response;
		using (HttpRequestMessage request = createRequest())
		{
			request.Headers.Authorization = new AuthenticationHeaderValue(_BEARER_SCHEME, token);
			request.Headers.Add(_CSRF_HEADER_NAME, _CSRF_HEADER_VALUE);
			response = await _client.SendAsync(request);
		}

		return response;
	}
}
```

**.NET Framework 4.7.2**: the same class with the serializer the runtime ships (`System.Web.Extensions`, referenced together with `System.Net.Http`), and C# 7.3 syntax:

```csharp
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

public sealed class AccessTokenProvider : IDisposable
{
	private const string _TOKEN_PATH = "/Relativity/Identity/connect/token";
	private const string _BEARER_SCHEME = "Bearer";
	private const string _CSRF_HEADER_NAME = "X-CSRF-Header";
	private const string _CSRF_HEADER_VALUE = "-";
	private static readonly TimeSpan _renewalMargin = TimeSpan.FromSeconds(60);

	private readonly HttpClient _client = new HttpClient();
	private readonly SemaphoreSlim _gate = new SemaphoreSlim(1, 1);
	private readonly string _host;
	private readonly string _clientId;
	private readonly string _clientSecret;
	private readonly string _scope;
	private string _accessToken;
	private DateTimeOffset _expiresAt;

	public AccessTokenProvider(string host, string clientId, string clientSecret, string scope)
	{
		_host = host;
		_clientId = clientId;
		_clientSecret = clientSecret;
		_scope = scope;
	}

	// Cached token; renewed shortly before it expires, or on demand after a rejected call.
	public async Task<string> GetAccessTokenAsync(bool forceRenewal)
	{
		await _gate.WaitAsync();
		try
		{
			if (forceRenewal || _accessToken == null || DateTimeOffset.UtcNow >= _expiresAt - _renewalMargin)
			{
				await AcquireAccessTokenAsync();
			}

			return _accessToken;
		}
		finally
		{
			_gate.Release();
		}
	}

	// Sends the request with the bearer token; when the call is rejected for authentication, renews the token and sends it once more.
	public async Task<HttpResponseMessage> SendAsync(Func<HttpRequestMessage> createRequest)
	{
		HttpResponseMessage response = await SendOnceAsync(createRequest, false);
		if (response.StatusCode == HttpStatusCode.Unauthorized)
		{
			response.Dispose();
			response = await SendOnceAsync(createRequest, true);
		}

		return response;
	}

	public void Dispose()
	{
		_client.Dispose();
		_gate.Dispose();
	}

	private async Task AcquireAccessTokenAsync()
	{
		using (FormUrlEncodedContent form = new FormUrlEncodedContent(new Dictionary<string, string>
		{
			{ "grant_type", "client_credentials" },
			{ "client_id", _clientId },
			{ "client_secret", _clientSecret },
			{ "scope", _scope }
		}))
		using (HttpResponseMessage response = await _client.PostAsync($"{_host}{_TOKEN_PATH}", form))
		{
			response.EnsureSuccessStatusCode();
			string body = await response.Content.ReadAsStringAsync();
			Dictionary<string, object> fields = new JavaScriptSerializer().Deserialize<Dictionary<string, object>>(body);
			_accessToken = fields["access_token"].ToString();
			_expiresAt = DateTimeOffset.UtcNow.AddSeconds(Convert.ToInt32(fields["expires_in"]));
		}
	}

	private async Task<HttpResponseMessage> SendOnceAsync(Func<HttpRequestMessage> createRequest, bool forceRenewal)
	{
		string token = await GetAccessTokenAsync(forceRenewal);
		HttpResponseMessage response;
		using (HttpRequestMessage request = createRequest())
		{
			request.Headers.Authorization = new AuthenticationHeaderValue(_BEARER_SCHEME, token);
			request.Headers.Add(_CSRF_HEADER_NAME, _CSRF_HEADER_VALUE);
			response = await _client.SendAsync(request);
		}

		return response;
	}
}
```

**The scope depends on what the integration needs, and no single scope is required.** Relativity's documentation shows two values for this grant: `SystemUserInfo` on the REST authentication page, the Usage Reports page and in the official Transfer SDK sample, and `UserInfoAccess` in the Kepler .NET samples. No page says which is correct, whether both work, or what either grants, and the OAuth2 Client's `Scopes` property is documented as one that cannot be set. Use the value the API you call documents, and confirm it.

**Cache the token, and re-acquire it on expiry — do not refresh.** Keep the token for its lifetime (part 3 recommends 60 minutes, Relativity's own wording) and request a new one with the client secret shortly before it expires (the JavaScript, TypeScript and Python examples use 60 seconds), and once when a call is rejected for authentication. Client Credentials does not offer refresh tokens: **verified by NSerio against a live instance; not documented by Relativity** — the Allow Refresh Tokens control does not appear for this grant in the RelativityOne user interface. Relativity's own tables and text on refresh tokens conflict with each other, so re-acquisition with the secret is the advice that holds whichever way they are read.

**Long-running processes that use the Relativity SDK.** A `ServiceFactory` built once from a bearer token keeps that token after it expires. Rebuild the `ServiceFactory` and its proxies with a fresh token before expiry. This is the only advice consistent with the documentation: the package `Relativity.Kepler.Client.SDK` (documented) exposes a settable `Token` and an `HttpMessageHandler` overload but no token-provider overload, and no page documents renewing a token behind a factory.

**The client secret.** It is displayed once, at creation or reset. Store it as Secret Management (Part B of the Migration Documentation) describes; never in source, never in tracked configuration. A reset invalidates the previous secret immediately, so plan the cut-over before resetting it.

**Before deploying, confirm the calling identity.** Every call now runs as the OAuth2 Client's **Context User** — not as the user in the replaced credential, and for an import or a job as much as for a request. If that user has different workspace permissions or item-level security, the code returns fewer rows or `Unauthorized` in production with no change to its logic, and Relativity's audit attributes every call to that one account. Confirm the Context User's permissions match those of the previous identity. Relativity's REST API authentication page describes the Context User as usually a member of the Relativity Administrators group; the OAuth2 Client block (part 3) gives NSerio's recommendation, which differs.

**When the integration acts for a signed-in user.** Client Credentials is one service identity by construction. An integration that acts on behalf of a signed-in user is the Authorization Code case, and moving to it means a new OAuth2 Client, because the flow is fixed when a client is created. Which of the two flows applies depends on who supplies the credential at run time.

Verification: **Verification note — two things.**

**Verify the destination accepts `Bearer` before changing the prefix.** Some services accept only Basic, and there the prefix change breaks working code.

**A header set once on a long-lived client cannot be refreshed.** If the token has a lifetime, set the header per request with a current token rather than on a shared client's default headers.

OAuth2 Client: See [OAuth2 Client for Client Credentials](#oauth2-client-for-client-credentials).
<!-- generated:end CS-018 client-credentials -->

#### Authorization Code

<!-- generated:begin CS-018 authorization-code -->
**Flow.** This entry is for Authorization Code. The "Which flow" paragraph of CS-018 in [Part A](part-a-csharp.md#cs-018) says when a match belongs to this flow.

Source form:
```csharp
string pair = "" + ":" + accessToken;
string encoded = Convert.ToBase64String(Encoding.ASCII.GetBytes(pair));
```

Under token authentication: **This change applies to any destination that accepts `Bearer`**, RelativityOne or not: no credential is exchanged, so only the token's provenance differs when the destination is not RelativityOne.

**Delete the encoding and change the prefix.** The token already in hand is carried verbatim:

```
Authorization: Basic <base64 of ":" + token>      ->      Authorization: Bearer <token>
```

```csharp
request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
```

The header change itself exchanges no credential; where the destination is RelativityOne, the token still comes from an OAuth2 Client, as the token-acquisition part that follows describes. This is the cheapest remediation of any other class.

Credentials: **Where the credentials come from.** The prefix change applies to any destination that accepts `Bearer`. If the destination is RelativityOne, the token that belongs in the header is obtained as the token-acquisition part that follows describes. If it is not RelativityOne, the token's provenance is that service's and does not change: the token already in hand is the one that moves to the `Bearer` prefix, issued however that service issues it.

**Common to both variants**

**Sign in.** Replace the code that collected the user's credentials with a redirect: send the user's browser to the authorize endpoint `<host>/Relativity/Identity/connect/authorize`, which leads to the RelativityOne login page. The authorize endpoint is **verified by NSerio against a live instance; not documented by Relativity**. The request carries the standard authorization-code parameters — `client_id`, `response_type=code`, the registered `redirect_uri`, a `scope`, and a `state` value the application checks on return; these are standard OAuth, and Relativity documents none of them. No discovery document is relied on.

**Receive the code.** The browser returns to the Redirect URL registered on the OAuth2 Client, with the authorization code and the `state` value. Check the `state` before using the code. The Redirect URL must be exactly the one registered (the variants say how).

**Exchange the code.** Send a form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=authorization_code`, the `code`, the same `redirect_uri`, the `client_id` and the `client_secret`, plus the `code_verifier` when PKCE is used. The code is exchanged at that endpoint, which Relativity documents for the Client Credentials grant only; the exchange is **verified by NSerio against a live instance; not documented by Relativity**. Send the `code_verifier` only when the authorize request carried a `code_challenge`; otherwise leave it out.

**.NET** (`HttpClient` and `System.Text.Json`):

```csharp
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

public sealed class AuthorizationCodeRequest
{
	public string Host { get; set; }
	public string AuthorizationCode { get; set; }
	public string RedirectUri { get; set; }
	public string ClientId { get; set; }
	public string ClientSecret { get; set; }

	// The PKCE verifier; null when no code_challenge was sent on the authorize request.
	public string CodeVerifier { get; set; }
}

public static class AuthorizationCodeExchange
{
	private const string _TOKEN_PATH = "/Relativity/Identity/connect/token";

	public static async Task<string> ExchangeAuthorizationCodeAsync(HttpClient client, AuthorizationCodeRequest request)
	{
		Dictionary<string, string> fields = new Dictionary<string, string>
		{
			{ "grant_type", "authorization_code" },
			{ "code", request.AuthorizationCode },
			{ "redirect_uri", request.RedirectUri },
			{ "client_id", request.ClientId },
			{ "client_secret", request.ClientSecret }
		};
		if (!string.IsNullOrEmpty(request.CodeVerifier))
		{
			fields["code_verifier"] = request.CodeVerifier;
		}

		string accessToken;
		using (FormUrlEncodedContent form = new FormUrlEncodedContent(fields))
		using (HttpResponseMessage response = await client.PostAsync($"{request.Host}{_TOKEN_PATH}", form))
		{
			response.EnsureSuccessStatusCode();
			using (JsonDocument document = JsonDocument.Parse(await response.Content.ReadAsStringAsync()))
			{
				accessToken = document.RootElement.GetProperty("access_token").GetString();
			}
		}

		return accessToken;
	}
}
```

**.NET Framework 4.7.2** (`System.Web.Extensions` and C# 7.3 syntax):

```csharp
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

public sealed class AuthorizationCodeRequest
{
	public string Host { get; set; }
	public string AuthorizationCode { get; set; }
	public string RedirectUri { get; set; }
	public string ClientId { get; set; }
	public string ClientSecret { get; set; }

	// The PKCE verifier; null when no code_challenge was sent on the authorize request.
	public string CodeVerifier { get; set; }
}

public static class AuthorizationCodeExchange
{
	private const string _TOKEN_PATH = "/Relativity/Identity/connect/token";

	public static async Task<string> ExchangeAuthorizationCodeAsync(HttpClient client, AuthorizationCodeRequest request)
	{
		Dictionary<string, string> fields = new Dictionary<string, string>
		{
			{ "grant_type", "authorization_code" },
			{ "code", request.AuthorizationCode },
			{ "redirect_uri", request.RedirectUri },
			{ "client_id", request.ClientId },
			{ "client_secret", request.ClientSecret }
		};
		if (!string.IsNullOrEmpty(request.CodeVerifier))
		{
			fields["code_verifier"] = request.CodeVerifier;
		}

		string accessToken;
		using (FormUrlEncodedContent form = new FormUrlEncodedContent(fields))
		using (HttpResponseMessage response = await client.PostAsync($"{request.Host}{_TOKEN_PATH}", form))
		{
			response.EnsureSuccessStatusCode();
			string body = await response.Content.ReadAsStringAsync();
			accessToken = new JavaScriptSerializer().Deserialize<Dictionary<string, object>>(body)["access_token"].ToString();
		}

		return accessToken;
	}
}
```

**Use the token.** Send `Authorization: Bearer <access token>` — the change at the use site in part 1 — with `X-CSRF-Header: -` on REST and Kepler calls.

**On expiry.** With Allow Refresh Tokens enabled on the OAuth2 Client and PKCE used, the application holds a refresh token and, when the access token expires, sends a `grant_type=refresh_token` request with it and the client credentials in place of the code exchange — *inferred* from the standard OAuth shape, not tested. The exchange example above reads only the `access_token` of the response; to renew with a refresh token, read the `refresh_token` from the same response as well and keep it as a secret, and otherwise the application sends the user through the sign-in again when the access token expires. Relativity does not document a refresh token's lifetime, whether it rotates, how it is revoked, or whether a revocation endpoint exists, nor what disabling or deleting the client does to tokens already issued. A refresh token is a secret: store it as Secret Management (Part B of the Migration Documentation) describes, and on compromise disable the client and replace it. Enable refresh tokens only if the application must keep a session beyond one token lifetime without asking the user to sign in again.

**PKCE.** PKCE is required by Relativity's own documentation to obtain a refresh token, and not otherwise: a Code request that does not ask for a refresh token works without it, **verified by NSerio against a live instance; not documented by Relativity**. It is never a blanket requirement. NSerio recommends PKCE with the `S256` method in both variants (NSerio recommendation), and does not claim that Relativity accepts any particular method, which it does not document.

**Scope.** No scope set is prescribed for Code. The scope below works — `openid RelativityRequestOrigin UserInfo UserInfoAccess AuthenticationID RelativityWeb` — **verified by NSerio against a live instance; not documented by Relativity**, verified as working and not as minimal. `state` is sent as standard OAuth practice and `nonce` is not used; Relativity does not document whether it enforces either.

**The application stops collecting the password.** After this change the application never receives, stores or forwards the user's username and password: the user signs in on RelativityOne's own page, and the application holds only tokens. Remove the credential prompt, the form field and any storage of the password that the replaced code needed.

**Single sign-on.** Sign-in happens on RelativityOne's own login page, so the instance's single sign-on and two-factor rules apply unchanged — *inferred* from where the sign-in happens, and not documented for clients that customers create.

**The client secret is required.** Code does not work without a client secret: tested and rejected, **verified by NSerio against a live instance; not documented by Relativity**, which confirms Relativity's own statement that the grant requires one. It is displayed once and is stored as Secret Management (Part B of the Migration Documentation) describes.

**Server-side variant.** For a web application whose code runs on a server:

- **Redirect.** The application receives the code at one exact HTTPS URL per environment, a route of the application, with no wildcards (NSerio recommendation; Relativity's matching rules are not documented). The Relativity administrator registers the exact URL on the OAuth2 Client.
- **Secret and tokens.** The client secret stays on the server and never reaches the browser. The user's tokens are held server-side, keyed to the session, and never in the browser.
- **Exchange.** The server performs the code exchange itself.

**Desktop variant.** For a desktop or command-line application run by a user on their own machine:

- **The system browser.** Open the authorize URL in the user's default browser and not in a web view the application hosts, so the application never sees the password (NSerio recommendation).
- **A loopback redirect on a fixed port.** Listen on the loopback address only, on the registered port, for one request. The Redirect URL is `http://127.0.0.1:<port>/<path>/`, ending with a slash because the listener below registers it as its prefix: a plain-HTTP loopback URL is accepted, **verified by NSerio against a live instance; not documented by Relativity**. The Relativity administrator registers that exact URL; because it is registered in advance, the port is fixed and not chosen at runtime.

```csharp
using System;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Net;
using System.Threading.Tasks;

public static class LoopbackAuthorizationReceiver
{
	private const string _CODE_PARAMETER = "code";
	private const string _STATE_PARAMETER = "state";
	private const string _ERROR_PARAMETER = "error";
	private const string _ERROR_DESCRIPTION_PARAMETER = "error_description";
	private const string _STATE_MISMATCH_MESSAGE = "The returned state is not the one that was sent; the response is rejected.";
	private const string _MISSING_CODE_MESSAGE = "The response carries no authorization code.";

	// Waits for the one request the browser sends to the registered loopback Redirect URL, and returns its code
	// only when its state is the one that was sent on the authorize request and the response carries no error.
	// redirectUrl is the exact URL registered on the OAuth2 Client, for example http://127.0.0.1:<port>/<path>/ :
	// it ends with a slash, as a listener prefix must.
	public static async Task<string> ReceiveAuthorizationCodeAsync(string authorizeUrl, string redirectUrl, string expectedState)
	{
		string authorizationCode;
		using (HttpListener listener = new HttpListener())
		{
			listener.Prefixes.Add(redirectUrl);
			listener.Start();
			Process.Start(new ProcessStartInfo(authorizeUrl) { UseShellExecute = true })?.Dispose();
			HttpListenerContext context = await listener.GetContextAsync();
			NameValueCollection query = context.Request.QueryString;
			context.Response.Close();
			if (query[_STATE_PARAMETER] != expectedState)
			{
				throw new InvalidOperationException(_STATE_MISMATCH_MESSAGE);
			}

			if (!string.IsNullOrEmpty(query[_ERROR_PARAMETER]))
			{
				throw new InvalidOperationException($"The authorization server returned an error: {query[_ERROR_PARAMETER]} {query[_ERROR_DESCRIPTION_PARAMETER]}".TrimEnd());
			}

			authorizationCode = query[_CODE_PARAMETER];
			if (string.IsNullOrEmpty(authorizationCode))
			{
				throw new InvalidOperationException(_MISSING_CODE_MESSAGE);
			}
		}

		return authorizationCode;
	}
}
```

- **The secret is extractable.** Code requires a client secret (above), so a distributed desktop application holds one, and **a secret shipped inside a distributed binary can be extracted by anyone who holds the binary**. It protects nothing against the person running the application.
- **Compensating guidance** (NSerio recommendation, subject to your own security policy): set Trusted IPs where the users' outbound addresses are known and stable — they restrict where a token can be *requested*, and are checked when a token is issued against the client's ID and secret; use the recommended short Access Token Lifetime of 60 minutes; enable refresh tokens only when needed; and create one OAuth2 Client per distribution, so a leaked secret can be disabled without affecting the others.
- **A documentation gap.** Relativity documents no recommendation for a distributed desktop application; this design is what NSerio verified works.

Verification: **Verification note — two things.**

**Verify the destination accepts `Bearer` before changing the prefix.** Some services accept only Basic, and there the prefix change breaks working code.

**A header set once on a long-lived client cannot be refreshed.** If the token has a lifetime, set the header per request with a current token rather than on a shared client's default headers.

OAuth2 Client: See [OAuth2 Client for Authorization Code](#oauth2-client-for-authorization-code).
<!-- generated:end CS-018 authorization-code -->

### CS-020 · SdkProxyFactoryWithUserAndPassword

<a id="cs-020-migration"></a>

#### Client Credentials

<!-- generated:begin CS-020 client-credentials -->
**Flow.** This entry is for Client Credentials. The "Which flow" paragraph of CS-020 in [Part A](part-a-csharp.md#cs-020) says when a match belongs to this flow.

Source form:
```csharp
var proxy = helper.GetProxy<IExampleService>(userName, password);
```

Under token authentication: **First, the destination:** if this Relativity instance is not RelativityOne — including a Relativity Server instance, whose own documentation governs — no migration to RelativityOne applies and the rest of this entry does not apply.

Replace the call that passes a user name and a password with **the helper's token-bearing equivalent**, given the access token, and remove the two credential arguments. This guidance does not name that overload or member, because which one exists depends on the version of the helper or SDK in use. **Confirm the version in use offers one; where it does not, this is an upgrade before it is a code change.**

Credentials: **Where the credentials come from.** The call shape establishes that a user name and a password are given to a factory, not that the factory connects to RelativityOne; if it is not RelativityOne — including a Relativity Server instance, whose own documentation governs — the migration is that service's own token mechanism, and no migration to RelativityOne applies. If it is RelativityOne, the token comes from the token-acquisition part that follows.

**Where the token comes from.** Create an OAuth2 Client with Flow Grant Type set to Client Credentials (part 3 lists its settings), then send a form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=client_credentials`, `client_id`, `client_secret` and a `scope`. The token endpoint is **documented** for this grant. The token is then sent as `Authorization: Bearer <token>`, with `X-CSRF-Header: -` on REST and Kepler calls — the header is documented as required, and a Kepler call without it returns 404.

**.NET** (`HttpClient` and `System.Text.Json`). The class requests the token with the client ID and the client secret, caches it, requests a new one 60 seconds before it expires, and, when a call is rejected for authentication, requests a new one and sends the call once more. Build one instance for the life of the process and send every call through `SendAsync`; the request is created by the delegate on each attempt, because a request message cannot be sent twice. The client ID and the client secret come from the process environment or a secret store, never from source. Where the code uses the Relativity SDK, `GetAccessTokenAsync(false)` gives the token for `BearerTokenCredentials` each time a `ServiceFactory` is built.

```csharp
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

public sealed class AccessTokenProvider : IDisposable
{
	private const string _TOKEN_PATH = "/Relativity/Identity/connect/token";
	private const string _BEARER_SCHEME = "Bearer";
	private const string _CSRF_HEADER_NAME = "X-CSRF-Header";
	private const string _CSRF_HEADER_VALUE = "-";
	private static readonly TimeSpan _renewalMargin = TimeSpan.FromSeconds(60);

	private readonly HttpClient _client = new HttpClient();
	private readonly SemaphoreSlim _gate = new SemaphoreSlim(1, 1);
	private readonly string _host;
	private readonly string _clientId;
	private readonly string _clientSecret;
	private readonly string _scope;
	private string _accessToken;
	private DateTimeOffset _expiresAt;

	public AccessTokenProvider(string host, string clientId, string clientSecret, string scope)
	{
		_host = host;
		_clientId = clientId;
		_clientSecret = clientSecret;
		_scope = scope;
	}

	// Cached token; renewed shortly before it expires, or on demand after a rejected call.
	public async Task<string> GetAccessTokenAsync(bool forceRenewal)
	{
		await _gate.WaitAsync();
		try
		{
			if (forceRenewal || _accessToken == null || DateTimeOffset.UtcNow >= _expiresAt - _renewalMargin)
			{
				await AcquireAccessTokenAsync();
			}

			return _accessToken;
		}
		finally
		{
			_gate.Release();
		}
	}

	// Sends the request with the bearer token; when the call is rejected for authentication, renews the token and sends it once more.
	public async Task<HttpResponseMessage> SendAsync(Func<HttpRequestMessage> createRequest)
	{
		HttpResponseMessage response = await SendOnceAsync(createRequest, false);
		if (response.StatusCode == HttpStatusCode.Unauthorized)
		{
			response.Dispose();
			response = await SendOnceAsync(createRequest, true);
		}

		return response;
	}

	public void Dispose()
	{
		_client.Dispose();
		_gate.Dispose();
	}

	private async Task AcquireAccessTokenAsync()
	{
		using (FormUrlEncodedContent form = new FormUrlEncodedContent(new Dictionary<string, string>
		{
			{ "grant_type", "client_credentials" },
			{ "client_id", _clientId },
			{ "client_secret", _clientSecret },
			{ "scope", _scope }
		}))
		using (HttpResponseMessage response = await _client.PostAsync($"{_host}{_TOKEN_PATH}", form))
		{
			response.EnsureSuccessStatusCode();
			using (JsonDocument document = JsonDocument.Parse(await response.Content.ReadAsStringAsync()))
			{
				_accessToken = document.RootElement.GetProperty("access_token").GetString();
				_expiresAt = DateTimeOffset.UtcNow.AddSeconds(document.RootElement.GetProperty("expires_in").GetInt32());
			}
		}
	}

	private async Task<HttpResponseMessage> SendOnceAsync(Func<HttpRequestMessage> createRequest, bool forceRenewal)
	{
		string token = await GetAccessTokenAsync(forceRenewal);
		HttpResponseMessage response;
		using (HttpRequestMessage request = createRequest())
		{
			request.Headers.Authorization = new AuthenticationHeaderValue(_BEARER_SCHEME, token);
			request.Headers.Add(_CSRF_HEADER_NAME, _CSRF_HEADER_VALUE);
			response = await _client.SendAsync(request);
		}

		return response;
	}
}
```

**.NET Framework 4.7.2**: the same class with the serializer the runtime ships (`System.Web.Extensions`, referenced together with `System.Net.Http`), and C# 7.3 syntax:

```csharp
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

public sealed class AccessTokenProvider : IDisposable
{
	private const string _TOKEN_PATH = "/Relativity/Identity/connect/token";
	private const string _BEARER_SCHEME = "Bearer";
	private const string _CSRF_HEADER_NAME = "X-CSRF-Header";
	private const string _CSRF_HEADER_VALUE = "-";
	private static readonly TimeSpan _renewalMargin = TimeSpan.FromSeconds(60);

	private readonly HttpClient _client = new HttpClient();
	private readonly SemaphoreSlim _gate = new SemaphoreSlim(1, 1);
	private readonly string _host;
	private readonly string _clientId;
	private readonly string _clientSecret;
	private readonly string _scope;
	private string _accessToken;
	private DateTimeOffset _expiresAt;

	public AccessTokenProvider(string host, string clientId, string clientSecret, string scope)
	{
		_host = host;
		_clientId = clientId;
		_clientSecret = clientSecret;
		_scope = scope;
	}

	// Cached token; renewed shortly before it expires, or on demand after a rejected call.
	public async Task<string> GetAccessTokenAsync(bool forceRenewal)
	{
		await _gate.WaitAsync();
		try
		{
			if (forceRenewal || _accessToken == null || DateTimeOffset.UtcNow >= _expiresAt - _renewalMargin)
			{
				await AcquireAccessTokenAsync();
			}

			return _accessToken;
		}
		finally
		{
			_gate.Release();
		}
	}

	// Sends the request with the bearer token; when the call is rejected for authentication, renews the token and sends it once more.
	public async Task<HttpResponseMessage> SendAsync(Func<HttpRequestMessage> createRequest)
	{
		HttpResponseMessage response = await SendOnceAsync(createRequest, false);
		if (response.StatusCode == HttpStatusCode.Unauthorized)
		{
			response.Dispose();
			response = await SendOnceAsync(createRequest, true);
		}

		return response;
	}

	public void Dispose()
	{
		_client.Dispose();
		_gate.Dispose();
	}

	private async Task AcquireAccessTokenAsync()
	{
		using (FormUrlEncodedContent form = new FormUrlEncodedContent(new Dictionary<string, string>
		{
			{ "grant_type", "client_credentials" },
			{ "client_id", _clientId },
			{ "client_secret", _clientSecret },
			{ "scope", _scope }
		}))
		using (HttpResponseMessage response = await _client.PostAsync($"{_host}{_TOKEN_PATH}", form))
		{
			response.EnsureSuccessStatusCode();
			string body = await response.Content.ReadAsStringAsync();
			Dictionary<string, object> fields = new JavaScriptSerializer().Deserialize<Dictionary<string, object>>(body);
			_accessToken = fields["access_token"].ToString();
			_expiresAt = DateTimeOffset.UtcNow.AddSeconds(Convert.ToInt32(fields["expires_in"]));
		}
	}

	private async Task<HttpResponseMessage> SendOnceAsync(Func<HttpRequestMessage> createRequest, bool forceRenewal)
	{
		string token = await GetAccessTokenAsync(forceRenewal);
		HttpResponseMessage response;
		using (HttpRequestMessage request = createRequest())
		{
			request.Headers.Authorization = new AuthenticationHeaderValue(_BEARER_SCHEME, token);
			request.Headers.Add(_CSRF_HEADER_NAME, _CSRF_HEADER_VALUE);
			response = await _client.SendAsync(request);
		}

		return response;
	}
}
```

**The scope depends on what the integration needs, and no single scope is required.** Relativity's documentation shows two values for this grant: `SystemUserInfo` on the REST authentication page, the Usage Reports page and in the official Transfer SDK sample, and `UserInfoAccess` in the Kepler .NET samples. No page says which is correct, whether both work, or what either grants, and the OAuth2 Client's `Scopes` property is documented as one that cannot be set. Use the value the API you call documents, and confirm it.

**Cache the token, and re-acquire it on expiry — do not refresh.** Keep the token for its lifetime (part 3 recommends 60 minutes, Relativity's own wording) and request a new one with the client secret shortly before it expires (the JavaScript, TypeScript and Python examples use 60 seconds), and once when a call is rejected for authentication. Client Credentials does not offer refresh tokens: **verified by NSerio against a live instance; not documented by Relativity** — the Allow Refresh Tokens control does not appear for this grant in the RelativityOne user interface. Relativity's own tables and text on refresh tokens conflict with each other, so re-acquisition with the secret is the advice that holds whichever way they are read.

**Long-running processes that use the Relativity SDK.** A `ServiceFactory` built once from a bearer token keeps that token after it expires. Rebuild the `ServiceFactory` and its proxies with a fresh token before expiry. This is the only advice consistent with the documentation: the package `Relativity.Kepler.Client.SDK` (documented) exposes a settable `Token` and an `HttpMessageHandler` overload but no token-provider overload, and no page documents renewing a token behind a factory.

**The client secret.** It is displayed once, at creation or reset. Store it as Secret Management (Part B of the Migration Documentation) describes; never in source, never in tracked configuration. A reset invalidates the previous secret immediately, so plan the cut-over before resetting it.

**Before deploying, confirm the calling identity.** Every call now runs as the OAuth2 Client's **Context User** — not as the user in the replaced credential, and for an import or a job as much as for a request. If that user has different workspace permissions or item-level security, the code returns fewer rows or `Unauthorized` in production with no change to its logic, and Relativity's audit attributes every call to that one account. Confirm the Context User's permissions match those of the previous identity. Relativity's REST API authentication page describes the Context User as usually a member of the Relativity Administrators group; the OAuth2 Client block (part 3) gives NSerio's recommendation, which differs.

**When the integration acts for a signed-in user.** Client Credentials is one service identity by construction. An integration that acts on behalf of a signed-in user is the Authorization Code case, and moving to it means a new OAuth2 Client, because the flow is fixed when a client is created. Which of the two flows applies depends on who supplies the credential at run time.

Verification: **Verification note — two things. The first decides whether the change can be made at all.**

**Helper or SDK version support comes first.** Where the version in use has no token-bearing equivalent of the call, the change cannot be made at this call site at all, and the upgrade carries its own verification.

**A token has a finite lifetime, and the method is read by its name.** A proxy obtained once and reused across a long-running process or test run holds a token that expires; obtain it with a current token for each run. Also confirm the call is the Relativity helper: a method of another library with the same name and two arguments is read the same way.

OAuth2 Client: See [OAuth2 Client for Client Credentials](#oauth2-client-for-client-credentials).
<!-- generated:end CS-020 client-credentials -->

#### Authorization Code

<!-- generated:begin CS-020 authorization-code -->
**Flow.** This entry is for Authorization Code. The "Which flow" paragraph of CS-020 in [Part A](part-a-csharp.md#cs-020) says when a match belongs to this flow.

Source form:
```csharp
var proxy = helper.GetProxy<IExampleService>(userName, password);
```

Under token authentication: **First, the destination:** if this Relativity instance is not RelativityOne — including a Relativity Server instance, whose own documentation governs — no migration to RelativityOne applies and the rest of this entry does not apply.

Replace the call that passes a user name and a password with **the helper's token-bearing equivalent**, given the access token, and remove the two credential arguments. This guidance does not name that overload or member, because which one exists depends on the version of the helper or SDK in use. **Confirm the version in use offers one; where it does not, this is an upgrade before it is a code change.**

Credentials: **Where the credentials come from.** The call shape establishes that a user name and a password are given to a factory, not that the factory connects to RelativityOne; if it is not RelativityOne — including a Relativity Server instance, whose own documentation governs — the migration is that service's own token mechanism, and no migration to RelativityOne applies. If it is RelativityOne, the token comes from the token-acquisition part that follows.

**Common to both variants**

**Sign in.** Replace the code that collected the user's credentials with a redirect: send the user's browser to the authorize endpoint `<host>/Relativity/Identity/connect/authorize`, which leads to the RelativityOne login page. The authorize endpoint is **verified by NSerio against a live instance; not documented by Relativity**. The request carries the standard authorization-code parameters — `client_id`, `response_type=code`, the registered `redirect_uri`, a `scope`, and a `state` value the application checks on return; these are standard OAuth, and Relativity documents none of them. No discovery document is relied on.

**Receive the code.** The browser returns to the Redirect URL registered on the OAuth2 Client, with the authorization code and the `state` value. Check the `state` before using the code. The Redirect URL must be exactly the one registered (the variants say how).

**Exchange the code.** Send a form-encoded `POST` to `<host>/Relativity/Identity/connect/token` with `grant_type=authorization_code`, the `code`, the same `redirect_uri`, the `client_id` and the `client_secret`, plus the `code_verifier` when PKCE is used. The code is exchanged at that endpoint, which Relativity documents for the Client Credentials grant only; the exchange is **verified by NSerio against a live instance; not documented by Relativity**. Send the `code_verifier` only when the authorize request carried a `code_challenge`; otherwise leave it out.

**.NET** (`HttpClient` and `System.Text.Json`):

```csharp
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

public sealed class AuthorizationCodeRequest
{
	public string Host { get; set; }
	public string AuthorizationCode { get; set; }
	public string RedirectUri { get; set; }
	public string ClientId { get; set; }
	public string ClientSecret { get; set; }

	// The PKCE verifier; null when no code_challenge was sent on the authorize request.
	public string CodeVerifier { get; set; }
}

public static class AuthorizationCodeExchange
{
	private const string _TOKEN_PATH = "/Relativity/Identity/connect/token";

	public static async Task<string> ExchangeAuthorizationCodeAsync(HttpClient client, AuthorizationCodeRequest request)
	{
		Dictionary<string, string> fields = new Dictionary<string, string>
		{
			{ "grant_type", "authorization_code" },
			{ "code", request.AuthorizationCode },
			{ "redirect_uri", request.RedirectUri },
			{ "client_id", request.ClientId },
			{ "client_secret", request.ClientSecret }
		};
		if (!string.IsNullOrEmpty(request.CodeVerifier))
		{
			fields["code_verifier"] = request.CodeVerifier;
		}

		string accessToken;
		using (FormUrlEncodedContent form = new FormUrlEncodedContent(fields))
		using (HttpResponseMessage response = await client.PostAsync($"{request.Host}{_TOKEN_PATH}", form))
		{
			response.EnsureSuccessStatusCode();
			using (JsonDocument document = JsonDocument.Parse(await response.Content.ReadAsStringAsync()))
			{
				accessToken = document.RootElement.GetProperty("access_token").GetString();
			}
		}

		return accessToken;
	}
}
```

**.NET Framework 4.7.2** (`System.Web.Extensions` and C# 7.3 syntax):

```csharp
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

public sealed class AuthorizationCodeRequest
{
	public string Host { get; set; }
	public string AuthorizationCode { get; set; }
	public string RedirectUri { get; set; }
	public string ClientId { get; set; }
	public string ClientSecret { get; set; }

	// The PKCE verifier; null when no code_challenge was sent on the authorize request.
	public string CodeVerifier { get; set; }
}

public static class AuthorizationCodeExchange
{
	private const string _TOKEN_PATH = "/Relativity/Identity/connect/token";

	public static async Task<string> ExchangeAuthorizationCodeAsync(HttpClient client, AuthorizationCodeRequest request)
	{
		Dictionary<string, string> fields = new Dictionary<string, string>
		{
			{ "grant_type", "authorization_code" },
			{ "code", request.AuthorizationCode },
			{ "redirect_uri", request.RedirectUri },
			{ "client_id", request.ClientId },
			{ "client_secret", request.ClientSecret }
		};
		if (!string.IsNullOrEmpty(request.CodeVerifier))
		{
			fields["code_verifier"] = request.CodeVerifier;
		}

		string accessToken;
		using (FormUrlEncodedContent form = new FormUrlEncodedContent(fields))
		using (HttpResponseMessage response = await client.PostAsync($"{request.Host}{_TOKEN_PATH}", form))
		{
			response.EnsureSuccessStatusCode();
			string body = await response.Content.ReadAsStringAsync();
			accessToken = new JavaScriptSerializer().Deserialize<Dictionary<string, object>>(body)["access_token"].ToString();
		}

		return accessToken;
	}
}
```

**Use the token.** Send `Authorization: Bearer <access token>` — the change at the use site in part 1 — with `X-CSRF-Header: -` on REST and Kepler calls.

**On expiry.** With Allow Refresh Tokens enabled on the OAuth2 Client and PKCE used, the application holds a refresh token and, when the access token expires, sends a `grant_type=refresh_token` request with it and the client credentials in place of the code exchange — *inferred* from the standard OAuth shape, not tested. The exchange example above reads only the `access_token` of the response; to renew with a refresh token, read the `refresh_token` from the same response as well and keep it as a secret, and otherwise the application sends the user through the sign-in again when the access token expires. Relativity does not document a refresh token's lifetime, whether it rotates, how it is revoked, or whether a revocation endpoint exists, nor what disabling or deleting the client does to tokens already issued. A refresh token is a secret: store it as Secret Management (Part B of the Migration Documentation) describes, and on compromise disable the client and replace it. Enable refresh tokens only if the application must keep a session beyond one token lifetime without asking the user to sign in again.

**PKCE.** PKCE is required by Relativity's own documentation to obtain a refresh token, and not otherwise: a Code request that does not ask for a refresh token works without it, **verified by NSerio against a live instance; not documented by Relativity**. It is never a blanket requirement. NSerio recommends PKCE with the `S256` method in both variants (NSerio recommendation), and does not claim that Relativity accepts any particular method, which it does not document.

**Scope.** No scope set is prescribed for Code. The scope below works — `openid RelativityRequestOrigin UserInfo UserInfoAccess AuthenticationID RelativityWeb` — **verified by NSerio against a live instance; not documented by Relativity**, verified as working and not as minimal. `state` is sent as standard OAuth practice and `nonce` is not used; Relativity does not document whether it enforces either.

**The application stops collecting the password.** After this change the application never receives, stores or forwards the user's username and password: the user signs in on RelativityOne's own page, and the application holds only tokens. Remove the credential prompt, the form field and any storage of the password that the replaced code needed.

**Single sign-on.** Sign-in happens on RelativityOne's own login page, so the instance's single sign-on and two-factor rules apply unchanged — *inferred* from where the sign-in happens, and not documented for clients that customers create.

**The client secret is required.** Code does not work without a client secret: tested and rejected, **verified by NSerio against a live instance; not documented by Relativity**, which confirms Relativity's own statement that the grant requires one. It is displayed once and is stored as Secret Management (Part B of the Migration Documentation) describes.

**Server-side variant.** For a web application whose code runs on a server:

- **Redirect.** The application receives the code at one exact HTTPS URL per environment, a route of the application, with no wildcards (NSerio recommendation; Relativity's matching rules are not documented). The Relativity administrator registers the exact URL on the OAuth2 Client.
- **Secret and tokens.** The client secret stays on the server and never reaches the browser. The user's tokens are held server-side, keyed to the session, and never in the browser.
- **Exchange.** The server performs the code exchange itself.

**Desktop variant.** For a desktop or command-line application run by a user on their own machine:

- **The system browser.** Open the authorize URL in the user's default browser and not in a web view the application hosts, so the application never sees the password (NSerio recommendation).
- **A loopback redirect on a fixed port.** Listen on the loopback address only, on the registered port, for one request. The Redirect URL is `http://127.0.0.1:<port>/<path>/`, ending with a slash because the listener below registers it as its prefix: a plain-HTTP loopback URL is accepted, **verified by NSerio against a live instance; not documented by Relativity**. The Relativity administrator registers that exact URL; because it is registered in advance, the port is fixed and not chosen at runtime.

```csharp
using System;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Net;
using System.Threading.Tasks;

public static class LoopbackAuthorizationReceiver
{
	private const string _CODE_PARAMETER = "code";
	private const string _STATE_PARAMETER = "state";
	private const string _ERROR_PARAMETER = "error";
	private const string _ERROR_DESCRIPTION_PARAMETER = "error_description";
	private const string _STATE_MISMATCH_MESSAGE = "The returned state is not the one that was sent; the response is rejected.";
	private const string _MISSING_CODE_MESSAGE = "The response carries no authorization code.";

	// Waits for the one request the browser sends to the registered loopback Redirect URL, and returns its code
	// only when its state is the one that was sent on the authorize request and the response carries no error.
	// redirectUrl is the exact URL registered on the OAuth2 Client, for example http://127.0.0.1:<port>/<path>/ :
	// it ends with a slash, as a listener prefix must.
	public static async Task<string> ReceiveAuthorizationCodeAsync(string authorizeUrl, string redirectUrl, string expectedState)
	{
		string authorizationCode;
		using (HttpListener listener = new HttpListener())
		{
			listener.Prefixes.Add(redirectUrl);
			listener.Start();
			Process.Start(new ProcessStartInfo(authorizeUrl) { UseShellExecute = true })?.Dispose();
			HttpListenerContext context = await listener.GetContextAsync();
			NameValueCollection query = context.Request.QueryString;
			context.Response.Close();
			if (query[_STATE_PARAMETER] != expectedState)
			{
				throw new InvalidOperationException(_STATE_MISMATCH_MESSAGE);
			}

			if (!string.IsNullOrEmpty(query[_ERROR_PARAMETER]))
			{
				throw new InvalidOperationException($"The authorization server returned an error: {query[_ERROR_PARAMETER]} {query[_ERROR_DESCRIPTION_PARAMETER]}".TrimEnd());
			}

			authorizationCode = query[_CODE_PARAMETER];
			if (string.IsNullOrEmpty(authorizationCode))
			{
				throw new InvalidOperationException(_MISSING_CODE_MESSAGE);
			}
		}

		return authorizationCode;
	}
}
```

- **The secret is extractable.** Code requires a client secret (above), so a distributed desktop application holds one, and **a secret shipped inside a distributed binary can be extracted by anyone who holds the binary**. It protects nothing against the person running the application.
- **Compensating guidance** (NSerio recommendation, subject to your own security policy): set Trusted IPs where the users' outbound addresses are known and stable — they restrict where a token can be *requested*, and are checked when a token is issued against the client's ID and secret; use the recommended short Access Token Lifetime of 60 minutes; enable refresh tokens only when needed; and create one OAuth2 Client per distribution, so a leaked secret can be disabled without affecting the others.
- **A documentation gap.** Relativity documents no recommendation for a distributed desktop application; this design is what NSerio verified works.

Verification: **Verification note — two things. The first decides whether the change can be made at all.**

**Helper or SDK version support comes first.** Where the version in use has no token-bearing equivalent of the call, the change cannot be made at this call site at all, and the upgrade carries its own verification.

**A token has a finite lifetime, and the method is read by its name.** A proxy obtained once and reused across a long-running process or test run holds a token that expires; obtain it with a current token for each run. Also confirm the call is the Relativity helper: a method of another library with the same name and two arguments is read the same way.

OAuth2 Client: See [OAuth2 Client for Authorization Code](#oauth2-client-for-authorization-code).
<!-- generated:end CS-020 authorization-code -->


## Generic suggestion

The generic recommendation applies when a match has High or Medium confidence but no pattern-specific entry fits it. It is shorter than a class entry: it does not show a before-and-after form, because the construct was not characterized. Use it as a starting point, and read the Part A description of the pattern class if you can identify the construct.

### Client Credentials

<!-- generated:begin generic client-credentials -->
**Flow.** This entry is for Client Credentials.

Basic Authentication is found at this location, but the construct is not one that this text describes, so the change has not been characterized and no before-and-after form is offered here. If the destination is not RelativityOne, Relativity's identity endpoint does not issue tokens for it: the migration is that service's own, and no migration to RelativityOne applies. If the destination is RelativityOne, Basic Authentication is no longer documented for its REST API. The documented destination in C# is either the Relativity SDK's bearer-token credential type, where the code uses the SDK, or a bearer `Authorization` header, where it builds requests itself; either carries a token obtained from the platform's identity endpoint with the client-credentials grant. The client ID and secret that obtain that token belong in the process environment or a secret store, per Secret Management (Part B of the Migration Documentation), and never in source or tracked configuration. Beyond those facts this is a pointer and not an instruction: the migration must be designed from the code by whoever owns it, rather than applied from this text.

OAuth2 Client: See [OAuth2 Client for Client Credentials](#oauth2-client-for-client-credentials).
<!-- generated:end generic client-credentials -->

### Authorization Code

<!-- generated:begin generic authorization-code -->
**Flow.** This entry is for Authorization Code.

Basic Authentication is found at this location, but the construct is not one that this text describes, so the change has not been characterized and no before-and-after form is offered here. If the destination is not RelativityOne, Relativity's identity endpoint does not issue tokens for it: the migration is that service's own, and no migration to RelativityOne applies. If the destination is RelativityOne, Basic Authentication is no longer documented for its REST API, and this match is assigned the Authorization Code flow, in which the application sends the user to RelativityOne to sign in, receives an authorization code at a Redirect URL registered on its OAuth2 Client, and exchanges the code for an access token that it sends as a Bearer token. The authorize endpoint is `<host>/Relativity/Identity/connect/authorize` and the code is exchanged at `<host>/Relativity/Identity/connect/token`; both are verified by NSerio against a live instance; not documented by Relativity. PKCE is required by Relativity's own documentation to obtain a refresh token, and not otherwise. The client ID and secret belong in the process environment or a secret store, per Secret Management (Part B of the Migration Documentation), and never in source or tracked configuration; Code requires a client secret. After the change the application no longer collects or forwards the user's password. Beyond those facts this is a pointer and not an instruction: the migration must be designed from the code by whoever owns it, rather than applied from this text.

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
