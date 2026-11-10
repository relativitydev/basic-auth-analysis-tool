# Rule Patterns for PowerShell (Part A of the Migration Documentation)

Part A describes the Basic Authentication patterns the tool looks for in PowerShell code, so that you can find them in your own scripts without the tool, and so that you can understand why the tool reported a finding. Each pattern class has an identifier, such as `PS-005`, which is the same identifier a tool report shows, so a finding and this document can be matched.

Every pattern class below has the same five parts:

- **Identifier and name.** In the heading.
- **What it looks like.** A short description and a placeholder example.
- **How to search.** A pattern you can run with `rg` (ripgrep). PowerShell is not case-sensitive, so every search ignores letter case (`-i`). If you use `grep`, run `grep -rniE --include="*.ps1" --include="*.psm1"` with the same expression, replacing `\s` by `[[:space:]]`, `\w` by `[[:alnum:]_]` and `\W` by `[^[:alnum:]_]`. In PowerShell itself the expression works unchanged with `Select-String`: `Get-ChildItem -Recurse -Include *.ps1,*.psm1 | Select-String -Pattern '<expression>'`, which also ignores letter case. A script saved as UTF-16, which some Windows editors do by default, is not read by `grep`, which sees it as binary and reports no match; `rg` and `Select-String` read it, so use one of them when your scripts may be UTF-16.
- **Confidence.** When a match is High, Medium or Low, including the cases that go to manual review.
- **Which flow.** The signals that indicate Client Credentials or Authorization Code, and the default.

Every example uses obvious placeholders such as `'<user>'`, `'<password>'` and `<host>`. No example contains a real credential. To decide what to do about a pattern, go to [Part C](part-c-powershell.md); for where the new secrets go, see Secret Management (Part B of the Migration Documentation), [`part-b-secret-management.md`](part-b-secret-management.md).

