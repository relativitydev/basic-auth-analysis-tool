# Release notes and What's next (User Guide, section 2 of 8)

These notes describe the current release of `migrate-auth`. They are updated with every release.

## Version 0.2.0-alpha

This is an **alpha** release. It is the third build of the first version of the tool: it adds PowerShell to C#, and it has the known issues listed below. The version string is `0.2.0-alpha`. The tool prints it with `--version`, in the session banner, and in the provenance of the report and of the JSON data file, beside the version of the detection catalog (`1.13.0` in this release).

Use an alpha release to assess repositories and to give feedback. Read every finding and recommendation before you act on it: the tool locates Basic Authentication and proposes a migration, and you decide.

### Changes from 0.1.1-alpha

- **PowerShell is analyzed.** The tool analyzes PowerShell scripts, with fourteen pattern classes, each described in [Part A of the Migration Documentation for PowerShell](../migration/part-a-powershell.md), with the constructs the catalog declares unsupported. The migration suggestion for each class and each flow is in [Part C for PowerShell](../migration/part-c-powershell.md). The report and the JSON data file name the class of a PowerShell finding with an identifier from `PS-001` to `PS-014`.
- **The C# catalog reads more forms.** It reads more of the ways a C# credential or client can be written, such as a type name written with its namespace in front of it, and more of the ways a header value can be built, such as a scheme held in a constant written with its type name in a header-enumeration write. [Part A for C#](../migration/part-a-csharp.md) lists the classes, and [Limits of the tool](03-limits.md#what-the-catalogs-declare-they-do-not-cover) lists the forms that are still not read.
- **The time limit on the tool's matching is 22.75 seconds, and the first-line check and the reading of strings and comments have a limit of 250 milliseconds.** See known issue 9.
- **A session saved by an earlier release is not resumed.** See known issue 6.

### What this release supports

