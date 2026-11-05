# migrate-auth

`migrate-auth` is a command-line tool that finds Basic Authentication in the source code of a repository and tells you how to move it to OAuth 2.0 for RelativityOne. It runs offline, reads your code, and writes a report. It never edits your code, never contacts RelativityOne or any other network service, and never creates or changes an OAuth2 Client.

**Status: alpha.** This is version `0.6.1-alpha`, with detection catalog `1.45.1`. It analyzes **C#, PowerShell, VB.NET, Shell, JavaScript and TypeScript**. Files in other languages are counted and are not analyzed, and an empty result for them does not mean that no Basic Authentication was found. A finding in test code is reported at Medium at most; test code that is also a mock, a sample, vendored or generated stays Low. This release also changes what the tool reports compared with `0.6.0-alpha`. In C# it reports a call of the Relativity test helper `GetProxy` that passes a user name and a password and the `Rsapi` user-name and password pair of a settings object, it reports a credential stored in a member and handed to a mail, proxy or file-transfer client at Low instead of Medium, it reports a colon-joined pair whose halves are not named like a credential at Low instead of Medium, and it reports code in a block that the file's own conditional-compilation symbols switch off at Low. In JavaScript and TypeScript it reports an `Authorization` header whose value is read from a settings property at Low. A genuine pair with neutral names, and a genuine use under a symbol that only the project file defines, are also reported at Low and need a review; a repository you assessed with an earlier release should be assessed again. Two forms in JavaScript and TypeScript are declared as not detected, and in C# a credential type of another library that has the same name as the Relativity one is still reported at High. Read the [release notes](user-guide/02-release-notes.md) for the changes and the known issues.

## Download and verify

A release provides one file for each platform and a `SHA256SUMS` file that lists the SHA-256 checksum of each file:

- Windows (x64): the `win-x64` binary, `migrate-auth-0.6.1-alpha-win-x64.exe`.
- Linux (x64): the `linux-x64` binary, `migrate-auth-0.6.1-alpha-linux-x64`.
- macOS on Apple silicon: the `osx-arm64` binary, `migrate-auth-0.6.1-alpha-osx-arm64`.
- macOS on Intel: the `osx-x64` binary, `migrate-auth-0.6.1-alpha-osx-x64`.

Download the file for your platform and `SHA256SUMS` into the same folder, and check the checksum before you run anything. The commands for Windows, Linux and macOS, and the first-run steps for each, are in [Using the tool](user-guide/01-using-the-tool.md#installing). In this release the Windows binary is not signed, and the macOS binaries are not signed with an Apple Developer ID and not notarized by Apple. Windows SmartScreen therefore asks you to confirm, and macOS Gatekeeper blocks a downloaded macOS binary until you allow it. The checksum is how you check that your download is intact.

## Quick start

The example assesses a repository at `C:\src\example-integration` on Windows, or `~/src/example-integration` on Linux and macOS, and writes its output to a separate folder. The output folder must not be the repository or inside it. Run the command from the folder that holds the downloaded file, under the name it was downloaded with.

On Windows, in PowerShell, start the file with `.\`, because PowerShell does not run a program from the current folder by name alone:

```powershell
.\migrate-auth-0.6.1-alpha-win-x64.exe --repositoryFolderPath C:\src\example-integration --documentationDestinationFolderPath C:\src\example-assessment
```

On Linux, make the file executable once (`chmod +x`), then run it:

```bash
chmod +x ./migrate-auth-0.6.1-alpha-linux-x64
./migrate-auth-0.6.1-alpha-linux-x64 --repositoryFolderPath ~/src/example-integration --documentationDestinationFolderPath ~/src/example-assessment
```

On macOS, in Terminal, make the file executable once (`chmod +x`), then run it. The example uses the Apple silicon binary; on Intel, use `migrate-auth-0.6.1-alpha-osx-x64`. The first time, macOS blocks a downloaded file that is not signed with a Developer ID or notarized; the steps to allow it are in [First run on macOS](user-guide/01-using-the-tool.md#first-run-on-macos).

```bash
chmod +x ./migrate-auth-0.6.1-alpha-osx-arm64
./migrate-auth-0.6.1-alpha-osx-arm64 --repositoryFolderPath ~/src/example-integration --documentationDestinationFolderPath ~/src/example-assessment
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

## Documentation

- [User Guide](user-guide/index.md): installing, running and configuring the tool, reading the report, its limits, and the OAuth 2.0 background.
- [Migration Documentation](migration/part-a-csharp.md): the rules behind each finding. Part A describes each pattern the tool looks for and how to search for it yourself: [C#](migration/part-a-csharp.md), [PowerShell](migration/part-a-powershell.md), [VB.NET](migration/part-a-vbnet.md), [Shell](migration/part-a-shell.md), [JavaScript](migration/part-a-javascript.md) and [TypeScript](migration/part-a-typescript.md). [Part B](migration/part-b-secret-management.md) covers where the new secrets go. Part C gives the migration suggestion for each pattern and each flow: [C#](migration/part-c-csharp.md), [PowerShell](migration/part-c-powershell.md), [VB.NET](migration/part-c-vbnet.md), [Shell](migration/part-c-shell.md), [JavaScript](migration/part-c-javascript.md) and [TypeScript](migration/part-c-typescript.md).

## What is next

This alpha supports C#, PowerShell, VB.NET, Shell, JavaScript and TypeScript. Later releases add one language each: Python and Go, and then an optional AI-assisted review. They have no dates. See [What's next](user-guide/02-release-notes.md#whats-next).