The tool reads files with the extensions `.ps1`, `.psm1` and `.psd1`, and a file with no extension whose first line names the PowerShell interpreter (`#!/usr/bin/env pwsh`). The searches below use `.ps1` and `.psm1`, where the code lives; a `.psd1` file is a data file, and the tool declares no detection for its contents (see [Declared unsupported coverage](#declared-unsupported-coverage)). The catalog describes the constructs by their shape, so a pattern class can match code from a library it does not name.

**The two editions.** Windows PowerShell 5.1 and PowerShell 7 differ in ways that matter here, and each class says where. `Invoke-RestMethod` and `Invoke-WebRequest` have an `-Authentication` parameter on PowerShell 7 only. `curl` and `wget` are aliases of the web commands on Windows PowerShell 5.1, and native programs on PowerShell 7. PowerShell 7 refuses to send `-Credential` over a plain `http://` address unless `-AllowUnencryptedAuthentication` is given.

**Scripts with no extension.** The searches in this page use `--glob '*.ps1' --glob '*.psm1'`, so they do not open a script that has no extension and starts with a line such as `#!/usr/bin/env pwsh`, which the tool does analyze. The tool treats the first line as naming PowerShell when it starts with `#!`, optionally a path and `env`, and then the word `pwsh` or `powershell` in lower case. To list the scripts that start this way, run one of these commands, then run each search below on the files it lists, passing the file names in place of the `--glob` options. The `rg` and `grep` commands also list `.ps1` files that start this way; ignore those.

```bash
rg -l '^#!\s*(\S*/)*(env\s+(-\S+\s+)*)?(pwsh|powershell)(\s|$)'
```

```bash
grep -rlE '^#![[:space:]]*([^[:space:]/]*/)*(env[[:space:]]+(-[^[:space:]]+[[:space:]]+)*)?(pwsh|powershell)([[:space:]]|$)' .
```

```powershell
Get-ChildItem -Recurse -File | Where-Object { -not $_.Extension } | Select-String -Pattern '^#!\s*(\S*/)*(env\s+(-\S+\s+)*)?(pwsh|powershell)(\s|$)' -List | Select-Object -ExpandProperty Path
```

The commands match the `#!` line wherever it is in a file, and the tool reads the first line only, so a match deeper in a file is not a script the tool analyzes.

## Find your script

Start here if you know what your script does and not which class describes it. Each row names a common situation and the class that covers it. Open the class, read its description and its search, and check the match against your code. A situation that is not in the table may still be covered: the classes are listed in full below, and what the tool does not read is in [Declared unsupported coverage](#declared-unsupported-coverage).

| Your script does this | Class |
| --- | --- |
| Builds the `Authorization` header by hand in a hashtable, such as `@{ Authorization = "Basic $encoded" }` | [PS-004](#ps-004) |
| Writes the `Authorization` header by name, with `.Add('Authorization', ...)`, `.Set(...)` or `$headers['Authorization'] = ...` | [PS-005](#ps-005) |
| Writes the header through the framework's header enumeration, with no `"Authorization"` string | [PS-006](#ps-006) |
| Creates an authentication header value with the scheme `Basic` | [PS-007](#ps-007) |
| Joins a user name and a password with a colon and encodes the result to Base64 | [PS-010](#ps-010) |
| Sends a token or key under the `Basic` scheme, with one half of the pair empty | [PS-011](#ps-011) |
| Passes `-Authentication Basic` together with `-Credential` to a web command | [PS-003](#ps-003) |
| Passes `-Credential` to a web command with no `-Authentication` | [PS-012](#ps-012) |
| Adds a credential to a credential cache under the `Basic` scheme | [PS-008](#ps-008) |
| Assigns a `NetworkCredential` to a handler, web client or web request | [PS-009](#ps-009) |
| Passes a credential to remoting, a drive, a process, a job, CIM or WMI, a mail command or client, a proxy, a file-transfer request or a directory object | [PS-013](#ps-013) |
| Defines its own function that builds the Basic header or the credential | [PS-014](#ps-014) |
| Builds a Relativity SDK user name and password credential | [PS-001](#ps-001) |
| Builds the retired RSAPI client with a user name and password | [PS-002](#ps-002) |

## How confidence is assigned

Every finding receives exactly one of three levels. The levels depend on what the tool can establish from the file, and never on how sure it is about the rest of your system.

| Level | What it means |
| --- | --- |
| **High** | The code almost certainly performs Basic Authentication against a live call path. A known credential-bearing type or call is used, a web command is told to use the Basic scheme, or a credential is added to a credential cache under the Basic scheme, in executable, non-test code. |
| **Medium** | The code matches a Basic Authentication shape, but the credential source or the call path cannot be established from the file alone. |
| **Low** | The match is present in the text but unlikely to be live authenticating code: a comment, a help block, a test fixture or mock, sample or example code, or a vendored or generated file. Three classes are also Low by their own definition. |

Five rules apply to every class:

- **A class states a base level, and levels combine by taking the lower.** A class whose construct shows both the scheme and the credential carrier keeps its base level. A class whose construct does not, because the scheme or the credential is in another statement, or because the destination is unknown, lands one level lower. That is why several classes whose base level is High are reported as Medium.
- **Ambiguity resolves downward.** When two levels are possible, the lower one is used, so that uncertainty goes to manual review and not to a recommendation. A scheme held in a variable that cannot be resolved in the file gives Medium; a scheme that resolves to something other than Basic gives no finding.
- **A file's status only lowers the level.** A match is demoted to **Low** when it sits in a comment, in a help block or in a string body, or in a file the tool recognizes as test, generated, sample, example or vendored code. Nothing raises a level.
- **A Low match is reported for manual review, with no recommendation and no flow.** It is not evidence that the code does not authenticate. Part C says how to judge each case.
- **An argument may be an indexer, and an indexer argument is never a literal credential.** Wherever a class below reads a user name, a password, a token or an address from the call, the argument may be a quoted literal of any of the six string kinds, a variable in any spelling, an environment variable, a parenthesized expression, a `$( ... )` sub-expression, or one of those followed by a member chain with calls and by one or two bracket suffixes, such as `$settings['<key>']` or `$parts[1]`. The bracket holds a quoted literal or a short expression with no bracket, comma, quote, parenthesis or semicolon inside. A value read from configuration is code and not an embedded secret, so a match that captures an indexer never raises the in-code secret warning.

**Why a `NetworkCredential` on a handler is Medium and `-Credential` with no scheme is Low.** Both pass a credential and name no scheme, and the tool treats them differently because of what it can show about what is sent. A web command called with `-Credential` and no `-Authentication` (PS-012) does not send Basic to RelativityOne: the call goes out without credentials, RelativityOne answers with a bearer challenge, and PowerShell sends nothing more. NSerio confirmed this in local checks on Windows PowerShell 5.1 and on PowerShell 7, so the match is Low and goes to manual review. A `NetworkCredential` assigned to an HTTP handler, web client or web request (PS-009) is the usual way a script gives such an object a user name and a password, and the tool has not shown that those types stay silent after a bearer challenge. It cannot tell from the file where the call goes either, so the class is Medium: it is reported, with a recommendation, and you confirm the destination. If the same assignment hands the credential to a mail client, a proxy or a directory object, it is PS-013 and Low.

What marks a file for demotion in PowerShell:

- **File names:** `.Tests.ps1`; `.generated.ps1` and `.g.ps1`. Letter case does not matter.
- **Folders:** a folder named `test`, `tests`, `mock`, `mocks`, `fixture` or `fixtures`, in any letter case, matched as a whole folder name, so `Contest` does not match.
- **Sample and example code:** a folder named `sample`, `samples`, `example` or `examples` in any letter case, matched as a whole folder name, so `SamplesCore` and `Sampler` do not match; and a file whose name begins with `Sample` or `Example`. A file name that only contains the word further in does not match. The file-name rule has a cost: it also reaches a real script whose name starts with the word, such as `SampleRateConverter.ps1`, and that match is reported at Low for manual review instead of being lost.
- **Vendored code:** a folder named `vendor` or `third-party` in any letter case, matched as a whole folder name. The tool reads vendored folders, it does not skip them. A vendored file that is not under a folder of those names is not demoted and is judged like any other file.
- **File content:** a generated-file header (`auto-generated` or `generated by` within the first ten lines), or Pester test content: a `Describe` or `Context` block opened with a quoted name, together with `It` and `Should`. The Pester words count only in code, not in a comment or a string. A Pester file that is not named `*.Tests.ps1` and does not hold that content is judged as production code.
- **Not a signal: a folder named `Deployment`.** In PowerShell the folder holds the deployment scripts this tool exists to find, so a script under it is reported at its full level. The cost is that a deployment script that only illustrates a pattern is reported as if it were live.

**A literal secret is a separate warning.** When a class captures a credential and the password or token is written literally into the script, the tool also raises an in-code secret warning. The warning is independent of the level: a demoted match keeps it. It is read from where the credential sits, not from its value: a literal inside a string, including help or documentation text, raises it, and a literal inside a comment does not. A value read through an indexer is code, not a literal, and does not raise it. The warning does not follow a credential back to the literal that built it; see the last item of [Declared unsupported coverage](#declared-unsupported-coverage).

## Pattern classes

Fourteen classes are registered for PowerShell. Eleven of them can produce a recommendation. `PS-012`, `PS-013` and `PS-014` are always Low and always go to manual review.

### PS-001 · SdkUserNamePasswordCredentials

<a id="ps-001"></a>

A Relativity SDK credential object built from a user name and a password, usually handed to the SDK's service factory settings. Both argument forms count: values that arrive in variables or indexer reads, and values written literally into the script. The type may be created with `New-Object` in any of its forms (positional, with parentheses, or with `-TypeName` and `-ArgumentList` in either order and with any abbreviation of the parameter names), with `[Type]::new(...)`, with the type name in quotes, and with the type written bare, partly qualified or fully qualified. The argument list may be a comma list, `@(...)` or an array variable.

**Example.** Placeholders only:

```powershell
$credentials = New-Object Vendor.Sdk.UsernamePasswordCredentials('<user>', '<password>')
$settings = New-Object Vendor.Sdk.ServiceFactorySettings('<host>/Relativity.Rest/api', $credentials)
```

```powershell
$credentials = [Vendor.Sdk.UsernamePasswordCredentials]::new($loginName, $loginSecret)
```

**How to search.** This finds candidate files; open each match and check it against the description above.

```bash
rg -n -i --glob '*.ps1' --glob '*.psm1' 'UsernamePasswordCredentials'
```

**Confidence.** **High.** The type name declares both that the object is a credential and that it carries a user name and a password, so the construct is High wherever it sits in executable, non-test code, and whatever namespace is written in front of the name. A credential type of the same SDK that carries a bearer token does not match. **Low** when the match is in a comment, in a string body, or in a file the tool recognizes as test, generated or vendored code. When the password is written literally into the script, the match also raises the in-code secret warning, which is separate from the level and is not removed by a Low level. When the same construct is also a legacy RSAPI client construction, PS-002 is reported instead of this class, so that one construct produces one finding. See [How confidence is assigned](#how-confidence-is-assigned).

**Which flow.** Look in the same file as the match for the signals listed under [Flow signals](#flow-signals). An interactive-user signal points to Authorization Code. A fixed-credential signal points to Client Credentials. With no signal, the default applies, and the default is Client Credentials. Signals are read in the same file as the match only, so a prompt or a credential source in another file is not seen; follow the credential to where it comes from before you decide. Credentials usually arrive here as variables, parameters or indexer reads. A variable filled by `Get-Credential` or by a password prompt points to Authorization Code; one filled from an environment variable, an `Import-Clixml` file or a secret store points to Client Credentials. See [the migration entries in Part C](part-c-powershell.md#ps-001-migration).

### PS-002 · SdkLegacyRsapiCredentials

<a id="ps-002"></a>

The retired RSAPI client constructed together with a Relativity SDK user name and password credential. The client's first argument, its address, may be a variable, an indexer read or a construction such as `New-Object System.Uri` or `[Uri]$address` with at most one further level of nested parentheses; an address nested deeper is not read. The recommendation differs from PS-001 because the RSAPI client itself is retired: the call moves to Object Manager rather than only changing its credential.

**Example.** Placeholders only:

```powershell
$client = New-Object Vendor.Sdk.RSAPIClient ((New-Object System.Uri -ArgumentList '<host>/Relativity.Services'), (New-Object Vendor.Sdk.UsernamePasswordCredentials('<user>', '<password>')))
```

**How to search.** This finds candidate files; open each match and check it against the description above.

```bash
rg -n -i --glob '*.ps1' --glob '*.psm1' 'RSAPIClient'
```

**Confidence.** **High.** The credential type is nested inside the client construction, so the construct shows both the scheme and the credential carrier; a client built with a bearer credential, or with no credential, does not match. The client and the nested credential are read in every creation form listed for PS-001. **Low** in a comment, in a string body, or in a test, generated or vendored file. This class replaces PS-001 on the same construct. It does not match an RSAPI client obtained without a visible credential; that form is listed under [Declared unsupported coverage](#declared-unsupported-coverage). See [How confidence is assigned](#how-confidence-is-assigned).

**Which flow.** Look in the same file as the match for the signals listed under [Flow signals](#flow-signals). An interactive-user signal points to Authorization Code. A fixed-credential signal points to Client Credentials. With no signal, the default applies, and the default is Client Credentials. Signals are read in the same file as the match only, so a prompt or a credential source in another file is not seen; follow the credential to where it comes from before you decide. The credential comes from wherever the surrounding script obtains the user name and password. A prompt in the same file points to Authorization Code; an environment variable, a stored credential file or a secret-store read points to Client Credentials. See [the migration entries in Part C](part-c-powershell.md#ps-002-migration).

### PS-003 · ExplicitBasicAuthenticationParameter

<a id="ps-003"></a>

A web command told to use the Basic scheme with its `-Authentication` parameter: `Invoke-RestMethod`, `Invoke-WebRequest`, their aliases `irm` and `iwr`, and `Start-BitsTransfer`. On Windows PowerShell 5.1 the aliases `curl` and `wget` are the same web commands and are read too; on PowerShell 7 they are native programs and are not (see [Declared unsupported coverage](#declared-unsupported-coverage)). The parameter may be abbreviated from `-Au`, written with a colon (`-Authentication:Basic`), and written with any of the four dash characters PowerShell accepts. The value may be the bare word, a quoted word, the enumeration member `[Microsoft.PowerShell.Commands.WebAuthenticationType]::Basic`, or a variable. The same parameter written as a key `Authentication = 'Basic'` of a hashtable that is splatted into one of these commands with `@name` within twenty lines is read as well, and so is a command continued over several lines with backticks.

**Example.** Placeholders only:

```powershell
Invoke-RestMethod -Uri '<host>/Relativity.REST/api/<endpoint>' -Credential $credential -Authentication Basic
```

**How to search.** This finds candidate files; open each match and check it against the description above.

```bash
rg -n -i --glob '*.ps1' --glob '*.psm1' '(^|[^\w-])[-–—―]Au[a-z]*(\W|$)|\bAuthentication\s*='
```

**Confidence.** **High.** The parameter declares the scheme, and the command declares the outbound call, so the construct is High without anything else in the match. A scheme held in a variable is resolved when the script assigns it once: a variable that resolves to something other than Basic gives no finding, one that resolves to Basic is High, and one the tool cannot resolve (assigned twice, or given by a parameter default) keeps the level. `-Authentication Bearer`, `OAuth` and `None` are not reported, and neither is `-AllowUnencryptedAuthentication`, a different parameter. The parameter exists only on PowerShell 7 for `Invoke-RestMethod` and `Invoke-WebRequest`, so a script that uses it does not run on Windows PowerShell 5.1 as written; `Start-BitsTransfer` has it on both editions. A hashtable key `Authentication = 'Basic'` that is not splatted into a web command, such as the settings of a mail or directory command, is not matched. **Low** in a comment, in a string body, or in a test, generated or vendored file. See [How confidence is assigned](#how-confidence-is-assigned).

**Which flow.** Look in the same file as the match for the signals listed under [Flow signals](#flow-signals). An interactive-user signal points to Authorization Code. A fixed-credential signal points to Client Credentials. With no signal, the default applies, and the default is Client Credentials. Signals are read in the same file as the match only, so a prompt or a credential source in another file is not seen; follow the credential to where it comes from before you decide. The credential comes from the `-Credential` argument, usually a variable. A variable filled by `Get-Credential` points to Authorization Code; one filled from an environment variable, an `Import-Clixml` file or a secret store points to Client Credentials. See [the migration entries in Part C](part-c-powershell.md#ps-003-migration).

### PS-004 · AuthorizationHeaderHashtableLiteral

<a id="ps-004"></a>

A hashtable literal that carries the `Authorization` header with a Basic value. The class reads the literal, not how it is used: the table may be written inline after `-Headers`, assigned to a variable and passed later, nested under the `Headers` key of a splatted table, or never used in the file. The key is the whole word `Authorization`, bare or quoted, in any letter case; the value may be one literal string, a literal prefix joined with an expression, an interpolated string, a format expression, a call of a format or join method, the scheme held in a variable, a call of a function whose name carries the word `Basic`, or a variable assigned one of those within the five lines before.

**Example.** Placeholders only:

```powershell
$headers = @{ Authorization = "Basic $encoded" }
Invoke-RestMethod -Uri '<host>/Relativity.REST/api/<endpoint>' -Headers $headers
```

**How to search.** This finds candidate files; open each match and check it against the description above.

```bash
rg -n -i --glob '*.ps1' --glob '*.psm1' 'Authorization\W{0,3}='
```

**Confidence.** **Medium.** The confidence rules name an explicitly constructed Basic `Authorization` header as High, but this construct does not by itself show that the header reaches an outbound request, so it lands at Medium. If the scheme is a variable that resolves to something other than Basic, such as `Bearer`, the tool reports nothing; if it cannot be resolved, the level stays Medium. A key such as `Proxy-Authorization` or `X-Authorization-Mode` is not this header, and `Authorization = $token` with no scheme in the literal and none assigned nearby is not matched. **Low** in a comment, in a string body, or in a test, generated or vendored file. See [How confidence is assigned](#how-confidence-is-assigned).

**Which flow.** Look in the same file as the match for the signals listed under [Flow signals](#flow-signals). An interactive-user signal points to Authorization Code. A fixed-credential signal points to Client Credentials. With no signal, the default applies, and the default is Client Credentials. Signals are read in the same file as the match only, so a prompt or a credential source in another file is not seen; follow the credential to where it comes from before you decide. The encoded credential is usually built a few lines above. A prompt in the same file points to Authorization Code; an environment variable, a stored credential file or a secret-store read points to Client Credentials. See [the migration entries in Part C](part-c-powershell.md#ps-004-migration).

### PS-005 · AuthorizationHeaderStringKeyWrite

<a id="ps-005"></a>

The `Authorization` header written with its name as a string, on any receiver: a header table, a web client, a web request or a header collection. The writes read are `.Add(`, `.Set(`, `.Append(`, `.TryAddWithoutValidation(`, `.AddHeader(` and the `.setRequestHeader(` call of the COM request object; the indexer `$headers['Authorization'] = ...`; the property assignment `$headers.Authorization = ...` with a string value, including `$client.DefaultRequestHeaders.Authorization = "Basic $encoded"`; and the one-string form `$request.Headers.Add("Authorization: Basic $encoded")`. The value takes the same shapes as in PS-004.

**Example.** Placeholders only:

```powershell
$headerTable.Add('Authorization', 'Basic ' + $encoded)
$headerTable['Authorization'] = "Basic $encoded"
$request.Headers.Add("Authorization: Basic $encoded")
```

**How to search.** This finds candidate files; open each match and check it against the description above.

```bash
rg -n -i --glob '*.ps1' --glob '*.psm1' 'Authorization\W{0,4}[,:=]'
```

**Confidence.** **Medium.** A hand-built header does not by itself show that it reaches an outbound request, so the class lands at Medium. The class needs a write: a read such as `$value = $Request.Headers['Authorization']` is a service looking at incoming authentication and is not matched. Another key, `Proxy-Authorization` (also in the one-string form), a `Bearer` value, and a write whose value is a construction of an authentication header type (that is PS-007) are not this class. **Low** in a comment, in a string body, or in a test, generated or vendored file. See [How confidence is assigned](#how-confidence-is-assigned).

**Which flow.** Look in the same file as the match for the signals listed under [Flow signals](#flow-signals). An interactive-user signal points to Authorization Code. A fixed-credential signal points to Client Credentials. With no signal, the default applies, and the default is Client Credentials. Signals are read in the same file as the match only, so a prompt or a credential source in another file is not seen; follow the credential to where it comes from before you decide. The encoded credential is built nearby. A prompt in the same file points to Authorization Code; an environment variable, a stored credential file or a secret-store read points to Client Credentials. See [the migration entries in Part C](part-c-powershell.md#ps-005-migration).

### PS-006 · AuthorizationHeaderEnumWrite

<a id="ps-006"></a>

The `Authorization` header set through the framework's header enumeration on a web client or web request, so no `"Authorization"` string appears in the script. Both writes count: the indexer, and the collection's `.Add(` or `.Set(` call with the enumeration member as the first argument. The enumeration may be written fully qualified, partly qualified (`Net.HttpRequestHeader`) or by its short name under `using namespace`.

**Example.** Placeholders only:

```powershell
$request.Headers.Add([System.Net.HttpRequestHeader]::Authorization, "Basic $encoded")
$client.Headers[[System.Net.HttpRequestHeader]::Authorization] = "Basic $encoded"
```

**How to search.** This finds candidate files; open each match and check it against the description above.

```bash
rg -n -i --glob '*.ps1' --glob '*.psm1' 'HttpRequestHeader\]::Authorization'
```

**Confidence.** **Medium.** It shows the scheme in the assigned value but not that the request goes out, so it lands at Medium. A scheme that resolves to something other than Basic is not reported. Another member of the enumeration, such as `ContentType`, does not match, and a read does not match. The string-named write `Headers.Add('Authorization', ...)` is PS-005. **Low** in a comment, in a string body, or in a test, generated or vendored file. See [How confidence is assigned](#how-confidence-is-assigned).

**Which flow.** Look in the same file as the match for the signals listed under [Flow signals](#flow-signals). An interactive-user signal points to Authorization Code. A fixed-credential signal points to Client Credentials. With no signal, the default applies, and the default is Client Credentials. Signals are read in the same file as the match only, so a prompt or a credential source in another file is not seen; follow the credential to where it comes from before you decide. The encoded credential is usually built nearby. A prompt in the same file points to Authorization Code; an environment variable, a stored credential file or a secret-store read points to Client Credentials. See [the migration entries in Part C](part-c-powershell.md#ps-006-migration).

### PS-007 · AuthenticationHeaderValueScheme

<a id="ps-007"></a>

An authentication header value object created with the Basic scheme: `[System.Net.Http.Headers.AuthenticationHeaderValue]::new('Basic', $encoded)`, the same with `New-Object` (positional, or with `-TypeName` and `-ArgumentList`), the static `[AuthenticationHeaderValue]::Parse(...)` and `::TryParse(...)` whose text begins with the scheme, and the string cast `[AuthenticationHeaderValue]"Basic $encoded"`. The scheme may be a literal or a variable.

**Example.** Placeholders only:

```powershell
$httpClient.DefaultRequestHeaders.Authorization = [System.Net.Http.Headers.AuthenticationHeaderValue]::new('Basic', $encoded)
```

**How to search.** This finds candidate files; open each match and check it against the description above.

```bash
rg -n -i --glob '*.ps1' --glob '*.psm1' 'AuthenticationHeaderValue'
```

**Confidence.** **Medium.** The object shows the scheme but not that the header is sent, so it lands at Medium. A scheme that resolves to something other than Basic, such as `Bearer`, is not reported; a scheme the tool cannot resolve stays at Medium. **Low** in a comment, in a string body, or in a test, generated or vendored file. See [How confidence is assigned](#how-confidence-is-assigned).

**Which flow.** Look in the same file as the match for the signals listed under [Flow signals](#flow-signals). An interactive-user signal points to Authorization Code. A fixed-credential signal points to Client Credentials. With no signal, the default applies, and the default is Client Credentials. Signals are read in the same file as the match only, so a prompt or a credential source in another file is not seen; follow the credential to where it comes from before you decide. The encoded credential is built nearby. A prompt in the same file points to Authorization Code; an environment variable, a stored credential file or a secret-store read points to Client Credentials. See [the migration entries in Part C](part-c-powershell.md#ps-007-migration).

### PS-008 · CredentialCacheBasicScheme

<a id="ps-008"></a>

A credential added to a credential cache under the Basic scheme, so that an HTTP client sends Basic Authentication to that address: `$cache.Add($address, 'Basic', $credential)`, and the four-argument overload that takes a host and a port before the scheme. The address may be a variable, `[Uri]$address` or a `New-Object` construction. The credential may be a construction, a call of `.GetNetworkCredential()`, or a variable, which differs from C#. The scheme may be a literal or a variable.

**Example.** Placeholders only:

```powershell
$cache = New-Object System.Net.CredentialCache
$cache.Add([Uri]'<host>', 'Basic', (New-Object System.Net.NetworkCredential('<user>', '<password>')))
```

**How to search.** This finds candidate files; open each match and check it against the description above.

```bash
rg -n -i --glob '*.ps1' --glob '*.psm1' 'CredentialCache|\.Add\s*\(.*\WBasic\W|=\s*\WBasic\W'
```

**Confidence.** **High** when the scheme argument is the text `Basic`, or a variable in the same file that resolves to it. A scheme that resolves to `Negotiate`, `NTLM`, `Digest` or anything else is not reported, and neither is an `.Add` with two arguments, which is another kind of collection. The credential argument is required to be a credential by the call's own signature, which is why a variable is enough. This class replaces PS-009 on a credential constructed inside the cache entry's own call, so one cache entry is one finding. **Low** in a comment, in a string body, or in a test, generated or vendored file. See [How confidence is assigned](#how-confidence-is-assigned).

**Which flow.** Look in the same file as the match for the signals listed under [Flow signals](#flow-signals). An interactive-user signal points to Authorization Code. A fixed-credential signal points to Client Credentials. With no signal, the default applies, and the default is Client Credentials. Signals are read in the same file as the match only, so a prompt or a credential source in another file is not seen; follow the credential to where it comes from before you decide. The credential is usually built from the surrounding script. A prompt in the same file points to Authorization Code; an environment variable, a stored credential file or a secret-store read points to Client Credentials. See [the migration entries in Part C](part-c-powershell.md#ps-008-migration).

### PS-009 · NetworkCredentialOnHttpType

<a id="ps-009"></a>

A network credential built from a user name and a password and assigned to an HTTP client, handler or request: `$handler.Credentials = New-Object System.Net.NetworkCredential($user, $secret)`. The receiver must be created in the eight lines before the assignment, as a `WebClient`, an `HttpWebRequest` (by `[System.Net.WebRequest]::Create`, `::CreateHttp` or, on Windows PowerShell 5.1, `New-Object`), an `HttpClientHandler`, a `SocketsHttpHandler` or a `WinHttpHandler`, or the assignment sits in the initializer of one of them (`[HttpClientHandler]@{ Credentials = ... }` or `-Property @{ Credentials = ... }`). The credential may be a construction, a call of `.GetNetworkCredential()` or a cast `[System.Net.NetworkCredential]$credential`. Assigning the default or integrated credentials does not match, because that is integrated Windows authentication.

**Example.** Placeholders only:

```powershell
$handler = New-Object System.Net.Http.HttpClientHandler
$handler.Credentials = New-Object System.Net.NetworkCredential('<user>', '<password>')
```

**How to search.** This finds candidate files; open each match and check it against the description above.

```bash
rg -n -i --glob '*.ps1' --glob '*.psm1' '\bCredentials\s*='
```

**Confidence.** **Medium.** The credential is handed to an HTTP type, but the construct does not show that the call reaches RelativityOne or that the server asks for Basic, so it is Medium. A credential assigned from a plain variable (`$handler.Credentials = $networkCredential`) is not matched; that form is listed under [Declared unsupported coverage](#declared-unsupported-coverage). A receiver created more than eight lines before, or a type outside the list, is not matched. A web request session is not on the list: its credential is PS-012. A mail client, a proxy, a file-transfer request or a directory object takes the credential as PS-013, which replaces this class on the same assignment. **Low** in a comment, in a string body, or in a test, generated or vendored file. See [How confidence is assigned](#how-confidence-is-assigned).

**Which flow.** Look in the same file as the match for the signals listed under [Flow signals](#flow-signals). An interactive-user signal points to Authorization Code. A fixed-credential signal points to Client Credentials. With no signal, the default applies, and the default is Client Credentials. Signals are read in the same file as the match only, so a prompt or a credential source in another file is not seen; follow the credential to where it comes from before you decide. The credential pair comes from the surrounding script. A prompt in the same file points to Authorization Code; an environment variable, a stored credential file or a secret-store read points to Client Credentials. See [the migration entries in Part C](part-c-powershell.md#ps-009-migration).

### PS-010 · Base64UserPasswordPair

<a id="ps-010"></a>

A user name and a password joined with a colon, built by interpolation (`"$($user):$($secret)"`, `"${user}:${secret}"`, ``"$user`:$secret"``), concatenation, a join (`($user, $secret) -join ':'`), a format (`'{0}:{1}' -f $user, $secret` or `[string]::Format(...)`) or one literal that holds both halves, and passed to a Base64 encoder through any call that turns text into bytes. Both halves of the pair must be present; a pair with an empty half is PS-011.

**Example.** Placeholders only:

```powershell
$pair = "$($loginName):$($loginSecret)"
$encoded = [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes($pair))
```

```powershell
$encoded = [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes('<user>:<password>'))
```

**How to search.** This finds candidate files; open each match and check it against the description above.

```bash
rg -n -i --glob '*.ps1' --glob '*.psm1' 'ToBase64String'
```

**Confidence.** **Medium.** Building and encoding a pair is the shape of Basic Authentication, but the construct does not show where the result goes, so it is Medium. The encoder is `[Convert]::ToBase64String`, `[System.Convert]::ToBase64String` or the short name under `using namespace`; the text-to-bytes call between the pair and the encoder may be any call, so every encoding is read. The pair may be written inside the encoder's call or in the statements before it, within ten lines; a pair assembled further away is not matched, and an encoder over a single value is not matched. A colon-joined value that is not a credential, such as a host and a port that are then encoded, can also be reported and should be dismissed on review; see [Declared unsupported coverage](#declared-unsupported-coverage). A pair that an encoder helper other than the standard conversion encodes is not matched on its own. **Low** in a comment, in a string body, or in a test, generated or vendored file. See [How confidence is assigned](#how-confidence-is-assigned).

**Which flow.** Look in the same file as the match for the signals listed under [Flow signals](#flow-signals). An interactive-user signal points to Authorization Code. A fixed-credential signal points to Client Credentials. With no signal, the default applies, and the default is Client Credentials. Signals are read in the same file as the match only, so a prompt or a credential source in another file is not seen; follow the credential to where it comes from before you decide. The two halves of the pair are usually parameters or variables. A prompt in the same file points to Authorization Code; an environment variable, a stored credential file or a secret-store read points to Client Credentials. See [the migration entries in Part C](part-c-powershell.md#ps-010-migration).

### PS-011 · TokenCarriedUnderBasicScheme

<a id="ps-011"></a>

A token or API key carried through a Basic header with one half of the colon-joined pair empty, for example `":" + $accessToken` or `"$($user):"` encoded to Base64. The empty half may be `""`, `''`, `$null`, `[string]::Empty` or a variable that resolves to an empty literal. The token is not the defect; the scheme is, and this is the cheapest migration of the classes: drop the encoding and send the token under `Bearer`.

**Example.** Placeholders only:

```powershell
$pair = ':' + $accessToken
$encoded = [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes($pair))
```

**How to search.** This finds candidate files; open each match and check it against the description above.

```bash
rg -n -i --glob '*.ps1' --glob '*.psm1' 'ToBase64String'
```

**Confidence.** **Medium.** The pair is built and encoded in the same ways as in PS-010, within the same windows, but one half is empty, which is what separates the two classes: a pair with two non-empty halves is PS-010. A pair with an empty half is always reported here and never as PS-010. **Low** in a comment, in a string body, or in a test, generated or vendored file. See [How confidence is assigned](#how-confidence-is-assigned).

**Which flow.** Look in the same file as the match for the signals listed under [Flow signals](#flow-signals). An interactive-user signal points to Authorization Code. A fixed-credential signal points to Client Credentials. With no signal, the default applies, and the default is Client Credentials. Signals are read in the same file as the match only, so a prompt or a credential source in another file is not seen; follow the credential to where it comes from before you decide. The token usually comes from a variable, a parameter, an environment variable or a secret store. A prompt in the same file points to Authorization Code; an environment variable, a stored credential file or a secret-store read points to Client Credentials. See [the migration entries in Part C](part-c-powershell.md#ps-011-migration).

### PS-012 · CredentialOnWebCmdletWithoutScheme

<a id="ps-012"></a>

A credential passed to a web command with `-Credential`, or placed on a web request session, with no scheme named: `Invoke-RestMethod -Uri $uri -Credential $credential`. The commands are `Invoke-RestMethod`, `Invoke-WebRequest`, `irm`, `iwr`, `New-WebServiceProxy` and, on Windows PowerShell 5.1, `curl` and `wget`. The parameter may be abbreviated from `-Cr`, written with a colon, and splatted from a hashtable within twenty lines. A web request session (`Microsoft.PowerShell.Commands.WebRequestSession`, created in the eight lines before or named by `-SessionVariable`) whose `.Credentials` is assigned is read the same way. This is a near-miss class that goes to manual review.

**Example.** Placeholders only:

```powershell
Invoke-RestMethod -Uri '<host>/Relativity.REST/api/<endpoint>' -Credential $credential
```

**How to search.** This finds candidate files; open each match and check it against the description above.

```bash
rg -n -i --glob '*.ps1' --glob '*.psm1' '(^|[^\w-])[-–—―]Cr[a-z]*(\W|$)|\bCredentials?\s*='
```

**Confidence.** **Low, always, and always manual review.** On its own, `-Credential` without `-Authentication` does not send Basic to RelativityOne: the call is first sent without credentials, and after RelativityOne answers with a bearer challenge, PowerShell sends nothing more. The tool therefore reports the match at Low with no recommendation, and a person checks where the call points and whether an `-Authentication Basic` parameter or a hand-built header is also used. `-UseDefaultCredentials`, a certificate parameter, and `-Credential` on a command that also carries `-Authentication` (that is PS-003) are not this class; a `-Credential` on a remoting, drive, process or CIM command is PS-013. [Part C](part-c-powershell.md#ps-012-judging) says how to judge it. See [How confidence is assigned](#how-confidence-is-assigned).

**Which flow.** This class produces no flow, because a Low match gets no recommendation. If the same script also builds a Basic header or passes `-Authentication Basic`, that construct is reported separately and gets its own flow.

### PS-013 · NonHttpCredentialSink

<a id="ps-013"></a>

A credential handed to a command or object that is not an HTTP client: remoting, drive, process, job, CIM and WMI commands and `Send-MailMessage` take `-Credential`, and the mail client, the web proxy, the file-transfer request and the directory entry and searcher take a credential object. It is a near-miss class. It exists so that these credentials stop being reported as migration targets.

**Example.** Placeholders only:

```powershell
Invoke-Command -ComputerName $server -Credential $credential -ScriptBlock { hostname }
```

```powershell
$mailer = New-Object System.Net.Mail.SmtpClient('<mail host>')
$mailer.Credentials = New-Object System.Net.NetworkCredential('<user>', '<password>')
```

**How to search.** This finds candidate files; open each match and check it against the description above.

```bash
rg -n -i --glob '*.ps1' --glob '*.psm1' '(^|[^\w-])[-–—―]Cr[a-z]*(\W|$)|SmtpClient|WebProxy|FtpWebRequest|DirectoryEntry|DirectorySearcher'
```

**Confidence.** **Low, always, and always manual review.** The tool does not believe this is Basic Authentication towards a system being migrated, so it reports the match at Low with no recommendation. The set of sinks it knows is small and fixed: `Invoke-Command`, `Enter-PSSession`, `New-PSSession`, `New-PSDrive`, `Start-Process`, `Start-Job`, `New-CimSession`, `Get-CimInstance`, `Get-WmiObject`, `Invoke-WmiMethod` and `Send-MailMessage`, and the mail client, web proxy, file-transfer request, directory entry and directory searcher. A sink outside that set produces no finding at all, and every call in the set produces a Low row, which is noise in an administrative repository. This class replaces PS-009 on the same assignment. [Part C](part-c-powershell.md#ps-013-judging) says how to judge it. See [How confidence is assigned](#how-confidence-is-assigned).

**Which flow.** This class produces no flow, because a Low match gets no recommendation. If the same credential is also used to call RelativityOne, that use is reported separately and gets its own flow.

### PS-014 · AuthenticationHelperDefinition

<a id="ps-014"></a>

The definition of the script's own authentication helper: a function or filter whose name carries the scheme word `Basic` and a role word (`Auth`, `Header`, `Credential` or `Cred`) and that builds the header or the credential. It is the definition, not a call site.

**Example.** Placeholders only:

```powershell
function New-BasicAuthorizationHeader {
	param($loginName, $loginSecret)
	'Basic ' + [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes("$($loginName):$($loginSecret)"))
}
```

**How to search.** This finds candidate files; open each match and check it against the description above.

```bash
rg -n -i --glob '*.ps1' --glob '*.psm1' '\b(function|filter)\s+(\w+:)?[\w-]*Basic'
```

**Confidence.** **Low, always, and always manual review.** The advice for a helper is to migrate the helper once, and that is not a mechanical code change, so the tool reports the definition at Low with no recommendation. Its call sites do not appear as findings, because the helper hides the credential from them. The definition is one finding. A credential the helper builds is a second finding, at its own line, when another class recognizes it, such as the Base64 pair of PS-010. A name that holds the word `Basic` inside another word, such as `Basically`, and a name without the scheme word are not matched. Search for the helper's name to find its callers; [Part C](part-c-powershell.md#ps-014-judging) says how to judge it. The class is reached only in a file that also holds one of the words the tool preselects on (`Credential`, `Authorization`, `Authentication` or `ToBase64String`). See [How confidence is assigned](#how-confidence-is-assigned).

**Which flow.** This class produces no flow, because a Low match gets no recommendation. It has no flow of its own to confirm; decide the flow from who supplies the credential to the helper's callers, using the signals in [Flow signals](#flow-signals) in the same file as each caller.

## Flow signals

The tool recommends one of two OAuth 2.0 flows for every High or Medium finding. It chooses from signals in the code, and only from the code.

- An **interactive-user signal** is evidence that the credential belongs to a person who supplies it at run time. It decides **Authorization Code**.
- A **fixed-credential signal** is evidence that the credential is fixed for the integration. It decides **Client Credentials**.
- With **no signal**, the default flow applies. The default is **Client Credentials** unless your user profile file sets another, and the recommendation carries a note asking you to confirm the choice.
- If both kinds are present, the interactive-user signal wins. A script that reads a stored or environment credential and falls back to `Get-Credential` when none is found therefore receives Authorization Code, although it may well be an unattended integration. Confirm the flow for such a script by hand.

**The cross-file limit.** Signals are read in the same file as the finding, and only there. A prompt in a dot-sourced script, a credential read in an imported module or a loaded configuration file, and a credential passed in as a parameter by another script are not seen. For most real scripts the credential reaches the finding's file from another file, so the default is the expected outcome, and the note it carries is the guide. A comment never counts as a signal, and a literal credential written into the script is not evidence of either flow.

Some signals read the name of an environment variable. The name matches when it contains `password`, `secret`, `credential` or `token` anywhere, in any letter case, and when it contains `pass`, `pwd` or `apikey` (also written `api_key` or `api-key`) only as a whole word, so `DB_PWD` and `API_KEY` match while `Path` and `Bypass` do not.

### Interactive-user signals

- `GetCredentialCall`: The command `Get-Credential`, as a command name in its own right (`Get-CredentialFromVault` is another command), with or without `-Message` and `-UserName`. It is not a signal when it is written directly as the `-Credential` argument of a remoting, drive, process, job or CIM command, because that credential is for Windows and not for RelativityOne. A `Get-Credential` kept in a variable and passed to such a command ten lines later is still read as a signal.
- `ReadHostSecureInput`: `Read-Host` with `-AsSecureString` (or any abbreviation of it from `-A`) or, on PowerShell 7, with `-MaskInput`: a value read with the echo suppressed was typed by a person.
- `PromptForCredentialCall`: A call of `$Host.UI.PromptForCredential(...)`, the host's credential dialog.
- `ReadHostCredentialPromptText`: `Read-Host` whose prompt text asks for a password, passphrase, secret, PIN or user name, in ordinary wording. A prompt that is commented out does not count.
- `DesktopPasswordControl`: A desktop password control: the Windows Forms assignment `UseSystemPasswordChar = $true` or an assignment to `PasswordChar`, or a WPF `PasswordBox` created with `New-Object` or `::new` or cast, whose `Password` or `SecurePassword` is read in the same file within a short distance. A control declared in markup written inside a here-string is not recognized, because its type is written only in the markup.

### Fixed-credential signals

- `EnvironmentCredentialRead`: A read of an environment variable whose name is credential-named: `$env:NAME`, `${env:NAME}`, `[Environment]::GetEnvironmentVariable('NAME')` (also written `[System.Environment]`) or `Get-Item Env:NAME`.
- `ImportClixmlRead`: The command `Import-Clixml`, the idiom unattended scripts use to read a credential saved with `Export-Clixml`. It also fires on a file that holds no credential, which does not change the result, because that flow tag is Client Credentials, the default.
- `SecretStoreRead`: A call of `Get-Secret` (the SecretManagement module), `Get-StoredCredential` or `Get-AutomationPSCredential` (Azure Automation). Other vault commands are not recognized.
- `PlainTextSecureString`: `ConvertTo-SecureString` with `-AsPlainText` (or any abbreviation from `-A`) whose string argument is not a quoted literal, that is, a value that arrives from a variable, an environment read or a call. A quoted literal is a secret written into the script, which is not evidence of either flow.

Part C explains how to confirm the flow yourself: see [Confirming the flow](part-c-powershell.md#confirming-the-flow).

## Declared unsupported coverage

The catalog states what it does not cover, so that a missing finding is not read as a clean result. The tool prints the same list in its report. The list below is what the PowerShell analyzer declares, in the tool's own words, followed by the reason.

- **Confirming whether a piece of Basic Authentication code is actually calling RelativityOne or a different server.** The tool cannot determine, from the code in a single file, what server a request is actually sent to; such findings are still reported, with a note that the finding may not apply if the destination is not RelativityOne - review each one to confirm the destination before acting on it.
- **Confirmation that a matched Relativity SDK credential type actually comes from the Relativity SDK, rather than a different library that happens to use the same type name.** The tool matches on the type name as written in the script, not on which assembly it was loaded from; a different library using an identically named type would be matched the same way - confirm the source of the type where this matters.
- **A service that checks incoming Basic Authentication from its own callers, rather than sending it out to another server.** The tool cannot always tell which direction a credential is flowing in from the code in one file; a service that only accepts Basic Authentication from its callers has nothing to migrate to Relativity's token-based authentication. A service that decodes the header itself may still be reported as if it were sending credentials out, so confirm the direction before acting on a finding like this.
- **A single hand-built Authorization header that the tool reports as two or three separate findings at nearby locations.** The tool does not merge findings that come from more than one piece of code working together, such as a credential encoded on one line and used in a header on another. Each part is reported separately even though they are one migration, and all of the reported locations need to be fixed together.
- **A file whose analysis takes too long and is stopped by a time limit.** When this happens, every finding for that file is discarded, not just the part that was slow. The file is counted in the report's "Skipped items" section, so the loss is visible as a number, but which specific issues were lost in that file is not shown - such files should be reviewed by hand.
- **Which specific findings were left out when the tool reached its limit on the number of findings it reports, for a single file or for the whole scan.** The tool does limit how many findings it reports per file and overall, and the report states how many were left out - only aggregate counts are reported; the omitted findings themselves are not named or located individually.
- **Credentials, flow evidence or an authentication scheme supplied from another file: a dot-sourced script, an imported module, a loaded configuration or data file; and secret-store commands other than the three the tool lists.** The tool examines each file on its own and cannot join a definition in one file to its use in another. For many scripts the recommended flow therefore falls back to the default, which is not a failure of the tool: the note tells the developer to use the Authorization Code flow where the script acts for a signed-in user. A vault command it does not list is not read as evidence that the credential is fixed.
- **A header table or a header value built in another file or received as a parameter.** The value never appears in the text of the file the tool is reading, so there is nothing for it to match.
- **A splatted hashtable whose key is written more than twenty lines before the command that uses it, in another file, or built from $PSBoundParameters or $args.** The tool reads a splatted hashtable only within twenty lines before the command that splats it, including a table that is built key by key in that window with a dot write, an indexer or the Add method; a longer gap, or a table whose keys come from another file or from run-time parameters, is not followed.
- **Parameters supplied through $PSDefaultParameterValues.** The cmdlet call then carries no credential or authentication parameter in its own text, so the tool has nothing to match against.
- **A header written with a key or a value held in a variable, with no assignment that names the scheme in the five lines before it; and a value produced by the script's own function whose name does not carry the scheme word.** The scheme is not in the file's text near the write; a variable assigned a scheme-bearing value nearby, and a helper whose name carries the scheme word, are both read.
- **An authentication scheme held in a variable that the tool cannot read as a constant: set by Set-Variable or New-Variable, by a parameter default, as a member of an object or hashtable, by a multiple assignment, through $using:, written inside a double-quoted string as a bare variable, or declared in another file.** The finding is kept and is reported at a lower confidence level, so a header whose scheme is actually Bearer but is held in such a variable is reported as a possible Basic header and should be dismissed on review. A parameter default is left unresolved on purpose, because a caller can override it.
- **A variable written inside a double-quoted string as $name or ${name}.** The tool reads such a variable as part of the string's text and not as code. No construct can hide in a bare variable name, so detection is not affected, but a name that itself contains a quote character can end the string early for the tool and not for PowerShell.
- **Credential objects created in a form the tool does not read: a hashtable cast, -Property or a property assignment on an SDK credential type; a NetworkCredential built in one statement and assigned to a handler's Credentials through a variable; [Activator]::CreateInstance, reflection, or a type name computed from a string or a variable; an SDK entry point constructed directly from a user name and a password; and an SDK settings object with user-name and password properties.** The type name is never written out as plain text in these forms, or the assignment sees only a variable, so the tool has nothing to match against.
- **A user name and password joined by a colon and encoded by a helper whose body the file does not contain, by a native tool, or by a method other than the standard library's Base64 conversion.** The tool recognizes the standard library's conversion only; widening it to any call with Base64 in its name would read every conversion as the end of a credential pair. A header value that names the scheme, written next to the conversion, is still reported.
- **A user name and password written directly into a URL, such as "https://user:password@host", including through [UriBuilder].** The tool does not detect this form at all. This is a real exposure independent of the OAuth2 migration and should be searched for and removed regardless of which authentication scheme is in use.
- **Basic Authentication performed by a native program the script calls: curl.exe with -u or --user, an "Authorization: Basic ..." header argument, wget with --user.** An argument to an external command is text and not a PowerShell construct. On the newer edition of PowerShell curl and wget are these programs and not aliases of the web cmdlets.
- **Basic Authentication through other stacks that take the credential as a parameter: a COM request's open call with a user name and password or its SetCredentials, Start-BitsTransfer -CustomHeaders carrying a header string, and the proxy object of New-WebServiceProxy.** The scheme is implicit, or the header is a string argument the tool does not read. A COM request that writes the Authorization header with setRequestHeader is read.
- **A Basic header declared as text and turned into a table at run time: an expandable here-string of name-and-value lines converted by ConvertFrom-StringData, or a JSON text converted by ConvertFrom-Json.** The key and the scheme sit in the body of a string, which the tool reads as text and reports at the lowest level; a script that builds its header this way is reported only through its Base64 step when one is visible, and through nothing when the credential arrives already encoded.
- **A Base64-encoded value made of two parts separated by a colon that is not actually a credential, such as a host and a port.** The tool cannot always tell such values apart from an encoded user name and password pair, so a non-credential value with this same shape may occasionally be reported and should be dismissed on review.
- **A user name and password whose halves, or whose joined pair, reach the Base64 conversion through a pipeline: as $_ or $PSItem inside a ForEach-Object script block, or accumulated in its Begin, Process and End blocks.** The tool does not follow a value from one stage of a pipeline to the next, so the pair is never written in one of the spellings it reads. A pair written whole before the pipeline that encodes it is read. A script built this way is reported through its header write when the scheme is visible, and through nothing otherwise.
- **A user name and password accumulated over two statements with +=, such as a variable assigned the user name and then appended with a colon and the password.** The second statement alone is a colon followed by a value, which the tool reads as a token carried under the Basic scheme. The construct is reported at the same confidence level and with the same flow advice, but as that other kind of finding, so read its guidance with that in mind.
- **Whether a constructed credential, header or Base64 pair is ever sent, is reached on a path that runs, or goes to an HTTP server and not to another protocol: a header built and only logged, code in a branch that never runs, a web client aimed at an ftp:// address, a Base64 pair placed in a request body.** Knowing this takes control-flow and data-flow analysis that a tool reading the text of each file does not have. Such code is still reported at its confidence level, and is a false positive to dismiss on review.
- **PowerShell data and configuration files and other containers: constructs in module and data manifests (.psd1 files), DSC documents, .ps1xml, .psc1, .cdxml and .mof files, and credentials stored in configuration files.** No detection is designed for a data file, and configuration files are not source code in any language the tool scans; this is where a credential usually lives, so review those files separately.
- **PowerShell code inside another file type, and code inside an Add-Type -TypeDefinition here-string, a -Command string or an Invoke-Expression argument.** The inner code is read as the text of the string that holds it and is not analyzed as code; the damage is bounded to that string and the rest of the file is read correctly. PowerShell embedded in a batch file, a pipeline definition or a SQL script is a different file type that the tool does not scan.
- **A call written inside the "$( ... )" part of an expandable here-string (a multi-line string opened with @" and closed with "@), or a user name and password written as a colon pair in the body of one.** The tool reads the body of such a string as plain text, so a credential prompt or an encoding call written inside one of its embedded expressions is not seen, and neither is a pair spelled across its embedded variables.
- **A string delimited by a typographic quote other than the double pair and the single pair the tool reads (U+201C and U+201D, U+2018 and U+2019), or opened with one member of a quote set and closed with another.** The tool reads such a string wrongly from its opening quote to the next quote of its kind, and in PowerShell a string can span lines, so the misreading can reach the end of the file: code after it may be read as text and not examined. Replace such quotes with ordinary ones and scan again.
- **The name of a command that decides a finding, written so that the tool cannot read it: a backtick inside the name, a computed name invoked with &, a user-defined alias for a web cmdlet, or Invoke-Expression.** The command name is not text the tool's matching sees.
- **A PowerShell script saved in a single-byte text encoding other than Windows-1252 (for example a Cyrillic or Central European code page), rather than in UTF-8, UTF-16 or Windows-1252.** A file that is not UTF-8 or UTF-16 is read as Windows-1252, which covers Latin-1 and the other Western European files. A file in another single-byte code page that holds a byte Windows-1252 leaves undefined cannot be read that way: it is counted among the files that could not be read as text, and nothing inside it is examined for Basic Authentication. One that holds only bytes Windows-1252 defines is read, but its accented or other non-ASCII characters come out as different characters. Convert those files to UTF-8 and scan again, or review them by hand.
- **Flow evidence the tool's signals do not list: an encrypted standard string read with Get-Content and converted without -AsPlainText, a vault command other than the three it lists, and credentials passed in with $using:.** PowerShell has many ways to hold a fixed credential, and the tool reads the nine with a stable shape. Where none is present the recommended flow is the default one.
- **A password written as a quoted literal and turned into a secure string, then wrapped in a credential object and passed to `-Credential`: `ConvertTo-SecureString '<literal>' -AsPlainText` (or any abbreviation of `-AsPlainText`).** The in-code secret warning is read from the construct that captured the credential, not by following a variable back to the literal that built it. A finding on the call that receives the credential is reported at its level, but with no literal-credential warning, whereas a literal written straight into `New-Object System.Net.NetworkCredential('<user>', '<password>')` or into a Basic `Authorization` header does raise it. A reader who uses the warning to find the secrets to rotate will miss these. Search for them by hand, in every script and module, with `rg -n -i --glob '*.ps1' --glob '*.psm1' 'ConvertTo-SecureString'`, and read each hit: one whose string argument is a quoted literal, with `-AsPlainText` or an abbreviation of it on the same command, is a password written into the script. Rotate each password found.
