# Token expiry (User Guide, section 7 of 8)

An access token stops working after a fixed time. This section says what controls that time, what happens when a token expires, how each flow handles it, and how code should react to an expired or rejected token.

## What the Access Token Lifetime controls

The **Access Token Lifetime** field of the OAuth2 Client sets how long each access token issued to that client stays valid. The field is expressed in minutes. The recommended setting is the same for both flows:

> Access Token Lifetime: 60 minutes, stated in minutes because the field is in minutes. Relativity documentation: tokens for this flow must have a short lifetime, and the recommended value matches the Identity Server default.

Relativity recommends 60 minutes for Client Credentials and for Code clients ([OAuth2 clients](https://help.relativity.com/RelativityOne/Content/Relativity/Authentication/OAuth2_clients.htm)). Use it unless your own policy says otherwise. The tool does not claim that the field is required; the form enforces what it enforces. A short lifetime limits how long a stolen token is useful, and it means every integration must handle expiry, because the Basic credential it replaces never expired.

## What happens when a token expires

A call made with an expired token is rejected for authentication. A token that was valid an hour ago is therefore not a token you can keep in a variable for the life of a process. This is the change that most often breaks working code during the migration: a `ServiceFactory` built once from a bearer token keeps that token after it expires, and the calls made through it then fail.

## How each flow handles expiry

**Client Credentials: cache the token, and request a new one.** Keep the token for its lifetime. When it expires, or when a call is rejected for authentication, request a new token from the token endpoint with the client ID and the client secret. Client Credentials does not offer refresh tokens: this is **verified by NSerio against a live instance; not documented by Relativity**, because the Allow Refresh Tokens control does not appear for this grant in the RelativityOne user interface. Relativity's own tables and text on refresh tokens conflict with each other, so re-acquiring a token with the secret is the advice that holds whichever way they are read. Where the application uses the Relativity SDK and runs for a long time, rebuild the `ServiceFactory` and its proxies with a fresh token before the old one expires; Relativity's SDK exposes a settable token but documents no way to renew a token behind a factory.

**Authorization Code: use the refresh token, or sign in again.** If Allow Refresh Tokens is enabled on the client and the application used PKCE, it holds a refresh token, and when the access token expires it sends a `grant_type=refresh_token` request with it and the client credentials in place of the code exchange. That flow follows the standard OAuth shape; it is inferred and has not been tested against RelativityOne. If there is no refresh token, the user signs in again. Relativity documents PKCE as required to obtain a refresh token. Relativity does not document a refresh token's lifetime, whether it rotates, how it is revoked, or whether a revocation endpoint exists. Enable refresh tokens only if the application must keep a session beyond one token lifetime without asking the user to sign in again, and store a refresh token as a secret; see [Secrets](06-secrets.md).

## Handling an expired or rejected token in code

- **Do not assume a token is good.** Record when you obtained it, and obtain a new one shortly before the lifetime ends, not after a failure.
- **Treat an authentication failure as a signal to obtain a new token once**, then repeat the call once. If the call fails again, stop and surface the error: repeating without limit locks out or floods the identity service.
- **Send the token per request** from one place that owns it, and do not copy it into long-lived clients or default headers that cannot be refreshed.
- **Keep the token out of logs and error messages.**
- **In an Authorization Code application, decide in advance what a user sees** when the refresh token is missing, expired or rejected: send the user through sign-in again.

Part C gives the same guidance, with the requests, for each pattern class and each flow: [Part C for C#](../migration/part-c-csharp.md), [Part C for PowerShell](../migration/part-c-powershell.md), [Part C for VB.NET](../migration/part-c-vbnet.md), [Part C for Shell](../migration/part-c-shell.md), [Part C for JavaScript](../migration/part-c-javascript.md) and [Part C for TypeScript](../migration/part-c-typescript.md).
