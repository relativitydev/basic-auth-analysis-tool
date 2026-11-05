# migrate-auth

`migrate-auth` is a command-line tool that finds Basic Authentication in the source code of a repository and tells you how to move it to OAuth 2.0 for RelativityOne. It runs offline, reads your code, and writes a report. It never edits your code, never contacts RelativityOne or any other network service, and never creates or changes an OAuth2 Client.

**Status: alpha.** This is version `0.3.0-alpha`, with detection catalog `1.19.0`. It analyzes **C#, PowerShell and VB.NET**. Files in other languages are counted and are not analyzed, and an empty result for them does not mean that no Basic Authentication was found. Read the [release notes](user-guide/02-release-notes.md) for the known issues.

## Download and verify

A release provides one file for each platform and a `SHA256SUMS` file that lists the SHA-256 checksum of each file:

- Windows (x64): the `win-x64` binary, `migrate-auth-0.3.0-alpha-win-x64.exe`.
- Linux (x64): the `linux-x64` binary, `migrate-auth-0.3.0-alpha-linux-x64`.
- macOS on Apple silicon: the `osx-arm64` binary, `migrate-auth-0.3.0-alpha-osx-arm64`.
- macOS on Intel: the `osx-x64` binary, `migrate-auth-0.3.0-alpha-osx-x64`.

Download the file for your platform and `SHA256SUMS` into the same folder, and check the checksum before you run anything. The commands for Windows, Linux and macOS, and the first-run steps for each, are in [Using the tool](user-guide/01-using-the-tool.md#installing). In this release the Windows binary is not signed, and the macOS binaries are not signed with an Apple Developer ID and not notarized by Apple. Windows SmartScreen therefore asks you to confirm, and macOS Gatekeeper blocks a downloaded macOS binary until you allow it. The checksum is how you check that your download is intact.

## Quick start

The example assesses a repository at `C:\src\example-integration` on Windows, or `~/src/example-integration` on Linux and macOS, and writes its output to a separate folder. The output folder must not be the repository or inside it. Run the command from the folder that holds the downloaded file, under the name it was downloaded with.

On Windows, in PowerShell, start the file with `.\`, because PowerShell does not run a program from the current folder by name alone:

```powershell
.\migrate-auth-0.3.0-alpha-win-x64.exe --repositoryFolderPath C:\src\example-integration --documentationDestinationFolderPath C:\src\example-assessment
```

On Linux, make the file executable once (`chmod +x`), then run it:

```bash
chmod +x ./migrate-auth-0.3.0-alpha-linux-x64
./migrate-auth-0.3.0-alpha-linux-x64 --repositoryFolderPath ~/src/example-integration --documentationDestinationFolderPath ~/src/example-assessment
```

On macOS, in Terminal, make the file executable once (`chmod +x`), then run it. The example uses the Apple silicon binary; on Intel, use `migrate-auth-0.3.0-alpha-osx-x64`. The first time, macOS blocks a downloaded file that is not signed with a Developer ID or notarized; the steps to allow it are in [First run on macOS](user-guide/01-using-the-tool.md#first-run-on-macos).

```bash
chmod +x ./migrate-auth-0.3.0-alpha-osx-arm64
./migrate-auth-0.3.0-alpha-osx-arm64 --repositoryFolderPath ~/src/example-integration --documentationDestinationFolderPath ~/src/example-assessment
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
- [Migration Documentation](migration/part-a-csharp.md): the rules behind each finding. Part A describes each pattern the tool looks for and how to search for it yourself: [C#](migration/part-a-csharp.md), [PowerShell](migration/part-a-powershell.md) and [VB.NET](migration/part-a-vbnet.md). [Part B](migration/part-b-secret-management.md) covers where the new secrets go. Part C gives the migration suggestion for each pattern and each flow: [C#](migration/part-c-csharp.md), [PowerShell](migration/part-c-powershell.md) and [VB.NET](migration/part-c-vbnet.md).

## What is next

This alpha supports C#, PowerShell and VB.NET. Later releases add one language each: Shell, JavaScript, TypeScript, Python and Go, and then an optional AI-assisted review. They have no dates. See [What's next](user-guide/02-release-notes.md#whats-next).
