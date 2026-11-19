# migrate-auth

This repository offers two ways to find Basic Authentication in source code and move it to OAuth 2.0 for RelativityOne. You can **run the tool**: `migrate-auth` is a command-line tool that reads the source code of a repository and writes a report. It runs offline, never edits your code, never contacts RelativityOne or any other network service, and never creates or changes an OAuth2 Client. Or you can **follow the Migration Documentation by hand**, without running anything: it gives, for each of eight languages, the expressions to search your code with and the change to make for each match. Start with the [Migration Documentation overview](docs/public/migration/README.md).

**Status: alpha.** This is version `0.9.2-alpha`, with detection catalog `1.51.2`. The tool analyzes **C#, PowerShell, VB.NET, Shell, JavaScript, TypeScript, Python and Go**, and it includes the optional `ai-assist` command, which prepares a prompt pack for an AI-assisted review of the findings and, only when you confirm, starts your own AI command-line tool against it. Files in other languages are counted and are not analyzed, and an empty result for them does not mean that no Basic Authentication was found. A finding in test code is reported at Medium at most; test code that is also a mock, a sample, vendored or generated stays Low. Two forms in JavaScript and TypeScript are declared as not detected, and in C# a credential type of another library that has the same name as the Relativity one is still reported at High. The launch of `ai-assist` was run with three real tools on Windows, Linux and macOS on Intel, and each completed a review. Assess again a repository you assessed with an earlier release: how some languages are read has changed. Read the [release notes](docs/public/user-guide/02-release-notes.md) for the changes and the known issues.

## Documentation

- [Migration Documentation](docs/public/migration/README.md): how to find Basic Authentication in your code and move it to OAuth 2.0 by hand, with no tool needed. Part A gives, for each of eight languages, the patterns to look for and how to search for them yourself, Part C gives the migration suggestion for each pattern and each OAuth 2.0 flow, and [Part B](docs/public/migration/part-b-secret-management.md) covers where the new secrets go. The overview opens with the three parts and the order to use them.
- [User Guide](docs/public/user-guide/index.md): installing, running and configuring the tool, the optional `ai-assist` step, reading the report, its limits, and the OAuth 2.0 background. The guide covers C#, PowerShell, VB.NET, Shell, JavaScript, TypeScript, Python, Go and `ai-assist`.

## Download and verify

A release provides one file for each platform and a `SHA256SUMS` file that lists the SHA-256 checksum of each file:

- Windows (x64): the `win-x64` binary, `migrate-auth-0.9.2-alpha-win-x64.exe`.
- Linux (x64): the `linux-x64` binary, `migrate-auth-0.9.2-alpha-linux-x64`.
- macOS on Apple silicon: the `osx-arm64` binary, `migrate-auth-0.9.2-alpha-osx-arm64`.
- macOS on Intel: the `osx-x64` binary, `migrate-auth-0.9.2-alpha-osx-x64`.

Download the file for your platform and `SHA256SUMS` into the same folder, and check the checksum before you run anything. The commands for Windows, Linux and macOS, and the first-run steps for each, are in [Using the tool](docs/public/user-guide/01-using-the-tool.md#installing). In this release the Windows binary is not signed, and the macOS binaries are not signed with an Apple Developer ID and not notarized by Apple. Windows SmartScreen therefore asks you to confirm, and macOS Gatekeeper blocks a downloaded macOS binary until you allow it. The checksum is how you check that your download is intact.

## Quick start

The example assesses a repository at `C:\src\example-integration` on Windows, or `~/src/example-integration` on Linux and macOS, and writes its output to a separate folder. The output folder must not be the repository or inside it. Run the command from the folder that holds the downloaded file, under the name it was downloaded with.

On Windows, in PowerShell, start the file with `.\`, because PowerShell does not run a program from the current folder by name alone:

```powershell
.\migrate-auth-0.9.2-alpha-win-x64.exe --repositoryFolderPath C:\src\example-integration --documentationDestinationFolderPath C:\src\example-assessment
```

On Linux, make the file executable once (`chmod +x`), then run it:

```bash
chmod +x ./migrate-auth-0.9.2-alpha-linux-x64
./migrate-auth-0.9.2-alpha-linux-x64 --repositoryFolderPath ~/src/example-integration --documentationDestinationFolderPath ~/src/example-assessment
```

On macOS, in Terminal, make the file executable once (`chmod +x`), then run it. The example uses the Apple silicon binary; on Intel, use `migrate-auth-0.9.2-alpha-osx-x64`. The first time, macOS blocks a downloaded file that is not signed with a Developer ID or notarized; the steps to allow it are in [First run on macOS](docs/public/user-guide/01-using-the-tool.md#first-run-on-macos).

```bash
chmod +x ./migrate-auth-0.9.2-alpha-osx-arm64
./migrate-auth-0.9.2-alpha-osx-arm64 --repositoryFolderPath ~/src/example-integration --documentationDestinationFolderPath ~/src/example-assessment
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

Building the tool or running its tests needs the .NET SDK that `global.json` names, version `10.0.403`. When that exact version is not installed, `dotnet` uses a later installed patch of the same feature band, such as `10.0.404`; it does not build with an earlier patch, such as `10.0.402`, or with an SDK from another band. The release scripts, `scripts/build-release.sh` on Linux and macOS and `scripts/build-release.ps1` on Windows, stop unless the SDK in use is exactly `10.0.403`.

Running the tests also needs Python 3. Before `dotnet test`, run `python tests/FakeAgent/make-sentinels.py --output <a folder outside the repository>` (on macOS add `--rid osx-arm64` or `--rid osx-x64`, as `uname -m` says): it builds a stand-in agent and stand-ins named `claude`, `codex` and `copilot`, and prints the folder of the stand-ins as its last line. Put that folder first on your `PATH` for every test command, so that no test can start a real AI tool; the agent tests fail without it. On Linux, two tests also need `strace` and unprivileged user namespaces.

## What is next

This alpha supports C#, PowerShell, VB.NET, Shell, JavaScript, TypeScript, Python and Go, with the optional `ai-assist` command. All eight languages are analyzed, and none is waiting for a later release. See [What's next](docs/public/user-guide/02-release-notes.md#whats-next).
