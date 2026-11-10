# Configuring the OAuth2 Client (User Guide, section 5 of 8)

Before an application can obtain a token, an OAuth2 Client has to exist in RelativityOne. This section lists the fields of the OAuth2 Client for each flow, with NSerio's recommended settings. It is the same text the tool prints under **OAuth2 Client** in every High or Medium recommendation.

**The tool never creates or changes an OAuth2 Client.** It never connects to RelativityOne. Creating or changing the client is a manual step, done by a RelativityOne administrator, and everything below is guidance.

## Where to create it

In RelativityOne, open the OAuth2 Client tab and create a client. NSerio's observation is that the menu path to it is **Home > Authentication > OAuth2 Client Manager**, as Relativity's [REST API authentication](https://platform.relativity.com/RelativityOne/Content/REST_API/REST_API_authentication.htm) page names it; the OAuth2 clients page below describes the form and does not give that path. The **Flow Grant Type** chooses the flow: **Client Credentials** for [Client Credentials](04-oauth2-essentials.md#the-two-flows-the-tool-recommends), and **Code** for Authorization Code. The flow of a client is fixed when it is created and cannot be changed when you edit it, so moving an integration from one flow to the other means creating a new client. Relativity describes the form on its [OAuth2 clients](https://help.relativity.com/RelativityOne/Content/Relativity/Authentication/OAuth2_clients.htm) page.

When you create the client, RelativityOne generates the client ID and the client secret. **The secret is shown once**, when the client is created or when the secret is reset. Store it as [Secret Management](../migration/part-b-secret-management.md) describes, and see [Secrets](06-secrets.md).

## How each statement is labelled

Every claim in the field lists carries its source, and the three sources are never blended:

- **Relativity documentation**: a fact that Relativity's own documentation states.
- **verified by NSerio against a live instance; not documented by Relativity**: a fact that NSerio checked against a running RelativityOne instance and that Relativity does not document.
- **NSerio's recommendation**: a choice that is guidance and not a fact. You can depart from it under your own security policy.

The fields that Relativity's form enforces are enforced by the form. The lists below describe settings and recommend values; they do not claim that a field is mandatory.

## Client Credentials

Use this client for an integration that runs as itself, with no user present.

<!-- generated:begin oauth2-client client-credentials -->
Create or confirm this OAuth2 Client in RelativityOne (NSerio's observation: the menu path is Home > Authentication > OAuth2 Client Manager, as Relativity's REST API authentication page names it). This tool never connects to RelativityOne and never creates or changes the OAuth2 Client; the settings below are guidance.
- Name: a descriptive name identifying the integration it serves. Use one OAuth2 Client per integration and do not share it across integrations.
- Flow Grant Type: Client Credentials
- Context User: NSerio's recommendation: a dedicated integration user with the minimum permissions the integration needs, and not a personal account and not an administrator, because every call runs with that user's permissions and an administrator Context User gives the integration an administrator's reach. Relativity documentation (REST API authentication page): describes the Context User as usually a member of the Relativity Administrators group, though it could differ depending on the API or operation invoked; NSerio recommends against that default for an integration.
- Access Token Lifetime: 60 minutes, stated in minutes because the field is in minutes. Relativity documentation: tokens for this flow must have a short lifetime, and the recommended value matches the Identity Server default.
- Trusted IPs: Relativity documentation: the address list is checked whenever a token is requested with the OAuth2 Client's ID and secret. NSerio's recommendation: populate it with the integration's outbound IP addresses, one per line, wherever they are known and stable. It restricts where a token can be requested, checked when the token is issued; it does not restrict a token already issued, which travels in the request header and is valid from anywhere until it expires.
- Allow refresh tokens: not used by this flow. The integration requests a new access token when the current one expires.
- Enabled: on.
- Every call made with this OAuth2 Client runs with the Context User's permissions.
- Relativity documentation: the flow is fixed when the OAuth2 Client is created and cannot be changed when editing it, so moving this integration to Authorization Code later means creating a new OAuth2 Client.
<!-- generated:end oauth2-client client-credentials -->

## Authorization Code

Use this client for an application that acts for a signed-in user. RelativityOne names the flow **Code**.

<!-- generated:begin oauth2-client authorization-code -->
Create or confirm this OAuth2 Client in RelativityOne (NSerio's observation: the menu path is Home > Authentication > OAuth2 Client Manager, as Relativity's REST API authentication page names it). This tool never connects to RelativityOne and never creates or changes the OAuth2 Client; the settings below are guidance.
- Name: a descriptive name identifying the application it serves.
- Flow Grant Type: Code
- Redirect URLs: Relativity documentation: each URL includes the http or https protocol prefix. The exact callback URL is registered by the Relativity administrator. Server-side web applications, NSerio's recommendation: one exact HTTPS URL per environment, with no wildcards. Desktop and CLI applications: a plain-HTTP loopback URL, `http://127.0.0.1:<port>/<path>`, on a fixed port because the URL is registered in advance and the port cannot be chosen at runtime (verified by NSerio against a live instance; not documented by Relativity).
- Access Token Lifetime: 60 minutes, stated in minutes because the field is in minutes. Relativity documentation: tokens for this flow must have a short lifetime, and the recommended value matches the Identity Server default.
- Allow refresh tokens: NSerio's recommendation: leave it off unless the application must keep a user session active beyond the access token lifetime without asking the user to sign in again. If it is on, the refresh tokens are stored as secrets, per Secret Management (Part B of the Migration Documentation).
- Enabled: on.
<!-- generated:end oauth2-client authorization-code -->

## After the client exists

- Give the application the client ID and, through your secret store, the client secret. Never put either in source code or in tracked configuration.
- Tell your Relativity administrator the exact Redirect URL you need, for an Authorization Code client. The administrator registers it on the client.
- Keep one OAuth2 Client for each integration or each distribution. A leaked secret can then be disabled without affecting the others.
- The recommended token lifetime and what happens when a token expires are in [Token expiry](07-token-expiry.md).
