# migrate-auth

**Contents**

- [Documentation](#documentation)
	- [User Guide](#user-guide)
	- [Migration Documentation](#migration-documentation)
- [Download and verify](#download-and-verify)
- [Quick start](#quick-start)
- [Build from source](#build-from-source)

This repository offers two ways to find Basic Authentication in source code and move it to OAuth 2.0 for RelativityOne. You can **run the tool**: `migrate-auth` is a command-line tool that reads the source code of a repository and writes a report. It runs offline, never edits your code, never contacts RelativityOne or any other network service, and never creates or changes an OAuth2 Client. Or you can **follow the Migration Documentation by hand**, without running anything: it gives, for each of eight languages, the expressions to search your code with and the change to make for each match. Start with the [Migration Documentation overview](docs/public/migration/README.md).

This is version `1.0.0`, with detection catalog `1.51.2`. The tool analyzes **C#, PowerShell, VB.NET, Shell, JavaScript, TypeScript, Python and Go**, and it includes the optional `ai-assist` command, which prepares a prompt pack for an AI-assisted review of the findings and, only when you confirm, starts your own AI command-line tool against it. The launch completes a review with each of the three tools it starts, each signed in to an account, on Windows, Linux and macOS on Intel. Files in other languages are counted and are not analyzed, and an empty result for them does not mean that no Basic Authentication was found. A finding in test code is reported at Medium at most; test code that is also a mock, a sample, vendored or generated stays Low. Two forms in JavaScript and TypeScript are declared as not detected, and in C# a credential type of another library that has the same name as the Relativity one is still reported at High. Assess again a repository you assessed with an earlier release: how some languages are read has changed. Read the [release notes](docs/public/user-guide/02-release-notes.md) for the changes and the known issues.

## Documentation

This is the index of every document. Each document opens with a link back to this index and, when it has sections, a table of contents.

- [Migration Documentation](docs/public/migration/README.md): how to find Basic Authentication in your code and move it to OAuth 2.0 by hand, with no tool needed. Part A gives, for each of eight languages, the patterns to look for and how to search for them yourself, Part C gives the migration suggestion for each pattern and each OAuth 2.0 flow, and [Part B](docs/public/migration/part-b-secret-management.md) covers where the new secrets go. The overview opens with the three parts and the order to use them.
- [User Guide](docs/public/user-guide/index.md): setting up, running and configuring the tool, the optional `ai-assist` step, reading the report, its limits, and the OAuth 2.0 background. The guide covers C#, PowerShell, VB.NET, Shell, JavaScript, TypeScript, Python, Go and `ai-assist`.

### User Guide

| Section | What it covers |
| --- | --- |
| [User Guide](docs/public/user-guide/index.md) | The guide's contents. |
| [1. Using the tool](docs/public/user-guide/01-using-the-tool.md) | What the tool does and does not do, setting it up and verifying it, the session, the commands, the configuration file, reading the report, choosing and confirming the flow, where outputs go, and exit codes. |
| [2. Release notes](docs/public/user-guide/02-release-notes.md) | What changed in this release, what it supports, known issues, and how to report an issue. |
| [3. Limits of the tool](docs/public/user-guide/03-limits.md) | Supported languages, what is not covered, the accuracy figures, and what a result does and does not mean. |
| [4. OAuth 2.0 essentials](docs/public/user-guide/04-oauth2-essentials.md) | Why Basic Authentication is being replaced, Bearer tokens, and the two flows. |
| [5. Configuring the OAuth2 Client](docs/public/user-guide/05-configuring-the-oauth2-client.md) | The OAuth2 Client fields for each flow. |
| [6. Secrets](docs/public/user-guide/06-secrets.md) | What the new secrets are and the basic rules. |
| [7. Token expiry](docs/public/user-guide/07-token-expiry.md) | The token lifetime and how each flow handles expiry. |
| [8. More information from Relativity](docs/public/user-guide/08-more-from-relativity.md) | Links to Relativity's official documentation. |

### Migration Documentation

The [Migration Documentation overview](docs/public/migration/README.md) opens with the three parts and the order to use them. [Part B: Secret Management](docs/public/migration/part-b-secret-management.md) is shared by all languages.

| Language | Part A: find it | Part C: change it |
| --- | --- | --- |
| C# | [Rule patterns for C#](docs/public/migration/part-a-csharp.md) | [Migration suggestions for C#](docs/public/migration/part-c-csharp.md) |
| VB.NET | [Rule patterns for VB.NET](docs/public/migration/part-a-vbnet.md) | [Migration suggestions for VB.NET](docs/public/migration/part-c-vbnet.md) |
| JavaScript | [Rule patterns for JavaScript](docs/public/migration/part-a-javascript.md) | [Migration suggestions for JavaScript](docs/public/migration/part-c-javascript.md) |
| TypeScript | [Rule patterns for TypeScript](docs/public/migration/part-a-typescript.md) | [Migration suggestions for TypeScript](docs/public/migration/part-c-typescript.md) |
| Python | [Rule patterns for Python](docs/public/migration/part-a-python.md) | [Migration suggestions for Python](docs/public/migration/part-c-python.md) |
| Go | [Rule patterns for Go](docs/public/migration/part-a-go.md) | [Migration suggestions for Go](docs/public/migration/part-c-go.md) |
| PowerShell | [Rule patterns for PowerShell](docs/public/migration/part-a-powershell.md) | [Migration suggestions for PowerShell](docs/public/migration/part-c-powershell.md) |
| Shell | [Rule patterns for Shell](docs/public/migration/part-a-shell.md) | [Migration suggestions for Shell](docs/public/migration/part-c-shell.md) |

## Download and verify

A release provides one file for each platform and a `SHA256SUMS` file that lists the SHA-256 checksum of each file:

- Windows (x64): the `win-x64` binary, `migrate-auth-1.0.0-win-x64.exe`.
- Linux (x64): the `linux-x64` binary, `migrate-auth-1.0.0-linux-x64`.
- macOS on Apple silicon: the `osx-arm64` binary, `migrate-auth-1.0.0-osx-arm64`.
- macOS on Intel: the `osx-x64` binary, `migrate-auth-1.0.0-osx-x64`.

Download the file for your platform and `SHA256SUMS` into the same folder, and check the checksum before you run anything. The commands for Windows, Linux and macOS, and the first-run steps for each, are in [Using the tool](docs/public/user-guide/01-using-the-tool.md#setting-up). In this release the Windows binary is not signed, and the macOS binaries are not signed with an Apple Developer ID and not notarized by Apple. Windows SmartScreen therefore asks you to confirm, and macOS Gatekeeper blocks a downloaded macOS binary until you allow it. The checksum is how you check that your download is intact.

## Quick start

The example assesses a repository at `C:\src\example-integration` on Windows, or `~/src/example-integration` on Linux and macOS, and writes its output to a separate folder. The output folder must not be the repository or inside it. Run the command from the folder that holds the downloaded file, under the name it was downloaded with.

On Windows, in PowerShell, start the file with `.\`, because PowerShell does not run a program from the current folder by name alone:

```powershell
.\migrate-auth-1.0.0-win-x64.exe --repositoryFolderPath C:\src\example-integration --documentationDestinationFolderPath C:\src\example-assessment
```

On Linux, make the file executable once (`chmod +x`), then run it:

```bash
chmod +x ./migrate-auth-1.0.0-linux-x64
./migrate-auth-1.0.0-linux-x64 --repositoryFolderPath ~/src/example-integration --documentationDestinationFolderPath ~/src/example-assessment
```

On macOS, in Terminal, make the file executable once (`chmod +x`), then run it. The example uses the Apple silicon binary; on Intel, use `migrate-auth-1.0.0-osx-x64`. The first time, macOS blocks a downloaded file that is not signed with a Developer ID or notarized; the steps to allow it are in [First run on macOS](docs/public/user-guide/01-using-the-tool.md#first-run-on-macos).

```bash
chmod +x ./migrate-auth-1.0.0-osx-arm64
./migrate-auth-1.0.0-osx-arm64 --repositoryFolderPath ~/src/example-integration --documentationDestinationFolderPath ~/src/example-assessment
```

If you rename the file to `migrate-auth.exe` or `migrate-auth`, use that name in the same way, and keep the `.\` or `./` prefix unless the folder is in your `PATH`.

The tool starts an interactive session and shows the prompt `migrate-auth>`. Run the four stages in order:

```text
scan
analyze
suggest
report
exit
```

`report` writes `basic-auth-migration-YYYY-MM-DD.md` and `basic-auth-migration-YYYY-MM-DD.json` in the output folder. Open the Markdown report first. It lists the findings with a confidence level, a recommended OAuth 2.0 flow, and a migration recommendation for each. `help` lists every command.

The report locates credential material in your repository. It contains no credential values, but handle it and share it accordingly.

## Build from source

Building the tool or running its tests needs the .NET SDK that `global.json` names, version `10.0.403`. When that exact version is not installed, `dotnet` uses a later installed patch of the same feature band, such as `10.0.404`; it does not build with an earlier patch, such as `10.0.402`, or with an SDK from another band.

Running the tests also needs Python 3. Before `dotnet test`, run `python tests/FakeAgent/make-sentinels.py --output <a folder outside the repository>` (on macOS add `--rid osx-arm64` or `--rid osx-x64`, as `uname -m` says): it builds a stand-in agent and stand-ins named `claude`, `codex` and `copilot`, and prints the folder of the stand-ins as its last line. Put that folder first on your `PATH` for every test command, so that no test can start a real AI tool; the agent tests fail without it. On Linux, two tests also need `strace` and unprivileged user namespaces.
