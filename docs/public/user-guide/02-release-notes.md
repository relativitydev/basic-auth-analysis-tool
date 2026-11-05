# Release notes and What's next (User Guide, section 2 of 8)

These notes describe the current release of `migrate-auth`. They are updated with every release.

## Version 0.1.1-alpha

This is an **alpha** release. It is the second build of the first version of the tool, it supports C# only, and it has the known issues listed below. The version string is `0.1.1-alpha`. The tool prints it with `--version`, in the session banner, and in the provenance of the report and of the JSON data file, beside the version of the detection catalog (`1.6.0` in this release).

Use an alpha release to assess repositories and to give feedback. Read every finding and recommendation before you act on it: the tool locates Basic Authentication and proposes a migration, and you decide.

### Changes from 0.1.0-alpha

- **A key in the configuration file with a value that is not valid no longer discards the rest of the file.** Before, a `defaultFlow` such as `"Client Credentials"` (instead of `ClientCredentials`) in the user profile file made the tool use the built-in defaults for every setting, so the report was written to the folder the tool was started from instead of the configured `outputPath`. Now only that key is ignored, as if it were absent; a warning at start-up names the key and its value, and the other settings in the file still apply. A file that is not valid JSON, is not a JSON object or is too large is still reported as not having the expected structure, and the built-in defaults are used for that file. See [The configuration file](01-using-the-tool.md#the-configuration-file).

### What this release supports

- **Language: C# only.** The tool analyzes C# source files (`.cs`). Files in any other language, including VB.NET, JavaScript, TypeScript, Python, Go, PowerShell and Shell, are counted in the scan summary and are **not analyzed in this release. An empty result for those languages does not mean that no Basic Authentication was found.**
- **Detection.** Eighteen pattern classes for C#, each described in [Part A of the Migration Documentation](../migration/part-a-csharp.md), with the constructs the catalog declares unsupported.
- **Both OAuth 2.0 flows.** Every High or Medium finding is given a recommended flow, Client Credentials or Authorization Code, with a note that asks you to confirm the choice, and a migration recommendation for that flow. The recommendations are the ones in [Part C](../migration/part-c-csharp.md).
- **The session, the four stages and the reports.** `scan`, `analyze`, `suggest` and `report`, a Markdown report and a JSON data file, and the configuration file. `ai-assist` is listed and is not available; it arrives with M8.
- **Platforms.** Windows (x64), Linux (x64), and macOS on Apple silicon (`osx-arm64`) and on Intel (`osx-x64`). The macOS binaries are plain binaries, **not signed with an Apple Developer ID and not notarized by Apple**; see known issue 3. The `osx-arm64` binary was built on an Intel Mac and **has not been run on Apple silicon hardware in this release**.

### Installing and running

The steps are in [Using the tool](01-using-the-tool.md): download the file for your platform and `SHA256SUMS`, [verify the checksum](01-using-the-tool.md#verifying-the-checksum), follow the [first-run steps](01-using-the-tool.md#first-run) for your platform (the Windows and macOS binaries are unsigned, so Windows SmartScreen asks you to confirm and macOS Gatekeeper asks you to allow the file), and start a session:

```text
migrate-auth --repositoryFolderPath <path to the repository> --documentationDestinationFolderPath <path to an output folder>
```

At the prompt run `scan`, `analyze`, `suggest` and `report` in that order.

### Known issues

The items below affect how the tool behaves or what its text says in this release.

1. **The Authorization Code endpoints are verified by NSerio and not documented by Relativity.** The authorize endpoint and the code exchange that the Authorization Code recommendations use were verified by NSerio against a live RelativityOne instance. Relativity does not document them, and its own confirmation is pending. The recommendations label them "verified by NSerio against a live instance; not documented by Relativity". PKCE is required by Relativity's documentation to obtain a refresh token; the `S256` method is NSerio's recommendation, because Relativity does not document which method it accepts.

2. **Relativity has not stated a recommendation for distributed desktop applications.** A desktop or command-line application that uses Authorization Code has to embed a client secret, and a secret shipped inside a distributed binary can be extracted by anyone who holds the binary. The desktop guidance in the recommendations is NSerio's guidance, verified to work; it is not a Relativity recommendation. Confirm it against your own security policy.

3. **The Windows and macOS binaries are unsigned.** Windows SmartScreen shows a warning the first time you run a downloaded copy. The macOS binaries are not signed with an Apple Developer ID certificate and not notarized by Apple, so macOS cannot vouch for them: Gatekeeper blocks the first run of a copy downloaded with a web browser until you allow it, and the checksum is the only check that your download is intact. The first-run steps for [Windows](01-using-the-tool.md#first-run-on-windows) and for [macOS](01-using-the-tool.md#first-run-on-macos) explain how to continue after you have verified the checksum. The `osx-arm64` binary was built on an Intel Mac and has not been run on Apple silicon hardware in this release. Signed, notarized macOS disk images are planned for a later release.

4. **There is no automated build and release pipeline yet (continuous integration and delivery is deferred).** The binaries of a release are built by hand: the Windows and Linux binaries each on their own platform, and both macOS binaries on one Intel Mac, where the `osx-arm64` binary is cross-built. The release publishes a `SHA256SUMS` file. Verify the checksum of every download.

5. **The first launch of a freshly downloaded copy can take longer than one second.** On Windows, the first launch of one freshly downloaded copy took 4.874 seconds; two other freshly downloaded copies, and three copies that were not marked as downloaded, started in about 0.17 seconds, and every second launch took about 0.03 seconds. The cause was not measured. The first launch on macOS has not been measured yet. There is no first-launch figure for Linux, because Linux does not run a reputation check on a downloaded binary.

6. **A session saved by another build is not resumed.** The saved state of a session is resumed only when its state format version and its detection catalog version are both equal to those of the running tool. If either differs, whether the state was written by an older or a newer build, the tool says so, does not use it, and starts a fresh session. Run `scan` again. The tool version itself is not compared, so two builds with the same versions of both can resume each other's sessions.

7. **File names in decomposed Unicode form are identified less strictly.** Two file names that differ only in their Unicode normalization form are reported as two files, and a file name in decomposed form (as macOS returns some names, and as a name can be on Windows NTFS or on a Linux file system, which keeps names byte for byte) may be positioned and identified differently from the same repository scanned on another platform. Detection within each file is not affected. On a repository that reaches the limit of 50,000 findings for one scan, the findings kept are the first ones in path order, and that order depends on the normalization form, so the findings kept can differ between platforms. See [Limits of the tool](03-limits.md#file-names-in-decomposed-unicode-form).

8. **The flow is chosen from signals in the finding's own file.** In real code the credential often reaches that file from another one, so most findings take the default flow with a note asking who supplies the credential. Pointing to interactive-user evidence found elsewhere in the same repository is planned after the alpha.

9. **The time limit on the tool's matching is provisional.** The tool bounds the time it spends evaluating each of its own search expressions on a file, currently 5.25 seconds. It was chosen with a tenfold margin over the slowest benign file measured on Windows and on a Linux virtual machine, and it has not been confirmed on macOS or on other hardware. If a file reaches the limit, the tool stops analyzing it and discards **every** finding for that file, and the report counts the file under "Files whose analysis took too long and was stopped; their findings are not reported". Review such a file by hand.

10. **Files in older single-byte text encodings are not analyzed.** The tool reads source files as UTF-8 or UTF-16. A file saved in another encoding, such as Latin-1 or Windows-1252, and containing accented or other non-ASCII characters, cannot be read: the report counts it under "Files that could not be read as text". Convert it to UTF-8 and scan again, or review it by hand.

11. **One construct can be reported as two findings at the same place.** A Basic `Authorization` header built from a user name and a password that are encoded where the header is written is reported once as the header (`CS-005`, or `CS-006` when the header name is written as a string) and once as the encoded pair (`CS-009`), on the same line or a few lines apart. Both findings belong to one migration, and both locations need to be fixed together. This is not the case for a credential cache entry, which is one finding, nor for an authentication helper (`CS-011`): the helper's definition is one finding and the credential it builds, when the tool recognizes it, is another, at its own line.

12. **For the legacy Import API construction (class `CS-015`), no documented factory accepts a token that a standalone or desktop process acquired itself.** The recommendation for this class names the one documented factory, `ImportAPI.CreateByRsaBearerToken(webServiceUrl)`. It takes no token, and Relativity documents it for processes hosted by the Relativity service account, for example an agent. Relativity's Import API concepts page lists bearer-token authentication as not supported for a desktop application. The recommendation therefore gives no code form for a standalone or desktop process that would obtain an OAuth2 token itself, and says so. Both pages are Relativity Server documentation, and no RelativityOne page for this legacy class was found, so whether the factory exists in an SDK version aimed at RelativityOne is not established. The sources are [`ImportAPI.CreateByRsaBearerToken`](https://platform.relativity.com/Server2021/Subsystems/importapi/Content/html/M_kCura_Relativity_ImportAPI_ImportAPI_CreateByRsaBearerToken.htm) and [Import API concepts](https://platform.relativity.com/Server2025/Content/Import_API/Overview/Import_API_concepts.htm). The entry for this class in [Part C](../migration/part-c-csharp.md#cs-015-migration) says the same.

### How to report an issue

Report an issue through the channel this release was distributed with, the place where you downloaded it. Include the version string from `migrate-auth --version`, your platform, the command you ran, and what you expected. **Do not attach a report or a JSON data file to an issue without reading it first:** they contain no credential values, but they list where credentials are in your repository. Never paste a credential.

## What's next

The alpha is milestone M0, the base and C#. The following milestones add one language each and then the last feature, in this order. None has a date.

| Milestone | Adds |
| --- | --- |
| M1 | PowerShell |
| M2 | VB.NET |
| M3 | Shell |
| M4 | JavaScript |
| M5 | TypeScript |
| M6 | Python |
| M7 | Go |
| M8 | AI-assisted fallback (`ai-assist`) |

Until a language is delivered, its files are counted and are not analyzed. Each milestone updates these release notes.

Also planned after the alpha: C# code examples for token handling. In this release the entries in [Part C](../migration/part-c-csharp.md#migration-suggestions-by-pattern-class) describe how to acquire a token, how to cache it and how to handle its expiry in words, and contain no token-handling code.
