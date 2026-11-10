# OAuth 2.0 essentials (User Guide, section 4 of 8)

This section gives the OAuth 2.0 background you need to act on a report: why Basic Authentication is being replaced, what a Bearer token is, and how to choose between the two flows the tool recommends.

## Why Basic Authentication is being replaced

Basic Authentication sends a user name and a password with every request. Anyone who obtains one request, one log line or one configuration file has the password, and the password is usually a person's password or that of a long-lived integration account. OAuth 2.0 replaces it with a short-lived access token that the application obtains from RelativityOne's identity service, so the password (or the client secret) travels only when a token is requested.

**There is no date for the removal of Basic Authentication.** Relativity's public documentation gives none, and this guide does not assert one. What the documentation does say is per API and per page:

- The Usage Reports API page states: "Relativity aiR supports Bearer Token Authentication. Basic authentication is no longer supported." ([Usage Reports API](https://platform.relativity.com/RelativityOne/Content/BD_Usage_Report/Usage_Reports_API.htm))
- The Staging Insights API (REST) page states: "Basic authentication is no longer supported." ([Staging Insights API (REST)](https://platform.relativity.com/RelativityOne/Content/BD_Staging/Staging_Insights_API__REST_.htm))
- The Kepler framework page answers "What type of authentication should a Kepler service use?" with "A Kepler service must use bearer token authentication." ([Kepler framework](https://platform.relativity.com/RelativityOne/Content/Kepler_framework/Kepler_framework.htm)) That answer sits among questions about custom Kepler services, so it is guidance and not a dated removal for Relativity's own APIs.
- The Kepler .NET page states: "Using Bearer Token is recommended over using username and password." ([Proxies and authentication](https://platform.relativity.com/RelativityOne/Content/Kepler_framework/Proxies_and_authentication.htm))
- Other pages still describe Basic Authentication as a working option. That is a reason to plan the migration now, on your own schedule, and not to wait for a date.

## What a Bearer token is

A Bearer token is an access token that the application sends in the `Authorization` header of each request, in place of the Basic header:

```text
Authorization: Bearer <access token>
```

Whoever holds the token can use it until it expires, so a token is a secret. Send it only over HTTPS and never log it.

**Send the header `X-CSRF-Header: -` on REST and Kepler calls.** The value is a single dash, never blank. Relativity's Kepler framework page says: "You must set this header, but you can assign any value to it. In general, the header value is set to a dash(-)... Don't leave this header value blank. Some browsers remove empty headers. The Kepler framework returns a 404 status code when the X-CSRF-header isn't submitted." A Kepler call without the header therefore fails with a 404 that looks like a missing endpoint. Relativity's pages do not agree on whether an empty value is accepted ([HTTP headers](https://platform.relativity.com/RelativityOne/Content/REST_API/HTTP_headers.htm) and [Basic REST API concepts](https://platform.relativity.com/RelativityOne/Content/Getting_Started/Basic_REST_API_concepts.htm)), so always send the dash.

```bash
curl -H "Authorization: Bearer <access token>" -H "X-CSRF-Header: -" "<host>/Relativity.Rest/api/<path>"
```

The application obtains the token from the token endpoint `<host>/Relativity/Identity/connect/token`, which [REST API authentication](https://platform.relativity.com/RelativityOne/Content/REST_API/REST_API_authentication.htm) documents for the Client Credentials grant. Part C ([C#](../migration/part-c-csharp.md), [PowerShell](../migration/part-c-powershell.md), [VB.NET](../migration/part-c-vbnet.md), [Shell](../migration/part-c-shell.md), [JavaScript](../migration/part-c-javascript.md)) shows the exact requests for each flow.

## The two flows the tool recommends

The tool recommends one of two flows for every High or Medium finding. RelativityOne calls them **Client Credentials** and **Code** (Authorization Code) in its OAuth2 Client form.

| | Client Credentials | Authorization Code |
| --- | --- | --- |
| Who acts | The application, as itself. No user is present. | A signed-in user. The application acts with that user's permissions. |
| How the application gets a token | It sends its client ID and client secret to the token endpoint. | It sends the user to RelativityOne to sign in, receives an authorization code at a registered Redirect URL, and exchanges the code for a token. |
| Which permissions a call has | Those of the OAuth2 Client's Context User, for every call. | Those of the signed-in user. |
| When the token expires | The application requests a new one with the client secret. There are no refresh tokens. | The application uses a refresh token if refresh tokens are enabled, and otherwise the user signs in again. |
| Typical use | Services, jobs, scheduled tasks and integrations with fixed credentials. | Web applications and desktop or command-line tools run by a person who types a password or signs in. |

**How to choose.** Ask one question of the code: **who supplies the credential at run time?** If a person types it or signs in, the integration acts for that person: use **Authorization Code**. If it comes from configuration, settings, an environment variable or a secret store and no person is present, use **Client Credentials**. If your RelativityOne instance already has a Code-flow OAuth2 Client for the integration, use Authorization Code. The tool asks the same question of each file, and its note on each finding tells you when to choose the other flow. The flow of an OAuth2 Client is fixed when the client is created, so moving an integration from one flow to the other means creating a new OAuth2 Client. [Confirming the flow](../migration/part-c-csharp.md#confirming-the-flow) in Part C has the full procedure.

## Flows the tool does not recommend or cover

RelativityOne's OAuth2 Client form offers other grant types. **The tool recommends and covers only Client Credentials and Authorization Code.** In particular:

- **Implicit** returns the access token directly in the browser redirect, with no client authentication and no refresh token. Current OAuth 2.0 security guidance advises against it, and Authorization Code (with PKCE where supported) replaces it.
- **Resource Owner** (Resource Owner Password Credentials) has the application collect the user's user name and password and send them to the token endpoint. It keeps the credential-exposure risk that this migration exists to remove, and current OAuth 2.0 security guidance advises against it. Relativity's [OAuth2 clients](https://help.relativity.com/RelativityOne/Content/Relativity/Authentication/OAuth2_clients.htm) page describes this grant as a way to migrate existing clients that use direct schemes such as HTTP Basic. The tool does not recommend it, for the reason above.
- Any other grant type, such as Device Code, is not relevant to replacing Basic Authentication in the integrations the tool targets.

The tool looks for Basic Authentication only. It does not report existing OAuth 2.0 usage of any grant type.