- **Languages: C# and PowerShell.** The tool analyzes C# source files (`.cs`) and PowerShell files: `.ps1`, `.psm1` and `.psd1` files, and files without an extension whose first line names the PowerShell interpreter. A `.psd1` file is a data file and the PowerShell catalog declares no detection for its contents. Files in any other language, including VB.NET, JavaScript, TypeScript, Python, Go and Shell, are counted in the scan summary and are **not analyzed in this release. An empty result for those languages does not mean that no Basic Authentication was found.**
- **Detection.** Eighteen pattern classes for C#, described in [Part A for C#](../migration/part-a-csharp.md), and fourteen for PowerShell, described in [Part A for PowerShell](../migration/part-a-powershell.md), each with the constructs its catalog declares unsupported.
- **Measured accuracy.** The detection, flow-assignment and recommendation-coverage figures measured for this release, for C# and for PowerShell, with what they do and do not show, are in [What the figures cover](03-limits.md#what-the-figures-cover).
- **Both OAuth 2.0 flows.** Every High or Medium finding, in either language, is given a recommended flow, Client Credentials or Authorization Code, with a note that asks you to confirm the choice, and a migration recommendation for that flow. The recommendations are the ones in [Part C for C#](../migration/part-c-csharp.md) and [Part C for PowerShell](../migration/part-c-powershell.md). Where the new secrets go is in [Part B](../migration/part-b-secret-management.md).
- **The session, the four stages and the reports.** `scan`, `analyze`, `suggest` and `report`, a Markdown report and a JSON data file, and the configuration file. `ai-assist` is listed and is not available; it arrives in a later release (see [What's next](#whats-next)).
- **Platforms.** Windows (x64), Linux (x64), and macOS on Apple silicon (`osx-arm64`) and on Intel (`osx-x64`). The macOS binaries are plain binaries, **not signed with an Apple Developer ID and not notarized by Apple**; see known issue 3. The operating system versions the tool supports are in [Supported operating systems](01-using-the-tool.md#supported-operating-systems).

### Installing and running

The steps are in [Using the tool](01-using-the-tool.md): download the file for your platform and `SHA256SUMS`, [verify the checksum](01-using-the-tool.md#verifying-the-checksum), follow the [first-run steps](01-using-the-tool.md#first-run) for your platform (the Windows and macOS binaries are unsigned, so Windows SmartScreen asks you to confirm and macOS Gatekeeper asks you to allow the file), and start a session:

```text
migrate-auth --repositoryFolderPath <path to the repository> --documentationDestinationFolderPath <path to an output folder>
```

At the prompt run `scan`, `analyze`, `suggest` and `report` in that order.

### Known issues

The items below affect how the tool behaves or what its text says in this release.

1. **The Authorization Code endpoints are verified by NSerio and not documented by Relativity.** The authorize endpoint and the code exchange that the Authorization Code recommendations use were verified by NSerio against a live RelativityOne instance. Relativity does not document them, and its own confirmation is pending. The recommendations label them "verified by NSerio against a live instance; not documented by Relativity". PKCE is required by Relativity's documentation to obtain a refresh token; the `S256` method is NSerio's recommendation, because Relativity does not document which method it accepts.

2. **Relativity has not stated a recommendation for distributed desktop applications.** A desktop or command-line application that uses Authorization Code has to embed a client secret, and a secret shipped inside a distributed binary can be extracted by anyone who holds the binary. The desktop guidance in the recommendations is NSerio's guidance, verified to work; it is not a Relativity recommendation. Confirm it against your own security policy. The PowerShell recommendation for a desktop script follows the same guidance.

3. **The Windows and macOS binaries are unsigned.** Windows SmartScreen shows a warning the first time you run a downloaded copy. The macOS binaries are not signed with an Apple Developer ID certificate and not notarized by Apple, so macOS cannot vouch for them: Gatekeeper blocks the first run of a copy downloaded with a web browser until you allow it, and the checksum is the only check that your download is intact. The first-run steps for [Windows](01-using-the-tool.md#first-run-on-windows) and for [macOS](01-using-the-tool.md#first-run-on-macos) explain how to continue after you have verified the checksum. Signed, notarized macOS disk images are planned for a later release.

4. **There is no automated build and release pipeline yet (continuous integration and delivery is deferred).** The binaries of a release are built by hand. The release publishes a `SHA256SUMS` file. Verify the checksum of every download.

5. **The first launch of a freshly downloaded copy can take longer than one second.** In the measurement made for the previous release, the first launch of one freshly downloaded copy on Windows took 4.874 seconds; two other freshly downloaded copies, and three copies that were not marked as downloaded, started in about 0.17 seconds, and every second launch took about 0.03 seconds. The cause was not measured. The first launch on macOS has not been measured yet. There is no first-launch figure for Linux, because Linux does not run a reputation check on a downloaded binary.

6. **A session saved by another build is not resumed, and this includes every session saved by an earlier release.** The saved state of a session is resumed only when its state format version and its detection catalog version are both equal to those of the running tool. The detection catalog of this release is version `1.13.0`, and the earlier releases, `0.1.0-alpha` and `0.1.1-alpha`, used other catalog versions, so a session saved by either of them is not resumed. The tool says so, does not use it, and starts a fresh session: run `scan` again. The same happens with a state written by a newer build. The tool version itself is not compared, so two builds with the same versions of both can resume each other's sessions.

7. **File names in decomposed Unicode form are identified less strictly.** Two file names that differ only in their Unicode normalization form are reported as two files, and a file name in decomposed form (as macOS returns some names, and as a name can be on Windows NTFS or on a Linux file system, which keeps names byte for byte) may be positioned and identified differently from the same repository scanned on another platform. Detection within each file is not affected. On a repository that reaches the limit of 50,000 findings for one scan, the findings kept are the first ones in path order, and that order depends on the normalization form, so the findings kept can differ between platforms. See [Limits of the tool](03-limits.md#file-names-in-decomposed-unicode-form).

8. **The flow is chosen from signals in the finding's own file, one file at a time.** In real code the credential often reaches that file from another one, so most findings take the default flow with a note asking who supplies the credential. This holds for C# and for PowerShell. In a PowerShell script that reads a stored credential and falls back to a prompt for one when none is stored, both signals are in one file and the tool resolves that file to Authorization Code; see [Limits of the tool](03-limits.md#a-script-with-both-a-stored-credential-and-a-prompt). Pointing to interactive-user evidence found elsewhere in the same repository is planned after the alpha.

9. **The time limits on the tool's matching are provisional.** The tool bounds the time it spends evaluating each of its own search expressions on a file, currently 22.75 seconds, and the time it spends on the first-line check of a file and on reading the strings and comments of a file, currently 250 milliseconds for each. Each was chosen with a tenfold margin over the slowest benign case measured on Windows and on a Linux virtual machine, and neither has been confirmed on macOS or on other hardware. What the user sees when a limit is reached depends on which one:
   - **The 22.75-second limit.** The tool stops analyzing the file and discards **every** finding for that file, and the report counts the file under "Files whose analysis took too long and was stopped; their findings are not reported". Review such a file by hand.
   - **The 250-millisecond limit on the first-line check, during `scan`.** The tool does not use the first line to recognize the file's language. It identifies the file by its extension instead; a file without an extension then counts as a file whose language was not recognized, so a PowerShell script that has no extension is not analyzed. The report says how many files were identified by their extension for this reason ("Files attributed by extension because first-line detection exceeded its budget"), and the JSON data file carries the count as `firstLineFallbackFiles`.
   - **The 250-millisecond limit on reading strings and comments, during `analyze`.** The tool stops reading the file as code at the point where the limit was reached, keeps the findings before that point, and reports **no** finding at or after it. The report counts the file under "Files only partly readable as code, so not analyzed in full". Review the rest of that file by hand.

10. **A file in a single-byte code page other than Windows-1252 can be read with the wrong characters.** The tool reads a source file as UTF-8, UTF-16 or UTF-32 when it starts with a byte order mark, and as UTF-8 or UTF-16 when it does not. A file that is none of these is read as Windows-1252, which covers Latin-1 and the other Western European files; the report counts such files and names them under "Files read as Windows-1252" (`scanSummary.legacyEncodedFiles` in the JSON file). The tool cannot tell which single-byte code page a file was written in. A file in another one, for example a Cyrillic or Central European code page, is read with its accented or other non-ASCII characters changed, or, when it holds a byte that Windows-1252 leaves undefined, is counted under "Files that could not be read as text" and is not analyzed. A credential written in plain ASCII is found either way; one whose value holds a non-ASCII character is read with the wrong character. A UTF-8 file with a damaged byte sequence is also read as Windows-1252. This applies to C# files and to PowerShell scripts alike. Convert such files to UTF-8 and scan again, or review them by hand.

11. **One construct can be reported as two findings at the same place.** A Basic `Authorization` header built from a user name and a password that are encoded where the header is written is reported once as the header (`CS-005`, or `CS-006` when the header name is written as a string) and once as the encoded pair (`CS-009`), on the same line or a few lines apart. Both findings belong to one migration, and both locations need to be fixed together. This is not the case for a credential cache entry, which is one finding, nor for an authentication helper (`CS-011`): the helper's definition is one finding and the credential it builds, when the tool recognizes it, is another, at its own line. A PowerShell header built the same way is reported as the encoded pair (`PS-010`) and the header write (`PS-005`), at two locations that need the same attention.

12. **For the legacy Import API construction (class `CS-015`), no documented factory accepts a token that a standalone or desktop process acquired itself.** The recommendation for this class names the one documented factory, `ImportAPI.CreateByRsaBearerToken(webServiceUrl)`. It takes no token, and Relativity documents it for processes hosted by the Relativity service account, for example an agent. Relativity's Import API concepts page lists bearer-token authentication as not supported for a desktop application. The recommendation therefore gives no code form for a standalone or desktop process that would obtain an OAuth2 token itself, and says so. Both pages are Relativity Server documentation, and no RelativityOne page for this legacy class was found, so whether the factory exists in an SDK version aimed at RelativityOne is not established. The sources are [`ImportAPI.CreateByRsaBearerToken`](https://platform.relativity.com/Server2021/Subsystems/importapi/Content/html/M_kCura_Relativity_ImportAPI_ImportAPI_CreateByRsaBearerToken.htm) and [Import API concepts](https://platform.relativity.com/Server2025/Content/Import_API/Overview/Import_API_concepts.htm). The entry for this class in [Part C](../migration/part-c-csharp.md#cs-015-migration) says the same.

13. **The PowerShell recommendations were run only against a local test server, not against RelativityOne.** The scripts in the PowerShell migration suggestions that acquire a token and call the API were run on Windows PowerShell 5.1 and on PowerShell 7 against a local server that imitates the token endpoints and the API of RelativityOne. They were not run against a RelativityOne instance. Every statement the scripts make about how RelativityOne behaves rests on Relativity's documentation and on NSerio's earlier verification, not on this run, so the local run shows that the scripts work as written and not that RelativityOne accepts them. Try each script against a test instance before you rely on it. The C# recommendations were not run against RelativityOne either. The desktop script, which opens a local listener to receive the sign-in redirect, was run from a process that was not elevated, under an account that is a local administrator; it has not been tried under a standard user account.

14. **In PowerShell, a password written as a quoted literal through `ConvertTo-SecureString` with `-AsPlainText` does not raise the literal-credential warning.** When such a secure string is wrapped in a credential object and passed to `-Credential`, the finding is reported at its level with no warning that a credential is written in the script. A literal written straight into `New-Object System.Net.NetworkCredential(...)` or into a Basic `Authorization` header does raise it. If you use the warning to find the secrets to rotate, search the PowerShell files by hand for `ConvertTo-SecureString` used with `-AsPlainText`. See [Limits of the tool](03-limits.md#the-literal-credential-warning-is-read-from-where-the-credential-sits) and [Part A for PowerShell](../migration/part-a-powershell.md#declared-unsupported-coverage).

### How to report an issue

Report an issue through the channel this release was distributed with, the place where you downloaded it. Include the version string from `migrate-auth --version`, your platform, the command you ran, and what you expected. **Do not attach a report or a JSON data file to an issue without reading it first:** they contain no credential values, but they list where credentials are in your repository. Never paste a credential.

## What's next

This release is the base, C# and PowerShell. The following releases add one language each and then the last feature, in this order. None has a date.

| Order | Adds |
| --- | --- |
| 1 | VB.NET |
| 2 | Shell |
| 3 | JavaScript |
| 4 | TypeScript |
| 5 | Python |
| 6 | Go |
| 7 | AI-assisted fallback (`ai-assist`) |

Until a language is delivered, its files are counted and are not analyzed. Each release updates these release notes.

Also planned: code examples for token handling in the C# recommendations. In this release the entries in [Part C for C#](../migration/part-c-csharp.md#migration-suggestions-by-pattern-class) describe how to acquire a token, how to cache it and how to handle its expiry in words, and contain no token-handling code. The PowerShell recommendations carry scripts that acquire the token and call the API; how to cache the token and how to handle its expiry or refresh are described in words, as for C#, and the scripts do not implement them.

## Version 0.1.1-alpha

The previous release, `0.1.1-alpha`, is kept here as the history of the tool. Its catalog was version `1.6.0`, it supported C# only, and the time limit on its matching was 5.25 seconds.

### Changes from 0.1.0-alpha

- **A key in the configuration file with a value that is not valid no longer discards the rest of the file.** Before, a `defaultFlow` such as `"Client Credentials"` (instead of `ClientCredentials`) in the user profile file made the tool use the built-in defaults for every setting, so the report was written to the folder the tool was started from instead of the configured `outputPath`. Now only that key is ignored, as if it were absent; a warning at start-up names the key (and the value, when it is text), and the other settings in the file still apply. A file that is not valid JSON, is not a JSON object or is too large is still reported at start-up and not applied: each of its settings is taken from the next source. See [The configuration file](01-using-the-tool.md#the-configuration-file).
