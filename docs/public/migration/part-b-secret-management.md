# Secret Management (Part B of the Migration Documentation)

## Who this is for and what it covers

This document is for developers and operators who are moving an application from Basic Authentication to OAuth 2.0 against RelativityOne, and who need to know where the new credentials live and how to look after them. You can follow it on its own. You do not need the analysis tool that points you here, and you do not need the source tree that the tool scanned.

It covers the two OAuth2 Client flows this migration guidance targets:

- **Client Credentials**, for background processes and services. The secret you protect is the OAuth2 Client's client secret.
- **Code**, for applications that sign a user in through the Relativity login page. The secrets you protect are the client secret and, if you turn refresh tokens on, the refresh tokens.

The other grant types that the OAuth2 Client form offers (Implicit and Resource Owner) are not supported by this guidance, and neither is a device-code flow. They are not covered here.

Three labels are used throughout, so you always know how much weight a statement carries:

- Statements about how Relativity behaves carry a link to the Relativity page that says it. The pages are listed under [Sources](#sources).
- **Inferred** means the statement follows from documented facts, or from general OAuth 2.0 practice, but no Relativity page states it.
- **Not documented by Relativity** means that the pages listed under Sources do not say. Where this matters, the text tells you what to do until Relativity publishes an answer.

Advice that is not a statement about Relativity is labelled **NSerio guidance**.

The sections are:

1. [Rotating client secrets](#rotating-client-secrets)
2. [Reading secrets from a vault at runtime](#reading-secrets-from-a-vault-at-runtime)
3. [Hardcoded credentials](#hardcoded-credentials)
4. [Choosing a least-privilege Context User](#choosing-a-least-privilege-context-user)
5. [Storing and revoking refresh tokens](#storing-and-revoking-refresh-tokens)
6. [Sources](#sources)

## Rotating client secrets

Rotation means replacing a client secret with a new one, either on a schedule or because the old one may have been exposed. This section applies to Client Credentials clients and to Code clients, because both are issued a client secret.

### What Relativity documents

- **The secret is shown once.** Since 2025-09-25, RelativityOne shows an OAuth2 Client's secret only once. Copy it into your secret store at the moment it is displayed, because you cannot read it again afterwards ([What's new][whats-new]).
- **A reset produces a new secret.** In the UI you open the OAuth2 Client, click Reset Secret and confirm ([OAuth2 clients][oauth2-clients]). Through REST you send a `GET` request to `.../oauth2-clients/{clientID}/regenerate-secret` with an empty body, and the response contains the new secret ([OAuth2 Client Manager (REST)][client-manager-rest]). Through .NET you call `RegenerateSecretAsync` with the client's ID and it returns the new secret ([OAuth2 Client Manager (.NET)][client-manager-net]).
- **The previous secret stops working immediately.** The .NET page states that "All previous secrets are immediately invalidated" ([OAuth2 Client Manager (.NET)][client-manager-net]). Anything that is still using the old secret fails at its next token request. Plan the reset as downtime for that workload.
- **The reset can be applied to Client Credentials and Code clients.** The reset is available from the UI, from REST and from .NET ([OAuth2 clients][oauth2-clients], [OAuth2 Client Manager (REST)][client-manager-rest], [OAuth2 Client Manager (.NET)][client-manager-net]).

### What Relativity does not document

- **A grace period or overlap is not documented by Relativity.** The .NET page also says that "The reset takes effect immediately or with a specified delay" ([OAuth2 Client Manager (.NET)][client-manager-net]). No documented call accepts a delay: the .NET method takes only the client's ID, and the REST call has an empty body ([OAuth2 Client Manager (REST)][client-manager-rest], [OAuth2 Client Manager (.NET)][client-manager-net]). This document therefore assumes the immediate behaviour.
- **A second active secret on the same client is not documented by Relativity.**
- **Whether access tokens that were issued under the old secret keep working until they expire is not documented by Relativity.**

The open point, in plain words: Relativity does not document a way to rotate a secret without downtime. Until it does, plan every rotation either as a short, scheduled outage for the workload that uses the secret, or with the two-client cut-over below.

### Procedure: rotate with a reset (short downtime)

1. Schedule a window in which the workload can be unavailable, and stop or pause it.
2. Reset the secret (UI: Reset Secret; REST: the `regenerate-secret` call; .NET: `RegenerateSecretAsync`).
3. Copy the new secret from the response or the dialog into your secret store immediately. It is not shown again.
4. Restart the workload so that it reads the new secret from the store.
5. Confirm that the workload obtains an access token and completes a call.

### Procedure: the two-client cut-over (inferred, not documented by Relativity)

This pattern avoids downtime, but it is **inferred**, and it is **not documented by Relativity**. It is built only from facts that Relativity does document: an OAuth2 Client needs a unique name, several clients can exist side by side, and a client can be disabled or deleted ([OAuth2 clients][oauth2-clients], [OAuth2 Client Manager (REST)][client-manager-rest]). Relativity does not recommend it, does not describe it and has not confirmed it. Test it in a sandbox instance before you use it in production.

1. Create a second OAuth2 Client with the same settings as the first. For Client Credentials, that means the same Context User, the same Access Token Lifetime and the same Trusted IPs, if you use them. For Code, it means the same Redirect URLs. Give it a distinct name.
2. Copy the second client's secret into your secret store, under a new entry, at the moment it is shown.
3. Deploy the workload configuration that points at the second client's ID and secret. If several instances run in parallel, roll them out one by one.
4. Confirm that every instance obtains tokens with the second client.
5. Disable the first client. Watch for failures for a period that suits your workload, then delete it.
6. Remove the old secret from your secret store.

What the pattern does not settle: whether disabling or deleting the first client ends access tokens that are already issued is not documented by Relativity. Treat those tokens as valid until they expire. Relativity recommends an Access Token Lifetime of 60 minutes for Client Credentials and Code clients ([OAuth2 clients][oauth2-clients]); a short lifetime also shortens that window. The field is expressed in minutes.

For a Code client, refresh tokens that were issued to the first client do not carry over to the second, so users sign in again after the cut-over. That is **inferred**.

## Reading secrets from a vault at runtime

Whichever vault you use, the shape of the solution is the same:

- The workload's own identity (a managed identity, an IAM role, a vault token or a service account) authorizes the read. No secret is needed to fetch the secret.
- The secret is read when the process starts, or when the access token has to be requested, and it is held in memory only.
- The secret is never written to a log, a configuration file in the repository or an error message.

The examples below use placeholders. Replace each `<...>` value with your own. The examples are illustrations of each vendor's public SDK or API. The migration tool never calls any vault and does not depend on any of these packages.

### Azure Key Vault

Use the `Azure.Security.KeyVault.Secrets` package together with `Azure.Identity`. `DefaultAzureCredential` picks up a managed identity when the code runs in Azure and a developer sign-in on a workstation.

```csharp
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;

Uri vaultUri = new Uri("https://<vault-name>.vault.azure.net/");
SecretClient secretClient = new SecretClient(vaultUri, new DefaultAzureCredential());

KeyVaultSecret clientSecret = await secretClient.GetSecretAsync("<secret-name>");
string clientSecretValue = clientSecret.Value;
```

Grant the workload's identity permission to read secrets only, and only on this vault.

### AWS Secrets Manager

Use the `AWSSDK.SecretsManager` package. The client resolves credentials from the environment, so an IAM role attached to the workload is enough when the code runs in AWS.

```csharp
using Amazon;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;

IAmazonSecretsManager secretsManager = new AmazonSecretsManagerClient(RegionEndpoint.GetBySystemName("<aws-region>"));

GetSecretValueResponse response = await secretsManager.GetSecretValueAsync(
	new GetSecretValueRequest { SecretId = "<secret-name-or-arn>" });
string clientSecretValue = response.SecretString;
```

Allow the role only `secretsmanager:GetSecretValue` on the specific secret.

### HashiCorp Vault

Vault exposes an HTTP API, and there are community client libraries for it. The example uses the API directly, with the KV version 2 secrets engine. The Vault token comes from the environment of the process, for instance from Vault Agent, and is never written into the source.

```bash
curl --silent --header "X-Vault-Token: ${VAULT_TOKEN}" \
	"${VAULT_ADDR}/v1/<mount>/data/<path>"
```

The response contains the secret under `data.data`. In C#, the same request is an `HttpClient` `GET` with the `X-Vault-Token` header set from `Environment.GetEnvironmentVariable("VAULT_TOKEN")`, followed by reading the `data.data.<key-name>` property of the JSON response.

Give the token a policy that can read this one path and nothing else.

### PowerShell: SecretManagement, stored credential files and SecureString

A PowerShell script has three more places a secret commonly lives, and each has a limit worth knowing before you choose it.

**The SecretManagement module.** The `Microsoft.PowerShell.SecretManagement` module reads a secret from a registered vault through one command, `Get-Secret`, whatever the vault is. The vault itself is a separate extension module that you register once: Microsoft publishes one for a local store, `Microsoft.PowerShell.SecretStore`, and there is an extension for Azure Key Vault. The migration tool recognizes `Get-Secret`, `Get-StoredCredential` and `Get-AutomationPSCredential` as evidence that a script's credential is fixed, which makes Client Credentials the recommended flow. The example uses placeholders.

```powershell
$clientSecret = Get-Secret -Name '<secret-name>' -Vault '<vault-name>' -AsPlainText
```

`Get-Secret` returns a `SecureString` unless you ask for plain text, and the OAuth2 token request needs the plain text, so read it at the moment you build the request and do not keep it in a variable longer than the request needs. **NSerio guidance:** on an unattended host, give the account the script runs as access to the vault and to nothing else, as for the other vaults above.

**A credential saved with `Export-Clixml`.** `Export-Clixml` writes a credential object to a file, and `Import-Clixml` reads it back. On Windows the password in the file is encrypted with the Windows data-protection API, so only the same user on the same machine can read it back (documented by Microsoft). On Linux and macOS the file is **not protected**: the secret is stored in a form anyone who can read the file can recover (documented by Microsoft). A saved credential file is therefore a convenience for one user on one Windows machine, not a secret store for a script that runs on a shared build agent, in a container or on another platform. Keep the file out of the repository, as for any local secret file.

**A `SecureString`.** `ConvertTo-SecureString -AsPlainText -Force` does not protect anything: it wraps a plain-text value that is already in memory, and when that value was written into the script as a literal, the secret is in the source. The tool treats a literal argument as a hardcoded credential and not as evidence of either flow, and it counts the call as evidence of a fixed credential only when its argument comes from a variable, an environment read or a call. Read the value from the environment or a secret store, and convert it only if a command needs a `SecureString`, as `Invoke-RestMethod -Authentication Bearer -Token` does on PowerShell 7.

For Authorization Code, a script that asks a person with `Get-Credential` or `Read-Host -AsSecureString` stores nothing, which is the point of that flow. What it must protect is the client secret and, if refresh tokens are on, the refresh tokens, as in [Storing refresh tokens](#storing-refresh-tokens).

### Relativity Secret Store

The Relativity Secret Store is for code that runs inside Relativity, such as custom pages, agents and event handlers, where the code is already trusted by the platform and should not carry a credential of its own. Use it there in place of an external vault.

The Secret Store interface, `ISecretStore`, ships in the `Relativity.API` assembly that Relativity-side code already references, so no extra package is needed. Your code obtains it from the `IHelper` it is given, by calling `helper.GetSecretStore()`. The member names below (`GetSecretStore`, `GetAsync`, `Secret.Data`, `SetAsync`) are not confirmed against a Relativity reference page for the Secret Store: this document cites no Relativity page for them. Check them against the `Relativity.API` version that your application references before you rely on them. Which secret paths your application may use, and the store's access rules, are not covered here.

Read the secret at the moment you need it, hold it in memory only, and do not copy it elsewhere.

```csharp
public async Task<string> ReadClientSecretAsync(IHelper helper)
{
	ISecretStore secretStore = helper.GetSecretStore();

	Secret secret = await secretStore.GetAsync("<secret-store-path>");
	string clientSecret = secret.Data["<key-name>"];

	return clientSecret;
}
```

`Secret.Data` is a string dictionary, so a single entry can hold several named values. The write counterpart, `SetAsync`, exists for the administrator or installer step that stores the value in the first place. This document gives no example for it, because any example would have to carry a value.

For code that runs outside Relativity, use one of the three vaults above.

## Hardcoded credentials

A hardcoded credential is a password, client secret or token written into source code, a configuration file or a script that is committed to a repository. It is exposed to everyone who can read the repository, and to its history, for as long as it exists.

### What the tool warns about

While it analyzes, the tool warns about every finding that looks like a credential written into the source code. The warning is printed for each such finding, whatever its confidence level, and it reads:

```text
Warning: <finding-identifier> <path>:<line> <pattern-class>: a credential appears to be written into the source code. Treat it as compromised and rotate it. See Secret Management (Part B of the Migration Documentation).
```

Points to know:

- The warning gives the location and the pattern class only. It never prints the value, and neither does the report.
- The warning is a signal to look at that location, not proof that the value is a live credential. Check it yourself.
- If a finding is below the minimum confidence level you configured, it does not appear in the findings table, but its warning still appears and says that it is below the threshold. A credential in source is a risk at every confidence level.
- The tool reads only the languages it supports in this release, and only the constructs its catalog recognizes. A clean run does not mean your repository holds no hardcoded credentials, so use the searches in the next subsection as well.
- Treat a credential that appears in a warning as compromised. The steps are: rotate it as described in [Rotating client secrets](#rotating-client-secrets), move the new value into a secret store, and remove the old value from the code. Removing a value from the latest commit does not remove it from the history.

### Finding hardcoded credentials without the tool

You can find candidates with `git grep` or ripgrep (`rg`). These patterns are starting points and use only placeholders. Expect false positives, and read every hit.

Search for a Basic Authentication header:

```bash
git grep -nEi "authorization[\"']?\s*[:=,]\s*[\"']?basic"
rg -n -i "authorization[\"']?\s*[:=,]\s*[\"']?basic"
```

Search for credentials built in code, such as .NET network credentials:

```bash
git grep -nE "NetworkCredential\(|UsernamePasswordCredentials\("
rg -n "NetworkCredential\(|UsernamePasswordCredentials\("
```

Search for a PowerShell script that turns a literal into a `SecureString`, which hides nothing:

```bash
git grep -nEi "ConvertTo-SecureString\s+[\"'][^\"']+[\"']\s+-AsPlainText"
rg -n -i "ConvertTo-SecureString\s+[\"'][^\"']+[\"']\s+-AsPlainText"
```

Search for a client secret assigned to a literal:

```bash
git grep -nEi "client_?secret[\"']?\s*[:=]\s*[\"'][^\"']+[\"']"
rg -n -i "client_?secret[\"']?\s*[:=]\s*[\"'][^\"']+[\"']"
```

Search for a password assigned to a literal:

```bash
git grep -nEi "password[\"']?\s*[:=]\s*[\"'][^\"']+[\"']"
rg -n -i "password[\"']?\s*[:=]\s*[\"'][^\"']+[\"']"
```

Search for a bearer token or an encoded credential in a header:

```bash
git grep -nEi "bearer\s+[A-Za-z0-9._~+/=-]{20,}"
```

To search the history and not only the current files, add `git log -p -S "<literal-fragment>"` for a string you already know, or run a dedicated scanner over the full history (see the next subsection).

When you find one:

1. Do not paste the value into a ticket, a chat or a log.
2. Rotate the credential first, then clean the code.
3. Replace the literal with a read from the environment or a secret store.
4. Add a check that stops the same pattern coming back.

### Keeping credentials out of source

**Read them from the environment or a secret store.** For local development and for containers, an environment variable is the simplest step. For production, prefer a vault (see [Reading secrets from a vault at runtime](#reading-secrets-from-a-vault-at-runtime)).

```csharp
string clientId = Environment.GetEnvironmentVariable("RELATIVITY_CLIENT_ID")
	?? throw new InvalidOperationException("RELATIVITY_CLIENT_ID is not set.");
string clientSecret = Environment.GetEnvironmentVariable("RELATIVITY_CLIENT_SECRET")
	?? throw new InvalidOperationException("RELATIVITY_CLIENT_SECRET is not set.");
```

In PowerShell the same read is:

```powershell
$clientId = $env:RELATIVITY_CLIENT_ID
$clientSecret = $env:RELATIVITY_CLIENT_SECRET

if (-not $clientId -or -not $clientSecret) {
	throw 'RELATIVITY_CLIENT_ID and RELATIVITY_CLIENT_SECRET must be set.'
}
```

**Keep local secret files out of the repository.** If you use a local file for development, add it to `.gitignore` before it is created.

```text
.env
.env.*
*.secrets.json
```

**Scan before every commit.** Install a secret scanner as a pre-commit hook, so a credential is stopped before it enters the history. Tools such as gitleaks and detect-secrets do this. The hook below uses gitleaks. Its command line differs between versions, so check the syntax for the version you install.

```bash
#!/bin/sh
# .git/hooks/pre-commit
gitleaks protect --staged --redact
```

Run the same scanner in your continuous integration pipeline over pull requests, so a hook that a developer skipped is still caught.

**Do not print secrets.** Do not log request bodies for the token endpoint, and do not include a secret in an exception message.

## Choosing a least-privilege Context User

This section applies to Client Credentials clients.

### What Relativity documents

The first three points are from the [OAuth2 clients][oauth2-clients] page:

- A Context User is **required** when the flow is Client Credentials, and it **cannot be specified for other flows**.
- The Context User is the "Relativity user context for OAuth2 client authorization". Relativity states that this enables an administrator to restrict the access privileges of an OAuth2 Client based on the user's permissions, as well as to audit.
- An OAuth2 Client continues to function even if the Context User's Relativity account is disabled. The client stays operational as long as the OAuth2 Client itself is enabled.

The last point is from a different page, the [REST API authentication][rest-authentication] page, and is not on the OAuth2 clients page:

- Its instruction for the Context User is to select a user with the appropriate level of permissions or group membership for the operations you want to perform. It adds that this is usually a member of the Relativity Administrators group, but could be different depending on the API or operation invoked.

Reading that last point together with the first three: a Client Credentials client acts with the Context User's permissions. Whatever that user can do, the client can do.

### Guidance

The following is **NSerio guidance**. It goes further than the REST API authentication page's "usually a member of the Relativity Administrators group" because that is a description of a common case, not a recommendation to use administrator rights.

- **Create a dedicated integration user for each integration.** Do not reuse a personal account. A personal account leaves with the person, changes when the person's role changes and mixes the person's activity with the integration's in the audit trail.
- **Give it the minimum permissions** that the operations need: only the workspaces, object types and actions that the integration touches, granted through a group that exists for that purpose.
- **Do not make it a system administrator** and do not add it to the Relativity Administrators group unless a specific API you call demands it. Where a call fails with a permission error, add the one missing permission rather than widening the role.
- **Use one integration user per application** where you can. A compromised secret then exposes one application's rights.
- **Disabling the user does not stop the client.** The documented behaviour above means the client keeps working when its Context User is disabled. To cut off access, disable the OAuth2 Client, or reset its secret.

Procedure:

1. Write down the operations your integration performs and the workspaces where it performs them.
2. Create a Relativity user for the integration, with a name that identifies the application, and put it in a group holding only the permissions from step 1.
3. Create the OAuth2 Client with the Client Credentials flow and select that user as the Context User.
4. Run the integration and check each operation. Add a missing permission only when a call is refused.
5. Review the user's group membership on a regular schedule.

## Storing and revoking refresh tokens

This section applies to Code clients that have Allow Refresh Tokens turned on.

### What Relativity documents

- **Refresh tokens need PKCE.** The [OAuth2 clients][oauth2-clients] page states: "To obtain a refresh token, it is necessary to use an Authorization Code with a Proof Key for Code Exchange (PKCE) flow." The same entry for Allow Refresh Tokens says that refresh tokens require updates to the application and that you must test any changes in a sandbox instance before you deploy to production.
- **Relativity does not document the setting's default.** The pages listed under Sources do not say whether Allow Refresh Tokens starts on or off. Check the form when you create the client. The setting is **NSerio guidance**: leave it off unless the application must keep a user session alive beyond the access token's lifetime without asking the user to sign in again.
- **Relativity's pages are not consistent about which flows can receive refresh tokens** ([OAuth2 clients][oauth2-clients], [OAuth2 Client Manager (REST)][client-manager-rest], [OAuth2 Client Manager (.NET)][client-manager-net]). For a Client Credentials client, request a new access token with the client secret when the old one expires, and do not rely on a refresh token. That advice is **inferred**, and it holds whatever the answer is.

### What Relativity does not document

- **The lifetime of a refresh token is not documented by Relativity.**
- **Rotation is not documented by Relativity.** It is not stated whether each use returns a new refresh token and invalidates the old one.
- **Revocation is not documented by Relativity.** No revocation endpoint is documented. The descriptions of deleting, disabling and resetting the secret of an OAuth2 Client do not say what happens to refresh tokens that are already issued ([OAuth2 clients][oauth2-clients], [OAuth2 Client Manager (REST)][client-manager-rest]).
- **The refresh call itself is inferred.** Relativity documents the token endpoint, `https://<instance>/Relativity/Identity/connect/token` ([REST API authentication][rest-authentication]), but does not document a refresh request. By standard OAuth 2.0 practice, a refresh is a request to that same token endpoint with `grant_type=refresh_token`. This is **inferred**, and it is **not documented by Relativity**:

```bash
curl --request POST "https://<instance>/Relativity/Identity/connect/token" \
	--data-urlencode "grant_type=refresh_token" \
	--data-urlencode "refresh_token=<refresh-token>" \
	--data-urlencode "client_id=<client-id>" \
	--data-urlencode "client_secret=<client-secret>"
```

Confirm the request in a sandbox instance before you rely on it.

### Storing refresh tokens

A refresh token lets its holder obtain new access tokens without the user. Treat it with the same care as a client secret.

- Store it in a secret store (see [Reading secrets from a vault at runtime](#reading-secrets-from-a-vault-at-runtime)), or in encrypted server-side session storage, never in a source file, a plain configuration file, a log line or a URL.
- Keep it on the server. Do not send it to a browser.
- Store one refresh token per user, and delete it when the user signs out or when the account is removed from the application.
- Because rotation is not documented, keep the latest refresh token that Relativity returned, and replace the stored value whenever a refresh response carries a new one.

### If a refresh token or client secret is compromised

Because revocation is not documented by Relativity and no revocation endpoint is documented, do not depend on revoking a single token. Disable the OAuth2 Client and replace it.

1. Disable the OAuth2 Client that issued the refresh token, in the OAuth2 Client Manager. A disabled client no longer has access to Relativity ([OAuth2 clients][oauth2-clients]).
2. Create a new OAuth2 Client with the same settings, including its Redirect URLs.
3. Store the new client's secret in your secret store at the moment it is shown.
4. Deploy the new client ID and secret to the application.
5. Delete the stored refresh tokens that belong to the old client. Users sign in again to receive new ones.
6. Delete the old client once you have confirmed the application works with the new one.
7. Review how the credential was exposed and close that route.

Access tokens that were issued before step 1 may stay valid until they expire (not documented by Relativity). A short Access Token Lifetime limits that window. Relativity recommends 60 minutes for Code clients ([OAuth2 clients][oauth2-clients]). The field is expressed in minutes.

## Sources

The statements about Relativity in this document come from these public Relativity pages.

- [OAuth2 clients][oauth2-clients] (RelativityOne documentation): https://help.relativity.com/RelativityOne/Content/Relativity/Authentication/OAuth2_clients.htm. It supports the fields of the OAuth2 Client form, the Context User rules and the Context User behaviour when the account is disabled, the PKCE sentence for refresh tokens, the UI steps for Reset Secret, the recommended Access Token Lifetime of 60 minutes, and the fact that a client can be enabled or disabled.
- [OAuth2 Client Manager (REST)][client-manager-rest]: https://platform.relativity.com/RelativityOne/Content/BD_Identity/OAuth2_Client_Manager__REST_.htm. It supports the `regenerate-secret` call, its empty request body, and the deletion of a client through the API.
- [OAuth2 Client Manager (.NET)][client-manager-net]: https://platform.relativity.com/RelativityOne/Content/BD_Identity/OAuth2_Client_Manager__.NET_.htm. It supports `RegenerateSecretAsync`, the statement that all previous secrets are immediately invalidated, and the sentence about a reset taking effect immediately or with a specified delay.
- [What's new][whats-new] (Relativity developer news): https://platform.relativity.com/RelativityOne/Content/What_s_new/What_s_new.htm. It supports the change, dated 2025-09-25, that an OAuth2 Client's secret is shown once only.

- [REST API authentication][rest-authentication]: https://platform.relativity.com/RelativityOne/Content/REST_API/REST_API_authentication.htm. It supports the address of the token endpoint, `<host>/Relativity/Identity/connect/token`, and the description of the Context User as usually a member of the Relativity Administrators group, though it could be different depending on the API or operation invoked.

The Azure Key Vault, AWS Secrets Manager and HashiCorp Vault examples follow those vendors' own public SDK and API documentation, and the PowerShell section follows Microsoft's public documentation of the SecretManagement module and of `Export-Clixml`. They are not Relativity statements.

[oauth2-clients]: https://help.relativity.com/RelativityOne/Content/Relativity/Authentication/OAuth2_clients.htm
[client-manager-rest]: https://platform.relativity.com/RelativityOne/Content/BD_Identity/OAuth2_Client_Manager__REST_.htm
[client-manager-net]: https://platform.relativity.com/RelativityOne/Content/BD_Identity/OAuth2_Client_Manager__.NET_.htm
[whats-new]: https://platform.relativity.com/RelativityOne/Content/What_s_new/What_s_new.htm
[rest-authentication]: https://platform.relativity.com/RelativityOne/Content/REST_API/REST_API_authentication.htm
