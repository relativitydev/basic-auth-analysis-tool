# Limits of the tool (User Guide, section 3 of 8)

This section says what the tool does not do, how well it did on its test repository, and what a result does and does not mean. Read it before you rely on a report.

## Languages delivered so far

This release analyzes **C#, PowerShell, VB.NET and Shell**. The other four languages of the plan (JavaScript, TypeScript, Python and Go) are delivered one at a time; see [What's next](02-release-notes.md#whats-next).

**Files in languages that are not delivered yet are not analyzed. An empty result for them does not mean that no Basic Authentication was found.** The scan summary of the report counts those files so that you can see how many there are. The same holds inside C# and PowerShell: the tool reports what its catalogs search for, and a run with no findings means that none of the constructs the catalogs cover was found in the C#, PowerShell, VB.NET and Shell files it could read. A run that finds nothing completes normally, with exit code 0, and its report says that it found zero findings.

## Folders the walk does not enter

The walk does not enter eight folder names, matched exactly and in lower case at any depth: `bin`, `obj`, `.git`, `.vs`, `node_modules`, `dist`, `build` and `packages`. Nothing in them is read, so hand-written code or scripts kept in a folder with one of those names are not assessed. The report names the excluded folders and counts them, so the gap is visible; they are not skipped items. No other folder name is excluded. See [What the scan reads and what it leaves out](01-using-the-tool.md#what-the-scan-reads-and-what-it-leaves-out).

## Files that are too large

The tool analyzes a file only if it is at most 2 MiB (2,097,152 bytes) and its longest line has at most 10,000 characters. A file above either limit is not analyzed at all, and the report counts it under **Files larger than this release analyzes** or **Files with a line longer than this release analyzes** and names it. Review such a file by hand. A minified or machine-written file is the usual cause.

## Text encodings

The tool reads a source file as UTF-8, UTF-16 or UTF-32 (the last two with a byte order mark, and UTF-16 also without one). A file that is none of these is read as **Windows-1252**, which covers Latin-1 and the other Western European files, and the report counts and names these files so that the reading is never silent. The tool cannot tell which single-byte code page a file was written in, so a file in another one, such as a Cyrillic or Central European code page, is read with its non-ASCII characters changed, or is counted as a file that could not be read as text when it holds a byte Windows-1252 leaves undefined. A credential written in plain ASCII is found either way. See the [release notes](02-release-notes.md#known-issues).

## What the catalogs declare they do not cover

Each catalog declares what it does not cover, and the report prints the same list in its Coverage section. The lists are in [Part A for C#, Declared unsupported coverage](../migration/part-a-csharp.md#declared-unsupported-coverage), [Part A for PowerShell, Declared unsupported coverage](../migration/part-a-powershell.md#declared-unsupported-coverage), [Part A for VB.NET, Declared unsupported coverage](../migration/part-a-vbnet.md#declared-unsupported-coverage) and [Part A for Shell, Declared unsupported coverage](../migration/part-a-shell.md#declared-unsupported-coverage). A construct on a list produces no finding; review those areas by hand, or search for them with the patterns in Part A.

**C#.** The list includes code in Razor views and in other .NET languages, credentials in configuration files, credentials passed through dependency injection or configuration binding, a user name and password written into a URL, calls made by compiled libraries, code inside preprocessor conditional blocks, and files in single-byte code pages other than Windows-1252. The forms below are the ones this release's catalog declares as not read, so check them by hand:

- **A scheme held in a named constant written with its type or namespace in front of it** is not read when it is the scheme argument of a credential-cache entry, of an `AuthenticationHeaderValue` or of a header written by name (`CS-003`, `CS-005`, `CS-006`), nor in the concatenated and interpolated spellings of `CS-006`. It is read in a header-enumeration write (`CS-012`). A constant that carries both the scheme and the space and is concatenated directly with the credential, and a `string.Format` over the constant, are not read.
- **A type, client or credential renamed with a `using` alias**, or an enumeration member or conversion reached through `using static` without its type, is not recognized, and a file that does nothing else is missed. The spellings with the namespace written in front of the type name are read.
- **A credential or client created with the short `new(...)` form**, where the type name does not stand in front of the variable (an assignment to an existing variable, a return, an argument or a property initializer), is not read. The form with the declared type in front of the variable is read where [Part A](../migration/part-a-csharp.md) says so.
- **A credential cache entry added by a collection initializer**, and **a credential created by an object initializer that carries both the user name and the password in the one initializer**, are not read by the classes for those constructs, so a file that holds only such a construct can be missed.
- **A password control reached through a call with a nested call in its argument, a constructed operand or a parenthesized operand** (`Find(Get("x")) as PasswordBox`) is not used as evidence that a person signs in. The finding is then decided as if that evidence were absent, which can only move the result toward the default flow.
- **A mail, file-transfer, directory or proxy credential whose client is renamed with a `using` alias, or is created with the short `new(...)` form where the type name does not stand in front of the variable,** is reported at Medium as a plain credential construction and not at Low as a non-HTTP credential, because the client's type name is not in the text. A file-transfer request is read as the non-HTTP case whether it is obtained from `WebRequest.Create(...)` or from `FtpWebRequest.Create(...)`, and whether its type is given by a cast in front of the call or by `as FtpWebRequest` after it.
- **The standard library's Base64 conversion called without its type name** through a `using static` directive is not read as the end of a credential pair.

**PowerShell.** The list in Part A has the full text. In short, the tool does not read:

- credentials, flow evidence, a header table or a header scheme that comes from another file: a dot-sourced script, an imported module, a loaded configuration or data file, a value passed in as a parameter, a splatted table defined more than twenty lines before its use or built at run time, and defaults supplied through `$PSDefaultParameterValues`; and secret-store commands other than the three the tool lists;
- a credential object created in a form it cannot see: a hashtable cast or `-Property` on an SDK credential type, reflection or a computed type name, a credential built in one statement and assigned to a handler by variable, an SDK entry point built directly from a user name and a password;
- a header written with a key or a value held in a variable that does not name the scheme close by, and a scheme held in a variable that cannot be read as a constant: the finding is then kept at a lower level, so a Bearer header held that way can be reported as a possible Basic header and should be dismissed on review;
- a user name and password written into a URL, Basic Authentication performed by a program the script calls (`curl.exe -u`, `wget --user`), and Basic Authentication through other stacks that take the credential as a parameter, such as a COM request or `Start-BitsTransfer -CustomHeaders`;
- a header declared as text and converted into a table at run time (`ConvertFrom-StringData`, `ConvertFrom-Json`), which is reported only through its Base64 step, when one is visible;
- PowerShell data and configuration files (`.psd1` manifests, DSC documents, `.ps1xml`, `.psc1`, `.cdxml`, `.mof`), PowerShell inside another file type, and any code inside an `Add-Type -TypeDefinition` here-string, a `-Command` string or an `Invoke-Expression` argument: the inner code is read as the text of its string;
- a command name written so that the tool cannot read it: a backtick inside the name, a computed name invoked with `&`, a user-defined alias for a web cmdlet;
- a call written inside the `$( ... )` of an expandable here-string (`@"` ... `"@`), and a string delimited by a typographic quote other than the two pairs the tool reads, which can make the tool misread the rest of the file;
- a script in a single-byte code page other than Windows-1252 (see [Text encodings](#text-encodings)).

**VB.NET.** The list in Part A has the full text. In short, the tool does not read:

- **Visual Basic 6, VBA, VBScript and other Basic dialects.** Files ending in `.vbs`, `.bas`, `.cls`, `.frm` or `.vbhtml`, and classic ASP, are a different language from VB.NET. The tool does not attribute them to VB.NET, and they stay in the list of files that were not assessed, so a repository whose Basic Authentication is in such files needs a hand search.
- **VB.NET written inside markup.** The server-side script blocks and the inline code of `.aspx`, `.ascx` and `.master` files are not scanned. The code-behind file of the same page, which ends in `.aspx.vb`, is.
- **XML literals**, and any code inside them. An apostrophe in their text is read as a comment, and an odd double quote can make the tool read the rest of the file as text.
- **A string delimited by a mixed pair of typographic quotes**, a typographic interpolated string, and an `Authorization` header key written between typographic quotes.
- **Code inside a disabled conditional-compilation branch.** The tool reads a block under a `#If` condition that is false as code, so a construct in it is reported as if it were live.
- credentials, a scheme or flow evidence supplied from another file (a partial class or module, a code-behind page whose markup holds the login control, project-level imports, a loaded configuration file), and credentials in configuration files such as `app.config`, `web.config`, `.settings` files, `appsettings.json` and `.resx` resources;
- a credential or a client created in a form the tool cannot see: an SDK credential created by an object initializer, reflection or `Activator.CreateInstance`, a type renamed with an `Imports` alias of the type, a credential built across statements, a credential built with a domain as its third argument, and arguments written out of order with names;
- a user name and password joined by a colon and encoded by the project's own Base64 helper, an encoder called without its type name, or a separator held in a constant; a user name and password written into a URL; and Basic Authentication performed by a native program the code starts;
- an `Authorization` header written with a value held in a variable or a parameter whose name carries no credential word;
- a source file saved in a single-byte code page other than Windows-1252 (see [Text encodings](#text-encodings)).

**Shell.** The list in Part A has the full text. [Shell: what the tool reads and what it does not](#shell-what-the-tool-reads-and-what-it-does-not) says what each item means for a result.

### Variables inside double-quoted strings

In PowerShell only `$( ... )` inside a double-quoted string is read as code. A variable written as `$name` or `${name}` inside such a string is read as part of the string's text. No construct can hide in a bare variable name, so detection is not affected, but a `${...}` whose name itself contains a quote character can end the string early for the tool and not for PowerShell, and the tool then reads the rest of the string wrongly.

## Accuracy thresholds and what was measured

The tool is measured against a benchmark test repository that holds planted Basic Authentication patterns and deliberate near-misses (constructs that resemble Basic Authentication and are not). The targets are these:

| Measure | Target |
| --- | --- |
| Recall on planted patterns the catalog claims to support | at least 90 % reported at High or Medium |
| False-positive rate on near-misses | at most 10 % reported at High or Medium |
| Recall floor for each language | at least 80 % |
| Recommendation coverage | 100 %: every High or Medium finding carries a recommendation for its flow |
| Flow-assignment accuracy | at least 90 % overall and at least 90 % for each expected flow |

These are NSerio's recommended starting thresholds. They have not been confirmed by Relativity.

### What the figures cover

The figures below were measured for the previous release, `0.3.0-alpha`, on its detection catalog (catalog `1.19.0`), for C#, for PowerShell and for VB.NET. This release reports test code at Medium at most instead of Low (test code that is also a mock, a sample, vendored or generated stays Low), corrects how VB.NET is read and adds Shell, and the figures below are those of the previous release's measurement; the measurement of this release is not published here: **no figure is stated for Shell, and the VB.NET figures predate the VB.NET corrections.** They were measured on Windows and on Linux, and the two runs produced byte-identical results. The Linux run used a virtual machine on the same computer as the Windows run, so it is another operating system and not other hardware. **The measurement on macOS is pending.** Percentages are rounded to one decimal. Beside each rate the tables give the lower end of its 95 % Wilson confidence interval, because the populations are small.

**The PowerShell judgement is bounded, not confirmed.** The PowerShell catalog rests on one real repository, which holds very few cases. The planted items of the benchmark were written from the same documentation the catalog describes. So the figures say how consistent the tool is with its own catalog. They do **not** say how it performs on real scripts. A separate small set of 12 deliberately tricky near-miss scripts, written to be hard for a matcher that reads one file at a time, had 7 of 12 = 58.3 % reported at High or Medium. That is far above the rate in the tables, and it shows that the near-misses of the benchmark are easier than adversarial ones.

**"Blind" has a narrow meaning here.** A set of planted items is blind when it was planted without sight of the tool's signal expressions or of the analyzer source. It does **not** mean blind to the catalog. A figure on a blind set still measures consistency with the catalog.

### PowerShell

PowerShell figures are measured on two sets of planted items, both blind in the sense above. **The newest set is the independent one.** It was planted from the PowerShell catalog of this release and set aside before any correction to the detection was written. The older set was used, after the previous measurement, to decide what to correct, through the verdict on each item and never its content; so an item of the older set that is found now and was not found then is not independent evidence. The first table gives the figures of the newest set against each target.

| Measure | Result | Lower bound (95 %) | Target |
| --- | --- | --- | --- |
| Recall on catalogued patterns | 72 of 72 = 100.0 % | 94.9 % | at least 90 %: met |
| False-positive rate on near-misses | 2 of 34 = 5.9 % | 1.6 % (the upper end of the interval is 19.1 %) | at most 10 %: met |
| Flow accuracy, overall | 72 of 72 = 100.0 % | 94.9 % | at least 90 %: met |
| Flow accuracy, expected Authorization Code | 24 of 24 = 100.0 % | 86.2 % | at least 90 %: met; the lower bound lies below 90 % |
| Flow accuracy, expected Client Credentials | 48 of 48 = 100.0 % | 92.6 % | at least 90 %: met |
| Recommendation coverage for the assigned flow | 72 of 72 = 100.0 % | 94.9 % | 100 %: met |

The two near-misses reported at Medium are of kinds the PowerShell catalog documents it reports at Medium: a bearer-style header assembled from parts, and a colon-joined pair of values that is not a credential. No other near-miss of this set was reported at High or Medium. Every pattern of this set that the catalog covers was found. The 34 near-misses are a small population, which is why the upper end of the interval is above the 10 % target.

On the older set, measured again with the previous release, recall on catalogued patterns was 70 of 72 = 97.2 % (lower bound 90.4 %), the false-positive rate was 1 of 49 = 2.0 % (lower bound 0.4 %, upper end 10.7 %), flow accuracy was 70 of 70 = 100.0 % (lower bound 94.8 %; 22 of 22 = 100.0 % for expected Authorization Code, lower bound 85.1 %, and 48 of 48 = 100.0 % for expected Client Credentials, lower bound 92.6 %) and recommendation coverage for the assigned flow was 70 of 70 = 100.0 % (lower bound 94.8 %). Two patterns of that set are still not matched; they are recorded for the catalog's maintainers.

Both sets together:

| Measure | Result | Lower bound (95 %) | Target |
| --- | --- | --- | --- |
| Recall on catalogued patterns | 142 of 144 = 98.6 % | 95.1 % | at least 90 %: met |
| False-positive rate on near-misses | 3 of 83 = 3.6 % | 1.2 % (the upper end of the interval is 10.1 %) | at most 10 %: met |
| Flow accuracy, overall | 142 of 142 = 100.0 % | 97.4 % | at least 90 %: met |
| Flow accuracy, expected Authorization Code | 46 of 46 = 100.0 % | 92.3 % | at least 90 %: met |
| Flow accuracy, expected Client Credentials | 96 of 96 = 100.0 % | 96.2 % | at least 90 %: met |
| Recommendation coverage, every High and Medium finding of the scan | 150 of 150 = 100.0 % | 97.5 % | 100 %: met |

The last row counts a different thing from the rows above it. The rows above count planted items (144 for recall, 142 of them with an expected flow). The coverage row counts findings: both sets were planted in one tree and scanned once, and that scan reported 150 PowerShell findings at High or Medium, which includes findings that are not the one expected for a planted item, such as those on near-misses. Every one of the 150 carries a recommendation. The recommendation coverage for the assigned flow of the planted items is 72 of 72 and 70 of 70, as stated above.

Read a 100 % in a small population as "no miss in that population", not as a guarantee.

### C#

**Recall and false positives on the first set of planted items are not blind.** Those items were read while the catalog was being diagnosed, so the two figures are not independent evidence. They are unchanged by this release.

| Measure | Result | Lower bound (95 %) | Target |
| --- | --- | --- | --- |
| Recall on catalogued patterns, without the one manual-review class (`CS-011`) | 30 of 33 = 90.9 % | 76.4 % | at least 90 %: met, with no margin |
| Recall with that class included | 30 of 34 = 88.2 % | 73.4 % | reported for reference; not gated |
| False-positive rate on near-misses | 0 of 28 = 0.0 % | 0.0 % (the upper end of the interval is 12.1 %) | at most 10 %: met |

`CS-011` is the class that marks a helper that builds an authentication header. It is always routed to manual review, so it does not count toward the recall that is gated. The three catalogued patterns that were not found are recorded catalog gaps.

**Flow accuracy and recommendation coverage are measured on the newer sets of planted items**, which were planted blind in the sense above. The newest set was planted from the catalog of this release. These 151 files were measured blind.

| Measure | Result | Lower bound (95 %) | Target |
| --- | --- | --- | --- |
| Flow accuracy, overall | 151 of 151 = 100.0 % | 97.5 % | at least 90 %: met |
| Flow accuracy, expected Authorization Code | 42 of 42 = 100.0 % | 91.6 % | at least 90 %: met |
| Flow accuracy, expected Client Credentials | 109 of 109 = 100.0 % | 96.6 % | at least 90 %: met |
| Recommendation coverage for the assigned flow | 151 of 151 = 100.0 % | 97.5 % | 100 %: met |

On the newest set alone, which is blind, recall on catalogued patterns was 19 of 20 = 95.0 % (lower bound 76.4 %) and the false-positive rate was 0 of 41 = 0.0 % (lower bound 0.0 %, upper end 8.6 %). One catalogued pattern of that set was not found; it is recorded for the catalog's maintainers. These two figures are reported for information and are not gated. On the set before it, measured again with the previous release, recall was 51 of 51 = 100.0 % (lower bound 93.0 %) and the false-positive rate was 0 of 21 = 0.0 % (lower bound 0.0 %, upper end 15.5 %); that set was used to decide what to correct after the previous measurement, which had reported two of its near-misses at Medium, so its improvement is not independent evidence.

### VB.NET

**The VB.NET judgement is bounded, not confirmed.** No VB.NET repository was available to calibrate the catalog. It was written from Relativity's and Microsoft's documentation and from C# code converted to VB.NET, and the planted items of the benchmark were written from the catalog. So the figures say how consistent the tool is with its own catalog. They do **not** say how it performs on real VB.NET code.

VB.NET is measured on two sets of planted items. **The newer set is the independent one.** It was planted from the VB.NET catalog by a writer who had not seen the tool's expressions, and set aside before any correction to the detection was written. The older set was used afterwards to decide what to correct, so what the corrections fixed in it is not independent evidence. Both sets together are the full VB.NET benchmark, and the first table gives their figures against each target. **Beside each recall figure, the unrestricted recall counts also the planted patterns that the catalog declares it does not cover**, which the gated figure leaves out.

| Measure | Result | Lower bound (95 %) | Target |
| --- | --- | --- | --- |
| Recall on catalogued patterns | 242 of 246 = 98.4 % | 95.9 % | at least 90 %: met |
| Unrestricted recall, counting also the patterns the catalog declares it does not cover | 252 of 286 = 88.1 % | not stated | reported beside the figure above; not gated |
| False-positive rate on near-misses | 13 of 139 = 9.4 % | 5.5 % (the upper end of the interval is 15.3 %) | at most 10 %: met, by the point value |
| Flow accuracy, overall | 242 of 242 = 100.0 % | 98.4 % | at least 90 %: met |
| Flow accuracy, expected Authorization Code | 55 of 55 = 100.0 % | 93.5 % | at least 90 %: met |
| Flow accuracy, expected Client Credentials | 187 of 187 = 100.0 % | 98.0 % | at least 90 %: met |
| Recommendation coverage, every High and Medium finding of the scan | 280 of 280 = 100.0 % | not stated | 100 %: met |

**On the independent set alone the false-positive ceiling is missed.** On that set recall on catalogued patterns was 58 of 61 = 95.1 % (lower bound 86.5 %), and unrestricted recall was 58 of 66 = 87.9 %. Flow accuracy was 58 of 58 = 100.0 %. The false-positive rate was **11 of 55 = 20.0 %** (lower bound 11.6 %, upper end 32.4 %), against the target of at most 10 %. Nine of those eleven near-misses are costs that the catalog declares and that this page and Part A describe: a credential handed to a share or mail consumer the tool does not list, a bearer value held in a variable whose name carries `token`, a host joined to a value that is not a port, a non-credential pair that is encoded, a type declared in another file with an SDK type's name, and a name declared in two procedures. Two are not declared costs: a system token given as the second argument of the Import API constructor, written as a call whose name carries `Token`, and a type of your own with the Import API type's name, declared in the same file. The older set, which the corrections were written against, reports 2 of 84 = 2.4 %, and that is why the rate over both sets is inside the target. **Read the VB.NET false-positive rate as met over the whole benchmark and missed on its independent part.** Expect to dismiss a fair share of the Medium findings on review, and see [VB.NET findings that are not Basic Authentication](#vbnet-findings-that-are-not-basic-authentication).

The recommendations of VB.NET were compiled and the token-acquisition code was run against a local test server; see the [release notes](02-release-notes.md#known-issues). For the previous release the newest planted items gave these results: C# recall 7 of 7 and false positives 0 of 7, and PowerShell recall 15 of 15 and false positives 0 of 13.

### All the planted items together

The aggregate adds the two PowerShell sets and the four C# sets. It does not include VB.NET, whose figures are in their own subsection. It mixes sets that gate and sets that do not, and it includes the first C# set, which is not blind. It is reported beside the per-language figures and is never a gate.

| Measure | Result | Lower bound (95 %) |
| --- | --- | --- |
| Recall | 323 of 330 = 97.9 % | 95.7 % |
| False-positive rate | 3 of 190 = 1.6 % | 0.5 % |
| Flow accuracy, overall | 323 of 323 = 100.0 % | 98.8 % |
| Flow accuracy, expected Authorization Code | 88 of 88 = 100.0 % | 95.8 % |
| Flow accuracy, expected Client Credentials | 235 of 235 = 100.0 % | 98.4 % |
| Recommendation coverage, every High and Medium finding of the scan | 340 of 340 = 100.0 % | 98.9 % |

For completeness, these figures are disclosed and do **not** gate. For C#, recall over all planted items of 181 of 186 = 97.3 %, detection on the newer sets of 151 of 153 = 98.7 %, and flow assignment on the first set of 30 of 30 = 100.0 %. For the constructs that the catalogs declare they do not cover, the planted items were reported at High or Medium in 2 of 23 cases for PowerShell and in 6 of 27 cases for C#; that is expected, and it is the reason to review those areas by hand.

**These figures will be updated.** A later re-measurement, after corrections to detection, will replace them. This release makes no promise about the figures of a later release.

## Low confidence needs review

A finding at **Low** confidence is present in the text but unlikely to be live authenticating code: a comment, a documentation string, a mock, fake or stub, sample or example code, vendored code, or a generated file. **Test code is not Low:** a finding in a file the tool recognizes as test code is reported at Medium at most, with a recommendation, and the report marks it as test code; a file that is test code and also a mock, a sample, vendored or generated is Low. Sample and example code means a folder named `sample`, `samples`, `example` or `examples`, or a file whose name begins with `Sample` or `Example`; vendored code means a folder named `vendor` or `third-party`. A sample kept under another name, such as a folder called `demo`, is not recognized and is reported at its ordinary level. A folder named `Deployment` lowers a C# finding to Low and does not lower a PowerShell, a VB.NET or a Shell finding: a script or source file under that folder is reported at its full level. Part A lists every signal, for [C#](../migration/part-a-csharp.md#how-confidence-is-assigned), for [PowerShell](../migration/part-a-powershell.md#how-confidence-is-assigned), for [VB.NET](../migration/part-a-vbnet.md#how-confidence-is-assigned) and for [Shell](../migration/part-a-shell.md#how-confidence-is-assigned). The tool gives a Low finding no flow and no recommendation and routes it to manual review. It is not evidence that the code does not authenticate. [Part C](../migration/part-c-csharp.md#judging-a-low-confidence-match) explains how to judge each cause.

## The flow is chosen from the finding's own file

The flow is decided for each file, not for each finding and not for the repository: the tool decides between Client Credentials and Authorization Code from signals in the same file as the finding, and only there, and every finding of one file takes that file's flow. A login page, a login handler or a credential prompt in a different file is not seen, and neither is a script dot-sourced or a module imported by a PowerShell script, and neither is a credential that arrives through dependency injection or configuration binding, nor the markup of a web page whose code-behind is the file being read. In real code the credential usually reaches the finding's file from another file, so **`Default` is the expected decision for most real integrations**: the finding gets the default flow, and its note asks you who supplies the credential at run time. That is not a failure of the tool; it is the reason the note exists. [Confirming the flow](../migration/part-c-csharp.md#confirming-the-flow) in Part C explains how to decide by hand, and [Part C for Shell](../migration/part-c-shell.md) does the same for a Shell script. Pointing to evidence found elsewhere in the same repository is planned after the alpha.

## A script with both a stored credential and a prompt

When one file holds both interactive evidence (a credential prompt such as `Get-Credential`, a console prompt for a secret, a desktop password control) and fixed-credential evidence (a stored credential read from the environment, from a credential file or from a secret store, or a credential built from a plain-text value), the tool assigns **Authorization Code** to the file. The same rule applies to C#. A common PowerShell script reads a stored credential and falls back to `Get-Credential` only when none is stored: it runs unattended in its normal use, so Client Credentials is usually the better choice, and the tool recommends Authorization Code for it anyway. The note on the finding asks you who supplies the credential at run time; answer it, and apply the other flow where the script normally runs without a person. [Confirming the flow](../migration/part-c-powershell.md#confirming-the-flow) in Part C for PowerShell explains how to decide by hand. A Shell script that reads a credential from the environment and falls back to a silent `read -s` when it is unset is resolved in the same way, to Authorization Code; [Confirming the flow](../migration/part-c-shell.md#confirming-the-flow) in Part C for Shell explains how to decide by hand.

## VB.NET: Web Forms pages and a file with both kinds of evidence

**A Web Forms page is decided by its code-behind alone.** A page whose login control is declared in the `.aspx` markup and whose code is in the `.aspx.vb` file reaches the tool as the code-behind file only, because the markup is not scanned. If the code-behind holds no login handler of its own, the finding takes the default flow, Client Credentials, although the page acts for a signed-in user. The same happens to any VB.NET file whose evidence of how a user signs in is in another file, such as a partial class or a module defined elsewhere. For code that serves a signed-in user, use Authorization Code whatever the tool recommended. [Confirming the flow](../migration/part-c-vbnet.md#confirming-the-flow) in Part C for VB.NET explains how to decide by hand.

**A file that holds interactive evidence and fixed-credential evidence receives Authorization Code.** This is the same rule as in the other languages, and in VB.NET it also reaches a file that merely contains two unrelated pieces of code, one that serves a signed-in user and one that runs unattended, such as a module that holds both a login handler and a scheduled job. If the match is in the unattended part, Client Credentials is the right flow. The note on the finding asks who supplies the credential at run time; answer it and apply the other flow where the code normally runs without a person.

## VB.NET: how the tool reads quotes and conditional blocks

**A double quote the tool does not see closed turns the rest of the file into text.** When the tool reads a VB.NET file it marks where each comment and string begins and ends. A stray or odd double quote, for example inside an XML literal, inside a long string that the tool pairs differently from the compiler, or inside a block switched off with `#If`, can make it read everything after that point as the inside of a string. Every match after the quote is then reported at Low, with no flow and no recommendation. If a VB.NET file reports Low findings below some line and you expect higher ones, look for an unbalanced double quote above that line, fix or review it, and read the file by hand from there.

**A disabled `#If` branch is read as code.** The tool does not evaluate conditional compilation. A block under a condition that is false, such as `#If False Then`, is read like any other code, so a construct in it is reported at its ordinary level although it is never compiled. A double quote in such a block can also end the analysis of the rest of the file, as described above. Check whether the branch is built before you migrate a finding in it.

## VB.NET findings that are not Basic Authentication

The tool reports some VB.NET code that does not perform Basic Authentication towards RelativityOne, and misses or classes differently a few forms that do. Each item says what you see and what to do.

- **A credential handed to a consumer the tool does not know is reported at Medium.** A `NetworkCredential` built for a share, a mail server, a directory or a library whose consumer the tool does not list is reported as an unaccounted credential (`VB-013`). A credential whose receiver is named for a share or a mail service is reported the same way. Dismiss the finding if the consumer is not RelativityOne.
- **A system token given to the Import API constructor can be reported as a user and password.** A token-named argument, and a token returned by a call whose name carries `Token`, are declined as the password of the constructor; a call whose own argument holds a further call (`FetchPlatformToken(Resolve(scope))`) is still reported. A type of your own with the same name as the SDK's Import API type is not reported when it is declared as a class or a structure in the same file, close to the use, and written without a namespace; a type declared in another file or farther than about one thousand characters away is still reported by that class and by the SDK credential classes. Check that the second argument is a password.
- **A variable that holds a bearer value under a name that carries `token` is reported as a Basic header value.** A header write whose value sits in a variable is read when the variable's name carries a credential word; a name that carries `bearer` or `access_token` is read as a bearer and is not reported, and other token names are. Check what the variable holds.
- **A host joined by a colon to a value that is not a port can be reported as an encoded pair**, and a value held under a name declared in two procedures of one file can be read as the credential of the other. Dismiss such a finding on review.
- **A pair whose second half is held under a name with no credential word is not reported as an encoded pair.** The second half is read only when its name carries a credential word, such as `password`, `secret`, `credential` or `token`, or `pass`, `pwd` or `apikey` as a whole word, so a half held under `pin` or `passphrase` is not read; a header value that names the scheme next to the conversion is still reported. An `Authorization` header whose value is held in a variable whose name carries `pass`, `pwd` or `apikey` in lower case followed directly by a digit, such as `pass2`, is not reported, while the same name with a capital first letter, `Pass2`, is. Search for these forms by hand with the searches in [Part A for VB.NET](../migration/part-a-vbnet.md).
- **The check that declines a name and a size or a port is not applied in every spelling.** A half ending in `Length`, `Count`, `Size` or `Port` is declined only in some spellings of the pair: it is not applied to a first half ending in `Length`, `Count` or `Size` joined with `&` or `+`, to a half followed by `.ToString()`, to a second half ending in `Port` written inside the encoder call, or to a second half ending in `Length`, `Count` or `Size` written inside the encoder call with `&`, `+`, an interpolated string or `String.Concat`. In those spellings a name and a size or a port can be reported as an encoded pair when the second half carries a credential word. Dismiss such a finding on review.

## Shell: what the tool reads and what it does not

A Shell script is a program that sends a credential by calling an HTTP client, and the tool reads the call as text, without running or parsing the script. Each item says what you see and what to do. [Part A for Shell](../migration/part-a-shell.md#declared-unsupported-coverage) has the full list the report prints.

- **One family of shells is read.** The tool reads `bash`, `sh` (POSIX, usually `dash`), `zsh` and `ksh`. Scripts for `fish`, `csh` and `tcsh` and Windows batch files (`.bat`, `.cmd`) are different languages: they are not attributed to Shell and stay in the list of files that were not assessed.
- **Shell inside other files is not read, and much of it is there.** A CI pipeline definition, a Dockerfile, a Makefile, a crontab, a service unit file or a Markdown document that holds shell commands is not a shell script, so its commands are not analyzed. In practice many `curl -u` lines live in such files. Search them by hand with the searches in Part A.
- **Some shell files are not attributed to Shell.** `.bats` test files, shell start-up files such as `.bashrc`, `.bash_profile`, `.profile`, `.zshrc`, `.zshenv` and `.envrc`, and a script with no file extension whose first line does not name `bash`, `sh`, `zsh` or `ksh`, in lower case, as the interpreter, directly or after `env` and any options written as one word, such as `dash`, `ash`, `mksh`, `ksh93` or `busybox sh`, or that puts a separate argument word or a variable assignment between `env` and the shell, stay in the list of files that were not assessed, and the report names them. The first line is read only for a file with no extension: a file ending in `.sh`, `.bash`, `.zsh` or `.ksh` is read whatever its first line names, and a file with any other extension is not attributed to Shell by its first line.
- **A credential in a file the script only reads is not seen.** The contents of a `.netrc`, a `.curlrc`, a `.wgetrc` or an `.env` file are not in the text the tool analyzes, and that is where a credential usually lives. A configuration that the script writes itself is read. A header, a token or a scheme that another script supplies through `source` is not joined to its use, and neither is sign-in evidence in a sourced file.
- **Code that is never sent is not reported, and code that never runs is.** A credential built into a variable, an array or a pair is reported only when a call to a known client uses it within the next thirty lines, so a pair that is only printed or compared is not reported. A construct in a dead branch (`if false`), after an `exit`, or in a function that nothing calls and that uses no parameter is reported as if it ran, because the tool does not know the order of execution.
- **A command that runs from a string is read in the listed forms.** The tool reads the commands in `eval`, `bash -c` and the other shells with `-c`, `su -c`, `ssh host "..."`, a backtick or `${...}` inside a double-quoted string, and a here-document fed to a shell. A command in another form (the stage after a pipe on a continuation line, a script written to a file and run by a separate command, `bash < file`, a process substitution, a here-string, a shell behind `env`, `nohup` or `exec`, a quoted command word) is text to the tool and is reported at Low. A Low match inside a quoted command may be live code.
- **Four forms are read the way `bash` reads them.** A here-document terminator written with ANSI-C quoting, one continued across a backslash and a line feed, one quoted across a line feed, and a here-document opened inside a command substitution that closes on the opening line are read differently by `dash`, and the last also by `bash` before 5.2. In those forms the lines after the here-document can be read as its body where the shell runs them as commands.
- **Two limits are silent.** More than 5,000 here-document openings waiting for their bodies in one region of a file, and an arithmetic double parenthesis whose closing is hidden by a here-document body or is past the per-file read-ahead budget, are read without any message. The time the tool spends on the strings and comments of a Shell file, arithmetic included, is limited to 1000 milliseconds; see [A file that takes too long](#a-file-that-takes-too-long).
- **A mock or a test with a neutral name is not recognized.** The tool recognizes test code by a folder, a file name or a test-library marker, and a mock by a folder or a file name. A mock with a neutral name is reported at the level of its class and may be a false positive; a test with a neutral name outside every test folder is reported as production code.
- **The destination is not known.** The tool sees a command line and cannot tell whether a call goes to RelativityOne or to another server. Such findings are reported with a note that they may not apply when the destination is not RelativityOne.
- **A shell script that prompts a person is read as interactive.** A script that reads from the environment and falls back to `read -s` is assigned Authorization Code, as described in [A script with both a stored credential and a prompt](#a-script-with-both-a-stored-credential-and-a-prompt).

## The literal-credential warning is read from where the credential sits

The warning that a credential is written literally into the source depends on the position of the text, not on what it says. A credential construct inside a string, such as a help message or an example shown to the user, can raise the warning although no credential is in use. A literal credential inside a comment raises none, so a commented-out credential is not flagged. Check each warning against the line it names, and search the repository by hand for credentials left in comments.

In Shell, a value that the shell expands (`$`, a backtick, a backslash, a brace, a glob character or a leading `~`) is never a literal credential, so a call such as `curl -u "$API_USER:$API_PASSWORD"` is reported at its level and raises no warning, and neither does a value read with `read`. A password written as a bare word or in single quotes, in a user option, an address, a header, an encoding pipeline or a configuration the script writes, does raise it.

The warning is also not raised when a PowerShell password is written as a quoted literal and passed through `ConvertTo-SecureString '<literal>' -AsPlainText`, wrapped in a credential object and given to `-Credential`. The tool does not follow `-Credential` back to the literal, so the finding is reported at its level with no literal-credential warning, whereas `New-Object System.Net.NetworkCredential('<user>', '<password>')` and a hard-coded Basic `Authorization` header do raise it. If you use the warning to find the secrets to rotate, you will miss these: search the PowerShell files by hand for `ConvertTo-SecureString` used with `-AsPlainText`, and rotate each password written into a script. [Part A for PowerShell](../migration/part-a-powershell.md#declared-unsupported-coverage) gives a search.

## The findings limits

The tool limits how many findings it reports: **2,000 for one file and 50,000 for one scan**. These limits are a bound on pathological or generated input, such as a generated file with an enormous number of matches. An ordinary repository produces a few dozen findings, far below them, so you should never meet a limit in ordinary use. If you do see a notice that findings were counted and not listed, a generated or machine-written file is the likely cause. The report states how many findings each limit removed. The findings that are kept are the first ones in a fixed order (by path for a whole scan, by position for one file), the findings that are dropped are not named or located individually, and the counts stay correct. Find the file that produced the notice in the report, and review it by hand.

## A file that takes too long

The tool bounds the time it spends evaluating each of its search expressions on a file, currently 15.25 seconds. If a file reaches the limit, the tool discards every finding for that file and counts the file in the report's skipped items, so the loss shows up as a number. Review that file by hand.

Two shorter limits, of 1000 milliseconds each, apply to the first-line check of a file during `scan` and to the reading of its strings and comments during `analyze`; in a Shell script the second also covers the read-ahead that decides whether a double parenthesis opens arithmetic. A file that reaches the first is identified by its extension instead of its first line, and is counted in the report; a file that reaches the second keeps the findings found before the point where the limit was reached, reports none at or after it, and is counted in the report's skipped items. All the limits are provisional; see the [release notes](02-release-notes.md#known-issues).

## File names in decomposed Unicode form

The tool identifies a file by its path as the platform returns it, with the separators normalized. It does not convert file names to a single Unicode normalization form. Two file names that differ only in normalization form are therefore reported as **two files**. A name that reaches the tool in decomposed form, that is an accented letter stored as a letter plus a combining accent, may be positioned and identified differently from the same repository scanned on another platform. This applies on **Windows (NTFS)**, where such a name can exist, on **Linux**, whose file systems keep a name byte for byte and can therefore hold a decomposed name or two spellings of one name, and on **macOS**, which returns some names in decomposed form. Finding identifiers and the order of findings can then differ between platforms for those files. Detection within each file is not affected. The findings of a scan that reaches the limit of 50,000 findings are the exception: the ones kept are the first in path order (see [Limits on the number of findings](#the-findings-limits)), path order depends on the normalization form, and so the findings kept can differ between platforms.
