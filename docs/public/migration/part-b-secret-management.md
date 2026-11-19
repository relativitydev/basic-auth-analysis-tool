# Secret Management (Part B of the Migration Documentation)

## Who this is for and what it covers

This document is for developers and operators who are moving an application from Basic Authentication to OAuth 2.0 against RelativityOne, and who need to know where the new credentials live and how to look after them. It stands on its own: you need nothing else to follow it. It covers how to store, rotate and revoke the credentials, how to keep them out of source code, and how to choose a least-privilege Context User, and it has a section for each of the eight languages the migration guidance covers: C#, VB.NET, JavaScript, TypeScript, Python, Go, PowerShell and Shell.

This is Part B of the Migration Documentation, which you use without running anything. Read it after you have found Basic Authentication with Part A and chosen the change with Part C, or at any point when you need to decide where a new credential lives. It applies to every language. New here? Start with the [Migration Documentation overview](README.md).

It covers the two OAuth2 Client flows this migration guidance targets:

- **Client Credentials**, for background processes and services. The secret you protect is the OAuth2 Client's client secret.
- **Authorization Code**, for applications that sign a user in through the Relativity login page (the grant type that the OAuth2 Client form calls **Code**, so a client of this flow is a "Code client"). The secrets you protect are the client secret and, if you turn refresh tokens on, the refresh tokens.

The flow of an OAuth2 Client is fixed when the client is created: editing a client cannot change it ([OAuth2 clients][oauth2-clients], [OAuth2 Client Manager (REST)][client-manager-rest]). To change a client's flow, create a new client.

Implicit, Resource Owner and any other grant type, such as Device Code, are not recommended or covered here.

These labels are used throughout, so you always know how much weight a statement carries:

