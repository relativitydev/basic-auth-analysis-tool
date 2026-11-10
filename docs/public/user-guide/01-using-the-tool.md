# Using the tool (User Guide, section 1 of 8)

This section explains how to install and run `migrate-auth`, how to configure it, and how to read what it writes. For the rules behind each finding, see the Migration Documentation: Part A describes each pattern the tool looks for ([C#](../migration/part-a-csharp.md), [PowerShell](../migration/part-a-powershell.md), [VB.NET](../migration/part-a-vbnet.md), [Shell](../migration/part-a-shell.md)), and Part C gives the migration suggestion ([C#](../migration/part-c-csharp.md), [PowerShell](../migration/part-c-powershell.md), [VB.NET](../migration/part-c-vbnet.md), [Shell](../migration/part-c-shell.md)) for each pattern and each OAuth 2.0 flow. Where the new secrets go is in [Part B](../migration/part-b-secret-management.md), Secret Management.

The other sections of this guide are listed in the [index](index.md).

## What the tool does and does not do

`migrate-auth` finds Basic Authentication in the source code of a repository and tells you how to move it to OAuth 2.0.

- **It analyzes only.** It reads the files of the repository you point it at and writes a report. It never edits your code, never rewrites a file, and never changes the repository.
- **It never contacts RelativityOne or any network service.** It works offline. It cannot tell you which OAuth2 Clients your RelativityOne instance already has, and it does not check that a URL, a user or a client exists.
- **It never configures an OAuth2 Client.** The OAuth2 Client settings in the report are guidance. Creating or changing the client is a manual step you do in RelativityOne; see [Configuring the OAuth2 Client](05-configuring-the-oauth2-client.md).
- **It reports what its catalog searches for.** A construct the catalog does not cover, or a language this release does not analyze, produces no finding. An empty result is not proof that no Basic Authentication exists. See [Limits of the tool](03-limits.md).
- **This release analyzes C#, PowerShell, VB.NET and Shell only.** Files in other languages are counted and are not analyzed. See the [release notes](02-release-notes.md).

## Installing

A release consists of one file for each platform and a `SHA256SUMS` file that lists the SHA-256 checksum of every file. There is no installer: you download the file for your platform, check its checksum, and run it.

| Platform | File you download | What to do after checking the checksum |
| --- | --- | --- |
| Windows (x64) | the `win-x64` binary, `migrate-auth-0.4.0-alpha-win-x64.exe` | Put it in a folder of your choice. See [First run on Windows](#first-run-on-windows). |
| Linux (x64) | the `linux-x64` binary, `migrate-auth-0.4.0-alpha-linux-x64` | Make it executable. See [First run on Linux](#first-run-on-linux). |
| macOS on Apple silicon | the `osx-arm64` binary, `migrate-auth-0.4.0-alpha-osx-arm64` | Make it executable, and allow it in Gatekeeper. See [First run on macOS](#first-run-on-macos). |
| macOS on Intel | the `osx-x64` binary, `migrate-auth-0.4.0-alpha-osx-x64` | Make it executable, and allow it in Gatekeeper. See [First run on macOS](#first-run-on-macos). |

In this release the macOS binaries are plain binaries, like the Linux one. They are not signed with an Apple Developer ID certificate and not notarized by Apple, so the checksum is the only check that your download is intact. This guide makes no claim that a macOS binary of this release has been run on a particular version of macOS: the macOS binaries are built and checked when the release is assembled. To find which binary your Mac needs, run `uname -m` in Terminal: it prints `arm64` on Apple silicon and `x86_64` on Intel.

Download the file for your platform and `SHA256SUMS` from the same release page, into the same folder. The file name carries the version and the platform. You can run the file under that name, or rename it to `migrate-auth` (`migrate-auth.exe` on Windows) to match the commands in this guide. In PowerShell, and in a Linux or macOS shell, a program in the current folder is started with the `.\` or `./` prefix, for example `.\migrate-auth-0.4.0-alpha-win-x64.exe`, unless its folder is in your `PATH`; the commands below show the name without the prefix, and you add it.

### Supported operating systems

Each binary is self-contained: it does not need .NET or any other runtime installed. The tool is built with .NET 10 and compiled ahead of time to native code, so the operating systems it supports are the ones Microsoft lists for .NET 10, narrowed where the way a binary of this release was built requires a newer system. The last column says where each binary was run for this release; for macOS it makes no claim.

| Platform | Minimum supported version | Run for this release |
| --- | --- | --- |
| Windows (x64) | Windows 11, version 24H2. Windows 11 version 23H2 and Windows 10 versions 21H2, 1809 and 1607 only in the Enterprise or IoT editions that .NET 10 lists. Windows Server 2016; Windows Server 2012 and 2012 R2 only with Extended Security Updates | Windows 11 Enterprise, version 25H2 (build 26200) |
| Linux (x64) | A distribution with glibc 2.35 or later, for example Ubuntu 22.04 | Ubuntu 22.04 LTS, in a virtual machine |
| macOS on Apple silicon (`osx-arm64`) | macOS 15 | No run is claimed for this release |
| macOS on Intel (`osx-x64`) | macOS 15 | No run is claimed for this release |

- **Linux.** .NET 10 itself supports glibc 2.27 and later on x64, but a native binary built on Linux runs only on the same or a newer Linux version than the one it was built on. The Linux binary of this release was built on Ubuntu 22.04, whose glibc is 2.35, so a distribution with an older glibc is not supported even where .NET 10 lists it. Distributions based on musl rather than glibc, such as Alpine Linux, are not supported: the `linux-x64` binary needs glibc.
- **macOS.** .NET 10 supports macOS 15, 26 and 27. macOS 13 and macOS 14 are older than the versions .NET 10 supports, so they are not supported, whether or not the binary happens to start on them. Use macOS 15 or later.
- **Other versions.** A version older than the minimum in the table may happen to run the tool, and it is not supported. A supported version not named in the last column has not been run for this release.

**Where these versions come from.** The versions follow ".NET 10.0 - Supported OS versions" in the .NET release notes (`https://github.com/dotnet/core/blob/main/release-notes/10.0/supported-os.md`, last updated 2026-09-28). That a native binary built on Linux runs only on the same or a newer Linux version is stated in "Native AOT deployment overview" (`https://learn.microsoft.com/dotnet/core/deploying/native-aot/`), and that `linux-x64` is the binary for glibc distributions while musl distributions such as Alpine Linux use another one is stated in ".NET Runtime Identifier (RID) catalog" (`https://learn.microsoft.com/dotnet/core/rid-catalog`). The addresses are given as text rather than as links, because this guide links only to Relativity documentation.

### Verifying the checksum

Check the checksum before you run the file. If the value does not match the one in `SHA256SUMS`, do not run the file: download it again, and if it still does not match, report it (see [how to report an issue](02-release-notes.md#how-to-report-an-issue)).

In the commands below, replace `<file name>` with the name of the file you downloaded.

**Windows (PowerShell).**

```powershell
Get-FileHash -Algorithm SHA256 .\<file name>
Select-String -Path .\SHA256SUMS -Pattern "<file name>"
```

The first command prints the checksum of your download and the second prints the line of `SHA256SUMS` that names the file. The long hexadecimal values must be identical. The comparison ignores letter case.

**Linux.**

```bash
sha256sum --check --ignore-missing SHA256SUMS
```

The command checks every file named in `SHA256SUMS` that is present in the current folder, and prints `OK` beside a file whose checksum matches.

**macOS (Terminal).** The example checks the Apple silicon binary; on Intel, use `migrate-auth-0.4.0-alpha-osx-x64`.

```bash
shasum -a 256 migrate-auth-0.4.0-alpha-osx-arm64
grep migrate-auth-0.4.0-alpha-osx-arm64 SHA256SUMS
```

The first command prints the checksum of your download and the second prints the line of `SHA256SUMS` that names the file. The two hexadecimal values must be identical. Check the checksum before you make the file executable or allow it in Gatekeeper: the macOS binaries are not signed or notarized, so macOS cannot vouch for them, and the checksum is the integrity check.

## First run

### First run on Windows

The Windows binary is not signed. When you start a file you downloaded with a browser, Windows SmartScreen may show a blue window titled **Windows protected your PC**. This is expected for this release. After you have checked the checksum as described above:

1. In the SmartScreen window, choose **More info**.
2. Choose **Run anyway**.

If you start the tool from a terminal and Windows blocks it without showing that window, open the file's **Properties**, tick **Unblock** at the bottom of the General tab, and choose **OK**; or run `Unblock-File .\migrate-auth-0.4.0-alpha-win-x64.exe` in PowerShell. Both do the same thing, and you need to do it once per downloaded copy.

The first launch of a freshly downloaded copy can take longer than later launches. See the [release notes](02-release-notes.md#known-issues).

### First run on Linux

Linux does not show a reputation check for a downloaded binary. Make the file executable, then run it:

```bash
chmod +x ./migrate-auth-0.4.0-alpha-linux-x64
./migrate-auth-0.4.0-alpha-linux-x64 --version
```

### First run on macOS

The macOS binaries of this release are **not signed with an Apple Developer ID certificate and not notarized by Apple**. macOS therefore cannot tell you who made the file or whether it was changed after it was built, and the checksum you verified above is the only integrity check. Do not continue unless the checksum matches.

1. Make the file executable. The commands use the Apple silicon binary; on Intel, use `migrate-auth-0.4.0-alpha-osx-x64`.

	```bash
	chmod +x ./migrate-auth-0.4.0-alpha-osx-arm64
	```

2. Run it once:

	```bash
	./migrate-auth-0.4.0-alpha-osx-arm64 --version
	```

	If you downloaded the file with a web browser, macOS Gatekeeper blocks this first run and shows a warning that Apple cannot check the file for malicious software. This is expected for this release. Close the warning with **Done**; do not choose **Move to Trash**.

3. Allow the file in **one** of these two ways. You need to do it once per downloaded copy.

	- **In System Settings.** Choose **Apple menu > System Settings**, then click **Privacy & Security** in the sidebar (you may need to scroll down). Go to **Security**, click **Open Anyway** beside the message about the blocked file, enter your login password, and click **OK**. Apple states that the button is available for about an hour after you try to open the file. Run the command again, and if the warning reappears, click **Open**.
	- **In Terminal.** Remove the quarantine attribute that macOS attaches to a downloaded file, then run the command again:

		```bash
		xattr -d com.apple.quarantine ./migrate-auth-0.4.0-alpha-osx-arm64
		```

		If the file does not carry the attribute, the command reports that there is no such attribute and changes nothing.

If your Mac is managed by your organization, its device management can prevent you from overriding Gatekeeper; ask your administrator.

**Where these steps come from.** The System Settings steps and the warning follow two pages of Apple Support: "Open a Mac app from an unknown developer" (`https://support.apple.com/guide/mac-help/mh40616/mac`) and "Safely open apps on your Mac" (`https://support.apple.com/en-us/102445`). How Gatekeeper checks downloaded software, and that an organization can restrict overriding it, is described in "Gatekeeper and runtime protection" in Apple Platform Security (`https://support.apple.com/guide/security/sec5599b66df/web`). The addresses are given as text rather than as links, because this guide links only to Relativity documentation. Those pages describe apps. Apple does not document how Gatekeeper presents a command-line binary started from Terminal, so the warning you see may be worded differently from the one Apple shows. Apple's support documentation also does not describe removing the quarantine attribute; the `xattr` command and its `-d` option are described in the `xattr` manual page on your Mac (`man xattr`).

### Checking that the tool starts

Run the tool with `--version` (on Windows, `.\migrate-auth-0.4.0-alpha-win-x64.exe --version`). It prints one line with the tool version and the version of the detection catalog, and exits:

```text
migrate-auth 0.4.0-alpha (catalog 1.31.0)
```

`--help` (or `-h`) prints the usage, the two options described below, and the list of commands, and exits.

## Starting a session

The tool is an interactive session. You start it on one repository, and then type commands at its prompt.

```text
migrate-auth --repositoryFolderPath <path to the repository> --documentationDestinationFolderPath <path to an output folder>
```

| Option | Meaning |
| --- | --- |
| `--repositoryFolderPath <path>` | The folder to assess. Required. |
| `--documentationDestinationFolderPath <path>` | The folder the report and the data file are written to. If you leave it out, the tool uses the setting `outputPath` of your user profile file (see [The configuration file](#the-configuration-file)), and otherwise the folder you started the tool from. |

Each option also accepts the form `--option=<value>`. An option the tool does not know is reported as `command not found: <word>`, followed by the usage line, and is ignored; an option given without a value is reported as `no value supplied for: <option>`.

**The output folder must not be the repository, inside it, or a folder that contains it.** The tool refuses to start in that case, so that its own output can never become part of the code under assessment. If you start the tool from a folder inside the repository and give no destination, give one.

When the session starts, the tool prints its identification line and one of two lines: `Starting a fresh session for <repository>.` or `Resuming the session for <repository>.` The prompt is `migrate-auth>`.

A session is remembered. When you end it, the stages you completed are saved in the output folder, and the next session for the same repository resumes from them. The tool does not watch the repository: **run `scan` again after any change to the code**, because a resumed session keeps the results of the files as they were when they were scanned.

The saved state of a session is used only when it was written with the same state format version and the same detection catalog version as the running tool, and for the same repository. Otherwise it is not used, whether it was written by an older or a newer build: the tool says so and starts a fresh session. The tool version itself is not compared.

Press Ctrl+C to interrupt a running stage. The session stays open and returns to the prompt.

## The workflow

The four stages run in order. A stage can run only after the stage before it has completed; if you type one out of order the tool answers `Run '<stage>' first.`, naming the stage to run, and stays at the prompt.

1. **`scan`** walks the repository, counts the files by language, and selects the candidate files: the files in an enabled language that could contain the constructs the catalog looks for.
2. **`analyze`** reads the candidate files and produces the findings.
3. **`suggest`** attaches a migration recommendation to each finding that warrants one, and prints a summary on the console. The summary has the counts of recommendations by route and by flow, a legend that gives each flow note once with a short key such as `[N1]`, and one row for each reported High or Medium finding: its identifier, the flow, the decision that chose the flow, and the key of its note. The console shows at most 100 rows. When there are more, a last line counts the rest by decision and flow; the report lists every finding.
4. **`report`** writes the Markdown report and the JSON data file. See [Where the outputs go](#where-the-outputs-go).

Running a stage again invalidates the stages after it: after a new `scan` you run `analyze`, `suggest` and `report` again.

### What the scan reads and what it leaves out

**Folders the walk does not enter.** The walk skips eight folder names, matched exactly, in lower case, at any depth in the repository: `bin`, `obj`, `.git`, `.vs`, `node_modules`, `dist`, `build` and `packages`. A folder named `Bin` or `Build` is walked, and so is any other name, including `vendor`, `target`, `out`, `Debug` and `Release`. Nothing inside an excluded folder is read, so hand-written code or scripts kept in a folder with one of those names are not assessed and no finding can come from them. These folders are not skipped items, but they are not hidden: `scan` prints `Folders excluded from the scan and not read: <n>. The report lists them.`, and the report names them (see `excludedFolders` under [Scan summary](#scan-summary)).

**Files the tool reads and reports at Low.** These are read and analyzed, and a match in them is reported at Low confidence, for manual review:

- a folder named `vendor` or `third-party`, in any letter case, at any depth (vendored code). `VendorPortal` does not match;
- a folder named `sample`, `samples`, `example` or `examples`, in any letter case, at any depth, and a file whose name begins with `Sample` or `Example` with a capital first letter (sample and example code). `SampleClient.cs` and `ExampleRunner.cs` match; `sampleClient.cs`, `SamplesCore` and a folder named `demo` do not;
- mocks, fakes and stubs, and generated code, in all four languages, as Part A lists for [C#](../migration/part-a-csharp.md#how-confidence-is-assigned), for [PowerShell](../migration/part-a-powershell.md#how-confidence-is-assigned), for [VB.NET](../migration/part-a-vbnet.md#how-confidence-is-assigned) and for [Shell](../migration/part-a-shell.md#how-confidence-is-assigned);
- for C# only, a folder named `Deployment`. PowerShell, VB.NET and Shell do not use this signal: a script or a source file under a `Deployment` folder is reported at its full level, because that folder is where deployment code is kept.

**Test code the tool reads and reports at Medium at most.** A finding in a file that the tool recognizes as test code, by its folder, its file name or a test-framework marker, is reported at Medium at most and not at Low, with the recommendation of its class; the report writes `Test code: yes` under it and the JSON data file sets `testCode` to `true`. Part A lists the signals for each of the four languages (the links above). A file that is test code and also a mock, a sample, vendored or generated is Low.

**Text encodings.** A file that starts with a byte order mark is read as the UTF-8, UTF-16 or UTF-32 form the mark declares. Without a mark, a file is read as UTF-8 when it is valid UTF-8, as UTF-16 when its bytes have the shape of two-byte text, and otherwise as Windows-1252, which covers Latin-1 and the other Western European single-byte files. A file read as Windows-1252 was read by a guess, so it is not hidden: the report counts it and names it (see `legacyEncodedFiles` under [Scan summary](#scan-summary)). A file that contains a null byte or another control character that is not text, or a byte Windows-1252 leaves undefined, is not read this way and is counted among the files that could not be read as text. The line and column of a finding count the decoded characters, so a file with accented characters reports the same column as it would in UTF-8.

**File size and line length.** The tool analyzes a file only if it is at most 2 MiB (2,097,152 bytes) and its longest line has at most 10,000 characters. A larger file, or one with a longer line, is not analyzed at all: it is counted under **Files larger than this release analyzes** or **Files with a line longer than this release analyzes** in the skipped items, and the report names it.

### Progress and large repositories

A stage that works through many files says how far it is. Once a stage has more than 5,000 files to go through, it prints a progress line at most every 2 seconds, for example `scan: 12,000 of 50,000 files examined.` or `analyze: 6,000 of 9,000 candidate files examined.` While `scan` is still counting the files and does not yet know the total, the line reads `scan: 6,000 files found so far.` A repository with fewer files finishes before the first line and prints none.

Before a stage starts on more than 50,000 files, it prints one warning: `scan: 50,001 files to examine. This will take several minutes.` For `analyze` the count is of candidate files and the warning reads `analyze: 50,001 candidate files to examine. This will take several minutes.` The warning is advance notice and not a limit: the stage carries on, and Ctrl+C interrupts it.

**`ai-assist`** is an optional fifth step, listed in `help`. It is not available in this release: AI-assisted review arrives in a later release. Running it before `report` gives the same answer as any stage out of order: it names the first stage not yet completed, for example `Run 'analyze' first.` after only `scan`, and running it after `report` says that it is not available in this release. The report and the JSON file already hold the complete assessment.

## The commands

`help` lists every command in two groups. This is the complete list.

| Command | Group | What it does |
| --- | --- | --- |
| `scan` | Workflow, 1 | Walk the repository, inventory it by language, and select the candidate files. |
| `analyze` | Workflow, 2 | Analyze the candidate files and produce the findings. |
| `suggest` | Workflow, 3 | Attach a migration recommendation to each finding that warrants one. |
| `report` | Workflow, 4 | Write the Markdown report and the findings JSON file. |
| `ai-assist` | Workflow, 5 | Not available in this release: AI-assisted review ships in a later deliverable. |
| `help` | Session | List every command this session accepts. |
| `clear` | Session | Clear the console. |
| `exit` (or `quit`) | Session | End the session. The completed stages are kept for the next one. |

Command names are not case-sensitive. A word the session does not recognize is answered with `command not found: <word>` and a reminder to run `help`; the session stays open.

## The configuration file

The tool reads settings from a file named `.migrate-auth.json`. It looks in two places: the **root of the repository you assess**, and your **user profile folder**. A missing file is normal and is not reported. A file that exists but cannot be read, or does not have the expected structure, is reported at start-up and is not applied: each setting is then taken from the next source, which for a repository file is the user profile file where it sets that key and the built-in default otherwise, and for a profile file is the built-in default; for a file with the wrong structure the tool adds one line saying what was wrong, such as the line and position of a JSON error. A file has the wrong structure only when it is not valid JSON or when it is not a JSON object. A key whose value is not valid, such as a `defaultFlow` of `"Client Credentials"` instead of `ClientCredentials`, does not cost the rest of the file: that key alone is ignored, as if it were absent, so it is taken from the next source in the same way, a warning at start-up names the key (and the value, when it is text), and the other keys of the file still apply. Keys and language names the tool does not know are ignored too, so a file written for a later release still loads, and each one is named in a warning at start-up so that a misspelt key does not go unnoticed. Values are accepted without regard to letter case: `"csharp"` and `"high"` are read as `CSharp` and `High`, and the tool announces and records them in that canonical form. A key whose value is JSON `null` is treated as if the key were absent. A file larger than 262,144 bytes (256 KiB) is not read: start-up reports it as larger than that limit and does not apply it, like a file with the wrong structure.

**Where the user profile file is.** It is a file named `.migrate-auth.json` directly in your user profile folder, the folder the .NET runtime reports as the user profile. On Windows that is `%USERPROFILE%\.migrate-auth.json`, for example `C:\Users\<you>\.migrate-auth.json`. On Linux, and on macOS, it is `$HOME/.migrate-auth.json`; when `HOME` is not set, the runtime falls back to the home folder recorded for your account. No other folder is searched, the tool never creates the file, and if the runtime reports no user profile folder at all the profile file is treated as absent, without a message.

**What a relative path is relative to.** A relative `outputPath`, like a relative `--documentationDestinationFolderPath`, is resolved against the folder you started the tool from (its working directory). It is not resolved against the repository or against the profile folder.

The file is a JSON object. This example uses every key:

```json
{
	"enabledLanguages": ["CSharp"],
	"minimumConfidenceLevel": "Medium",
	"outputPath": "<path to an output folder>",
	"defaultFlow": "AuthorizationCode"
}
```

| Key | Values | Meaning | Where it is read |
| --- | --- | --- | --- |
| `enabledLanguages` | A list of language names: `CSharp`, `VBNet`, `JavaScript`, `TypeScript`, `Python`, `Go`, `PowerShell`, `Shell`. | The languages to assess. When the key is absent, all eight are enabled. A language you leave out is not scanned for candidates and has no findings; its files are still counted and are marked as not assessed. Only `CSharp`, `PowerShell`, `VBNet` and `Shell` have an analyzer in this release, so enabling another language changes nothing yet. A list that enables no language at all is refused at start-up (exit code 6). This includes a list that names only languages the tool does not know: the tool says that every name was unknown, names them together with the accepted names, and does not start the session. | Repository file first, then user profile. A list in the repository file replaces the profile's. |
| `minimumConfidenceLevel` | `Low`, `Medium`, `High`. | The lowest confidence level that appears in the output. The default is `Low`, which shows everything. The setting only hides findings from the output; it never changes the level a finding receives. | Repository file first, then user profile. A level in the repository file replaces the profile's. |
| `outputPath` | A folder path. | Where the report, the data file and the saved session state are written, when `--documentationDestinationFolderPath` is not given. | User profile only. |
| `defaultFlow` | `ClientCredentials` or `AuthorizationCode`. | The flow a finding receives when no signal in its file decides one. The default is `ClientCredentials`. | User profile only. |

**A repository's file may narrow its own assessment, and it is always said.** The file in the repository root takes precedence over the user profile for `enabledLanguages` and `minimumConfidenceLevel`. When it disables languages that would otherwise be assessed, or raises the confidence threshold, the tool says so at start-up, for example that the file sets the enabled languages to `Python` and disables the others, or raises the minimum confidence level from `Low` to `High` so that `Medium` and `Low` findings are not shown; and the report's provenance section states the same. The same applies in the other direction: a repository file that enables a language your profile disabled, or lowers the threshold, is applied and recorded in the provenance as well (`languagesEnabledByRepository`, and a `thresholdChange` whose `direction` is `Lowered`). A file that disables every language is refused at start-up (exit code 6), as a profile that does so is. The output folder and `defaultFlow` are never read from the repository's own file: a value of either there is ignored and recorded as ignored.

Command-line options win over the file: `--documentationDestinationFolderPath` overrides `outputPath`.

## Reading the report

The `report` stage writes two files with the same content: a Markdown report for people, and a JSON data file for tools. This section describes both. The names in `code style` in the headings below are the field names of the JSON file.

The Markdown report is titled **Basic Authentication assessment**. It starts with a **handling notice**, and the JSON file carries the same text in `handlingNotice`. The report contains no credential values, but it is a precise index of where credentials are. Handle it and share it accordingly.

### Findings summary

Right after the title, before the numbered sections. The Markdown report only; the JSON file's `findings` array is the same list.

A table with one row for each finding the report lists in section 4, in the same order and under the same identifiers, with the columns **ID**, **File:Line**, **Language**, **Pattern**, **Confidence** and **Flow** (`none` for a Low finding). A finding the confidence filter hides is not in the table, exactly as it is not in section 4 or in `findings`. The table does not say why a finding is Low: the reason is on the `Low because` line under the finding in section 4, and in `lowReasons` in the JSON file (see [Findings](#findings)).

When there is nothing to list, the table is replaced by one line, repeated at the top of section 4. It says what was not assessed as well as that nothing was found, for example: `0 findings; 3 files in languages this release does not assess (Python, Go, TypeScript).` It names files in languages this release has no analyzer for, files in languages your configuration disabled (said as "disabled by the repository's configuration file" when the repository's own file set the enabled languages, and "disabled by configuration" otherwise), and any recognized findings the confidence filter hides.

When the repository's own configuration file raised the minimum confidence level, the summary also says so, and how many findings that hides: `The configuration file in the repository root raises the minimum confidence level from Low to High; 12 findings (Medium and Low) are not shown.` The number is `hiddenFindingCount` of `repositoryConfigurationEffect.thresholdChange` in the JSON file.

### Coverage

Report section **1. Coverage**; JSON `coverage`.

A table lists all eight languages and what this release does for each (`coverage.languages[]`, with `language` and `coverage`): "supported, 18 classes searched" for C#, "supported, 14 classes searched" for PowerShell and "supported, 19 classes searched" for VB.NET and "supported, 25 classes searched" for Shell (`patternClassesSearched` is the number of classes), and "declared unsupported in this release" for the others. An introductory sentence (`coverage.statement`) says that the report is not a guarantee of absence.

**Declared unsupported constructs** (`declaredUnsupportedConstructs[]`, each with a `construct` and a `reason`) lists what the catalogs of the supported languages do not cover, so that a missing finding is not read as a clean result. Part A explains the same list ([C#](../migration/part-a-csharp.md#declared-unsupported-coverage), [PowerShell](../migration/part-a-powershell.md#declared-unsupported-coverage), [VB.NET](../migration/part-a-vbnet.md#declared-unsupported-coverage), [Shell](../migration/part-a-shell.md#declared-unsupported-coverage)).

### Scan summary

Report section **2. Scan summary**; JSON `scanSummary`.

The number of files enumerated (`filesEnumerated`), the lines counted (`linesCounted`) and the candidate files selected (`candidateFiles`), and a table with one row per language (`languages[]`): its `language`, `files`, `lines`, `candidateFiles`, `shareOfFilesPercentage`, whether it was `assessed`, and, when it was not, `notAssessedReason` (`noAnalyzer` when the release has no analyzer for it, `disabledByConfiguration` when your configuration turned it off). `firstLineFallbackFiles` counts files whose language was decided from their first line because their name did not say. `enabledLanguagesDifferFromSession` is `true` when the languages enabled when `scan` ran differ from the languages enabled in the session that wrote the report, which is when the console and the Markdown report say that the scan used a different set of enabled languages and ask you to run `scan` again; the rows above then describe the earlier scan. Only the enabled languages are compared; a change to any other setting after `scan` is not flagged. Files in languages this release does not assess are counted and not analyzed. The tool's own configuration file at the root of the repository is not counted; a file of that name in a subfolder is an ordinary file.

`excludedFolders` lists the folders the tool does not enter, such as build output and installed dependencies: `count` is how many it met, `paths` names up to a hundred of them relative to the repository, and `omittedPathCount` is the number counted and not listed. They are excluded by design and are not skipped items: nothing in them is read, so a folder listed here is a place where no finding could have been reported.

`legacyEncodedFiles` lists the files the tool read as Windows-1252 because they were not valid UTF-8 or UTF-16: `encoding` is `Windows-1252`, `count` is how many files were read that way, `paths` names up to a hundred of them relative to the repository, and `omittedPathCount` is the number counted and not listed. These files were read and analyzed, so they are not skipped items; they are listed because that reading is a guess, and an accented character in them may differ from what their author wrote. The Markdown report states the same count in section 2.

### Skipped items

Report section **3. Skipped items**; JSON `skippedItems[]`.

Every file or folder the tool could not fully read is counted here, so that a gap is visible as a number. Each entry has a `category`, a `description`, a `unit` (`files` or `directories`), a `count`, and the paths in `paths`; `omittedPathCount` is the number of paths counted but not listed, when there are too many to list. The JSON file always holds one entry for each category below, with a count of zero when nothing was skipped. The exact text of each `category` is in [Values of the JSON fields](#values-of-the-json-fields). The categories, by `description`, are:

- files that could not be opened;
- directories whose contents could not be listed;
- links to other locations, which are not followed;
- files that could not be read as text (for example, binary files, and files in a single-byte code page that holds a byte Windows-1252 leaves undefined);
- files larger than this release analyzes;
- files with a line longer than this release analyzes;
- files whose path is too long for the operating system to open;
- files only partly readable as code, so not analyzed in full;
- files whose analysis took too long and was stopped, whose findings are not reported (see [Limits](03-limits.md));
- files where at least one unreliable match was discarded, whose other findings are reported;
- files found during the scan and no longer present when analyzed.

The report gives the total of skipped files and the number of directories separately, and never adds them together: a directory that could not be listed is a hole of unknown size.

### Findings

Report section **4. Findings**; JSON `findings`.

The section opens with the number of findings reported (`findingsCap`, below), the minimum confidence level in effect, and, for the findings that carry a flow, how many were assigned each flow and how many of those were assigned by default. The findings are then grouped under two headings, **Destination established as Relativity** and **Destination not established**, according to `destinationCertainty` (`established` or `unknown`). The tool reads one file at a time and often cannot tell which server a request goes to; where it cannot, the recommendation says so and asks you to confirm the destination before you act.

Each finding has a heading of the form `<id> - <file>:<line> - <language> - <pattern class name> (<class identifier>) - <confidence level>` and these fields:

| JSON field | What it is |
| --- | --- |
| `id` | The finding identifier, such as `003`. Identifiers are assigned over the full set of findings before the confidence filter is applied, so a filtered report shows gaps. |
| `sequentialIndex` | The finding's position in the deterministic order. |
| `filePath` | The path of the file, relative to the repository, as the platform returned it. |
| `line`, `column` | Where the match starts. |
| `language` | The language of the file. |
| `patternClass` | The class identifier, such as `CS-005`, the same identifier [Part A](../migration/part-a-csharp.md) uses. |
| `confidence` | `High`, `Medium` or `Low`; see below. |
| `destinationCertainty` | `established` or `unknown`; see above. |
| `literalCredentialSignal` | `true` when the match is a credential written literally into the source. The tool never copies the value into its output. |
| `testCode` | `true` when the file's name, folder or a test-framework marker marks it as test code, at any level, and `false` otherwise; present on every finding. A test-code finding is reported at most at Medium. The Markdown report states it on a `Test code` line under the finding's heading and writes nothing for a finding that is not test code. The value is not part of the `fingerprint`. |
| `securityNote` | For a finding with a literal credential: a security warning, separate from the migration. See [secret warnings](#secret-warnings). Otherwise null. |
| `fingerprint` | A stable text that identifies the finding across runs. |
| `recommendation` | The migration recommendation, or null for a Low finding. See [The recommendation](#the-recommendation). |
| `flow` | The recommended OAuth 2.0 flow, `ClientCredentials` or `AuthorizationCode`, or null for a Low finding. |
| `flowDecision` | Why that flow was chosen: `InteractiveSignal`, `FixedSignal` or `Default`; null for a Low finding. |
| `flowNote` | The text that goes with the decision; null for a Low finding. |
| `lowReasons` | For a Low finding, why it is Low, as a list of the values `patternClass` (the class is Low by definition), `commentOrStringLiteral` (the match sits in a comment or a string literal), `demotedFile` (kept so that a data file written by an earlier release can be read; this release does not write it), `demotedMockFile` (the file's name, folder or content marks it as a mock, fake or stub), `demotedSampleFile` (the file's name or folder marks it as a sample or example), `demotedVendoredFile` (the file sits in a folder named `vendor` or `third-party`), `demotedGeneratedFile` (the file's name or content marks it as generated code) and `demotedDeploymentFolder` (the file sits in a deployment folder); see [What the scan reads and what it leaves out](#what-the-scan-reads-and-what-it-leaves-out) for both; more than one when several apply, and empty when the reason was not recorded. Null for a finding that is not Low. The Markdown report states the same on a `Low because` line under the finding's heading, in words: for `patternClass` it reads "this pattern class is Low by definition" and then says why for that class: a definition that builds a credential or an authentication header rather than a call, a credential that goes to a sink that is not an HTTP client, a credential passed to a web command with no scheme named, or, for any other Low class, that as written it does not establish Basic Authentication towards RelativityOne; for `demotedMockFile` it reads "the file's name, folder or content marks it as a mock, fake or stub, a test double, so it stands in for the real service rather than calling it", for `demotedSampleFile` "the file's name or folder marks it as a sample or example, so it illustrates a pattern rather than running in the application", for `demotedVendoredFile` "the file sits in a vendor or third-party folder, so it is code the repository carries rather than its own", for `demotedGeneratedFile` "the file's name or content marks it as generated code, so the fix belongs in what it is generated from rather than in the file", and for `demotedDeploymentFolder` "the file sits in a deployment folder, so it is installation or release tooling rather than the application's own code". A file that is demoted for several kinds of reason lists every value, in the order above. |

**Confidence.** Every finding receives one level. **High**: the code almost certainly performs Basic Authentication against a live call path. **Medium**: the code matches a Basic Authentication shape, but the credential source or the call path cannot be established from the file alone. **Low**: the match is present but unlikely to be live authenticating code, such as a comment, a mock, sample or example code, vendored code or a generated file. A match in test code is Medium at most. [Part A](../migration/part-a-csharp.md#how-confidence-is-assigned) explains how a level is assigned.

**Every finding needs human review.** The tool locates constructs and proposes a migration; it does not know your system. A person confirms the destination, the flow and the change before any code is edited, at every level.

**Low findings are for manual review.** A Low finding carries no flow and no recommendation (`flow`, `flowDecision`, `flowNote` and `recommendation` are null); the report says it is routed to manual review and points to that section of Part C. A Low level is not evidence that the code does not authenticate. It means a person has to look. [Part C](../migration/part-c-csharp.md#judging-a-low-confidence-match) says how to judge each cause.

#### The flow, the decision and the note

Every High and Medium finding is given one of two OAuth 2.0 flows, and the report says how it chose. These three fields are extensions of the data contract that the tool adds to each finding.

- **`flow`** is `ClientCredentials` (the application authenticates as itself, with no user present) or `AuthorizationCode` (the application acts for a signed-in user). See [OAuth 2.0 essentials](04-oauth2-essentials.md) for how to choose.
- **`flowDecision`** is how the flow was chosen:
	- `InteractiveSignal`: something in the file shows that a person supplies the credential, such as a password prompt, a password box or a login handler. The flow is `AuthorizationCode`.
	- `FixedSignal`: something in the file shows that the credential is fixed for the integration, such as a read from configuration, an environment variable or a secret store. The flow is `ClientCredentials`.
	- `Default`: nothing in the file decides. The flow is the default flow: `ClientCredentials` unless your user profile file sets `defaultFlow`.
- **`flowNote`** is a sentence that asks you to confirm the choice. It is one of four texts, given once each in this table with the short key the `suggest` console uses:

<!-- generated:begin flow-notes -->
| Key | When the tool shows it | Note |
| --- | --- | --- |
| [N1] | No signal in the file decides, and the default flow is Client Credentials (`flowDecision` is `Default`, `flow` is `ClientCredentials`). | Who supplies the credential at run time? If a person types it or signs in, the integration acts for that person: use Authorization Code. If it comes from configuration, settings, an environment variable or a secret store: use Client Credentials. No signal in this file showed which applies, so Client Credentials, the default, is recommended. Use Authorization Code instead if the integration acts on behalf of a signed-in user, or if your RelativityOne instance already has a Code-flow OAuth2 Client for this integration. |
| [N2] | No signal in the file decides, and your user profile file makes Authorization Code the default (`flowDecision` is `Default`, `flow` is `AuthorizationCode`). | Who supplies the credential at run time? If a person types it or signs in, the integration acts for that person: use Authorization Code. If it comes from configuration, settings, an environment variable or a secret store: use Client Credentials. No signal in this file showed which applies, so Authorization Code, the configured default, is recommended. Keep Authorization Code if the integration acts on behalf of a signed-in user, or if your RelativityOne instance already has a Code-flow OAuth2 Client for this integration; otherwise use Client Credentials instead. |
| [N3] | A fixed-credential signal in the file (`flowDecision` is `FixedSignal`, `flow` is `ClientCredentials`). | A fixed-credential signal in this file points to Client Credentials, but how often that kind of signal is right has not been measured. Use Authorization Code instead if the integration acts on behalf of a signed-in user, or if your RelativityOne instance already has a Code-flow OAuth2 Client for this integration. |
| [N4] | An interactive-user signal in the file (`flowDecision` is `InteractiveSignal`, `flow` is `AuthorizationCode`). | An interactive-user signal in this file points to Authorization Code. Confirm the flow: use Client Credentials instead if the integration runs with credentials fixed for the integration and no signed-in user. |
<!-- generated:end flow-notes -->

The tool chooses only from signals in the finding's own file. It cannot see a login page or a login handler in another file, or a credential that arrives through configuration binding or dependency injection. For most real integrations the credential reaches the finding's file from another file, so `Default` is the expected decision, and its note is the guide. See [Limits of the tool](03-limits.md#the-flow-is-chosen-from-the-findings-own-file), and [Confirming the flow](../migration/part-c-csharp.md#confirming-the-flow) in Part C for how to decide by hand.

#### The recommendation

The `recommendation` of a High or Medium finding is the same text as the matching entry in [Part C](../migration/part-c-csharp.md), and is composed in pieces. In the Markdown report each piece has its own label:

- **Source form** shows the construct in generic form.
- **Under token authentication** is the change at the point of use. Part C and the recommendation call this "part 1".
- **Credentials** says where the credentials come from and how to obtain a token: the token acquisition, which is "part 2".
- **Verification** says what to check before you deploy.
- **OAuth2 Client** lists the settings of the OAuth2 Client for the chosen flow. The recommendation calls this "part 3". The same settings are in [Configuring the OAuth2 Client](05-configuring-the-oauth2-client.md).

In the JSON file the same pieces are joined in the one `recommendation` string. When a pattern class has no template of its own for the flow, the finding carries the generic recommendation for the flow, and the OAuth2 Client piece is still present.

#### Secret warnings

A finding whose match is a credential written literally into the source carries a security note, and `literalCredentialSignal` is `true`. The note is separate from the migration: treat the credential as compromised, rotate it now, and remember that deleting the line does not undo the exposure, because the repository history has published it. Store its replacement as [Secret Management](../migration/part-b-secret-management.md) describes. A literal credential is never evidence of which flow applies.

### Findings limits

JSON `findingsCap`; the Findings section of the report.

The tool limits how many findings it reports for one file and for the whole scan (see [Limits of the tool](03-limits.md#the-findings-limits)). `findingsCap` records whether a limit applied: `truncated`, the number `totalRecognized`, the number `retained`, and, for each limit, `perFileCapTrippedFileCount` and `perFileOmittedCount` (the files that reached the per-file limit and how many findings they lost), and `globalCapTripped` and `globalOmittedCount`. When a limit applied, the report says how many findings are counted and not listed.

### Provenance

Report section **5. Provenance**; JSON `provenance`.

What the run was: `runDate`, `toolVersion`, `catalogVersion`, `repositoryLabel` (the name of the repository folder, never a full path), the `enabledLanguages`, the `minimumConfidenceLevel`, the `defaultFlow`, and where each setting came from (`minimumConfidenceLevelOrigin`, `enabledLanguagesOrigin`, `outputPathOrigin`, `defaultFlowOrigin`: the command line, your user profile file, the repository file or the built-in default). `configurationFiles[]` says, for the repository root and the user profile, the `condition` of the file at each `location`: absent, read and applied, found and malformed, or found and unreadable (in the last two the file was not applied, and each setting came from the next source). `ignoredRepositoryFields[]` lists any field of the repository's file that the tool declined to apply, with the `field`, the `reason`, and `wouldHaveBeen`, what the repository asked for when that is safe to repeat. `repositoryConfigurationEffect` says what the repository's file changed against what would have applied without it: `languagesSetByRepository`, `languagesDisabledByRepository` and `languagesEnabledByRepository`, and a `thresholdChange` (null when there was none) with the `previous` level, the `current` level, the `direction`, `Raised` or `Lowered`, and the `hiddenFindingCount`: how many findings a raise hides that the previous level would have shown (zero for a `Lowered` change).

The same input, configuration and tool version produce the same findings, identifiers, levels and flows, on every platform, with the one exception for file names in decomposed Unicode form that [Limits of the tool](03-limits.md#file-names-in-decomposed-unicode-form) describes.

### Values of the JSON fields

Every field of the JSON file that takes one of a fixed set of values is listed here with the values exactly as the tool writes them, including their letter case. A field that can be empty says so. Fields that hold numbers, paths, dates and free text are not repeated.

| JSON field | Values |
| --- | --- |
| `language` (in `coverage.languages[]`, `scanSummary.languages[]`, `findings[]`) | `CSharp`, `VBNet`, `JavaScript`, `TypeScript`, `Python`, `Go`, `PowerShell`, `Shell`. In `scanSummary.languages[]` also `unrecognized`, for files whose language the tool could not identify. |
| `coverage.languages[].coverage` | `supported, <n> classes searched` (`<n>` is `patternClassesSearched`), `declared unsupported in this release` or `disabled by configuration`. |
| `scanSummary.languages[].assessed` | `true`, `false`, or null for `unrecognized`. |
| `scanSummary.enabledLanguagesDifferFromSession` | `true` or `false`; always present. `true` when the languages enabled when `scan` ran differ from those enabled in the session that wrote the report. |
| `scanSummary.languages[].notAssessedReason` | `noAnalyzer` or `disabledByConfiguration`; null when `assessed` is `true` or null. |
| `skippedItems[].unit` | `files` or `directories`. |
| `findings[].confidence`, `provenance.minimumConfidenceLevel`, `thresholdChange.previous` and `thresholdChange.current` | `High`, `Medium` or `Low`. |
| `findings[].destinationCertainty` | `established` or `unknown`. |
| `findings[].flow`, `provenance.defaultFlow` | `ClientCredentials` or `AuthorizationCode`; `flow` is null for a Low finding. |
| `findings[].flowDecision` | `InteractiveSignal`, `FixedSignal` or `Default`; null for a Low finding. |
| `findings[].testCode` | `true` or `false`; present on every finding. |
| `findings[].lowReasons` | A list of `patternClass`, `commentOrStringLiteral`, `demotedFile`, `demotedMockFile`, `demotedSampleFile`, `demotedVendoredFile`, `demotedGeneratedFile` and `demotedDeploymentFolder`, in that order when several apply; empty when the reason was not recorded; null for a finding that is not Low. |
| `findings[].patternClass` | `CS-001` to `CS-019`, without `CS-010`, for C#; `PS-001` to `PS-014` for PowerShell; `VB-001` to `VB-019` for VB.NET; `SH-001` to `SH-025` for Shell. See Part A ([C#](../migration/part-a-csharp.md#pattern-classes), [PowerShell](../migration/part-a-powershell.md#pattern-classes), [VB.NET](../migration/part-a-vbnet.md#pattern-classes), [Shell](../migration/part-a-shell.md#pattern-classes)). |
| `provenance.minimumConfidenceLevelOrigin`, `provenance.enabledLanguagesOrigin` | `built-in default`, `the configuration file in the user profile` or `the configuration file in the repository`. |
| `provenance.outputPathOrigin` | `the command line`, `the configuration file in the user profile` or `the working directory captured at launch`. |
| `provenance.defaultFlowOrigin` | `BuiltInDefault` or `Profile`. The other origin fields use the phrases above, and this one uses a code. |
| `configurationFiles[].location` | `repository root` or `user profile`. |
| `configurationFiles[].condition` | `absent`, `read and applied`, `found and malformed, so not applied` or `found and unreadable, so not applied`. |
| `ignoredRepositoryFields[].field` | `output path` or `default flow`. |
| `ignoredRepositoryFields[].reason` | `never read from the repository under assessment`. |
| `ignoredRepositoryFields[].wouldHaveBeen` | An empty string in this release: the tool does not repeat what the repository asked for. |
| `thresholdChange.direction` | `Raised` or `Lowered`. |

The `category` of each entry of `skippedItems[]` is a short phrase, given here with the `description` that goes with it:

| `category` | `description` |
| --- | --- |
| `could not be opened for reading` | Files that could not be opened |
| `directory whose contents could not be listed` | Directories whose contents could not be listed |
| `link, junction or reparse point, which is never followed` | Links to other locations, which are not followed |
| `could not be decoded as text` | Files that could not be read as text |
| `above the file size ceiling` | Files larger than this release analyzes |
| `carries a line above the line length ceiling` | Files with a line longer than this release analyzes |
| `path refused by the platform for its length` | Files whose path is too long for the operating system to open |
| `lexical scan abandoned part-way through the file` | Files only partly readable as code, so not analyzed in full |
| `matching stopped before every rule had run` | Files whose analysis took too long and was stopped; their findings are not reported |
| `a rule's match could not be trusted and was dropped` | Files where at least one unreliable match was discarded; their other findings are reported |
| `recorded during the scan and no longer present` | Files found during the scan and no longer present when analyzed |

`shareOfFilesPercentage` is a number that is not rounded, for example `16.666666666666668`. The JSON file holds no totals by confidence or by flow; the Markdown report and the console give them.

## Where the outputs go

The `report` stage writes into the output folder (the `--documentationDestinationFolderPath` option, else `outputPath` from your user profile file, else the folder you started the tool from):

- `basic-auth-migration-YYYY-MM-DD.md`, the Markdown report;
- `basic-auth-migration-YYYY-MM-DD.json`, the JSON data file.

The date is the day you ran the report. The tool never overwrites an earlier report: a second report on the same day is written as `basic-auth-migration-YYYY-MM-DD-2.md` and `.json`, then `-3`, and so on. The files are written completely or not at all. As each file is written, the tool prints its name and the name of the output folder: `Written: <file name> (in the output folder <folder name>).`

The output folder also holds the saved state of the session, in a file whose name begins `migrate-auth.session.`. It is safe to delete when you want the next session to start fresh. The tool leaves nothing else behind, and writes nothing inside the repository.

## Exit codes

When the tool ends it returns an exit code. A session that ends normally, including a run with no findings, returns 0.

| Code | Name | Meaning |
| --- | --- | --- |
| 0 | `Success` | The session ended normally. A completed run that found nothing is a success. |
| 1 | `UnusableRepositoryPath` | The repository path is unusable: none was given (the tool prints the usage line), or it does not exist, is not a folder, or cannot be opened. |
| 2 | `SequentialGatingViolation` | A stage was invoked before its prerequisite stage had completed. In the interactive session the tool answers `Run '<stage>' first.` and stays at the prompt, so this release does not exit with this code. The number is reserved. |
| 3 | `UnrecognizedCommand` | The command entered is not recognized. In the interactive session the tool answers `command not found: <word>` and stays at the prompt, so this release does not exit with this code. The number is reserved. |
| 4 | `OutputDestinationNotWritable` | The output folder cannot be created or written to, or it is the repository, inside it, or a folder that contains it. |
| 5 | `UnexpectedInternalError` | An unexpected internal error. The tool prints a message and ends. |
| 6 | `NoLanguageEnabled` | The effective configuration enables none of the eight languages, so no session is started. |

The codes are stable: a number is never given a new meaning in a later release.

## Where to go next

- Part A ([C#](../migration/part-a-csharp.md), [PowerShell](../migration/part-a-powershell.md), [VB.NET](../migration/part-a-vbnet.md), [Shell](../migration/part-a-shell.md)): what each pattern looks like, how to search for it yourself, and how the tool assigns confidence and a flow.
- Part C ([C#](../migration/part-c-csharp.md), [PowerShell](../migration/part-c-powershell.md), [VB.NET](../migration/part-c-vbnet.md), [Shell](../migration/part-c-shell.md)): the migration suggestion for each pattern and each flow, the same text as the tool's recommendation.
- [Part B](../migration/part-b-secret-management.md): where the new secrets go.
- [Limits of the tool](03-limits.md) and the [release notes](02-release-notes.md).