- A statement about how Relativity behaves carries a link to the Relativity page that says it. The pages are listed under [Sources](#sources).
- **Documented by Microsoft**, or by another named vendor or manual, means that the statement comes from that source's public documentation.
- **Checked by NSerio** means that NSerio ran the command or the code on the versions named in the text.
- **Inferred** means the statement follows from documented facts, or from general OAuth 2.0 practice, but no Relativity page states it.
- **Not documented by Relativity** means that the pages listed under Sources do not say. Where this matters, the text tells you what to do until Relativity publishes an answer.
- **Verified by NSerio against a live instance; not documented by Relativity** means that NSerio checked the statement against a running RelativityOne instance and that no Relativity page documents it.
- **NSerio guidance** is advice that is not a statement about Relativity.

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

For a user's password, which is not a client secret, change it in RelativityOne or in the identity provider that owns the account. For a client secret, follow this section.

## Reading secrets from a vault at runtime

Whichever vault you use, the shape of the solution is the same:

- The workload's own identity (a managed identity, an IAM role, a vault token or a service account) authorizes the read. No secret is needed to fetch the secret.
- The secret is read when the process starts, or when the access token has to be requested, and it is held in memory only. A shell script is the one exception, because a script has no memory that outlives its commands: see [Shell: the command line, secret files and the keychain](#shell-the-command-line-secret-files-and-the-keychain).
- The secret is never written to a log, a configuration file in the repository or an error message.

The examples below use placeholders. Replace each `<...>` value with your own. The examples are illustrations of each vendor's public SDK or API, and none of them is required.

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

Vault exposes an HTTP API, and there are community client libraries for it. The example uses the API directly, with the KV version 2 secrets engine. The Vault token comes from the environment of the process, for instance from Vault Agent, and is never written into the source. It is also never put on the `curl` command line, where every user of the machine could read it: the example writes the header to a file that only its owner can read, passes the file with `--header @<file>`, and removes the file when the script ends. `mktemp` creates a file that only its owner can read, and `printf` is a shell built-in, so the token is not an argument of any process.

```bash
header_file="$(mktemp)"
trap 'rm -f "${header_file}"' EXIT

printf 'X-Vault-Token: %s\n' "${VAULT_TOKEN}" > "${header_file}"

curl --silent --header "@${header_file}" \
	"${VAULT_ADDR}/v1/<mount>/data/<path>"
```

The response contains the secret under `data.data`. In C#, the same request is an `HttpClient` `GET` with the `X-Vault-Token` header set from `Environment.GetEnvironmentVariable("VAULT_TOKEN")`, followed by reading the `data.data.<key-name>` property of the JSON response.

Give the token a policy that can read this one path and nothing else.

### PowerShell: SecretManagement, stored credential files and SecureString

A PowerShell script has three more places a secret commonly lives, and each has a limit worth knowing before you choose it.

**The SecretManagement module.** The `Microsoft.PowerShell.SecretManagement` module reads a secret from a registered vault through one command, `Get-Secret`, whatever the vault is. The vault itself is a separate extension module that you register once: Microsoft publishes one for a local store, `Microsoft.PowerShell.SecretStore`, and there is an extension for Azure Key Vault. Automation platforms and credential managers offer their own commands for the same purpose, such as `Get-StoredCredential` and `Get-AutomationPSCredential`. The example uses placeholders.

```powershell
$clientSecret = Get-Secret -Name '<secret-name>' -Vault '<vault-name>' -AsPlainText
```

`Get-Secret` returns a `SecureString` unless you ask for plain text, and the OAuth2 token request needs the plain text, so read it at the moment you build the request and do not keep it in a variable longer than the request needs. **NSerio guidance:** on an unattended host, give the account the script runs as access to the vault and to nothing else, as for the other vaults in this section.

**A credential saved with `Export-Clixml`.** `Export-Clixml` writes a credential object to a file, and `Import-Clixml` reads it back. On Windows the password in the file is encrypted with the Windows data-protection API, so only the same user on the same machine can read it back (documented by Microsoft). On Linux and macOS the file is **not protected**: the secret is stored in a form anyone who can read the file can recover (documented by Microsoft). A saved credential file is therefore a convenience for one user on one Windows machine, not a secret store for a script that runs on a shared build agent, in a container or on another platform. Keep the file out of the repository, as for any local secret file.

**A `SecureString`.** `ConvertTo-SecureString -AsPlainText -Force` does not protect anything: it wraps a plain-text value that is already in memory, and when that value was written into the script as a literal, the secret is in the source. Read the value from the environment or a secret store, and convert it only if a command needs a `SecureString`, as `Invoke-RestMethod -Authentication Bearer -Token` does on PowerShell 7.

For Authorization Code, a script that asks a person with `Get-Credential` or `Read-Host -AsSecureString` stores nothing, which is the point of that flow. What it must protect is the client secret and, if refresh tokens are on, the refresh tokens, as in [Storing refresh tokens](#storing-refresh-tokens).

### VB.NET: configuration files, My.Settings and protected data

A VB.NET application has places a secret commonly lives that look safer than they are, and each has a limit worth knowing before you choose it.

**`app.config`, `web.config` and `My.Settings`.** An application setting declared in the project's settings designer is stored in a `.settings` file and in the application's configuration file (`app.config`, or `web.config` for a web application) as plain text, and a setting of the user scope is also written to a per-user file. None of these encrypts the value. Do not keep the client secret in any of them, and do not commit one with a real value in it. Search these files by hand for credentials; see [Finding hardcoded credentials](#finding-hardcoded-credentials).

**`ProtectedData`.** `ProtectedData.Protect` and `ProtectedData.Unprotect` use the Windows data-protection API. With `DataProtectionScope.CurrentUser` the data can be read back only by the same Windows user, and with `DataProtectionScope.LocalMachine` by any process on the machine (documented by Microsoft). The API is a Windows facility: on .NET it works on Windows only (documented by Microsoft). It suits one user on one Windows machine, not a service that runs in a container or on another platform.

**User secrets.** `AddUserSecrets` reads a development-time secret file that lives in the user's profile folder, outside the repository. The file is not encrypted (documented by Microsoft), so it is a convenience for development and not a place for a production secret.

For Authorization Code, an application that asks a person for the password stores nothing, which is the point of that flow. What it must protect is the client secret and, if refresh tokens are on, the refresh tokens, as in [Storing refresh tokens](#storing-refresh-tokens).

### C#: the same .NET facilities

A C# application keeps its secrets in the same places as a VB.NET application, because both use the same .NET facilities, and the limits described in [VB.NET: configuration files, My.Settings and protected data](#vbnet-configuration-files-mysettings-and-protected-data) apply to it unchanged. In C# the configuration files are `app.config`, `web.config` and `appsettings.json`: none of them encrypts a value, so do not keep the client secret in any of them and do not commit one with a real value in it. `ProtectedData` and `AddUserSecrets` have the limits described there. For a C# application that runs inside Relativity, see [Relativity Secret Store](#relativity-secret-store); for everything else, read the secret from the environment or a vault, as in [Keeping credentials out of source](#keeping-credentials-out-of-source).

### Shell: the command line, secret files and the keychain

A shell script has places a secret commonly lives that look safer than they are, and each has a limit worth knowing before you choose it.

**The command line.** Everything on a command line is visible to every user of the machine in the process list while the command runs. That includes `curl -u <user>:<secret>`, a `wget` password option, a token given as `-H "Authorization: Bearer <token>"`, and a client secret given as `--data-urlencode "client_secret=<secret>"`. A shell variable that holds options and is expanded unquoted, such as `curl ${options}`, puts them on the command line in the same way. `curl` can read the value from a file instead: `--data-urlencode "client_secret@<file>"` reads the client secret from a file (available since `curl` 7.18.0), and `-H @<file>` reads header lines from a file (available since `curl` 7.55.0) (documented by the `curl` manual; checked by NSerio on 7.81 and 8.17). The file is read whole, a trailing line feed included, so write a secret file with `printf '%s'` and not with `echo`. `curl` also tries to blank a `-u` password in the process list, but that is best-effort, so do not rely on it (documented by the `curl` project).

**A secret file.** A file that holds the client secret should be created readable by its owner only, which a `umask 077` before the write does, in a subshell so that the setting does not outlive the write:

```sh
( umask 077; printf '%s' "${CLIENT_SECRET}" > "${CLIENT_SECRET_FILE}" )
```

**NSerio guidance:** keep the file untracked (git-ignored) or outside the repository, and out of every build output, never in a tracked `.env` file, and do not export the secret to child processes: an exported variable is inherited by every program the script starts. A script that runs on a shared host should read the secret from the file at the moment it builds the request, and not hold it in a variable longer than the request needs.

**The keychain and the secret tools.** The operating system's keychain (`security find-generic-password` on macOS, `secret-tool lookup` on a Linux desktop) and the secret tools of the vaults above (`pass show`, `gopass show`, `vault kv get`, `aws secretsmanager get-secret-value`, `aws ssm get-parameter`, `az keyvault secret show`, `gcloud secrets versions access`, `op read`, `bw get password`) read a secret that was stored outside the script. As for the other vaults, give the account the script runs as access to the secret it needs and to nothing else.

**`.netrc` and `.wgetrc`.** `curl --netrc` and `wget` read a credential from a plain-text file in the user's home folder. The file is not encrypted: restrict it to its owner (`chmod 600`), keep it out of every repository, and do not treat it as a secret store for a shared account.

**The shell's history.** A command typed at a prompt is saved in the history file of an interactive shell, secret included. Do not type a client secret on a command line: read it from a file, or with `read -s`. `read -s` hides the typed input in `bash` and `zsh`. In `mksh` the option means something else: it saves the input line to the history, and `mksh` has no `read` option that hides the input (documented by the `mksh` manual), so in `mksh` hide the input with `stty -echo` around the `read`. `ksh93` does not hide the input either and saves the line to its history file even when the script is not interactive, so in `ksh93` read the secret from a file instead.

For Authorization Code, a script that asks a person with `read -s` stores nothing, which is the point of that flow. What it must protect is the client secret and, if refresh tokens are on, the refresh tokens, as in [Storing refresh tokens](#storing-refresh-tokens).

### JavaScript: bundles, browser storage and the server

JavaScript runs in two places, and the two do not keep a secret the same way. Code that runs in a browser is delivered to the person using it, so nothing in it, and nothing it can read, is a secret from that person.

**A bundle.** A client secret written into a source file that is built into a browser bundle is public: anyone who loads the page can read it, minified or not. This holds for a value that a build step substitutes from an environment variable at build time, because the value is then in the bundle. Do not put the client secret in browser code or in any file that is built into it. **NSerio guidance:** the browser code asks a server-side component of your own, and that component holds the client secret and calls RelativityOne.

**Browser storage.** `localStorage`, `sessionStorage`, IndexedDB and a cookie that script can read are all readable by any script that runs in the page, including a script that came in through a compromised dependency. Do not keep a client secret or a refresh token in them. **NSerio guidance:** keep the tokens on the server, and give the browser only a session cookie that is set by the server with the `HttpOnly`, `Secure` and `SameSite` attributes, so that page script cannot read it. The browser never holds a RelativityOne access token or refresh token: the server-side component keeps both.

**A `.env` file.** A file read by Node.js at start-up is plain text on the disk. Keep it untracked (git-ignored) or outside the repository, and out of every build output, as in [Keeping credentials out of source](#keeping-credentials-out-of-source). A bundler may copy values from such a file into a browser bundle, so check what a build step exposes before you use one.

**Node.js.** A Node.js process reads the client secret from its environment or from a vault, as in [Reading secrets from a vault at runtime](#reading-secrets-from-a-vault-at-runtime). Do not pass it as a command-line argument, which other users of the machine can see in the process list, and do not write it to the log or into an error message.

For Authorization Code, a server-side component keeps the client secret and, if refresh tokens are on, the refresh tokens, as in [Storing refresh tokens](#storing-refresh-tokens).

### TypeScript: environment files, types and the server

Everything under [JavaScript: bundles, browser storage and the server](#javascript-bundles-browser-storage-and-the-server) holds for TypeScript, because TypeScript is compiled to JavaScript and runs in the same two places. This section adds what TypeScript changes.

**An environment file of a browser application.** A TypeScript browser application, an Angular application for example, often keeps its settings in an environment file that the build compiles into the bundle. **Inferred:** a file that a browser build reads is part of what the build ships, so a client secret written in it is public to anyone who loads the page, as for any other source file, whether the file is named for development or for production and whether or not it is checked in. **NSerio guidance:** never put the client secret, or any RelativityOne token, in an environment file or in any file the browser build reads. Put there only values that are meant to be public, such as the address of your own server-side component. The browser calls that component, and the component holds the client secret and calls RelativityOne.

**The server reads the secret through its configuration.** A TypeScript server, a NestJS server for example, should read the client secret from its own configuration, which the process fills from the environment or from a vault, as in [Reading secrets from a vault at runtime](#reading-secrets-from-a-vault-at-runtime). **NSerio guidance:** read the value once, check that it is set, and fail at start-up when it is not, so that a missing secret is an error you see and not an empty string that reaches the token endpoint. A configuration file that is checked in, an object literal in a class and a `const` in a source file are source, whatever the type says. The names of the configuration service of a particular framework are not verified in this document: follow that framework's own documentation for where the service reads its values from.

**A type does not protect a secret.** A `readonly` field, a `private` member and a type such as `string` or `Secret` are checked by the compiler and are gone from the running program; they do not hide a value that is written in the source. A non-null mark or a cast on an environment read (`process.env.RELATIVITY_CLIENT_SECRET!`, `process.env.RELATIVITY_CLIENT_SECRET as string`) tells the compiler that the value is set and does not check it. Check it yourself, as in [Keeping credentials out of source](#keeping-credentials-out-of-source).

**Do not log the secret through a typed object.** A request-configuration object that holds an `auth` member, or a typed settings object that holds the client secret, is easily written to a log or to an error message whole. Log the fields you need and never the object.

### Python: the environment, credential stores and the command line

A Python program has places a secret commonly lives that look safer than they are, and each has a limit worth knowing before you choose it.

**The environment.** `os.environ` holds the process's environment variables. Read the client secret from it, check that it is set, and fail at start-up when it is not, as in [Keeping credentials out of source](#keeping-credentials-out-of-source). An environment variable is inherited by every child process the program starts, so do not pass `os.environ` whole to a subprocess that does not need the secret.

**A credential store.** A package such as `keyring` reads and writes the operating system's credential store, such as the Windows Credential Manager, the macOS Keychain or a Linux secret service (documented by that package). On a server, read the secret from a vault, as in [Reading secrets from a vault at runtime](#reading-secrets-from-a-vault-at-runtime). **NSerio guidance:** a credential store tied to a user session suits a program that a person runs, not an unattended service.

**The command line and the prompt.** Do not accept the client secret as a command-line argument, for example through `argparse`: other users of the machine can read it from the process list while the program runs, and it is saved in the shell's history. For Authorization Code, a program that asks a person for a password with `getpass.getpass()` stores nothing and does not echo the typed text, which is the point of that flow. What it must protect is the client secret and, if refresh tokens are on, the refresh tokens, as in [Storing refresh tokens](#storing-refresh-tokens).

**Source and logs.** A `.env` file that a library loads at start-up is plain text on the disk: keep it untracked (git-ignored) or outside the repository, as in [Keeping credentials out of source](#keeping-credentials-out-of-source). A literal in a source file, a default value of a function parameter and a module-level constant are source. Do not write the secret to a log, and do not log a request or a settings object whole: log the fields you need.

### Go: the environment, build-time values and the command line

A Go program is built into one binary, which changes where a secret can end up.

**The environment.** `os.LookupEnv` returns the value and a second result that says whether the variable is set, and `os.Getenv` returns an empty string for a variable that is unset and for one that is empty, so it cannot tell the two apart. Read the client secret with `os.LookupEnv`, check both the result and the value, and fail at start-up when the secret is missing, as in [Keeping credentials out of source](#keeping-credentials-out-of-source).

**The binary carries what you put in it.** A file included with `go:embed`, a value stamped at build time with `-ldflags "-X <package>.<variable>=<value>"` and a string constant are all stored in the binary, and anyone who holds the binary can read them, so a client secret must be in none of them. The build command, with the stamped value in it, is also saved in the build log and in the shell's history.

**The command line.** Do not accept the client secret through the `flag` package or in `os.Args`: other users of the machine can read it from the process list while the program runs, and it is saved in the shell's history. Read it from the environment, a file that only its owner can read, or a vault.

For Authorization Code, a program that asks a person for a password at the terminal stores nothing, which is the point of that flow. What it must protect is the client secret and, if refresh tokens are on, the refresh tokens, as in [Storing refresh tokens](#storing-refresh-tokens). Do not write the secret to a log, and do not log a configuration struct whole: log the fields you need.

### Relativity Secret Store

The Relativity Secret Store is for code that runs inside Relativity, such as custom pages, agents and event handlers, where the code is already trusted by the platform and should not carry a credential of its own. Use it there in place of an external vault.

The Secret Store interface, `ISecretStore`, ships in the `Relativity.API` assembly that Relativity-side code already references, so no extra package is needed. Your code obtains it from the `IHelper` it is given, by calling `helper.GetSecretStore()`. The interface has a synchronous read, `Get(path)`, and an asynchronous one, `GetAsync(path)`; the path is the logical path where the secret is stored and identifies the secret ([ISecretStore.Get][secretstore-get], [ISecretStore.GetAsync][secretstore-getasync], [ISecretStore][secretstore-interface]). Which secret paths your application may use, the store's access rules, `Secret.Data` and the write counterpart `SetAsync` are not covered by those pages as cited here: check them against the `Relativity.API` version that your application references before you rely on them.

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

Code that cannot await can call `secretStore.Get("<secret-store-path>")` instead, which returns the same `Secret`.

`Secret.Data` is a string dictionary, so a single entry can hold several named values. `SetAsync` exists for the administrator or installer step that stores the value in the first place. This document gives no example for it, because any example would have to carry a value.

For code that runs outside Relativity, use one of the vaults in this section, or a store named in the language subsections.

## Hardcoded credentials

A hardcoded credential is a password, client secret or token written into source code, a configuration file or a script that is committed to a repository. It is exposed to everyone who can read the repository, and to its history, for as long as it exists.

Treat a hardcoded credential as compromised. The steps are: rotate it, move the new value into a secret store, and remove the old value from the code. For a user's password, change it in RelativityOne or in the identity provider that owns the account; for a client secret, follow [Rotating client secrets](#rotating-client-secrets). Removing a value from the latest commit does not remove it from the history.

### Finding hardcoded credentials

You can find candidates with `git grep` or ripgrep (`rg`). These patterns are starting points and use only placeholders. Expect false positives, and read every hit. A search over the files of a repository can miss a construct that it does not describe, so a search that finds nothing does not mean the repository holds no hardcoded credentials: also run a secret scanner, as in [Keeping credentials out of source](#keeping-credentials-out-of-source).

These searches also match lines in comments. A credential left in a comment is as exposed as a live one: rotate it and remove it.

Search for a Basic Authentication header:

```bash
git grep -nEi "authorization[\"']?[[:space:]]*[:=,][[:space:]]*[\"']?basic"
rg -n -i "authorization[\"']?\s*[:=,]\s*[\"']?basic"
```

Search for credentials built in code, such as .NET network credentials:

```bash
git grep -nE "NetworkCredential\(|UsernamePasswordCredentials\("
rg -n "NetworkCredential\(|UsernamePasswordCredentials\("
```

Search for a PowerShell script that turns a literal into a `SecureString`, which hides nothing:

```bash
git grep -nEi "ConvertTo-SecureString[[:space:]]+[\"'][^\"']+[\"'][[:space:]]+-AsPlainText"
rg -n -i "ConvertTo-SecureString\s+[\"'][^\"']+[\"']\s+-AsPlainText"
```

Search for a client secret assigned to a literal:

```bash
git grep -nEi "client_?secret[\"']?[[:space:]]*[:=][[:space:]]*[\"'][^\"']+[\"']"
rg -n -i "client_?secret[\"']?\s*[:=]\s*[\"'][^\"']+[\"']"
```

Search for a password assigned to a literal:

```bash
git grep -nEi "password[\"']?[[:space:]]*[:=][[:space:]]*[\"'][^\"']+[\"']"
rg -n -i "password[\"']?\s*[:=]\s*[\"'][^\"']+[\"']"
```

Search for a bearer token or an encoded credential in a header:

```bash
git grep -nEi "bearer[[:space:]]+[A-Za-z0-9._~+/=-]{20,}"
rg -n -i "bearer\s+[A-Za-z0-9._~+/=-]{20,}"
```

To search the history and not only the current files, add `git log -p -S "<literal-fragment>"` for a string you already know, or run a dedicated secret scanner over the full history.

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

In VB.NET the same read is:

```vbnet
Dim clientId As String = Environment.GetEnvironmentVariable("RELATIVITY_CLIENT_ID")
Dim clientSecret As String = Environment.GetEnvironmentVariable("RELATIVITY_CLIENT_SECRET")

If String.IsNullOrEmpty(clientId) OrElse String.IsNullOrEmpty(clientSecret) Then
	Throw New InvalidOperationException("RELATIVITY_CLIENT_ID and RELATIVITY_CLIENT_SECRET must be set.")
End If
```

In JavaScript, in a Node.js process, the same read is:

```javascript
const clientId = process.env.RELATIVITY_CLIENT_ID;
const clientSecret = process.env.RELATIVITY_CLIENT_SECRET;

if (!clientId || !clientSecret) {
	throw new Error("RELATIVITY_CLIENT_ID and RELATIVITY_CLIENT_SECRET must be set.");
}
```

In TypeScript, in a Node.js process, the same read checks the value instead of asserting it:

```typescript
function requireEnvironmentValue(name: string): string {
	const value = process.env[name];

	if (value === undefined || value === "") {
		throw new Error(`Environment variable ${name} is not set`);
	}

	return value;
}

const clientId: string = requireEnvironmentValue("RELATIVITY_CLIENT_ID");
const clientSecret: string = requireEnvironmentValue("RELATIVITY_CLIENT_SECRET");
```

In Python the same read checks the value:

```python
import os


def require_environment_value(name):
	value = os.environ.get(name)

	if not value:
		raise RuntimeError(f"Environment variable {name} is not set")

	return value


client_id = require_environment_value("RELATIVITY_CLIENT_ID")
client_secret = require_environment_value("RELATIVITY_CLIENT_SECRET")
```

In Go the same read uses `os.LookupEnv`, so that an unset variable and an empty one are both caught:

```go
func requireEnvironmentValue(name string) (string, error) {
	value, isSet := os.LookupEnv(name)

	if !isSet || value == "" {
		return "", fmt.Errorf("environment variable %s is not set", name)
	}

	return value, nil
}
```

In a POSIX shell script the same read stops the script when a variable is unset or empty. Read the values and do not `export` them:

```sh
: "${RELATIVITY_CLIENT_ID:?RELATIVITY_CLIENT_ID is not set}"
: "${RELATIVITY_CLIENT_SECRET:?RELATIVITY_CLIENT_SECRET is not set}"
```

**Keep local secret files untracked.** If you use a local file for development, add it to `.gitignore` before it is created, and keep it out of every build output.

```text
.env
.env.*
*.secrets.json
```

**Check every commit for secrets.** Install a secret scanner as a pre-commit hook, so a credential is stopped before it enters the history. Tools such as gitleaks and detect-secrets do this. The hook below uses gitleaks version 8.19.0 or later, with the staged-changes form that gitleaks documents for a pre-commit hook (documented by gitleaks). The older form, `gitleaks protect --staged --redact`, is deprecated since version 8.19.0: it still runs, but it is hidden from `--help` and may be removed. Its command line differs between versions, so check the syntax for the version you install.

```bash
#!/bin/sh
# .git/hooks/pre-commit
gitleaks git --pre-commit --redact --staged
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
- **A Client Credentials client has no refresh tokens** (verified by NSerio against a live instance; not documented by Relativity). Relativity's own pages are not consistent about which flows can receive refresh tokens ([OAuth2 clients][oauth2-clients], [OAuth2 Client Manager (REST)][client-manager-rest], [OAuth2 Client Manager (.NET)][client-manager-net]). For a Client Credentials client, request a new access token with the client secret when the old one expires.

### What Relativity does not document

- **The lifetime of a refresh token is not documented by Relativity.**
- **Rotation is not documented by Relativity.** It is not stated whether each use returns a new refresh token and invalidates the old one.
- **Revocation is not documented by Relativity.** No revocation endpoint is documented. The descriptions of deleting, disabling and resetting the secret of an OAuth2 Client do not say what happens to refresh tokens that are already issued ([OAuth2 clients][oauth2-clients], [OAuth2 Client Manager (REST)][client-manager-rest]).
- **The refresh call itself is inferred.** Relativity documents the token endpoint, `https://<host>/Relativity/Identity/connect/token` ([REST API authentication][rest-authentication]), but does not document a refresh request. By standard OAuth 2.0 practice, a refresh is a request to that same token endpoint with `grant_type=refresh_token`. This is **inferred**, and it is **not documented by Relativity**. The example reads the refresh token and the client secret from files, as in [Shell: the command line, secret files and the keychain](#shell-the-command-line-secret-files-and-the-keychain), so that neither appears on the command line. Each file holds only its value, written with `printf '%s'`, and is readable by its owner only:

```bash
curl --request POST "https://<host>/Relativity/Identity/connect/token" \
	--data-urlencode "grant_type=refresh_token" \
	--data-urlencode "refresh_token@<refresh-token-file>" \
	--data-urlencode "client_id=<client-id>" \
	--data-urlencode "client_secret@<client-secret-file>"
```

Confirm the request in a sandbox instance before you rely on it.

### Storing refresh tokens

A refresh token lets its holder obtain new access tokens without the user. Treat it with the same care as a client secret.

- Store it in a secret store (see [Reading secrets from a vault at runtime](#reading-secrets-from-a-vault-at-runtime)), or in encrypted server-side session storage, never in a source file, a plain configuration file, a log line or a URL.
- In a web application, keep it on the server and do not send it to a browser. For a desktop or command-line application, see [Desktop and command-line applications](#desktop-and-command-line-applications).
- Store one refresh token per user, and delete it when the user signs out or when the account is removed from the application.
- Because rotation is not documented, keep the latest refresh token that Relativity returned, and replace the stored value whenever a refresh response carries a new one.

### Desktop and command-line applications

A desktop or command-line application runs on the user's own machine, so the refresh token lives there and the application cannot keep anything from the person who runs it. **NSerio guidance:**

- **The refresh token.** Keep it in the operating system's credential store (the Windows Credential Manager, the macOS Keychain or a Linux secret service), or in a file that only its owner can read: `chmod 600` on Linux and macOS, and on Windows an access-control list that grants the file to that user alone. Never keep it in a plain configuration file, a log or the application's install folder.
- **The client secret.** Provision it for each installation and do not compile it into the application, put it in a resource file or ship it in the installer.
- **A distributed application cannot hide a secret.** A Code client requires a client secret, so an application that you distribute to users holds one, and anyone who has the application can extract it. It protects nothing against the person running the application. Reduce the harm instead: create one OAuth2 Client per distribution, so that a leaked secret can be disabled without affecting the others; set Trusted IPs where the users' outbound addresses are known and stable; use the recommended Access Token Lifetime of 60 minutes ([OAuth2 clients][oauth2-clients]); and turn refresh tokens on only when needed. The Authorization Code entries of Part C describe the OAuth2 Client to create for your language, and those for C#, VB.NET and PowerShell also describe the desktop variant with these compensating steps: Part C for [C#](part-c-csharp.md), [VB.NET](part-c-vbnet.md), [JavaScript](part-c-javascript.md), [TypeScript](part-c-typescript.md), [Python](part-c-python.md), [Go](part-c-go.md), [PowerShell](part-c-powershell.md) and [Shell](part-c-shell.md).

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

- [OAuth2 clients][oauth2-clients] (RelativityOne documentation): https://help.relativity.com/RelativityOne/Content/Relativity/Authentication/OAuth2_clients.htm. It supports the fields of the OAuth2 Client form, the statement that the flow cannot be changed when editing a client, the Context User rules and the Context User behaviour when the account is disabled, the PKCE sentence for refresh tokens, the UI steps for Reset Secret, the recommended Access Token Lifetime of 60 minutes, and the fact that a client can be enabled or disabled.
- [OAuth2 Client Manager (REST)][client-manager-rest]: https://platform.relativity.com/RelativityOne/Content/BD_Identity/OAuth2_Client_Manager__REST_.htm. It supports the `regenerate-secret` call, its empty request body, the statement that the flow is set during client creation and cannot be updated, and the deletion of a client through the API.
- [OAuth2 Client Manager (.NET)][client-manager-net]: https://platform.relativity.com/RelativityOne/Content/BD_Identity/OAuth2_Client_Manager__.NET_.htm. It supports `RegenerateSecretAsync`, the statement that all previous secrets are immediately invalidated, and the sentence about a reset taking effect immediately or with a specified delay.
- [What's new][whats-new] (Relativity developer news): https://platform.relativity.com/RelativityOne/Content/What_s_new/What_s_new.htm. It supports the change, dated 2025-09-25, that an OAuth2 Client's secret is shown once only.
- [REST API authentication][rest-authentication]: https://platform.relativity.com/RelativityOne/Content/REST_API/REST_API_authentication.htm. It supports the address of the token endpoint, `<host>/Relativity/Identity/connect/token`, and the description of the Context User as usually a member of the Relativity Administrators group, though it could be different depending on the API or operation invoked.
- [ISecretStore.Get][secretstore-get] and [ISecretStore.GetAsync][secretstore-getasync] (Relativity API reference), and the [ISecretStore][secretstore-interface] interface overview: https://platform.relativity.com/Server2021/Subsystems/apihelpers/Content/html/M_Relativity_API_ISecretStore_Get.htm, https://platform.relativity.com/Server2021/Subsystems/apihelpers/Content/html/M_Relativity_API_ISecretStore_GetAsync.htm and https://platform.relativity.com/Server2022/Subsystems/apihelpers/Content/html/T_Relativity_API_ISecretStore.htm. They support the two read members, the logical path that identifies a secret and the exceptions they throw.

The other sections are not Relativity statements. The Azure Key Vault, AWS Secrets Manager and HashiCorp Vault examples follow those vendors' own public SDK and API documentation. The C#, VB.NET and PowerShell sections follow Microsoft's public documentation of `ProtectedData`, user secrets, the SecretManagement module and `Export-Clixml`. The Shell section follows the `curl`, GNU Wget and `mksh` manuals and the behaviour of the shells NSerio checked. The JavaScript and TypeScript sections follow the general behaviour of browsers and of Node.js. The Python and Go sections follow the documentation of the Python and Go standard libraries and of the `keyring` package. The pre-commit hook follows the README and the hook definition of gitleaks.

[oauth2-clients]: https://help.relativity.com/RelativityOne/Content/Relativity/Authentication/OAuth2_clients.htm
[client-manager-rest]: https://platform.relativity.com/RelativityOne/Content/BD_Identity/OAuth2_Client_Manager__REST_.htm
[client-manager-net]: https://platform.relativity.com/RelativityOne/Content/BD_Identity/OAuth2_Client_Manager__.NET_.htm
[whats-new]: https://platform.relativity.com/RelativityOne/Content/What_s_new/What_s_new.htm
[rest-authentication]: https://platform.relativity.com/RelativityOne/Content/REST_API/REST_API_authentication.htm
[secretstore-get]: https://platform.relativity.com/Server2021/Subsystems/apihelpers/Content/html/M_Relativity_API_ISecretStore_Get.htm
[secretstore-getasync]: https://platform.relativity.com/Server2021/Subsystems/apihelpers/Content/html/M_Relativity_API_ISecretStore_GetAsync.htm
[secretstore-interface]: https://platform.relativity.com/Server2022/Subsystems/apihelpers/Content/html/T_Relativity_API_ISecretStore.htm
